using Foundation;
using Gnothi.Assistant;
// THE TEXT CATALOGUE, by alias and not by importing the whole namespace: `Gnothi.Host` also
// declares a `Shell` -- installation.json block -- and this file uses Gnothi.MobileShared
// `Shell`. Importing both namespaces leaves the name ambiguous and iOS stops compiling. It is
// the SAME mistake 898edec fixed in MainActivity, and it survived here because the iOS SDK
// demands an exact version of Xcode and the build does not even start: it needs
// -p:ValidateXcodeVersion=false, which lives in the team recipe and not in anybody path.
using Textos = Gnothi.Host.Textos;
using Gnothi.MobileShared;
using UIKit;
using WebKit;

namespace Gnothi.iOS;

// GNOTHI'S HOST ON AN iPHONE, with a face. THE iOS HALF AND NOTHING ELSE.
//
// This file is the mirror of Gnothi.Mobile/MainActivity.cs, and they are worth reading side by side:
// they do the same things, in the same order, with the same names. What changes is the API.
//
//     Android                          iOS
//     -------------------------------  ---------------------------------------
//     WebView                          WKWebView
//     WebChromeClient.OnJsPrompt       WKUIDelegate.RunJavaScriptTextInputPanel
//     result.Confirm(x)                completionHandler(x)
//     RunOnUiThread                    InvokeOnMainThread
//     Android.Util.Log.Info            Console.WriteLine  (comes out through Xcode's log)
//     FilesDir                         Library/Application Support
//     GetExternalFilesDir/incoming     Documents/incoming   <- and this one IS VISIBLE in Files
//     file:///android_asset/ui.html    LoadFileUrl from the bundle
//
// THE ONLY REAL DIFFERENCE is in the last row, and it favours iOS: on Android the package is pushed
// with adb because there is no other way; here Documents/ shows up in the Files app, so the person
// can drop it in themselves. No cable and no developer.
public sealed class ViewController : UIViewController
{
	private static readonly UIColor Fondo = UIColor.FromRGB(0xF5, 0xF3, 0xFB);

	// [PENDING GEANINNA] The height of the module's strip. Here and not inline so that adjusting it is
	// changing one number.
	private const int FranjaAlto = 40;

	private WKWebView? web;
	private Shell? shell;
	private IosWebModuleHost? moduleHost;
	private NSObject? foregroundWatch;   // keeps the WillEnterForeground observer alive with this controller

	// THE MODULE THE PERSON IS LOOKING AT, and the button for leaving it. Both belong to this class and
	// not to the Shell: the Shell does not know a screen exists, and that is the line that lets the
	// same Host run on the desktop without dragging UIKit along.
	private IosWebModule? enPantalla;
	private string? enPantallaNombre;
	private UIViewController? enPantallaVc;

