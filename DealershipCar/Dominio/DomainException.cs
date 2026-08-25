using System;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// La violacion de un contrato del dominio: algo que el negocio dice que nunca puede pasar, y
	/// que si paso es un defecto de quien llama. Un resultado esperado no viaja por aca: es un
	/// estado.
	/// </summary>
	internal sealed class DomainException : Exception
	{
		/// <summary>El mensaje dice que regla se rompio y con que valor.</summary>
		internal DomainException(string message) : base(message)
		{
		}
	}
}
