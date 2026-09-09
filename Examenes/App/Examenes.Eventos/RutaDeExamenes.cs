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
		// DOS TIPOS Y NO UNO, y este es el punto de extension funcionando: el laboratorio
		// empezo a emitir un segundo tipo —los datos del paciente— y lo unico que hizo
		// falta fue agregar un caso. Los tipos que esta app no consume siguen devolviendo
		// null sin tocar lo que ya funciona.
		//
		// El orden no importa: cada Interpretar mira su propio Tipo y devuelve null si no
		// es el suyo.
		public static IMensajeDeEvento Rutear(SenalDeEvento senal) =>
			(IMensajeDeEvento)ExamenAgregado.Interpretar(senal)
			?? DatosDePacienteActualizados.Interpretar(senal);
	}
}
