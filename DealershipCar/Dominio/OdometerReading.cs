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

		/// <summary>De donde vino. Se dice en cada fila del registro y no pondera nada.</summary>
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
					"Una lectura sin fuente es una lectura que no dice de donde salio, y cada fila "
					+ "del registro tiene que decirlo.");

			Value = value;
			Unit = unit;
			ObservedAt = observedAt;
			Source = source;
		}

		/// <summary>
		/// Esa misma unidad, dicha con su codigo, para que un script pueda pasarla como argumento
		/// al metodo de otro agregado. Total con excepcion: una unidad que este mapeo no nombre
		/// revienta en vez de caer en la primera, porque caer en la primera mediria contra una
		/// unidad distinta de la que la frase nombra y nada se veria mal.
		/// </summary>
		internal DistanceUnitCode TheCodeOfItsUnit
		{
			get
			{
				if (Unit == DistanceUnit.UnitA) return DistanceUnitCode.UnitA;
				if (Unit == DistanceUnit.UnitB) return DistanceUnitCode.UnitB;

				throw new DomainException(
					$"La unidad '{Unit.Name}' de esta lectura no tiene codigo con que cruzar hacia "
					+ "otro agregado. Entro una unidad de distancia que este mapeo no nombra.");
			}
		}

		/// <summary>Si esta expresada en esa unidad. Por IDENTIDAD, nunca por nombre.</summary>
		internal bool IsExpressedIn(DistanceUnit unit)
		{
			return Unit == unit;
		}
	}
}
