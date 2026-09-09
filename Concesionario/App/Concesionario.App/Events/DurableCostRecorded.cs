using System;
using System.Text.Json;
using Puppets.Portador;

namespace Concesionario.App.Events
{
	/// <summary>
	/// Una sucursal registra un gasto que sigue dando valor despues del periodo en que se incurrio, con
	/// la vida util en meses que le dio quien lo vendio. Que sea de los duraderos lo dice ESTE tipo de
	/// hecho, y la vida util es OBLIGATORIA: no hay ninguna por defecto que poner en su lugar.
	/// </summary>
	public sealed class DurableCostRecorded : IMensajeDeEvento
	{
		public const string TheEventType = "DurableCostRecorded";

		private DurableCostRecorded() { }

		public long EventNumber { get; private set; }

		public DateTime IncurredAt { get; private set; }

		public decimal Amount { get; private set; }

		public string Currency { get; private set; }

		public string Subject { get; private set; }

		public int ExpectedLifeMonths { get; private set; }

		long IMensajeDeEvento.Evento { get { return EventNumber; } }

		string IMensajeDeEvento.Tipo { get { return TheEventType; } }

		/// <summary>
		/// Lee la senal y contesta el hecho, o null para DESCARTARLA. Se descarta todo lo del hecho
		/// recurrente, y ademas una vida util ausente, que no es un numero entero, o que no es positiva:
		/// una vida que no es positiva no deja ninguna ventana sobre la que repartir el gasto.
		/// <para>
		/// Lo que NO se descarta aca es una ventana que no cabe -- un gasto incurrido tan adelante que
		/// su fin se pasa del tiempo que se puede decir. Eso es una interaccion entre la vida util y el
		/// instante, y adelantarla pide reproducir la aritmetica del dominio: la contesta el dominio, y
		/// deja su marca.
		/// </para>
		/// </summary>
		public static DurableCostRecorded Interpret(SenalDeEvento signal)
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

					int expectedLifeMonths;
					if (!Payload.Whole(root, "expectedLifeMonths", out expectedLifeMonths)) return null;

					if (expectedLifeMonths <= 0) return null;

					DateTime incurredAt;
					if (!Payload.Instant(root, "incurredAt", out incurredAt)) return null;

					return new DurableCostRecorded
					{
						EventNumber = signal.Numero,
						IncurredAt = incurredAt,
						Amount = amount,
						Currency = currency,
						Subject = subject,
						ExpectedLifeMonths = expectedLifeMonths,
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
