using Android.App;
using Android.Views;
using Android.Webkit;
using Android.Widget;
using Java.Interop;

namespace Lab.Sala;

// LA SALA CORRIENDO ADENTRO DE UN APK, en el WebView de la app y con los bytes saliendo de sus
// propios assets. Es la misma corrida que la nota 49 midio por `adb reverse` contra Chrome, sin
// las dos dudas que aquella dejaba: ni la laptop en el medio, ni un navegador que no es el de
// la app.
//
// Lo que la pagina hace adentro -- cargar cinco .dll que el sustrato nunca referencio,
// descubrir su superficie por reflexion, montarles una carpeta en memoria e invocarlos -- esta
// en Sustrato/Program.cs. Aca solo se le sirven los bytes y se anota lo que dice.
[Activity(Label = "La sala", MainLauncher = true, Theme = "@android:style/Theme.Material.Light.NoActionBar")]
public class MainActivity : Activity
{
	// EL ORIGEN DE LA SALA. https con host virtual y no un esquema propio, por la misma razon
	// medida en Lab.WebViewWasm: un esquema propio no es contexto seguro, y sin contexto seguro
	// se cae lo que el runtime necesita. ShouldInterceptRequest atiende ANTES de la red: no hay
	// DNS ni TLS, el host no existe, y los bytes salen de los assets.
	//
	// Y es UN solo origen, que es justamente la forma: la sala es de Gnothi, no de ninguna
	// institucion. Adentro de este documento no hay una linea de JavaScript de nadie mas.
	internal const string Host = "sala.gnothi.local";

	private const string Raiz = "https://" + Host + "/";

	internal static long Arranque;

	private TextView? marcador;
	private WebView? web;

	protected override void OnCreate(Bundle? savedInstanceState)
	{
		Arranque = Android.OS.SystemClock.ElapsedRealtime();
		base.OnCreate(savedInstanceState);

		WebView.SetWebContentsDebuggingEnabled(true);

		web = new WebView(this);
		web.Settings.JavaScriptEnabled = true;
		web.SetWebViewClient(new Servidor(this));
		web.SetWebChromeClient(new Consola());
		web.AddJavascriptInterface(new Puente(this), "Sala");

		marcador = new TextView(this) { Text = "arrancando la sala…", TextSize = 12 };
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

	// Cada linea que la sala escribe va tambien a logcat, para poder leer la corrida sin
	// depender de que alguien mire la pantalla. Y en las dos lineas que cierran la corrida se
	// anota lo que la pagina no puede saber de si misma: cuanto tardo desde que arranco el
	// proceso, y cuanta memoria cobra Android por todo esto.
	internal void Linea(string linea)
	{
		Android.Util.Log.Info("SALA", linea);

		if (!linea.StartsWith("    sustrato solo", StringComparison.Ordinal)
			&& !linea.StartsWith("=== VEREDICTO", StringComparison.Ordinal)
			&& !linea.StartsWith("=== EL SUSTRATO COMPARTIDO", StringComparison.Ordinal))
			return;

		long total = Android.OS.SystemClock.ElapsedRealtime() - Arranque;

		var gestor = (ActivityManager)GetSystemService(ActivityService)!;
		Android.OS.Debug.MemoryInfo memoria =
			gestor.GetProcessMemoryInfo(new[] { Android.OS.Process.MyPid() })![0];
		int pss = memoria.TotalPss / 1024;

		string resumen = $"proceso→aqui {total} ms · PSS del proceso {pss} MB";
		Android.Util.Log.Info("SALA-MEDIDA", $"{resumen}   ({linea.Trim()})");
		RunOnUiThread(() => marcador!.Text = resumen);
	}
}

// Gnothi sirviendo su propia sala: los bytes salen de sus assets y la CSP viaja como cabecera
// del documento, que es lo unico que el motor web respeta.
internal sealed class Servidor : WebViewClient
{
	// LA CSP. connect-src 'self' y no 'none' porque el propio runtime pide su manifiesto con
	// fetch, y la sala pide ademas los .dll de los modulos -- todo contra su mismo origen, que
	// lo atiende este interceptor. 'wasm-unsafe-eval' porque sin el no se compila wasm.
	//
	// Y lo que NO esta: no hay forma de alcanzar otro origen desde adentro.
	private const string CspSinScript =
		"default-src 'self'; " +
		"style-src 'self' 'unsafe-inline'; " +
		"connect-src 'self'; " +
		"frame-src 'none'; " +
		"worker-src 'none'";

