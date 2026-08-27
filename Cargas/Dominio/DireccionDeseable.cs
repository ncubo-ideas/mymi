using System;

namespace Cargas.Dominio
{
	/// <summary>
	/// Hacia donde conviene que se mueva un numero EN LA UNIDAD EN QUE SE MIRA. En km/L
	/// mas es mejor; en L/100km menos es mejor, y son la misma realidad fisica al reves.
	/// Por eso la direccion es una propiedad de la PresentacionDeRendimiento y no del
	/// veredicto: el veredicto ya es semantico (mejoro/empeoro) y no se invierte nunca.
	/// </summary>
	public sealed class DireccionDeseable
	{
		public static readonly DireccionDeseable MasEsMejor = new DireccionDeseable("mas es mejor", 1);
		public static readonly DireccionDeseable MenosEsMejor = new DireccionDeseable("menos es mejor", -1);

		public string Nombre { get; private set; }

		private readonly int favorable;

		private DireccionDeseable(string nombre, int favorable)
		{
			Nombre = nombre;
			this.favorable = favorable;
		}

		// Si un cambio en la unidad presentada es favorable. Lo usa la superficie para
		// pintar la flecha; el juicio del dominio ya viene hecho en canonico.
		public bool EsFavorable(decimal cambioEnLaUnidadPresentada) =>
			cambioEnLaUnidadPresentada * favorable > 0m;
	}
}
