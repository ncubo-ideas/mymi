using System;
using System.Collections.Generic;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// La respuesta del historial de UNA clase de operacion: las completaciones alcanzables en su
	/// cadena, de la mas reciente a la mas vieja, o la constancia con nombre de por que no hay
	/// ninguna. Es un carrier interno -- se camina en cada lectura y no cruza hacia ninguna
	/// caratula.
	/// <para>
	/// La ausencia tiene DOS razones y cada una es su propio tipo: la clase esta agendada y nunca se
	/// completo, que una visita al taller cura, o la clase no esta agendada, que ninguna visita cura
	/// hasta que alguien siembre la operacion.
	/// </para>
	/// </summary>
	internal abstract class CompletionsOfAnOperation
	{
		/// <summary>
		/// Arma la respuesta de una clase que SI esta agendada, con las filas que su cadena dejo. Sin
		/// ninguna, la respuesta es <see cref="NoCompletionOnRecord"/> y no un juego vacio.
		/// </summary>
		internal static CompletionsOfAnOperation Of(
			MaintenanceOperationKind kind, List<ACompletionRow> rows)
		{
			if (kind == null)
				throw new DomainException(
					"Un historial sin clase de operacion no dice de que esta hablando.");

			if (rows == null)
				throw new DomainException(
					"Un historial se arma con las filas que la cadena dejo, y aca no llego ninguna "
					+ "lista: ninguna completacion y una lista ausente no son lo mismo.");

			if (rows.Count == 0) return new NoCompletionOnRecord(kind);

			return new AnOperationWithCompletionsOnRecord(kind, rows);
		}

		/// <summary>
		/// La respuesta de una clase que esta agenda no tiene agendada: no hay cadena que caminar, y
		/// eso no es lo mismo que una cadena sin completaciones.
		/// </summary>
		internal static CompletionsOfAnOperation OfAKindThatIsNotOnTheSchedule(
			MaintenanceOperationKind kind)
		{
			if (kind == null)
				throw new DomainException(
					"Una ausencia de operacion agendada sin clase no dice de que clase se pregunto.");

			return new AKindThatIsNotOnTheSchedule(kind);
		}

		/// <summary>Si hay alguna completacion alcanzable. TOTAL: contesta siempre.</summary>
		internal abstract bool HasAnyCompletion();

		/// <summary>
		/// Las completaciones, de la mas reciente a la mas vieja. TOTAL: vacia cuando no hay ninguna,
		/// y es la respuesta primaria -- de ella sale el conteo y del conteo no salen ellas.
		/// </summary>
		internal abstract List<ACompletionRow> TheCompletions();

		/// <summary>
		/// Cuantas completaciones hay. Se cuenta sobre las filas en cada lectura y nunca se guarda:
		/// un contador aparte arrastraria un historial que la cadena ya no dice.
		/// </summary>
		internal int HowManyCompletions()
		{
			return TheCompletions().Count;
		}

		/// <summary>
		/// El historial en una frase. TOTAL: las dos ausencias tambien se dicen con palabras, y cada
		/// una nombra su propia razon.
		/// </summary>
		internal abstract string Describe();
	}
}
