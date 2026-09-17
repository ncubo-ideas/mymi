using System;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// REGLA 2 DE LA CONVENCION DE REGISTRO CRUDO (§8 de SLICE-CostLedger.md): "CADA FILA SE DESCRIBE A
	// SI MISMA. Nombra su propio asunto, lleva su propia moneda y su propia fecha, y dice de que
	// clasificacion es. Una fila que no significa nada sin la fila de al lado es una mitad con una
	// tabla encima."
	//
	// Y ES LA DISTINCION QUE HACE QUE AHalfOfAComposedAnswerNeverLeavesTheDoorAsANumber Y ESTA
	// CONVENCION VALGAN LAS DOS: una MITAD es un operando -- entregarla deja que una caratula arme una
	// frase rival -- y un REGISTRO es una fila. Lo que hace completa a una fila del libro es que todo
	// aquello con lo que se combinaria ya viene compuesto adentro: el dinero trae su moneda, el
	// instante trae su hora, y la frase de la fila dice las tres cosas juntas.
	//
	// LAS COLUMNAS SE AFIRMAN POR VALOR Y NO POR LARGO. Que dos columnas midan tres no dice que la
	// fila 2 de una corresponda con la fila 2 de la otra: lo que lo dice es que la fila que menciona
	// las llantas es la misma fila en las seis, y por eso cada assert de aca abajo mira el indice.
	[TestFixture]
	public sealed class EveryRowOfTheLedgerSaysItself
	{
		private const string TheVin = "1FTFW1ET5DFC10312";
		private const string TheFuel = "Gasolina de enero";
		private const string TheTyres = "Llantas nuevas";

		private static readonly DateTime TheStart = new DateTime(2026, 1, 1, 0, 0, 0);
		private static readonly DateTime TheEnd = new DateTime(2026, 12, 31, 0, 0, 0);

		private CostLedger ledger;

		[SetUp]
		public void Setup()
		{
			ledger = new CostLedger(TheVin);

			ledger.RecordRecurringCost(
				new DateTime(2026, 1, 15, 9, 30, 0), 25000m, "CRC", TheFuel);
			ledger.RecordDurableCost(
				new DateTime(2026, 2, 3, 14, 0, 0), 240000m, "CRC", TheTyres, 48);
		}

		[Test]
		public void EachRowNamesItsOwnSubjectInTheWordsItArrivedWith()
		{
			Assert.That(ledger.CostEntrySubjectsBetween(TheStart, TheEnd),
				Is.EqualTo(new[] { TheFuel, TheTyres }),
				"G3: el asunto es texto recibido y este registro no tiene catalogo de motivos, asi "
				+ "que sale tal como llego.");
		}

		[Test]
		public void EachRowCarriesItsOwnMoneyWithItsOwnCurrency()
		{
			Assert.That(TheSentenceTheDoorWouldCompose.EachCostEntryMoney(
				ledger, TheStart, TheEnd, TheLanguage.OfTheRecord),
				Is.EqualTo(new[] { "₡25.000,00", "₡240.000,00" }),
				"L1: el monto sale dicho con su moneda, siempre, y la moneda que el conjunto nombra "
				+ "se dice con su signo.");
		}

		// EL INSTANTE Y NO UN DIA DE CALENDARIO: sacar el dia local de un instante pide una zona que
		// este modulo no tiene, y G4 dice que los limites llegan como instantes.
		[Test]
		public void EachRowCarriesItsOwnInstantWithItsHour()
		{
			Assert.That(ledger.CostEntryInstantsBetween(TheStart, TheEnd),
				Is.EqualTo(new[] { "2026-01-15 09:30:00", "2026-02-03 14:00:00" }));
		}

		[Test]
		public void EachRowSaysWhichClassificationItIs()
		{
			Assert.That(ledger.CostEntryClassificationsBetween(TheStart, TheEnd),
				Is.EqualTo(new[] { "Recurring", "Durable" }),
				"§8 regla 2: la fila dice de que clasificacion es. Sin eso, dos filas de ₡240000 "
				+ "serian indistinguibles y la distincion que ES la respuesta se perderia en la "
				+ "tabla.");
		}

		// Y LA FRASE DE LA FILA ES LA FILA ENTERA DICHA, con la clasificacion en palabras: es lo que
		// hace que una fila sirva sola, sin encabezados y sin la de al lado.
		[Test]
		public void AndTheRowsOwnSentenceSaysAllOfItAtOnce()
		{
			string[] said = TheSentenceTheDoorWouldCompose.EachCostEntry(
				ledger, TheStart, TheEnd, TheLanguage.OfTheRecord);

			Assert.That(said[0],
				Does.Contain("₡25.000,00").And.Contain(TheFuel).And.Contain("2026-01-15 09:30:00"));
			Assert.That(said[0], Does.Contain("Recurrente"),
				"La clasificacion en palabras la dice el dominio, asi que ninguna caratula necesita "
				+ "una tabla propia -- que es el costo que DEC-84 midio.");

			Assert.That(said[1],
				Does.Contain("₡240.000,00").And.Contain(TheTyres).And.Contain("2026-02-03 14:00:00"));
			Assert.That(said[1], Does.Contain("Duradero").And.Contain("48 meses"),
				"La duradera dice ademas su vida util, que es la mitad que hace revisable la parte "
				+ "que se le imputa a un periodo.");
		}

		// LAS SEIS COLUMNAS SON LA MISMA FILA EN EL MISMO INDICE. Sin este assert, seis columnas de
		// largo tres pueden estar cruzadas y las seis pruebas de arriba seguirian pasando.
		[Test]
		public void AndTheSixColumnsLineUpRowByRowAndNotOnlyInLength()
		{
			string[] subjects = ledger.CostEntrySubjectsBetween(TheStart, TheEnd);
			string[] monies = TheSentenceTheDoorWouldCompose.EachCostEntryMoney(
				ledger, TheStart, TheEnd, TheLanguage.OfTheRecord);
			string[] classifications = ledger.CostEntryClassificationsBetween(TheStart, TheEnd);
			string[] lives = TheSentenceTheDoorWouldCompose.EachCostEntryExpectedLife(
				ledger, TheStart, TheEnd, TheLanguage.OfTheRecord);
			string[] instants = ledger.CostEntryInstantsBetween(TheStart, TheEnd);
			string[] sentences = TheSentenceTheDoorWouldCompose.EachCostEntry(
				ledger, TheStart, TheEnd, TheLanguage.OfTheRecord);

			int theTyresRow = Array.IndexOf(subjects, TheTyres);

			Assert.That(theTyresRow, Is.EqualTo(1));
			Assert.That(monies[theTyresRow], Is.EqualTo("₡240.000,00"));
			Assert.That(classifications[theTyresRow], Is.EqualTo("Durable"));
			Assert.That(lives[theTyresRow], Is.EqualTo("48 meses"));
			Assert.That(instants[theTyresRow], Is.EqualTo("2026-02-03 14:00:00"));
			Assert.That(sentences[theTyresRow], Does.Contain(TheTyres));
		}

		// Y LAS FILAS SON LAS QUE SE INCURRIERON ADENTRO, dicho aca porque es una eleccion y no una
		// consecuencia: una duradera comprada ANTES del periodo consume adentro y NO es una fila del
		// periodo, porque su dinero no salio del banco aca. Es lo que hace cierta la regla 4 sobre la
		// vista de caja, y la frase del periodo si nombra la fecha en que se compro.
		[Test]
		public void AndTheRowsAreTheEntriesIncurredInsideEvenWhenAnOlderOneStillConsumes()
		{
			DateTime marchFrom = new DateTime(2026, 3, 1, 0, 0, 0);
			DateTime marchTo = new DateTime(2026, 3, 31, 0, 0, 0);

			Assert.That(ledger.CostEntryCountBetween(marchFrom, marchTo), Is.EqualTo(0),
				"En marzo no se incurrio nada: las llantas se compraron en febrero.");
			Assert.That(ledger.HasAnyCostBetween(marchFrom, marchTo), Is.True,
				"Y sin embargo marzo SI tiene gasto, porque consumio parte de las llantas. Un cero "
				+ "de filas y un gasto que existe son dos hechos distintos y los dos se contestan.");
			Assert.That(TheSentenceTheDoorWouldCompose.TheSpend(ledger, marchFrom, marchTo, TheLanguage.OfTheRecord),
				Does.Contain("2026-02-03"),
				"Y la frase nombra la fecha en que se compro, asi que quien lea ve de donde sale la "
				+ "cifra sin tener la fila delante.");
		}
	}
}
