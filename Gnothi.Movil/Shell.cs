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
	/// Instantanea.cs.
	/// </param>
	public sealed record Paths(
		string Files,
		string Incoming,
		bool ReescribirRutas = false,
		string? Instantaneas = null);

	private readonly Paths paths;
	private readonly Action<string> log;
	private readonly Action<string, string, string> push;
	private readonly CancellationTokenSource stop = new();

	private Dispatcher? dispatcher;
	private ModuleCatalog? catalog;
	private ConsentWatch? watch;

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
	public Shell(Paths paths, Action<string> log, Action<string, string, string> push)
	{
		this.paths = paths;
		this.log = log;
		this.push = push;
	}

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

	/// La foto para la caratula, si esta plataforma la pide. Android pasa null y esto no hace
	/// nada -- alla la caratula pregunta en vivo.
	private async Task Fotografiar()
	{
		if (paths.Instantaneas is null || dispatcher is null) return;
		await Instantanea.Escribir(dispatcher, paths.Instantaneas, log, stop.Token);
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
		return ("Groq", ModelUrl, ModelName);
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
				(_, string url, string model) = Provider();
				var loop = new AgentLoop(dispatcher!, new ModelOpenAiCompatible(url, model, KeyPath));

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
	//   shell.arguments[0]   donde esta el dll        se conserva el NOMBRE del archivo
	//   grantedFolder        que carpeta se le otorga  se conserva el negocio
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

				// El nombre del dll es lo unico que se rescata de la ruta vieja: cual dll se
				// carga lo decidio quien armo el paquete, y eso no cambia por mudarse de
				// aparato. Donde esta, si.
				string dll = vieja.Shell.Arguments.Count > 0 ? Path.GetFileName(vieja.Shell.Arguments[0]) : "";
				if (dll.Length == 0) { log($"localizar: {negocio} no declara dll; se deja como esta"); continue; }

				Installation nueva = vieja with
				{
					Shell = vieja.Shell with { Arguments = [Path.Combine(dir, "bin", dll)] },
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
