namespace Concesionario.App
{
	public sealed class EvaluationOutcome
	{
		private EvaluationOutcome(
			bool wasEvaluated, string rejection,
			int transitionCount, bool anythingBecameDue, bool anythingBecameOverdue)
		{
			WasEvaluated = wasEvaluated;
			Rejection = rejection;
			TransitionCount = transitionCount;
			AnythingBecameDue = anythingBecameDue;
			AnythingBecameOverdue = anythingBecameOverdue;
		}

		/// <summary>Whether the cadence guard let the evaluation through.</summary>
		public bool WasEvaluated { get; }

		/// <summary>The Check's rejection exactly as it returned it, or empty when there was none.</summary>
		public string Rejection { get; }

		/// <summary>How many operations changed state. Zero when the evaluation was rejected.</summary>
		public int TransitionCount { get; }

		/// <summary>Whether any operation became due without having been so before.</summary>
		public bool AnythingBecameDue { get; }

		/// <summary>Whether any operation went past its grace period.</summary>
		public bool AnythingBecameOverdue { get; }

		internal static EvaluationOutcome Of(
			int transitionCount, bool anythingBecameDue, bool anythingBecameOverdue)
		{
			return new EvaluationOutcome(
				true, "", transitionCount, anythingBecameDue, anythingBecameOverdue);
		}

		internal static EvaluationOutcome Rejected(string rejection)
		{
			return new EvaluationOutcome(false, rejection, 0, false, false);
		}
	}
}
