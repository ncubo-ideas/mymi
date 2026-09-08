using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// La ley de intervalo que no mide nada: vence cuando se recibe un cruce de umbral, y no
	/// antes. Sin limite de tiempo ni de distancia, y por lo tanto sin punto de vencimiento
	/// propio -- que es lo que hace que un cruce recibido solo pueda ADELANTAR una operacion y
	/// nunca posponerla.
	/// </summary>
	internal sealed class ConditionOnly : IntervalRule
	{
		/// <summary>
		/// Sin limites. La firma vacia es la ley dicha en la forma del tipo: no hay numero que
		/// pueda hacer vencer esta operacion.
		/// </summary>
		internal ConditionOnly()
		{
		}

		/// <summary>No vencida, siempre. Delega en <see cref="WaitsForAReceivedCrossing"/>.</summary>
		internal override DueAssessment Assess(
			BasePoint from, DateTime asOf, UsableReading latestUsable, IntervalLengthFactor factor)
		{
			return WaitsForAReceivedCrossing();
		}

		/// <summary>
		/// Contesta no vencida sin mirar el instante, el ancla ni las lecturas: esta ley no mide
		/// desde ningun lado, asi que no tiene punto de vencimiento que empujar. Esa ausencia es
		/// la mitad de la ley que la regla sostiene.
		/// </summary>
		internal DueAssessment WaitsForAReceivedCrossing()
		{
			return DueAssessment.WaitingForAReceivedCrossing();
		}

		/// <summary>
		/// El ancla nueva es el instante real de la completacion y solo el. No hay
		/// intervalo que medir desde ahi, asi que la lectura se descarta.
		/// </summary>
		internal override BasePoint NextOccurrenceFromActualCompletion(
			DateTime completedAt, int reading, DistanceUnit unit)
		{
			return BasePoint.AtInstantOnly(completedAt);
		}
	}
}
