using System;
using System.Globalization;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// Un monto con su moneda, INSEPARABLES: un monto suelto no se puede leer, y ningun miembro de
	/// aqui entrega el monto solo.
	/// <para>
	/// Dos montos en monedas distintas no se suman. El dinero no se convierte, se registra; agrupar
	/// por moneda es de quien consulta.
	/// </para>
	/// </summary>
	internal sealed class Money
	{
		private readonly decimal _amount;
		private readonly Currency _currency;

		/// <summary>
		/// Se arma entero: sin moneda no hay dinero. Un monto negativo no es dinero; cero si lo es,
		/// porque un servicio incluido tambien es un monto.
		/// </summary>
		internal Money(decimal amount, Currency currency)
		{
			if (currency == null)
				throw new DomainException(
					"Llego un monto de " + AsSaid(amount) + " sin decir en que moneda. Un monto sin "
					+ "moneda no se puede leer, y aca las monedas no se convierten entre si.");

			if (amount < 0)
				throw new DomainException(
					"Llego un monto de " + AsSaid(amount) + " en " + currency.Code + ". Un monto "
					+ "negativo no es dinero: es la sucursal pagandole a la persona.");

			_amount = amount;
			_currency = currency;
		}

		/// <summary>
		/// Si esta en la misma moneda que ese otro. TOTAL, y es la pregunta que hay que hacer antes de
		/// sumar. Una moneda que este modulo no nombra contesta que no de todas.
		/// </summary>
		internal bool IsInTheSameCurrencyAs(Money other)
		{
			if (other == null)
				throw new DomainException(
					"Se pregunto si dos montos estan en la misma moneda sin decir el segundo.");

			return _currency.IsTheSameCurrencyAs(other._currency);
		}

		/// <summary>
		/// La suma de los dos, en la misma moneda. Accesor GUARDADO: se niega a cruzar monedas, asi que
		/// hay que preguntar antes. No existe la moneda base contra la que sumarlas.
		/// </summary>
		internal Money Plus(Money other)
		{
			if (!IsInTheSameCurrencyAs(other))
				throw new DomainException(
					"Se pidio sumar " + Expressed() + " con " + other.Expressed() + ": "
					+ _currency.Code + " con " + other._currency.Code + ". El dinero no se convierte, "
					+ "se registra, y agrupar por moneda es de quien consulta.");

			return new Money(_amount + other._amount, _currency);
		}

		/// <summary>
		/// La parte de este monto que le corresponde a esa fraccion del entero, en la MISMA moneda.
		/// Una parte que no es positiva, un entero que no lo es, o una parte mayor que el entero son
		/// defectos de quien llama.
		/// <para>
		/// No redondea: el redondeo vive donde el monto se DICE, una sola vez, en la precision de su
		/// propia moneda. Redondear aqui haria que las partes de un entero no sumaran el entero.
		/// </para>
		/// </summary>
		internal Money Prorated(long part, long whole)
		{
			if (whole <= 0)
				throw new DomainException(
					"Se pidio una parte de " + Expressed() + " sobre un entero de "
					+ whole.ToString(CultureInfo.InvariantCulture)
					+ ". Sin un entero positivo no hay fraccion que sacar.");

			if (part <= 0)
				throw new DomainException(
					"Se pidio una parte de " + part.ToString(CultureInfo.InvariantCulture)
					+ " de " + Expressed() + ". Una parte que no es positiva no es una parte, y un "
					+ "cero de dinero se lee como un gasto que hubo.");

			if (part > whole)
				throw new DomainException(
					"Se pidio la parte " + part.ToString(CultureInfo.InvariantCulture) + " de "
					+ whole.ToString(CultureInfo.InvariantCulture) + " de " + Expressed()
					+ ". Una parte mayor que el entero daria mas dinero del que hubo.");

			return new Money(_amount * part / whole, _currency);
		}

		/// <summary>
		/// El monto dicho POR EL DOMINIO, con el signo de su moneda y su precision. La caratula lo
		/// imprime y no compone nada.
		/// </summary>
		internal string Expressed()
		{
			return _currency.Says(_amount);
		}

		private static string AsSaid(decimal amount)
		{
			return amount.ToString(CultureInfo.InvariantCulture);
		}
	}
}
