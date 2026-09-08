using Choreography.Theater;

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
	// ⚠️ CAMBIAR EL CUERPO DE ESTA SEMILLA ROMPE UN JOURNAL QUE YA EXISTE, y no en
	// silencio: la app NO ABRE. Medido en el browser al pasar la edad de 60 a 34, con el
	// journal viejo en el almacenamiento:
	//
	//   Puppeteer.LanguageException: Upgrade 'semilla' differs from the one previously
	//   applied to this actor. To change the logic of an already-applied upgrade, give it
	//   a new name (e.g. 'semilla_v2').
	//     Applied:             { expediente = Expediente(60); bitacora = Bitacora();}
	//     Attempting to apply: { expediente = Expediente(34); bitacora = Bitacora();}
	//
	// O sea que el motor NO deja que dos actores con el mismo nombre de upgrade tengan
	// cuerpos distintos, y hace bien: el journal se reproduce con el cuerpo que se
	// journalizo, no con el que hay en el codigo hoy.
	//
	// QUE HACER: borrar el journal —es un demo, no hay dato de nadie que preservar—. Para
	// la carpeta del disco es un rm; para el browser, limpiar el almacenamiento del sitio,
	// o abrir con "Abrir sin guardar nada", que arranca un actor nuevo cada vez.
	//
	// LO QUE NO HAY QUE HACER es renombrarla a 'semilla_v2' para esquivar el error. Sobre
	// un journal existente eso aplica un upgrade NUEVO que reasigna los dos globales, o
	// sea que tira el expediente y el checkpoint que ya estaban; y ademas es exactamente
	// el mecanismo del que habla la nota de abajo, que ya dio problemas al Host.
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
			: base(nombreDelActor, ElDominio.Assembly)
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
