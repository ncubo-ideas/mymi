using System;

namespace Compras.Dominio
{
	/// <summary>
	/// Una compra en la que apareció lo que se buscaba, y cuánto de eso entró ahí.
	/// </summary>
	// LLEVA LAS DOS ESCALAS a proposito: lo del producto buscado -unidades y gasto- y lo de
	// la compra entera -lineas y total-. Sin las del producto, un resultado de busqueda es
	// indistinguible de una fila cualquiera de la lista y no dice POR QUE aparecio; sin las
	// de la compra, la fila no se puede pintar igual que las de la lista y el resultado se
	// ve como otra pantalla.
	public class CompraConElProducto
	{
		public string Numero { get; private set; }
		public DateTime Fecha { get; private set; }

		/// <summary>En qué sucursal fue, o vacío si el tiquete no lo dijo.</summary>
		public string Sucursal { get; private set; }

		/// <summary>Cuántas unidades de lo buscado entraron en ESTA compra.</summary>
		public int Unidades { get; private set; }

		/// <summary>Cuánto se gastó en lo buscado en ESTA compra, no el total del ticket.</summary>
		public decimal Gasto { get; private set; }

		/// <summary>Cuántas líneas tiene la compra entera.</summary>
		public int LineasDeLaCompra { get; private set; }

		public decimal TotalDeLaCompra { get; private set; }

		internal CompraConElProducto(Compra compra)
		{
			ArgumentNullException.ThrowIfNull(compra);

			Numero = compra.Numero;
			Fecha = compra.Fecha;
			Sucursal = compra.Sucursal;
			LineasDeLaCompra = compra.Items.Count;
			TotalDeLaCompra = compra.Total;
		}

		/// <summary>Como aaaa-mm-dd, que es la forma en que la fecha viaja.</summary>
		public string FechaExpresada => Fecha.ToString("yyyy-MM-dd");

		internal void Sumar(int unidades, decimal gasto)
		{
			Unidades = Unidades + unidades;
			Gasto = Gasto + gasto;
		}
	}
}
