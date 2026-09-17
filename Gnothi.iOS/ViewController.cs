using Foundation;
using Gnothi.Assistant;
// EL CATALOGO DE TEXTOS, por alias y no importando el namespace entero: `Gnothi.Host`
// tambien declara un `Shell` -- el bloque de installation.json -- y este archivo usa el
// `Shell` de Gnothi.MobileShared. Importar los dos namespaces deja el nombre ambiguo y el iOS
// deja de compilar. Es el MISMO error que 898edec arreglo en MainActivity, y aca sobrevivio porque
// el SDK de iOS exige una version exacta de Xcode y el build ni empieza: hay que pasarle
// -p:ValidateXcodeVersion=false, que esta en la receta del equipo y no en el camino de nadie.
using Textos = Gnothi.Host.Textos;
using Gnothi.MobileShared;
using UIKit;
using WebKit;

namespace Gnothi.iOS;

// EL HOST DE GNOTHI EN UN iPHONE, con cara. LA MITAD DE iOS Y NADA MAS.
//
// Este archivo es el espejo de Gnothi.Mobile/MainActivity.cs, y conviene leerlos al lado:
// hacen lo mismo, en el mismo orden, con los mismos nombres. Lo que cambia es la API.
//
//     Android                          iOS
//     -------------------------------  ---------------------------------------
//     WebView                          WKWebView
//     WebChromeClient.OnJsPrompt       WKUIDelegate.RunJavaScriptTextInputPanel
//     result.Confirm(x)                completionHandler(x)
//     RunOnUiThread                    InvokeOnMainThread
//     Android.Util.Log.Info            Console.WriteLine  (sale por el log de Xcode)
//     FilesDir                         Library/Application Support
//     GetExternalFilesDir/incoming     Documents/incoming   <- y este SE VE en Archivos
//     file:///android_asset/ui.html    LoadFileUrl del bundle
//
// LA UNICA DIFERENCIA DE FONDO esta en la ultima fila, y es a favor de iOS: en Android el
// paquete se empuja con adb porque no hay otra forma; aca Documents/ aparece en la app
// Archivos, asi que la persona puede dejarlo ella misma. Sin cable y sin desarrollador.
public sealed class ViewController : UIViewController
{
	private static readonly UIColor Fondo = UIColor.FromRGB(0xF5, 0xF3, 0xFB);

	// [PENDIENTE DE GEANINNA] El alto de la franja del modulo. Aca y no inline para que ajustarlo
	// sea cambiar un numero.
	private const int FranjaAlto = 40;

	private WKWebView? web;
	private Shell? shell;
	private IosWebModuleHost? moduleHost;
	private NSObject? foregroundWatch;   // keeps the WillEnterForeground observer alive with this controller

	// EL MODULO QUE LA PERSONA ESTA MIRANDO, y el boton para salir de el. Los dos son de esta
	// clase y no del Shell: el Shell no sabe que existe una pantalla, y esa es la linea que
	// permite que el mismo Host corra en escritorio sin arrastrar UIKit.
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
		// LA MARCA QUE LA PAGINA BUSCA. ui.html hace navigator.userAgent.includes('GnothiHost')
		// y si no la ve NUNCA llama prompt() -- en un browser comun ese dialogo es modal y
		// congela la pagina.
		//
		// ApplicationNameForUserAgent y NO CustomUserAgent: el primero AGREGA al user agent por
		// omision, el segundo lo REEMPLAZA entero. Con el segundo la marca queda igual pero se
		// pierde todo lo demas, y eso rompe cualquier otra deteccion de la pagina.
		config.ApplicationNameForUserAgent = "GnothiHost";

