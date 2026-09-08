using System;
using System.IO;
using System.Text;
using Choreography.Theater;
using Concesionario.App;
using NUnit.Framework;
using Puppeteer;

namespace Concesionario.Dominio.Pruebas
{
	// LA RUTA DE DOS ACTOS, POR LA PUERTA Y SOBRE UN JOURNAL DE VERDAD. Es la prueba 1 y la 3 de §11
	// del brief en el nivel B: una entrada registrada y despues la entrega, con los dias que la
	// sucursal dice que cubrio.
	//
	// EL NIVEL A YA FIJA LAS LEYES -- AStayIsAnIntervalAndNotAnEvent, AnOpenStayIsANotYetAndNotAnAbsence
	// y BothEndsInclusive las tienen. Lo que ninguna prueba del nivel A puede tocar es lo que esta
	// prueba mide: que los DOS SCRIPTS del DSL parseen, aten sus parametros y corran, que los dias
	// crucen como LISTA y no como un literal, y que todo eso sobreviva al replay desde el journal.
	// Es la obligacion S5 -- un error de tipeo en un script no se descubre al compilar -- aplicada a
	// los dos primeros comandos de la quinta global.
	//
	// LOS DIAS SE AFIRMAN POR VALOR Y EL TERCERO INDIVIDUALMENTE, que es L3: la respuesta son los
	// dias y no su cuenta, y una cuenta de tres se satisface igual con los tres dias en desorden.
	//
	// Y EL JOURNAL SE LEE COMO TEXTO. PROBE 1 midio que un string[] cruza como comando y que el
	// journal lo guarda como LISTA -- {'2026-06-10','2026-06-11','2026-06-12'} -- y no como un
	// literal separado por espacios. Aquella medicion fue contra un sujeto de sonda; esta es contra
	// el ShopStay de verdad, por la puerta de verdad. El journal lo lee gente y se reproduce para
	// siempre: la forma que dice lo que lleva no es un detalle de gusto.
	[TestFixture]
	public sealed class AnEntryAndACollectionArriveRecordedThroughTheDoor
	{
		private const string TheVin = "1FTFW1ET5DFC10312";
		private const string TheModel = "YARIS-2019";
		private const string TheBranch = "Taller Curridabat";
		private const string TheSubject = "Cambio de aceite";

		private static readonly DateTime Entered =
			new DateTime(2026, 6, 10, 8, 30, 0, DateTimeKind.Utc);

		private static readonly DateTime Collected =
			new DateTime(2026, 6, 12, 16, 45, 0, DateTimeKind.Utc);

		private static readonly string[] TheDaysItCovered =
			{ "2026-06-10", "2026-06-11", "2026-06-12" };

		private const string TheYearFrom = "2026-01-01";
		private const string TheYearTo = "2026-12-31";

		private string folder;
		private DealershipCarApp app;
		private DealershipCarAPI api;

		[SetUp]
		public void Setup()
		{
			folder = ATemporaryFolder();
			Open();
		}

		[TearDown]
		public void TearDown()
		{
			Close();
			Sweep(folder);
		}

		// PRUEBA 1 DEL BRIEF, POR LA PUERTA: entra y no sale. El vehiculo esta adentro, la estadia
		// abierta lo dice, y NO SE INVENTA NINGUNA SALIDA -- ni un centinela ni un dia.
		[Test]
		public void AnEntryRecordedLeavesTheVehicleInsideAndInventsNoExit()
		{
			api.RecordShopEntry(TheBranch, TheSubject, Entered);

			Assert.That(api.IsInsideTheShopAt(Entered), Is.True,
				"En el instante mismo de la entrada ya esta adentro: los dos extremos son inclusivos.");
			Assert.That(api.IsInsideTheShopAt(Collected), Is.True,
				"Y sigue adentro despues, porque nadie ha registrado que lo entregaran.");

			string says = api.DescribeTheStayAt(Collected);

			Assert.That(says, Does.Contain(TheBranch), "La frase dice DONDE esta.");
			Assert.That(says, Does.Contain("2026-06-10 08:30:00"),
				"Y desde cuando, como INSTANTE con su hora: un instante nunca se dice como un dia de "
				+ "calendario a secas, porque sacarle el dia pide una zona que este registro no tiene.");
			Assert.That(says, Does.Not.Contain("0001"),
				"Y no hay salida inventada: ni el instante minimo...");
			Assert.That(says, Does.Not.Contain("9999"), "...ni el maximo.");
		}

		// PRUEBA 3 DEL BRIEF, POR LA PUERTA: registrada la entrega, la estadia cierra y LOS DIAS SE
		// PUEDEN CONTESTAR. Se afirman por valor y el tercero individualmente (L3).
		[Test]
		public void ACollectionRecordedClosesTheStayAndTheDaysAreAnswerableOneByOne()
		{
			api.RecordShopEntry(TheBranch, TheSubject, Entered);
			api.RecordShopCollection(Collected, TheDaysItCovered);

			string[] days = api.DaysInTheShopBetween(TheYearFrom, TheYearTo);

			Assert.That(days, Has.Length.EqualTo(3));
			Assert.That(days[0], Is.EqualTo("2026-06-10"));
			Assert.That(days[1], Is.EqualTo("2026-06-11"));
			Assert.That(days[2], Is.EqualTo("2026-06-12"),
				"EL TERCER DIA POR VALOR. Una cuenta de tres se satisface igual con los tres dias en "
				+ "desorden, y el desorden es justo lo que la ley de contiguidad mediria mal.");

			Assert.That(api.IsInsideTheShopAt(Collected.AddDays(1)), Is.False,
				"Y ya no esta adentro despues de la salida: la estadia cerro.");
		}

