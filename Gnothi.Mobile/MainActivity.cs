using Android.App;
using Android.OS;
using Android.Webkit;
using Gnothi.Assistant;
using Gnothi.Host;
using System.Text.Json;

// QUE PUEDE HABLAR GNOTHI EN CLARO: solo el loopback, que es donde aterriza un tunel de
// desarrollo. Todo lo demas exige HTTPS, que es lo que Android impone desde API 28 y que el
// cartero cumple. Ver Resources/xml/network_security_config.xml.
[assembly: Android.App.Application(NetworkSecurityConfig = "@xml/network_security_config")]

namespace Gnothi.Mobile;

// THE GNOTHI HOST ON A PHONE, with a face.
//
// The face is ONE html file in Assets, rendered by a WebView. Why not Blazor/MAUI, which was
// the natural suggestion: real Blazor on Android needs the MAUI workload, which installs into
// Program Files -- admin on this machine -- and rewrites the project. The WebView gives what
// was wanted from it (web-grade UI, and the model's markdown rendered with real tables, which
// native TextViews cannot do) while the Host code stays untouched.
//
// THE BRIDGE IS window.prompt(). The page calls prompt('gnothi', json) and OnJsPrompt answers
// synchronously; C# pushes back with EvaluateJavascript(gn.push(...)). Old-school on purpose:
// no exported Java methods, no extra packages, works on every WebView.
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
[Activity(Label = "Gnothi", MainLauncher = true, Theme = "@android:style/Theme.Material.Light.NoActionBar")]
public class MainActivity : Activity
{
	private Dispatcher? dispatcher;
	private ModuleCatalog? catalog;
	private WebView? web;

	// La raiz de la pantalla, para poder poner el modulo encima; el anfitrion, para pedirselo;
	// y cual esta a la vista, que es lo que el boton atras tiene que cerrar primero.
	private Android.Widget.FrameLayout? raiz;
	private AnfitrionDeModulosWeb? anfitrion;
	private ModuloWeb? enPantalla;
	private ConsentWatch? watch;
	private readonly CancellationTokenSource stop = new();

	// WHERE THE MODEL LIVES. Two constants, because switching provider must stay this cheap --
	// that is the whole argument for IModel being an interface.
	//
	//   Groq        https://api.groq.com/openai/v1     free tier, no card asked
	//   Cerebras    https://api.cerebras.ai/v1
	//   OpenRouter  https://openrouter.ai/api/v1
	//   LM Studio   http://<the machine's IP>:1234/v1  NO key -- nothing leaves the LAN
	//
	// 'localhost' does NOT work from the phone: on Android that is the phone itself.
	private const string ModelUrl = "https://api.groq.com/openai/v1";
	private const string ModelName = "openai/gpt-oss-120b";

	// El glifo por negocio, DE RESPALDO. Un mapa chico y explicito: un modulo que no este aca
	// cae a la caja generica. Desde el 2026-09-08 la tarjeta pinta la MARCA de la institucion
	// -su icono y su color, leidos del bundle que ella publico (Marca.cs)- y este mapa solo
	// se usa cuando un modulo no la trae.
	private static string Icon(string module) => module switch
	{
		"examenes" => "\U0001FA7A",   // stethoscope
		"facturas" => "\U0001F9FE",   // receipt
		"compras"  => "\U0001F6D2",   // trolley
		_          => "\U0001F4E6",   // box
	};

	protected override void OnCreate(Bundle? savedInstanceState)
	{
		base.OnCreate(savedInstanceState);

		// Si Gnothi se abrio POR un enlace de alta, la direccion del bundle viene ahi.
		altaPendiente = Intent?.Data?.GetQueryParameter("bundle");
		LeerTraspasoDe(Intent);

		web = new WebView(this);
		web.Settings.JavaScriptEnabled = true;
		// The page detects the app by this mark and NEVER calls prompt() outside it: in a plain
		// browser that dialog is modal and freezes the page -- measured in the preview pane.
		web.Settings.UserAgentString += " GnothiHost";
		web.SetBackgroundColor(Android.Graphics.Color.ParseColor("#F5F3FB"));
		web.SetWebChromeClient(new Bridge(this));

		// targetSdk 36 makes edge-to-edge MANDATORY: the page would extend under the status
		// bar and the gesture bar, and the bottom nav ends up beneath the system's. The fix is
		// native and in one place -- pad by the system bars -- BUT the padding goes on a
		// CONTAINER, not on the WebView: a WebView ignores its own padding, which cost one
		// screenshot to find out.
		var frame = new Android.Widget.FrameLayout(this);
		frame.SetBackgroundColor(Android.Graphics.Color.ParseColor("#F5F3FB"));
		frame.AddView(web);
		frame.SetOnApplyWindowInsetsListener(new Insets());

		raiz = frame;
		SetContentView(frame);
		web.LoadUrl("file:///android_asset/ui.html");

		_ = Task.Run(Start);
	}

