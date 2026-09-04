using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using DealershipCar.App;
using DealershipCar.App.Events;
using DealershipCar.Mcp;
using DealershipCar.Mcp.Tools;
using NUnit.Framework;
using Puppets.Portador;

namespace DealershipCar.Dominio.Pruebas
{
	// LA INVARIANTE DE UN SOLO ESCRITOR, que es lo unico que T9 tiene que demostrar.
	//
	// El servidor MCP y la app instalada comparten la carpeta. La app escribe; este proceso solo
	// lee. Si leer escribiera, dos procesos estarian journalizando sobre el mismo actor y la
	// promesa de que el journal reproduce la misma historia dos veces se caeria.
	//
	// SE MIDE POR LA ENTRADA Y POR EL SEGUNDO PROGRAMA, no por el md5 de la carpeta entera, y esa
	// eleccion esta medida y no elegida por comodidad: el motor NO tiene modo de solo lectura.
	// Abrir un journal existente le crea index/index.bin, le reescribe meta.bin, y a la SEPTIMA
	// apertura le escribe tres bytes de CABECERA al archivo del journal -- un punto de control de
	// replay que lleva la cuenta de entradas (28 -> 0x1c, 29 -> 0x1d, 32 -> 0x20, comprobado). Todo
	// eso converge y nada de eso es un HECHO: la cuenta de entradas no se mueve, el largo del
	// archivo no se mueve, y el segundo programa -- cada define, cada invoke y cada blob de
	// argumentos -- queda igual. Ver CONTRADICTIONS.md C30.
	[TestFixture]
	public sealed class TheAssistantOnlyEverReadsTheJournal
	{
		private const string TheVin = "1HGCM82633A004352";

		[Test]
		public void EveryToolTheAssistantCanCallLeavesTheJournalWithoutANewEntry()
		{
			using (AScheduleOnARealJournal journal = AVehicleWithAPlanAndAnEvaluation())
			{
				long entriesBefore = TheEntryCountOf(journal.Folder);
				long lengthBefore = TheJournalLengthOf(journal.Folder);

				DealershipCarTools tools = TheAssistantsToolsOver(journal.Folder);

				// Las tres tools, varias veces, que es como pregunta un asistente de verdad.
				for (int round = 0; round < 4; round++)
				{
					tools.Review();
					tools.HowUpToDate();

					foreach (MaintenanceOperation each in TheOperationKind.All)
						tools.CheckOne(each);
				}

				Assert.That(TheEntryCountOf(journal.Folder), Is.EqualTo(entriesBefore),
					"Una pregunta de la IA dejo una entrada en el journal de la persona. Eso es "
					+ "un segundo escritor sobre el mismo actor, que es exactamente lo que T9 "
					+ "existe para no hacer.");
				Assert.That(TheJournalLengthOf(journal.Folder), Is.EqualTo(lengthBefore),
					"El archivo del journal crecio. Ninguna lectura puede hacerlo crecer.");
			}
		}

		// EL SEGUNDO PROGRAMA ES LO QUE TIENE QUE QUEDAR IGUAL, y es la forma de la afirmacion que
		// C23 dejo establecida: no los bytes de la carpeta -- que el motor toca por su cuenta --
		// sino las sentencias que el actor pronuncio y los valores que llevaron.
		[Test]
		public void TheSecondProgramIsIdenticalAfterTheAssistantHasAsked()
		{
			using (AScheduleOnARealJournal journal = AVehicleWithAPlanAndAnEvaluation())
			{
				string before = TheSecondProgramOf(journal.Folder);

				DealershipCarTools tools = TheAssistantsToolsOver(journal.Folder);
				for (int round = 0; round < 12; round++) tools.Review();

				Assert.That(TheSecondProgramOf(journal.Folder), Is.EqualTo(before),
					"El programa que quedo journalizado cambio despues de doce preguntas. Leer no "
					+ "puede agregar ni una sentencia.");
			}
		}

