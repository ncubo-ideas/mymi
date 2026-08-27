using System;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// La ley de intervalo que vence con la PRIMERA de sus dos dimensiones que llegue: doce
	/// meses o diez mil unidades, lo que ocurra antes. Ninguna de las dos domina (ley L1,
	/// OINV-20).
	/// </summary>
	internal sealed class TimeOrDistance : IntervalRule
	{
		private readonly int _timeLimitDays;
		private readonly int _distanceLimit;
		private readonly DistanceUnit _limitUnit;

		/// <summary>
		/// Los dos limites y la unidad del de distancia. Que los dos sean obligatorios lo hace
		/// cumplir <see cref="IntervalRule.BasisIsNeverInferred"/>, antes de llegar aqui.
		/// </summary>
		internal TimeOrDistance(int timeLimitDays, int distanceLimit, DistanceUnit limitUnit)
		{
			_timeLimitDays = timeLimitDays;
			_distanceLimit = distanceLimit;
			_limitUnit = limitUnit;
		}

		/// <summary>Delega en <see cref="WhicheverLimitArrivesFirst"/>, donde la ley L1 tiene su nombre.</summary>
		internal override DueAssessment Assess(
			BasePoint from, DateTime asOf, UsableReading latestUsable, IntervalLengthFactor factor)
		{
			return WhicheverLimitArrivesFirst(from, asOf, latestUsable, factor);
		}

		/// <summary>
		/// LEY L1 (OINV-20): la operacion vence en el PRIMERO de sus dos limites; ninguna
		/// dimension domina. Decide la mas avanzada, y si empataron deciden las dos. Si la
		/// distancia no se puede evaluar, degrada al tiempo (GAP G2).
		/// </summary>
		internal DueAssessment WhicheverLimitArrivesFirst(
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
				return DueAssessment.DecidedBy(time.State, time, distance, true, false);

			return DueAssessment.DecidedBy(distance.State, time, distance, false, true);
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
