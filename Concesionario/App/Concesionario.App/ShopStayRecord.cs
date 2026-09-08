using System;

namespace Concesionario.App
{
	/// <summary>
	/// The stays of a period as a record of parallel columns: one entry per row in every column, in
	/// entry order. A wire-shaped DTO of primitive arrays, never a domain carrier.
	/// </summary>
	public sealed class ShopStayRecord
	{
		private ShopStayRecord(
			string[] branch, string[] subject, string[] enteredAt, string[] coveredDays,
			string[] says)
		{
			Branch = branch;
			Subject = subject;
			EnteredAt = enteredAt;
			CoveredDays = coveredDays;
			Says = says;
		}

		/// <summary>The branch each stay was spent in, in the words the branch sent.</summary>
		public string[] Branch { get; }

		/// <summary>Why each stay began, in the words the branch sent.</summary>
		public string[] Subject { get; }

		/// <summary>When each stay began, as an instant with its time, never as a bare day.</summary>
		public string[] EnteredAt { get; }

		/// <summary>
		/// The calendar days each stay covered, space separated, empty for a stay still open.
		/// </summary>
		public string[] CoveredDays { get; }

		/// <summary>Each stay in the sentence the domain composed for it.</summary>
		public string[] Says { get; }

		/// <summary>
		/// Builds the record and refuses columns of different lengths, so a row is a row by
		/// construction: a column short by one would silently pair every later row with the wrong
		/// stay.
		/// </summary>
		internal static ShopStayRecord Of(
			string[] branch, string[] subject, string[] enteredAt, string[] coveredDays,
			string[] says)
		{
			string[][] columns = { branch, subject, enteredAt, coveredDays, says };

			foreach (string[] each in columns)
				if (each == null)
					throw new InvalidOperationException(
						"Una columna del registro de estadias llego ausente, y una columna ausente no "
						+ "es una columna vacia.");

			foreach (string[] each in columns)
				if (each.Length != branch.Length)
					throw new InvalidOperationException(
						$"Las columnas del registro de estadias miden {branch.Length} y {each.Length}. "
						+ "Una fila es una fila: con largos distintos, cada fila despues de la primera "
						+ "diferencia quedaria emparejada con otra estadia.");

			return new ShopStayRecord(branch, subject, enteredAt, coveredDays, says);
		}
	}
}
