using Choreography.Theater;
using Compras.Dominio;

namespace Compras.App
{
	// El puppet hospedado. La caratula (consola, browser, servidor MCP) construye ESTO
	// una sola vez y lo mantiene vivo; un actor no se crea por request.
	//
	// La semilla va por OnHydrated y no por el constructor porque OnHydrated corre en
	// CADA hidratacion, incluidos los reinicios, y la guarda de upgrade('nombre') es
	// una-vez-por-actor. Asi el arranque es idempotente sin un .exe de migracion: un
	// upgrade ya aplicado no hace nada, uno nuevo se aplica y se journaliza.
	public sealed class ComprasApp : PerformanceV2
	{
		public ComprasApp(string nombreDelActor)
			: base(nombreDelActor, typeof(Historial).Assembly)
		{
		}

		protected override void OnHydrated()
		{
			Using(@"
				upgrade('semilla') {
					compras = Historial();
					bitacora = Bitacora();
				}
			")
			.PerformCommand();
		}
	}
}