		// LA CAMARA DEL LECTOR SE QUEDA EN LA PAGINA. En iPhone un WKWebView manda TODO video al
		// reproductor nativo de pantalla completa, y el playsinline del <video> de ui.html no
		// alcanza solo: hace falta esta linea. Sin ella el stream de getUserMedia salia como
		// una pelicula -X, imagen en imagen, LIVE- y al cerrarla iOS lo dejaba pausado detras
		// del recuadro. Medido en el 13 Pro el 2026-09-11.
		//
		// Y la segunda es la gemela de MediaPlaybackRequiresUserGesture = false en
		// MainActivity.cs: el play() viene despues del await de getUserMedia, y WebKit puede
		// dar por perdido el gesto de la persona para entonces.
		config.AllowsInlineMediaPlayback = true;
		config.MediaTypesRequiringUserActionForPlayback = WKAudiovisualMediaTypes.None;

		web = new WKWebView(View!.Bounds, config)
		{
			AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight,
			UIDelegate = new Bridge(this),
			BackgroundColor = Fondo,
			Opaque = false,
		};

		// El equivalente del problema de insets de Android, y aca sale gratis: se ancla a las
		// safe area guides y el sistema se encarga de la isla dinamica y de la barra de gestos.
		View.BackgroundColor = Fondo;
		View.AddSubview(web);

		string bundle = NSBundle.MainBundle.BundlePath;
		string html = Path.Combine(bundle, "Assets", "ui.html");
		if (File.Exists(html))
			web.LoadFileUrl(NSUrl.FromFilename(html), NSUrl.FromFilename(Path.GetDirectoryName(html)!));
		else
			Log($"NO se encontro {html} -- revisar el BundleResource de ui.html en el csproj");

		// LAS DOS CARPETAS LAS ELIGE LA PLATAFORMA, igual que en Android.
		string files = Path.Combine(Home(NSSearchPathDirectory.LibraryDirectory), "Application Support");
		string incoming = Path.Combine(Home(NSSearchPathDirectory.DocumentDirectory), "incoming");
		Directory.CreateDirectory(files);

		SeedFromBundle(incoming);

		// EL IDIOMA DE LA PERSONA, RESUELTO UNA VEZ Y AQUI, antes de que la cascara componga su
		// primera frase. Sale del APARATO: no hay selector adentro de Mymi.
		//
		// CurrentUICulture sirve porque este proceso es NATIVO -- la advertencia de
		// Municipalidad.Wasm, que dice de no leer la cultura del proceso, es para wasm, donde
		// esa cultura es la del runtime y no la de quien usa el telefono. Aca no.
		//
		// Y la pagina NO lo resuelve por su cuenta: se lo dice la cascara. Dos resolutores
		// serian dos fuentes de verdad.
		Textos.Idioma = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

		shell = new Shell(
			// ReescribirRutas: true, y es obligatorio aca. El paquete lo arma armar-paquete.ps1
			// con rutas absolutas de Android, que alla se saben de antemano. El contenedor de
			// iOS lleva un GUID que cambia en cada instalacion, asi que la unica ruta valida es
			// la que este proceso acaba de averiguar.
			new Shell.Paths(
				Files: files,
				Incoming: incoming,
				ReescribirRutas: true,
				Instantaneas: SharedContainer(),
				// La foto local es la que lee Intents/GnothiIntents.swift, la intent que vive
				// DENTRO de esta app. No depende del App Group: si SharedContainer() da null, esta
				// se escribe igual.
				FotoLocal: true),
			Log,
			(kind, id, payload) => InvokeOnMainThread(() =>
				web?.EvaluateJavaScript(Shell.PushScript(kind, id, payload), (_, _) => { })),

			// EL MODELO DE ESTE TELEFONO, si de verdad genera. Se pasa como fabrica porque el
			// Shell se comparte con Android, donde estos simbolos no existen: alla el argumento
			// va en null y el proveedor no se ofrece.
			//
			// La comprobacion es Sirve() y no isAvailable, que MIENTE -en iOS 26.2 dice que si y
			// la generacion falla-. Sirve() lo verifica generando, y cachea.
			ModelFoundation.Works() ? encuadre => new ModelFoundation(encuadre) : null);

