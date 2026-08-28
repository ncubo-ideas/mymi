using Android.App;
using Android.Content;
using Android.OS;
using Gnothi.Host;
using System.Text.Json;

namespace Gnothi.Mobile;

// SPIKE 2 (Puerta 3) -- THE JUMP INTO GNOTHI FROM ANOTHER APP. Uncommitted spike code.
//
// This is the half of Puerta 3 that Alvaro's 08 calls "lo riesgoso": an adapter APK owned by
// a module has to reach Dispatcher.Serve ACROSS PROCESSES, with Gnothi CLOSED, and we have to
// measure what that costs and what identity Gnothi sees. Until now the phone app had NO
// server surface at all -- everything MCP was deliberately left out of the APK. This service
// is the smallest possible surface: a bound Messenger service.
//
// WHY MESSENGER and not AIDL: same cross-process semantics, no codegen, and the caller
// identity still arrives (Message.SendingUid -- NOT Binder.CallingUid, which by the time the
// Handler runs would report ourselves; that gotcha is exactly the kind of thing the spike
// exists to surface).
//
// WHAT THE SPIKE DELIBERATELY LEAVES OUT (and the real design adds): the identity CHECK.
// Alvaro's R13 gates the IPC on publisher+module-id+release-id+tool-schema-hash. Here we
// only MEASURE what the caller looks like; we do not yet reject anyone. Exported and
// unguarded -- spike-grade, not product-grade.
// Name= is NOT decoration: it fixes the generated JAVA class name. Without it the class is
// a crc64 hash, and a manifest entry naming this type resolves to nothing -- measured:
// "Not found; no service started".
[Service(Exported = true, Name = "com.ncubo.gnothi.host.IpcService")]
public sealed class IpcService : Service
{
	// The Host core, built ONCE per process and lazily -- binding to this service from
	// another app STARTS the Gnothi process without any Activity, and that is the point:
	// the catalog load + module hydration ARE the cold start the spike measures.
	private static Dispatcher? dispatcher;
	private static readonly object gate = new();
	private static long coldCatalogMs = -1;

	private Dispatcher Core()
	{
		if (dispatcher is not null) return dispatcher;
		lock (gate)
		{
			if (dispatcher is not null) return dispatcher;
			var clock = System.Diagnostics.Stopwatch.StartNew();
			var catalog = new ModuleCatalog(System.IO.Path.Combine(FilesDir!.AbsolutePath, "Modulos"));
			catalog.Load();
			dispatcher = new Dispatcher(catalog, new Meter());
			clock.Stop();
			coldCatalogMs = clock.ElapsedMilliseconds;
			Log($"catalog loaded in {coldCatalogMs} ms ({catalog.Modules.Count} modules)");
			return dispatcher;
		}
	}

	private Messenger? messenger;

	public override IBinder? OnBind(Intent? intent)
	{
		Log($"bound by uid {Binder.CallingUid}");   // valid HERE: OnBind runs inside the binder call
		messenger ??= new Messenger(new Inbox(this));
		return messenger.Binder;
	}

	private void Log(string s) => global::Android.Util.Log.Info("GNOTHI", $"ipc: {s}");

	private sealed class Inbox : Handler
	{
		private readonly IpcService host;
		public Inbox(IpcService host) : base(Looper.MainLooper!) => this.host = host;

		public override void HandleMessage(Message msg)
		{
			// THE IDENTITY, measured -- Alvaro's fourth spike result. SendingUid, because by
			// the time a Messenger handler runs, Binder.CallingUid is already ourselves.
			int uid = msg.SendingUid;
			string[] pkgs = host.PackageManager?.GetPackagesForUid(uid) ?? [];
			host.Log($"call from uid={uid} packages=[{string.Join(",", pkgs)}]");

			Bundle data = msg.Data ?? new Bundle();
			string module = data.GetString("module") ?? "";
			string tool = data.GetString("tool") ?? "";

			var clock = System.Diagnostics.Stopwatch.StartNew();
			Bundle reply = new();
			try
			{
				Response r = host.Core()
					.Serve(new Request(module, tool, null), CancellationToken.None)
					.GetAwaiter().GetResult();

				reply.PutBoolean("authorized", r.Authorized);
				reply.PutString("json", r.Authorized ? JsonSerializer.Serialize(r.Content) : r.Reason);
			}
			catch (Exception e)
			{
				reply.PutBoolean("authorized", false);
				reply.PutString("json", $"{e.GetType().Name}: {e.Message}");
			}
			clock.Stop();

			reply.PutLong("serveMs", clock.ElapsedMilliseconds);
			reply.PutLong("catalogMs", coldCatalogMs);
			host.Log($"{module}.{tool} served in {clock.ElapsedMilliseconds} ms");

			Messenger? back = msg.ReplyTo;
			if (back is not null)
			{
				Message answer = Message.Obtain()!;
				answer.Data = reply;
				back.Send(answer);
			}
		}
	}
}