	public override void ViewDidLoad()
	{
		base.ViewDidLoad();

		// GNOTHI CAME BACK TO THE FRONT, the twin of OnResume on Android: the origins are asked again
		// if it has been a while -the shell caps it at one review an hour, and the launch review
		// counts. The shell may not exist yet when this is registered; it is read when it fires.
		foregroundWatch = NSNotificationCenter.DefaultCenter.AddObserver(
			UIApplication.WillEnterForegroundNotification, _ => shell?.ReviewOriginsIfDue());

		var config = new WKWebViewConfiguration();
		// THE MARK THE PAGE LOOKS FOR. ui.html does navigator.userAgent.includes('GnothiHost') and if it
		// does not see it, it NEVER calls prompt() -- in an ordinary browser that dialogue is modal and
		// freezes the page.
		//
		// ApplicationNameForUserAgent and NOT CustomUserAgent: the first ADDS to the default user agent,
		// the second REPLACES it whole. With the second the mark is still there but everything else is
		// lost, and that breaks any other detection the page does.
		config.ApplicationNameForUserAgent = "GnothiHost";

		// THE READER CAMERA STAYS IN THE PAGE. On an iPhone a WKWebView sends ALL video to the native
		// full-screen player, and the playsinline on ui.html <video> is not enough on its own: this
		// line is needed. Without it the getUserMedia stream came out as a film -X, picture in
		// picture, LIVE- and on closing it iOS left it paused behind the frame. Measured on the 13
		// Pro on 2026-09-11.
		//
		// And the second is the twin of MediaPlaybackRequiresUserGesture = false in MainActivity.cs:
		// the play() comes after the await of getUserMedia, and WebKit may have given up on the
		// person gesture by then.
		config.AllowsInlineMediaPlayback = true;
		config.MediaTypesRequiringUserActionForPlayback = WKAudiovisualMediaTypes.None;

		web = new WKWebView(View!.Bounds, config)
		{
			AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight,
			UIDelegate = new Bridge(this),
			BackgroundColor = Fondo,
			Opaque = false,
		};

		// The equivalent of Android's insets problem, and here it comes for free: it anchors to the safe
		// area guides and the system takes care of the dynamic island and the gesture bar.
		View.BackgroundColor = Fondo;
		View.AddSubview(web);

		string bundle = NSBundle.MainBundle.BundlePath;
		string html = Path.Combine(bundle, "Assets", "ui.html");
		if (File.Exists(html))
			web.LoadFileUrl(NSUrl.FromFilename(html), NSUrl.FromFilename(Path.GetDirectoryName(html)!));
		else
			Log($"NO se encontro {html} -- revisar el BundleResource de ui.html en el csproj");

		// THE TWO FOLDERS ARE CHOSEN BY THE PLATFORM, just as on Android.
		string files = Path.Combine(Home(NSSearchPathDirectory.LibraryDirectory), "Application Support");
		string incoming = Path.Combine(Home(NSSearchPathDirectory.DocumentDirectory), "incoming");
		Directory.CreateDirectory(files);

		SeedFromBundle(incoming);

		// THE PERSON LANGUAGE, RESOLVED ONCE AND HERE, before the shell composes its first sentence.
		// It comes from the DEVICE: there is no picker inside Mymi.
		//
		// CurrentUICulture works because this process is NATIVE -- the warning in Municipalidad.Wasm,
		// which says not to read the process culture, is for wasm, where that culture is the
		// runtime and not that of whoever uses the phone. Not here.
		//
		// And the page does NOT resolve it on its own: the shell tells it. Two resolvers would be two
		// sources of truth.
		Textos.Idioma = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

		shell = new Shell(
			// ReescribirRutas: true, and it is mandatory here. The package is assembled by
			// armar-paquete.ps1 with absolute Android paths, which over there are known in advance. The
			// iOS container carries a GUID that changes on every install, so the only valid path is the
			// one this process has just found out.
			new Shell.Paths(
				Files: files,
				Incoming: incoming,
				ReescribirRutas: true,
				Instantaneas: SharedContainer(),
				// The local snapshot is the one Intents/GnothiIntents.swift reads, the intent that lives
				// INSIDE this app. It does not depend on the App Group: if SharedContainer() returns
				// null, this one still gets written.
				FotoLocal: true),
			Log,
			(kind, id, payload) => InvokeOnMainThread(() =>
				web?.EvaluateJavaScript(Shell.PushScript(kind, id, payload), (_, _) => { })),

			// THIS PHONE'S MODEL, if it really generates. It is passed as a factory because the Shell is
			// shared with Android, where these symbols do not exist: over there the argument goes null and
			// the provider is not offered.
			//
			// The check is Works() and not isAvailable, which LIES -- on iOS 26.2 it says yes and the
			// generation fails. Works() verifies it by generating, and caches.
			ModelFoundation.Works() ? encuadre => new ModelFoundation(encuadre) : null);

		// WHO KNOWS HOW TO CREATE WEBVIEWS. ChooseShell is static and has no ViewController, so the
		// webview shell cannot build the moduleHost: this class registers it, because this class does
		// have one. Without this, a module with "type": "webview" fails saying exactly that.
		//
		// It is the SAME line MainActivity runs on Android, and in the same shape: the Host only names
		// the interface, the platform provides whoever fulfils it.
		moduleHost = new IosWebModuleHost(this, new IosSecretStore(), Log);
		Gnothi.Host.WebViewShell.Host = moduleHost;

		// AND WHO KNOWS HOW TO OPEN THE SALA. Same reason and same seam, one line lower than in
		// MainActivity: it is ONE document for every module, created the first time somebody asks.
		// It does not receive `this`, because unlike a module's WebView the sala is never shown --
		// it needs no ViewController to put it on screen.
		var room = new IosRoom(Log);
		Gnothi.Host.RoomShell.Host = room;

		// Y EL MISMO CABLE QUE `HybridShell.Log`: sin el, el contador de vueltas al borde de la sala
		// cuenta y no lo dice nadie. En iOS importa mas que en ningun lado -- es la plataforma donde
		// el precio de una vuelta esta sin medir.
		Gnothi.Host.RoomShell.Log = Log;

		// THE SALA, FORCED IN ORDER TO MEASURE, without touching anybody's installation.json. The
		// rest of the path is the usual one: Dispatcher, catalogue, consent. The only thing that
		// changes is which shell builds the Module for those names.
		//
		// WHERE THE SWITCH COMES FROM, AND WHY FROM THERE. `Library/Application Support` belongs to
		// this app's container: no other app sees it -- iOS has no equivalent of an Intent extra,
		// which is the anybody-channel Android had to be stripped of -- and, unlike `Documents`, the
		// Files app does NOT expose it, so the person does not touch it by accident either. It is
		// read ONCE, here: changing it with the app alive does nothing.
		//
		//     xcrun simctl get_app_container booted com.ncubo.gnothi.host data
		//     echo comprasw,examenesw > <ese contenedor>/Library/Application\ Support/sala
		//
		// And besides, it goes in DEBUG, so the .app that ships has no way of switching it on even
		// if somebody leaves the file in place. It is the same double guard as Android: there, the
		// private file PLUS the debuggable flag.
#if DEBUG
		string switchFile = Path.Combine(files, "sala");
		if (File.Exists(switchFile))
		{
			// THE EMPTY FILE ALREADY SAYS SOMETHING, and it is on purpose that it says the least:
			// start the sala and nothing else. The question that decides the form on iOS -- whether
			// the substrate comes up under `gnothi-sala://` -- is answered with the substrate ALONE,
			// with no module in between. With names inside, those modules also run through the sala.
			//
			// Separate and not together: if they were measured in the same step, an enrolment that
			// failed would read the same as an origin that does not serve, and they are two
			// different fixes.
			_ = Task.Run(async () =>
			{
				try { await room.WarmUp(CancellationToken.None); Log("sala: arranco sin modulos"); }
				catch (Exception e) { Log($"sala: NO arranco -- {e.GetType().Name}: {e.Message}"); }
			});

			HashSet<string> names = [.. File.ReadAllText(switchFile)
				.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
			if (names.Count > 0)
			{
				Log($"sala: forzada para {string.Join(", ", names)}");

				// AND IT CHAINS instead of assigning. IT BREAKS NOTHING TODAY -- there is no box on
				// iOS, so this is the only factory -- and it is written this way for the same
				// reason a fence is: on Android it DID break. The defect was in the shape, not in
				// the coincidence of there being two. The reasoning is in
				// Module.ChainShellFactory, in one place for both platforms.
				if (Gnothi.Host.Module.ChainShellFactory((name, installation) => names.Contains(name)
						? new Gnothi.Host.RoomShell(name, installation.Shell, installation.GrantedFolder)
						: null))
					Log("sala: ya habia una fabrica de cascaras; la sala se encadena y solo contesta por sus nombres");
			}
		}
#endif

		// AND WHO CAN CLOSE THEM: an update moves the module's folder while its WebView may be
		// sitting on it (44 §3.6). The Shell only names the hook; the platform fulfils it.
		shell.CloseModule = module =>
		{
			if (!moduleHost.IsLive(module)) return false;
			moduleHost.Evict(module);
			return true;
		};

		_ = Task.Run(async () =>
		{
			// THE PROBE GOES BEFORE THE START-UP, and on purpose: if the Host does not come up because
			// the AOT would not let a module load, the answer to WHY has to be written in the log
			// already. A spike that fails without saying which of the four steps it failed at costs an
			// afternoon.
			Probe.Run(Log);
			BridgeProbe.Run(Log);
			try
			{
				// Run() is the Host's loop and never ends: it is started ONCE and awaited at the end. In
				// between, whatever had arrived earlier gets served.
				Task corriendo = shell.Run();

				// THE LINK THAT ARRIVED BEFORE THE HOST CAME UP. Android has the same pending slot for
				// the same reason: iOS can launch the app WITH the universal link, and then
				// ContinueUserActivity runs before this has finished assembling itself.
				if (pendingEnrollment is { Length: > 0 } p) { pendingEnrollment = null; await shell.Enroll(p); }

				// AND THE FILES THAT ARRIVED BEFORE IT: a .gmod opened with «Copiar en Gnothi» can
				// launch the app too. Same shape as archivoPendiente on Android.
				if (pendingFiles is { Count: > 0 } files) { pendingFiles = null; await shell.EnrollFiles(files); }

				await corriendo;
			}
			catch (Exception e) { Log($"shell fallo: {e.GetType().Name}: {e.Message}"); }
		});
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing) shell?.Dispose();
		base.Dispose(disposing);
	}

