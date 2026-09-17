using Android.App;
using Android.Content;
using Android.OS;
using Gnothi.Host;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

// THE PERMISSIONS THIS SPIKE DEMANDS, AND THEY ARE PART OF THE MEASUREMENT -- not paperwork.
//
// A socket held open indefinitely needs a foreground service, which means a PERMANENT NOTIFICATION.
// And since Android 14 a foregroundServiceType has to be declared: there is none for "local server",
// so it falls under specialUse, which Play reviews and demands a justification for.
//
// This was not discovered while writing the spike: it was researched beforehand (F1, "the background
// process, which is what this demands"). What the spike adds is that the declaration ends up written
// in the code, where it can be seen, instead of in a paragraph.
[assembly: Android.App.UsesPermission(Android.Manifest.Permission.ForegroundService)]
[assembly: Android.App.UsesPermission("android.permission.FOREGROUND_SERVICE_SPECIAL_USE")]
[assembly: Android.App.UsesPermission("android.permission.POST_NOTIFICATIONS")]

namespace Gnothi.Mobile;

// THE LOOPBACK DOOR -- A SPIKE, NOT A PRODUCT SURFACE.
//
// It exists to measure route 04 of the four PWA routes: "the app is only the face", where the
// institution's page asks Gnothi what to paint. When that face is web content in a BROWSER it
// cannot bind to IpcService -- a page has no uid and no APK -- so the only channel left that is
// an API and not a handoff is a fetch to loopback. This service is the other end of that fetch.
//
// WHAT IT DELIBERATELY MIRRORS. IpcService answers with Dispatcher.Serve and reports catalogMs
// and serveMs. This does the same, on purpose: the Binder door is already measured at 2 055 ms
// cold and 153 ms hot on an A54, and a latency number taken with a different stopwatch could not
// be put next to it.
//
// WHY TcpListener AND NOT HttpListener: System.Net.HttpListener is not dependable on Android.
// A minimal HTTP/1.1 responder is ~50 lines, has no platform risk, and for measuring a channel
// the parser is not the subject.
//
// WHY Exported AND STARTED FROM adb: so that measuring costs no change to the product. Nothing
// in MainActivity calls this. Start it with
//     adb shell am start-foreground-service -n com.ncubo.gnothi.host/.LoopbackSpikeService
// and read the port and the token from  adb logcat -s GNOTHI.
//
// THE TWO ROUTES THAT LOOK REDUNDANT ARE THE POINT. /gnothi/ping sends the CORS header and
// /gnothi/sincors does not. A browser applies TWO independent gates to this fetch -- the address
// space (Local Network Access) and the origin (CORS) -- and they fail the same way from
// JavaScript. Two routes separate them in one run.
[Service(Exported = true, Name = "com.ncubo.gnothi.host.LoopbackSpikeService",
	ForegroundServiceType = Android.Content.PM.ForegroundService.TypeSpecialUse)]
public sealed class LoopbackSpikeService : Service
{
	private const int Port = 47623;
	private const string Canal = "gnothi-loopback-spike";

	private static Dispatcher? dispatcher;
	private static readonly object gate = new();
	private static long coldCatalogMs = -1;

	private TcpListener? listener;
	private CancellationTokenSource? stopping;

	// The token is minted per service start and ONLY reaches logcat. In the real design it would
	// travel to the PWA in the deep link -- the channel that is already measured. Here it exists
	// to measure the thing without it: a port cannot tell who is knocking.
	private string token = "";

	private void Log(string s) => Android.Util.Log.Info("GNOTHI", $"loopback: {s}");

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

	public override IBinder? OnBind(Intent? intent) => null;

	public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
	{
		if (listener is not null) { Log("already listening"); return StartCommandResult.Sticky; }

		// THE NOTIFICATION FIRST, AND BEFORE ANYTHING ELSE. Android kills the service if it does not
		// call StartForeground within a few seconds. And that notification cannot be hidden or dismissed
		// while the socket lives: it is this channel's visible price, and the person sees it in their
		// status bar the whole time the PWA is in use.
		Frente();

		token = Guid.NewGuid().ToString("N")[..16];
		stopping = new CancellationTokenSource();

		// IPAddress.Loopback and NOT Any, and it is a claim worth being able to check: bound this
		// way the port does not exist for the local network, only for this device.
		listener = new TcpListener(IPAddress.Loopback, Port);
		listener.Start();
		Log($"listening on http://127.0.0.1:{Port}  token={token}");

		_ = Task.Run(() => Accept(stopping.Token));
		return StartCommandResult.Sticky;
	}

	// The permanent notification. It is assembled by hand and not with MAUI helpers because this
	// service has to be able to start WITHOUT any Activity having run -- which is precisely what makes
	// it measurable from adb without touching the product.
	private void Frente()
	{
		var gestor = (NotificationManager)GetSystemService(NotificationService)!;
		if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
			gestor.CreateNotificationChannel(new NotificationChannel(
				Canal, "Puerta de loopback (spike)", NotificationImportance.Low));

		Notification aviso = new Notification.Builder(this, Canal)
			.SetContentTitle("Gnothi está contestando en el dispositivo")
			.SetContentText($"Puerta de loopback abierta en el puerto {Port}")
			.SetSmallIcon(Android.Resource.Drawable.StatSysDownload)
			.SetOngoing(true)
			.Build();

		if (Build.VERSION.SdkInt >= BuildVersionCodes.UpsideDownCake)
			StartForeground(1, aviso, Android.Content.PM.ForegroundService.TypeSpecialUse);
		else
			StartForeground(1, aviso);

		Log("foreground con notificacion permanente, tipo specialUse");
	}

