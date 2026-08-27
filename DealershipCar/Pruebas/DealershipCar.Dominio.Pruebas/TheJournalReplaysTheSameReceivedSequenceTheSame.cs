using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using DealershipCar.App;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
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
				Assert.That(program, Does.Contain("'Telemetry'"),
					"La PROCEDENCIA de cada lectura tambien queda escrita. Ninguna ley de este "
					+ "slice la consulta -- la confianza por procedencia es de OINV-54 y vive en "
					+ "OS3 -- asi que el journal es lo unico que la defiende hoy: una procedencia "
					+ "sustituida en la puerta seria un hecho falso esperando a que alguien lo lea.");
			}
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
