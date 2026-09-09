using System;

namespace Municipalidad.App
{
	/// <summary>
	/// La ley que hace de una fila una fila, en un solo lugar: ninguna columna de un registro llega
	/// ausente, y todas miden lo mismo.
	/// </summary>
	internal static class ColumnasParalelas
	{
		/// <summary>
		/// Refusa un registro cuyas columnas no sean paralelas. Una columna ausente no es una columna
		/// vacia, y una columna corta por uno emparejaria en silencio cada fila posterior con otra.
		/// </summary>
		internal static void Comprobar(string registro, Array[] columnas)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(registro);
			ArgumentNullException.ThrowIfNull(columnas);
			if (columnas.Length == 0)
				throw new InvalidOperationException(
					"Un registro de " + registro + " sin ninguna columna no se puede comprobar.");

			foreach (Array cada in columnas)
				if (cada == null)
					throw new InvalidOperationException(
						"Una columna del registro de " + registro + " llego ausente, y una columna "
						+ "ausente no es una columna vacia.");

			int filas = columnas[0].Length;
			foreach (Array cada in columnas)
				if (cada.Length != filas)
					throw new InvalidOperationException(
						"Las columnas del registro de " + registro + " miden " + filas + " y "
						+ cada.Length + ". Una fila es una fila: con largos distintos, cada fila "
						+ "despues de la primera diferencia queda emparejada con otra.");
		}
	}
}