	public override void OnDestroy()
	{
		Log("stopping");
		try { stopping?.Cancel(); } catch { }
		try { listener?.Stop(); } catch { }
		listener = null;
		base.OnDestroy();
	}

	private async Task Accept(CancellationToken ct)
	{
		while (!ct.IsCancellationRequested)
		{
			TcpClient client;
			try { client = await listener!.AcceptTcpClientAsync(ct); }
			// Qualified: Android.OS has its own OperationCanceledException and the bare name is ambiguous
			// here.
			catch (System.OperationCanceledException) { return; }
			catch (Exception e) { Log($"accept failed: {e.GetType().Name}"); return; }

			try { await Answer(client); }
			catch (Exception e) { Log($"answer failed: {e.GetType().Name}: {e.Message}"); }
			finally { client.Dispose(); }
		}
	}

	private async Task Answer(TcpClient client)
	{
		NetworkStream net = client.GetStream();
		var reader = new StreamReader(net, Encoding.UTF8);

		string? request = await reader.ReadLineAsync();
		if (string.IsNullOrEmpty(request)) return;

		// The headers are read for ONE reason: Origin. It is the only hint the door gets about
		// who is calling, and it is worth logging to make the difference with Binder concrete --
		// there the identity is the caller's APK signature, read from the uid the kernel gave us.
		// Here it is a string the caller sends. A browser fills it honestly; a native app can
		// write whatever it likes.
		string origin = "", header;
		while (!string.IsNullOrEmpty(header = await reader.ReadLineAsync() ?? ""))
			if (header.StartsWith("Origin:", StringComparison.OrdinalIgnoreCase))
				origin = header[7..].Trim();

		string[] parts = request.Split(' ');
		string target = parts.Length > 1 ? parts[1] : "/";
		string path = target.Split('?')[0];
		Dictionary<string, string> q = Query(target);

		Log($"{parts[0]} {target}  Origin={(origin.Length == 0 ? "(ninguno)" : origin)}");

		var clock = System.Diagnostics.Stopwatch.StartNew();
		string body;
		bool cors = true;
		int status = 200;

		switch (path)
		{
			case "/gnothi/ping":
				body = JsonSerializer.Serialize(new { ok = true, puerto = Port, catalogMs = coldCatalogMs });
				break;

			// The same answer with the CORS header withheld. If the browser blocks THIS and lets
			// /gnothi/ping through, the gate that was in the way is the origin, not the address
			// space -- and from JavaScript both look like the same TypeError.
			case "/gnothi/sincors":
				body = JsonSerializer.Serialize(new { ok = true, sinCors = true });
				cors = false;
				break;

			case "/gnothi/consulta":
				{
					if (!string.Equals(q.GetValueOrDefault("token", ""), token, StringComparison.Ordinal))
					{
						status = 401;
						body = JsonSerializer.Serialize(new { error = "token" });
						Log("DENIED: wrong or missing token");
						break;
					}
					Response r = await Core().Serve(
						new Request(q.GetValueOrDefault("modulo", ""), q.GetValueOrDefault("tool", ""), null),
						CancellationToken.None);
					body = JsonSerializer.Serialize(new
					{
						authorized = r.Authorized,
						contenido = r.Authorized ? r.Content : null,
						motivo = r.Authorized ? null : r.Reason,
						catalogMs = coldCatalogMs,
					});
					break;
				}

			default:
				status = 404;
				body = JsonSerializer.Serialize(new { error = "ruta" });
				break;
		}
		clock.Stop();

		byte[] bytes = Encoding.UTF8.GetBytes(body);
		var head = new StringBuilder();
		head.Append($"HTTP/1.1 {status} {(status == 200 ? "OK" : status == 401 ? "Unauthorized" : "Not Found")}\r\n");
		head.Append("Content-Type: application/json; charset=utf-8\r\n");
		head.Append($"Content-Length: {bytes.Length}\r\n");
		head.Append($"X-Gnothi-Serve-Ms: {clock.ElapsedMilliseconds}\r\n");
		if (cors)
		{
			// '*' ON PURPOSE, AND IT IS THE FINDING, NOT AN OVERSIGHT. Any page the person visits
			// could read this. That is the property the Binder door does not have, and the reason
			// the design needs a token: the port cannot tell who is knocking.
			head.Append("Access-Control-Allow-Origin: *\r\n");
			head.Append("Access-Control-Expose-Headers: X-Gnothi-Serve-Ms\r\n");
		}
		head.Append("Connection: close\r\n\r\n");

		byte[] all = Encoding.UTF8.GetBytes(head.ToString()).Concat(bytes).ToArray();
		await net.WriteAsync(all);
		await net.FlushAsync();
		Log($"{path} -> {status} in {clock.ElapsedMilliseconds} ms, {bytes.Length} bytes");
	}

	private static Dictionary<string, string> Query(string target)
	{
		var found = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		int i = target.IndexOf('?');
		if (i < 0) return found;
		foreach (string pair in target[(i + 1)..].Split('&', StringSplitOptions.RemoveEmptyEntries))
		{
			string[] kv = pair.Split('=', 2);
			found[Uri.UnescapeDataString(kv[0])] = kv.Length > 1 ? Uri.UnescapeDataString(kv[1]) : "";
		}
		return found;
	}
}
