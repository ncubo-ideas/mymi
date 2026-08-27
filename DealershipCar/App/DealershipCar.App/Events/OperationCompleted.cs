using System;
using System.Text.Json;
using Puppets.Portador;

namespace DealershipCar.App.Events
{
	// EL TALLER HIZO LA OPERACION DE VERDAD. Re-ancla todo el futuro de esa clase: la sucesora se
	// mide desde la completacion REAL y no desde donde el plan decia (ley L4). Por eso la lectura
	// del odometro viaja con su unidad -- es la unidad del ancla de la sucesora, y fijarla haria
	// medir a la sucesora contra una escala que nadie uso.
	//
	// ES EL MENSAJE QUE MAS CARO SALE CUANDO NO CALZA. Una completacion de una clase que esta
	// agenda no tiene activa revienta ADENTRO del comando, y eso deja una entrada permanente
	// (C22). Esta frontera no lo puede evitar: para saberlo hay que preguntarle a la agenda, y la
	// fachada de §4 no proyecta esa pregunta. Lo que si hace es no dejar pasar nada MAS.
	//
	// Ver OdometerReported para por que los codigos se validan aca.
	public sealed class OperationCompleted : IMensajeDeEvento
	{
		public const string TheEventType = "OperationCompleted";

		private OperationCompleted() { }

		public long EventNumber { get; private set; }

		public string Kind { get; private set; }

		public DateTime CompletedAt { get; private set; }

		public int CompletionReading { get; private set; }

		public string Unit { get; private set; }

		long IMensajeDeEvento.Evento { get { return EventNumber; } }

		string IMensajeDeEvento.Tipo { get { return TheEventType; } }

		public static OperationCompleted Interpret(SenalDeEvento signal)
		{
			if (signal.Tipo != TheEventType) return null;
			if (signal.Numero <= 0) return null;
			if (signal.Contenido.Length == 0) return null;

			try
			{
				using (JsonDocument document = JsonDocument.Parse(signal.Contenido))
				{
					JsonElement root = document.RootElement;

					string kind = Payload.Text(root, "kind");
					string unit = Payload.Text(root, "unit");

					if (!WireName.NamesAnOperationKind(kind)) return null;
					if (!WireName.NamesADistanceUnit(unit)) return null;

					DateTime completedAt;
					if (!Payload.Instant(root, "completedAt", out completedAt)) return null;

					int completionReading;
					if (!Payload.Whole(root, "completionReading", out completionReading)) return null;
					if (completionReading < 0) return null;

					return new OperationCompleted
					{
						EventNumber = signal.Numero,
						Kind = kind,
						CompletedAt = completedAt,
						CompletionReading = completionReading,
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
