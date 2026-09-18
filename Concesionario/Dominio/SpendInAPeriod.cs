using System;
using System.Collections.Generic;
using System.Globalization;

namespace Concesionario.Dominio
{
	/// <summary>
	/// La respuesta de un periodo del libro: el gasto dicho con sus dos vistas, moneda por moneda,
	/// o la constancia con nombre de que no hay ninguno. Es un carrier interno -- se arma en cada
	/// lectura y no cruza hacia ninguna caratula.
	/// <para>
	/// Contesta TOTALMENTE las dos preguntas con las que se distinguen sus tres situaciones, asi
	/// que ningun llamador pregunta que tipo tiene en la mano, y entrega su situacion por un CODIGO
	/// para que quien redacte elija el marco sin conocer los tipos.
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
		/// Arma la respuesta con las entradas que tocan el periodo -- las que se incurrieron
		/// adentro y las que consumen adentro. Sin ninguna, la respuesta es la ausencia con nombre.
		/// Con mas de una moneda, el estado con nombre en que no hay total entre ellas.
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
		/// Si todo el gasto del periodo esta dicho en una sola moneda. TOTAL: un periodo sin gasto
		/// no esta dicho en ninguna, y una moneda que este modulo no nombra no se sabe la misma que
		/// otra.
		/// </summary>
		internal abstract bool IsSaidInOneCurrency();

		/// <summary>
		/// Que tiene este periodo que decir, por su codigo. TOTAL, y es lo mismo que las dos
		/// preguntas de arriba dicho como nombre: el booleano lo pregunta quien decide, el codigo
		/// lo lleva quien redacta.
		/// </summary>
		internal abstract string TheShapeItHas();

		/// <summary>
		/// Cada moneda del periodo con sus dos vistas y las partes que reparte, en el orden en que
		/// cada moneda aparecio. TOTAL: vacia donde no hay ningun gasto.
		/// </summary>
		internal abstract List<ASummaryClause> TheClausesOfItsCurrencies();

		/// <summary>Donde empieza el periodo, como instante con su hora. TOTAL.</summary>
		internal string TheInstantItStartsAt()
		{
			return TheInstant(_from);
		}

		/// <summary>Donde termina el periodo, como instante con su hora. TOTAL.</summary>
		internal string TheInstantItEndsAt()
		{
			return TheInstant(_to);
		}

		/// <summary>
		/// Las entradas que se INCURRIERON adentro del periodo, en orden del instante en que
		/// pasaron. TOTAL: vacia cuando ninguna. Son las filas del periodo, y su dinero es el que
		/// salio del banco aca.
		/// </summary>
		internal List<ACostEntry> TheEntriesIncurredInside()
		{
			List<ACostEntry> inside = new List<ACostEntry>();

			foreach (ACostEntry entry in _touching)
				if (entry.WasIncurredBetween(_from, _to)) inside.Add(entry);

			return inside;
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
