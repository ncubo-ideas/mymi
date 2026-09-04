using System;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// Los dos extremos del periodo se observaron en el mismo segundo entero, asi que no hay tiempo
	/// transcurrido por el que dividir.
	/// <para>
	/// Es un estado CON NOMBRE y no una division por cero, y tampoco es un tramo demasiado corto
	/// para decirse: el journal mide en segundos enteros, asi que dos lecturas dentro de uno son
	/// indistinguibles en el tiempo y eso es una contradiccion que se rechaza, no un umbral que se
	/// ajusta.
	/// </para>
	/// </summary>
	internal sealed class ARateSuspendedByAZeroLengthSpan : RateAnswer
	{
		private readonly string _theSecond;

		/// <summary>Se llega por <see cref="RateAnswer.AZeroLengthSpan"/>.</summary>
		internal ARateSuspendedByAZeroLengthSpan(string theSecond)
		{
			if (string.IsNullOrWhiteSpace(theSecond))
				throw new DomainException(
					"Esta suspension tiene que nombrar el segundo en que caen las dos lecturas: es "
					+ "el hecho que la explica.");

			_theSecond = theSecond;
		}

		/// <summary>Suspendido porque el tramo tiene largo cero.</summary>
		internal override bool SuspendedByAZeroLengthSpan()
		{
			return true;
		}

		/// <summary>La situacion: suspendido por un tramo de largo cero.</summary>
		internal override RateStanding Standing()
		{
			return RateStanding.SuspendedByAZeroLengthSpan;
		}

		/// <summary>La razon con el segundo nombrado.</summary>
		internal override string Describe()
		{
			return "no se puede decir cuánto se maneja: las dos lecturas del periodo se observaron "
				+ "en el mismo segundo, el " + _theSecond
				+ ", así que no hay tiempo transcurrido que dividir";
		}
	}
}
