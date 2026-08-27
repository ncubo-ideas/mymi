using System;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// El codigo con el que un <see cref="DeferralReason"/> cruza el wire hacia adentro.
	/// Cada miembro nombra al homonimo del conjunto cerrado; se resuelve con
	/// <see cref="DeferralReason.OfCode"/>.
	/// </summary>
	internal enum DeferralReasonCode
	{
		/// <summary>Vease <see cref="DeferralReason.Cost"/>.</summary>
		Cost,

		/// <summary>Vease <see cref="DeferralReason.Timing"/>.</summary>
		Timing,

		/// <summary>Vease <see cref="DeferralReason.SecondOpinion"/>.</summary>
		SecondOpinion,

		/// <summary>Vease <see cref="DeferralReason.NotConvinced"/>.</summary>
		NotConvinced,

		/// <summary>Vease <see cref="DeferralReason.WillDoElsewhere"/>.</summary>
		WillDoElsewhere,
	}

	/// <summary>
	/// Por que la persona decidio dejar una operacion para despues. Conjunto cerrado de cinco
	/// miembros. Ninguno cambia el tope ni la ventana de aplazamiento.
	/// </summary>
	internal sealed class DeferralReason
	{
		/// <summary>No ahora por lo que cuesta.</summary>
		internal static readonly DeferralReason Cost = new DeferralReason("Cost");

		/// <summary>No ahora por cuando: no le calza dejar el vehiculo en esta fecha.</summary>
		internal static readonly DeferralReason Timing = new DeferralReason("Timing");

		/// <summary>Quiere que otro lo vea antes de autorizarlo.</summary>
		internal static readonly DeferralReason SecondOpinion =
			new DeferralReason("SecondOpinion");

		/// <summary>No esta convencida de que haga falta.</summary>
		internal static readonly DeferralReason NotConvinced = new DeferralReason("NotConvinced");

		/// <summary>Lo va a hacer, pero en otro lado.</summary>
		internal static readonly DeferralReason WillDoElsewhere =
			new DeferralReason("WillDoElsewhere");

		/// <summary>El nombre del motivo, para proyectarlo como escalar y para diagnosticar.</summary>
		internal string Name { get; }

		private DeferralReason(string name)
		{
			Name = name;
		}

		/// <summary>
		/// Resuelve el codigo del wire al miembro. Total con excepcion: un codigo desconocido es un
		/// defecto de quien llama y revienta.
		/// </summary>
		internal static DeferralReason OfCode(DeferralReasonCode code)
		{
			switch (code)
			{
				case DeferralReasonCode.Cost: return Cost;
				case DeferralReasonCode.Timing: return Timing;
				case DeferralReasonCode.SecondOpinion: return SecondOpinion;
				case DeferralReasonCode.NotConvinced: return NotConvinced;
				case DeferralReasonCode.WillDoElsewhere: return WillDoElsewhere;
			}

			throw new DomainException(
				$"'{code}' no nombra ningun motivo de aplazamiento. Hay exactamente cinco; uno "
				+ "desconocido no es un motivo nuevo, es un defecto de quien llama.");
		}
	}
}
