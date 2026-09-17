using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// El par derivado cuya mitad de distancia NO se pudo medir: la de tiempo lleva su numero y la
	/// otra dice cual de las dos razones lo impidio. La mitad ausente se dice; no vale cero, ni la
	/// lectura de la entrega haciendo de la de hoy, ni el limite entero como si nada se hubiera
	/// recorrido.
	/// <para>
	/// El motivo llega por su CODIGO y no como un tipo por cada uno: quien compone es quien sabe cual
	/// de las dos distancias del registro no calzo, y en la respuesta el motivo ya tiene su propio
	/// tipo un piso mas arriba.
	/// </para>
	/// </summary>
	internal sealed class ARemainingEnvelopeMeasuredOnlyInTime : RemainingEnvelope
	{
		private readonly string _whyItCouldNotBeMeasured;
		private readonly string _whoseDistance;
		private readonly string _itsUnit;

		/// <summary>
		/// Se llega por las dos fabricas suspendidas de <see cref="RemainingEnvelope"/>, que llevan
		/// las guardas.
		/// </summary>
		internal ARemainingEnvelopeMeasuredOnlyInTime(
			int monthsLeft, int monthsLimit, string whyItCouldNotBeMeasured,
			string whoseDistance, string itsUnit)
			: base(monthsLeft, monthsLimit)
		{
			_whyItCouldNotBeMeasured = whyItCouldNotBeMeasured;
			_whoseDistance = whoseDistance;
			_itsUnit = itsUnit;
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

		/// <summary>Cual de las dos razones impidio medirla, y no da ningun numero en su lugar.</summary>
		internal override string TheDistanceHalfShape()
		{
			return _whyItCouldNotBeMeasured;
		}

		/// <summary>La unidad que no coincidio con la del limite, o vacia si no fue por unidades.</summary>
		internal override string TheUnitThatDidNotMatch()
		{
			return _itsUnit;
		}

		/// <summary>Cual de las dos distancias del registro no calzo, o vacio por lo mismo.</summary>
		internal override string WhoseDistanceDidNotMatch()
		{
			return _whoseDistance;
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
