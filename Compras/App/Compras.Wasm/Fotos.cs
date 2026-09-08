using System.Collections.Generic;

namespace Compras.Wasm
{
	/// <summary>Qué imagen le toca a cada producto.</summary>
	// VIVE ACA Y NO EN EL DOMINIO porque no es un hecho de la compra: es una decision de
	// pantalla. El dominio sabe que se llevo un "Aceite de girasol 1L"; que eso se dibuje
	// con una botella amarilla es cosa de quien pinta.
	//
	// Y VIVE EN UN SOLO LUGAR desde que son treinta y tres. Antes el diccionario estaba
	// COPIADO en MisCompras y en Infaltables -trece entradas cada uno- con un comentario
	// que decia que le tocaba un componente cuando apareciera la tercera pagina. No
	// aparecio una tercera pagina: aparecieron veinte productos mas, y una tabla de
	// treinta y tres lineas duplicada es peor que una de trece. La señal era el tamaño,
	// no el numero de paginas.
	public static class Fotos
	{
		// LA LLAVE ES EL SKU y no la descripcion: el super escribe "Leche semidescremada 1L"
		// y "LECHE SEMIDESC 1 LT" para lo mismo, y el sku es lo unico estable. Es la unica
		// parte de la UI que toca la llave interna — nunca sale a la pantalla ni a una url.
		private static readonly Dictionary<string, string> PorSku = new()
		{
			// frutas y verduras
			["BANANO-KG"] = "p-banano",
			["TOMATE-KG"] = "p-tomate",
			["BROCOLI"] = "p-brocoli",
			["ESPINACA"] = "p-espinaca",
			["LECHUGA"] = "p-lechuga",
			["CHILE-DULCE"] = "p-chile",

			// lacteos y huevos
			["LECHE-1L"] = "p-leche",
			["HUEVOS-30"] = "p-huevos",
			["QUESO-500"] = "p-queso",
			["QUESO-CREMA"] = "p-queso",
			["MANTEQUILLA-250"] = "p-mantequilla",

			// proteinas
			["POLLO-KG"] = "p-pollo",
			["TILAPIA-KG"] = "p-tilapia",
			["CARNE-MOL"] = "p-carne",

			// embutidos
			["JAMON-200"] = "p-jamon",
			["SALCHICHA-PK"] = "p-salchicha",
			["MORTADELA-250"] = "p-mortadela",

			// granos y abarrotes
			["ARROZ-2KG"] = "p-arroz",
			["FRIJOL-900"] = "p-frijol",
			["PAN-CUADRADO"] = "p-pan",
			// LA PASTA SALE DE LA SEGUNDA HOJA, no de la primera: ver
			// generar-fotos-2.py, caja #1 ("Spaghetti"). La primera hoja no traia
			// ningun espagueti, y este era el unico producto del catalogo sin foto.
			["PASTA-500"] = "p-espagueti",

			// aceites
			["ACEITE-GIR-1L"] = "p-aceite",

			// dulces, snacks y bebidas
			["HELADO-1L"] = "p-helado",
			["GALLETA-CHOC"] = "p-galletas",
			["PAPAS-FAM"] = "p-papas",
			["SODA-2L"] = "p-soda",
			["AGUA-6PK"] = "p-agua",

			// comidas preparadas
			["POLLO-ROST"] = "p-rostizado",
			["PIZZA-CONG"] = "p-pizza",
			["LASAGNA-CONG"] = "p-lasagna",

			// limpieza
			["DETERG-5L"] = "p-detergente",
			["CLORO-1L"] = "p-cloro",

			// mascotas
			["PERRO-CROQ-10KG"] = "p-perro",

			// electrohogar
			["OLLA-ARR-18"] = "p-olla",
		};

		/// <summary>La ruta de la imagen, o vacío si a ese producto no le toca ninguna.</summary>
		// DEVUELVE VACIO Y NO NULL, igual que las lecturas del dominio: quien pinta pregunta
		// `.Length > 0` y no tiene que acordarse de un caso nulo. Un producto sin foto no es
		// un error — es un producto que todavia no tiene foto, y la pantalla dibuja su caja
		// punteada. Con la segunda hoja ya no queda ninguno sin foto en el catalogo actual.
		public static string De(string sku) =>
			sku is not null && PorSku.TryGetValue(sku, out string nombre) ? $"assets/{nombre}.png" : "";
	}
}