	/// The first time, if the package shipped inside the app, Documents/incoming is seeded. After that
	/// it is not touched: whatever the person drops there through Files wins.
	private void SeedFromBundle(string incoming)
	{
		if (Directory.Exists(incoming)) return;

		string semilla = Path.Combine(NSBundle.MainBundle.BundlePath, "paquete");
		if (!Directory.Exists(semilla))
		{
			Log("no hay paquete en el bundle ni en Documents/incoming: dejalo por la app Archivos");
			return;
		}

		int n = 0;
		foreach (string file in Directory.GetFiles(semilla, "*", SearchOption.AllDirectories))
		{
			string destino = file.Replace(semilla, incoming);
			Directory.CreateDirectory(Path.GetDirectoryName(destino)!);
			File.Copy(file, destino, overwrite: true);
			n++;
		}
		Log($"sembrado desde el bundle: {n} archivos -> {incoming}");
	}

	private static string Home(NSSearchPathDirectory which) =>
		NSSearchPath.GetDirectories(which, NSSearchPathDomain.User, true)[0];

	/// THE CONTAINER SHARED WITH THE CARATULA, or null if it is not provisioned.
	///
	/// It is the only way the .NET Host and Swift's App Intents can pass each other anything: they do
	/// not call each other, they leave a file. And it works because both apps are OURS -- App Groups
	/// demands the same Team ID, which is also the reason this would not work for a third-party
	/// institution shipping its own caratula.
	///
	/// IT RETURNS NULL IF PROVISIONING IS MISSING, and you have to read the log to find out: the App
	/// Group has to be enabled in Apple's portal for BOTH app IDs and come in the provisioning profile.
	/// If it is missing, this does not throw -- it returns null in silence, which is the kind of failure
	/// that costs an afternoon if it is not written down somewhere.
	private static string? SharedContainer()
	{
		const string grupo = "group.com.ncubo.gnothi";
		string? ruta = NSFileManager.DefaultManager.GetContainerUrl(grupo)?.Path;

		if (ruta is null)
			Log($"NO hay contenedor para '{grupo}': la caratula no va a tener foto que leer. " +
				"Revisar que el App Group este habilitado en el portal de Apple para esta app " +
				"y para la del spike, y que el perfil de aprovisionamiento lo traiga.");
		else
			Log($"contenedor compartido: {ruta}");

		return ruta;
	}

