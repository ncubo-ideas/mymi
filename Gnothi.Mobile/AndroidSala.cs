using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Android.App;
using Android.Webkit;
using Gnothi.Host;
using Java.Interop;

namespace Gnothi.Mobile;

// LA SALA EN ANDROID: UN documento, propiedad de Gnothi, con el runtime .NET y el motor
// compilados a wasm. Los modulos llegan despues, como bytes, y corren ahi adentro.
//
// LO QUE ESTE ARCHIVO HACE Y LO QUE NO. Sirve los bytes y pasa los pedidos. No decide nada: ni
// que modulo se carga, ni cuando se desaloja, ni quien pregunta. Eso lo decide SalaShell, que
// es quien tiene el marco de la llamada que abrio el Host.
//
// UN SOLO WEBVIEW, y ahi esta toda la diferencia con la forma hospedada: ahi hay uno por modulo
// y cada uno trae su propio runtime de 10 MB; aca hay uno para todos y el runtime se paga una
// vez. Medido en un A54: 46,1 MB de memoria lineal el sustrato solo, y 4,1 MB por cada modulo
// que se suma.
//
// EL DOCUMENTO NO TIENE PANTALLA. No se agrega a ninguna vista: existe para contestar. La cara
// de la institucion sigue siendo la forma hospedada, en su propio origen, y se monta solo
// cuando la persona la abre.
internal sealed class AndroidSala : ISalaHost
{
	// El origen de la sala. https con host virtual y no un esquema propio, por la misma razon
	// medida en la forma hospedada: sin contexto seguro se cae lo que el runtime necesita.
	private const string Host = "sala.gnothi.local";
	private const string Raiz = "https://" + Host + "/";

	private readonly Activity actividad;
	private readonly Action<string> log;
	private readonly ConcurrentDictionary<string, Preparado> preparados = new(StringComparer.Ordinal);
	private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> esperando = new(StringComparer.Ordinal);

	private WebView? web;
	private TaskCompletionSource<bool>? listo;
	private int siguiente;

	public AndroidSala(Activity actividad, Action<string> log)
	{
		this.actividad = actividad;
		this.log = log;
	}

	private sealed record Preparado(string Dominio, string Entrada, string Journal);

	// ---------------------------------------------------------------- lo que SalaShell pide

	public Task Preparar(string modulo, string entrada, string grantedFolder, CancellationToken ct)
	{
		// `entrada` es la ruta del .dll que el manifiesto declara. Su carpeta es de donde salen
		// los demas ensamblados del modulo, que es la convencion del paquete hibrido.
		string dominio = Path.GetDirectoryName(entrada) ?? "";
		preparados[modulo] = new Preparado(dominio, Path.GetFileNameWithoutExtension(entrada), grantedFolder);
		log($"sala: {modulo} preparado desde {dominio}");
		return Task.CompletedTask;
	}

	public async Task<string> Pedir(string verbo, string modulo, string a, string b, CancellationToken ct)
	{
		await Arrancar(ct);

		string id = Interlocked.Increment(ref siguiente).ToString();
		var espera = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
		esperando[id] = espera;

		string js = $"window.sala.pedido({J(id)}, {J(verbo)}, {J(modulo)}, {J(a)}, {J(b)})";
		actividad.RunOnUiThread(() => web!.EvaluateJavascript(js, null));

		using CancellationTokenRegistration _ = ct.Register(() => espera.TrySetCanceled(ct));
		return await espera.Task;
	}

	// ---------------------------------------------------------------- el documento

	private async Task Arrancar(CancellationToken ct)
	{
		if (listo is not null) { await listo.Task; return; }

		var arrancando = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		if (Interlocked.CompareExchange(ref listo, arrancando, null) is not null)
		{
			await listo!.Task;
			return;
		}

		log("sala: creando el documento");

		actividad.RunOnUiThread(() =>
		{
			web = new WebView(actividad);
			web.Settings!.JavaScriptEnabled = true;
			web.SetWebViewClient(new ServidorDeLaSala(this));
			web.SetWebChromeClient(new ConsolaDeLaSala(log));
			web.AddJavascriptInterface(new PuenteDeLaSala(this), "SalaPuente");
			web.LoadUrl(Raiz);
		});

		await arrancando.Task.WaitAsync(TimeSpan.FromSeconds(60), ct);
	}

	internal void Lista()
	{
		log("sala: el sustrato contesta");
		listo?.TrySetResult(true);
	}

	internal void Contesta(string id, string json)
	{
		if (esperando.TryRemove(id, out TaskCompletionSource<string>? espera)) espera.TrySetResult(json);
	}

	// ---------------------------------------------------------------- de donde salen los bytes

