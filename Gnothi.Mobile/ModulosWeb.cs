using Android.App;
using Android.Webkit;
using Gnothi.Host;
using Java.Interop;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace Gnothi.Mobile;

// EL ANFITRIÓN DE MÓDULOS WEB: un WebView por módulo, creados POR DEMANDA y desalojables.
//
// Por demanda y no al arrancar, porque medido cada uno pesa ~150 MB con el runtime adentro
// (11 §12). Y desalojarlos no pierde nada: el módulo no despierta solo, no notifica, no
// tiene proceso propio. Sin red, sin service worker, sin worker.
internal sealed class AnfitrionDeModulosWeb : IAnfitrionDeModulosWeb
{
	private readonly Activity duenio;
	private readonly ConcurrentDictionary<string, ModuloWeb> vivos = new();

	internal AnfitrionDeModulosWeb(Activity duenio) => this.duenio = duenio;

	public bool EstaVivo(string modulo) => vivos.ContainsKey(modulo);

	public async Task<IModuloWeb> Vivo(string modulo, string bundle, string journal, string publicador, CancellationToken ct)
	{
		if (vivos.TryGetValue(modulo, out ModuloWeb? yaEsta)) return yaEsta;

		var nuevo = new ModuloWeb(duenio, modulo, bundle, journal, publicador);
		if (!vivos.TryAdd(modulo, nuevo)) { nuevo.Desalojar(); return vivos[modulo]; }

		await nuevo.Arrancar(ct);
		return nuevo;
	}

	// ABRIR UN MODULO DEL CATALOGO. La cascara conoce el bundle y el publicador porque los
	// lee de la instalacion; la pantalla no, asi que se los saca de ahi por el mismo camino.
	// Si ya estaba vivo -porque el asistente le pregunto algo- se devuelve tal cual y abrir es
	// instantaneo.
	internal async Task<ModuloWeb> Abrir(Gnothi.Host.Module m, CancellationToken ct)
	{
		System.Collections.Generic.IReadOnlyList<string> args = m.Installation.Shell.Arguments;
		string bundle = args.Count > 0 ? args[0] : "";
		string publicador = args.Count > 1 ? args[1] : "";

		await Vivo(m.Manifest.Module, bundle, m.Installation.GrantedFolder, publicador, ct);
		return vivos[m.Manifest.Module];
	}

	public void Desalojar(string modulo)
	{
		// Un modulo que Sonia esta mirando NO se desaloja por debajo. La regla de desalojar por
		// demanda sigue en pie para todos los demas.
		if (vivos.TryGetValue(modulo, out ModuloWeb? mirando) && mirando.Visible) return;

		if (vivos.TryRemove(modulo, out ModuloWeb? irse)) irse.Desalojar();
	}
}

// UN MÓDULO WEB: su WebView, su origen, su CSP y su puente. Es lo que el spike probó,
// puesto donde va.
internal sealed class ModuloWeb : IModuloWeb
{
	// EL ORIGEN DEL MÓDULO. Host virtual sobre https, uno por módulo, y NO un esquema propio:
	// un esquema propio no es contexto seguro y ahí se caen el almacenamiento del origen y las
	// funciones de cifrado (11 §3, corregido por el spike). Se intercepta ANTES de la red, así
	// que no hay DNS ni TLS: el host no existe fuera del aparato.
	//
	// Un host distinto por módulo es un ORIGEN distinto, que es el aislamiento que pide §3, y
	// sale gratis: lo hace el motor web por same-origin.
	private static string HostDe(string modulo) => $"{modulo}.gnothi.local";

	private readonly Activity duenio;
	private readonly string modulo;
	private readonly string bundle;
	private readonly string journal;
	private readonly CarteroDeGnothi? cartero;

	// El módulo avisa cuando su actor ya está hidratado. Hasta entonces no se le puede
	// preguntar nada: la pantalla se dibuja antes de que el actor exista, y adivinar por
	// píxeles ya falló una vez.
	private readonly TaskCompletionSource listo =
		new(TaskCreationOptions.RunContinuationsAsynchronously);

	private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> esperando = new();
	private int siguiente;

	private WebView? web;

	// La vista del modulo, para cuando Gnothi lo pone en pantalla (11 §8). Es EL MISMO WebView
	// que contesta las preguntas: no hay dos.
	internal Android.Views.View? Vista => web;

	// Mientras este a la vista no se desaloja, aunque cueste sus ~200 MB. Es la unica concesion
	// a la regla de desalojar por demanda, y dura lo que dura la mirada.
	internal bool Visible { get; set; }

