using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using CoreFoundation;
using CoreGraphics;
using Foundation;
using Gnothi.Host;
using UIKit;
using WebKit;

namespace Gnothi.iOS;

// THE SALA ON iOS: AndroidRoom.cs's TWIN. ONE document, Gnothi's property, with the .NET runtime
// and the engine compiled to wasm. The modules arrive afterwards, as bytes, and run in there.
//
// WHAT THIS FILE DOES AND WHAT IT DOES NOT. It serves the bytes and passes the requests along. It
// decides nothing: not which module is loaded, not when it is evicted, not who is asking. That is
// decided by RoomShell, which is the one holding the frame of the call the Host opened. And
// RoomShell is the SAME file on both platforms -- it lives in Gnothi.MobileShared -- so what
// changes here is how WebKit is spoken to, and nothing else.
//
// ── WHAT DIFFERS FROM ANDROID, AND IT IS LITTLE ───────────────────────────────────────────────
//
//   1 · THE ORIGIN IS A SCHEME OF OUR OWN, AND HERE IT COSTS NOTHING. Android serves the sala
//       over `https://sala.gnothi.local` because there a custom scheme is NOT a secure context,
//       and without a secure context what the runtime needs falls over. WebKit does not allow
//       handling `http` or `https` -- `SetUrlSchemeHandler` throws for the reserved schemes -- so
//       a custom scheme is not a preference, it is the only thing there is.
//
//       AND IT CAN BE DONE, BECAUSE IT IS ALREADY MEASURED. The hosted form's P0 probe answered
//       on 2026-09-07 in all four modules, on an iPhone 17 Pro simulator / iOS 26.5:
//
//           secureContext=true crypto.subtle=true storage=true origin=gnothi-mod://<module>
//
//       See HALLAZGOS-CORRIENDO-EL-GUION.md §1. Which means the reason Android uses a virtual
//       host does not exist on this side.
//
//       AND WHAT THAT MEASUREMENT DID NOT SAY -- whether the SUBSTRATE starts under a scheme
//       handler -- IS ALSO ANSWERED, because the probe below asked it again inside the sala.
//       Measured on 2026-09-21, same simulator, two runs:
//
//           sala: P0 · secureContext=true crypto.subtle=true storage=true wasm=true
//                      wasm.streaming=true origin=gnothi-sala://sala
//           sala: P0 · fetch(mismo origen)=200 basic
//           sala-web: log: la sala esta lista, hospedada por Gnothi. Esperando pedidos.
//
//       Without a single 404 and without a CSP rejection. The line that decides is the SECOND,
//       not the first: see point 6.
//
//   2 · THE BRIDGE IS ONE-WAY. `WKScriptMessageHandler` returns no value, so the substrate
//       answers with `postMessage` and the native side replies through `EvaluateJavaScript` with
//       the same id. It costs nothing: the sala's contract ALREADY worked that way --
//       `window.sala.pedido(id, ...)` going out and `SalaPuente.contesta(id, json)` coming back
//       -- because Android needed it for the opposite reason, namely that there the injected
//       methods are synchronous.
//
//   3 · THE SHIM IS NOT WRITTEN INTO `index.html`, AND THAT IS THE DECISION WORTH TELLING. The
//       substrate decides whether it is hosted with `!!window.SalaPuente`: if there is a bridge
//       it waits for requests, and if not, it runs the lab's measurement sweep. If the shim lived
//       in the document, `SalaPuente` would ALWAYS exist and the standalone lab would cease to
//       exist.
//
//       It goes in as a `WKUserScript` at `AtDocumentStart`, which runs before a single line of
//       the substrate and only when Gnothi hosts. The document is left untouched -- it is the
//       SAME file the substrate publishes for both platforms -- and `salaHospedada()` goes on
//       telling the truth. It is the same shape `IosWebModule.Shim` already uses for the hosted
//       form.
//
//   4 · THERE IS NO WebChromeClient. The substrate's console is captured by patching `console` in
//       that same user script and comes out through the same channel, just as in the hosted form.
//
//   5 · THE PROTOCOL OBJECTS GO SEPARATELY. WebKit's protocols are ObjC protocols, so whoever
//       implements them has to descend from `NSObject`. Android arrives at the same shape for the
//       mirror reason: `@JavascriptInterface` requires a `Java.Lang.Object`.
//
//   6 · WHAT REALLY HAD TO BE MEASURED WAS NOT THE SECURE CONTEXT: IT WAS `fetch`.
//
//       It is easy to believe P0 already covered it, and it does not. The hosted form NEVER
//       fetches through its origin: its subresources are requested by the document and
//       read/write/pull go through the bridge. The substrate cannot avoid it -- Blazor downloads
//       `_framework/*` with fetch, and `Sala.cs` asks for the module's .dll files and its journal
//       with HttpClient, which in wasm IS fetch -- so if WebKit did not hand the custom scheme to
//       fetch, the sala would not start EVEN IF the origin were secure, and plan B would be a
//       local server on 127.0.0.1 with its port and its life cycle.
//
//       It does hand it over: `fetch(mismo origen)=200 basic`. The local server is not needed.
//
// THE DOCUMENT HAS NO SCREEN. It is not added to any view: it exists in order to answer. The
// institution's face is still the hosted form, in its own origin, and it is mounted only when the
// person opens it.
internal sealed class IosRoom : IRoomHost
{
	// The sala's origin. A custom scheme and a fixed host: there is ONE sala for all the modules,
	// not one per module -- which is precisely the difference from `gnothi-mod://<module>`, where
	// the host is the module because there one origin each IS wanted.
	private const string Scheme = "gnothi-sala";
	private const string TheHost = "sala";
	private const string Root = Scheme + "://" + TheHost + "/";

