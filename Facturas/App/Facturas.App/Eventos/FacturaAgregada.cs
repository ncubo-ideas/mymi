using System;
using System.Collections.Generic;
using System.Text.Json;
using Puppets.Portador;

namespace Facturas.App.Eventos
{
	// El unico evento de APP1: el proveedor registro una factura a nombre del usuario.
	//
	// Forma de IDispatchMessage: un tag de routing compacto y un Deserialize que
	// entiende SU propio wire. El tag es "FacturaAgregada" y el layout es el JSON
	// del proveedor; ninguno de los dos aparece en el texto de un comando.
	//
	// Este tipo es la frontera: de aca para adentro no entra nada del payload del
	// proveedor. Solo se usa como ENTRADA de un PerformCommand, nunca se le muestra
	// a la UI - lo que la pantalla ve sale siempre de un PerformQuery sobre el actor.
	public sealed class FacturaAgregada : IMensajeDeEvento
	{
		public const string TipoDeEvento = "FacturaAgregada";

		public long Evento { get; private set; }
		public string Tipo => TipoDeEvento;

		public string Numero { get; private set; }
		public DateTime Fecha { get; private set; }
		public string[] Skus { get; private set; }
		public string[] Descripciones { get; private set; }
		public int[] Cantidades { get; private set; }
		public decimal[] PreciosUnitarios { get; private set; }

		private FacturaAgregada() { }

		// Devuelve null si la senal no es una FacturaAgregada bien formada. El bucle
		// DESCARTA un null; no inventa un comando para tragarse la senal.
		public static FacturaAgregada Interpretar(SenalDeEvento senal)
		{
			if (senal.Tipo != TipoDeEvento) return null;
			if (senal.Contenido.Length == 0) return null;

			try
			{
				using JsonDocument documento = JsonDocument.Parse(senal.Contenido);
				JsonElement raiz = documento.RootElement;

				string numero = Texto(raiz, "numero");
				if (numero.Length == 0) return null;

				if (!raiz.TryGetProperty("fecha", out JsonElement fechaElemento)
					|| !fechaElemento.TryGetDateTime(out DateTime fecha)) return null;

				string[] skus = Textos(raiz, "skus");
				string[] descripciones = Textos(raiz, "descripciones");
				int[] cantidades = Enteros(raiz, "cantidades");
				decimal[] precios = Decimales(raiz, "preciosUnitarios");

				if (skus.Length == 0) return null;
				if (descripciones.Length != skus.Length) return null;
				if (cantidades.Length != skus.Length) return null;
				if (precios.Length != skus.Length) return null;

				return new FacturaAgregada
				{
					Evento = senal.Numero,
					Numero = numero,
					Fecha = fecha,
					Skus = skus,
					Descripciones = descripciones,
					Cantidades = cantidades,
					PreciosUnitarios = precios,
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

		private static string[] Textos(JsonElement raiz, string nombre)
		{
			if (!raiz.TryGetProperty(nombre, out JsonElement arreglo) || arreglo.ValueKind != JsonValueKind.Array)
				return Array.Empty<string>();

			var valores = new List<string>();
			foreach (JsonElement e in arreglo.EnumerateArray())
			{
				if (e.ValueKind != JsonValueKind.String) return Array.Empty<string>();
				valores.Add(e.GetString());
			}
			return valores.ToArray();
		}

		private static int[] Enteros(JsonElement raiz, string nombre)
		{
			if (!raiz.TryGetProperty(nombre, out JsonElement arreglo) || arreglo.ValueKind != JsonValueKind.Array)
				return Array.Empty<int>();

			var valores = new List<int>();
			foreach (JsonElement e in arreglo.EnumerateArray())
			{
				if (!e.TryGetInt32(out int valor)) return Array.Empty<int>();
				valores.Add(valor);
			}
			return valores.ToArray();
		}

		private static decimal[] Decimales(JsonElement raiz, string nombre)
		{
			if (!raiz.TryGetProperty(nombre, out JsonElement arreglo) || arreglo.ValueKind != JsonValueKind.Array)
				return Array.Empty<decimal>();

			var valores = new List<decimal>();
			foreach (JsonElement e in arreglo.EnumerateArray())
			{
				if (!e.TryGetDecimal(out decimal valor)) return Array.Empty<decimal>();
				valores.Add(valor);
			}
			return valores.ToArray();
		}
	}
}
