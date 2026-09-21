using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// COMO SE PREGUNTO POR EL RITMO DE USO, por su codigo: por un periodo, o por dos comparados.
	/// Elige el MARCO y nunca las cifras -- los dos periodos dicen su ritmo igual de entero en los
	/// dos --, y un marco es redaccion.
	/// </summary>
	internal static class RateAsked
	{
		/// <summary>Un solo periodo, con su ritmo y sin nada con que compararlo.</summary>
		internal const string AsOnePeriod = "AsOnePeriod";

		/// <summary>
		/// Dos periodos en ese orden, cada uno con su ritmo, y el cambio del primero respecto al
		/// segundo dicho adentro de la misma respuesta.
		/// </summary>
		internal const string AsTwoPeriodsCompared = "AsTwoPeriodsCompared";
	}
}
