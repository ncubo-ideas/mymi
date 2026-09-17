using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// QUE TIENE QUE DECIR UNA ESTADIA, por su codigo: sigue adentro, ya la entregaron, o ninguna
	/// cubre el instante preguntado.
	/// <para>
	/// Las dos primeras son el estado de una estadia y la tercera es la ausencia de cualquiera. Una
	/// fila de un periodo solo puede estar en las dos primeras: cero filas es cero filas y no una
	/// tercera situacion.
	/// </para>
	/// </summary>
	internal static class StayShape
	{
		/// <summary>Entro y todavia no lo han entregado, asi que no tiene dias ni salida.</summary>
		internal const string StillOpen = "StillOpen";

		/// <summary>Ya lo entregaron, asi que tiene sus dos extremos y los dias que cubrio.</summary>
		internal const string Collected = "Collected";

		/// <summary>Ninguna estadia del registro cubre el instante por el que se pregunto.</summary>
		internal const string NoStayCoversTheInstant = "NoStayCoversTheInstant";
	}
}
