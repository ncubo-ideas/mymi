using System;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// El ancla desde la que se mide un intervalo: el instante, y -- cuando la ley mide distancia
	/// y la operacion ya se completo -- la lectura del odometro y su unidad. La lectura es la
	/// mitad opcional; el instante siempre esta.
	/// </summary>
	internal sealed class BasePoint
	{
		private readonly AnchorReading _reading;

		private BasePoint(DateTime instant, AnchorReading reading)
		{
			Instant = instant;
			_reading = reading;
		}

		/// <summary>Desde cuando se cuenta el intervalo.</summary>
		internal DateTime Instant { get; }

		/// <summary>
		/// Si este ancla tiene lectura de odometro. Se pregunta antes de leer
		/// <see cref="Reading"/>.
		/// </summary>
		internal bool HasReading
		{
			get { return _reading.Exists(); }
		}

		/// <summary>La lectura del ancla. Revienta si no hay: ausente no es cero.</summary>
		internal int Reading
		{
			get { return _reading.Value; }
		}

		/// <summary>La unidad de la lectura del ancla. Revienta si no hay.</summary>
		internal DistanceUnit Unit
		{
			get { return _reading.Unit; }
		}

		/// <summary>
		/// El ancla completa, la que deja una completacion real de una operacion que mide
		/// distancia. Las guardas viven en <see cref="AnchorReading.Of"/>.
		/// </summary>
		internal static BasePoint At(DateTime instant, int reading, DistanceUnit unit)
		{
			return new BasePoint(instant, AnchorReading.Of(reading, unit));
		}

		/// <summary>
		/// El ancla de solo instante: una ley que no mide distancia, o una operacion que nunca se
		/// completo.
		/// </summary>
		internal static BasePoint AtInstantOnly(DateTime instant)
		{
			return new BasePoint(instant, AnchorReading.None);
		}

		/// <summary>
		/// Si este ancla esta medido en esa unidad. Total, y por IDENTIDAD: sin lectura no esta
		/// medido en ninguna.
		/// </summary>
		internal bool MeasuresDistanceIn(DistanceUnit unit)
		{
			return _reading.IsExpressedIn(unit);
		}
	}
}
