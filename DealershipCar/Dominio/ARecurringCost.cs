using System;

namespace DealershipCar.Dominio
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

		/// <summary>Ninguna: una recurrente se consume en su periodo y no tiene vida util.</summary>
		internal override string HowItsExpectedLifeIsSaid()
		{
			return "";
		}

		/// <summary>Si se incurrio adentro: es el mismo periodo que la consume.</summary>
		internal override bool ConsumesAnythingBetween(DateTime from, DateTime to)
		{
			return WasIncurredBetween(from, to);
		}

		/// <summary>El gasto entero, porque el periodo en que se incurrio lo consume todo.</summary>
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
		internal override string TheClauseOfItsConsumptionBetween(DateTime from, DateTime to)
		{
			return "";
		}

		/// <summary>La entrada dicha entera, con su clase y con lo que su clase significa.</summary>
		internal override string Describe()
		{
			return TheSpendClause() + ". " + Classification().HowItIsSaid
				+ ": se consumió en el período en que se incurrió.";
		}
	}
}
