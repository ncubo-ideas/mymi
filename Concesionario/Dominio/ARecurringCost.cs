using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// Un gasto que el periodo en que se incurrio consume entero: gasolina, un cambio de aceite, un
	/// lavado. No lleva vida util, y no la lleva por estructura y no por omision.
	/// </summary>
	internal sealed class ARecurringCost : ACostEntry
	{
		/// <summary>
		/// Se llega por el comando recurrente del libro, que es lo que la clasifica: no hay ninguna
		/// ranura por donde decir de que clase es.
		/// </summary>
		internal ARecurringCost(Money money, DateTime incurredAt, CostSubject subject)
			: base(money, incurredAt, subject)
		{
		}

		/// <summary>Recurrente.</summary>
		internal override CostClassification Classification()
		{
			return CostClassification.Recurring;
		}

		/// <summary>No: una recurrente se consume en su periodo y no tiene vida util.</summary>
		internal override bool SpreadsItselfOverTime()
		{
			return false;
		}

		/// <summary>
		/// Cero, y quien lo dice es la clasificacion y no este cero: una recurrente no tiene vida
		/// util.
		/// </summary>
		internal override int TheMonthsOfItsExpectedLife()
		{
			return 0;
		}

		/// <summary>Si se incurrio adentro: es el mismo periodo que la consume.</summary>
		internal override bool ConsumesAnythingBetween(DateTime from, DateTime to)
		{
			return WasIncurredBetween(from, to);
		}

		/// <summary>El gasto entero, porque el periodo en que se incurrio lo consume
		/// todo.</summary>
		internal override Money TheMoneyItConsumedBetween(DateTime from, DateTime to)
		{
			if (!ConsumesAnythingBetween(from, to))
				throw new DomainException(
					"Ese periodo no consumio nada del gasto en " + Subject.Value + ", porque no se "
					+ "incurrio adentro: hay que preguntar antes si consume algo.");

			return TheMoneyItCost();
		}

		/// <summary>
		/// Ninguna: no hay regla que hacer visible. Un gasto que se consume en su propio periodo ya
		/// esta dicho entero por las dos cifras del periodo.
		/// </summary>
		internal override AnAmortizedShare TheShareItGivesBetween(DateTime from, DateTime to)
		{
			throw new DomainException(
				"Se pidio la parte que un periodo consumio del gasto en " + Subject.Value + ", que es "
				+ "recurrente y se consume entero en el periodo en que se incurrio: hay que preguntar "
				+ "antes si se reparte. Una parte inventada aqui seria una regla que nadie aplico.");
		}
	}
}
