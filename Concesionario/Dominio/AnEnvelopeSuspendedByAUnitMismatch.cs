using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// La respuesta de sobre que no se pudo decidir porque la distancia del sobre y el odometro
	/// del vehiculo estan en unidades distintas. No se convierte: se deja de comparar y se dice
	/// cual contra cual. El motivo esta en el NOMBRE del tipo y no en un campo, para que quien
	/// busque de donde sale este caso lo encuentre.
	/// </summary>
	internal sealed class AnEnvelopeSuspendedByAUnitMismatch : EnvelopeAnswer
	{
		private readonly DistanceUnit _odometerUnit;

		/// <summary>
		/// La compone quien vio las dos unidades que no calzaron: la del sobre publicado, que la
		/// clausula ya cita, y la del odometro recibido.
		/// </summary>
		internal AnEnvelopeSuspendedByAUnitMismatch(
			DistanceUnit limitUnit, DistanceUnit odometerUnit, AnEnvelopeClause clause)
			: base(clause)
		{
			if (limitUnit == null || odometerUnit == null)
				throw new DomainException(
					"Se compuso un sobre suspendido sin nombrar las dos unidades que no coincidieron. "
					+ "Decir que no se pudo medir sin decir cual contra cual no le deja a nadie "
					+ "corregirlo.");

			_odometerUnit = odometerUnit;
		}

		/// <summary>Si: es esta clase de ausencia y no otra.</summary>
		internal override bool SuspendedByAUnitMismatch()
		{
			return true;
		}

		/// <summary>Suspendida por unidades, que es la que nombra las dos.</summary>
		internal override string TheShapeItHas()
		{
			return UnitMismatch;
		}

		/// <summary>
		/// La unidad que no coincidio, por su nombre. Sin ella, quien lea la respuesta sabe que algo
		/// no calzo y no sabe que arreglar; la del limite la cita la clausula.
		/// </summary>
		internal override string TheUnitThatDidNotMatch()
		{
			return _odometerUnit.Name;
		}
	}
}
