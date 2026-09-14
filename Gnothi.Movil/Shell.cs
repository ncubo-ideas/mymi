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

	// THE ORIGINS ARE ASKED AGAIN WHEN GNOTHI COMES BACK TO THE FRONT, at most once an hour
	// (NotasGnothiHost/44 §7, step 2b): the card is fresh when she returns after a while, and an
	// institution hears from this phone no more than once an hour however often she switches apps.
	public static readonly TimeSpan ReviewEvery = TimeSpan.FromHours(1);
	private readonly ReviewSchedule reviews = new(ReviewEvery);

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

	// EL ICONO POR RUBRO, no por nombre de modulo.
	//
	// ESTE SWITCH DECIA ANTES 'examenes', 'facturas' y 'compras', con un comentario que aclaraba
	// que el icono NO venia del manifiesto a proposito, porque era asunto del Host. La posicion
	// era defendible y el costo se veia: el Host tenia que CONOCER a cada institucion por nombre
	// para pintarla, asi que un modulo de una institucion nueva entraba anonimo -- y eso
	// contradice el alta dinamica, donde agregar un modulo son dos json y no dos json y un
	// release del Host. El concesionario y la estacion de servicio, que ya estaban en el repo,
	// caian los dos a la misma caja.
	//
	// EL CORTE NUEVO: la institucion declara su RUBRO -una lista cerrada, en About.Known- y el
	// Host elige el dibujo. La institucion no elige el icono; elige de que negocio es, que es un
	// hecho sobre ella y no una decision de diseno. Y el que sigue eligiendo el dibujo es el
	// Host, que es lo que la posicion anterior queria proteger.
	//
	// SIGUE SIENDO UN EMOJI porque su unico consumidor es la FOTO -- la instantanea que lee el
	// intent de Siri, que no puede pintar un svg. La cara del telefono usa el logo de la
	// institucion si lo hay, y si no un dibujo propio por rubro.
	public static string Icon(Gnothi.Host.Manifest m) => (m.About?.Category ?? "") switch
	{
		"laboratorio" or "salud"        => "\U0001FA7A",   // estetoscopio
		"supermercado"                  => "\U0001F6D2",   // carrito
		"comercio"                      => "\U0001F9FE",   // recibo
		"banca" or "seguros"            => "\U0001F3E6",   // banco
		"combustible"                   => "⛽",       // surtidor
		"taller"                        => "\U0001F527",   // llave
		"telecom"                       => "\U0001F4F1",   // telefono
		"educacion"                     => "\U0001F393",   // birrete
		"gobierno"                      => "\U0001F3DB",   // edificio
		_                               => "\U0001F4E6",   // caja
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

		// THE KEYS OF WHAT IS INSTALLED, against what their origins say now. In the background: it
		// is network, it is a warning, and the screen is already painted.
		_ = Task.Run(() => ReviewOrigins(stop.Token));

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
	// asistente, no cuando ella ya pregunto. Es el cuarto pilar aplicado a si mismo -Mymi
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

		// A NEW VERSION THAT DOES NOT ANSWER GOES BACK (44 §3.5): while the update kept the
		// previous version parked next to it, a failed check puts that one back and the catalog
		// is reloaded. Collected here and applied after the loop, which is over the catalog's list.
		List<string> rolledBack = [];

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

				// SALVO que sea una version nueva con la anterior todavia al lado: entonces la
				// que no contesta se va y vuelve la que contestaba. La comprobacion de la que
				// vuelve ya esta hecha (su marca viajo con la carpeta), asi que no se repite.
				if (ModuleEnrollment.Rollback(catalog.Root, m.Manifest.Module, log))
				{
					CloseModule?.Invoke(m.Manifest.Module);
					rolledBack.Add(m.Manifest.Module);
				}
			}
		}

		if (rolledBack.Count > 0)
		{
			foreach (string w in catalog.Load()) log($"warning: {w}");
			PushState();
			foreach (string module in rolledBack)
				PushEnrollment("error", $"La versión nueva de '{TitleOf(module)}' no contestó; volvió la anterior.");
		}
	}

	// WHAT AN INSTALLED MODULE'S ORIGIN SAYS NOW ABOUT THE KEY IT WAS ACCEPTED WITH. A signature
	// is checked when a package arrives; a revocation happens later, to something already here,
	// and the only way to learn of it is to ask. So every launch, for each module that came signed
	// and whose manifest names an origin, the origin's well-known is fetched and its retired keys
	// compared with the pin. A hit is written into the pin (RevokedAt) -nothing else in the file
	// moves: the switches are the person's- and the card says it. The screen is repainted from
	// here on purpose: the consent watcher only wakes for a switch that moved, and no switch did.
	//
	// AND, WITH THE SAME ANSWER, WHETHER THE ORIGIN PUBLISHES SOMETHING NEWER (NotasGnothiHost/44
	// §4): the list carries each module's seal and version; when the published one is newer than
	// what is installed the note goes into installation.json and the card says "hay una versión
	// nueva". A note, never an action: nothing downloads or installs until she taps it. When the
	// installed package matches what is published again, the note is cleared.
	private async Task ReviewOrigins(CancellationToken ct)
	{
		if (catalog is null) return;
		DateTimeOffset started = DateTimeOffset.UtcNow;
		if (!reviews.TryBegin(started)) return;
		try
		{
			bool changed = await ReviewEachOrigin(ct);
			if (changed) PushState();
		}
		finally
		{
			reviews.End(DateTimeOffset.UtcNow);
		}
	}

	// GNOTHI CAME BACK TO THE FRONT -or a module was closed and the home is on screen again-: ask
	// the origins again if the last time was long enough ago. Cheap to call from every lifecycle
	// hook: the schedule says no when it is not due -the launch review counts- and never runs two
	// at once. The line in the log says which of the two happened.
	public void ReviewOriginsIfDue()
	{
		// Before the launch review has begun there is nothing to repeat -and on Android OnResume
		// fires right after OnCreate, while the catalog may still be loading. The launch owns
		// the first one; this only ever repeats it.
		if (catalog is null || stop.IsCancellationRequested || !reviews.Started) return;
		DateTimeOffset now = DateTimeOffset.UtcNow;
		if (!reviews.IsDue(now))
		{
			log($"revisión: la última consulta a los orígenes fue hace poco; la próxima, en {Math.Ceiling(reviews.Until(now).TotalMinutes):0} min");
			return;
		}
		log("revisión: se vuelve a consultar a los orígenes");
		_ = Task.Run(() => ReviewOrigins(stop.Token));
	}

	// One pass over the installed modules that declare an origin. True when something was marked
	// or noted -a retired key, a newer package, or a note cleared- and the screen must repaint.
	private async Task<bool> ReviewEachOrigin(CancellationToken ct)
	{
		if (catalog is null) return false;
		bool changed = false;

		foreach (Module m in catalog.Modules)
		{
			Uri? origin = ModuleEnrollment.DeclaredOrigin(m.Manifest.Origin);
			if (origin is null) continue;

			try
			{
				string? published = await ModuleEnrollment.FetchWellKnown(origin, ct);
				if (published is null) continue;
				string installationPath = Path.Combine(m.Directory, "installation.json");

				// The key it was accepted with, against the keys the origin retired.
				if (m.Installation.Signer is { RevokedAt: null } signer && ModuleEnrollment.IsRevoked(published, signer.Public))
				{
					string? at = ModuleEnrollment.MarkRevoked(installationPath, DateTimeOffset.UtcNow);
					if (at is not null)
					{
						m.MarkSignerRevoked(at);
						changed = true;
						log($"revocación: '{m.Name}': {origin.Host} retiró la llave '{signer.KeyId}' con la que se aceptó");
					}
				}

				// The package installed, against the one the origin publishes now.
				AvailableUpdate? newer = ModuleEnrollment.UpdatePublished(published, m.Name,
					ModuleEnrollment.InstalledSeal(m.Directory), m.Manifest.Publisher?.Version, origin, DateTimeOffset.UtcNow);
				if (ModuleEnrollment.WriteUpdateNote(installationPath, newer))
				{
					m.NoteUpdate(newer);
					changed = true;
					log(newer is null
						? $"actualización: '{m.Name}' está al día con {origin.Host}"
						: $"actualización: '{m.Name}': {origin.Host} publica {(newer.Version.Length > 0 ? newer.Version : "otro paquete")}; instalada {(m.Manifest.Publisher?.Version is { Length: > 0 } v ? v : "sin versión")}");
				}
			}
			catch (Exception ex)
			{
				log($"revisión: no se pudo consultar a {origin.Host} por '{m.Name}': {ex.GetType().Name}: {ex.Message}");
			}
		}

		return changed;
	}

	// SHE TAPPED «HAY UNA VERSIÓN NUEVA»: the package the origin publishes is fetched from the address
	// the note kept, with its seal, and goes through the whole examination like a link that landed.
	// The sheet is the update sheet, and only her yes there installs anything.
	// QUITAR UN MODULO ES DECISION DE LA PERSONA, y hasta 2026-09-11 no tenia como tomarla: podia
	// apagarlo, pero no sacarlo del telefono, y «volver a instalar» era borrar Mymi entera. Dos
	// cosas distintas se van o se quedan: la app de la institucion -Modulos/<modulo>, y su
	// .anterior si quedo de una actualizacion- se va siempre; la bitacora -journals/<modulo>, que
	// es tambien la carpeta concedida- es de la persona y se queda salvo que ella lo pida. Un alta
	// posterior la encuentra ahi, igual que una actualizacion. El modulo vivo se cierra antes,
	// por la misma razon que en una actualizacion: su WebView esta parado sobre la carpeta.
	private string RemoveModule(string module, bool journalToo)
	{
		Module? m = Find(module);
		if (m is null) return StateJson();

		if (CloseModule?.Invoke(module) == true)
			log($"quitar: '{module}' estaba vivo; se cerró antes de borrarlo");

		string folder = Path.Combine(paths.Files, "Modulos", module);
		string previous = folder + ModuleCatalog.PreviousSuffix;
		string journal = Path.Combine(paths.Files, "journals", module);

		if (Directory.Exists(previous)) Directory.Delete(previous, recursive: true);
		if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
		if (journalToo && Directory.Exists(journal)) Directory.Delete(journal, recursive: true);

		log($"quitar: '{module}' fuera del teléfono" + (journalToo ? ", con su bitácora" : "; la bitácora se queda"));

		foreach (string w in ReloadAfterInstall()) log($"warning: {w}");
		return StateJson();
	}

	private string StartUpdate(string module)
	{
		Module? m = Find(module);
		if (m?.Installation.Update is not { } update || update.Bundle.Length == 0) return "{}";

		log($"alta: actualización de '{module}' pedida desde la tarjeta: {update.Bundle}");
		_ = Enroll(update.Bundle, EnrollmentSource.Link, update.Seal.Length > 0 ? update.Seal : null, stop.Token);
		return "\"ok\"";
	}

	// ---------------------------------------------------------------- el alta desde el enlace

	// LA PROPUESTA EN CURSO, esperando que la persona diga si o no. UNA sola a la vez: si llega
	// otro enlace mientras hay una en pantalla, la anterior se descarta -- no se acumulan
	// decisiones que ella no vio.
	//
	// ESTO VIVE ACA Y NO EN LA PLATAFORMA porque nada de esto es de una plataforma: bajar un
	// zip, leer un manifiesto, mostrar una propuesta y esperar. Estuvo en MainActivity hasta
	// que hubo que hacer la version de iOS, y ahi se vio: el ViewController tuvo que copiarlo
	// entero. Dos copias de la pantalla mas importante que Mymi le muestra a la persona.
	private EnrollmentProposal? proposal;
	private ModuleEnrollment? enrolling;

	/// CLOSE A MODULE IF IT IS OPEN, and say whether it was. An update moves the module's folder
	/// aside while its WebView may be sitting on it (44 §3.6), so the screen has to let go first.
	/// The platform wires it where it registers its host of WebViews -Android and iOS each have
	/// one-; the Shell only names it, so that this file compiles wherever the suite compiles it.
	public Func<string, bool>? CloseModule { get; set; }

	/// LO QUE LLEGA DEL ENLACE DE LA INSTITUCION. Baja el bundle y lee su manifiesto; NO
	/// instala nada. La persona decide despues, y hasta entonces el bundle vive en un temporal.
	///
	/// Lo llaman las dos plataformas cuando su enlace aterriza: un App Link en Android, un
	/// universal link en iOS. Lo unico que cambia es como llega la URL.
	public Task Enroll(string url, CancellationToken ct = default)
	{
		// The platforms hand over the link as it landed -go.gnothi.app/alta?bundle=…&h=…- or
		// just the bundle's address. Either way the seal, when there is one, travels with it.
		EnrollmentLink link = ModuleEnrollment.ParseLink(url);
		return Enroll(link.Bundle, EnrollmentSource.Link, link.Seal, ct);
	}

	/// THE PACKAGE ARRIVED AS A FILE: opened from Files or Downloads, shared from another app, or
	/// picked from inside Mymi. The platform read the bytes and knows the file's name; nothing
	/// else about where it came from is known, and the sheet says so -unless the package claims an
	/// origin that publishes exactly this manifest, which the enrollment asks.
	public Task EnrollFile(byte[] zip, string fileName, CancellationToken ct = default)
		=> Propose(e => e.ExamineFile(zip, fileName, ct, PushEnrollment), fileName);

	// ── SEVERAL FILES AT ONCE ────────────────────────────────────────────────────────────

	// WHAT SHE PICKED, when she picked more than one: the bytes wait here, with the quick look
	// Mymi took at each, until she chooses which one to examine for real. The list on the screen
	// is repainted as she installs them one by one; a single file skips all this and goes straight
	// to its proposal, as before.
	private List<(byte[] Bytes, PackagePeek Peek)>? picked;
	private readonly HashSet<int> pickedInstalled = [];
	private readonly Dictionary<int, string> pickedProblems = [];
	private int pickedChosen = -1;

	/// THE PICKER LET HER CHOOSE SEVERAL, so first the screen shows what Mymi found in them -which
	/// are modules, of whom, which are not- and she chooses which to install. Each choice runs the
	/// whole examination, origin and signature included; the list only looks inside the zips.
	public Task EnrollFiles(IReadOnlyList<(byte[] Bytes, string Name)> files, CancellationToken ct = default)
	{
		if (files.Count == 1) return EnrollFile(files[0].Bytes, files[0].Name, ct);

		picked = [.. files.Select(f => (f.Bytes, ModuleEnrollment.Peek(f.Bytes, f.Name)))];
		pickedInstalled.Clear();
		pickedProblems.Clear();
		pickedChosen = -1;
		log($"alta: la persona eligió {files.Count} archivos; {picked.Count(p => p.Peek.IsModule)} son módulos");
		PushPicked();
		return Task.CompletedTask;
	}

	// The list as the page paints it: one row per file, in the person's words. `problema` is why a
	// row cannot be chosen -not a module, rejected by the inspection, or refused when she tried-.
	private void PushPicked()
	{
		if (picked is null) return;

		var rows = picked.Select((p, i) =>
		{
			PackagePeek peek = p.Peek;
			string problem = pickedProblems.TryGetValue(i, out string? refused) ? refused
				: peek.Manifest is null ? "No es un módulo de Mymi: no trae un manifiesto legible."
				: peek.Inspection is { Rejected: true } inspection ? "Mymi no lo instalaría: " + string.Join("; ", inspection.Rejections)
				: "";
			return new
			{
				indice = i,
				archivo = peek.FileName,
				bytes = peek.Bytes,
				modulo = peek.Manifest?.Module ?? "",
				titulo = peek.Manifest?.Title ?? "",
				institucion = peek.Manifest?.Institution ?? "",
				nombreCorto = peek.Manifest?.Brand?.ShortName ?? "",
				eslogan = peek.Manifest?.Brand?.Slogan ?? "",
				color = Brand.ValidColor(peek.Manifest?.Brand?.Color) ?? "",
				categoria = peek.Manifest?.About?.Category ?? "",
				firmado = peek.Signature == SignatureState.Valid,
				problema = problem,
				instalado = pickedInstalled.Contains(i),
				yaEstaba = peek.Manifest is not null && !pickedInstalled.Contains(i) && Find(peek.Manifest.Module) is not null,
			};
		}).ToArray();

		Push("hallazgos", "", JsonSerializer.Serialize(new { archivos = rows }));
	}

	// She chose one of the list: the whole examination, as if she had opened that file alone.
	private string ChoosePicked(int index)
	{
		if (picked is null || index < 0 || index >= picked.Count) return "{}";
		pickedChosen = index;
		_ = EnrollFile(picked[index].Bytes, picked[index].Peek.FileName, stop.Token);
		return "\"ok\"";
	}

	// She is done with the list: the bytes go, nothing else was kept.
	private string ClosePicked()
	{
		picked = null;
		pickedInstalled.Clear();
		pickedProblems.Clear();
		pickedChosen = -1;
		return "\"ok\"";
	}

	// After a proposal from the list ends -installed, declined or refused- the list comes back with
	// that row marked, so she can go on with the next one.
	private void PickedResolved(bool installed, string? refusal = null)
	{
		if (picked is null) return;
		if (pickedChosen >= 0)
		{
			if (installed) pickedInstalled.Add(pickedChosen);
			if (refusal is not null) pickedProblems[pickedChosen] = refusal;
		}
		pickedChosen = -1;
		PushPicked();
	}

	/// Something the platform could not turn into an enrollment -a picked file that could not be
	/// read- said on the screen, in the person's words. Same strip as any failed enrollment.
	public void ReportEnrollmentError(string message) => PushEnrollment("error", message);

	private Task Enroll(string bundle, EnrollmentSource source, string? seal, CancellationToken ct)
		=> Propose(e => e.Examine(bundle, source, seal, ct, PushEnrollment), bundle);

	// ONE ENTRY FOR EVERY WAY IN; only how the bytes are obtained differs, and the provenance
	// that comes back travels to the sheet. `what` is what to blame in the log: an address, a file.
	private async Task Propose(Func<ModuleEnrollment, Task<EnrollmentProposal>> examine, string what)
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
			proposal = await examine(enrolling);

			log($"alta: propuesta de '{proposal.Manifest.Module}' servida por {proposal.ServedBy}");
			PushProposal(proposal);
		}
		catch (Exception ex)
		{
			// UN ALTA QUE FALLA TIENE QUE DECIRLO, Y EN LA PANTALLA. Cuando esto solo escribia
			// en el log, la persona tocaba el boton de su institucion, volvia a Mymi y no
			// pasaba nada, sin ninguna pista de por que.
			log($"alta: no se pudo leer el módulo de {what}: {ex.GetType().Name}: {ex.Message}");
			PushEnrollment("error", InPlainWords(ex));
			PickedResolved(installed: false, refusal: InPlainWords(ex));
		}
	}

	// LA PERSONA ACEPTO. Recien aca el modulo existe.
	// `acceptNewPublisher`: on an update whose manifest moved the publisher's endpoint, whether she
	// said yes to the new one on the sheet. Otherwise the endpoint Mymi fixed stays.
	private string AcceptEnrollment(bool acceptNewPublisher = false)
	{
		if (proposal is null || enrolling is null) return "{}";

		try
		{
			// ON AN UPDATE the module may be open: its WebView sits on the folder about to move
			// aside. It is evicted first; opening it again loads the new version (44 §3.6).
			enrolling.BeforeReplacing = m =>
			{
				if (CloseModule?.Invoke(m) == true)
					log($"alta: '{m}' estaba abierto; se cerró antes de reemplazarlo");
			};

			string module = enrolling.Accept(proposal, acceptNewPublisher);
			proposal = null;

			// El catalogo se relee entero -- y con eso se comprueba el modulo nuevo.
			foreach (string w in ReloadAfterInstall()) log($"warning: {w}");
			log($"alta: '{module}' quedó instalado; {ModuleCount} módulos");

			PushEnrollment("listo", $"Listo: ya tenés '{TitleOf(module)}' en Mymi.");
			PickedResolved(installed: true);
		}
		catch (Exception ex)
		{
			log($"alta: no se pudo instalar: {ex.GetType().Name}: {ex.Message}");
			PushEnrollment("error", InPlainWords(ex));
			PickedResolved(installed: false, refusal: InPlainWords(ex));
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
		PickedResolved(installed: false);
		return "\"ok\"";
	}

	// The page brought a link: pasted by the person, or read off the institution's code. It may
	// be the link to Mymi's domain with the bundle and its seal as parameters, or the bundle's
	// address itself; both are accepted. Fire and forget, like RunAsk: the page hears back
	// through the same "alta"/"propuesta" pushes as a link that landed.
	private string EnrollFromPage(JsonElement req)
	{
		string text = req.GetProperty("url").GetString() ?? "";
		bool scanned = req.TryGetProperty("via", out JsonElement via) && via.GetString() == "qr";
		EnrollmentSource source = scanned ? EnrollmentSource.Qr : EnrollmentSource.PastedLink;

		EnrollmentLink link = ModuleEnrollment.ParseLink(text);
		if (link.Bundle.Length == 0)
		{
			PushEnrollment("error", scanned ? "El código no traía ningún enlace." : "Pegá el enlace que te dio tu institución.");
			return "\"ok\"";
		}

		_ = Enroll(link.Bundle, source, link.Seal, stop.Token);
		return "\"ok\"";
	}

	// COMO SE CUENTA EL ALTA EN LA PANTALLA. Es lo primero que la persona ve hacer a Mymi por
	// ella -toco un boton en la pagina de su institucion y volvio aca- y era un silencio de
	// 12 MB. Cuatro estados: trabajando, listo, error, nada.
	private void PushEnrollment(string estado, string detalle) =>
		Push("alta", "", JsonSerializer.Serialize(new { estado, detalle }));

	// WHILE IT WORKS, with the numbers: how many bytes of how many came down, so the screen draws a
	// bar and says "4,1 de 12,6 MB" instead of spinning in silence over twelve megabytes. The
	// fraction is null before the server says how much is coming, and for the steps that are not
	// a download.
	private void PushEnrollment(EnrollmentProgress step) =>
		Push("alta", "", JsonSerializer.Serialize(new { estado = "trabajando", detalle = step.Text, progreso = step.Fraction, bytes = step.Bytes, total = step.Total }));

	// LO QUE LA INSTITUCION PROPONE, mas las dos cosas que decide Mymi y ella tiene derecho a
	// saber antes de aceptar: donde va a vivir el journal y contra que endpoint va a hablar.
	// LA CARA DE LA INSTITUCION EN LA FORMA QUE LA PANTALLA ESPERA: un objeto con logo y color,
	// o null cuando no declaro ninguno de los dos -- y entonces la tarjeta cae al dibujo de su
	// rubro, que es un modulo sin marca y es legitimo.
	//
	// EXISTE PARA LA PROPUESTA DEL ALTA, donde el modulo todavia no esta instalado: el bundle
	// esta desempaquetado en una carpeta temporal y no hay ningun Module al que preguntarle.
	private static string InstalledAt(Module m)
	{
		string at = m.Installation.Provenance?.At ?? "";
		if (at.Length > 0) return at;
		try { return File.GetLastWriteTimeUtc(Path.Combine(m.Directory, "installation.json")).ToString("yyyy-MM-ddTHH:mm:ssZ"); }
		catch { return ""; }
	}

	private static Dictionary<string, object?>? Marca(Gnothi.Host.Manifest manifest, string folder)
		=> BrandOf(Gnothi.Host.Module.LookOf(manifest, folder, out _));

	// THE BRAND AS THE PAGE EXPECTS IT: logo and color as before, plus the banner behind the card
	// and the typeface (2026-09-09), all as data URIs already through the Host's customs. Null when
	// the institution declared none of it, and the card falls to the drawing of its category.
	private static Dictionary<string, object?>? BrandOf(Gnothi.Host.Module.BrandLook look)
	{
		if (!look.Any) return null;
		return new Dictionary<string, object?>
		{
			["logo"] = look.Logo,
			["color"] = look.Color,
			["banner"] = look.Banner,
			["font"] = look.FontFamily.Length > 0
				? new Dictionary<string, object?> { ["family"] = look.FontFamily, ["regular"] = look.FontRegular, ["bold"] = look.FontBold }
				: null,
		};
	}

	private void PushProposal(EnrollmentProposal p) => Push("propuesta", "", JsonSerializer.Serialize(new
	{
		modulo = p.Manifest.Module,
		identidad = p.Manifest.Identity,
		titulo = p.Manifest.Title,
		institucion = p.Manifest.Institution,

		// EL LOGO SE LEE DE LA CARPETA TEMPORAL, que es donde el bundle ya esta desempaquetado y
		// esperando la decision de la persona. Pasa por la MISMA aduana que despues de instalado
		// -- no hay una version relajada para la pantalla del alta, que seria justo la peor de
		// las dos donde relajarla.
		marca = Marca(p.Manifest, p.Folder),
		eslogan = p.Manifest.Brand?.Slogan ?? "",
		categoria = p.Manifest.About?.Category ?? "",
		descripcion = p.Manifest.About?.Description ?? "",
		noHace = p.Manifest.About?.DoesNotDo ?? "",

		// LA PROCEDENCIA, Y LA UNICA COMPROBACION QUE GNOTHI PUEDE HACER SOLO. 'servido' es un
		// dato OBSERVADO -- de que host bajo el bundle -- y 'dominio' es lo que la institucion
		// DECLARA. Compararlos es el mismo patron que gnothi_verify_module: declarado contra
		// real. Cuando no coinciden, la pantalla lo dice ANTES del si.
		razonSocial = p.Manifest.Publisher?.Legal ?? "",
		cedula = p.Manifest.Publisher?.TaxId ?? "",
		dominio = p.Manifest.Publisher?.Domain ?? "",
		version = p.Manifest.Publisher?.Version ?? "",
		// A file came from no server: nothing to compare, and the sheet says so instead of painting
		// a verdict. Null is that third state, the same one Provenance.Matches gives the detail.
		coincide = p.Provenance.Source == EnrollmentSource.File ? null : (bool?)(p.Manifest.Publisher?.MatchesServer(p.ServedBy) ?? true),

		servido = p.ServedBy,
		publicador = p.Publisher,
		journal = p.JournalGoesTo,

		// LO QUE LA PANTALLA NO PODIA CONTESTAR: ya decia DONDE va a vivir el registro, y no
		// cuanto va a pesar, cada cuanto se actualiza ni por cuanto tiempo lo guardan -- que es
		// lo que la persona pregunta antes de aceptar.
		retencion = p.Manifest.Journal?.Retention ?? "",
		tamano = p.Manifest.Journal?.ExpectedSize ?? "",
		refresco = p.Manifest.Journal?.Updates ?? "",

		yaEstaba = p.AlreadyThere,

		// WHAT THIS PACKAGE IS NEXT TO WHAT SHE HAS (Gnothi.Host/UpdatePlan.cs), by name, so the
		// sheet says "update from 2.4.0 to 2.5.0" and lists the change instead of "you already
		// have this". Null the first time.
		actualizacion = p.Update.Kind == UpdateKind.Fresh ? null : new
		{
			tipo = p.Update.Kind switch
			{
				UpdateKind.Same => "misma",
				UpdateKind.Downgrade => "anterior",
				UpdateKind.SameVersionOtherContent => "otro-contenido",
				_ => "actualizacion",
			},
			versionada = p.Update.Versioned,
			versionInstalada = p.Update.InstalledVersion,
			versionNueva = p.Update.IncomingVersion,
			consultasNuevas = p.Update.NewTools.Select(t => new { nombre = t.Name, titulo = t.Title }).ToArray(),
			consultasQuitadas = p.Update.RemovedTools.Select(t => new { nombre = t.Name, titulo = t.Title }).ToArray(),
			capacidadesNuevas = p.Update.NewCapabilities,
			publicadorCambia = p.Update.PublisherChanged,
			publicadorInstalado = p.Update.InstalledPublisher,
			publicadorNuevo = p.Update.IncomingPublisher,
			origenCambia = p.Update.OriginChanged,
			origenInstalado = p.Update.InstalledOrigin,
			origenNuevo = p.Update.IncomingOrigin,
			registroCambia = p.Update.JournalChanged,
			sustratoCambia = p.Update.SubstrateChanged,
			sustratoNuevo = p.Update.IncomingSubstrate,
			hayCambios = p.Update.HasVisibleChanges,
		},
		tools = p.Manifest.Tools.Select(t => new { nombre = t.Name, titulo = t.Title, descripcion = t.Description }),

		// WHERE IT CAME FROM: which way in, which host answered, whether the package still
		// matches the seal the link carried, and how far that host could be tied to the
		// institution -the origin check, by name, so the sheet can say which piece failed.
		procedencia = new
		{
			origen = p.Provenance.Source switch
			{
				EnrollmentSource.Qr => "qr",
				EnrollmentSource.PastedLink => "pegado",
				EnrollmentSource.File => "archivo",
				_ => "enlace",
			},
			servido = p.Provenance.ServedBy,
			sello = p.Provenance.MatchesLink,
			respaldo = p.Provenance.Origin switch
			{
				OriginCheck.Verified => "verificado",
				OriginCheck.NoWellKnown => "sin-publicacion",
				OriginCheck.NotListed => "no-publicado",
				OriginCheck.NotDeclared => "sin-origen-declarado",
				OriginCheck.DeclaresAnother => "otro-origen",
				_ => "sin-origen",
			},
			seguro = p.Provenance.Secure,
			origenDeclarado = p.Provenance.DeclaredOrigin,

			// Whose key signed the package, by name, and since when Mymi knows that key.
			firma = p.Provenance.Signer switch
			{
				SignerCheck.Published => "publicada",
				SignerCheck.Pinned => "conocida",
				SignerCheck.Rotated => "rotada",
				SignerCheck.Unknown => "desconocida",
				SignerCheck.Changed => "cambiada",
				SignerCheck.Revoked => "retirada",
				_ => "sin-firma",
			},
			firmante = p.Provenance.SignerId,
			firmaDesde = p.Provenance.SignedSince,
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

		// The link carried the seal of the manifest it was made for, and the package served now
		// does not hash to it: what the address serves changed after the code was made. Before
		// the InvalidOperationException arm on purpose: it derives from it.
		PackageSealMismatchException =>
			"El paquete no es el que sellaba el enlace: cambió después de que se generó el código. Pedile a tu institución uno nuevo.",

		// Signed, and the signature does not hold: somebody touched the package after it was
		// signed. Not "broken": tampered, and that is the word.
		PackageSignatureInvalidException =>
			"El paquete viene firmado y la firma no cierra: lo tocaron después de firmarlo. Pedile a tu institución uno nuevo.",

		// The institution retired the key this package is signed with. Not installed, no sheet.
		PackageKeyRevokedException =>
			"El paquete viene firmado con una llave que tu institución retiró: no se instala. Pedile un paquete nuevo.",

		// Another institution's package under the name of a module she has: refused, no sheet.
		PackageCollisionException col =>
			$"Ya tenés un módulo llamado «{col.Module}», de {(col.InstalledInstitution.Length > 0 ? col.InstalledInstitution : "otra institución")}. "
			+ $"Este paquete dice ser de {(col.IncomingInstitution.Length > 0 ? col.IncomingInstitution : "otra institución")} y no se instala. Si es el mismo, pedile a tu institución su enlace actual.",

		// The origin contradicts the package: refused, no sheet. Which contradiction, in her words.
		PackageNotBackedException nb => nb.Origin == OriginCheck.DeclaresAnother
			? "El paquete dice ser de otro origen del que lo sirvió: no se instala. Pedile a tu institución su enlace."
			: "Tu institución no reconoce este paquete como suyo: no se instala. Pedile el enlace o el paquete actual.",

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
				"altaAceptar" => AcceptEnrollment(req.TryGetProperty("publicadorNuevo", out JsonElement pn) && pn.ValueKind == JsonValueKind.True),
				"altaDescartar" => DiscardEnrollment(),

				// THE LINK FROM INSIDE GNOTHI: pasted by hand, or read off the institution's
				// code by the page's own reader ("via": "qr"). Until 2026-09-08 every enrollment
				// began outside, on the institution's page, and the "Agregar módulo" card only
				// explained. It is also what makes the enrollment testable on a phone while
				// go.gnothi.app does not resolve.
				"alta" => EnrollFromPage(req),

				// THE LIST OF WHAT SHE PICKED: one row chosen -the whole examination follows- or
				// the list closed. The picker itself is the platform's; the list is not.
				"altaElegido" => ChoosePicked(req.GetProperty("indice").GetInt32()),
				"altaCerrarLista" => ClosePicked(),

				// THE CARD'S «HAY UNA VERSIÓN NUEVA»: fetch what the origin publishes and open the
				// update sheet. Her yes on that sheet is what installs, as with any package.
				"actualizar" => StartUpdate(req.GetProperty("module").GetString() ?? ""),
				"quitar" => RemoveModule(
					req.GetProperty("module").GetString() ?? "",
					req.TryGetProperty("journal", out JsonElement qj) && qj.ValueKind == JsonValueKind.True),

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

		// SOBRE UNA COPIA DE LA LISTA, Y NO SOBRE LA LISTA.
		//
		// Medido en el emulador el 2026-09-08: `bridge: InvalidOperationException: Collection
		// was modified; enumeration operation may not execute`. Aceptar un alta recarga el
		// catalogo -- ModuleCatalog.Load hace Clear() y vuelve a llenar la lista -- y si la
		// pagina pide el estado en ese instante, este recorrido estalla y la pantalla se queda
		// sin modulos.
		//
		// La carrera existia antes y era estrecha. Este metodo ahora LEE EL LOGO DE CADA MODULO
		// del disco, asi que tarda mas y la ventana se agrando: el sintoma aparecio recien
		// cuando la marca entro. Copiar la lista cuesta cuatro punteros y la cierra.
		var modules = catalog.Modules.ToArray().Select(m =>
		{
			long bytes = 0; int files = 0;
			try
			{
				foreach (FileInfo f in new DirectoryInfo(m.Installation.GrantedFolder)
					.EnumerateFiles("*", SearchOption.AllDirectories))
				{ bytes += f.Length; files++; }
			}
			catch { /* un modulo sin journal todavia no pesa nada */ }

			// LA CARA DE LA INSTITUCION, resuelta una vez por modulo: su `brand` si lo declaro,
			// y si no el manifest.webmanifest que su PWA ya trae en el bundle. Los dos caminos
			// pasan por la misma aduana.
			Module.BrandLook look = Module.LookOf(m.Manifest, m.Directory, out _);

			// LO QUE LA INSTITUCION DECLARO DE SI MISMA, para la pantalla.
			//
			// 'identity' es LA LINEA GRANDE y la resuelve el manifiesto -- Manifest.Identity --
			// y no esta pagina, porque hay tres caras y un fallback implementado tres veces es
			// un fallback que diverge. Antes esta linea la decidia un mapa NOMBRE del propio
			// html, que TAPABA a la institucion cuando el Host la conocia y la dejaba cruda
			// cuando no.
			//
			// 'logo' viaja como data: URI ya validado por la aduana del Host -- Module.LogoDataUri
			// --, no como ruta: la pagina se sirve como file:// y no puede leer otros file://, y
			// darle una ruta del modulo seria dejar que el modulo elija que archivo se abre.
			//
			// EL PESO DEL LOGO SE PAGA EN CADA REPINTADO, porque el estado se pide entero. Con
			// el tope de 64 KB y cuatro modulos son unos 350 KB en el peor caso y ~40 en el
			// tipico. Es la razon practica del tope, ademas de la de seguridad.
			return new Dictionary<string, object?>
			{
				["name"] = m.Name,
				["identity"] = m.Manifest.Identity,
				["title"] = m.Manifest.Title,
				["institution"] = m.Manifest.Institution,
				["icon"] = Icon(m.Manifest),
				["category"] = m.Manifest.About?.Category ?? "",
				// La cara de la institucion, resuelta por el Host: su `brand` si lo declaro, y
				// si no el manifest.webmanifest que su PWA ya trae en el bundle. Los dos
				// caminos pasan por la misma aduana.
				["shortName"] = m.Manifest.Brand?.ShortName ?? "",
				["slogan"] = m.Manifest.Brand?.Slogan ?? "",
				["brand"] = BrandOf(look),
				["description"] = m.Manifest.About?.Description ?? "",
				["doesNotDo"] = m.Manifest.About?.DoesNotDo ?? "",
				["publisher"] = new Dictionary<string, object?>
				{
					["domain"] = m.Manifest.Publisher?.Domain ?? "",
					["legal"] = m.Manifest.Publisher?.Legal ?? "",
					["taxId"] = m.Manifest.Publisher?.TaxId ?? "",
					["version"] = m.Manifest.Publisher?.Version ?? "",
					["released"] = m.Manifest.Publisher?.Released ?? "",
				},

				// EL ACTA DEL ALTA, para que el detalle pueda CONTRASTAR y no solo repetir.
				//
				// 'matches' viene en tres estados y no en dos: true, false y NULL cuando no hay
				// con que comparar -- un .gmod importado de un archivo no tiene servidor--. La
				// pantalla tiene que poder decir "no se puede saber" en vez de pintar un visto
				// bueno, que es exactamente el bug que esto arregla.
				// WHEN IT ARRIVED, for the order of the home: the most recently added module is the hero
				// and stays there until the next one arrives (Geaninna, 2026-09-09). The enrollment act
				// says when; a module that came in by other means falls back to its installation file.
				["installedAt"] = InstalledAt(m),
				["provenance"] = new Dictionary<string, object?>
				{
					["servedBy"] = m.Installation.Provenance?.ServedBy ?? "",
					["at"] = m.Installation.Provenance?.At ?? "",
					["version"] = m.Installation.Provenance?.Version ?? "",
					["matches"] = m.Installation.Provenance?.Matches(m.Manifest.Publisher),
				},
				["journal"] = new Dictionary<string, object?>
				{
					["retention"] = m.Manifest.Journal?.Retention ?? "",
					["expectedSize"] = m.Manifest.Journal?.ExpectedSize ?? "",
					["updates"] = m.Manifest.Journal?.Updates ?? "",
				},
				["enabled"] = m.Installation.Enabled,
				["journalBytes"] = bytes,
				["journalFiles"] = files,
				// The key it was accepted with, and whether its origin has since retired it.
				["signerKey"] = m.Installation.Signer?.KeyId,
				["signerRevokedAt"] = m.Installation.Signer?.RevokedAt,
				// What the origin publishes and she does not have yet, if Mymi saw it.
				["updateAvailable"] = m.Installation.Update is { } up
					? new Dictionary<string, object?> { ["version"] = up.Version, ["seenAt"] = up.SeenAt }
					: null,
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
		"Sos el asistente de Mymi. Los datos que devuelven las herramientas son los datos REALES " +
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
