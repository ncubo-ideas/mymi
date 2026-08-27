using System;
using System.Collections.Generic;
using System.Text.Json;
using Compras.Dominio;
using Puppets.Portador;

namespace Compras.App.Eventos
{
	// El unico evento de este negocio: el supermercado registro una compra a nombre del
	// usuario.
	//
	// Forma de IDispatchMessage: un tag de routing compacto y un Interpretar que entiende
	// SU propio wire. El tag es "CompraRegistrada" y el layout es el JSON del proveedor;
	// ninguno de los dos aparece en el texto de un comando.
	//
	// Este tipo es la frontera: de aca para adentro no entra nada del payload del
	// proveedor. Solo se usa como ENTRADA de un PerformCommand, nunca se le muestra a la
	// UI - lo que la pantalla ve sale siempre de un PerformQuery sobre el actor.
	//
	// Las CATEGORIAS que esta app sabe aplicar estan listadas aca, como los examenes en
	// el negocio del laboratorio: el supermercado puede emitir categorias que esta app
	// no modela, y un evento que traiga una se descarta ENTERO en la ruta — una compra
	// no se aplica a medias.
	public sealed class CompraRegistrada : IMensajeDeEvento
	{
		public const string TipoDeEvento = "CompraRegistrada";

		public long Evento { get; private set; }
		public string Tipo => TipoDeEvento;

		public string Numero { get; private set; }
		public DateTime Fecha { get; private set; }
		public string[] Skus { get; private set; }
		public string[] Descripciones { get; private set; }
		public int[] Cantidades { get; private set; }
		public decimal[] PreciosUnitarios { get; private set; }
		public string[] Categorias { get; private set; }
		public int[] MesesDeGarantia { get; private set; }

		private CompraRegistrada() { }

		// Devuelve null si la senal no es una CompraRegistrada bien formada, o si trae
		// una categoria que esta app no modela. El bucle DESCARTA un null; no inventa un
		// comando para tragarse la senal.
		public static CompraRegistrada Interpretar(SenalDeEvento senal)
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
				string[] categorias = Textos(raiz, "categorias");
				int[] meses = Enteros(raiz, "mesesDeGarantia");

				if (skus.Length == 0) return null;
				if (descripciones.Length != skus.Length) return null;
				if (cantidades.Length != skus.Length) return null;
				if (precios.Length != skus.Length) return null;
				if (categorias.Length != skus.Length) return null;
				if (meses.Length != skus.Length) return null;

				foreach (string categoria in categorias)
					if (!EsCategoriaConocida(categoria)) return null;

				return new CompraRegistrada
				{
					Evento = senal.Numero,
					Numero = numero,
					Fecha = fecha,
					Skus = skus,
					Descripciones = descripciones,
					Cantidades = cantidades,
					PreciosUnitarios = precios,
					Categorias = categorias,
					MesesDeGarantia = meses,
				};
			}
			catch (JsonException)
			{
				return null;
			}
		}

		// LO PREGUNTA AL DOMINIO, que es quien posee el conjunto. Aca habia una copia de
		// los nueve nombres, con el argumento de "descartar en la frontera sin referenciar
		// al dominio desde el wire" — y el argumento era falso: este ensamblado YA
		// referencia Compras.Dominio, porque es el que consulta el historial.
		//
		// La copia se desincronizo y costo caro: al agregar mascotas, embutidos y comidas
		// preparadas, el borde descarto 47 de 87 compras EN SILENCIO. Ni un error, ni un
		// log: solo un historial que empezaba en 2026 sin que nada dijera por que. Una
		// lista espejo mantenida a mano no falla haciendo ruido, falla callada.
		private static bool EsCategoriaConocida(string categoria) => Categoria.Existe(categoria);

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
