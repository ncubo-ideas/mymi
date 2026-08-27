using System;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// Donde estaba el odometro cuando se anclo el intervalo, o la constancia de que el ancla no
	/// tiene mitad de odometro. Es la mitad opcional de un <see cref="BasePoint"/>.
	/// </summary>
	internal abstract class AnchorReading
	{
		/// <summary>El ancla no tiene lectura. Instancia unica, compartida.</summary>
		internal static readonly AnchorReading None = new NoAnchorReading();

		/// <summary>
		/// El ancla con lectura y unidad. Un valor negativo o una unidad ausente son defectos de
		/// quien llama.
		/// </summary>
		internal static AnchorReading Of(int value, DistanceUnit unit)
		{
			if (value < 0)
				throw new DomainException(
					$"La lectura de un punto base no puede ser negativa, y llego {value}.");

			if (unit == null)
				throw new DomainException(
					"Un punto base con lectura y sin unidad es un numero que se puede comparar "
					+ "contra cualquier cosa.");

			return new AnAnchorReading(value, unit);
		}

		/// <summary>Si el ancla tiene lectura. Total: contesta siempre.</summary>
		internal abstract bool Exists();

		/// <summary>La lectura. Revienta si no hay: ausente no es cero.</summary>
		internal abstract int Value { get; }

		/// <summary>La unidad de la lectura. Revienta si no hay.</summary>
		internal abstract DistanceUnit Unit { get; }

		/// <summary>
		/// Si el ancla esta medida en esa unidad. Total, y por IDENTIDAD: sin lectura no esta
		/// medida en ninguna.
		/// </summary>
		internal abstract bool IsExpressedIn(DistanceUnit unit);
	}
}
