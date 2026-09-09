using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// Lo que una evaluacion le hizo a una operacion: si cambio de estado, si con eso vencio y si
	/// ademas se paso de la gracia. Es lo que el agregado suma para su
	/// <c>EvaluationResult</c>; un Evaluate sin respuesta esconderia el hecho que el host maneja.
	/// </summary>
	internal sealed class DueStateTransition
	{
		/// <summary>Nada se movio. Instancia unica, compartida.</summary>
		internal static readonly DueStateTransition None =
			new DueStateTransition(false, false, false);

		private DueStateTransition(bool happened, bool becameDue, bool becameOverdue)
		{
			Happened = happened;
			BecameDue = becameDue;
			BecameOverdue = becameOverdue;
		}

		/// <summary>Si la operacion cambio de estado.</summary>
		internal bool Happened { get; }

		/// <summary>
		/// Si con este cambio la operacion paso a estar vencida sin estarlo antes. Overdue cuenta:
		/// es vencida y ademas pasada.
		/// </summary>
		internal bool BecameDue { get; }

		/// <summary>Si con este cambio la operacion paso a estar pasada de la gracia.</summary>
		internal bool BecameOverdue { get; }

		/// <summary>
		/// El cambio entre dos estados. Una sola transicion aunque el camino del diagrama tenga
		/// escalas: el aviso de cortesia no es una parada obligatoria.
		/// </summary>
		internal static DueStateTransition Between(DueState previous, DueState current)
		{
			if (previous == current) return None;

			return new DueStateTransition(
				true,
				IsDueOrPast(current) && !IsDueOrPast(previous),
				current == DueState.Overdue && previous != DueState.Overdue);
		}

		private static bool IsDueOrPast(DueState state)
		{
			return state == DueState.Due || state == DueState.Overdue;
		}
	}
}
