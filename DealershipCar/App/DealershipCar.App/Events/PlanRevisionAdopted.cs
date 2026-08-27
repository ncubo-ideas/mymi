using System;
using System.Text.Json;
using Puppets.Portador;

namespace DealershipCar.App.Events
{
	// EL FABRICANTE ABRE UNA REVISION DEL PLAN. Deja su codigo y su instante; las operaciones que
	// la revision trae entran despues, una por una, como PlanOperationSeeded (C5). Ese es el unico
	// camino por el que una operacion entra a la agenda.
	//
	// El codigo de revision es TEXTO LIBRE del fabricante y no un conjunto cerrado: "R1", "24-B",
	// lo que el fabricante numere. Lo unico exigible es que venga y que no venga en blanco -- una
	// revision sin codigo no se puede nombrar despues, y nombrarla es para lo que existe.
	//
	// Una revision fechada antes de la que rige revienta ADENTRO del comando (OINV-19) y deja una
	// entrada permanente, por la misma razon que OperationCompleted: para saberlo hay que
	// preguntarle a la agenda cual rige, y la fachada de §4 no proyecta esa pregunta (C22).
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
