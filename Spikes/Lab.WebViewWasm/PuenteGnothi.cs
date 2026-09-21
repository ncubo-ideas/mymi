using Android.Webkit;
using Java.Interop;
using System.Text.Json;

namespace Lab.WebViewWasm;

// GNOTHI, EN CHIQUITO. El lado nativo del puente que el modulo usa para lo que no puede
// hacer solo (especificacion 11 §6 y §7, 13 §8):
//
//     pull(after)                  Gnothi hace el HTTP con SU credencial y SU endpoint
//     listar / leer / escribir     el journal, en los archivos del contenedor de Gnothi
//
// TRES COSAS QUE ESTE PUENTE DEMUESTRA, y son el punto del spike:
//
//   1. EL CURSOR ES DEL ACTOR. El after llega del modulo, salido de su replay. Gnothi lo
//      valida como entero y lo releva; NO guarda cursor. No hay dos verdades sobre hasta
//      donde leyo.
//   2. LA CREDENCIAL ES DE GNOTHI. El modulo nunca la ve, y con connect-src 'self' tampoco
//      podria usarla. Gnothi es OTRO cliente ante el publicador, con su propio device key.
//   3. EL ENDPOINT ES FIJO. Lo pone Gnothi al instalar el modulo; el modulo no lo elige.
//
// CONTESTA POR CALLBACK. Los metodos de un objeto inyectado con @JavascriptInterface son
// SINCRONOS: la pagina se congela hasta que vuelven, y un pull hace red. Por eso cada verbo
// recibe un id, retorna enseguida, y la respuesta viaja despues por
// window.gnothi.respuesta(id, json, error).
//
// Y SU SOLA PRESENCIA es lo que le dice al modulo que esta hospedado: no hay bandera que
// mirar ni origen que interpretar, hay o no hay con quien hablar.
internal sealed class PuenteGnothi : Java.Lang.Object
{
	// El endpoint del publicador, registrado al instalar el modulo.
	//
	// 127.0.0.1 y no la IP de la maquina: con `adb reverse tcp:8095 tcp:8095` el propio
	// aparato reenvia ese puerto a la maquina de desarrollo. Funciona igual en el emulador y
	// en un telefono fisico, y evita depender de en que red esta cada uno.
	private const string Publicador = "http://127.0.0.1:8095/";
	private const string Recurso = "expediente";
	private const string IdDeRecurso = "1";

	// El device key DE GNOTHI, no el del modulo. Gnothi se presenta como otro cliente.
	private const string DeviceDeGnothi = "device-gnothi-spike";

	private static readonly HttpClient http = new() { BaseAddress = new Uri(Publicador) };

	private readonly MainActivity duenio;
	private readonly string raiz;
	private string token = "";

	internal PuenteGnothi(MainActivity duenio)
	{
		this.duenio = duenio;

		// Un directorio por modulo, dentro del contenedor privado de Gnothi. No en OPFS: el
		// cuarto pilar exige que Gnothi pueda mirar el journal desde afuera del motor web.
		raiz = Path.Combine(duenio.FilesDir!.AbsolutePath, "journals", "laboratorio");
		Directory.CreateDirectory(raiz);
		Android.Util.Log.Info("SPIKE", $"journal del modulo en {raiz}");
	}

	// EL MODULO AVISA QUE SU ACTOR YA ESTA HIDRATADO, y recien ahi Gnothi le entrega lo que
	// trajo el handoff. Antes esto se disparaba al ver la pantalla dibujada, y en una
	// reapertura -mas rapida- el traspaso llegaba antes que el actor y rebotaba con "todavia
	// no arranco". Adivinar por pixeles estaba mal: el modulo es el unico que sabe cuando
	// puede recibir.
	//
	// Es una NOTIFICACION: no lleva id ni contesta nada.
	[JavascriptInterface]
	[Export("listo")]
	public void Listo()
	{
		Android.Util.Log.Info("SPIKE", "el modulo avisa que su actor ya esta hidratado");
		duenio.EntregarElTraspaso();
	}

