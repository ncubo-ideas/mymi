using Android.App;
using Android.Content;
using Android.Views;
using Android.Webkit;
using Android.Widget;
using Java.Interop;
using System.Text.Json;

// Gnothi necesita red: ES el cartero. Por eso la contencion del modulo NO puede venir de no
// declarar el permiso, tiene que venir de la CSP. Declararlo hace honesta la medicion.
[assembly: UsesPermission(Android.Manifest.Permission.Internet)]

// El mock del publicador corre en la maquina del desarrollador por http. En produccion es
// https y esto se saca; sin esto Android corta el trafico en claro (API 28+).
[assembly: Application(UsesCleartextTraffic = true)]

namespace Lab.WebViewWasm;

// EL SPIKE QUE DECIDE (doc 11 §12): la PWA real de Examenes dentro de un WebView, arrancando
// en frio. Contesta tres cosas y ninguna esta medida hoy:
//
//   1. ¿arranca el runtime wasm en un WebView de Android, y en cuanto?
//   2. ¿cuanta memoria ocupa el WebView con el modulo adentro?
//   3. ¿la CSP corregida del doc 11 §4 lo deja cargar y ademas lo contiene?
// EL HANDOFF: Gnothi atiende el enlace que armó la instalación suelta. En produccion este
// filtro lleva autoVerify y un assetlinks.json en el dominio, para que Android lo abra sin
// dialogo de desambiguacion (especificacion 13 §7).
[IntentFilter(new[] { Intent.ActionView },
	Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
	DataScheme = "https", DataHost = "go.gnothi.app", DataPathPrefix = "/abrir")]
[Activity(Label = "Spike WebView wasm", MainLauncher = true, Theme = "@android:style/Theme.Material.Light.NoActionBar")]
public class MainActivity : Activity
{
	// EL ORIGEN DEL MODULO. https con host virtual, y NO un esquema propio como decia el doc
	// 11 §3, por una razon que hay que medir: un esquema propio no es contexto seguro, y sin
	// contexto seguro se caen crypto.subtle, OPFS y todo lo que la PWA usa para guardar.
	// ShouldInterceptRequest atiende ANTES de la red: no hay DNS ni TLS, el host no existe y
	// los bytes salen de los assets. Un host distinto por modulo = un origen distinto = el
	// aislamiento same-origin que el doc 11 §3 pide, gratis.
	internal const string Host = "laboratorio.gnothi.local";

	// El bundle vive bajo /app/ porque su index.html trae <base href="/app/"> horneado.
	private const string Raiz = "https://" + Host + "/app/";

	internal static long Arranque;

	private TextView? marcador;
	private WebView? web;

	protected override void OnCreate(Bundle? savedInstanceState)
	{
		Arranque = Android.OS.SystemClock.ElapsedRealtime();
		base.OnCreate(savedInstanceState);

		TomarDelIntent(Intent);

		WebView.SetWebContentsDebuggingEnabled(true);

		web = new WebView(this);
		web.Settings.JavaScriptEnabled = true;
		// localStorage: es donde la PWA guarda la credencial desde que salio del journal.
		web.Settings.DomStorageEnabled = true;
		web.SetWebViewClient(new Servidor(this));
		web.SetWebChromeClient(new Consola());
		web.AddJavascriptInterface(new Puente(this), "Spike");

		// EL PUENTE DE GNOTHI. Su sola presencia le dice al modulo que esta hospedado.
		web.AddJavascriptInterface(new PuenteGnothi(this), "gnothiPuente");

		marcador = new TextView(this) { Text = "midiendo…", TextSize = 12 };
		marcador.SetBackgroundColor(Android.Graphics.Color.ParseColor("#111111"));
		marcador.SetTextColor(Android.Graphics.Color.White);
		marcador.SetPadding(24, 40, 24, 24);

		var columna = new LinearLayout(this) { Orientation = Android.Widget.Orientation.Vertical };
		columna.AddView(marcador, new LinearLayout.LayoutParams(
			ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));
		columna.AddView(web, new LinearLayout.LayoutParams(
			ViewGroup.LayoutParams.MatchParent, 0, 1f));
		SetContentView(columna);

		web.LoadUrl(Raiz);
	}

