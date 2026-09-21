using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// QUE MITADES CONTESTA una respuesta de la hoja, por su codigo: la publicada sola, o la publicada
	/// emparejada con la posicion de este vehiculo frente a ella. Lo que separa las dos mitades cuando
	/// van juntas es puntuacion, y la puntuacion es del idioma, asi que aqui viaja el codigo y no la
	/// juntura.
	/// </summary>
	internal static class HalvesAnswered
	{
		/// <summary>Lo que el fabricante publico, y nada de ningun vehiculo.</summary>
		internal const string ThePublishedHalfAlone = "ThePublishedHalfAlone";

		/// <summary>Lo publicado y donde queda este vehiculo frente a ello, que es el producto entero.</summary>
		internal const string GeneralAndPersonalTogether = "GeneralAndPersonalTogether";
	}
}
