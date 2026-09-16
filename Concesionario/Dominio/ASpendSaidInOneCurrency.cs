using System;
using System.Collections.Generic;

namespace Concesionario.Dominio
{
	/// <summary>
	/// La respuesta de un periodo cuyo gasto entero esta dicho en una sola moneda: el resumen de
	/// esa moneda, con sus dos vistas juntas y con las clausulas que hacen visible como se repartio
	/// lo duradero.
	/// </summary>
	internal sealed class ASpendSaidInOneCurrency : SpendInAPeriod
	{
		private readonly ASpendSummary _summary;

		/// <summary>Se arma con el periodo, las entradas que lo tocan y el resumen de la unica
		/// moneda.</summary>
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

		/// <summary>Todo el gasto dicho en una sola moneda.</summary>
		internal override string TheShapeItHas()
		{
			return SpendShape.SaidInOneCurrency;
		}

		/// <summary>La unica moneda, con sus dos vistas adentro.</summary>
		internal override List<ASummaryClause> TheClausesOfItsCurrencies()
		{
			return new List<ASummaryClause> { _summary.TheClauseItComposes() };
		}
	}
}
