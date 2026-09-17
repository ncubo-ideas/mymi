using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// POR QUE UNA RESPUESTA DE COBERTURA quedo donde quedo, por su codigo. Es el paso de la ley que
	/// decidio, y el orden de esos pasos es la ley misma: el catalogo decide antes que el alcance, el
	/// alcance antes que los limites, y los limites antes que las exclusiones.
	/// <para>
	/// No lleva un miembro para "nadie publico nada": eso lo dice
	/// <see cref="SpecificationPublication"/>, que la clausula ya carga, y decirlo dos veces dejaria
	/// dos sitios donde pueden dejar de coincidir.
	/// </para>
	/// </summary>
	internal static class CoverageReason
	{
		/// <summary>El alcance publicado no nombra ese sistema en ninguna parte.</summary>
		internal const string NotInTheVocabulary = "NotInTheVocabulary";

		/// <summary>El alcance lo nombra solo entre sus exclusiones, nunca entre los de adentro.</summary>
		internal const string OnlyAmongTheExclusions = "OnlyAmongTheExclusions";

		/// <summary>Se midio contra los limites que rigen y el vehiculo esta dentro.</summary>
		internal const string InsideTheLimits = "InsideTheLimits";

		/// <summary>Se midio contra los limites que rigen y el vehiculo esta fuera.</summary>
		internal const string OutsideTheLimits = "OutsideTheLimits";

		/// <summary>El limite que rige y el odometro llegaron en unidades distintas.</summary>
		internal const string UnitMismatch = "UnitMismatch";

		/// <summary>No hubo ningun paso que nombrar, porque no habia contra que medir.</summary>
		internal const string None = "";
	}
}
