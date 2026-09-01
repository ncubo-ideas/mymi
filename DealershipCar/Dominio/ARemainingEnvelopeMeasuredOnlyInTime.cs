using System;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// El par derivado cuya mitad de distancia NO se pudo medir: la de tiempo lleva su numero y la
	/// otra dice que falta y por que. La mitad ausente se dice; no vale cero, ni la lectura de la
	/// entrega haciendo de la de hoy, ni el limite entero como si nada se hubiera recorrido.
	/// <para>
	/// El motivo llega en palabras y no como un tipo por cada uno: quien compone es quien sabe cual
	/// de las dos distancias del registro no calzo, y en la respuesta el motivo ya tiene su propio
	/// tipo un piso mas arriba.
	/// </para>
	/// </summary>
	internal sealed class ARemainingEnvelopeMeasuredOnlyInTime : RemainingEnvelope
	{
		private readonly string _whyItCouldNotBeMeasured;

		/// <summary>
		/// Se llega por <see cref="RemainingEnvelope.MeasuredOnlyInTime"/>, que lleva las guardas.
		/// </summary>
		internal ARemainingEnvelopeMeasuredOnlyInTime(
			int monthsLeft, int monthsLimit, string whyItCouldNotBeMeasured)
			: base(monthsLeft, monthsLimit)
		{
			_whyItCouldNotBeMeasured = whyItCouldNotBeMeasured;
		}

		/// <summary>No.</summary>
		internal override bool TheDistanceHalfWasMeasured()
		{
			return false;
		}

		/// <summary>Revienta: no hay distancia medida, asi que tampoco lo que queda de ella.</summary>
		internal override int DistanceLeft
		{
			get { throw TheDistanceHalfWasNotMeasured(); }
		}

		/// <summary>
		/// Revienta, y no contesta que no. Un "no llego" para una mitad que nadie midio se lee como
		/// una medicion, y desde ahi todo lo recorrido cabe adentro de cualquier sobre.
		/// </summary>
		internal override bool TheDistanceHalfIsReached
		{
			get { throw TheDistanceHalfWasNotMeasured(); }
		}

		/// <summary>Dice que falta y por que, y no da ningun numero en su lugar.</summary>
		protected override string TheDistanceHalfInWords()
		{
			return "Distance: cannot be measured, " + _whyItCouldNotBeMeasured.Trim();
		}

		private static DomainException TheDistanceHalfWasNotMeasured()
		{
			return new DomainException(
				"La mitad de distancia de este sobre no se pudo medir: hay que preguntar primero si "
				+ "se midio. Un numero devuelto aca se leeria como un odometro que alguien leyo, y "
				+ "nadie mas abajo podria distinguirlo.");
		}
	}
}
