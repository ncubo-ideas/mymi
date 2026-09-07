using Foundation;
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
	private IosWebModuleHost? anfitrion;

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

		SembrarDesdeElBundle(incoming);

		shell = new Shell(
			// ReescribirRutas: true, y es obligatorio aca. El paquete lo arma armar-paquete.ps1
			// con rutas absolutas de Android, que alla se saben de antemano. El contenedor de
			// iOS lleva un GUID que cambia en cada instalacion, asi que la unica ruta valida es
			// la que este proceso acaba de averiguar.
			new Shell.Paths(
				Files: files,
				Incoming: incoming,
				ReescribirRutas: true,
				Instantaneas: Compartido(),
				// La foto local es la que lee Intents/GnothiIntents.swift, la intent que vive
				// DENTRO de esta app. No depende del App Group: si Compartido() da null, esta
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
			ModelFoundation.Sirve() ? encuadre => new ModelFoundation(encuadre) : null);

		// QUIEN SABE CREAR WEBVIEWS. ChooseShell es estatico y no tiene ViewController, asi que
		// la cascara de webview no puede construir el anfitrion: se lo registra esta clase, que
		// si lo tiene. Sin esto, un modulo con "type": "webview" falla diciendo exactamente eso.
		//
		// Es la MISMA linea que MainActivity corre en Android, y con la misma forma: el Host solo
		// nombra la interfaz, la plataforma pone quien la cumple.
		anfitrion = new IosWebModuleHost(this, new IosSecretStore(), Log);
		Gnothi.Host.WebViewShell.Host = anfitrion;

		_ = Task.Run(async () =>
		{
			// LA SONDA VA ANTES DEL ARRANQUE, y a proposito: si el Host no levanta porque el
			// AOT no dejo cargar un modulo, la respuesta a POR QUE tiene que estar ya escrita
			// en el log. Un spike que falla sin decir en cual de los cuatro pasos fallo cuesta
			// una tarde.
			Sonda.Correr(Log);
			SondaPuente.Correr(Log);
			try
			{
				// Run() es el bucle del Host y no termina: se arranca UNA vez y se espera al
				// final. Entre medio se atiende lo que hubiera llegado antes.
				Task corriendo = shell.Run();

				// EL ENLACE QUE LLEGO ANTES DE QUE EL HOST LEVANTARA. Android tiene el mismo
				// pendiente por la misma razon: iOS puede lanzar la app CON el universal link,
				// y ahi ContinueUserActivity corre antes de que esto termine de armarse.
				if (altaPendiente is { Length: > 0 } p) { altaPendiente = null; await shell.Enroll(p); }

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
	private void SembrarDesdeElBundle(string incoming)
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
	private static string? Compartido()
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
	private static void Log(string s) => Console.WriteLine($"GNOTHI: {s}");
	private string? altaPendiente;

	/// LO QUE LLEGA DEL UNIVERSAL LINK. Si el Host todavia no levanto se guarda y se atiende
	/// cuando termine -- igual que altaPendiente en Android.
	internal void AtenderEnlace(string url)
	{
		if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? donde)) { Log($"alta: enlace ilegible: {url}"); return; }

		// El bundle viaja como parametro, no como path: la institucion sirve su zip donde
		// quiera y go.gnothi.app solo lo transporta.
		string bundle = System.Web.HttpUtility.ParseQueryString(donde.Query)["bundle"] ?? "";
		if (bundle.Length == 0) { Log($"alta: el enlace no trae ?bundle= : {url}"); return; }

		if (shell is null) { altaPendiente = bundle; Log("alta: el Host todavia no levanto; queda pendiente"); return; }

		_ = shell.Enroll(bundle);
	}

	private void Empujar(string kind, string id, string payload) => InvokeOnMainThread(() =>
		web?.EvaluateJavaScript(Shell.PushScript(kind, id, payload), (_, _) => { }));

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

			// TODO lo que la pagina pide lo atiende Shell.Handle, que es el MISMO archivo en las
			// dos plataformas -- incluidos los dos verbos del alta, desde que la propuesta vive
			// alla. iOS no intercepta nada: no tiene pantalla de modulo todavia.
			completionHandler(host.shell?.Handle(defaultText) ?? "{}");
		}
	}
}
