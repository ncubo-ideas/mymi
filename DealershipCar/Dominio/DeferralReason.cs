using System;

namespace DealershipCar.Dominio
{
	// POR QUE LA PERSONA APLAZO. Conjunto cerrado del canon (§3).
	//
	// Los cinco motivos son de la PERSONA, no del taller, y estan puestos para que la respuesta
	// "no ahora" no tenga que ser anonima. Son un hecho recibido (E44): el dominio no adivina
	// el motivo, llega como parametro con la decision.
	//
	// LO QUE EL MOTIVO NO HACE: no cambia la regla. El tope de aplazamientos y el largo de la
	// ventana son los mismos para los cinco (ley L6, SchedulePolicy §6) -- ningun motivo compra
	// un aplazamiento extra, y ninguno se lo quita. Y con una operacion clasificada de
	// seguridad, ninguno de los cinco sirve: esa no se aplaza nunca, ni la primera vez
	// (OINV-27, OINV-52).
	//
	// Por eso este tipo no tiene comportamiento, y esa falta es correcta: si manana un motivo
	// tuviera que decidir distinto que otro -- WillDoElsewhere cerrando la operacion en vez de
	// aplazarla -- eso seria comportamiento y ahi si se justificarian subtipos (E39). Hoy son
	// cinco valores que el registro conserva.
	//
	// Y CONSERVARLOS ES EL PUNTO. "Cost" repetido tres veces en un anio es una conversacion
	// sobre precio; "Timing" tres veces es una sobre horarios de taller. La app puede decir
	// cual de las dos, y solo porque el motivo se guardo en vez de resumirse en un contador.

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
	/// miembros comparados por identidad. Ninguno cambia el tope ni la ventana de aplazamiento
	/// (ley L6): el motivo se conserva para poder leerlo, no para negociar la regla.
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
		/// Resuelve el codigo del wire al miembro. FUNCION TOTAL CON EXCEPCION: un codigo
		/// desconocido es un defecto de quien llama y revienta (E32). Aceptarlo callado
		/// guardaria un aplazamiento sin motivo legible, que es justamente lo que el conjunto
		/// esta puesto para evitar.
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
