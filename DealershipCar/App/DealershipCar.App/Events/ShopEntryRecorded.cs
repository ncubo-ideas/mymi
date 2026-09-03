using System;
using System.Text.Json;
using Puppets.Portador;

namespace DealershipCar.App.Events
{
	/// <summary>
	/// Una sucursal RECIBIO este vehiculo: donde lo recibieron, con que asunto entro y cuando. Abre
	/// una estadia, que queda abierta hasta que llegue la entrega.
	/// </summary>
	public sealed class ShopEntryRecorded : IMensajeDeEvento
	{
		public const string TheEventType = "ShopEntryRecorded";

		private ShopEntryRecorded() { }

		public long EventNumber { get; private set; }

		public string Branch { get; private set; }

		public string Subject { get; private set; }

		public DateTime EnteredAt { get; private set; }

		long IMensajeDeEvento.Evento { get { return EventNumber; } }

		string IMensajeDeEvento.Tipo { get { return TheEventType; } }

		/// <summary>
		/// Lee la senal y contesta el hecho, o null para DESCARTARLA. Se descarta lo que no se puede
		/// decir: otro tipo, un numero de evento que no avanza, un payload que no es JSON, una
		/// sucursal en blanco, un asunto en blanco y un instante de entrada ausente. Que el vehiculo
		/// ya estuviera adentro es una ley sobre el ESTADO del registro y no sobre lo que la senal
		/// dice, asi que esa la contesta el dominio y deja su marca.
		/// </summary>
		public static ShopEntryRecorded Interpret(SenalDeEvento signal)
		{
			if (signal.Tipo != TheEventType) return null;
			if (signal.Numero <= 0) return null;
			if (signal.Contenido.Length == 0) return null;

			try
			{
				using (JsonDocument document = JsonDocument.Parse(signal.Contenido))
				{
					JsonElement root = document.RootElement;

					string branch = Payload.Text(root, "branch");
					if (branch.Trim().Length == 0) return null;

					string subject = Payload.Text(root, "subject");
					if (subject.Trim().Length == 0) return null;

					DateTime enteredAt;
					if (!Payload.Instant(root, "enteredAt", out enteredAt)) return null;

					return new ShopEntryRecorded
					{
						EventNumber = signal.Numero,
						Branch = branch,
						Subject = subject,
						EnteredAt = enteredAt,
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
