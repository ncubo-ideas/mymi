using System.Text.Json;
using Puppets.Portador;

namespace DealershipCar.App.Events
{
	/// <summary>
	/// El fabricante PUBLICO, dentro de una revision, algo que el alcance de la garantia NO cubre, con
	/// la condicion bajo la cual muerde. La condicion es texto publicado: este modulo la nombra y no
	/// decide si se cumple en un caso concreto.
	/// </summary>
	public sealed class CoverageExclusionPublished : IMensajeDeEvento
	{
		public const string TheEventType = "CoverageExclusionPublished";

		private CoverageExclusionPublished() { }

		public long EventNumber { get; private set; }

		public string RevisionCode { get; private set; }

		public string SystemCode { get; private set; }

		public string Exclusion { get; private set; }

		public string Condition { get; private set; }

		long IMensajeDeEvento.Evento { get { return EventNumber; } }

		string IMensajeDeEvento.Tipo { get { return TheEventType; } }

		/// <summary>
		/// Lee la senal y contesta el hecho, o null para DESCARTARLA. Las tres partes son obligatorias:
		/// sin sistema no se sabe a que alcanza, sin texto no dice que excluye, y sin condicion la
		/// persona no puede saber si le toca. Descartar aca no cuesta ninguna entrada.
		/// </summary>
		public static CoverageExclusionPublished Interpret(SenalDeEvento signal)
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

					string exclusion = Payload.Text(root, "exclusion");
					if (exclusion.Trim().Length == 0) return null;

					string condition = Payload.Text(root, "condition");
					if (condition.Trim().Length == 0) return null;

					return new CoverageExclusionPublished
					{
						EventNumber = signal.Numero,
						RevisionCode = revisionCode,
						SystemCode = systemCode,
						Exclusion = exclusion,
						Condition = condition,
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