	internal ModuloWeb(Activity duenio, string modulo, string bundle, string journal, string publicador)
	{
		this.duenio = duenio;
		this.modulo = modulo;
		this.bundle = bundle;
		this.journal = journal;
		Directory.CreateDirectory(journal);

		// Sin endpoint no hay cartero, y eso es legitimo: un modulo puede vivir solo de lo que
		// ya tiene en su journal. Lo que NO puede es salir a buscarlo por su cuenta.
		cartero = publicador.Length > 0
			? new CarteroDeGnothi(modulo, publicador,
				new CredencialDeGnothi(modulo, duenio.FilesDir!.AbsolutePath))
			: null;
	}

	// DÓNDE DICE EL BUNDLE QUE VIVE. Su index.html trae un `<base href="…">` horneado al
	// publicar, y todas sus rutas cuelgan de ahí. Gnothi tiene que servirlo EN ESE LUGAR:
	// servirlo en la raíz cuando el bundle se cree en `/app/` da 404 en todo y una página en
	// blanco, sin ninguna pista. Medido, y costó una corrida entera.
	//
	// Se lee una vez, del archivo, y no se configura: es un hecho del bundle, no una decisión
	// del Host.
	private static string BaseDelBundle(string bundle)
	{
		string index = Path.Combine(bundle, "index.html");
		if (!File.Exists(index)) return "/";

		System.Text.RegularExpressions.Match m = System.Text.RegularExpressions.Regex.Match(
			File.ReadAllText(index), @"<base\s[^>]*href\s*=\s*[""']([^""']+)[""']",
			System.Text.RegularExpressions.RegexOptions.IgnoreCase);

		if (!m.Success) return "/";

		string href = m.Groups[1].Value;
		if (!href.StartsWith('/')) href = "/" + href;
		if (!href.EndsWith('/')) href += "/";
		return href;
	}

	internal async Task Arrancar(CancellationToken ct)
	{
		string raiz = BaseDelBundle(bundle);
		Android.Util.Log.Info("gnothi", $"{modulo}: el bundle declara su base en {raiz}");

		var creado = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

		// El WebView se crea en el hilo de la UI. No hay alternativa: es una vista.
		duenio.RunOnUiThread(() =>
		{
			try
			{
				var w = new WebView(duenio);
				w.Settings.JavaScriptEnabled = true;
				w.Settings.DomStorageEnabled = true;
				w.SetWebViewClient(new ServidorDelBundle(bundle, HostDe(modulo), raiz));
				w.SetWebChromeClient(new ConsolaDelModulo(modulo));
				w.AddJavascriptInterface(new PuenteHaciaElModulo(this), "gnothiPuente");

				// NO se agrega a ninguna vista: el módulo no tiene pantalla acá. Esta cáscara
				// contesta preguntas; la UI de la institución es otra historia (11 §8).
				web = w;
				creado.TrySetResult();
			}
			catch (Exception ex) { creado.TrySetException(ex); }
		});

		await creado.Task.WaitAsync(ct);

		duenio.RunOnUiThread(() => web!.LoadUrl($"https://{HostDe(modulo)}{raiz}"));

		// Esperar a que el actor esté hidratado. Si el módulo nunca avisa, la pregunta falla
		// diciendo eso, en vez de colgarse para siempre.
		await listo.Task.WaitAsync(TimeSpan.FromSeconds(60), ct);
	}

	public Task<string> Preguntar(string tool, string argumentosJson, CancellationToken ct) =>
		Pedir($"window.gnothi.call({Json(tool)}, {Json(argumentosJson)})", ct);

	public Task<string> Tools(CancellationToken ct) => Pedir("window.gnothi.tools()", ct);

	// EL TRASPASO de la instalacion suelta: las aserciones de Sonia, no eventos del proveedor.
	// Entra por su propio verbo y NO por deliver, porque son comandos locales sin numero de
	// evento: meterlos por el mismo camino obligaria a inventarles uno y desalinearia la
	// bitacora. La idempotencia tambien es distinta y vive del lado del modulo (13 §5.3).
	internal Task<string> Traspasar(string asercionesJson, CancellationToken ct) =>
		Pedir($"window.gnothi.traspasar({Json(asercionesJson)})", ct);