	// GNOTHI COMPRUEBA UN MÓDULO LA PRIMERA VEZ QUE LO VE, y deja constancia.
	//
	// Por qué al instalar y no cuando Sonia pregunta: un módulo que declara tools y después no
	// contesta es un módulo roto, y el momento de descubrirlo es antes de ofrecérselas al
	// asistente, no cuando ella ya preguntó. Es el cuarto pilar aplicado a sí mismo —Gnothi
	// responde lo que ningún módulo puede responder de sí— y es la conformance que la
	// especificación pide (11 §15.7).
	//
	// Por qué UNA VEZ: en móvil un módulo web cuesta un WebView con el runtime adentro, medido
	// en 216-248 MB. Comprobarlo en cada arranque sería pagar eso por nada. La marca queda en
	// la carpeta del módulo, así que reinstalarlo vuelve a comprobarlo.
	private async Task Comprobar(CancellationToken ct)
	{
		if (dispatcher is null || catalog is null) return;

		foreach (Gnothi.Host.Module m in catalog.Modules)
		{
			string marca = System.IO.Path.Combine(m.Directory, "comprobado.json");
			if (System.IO.File.Exists(marca)) continue;

			try
			{
				// 1. LO QUE DICE CONTRA LO QUE HACE. Esto ya lo sabe hacer el Host.
				var verificar = new Request(Dispatcher.HostModule, "verify_module",
					new Dictionary<string, object?> { ["module"] = m.Manifest.Module });
				Response dicho = await dispatcher.Serve(verificar, ct);
				Log($"comprobación de {m.Manifest.Module}: {JsonSerializer.Serialize(dicho.Content)}");

				// 2. Y QUE CONTESTE DE VERDAD. Declarar bien y fallar al primer pedido es otra
				//    forma de estar roto, y no la detecta comparar listas.
				DeclaredTool? primera = m.Manifest.Tools.FirstOrDefault(t => t.ReadOnly && t.Input.Count == 0);
				if (primera is not null)
				{
					Response respuesta = await dispatcher.Serve(
						new Request(m.Manifest.Module, primera.Name, null), ct);
					string texto = JsonSerializer.Serialize(respuesta.Content);
					Log($"  {primera.Name} contestó {texto.Length} bytes: {texto[..Math.Min(160, texto.Length)]}");
				}

				System.IO.File.WriteAllText(marca,
					JsonSerializer.Serialize(new { cuando = DateTime.UtcNow.ToString("O") }));
			}
			catch (Exception ex)
			{
				// Un módulo que no pasa la comprobación NO se desinstala solo: eso es decisión
				// de Sonia. Queda dicho, y sin marca, así que se vuelve a intentar.
				Log($"comprobación de {m.Manifest.Module} FALLÓ: {ex.GetType().Name}: {ex.Message}");
			}
		}
	}

