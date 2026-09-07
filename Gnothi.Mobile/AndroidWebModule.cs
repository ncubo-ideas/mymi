using Android.App;
using Android.Webkit;
using Gnothi.Movil;
using Java.Interop;
using System.Text;

namespace Gnothi.Mobile;

// THE ANDROID TWIN OF THE WEB MODULE HOST. Everything that is a decision lives in
// Gnothi.Movil; what is here is how a WebView is talked to, and nothing else.
//
// THIS FILE USED TO BE 500 LINES and most of them were not about Android. The CSP with its
// hash computation, the MIME map, the path guard, the journal, the cursor, the bridge protocol
// and its dispatch went to Gnothi.Movil when the iOS one had to be written -- not to save
// typing, but because every one of them IS the module's contract, and two copies of a contract
// is two contracts.
internal sealed class AndroidWebModuleHost(Activity owner, ISecretStore store, Action<string> log)
	: WebModuleHost<AndroidWebModule>
{
	protected override AndroidWebModule Create(string module, string bundle, string journal, string publisher) =>
		new AndroidWebModule(owner, module, bundle, journal, publisher, store, log);
}

// A WEB MODULE ON ANDROID: its WebView, its origin, its CSP and its bridge.
internal sealed class AndroidWebModule : WebModule
{
	// THE MODULE'S ORIGIN. A virtual host over https, one per module, and NOT a scheme of our
	// own: a custom scheme is not a secure context and there origin storage and the crypto
	// functions fall over (11 §3, corrected by the spike). It is intercepted BEFORE the
	// network, so there is no DNS and no TLS: the host does not exist outside the device.
	//
	// A different host per module is a different ORIGIN, which is the isolation §3 asks for,
	// and it is free: the web engine does it by same-origin.
	private static string HostOf(string module) => $"{module}.gnothi.local";

	private readonly Activity owner;

	private WebView? web;

	internal AndroidWebModule(
		Activity owner, string module, string bundle, string journal, string publisher,
		ISecretStore store, Action<string> log)
		: base(module, bundle, journal, publisher, store, log)
		=> this.owner = owner;

	// The module's view, for when Gnothi puts it on screen (11 §8). It is THE SAME WebView that
	// answers the questions: there are not two.
	internal Android.Views.View? View => web;

	protected override bool Alive => web is not null;

	protected override string OriginFor(string basePath) => $"https://{HostOf(Name)}{basePath}";

	protected override Task Create(BundleServer server, string url, CancellationToken ct)
	{
		var created = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

		// The WebView is created on the UI thread. There is no alternative: it is a view.
		owner.RunOnUiThread(() =>
		{
			try
			{
				var w = new WebView(owner);
				w.Settings.JavaScriptEnabled = true;
				w.Settings.DomStorageEnabled = true;
				w.SetWebViewClient(new BundleServerClient(server, HostOf(Name)));
				w.SetWebChromeClient(new ModuleConsole(Name));
				w.AddJavascriptInterface(new BridgeToModule(this), "gnothiPuente");

				// NOT added to any view: the module has no screen here. This shell answers
				// questions; the institution's UI is another story (11 §8).
				web = w;
				w.LoadUrl(url);

				created.TrySetResult();
			}
			catch (Exception ex) { created.TrySetException(ex); }
		});

		return created.Task.WaitAsync(ct);
	}

	protected override void RunJs(string js) =>
		owner.RunOnUiThread(() => web?.EvaluateJavascript(js, null));

	protected override void Teardown()
	{
		WebView? going = web;
		web = null;
		if (going is null) return;

		owner.RunOnUiThread(() => { going.StopLoading(); going.Destroy(); });
	}
}

// GNOTHI SERVING THE BUNDLE. What to answer is decided by the shared BundleServer; this only
// turns it into what the WebView expects.
internal sealed class BundleServerClient(BundleServer server, string host) : WebViewClient
{
	public override WebResourceResponse? ShouldInterceptRequest(WebView? view, IWebResourceRequest? request)
	{
		Android.Net.Uri? url = request?.Url;
		if (url is null || !string.Equals(url.Host, host, StringComparison.Ordinal)) return null;

		ServedFile served = server.Serve(url.Path ?? "/");

		var headers = new Dictionary<string, string>(served.Headers);
		string reason = served.Status == 200 ? "OK" : "Not Found";

		// The document arrives already read, because its CSP was computed from its bytes.
		// Everything else is streamed, which is what keeps a 20 MB wasm out of memory.
		Stream body = served.Body is not null
			? new MemoryStream(served.Body)
			: File.OpenRead(served.Path!);

		return new WebResourceResponse(served.Mime, "utf-8", served.Status, reason, headers, body);
	}
}

// The module's console goes to logcat. It is how CSP violations and the institution's domain
// errors become visible.
//
// THE MODULE IS NAMED IN THE MESSAGE AND NOT IN THE TAG, and that is not a style choice: a tag
// carrying a colon CANNOT BE FILTERED. `adb logcat -s` parses each term as `TAG:PRIORITY`, so
// `modulo:examenes:I` reads as tag `modulo` at priority `examenes` -- and an invalid term makes
// logcat reject the WHOLE filter list, not just that one. Measured: it printed its usage and
// not a single line came out, GNOTHI's included, so the filter looked like a silent app.
internal sealed class ModuleConsole(string module) : WebChromeClient
{
	public override bool OnConsoleMessage(ConsoleMessage? message)
	{
		if (message is not null)
			Android.Util.Log.Info("modulo", $"{module}: {message.Message()} ({message.LineNumber()})");
		return true;
	}
}

// THE BRIDGE, on the native side. Six verbs, and all six do the same thing: hand the call to
// the shared dispatch and return immediately.
//
// RETURNING IMMEDIATELY IS NOT OPTIONAL HERE: the methods of an object injected with
// @JavascriptInterface are SYNCHRONOUS, and the page freezes until they come back. A pull does
// network. That is why the module's whole contract answers by callback -- and why iOS, whose
// message handler cannot return a value at all, fits the same shape without changing a thing.
internal sealed class BridgeToModule(AndroidWebModule module) : Java.Lang.Object
{
	[JavascriptInterface]
	[Export("listo")]
	public void Ready() => module.Serve("listo", "", null, null);

	[JavascriptInterface]
	[Export("contesta")]
	public void Answer(string id, string? json, string? error) => module.Serve("contesta", id, json, error);

	[JavascriptInterface]
	[Export("listar")]
	public void List(string id) => module.Serve("listar", id, null, null);

	[JavascriptInterface]
	[Export("leer")]
	public void Read(string id, string path) => module.Serve("leer", id, path, null);

	[JavascriptInterface]
	[Export("escribir")]
	public void Write(string id, string path, string contentBase64) =>
		module.Serve("escribir", id, path, contentBase64);

	[JavascriptInterface]
	[Export("pull")]
	public void Pull(string id, string after) => module.Serve("pull", id, after, null);
}
