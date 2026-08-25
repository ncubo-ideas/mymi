using System;

namespace DealershipCar.Dominio
{
	// OJO CON EL NOMBRE, Y NO SE CAMBIA: System.TimeOnly existe desde .NET 6, y desde CUALQUIER
	// namespace que no sea este, un TimeOnly a secas resuelve al del BCL sin error ninguno. El
	// canon manda sobre los nombres, asi que se resuelve con un alias en el sitio que lo nombra
	// y no renombrando la base. Detalle completo y el por que en CONTRADICTIONS.md C15.

	/// <summary>
	/// La ley de intervalo que vence por tiempo cumplido, sin dimension de distancia: un fluido
	/// que envejece, un componente con vida declarada en anos. No guarda limite de distancia ni
	/// unidad, asi que no puede evaluar una aunque alguien se la ofrezca.
	/// </summary>
	internal sealed class TimeOnly : IntervalRule
	{
		private readonly int _timeLimitDays;

		/// <summary>
		/// Levanta la ley con su unico limite. Que no reciba limite de distancia ni unidad no
		/// es una firma incompleta: es la ley dicha en la forma del tipo.
		/// </summary>
		internal TimeOnly(int timeLimitDays)
		{
			_timeLimitDays = timeLimitDays;
		}

		/// <summary>
		/// Donde queda parada la operacion, mirando solo el tiempo transcurrido desde el ancla.
		/// La dimension de distancia no aplica -- no esta suspendida: no existe -- y por eso el
		/// motivo no la nombra.
		/// </summary>
		internal override DueAssessment Assess(
			BasePoint from, DateTime asOf, OdometerReading latestUsable, IntervalLengthFactor factor)
		{
			RequireAnAnchorAndAFactor(from, asOf, factor);

			LimitStanding time = LimitStanding.OfTime(from.Instant, asOf, _timeLimitDays, factor);

			return DueAssessment.DecidedBy(
				time.State, time, LimitStanding.DistanceIsNotPartOfThisLaw(), true, false);
		}

		/// <summary>
		/// LEY L4 para esta base: el ancla nueva es el instante REAL de la completacion, y solo
		/// el. La lectura que venga se descarta a proposito -- ninguna ley de este tipo la lee,
		/// y guardarla haria creer que la distancia cuenta para esta operacion.
		/// </summary>
		internal override BasePoint NextOccurrenceFromActualCompletion(
			DateTime completedAt, int reading, DistanceUnit unit)
		{
			return BasePoint.AtInstantOnly(completedAt);
		}
	}
}