	// TODO LO QUE EL MÓDULO CONTESTA VIENE POR CALLBACK, y no por el retorno de
	// evaluateJavascript, por una razón concreta: la superficie del módulo devuelve promesas
	// —entra a C# por DotNet.invokeMethodAsync— y evaluateJavascript no las espera: devolvería
	// el objeto Promise sin resolver.
	private async Task<string> Pedir(string expresion, CancellationToken ct)
	{
		if (web is null) throw new InvalidOperationException($"el módulo '{modulo}' no está vivo");

		string id = Interlocked.Increment(ref siguiente).ToString();
		var espera = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
		esperando[id] = espera;

		string js = $"Promise.resolve({expresion})"
			+ $".then(function (r) {{ gnothiPuente.contesta({Json(id)}, r, null); }})"
			+ $".catch(function (e) {{ gnothiPuente.contesta({Json(id)}, null, String(e)); }});";

		duenio.RunOnUiThread(() => web.EvaluateJavascript(js, null));

		try { return await espera.Task.WaitAsync(TimeSpan.FromSeconds(30), ct); }
		finally { esperando.TryRemove(id, out _); }
	}

	// Lo llama el puente cuando el módulo contestó.
	internal void Contesta(string id, string? json, string? error)
	{
		if (!esperando.TryRemove(id, out TaskCompletionSource<string>? espera)) return;

		if (error is not null) espera.TrySetException(new InvalidOperationException(error));
		else espera.TrySetResult(json ?? "");
	}

	// La respuesta a un pedido que hizo el MODULO. Va por el canal que su interop escucha.
	internal void Responde(string id, string? json, string? error)
	{
		if (web is null) return;

		string cuerpo = json is null ? "null" : Json(json);
		string motivo = error is null ? "null" : Json(error);

		duenio.RunOnUiThread(() => web.EvaluateJavascript(
			$"window.gnothi.respuesta({Json(id)}, {cuerpo}, {motivo})", null));
	}

	internal void Listo() => listo.TrySetResult();

	// ---------------------------------------------------------------- lo que el módulo pide

	// EL JOURNAL, contra los archivos del contenedor de Gnothi. Un directorio por módulo, y
	// NO en OPFS: el cuarto pilar —Gnothi responde lo que ningún módulo puede responder de sí
	// mismo— exige poder mirarlo desde afuera del motor web (11 §5.1).
	//
	// GNOTHI NO COMPONE NADA: escribe los bytes que el módulo ya escribió, tal cual. Es el
	// brazo del módulo, no un segundo autor.
	internal string Listar()
	{
		var rutas = new List<string>();
		foreach (string archivo in Directory.EnumerateFiles(journal, "*", SearchOption.AllDirectories))
			rutas.Add(Path.GetRelativePath(journal, archivo).Replace(Path.DirectorySeparatorChar, '/'));

		return JsonSerializer.Serialize(new { ok = true, rutas, motivo = "" });
	}

	internal string Leer(string ruta)
	{
		string completa = EnElJournal(ruta);
		if (!File.Exists(completa))
			return JsonSerializer.Serialize(new { ok = false, contenido = "", motivo = "no existe" });

		return JsonSerializer.Serialize(new
		{
			ok = true,
			contenido = Convert.ToBase64String(File.ReadAllBytes(completa)),
			motivo = "",
		});
	}

	internal string Escribir(string ruta, string contenidoBase64)
	{
		string completa = EnElJournal(ruta);
		Directory.CreateDirectory(Path.GetDirectoryName(completa)!);
		File.WriteAllBytes(completa, Convert.FromBase64String(contenidoBase64));

		return JsonSerializer.Serialize(new { ok = true, motivo = "" });
	}

	// EL CARTERO, cableado. El cursor lo pone el actor y Gnothi releva con SU credencial
	// contra SU endpoint (13 §8).
	//
	// Sin endpoint registrado se devuelve un sobre VACÍO y no un error, deliberadamente: ya
	// está medido que el módulo arranca sin poder hablar con el publicador, y así se abre
	// igual mostrando lo que ya tiene en su journal —que es justamente de lo que sirve tener
	// el dato propio—. Un error acá lo dejaría sin abrir.
	internal async Task<string> Pull(string despuesDe)
	{
		if (!long.TryParse(despuesDe, out long desde) || desde < 0)
			throw new ArgumentException($"cursor inválido: {despuesDe}");

		if (cartero is null)
		{
			Android.Util.Log.Warn("gnothi", $"{modulo} pidió after={desde} y no tiene publicador registrado");
			return JsonSerializer.Serialize(new { events = Array.Empty<object>(), last = desde });
		}

		return await cartero.Pull(desde);
	}

