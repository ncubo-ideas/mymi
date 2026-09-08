using System;
using System.Collections.Generic;

namespace Concesionario.Dominio
{
	/// <summary>
	/// La respuesta de una clase que esta agendada y cuya cadena no tiene ninguna completacion
	/// alcanzable. Es una ausencia CON TIPO y nombra la clase por la que se pregunto: sin el nombre,
	/// todas las clases sin completar dirian la misma frase.
	/// <para>
	/// Es la ausencia que una visita al taller cura, que es lo que la separa de
	/// <see cref="AKindThatIsNotOnTheSchedule"/>.
	/// </para>
	/// </summary>
	internal sealed class NoCompletionOnRecord : CompletionsOfAnOperation
	{
		private readonly MaintenanceOperationKind _kind;

		/// <summary>Se arma con la clase, que es lo que la frase nombra.</summary>
		internal NoCompletionOnRecord(MaintenanceOperationKind kind)
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

		/// <summary>La ausencia en palabras, con la clase adentro.</summary>
		internal override string Describe()
		{
			return _kind.HowItIsSaid + ": no hay ninguna completación registrada para este vehículo.";
		}
	}
}
