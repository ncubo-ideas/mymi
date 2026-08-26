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

			// NO AGREGAR UN SEGUNDO upgrade ACA SIN LEER ESTO.
			//
			// Hubo uno -'permisos-de-la-ia'- y dio problemas al Host. Reportado por Edgar
			// y Jonathan con receta de reproduccion: el segundo upgrade NO asienta en la
			// misma corrida que siembra el journal, asi que la vista de SoloLectura del
			// MCP -que hidrata con OnHydrated vacio- rehidrataba y no encontraba el
			// global. Las tools fallaban con algo que no era culpa del Host.
			//
			// Verificado en un journal real: 'semilla' y 'permisos-de-la-ia' aparecen
			// DOS veces cada uno, o sea que la guarda una-vez-por-actor tampoco estaba
			// conteniendo lo que parecia contener en la corrida de siembra.
			//
			// La funcionalidad se fue por otra razon -no era responsabilidad de esta app-,
			// pero el mecanismo sigue siendo delicado: si alguna vez hace falta un upgrade
			// nuevo, hay que probarlo abriendo el journal desde CERO con el MCP, no solo
			// con la app que lo sembro.
		}
	}
}
