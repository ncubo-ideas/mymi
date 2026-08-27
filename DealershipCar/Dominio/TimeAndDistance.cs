using System;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// La ley de intervalo que vence solo cuando se alcanzan LOS DOS limites. Alcanzar uno no
	/// es vencer (ley L2, OINV-20).
	/// </summary>
	internal sealed class TimeAndDistance : IntervalRule
	{
		private readonly int _timeLimitDays;
		private readonly int _distanceLimit;
		private readonly DistanceUnit _limitUnit;

		/// <summary>
		/// Los dos limites y la unidad del de distancia. Que los dos sean obligatorios lo hace
		/// cumplir <see cref="IntervalRule.BasisIsNeverInferred"/>, antes de llegar aqui.
		/// </summary>
		internal TimeAndDistance(int timeLimitDays, int distanceLimit, DistanceUnit limitUnit)
		{
			_timeLimitDays = timeLimitDays;
			_distanceLimit = distanceLimit;
			_limitUnit = limitUnit;
		}

		/// <summary>Delega en <see cref="BothLimitsRequired"/>, donde la ley L2 tiene su nombre.</summary>
		internal override DueAssessment Assess(
			BasePoint from, DateTime asOf, UsableReading latestUsable, IntervalLengthFactor factor)
		{
			return BothLimitsRequired(from, asOf, latestUsable, factor);
		}

		/// <summary>
		/// LEY L2 (OINV-20): la operacion vence solo cuando se alcanzan LOS DOS limites; alcanzar
		/// uno no es vencer. Manda la menos avanzada, que es la que la retiene. Si la distancia no
		/// se puede evaluar degrada al tiempo, porque contarla como "no llego" dejaria la
		/// operacion NotDue para siempre (GAP G2).
		/// </summary>
		internal DueAssessment BothLimitsRequired(
			BasePoint from, DateTime asOf, UsableReading latestUsable, IntervalLengthFactor factor)
		{
			RequireAnAnchorAFactorAndAReading(from, asOf, latestUsable, factor);

			LimitStanding time = LimitStanding.OfTime(from.Instant, asOf, _timeLimitDays, factor);
			LimitStanding distance = LimitStanding.OfDistance(
				from, latestUsable, _limitUnit, _distanceLimit, factor);

			if (!distance.CanBeEvaluated)
				return DueAssessment.DecidedBy(time.State, time, distance, true, false);

			if (time.IsAsAdvancedAs(distance))
				return DueAssessment.DecidedBy(time.State, time, distance, true, true);

			if (time.IsMoreAdvancedThan(distance))
				return DueAssessment.DecidedBy(distance.State, time, distance, false, true);

			return DueAssessment.DecidedBy(time.State, time, distance, true, false);
		}

		/// <summary>
		/// LEY L4: el ancla nueva son el instante y la lectura reales de la completacion, las dos,
		/// porque las dos dimensiones se miden desde ahi.
		/// </summary>
		internal override BasePoint NextOccurrenceFromActualCompletion(
			DateTime completedAt, int reading, DistanceUnit unit)
		{
			return BasePoint.At(completedAt, reading, unit);
		}
	}
}
