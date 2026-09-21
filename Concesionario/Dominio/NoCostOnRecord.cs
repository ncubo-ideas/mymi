using System;
using System.Collections.Generic;

namespace Concesionario.Dominio
{
	/// <summary>
	/// La respuesta de un periodo que no tiene ningun gasto: ninguno se incurrio adentro y ninguno
	/// consume adentro. Es una ausencia CON TIPO y nombra el periodo por el que se pregunto: un
	/// cero no diria entre cuando y cuando, y un cero de dinero se lee como un gasto que hubo.
	/// </summary>
	internal sealed class NoCostOnRecord : SpendInAPeriod
	{
		/// <summary>Se arma con los dos instantes del periodo, que son lo que la frase
		/// nombra.</summary>
		internal NoCostOnRecord(DateTime from, DateTime to)
			: base(from, to, new List<ACostEntry>())
		{
		}

		/// <summary>No.</summary>
		internal override bool HasAnyCost()
		{
			return false;
		}

		/// <summary>
		/// No: no hay ninguna moneda en que este dicho. Contestar que si seria dar por respuesta el
		/// miembro benigno del conjunto de salida.
		/// </summary>
		internal override bool IsSaidInOneCurrency()
		{
			return false;
		}

		/// <summary>Ningun gasto registrado para este vehiculo en ese periodo.</summary>
		internal override string TheShapeItHas()
		{
			return SpendShape.NoCostOnRecord;
		}

		/// <summary>Ninguna: no hay ninguna moneda en la que decirlo.</summary>
		internal override List<ASummaryClause> TheClausesOfItsCurrencies()
		{
			return new List<ASummaryClause>();
		}
	}
}
