using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// QUE FORMA TIENE LA MITAD DE DISTANCIA de lo que le queda a un sobre, por su codigo: medida,
	/// o cual de las dos razones impidio medirla. El codigo es identidad y cruza intacto; como se
	/// dice cada una es de quien la muestra.
	/// </summary>
	internal static class EnvelopeDistanceHalf
	{
		/// <summary>Se midio y trae su numero.</summary>
		internal const string Measured = "Measured";

		/// <summary>No hay ninguna lectura usable del odometro de donde medirla.</summary>
		internal const string AbsentReading = "AbsentReading";

		/// <summary>El limite publicado y la otra distancia estan en unidades distintas.</summary>
		internal const string UnitMismatch = "UnitMismatch";
	}
}
