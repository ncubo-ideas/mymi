using System;
using System.Text.Json;
using Puppets.Portador;

namespace DealershipCar.App.Events
{
	/// <summary>
	/// Una sucursal ENTREGO este vehiculo: cuando salio y que dias de calendario dice que cubrio la
	/// estadia. Cierra la estadia que estaba abierta.
	/// </summary>
	public sealed class ShopCollectionRecorded : IMensajeDeEvento
	{
		public const string TheEventType = "ShopCollectionRecorded";

		private ShopCollectionRecorded() { }

		public long EventNumber { get; private set; }

		public DateTime CollectedAt { get; private set; }

		public string[] CoveredDays { get; private set; }

		long IMensajeDeEvento.Evento { get { return EventNumber; } }

		string IMensajeDeEvento.Tipo { get { return TheEventType; } }

		/// <summary>
		/// Lee la senal y contesta el hecho, o null para DESCARTARLA. Se descarta lo que no se puede
		/// decir: otro tipo, un numero de evento que no avanza, un payload que no es JSON, un instante
		/// de entrega ausente y unos dias que no dicen ningun dia. Que haya algo que cerrar, que los
		/// dias vengan contiguos y que la entrega no sea anterior a la entrada son leyes que miran una
		/// estadia que esta EN EL REGISTRO -- esta senal no la trae -- asi que las contesta el dominio.
		/// </summary>
		public static ShopCollectionRecorded Interpret(SenalDeEvento signal)
		{
			if (signal.Tipo != TheEventType) return null;
			if (signal.Numero <= 0) return null;
			if (signal.Contenido.Length == 0) return null;

			try
			{
				using (JsonDocument document = JsonDocument.Parse(signal.Contenido))
				{
					JsonElement root = document.RootElement;

					DateTime collectedAt;
					if (!Payload.Instant(root, "collectedAt", out collectedAt)) return null;

					string[] coveredDays;
					if (!Payload.Days(root, "coveredDays", out coveredDays)) return null;

					return new ShopCollectionRecorded
					{
						EventNumber = signal.Numero,
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
