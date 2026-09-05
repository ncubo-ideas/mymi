using System;
using System.Globalization;
using System.Threading;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// §4 de SLICE-CostLedger.md: Money.Expressed es "el monto dicho con su simbolo, POR EL DOMINIO".
	// La caratula lo imprime y no compone nada (E40).
	//
	// EL REDONDEO ES EL UNICO NUMERO QUE ESTA REBANADA INVENTA, y sale afirmado y no comentado. DEC-77
	// midio que una decision argumentada solo en un /// no la sostiene la suite: falsificar el
	// redondeo tiene que poner algo rojo. Por eso hay casos cuyo resultado DIFIERE entre
	// MidpointRounding.AwayFromZero y MidpointRounding.ToEven -- 0.125 y 0.145 con dos decimales --
	// porque con cualquier otro monto los dos modos coinciden y la eleccion queda sin defender.
	//
	// LA CULTURA SE AFIRMA, NO SE SUPONE. Cargas tiene un tipo Cifras cuyo argumento es que
	// "{Monto:0.##}" usa la cultura DEL PROCESO, asi que una misma frase compuesta se lee "1234.5" en
	// una consola y "1234,5" en un browser en espanol -- "y esa frase la lee tambien el MCP". Aca la
	// frase tiene TRES lectores con tres culturas. Se adopta el argumento y no el tipo: cada miembro
	// que dice un numero usa CultureInfo.InvariantCulture explicitamente, y esta prueba cambia la
	// cultura del hilo para que un olvido se vea.
	//
	// Y EL SIMBOLO ES NO-ASCII A PROPOSITO. El colon se dice con su signo porque es lo que una persona
	// lee; la regla de este modulo es que la prosa que la persona lee lleva sus acentos y sus signos, y
	// que los /// y los mensajes de DomainException se quedan en ASCII sin acentos.
	[TestFixture]
	public sealed class MoneyIsSaidWithItsSymbolAndItsMinorUnits
	{
		[Test]
		public void TheColonIsSaidWithItsSignAndTwoDecimals()
		{
			Assert.That(new Money(85000m, Currency.CRC).Expressed(), Is.EqualTo("₡85000.00"));
		}

		[Test]
		public void TheDollarIsSaidWithItsSignAndTwoDecimals()
		{
			Assert.That(new Money(120.5m, Currency.USD).Expressed(), Is.EqualTo("$120.50"));
		}

		// PROBE 9 midio que la ESCALA de un decimal no sobrevive al journal: 145.00 escrito sale 145
		// leido. Decir el monto con los decimales de la moneda hace que esa perdida no se vea en ESTA
		// frase -- que es una razon para que la migracion de CL-2 guste, no una razon para saltarse el
		// Normalized de la puerta.
		[TestCase("145.00")]
		[TestCase("145.0")]
		[TestCase("145")]
		public void TheScaleTheJournalDropsDoesNotChangeThisSentence(string monto)
		{
			decimal valor = decimal.Parse(monto, CultureInfo.InvariantCulture);

			Assert.That(new Money(valor, Currency.CRC).Expressed(), Is.EqualTo("₡145.00"));
		}

		// EL CASO QUE DISCRIMINA EL MODO. Con AwayFromZero 0.125 se dice 0.13 y 0.145 se dice 0.15;
		// con ToEven se dirian 0.12 y 0.14. Con cualquier otro par los dos modos coinciden.
		[TestCase("0.125", "₡0.13")]
		[TestCase("0.145", "₡0.15")]
		public void TheMidpointGoesAwayFromZeroAndNotToTheEvenDigit(string monto, string dicho)
		{
			decimal valor = decimal.Parse(monto, CultureInfo.InvariantCulture);

			Assert.That(new Money(valor, Currency.CRC).Expressed(), Is.EqualTo(dicho));
		}

		[TestCase("85000.004", "₡85000.00")]
		[TestCase("85000.006", "₡85000.01")]
		public void MoreDecimalsThanTheCurrencyHasAreRoundedToTheOnesItHas(string monto, string dicho)
		{
			decimal valor = decimal.Parse(monto, CultureInfo.InvariantCulture);

			Assert.That(new Money(valor, Currency.CRC).Expressed(), Is.EqualTo(dicho));
		}

		// LA CULTURA DEL PROCESO NO CAMBIA LA FRASE. Se prueba con una cultura cuyo separador decimal
		// es la coma, que es exactamente la del browser de la persona.
		[TestCase("es-CR")]
		[TestCase("de-DE")]
		[TestCase("en-US")]
		public void TheSentenceSaysTheSameNumberInEveryCultureTheProcessCouldBeIn(string cultura)
		{
			CultureInfo previa = Thread.CurrentThread.CurrentCulture;

			try
			{
				Thread.CurrentThread.CurrentCulture = new CultureInfo(cultura);

				Assert.That(new Money(120.5m, Currency.USD).Expressed(), Is.EqualTo("$120.50"));
				Assert.That(
					new Money(85000.1m, Currency.OfPublished("XYZ")).Expressed(),
					Is.EqualTo("85000.1 XYZ"));
			}
			finally
			{
				Thread.CurrentThread.CurrentCulture = previa;
			}
		}

		// UNA MONEDA QUE ESTE MODULO NO NOMBRA SE DICE DE OTRA FORMA, y la diferencia es honesta: no
		// se conoce su simbolo ni cuantos decimales tiene, asi que el monto sale TAL COMO SE RECIBIO
		// con el codigo detras. Redondearlo a dos decimales seria inventar una precision que nadie
		// publico. La frase es la misma que el arbol sin migrar ya decia (PROBE 8, letra por letra).
		[Test]
		public void ACurrencyThisModuleDoesNotNameSaysItsCodeAndTheAmountAsItArrived()
		{
			Currency recibida = Currency.OfPublished("XYZ");

			Assert.That(new Money(85000.1m, recibida).Expressed(), Is.EqualTo("85000.1 XYZ"));
			Assert.That(new Money(85000.125m, recibida).Expressed(), Is.EqualTo("85000.125 XYZ"),
				"Sin decimales publicados no hay a que redondear, asi que el monto sale entero.");
		}

		// UN SOLO AUTOR DE LA FRASE. Money delega en su moneda, que es la que tiene el simbolo y los
		// decimales (Information Expert), asi que no hay dos sitios que puedan discrepar -- y ninguna
		// caratula necesita un formateador propio, que seria un segundo autor.
		[Test]
		public void TheMoneyAndItsCurrencySayTheSameThing()
		{
			Assert.That(new Money(85000m, Currency.CRC).Expressed(),
				Is.EqualTo(Currency.CRC.Says(85000m)));
			Assert.That(new Money(120.5m, Currency.USD).Expressed(),
				Is.EqualTo(Currency.USD.Says(120.5m)));
		}

		[Test]
		public void ZeroIsSaidWithItsSymbolTooAndIsNotAnAbsence()
		{
			Assert.That(new Money(0m, Currency.CRC).Expressed(), Is.EqualTo("₡0.00"));
		}
	}
}
