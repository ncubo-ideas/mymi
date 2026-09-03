using System;
using System.Globalization;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// Un dia de calendario que una estadia en el taller cubrio. Llega RECIBIDO, dicho por la
	/// sucursal en yyyy-MM-dd: no sale de ningun instante, porque un dia de calendario es un hecho
	/// local y este registro no lleva ninguna zona horaria con que sacarlo.
	/// <para>
	/// Se compara por operaciones con nombre y nunca por igualdad escrita, asi que un conjunto de
	/// dias es una lista validada y jamas una clave de valor.
	/// </para>
	/// </summary>
	internal sealed class StayDay
	{
		private readonly DateOnly _day;

		/// <summary>
		/// El dia tal como la sucursal lo dijo. Un texto en blanco o en cualquier otra forma que no
		/// sea yyyy-MM-dd es un defecto de quien llama: afuera de ese formato no hay un dia por
		/// defecto que poner en su lugar.
		/// </summary>
		internal StayDay(string text)
		{
			if (string.IsNullOrWhiteSpace(text))
				throw new DomainException(
					"Un dia de estadia en blanco no dice que dia estuvo el vehiculo adentro, y el "
					+ "dia lo dice la sucursal con el hecho.");

			DateOnly day;

			if (!DateOnly.TryParseExact(
					text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out day))
				throw new DomainException(
					$"'{text}' no dice un dia de calendario en yyyy-MM-dd. El dia llega escrito con "
					+ "el hecho, asi que una forma que este registro no lea no es un dia menos "
					+ "preciso: no es ninguno.");

			_day = day;
		}

		private StayDay(DateOnly day)
		{
			_day = day;
		}

		/// <summary>Si los dos nombran el mismo dia de calendario.</summary>
		internal bool IsTheSameDayAs(StayDay other)
		{
			return _day == other._day;
		}

		/// <summary>Si este dia es anterior al otro.</summary>
		internal bool IsBefore(StayDay other)
		{
			return _day < other._day;
		}

		/// <summary>Si este dia es posterior al otro.</summary>
		internal bool IsAfter(StayDay other)
		{
			return _day > other._day;
		}

		/// <summary>
		/// El dia siguiente en el calendario. Es con quien se compara la contiguidad de una estadia,
		/// y cruza el fin de mes y el ano bisiesto sin que nadie le diga cuantos dias tienen.
		/// </summary>
		internal StayDay Next()
		{
			return new StayDay(_day.AddDays(1));
		}

		/// <summary>Como se dice este dia: yyyy-MM-dd, igual que llego.</summary>
		internal string HowItIsSaid()
		{
			return _day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
		}
	}
}
