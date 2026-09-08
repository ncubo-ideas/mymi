using System;

namespace Concesionario.Dominio
{
	/// <summary>La ausencia de evaluacion: la operacion existe y todavia nadie la evaluo.</summary>
	internal sealed class NoRecordedAssessment : RecordedAssessment
	{
		/// <summary>Se llega por <see cref="RecordedAssessment.None"/>.</summary>
		internal NoRecordedAssessment()
		{
		}

		/// <summary>Lo dice en vez de inventar una respuesta que nadie calculo.</summary>
		internal override string Describe()
		{
			return "Sin vencer, pero esta operación todavía no se ha evaluado, así que esto no es "
				+ "evidencia de que la operación no esté vencida.";
		}

		/// <summary>No: no hay evaluacion de donde derivarlo.</summary>
		internal override bool SuspendsDistanceByAUnitMismatch()
		{
			return false;
		}

		/// <summary>No: ninguna ley dijo todavia nada sobre esta operacion.</summary>
		internal override bool SaysTheOperationIsOverdue()
		{
			return false;
		}
	}
}
