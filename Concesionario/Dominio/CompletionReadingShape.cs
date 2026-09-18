using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// SI UNA COMPLETACION TIENE LECTURA DEL ODOMETRO, por su codigo. La ausencia es una SITUACION con
	/// nombre y no una celda vacia ni un cero: las dos se leerian como que hubo medicion.
	/// </summary>
	internal static class CompletionReadingShape
	{
		/// <summary>El ancla de la que sale la fila tiene lectura, con su valor y su unidad.</summary>
		internal const string WithAReading = "WithAReading";

		/// <summary>El ancla no tiene lectura, porque la ley de la clase no mide distancia.</summary>
		internal const string WithNoReading = "WithNoReading";
	}
}
