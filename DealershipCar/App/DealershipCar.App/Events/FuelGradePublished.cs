using System.Text.Json;
using Puppets.Portador;

namespace DealershipCar.App.Events
{
	/// <summary>
	/// El fabricante PUBLICO, dentro de una revision, un grado de combustible con su postura. La
	/// especificacion de combustible de una revision se arma grado a grado, asi que un feed publica
	/// tantos hechos de estos como grados tenga que decir.
	/// </summary>
	public sealed class FuelGradePublished : IMensajeDeEvento
	{
		public const string TheEventType = "FuelGradePublished";

		private FuelGradePublished() { }

		public long EventNumber { get; private set; }

		public string RevisionCode { get; private set; }

		public string Grade { get; private set; }

		public string Standing { get; private set; }

		long IMensajeDeEvento.Evento { get { return EventNumber; } }

		string IMensajeDeEvento.Tipo { get { return TheEventType; } }

		/// <summary>
		/// Lee la senal y contesta el hecho, o null para DESCARTARLA. Un grado sin texto no se puede
		/// pedir en una bomba y una postura que no nombra un miembro del conjunto cerrado se descarta
		/// en vez de tomarse como la benigna. Descartar aca no cuesta ninguna entrada.
		/// </summary>
		public static FuelGradePublished Interpret(SenalDeEvento signal)
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

					string standing = Payload.Text(root, "standing");
					if (!WireName.NamesAFuelGradeStanding(standing)) return null;

					return new FuelGradePublished
					{
						EventNumber = signal.Numero,
						RevisionCode = revisionCode,
						Grade = grade,
						Standing = standing,
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
