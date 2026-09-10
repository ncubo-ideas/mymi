using Choreography.Theater;

namespace Municipalidad.App
{
	// EL ACTOR. La caratula lo construye una vez y lo mantiene vivo.
	//
	// La semilla va en upgrade('semilla') porque OnHydrated corre en CADA hidratacion y
	// la guarda de upgrade es una-vez-por-actor: asi el arranque es idempotente sin un
	// ejecutable de migracion. La raiz se construye ADENTRO de un comando, como global
	// raiz: un campo de C# no se journaliza y vuelve vacio al siguiente arranque.
	public sealed class MunicipalidadApp : PerformanceV2
	{
		public MunicipalidadApp(string nombreDelActor)
			: base(nombreDelActor, ElDominio.Assembly)
		{
		}

		protected override void OnHydrated()
		{
			Using(@"
				upgrade('semilla') {
					cuenta = Cuenta();
				}
			")
			.PerformCommand();
		}
	}
}
