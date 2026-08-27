using System;
using System.Collections.Generic;

namespace Compras.Dominio
{
	/// <summary>
	/// Los productos que la persona conoce, armados desde SUS compras. No es el catálogo
	/// del supermercado —ese tiene miles y vive del otro lado— es el de ella: solo lo que
	/// pasó por su canasta alguna vez.
	/// </summary>
	// Es la pieza que vuelve COHERENTE el historial. Cada linea que entra se reconoce
	// contra el catalogo: si el sku es nuevo, se registra con su categoria y su
	// presentacion; si ya estaba, se valida que no se contradiga.
	//
	// Y ES LO QUE HACE POSIBLE UNA PREGUNTA QUE ANTES NO SE PODIA HACER: cuantos productos
	// distintos de una categoria compra esta persona. Eso no esta en ningun ticket.
	public class Catalogo
	{
		private readonly List<Producto> productos = new List<Producto>();
		private readonly Dictionary<string, Producto> porSku =
			new Dictionary<string, Producto>(StringComparer.OrdinalIgnoreCase);

		public IReadOnlyList<Producto> Lista => productos;

		public int Cantidad => productos.Count;

		/// <summary>Reconocimiento total: la pareja del indexador, para preguntar sin reventar.</summary>
		public bool Tiene(string sku)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(sku);
			return porSku.ContainsKey(sku);
		}

		/// <summary>Un sku que nunca se compró es un error de quien pregunta, no un producto vacío.</summary>
		public Producto this[string sku]
		{
			get
			{
				ArgumentNullException.ThrowIfNullOrWhiteSpace(sku);
				if (!porSku.TryGetValue(sku, out Producto producto))
					throw new KeyNotFoundException(
						$"La persona nunca compro el sku '{sku}'. Preguntar Tiene(sku) antes.");

				return producto;
			}
		}

		/// <summary>
		/// Registra el producto de una línea, o valida que no contradiga al que ya estaba.
		/// </summary>
		// LA CATEGORIA Y LA PRESENTACION QUEDAN FIJADAS la primera vez. Un evento posterior
		// que traiga el mismo sku con otra categoria REVIENTA, y eso es deliberado: dejarlo
		// pasar cambiaria el significado de las canastas ya leidas -el mismo helado
		// contaria como proteina en la historia vieja- y nadie se enteraria. Un comercio
		// que de verdad recategoriza necesita su propio verbo journalizado, no colarlo en
		// una compra.
		//
		// La DESCRIPCION si puede afinarse: es el mismo producto mejor nombrado.
		internal Producto Reconocer(Item item)
		{
			ArgumentNullException.ThrowIfNull(item);

			if (!porSku.TryGetValue(item.Sku, out Producto conocido))
			{
				var nuevo = new Producto(item.Sku, item.Descripcion, item.Categoria, item.Presentacion);
				productos.Add(nuevo);
				porSku[item.Sku] = nuevo;
				return nuevo;
			}

			if (!ReferenceEquals(conocido.Categoria, item.Categoria))
				throw new InvalidOperationException(
					$"El sku '{item.Sku}' ya estaba registrado como '{conocido.Categoria.Nombre}' y esta "
					+ $"compra lo trae como '{item.Categoria.Nombre}'. Aceptarlo cambiaria el significado "
					+ "de las canastas ya leidas.");

			conocido.Renombrar(item.Descripcion);
			return conocido;
		}

		/// <summary>Los productos de una categoría que esta persona compra. No está en ningún ticket.</summary>
		public IReadOnlyList<Producto> De(Categoria categoria)
		{
			ArgumentNullException.ThrowIfNull(categoria);

			var suyos = new List<Producto>();
			foreach (Producto producto in productos)
				if (ReferenceEquals(producto.Categoria, categoria)) suyos.Add(producto);

			return suyos;
		}

		/// <summary>Cuántos productos distintos conoce de una categoría.</summary>
		public int CuantosDe(Categoria categoria) => De(categoria).Count;
	}
}
