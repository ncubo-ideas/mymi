using System;

namespace Municipalidad.Dominio
{
	/// <summary>
	/// La negativa del dominio: el registro de la cuenta no permite lo que se pidio. Toda guarda
	/// que niegue por el estado de la cuenta lanza esta, y ninguna otra la lanza por otra razon.
	/// </summary>
	internal sealed class DomainException : Exception
	{
		/// <summary>El mensaje dice la regla en palabras y que predicado preguntar antes.</summary>
		internal DomainException(string message) : base(message)
		{
		}
	}
}
