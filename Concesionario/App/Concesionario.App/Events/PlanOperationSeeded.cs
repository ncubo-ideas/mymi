using System;
using System.Text.Json;
using Puppets.Portador;

namespace Concesionario.App.Events
{
	public sealed class PlanOperationSeeded : IMensajeDeEvento
	{
		public const string TheEventType = "PlanOperationSeeded";

		private PlanOperationSeeded() { }

		public long EventNumber { get; private set; }

		public string Kind { get; private set; }

		public string Basis { get; private set; }

		public int TimeLimitDays { get; private set; }

		public int DistanceLimit { get; private set; }

		public string Unit { get; private set; }

		public bool IsMandatory { get; private set; }

		public bool IsSafety { get; private set; }

		public DateTime AnchoredAt { get; private set; }

		public int AnchorReading { get; private set; }

		public string AnchorUnit { get; private set; }

		long IMensajeDeEvento.Evento { get { return EventNumber; } }

		string IMensajeDeEvento.Tipo { get { return TheEventType; } }

		public static PlanOperationSeeded Interpret(SenalDeEvento signal)
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
					string basis = Payload.Text(root, "basis");
					string unit = Payload.Text(root, "unit");
					string anchorUnit = Payload.Text(root, "anchorUnit");

					if (!WireName.NamesAnOperationKind(kind)) return null;
					if (!WireName.NamesAnIntervalBasis(basis)) return null;
					if (!WireName.NamesADistanceUnit(unit)) return null;
					if (!WireName.NamesADistanceUnit(anchorUnit)) return null;

					int timeLimitDays;
					if (!Payload.Whole(root, "timeLimitDays", out timeLimitDays)) return null;

					int distanceLimit;
					if (!Payload.Whole(root, "distanceLimit", out distanceLimit)) return null;

					int anchorReading;
					if (!Payload.Whole(root, "anchorReading", out anchorReading)) return null;

					if (timeLimitDays < 0 || distanceLimit < 0 || anchorReading < 0) return null;

					DateTime anchoredAt;
					if (!Payload.Instant(root, "anchoredAt", out anchoredAt)) return null;

					bool isMandatory;
					if (!Payload.Truth(root, "isMandatory", out isMandatory)) return null;

					bool isSafety;
					if (!Payload.Truth(root, "isSafety", out isSafety)) return null;

					return new PlanOperationSeeded
					{
						EventNumber = signal.Numero,
						Kind = kind,
						Basis = basis,
						TimeLimitDays = timeLimitDays,
						DistanceLimit = distanceLimit,
						Unit = unit,
						IsMandatory = isMandatory,
						IsSafety = isSafety,
						AnchoredAt = anchoredAt,
						AnchorReading = anchorReading,
						AnchorUnit = anchorUnit,
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
