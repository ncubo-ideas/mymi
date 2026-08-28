using System.Text.Json;
using Puppets.Portador;

namespace Cargas.App.Eventos
{
	// La institucion acredito puntos de lealtad. Los puntos LLEGAN, no se calculan
	// localmente: si el programa cambia la tasa, el historico no se recalcula, que es
	// lo que significa append-only (§11 del brief).
	public sealed class PuntosAcreditados : IMensajeDeEvento
	{
		public const string TipoDeEvento = "PuntosAcreditados";

		public long Evento { get; private set; }
		public string Tipo => TipoDeEvento;

		public int Puntos { get; private set; }

		private PuntosAcreditados() { }

		public static PuntosAcreditados Interpretar(SenalDeEvento senal)
		{
			if (senal.Tipo != TipoDeEvento) return null;
			if (senal.Contenido.Length == 0) return null;

			try
			{
				using JsonDocument documento = JsonDocument.Parse(senal.Contenido);
				JsonElement raiz = documento.RootElement;

				// El valor y no la presencia: cero puntos no es una acreditacion.
				if (!raiz.TryGetProperty("puntos", out JsonElement elemento)
					|| !elemento.TryGetInt32(out int puntos) || puntos <= 0) return null;

				return new PuntosAcreditados { Evento = senal.Numero, Puntos = puntos };
			}
			catch (JsonException)
			{
				return null;
			}
		}
	}
}
