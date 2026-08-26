using System;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// Lo que una evaluacion de la agenda le hizo al conjunto: cuantas operaciones se movieron, si
	/// alguna vencio y si alguna se paso de la gracia. Es la respuesta que el host actua; un
	/// Evaluate mudo esconderia el hecho por el que se llamo (E9, E30).
	/// </summary>
	internal sealed class EvaluationResult
	{
		private EvaluationResult(
			int transitionCount, bool anythingBecameDue, bool anythingBecameOverdue)
		{
			TransitionCount = transitionCount;
			AnythingBecameDue = anythingBecameDue;
			AnythingBecameOverdue = anythingBecameOverdue;
		}

		/// <summary>Cuantas operaciones cambiaron de estado en esta evaluacion.</summary>
		internal int TransitionCount { get; }

		/// <summary>Si alguna paso a estar vencida sin estarlo antes.</summary>
		internal bool AnythingBecameDue { get; }

		/// <summary>Si alguna paso a estar pasada de la gracia.</summary>
		internal bool AnythingBecameOverdue { get; }

		/// <summary>Lo que sumo una evaluacion. Un conteo negativo es un defecto de quien llama.</summary>
		internal static EvaluationResult Of(
			int transitionCount, bool anythingBecameDue, bool anythingBecameOverdue)
		{
			if (transitionCount < 0)
				throw new DomainException(
					$"Una evaluacion no puede haber movido {transitionCount} operaciones.");

			return new EvaluationResult(transitionCount, anythingBecameDue, anythingBecameOverdue);
		}
	}
}
