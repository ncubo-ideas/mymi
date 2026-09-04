using System;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// REGLA 4 DE LA CONVENCION DE REGISTRO CRUDO (§8 de SLICE-CostLedger.md): "el total del dominio y
	// la suma de las filas coinciden, y un test lo dice. No porque la caratula pueda sumarlas, sino
	// para que nunca este tentada ni tenga razon."
	//
	// Y ES LA TRAMPA DE E49 CON UN TOTAL PUESTO. El total y las filas son DOS CONSUMIDORES DE UNA
	// MISMA ENUMERACION -- las dos abren por TheSpendBetween -- asi que coinciden POR CONSTRUCCION,
	// incluido cuando la enumeracion esta uniformemente mal. Un test que comparara el total contra la
	// suma de las filas seria verde con un periodo mal acotado, con una entrada de otro ano adentro y
	// con la lista entera vacia.
	//
	// ASI QUE UN LADO ESTA ANCLADO A LO QUE SE RECIBIO, POR LITERAL. Los montos entran como ₡25000,
	// ₡40000 y ₡100000; el total se afirma contra ₡165000, escrito a mano en el test y no calculado
	// desde las filas. Y hay una cuarta entrada FUERA del periodo, de ₡999000: si la enumeracion se
	// acotara mal, las filas y el total seguirian coincidiendo -- en ₡1164000 -- y es el ancla la que
	// lo ve.
	//
	// UN LIMITE, DICHO EN VEZ DE CUBIERTO CON UN TEST QUE PAREZCA COBERTURA (C56). Esta regla se puede
	// afirmar de la vista de CAJA y NO de la vista de lo CONSUMIDO: las filas llevan el dinero de cada
	// entrada -- la fila es un registro completo -- y no llevan la parte que el periodo consumio de
	// ella. Una columna de partes seria una mitad de la respuesta de L6 puesta en una tabla, que es
	// exactamente lo que §8 llama "una mitad con una tabla encima". Asi que la vista consumida no
	// tiene suma de filas contra la que compararse, y eso queda escrito aca.
	[TestFixture]
	public sealed class TheDomainsTotalsEqualTheSumsOverTheRows
	{
		private const string TheVin = "1FTFW1ET5DFC10312";

		private static readonly DateTime TheStart = new DateTime(2026, 1, 1, 0, 0, 0);
		private static readonly DateTime TheEnd = new DateTime(2026, 12, 31, 0, 0, 0);

		private CostLedger ledger;

		[SetUp]
		public void Setup()
		{
			ledger = new CostLedger(TheVin);

			ledger.RecordRecurringCost(
				new DateTime(2026, 1, 15, 9, 0, 0), 25000m, "CRC", "Gasolina de enero");
			ledger.RecordRecurringCost(
				new DateTime(2026, 3, 20, 9, 0, 0), 40000m, "CRC", "Cambio de aceite");
			ledger.RecordRecurringCost(
				new DateTime(2026, 7, 4, 9, 0, 0), 100000m, "CRC", "Frenos");

			ledger.RecordRecurringCost(
				new DateTime(2025, 11, 2, 9, 0, 0), 999000m, "CRC", "Reparación del año anterior");
		}

		// EL ANCLA, PRIMERA MITAD: las filas son las tres que se recibieron adentro del periodo, con
		// sus montos tal como entraron y en el orden en que se incurrieron.
		[Test]
		public void TheRowsAreTheThreeThatWereReceivedInsideThePeriod()
		{
			Assert.That(ledger.CostEntryCountBetween(TheStart, TheEnd), Is.EqualTo(3));
			Assert.That(ledger.CostEntryMoniesBetween(TheStart, TheEnd),
				Is.EqualTo(new[] { "₡25000.00", "₡40000.00", "₡100000.00" }));
		}

		// EL ANCLA, SEGUNDA MITAD Y LA QUE HACE EL TEST: el total se compara contra la suma de los
		// literales recibidos, no contra la suma de las filas.
		[Test]
		public void AndTheDomainsCashOutTotalIsTheSumOfTheAmountsThatWereReceived()
		{
			Assert.That(ledger.DescribeSpendBetween(TheStart, TheEnd),
				Does.Contain("₡165000.00"),
				"25000 + 40000 + 100000, sumado a mano en el test. Comparar el total contra la suma "
				+ "de las filas es comparar dos consumidores de una misma enumeracion, y coinciden "
				+ "aunque la enumeracion este mal.");
		}

		// Y LA ENTRADA DE FUERA NO ESTA EN NINGUNO DE LOS DOS. Sin esta mitad, el ancla de arriba
		// pasaria con un periodo que no acota nada, porque las tres de adentro tambien estarian.
		[Test]
		public void AndTheEntryFromOutsideThePeriodIsInNeitherTheRowsNorTheTotal()
		{
			Assert.That(ledger.CostEntryMoniesBetween(TheStart, TheEnd),
				Does.Not.Contain("₡999000.00"));
			Assert.That(ledger.DescribeSpendBetween(TheStart, TheEnd),
				Does.Not.Contain("₡999000.00").And.Not.Contain("₡1164000.00"),
				"₡1164000 es lo que dirian LAS DOS -- el total y la suma de las filas -- si el "
				+ "periodo no acotara: coincidiendo, y mal.");
		}

		// Y EL PERIODO DE AFUERA CONTESTA LO SUYO, que es la otra direccion del mismo acotamiento: sin
		// esto, una enumeracion que devolviera siempre vacio tambien pasaria los tres de arriba.
		[Test]
		public void AndThePeriodThatDoesHoldItAnswersWithItAlone()
		{
			DateTime from = new DateTime(2025, 1, 1, 0, 0, 0);
			DateTime to = new DateTime(2025, 12, 31, 0, 0, 0);

			Assert.That(ledger.CostEntryMoniesBetween(from, to),
				Is.EqualTo(new[] { "₡999000.00" }));
			Assert.That(ledger.DescribeSpendBetween(from, to), Does.Contain("₡999000.00"));
		}

		// LAS SEIS COLUMNAS SON PARALELAS, y lo son porque las seis abren por el mismo resolutor. Es
		// DEC-53 en este agregado: un largo distinto entre dos columnas cruzaria las filas.
		[Test]
		public void AndTheSixColumnsAreParallelBecauseTheSixOpenWithOneResolver()
		{
			Assert.That(ledger.CostEntryInstantsBetween(TheStart, TheEnd), Has.Length.EqualTo(3));
			Assert.That(ledger.CostEntrySubjectsBetween(TheStart, TheEnd), Has.Length.EqualTo(3));
			Assert.That(ledger.CostEntryMoniesBetween(TheStart, TheEnd), Has.Length.EqualTo(3));
			Assert.That(
				ledger.CostEntryClassificationsBetween(TheStart, TheEnd), Has.Length.EqualTo(3));
			Assert.That(
				ledger.CostEntryExpectedLivesBetween(TheStart, TheEnd), Has.Length.EqualTo(3));
			Assert.That(
				ledger.DescribeEachCostEntryBetween(TheStart, TheEnd), Has.Length.EqualTo(3));
		}

		// EL LIMITE, AFIRMADO COMO LIMITE: no hay ninguna columna de partes consumidas, y por eso la
		// vista consumida no tiene suma de filas. Se afirma la AUSENCIA de la columna, que es lo que
		// se puede afirmar, en vez de un test que compare la vista consumida contra algo.
		[Test]
		public void AndThereIsNoRowColumnOfSharesSoTheConsumedViewHasNoSumToCompareAgainst()
		{
			Assert.That(typeof(CostLedger).GetMethod("CostEntrySharesBetween",
					System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public
					| System.Reflection.BindingFlags.NonPublic),
				Is.Null,
				"Una columna de partes consumidas por fila es una mitad de la respuesta de L6 con "
				+ "una tabla encima: quien la tuviera podria sumarla y decir un consumido que el "
				+ "dominio no dijo, o mostrarla sin la caja al lado.");
		}
	}
}
