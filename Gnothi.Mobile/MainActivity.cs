using Android.App;
using Android.OS;
using Android.Webkit;
using Gnothi.Movil;
using System.Text.Json;

// QUE PUEDE HABLAR GNOTHI EN CLARO: solo el loopback, que es donde aterriza un tunel de
// desarrollo. Todo lo demas exige HTTPS, que es lo que Android impone desde API 28 y que el
// cartero cumple. Ver Resources/xml/network_security_config.xml.
[assembly: Android.App.Application(NetworkSecurityConfig = "@xml/network_security_config")]

// THE CAMERA, for reading the institution's code from inside Gnothi. The page asks for it
// through getUserMedia, and the WebView will not even ask the person unless the app declares
// the permission. Not required: a phone without a camera pastes the link instead.
// LA RED DEL HOST, DECLARADA A MANO, y hay que declararla aunque "ya funcionaba".
//
// El SDK de .NET Android agrega INTERNET SOLO EN DEBUG, para que el depurador pueda conectarse.
// En Release no lo agrega, y entonces el cartero -- que es el unico que habla con publicadores --
// se queda sin red en el build que de verdad se entrega.
//
// COMO SE VE CUANDO FALTA, medido el 2026-09-11 en un Xiaomi con Android 16: el pedido a
// 127.0.0.1 muere con `HttpRequestException: Permission denied (127.0.0.1:8095)`. Loopback y
// todo, Android exige INTERNET para abrir CUALQUIER socket. Y el mensaje se lee como una
// restriccion del fabricante -- MIUI tiene fama de cortarle la red a las apps -- asi que se
// buscan permisos de runtime que no existen: INTERNET es un permiso normal y no sale en la
// pantalla de permisos de la app. El unico lugar donde se ve es `dumpsys package`.
[assembly: Android.App.UsesPermission(Android.Manifest.Permission.Internet)]
[assembly: Android.App.UsesPermission(Android.Manifest.Permission.Camera)]
[assembly: Android.App.UsesFeature("android.hardware.camera", Required = false)]

namespace Gnothi.Mobile;

// EL HOST DE GNOTHI EN UN TELEFONO, con cara. LA MITAD DE ANDROID Y NADA MAS.
//
// La cara es UN html en Assets, renderizado por un WebView. Por que no Blazor/MAUI, que era la
// sugerencia natural: Blazor de verdad en Android necesita el workload de MAUI, que se instala
// en Program Files -administrador en esta maquina- y reescribe el proyecto. El WebView da lo
// que se le pedia (UI de calidad web, y el markdown del modelo con tablas de verdad, que un
// TextView nativo no puede) mientras el codigo del Host queda intacto.
//
// Y esa decision se pago sola el dia de iOS: como la cara es un HTML, es EL MISMO archivo alla.
// Lo unico que cambia entre plataformas es quien monta el WebView y como se traduce
// window.prompt(). Todo lo demas -el estado, el consentimiento, la llave, el proveedor, la
// pregunta- vive en Gnothi.Movil/Shell.cs y se comparte tal cual.
//
// WHAT THIS FILE OWNS: the screen and the lifecycle (installing what was downloaded). The
// catalog, the consent and the dispatch are the Host's -- the same files the desktop runs.
// EL ALTA DESDE LA PAGINA DE LA INSTITUCION. En produccion el dominio ademas sirve un
// assetlinks.json con la huella de firma, y con eso Android abre Gnothi sin dialogo de
// desambiguacion (especificacion 13 §7).
//
// AutoVerify NO ES OPCIONAL, y no solo para produccion. Sin el, Android ni siquiera registra
// el dominio como candidato: `pm get-app-links` no devuelve NADA, y Chrome -- que solo entrega
// un https a una app cuando esa app es un handler verificado -- navega el mismo y muestra
// DNS_PROBE_FINISHED_NXDOMAIN. El boton de la institucion no hace nada. Medido el 2026-09-04.
//
// En una prueba local el dominio no se puede verificar de verdad (haria falta servir
// assetlinks.json desde go.gnothi.app con la huella de firma), pero CON AutoVerify se puede
// aprobar a mano, que es lo que hace escenario-webview.ps1:
//
//     adb shell pm set-app-links-user-selection --user 0 --package <paquete> true go.gnothi.app
[IntentFilter(new[] { Android.Content.Intent.ActionView },
	Categories = new[] { Android.Content.Intent.CategoryDefault, Android.Content.Intent.CategoryBrowsable },
	DataScheme = "https", DataHost = "go.gnothi.app", DataPathPrefix = "/alta",
	AutoVerify = true)]
