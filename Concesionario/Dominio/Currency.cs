using System;
using System.Globalization;

namespace Concesionario.Dominio
{
	/// <summary>
	/// La moneda en que esta dicho un monto: el conjunto cerrado de las que este modulo nombra, mas
	/// la variante de una que se RECIBIO sin que el conjunto la nombre. Se alcanza por un codigo en
	/// texto y se compara por identidad.
	/// <para>
	/// No lleva ningun factor de conversion, y esa ausencia es la invariante: un factor aqui no es una
	/// comodidad, es un defecto. Reconvertir un historico es falsear el pasado.
	/// </para>
	/// </summary>
	internal abstract class Currency
	{
		/// <summary>El colon costarricense, con dos decimales.</summary>
		internal static readonly Currency CRC = new ANamedCurrency("CRC", "₡", 2);

		/// <summary>El dolar estadounidense, con dos decimales.</summary>
		internal static readonly Currency USD = new ANamedCurrency("USD", "$", 2);

		/// <summary>
		/// El codigo con el que la moneda llego. Es lo que viaja por el wire y lo que queda escrito en
		/// el journal, tanto para una que este modulo nombra como para una que no.
		/// </summary>
		internal abstract string Code { get; }

		/// <summary>
		/// Si ese codigo nombra una de las monedas de este modulo. TOTAL: contesta, no revienta, y es
		/// la pregunta que la frontera hace antes de que haya nada que performar.
		/// </summary>
		internal static bool IsNamed(string code)
		{
			return !string.IsNullOrWhiteSpace(code) && !TheOneItNames(code).ReceivedButNotNamed();
		}

		/// <summary>
		/// Resuelve el codigo al miembro. TOTAL CON EXCEPCION: un codigo que el conjunto no nombra es
		/// un defecto de quien llama, y uno en blanco tampoco nombra ninguna.
		/// </summary>
		internal static Currency Of(string code)
		{
			if (string.IsNullOrWhiteSpace(code))
				throw new DomainException(
					"Se pidio una moneda sin decir cual. Un monto sin moneda no se puede leer, y aca "
					+ "las monedas no se convierten entre si. Las que hay son: "
					+ TheOnesThereAre() + ".");

			Currency resolved = TheOneItNames(code);

			if (resolved.ReceivedButNotNamed())
				throw new DomainException(
					"'" + code + "' no es una moneda que este modulo nombre. Las que hay son: "
					+ TheOnesThereAre() + ".");

			return resolved;
		}

		/// <summary>
		/// Resuelve el codigo de un monto que el registro YA GUARDO. TOTAL: si el conjunto no lo
		/// nombra contesta la variante recibida-y-sin-nombrar, porque rechazar un hecho ya escrito
		/// deja una entrada fallida en cada arranque futuro. Un codigo en blanco si revienta: ninguna
		/// entrada lo pudo guardar.
		/// </summary>
		internal static Currency OfPublished(string code)
		{
			if (string.IsNullOrWhiteSpace(code))
				throw new DomainException(
					"Llego un monto publicado sin decir en que moneda. Un monto sin moneda no se puede "
					+ "leer, y ninguna entrada lo pudo guardar asi.");

			return TheOneItNames(code);
		}

		/// <summary>
		/// Los codigos que este modulo nombra, dichos en una lista, para que un rechazo diga contra
		/// que se miro. Es el unico sitio donde esa lista se escribe.
		/// </summary>
		internal static string TheOnesThereAre()
		{
			return CRC.Code + ", " + USD.Code;
		}

		/// <summary>
		/// Recibida y sin nombrar: el conjunto no la nombra, asi que no se sabe su simbolo ni cuantos
		/// decimales tiene. TOTAL.
		/// </summary>
		internal virtual bool ReceivedButNotNamed()
		{
			return false;
		}

		/// <summary>
		/// Si se SABE que las dos son la misma moneda. TOTAL. Una que este modulo no nombra no lo sabe
		/// de ninguna, ni de otra con su mismo codigo.
		/// </summary>
		internal abstract bool IsTheSameCurrencyAs(Currency other);

		/// <summary>
		/// El monto dicho en esta moneda, en cultura invariante: la frase la leen tres caratulas con
		/// tres culturas y tiene que decir el mismo numero.
		/// </summary>
		internal abstract string Says(decimal amount);

		/// <summary>
		/// El unico sitio que enumera el conjunto. Los tres miembros de arriba lo envuelven, asi que
		/// no hay dos enumeraciones que puedan discrepar.
		/// </summary>
		private static Currency TheOneItNames(string code)
		{
			if (string.Equals(CRC.Code, code, StringComparison.Ordinal)) return CRC;
			if (string.Equals(USD.Code, code, StringComparison.Ordinal)) return USD;

			return new ACurrencyThisModuleDoesNotName(code);
		}
	}

