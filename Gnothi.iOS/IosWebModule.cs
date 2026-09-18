using CoreFoundation;
using CoreGraphics;
using Foundation;
using Gnothi.MobileShared;
using System.Text.Json;
using UIKit;
using WebKit;

namespace Gnothi.iOS;

// THE iOS TWIN OF THE WEB MODULE HOST. Everything that is a decision is in Gnothi.MobileShared; what
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
// the cursor, the bridge protocol, the dispatch -- is the same code in Gnothi.MobileShared:
//
//   1. THE ORIGIN. Android intercepts a virtual `https` host before the network. WebKit does
//      NOT let anyone handle http or https -- `SetUrlSchemeHandler` throws for reserved
//      schemes -- so here it has to be a scheme of our own.
//
//      [ANSWERED 2026-09-07, AND THE SHAPE IT DECIDED IS THIS ONE] This comment used to say
//      the secure context was not measured, and it stayed that way after the measurement --
//      which is how somebody planning the sala read it as still open. It is not: the probe
//      below answered it in the four modules, on an iPhone 17 Pro simulator / iOS 26.5:
//
//          secureContext=true crypto.subtle=true storage=true origin=gnothi-mod://<module>
//
//      See HALLAZGOS-CORRIENDO-EL-GUION.md §1 and PRUEBAS-WEBVIEW-IOS.md P0. So `OriginFor`
//      stays as it is and the 127.0.0.1 fallback is NOT needed: no port, no lifecycle, and the
//      interception stays «before the network». Had it been false, the module would not have
//      started at all -- origin storage and `crypto.subtle` fall over -- which is exactly what
//      bit Android when its first version used `gnothi-mod://` there (03 §2).
//
//      WHAT IT DOES NOT ANSWER, AND THE SALA NEEDS IT. Android's sala serves itself over
//      `https://sala.gnothi.local` and not a scheme of its own, for the Android version of
//      this same reason. On iOS that reason is gone, so the sala CAN use a scheme -- but a
//      secure origin is the floor, not the question: its substrate is a Blazor wasm document
//      booting a .NET runtime, and whether THAT boots under a scheme handler is measured by
//      booting it, not by reading this line.
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
				web = new WKWebView(CGRect.Empty, configuration)
				{
					// THE SECOND LAYER OF CONTAINMENT, and the one Android is still missing.
					// The CSP stops the module from FETCHING another origin and, since
					// `form-action`, from POSTING to one. Neither stops it from NAVIGATING the
					// whole view somewhere else -- `navigate-to` was never standardised -- so
					// this hook is the only place to refuse it. Android does the same in
					// `BundleServerClient.ShouldOverrideUrlLoading`.
					NavigationDelegate = new OnlyItsOwnOrigin(this),
				};

#if DEBUG
				// WITHOUT THIS, SAFARI'S INSPECTOR DOES NOT SEE THIS WEBVIEW AT ALL. Since iOS
				// 16.4 a WKWebView is inspectable only if it says so, and the default is false:
				// Develop > Simulator simply lists nothing, with no hint as to why.
				//
				// It matters because PRUEBAS-WEBVIEW-IOS.md sends the person to that inspector
				// three times, P5's navigation check included -- and that check is what measures
				// the containment declared right above. A test that cannot be run is not a test.
				//
				// DEBUG ONLY, and not out of caution: an inspectable WebView lets anyone with a
				// cable read the page's memory, and the module holds the person's journal there
				// by design. Shipping it inspectable would undo what the CSP is for.
				if (OperatingSystem.IsIOSVersionAtLeast(16, 4)) web.Inspectable = true;
#endif

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

			// P0, ANSWERED BY THE APP AND NOT BY A PERSON. Whether this origin is a secure
			// context decides the shape of the whole design, and it used to be three lines
			// somebody had to type into Safari's inspector -- which means the answer lived in
			// the memory of whoever ran it. Now it lands in the log next to everything else.
			//
			// If any of the three is false the module will not start: origin storage and
			// crypto.subtle fall over. That is exactly what bit Android when it used a custom
			// scheme, and it is why the fallback is a local server on 127.0.0.1.
			if (op == "probe") { Log($"{Name}: P0 · {Text(root, "a")}"); return; }

			Serve(op, Text(root, "id"), Optional(root, "a"), Optional(root, "b"));
		}
		catch (JsonException ex)
		{
			Log($"{Name}: unreadable bridge message: {ex.Message}");
		}
	}

	// WHETHER THE MODULE MAY NAVIGATE THERE, which is the half of containment the CSP does not
	// cover. `connect-src 'self'` stops a fetch; it says nothing about the view being sent
	// somewhere else, and a module that can navigate the top frame can show whatever it likes
	// inside Gnothi's own window.
	//
	// The rule is the origin, not a list: same scheme, same host as what this module was
	// served under. Everything else -- another module's origin included -- is refused.
	//
	// A REFUSAL IS NEVER MUTE. On Android the equivalent gap was found by reading the code with
	// the question already in mind, which only works if somebody thinks to ask. A logged line
	// means the next person sees it happen instead.
	internal bool MayNavigateTo(NSUrl? url)
	{
		if (url is null) return false;

		bool mine = string.Equals(url.Scheme, Scheme, StringComparison.OrdinalIgnoreCase)
			&& string.Equals(url.Host, Name, StringComparison.OrdinalIgnoreCase);

		if (!mine) Log($"{Name}: navigation refused to {url.Scheme}://{url.Host} -- outside its own origin");

		return mine;
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
		    leer:      function (id, path)     { send({ op: 'leer',     id: id, a: path }); },
		    escribir:  function (id, path, c)  { send({ op: 'escribir', id: id, a: path, b: c }); },
		    pull:      function (id, from)     { send({ op: 'pull',     id: id, a: String(from) }); },

		    // The seventh of the contract, and the only one that runs the arrow backwards: a
		    // HYBRID face asks, and the answer comes from the module's surface in process. No
		    // module parameter -- a face can only ask about the module it belongs to.
		    consultar: function (id, tool, a)  { send({ op: 'consultar', id: id, a: tool, b: a || '{}' }); },

		    // The seventh, and the only one no bundle calls: the watchdog Gnothi injects into
		    // the document reports a refusal back through the same door. See WebModule.Serve.
		    violation: function (r)            { send({ op: 'violation', id: '', a: r }); }
		  };

		  // P0, MEASURED BY THE APP. It runs before anything from the bundle, so the answer
		  // arrives even if the module then fails to start -- which is exactly the case that
		  // matters.
		  try {
		    send({ op: 'probe', id: '', a:
		      'secureContext=' + !!window.isSecureContext +
		      ' crypto.subtle=' + !!(window.crypto && window.crypto.subtle) +
		      ' storage=' + !!navigator.storage +
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

// THE SECOND LAYER OF CONTAINMENT. WebKit asks before every navigation, which is the hook
// Android never wired -- there the equivalent is `ShouldOverrideUrlLoading` and it is still a
// gap. Arriving later has one advantage: the gap was already written down, so it goes in from
// the start instead of being found by reading the code with the question in mind.
//
// It descends from NSObject for the same reason as the other two: WebKit's delegates are ObjC
// protocols, and IosWebModule already descends from WebModule.
internal sealed class OnlyItsOwnOrigin(IosWebModule module) : WKNavigationDelegate
{
	public override void DecidePolicy(
		WKWebView webView, WKNavigationAction action, Action<WKNavigationActionPolicy> decide) =>
		decide(module.MayNavigateTo(action.Request.Url)
			? WKNavigationActionPolicy.Allow
			: WKNavigationActionPolicy.Cancel);
}
