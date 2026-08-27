using System;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// La ley de intervalo que vence por distancia recorrida, sin dimension de tiempo: piezas
	/// de desgaste que se gastan con el uso y no con el calendario.
	/// </summary>
	internal sealed class DistanceOnly : IntervalRule
	{
		private readonly int _distanceLimit;
		private readonly DistanceUnit _limitUnit;

		/// <summary>
		/// Su unico limite y la unidad en que esta expresado. Que no reciba limite de tiempo es la
		/// ley dicha en la forma del tipo.
		/// </summary>
		internal DistanceOnly(int distanceLimit, DistanceUnit limitUnit)
		{
			_distanceLimit = distanceLimit;
			_limitUnit = limitUnit;
		}

		/// <summary>
		/// Mira solo la distancia recorrida desde el ancla. Es la unica base sin a donde degradar:
		/// si su dimension no se puede evaluar contesta NotDue con
		/// <see cref="DueAssessment.CanBeEvaluated"/> en falso.
		/// </summary>
		internal override DueAssessment Assess(
			BasePoint from, DateTime asOf, UsableReading latestUsable, IntervalLengthFactor factor)
		{
			RequireAnAnchorAFactorAndAReading(from, asOf, latestUsable, factor);

			LimitStanding time = LimitStanding.TimeIsNotPartOfThisLaw();
			LimitStanding distance = LimitStanding.OfDistance(
				from, latestUsable, _limitUnit, _distanceLimit, factor);

			if (!distance.CanBeEvaluated)
				return DueAssessment.NothingCouldBeEvaluated(time, distance);

			return DueAssessment.DecidedBy(distance.State, time, distance, false, true);
		}

		/// <summary>
		/// El ancla nueva son el instante y la lectura reales de la completacion. El
		/// instante viaja aunque esta ley no lo mida: la mitad opcional es la lectura, no el
		/// instante.
		/// </summary>
		internal override BasePoint NextOccurrenceFromActualCompletion(
			DateTime completedAt, int reading, DistanceUnit unit)
		{
			return BasePoint.At(completedAt, reading, unit);
		}
	}
}