	// The only channel: the bridge and the console both come back this way.
	private const string Channel = "SalaPuente";

	// WHERE THE SUBSTRATE COMES FROM INSIDE THE .app. The csproj puts it in as a BundleResource
	// with LogicalName `sala/...`, which is the exact mirror of Android's `Assets\sala\...`.
	private static string InTheBundle(string path) =>
		Path.Combine(NSBundle.MainBundle.BundlePath, "sala", path.Replace('/', Path.DirectorySeparatorChar));

	private readonly Action<string> log;
	private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> waiting = new(StringComparer.Ordinal);

	// A SINGLE SLOT. Same correction and same reason as on Android: if this accumulated the
	// session's modules, any domain inside the sala could ask through its own origin for ANOTHER
	// module's journal, straight off the disk, without going through memory and with the
	// unmounting having nothing to do with it. With a single slot what is servable is the module
	// IN FLIGHT and nothing else; the rest is a 404 even if asked for by its exact name.
	private readonly object padlock = new();
	private string inFlight = "";
	private Prepared? prepared;

	private WKWebView? web;
	private TaskCompletionSource<bool>? ready;
	private int next;

	internal IosRoom(Action<string> log) => this.log = log;

	private sealed record Prepared(string Domain, string Entry, string Journal);

	// ---------------------------------------------------------------- what RoomShell asks for

	public Task Prepare(string module, string entry, string grantedFolder, CancellationToken ct)
	{
		// `entry` is the path of the .dll the manifest declares. Its folder is where the module's
		// other assemblies come from, which is the hybrid package's convention.
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

	// RELEASING: after saving what the module wrote, it stops being servable. Between the eviction
	// and the next one's load there is NO servable module, which is what is wanted: if something
	// asked for a journal in that window, the answer is a 404.
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

	private Prepared? Servable(string module)
	{
		lock (padlock)
		{
			return string.Equals(inFlight, module, StringComparison.Ordinal) ? prepared : null;
		}
	}

	// WHAT THE MODULE WROTE, LANDING ON THE DISK. The fence is the same one the hosted form's
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
		DispatchQueue.MainQueue.DispatchAsync(() => web?.EvaluateJavaScript(new NSString(js), null));

		using CancellationTokenRegistration _ = ct.Register(() => wait.TrySetCanceled(ct));
		string answer = await wait.Task;

		// CONFORMANCE IS LEFT IN THE LOG, not in a comment. Loading and evicting report what was
		// left of the OTHER modules in memory; if it ever says more than zero bytes, the border
		// between journals broke and this shouts it where it can be seen.
		if (verb is "cargar" or "desalojar") log($"sala/{verb} {module}: {answer}");

		return answer;
	}

