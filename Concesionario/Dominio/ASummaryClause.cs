using System;
using System.Collections.Generic;

namespace Concesionario.Dominio
{
	/// <summary>
	/// LO QUE EL GASTO DE UN PERIODO EN UNA MONEDA TIENE QUE DECIR: sus DOS vistas -- lo que salio
	/// del banco adentro y lo que el periodo consumio -- y las clausulas que hacen visible como se
	/// repartio lo duradero.
	/// <para>
	/// Sale como FORMA mas valores ya tipados y nunca como una frase. Las dos vistas van siempre
	/// juntas: cual de ellas tiene cifra lo dice UN codigo y no dos banderas, y la que no la tiene
	/// se dice con palabras y nunca con un cero, porque un cero de dinero se lee como un gasto que
	/// hubo.
	/// </para>
	/// <para>
	/// Hay una fabrica por situacion y ninguna recibe una ausencia: cual vista falta es el nombre
	/// de la fabrica, no un hueco que alguien tenga que mirar.
	/// </para>
	/// </summary>
	internal sealed class ASummaryClause
	{
		private const string NoCurrencyToSay = "";

		private readonly string _views;
		private readonly string _cashOutShape;
		private readonly string _cashOutAmount;
		private readonly string _cashOutCode;
		private readonly string _consumedShape;
		private readonly string _consumedAmount;
		private readonly string _consumedCode;
		private readonly string[] _wholeShapes;
		private readonly string[] _wholeAmounts;
		private readonly string[] _wholeCodes;
		private readonly string[] _subjects;
		private readonly int[] _lives;
		private readonly string[] _incurredAt;
		private readonly string[] _shareShapes;
		private readonly string[] _shareAmounts;
		private readonly string[] _shareCodes;

		private ASummaryClause(
			string views, string cashOutShape, string cashOutAmount, string cashOutCode,
			string consumedShape, string consumedAmount, string consumedCode,
			List<AnAmortizedShare> shares)
		{
			_views = views;
			_cashOutShape = cashOutShape;
			_cashOutAmount = cashOutAmount;
			_cashOutCode = cashOutCode;
			_consumedShape = consumedShape;
			_consumedAmount = consumedAmount;
			_consumedCode = consumedCode;

			_wholeShapes = new string[shares.Count];
			_wholeAmounts = new string[shares.Count];
			_wholeCodes = new string[shares.Count];
			_subjects = new string[shares.Count];
			_lives = new int[shares.Count];
			_incurredAt = new string[shares.Count];
			_shareShapes = new string[shares.Count];
			_shareAmounts = new string[shares.Count];
			_shareCodes = new string[shares.Count];

			for (int each = 0; each < shares.Count; each++)
			{
				AnAmortizedShare share = shares[each];

				_wholeShapes[each] = share.TheShapeOfTheWholeCurrency();
				_wholeAmounts[each] = share.TheWholeAsSaid();
				_wholeCodes[each] = share.TheCodeOfTheWholeCurrency();
				_subjects[each] = share.TheSubjectItWasSpentOn();
				_lives[each] = share.TheMonthsItIsSpreadOver();
				_incurredAt[each] = share.TheInstantItWasIncurred();
				_shareShapes[each] = share.TheShapeOfTheShareCurrency();
				_shareAmounts[each] = share.TheShareAsSaid();
				_shareCodes[each] = share.TheCodeOfTheShareCurrency();
			}
		}

		/// <summary>
		/// La clausula de una moneda cuyo periodo tiene las DOS vistas con cifra.
		/// </summary>
		internal static ASummaryClause OfBothViews(
			Money cashOut, Money consumed, List<AnAmortizedShare> shares)
		{
			Money bank = TheMoneyThatSays(cashOut, "la caja");
			Money used = TheMoneyThatSays(consumed, "lo consumido");

			return new ASummaryClause(
				SummaryViews.BothViews,
				bank.TheShapeOfItsCurrency(), bank.TheAmountAsSaid(), bank.TheCodeOfItsCurrency(),
				used.TheShapeOfItsCurrency(), used.TheAmountAsSaid(), used.TheCodeOfItsCurrency(),
				TheShares(shares));
		}

		/// <summary>
		/// La clausula de una moneda cuyo periodo pago y todavia no consumio nada de eso. Lo
		/// consumido se dice con palabras, que es lo que impide que un cero se lea como un gasto
		/// que hubo.
		/// </summary>
		internal static ASummaryClause OfTheCashOutAlone(Money cashOut, List<AnAmortizedShare> shares)
		{
			Money bank = TheMoneyThatSays(cashOut, "la caja");

			return new ASummaryClause(
				SummaryViews.TheCashOutAlone,
				bank.TheShapeOfItsCurrency(), bank.TheAmountAsSaid(), bank.TheCodeOfItsCurrency(),
				NoCurrencyToSay, NoCurrencyToSay, NoCurrencyToSay,
				TheShares(shares));
		}

