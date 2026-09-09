using System.Text.Json;
using Puppets.Portador;

namespace Concesionario.App.Events
{
	/// <summary>
	/// El fabricante SELLO una revision, que es lo unico que la hace contestar. Sellar pasa una sola
	/// vez por revision: un segundo sello solo puede venir de un feed que perdio la cuenta.
	/// </summary>
	public sealed class SpecificationRevisionSealed : IMensajeDeEvento
	{
		public const string TheEventType = "SpecificationRevisionSealed";

		private SpecificationRevisionSealed() { }

		public long EventNumber { get; private set; }

		public string RevisionCode { get; private set; }

		long IMensajeDeEvento.Evento { get { return EventNumber; } }

		string IMensajeDeEvento.Tipo { get { return TheEventType; } }

		/// <summary>
		/// Lee la senal y contesta el hecho, o null para DESCARTARLA. Sin codigo no se sabe que
		/// revision sellar, y descartar aca no cuesta ninguna entrada del journal.
		/// </summary>
		public static SpecificationRevisionSealed Interpret(SenalDeEvento signal)
		{
			if (signal.Tipo != TheEventType) return null;
			if (signal.Numero <= 0) return null;
			if (signal.Contenido.Length == 0) return null;

			try
			{
				using (JsonDocument document = JsonDocument.Parse(signal.Contenido))
				{
					string revisionCode = Payload.Text(document.RootElement, "revisionCode");
					if (revisionCode.Trim().Length == 0) return null;

					return new SpecificationRevisionSealed
					{
						EventNumber = signal.Numero,
						RevisionCode = revisionCode,
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
