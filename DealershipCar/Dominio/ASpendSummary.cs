using System;
using System.Collections.Generic;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// El gasto de un periodo en UNA moneda, dicho con sus dos vistas juntas: lo que salio del banco
	/// adentro del periodo y lo que el periodo consumio. Se compone en cada lectura.
	/// <para>
	/// Las dos vistas van siempre juntas y no hay ningun miembro que conteste una sola: cada una sola
	/// es el numero que engana -- la caja dice "el carro se puso caro" cuando la verdad es "compro
	/// llantas", y lo consumido calla que el dinero ya salio.
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
		/// El resumen de esas entradas, que estan todas en la misma moneda. Sin ninguna entrada no hay
		/// resumen: un par de ceros se leeria como un periodo que gasto cero.
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
		/// Las dos vistas en una frase, con las clausulas que hacen visible como se repartio lo
		/// duradero. La vista que no tiene cifra se dice con palabras y nunca con un cero.
		/// </summary>
		internal string Describe()
		{
			return TheTwoViews() + TheRulesBehindThem();
		}

		private string TheTwoViews()
		{
			bool anythingLeftTheBank = false;
			bool anythingWasConsumed = false;

			Money cashOut = null;
			Money consumed = null;

			foreach (ACostEntry entry in _entries)
			{
				if (entry.WasIncurredBetween(_from, _to))
				{
					cashOut = anythingLeftTheBank
						? cashOut.Plus(entry.TheMoneyItCost())
						: entry.TheMoneyItCost();
					anythingLeftTheBank = true;
				}

				if (entry.ConsumesAnythingBetween(_from, _to))
				{
					Money its = entry.TheMoneyItConsumedBetween(_from, _to);

					consumed = anythingWasConsumed ? consumed.Plus(its) : its;
					anythingWasConsumed = true;
				}
			}

			string theCashOut = anythingLeftTheBank
				? "Salieron " + cashOut.Expressed() + " del banco"
				: "No salió nada del banco en este período";

			string theConsumed = anythingWasConsumed
				? "el período consumió " + consumed.Expressed()
				: "el período no consumió nada de eso todavía";

			return theCashOut + " y " + theConsumed + ".";
		}

		private string TheRulesBehindThem()
		{
			string said = "";

			foreach (ACostEntry entry in _entries)
			{
				string clause = entry.TheClauseOfItsConsumptionBetween(_from, _to);

				if (clause.Length == 0) continue;

				said = said + " De eso: " + clause;
			}

			return said;
		}
	}
}
