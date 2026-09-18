using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Gnothi.MobileShared;
using Microsoft.JSInterop;

namespace Gnothi.Desktop;

// THE FACE INSIDE GNOTHI'S OWN WINDOW -- the phone's shape, on the desktop, on the three systems.
//
// On the phone the face is a WebView Gnothi puts over its screen, full size, with "back". Here
// Gnothi's window IS a web page (the one Blazor paints), so the face is a frame in that page:
// an <iframe> on its own origin, filling the window under a bar that says whose it is and takes
// you back. One window, as asked; and nothing Windows-only in it, unlike the WebView2 window in
// WebView2WebModule.cs, which stays as the proof that ran first.
//
// HOW THE FRAME GETS ITS FILES. Not through Photino's custom scheme: a frame's request to a
// second scheme never reached the handler (measured 2026-09-14, zero calls). Instead Gnothi
// opens ONE LOOPBACK LISTENER PER LIVE MODULE, on 127.0.0.1 and a port of its own, and the frame
// is `http://127.0.0.1:<puerto>/`. The port is what makes the origin: two modules, two ports, two
// origins, and the same-origin rule keeps them apart as on the phone. A loopback IP is a secure
// context in Chromium and WebKit. And a real HTTP answer carries real headers, so the CSP that
// BundleServer composes IS applied here -- which Photino's scheme could never do. This listener
// is a shell of the Host, like the stdio one: the module has no transport of its own, it asks
// its origin and Gnothi answers from its files. Nothing of the journal or the bridge goes through
// the port: the bridge is postMessage between the frame and Gnothi's page, and the courier is C#.
//
// THE ONE THING THE PHONE HAS AND A FRAME DOES NOT: a native "run this script" call that the
// page's CSP does not see. Here the Host's JavaScript arrives as a message and the shim
// evaluates it, and that needs 'unsafe-eval' in the module's script-src. It is added to the CSP
// for this face only, and written down here: it lets the module's own code use eval too. On the
// desktop the hybrid's domain runs in process anyway, so containment is not this form's claim.
// The rest of the policy -no network but its origin, no workers, no frames, no forms out- stays
// as composed.
internal sealed class FrameWebModuleHost : WebModuleHost<FrameWebModule>, IFaces
{
	// For the static [JSInvokable] entry, which has nowhere else to find the host.
	internal static FrameWebModuleHost? Current { get; private set; }

	private readonly ISecretStore store;
	private readonly Action<string> log;
	private readonly ConcurrentDictionary<string, FrameWebModule> frames = new(StringComparer.OrdinalIgnoreCase);

	internal FrameWebModuleHost(ISecretStore store, Action<string> log)
	{
		this.store = store;
		this.log = log;
		Current = this;
	}

	// THE PAGE'S SIDE, filled in by Root once Blazor has rendered: put a frame on screen, take it
	// off, run JavaScript in it, and evaluate an expression in it (for tests). Until Root sets
	// them, no face can open -- and that is said, not hung.
	internal Func<string, string, Task>? Show;
	internal Func<string, Task>? Hide;
	internal Func<string, string, Task>? RunJs;
	internal Func<string, string, string, Task>? EvalIn;

	internal bool PageReady => Show is not null && RunJs is not null;

	protected override FrameWebModule Create(string module, string bundle, string journal, string publisher)
	{
		var face = new FrameWebModule(this, module, bundle, journal, publisher, store, log);
		frames[module] = face;
		return face;
	}

	internal void Forget(string module) => frames.TryRemove(module, out _);

	// A bridge message a frame posted, forwarded by the page.
	internal void FromFrame(string module, string raw)
	{
		if (frames.TryGetValue(module, out FrameWebModule? face)) face.OnMessage(raw);
		else log($"marco: mensaje de '{module}', que no está vivo");
	}

	public async Task<IFace> OpenFace(Gnothi.Host.Module m, CancellationToken ct) => await Open(m, ct);

	public void Close(string module)
	{
		if (frames.TryGetValue(module, out FrameWebModule? face)) face.Visible = false;
		if (IsLive(module)) Evict(module);
	}

	public string PinToDesktop(Gnothi.Host.Module m, string modulesRoot, string journalsRoot) =>
		DesktopShortcut.Pin(m, modulesRoot, journalsRoot, log);
}

// The one door from the page into C#: `DotNet.invokeMethodAsync('Gnothi.Desktop', 'FromFrame', module, raw)`.
public static class FrameBridge
{
	[JSInvokable("FromFrame")]
	public static void FromFrame(string module, string raw) => FrameWebModuleHost.Current?.FromFrame(module, raw);
}

internal sealed class FrameWebModule : WebModule, IFace
{
	// The shim is served as a file of the module's own origin, so `script-src 'self'` admits it
	// without a hash -- a hash would have to be in the CSP BundleServer already composed.
	private const string ShimPath = "/__gnothi/puente.js";

