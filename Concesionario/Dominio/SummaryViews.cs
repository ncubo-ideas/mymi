using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// CUALES DE LAS DOS VISTAS de una moneda tienen cifra, por su codigo: la caja y lo consumido,
	/// solo la caja, o solo lo consumido. Las dos se dicen SIEMPRE -- la que no tiene cifra se dice
	/// con palabras --, y este codigo es cual de las dos redacciones le toca a cada una.
	/// <para>
	/// Viaja como UN codigo y no como dos banderas: dos banderas pueden decir que no hay ninguna
	/// vista, y un resumen se arma con entradas que tocan el periodo, asi que siempre hay al menos
	/// una.
	/// </para>
	/// </summary>
	internal static class SummaryViews
	{
		/// <summary>Salio dinero del banco adentro Y el periodo consumio algo.</summary>
		internal const string BothViews = "BothViews";

		/// <summary>Salio dinero adentro y el periodo no consumio nada de eso todavia.</summary>
		internal const string TheCashOutAlone = "TheCashOutAlone";

		/// <summary>El periodo consumio algo y no salio nada del banco adentro.</summary>
		internal const string TheConsumedAlone = "TheConsumedAlone";
	}
}