	[JavascriptInterface]
	[Export("pull")]
	public void Pull(string id, string despuesDe) => Atender(id, async () =>
	{
		// Gnothi VALIDA el cursor y no lo guarda. Es del actor.
		if (!long.TryParse(despuesDe, out long desde) || desde < 0)
			throw new ArgumentException($"cursor invalido: {despuesDe}");

		string credencial = await Credencial();

		using var pedido = new HttpRequestMessage(HttpMethod.Get,
			$"{Recurso}/{IdDeRecurso}/events?after={desde}");
		pedido.Headers.Authorization =
			new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", credencial);

		using HttpResponseMessage respuesta = await http.SendAsync(pedido);
		respuesta.EnsureSuccessStatusCode();

		string sobre = await respuesta.Content.ReadAsStringAsync();
		Android.Util.Log.Info("SPIKE", $"pull after={desde} -> {sobre.Length} bytes");

		// El sobre viaja TAL CUAL. Gnothi no compone nada: es el brazo del modulo.
		return sobre;
	});

	[JavascriptInterface]
	[Export("listar")]
	public void Listar(string id) => Atender(id, () =>
	{
		var rutas = new List<string>();
		foreach (string archivo in Directory.EnumerateFiles(raiz, "*", SearchOption.AllDirectories))
			rutas.Add(Path.GetRelativePath(raiz, archivo).Replace(Path.DirectorySeparatorChar, '/'));

		return Task.FromResult(JsonSerializer.Serialize(new { ok = true, rutas, motivo = "" }));
	});

	[JavascriptInterface]
	[Export("leer")]
	public void Leer(string id, string ruta) => Atender(id, () =>
	{
		string completa = Resolver(ruta);
		if (!File.Exists(completa))
			return Task.FromResult(JsonSerializer.Serialize(new { ok = false, contenido = "", motivo = "no existe" }));

		string contenido = Convert.ToBase64String(File.ReadAllBytes(completa));
		return Task.FromResult(JsonSerializer.Serialize(new { ok = true, contenido, motivo = "" }));
	});

	[JavascriptInterface]
	[Export("escribir")]
	public void Escribir(string id, string ruta, string contenidoBase64) => Atender(id, () =>
	{
		string completa = Resolver(ruta);
		Directory.CreateDirectory(Path.GetDirectoryName(completa)!);

		// Los bytes que el modulo YA escribio, tal cual. Gnothi nunca compone un registro.
		File.WriteAllBytes(completa, Convert.FromBase64String(contenidoBase64));
		return Task.FromResult(JsonSerializer.Serialize(new { ok = true, motivo = "" }));
	});

	// La credencial DE GNOTHI ante el publicador, contra su propio device key. En este spike
	// vive en memoria; en Gnothi de verdad va al Android Keystore (13 §3.3), y NUNCA al
	// journal.
	private async Task<string> Credencial()
	{
		if (token.Length > 0) return token;

		string cuerpo = JsonSerializer.Serialize(new
		{
			deviceKey = DeviceDeGnothi,
			recurso = Recurso,
			id = IdDeRecurso,
		});

		using var contenido = new StringContent(cuerpo, System.Text.Encoding.UTF8, "application/json");
		using HttpResponseMessage respuesta = await http.PostAsync("device/token", contenido);
		respuesta.EnsureSuccessStatusCode();

		using JsonDocument documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
		token = documento.RootElement.GetProperty("accessToken").GetString() ?? "";
		Android.Util.Log.Info("SPIKE", "Gnothi consiguio SU credencial ante el publicador");
		return token;
	}

	// Una ruta del modulo NO puede salirse de su directorio. El modulo pide rutas relativas;
	// que se queden adentro es responsabilidad de Gnothi, no confianza en el modulo.
	private string Resolver(string ruta)
	{
		string completa = Path.GetFullPath(
			Path.Combine(raiz, ruta.Replace('/', Path.DirectorySeparatorChar)));

		if (!completa.StartsWith(raiz, StringComparison.Ordinal))
			throw new UnauthorizedAccessException($"ruta fuera del modulo: {ruta}");

		return completa;
	}

	// Retorna YA y contesta despues. Un fallo viaja como error, no como excepcion de JS.
	private void Atender(string id, Func<Task<string>> trabajo) => _ = Task.Run(async () =>
	{
		try { duenio.Responder(id, await trabajo(), null); }
		catch (Exception ex) { duenio.Responder(id, null, $"{ex.GetType().Name}: {ex.Message}"); }
	});
}
