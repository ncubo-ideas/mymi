using System;
using System.Collections.Generic;

namespace Concesionario.Dominio
{
	/// <summary>
	/// La respuesta de una clase que esta agenda no tiene agendada: no hay cadena que caminar, asi
	/// que no hay ninguna completacion que alcanzar. Es una ausencia CON TIPO y nombra la clase.
	/// <para>
	/// Es la ausencia que ninguna visita al taller cura hasta que alguien siembre la operacion, que
	/// es lo que la separa de <see cref="NoCompletionOnRecord"/>. Decir la de alla por la de aca
	/// afirmaria que una operacion que no esta en el plan del vehiculo nunca se hizo.
	/// </para>
	/// </summary>
	internal sealed class AKindThatIsNotOnTheSchedule : CompletionsOfAnOperation
	{
		private readonly MaintenanceOperationKind _kind;

		/// <summary>Se arma con la clase por la que se pregunto, que es lo que la frase nombra.</summary>
		internal AKindThatIsNotOnTheSchedule(MaintenanceOperationKind kind)
		{
			_kind = kind;
		}

		/// <summary>No.</summary>
		internal override bool HasAnyCompletion()
		{
			return false;
		}

		/// <summary>Ninguna.</summary>
		internal override List<ACompletionRow> TheCompletions()
		{
			return new List<ACompletionRow>();
		}

		/// <summary>
		/// La ausencia con su propio codigo, que es lo que deja decir que lo que falta es la
		/// operacion.
		/// </summary>
		internal override ACompletionsClause TheClause()
		{
			return ACompletionsClause.OfAKindThatIsNotOnTheSchedule(_kind.Name);
		}
	}
}
