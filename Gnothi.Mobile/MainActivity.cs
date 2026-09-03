using Android.App;
using Android.OS;
using Android.Webkit;
using Gnothi.Movil;

namespace Gnothi.Mobile;

// EL HOST DE GNOTHI EN UN TELEFONO, con cara. LA MITAD DE ANDROID Y NADA MAS.
//
// La cara es UN html en Assets, renderizado por un WebView. Por que no Blazor/MAUI, que era la
// sugerencia natural: Blazor de verdad en Android necesita el workload de MAUI, que se instala
// en Program Files -administrador en esta maquina- y reescribe el proyecto. El WebView da lo
// que se le pedia (UI de calidad web, y el markdown del modelo con tablas de verdad, que un
// TextView nativo no puede) mientras el codigo del Host queda intacto.
//
// Y esa decision se pago sola el dia de iOS: como la cara es un HTML, es EL MISMO archivo alla.
// Lo unico que cambia entre plataformas es quien monta el WebView y como se traduce
// window.prompt(). Todo lo demas -el estado, el consentimiento, la llave, el proveedor, la
// pregunta- vive en Gnothi.Movil/Shell.cs y se comparte tal cual.
//
// LO QUE ESTE ARCHIVO POSEE, entonces: la pantalla, el ciclo de vida y el boton atras. Ni el
// catalogo, ni el consentimiento, ni el despacho: eso es del Host, y son los mismos archivos
// que corre el escritorio.
[Activity(Label = "Gnothi", MainLauncher = true, Theme = "@android:style/Theme.Material.Light.NoActionBar")]
public class MainActivity : Activity
{
	private WebView? web;
	private Shell? shell;

	protected override void OnCreate(Bundle? savedInstanceState)
	{
		base.OnCreate(savedInstanceState);

		web = new WebView(this);
		web.Settings.JavaScriptEnabled = true;
		// La pagina detecta la app por esta marca y NUNCA llama prompt() fuera de ella: en un
		// browser comun ese dialogo es modal y congela la pagina -- medido en el panel de
		// vista previa.
		web.Settings.UserAgentString += " GnothiHost";
		web.SetBackgroundColor(Android.Graphics.Color.ParseColor("#F5F3FB"));
		web.SetWebChromeClient(new Bridge(this));

		// targetSdk 36 hace el edge-to-edge OBLIGATORIO: la pagina se extenderia debajo de la
		// barra de estado y de la de gestos, y la navegacion de abajo termina bajo la del
		// sistema. El arreglo es nativo y en un solo lugar -padding por las barras del
		// sistema- PERO el padding va en un CONTENEDOR, no en el WebView: un WebView ignora su
		// propio padding, lo cual costo una captura de pantalla descubrirlo.
		var frame = new Android.Widget.FrameLayout(this);
		frame.SetBackgroundColor(Android.Graphics.Color.ParseColor("#F5F3FB"));
		frame.AddView(web);
		frame.SetOnApplyWindowInsetsListener(new Insets());

		SetContentView(frame);
		web.LoadUrl("file:///android_asset/ui.html");

		// LAS DOS CARPETAS LAS ELIGE LA PLATAFORMA, no la cascara. adb no puede escribir en el
		// area privada, y eso es correcto: nadie de afuera instala un modulo.
		shell = new Shell(
			new Shell.Paths(
				Files: FilesDir!.AbsolutePath,
				Incoming: System.IO.Path.Combine(GetExternalFilesDir(null)!.AbsolutePath, "incoming")),
			Log,
			(kind, id, payload) => RunOnUiThread(() =>
				web?.EvaluateJavascript(Shell.PushScript(kind, id, payload), null)));

		_ = Task.Run(shell.Run);
	}

	protected override void OnDestroy()
	{
		shell?.Dispose();
		base.OnDestroy();
	}

	// EL BOTON ATRAS le pregunta a la pagina primero: una hoja se cierra, un detalle vuelve al
	// inicio. Solo cuando la pagina contesta false -ya esta en el inicio- la app se hace a un
	// lado, al FONDO y no a la muerte: terminar tiraria el catalogo cargado sin razon.
	//
	// OnBackPressed es el camino deprecado, y se elige a proposito: el moderno
	// OnBackInvokedCallback solo toma el control cuando el manifest lo pide, y esta app no.
	public override void OnBackPressed() =>
		web!.EvaluateJavascript("gn.back()", new BackAnswer(this));

	private sealed class BackAnswer : Java.Lang.Object, IValueCallback
	{
		private readonly MainActivity host;
		public BackAnswer(MainActivity host) => this.host = host;

		public void OnReceiveValue(Java.Lang.Object? value)
		{
			if (value?.ToString() == "true") return;   // la pagina volvio sola
			host.MoveTaskToBack(true);
		}
	}

	private void Log(string s) => global::Android.Util.Log.Info("GNOTHI", s);

	// ── el puente, la mitad que SI es de Android ────────────────────────────────────────
	//
	// La pagina llama window.prompt('gnothi', json). Cualquier otra cosa es un prompt de
	// verdad y recibe el comportamiento normal.
	//
	// Lo unico que hace este tipo es TRADUCIR: saca el json del dialogo y entrega la respuesta
	// por result.Confirm. Que se hace con el pedido es de Shell.Handle, y es identico en iOS
	// -- alla el mismo json llega por WKUIDelegate y la respuesta sale por un completionHandler.
	private sealed class Bridge : WebChromeClient
	{
		private readonly MainActivity host;
		public Bridge(MainActivity host) => this.host = host;

		public override bool OnJsPrompt(WebView? view, string? url, string? message, string? defaultValue, JsPromptResult? result)
		{
			if (message != "gnothi" || defaultValue is null || result is null)
				return base.OnJsPrompt(view, url, message, defaultValue, result);

			result.Confirm(host.shell?.Handle(defaultValue) ?? "{}");
			return true;
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
}
