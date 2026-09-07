using System;
using System.IO;
using System.Text;
using Choreography.Theater;
using DealershipCar.App;
using NUnit.Framework;
using Puppeteer;

namespace DealershipCar.Dominio.Pruebas
{
	// LA RUTA CORRECTIVA, POR LA PUERTA Y SOBRE UN JOURNAL DE VERDAD. Una estadia que ya termino se
	// registra de UN SOLO ACTO: donde, por que, sus dos instantes y los dias que cubrio. Es la forma
	// que la §9 del brief llama correctiva, la misma que RecordDelivery (DEC-23), y esta para lo que
	// llega despues de haber pasado -- una sucursal que registra el jueves lo que pasó el lunes.
	//
	// LO QUE ESTA PRUEBA MIDE Y EL NIVEL A NO PUEDE: que el TERCER script del DSL parsee, ate sus
	// cinco parametros -- dos instantes, dos textos y una lista -- corra, y sobreviva al replay. Es
	// la obligacion S5 sobre el ultimo de los tres comandos de la quinta global.
	//
	// LA AFIRMACION QUE NADIE ESPERA ES LA SEGUNDA: un acto unico CERRADO mientras hay otra estadia
	// ABIERTA no se rechaza, y la puerta NO lo pre-chequea. La tabla de §10 del brief pone la guarda
	// de "una sola abierta a la vez" en la fila de la ENTRADA y solo L6 en la fila del acto unico:
	// una estadia cerrada no deja una segunda abierta. Un pre-chequeo aca refusaria un registro
	// correctivo legitimo, que es exactamente lo que esta forma existe para permitir.
	//
	// EL NIVEL A YA LO FIJA EN EL DOMINIO -- ACompletedStayRecordedInOneActWhileAnotherIsOpenIsNotRefused
	// vive en ASecondEntryWhileOneIsOpenIsRefusedNamingTheFirst. Lo que se agrega aca es que la
	// PUERTA tampoco lo refusa, que es una decision distinta y esta en otro sitio.
	[TestFixture]
	public sealed class ACompletedStayArrivesInOneAct
	{
		private const string TheVin = "3VWDX7AJ5DM389722";
		private const string TheModel = "YARIS-2019";
		private const string TheBranch = "Taller Escazu";
		private const string TheSubject = "Frenos";
		private const string TheOtherBranch = "Taller Curridabat";
		private const string TheOtherSubject = "Llanta ponchada";

		private static readonly DateTime Entered =
			new DateTime(2026, 3, 2, 7, 15, 0, DateTimeKind.Utc);

		private static readonly DateTime Collected =
			new DateTime(2026, 3, 3, 17, 0, 0, DateTimeKind.Utc);

		private static readonly string[] TheDaysItCovered = { "2026-03-02", "2026-03-03" };

		// LA OTRA ESTADIA ABRE ANTES DEL ACTO UNICO, Y ESE ES EL UNICO MOMENTO EN QUE LA PRUEBA DE
		// ABAJO MIDE ALGO. Con la abierta empezando DESPUES, IsInsideTheShopAt en el instante del
		// acto unico es falso, y un pre-chequeo prohibido pasaria por ahi sin refusar nada: la prueba
		// quedaria verde con el defecto adentro. MEDIDO -- la primera version de este fixture abria la
		// otra estadia dos meses despues y el falsificador no la puso roja.
		private static readonly DateTime TheOtherStayOpenedEarlier =
			new DateTime(2026, 3, 1, 9, 0, 0, DateTimeKind.Utc);

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

