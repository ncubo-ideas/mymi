using System;

namespace Concesionario.Dominio
{
	/// <summary>La ausencia de ocurrencia anterior: esta es la primera de su cadena.</summary>
	internal sealed class NoPredecessor : Predecessor
	{
		/// <summary>Se llega por <see cref="Predecessor.None"/>.</summary>
		internal NoPredecessor()
		{
		}

		/// <summary>No.</summary>
		internal override bool Exists()
		{
			return false;
		}

		/// <summary>Revienta: no hay ocurrencia anterior que devolver.</summary>
		internal override ScheduledOperation Occurrence()
		{
			throw new DomainException(
				"Esta ocurrencia no viene de otra: pregunte HasAPredecessor antes. La cadena "
				+ "termina en un hecho -- la genesis -- y no en un hueco.");
		}
	}
}
