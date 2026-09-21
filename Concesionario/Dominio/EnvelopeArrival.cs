using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// CUAL DE LAS DOS MITADES DEL SOBRE llego primero, por su codigo -- incluidas las dos
	/// respuestas que no nombran ninguna, que son respuestas y no ausencias: una porque las dos
	/// mitades siguen abiertas, y la otra porque una de las dos no se pudo medir.
	/// </summary>
	internal static class EnvelopeArrival
	{
		/// <summary>No se puede decir: una de las dos mitades no se midio.</summary>
		internal const string NeitherHalfCanBeSaid = "NeitherHalfCanBeSaid";

		/// <summary>Ninguna llego, y cual llegara depende de cuanto se maneje.</summary>
		internal const string NeitherHalfIsReached = "NeitherHalfIsReached";

		/// <summary>Las dos llegaron juntas.</summary>
		internal const string BothHalvesTogether = "BothHalvesTogether";

		/// <summary>Llego primero la de distancia.</summary>
		internal const string TheDistanceHalf = "TheDistanceHalf";

		/// <summary>Llego primero la de tiempo.</summary>
		internal const string TheTimeHalf = "TheTimeHalf";
	}
}
