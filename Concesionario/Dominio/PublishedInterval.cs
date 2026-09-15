using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// Cada cuanto se hace algo, TAL COMO LO PUBLICO el fabricante: una distancia con su unidad,
	/// un plazo en dias, o las dos. Es un numero publicado y no un procedimiento de decision --
	/// quien mide contra el es la agenda del vehiculo, con sus propias bases.
	/// </summary>
	internal sealed class PublishedInterval
	{
		private readonly int _distance;
		private readonly DistanceUnit _unit;
		private readonly int _days;

		/// <summary>
		/// Un intervalo lleva al menos una de sus dos mitades, y la distancia siempre viene con la
		/// unidad en que se publico. Sin ninguna de las dos no es un intervalo: seria una hoja que
		/// dice que algo se cambia cada nada.
		/// </summary>
		internal PublishedInterval(int distance, DistanceUnit unit, int days)
		{
			if (unit == null)
				throw new DomainException(
					"Se publico un intervalo sin decir en que unidad esta su distancia. Una "
					+ "distancia sin unidad no se puede comparar con nada, y aca las unidades no se "
					+ "convierten entre si.");

			if (distance < 0 || days < 0)
				throw new DomainException(
					$"Se publico un intervalo de {distance} {unit.Name} y {days} dias. Un intervalo "
					+ "es cada cuanto se repite algo: en negativo no dice nada que se pueda "
					+ "cumplir.");

			if (distance == 0 && days == 0)
				throw new DomainException(
					"Se publico un intervalo sin distancia y sin plazo. Un intervalo que no es ni "
					+ "una distancia ni un tiempo no es un intervalo, y publicado dejaria una hoja "
					+ "diciendo que algo se hace cada nada.");

			_distance = distance;
			_unit = unit;
			_days = days;
		}

		/// <summary>
		/// Que mitades se publicaron, por su codigo. La que no se publico no se nombra: un cero ahi se
		/// leeria como un plazo cumplido desde siempre. TOTAL.
		/// </summary>
		internal string TheHalvesItPublished()
		{
			if (_distance == 0) return IntervalHalvesPublished.ByTime;
			if (_days == 0) return IntervalHalvesPublished.ByDistance;

			return IntervalHalvesPublished.Both;
		}

		/// <summary>Cada cuanta distancia, o cero donde esa mitad no se publico. TOTAL.</summary>
		internal int TheDistanceItPublished()
		{
			return _distance;
		}

		/// <summary>Cada cuantos dias, o cero donde esa mitad no se publico. TOTAL.</summary>
		internal int TheDaysItPublished()
		{
			return _days;
		}

		/// <summary>La unidad de esa distancia, como se dice. TOTAL.</summary>
		internal string TheUnitItsDistanceIsIn()
		{
			return _unit.Name;
		}

		/// <summary>
		/// Donde queda una distancia RECIBIDA frente a este intervalo, por su codigo. Es el unico sitio
		/// del modulo donde un numero publicado y uno recibido se ponen uno al lado del otro, y por eso
		/// es donde vive la regla: si vienen en unidades distintas NO SE CONVIERTE NINGUNA -- se dice
		/// que no se puede evaluar y se nombran las dos, para que quien lea sepa por que. Un intervalo
		/// publicado solo como plazo tampoco se puede comparar con una distancia: no hay mitad de
		/// distancia contra la cual pararse, e inventarla seria publicar por el fabricante.
		/// </summary>
		internal string UnitsAreNeverConverted(DistanceUnit remainingUnit)
		{
			if (remainingUnit == null)
				throw new DomainException(
					"Se pregunto por una distancia sin decir en que unidad esta. Sin unidad no hay "
					+ "nada que comparar ni nada que decir que no se puede comparar.");

			if (_distance == 0) return RemainderAgainstTheInterval.NoDistanceHalfPublished;
			if (_unit != remainingUnit) return RemainderAgainstTheInterval.UnitMismatch;

			return RemainderAgainstTheInterval.Measured;
		}
	}
}
