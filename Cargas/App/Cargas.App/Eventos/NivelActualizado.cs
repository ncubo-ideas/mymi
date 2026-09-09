using System.Text.Json;
using Puppets.Portador;

namespace Cargas.App.Eventos
{
	// La institucion cambio el nivel de lealtad. El nivel lo decide su programa; la app
	// solo lo registra, y descarta uno que no modela.
	public sealed class NivelActualizado : IMensajeDeEvento
	{
		public const string TipoDeEvento = "NivelActualizado";

		public long Evento { get; private set; }
		public string Tipo => TipoDeEvento;

		public string Nivel { get; private set; }

		private NivelActualizado() { }

		public static NivelActualizado Interpretar(SenalDeEvento senal)
		{
			if (senal.Tipo != TipoDeEvento) return null;
			if (senal.Contenido.Length == 0) return null;

			try
			{
				using JsonDocument documento = JsonDocument.Parse(senal.Contenido);

				string nivel = documento.RootElement.TryGetProperty("nivel", out JsonElement e)
					&& e.ValueKind == JsonValueKind.String ? e.GetString() : "";

				// El nivel que esta app no modela ya no se descarta aca: reconocerlo
				// obligaba a nombrar NivelDeLealtad en C#. El Check de CargasAPI lo rechaza
				// adentro del actor, sin dejar entrada en el journal.
				if (nivel.Length == 0) return null;

				return new NivelActualizado { Evento = senal.Numero, Nivel = nivel };
			}
			catch (JsonException)
			{
				return null;
			}
		}
	}
}
