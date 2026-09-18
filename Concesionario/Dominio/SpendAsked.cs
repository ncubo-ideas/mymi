using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// COMO SE PREGUNTO por el gasto, por su codigo: por un periodo, por ese mismo periodo moneda
	/// por moneda, o por dos periodos juntos. Mueve el MARCO y nunca las cifras -- las dos vistas
	/// van adentro de los tres --, asi que aqui viaja el codigo y la redaccion la pone el idioma.
	/// </summary>
	internal static class SpendAsked
	{
		/// <summary>Un periodo, con sus dos vistas adentro.</summary>
		internal const string AsOnePeriod = "AsOnePeriod";

		/// <summary>El mismo periodo, diciendo ademas en cuantas monedas esta dicho.</summary>
		internal const string AsOnePeriodPerCurrency = "AsOnePeriodPerCurrency";

		/// <summary>Dos periodos, cada uno con sus limites y sus dos vistas, y ninguna
		/// diferencia.</summary>
		internal const string AsTwoPeriodsCompared = "AsTwoPeriodsCompared";
	}
}