	private readonly FrameWebModuleHost host;
	private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> evals = new();
	private readonly int port;
	private HttpListener? listener;
	private BundleServer? server;
	private volatile bool mounted;
	private int nextEval;

	internal FrameWebModule(
		FrameWebModuleHost host, string module, string bundle, string journal, string publisher,
		ISecretStore store, Action<string> log)
		: base(module, bundle, journal, publisher, store, log)
	{
		this.host = host;
		port = FreePort();
	}

	// The bar above the frame shows the institution's identity itself; nothing to set here.
	public string Title { set { } }

	int IFace.Violations => Violations;

	protected override bool Alive => mounted;

	// The origin is the port: chosen at construction, because Start asks for the origin before
	// it asks to create.
	protected override string OriginFor(string basePath) => $"http://127.0.0.1:{port}{basePath}";

	protected override async Task Create(BundleServer bundleServer, string url, CancellationToken ct)
	{
		server = bundleServer;
		if (host.Show is null)
			throw new InvalidOperationException("la página de Gnothi todavía no está lista para mostrar un módulo");

		var l = new HttpListener();
		l.Prefixes.Add($"http://127.0.0.1:{port}/");
		l.Start();
		listener = l;
		_ = Task.Run(() => Accept(l));
		Log($"{Name}: sirviendo la cara en http://127.0.0.1:{port}/ (solo loopback)");

		mounted = true;
		Visible = true;
		await host.Show(Name, url);
	}

	protected override void RunJs(string js)
	{
		if (!mounted) return;
		_ = host.RunJs?.Invoke(Name, js);
	}

	protected override void Teardown()
	{
		mounted = false;
		host.Forget(Name);
		HttpListener? going = listener;
		listener = null;
		try { going?.Stop(); going?.Close(); } catch { /* already gone */ }
		_ = host.Hide?.Invoke(Name);
	}

	// ---------------------------------------------------------------- the loopback shell

	private static int FreePort()
	{
		var probe = new TcpListener(IPAddress.Loopback, 0);
		probe.Start();
		int port = ((IPEndPoint)probe.LocalEndpoint).Port;
		probe.Stop();
		return port;
	}

	private async Task Accept(HttpListener l)
	{
		while (l.IsListening)
		{
			HttpListenerContext ctx;
			try { ctx = await l.GetContextAsync(); }
			catch (HttpListenerException) { break; }
			catch (ObjectDisposedException) { break; }
			catch (InvalidOperationException) { break; }
			_ = Task.Run(() => Handle(ctx));
		}
	}

	private async Task Handle(HttpListenerContext ctx)
	{
		HttpListenerResponse res = ctx.Response;
		try
		{
			string path = ctx.Request.Url?.AbsolutePath ?? "/";

			if (path == ShimPath)
			{
				res.StatusCode = 200;
				res.ContentType = "text/javascript";
				res.Headers["Cache-Control"] = "no-store";
				await Write(res, Encoding.UTF8.GetBytes(Shim));
				return;
			}

			if (server is null) { res.StatusCode = 503; return; }

			ServedFile served = server.Serve(path);
			res.StatusCode = served.Status;
			res.ContentType = served.Mime;
			foreach (KeyValuePair<string, string> h in served.Headers)
			{
				// The one relaxation of this face, explained at the top of the file.
				res.Headers[h.Key] = h.Key.Equals("Content-Security-Policy", StringComparison.OrdinalIgnoreCase)
					? h.Value.Replace("script-src ", "script-src 'unsafe-eval' ", StringComparison.Ordinal)
					: h.Value;
			}

			byte[] body = served.Body ?? File.ReadAllBytes(served.Path!);
			if (served.Body is not null && served.Mime.StartsWith("text/html", StringComparison.OrdinalIgnoreCase))
				body = WithShimTag(body);

			await Write(res, body);
		}
		catch (Exception ex)
		{
			Log($"{Name}: could not serve {ctx.Request.Url?.AbsolutePath}: {ex.GetType().Name}: {ex.Message}");
			try { res.StatusCode = 500; } catch { /* headers already out */ }
		}
		finally
		{
			try { res.Close(); } catch { /* client gone */ }
		}
	}

	private static async Task Write(HttpListenerResponse res, byte[] body)
	{
		res.ContentLength64 = body.Length;
		await res.OutputStream.WriteAsync(body);
	}

	// The shim goes in at the top of the document, before anything of the module's, as an
	// external script of the module's own origin.
	private static byte[] WithShimTag(byte[] document)
	{
		string html = Encoding.UTF8.GetString(document);
		int head = html.IndexOf("<head", StringComparison.OrdinalIgnoreCase);
		int close = head >= 0 ? html.IndexOf('>', head) : -1;
		string tag = $"<script src=\"{ShimPath}\"></script>";
		html = close >= 0 ? html.Insert(close + 1, tag) : tag + html;
		return Encoding.UTF8.GetBytes(html);
	}

