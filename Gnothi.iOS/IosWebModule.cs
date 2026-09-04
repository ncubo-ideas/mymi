using CoreFoundation;
using CoreGraphics;
using Foundation;
using Gnothi.Movil;
using System.Text.Json;
using UIKit;
using WebKit;

namespace Gnothi.iOS;

// THE iOS TWIN OF THE WEB MODULE HOST. Everything that is a decision is in Gnothi.Movil; what
// is here is how WebKit is talked to, and nothing else.
internal sealed class IosWebModuleHost(UIViewController owner, ISecretStore store, Action<string> log)
	: WebModuleHost<IosWebModule>
{
	protected override IosWebModule Create(string module, string bundle, string journal, string publisher) =>
		new IosWebModule(owner, module, bundle, journal, publisher, store, log);
}

// A WEB MODULE ON iOS: its WKWebView, its origin, its CSP and its bridge.
//
// FOUR THINGS DIFFER FROM ANDROID, AND ONLY FOUR. Everything else -- the CSP, the journal,
// the cursor, the bridge protocol, the dispatch -- is the same code in Gnothi.Movil:
//
//   1. THE ORIGIN. Android intercepts a virtual `https` host before the network. WebKit does
//      NOT let anyone handle http or https -- `SetUrlSchemeHandler` throws for reserved
//      schemes -- so here it has to be a scheme of our own.
//
//      [OPEN, AND IT DECIDES THE SHAPE] Whether that origin is a secure context is not
//      measured. If it is not, the module does not start: origin storage and `crypto.subtle`
//      fall over, and that is exactly what bit us on Android when the first version used
//      `gnothi-mod://` there (03 §2). It is one line to check and it is the first thing to run
//      on a device -- see PRUEBAS-WEBVIEW-IOS.md P0. If it fails, the fallback is a local
//      server on 127.0.0.1 and the ONLY thing that changes is `OriginFor` plus who answers.
//
//   2. THE BRIDGE IS ONE-WAY. `WKScriptMessageHandler` cannot return a value, so the module
//      talks through `postMessage`. That costs nothing, because the contract was ALREADY
//      callback-shaped: Android needed it too, for the opposite reason -- there the injected
//      methods are synchronous and would freeze the page.
//
//      What the module sees is identical because a `WKUserScript` defines `window.gnothiPuente`
//      with the same six verbs before any of its own code runs.
//
//   3. THERE IS NO WebChromeClient. The module's console is captured by patching `console` in
//      that same user script, and it comes out through the same channel.
//
//   4. THE PROTOCOL OBJECTS ARE SEPARATE, and that is not a style choice: WebKit's protocols
//      are ObjC protocols, so whoever implements them has to descend from `NSObject`, and this
//      class already descends from `WebModule`. Android landed on the same shape for the
//      mirror-image reason -- `@JavascriptInterface` needs a `Java.Lang.Object` -- so both
//      platforms end up with a small bridge object pointing back at the module.
internal sealed class IosWebModule : WebModule
{
	// The scheme is ours; the host is the module, so each module is a DIFFERENT ORIGIN and the
	// engine isolates them by same-origin for free -- the same property Android gets from one
	// virtual host per module.
	private const string Scheme = "gnothi-mod";

	// The one channel: both the bridge and the console come through here.
	private const string Channel = "gnothiPuente";

	private readonly UIViewController owner;

	private WKWebView? web;
	private BundleServer? server;

	internal IosWebModule(
		UIViewController owner, string module, string bundle, string journal, string publisher,
		ISecretStore store, Action<string> log)
		: base(module, bundle, journal, publisher, store, log)
		=> this.owner = owner;

	// The module's view, for when Gnothi puts it on screen (11 §8). It is THE SAME WKWebView
	// that answers the questions: there are not two.
	internal UIView? View => web;

	protected override bool Alive => web is not null;

	protected override string OriginFor(string basePath) => $"{Scheme}://{Name}{basePath}";

	protected override Task Create(BundleServer bundleServer, string url, CancellationToken ct)
	{
		server = bundleServer;

		var created = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

		// The WKWebView is created on the main thread. There is no alternative: it is a view.
		DispatchQueue.MainQueue.DispatchAsync(() =>
		{
			try
			{
				var configuration = new WKWebViewConfiguration();
				configuration.SetUrlSchemeHandler(new BundleSchemeHandler(this), Scheme);

				WKUserContentController content = configuration.UserContentController;
				content.AddScriptMessageHandler(new BridgeToModule(this), Channel);
				content.AddUserScript(new WKUserScript(
					new NSString(Shim), WKUserScriptInjectionTime.AtDocumentStart, isForMainFrameOnly: true));

				// NOT added to any view: the module has no screen here. This shell answers
				// questions; the institution's UI is another story (11 §8).
				web = new WKWebView(CGRect.Empty, configuration);
				web.LoadRequest(new NSUrlRequest(new NSUrl(url)));

				created.TrySetResult();
			}
			catch (Exception ex) { created.TrySetException(ex); }
		});

		return created.Task.WaitAsync(ct);
	}

