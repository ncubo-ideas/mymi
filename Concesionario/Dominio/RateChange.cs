using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// QUE PUDO DECIRSE DEL CAMBIO entre dos ritmos, por su codigo. Las cuatro situaciones sin
	/// porcentaje son respuestas con nombre y no huecos: un cero por ciento se leeria como que el
	/// ritmo no se movio, que es justamente una de las otras cuatro.
	/// <para>
	/// Viaja como UN codigo y no como un numero con signo: el signo dice a que lado, y no dice ni
	/// que los dos ritmos sean iguales ni que el otro periodo estuviera quieto.
	/// </para>
	/// </summary>
	internal static class RateChange
	{
		/// <summary>Se pregunto por un solo periodo, asi que no hay contra que comparar.</summary>
		internal const string NotAsked = "NotAsked";

		/// <summary>Alguno de los dos periodos no tiene ritmo que decir.</summary>
		internal const string CannotBeSaid = "CannotBeSaid";

		/// <summary>Los dos ritmos son el mismo.</summary>
		internal const string TheSameRate = "TheSameRate";

		/// <summary>El otro periodo esta en cero, y un porcentaje contra cero no existe.</summary>
		internal const string NotAPercentage = "NotAPercentage";

		/// <summary>El primer periodo se maneja mas que el segundo.</summary>
		internal const string Higher = "Higher";

		/// <summary>El primer periodo se maneja menos que el segundo.</summary>
		internal const string Lower = "Lower";
	}
}
