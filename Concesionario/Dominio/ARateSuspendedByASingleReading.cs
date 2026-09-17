using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// El periodo tiene una sola lectura usable. Un ritmo necesita dos puntos, y un punto es un
	/// TODAVIA NO.
	/// <para>
	/// Nunca cero: cero quiere decir que el vehiculo no se movio, que es otro hecho y hasta puede
	/// ser cierto. La lectura que hay se nombra con su fecha, que es lo que deja ver que falta la
	/// segunda y no la primera.
	/// </para>
	/// </summary>
	internal sealed class ARateSuspendedByASingleReading : RateAnswer
	{
		private readonly ASeriesRow _theOnlyOne;

		/// <summary>Se llega por <see cref="RateAnswer.OnlyOneReading"/>.</summary>
		internal ARateSuspendedByASingleReading(ASeriesRow theOnlyOne)
		{
			if (theOnlyOne == null)
				throw new DomainException(
					"Esta suspension existe porque hay UNA lectura, asi que tiene que nombrarla: "
					+ "sin ella la frase no se distingue de la del periodo vacio.");

			_theOnlyOne = theOnlyOne;
		}

		/// <summary>Suspendido porque hay una sola lectura.</summary>
		internal override bool SuspendedByASingleReading()
		{
			return true;
		}

		/// <summary>La situacion: suspendido por una sola lectura.</summary>
		internal override RateStanding Standing()
		{
			return RateStanding.SuspendedByASingleReading;
		}

		/// <summary>
		/// El todavia no, con la lectura que hay nombrada y con su fecha. Lo que se dice es que
		/// faltan dos puntos, no que el ritmo sea cero.
		/// </summary>
		internal override ARateInAPeriod TheClauseOfItsPeriod()
		{
			return ARateInAPeriod.SuspendedByASingleReading(
				_theOnlyOne.TheValueItRead(),
				_theOnlyOne.TheNameOfItsUnit(),
				_theOnlyOne.TheInstantItWasObserved());
		}
	}
}