	// La llama el JS cuando el modulo ya se dibujo. msDesdeNavegacion viene de
	// performance.now(), que cuenta desde el inicio de la navegacion.
	// LA RESPUESTA A UN PEDIDO DEL MODULO. Va por evaluateJavascript y en el hilo de la UI,
	// que es el unico desde el que se puede tocar un WebView. Todo lo que viaja se serializa
	// como JSON -tambien el id y el error- para que unas comillas o un salto de linea dentro
	// del sobre no rompan la expresion que se evalua.
	// LO QUE TRAJO EL HANDOFF, esperando a que el modulo este vivo. No se le puede entregar
	// antes: el actor todavia no existe y no habria donde asentarlo.
	private string? asercionesPendientes;

	// Las aserciones viajan en la URL porque hoy son un dato (13 §5.2). Cuando sean muchas no
	// van a entrar y el camino es el archivo por el share sheet.
	private void TomarDelIntent(Intent? intento)
	{
		string? aserciones = intento?.Data?.GetQueryParameter("aserciones");
		if (string.IsNullOrWhiteSpace(aserciones)) return;

		asercionesPendientes = aserciones;
		Android.Util.Log.Info("SPIKE", $"handoff recibido: {aserciones.Length} bytes de aserciones");
	}

	// Si la app ya estaba abierta cuando llego el enlace.
	protected override void OnNewIntent(Intent? intento)
	{
		base.OnNewIntent(intento);
		TomarDelIntent(intento);
		EntregarElTraspaso();
	}

	// Se entrega DESPUES de que el modulo se dibujo, que es cuando su actor ya existe. El
	// resultado se registra: "omitidas" no es un fallo, es que esa asercion ya estaba.
	internal void EntregarElTraspaso()
	{
		if (asercionesPendientes is null || web is null)
		{
			Android.Util.Log.Info("SPIKE", $"nada que entregar (pendiente={asercionesPendientes is not null}, web={web is not null})");
			return;
		}

		Android.Util.Log.Info("SPIKE", "entregando el traspaso al modulo");

		string json = JsonSerializer.Serialize(asercionesPendientes);
		asercionesPendientes = null;

		RunOnUiThread(() => web.EvaluateJavascript(
			$"window.gnothi.traspasar({json}).then(function (r) {{ Spike.traspaso(r); }})", null));
	}

	internal void Responder(string id, string? json, string? error) => RunOnUiThread(() =>
	{
		string cuerpo = json is null ? "null" : JsonSerializer.Serialize(json);
		string motivo = error is null ? "null" : JsonSerializer.Serialize(error);

		web!.EvaluateJavascript(
			$"window.gnothi.respuesta({JsonSerializer.Serialize(id)}, {cuerpo}, {motivo})", null);
	});

	internal void Dibujado(int msDesdeNavegacion)
	{
		long total = Android.OS.SystemClock.ElapsedRealtime() - Arranque;

		var gestor = (ActivityManager)GetSystemService(ActivityService)!;
		Android.OS.Debug.MemoryInfo memoria =
			gestor.GetProcessMemoryInfo(new[] { Android.OS.Process.MyPid() })![0];
		int pss = memoria.TotalPss / 1024;

		string linea = $"proceso→dibujado {total} ms · runtime wasm {msDesdeNavegacion} ms · PSS {pss} MB";
		Android.Util.Log.Info("SPIKE", linea);
		RunOnUiThread(() => marcador!.Text = linea);

	}
}

