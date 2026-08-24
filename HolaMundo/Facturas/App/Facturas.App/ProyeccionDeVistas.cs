using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using Facturas.App.Vistas;

namespace Facturas.App
{
	// Convierte la emision del print en los records de vista.
	//
	// Esto NO es el antipatron de "parsear el print para sacar un valor tipado". Ese
	// antipatron es re-derivar en C# un ESCALAR que el actor ya calculo, y para eso la
	// app usa parametros Out tipados (TotalDeFacturas, CantidadDeFacturas). Lo de aca es
	// otra cosa: una VISTA, y el print con formatter JSON es su canal declarado - la
	// guia dice devolver ese string tal cual porque "el print ya hizo todo el trabajo
	// de serializacion". Un cliente REST hace exactamente esto. La diferencia es que
	// aca el cliente esta en el mismo proceso.
	//
	// El formato de fecha es el del formatter: MM/dd/yyyy HH:mm:ss en cultura
	// invariante.
	internal static class ProyeccionDeVistas
	{
		private const string FormatoDeFecha = "MM/dd/yyyy HH:mm:ss";
		private const string FormatoDeFechaSinHora = "MM/dd/yyyy";

		internal static IReadOnlyList<FacturaEnLista> Lista(string json)
		{
			var facturas = new List<FacturaEnLista>();
			if (string.IsNullOrWhiteSpace(json)) return facturas;

			using JsonDocument documento = JsonDocument.Parse(json);

			// Sin facturas el foreach no emite nada y la propiedad no existe: eso es
			// una lista vacia, no un error.
			if (!documento.RootElement.TryGetProperty("factura", out JsonElement arreglo)) return facturas;

			foreach (JsonElement elemento in Elementos(arreglo))
				facturas.Add(new FacturaEnLista
				{
					Numero = Texto(elemento, "numero"),
					Fecha = Fecha(elemento, "fecha"),
					Lineas = Entero(elemento, "lineas"),
					Total = Decimal(elemento, "total"),
				});

			return facturas;
		}

		internal static FacturaDetallada Detalle(string json)
		{
			if (string.IsNullOrWhiteSpace(json)) return new FacturaDetallada();

			using JsonDocument documento = JsonDocument.Parse(json);
			JsonElement raiz = documento.RootElement;

			var lineas = new List<LineaDeFactura>();
			if (raiz.TryGetProperty("item", out JsonElement items))
				foreach (JsonElement elemento in Elementos(items))
					lineas.Add(new LineaDeFactura
					{
						Sku = Texto(elemento, "sku"),
						Descripcion = Texto(elemento, "descripcion"),
						Cantidad = Entero(elemento, "cantidad"),
						CostoUnitario = Decimal(elemento, "costoUnitario"),
						TotalDeLinea = Decimal(elemento, "totalDeLinea"),
					});

			return new FacturaDetallada
			{
				Numero = Texto(raiz, "numero"),
				Fecha = Fecha(raiz, "fecha"),
				Total = Decimal(raiz, "total"),
				Lineas = lineas,
			};
		}

		// Un solo print emite un objeto; varios emiten un arreglo. Tratar los dos casos
		// evita que la vista se rompa justo cuando hay una sola factura.
		private static IEnumerable<JsonElement> Elementos(JsonElement elemento)
		{
			if (elemento.ValueKind == JsonValueKind.Array)
			{
				foreach (JsonElement hijo in elemento.EnumerateArray()) yield return hijo;
				yield break;
			}
			if (elemento.ValueKind == JsonValueKind.Object) yield return elemento;
		}

		private static string Texto(JsonElement padre, string nombre) =>
			padre.TryGetProperty(nombre, out JsonElement e) && e.ValueKind == JsonValueKind.String
				? e.GetString() : "";

		private static int Entero(JsonElement padre, string nombre) =>
			padre.TryGetProperty(nombre, out JsonElement e) && e.TryGetInt32(out int valor) ? valor : 0;

		private static decimal Decimal(JsonElement padre, string nombre) =>
			padre.TryGetProperty(nombre, out JsonElement e) && e.TryGetDecimal(out decimal valor) ? valor : 0m;

		private static DateTime Fecha(JsonElement padre, string nombre)
		{
			string texto = Texto(padre, nombre);
			if (texto.Length == 0) return default;

			if (DateTime.TryParseExact(texto, FormatoDeFecha, CultureInfo.InvariantCulture,
					DateTimeStyles.None, out DateTime conHora)) return conHora;
			if (DateTime.TryParseExact(texto, FormatoDeFechaSinHora, CultureInfo.InvariantCulture,
					DateTimeStyles.None, out DateTime soloFecha)) return soloFecha;

			return DateTime.TryParse(texto, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime suelta)
				? suelta : default;
		}
	}
}
