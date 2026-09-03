using System;
using System.Collections.Generic;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// La respuesta de un periodo: las estadias que se cruzan con el y los dias del periodo en que
	/// el vehiculo estuvo adentro, o la constancia con nombre de que no hay ninguna. Es un carrier
	/// interno -- se arma en cada lectura y no cruza hacia ninguna caratula.
	/// </summary>
	internal abstract class StaysInAPeriod
	{
		/// <summary>
		/// Arma la respuesta del periodo con las estadias que se cruzan con el. Sin ninguna, la
		/// respuesta es la ausencia con nombre y no un periodo vacio.
		/// </summary>
		internal static StaysInAPeriod Of(StayDay from, StayDay to, List<AStay> overlapping)
		{
			if (from == null || to == null)
				throw new DomainException(
					"Un periodo sin sus dos dias no dice entre cuando y cuando se pregunto.");

			if (overlapping == null)
				throw new DomainException(
					"Un periodo se arma con las estadias que se cruzan con el, y aca no llego "
					+ "ninguna lista: ninguna estadia y una lista ausente no son lo mismo.");

			if (overlapping.Count == 0) return new NoStayOnRecord(from, to);

			return new APeriodWithStaysOnRecord(from, to, overlapping);
		}

		/// <summary>Si el periodo tiene alguna estadia. TOTAL.</summary>
		internal abstract bool HasAnyStay();

		/// <summary>
		/// Las estadias que se cruzan con el periodo, en orden de entrada. TOTAL: vacia cuando no
		/// hay ninguna.
		/// </summary>
		internal abstract List<AStay> TheStays();

		/// <summary>
		/// Los dias del periodo en que el vehiculo estuvo adentro, ascendentes y sin repetidos.
		/// TOTAL, y es la respuesta primaria: de ella sale el conteo y del conteo no salen ellos.
		/// </summary>
		internal abstract List<StayDay> TheDays();

		/// <summary>Cuantos dias estuvo adentro en el periodo. Se cuenta sobre los dias, nunca se guarda.</summary>
		internal int HowManyDays()
		{
			return TheDays().Count;
		}

		/// <summary>Cuantas estadias se cruzan con el periodo. Se cuenta sobre ellas, nunca se guarda.</summary>
		internal int HowManyStays()
		{
			return TheStays().Count;
		}

		/// <summary>
		/// El periodo en una frase. TOTAL: la ausencia tambien se dice con palabras y nombra los dos
		/// dias por los que se pregunto.
		/// </summary>
		internal abstract string Describe();
	}
}
