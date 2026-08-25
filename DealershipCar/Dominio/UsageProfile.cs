using System;

namespace DealershipCar.Dominio
{
	// COMO SE USA EL VEHICULO. Conjunto cerrado del canon (§3).
	//
	// AJUSTA LARGO, NUNCA BASE (OINV-22, ley L8). Un perfil de uso escala el LARGO de los
	// intervalos por un factor declarado -- y nada mas. No cambia la base de una operacion (una
	// TimeOrDistance no se vuelve TimeOnly porque el vehiculo trabaje duro) y no quita
	// operaciones del plan.
	//
	// Ese limite es la razon por la que el perfil es un conjunto cerrado y no una configuracion
	// libre: OHS-07 lo decidio para una tension real -- "una sucursal que acorta intervalos para
	// vender servicios destruye la confianza; una que no puede adaptarse a las condiciones
	// locales es inutil". La respuesta fue: el plan del fabricante es la linea base y no se
	// puede acortar; los factores del perfil son el UNICO ajuste permitido, y la app siempre
	// muestra cual aplico. Con cuatro miembros y factores nombrados en SchedulePolicy, "cual
	// aplico" es una pregunta contestable.
	//
	// El factor de cada perfil NO vive aca sino en SchedulePolicy, con los otros umbrales de
	// politica (§6). Aca esta el conjunto; alla el numero; el tipo que los junta es
	// IntervalLengthFactor, y llega en T3.
	//
	// HACIA ADELANTE, NO HACIA ATRAS (OINV-15): declarar un perfil trae su instante de vigencia
	// y no re-fecha lo que ya se completo. Eso es del agregado (T5), no de este tipo.

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
	/// una operacion ni la quita del plan (OINV-22, ley L8).
	/// </summary>
	internal sealed class UsageProfile
	{
		/// <summary>Uso normal: el plan del fabricante tal como viene, sin escalar.</summary>
		internal static readonly UsageProfile Standard = new UsageProfile("Standard");

		/// <summary>
		/// Uso severo -- polvo, remolque, ciudad pesada, temperaturas extremas. Acorta el largo
		/// de los intervalos por SevereDutyIntervalFactor.
		/// </summary>
		internal static readonly UsageProfile SevereDuty = new UsageProfile("SevereDuty");

		/// <summary>
		/// Poca distancia al año. Los limites de tiempo quedan intactos y los de distancia
		/// pierden relevancia: un vehiculo que casi no se mueve igual necesita sus operaciones
		/// por tiempo cumplido.
		/// </summary>
		internal static readonly UsageProfile LowDistance = new UsageProfile("LowDistance");

		/// <summary>Uso comercial: el vehiculo es herramienta de trabajo, no transporte personal.</summary>
		internal static readonly UsageProfile Commercial = new UsageProfile("Commercial");

		/// <summary>
		/// El nombre del miembro. Sirve para proyectar CUAL perfil aplico, que es lo que OHS-07
		/// exige que la app pueda mostrar siempre.
		/// </summary>
		internal string Name { get; }

		private UsageProfile(string name)
		{
			Name = name;
		}

		/// <summary>
		/// Resuelve el codigo del wire al miembro. FUNCION TOTAL CON EXCEPCION: un codigo
		/// desconocido es un defecto de quien llama y revienta (E32). Un perfil que nadie
		/// declaro no es un ajuste nuevo -- y aceptarlo callado seria acortar intervalos por un
		/// camino que OHS-07 cerro.
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
				+ "ajuste permitido sobre el plan del fabricante (OHS-07).");
		}
	}
}
