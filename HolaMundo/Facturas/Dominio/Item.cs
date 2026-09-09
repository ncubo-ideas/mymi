using System;

namespace Facturas.Dominio
{
	// Una linea de la factura. El total de la linea no se recibe: se deriva de
	// cantidad y costo unitario, asi que no puede llegar inconsistente desde el
	// evento del proveedor.
	internal class Item
	{
		internal string Sku { get; private set; }
		internal string Descripcion { get; private set; }
		internal int Cantidad { get; private set; }
		internal decimal CostoUnitario { get; private set; }

		internal decimal Total => Cantidad * CostoUnitario;

		internal Item(string sku, string descripcion, int cantidad, decimal costoUnitario)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(sku);
			ArgumentNullException.ThrowIfNull(descripcion);
			if (cantidad <= 0)
				throw new ArgumentOutOfRangeException(nameof(cantidad), cantidad, "La cantidad de una linea debe ser mayor que cero.");
			if (costoUnitario < 0)
				throw new ArgumentOutOfRangeException(nameof(costoUnitario), costoUnitario, "El costo unitario no puede ser negativo.");

			Sku = sku;
			Descripcion = descripcion;
			Cantidad = cantidad;
			CostoUnitario = costoUnitario;
		}
	}
}
