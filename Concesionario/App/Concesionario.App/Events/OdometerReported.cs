using System;
using System.Text.Json;
using Puppets.Portador;

namespace Concesionario.App.Events
{
	public sealed class OdometerReported : IMensajeDeEvento
	{
		public const string TheEventType = "OdometerReported";

		private OdometerReported() { }

		public long EventNumber { get; private set; }

		public int Value { get; private set; }

		public string Unit { get; private set; }

		public DateTime ObservedAt { get; private set; }

		public string Source { get; private set; }

		long IMensajeDeEvento.Evento { get { return EventNumber; } }

		string IMensajeDeEvento.Tipo { get { return TheEventType; } }

		public static OdometerReported Interpret(SenalDeEvento signal)
		{
			if (signal.Tipo != TheEventType) return null;
			if (signal.Numero <= 0) return null;
			if (signal.Contenido.Length == 0) return null;

			try
			{
				using (JsonDocument document = JsonDocument.Parse(signal.Contenido))
				{
					JsonElement root = document.RootElement;

					string unit = Payload.Text(root, "unit");
					string source = Payload.Text(root, "source");

					if (!WireName.NamesADistanceUnit(unit)) return null;
					if (!WireName.NamesAReadingSource(source)) return null;

					int value;
					if (!Payload.Whole(root, "value", out value)) return null;

					DateTime observedAt;
					if (!Payload.Instant(root, "observedAt", out observedAt)) return null;

					if (value < 0) return null;

					return new OdometerReported
					{
						EventNumber = signal.Numero,
						Value = value,
						Unit = unit,
						ObservedAt = observedAt,
						Source = source,
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
