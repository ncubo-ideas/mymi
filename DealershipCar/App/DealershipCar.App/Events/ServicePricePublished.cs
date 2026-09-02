using System;
using System.Text.Json;
using Puppets.Portador;

namespace DealershipCar.App.Events
{
	/// <summary>
	/// El fabricante PUBLICO, dentro de una revision, el precio de una operacion en un hito: el monto
	/// con su moneda y entre que instantes rige. El monto y la moneda entran tal como se publicaron y
	/// aca no se convierte ninguno de los dos.
	/// </summary>
	public sealed class ServicePricePublished : IMensajeDeEvento
	{
		public const string TheEventType = "ServicePricePublished";

		private ServicePricePublished() { }

		public long EventNumber { get; private set; }

		public string RevisionCode { get; private set; }

		public string Kind { get; private set; }

		public int MilestoneDistance { get; private set; }

		public string MilestoneUnit { get; private set; }

		public decimal Amount { get; private set; }

		public string Currency { get; private set; }

		public DateTime ValidFrom { get; private set; }

		public DateTime ValidUntil { get; private set; }

		long IMensajeDeEvento.Evento { get { return EventNumber; } }

		string IMensajeDeEvento.Tipo { get { return TheEventType; } }

		/// <summary>
		/// Lee la senal y contesta el hecho, o null para DESCARTARLA. Los dos instantes de la vigencia
		/// son obligatorios: sin ellos un precio se leeria como el de hoy para siempre. Un monto
		/// negativo no es un precio. Descartar aca no cuesta ninguna entrada.
		/// </summary>
		public static ServicePricePublished Interpret(SenalDeEvento signal)
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

					string currency = Payload.Text(root, "currency");
					if (currency.Trim().Length == 0) return null;

					string kind = Payload.Text(root, "kind");
					string milestoneUnit = Payload.Text(root, "milestoneUnit");

					if (!WireName.NamesAnOperationKind(kind)) return null;
					if (!WireName.NamesADistanceUnit(milestoneUnit)) return null;

					int milestoneDistance;
					if (!Payload.Whole(root, "milestoneDistance", out milestoneDistance)) return null;

					decimal amount;
					if (!Payload.Amount(root, "amount", out amount)) return null;

					if (milestoneDistance < 0 || amount < 0) return null;

					DateTime validFrom;
					if (!Payload.Instant(root, "validFrom", out validFrom)) return null;

					DateTime validUntil;
					if (!Payload.Instant(root, "validUntil", out validUntil)) return null;

					return new ServicePricePublished
					{
						EventNumber = signal.Numero,
						RevisionCode = revisionCode,
						Kind = kind,
						MilestoneDistance = milestoneDistance,
						MilestoneUnit = milestoneUnit,
						Amount = amount,
						Currency = currency,
						ValidFrom = validFrom,
						ValidUntil = validUntil,
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
