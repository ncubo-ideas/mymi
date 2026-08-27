namespace DealershipCar.App
{
	// LO QUE UNA EVALUACION DEJO, contado hacia afuera. Es el EvaluationResult del dominio pasado a
	// forma de wire, mas UN hecho que el dominio no puede saber: si la evaluacion llego a correr.
	//
	// Esa mitad de mas es la cadencia de C6. Evaluate entra por PerformCheckThenCommand guardado
	// sobre el punto de control monotono de la agenda, asi que re-evaluar un instante ya evaluado
	// se rechaza bajo el read lock y NO deja entrada en el journal. Sin este campo, la caratula no
	// tendria como distinguir "no se movio nada" de "ni siquiera se evaluo", y son cosas distintas:
	// la primera es una respuesta, la segunda es que la pregunta ya estaba contestada.
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

		// Si la guarda de cadencia dejo pasar la evaluacion.
		public bool WasEvaluated { get; }

		// El motivo del rechazo, tal cual lo devolvio el Check, o vacio si no hubo rechazo.
		//
		// Viene envuelto en JSON -- {"EWI":[{"Error":"..."}]} -- y se deja crudo a proposito: quien
		// quiera MOSTRARLO tiene que desenvolverlo, y desenvolver es de la vista (medido, SPIKES-OS2
		// S3).
		public string Rejection { get; }

		// Cuantas operaciones cambiaron de estado. Cero si la evaluacion se rechazo.
		public int TransitionCount { get; }

		// Si alguna paso a estar vencida sin estarlo antes.
		public bool AnythingBecameDue { get; }

		// Si alguna paso a estar pasada de la gracia.
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
