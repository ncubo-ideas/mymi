using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Android.App;
using Android.Webkit;
using Gnothi.Host;
using Java.Interop;

namespace Gnothi.Mobile;

// THE SALA ON ANDROID: ONE document, Gnothi's property, with the .NET runtime and the engine
// compiled to wasm. The modules arrive afterwards, as bytes, and run in there.
//
// WHAT THIS FILE DOES AND WHAT IT DOES NOT. It serves the bytes and passes the requests along. It
// decides nothing: not which module is loaded, not when it is evicted, not who is asking. That is
// decided by RoomShell, which is the one holding the frame of the call the Host opened.
//
// A SINGLE WEBVIEW, and that is the whole difference from the hosted form: there there is one per
// module and each brings its own 10 MB runtime; here there is one for all of them and the runtime
// is paid for once. Measured on an A54: 46.1 MB of linear memory for the substrate alone, and
// 4.1 MB for each module added on top.
//
// THE DOCUMENT HAS NO SCREEN. It is not added to any view: it exists in order to answer. The
// institution's face is still the hosted form, in its own origin, and it is mounted only when the
// person opens it.
internal sealed class AndroidRoom : IRoomHost
{
	// The sala's origin. https with a virtual host and not a scheme of our own, for the same
	// reason measured in the hosted form: without a secure context, what the runtime needs falls
	// over.
	private const string VirtualHost = "sala.gnothi.local";
	private const string Root = "https://" + VirtualHost + "/";

	private readonly Activity activity;
	private readonly Action<string> log;
	private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> waiting = new(StringComparer.Ordinal);

	// A SINGLE SLOT, AND IT IS THE MOST IMPORTANT CORRECTION IN THIS WHOLE FILE.
	//
	// This used to be a dictionary that ACCUMULATED every module of the session and was never
	// cleaned. Serve() answered `module/<anyone>/journal/<file>` for any of them, reading from
	// the real disk. And the CSP leaves `connect-src 'self'`, which is exactly this origin: which
	// means a domain inside the sala could ask over HTTP for ANOTHER module's journal, straight
	// off the disk, WITHOUT GOING THROUGH MEMORY and with the unmounting having nothing to do
	// with it. The sala's whole border was skipped with one fetch.
	//
	// Jonathan found it reasoning about the design, without seeing the code: in the box the
	// network is impossible because the syscall does not exist; here the web engine DOES have it,
	// so the only thing that limits it is the origin. And the origin is served by us.
	//
	// With a single slot, what is served is the module IN FLIGHT and nothing else. Everything
	// else is a 404 even if asked for by its exact name.
	private readonly object padlock = new();
	private string inFlight = "";
	private Prepared? prepared;

	private WebView? web;
	private TaskCompletionSource<bool>? ready;
	private int next;

	public AndroidRoom(Activity activity, Action<string> log)
	{
		this.activity = activity;
		this.log = log;
	}

	private sealed record Prepared(string Domain, string Entry, string Journal);

	// ---------------------------------------------------------------- what RoomShell asks for

	public Task Prepare(string module, string entry, string grantedFolder, CancellationToken ct)
	{
		// `entry` is the path of the .dll the manifest declares. Its folder is where the
		// module's other assemblies come from, which is the hybrid package's convention.
		string domain = Path.GetDirectoryName(entry) ?? "";

		lock (padlock)
		{
			// PREPARING THE NEXT ONE CLOSES THE PREVIOUS ONE. Nothing accumulates: the slot is one.
			if (inFlight.Length > 0 && !string.Equals(inFlight, module, StringComparison.Ordinal))
				log($"sala: {inFlight} deja de ser servible");

			inFlight = module;
			prepared = new Prepared(domain, Path.GetFileNameWithoutExtension(entry), grantedFolder);
		}

		log($"sala: {module} preparado desde {domain}");
		return Task.CompletedTask;
	}

	// RELEASING: after saving what the module wrote, it stops being servable. Between the
	// eviction and the next one's load there is NO servable module, which is what is wanted: if
	// something asked for a journal in that window, the answer is a 404.
	public Task Release(string module, CancellationToken ct)
	{
		lock (padlock)
		{
			if (!string.Equals(inFlight, module, StringComparison.Ordinal)) return Task.CompletedTask;
			inFlight = "";
			prepared = null;
		}

		log($"sala: {module} soltado, ya no se sirve por el origen");
		return Task.CompletedTask;
	}

