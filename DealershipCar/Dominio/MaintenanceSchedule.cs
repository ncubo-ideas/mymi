using System;
using System.Collections.Generic;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// La lista viva de lo que un vehiculo necesita: sus operaciones activas, las lecturas del
	/// odometro ordenadas por observacion, el perfil de uso que rige y hasta cuando se evaluo. Es
	/// el agregado, uno por <see cref="Vin"/>, y la raiz global del DSL.
	/// </summary>
	internal sealed class MaintenanceSchedule
	{
		private readonly Vin _vin;
		private readonly UsageProfile _initialProfile;
		private readonly List<UsageProfileDeclaration> _declarations =
			new List<UsageProfileDeclaration>();
		private readonly List<PlanRevision> _revisions = new List<PlanRevision>();
		private readonly List<OdometerReading> _readings = new List<OdometerReading>();
		private readonly Dictionary<OperationKindCode, ScheduledOperation> _operations =
			new Dictionary<OperationKindCode, ScheduledOperation>();
		private readonly List<OperationKindCode> _order = new List<OperationKindCode>();

		private LastEvaluation _lastEvaluation = LastEvaluation.None;

		/// <summary>
		/// El constructor ES la creacion: el vehiculo y el perfil de uso con que arranca, y
		/// nada mas. El plan llega despues, por <see cref="SeedPlanOperation"/>, y cada operacion
		/// trae su propia ancla.
		/// </summary>
		internal MaintenanceSchedule(string vin, UsageProfileCode usageProfile)
		{
			_vin = new Vin(vin);
			_initialProfile = UsageProfile.OfCode(usageProfile);
		}

		/// <summary>
		/// Siembra una operacion del plan del fabricante con sus limites, sus clasificaciones y el
		/// ancla desde la que se mide -- instante, lectura y la unidad de esa lectura. Si la
		/// clase ya tiene una operacion activa, la SUSTITUYE en vez de duplicarla.
		/// </summary>
		internal void SeedPlanOperation(
			OperationKindCode kind, IntervalBasisCode basis,
			int timeLimitDays, int distanceLimit, DistanceUnitCode unit,
			bool isMandatory, bool isSafety,
			DateTime anchoredAt, int anchorReading, DistanceUnitCode anchorUnit)
		{
			MaintenanceOperationDefinition definition = new MaintenanceOperationDefinition(
				MaintenanceOperationKind.OfCode(kind),
				IntervalRule.Of(basis, timeLimitDays, distanceLimit, unit),
				isMandatory,
				isSafety);

			OneActiveOperationPerKind(
				kind, definition, TheAnchor(anchoredAt, anchorReading, anchorUnit), false);
		}

		/// <summary>
		/// ABRE una revision del plan: deja su codigo y su instante, y las operaciones que trae
		/// entran despues, una por una, por <see cref="SeedPlanOperation"/>. Una revision
		/// fechada antes de la que rige seria re-fechar el pasado y se rechaza.
		/// </summary>
		internal void AdoptPlanRevision(string revisionCode, DateTime adoptedAt)
		{
			PlanRevision adopted = new PlanRevision(revisionCode, adoptedAt);

			if (_revisions.Count > 0)
			{
				PlanRevision inForce = _revisions[_revisions.Count - 1];

				if (adopted.WasAdoptedBefore(inForce))
					throw new DomainException(
						$"Se pidio adoptar la revision '{adopted.Code}' con fecha "
						+ $"{adopted.AdoptedAt}, anterior a la revision '{inForce.Code}' que rige "
						+ $"desde {inForce.AdoptedAt}. Las revisiones van hacia adelante: una "
						+ "anterior no es una correccion, es el plan retrocediendo.");
			}

			_revisions.Add(adopted);
		}

		/// <summary>
		/// Agrega una operacion que pidio la persona, con su propia ancla. Nunca es obligatoria ni
		/// clasificada de seguridad: eso lo declara el fabricante. Sobre una clase que ya ocupa el
		/// plan se rechaza.
		/// </summary>
		internal void AddOwnerOperation(
			OperationKindCode kind, IntervalBasisCode basis,
			int timeLimitDays, int distanceLimit, DistanceUnitCode unit,
			DateTime anchoredAt, int anchorReading, DistanceUnitCode anchorUnit)
		{
			MaintenanceOperationDefinition definition = new MaintenanceOperationDefinition(
				MaintenanceOperationKind.OfCode(kind),
				IntervalRule.Of(basis, timeLimitDays, distanceLimit, unit),
				false,
				false);

			if (PlanIsNotTheOwnersToEdit(kind)) return;

			OneActiveOperationPerKind(
				kind, definition, TheAnchor(anchoredAt, anchorReading, anchorUnit), true);
		}

		/// <summary>
		/// La persona quita una operacion suya. Sobre una del plan se rechaza, y rechazar es un
		/// hecho del negocio y no una excepcion.
		/// </summary>
		internal void RemoveOwnerOperation(OperationKindCode kind)
		{
			AKindThatNamesNoMemberIsACallerBug(kind);

			if (PlanIsNotTheOwnersToEdit(kind)) return;
			if (!_operations.ContainsKey(kind)) return;

			_operations[kind].Supersede();
			Uninstall(kind);
		}

		/// <summary>
		/// La persona declara como usa el vehiculo, desde cuando. Una fecha de vigencia
		/// retroactiva se RECHAZA: re-escribiria lo que la ley ya decidio.
		/// </summary>
		internal void DeclareUsageProfile(UsageProfileCode profile, DateTime effectiveFrom)
		{
			UsageProfile declared = UsageProfile.OfCode(profile);

			if (_declarations.Count > 0
				&& effectiveFrom < _declarations[_declarations.Count - 1].EffectiveFrom)
				throw new DomainException(
					$"Se declaro un perfil de uso vigente desde {effectiveFrom}, antes del que "
					+ $"rige desde {_declarations[_declarations.Count - 1].EffectiveFrom}. "
					+ "Los cambios de perfil rigen hacia adelante.");

			if (_lastEvaluation.IsAfter(effectiveFrom))
				throw new DomainException(
					$"Se declaro un perfil de uso vigente desde {effectiveFrom}, y esta agenda ya "
					+ $"se evaluo hasta {_lastEvaluation.Instant()}. Un perfil nuevo no "
					+ "re-fecha lo que la ley ya decidio con el que regia entonces.");

			_declarations.Add(new UsageProfileDeclaration(declared, effectiveFrom));
		}

		/// <summary>
		/// La persona deja una operacion para despues. Fuera de un estado vencido, en el tope o
		/// sobre una clasificada de seguridad es un no-op, y la operacion se queda donde estaba.
		/// Sobre una clase que no esta agendada tampoco hace nada.
		/// </summary>
		internal void DeferOperation(
			OperationKindCode kind, DeferralReasonCode reason, DateTime decidedAt)
		{
			AKindThatNamesNoMemberIsACallerBug(kind);

			DeferralReason why = DeferralReason.OfCode(reason);

			if (!_operations.ContainsKey(kind)) return;

			_operations[kind].Defer(why, decidedAt);
		}

		/// <summary>
		/// Entra una lectura del odometro. Se guarda en la linea de tiempo de OBSERVACION y no en
		/// la de llegada; dentro de un mismo instante manda el orden de llegada, que es
		/// el desempate que el journal conserva. Una lectura que no calza en la linea se rechaza.
		/// </summary>
		internal void RecordReading(
			int value, DistanceUnitCode unit, DateTime observedAt, ReadingSourceCode source)
		{
			OdometerReading arriving = new OdometerReading(
				value, DistanceUnit.OfCode(unit), observedAt, ReadingSource.OfCode(source));

			int place = ItsPlaceOnTheObservationTimeline(arriving);

			RefuseAReadingThatDoesNotFitTheTimeline(arriving, place);

			_readings.Insert(place, arriving);
		}

		/// <summary>
		/// Entra un cruce de umbral observado para una clase. Solo ADELANTA la operacion de esa
		/// clase; sobre una clase que no esta agendada no hay nada que adelantar.
		/// </summary>
		internal void RecordConditionThreshold(OperationKindCode kind, DateTime observedAt)
		{
			AKindThatNamesNoMemberIsACallerBug(kind);

			if (!_operations.ContainsKey(kind)) return;

			_operations[kind].RecordConditionThresholdCrossing(observedAt);
		}

		/// <summary>
		/// La operacion se hizo de verdad. Termina esa ocurrencia y su SUCESORA ocupa la clase,
		/// anclada en la completacion real. Completar una clase que no esta agendada es
		/// un defecto de quien llama: no produce sucesora y el hecho se perderia.
		/// </summary>
		internal void CompleteOperation(
			OperationKindCode kind, DateTime completedAt, int completionReading,
			DistanceUnitCode unit)
		{
			MaintenanceOperationKind completed = MaintenanceOperationKind.OfCode(kind);
			DistanceUnit measuredIn = DistanceUnit.OfCode(unit);

			if (!_operations.ContainsKey(kind))
				throw new DomainException(
					$"Se recibio la completacion de un {completed.Name} que esta agenda no tiene "
					+ "activo. Una completacion re-ancla todo el futuro de su operacion, asi que "
					+ "una sin operacion a la que aplicarse no es un hecho de mas: es un hecho "
					+ "que se perderia.");

			Install(kind, _operations[kind].Complete(completedAt, completionReading, measuredIn));
		}

		/// <summary>
		/// Donde queda cada operacion en ese instante, y que se movio. El instante SE RECIBE: el
		/// dominio no lee un reloj. No se re-evalua el pasado -- el punto de control es
		/// monotono.
		/// </summary>
		internal EvaluationResult Evaluate(DateTime asOf)
		{
			if (_lastEvaluation.IsAfter(asOf))
				throw new DomainException(
					$"Se pidio evaluar al {asOf} una agenda ya evaluada hasta "
					+ $"{_lastEvaluation.Instant()}. Los estados que la ley decidio con hechos "
					+ "posteriores no se des-calculan volviendo el instante hacia atras.");

			UsableReading latestUsable = TheLatestUsableReadingAsOf(asOf);
			IntervalLengthFactor factor = UsageAdjustsLengthNotBasis(asOf);

			int transitions = 0;
			bool becameDue = false;
			bool becameOverdue = false;

			foreach (OperationKindCode kind in _order)
			{
				DueStateTransition moved = _operations[kind].Evaluate(asOf, latestUsable, factor);

				if (moved.Happened) transitions++;
				if (moved.BecameDue) becameDue = true;
				if (moved.BecameOverdue) becameOverdue = true;
			}

			_lastEvaluation = LastEvaluation.At(asOf);

			return EvaluationResult.Of(transitions, becameDue, becameOverdue);
		}

		/// <summary>
		/// A lo sumo UNA operacion no terminal por clase. Una revision del plan
		/// SUSTITUYE y nunca duplica, asi que la reemplazante toma el lugar de la activa y queda
		/// enlazada hacia atras. La cadena sigue solo entre operaciones del MISMO origen: el plan
		/// que desplaza a un recordatorio de la persona termina esa ocurrencia y entra como
		/// genesis suya, porque un reemplazo hereda el origen de la que sustituye y heredarlo
		/// cruzado dejaria a la ley protegiendo lo que no es. Devuelve la que quedo activa, como
		/// <see cref="ScheduledOperation.Complete"/> devuelve la sucesora: es la punta de la
		/// cadena, que es donde vive el historial.
		/// </summary>
		internal ScheduledOperation OneActiveOperationPerKind(
			OperationKindCode kind, MaintenanceOperationDefinition definition,
			BasePoint anchoredAt, bool requestedByTheOwner)
		{
			if (!_operations.ContainsKey(kind))
				return Install(kind, AGenesisOf(definition, anchoredAt, requestedByTheOwner));

			ScheduledOperation active = _operations[kind];

			if (active.WasRequestedByTheOwner() == requestedByTheOwner)
				return Install(kind, active.SupersededBy(definition, anchoredAt));

			active.Supersede();

			return Install(kind, AGenesisOf(definition, anchoredAt, requestedByTheOwner));
		}

		/// <summary>
		/// El perfil de uso escala el LARGO de los intervalos por un factor
		/// declarado, y nunca cambia la base de una operacion ni la quita. Rige el perfil que
		/// estaba declarado PARA ESE INSTANTE: una declaracion posterior no alcanza
		/// hacia atras.
		/// </summary>
		internal IntervalLengthFactor UsageAdjustsLengthNotBasis(DateTime asOf)
		{
			UsageProfile inForce = _initialProfile;

			foreach (UsageProfileDeclaration declared in _declarations)
				if (declared.TakesEffectBy(asOf)) inForce = declared.Profile;

			return IntervalLengthFactor.For(inForce);
		}

		/// <summary>
		/// La persona agrega y quita SUS operaciones; una del plan del
		/// fabricante se puede aplazar o declinar, pero nunca agregar, quitar ni re-fechar.
		/// Contesta si esa clase esta ocupada por una del plan, que es lo que no es suyo de tocar.
		/// Total: una clase sin operacion activa no es del plan.
		/// </summary>
		internal bool PlanIsNotTheOwnersToEdit(OperationKindCode kind)
		{
			return _operations.ContainsKey(kind) && !_operations[kind].WasRequestedByTheOwner();
		}

		/// <summary>De que vehiculo es esta agenda.</summary>
		internal string ReadVin()
		{
			return _vin.Value;
		}

		/// <summary>
		/// Donde esta parada la operacion de esa clase. TOTAL: una clase sin operacion
		/// agendada no esta vencida, y esa ausencia es una respuesta legal. Sale como escalar; el
		/// <see cref="DueState"/> no se entrega.
		/// </summary>
		internal string ReadDueState(OperationKindCode kind)
		{
			AKindThatNamesNoMemberIsACallerBug(kind);

			if (!_operations.ContainsKey(kind)) return DueState.NotDue.Name;

			return _operations[kind].State.Name;
		}

		/// <summary>
		/// Cuantas operaciones estan vencidas o pasadas. Overdue cuenta: es vencida y ademas
		/// pasada, con el mismo criterio con que <see cref="DueStateTransition"/> dice BecameDue.
		/// </summary>
		internal int CountDue()
		{
			int due = 0;

			foreach (OperationKindCode kind in _order)
				if (IsDueOrPast(_operations[kind])) due++;

			return due;
		}

		/// <summary>
		/// Cuantas operaciones no tienen nada pendiente. DERIVADO de donde esta parada cada una,
		/// igual que <see cref="MissedCount"/>: un contador aparte se quedaria abajo despues de
		/// completar una operacion, porque la que sube es la sucesora y no la que se hizo.
		/// </summary>
		internal int CountUpToDate()
		{
			int upToDate = 0;

			foreach (OperationKindCode kind in _order)
				if (_operations[kind].IsUpToDate()) upToDate++;

			return upToDate;
		}

		/// <summary>
		/// Cuantas operaciones tiene agendadas este vehiculo. Es el denominador de la anterior, y va
		/// al lado de ella para que nadie divida una cuenta del dominio entre una lista de la
		/// caratula: las clases del canon son nueve y las agendadas son las que la agencia programo.
		/// </summary>
		internal int CountScheduled()
		{
			return _order.Count;
		}

		/// <summary>Cuantas operaciones se pasaron de la gracia.</summary>
		internal int CountOverdue()
		{
			int overdue = 0;

			foreach (OperationKindCode kind in _order)
				if (_operations[kind].State == DueState.Overdue) overdue++;

			return overdue;
		}

		/// <summary>
		/// Cuantos aplazamientos lleva usados esa clase. Total, y DERIVADO de los que se
		/// admitieron: una clase sin operacion agendada no uso ninguno.
		/// </summary>
		internal int DeferralsUsed(OperationKindCode kind)
		{
			AKindThatNamesNoMemberIsACallerBug(kind);

			if (!_operations.ContainsKey(kind)) return 0;

			return _operations[kind].DeferralsUsed;
		}

		/// <summary>
		/// Si aplazar esa clase haria algo. Total: una clase sin operacion agendada no se
		/// puede aplazar, que es la misma respuesta que da el comando.
		/// </summary>
		internal bool CanBeDeferred(OperationKindCode kind)
		{
			AKindThatNamesNoMemberIsACallerBug(kind);

			if (!_operations.ContainsKey(kind)) return false;

			return _operations[kind].CanBeDeferred();
		}

		/// <summary>
		/// Si hay algo vencido. Total: es la pregunta de presencia que se hace ANTES de
		/// <see cref="ReadNextDueKind"/>.
		/// </summary>
		internal bool HasAnythingDue()
		{
			return CountDue() > 0;
		}

		/// <summary>
		/// Que hay que hacer primero: la operacion mas avanzada, y entre iguales la que entro
		/// antes a la agenda. GUARDADA: sin nada vencido revienta, porque nada vencido es la
		/// ausencia de la cosa que se pide y no una clase vacia.
		/// </summary>
		internal string ReadNextDueKind()
		{
			foreach (OperationKindCode kind in _order)
				if (_operations[kind].State == DueState.Overdue)
					return _operations[kind].Kind.Name;

			foreach (OperationKindCode kind in _order)
				if (_operations[kind].State == DueState.Due)
					return _operations[kind].Kind.Name;

			throw new DomainException(
				"No hay nada vencido en esta agenda: pregunte HasAnythingDue antes. Devolver una "
				+ "clase vacia haria indistinguible 'no hay nada que hacer' de 'lo siguiente es "
				+ "una operacion sin nombre'.");
		}

		/// <summary>
		/// Por que esa clase esta donde esta, en una frase. Total: una clase que no esta agendada
		/// lo dice con todas las letras. Componer el motivo es del dominio y no de la vista.
		/// </summary>
		internal string ReadDueReason(OperationKindCode kind)
		{
			MaintenanceOperationKind asked = MaintenanceOperationKind.OfCode(kind);

			if (!_operations.ContainsKey(kind))
				return "Este vehículo no tiene agendada ninguna operación de "
					+ asked.HowItIsSaid.ToLowerInvariant() + ", así que no hay nada que pueda "
					+ "estar vencido.";

			return _operations[kind].Describe();
		}

		/// <summary>
		/// Cuantas operaciones cuentan como mantenimiento no hecho. DERIVADO de donde estan
		/// paradas, asi que baja solo cuando la operacion se hace: un contador aparte
		/// arrastraria para siempre un mantenimiento ya hecho.
		/// </summary>
		internal int MissedCount()
		{
			int missed = 0;

			foreach (OperationKindCode kind in _order)
				if (_operations[kind].CountsAsMissedMaintenance()) missed++;

			return missed;
		}

		/// <summary>Si la agenda se evaluo alguna vez. Total: contesta siempre.</summary>
		internal bool HasBeenEvaluated()
		{
			return _lastEvaluation.Exists();
		}

		/// <summary>
		/// Hasta cuando se evaluo. Revienta si nunca se evaluo. Es el punto de control que
		/// la guarda de cadencia mira antes de dejar pasar un Evaluate.
		/// </summary>
		internal DateTime LastEvaluatedAt()
		{
			return _lastEvaluation.Instant();
		}

		/// <summary>
		/// Si hay una lectura del odometro que sirva para ese instante. Total: un vehiculo cuyo
		/// feed no trajo nada contesta que no hay, y esa ausencia es una respuesta legal. Es la
		/// pregunta de presencia que se hace ANTES de las cuatro de abajo.
		/// </summary>
		internal bool HasAnOdometerReading(DateTime asOf)
		{
			return TheLatestUsableReadingAsOf(asOf).Exists();
		}

		/// <summary>
		/// El odometro con que esta agenda mediria a ese instante. Es el MISMO numero que
		/// <see cref="Evaluate"/> usa al mismo instante, porque los dos lo resuelven por el mismo
		/// lugar: una lectura observada despues no cuenta para ninguno de los dos. Accesor
		/// GUARDADO.
		/// </summary>
		internal int ReadOdometer(DateTime asOf)
		{
			return TheOdometerReadingAsOf(asOf).Value;
		}

		/// <summary>
		/// La unidad de esa lectura, por su nombre, para quien la lea. Accesor GUARDADO.
		/// </summary>
		internal string ReadOdometerUnit(DateTime asOf)
		{
			return TheOdometerReadingAsOf(asOf).Unit.Name;
		}

		/// <summary>
		/// Esa misma unidad, dicha con su CODIGO para que un script pueda pasarla como argumento al
		/// metodo de otro agregado. Accesor GUARDADO. No sale hacia ninguna vista: el valor va
		/// directo a otro metodo de dominio adentro del mismo script.
		/// </summary>
		internal DistanceUnitCode OdometerUnit(DateTime asOf)
		{
			return TheOdometerReadingAsOf(asOf).TheCodeOfItsUnit;
		}

		/// <summary>
		/// Cuando se OBSERVO esa lectura, que no es el instante preguntado. Accesor GUARDADO. Sin
		/// esto, una frase compuesta con una lectura de marzo se leeria como tomada hoy.
		/// </summary>
		internal DateTime ReadOdometerObservedAt(DateTime asOf)
		{
			return TheOdometerReadingAsOf(asOf).ObservedAt;
		}

		/// <summary>
		/// Si se puede decir el ritmo de uso de ese periodo. Total: es la pregunta que se hace antes
		/// de leer la frase, y las cuatro razones que impiden decirlo son respuestas legales.
		/// </summary>
		internal bool CanARateOfUseBeSaid(DateTime from, DateTime to)
		{
			return TheSpanOfUseBetween(from, to).TheRate().CanBeSaid();
		}

		/// <summary>
		/// En cual de las cinco situaciones esta el ritmo de ese periodo, por el NOMBRE del miembro.
		/// TOTAL.
		/// </summary>
		internal string ReadRateOfUseStanding(DateTime from, DateTime to)
		{
			return TheSpanOfUseBetween(from, to).TheRate().Standing().Name;
		}

		/// <summary>
		/// El ritmo de uso de ese periodo en una frase: los dos extremos con sus instantes, la
		/// distancia, el tiempo y la distancia por dia; o la razon con nombre que impide decirlo.
		/// TOTAL. Los dos extremos no salen nunca sueltos: salen dentro de esta frase, que ya trae
		/// el ritmo.
		/// </summary>
		internal string DescribeRateOfUse(DateTime from, DateTime to)
		{
			return TheSpanOfUseBetween(from, to).TheRate().Describe();
		}

		/// <summary>
		/// Los dos periodos comparados en una sola frase: cada uno con su propio ritmo y el cambio
		/// del PRIMERO respecto al SEGUNDO dicho adentro. TOTAL. Ninguna lectura entrega el cambio
		/// solo.
		/// </summary>
		internal string DescribeTwoPeriodsOfUseCompared(
			DateTime from1, DateTime to1, DateTime from2, DateTime to2)
		{
			return ATwoSpanComparison.Of(
				TheSpanOfUseBetween(from1, to1).TheRate(),
				TheSpanOfUseBetween(from2, to2).TheRate()).Describe();
		}

		/// <summary>
		/// Cuando se observo la PRIMERA lectura de ese periodo, que es el ancla contra la que otro
		/// agregado mide lo suyo. Accesor GUARDADO: hay que preguntar antes si se puede decir el ritmo,
		/// con los mismos dos extremos. No sale hacia ninguna vista: el valor va directo al metodo de
		/// otro agregado adentro del mismo script.
		/// </summary>
		internal DateTime FirstReadingInstantBetween(DateTime from, DateTime to)
		{
			return TheSpanOfUseBetween(from, to).TheRate().TheInstantOfItsFirstEnd();
		}

		/// <summary>
		/// La distancia que el vehiculo recorrio entre los dos extremos de ese periodo, exacta.
		/// Accesor GUARDADO, y por el mismo camino: va directo al metodo de otro agregado adentro del
		/// mismo script y no sale por ninguna lectura de la puerta -- quien la tuviera suelta armaria
		/// una frase que este dominio no dijo.
		/// </summary>
		internal int DistanceTravelledBetween(DateTime from, DateTime to)
		{
			return TheSpanOfUseBetween(from, to).TheRate().TheDistanceBetweenItsEnds();
		}

		/// <summary>
		/// La unidad de esa distancia, dicha con su CODIGO. Accesor GUARDADO, y es la misma pareja que
		/// el odometro ya tiene: el codigo para un script, el nombre para quien lee.
		/// </summary>
		internal DistanceUnitCode UnitOfTravelBetween(DateTime from, DateTime to)
		{
			return TheSpanOfUseBetween(from, to).TheRate().TheCodeOfItsUnit();
		}

		/// <summary>Cuantas lecturas del odometro cayeron dentro de ese periodo. TOTAL.</summary>
		internal int ReadingCountBetween(DateTime from, DateTime to)
		{
			return TheSpanOfUseBetween(from, to).HowManyReadings();
		}

		/// <summary>
		/// Cuando se observo cada lectura del periodo, una por fila, en orden de observacion.
		/// TOTAL.
		/// </summary>
		internal string[] ReadingInstantsBetween(DateTime from, DateTime to)
		{
			List<ASeriesRow> rows = TheSpanOfUseBetween(from, to).Rows();

			string[] said = new string[rows.Count];

			for (int row = 0; row < rows.Count; row++) said[row] = rows[row].TheInstantItWasObserved();

			return said;
		}

		/// <summary>
		/// Que valor leyo cada lectura del periodo, una por fila y en el mismo orden. TOTAL. La
		/// unidad de cada una va en su propia columna: un valor sin su unidad no dice nada.
		/// </summary>
		internal int[] ReadingValuesBetween(DateTime from, DateTime to)
		{
			List<ASeriesRow> rows = TheSpanOfUseBetween(from, to).Rows();

			int[] said = new int[rows.Count];

			for (int row = 0; row < rows.Count; row++) said[row] = rows[row].TheValueItRead();

			return said;
		}

		/// <summary>
		/// En que unidad esta cada lectura del periodo, por su nombre de mundo real, una por fila y
		/// en el mismo orden. TOTAL.
		/// </summary>
		internal string[] ReadingUnitsBetween(DateTime from, DateTime to)
		{
			List<ASeriesRow> rows = TheSpanOfUseBetween(from, to).Rows();

			string[] said = new string[rows.Count];

			for (int row = 0; row < rows.Count; row++) said[row] = rows[row].TheNameOfItsUnit();

			return said;
		}

		/// <summary>
		/// De que fuente vino cada lectura del periodo, por el NOMBRE del miembro, una por fila y en
		/// el mismo orden. TOTAL. Nada se pesa ni se descarta por ella: decirla es la obligacion.
		/// </summary>
		internal string[] ReadingSourcesBetween(DateTime from, DateTime to)
		{
			List<ASeriesRow> rows = TheSpanOfUseBetween(from, to).Rows();

			string[] said = new string[rows.Count];

			for (int row = 0; row < rows.Count; row++) said[row] = rows[row].TheNameOfItsSource();

			return said;
		}

		/// <summary>
		/// Cada lectura del periodo en su propia frase, una por fila y en el mismo orden. TOTAL.
		/// </summary>
		internal string[] DescribeEachReadingBetween(DateTime from, DateTime to)
		{
			List<ASeriesRow> rows = TheSpanOfUseBetween(from, to).Rows();

			string[] said = new string[rows.Count];

			for (int row = 0; row < rows.Count; row++) said[row] = rows[row].Describe();

			return said;
		}

		/// <summary>
		/// Si se puede decir cuanta distancia le queda a esa operacion en ese instante. Total: es
		/// la pregunta que hay que hacer antes de las dos guardadas de abajo.
		/// </summary>
		internal bool CanDistanceRemainingBeSaid(OperationKindCode kind, DateTime asOf)
		{
			return TheRemainderOnDistanceFor(kind, asOf).CanBeSaid();
		}

		/// <summary>
		/// La distancia que le queda, compuesta adentro contra el limite ya escalado. Accesor
		/// GUARDADO. Pasado el limite es el EXCESO y nunca un negativo: cual de las dos lo dice
		/// <see cref="ReadDistanceRemainderStanding"/>.
		/// </summary>
		internal int ReadDistanceRemaining(OperationKindCode kind, DateTime asOf)
		{
			return TheRemainderOnDistanceFor(kind, asOf).Amount();
		}

		/// <summary>
		/// La unidad de ese monto, por su nombre, para quien la lea. Accesor GUARDADO.
		/// </summary>
		internal string ReadDistanceRemainingUnit(OperationKindCode kind, DateTime asOf)
		{
			return TheRemainderOnDistanceFor(kind, asOf).UnitName();
		}

		/// <summary>
		/// En cual de las siete situaciones esta la mitad de distancia, por su nombre. Total, y es
		/// lo que separa "no se mide asi" de "falta la lectura", que un booleano juntaria.
		/// </summary>
		internal string ReadDistanceRemainderStanding(OperationKindCode kind, DateTime asOf)
		{
			return TheRemainderOnDistanceFor(kind, asOf).Standing().Name;
		}

		/// <summary>
		/// Lo que le queda de distancia, en una frase. Total: las siete situaciones dicen algo.
		/// </summary>
		internal string DescribeDistanceRemaining(OperationKindCode kind, DateTime asOf)
		{
			return TheRemainderOnDistanceFor(kind, asOf).Describe();
		}

		/// <summary>Si se puede decir cuantos dias le quedan a esa operacion. Total.</summary>
		internal bool CanDaysRemainingBeSaid(OperationKindCode kind, DateTime asOf)
		{
			return TheRemainderOnTimeFor(kind, asOf).CanBeSaid();
		}

		/// <summary>
		/// Los dias que le quedan del plazo ya escalado. Accesor GUARDADO, y pasado el plazo es el
		/// exceso, igual que en la distancia.
		/// </summary>
		internal int ReadDaysRemaining(OperationKindCode kind, DateTime asOf)
		{
			return TheRemainderOnTimeFor(kind, asOf).Amount();
		}

		/// <summary>En cual de las siete situaciones esta la mitad de tiempo, por su nombre. Total.</summary>
		internal string ReadTimeRemainderStanding(OperationKindCode kind, DateTime asOf)
		{
			return TheRemainderOnTimeFor(kind, asOf).Standing().Name;
		}

		/// <summary>Lo que le queda de plazo, en una frase. Total.</summary>
		internal string DescribeDaysRemaining(OperationKindCode kind, DateTime asOf)
		{
			return TheRemainderOnTimeFor(kind, asOf).Describe();
		}

		/// <summary>
		/// La cuenta atras entera: LAS DOS dimensiones y las dos situaciones en una frase, sin
		/// elegir ninguna. Elegir la mas cercana dejaria el numero de la pantalla discrepando del
		/// motivo del estado para quien lea los dos juntos. Total.
		/// <para>
		/// Sin operacion agendada no hay dos mitades que decir, asi que se dice una sola vez.
		/// </para>
		/// </summary>
		internal string DescribeCountdown(OperationKindCode kind, DateTime asOf)
		{
			ARemainder distance = TheRemainderOnDistanceFor(kind, asOf);

			if (distance.HasNoOperationScheduled()) return distance.Describe();

			return distance.Describe() + "; " + TheRemainderOnTimeFor(kind, asOf).Describe();
		}

		/// <summary>
		/// Si esa clase tiene alguna completacion alcanzable en su cadena. Total: es la pregunta que
		/// hay que hacer antes de leer las filas, y las dos ausencias contestan que no por razones
		/// distintas que <see cref="DescribeCompletions"/> dice.
		/// </summary>
		internal bool HasAnyCompletion(OperationKindCode kind)
		{
			return TheCompletionsOf(kind).HasAnyCompletion();
		}

		/// <summary>
		/// Cuantas completaciones hay en la cadena de esa clase. DERIVADO de las filas en cada
		/// lectura y nunca guardado. Total.
		/// </summary>
		internal int CompletionCount(OperationKindCode kind)
		{
			return TheCompletionsOf(kind).HowManyCompletions();
		}

		/// <summary>
		/// El historial de esa clase en una frase. Total: nunca completada y no agendada son dos
		/// ausencias distintas y cada una dice la suya.
		/// </summary>
		internal string DescribeCompletions(OperationKindCode kind)
		{
			return TheCompletionsOf(kind).Describe();
		}

		/// <summary>
		/// Cuando se hizo cada completacion de esa clase, una por fila, de la mas reciente a la mas
		/// vieja. TOTAL.
		/// </summary>
		internal string[] CompletionInstantsOf(OperationKindCode kind)
		{
			List<ACompletionRow> rows = TheCompletionsOf(kind).TheCompletions();

			string[] said = new string[rows.Count];

			for (int row = 0; row < rows.Count; row++) said[row] = rows[row].TheInstantItWasDone();

			return said;
		}

		/// <summary>
		/// Con que lectura del odometro se hizo cada completacion de esa clase, una por fila y en el
		/// mismo orden. La que no tiene lectura lo DICE con palabras. TOTAL.
		/// </summary>
		internal string[] CompletionReadingsOf(OperationKindCode kind)
		{
			List<ACompletionRow> rows = TheCompletionsOf(kind).TheCompletions();

			string[] said = new string[rows.Count];

			for (int row = 0; row < rows.Count; row++) said[row] = rows[row].TheReadingItWasDoneAt();

			return said;
		}

		/// <summary>
		/// Cada completacion de esa clase en su propia frase, una por fila y en el mismo orden.
		/// TOTAL.
		/// </summary>
		internal string[] DescribeEachCompletionOf(OperationKindCode kind)
		{
			List<ACompletionRow> rows = TheCompletionsOf(kind).TheCompletions();

			string[] said = new string[rows.Count];

			for (int row = 0; row < rows.Count; row++) said[row] = rows[row].Describe();

			return said;
		}

		/// <summary>
		/// De que clase es cada completacion del historial entero, por el NOMBRE del miembro, una por
		/// fila y de la mas reciente a la mas vieja. TOTAL.
		/// </summary>
		internal string[] CompletionKindsOnRecord()
		{
			List<ACompletionRow> rows = TheCompletionsOnRecord();

			string[] said = new string[rows.Count];

			for (int row = 0; row < rows.Count; row++) said[row] = rows[row].TheNameOfItsKind();

			return said;
		}

		/// <summary>
		/// Cuando se hizo cada completacion del historial entero, una por fila y en el mismo orden.
		/// TOTAL.
		/// </summary>
		internal string[] CompletionInstantsOnRecord()
		{
			List<ACompletionRow> rows = TheCompletionsOnRecord();

			string[] said = new string[rows.Count];

			for (int row = 0; row < rows.Count; row++) said[row] = rows[row].TheInstantItWasDone();

			return said;
		}

		/// <summary>
		/// Con que lectura del odometro se hizo cada completacion del historial entero, una por fila
		/// y en el mismo orden. La que no tiene lectura lo dice. TOTAL.
		/// </summary>
		internal string[] CompletionReadingsOnRecord()
		{
			List<ACompletionRow> rows = TheCompletionsOnRecord();

			string[] said = new string[rows.Count];

			for (int row = 0; row < rows.Count; row++) said[row] = rows[row].TheReadingItWasDoneAt();

			return said;
		}

		/// <summary>
		/// Cada completacion del historial entero en su propia frase, una por fila y en el mismo
		/// orden. TOTAL.
		/// </summary>
		internal string[] DescribeEachCompletionOnRecord()
		{
			List<ACompletionRow> rows = TheCompletionsOnRecord();

			string[] said = new string[rows.Count];

			for (int row = 0; row < rows.Count; row++) said[row] = rows[row].Describe();

			return said;
		}

		private CompletionsOfAnOperation TheCompletionsOf(OperationKindCode kind)
		{
			MaintenanceOperationKind asked = MaintenanceOperationKind.OfCode(kind);

			if (!_operations.ContainsKey(kind))
				return CompletionsOfAnOperation.OfAKindThatIsNotOnTheSchedule(asked);

			return CompletionsOfAnOperation.Of(
				asked, new List<ACompletionRow>(_operations[kind].TheCompletionsBehindIt()));
		}

		private List<ACompletionRow> TheCompletionsOnRecord()
		{
			List<ACompletionRow> rows = new List<ACompletionRow>();

			foreach (OperationKindCode kind in _order)
				foreach (ACompletionRow arriving in TheCompletionsOf(kind).TheCompletions())
					PlaceNewestFirst(rows, arriving);

			return rows;
		}

		private static void PlaceNewestFirst(List<ACompletionRow> rows, ACompletionRow arriving)
		{
			int place = rows.Count;

			while (place > 0 && arriving.IsNewerThan(rows[place - 1])) place--;

			rows.Insert(place, arriving);
		}

		private ARemainder TheRemainderOnDistanceFor(OperationKindCode kind, DateTime asOf)
		{
			AKindThatNamesNoMemberIsACallerBug(kind);

			if (!_operations.ContainsKey(kind)) return NoOperationScheduledFor(kind);

			return TheAssessmentAsOf(kind, asOf).WhatRemainsOnDistance();
		}

		private ARemainder TheRemainderOnTimeFor(OperationKindCode kind, DateTime asOf)
		{
			AKindThatNamesNoMemberIsACallerBug(kind);

			if (!_operations.ContainsKey(kind)) return NoOperationScheduledFor(kind);

			return TheAssessmentAsOf(kind, asOf).WhatRemainsOnTime();
		}

		private DueAssessment TheAssessmentAsOf(OperationKindCode kind, DateTime asOf)
		{
			return _operations[kind].AssessedAt(
				asOf, TheLatestUsableReadingAsOf(asOf), UsageAdjustsLengthNotBasis(asOf));
		}

		private static ARemainder NoOperationScheduledFor(OperationKindCode kind)
		{
			return ARemainder.WithNoOperationScheduled(
				MaintenanceOperationKind.OfCode(kind).HowItIsSaid);
		}

		private static BasePoint TheAnchor(
			DateTime anchoredAt, int anchorReading, DistanceUnitCode anchorUnit)
		{
			return BasePoint.At(anchoredAt, anchorReading, DistanceUnit.OfCode(anchorUnit));
		}

		private static ScheduledOperation AGenesisOf(
			MaintenanceOperationDefinition definition, BasePoint anchoredAt,
			bool requestedByTheOwner)
		{
			if (requestedByTheOwner)
				return ScheduledOperation.RequestedByTheOwner(definition, anchoredAt);

			return ScheduledOperation.FromTheManufacturerPlan(definition, anchoredAt);
		}

		private static void AKindThatNamesNoMemberIsACallerBug(OperationKindCode kind)
		{
			MaintenanceOperationKind.OfCode(kind);
		}

		private static bool IsDueOrPast(ScheduledOperation operation)
		{
			return operation.State == DueState.Due || operation.State == DueState.Overdue;
		}

		private ScheduledOperation Install(OperationKindCode kind, ScheduledOperation operation)
		{
			if (!_operations.ContainsKey(kind)) _order.Add(kind);

			_operations[kind] = operation;

			return operation;
		}

		private void Uninstall(OperationKindCode kind)
		{
			_operations.Remove(kind);
			_order.Remove(kind);
		}

		private OdometerReading TheOdometerReadingAsOf(DateTime asOf)
		{
			UsableReading usable = TheLatestUsableReadingAsOf(asOf);

			if (!usable.Exists())
				throw new DomainException(
					$"Esta agenda no tiene ninguna lectura del odometro observada al {asOf} ni "
					+ "antes: pregunte HasAnOdometerReading antes. Devolver cero seria una "
					+ "medicion que nadie tomo, y contra un limite de distancia se leeria como un "
					+ "vehiculo que no ha recorrido nada.");

			return usable.Reading();
		}

		private UsableReading TheLatestUsableReadingAsOf(DateTime asOf)
		{
			for (int reading = _readings.Count - 1; reading >= 0; reading--)
				if (_readings[reading].ObservedAt <= asOf)
					return UsableReading.Of(_readings[reading]);

			return UsableReading.None;
		}

		// EL TRAMO DE UN PERIODO, armado en cada lectura y nunca guardado. Los dos limites son
		// INCLUSIVOS, igual que los del historial de estadias, y las lecturas salen en el orden en
		// que _readings las guarda, que es el de OBSERVACION.
		//
		// ES PRIVADO, y ASpanOfUse es el unico que recibe la lista. Lo que sale de la agenda son
		// escalares y columnas de escalares: ningun portador de lectura cruza la fachada.
		private ASpanOfUse TheSpanOfUseBetween(DateTime from, DateTime to)
		{
			List<OdometerReading> inside = new List<OdometerReading>();

			foreach (OdometerReading reading in _readings)
				if (reading.ObservedAt >= from && reading.ObservedAt <= to) inside.Add(reading);

			return ASpanOfUse.Of(inside, from, to);
		}

		private int ItsPlaceOnTheObservationTimeline(OdometerReading arriving)
		{
			int place = _readings.Count;

			while (place > 0 && _readings[place - 1].ObservedAt > arriving.ObservedAt) place--;

			return place;
		}

		private void RefuseAReadingThatDoesNotFitTheTimeline(OdometerReading arriving, int place)
		{
			for (int earlier = 0; earlier < place; earlier++)
				if (arriving.IsExpressedIn(_readings[earlier].Unit)
					&& _readings[earlier].Value > arriving.Value)
					throw new DomainException(
						$"Llego una lectura de {arriving.Value} {arriving.Unit.Name} observada el "
						+ $"{arriving.ObservedAt}, y el {_readings[earlier].ObservedAt} ya se "
						+ $"habia observado {_readings[earlier].Value}. El odometro no va para "
						+ "atras: no es una dimension suspendida, es un hecho "
						+ "imposible.");

			for (int later = place; later < _readings.Count; later++)
				if (arriving.IsExpressedIn(_readings[later].Unit)
					&& _readings[later].Value < arriving.Value)
					throw new DomainException(
						$"Llego una lectura de {arriving.Value} {arriving.Unit.Name} observada el "
						+ $"{arriving.ObservedAt}, y el {_readings[later].ObservedAt} -- despues "
						+ $"-- se observo {_readings[later].Value}. Un relleno tardio vale si "
						+ "calza en la linea de observacion; este no calza.");
		}
	}
}
