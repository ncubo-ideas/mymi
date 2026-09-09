using System;
using System.Text.Json;
using Puppets.Portador;

namespace Concesionario.App.Events
{
	public sealed class OperationCompleted : IMensajeDeEvento
	{
		public const string TheEventType = "OperationCompleted";

		private OperationCompleted() { }

		public long EventNumber { get; private set; }

		public string Kind { get; private set; }

		public DateTime CompletedAt { get; private set; }

		public int CompletionReading { get; private set; }

		public string Unit { get; private set; }

		long IMensajeDeEvento.Evento { get { return EventNumber; } }

		string IMensajeDeEvento.Tipo { get { return TheEventType; } }

		public static OperationCompleted Interpret(SenalDeEvento signal)
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
					string unit = Payload.Text(root, "unit");

					if (!WireName.NamesAnOperationKind(kind)) return null;
					if (!WireName.NamesADistanceUnit(unit)) return null;

					DateTime completedAt;
					if (!Payload.Instant(root, "completedAt", out completedAt)) return null;

					int completionReading;
					if (!Payload.Whole(root, "completionReading", out completionReading)) return null;
					if (completionReading < 0) return null;

					return new OperationCompleted
					{
						EventNumber = signal.Numero,
						Kind = kind,
						CompletedAt = completedAt,
						CompletionReading = completionReading,
						Unit = unit,
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
