using System;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// El periodo no tiene ninguna lectura del odometro. Espeja
	/// <see cref="ADimensionSuspendedByAnAbsentReading"/>, que dice la misma ausencia en la agenda.
	/// <para>
	/// Nombra el periodo por el que se pregunto: sin el, todos los periodos vacios dirian la misma
	/// frase y quien lea no sabria de cual le estan hablando.
	/// </para>
	/// </summary>
	internal sealed class ARateSuspendedByAnAbsentReading : RateAnswer
	{
		private readonly string _from;
		private readonly string _to;

		/// <summary>Se llega por <see cref="RateAnswer.NoReadingInThePeriod"/>.</summary>
		internal ARateSuspendedByAnAbsentReading(string from, string to)
		{
			if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
				throw new DomainException(
					"Una ausencia tiene que nombrar el periodo del que habla: sin los dos limites "
					+ "todos los periodos vacios dicen la misma frase.");

			_from = from;
			_to = to;
		}

		/// <summary>Suspendido porque no hay ninguna lectura en el periodo.</summary>
		internal override bool SuspendedByAnAbsentReading()
		{
			return true;
		}

		/// <summary>La situacion: suspendido por ausencia de lecturas.</summary>
		internal override RateStanding Standing()
		{
			return RateStanding.SuspendedByAnAbsentReading;
		}

		/// <summary>La ausencia con el periodo nombrado. Nunca cero y nunca la frase vacia.</summary>
		internal override string Describe()
		{
			return "no se puede decir cuánto se maneja entre el " + _from + " y el " + _to
				+ ": no hay ninguna lectura del odómetro en ese periodo";
		}
	}
}
