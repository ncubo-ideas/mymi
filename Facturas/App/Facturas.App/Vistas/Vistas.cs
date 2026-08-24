using System;
using System.Collections.Generic;

namespace Facturas.App.Vistas
{
	// Las dos vistas que la app expone. Viven en la App y no en la caratula porque las
	// consumen DOS caratulas: la UI del browser y las tools del servidor MCP. Que la
	// forma de la vista sea una sola es lo que hace que la IA y la persona vean lo
	// mismo.
	//
	// No son el dominio. El dominio tiene Facturario / Factura / Item con su
	// comportamiento; esto es la proyeccion que la app decidio publicar, y por eso
	// puede quedarse corta o de mas respecto del dominio sin arrastrarlo.

	public sealed class FacturaEnLista
	{
		public string Numero { get; init; } = "";
		public DateTime Fecha { get; init; }
		public int Lineas { get; init; }
		public decimal Total { get; init; }
	}

	public sealed class LineaDeFactura
	{
		public string Sku { get; init; } = "";
		public string Descripcion { get; init; } = "";
		public int Cantidad { get; init; }
		public decimal CostoUnitario { get; init; }
		public decimal TotalDeLinea { get; init; }
	}

	public sealed class FacturaDetallada
	{
		public string Numero { get; init; } = "";
		public DateTime Fecha { get; init; }
		public decimal Total { get; init; }
		public IReadOnlyList<LineaDeFactura> Lineas { get; init; } = new List<LineaDeFactura>();
	}
}
