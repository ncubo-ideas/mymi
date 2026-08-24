using Puppets.Portador;

namespace Examenes.App.Eventos
{
	// La costura de routing de este negocio. Forma de InputRouting: senal cruda ->
	// mensaje tipado, y null DESCARTA.
	//
	// Es el punto de extension cuando el laboratorio empiece a emitir mas tipos: se
	// agrega un caso, y los tipos que esta app no consume siguen devolviendo null sin
	// tocar lo que ya funciona.
	public static class RutaDeExamenes
	{
		public static IMensajeDeEvento Rutear(SenalDeEvento senal) =>
			ExamenAgregado.Interpretar(senal);
	}
}
