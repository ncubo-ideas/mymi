using System.Text.Json;
using Puppets.Portador;

namespace Concesionario.App.Events
{
	/// <summary>
	/// El fabricante PUBLICO, dentro de una revision, su regla sobre servicio en otro taller y que
	/// conserva la cobertura. Los dos textos se guardan como se publicaron: sin traducir, sin resumir
	/// y sin reescribir.
	/// </summary>
	public sealed class ServicingPolicyPublished : IMensajeDeEvento
	{
		public const string TheEventType = "ServicingPolicyPublished";

		private ServicingPolicyPublished() { }

		public long EventNumber { get; private set; }

		public string RevisionCode { get; private set; }

		public string Rule { get; private set; }

		public string WhatPreservesCover { get; private set; }

		long IMensajeDeEvento.Evento { get { return EventNumber; } }

		string IMensajeDeEvento.Tipo { get { return TheEventType; } }

		/// <summary>
		/// Lee la senal y contesta el hecho, o null para DESCARTARLA. Las dos mitades son obligatorias:
		/// la regla sola deja a la persona sabiendo que tiene un problema y no que hacer con el.
		/// Descartar aca no cuesta ninguna entrada.
		/// </summary>
		public static ServicingPolicyPublished Interpret(SenalDeEvento signal)
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

					string rule = Payload.Text(root, "rule");
					if (rule.Trim().Length == 0) return null;

					string whatPreservesCover = Payload.Text(root, "whatPreservesCover");
					if (whatPreservesCover.Trim().Length == 0) return null;

					return new ServicingPolicyPublished
					{
						EventNumber = signal.Numero,
						RevisionCode = revisionCode,
						Rule = rule,
						WhatPreservesCover = whatPreservesCover,
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