// Gnothi sirviendo el modulo: los bytes salen de sus propios archivos y la CSP viaja como
// cabecera del documento, que es lo unico que el motor web respeta.
internal sealed class Servidor : WebViewClient
{
	// LA CSP, CON EL HASH CALCULADO AL SERVIR (doc 11 §4).
	//
	// Dos cosas que la rev. 2 no tenia y que costaron una medicion cada una:
	//
	//   connect-src 'self' y no 'none'. Con 'none' el modulo NO ARRANCA: el propio runtime
	//   pide blazor.boot.json con fetch, y connect-src gobierna fetch. Medido en escritorio
	//   el 2026-09-03. Con 'self' sigue sin poder alcanzar al publicador -es otro origen- y
	//   ademas arranca.
	//
	//   'wasm-unsafe-eval', sin el cual el motor no puede compilar wasm.
	//
	// EL HASH SE CALCULA, NO SE ESCRIBE. Un hash a mano se desincroniza en silencio la
	// primera vez que la institucion recompila su bundle, y el sintoma es una pantalla en
	// blanco sin ninguna pista. Gnothi tiene el archivo en la mano cuando lo sirve, asi que
	// lo calcula ahi. Esto ademas es lo que hace la CSP viable de verdad: la alternativa
	// -'unsafe-inline' en script-src- anularia la proteccion entera.
	private const string CspSinScript =
		"default-src 'self'; " +
		"style-src 'self' 'unsafe-inline'; " +
		"connect-src 'self'; " +
		"frame-src 'none'; " +
		"worker-src 'none'";

	// Los <script> SIN src, que son los que la CSP cubre por hash. El grupo captura el
	// cuerpo exacto, que es sobre lo que el motor calcula el suyo: un espacio de mas y no
	// coincide.
	private static readonly System.Text.RegularExpressions.Regex ScriptsEnLinea =
		new(@"<script\b(?![^>]*\bsrc\s*=)[^>]*>(.*?)</script\s*>",
			System.Text.RegularExpressions.RegexOptions.Singleline
			| System.Text.RegularExpressions.RegexOptions.IgnoreCase);

	// Cuando el modulo ya se dibujo, avisa. Se inyecta por evaluateJavascript, que corre
	// fuera de la CSP de la pagina.
	private const string Probe = @"
(function () {
  var i = setInterval(function () {
    var app = document.querySelector('#app');
    if (app && app.innerText && app.innerText.trim().length > 0) {
      clearInterval(i);
      Spike.dibujado(Math.round(performance.now()));
    }
  }, 25);
  setTimeout(function () { clearInterval(i); }, 180000);
})();";


	// CONFORMANCE: EL MODULO NO PUEDE SALIR A LA RED (doc 11 §4 y §15.7).
	//
	// No alcanza con que el modulo NO INTENTE hablar con el publicador: la especificacion
	// dice que NO PUEDE, y eso hay que falsarlo. Esta sonda intenta el fetch que el modulo
	// tiene prohibido -al MISMO publicador que Gnothi si alcanza- y reporta que paso.
	//
	// Si algun dia esto dice "PUDO", la contencion se rompio y el spike lo grita.
	private const string Conformance = @"
fetch('http://127.0.0.1:8095/expediente/1/events?after=0')
  .then(function (r) { Spike.conformance('LA CONTENCION SE ROMPIO: el modulo alcanzo al publicador, HTTP ' + r.status); })
  .catch(function (e) { Spike.conformance('negado por el motor web: ' + e.message); });";

	private readonly MainActivity duenio;

	public Servidor(MainActivity duenio) => this.duenio = duenio;

