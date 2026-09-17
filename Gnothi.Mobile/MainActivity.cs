using Android.App;
using Android.OS;
using Android.Webkit;
using Gnothi.MobileShared;
using System.Text.Json;

// WHAT GNOTHI MAY SPEAK IN THE CLEAR: the loopback only, which is where a development tunnel lands.
// Everything else demands HTTPS, which is what Android imposes from API 28 and what the postman
// complies with. See Resources/xml/network_security_config.xml.
[assembly: Android.App.Application(NetworkSecurityConfig = "@xml/network_security_config")]

// THE CAMERA, for reading the institution's code from inside Gnothi. The page asks for it
// through getUserMedia, and the WebView will not even ask the person unless the app declares
// the permission. Not required: a phone without a camera pastes the link instead.
// THE HOST'S NETWORK, DECLARED BY HAND, and it has to be declared even though "it already worked".
//
// The .NET Android SDK adds INTERNET ONLY IN DEBUG, so the debugger can connect. In Release it does
// not, and then the postman -- the only thing that talks to publishers -- is left with no network in
// the build that actually ships.
//
// HOW IT LOOKS WHEN IT IS MISSING, measured on 2026-09-11 on a Xiaomi with Android 16: the request to
// 127.0.0.1 dies with `HttpRequestException: Permission denied (127.0.0.1:8095)`. Loopback and all,
// Android demands INTERNET to open ANY socket. And the message reads like a manufacturer restriction
// -- MIUI is famous for cutting apps off the network -- so people go looking for runtime permissions
// that do not exist: INTERNET is a normal permission and never shows up on the app's permissions
// screen. The only place it can be seen is `dumpsys package`.
[assembly: Android.App.UsesPermission(Android.Manifest.Permission.Internet)]
[assembly: Android.App.UsesPermission(Android.Manifest.Permission.Camera)]
[assembly: Android.App.UsesFeature("android.hardware.camera", Required = false)]

namespace Gnothi.Mobile;

// GNOTHI'S HOST ON A PHONE, with a face. THE ANDROID HALF AND NOTHING ELSE.
//
// The face is ONE html in Assets, rendered by a WebView. Why not Blazor/MAUI, which was the natural
// suggestion: real Blazor on Android needs the MAUI workload, which installs into Program Files
// -- administrator on this machine -- and rewrites the project. The WebView gives what was asked for
// (web-quality UI, and the model's markdown with real tables, which a native TextView cannot) while
// the Host's code stays untouched.
//
// And that decision paid for itself the day iOS arrived: because the face is an HTML, it is THE SAME
// FILE over there. The only thing that changes between platforms is who mounts the WebView and how
// window.prompt() is translated. Everything else -- the state, the consent, the key, the provider,
// the question -- lives in Gnothi.MobileShared/Shell.cs and is shared as it is.
//
// WHAT THIS FILE OWNS: the screen and the lifecycle (installing what was downloaded). The
// catalog, the consent and the dispatch are the Host's -- the same files the desktop runs.
// ENROLMENT FROM THE INSTITUTION'S PAGE. In production the domain also serves an assetlinks.json
// with the signing fingerprint, and with that Android opens Gnothi without a disambiguation dialogue
// (specification 13 §7).
//
// AutoVerify IS NOT OPTIONAL, and not only for production. Without it Android does not even register
// the domain as a candidate: `pm get-app-links` returns NOTHING, and Chrome -- which only hands an
// https to an app when that app is a verified handler -- navigates it itself and shows
// DNS_PROBE_FINISHED_NXDOMAIN. The institution's button does nothing. Measured on 2026-09-04.
//
// In a local test the domain cannot really be verified (it would take serving assetlinks.json from
// go.gnothi.app with the signing fingerprint), but WITH AutoVerify it can be approved by hand, which
// is what escenario-webview.ps1 does:
//
//     adb shell pm set-app-links-user-selection --user 0 --package <paquete> true go.gnothi.app
[IntentFilter(new[] { Android.Content.Intent.ActionView },
	Categories = new[] { Android.Content.Intent.CategoryDefault, Android.Content.Intent.CategoryBrowsable },
	DataScheme = "https", DataHost = "go.gnothi.app", DataPathPrefix = "/alta",
	AutoVerify = true)]
// THE LINK BACK from the standalone install: it carries Sonia's assertions (13 §6.1).
[IntentFilter(new[] { Android.Content.Intent.ActionView },
	Categories = new[] { Android.Content.Intent.CategoryDefault, Android.Content.Intent.CategoryBrowsable },
	DataScheme = "https", DataHost = "go.gnothi.app", DataPathPrefix = "/abrir",
	AutoVerify = true)]
// THE SHARE SHEET, for when the assertions do not fit in a link. text/plain and not
// application/json because Web Share rejects JSON: its list of allowed types is closed.
[IntentFilter(new[] { Android.Content.Intent.ActionSend },
	Categories = new[] { Android.Content.Intent.CategoryDefault },
	DataMimeType = "text/plain")]
// A MODULE PACKAGE ARRIVING AS A FILE: "open with" from Files or Downloads, or the share sheet
// from another app. Two mimes, on purpose: the institution's page serves the .gmod as
// application/vnd.gnothi.module, but a file that travelled through chat or mail usually arrives
// as plain application/zip -- the inspection tells a module from any other zip, so Gnothi offers
// itself for both. The mime is what is matched, not the extension: for content:// files the path
// is opaque. A file that shows up as anything else is reached from inside Gnothi, with the picker.
[IntentFilter(new[] { Android.Content.Intent.ActionView },
	Categories = new[] { Android.Content.Intent.CategoryDefault },
	DataSchemes = new[] { "content", "file" },
	DataMimeTypes = new[] { "application/vnd.gnothi.module", "application/zip" })]
