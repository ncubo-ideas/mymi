using System.Text.Json;
using Puppets.Portador;

namespace DealershipCar.App.Events
{
	/// <summary>
	/// El fabricante PUBLICO, dentro de una revision, un sistema que declara adentro del alcance de la
	/// garantia, con sus limites propios si los publico. El codigo del sistema es del vocabulario del
	/// FABRICANTE, asi que es texto con catalogo y no un miembro de ningun conjunto cerrado.
	/// </summary>
	public sealed class CoveredSystemPublished : IMensajeDeEvento
	{
		public const string TheEventType = "CoveredSystemPublished";

		private CoveredSystemPublished() { }

		public long EventNumber { get; private set; }

		public string RevisionCode { get; private set; }

		public string SystemCode { get; private set; }

		/// <summary>
		/// La mitad de plazo del limite propio. Un CERO EN LAS DOS MITADES es como se dice que el
		/// sistema no publico limites propios, y esa traduccion es del dominio: aca viaja tal cual.
		/// </summary>
		public int OwnLimitMonths { get; private set; }

		/// <summary>La mitad de distancia del limite propio, con la misma lectura del cero.</summary>
		public int OwnLimitDistance { get; private set; }

		public string OwnLimitUnit { get; private set; }

		long IMensajeDeEvento.Evento { get { return EventNumber; } }

		string IMensajeDeEvento.Tipo { get { return TheEventType; } }

		/// <summary>
		/// Lee la senal y contesta el hecho, o null para DESCARTARLA. Las dos mitades del limite se
		/// exigen PRESENTES, porque el cero de las dos juntas es lo que dice que no hay limite propio y
		/// omitir el campo no lo dice. Un limite negativo ya estaria cumplido antes de empezar.
		/// Descartar aca no cuesta ninguna entrada.
		/// </summary>
		public static CoveredSystemPublished Interpret(SenalDeEvento signal)
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

					string systemCode = Payload.Text(root, "systemCode");
					if (systemCode.Trim().Length == 0) return null;

					string ownLimitUnit = Payload.Text(root, "ownLimitUnit");
					if (!WireName.NamesADistanceUnit(ownLimitUnit)) return null;

					int ownLimitMonths;
					if (!Payload.Whole(root, "ownLimitMonths", out ownLimitMonths)) return null;

					int ownLimitDistance;
					if (!Payload.Whole(root, "ownLimitDistance", out ownLimitDistance)) return null;

					if (ownLimitMonths < 0 || ownLimitDistance < 0) return null;

					return new CoveredSystemPublished
					{
						EventNumber = signal.Numero,
						RevisionCode = revisionCode,
						SystemCode = systemCode,
						OwnLimitMonths = ownLimitMonths,
						OwnLimitDistance = ownLimitDistance,
						OwnLimitUnit = ownLimitUnit,
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
