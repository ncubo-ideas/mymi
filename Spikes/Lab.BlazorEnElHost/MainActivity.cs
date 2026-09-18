using Android.App;
using Android.OS;
using Android.Webkit;
using Java.Interop;
using Microsoft.AspNetCore.Components;

using Stopwatch = System.Diagnostics.Stopwatch;

namespace Lab.BlazorEnElHost;

/// EL SPIKE DE LA CUARTA FORMA: componentes razor de la institucion corriendo en el .NET NATIVO
/// de la app, y del otro lado del puente un WebView que solo aplica diffs.
///
/// TRES PREGUNTAS, y las tres se contestan mirando el logcat con el tag `SPIKE`:
///
///   1. ¿arranca un `Renderer` propio en Mono/Android, sin MAUI?
///   2. ¿llega el `RenderBatch` al otro lado con contenido usable, y en cuanto?
///   3. ¿vuelve un evento, corre el manejador NATIVO, y re-renderiza?
///
/// LA CUARTA LA DA `dumpsys meminfo` y el tamano del apk, para comparar contra los 45,6 MB que
/// cuesta el primer modulo en la forma hibrida.
[Activity(Label = "Spike Blazor en el Host", MainLauncher = true,
	Theme = "@android:style/Theme.Material.Light.NoActionBar")]
public class MainActivity : Activity
{
	private const string Tag = "SPIKE";

	private WebView? vista;
	private RenderizadorAlPuente? renderizador;
	private readonly Stopwatch reloj = new();

	protected override void OnCreate(Bundle? savedInstanceState)
	{
		base.OnCreate(savedInstanceState);

		vista = new WebView(this);
		WebView.SetWebContentsDebuggingEnabled(true);
		vista.Settings.JavaScriptEnabled = true;
		vista.Settings.AllowFileAccess = false;
		vista.AddJavascriptInterface(new Puente(this), "spikePuente");
		vista.SetWebViewClient(new AlCargar(this));

		// LA CONSOLA DE LA PAGINA AL LOGCAT, y sin esto el spike es ciego: un error de javascript
		// se ve exactamente igual que «el boton no hace nada».
		vista.SetWebChromeClient(new AlHablar());
		SetContentView(vista);

		// El html y el aplicador salen de los assets: no hay red, igual que una cara hibrida.
		vista.LoadUrl("file:///android_asset/cara.html");
	}

	/// ARRANCA CUANDO LA PAGINA AVISO QUE ESTA LISTA. Antes no: empujar un lote a un documento que
	/// todavia no existe es el error que en Gnothi resolvio `gnothiPuente.listo`.
	internal async void Arrancar()
	{
		reloj.Restart();

		// SEGUNDA VUELTA: la pantalla REAL de la institucion, `MisCompras.razor`, 1798 lineas,
		// compilada desde su propio repo sin tocar una linea.
		//
		// Y AHORA SI HACE FALTA UN CONTENEDOR. El contador no inyectaba nada; esta pantalla inyecta
		// tres cosas -- `EstadoDeLaApp`, `NavigationManager` y `ConfiguracionDeLaInstitucion` --, y
		// el `Renderer` las resuelve por el `IServiceProvider` que se le pasa. O sea que el «no
		// hace falta inyeccion» de la primera vuelta valia para un componente de juguete.
		var servicios = new ServiciosDelHost(
			new Compras.Wasm.EstadoDeLaApp(Carpetas.DelModulo(this)),
			new NavegadorDelHost(s => Android.Util.Log.Info(Tag, s)),
			new Compras.Wasm.ConfiguracionDeLaInstitucion());

		renderizador = new RenderizadorAlPuente(servicios, Empujar);

		await renderizador.Dispatcher.InvokeAsync(() =>
			renderizador.Montar(typeof(MisCompras), ParameterView.Empty));

		// EL NOMBRE DE LA CLASE LO DECIDE EL `_Imports.razor` DE ESTE PROYECTO, no la carpeta de
		// origen: el archivo de la institucion vive en `Compras.Wasm.Pages` en su repo y aca quedo en
		// `Lab.BlazorEnElHost`. Es un detalle, pero dice algo de la forma: al compilar el razor de
		// otro, el namespace es del que compila.

		Android.Util.Log.Info(Tag, $"primer render: {reloj.ElapsedMilliseconds} ms");
	}

	/// LO QUE VUELVE DEL PUENTE: un id de manejador y nada mas. El manejador ES codigo de la
	/// institucion y corre de ESTE lado, que es justo lo bueno del mecanismo y justo la objecion.
	internal async void Evento(ulong manejador)
	{
		if (renderizador is null) return;

		reloj.Restart();
		await renderizador.Evento(manejador);
		Android.Util.Log.Info(Tag, $"evento {manejador} y su re-render: {reloj.ElapsedMilliseconds} ms");
	}

	private void Empujar(string lote)
	{
		Android.Util.Log.Info(Tag, $"lote de {lote.Length} bytes: {lote}");
		RunOnUiThread(() => vista?.EvaluateJavascript(
			"window.aplicar(" + lote + ")", null));
	}

	private sealed class AlHablar : WebChromeClient
	{
		public override bool OnConsoleMessage(ConsoleMessage? m)
		{
			Android.Util.Log.Info(Tag, $"js: {m?.Message()}");
			return true;
		}
	}

	private sealed class AlCargar(MainActivity dueno) : WebViewClient
	{
		public override void OnPageFinished(WebView? v, string? url) =>
			Android.Util.Log.Info(Tag, "pagina cargada; esperando su 'listo'");
	}

	/// EL PUENTE, con dos verbos. El de Gnothi tiene ocho; este spike no necesita mas.
	private sealed class Puente(MainActivity dueno) : Java.Lang.Object
	{
		[JavascriptInterface, Export("listo")]
		public void Listo() => dueno.Arrancar();

		[JavascriptInterface, Export("evento")]
		public void Evento(string manejador)
		{
			if (ulong.TryParse(manejador, out ulong id)) dueno.Evento(id);
		}
	}
}

/// EL CONTENEDOR MINIMO, y la primera vuelta del spike dijo que no hacia falta: era mentira de un
/// componente de juguete. `MisCompras.razor` inyecta tres cosas, y `Renderer` las busca por aca.
///
/// Es un `switch`, no un contenedor de verdad: registrar tres tipos a mano deja ver EXACTAMENTE lo
/// que la pantalla pide, que es lo que el spike vino a contar. Una cuarta forma usaria
/// Microsoft.Extensions.DependencyInjection y entonces el Host tendria que decidir que hay adentro
/// -- que es una decision de contencion, no de comodidad.
internal sealed class ServiciosDelHost(
	Compras.Wasm.EstadoDeLaApp estado,
	NavegadorDelHost navegador,
	Compras.Wasm.ConfiguracionDeLaInstitucion institucion) : IServiceProvider
{
	public object? GetService(Type servicio) =>
		servicio == typeof(Compras.Wasm.EstadoDeLaApp) ? estado
		: servicio == typeof(Microsoft.AspNetCore.Components.NavigationManager) ? navegador
		: servicio == typeof(Compras.Wasm.ConfiguracionDeLaInstitucion) ? institucion
		: null;
}

internal static class Carpetas
{
	/// LA CARPETA CONCEDIDA. En Gnothi la da el Host; aca el spike usa la suya, vacia: lo que se
	/// mide es el MONTAJE de la pantalla, no sus datos.
	public static string DelModulo(Android.Content.Context c) =>
		System.IO.Path.Combine(c.FilesDir!.AbsolutePath, "compras");
}
