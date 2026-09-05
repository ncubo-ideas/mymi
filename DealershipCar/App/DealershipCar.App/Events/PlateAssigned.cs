using System;
using System.Text.Json;
using Puppets.Portador;

namespace DealershipCar.App.Events
{
	/// <summary>
	/// La agencia le ASIGNO una placa a este vehiculo: el texto tal como lo publico y desde cuando
	/// rige. Recibir otra NO corrige la anterior -- son dos hechos, y la anterior se sigue leyendo
	/// por su propio instante.
	/// </summary>
	public sealed class PlateAssigned : IMensajeDeEvento
	{
		public const string TheEventType = "PlateAssigned";

		private PlateAssigned() { }

		public long EventNumber { get; private set; }

		public string Plate { get; private set; }

		public DateTime AssignedAt { get; private set; }

		long IMensajeDeEvento.Evento { get { return EventNumber; } }

		string IMensajeDeEvento.Tipo { get { return TheEventType; } }

		/// <summary>
		/// Lee la senal y contesta el hecho, o null para DESCARTARLA. Se descarta lo que no se puede
		/// decir: otro tipo, un numero de evento que no avanza, un payload que no es JSON, un
		/// instante ausente o ilegible, y un texto que no deja NINGUNA placa al normalizar.
		/// Descartar aca no cuesta ninguna entrada del journal; rechazarlo adentro del comando
		/// dejaria una cicatriz permanente, porque la entrada ya esta escrita cuando el cuerpo corre.
		/// La regla de que es una placa vive en el dominio y aca solo se le pregunta.
		/// </summary>
		public static PlateAssigned Interpret(SenalDeEvento signal)
		{
			if (signal.Tipo != TheEventType) return null;
			if (signal.Numero <= 0) return null;
			if (signal.Contenido.Length == 0) return null;

			try
			{
				using (JsonDocument document = JsonDocument.Parse(signal.Contenido))
				{
					JsonElement root = document.RootElement;

					string plate = Payload.Text(root, "plate");
					if (!WireName.NamesAPlate(plate)) return null;

					DateTime assignedAt;
					if (!Payload.Instant(root, "assignedAt", out assignedAt)) return null;

					return new PlateAssigned
					{
						EventNumber = signal.Numero,
						Plate = plate,
						AssignedAt = assignedAt,
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
