using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// SI HAY LIMITES QUE NOMBRAR en una respuesta de cobertura, por su codigo. No es lo mismo que
	/// unos limites en cero: un cero se leeria como un limite cumplido desde siempre, y esta ausencia
	/// dice que no habia contra que medir.
	/// </summary>
	internal static class LimitsInForce
	{
		/// <summary>Rigen unos limites y la respuesta los nombra.</summary>
		internal const string Named = "Named";

		/// <summary>No se llego a medir contra ningunos, asi que no hay cuales nombrar.</summary>
		internal const string None = "None";
	}
}
