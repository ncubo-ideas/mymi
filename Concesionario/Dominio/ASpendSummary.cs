using System;
using System.Collections.Generic;

namespace Concesionario.Dominio
{
	/// <summary>
	/// El gasto de un periodo en UNA moneda, dicho con sus dos vistas juntas: lo que salio del
	/// banco adentro del periodo y lo que el periodo consumio. Se compone en cada lectura.
	/// <para>
	/// Las dos vistas van siempre juntas y no hay ningun miembro que conteste una sola: cada una
	/// sola es el numero que engana -- la caja dice "el carro se puso caro" cuando la verdad es
	/// "compro llantas", y lo consumido calla que el dinero ya salio.
	/// </para>
	/// <para>
	/// Contesta UNA cosa, y esa cosa es la clausula entera. Que la clausula entregue despues cada
	/// mitad por separado no abre el atajo: la clausula no cruza ninguna puerta, y lo que si cruza
	/// es la respuesta ya redactada, que dice las dos.
	/// </para>
	/// </summary>
	internal sealed class ASpendSummary
	{
		private readonly DateTime _from;
		private readonly DateTime _to;
		private readonly List<ACostEntry> _entries;

		private ASpendSummary(DateTime from, DateTime to, List<ACostEntry> entries)
		{
			_from = from;
			_to = to;
			_entries = new List<ACostEntry>(entries);
		}

		/// <summary>
		/// El resumen de esas entradas, que estan todas en la misma moneda. Sin ninguna entrada no
		/// hay resumen: un par de ceros se leeria como un periodo que gasto cero.
		/// </summary>
		internal static ASpendSummary Of(DateTime from, DateTime to, List<ACostEntry> entries)
		{
			if (entries == null)
				throw new DomainException(
					"Un resumen se arma con las entradas de una moneda, y aca no llego ninguna "
					+ "lista: ninguna entrada y una lista ausente no son lo mismo.");

			if (entries.Count == 0)
				throw new DomainException(
					"Un resumen de ninguna entrada seria un par de ceros, y un cero de dinero se lee "
					+ "como un gasto que hubo. Un periodo sin entradas se dice con su propia "
					+ "constancia.");

			return new ASpendSummary(from, to, entries);
		}

		/// <summary>
		/// LAS DOS VISTAS Y LAS CLAUSULAS DE LO DURADERO en una sola clausula, como forma y
		/// valores. La vista que no tiene cifra se dice por su codigo y nunca con un cero.
		/// </summary>
		internal ASummaryClause TheClauseItComposes()
		{
			bool anythingLeftTheBank = false;
			bool anythingWasConsumed = false;

			Money cashOut = null;
			Money consumed = null;

			List<AnAmortizedShare> shares = new List<AnAmortizedShare>();

			foreach (ACostEntry entry in _entries)
			{
				if (entry.WasIncurredBetween(_from, _to))
				{
					cashOut = anythingLeftTheBank
						? cashOut.Plus(entry.TheMoneyItCost())
						: entry.TheMoneyItCost();
					anythingLeftTheBank = true;
				}

				if (!entry.ConsumesAnythingBetween(_from, _to)) continue;

				Money its = entry.TheMoneyItConsumedBetween(_from, _to);

				consumed = anythingWasConsumed ? consumed.Plus(its) : its;
				anythingWasConsumed = true;

				if (entry.SpreadsItselfOverTime()) shares.Add(entry.TheShareItGivesBetween(_from, _to));
			}

			if (!anythingWasConsumed) return ASummaryClause.OfTheCashOutAlone(cashOut, shares);

			if (!anythingLeftTheBank) return ASummaryClause.OfTheConsumedAlone(consumed, shares);

			return ASummaryClause.OfBothViews(cashOut, consumed, shares);
		}
	}
}
