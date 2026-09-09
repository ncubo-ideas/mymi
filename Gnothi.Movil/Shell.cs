using Gnothi.Assistant;
using Gnothi.Host;
using System.Text.Json;

namespace Gnothi.Movil;

// LA CASCARA DEL TELEFONO, COMPARTIDA POR ANDROID Y POR iOS.
//
// DE DONDE SALE. Todo esto vivia dentro de MainActivity.cs, y ahi funcionaba. Se saco cuando
// hubo que hacer la version de iOS: la alternativa era copiar 250 lineas y mantenerlas en dos
// lados, que es exactamente como se desincronizan.
//
// EL CORTE, y es el mismo criterio que separa al Dispatcher de la Puerta 1: aca esta lo que NO
// depende de la plataforma -el estado que la pagina necesita, el consentimiento, la llave, el
// proveedor, y el switch que atiende lo que la pagina pide- y afuera queda solo lo que sabe de
// WebView, de hilos de UI y de logs del sistema. Medido en lineas: la parte de cada plataforma
// quedo en ~60.
//
// POR QUE NO MAUI, que es la pregunta natural. MainActivity.cs ya trae la respuesta escrita:
// el workload de MAUI se instala en Program Files -administrador en esta maquina- y reescribe
// el proyecto. Pero hay una razon mas fuerte, y es de diseño: LA CARA DE GNOTHI ES UN HTML.
// Un solo Assets/ui.html, el MISMO archivo en los dos telefonos, renderizado por un WebView.
// MAUI compartiria su propio framework de UI -que aca no se usa- y NO compartiria el puente
// del WebView, que es API nativa de cada plataforma igual. O sea: costo alto por compartir lo
// que ya estaba compartido, sin resolver lo que de verdad difiere.
//
// LO QUE SE COMPARTE, CONTADO:
//
//     Gnothi.Host/*.cs      el nucleo         identico, ya se compartia
//     Gnothi.Assistant/*.cs la Puerta 2       identico, ya se compartia
//     Gnothi.Movil/Shell.cs esta cascara      identico, NUEVO
//     Assets/ui.html        la cara           identico, un solo archivo
//     ---------------------------------------------------------------
//     MainActivity.cs       WebView Android   ~60 lineas
//     ViewController.cs     WKWebView iOS     ~60 lineas
public sealed class Shell : IDisposable
{
	// LAS DOS CARPETAS, Y LA PLATAFORMA LAS ELIGE. Es el mismo principio que el grantedFolder
	// de un modulo: quien otorga no es quien usa.
	//
	//   Files      el area privada de la app. Nadie de afuera escribe aca, y eso es correcto:
	//              nadie de afuera instala un modulo.
	//   Incoming   donde cae lo que se bajo, para que el Host lo instale al arrancar.
	//
	// En Android son FilesDir y GetExternalFilesDir(null)/incoming.
	// En iOS son Library/Application Support y Documents/incoming -- y ahi hay una ventaja
	// que Android no tiene: Documents/ SE VE en la app Archivos, asi que la persona puede
	// dejar el paquete ella misma, sin cable y sin adb.
	/// <param name="ReescribirRutas">
	/// Si el installation.json que viene en Incoming trae rutas de OTRA maquina y hay que
	/// hacerlas locales al copiarlo.
	///
	/// ANDROID: false. armar-paquete.ps1 ya escribio las rutas buenas, porque en Android el
	/// area privada de una app es predecible: /data/user/0/&lt;applicationId&gt;/files. Se sabe
	/// al armar el paquete.
	///
	/// iOS: true, y no es un capricho. El contenedor es
	/// /var/mobile/Containers/Data/Application/&lt;GUID&gt;/... y ESE GUID CAMBIA EN CADA
	/// INSTALACION. No hay ninguna ruta que se pueda dejar escrita de antemano: la unica que
	/// existe es la que el proceso averigua cuando ya esta corriendo.
	/// </param>
	/// <param name="Instantaneas">
	/// El contenedor COMPARTIDO donde el Host le deja la foto a la caratula, o null si esta
	/// plataforma no la necesita.
	///
	/// ANDROID: null. Alla la caratula es un APK aparte que llama al Host EN VIVO por binder,
	/// con la compuerta de identidad de por medio. No hay nada que dejar escrito.
	///
	/// iOS: el contenedor del App Group. Alla los App Intents solo existen en Swift y el Host
	/// esta en .NET, asi que no se tocan: el Host deja la foto y la caratula la lee. Ver
	/// Snapshot.cs.
	/// </param>
	/// <param name="FotoLocal">
	/// Si ademas hay que dejar la foto en la carpeta PROPIA de la app, en Files/instantanea/.
	///
	/// Es para una intent que viva DENTRO de esta misma app (Gnothi.iOS/Intents/): ahi no hace
	/// falta App Group, ni entitlement, ni portal de Apple · es un archivo en la carpeta de uno.
	/// Android lo deja en false: alla la caratula es un APK aparte que pregunta en vivo.
	/// </param>
	public sealed record Paths(
		string Files,
		string Incoming,
		bool ReescribirRutas = false,
		string? Instantaneas = null,
		bool FotoLocal = false);

	private readonly Paths paths;
	private readonly Action<string> log;
	private readonly Action<string, string, string> push;
	private readonly CancellationTokenSource stop = new();

