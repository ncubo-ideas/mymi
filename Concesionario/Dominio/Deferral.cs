using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// Una decision de la persona de dejar una operacion para despues: por que, cuando la tomo y
	/// hasta cuando la sostiene. La ventana se mide desde la decision recibida, no desde el
	/// vencimiento.
	/// </summary>
	internal sealed class Deferral
	{
		/// <summary>El motivo se journaliza con la decision: sin el no hay aplazamiento.</summary>
		internal Deferral(DeferralReason reason, DateTime decidedAt)
		{
			if (reason == null)
				throw new DomainException(
					"Un aplazamiento sin motivo no se puede registrar: se journaliza CON "
					+ "su DeferralReason, porque lo que la persona decidio es la mitad del hecho.");

			Reason = reason;
			DecidedAt = decidedAt;
			WindowEndsAt = decidedAt.AddDays(SchedulePolicy.DeferralWindowDays);
		}

		/// <summary>Por que la persona lo dejo para despues.</summary>
		internal DeferralReason Reason { get; }

		/// <summary>Cuando lo decidio.</summary>
		internal DateTime DecidedAt { get; }

		/// <summary>Hasta cuando lo sostiene, cumplida la ventana de la politica.</summary>
		internal DateTime WindowEndsAt { get; }

		/// <summary>
		/// Si la ventana ya se cumplio en ese instante. Inclusiva: el dia que la consume entera es
		/// el que la termina.
		/// </summary>
		internal bool WindowHasElapsedBy(DateTime asOf)
		{
			return asOf >= WindowEndsAt;
		}
	}
}
