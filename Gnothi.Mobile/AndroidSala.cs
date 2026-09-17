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
	private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> esperando = new(StringComparer.Ordinal);

	// UNA SOLA RANURA, Y ES LA CORRECCION QUE MAS IMPORTA DE TODO ESTE ARCHIVO.
	//
	// Antes esto era un diccionario que ACUMULABA todos los modulos de la sesion y no se limpiaba
	// nunca. Servir() atendia `modulo/<cualquiera>/journal/<archivo>` para cualquiera de ellos,
	// leyendo del disco de verdad. Y la CSP deja `connect-src 'self'`, que es exactamente este
	// origen: o sea que un dominio adentro de la sala podia pedir por HTTP el journal de OTRO
	// modulo, directo del disco, SIN PASAR POR LA MEMORIA y sin que el desmontaje tuviera nada
	// que ver. Toda la frontera de la sala se saltaba con un fetch.
	//
	// Lo encontro Jonathan razonando sobre el diseno, sin ver el codigo: en la caja la red no se
	// puede porque la syscall no existe; aca el motor web SI la tiene, asi que lo unico que
	// limita es el origen. Y el origen lo servimos nosotros.
	//
	// Con una sola ranura, lo que se sirve es lo del modulo EN VUELO y nada mas. Lo demas es 404
	// aunque se pida por su nombre exacto.
	private readonly object cerrojo = new();
	private string enVuelo = "";
	private Preparado? preparado;

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

		lock (cerrojo)
		{
			// PREPARAR AL SIGUIENTE CIERRA AL ANTERIOR. No se acumula: la ranura es una.
			if (enVuelo.Length > 0 && !string.Equals(enVuelo, modulo, StringComparison.Ordinal))
				log($"sala: {enVuelo} deja de ser servible");

			enVuelo = modulo;
			preparado = new Preparado(dominio, Path.GetFileNameWithoutExtension(entrada), grantedFolder);
		}

		log($"sala: {modulo} preparado desde {dominio}");
		return Task.CompletedTask;
	}

	// SOLTAR: despues de guardar lo que el modulo escribio, deja de ser servible. Entre el
	// desalojo y la carga del siguiente NO hay ningun modulo servible, que es lo que se quiere:
	// si en esa ventana algo pidiera un journal, la respuesta es 404.
	public Task Soltar(string modulo, CancellationToken ct)
	{
		lock (cerrojo)
		{
			if (!string.Equals(enVuelo, modulo, StringComparison.Ordinal)) return Task.CompletedTask;
			enVuelo = "";
			preparado = null;
		}

		log($"sala: {modulo} soltado, ya no se sirve por el origen");
		return Task.CompletedTask;
	}

	// LO UNICO SERVIBLE ES EL MODULO EN VUELO. Un nombre que no sea ese no existe para el origen,
	// aunque sea un modulo instalado y aunque se pida con su ruta exacta.
	private Preparado? Servible(string modulo)
	{
		lock (cerrojo)
		{
			return string.Equals(enVuelo, modulo, StringComparison.Ordinal) ? preparado : null;
		}
	}

	// LO QUE EL MODULO ESCRIBIO, ATERRIZANDO EN EL DISCO. Y el cerco es el mismo que el puente de
	// la forma hospedada: la ruta que el modulo da es RELATIVA, la ancla Gnothi a SU carpeta, y si
	// el resultado se sale de ahi no se escribe. Es responsabilidad del Host, no confianza en el
	// modulo.
	public Task Guardar(string modulo, string ruta, string contenidoBase64, CancellationToken ct)
	{
		// Y SOLO SE LE GUARDA AL QUE ESTA EN VUELO. Guardar para un modulo que ya se solto seria
		// escribir en la carpeta de alguien que no esta, a pedido de quien si esta.
		if (Servible(modulo) is not Preparado p)
			throw new InvalidOperationException($"'{modulo}' no esta en vuelo: no se le guarda nada");

		string raiz = Path.GetFullPath(p.Journal);
		string destino = Path.GetFullPath(Path.Combine(raiz, ruta.Replace('/', Path.DirectorySeparatorChar)));

		if (!destino.StartsWith(raiz, StringComparison.Ordinal))
			throw new UnauthorizedAccessException($"ruta fuera del journal de {modulo}: {ruta}");

		Directory.CreateDirectory(Path.GetDirectoryName(destino)!);
		byte[] bytes = Convert.FromBase64String(contenidoBase64);
		File.WriteAllBytes(destino, bytes);

		log($"sala/guardar {modulo}: {ruta} · {bytes.Length} bytes");
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
		string respuesta = await espera.Task;

		// LA CONFORMIDAD QUEDA EN EL LOG, no en un comentario. Cargar y desalojar reportan que
		// quedo de los OTROS modulos en memoria; si alguna vez dice mas de cero bytes, la
		// frontera entre journals se rompio y esto lo grita donde se ve.
		if (verbo is "cargar" or "desalojar") log($"sala/{verbo} {modulo}: {respuesta}");

		return respuesta;
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
				if (Servible(modulo) is not Preparado p) return NoEsta(ruta);

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

	// LA CSP DEL DOCUMENTO DE LA SALA, Y LA CALCULA BundleServer, NO ESTA CLASE.
	//
	// Tuve una copia aca y se rompio exactamente como el repo ya tenia documentado: el analizador
	// de HTML normaliza CRLF a LF antes de hashear el script en linea, y mi copia hasheaba los
	// bytes crudos. Con el archivo en LF funcionaba; en cuanto git lo dejo en CRLF, el motor
	// rechazo el script que Gnothi acababa de permitir y el documento quedo EN BLANCO.
	//
	// El arreglo ya existia en BundleServer.AsTheParserSeesIt. Lo que faltaba no era el arreglo:
	// era que hubiera UNA sola copia. Y de paso la sala hereda la misma politica que la forma
	// hospedada, que es una cosa menos que auditar.

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
			cabeceras["Content-Security-Policy"] = Gnothi.MobileShared.BundleServer.CspFor(documento);

			return new WebResourceResponse(Tipo(ruta), "utf-8", 200, "OK", cabeceras, new MemoryStream(documento));
		}

		Stream cuerpo = actividad.Assets!.Open("sala/" + ruta);
		return new WebResourceResponse(Tipo(ruta), "utf-8", 200, "OK", Cabeceras(), cuerpo);
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
