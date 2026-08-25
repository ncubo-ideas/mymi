using System;

namespace DealershipCar.Dominio
{
	// ===== AQUI ES DONDE EL GAP G2 MUERDE DE VERDAD ===================================
	//
	// Si una dimension inevaluable contara como "todavia no llego", la menos avanzada de las
	// dos seria SIEMPRE la inevaluable y la operacion quedaria NotDue PARA SIEMPRE -- no un
	// rato: mientras la unidad no coincida, ninguna cantidad de tiempo la mueve.
	//
	// DEGRADAR HACE VENCER ANTES de lo que "los dos limites" pediria, y se eligio a proposito:
	// avisar de mas es una molestia, callar es un motor roto.
	//
	// ==================================================================================

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
		/// Levanta la ley con sus dos limites y la unidad del limite de distancia. Los dos son
		/// obligatorios y lo hace cumplir <see cref="IntervalRule.BasisIsNeverInferred"/> antes
		/// de llegar aqui: faltando uno, "los dos limites" nunca se cumpliria.
		/// </summary>
		internal TimeAndDistance(int timeLimitDays, int distanceLimit, DistanceUnit limitUnit)
		{
			_timeLimitDays = timeLimitDays;
			_distanceLimit = distanceLimit;
			_limitUnit = limitUnit;
		}

		/// <summary>
		/// Donde queda parada la operacion. Delega en <see cref="BothLimitsRequired"/>, que es
		/// donde la ley L2 tiene su nombre.
		/// </summary>
		internal override DueAssessment Assess(
			BasePoint from, DateTime asOf, OdometerReading latestUsable, IntervalLengthFactor factor)
		{
			return BothLimitsRequired(from, asOf, latestUsable, factor);
		}

		/// <summary>
		/// LEY L2 (§5 del slice, OINV-20): "La operacion vence solo cuando se alcanzan LOS DOS
		/// limites. Alcanzar uno no es vencer."
		/// <para>
		/// Manda la dimension MENOS avanzada, que es la que retiene la operacion; si empataron,
		/// decidieron las dos.
		/// </para>
		/// <para>
		/// GAP G2: si la distancia no se puede evaluar, NO cuenta como "todavia no llego" --
		/// eso dejaria la operacion NotDue para siempre. Degrada al tiempo y lo dice.
		/// </para>
		/// </summary>
		internal DueAssessment BothLimitsRequired(
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
				return DueAssessment.DecidedBy(distance.State, time, distance, false, true);

			return DueAssessment.DecidedBy(time.State, time, distance, true, false);
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
