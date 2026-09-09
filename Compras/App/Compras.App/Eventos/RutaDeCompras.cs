using Puppets.Portador;

namespace Compras.App.Eventos
{
	// La costura de routing de este negocio. Forma de InputRouting: senal cruda ->
	// mensaje tipado, y null DESCARTA.
	//
	// Es el punto de extension cuando el supermercado emite mas tipos, y ya paso: la
	// promocion es el segundo. Cada Interpretar solo reconoce SU tipo y devuelve null
	// para el resto, asi que encadenarlos con ?? es el switch — y los tipos que esta app
	// no consume siguen cayendo en null sin tocar lo que ya funciona.
	public static class RutaDeCompras
	{
		public static IMensajeDeEvento Rutear(SenalDeEvento senal) =>
			(IMensajeDeEvento)CompraRegistrada.Interpretar(senal)
			?? PromocionPublicada.Interpretar(senal);
	}
}
