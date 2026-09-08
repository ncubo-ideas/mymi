using System;
using System.Text.Json;
using Puppets.Portador;

namespace Concesionario.App.Events
{
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
