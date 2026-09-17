using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// QUE TIENE UN PERIODO DEL LIBRO que decir, por su codigo: ningun gasto, el gasto entero dicho
	/// en una sola moneda, o el gasto en mas de una y sin total entre ellas.
	/// <para>
	/// El de mas de una moneda es un estado CON NOMBRE y no una ausencia: el gasto esta dicho
	/// entero, y lo unico que no existe es un total que cruce monedas.
	/// </para>
	/// </summary>
	internal static class SpendShape
	{
		/// <summary>Ninguno se incurrio adentro y ninguno consume adentro.</summary>
		internal const string NoCostOnRecord = "NoCostOnRecord";

		/// <summary>Todo el gasto del periodo esta dicho en una sola moneda.</summary>
		internal const string SaidInOneCurrency = "SaidInOneCurrency";

		/// <summary>Hay mas de una moneda, asi que cada una se dice por su lado.</summary>
		internal const string NotSaidAcrossCurrencies = "NotSaidAcrossCurrencies";
	}
}
