using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// El par derivado con sus DOS mitades medidas: los meses que quedan del plazo y la distancia que
	/// queda del limite, en la unidad en que se publico. Llegar aqui quiere decir que las tres
	/// distancias -- el limite, la entrega y el odometro -- estaban en la misma unidad.
	/// </summary>
	internal sealed class ARemainingEnvelope : RemainingEnvelope
	{
		private readonly int _distanceLeft;
		private readonly int _distanceLimit;
		private readonly DistanceUnit _unit;

		/// <summary>Se llega por <see cref="RemainingEnvelope.InBothHalves"/>, que lleva las guardas.</summary>
		internal ARemainingEnvelope(
			int monthsLeft, int monthsLimit, int distanceLeft, int distanceLimit, DistanceUnit unit)
			: base(monthsLeft, monthsLimit)
		{
			_distanceLeft = distanceLeft;
			_distanceLimit = distanceLimit;
			_unit = unit;
		}

		/// <summary>Si.</summary>
		internal override bool TheDistanceHalfWasMeasured()
		{
			return true;
		}

		/// <summary>Lo que queda del limite de distancia, que aca si se pudo medir.</summary>
		internal override int DistanceLeft
		{
			get { return _distanceLeft; }
		}

		/// <summary>El umbral es INCLUSIVO: la distancia exacta del limite ya lo alcanzo.</summary>
		internal override bool TheDistanceHalfIsReached
		{
			get { return _distanceLeft <= 0; }
		}

		/// <summary>Medida, que es la forma que trae numero.</summary>
		internal override string TheDistanceHalfShape()
		{
			return EnvelopeDistanceHalf.Measured;
		}

		/// <summary>
		/// Lo que queda del limite, en una fila. Cero es el limite alcanzado exacto y negativo es
		/// cuanto se paso; las dos son respuestas, y cual de las tres se dice lo elige quien redacta
		/// por el signo.
		/// </summary>
		internal override int[] TheDistanceLeftCounted()
		{
			return OneAmountOf(_distanceLeft);
		}

		/// <summary>El limite contra el que se midio, en una fila.</summary>
		internal override int[] TheDistanceLimitCounted()
		{
			return OneAmountOf(_distanceLimit);
		}

		/// <summary>La unidad en que estan las dos, por su nombre.</summary>
		internal override string TheUnitTheDistanceIsSaidIn()
		{
			return _unit.Name;
		}
	}
}
