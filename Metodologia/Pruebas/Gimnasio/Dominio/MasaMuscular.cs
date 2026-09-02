using System;

namespace Gimnasio.Dominio
{
	// LA MASA MUSCULAR, en kilos.
	//
	// LA EXCEPCION QUE ROMPE EL JUICIO 1. El Preguntas.md dice que la direccion
	// deseable la da el objetivo, y despues encuentra que aca no: subir es bueno para
	// quien quiere bajar de peso y para quien quiere subir. No hay objetivo que la
	// cambie.
	//
	// Por eso NO consulta al objetivo. Y es la razon por la que DireccionDeseable es
	// virtual en la base en vez de un metodo del objetivo: si el objetivo mandara
	// siempre, esta verdad no tendria donde vivir mas que en un `if` por tipo.
	//
	// Su referencia no depende del perfil. Que sea la unica sin dependencias tambien
	// la distingue.
	internal sealed class MasaMuscular : Medida
	{
		internal MasaMuscular(decimal kilos) : base(kilos) { }

		internal override string Nombre { get { return "masa-muscular"; } }

		internal override string ClaveDeReferencia(Perfil perfil)
		{
			ArgumentNullException.ThrowIfNull(perfil);
			return "masa-muscular";
		}

		internal override Direccion DireccionDeseable(Objetivo objetivo)
		{
			return Direccion.Subir();
		}
	}
}
