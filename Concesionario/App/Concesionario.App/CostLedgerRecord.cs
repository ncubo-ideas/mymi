using System;

namespace Concesionario.App
{
	/// <summary>
	/// Las entradas del libro de costos de un periodo como un registro de columnas paralelas: una
	/// entrada por fila en cada columna, en orden del instante en que se incurrio. Un DTO con forma
	/// de wire hecho de arreglos primitivos, nunca un portador del dominio.
	/// <para>
	/// No lleva columna de identidad ni lugar de la entrada -- una entrada del libro no tiene clave
	/// -- ni columna con la parte que a este periodo le toca, que es media respuesta con una tabla
	/// encima.
	/// </para>
	/// </summary>
	public sealed class CostLedgerRecord
	{
		private CostLedgerRecord(
			string[] incurredAt, string[] subject, string[] money, string[] classification,
			string[] expectedLife, string[] says)
		{
			IncurredAt = incurredAt;
			Subject = subject;
			Money = money;
			Classification = classification;
			ExpectedLife = expectedLife;
			Says = says;
		}

		/// <summary>
		/// Cuando se incurrio cada entrada, como instante con su hora y nunca como un dia pelado.
		/// </summary>
		public string[] IncurredAt { get; }

		/// <summary>En que se fue cada una, con las palabras que llegaron.</summary>
		public string[] Subject { get; }

		/// <summary>
		/// Lo que costo cada una, dicho como su propia moneda lo dice: con su signo y sus decimales.
		/// Nunca el numero pelado, que sin su moneda no se puede leer.
		/// </summary>
		public string[] Money { get; }

		/// <summary>
		/// De que clase es cada una, por el NOMBRE del miembro. La puso el comando que la registro.
		/// </summary>
		public string[] Classification { get; }

		/// <summary>
		/// La vida util de cada una, y nada en la celda de la que no tiene ninguna. Una celda vacia
		/// dice que no tiene; un cero se leeria como una vida de cero meses.
		/// </summary>
		public string[] ExpectedLife { get; }

		/// <summary>Cada entrada en la frase que el dominio compuso para ella.</summary>
		public string[] Says { get; }

		/// <summary>
		/// Arma el registro y refusa columnas de largos distintos, asi que una fila es una fila por
		/// construccion: una columna corta por uno emparejaria en silencio cada fila posterior con
		/// otra entrada.
		/// </summary>
		internal static CostLedgerRecord Of(
			string[] incurredAt, string[] subject, string[] money, string[] classification,
			string[] expectedLife, string[] says)
		{
			string[][] columns = { incurredAt, subject, money, classification, expectedLife, says };

			foreach (string[] each in columns)
				if (each == null)
					throw new InvalidOperationException(
						"Una columna del registro del libro de costos llego ausente, y una columna "
						+ "ausente no es una columna vacia.");

			foreach (string[] each in columns)
				if (each.Length != incurredAt.Length)
					throw new InvalidOperationException(
						$"Las columnas del registro del libro de costos miden {incurredAt.Length} y "
						+ $"{each.Length}. Una fila es una fila: con largos distintos, cada fila "
						+ "despues de la primera diferencia quedaria emparejada con otra entrada.");

			return new CostLedgerRecord(
				incurredAt, subject, money, classification, expectedLife, says);
		}
	}
}
