using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using Choreography.Theater;
using Concesionario.App;
using NUnit.Framework;
using Puppeteer;

namespace Concesionario.Dominio.Pruebas
{
	// LA PRUEBA 18 DE SLICE-TheMaintenanceScreen.md, Y ES LA PRUEBA DE UNA AUSENCIA. Un journal
	// escrito por la app de ANTES de esta rebanada se abre con la app de HOY, contesta todo lo que
	// contestaba igualito, y las diecinueve lecturas nuevas contestan sobre ese mismo registro. NO HAY
	// MIGRACION AQUI: esta prueba demuestra que no hay ninguna, no ejecuta una.
	//
	// POR QUE UN HOST ESCRITO A MANO SI LOS CINCO UPGRADES SON LOS MISMOS. Justamente por eso. MS- no
	// agrega global de raiz, ni nombre de upgrade, ni comando: la app de antes de la rebanada y la de
	// hoy declaran EL MISMO TEXTO. Un fixture que abriera el journal con DealershipCarApp las dos
	// veces estaria comparando la app de hoy consigo misma y no probaria nada -- que es la forma
	// verde-y-vacia que esta prueba mas facilmente podia tomar. La copia congelada de OnHydrated que
	// esta abajo es la FORMA HISTORICA: el dia que alguien le agregue una global a DealershipCarApp,
	// le reordene las cinco lineas o le reescriba un cuerpo, los dos textos dejan de calzar y esta
	// prueba se pone roja diciendo que aparecio una migracion donde la rebanada prometio ninguna.
	//
	// EL INSTRUMENTO ES EL COSTO DE LA APERTURA, EN ENTRADAS, Y NO UNA AFIRMACION. La medicion la dejo
	// TheExtraEntryOfAMigrationIsTheNewScriptAndNotTheUpgradeBody (SH-3, quinta global): una apertura
	// en regimen cuesta UNA entrada, y cuesta DOS en cuanto el texto del script cambia -- aunque no
	// haya ningun cuerpo de upgrade nuevo, porque un texto que este actor nunca vio es una PLANTILLA
	// nueva. Asi que +1 dice las dos cosas a la vez: ningun cuerpo corrio Y el texto es identico.
	// El control positivo esta en el mismo test: un host con los CINCO MISMOS NOMBRES en otro orden
	// cuesta +2.
	//
	// LO QUE ESTE INSTRUMENTO NO VE, MEDIDO Y NO SUPUESTO: un ESPACIO de mas adentro de un cuerpo de
	// upgrade no cuesta nada. Se falsifico escribiendo "logbook  = Logbook();" en el host congelado
	// de abajo y la suite entera quedo verde. Asi que la plantilla no se llavea por el texto crudo:
	// el reordenamiento SI cuesta +2 y el blanco no. Agrega un nombre, quitalo o cambialo de lugar y
	// esta prueba lo ve; reformatea un cuerpo y no.
	//
	// Y EL REGISTRO YA TRAIA TODO LO QUE LAS DIECINUEVE NECESITAN. Las completaciones y los precios
	// los escriben CompleteOperation y PublishServicePrice, que existen desde antes; la rebanada no
	// agrego ni un hecho. Por eso la mitad "y las lecturas nuevas contestan" se puede pedir sobre el
	// journal viejo SIN escribirle nada primero -- que es la forma mas fuerte de decir que no hubo
	// nada que migrar.
	//
	// DOS MIEMBROS PUBLICOS DE LA PUERTA QUE NO SON LECTURAS, y por eso el barrido los excluye POR
	// NOMBRE: Evaluate(DateTime) devuelve EvaluationOutcome y hace PerformCheckThenCommand, y
	// Apply(IMensajeDeEvento) es la ruta de ingesta. "Devuelve algo" no es "es una lectura". La lista
	// de exclusiones se defiende sola: el barrido compara el CurrentEntryId del actor antes y despues
	// de correr, asi que un tercer miembro que escribiera pondria rojo este mismo test.
	//
	// POR ENTRADAS Y NO POR BYTES, y se midio antes de escribirlo asi. Los bytes del archivo del
	// journal se mueven mientras el proceso vive aunque ninguna lectura escriba: con el barrido
	// instrumentado miembro por miembro, NINGUNO movio un byte, y la comparacion de antes-y-despues
	// del barrido entero si se movia. La forma por bytes es la de
	// NoneOfTheNineteenReadsWritesAnEntrySoThereIsNothingToMigrate, que la toma despues de una fase
	// de escritura completa; aqui se toma con el host recien abierto y no aguanta.
	[TestFixture]
	public sealed class NothingMovedAndThereWasNoMigrationToPerform
	{
		private const string TheVin = "JH4KA7561PC008269";
		private const string TheModel = "YARIS-2019";

		private const string TheKindThatMeasuresDistance = "FluidChange";
		private const string TheKindMeasuredOnlyInTime = "TimedComponent";
		private const string TheUnitEverythingIsIn = "UnitA";
		private const string TheRevision = "REV-2025-01";
		private const string TheSystemThatIsCovered = "POWERTRAIN";

