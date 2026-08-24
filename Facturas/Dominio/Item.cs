using System;

namespace Facturas.Dominio
{
	// Una linea de la factura. El total de la linea no se recibe: se deriva de
	// cantidad y costo unitario, asi que no puede llegar inconsistente desde el
	// evento del proveedor.
	public class Item
	{
		public string Sku { get; private set; }
		public string Descripcion { get; private set; }
		public int Cantidad { get; private set; }
		public decimal CostoUnitario { get; private set; }

		public decimal Total => Cantidad * CostoUnitario;

		public Item(string sku, string descripcion, int cantidad, decimal costoUnitario)
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
