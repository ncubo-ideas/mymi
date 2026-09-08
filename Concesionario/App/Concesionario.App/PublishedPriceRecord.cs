using System;

namespace Concesionario.App
{
	/// <summary>
	/// The published service prices as a record of parallel columns: one entry per row in every
	/// column, newest revision first. A wire-shaped DTO of primitive arrays, never a domain carrier.
	/// </summary>
	public sealed class PublishedPriceRecord
	{
		private PublishedPriceRecord(
			string[] operation, string[] milestone, string[] says, string[] revision)
		{
			Operation = operation;
			Milestone = milestone;
			Says = says;
			Revision = revision;
		}

		/// <summary>
		/// The operation each price is for, by the member's own name, which is the addressable half: a
		/// caller can ask about that operation again with it.
		/// </summary>
		public string[] Operation { get; }

		/// <summary>
		/// The milestone each price was published at, said with its unit as a person reads it. It is
		/// the milestone SAID and not an addressable form.
		/// </summary>
		public string[] Milestone { get; }

		/// <summary>
		/// Each price in the sentence the domain composed for it: the amount, its currency, its
		/// validity, whether that validity is in force at the instant asked, and its revision.
		/// </summary>
		public string[] Says { get; }

		/// <summary>The revision each price came from, which is the one that governs its key.</summary>
		public string[] Revision { get; }

		/// <summary>
		/// Builds the record and refuses columns of different lengths, so a row is a row by
		/// construction: a column short by one would silently pair every later row with the wrong
		/// price.
		/// </summary>
		internal static PublishedPriceRecord Of(
			string[] operation, string[] milestone, string[] says, string[] revision)
		{
			string[][] columns = { operation, milestone, says, revision };

			foreach (string[] each in columns)
				if (each == null)
					throw new InvalidOperationException(
						"Una columna del registro de precios publicados llego ausente, y una columna "
						+ "ausente no es una columna vacia.");

			foreach (string[] each in columns)
				if (each.Length != operation.Length)
					throw new InvalidOperationException(
						$"Las columnas del registro de precios publicados miden {operation.Length} y "
						+ $"{each.Length}. Una fila es una fila: con largos distintos, cada fila despues "
						+ "de la primera diferencia quedaria emparejada con otro precio.");

			return new PublishedPriceRecord(operation, milestone, says, revision);
		}
	}
}
