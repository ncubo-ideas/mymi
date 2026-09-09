using System;
using System.Globalization;

namespace Compras.Dominio
{
	/// <summary>
	/// Una línea del ticket: qué se compró, cuánto, a qué precio, de qué categoría, cómo
	/// se vende y — solo si el producto la trae — con qué garantía.
	/// </summary>
	// La CANTIDAD sola no dice nada: tres leches de 1 L y tres kilos de carne son el mismo
	// "3". Con la presentacion, la linea sabe cuanto entro de verdad -6 L, 1 kg, 24 u- y
	// cuanto costo la unidad de medida, que es lo unico con lo que se pueden comparar dos
	// presentaciones del mismo producto.
	//
	// NO todo lo que se compra tiene garantia: el helado no la tiene y no la finge.
	internal class Item
	{
		internal string Sku { get; private set; }
		internal string Descripcion { get; private set; }
		internal int Cantidad { get; private set; }
		internal decimal PrecioUnitario { get; private set; }
		internal Categoria Categoria { get; private set; }
		internal Presentacion Presentacion { get; private set; }
		internal Garantia Garantia { get; private set; }

		internal decimal Total => Cantidad * PrecioUnitario;

		internal bool TieneGarantia => Garantia.Existe();

		/// <summary>Cuánto entró a la canasta en la unidad base: 6 L, 1 kg, 24 u.</summary>
		internal decimal TotalEnUnidadBase => Presentacion.TotalEnBase(Cantidad);

		internal UnidadDeVenta UnidadBase => Presentacion.UnidadBase;

		/// <summary>
		/// Cuánto cuesta la unidad de medida — el colón por litro o por kilo. Es lo único
		/// con lo que se pueden comparar dos presentaciones del mismo producto: una leche
		/// de 2 L a ₡1.800 es más barata que dos de 1 L a ₡950.
		/// </summary>
		internal decimal PrecioPorUnidadBase
		{
			get
			{
				decimal enBase = TotalEnUnidadBase;
				if (enBase == 0m)
					throw new InvalidOperationException(
						$"La linea '{Sku}' no tiene cantidad en su unidad base, asi que no tiene precio por unidad.");

				return Total / enBase;
			}
		}

		/// <summary>La cantidad ya dicha, con su unidad: "6 L", "1 kg", "3 u".</summary>
		internal string CantidadExpresada =>
			$"{TotalEnUnidadBase.ToString("0.###", CultureInfo.InvariantCulture)} {UnidadBase.Simbolo}";

		internal Item(string sku, string descripcion, int cantidad, decimal precioUnitario,
			Categoria categoria, int mesesDeGarantia, Presentacion presentacion)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(sku);
			ArgumentNullException.ThrowIfNull(descripcion);
			ArgumentNullException.ThrowIfNull(categoria);
			ArgumentNullException.ThrowIfNull(presentacion);
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
			Presentacion = presentacion;
			Garantia = mesesDeGarantia == 0 ? Garantia.Ninguna : Garantia.De(mesesDeGarantia);
		}

		// Los journals ya escritos tienen la forma SIN presentacion. Este constructor se
		// conserva a proposito para que un historial viejo siga reproduciendo igual: lo que
		// entro sin decir como se vende se lee como piezas sueltas, que es lo que la app
		// asumia cuando se escribio.
		internal Item(string sku, string descripcion, int cantidad, decimal precioUnitario,
			Categoria categoria, int mesesDeGarantia)
			: this(sku, descripcion, cantidad, precioUnitario, categoria, mesesDeGarantia,
				Presentacion.PorUnidad)
		{
		}
	}
}
