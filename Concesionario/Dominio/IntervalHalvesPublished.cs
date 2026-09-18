using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// QUE MITADES PUBLICO el fabricante de un intervalo, por su codigo. La que no se publico no se
	/// nombra: un cero ahi se leeria como un plazo cumplido desde siempre.
	/// </summary>
	internal static class IntervalHalvesPublished
	{
		/// <summary>Solo la distancia.</summary>
		internal const string ByDistance = "ByDistance";

		/// <summary>Solo el plazo.</summary>
		internal const string ByTime = "ByTime";

		/// <summary>Las dos, y la primera que llegue manda.</summary>
		internal const string Both = "Both";

		/// <summary>No hay intervalo publicado, asi que no hay mitades que nombrar.</summary>
		internal const string None = "";
	}
}
