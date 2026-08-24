using Choreography.Theater;
using Examenes.Dominio;

namespace Examenes.App
{
	// El puppet hospedado. La caratula (consola, browser, servidor MCP) construye ESTO
	// una sola vez y lo mantiene vivo; un actor no se crea por request.
	//
	// La semilla va por OnHydrated y no por el constructor porque OnHydrated corre en
	// CADA hidratacion, incluidos los reinicios, y la guarda de upgrade('nombre') es
	// una-vez-por-actor. Asi el arranque es idempotente sin un .exe de migracion: un
	// upgrade ya aplicado no hace nada, uno nuevo se aplica y se journaliza.
	//
	// La EDAD del paciente entra como argumento del constructor del Expediente, en la
	// semilla. Esta quemada en 60 para el demo, y eso es deliberado por dos razones: el
	// dominio no puede leer el reloj para calcularla (rompe el replay), y el proveedor
	// todavia no manda fecha de nacimiento. El dia que la mande, entra por el mismo
	// lugar y queda journalizada como cualquier otro dato.
	public sealed class ExamenesApp : PerformanceV2
	{
		// La edad del paciente del demo. Los rangos de referencia del dominio dependen
		// de ella: a los 60 el techo del antigeno prostatico es 4.5 y el del colesterol
		// baja a 200.
		public const int EdadDelPacienteDelDemo = 60;

		public ExamenesApp(string nombreDelActor)
			: base(nombreDelActor, typeof(Expediente).Assembly)
		{
		}

		protected override void OnHydrated()
		{
			Using(@"
				upgrade('semilla') {
					expediente = Expediente(60);
					bitacora = Bitacora();
				}
			")
			.PerformCommand();
		}
	}
}
