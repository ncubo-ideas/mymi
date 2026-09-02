using System;

namespace Gimnasio.Dominio
{
	// LA CIRCUNFERENCIA DE CINTURA, en centimetros.
	//
	// Lo propio, y es lo que la separa de todas las demas: es la unica que DICE ALGO DE
	// SALUD SIN COMPARARSE CONSIGO MISMA. Las otras hablan de progreso -donde estabas,
	// donde estas-; esta tiene un umbral de riesgo propio, y estar del lado malo de ese
	// umbral es una noticia aunque nunca te hayan medido antes.
	//
	// Su referencia depende del genero y de nada mas.
	internal sealed class Cintura : Medida
	{
		internal Cintura(decimal centimetros) : base(centimetros) { }

		internal override string Nombre { get { return "cintura"; } }

		internal override string ClaveDeReferencia(Perfil perfil)
		{
			ArgumentNullException.ThrowIfNull(perfil);
			return $"cintura:{perfil.Genero}";
		}
	}
}
