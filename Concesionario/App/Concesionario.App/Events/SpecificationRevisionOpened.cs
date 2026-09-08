using System;
using System.Text.Json;
using Puppets.Portador;

namespace Concesionario.App.Events
{
	/// <summary>
	/// El fabricante ABRIO una revision de la especificacion del modelo, con su codigo y el instante
	/// en que la publico. Nace borrador y su contenido llega despues, un hecho por comando.
	/// </summary>
	public sealed class SpecificationRevisionOpened : IMensajeDeEvento
	{
		public const string TheEventType = "SpecificationRevisionOpened";

		private SpecificationRevisionOpened() { }

		public long EventNumber { get; private set; }

		public string RevisionCode { get; private set; }

		public DateTime PublishedAt { get; private set; }

		long IMensajeDeEvento.Evento { get { return EventNumber; } }

		string IMensajeDeEvento.Tipo { get { return TheEventType; } }

		/// <summary>
		/// Lee la senal y contesta el hecho, o null para DESCARTARLA. Se descarta lo que no se puede
		/// decir: otro tipo, un numero de evento que no avanza, un payload que no es JSON, un codigo
		/// en blanco o un instante ausente. Descartar aca no cuesta ninguna entrada del journal.
		/// </summary>
		public static SpecificationRevisionOpened Interpret(SenalDeEvento signal)
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

					DateTime publishedAt;
					if (!Payload.Instant(root, "publishedAt", out publishedAt)) return null;

					return new SpecificationRevisionOpened
					{
						EventNumber = signal.Numero,
						RevisionCode = revisionCode,
						PublishedAt = publishedAt,
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
