using System;
using System.Text.Json;
using Puppets.Portador;

namespace Concesionario.App.Events
{
	/// <summary>
	/// Una sucursal registra DE UN SOLO ACTO una estadia que ya termino: donde, por que, sus dos
	/// instantes y los dias que cubrio. Es la forma correctiva, para lo que se registra despues de
	/// haber pasado, y no deja ninguna estadia abierta.
	/// </summary>
	public sealed class ShopStayRecorded : IMensajeDeEvento
	{
		public const string TheEventType = "ShopStayRecorded";

		private ShopStayRecorded() { }

		public long EventNumber { get; private set; }

		public string Branch { get; private set; }

		public string Subject { get; private set; }

		public DateTime EnteredAt { get; private set; }

		public DateTime CollectedAt { get; private set; }

		public string[] CoveredDays { get; private set; }

		long IMensajeDeEvento.Evento { get { return EventNumber; } }

		string IMensajeDeEvento.Tipo { get { return TheEventType; } }

		/// <summary>
		/// Lee la senal y contesta el hecho, o null para DESCARTARLA. Se descarta lo que no se puede
		/// decir: otro tipo, un numero de evento que no avanza, un payload que no es JSON, una
		/// sucursal en blanco, un asunto en blanco, cualquiera de los dos instantes ausente, unos dias
		/// que no dicen ningun dia, y una salida anterior a su entrada -- que aca SI se puede ver,
		/// porque esta senal trae los dos extremos. Cuantos dias son, en que orden vienen y si son
		/// contiguos sigue siendo ley del dominio.
		/// </summary>
		public static ShopStayRecorded Interpret(SenalDeEvento signal)
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

					DateTime collectedAt;
					if (!Payload.Instant(root, "collectedAt", out collectedAt)) return null;

					if (collectedAt < enteredAt) return null;

					string[] coveredDays;
					if (!Payload.Days(root, "coveredDays", out coveredDays)) return null;

					return new ShopStayRecorded
					{
						EventNumber = signal.Numero,
						Branch = branch,
						Subject = subject,
						EnteredAt = enteredAt,
						CollectedAt = collectedAt,
						CoveredDays = coveredDays,
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
