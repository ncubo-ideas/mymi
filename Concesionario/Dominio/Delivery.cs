using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// La entrega del vehiculo -- cuando se entrego y en que odometro estaba ese dia -- o la
	/// constancia de que la agencia todavia no la registro. Es el ancla desde donde se miden las
	/// dos mitades del sobre de garantia.
	/// </summary>
	internal abstract class Delivery
	{
		/// <summary>No hay entrega registrada. Instancia unica, compartida.</summary>
		internal static readonly Delivery None = new NoDelivery();

		/// <summary>
		/// La entrega con su instante, su lectura y la unidad en que se leyo. Una lectura negativa
		/// o una unidad ausente son defectos de quien llama.
		/// </summary>
		internal static Delivery Of(DateTime deliveredAt, int reading, DistanceUnit unit)
		{
			if (reading < 0)
				throw new DomainException(
					$"El odometro de la entrega no puede ser negativo, y llego {reading}.");

			if (unit == null)
				throw new DomainException(
					"Una entrega con lectura y sin unidad es un numero que se puede comparar contra "
					+ "cualquier cosa, y contra el odometro de hoy hay que comparar.");

			return new ADelivery(deliveredAt, reading, unit);
		}

		/// <summary>Si la entrega esta registrada. Es la pregunta TOTAL del par: nunca revienta.</summary>
		internal abstract bool Exists();

		/// <summary>Cuando se entrego. Revienta si no hay entrega registrada.</summary>
		internal abstract DateTime DeliveredAt { get; }

		/// <summary>
		/// El odometro del dia de la entrega. Revienta si no hay entrega: ausente no es cero, y un
		/// cero aqui contaria como recorrido bajo garantia todo lo que el vehiculo llevaba encima.
		/// </summary>
		internal abstract int Reading { get; }

		/// <summary>La unidad de esa lectura. Revienta si no hay entrega.</summary>
		internal abstract DistanceUnit Unit { get; }

		/// <summary>
		/// Si la lectura de la entrega esta en esa unidad, POR IDENTIDAD. Accesor GUARDADO, y aca
		/// no se convierte nada: dos unidades distintas no se comparan, se dejan de comparar.
		/// </summary>
		internal abstract bool IsExpressedIn(DistanceUnit unit);
	}
}
