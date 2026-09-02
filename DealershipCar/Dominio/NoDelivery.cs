using System;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// La entrega que la agencia todavia no registro. No es una entrega en cero el dia cero: es la
	/// constancia de que no hay ancla, y sin ancla ninguna de las dos mitades del sobre se puede
	/// medir.
	/// </summary>
	internal sealed class NoDelivery : Delivery
	{
		/// <summary>Se llega por <see cref="Delivery.None"/>.</summary>
		internal NoDelivery()
		{
		}

		/// <summary>No.</summary>
		internal override bool Exists()
		{
			return false;
		}

		/// <summary>Revienta: no hay entrega, asi que no hay instante.</summary>
		internal override DateTime DeliveredAt
		{
			get { throw NothingToMeasureFrom(); }
		}

		/// <summary>Revienta: no hay entrega, asi que no hay lectura.</summary>
		internal override int Reading
		{
			get { throw NothingToMeasureFrom(); }
		}

		/// <summary>Revienta: sin lectura no hay unidad en que este medida.</summary>
		internal override DistanceUnit Unit
		{
			get { throw NothingToMeasureFrom(); }
		}

		/// <summary>
		/// Revienta, y no contesta que no. Un "no esta en esa unidad" se leeria como un desajuste
		/// de unidades sobre una entrega que no existe, y quien componga la respuesta nombraria la
		/// ausencia equivocada: la que falta es la entrega, y tiene su propio tipo.
		/// </summary>
		internal override bool IsExpressedIn(DistanceUnit unit)
		{
			throw NothingToMeasureFrom();
		}

		private static DomainException NothingToMeasureFrom()
		{
			return new DomainException(
				"No hay entrega registrada para este vehiculo: hay que preguntar primero si la hay. "
				+ "Las dos mitades del sobre se miden desde la entrega, asi que un instante o una "
				+ "lectura inventados aqui pondrian al vehiculo adentro o afuera de una garantia "
				+ "que nadie midio.");
		}
	}
}