		// UN SOLO COMANDO Y LA ESTADIA QUEDA CERRADA CON SUS DOS EXTREMOS Y SUS DOS DIAS. Nadie
		// registro una entrada antes: eso es lo que separa esta forma de la de dos actos.
		[Test]
		public void OneCommandRecordsBothEndsAndTheDaysItCovered()
		{
			api.RecordShopStay(TheBranch, TheSubject, Entered, Collected, TheDaysItCovered);

			Assert.That(api.IsInsideTheShopAt(Entered), Is.True,
				"Adentro en el instante de la entrada.");
			Assert.That(api.IsInsideTheShopAt(Collected), Is.True,
				"Y en el de la salida: los dos extremos son inclusivos.");
			Assert.That(api.IsInsideTheShopAt(Collected.AddSeconds(1)), Is.False,
				"Y un segundo despues ya no: la estadia llego cerrada.");

			string[] days = api.DaysInTheShopBetween(TheYearFrom, TheYearTo);

			Assert.That(days, Has.Length.EqualTo(2));
			Assert.That(days[0], Is.EqualTo("2026-03-02"));
			Assert.That(days[1], Is.EqualTo("2026-03-03"),
				"Los dos dias por valor. DOS es el control que mata una constante: una estadia de tres "
				+ "dias y una de dos no pueden contestar lo mismo.");

			string says = api.DescribeTheStayAt(Collected);

			Assert.That(says, Does.Contain(TheBranch));
			Assert.That(says, Does.Contain("2026-03-02 07:15:00"),
				"Los dos extremos como INSTANTES con su hora.");
			Assert.That(says, Does.Contain("2026-03-03 17:00:00"));
			Assert.That(says, Does.Contain(TheSubject), "Y por que entro, con las palabras que llegaron.");
		}

		// LA PUERTA NO PRE-CHEQUEA ESTA. Con una estadia ABIERTA en curso, un acto unico cerrado en
		// otras fechas entra, y las dos conviven. Un pre-chequeo contra IsInsideTheShopAt aca
		// refusaria el registro correctivo que esta forma existe para permitir.
		[Test]
		public void ACompletedStayIsNotRefusedWhileAnotherStayIsOpen()
		{
			api.RecordShopEntry(TheOtherBranch, TheOtherSubject, TheOtherStayOpenedEarlier);

			Assert.That(api.IsInsideTheShopAt(Entered), Is.True,
				"Hay una estadia abierta que ya cubre el instante del acto unico. ES LA CONDICION QUE "
				+ "HACE QUE ESTA PRUEBA MIDA ALGO: es el unico arreglo en que un pre-chequeo prohibido "
				+ "sobre RecordShopStay refusaria la llamada.");

			Assert.DoesNotThrow(
				() => api.RecordShopStay(TheBranch, TheSubject, Entered, Collected, TheDaysItCovered),
				"Y entra igual: una estadia CERRADA no deja una segunda abierta, asi que la guarda de "
				+ "'una sola abierta a la vez' no la mira. Un pre-chequeo aca refusaria el registro "
				+ "correctivo que esta forma existe para permitir.");

			Assert.That(api.DaysInTheShopBetween(TheYearFrom, TheYearTo),
				Has.Length.EqualTo(2),
				"Y los dias de la cerrada quedaron. La abierta no aporta ninguno: no tiene todavia.");

			Assert.That(api.IsInsideTheShopAt(Collected.AddMonths(3)), Is.True,
				"Y la abierta sigue abierta mucho despues de la salida de la cerrada: registrar la "
				+ "cerrada no la toco.");
		}

		// EL TERCER SCRIPT SOBREVIVE AL REPLAY, y con el sus cinco parametros: dos instantes, dos
		// textos y la lista de dias.
		[Test]
		public void TheOneActScriptSurvivesTheJournalAndAnswersTheSameAfterAReopening()
		{
			api.RecordShopStay(TheBranch, TheSubject, Entered, Collected, TheDaysItCovered);

			string live = api.DescribeTheStayAt(Collected);
			string[] liveDays = api.DaysInTheShopBetween(TheYearFrom, TheYearTo);

			CloseAndReopenFromTheJournal();

			Assert.That(api.DescribeTheStayAt(Collected), Is.EqualTo(live));
			Assert.That(api.DaysInTheShopBetween(TheYearFrom, TheYearTo),
				Is.EqualTo(liveDays));
		}

		// Y EL JOURNAL GUARDA SUS DOS DIAS COMO LISTA, igual que el de dos actos: es el mismo
		// parametro string[] cruzando el mismo borde, y se afirma aca tambien porque es OTRO script.
		[Test]
		public void TheJournalKeepsTheOneActDaysAsAListToo()
		{
			api.RecordShopStay(TheBranch, TheSubject, Entered, Collected, TheDaysItCovered);

			string journal = TheTextOfTheJournal();

			TestContext.Out.WriteLine("=== EL SEGUNDO PROGRAMA DEL ACTO UNICO ===");
			TestContext.Out.WriteLine(journal);

			Assert.That(journal, Does.Contain("stays.RecordStay"));
			Assert.That(journal, Does.Contain("days:string[]"));
			Assert.That(journal, Does.Contain("'2026-03-02','2026-03-03'"));
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
