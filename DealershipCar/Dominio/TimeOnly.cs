using System;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// La ley de intervalo que vence por tiempo cumplido, sin dimension de distancia: un fluido
	/// que envejece, un componente con vida declarada en anos. No guarda limite de distancia ni
	/// unidad, asi que no puede evaluar una aunque alguien se la ofrezca.
	/// </summary>
	internal sealed class TimeOnly : IntervalRule
	{
		private readonly int _timeLimitDays;

		/// <summary>Su unico limite. Que no reciba unidad es la ley dicha en la forma del tipo.</summary>
		internal TimeOnly(int timeLimitDays)
		{
			_timeLimitDays = timeLimitDays;
		}

		/// <summary>
		/// Mira solo el tiempo transcurrido desde el ancla. La distancia no aplica -- no existe,
		/// no esta suspendida -- asi que el motivo no la nombra.
		/// </summary>
		internal override DueAssessment Assess(
			BasePoint from, DateTime asOf, UsableReading latestUsable, IntervalLengthFactor factor)
		{
			RequireAnAnchorAFactorAndAReading(from, asOf, latestUsable, factor);

			LimitStanding time = LimitStanding.OfTime(from.Instant, asOf, _timeLimitDays, factor);

			return DueAssessment.DecidedBy(
				time.State, time, LimitStanding.DistanceIsNotPartOfThisLaw(), true, false);
		}

		/// <summary>
		/// LEY L4: el ancla nueva es el instante real de la completacion y solo el. La lectura se
		/// descarta: guardarla haria creer que la distancia cuenta para esta operacion.
		/// </summary>
		internal override BasePoint NextOccurrenceFromActualCompletion(
			DateTime completedAt, int reading, DistanceUnit unit)
		{
			return BasePoint.AtInstantOnly(completedAt);
		}
	}
}