		/// <summary>
		/// La clausula de una moneda cuyo periodo consumio algo pagado antes y no pago nada
		/// adentro. La caja se dice con palabras, por la misma razon.
		/// </summary>
		internal static ASummaryClause OfTheConsumedAlone(
			Money consumed, List<AnAmortizedShare> shares)
		{
			Money used = TheMoneyThatSays(consumed, "lo consumido");

			return new ASummaryClause(
				SummaryViews.TheConsumedAlone,
				NoCurrencyToSay, NoCurrencyToSay, NoCurrencyToSay,
				used.TheShapeOfItsCurrency(), used.TheAmountAsSaid(), used.TheCodeOfItsCurrency(),
				TheShares(shares));
		}

		/// <summary>Cuales de las dos vistas tienen cifra, por su codigo. TOTAL.</summary>
		internal string TheViewsItAnswers()
		{
			return _views;
		}

		/// <summary>La forma de la moneda de la caja, o vacia donde no salio nada. TOTAL.</summary>
		internal string TheShapeOfTheCashOutCurrency()
		{
			return _cashOutShape;
		}

		/// <summary>Los digitos de la caja, o vacios por lo mismo. TOTAL.</summary>
		internal string TheCashOutAsSaid()
		{
			return _cashOutAmount;
		}

		/// <summary>El codigo de la moneda de la caja, o vacio por lo mismo. TOTAL.</summary>
		internal string TheCodeOfTheCashOutCurrency()
		{
			return _cashOutCode;
		}

		/// <summary>La forma de la moneda de lo consumido, o vacia si no se consumio nada.
		/// TOTAL.</summary>
		internal string TheShapeOfTheConsumedCurrency()
		{
			return _consumedShape;
		}

		/// <summary>Los digitos de lo consumido, o vacios por lo mismo. TOTAL.</summary>
		internal string TheConsumedAsSaid()
		{
			return _consumedAmount;
		}

		/// <summary>El codigo de la moneda de lo consumido, o vacio por lo mismo. TOTAL.</summary>
		internal string TheCodeOfTheConsumedCurrency()
		{
			return _consumedCode;
		}

		/// <summary>Cuantas partes amortizadas tiene que decir esta moneda. TOTAL.</summary>
		internal int TheShareCount()
		{
			return _subjects.Length;
		}

		/// <summary>La forma de la moneda del gasto entero de cada parte, una por fila.
		/// TOTAL.</summary>
		internal string[] TheShapesOfTheWholeCurrencies()
		{
			return _wholeShapes;
		}

		/// <summary>Los digitos del gasto entero de cada parte, en el mismo orden. TOTAL.</summary>
		internal string[] TheWholesAsSaid()
		{
			return _wholeAmounts;
		}

		/// <summary>El codigo de la moneda de cada gasto entero, en el mismo orden.
		/// TOTAL.</summary>
		internal string[] TheCodesOfTheWholeCurrencies()
		{
			return _wholeCodes;
		}

		/// <summary>En que se fue cada gasto que se reparte, con las palabras que llegaron.
		/// TOTAL.</summary>
		internal string[] TheSubjectsOfTheShares()
		{
			return _subjects;
		}

		/// <summary>A lo largo de cuantos meses se reparte cada uno, en el mismo orden.
		/// TOTAL.</summary>
		internal int[] TheMonthsEachIsSpreadOver()
		{
			return _lives;
		}

		/// <summary>El instante en que se incurrio cada uno, en el mismo orden. TOTAL.</summary>
		internal string[] TheInstantsEachWasIncurred()
		{
			return _incurredAt;
		}

		/// <summary>La forma de la moneda de cada parte, en el mismo orden. TOTAL.</summary>
		internal string[] TheShapesOfTheShareCurrencies()
		{
			return _shareShapes;
		}

		/// <summary>Los digitos de la parte que a este periodo le toca, en el mismo orden.
		/// TOTAL.</summary>
		internal string[] TheSharesAsSaid()
		{
			return _shareAmounts;
		}

		/// <summary>El codigo de la moneda de cada parte, en el mismo orden. TOTAL.</summary>
		internal string[] TheCodesOfTheShareCurrencies()
		{
			return _shareCodes;
		}

		private static Money TheMoneyThatSays(Money money, string whichView)
		{
			if (money == null)
				throw new DomainException(
					"Se compuso " + whichView + " de un periodo sin el dinero que la dice. Esta "
					+ "situacion es la que SI tiene cifra: la que no la tiene se compone con la fabrica "
					+ "que lleva su nombre.");

			return money;
		}

		private static List<AnAmortizedShare> TheShares(List<AnAmortizedShare> shares)
		{
			if (shares == null)
				throw new DomainException(
					"Se compuso el gasto de una moneda sin la lista de sus partes amortizadas, y ninguna "
					+ "parte y una lista ausente no son lo mismo.");

			return shares;
		}
	}
}
