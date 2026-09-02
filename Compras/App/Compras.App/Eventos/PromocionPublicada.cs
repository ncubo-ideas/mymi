using System;
using System.Text.Json;
using Puppets.Portador;

namespace Compras.App.Eventos
{
	// El segundo evento del negocio: el supermercado publicó una promoción. Viaja por el
	// MISMO stream de URL2 que las compras -numerado por la misma secuencia- y por eso
	// comparte checkpoint: la bitácora avanza igual, venga lo que venga.
	//
	// Que las promociones lleguen COMO EVENTOS y no de un endpoint en vivo es la decision
	// que el equipo tomo sobre la maqueta: son copia de ella en su journal, funcionan sin
	// conexion, y el super nunca se entera de cuales vio.
	public sealed class PromocionPublicada : IMensajeDeEvento
	{
		public const string TipoDeEvento = "PromocionPublicada";

		public long Evento { get; private set; }
		public string Tipo => TipoDeEvento;

		public string Gancho { get; private set; }
		public string Detalle { get; private set; }

		/// <summary>El sku al que apunta, o vacío si es general (un pasillo, una rifa).</summary>
		public string Sku { get; private set; }

		public DateTime ValidaHasta { get; private set; }

		private PromocionPublicada() { }

		// Devuelve null si la senal no es una PromocionPublicada bien formada. El bucle
		// DESCARTA un null; no inventa un comando para tragarse la senal.
		public static PromocionPublicada Interpretar(SenalDeEvento senal)
		{
			if (senal.Tipo != TipoDeEvento) return null;
			if (senal.Contenido.Length == 0) return null;

			try
			{
				using JsonDocument documento = JsonDocument.Parse(senal.Contenido);
				JsonElement raiz = documento.RootElement;

				string gancho = Texto(raiz, "gancho");
				string detalle = Texto(raiz, "detalle");
				if (gancho.Length == 0 || detalle.Length == 0) return null;

				// SIN FECHA NO HAY CARTEL. El equipo pidio "validas hasta tal fecha", y un
				// cartel sin vencimiento no se puede ordenar ni retirar: se descarta entero,
				// como una compra con una categoria desconocida.
				if (!raiz.TryGetProperty("validaHasta", out JsonElement hastaElemento)
					|| !hastaElemento.TryGetDateTime(out DateTime validaHasta)) return null;

				return new PromocionPublicada
				{
					Evento = senal.Numero,
					Gancho = gancho,
					Detalle = detalle,
					Sku = Texto(raiz, "sku"),
					ValidaHasta = validaHasta,
				};
			}
			catch (JsonException)
			{
				return null;
			}
		}

		private static string Texto(JsonElement raiz, string nombre) =>
			raiz.TryGetProperty(nombre, out JsonElement e) && e.ValueKind == JsonValueKind.String
				? e.GetString() : "";
	}
}
