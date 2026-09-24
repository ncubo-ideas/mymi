using Microsoft.JSInterop;

namespace Facturas.Wasm;

// EL ALIAS PERSONAL DE UNA FACTURA (p.ej. "Birthday party" para TQ-0003), fuera del
// journal Y fuera de Facturas.App.Vistas -mismo criterio que AlmacenDeCredencial (ver
// ese archivo): esto no es un hecho de la vida de la persona emitido por el proveedor,
// es una etiqueta que la persona le pone a su propio historial ACA, en este
// dispositivo. FacturaEnLista/FacturaDetallada las consumen DOS caratulas -la UI del
// browser y las tools del servidor MCP- y ese alias es un concepto de la UI, no algo
// que deba viajar hasta ahi ni journalizarse: por eso no se agrego "Nombre" al dominio
// ni a la proyeccion compartida, y vive en localStorage del origen (alias-interop.js),
// exactamente igual que la credencial.
//
// Cacheada en memoria tras la primera lectura -CargarAsync()- para que las filas de
// una lista puedan preguntar Obtener() de forma SINCRONA, sin un round-trip a JS por
// fila. Quien renderiza la lista es responsable de haber llamado CargarAsync() antes
// (MainLayout ya lo hace una vez por navegacion; es idempotente).
public sealed class AlmacenDeAlias
{
	private readonly IJSRuntime js;
	private IJSObjectReference? modulo;
	private Dictionary<string, string> enMemoria = new(StringComparer.Ordinal);
	private bool cargado;

	public AlmacenDeAlias(IJSRuntime js)
	{
		ArgumentNullException.ThrowIfNull(js);
		this.js = js;
	}

	private async ValueTask<IJSObjectReference> Modulo() =>
		modulo ??= await js.InvokeAsync<IJSObjectReference>("import", "./alias-interop.js");

	public async ValueTask CargarAsync()
	{
		if (cargado) return;
		enMemoria = await (await Modulo()).InvokeAsync<Dictionary<string, string>>("leerAlias")
		            ?? new Dictionary<string, string>(StringComparer.Ordinal);
		cargado = true;
	}

	// Sincrono a proposito: se pregunta al renderizar cada fila, y para entonces
	// CargarAsync() ya corrio (MainLayout la llama en su OnInitializedAsync, antes de
	// que cualquier pagina hija renderice por primera vez).
	public string Obtener(string numero) =>
		enMemoria.TryGetValue(numero, out string? alias) ? alias : "";

	public async ValueTask GuardarAsync(string numero, string alias)
	{
		ArgumentNullException.ThrowIfNull(numero);

		alias = (alias ?? "").Trim();
		if (alias.Length == 0) enMemoria.Remove(numero);
		else enMemoria[numero] = alias;

		await (await Modulo()).InvokeVoidAsync("guardarAlias", enMemoria);
		cargado = true;
	}
}