	// ---------------------------------------------------------------- the document

	// THROWING THE DOCUMENT AWAY, which is the only thing that recovers a sala that does not
	// answer. The shell asks for it when a request went past the ceiling, and the case that
	// motivates it is the eviction: in there runs the module's Dispose(), that is, code the module
	// chose, and an infinite loop cannot be interrupted from inside a single-threaded web engine.
	//
	// FROM HERE IT CAN BE, because the engine runs in ANOTHER process -- on iOS it is
	// com.apple.WebKit.WebContent and on Android the WebView's -- so releasing the view takes the
	// loop with it.
	//
	// AND IT TAKES THE BORDER WITH IT: what the module had mounted lives in the document's memory,
	// not on disk, so there is nothing here to unmount or to confirm. It also releases the slot,
	// because a module without a document has no reason to go on being servable.
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

		WKWebView? old = Interlocked.Exchange(ref web, null);

		// AND ONLY NOW is the start-up forgotten, so that nobody is left waiting on a dead
		// document. The next request creates a new one; requests arrive serialised by `OneAtATime`
		// on the shell's side, so there are never two creating it at once.
		ready = null;

		if (old is not null)
			DispatchQueue.MainQueue.DispatchAsync(() =>
			{
				try
				{
					old.StopLoading();
					// The channel's handler RETAINS the bridge, and the bridge retains this sala.
					// Without removing it, the old document is never released -- it is the same
					// clean-up `IosWebModule.Teardown` does with its own.
					old.Configuration.UserContentController.RemoveScriptMessageHandler(Channel);
					old.RemoveFromSuperview();
				}
				catch (Exception e) { log($"sala: al tirar el documento -- {e.GetType().Name}: {e.Message}"); }
			});

