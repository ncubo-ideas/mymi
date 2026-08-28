using System;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// El hito de mantenimiento al que se le publico un precio: una distancia con la unidad en que
	/// se publico. Las dos mitades son una sola cosa -- una distancia suelta no nombra ningun hito
	/// -- y dos hitos en unidades distintas nunca son el mismo, porque aca no se convierte nada.
	/// </summary>
	internal sealed class PublishedMilestone
	{
		private readonly int _distance;
		private readonly DistanceUnit _unit;

		/// <summary>
		/// Un hito es una distancia positiva con su unidad. En cero o en negativo no nombra ningun
		/// servicio, y sin unidad no se puede decir cual es.
		/// </summary>
		internal PublishedMilestone(int distance, DistanceUnit unit)
		{
			if (unit == null)
				throw new DomainException(
					"Se nombro un hito sin decir en que unidad esta su distancia. Una distancia sin "
					+ "unidad no se puede comparar con nada, y aca las unidades no se convierten "
					+ "entre si.");

			if (distance <= 0)
				throw new DomainException(
					$"Se nombro un hito de {distance} {unit.Name}. Un hito es la distancia a la que "
					+ "se hace un servicio: en cero o en negativo no hay servicio que nombrar.");

			_distance = distance;
			_unit = unit;
		}

		/// <summary>
		/// Si los dos hitos son el mismo: la misma distancia Y la misma unidad. Es la comparacion
		/// con nombre que reemplaza a una igualdad escrita, y se niega a mirar dos unidades
		/// distintas como si fueran una -- el mismo numero en la otra unidad es OTRO hito.
		/// </summary>
		internal bool IsTheSameMilestoneAs(PublishedMilestone other)
		{
			if (other == null)
				throw new DomainException(
					"Se pregunto si un hito es el mismo que ninguno. Sin el otro hito no hay "
					+ "comparacion que hacer.");

			return _distance == other._distance && _unit == other._unit;
		}

		/// <summary>El hito dicho: la distancia con la unidad en que se publico.</summary>
		internal string Describe()
		{
			return _distance + " " + _unit.Name;
		}
	}
}
