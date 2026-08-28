using System.Text.Json;
using Puppets.Portador;

namespace DealershipCar.App.Events
{
	/// <summary>
	/// El fabricante SUSTITUYO una revision por otra, que queda nombrada como la que la reemplazo.
	/// Los dos codigos son obligatorios: sustituir sin nombrar la sucesora apaga la revision sin
	/// dejar donde seguir.
	/// </summary>
	public sealed class SpecificationRevisionSuperseded : IMensajeDeEvento
	{
		public const string TheEventType = "SpecificationRevisionSuperseded";

		private SpecificationRevisionSuperseded() { }

		public long EventNumber { get; private set; }

		public string RevisionCode { get; private set; }

		public string SupersededByCode { get; private set; }

		long IMensajeDeEvento.Evento { get { return EventNumber; } }

		string IMensajeDeEvento.Tipo { get { return TheEventType; } }

		/// <summary>
		/// Lee la senal y contesta el hecho, o null para DESCARTARLA. Se descarta si falta cualquiera
		/// de los dos codigos, y descartar aca no cuesta ninguna entrada del journal.
		/// </summary>
		public static SpecificationRevisionSuperseded Interpret(SenalDeEvento signal)
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

					string supersededByCode = Payload.Text(root, "supersededByCode");
					if (supersededByCode.Trim().Length == 0) return null;

					return new SpecificationRevisionSuperseded
					{
						EventNumber = signal.Numero,
						RevisionCode = revisionCode,
						SupersededByCode = supersededByCode,
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
