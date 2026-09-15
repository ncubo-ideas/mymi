using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LAS RESPUESTAS DE DINERO, DICHAS POR VALOR EN LOS DOS IDIOMAS: el gasto de un periodo con sus
	// dos vistas, ese mismo periodo moneda por moneda, dos periodos comparados, la fila de cada
	// entrada, el costo por distancia y el precio publicado.
	//
	// POR QUE NO ALCANZA CON NINGUNA PRUEBA DE ACUERDO. Comparar dos rutas -- el ayudante y la
	// puerta -- no dice nada sobre el catalogo: las dos pasan por el mismo renderizador, asi que una
	// clave que falta se dice "{clave}" por las dos y un hueco con el nombre cambiado se queda
	// "{hueco}" por las dos. Las dos coinciden y las dos estan mal, que es E49 dicho sobre un
	// catalogo. Lo unico que lo ve es afirmar la frase.
	//
	// Y TAMPOCO ALCANZA CON LA PARIDAD DE CLAVES. TheTwoCataloguesOfTheAppCarryTheSameKeys ve una
	// clave que falta en un catalogo; no ve una clave que sobra en los dos, ni dos huecos cruzados
	// adentro de una sola clave, porque un hueco no es una clave.
	//
	// LOS INSTANTES NO SE VUELVEN A DECIR y las FECHAS SI. Un instante con su hora es la forma
	// canonica en que este libro nombra un momento -- de que dia local es necesitaria una zona que
	// no tiene -- y se cita tal cual en los dos idiomas; un DIA del calendario -- la vigencia de un
	// precio -- se dice en palabras, como ya se dice el dia en que se publico un sobre.
	[TestFixture]
	public sealed class TheMoneyIsSaidInTheLanguageItIsAskedIn
	{
		private const string TheVin = "1FTFW1ET5DFC10312";
		private const string TheModel = "YARIS-2019";
		private const string TheRevision = "R1";
		private const string TheTyres = "Llantas nuevas";
		private const string TheFuel = "Gasolina de enero";
		private const string TheImport = "Repuesto importado";

		private static readonly DateTime TheTyresWereBought =
			new DateTime(2026, 1, 1, 0, 0, 0);

		private static readonly DateTime TheFuelWasBought =
			new DateTime(2026, 1, 15, 9, 0, 0);

		private static readonly DateTime TheImportWasBought =
			new DateTime(2026, 1, 20, 10, 0, 0);

		private static readonly DateTime TheStartOfJanuary = new DateTime(2026, 1, 1, 0, 0, 0);
		private static readonly DateTime TheStartOfFebruary = new DateTime(2026, 2, 1, 0, 0, 0);
		private static readonly DateTime TheStartOfMarch = new DateTime(2026, 3, 1, 0, 0, 0);

		private static readonly DateTime ThePriceWasPublished =
			new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheValidityStarts =
			new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheValidityEnds =
			new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime WhileItIsInForce =
			new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc);

		private CostLedger ledger;

		[SetUp]
		public void Setup()
		{
			ledger = new CostLedger(TheVin);

			ledger.RecordDurableCost(TheTyresWereBought, 240000m, "CRC", TheTyres, 4);
			ledger.RecordRecurringCost(TheFuelWasBought, 25000m, "CRC", TheFuel);
		}

		// EL GASTO DE UN PERIODO CON SUS DOS VISTAS Y LA REGLA DE LO DURADERO ADENTRO, letra por
		// letra. Los numeros estan elegidos para que la cuenta sea exacta y se pueda escribir: 240000
		// de llantas con 4 meses de vida desde el 1 de enero reparten 2000 por dia de los 120 de su
		// ventana, asi que los 31 dias de enero le tocan 62000.
		[Test]
		public void TheSpendOfAPeriodIsSaidInTheLanguageOfTheRecord()
		{
			Assert.That(
				TheSentenceTheDoorWouldCompose.TheSpend(
					ledger, TheStartOfJanuary, TheStartOfFebruary, TheLanguage.OfTheRecord),
				Is.EqualTo(
					"Entre el 2026-01-01 00:00:00 y el 2026-02-01 00:00:00: Salieron ₡265.000,00 del "
					+ "banco y el período consumió ₡87.000,00. De eso: ₡240.000,00 en Llantas nuevas, "
					+ "en línea recta a lo largo de 4 meses desde el 2026-01-01 00:00:00: a este "
					+ "período le tocan ₡62.000,00."));
		}

		[Test]
		public void AndTheSameSpendIsSaidInTheOtherLanguageToo()
		{
			Assert.That(
				TheSentenceTheDoorWouldCompose.TheSpend(
					ledger, TheStartOfJanuary, TheStartOfFebruary, TheLanguage.OfAnEnglishReader),
				Is.EqualTo(
					"Between 2026-01-01 00:00:00 and 2026-02-01 00:00:00: ₡265,000.00 left the bank "
					+ "and the period consumed ₡87,000.00. Of that: ₡240,000.00 on Llantas nuevas, in "
					+ "a straight line over 4 months from 2026-01-01 00:00:00: this period's share is "
					+ "₡62,000.00."));
		}

		// EL MISMO PERIODO MONEDA POR MONEDA: otro marco y las mismas cifras. Las dos vistas van
		// adentro de los dos, que es la ley que no puede ceder.
		[Test]
		public void TheSameSpendPerCurrencyIsAnotherFrameAndTheSameFiguresInBothLanguages()
		{
			Assert.That(
				TheSentenceTheDoorWouldCompose.TheSpendPerCurrency(
					ledger, TheStartOfJanuary, TheStartOfFebruary, TheLanguage.OfTheRecord),
				Is.EqualTo(
					"Entre el 2026-01-01 00:00:00 y el 2026-02-01 00:00:00 todo el gasto está dicho "
					+ "en una sola moneda: Salieron ₡265.000,00 del banco y el período consumió "
					+ "₡87.000,00. De eso: ₡240.000,00 en Llantas nuevas, en línea recta a lo largo "
					+ "de 4 meses desde el 2026-01-01 00:00:00: a este período le tocan ₡62.000,00."));

			Assert.That(
				TheSentenceTheDoorWouldCompose.TheSpendPerCurrency(
					ledger, TheStartOfJanuary, TheStartOfFebruary, TheLanguage.OfAnEnglishReader),
				Is.EqualTo(
					"Between 2026-01-01 00:00:00 and 2026-02-01 00:00:00 the whole spend is said in a "
					+ "single currency: ₡265,000.00 left the bank and the period consumed ₡87,000.00. "
					+ "Of that: ₡240,000.00 on Llantas nuevas, in a straight line over 4 months from "
					+ "2026-01-01 00:00:00: this period's share is ₡62,000.00."));
		}

		// DOS MONEDAS SON UN ESTADO CON NOMBRE Y NO UNA AUSENCIA, y el marco lo dice en los dos
		// idiomas con el conteo que el dominio hizo.
		[Test]
		public void TwoCurrenciesAreSaidAsAStateWithItsCountInBothLanguages()
		{
			ledger.RecordRecurringCost(TheImportWasBought, 120.50m, "USD", TheImport);

			Assert.That(
				TheSentenceTheDoorWouldCompose.TheSpendPerCurrency(
					ledger, TheStartOfJanuary, TheStartOfFebruary, TheLanguage.OfTheRecord),
				Does.StartWith(
					"Entre el 2026-01-01 00:00:00 y el 2026-02-01 00:00:00 el gasto está en 2 "
					+ "monedas distintas, así que no hay un total entre ellas y cada una se dice por "
					+ "su lado. "));

			Assert.That(
				TheSentenceTheDoorWouldCompose.TheSpendPerCurrency(
					ledger, TheStartOfJanuary, TheStartOfFebruary, TheLanguage.OfAnEnglishReader),
				Does.StartWith(
					"Between 2026-01-01 00:00:00 and 2026-02-01 00:00:00 the spend is in 2 different "
					+ "currencies, so there is no total across them and each one is said on its own. "));

			Assert.That(
				TheSentenceTheDoorWouldCompose.TheSpendPerCurrency(
					ledger, TheStartOfJanuary, TheStartOfFebruary, TheLanguage.OfAnEnglishReader),
				Does.Contain("$120.50 left the bank"),
				"Y la moneda que no es la del registro se dice con SU signo y los separadores del "
				+ "idioma en que se pregunto: el signo es del catalogo y la coma es del idioma.");
		}

		// DOS PERIODOS COMPARADOS, cada uno con sus limites y sus dos vistas, y ninguna diferencia.
		[Test]
		public void TwoPeriodsComparedNameBothAndCarryNoDifferenceInBothLanguages()
		{
			Assert.That(
				TheSentenceTheDoorWouldCompose.TheTwoPeriodsOfSpendCompared(
					ledger, TheStartOfFebruary, TheStartOfMarch, TheStartOfJanuary,
					TheStartOfFebruary, TheLanguage.OfTheRecord),
				Is.EqualTo(
					"Primer período: Entre el 2026-02-01 00:00:00 y el 2026-03-01 00:00:00: No salió "
					+ "nada del banco en este período y el período consumió ₡56.000,00. De eso: "
					+ "₡240.000,00 en Llantas nuevas, en línea recta a lo largo de 4 meses desde el "
					+ "2026-01-01 00:00:00: a este período le tocan ₡56.000,00. Segundo período: "
					+ "Entre el 2026-01-01 00:00:00 y el 2026-02-01 00:00:00: Salieron ₡265.000,00 "
					+ "del banco y el período consumió ₡87.000,00. De eso: ₡240.000,00 en Llantas "
					+ "nuevas, en línea recta a lo largo de 4 meses desde el 2026-01-01 00:00:00: a "
					+ "este período le tocan ₡62.000,00."));

			Assert.That(
				TheSentenceTheDoorWouldCompose.TheTwoPeriodsOfSpendCompared(
					ledger, TheStartOfFebruary, TheStartOfMarch, TheStartOfJanuary,
					TheStartOfFebruary, TheLanguage.OfAnEnglishReader),
				Does.StartWith(
					"First period: Between 2026-02-01 00:00:00 and 2026-03-01 00:00:00: Nothing left "
					+ "the bank in this period and the period consumed ₡56,000.00."));
		}

		// LA FILA DE CADA ENTRADA, con su clase adentro y con lo que su clase significa.
		[Test]
		public void EveryRowOfTheLedgerIsSaidWholeInBothLanguages()
		{
			string[] inSpanish = TheSentenceTheDoorWouldCompose.EachCostEntry(
				ledger, TheStartOfJanuary, TheStartOfFebruary, TheLanguage.OfTheRecord);

			string[] inEnglish = TheSentenceTheDoorWouldCompose.EachCostEntry(
				ledger, TheStartOfJanuary, TheStartOfFebruary, TheLanguage.OfAnEnglishReader);

			Assert.That(inSpanish[0], Is.EqualTo(
				"₡240.000,00 en Llantas nuevas el 2026-01-01 00:00:00. Duradero: se reparte en línea "
				+ "recta a lo largo de 4 meses desde esa fecha."));

			Assert.That(inEnglish[0], Is.EqualTo(
				"₡240,000.00 on Llantas nuevas on 2026-01-01 00:00:00. Durable: spread in a straight "
				+ "line over 4 months from that date."));

			Assert.That(inSpanish[1], Is.EqualTo(
				"₡25.000,00 en Gasolina de enero el 2026-01-15 09:00:00. Recurrente: se consumió en "
				+ "el período en que se incurrió."));

			Assert.That(inEnglish[1], Is.EqualTo(
				"₡25,000.00 on Gasolina de enero on 2026-01-15 09:00:00. Recurring: consumed in the "
				+ "period it was incurred in."));
		}

		// LA VIDA UTIL Y EL DINERO DE CADA FILA, que son las dos columnas que el registro compone
		// aparte de la frase. La celda vacia de la recurrente dice que no tiene vida util, en los dos.
		[Test]
		public void TheMoneyAndTheLifeColumnsAreSaidInBothLanguagesAndTheAbsentLifeIsEmpty()
		{
			foreach (string language in TheTwoLanguages())
			{
				string[] lives = TheSentenceTheDoorWouldCompose.EachCostEntryExpectedLife(
					ledger, TheStartOfJanuary, TheStartOfFebruary, language);

				Assert.That(lives[1], Is.Empty,
					"Una celda vacia dice que no tiene vida util. Un cero ahi se leeria como una "
					+ "vida de cero meses, que es otro hecho.");
			}

			Assert.That(
				TheSentenceTheDoorWouldCompose.EachCostEntryMoney(
					ledger, TheStartOfJanuary, TheStartOfFebruary, TheLanguage.OfTheRecord),
				Is.EqualTo(new[] { "₡240.000,00", "₡25.000,00" }));

			Assert.That(
				TheSentenceTheDoorWouldCompose.EachCostEntryMoney(
					ledger, TheStartOfJanuary, TheStartOfFebruary, TheLanguage.OfAnEnglishReader),
				Is.EqualTo(new[] { "₡240,000.00", "₡25,000.00" }));

			Assert.That(
				TheSentenceTheDoorWouldCompose.EachCostEntryExpectedLife(
					ledger, TheStartOfJanuary, TheStartOfFebruary, TheLanguage.OfTheRecord)[0],
				Is.EqualTo("4 meses"));

			Assert.That(
				TheSentenceTheDoorWouldCompose.EachCostEntryExpectedLife(
					ledger, TheStartOfJanuary, TheStartOfFebruary, TheLanguage.OfAnEnglishReader)[0],
				Is.EqualTo("4 months"));
		}

		// LO QUE COSTO LA DISTANCIA, con las dos unidades dichas y el ancla nombrada.
		[Test]
		public void TheCostPerDistanceIsSaidWithBothUnitsInBothLanguages()
		{
			Assert.That(
				TheSentenceTheDoorWouldCompose.TheCostPerDistance(
					ledger.TheCostPerDistanceClause(
						TheStartOfJanuary, TheStartOfFebruary, TheStartOfJanuary, 2000,
						DistanceUnitCode.UnitA),
					TheLanguage.OfTheRecord),
				Is.EqualTo(
					"Entre el 2026-01-01 00:00:00 y el 2026-02-01 00:00:00, desde la primera lectura "
					+ "del odómetro del período -- la del 2026-01-01 00:00:00 -- el vehículo recorrió "
					+ "2.000 km: lo que se gastó después de ella sale a ₡12,50 por km. Lo que se pagó "
					+ "en esa lectura o antes pagó el recorrido anterior y no cuenta acá."));

			Assert.That(
				TheSentenceTheDoorWouldCompose.TheCostPerDistance(
					ledger.TheCostPerDistanceClause(
						TheStartOfJanuary, TheStartOfFebruary, TheStartOfJanuary, 2000,
						DistanceUnitCode.UnitA),
					TheLanguage.OfAnEnglishReader),
				Is.EqualTo(
					"Between 2026-01-01 00:00:00 and 2026-02-01 00:00:00, since the period's first "
					+ "odometer reading -- the one on 2026-01-01 00:00:00 -- the vehicle travelled "
					+ "2,000 km: what was spent after it works out at ₡12.50 per km. What was paid at "
					+ "that reading or before paid for the earlier distance and does not count here."));
		}

		// EL MOTIVO DE LA QUE NO TIENE TRAMO SE CITA TAL CUAL, en los dos: lo dijo la serie del
		// odometro, que es de quien es esa ley, y este libro no lo vuelve a escribir. Es la costura
		// que el turno de la serie cierra.
		[Test]
		public void TheReasonTheOdometerSeriesGaveIsQuotedVerbatimInBothLanguages()
		{
			const string whatTheSeriesSaid = "no se puede decir cuánto se maneja";

			foreach (string language in TheTwoLanguages())
				Assert.That(
					TheSentenceTheDoorWouldCompose.TheCostPerDistance(
						ledger.TheCostPerDistanceClauseWithNoSpan(
							TheStartOfJanuary, TheStartOfFebruary, whatTheSeriesSaid),
						language),
					Does.EndWith(whatTheSeriesSaid),
					"El motivo llega YA DICHO y se pasa entero. Volver a componerlo aqui dejaria dos "
					+ "versiones de una misma razon que pueden discrepar sin que nada lo vea.");
		}

		// EL PRECIO PUBLICADO, con su hito, su moneda, su vigencia y donde cae el instante que se
		// pregunto. Los DIAS de la vigencia se dicen en palabras, como el dia en que se publico un
		// sobre; el monto lleva el signo del catalogo y la coma del idioma.
		[Test]
		public void ThePublishedPriceIsSaidWholeInBothLanguages()
		{
			ModelSpecification specification = ASheetWithAPrice();

			Assert.That(
				TheSentenceTheDoorWouldCompose.TheServicePrice(
					specification, OperationKindCode.Inspection, 10000, DistanceUnitCode.UnitA,
					WhileItIsInForce, TheLanguage.OfTheRecord),
				Is.EqualTo(
					"Revisión a los 10.000 km: ₡120,50, vigente del 1 de enero de 2025 al 31 de "
					+ "diciembre de 2025. Al 15 de junio de 2025 esa vigencia está rigiendo. "
					+ "Publicado en la revisión R1."));

			Assert.That(
				TheSentenceTheDoorWouldCompose.TheServicePrice(
					specification, OperationKindCode.Inspection, 10000, DistanceUnitCode.UnitA,
					WhileItIsInForce, TheLanguage.OfAnEnglishReader),
				Is.EqualTo(
					"Inspection at 10,000 km: ₡120.50, valid from 1 January 2025 to 31 December 2025. "
					+ "On 15 June 2025 that validity is in force. Published in revision R1."));

			Assert.That(
				TheSentenceTheDoorWouldCompose.ThePublishedServicePrice(
					specification, OperationKindCode.Inspection, 10000, DistanceUnitCode.UnitA,
					TheLanguage.OfAnEnglishReader),
				Is.EqualTo(
					"Inspection at 10,000 km: ₡120.50, valid from 1 January 2025 to 31 December 2025. "
					+ "Published in revision R1."),
				"Sin instante no se dice si rige, y eso es un MIEMBRO de la vigencia y no un "
				+ "instante en blanco.");
		}

		// LA AUSENCIA NOMBRA EL SUJETO, que es la unica parte del item que el marco de la ausencia
		// dice: con algo publicado el sujeto no aparece en ninguna frase.
		[Test]
		public void AnUnpublishedPriceNamesItsSubjectInBothLanguages()
		{
			ModelSpecification specification = ASheetWithAPrice();

			Assert.That(
				TheSentenceTheDoorWouldCompose.TheServicePrice(
					specification, OperationKindCode.Repair, 10000, DistanceUnitCode.UnitA,
					WhileItIsInForce, TheLanguage.OfTheRecord),
				Does.StartWith("Precio del servicio de reparación en el hito preguntado: no se ha "
					+ "publicado nada"));

			Assert.That(
				TheSentenceTheDoorWouldCompose.TheServicePrice(
					specification, OperationKindCode.Repair, 10000, DistanceUnitCode.UnitA,
					WhileItIsInForce, TheLanguage.OfAnEnglishReader),
				Does.StartWith("Price of the repair service at the milestone asked: nothing has been "
					+ "published"));
		}

		// NINGUNA RESPUESTA DE DINERO SE QUEDA CON UN HUECO SIN LLENAR NI CON UNA CLAVE CRUDA. Es el
		// barrido que hace que las pruebas de arriba no dependan de que a alguien se le ocurriera el
		// caso: Say() devuelve "{clave}" cuando no la tiene y deja "{hueco}" cuando el nombre no
		// calza, y las dos cosas se ven con una llave en la frase final.
		[Test]
		public void NoMoneySentenceComesBackWithAHoleOrARawKeyInIt()
		{
			ledger.RecordRecurringCost(TheImportWasBought, 120.50m, "USD", TheImport);

			foreach (string language in TheTwoLanguages())
				foreach (string said in EveryMoneySentence(language))
					Assert.That(said, Does.Not.Contain("{").And.Not.Contain("}"),
						"Una llave en la frase final es una clave que el catalogo no tiene o un hueco "
						+ "cuyo nombre no calza con el que quien compone le pasa. En " + language
						+ ": " + said);
		}

		private IEnumerable<string> EveryMoneySentence(string language)
		{
			ModelSpecification specification = ASheetWithAPrice();

			List<string> said = new List<string>
			{
				TheSentenceTheDoorWouldCompose.TheSpend(
					ledger, TheStartOfJanuary, TheStartOfFebruary, language),
				TheSentenceTheDoorWouldCompose.TheSpend(
					ledger, TheStartOfMarch, TheStartOfMarch, language),
				TheSentenceTheDoorWouldCompose.TheSpendPerCurrency(
					ledger, TheStartOfJanuary, TheStartOfFebruary, language),
				TheSentenceTheDoorWouldCompose.TheSpendPerCurrency(
					ledger, TheStartOfMarch, TheStartOfMarch, language),
				TheSentenceTheDoorWouldCompose.TheSpendPerCurrency(
					ledger, TheStartOfFebruary, TheStartOfMarch, language),
				TheSentenceTheDoorWouldCompose.TheTwoPeriodsOfSpendCompared(
					ledger, TheStartOfJanuary, TheStartOfFebruary, TheStartOfMarch, TheStartOfMarch,
					language),
				TheSentenceTheDoorWouldCompose.TheCostPerDistance(
					ledger.TheCostPerDistanceClause(
						TheStartOfJanuary, TheStartOfFebruary, TheStartOfJanuary, 2000,
						DistanceUnitCode.UnitA),
					language),
				TheSentenceTheDoorWouldCompose.TheCostPerDistance(
					ledger.TheCostPerDistanceClause(
						TheStartOfJanuary, TheStartOfFebruary, TheStartOfJanuary, 0,
						DistanceUnitCode.UnitA),
					language),
				TheSentenceTheDoorWouldCompose.TheCostPerDistance(
					ledger.TheCostPerDistanceClause(
						TheStartOfMarch, TheStartOfMarch, TheStartOfMarch, 2000,
						DistanceUnitCode.UnitA),
					language),
				TheSentenceTheDoorWouldCompose.TheCostPerDistance(
					ledger.TheCostPerDistanceClauseWithNoSpan(
						TheStartOfJanuary, TheStartOfFebruary, "lo que la serie dijo"),
					language),
				TheSentenceTheDoorWouldCompose.TheServicePrice(
					specification, OperationKindCode.Inspection, 10000, DistanceUnitCode.UnitA,
					WhileItIsInForce, language),
				TheSentenceTheDoorWouldCompose.TheServicePrice(
					specification, OperationKindCode.Inspection, 10000, DistanceUnitCode.UnitA,
					TheValidityStarts.AddYears(-1), language),
				TheSentenceTheDoorWouldCompose.TheServicePrice(
					specification, OperationKindCode.Inspection, 10000, DistanceUnitCode.UnitA,
					TheValidityEnds.AddYears(1), language),
				TheSentenceTheDoorWouldCompose.ThePublishedServicePrice(
					specification, OperationKindCode.Inspection, 10000, DistanceUnitCode.UnitA,
					language),
				TheSentenceTheDoorWouldCompose.TheServicePrice(
					specification, OperationKindCode.Repair, 10000, DistanceUnitCode.UnitA,
					WhileItIsInForce, language),
			};

			said.AddRange(TheSentenceTheDoorWouldCompose.EachCostEntry(
				ledger, TheStartOfJanuary, TheStartOfFebruary, language));

			said.AddRange(TheSentenceTheDoorWouldCompose.EachCostEntryMoney(
				ledger, TheStartOfJanuary, TheStartOfFebruary, language));

			said.AddRange(TheSentenceTheDoorWouldCompose.EachPublishedPrice(
				specification, WhileItIsInForce, language));

			said.AddRange(TheSentenceTheDoorWouldCompose.EachPublishedMilestone(
				specification, WhileItIsInForce, language));

			return said;
		}

		private static ModelSpecification ASheetWithAPrice()
		{
			ModelSpecification specification = new ModelSpecification(TheModel);

			specification.OpenRevision(TheRevision, ThePriceWasPublished);
			specification.PublishServicePrice(
				TheRevision, OperationKindCode.Inspection, 10000, DistanceUnitCode.UnitA, 120.50m,
				"CRC", TheValidityStarts, TheValidityEnds);
			specification.SealRevision(TheRevision);

			return specification;
		}

		private static string[] TheTwoLanguages()
		{
			return new[] { TheLanguage.OfTheRecord, TheLanguage.OfAnEnglishReader };
		}
	}
}