		// LA MARCA DE QUE LA CADENA BARRIDA TRAE CONTENIDO DE VERDAD. Era TheDeviceKey, que se fue
		// con la credencial el 2026-09-03 (un token no es un hecho de la vida de la persona).
		//
		// TIENE QUE SER UN VALOR QUE EL BARRIDO NO INYECTE, y eso descarta a casi todas las otras
		// constantes: Invoke() le pasa TheRevision a cualquier `revisionCode`, TheSystemThatIsCovered
		// a `systemCode`, TheUnitEverythingIsIn a lo que termine en Unit y TheKindThatMeasuresDistance
		// a cualquier otro string, asi que aparecen ECHADAS en las respuestas hasta de un journal
		// vacio y la marca dejaria de distinguir nada. Se probo con TheRevision y esta prueba se puso
		// roja por eso mismo. El grado de combustible lo escribe PublishFuelGrade, lo contesta
		// ReadMinimumFuelGrade() -sin parametros, asi que nadie se lo inyecta- y un journal fresco no
		// lo tiene.
		private const string TheFuelGradeThatWasPublished = "91 RON";

		// TheDeviceKey SE FUE con la credencial. Era la marca; ver TheFuelGradeThatWasPublished.

		private const int TheTimeLimitInDays = 365;
		private const int TheDistanceLimit = 12345;
		private const int TheOdometerAtTheAnchor = 20000;
		private const int TheOdometerAtDelivery = 18000;
		private const int TheOdometerToday = 31900;

