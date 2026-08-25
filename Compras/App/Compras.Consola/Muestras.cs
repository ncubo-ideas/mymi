using Puppets.Portador;

namespace Compras.Consola
{
	// EL GUION DE LA DEMO, en la MISMA forma de wire que emite el mock del supermercado.
	// Se usa para medir el dominio sin depender de que docker este arriba: entran por la
	// misma ruta (SenalDeEvento -> RutaDeCompras -> comando) que los reales.
	//
	// Las fechas son LITERALES, no "hoy menos un ano". Es obligatorio: el dominio no lee
	// el reloj, y un guion con fechas moviles no es reproducible.
	//
	// La historia que cuentan estas seis compras (y por que los datos se prestan para
	// ser analizados despues):
	//
	//   agosto 2025      la canasta base: pollo, pescado, verduras, granos, lacteos y UN
	//                    aceite. Proporcion ocasional ~9%.
	//   feb-jul 2026     el deslizamiento: entran helado, sodas, galletas, snacks.
	//   mayo 2026        LA COMPRA DE LA GARANTIA: la olla arrocera (12 meses), con su
	//                    fecha y su respaldo. Es la respuesta a "¿hasta cuando tiene
	//                    garantia mi olla arrocera?".
	//   agosto 2026      la canasta ya es ~97% ocasional.
	//
	// El cruce preplanificado del milestone: el laboratorio siembra colesterol 200
	// (ago-2025) -> 280 (ago-2026). La comparacion de canastas 2025-08 vs 2026-08 es la
	// otra mitad de esa historia — "el colesterol alto es por culpa del helado" — y la
	// arma el ASISTENTE, que es el unico que ve los dos negocios.
	internal static class Muestras
	{
		internal static SenalDeEvento[] LaHistoria() => new[]
		{
			// --- agosto 2025: la canasta base ---
			Compra(301, "T-88101", "2025-08-05T17:20:00",
				Linea("POLLO-KG", "Pechuga de pollo kg", 2, "4200", "Proteinas"),
				Linea("FRIJOL-900", "Frijoles negros 900g", 2, "1150", "GranosYAbarrotes"),
				Linea("LECHE-1L", "Leche semidescremada 1L", 6, "950", "LacteosYHuevos"),
				Linea("BROCOLI", "Brocoli unidad", 3, "890", "FrutasYVerduras")),

			Compra(302, "T-88240", "2025-08-20T18:05:00",
				Linea("ACEITE-GIR-1L", "Aceite de girasol 1L", 1, "2890", "AceitesYGrasas"),
				Linea("ARROZ-2KG", "Arroz grano entero 2kg", 1, "2450", "GranosYAbarrotes"),
				Linea("HUEVOS-30", "Huevos carton 30", 1, "3300", "LacteosYHuevos"),
				Linea("TILAPIA-KG", "Filet de tilapia kg", 1, "5200", "Proteinas")),

			// --- febrero 2026: empieza el deslizamiento ---
			Compra(303, "T-91515", "2026-02-14T19:40:00",
				Linea("HELADO-1L", "Helado de vainilla 1L", 2, "3450", "Azucarados"),
				Linea("SODA-2L", "Soda cola 2L", 4, "1750", "Azucarados"),
				Linea("GALLETA-CHOC", "Galletas de chocolate", 3, "1250", "Azucarados"),
				Linea("POLLO-KG", "Pechuga de pollo kg", 1, "4350", "Proteinas")),

			// --- mayo 2026: LA COMPRA DE LA GARANTIA ---
			Compra(304, "T-93872", "2026-05-10T10:15:00",
				LineaConGarantia("OLLA-ARR-18", "Olla arrocera 1.8L", 1, "45900", "Electrohogar", 12),
				Linea("ARROZ-2KG", "Arroz grano entero 2kg", 2, "2520", "GranosYAbarrotes"),
				Linea("MANTECA-500", "Manteca de palma 500g", 1, "1980", "AceitesYGrasas")),

			// --- julio 2026 ---
			Compra(305, "T-95104", "2026-07-03T20:10:00",
				Linea("SODA-12PK", "Soda cola lata 12-pack", 2, "5990", "Azucarados"),
				Linea("HELADO-1L", "Helado de vainilla 1L", 2, "3560", "Azucarados"),
				Linea("PAPAS-FAM", "Papas fritas familiar", 3, "2100", "Snacks"),
				Linea("DETERG-5L", "Detergente liquido 5L", 1, "6800", "Limpieza")),

			// --- agosto 2026: la canasta ya se volteo ---
			Compra(306, "T-96470", "2026-08-15T18:50:00",
				Linea("HELADO-1L", "Helado de vainilla 1L", 3, "3680", "Azucarados"),
				Linea("SODA-2L", "Soda cola 2L", 6, "1790", "Azucarados"),
				Linea("CHOC-BARRA", "Chocolate de leche barra", 4, "1450", "Azucarados"),
				Linea("MANTECA-500", "Manteca de palma 500g", 2, "2050", "AceitesYGrasas"),
				Linea("BROCOLI", "Brocoli unidad", 1, "920", "FrutasYVerduras")),
		};