	private async Task Start()
	{
		// 1 - LIFECYCLE: the Host installs what was downloaded. adb cannot write into the
		//     private area, and that is correct: nobody from outside installs a module.
		string incoming = System.IO.Path.Combine(GetExternalFilesDir(null)!.AbsolutePath, "incoming");
		string root = System.IO.Path.Combine(FilesDir!.AbsolutePath, "Modulos");
		string journals = System.IO.Path.Combine(FilesDir!.AbsolutePath, "journals");

		// Migration: the key used to live in one file for one hardcoded provider. If that old
		// file is here and the per-provider one is not, it becomes the Groq key.
		string old = System.IO.Path.Combine(FilesDir!.AbsolutePath, "model.key");
		if (File.Exists(old) && !File.Exists(KeyPath)) File.Move(old, KeyPath);

		int copied = 0;
		if (System.IO.Directory.Exists(incoming))
		{
			string modulesIn = System.IO.Path.Combine(incoming, "Modulos");
			string journalsIn = System.IO.Path.Combine(incoming, "journals");
			if (System.IO.Directory.Exists(modulesIn)) copied += Copy(modulesIn, root);
			if (System.IO.Directory.Exists(journalsIn)) copied += Copy(journalsIn, journals);
		}

		// QUIEN SABE CREAR WEBVIEWS. ChooseShell es estatico y no tiene Context, asi que la
		// cascara de webview no puede construir el anfitrion: se lo registra la Activity, que
		// si lo tiene. Sin esto, un modulo con "type": "webview" falla diciendo exactamente eso.
		anfitrion = new AnfitrionDeModulosWeb(this);
		Gnothi.Host.WebViewShell.Anfitrion = anfitrion;

		// 2 - DISCOVER. The very same code the desktop Host runs.
		catalog = new ModuleCatalog(root);
		IReadOnlyList<string> warnings = catalog.Load();
		// THIS GATE PAYS FOR ITS OWN TOKENS. The key is Sonia's, so every character the module
		// wraps its facts in comes out of her pocket -- which is why this is the gate that
		// declares it can also read TOON. What it does not do is DEMAND it: a module answers in
		// whichever of the two suits the shape of that particular answer, and JSON stays valid.
		dispatcher = new Dispatcher(catalog, new Meter(), Accepts.JsonOrToon);

		foreach (string w in warnings) Log($"warning: {w}");
		Log($"{catalog.Modules.Count} modules installed" + (copied > 0 ? $" - {copied} files this launch" : ""));

		PushState();

		// La comprobacion de los modulos nuevos. Va DESPUES de PushState para que la pantalla
		// ya este pintada: en movil comprobar un modulo web cuesta levantar su WebView.
		await Comprobar(stop.Token);

		// Si la app se abrio por un enlace de alta, recien ahora hay catalogo donde instalar.
		if (altaPendiente is { Length: > 0 } pendiente)
		{
			altaPendiente = null;
			await DarDeAlta(pendiente, stop.Token);
		}

		// Y si vino con un traspaso, recien ahora hay a quien entregarselo.
		if (traspasoPendiente is { } t)
		{
			traspasoPendiente = null;
			await EntregarTraspaso(t.Modulo, t.Aserciones, stop.Token);
		}

		// 3 - THE PERSON'S SWITCH, WATCHED. The UI's switches write installation.json and this
		//     is what makes them take effect; and an edit from OUTSIDE the app (adb, another
		//     tool) repaints the screen through the same path. One source of truth: her file.
		watch = new ConsentWatch(dispatcher) { Changed = (_, _) => { PushState(); return Task.CompletedTask; } };
		await watch.Run(Log, stop.Token);
	}

	protected override void OnDestroy()
	{
		stop.Cancel();
		watch?.Dispose();
		base.OnDestroy();
	}

	// THE BACK BUTTON asks the page first: a sheet closes, a detail goes home. Only when the
	// page answers false -- already on home -- does the app step aside, to the BACKGROUND and
	// not to death: finishing would throw away the loaded catalog for no reason.
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
		if (bundle is { Length: > 0 }) _ = DarDeAlta(bundle, stop.Token);

