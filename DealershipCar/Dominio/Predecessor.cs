using System;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// La ocurrencia terminal de la que salio esta, o la constancia de que esta es la primera. La
	/// cadena hacia atras ES el historial de la operacion.
	/// </summary>
	internal abstract class Predecessor
	{
		/// <summary>No hay anterior: esta ocurrencia es la genesis. Instancia unica, compartida.</summary>
		internal static readonly Predecessor None = new NoPredecessor();

		/// <summary>
		/// La ocurrencia anterior. Un <c>null</c> es un defecto de quien llama: la genesis se dice
		/// con <see cref="None"/>.
		/// </summary>
		internal static Predecessor Of(ScheduledOperation occurrence)
		{
			if (occurrence == null)
				throw new DomainException(
					"Un enlace hacia atras en null no es una genesis: la genesis se dice con "
					+ "Predecessor.None, que contesta que no hay anterior. Un hueco no contesta "
					+ "nada y corta la cadena sin decir que la corto.");

			return new APredecessor(occurrence);
		}

		/// <summary>Si hay ocurrencia anterior. Total: contesta siempre.</summary>
		internal abstract bool Exists();

		/// <summary>La ocurrencia anterior. Revienta si no hay.</summary>
		internal abstract ScheduledOperation Occurrence();
	}
}