		return Task.CompletedTask;
	}

	// STARTING THE SALA WITHOUT ANYBODY HAVING ASKED, AND IT IS AN INSTRUMENT OF MEASUREMENT.
	//
	// `Start` is private and is triggered by the first `Ask`, which in turn requires an
	// installed module forced into the sala. But the question that decides the form on iOS --
	// whether a Blazor wasm document starts under a scheme handler -- needs NO module: it is
	// answered with the substrate alone. Separating them matters, because if they were measured
	// together an installation failure would read the same as an origin failure.
	//
	// It is only called by ViewController's measurement switch, which is already in DEBUG.
	internal Task WarmUp(CancellationToken ct) => Start(ct);

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

		DispatchQueue.MainQueue.DispatchAsync(() =>
		{
			try
			{
				var configuration = new WKWebViewConfiguration();
				configuration.SetUrlSchemeHandler(new RoomServer(this), Scheme);

				WKUserContentController content = configuration.UserContentController;
				content.AddScriptMessageHandler(new RoomBridge(this), Channel);
				content.AddUserScript(new WKUserScript(
					new NSString(Shim), WKUserScriptInjectionTime.AtDocumentStart, isForMainFrameOnly: true));

				// It is NOT added to any view: the sala has no screen, it exists in order to answer.
				web = new WKWebView(CGRect.Empty, configuration);

#if DEBUG
				// Without this Safari's inspector does not see this WebView. Since iOS 16.4 it has
				// to be said, and the default is false. Only in DEBUG: an inspectable WebView lets
				// the page's memory be read over a cable, and the person's journal is in there.
				// Same criterion as `IosWebModule`.
				if (OperatingSystem.IsIOSVersionAtLeast(16, 4)) web.Inspectable = true;
#endif

				web.LoadRequest(new NSUrlRequest(new NSUrl(Root)));
			}
			catch (Exception e)
			{
				log($"sala: no se pudo crear el documento -- {e.GetType().Name}: {e.Message}");
				starting.TrySetException(e);
			}
		});

		// THE START-UP CEILING IS 60 s AS ON ANDROID, AND IT IS NOT THE ONE IN CHARGE.
		//
		// `RoomShell.Ask` wraps EACH verb with `RoomShell.Ceiling`, which is 10 s, and the start-up
		// happens inside the first verb. Which means the shell's 10 expire before the 60 here: at
		// 10 s the shell throws the document away and the next request starts a new one.
		//
		// IT WAS FEARED THAT THIS WOULD BE THE NORMAL CASE ON THE SIMULATOR, because the hosted
		// form measured ~64 s cold there (HALLAZGOS §11.4) and with that number the sala would
		// enter a cycle of starting and being discarded without ever answering. IT DOES NOT
		// HAPPEN. Measured on 2026-09-21 on an iPhone 17 Pro simulator / iOS 26.5, from `creando
		// el documento` to `el sustrato contesta`:
		//
		//     run 1 .... 4.18 s        run 2 .... 3.84 s
		//
		// It fits inside the shell's 10 s, and with room. That the slimmed-down substrate starts
		// fifteen times faster than a hosted module ON THE SAME SIMULATOR is a measured fact; why,
		// was not measured, so it is not claimed.
		//
		// `Ceiling` IS NOT TOUCHED FROM HERE in any case: it is an invariant of the shell, the same on
		// both platforms, and fitting it to the test bench would be backwards. If some day a real
		// device comes close to 10 s, it is changed where the ceiling lives and with the number
		// beside it.
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

	// ---------------------------------------------------------------- the bridge, coming back

	internal void OnMessage(WKScriptMessage message)
	{
		if (message.Body is not NSString raw) return;

		try
		{
			using JsonDocument envelope = JsonDocument.Parse(raw.ToString());
			JsonElement root = envelope.RootElement;

			switch (Field(root, "op"))
			{
				case "lista": Ready(); return;
				case "contesta": Answered(Field(root, "id"), Field(root, "a")); return;

				// The substrate's console, which on iOS has no WebChromeClient to pick it up. It is
				// where the CSP violations and the runtime's errors are seen.
				case "consola": log($"sala-web: {Field(root, "a")}"); return;

				// THE PROBE, ANSWERED BY THE APP AND NOT BY A PERSON. It goes out before a line of
				// the substrate runs, so the answer arrives EVEN IF THE SUBSTRATE DOES NOT START --
				// which is exactly the case where it was going to be needed.
				case "sonda": log($"sala: P0 · {Field(root, "a")}"); return;

				default: log($"sala: verbo desconocido del puente: {Field(root, "op")}"); return;
			}
		}
		catch (JsonException e) { log($"sala: mensaje ilegible del puente: {e.Message}"); }
	}

	private static string Field(JsonElement o, string field) =>
		o.TryGetProperty(field, out JsonElement v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";

	// ---------------------------------------------------------------- where the bytes come from

	// THREE SOURCES FOR A SINGLE WEB ORIGIN. The substrate comes out of the .app; the module's
	// .dll files, out of its installed folder; and the journal, out of the person's folder. The
	// module does not tell them apart: to it, it is all its own origin, and it never opens a file
	// in its life.
	internal void OnFileRequested(IWKUrlSchemeTask task)
	{
		NSUrl? url = task.Request.Url;
		if (url is null) return;

		string path = (url.Path ?? "/").TrimStart('/');
		if (path.Length == 0 || path.EndsWith("/", StringComparison.Ordinal)) path += "index.html";

		try
		{
			Served served = Serve(path);

			var headers = new NSMutableDictionary
			{
				["Content-Type"] = new NSString(served.Type),
				["Cache-Control"] = new NSString("no-store"),
			};
			if (served.Csp is not null) headers["Content-Security-Policy"] = new NSString(served.Csp);

			task.DidReceiveResponse(new NSHttpUrlResponse(url, served.Status, "HTTP/1.1", headers));
			// A 404 carries no body, and `DidReceiveData` with zero bytes is not the same as not
			// calling it: WebKit accepts a task finishing with no data.
			if (served.Body.Length > 0) task.DidReceiveData(NSData.FromArray(served.Body));
			task.DidFinish();
		}
		catch (Exception e)
		{
			// A substrate that cannot be served CANNOT fail in silence: WebKit would show a blank
			// page, which is the symptom that already cost a whole run on Android.
			log($"sala: 500 {path} -- {e.GetType().Name}: {e.Message}");
			task.DidFailWithError(new NSError(new NSString("Gnothi"), 1));
		}
	}

	private sealed record Served(byte[] Body, string Type, int Status, string? Csp);

	private Served Serve(string path)
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
				// ONLY WHAT IS THE MODULE'S. Everything else is lent by the substrate, and that is
				// not an optimisation: a BCL package is compiled PER PLATFORM, and the one in the
				// module's folder is this phone's. Measured on Android: loading it inside the web
				// engine blows up with TypeLoadException over the ARM intrinsics.
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

		// everything else is the substrate, which travels in the .app
		return FromBundle(path);
	}

	// THE CSP OF THE SALA'S DOCUMENT, AND IT IS COMPUTED BY BundleServer, NOT BY THIS CLASS.
	// Android had a copy here and it broke: the HTML parser normalises CRLF to LF before hashing
	// the inline script, and the copy hashed the raw bytes; with the file in CRLF the engine
	// rejected the script Gnothi had just allowed and the document came out BLANK. There is ONE
	// copy, and in passing the sala inherits the same policy as the hosted form.
	private static Served FromBundle(string path)
	{
		string full = InTheBundle(path);
		if (!File.Exists(full)) return new Served([], "text/plain", 404, null);

		byte[] body = File.ReadAllBytes(full);

		// The document is read whole because its CSP depends on its content. The rest are served as
		// they are. There is no streaming here: `IWKUrlSchemeTask` hands over NSData, not a Stream.
		return path.EndsWith(".html", StringComparison.Ordinal)
			? new Served(body, MimeOf(path), 200, Gnothi.MobileShared.BundleServer.CspFor(body))
			: new Served(body, MimeOf(path), 200, null);
	}

	private static Served FromFile(string path, string type) =>
		File.Exists(path)
			? new Served(File.ReadAllBytes(path), type, 200, null)
			: new Served([], "text/plain", 404, null);

	private static Served TextBody(string body, string type) =>
		new(Encoding.UTF8.GetBytes(body), type, 200, null);

	private Served NotFound(string path)
	{
		log($"sala: 404 {path}");
		return new Served([], "text/plain", 404, null);
	}

	// THE LIST OF WHAT IS LENT, which is the other face of the installer's list of forbidden
	// things. What is here is put there by the substrate in its browser-wasm build; what is not,
	// travels in the module. If a module needed something that is not there, the answer is NOT to
	// load it from its folder: it is to add it to the substrate. Same list as AndroidRoom, and it
	// has to stay that way.
	private static bool LentByTheRoom(string assembly) =>
		assembly.StartsWith("System.", StringComparison.OrdinalIgnoreCase)
		|| assembly.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase)
		|| assembly is "Puppeteer" or "Choreography" or "Puppets.Portador" or "Gnothi.Contract"
		|| assembly.StartsWith("ModelContextProtocol", StringComparison.OrdinalIgnoreCase)
		|| assembly is "netstandard" or "mscorlib";

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

	// ---------------------------------------------------------------- what the page sees

	// THE BRIDGE, AND WHAT IT DOES NOT HAVE. Two verbs: «I am here» and «here is the answer».
	// There is no read, no write, no asking for anything -- the journal is put there by Gnothi
	// before asking, and whatever the module writes stays in memory. It is the same pair Android
	// injects with `AddJavascriptInterface(..., "SalaPuente")`.
	//
	// AND IT IS WHAT THE INSTITUTION'S DOMAIN CANNOT REACH: the module runs inside the substrate's
	// .NET runtime, with no access to `DotNet.invokeMethodAsync` nor to this object. The one who
	// uses it is the substrate, which is Gnothi's.
	private const string Shim = """
		(function () {
		  var send = function (m) { window.webkit.messageHandlers.SalaPuente.postMessage(JSON.stringify(m)); };

		  window.SalaPuente = {
		    lista:    function ()         { send({ op: 'lista',    a: '' }); },
		    contesta: function (id, json) { send({ op: 'contesta', id: id, a: json }); }
		  };

		  // THE PROBE, MEASURED BY THE APP. It runs before a single line of the substrate, so the
		  // answer arrives even if the substrate afterwards does not start -- which is the case
		  // that matters.
		  try {
		    send({ op: 'sonda', a:
		      'secureContext=' + !!window.isSecureContext +
		      ' crypto.subtle=' + !!(window.crypto && window.crypto.subtle) +
		      ' storage=' + !!navigator.storage +
		      ' wasm=' + (typeof WebAssembly !== 'undefined') +
		      ' wasm.streaming=' + (typeof WebAssembly !== 'undefined' && !!WebAssembly.instantiateStreaming) +
		      ' origin=' + window.location.origin });
		  } catch (x) { }

		  // AND THE ONE THAT REALLY DECIDES THE FORM, WHICH IS NOT THE SECURE CONTEXT: WHETHER
		  // `fetch` WORKS AGAINST A CUSTOM SCHEME.
		  //
		  // P0 does not answer it, although it looks as if it did. The hosted form NEVER fetches
		  // through its origin: its subresources are requested by the document and everything
		  // else -- read, write, pull -- goes through the bridge. The substrate cannot do that:
		  // Blazor downloads `_framework/*` with fetch, and `Sala.cs` asks for the module's .dll
		  // files and its journal with HttpClient, which in wasm IS fetch. If WebKit does not hand
		  // the custom scheme to fetch, the sala does not start even if the origin is secure, and
		  // then the local server on 127.0.0.1 is unavoidable.
		  //
		  // It goes against the document itself, which does exist securely, and arrives late on
		  // purpose: it is asynchronous, so it comes out on its own line of the log.
		  try {
		    fetch('index.html', { cache: 'no-store' })
		      .then(function (r) { send({ op: 'sonda', a: 'fetch(mismo origen)=' + r.status + ' ' + r.type }); })
		      .catch(function (e) { send({ op: 'sonda', a: 'fetch(mismo origen) FALLA: ' + e }); });
		  } catch (x) { send({ op: 'sonda', a: 'fetch(mismo origen) ni existe: ' + x }); }

		  ['log', 'warn', 'error'].forEach(function (level) {
		    var before = console[level].bind(console);
		    console[level] = function () {
		      try { send({ op: 'consola', a: level + ': ' + Array.prototype.join.call(arguments, ' ') }); }
		      catch (x) { }
		      before.apply(console, arguments);
		    };
		  });
		})();
		""";
}

// THE BRIDGE, on the native side. WebKit's protocols are ObjC protocols, so this has to descend
// from NSObject -- which is why it is a separate object and not the sala itself. Android has the
// same pair for the mirror reason.
internal sealed class RoomBridge(IosRoom room) : NSObject, IWKScriptMessageHandler
{
	public void DidReceiveScriptMessage(WKUserContentController controller, WKScriptMessage message) =>
		room.OnMessage(message);
}

// GNOTHI ANSWERING THE ENGINE for the substrate's bytes, the .dll files' and the journal's. It is
// the equivalent of `ShouldInterceptRequest` on Android, and the difference is that here the
// scheme is ours: WebKit hands `http` and `https` to nobody.
internal sealed class RoomServer(IosRoom room) : NSObject, IWKUrlSchemeHandler
{
	public void StartUrlSchemeTask(WKWebView web, IWKUrlSchemeTask task) => room.OnFileRequested(task);

	// There is nothing to cancel: every response is produced and finished inside
	// StartUrlSchemeTask. The method is not optional in the protocol, so it exists and says why it
	// is empty.
	public void StopUrlSchemeTask(WKWebView web, IWKUrlSchemeTask task) { }
}
