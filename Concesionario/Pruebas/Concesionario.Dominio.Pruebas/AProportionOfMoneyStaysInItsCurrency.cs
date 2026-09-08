using System;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LA PARTE AMORTIZADA ES ARITMETICA SOBRE UN MONTO, Y EL MONTO ES DE Money. G2 fija la regla --
	// linea recta sobre la ventana de vida, por solape -- y esa regla necesita multiplicar el monto
	// por una fraccion. Money no entrega el monto (L1) y ningun otro tipo puede tenerlo, asi que la
	// proporcion tiene que ser una operacion DE Money: es Information Expert, y es la unica forma que
	// no rompe L1.
	//
	// LO QUE ESTE ARCHIVO FIJA ES QUE ESA OPERACION NO ES UNA GRIETA. Devuelve un Money entero, en la
	// MISMA moneda, y no hay forma de sacarle una mitad: es el primer miembro nuevo de Money desde
	// CL-1 y el argumento por el que no es un accesor de moneda esta en DECISIONS.md.
	//
	// Y NO REDONDEA. R7 del plan dice que en este plan el dominio autora DOS redondeos -- el de OD-1 y
	// el de DEC-87 -- y que un tercero necesita su propio argumento. La proporcion no lo tiene: el
	// redondeo vive donde el monto se DICE, en Currency.Says, una sola vez. El test que lo mide son
	// tres tercios de ₡100 sumados con Plus: sin redondeo dicen ₡100.00, y con un redondeo a dos
	// decimales adentro dirian ₡99.99.
	[TestFixture]
	public sealed class AProportionOfMoneyStaysInItsCurrency
	{
		private const string TheSubject = "Llantas nuevas";

		private static readonly DateTime Incurred = new DateTime(2026, 1, 1, 0, 0, 0);

		[Test]
		public void AProportionOfMoneyIsSaidInTheSameCurrencyItWasIn()
		{
			Money whole = new Money(240000m, Currency.CRC);

			Assert.That(whole.Prorated(1, 4).Expressed(), Is.EqualTo("₡60000.00"));
			Assert.That(whole.IsInTheSameCurrencyAs(whole.Prorated(1, 4)), Is.True,
				"Una proporcion de un monto sigue siendo dinero de la misma moneda: es lo que "
				+ "permite sumarla con las demas del periodo.");
		}

		[Test]
		public void AndTheWholeProportionIsTheWholeMoney()
		{
			Assert.That(new Money(240000m, Currency.CRC).Prorated(4, 4).Expressed(),
				Is.EqualTo("₡240000.00"));
		}

		// EL TERCER REDONDEO QUE NO SE AUTORO, medido: tres tercios que se suman entero.
		[Test]
		public void TheProportionIsNotRoundedBecauseTheRoundingLivesWhereTheMoneyIsSaid()
		{
			Money whole = new Money(100m, Currency.CRC);
			Money aThird = whole.Prorated(1, 3);

			Assert.That(aThird.Plus(aThird).Plus(aThird).Expressed(), Is.EqualTo("₡100.00"),
				"Tres tercios de ₡100 son ₡100. Con un redondeo a dos decimales adentro de la "
				+ "proporcion serian ₡99.99, y el modulo tendria un tercer redondeo que nadie "
				+ "argumento (R7).");
			Assert.That(aThird.Expressed(), Is.EqualTo("₡33.33"),
				"Y al DECIRSE una sola parte, se dice con la precision de su moneda: ese redondeo "
				+ "es el de DEC-87 y vive en un solo sitio.");
		}

		// LAS TRES GUARDAS, CADA UNA CON SU TEST DIRECTO. CL-2 §12 midio tres guardas de este modulo
		// que no ponen rojo ningun test cuando se borran, y la respuesta cada vez es un test directo.
		// LA GUARDA DEL ENTERO ES LOAD-BEARING PARA EL MENSAJE Y NO PARA EL TIPO, y se midio: borrarla
		// no pone rojo NADA si el test solo exige que algo reviente, porque la guarda de "parte mayor
		// que el entero" tira un DomainException indistinguible -- con un entero de cero, cualquier
		// parte positiva es mayor que el. Es la distincion de DEC-65 y DEC-90 en un cuarto sitio, y la
		// forma de defender una guarda que solo aporta el mensaje es un test que NOMBRE el mensaje.
		[Test]
		public void AProportionOfNothingIsRefusedAndTheRefusalNamesTheWholeAndNotThePart()
		{
			Money whole = new Money(240000m, Currency.CRC);

			DomainException refused = Assert.Throws<DomainException>(() => whole.Prorated(1, 0),
				"Dividir entre cero no es una proporcion. Y una ventana de vida de largo cero es la "
				+ "vida no positiva que L5 ya rechaza un nivel mas arriba.");

			Assert.That(refused.Message, Does.Contain("entero de 0"),
				"El rechazo dice que el ENTERO no sirve. Sin esta mitad, la guarda del entero es "
				+ "invisible: la de la parte mayor que el entero tira un rechazo del mismo tipo.");

			Assert.That(Assert.Throws<DomainException>(() => whole.Prorated(1, -4)).Message,
				Does.Contain("entero de -4"));
		}

		[Test]
		public void AProportionOfNonePartIsRefused()
		{
			Money whole = new Money(240000m, Currency.CRC);

			Assert.Throws<DomainException>(() => whole.Prorated(0, 4),
				"Una parte de cero no es una parte: seria un cero devuelto por una pregunta que "
				+ "habia que no hacer, y un cero de dinero se lee como un gasto.");
			Assert.Throws<DomainException>(() => whole.Prorated(-1, 4));
		}

		[Test]
		public void APartLargerThanTheWholeIsRefused()
		{
			DomainException refused = Assert.Throws<DomainException>(
				() => new Money(240000m, Currency.CRC).Prorated(5, 4));

			Assert.That(refused.Message, Does.Contain("5").And.Contain("4"),
				"El rechazo nombra las dos: una parte mayor que el entero daria una parte mayor que "
				+ "el gasto, y eso se veria plausible.");
		}

		[Test]
		public void AndEveryOneOfThoseRefusalsIsTheDomainsOwn()
		{
			Assert.That(Assert.Catch(() => new Money(1m, Currency.CRC).Prorated(1, 0)),
				Is.InstanceOf<DomainException>().And.Not.InstanceOf<ArgumentException>(),
				"C22/C36/C37: una guarda que tira otra cosa se escapa de Apply.");
		}

		// LAS GUARDAS DE LA PARTE, tambien directas: los tres colaboradores requeridos y el estado que
		// no tiene parte que decir.
		[Test]
		public void AShareNeedsTheMoneyTheSubjectAndTheLifeAndSaysWhichIsMissing()
		{
			Money whole = new Money(240000m, Currency.CRC);
			CostSubject subject = new CostSubject(TheSubject);
			AnExpectedLife life = new AnExpectedLife(4);

			DateTime from = new DateTime(2026, 1, 1, 0, 0, 0);
			DateTime to = new DateTime(2026, 2, 1, 0, 0, 0);

			Assert.Throws<DomainException>(
				() => AnAmortizedShare.Of(null, subject, life, Incurred, from, to));
			Assert.Throws<DomainException>(
				() => AnAmortizedShare.Of(whole, null, life, Incurred, from, to));

			DomainException refused = Assert.Throws<DomainException>(
				() => AnAmortizedShare.Of(whole, subject, null, Incurred, from, to));

			Assert.That(refused.Message, Does.Contain("vida"),
				"Sin la vida util no hay ventana sobre la que repartir, y decirlo es la mitad de "
				+ "L5.");
		}

		// Y UNA PARTE DE UN PERIODO QUE NO TOCA LA VENTANA ES UN DEFECTO DE QUIEN LLAMA, no un cero:
		// es E41 -- un accesor sin clave sobre un opcional ausente revienta, y la pregunta total que
		// hay que hacer antes es ConsumesAnythingBetween.
		[Test]
		public void AndAShareOfAPeriodTheWindowDoesNotTouchIsACallerBugAndNotAZero()
		{
			DomainException refused = Assert.Throws<DomainException>(
				() => AnAmortizedShare.Of(
					new Money(240000m, Currency.CRC), new CostSubject(TheSubject),
					new AnExpectedLife(4), Incurred,
					new DateTime(2027, 1, 1, 0, 0, 0), new DateTime(2027, 2, 1, 0, 0, 0)));

			Assert.That(refused.Message, Does.Contain("consumio"),
				"Sin acento: un mensaje de DomainException es ASCII sin acentos, y la frase que una "
				+ "persona lee es otra cosa. El rechazo dice que ese periodo no consumio nada de "
				+ "esa entrada, y no devuelve un "
				+ "cero: un cero de dinero se lee como que el periodo consumio nada de algo que "
				+ "estaba activo, que es otro hecho.");
		}
	}
}
