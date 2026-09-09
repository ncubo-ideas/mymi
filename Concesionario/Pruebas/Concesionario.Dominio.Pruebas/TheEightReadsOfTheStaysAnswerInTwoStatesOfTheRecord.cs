using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Concesionario.App;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LAS OCHO LECTURAS DE SH-4, CADA UNA EN LOS DOS ESTADOS DEL REGISTRO. Es la obligacion S5 --
	// un error de tipeo en un script del DSL no se descubre al compilar, se descubre al ejecutarlo, y
	// deja una entrada permanente que hace loguear un fallo de rehidratacion en cada arranque futuro
	// -- aplicada a los ocho scripts que esta tarea le agrega a la puerta.
	//
	// POR QUE DOS ESTADOS Y NO UNO (C61). Un registro sin ninguna estadia contesta la ausencia a casi
	// todo, asi que un barrido tomado ahi compara ocho ausencias que se ponen de acuerdo
	// correctamente sobre nada. El estado que hace que el barrido mida algo es el SEGUNDO: dos
	// estadias, una abierta, con contenido distinto en cada columna.
	//
	// Y CADA RESPUESTA VA CON EL NOMBRE DE SU MIEMBRO AL LADO (C60). Is.Not.Empty prueba que llego
	// una respuesta, nunca que es la de ESE miembro.
	//
	// LOS VALORES ESTAN ELEGIDOS PARA QUE UNA RANURA SIN LIGAR NO PASE. Una cuenta de cero es lo que
	// contesta un int que el motor dejo por defecto, asi que en el segundo estado ninguna de las dos
	// cuentas vale cero, y las dos valen numeros DISTINTOS entre si -- 5 dias en 2 estadias -- para
	// que cruzarlas cambie las dos afirmaciones.
	[TestFixture]
	public sealed class TheEightReadsOfTheStaysAnswerInTwoStatesOfTheRecord
	{
		private const string TheVin = "1FTFW1ET5DFC10312";

		private const string TheYearFrom = "2026-01-01";
		private const string TheYearTo = "2026-12-31";

		// LA ESTADIA CERRADA DE MARZO: tres dias, en una sucursal y con un asunto que no se repiten.
		private const string TheMarchBranch = "Taller Escazú";
		private const string TheMarchSubject = "Frenos";

		private static readonly DateTime MarchEntered =
			new DateTime(2026, 3, 2, 7, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime MarchCollected =
			new DateTime(2026, 3, 4, 17, 0, 0, DateTimeKind.Utc);

		private static readonly string[] TheMarchDays =
			{ "2026-03-02", "2026-03-03", "2026-03-04" };

		// LA ESTADIA CERRADA DE JULIO: DOS dias donde la otra tiene tres, asi que un cruce entre las
		// dos cambia hasta el largo.
		private const string TheJulyBranch = "Taller Curridabat";
		private const string TheJulySubject = "Cambio de aceite";

		private static readonly DateTime JulyEntered =
			new DateTime(2026, 7, 20, 9, 15, 0, DateTimeKind.Utc);

		private static readonly DateTime JulyCollected =
			new DateTime(2026, 7, 21, 16, 0, 0, DateTimeKind.Utc);

		private static readonly string[] TheJulyDays = { "2026-07-20", "2026-07-21" };

		// LA ESTADIA ABIERTA DE OCTUBRE: entro y no ha salido, asi que no tiene ningun dia y ninguna
		// lectura de periodo la alcanza.
		private const string TheOctoberBranch = "Taller Alajuela";
		private const string TheOctoberSubject = "Revisión de garantía";

		private static readonly DateTime OctoberEntered =
			new DateTime(2026, 10, 5, 8, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheInstantInsideTheOpenStay =
			new DateTime(2026, 10, 7, 12, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheInstantNoStayCovers =
			new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

		private AScheduleOnARealJournal journal;

		[SetUp]
		public void Setup()
		{
			journal = new AScheduleOnARealJournal(TheVin, "Standard");
		}

		[TearDown]
		public void TearDown()
		{
			journal.Dispose();
		}

		// ===== ESTADO UNO: NADA EN EL REGISTRO ============================================

		// LAS OCHO CONTESTAN SIN NINGUNA ESTADIA, y ninguna revienta. Es la mitad de la polaridad que
		// §9 del brief pide total: la ausencia se dice, no se rechaza y no se inventa.
		[Test]
		public void EveryOneOfTheEightAnswersWhenNothingIsOnRecord()
		{
			Assert.That(journal.Api.IsInsideTheShopAt(TheInstantNoStayCovers), Is.False,
				"IsInsideTheShopAt sin ninguna estadia.");

			Assert.That(
				journal.Api.DescribeTheStayAt(TheInstantNoStayCovers),
				Does.Contain("ninguna estadía").And.Contains("2026-05-01 00:00:00"),
				"DescribeTheStayAt nombra el INSTANTE por el que se pregunto, con su hora.");

			Assert.That(journal.Api.HasAnyStayBetween(TheYearFrom, TheYearTo), Is.False,
				"HasAnyStayBetween sin ninguna estadia.");

			Assert.That(journal.Api.DaysInTheShopBetween(TheYearFrom, TheYearTo), Is.Empty,
				"DaysInTheShopBetween contesta la lista VACIA, que es una respuesta y no un rechazo.");

			Assert.That(journal.Api.DaysInTheShopCountBetween(TheYearFrom, TheYearTo), Is.EqualTo(0),
				"DaysInTheShopCountBetween sin ninguna estadia.");

			Assert.That(
				journal.Api.DescribeDaysInTheShopBetween(TheYearFrom, TheYearTo),
				Does.Contain("2026-01-01").And.Contains("2026-12-31").And.Contains("ninguna estadía"),
				"DescribeDaysInTheShopBetween nombra LOS DOS DIAS del periodo: un cero no diria entre "
				+ "cuando y cuando se pregunto.");

			Assert.That(journal.Api.StayCountBetween(TheYearFrom, TheYearTo), Is.EqualTo(0),
				"StayCountBetween sin ninguna estadia.");

			ShopStayRecord record = journal.Api.StaysBetween(TheYearFrom, TheYearTo);

			Assert.That(record.Branch, Is.Empty, "StaysBetween: la columna Branch, sin filas.");
			Assert.That(record.Subject, Is.Empty);
			Assert.That(record.EnteredAt, Is.Empty);
			Assert.That(record.CoveredDays, Is.Empty);
			Assert.That(record.Says, Is.Empty);
		}

		// ===== ESTADO DOS: DOS ESTADIAS CERRADAS Y UNA ABIERTA ============================

		// EL ESTADO QUE HACE QUE EL BARRIDO MIDA ALGO. Las ocho contestan, cada una lo suyo y por
		// valor, y ninguna de las respuestas se parece a otra: dos cuentas distintas, dos frases
		// distintas, dos filas con largos de dias distintos.
		[Test]
		public void EveryOneOfTheEightAnswersWhenTwoStaysAreOnRecordAndOneIsStillOpen()
		{
			TheTwoClosedStaysAndTheOpenOne();

			Assert.That(journal.Api.IsInsideTheShopAt(TheInstantInsideTheOpenStay), Is.True,
				"IsInsideTheShopAt DENTRO de la estadia abierta.");
			Assert.That(journal.Api.IsInsideTheShopAt(TheInstantNoStayCovers), Is.False,
				"Y fuera de las tres, que es lo que separa esta lectura de 'hay alguna estadia'.");

			Assert.That(
				journal.Api.DescribeTheStayAt(TheInstantInsideTheOpenStay),
				Does.Contain(TheOctoberBranch).And.Contains(TheOctoberSubject),
				"DescribeTheStayAt dice la estadia ABIERTA entera: donde y por que.");
			Assert.That(
				journal.Api.DescribeTheStayAt(MarchCollected),
				Does.Contain(TheMarchBranch).And.Contains("2026-03-04"),
				"Y la cerrada de marzo cuando se pregunta por un instante que ELLA cubre.");

			Assert.That(journal.Api.HasAnyStayBetween(TheYearFrom, TheYearTo), Is.True,
				"HasAnyStayBetween con dos estadias con dias registrados.");

			Assert.That(
				journal.Api.DaysInTheShopBetween(TheYearFrom, TheYearTo),
				Is.EqualTo(new[]
				{
					"2026-03-02", "2026-03-03", "2026-03-04", "2026-07-20", "2026-07-21",
				}),
				"DaysInTheShopBetween: los CINCO dias recibidos, ascendentes y por valor. Un largo de "
				+ "cinco se satisface igual con cinco dias equivocados.");

			Assert.That(journal.Api.DaysInTheShopCountBetween(TheYearFrom, TheYearTo), Is.EqualTo(5),
				"DaysInTheShopCountBetween: CINCO dias.");

			Assert.That(
				journal.Api.DescribeDaysInTheShopBetween(TheYearFrom, TheYearTo),
				Does.Contain("5 días").And.Contains("2 estadías").And.Contains("2026-07-21"),
				"DescribeDaysInTheShopBetween: los dias, en cuantas estadias, y cuales.");

			Assert.That(journal.Api.StayCountBetween(TheYearFrom, TheYearTo), Is.EqualTo(2),
				"StayCountBetween: DOS estadias, y no cinco -- estadias y dias son dos cuentas "
				+ "distintas, y cruzarlas cambia esta afirmacion y la de arriba.");

			ShopStayRecord record = journal.Api.StaysBetween(TheYearFrom, TheYearTo);

			Assert.That(record.Branch, Is.EqualTo(new[] { TheMarchBranch, TheJulyBranch }),
				"StaysBetween: las dos filas, en orden de ENTRADA.");
			Assert.That(record.Subject, Is.EqualTo(new[] { TheMarchSubject, TheJulySubject }));
			Assert.That(
				record.EnteredAt,
				Is.EqualTo(new[] { "2026-03-02 07:00:00", "2026-07-20 09:15:00" }),
				"Y los instantes de entrada con su HORA: un instante nunca se dice como un dia de "
				+ "calendario a secas.");
			Assert.That(
				record.CoveredDays,
				Is.EqualTo(new[] { "2026-03-02 2026-03-03 2026-03-04", "2026-07-20 2026-07-21" }));
			Assert.That(record.Says[0], Does.Contain(TheMarchSubject));
			Assert.That(record.Says[1], Does.Contain(TheJulySubject));
		}

		// ===== Y LOS OCHO SCRIPTS SOBREVIVEN AL JOURNAL ===================================

		// LO QUE NINGUNA PRUEBA DEL NIVEL A PUEDE TOCAR: las ocho respuestas vivas contra las
		// reproducidas. Es lo unico que ve una normalizacion del borde de la puerta (C53) -- el
		// segundo programa no la muestra, porque el journal normaliza tambien.
		[Test]
		public void TheEightAnswerTheSameAfterTheJournalIsClosedAndReopened()
		{
			TheTwoClosedStaysAndTheOpenOne();

			Dictionary<string, string> live = TheEightAnswers();

			journal.CloseAndReopenFromTheJournal();

			Dictionary<string, string> replayed = TheEightAnswers();

			foreach (KeyValuePair<string, string> each in live)
				Assert.That(replayed[each.Key], Is.EqualTo(each.Value),
					"'" + each.Key + "' contesto distinto despues de reabrir el journal.");
		}

		// NINGUNA DE LAS OCHO ESCRIBE UNA ENTRADA. El error que esto atrapa es de una sola letra --
		// PerformCommand donde iba PerformQuery -- y el estado seguiria siendo correcto, asi que
		// ninguna otra prueba se pondria roja.
		[Test]
		public void NotOneOfTheEightWritesAnEntry()
		{
			TheTwoClosedStaysAndTheOpenOne();

			string before = TheBytesOf(journal.Folder);

			TheEightAnswers();

			Assert.That(TheBytesOf(journal.Folder), Is.EqualTo(before),
				"El journal quedo byte a byte como estaba. Una lectura que escribe le agrega una "
				+ "entrada al journal de la persona cada vez que abre la app, para siempre, y el "
				+ "estado sigue siendo correcto -- asi que ninguna otra prueba se pondria roja.");
		}

		// El arbol entero del journal reducido a una cadena: nombre relativo mas hash del contenido,
		// en orden. Repetido a proposito -- cada fixture de esta suite es dueño de su aparejo.
		private static string TheBytesOf(string folder)
		{
			List<string> files = new List<string>(
				Directory.GetFiles(folder, "*", SearchOption.AllDirectories));
			files.Sort(StringComparer.Ordinal);

			StringBuilder tree = new StringBuilder();

			foreach (string file in files)
			{
				using (FileStream open = new FileStream(
					file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
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

		// LAS SEIS DE PERIODO PASAN SUS DOS LIMITES POR ADayRange, Y ESO SE VE EN EL TIPO DEL RECHAZO.
		// Una lectura que atara la cadena cruda dejaria que el StayDay del dominio la rechazara
		// adentro del script: no escribiria ninguna entrada -- es una consulta -- pero el rechazo
		// seria un DomainException, que ninguna caratula puede nombrar, y no diria que parametro fue.
		// El sitio de cada una es propio, asi que se afirman las seis por separado y el rojo dice cual.
		[Test]
		public void TheSixPeriodReadsRefuseABoundThatIsNotADayAndNameTheParameter()
		{
			TheTwoClosedStaysAndTheOpenOne();

			foreach (KeyValuePair<string, TestDelegate> each in TheSixPeriodReadsAskedWith("10/06/2026"))
			{
				ArgumentException refused = Assert.Throws<ArgumentException>(each.Value,
					"'" + each.Key + "' tenia que rechazar un limite que no dice un dia, y en la "
					+ "puerta: el rechazo del dominio es internal y ninguna caratula lo puede nombrar.");

				Assert.That(refused.ParamName, Is.EqualTo("fromDay"),
					"Y el rechazo de '" + each.Key + "' tenia que decir QUE limite no era un dia.");
			}
		}

		// Y EL SEGUNDO LIMITE ANTES QUE EL PRIMERO ES UN PERIODO QUE TERMINA ANTES DE EMPEZAR, y las
		// seis lo rechazan igual. Es la otra ley de ADayRange, y sin este renglon una lectura que solo
		// validara el primer limite pasaria el de arriba.
		[Test]
		public void TheSixPeriodReadsRefuseAPeriodThatEndsBeforeItBegins()
		{
			foreach (KeyValuePair<string, TestDelegate> each
				in TheSixPeriodReadsAskedWith(TheYearFrom, "2025-12-31"))
			{
				ArgumentException refused = Assert.Throws<ArgumentException>(each.Value,
					"'" + each.Key + "' tenia que rechazar un periodo al reves.");

				Assert.That(refused.ParamName, Is.EqualTo("toDay"));
			}
		}

		private Dictionary<string, TestDelegate> TheSixPeriodReadsAskedWith(string fromDay)
		{
			return TheSixPeriodReadsAskedWith(fromDay, TheYearTo);
		}

		private Dictionary<string, TestDelegate> TheSixPeriodReadsAskedWith(
			string fromDay, string toDay)
		{
			return new Dictionary<string, TestDelegate>
			{
				{ "HasAnyStayBetween", () => journal.Api.HasAnyStayBetween(fromDay, toDay) },
				{ "DaysInTheShopBetween", () => journal.Api.DaysInTheShopBetween(fromDay, toDay) },
				{
					"DaysInTheShopCountBetween",
					() => journal.Api.DaysInTheShopCountBetween(fromDay, toDay)
				},
				{
					"DescribeDaysInTheShopBetween",
					() => journal.Api.DescribeDaysInTheShopBetween(fromDay, toDay)
				},
				{ "StayCountBetween", () => journal.Api.StayCountBetween(fromDay, toDay) },
				{ "StaysBetween", () => journal.Api.StaysBetween(fromDay, toDay) },
			};
		}

		// Las ocho, cada una con su nombre al lado, reducidas a texto para poder compararlas de a
		// pares. La fila entera va en la cadena: comparar solo la cuenta dejaria pasar una columna
		// que cambiara de contenido al reproducirse.
		private Dictionary<string, string> TheEightAnswers()
		{
			ShopStayRecord record = journal.Api.StaysBetween(TheYearFrom, TheYearTo);

			return new Dictionary<string, string>
			{
				{
					"IsInsideTheShopAt",
					journal.Api.IsInsideTheShopAt(TheInstantInsideTheOpenStay).ToString()
				},
				{ "DescribeTheStayAt", journal.Api.DescribeTheStayAt(TheInstantInsideTheOpenStay) },
				{
					"HasAnyStayBetween",
					journal.Api.HasAnyStayBetween(TheYearFrom, TheYearTo).ToString()
				},
				{
					"DaysInTheShopBetween",
					string.Join("|", journal.Api.DaysInTheShopBetween(TheYearFrom, TheYearTo))
				},
				{
					"DaysInTheShopCountBetween",
					journal.Api.DaysInTheShopCountBetween(TheYearFrom, TheYearTo).ToString()
				},
				{
					"DescribeDaysInTheShopBetween",
					journal.Api.DescribeDaysInTheShopBetween(TheYearFrom, TheYearTo)
				},
				{
					"StayCountBetween",
					journal.Api.StayCountBetween(TheYearFrom, TheYearTo).ToString()
				},
				{
					"StaysBetween",
					string.Join("|", record.Branch) + " / " + string.Join("|", record.Subject)
					+ " / " + string.Join("|", record.EnteredAt)
					+ " / " + string.Join("|", record.CoveredDays)
					+ " / " + string.Join("|", record.Says)
				},
			};
		}

		// LAS TRES, EN ESTE ORDEN Y NO EN OTRO. La abierta va de ULTIMA porque la puerta pre-chequea
		// la entrada: con ella abierta desde octubre, registrar la de julio despues seria una entrada
		// fechada antes de la abierta y el dominio la refusaria adentro del comando.
		private void TheTwoClosedStaysAndTheOpenOne()
		{
			journal.Api.RecordShopStay(
				TheMarchBranch, TheMarchSubject, MarchEntered, MarchCollected, TheMarchDays);

			journal.Api.RecordShopStay(
				TheJulyBranch, TheJulySubject, JulyEntered, JulyCollected, TheJulyDays);

			journal.Api.RecordShopEntry(TheOctoberBranch, TheOctoberSubject, OctoberEntered);
		}
	}
}
