using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// LAS DOS DIMENSIONES con que se mide un intervalo, por su codigo. El codigo es identidad y
	/// cruza intacto; como se dice cada una es de quien la muestra.
	/// </summary>
	internal static class IntervalDimension
	{
		/// <summary>La mitad que se cuenta en dias desde el ancla.</summary>
		internal const string Time = "Time";

		/// <summary>La mitad que se cuenta en la unidad del limite desde el ancla.</summary>
		internal const string Distance = "Distance";
	}
}