	// TRES ORIGENES PARA UN SOLO ORIGEN WEB. El sustrato sale de los assets del apk; los .dll del
	// modulo, de su carpeta instalada; y el journal, de la carpeta de la persona. El modulo no
	// distingue: para el es todo su propio origen, y no abre un archivo en su vida.
	internal Android.Webkit.WebResourceResponse? Servir(string ruta)
	{
		try
		{
			if (ruta.StartsWith("modulo/", StringComparison.Ordinal))
			{
				string[] partes = ruta.Split('/', 4);
				if (partes.Length < 3) return NoEsta(ruta);

				string modulo = partes[1];
				if (!preparados.TryGetValue(modulo, out Preparado? p)) return NoEsta(ruta);

				// modulo/<m>/indice.json -- que ensamblados trae y cual es la entrada
				if (partes[2] == "indice.json")
				{
					// SOLO LO QUE ES DEL MODULO. Todo lo demas lo presta el sustrato, y no es una
					// optimizacion: un paquete del BCL viene compilado POR PLATAFORMA, y el que
					// esta en la carpeta del modulo es el de Android. Medido: cargarlo adentro
					// del motor web revienta con TypeLoadException sobre las intrinsecas de ARM.
					//
					// Es la misma regla que PackageInspector ya hace cumplir del otro lado, donde
					// un paquete cargo que trae su propio motor se RECHAZA.
					string[] archivos = [.. Directory.EnumerateFiles(p.Dominio, "*.dll")
						.Select(Path.GetFileNameWithoutExtension)
						.Where(n => n is not null && !LoPrestaLaSala(n))
						.Select(n => n!)
						.OrderBy(n => string.Equals(n, p.Entrada, StringComparison.Ordinal) ? 1 : 0)];

					return Texto(JsonSerializer.Serialize(new { entrada = p.Entrada, archivos }), "application/json");
				}

				// modulo/<m>/dominio/<x>.dll
				if (partes[2] == "dominio" && partes.Length == 4)
					return Archivo(Path.Combine(p.Dominio, partes[3]), "application/octet-stream");

				// modulo/<m>/journal/indice.json -- lo que hay en la carpeta de la persona
				if (partes[2] == "journal" && partes.Length == 4 && partes[3] == "indice.json")
				{
					string[] rutas = Directory.Exists(p.Journal)
						? [.. Directory.EnumerateFiles(p.Journal, "*", SearchOption.AllDirectories)
							.Select(f => Path.GetRelativePath(p.Journal, f).Replace(Path.DirectorySeparatorChar, '/'))]
						: [];

					return Texto(JsonSerializer.Serialize(new { rutas }), "application/json");
				}

				// modulo/<m>/journal/<ruta> -- y NO se sale de la carpeta concedida
				if (partes[2] == "journal" && partes.Length == 4)
				{
					string raiz = Path.GetFullPath(p.Journal);
					string pedido = Path.GetFullPath(Path.Combine(raiz, partes[3].Replace('/', Path.DirectorySeparatorChar)));
					if (!pedido.StartsWith(raiz, StringComparison.Ordinal)) return NoEsta(ruta);

					return Archivo(pedido, "application/octet-stream");
				}

				return NoEsta(ruta);
			}

			// todo lo demas es el sustrato, que viaja en el apk
			return Assets(ruta);
		}
		catch (Exception e)
		{
			log($"sala: 500 {ruta} -- {e.GetType().Name}: {e.Message}");
			return NoEsta(ruta);
		}
	}

	// LA CSP DEL DOCUMENTO DE LA SALA, y no es un tramite: es lo que hace la contencion
	// verificable en vez de declarada. Adentro de la sala corre el dominio de una institucion, y
	// esto es lo que le impide alcanzar cualquier cosa que no sea su propio origen -- que lo
	// atiende ShouldInterceptRequest, o sea Gnothi.
	//
	// connect-src 'self' y no 'none': el propio runtime pide su manifiesto con fetch, y la sala
	// pide los .dll y el journal por el mismo camino. Con 'none' no arranca. Con 'self' arranca
	// y sigue sin poder salir. 'wasm-unsafe-eval' porque sin el no se compila wasm.
	//
	// EL HASH SE CALCULA, NO SE ESCRIBE: uno a mano se desincroniza en silencio la primera vez
	// que se republica el sustrato, y el sintoma es una pantalla en blanco sin ninguna pista.
	private const string CspSinScript =
		"default-src 'self'; " +
		"style-src 'self' 'unsafe-inline'; " +
		"connect-src 'self'; " +
		"img-src 'self' data:; " +
		"frame-src 'none'; " +
		"object-src 'none'; " +
		"base-uri 'self'; " +
		"form-action 'none'";

	private static readonly System.Text.RegularExpressions.Regex ScriptsEnLinea =
		new(@"<script\b(?![^>]*\bsrc\s*=)[^>]*>(.*?)</script\s*>",
			System.Text.RegularExpressions.RegexOptions.Singleline
			| System.Text.RegularExpressions.RegexOptions.IgnoreCase);

