using System;

namespace DealershipCar.Dominio
{
	/// <summary>La respuesta que la ley dio en la ultima evaluacion de la operacion.</summary>
	internal sealed class ARecordedAssessment : RecordedAssessment
	{
		private readonly DueAssessment _assessment;

		/// <summary>Se llega por <see cref="RecordedAssessment.Of"/>, que rechaza el null.</summary>
		internal ARecordedAssessment(DueAssessment assessment)
		{
			_assessment = assessment;
		}

		/// <summary>El motivo que compuso la ley.</summary>
		internal override string Describe()
		{
			return _assessment.Describe();
		}

		/// <summary>Lo que dijo la ley de esta evaluacion, no lo que dijo alguna vez.</summary>
		internal override bool SuspendsDistanceByAUnitMismatch()
		{
			return _assessment.SuspendedByUnitMismatch;
		}

		/// <summary>Si el estado que la ley decidio es Overdue.</summary>
		internal override bool SaysTheOperationIsOverdue()
		{
			return _assessment.State == DueState.Overdue;
		}
	}
}
