using System;
using System.Text.Json;
using Puppets.Portador;

namespace DealershipCar.App.Events
{
	/// <summary>
	/// La agencia PUBLICO el sobre de garantia de este vehiculo: su plazo en meses, su distancia con
	/// la unidad en que se midio, y cuando lo publico. Rige desde su instante de publicacion hacia
	/// adelante, asi que uno fechado antes del que rige no lo desplaza y tampoco se rechaza.
	/// </summary>
	public sealed class WarrantyEnvelopePublished : IMensajeDeEvento
	{
		public const string TheEventType = "WarrantyEnvelopePublished";

		private WarrantyEnvelopePublished() { }

		public long EventNumber { get; private set; }

		public int Months { get; private set; }

		public int Distance { get; private set; }

		public string Unit { get; private set; }

		public DateTime PublishedAt { get; private set; }

		long IMensajeDeEvento.Evento { get { return EventNumber; } }

		string IMensajeDeEvento.Tipo { get { return TheEventType; } }

		/// <summary>
		/// Lee la senal y contesta el hecho, o null para DESCARTARLA. Se descarta lo que no se puede
		/// decir: otro tipo, un numero de evento que no avanza, un payload que no es JSON, una unidad
		/// que el canon no escribe, una mitad o el instante ausentes, y una mitad negativa. Descartar
		/// aca no cuesta ninguna entrada del journal. Una mitad en CERO si pasa: cero es un numero que
		/// se puede decir, y que en un sobre signifique un limite ya cumplido el dia de la entrega es
		/// la ley del sobre, dicha con las palabras del sobre.
		/// </summary>
		public static WarrantyEnvelopePublished Interpret(SenalDeEvento signal)
		{
			if (signal.Tipo != TheEventType) return null;
			if (signal.Numero <= 0) return null;
			if (signal.Contenido.Length == 0) return null;

			try
			{
				using (JsonDocument document = JsonDocument.Parse(signal.Contenido))
				{
					JsonElement root = document.RootElement;

					string unit = Payload.Text(root, "unit");
					if (!WireName.NamesADistanceUnit(unit)) return null;

					int months;
					if (!Payload.Whole(root, "months", out months)) return null;

					int distance;
					if (!Payload.Whole(root, "distance", out distance)) return null;

					DateTime publishedAt;
					if (!Payload.Instant(root, "publishedAt", out publishedAt)) return null;

					if (months < 0 || distance < 0) return null;

					return new WarrantyEnvelopePublished
					{
						EventNumber = signal.Numero,
						Months = months,
						Distance = distance,
						Unit = unit,
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