	private Dispatcher? dispatcher;
	private ModuleCatalog? catalog;
	private ConsentWatch? watch;

	/// La cola de escrituras de la foto. Existe para SERIALIZARLAS: dos Fotografiar() a la vez
	/// pelean por el mismo archivo temporal. Ver SetConsent.
	private Task fotos = Task.CompletedTask;

	// DONDE VIVE EL MODELO. Dos constantes, porque cambiar de proveedor tiene que seguir
	// siendo asi de barato -- ese es todo el argumento de que IModel sea una interfaz.
	//
	//   Groq        https://api.groq.com/openai/v1     free tier, sin tarjeta
	//   Cerebras    https://api.cerebras.ai/v1
	//   OpenRouter  https://openrouter.ai/api/v1
	//   LM Studio   http://<IP de la maquina>:1234/v1  SIN llave -- nada sale de la LAN
	//
	// 'localhost' NO sirve desde el telefono: ahi localhost es el telefono.
	private const string ModelUrl = "https://api.groq.com/openai/v1";
	private const string ModelName = "openai/gpt-oss-120b";

	/// <param name="push">
	/// Como se le empuja algo a la pagina. La plataforma se encarga de meterlo en el hilo de
	/// UI: en Android es RunOnUiThread + EvaluateJavascript, en iOS InvokeOnMainThread +
	/// EvaluateJavaScript. La cascara no quiere saber cual.
	/// </param>
	// LA FABRICA DEL MODELO LOCAL LA PONE LA CASCARA, y por eso es opcional. El modelo del
	// telefono se habla por DllImport contra simbolos del propio ejecutable, asi que solo iOS
	// puede construirlo: Android pasa null y ese proveedor no se ofrece. Este archivo se compila
	// en las dos plataformas y no debe saber cual es cual.
	public Shell(Paths paths, Action<string> log, Action<string, string, string> push,
		Func<string, IModel>? modeloLocal = null)
	{
		this.paths = paths;
		this.log = log;
		this.push = push;
		this.modeloLocal = modeloLocal;
	}

	private readonly Func<string, IModel>? modeloLocal;

	/// <summary>El proveedor que corre en el aparato, sin url y sin llave.</summary>
	public const string ProveedorLocal = "En este teléfono";

	// El icono por negocio. Un mapa chico y explicito: un modulo que no este aca cae a la caja
	// generica. NO viene del manifiesto a proposito -- el icono es asunto del Host, no de la
	// institucion.
	public static string Icon(string module) => module switch
	{
		"examenes" => "\U0001FA7A",   // estetoscopio
		"facturas" => "\U0001F9FE",   // recibo
		"compras"  => "\U0001F6D2",   // carrito
		_          => "\U0001F4E6",   // caja
	};

	// ── el arranque ─────────────────────────────────────────────────────────────────────

	public async Task Run()
	{
		// 1 - CICLO DE VIDA: el Host instala lo que se bajo.
		string root = Path.Combine(paths.Files, "Modulos");
		string journals = Path.Combine(paths.Files, "journals");

		// Migracion: la llave vivia en un archivo para un solo proveedor quemado. Si ese
		// archivo viejo esta y el por-proveedor no, pasa a ser la llave de Groq.
		string old = Path.Combine(paths.Files, "model.key");
		if (File.Exists(old) && !File.Exists(KeyPath)) File.Move(old, KeyPath);

		int copied = 0;
		if (Directory.Exists(paths.Incoming))
		{
			string modulesIn = Path.Combine(paths.Incoming, "Modulos");
			string journalsIn = Path.Combine(paths.Incoming, "journals");
			if (Directory.Exists(modulesIn)) copied += Copy(modulesIn, root);
			if (Directory.Exists(journalsIn)) copied += Copy(journalsIn, journals);

			if (paths.ReescribirRutas) Localizar(root, journals);
		}

		// 2 - DESCUBRIR. El mismisimo codigo que corre el Host de escritorio.
		catalog = new ModuleCatalog(root);
		IReadOnlyList<string> warnings = catalog.Load();
		// ESTA PUERTA PAGA SUS PROPIOS TOKENS. La llave es de Sonia, asi que cada caracter con
		// que un modulo envuelve sus hechos sale de su bolsillo -- por eso esta es la puerta
		// que declara que tambien lee TOON. Lo que no hace es EXIGIRLO: el modulo contesta en
		// el que le sirva a esa respuesta, y JSON sigue valiendo.
		dispatcher = new Dispatcher(catalog, new Meter(), Accepts.JsonOrToon);

		foreach (string w in warnings) log($"warning: {w}");
		log($"{catalog.Modules.Count} modules installed" + (copied > 0 ? $" - {copied} files this launch" : ""));

		PushState();
		await Fotografiar();

		// LA COMPROBACION DE LOS MODULOS NUEVOS. Va DESPUES de PushState para que la pantalla
		// ya este pintada: en movil comprobar un modulo web cuesta levantar su WebView.
		await VerifyNewModules(stop.Token);

		// 3 - EL INTERRUPTOR DE LA PERSONA, VIGILADO. Los switches de la UI escriben
		//     installation.json y esto es lo que los hace efectivos; y una edicion de AFUERA
		//     (adb, otra herramienta) repinta la pantalla por el mismo camino. Una sola
		//     fuente de verdad: su archivo.
		//
		//     LA FOTO SE REHACE ACA TAMBIEN, y es lo que cierra el agujero de consentimiento:
		//     si la persona apaga un modulo, la foto se reescribe SIN el, asi que la caratula
		//     deja de poder contestar por ese modulo. Sin esta linea quedaria una foto vieja
		//     contestando algo que ella ya revoco.
		watch = new ConsentWatch(dispatcher)
		{
			Changed = async (_, _) => { PushState(); await Fotografiar(); },
		};
		await watch.Run(log, stop.Token);
	}

