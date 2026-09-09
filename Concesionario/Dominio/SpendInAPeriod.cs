using System;
using System.Collections.Generic;
using System.Globalization;

namespace Concesionario.Dominio
{
	/// <summary>
	/// La respuesta de un periodo del libro: el gasto dicho con sus dos vistas, moneda por moneda, o
	/// la constancia con nombre de que no hay ninguno. Es un carrier interno -- se arma en cada
	/// lectura y no cruza hacia ninguna caratula.
	/// <para>
	/// Contesta TOTALMENTE las dos preguntas con las que se distinguen sus tres situaciones, asi que
	/// ningun llamador pregunta que tipo tiene en la mano.
	/// </para>
	/// </summary>
	internal abstract class SpendInAPeriod
	{
		private readonly DateTime _from;
		private readonly DateTime _to;
		private readonly List<ACostEntry> _touching;

		/// <summary>
		/// El periodo y las entradas que lo tocan, que es lo que las tres situaciones comparten.
		/// </summary>
		internal SpendInAPeriod(DateTime from, DateTime to, List<ACostEntry> touching)
		{
			_from = from;
			_to = to;
			_touching = new List<ACostEntry>(touching);
		}

		/// <summary>
		/// Arma la respuesta con las entradas que tocan el periodo -- las que se incurrieron adentro y
		/// las que consumen adentro. Sin ninguna, la respuesta es la ausencia con nombre. Con mas de
		/// una moneda, el estado con nombre en que no hay total entre ellas.
		/// </summary>
		internal static SpendInAPeriod Of(DateTime from, DateTime to, List<ACostEntry> touching)
		{
			if (touching == null)
				throw new DomainException(
					"Un periodo se arma con las entradas que lo tocan, y aca no llego ninguna lista: "
					+ "ninguna entrada y una lista ausente no son lo mismo.");

			if (touching.Count == 0) return new NoCostOnRecord(from, to);

			List<List<ACostEntry>> byCurrency = ByCurrency(touching);

			if (byCurrency.Count == 1)
				return new ASpendSaidInOneCurrency(
					from, to, touching, ASpendSummary.Of(from, to, byCurrency[0]));

			List<ASpendSummary> eachOnItsOwn = new List<ASpendSummary>();

			foreach (List<ACostEntry> ofOneCurrency in byCurrency)
				eachOnItsOwn.Add(ASpendSummary.Of(from, to, ofOneCurrency));

			return new ASpendNotSaidAcrossCurrencies(from, to, touching, eachOnItsOwn);
		}

		/// <summary>Si el periodo tiene algun gasto, salido o consumido. TOTAL.</summary>
		internal abstract bool HasAnyCost();

		/// <summary>
		/// Si todo el gasto del periodo esta dicho en una sola moneda. TOTAL: un periodo sin gasto no
		/// esta dicho en ninguna, y una moneda que este modulo no nombra no se sabe la misma que otra.
		/// </summary>
		internal abstract bool IsSaidInOneCurrency();

		/// <summary>
		/// El periodo en una frase, con las dos vistas adentro. TOTAL: la ausencia tambien se dice con
		/// palabras y nombra los dos instantes por los que se pregunto.
		/// </summary>
		internal abstract string Describe();

		/// <summary>
		/// El periodo dicho moneda por moneda. TOTAL, y con mas de una dice que no hay total entre
		/// ellas en vez de sumar dos escalas que nadie puede comparar.
		/// </summary>
		internal abstract string DescribePerCurrency();

		/// <summary>
		/// Las entradas que se INCURRIERON adentro del periodo, en orden del instante en que pasaron.
		/// TOTAL: vacia cuando ninguna. Son las filas del periodo, y su dinero es el que salio del
		/// banco aca.
		/// </summary>
		internal List<ACostEntry> TheEntriesIncurredInside()
		{
			List<ACostEntry> inside = new List<ACostEntry>();

			foreach (ACostEntry entry in _touching)
				if (entry.WasIncurredBetween(_from, _to)) inside.Add(entry);

			return inside;
		}

		/// <summary>La clausula que las tres situaciones comparten: entre cuando y cuando.</summary>
		protected string ThePeriodClause()
		{
			return "Entre el " + TheInstant(_from) + " y el " + TheInstant(_to);
		}

		/// <summary>Un instante dicho como instante, con su hora.</summary>
		protected static string TheInstant(DateTime instant)
		{
			return instant.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
		}

		private static List<List<ACostEntry>> ByCurrency(List<ACostEntry> touching)
		{
			List<List<ACostEntry>> groups = new List<List<ACostEntry>>();

			foreach (ACostEntry entry in touching)
			{
				bool placed = false;

				foreach (List<ACostEntry> group in groups)
				{
					if (!group[0].IsInTheSameCurrencyAs(entry)) continue;

					group.Add(entry);
					placed = true;
					break;
				}

				if (!placed) groups.Add(new List<ACostEntry> { entry });
			}

			return groups;
		}
	}
}
