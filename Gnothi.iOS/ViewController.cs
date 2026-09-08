using Foundation;
using Gnothi.Assistant;
using Gnothi.Movil;
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

	private WKWebView? web;
	private Shell? shell;
	private IosWebModuleHost? moduleHost;

	// EL MODULO QUE LA PERSONA ESTA MIRANDO, y el boton para salir de el. Los dos son de esta
	// clase y no del Shell: el Shell no sabe que existe una pantalla, y esa es la linea que
	// permite que el mismo Host corra en escritorio sin arrastrar UIKit.
	private IosWebModule? enPantalla;
	private string? enPantallaNombre;
	private UIViewController? enPantallaVc;

	public override void ViewDidLoad()
	{
		base.ViewDidLoad();

		var config = new WKWebViewConfiguration();
		// LA MARCA QUE LA PAGINA BUSCA. ui.html hace navigator.userAgent.includes('GnothiHost')
		// y si no la ve NUNCA llama prompt() -- en un browser comun ese dialogo es modal y
		// congela la pagina.
		//
		// ApplicationNameForUserAgent y NO CustomUserAgent: el primero AGREGA al user agent por
		// omision, el segundo lo REEMPLAZA entero. Con el segundo la marca queda igual pero se
		// pierde todo lo demas, y eso rompe cualquier otra deteccion de la pagina.
		config.ApplicationNameForUserAgent = "GnothiHost";

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

	// EL MODULO SE PRESENTA COMO SHEET, y la decision es de Geaninna. La alternativa medida era
	// una barra de Gnothi arriba con el modulo debajo; se descarto porque el sheet gana dos cosas
	// que la barra no puede dar.
	//
	//   1. NO LE TAPA NADA A LA INSTITUCION. Un boton flotante sobre un modulo full-bleed le pisa
	//      la marca: medido el 2026-09-07, el `‹ Gnothi` que tenia esta funcion antes quedaba
	//      encima del logo y de la primera letra de "Supermercado La Guaria". Abajo pasa lo mismo
	//      con su nav. El sheet le entrega el area entera y no le pone cromo arriba.
	//
	//   2. LA SALIDA ES EL GESTO QUE LA PERSONA YA SABE. Android no necesita nada porque tiene el
	//      boton atras del sistema; iOS no tiene equivalente -el gesto de borde lo consume el
	//      contenido del modulo- asi que la vuelta hay que darla. El grabber de un sheet es la
	//      respuesta nativa: se arrastra hacia abajo y se cierra, sin que nadie lo explique.
	//
	// Y GNOTHI QUEDA ASOMANDO DETRAS, que no es un efecto lateral: se ve de quien es la app en la
	// que estas parado. Para el argumento de Gnothi eso no es decoracion.
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
		pantalla.View!.BackgroundColor = UIColor.SystemBackground;
		vista.Frame = pantalla.View.Bounds;
		vista.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
		pantalla.View.AddSubview(vista);

		pantalla.ModalPresentationStyle = UIModalPresentationStyle.PageSheet;

		// UN SOLO DETENT, el grande. Con medium tambien disponible, el sheet se puede quedar a
		// media altura y la app de la institucion -que es una pagina entera- queda ilegible; el
		// grabber sigue estando igual, que es lo que hacia falta.
		if (pantalla.SheetPresentationController is { } hoja)
		{
			hoja.Detents = [UISheetPresentationControllerDetent.CreateLargeDetent()];
			hoja.PrefersGrabberVisible = true;
		}

		// EL DESCARTE POR GESTO TAMBIEN TIENE QUE LIMPIAR. Si solo se limpiara en Cerrar(), la
		// persona arrastra el sheet hacia abajo y el modulo queda marcado como visible para
		// siempre: WebModuleHost.Evict no lo desalojaria nunca mas, y Gnothi se quedaria con su
		// motor de 20 MB en memoria sin nadie mirandolo.
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

	/// WHAT ARRIVES FROM THE UNIVERSAL LINK. If the Host has not come up yet this is kept and
	/// attended when it does -- the same as altaPendiente on Android.
	internal void HandleLink(string url)
	{
		if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? where)) { Log($"alta: enlace ilegible: {url}"); return; }

		// The bundle travels as a QUERY PARAMETER, not as a path: the institution serves its
		// zip wherever it likes and go.gnothi.app only carries it.
		string bundle = System.Web.HttpUtility.ParseQueryString(where.Query)["bundle"] ?? "";
		if (bundle.Length == 0) { Log($"alta: el enlace no trae ?bundle= : {url}"); return; }

		if (shell is null) { pendingEnrollment = bundle; Log("alta: el Host todavia no levanto; queda pendiente"); return; }

		_ = shell.Enroll(bundle);
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
			string respuesta;
			try
			{
				System.Text.Json.JsonElement req = Json.Parse(defaultText);
				respuesta = req.GetProperty("op").GetString() == "abrir"
					? host.AbrirModulo(req.GetProperty("module").GetString() ?? "")
					: host.shell?.Handle(defaultText) ?? "{}";
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