	// THE ONLY SERVABLE THING IS THE MODULE IN FLIGHT. A name other than that one does not exist
	// for the origin, even if it is an installed module and even if it is asked for by its exact
	// path.
	private Prepared? Servable(string module)
	{
		lock (padlock)
		{
			return string.Equals(inFlight, module, StringComparison.Ordinal) ? prepared : null;
		}
	}

	// WHAT THE MODULE WROTE, LANDING ON THE DISK. And the fence is the same one the hosted form's
	// bridge has: the path the module gives is RELATIVE, Gnothi anchors it to ITS folder, and if
	// the result falls outside it is not written. It is the Host's responsibility, not trust in
	// the module.
	public Task Save(string module, string path, string contentBase64, CancellationToken ct)
	{
		// AND IT IS ONLY SAVED FOR THE ONE IN FLIGHT. Saving for a module already released would be
		// writing into the folder of somebody who is not there, at the request of somebody who is.
		if (Servable(module) is not Prepared p)
			throw new InvalidOperationException($"'{module}' no esta en vuelo: no se le guarda nada");

		string root = Path.GetFullPath(p.Journal);
		string target = Path.GetFullPath(Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar)));

		if (!target.StartsWith(root, StringComparison.Ordinal))
			throw new UnauthorizedAccessException($"ruta fuera del journal de {module}: {path}");

		Directory.CreateDirectory(Path.GetDirectoryName(target)!);
		byte[] bytes = Convert.FromBase64String(contentBase64);
		File.WriteAllBytes(target, bytes);

		log($"sala/guardar {module}: {path} · {bytes.Length} bytes");
		return Task.CompletedTask;
	}

	public async Task<string> Ask(string verb, string module, string a, string b, CancellationToken ct)
	{
		await Start(ct);

		string id = Interlocked.Increment(ref next).ToString();
		var wait = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
		waiting[id] = wait;

		string js = $"window.sala.pedido({J(id)}, {J(verb)}, {J(module)}, {J(a)}, {J(b)})";
		activity.RunOnUiThread(() => web!.EvaluateJavascript(js, null));

		using CancellationTokenRegistration _ = ct.Register(() => wait.TrySetCanceled(ct));
		string answer = await wait.Task;

		// CONFORMANCE IS LEFT IN THE LOG, not in a comment. Loading and evicting report what was
		// left of the OTHER modules in memory; if it ever says more than zero bytes, the border
		// between journals broke and this shouts it where it can be seen.
		// Not whole: the eviction carries the journal itself, see RoomShell.ForTheLog.
		if (verb is "cargar" or "desalojar") log($"sala/{verb} {module}: {Gnothi.Host.RoomShell.ForTheLog(answer)}");

		return answer;
	}

	// ---------------------------------------------------------------- the document

	// THROWING THE DOCUMENT AWAY, which is the only thing that recovers a sala that does not
	// answer. The shell asks for it when a request went past the ceiling, and the case that
	// motivates it is the eviction: in there runs the module's Dispose(), that is, code the
	// module chose, and an infinite loop cannot be interrupted from inside a single-threaded web
	// engine.
	//
	// FROM HERE IT CAN BE, because the document is an Android object and the engine runs in
	// ANOTHER process: destroying it takes the loop with it.
	//
	// AND IT TAKES THE BORDER WITH IT, which is what makes this an honest way out and not a
	// patch: what the module had mounted lives in the document's memory, not on disk, so there is
	// nothing here to unmount or to confirm. It also releases the slot, because a module without
	// a document has no reason to go on being servable by the origin.
	public Task Discard(string reason, CancellationToken ct)
	{
		log($"sala: se tira el documento -- {reason}");

		lock (padlock)
		{
			inFlight = "";
			prepared = null;
		}

		// The pending waits are not left hanging: the one that got stuck, and any other, fail with
		// the reason. The document where they were going to be answered no longer exists.
		foreach (string id in waiting.Keys)
			if (waiting.TryRemove(id, out TaskCompletionSource<string>? wait))
				wait.TrySetException(new InvalidOperationException($"se tiro la sala: {reason}"));

		WebView? old = Interlocked.Exchange(ref web, null);

		// AND ONLY NOW is the start-up forgotten, so that nobody is left waiting on a dead
		// document. The next request creates a new one; requests arrive serialised by `OneAtATime`
		// on the shell's side, so there are never two creating it at once.
		ready = null;

		activity.RunOnUiThread(() =>
		{
			try { old?.StopLoading(); old?.Destroy(); }
			catch (Exception e) { log($"sala: al tirar el documento -- {e.GetType().Name}: {e.Message}"); }
		});

		return Task.CompletedTask;
	}

	private async Task Start(CancellationToken ct)
	{
		if (ready is not null) { await ready.Task; return; }

		var starting = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		if (Interlocked.CompareExchange(ref ready, starting, null) is not null)
		{
			await ready!.Task;
			return;
		}

		log("sala: creando el documento");

		activity.RunOnUiThread(() =>
		{
			web = new WebView(activity);
			web.Settings!.JavaScriptEnabled = true;
			web.SetWebViewClient(new RoomServer(this));
			web.SetWebChromeClient(new RoomConsole(log));
			web.AddJavascriptInterface(new RoomBridge(this), "SalaPuente");
			web.LoadUrl(Root);
		});

		await starting.Task.WaitAsync(TimeSpan.FromSeconds(60), ct);
	}

	internal void Ready()
	{
		log("sala: el sustrato contesta");
		ready?.TrySetResult(true);
	}

	internal void Answered(string id, string json)
	{
		if (waiting.TryRemove(id, out TaskCompletionSource<string>? wait)) wait.TrySetResult(json);
	}

	// ---------------------------------------------------------------- where the bytes come from

	// THREE SOURCES FOR A SINGLE WEB ORIGIN. The substrate comes out of the apk's assets; the
	// module's .dll files, out of its installed folder; and the journal, out of the person's
	// folder. The module does not tell them apart: to it, it is all its own origin, and it never
	// opens a file in its life.
	internal Android.Webkit.WebResourceResponse? Serve(string path)
	{
		try
		{
			if (path.StartsWith("modulo/", StringComparison.Ordinal))
			{
				string[] parts = path.Split('/', 4);
				if (parts.Length < 3) return NotFound(path);

				string module = parts[1];
				if (Servable(module) is not Prepared p) return NotFound(path);

				// module/<m>/indice.json -- which assemblies it brings and which is the entry
				if (parts[2] == "indice.json")
				{
					// ONLY WHAT IS THE MODULE'S. Everything else is lent by the substrate, and that
					// is not an optimisation: a BCL package is compiled PER PLATFORM, and the one
					// in the module's folder is Android's. Measured: loading it inside the web
					// engine blows up with TypeLoadException over the ARM intrinsics.
					//
					// It is the same rule PackageInspector already enforces on the other side,
					// where a cargo package that brings its own engine is REJECTED.
					string[] files = [.. Directory.EnumerateFiles(p.Domain, "*.dll")
						.Select(Path.GetFileNameWithoutExtension)
						.Where(n => n is not null && !LentByTheRoom(n))
						.Select(n => n!)
						.OrderBy(n => string.Equals(n, p.Entry, StringComparison.Ordinal) ? 1 : 0)];

					return TextBody(JsonSerializer.Serialize(new { entrada = p.Entry, archivos = files }), "application/json");
				}

				// module/<m>/domain/<x>.dll
				if (parts[2] == "dominio" && parts.Length == 4)
					return FromFile(Path.Combine(p.Domain, parts[3]), "application/octet-stream");

				// module/<m>/journal/indice.json -- what is in the person's folder
				if (parts[2] == "journal" && parts.Length == 4 && parts[3] == "indice.json")
				{
					string[] paths = Directory.Exists(p.Journal)
						? [.. Directory.EnumerateFiles(p.Journal, "*", SearchOption.AllDirectories)
							.Select(f => Path.GetRelativePath(p.Journal, f).Replace(Path.DirectorySeparatorChar, '/'))]
						: [];

					return TextBody(JsonSerializer.Serialize(new { rutas = paths }), "application/json");
				}

				// module/<m>/journal/<path> -- and it does NOT leave the granted folder
				if (parts[2] == "journal" && parts.Length == 4)
				{
					string root = Path.GetFullPath(p.Journal);
					string requested = Path.GetFullPath(Path.Combine(root, parts[3].Replace('/', Path.DirectorySeparatorChar)));
					if (!requested.StartsWith(root, StringComparison.Ordinal)) return NotFound(path);

					return FromFile(requested, "application/octet-stream");
				}

				return NotFound(path);
			}

			// everything else is the substrate, which travels in the apk
			return FromAssets(path);
		}
		catch (Exception e)
		{
			log($"sala: 500 {path} -- {e.GetType().Name}: {e.Message}");
			return NotFound(path);
		}
	}

	// THE CSP OF THE SALA'S DOCUMENT, AND IT IS COMPUTED BY BundleServer, NOT BY THIS CLASS.
	//
	// I had a copy here and it broke exactly as the repo already had documented: the HTML parser
	// normalises CRLF to LF before hashing the inline script, and my copy hashed the raw bytes.
	// With the file in LF it worked; the moment git left it in CRLF, the engine rejected the
	// script Gnothi had just allowed and the document came out BLANK.
	//
	// The fix already existed in BundleServer.AsTheParserSeesIt. What was missing was not the
	// fix: it was that there should be ONE copy. And in passing the sala inherits the same policy
	// as the hosted form, which is one less thing to audit.

	private WebResourceResponse FromAssets(string path)
	{
		// The document is read whole before being served, because its CSP depends on its content.
		// The other files travel as a stream, without being copied into memory.
		if (path.EndsWith(".html", StringComparison.Ordinal))
		{
			using Stream original = activity.Assets!.Open("sala/" + path);
			using var buffer = new MemoryStream();
			original.CopyTo(buffer);
			byte[] documento = buffer.ToArray();

			Dictionary<string, string> headers = Headers();
			headers["Content-Security-Policy"] = Gnothi.MobileShared.BundleServer.CspFor(documento);

			return new WebResourceResponse(MimeOf(path), "utf-8", 200, "OK", headers, new MemoryStream(documento));
		}

		Stream body = activity.Assets!.Open("sala/" + path);
		return new WebResourceResponse(MimeOf(path), "utf-8", 200, "OK", Headers(), body);
	}

	private static WebResourceResponse FromFile(string path, string type) =>
		File.Exists(path)
			? new WebResourceResponse(type, "utf-8", 200, "OK", Headers(), File.OpenRead(path))
			: new WebResourceResponse("text/plain", "utf-8", 404, "Not Found", Headers(), new MemoryStream());

	private static WebResourceResponse TextBody(string body, string type) =>
		new(type, "utf-8", 200, "OK", Headers(), new MemoryStream(Encoding.UTF8.GetBytes(body)));

	private WebResourceResponse NotFound(string path)
	{
		log($"sala: 404 {path}");
		return new WebResourceResponse("text/plain", "utf-8", 404, "Not Found", Headers(), new MemoryStream());
	}

	// THE LIST OF WHAT IS LENT, which is the other face of the installer's list of forbidden
	// things. What is here is put there by the substrate in its browser-wasm build; what is not,
	// travels in the module. If a module needed something that is not there, the answer is NOT to
	// load it from its folder: it is to add it to the substrate, and let `substrate.range`
	// negotiate it.
	private static bool LentByTheRoom(string assembly) =>
		assembly.StartsWith("System.", StringComparison.OrdinalIgnoreCase)
		|| assembly.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase)
		|| assembly is "Puppeteer" or "Choreography" or "Puppets.Portador" or "Gnothi.Contract"
		|| assembly.StartsWith("ModelContextProtocol", StringComparison.OrdinalIgnoreCase)
		|| assembly is "netstandard" or "mscorlib";

	private static Dictionary<string, string> Headers() => new() { ["Cache-Control"] = "no-store" };

	internal static string TheHost => VirtualHost;

	private static string MimeOf(string path) => Path.GetExtension(path).ToLowerInvariant() switch
	{
		".html" => "text/html",
		".js" or ".mjs" => "text/javascript",
		".css" => "text/css",
		".wasm" => "application/wasm",
		".json" => "application/json",
		_ => "application/octet-stream",
	};

	private static string J(string s) => JsonSerializer.Serialize(s);
}

