using System;
using System.Text.Json;
using Puppets.Portador;

namespace DealershipCar.App.Events
{
	/// <summary>
	/// La agencia ENTREGO este vehiculo: cuando lo entrego, en que odometro estaba ese dia y en que
	/// unidad se leyo. Es el ancla desde donde se miden las dos mitades del sobre de garantia.
	/// Recibirlo de nuevo lo corrige: gana el ultimo que llego.
	/// </summary>
	public sealed class VehicleDelivered : IMensajeDeEvento
	{
		public const string TheEventType = "VehicleDelivered";

		private VehicleDelivered() { }

		public long EventNumber { get; private set; }

		public DateTime DeliveredAt { get; private set; }

		public int Reading { get; private set; }

		public string Unit { get; private set; }

		long IMensajeDeEvento.Evento { get { return EventNumber; } }

		string IMensajeDeEvento.Tipo { get { return TheEventType; } }

		/// <summary>
		/// Lee la senal y contesta el hecho, o null para DESCARTARLA. Se descarta lo que no se puede
		/// decir: otro tipo, un numero de evento que no avanza, un payload que no es JSON, una unidad
		/// que el canon no escribe, un instante o una lectura ausentes, y una lectura negativa.
		/// Descartar aca no cuesta ninguna entrada del journal. Una lectura en CERO si pasa: un carro
		/// entregado con el odometro en cero es un carro nuevo, que es el caso normal.
		/// </summary>
		public static VehicleDelivered Interpret(SenalDeEvento signal)
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

					DateTime deliveredAt;
					if (!Payload.Instant(root, "deliveredAt", out deliveredAt)) return null;

					int reading;
					if (!Payload.Whole(root, "reading", out reading)) return null;

					if (reading < 0) return null;

					return new VehicleDelivered
					{
						EventNumber = signal.Numero,
						DeliveredAt = deliveredAt,
						Reading = reading,
						Unit = unit,
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
