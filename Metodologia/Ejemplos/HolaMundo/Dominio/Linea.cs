using System;

namespace HolaMundo.Dominio
{
	// UNA LÍNEA DE LA FACTURA.
	//
	// Dos decisiones acá, y las dos se repiten en cualquier dominio que escribas:
	//
	// 1. **`Total` se deriva, no se recibe.** El proveedor podría mandarnos un total y
	//    podría venir mal. Si lo calculamos, no puede llegar inconsistente. Regla
	//    barata: no recibas lo que podés derivar.
	//
	// 2. **El constructor valida.** Una línea con cantidad cero no existe, así que no
	//    se puede construir. El modelo no se defiende de datos malos más tarde: no los
	//    deja entrar.
	internal class Linea
	{
		public string Descripcion { get; }
		public int Cantidad { get; }
		public decimal Precio { get; }

		public decimal Total => Cantidad * Precio;

		public Linea(string descripcion, int cantidad, decimal precio)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(descripcion);
			if (cantidad <= 0)
				throw new ArgumentOutOfRangeException(nameof(cantidad), cantidad,
					"Una línea con cantidad cero o negativa no es una línea.");
			if (precio < 0)
				throw new ArgumentOutOfRangeException(nameof(precio), precio,
					"El precio no puede ser negativo.");

			Descripcion = descripcion;
			Cantidad = cantidad;
			Precio = precio;
		}
	}
}
