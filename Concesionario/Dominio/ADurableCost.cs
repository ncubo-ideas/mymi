using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// Un gasto que sigue dando valor despues del periodo en que se pago: llantas, una bateria, una
	/// faja de tiempo. Lleva su vida util RECIBIDA y se reparte en linea recta a lo largo de ella.
	/// <para>
	/// La parte que a un periodo le toca se deriva en cada lectura desde la ventana y el periodo, y no
	/// se guarda: una entrada que llegara despues redefine periodos ya vistos.
	/// </para>
	/// </summary>
	internal sealed class ADurableCost : ACostEntry
	{
		private readonly AnExpectedLife _life;

		/// <summary>
		/// Se llega por el comando duradero del libro, que es lo que la clasifica. La vida util es
		/// obligatoria: sin ella no se puede amortizar, y repartirla sobre un solo periodo convertiria
		/// la vista de lo consumido en la de caja.
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

		/// <summary>La vida util con que llego, dicha en meses.</summary>
		internal override string HowItsExpectedLifeIsSaid()
		{
			return _life.HowItIsSaid();
		}

		/// <summary>Si la ventana de su vida util se cruza con ese periodo. TOTAL.</summary>
		internal override bool ConsumesAnythingBetween(DateTime from, DateTime to)
		{
			return _life.TicksItCoversBetween(IncurredAt, from, to) > 0;
		}

		/// <summary>La parte del gasto que ese periodo consumio. Accesor GUARDADO.</summary>
		internal override Money TheMoneyItConsumedBetween(DateTime from, DateTime to)
		{
			return TheShareBetween(from, to).TheMoneyOfTheShare();
		}

		/// <summary>
		/// La regla dicha para ese periodo: el gasto entero, la vida util, desde cuando, y lo que a
		/// este periodo le toca. Nada cuando el periodo no toca la ventana.
		/// </summary>
		internal override string TheClauseOfItsConsumptionBetween(DateTime from, DateTime to)
		{
			if (!ConsumesAnythingBetween(from, to)) return "";

			return TheShareBetween(from, to).Describe();
		}

		/// <summary>La entrada dicha entera, con su clase y con la regla con que se reparte.</summary>
		internal override string Describe()
		{
			return TheSpendClause() + ". " + Classification().HowItIsSaid
				+ ": se reparte en línea recta a lo largo de " + _life.HowItIsSaid()
				+ " desde esa fecha.";
		}

		private AnAmortizedShare TheShareBetween(DateTime from, DateTime to)
		{
			return AnAmortizedShare.Of(
				TheMoneyItCost(), Subject, _life, IncurredAt, from, to);
		}
	}
}
