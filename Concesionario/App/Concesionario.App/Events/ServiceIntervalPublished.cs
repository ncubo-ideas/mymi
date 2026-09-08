using System.Text.Json;
using Puppets.Portador;

namespace Concesionario.App.Events
{
	/// <summary>
	/// El fabricante PUBLICO, dentro de una revision, cada cuanto se hace una operacion. Es el numero
	/// publicado y nombra su revision; la copia por vehiculo que la agenda usa para medir es otra cosa
	/// y este hecho no la toca.
	/// </summary>
	public sealed class ServiceIntervalPublished : IMensajeDeEvento
	{
		public const string TheEventType = "ServiceIntervalPublished";

		private ServiceIntervalPublished() { }

		public long EventNumber { get; private set; }

		public string RevisionCode { get; private set; }

		public string Kind { get; private set; }

		public int IntervalDistance { get; private set; }

		public string IntervalUnit { get; private set; }

		public int IntervalDays { get; private set; }

		long IMensajeDeEvento.Evento { get { return EventNumber; } }

		string IMensajeDeEvento.Tipo { get { return TheEventType; } }

		/// <summary>
		/// Lee la senal y contesta el hecho, o null para DESCARTARLA. Las dos mitades del intervalo se
		/// exigen PRESENTES aunque valgan cero -- cero es "esta mitad no se publico", una afirmacion,
		/// y omitir el campo no es decir cero. Descartar aca no cuesta ninguna entrada.
		/// </summary>
		public static ServiceIntervalPublished Interpret(SenalDeEvento signal)
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

					string kind = Payload.Text(root, "kind");
					string intervalUnit = Payload.Text(root, "intervalUnit");

					if (!WireName.NamesAnOperationKind(kind)) return null;
					if (!WireName.NamesADistanceUnit(intervalUnit)) return null;

					int intervalDistance;
					if (!Payload.Whole(root, "intervalDistance", out intervalDistance)) return null;

					int intervalDays;
					if (!Payload.Whole(root, "intervalDays", out intervalDays)) return null;

					if (intervalDistance < 0 || intervalDays < 0) return null;

					return new ServiceIntervalPublished
					{
						EventNumber = signal.Numero,
						RevisionCode = revisionCode,
						Kind = kind,
						IntervalDistance = intervalDistance,
						IntervalUnit = intervalUnit,
						IntervalDays = intervalDays,
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
