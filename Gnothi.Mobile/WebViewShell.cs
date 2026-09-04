using System.Text.Json;

namespace Gnothi.Host;

// LA TERCERA CÁSCARA. El Host ya sabía cargar un módulo como proceso hijo (stdio) y como
// ensamblado en su propio proceso (in-process). Esta es la de móvil: el módulo es la PWA de
// la institución corriendo dentro de un WebView que Gnothi controla (especificación 11).
//
// Y `Dispatcher` no cambia una línea. Consentimiento, medición y procedencia siguen siendo
// del Host; lo único que cambia es POR DÓNDE se le pregunta al módulo. Ese es el punto de
// que `IShell` exista.
//
// POR QUÉ ESTA ES LA QUE VA EN MÓVIL, y no in-process: las tiendas prohíben cargar código
// ejecutable descargado dentro del proceso —Play lo dice de dex, JAR e IL; Apple de todo lo
// que no sea WebKit— y la única excepción que ambas nombran es JavaScript en un WebView.
// `InProcessShell` carga IL de la institución por reflexión: en un teléfono eso no se puede
// publicar.
//
// EL ANFITRIÓN SE REGISTRA, no se construye acá. `ChooseShell` es estático y no tiene
// Context, y un WebView necesita uno. La Activity registra quién sabe crearlos al arrancar,
// igual que el módulo web registra su superficie.
internal sealed class WebViewShell : IShell
{
	internal static IAnfitrionDeModulosWeb? Anfitrion;

	private readonly string modulo;
	private readonly string bundle;
	private readonly string journal;
	private readonly string publicador;

	// El primer argumento de la cáscara es la carpeta del bundle web. Mismo campo que usa
	// stdio para el ejecutable y in-process para el dll, otro significado: acá es lo que se
	// SIRVE. La convención del manifiesto es 'web' al lado de 'dominio'.
	public WebViewShell(string name, Shell shell, string grantedFolder)
	{
		modulo = name;
		bundle = Path.GetFullPath(shell.Arguments.Count > 0 ? shell.Arguments[0] : "web");

		// La carpeta otorgada es la del journal del modulo, igual que en las otras cascaras.
		// Hospedado NO va a OPFS: el cuarto pilar exige que Gnothi pueda mirarlo desde afuera
		// del motor web (11 §5.1).
		journal = Path.GetFullPath(grantedFolder);

		// El SEGUNDO argumento es el endpoint del publicador para el cartero, en la forma
		// `baseUrl|recurso|id`. Registrado al instalar el modulo: el modulo no lo elige ni lo
		// puede cambiar (11 §6).
		publicador = shell.Arguments.Count > 1 ? shell.Arguments[1] : "";
	}

	public bool IsRunning => Anfitrion?.EstaVivo(modulo) == true;

	// LA RESPUESTA VIENE OPACA y el Host la reenvía sin mirarla, igual que con las otras dos
	// cáscaras. Es el JSON que armó el dominio de la institución.
	public async Task<object> Invoke(
		string tool, IReadOnlyDictionary<string, object?>? arguments, Accepts accepts, CancellationToken ct)
	{
		IModuloWeb vivo = await Vivo(ct);
		return await vivo.Preguntar(tool, JsonSerializer.Serialize(arguments ?? new Dictionary<string, object?>()), ct);
	}

	// Lo que el módulo DE VERDAD atiende, para el contraste con lo que declaró su manifiesto.
	// Nombres y nada más: es lo único que se compara.
	public async Task<IReadOnlyList<string>> ActualTools(CancellationToken ct)
	{
		IModuloWeb vivo = await Vivo(ct);
		string json = await vivo.Tools(ct);

		try { return JsonSerializer.Deserialize<string[]>(json) ?? []; }
		catch (JsonException) { return []; }
	}

	private async Task<IModuloWeb> Vivo(CancellationToken ct)
	{
		if (Anfitrion is null)
			throw new InvalidOperationException(
				"no hay anfitrión de módulos web registrado: la app tiene que llamar a "
				+ "WebViewShell.Anfitrion antes de despachar. En escritorio esta cáscara no existe.");

		return await Anfitrion.Vivo(modulo, bundle, journal, publicador, ct);
	}

	// DESALOJAR ES OBLIGATORIO, no una optimización: medido, un WebView con el runtime wasm
	// adentro pesa ~150 MB de PSS, y quince módulos vivos no caben (11 §12). No se pierde
	// nada al desalojar porque el módulo no inicia nada: no despierta, no notifica.
	public ValueTask DisposeAsync()
	{
		Anfitrion?.Desalojar(modulo);
		return ValueTask.CompletedTask;
	}
}

// QUIÉN SABE CREAR Y SOSTENER WebViews. Lo implementa la capa de Android, porque necesita
// Context y el hilo de la UI; el Host solo lo nombra.
internal interface IAnfitrionDeModulosWeb
{
	bool EstaVivo(string modulo);

	// Devuelve el módulo LISTO PARA PREGUNTARLE: WebView creado, bundle servido con su CSP,
	// runtime arrancado y actor hidratado. Si ya estaba vivo, lo devuelve tal cual.
	Task<IModuloWeb> Vivo(string modulo, string bundle, string journal, string publicador, CancellationToken ct);

	void Desalojar(string modulo);
}

// UN MÓDULO WEB VIVO. Dos verbos, los mismos que `IShell` necesita.
internal interface IModuloWeb
{
	Task<string> Preguntar(string tool, string argumentosJson, CancellationToken ct);

	Task<string> Tools(CancellationToken ct);
}
