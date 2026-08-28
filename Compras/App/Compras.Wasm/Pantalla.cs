using Microsoft.JSInterop;

namespace Compras.Wasm;

/// <summary>Si la pantalla es angosta, y avisa cuando eso cambia.</summary>
// EXISTE POR UNA SOLA COSA QUE EL CSS NO PUEDE HACER: elegir QUE FILAS SE RENDERIZAN. Todo
// lo demas que cambia por resolucion en esta app -la barra que se va al pie, la tabla que se
// apila, las tarjetas que se acomodan- son formas distintas del MISMO contenido, y para eso
// alcanza un @media.
//
// El detalle de una compra no: en escritorio pagina de a quince y en telefono muestra quince
// con un boton que abre el resto. Son dos subconjuntos distintos de las lineas, y quien los
// elige es C#, que no ve el ancho de la ventana.
//
// SI APARECE UNA SEGUNDA PANTALLA QUE LO NECESITE, esto ya es el lugar. Si no aparece
// ninguna, sobra — y conviene acordarse de que sobra en vez de dejarlo crecer.
public sealed class Pantalla : IAsyncDisposable
{
	private readonly IJSRuntime js;
	private IJSObjectReference? modulo;

	public Pantalla(IJSRuntime js)
	{
		ArgumentNullException.ThrowIfNull(js);
		this.js = js;
	}

	private async ValueTask<IJSObjectReference> Modulo() =>
		modulo ??= await js.InvokeAsync<IJSObjectReference>("import", "./pantalla-interop.js");

	public async ValueTask<bool> EsAngosta() =>
		await (await Modulo()).InvokeAsync<bool>("esAngosta");

	/// <summary>Avisa cuando se CRUZA el corte, no en cada pixel que se arrastra.</summary>
	// La referencia la crea y la suelta QUIEN OBSERVA, no esto: si la creara aca, habria que
	// llevar la cuenta de cuantos componentes estan escuchando para saber cuando soltarla, y
	// una referencia de interop que no se suelta es una fuga que el GC no puede ver.
	public async ValueTask Observar<T>(DotNetObjectReference<T> quien) where T : class =>
		await (await Modulo()).InvokeVoidAsync("observar", quien);

	public async ValueTask DisposeAsync()
	{
		if (modulo is null) return;

		// Al cerrar la pestaña el runtime de JS ya puede estar caido, y ahi el dispose tira.
		// No hay nada que recuperar: la pagina se esta yendo.
		try { await modulo.DisposeAsync(); }
		catch (JSDisconnectedException) { }
	}
}
