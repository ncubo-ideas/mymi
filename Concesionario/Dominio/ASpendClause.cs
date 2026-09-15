using System;
using System.Collections.Generic;

namespace Concesionario.Dominio
{
	/// <summary>
	/// LO QUE EL GASTO TIENE QUE DECIR, sea de un periodo o de dos comparados: como se pregunto,
	/// que tiene cada periodo que decir, y cada moneda con sus DOS vistas y las partes que reparte.
	/// <para>
	/// Sale como FORMA mas valores ya tipados y nunca como una frase. Las tres lecturas pasan por
	/// aqui porque la diferencia entre ellas es el MARCO y nunca las cifras: las dos vistas van
	/// adentro de las tres, y un marco es redaccion.
	/// </para>
	/// <para>
	/// Las monedas y las partes viajan APLANADAS con el indice de a quien pertenecen, porque lo que
	/// un periodo tiene adentro es ragged -- una moneda puede repartir tres gastos y otra ninguno
	/// -- y por el wire no cruza nada ragged.
	/// </para>
	/// </summary>
	internal sealed class ASpendClause
	{
		private readonly string _asked;
		private readonly int[] _currencyCounts;
		private readonly string[] _periodShapes;
		private readonly string[] _periodFroms;
		private readonly string[] _periodTos;
		private readonly List<int> _summaryOf = new List<int>();
		private readonly List<string> _views = new List<string>();
		private readonly List<string> _cashOutShapes = new List<string>();
		private readonly List<string> _cashOutAmounts = new List<string>();
		private readonly List<string> _cashOutCodes = new List<string>();
		private readonly List<string> _consumedShapes = new List<string>();
		private readonly List<string> _consumedAmounts = new List<string>();
		private readonly List<string> _consumedCodes = new List<string>();
		private readonly List<int> _shareOf = new List<int>();
		private readonly List<string> _wholeShapes = new List<string>();
		private readonly List<string> _wholeAmounts = new List<string>();
		private readonly List<string> _wholeCodes = new List<string>();
		private readonly List<string> _subjects = new List<string>();
		private readonly List<int> _lives = new List<int>();
		private readonly List<string> _instants = new List<string>();
		private readonly List<string> _shareShapes = new List<string>();
		private readonly List<string> _shareAmounts = new List<string>();
		private readonly List<string> _shareCodes = new List<string>();

		private ASpendClause(string asked, List<SpendInAPeriod> periods)
		{
			_asked = asked;
			_currencyCounts = new int[periods.Count];
			_periodShapes = new string[periods.Count];
			_periodFroms = new string[periods.Count];
			_periodTos = new string[periods.Count];

			for (int period = 0; period < periods.Count; period++)
			{
				_periodShapes[period] = periods[period].TheShapeItHas();
				_periodFroms[period] = periods[period].TheInstantItStartsAt();
				_periodTos[period] = periods[period].TheInstantItEndsAt();

				List<ASummaryClause> currencies = periods[period].TheClausesOfItsCurrencies();

				_currencyCounts[period] = currencies.Count;

				foreach (ASummaryClause currency in currencies) Place(period, currency);
			}
		}

		/// <summary>
		/// La clausula de UN periodo, dicho con sus dos vistas y sin nombrar cuantas monedas hay.
		/// </summary>
		internal static ASpendClause OfOnePeriod(SpendInAPeriod period)
		{
			return new ASpendClause(SpendAsked.AsOnePeriod, TheOne(period));
		}

		/// <summary>
		/// La clausula del MISMO periodo diciendo ademas en cuantas monedas esta dicho. Es otro
		/// marco y no otras cifras: el dinero no se convierte, y agrupar por moneda es de quien
		/// consulta.
		/// </summary>
		internal static ASpendClause OfOnePeriodPerCurrency(SpendInAPeriod period)
		{
			return new ASpendClause(SpendAsked.AsOnePeriodPerCurrency, TheOne(period));
		}

