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
	// semilla. Esta quemada para el demo, y eso es deliberado por dos razones: el
	// dominio no puede leer el reloj para calcularla (rompe el replay), y el proveedor
	// todavia no manda fecha de nacimiento. El dia que la mande, entra por el mismo
	// lugar y queda journalizada como cualquier otro dato.
	//
	// ⚠️ CAMBIAR ESTA EDAD NO ALCANZA PARA UN JOURNAL QUE YA EXISTE. upgrade('semilla') es
	// una-vez-por-actor: un expediente ya sembrado se queda con la edad con la que nacio.
	// Para que la demo muestre la edad nueva hay que sembrar una carpeta desde cero.
	public sealed class ExamenesApp : PerformanceV2
	{
		// La edad de Sonia, la persona de la demo. Los rangos de referencia del dominio
		// dependen de ella, y a los 34 no son los mismos que a los 60: el techo del
		// colesterol es 220 y no 200, el del LDL 130 y no 100, y un colesterol fuera de
		// rango se lee "a tu edad esto se corrige" en vez de "que lo vea tu medico".
		//
		// Es la edad que sostiene el veredicto ambar de la landing: 212 mg/dL a los 34
		// subio de verdad Y sigue dentro de lo deseable. A los 60 el mismo numero seria un
		// fuera de rango, y la portada diria otra cosa.
		public const int EdadDelPacienteDelDemo = 34;

		public ExamenesApp(string nombreDelActor)
			: base(nombreDelActor, typeof(Expediente).Assembly)
		{
		}

		protected override void OnHydrated()
		{
			Using(@"
				upgrade('semilla') {
					expediente = Expediente(34);
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
