using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Puppets.Portador;

namespace Compras.Consola
{
	// EL GUION DE LA DEMO, en la MISMA forma de wire que emite el mock del supermercado.
	// Se usa para medir el dominio sin depender de que docker este arriba: entra por la
	// misma ruta (SenalDeEvento -> RutaDeCompras -> comando) que los eventos reales.
	//
	// LEE guion.json, EL MISMO ARCHIVO QUE SIRVE EL MOCK. Antes habia aca una copia a
	// mano de seis compras, y eso es un problema y no una comodidad: las mismas
	// constantes de abajo validan los dos caminos -medir sin mock y refrescar contra el
	// mock-, asi que si los dos guiones difieren, uno de los dos veredictos miente. El
	// csproj enlaza el json de EndPointDeEventos; el build lo copia, nadie lo sincroniza
	// a mano.
	//
	// Las fechas son LITERALES, no "hoy menos un ano". Es obligatorio: el dominio no lee
	// el reloj, y un guion con fechas moviles no es reproducible.
	//
	// La historia que cuentan las 87 compras semanales, y por que los datos se prestan
	// para ser analizados despues:
	//
	//   2025            la canasta de cocinar en casa: frescos todas las semanas, pollo,
	//                   granos, lacteos, y helado UNA vez al mes.
	//   enero 2026      EL QUIEBRE. Entran embutidos y comidas preparadas, el helado pasa
	//                   a semanal y los frescos bajan a quincenales. El total sube 11%:
	//                   lo que entra desplaza a lo que sale.
	//   mayo 2026       LA COMPRA DE LA GARANTIA: la olla arrocera (12 meses), con su
	//                   fecha y su respaldo.
	//
	// Los numeros los fija la wiki (Ecosistema/Preguntas.md, invariantes 4, 13 y 14) y los
	// verifica generar-guion.py, que se cae si uno no cierra.
	internal static class Muestras
	{
		private static SenalDeEvento[] historia;

		internal static SenalDeEvento[] LaHistoria()
		{
			if (historia != null) return historia;

			string ruta = Path.Combine(AppContext.BaseDirectory, "guion.json");
			if (!File.Exists(ruta))
				throw new FileNotFoundException(
					"No esta guion.json al lado del ejecutable. Se genera con "
					+ "'python generar-guion.py' en Compras/EndPointDeEventos.", ruta);

			using JsonDocument documento = JsonDocument.Parse(File.ReadAllText(ruta));

			var senales = new List<SenalDeEvento>();
			foreach (JsonElement compra in documento.RootElement.EnumerateArray())
			{
				long id = compra.GetProperty("id").GetInt64();

				// El wire lleva el tipo y el id ADENTRO del cuerpo, igual que lo emite el
				// mock: la ruta los lee de ahi.
				string cuerpo = $@"{{
				""id"": {id}, ""tipo"": ""CompraRegistrada"",
				""numero"": {Texto(compra, "numero")},
				""fecha"": {Texto(compra, "fecha")},
				""skus"": {Crudo(compra, "skus")},
				""descripciones"": {Crudo(compra, "descripciones")},
				""cantidades"": {Crudo(compra, "cantidades")},
				""preciosUnitarios"": {Crudo(compra, "preciosUnitarios")},
				""categorias"": {Crudo(compra, "categorias")},
				""mesesDeGarantia"": {Crudo(compra, "mesesDeGarantia")}{Sucursal(compra)}
			}}";

				senales.Add(new SenalDeEvento(id, "CompraRegistrada", cuerpo));
			}

			historia = senales.ToArray();
			return historia;
		}

		// La sucursal viaja solo si el guion la trae: el campo es opcional en el
		// contrato, y estas muestras tienen que poder reproducir las dos formas.
		private static string Sucursal(JsonElement compra) =>
			compra.TryGetProperty("sucursal", out JsonElement s) && s.ValueKind == JsonValueKind.String
				? $@", ""sucursal"": {s.GetRawText()}"
				: "";

		private static string Texto(JsonElement compra, string llave) =>
			JsonSerializer.Serialize(compra.GetProperty(llave).GetString());

		private static string Crudo(JsonElement compra, string llave) =>
			compra.GetProperty(llave).GetRawText();

		// Lo que la historia tiene que dar. Son los criterios de la fase 1: si esto no
		// sale asi, el dominio no esta contando la historia que la demo promete.
		internal const int ComprasEsperadas = 87;
		// UN DECIMAL Y NO UN STRING, y la diferencia importa: comparar por texto depende de
		// la ESCALA del decimal, que a su vez depende de como se escribio el json. El guion
		// trae los precios como 990.0, asi que la suma sale con escala 1 y "3222336.0" no es
		// igual a "3222336" — aunque los dos numeros si lo sean.
		//
		// Es la misma trampa que el README ya advierte para el journal, mordiendo esta vez a
		// la comprobacion en vez al dato. Un total se compara como numero.
		internal const decimal TotalEsperado = 3222336m;
		internal const string NumeroDeLaCompraDeLaGarantia = "T-89917";
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
				""skus"": [""MARTILLO""], ""descripciones"": [""Martillo de carpintero""],
				""cantidades"": [1], ""preciosUnitarios"": [9500],
				""categorias"": [""Ferreteria""], ""mesesDeGarantia"": [0]
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
