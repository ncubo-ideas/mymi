#if WINDOWS
using System.Text.Json;
using Gnothi.MobileShared;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace Gnothi.Desktop;

// THE FACE IN ITS OWN WINDOW, over WebView2 -- Windows only. It was the first shape that ran on
// the desktop (2026-09-14) and it stays as the variant with the phone's full guarantees: the CSP
// travels as a real header, the module's origin is a secure https context, and a refusal is
// counted. The default face is now the frame inside Gnothi's window (FrameWebModule.cs), which
// is what the person asked for and runs on the three systems; this one is reached with
// `--cara ventana` and is the yardstick to compare against.
//
// Everything that is a decision -the bridge verbs, the journal guard, the courier, the cursor,
// `consultar`- is WebModule, shared with Android and iOS byte for byte. This file is the WebView2
// counterpart of AndroidWebModule.cs and IosWebModule.cs, shaped after the iOS one: the engine
// cannot return a value from an injected method, so the bridge is one string message in
// (`chrome.webview.postMessage`) and one line of JavaScript out (`ExecuteScriptAsync`).
//
// THE FACES LIVE ON ONE DEDICATED STA THREAD with a WinForms message loop, apart from Photino's
// loop on the app's thread. One hidden form is the post box everything is marshalled through;
// each module gets its own visible form.
internal static class FaceThread
{
	private static Form? marshal;
	private static readonly ManualResetEventSlim ready = new();

	internal static Form Marshal
	{
		get
		{
			if (marshal is not null) return marshal;
			lock (ready)
			{
				if (marshal is not null) return marshal;
				var thread = new Thread(() =>
				{
					Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
					var box = new Form { ShowInTaskbar = false, Opacity = 0, FormBorderStyle = FormBorderStyle.None, Size = new Size(1, 1) };
					box.Load += (_, _) => { box.Hide(); marshal = box; ready.Set(); };
					Application.Run(box);
				})
				{ IsBackground = true, Name = "caras" };
				thread.SetApartmentState(ApartmentState.STA);
				thread.Start();
				ready.Wait();
				return marshal!;
			}
		}
	}

	internal static void Post(Action work) => Marshal.BeginInvoke(work);
}

internal sealed class WebView2WebModuleHost(ISecretStore store, Action<string> log)
	: WebModuleHost<WebView2WebModule>, IFaces
{
	protected override WebView2WebModule Create(string module, string bundle, string journal, string publisher)
	{
		var face = new WebView2WebModule(module, bundle, journal, publisher, store, log);
		// CLOSING THE WINDOW IS CLOSING THE FACE. On the phone the WebView survives "back" and
		// reopening re-shows it; on the desktop a closed Form is gone, so the module leaves the
		// live list too, and the next "Abrir la pantalla" builds it again. Without this the host
		// kept handing back a face whose window no longer existed (measured 2026-09-14).
		face.OnClosed = name => Evict(name);
		return face;
	}

	public async Task<IFace> OpenFace(Gnothi.Host.Module m, CancellationToken ct)
	{
		WebView2WebModule face = await Open(m, ct);
		face.Title = m.Manifest.Identity;
		face.Front();
		return face;
	}

	public void Close(string module)
	{
		// The evict-on-demand rule spares a face the person is looking at; before REPLACING the
		// module it has to go regardless, so it is marked not visible first.
		if (IsLive(module)) Evict(module);
	}

	public string PinToDesktop(Gnothi.Host.Module m, string modulesRoot, string journalsRoot) =>
		DesktopShortcut.Pin(m, modulesRoot, journalsRoot, log);
}

internal sealed class WebView2WebModule : WebModule, IFace
{
	// One virtual https host per module, as on Android: an origin per module is the isolation,
	// and https makes it a secure context. WebView2 answers it from our files, before the
	// network, through WebResourceRequested -- the ShouldInterceptRequest of this engine.
	private static string HostOf(string module) => $"{module}.gnothi.local";

	private Form? window;
	private WebView2? web;
	private CoreWebView2Environment? env;
	private BundleServer? server;

	internal WebView2WebModule(
		string module, string bundle, string journal, string publisher, ISecretStore store, Action<string> log)
		: base(module, bundle, journal, publisher, store, log) { }

	public string Title
	{
		set { string t = value; FaceThread.Post(() => { if (window is not null) window.Text = t; }); }
	}

	// Told by the host, called when the person closes the window.
	internal Action<string>? OnClosed;

	// Bring an open face to the front: asking to open what is already open should show it, not
	// do nothing.
	internal void Front() => FaceThread.Post(() =>
	{
		if (window is null) return;
		if (window.WindowState == FormWindowState.Minimized) window.WindowState = FormWindowState.Normal;
		window.Activate();
	});

	int IFace.Violations => Violations;

	protected override bool Alive => web is not null;

	protected override string OriginFor(string basePath) => $"https://{HostOf(Name)}{basePath}";

