using System;
using Puppets.Portador;

namespace Facturas.Consola
{
	// Tres eventos fijos, en la MISMA forma de wire que emite el mock del proveedor.
	// Se usan para medir el dominio sin depender de que docker este arriba: entran
	// por la misma ruta (SenalDeEvento -> RutaDeFacturas -> comando) que los reales.
	internal static class Muestras
	{
		internal static SenalDeEvento[] TresFacturas() => new[]
		{
			new SenalDeEvento(101, "FacturaAgregada", @"{
				""id"": 101, ""tipo"": ""FacturaAgregada"",
				""numero"": ""TQ-0001"", ""fecha"": ""2026-08-03T10:14:00"",
				""skus"":             [""ARR-1KG"", ""LEC-1L""],
				""descripciones"":    [""Arroz 1kg"", ""Leche 1L""],
				""cantidades"":       [2, 3],
				""preciosUnitarios"": [1450.00, 890.50]
			}"),
			new SenalDeEvento(102, "FacturaAgregada", @"{
				""id"": 102, ""tipo"": ""FacturaAgregada"",
				""numero"": ""TQ-0002"", ""fecha"": ""2026-08-07T18:32:00"",
				""skus"":             [""CAF-500G""],
				""descripciones"":    [""Cafe molido 500g""],
				""cantidades"":       [1],
				""preciosUnitarios"": [4750.00]
			}"),
			new SenalDeEvento(103, "FacturaAgregada", @"{
				""id"": 103, ""tipo"": ""FacturaAgregada"",
				""numero"": ""TQ-0003"", ""fecha"": ""2026-08-12T09:05:00"",
				""skus"":             [""PAN-INT"", ""HUE-12"", ""ACE-900ML""],
				""descripciones"":    [""Pan integral"", ""Huevos x12"", ""Aceite 900ml""],
				""cantidades"":       [1, 2, 1],
				""preciosUnitarios"": [1200.00, 2350.00, 3100.00]
			}"),
		};

		// 2*1450 + 3*890.50 = 2900 + 2671.50 = 5571.50
		// 1*4750                                = 4750.00
		// 1200 + 2*2350 + 3100                  = 9000.00
		//                                  total = 19321.50
		internal const decimal TotalEsperado = 19321.50m;

		// Una senal de un tipo que APP1 no consume. La ruta debe devolver null y el
		// bucle descartarla, sin inventar un comando para tragarsela.
		internal static SenalDeEvento TipoDesconocido() =>
			new SenalDeEvento(104, "PagoRegistrado", @"{ ""id"": 104, ""tipo"": ""PagoRegistrado"" }");
	}
}