	// GNOTHI COMPRUEBA UN MODULO LA PRIMERA VEZ QUE LO VE, y deja constancia.
	//
	// Por que al instalar y no cuando Sonia pregunta: un modulo que declara tools y despues no
	// contesta es un modulo roto, y el momento de descubrirlo es antes de ofrecerselas al
	// asistente, no cuando ella ya pregunto. Es el cuarto pilar aplicado a si mismo -Gnothi
	// responde lo que ningun modulo puede responder de si- y es la conformance que la
	// especificacion pide (11 §15.7).
	//
	// Por que UNA VEZ: en movil un modulo web cuesta un WebView con el runtime adentro, medido
	// en 216-248 MB. Comprobarlo en cada arranque seria pagar eso por nada. La marca queda en
	// la carpeta del modulo, asi que reinstalarlo vuelve a comprobarlo.
	//
	// ESTABA EN MainActivity Y NO USABA NADA DE ANDROID -- Dispatcher, catalogo, archivos y
	// json-. Al traerlo aca, iOS tambien comprueba sus modulos al instalarlos, sin escribir
	// una linea de mas.
	private async Task VerifyNewModules(CancellationToken ct)
	{
		if (dispatcher is null || catalog is null) return;

		foreach (Module m in catalog.Modules)
		{
			string mark = Path.Combine(m.Directory, "comprobado.json");
			if (File.Exists(mark)) continue;

			try
			{
				// 1. LO QUE DICE CONTRA LO QUE HACE. Esto ya lo sabe hacer el Host.
				var verify = new Request(Dispatcher.HostModule, "verify_module",
					new Dictionary<string, object?> { ["module"] = m.Manifest.Module });
				Response said = await dispatcher.Serve(verify, ct);
				log($"comprobación de {m.Manifest.Module}: {JsonSerializer.Serialize(said.Content)}");

				// 2. Y QUE CONTESTE DE VERDAD. Declarar bien y fallar al primer pedido es otra
				//    forma de estar roto, y no la detecta comparar listas.
				DeclaredTool? first = m.Manifest.Tools.FirstOrDefault(t => t.ReadOnly && t.Input.Count == 0);
				if (first is not null)
				{
					Response answer = await dispatcher.Serve(new Request(m.Manifest.Module, first.Name, null), ct);
					string text = JsonSerializer.Serialize(answer.Content);
					log($"  {first.Name} contestó {text.Length} bytes: {text[..Math.Min(160, text.Length)]}");
				}

				File.WriteAllText(mark, JsonSerializer.Serialize(new { cuando = DateTime.UtcNow.ToString("O") }));
			}
			catch (Exception ex)
			{
				// Un modulo que no pasa la comprobacion NO se desinstala solo: eso es decision
				// de Sonia. Queda dicho, y sin marca, asi que se vuelve a intentar.
				log($"comprobación de {m.Manifest.Module} FALLÓ: {ex.GetType().Name}: {ex.Message}");
			}
		}
	}

	// ---------------------------------------------------------------- el alta desde el enlace

	// LA PROPUESTA EN CURSO, esperando que la persona diga si o no. UNA sola a la vez: si llega
	// otro enlace mientras hay una en pantalla, la anterior se descarta -- no se acumulan
	// decisiones que ella no vio.
	//
	// ESTO VIVE ACA Y NO EN LA PLATAFORMA porque nada de esto es de una plataforma: bajar un
	// zip, leer un manifiesto, mostrar una propuesta y esperar. Estuvo en MainActivity hasta
	// que hubo que hacer la version de iOS, y ahi se vio: el ViewController tuvo que copiarlo
	// entero. Dos copias de la pantalla mas importante que Gnothi le muestra a la persona.
	private EnrollmentProposal? proposal;
	private ModuleEnrollment? enrolling;

	/// LO QUE LLEGA DEL ENLACE DE LA INSTITUCION. Baja el bundle y lee su manifiesto; NO
	/// instala nada. La persona decide despues, y hasta entonces el bundle vive en un temporal.
	///
	/// Lo llaman las dos plataformas cuando su enlace aterriza: un App Link en Android, un
	/// universal link en iOS. Lo unico que cambia es como llega la URL.
	public Task Enroll(string url, CancellationToken ct = default) => Enroll(url, EnrollmentSource.Link, ct);

