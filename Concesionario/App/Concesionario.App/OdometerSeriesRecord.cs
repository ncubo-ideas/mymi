using System;

namespace Concesionario.App
{
	/// <summary>
	/// Las lecturas del odometro de un periodo como un registro de columnas paralelas: una entrada
	/// por fila en cada columna, en orden de observacion. Un DTO con forma de wire hecho de arreglos
	/// primitivos, nunca un portador del dominio.
	/// </summary>
	public sealed class OdometerSeriesRecord
	{
		private OdometerSeriesRecord(
			string[] observedAt, int[] value, string[] unit, string[] source, string[] says)
		{
			ObservedAt = observedAt;
			Value = value;
			Unit = unit;
			Source = source;
			Says = says;
		}

		/// <summary>
		/// Cuando se observo cada lectura, como instante con su hora y nunca como un dia pelado.
		/// </summary>
		public string[] ObservedAt { get; }

		/// <summary>
		/// Que valor leyo cada una, en la unidad que dice su propia fila. Sale como el numero que es:
		/// no es la mitad de nada, porque el ritmo ya viene compuesto y llega por su propia lectura.
		/// </summary>
		public int[] Value { get; }

		/// <summary>La unidad de cada lectura, por su nombre de mundo real.</summary>
		public string[] Unit { get; }

		/// <summary>
		/// De que fuente vino cada lectura, por el NOMBRE del miembro. Nada se pesa ni se descarta
		/// por ella.
		/// </summary>
		public string[] Source { get; }

		/// <summary>Cada lectura en la frase que el dominio compuso para ella.</summary>
		public string[] Says { get; }

		/// <summary>
		/// Arma el registro y refusa columnas de largos distintos, asi que una fila es una fila por
		/// construccion: una columna corta por uno emparejaria en silencio cada fila posterior con
		/// otra lectura.
		/// </summary>
		internal static OdometerSeriesRecord Of(
			string[] observedAt, int[] value, string[] unit, string[] source, string[] says)
		{
			string[][] text = { observedAt, unit, source, says };

			if (value == null)
				throw new InvalidOperationException(
					"La columna de valores del registro de la serie llego ausente, y una columna "
					+ "ausente no es una columna vacia.");

			foreach (string[] each in text)
				if (each == null)
					throw new InvalidOperationException(
						"Una columna del registro de la serie llego ausente, y una columna ausente no "
						+ "es una columna vacia.");

			if (value.Length != observedAt.Length)
				throw new InvalidOperationException(
					$"Las columnas del registro de la serie miden {observedAt.Length} y "
					+ $"{value.Length}. Una fila es una fila: con largos distintos, cada fila despues "
					+ "de la primera diferencia quedaria emparejada con otra lectura.");

			foreach (string[] each in text)
				if (each.Length != observedAt.Length)
					throw new InvalidOperationException(
						$"Las columnas del registro de la serie miden {observedAt.Length} y "
						+ $"{each.Length}. Una fila es una fila: con largos distintos, cada fila "
						+ "despues de la primera diferencia quedaria emparejada con otra lectura.");

			return new OdometerSeriesRecord(observedAt, value, unit, source, says);
		}
	}
}
