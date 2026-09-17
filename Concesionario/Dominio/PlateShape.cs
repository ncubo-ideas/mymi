using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// QUE TIENE QUE DECIR LA PLACA, por su codigo: hay una publicada al instante preguntado, o no hay
	/// ninguna.
	/// <para>
	/// La ausencia tiene una sola clase, igual que del lado del dominio: o hay placa a ese instante o
	/// no la hay, y no hay una segunda razon estructuralmente distinta que nombrar.
	/// </para>
	/// </summary>
	internal static class PlateShape
	{
		/// <summary>Hay una placa publicada, con su texto y desde cuando rige.</summary>
		internal const string Assigned = "Assigned";

		/// <summary>No hay ninguna, y la frase nombra a quien publica una placa.</summary>
		internal const string NoneOnRecord = "NoneOnRecord";
	}
}