[IntentFilter(new[] { Android.Content.Intent.ActionSend },
	Categories = new[] { Android.Content.Intent.CategoryDefault },
	DataMimeTypes = new[] { "application/vnd.gnothi.module", "application/zip" })]
[Activity(Label = "Gnothi", MainLauncher = true, Icon = "@mipmap/ic_launcher", Theme = "@android:style/Theme.Material.Light.NoActionBar")]
public partial class MainActivity : Activity
{
	private WebView? web;

	// The screen's root, so the module can be put on top of it; the host, to ask it for one; and which
	// one is in view, which is what the back button has to close first.
	private Android.Widget.FrameLayout? raiz;
	private AndroidWebModuleHost? anfitrion;
	private AndroidWebModule? enPantalla;
	private Shell? shell;
	private readonly CancellationTokenSource stop = new();

	protected override void OnCreate(Bundle? savedInstanceState)
	{
		base.OnCreate(savedInstanceState);

		// If Gnothi was opened BY an enrolment link, the WHOLE link goes to the shell -- with its seal,
		// if it carries one -- and the shell takes it apart. It used to travel as just ?bundle=, and the
		// seal was lost.
		altaPendiente = Intent?.Data?.GetQueryParameter("bundle") is { Length: > 0 } ? Intent!.Data!.ToString() : null;
		// Or by a file -opened, shared- which is read now and enrolled once the Host is up. A
		// share of text is the other thing a share can be: the handoff from the institution's
		// standalone app, read below.
		archivoPendiente = LeerArchivoDeModulo(Intent, strict: true);
		if (archivoPendiente is null) LeerTraspasoDe(Intent);
		// And the home-screen icon, which asks for a module to be opened by name.
		LeerAperturaDe(Intent);

		web = new WebView(this);
		web.Settings.JavaScriptEnabled = true;
		// The camera preview is a <video> the page plays as soon as the stream arrives; without
		// this the WebView would wait for a gesture that already happened, on the button.
		web.Settings.MediaPlaybackRequiresUserGesture = false;
		// The page detects the app by this mark and NEVER calls prompt() outside it: in an ordinary
		// browser that dialogue is modal and freezes the page -- measured in the preview pane.
		web.Settings.UserAgentString += " GnothiHost";
		web.SetBackgroundColor(Android.Graphics.Color.ParseColor("#F5F3FB"));
		web.SetWebChromeClient(new Bridge(this));

		// targetSdk 36 makes edge-to-edge MANDATORY: the page would extend under the status bar and the
		// gesture bar, and the bottom navigation ends up beneath the system's. The fix is native and in
		// one place -- padding for the system bars -- BUT the padding goes on a CONTAINER, not on the
		// WebView: a WebView ignores its own padding, which cost a screenshot to discover.
		var frame = new Android.Widget.FrameLayout(this);
		frame.SetBackgroundColor(Android.Graphics.Color.ParseColor("#F5F3FB"));
		frame.AddView(web);
		frame.SetOnApplyWindowInsetsListener(new Insets());

		raiz = frame;
		SetContentView(frame);

		// DARK ICONS ON THE SYSTEM BARS. The page is light lilac and edge-to-edge puts the status
		// bar over it: with the default (light) icons the clock and the battery came out white on
		// white -- seen on the Honor, 2026-09-09. Android 11+ has the controller; older ones, the flags.
		if (OperatingSystem.IsAndroidVersionAtLeast(30) && Window?.InsetsController is { } bars)
		{
			var light = Android.Views.WindowInsetsControllerAppearance.LightStatusBars
			          | Android.Views.WindowInsetsControllerAppearance.LightNavigationBars;
			bars.SetSystemBarsAppearance((int)light, (int)light);
		}
		else if (Window?.DecorView is { } decor)
		{
#pragma warning disable CA1422 // the pre-30 path, by design
			decor.SystemUiFlags |= Android.Views.SystemUiFlags.LightStatusBar | Android.Views.SystemUiFlags.LightNavigationBar;
#pragma warning restore CA1422
		}

		web.LoadUrl("file:///android_asset/ui.html");

		PrepareTheBox();

		// THE TWO FOLDERS ARE CHOSEN BY THE PLATFORM, not by the shell. adb cannot write into the private
		// area, and that is right: nobody from outside installs a module.
		shell = new Shell(
			new Shell.Paths(
				Files: FilesDir!.AbsolutePath,
				Incoming: System.IO.Path.Combine(GetExternalFilesDir(null)!.AbsolutePath, "incoming")),
			Log,
			(kind, id, payload) => RunOnUiThread(() =>
				web?.EvaluateJavascript(Shell.PushScript(kind, id, payload), null)));

		_ = Task.Run(Start);
	}