// EL ENLACE DE VUELTA de la instalacion suelta: trae las aserciones de Sonia (13 §6.1).
[IntentFilter(new[] { Android.Content.Intent.ActionView },
	Categories = new[] { Android.Content.Intent.CategoryDefault, Android.Content.Intent.CategoryBrowsable },
	DataScheme = "https", DataHost = "go.gnothi.app", DataPathPrefix = "/abrir",
	AutoVerify = true)]
// LA HOJA DE COMPARTIR, para cuando las aserciones no caben en un enlace. text/plain y no
// application/json porque Web Share rechaza JSON: su lista de tipos permitidos es cerrada.
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
public class MainActivity : Activity
{
	private WebView? web;

	// La raiz de la pantalla, para poder poner el modulo encima; el anfitrion, para pedirselo;
	// y cual esta a la vista, que es lo que el boton atras tiene que cerrar primero.
	private Android.Widget.FrameLayout? raiz;
	private AndroidWebModuleHost? anfitrion;
	private AndroidWebModule? enPantalla;
	private Shell? shell;
	private readonly CancellationTokenSource stop = new();

	protected override void OnCreate(Bundle? savedInstanceState)
	{
		base.OnCreate(savedInstanceState);

		// Si Gnothi se abrio POR un enlace de alta, el enlace ENTERO va a la cascara -con su
		// sello, si trae- y ella lo desarma. Antes viajaba solo ?bundle=, y el sello se perdia.
		altaPendiente = Intent?.Data?.GetQueryParameter("bundle") is { Length: > 0 } ? Intent!.Data!.ToString() : null;
		// Or by a file -opened, shared- which is read now and enrolled once the Host is up. A
		// share of text is the other thing a share can be: the handoff from the institution's
		// standalone app, read below.
		archivoPendiente = LeerArchivoDeModulo(Intent, strict: true);
		if (archivoPendiente is null) LeerTraspasoDe(Intent);
		// Y el icono de la pantalla de inicio, que pide abrir un modulo por su nombre.
		LeerAperturaDe(Intent);

		web = new WebView(this);
		web.Settings.JavaScriptEnabled = true;
		// The camera preview is a <video> the page plays as soon as the stream arrives; without
		// this the WebView would wait for a gesture that already happened, on the button.
		web.Settings.MediaPlaybackRequiresUserGesture = false;
		// La pagina detecta la app por esta marca y NUNCA llama prompt() fuera de ella: en un
		// browser comun ese dialogo es modal y congela la pagina -- medido en el panel de
		// vista previa.
		web.Settings.UserAgentString += " GnothiHost";
		web.SetBackgroundColor(Android.Graphics.Color.ParseColor("#F5F3FB"));
		web.SetWebChromeClient(new Bridge(this));

		// targetSdk 36 hace el edge-to-edge OBLIGATORIO: la pagina se extenderia debajo de la
		// barra de estado y de la de gestos, y la navegacion de abajo termina bajo la del
		// sistema. El arreglo es nativo y en un solo lugar -padding por las barras del
		// sistema- PERO el padding va en un CONTENEDOR, no en el WebView: un WebView ignora su
		// propio padding, lo cual costo una captura de pantalla descubrirlo.
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

		// LAS DOS CARPETAS LAS ELIGE LA PLATAFORMA, no la cascara. adb no puede escribir en el
		// area privada, y eso es correcto: nadie de afuera instala un modulo.
		shell = new Shell(
			new Shell.Paths(
				Files: FilesDir!.AbsolutePath,
				Incoming: System.IO.Path.Combine(GetExternalFilesDir(null)!.AbsolutePath, "incoming")),
			Log,
			(kind, id, payload) => RunOnUiThread(() =>
				web?.EvaluateJavascript(Shell.PushScript(kind, id, payload), null)));

		_ = Task.Run(Start);
	}


