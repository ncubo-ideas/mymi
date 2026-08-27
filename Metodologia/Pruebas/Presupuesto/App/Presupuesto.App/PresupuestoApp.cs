using System.Reflection;
using Choreography.Theater;

namespace Presupuesto.App
{
	// EL PUPPET HOSPEDADO. La carátula —consola, browser, servidor MCP— construye ESTO una
	// sola vez y lo mantiene vivo. Un actor no se crea por request.
	//
	// EL DOMINIO SE CARGA POR ARCHIVO, no con typeof. Ésa es la pieza que mantiene el
	// dominio entero `internal`: si dijera `typeof(Panorama).Assembly`, este proyecto
	// tendría que nombrar un tipo del dominio y el modelo tendría que abrirse. No hace
	// falta — el motor resuelve los tipos por reflexión desde el texto del script, y la
	// reflexión no tiene frontera de assembly.
	//
	// PRECAUCIÓN PARA WASM Y WASI, y conviene decirla completa porque es un pie que este
	// código deja puesto: `Assembly.Load` no deja una referencia IL dura, así que el
	// trimmer que Blazor activa en publish Release podría sacar el dominio del output. El
	// síntoma sería un `dotnet run` que anda y un publish que revienta. La respuesta es
	// `<TrimmerRootAssembly Include="Presupuesto.Dominio" />` en el csproj del host que
	// publique a wasm — y vale saber que ese riesgo existe igual con `typeof`, que ancla
	// el tipo pero no los miembros que el DSL resuelve.
	public sealed class PresupuestoApp : PerformanceV2
	{
		// El 10 % del juicio 2 de Preguntas.md. Entra en la semilla y queda journalizado:
		// si mañana el umbral cambia, entra como un upgrade nuevo y el journal conserva
		// contra qué umbral se evaluó cada mes.
		public const string UmbralDeVariacion = "0.10";

		public PresupuestoApp(string nombreDelActor)
			: base(nombreDelActor, ElDominio.Assembly)
		{
		}

		// La semilla va por OnHydrated y no por el constructor porque OnHydrated corre en
		// CADA hidratación —incluidos los reinicios— y la guarda de `upgrade('nombre')` es
		// una-vez-por-actor. Así el arranque es idempotente sin un ejecutable de
		// migración.
		//
		// UN SOLO upgrade, y no es casualidad. El README de Exámenes documenta que un
		// segundo upgrade no asienta en la misma corrida que siembra el journal, y que la
		// vista de sólo lectura del MCP rehidrataba sin encontrar el global. Si alguna vez
		// hace falta uno nuevo, hay que probarlo abriendo el journal desde CERO con otro
		// proceso, no sólo con la app que lo sembró.
		protected override void OnHydrated()
		{
			Using(@"
				upgrade('semilla') {
					panorama = Panorama(" + UmbralDeVariacion + @"m);
				}
			")
			.PerformCommand();
		}
	}
}