	// ---------------------------------------------------------------- what a test needs to SEE

	// An expression evaluated in the frame, answered back through the bridge as an `evalr`.
	public async Task<string> Eval(string js)
	{
		if (!mounted || host.EvalIn is null) return "null";
		string id = Interlocked.Increment(ref nextEval).ToString();
		var wait = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
		evals[id] = wait;
		try
		{
			await host.EvalIn(Name, id, js);
			return await wait.Task.WaitAsync(TimeSpan.FromSeconds(10));
		}
		finally { evals.TryRemove(id, out _); }
	}

	// No capture from inside the page; a test captures Gnothi's window from outside.
	public Task Screenshot(string path) => Task.CompletedTask;

	// ---------------------------------------------------------------- the bridge, page -> module

	internal void OnMessage(string raw)
	{
		try
		{
			using JsonDocument payload = JsonDocument.Parse(raw);
			JsonElement root = payload.RootElement;
			string op = Text(root, "op");
			if (op == "console") { Log($"modulo:{Name}: {Text(root, "a")}"); return; }
			if (op == "probe") { Log($"{Name}: P0 · {Text(root, "a")}"); return; }
			if (op == "evalr")
			{
				if (evals.TryGetValue(Text(root, "id"), out TaskCompletionSource<string>? wait)) wait.TrySetResult(Text(root, "a"));
				return;
			}
			Serve(op, Text(root, "id"), Optional(root, "a"), Optional(root, "b"));
		}
		catch (JsonException ex)
		{
			Log($"{Name}: unreadable bridge message: {ex.Message}");
		}
	}

	private static string Text(JsonElement o, string field) => Optional(o, field) ?? "";

	private static string? Optional(JsonElement o, string field) =>
		o.TryGetProperty(field, out JsonElement v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

	// THE SHIM: the seven verbs the iOS shim publishes, over postMessage to the parent page, plus
	// the inbox where the Host's JavaScript lands -- only from the parent, only Gnothi's own code.
	// `gnothiPuente` is the institution's API and is not translated.
	//
	// Upwards the target origin is '*': the parent is the window that created this frame, Gnothi's
	// page, and the shim does not know that page's origin (Photino's). Downwards frames.js does
	// name this frame's origin, which is the direction that carries the journal.
	private const string Shim = """
		(function () {
		  var up = window.parent;
		  var send = function (m) { up.postMessage({ gnothi: 'puente', m: JSON.stringify(m) }, '*'); };

		  window.gnothiPuente = {
		    listo:     function ()             { send({ op: 'listo',    id: '' }); },
		    contesta:  function (id, j, e)     { send({ op: 'contesta', id: id, a: j, b: e }); },
		    listar:    function (id)           { send({ op: 'listar',   id: id }); },
		    leer:      function (id, path)     { send({ op: 'leer',     id: id, a: path }); },
		    escribir:  function (id, path, c)  { send({ op: 'escribir', id: id, a: path, b: c }); },
		    pull:      function (id, from)     { send({ op: 'pull',     id: id, a: String(from) }); },
		    consultar: function (id, tool, a)  { send({ op: 'consultar', id: id, a: tool, b: a || '{}' }); },
		    violation: function (r)            { send({ op: 'violation', id: '', a: r }); },
		    __send: send
		  };

		  window.addEventListener('message', function (e) {
		    if (e.source !== up || !e.data || typeof e.data !== 'object') return;
		    if (e.data.gnothi === 'js') {
		      try { (0, eval)(e.data.js); }
		      catch (x) { send({ op: 'console', id: '', a: 'error evaluando lo que mandó Gnothi: ' + x }); }
		    } else if (e.data.gnothi === 'eval') {
		      var r; try { r = String((0, eval)(e.data.js)); } catch (x) { r = 'error: ' + x; }
		      send({ op: 'evalr', id: e.data.id, a: r });
		    }
		  });

		  try {
		    send({ op: 'probe', id: '', a:
		      'secureContext=' + !!window.isSecureContext +
		      ' crypto.subtle=' + !!(window.crypto && window.crypto.subtle) +
		      ' origin=' + window.location.origin });
		  } catch (x) { }

		  ['log', 'warn', 'error'].forEach(function (level) {
		    var before = console[level].bind(console);
		    console[level] = function () {
		      try { send({ op: 'console', id: '', a: level + ': ' + Array.prototype.join.call(arguments, ' ') }); }
		      catch (x) { }
		      before.apply(console, arguments);
		    };
		  });
		})();
		""";
}