	// ONE ENTRY FOR EVERY WAY IN; only the provenance differs, and it travels to the sheet.
	private async Task Enroll(string url, EnrollmentSource source, CancellationToken ct)
	{
		try
		{
			if (proposal is not null) { enrolling?.Discard(proposal); proposal = null; }

			enrolling = new ModuleEnrollment(
				Path.Combine(paths.Files, "Modulos"),
				Path.Combine(paths.Files, "journals"),
				log);

			// BAJAR NO ES INSTALAR. Esto trae el bundle, lo inspecciona y lee su manifiesto; el
			// modulo todavia no ocupa ningun lugar en el aparato.
			proposal = await enrolling.Examine(url, source, ct, step => PushEnrollment("trabajando", step));

			log($"alta: propuesta de '{proposal.Manifest.Module}' servida por {proposal.ServedBy}");
			PushProposal(proposal);
		}
		catch (Exception ex)
		{
			// UN ALTA QUE FALLA TIENE QUE DECIRLO, Y EN LA PANTALLA. Cuando esto solo escribia
			// en el log, la persona tocaba el boton de su institucion, volvia a Gnothi y no
			// pasaba nada, sin ninguna pista de por que.
			log($"alta: no se pudo leer el módulo de {url}: {ex.GetType().Name}: {ex.Message}");
			PushEnrollment("error", InPlainWords(ex));
		}
	}

	// LA PERSONA ACEPTO. Recien aca el modulo existe.
	private string AcceptEnrollment()
	{
		if (proposal is null || enrolling is null) return "{}";

		try
		{
			string module = enrolling.Accept(proposal);
			proposal = null;

			// El catalogo se relee entero -- y con eso se comprueba el modulo nuevo.
			foreach (string w in ReloadAfterInstall()) log($"warning: {w}");
			log($"alta: '{module}' quedó instalado; {ModuleCount} módulos");

			PushEnrollment("listo", $"Listo: ya tenés '{TitleOf(module)}' en Gnothi.");
		}
		catch (Exception ex)
		{
			log($"alta: no se pudo instalar: {ex.GetType().Name}: {ex.Message}");
			PushEnrollment("error", InPlainWords(ex));
		}

		return "\"ok\"";
	}

	// LA PERSONA DIJO QUE NO. No queda nada: ni carpeta, ni journal, ni entrada en el catalogo.
	private string DiscardEnrollment()
	{
		if (proposal is not null)
		{
			enrolling?.Discard(proposal);
			proposal = null;
		}
		PushEnrollment("nada", "");
		return "\"ok\"";
	}

	// The person pasted the institution's link. It may be the link to Gnothi's domain with the
	// bundle as a parameter, or the bundle's address itself; both are accepted. Fire and forget,
	// like RunAsk: the page hears back through the same "alta"/"propuesta" pushes as a link.
	private string EnrollPasted(string link)
	{
		string bundle = ModuleEnrollment.BundleUrlOf(link);
		if (bundle.Length == 0)
		{
			PushEnrollment("error", "Pegá el enlace que te dio tu institución.");
			return "\"ok\"";
		}

		_ = Enroll(bundle, EnrollmentSource.PastedLink, stop.Token);
		return "\"ok\"";
	}

	// COMO SE CUENTA EL ALTA EN LA PANTALLA. Es lo primero que la persona ve hacer a Gnothi por
	// ella -toco un boton en la pagina de su institucion y volvio aca- y era un silencio de
	// 12 MB. Cuatro estados: trabajando, listo, error, nada.
	private void PushEnrollment(string estado, string detalle) =>
		Push("alta", "", JsonSerializer.Serialize(new { estado, detalle }));

	// LO QUE LA INSTITUCION PROPONE, mas las dos cosas que decide Gnothi y ella tiene derecho a
	// saber antes de aceptar: donde va a vivir el journal y contra que endpoint va a hablar.
	private void PushProposal(EnrollmentProposal p) => Push("propuesta", "", JsonSerializer.Serialize(new
	{
		modulo = p.Manifest.Module,
		titulo = p.Manifest.Title,
		institucion = p.Manifest.Institution,
		servido = p.ServedBy,
		publicador = p.Publisher,
		journal = p.JournalGoesTo,
		yaEstaba = p.AlreadyThere,
		tools = p.Manifest.Tools.Select(t => new { nombre = t.Name, titulo = t.Title, descripcion = t.Description }),

		// WHERE IT CAME FROM, as a fact about transport: which way in, and which host answered.
		// The sheet says out loud that the host is not yet verified against the institution.
		procedencia = new
		{
			origen = p.Provenance.Source == EnrollmentSource.PastedLink ? "pegado" : "enlace",
			servido = p.Provenance.ServedBy,
		},

		// WHAT GNOTHI CHECKED before showing this, so she decides with it in view: the form, the
		// weight, whether it carries an engine, and what it asks for beyond a data module.
		revision = new
		{
			forma = p.Inspection.Form == PackageForm.Hosted ? "hospedado" : "cargo",
			archivos = p.Inspection.Files,
			bytes = p.Inspection.Bytes,
			motorArchivos = p.Inspection.EngineFiles,
			motorBytes = p.Inspection.EngineBytes,
			excesos = p.Inspection.Excesses,
			encendido = p.Inspection.InstallEnabled,
		},
	}));

	// EL FALLO, DICHO EN CASTELLANO Y NO EN .NET. La persona no tiene por que leer el nombre de
	// una excepcion para entender que su institucion movio el archivo.
	private static string InPlainWords(Exception ex) => ex switch
	{
		HttpRequestException http when http.StatusCode == System.Net.HttpStatusCode.NotFound =>
			"La dirección que dio tu institución ya no tiene el módulo. Pedile el enlace otra vez.",

		HttpRequestException =>
			"No se pudo hablar con tu institución para bajar el módulo. Revisá tu conexión y probá de nuevo.",

		TaskCanceledException or OperationCanceledException =>
			"La descarga tardó demasiado y se cortó. Probá de nuevo.",

		ArgumentException =>
			"El enlace no es una dirección válida de módulo.",

		// Lo que tira ModuleEnrollment -bundle sin manifiesto, nombre invalido, zip que escribe
		// afuera, techo de tamano- ya viene escrito para leerse. Se pasa tal cual.
		InvalidOperationException =>
			"No se pudo instalar el módulo: " + ex.Message,

		_ => "No se pudo instalar el módulo. " + ex.Message,
	};

