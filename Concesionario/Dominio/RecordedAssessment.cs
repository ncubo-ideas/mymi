using System;

namespace Concesionario.Dominio
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

		/// <summary>El motivo. Total: la ausencia de evaluacion tambien tiene el suyo.</summary>
		internal abstract ADueReason TheReason();

		/// <summary>
		/// Si la distancia quedo suspendida por unidades distintas. Total, y derivada de
		/// esta respuesta y no de un latch: por eso se cura sola.
		/// </summary>
		internal abstract bool SuspendsDistanceByAUnitMismatch();

		/// <summary>
		/// Si la ley dice que la operacion se paso de la gracia. Total. Es lo que deja contar como
		/// no hecha a una operacion aplazada.
		/// </summary>
		internal abstract bool SaysTheOperationIsOverdue();
	}
}
