using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// Dos periodos dichos juntos: cada uno nombrado con su propio ritmo, y el cambio del primero
	/// respecto al segundo dicho DENTRO de esa misma frase.
	/// <para>
	/// Nunca un cambio suelto. Un porcentaje sin los dos ritmos al lado es una mitad de una
	/// respuesta compuesta: quien lo reciba no puede revisarlo, y el dominio no dijo de que a que.
	/// Cuando alguno de los dos no tiene ritmo, la frase dice cual y por que, y no dice ningun
	/// cambio.
	/// </para>
	/// </summary>
	internal sealed class ATwoSpanComparison
	{
		private readonly RateAnswer _first;
		private readonly RateAnswer _second;

		private ATwoSpanComparison(RateAnswer first, RateAnswer second)
		{
			_first = first;
			_second = second;
		}

		/// <summary>
		/// La comparacion de esos dos ritmos, en ese orden: el cambio se dice del primero respecto
		/// al segundo.
		/// </summary>
		internal static ATwoSpanComparison Of(RateAnswer first, RateAnswer second)
		{
			if (first == null || second == null)
				throw new DomainException(
					"Una comparacion necesita los dos periodos: con uno solo no hay nada que "
					+ "comparar, y un periodo ausente no es un periodo sin ritmo.");

			return new ATwoSpanComparison(first, second);
		}

		/// <summary>
		/// Si el cambio se puede decir: hace falta que los DOS periodos tengan ritmo. TOTAL.
		/// </summary>
		internal bool CanTheChangeBeSaid()
		{
			return _first.CanBeSaid() && _second.CanBeSaid();
		}

		/// <summary>
		/// Los dos periodos y el cambio en una sola clausula. TOTAL: si alguno no tiene ritmo, la
		/// clausula sigue trayendo los dos periodos y dice que el cambio no se puede decir.
		/// </summary>
		internal ARateClause TheClauseItComposes()
		{
			if (!CanTheChangeBeSaid())
				return ARateClause.OfTwoPeriodsCompared(
					_first.TheClauseOfItsPeriod(), _second.TheClauseOfItsPeriod(),
					RateChange.CannotBeSaid, "");

			return ARateClause.OfTwoPeriodsCompared(
				_first.TheClauseOfItsPeriod(), _second.TheClauseOfItsPeriod(),
				_first.TheShapeOfItsChangeAgainst(_second),
				_first.TheChangeAgainstAsSaid(_second));
		}
	}
}