	// THE START-UP. What was left here after master's refactor moved the core into
	// Gnothi.MobileShared/Shell.cs: installing, discovery, the dispatcher, the consent watcher and the
	// check on new modules all live over there. Here, only what knows about Android, and what has to
	// happen AFTER the Host comes up.
	private async Task Start()
	{
		if (shell is null) return;

		// WHO KNOWS HOW TO CREATE WEBVIEWS. ChooseShell is static and has no Context, so the webview
		// shell cannot build the host: the Activity registers it, because the Activity does have one.
		// Without this, a module with "type": "webview" fails saying exactly that.
		anfitrion = new AndroidWebModuleHost(
			this, new AndroidSecretStore(FilesDir!.AbsolutePath),
			s => global::Android.Util.Log.Info("gnothi", s));
		Gnothi.Host.WebViewShell.Host = anfitrion;

		// AND WHO KNOWS HOW TO CLOSE THEM: an update moves the module's folder while its WebView may be
		// on top of it (44 §3.6). The Shell only names the hook; here it is honoured.
		shell.CloseModule = modulo =>
		{
			if (!anfitrion.IsLive(modulo)) return false;
			anfitrion.Evict(modulo);
			return true;
		};

		// Run() is the Host's loop and never ends: it is started ONCE and awaited at the end.
		Task corriendo = shell.Run();

		// If the app was opened BY a link, only now is there somewhere to install into or somebody to hand
		// it to. It is the same shape ViewController uses on iOS.
		if (altaPendiente is { Length: > 0 } pendiente)
		{
			altaPendiente = null;
			await shell.Enroll(pendiente, stop.Token);
		}

		if (archivoPendiente is { } archivo)
		{
			archivoPendiente = null;
			await shell.EnrollFile(archivo.Bytes, archivo.Name, stop.Token);
		}

		if (archivosPendientes is { } archivos)
		{
			archivosPendientes = null;
			await shell.EnrollFiles(archivos, stop.Token);
		}

		if (traspasoPendiente is { } t)
		{
			traspasoPendiente = null;
			await EntregarTraspaso(t.Modulo, t.Aserciones, stop.Token);
		}

		// THE ICON GOES LAST, and it is not a detail of ordering: if an enrolment or a handoff also
		// arrived in the same start-up, whatever came in has to be installed and recorded first --
		// opening the face before that would show the person the old file.
		if (aperturaPendiente is { } cual)
		{
			aperturaPendiente = null;
			AbrirModulo(cual);
		}

		await corriendo;
	}

	protected override void OnDestroy()
	{
		shell?.Dispose();
		base.OnDestroy();
	}

	// THE BACK BUTTON asks the page first: a sheet closes, a detail goes back to the home screen. Only
	// when the page answers false -- it is already at home -- does the app step aside, to the
	// BACKGROUND and not to death: finishing would throw away the loaded catalogue for no reason.
	//
	// OnBackPressed is the deprecated path, and it is chosen on purpose: the modern
	// OnBackInvokedCallback only takes over when the manifest opts in, which this app does not.
	// BACK: first the module on screen, then Gnothi's page. The order matters: if the module is
	// covering everything, the button has to take IT away, not navigate underneath it.
	// The enrolment link with the app ALREADY open.
	protected override void OnNewIntent(Android.Content.Intent? intento)
	{
		base.OnNewIntent(intento);

		// THE NEW INTENT IS THE ONE THAT COUNTS. Without this, `Intent` keeps returning the one that
		// opened the activity, and a second link -- another institution, with Gnothi already open -- is
		// lost.
		if (intento is not null) Intent = intento;

		string? bundle = intento?.Data?.GetQueryParameter("bundle");
		Log($"nuevo enlace: {intento?.Data?.ToString() ?? "(sin datos)"}");
		if (bundle is { Length: > 0 }) _ = shell?.Enroll(intento!.Data!.ToString()!, stop.Token);

		// A file, with Gnothi already open: the same reader as at launch.
		if (LeerArchivoDeModulo(intento, strict: true) is { } archivo)
		{
			_ = shell?.EnrollFile(archivo.Bytes, archivo.Name, stop.Token);
			return;
		}

		LeerTraspasoDe(intento);
		if (traspasoPendiente is { } t)
		{
			traspasoPendiente = null;
			_ = EntregarTraspaso(t.Modulo, t.Aserciones, stop.Token);
		}

		// The icon with Gnothi already open. Here the catalogue is ALREADY there, so it opens straight
		// away.
		LeerAperturaDe(intento);
		if (aperturaPendiente is { } cual)
		{
			aperturaPendiente = null;
			AbrirModulo(cual);
		}
	}

	// GNOTHI CAME BACK TO THE FRONT: the origins are asked again if it has been a while -the shell
	// caps it at one review an hour, and the launch review counts. OnResume also fires right after
	// OnCreate, while that first review is still running, and the shell's schedule says no to it.
	protected override void OnResume()
	{
		base.OnResume();
		shell?.ReviewOriginsIfDue();
	}

	public override void OnBackPressed()
	{
		// Back out of a module puts the home on screen again: same question, same cap.
		if (Cerrar()) { shell?.ReviewOriginsIfDue(); return; }

		web!.EvaluateJavascript("gn.back()", new BackAnswer(this));
	}

	private sealed class BackAnswer : Java.Lang.Object, IValueCallback
	{
		private readonly MainActivity host;
		public BackAnswer(MainActivity host) => this.host = host;

		public void OnReceiveValue(Java.Lang.Object? value)
		{
			if (value?.ToString() == "true") return;   // la pagina volvio sola
			host.MoveTaskToBack(true);
		}
	}

	// ── THE HANDOFF ARRIVING FROM THE STANDALONE INSTALL ─────────────────────────────────

	// What the link or the share sheet brought, waiting for there to be a catalogue.
	private (string? Modulo, string Aserciones)? traspasoPendiente;

