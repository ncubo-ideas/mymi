using System;
using System.Globalization;

namespace Concesionario.Dominio
{
	/// <summary>
	/// El precio que el fabricante publico para una operacion en un hito: el dinero con que se
	/// publico y la vigencia entre la que rige. La clave de un precio son tres cosas juntas -- la
	/// operacion, la distancia del hito y su unidad -- asi que los precios se guardan en una lista y
	/// se buscan por una operacion con nombre, no por una clave compuesta.
	/// </summary>
	internal sealed class ServicePriceEntry
	{
		private readonly MaintenanceOperationKind _kind;
		private readonly PublishedMilestone _milestone;
		private readonly Money _price;
		private readonly DateTime _validFrom;
		private readonly DateTime _validUntil;

		/// <summary>
		/// Se arma entero y se valida antes de entrar a ninguna revision: de que operacion es, para
		/// que hito, con cuanto dinero y entre que fechas rige. El dinero llega ya armado, asi que un
		/// monto sin su moneda no llega hasta aca, y el precio se guarda tal como se publico.
		/// </summary>
		internal ServicePriceEntry(
			MaintenanceOperationKind kind, PublishedMilestone milestone, Money price,
			DateTime validFrom, DateTime validUntil)
		{
			if (kind == null)
				throw new DomainException(
					"Se publico un precio sin decir de que operacion es.");

			if (milestone == null)
				throw new DomainException(
					$"Se publico un precio de {kind.Name} sin decir para que hito. Un precio sin hito "
					+ "no se puede pedir: no se sabe que servicio cubre.");

			if (price == null)
				throw new DomainException(
					$"Se publico un precio de {kind.Name} sin decir cuanto. Un precio sin dinero no "
					+ "es un precio: no hay nada que cotizar.");

			if (validUntil < validFrom)
				throw new DomainException(
					$"Se publico un precio de {kind.Name} cuya vigencia termina antes de empezar. No "
					+ "hay ningun instante en que ese precio sea el de la sucursal, asi que "
					+ "publicarlo dejaria un precio que nunca rige y siempre se puede citar.");

			_kind = kind;
			_milestone = milestone;
			_price = price;
			_validFrom = validFrom;
			_validUntil = validUntil;
		}

		/// <summary>
		/// La clave bajo la que se publico, armada por quien la tiene. Es lo que deja enumerar los
		/// precios de una revision sin entregar ni las entradas ni sus dos mitades por separado.
		/// </summary>
		internal APublishedPriceKey TheKeyItWasPublishedUnder()
		{
			return APublishedPriceKey.Of(_kind, _milestone);
		}

		/// <summary>
		/// Si este es el precio que se pidio: la misma operacion Y el mismo hito. Es la busqueda con
		/// nombre que recorre la lista, y se niega a mirar dos hitos en unidades distintas como si
		/// fueran uno. La comparacion misma vive en la clave, en un solo sitio.
		/// </summary>
		internal bool IsFor(MaintenanceOperationKind kind, PublishedMilestone milestone)
		{
			if (kind == null || milestone == null)
				throw new DomainException(
					"Se busco un precio sin decir de que operacion o para que hito. Sin las dos "
					+ "mitades no hay precio que buscar.");

			return TheKeyItWasPublishedUnder().IsTheSameKeyAs(
				APublishedPriceKey.Of(kind, milestone));
		}

		/// <summary>
		/// El precio CON su vigencia, como forma y valores: la operacion, el hito, el dinero con su
		/// moneda y entre que dias rige. La vigencia no es un adorno de la respuesta -- es lo que
		/// impide leer un precio viejo como el de hoy.
		/// </summary>
		internal APriceClause APriceCarriesItsValidity(string revisionCode)
		{
			return APriceClause.Published(
				revisionCode, _kind, _milestone, _price, Fecha(_validFrom), Fecha(_validUntil));
		}

		/// <summary>
		/// Lo mismo, y ademas donde cae EL INSTANTE RECIBIDO dentro de esa vigencia. El dominio no
		/// lee ningun reloj, asi que el instante entra por parametro; sin el, "vencido" no se puede
		/// decir respecto a nada. Se compara al segundo entero, que es la resolucion que el journal
		/// conserva.
		/// </summary>
		internal APriceClause APriceCarriesItsValidity(string revisionCode, DateTime asOf)
		{
			return APriceCarriesItsValidity(revisionCode)
				.AskedAt(WhereItFalls(asOf), Fecha(asOf));
		}

		private string WhereItFalls(DateTime asOf)
		{
			if (asOf < _validFrom) return PriceValidity.NotYetStarted;
			if (asOf > _validUntil) return PriceValidity.AlreadyPassed;

			return PriceValidity.InForce;
		}

		private static string Fecha(DateTime instant)
		{
			return instant.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
		}
	}
}
