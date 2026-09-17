using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// Un gasto que sigue dando valor despues del periodo en que se pago: llantas, una bateria, una
	/// faja de tiempo. Lleva su vida util RECIBIDA y se reparte en linea recta a lo largo de ella.
	/// <para>
	/// La parte que a un periodo le toca se deriva en cada lectura desde la ventana y el periodo, y
	/// no se guarda: una entrada que llegara despues redefine periodos ya vistos.
	/// </para>
	/// </summary>
	internal sealed class ADurableCost : ACostEntry
	{
		private readonly AnExpectedLife _life;

		/// <summary>
		/// Se llega por el comando duradero del libro, que es lo que la clasifica. La vida util es
		/// obligatoria: sin ella no se puede amortizar, y repartirla sobre un solo periodo
		/// convertiria la vista de lo consumido en la de caja.
		/// </summary>
		internal ADurableCost(
			Money money, DateTime incurredAt, CostSubject subject, AnExpectedLife life)
			: base(money, incurredAt, subject)
		{
			if (life == null)
				throw new DomainException(
					"Llego un gasto duradero en " + subject.Value + " sin su vida util. Sin ella no "
					+ "hay a lo largo de cuanto repartirlo, y cuanto dura una pieza lo sabe quien la "
					+ "vendio.");

			if (!life.FitsAWindowFrom(incurredAt))
				throw new DomainException(
					"Llego un gasto duradero en " + subject.Value + " con una vida util de "
					+ life.HowItIsSaid() + " incurrido tan adelante que su ventana no cabe en el "
					+ "tiempo que este modulo puede decir.");

			_life = life;
		}

		/// <summary>Duradero.</summary>
		internal override CostClassification Classification()
		{
			return CostClassification.Durable;
		}

		/// <summary>Si: se reparte en linea recta a lo largo de su vida util.</summary>
		internal override bool SpreadsItselfOverTime()
		{
			return true;
		}

		/// <summary>A lo largo de cuantos meses, tal como llegaron con la entrada.</summary>
		internal override int TheMonthsOfItsExpectedLife()
		{
			return _life.TheMonthsItLasts();
		}

		/// <summary>Si la ventana de su vida util se cruza con ese periodo. TOTAL.</summary>
		internal override bool ConsumesAnythingBetween(DateTime from, DateTime to)
		{
			return _life.TicksItCoversBetween(IncurredAt, from, to) > 0;
		}

		/// <summary>La parte del gasto que ese periodo consumio. Accesor GUARDADO.</summary>
		internal override Money TheMoneyItConsumedBetween(DateTime from, DateTime to)
		{
			return TheShareItGivesBetween(from, to).TheMoneyOfTheShare();
		}

		/// <summary>
		/// La parte que ese periodo consumio, con la regla adentro: el gasto entero, la vida util,
		/// desde cuando, y lo que a este periodo le toca. Accesor GUARDADO en la parte misma.
		/// </summary>
		internal override AnAmortizedShare TheShareItGivesBetween(DateTime from, DateTime to)
		{
			return AnAmortizedShare.Of(TheMoneyItCost(), Subject, _life, IncurredAt, from, to);
		}
	}
}
