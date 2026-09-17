using System;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LEY L3 de SLICE-CostLedger.md: "un periodo que tiene dos monedas contesta con CADA UNA DICHA
	// POR SU LADO, en un estado con nombre, y sin ningun total entre ellas. Nunca cero, nunca la
	// mayor, nunca un throw que la caratula tenga que interpretar."
	//
	// LA LEY ES ESTRUCTURAL ANTES DE SER UNA PRUEBA, y por eso el primer test de aca abajo es el que
	// mide eso: Money.Plus SE NIEGA a cruzar monedas (CL-1), asi que un total entre dos monedas no
	// existe como valor y no hay forma de decirlo sin escribirlo a mano. Lo que estos tests miden es
	// lo otro: que el agregado NO reaccione a eso reventando, ni contestando la mayor, ni sumando
	// numeros pelados por su cuenta.
	//
	// Y LA CONSECUENCIA DE DEC-88 SE FIJA ACA TAMBIEN, porque sorprende: una moneda que el conjunto
	// no nombra no se compara con ninguna, ni con otra de su mismo codigo, asi que dos gastos en
	// 'XYZ' son DOS grupos. Es alcanzable solo a nivel A -- el door y el feed la rechazan por L14 --
	// y esta fijado para que quien lo vea sepa que es la decision y no un defecto.
	[TestFixture]
	public sealed class ATotalAcrossTwoCurrenciesIsNotANumber
	{
		private const string TheVin = "1FTFW1ET5DFC10312";

		private static readonly DateTime TheStart = new DateTime(2026, 1, 1, 0, 0, 0);
		private static readonly DateTime TheEnd = new DateTime(2026, 3, 31, 0, 0, 0);

		private CostLedger ledger;

		[SetUp]
		public void Setup()
		{
			ledger = new CostLedger(TheVin);
		}

		// La mitad estructural: no hay ningun valor que sea el total de dos monedas.
		[Test]
		public void TwoMoniesInDifferentCurrenciesCannotBeAddedAtAll()
		{
			Money inColones = new Money(85000m, Currency.CRC);
			Money inDollars = new Money(120m, Currency.USD);

			Assert.That(inColones.IsInTheSameCurrencyAs(inDollars), Is.False);
			Assert.Throws<DomainException>(() => inColones.Plus(inDollars),
				"L2: el dinero no se convierte. Un total entre dos monedas no es un valor que el "
				+ "dominio pueda construir, y esa es la mitad estructural de L3.");
		}

		[Test]
		public void APeriodWithTwoCurrenciesIsNotSaidInOneCurrency()
		{
			ledger.RecordRecurringCost(
				new DateTime(2026, 1, 15, 9, 0, 0), 85000m, "CRC", "Gasolina de enero");
			ledger.RecordRecurringCost(
				new DateTime(2026, 2, 10, 9, 0, 0), 120m, "USD", "Repuesto importado");

			Assert.That(ledger.HasAnyCostBetween(TheStart, TheEnd), Is.True);
			Assert.That(ledger.IsSpendSaidInOneCurrencyBetween(TheStart, TheEnd), Is.False);
		}

		// EL CENTRO DE LA LEY. Las dos monedas salen dichas, cada una con su propio signo y su propio
		// monto, y la frase dice que no hay total entre ellas. No dice un numero.
		[Test]
		public void EachCurrencyIsSaidOnItsOwnAndNoTotalCrossesThem()
		{
			ledger.RecordRecurringCost(
				new DateTime(2026, 1, 15, 9, 0, 0), 85000m, "CRC", "Gasolina de enero");
			ledger.RecordRecurringCost(
				new DateTime(2026, 2, 10, 9, 0, 0), 120m, "USD", "Repuesto importado");

			string said = TheSentenceTheDoorWouldCompose.TheSpendPerCurrency(ledger, TheStart, TheEnd, TheLanguage.OfTheRecord);

			Assert.That(said, Does.Contain("₡85.000,00"),
				"La moneda que el conjunto nombra se dice con su signo, por DEC-87.");
			Assert.That(said, Does.Contain("$120,00"));
			Assert.That(said, Does.Contain("2 monedas"),
				"El estado con nombre dice CUANTAS monedas hay, que es lo que hace la frase "
				+ "revisable.");
			Assert.That(said, Does.Contain("no hay un total entre ellas"));
		}

		// NI CERO NI LA MAYOR NI UN THROW: los tres desenlaces que L3 nombra, uno por uno.
		[Test]
		public void AndTheAnswerIsNeitherZeroNorTheLargerNorAThrow()
		{
			ledger.RecordRecurringCost(
				new DateTime(2026, 1, 15, 9, 0, 0), 85000m, "CRC", "Gasolina de enero");
			ledger.RecordRecurringCost(
				new DateTime(2026, 2, 10, 9, 0, 0), 120m, "USD", "Repuesto importado");

			string whole = TheSentenceTheDoorWouldCompose.TheSpend(ledger, TheStart, TheEnd, TheLanguage.OfTheRecord);

			Assert.That(whole, Does.Not.Contain("85120"),
				"Sumar 85000 con 120 seria decir un total que el dominio no puede construir.");
			Assert.That(whole, Does.Contain("₡85.000,00").And.Contain("$120,00"),
				"Las dos siguen dichas: contestar solo la mayor perderia una de las dos.");
			Assert.That(whole, Does.Not.Contain("₡0").And.Not.Contain("$0"));
		}

		// DEC-88 medido desde este lado: una moneda sin nombrar no se compara ni consigo misma, asi
		// que dos gastos en 'XYZ' son dos grupos. El door la rechaza gratis y el feed la descarta
		// (L14), asi que este estado solo se alcanza aca; queda fijado para que no sorprenda.
		[Test]
		public void TwoCostsInACurrencyThisModuleDoesNotNameAreTwoGroupsAndThatIsDEC88()
		{
			ledger.RecordRecurringCost(
				new DateTime(2026, 1, 15, 9, 0, 0), 500m, "XYZ", "Peaje en moneda ajena");
			ledger.RecordRecurringCost(
				new DateTime(2026, 1, 16, 9, 0, 0), 700m, "XYZ", "Otro peaje en la misma");

			Assert.That(ledger.IsSpendSaidInOneCurrencyBetween(TheStart, TheEnd), Is.False,
				"DEC-88: de dos montos en una moneda que nadie nombro no se SABE que esten en la "
				+ "misma, asi que decir que hay una sola moneda seria decir algo que el modulo no "
				+ "sabe.");

			string said = TheSentenceTheDoorWouldCompose.TheSpendPerCurrency(ledger, TheStart, TheEnd, TheLanguage.OfTheRecord);

			Assert.That(said, Does.Contain("500 XYZ").And.Contain("700 XYZ"));
			Assert.That(said, Does.Not.Contain("1200"),
				"Y no se suman: es exactamente el mismo rechazo que entre CRC y USD.");
		}
	}
}
