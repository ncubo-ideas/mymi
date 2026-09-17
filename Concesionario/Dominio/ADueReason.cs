using System;
using System.Collections.Generic;

namespace Concesionario.Dominio
{
	/// <summary>
	/// POR QUE UNA OPERACION ESTA DONDE ESTA: que forma tiene la respuesta, los valores que la
	/// llenan, y las clausulas que la ley aporto en columnas paralelas y en el orden en que la ley
	/// las quiere. Se compone en cada lectura y nunca se guarda.
	/// <para>
	/// Es FORMA mas valores ya tipados y nunca una frase: la redaccion vive donde se elige el
	/// idioma, y el dominio no elige uno.
	/// </para>
	/// <para>
	/// La dimension que la ley no tiene no aporta fila, asi que las columnas traen ninguna, una o
	/// dos: el largo es la misma pregunta que <see cref="LimitStanding.TheClauseItAdds"/> contesta.
	/// </para>
	/// </summary>
	internal sealed class ADueReason
	{
		/// <summary>Esa clase no tiene ninguna operacion agendada en este vehiculo.</summary>
		internal const string NoOperationScheduled = "NoOperationScheduled";

		/// <summary>La operacion existe y todavia nadie la evaluo.</summary>
		internal const string NotEvaluatedYet = "NotEvaluatedYet";

		/// <summary>La persona la aplazo, y la ventana del aplazamiento sigue corriendo.</summary>
		internal const string Deferred = "Deferred";

		/// <summary>La ocurrencia termino: se hizo o la sustituyeron.</summary>
		internal const string Ended = "Ended";

		/// <summary>Vencio porque llego un cruce de umbral observado.</summary>
		internal const string ACrossingArrived = "ACrossingArrived";

		/// <summary>La ley no mide ni tiempo ni distancia: solo la mueve un cruce recibido.</summary>
		internal const string WaitsForACrossing = "WaitsForACrossing";

		/// <summary>La ley mide algo y no se pudo evaluar ninguna de sus dimensiones.</summary>
		internal const string NothingCouldBeEvaluated = "NothingCouldBeEvaluated";

		/// <summary>La ley decidio, y una o las dos dimensiones lo decidieron.</summary>
		internal const string Decided = "Decided";

		/// <summary>Las dos dimensiones llegaron igual de lejos y el motivo nombra las dos.</summary>
		internal const string TimeAndDistance = "TimeAndDistance";

		private static readonly LimitStanding[] WithoutClauses = new LimitStanding[0];

		private ADueReason(
			string shape, string state, string whoDecided, string kind,
			string deferredUntil, string deferralReason, int deferralsUsed, int deferralsAllowed,
			LimitStanding[] clauses)
		{
			Shape = shape;
			State = state;
			WhoDecided = whoDecided;
			Kind = kind;
			DeferredUntil = deferredUntil;
			DeferralReason = deferralReason;
			DeferralsUsed = deferralsUsed;
			DeferralsAllowed = deferralsAllowed;

			List<LimitStanding> said = new List<LimitStanding>();

			foreach (LimitStanding each in clauses)
				if (each.TheClauseItAdds().Length > 0) said.Add(each);

			ClauseCodes = new string[said.Count];
			ClauseDimensions = new string[said.Count];
			ClauseAmounts = new int[said.Count];
			ClauseLimits = new int[said.Count];
			ClauseUnits = new string[said.Count];
			ClauseMeasuredUnits = new string[said.Count];
			ClauseUsageProfiles = new string[said.Count];

			for (int row = 0; row < said.Count; row++)
			{
				ClauseCodes[row] = said[row].TheClauseItAdds();
				ClauseDimensions[row] = said[row].TheDimensionItMeasures();
				ClauseAmounts[row] = said[row].TheAmountItReached();
				ClauseLimits[row] = said[row].TheLimitItIsAgainst();
				ClauseUnits[row] = said[row].TheUnitOfTheLimit();
				ClauseMeasuredUnits[row] = said[row].TheUnitTheMeasurementsAreIn();
				ClauseUsageProfiles[row] = said[row].TheUsageProfileThatIgnoresIt();
			}
		}

		/// <summary>Que forma tiene la respuesta, por su codigo. TOTAL.</summary>
		internal string Shape { get; }

		/// <summary>Donde queda parada la operacion, por el nombre del miembro. TOTAL.</summary>
		internal string State { get; }

