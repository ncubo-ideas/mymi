using System.Text.Json;
using Puppets.Portador;

namespace DealershipCar.App.Events
{
	/// <summary>
	/// El fabricante PUBLICO, dentro de una revision, que fluido de una clase lleva el modelo: el
	/// grado, la capacidad con su unidad, y cada cuanto se cambia. La capacidad viaja en la unidad en
	/// que se publico y aca no se convierte a ninguna otra.
	/// </summary>
	public sealed class FluidSpecificationPublished : IMensajeDeEvento
	{
		public const string TheEventType = "FluidSpecificationPublished";

		private FluidSpecificationPublished() { }

		public long EventNumber { get; private set; }

		public string RevisionCode { get; private set; }

		public string Kind { get; private set; }

		public string Grade { get; private set; }

		public decimal Capacity { get; private set; }

		public string CapacityUnit { get; private set; }

		public int IntervalDistance { get; private set; }

		public string IntervalUnit { get; private set; }

		public int IntervalDays { get; private set; }

		long IMensajeDeEvento.Evento { get { return EventNumber; } }

		string IMensajeDeEvento.Tipo { get { return TheEventType; } }

		/// <summary>
		/// Lee la senal y contesta el hecho, o null para DESCARTARLA. Se descarta lo que no se puede
		/// decir: un nombre que no es de ningun miembro de su conjunto cerrado, un campo numerico
		/// ausente -- que no es un cero, es un silencio -- o un numero negativo en una escala que no
		/// los tiene. Descartar aca no cuesta ninguna entrada del journal.
		/// </summary>
		public static FluidSpecificationPublished Interpret(SenalDeEvento signal)
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

					string grade = Payload.Text(root, "grade");
					if (grade.Trim().Length == 0) return null;

					string kind = Payload.Text(root, "kind");
					string capacityUnit = Payload.Text(root, "capacityUnit");
					string intervalUnit = Payload.Text(root, "intervalUnit");

					if (!WireName.NamesAFluidKind(kind)) return null;
					if (!WireName.NamesAVolumeUnit(capacityUnit)) return null;
					if (!WireName.NamesADistanceUnit(intervalUnit)) return null;

					decimal capacity;
					if (!Payload.Amount(root, "capacity", out capacity)) return null;

					int intervalDistance;
					if (!Payload.Whole(root, "intervalDistance", out intervalDistance)) return null;

					int intervalDays;
					if (!Payload.Whole(root, "intervalDays", out intervalDays)) return null;

					if (capacity < 0 || intervalDistance < 0 || intervalDays < 0) return null;

					return new FluidSpecificationPublished
					{
						EventNumber = signal.Numero,
						RevisionCode = revisionCode,
						Kind = kind,
						Grade = grade,
						Capacity = capacity,
						CapacityUnit = capacityUnit,
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