		private static readonly DateTime TheAnchor =
			new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Utc);
		private static readonly DateTime TheDelivery =
			new DateTime(2025, 1, 5, 0, 0, 0, DateTimeKind.Utc);
		private static readonly DateTime TheFirstCompletion =
			new DateTime(2025, 4, 11, 12, 0, 0, DateTimeKind.Utc);
		private static readonly DateTime TheSecondCompletion =
			new DateTime(2025, 8, 22, 9, 30, 0, DateTimeKind.Utc);
		private static readonly DateTime TheTimedCompletion =
			new DateTime(2025, 11, 5, 7, 45, 0, DateTimeKind.Utc);
		private static readonly DateTime TheDayItWasObserved =
			new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc);
		private static readonly DateTime ThePublication =
			new DateTime(2025, 3, 14, 10, 0, 0, DateTimeKind.Utc);
		private static readonly DateTime ValidFrom =
			new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		private static readonly DateTime ValidUntil =
			new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc);
		private static readonly DateTime TheStayEntered =
			new DateTime(2026, 1, 15, 8, 30, 0, DateTimeKind.Utc);
		private static readonly DateTime TheStayCollected =
			new DateTime(2026, 1, 16, 16, 45, 0, DateTimeKind.Utc);

		private static readonly string[] TheStayDays = { "2026-01-15", "2026-01-16" };

		// El instante con que se le pregunta a toda la puerta, y los dos limites de periodo. Fijos:
		// una respuesta que cambiara entre las dos mitades de la comparacion por el reloj no diria
		// nada sobre la migracion.
		private static readonly DateTime TheDayItIsAsked =
			new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);

		private const string TheFirstDayOfThePeriod = "2026-01-01";
		private const string TheLastDayOfThePeriod = "2026-12-31";

		// LAS DIECINUEVE DE ESTA REBANADA, escritas por nombre y aridad. Sirven dos veces: se restan
		// del barrido de "lo que ya existia", y se afirma que TODAS siguen en la puerta -- una lista
		// que envejeciera dejaria alguna del lado equivocado de la resta sin avisar.
		private static readonly string[] TheNineteenThisSliceAdded =
		{
			"CanDistanceRemainingBeSaid/2",
			"ReadDistanceRemaining/2",
			"ReadDistanceRemainingUnit/2",
			"ReadDistanceRemainderStanding/2",
			"DescribeDistanceRemaining/3",
			"CanDaysRemainingBeSaid/2",
			"ReadDaysRemaining/2",
			"ReadTimeRemainderStanding/2",
			"DescribeDaysRemaining/3",
			"DescribeCountdown/3",
			"HasAnyCompletion/1",
			"CompletionCount/1",
			"DescribeCompletions/1",
			"CompletionsOf/1",
			"CompletionsOfEveryKind/0",
			"HasAnyPublishedServicePrice/0",
			"PublishedServicePriceCount/0",
			"PublishedServicePricesAsOf/2",
			"DescribePublishedServicePrice/4",
		};

		// LAS NUEVE DEL LIBRO DE COSTOS, Y ESTAR AQUI ES OTRA COSA QUE ESTAR EN LA LISTA DE ARRIBA.
		// Las diecinueve se restan porque son las que ESTA rebanada agrego y la comparacion es sobre
		// lo que ya existia. Estas nueve se restan por una razon estructural: BuildOrder/CL-4 le
		// agrego a la app una SEXTA GLOBAL DE RAIZ -- el libro de costos -- y la app de antes de
		// MS- no la tiene, asi que sobre ella las nueve no rechazan una regla de negocio: el DSL no
		// encuentra 'ledger' y revienta. Comparar ese rechazo contra la respuesta que da la app de hoy
		// -- que abrio el mismo journal y le APLICO la migracion -- mide la migracion de CL-4 y no la
		// promesa de esta rebanada.
		//
		// ESTE FIXTURE SE PUSO ROJO EN CL-5 Y EL ROJO ERA CORRECTO, que es la misma forma que CL-4 ya
		// habia encontrado por el lado del CONTEO DE ENTRADAS: una prueba de ausencia-de-migracion
		// escrita contra LA APP DE HOY envejece el dia que una rebanada POSTERIOR agrega una global, y
		// envejece poniendose roja, que es la buena manera. La correccion es restarlas POR NOMBRE con
		// el motivo escrito, no aflojar la comparacion.
		//
		// Y VOLVIO A PONERSE ROJO EN CL-6, POR LA MISMA RAZON Y CON DOS NOMBRES MAS: las dos lecturas
		// del CRUCE nombran 'ledger' adentro de su script -- una en la rama then de un if y la otra en
		// el segundo operando de un && -- asi que sobre la app de antes revientan igual que las siete.
		// Que sean CRUZADAS no cambia nada aca: lo que decide es que global nombra el script.
		//
		// Y NO SE AFLOJA, porque la resta se defiende con las mismas dos mitades que la de arriba: la
		// cuenta escrita, que ve un nombre que se caiga, y la afirmacion de que las nueve siguen en la
		// puerta, que ve un nombre que sobre. Que las nueve CONTESTEN sobre el journal ya migrado lo
		// afirma TheSixthGlobalReachesAJournalThatHasTheOtherFive, que es de quien es esa mitad.
		private static readonly string[] TheNineTheCostLedgerAdded =
		{
			"HasAnyCostBetween/2",
			"DescribeSpendBetween/3",
			"IsSpendSaidInOneCurrencyBetween/2",
			"DescribeSpendPerCurrencyBetween/3",
			"DescribeTwoPeriodsOfSpendCompared/5",
			"CostEntriesBetween/3",
			"CostEntryCountBetween/2",
			"CanCostPerDistanceBeSaid/2",
			"DescribeCostPerDistanceBetween/3",
		};

		// LOS DOS MIEMBROS PUBLICOS QUE DEVUELVEN ALGO Y ESCRIBEN. No son lecturas y el barrido no los
		// puede invocar sin ensuciar justo lo que compara.
		private static readonly string[] TheTwoQuestionShapedWriters = { "Evaluate/1", "Apply/1" };

		private string folder;

		[SetUp]
		public void Setup()
		{
			folder = ATemporaryFolder();
		}

		[TearDown]
		public void TearDown()
		{
			Sweep(folder);
		}

		// LA AUSENCIA DE LA MIGRACION DE MS-, MEDIDA EN ENTRADAS DEL JOURNAL -- Y LO QUE ESTE
		// INSTRUMENTO MIDE DE VERDAD ES LA DISTANCIA ENTRE DOS APPS, no la deuda de una rebanada. +1
		// es lo que cuesta una apertura en regimen; +2 es lo que cuesta una que migra, y lo cuesta
		// tambien cuando lo unico nuevo es el TEXTO del script. Se escribio comparando el journal de
		// antes de MS- contra LA APP DE HOY, y ese +1 valia mientras la app de hoy declarara los
		// mismos CINCO upgrades que la de entonces. BuildOrder/CL-4 le agrego el SEXTO --
		// 'libro-de-costos' -- asi que la primera apertura con la app de hoy paga UNA migracion, y esa
		// migracion es de CL-4 y no de MS-.
		//
		// LO QUE DEFIENDE LA AUSENCIA DE MS- DESPUES DE ESO SON LAS OTRAS DOS MITADES: la segunda
		// apertura vuelve a +1, y el renglon que nombra CUAL global llego dice que la unica deuda
		// pagada es la del libro de costos. El control positivo sigue en el mismo test para que un +2
		// que dejara de discriminar se vea.
		[Test]
		public void AJournalFromBeforeThisSliceCostsTheOpeningItAlwaysCostAndNothingMore()
		{
			long afterTheOlderAppWrote = TheWholeReceivedSequenceWrittenByTheOlderApp();

			DealershipCarApp today = TheAppOfTodayOpening(folder);
			long afterTodaysApp = today.Actor.CurrentEntryId;
			string theGlobalThatArrived = ALedgerReadWithoutTheDoor.Vin(today);
			today.Dispose();

			TestContext.Out.WriteLine(
				"COSTO DE ABRIR UN JOURNAL DE ANTES DE MS- CON LA APP DE HOY -- CurrentEntryId: "
				+ afterTheOlderAppWrote + " -> " + afterTodaysApp);

			Assert.That(afterTodaysApp, Is.EqualTo(afterTheOlderAppWrote + 2),
				"DOS, y las dos estan contadas: la que toda apertura paga por el comando de la "
				+ "semilla, mas la del texto de script nuevo que trajo la sexta global de CL-4. MS- "
				+ "no agrega global, ni nombre de upgrade, ni comando; lo que este numero mide es la "
				+ "distancia entre la app de antes de MS- y la de HOY, y esa distancia ya no es cero "
				+ "por una rebanada posterior.");

			Assert.That(theGlobalThatArrived, Is.EqualTo(TheVin),
				"Y LA MIGRACION QUE SE PAGO ES LA DEL LIBRO DE COSTOS, nombrada. Un +2 solo dice que "
				+ "algo se aplico; esto dice que.");

			DealershipCarApp again = TheAppOfTodayOpening(folder);
			long afterASecondOpening = again.Actor.CurrentEntryId;
			again.Dispose();

			Assert.That(afterASecondOpening, Is.EqualTo(afterTodaysApp + 1),
				"Y VUELVE A COSTAR UNA, que es la mitad que sigue defendiendo lo que esta prueba "
				+ "nombra: no queda nada por aplicar. Si la pantalla de mantenimiento hubiera "
				+ "necesitado una migracion propia, esta apertura la seguiria pagando.");

			// EL CONTROL POSITIVO, y sin el un +1 constante pasaria igual. Los CINCO MISMOS NOMBRES en
			// otro orden son otro TEXTO, y otro texto cuesta +2 sin que ningun cuerpo vuelva a correr.
			string elsewhere = ATemporaryFolder();

			try
			{
				AnAppFromBeforeTheMaintenanceScreen older =
					new AnAppFromBeforeTheMaintenanceScreen(TheVin, TheModel);
				older.ConfigureStorage(DatabaseType.FileSystem, "path=" + elsewhere);
				older.Start();

				long beforeTheOtherText = older.Actor.CurrentEntryId;
				older.Dispose();

				AnAppWithTheSameFiveUpgradesReordered reordered =
					new AnAppWithTheSameFiveUpgradesReordered(TheVin, TheModel);
				reordered.ConfigureStorage(DatabaseType.FileSystem, "path=" + elsewhere);
				reordered.Start();

				long afterTheOtherText = reordered.Actor.CurrentEntryId;
				reordered.Dispose();

				TestContext.Out.WriteLine(
					"EL CONTROL -- los mismos cinco nombres en otro orden: " + beforeTheOtherText
					+ " -> " + afterTheOtherText);

				Assert.That(afterTheOtherText, Is.EqualTo(beforeTheOtherText + 2),
					"+2 CON CERO UPGRADES NUEVOS. Es lo que hace que el +1 de arriba signifique algo: "
					+ "el instrumento SI distingue, y lo que mide es que el texto de la app de hoy y "
					+ "el de la de antes de MS- son el mismo.");
			}
			finally
			{
				Sweep(elsewhere);
			}
		}

		// TODA PREGUNTA QUE YA EXISTIA CONTESTA LO MISMO. Se arma por REFLEXION sobre la puerta entera
		// y no con una lista escrita a mano: lo que hay que barrer son los SITIOS DE LLAMADA de la
		// regla -- las lecturas -- y una lista a mano deja afuera justo la que alguien agregue.
		[Test]
		public void EveryQuestionThatExistedBeforeThisSliceAnswersExactlyTheSameAfterwards()
		{
			TheWholeReceivedSequenceWrittenByTheOlderApp();

			AnAppFromBeforeTheMaintenanceScreen older =
				new AnAppFromBeforeTheMaintenanceScreen(TheVin, TheModel);
			older.ConfigureStorage(DatabaseType.FileSystem, "path=" + folder);
			older.Start();

			string beforeThisAppOpenedIt = EveryAnswerOf(older, TheReadsThatAlreadyExisted());
			older.Dispose();

			DealershipCarApp today = TheAppOfTodayOpening(folder);

			string afterThisAppOpenedIt = EveryAnswerOf(today, TheReadsThatAlreadyExisted());

			today.Dispose();

			Assert.That(afterThisAppOpenedIt, Is.EqualTo(beforeThisAppOpenedIt),
				"Una pregunta que ya existia contesta distinto despues de que la app de hoy abrio el "
				+ "journal. Como no se le aplico nada, la unica explicacion seria que la puerta "
				+ "cambio lo que esa pregunta responde -- que es exactamente lo que la rebanada "
				+ "prometio no hacer.");

			Assert.That(beforeThisAppOpenedIt, Does.Contain(TheFuelGradeThatWasPublished),
				"Y la cadena comparada trae de verdad lo que se escribio. Sin esto, dos cadenas "
				+ "vacias serian iguales y este test estaria verde sin mirar nada.");
		}

		// LA MITAD QUE FALTA DE LA FRASE DE LA PRUEBA 18: "y las lecturas nuevas contestan". Sobre el
		// journal que escribio la app de antes, SIN aplicarle nada. Cada una nombrada, y cada
		// afirmacion sobre un valor que solo esa lectura puede dar (C60): una de por si no vacia
		// pasaria aunque las diecinueve contestaran todas lo mismo.
		[Test]
		public void AndTheNineteenOfThisSliceAnswerOnThatSameUnmigratedJournal()
		{
			TheWholeReceivedSequenceWrittenByTheOlderApp();

			DealershipCarApp today = TheAppOfTodayOpening(folder);
			DealershipCarAPI api = new DealershipCarAPI(today);

			try
			{
				Assert.That(api.CanDistanceRemainingBeSaid(TheKindThatMeasuresDistance, TheDayItIsAsked),
					Is.True);
				Assert.That(api.ReadDistanceRemaining(TheKindThatMeasuresDistance, TheDayItIsAsked),
					Is.EqualTo(TheDistanceLimit - (TheOdometerToday - 27300)),
					"El resto se cuenta desde el ancla de la ocurrencia viva, que es la completacion "
					+ "mas reciente que escribio la app de antes.");
				Assert.That(api.ReadDistanceRemainingUnit(TheKindThatMeasuresDistance, TheDayItIsAsked),
					Is.EqualTo("km"),
					"C67: la unidad se dice dos veces -- UnitA a la maquina y km a la persona. La puerta "
					+ "contesta la de la persona, y en un journal viejo tambien.");
				Assert.That(api.ReadDistanceRemainderStanding(TheKindThatMeasuresDistance, TheDayItIsAsked),
					Is.EqualTo("StillAhead"));
				Assert.That(api.DescribeDistanceRemaining(TheKindThatMeasuresDistance, TheDayItIsAsked,
					TheLanguage.OfTheRecord),
					Does.Contain("distancia: te faltan"));

				Assert.That(api.CanDaysRemainingBeSaid(TheKindThatMeasuresDistance, TheDayItIsAsked),
					Is.True);
				Assert.That(api.ReadDaysRemaining(TheKindThatMeasuresDistance, TheDayItIsAsked),
					Is.EqualTo(TheTimeLimitInDays - (int)(TheDayItIsAsked - TheSecondCompletion).TotalDays));
				Assert.That(api.ReadTimeRemainderStanding(TheKindThatMeasuresDistance, TheDayItIsAsked),
					Is.EqualTo("StillAhead"));
				Assert.That(api.DescribeDaysRemaining(TheKindThatMeasuresDistance, TheDayItIsAsked,
					TheLanguage.OfTheRecord),
					Does.Contain("tiempo: te faltan"));
				Assert.That(api.DescribeCountdown(TheKindThatMeasuresDistance, TheDayItIsAsked,
					TheLanguage.OfTheRecord),
					Does.Contain("distancia:").And.Contain("tiempo:"),
					"G1 en el journal viejo: dice las dos mitades y no escoge.");

				Assert.That(api.HasAnyCompletion(TheKindThatMeasuresDistance), Is.True);
				Assert.That(api.CompletionCount(TheKindThatMeasuresDistance), Is.EqualTo(2),
					"Las dos completaciones que escribio la app de antes, caminadas de la cadena.");
				Assert.That(api.DescribeCompletions(TheKindThatMeasuresDistance),
					Does.Contain("se hizo el").And.Contain("27300"));
				Assert.That(api.CompletionsOf(TheKindThatMeasuresDistance).CompletedAt,
					Has.Length.EqualTo(2));
				Assert.That(api.CompletionsOfEveryKind().Kind, Has.Length.EqualTo(3),
					"Tres filas: dos de la clase que mide distancia y una de la que solo mide tiempo.");

				Assert.That(api.HasAnyPublishedServicePrice(), Is.True);
				Assert.That(api.PublishedServicePriceCount(), Is.EqualTo(1));
				Assert.That(api.PublishedServicePricesAsOf(
					TheDayItIsAsked, TheLanguage.OfTheRecord).Revision,
					Is.EqualTo(new[] { TheRevision }),
					"L9 en el journal viejo: la fila nombra la revision de la que salio.");
				Assert.That(api.DescribePublishedServicePrice(
					"Inspection", 10000, TheUnitEverythingIsIn, TheLanguage.OfTheRecord),
					Does.Contain("₡120,50"));
			}
			finally
			{
				today.Dispose();
			}
		}

		// QUE LAS RESPUESTAS SALEN DEL JOURNAL Y NO DEL PROCESO QUE LAS CONTESTA. Dos controles: la
		// app de hoy sobre una carpeta VACIA no contesta ninguno de esos hechos, y su CurrentEntryId
		// no es el del journal escrito. Sin esto, "se reabrio" es una suposicion.
		[Test]
		public void TheAnswersAreReadFromTheJournalAndNotFromTheAppThatIsAnsweringThem()
		{
			long afterTheOlderAppWrote = TheWholeReceivedSequenceWrittenByTheOlderApp();

			DealershipCarApp today = TheAppOfTodayOpening(folder);
			string fromTheWrittenJournal = EveryAnswerOf(today, TheReadsThatAlreadyExisted());
			long onTheWrittenJournal = today.Actor.CurrentEntryId;
			today.Dispose();

			string empty = ATemporaryFolder();

			try
			{
				DealershipCarApp fresh = TheAppOfTodayOpening(empty);
				string fromAJournalNobodyWroteTo = EveryAnswerOf(fresh, TheReadsThatAlreadyExisted());
				long onAFreshJournal = fresh.Actor.CurrentEntryId;
				fresh.Dispose();

				TestContext.Out.WriteLine(
					"CurrentEntryId sobre el journal escrito: " + onTheWrittenJournal
					+ " -- sobre una carpeta vacia: " + onAFreshJournal);

				Assert.That(fromAJournalNobodyWroteTo, Is.Not.EqualTo(fromTheWrittenJournal),
					"La MISMA clase de app sobre una carpeta vacia contesta otra cosa. Eso es lo que "
					+ "dice que las respuestas de arriba vienen de los bytes del journal y no de un "
					+ "campo que la app se siembra sola al arrancar.");
				Assert.That(fromAJournalNobodyWroteTo, Does.Not.Contain(TheFuelGradeThatWasPublished));
				Assert.That(onAFreshJournal, Is.LessThan(onTheWrittenJournal),
					"Y el actor de hoy sabe cuantas entradas traia el journal, que es un numero que "
					+ "solo se puede saber leyendolo.");
			}
			finally
			{
				Sweep(empty);
			}
		}

		// ===== EL BARRIDO DE LAS LECTURAS QUE YA EXISTIAN ==================================

		// Toda pregunta publica de la puerta, menos las diecinueve de esta rebanada y menos los dos
		// miembros que devuelven algo y escriben.
		private static List<MethodInfo> TheReadsThatAlreadyExisted()
		{
			var nineteen = new List<string>(TheNineteenThisSliceAdded);
			var ofTheLedger = new List<string>(TheNineTheCostLedgerAdded);
			var writers = new List<string>(TheTwoQuestionShapedWriters);

			var reads = new List<MethodInfo>();
			var seenNineteen = new List<string>();
			var seenNine = new List<string>();

			foreach (MethodInfo each in typeof(DealershipCarAPI).GetMethods(
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
			{
				if (each.ReturnType == typeof(void)) continue;

				string key = each.Name + "/" + each.GetParameters().Length;

				if (nineteen.Contains(key)) { seenNineteen.Add(key); continue; }
				if (ofTheLedger.Contains(key)) { seenNine.Add(key); continue; }
				if (writers.Contains(key)) continue;

				reads.Add(each);
			}

			// LAS DOS MITADES HACEN FALTA, Y LA PRIMERA SE GANO CON UNA FALSIFICACION QUE NO PUSO NADA
			// ROJO. La comparacion de abajo es contra la lista misma, asi que borrarle un nombre la deja
			// verde: lo que encuentra el filtro se encoge junto con lo que se compara. El numero
			// escrito es lo unico que ve ese borrado -- y borrar un nombre es justo como una de las
			// diecinueve se colaria del lado de "lo que ya existia".
			Assert.That(TheNineteenThisSliceAdded, Has.Length.EqualTo(19),
				"La lista dejo de tener diecinueve nombres. Una que se le caiga se cuenta como si ya "
				+ "hubiera existido antes de la rebanada, y la comparacion pasaria diciendo lo "
				+ "contrario de lo que dice.");

			Assert.That(seenNineteen, Is.EquivalentTo(TheNineteenThisSliceAdded),
				"Un nombre de la lista ya no esta en la puerta, o cambio de aridad.");

			// Y LAS NUEVE DEL LIBRO, CON LAS MISMAS DOS MITADES. La cuenta escrita ve un nombre que se
			// caiga -- que se contaria como si hubiera existido antes de esta rebanada -- y la
			// equivalencia ve uno que sobre.
			Assert.That(TheNineTheCostLedgerAdded, Has.Length.EqualTo(9),
				"La lista del libro dejo de tener nueve nombres. Una que se le caiga se cuenta como si "
				+ "ya hubiera existido antes de MS-, y la comparacion volveria a medir la migracion de "
				+ "CL-4 en vez de lo que dice medir.");

			Assert.That(seenNine, Is.EquivalentTo(TheNineTheCostLedgerAdded),
				"Un nombre de la lista del libro ya no esta en la puerta, o cambio de aridad.");

			reads.Sort((a, b) => string.CompareOrdinal(
				a.Name + "/" + a.GetParameters().Length, b.Name + "/" + b.GetParameters().Length));

			return reads;
		}

		// Cada lectura NOMBRADA al lado de su respuesta, por la razon de C60: una comparacion de
		// respuestas sin el nombre del miembro no ve un cruce entre dos de ellas.
		private static string EveryAnswerOf(PerformanceV2 host, List<MethodInfo> reads)
		{
			DealershipCarAPI api = new DealershipCarAPI(host);

			long beforeTheSweep = host.Actor.CurrentEntryId;

			var every = new StringBuilder();

			foreach (MethodInfo each in reads)
			{
				every.Append('\n').Append(each.Name).Append('/')
					.Append(each.GetParameters().Length).Append(" : ");

				try
				{
					every.Append(Rendered(Invoke(api, each)));
				}
				catch (Exception refused)
				{
					every.Append("REFUSED ").Append(refused.GetType().Name)
						.Append(" ").Append(refused.Message);
				}
			}

			Assert.That(host.Actor.CurrentEntryId, Is.EqualTo(beforeTheSweep),
				"El barrido ESCRIBIO una entrada, asi que uno de los miembros que recorre no es una "
				+ "lectura. La lista de exclusiones tiene dos nombres y se defiende con este renglon: "
				+ "un tercer miembro que escriba pone rojo esto y no otra cosa. SE MIDE POR ENTRADAS "
				+ "Y NO POR BYTES: los bytes del archivo se mueven solos mientras el proceso vive -- "
				+ "medido aqui, con un barrido cuyos miembros, uno por uno, no mueven ninguno.");

			return every.ToString();
		}

		// Los argumentos se arman por TIPO, con las excepciones por NOMBRE que la puerta obliga: un
		// limite de periodo tiene que DECIR UN DIA, y una unidad tiene que nombrar un miembro del
		// conjunto cerrado. Un nombre que este barrido no sepa armar falla en vez de adivinar.
		private static object Invoke(DealershipCarAPI api, MethodInfo member)
		{
			var arguments = new List<object>();

			foreach (ParameterInfo each in member.GetParameters())
			{
				if (each.ParameterType == typeof(DateTime)) { arguments.Add(TheDayItIsAsked); continue; }

				if (each.Name == "fromDay") { arguments.Add(TheFirstDayOfThePeriod); continue; }
				if (each.Name == "toDay") { arguments.Add(TheLastDayOfThePeriod); continue; }
				if (each.Name.EndsWith("Unit", StringComparison.Ordinal))
				{
					arguments.Add(TheUnitEverythingIsIn);
					continue;
				}
				if (each.Name == "systemCode") { arguments.Add(TheSystemThatIsCovered); continue; }
				if (each.Name == "revisionCode") { arguments.Add(TheRevision); continue; }
				if (each.Name == "position") { arguments.Add("Front"); continue; }

				if (each.ParameterType == typeof(string))
				{
					arguments.Add(TheKindThatMeasuresDistance);
					continue;
				}

				if (each.ParameterType == typeof(int)) { arguments.Add(1000); continue; }

				Assert.Fail(
					"La pregunta '" + member.Name + "' toma un " + each.ParameterType.Name + " llamado '"
					+ each.Name + "', que este barrido no sabe armar. Se arma por TIPO a proposito; "
					+ "agregue el tipo nuevo y diga con que valor se pregunta.");
			}

			try
			{
				return member.Invoke(api, arguments.ToArray());
			}
			catch (TargetInvocationException wrapped)
			{
				throw wrapped.InnerException;
			}
		}

		// Un DTO de la puerta es un juego de columnas paralelas: se rinde columna por columna, con el
		// nombre de cada una, para que un cruce entre dos columnas cambie la cadena.
		private static string Rendered(object answer)
		{
			if (answer == null) return "null";

			if (answer is string text) return text;
			if (answer is DateTime instant) return instant.ToString("O", CultureInfo.InvariantCulture);

			// CUALQUIER COLUMNA, Y NO SOLO UNA DE TEXTO. Decia string[] y BuildOrder/OD-2 le trajo la
			// primera columna numerica de este modulo: un int[] caia a la rama de reflexion de abajo,
			// donde Array.SyncRoot devuelve EL PROPIO ARREGLO y la recursion no termina.
			//
			// EL MODO DE FALLA ES POR QUE ESTO SE ARREGLA EN VEZ DE ANOTARSE: no era un rojo. Era un
			// desborde de pila que ABORTA EL PROCESO ENTERO, asi que la corrida no reportaba 1 fallo
			// -- reportaba "Test Run Aborted" y ninguna cuenta. Rendir elemento por elemento sirve
			// para toda columna que venga despues y de paso saca del camino el SyncRoot.
			if (answer is Array column)
			{
				var joined = new List<string>();

				foreach (object each in column) joined.Add(Rendered(each));

				return string.Join(" / ", joined);
			}

			if (answer is IFormattable formattable)
				return formattable.ToString(null, CultureInfo.InvariantCulture);

			var rendered = new StringBuilder(answer.GetType().Name).Append('{');

			foreach (PropertyInfo each in answer.GetType().GetProperties(
				BindingFlags.Instance | BindingFlags.Public))
				rendered.Append(' ').Append(each.Name).Append('=')
					.Append(Rendered(each.GetValue(answer)));

			foreach (FieldInfo each in answer.GetType().GetFields(
				BindingFlags.Instance | BindingFlags.Public))
				rendered.Append(' ').Append(each.Name).Append('=')
					.Append(Rendered(each.GetValue(answer)));

			return rendered.Append(" }").ToString();
		}

		// ===== LA SECUENCIA RECIBIDA, ESCRITA POR LA APP DE ANTES ==========================

		// Todo lo de abajo son comandos que existen desde antes de esta rebanada: MS- no agrega
		// ninguno. Devuelve el CurrentEntryId con que quedo el journal.
		private long TheWholeReceivedSequenceWrittenByTheOlderApp()
		{
			AnAppFromBeforeTheMaintenanceScreen older =
				new AnAppFromBeforeTheMaintenanceScreen(TheVin, TheModel);
			older.ConfigureStorage(DatabaseType.FileSystem, "path=" + folder);
			older.Start();

			DealershipCarAPI api = new DealershipCarAPI(older);


			api.SeedPlanOperation(
				TheKindThatMeasuresDistance, "TimeOrDistance",
				TheTimeLimitInDays, TheDistanceLimit, TheUnitEverythingIsIn, true, false,
				TheAnchor, TheOdometerAtTheAnchor, TheUnitEverythingIsIn);

			api.SeedPlanOperation(
				TheKindMeasuredOnlyInTime, "TimeOnly",
				TheTimeLimitInDays, 0, TheUnitEverythingIsIn, true, false,
				TheAnchor, TheOdometerAtTheAnchor, TheUnitEverythingIsIn);

			api.CompleteOperation(
				TheKindThatMeasuresDistance, TheFirstCompletion, 22500, TheUnitEverythingIsIn);
			api.CompleteOperation(
				TheKindThatMeasuresDistance, TheSecondCompletion, 27300, TheUnitEverythingIsIn);
			api.CompleteOperation(
				TheKindMeasuredOnlyInTime, TheTimedCompletion, 29100, TheUnitEverythingIsIn);

			api.RecordReading(
				TheOdometerToday, TheUnitEverythingIsIn, TheDayItWasObserved, "Telemetry");

			api.RecordDelivery(TheDelivery, TheOdometerAtDelivery, TheUnitEverythingIsIn);
			api.PublishWarrantyEnvelope(36, 60000, TheUnitEverythingIsIn, ThePublication);

			api.OpenRevision(TheRevision, ThePublication);
			api.PublishFuelGrade(TheRevision, TheFuelGradeThatWasPublished, "Minimum");
			api.PublishServiceInterval(
				TheRevision, TheKindThatMeasuresDistance, 10000, TheUnitEverythingIsIn, 365);
			api.PublishCoveredSystem(
				TheRevision, TheSystemThatIsCovered, 60, 100000, TheUnitEverythingIsIn);
			api.PublishServicePrice(
				TheRevision, "Inspection", 10000, TheUnitEverythingIsIn,
				120.50m, "CRC", ValidFrom, ValidUntil);
			api.SealRevision(TheRevision);

			api.RecordShopStay(
				"Taller Curridabat", "Cambio de aceite", TheStayEntered, TheStayCollected, TheStayDays);

			long written = older.Actor.CurrentEntryId;

			older.Dispose();

			return written;
		}

		private static DealershipCarApp TheAppOfTodayOpening(string where)
		{
			DealershipCarApp app = new DealershipCarApp(TheVin, "Standard", TheModel);
			app.ConfigureStorage(DatabaseType.FileSystem, "path=" + where);
			app.Start();

			return app;
		}

		private static string ATemporaryFolder()
		{
			string made = Path.Combine(
				AScheduleOnARealJournal.TheRootOfEveryTemporaryJournal, Guid.NewGuid().ToString("N"));

			Directory.CreateDirectory(made);

			return made;
		}

		private static void Sweep(string where)
		{
			try
			{
				if (Directory.Exists(where)) Directory.Delete(where, true);
			}
			catch (IOException)
			{
			}
			catch (UnauthorizedAccessException)
			{
			}
		}

		// LA APP TAL COMO ERA ANTES DE ESTA REBANADA, y son los MISMOS cinco upgrades de hoy porque
		// MS- no agrego ninguno. Los cinco cuerpos estan copiados palabra por palabra de
		// DealershipCarApp.OnHydrated, y esa copia ES la prueba: lo que se mide es que los dos textos
		// calzan letra por letra, de modo que la app de hoy no aplica nada sobre un journal viejo.
		// Copiar y no heredar tambien es a proposito: DealershipCarApp es sealed, y sobreescribir
		// OnHydrated reemplazaria los cinco.
		private sealed class AnAppFromBeforeTheMaintenanceScreen : PerformanceV2
		{
			private readonly string vin;
			private readonly string modelCode;

			internal AnAppFromBeforeTheMaintenanceScreen(string vin, string modelCode)
				: base(DealershipCarApp.TheActorFor(vin), typeof(MaintenanceSchedule).Assembly)
			{
				this.vin = vin;
				this.modelCode = modelCode;
			}

			protected override void OnHydrated()
			{
				Using(@"
					upgrade('semilla') {
						schedule = MaintenanceSchedule(@vin, @usage);
					}
					upgrade('bitacora-de-eventos') {
						logbook = Logbook();
					}
					upgrade('especificacion-del-modelo') {
						specification = ModelSpecification(@model);
					}
					upgrade('perfil-del-vehiculo') {
						profile = VehicleProfile(@vin);
					}
					upgrade('estadias-en-taller') {
						stays = ShopStay(@vin);
					}
				")
				.WithParameters(p => {
					p["vin",   typeof(string)] = vin;
					p["usage", typeof(Enum)]   = "Standard";
					p["model", typeof(string)] = modelCode;
				})
				.PerformCommand();
			}
		}

		// EL CONTROL POSITIVO: los MISMOS cinco nombres, en otro orden. Ningun cuerpo vuelve a correr
		// -- los upgrades llavean por nombre -- y la apertura cuesta +2 igual, porque el texto es una
		// plantilla que este actor nunca vio. Existe para que el +1 del test de arriba discrimine.
		private sealed class AnAppWithTheSameFiveUpgradesReordered : PerformanceV2
		{
			private readonly string vin;
			private readonly string modelCode;

			internal AnAppWithTheSameFiveUpgradesReordered(string vin, string modelCode)
				: base(DealershipCarApp.TheActorFor(vin), typeof(MaintenanceSchedule).Assembly)
			{
				this.vin = vin;
				this.modelCode = modelCode;
			}

			protected override void OnHydrated()
			{
				Using(@"
					upgrade('estadias-en-taller') {
						stays = ShopStay(@vin);
					}
					upgrade('perfil-del-vehiculo') {
						profile = VehicleProfile(@vin);
					}
					upgrade('especificacion-del-modelo') {
						specification = ModelSpecification(@model);
					}
					upgrade('bitacora-de-eventos') {
						logbook = Logbook();
					}
					upgrade('semilla') {
						schedule = MaintenanceSchedule(@vin, @usage);
					}
				")
				.WithParameters(p => {
					p["vin",   typeof(string)] = vin;
					p["usage", typeof(Enum)]   = "Standard";
					p["model", typeof(string)] = modelCode;
				})
				.PerformCommand();
			}
		}
	}
}