		/// <summary>
		/// Que dimension decidio, por su codigo, o vacio donde ninguna decidio. TOTAL.
		/// </summary>
		internal string WhoDecided { get; }

		/// <summary>La clase que no esta agendada, por su codigo, o vacia donde si lo esta. TOTAL.</summary>
		internal string Kind { get; }

		/// <summary>Hasta que dia corre el aplazamiento, o vacio donde no hay ninguno. TOTAL.</summary>
		internal string DeferredUntil { get; }

		/// <summary>Por que se aplazo, por el codigo del motivo, o vacio donde no se aplazo. TOTAL.</summary>
		internal string DeferralReason { get; }

		/// <summary>Cuantos aplazamientos lleva usados, o cero donde no se aplazo. TOTAL.</summary>
		internal int DeferralsUsed { get; }

		/// <summary>Cuantos admite la politica, o cero donde no se aplazo. TOTAL.</summary>
		internal int DeferralsAllowed { get; }

		/// <summary>Que clausula aporto cada dimension, por su codigo.</summary>
		internal string[] ClauseCodes { get; }

		/// <summary>De que dimension es cada clausula, por su codigo.</summary>
		internal string[] ClauseDimensions { get; }

		/// <summary>El monto del que habla cada clausula, o cero donde no habla de ninguno.</summary>
		internal int[] ClauseAmounts { get; }

		/// <summary>El limite ya escalado de cada clausula, o cero donde no nombra ninguno.</summary>
		internal int[] ClauseLimits { get; }

		/// <summary>La unidad del limite de cada clausula, o vacia donde no nombra ninguna.</summary>
		internal string[] ClauseUnits { get; }

		/// <summary>La unidad de las mediciones de cada clausula donde no coincide con la del limite.</summary>
		internal string[] ClauseMeasuredUnits { get; }

		/// <summary>El perfil que ignora la dimension de cada clausula, o vacio donde no la ignora.</summary>
		internal string[] ClauseUsageProfiles { get; }

		/// <summary>Esa clase no tiene ninguna operacion agendada, y se nombra.</summary>
		internal static ADueReason WithNoOperationScheduled(string kind)
		{
			return new ADueReason(
				NoOperationScheduled, DueState.NotDue.Name, "", kind, "", "", 0, 0, WithoutClauses);
		}

		/// <summary>La operacion existe y ninguna ley la evaluo todavia.</summary>
		internal static ADueReason NeverEvaluated()
		{
			return new ADueReason(
				NotEvaluatedYet, DueState.NotDue.Name, "", "", "", "", 0, 0, WithoutClauses);
		}

		/// <summary>La persona la aplazo: hasta cuando, por que, y cuantos lleva de cuantos.</summary>
		internal static ADueReason DeferredUntilTheWindowEnds(
			string until, string reason, int used, int allowed)
		{
			return new ADueReason(
				Deferred, DueState.Deferred.Name, "", "", until, reason, used, allowed,
				WithoutClauses);
		}

		/// <summary>La ocurrencia termino, en el estado que la termino.</summary>
		internal static ADueReason TheOccurrenceEnded(DueState state)
		{
			return new ADueReason(Ended, state.Name, "", "", "", "", 0, 0, WithoutClauses);
		}

		/// <summary>Vencio porque llego un cruce de umbral observado para esta operacion.</summary>
		internal static ADueReason ACrossingWasReceived()
		{
			return new ADueReason(
				ACrossingArrived, DueState.Due.Name, "", "", "", "", 0, 0, WithoutClauses);
		}

		/// <summary>La ley no mide ni tiempo ni distancia y espera un cruce recibido.</summary>
		internal static ADueReason WaitingForAReceivedCrossing(DueState state)
		{
			return new ADueReason(
				WaitsForACrossing, state.Name, "", "", "", "", 0, 0, WithoutClauses);
		}

		/// <summary>Ninguna dimension se pudo evaluar, y las clausulas dicen por que.</summary>
		internal static ADueReason NothingWasEvaluable(DueState state, LimitStanding[] clauses)
		{
			return new ADueReason(
				NothingCouldBeEvaluated, state.Name, "", "", "", "", 0, 0, clauses);
		}

		/// <summary>La ley decidio, con la dimension que decidio y las clausulas en su orden.</summary>
		internal static ADueReason DecidedBy(
			DueState state, string whoDecided, LimitStanding[] clauses)
		{
			return new ADueReason(Decided, state.Name, whoDecided, "", "", "", 0, 0, clauses);
		}
	}
}