		// LO QUE NINGUNA PRUEBA DEL NIVEL A PUEDE TOCAR: los dos scripts sobreviven al replay. Se
		// cierra el proceso y se vuelve a abrir sobre el MISMO journal, y las respuestas son las
		// MISMAS -- vivas contra reproducidas, que es lo unico que ve una normalizacion del borde
		// (C53).
		[Test]
		public void TheTwoScriptsSurviveTheJournalAndAnswerTheSameAfterAReopening()
		{
			api.RecordShopEntry(TheBranch, TheSubject, Entered);
			api.RecordShopCollection(Collected, TheDaysItCovered);

			string live = api.DescribeTheStayAt(Collected);
			string[] liveDays = api.DaysInTheShopBetween(TheYearFrom, TheYearTo);

			CloseAndReopenFromTheJournal();

			Assert.That(api.DescribeTheStayAt(Collected), Is.EqualTo(live),
				"La misma frase, palabra por palabra. Si el borde de la puerta le hiciera algo al "
				+ "instante que el journal no repite, aca vendria otra.");
			Assert.That(api.DaysInTheShopBetween(TheYearFrom, TheYearTo),
				Is.EqualTo(liveDays));
			Assert.That(AStayReadWithoutTheDoor.Vin(app), Is.EqualTo(TheVin),
				"Y la quinta global sigue siendo la de ESTE vehiculo despues de reproducirla.");
		}

		// PROBE 1 CONTRA EL ShopStay DE VERDAD: el journal guarda los dias como una LISTA de tres
		// literales que el motor delimita, no como una frase que solo el dominio sabe partir.
		//
		// SE LEE CON FileShare.ReadWrite a proposito: el motor no suelta el archivo mientras el
		// proceso vive, y un File.ReadAllText pelado revienta con IOException. Un catch que se lo
		// tragara devolveria "" y dejaria esta afirmacion verde habiendo leido nada.
		[Test]
		public void TheJournalKeepsTheReceivedDaysAsAListAndNotAsOneLiteral()
		{
			api.RecordShopEntry(TheBranch, TheSubject, Entered);
			api.RecordShopCollection(Collected, TheDaysItCovered);

			string journal = TheTextOfTheJournal();

			TestContext.Out.WriteLine("=== EL SEGUNDO PROGRAMA DE LOS DOS ACTOS ===");
			TestContext.Out.WriteLine(journal);

			Assert.That(journal, Does.Contain("stays.RecordEntry"),
				"El primer comando quedo escrito con el nombre del miembro del dominio.");
			Assert.That(journal, Does.Contain("stays.RecordCollection"));

			Assert.That(journal, Does.Contain("days:string[]"),
				"Y el parametro de los dias se declara con su tipo de ELEMENTO. Es lo que separa una "
				+ "lista de una frase: el journal dice lo que lleva.");
			Assert.That(journal, Does.Contain("'2026-06-10','2026-06-11','2026-06-12'"),
				"Los tres dias son TRES literales que el motor delimita. La otra forma -- un solo "
				+ "literal separado por espacios -- funciona igual y no dice nada de lo que lleva; "
				+ "PROBE 1 la retiro por eso.");
		}

		private void Open()
		{
			app = new DealershipCarApp(TheVin, "Standard", TheModel);
			app.ConfigureStorage(DatabaseType.FileSystem, "path=" + folder);
			app.Start();

			api = new DealershipCarAPI(app);
		}

		private void Close()
		{
			if (app == null) return;

			app.Dispose();
			app = null;
			api = null;
		}

		private void CloseAndReopenFromTheJournal()
		{
			Close();
			Open();
		}

		private string TheTextOfTheJournal()
		{
			StringBuilder everything = new StringBuilder();

			foreach (string file in Directory.GetFiles(folder, "*", SearchOption.AllDirectories))
			{
				using (FileStream open = new FileStream(
					file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
				using (StreamReader reader = new StreamReader(open))
				{
					everything.AppendLine(reader.ReadToEnd());
				}
			}

			return everything.ToString();
		}

		private static string ATemporaryFolder()
		{
			string made = Path.Combine(
				AScheduleOnARealJournal.TheRootOfEveryTemporaryJournal,
				Guid.NewGuid().ToString("N"));

			Directory.CreateDirectory(made);

			return made;
		}

		private static void Sweep(string directory)
		{
			try
			{
				if (Directory.Exists(directory)) Directory.Delete(directory, true);
			}
			catch (IOException)
			{
			}
			catch (UnauthorizedAccessException)
			{
			}
		}
	}
}
