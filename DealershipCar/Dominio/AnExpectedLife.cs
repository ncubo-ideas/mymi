using System;
using System.Globalization;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// Cuanto se espera que dure un gasto duradero, en meses enteros, TAL COMO LLEGO con la entrada.
	/// Nunca la elige este modulo y no tiene valor por defecto.
	/// <para>
	/// Es tambien el tipo que sabe la ventana: desde el instante en que se incurrio hasta ese instante
	/// mas sus meses, con el final abierto, y cuanto de esa ventana cae adentro de un periodo.
	/// </para>
	/// </summary>
	internal sealed class AnExpectedLife
	{
		private readonly int _months;

		/// <summary>
		/// Se recibe en meses positivos. Una vida que no es positiva no deja ninguna ventana sobre la
		/// que repartir el gasto, y no hay ninguna que poner en su lugar.
		/// </summary>
		internal AnExpectedLife(int months)
		{
			if (months <= 0)
				throw new DomainException(
					"Llego una vida util de " + months.ToString(CultureInfo.InvariantCulture)
					+ " meses. Una vida util que no es positiva no deja ninguna ventana sobre la que "
					+ "repartir el gasto, y cuanto dura una pieza lo sabe quien la vendio.");

			_months = months;
		}

		/// <summary>Como se dice la vida util en la frase que alguien lee.</summary>
		internal string HowItIsSaid()
		{
			return _months.ToString(CultureInfo.InvariantCulture) + (_months == 1 ? " mes" : " meses");
		}

		/// <summary>
		/// Si una entrada incurrida en ese instante tiene ventana entera dentro del tiempo que se
		/// puede decir. TOTAL, y es la pregunta que hay que hacer antes de armar la entrada.
		/// </summary>
		internal bool FitsAWindowFrom(DateTime incurredAt)
		{
			return incurredAt <= DateTime.MaxValue.AddMonths(-_months);
		}

		/// <summary>
		/// Cuanto dura la ventana entera de una entrada incurrida en ese instante. Accesor GUARDADO:
		/// un instante tan adelante que su ventana no cabe es un defecto de quien llama.
		/// </summary>
		internal long TicksOfTheWholeWindowFrom(DateTime incurredAt)
		{
			TheWindowFits(incurredAt);

			return (incurredAt.AddMonths(_months) - incurredAt).Ticks;
		}

		/// <summary>
		/// Cuanto de esa ventana cae entre esos dos instantes, ambos inclusive. TOTAL: un periodo que
		/// no la toca contesta nada, y un periodo al reves tambien.
		/// </summary>
		internal long TicksItCoversBetween(DateTime incurredAt, DateTime from, DateTime to)
		{
			TheWindowFits(incurredAt);

			DateTime windowEnds = incurredAt.AddMonths(_months);

			DateTime starts = from > incurredAt ? from : incurredAt;
			DateTime ends = to < windowEnds ? to : windowEnds;

			return ends <= starts ? 0 : (ends - starts).Ticks;
		}

		private void TheWindowFits(DateTime incurredAt)
		{
			if (!FitsAWindowFrom(incurredAt))
				throw new DomainException(
					"Llego un gasto duradero con una vida util de " + HowItIsSaid() + " incurrido "
					+ "tan adelante que su ventana no cabe en el tiempo que este modulo puede decir. "
					+ "Un gasto se reparte a lo largo de una ventana, y esa no existe.");
		}
	}
}