		/// <summary>
		/// La clausula de DOS periodos, en ese orden, cada uno con sus limites y sus dos vistas.
		/// Nunca una diferencia suelta ni un porcentaje: entre dos monedas distintas esa resta no
		/// seria un numero, y decirla donde si lo es dejaria dos formas para una misma respuesta.
		/// </summary>
		internal static ASpendClause OfTwoPeriodsCompared(
			SpendInAPeriod first, SpendInAPeriod second)
		{
			return new ASpendClause(
				SpendAsked.AsTwoPeriodsCompared,
				new List<SpendInAPeriod> { ThePeriod(first), ThePeriod(second) });
		}

		/// <summary>Como se pregunto por el gasto, por su codigo. TOTAL.</summary>
		internal string HowItWasAsked()
		{
			return _asked;
		}

		/// <summary>Que tiene que decir cada periodo, por su codigo, uno por fila. TOTAL.</summary>
		internal string[] TheShapesOfItsPeriods()
		{
			return _periodShapes;
		}

		/// <summary>Donde empieza cada periodo, como instante con su hora. TOTAL.</summary>
		internal string[] TheInstantsEachPeriodStartsAt()
		{
			return _periodFroms;
		}

		/// <summary>Donde termina cada periodo, en el mismo orden. TOTAL.</summary>
		internal string[] TheInstantsEachPeriodEndsAt()
		{
			return _periodTos;
		}

		/// <summary>
		/// En cuantas monedas esta dicho el gasto de cada periodo, en el mismo orden. TOTAL, y lo
		/// cuenta ESTE lado: contar las monedas de un periodo del otro lado seria una segunda
		/// version del mismo hecho, y la frase que dice "en N monedas distintas" es la que lo lee.
		/// </summary>
		internal int[] TheCurrencyCountOfEachPeriod()
		{
			return _currencyCounts;
		}

		/// <summary>De que periodo es cada moneda, por su indice. TOTAL.</summary>
		internal int[] ThePeriodOfEachCurrency()
		{
			return _summaryOf.ToArray();
		}

		/// <summary>Cuales de las dos vistas tiene cada moneda con cifra, por su codigo.
		/// TOTAL.</summary>
		internal string[] TheViewsOfEachCurrency()
		{
			return _views.ToArray();
		}

		/// <summary>La forma de la moneda de la caja de cada una. TOTAL.</summary>
		internal string[] TheShapesOfEachCashOut()
		{
			return _cashOutShapes.ToArray();
		}

		/// <summary>Los digitos de la caja de cada una. TOTAL.</summary>
		internal string[] TheCashOutsAsSaid()
		{
			return _cashOutAmounts.ToArray();
		}

		/// <summary>El codigo de la moneda de la caja de cada una. TOTAL.</summary>
		internal string[] TheCodesOfEachCashOut()
		{
			return _cashOutCodes.ToArray();
		}

		/// <summary>La forma de la moneda de lo consumido de cada una. TOTAL.</summary>
		internal string[] TheShapesOfEachConsumed()
		{
			return _consumedShapes.ToArray();
		}

		/// <summary>Los digitos de lo consumido de cada una. TOTAL.</summary>
		internal string[] TheConsumedAsSaid()
		{
			return _consumedAmounts.ToArray();
		}

		/// <summary>El codigo de la moneda de lo consumido de cada una. TOTAL.</summary>
		internal string[] TheCodesOfEachConsumed()
		{
			return _consumedCodes.ToArray();
		}

		/// <summary>De que moneda es cada parte amortizada, por su indice. TOTAL.</summary>
		internal int[] TheCurrencyOfEachShare()
		{
			return _shareOf.ToArray();
		}

		/// <summary>La forma de la moneda del gasto entero de cada parte. TOTAL.</summary>
		internal string[] TheShapesOfEachWhole()
		{
			return _wholeShapes.ToArray();
		}

