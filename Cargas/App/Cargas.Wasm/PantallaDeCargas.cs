using Microsoft.AspNetCore.Components;

namespace Cargas.Wasm;

// LO QUE COMPARTEN LAS CUATRO PANTALLAS. Existe por una razon puntual y no por prolijidad:
// el `Intentar` de abajo tiene dos detalles que costaron depuracion, y con cuatro copias la
// proxima pantalla iba a nacer sin alguno de los dos.
//
//   1. El StateHasChanged en el FINALLY. Casi todo lo que deciden estas pantallas vive en
//      EstadoDeLaApp, y Blazor NO observa servicios inyectados. Sin el, "hay que apretar
//      dos veces".
//   2. El error se arma con la cadena entera de InnerException (ver Fallo.Detalle). Un
//      mensaje pelado del motor no dice en que paso se rompio, y estas pantallas corren en
//      el browser de otra persona: no hay debugger del otro lado.
//
// SE RELEE CUANDO EL ESTADO CAMBIA. Con pestañas, el boton de Actualizar vive en el layout
// y lo que hay que releer vive en la pagina, asi que la pagina se suscribe al aviso del
// singleton. Antes de las pestañas esto no hacia falta porque el boton y los datos estaban
// en el mismo componente.
public abstract class PantallaDeCargas : ComponentBase, IDisposable
{
	[Inject] protected EstadoDeLaApp Estado { get; set; } = default!;

	/// <summary>UN solo lugar para los errores, y arriba de todo en cada pantalla: un
	/// fallo abajo queda fuera de pantalla y la app parece no reaccionar al click.</summary>
	protected string Error { get; private set; } = "";

	/// <summary>EL ÉXITO, y se va solo. Ver AvisoDeListo. Cada pantalla decide dónde lo
	/// dibuja; acá solo se garantiza que repinta cuando se apaga.</summary>
	protected AvisoDeListo Listo { get; } = new();

	protected void MostrarListo(string texto) =>
		Listo.Mostrar(texto, () => InvokeAsync(StateHasChanged));

	/// <summary>Lo que la pantalla hace cuando el actor cambio: releer sus consultas.
	/// TODO lo que se ve sale de consultar al actor.</summary>
	protected abstract void Releer();

	protected override void OnInitialized()
	{
		Estado.Cambio += CuandoElEstadoCambia;
		if (Estado.EstaArriba) Releer();
	}

	private void CuandoElEstadoCambia()
	{
		if (Estado.EstaArriba) Releer();
		StateHasChanged();
	}

	protected async Task Intentar(string paso, Func<Task> accion)
	{
		Error = "";
		try
		{
			await accion();
		}
		catch (Exception ex)
		{
			Error = Fallo.Detalle(paso, ex);
		}
		finally
		{
			// Repintar SIEMPRE. Ver el comentario de arriba: sin esto hay que apretar dos
			// veces, y el sintoma no apunta para nada a esta linea.
			StateHasChanged();
		}
	}

	public void Dispose() => Estado.Cambio -= CuandoElEstadoCambia;
}
