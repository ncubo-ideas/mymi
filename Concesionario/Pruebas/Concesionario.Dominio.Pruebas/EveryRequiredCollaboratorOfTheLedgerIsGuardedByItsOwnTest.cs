using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// CL-2 §12 ITEM 4 MIDIO EL PROBLEMA Y ESTE ARCHIVO ES LA RESPUESTA. Tres guardas de este modulo
	// no ponen rojo NINGUN test cuando se borran -- la de codigo en blanco de Currency.OfPublished y
	// las dos de colaborador de ServicePriceEntry -- porque ningun estado alcanzable las produce: el
	// colaborador se arma una linea antes. Son afirmaciones estructurales que nada puede ver, y
	// MS-3 §6 lo encontro primero en APublishedPriceRow.
	//
	// LA RESPUESTA CADA VEZ ES UN TEST DIRECTO, y esta cuesta un archivo. Este slice escribe varias
	// guardas de colaborador requerido nuevas, asi que cada una entra aca con su propio test y con lo
	// que su mensaje tiene que DECIR -- no solo con que algo revento. E2/E32: un colaborador
	// requerido ausente es un defecto de quien llama, y el rechazo es DomainException en las dos
	// ramas (C22/C36/C37), porque una guarda que tira otra cosa se escapa de Apply y para el bloque de
	// refresco entero.
	//
	// Y LAS GUARDAS DE VALOR NO ESTAN ACA: viven en el tipo que tiene el valor, que es DEC-90. El
	// monto negativo es de Money, la vida no positiva es de AnExpectedLife (y su test esta en el
	// archivo de L5), y el codigo de moneda en blanco es de Currency.
	[TestFixture]
	public sealed class EveryRequiredCollaboratorOfTheLedgerIsGuardedByItsOwnTest
	{
		private const string TheVin = "1FTFW1ET5DFC10312";
		private const string TheSubject = "Llantas nuevas";

		private static readonly DateTime Incurred = new DateTime(2026, 1, 1, 0, 0, 0);
		private static readonly DateTime TheStart = new DateTime(2026, 1, 1, 0, 0, 0);
		private static readonly DateTime TheEnd = new DateTime(2026, 12, 31, 0, 0, 0);

		// EL LIBRO ES UNO POR VIN, asi que una identidad en blanco no dice de que vehiculo es.
		[TestCase(null)]
		[TestCase("")]
		[TestCase("   ")]
		public void ACostLedgerWithoutAVinHasNoVehicleToBeAbout(string vin)
		{
			Assert.Throws<DomainException>(() => new CostLedger(vin));
		}

		// EL ASUNTO ES TEXTO RECIBIDO Y NO TIENE VALOR POR DEFECTO: en que se fue la plata lo sabe
		// quien la gasto, y "Gasto" puesto por el modulo seria la primera invencion prohibida.
		[TestCase(null)]
		[TestCase("")]
		[TestCase("   ")]
		public void ACostSubjectThatSaysNothingIsRefused(string text)
		{
			DomainException refused = Assert.Throws<DomainException>(() => new CostSubject(text));

			Assert.That(refused.Message, Does.Contain("gast"),
				"El rechazo dice de que hecho esta hablando y no solo que algo esta en blanco.");
		}

		// Y EL COMANDO LO HEREDA, por los dos caminos.
		[Test]
		public void AndNeitherCommandAcceptsAnEntryWithNoSubject()
		{
			CostLedger ledger = new CostLedger(TheVin);

			Assert.Throws<DomainException>(
				() => ledger.RecordRecurringCost(Incurred, 25000m, "CRC", "  "));
			Assert.Throws<DomainException>(
				() => ledger.RecordDurableCost(Incurred, 240000m, "CRC", "  ", 48));

			Assert.That(ledger.HasAnyCostBetween(TheStart, TheEnd), Is.False,
				"Y nada se escribio: E12 -- validar y armar antes de mutar.");
		}

		// LA GUARDA DEL DINERO EN LA ENTRADA, y lo que se mide es que su mensaje NOMBRA EL ASUNTO. Es
		// el precedente de DEC-90 exacto: el rechazo del valor es de Money y nombra el monto; el de la
		// entrada nombra en que se estaba gastando, que es lo unico con lo que una persona la
		// reconoce entre diez.
		[TestCase(typeof(ARecurringCost))]
		[TestCase(typeof(ADurableCost))]
		public void AnEntryWithNoMoneyAtAllIsRefusedAndTheRefusalNamesTheSubject(Type entry)
		{
			DomainException refused = Assert.Throws<DomainException>(
				() => TheEntryOf(entry, null, new CostSubject(TheSubject)));

			Assert.That(refused.Message, Does.Contain(TheSubject),
				entry.Name + " se dejo construir sin dinero, o su rechazo no dice en que se estaba "
				+ "gastando.");
		}

		[TestCase(typeof(ARecurringCost))]
		[TestCase(typeof(ADurableCost))]
		public void AndAnEntryWithNoSubjectIsRefusedToo(Type entry)
		{
			DomainException refused = Assert.Throws<DomainException>(
				() => TheEntryOf(entry, new Money(25000m, Currency.CRC), null));

			Assert.That(refused.Message, Does.Contain("asunto"),
				entry.Name + " se dejo construir sin asunto, o su rechazo no dice que es lo que "
				+ "falta.");
		}

		// EL COMPOSITOR DEL PERIODO: ninguna entrada y una lista ausente no son lo mismo. Es la misma
		// guarda que StaysInAPeriod.Of lleva, por la misma razon.
		[Test]
		public void ThePeriodComposerRefusesAnAbsentListAndNotAnEmptyOne()
		{
			Assert.Throws<DomainException>(() => SpendInAPeriod.Of(TheStart, TheEnd, null));

			Assert.That(
				SpendInAPeriod.Of(TheStart, TheEnd, new List<ACostEntry>()).HasAnyCost(),
				Is.False,
				"Una lista vacia SI es una respuesta: es la ausencia con nombre de L8.");
		}

		// EL RESUMEN POR MONEDA NECESITA AL MENOS UNA ENTRADA: un resumen de ninguna entrada seria un
		// par de ceros, y un cero de dinero se lee como un gasto (L8).
		[Test]
		public void ThePerCurrencySummaryRefusesToBeBuiltFromNoEntryAtAll()
		{
			Assert.Throws<DomainException>(
				() => ASpendSummary.Of(TheStart, TheEnd, null));
			Assert.Throws<DomainException>(
				() => ASpendSummary.Of(TheStart, TheEnd, new List<ACostEntry>()));
		}

		// LAS DOS SITUACIONES CON GASTO NECESITAN SUS RESUMENES, cada una con su propia guarda: una sin
		// resumen no tendria nada que decir, y el estado de varias monedas con un solo resumen seria un
		// periodo de una sola moneda con el nombre equivocado.
		[Test]
		public void APeriodWithCostRefusesToBeBuiltWithoutTheSummariesItSays()
		{
			List<ACostEntry> one = new List<ACostEntry>
			{
				new ARecurringCost(new Money(25000m, Currency.CRC), Incurred, new CostSubject(TheSubject)),
			};

			Assert.Throws<DomainException>(
				() => new ASpendSaidInOneCurrency(TheStart, TheEnd, one, null));

			Assert.Throws<DomainException>(
				() => new ASpendNotSaidAcrossCurrencies(TheStart, TheEnd, one, null));

			Assert.Throws<DomainException>(
				() => new ASpendNotSaidAcrossCurrencies(
					TheStart, TheEnd, one,
					new List<ASpendSummary> { ASpendSummary.Of(TheStart, TheEnd, one) }),
				"Un solo resumen no es el estado de varias monedas: con una sola el periodo se dice "
				+ "entero, y nombrarlo asi diria que no hay un total que si hay.");
		}

		// PREGUNTAR SI DOS GASTOS COMPARTEN MONEDA SIN EL SEGUNDO: es la guarda con la que se agrupa
		// recorriendo la lista, y sin ella el agrupamiento reventaria con un NullReference en vez de
		// decir lo que falta.
		[Test]
		public void AskingWhetherTwoEntriesShareACurrencyNeedsTheSecondOne()
		{
			ACostEntry entry = new ARecurringCost(
				new Money(25000m, Currency.CRC), Incurred, new CostSubject(TheSubject));

			DomainException refused = Assert.Throws<DomainException>(
				() => entry.IsInTheSameCurrencyAs(null));

			Assert.That(refused.Message, Does.Contain("segundo"));
		}

		// UNA VENTANA QUE NO CABE EN EL TIEMPO QUE SE PUEDE DECIR: AddMonths no es total, y sin esta
		// guarda un gasto duradero incurrido al final del tiempo representable tiraria una
		// ArgumentOutOfRangeException -- que no es DomainException y se escapa de Apply.
		[Test]
		public void ADurableCostWhoseWindowDoesNotFitInTimeIsRefusedByTheDomainAndNotByTheRuntime()
		{
			AnExpectedLife life = new AnExpectedLife(48);
			DateTime tooLate = DateTime.MaxValue.AddMonths(-1);

			Assert.That(life.FitsAWindowFrom(tooLate), Is.False,
				"La pregunta total que hay que hacer antes.");

			Assert.That(
				Assert.Catch(() => new ADurableCost(
					new Money(240000m, Currency.CRC), tooLate, new CostSubject(TheSubject), life)),
				Is.InstanceOf<DomainException>()
					.And.Not.InstanceOf<ArgumentOutOfRangeException>(),
				"La entrada se rechaza al ARMARSE, no al leerse: un rechazo en la lectura dejaria "
				+ "toda pregunta futura sobre ese periodo reventando para siempre.");

			Assert.That(
				Assert.Catch(() => life.TicksItCoversBetween(tooLate, TheStart, TheEnd)),
				Is.InstanceOf<DomainException>()
					.And.Not.InstanceOf<ArgumentOutOfRangeException>(),
				"Y la ventana misma tambien lo dice, porque es la que sabe cuanto dura.");
		}

		// EL DINERO CONSUMIDO DE UNA RECURRENTE EN UN PERIODO QUE NO LA CONTIENE: accesor guardado, y
		// un cero ahi se leeria como un gasto que hubo.
		[Test]
		public void TheMoneyARecurringEntryConsumedIsGuardedByThePeriodThatHoldsIt()
		{
			ACostEntry entry = new ARecurringCost(
				new Money(25000m, Currency.CRC), Incurred, new CostSubject(TheSubject));

			Assert.That(entry.ConsumesAnythingBetween(TheStart, TheEnd), Is.True);

			DomainException refused = Assert.Throws<DomainException>(
				() => entry.TheMoneyItConsumedBetween(
					new DateTime(2027, 1, 1, 0, 0, 0), new DateTime(2027, 12, 31, 0, 0, 0)));

			Assert.That(refused.Message, Does.Contain(TheSubject),
				"El rechazo nombra el gasto del que se pregunto.");
		}

		// LOS TRES COLABORADORES DEL COSTO POR DISTANCIA. Los tres llegan de OTRO agregado adentro del
		// mismo script, asi que ningun estado alcanzable los produce ausentes: son afirmaciones
		// estructurales y sin este test nada las veria (es el problema que CL-2 §12 item 4 midio).
		[Test]
		public void ACostPerDistanceWithoutTheUnitOfItsDistanceHasNothingToDivideBy()
		{
			DomainException refused = Assert.Throws<DomainException>(
				() => ACostPerDistance.Of(
					TheStart, TheEnd, TheStart, 3200, null, new List<ACostEntry>()));

			Assert.That(refused.Message, Does.Contain("unidad"),
				"El rechazo dice que falta la unidad y no solo que algo llego nulo.");
		}

		[Test]
		public void AndACostPerDistanceWithNoListOfEntriesIsNotAPeriodWithoutSpend()
		{
			DomainException refused = Assert.Throws<DomainException>(
				() => ACostPerDistance.Of(
					TheStart, TheEnd, TheStart, 3200, DistanceUnit.UnitA, null));

			Assert.That(refused.Message, Does.Contain("lista ausente"),
				"Ninguna entrada y una lista ausente no son lo mismo, y el rechazo lo dice.");
		}

		// Y UNA DISTANCIA NEGATIVA ES UN HECHO IMPOSIBLE Y NO UN TRAMO CORTO: el odometro no va para
		// atras. Un cero SI es legal -- el vehiculo no se movio -- y lo dice la frase.
		[Test]
		public void AndADistanceThatWentBackwardsIsAnImpossibleFactAndNotAShortSpan()
		{
			DomainException refused = Assert.Throws<DomainException>(
				() => ACostPerDistance.Of(
					TheStart, TheEnd, TheStart, -1, DistanceUnit.UnitA, new List<ACostEntry>()));

			Assert.That(refused.Message, Does.Contain("no va para atras"));

			Assert.That(
				TheSentenceTheDoorWouldCompose.TheCostPerDistance(
					ACostPerDistance.Of(
						TheStart, TheEnd, TheStart, 0, DistanceUnit.UnitA, new List<ACostEntry>())
						.TheClauseItComposes(),
					TheLanguage.OfTheRecord),
				Is.Not.Empty,
				"Y cero no se rechaza: es un tramo en el que el vehiculo no se movio, que es un hecho.");
		}

		// LA VARIANTE SIN TRAMO YA NO RECIBE NINGUN MOTIVO, asi que no hay colaborador que guardar.
		// Hasta el turno de la serie el motivo cruzaba por aqui ya redactado en espanol y una guarda
		// exigia que viniera; desde que el ritmo compone su propia clausula, el motivo no lo lleva
		// este libro y quien redacta pone una frase dentro de la otra. La ley que reemplaza a esa
		// guarda es por VALOR y en los dos idiomas: TheReasonForANoSpanPeriodIsTheOdometerSeriesOwn
		// SentenceInBothLanguages, en TheRateOfUseIsSaidInTheLanguageItIsAskedIn.
		[Test]
		public void ACostPerDistanceWithNoSpanCarriesNoReasonOfItsOwn()
		{
			Assert.That(
				typeof(ACostPerDistance).GetMethod(
					"WithNoSpanOfUse",
					BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
					.GetParameters().Length,
				Is.EqualTo(2),
				"Un tercer parametro aqui es el motivo volviendo a cruzar como texto ya redactado, "
				+ "que es justo lo que deja dos versiones de una razon que no es de este libro.");

			Assert.That(
				TheSentenceTheDoorWouldCompose.TheCostPerDistance(
					ACostPerDistance.WithNoSpanOfUse(TheStart, TheEnd).TheClauseItComposes(),
					TheLanguage.OfTheRecord),
				Is.Not.Empty);
		}

		// Y LAS NUEVE SON DEL DOMINIO Y NINGUNA ES UNA ArgumentException, censado de una vez sobre los
		// nueve sitios en vez de repetir el assert en cada test.
		[Test]
		public void AndEveryOneOfTheseRefusalsIsTheDomainsOwn()
		{
			List<TestDelegate> theNineWays = new List<TestDelegate>
			{
				() => new CostLedger("  "),
				() => new CostSubject("  "),
				() => new ARecurringCost(null, Incurred, new CostSubject(TheSubject)),
				() => new ARecurringCost(new Money(1m, Currency.CRC), Incurred, null),
				() => SpendInAPeriod.Of(TheStart, TheEnd, null),
				() => ASpendSummary.Of(TheStart, TheEnd, new List<ACostEntry>()),
				() => ACostPerDistance.Of(
					TheStart, TheEnd, TheStart, 3200, null, new List<ACostEntry>()),
				() => ACostPerDistance.Of(
					TheStart, TheEnd, TheStart, 3200, DistanceUnit.UnitA, null),
				() => ACostPerDistance.Of(
					TheStart, TheEnd, TheStart, -1, DistanceUnit.UnitA, new List<ACostEntry>()),
			};

			foreach (TestDelegate way in theNineWays)
				Assert.That(Assert.Catch(way),
					Is.InstanceOf<DomainException>().And.Not.InstanceOf<ArgumentException>(),
					"C22/C36/C37: una guarda que tira algo que no es DomainException se escapa de "
					+ "Apply y para el bloque de refresco entero -- estrictamente peor que una "
					+ "entrada cicatrizada por un hecho recibido.");
		}

		private static ACostEntry TheEntryOf(Type entry, Money money, CostSubject subject)
		{
			if (entry == typeof(ARecurringCost)) return new ARecurringCost(money, Incurred, subject);

			return new ADurableCost(money, Incurred, subject, new AnExpectedLife(48));
		}
	}
}
