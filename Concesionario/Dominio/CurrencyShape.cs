using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// SI ESTE MODULO NOMBRA la moneda de un monto, por su codigo. Son dos marcos distintos y no
	/// dos adornos del mismo: la nombrada se dice con su signo delante, y la que no, con el codigo
	/// tal como llego detras, porque de esa no se sabe ni el signo ni cuantos decimales tiene.
	/// <para>
	/// Donde va el signo respecto al numero es del idioma, asi que aqui viaja el codigo y nunca la
	/// juntura.
	/// </para>
	/// </summary>
	internal static class CurrencyShape
	{
		/// <summary>Una de las monedas que este modulo nombra, con su signo y su
		/// precision.</summary>
		internal const string Named = "Named";

		/// <summary>Una que llego con un monto y que este conjunto no nombra.</summary>
		internal const string NotNamed = "NotNamed";
	}
}
