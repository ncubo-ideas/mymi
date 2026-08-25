using System;

namespace DealershipCar.Dominio
{
	// LA ALTERNATIVA RECHAZADA: un int en cero en vez de HasReading. Se veria mas simple y
	// seria falso. Con "lectura cero", un vehiculo recien entregado parece haber recorrido todo
	// el odometro desde una completacion que nunca ocurrio, y la operacion aparece vencidisima
	// el primer dia. Ausente y cero son dos hechos, y el tipo tiene que poder distinguirlos.

	/// <summary>
	/// El ancla desde la que se mide un intervalo: el instante, y -- cuando la ley del
	/// intervalo mide distancia y la operacion ya se completo alguna vez -- la lectura del
	/// odometro y su unidad. La lectura es la mitad opcional; el instante siempre esta.
	/// </summary>
	internal sealed class BasePoint
	{
		private readonly int _reading;
		private readonly DistanceUnit _unit;

		private BasePoint(DateTime instant, bool hasReading, int reading, DistanceUnit unit)
		{
			Instant = instant;
			HasReading = hasReading;
			_reading = reading;
			_unit = unit;
		}

		/// <summary>
		/// Desde cuando se cuenta el intervalo. Siempre esta: una operacion sabe cuando se hizo
		/// aunque nadie haya leido el odometro.
		/// </summary>
		internal DateTime Instant { get; }

		/// <summary>
		/// Si este ancla tiene lectura de odometro. Es la pregunta de presencia que hay que
		/// hacer ANTES de leer <see cref="Reading"/> (E41): "no hay lectura" y "la lectura es
		/// cero" son hechos distintos, y confundirlos hace aparecer vencida una operacion que
		/// nunca se completo.
		/// </summary>
		internal bool HasReading { get; }

		/// <summary>
		/// La lectura del odometro en el momento del ancla. GUARDADA (E41): leerla cuando
		/// <see cref="HasReading"/> es falso es un defecto de quien llama y revienta, porque la
		/// ausencia de la lectura no es un cero.
		/// </summary>
		internal int Reading
		{
			get
			{
				if (!HasReading)
					throw new DomainException(
						"Este punto base no tiene lectura: pregunte HasReading antes. Devolver "
						+ "cero haria aparecer recorrido todo el odometro desde una completacion "
						+ "que nunca ocurrio.");

				return _reading;
			}
		}

		/// <summary>
		/// La unidad en que se tomo la lectura del ancla. GUARDADA igual que
		/// <see cref="Reading"/>: sin lectura no hay unidad que devolver.
		/// </summary>
		internal DistanceUnit Unit
		{
			get
			{
				if (!HasReading)
					throw new DomainException(
						"Este punto base no tiene lectura, asi que tampoco tiene unidad: "
						+ "pregunte HasReading antes.");

				return _unit;
			}
		}

		/// <summary>
		/// El ancla completa: instante, lectura y la unidad en que se tomo. Es la que deja una
		/// completacion real de una operacion cuya ley mide distancia (ley L4).
		/// </summary>
		internal static BasePoint At(DateTime instant, int reading, DistanceUnit unit)
		{
			if (reading < 0)
				throw new DomainException(
					$"La lectura de un punto base no puede ser negativa, y llego {reading}.");

			if (unit == null)
				throw new DomainException(
					"Un punto base con lectura y sin unidad es un numero que se puede comparar "
					+ "contra cualquier cosa (OINV-21).");

			return new BasePoint(instant, true, reading, unit);
		}

		/// <summary>
		/// El ancla de solo instante, sin lectura. Es la que deja una ley que no mide distancia
		/// -- TimeOnly, ConditionOnly -- y tambien la de una operacion que todavia no se
		/// completo nunca. Guardar aca una lectura que ninguna ley lee invitaria al proximo
		/// lector a creer que la distancia cuenta para esa operacion.
		/// </summary>
		internal static BasePoint AtInstantOnly(DateTime instant)
		{
			return new BasePoint(instant, false, 0, null);
		}

		/// <summary>
		/// Si este ancla esta medido en esa unidad. Comparacion por IDENTIDAD (E16/E23): un
		/// ancla sin lectura no esta medido en ninguna, y comparar por nombre seria igualdad de
		/// valor disfrazada de string.
		/// </summary>
		internal bool MeasuresDistanceIn(DistanceUnit unit)
		{
			return HasReading && _unit == unit;
		}
	}
}
