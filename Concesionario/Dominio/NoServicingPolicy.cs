using System;

namespace Concesionario.Dominio
{
	/// <summary>La revision que no publico ninguna regla de servicio en otro taller.</summary>
	internal sealed class NoServicingPolicy : ServicingPolicy
	{
		/// <summary>Se llega por <see cref="ServicingPolicy.None"/>.</summary>
		internal NoServicingPolicy()
		{
		}

		/// <summary>No.</summary>
		internal override bool Exists()
		{
			return false;
		}

		/// <summary>
		/// Revienta: no hay regla que decir. Una clausula sin texto se leeria como que el fabricante
		/// no pone ninguna condicion, que es la lectura opuesta a la verdadera.
		/// </summary>
		internal override AServicingClause TheClauseItComposes(string revisionCode)
		{
			throw new DomainException(
				"Esta revision no publico ninguna regla de servicio en otro taller. Hay que "
				+ "preguntar primero si la publico.");
		}

		/// <summary>
		/// Revienta: sin regla publicada no hay contra que parar los conteos del vehiculo, y llevar
		/// solo los conteos seria dar numeros sin la regla que los hace significar algo.
		/// </summary>
		internal override AServicingClause TheClauseItComposesAgainst(
			string revisionCode, int externalServicesWithEvidence,
			int externalServicesWithoutEvidence, int missedMaintenanceCount)
		{
			throw new DomainException(
				"Se pidio la posicion de un vehiculo frente a una regla de servicio en otro taller "
				+ "que esta revision no publico. Hay que preguntar primero si la publico.");
		}
	}
}
