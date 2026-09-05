using System;
using System.Globalization;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// La parte de un gasto duradero que un periodo consumio: en linea recta a lo largo de su vida
	/// util, por cuanto de esa ventana cae adentro del periodo. Se compone en cada lectura y no se
	/// guarda en ninguna parte.
	/// <para>
	/// Su frase dice la regla y no solo el resultado -- el gasto entero, la vida util, el instante en
	/// que se incurrio y lo que a este periodo le toca -- para que quien lea pueda revisar la cuenta
	/// en vez de creerla.
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
		/// La parte que ese periodo consumio de ese gasto. Revienta si el periodo no toca la ventana:
		/// un cero ahi se leeria como que el periodo consumio nada de algo que estaba activo, y hay
		/// que preguntar antes si consume algo.
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

		/// <summary>
		/// La parte en una frase, con la regla adentro: el gasto entero, en que se fue, a lo largo de
		/// cuanto se reparte, desde cuando, y lo que a este periodo le toca.
		/// </summary>
		internal string Describe()
		{
			return _whole.Expressed() + " en " + _subject.Value + ", en línea recta a lo largo de "
				+ _life.HowItIsSaid() + " desde el " + TheInstant(_incurredAt)
				+ ": a este período le tocan " + _share.Expressed() + ".";
		}

		private static string TheInstant(DateTime instant)
		{
			return instant.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
		}
	}
}
