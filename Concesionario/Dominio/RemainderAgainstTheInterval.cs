using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// DONDE QUEDO la distancia que le queda a un vehiculo frente al intervalo publicado, por su
	/// codigo. Las dos suspensiones son respuestas y no huecos: aqui las unidades nunca se
	/// convierten, asi que decir por que no se pudo comparar es la respuesta entera.
	/// </summary>
	internal static class RemainderAgainstTheInterval
	{
		/// <summary>Las dos distancias son comparables y la respuesta dice la que queda.</summary>
		internal const string Measured = "Measured";

		/// <summary>El intervalo se publico como plazo y sin distancia: no hay contra que pararla.</summary>
		internal const string NoDistanceHalfPublished = "NoDistanceHalfPublished";

		/// <summary>La distancia publicada y la recibida llegaron en unidades distintas.</summary>
		internal const string UnitMismatch = "UnitMismatch";

		/// <summary>Nadie pregunto por ningun vehiculo, asi que no hay remanente que situar.</summary>
		internal const string None = "";
	}
}
