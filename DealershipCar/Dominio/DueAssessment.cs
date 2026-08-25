using System;

namespace DealershipCar.Dominio
{
	// LA FRASE SE COMPONE EN INGLES, que es la unica decision de este archivo que no se lee del
	// codigo. El canon nombra en ingles y DueState.Name ya sale asi hacia la frontera; una
	// frase mitad en un idioma y mitad en otro seria peor que cualquiera de las dos completas.
	// Traducir, si algun dia hace falta, es de la vista -- lo que es del dominio es QUE se dice
	// (OUS-07), no en que idioma termina impreso.

	/// <summary>
	/// La respuesta de una <see cref="IntervalRule"/>: en que estado queda la operacion, cual
	/// de las dos dimensiones lo decidio, y si alguna no se pudo evaluar en vez de no haber
	/// llegado todavia (GAP G2). Portador de E9, sin igualdad escrita.
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

		/// <summary>
		/// Donde queda parada la operacion. Es lo unico que el agregado necesita para moverla;
		/// todo lo demas de este portador existe para poder explicarlo.
		/// </summary>
		internal DueState State { get; }

		/// <summary>
		/// Si la dimension de tiempo participo en la decision. Con la ley L1 decide la que
		/// llego mas lejos; con la L2, la que va atrasada, que es la que retiene la operacion.
		/// </summary>
		internal bool DecidedByTime { get; }

		/// <summary>Si la dimension de distancia participo en la decision.</summary>
		internal bool DecidedByDistance { get; }

		/// <summary>
		/// Si la evaluacion pudo hacerse con al menos una dimension. Falso quiere decir que el
		/// <see cref="State"/> no es evidencia de nada: la unica dimension de la ley no se pudo
		/// evaluar, y un NotDue callado ahi seria "parece al dia y no lo esta" (GAP G2).
		/// </summary>
		internal bool CanBeEvaluated { get; }

		/// <summary>
		/// Si la dimension de distancia existe en esta ley pero no se pudo evaluar: unidades
		/// distintas, sin lectura, o un perfil que la ignora. NO es "todavia no llego" -- esa
		/// distincion es el GAP G2 entero -- y se recalcula en cada evaluacion, asi que se cura
		/// sola cuando llegan los hechos que faltaban (GAP G4).
		/// </summary>
		internal bool DistanceIsSuspended
		{
			get { return _distance.Applies && !_distance.CanBeEvaluated; }
		}

		/// <summary>
		/// Suspendida porque el limite y las mediciones estan en unidades distintas. Es la
		/// cuenta sobre la que ScheduledOperation nombra la ley L7 (OINV-21).
		/// </summary>
		internal bool SuspendedByUnitMismatch
		{
			get { return _distance.SuspendedByUnitMismatch; }
		}

		/// <summary>Suspendida porque no hay de donde contar: sin ancla con lectura, o sin lectura usable.</summary>
		internal bool SuspendedByAnAbsentReading
		{
			get { return _distance.SuspendedByAnAbsentReading; }
		}

		/// <summary>Suspendida porque el perfil de uso declarado ignora los limites de distancia (§6).</summary>
		internal bool SuspendedByTheUsageProfile
		{
			get { return _distance.SuspendedByTheUsageProfile; }
		}

		/// <summary>
		/// La respuesta de una evaluacion que si pudo hacerse: el estado, las dos dimensiones y
		/// cual de ellas decidio.
		/// </summary>
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
		/// La respuesta de una ley cuya UNICA dimension no se pudo evaluar, y que por lo tanto
		/// no tiene a donde degradar. El estado es NotDue porque el conjunto cerrado tiene
		/// siete miembros y ninguno dice "no lo se" -- pero viene con
		/// <see cref="CanBeEvaluated"/> en falso y con el motivo diciendolo, que es lo que
		/// impide que se lea como evidencia de que la operacion esta al dia (GAP G2).
		/// </summary>
		internal static DueAssessment NothingCouldBeEvaluated(
			LimitStanding time, LimitStanding distance)
		{
			return new DueAssessment(
				DueState.NotDue, time, distance, false, false, false, false);
		}

		/// <summary>
		/// La respuesta de ConditionOnly: no vence, y no vencera por si sola, porque no mide
		/// nada. Su NotDue SI es firme -- a diferencia del de
		/// <see cref="NothingCouldBeEvaluated"/> -- y lo unico que la mueve es un cruce de
		/// umbral recibido de afuera (ley L3, E44).
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
		/// El motivo, compuesto: el estado, que dimension lo decidio y con que monto, o por que
		/// no se pudo evaluar. Componer esta frase es conocimiento del dominio y no
		/// presentacion (OUS-07): es la respuesta a "por que vence", que es lo que la persona
		/// pregunta, y no solo "vence".
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

		// La dimension que decidio va primero: la frase se lee de lo que contesta la pregunta
		// hacia lo que la acompaña.
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
