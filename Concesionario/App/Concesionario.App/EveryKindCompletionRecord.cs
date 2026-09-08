using System;

namespace Concesionario.App
{
	/// <summary>
	/// The completions of every operation kind as a record of parallel columns: one entry per row in
	/// every column, newest first across the kinds. A wire-shaped DTO of primitive arrays, never a
	/// domain carrier.
	/// </summary>
	public sealed class EveryKindCompletionRecord
	{
		private EveryKindCompletionRecord(
			string[] kind, string[] completedAt, string[] reading, string[] says)
		{
			Kind = kind;
			CompletedAt = completedAt;
			Reading = reading;
			Says = says;
		}

		/// <summary>
		/// The kind each completion was of, by the member's own name, which is the addressable half: a
		/// caller can ask about that kind again with it.
		/// </summary>
		public string[] Kind { get; }

		/// <summary>When each completion was done, as an instant with its time, never a bare day.</summary>
		public string[] CompletedAt { get; }

		/// <summary>
		/// The odometer each completion was done at, with its unit, or the words that say there was
		/// none. Never a zero and never an empty cell.
		/// </summary>
		public string[] Reading { get; }

		/// <summary>Each completion in the sentence the domain composed for it.</summary>
		public string[] Says { get; }

		/// <summary>
		/// Builds the record and refuses columns of different lengths, so a row is a row by
		/// construction: a column short by one would silently pair every later row with the wrong
		/// completion.
		/// <para>
		/// This one carries the kind because here it changes from row to row and no other column says
		/// it in a form a caller can pass back.
		/// </para>
		/// </summary>
		internal static EveryKindCompletionRecord Of(
			string[] kind, string[] completedAt, string[] reading, string[] says)
		{
			string[][] columns = { kind, completedAt, reading, says };

			foreach (string[] each in columns)
				if (each == null)
					throw new InvalidOperationException(
						"Una columna del registro de completaciones de todas las clases llego ausente, y "
						+ "una columna ausente no es una columna vacia.");

			foreach (string[] each in columns)
				if (each.Length != kind.Length)
					throw new InvalidOperationException(
						$"Las columnas del registro de completaciones de todas las clases miden "
						+ $"{kind.Length} y {each.Length}. Una fila es una fila: con largos distintos, "
						+ "cada fila despues de la primera diferencia quedaria emparejada con otra "
						+ "completacion.");

			return new EveryKindCompletionRecord(kind, completedAt, reading, says);
		}
	}
}
