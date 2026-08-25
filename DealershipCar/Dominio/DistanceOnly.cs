using System;

namespace DealershipCar.Dominio
{
	// ES LA UNICA DE LAS CINCO QUE PUEDE QUEDARSE SIN NADA QUE EVALUAR: las dos de dos
	// dimensiones degradan al tiempo, TimeOnly siempre puede contar dias, ConditionOnly no mide
	// nada y lo sabe. Esta tiene exactamente una dimension y no hay a donde degradar.
	//
	// LA ALTERNATIVA RECHAZADA: un octavo DueState que dijera "no lo se". Seria vocabulario que
	// el canon no nombra, y ademas un estado que el agregado tendria que saber mover. La
	// respuesta elegida es NotDue con CanBeEvaluated en falso -- la inevaluabilidad dicha en voz
	// alta en vez de convertida en un estado.

	/// <summary>
	/// La ley de intervalo que vence por distancia recorrida, sin dimension de tiempo: piezas
	/// de desgaste que se gastan con el uso y no con el calendario.
	/// </summary>
	internal sealed class DistanceOnly : IntervalRule
	{
		private readonly int _distanceLimit;
		private readonly DistanceUnit _limitUnit;

		/// <summary>
		/// Levanta la ley con su unico limite y la unidad en que esta expresado. Que no reciba
		/// limite de tiempo es la ley dicha en la forma del tipo.
		/// </summary>
		internal DistanceOnly(int distanceLimit, DistanceUnit limitUnit)
		{
			_distanceLimit = distanceLimit;
			_limitUnit = limitUnit;
		}

		/// <summary>
		/// Donde queda parada la operacion, mirando solo la distancia recorrida desde el ancla.
		/// <para>
		/// GAP G2: si esa unica dimension no se puede evaluar, no hay a donde degradar, y la
		/// respuesta lo dice -- NotDue con <see cref="DueAssessment.CanBeEvaluated"/> en falso.
		/// Devolver un NotDue callado seria mentir con la verdad.
		/// </para>
		/// </summary>
		internal override DueAssessment Assess(
			BasePoint from, DateTime asOf, OdometerReading latestUsable, IntervalLengthFactor factor)
		{
			RequireAnAnchorAndAFactor(from, asOf, factor);

			LimitStanding time = LimitStanding.TimeIsNotPartOfThisLaw();
			LimitStanding distance = LimitStanding.OfDistance(
				from, latestUsable, _limitUnit, _distanceLimit, factor);

			if (!distance.CanBeEvaluated)
				return DueAssessment.NothingCouldBeEvaluated(time, distance);

			return DueAssessment.DecidedBy(distance.State, time, distance, false, true);
		}

		/// <summary>
		/// LEY L4 para esta base: el ancla nueva son el instante y la lectura REALES de la
		/// completacion. El instante viaja aunque ninguna ley de este tipo lo mida, porque un
		/// punto base siempre sabe cuando fue -- la mitad opcional es la lectura, no el
		/// instante.
		/// </summary>
		internal override BasePoint NextOccurrenceFromActualCompletion(
			DateTime completedAt, int reading, DistanceUnit unit)
		{
			return BasePoint.At(completedAt, reading, unit);
		}
	}
}