	/// Un modulo del catalogo por nombre, o null. Lo necesita la pantalla que pone un modulo
	/// web en pantalla, que es lo unico de esto que sigue siendo de la plataforma.
	public Module? Find(string name) => catalog?.Find(name);

	/// Los modulos instalados, para que la plataforma pueda recorrerlos sin ver el catalogo.
	public IReadOnlyList<Module> Modules => catalog?.Modules ?? [];

	/// La foto para la caratula, si esta plataforma la pide. Android pasa null y esto no hace
	/// nada -- alla la caratula pregunta en vivo.
	private async Task Fotografiar()
	{
		if (dispatcher is null) return;

		// LOCAL: para la intent que vive dentro de esta app. Sin App Group. La ruta tiene que
		// coincidir con FotoLocal.ruta en Intents/GnothiIntents.swift.
		if (paths.FotoLocal)
			await Snapshot.Write(dispatcher, Path.Combine(paths.Files, "instantanea"), log, stop.Token);

		// COMPARTIDA: para una app hermana (el spike de Swift). Puede no existir si el App Group
		// no esta aprovisionado, y eso no impide la local.
		if (paths.Instantaneas is not null)
			await Snapshot.Write(dispatcher, paths.Instantaneas, log, stop.Token);
	}

	public void Dispose()
	{
		stop.Cancel();
		watch?.Dispose();
		stop.Dispose();
	}

	// ── EL PUENTE, del lado que no sabe de plataforma ───────────────────────────────────
	//
	// La pagina llama window.prompt('gnothi', json). Quien traduce ese prompt a una llamada
	// es la plataforma; QUE se hace con el pedido es esto, y es identico en los dos.
	public string Handle(string json)
	{
		try
		{
			JsonElement req = Json.Parse(json);
			string op = req.GetProperty("op").GetString() ?? "";
			log($"bridge: {op}");

			return op switch
			{
				"modules" => StateJson(),
				"setTool" => SetConsent(
					req.GetProperty("module").GetString() ?? "",
					req.GetProperty("tool").GetString(),
					req.GetProperty("enabled").GetBoolean()),
				"setModule" => SetConsent(
					req.GetProperty("module").GetString() ?? "",
					null,
					req.GetProperty("enabled").GetBoolean()),
				"setProvider" => SetProvider(
					req.GetProperty("name").GetString() ?? "",
					req.GetProperty("url").GetString() ?? "",
					req.GetProperty("model").GetString() ?? ""),
				"setKey" => SetKey(req.GetProperty("key").GetString() ?? ""),
				"clearKey" => ClearKey(),
				"ask" => RunAsk(req),

				// LOS DOS VERBOS DEL ALTA. Estaban interceptados en cada plataforma porque la
				// propuesta era estado de su pantalla; ahora que vive aca, los puentes se
				// quedan con lo que de verdad necesita pantalla y nada mas.
				"altaAceptar" => AcceptEnrollment(),
				"altaDescartar" => DiscardEnrollment(),

				// THE LINK, PASTED BY HAND. The first way in that starts INSIDE Gnothi: until
				// now every enrollment began outside, on the institution's page, and the
				// "Agregar módulo" card only explained. It is also what makes the enrollment
				// testable on a phone while go.gnothi.app does not resolve.
				"alta" => EnrollPasted(req.GetProperty("url").GetString() ?? ""),

				_ => "{}",
			};
		}
		catch (Exception e)
		{
			log($"bridge: {e.GetType().Name}: {e.Message}");
			return "{}";
		}
	}

	private string RunAsk(JsonElement req)
	{
		Ask(req.GetProperty("id").GetString() ?? "", req.GetProperty("question").GetString() ?? "");
		return "\"ok\"";
	}

	// ── lo que la pagina pide ───────────────────────────────────────────────────────────

	// El estado como la UI lo necesita: los modulos, su prosa, donde esta cada switch, y el
	// peso real del journal -- medido desde afuera, que es el verbo propio del Host.
	public string StateJson()
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
			catch { /* un modulo sin journal todavia no pesa nada */ }

			return new Dictionary<string, object?>
			{
				["name"] = m.Name,
				["title"] = m.Manifest.Title,
				["institution"] = m.Manifest.Institution,
				["icon"] = Icon(m.Name),
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

		return Json.Write(new Dictionary<string, object?>
		{
			["modules"] = modules,
			["settings"] = Settings(),
		});
	}

