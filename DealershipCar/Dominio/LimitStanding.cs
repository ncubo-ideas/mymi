using System;

namespace DealershipCar.Dominio
{
	// POR QUE EXISTE ESTE TIPO, que el plan (§3.3) no lista. Un intervalo tiene a lo sumo DOS
	// dimensiones y las cinco bases se diferencian en COMO COMBINAN las dos, no en como se mide
	// cada una. Sin un lugar para "una dimension", la misma cuenta se copia en tres subtipos y
	// el GAP G2 no tiene donde aterrizar. Con el, cada ley queda en una linea que se lee como la
	// ley: la mas avanzada de las dos, o la menos avanzada de las dos.
	//
	// LA ALTERNATIVA RECHAZADA, y es la que el GAP G2 nombra: un solo booleano donde hoy hay
	// dos. "No llego" es informacion -- se midio y falta. "No se puede evaluar" es la ausencia
	// de informacion. Colapsarlas hace que una operacion se vea al dia para siempre, y con la
	// ley L2 literalmente para siempre, porque el minimo de las dos dimensiones nunca sube.
	//
	// El limite es INCLUSIVO, y los otros dos umbrales tambien, por la misma regla: alcanzar un
	// umbral es entrar en su estado. Esa extension es una lectura y no un hecho -- OINV-24 se
	// puede leer al reves. Esta anotada en CONTRADICTIONS.md C16 con lo que costaria cambiarla.

	/// <summary>
	/// Donde esta parado uno de los dos limites de un intervalo: si esa dimension aplica a la
	/// ley, si se puede evaluar con los hechos que hay, cuanto se lleva recorrido de ella y que
	/// estado implica por si sola. Es una respuesta calculada en cada evaluacion y nunca estado
	/// guardado (GAP G4).
	/// </summary>
	internal sealed class LimitStanding
	{
		private readonly string _dimension;
		private readonly DistanceUnit _limitUnit;
		private readonly DistanceUnit _mismatchedUnit;
		private readonly string _profileName;

		private LimitStanding(
			string dimension,
			bool applies,
			bool canBeEvaluated,
			DueState state,
			int travelled,
			int effectiveLimit,
			DistanceUnit limitUnit,
			DistanceUnit mismatchedUnit,
			string profileName)
		{
			_dimension = dimension;
			_limitUnit = limitUnit;
			_mismatchedUnit = mismatchedUnit;
			_profileName = profileName;

			Applies = applies;
			CanBeEvaluated = canBeEvaluated;
			State = state;
			Travelled = travelled;
			EffectiveLimit = effectiveLimit;
			SuspendedByUnitMismatch = mismatchedUnit != null;
			SuspendedByAnAbsentReading = applies && !canBeEvaluated && mismatchedUnit == null
				&& profileName == null;
			SuspendedByTheUsageProfile = profileName != null;
		}

		/// <summary>
		/// Si esta dimension existe en la ley que se esta evaluando. TimeOnly no tiene
		/// dimension de distancia y ConditionOnly no tiene ninguna de las dos. Ausente y
		/// suspendida no son lo mismo: lo que no existe no se puede curar.
		/// </summary>
		internal bool Applies { get; }

		/// <summary>
		/// Si esta dimension se puede evaluar con los hechos que hay. Falso significa NO SE
		/// PUEDE SABER, que es distinto de <see cref="DueState.NotDue"/> -- y confundirlos es
		/// lo que hace que una operacion se vea al dia para siempre (GAP G2).
		/// </summary>
		internal bool CanBeEvaluated { get; }

		/// <summary>
		/// El estado que esta sola dimension implica: NotDue, DueSoon, Due u Overdue. Solo
		/// tiene sentido cuando <see cref="CanBeEvaluated"/> es verdadero.
		/// </summary>
		internal DueState State { get; }

		/// <summary>
		/// Cuanto se lleva recorrido de esta dimension desde el ancla: dias transcurridos, o
		/// distancia recorrida en la unidad del limite.
		/// </summary>
		internal int Travelled { get; }

		/// <summary>
		/// El limite de esta dimension despues de aplicar el escalamiento del perfil de uso
		/// (ley L8). Es contra este numero que se compara <see cref="Travelled"/>.
		/// </summary>
		internal int EffectiveLimit { get; }

		/// <summary>
		/// Suspendida porque el limite y las mediciones estan en unidades distintas (OINV-21,
		/// ley L7). No se convierte: se deja de comparar y se dice.
		/// </summary>
		internal bool SuspendedByUnitMismatch { get; }