	// EL HASH SE CALCULA, NO SE ESCRIBE. Un hash a mano se desincroniza en silencio la primera
	// vez que se recompila, y el sintoma es una pantalla en blanco sin ninguna pista.
	private static readonly System.Text.RegularExpressions.Regex ScriptsEnLinea =
		new(@"<script\b(?![^>]*\bsrc\s*=)[^>]*>(.*?)</script\s*>",
			System.Text.RegularExpressions.RegexOptions.Singleline
			| System.Text.RegularExpressions.RegexOptions.IgnoreCase);

	private readonly MainActivity duenio;

	public Servidor(MainActivity duenio) => this.duenio = duenio;

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

		string enAssets = "sala/" + ruta;
		var cabeceras = new Dictionary<string, string> { ["Cache-Control"] = "no-store" };

		try
		{
			// El documento se lee entero antes de servirlo, porque la CSP depende de su
			// contenido. Los demas viajan como stream, sin copiarlos a memoria.
			if (ruta.EndsWith(".html", StringComparison.Ordinal))
			{
				using Stream original = duenio.Assets!.Open(enAssets);
				using var buffer = new MemoryStream();
				original.CopyTo(buffer);
				byte[] documento = buffer.ToArray();

				cabeceras["Content-Security-Policy"] = CspPara(documento);
				return new WebResourceResponse(Tipo(ruta), "utf-8", 200, "OK",
					cabeceras, new MemoryStream(documento));
			}

			Stream cuerpo = duenio.Assets!.Open(enAssets);
			return new WebResourceResponse(Tipo(ruta), "utf-8", 200, "OK", cabeceras, cuerpo);
		}
		catch (Exception)
		{
			Android.Util.Log.Warn("SALA", $"404 {enAssets}");
			return new WebResourceResponse("text/plain", "utf-8", 404, "Not Found",
				new Dictionary<string, string>(), new MemoryStream());
		}
	}

	// El .wasm NECESITA su tipo o el motor no lo compila en streaming. Y el .dll de un modulo
	// viaja como octet-stream: para el motor web es un archivo cualquiera, lo que ES -- quien
	// sabe que hacer con el es el runtime de adentro.
	private static string Tipo(string ruta) => Path.GetExtension(ruta).ToLowerInvariant() switch
	{
		".html" => "text/html",
		".js" or ".mjs" => "text/javascript",
		".css" => "text/css",
		".wasm" => "application/wasm",
		".json" => "application/json",
		".dat" => "application/octet-stream",
		".dll" => "application/octet-stream",
		".png" => "image/png",
		".ico" => "image/x-icon",
		_ => "application/octet-stream",
	};
}

// La consola de la sala va a logcat. Es como se ven las violaciones de CSP.
internal sealed class Consola : WebChromeClient
{
	public override bool OnConsoleMessage(ConsoleMessage? mensaje)
	{
		if (mensaje is not null)
			Android.Util.Log.Info("SALA-WEB", $"{mensaje.Message()}  ({mensaje.SourceId()}:{mensaje.LineNumber()})");
		return true;
	}
}

internal sealed class Puente : Java.Lang.Object
{
	private readonly MainActivity duenio;

	public Puente(MainActivity duenio) => this.duenio = duenio;

	[JavascriptInterface]
	[Export("linea")]
	public void Linea(string linea) => duenio.Linea(linea);
}