	// TWO DOORS FOR THE SAME THING (specification 13 §5.2 and §6.1):
	//
	//   the link      go.gnothi.app/abrir?modulo=…&aserciones=…   when there are few and they fit in the URL
	//   sharing       ACTION_SEND with a text file                 when they do not fit
	//
	// Today in Exámenes, Sonia's assertions are ONE -- the sex she declared -- and fit in the link with
	// room to spare. The second door exists because other institutions are going to expect more.
	private void LeerTraspasoDe(Android.Content.Intent? intento)
	{
		if (intento is null) return;

		// Door 1: the link back that the standalone app assembled.
		string? porEnlace = intento.Data?.GetQueryParameter("aserciones");
		if (porEnlace is { Length: > 0 })
		{
			traspasoPendiente = (intento.Data?.GetQueryParameter("modulo"), porEnlace);
			Log($"traspaso: llegó por enlace, {porEnlace.Length} bytes");
			return;
		}

		// Door 2: the share sheet. The file travels as text/plain and NOT as application/json, because
		// Web Share rejects JSON: its list of allowed types is closed and does not include it. The .txt
		// name is wrapping; what matters is the contents.
		if (intento.Action != Android.Content.Intent.ActionSend) return;

		try
		{
			string? contenido = LeerLoCompartido(intento);
			if (contenido is not { Length: > 0 }) return;

			traspasoPendiente = (null, contenido);
			Log($"traspaso: llegó por la hoja de compartir, {contenido.Length} bytes");
		}
		catch (Exception ex)
		{
			Log($"traspaso: no se pudo leer lo compartido: {ex.GetType().Name}: {ex.Message}");
		}
	}

	private string? LeerLoCompartido(Android.Content.Intent intento)
	{
		// A share can bring the file or, if it was small, the bare text. Both are accepted.
		if (intento.GetParcelableExtra(Android.Content.Intent.ExtraStream) is Android.Net.Uri archivo)
		{
			using System.IO.Stream? cuerpo = ContentResolver!.OpenInputStream(archivo);
			if (cuerpo is null) return null;

			using var lector = new System.IO.StreamReader(cuerpo);
			return lector.ReadToEnd();
		}

		return intento.GetStringExtra(Android.Content.Intent.ExtraText);
	}

	// WHICH MODULE IT BELONGS TO. The link says so; the share sheet does not, so it is deduced from the
	// publisher the assertions declare. If it still cannot be known, it is said out loud: recording a
	// person's assertions into the wrong module would be worse than not recording them.
	private Gnothi.Host.Module? ADondeVan(string? nombrado, string asercionesJson)
	{
		if (shell is null) return null;
		if (nombrado is { Length: > 0 }) return shell.Find(nombrado);

		string publicador = "";
		try
		{
			using JsonDocument d = JsonDocument.Parse(asercionesJson);
			if (d.RootElement.TryGetProperty("publicador", out JsonElement p) && p.ValueKind == JsonValueKind.String)
				publicador = p.GetString() ?? "";
		}
		catch (JsonException) { }

		if (publicador.Length == 0) return null;

		foreach (Gnothi.Host.Module m in shell.Modules)
		{
			System.Collections.Generic.IReadOnlyList<string> args = m.Installation.Shell.Arguments;
			if (args.Count > 1 && args[1].StartsWith(publicador, StringComparison.OrdinalIgnoreCase)) return m;
		}

		return null;
	}

	private async Task EntregarTraspaso(string? nombrado, string asercionesJson, CancellationToken ct)
	{
		Gnothi.Host.Module? m = ADondeVan(nombrado, asercionesJson);
		if (m is null || anfitrion is null)
		{
			Log("traspaso: no se pudo saber a qué módulo corresponde; no se asentó nada");
			return;
		}

		try
		{
			AndroidWebModule vivo = await anfitrion.Open(m, ct);
			string resultado = await vivo.Handoff(asercionesJson, ct);
			Log($"traspaso a {m.Manifest.Module}: {resultado}");
		}
		catch (Exception ex)
		{
			Log($"traspaso a {m.Manifest.Module} falló: {ex.GetType().Name}: {ex.Message}");
		}
	}

	// ── EL ALTA DESDE UN ENLACE ──────────────────────────────────────────────────────────

	// What the enrolment link brought, waiting for the catalogue to exist. It cannot be installed
	// before that: the Host does not yet know where the modules live.
	private string? altaPendiente;

	// ── EL ALTA DESDE UN ARCHIVO ─────────────────────────────────────────────────────────

	// What a file brought, waiting for the catalog to exist -the same reason as altaPendiente.
	private (byte[] Bytes, string Name)? archivoPendiente;

	// What the picker brought back while the Host was not up yet -the system may recreate the
	// process behind the picker-, waiting like archivoPendiente does.
	private List<(byte[] Bytes, string Name)>? archivosPendientes;

	private const string ModuleMime = "application/vnd.gnothi.module";
	private const long FileCeiling = 64L * 1024 * 1024;
	private const int PickModuleRequest = 8;