		/// <summary>
		/// Suspendida porque no hay de donde contar: el ancla no tiene lectura, o no hay
		/// ninguna lectura usable todavia.
		/// </summary>
		internal bool SuspendedByAnAbsentReading { get; }

		/// <summary>
		/// Suspendida porque el perfil de uso declarado IGNORA los limites de distancia (§6,
		/// LowDistance). Es una decision declarada, no una falta de datos.
		/// </summary>
		internal bool SuspendedByTheUsageProfile { get; }

		/// <summary>
		/// La dimension de tiempo: cuantos dias transcurrieron desde el ancla hasta el instante
		/// que se evalua, contra el limite de tiempo ya escalado por el perfil de uso.
		/// </summary>
		internal static LimitStanding OfTime(
			DateTime anchor, DateTime asOf, int declaredTimeLimitDays, IntervalLengthFactor factor)
		{
			int effectiveLimit = factor.ApplyToTimeLimitDays(declaredTimeLimitDays);
			int elapsedDays = (int)((asOf.Ticks - anchor.Ticks) / TimeSpan.TicksPerDay);

			return new LimitStanding(
				"time",
				true,
				true,
				StateFor(elapsedDays, effectiveLimit,
					SchedulePolicy.DueSoonLeadDays, SchedulePolicy.OverdueGraceDays),
				elapsedDays,
				effectiveLimit,
				null,
				null,
				null);
		}

		/// <summary>
		/// La dimension de distancia: cuanta se recorrio desde el ancla hasta la ultima lectura
		/// usable, contra el limite ya escalado. Aqui se decide si la dimension SE PUEDE
		/// evaluar (GAP G2) -- el perfil la ignora, falta la lectura, o las unidades no
		/// coinciden -- y la decision se toma de nuevo en cada evaluacion (GAP G4).
		/// </summary>
		internal static LimitStanding OfDistance(
			BasePoint from,
			OdometerReading latestUsable,
			DistanceUnit limitUnit,
			int declaredDistanceLimit,
			IntervalLengthFactor factor)
		{
			if (factor.DistanceIsIgnored)
				return Suspended(limitUnit, null, factor.Profile.Name);

			if (!from.HasReading || latestUsable == null)
				return Suspended(limitUnit, null, null);

			// Las tres unidades en juego son la del limite, la del ancla y la de la lectura.
			// Cualquier par que no coincida deja la comparacion sin sentido, y la respuesta es
			// dejar de comparar -- nunca convertir (OINV-21 / ley L7).
			if (!from.MeasuresDistanceIn(limitUnit))
				return Suspended(limitUnit, from.Unit, null);

			if (!latestUsable.IsExpressedIn(limitUnit))
				return Suspended(limitUnit, latestUsable.Unit, null);

			if (latestUsable.Value < from.Reading)
				throw new DomainException(
					$"La ultima lectura usable es {latestUsable.Value} y el punto base es "
					+ $"{from.Reading}, en la misma unidad: el odometro fue para atras. Eso no es "
					+ "una dimension suspendida, es un hecho imposible, y cual de los dos esta "
					+ "mal lo decide quien ordena las lecturas, no la regla del intervalo.");

			int effectiveLimit = factor.ApplyToDistanceLimit(declaredDistanceLimit);
			int travelled = latestUsable.Value - from.Reading;

			return new LimitStanding(
				"distance",
				true,
				true,
				StateFor(travelled, effectiveLimit,
					SchedulePolicy.DueSoonLeadDistance, SchedulePolicy.OverdueGraceDistance),
				travelled,
				effectiveLimit,
				limitUnit,
				null,
				null);
		}

		/// <summary>
		/// La dimension de tiempo no existe en esta ley: es el caso de DistanceOnly y de
		/// ConditionOnly. Ausente no es suspendida.
		/// </summary>
		internal static LimitStanding TimeIsNotPartOfThisLaw()
		{
			return new LimitStanding("time", false, false, null, 0, 0, null, null, null);
		}

		/// <summary>
		/// La dimension de distancia no existe en esta ley: es el caso de TimeOnly y de
		/// ConditionOnly. Ausente no es suspendida, y por eso no hay nada que curar.
		/// </summary>
		internal static LimitStanding DistanceIsNotPartOfThisLaw()
		{
			return new LimitStanding("distance", false, false, null, 0, 0, null, null, null);
		}

		/// <summary>
		/// Si esta dimension llego mas lejos que la otra. Es la comparacion con nombre que E23
		/// pide en lugar de una igualdad escrita, y es con la que las leyes L1 y L2 se dicen en
		/// una linea: la mas avanzada decide con L1, la menos avanzada con L2.
		/// </summary>
		internal bool IsMoreAdvancedThan(LimitStanding other)
		{
			return Advancement() > other.Advancement();
		}

