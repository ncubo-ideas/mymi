using Choreography.Theater;

namespace Gimnasio.App
{
	// EL ACTOR. La caratula lo construye una vez y lo mantiene vivo.
	//
	// La semilla va en upgrade('semilla') porque OnHydrated corre en CADA hidratacion y
	// la guarda de upgrade es una-vez-por-actor: asi el arranque es idempotente sin un
	// ejecutable de migracion.
	public sealed class GimnasioApp : PerformanceV2
	{
		public GimnasioApp(string nombreDelActor)
			: base(nombreDelActor, ElDominio.Assembly)
		{
		}

		protected override void OnHydrated()
		{
			Using(@"
				upgrade('semilla') {
					trayectoria = Trayectoria();
				}
			")
			.PerformCommand();
		}
	}
}
