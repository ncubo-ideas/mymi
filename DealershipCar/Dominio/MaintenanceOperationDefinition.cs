using System;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// La regla inmutable de una clase de operacion de mantenimiento: que es, cuando vence, si el
	/// plan la manda y si esta clasificada de seguridad. Es la DEFINICION; la ocurrencia que
	/// la vive es <see cref="ScheduledOperation"/>.
	/// </summary>
	internal sealed class MaintenanceOperationDefinition
	{
		private readonly IntervalRule _rule;

		/// <summary>
		/// La clase, la ley de su intervalo y sus dos clasificaciones. Los tres primeros son
		/// obligatorios: sin ley de vencimiento no hay operacion.
		/// </summary>
		internal MaintenanceOperationDefinition(
			MaintenanceOperationKind kind, IntervalRule rule, bool isMandatory, bool isSafetyClassified)
		{
			if (kind == null)
				throw new DomainException(
					"Una definicion de operacion sin clase no dice QUE hay que hacer.");

			if (rule == null)
				throw new DomainException(
					"Una definicion de operacion sin regla de intervalo no vence nunca, y no hay "
					+ "operacion sin regla de cuando vence.");

			Kind = kind;
			_rule = rule;
			IsMandatory = isMandatory;
			IsSafetyClassified = isSafetyClassified;
		}

		/// <summary>Que operacion es.</summary>
		internal MaintenanceOperationKind Kind { get; }

		/// <summary>Si el plan del fabricante la manda o solo la recomienda.</summary>
		internal bool IsMandatory { get; }

		/// <summary>
		/// Si esta clasificada de seguridad. Una de seguridad no se aplaza nunca.
		/// </summary>
		internal bool IsSafetyClassified { get; }

		/// <summary>
		/// Donde esta parada una ocurrencia suya, segun su ley de intervalo. La definicion no lee
		/// un reloj: el instante se recibe.
		/// </summary>
		internal DueAssessment Assess(
			BasePoint from, DateTime asOf, UsableReading latestUsable, IntervalLengthFactor factor)
		{
			return _rule.Assess(from, asOf, latestUsable, factor);
		}

		/// <summary>
		/// Donde queda el ancla de la proxima ocurrencia, medida desde la completacion
		/// real. Cada base ancla lo que su ley mide.
		/// </summary>
		internal BasePoint NextOccurrenceFromActualCompletion(
			DateTime completedAt, int reading, DistanceUnit unit)
		{
			return _rule.NextOccurrenceFromActualCompletion(completedAt, reading, unit);
		}
	}
}
