using System;

namespace DealershipCar.Dominio
{
	// ===== LA MITAD QUE ESTA REGLA SOSTIENE DE LA LEY L3 ==============================
	//
	// L3, ConditionOnlyBringsForward, vive en ScheduledOperation y llega en T4: un cruce
	// recibido puede ADELANTAR una operacion a Due y nunca empujar un vencimiento hacia afuera
	// (OHS-08). Lo que esta regla aporta no es una cuenta sino una AUSENCIA:
	//
	//   NUNCA CALCULA UN PUNTO DE VENCIMIENTO, ASI QUE NO TIENE NINGUNO QUE EMPUJAR.
	//
	// Una base que calculara uno propio podria contestar "esta no vence hasta marzo" y estaria
	// posponiendo por la puerta de atras algo que un cruce ya adelanto.
	//
	// ==================================================================================

	/// <summary>
	/// La ley de intervalo que no mide nada: vence cuando se recibe un cruce de umbral, y no
	/// antes. Sin limite de tiempo ni de distancia, y por lo tanto sin punto de vencimiento
	/// propio -- que es lo que hace que un cruce recibido solo pueda ADELANTAR una operacion y
	/// nunca posponerla (ley L3, OHS-08).
	/// </summary>
	internal sealed class ConditionOnly : IntervalRule
	{
		/// <summary>
		/// Levanta la ley. No recibe ningun limite, y esa firma vacia es la ley dicha en la
		/// forma del tipo: no hay numero que pueda hacer vencer esta operacion.
		/// </summary>
		internal ConditionOnly()
		{
		}

		/// <summary>
		/// Donde queda parada la operacion: no vencida, siempre. Delega en
		/// <see cref="WaitsForAReceivedCrossing"/>, que es donde esta dicho por que.
		/// </summary>
		internal override DueAssessment Assess(
			BasePoint from, DateTime asOf, OdometerReading latestUsable, IntervalLengthFactor factor)
		{
			return WaitsForAReceivedCrossing();
		}

		/// <summary>
		/// Contesta NO VENCIDA, y lo contesta sin mirar nada: ni el instante, ni el ancla, ni
		/// las lecturas. No es que los ignore por comodidad -- es que esta ley no mide desde
		/// ningun lado, y por eso NO TIENE UN PUNTO DE VENCIMIENTO QUE EMPUJAR. Esa ausencia es
		/// la mitad que esta regla sostiene de la ley L3: lo unico que puede mover esta
		/// operacion es un cruce de umbral recibido, y un cruce solo la adelanta.
		/// </summary>
		internal DueAssessment WaitsForAReceivedCrossing()
		{
			return DueAssessment.WaitingForAReceivedCrossing();
		}

		/// <summary>
		/// LEY L4 para esta base: el ancla nueva es el instante REAL de la completacion, y solo
		/// el. Sirve para saber cuando se hizo por ultima vez; no hay intervalo que medir desde
		/// ahi, asi que la lectura que venga se descarta.
		/// </summary>
		internal override BasePoint NextOccurrenceFromActualCompletion(
			DateTime completedAt, int reading, DistanceUnit unit)
		{
			return BasePoint.AtInstantOnly(completedAt);
		}
	}
}
