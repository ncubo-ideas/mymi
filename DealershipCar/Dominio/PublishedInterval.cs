using System;

namespace DealershipCar.Dominio
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
		/// La frase del intervalo, con la mitad o las dos mitades que se publicaron. La que no se
		/// publico no se nombra: un cero ahi se leeria como un plazo cumplido desde siempre.
		/// </summary>
		internal string Describe()
		{
			if (_distance == 0) return "every " + _days + " days";

			string byDistance = "every " + _distance + " " + _unit.Name;

			if (_days == 0) return byDistance;

			return byDistance + " or " + _days + " days";
		}
	}
}
