using Puppets.Portador;

namespace Compras.App.Eventos
{
	// La costura de routing de este negocio. Forma de InputRouting: senal cruda ->
	// mensaje tipado, y null DESCARTA.
	//
	// Es el punto de extension cuando el supermercado empiece a emitir mas tipos
	// (una devolucion, un cambio de precio): se agrega un caso, y los tipos que esta
	// app no consume siguen devolviendo null sin tocar lo que ya funciona.
	public static class RutaDeCompras
	{
		public static IMensajeDeEvento Rutear(SenalDeEvento senal) =>
			CompraRegistrada.Interpretar(senal);
	}
}
