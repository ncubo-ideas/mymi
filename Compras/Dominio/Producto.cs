using System;

namespace Compras.Dominio
{
	/// <summary>
	/// Lo que el supermercado vende, visto desde el historial de la persona: su sku, cómo
	/// se llama, a qué categoría pertenece y cómo se vende.
	/// </summary>
	// EXISTE PORQUE LA CATEGORIA NO ES DE LA COMPRA. El helado es azucarado siempre, no
	// segun el ticket en que aparezca: que la categoria llegara por linea permitia que el
	// mismo sku entrara como Azucarados en agosto y como Proteinas en setiembre, y ahi la
	// canasta de un mes deja de ser comparable con la de otro sin que nada avise.
	//
	// Identidad por SKU. Dos lineas del mismo sku son el mismo producto, aunque su
	// descripcion o su precio cambien.
	public class Producto
	{
		public string Sku { get; private set; }
		public Categoria Categoria { get; private set; }
		public Presentacion Presentacion { get; private set; }

		/// <summary>
		/// Como lo nombra el supermercado. Puede AFINARSE con el tiempo -"Leche 1L" pasa a
		/// "Leche semidescremada 1L"- y eso no es un conflicto: es el mismo producto mejor
		/// descrito, y gana la ultima forma en que se lo nombro.
		/// </summary>
		public string Descripcion { get; private set; }

		internal Producto(string sku, string descripcion, Categoria categoria, Presentacion presentacion)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(sku);
			ArgumentNullException.ThrowIfNull(descripcion);
			ArgumentNullException.ThrowIfNull(categoria);
			ArgumentNullException.ThrowIfNull(presentacion);

			Sku = sku;
			Descripcion = descripcion;
			Categoria = categoria;
			Presentacion = presentacion;
		}

		public bool EsElMismo(string sku)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(sku);
			return string.Equals(Sku, sku, StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Lo que una línea nueva del mismo sku puede cambiar sin contradecir la historia:
		/// solo cómo se lo nombra.
		/// </summary>
		internal void Renombrar(string descripcion)
		{
			ArgumentNullException.ThrowIfNull(descripcion);
			if (descripcion.Length == 0) return;

			Descripcion = descripcion;
		}

		// Lo que NO tiene metodo, y es a proposito: no hay Recategorizar. Un comercio
		// puede recategorizar un producto de verdad, pero eso cambiaria el significado de
		// las canastas ya leidas -el mismo helado contaria como proteina en la historia
		// vieja-, asi que no puede entrar por la puerta de una compra: seria un verbo
		// propio y journalizado, con su propia entrada. Queda NOMBRADO y sin construir.
	}
}