	// THE FILE, IF THE INTENT BROUGHT ONE: opened (ACTION_VIEW on a content:// or file://) or
	// shared (ACTION_SEND with a stream). `strict` is for intents that arrive on their own -- they
	// are taken only when they look like a module, by mime or by name, so a shared text file still
	// reaches the handoff below. A file the person picked herself is read whatever it looks like:
	// she chose it, and the inspection is what tells her if it is not a module.
	private (byte[] Bytes, string Name)? LeerArchivoDeModulo(Android.Content.Intent? intento, bool strict)
	{
		if (intento is null) return null;

		Android.Net.Uri? uri = intento.Action == Android.Content.Intent.ActionView ? intento.Data
			: intento.Action == Android.Content.Intent.ActionSend ? intento.GetParcelableExtra(Android.Content.Intent.ExtraStream) as Android.Net.Uri
			: null;
		if (uri is null || uri.Scheme is not ("content" or "file")) return null;

		string name = DisplayNameOf(uri);
		string type = intento.Type ?? ContentResolver!.GetType(uri) ?? "";
		bool looksLikeModule = type == ModuleMime || type == "application/zip" || name.EndsWith(".gmod", StringComparison.OrdinalIgnoreCase);
		if (strict && !looksLikeModule) return null;

		try
		{
			byte[] bytes = LeerTodo(uri);
			Log($"alta: archivo '{name}' ({bytes.Length} bytes, {type}) por {intento.Action}");
			return (bytes, name);
		}
		catch (Exception ex)
		{
			Log($"alta: no se pudo leer el archivo '{name}': {ex.GetType().Name}: {ex.Message}");
			shell?.ReportEnrollmentError($"No se pudo leer el archivo {name}.");
			return null;
		}
	}

	// Through the ContentResolver, with the same ceiling as a download: a wrong file cannot fill
	// the device either.
	private byte[] LeerTodo(Android.Net.Uri uri)
	{
		using System.IO.Stream? content = ContentResolver!.OpenInputStream(uri)
			?? throw new System.IO.IOException("the content resolver returned no stream");
		using var buffer = new System.IO.MemoryStream();
		var chunk = new byte[81920];
		int read;
		while ((read = content.Read(chunk, 0, chunk.Length)) > 0)
		{
			buffer.Write(chunk, 0, read);
			if (buffer.Length > FileCeiling) throw new System.IO.IOException($"the file went past the {FileCeiling / 1048576} MB ceiling");
		}
		return buffer.ToArray();
	}

	// The name the person sees in Files, for the sheet: a content:// path says nothing.
	private string DisplayNameOf(Android.Net.Uri uri)
	{
		try
		{
			using Android.Database.ICursor? cursor = ContentResolver!.Query(uri, [Android.Provider.OpenableColumns.DisplayName], null, null, null);
			if (cursor is not null && cursor.MoveToFirst())
			{
				string? name = cursor.GetString(0);
				if (!string.IsNullOrWhiteSpace(name)) return name;
			}
		}
		catch (Exception) { /* a provider that will not say: fall back to the path */ }

		return uri.LastPathSegment ?? "archivo";
	}

	// "ELEGIR EL ARCHIVO EN MI TELÉFONO", from the page. The system's document picker: no storage
	// permission, no walk over the disk -- the system finds, the person chooses, Gnothi receives
	// exactly that one file. This is the phone-side of the "buscador" the plan turned down:
	// scanning Downloads would have asked the app whose thesis is that the data is hers for
	// permission over all of it.
	internal string PickModuleFile()
	{
		var pick = new Android.Content.Intent(Android.Content.Intent.ActionOpenDocument);
		pick.AddCategory(Android.Content.Intent.CategoryOpenable);
		pick.SetType("*/*");
		pick.PutExtra(Android.Content.Intent.ExtraMimeTypes, new[] { ModuleMime, "application/zip", "application/octet-stream" });
		// SEVERAL AT ONCE: she may have downloaded more than one. The shell shows what it found in
		// them and she chooses which to install, one proposal at a time.
		pick.PutExtra(Android.Content.Intent.ExtraAllowMultiple, true);
		// OPEN IN DOWNLOADS, where a .gmod that came through the browser or a chat lands, instead of
		// "Recent" and a drawer to find the folder. The picker's own filter is by MIME and cannot be
		// tighter than this: a .gmod has no registered type on the phone and shows up as
		// application/octet-stream, so the list Gnothi shows afterwards is what tells a module from
		// any other file.
		pick.PutExtra(Android.Provider.DocumentsContract.ExtraInitialUri,
			Android.Net.Uri.Parse("content://com.android.externalstorage.documents/document/primary%3ADownload"));
		StartActivityForResult(pick, PickModuleRequest);
		return "{\"ok\":true}";
	}

	protected override void OnActivityResult(int requestCode, Result resultCode, Android.Content.Intent? data)
	{
		base.OnActivityResult(requestCode, resultCode, data);
		Log($"alta: el selector volvió: request={requestCode} result={resultCode} data={data?.Data?.ToString() ?? "-"} clip={data?.ClipData?.ItemCount ?? 0}");
		if (requestCode != PickModuleRequest) return;

		// One document comes in Data; several come in ClipData, one item each.
		var uris = new List<Android.Net.Uri>();
		if (resultCode == Result.Ok && data is not null)
		{
			if (data.ClipData is { } clip)
				for (int i = 0; i < clip.ItemCount; i++)
					if (clip.GetItemAt(i)?.Uri is { } uri) uris.Add(uri);
			else if (data.Data is { } one) uris.Add(one);
		}
		if (uris.Count == 0)
		{
			Log("alta: la persona no eligió ningún archivo");
			return;
		}

		// Each answer of the picker, shaped as an "open" of that document. Not strict: she chose them.
		// SetDataAndType, never SetType: Intent.setType CLEARS the data uri (measured 2026-09-09: the
		// picker came back with three documents and the reader saw an intent without any).
		var archivos = new List<(byte[] Bytes, string Name)>();
		foreach (Android.Net.Uri uri in uris)
		{
			var abrir = new Android.Content.Intent(Android.Content.Intent.ActionView);
			abrir.SetDataAndType(uri, ContentResolver!.GetType(uri) ?? data!.Type);
			if (LeerArchivoDeModulo(abrir, strict: false) is { } archivo) archivos.Add(archivo);
		}
		if (archivos.Count == 0) return;
		if (shell is null) archivosPendientes = archivos;
		else _ = shell.EnrollFiles(archivos, stop.Token);
	}

