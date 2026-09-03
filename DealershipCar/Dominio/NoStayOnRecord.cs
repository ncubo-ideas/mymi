using System;
using System.Collections.Generic;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// La respuesta de un periodo que no tiene ninguna estadia con dias registrados. Es una ausencia
	/// CON TIPO y nombra el periodo por el que se pregunto: un cero no diria entre cuando y cuando.
	/// <para>
	/// Dice que no hay dias REGISTRADOS, que no es lo mismo que decir que el vehiculo no estuvo
	/// adentro: una estadia abierta todavia no tiene dias.
	/// </para>
	/// </summary>
	internal sealed class NoStayOnRecord : StaysInAPeriod
	{
		private readonly StayDay _from;
		private readonly StayDay _to;

		/// <summary>Se arma con los dos dias del periodo, que son lo que la frase nombra.</summary>
		internal NoStayOnRecord(StayDay from, StayDay to)
		{
			_from = from;
			_to = to;
		}

		/// <summary>No.</summary>
		internal override bool HasAnyStay()
		{
			return false;
		}

		/// <summary>Ninguna.</summary>
		internal override List<AStay> TheStays()
		{
			return new List<AStay>();
		}

		/// <summary>Ninguno.</summary>
		internal override List<StayDay> TheDays()
		{
			return new List<StayDay>();
		}

		/// <summary>La ausencia en palabras, con los dos dias del periodo adentro.</summary>
		internal override string Describe()
		{
			return "Entre el " + _from.HowItIsSaid() + " y el " + _to.HowItIsSaid()
				+ " no hay ninguna estadía en el taller con días registrados.";
		}
	}
}
