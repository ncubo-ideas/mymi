using System;

namespace Concesionario.Dominio
{
	/// <summary>La entrega registrada: el instante, el odometro de ese dia y su unidad.</summary>
	internal sealed class ADelivery : Delivery
	{
		private readonly DateTime _deliveredAt;
		private readonly int _reading;
		private readonly DistanceUnit _unit;

		/// <summary>Se llega por <see cref="Delivery.Of"/>, que lleva las guardas.</summary>
		internal ADelivery(DateTime deliveredAt, int reading, DistanceUnit unit)
		{
			_deliveredAt = deliveredAt;
			_reading = reading;
			_unit = unit;
		}

		/// <summary>Si.</summary>
		internal override bool Exists()
		{
			return true;
		}

		/// <summary>Cuando se entrego.</summary>
		internal override DateTime DeliveredAt
		{
			get { return _deliveredAt; }
		}

		/// <summary>El odometro del dia de la entrega.</summary>
		internal override int Reading
		{
			get { return _reading; }
		}

		/// <summary>La unidad en que se leyo.</summary>
		internal override DistanceUnit Unit
		{
			get { return _unit; }
		}

		/// <summary>Compara por IDENTIDAD, nunca por nombre.</summary>
		internal override bool IsExpressedIn(DistanceUnit unit)
		{
			return _unit == unit;
		}
	}
}