	// ── THE MODULE ON SCREEN ─────────────────────────────────────────────────────────────

	// THE INSTITUTION'S PWA DRAWS ITSELF (specification 11 §8). Sonia opens the module and sees the
	// laboratory's dashboard: its branding, its Blazor, its charts. Gnothi neither describes it nor
	// paints it.
	//
	// AND IT IS THE SAME WebView that answers the assistant's questions. There are not two: just one,
	// which is sometimes on screen and sometimes only answering. That is why opening it is fast if it
	// was already alive.
	//
	// THE RULE THAT CHANGES WHEN IT IS SHOWN: while it is on screen it CANNOT be evicted, even at its
	// 200 MB. That is the only concession; the moment she leaves, it becomes evictable again.
	//
	// What does NOT change is the containment: it still has no network, no service worker and no
	// process of its own, and it stops existing when Gnothi evicts it. "It does not wake, it does not
	// notify, it does not interrupt" holds by incapacity, not because the module behaves.
	private string AbrirModulo(string nombre)
	{
		Gnothi.Host.Module? m = shell?.Find(nombre);
		if (m is null || anfitrion is null) return "{\"ok\":false}";

		_ = Task.Run(async () =>
		{
			try
			{
				AndroidWebModule vivo = await anfitrion.Open(m, stop.Token);
				RunOnUiThread(() => Mostrar(vivo));
			}
			catch (Exception ex)
			{
				Log($"no se pudo abrir {nombre}: {ex.GetType().Name}: {ex.Message}");
			}
		});

		return "{\"ok\":true}";
	}

	// The module's WebView goes ON TOP of Gnothi's, covering it. The view is not replaced: Gnothi's
	// stays alive behind it, with its state and its conversation intact.
	private void Mostrar(AndroidWebModule vivo)
	{
		Android.Views.View? vista = vivo.View;
		if (vista is null || raiz is null) return;

		Cerrar();

		enPantalla = vivo;
		vivo.Visible = true;
		raiz.AddView(vista, new Android.Widget.FrameLayout.LayoutParams(
			Android.Views.ViewGroup.LayoutParams.MatchParent, Android.Views.ViewGroup.LayoutParams.MatchParent));
	}

	// Taking it off the screen does NOT kill it: the module stays alive and goes on answering the
	// assistant's questions. It just stops being visible, and becomes evictable again.
	private bool Cerrar()
	{
		if (enPantalla is null) return false;

		Android.Views.View? vista = enPantalla.View;
		if (vista is not null) raiz?.RemoveView(vista);

		enPantalla.Visible = false;
		enPantalla = null;
		return true;
	}

	// ── THE SHORTCUT ON THE HOME SCREEN ─────────────────────────────────────────────────
	//
	// AN ICON THAT LOOKS LIKE THE INSTITUTION'S APP AND OPENS GNOTHI. It is as close as it is possible
	// to get to «their app» without the institution publishing one: the icon carries its logo and its
	// name, and tapping it makes Gnothi bring its module up. The process, the folder and the engine are
	// still Gnothi's, and that is not disguised -- the launcher stamps Gnothi's badge in the corner and
	// there is no way to remove it. It is right that it is there: it says who runs the code.
	//
	// IT IS NOT AN APP, AND THE DIFFERENCE MATTERS: it does not appear in the app drawer -- only
	// installed packages go there --, it has no process of its own, it does not wake, it does not
	// notify, and if Gnothi is uninstalled the icon stops working. It is exactly the containment the
	// module already has, with a shorter door.
	//
	// THE INTENT IS EXPLICIT AND NOT A LINK. `go.gnothi.app/abrir?modulo=…` exists and would be the
	// natural thing, but an https with no verified assetlinks.json brings up the «which app should I
	// open this with?» dialogue, depends on DNS, and anybody can register a handler for that domain. An
	// intent against this class never leaves the phone nor goes through the resolver.
	internal const string ExtraModulo = "gnothi.modulo";

	// The module the icon asked for, waiting for there to be a catalogue -- just like the enrolment and
	// the handoff. Tapping the icon with Gnothi closed reaches OnCreate BEFORE the Host is up, and
	// opening it there would find nothing to open.
	private string? aperturaPendiente;

	private void LeerAperturaDe(Android.Content.Intent? intento)
	{
		string? modulo = intento?.GetStringExtra(ExtraModulo);
		if (modulo is { Length: > 0 }) aperturaPendiente = modulo;
	}