	// Una ruta del módulo NO puede salirse de su journal. Es responsabilidad de Gnothi, no
	// confianza en el módulo.
	private string EnElJournal(string ruta)
	{
		string completa = Path.GetFullPath(
			Path.Combine(journal, ruta.Replace('/', Path.DirectorySeparatorChar)));

		if (!completa.StartsWith(journal, StringComparison.Ordinal))
			throw new UnauthorizedAccessException($"ruta fuera del módulo: {ruta}");

		return completa;
	}

	internal void Desalojar()
	{
		WebView? irse = web;
		web = null;
		if (irse is null) return;

		duenio.RunOnUiThread(() => { irse.StopLoading(); irse.Destroy(); });
	}

	private static string Json(string valor) => JsonSerializer.Serialize(valor);
}

// EL PUENTE, del lado nativo. Lo que el módulo puede pedirle a Gnothi.
//
// Contesta por callback y no por retorno porque los métodos de un objeto inyectado con
// @JavascriptInterface son SÍNCRONOS: la página se congela hasta que vuelven, y un pull hace
// red.
internal sealed class PuenteHaciaElModulo : Java.Lang.Object
{
	private readonly ModuloWeb modulo;

	internal PuenteHaciaElModulo(ModuloWeb modulo) => this.modulo = modulo;

	// El módulo avisa que su actor ya está hidratado y puede recibir preguntas.
	[JavascriptInterface]
	[Export("listo")]
	public void Listo() => modulo.Listo();

	// La respuesta del módulo a una pregunta que le hizo Gnothi.
	[JavascriptInterface]
	[Export("contesta")]
	public void Contesta(string id, string? json, string? error) => modulo.Contesta(id, json, error);

	// EL JOURNAL Y EL CARTERO. Contestan por callback como todo lo demás: los métodos de un
	// objeto inyectado son síncronos y congelarían la página.
	[JavascriptInterface]
	[Export("listar")]
	public void Listar(string id) => Atender(id, modulo.Listar);

	[JavascriptInterface]
	[Export("leer")]
	public void Leer(string id, string ruta) => Atender(id, () => modulo.Leer(ruta));

	[JavascriptInterface]
	[Export("escribir")]
	public void Escribir(string id, string ruta, string contenidoBase64) =>
		Atender(id, () => modulo.Escribir(ruta, contenidoBase64));

	[JavascriptInterface]
	[Export("pull")]
	public void Pull(string id, string despuesDe) => AtenderAsync(id, () => modulo.Pull(despuesDe));

	// Retorna YA y contesta después. Un fallo viaja como error, no como excepción de JS.
	private void Atender(string id, Func<string> trabajo) => _ = Task.Run(() =>
	{
		try { modulo.Responde(id, trabajo(), null); }
		catch (Exception ex) { modulo.Responde(id, null, $"{ex.GetType().Name}: {ex.Message}"); }
	});

	// Igual, para lo que hace red. El pull no puede bloquear: los metodos de un objeto
	// inyectado son sincronos y congelarian la pagina del modulo.
	private void AtenderAsync(string id, Func<Task<string>> trabajo) => _ = Task.Run(async () =>
	{
		try { modulo.Responde(id, await trabajo(), null); }
		catch (Exception ex) { modulo.Responde(id, null, $"{ex.GetType().Name}: {ex.Message}"); }
	});
}

// GNOTHI SIRVIENDO EL BUNDLE del módulo desde su propio contenedor, con la CSP puesta.
internal sealed class ServidorDelBundle : WebViewClient
{
	// La CSP menos script-src, que se arma por documento con el hash de cada script en línea.
	//
	// `connect-src 'self'` y NO 'none': con 'none' el módulo no arranca, porque el propio
	// runtime pide su blazor.boot.json con fetch. Medido. Con 'self' sigue sin poder alcanzar
	// al publicador —es otro origen— y además arranca.
	private const string CspSinScript =
		"default-src 'self'; " +
		"style-src 'self' 'unsafe-inline'; " +
		"connect-src 'self'; " +
		"frame-src 'none'; " +
		"worker-src 'none'";

	private static readonly System.Text.RegularExpressions.Regex ScriptsEnLinea =
		new(@"<script\b(?![^>]*\bsrc\s*=)[^>]*>(.*?)</script\s*>",
			System.Text.RegularExpressions.RegexOptions.Singleline
			| System.Text.RegularExpressions.RegexOptions.IgnoreCase);