		/// <summary>
		/// Si las dos dimensiones llegaron igual de lejos. Cuando eso pasa, LAS DOS deciden, y
		/// el motivo tiene que nombrar las dos (prueba 4 de §9).
		/// </summary>
		internal bool IsAsAdvancedAs(LimitStanding other)
		{
			return Advancement() == other.Advancement();
		}

		/// <summary>
		/// La clausula que esta dimension aporta al motivo que se le muestra a la persona:
		/// la dimension y el monto, o por que no se pudo evaluar. Componerla es conocimiento
		/// del dominio y no presentacion (OUS-07): saber que "1200 unidades pasadas del
		/// intervalo" es la forma de decir un vencimiento por distancia es parte de saber que
		/// es un vencimiento por distancia.
		/// </summary>
		internal string Describe()
		{
			if (!Applies) return "";

			if (SuspendedByTheUsageProfile)
				return _dimension + ": cannot be evaluated because the " + _profileName
					+ " usage profile ignores distance limits";

			if (SuspendedByUnitMismatch)
				return _dimension + ": cannot be evaluated because the limit is in "
					+ _limitUnit.Name + " and the measurements are in " + _mismatchedUnit.Name
					+ ", and units are never converted";

			if (!CanBeEvaluated)
				return _dimension + ": cannot be evaluated because no usable reading is available";

			// El limite alcanzado EXACTO se dice aparte y no como "0 pasadas del limite": el
			// borde inclusivo es un momento con significado propio -- justo ahi vence -- y
			// decirle a la persona "cero unidades pasado" suena a defecto en vez de a respuesta.
			if (Travelled == EffectiveLimit)
				return _dimension + ": exactly at a limit of " + Amount(EffectiveLimit);

			if (Travelled > EffectiveLimit)
				return _dimension + ": " + Amount(Travelled - EffectiveLimit)
					+ " past a limit of " + Amount(EffectiveLimit);

			return _dimension + ": " + Travelled + " of " + Amount(EffectiveLimit)
				+ " since the last one";
		}

		// El tiempo se dice en dias y la distancia en la unidad de su limite -- que no se
		// pluraliza, porque UnitA y UnitB son nombres y no sustantivos contables.
		private string Amount(int quantity)
		{
			if (_dimension == "time")
				return quantity + (quantity == 1 ? " day" : " days");

			return quantity + " " + _limitUnit.Name;
		}

		// La escalera de severidad, en orden. Alcanzado el limite VENCE -- inclusivo -- y por
		// eso la pregunta del anticipo va ULTIMA: si ya se alcanzo el limite, el aviso de
		// cortesia no se emite (ley L10).
		private static DueState StateFor(int travelled, int effectiveLimit, int courtesyLead, int grace)
		{
			if (travelled >= effectiveLimit + grace) return DueState.Overdue;
			if (travelled >= effectiveLimit) return DueState.Due;
			if (travelled >= effectiveLimit - courtesyLead) return DueState.DueSoon;

			return DueState.NotDue;
		}

		// El unico constructor de una dimension que no se puede evaluar. Los tres motivos son
		// excluyentes por construccion: cual sea se deduce de que trajo el llamador, y nadie de
		// afuera puede fabricar una suspension con dos motivos a la vez.
		//
		// La unidad del limite viaja igual, aunque no se pueda comparar contra nada: el motivo
		// tiene que poder decir CONTRA QUE no se pudo comparar, que es la mitad util del aviso.
		private static LimitStanding Suspended(
			DistanceUnit limitUnit, DistanceUnit mismatchedUnit, string profileName)
		{
			return new LimitStanding(
				"distance", true, false, null, 0, 0, limitUnit, mismatchedUnit, profileName);
		}

		// El rango de avance existe solo para comparar dos dimensiones entre si, y por eso es
		// privado: hacia afuera se preguntan las dos operaciones con nombre. Un numero de
		// severidad expuesto invita a que alguien ordene DueStates por el, y el conjunto
		// cerrado no tiene orden -- tiene siete miembros con significados propios.
		private int Advancement()
		{
			if (!CanBeEvaluated)
				throw new DomainException(
					"Se comparo el avance de una dimension que no se puede evaluar. Lo "
					+ "inevaluable no va en la misma escalera que lo medido: primero se "
					+ "pregunta CanBeEvaluated y se degrada (GAP G2).");

			if (State == DueState.Overdue) return 3;
			if (State == DueState.Due) return 2;
			if (State == DueState.DueSoon) return 1;

			return 0;
		}
	}
}