	// WHAT THE PAGE ASKS FOR. It answers with `ok` and, when it could not be done, with the reason IN
	// WORDS: a button that does nothing and does not say why is worse than not having the button.
	private string FijarAcceso(string nombre)
	{
		Gnothi.Host.Module? m = shell?.Find(nombre);
		if (m is null) return Respuesta(false, "no encuentro ese módulo");

		// API 26, and the guard is written with OperatingSystem and not with Build.VERSION.SdkInt: the
		// compatibility analyser understands this shape and not the other one, so with the other one a
		// CA1416 was left on every line below here. Below 26 there was the
		// com.android.launcher.action.INSTALL_SHORTCUT broadcast, which Android 8 stopped serving: there
		// is nothing to fall back to. Gnothi's minimum is 24, so this really happens.
		if (!OperatingSystem.IsAndroidVersionAtLeast(26))
			return Respuesta(false, "este Android no sabe poner accesos directos");

		var gestor = (Android.Content.PM.ShortcutManager?)GetSystemService(ShortcutService);
		if (gestor is null) return Respuesta(false, "este teléfono no ofrece accesos directos");

		// The launcher may not know how to receive them -- and on MIUI there is also a permission of its
		// own, which this CANNOT request: if it is off, the pin is accepted and the icon never appears.
		if (!gestor.IsRequestPinShortcutSupported)
			return Respuesta(false, "la pantalla de inicio de este teléfono no acepta accesos directos");

		try
		{
			var abrir = new Android.Content.Intent(this, typeof(MainActivity));
			abrir.SetAction(Android.Content.Intent.ActionView);       // un shortcut SIN acción se rechaza
			abrir.PutExtra(ExtraModulo, m.Name);
			abrir.AddFlags(Android.Content.ActivityFlags.NewTask | Android.Content.ActivityFlags.SingleTop);

			// THE LABEL IS THE INSTITUTION'S NAME, in BOTH fields. The long one started out as the module's
			// title -- «Tus exámenes, al día» -- which is what anybody would write, and on the phone the
			// icon came out saying «Tus examenes,…»: MIUI prefers the long one and cuts it in half. An icon
			// carries a NAME, not a slogan, and both fields have to be readable when cut short.
			string rotulo = Primero(m.Manifest.Identity, m.Manifest.Institution, m.Manifest.Title, m.Name);

			var info = new Android.Content.PM.ShortcutInfo.Builder(this, "modulo:" + m.Name)
				.SetShortLabel(new Java.Lang.String(rotulo))!
				.SetLongLabel(new Java.Lang.String(rotulo))!
				.SetIntent(abrir)!;

			if (IconoDe(m) is { } icono) info.SetIcon(icono);

			Android.Content.PM.ShortcutInfo hecho = info.Build()!;

			// UPDATE FIRST AND ASK AFTERWARDS. If it is already pinned, `RequestPinShortcut` changes nothing
			// about it: the old icon keeps the label and the logo from the day it was put there.
			// `UpdateShortcuts` does reach the pinned ones, and it is what makes an institution that
			// changes its name or its logo in a new version show up on the home screen. It does nothing if
			// there were none, so there is no need to ask.
			gestor.UpdateShortcuts([hecho]);

			gestor.RequestPinShortcut(hecho, null);
			Log($"acceso directo: pedido el pin de '{m.Name}' como «{rotulo}»");
			return Respuesta(true, "");
		}
		catch (Exception ex)
		{
			Log($"acceso directo: {ex.GetType().Name}: {ex.Message}");
			return Respuesta(false, "no se pudo: " + ex.Message);
		}
	}

	private static string Primero(params string[] cuales)
	{
		foreach (string c in cuales) if (!string.IsNullOrWhiteSpace(c)) return c.Trim();
		return "";
	}

	private static string Respuesta(bool ok, string razon) =>
		JsonSerializer.Serialize(new { ok, razon });

	// THE ICON IS COMPOSED, the logo is not used as it is. An adaptive icon clips everything outside the
	// central 66% -- circle, rounded square, whatever the launcher decides -- so a logo laid out
	// full-bleed loses its edges. It is drawn over the branding colour and at 52% of the canvas, which
	// fits whole inside any mask.
	//
	// It returns null -- and the launcher puts up Gnothi's icon -- when the logo is an SVG: Android does
	// not decode those, and the Host's customs house does admit them.
	private Android.Graphics.Drawables.Icon? IconoDe(Gnothi.Host.Module m)
	{
		if (!OperatingSystem.IsAndroidVersionAtLeast(26)) return null;

		try
		{
			string uri = m.LogoDataUri();
			int coma = uri.IndexOf(",", StringComparison.Ordinal);
			if (coma < 0 || uri.Contains("svg", StringComparison.OrdinalIgnoreCase)) return null;

			byte[] crudo = Convert.FromBase64String(uri[(coma + 1)..]);
			Android.Graphics.Bitmap? logo = Android.Graphics.BitmapFactory.DecodeByteArray(crudo, 0, crudo.Length);
			if (logo is null) return null;

			const int lado = 432;   // 108dp a 4x, la medida que Android recomienda para el adaptativo
			Android.Graphics.Bitmap lienzo = Android.Graphics.Bitmap.CreateBitmap(
				lado, lado, Android.Graphics.Bitmap.Config.Argb8888!)!;
			var pincel = new Android.Graphics.Canvas(lienzo);
			pincel.DrawColor(ColorDeMarca(m));

			int cabe = (int)(lado * 0.52);
			float escala = Math.Min(cabe / (float)logo.Width, cabe / (float)logo.Height);
			int ancho = Math.Max(1, (int)(logo.Width * escala)), alto = Math.Max(1, (int)(logo.Height * escala));
			var donde = new Android.Graphics.Rect((lado - ancho) / 2, (lado - alto) / 2,
				(lado + ancho) / 2, (lado + alto) / 2);
			pincel.DrawBitmap(logo, null, donde, new Android.Graphics.Paint(Android.Graphics.PaintFlags.FilterBitmap));

			return Android.Graphics.Drawables.Icon.CreateWithAdaptiveBitmap(lienzo);
		}
		catch (Exception ex)
		{
			Log($"acceso directo: no se pudo componer el icono: {ex.GetType().Name}: {ex.Message}");
			return null;
		}
	}

