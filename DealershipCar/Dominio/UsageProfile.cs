using System;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// El codigo con el que un <see cref="UsageProfile"/> cruza el wire hacia adentro.
	/// Cada miembro nombra al homonimo del conjunto cerrado; se resuelve con
	/// <see cref="UsageProfile.OfCode"/>.
	/// </summary>
	internal enum UsageProfileCode
	{
		/// <summary>Vease <see cref="UsageProfile.Standard"/>.</summary>
		Standard,

		/// <summary>Vease <see cref="UsageProfile.SevereDuty"/>.</summary>
		SevereDuty,

		/// <summary>Vease <see cref="UsageProfile.LowDistance"/>.</summary>
		LowDistance,

		/// <summary>Vease <see cref="UsageProfile.Commercial"/>.</summary>
		Commercial,
	}

	/// <summary>
	/// Como se usa el vehiculo. Conjunto cerrado de cuatro miembros comparados por identidad.
	/// Solo escala el LARGO de los intervalos por un factor declarado: nunca cambia la base de
	/// una operacion ni la quita del plan.
	/// </summary>
	internal sealed class UsageProfile
	{
		/// <summary>Uso normal: el plan del fabricante tal como viene, sin escalar.</summary>
		internal static readonly UsageProfile Standard = new UsageProfile("Standard", "normal");

		/// <summary>
		/// Uso severo -- polvo, remolque, ciudad pesada, temperaturas extremas. Acorta el largo
		/// de los intervalos por SevereDutyIntervalFactor.
		/// </summary>
		internal static readonly UsageProfile SevereDuty = new UsageProfile("SevereDuty", "de uso severo");

		/// <summary>
		/// Poca distancia al año: los limites de tiempo quedan intactos y los de distancia se
		/// ignoran.
		/// </summary>
		internal static readonly UsageProfile LowDistance = new UsageProfile("LowDistance", "de poca distancia");

		/// <summary>Uso comercial: el vehiculo es herramienta de trabajo, no transporte personal.</summary>
		internal static readonly UsageProfile Commercial = new UsageProfile("Commercial", "comercial");

		/// <summary>El nombre, para proyectar cual perfil aplico.</summary>
		internal string Name { get; }

		/// <summary>
		/// Como se dice este perfil en la frase que alguien lee. NO es lo que cruza la puerta ni
		/// lo que viaja en el wire: eso sigue siendo <see cref="Name"/>.
		/// </summary>
		internal string HowItIsSaid { get; }

		private UsageProfile(string name, string howItIsSaid)
		{
			Name = name;
			HowItIsSaid = howItIsSaid;
		}

		/// <summary>
		/// Resuelve el codigo del wire al miembro. Total con excepcion: un codigo desconocido es un
		/// defecto de quien llama y revienta.
		/// </summary>
		internal static UsageProfile OfCode(UsageProfileCode code)
		{
			switch (code)
			{
				case UsageProfileCode.Standard: return Standard;
				case UsageProfileCode.SevereDuty: return SevereDuty;
				case UsageProfileCode.LowDistance: return LowDistance;
				case UsageProfileCode.Commercial: return Commercial;
			}

			throw new DomainException(
				$"'{code}' no nombra ningun perfil de uso. Hay exactamente cuatro, y son el unico "
				+ "ajuste permitido sobre el plan del fabricante.");
		}
	}
}
