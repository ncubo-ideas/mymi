using System;
using System.Collections.Generic;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// Una operacion de mantenimiento concreta en la agenda de un vehiculo: su definicion, el ancla
	/// desde la que se mide, donde esta parada, los aplazamientos que la persona uso y de que
	/// ocurrencia terminal salio. Es la OCURRENCIA (E39); Completed y Superseded la terminan y la
	/// siguiente es otra (E29, GAP G3).
	/// </summary>
	internal sealed class ScheduledOperation
	{
		private readonly MaintenanceOperationDefinition _definition;
		private readonly BasePoint _basePoint;
		private readonly bool _requestedByTheOwner;
		private readonly Predecessor _precededBy;
		private readonly List<Deferral> _deferrals = new List<Deferral>();

		private DueState _state = DueState.NotDue;
		private RecordedAssessment _standing = RecordedAssessment.None;
		private bool _conditionThresholdCrossed;

		private ScheduledOperation(
			MaintenanceOperationDefinition definition,
			BasePoint anchoredAt,
			bool requestedByTheOwner,
			Predecessor precededBy)
		{
			if (definition == null)
				throw new DomainException(
					"Una operacion agendada sin definicion no sabe que hay que hacer ni cuando "
					+ "vence (OINV-19).");

			if (anchoredAt == null)
				throw new DomainException(
					"Una operacion agendada sin punto base no tiene desde donde medir su "
					+ "intervalo. Que nunca se haya completado se dice con un ancla sin lectura, "
					+ "no con la falta del ancla.");

			_definition = definition;
			_basePoint = anchoredAt;
			_requestedByTheOwner = requestedByTheOwner;
			_precededBy = precededBy;
		}

		/// <summary>La operacion que el plan del fabricante manda, anclada donde se la sembro.</summary>
		internal static ScheduledOperation FromTheManufacturerPlan(
			MaintenanceOperationDefinition definition, BasePoint anchoredAt)
		{
			return new ScheduledOperation(definition, anchoredAt, false, Predecessor.None);
		}

		/// <summary>La operacion que pidio la persona. Solo estas se pueden quitar (ley L9).</summary>
		internal static ScheduledOperation RequestedByTheOwner(
			MaintenanceOperationDefinition definition, BasePoint anchoredAt)
		{
			return new ScheduledOperation(definition, anchoredAt, true, Predecessor.None);
		}

		/// <summary>Que operacion es.</summary>
		internal MaintenanceOperationKind Kind
		{
			get { return _definition.Kind; }
		}

		/// <summary>Donde esta parada. Sale del agregado como escalar, nunca como objeto (E24).</summary>
		internal DueState State
		{
			get { return _state; }
		}

		/// <summary>
		/// Cuantos aplazamientos lleva usados. DERIVADO de los que se admitieron (E35): un contador
		/// aparte se desincroniza el dia que alguien sume antes de mirar el tope.
		/// </summary>
		internal int DeferralsUsed
		{
			get { return _deferrals.Count; }
		}

		/// <summary>Si la pidio la persona en vez de mandarla el plan (ley L9).</summary>
		internal bool WasRequestedByTheOwner()
		{
			return _requestedByTheOwner;
		}

		/// <summary>Si esta ocurrencia salio de otra. Total: contesta siempre.</summary>
		internal bool HasAPredecessor()
		{
			return _precededBy.Exists();
		}

		/// <summary>La ocurrencia de la que salio. Revienta si es la genesis (E41).</summary>
		internal ScheduledOperation PrecededBy()
		{
			return _precededBy.Occurrence();
		}

		/// <summary>
		/// Si ya termino. Completed y Superseded son terminales: la instancia solto su identidad y
		/// no vuelve a moverse (E29).
		/// </summary>
		internal bool HasEnded()
		{
			return _state == DueState.Completed || _state == DueState.Superseded;
		}

		/// <summary>
		/// Si cuenta como mantenimiento no hecho. DERIVADO del estado (E35), asi que se apaga solo
		/// cuando la operacion se hace. Una aplazada sigue contando si la ley dice que se paso de
		/// la gracia: aplazar es la decision de la persona y no la saca de la cuenta (OINV-28).
		/// </summary>
		internal bool CountsAsMissedMaintenance()
		{
			if (HasEnded()) return false;
			if (_state == DueState.Deferred) return _standing.SaysTheOperationIsOverdue();

			return _state == DueState.Overdue;
		}

		/// <summary>
		/// LEY L7 (OINV-21): si la ultima evaluacion no pudo comparar la distancia porque el limite
		/// y las mediciones estan en unidades distintas, la operacion cae a solo-tiempo y LO DICE.
		/// Derivada de esa evaluacion y no guardada, asi que se cura sola (GAP G4).
		/// </summary>
		internal bool UnitMismatchSuspendsDistance()
		{
			return _standing.SuspendsDistanceByAUnitMismatch();
		}

		/// <summary>
		/// Por que esta parada donde esta, en una frase. Es del dominio y no de la vista (OUS-07):
		/// lo que la persona necesita saber es la dimension, el monto y, si no se pudo evaluar, el
		/// motivo.
		/// </summary>
		internal string Describe()
		{
			if (_state == DueState.Deferred) return TheDeferralSentence();
			if (_state == DueState.Completed) return "Completed.";
			if (_state == DueState.Superseded) return "Superseded.";

			if (_conditionThresholdCrossed && _state == DueState.Due)
				return "Due because a condition threshold crossing was received for this "
					+ "operation.";

			return _standing.Describe();
		}

		/// <summary>
		/// Donde queda la operacion en ese instante, con los hechos que el agregado le pasa. El
		/// instante SE RECIBE: el dominio no lee un reloj (§7, E30). La respuesta es lo que el host
		/// cuenta; un Evaluate mudo esconderia el hecho que maneja (E9).
		/// </summary>
		internal DueStateTransition Evaluate(
			DateTime asOf, UsableReading latestUsable, IntervalLengthFactor factor)
		{
			if (HasEnded()) return DueStateTransition.None;

			DueAssessment assessment = _definition.Assess(_basePoint, asOf, latestUsable, factor);
			_standing = RecordedAssessment.Of(assessment);

			if (_state == DueState.Deferred && !TheLatestDeferralHasElapsedBy(asOf))
				return DueStateTransition.None;

			return DueSoonIsACourtesy(ConditionOnlyBringsForward(assessment.State));
		}

		/// <summary>
		/// La persona deja la operacion para despues. Delega en
		/// <see cref="DeferralCapAndWindow"/>, donde la ley L6 tiene su nombre.
		/// </summary>
		internal void Defer(DeferralReason reason, DateTime decidedAt)
		{
			if (reason == null)
				throw new DomainException(
					"Un aplazamiento sin motivo no se puede registrar: la decision se journaliza "
					+ "CON su motivo (OINV-28).");

			DeferralCapAndWindow(reason, decidedAt);
		}

		/// <summary>
		/// Si un aplazamiento haria algo. Es la forma PREGUNTA de la ley L6, y la forma comando la
		/// usa: quien ofrece la accion pregunta lo mismo que la ley decide.
		/// </summary>
		internal bool CanBeDeferred()
		{
			return IsInADueState()
				&& !_definition.IsSafetyClassified
				&& _deferrals.Count < SchedulePolicy.MaxDeferralsPerOperation;
		}

		/// <summary>
		/// LEY L6 (OINV-27, OINV-52): la operacion se aplaza como maximo
		/// <see cref="SchedulePolicy.MaxDeferralsPerOperation"/> veces, cada una por
		/// <see cref="SchedulePolicy.DeferralWindowDays"/> dias, y una clasificada de seguridad no
		/// se aplaza nunca. Los tres rechazos son ESTADO, no excepcion (E32): la operacion se queda
		/// donde estaba. Fuera de un estado vencido es un no-op (E36).
		/// </summary>
		internal void DeferralCapAndWindow(DeferralReason reason, DateTime decidedAt)
		{
			if (!CanBeDeferred()) return;

			_deferrals.Add(new Deferral(reason, decidedAt));
			_state = DueState.Deferred;
		}

		/// <summary>
		/// Llega un cruce de umbral observado. Se recuerda -- si solo moviera el estado, la
		/// siguiente evaluacion lo pisaria -- y adelanta la operacion en el acto, salvo mientras
		/// corre un aplazamiento, que es una decision de la persona ya tomada.
		/// </summary>
		internal void RecordConditionThresholdCrossing(DateTime observedAt)
		{
			if (HasEnded()) return;

			if (observedAt < _basePoint.Instant)
				throw new DomainException(
					$"Se recibio un cruce de umbral observado el {observedAt} para una operacion "
					+ $"anclada en {_basePoint.Instant}: el cruce es anterior al ancla, asi que no "
					+ "habla de esta ocurrencia.");

			_conditionThresholdCrossed = true;

			if (_state == DueState.Deferred) return;

			_state = ConditionOnlyBringsForward(_state);
		}

		/// <summary>
		/// LEY L3 (OINV-22, OHS-08): un cruce de umbral recibido solo ADELANTA. Lleva la operacion a
		/// Due y nunca la baja de Overdue: el intervalo es el piso y un cruce no empuja un
		/// vencimiento hacia afuera.
		/// </summary>
		internal DueState ConditionOnlyBringsForward(DueState decided)
		{
			if (!_conditionThresholdCrossed) return decided;
			if (decided == DueState.Overdue) return decided;

			return DueState.Due;
		}

		/// <summary>
		/// LEY L10 (OINV-23, OEX-06): el aviso de cortesia nunca es una parada obligatoria. La
		/// operacion se mueve DIRECTO al estado que la ley decidio -- UNA transicion -- aunque el
		/// camino del diagrama pase por DueSoon.
		/// </summary>
		internal DueStateTransition DueSoonIsACourtesy(DueState decided)
		{
			DueState previous = _state;
			_state = decided;

			return DueStateTransition.Between(previous, decided);
		}

		/// <summary>
		/// La operacion se hizo de verdad. Termina esta instancia y devuelve la siguiente ocurrencia,
		/// anclada en la completacion real (ley L4) y enlazada hacia atras. Completar una que ya
		/// termino es un defecto de quien llama: bifurcaria la cadena, que es donde vive el
		/// historial.
		/// </summary>
		internal ScheduledOperation Complete(
			DateTime completedAt, int completionReading, DistanceUnit unit)
		{
			if (HasEnded())
				throw new DomainException(
					$"Se pidio completar una operacion que ya esta {_state.Name}. Una segunda "
					+ "completacion produciria una segunda sucesora y partiria la cadena en dos "
					+ "historiales.");

			if (unit == null)
				throw new DomainException(
					"Una lectura de completacion sin unidad es un numero suelto (OINV-21).");

			if (completionReading < 0)
				throw new DomainException(
					$"La lectura de una completacion no puede ser negativa, y llego "
					+ $"{completionReading}.");

			if (completedAt < _basePoint.Instant)
				throw new DomainException(
					$"Se pidio completar el {completedAt} una operacion anclada en "
					+ $"{_basePoint.Instant}: la completacion es anterior al ancla.");

			BasePoint next = _definition.NextOccurrenceFromActualCompletion(
				completedAt, completionReading, unit);

			_state = DueState.Completed;

			return new ScheduledOperation(
				_definition, next, _requestedByTheOwner, Predecessor.Of(this));
		}

		/// <summary>
		/// La operacion deja de estar activa sin haberse hecho: la sustituye una revision del plan
		/// (ley L5) o la quita la persona (ley L9). Terminal.
		/// </summary>
		internal void Supersede()
		{
			if (HasEnded())
				throw new DomainException(
					$"Se pidio sustituir una operacion que ya esta {_state.Name}. Una operacion "
					+ "terminal no ocupa el lugar de nadie, asi que no hay nada que sustituir.");

			_state = DueState.Superseded;
		}

		/// <summary>
		/// Una revision del plan la sustituye por otra. La reemplazante es una ocurrencia NUEVA
		/// enlazada hacia atras, asi que nace en NotDue y sin aplazamientos por ser nueva y no
		/// porque alguien la limpio (OEX-10).
		/// </summary>
		internal ScheduledOperation SupersededBy(
			MaintenanceOperationDefinition replacement, BasePoint anchoredAt)
		{
			if (replacement == null)
				throw new DomainException(
					"Una sustitucion sin definicion reemplazante deja la agenda sin la operacion "
					+ "en vez de con otra. Quitarla es RemoveOwnerOperation, que es otra cosa.");

			if (anchoredAt == null)
				throw new DomainException(
					"Una operacion reemplazante sin punto base no tiene desde donde medir su "
					+ "intervalo.");

			Supersede();

			return new ScheduledOperation(
				replacement, anchoredAt, _requestedByTheOwner, Predecessor.Of(this));
		}

		private bool IsInADueState()
		{
			return _state == DueState.DueSoon
				|| _state == DueState.Due
				|| _state == DueState.Overdue;
		}

		private bool TheLatestDeferralHasElapsedBy(DateTime asOf)
		{
			return _deferrals[_deferrals.Count - 1].WindowHasElapsedBy(asOf);
		}

		private string TheDeferralSentence()
		{
			Deferral latest = _deferrals[_deferrals.Count - 1];

			return "Deferred until " + latest.WindowEndsAt.ToString("yyyy-MM-dd")
				+ " because " + latest.Reason.Name + ". " + _deferrals.Count + " of "
				+ SchedulePolicy.MaxDeferralsPerOperation + " deferrals used.";
		}
	}
}
