using Choreography.Theater;

namespace HolaMundo.App
{
	// EL ACTOR. Es la app viva: la carátula lo construye UNA vez y lo mantiene. No se
	// crea uno por request.
	//
	// `OnHydrated` corre en CADA arranque, incluidos los reinicios. Por eso la semilla va
	// envuelta en `upgrade('semilla')`, que es una-vez-por-actor: la segunda vez que
	// arranques, el upgrade ya aplicado no hace nada. Así el arranque es idempotente sin
	// un ejecutable de migración.
	//
	// ROMPÉ ESTO PARA ENTENDERLO: sacá el `upgrade(...)` y dejá el `libro =
	// Libro();` solo. Corré dos veces. La segunda te borra las facturas, porque la
	// semilla se vuelve a aplicar sobre el journal existente.
	public sealed class HolaMundoApp : PerformanceV2
	{
		public HolaMundoApp(string nombreDelActor)
			: base(nombreDelActor, ElDominio.Assembly)
		{
		}

		protected override void OnHydrated()
		{
			Using(@"
				upgrade('semilla') {
					libro = Libro();
				}
			")
			.PerformCommand();
		}
	}
}
