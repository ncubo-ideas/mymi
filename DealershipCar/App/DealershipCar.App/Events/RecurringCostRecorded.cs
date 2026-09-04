using System;
using System.Text.Json;
using Puppets.Portador;

namespace DealershipCar.App.Events
{
	/// <summary>
	/// Una sucursal registra un gasto que se consume en el periodo en que se incurrio: cuando, cuanto,
	/// en que moneda y en que se fue. Que sea de los que se consumen en su periodo lo dice ESTE tipo de
	/// hecho: no trae ninguna clasificacion ni ninguna vida util que darle.
	/// </summary>
	public sealed class RecurringCostRecorded : IMensajeDeEvento
	{
		public const string TheEventType = "RecurringCostRecorded";

		private RecurringCostRecorded() { }

		public long EventNumber { get; private set; }

		public DateTime IncurredAt { get; private set; }

		public decimal Amount { get; private set; }

		public string Currency { get; private set; }

		public string Subject { get; private set; }

		long IMensajeDeEvento.Evento { get { return EventNumber; } }

		string IMensajeDeEvento.Tipo { get { return TheEventType; } }

		/// <summary>
		/// Lee la senal y contesta el hecho, o null para DESCARTARLA. Se descarta lo que no se puede
		/// decir: otro tipo, un numero de evento que no avanza, un payload que no es JSON, un asunto en
		/// blanco, una moneda que este modulo no nombra, un monto ausente o que no es un numero, un
		/// monto negativo y un instante ausente. Un monto en CERO si cruza: cero es un gasto que se
		/// dijo.
		/// <para>
		/// La moneda la decide una consulta de reconocimiento TOTAL, no una guarda: una guarda que
		/// reventara por aca saldria de Apply y se llevaria el bloque de refresh entero. El dominio
		/// rechaza el HECHO; el borde decide si hay hecho que ofrecer.
		/// </para>
		/// </summary>
		public static RecurringCostRecorded Interpret(SenalDeEvento signal)
		{
			if (signal.Tipo != TheEventType) return null;
			if (signal.Numero <= 0) return null;
			if (signal.Contenido.Length == 0) return null;

			try
			{
				using (JsonDocument document = JsonDocument.Parse(signal.Contenido))
				{
					JsonElement root = document.RootElement;

					string subject = Payload.Text(root, "subject");
					if (subject.Trim().Length == 0) return null;

					string currency = Payload.Text(root, "currency");
					if (!WireName.NamesACurrency(currency)) return null;

					decimal amount;
					if (!Payload.Amount(root, "amount", out amount)) return null;

					if (amount < 0) return null;

					DateTime incurredAt;
					if (!Payload.Instant(root, "incurredAt", out incurredAt)) return null;

					return new RecurringCostRecorded
					{
						EventNumber = signal.Numero,
						IncurredAt = incurredAt,
						Amount = amount,
						Currency = currency,
						Subject = subject,
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