	protected override void RunJs(string js) =>
		DispatchQueue.MainQueue.DispatchAsync(() => web?.EvaluateJavaScript(new NSString(js), null));

	protected override void Teardown()
	{
		WKWebView? going = web;
		web = null;
		if (going is null) return;

		DispatchQueue.MainQueue.DispatchAsync(() =>
		{
			going.StopLoading();
			going.Configuration.UserContentController.RemoveScriptMessageHandler(Channel);
			going.RemoveFromSuperview();
		});
	}

	// ---------------------------------------------------------------- the bridge, coming in

	internal void OnMessage(WKScriptMessage message)
	{
		if (message.Body is not NSString raw) return;

		try
		{
			using JsonDocument payload = JsonDocument.Parse(raw.ToString());
			JsonElement root = payload.RootElement;

			string op = Text(root, "op");

			// The console arrives through the same channel because iOS has no WebChromeClient.
			// It is how CSP violations and the institution's own errors become visible.
			if (op == "console") { Log($"modulo:{Name}: {Text(root, "a")}"); return; }

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

	// ---------------------------------------------------------------- serving the bundle

	internal void OnSchemeTask(IWKUrlSchemeTask task)
	{
		NSUrl? url = task.Request.Url;
		if (url is null || server is null) return;

		try
		{
			ServedFile served = server.Serve(url.Path ?? "/");

			byte[] body = served.Body ?? File.ReadAllBytes(served.Path!);

			var headers = new NSMutableDictionary();
			headers["Content-Type"] = new NSString(served.Mime);
			foreach (KeyValuePair<string, string> h in served.Headers)
				headers[h.Key] = new NSString(h.Value);

			task.DidReceiveResponse(new NSHttpUrlResponse(url, served.Status, "HTTP/1.1", headers));
			task.DidReceiveData(NSData.FromArray(body));
			task.DidFinish();
		}
		catch (Exception ex)
		{
			// A module that cannot be served must not fail silently: WebKit would just show a
			// blank page, which is the symptom that already cost a whole run on Android.
			Log($"{Name}: could not serve {url.Path}: {ex.GetType().Name}: {ex.Message}");
			task.DidFailWithError(new NSError(new NSString("Gnothi"), 1));
		}
	}

	// WHAT MAKES THE MODULE UNABLE TO TELL. It defines the same object with the same six verbs
	// Android injects, plus the console capture, before any of the bundle's own code runs.
	//
	// Every verb returns immediately: the answer arrives later at `window.gnothi.respuesta`,
	// which is exactly what the module already expects from Android.
	private const string Shim = """
		(function () {
		  var send = function (m) { window.webkit.messageHandlers.gnothiPuente.postMessage(JSON.stringify(m)); };

		  window.gnothiPuente = {
		    listo:     function ()             { send({ op: 'listo',    id: '' }); },
		    contesta:  function (id, j, e)     { send({ op: 'contesta', id: id, a: j, b: e }); },
		    listar:    function (id)           { send({ op: 'listar',   id: id }); },
		    leer:      function (id, ruta)     { send({ op: 'leer',     id: id, a: ruta }); },
		    escribir:  function (id, ruta, c)  { send({ op: 'escribir', id: id, a: ruta, b: c }); },
		    pull:      function (id, desde)    { send({ op: 'pull',     id: id, a: String(desde) }); }
		  };

		  ['log', 'warn', 'error'].forEach(function (nivel) {
		    var antes = console[nivel].bind(console);
		    console[nivel] = function () {
		      try { send({ op: 'console', id: '', a: nivel + ': ' + Array.prototype.join.call(arguments, ' ') }); }
		      catch (x) { }
		      antes.apply(console, arguments);
		    };
		  });
		})();
		""";
}

// THE BRIDGE, on the native side. WebKit's protocols are ObjC protocols, so this has to
// descend from NSObject -- which is why it is a separate object and not the module itself.
// Android has the same pair for the mirror-image reason.
internal sealed class BridgeToModule(IosWebModule module) : NSObject, IWKScriptMessageHandler
{
	public void DidReceiveScriptMessage(WKUserContentController controller, WKScriptMessage message) =>
		module.OnMessage(message);
}

// GNOTHI ANSWERING THE ENGINE'S REQUESTS for the bundle. What to answer is decided by the
// shared BundleServer; this only turns it into what WebKit expects.
internal sealed class BundleSchemeHandler(IosWebModule module) : NSObject, IWKUrlSchemeHandler
{
	public void StartUrlSchemeTask(WKWebView webView, IWKUrlSchemeTask task) => module.OnSchemeTask(task);

	// Nothing to cancel: every response is produced and finished inside StartUrlSchemeTask.
	// The method is not optional in the protocol, so it exists and says why it is empty.
	public void StopUrlSchemeTask(WKWebView webView, IWKUrlSchemeTask task) { }
}