	private readonly string bundle;
	private readonly string host;
	private readonly string raiz;

	internal ServidorDelBundle(string bundle, string host, string raiz)
	{
		this.bundle = bundle;
		this.host = host;
		this.raiz = raiz;
	}

	public override WebResourceResponse? ShouldInterceptRequest(WebView? view, IWebResourceRequest? request)
	{
		Android.Net.Uri? url = request?.Url;
		if (url is null || !string.Equals(url.Host, host, StringComparison.Ordinal)) return null;

		// La ruta del pedido viene con la base del bundle adelante; se recorta para caer
		// dentro de su carpeta.
		string pedida = url.Path ?? "/";
		if (pedida.StartsWith(raiz, StringComparison.Ordinal)) pedida = pedida[raiz.Length..];

		string ruta = pedida.TrimStart('/');
		if (ruta.Length == 0 || ruta.EndsWith("/", StringComparison.Ordinal)) ruta += "index.html";

		string completa = Resolver(ruta);
		var cabeceras = new Dictionary<string, string> { ["Cache-Control"] = "no-store" };

		if (!File.Exists(completa))
			return new WebResourceResponse("text/plain", "utf-8", 404, "Not Found",
				cabeceras, new MemoryStream());

		// El documento se lee entero porque su CSP depende de su contenido. Los demás
		// archivos viajan como stream.
		if (ruta.EndsWith(".html", StringComparison.Ordinal))
		{
			byte[] documento = File.ReadAllBytes(completa);
			cabeceras["Content-Security-Policy"] = CspPara(documento);
			return new WebResourceResponse(Tipo(ruta), "utf-8", 200, "OK",
				cabeceras, new MemoryStream(documento));
		}

		return new WebResourceResponse(Tipo(ruta), "utf-8", 200, "OK",
			cabeceras, File.OpenRead(completa));
	}

	// EL HASH SE CALCULA, NO SE ESCRIBE. Uno a mano se desincroniza en silencio la primera
	// vez que la institución recompila, y el síntoma es una pantalla en blanco sin pista.
	// Gnothi tiene el archivo en la mano: lo calcula acá.
	private static string CspPara(byte[] documento)
	{
		string html = Encoding.UTF8.GetString(documento);

		var hashes = new List<string>();
		foreach (System.Text.RegularExpressions.Match m in ScriptsEnLinea.Matches(html))
		{
			string cuerpo = m.Groups[1].Value;
			if (cuerpo.Length == 0) continue;

			byte[] resumen = System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(cuerpo));
			hashes.Add($"'sha256-{Convert.ToBase64String(resumen)}'");
		}

		string script = string.Join(" ", new[] { "'self'", "'wasm-unsafe-eval'" }.Concat(hashes));
		return $"{CspSinScript}; script-src {script}";
	}

	// Una ruta del módulo NO puede salirse de su bundle. Es responsabilidad de Gnothi, no
	// confianza en el módulo.
	private string Resolver(string ruta)
	{
		string completa = Path.GetFullPath(
			Path.Combine(bundle, ruta.Replace('/', Path.DirectorySeparatorChar)));

		if (!completa.StartsWith(bundle, StringComparison.Ordinal))
			throw new UnauthorizedAccessException($"ruta fuera del módulo: {ruta}");

		return completa;
	}

	// El .wasm NECESITA su tipo o el motor no lo compila en streaming.
	private static string Tipo(string ruta) => Path.GetExtension(ruta).ToLowerInvariant() switch
	{
		".html" => "text/html",
		".js" or ".mjs" => "text/javascript",
		".css" => "text/css",
		".wasm" => "application/wasm",
		".json" => "application/json",
		".webmanifest" => "application/manifest+json",
		".png" => "image/png",
		".ico" => "image/x-icon",
		".svg" => "image/svg+xml",
		".woff2" => "font/woff2",
		_ => "application/octet-stream",
	};
}

// La consola del módulo va a logcat con su nombre. Es como se ven las violaciones de CSP y
// los errores del dominio de la institución.
internal sealed class ConsolaDelModulo : WebChromeClient
{
	private readonly string modulo;

	internal ConsolaDelModulo(string modulo) => this.modulo = modulo;

	public override bool OnConsoleMessage(ConsoleMessage? mensaje)
	{
		if (mensaje is not null)
			Android.Util.Log.Info($"modulo:{modulo}", $"{mensaje.Message()} ({mensaje.LineNumber()})");
		return true;
	}
}