	/// <summary>
	/// Una de las monedas que este modulo nombra: su codigo, el signo con el que una persona la lee y
	/// cuantos decimales tiene, que es un hecho de ISO 4217 recibido del mundo y no una eleccion de
	/// este modulo.
	/// </summary>
	internal sealed class ANamedCurrency : Currency
	{
		private readonly string _code;
		private readonly string _symbol;
		private readonly int _minorUnits;

		/// <summary>
		/// Se llega por los miembros estaticos de <see cref="Currency"/>, que son los unicos que el
		/// canon nombra.
		/// </summary>
		internal ANamedCurrency(string code, string symbol, int minorUnits)
		{
			if (string.IsNullOrWhiteSpace(code))
				throw new DomainException(
					"Se declaro una moneda sin codigo. El codigo es lo que viaja y lo que queda en el "
					+ "journal: sin el no hay como nombrarla.");

			if (string.IsNullOrWhiteSpace(symbol))
				throw new DomainException(
					"Se declaro la moneda '" + code + "' sin el signo con que una persona la lee. Una "
					+ "moneda que este modulo nombra se dice con su signo.");

			if (minorUnits < 0 || minorUnits > 28)
				throw new DomainException(
					"Se declaro la moneda '" + code + "' con "
					+ minorUnits.ToString(CultureInfo.InvariantCulture) + " decimales. Los decimales "
					+ "de una moneda son un hecho de ISO 4217 y ninguna los tiene asi.");

			_code = code;
			_symbol = symbol;
			_minorUnits = minorUnits;
		}

		/// <summary>El codigo con que el canon la nombra.</summary>
		internal override string Code
		{
			get { return _code; }
		}

		/// <summary>Si es la misma moneda: por identidad, que es lo unico que este tipo compara.</summary>
		internal override bool IsTheSameCurrencyAs(Currency other)
		{
			return ReferenceEquals(this, other);
		}

		/// <summary>
		/// El monto con su signo delante y los decimales de esta moneda, redondeado al ultimo de ellos
		/// alejandose del cero. Es el unico numero que este dominio inventa: la precision, nunca la
		/// magnitud.
		/// </summary>
		internal override string Says(decimal amount)
		{
			decimal said = Math.Round(amount, _minorUnits, MidpointRounding.AwayFromZero);

			return _symbol + said.ToString(TheShapeOfItsMinorUnits(), CultureInfo.InvariantCulture);
		}

		private string TheShapeOfItsMinorUnits()
		{
			return _minorUnits == 0 ? "0" : "0." + new string('0', _minorUnits);
		}
	}

	/// <summary>
	/// Una moneda que llego con un monto y que este conjunto no nombra. Se dice con su codigo y el
	/// monto tal como se recibio, porque no se conoce su signo ni cuantos decimales tiene, y no se
	/// compara con ninguna: dos montos en una moneda que nadie nombro no se SABE que esten en la misma.
	/// </summary>
	internal sealed class ACurrencyThisModuleDoesNotName : Currency
	{
		private readonly string _code;

		/// <summary>Se llega por <see cref="Currency.OfPublished"/> y por ningun otro sitio.</summary>
		internal ACurrencyThisModuleDoesNotName(string code)
		{
			if (string.IsNullOrWhiteSpace(code))
				throw new DomainException(
					"Se recibio una moneda sin codigo. Sin codigo no hay nada que decir de ella, ni "
					+ "siquiera que este modulo no la nombra.");

			_code = code;
		}

		/// <summary>El codigo tal como llego. Este modulo no le arregla la ortografia a nadie.</summary>
		internal override string Code
		{
			get { return _code; }
		}

		/// <summary>Recibida y sin nombrar: si.</summary>
		internal override bool ReceivedButNotNamed()
		{
			return true;
		}

		/// <summary>
		/// Nunca. No se sabe que sea la misma que ninguna otra, ni que otra con su mismo codigo: sin
		/// que el conjunto la nombre, este modulo no tiene con que saberlo.
		/// </summary>
		internal override bool IsTheSameCurrencyAs(Currency other)
		{
			return false;
		}

		/// <summary>
		/// El monto tal como se recibio, con el codigo detras. Redondearlo inventaria una precision
		/// que nadie publico.
		/// </summary>
		internal override string Says(decimal amount)
		{
			return amount.ToString(CultureInfo.InvariantCulture) + " " + _code;
		}
	}
}
