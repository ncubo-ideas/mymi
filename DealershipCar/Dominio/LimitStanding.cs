using System;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// Donde esta parado uno de los dos limites de un intervalo: si esa dimension aplica a la ley,
	/// si se puede evaluar con los hechos que hay, y que estado implica por si sola. Es una
	/// respuesta calculada en cada evaluacion, nunca estado guardado (GAP G4).
	/// </summary>
	internal abstract class LimitStanding
	{
		private static readonly LimitStanding OutsideTheLaw = new ADimensionOutsideTheLaw();
		private static readonly LimitStanding NoUsableReadingToCountFrom =
			new ADimensionSuspendedByAnAbsentReading();

		/// <summary>
		/// La dimension de tiempo: dias desde el ancla hasta el instante evaluado, contra el limite
		/// ya escalado. Siempre se puede evaluar.
		/// </summary>
		internal static LimitStanding OfTime(
			DateTime anchor, DateTime asOf, int declaredTimeLimitDays, IntervalLengthFactor factor)
		{
			int effectiveLimit = factor.ApplyToTimeLimitDays(declaredTimeLimitDays);
			int elapsedDays = (int)((asOf.Ticks - anchor.Ticks) / TimeSpan.TicksPerDay);

			return new AMeasuredTimeDimension(
				StateFor(elapsedDays, effectiveLimit,
					SchedulePolicy.DueSoonLeadDays, SchedulePolicy.OverdueGraceDays),
				elapsedDays,
				effectiveLimit);
		}

		/// <summary>
		/// La dimension de distancia: lo recorrido desde el ancla hasta la ultima lectura usable,
		/// contra el limite ya escalado. Aqui se decide si se puede evaluar (GAP G2), y el motivo
		/// se lee del tipo que sale.
		/// </summary>
		internal static LimitStanding OfDistance(
			BasePoint from,
			UsableReading latestUsable,
			DistanceUnit limitUnit,
			int declaredDistanceLimit,
			IntervalLengthFactor factor)
		{
			if (factor.DistanceIsIgnored)
				return new ADimensionSuspendedByTheUsageProfile(factor.Profile.Name);

			if (!from.HasReading || !latestUsable.Exists())
				return NoUsableReadingToCountFrom;

			OdometerReading reading = latestUsable.Reading();

			if (!from.MeasuresDistanceIn(limitUnit))
				return new ADimensionSuspendedByAUnitMismatch(limitUnit, from.Unit);

			if (!reading.IsExpressedIn(limitUnit))
				return new ADimensionSuspendedByAUnitMismatch(limitUnit, reading.Unit);

			if (reading.Value < from.Reading)
				throw new DomainException(
					$"La ultima lectura usable es {reading.Value} y el punto base es "
					+ $"{from.Reading}, en la misma unidad: el odometro fue para atras. Eso no es "
					+ "una dimension suspendida, es un hecho imposible, y cual de los dos esta "
					+ "mal lo decide quien ordena las lecturas, no la regla del intervalo.");

			int effectiveLimit = factor.ApplyToDistanceLimit(declaredDistanceLimit);
			int travelled = reading.Value - from.Reading;

			return new AMeasuredDistanceDimension(
				StateFor(travelled, effectiveLimit,
					SchedulePolicy.DueSoonLeadDistance, SchedulePolicy.OverdueGraceDistance),
				travelled,
				effectiveLimit,
				limitUnit);
		}

		/// <summary>La ley no mide tiempo: DistanceOnly y ConditionOnly.</summary>
		internal static LimitStanding TimeIsNotPartOfThisLaw()
		{
			return OutsideTheLaw;
		}

		/// <summary>La ley no mide distancia: TimeOnly y ConditionOnly.</summary>
		internal static LimitStanding DistanceIsNotPartOfThisLaw()
		{
			return OutsideTheLaw;
		}

		/// <summary>
		/// Si esta dimension existe en la ley evaluada. Ausente y suspendida no son lo mismo: lo
		/// que no existe no se puede curar.
		/// </summary>
		internal abstract bool Applies { get; }

		/// <summary>
		/// Si se puede evaluar con los hechos que hay. Falso es NO SE PUEDE SABER, distinto de
		/// <see cref="DueState.NotDue"/> (GAP G2).
		/// </summary>
		internal virtual bool CanBeEvaluated
		{
			get { return false; }
		}

		/// <summary>
		/// El estado que esta sola dimension implica. Revienta si no se pudo medir (E41): no hay
		/// un octavo <see cref="DueState"/> que diga "no lo se".
		/// </summary>
		internal virtual DueState State
		{
			get
			{
				throw new DomainException(
					"Esta dimension no se pudo evaluar, asi que no tiene estado: pregunte "
					+ "CanBeEvaluated antes. Un NotDue devuelto aqui se leeria como que la "
					+ "operacion esta al dia, que es exactamente lo que el GAP G2 impide.");
			}
		}

		/// <summary>Suspendida porque el limite y las mediciones estan en unidades distintas.</summary>
		internal virtual bool SuspendedByUnitMismatch
		{
			get { return false; }
		}

		/// <summary>Suspendida porque no hay de donde contar.</summary>
		internal virtual bool SuspendedByAnAbsentReading
		{
			get { return false; }
		}

		/// <summary>Suspendida porque el perfil de uso ignora los limites de distancia.</summary>
		internal virtual bool SuspendedByTheUsageProfile
		{
			get { return false; }
		}

		/// <summary>
		/// La clausula que esta dimension aporta al motivo: la dimension y el monto, o por que no
		/// se pudo evaluar. Componerla es del dominio, no de la vista (OUS-07).
		/// </summary>
		internal abstract string Describe();

		/// <summary>
		/// Si llego mas lejos que la otra. Con ella se dice la ley L1: decide la mas avanzada.
		/// </summary>
		internal bool IsMoreAdvancedThan(LimitStanding other)
		{
			return Advancement() > other.Advancement();
		}

		/// <summary>
		/// Si las dos llegaron igual de lejos. Entonces deciden las dos y el motivo nombra las dos.
		/// </summary>
		internal bool IsAsAdvancedAs(LimitStanding other)
		{
			return Advancement() == other.Advancement();
		}

		/// <summary>
		/// El rango de avance, solo para comparar dos dimensiones. Protegido para que nadie ordene
		/// DueStates por el. Revienta si la dimension no se pudo medir (GAP G2).
		/// </summary>
		protected virtual int Advancement()
		{
			throw new DomainException(
				"Se comparo el avance de una dimension que no se puede evaluar. Lo inevaluable no "
				+ "va en la misma escalera que lo medido: primero se pregunta CanBeEvaluated y se "
				+ "degrada (GAP G2).");
		}

		private static DueState StateFor(int travelled, int effectiveLimit, int courtesyLead, int grace)
		{
			if (travelled >= effectiveLimit + grace) return DueState.Overdue;
			if (travelled >= effectiveLimit) return DueState.Due;
			if (travelled >= effectiveLimit - courtesyLead) return DueState.DueSoon;

			return DueState.NotDue;
		}
	}
}
