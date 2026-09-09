using System.Text.Json.Serialization;
using Microsoft.JSInterop;

namespace Cargas.Wasm;

// EL PUENTE DEL TRASPASO hacia el browser: por donde sale el sobre de anotaciones cuando la
// persona toca "pasar mis anotaciones a Gnothi". Solo plomeria del browser -la hoja de
// compartir, la descarga, el user agent-; que lleva el sobre y que hace el modulo con el lo
// decide Cargas.App.Traspaso, que no sabe que existe un browser.
public sealed class PuenteDeTraspaso : IAsyncDisposable
{
	private readonly IJSRuntime js;
	private IJSObjectReference? modulo;

	public PuenteDeTraspaso(IJSRuntime js)
	{
		ArgumentNullException.ThrowIfNull(js);
		this.js = js;
	}

	private async ValueTask<IJSObjectReference> Modulo() =>
		modulo ??= await js.InvokeAsync<IJSObjectReference>("import", "./traspaso-interop.js");

	// Android decide la FORMA del enlace del handoff: intent:// con fallback alla, https
	// normal en el resto. Es lo unico para lo que la app mira el user agent.
	public async ValueTask<bool> EsAndroid() =>
		await (await Modulo()).InvokeAsync<bool>("esAndroid");

	// Si este browser tiene hoja de compartir con archivos. Sin ella, compartir degrada a
	// descargar, y la pantalla lo anticipa en el texto de la tecla.
	public async ValueTask<bool> PuedeCompartirArchivos() =>
		await (await Modulo()).InvokeAsync<bool>("puedeCompartirArchivos");

	// EXIGE GESTO: desde un click. Ver traspaso-interop.js.
	public async ValueTask<ResultadoDeCompartir> Compartir(string nombre, string contenido, string titulo) =>
		await (await Modulo()).InvokeAsync<ResultadoDeCompartir>("compartir", nombre, contenido, titulo);

	public async ValueTask DisposeAsync()
	{
		if (modulo is not null)
		{
			try { await modulo.DisposeAsync(); } catch (JSDisconnectedException) { }
			modulo = null;
		}
	}
}

public sealed class ResultadoDeCompartir
{
	[JsonPropertyName("ok")]     public bool Ok { get; set; }

	// 'compartido' si salio por la hoja de compartir, 'descargado' si el browser no la tiene
	// y el archivo se bajo. La pantalla dice distinto cada caso.
	[JsonPropertyName("como")]   public string Como { get; set; } = "";

	[JsonPropertyName("motivo")] public string Motivo { get; set; } = "";

	public bool Cancelado => !Ok && Motivo == "cancelado";
}
