using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// Cada cuanto publica el fabricante que se hace una operacion de mantenimiento: la clase de
	/// operacion y su intervalo publicado. Es un NUMERO PUBLICADO y no un procedimiento de
	/// decision: quien decide si algo vencio es la agenda del vehiculo, con sus propias bases.
	///
	/// El mismo numero del fabricante vive tambien en la copia por vehiculo que la agenda usa para
	/// medir. Esta es la verdad PUBLICADA, y lo es porque nombra la revision de la que salio; la
	/// copia de la agenda sigue siendo la base de medida.
	/// </summary>
	internal sealed class ServiceIntervalEntry
	{
		private readonly MaintenanceOperationKind _kind;
		private readonly PublishedInterval _interval;

		/// <summary>
		/// Se arma entera antes de entrar a ninguna revision: de que operacion habla y cada cuanto
		/// se hace. Sin cualquiera de las dos no dice nada que se pueda cumplir.
		/// </summary>
		internal ServiceIntervalEntry(MaintenanceOperationKind kind, PublishedInterval interval)
		{
			if (kind == null)
				throw new DomainException(
					"Se publico un intervalo de servicio sin decir de que operacion habla.");

			if (interval == null)
				throw new DomainException(
					$"Se publico el intervalo de {kind.Name} sin decir cada cuanto.");

			_kind = kind;
			_interval = interval;
		}

		/// <summary>
		/// La clausula que este intervalo aporta a la respuesta, como forma y valores: la operacion y
		/// cada cuanto se hace, con las mitades que se publicaron y sin inventar la que falta.
		/// </summary>
		internal AServiceIntervalClause TheClauseItComposes(string revisionCode)
		{
			return AServiceIntervalClause.Published(revisionCode, _kind, _interval);
		}

		/// <summary>
		/// La misma clausula CON la mitad recibida: donde esta parado este vehiculo contra el
		/// intervalo, con la distancia que le queda tal como llego. Nada se guarda: entra por
		/// parametro y sale con la respuesta. Si las unidades no coinciden, el intervalo lo dice y no
		/// convierte.
		/// </summary>
		internal AServiceIntervalClause TheClauseItComposesAgainst(
			string revisionCode, int distanceRemaining, DistanceUnit remainingUnit)
		{
			return TheClauseItComposes(revisionCode)
				.PairedWith(_interval, distanceRemaining, remainingUnit);
		}
	}
}
