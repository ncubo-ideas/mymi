using System;
using System.Collections.Generic;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// El historial de una clase que si tiene completaciones: las filas que su cadena dejo, de la
	/// mas reciente a la mas vieja, en el orden en que el camino las encontro.
	/// </summary>
	internal sealed class AnOperationWithCompletionsOnRecord : CompletionsOfAnOperation
	{
		private readonly MaintenanceOperationKind _kind;
		private readonly List<ACompletionRow> _rows;

		/// <summary>
		/// Se arma con la clase y las filas de su cadena. Al menos una: sin ninguna la respuesta es
		/// la ausencia con nombre, y quien la elige es <see cref="CompletionsOfAnOperation.Of"/>.
		/// </summary>
		internal AnOperationWithCompletionsOnRecord(
			MaintenanceOperationKind kind, List<ACompletionRow> rows)
		{
			if (rows == null || rows.Count == 0)
				throw new DomainException(
					"Un historial con completaciones tiene al menos una, y con este no llego ninguna. "
					+ "Que no haya ninguna es NoCompletionOnRecord, que lo dice con palabras.");

			_kind = kind;
			_rows = new List<ACompletionRow>(rows);
		}

		/// <summary>Si.</summary>
		internal override bool HasAnyCompletion()
		{
			return true;
		}

		/// <summary>Las completaciones de la cadena, de la mas reciente a la mas vieja.</summary>
		internal override List<ACompletionRow> TheCompletions()
		{
			return new List<ACompletionRow>(_rows);
		}

		/// <summary>
		/// El historial en palabras: la clase, cuantas completaciones hay y como se hizo la mas
		/// reciente. Las filas dicen el resto de si mismas.
		/// </summary>
		internal override string Describe()
		{
			return _kind.HowItIsSaid + ": " + HowManyCompletions()
				+ (HowManyCompletions() == 1
					? " completación registrada"
					: " completaciones registradas")
				+ ". La más reciente " + _rows[0].HowItWasDone() + ".";
		}
	}
}
