using System;
using System.Collections.Generic;

namespace Facturas.Dominio
{
	// Una factura con sus lineas. El constructor recibe las listas PARALELAS tal
	// como vienen en el evento (sku, descripcion, cantidad, precio unitario) y las
	// arma en Items validando que tengan el mismo largo: es el unico punto donde
	// cuatro listas sueltas se vuelven una abstraccion, y por eso es donde se
	// rechaza un evento mal formado.
	internal class Factura
	{
		private readonly List<Item> items = new List<Item>();

		internal string Numero { get; private set; }
		internal DateTime Fecha { get; private set; }

		internal IReadOnlyList<Item> Items => items;

		internal int CantidadDeLineas => items.Count;

		internal decimal Total
		{
			get
			{
				decimal total = 0m;
				foreach (Item item in items) total = total + item.Total;
				return total;
			}
		}

		internal Factura(string numero, DateTime fecha, string[] skus, string[] descripciones,
			int[] cantidades, decimal[] preciosUnitarios)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(numero);
			ArgumentNullException.ThrowIfNull(skus);
			ArgumentNullException.ThrowIfNull(descripciones);
			ArgumentNullException.ThrowIfNull(cantidades);
			ArgumentNullException.ThrowIfNull(preciosUnitarios);

			if (skus.Length == 0)
				throw new ArgumentException("Una factura sin lineas no es una factura.", nameof(skus));
			if (descripciones.Length != skus.Length || cantidades.Length != skus.Length || preciosUnitarios.Length != skus.Length)
				throw new ArgumentException(
					$"Las cuatro listas de la factura '{numero}' deben tener el mismo largo. skus={skus.Length}, descripciones={descripciones.Length}, cantidades={cantidades.Length}, preciosUnitarios={preciosUnitarios.Length}.",
					nameof(skus));

			Numero = numero;
			Fecha = fecha;

			for (int i = 0; i < skus.Length; i++)
				items.Add(new Item(skus[i], descripciones[i], cantidades[i], preciosUnitarios[i]));
		}
	}
}
