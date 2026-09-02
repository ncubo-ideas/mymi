using System;
using System.Globalization;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// El precio que el fabricante publico para una operacion en un hito: el monto con su moneda y
	/// la vigencia entre la que rige. La clave de un precio son tres cosas juntas -- la operacion,
	/// la distancia del hito y su unidad -- asi que los precios se guardan en una lista y se buscan
	/// por una operacion con nombre, no por una clave compuesta.
	/// </summary>
	internal sealed class ServicePriceEntry
	{
		private readonly MaintenanceOperationKind _kind;
		private readonly PublishedMilestone _milestone;
		private readonly decimal _amount;
		private readonly string _currency;
		private readonly DateTime _validFrom;
		private readonly DateTime _validUntil;

		/// <summary>
		/// Se arma entero y se valida antes de entrar a ninguna revision. El monto se guarda tal
		/// como se publico y la moneda es texto publicado: ninguno de los dos se convierte ni se
		/// normaliza. Un monto en cero se acepta -- un servicio incluido es un precio publicado --
		/// pero uno negativo no es un precio.
		/// </summary>
		internal ServicePriceEntry(
			MaintenanceOperationKind kind, PublishedMilestone milestone, decimal amount,
			string currency, DateTime validFrom, DateTime validUntil)
		{
			if (kind == null)
				throw new DomainException(
					"Se publico un precio sin decir de que operacion es.");

			if (milestone == null)
				throw new DomainException(
					$"Se publico un precio de {kind.Name} sin decir para que hito. Un precio sin hito "
					+ "no se puede pedir: no se sabe que servicio cubre.");

			if (amount < 0)
				throw new DomainException(
					$"Se publico un precio de {kind.Name} de {amount}. Un precio negativo no es un "
					+ "precio, es la sucursal pagandole a la persona por traer el carro.");

			if (string.IsNullOrWhiteSpace(currency))
				throw new DomainException(
					$"Se publico un precio de {kind.Name} sin decir en que moneda. Un monto sin "
					+ "moneda no se puede leer, y aca las monedas no se convierten entre si.");

			if (validUntil < validFrom)
				throw new DomainException(
					$"Se publico un precio de {kind.Name} cuya vigencia termina antes de empezar. No "
					+ "hay ningun instante en que ese precio sea el de la sucursal, asi que "
					+ "publicarlo dejaria un precio que nunca rige y siempre se puede citar.");

			_kind = kind;
			_milestone = milestone;
			_amount = amount;
			_currency = currency;
			_validFrom = validFrom;
			_validUntil = validUntil;
		}

		/// <summary>
		/// Si este es el precio que se pidio: la misma operacion Y el mismo hito. Es la busqueda con
		/// nombre que recorre la lista, y se niega a mirar dos hitos en unidades distintas como si
		/// fueran uno.
		/// </summary>
		internal bool IsFor(MaintenanceOperationKind kind, PublishedMilestone milestone)
		{
			if (kind == null || milestone == null)
				throw new DomainException(
					"Se busco un precio sin decir de que operacion o para que hito. Sin las dos "
					+ "mitades no hay precio que buscar.");

			return _kind == kind && _milestone.IsTheSameMilestoneAs(milestone);
		}

		/// <summary>
		/// El precio dicho CON su vigencia: la operacion, el hito, el monto con su moneda y entre
		/// que fechas rige. La vigencia no es un adorno de la frase -- es lo que impide leer un
		/// precio viejo como el de hoy.
		/// </summary>
		internal string APriceCarriesItsValidity()
		{
			return _kind.Name + " at " + _milestone.Describe()
				+ ": " + _amount.ToString(CultureInfo.InvariantCulture) + " " + _currency
				+ ", valid from " + Fecha(_validFrom) + " to " + Fecha(_validUntil) + ".";
		}

		/// <summary>
		/// Lo mismo, y ademas si esa vigencia rige EN EL INSTANTE RECIBIDO. El dominio no lee ningun
		/// reloj, asi que el instante entra por parametro; sin el, "vencido" no se puede decir
		/// respecto a nada. Se compara al segundo entero, que es la resolucion que el journal
		/// conserva.
		/// </summary>
		internal string APriceCarriesItsValidity(DateTime asOf)
		{
			if (asOf < _validFrom)
				return APriceCarriesItsValidity()
					+ " As of " + Fecha(asOf)
					+ " that validity has not started, so this price is not current.";

			if (asOf > _validUntil)
				return APriceCarriesItsValidity()
					+ " As of " + Fecha(asOf)
					+ " that validity has passed, so this price is not current.";

			return APriceCarriesItsValidity()
				+ " As of " + Fecha(asOf) + " that validity is in force.";
		}

		private static string Fecha(DateTime instant)
		{
			return instant.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
		}
	}
}