	// It comes out through Xcode's log and through the Mac's `log stream`. The equivalent of Android's
	// `adb logcat -s GNOTHI` is:
	//
	//     log stream --predicate 'processImagePath CONTAINS "Gnothi"' --style compact
	// ── OPENING A MODULE AND SEEING IT ───────────────────────────────────────────────────
	//
	// The mirror of AbrirModulo/Mostrar/Cerrar in Gnothi.Mobile/MainActivity.cs, and deliberately with
	// the same names: they are the three pieces that cannot be shared, so the only thing that can keep
	// them together is that they read the same.

	// IT ANSWERS NOW AND OPENS AFTERWARDS. Bringing a module up means hydrating its actor, which takes
	// time; if this waited, prompt() on the page's side would be blocked and the face would freeze. The
	// page receives "ok" and the view appears when it is ready.
	private string AbrirModulo(string nombre)
	{
		Gnothi.Host.Module? m = shell?.Find(nombre);
		if (m is null || moduleHost is null) { Log($"abrir: no conozco '{nombre}'"); return "{\"ok\":false}"; }

		_ = Task.Run(async () =>
		{
			try
			{
				// If the assistant has already asked it something, the module is alive and opening is
				// instant: Open hands it back as it is. That is the reason P1a could be measured without
				// this screen existed.
				IosWebModule vivo = await moduleHost.Open(m, CancellationToken.None);
				InvokeOnMainThread(() => Mostrar(vivo, nombre));
			}
			catch (Exception ex)
			{
				Log($"no se pudo abrir {nombre}: {ex.GetType().Name}: {ex.Message}");
			}
		});

		return "{\"ok\":true}";
	}

