using Puppets.Portador;

namespace Facturas.App.Eventos
{
	// La costura de routing de APP1. Forma de InputRouting: senal cruda -> mensaje
	// tipado, y null DESCARTA.
	//
	// Es el punto de extension cuando el proveedor empiece a emitir mas tipos: se
	// agrega un caso, y los tipos que esta app no consume siguen devolviendo null
	// sin tocar lo que ya funciona.
	public static class RutaDeFacturas
	{
		public static IMensajeDeEvento Rutear(SenalDeEvento senal) =>
			FacturaAgregada.Interpretar(senal);
	}
}
