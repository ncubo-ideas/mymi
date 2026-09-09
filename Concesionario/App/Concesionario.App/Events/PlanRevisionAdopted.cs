using System;
using System.Text.Json;
using Puppets.Portador;

namespace Concesionario.App.Events
{
	public sealed class PlanRevisionAdopted : IMensajeDeEvento
	{
		public const string TheEventType = "PlanRevisionAdopted";

		private PlanRevisionAdopted() { }

		public long EventNumber { get; private set; }

		public string RevisionCode { get; private set; }

		public DateTime AdoptedAt { get; private set; }

		long IMensajeDeEvento.Evento { get { return EventNumber; } }

		string IMensajeDeEvento.Tipo { get { return TheEventType; } }

		public static PlanRevisionAdopted Interpret(SenalDeEvento signal)
		{
			if (signal.Tipo != TheEventType) return null;
			if (signal.Numero <= 0) return null;
			if (signal.Contenido.Length == 0) return null;

			try
			{
				using (JsonDocument document = JsonDocument.Parse(signal.Contenido))
				{
					JsonElement root = document.RootElement;

					string revisionCode = Payload.Text(root, "revisionCode");
					if (revisionCode.Trim().Length == 0) return null;

					DateTime adoptedAt;
					if (!Payload.Instant(root, "adoptedAt", out adoptedAt)) return null;

					return new PlanRevisionAdopted
					{
						EventNumber = signal.Numero,
						RevisionCode = revisionCode,
						AdoptedAt = adoptedAt,
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
