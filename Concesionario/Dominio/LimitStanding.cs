using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// Donde esta parado uno de los dos limites de un intervalo: si esa dimension aplica a la ley,
	/// si se puede evaluar con los hechos que hay, y que estado implica por si sola. Es una
	/// respuesta calculada en cada evaluacion, nunca estado guardado.
	/// </summary>
	internal abstract class LimitStanding
	{
		private static readonly LimitStanding TimeOutsideTheLaw =
			new ADimensionOutsideTheLaw(IntervalDimension.Time);
		private static readonly LimitStanding DistanceOutsideTheLaw =
			new ADimensionOutsideTheLaw(IntervalDimension.Distance);
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
		/// contra el limite ya escalado. Aqui se decide si se puede evaluar, y el motivo
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

		/// <summary>
		/// La ley no mide tiempo: DistanceOnly y ConditionOnly. Es su propia instancia y no la
		/// comparte con la de distancia: <see cref="WhatRemains"/> tiene que poder decir CUAL
		/// dimension es la que la ley no tiene.
		/// </summary>
		internal static LimitStanding TimeIsNotPartOfThisLaw()
		{
			return TimeOutsideTheLaw;
		}

		/// <summary>La ley no mide distancia: TimeOnly y ConditionOnly.</summary>
		internal static LimitStanding DistanceIsNotPartOfThisLaw()
		{
			return DistanceOutsideTheLaw;
		}

		/// <summary>
		/// Si esta dimension existe en la ley evaluada. Ausente y suspendida no son lo mismo: lo
		/// que no existe no se puede curar.
		/// </summary>
		internal abstract bool Applies { get; }

		/// <summary>
		/// Si se puede evaluar con los hechos que hay. Falso es NO SE PUEDE SABER, distinto de
		/// <see cref="DueState.NotDue"/>.
		/// </summary>
		internal virtual bool CanBeEvaluated
		{
			get { return false; }
		}

		/// <summary>
		/// El estado que esta sola dimension implica. Revienta si no se pudo medir: no hay
		/// un octavo <see cref="DueState"/> que diga "no lo se".
		/// </summary>
		internal virtual DueState State
		{
			get
			{
				throw new DomainException(
					"Esta dimension no se pudo evaluar, asi que no tiene estado: pregunte "
					+ "CanBeEvaluated antes. Un NotDue devuelto aqui se leeria como que la "
					+ "operacion esta al dia, que es exactamente lo que no se puede decir.");
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
		/// QUE CLAUSULA aporta esta dimension al motivo, por su codigo, o vacia donde no aporta
		/// ninguna. Elegirla es del dominio; decirla es de quien la muestra.
		/// </summary>
		internal abstract string TheClauseItAdds();

		/// <summary>La dimension de la clausula, por su codigo. TOTAL.</summary>
		internal abstract string TheDimensionItMeasures();

		/// <summary>
		/// Cuanto lleva contado esta dimension desde el ancla, o cero donde la clausula no cuenta
		/// ninguno. TOTAL.
		/// </summary>
		internal virtual int TheAmountItReached()
		{
			return 0;
		}

		/// <summary>
		/// El limite ya escalado contra el que se cuenta, o cero donde la clausula no nombra
		/// ninguno. TOTAL.
		/// </summary>
		internal virtual int TheLimitItIsAgainst()
		{
			return 0;
		}

		/// <summary>
		/// La unidad del limite, por su nombre, o vacia donde la clausula no nombra ninguna. TOTAL.
		/// </summary>
		internal virtual string TheUnitOfTheLimit()
		{
			return "";
		}

		/// <summary>
		/// La unidad en que estan las mediciones cuando no coincide con la del limite, o vacia donde
		/// coinciden. TOTAL.
		/// </summary>
		internal virtual string TheUnitTheMeasurementsAreIn()
		{
			return "";
		}

		/// <summary>
		/// El perfil de uso que ignora esta dimension, por su codigo, o vacio donde no la ignora
		/// ninguno. TOTAL.
		/// </summary>
		internal virtual string TheUsageProfileThatIgnoresIt()
		{
			return "";
		}

		/// <summary>
		/// Lo que le queda a esta dimension: el monto compuesto con su unidad, o cual razon impide
		/// decirlo. La resta se escribe UNA vez, en <see cref="AMeasuredDimension"/>, y el signo
		/// elige la variante.
		/// </summary>
		internal abstract ARemainder WhatRemains();

		/// <summary>
		/// Si llego mas lejos que la otra. Con ella se dice la ley: decide la mas avanzada. La otra
		/// dimension es obligatoria: sin ella no hay cual decide.
		/// </summary>
		internal bool IsMoreAdvancedThan(LimitStanding other)
		{
			if (other == null)
				throw new DomainException(
					"Se comparo el avance de una dimension contra ninguna. Sin la otra no hay cual "
					+ "decide, y comparar contra nada dejaria la ley decidida por el hueco.");

			return Advancement() > other.Advancement();
		}

		/// <summary>
		/// Si las dos llegaron igual de lejos. Entonces deciden las dos y el motivo nombra las dos.
		/// La otra dimension es obligatoria, por lo mismo que en <see cref="IsMoreAdvancedThan"/>.
		/// </summary>
		internal bool IsAsAdvancedAs(LimitStanding other)
		{
			if (other == null)
				throw new DomainException(
					"Se pregunto si dos dimensiones llegaron igual de lejos sin decir la segunda. "
					+ "Comparar contra nada dejaria la ley decidida por el hueco.");

			return Advancement() == other.Advancement();
		}

		/// <summary>
		/// El rango de avance, solo para comparar dos dimensiones. Protegido para que nadie ordene
		/// DueStates por el. Revienta si la dimension no se pudo medir.
		/// </summary>
		protected virtual int Advancement()
		{
			throw new DomainException(
				"Se comparo el avance de una dimension que no se puede evaluar. Lo inevaluable no "
				+ "va en la misma escalera que lo medido: primero se pregunta CanBeEvaluated y se "
				+ "degrada.");
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
