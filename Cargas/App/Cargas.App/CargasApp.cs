using Choreography.Theater;
using Cargas.Dominio;

namespace Cargas.App
{
	// El puppet hospedado. La caratula (consola, browser, servidor MCP) construye ESTO
	// una sola vez y lo mantiene vivo; un actor no se crea por request.
	//
	// La semilla va por OnHydrated y no por el constructor porque OnHydrated corre en
	// CADA hidratacion, incluidos los reinicios, y la guarda de upgrade('nombre') es
	// una-vez-por-actor. Asi el arranque es idempotente sin un .exe de migracion.
	public sealed class CargasApp : PerformanceV2
	{
		public CargasApp(string nombreDelActor)
			: base(nombreDelActor, typeof(BitacoraDeCargas).Assembly)
		{
		}

		protected override void OnHydrated()
		{
			Using(@"
				upgrade('semilla') {
					bitacoraDeCargas = BitacoraDeCargas();
					bitacora = Bitacora();
				}
			")
			.PerformCommand();

			// NO AGREGAR UN SEGUNDO upgrade ACA. La leccion viene de examenes, reportada
			// por Edgar y Jonathan: el segundo upgrade NO asienta en la misma corrida que
			// siembra el journal, asi que la vista de SoloLectura del MCP -que hidrata
			// con OnHydrated vacio- rehidrataba y no encontraba el global. Si alguna vez
			// hace falta uno nuevo, probarlo abriendo el journal desde CERO con el MCP,
			// no solo con la app que lo sembro.
		}
	}
}
