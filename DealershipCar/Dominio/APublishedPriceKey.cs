using System;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// La clave bajo la que se publico un precio: la operacion y el hito, que son una sola cosa.
	/// Existe para poder ENUMERAR los precios sin que nadie aporte un hito, y para que la
	/// comparacion de dos claves tenga un solo sitio.
	/// <para>
	/// Vive dentro del agregado y nunca cruza su superficie. Entrega sus dos mitades porque el
	/// resolvedor que contesta el precio las recibe; ninguna de las dos es una medicion.
	/// </para>
	/// </summary>
	internal sealed class APublishedPriceKey
	{
		private readonly MaintenanceOperationKind _operation;
		private readonly PublishedMilestone _milestone;

		private APublishedPriceKey(MaintenanceOperationKind operation, PublishedMilestone milestone)
		{
			_operation = operation;
			_milestone = milestone;
		}

		/// <summary>
		/// La unica forma de armar una clave, y las dos mitades son obligatorias: una operacion sin
		/// hito no nombra ningun precio, y un hito sin operacion tampoco.
		/// </summary>
		internal static APublishedPriceKey Of(
			MaintenanceOperationKind operation, PublishedMilestone milestone)
		{
			if (operation == null)
				throw new DomainException(
					"Se armo la clave de un precio sin decir de que operacion es. Sin la operacion la "
					+ "clave nombraria el hito de todos los servicios a la vez.");

			if (milestone == null)
				throw new DomainException(
					$"Se armo la clave de un precio de {operation.Name} sin decir para que hito. Un "
					+ "precio sin hito no se puede pedir: no se sabe que servicio cubre.");

			return new APublishedPriceKey(operation, milestone);
		}

		/// <summary>
		/// La clase de operacion que nombra. La entrega para que el resolvedor la reciba: es una
		/// clave de catalogo y no un hecho de ningun vehiculo.
		/// </summary>
		internal MaintenanceOperationKind TheOperationItNames()
		{
			return _operation;
		}

		/// <summary>El hito que nombra, por la misma razon.</summary>
		internal PublishedMilestone TheMilestoneItNames()
		{
			return _milestone;
		}

		/// <summary>
		/// Si las dos claves son la misma: la misma operacion Y el mismo hito. Es la comparacion CON
		/// NOMBRE que reemplaza a una igualdad escrita y a una tupla, y es el unico sitio donde vive
		/// la comparacion de dos claves de precio.
		/// </summary>
		internal bool IsTheSameKeyAs(APublishedPriceKey other)
		{
			if (other == null)
				throw new DomainException(
					"Se pregunto si la clave de un precio es la misma que ninguna. Sin la otra clave "
					+ "no hay comparacion que hacer.");

			return _operation == other._operation
				&& _milestone.IsTheSameMilestoneAs(other._milestone);
		}

		/// <summary>
		/// El NOMBRE del miembro de la operacion, que es la mitad direccionable de la clave: quien
		/// la lea puede volver a preguntar por esa operacion con el.
		/// </summary>
		internal string TheNameOfItsOperation()
		{
			return _operation.Name;
		}

		/// <summary>
		/// El hito dicho, con la unidad en que se publico. No hay forma direccionable del hito y no
		/// hace falta: quien recibe la enumeracion ya no tiene que nombrarlo.
		/// </summary>
		internal string TheMilestoneAsPublished()
		{
			return _milestone.Describe();
		}
	}
}
