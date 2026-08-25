using System;

namespace Compras.Dominio
{
	/// <summary>
	/// Una linea del ticket: que se compro, cuanto, a que precio, de que categoria y con
	/// que garantia. El total se deriva de cantidad y precio, asi que no puede llegar
	/// inconsistente. La garantia nunca es null: lo que no trae cobertura porta
	/// Garantia.Ninguna, y TieneGarantia existe para preguntar antes de leer.
	/// </summary>
	public class Item
	{
		public string Sku { get; private set; }
		public string Descripcion { get; private set; }
		public int Cantidad { get; private set; }
		public decimal PrecioUnitario { get; private set; }
		public Categoria Categoria { get; private set; }
		public Garantia Garantia { get; private set; }

		public decimal Total => Cantidad * PrecioUnitario;

		public bool TieneGarantia => Garantia.Existe();

		// mesesDeGarantia = 0 significa SIN garantia en el wire; aca se vuelve el tipo:
		// Garantia.Ninguna o una cobertura de verdad.
		public Item(string sku, string descripcion, int cantidad, decimal precioUnitario,
			Categoria categoria, int mesesDeGarantia)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(sku);
			ArgumentNullException.ThrowIfNull(descripcion);
			ArgumentNullException.ThrowIfNull(categoria);
			if (cantidad <= 0)
				throw new ArgumentOutOfRangeException(nameof(cantidad), cantidad,
					"La cantidad de una linea debe ser mayor que cero.");
			if (precioUnitario < 0)
				throw new ArgumentOutOfRangeException(nameof(precioUnitario), precioUnitario,
					"El precio unitario no puede ser negativo.");
			if (mesesDeGarantia < 0)
				throw new ArgumentOutOfRangeException(nameof(mesesDeGarantia), mesesDeGarantia,
					"Los meses de garantia no pueden ser negativos; cero significa sin garantia.");

			Sku = sku;
			Descripcion = descripcion;
			Cantidad = cantidad;
			PrecioUnitario = precioUnitario;
			Categoria = categoria;
			Garantia = mesesDeGarantia == 0 ? Garantia.Ninguna : Garantia.De(mesesDeGarantia);
		}
	}
}