	protected override Task Create(BundleServer bundleServer, string url, CancellationToken ct)
	{
		server = bundleServer;
		var created = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

		FaceThread.Post(async () =>
		{
			try
			{
				var form = new Form { Text = Name, Width = 520, Height = 940, StartPosition = FormStartPosition.CenterScreen };
				var w = new WebView2 { Dock = DockStyle.Fill };
				form.Controls.Add(w);
				form.FormClosed += (_, _) =>
				{
					web = null;
					window = null;
					Visible = false;
					Log($"{Name}: la cara se cerró");
					OnClosed?.Invoke(Name);
				};
				form.Show();
				Visible = true;

				string userData = Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Gnothi", "WebView2", Name);
				Directory.CreateDirectory(userData);
				env = await CoreWebView2Environment.CreateAsync(userDataFolder: userData);
				await w.EnsureCoreWebView2Async(env);

				CoreWebView2 core = w.CoreWebView2;
#if DEBUG
				core.Settings.AreDevToolsEnabled = true;
#else
				core.Settings.AreDevToolsEnabled = false;
#endif
				// Serve the bundle: every request to the module's own host goes to BundleServer,
				// which decides file, type and policy. The CSP travels as a real header, exactly
				// as on Android.
				core.AddWebResourceRequestedFilter($"https://{HostOf(Name)}/*", CoreWebView2WebResourceContext.All);
				core.WebResourceRequested += OnResource;

				// The module's side of the bridge: one string in.
				core.WebMessageReceived += OnMessage;

				// The half no CSP covers: a top-level navigation out of its own origin.
				core.NavigationStarting += (_, e) =>
				{
					var going = new Uri(e.Uri);
					if (going.Scheme == "about") return;
					if (string.Equals(going.Host, HostOf(Name), StringComparison.OrdinalIgnoreCase)) return;
					e.Cancel = true;
					Log($"{Name}: navigation refused to {going.Scheme}://{going.Host} -- outside its own origin");
				};

				await core.AddScriptToExecuteOnDocumentCreatedAsync(Shim);

				window = form;
				web = w;
				core.Navigate(url);
				created.TrySetResult();
			}
			catch (Exception ex) { created.TrySetException(ex); }
		});

		return created.Task.WaitAsync(ct);
	}

	protected override void RunJs(string js) =>
		FaceThread.Post(() => { _ = web?.CoreWebView2.ExecuteScriptAsync(js); });

	protected override void Teardown()
	{
		Form? going = window;
		web = null;
		window = null;
		if (going is null) return;
		FaceThread.Post(() => going.Close());
	}

	// ---------------------------------------------------------------- what a test needs to SEE

	public Task<string> Eval(string js)
	{
		var done = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
		FaceThread.Post(async () =>
		{
			try
			{
				if (web is null) { done.TrySetResult("null"); return; }
				done.TrySetResult(await web.CoreWebView2.ExecuteScriptAsync(js));
			}
			catch (Exception ex) { done.TrySetException(ex); }
		});
		return done.Task;
	}

	public Task Screenshot(string path)
	{
		var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		FaceThread.Post(async () =>
		{
			try
			{
				if (web is null) { done.TrySetResult(); return; }
				using FileStream file = File.Create(path);
				await web.CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, file);
				done.TrySetResult();
			}
			catch (Exception ex) { done.TrySetException(ex); }
		});
		return done.Task;
	}

	// ---------------------------------------------------------------- the two adapters

	private void OnResource(object? sender, CoreWebView2WebResourceRequestedEventArgs e)
	{
		if (server is null || env is null) return;
		var uri = new Uri(e.Request.Uri);
		if (!string.Equals(uri.Host, HostOf(Name), StringComparison.OrdinalIgnoreCase)) return;

		try
		{
			ServedFile served = server.Serve(uri.AbsolutePath);
			byte[] body = served.Body ?? File.ReadAllBytes(served.Path!);
			string headers = $"Content-Type: {served.Mime}"
				+ string.Concat(served.Headers.Select(h => $"\r\n{h.Key}: {h.Value}"));

			e.Response = env.CreateWebResourceResponse(
				new MemoryStream(body), served.Status, served.Status == 200 ? "OK" : "Not Found", headers);
		}
		catch (Exception ex)
		{
			Log($"{Name}: could not serve {uri.AbsolutePath}: {ex.GetType().Name}: {ex.Message}");
		}
	}

	private void OnMessage(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
	{
		string raw;
		try { raw = e.TryGetWebMessageAsString(); }
		catch (ArgumentException) { return; }

		try
		{
			using JsonDocument payload = JsonDocument.Parse(raw);
			JsonElement root = payload.RootElement;
			string op = Text(root, "op");
			if (op == "console") { Log($"modulo:{Name}: {Text(root, "a")}"); return; }
			if (op == "probe") { Log($"{Name}: P0 · {Text(root, "a")}"); return; }
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

	// THE SHIM: the same seven verbs the iOS shim publishes, over WebView2's message channel.
	// `gnothiPuente` is the institution's API and is not translated.
	private const string Shim = """
		(function () {
		  var send = function (m) { window.chrome.webview.postMessage(JSON.stringify(m)); };

		  window.gnothiPuente = {
		    listo:     function ()             { send({ op: 'listo',    id: '' }); },
		    contesta:  function (id, j, e)     { send({ op: 'contesta', id: id, a: j, b: e }); },
		    listar:    function (id)           { send({ op: 'listar',   id: id }); },
		    leer:      function (id, path)     { send({ op: 'leer',     id: id, a: path }); },
		    escribir:  function (id, path, c)  { send({ op: 'escribir', id: id, a: path, b: c }); },
		    pull:      function (id, from)     { send({ op: 'pull',     id: id, a: String(from) }); },
		    consultar: function (id, tool, a)  { send({ op: 'consultar', id: id, a: tool, b: a || '{}' }); },
		    violation: function (r)            { send({ op: 'violation', id: '', a: r }); }
		  };

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
#endif