	// THE MODULE TAKES THE WHOLE SCREEN, with a STRIP of Gnothi on top. Geaninna's decision, and the
	// pattern is the in-app browser's: the host keeps one line and gives it the rest.
	//
	// WHY THE STRIP AND NOT A FLOATING BUTTON. Measured on 2026-09-07: a '‹ Gnothi' on top of a
	// full-bleed module covered its logo and the first letter of "Supermercado La Guaria". At the
	// bottom the same happens with its nav. A module that takes up everything leaves no room for
	// somebody else's chrome, so the chrome has to take its own space and push the module down.
	//
	// WHY THE STRIP AND NOT A SHEET WITH A GRABBER, which was the previous version. The sheet HINTS
	// that you are inside Gnothi -- the app peeks out behind it --; the strip SAYS SO. And that is the
	// whole argument: this is the institution's app, running under Gnothi, reading a record that lives
	// on the phone. Besides, a labelled button beats a gesture: the grabber is standard but has to be
	// discovered, and "Listo" does not.
	//
	// WHAT IS NOT COPIED FROM THE PATTERN IS THE URL. An in-app browser shows the origin because the
	// content came off the network and there the origin is what can be audited. Here it is served by
	// Gnothi from its own container and the origin is gnothi-mod://<module>, which tells the person
	// nothing -- and showing it would be worse than not showing it, because it looks like an address
	// and is not one. The institution's NAME goes there instead. The pattern's chrome is taken, not its
	// semantics.
	//
	// [PENDING GEANINNA] This strip is the MINIMAL one on purpose, so she can adjust it over something
	// that already runs. What has to be watched is its weight: the module ALREADY brings its own header
	// and its own nav, so this makes two stacked headers. If it grows, the institution's app looks
	// boxed in. Height, typography and whether the subtitle goes there at all are hers.
	//
	// [DELIBERATE DIVERGENCE FROM ANDROID] Over there there is no strip because the system's back
	// button is enough. This point deliberately LOOKS different on the two platforms; it is noted in
	// PRUEBAS-WEBVIEW-IOS.md so nobody reports it as a bug.
	private void Mostrar(IosWebModule vivo, string nombre)
	{
		UIView? vista = vivo.View;
		if (vista is null) return;

		Cerrar();

		enPantalla = vivo;
		enPantallaNombre = nombre;
		// Visible belongs to the shared code and is NOT cosmetic: WebModuleHost.Evict refuses to evict a
		// module the person is looking at.
		vivo.Visible = true;

		// A VIEW CONTROLLER OF ITS OWN AND NOT the bare view: presenting demands a controller, and it is
		// also what gives the module's WKWebView a lifecycle to lean on.
		var pantalla = new UIViewController();
		UIView raiz = pantalla.View!;
		raiz.BackgroundColor = Fondo;

		var franja = new UIView { BackgroundColor = Fondo, TranslatesAutoresizingMaskIntoConstraints = false };

		// The institution's name, not the module's: 'compras' is the technical key and "Mis compras" is
		// what the person recognises.
		var quien = new UILabel
		{
			Text = Textos.T("ios.adentro-de", ("titulo", shell?.TitleOf(nombre) ?? nombre)),
			Font = UIFont.SystemFontOfSize(13, UIFontWeight.Medium),
			TextColor = UIColor.FromRGB(0x3B, 0x2E, 0x63),
			TranslatesAutoresizingMaskIntoConstraints = false,
		};

		var listo = UIButton.FromType(UIButtonType.System);
		listo.SetTitle(Textos.T("general.listo"), UIControlState.Normal);
		listo.TitleLabel!.Font = UIFont.SystemFontOfSize(15, UIFontWeight.Semibold);
		listo.TranslatesAutoresizingMaskIntoConstraints = false;
		listo.TouchUpInside += (_, _) => Cerrar();

		franja.AddSubview(quien);
		franja.AddSubview(listo);

		vista.TranslatesAutoresizingMaskIntoConstraints = false;
		raiz.AddSubview(franja);
		raiz.AddSubview(vista);

		NSLayoutConstraint.ActivateConstraints([
			franja.TopAnchor.ConstraintEqualTo(raiz.SafeAreaLayoutGuide.TopAnchor),
			franja.LeadingAnchor.ConstraintEqualTo(raiz.LeadingAnchor),
			franja.TrailingAnchor.ConstraintEqualTo(raiz.TrailingAnchor),
			franja.HeightAnchor.ConstraintEqualTo(FranjaAlto),

			quien.LeadingAnchor.ConstraintEqualTo(franja.LeadingAnchor, 16),
			quien.CenterYAnchor.ConstraintEqualTo(franja.CenterYAnchor),
			quien.TrailingAnchor.ConstraintLessThanOrEqualTo(listo.LeadingAnchor, -12),

			listo.TrailingAnchor.ConstraintEqualTo(franja.TrailingAnchor, -16),
			listo.CenterYAnchor.ConstraintEqualTo(franja.CenterYAnchor),

			// THE MODULE STARTS BELOW THE STRIP, which is what avoids the overlap by construction and not
			// by choosing the coordinates well.
			vista.TopAnchor.ConstraintEqualTo(franja.BottomAnchor),
			vista.LeadingAnchor.ConstraintEqualTo(raiz.LeadingAnchor),
			vista.TrailingAnchor.ConstraintEqualTo(raiz.TrailingAnchor),
			vista.BottomAnchor.ConstraintEqualTo(raiz.BottomAnchor),
		]);

		// FullScreen and not PageSheet: with the strip saying where you are, leaving Gnothi peeking out
		// behind adds nothing and eats the module screen.
		pantalla.ModalPresentationStyle = UIModalPresentationStyle.FullScreen;

		// THE DELEGATE STAYS even though in FullScreen there is no dismissal by gesture. It costs
		// nothing and protects what comes next: if somebody adds another closing path and forgets
		// to clean up, the module stays Visible forever and WebModuleHost.Evict never evicts it
		// again.
		pantalla.PresentationController!.Delegate = new AlDescartar(this);

		enPantallaVc = pantalla;
		PresentViewController(pantalla, animated: true, completionHandler: null);

		Log($"abrir: '{nombre}' en pantalla");
	}