		/// <summary>Los digitos del gasto entero de cada parte. TOTAL.</summary>
		internal string[] TheWholesAsSaid()
		{
			return _wholeAmounts.ToArray();
		}

		/// <summary>El codigo de la moneda del gasto entero de cada parte. TOTAL.</summary>
		internal string[] TheCodesOfEachWhole()
		{
			return _wholeCodes.ToArray();
		}

		/// <summary>En que se fue cada gasto que se reparte. TOTAL.</summary>
		internal string[] TheSubjectsOfEachShare()
		{
			return _subjects.ToArray();
		}

		/// <summary>A lo largo de cuantos meses se reparte cada uno. TOTAL.</summary>
		internal int[] TheMonthsEachShareIsSpreadOver()
		{
			return _lives.ToArray();
		}

		/// <summary>El instante en que se incurrio cada uno. TOTAL.</summary>
		internal string[] TheInstantsEachShareWasIncurred()
		{
			return _instants.ToArray();
		}

		/// <summary>La forma de la moneda de cada parte. TOTAL.</summary>
		internal string[] TheShapesOfEachShare()
		{
			return _shareShapes.ToArray();
		}

		/// <summary>Los digitos de la parte que a ese periodo le toca. TOTAL.</summary>
		internal string[] TheSharesAsSaid()
		{
			return _shareAmounts.ToArray();
		}

		/// <summary>El codigo de la moneda de cada parte. TOTAL.</summary>
		internal string[] TheCodesOfEachShare()
		{
			return _shareCodes.ToArray();
		}

		private void Place(int period, ASummaryClause currency)
		{
			int itsPlace = _views.Count;

			_summaryOf.Add(period);
			_views.Add(currency.TheViewsItAnswers());
			_cashOutShapes.Add(currency.TheShapeOfTheCashOutCurrency());
			_cashOutAmounts.Add(currency.TheCashOutAsSaid());
			_cashOutCodes.Add(currency.TheCodeOfTheCashOutCurrency());
			_consumedShapes.Add(currency.TheShapeOfTheConsumedCurrency());
			_consumedAmounts.Add(currency.TheConsumedAsSaid());
			_consumedCodes.Add(currency.TheCodeOfTheConsumedCurrency());

			string[] wholeShapes = currency.TheShapesOfTheWholeCurrencies();
			string[] wholeAmounts = currency.TheWholesAsSaid();
			string[] wholeCodes = currency.TheCodesOfTheWholeCurrencies();
			string[] subjects = currency.TheSubjectsOfTheShares();
			int[] lives = currency.TheMonthsEachIsSpreadOver();
			string[] instants = currency.TheInstantsEachWasIncurred();
			string[] shareShapes = currency.TheShapesOfTheShareCurrencies();
			string[] shareAmounts = currency.TheSharesAsSaid();
			string[] shareCodes = currency.TheCodesOfTheShareCurrencies();

			for (int share = 0; share < currency.TheShareCount(); share++)
			{
				_shareOf.Add(itsPlace);
				_wholeShapes.Add(wholeShapes[share]);
				_wholeAmounts.Add(wholeAmounts[share]);
				_wholeCodes.Add(wholeCodes[share]);
				_subjects.Add(subjects[share]);
				_lives.Add(lives[share]);
				_instants.Add(instants[share]);
				_shareShapes.Add(shareShapes[share]);
				_shareAmounts.Add(shareAmounts[share]);
				_shareCodes.Add(shareCodes[share]);
			}
		}

		private static List<SpendInAPeriod> TheOne(SpendInAPeriod period)
		{
			return new List<SpendInAPeriod> { ThePeriod(period) };
		}

		private static SpendInAPeriod ThePeriod(SpendInAPeriod period)
		{
			if (period == null)
				throw new DomainException(
					"Se compuso el gasto sin ningun periodo del que hablar. Un periodo ausente no es un "
					+ "periodo sin gasto.");

			return period;
		}
	}
}
