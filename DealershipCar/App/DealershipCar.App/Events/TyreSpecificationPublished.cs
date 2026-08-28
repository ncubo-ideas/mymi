using System.Text.Json;
using Puppets.Portador;

namespace DealershipCar.App.Events
{
	/// <summary>
	/// El fabricante PUBLICO, dentro de una revision, que llanta lleva el modelo en una posicion: la
	/// medida, la presion en frio con su unidad, y las clasificaciones. La presion viaja en la unidad
	/// en que se publico y aca no se convierte: quien la lee la va a marcar en un manometro.
	/// </summary>
	public sealed class TyreSpecificationPublished : IMensajeDeEvento
	{
		public const string TheEventType = "TyreSpecificationPublished";

		private TyreSpecificationPublished() { }

		public long EventNumber { get; private set; }

		public string RevisionCode { get; private set; }

		public string Position { get; private set; }

		public string Size { get; private set; }

		public decimal ColdPressure { get; private set; }

		public string PressureUnit { get; private set; }

		public string Ratings { get; private set; }

		long IMensajeDeEvento.Evento { get { return EventNumber; } }

		string IMensajeDeEvento.Tipo { get { return TheEventType; } }

		/// <summary>
		/// Lee la senal y contesta el hecho, o null para DESCARTARLA. Las tres partes publicadas son
		/// obligatorias, la posicion y la unidad tienen que nombrar un miembro de su conjunto cerrado,
		/// y una presion negativa no es una presion. Descartar aca no cuesta ninguna entrada.
		/// </summary>
		public static TyreSpecificationPublished Interpret(SenalDeEvento signal)
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

					string size = Payload.Text(root, "size");
					if (size.Trim().Length == 0) return null;

					string ratings = Payload.Text(root, "ratings");
					if (ratings.Trim().Length == 0) return null;

					string position = Payload.Text(root, "position");
					string pressureUnit = Payload.Text(root, "pressureUnit");

					if (!WireName.NamesATyrePosition(position)) return null;
					if (!WireName.NamesAPressureUnit(pressureUnit)) return null;

					decimal coldPressure;
					if (!Payload.Amount(root, "coldPressure", out coldPressure)) return null;

					if (coldPressure < 0) return null;

					return new TyreSpecificationPublished
					{
						EventNumber = signal.Numero,
						RevisionCode = revisionCode,
						Position = position,
						Size = size,
						ColdPressure = coldPressure,
						PressureUnit = pressureUnit,
						Ratings = ratings,
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