	// EL SWITCH ESCRIBE EL ARCHIVO DE LA PERSONA. No una bandera en memoria: el mismo
	// installation.json que lee el Host de escritorio, que vigila el watcher y desde el que
	// recarga el catalogo. La UI no tiene estado propio -- repinta desde lo que diga el
	// archivo, que es por lo que una edicion externa tambien aparece en pantalla.
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
			Path.Combine(m.Directory, "installation.json"),
			JsonSerializer.Serialize(updated, HostJson.Legible.Installation));

		// Aplicado YA, no cuando se calme la rafaga de 300ms del watcher: el switch que la
		// persona acaba de mover tiene que contestar de inmediato. El watcher dispara igual --
		// ReloadConsent no encuentra nada mas que cambiar y se queda callado.
		catalog!.ReloadConsent();
		log($"consent: {module}{(tool is null ? "" : "." + tool)} -> {(enabled ? "on" : "off")}");

		// Y LA FOTO SE REESCRIBE DESDE ACA. Esta linea parecia redundante -- ConsentWatch.Changed
		// ya llama a Fotografiar() -- y no lo es: es la unica que corre cuando la persona mueve el
		// switch DENTRO de la app, que es el caso normal.
		//
		// Por que el watcher no alcanza, y cuesta verlo: la linea de arriba ya consumio el cambio.
		// Cuando el watcher despierta vuelve a llamar ReloadConsent(), que ahora devuelve CERO
		// cambios, y ConsentWatch corta ahi -- «if (changes.Count == 0) continue» --. O sea que
		// Changed no dispara y Fotografiar() no corre. La foto quedaba con lo de antes hasta el
		// siguiente arranque de la app, y Siri y el Atajo seguian contestando por una tool que la
		// persona ya habia apagado. El watcher sigue haciendo falta para el otro caso: que el
		// archivo lo edite algo de afuera.
		//
		// EN SEGUNDO PLANO a proposito: Fotografiar() pide cada hecho por Dispatcher.Serve, o sea
		// que despierta modulos, y el switch tiene que contestar ya.
		//
		// Y ENCADENADA, no suelta: tres switches seguidos serian tres escrituras peleando por el
		// mismo archivo temporal. Encadenarlas las serializa sin bloquear a nadie.
		fotos = fotos.ContinueWith(_ => Fotografiar(), TaskScheduler.Default).Unwrap();

		return StateJson();
	}

	private Dictionary<string, object?> Settings()
	{
		(string name, string url, string model) = Provider();
		return new Dictionary<string, object?>
		{
			["name"] = name,
			// SIN URL NO HAY HOST, y el proveedor de este aparato no tiene servidor. `new Uri("")`
			// lanza, y como esto se serializa DENTRO de la respuesta que la pagina espera, la
			// excepcion no se veia como error: la pagina solo repinta si le vuelve estado, asi
			// que elegir el modelo local guardaba bien y no se reflejaba nunca. Y TryCreate
			// tambien cubre la url que la persona escribe a mano en "Otro".
			["provider"] = string.IsNullOrWhiteSpace(url)
				? "este aparato"
				: Uri.TryCreate(url, UriKind.Absolute, out Uri? u) ? u.Host : url,

			// Para que la pagina no pida llave donde no hace falta.
			["local"] = name == ProveedorLocal,
			["model"] = model,
			// SI HAY llave para ESTE proveedor, nunca la llave.
			["keyPresent"] = File.Exists(KeyPath),
			["root"] = catalog?.Root ?? "",
		};
	}

	// EL PROVEEDOR, ELEGIDO POR LA PERSONA. Todos los presets hablan la forma de OpenAI, que es
	// lo que habla ModelOpenAiCompatible -- asi que "usa ChatGPT en vez de eso" es un archivo
	// con una URL y un nombre de modelo, no codigo. Las constantes de arriba quedan solo como
	// el default cuando no se eligio nada.
	private string CfgPath => Path.Combine(paths.Files, "model.cfg");

	// QUIEN DECIDE LA IMPLEMENTACION. Antes no la decidia nadie: se construia siempre
	// ModelOpenAiCompatible y `Provider()` solo cambiaba la url. Elegir "Claude (Anthropic)" en
	// Ajustes mandaba un cuerpo con forma de OpenAI a una API que no la habla, y ModelClaude
	// -que existe- no se usaba nunca desde la app.
	private IModel Modelo(string proveedor, string url, string model)
	{
		if (proveedor == ProveedorLocal)
			return modeloLocal is not null
				? modeloLocal(Encuadre)
				: throw new InvalidOperationException("este aparato no ofrece modelo propio");

		// Por nombre y no por url: la url la puede editar la persona, el proveedor lo eligio.
		if (proveedor.Contains("Anthropic", StringComparison.OrdinalIgnoreCase))
			return new ModelClaude(KeyPath, model);

		return new ModelOpenAiCompatible(url, model, KeyPath);
	}

	// EL ENCUADRE, en un solo lugar. Viaja tal cual hasta el modelo: no es documentacion, es lo
	// unico que separa "te leo tus numeros" de "te diagnostico". Medido: un modelo capaz lo
	// respeta 7 de 7; el del telefono se le escapa 2 de 4 -ver Spikes/Lab.Cruce-.
	private const string Encuadre =
		"Sos el asistente de Gnothi. Los datos que devuelven las herramientas son los datos REALES " +
		"de la persona que te pregunta, leidos de los registros que sus instituciones le entregaron " +
		"y que viven en su propio telefono. No vienen de ningun servidor. Contestale sobre SUS " +
		"numeros, en su idioma, en prosa corrida y sin markdown. El veredicto clinico ya viene hecho " +
		"y no es tuyo: no diagnostiques ni recomendes tratamiento.";

	private (string name, string url, string model) Provider()
	{
		try
		{
			if (File.Exists(CfgPath))
			{
				JsonElement e = Json.Parse(File.ReadAllText(CfgPath));
				return (e.GetProperty("name").GetString() ?? "Groq",
						e.GetProperty("url").GetString() ?? ModelUrl,
						e.GetProperty("model").GetString() ?? ModelName);
			}
		}
		catch { /* una eleccion ilegible cae al default */ }

		// EL DEFAULT ES EL APARATO cuando el aparato puede. Es el unico proveedor donde el dato
		// no sale del telefono, asi que arrancar en otro seria elegir por la persona la opcion
		// menos privada. Si no hay modelo propio, el de siempre.
		return modeloLocal is not null
			? (ProveedorLocal, "", "apple-on-device")
			: ("Groq", ModelUrl, ModelName);
	}

	private string SetProvider(string name, string url, string model)
	{
		File.WriteAllText(CfgPath, Json.Write(new Dictionary<string, string>
		{
			["name"] = name, ["url"] = url, ["model"] = model,
		}));
		log($"provider: {name} · {model}");
		return StateJson();
	}

	// LA LLAVE, ADMINISTRADA POR LA PERSONA. Esto es lo que hace real el BYOK: hasta ahora el
	// archivo lo ponia un desarrollador por un cable, que no es "trae tu propia llave" -- es
	// trae LA MIA. Cualquier llave del proveedor elegido sirve, free tier o paga: el archivo
	// no nota la diferencia.
	//
	// POR PROVEEDOR, a proposito: una llave de Groq no es una de OpenAI. Cambiar de proveedor
	// cambia cual archivo de llave se lee, asi que ir a ChatGPT y volver no pierde nada.
	//
	// Tres reglas, y se cumplen aca: vive SOLO en el area privada de la app, NUNCA se le manda
	// de vuelta a la pagina (StateJson lleva un booleano, no el valor), y el log registra el
	// evento sin la llave.
	private string KeyPath
	{
		get
		{
			string safe = new string(Provider().name.Where(char.IsLetterOrDigit).ToArray());
			return Path.Combine(paths.Files, $"key-{safe}.key");
		}
	}

	private string SetKey(string key)
	{
		key = key.Trim();
		if (key.Length > 0)
		{
			File.WriteAllText(KeyPath, key);
			log($"model key: saved ({key.Length} chars)");
		}
		return StateJson();
	}

	private string ClearKey()
	{
		if (File.Exists(KeyPath)) File.Delete(KeyPath);
		log("model key: cleared");
		return StateJson();
	}

	// UNA PREGUNTA, contestada por la Puerta 2: el mismo AgentLoop, el mismo dispatcher, el
	// mismo archivo de llave. Corre suelto; la traza y la respuesta caen en la pagina a
	// medida que pasan.
	private void Ask(string id, string question)
	{
		_ = Task.Run(async () =>
		{
			try
			{
				(string proveedor, string url, string model) = Provider();
				var loop = new AgentLoop(dispatcher!, Modelo(proveedor, url, model));

				string answer = await loop.Answer(question, line => Push("trace", id, line), stop.Token);
				Push("answer", id, answer);
			}
			catch (Exception e)
			{
				log($"ask failed: {e.GetType().Name}: {e.Message}");
				Push("fail", id, $"{e.GetType().Name}: {e.Message}");
			}
		});
	}

	public void PushState() => Push("state", "", StateJson());

	/// DESPUES DE INSTALAR UN MODULO, y lo hacen las dos plataformas llamando aca.
	///
	/// El catalogo se relee ENTERO: es la misma ruta por la que un cambio hecho desde afuera
	/// repinta la pantalla. Una sola fuente de verdad, sus archivos -- y no un catalogo al que
	/// se le agrega una entrada a mano, que es como las dos verdades empiezan.
	///
	/// Devuelve los warnings de la carga para que quien llame los escriba en SU log, que es lo
	/// unico que cambia entre Android y iOS.
	public IReadOnlyList<string> ReloadAfterInstall()
	{
		IReadOnlyList<string> warnings = catalog?.Load() ?? [];
		PushState();

		// Y SE COMPRUEBA EL MODULO NUEVO, en segundo plano. Sin esta linea un modulo recien
		// instalado no se comprobaria hasta el siguiente arranque de la app -- que es justo
		// cuando MENOS sirve, porque el momento de descubrir que declara mal es antes de
		// ofrecerselo al asistente.
		_ = Task.Run(() => VerifyNewModules(stop.Token));

		return warnings;
	}

	/// Cuantos modulos ve el Host ahora mismo. Para poder decirlo en el log despues de un alta.
	public int ModuleCount => catalog?.Modules.Count ?? 0;

	/// El titulo que la persona leyo en la propuesta, para poder nombrarlo al confirmar.
	public string TitleOf(string module) =>
		catalog?.Modules.FirstOrDefault(m => m.Name == module)?.Manifest.Title ?? module;

	private void Push(string kind, string id, string payload) => push(kind, id, payload);

	/// El javascript que la plataforma tiene que evaluar para empujarle algo a la pagina.
	/// Se arma aca -y no en cada plataforma- porque el que serializa mal las comillas rompe
	/// la pagina sin un error visible en ningun lado.
	public static string PushScript(string kind, string id, string payload) =>
		$"gn.push({Json.Write(kind)},{Json.Write(id)},{Json.Write(payload)})";

	// HACER LOCALES LAS RUTAS DEL installation.json. Solo donde la plataforma lo pide.
	//
	// EL PROBLEMA, y es de iOS nada mas: armar-paquete.ps1 escribe rutas absolutas de Android
	// -/data/user/0/com.ncubo.gnothi.host/files/...- porque ahi se pueden saber al armar el
	// paquete. En iOS el contenedor lleva un GUID que CAMBIA EN CADA INSTALACION, asi que no
	// existe ninguna ruta que se pueda dejar escrita: la unica valida es la que el proceso
	// averigua ya corriendo.
	//
	// SE TOCAN DOS CAMPOS Y NADA MAS, y los dos son del Host, no del modulo:
	//
	//   shell.arguments[0]   donde esta lo que se carga  se conserva la HOJA de la ruta, y el
	//                                                  molde depende del tipo de cascara:
	//                                                  <dir>/bin/<dll> un modulo cargo
	//                                                  <dir>/web       uno hospedado
	//   grantedFolder        que carpeta se le otorga  se conserva el negocio
	//
	// Los argumentos DESPUES del primero no se tocan: no son direcciones de esta maquina.
	//
	// El manifiesto NO se toca nunca: eso es lo que declara la institucion. Y de la instalacion
	// se toca solo lo que es una direccion de esta maquina; el interruptor, las tools apagadas
	// y la caratula quedan exactamente como venian, porque esas son decisiones de la persona.
	//
	// SERIA MEJOR QUE ESTO NO EXISTIERA. La solucion de fondo es que installation.json admita
	// rutas RELATIVAS y que el Host las resuelva contra la carpeta del modulo -- ahi el archivo
	// de la persona deja de tener adentro una direccion de una maquina, que es una propiedad
	// que vale en las tres plataformas. Se dejo para despues a proposito: cambia el nucleo, y
	// el nucleo hay que volver a correrlo en el A54 antes de darlo por bueno.
	private void Localizar(string root, string journals)
	{
		if (!Directory.Exists(root)) return;

		foreach (string dir in Directory.GetDirectories(root))
		{
			string file = Path.Combine(dir, "installation.json");
			if (!File.Exists(file)) continue;

			try
			{
				string negocio = Path.GetFileName(dir);
				Installation vieja = JsonSerializer.Deserialize(File.ReadAllText(file), HostJson.Default.Installation)!;

				// La HOJA de la ruta vieja es lo unico que se rescata: QUE se carga lo decidio
				// quien armo el paquete, y eso no cambia por mudarse de aparato. Donde esta, si.
				string hoja = vieja.Shell.Arguments.Count > 0 ? Path.GetFileName(vieja.Shell.Arguments[0]) : "";
				if (hoja.Length == 0) { log($"localizar: {negocio} no declara que cargar; se deja como esta"); continue; }

				// DOS FORMAS, Y CONFUNDIRLAS DEJA LA PANTALLA EN BLANCO SIN UN SOLO ERROR. Un
				// modulo cargo guarda un dll bajo bin/; uno HOSPEDADO guarda su bundle publicado
				// en web/, al lado del installation.json. Componer bin/web da una ruta que no
				// existe, BundleServer.BaseOf no encuentra el index.html y devuelve "/" por
				// omision, y el bundle -publicado para /app/- se sirve en la raiz: 404 en todo,
				// window.gnothi nunca se define, y la pantalla queda en blanco. Medido en el
				// simulador el 2026-09-07 con los cuatro modulos del paquete.
				bool esWeb = string.Equals(vieja.Shell.Type, "webview", StringComparison.OrdinalIgnoreCase);
				string local = esWeb ? Path.Combine(dir, hoja) : Path.Combine(dir, "bin", hoja);

				// Y LO QUE SIGUE AL PRIMERO SE PASA TAL CUAL. En un modulo hospedado el segundo
				// argumento es "<url del publicador>|<recurso>|<id>", que no es una direccion de
				// esta maquina sino lo que la institucion metio en su manifiesto. Reemplazar el
				// array entero se lo comia, y el cartero quedaba sin publicador al que pedirle.
				Installation nueva = vieja with
				{
					Shell = vieja.Shell with { Arguments = [local, .. vieja.Shell.Arguments.Skip(1)] },
					GrantedFolder = Path.Combine(journals, negocio),
				};

				File.WriteAllText(file, JsonSerializer.Serialize(nueva, HostJson.Legible.Installation));
				log($"localizar: {negocio} -> {nueva.GrantedFolder}");
			}
			catch (Exception e)
			{
				// Un modulo que no se pudo localizar va a contestar "no hay datos", y eso se ve
				// en pantalla. Tumbar el arranque entero por uno solo seria peor.
				log($"localizar: {Path.GetFileName(dir)} FALLO {e.GetType().Name}: {e.Message}");
			}
		}
	}

	// Copiar del area de descarga a la privada. Tonto a proposito: el Host no interpreta nada
	// de lo que copia.
	private static int Copy(string source, string target)
	{
		int n = 0;
		Directory.CreateDirectory(target);
		foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
		{
			string destination = file.Replace(source, target);
			Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
			File.Copy(file, destination, overwrite: true);
			n++;
		}
		return n;
	}
}
