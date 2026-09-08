using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Gnothi.Host;
using System.Text.Json;

namespace Gnothi.Mobile;

// GATE 3'S DOOR INTO GNOTHI, from another app on the same phone.
//
// This is the half of Gate 3 that Alvaro's 08 calls "lo riesgoso": an adapter APK owned by a
// module -the "caratula"- reaches Dispatcher.Serve ACROSS PROCESSES, with Gnothi CLOSED. It
// is the only server surface the phone app has; everything MCP is deliberately left out of
// the APK.
//
// WHY MESSENGER and not AIDL: same cross-process semantics, no codegen, and the caller
// identity still arrives (Message.SendingUid -- NOT Binder.CallingUid, which by the time the
// Handler runs would report ourselves; that gotcha cost a measurement to find).
//
// THE IDENTITY CHECK IS NOW HERE, in Authorize below. It was not, and while it was not this
// service was exported and unguarded: any app on the phone could bind and read Sonia's
// journal. What changed is only the door -- Dispatcher.Serve still does not know who called,
// which is the property R14 asks for and what keeps the gates interchangeable.
//
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
			// THE IDENTITY. SendingUid, because by the time a Messenger handler runs,
			// Binder.CallingUid already reports ourselves -- measured, and exactly the kind of
			// gotcha the spike existed to surface.
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
				// THE GATE, AND IT LIVES HERE ON PURPOSE.
				//
				// Alvaro's R14 says it plainly: "Dispatcher.Serve no sabe quien lo llamo".
				// That ignorance is what makes the gates interchangeable -- the same request
				// answered the same way whichever door it came through. So the caller check
				// belongs to the DOOR and must not leak past it. Nothing below this block
				// tells the core who was on the other side.
				//
				// What is checked, in order of how hard it is to fake:
				//   1. the module names a caratula at all      (absent => nobody may enter)
				//   2. the calling package matches it          (a name; anyone can claim one)
				//   3. its SIGNATURE matches                   (this is the real gate)
				//
				// The signature is looked up BY US from the uid the kernel gave us. It is
				// never read from the message: a caller that could state its own identity
				// would not be a gate, it would be a form.
				string denial = Authorize(module, uid, pkgs);
				if (denial.Length > 0)
				{
					host.Log($"DENIED {module}.{tool} from uid={uid}: {denial}");
					reply.PutBoolean("authorized", false);
					// The caller is told it was refused and NOT why: whether the module exists,
					// whether it has a caratula, and which signature was expected are all
					// facts about Sonia's device that an impostor would happily enumerate.
					reply.PutString("json", "not authorized");
					reply.PutLong("serveMs", 0);
					reply.PutLong("catalogMs", coldCatalogMs);
					Answer(msg, reply);
					return;
				}

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

			Answer(msg, reply);
		}

		private static void Answer(Message msg, Bundle reply)
		{
			Messenger? back = msg.ReplyTo;
			if (back is null) return;
			Message answer = Message.Obtain()!;
			answer.Data = reply;
			back.Send(answer);
		}

		// Returns "" when the caller may proceed, or the reason it may not -- for OUR log, not
		// for the caller. Everything here is derived from the uid the kernel handed us; the
		// message contributes only the module NAME, which is a request, not a claim.
		private string Authorize(string module, int uid, string[] pkgs)
		{
			Module? m = host.Core().Modules.Modules
				.FirstOrDefault(x => string.Equals(x.Name, module, StringComparison.OrdinalIgnoreCase));

			if (m is null) return $"no module named '{module}'";

			AllowedCaller? declared = m.Installation.AllowedCaller;
			if (declared is null || !declared.IsDeclared)
				return "the module declares no caratula, so nobody may speak for it";

			// A uid can host several packages when they share a sharedUserId. Any of them
			// matching is enough -- they are the same trust domain by the OS's own rules.
			bool nameMatched = false;
			foreach (string pkg in pkgs)
			{
				if (string.Equals(pkg, declared.Package, StringComparison.Ordinal)) nameMatched = true;
				string sig = SignatureOf(pkg);
				if (sig.Length == 0) continue;
				if (declared.Accepts(pkg, sig)) return "";
			}

			// The two refusals are told apart ON PURPOSE, and only in OUR log. Measured: when
			// both said the same thing, a real denial read as
			// "none of [x] matches the declared caratula 'x'" -- the same name on both sides,
			// which looks like a bug and is in fact the gate doing its job on the signature.
			return nameMatched
				? $"'{declared.Package}' is declared, but its signature is not the declared one"
				: $"none of [{string.Join(",", pkgs)}] is the declared caratula '{declared.Package}'";
		}

		// SHA-256 of the caller's signing certificate, hex uppercase. "" when it cannot be
		// determined, and an empty answer NEVER passes the check.
		//
		// GetSigningCertificates is the flag that matters: the older GetSignatures reports the
		// ORIGINAL signer of a rotated key, which would keep accepting a certificate the
		// institution has already retired.
		private string SignatureOf(string package)
		{
			try
			{
				PackageManager pm = host.PackageManager!;
				PackageInfo? info = pm.GetPackageInfo(package, PackageInfoFlags.SigningCertificates);
				Android.Content.PM.Signature[]? signers = info?.SigningInfo?.GetApkContentsSigners();
				if (signers is null || signers.Length == 0) return "";

				// More than one signer means the APK carries a certificate chain; the contents
				// signer we compare against is the first, which is what apksigner prints.
				byte[] der = signers[0].ToByteArray()!;
				byte[] hash = System.Security.Cryptography.SHA256.HashData(der);
				return Convert.ToHexString(hash);
			}
			catch (Exception e)
			{
				host.Log($"could not read the signature of {package}: {e.GetType().Name}");
				return "";
			}
		}
	}
}