	// La CSP para ESTE documento: la parte fija mas un hash por cada script en linea que el
	// documento traiga. Si la institucion no trae ninguno, script-src queda sin hashes y es
	// mas estricta todavia.
	private static string CspPara(byte[] documento)
	{
		string html = System.Text.Encoding.UTF8.GetString(documento);

		var hashes = new List<string>();
		foreach (System.Text.RegularExpressions.Match m in ScriptsEnLinea.Matches(html))
		{
			string cuerpo = m.Groups[1].Value;
			if (cuerpo.Length == 0) continue;

			byte[] resumen = System.Security.Cryptography.SHA256.HashData(
				System.Text.Encoding.UTF8.GetBytes(cuerpo));
			hashes.Add($"'sha256-{Convert.ToBase64String(resumen)}'");
		}

		Android.Util.Log.Info("SPIKE", $"CSP: {hashes.Count} script(s) en linea con hash calculado");

		string script = string.Join(" ", new[] { "'self'", "'wasm-unsafe-eval'" }.Concat(hashes));
		return $"{CspSinScript}; script-src {script}";
	}

	public override WebResourceResponse? ShouldInterceptRequest(WebView? view, IWebResourceRequest? request)
	{
		Android.Net.Uri? url = request?.Url;
		if (url is null || !string.Equals(url.Host, MainActivity.Host, StringComparison.Ordinal))
			return null;

		string ruta = (url.Path ?? "/").TrimStart('/');
		if (ruta.Length == 0 || ruta.EndsWith("/", StringComparison.Ordinal)) ruta += "index.html";

		var cabeceras = new Dictionary<string, string> { ["Cache-Control"] = "no-store" };

		try
		{
			// El documento se lee ENTERO antes de servirlo, porque la CSP depende de su
			// contenido. Los demas archivos viajan como stream, sin copiarlos a memoria.
			if (ruta.EndsWith(".html", StringComparison.Ordinal))
			{
				using Stream original = duenio.Assets!.Open(ruta);
				using var buffer = new MemoryStream();
				original.CopyTo(buffer);
				byte[] documento = buffer.ToArray();

				cabeceras["Content-Security-Policy"] = CspPara(documento);
				return new WebResourceResponse(Tipo(ruta), "utf-8", 200, "OK",
					cabeceras, new MemoryStream(documento));
			}

			Stream cuerpo = duenio.Assets!.Open(ruta);
			return new WebResourceResponse(Tipo(ruta), "utf-8", 200, "OK", cabeceras, cuerpo);
		}
		catch (Exception)
		{
			Android.Util.Log.Warn("SPIKE", $"404 {ruta}");
			return new WebResourceResponse("text/plain", "utf-8", 404, "Not Found",
				new Dictionary<string, string>(), new MemoryStream());
		}
	}

	public override void OnPageFinished(WebView? view, string? url)
	{
		base.OnPageFinished(view, url);
		Android.Util.Log.Info("SPIKE", $"documento listo a los {Android.OS.SystemClock.ElapsedRealtime() - MainActivity.Arranque} ms");
		view?.EvaluateJavascript(Probe, null);
		view?.EvaluateJavascript(Conformance, null);
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

// La consola del modulo va a logcat. Es como se ven las violaciones de CSP.
internal sealed class Consola : WebChromeClient
{
	public override bool OnConsoleMessage(ConsoleMessage? mensaje)
	{
		if (mensaje is not null)
			Android.Util.Log.Info("SPIKE-WEB", $"{mensaje.Message()}  ({mensaje.SourceId()}:{mensaje.LineNumber()})");
		return true;
	}
}

internal sealed class Puente : Java.Lang.Object
{

	private readonly MainActivity duenio;

	public Puente(MainActivity duenio) => this.duenio = duenio;

	[JavascriptInterface]
	[Export("dibujado")]
	public void Dibujado(int ms) => duenio.Dibujado(ms);

	// El resultado de la sonda de contencion. Se registra tal cual: es una afirmacion sobre
	// el invariante central, no un detalle de depuracion.
	[JavascriptInterface]
	[Export("conformance")]
	public void Conformance(string mensaje) => Android.Util.Log.Info("SPIKE-CONF", mensaje);

	// Que hizo el modulo con el traspaso.
	[JavascriptInterface]
	[Export("traspaso")]
	public void Traspaso(string resultado) => Android.Util.Log.Info("SPIKE-TRAS", resultado);
}
