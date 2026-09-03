using System;

namespace DealershipCar.App
{
	/// <summary>
	/// The completions of one operation kind as a record of parallel columns: one entry per row in
	/// every column, newest first. A wire-shaped DTO of primitive arrays, never a domain carrier.
	/// </summary>
	public sealed class CompletionRecord
	{
		private CompletionRecord(string[] completedAt, string[] reading, string[] says)
		{
			CompletedAt = completedAt;
			Reading = reading;
			Says = says;
		}

		/// <summary>When each completion was done, as an instant with its time, never a bare day.</summary>
		public string[] CompletedAt { get; }

		/// <summary>
		/// The odometer each completion was done at, with its unit, or the words that say there was
		/// none. Never a zero and never an empty cell.
		/// </summary>
		public string[] Reading { get; }

		/// <summary>Each completion in the sentence the domain composed for it, which names its kind.</summary>
		public string[] Says { get; }

		/// <summary>
		/// Builds the record and refuses columns of different lengths, so a row is a row by
		/// construction: a column short by one would silently pair every later row with the wrong
		/// completion.
		/// <para>
		/// It carries no kind column: the caller supplied the kind, so a column repeating it would be
		/// the one column of the record the domain does not answer.
		/// </para>
		/// </summary>
		internal static CompletionRecord Of(string[] completedAt, string[] reading, string[] says)
		{
			string[][] columns = { completedAt, reading, says };

			foreach (string[] each in columns)
				if (each == null)
					throw new InvalidOperationException(
						"Una columna del registro de completaciones llego ausente, y una columna ausente "
						+ "no es una columna vacia.");

			foreach (string[] each in columns)
				if (each.Length != completedAt.Length)
					throw new InvalidOperationException(
						$"Las columnas del registro de completaciones miden {completedAt.Length} y "
						+ $"{each.Length}. Una fila es una fila: con largos distintos, cada fila despues "
						+ "de la primera diferencia quedaria emparejada con otra completacion.");

			return new CompletionRecord(completedAt, reading, says);
		}
	}
}
