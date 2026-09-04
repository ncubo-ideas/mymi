using System;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// El periodo tiene lecturas en mas de una unidad de distancia, asi que la distancia entre sus
	/// extremos no es una distancia. Sigue la forma de
	/// <see cref="ADimensionSuspendedByAUnitMismatch"/>, que dice la misma razon en la agenda.
	/// <para>
	/// Nombra las DOS unidades y la lectura que introdujo la segunda, con su fecha: "hay unidades
	/// mezcladas" es una categoria, y la lectura del 12 de junio es algo que alguien puede ir a
	/// arreglar.
	/// </para>
	/// </summary>
	internal sealed class ARateSuspendedByAUnitMismatch : RateAnswer
	{
		private readonly string _unitOfThePeriod;
		private readonly ASeriesRow _introducedTheSecond;

		/// <summary>Se llega por <see cref="RateAnswer.UnitsThatDoNotMatch"/>.</summary>
		internal ARateSuspendedByAUnitMismatch(
			string unitOfThePeriod, ASeriesRow introducedTheSecond)
		{
			if (string.IsNullOrWhiteSpace(unitOfThePeriod))
				throw new DomainException(
					"Una suspension por unidades tiene que nombrar la unidad del periodo: sin ella "
					+ "quien lea no sabe cual de las dos lecturas esta fuera de lugar.");

			if (introducedTheSecond == null)
				throw new DomainException(
					"Una suspension por unidades tiene que nombrar la lectura que introdujo la "
					+ "segunda unidad: es la que se puede ir a arreglar.");

			_unitOfThePeriod = unitOfThePeriod;
			_introducedTheSecond = introducedTheSecond;
		}

		/// <summary>Suspendido por unidades distintas.</summary>
		internal override bool SuspendedByAUnitMismatch()
		{
			return true;
		}

		/// <summary>La situacion: suspendido por unidades distintas.</summary>
		internal override RateStanding Standing()
		{
			return RateStanding.SuspendedByAUnitMismatch;
		}

		/// <summary>
		/// La razon con las dos unidades y la lectura que trajo la segunda. Las unidades nunca se
		/// convierten, y eso se dice: es la mitad que explica por que no hay un numero.
		/// </summary>
		internal override string Describe()
		{
			return "no se puede decir cuánto se maneja: el periodo tiene lecturas en "
				+ _unitOfThePeriod + " y en " + _introducedTheSecond.TheNameOfItsUnit()
				+ ", y las unidades nunca se convierten. La segunda unidad la introdujo la lectura "
				+ "de " + _introducedTheSecond.TheReadingItSays() + " del "
				+ _introducedTheSecond.TheInstantItWasObserved();
		}
	}
}