	private WebResourceResponse Assets(string ruta)
	{
		// El documento se lee entero antes de servirlo, porque su CSP depende de su contenido.
		// Los demas archivos viajan como stream, sin copiarlos a memoria.
		if (ruta.EndsWith(".html", StringComparison.Ordinal))
		{
			using Stream original = actividad.Assets!.Open("sala/" + ruta);
			using var buffer = new MemoryStream();
			original.CopyTo(buffer);
			byte[] documento = buffer.ToArray();

			Dictionary<string, string> cabeceras = Cabeceras();
			cabeceras["Content-Security-Policy"] = CspPara(documento);

			return new WebResourceResponse(Tipo(ruta), "utf-8", 200, "OK", cabeceras, new MemoryStream(documento));
		}

		Stream cuerpo = actividad.Assets!.Open("sala/" + ruta);
		return new WebResourceResponse(Tipo(ruta), "utf-8", 200, "OK", Cabeceras(), cuerpo);
	}

	private string CspPara(byte[] documento)
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

		log($"sala: CSP con {hashes.Count} script(s) en linea");

		string script = string.Join(" ", new[] { "'self'", "'wasm-unsafe-eval'" }.Concat(hashes));
		return $"{CspSinScript}; script-src {script}";
	}

	private static WebResourceResponse Archivo(string ruta, string tipo) =>
		File.Exists(ruta)
			? new WebResourceResponse(tipo, "utf-8", 200, "OK", Cabeceras(), File.OpenRead(ruta))
			: new WebResourceResponse("text/plain", "utf-8", 404, "Not Found", Cabeceras(), new MemoryStream());

	private static WebResourceResponse Texto(string cuerpo, string tipo) =>
		new(tipo, "utf-8", 200, "OK", Cabeceras(), new MemoryStream(Encoding.UTF8.GetBytes(cuerpo)));

	private WebResourceResponse NoEsta(string ruta)
	{
		log($"sala: 404 {ruta}");
		return new WebResourceResponse("text/plain", "utf-8", 404, "Not Found", Cabeceras(), new MemoryStream());
	}

	// LA LISTA DE LO PRESTADO, que es la otra cara de la lista de prohibidos del instalador. Lo
	// que esta aca lo pone el sustrato en su build de browser-wasm; lo que no, viaja en el
	// modulo. Si un modulo necesitara algo que no esta, la respuesta NO es cargarselo de su
	// carpeta: es agregarlo al sustrato, y que `substrate.range` lo negocie.
	private static bool LoPrestaLaSala(string ensamblado) =>
		ensamblado.StartsWith("System.", StringComparison.OrdinalIgnoreCase)
		|| ensamblado.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase)
		|| ensamblado is "Puppeteer" or "Choreography" or "Puppets.Portador" or "Gnothi.Contract"
		|| ensamblado.StartsWith("ModelContextProtocol", StringComparison.OrdinalIgnoreCase)
		|| ensamblado is "netstandard" or "mscorlib";

	private static Dictionary<string, string> Cabeceras() => new() { ["Cache-Control"] = "no-store" };

	internal static string Anfitrion => Host;

	private static string Tipo(string ruta) => Path.GetExtension(ruta).ToLowerInvariant() switch
	{
		".html" => "text/html",
		".js" or ".mjs" => "text/javascript",
		".css" => "text/css",
		".wasm" => "application/wasm",
		".json" => "application/json",
		_ => "application/octet-stream",
	};

	private static string J(string s) => JsonSerializer.Serialize(s);
}

internal sealed class ServidorDeLaSala(AndroidSala sala) : WebViewClient
{
	public override WebResourceResponse? ShouldInterceptRequest(WebView? view, IWebResourceRequest? request)
	{
		Android.Net.Uri? url = request?.Url;
		if (url is null || !string.Equals(url.Host, AndroidSala.Anfitrion, StringComparison.Ordinal))
			return null;

		string ruta = (url.Path ?? "/").TrimStart('/');
		if (ruta.Length == 0 || ruta.EndsWith("/", StringComparison.Ordinal)) ruta += "index.html";

		return sala.Servir(ruta);
	}
}

internal sealed class ConsolaDeLaSala(Action<string> log) : WebChromeClient
{
	public override bool OnConsoleMessage(ConsoleMessage? mensaje)
	{
		if (mensaje is not null) log($"sala-web: {mensaje.Message()}");
		return true;
	}
}

// EL PUENTE, Y LO QUE NO TIENE. Dos verbos: "ya estoy" y "aca esta la respuesta". No hay leer,
// ni escribir, ni pedir nada -- el journal se lo pone Gnothi antes de preguntar, y lo que el
// modulo escriba se queda en memoria.
internal sealed class PuenteDeLaSala(AndroidSala sala) : Java.Lang.Object
{
	[JavascriptInterface]
	[Export("lista")]
	public void Lista() => sala.Lista();

	[JavascriptInterface]
	[Export("contesta")]
	public void Contesta(string id, string json) => sala.Contesta(id, json);
}