internal sealed class RoomServer(AndroidRoom room) : WebViewClient
{
	public override WebResourceResponse? ShouldInterceptRequest(WebView? view, IWebResourceRequest? request)
	{
		Android.Net.Uri? url = request?.Url;
		if (url is null || !string.Equals(url.Host, AndroidRoom.TheHost, StringComparison.Ordinal))
			return null;

		string path = (url.Path ?? "/").TrimStart('/');
		if (path.Length == 0 || path.EndsWith("/", StringComparison.Ordinal)) path += "index.html";

		return room.Serve(path);
	}
}

internal sealed class RoomConsole(Action<string> log) : WebChromeClient
{
	public override bool OnConsoleMessage(ConsoleMessage? message)
	{
		if (message is not null) log($"sala-web: {message.Message()}");
		return true;
	}
}

// THE BRIDGE, AND WHAT IT DOES NOT HAVE. Two verbs: "I am here" and "here is the answer". There
// is no read, no write, no asking for anything -- the journal is put there by Gnothi before
// asking, and whatever the module writes stays in memory.
internal sealed class RoomBridge(AndroidRoom room) : Java.Lang.Object
{
	[JavascriptInterface]
	[Export("lista")]
	public void Ready() => room.Ready();

	[JavascriptInterface]
	[Export("contesta")]
	public void Answered(string id, string json) => room.Answered(id, json);
}