		// EL GUARDIAN DE LAS DOS DE ARRIBA, Y TIENE QUE SER DISTINTO. Las dos afirman que algo NO
		// cambio; una medicion que no pudiera detectar un cambio las dejaria verdes para siempre --
		// que es la leccion que T6, T7 y T8 aprendieron una vez cada uno. Abrir con el host DE LA
		// APP si escribe: OnHydrated deja la entrada de la semilla en CADA hidratacion, aunque los
		// dos upgrades ya esten aplicados y no hagan nada.
		[Test]
		public void TheAppsOwnHostAddsOneEntryEveryTimeItOpensAndThatIsWhyTheMcpHostIsADifferentOne()
		{
			using (AScheduleOnARealJournal journal = AVehicleWithAPlanAndAnEvaluation())
			{
				long entriesBefore = TheEntryCountOf(journal.Folder);
				string programBefore = TheSecondProgramOf(journal.Folder);

				journal.CloseAndReopenFromTheJournal();

				Assert.That(TheEntryCountOf(journal.Folder), Is.EqualTo(entriesBefore + 1),
					"Reabrir con DealershipCarApp tiene que costar exactamente una entrada. Si no "
					+ "cuesta ninguna, la medicion no distingue leer de escribir y las dos pruebas "
					+ "de arriba no prueban nada.");
				Assert.That(TheSecondProgramOf(journal.Folder), Is.Not.EqualTo(programBefore),
					"Y el segundo programa tiene que CAMBIAR, o el extractor no ve nada.");
			}
		}

		// LA CREDENCIAL DE LA PERSONA YA NO ESTA EN EL JOURNAL, asi que la prueba que vigilaba que
		// ninguna respuesta del asistente la copiara se fue con ella (estaba aca hasta el
		// 2026-09-03). Escribia un token por la puerta, serializaba todas las tools y verificaba
		// que el valor no apareciera en ninguna. Hoy la puerta no tiene con que escribirlo: el
		// token lo sostiene el host fuera del journal y el asistente lee el journal.
		//
		// LO QUE ESA PRUEBA YA NO CUBRE, y conviene decirlo en vez de dar por cerrado el asunto:
		// los journals escritos ANTES de ese cambio SI tienen el token en claro, porque el journal
		// es append-only y no se edita el historial. El asistente lee esas carpetas igual. Lo que
		// separa al token del asistente sigue siendo que ninguna tool lo pida.

		// HASTA DONDE LEYO LA APP LA NARRATIVA DEL CONCESIONARIO, que es la unica pregunta con la que
		// el asistente puede saber si lo que acaba de contestar esta al dia. Las instructions se
		// apoyan en ella: "preguntala dos veces alrededor de un Refresh; si subio, entraron cosas
		// nuevas".
		//
		// SE MIDE CONTRA UNA NARRATIVA DE VERDAD Y CONTRA UNA A MEDIAS, y hacen falta las dos. La
		// historia de la consola nunca ingiere eventos, asi que el punto de control vale CERO ahi y
		// una constante cero pasaria inadvertida -- corrio LIMPIO en la falsificacion de T9. Es la
		// misma forma que T8 encontro tres veces: todo en el guion coincide, asi que cruzarlo no
		// cambia nada.
		[Test]
		public void TheAssistantSeesHowFarTheAppHasReadTheDealershipsNarrative()
		{
			using (var journal = new AScheduleOnARealJournal(ADealershipNarrative.TheVin, "Standard"))
			{
				DealershipCarTools tools = TheAssistantsToolsOver(journal.Folder);

				Assert.That(tools.HowUpToDate().LastEventApplied, Is.EqualTo(0),
					"Nada entro todavia.");

				TheNarrativeUpTo(journal, 4);
				long halfway = tools.HowUpToDate().LastEventApplied;

				Assert.That(halfway, Is.EqualTo(4));

				TheNarrativeUpTo(journal, ADealershipNarrative.TheLastEvent);
				long whole = tools.HowUpToDate().LastEventApplied;

				// NUEVE Y NO DIEZ, que es el numero que ya fija AReceivedEventBlockIsAppliedExactlyOnce:
				// el evento 10 trae una unidad que el canon no escribe y la frontera lo DESCARTA, asi
				// que nunca llega a la puerta y no puede mover el punto de control.
				Assert.That(whole, Is.EqualTo(9L));
				Assert.That(whole, Is.GreaterThan(halfway),
					"Y SUBE. Es lo unico que hace util el campo: el asistente lo pregunta antes y "
					+ "despues de que la persona apriete Refresh.");

				// El tablero trae el mismo numero que la tool dedicada: dos respuestas distintas a la
				// misma pregunta serian peor que ninguna.
				Assert.That(tools.Review().LastEventApplied, Is.EqualTo(whole));
				Assert.That(tools.CheckOne(MaintenanceOperation.FluidChange).LastEventApplied,
					Is.EqualTo(whole));
			}
		}