	// Taking it off the screen does NOT kill it: the module stays alive and goes on answering the
	// asistente. Solo deja de verse, y vuelve a ser desalojable.
	private bool Cerrar()
	{
		if (enPantalla is null) return false;

		if (enPantallaVc is { } vc && PresentedViewController == vc)
			DismissViewController(animated: true, completionHandler: null);

		Soltar();
		return true;
	}

	// What has to be undone whatever happens, whether it comes from Cerrar() or from the gesture. The
	// module's view is taken out of the sheet's hierarchy so that a later Mostrar() can put it back:
	// the WKWebView belongs to the module and outlives the screen.
	private void Soltar()
	{
		if (enPantalla is null) return;

		enPantalla.View?.RemoveFromSuperview();
		enPantalla.Visible = false;
		Log($"cerrar: '{enPantallaNombre}' fuera de pantalla");

		enPantalla = null;
		enPantallaNombre = null;
		enPantallaVc = null;

		// The home is on screen again, whichever way the module went: same question, same cap.
		shell?.ReviewOriginsIfDue();
	}

	// The gesture of dragging the sheet downwards does not go through Cerrar(): it arrives here.
	private sealed class AlDescartar : UIAdaptivePresentationControllerDelegate
	{
		private readonly ViewController host;
		public AlDescartar(ViewController host) => this.host = host;

		public override void DidDismiss(UIPresentationController presentationController) => host.Soltar();
	}

	private static void Log(string s) => Console.WriteLine($"GNOTHI: {s}");
	private string? pendingEnrollment;

	// ── ENROLMENT FROM A FILE, the half that IS iOS-specific ───────────────────────────────
	//
	// WRITTEN ON WINDOWS ON 2026-09-09, WITHOUT A MAC TO COMPILE IT: it mirrors what
	// MainActivity.cs does and was measured to do on Android that day -the document picker with
	// several files, the list of what was found, and "open with"- and it was proofread against
	// the .NET for iOS bindings, not built. Whoever has the Mac compiles it and runs
	// PRUEBAS-WEBVIEW-IOS.md P6d. The shared part -reading the zips, the quick look, the list,
	// the proposal- is Shell.EnrollFiles and needs nothing here.