		// Lo que la historia tiene que dar. Son los criterios de la fase 1: si esto no
		// sale asi, el dominio no esta contando la historia que la demo promete.
		internal const int ComprasEsperadas = 6;
		internal const string TotalEsperado = "172630";
		internal const string NumeroDeLaCompraDeLaGarantia = "T-93872";
		internal const string ProductoConGarantia = "olla";

		// Una senal de un tipo que esta app no consume. La ruta debe devolver null y el
		// bucle descartarla, sin inventar un comando para tragarsela.
		internal static SenalDeEvento TipoDesconocido() =>
			new SenalDeEvento(307, "DevolucionAceptada", @"{ ""id"": 307, ""tipo"": ""DevolucionAceptada"" }");

		// Una CompraRegistrada con una categoria que la app NO modela. Tambien se
		// descarta ENTERA: una compra no se aplica a medias.
		internal static SenalDeEvento CategoriaNoModelada() =>
			new SenalDeEvento(308, "CompraRegistrada", @"{
				""id"": 308, ""tipo"": ""CompraRegistrada"",
				""numero"": ""T-99999"", ""fecha"": ""2026-08-16T10:00:00"",
				""skus"": [""PEZ-BETA""], ""descripciones"": [""Pez beta""],
				""cantidades"": [1], ""preciosUnitarios"": [9500],
				""categorias"": [""Mascotas""], ""mesesDeGarantia"": [0]
			}");

		// -------------------------------------------------------------- constructores

		private static SenalDeEvento Compra(long id, string numero, string fecha, params string[][] lineas)
		{
			var skus = new string[lineas.Length];
			var descripciones = new string[lineas.Length];
			var cantidades = new string[lineas.Length];
			var precios = new string[lineas.Length];
			var categorias = new string[lineas.Length];
			var meses = new string[lineas.Length];

			for (int i = 0; i < lineas.Length; i++)
			{
				skus[i] = $@"""{lineas[i][0]}""";
				descripciones[i] = $@"""{lineas[i][1]}""";
				cantidades[i] = lineas[i][2];
				precios[i] = lineas[i][3];
				categorias[i] = $@"""{lineas[i][4]}""";
				meses[i] = lineas[i][5];
			}

			return new SenalDeEvento(id, "CompraRegistrada", $@"{{
				""id"": {id}, ""tipo"": ""CompraRegistrada"",
				""numero"": ""{numero}"",
				""fecha"": ""{fecha}"",
				""skus"": [{string.Join(", ", skus)}],
				""descripciones"": [{string.Join(", ", descripciones)}],
				""cantidades"": [{string.Join(", ", cantidades)}],
				""preciosUnitarios"": [{string.Join(", ", precios)}],
				""categorias"": [{string.Join(", ", categorias)}],
				""mesesDeGarantia"": [{string.Join(", ", meses)}]
			}}");
		}

		private static string[] Linea(string sku, string descripcion, int cantidad, string precio, string categoria) =>
			new[] { sku, descripcion, cantidad.ToString(), precio, categoria, "0" };

		private static string[] LineaConGarantia(string sku, string descripcion, int cantidad, string precio,
			string categoria, int mesesDeGarantia) =>
			new[] { sku, descripcion, cantidad.ToString(), precio, categoria, mesesDeGarantia.ToString() };
	}
}
