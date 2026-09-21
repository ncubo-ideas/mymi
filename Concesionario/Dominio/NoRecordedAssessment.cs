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
		internal override ADueReason TheReason()
		{
			return ADueReason.NeverEvaluated();
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