	// The same ceiling a download has: a wrong file cannot fill the phone either.
	private const long FileCeiling = 64L * 1024 * 1024;

	// What a document brought before the Host was up, waiting like pendingEnrollment does.
	private List<(byte[] Bytes, string Name)>? pendingFiles;

	/// «Copiar en Gnothi»: a .gmod handed over by Archivos, AirDrop or a chat, as a file url.
	/// The url may be security-scoped (LSSupportsOpeningDocumentsInPlace is on), so it is
	/// opened under its scope and read at once; nothing of it is kept but the bytes.
	internal void HandleFile(NSUrl url)
	{
		(byte[] Bytes, string Name)? archivo = LeerDocumento(url);
		if (archivo is null) return;

		if (shell is null) { (pendingFiles ??= []).Add(archivo.Value); Log("alta: el Host todavia no levanto; el archivo queda pendiente"); return; }
		_ = shell.EnrollFile(archivo.Value.Bytes, archivo.Value.Name);
	}

	// THE SYSTEM'S DOCUMENT PICKER, asked for the module type, zips and generic data -a .gmod
	// that went through a chat often arrives typed as nothing- and for SEVERAL at once: the
	// shell shows what it found in them and the person chooses which to install, one proposal at
	// a time. `asCopy: true` gives Gnothi its own copies under tmp, readable without any scope.
	// No storage permission is involved: the system searches, the person chooses, Gnothi
	// receives those files and nothing else (NotasGnothiHost/42 §8).
	internal string PickModuleFiles()
	{
		UniformTypeIdentifiers.UTType?[] candidates =
		[
			UniformTypeIdentifiers.UTType.CreateFromIdentifier("app.gnothi.module"),
			UniformTypeIdentifiers.UTTypes.Zip,
			UniformTypeIdentifiers.UTTypes.Data,
		];
		var picker = new UIDocumentPickerViewController(candidates.OfType<UniformTypeIdentifiers.UTType>().ToArray(), asCopy: true)
		{
			AllowsMultipleSelection = true,
		};
		picker.DidPickDocumentAtUrls += (_, e) =>
		{
			var archivos = new List<(byte[] Bytes, string Name)>();
			foreach (NSUrl url in e.Urls)
				if (LeerDocumento(url) is { } archivo) archivos.Add(archivo);

			if (archivos.Count == 0) { Log("alta: la persona no eligió ningún archivo"); return; }
			if (shell is null) pendingFiles = archivos;
			else _ = shell.EnrollFiles(archivos);
		};
		picker.WasCancelled += (_, _) => Log("alta: la persona no eligió ningún archivo");
		PresentViewController(picker, true, null);
		return "{\"ok\":true}";
	}

	// Read whole, under the url's security scope when it has one, with the ceiling enforced
	// while it reads. The name is what Archivos shows: the last path component.
	private (byte[] Bytes, string Name)? LeerDocumento(NSUrl url)
	{
		string name = url.LastPathComponent ?? "archivo";
		bool scoped = url.StartAccessingSecurityScopedResource();
		try
		{
			string? path = url.Path;
			if (path is null) { Log($"alta: no se pudo leer el archivo '{name}': la url no tiene ruta"); return null; }

			using FileStream file = File.OpenRead(path);
			if (file.Length > FileCeiling) throw new IOException($"the file weighs {file.Length / 1048576} MB and the ceiling is {FileCeiling / 1048576} MB");
			byte[] bytes = new byte[file.Length];
			int at = 0, read;
			while (at < bytes.Length && (read = file.Read(bytes, at, bytes.Length - at)) > 0) at += read;

			Log($"alta: archivo '{name}' ({bytes.Length} bytes) por documento");
			return (bytes, name);
		}
		catch (Exception ex)
		{
			Log($"alta: no se pudo leer el archivo '{name}': {ex.GetType().Name}: {ex.Message}");
			shell?.ReportUnreadableFile(name);
			return null;
		}
		finally
		{
			if (scoped) url.StopAccessingSecurityScopedResource();
		}
	}

