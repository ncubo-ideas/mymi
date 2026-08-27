using System;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// La respuesta que la ley dio sobre una operacion en su ultima evaluacion, o la constancia de
	/// que todavia no se evaluo ninguna vez. Nunca se evaluo es un hecho, no un NotDue.
	/// </summary>
	internal abstract class RecordedAssessment
	{
		/// <summary>Todavia no se evaluo. Instancia unica, compartida.</summary>
		internal static readonly RecordedAssessment None = new NoRecordedAssessment();

		/// <summary>
		/// La respuesta que quedo registrada. Un <c>null</c> es un defecto de quien llama: la falta
		/// de evaluacion se dice con <see cref="None"/>.
		/// </summary>
		internal static RecordedAssessment Of(DueAssessment assessment)
		{
			if (assessment == null)
				throw new DomainException(
					"Una evaluacion en null no es la ausencia de evaluacion: la ausencia se dice "
					+ "con RecordedAssessment.None, que contesta que no la hubo.");

			return new ARecordedAssessment(assessment);
		}

		/// <summary>El motivo compuesto. Total: la ausencia de evaluacion tambien se dice.</summary>
		internal abstract string Describe();

		/// <summary>
		/// Si la distancia quedo suspendida por unidades distintas (ley L7). Total, y derivada de
		/// esta respuesta y no de un latch: por eso se cura sola (GAP G4).
		/// </summary>
		internal abstract bool SuspendsDistanceByAUnitMismatch();

		/// <summary>
		/// Si la ley dice que la operacion se paso de la gracia. Total. Es lo que deja contar como
		/// no hecha a una operacion aplazada (OINV-28).
		/// </summary>
		internal abstract bool SaysTheOperationIsOverdue();
	}
}
