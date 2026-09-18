using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Concesionario.App;
using Concesionario.App.Events;
using NUnit.Framework;
using Puppets.Portador;

namespace Concesionario.Dominio.Pruebas
{
	// §9 DEL BRIEF DICE QUE TODO SE RECIBE POR DOS CAMINOS QUE TIENEN QUE PONERSE DE ACUERDO -- por la
	// puerta, cuando una sucursal registra el gasto, y por el feed, cuando el concesionario lo publica
	// -- y que los dos aterrizan en el MISMO comando del dominio. Hasta CL-7 nada lo fijaba para el
	// libro: CL-4 escribio los comandos, CL-5 las lecturas, y ninguno de los dos tuvo el otro camino
	// con que compararlas.
	//
	// LO QUE SE COMPARA SON LAS SIETE LECTURAS, no un contador. Dos caminos que aplican el comando
	// equivocado de los dos dejan el mismo numero de entradas y el mismo punto de control; lo unico que
	// los separa es lo que el libro contesta despues -- la clase y la vida util.
	//
	// Y SE COMPARA EN DOS ESTADOS (C61). Con una sola entrada las siete contestan, pero un periodo de
	// una sola moneda no puede mostrar la ley L3 y una sola recurrente no puede mostrar las dos vistas
	// de L6 difiriendo; el estado que hace que la comparacion mida algo es el segundo -- una duradera y
	// dos recurrentes en DOS monedas.
	//
	// LO QUE ESTA COMPARACION NO PUEDE VER ESTA ESCRITO ABAJO COMO PRUEBA y no como comentario: los dos
	// caminos corren los MISMOS scripts y normalizan por los MISMOS DOS miembros, asi que un defecto que
	// este en los dos lados se pone de acuerdo consigo mismo. El ancla contra los hechos RECIBIDOS es lo
	// que le falta a una comparacion asi, y esta abajo tambien.
	[TestFixture]
	public sealed class TheSameCostThroughTheFeedAndThroughTheDoorAnswersTheSame
	{
		// LOS EXTREMOS SON INSTANTES Y LOS DOS SON INCLUSIVOS (G4): los del libro no son dias de
		// calendario, que es lo que separa APeriodOfInstants de ADayRange.
		private static readonly DateTime TheYearFrom =
			new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheYearTo =
			new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc);

		private static readonly DateTime TheColonesHalfFrom =
			new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheColonesHalfTo =
			new DateTime(2026, 6, 30, 23, 59, 59, DateTimeKind.Utc);

