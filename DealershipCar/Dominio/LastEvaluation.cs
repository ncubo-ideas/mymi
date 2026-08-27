using System;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// El instante en que la agenda se evaluo por ultima vez, o la constancia de que todavia no se
	/// evaluo ninguna vez. Es el punto de control monotono sobre el que C6 guarda la cadencia de
	/// <see cref="MaintenanceSchedule.Evaluate"/>.
	/// </summary>
	internal abstract class LastEvaluation
	{
		/// <summary>Todavia no se evaluo. Instancia unica, compartida.</summary>
		internal static readonly LastEvaluation None = new NoLastEvaluation();

		/// <summary>La evaluacion que si ocurrio, en el instante recibido.</summary>
		internal static LastEvaluation At(DateTime instant)
		{
			return new ALastEvaluation(instant);
		}

		/// <summary>Si la agenda se evaluo alguna vez. Total: contesta siempre.</summary>
		internal abstract bool Exists();

		/// <summary>Cuando. Revienta si nunca se evaluo (E41): ausente no es una fecha minima.</summary>
		internal abstract DateTime Instant();

		/// <summary>
		/// Si lo ya evaluado es POSTERIOR a ese instante, o sea si mirarlo seria volver atras.
		/// Total, y es la pregunta que hacen tanto Evaluate como DeclareUsageProfile.
		/// </summary>
		internal abstract bool IsAfter(DateTime instant);
	}
}