	/// WHAT ARRIVES FROM THE UNIVERSAL LINK. If the Host has not come up yet this is kept and
	/// attended when it does -- the same as altaPendiente on Android.
	internal void HandleLink(string url)
	{
		if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? where)) { Log($"alta: enlace ilegible: {url}"); return; }

		// The bundle travels as a QUERY PARAMETER, not as a path: the institution serves its
		// zip wherever it likes and go.gnothi.app only carries it.
		string bundle = System.Web.HttpUtility.ParseQueryString(where.Query)["bundle"] ?? "";
		if (bundle.Length == 0) { Log($"alta: el enlace no trae ?bundle= : {url}"); return; }

		// The whole link goes to the shell, which takes it apart itself: since 2026-09-08 it may
		// carry the seal of the manifest (&h=), and handing over only ?bundle= would drop it.
		if (shell is null) { pendingEnrollment = url; Log("alta: el Host todavia no levanto; queda pendiente"); return; }

		_ = shell.Enroll(url);
	}

	// ── the bridge, the half that IS iOS-specific ───────────────────────────────────────
	//
	// The page calls window.prompt('gnothi', json), just as on Android. All this type does is
	// TRANSLATE: it takes the json out and hands the answer back through the completionHandler. What is
	// done with the request belongs to Shell.Handle, and that is the same file on both.
	//
	// ONE DIFFERENCE THAT CANNOT BE NOTICED FROM THE PAGE: here the native side is asynchronous -- you
	// answer by calling a handler, not by returning. On the javascript side prompt() blocks all the
	// same until that handler is called, so ui.html never finds out and never changes.
	private sealed class Bridge : WKUIDelegate
	{
		private readonly ViewController host;
		public Bridge(ViewController host) => this.host = host;

		// THE CAMERA, for the code reader (iOS 15+). The page asks with getUserMedia; WebKit asks
		// this delegate before asking the person, and without an answer the request is denied
		// and the reader dies with NotAllowedError. Granting here is not skipping her: iOS still
		// shows its own dialog the first time, with NSCameraUsageDescription as the text. Only the
		// camera, and only for our own page: the modules run in their own WKWebViews with their
		// own delegate, so nothing of theirs reaches this. Written 2026-09-09 without a Mac.
		public override void RequestMediaCapturePermission(
			WKWebView webView, WKSecurityOrigin origin, WKFrameInfo frame, WKMediaCaptureType type, Action<WKPermissionDecision> decisionHandler)
		{
			bool camera = type == WKMediaCaptureType.Camera;
			Log($"cámara: {(camera ? "concedida a la página" : "pedido de " + type + " denegado")}");
			decisionHandler(camera ? WKPermissionDecision.Grant : WKPermissionDecision.Deny);
		}

		public override void RunJavaScriptTextInputPanel(
			WKWebView webView, string prompt, string? defaultText, WKFrameInfo frame, Action<string?> completionHandler)
		{
			if (prompt != "gnothi" || defaultText is null)
			{
				// A real prompt. It is cancelled: this app has none.
				completionHandler(null);
				return;
			}

			// ALMOST EVERYTHING the page asks for is served by Shell.Handle, which is the SAME file on both
			// platforms -- both enrolment verbs included, ever since the proposal came to live over there.
			//
			// THE ONLY OP INTERCEPTED HERE IS 'abrir', and for the same reason as on Android
			// (MainActivity.cs): putting a view on screen is the only thing in the whole protocol that
			// CANNOT be shared, because UIView and android.view.View have nothing in common. Shell.Handle
			// does not know about it on purpose -- if it did, it would have to know about screens -- and
			// that is why it fell through to its `_ => "{}"`: the button reached the Host and the Host had
			// nothing to do with it. Measured on 2026-09-07: `bridge: abrir` showed up in the log, which is
			// written BEFORE the switch, and after that nothing.
			//
			// AND 'altaArchivo' (2026-09-09), for the same reason: the document picker is a view
			// controller of this screen. What she picks comes back into the shared enrollment.
			string respuesta;
			try
			{
				System.Text.Json.JsonElement req = Json.Parse(defaultText);
				respuesta = req.GetProperty("op").GetString() switch
				{
					"abrir" => host.AbrirModulo(req.GetProperty("module").GetString() ?? ""),
					"altaArchivo" => host.PickModuleFiles(),
					_ => host.shell?.Handle(defaultText) ?? "{}",
				};
			}
			catch (Exception e)
			{
				Log($"bridge: {e.GetType().Name}: {e.Message}");
				respuesta = "{}";
			}

			completionHandler(respuesta);
		}
	}
}
