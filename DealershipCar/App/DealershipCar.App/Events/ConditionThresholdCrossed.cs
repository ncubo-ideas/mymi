using System;
using System.Text.Json;
using Puppets.Portador;

namespace DealershipCar.App.Events
{
	// EL TALLER OBSERVO UN CRUCE DE UMBRAL. Es el unico hecho que mueve una operacion de base
	// ConditionOnly, y solo la ADELANTA: nunca empuja un vencimiento hacia adelante (ley L3).
	//
	// Es el mas corto de los cinco -- una clase y un instante -- y aun asi tiene cuatro caminos
	// distintos que devuelven null. Esa es la proporcion que la frontera tiene siempre: mucho mas
	// codigo en descartar que en construir.
	//
	// Ver OdometerReported para por que los codigos se validan aca.
	public sealed class ConditionThresholdCrossed : IMensajeDeEvento
	{
		public const string TheEventType = "ConditionThresholdCrossed";

		private ConditionThresholdCrossed() { }

		public long EventNumber { get; private set; }

		public string Kind { get; private set; }

		public DateTime ObservedAt { get; private set; }

		long IMensajeDeEvento.Evento { get { return EventNumber; } }

		string IMensajeDeEvento.Tipo { get { return TheEventType; } }

		public static ConditionThresholdCrossed Interpret(SenalDeEvento signal)
		{
			if (signal.Tipo != TheEventType) return null;
			if (signal.Numero <= 0) return null;
			if (signal.Contenido.Length == 0) return null;

			try
			{
				using (JsonDocument document = JsonDocument.Parse(signal.Contenido))
				{
					JsonElement root = document.RootElement;

					string kind = Payload.Text(root, "kind");
					if (!WireName.NamesAnOperationKind(kind)) return null;

					DateTime observedAt;
					if (!Payload.Instant(root, "observedAt", out observedAt)) return null;

					return new ConditionThresholdCrossed
					{
						EventNumber = signal.Numero,
						Kind = kind,
						ObservedAt = observedAt,
					};
				}
			}
			catch (JsonException)
			{
				return null;
			}
		}
	}
}