		private static readonly DateTime TheYearBeforeFrom =
			new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheYearBeforeTo =
			new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc);

		[Test]
		public void TheSevenReadsAnswerTheSameWhicheverRouteTheCostsArrivedBy()
		{
			using (AScheduleOnARealJournal byTheFeed = AJournal())
			using (AScheduleOnARealJournal byTheDoor = AJournal())
			{
				TheDurableCostByTheFeed(byTheFeed);
				TheDurableCostByTheDoor(byTheDoor);

				TheSevenAgree(byTheFeed, byTheDoor, "con una sola entrada duradera en el libro");

				TheRestByTheFeed(byTheFeed);
				TheRestByTheDoor(byTheDoor);

				TheSevenAgree(byTheFeed, byTheDoor, "con una duradera y dos recurrentes en dos monedas");
			}
		}

		// Y SIGUEN DE ACUERDO DESPUES DE REABRIR LOS DOS JOURNALS. Es lo que el segundo programa no
		// muestra (C53): el journal normaliza tambien, asi que una normalizacion del borde solo se ve
		// comparando una respuesta VIVA contra una REPRODUCIDA -- y aca, ademas, las dos reproducidas
		// entre si, que es donde una diferencia de camino sobreviviria al replay.
		[Test]
		public void AndTheyStillAgreeAfterBothJournalsAreClosedAndReopened()
		{
			using (AScheduleOnARealJournal byTheFeed = AJournal())
			using (AScheduleOnARealJournal byTheDoor = AJournal())
			{
				TheDurableCostByTheFeed(byTheFeed);
				TheRestByTheFeed(byTheFeed);

				TheDurableCostByTheDoor(byTheDoor);
				TheRestByTheDoor(byTheDoor);

				Dictionary<string, string> liveByTheFeed = TheSevenAnswersOf(byTheFeed);

				byTheFeed.CloseAndReopenFromTheJournal();
				byTheDoor.CloseAndReopenFromTheJournal();

				foreach (KeyValuePair<string, string> each in TheSevenAnswersOf(byTheFeed))
					Assert.That(liveByTheFeed[each.Key], Is.EqualTo(each.Value),
						"'" + each.Key + "' contesto distinto despues de reabrir el journal que vivio "
						+ "por el feed.");

				TheSevenAgree(byTheFeed, byTheDoor, "despues de reabrir los dos journals");
			}
		}

		// EL ANCLA, y sin ella todo lo de arriba compara DOS CONSUMIDORES DE UN MISMO RESOLVEDOR (E49):
		// los dos caminos pasan por el mismo script y el mismo agregado, asi que se ponen de acuerdo por
		// construccion, incluido el caso en que los dos estan igual de equivocados. Algo tiene que atar
		// un lado a los hechos que LLEGARON, y son literales escritos aca.
		[Test]
		public void AndBothRoutesAnswerWithTheAmountsAndTheLivesTheBranchActuallySent()
		{
			using (AScheduleOnARealJournal byTheFeed = AJournal())
			using (AScheduleOnARealJournal byTheDoor = AJournal())
			{
				TheDurableCostByTheFeed(byTheFeed);
				TheRestByTheFeed(byTheFeed);

				TheDurableCostByTheDoor(byTheDoor);
				TheRestByTheDoor(byTheDoor);

				string[] theMoneyThatWasSent = { "₡240.000,00", "₡45.000,00", "$120,50" };
				string[] theLivesThatWereSent = { "48 meses", "", "" };
				string[] theClassesThatWereSent = { "Durable", "Recurring", "Recurring" };

				Assert.That(
					byTheFeed.Api.CostEntriesBetween(TheYearFrom, TheYearTo, TheLanguage.OfTheRecord).Money,
					Is.EqualTo(theMoneyThatWasSent),
					"Por el feed, los tres montos que la sucursal dijo y no otros tres -- cada uno con "
					+ "el signo de SU moneda.");

				Assert.That(
					byTheDoor.Api.CostEntriesBetween(TheYearFrom, TheYearTo, TheLanguage.OfTheRecord).Money,
					Is.EqualTo(theMoneyThatWasSent),
					"Y por la puerta, los mismos tres.");

				Assert.That(
					byTheFeed.Api.CostEntriesBetween(TheYearFrom, TheYearTo, TheLanguage.OfTheRecord).ExpectedLife,
					Is.EqualTo(theLivesThatWereSent),
					"Y la vida util donde la sucursal la mando, que es la unica columna que separa los "
					+ "dos comandos.");

				Assert.That(
					byTheFeed.Api.CostEntriesBetween(TheYearFrom, TheYearTo, TheLanguage.OfTheRecord).Classification,
					Is.EqualTo(theClassesThatWereSent),
					"Y la clase, que ninguna de las dos senales trae: la puso CUAL de los dos comandos "
					+ "aplico la ingesta.");
			}
		}

		// LA MITAD DE C53 QUE CL-5 NO PUDO PONER ROJA, VUELTA A CORRER SOBRE EL CAMINO QUE LA ABRIA.
		//
		// CL-5 escribio TheAmountsSayTheSameLiveAndReplayedAndTheTwoReasonsThatMakeThatTrueAreAssertedHere
		// contra la PUERTA y dijo que no podia ponerse roja por dos razones, y que el dia que CL-7
		// abriera el feed ese renglon era el unico instrumento que lo veria. CL-7 lo abrio, y la
		// respuesta es que TAMPOCO se puede -- pero por TRES razones y no dos, y la tercera es de este
		// camino.
		//
		// LAS TRES, afirmadas aca al lado en vez de creerse:
		//
		//   Money.Expressed() redondea a las unidades menores de la moneda, asi que para una moneda que
		//   el conjunto NOMBRA la escala que el journal deja caer no se ve.
		//
		//   El reconocedor del borde -- L14 -- descarta todo codigo que el conjunto no nombre, asi que
		//   por el feed no entra ninguna moneda sin nombrar, que es la unica para la que Expressed() no
		//   tendria precision a la que volver.
		//
		//   Y el Normalized de la puerta le da al decimal la forma del journal ANTES de que el journal
		//   la vea, en los DOS scripts de ingesta.
		//
		// LAS TRES JUNTAS CIERRAN EL CAMINO, asi que esto se dice como LIMITE y no se tapa con un verde
		// que parezca cobertura (C56). EL UNICO CAMINO QUE QUEDA es un journal que una app anterior ya
		// escribio, donde Currency.OfPublished sigue siendo total por DEC-96 -- y quien lo mide es
		// ACurrencyAlreadyWrittenIntoAJournalIsNeverRefusedOnReopening, no esto.
		[Test]
		public void TheFeedsAmountSaysTheSameLiveAndReplayedAndTheThreeThingsThatMakeThatTrueAreAssertedHere()
		{
			using (AScheduleOnARealJournal byTheFeed = AJournal())
			{
				// UNA ESCALA QUE EL JOURNAL DEJA CAER: 45000.00 mandado, 45000 guardado.
				Apply(byTheFeed, ADealershipNarrative.RecurringCost(
					34, ADealershipNarrative.TheOilChangeIncurred,
					ADealershipNarrative.TheOilChangeAmount,
					ADealershipNarrative.TheOilChangeCurrency,
					ADealershipNarrative.TheOilChangeSubject));

				string live = byTheFeed.Api.CostEntriesBetween(TheYearFrom, TheYearTo, TheLanguage.OfTheRecord).Money[0];

				byTheFeed.CloseAndReopenFromTheJournal();

				Assert.That(
					byTheFeed.Api.CostEntriesBetween(TheYearFrom, TheYearTo, TheLanguage.OfTheRecord).Money[0],
					Is.EqualTo(live),
					"El monto se dijo distinto vivo y reproducido. Por este camino eso no puede pasar; el "
					+ "dia que pase, este es el renglon que lo dice.");

				Assert.That(live, Is.EqualTo("₡45.000,00"),
					"PRIMERA RAZON: el dominio DICE el monto en las unidades menores de su moneda, asi "
					+ "que la escala que el journal deja caer se vuelve a poner al decirlo.");
			}

			Assert.That(
				DealershipEventRoute.Route(ADealershipNarrative.RecurringCost(
					34, ADealershipNarrative.TheOilChangeIncurred, "45000.00", "XYZ",
					ADealershipNarrative.TheOilChangeSubject)),
				Is.Null,
				"SEGUNDA RAZON: por este camino no entra una moneda que el conjunto no nombre. La senal "
				+ "se descarta en el borde, asi que no llega a haber comando -- y es la unica moneda "
				+ "para la que Expressed() no tendria precision a la que volver.");

			Assert.That(TheMembersOfTheDoorNamed("Normalized"), Has.Count.EqualTo(1),
				"TERCERA RAZON: la puerta tiene UN solo Normalized, y los dos scripts de ingesta del "
				+ "libro lo llaman -- que es lo que afirma "
				+ "TheAmountOfACostIsGivenTheJournalsShapeBeforeItCrosses sobre el fuente de cada uno, "
				+ "porque una atadura cruda no la ve ninguna respuesta de este camino. Aca se afirma que "
				+ "hay UNO: un segundo seria un segundo autor de la misma decision. MIENTRAS LAS TRES "
				+ "SIGAN AHI, ESTE RENGLON NO SE PUEDE PONER ROJO por un monto -- y eso es lo que dice, "
				+ "en vez de un verde que parezca cobertura.");
		}

		// Y EL InUtc DEL FEED SI SE VE POR VALOR, que es la mitad que CL-6 §11 dejo nombrada: dijo que su
		// instrumento seguia siendo el que veria un instante recibido llegando SIN pasar por InUtc, y que
		// ese camino era el que CL-7 abria. Aca esta.
		//
		// LO QUE HACE QUE SE VEA ES EL MOMENTO EN QUE SE TOMA LA COMPARACION (C61), y no el instante
		// suelto: un gasto incurrido en 23:59:59.500 contra un extremo de 23:59:59 esta AFUERA crudo y
		// ADENTRO recortado. El journal recorta igual, asi que crudo la respuesta viva y la reproducida
		// DISCREPAN -- y esa discrepancia es todo lo que este renglon mide. Con el instante ya en
		// segundos enteros, como los de la narrativa, no habria nada que ver.
		[Test]
		public void ACostIncurredInTheLastFractionOfAPeriodIsInsideItLiveAndReplayedByTheFeedToo()
		{
			using (AScheduleOnARealJournal byTheFeed = AJournal())
			{
				Apply(byTheFeed, ADealershipNarrative.RecurringCost(
					34, "2026-06-30T23:59:59.500Z", ADealershipNarrative.TheOilChangeAmount,
					ADealershipNarrative.TheOilChangeCurrency,
					ADealershipNarrative.TheOilChangeSubject));

				int live = byTheFeed.Api.CostEntryCountBetween(TheColonesHalfFrom, TheColonesHalfTo);

				Assert.That(live, Is.EqualTo(1),
					"El extremo del periodo es 23:59:59 y el gasto llego en 23:59:59.500. Recortado al "
					+ "segundo entero cae ADENTRO; crudo cae afuera y la cuenta seria cero.");

				byTheFeed.CloseAndReopenFromTheJournal();

				Assert.That(
					byTheFeed.Api.CostEntryCountBetween(TheColonesHalfFrom, TheColonesHalfTo),
					Is.EqualTo(live),
					"Y contesta lo mismo despues de reabrir: el journal recorta tambien, asi que un "
					+ "instante que cruzara crudo diria una cosa vivo y otra reproducido.");

				Assert.That(
					byTheFeed.Api.CostEntriesBetween(TheColonesHalfFrom, TheColonesHalfTo, TheLanguage.OfTheRecord).IncurredAt,
					Is.EqualTo(new[] { "2026-06-30 23:59:59" }),
					"Y la fila lo dice en segundos enteros, que es la forma en que quedo escrito.");
			}
		}

		// LO QUE ESTA COMPARACION NO PUEDE VER, dicho como prueba (C56). Los dos caminos normalizan por
		// UN SOLO miembro cada cosa -- el instante y el decimal -- y esa es exactamente la razon por la
		// que un defecto adentro de cualquiera de los dos deja los dos lados de acuerdo. El dia que la
		// ingesta normalice por su cuenta, esta prueba se pone roja -- y ese es el dia en que la
		// comparacion de arriba empieza a medir la normalizacion en vez de heredarla.
		//
		// SON DOS Y NO UNO, que es la diferencia con el libro de estadias: ahi lo unico compartido es el
		// instante, y aca el monto es un decimal que el journal cambia de forma.
		[Test]
		public void TheComparisonCannotSeeTheTwoNormalizationsBothRoutesShare()
		{
			Assert.That(TheMembersOfTheDoorNamed("InUtc"), Has.Count.EqualTo(1),
				"La puerta tiene mas de un miembro llamado InUtc. Con uno solo, los dos caminos comparten "
				+ "la normalizacion del instante y compararlos entre si no dice nada sobre ella; con dos, "
				+ "cada camino tiene la suya y la comparacion pasa a medirla.");

			Assert.That(TheMembersOfTheDoorNamed("Normalized"), Has.Count.EqualTo(1),
				"Y lo mismo con el decimal, que es lo que el libro agrega a esta forma: el journal le "
				+ "borra la escala, asi que un segundo Normalized seria una segunda decision sobre la "
				+ "misma cosa.");
		}

		// ------------------------------------------------------ el aparejo

		private static AScheduleOnARealJournal AJournal()
		{
			return new AScheduleOnARealJournal(ADealershipNarrative.TheVin, "Standard");
		}

		private static List<string> TheMembersOfTheDoorNamed(string name)
		{
			List<string> found = new List<string>();

			foreach (MethodInfo each in typeof(DealershipCarAPI).GetMethods(
				BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance
				| BindingFlags.DeclaredOnly))
				if (each.Name == name) found.Add(each.Name);

			return found;
		}

		// LOS MISMOS HECHOS, dichos una sola vez: los dos caminos leen las mismas constantes de la
		// narrativa, y el lado de la puerta PARSEA las cadenas de la narrativa en vez de volver a
		// escribirlas. Escribirlos dos veces dejaria que una tercera cosa -- un monto mal copiado -- los
		// separara sin que la comparacion supiera de que se trata.
		private static void TheDurableCostByTheFeed(AScheduleOnARealJournal journal)
		{
			Apply(journal, ADealershipNarrative.DurableCost(
				34, ADealershipNarrative.TheTyresIncurred, ADealershipNarrative.TheTyresAmount,
				ADealershipNarrative.TheTyresCurrency, ADealershipNarrative.TheTyresSubject,
				ADealershipNarrative.TheTyresLifeInMonths));
		}

		private static void TheRestByTheFeed(AScheduleOnARealJournal journal)
		{
			Apply(journal, ADealershipNarrative.RecurringCost(
				35, ADealershipNarrative.TheWashIncurred, ADealershipNarrative.TheWashAmount,
				ADealershipNarrative.TheWashCurrency, ADealershipNarrative.TheWashSubject));

			Apply(journal, ADealershipNarrative.RecurringCost(
				36, ADealershipNarrative.TheOilChangeIncurred, ADealershipNarrative.TheOilChangeAmount,
				ADealershipNarrative.TheOilChangeCurrency, ADealershipNarrative.TheOilChangeSubject));
		}

		private static void TheDurableCostByTheDoor(AScheduleOnARealJournal journal)
		{
			journal.Api.RecordDurableCost(
				TheInstant(ADealershipNarrative.TheTyresIncurred),
				TheAmount(ADealershipNarrative.TheTyresAmount),
				ADealershipNarrative.TheTyresCurrency, ADealershipNarrative.TheTyresSubject,
				ADealershipNarrative.TheTyresLifeInMonths);
		}

		private static void TheRestByTheDoor(AScheduleOnARealJournal journal)
		{
			journal.Api.RecordRecurringCost(
				TheInstant(ADealershipNarrative.TheWashIncurred),
				TheAmount(ADealershipNarrative.TheWashAmount),
				ADealershipNarrative.TheWashCurrency, ADealershipNarrative.TheWashSubject);

			journal.Api.RecordRecurringCost(
				TheInstant(ADealershipNarrative.TheOilChangeIncurred),
				TheAmount(ADealershipNarrative.TheOilChangeAmount),
				ADealershipNarrative.TheOilChangeCurrency, ADealershipNarrative.TheOilChangeSubject);
		}

		private static void Apply(AScheduleOnARealJournal journal, SenalDeEvento signal)
		{
			IMensajeDeEvento message = DealershipEventRoute.Route(signal);

			Assert.That(message, Is.Not.Null,
				"La ruta descarto " + signal.Tipo + ", asi que ese hecho no llego por el feed y la "
				+ "comparacion estaria entre un libro y otro que no tiene lo mismo.");

			Assert.That(journal.Api.Apply(message), Is.EqualTo(""),
				"El dominio rechazo " + signal.Tipo + " por el feed.");
		}

		private static DateTime TheInstant(string said)
		{
			return DateTime.Parse(said, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
		}

		private static decimal TheAmount(string said)
		{
			return decimal.Parse(said, NumberStyles.Number, CultureInfo.InvariantCulture);
		}

		private static void TheSevenAgree(
			AScheduleOnARealJournal byTheFeed, AScheduleOnARealJournal byTheDoor, string state)
		{
			Dictionary<string, string> feed = TheSevenAnswersOf(byTheFeed);
			Dictionary<string, string> door = TheSevenAnswersOf(byTheDoor);

			Assert.Multiple(() =>
			{
				foreach (KeyValuePair<string, string> each in feed)
					Assert.That(door[each.Key], Is.EqualTo(each.Value),
						"'" + each.Key + "' contesta distinto segun por donde llego el gasto, " + state
						+ ". Los dos caminos aterrizan en el mismo comando del dominio, asi que una "
						+ "diferencia aca es del camino y no del libro.");
			});
		}

		// Las siete, cada una con el nombre de su miembro al lado (C60), reducidas a texto para poder
		// compararlas de a pares. Las SEIS columnas enteras van en la cadena: comparar solo la cuenta
		// dejaria pasar una columna con otro contenido.
		private static Dictionary<string, string> TheSevenAnswersOf(AScheduleOnARealJournal journal)
		{
			CostLedgerRecord record = journal.Api.CostEntriesBetween(TheYearFrom, TheYearTo, TheLanguage.OfTheRecord);

			return new Dictionary<string, string>
			{
				{
					"HasAnyCostBetween",
					journal.Api.HasAnyCostBetween(TheYearFrom, TheYearTo).ToString()
				},
				{
					"DescribeSpendBetween",
					journal.Api.DescribeSpendBetween(TheColonesHalfFrom, TheColonesHalfTo, TheLanguage.OfTheRecord)
					+ " // " + journal.Api.DescribeSpendBetween(TheYearFrom, TheYearTo, TheLanguage.OfTheRecord)
				},
				{
					"IsSpendSaidInOneCurrencyBetween",
					journal.Api.IsSpendSaidInOneCurrencyBetween(
						TheColonesHalfFrom, TheColonesHalfTo).ToString()
					+ " // "
					+ journal.Api.IsSpendSaidInOneCurrencyBetween(TheYearFrom, TheYearTo)
				},
				{
					"DescribeSpendPerCurrencyBetween",
					journal.Api.DescribeSpendPerCurrencyBetween(TheYearFrom, TheYearTo, TheLanguage.OfTheRecord)
				},
				{
					"DescribeTwoPeriodsOfSpendCompared",
					journal.Api.DescribeTwoPeriodsOfSpendCompared(TheColonesHalfFrom, TheColonesHalfTo, TheYearBeforeFrom, TheYearBeforeTo, TheLanguage.OfTheRecord)
				},
				{
					"CostEntryCountBetween",
					journal.Api.CostEntryCountBetween(TheYearFrom, TheYearTo).ToString()
				},
				{
					"CostEntriesBetween",
					string.Join("|", record.IncurredAt) + " / " + string.Join("|", record.Subject)
					+ " / " + string.Join("|", record.Money)
					+ " / " + string.Join("|", record.Classification)
					+ " / " + string.Join("|", record.ExpectedLife)
					+ " / " + string.Join("|", record.Says)
				},
			};
		}
	}
}
