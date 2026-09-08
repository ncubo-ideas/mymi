using System;

namespace Examenes.Dominio
{
	// Donde cae un valor medido respecto del rango de referencia de su analito, para
	// la edad del paciente.
	//
	// Conjunto cerrado, comparado por referencia. Es deliberadamente lo que un rango
	// puede decir por si solo mirando UN valor: normal, alto o bajo. La lectura que
	// necesita la serie completa - si viene subiendo, si el salto es inverosimil - no
	// vive aca sino en Tendencia, porque no se puede contestar con un solo punto.
	internal sealed class ClasificacionDeValor
	{
		internal static readonly ClasificacionDeValor Normal = new ClasificacionDeValor("normal");
		internal static readonly ClasificacionDeValor Alto = new ClasificacionDeValor("alto");
		internal static readonly ClasificacionDeValor Bajo = new ClasificacionDeValor("bajo");

		internal string Nombre { get; private set; }

		private ClasificacionDeValor(string nombre)
		{
			Nombre = nombre;
		}

		internal bool EsNormal => ReferenceEquals(this, Normal);
	}
}
