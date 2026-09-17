using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// QUE PUDO DECIRSE del costo de la distancia de un periodo, por su codigo. Las tres
	/// situaciones sin cifra son respuestas con nombre y no huecos: un cero por unidad en
	/// cualquiera de ellas se leeria como un vehiculo que anduvo gratis.
	/// </summary>
	internal static class CostPerDistanceShape
	{
		/// <summary>Hubo tramo y hubo gasto despues del ancla, asi que hay una cifra por
		/// moneda.</summary>
		internal const string Measured = "Measured";

		/// <summary>Despues de la primera lectura del periodo no se gasto nada.</summary>
		internal const string NothingSpentAfterTheAnchor = "NothingSpentAfterTheAnchor";

		/// <summary>Se gasto, y el odometro no se movio: no hay distancia por la que
		/// dividir.</summary>
		internal const string NoDistanceTravelled = "NoDistanceTravelled";

		/// <summary>El periodo no tiene tramo de uso que medir, y el motivo lo dice la
		/// serie.</summary>
		internal const string NoSpanOfUse = "NoSpanOfUse";
	}
}
