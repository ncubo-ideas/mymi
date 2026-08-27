using System;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// La respuesta de una <see cref="IntervalRule"/>: en que estado queda la operacion, cual
	/// dimension lo decidio, y si alguna no se pudo evaluar en vez de no haber llegado.
	/// </summary>
	internal sealed class DueAssessment
	{
		private readonly LimitStanding _time;
		private readonly LimitStanding _distance;
		private readonly bool _waitsForAReceivedCrossing;

		private DueAssessment(
			DueState state,
			LimitStanding time,
			LimitStanding distance,
			bool decidedByTime,
			bool decidedByDistance,
			bool canBeEvaluated,
			bool waitsForAReceivedCrossing)
		{
			_time = time;
			_distance = distance;
			_waitsForAReceivedCrossing = waitsForAReceivedCrossing;

			State = state;
			DecidedByTime = decidedByTime;
			DecidedByDistance = decidedByDistance;
			CanBeEvaluated = canBeEvaluated;
		}

		/// <summary>Donde queda parada la operacion.</summary>
		internal DueState State { get; }

		/// <summary>Si la dimension de tiempo participo en la decision.</summary>
		internal bool DecidedByTime { get; }

		/// <summary>Si la dimension de distancia participo en la decision.</summary>
		internal bool DecidedByDistance { get; }

		/// <summary>
		/// Si pudo evaluarse al menos una dimension. Falso quiere decir que <see cref="State"/>
		/// no es evidencia de nada.
		/// </summary>
		internal bool CanBeEvaluated { get; }

		/// <summary>
		/// Si la ley mide distancia pero no se pudo evaluar. No es "todavia no llego", y
		/// se recalcula en cada evaluacion, asi que se cura sola.
		/// </summary>
		internal bool DistanceIsSuspended
		{
			get { return _distance.Applies && !_distance.CanBeEvaluated; }
		}

		/// <summary>Suspendida por unidades distintas.</summary>
		internal bool SuspendedByUnitMismatch
		{
			get { return _distance.SuspendedByUnitMismatch; }
		}

		/// <summary>Suspendida porque no hay de donde contar.</summary>
		internal bool SuspendedByAnAbsentReading
		{
			get { return _distance.SuspendedByAnAbsentReading; }
		}

		/// <summary>Suspendida porque el perfil de uso ignora los limites de distancia.</summary>
		internal bool SuspendedByTheUsageProfile
		{
			get { return _distance.SuspendedByTheUsageProfile; }
		}

		/// <summary>La respuesta de una evaluacion que si pudo hacerse.</summary>
		internal static DueAssessment DecidedBy(
			DueState state,
			LimitStanding time,
			LimitStanding distance,
			bool byTime,
			bool byDistance)
		{
			return new DueAssessment(state, time, distance, byTime, byDistance, true, false);
		}

		/// <summary>
		/// La respuesta de una ley cuya unica dimension no se pudo evaluar. NotDue, porque ningun
		/// <see cref="DueState"/> dice "no lo se", pero con <see cref="CanBeEvaluated"/> en falso.
		/// </summary>
		internal static DueAssessment NothingCouldBeEvaluated(
			LimitStanding time, LimitStanding distance)
		{
			return new DueAssessment(
				DueState.NotDue, time, distance, false, false, false, false);
		}

		/// <summary>
		/// La respuesta de ConditionOnly. Su NotDue es firme, a diferencia del de
		/// <see cref="NothingCouldBeEvaluated"/>: solo la mueve un cruce recibido.
		/// </summary>
		internal static DueAssessment WaitingForAReceivedCrossing()
		{
			return new DueAssessment(
				DueState.NotDue,
				LimitStanding.TimeIsNotPartOfThisLaw(),
				LimitStanding.DistanceIsNotPartOfThisLaw(),
				false,
				false,
				true,
				true);
		}

		/// <summary>
		/// El motivo compuesto: el estado, que dimension lo decidio y con que monto, o por que no
		/// se pudo evaluar. Es del dominio, no de la vista.
		/// </summary>
		internal string Describe()
		{
			if (_waitsForAReceivedCrossing)
				return State.Name + " because this operation has no time or distance limit: it "
					+ "becomes due only when a condition threshold crossing is received.";

			if (!CanBeEvaluated)
				return State.Name + ", but nothing could be evaluated, so this is not evidence "
					+ "that the operation is not due. " + Clauses() + ".";

			return State.Name + ", decided by " + WhoDecided() + ". " + Clauses() + ".";
		}

		private string WhoDecided()
		{
			if (DecidedByTime && DecidedByDistance) return "time and distance";
			if (DecidedByDistance) return "distance";

			return "time";
		}

		private string Clauses()
		{
			string first = DecidedByDistance && !DecidedByTime
				? _distance.Describe()
				: _time.Describe();

			string second = DecidedByDistance && !DecidedByTime
				? _time.Describe()
				: _distance.Describe();

			if (first.Length == 0) return second;
			if (second.Length == 0) return first;

			return first + "; " + second;
		}
	}
}
