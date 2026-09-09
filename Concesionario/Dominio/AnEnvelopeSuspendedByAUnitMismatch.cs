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
		private readonly DistanceUnit _limitUnit;
		private readonly DistanceUnit _odometerUnit;
		private readonly string _clause;

		/// <summary>
		/// La compone quien vio las dos unidades que no calzaron: la del sobre publicado y la del
		/// odometro recibido.
		/// </summary>
		internal AnEnvelopeSuspendedByAUnitMismatch(
			DistanceUnit limitUnit, DistanceUnit odometerUnit, string clause)
		{
			if (limitUnit == null || odometerUnit == null)
				throw new DomainException(
					"Se compuso un sobre suspendido sin nombrar las dos unidades que no coincidieron. "
					+ "Decir que no se pudo medir sin decir cual contra cual no le deja a nadie "
					+ "corregirlo.");

			_limitUnit = limitUnit;
			_odometerUnit = odometerUnit;
			_clause = clause;
		}

		/// <summary>Si: es esta clase de ausencia y no otra.</summary>
		internal override bool SuspendedByAUnitMismatch()
		{
			return true;
		}

		/// <summary>
		/// Nombra las dos unidades y dice que no se convierten. Sin las dos, quien lea la
		/// respuesta sabe que algo no calzo y no sabe que arreglar.
		/// </summary>
		internal override string Describe()
		{
			return TheAnswerSaying(
				"su mitad de distancia no se puede evaluar, porque el límite de distancia publicado "
				+ "está en " + _limitUnit.Name + " y el odómetro de este vehículo está en "
				+ _odometerUnit.Name + ", y las unidades nunca se convierten.",
				_clause);
		}
	}
}