		// Aplica la narrativa del concesionario hasta ese numero de evento, por la misma puerta y la
		// misma ruta que usa el bucle de refresh.
		private static void TheNarrativeUpTo(AScheduleOnARealJournal journal, long lastEvent)
		{
			foreach (SenalDeEvento signal in ADealershipNarrative.TheWholeStory())
			{
				if (signal.Numero > lastEvent) break;

				IMensajeDeEvento message = DealershipEventRoute.Route(signal);
				if (message != null) journal.Api.Apply(message);
			}
		}

		// LO QUE SE LE OFRECE A LA IA ES UNA LISTA CERRADA DE PREGUNTAS, no una consola de DSL.
		[Test]
		public void TheAssistantIsOfferedThreeToolsAndNoneOfThemIsACommand()
		{
			var offered = new List<string>();

			foreach (System.Reflection.MethodInfo each in typeof(DealershipCarTools).GetMethods())
				foreach (object attribute in each.GetCustomAttributes(true))
					if (attribute.GetType().Name == "McpServerToolAttribute")
						offered.Add((string)attribute.GetType().GetProperty("Name").GetValue(attribute));

			offered.Sort(StringComparer.Ordinal);

			Assert.That(offered, Is.EqualTo(new[]
			{
				"check_one_operation", "how_up_to_date_is_this", "review_my_maintenance",
			}), "Una tool que se agrega sin pensarla es como entra un comando a una superficie de "
				+ "solo lectura. La lista se escribe aca para que agregar una obligue a mirarla.");
		}

		// ------------------------------------------------------ el aparejo

		private static DealershipCarTools TheAssistantsToolsOver(string folder)
		{
			return new DealershipCarTools(new JournalReader(folder, ""));
		}

		private static AScheduleOnARealJournal AVehicleWithAPlanAndAnEvaluation()
		{
			var journal = new AScheduleOnARealJournal(TheVin, "Standard");

			journal.Api.SeedPlanOperation(
				"FluidChange", "TimeOrDistance", 365, 10000, "UnitA", true, false,
				new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc), 12000, "UnitA");
			journal.Api.SeedPlanOperation(
				"Inspection", "ConditionOnly", 0, 0, "UnitA", true, true,
				new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc), 12000, "UnitA");
			journal.Api.RecordReading(
				24000, "UnitA", new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), "Telemetry");
			journal.Api.Evaluate(new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc));

			return journal;
		}

		// LA CUENTA DE ENTRADAS SE LEE ABRIENDO EN SOLO LECTURA, no del actor que la prueba tiene
		// abierto: preguntarle al escritor mediria al escritor. Esto abre como abre la caratula.
		private static long TheEntryCountOf(string folder)
		{
			var reader = new ReadOnlyDealershipCarApp(TheVin);
			try
			{
				reader.ConfigureStorage(Puppeteer.DatabaseType.FileSystem, "path=" + folder);
				reader.Start();

				return reader.Actor.CurrentEntryId;
			}
			finally
			{
				try { reader.Actor.GracefulExit(); } catch { }
			}
		}

		private static long TheJournalLengthOf(string folder)
		{
			long total = 0;

			foreach (string file in Directory.GetFiles(
				Path.Combine(folder, DealershipCarApp.TheActorFor(TheVin), "journal"), "*.bin"))
				total += new FileInfo(file).Length;

			return total;
		}

		// Las sentencias que el actor pronuncio y los valores que llevaron, extraidas del binario:
		// las corridas imprimibles de seis caracteres o mas. Es el mismo extractor que usa
		// TheJournalReplaysTheSameReceivedSequenceTheSame, y por la misma razon (C23).
		//
		// SE SALTEA LA CABECERA DEL ARCHIVO a proposito -- los primeros 32 bytes -- porque es
		// justamente donde el motor deja su punto de control de replay a la septima apertura, y eso
		// no es una sentencia. No hay riesgo de tapar nada: ninguna sentencia empieza ahi.
		private static string TheSecondProgramOf(string folder)
		{
			var program = new StringBuilder();

			var files = new List<string>(
				Directory.GetFiles(folder, "*", SearchOption.AllDirectories));
			files.Sort(StringComparer.Ordinal);

			foreach (string file in files)
			{
				using (var open = new FileStream(
					file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
				{
					byte[] content = new byte[open.Length];
					open.ReadExactly(content, 0, content.Length);

					var run = new StringBuilder();

					for (int at = Math.Min(32, content.Length); at < content.Length; at++)
					{
						byte each = content[at];

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
