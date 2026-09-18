using System;
using System.Globalization;

namespace Concesionario.Dominio
{
	/// <summary>
	/// La parte de un gasto duradero que un periodo consumio: en linea recta a lo largo de su vida
	/// util, por cuanto de esa ventana cae adentro del periodo. Se compone en cada lectura y no se
	/// guarda en ninguna parte.
	/// <para>
	/// Sale como valores ya tipados y nunca como una frase, y los entrega TODOS -- el gasto entero,
	/// en que se fue, la vida util, el instante en que se incurrio y lo que a este periodo le toca
	/// -- porque la regla se dice y no solo el resultado: quien lea puede revisar la cuenta en vez
	/// de creerla.
	/// </para>
	/// </summary>
	internal sealed class AnAmortizedShare
	{
		private readonly Money _whole;
		private readonly Money _share;
		private readonly CostSubject _subject;
		private readonly AnExpectedLife _life;
		private readonly DateTime _incurredAt;

		private AnAmortizedShare(
			Money whole, Money share, CostSubject subject, AnExpectedLife life, DateTime incurredAt)
		{
			_whole = whole;
			_share = share;
			_subject = subject;
			_life = life;
			_incurredAt = incurredAt;
		}

		/// <summary>
		/// La parte que ese periodo consumio de ese gasto. Revienta si el periodo no toca la
		/// ventana: un cero ahi se leeria como que el periodo consumio nada de algo que estaba
		/// activo, y hay que preguntar antes si consume algo.
		/// </summary>
		internal static AnAmortizedShare Of(
			Money whole, CostSubject subject, AnExpectedLife life, DateTime incurredAt,
			DateTime from, DateTime to)
		{
			if (whole == null)
				throw new DomainException(
					"Se pidio la parte de un gasto sin decir de cuanto dinero era. Sin el entero no "
					+ "hay nada que repartir.");

			if (subject == null)
				throw new DomainException(
					"Se pidio la parte de un gasto sin decir en que se fue. La frase de la parte "
					+ "nombra el asunto, porque una cifra sola no dice de que gasto es.");

			if (life == null)
				throw new DomainException(
					"Se pidio la parte de un gasto duradero sin su vida util. Sin ella no hay "
					+ "ventana a lo largo de la cual repartirlo.");

			long ticksInThePeriod = life.TicksItCoversBetween(incurredAt, from, to);

			if (ticksInThePeriod <= 0)
				throw new DomainException(
					"Ese periodo no consumio nada de este gasto, asi que no tiene parte que decir: "
					+ "hay que preguntar antes si consume algo. Un cero de dinero se lee como un "
					+ "gasto que hubo.");

			return new AnAmortizedShare(
				whole, whole.Prorated(ticksInThePeriod, life.TicksOfTheWholeWindowFrom(incurredAt)),
				subject, life, incurredAt);
		}

		/// <summary>El dinero de la parte, en la misma moneda que el gasto entero.</summary>
		internal Money TheMoneyOfTheShare()
		{
			return _share;
		}

		/// <summary>La forma de la moneda del gasto entero, por su codigo. TOTAL.</summary>
		internal string TheShapeOfTheWholeCurrency()
		{
			return _whole.TheShapeOfItsCurrency();
		}

		/// <summary>Los digitos del gasto entero, en cultura invariante. TOTAL.</summary>
		internal string TheWholeAsSaid()
		{
			return _whole.TheAmountAsSaid();
		}

		/// <summary>El codigo de la moneda del gasto entero. TOTAL.</summary>
		internal string TheCodeOfTheWholeCurrency()
		{
			return _whole.TheCodeOfItsCurrency();
		}

		/// <summary>En que se fue, con las palabras que llegaron. TOTAL.</summary>
		internal string TheSubjectItWasSpentOn()
		{
			return _subject.Value;
		}

		/// <summary>A lo largo de cuantos meses se reparte, tal como llegaron. TOTAL.</summary>
		internal int TheMonthsItIsSpreadOver()
		{
			return _life.TheMonthsItLasts();
		}

		/// <summary>
		/// El instante en que se incurrio, dicho como INSTANTE con su hora: es el ancla de la
		/// ventana, y de que dia local es un instante necesitaria una zona que este libro no tiene.
		/// TOTAL.
		/// </summary>
		internal string TheInstantItWasIncurred()
		{
			return _incurredAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
		}

		/// <summary>La forma de la moneda de la parte, por su codigo. TOTAL.</summary>
		internal string TheShapeOfTheShareCurrency()
		{
			return _share.TheShapeOfItsCurrency();
		}

		/// <summary>Los digitos de la parte que a este periodo le toca. TOTAL.</summary>
		internal string TheShareAsSaid()
		{
			return _share.TheAmountAsSaid();
		}

		/// <summary>El codigo de la moneda de la parte. TOTAL.</summary>
		internal string TheCodeOfTheShareCurrency()
		{
			return _share.TheCodeOfItsCurrency();
		}
	}
}