		LeerTraspasoDe(intento);
		if (traspasoPendiente is { } t)
		{
			traspasoPendiente = null;
			_ = EntregarTraspaso(t.Modulo, t.Aserciones, stop.Token);
		}
	}

	public override void OnBackPressed()
	{
		if (Cerrar()) return;

		web!.EvaluateJavascript("gn.back()", new BackAnswer(this));
	}

	private sealed class BackAnswer : Java.Lang.Object, IValueCallback
	{
		private readonly MainActivity host;
		public BackAnswer(MainActivity host) => this.host = host;

		public void OnReceiveValue(Java.Lang.Object? value)
		{
			if (value?.ToString() == "true") return;   // the page went back by itself
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
		if (catalog is null) return null;
		if (nombrado is { Length: > 0 }) return catalog.Find(nombrado);

		string publicador = "";
		try
		{
			using JsonDocument d = JsonDocument.Parse(asercionesJson);
			if (d.RootElement.TryGetProperty("publicador", out JsonElement p) && p.ValueKind == JsonValueKind.String)
				publicador = p.GetString() ?? "";
		}
		catch (JsonException) { }

		if (publicador.Length == 0) return null;

		foreach (Gnothi.Host.Module m in catalog.Modules)
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
			ModuloWeb vivo = await anfitrion.Abrir(m, ct);
			string resultado = await vivo.Traspasar(asercionesJson, ct);
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

	// LA INSTITUCIÓN PONE UN BOTÓN EN SU PÁGINA y ese botón abre a Gnothi con la dirección de
	// su bundle (especificación 13 §7). El enlace apunta al dominio de GNOTHI y no al de la
	// institución, por una razón de plataforma y no de gusto: Chromium mantiene en el browser
	// los enlaces al mismo host de la página actual, y Apple hace lo mismo con los universal
	// links del mismo dominio que la navegación previa.
	// LA PROPUESTA EN CURSO, esperando que la persona diga si o no. Una sola a la vez: si llega
	// otro enlace mientras hay una en pantalla, la anterior se descarta -- no se acumulan
	// decisiones que ella no vio.
	private PropuestaDeAlta? propuesta;
	private AltaDeModulo? altaEnCurso;

	private async Task DarDeAlta(string url, CancellationToken ct)
	{
		if (catalog is null) { altaPendiente = url; return; }

		try
		{
			if (propuesta is not null) { AltaDeModulo.Descartar(propuesta); propuesta = null; }

			altaEnCurso = new AltaDeModulo(
				System.IO.Path.Combine(FilesDir!.AbsolutePath, "Modulos"),
				System.IO.Path.Combine(FilesDir!.AbsolutePath, "journals"));

			// BAJAR NO ES INSTALAR. Esto trae el bundle y lee su manifiesto; el módulo todavía
			// no ocupa ningún lugar en el aparato.
			propuesta = await altaEnCurso.Examinar(url, ct, paso => PushAlta("trabajando", paso));

			Log($"alta: propuesta de '{propuesta.Manifiesto.Module}' servida por {propuesta.Servido}");
			PushPropuesta(propuesta);
		}
		catch (Exception ex)
		{
			// Un alta que falla tiene que DECIRLO, Y EN LA PANTALLA. Hasta el 2026-09-04 esto
			// solo escribia en logcat: la persona tocaba el botón de su institución, volvía a
			// Gnothi y no pasaba nada, sin ninguna pista de por qué. El comentario decía
			// exactamente eso y la implementación se lo contaba a un desarrollador con un cable.
			Log($"alta: no se pudo leer el módulo de {url}: {ex.GetType().Name}: {ex.Message}");
			PushAlta("error", EnCristiano(ex));
		}
	}

	// LA PERSONA ACEPTÓ. Recién acá el módulo existe.
	private string AceptarAlta()
	{
		if (propuesta is null || altaEnCurso is null) return "{}";

		try
		{
			string modulo = altaEnCurso.Aceptar(propuesta);
			propuesta = null;

			// El catálogo se relee entero: es la misma ruta por la que un cambio hecho desde
			// afuera repinta la pantalla. Una sola fuente de verdad, sus archivos.
			foreach (string w in catalog!.Load()) Log($"warning: {w}");
			Log($"alta: '{modulo}' quedó instalado; {catalog.Modules.Count} módulos");

			PushState();
			PushAlta("listo", $"Listo: ya tenés '{TituloDe(modulo)}' en Gnothi.");
			_ = Task.Run(() => Comprobar(stop.Token));
		}
		catch (Exception ex)
		{
			Log($"alta: no se pudo instalar: {ex.GetType().Name}: {ex.Message}");
			PushAlta("error", EnCristiano(ex));
		}

		return "\"ok\"";
	}

	// LA PERSONA DIJO QUE NO. No queda nada: ni carpeta, ni journal, ni entrada en el catálogo.
	private string DescartarAlta()
	{
		if (propuesta is not null)
		{
			AltaDeModulo.Descartar(propuesta);
			propuesta = null;
		}
		PushAlta("nada", "");
		return "\"ok\"";
	}

	private void PushPropuesta(PropuestaDeAlta p) => Push("propuesta", "", JsonSerializer.Serialize(new
	{
		modulo = p.Manifiesto.Module,
		titulo = p.Manifiesto.Title,
		institucion = p.Manifiesto.Brand?.ShortName is { Length: > 0 } corto ? corto : p.Manifiesto.Institution,
		marca = Marca.Leer(p.Carpeta, p.Manifiesto),
		servido = p.Servido,
		publicador = p.Publicador,
		journal = p.DondeVaElJournal,
		yaEstaba = p.YaEstaba,
		tools = p.Manifiesto.Tools.Select(t => new { nombre = t.Name, titulo = t.Title, descripcion = t.Description }),
	}));

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
		Gnothi.Host.Module? m = catalog?.Find(nombre);
		if (m is null || anfitrion is null) return "{\"ok\":false}";

		_ = Task.Run(async () =>
		{
			try
			{
				ModuloWeb vivo = await anfitrion.Abrir(m, stop.Token);
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
	private void Mostrar(ModuloWeb vivo)
	{
		Android.Views.View? vista = vivo.Vista;
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

		Android.Views.View? vista = enPantalla.Vista;
		if (vista is not null) raiz?.RemoveView(vista);

		enPantalla.Visible = false;
		enPantalla = null;
		return true;
	}

	private void Log(string s) => global::Android.Util.Log.Info("GNOTHI", s);

	// ── what the page asks for ──────────────────────────────────────────────────────────

	// The state as the UI needs it: modules, their prose, where each switch stands, and the
	// journal's real weight -- measured from outside, which is the Host's own verb.
	private string StateJson()
	{
		if (catalog is null) return "{\"loading\":true}";

		var modules = catalog.Modules.Select(m =>
		{
			long bytes = 0; int files = 0;
			try
			{
				foreach (FileInfo f in new DirectoryInfo(m.Installation.GrantedFolder)
					.EnumerateFiles("*", SearchOption.AllDirectories))
				{ bytes += f.Length; files++; }
			}
			catch { /* a module with no journal yet weighs nothing */ }

			return new Dictionary<string, object?>
			{
				["name"] = m.Name,
				["title"] = m.Manifest.Title,
				["institution"] = m.Manifest.Institution,
				["shortName"] = m.Manifest.Brand?.ShortName ?? "",
				["slogan"] = m.Manifest.Brand?.Slogan ?? "",
				["icon"] = Icon(m.Name),
				["brand"] = Marca.Leer(m.Directory, m.Manifest),
				["enabled"] = m.Installation.Enabled,
				["journalBytes"] = bytes,
				["journalFiles"] = files,
				["tools"] = m.Manifest.Tools.Select(t => new Dictionary<string, object?>
				{
					["name"] = t.Name,
					["title"] = t.Title,
					["description"] = t.Description,
					["enabled"] = !m.Installation.DisabledTools.Contains(t.Name),
				}).ToArray(),
			};
		}).ToArray();

		return JsonSerializer.Serialize(new Dictionary<string, object?>
		{
			["modules"] = modules,
			["settings"] = Settings(),
		});
	}

	// THE SWITCH WRITES THE PERSON'S FILE. Not an in-memory flag: the same installation.json
	// the desktop Host reads, the watcher watches, and the catalog reloads from. The UI has no
	// state of its own -- it repaints from whatever the file says, which is why an external
	// edit shows up on screen too.
	private string SetConsent(string module, string? tool, bool enabled)
	{
		Module? m = catalog?.Modules.FirstOrDefault(x => x.Name == module);
		if (m is null) return StateJson();

		Installation updated;
		if (tool is null)
		{
			updated = m.Installation with { Enabled = enabled };
		}
		else
		{
			List<string> off = [.. m.Installation.DisabledTools.Where(t => t != tool)];
			if (!enabled) off.Add(tool);
			updated = m.Installation with { DisabledTools = off };
		}

		File.WriteAllText(
			System.IO.Path.Combine(m.Directory, "installation.json"),
			JsonSerializer.Serialize(updated, new JsonSerializerOptions { WriteIndented = true }));

		// Applied NOW, not when the watcher's 300ms burst settles: the switch the person just
		// moved has to answer immediately. The watcher still fires -- ReloadConsent finds
		// nothing further to change and stays quiet.
		catalog!.ReloadConsent();
		Log($"consent: {module}{(tool is null ? "" : "." + tool)} -> {(enabled ? "on" : "off")}");
		return StateJson();
	}

	private Dictionary<string, object?> Settings()
	{
		(string name, string url, string model) = Provider();
		return new Dictionary<string, object?>
		{
			["name"] = name,
			["provider"] = new Uri(url).Host,
			["model"] = model,
			// Whether there IS a key for THIS provider, never the key itself.
			["keyPresent"] = File.Exists(KeyPath),
			["root"] = catalog?.Root ?? "",
		};
	}

	// THE PROVIDER, CHOSEN BY THE PERSON. Every preset speaks the OpenAI shape, which is what
	// ModelOpenAiCompatible talks -- so "use ChatGPT instead" is a file with a URL and a model
	// name, not code. The constants above remain only as the default when nothing was chosen.
	private string CfgPath => System.IO.Path.Combine(FilesDir!.AbsolutePath, "model.cfg");

	private (string name, string url, string model) Provider()
	{
		try
		{
			if (File.Exists(CfgPath))
			{
				JsonElement e = JsonSerializer.Deserialize<JsonElement>(File.ReadAllText(CfgPath));
				return (e.GetProperty("name").GetString() ?? "Groq",
						e.GetProperty("url").GetString() ?? ModelUrl,
						e.GetProperty("model").GetString() ?? ModelName);
			}
		}
		catch { /* an unreadable choice falls back to the default */ }
		return ("Groq", ModelUrl, ModelName);
	}

	private string SetProvider(string name, string url, string model)
	{
		File.WriteAllText(CfgPath, JsonSerializer.Serialize(new Dictionary<string, string>
		{
			["name"] = name, ["url"] = url, ["model"] = model,
		}));
		Log($"provider: {name} · {model}");
		return StateJson();
	}

	// THE KEY, MANAGED BY THE PERSON. This is what makes BYOK real: until now the file was
	// placed by a developer over a cable, which is not "bring your own key" -- it is bring
	// MINE. Any key of the chosen provider works, free tier or paid: the file does not know
	// the difference.
	//
	// PER PROVIDER, on purpose: a Groq key is not an OpenAI key. Switching provider switches
	// which key file is read, so going to ChatGPT and back to Groq loses nothing.
	//
	// Three rules, enforced here: it lives ONLY in the app's private area, it is NEVER sent
	// back to the page (StateJson carries a boolean, not the value), and the log records the
	// event without the key.
	private string KeyPath
	{
		get
		{
			string safe = new string(Provider().name.Where(char.IsLetterOrDigit).ToArray());
			return System.IO.Path.Combine(FilesDir!.AbsolutePath, $"key-{safe}.key");
		}
	}

	private string SetKey(string key)
	{
		key = key.Trim();
		if (key.Length > 0)
		{
			File.WriteAllText(KeyPath, key);
			Log($"model key: saved ({key.Length} chars)");
		}
		return StateJson();
	}

	private string ClearKey()
	{
		if (File.Exists(KeyPath)) File.Delete(KeyPath);
		Log("model key: cleared");
		return StateJson();
	}

	// ONE QUESTION, answered through Gate 2: same AgentLoop, same dispatcher, same key file.
	// Runs detached; the trace and the answer land in the page as they happen.
	private void Ask(string id, string question)
	{
		_ = Task.Run(async () =>
		{
			try
			{
				(_, string url, string model) = Provider();
				var loop = new AgentLoop(dispatcher!, new ModelOpenAiCompatible(url, model, KeyPath));

				string answer = await loop.Answer(question, line => Push("trace", id, line), stop.Token);
				Push("answer", id, answer);
			}
			catch (Exception e)
			{
				Log($"ask failed: {e.GetType().Name}: {e.Message}");
				Push("fail", id, $"{e.GetType().Name}: {e.Message}");
			}
		});
	}

	private void PushState() => Push("state", "", StateJson());

	// COMO SE CUENTA EL ALTA EN LA PANTALLA. Es lo primero que la persona ve hacer a Gnothi por
	// ella -- toco un boton en la pagina de su institucion y volvio aca-, y hasta el 2026-09-04
	// era un silencio de 12 MB. Tres estados: trabajando, listo, error.
	private void PushAlta(string estado, string detalle) =>
		Push("alta", "", JsonSerializer.Serialize(new { estado, detalle }));

	// EL FALLO, DICHO PARA LA PERSONA. El detalle tecnico queda en logcat, que es donde sirve;
	// en la pantalla va lo que ella puede hacer algo con. "Response status code does not indicate
	// success: 404 (Not Found)" no le dice a nadie que el enlace de su institucion esta roto.
	private static string EnCristiano(Exception ex) => ex switch
	{
		HttpRequestException http when http.StatusCode == System.Net.HttpStatusCode.NotFound =>
			"La dirección que dio tu institución ya no tiene el módulo. Pedile el enlace otra vez.",

		HttpRequestException =>
			"No se pudo hablar con tu institución para bajar el módulo. Revisá tu conexión y probá de nuevo.",

		TaskCanceledException or System.OperationCanceledException =>
			"La descarga tardó demasiado y se cortó. Probá de nuevo.",

		ArgumentException =>
			"El enlace que tocaste no es una dirección válida de módulo.",

		// Lo que tira AltaDeModulo -bundle sin manifiesto, nombre invalido, zip que escribe
		// afuera, techo de tamano- ya viene escrito para leerse. Se pasa tal cual.
		InvalidOperationException =>
			"No se pudo instalar el módulo: " + ex.Message,

		_ => "No se pudo instalar el módulo. " + ex.Message,
	};

	// El titulo que la institucion se puso, si el modulo ya esta en el catalogo. Se prefiere al
	// nombre tecnico porque es lo que la persona reconoce.
	private string TituloDe(string modulo)
	{
		Gnothi.Host.Module? m = catalog?.Find(modulo);
		return m?.Manifest.Title is { Length: > 0 } titulo ? titulo : modulo;
	}

	private void Push(string kind, string id, string payload) => RunOnUiThread(() =>
		web?.EvaluateJavascript(
			$"gn.push({JsonSerializer.Serialize(kind)},{JsonSerializer.Serialize(id)},{JsonSerializer.Serialize(payload)})",
			null));

	// ── the bridge itself ───────────────────────────────────────────────────────────────
	//
	// The page calls window.prompt('gnothi', json). Anything else is a real prompt and gets
	// the default behaviour.
	private sealed class Bridge : WebChromeClient
	{
		private readonly MainActivity host;
		public Bridge(MainActivity host) => this.host = host;

		public override bool OnJsPrompt(WebView? view, string? url, string? message, string? defaultValue, JsPromptResult? result)
		{
			if (message != "gnothi" || defaultValue is null || result is null)
				return base.OnJsPrompt(view, url, message, defaultValue, result);

			try
			{
				JsonElement req = JsonSerializer.Deserialize<JsonElement>(defaultValue);
				string op = req.GetProperty("op").GetString() ?? "";
				host.Log($"bridge: {op}");

				string answer = op switch
				{
					"modules" => host.StateJson(),
					"setTool" => host.SetConsent(
						req.GetProperty("module").GetString() ?? "",
						req.GetProperty("tool").GetString(),
						req.GetProperty("enabled").GetBoolean()),
					"setModule" => host.SetConsent(
						req.GetProperty("module").GetString() ?? "",
						null,
						req.GetProperty("enabled").GetBoolean()),
					"setProvider" => host.SetProvider(
						req.GetProperty("name").GetString() ?? "",
						req.GetProperty("url").GetString() ?? "",
						req.GetProperty("model").GetString() ?? ""),
					"setKey" => host.SetKey(req.GetProperty("key").GetString() ?? ""),
					"clearKey" => host.ClearKey(),
					"abrir" => host.AbrirModulo(req.GetProperty("module").GetString() ?? ""),
					"altaAceptar" => host.AceptarAlta(),
					"altaDescartar" => host.DescartarAlta(),
					"ask" => RunAsk(req),
					_ => "{}",
				};

				result.Confirm(answer);
			}
			catch (Exception e)
			{
				host.Log($"bridge: {e.GetType().Name}: {e.Message}");
				result.Confirm("{}");
			}

			return true;
		}

		private string RunAsk(JsonElement req)
		{
			host.Ask(req.GetProperty("id").GetString() ?? "", req.GetProperty("question").GetString() ?? "");
			return "\"ok\"";
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

	// Copy from the download area into the private one. Dumb on purpose: the Host does not
	// interpret anything it copies.
	private static int Copy(string source, string target)
	{
		int n = 0;
		System.IO.Directory.CreateDirectory(target);
		foreach (string file in System.IO.Directory.GetFiles(source, "*", System.IO.SearchOption.AllDirectories))
		{
			string destination = file.Replace(source, target);
			System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(destination)!);
			System.IO.File.Copy(file, destination, overwrite: true);
			n++;
		}
		return n;
	}
}
