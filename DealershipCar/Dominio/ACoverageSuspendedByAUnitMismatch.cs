using System;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// La respuesta de cobertura que no se pudo decidir porque el limite que rige y el odometro
	/// del vehiculo estan en unidades distintas. No se convierte: se deja de comparar y se dice
	/// cual contra cual. El motivo esta en el NOMBRE del tipo y no en un campo, para que quien
	/// busque de donde sale este caso lo encuentre.
	/// </summary>
	internal sealed class ACoverageSuspendedByAUnitMismatch : CoverageAnswer
	{
		private readonly string _subject;
		private readonly DistanceUnit _limitUnit;
		private readonly DistanceUnit _odometerUnit;
		private readonly string _clause;

		/// <summary>
		/// La compone <see cref="WarrantyCoverage"/> con las dos unidades que no calzaron: la del
		/// limite que rige y la del odometro recibido.
		/// </summary>
		internal ACoverageSuspendedByAUnitMismatch(
			string subject, DistanceUnit limitUnit, DistanceUnit odometerUnit, string clause)
		{
			if (string.IsNullOrWhiteSpace(subject))
				throw new DomainException(
					"Se compuso una cobertura suspendida sin decir de que sistema habla.");

			if (limitUnit == null || odometerUnit == null)
				throw new DomainException(
					$"Se compuso una cobertura suspendida sobre {subject} sin nombrar las dos "
					+ "unidades que no coincidieron. Decir que no se pudo medir sin decir cual contra "
					+ "cual no le deja a nadie corregirlo.");

			_subject = subject;
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
		/// Nombra las dos unidades y dice que no se convierten. La frase contesta aunque la
		/// pregunta no: callarlo dejaria a la persona creyendo que le contestaron.
		/// </summary>
		internal override string Describe()
		{
			return _subject + ": cannot be evaluated, because the governing distance limit is in "
				+ _limitUnit.Name + " and this vehicle's odometer is in " + _odometerUnit.Name
				+ ", and units are never converted. " + _clause;
		}
	}
}
