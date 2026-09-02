using System;

namespace Compras.Dominio
{
	/// <summary>
	/// Cuánto entró de algo en UN año: unidades, gasto, en cuántas compras y en cuántos
	/// meses distintos. Es el mismo punto de serie que <see cref="MesDeConsumo"/>, un
	/// escalón más arriba.
	/// </summary>
	// EXISTE PORQUE LA SERIE MENSUAL CRECE PARA SIEMPRE. Doce filas por año: con dos años de
	// compras semanales son veinte, y en cinco años son sesenta. Y paginar una serie de
	// tiempo la ROMPE — el sentido de la serie esta en poder comparar enero contra enero, y
	// eso no sobrevive a que caigan en paginas distintas.
	//
	// El año conserva la lectura y ademas contesta mejor: "en 2025 llevaste 48 y en 2026 vas
	// por 31" es mas util que veinte filas mensuales que nadie recorre.
	//
	// CUANTOS MESES TUVIERON COMPRA es el campo que evita la trampa del año a medias: 2026
	// puede llevar 31 unidades en OCHO meses y 2025 llevar 48 en doce. Sin ese dato los dos
	// numeros se comparan como si midieran lo mismo, y no.
	public class AnioDeConsumo
	{
		public int Anio { get; private set; }
		public int Unidades { get; private set; }
		public decimal Gasto { get; private set; }

		/// <summary>En cuántas compras apareció durante el año.</summary>
		public int Compras { get; private set; }

		/// <summary>Cuántos meses distintos del año tuvieron alguna. Un año a medias tiene menos de 12.</summary>
		public int MesesConCompras { get; private set; }

		internal AnioDeConsumo(int anio)
		{
			if (anio < 1)
				throw new ArgumentOutOfRangeException(nameof(anio), anio, "No hay año cero.");

			Anio = anio;
		}

		/// <summary>Como aaaa, que es la forma en que se le dice a quien pregunta.</summary>
		public string AnioExpresado => $"{Anio:0000}";

		internal void Sumar(MesDeConsumo mes)
		{
			ArgumentNullException.ThrowIfNull(mes);

			Unidades = Unidades + mes.Unidades;
			Gasto = Gasto + mes.Gasto;
			Compras = Compras + mes.Compras;
			MesesConCompras = MesesConCompras + 1;
		}
	}
}