	// EL ARRANQUE. Lo que quedo aca despues de que el refactor de master mudara el nucleo a
	// Gnothi.Movil/Shell.cs: alla viven instalar, descubrir, el dispatcher, el vigilante del
	// consentimiento y la comprobacion de los modulos nuevos. Aca solo lo que sabe de Android,
	// y lo que tiene que pasar DESPUES de que el Host levante.
	private async Task Start()
	{
		if (shell is null) return;

		// QUIEN SABE CREAR WEBVIEWS. ChooseShell es estatico y no tiene Context, asi que la
		// cascara de webview no puede construir el anfitrion: se lo registra la Activity, que
		// si lo tiene. Sin esto, un modulo con "type": "webview" falla diciendo exactamente eso.
		anfitrion = new AndroidWebModuleHost(
			this, new AndroidSecretStore(FilesDir!.AbsolutePath),
			s => global::Android.Util.Log.Info("gnothi", s));
		Gnothi.Host.WebViewShell.Host = anfitrion;

		// Y QUIEN SABE CERRARLOS: una actualizacion mueve la carpeta del modulo mientras su
		// WebView puede estar encima (44 §3.6). El Shell solo nombra el gancho; aca se cumple.
		shell.CloseModule = modulo =>
		{
			if (!anfitrion.IsLive(modulo)) return false;
			anfitrion.Evict(modulo);
			return true;
		};

		// Run() es el bucle del Host y no termina: se arranca UNA vez y se espera al final.
		Task corriendo = shell.Run();

		// Si la app se abrio POR un enlace, recien ahora hay a donde instalar o a quien
		// entregarle. Es la misma forma que ViewController usa en iOS.
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

		// EL ICONO VA AL FINAL, y no es un detalle de orden: si en el mismo arranque llegó
		// además un alta o un traspaso, primero tiene que quedar instalado y asentado lo que
		// vino -- abrir la cara antes sería mostrarle a la persona el expediente viejo.
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

	// EL BOTON ATRAS le pregunta a la pagina primero: una hoja se cierra, un detalle vuelve al
	// inicio. Solo cuando la pagina contesta false -ya esta en el inicio- la app se hace a un
	// lado, al FONDO y no a la muerte: terminar tiraria el catalogo cargado sin razon.
	//
	// OnBackPressed is the deprecated path, and it is chosen on purpose: the modern
	// OnBackInvokedCallback only takes over when the manifest opts in, which this app does not.
	// ATRAS: primero el modulo en pantalla, despues la pagina de Gnothi. El orden importa:
	// si el modulo esta tapando todo, el boton tiene que sacarlo a el, no navegar por debajo.
	// El enlace de alta con la app YA abierta.
	protected override void OnNewIntent(Android.Content.Intent? intento)
	{
		base.OnNewIntent(intento);

		// EL INTENT NUEVO ES EL QUE MANDA. Sin esto, `Intent` sigue devolviendo el que abrió la
		// actividad, y un segundo enlace -otra institución, con Gnothi ya abierto- se pierde.
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

		// El icono con Gnothi ya abierta. Acá el catálogo YA está, así que se abre de una.
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

	// ── EL TRASPASO QUE LLEGA DE LA INSTALACIÓN SUELTA ───────────────────────────────────

	// Lo que trajo el enlace o la hoja de compartir, esperando a que haya catálogo.
	private (string? Modulo, string Aserciones)? traspasoPendiente;

	// DOS PUERTAS PARA LO MISMO (especificación 13 §5.2 y §6.1):
	//
	//   el enlace     go.gnothi.app/abrir?modulo=…&aserciones=…   cuando son pocas y caben en la URL
	//   compartir     ACTION_SEND con un archivo de texto          cuando no caben
	//
	// Hoy en Exámenes las aserciones de Sonia son UNA —el sexo que declaró— y entran en el
	// enlace de sobra. La segunda puerta existe porque otras instituciones van a esperar más.
	private void LeerTraspasoDe(Android.Content.Intent? intento)
	{
		if (intento is null) return;

		// Puerta 1: el enlace de vuelta que armó la app suelta.
		string? porEnlace = intento.Data?.GetQueryParameter("aserciones");
		if (porEnlace is { Length: > 0 })
		{
			traspasoPendiente = (intento.Data?.GetQueryParameter("modulo"), porEnlace);
			Log($"traspaso: llegó por enlace, {porEnlace.Length} bytes");
			return;
		}

		// Puerta 2: la hoja de compartir. El archivo viaja como text/plain y NO como
		// application/json, porque Web Share rechaza JSON: su lista de tipos permitidos es
		// cerrada y no lo incluye. El nombre .txt es envoltorio; lo que importa es el contenido.
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
		// Un share puede traer el archivo o, si era chico, el texto pelado. Se aceptan los dos.
		if (intento.GetParcelableExtra(Android.Content.Intent.ExtraStream) is Android.Net.Uri archivo)
		{
			using System.IO.Stream? cuerpo = ContentResolver!.OpenInputStream(archivo);
			if (cuerpo is null) return null;

			using var lector = new System.IO.StreamReader(cuerpo);
			return lector.ReadToEnd();
		}

		return intento.GetStringExtra(Android.Content.Intent.ExtraText);
	}

	// A QUÉ MÓDULO LE CORRESPONDE. El enlace lo dice; la hoja de compartir no, así que se
	// deduce del publicador que las aserciones declaran. Si aun así no se puede saber, se dice:
	// asentar las aserciones de una persona en el módulo equivocado sería peor que no asentarlas.
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

	// Lo que trajo el enlace de alta, esperando a que el catálogo exista. No se puede instalar
	// antes: el Host todavía no sabe dónde viven los módulos.
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

	// ── EL MÓDULO EN PANTALLA ────────────────────────────────────────────────────────────

	// LA PWA DE LA INSTITUCIÓN SE DIBUJA A SÍ MISMA (especificación 11 §8). Sonia abre el
	// módulo y ve el tablero del laboratorio: su marca, su Blazor, sus gráficos. Gnothi no lo
	// describe ni lo pinta.
	//
	// Y ES EL MISMO WebView que contesta las preguntas del asistente. No hay dos: uno solo,
	// que a veces está en pantalla y a veces solo responde. Por eso abrirlo es rápido si ya
	// estaba vivo.
	//
	// LA REGLA QUE CAMBIA AL MOSTRARLO: mientras esté en pantalla NO se puede desalojar,
	// aunque cueste sus 200 MB. Esa es la única concesión; en cuanto ella sale, vuelve a ser
	// desalojable.
	//
	// Lo que NO cambia es la contención: sigue sin red, sin service worker y sin proceso
	// propio, y deja de existir cuando Gnothi lo desaloja. "No despierta, no notifica, no
	// interrumpe" se sostiene por incapacidad, no porque el módulo se porte bien.
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

	// El WebView del módulo entra ARRIBA del de Gnothi, tapándolo. No se reemplaza la vista:
	// la de Gnothi sigue viva detrás, con su estado y su conversación intactos.
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

	// Sacarlo de la pantalla NO lo mata: el módulo sigue vivo y contestando preguntas del
	// asistente. Solo deja de verse, y vuelve a ser desalojable.
	private bool Cerrar()
	{
		if (enPantalla is null) return false;

		Android.Views.View? vista = enPantalla.View;
		if (vista is not null) raiz?.RemoveView(vista);

		enPantalla.Visible = false;
		enPantalla = null;
		return true;
	}

	// ── EL ACCESO DIRECTO EN LA PANTALLA DE INICIO ──────────────────────────────────────
	//
	// UN ICONO QUE PARECE LA APP DE LA INSTITUCIÓN Y ABRE GNOTHI. Es lo más cerca que se puede
	// estar de «su app» sin que la institución publique una: el icono lleva su logo y su nombre,
	// y al tocarlo Gnothi levanta su módulo. El proceso, la carpeta y el motor siguen siendo de
	// Gnothi, y eso no se disimula -- el launcher le estampa el badge de Gnothi en la esquina y
	// no hay forma de quitarlo. Está bien que esté: dice quién corre el código.
	//
	// NO ES UNA APP, Y LA DIFERENCIA IMPORTA: no aparece en el cajón de aplicaciones -- ahí solo
	// van paquetes instalados --, no tiene proceso propio, no despierta, no notifica, y si Gnothi
	// se desinstala el icono deja de servir. Es exactamente la contención que ya tiene el módulo,
	// con una puerta más corta.
	//
	// EL INTENT ES EXPLÍCITO Y NO UN ENLACE. `go.gnothi.app/abrir?modulo=…` existe y sería lo
	// natural, pero un https sin assetlinks.json verificado saca el diálogo de «¿con qué app
	// abro esto?», depende de DNS, y cualquiera puede registrar un handler para ese dominio.
	// Un intent contra esta clase no sale del teléfono ni pasa por el resolvedor.
	internal const string ExtraModulo = "gnothi.modulo";

	// El módulo que el icono pidió, esperando a que haya catálogo -- igual que el alta y el
	// traspaso. Tocar el icono con Gnothi cerrado llega a OnCreate ANTES de que el Host esté
	// arriba, y abrirlo ahí no encontraría nada que abrir.
	private string? aperturaPendiente;

	private void LeerAperturaDe(Android.Content.Intent? intento)
	{
		string? modulo = intento?.GetStringExtra(ExtraModulo);
		if (modulo is { Length: > 0 }) aperturaPendiente = modulo;
	}

	// LO QUE LA PÁGINA PIDE. Contesta con `ok` y, cuando no se pudo, con la razón EN PALABRAS:
	// un botón que no hace nada y no dice por qué es peor que no tener el botón.
	private string FijarAcceso(string nombre)
	{
		Gnothi.Host.Module? m = shell?.Find(nombre);
		if (m is null) return Respuesta(false, "no encuentro ese módulo");

		// API 26, y la guarda se escribe con OperatingSystem y no con Build.VERSION.SdkInt: el
		// analizador de compatibilidad entiende esta forma y no la otra, así que con la otra
		// quedaba un CA1416 en cada línea de acá abajo. Por debajo de 26 existía el broadcast
		// com.android.launcher.action.INSTALL_SHORTCUT, que Android 8 dejó de atender: no hay a
		// qué caer. El mínimo de Gnothi es 24, así que esto pasa de verdad.
		if (!OperatingSystem.IsAndroidVersionAtLeast(26))
			return Respuesta(false, "este Android no sabe poner accesos directos");

		var gestor = (Android.Content.PM.ShortcutManager?)GetSystemService(ShortcutService);
		if (gestor is null) return Respuesta(false, "este teléfono no ofrece accesos directos");

		// El launcher puede no saber recibirlos -- y en MIUI además hay un permiso propio, que
		// esto NO puede pedir: si está apagado, el pin se acepta y el icono nunca aparece.
		if (!gestor.IsRequestPinShortcutSupported)
			return Respuesta(false, "la pantalla de inicio de este teléfono no acepta accesos directos");

		try
		{
			var abrir = new Android.Content.Intent(this, typeof(MainActivity));
			abrir.SetAction(Android.Content.Intent.ActionView);       // un shortcut SIN acción se rechaza
			abrir.PutExtra(ExtraModulo, m.Name);
			abrir.AddFlags(Android.Content.ActivityFlags.NewTask | Android.Content.ActivityFlags.SingleTop);

			// EL RÓTULO ES EL NOMBRE DE LA INSTITUCIÓN, en los DOS campos. El largo empezó siendo
			// el título del módulo -«Tus exámenes, al día»- que es lo que uno escribiría, y en el
			// teléfono el icono salió diciendo «Tus examenes,…»: MIUI prefiere el largo y lo corta
			// a la mitad. Un icono lleva un NOMBRE, no un eslogan, y los dos campos tienen que
			// poder leerse cortados.
			string rotulo = Primero(m.Manifest.Identity, m.Manifest.Institution, m.Manifest.Title, m.Name);

			var info = new Android.Content.PM.ShortcutInfo.Builder(this, "modulo:" + m.Name)
				.SetShortLabel(new Java.Lang.String(rotulo))!
				.SetLongLabel(new Java.Lang.String(rotulo))!
				.SetIntent(abrir)!;

			if (IconoDe(m) is { } icono) info.SetIcon(icono);

			Android.Content.PM.ShortcutInfo hecho = info.Build()!;

			// PRIMERO ACTUALIZAR Y DESPUÉS PEDIR. Si ya está fijado, `RequestPinShortcut` no le
			// cambia nada: el icono viejo se queda con el rótulo y el logo del día que se puso.
			// `UpdateShortcuts` sí alcanza a los fijados, y es lo que hace que una institución que
			// cambia de nombre o de logo en una versión nueva se vea en la pantalla de inicio. No
			// hace nada si no había ninguno, así que no hace falta preguntar.
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

	// EL ICONO SE COMPONE, no se usa el logo tal cual. Un icono adaptativo recorta todo lo que
	// queda fuera del 66% central -- círculo, cuadrado redondeado, lo que el launcher decida --
	// así que un logo puesto a sangre pierde los bordes. Se dibuja sobre el color de la marca y
	// al 52% del lienzo, que entra entero en cualquier máscara.
	//
	// Devuelve null -- y el launcher pone el icono de Gnothi -- cuando el logo es un SVG: Android
	// no los decodifica, y la aduana del Host sí los admite.
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

	// El color que la institución declaró, y el navy del Host cuando no declaró ninguno. El
	// fondo del icono NO puede ser transparente: en una máscara circular se vería el recorte.
	private static Android.Graphics.Color ColorDeMarca(Gnothi.Host.Module m)
	{
		string declarado = Gnothi.Host.Module.LookOf(m.Manifest, m.Directory, out _).Color;
		try { if (declarado.Length > 0) return Android.Graphics.Color.ParseColor(declarado); }
		catch (Exception) { /* un color que no parsea es como no haberlo declarado */ }
		return Android.Graphics.Color.ParseColor("#241F3D");
	}

	// ── LA CÁMARA, PARA LEER EL CÓDIGO ──────────────────────────────────────────────────

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
	// Lo unico que hace este tipo es TRADUCIR: saca el json del dialogo y entrega la respuesta
	// por result.Confirm. Que se hace con el pedido es de Shell.Handle, y es identico en iOS
	// -- alla el mismo json llega por WKUIDelegate y la respuesta sale por un completionHandler.
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

		// LO QUE LA PAGINA PIDE lo atiende Shell.Handle, que es el MISMO archivo en las dos
		// plataformas: el estado, el consentimiento, la llave, el proveedor y la pregunta.
		//
		// LOS TRES VERBOS QUE SE INTERCEPTAN ACA son los que necesitan pantalla: abrir un modulo
		// pone su WebView encima, y la propuesta de alta en curso es estado de esta Activity.
		// iOS hace lo mismo con los dos suyos en ViewController.Bridge.
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
					// EL ICONO EN LA PANTALLA DE INICIO. Necesita esta Activity porque el intent
					// que se fija apunta a ELLA, y porque el diálogo que Android muestra es del
					// sistema y hay que estar en primer plano para verlo.
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
