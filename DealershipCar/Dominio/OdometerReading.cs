using System;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// Una lectura del odometro: el valor, su unidad, cuando se observo y de donde vino. La
	/// unidad viaja siempre con el valor.
	/// </summary>
	internal sealed class OdometerReading
	{
		/// <summary>El valor leido, en <see cref="Unit"/>.</summary>
		internal int Value { get; }

		/// <summary>La unidad del valor. Se compara por IDENTIDAD, nunca por nombre.</summary>
		internal DistanceUnit Unit { get; }

		/// <summary>Cuando se OBSERVO la lectura, que no es cuando llego.</summary>
		internal DateTime ObservedAt { get; }

		/// <summary>De donde vino, que es lo que determina cuanto se le cree.</summary>
		internal ReadingSource Source { get; }

		/// <summary>
		/// Un valor negativo, una unidad ausente o una fuente ausente son defectos de quien llama.
		/// </summary>
		internal OdometerReading(int value, DistanceUnit unit, DateTime observedAt, ReadingSource source)
		{
			if (value < 0)
				throw new DomainException(
					$"Una lectura del odometro no puede ser negativa, y llego {value}.");

			if (unit == null)
				throw new DomainException(
					"Una lectura sin unidad es un numero suelto, y un numero suelto se puede "
					+ "comparar contra cualquier cosa. La unidad viaja con el valor.");

			if (source == null)
				throw new DomainException(
					"Una lectura sin fuente no se puede pesar: la fuente es lo que determina "
					+ "cuanta confianza merece.");

			Value = value;
			Unit = unit;
			ObservedAt = observedAt;
			Source = source;
		}

		/// <summary>Si esta expresada en esa unidad. Por IDENTIDAD, nunca por nombre.</summary>
		internal bool IsExpressedIn(DistanceUnit unit)
		{
			return Unit == unit;
		}
	}
}
