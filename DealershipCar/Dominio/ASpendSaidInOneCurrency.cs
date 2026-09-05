using System;
using System.Collections.Generic;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// La respuesta de un periodo cuyo gasto entero esta dicho en una sola moneda: el resumen de esa
	/// moneda, con sus dos vistas juntas y con las clausulas que hacen visible como se repartio lo
	/// duradero.
	/// </summary>
	internal sealed class ASpendSaidInOneCurrency : SpendInAPeriod
	{
		private readonly ASpendSummary _summary;

		/// <summary>Se arma con el periodo, las entradas que lo tocan y el resumen de la unica moneda.</summary>
		internal ASpendSaidInOneCurrency(
			DateTime from, DateTime to, List<ACostEntry> touching, ASpendSummary summary)
			: base(from, to, touching)
		{
			if (summary == null)
				throw new DomainException(
					"Un periodo con gasto se dice con el resumen de su moneda, y aca no llego "
					+ "ninguno.");

			_summary = summary;
		}

		/// <summary>Si.</summary>
		internal override bool HasAnyCost()
		{
			return true;
		}

		/// <summary>Si.</summary>
		internal override bool IsSaidInOneCurrency()
		{
			return true;
		}

		/// <summary>El periodo y su resumen, con las dos vistas adentro.</summary>
		internal override string Describe()
		{
			return ThePeriodClause() + ": " + _summary.Describe();
		}

		/// <summary>Lo mismo, diciendo que todo el gasto esta en una sola moneda.</summary>
		internal override string DescribePerCurrency()
		{
			return ThePeriodClause() + " todo el gasto está dicho en una sola moneda: "
				+ _summary.Describe();
		}
	}
}
