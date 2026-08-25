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
		/// Levanta la ley con sus dos limites y la unidad del limite de distancia. Los dos
		/// limites son obligatorios y quien lo hace cumplir es la guarda
		/// <see cref="IntervalRule.BasisIsNeverInferred"/>, antes de llegar aqui: con una sola
		/// dimension no hay dos que comparar y la ley "la primera que llegue" no significa nada.
		/// </summary>
		internal TimeOrDistance(int timeLimitDays, int distanceLimit, DistanceUnit limitUnit)
		{
			_timeLimitDays = timeLimitDays;
			_distanceLimit = distanceLimit;
			_limitUnit = limitUnit;
		}

		/// <summary>
		/// Donde queda parada la operacion. Delega en <see cref="WhicheverLimitArrivesFirst"/>,
		/// que es donde la ley L1 tiene su nombre.
		/// </summary>
		internal override DueAssessment Assess(
			BasePoint from, DateTime asOf, OdometerReading latestUsable, IntervalLengthFactor factor)
		{
			return WhicheverLimitArrivesFirst(from, asOf, latestUsable, factor);
		}

		/// <summary>
		/// LEY L1 (§5 del slice, OINV-20): "La operacion vence en el PRIMERO de sus dos
		/// limites. Ninguna dimension domina; la que llegue primero decide."
		/// <para>
		/// Llegar primero es haber llegado MAS LEJOS en su propia dimension, asi que decide la
		/// mas avanzada de las dos; si empataron, decidieron las dos y el motivo nombra las dos.
		/// </para>
		/// <para>
		/// GAP G2: si la distancia NO SE PUEDE EVALUAR, la operacion degrada al tiempo, que es
		/// la dimension que todavia se puede evaluar. Lo inevaluable no entra en la comparacion
		/// como si fuera "todavia no llego" -- eso seria dejar que una ausencia de informacion
		/// compita con una medicion.
		/// </para>
		/// </summary>
		internal DueAssessment WhicheverLimitArrivesFirst(
			BasePoint from, DateTime asOf, OdometerReading latestUsable, IntervalLengthFactor factor)
		{
			RequireAnAnchorAndAFactor(from, asOf, factor);

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
		/// LEY L4 para esta base: el ancla nueva son el instante y la lectura REALES de la
		/// completacion, las dos, porque las dos dimensiones se miden desde ahi.
		/// </summary>
		internal override BasePoint NextOccurrenceFromActualCompletion(
			DateTime completedAt, int reading, DistanceUnit unit)
		{
			return BasePoint.At(completedAt, reading, unit);
		}
	}
}
