using System;

namespace DealershipCar.Dominio
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

		/// <summary>
		/// La clausula de la mitad de distancia. El limite alcanzado exacto tiene frase propia: "0
		/// pasados del limite" se leeria como un defecto y no como una respuesta.
		/// </summary>
		protected override string TheDistanceHalfInWords()
		{
			if (_distanceLeft == 0)
				return "Distance: exactly at a limit of " + Amount(_distanceLimit) + ".";

			if (_distanceLeft < 0)
				return "Distance: " + Amount(-_distanceLeft) + " past a limit of "
					+ Amount(_distanceLimit) + ".";

			return "Distance: " + Amount(_distanceLeft) + " left of " + Amount(_distanceLimit) + ".";
		}

		private string Amount(int quantity)
		{
			return quantity + " " + _unit.Name;
		}
	}
}