	// The colour the institution declared, and the Host's navy when it declared none. The icon's
	// background CANNOT be transparent: in a circular mask the clipping would show.
	private static Android.Graphics.Color ColorDeMarca(Gnothi.Host.Module m)
	{
		string declarado = Gnothi.Host.Module.LookOf(m.Manifest, m.Directory, out _).Color;
		try { if (declarado.Length > 0) return Android.Graphics.Color.ParseColor(declarado); }
		catch (Exception) { /* un color que no parsea es como no haberlo declarado */ }
		return Android.Graphics.Color.ParseColor("#241F3D");
	}

	// ── THE CAMERA, FOR READING THE CODE ────────────────────────────────────────────────

	// The page's pending camera request while Android asks the person. One at a time: a second
	// request while the dialog is up replaces the first, which is denied.
	private PermissionRequest? cameraRequest;
	private const int CameraRequestCode = 7;

	// THE PAGE ASKED FOR THE CAMERA (getUserMedia, to read a code). The WebView cannot grant what
	// the app does not hold, so: held -> granted right away; not held -> the request waits while
	// Android asks the person, and OnRequestPermissionsResult answers it. Only video is ever
	// granted: the page reads codes, nothing else.
	internal void GrantCameraOrAsk(PermissionRequest request)
	{
		if (CheckSelfPermission(Android.Manifest.Permission.Camera) == Android.Content.PM.Permission.Granted)
		{
			request.Grant([PermissionRequest.ResourceVideoCapture]);
			return;
		}

		cameraRequest?.Deny();
		cameraRequest = request;
		RequestPermissions([Android.Manifest.Permission.Camera], CameraRequestCode);
	}

	public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Android.Content.PM.Permission[] grantResults)
	{
		base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
		if (requestCode != CameraRequestCode || cameraRequest is null) return;

		bool granted = grantResults.Length > 0 && grantResults[0] == Android.Content.PM.Permission.Granted;
		if (granted) cameraRequest.Grant([PermissionRequest.ResourceVideoCapture]);
		else cameraRequest.Deny();
		cameraRequest = null;

		Log(granted ? "cámara: concedida a la página" : "cámara: la persona no la concedió");
	}

	private void Log(string s) => global::Android.Util.Log.Info("GNOTHI", s);

	private void Push(string kind, string id, string payload) => RunOnUiThread(() =>
		web?.EvaluateJavascript(
			$"gn.push({JsonSerializer.Serialize(kind)},{JsonSerializer.Serialize(id)},{JsonSerializer.Serialize(payload)})",
			null));

	// ── the bridge itself ───────────────────────────────────────────────────────────────
	//
	// All this type does is TRANSLATE: it takes the json out of the dialogue and hands the answer back
	// through result.Confirm. What is done with the request belongs to Shell.Handle, and is identical on
	// iOS -- over there the same json arrives through WKUIDelegate and the answer goes out through a
	// completionHandler.
	private sealed class Bridge : WebChromeClient
	{
		private readonly MainActivity host;
		public Bridge(MainActivity host) => this.host = host;

		// getUserMedia from the page lands here. Anything that is not the camera is denied: the
		// page has no business with the microphone or anything else.
		public override void OnPermissionRequest(PermissionRequest? request)
		{
			if (request is null) return;

			string[] resources = request.GetResources() ?? [];
			if (!resources.Contains(PermissionRequest.ResourceVideoCapture)) { request.Deny(); return; }

			host.GrantCameraOrAsk(request);
		}

		// WHAT THE PAGE ASKS FOR is served by Shell.Handle, which is the SAME file on both platforms: the
		// state, the consent, the key, the provider and the question.
		//
		// THE THREE VERBS INTERCEPTED HERE are the ones that need a screen: opening a module puts its
		// WebView on top, and the enrolment proposal in flight is state of this Activity. iOS does the
		// same with its two in ViewController.Bridge.
		public override bool OnJsPrompt(WebView? view, string? url, string? message, string? defaultValue, JsPromptResult? result)
		{
			if (message != "gnothi" || defaultValue is null || result is null)
				return base.OnJsPrompt(view, url, message, defaultValue, result);

			try
			{
				JsonElement req = JsonSerializer.Deserialize<JsonElement>(defaultValue);
				string op = req.GetProperty("op").GetString() ?? "";

				result.Confirm(op switch
				{
					"abrir" => host.AbrirModulo(req.GetProperty("module").GetString() ?? ""),
					// THE ICON ON THE HOME SCREEN. It needs this Activity because the intent being pinned
					// points at IT, and because the dialogue Android shows is the system's and you have
					// to be in the foreground to see it.
					"fijar" => host.FijarAcceso(req.GetProperty("module").GetString() ?? ""),
					// The document picker is the platform's: the page asks, the Activity opens it,
					// and what she picks comes back through OnActivityResult into the enrollment.
					"altaArchivo" => host.PickModuleFile(),
					_ => host.shell?.Handle(defaultValue) ?? "{}",
				});
			}
			catch (Exception e)
			{
				host.Log($"bridge: {e.GetType().Name}: {e.Message}");
				result.Confirm("{}");
			}

			return true;
		}
	}

	private sealed class Insets : Java.Lang.Object, Android.Views.View.IOnApplyWindowInsetsListener
	{
		public Android.Views.WindowInsets OnApplyWindowInsets(Android.Views.View v, Android.Views.WindowInsets insets)
		{
			var bars = insets.GetInsets(Android.Views.WindowInsets.Type.SystemBars());
			v.SetPadding(bars.Left, bars.Top, bars.Right, bars.Bottom);
			return Android.Views.WindowInsets.Consumed;
		}
	}
}
