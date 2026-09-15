using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Concesionario.App;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// PRUEBA 16 de §9 EN EL NIVEL B, y las tres verificaciones que §T6 pide hacer AQUI y no
	// despues.
	//
	// El nivel A ya prueba que dos agendas construidas con la misma secuencia contestan igual: eso
	// atrapa un reloj colado en el dominio. Lo que solo se puede probar aca es la promesa de E14
	// entera -- que la historia sobreviva al JOURNAL. La agenda es una global de raiz del DSL; si
	// fuera un campo de C# todo el nivel A seguiria verde y la primera rehidratacion encontraria la
	// agenda vacia.
	//
	// Y hay una tercera cosa que ninguno de los dos niveles ve por separado: que los valores hayan
	// viajado como PARAMETRO. Un valor interpolado en el texto del DSL tambien reproduce igual...
	// hasta que el texto es lo unico que queda y el dato original ya no existe.
	[TestFixture]
	public sealed class TheJournalReplaysTheSameReceivedSequenceTheSame
	{
		private const string TheVin = "1HGCM82633A004352";
		private const int TwelveMonthsInDays = 365;
		private const int TenThousandUnits = 10000;

		private static readonly DateTime Delivery =
			new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);
		private const int OdometerAtDelivery = 30000;

		private static readonly DateTime TheLastEvaluation =
			new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);

		// EL PAR DEL MISMO SEGUNDO, para las dos ultimas pruebas. El instante se construye ya
		// truncado a segundos y en UTC: la puerta normaliza en las dos direcciones (C24) y ademas
		// trunca, asi que construirlo asi deja la prueba honesta sobre lo que prueba.
		private const string TheOperationMeasuredByDistance = "WearPartReplacement";
		private const int TheReadingInThePlanUnit = 40000;
		private const int TheReadingInTheOtherUnit = 35000;

		private static readonly DateTime TheSameSecond =
			new DateTime(2026, 1, 20, 8, 30, 0, DateTimeKind.Utc);

		private static readonly DateTime TheEvaluationBeforeTheReopen =
			new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
		private static readonly DateTime TheEvaluationAfterTheReopen =
			new DateTime(2026, 2, 2, 0, 0, 0, DateTimeKind.Utc);

		// PRUEBA 16, la mitad que el nivel A tambien prueba: dos corridas de la misma secuencia
		// contestan lo mismo. Aca cada corrida pasa por su propio actor, su propio journal y su
		// propio disco.
		[Test]
		public void TwoRunsOfTheSameStoryAgreeOnEveryRead()
		{
			using (AScheduleOnARealJournal first = AJournalThatLivedTheWholeStory())
			using (AScheduleOnARealJournal second = AJournalThatLivedTheWholeStory())
			{
				Assert.That(
					second.EveryDueStateItCanAnswer(),
					Is.EqualTo(first.EveryDueStateItCanAnswer()));
			}
		}

		// La historia tiene que MOVER algo, o dos agendas vacias tambien coincidirian.
		[Test]
		public void TheStoryUsedByThisFixtureActuallyMovesTheSchedule()
		{
			using (AScheduleOnARealJournal journal = AJournalThatLivedTheWholeStory())
			{
				Assert.That(journal.Api.HasAnythingDue(), Is.True);
				Assert.That(journal.Api.CountDue(), Is.GreaterThan(0));
				Assert.That(journal.Api.MissedCount(), Is.GreaterThan(0));
				Assert.That(journal.Api.DeferralsUsed("FluidChange"), Is.EqualTo(1));
			}
		}

		// LA MITAD QUE SOLO EXISTE EN EL NIVEL B: cerrar el proceso y volver a abrir sobre el mismo
		// journal contesta lo mismo. Todo lo que sobrevive a esto vino del journal.
		[Test]
		public void TheStoryReplayedFromTheJournalAnswersExactlyTheSame()
		{
			using (AScheduleOnARealJournal journal = AJournalThatLivedTheWholeStory())
			{
				string live = journal.EveryDueStateItCanAnswer();

				journal.CloseAndReopenFromTheJournal();

				Assert.That(journal.EveryDueStateItCanAnswer(), Is.EqualTo(live),
					"E14: el mismo journal de valores recibidos se reproduce identico. Si la agenda "
					+ "fuera un campo de C# en vez de una global de raiz del DSL, esto reventaria en "
					+ "la primera lectura despues de rehidratar.");
			}
		}

		// LAS DOS PRUEBAS QUE DEFIENDEN AL APAREJO, y estan aca porque listar los defectos a
		// introducir las encontro faltando: sin ellas, todo lo de arriba pasa en verde con un
		// aparejo que no hace nada.
		//
		// La primera: que reabrir REABRA. Si CloseAndReopenFromTheJournal no cerrara nada, la
		// comparacion de arriba seria "leer dos veces da lo mismo", que es cierto siempre. La senal
		// de que el actor se reconstruyo de verdad es que el journal CRECIO: OnHydrated corre en
		// cada hidratacion y el PerformCommand que envuelve al upgrade deja una entrada aunque el
		// upgrade ya este aplicado y no haga nada.
		//
		// Ese hecho tambien es el que obliga a T9 a dejar el OnHydrated del servidor MCP vacio: con
		// la semilla puesta, cada pregunta de la IA escribiria en el journal de la persona.
		[Test]
		public void ReopeningReallyRebuildsTheActorFromTheJournal()
		{
			using (AScheduleOnARealJournal journal = AJournalThatLivedTheWholeStory())
			{
				string before = TheBytesOf(journal.Folder);

				journal.CloseAndReopenFromTheJournal();

				Assert.That(TheBytesOf(journal.Folder), Is.Not.EqualTo(before),
					"Cada arranque deja la entrada de la semilla. Si esto no creciera, el aparejo "
					+ "no habria cerrado nada y las pruebas de replay estarian comparando la misma "
					+ "instancia consigo misma.");
			}
		}

		// La segunda: que la comparacion COMPARE. Una que devolviera siempre lo mismo dejaria en
		// verde todas las de replay para siempre.
		[Test]
		public void TheComparisonUsedByThisFixtureCanTellTwoSchedulesApart()
		{
			using (AScheduleOnARealJournal lived = AJournalThatLivedTheWholeStory())
			using (AScheduleOnARealJournal bare =
				new AScheduleOnARealJournal(TheVin, "Standard"))
			{
				Assert.That(
					bare.EveryDueStateItCanAnswer(),
					Is.Not.EqualTo(lived.EveryDueStateItCanAnswer()));
			}
		}

		// EL DESEMPATE DEL MISMO SEGUNDO, DESPUES DE REABRIR. El journal guarda el instante como
		// texto con granularidad de SEGUNDOS (spike S4), asi que dos lecturas observadas dentro del
		// mismo segundo no se distinguen por su instante: las ordena el ORDEN DE LLEGADA -- y ese
		// orden es lo unico que queda escrito. RecordReading lo implementa por posicion de
		// insercion.
		//
		// El nivel A ya lo afirma (TwoReadingsObservedInTheSameInstantKeepTheirArrivalOrder), pero
		// ahi el agregado vive en memoria y el orden de insercion se conserva solo. Lo que S4
		// afirma es sobre el REPLAY, y eso ninguna prueba del nivel A lo puede tocar.
		//
		// SE VUELVE A EVALUAR DESPUES DE REABRIR en vez de solo releer el estado. Releerlo probaria
		// que el DueState guardado se rehidrato; volver a evaluar prueba que las LECTURAS volvieron
		// en el mismo orden, que es la afirmacion. La segunda evaluacion va a un instante
		// ESTRICTAMENTE POSTERIOR: la guarda de cadencia rechaza uno ya evaluado, y un rechazo no
		// escribe nada (spike S3).
		//
		// LAS DOS LECTURAS VAN EN UNIDADES DISTINTAS, y no es adorno: es la unica forma de que el
		// par se pueda recibir en los DOS ordenes. Dentro de una misma unidad la guarda de la linea
		// de observacion rechaza el orden invertido -- un odometro no va para atras -- asi que la
		// segunda en llegar tiene que ser siempre la mayor y el orden de llegada no se ve en el
		// estado. Dos unidades son dos lineas de tiempo que no se juzgan entre si (OINV-21): los
		// dos ordenes entran, y cual quedo ULTIMA se ve, porque la del plan mide y la otra suspende
		// la distancia.
		[Test]
		public void TheSameSecondTieBreakSurvivesAReopen()
		{
			using (AScheduleOnARealJournal journal = AScheduleWhereThePlanUnitArrivedLast())
			{
				string live = journal.Api.ReadDueState(TheOperationMeasuredByDistance);
				string why = journal.Api.ReadDueReason(TheOperationMeasuredByDistance, TheLanguage.OfTheRecord);

				Assert.That(live, Is.EqualTo("Due"),
					"La ultima en llegar dentro del segundo es la que mide: 40000 sobre un ancla de "
					+ "30000 con limite de 10000. Si mandara la primera, la distancia quedaria "
					+ "suspendida por unidad y esto seria NotDue.");

				Assert.That(TheDueStateAfterReplayingTheJournal(journal), Is.EqualTo(live),
					"S4: el instante llega al journal truncado al segundo, asi que el desempate que "
					+ "sobrevive es el ORDEN. Si el replay devolviera las dos lecturas al reves, "
					+ "esta evaluacion nueva contestaria otra cosa.");
				Assert.That(journal.Api.ReadDueReason(TheOperationMeasuredByDistance,
					TheLanguage.OfTheRecord), Is.EqualTo(why),
					"Y por el mismo motivo, no por otro que de la misma respuesta.");
			}
		}

		// EL CONTROL, y sin el la de arriba no prueba nada: que un estado sobreviva al replay se
		// cumple solo si el orden de llegada no decide NADA. Aca entra el mismo par al reves y
		// tiene que contestar DISTINTO.
		//
		// El dia que alguien le quite el desempate a RecordReading -- ordenar el mismo segundo por
		// valor, por ejemplo, en vez de por llegada -- los dos ordenes colapsan a la misma
		// respuesta y esta prueba se pone roja. La de arriba seguiria verde: para eso esta esta.
		[Test]
		public void TheReversedArrivalOrderInTheSameSecondAnswersDifferently()
		{
			using (AScheduleOnARealJournal asItArrived = AScheduleWhereThePlanUnitArrivedLast())
			using (AScheduleOnARealJournal reversed = AScheduleWhereTheOtherUnitArrivedLast())
			{
				Assert.That(
					TheDueStateAfterReplayingTheJournal(reversed),
					Is.Not.EqualTo(TheDueStateAfterReplayingTheJournal(asItArrived)),
					"Las dos lecturas son las mismas y el segundo observado es el mismo: lo unico "
					+ "que cambia es cual llego primero. Si las dos contestan igual, el orden de "
					+ "llegada dejo de decidir y el desempate del mismo segundo no existe.");
			}
		}

		// LA CADENCIA DE C6, medida contra el actor de verdad y no supuesta. Re-evaluar un instante
		// ya evaluado se rechaza bajo el read lock y NO deja entrada: el journal queda byte a byte
		// como estaba.
		[Test]
		public void ARejectedEvaluationLeavesTheJournalByteIdentical()
		{
			using (AScheduleOnARealJournal journal = AJournalThatLivedTheWholeStory())
			{
				string before = TheBytesOf(journal.Folder);
				string state = journal.EveryDueStateItCanAnswer();

				EvaluationOutcome again = journal.Api.Evaluate(TheLastEvaluation);

				Assert.That(again.WasEvaluated, Is.False);
				Assert.That(again.Rejection, Is.Not.Empty,
					"El rechazo se REPORTA. Un rechazo mudo seria indistinguible de una evaluacion "
					+ "que no movio nada.");
				Assert.That(again.TransitionCount, Is.EqualTo(0));

				Assert.That(TheBytesOf(journal.Folder), Is.EqualTo(before),
					"Un PerformCheckThenCommand rechazado no escribe. Con un PerformCommand pelado "
					+ "cada apertura de la app dejaria una entrada que no cambio nada.");
				Assert.That(journal.EveryDueStateItCanAnswer(), Is.EqualTo(state),
					"Y no movio el estado: el punto de control monotono sigue donde estaba.");
			}
		}

		// El otro lado de la misma guarda: un instante POSTERIOR si pasa, y deja entrada.
		[Test]
		public void AnEvaluationOfALaterInstantIsAcceptedAndWrites()
		{
			using (AScheduleOnARealJournal journal = AJournalThatLivedTheWholeStory())
			{
				string before = TheBytesOf(journal.Folder);

				EvaluationOutcome later = journal.Api.Evaluate(TheLastEvaluation.AddDays(1));

				Assert.That(later.WasEvaluated, Is.True);
				Assert.That(TheBytesOf(journal.Folder), Is.Not.EqualTo(before));
				Assert.That(
					journal.Api.LastEvaluatedAt(), Is.EqualTo(TheLastEvaluation.AddDays(1)));
			}
		}

		// La guarda tambien rechaza un instante ANTERIOR, y ahi importa por partida doble: el
		// dominio revienta si se le pide re-evaluar el pasado (T5), asi que sin la guarda del Check
		// la excepcion saldria DESPUES de que el motor escribio la entrada -- y esa entrada
		// quedaria para siempre, haciendo loguear un fallo de rehidratacion en cada arranque.
		[Test]
		public void AnEvaluationOfAnEarlierInstantIsRefusedBeforeAnythingIsWritten()
		{
			using (AScheduleOnARealJournal journal = AJournalThatLivedTheWholeStory())
			{
				string before = TheBytesOf(journal.Folder);

				EvaluationOutcome backwards = journal.Api.Evaluate(TheLastEvaluation.AddDays(-1));

				Assert.That(backwards.WasEvaluated, Is.False);
				Assert.That(TheBytesOf(journal.Folder), Is.EqualTo(before));

				journal.CloseAndReopenFromTheJournal();

				Assert.That(journal.Api.LastEvaluatedAt(), Is.EqualTo(TheLastEvaluation),
					"Y el journal sigue rehidratando limpio, que es lo que la entrada envenenada "
					+ "habria costado.");
			}
		}

		// EL JOURNAL MISMO entre dos corridas de la misma secuencia: el segundo programa que
		// escriben estos comandos es el MISMO programa, no uno equivalente.
		//
		// SE COMPARA EL PROGRAMA Y NO LOS BYTES, y la diferencia no es una comodidad: el journal no
		// puede ser byte a byte identico entre dos corridas y no es un defecto que no lo sea. Cada
		// entrada estampa el instante en que ocurrio -- es de donde el motor reinyecta @Now al
		// reproducir -- asi que dos corridas separadas en el tiempo difieren en esos ticks y en el
		// checksum que los cubre. MEDIDO sobre estas dos corridas: 1489 bytes cada una, 72 que
		// difieren, todos en tramos de tres o cuatro bytes pegados a cada entrada; index.bin y
		// meta.bin salen identicos. Lo que tiene que coincidir -- y coincide -- es el TEXTO de cada
		// sentencia y sus argumentos.
		//
		// Que eso coincida es lo que prueba que ningun valor viajo interpolado ni se genero en C#:
		// un Guid.NewGuid() o un DateTime.Now del lado de la app saldria justo aca.
		[Test]
		public void TheSecondProgramIsIdenticalAcrossTwoRunsOfTheSameSequence()
		{
			using (AScheduleOnARealJournal first = AJournalThatLivedTheWholeStory())
			using (AScheduleOnARealJournal second = AJournalThatLivedTheWholeStory())
			{
				Assert.That(
					TheSecondProgramOf(second.Folder), Is.EqualTo(TheSecondProgramOf(first.Folder)));
			}
		}

		// Y la mitad que hace falsificable a la de arriba: el programa que se comparo dice de
		// verdad lo que se recibio. Sin esto, un extractor que devolviera siempre lo mismo -- o
		// nada -- dejaria la comparacion verde para siempre.
		[Test]
		public void TheSecondProgramCarriesTheValuesAsArgumentsAndTheCodesAsNames()
		{
			using (AScheduleOnARealJournal journal = AJournalThatLivedTheWholeStory())
			{
				string program = TheSecondProgramOf(journal.Folder);

				Assert.That(program, Does.Contain("schedule = MaintenanceSchedule(vin,usage)"),
					"La semilla quedo como sentencia con PARAMETROS FORMALES. Si el vin se hubiera "
					+ "interpolado, el journal diria MaintenanceSchedule('1HGCM...') y el dato "
					+ "habria dejado de ser un dato.");
				Assert.That(program, Does.Contain("'1HGCM82633A004352','Standard'"),
					"Y los valores viajan aparte, como argumentos de esa sentencia.");
				Assert.That(program, Does.Contain("'FluidChange'"),
					"Un codigo cruza como NOMBRE y no como numero (C12): renumerar el enum no puede "
					+ "reinterpretar una entrada ya escrita.");
				Assert.That(program, Does.Not.Contain("FluidChange,"),
					"Y no aparece suelto en el texto de ninguna sentencia: eso seria interpolacion.");
				Assert.That(
					program,
					Does.Contain(
						"profile.PublishWarrantyEnvelope(months, distance, unit, publishedAt)"),
					"El sobre tambien quedo como sentencia con PARAMETROS FORMALES: los dos limites "
					+ "son datos y no texto de la sentencia.");
				Assert.That(
					program,
					Does.Contain("profile.RecordDelivery(deliveredAt, reading, unit)"),
					"Y la entrega igual.");
				Assert.That(program, Does.Contain("36,60000,'UnitB'"),
					"Los dos limites viajan APARTE, como argumentos, y la unidad del sobre cruza como "
					+ "NOMBRE. Va en UnitB a proposito: UnitA es el primer miembro del enum, asi que "
					+ "un codigo que no cruzara daria el mismo texto que uno que si.");
				Assert.That(program, Does.Not.Contain("PublishWarrantyEnvelope(36"),
					"Y ningun limite aparece dentro del texto de la sentencia: eso seria "
					+ "interpolacion, y renumerar o reformatear el valor reinterpretaria una entrada "
					+ "ya escrita.");

				Assert.That(program, Does.Contain("'Telemetry'"),
					"La PROCEDENCIA de cada lectura tambien queda escrita. Ninguna ley de este "
					+ "slice la consulta -- la confianza por procedencia es de OINV-54 y vive en "
					+ "OS3 -- asi que el journal es lo unico que la defiende hoy: una procedencia "
					+ "sustituida en la puerta seria un hecho falso esperando a que alguien lo lea.");
			}
		}

		// ===== PRUEBA 19 DE §11 DEL BRIEF: LAS LECTURAS COMPUESTAS ========================

		// LAS SEIS COMPUESTAS CONTESTAN LO MISMO EN DOS CORRIDAS DE LA MISMA SECUENCIA. Es la misma
		// promesa que las de arriba y sobre una superficie mas ancha: cada una junta hechos de DOS o
		// TRES globales de raiz en un solo script, asi que un valor que se hubiera generado en C# --
		// un reloj, un default -- saldria justo aca y en ninguna otra parte.
		[Test]
		public void TwoRunsOfTheSameStoryAgreeOnEveryComposedAnswer()
		{
			using (AScheduleOnARealJournal first = AJournalThatLivedTheWholeStory())
			using (AScheduleOnARealJournal second = AJournalThatLivedTheWholeStory())
			{
				Assert.That(
					EveryComposedAnswerOf(second), Is.EqualTo(EveryComposedAnswerOf(first)));
			}
		}

		// Y REPRODUCIDAS DESDE EL JOURNAL, lo mismo. Los siete hechos que las compuestas usan estan
		// repartidos entre tres globales de raiz; si alguno viviera en un campo de C# en vez de en el
		// journal, la primera lectura despues de rehidratar contestaria otra cosa.
		[Test]
		public void TheComposedAnswersReplayedFromTheJournalAreExactlyTheSame()
		{
			using (AScheduleOnARealJournal journal = AJournalThatLivedTheWholeStory())
			{
				string live = EveryComposedAnswerOf(journal);

				journal.CloseAndReopenFromTheJournal();

				Assert.That(EveryComposedAnswerOf(journal), Is.EqualTo(live));
			}
		}

		// EL INSTANTE DE UNA LECTURA NO ES MONOTONO, y esto lo mide. Dos preguntas separadas por un
		// segundo contestan las dos: una consulta no escribe ninguna entrada, asi que no hay nada que
		// marcar ni nada que proteger, y una guarda de monotonia aqui rechazaria una pregunta
		// legitima. Es la obligacion CONTRARIA a la del instante de un comando, y las dos salen del
		// mismo reloj.
		[Test]
		public void TwoComposedReadsASecondApartBothAnswer()
		{
			using (AScheduleOnARealJournal journal = AJournalThatLivedTheWholeStory())
			{
				DateTime asked = TheLastEvaluation.AddDays(10);

				string first = journal.Api.DescribeWarrantyEnvelope(asked, TheLanguage.OfTheRecord);

				Assert.That(first, Is.Not.Empty);
				Assert.That(
					journal.Api.DescribeWarrantyEnvelope(
						asked.AddSeconds(1), TheLanguage.OfTheRecord),
					Is.Not.Empty);

				Assert.That(
					journal.Api.DescribeWarrantyEnvelope(
						asked, TheLanguage.OfTheRecord), Is.EqualTo(first),
					"Y OTRA VEZ EL PRIMERO, DESPUES DEL POSTERIOR, Y CONTESTA LO MISMO. Una guarda de "
					+ "monotonia colada aqui dejaria pasar los dos primeros y rechazaria este; un "
					+ "punto de control movido por una consulta lo haria contestar otra cosa.");

				Assert.That(
					journal.Api.DescribeWarrantyEnvelope(
						asked.AddSeconds(-1), TheLanguage.OfTheRecord), Is.Not.Empty,
					"Y un instante ANTERIOR a todos los ya preguntados tambien contesta.");
			}
		}

		// Y LAS COMPUESTAS NO ESCRIBEN NINGUNA ENTRADA, medido sobre la narrativa entera. Es lo que
		// hace comparables a las dos pruebas de arriba: si preguntaran escribiendo, la segunda
		// corrida partiria de un journal distinto de la primera.
		[Test]
		public void AskingEveryComposedQuestionLeavesTheJournalByteIdentical()
		{
			using (AScheduleOnARealJournal journal = AJournalThatLivedTheWholeStory())
			{
				string before = TheBytesOf(journal.Folder);

				EveryComposedAnswerOf(journal);

				Assert.That(TheBytesOf(journal.Folder), Is.EqualTo(before));
			}
		}

		// Las seis compuestas en una sola cadena comparable, con el nombre de cada una al lado de su
		// respuesta (C60): una lectura que contestara siempre lo mismo dejaria seis renglones
		// iguales, y una comparacion que solo mirara que la cadena no viene vacia no podria verlo.
		//
		// LAS GUARDADAS SE PREGUNTAN DETRAS DE SU PRESENCIA, que es justo lo que una caratula tiene
		// que hacer: la cadena guarda el rechazo como respuesta en vez de reventar, para que los dos
		// lados de la comparacion se puedan construir siempre.
		//
		// EN ESTA NARRATIVA EL SOBRE ESTA EN UnitB Y LAS LECTURAS EN UnitA, asi que la respuesta del
		// sobre queda SUSPENDIDA por desajuste de unidades -- y eso es lo que hace fuerte a esta
		// comparacion en vez de debil: la suspension existe PORQUE el codigo de la unidad cruzo de
		// una global de raiz a otra como valor tipado. Si dejara de cruzar, las dos unidades
		// calzarian, la frase traeria un numero y las dos corridas dejarian de coincidir con lo
		// reproducido.
		private static string EveryComposedAnswerOf(AScheduleOnARealJournal journal)
		{
			DateTime asked = TheLastEvaluation.AddDays(10);

			bool theRecordHasTheThreeFacts = journal.Api.IsWarrantyEnvelopePublished()
				&& journal.Api.HasDeliveryBeenRecorded()
				&& journal.Api.HasAnOdometerReading(asked);

			return "canTheEnvelope=" + journal.Api.CanTheEnvelopeBeEvaluated(asked)
				+ "\nenvelopeStanding="
				+ (journal.Api.CanTheEnvelopeBeEvaluated(asked)
					? journal.Api.ReadEnvelopeStanding(asked)
					: "refused")
				+ "\nenvelope=" + journal.Api.DescribeWarrantyEnvelope(
					asked, TheLanguage.OfTheRecord)
				+ "\ncanCoverage="
				+ journal.Api.CanCoverageBeEvaluatedForThisVehicle("POWERTRAIN", asked)
				+ "\ncoverageStanding="
				+ (journal.Api.CanCoverageBeEvaluatedForThisVehicle("POWERTRAIN", asked)
					? journal.Api.ReadCoverageStandingForThisVehicle("POWERTRAIN", asked)
					: "refused")
				+ "\ncoverage="
				+ (theRecordHasTheThreeFacts
					? journal.Api.DescribeCoverageForThisVehicle("POWERTRAIN", asked)
					: "refused");
		}

		private static AScheduleOnARealJournal AJournalThatLivedTheWholeStory()
		{
			AScheduleOnARealJournal journal = new AScheduleOnARealJournal(TheVin, "Standard");

			journal.Api.SeedPlanOperation(
				"FluidChange", "TimeOrDistance", TwelveMonthsInDays, TenThousandUnits, "UnitA",
				true, false,
				Delivery, OdometerAtDelivery, "UnitA");

			journal.Api.SeedPlanOperation(
				"FilterChange", "DistanceOnly", 0, 9000, "UnitA",
				true, false,
				Delivery, OdometerAtDelivery, "UnitA");

			journal.Api.SeedPlanOperation(
				"Inspection", "ConditionOnly", 0, 0, "UnitA",
				true, false,
				Delivery, OdometerAtDelivery, "UnitA");

			journal.Api.AddOwnerOperation(
				"Diagnostic", "TimeOnly", TwelveMonthsInDays, 0, "UnitA",
				Delivery, OdometerAtDelivery, "UnitA");

			foreach (KeyValuePair<DateTime, int> reading in TheReadingsInTheOrderTheyArrived())
				journal.Api.RecordReading(reading.Value, "UnitA", reading.Key, "Telemetry");

			journal.Api.Evaluate(new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc));
			journal.Api.DeferOperation(
				"FluidChange", "Cost", new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc));

			journal.Api.DeclareUsageProfile(
				"SevereDuty", new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc));
			journal.Api.RecordConditionThreshold(
				"Inspection", new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc));

			journal.Api.Evaluate(new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc));

			journal.Api.AdoptPlanRevision(
				"R2", new DateTime(2026, 2, 20, 0, 0, 0, DateTimeKind.Utc));
			journal.Api.Evaluate(TheLastEvaluation);

			// LOS DOS COMANDOS DEL PERFIL, que T4 agrego. Entran a la narrativa por lo que este
			// fixture defiende: que dos corridas de la MISMA secuencia escriban el MISMO programa, y
			// que ningun valor viaje interpolado en el texto de una sentencia.
			//
			// EN UnitB Y NO EN UnitA (C60), aunque toda la agenda de arriba este en UnitA: UnitA es el
			// primer miembro del enum, y un codigo que no cruzara -- o que cruzara como cero -- daria
			// el mismo texto. Con UnitB, el programa dice 'UnitB' o el renglon de abajo se pone rojo.
			journal.Api.PublishWarrantyEnvelope(
				36, 60000, "UnitB", new DateTime(2023, 11, 15, 0, 0, 0, DateTimeKind.Utc));

			journal.Api.RecordDelivery(Delivery, OdometerAtDelivery, "UnitB");

			return journal;
		}

		// Llegan DESORDENADAS a proposito: la de diciembre entra despues de la de enero.
		private static IEnumerable<KeyValuePair<DateTime, int>> TheReadingsInTheOrderTheyArrived()
		{
			yield return new KeyValuePair<DateTime, int>(
				new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc), 40000);
			yield return new KeyValuePair<DateTime, int>(
				new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc), 35000);
			yield return new KeyValuePair<DateTime, int>(
				new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc), 41000);
			yield return new KeyValuePair<DateTime, int>(
				new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc), 42000);
		}

		// Las dos lecturas del mismo segundo, con la del PLAN llegando ultima. La telemetria del
		// vehiculo y el numero que tecleo la persona caen dentro del mismo segundo: no es un caso
		// de laboratorio, es lo que S4 vuelve indistinguible.
		private static AScheduleOnARealJournal AScheduleWhereThePlanUnitArrivedLast()
		{
			AScheduleOnARealJournal journal = AScheduleWithOneOperationMeasuredByDistance();

			journal.Api.RecordReading(
				TheReadingInTheOtherUnit, "UnitB", TheSameSecond, "HumanEntry");
			journal.Api.RecordReading(
				TheReadingInThePlanUnit, "UnitA", TheSameSecond, "Telemetry");

			journal.Api.Evaluate(TheEvaluationBeforeTheReopen);

			return journal;
		}

		// El mismo par, al reves. Es el control.
		private static AScheduleOnARealJournal AScheduleWhereTheOtherUnitArrivedLast()
		{
			AScheduleOnARealJournal journal = AScheduleWithOneOperationMeasuredByDistance();

			journal.Api.RecordReading(
				TheReadingInThePlanUnit, "UnitA", TheSameSecond, "Telemetry");
			journal.Api.RecordReading(
				TheReadingInTheOtherUnit, "UnitB", TheSameSecond, "HumanEntry");

			journal.Api.Evaluate(TheEvaluationBeforeTheReopen);

			return journal;
		}

		private static AScheduleOnARealJournal AScheduleWithOneOperationMeasuredByDistance()
		{
			AScheduleOnARealJournal journal = new AScheduleOnARealJournal(TheVin, "Standard");

			journal.Api.SeedPlanOperation(
				TheOperationMeasuredByDistance, "DistanceOnly", 0, TenThousandUnits, "UnitA",
				true, false,
				Delivery, OdometerAtDelivery, "UnitA");

			return journal;
		}

		// Cierra, reabre y VUELVE A EVALUAR. Que la evaluacion nueva haya pasado la guarda se
		// afirma aca: rechazada, ReadDueState devolveria el estado de antes de reabrir y las dos
		// pruebas de arriba pasarian sin haber evaluado nada despues del replay.
		private static string TheDueStateAfterReplayingTheJournal(AScheduleOnARealJournal journal)
		{
			journal.CloseAndReopenFromTheJournal();

			EvaluationOutcome again = journal.Api.Evaluate(TheEvaluationAfterTheReopen);

			Assert.That(again.WasEvaluated, Is.True,
				"El instante es posterior al ya evaluado, asi que la guarda tiene que dejarlo "
				+ "pasar. Si lo rechaza, lo que se esta leyendo despues es el estado viejo.");

			return journal.Api.ReadDueState(TheOperationMeasuredByDistance);
		}

		// El arbol entero del journal reducido a una cadena: nombre relativo mas hash del
		// contenido, en orden. Comparar hashes en vez de bytes hace legible el rojo -- dice QUE
		// archivo cambio, no que un arreglo de nueve mil bytes difiere.
		private static string TheBytesOf(string folder)
		{
			List<string> files = new List<string>(
				Directory.GetFiles(folder, "*", SearchOption.AllDirectories));
			files.Sort(StringComparer.Ordinal);

			StringBuilder tree = new StringBuilder();

			foreach (string file in files)
			{
				// El actor tiene el journal abierto mientras vive, asi que hay que pedir el archivo
				// COMPARTIDO. Cerrar el actor para poder mirarlo cambiaria lo que se esta midiendo.
				using (FileStream open = new FileStream(
					file, FileMode.Open, FileAccess.Read,
					FileShare.ReadWrite | FileShare.Delete))
				{
					byte[] content = new byte[open.Length];
					open.ReadExactly(content, 0, content.Length);

					tree.Append(Path.GetRelativePath(folder, file).Replace('\\', '/'));
					tree.Append(' ');
					tree.Append(content.Length);
					tree.Append(' ');
					tree.AppendLine(Convert.ToHexString(MD5.HashData(content)));
				}
			}

			return tree.ToString();
		}

		// El SEGUNDO PROGRAMA que el journal guarda: cada define, cada invoke y cada blob de
		// argumentos, en orden. Se saca leyendo los tramos imprimibles del binario, que es
		// exactamente lo que el motor escribe como texto -- los ticks de cada entrada y su checksum
		// son binarios y no forman tramos de este largo.
		private static string TheSecondProgramOf(string folder)
		{
			StringBuilder program = new StringBuilder();

			List<string> files = new List<string>(
				Directory.GetFiles(folder, "*", SearchOption.AllDirectories));
			files.Sort(StringComparer.Ordinal);

			foreach (string file in files)
			{
				using (FileStream open = new FileStream(
					file, FileMode.Open, FileAccess.Read,
					FileShare.ReadWrite | FileShare.Delete))
				{
					byte[] content = new byte[open.Length];
					open.ReadExactly(content, 0, content.Length);

					StringBuilder run = new StringBuilder();

					foreach (byte each in content)
					{
						if (each >= 0x20 && each <= 0x7E) { run.Append((char)each); continue; }

						if (run.Length >= 6) program.AppendLine(run.ToString());

						run.Clear();
					}

					if (run.Length >= 6) program.AppendLine(run.ToString());
				}
			}

			return program.ToString();
		}
	}
}