		// QUIEN SABE CREAR WEBVIEWS. ChooseShell es estatico y no tiene ViewController, asi que
		// la cascara de webview no puede construir el moduleHost: se lo registra esta clase, que
		// si lo tiene. Sin esto, un modulo con "type": "webview" falla diciendo exactamente eso.
		//
		// Es la MISMA linea que MainActivity corre en Android, y con la misma forma: el Host solo
		// nombra la interfaz, la plataforma pone quien la cumple.
		moduleHost = new IosWebModuleHost(this, new IosSecretStore(), Log);
		Gnothi.Host.WebViewShell.Host = moduleHost;

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
			// LA SONDA VA ANTES DEL ARRANQUE, y a proposito: si el Host no levanta porque el
			// AOT no dejo cargar un modulo, la respuesta a POR QUE tiene que estar ya escrita
			// en el log. Un spike que falla sin decir en cual de los cuatro pasos fallo cuesta
			// una tarde.
			Probe.Run(Log);
			BridgeProbe.Run(Log);
			try
			{
				// Run() es el bucle del Host y no termina: se arranca UNA vez y se espera al
				// final. Entre medio se atiende lo que hubiera llegado antes.
				Task corriendo = shell.Run();

				// EL ENLACE QUE LLEGO ANTES DE QUE EL HOST LEVANTARA. Android tiene el mismo
				// pendiente por la misma razon: iOS puede lanzar la app CON el universal link,
				// y ahi ContinueUserActivity corre antes de que esto termine de armarse.
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

	/// La primera vez, si el paquete vino dentro de la app, se siembra Documents/incoming.
	/// Despues no se toca: lo que la persona deje ahi por Archivos manda.
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

	/// EL CONTENEDOR COMPARTIDO CON LA CARATULA, o null si no esta aprovisionado.
	///
	/// Es la unica forma en que el Host de .NET y los App Intents de Swift se pueden pasar algo:
	/// no se llaman, se dejan un archivo. Y funciona porque las dos apps son NUESTRAS -- App
	/// Groups exige el mismo Team ID, que es tambien la razon por la que esto no serviria para
	/// que una institucion de terceros entregue su propia caratula.
	///
	/// DEVUELVE NULL SI FALTA APROVISIONAR, y hay que leer el log para saberlo: el App Group
	/// tiene que estar habilitado en el portal de Apple para los DOS app IDs y venir en el
	/// perfil de aprovisionamiento. Si falta, esto no tira excepcion -- devuelve null en
	/// silencio, que es la clase de falla que cuesta una tarde si no esta dicha en algun lado.
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

	// Sale por el log de Xcode y por `log stream` de la Mac. El equivalente del
	// `adb logcat -s GNOTHI` de Android es:
	//
	//     log stream --predicate 'processImagePath CONTAINS "Gnothi"' --style compact
	// ── ABRIR UN MODULO Y VERLO ──────────────────────────────────────────────────────────
	//
	// El espejo de AbrirModulo/Mostrar/Cerrar de Gnothi.Mobile/MainActivity.cs, y a proposito con
	// los mismos nombres: son las tres piezas que no se pueden compartir, asi que lo unico que
	// puede mantenerlas juntas es que se lean igual.

	// CONTESTA YA Y ABRE DESPUES. Levantar un modulo es hidratar su actor, que tarda; si esto
	// esperara, prompt() del lado de la pagina queda bloqueado y la cara se congela. La pagina
	// recibe "ok" y la vista aparece cuando esta lista.
	private string AbrirModulo(string nombre)
	{
		Gnothi.Host.Module? m = shell?.Find(nombre);
		if (m is null || moduleHost is null) { Log($"abrir: no conozco '{nombre}'"); return "{\"ok\":false}"; }

		_ = Task.Run(async () =>
		{
			try
			{
				// Si el asistente ya le pregunto algo, el modulo esta vivo y abrir es instantaneo:
				// Open lo devuelve tal cual. Esa es la razon por la que P1a se pudo medir sin que
				// esta pantalla existiera.
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

	// EL MODULO OCUPA LA PANTALLA ENTERA, con una FRANJA de Gnothi arriba. Decision de Geaninna,
	// y el patron es el del in-app browser: el anfitrion se queda un renglon y le da el resto.
	//
	// POR QUE LA FRANJA Y NO UN BOTON FLOTANTE. Medido el 2026-09-07: un '‹ Gnothi' encima de un
	// modulo full-bleed le tapaba el logo y la primera letra de "Supermercado La Guaria". Abajo
	// pasa lo mismo con su nav. Un modulo que ocupa todo no deja lugar para cromo ajeno, asi que
	// el cromo tiene que ocupar su propio espacio y correr al modulo hacia abajo.
	//
	// POR QUE LA FRANJA Y NO UN SHEET CON GRABBER, que fue la version anterior. El sheet INSINUA
	// que estas dentro de Gnothi -la app queda asomando detras-; la franja lo DICE. Y ese es el
	// argumento entero: esta es la app de la institucion, corriendo bajo Gnothi, leyendo un
	// registro que vive en el telefono. Ademas un boton con etiqueta le gana a un gesto: el
	// grabber es estandar pero hay que descubrirlo, y "Listo" no.
	//
	// LO QUE NO SE COPIA DEL PATRON ES LA URL. Un in-app browser muestra el origen porque el
	// contenido vino de la red y ahi el origen es lo auditable. Aca lo sirve Gnothi desde su
	// propio contenedor y el origen es gnothi-mod://<modulo>, que no le dice nada a la persona
	// -y mostrarlo seria peor que no mostrarlo, porque parece una direccion y no lo es. Va el
	// NOMBRE de la institucion. Se toma el cromo del patron, no su semantica.
	//
	// [PENDIENTE DE GEANINNA] Esta franja es la MINIMA a proposito, para que ella la ajuste sobre
	// algo que ya corre. Lo que hay que cuidar es el peso: el modulo YA trae su propio header y
	// su propia nav, asi que esto son dos encabezados apilados. Si engorda, la app de la
	// institucion parece encajonada. Alto, tipografia y si va o no el subtitulo son de ella.
	//
	// [DIVERGENCIA DELIBERADA CON ANDROID] Alla no hay franja porque el boton atras del sistema
	// alcanza. Este punto se VE distinto en las dos plataformas a proposito; esta anotado en
	// PRUEBAS-WEBVIEW-IOS.md para que nadie lo reporte como bug.
	private void Mostrar(IosWebModule vivo, string nombre)
	{
		UIView? vista = vivo.View;
		if (vista is null) return;

		Cerrar();

		enPantalla = vivo;
		enPantallaNombre = nombre;
		// Visible es del codigo compartido y NO es cosmetico: WebModuleHost.Evict se niega a
		// desalojar un modulo que la persona esta mirando.
		vivo.Visible = true;

		// UN VIEW CONTROLLER PROPIO Y NO la vista suelta: presentar exige un controller, y de paso
		// es quien le da al WKWebView del modulo un ciclo de vida donde apoyarse.
		var pantalla = new UIViewController();
		UIView raiz = pantalla.View!;
		raiz.BackgroundColor = Fondo;

		var franja = new UIView { BackgroundColor = Fondo, TranslatesAutoresizingMaskIntoConstraints = false };

		// El nombre de la institucion, no el del modulo: 'compras' es la llave tecnica y
		// "Mis compras" es lo que la persona reconoce.
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

			// EL MODULO ARRANCA DEBAJO DE LA FRANJA, que es lo que evita la superposicion por
			// construccion y no por elegir bien las coordenadas.
			vista.TopAnchor.ConstraintEqualTo(franja.BottomAnchor),
			vista.LeadingAnchor.ConstraintEqualTo(raiz.LeadingAnchor),
			vista.TrailingAnchor.ConstraintEqualTo(raiz.TrailingAnchor),
			vista.BottomAnchor.ConstraintEqualTo(raiz.BottomAnchor),
		]);

		// FullScreen y no PageSheet: con la franja diciendo donde estas, dejar a Gnothi asomando
		// atras no agrega y le come pantalla al modulo.
		pantalla.ModalPresentationStyle = UIModalPresentationStyle.FullScreen;

		// EL DELEGADO SE QUEDA aunque en FullScreen no haya descarte por gesto. Cuesta nada y
		// protege lo que sigue: si alguien agrega otro camino de cierre y olvida limpiar, el
		// modulo queda Visible para siempre y WebModuleHost.Evict no lo desaloja nunca mas.
		pantalla.PresentationController!.Delegate = new AlDescartar(this);

		enPantallaVc = pantalla;
		PresentViewController(pantalla, animated: true, completionHandler: null);

		Log($"abrir: '{nombre}' en pantalla");
	}

	// Sacarlo de la pantalla NO lo mata: el modulo sigue vivo y contestando preguntas del
	// asistente. Solo deja de verse, y vuelve a ser desalojable.
	private bool Cerrar()
	{
		if (enPantalla is null) return false;

		if (enPantallaVc is { } vc && PresentedViewController == vc)
			DismissViewController(animated: true, completionHandler: null);

		Soltar();
		return true;
	}

	// Lo que hay que deshacer pase lo que pase, venga de Cerrar() o del gesto. La vista del modulo
	// se saca de la jerarquia del sheet para que un Mostrar() posterior la pueda volver a poner:
	// el WKWebView es del modulo y le sobrevive a la pantalla.
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

	// El gesto de arrastrar el sheet hacia abajo no pasa por Cerrar(): llega por aca.
	private sealed class AlDescartar : UIAdaptivePresentationControllerDelegate
	{
		private readonly ViewController host;
		public AlDescartar(ViewController host) => this.host = host;

		public override void DidDismiss(UIPresentationController presentationController) => host.Soltar();
	}

	private static void Log(string s) => Console.WriteLine($"GNOTHI: {s}");
	private string? pendingEnrollment;

	// ── EL ALTA DESDE UN ARCHIVO, la mitad que SI es de iOS ────────────────────────────────
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

	// ── el puente, la mitad que SI es de iOS ────────────────────────────────────────────
	//
	// La pagina llama window.prompt('gnothi', json), igual que en Android. Lo unico que hace
	// este tipo es TRADUCIR: saca el json y entrega la respuesta por el completionHandler.
	// Que se hace con el pedido es de Shell.Handle, y es el mismo archivo en las dos.
	//
	// UNA DIFERENCIA QUE NO SE NOTA DESDE LA PAGINA: aca el lado nativo es asincronico -- se
	// contesta llamando un handler, no retornando. Del lado del javascript prompt() bloquea
	// igual hasta que ese handler se llame, asi que ui.html no se entera ni cambia.
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
				// Un prompt de verdad. Se cancela: esta app no tiene ninguno.
				completionHandler(null);
				return;
			}

			// CASI TODO lo que la pagina pide lo atiende Shell.Handle, que es el MISMO archivo en
			// las dos plataformas -- los dos verbos del alta incluidos, desde que la propuesta
			// vive alla.
			//
			// LA UNICA OP QUE SE INTERCEPTA ACA ES 'abrir', y por la misma razon que en Android
			// (MainActivity.cs): poner una vista en pantalla es lo unico de todo el protocolo que
			// NO se puede compartir, porque UIView y android.view.View no tienen nada en comun.
			// Shell.Handle no la conoce a proposito -- si la conociera tendria que saber de
			// pantallas- y por eso caia en su `_ => "{}"`: el boton llegaba al Host y el Host no
			// tenia que hacer con el. Medido el 2026-09-07: se veia `bridge: abrir` en el log,
			// que se escribe ANTES del switch, y despues nada.
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
