using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Choreography.Theater;
using DealershipCar.App;
using NUnit.Framework;
using Puppeteer;

namespace DealershipCar.Dominio.Pruebas
{
	// LO QUE CUESTA CADA UNO DE LOS RECHAZOS DE LA QUINTA GLOBAL, MEDIDO EN BYTES Y NO AFIRMADO.
	// ARefusedCommandCostsAJournalEntryUnlessItIsGuarded midio las tres maneras de rechazar y lo que
	// cuesta cada una; esta prueba las aplica a los tres comandos nuevos y dice CUALES caen de que
	// lado.
	//
	// LA GUARDA G2 -- una sola estadia abierta a la vez -- ES LA QUE DISPARA EN OPERACION ORDINARIA,
	// asi que la puerta paga el pre-chequeo (DEC-34: una composicion completa por rechazo). El
	// dominio SIGUE siendo el dueno de la invariante: el pre-chequeo no la reemplaza, la adelanta al
	// unico momento en que rechazar todavia es gratis.
	//
	// Y EL PRE-CHEQUEO NO LA VE ENTERA, que es la mitad que esta prueba mide y que no estaba escrita.
	// IsInsideTheShopAt(enteredAt) es falso cuando la estadia abierta empezo DESPUES del instante que
	// llega -- una sucursal registrando una entrada con fecha atrasada mientras otra estadia esta
	// abierta -- y ahi el hecho llega al dominio, la guarda revienta ADENTRO del cuerpo, y la entrada
	// ya estaba escrita. Esa es la cicatriz, y el motor la loguea en cada arranque futuro de este
	// journal, para siempre.
	//
	// LO MISMO DEL OTRO LADO: cerrar lo que no esta abierto se pre-chequea con LA MISMA lectura, y
	// tampoco la ve entera. IsInsideTheShopAt es verdadero cuando una estadia CERRADA cubre ese
	// instante, y ahi no hay nada que cerrar y el dominio revienta igual. Que el pre-chequeo sea un
	// ACOTAMIENTO y no una equivalencia queda medido abajo en vez de dicho.
	//
	// LAS QUE NO SE PRE-CHEQUEAN, y por que: L6 -- una salida anterior a su entrada -- y la
	// contiguidad de los dias viven en el CONSTRUCTOR de un tipo del dominio. Alcanzarlas desde la
	// frontera significaria armar el carrier del dominio ahi, que es justo la escalada que va adentro
	// de la fachada. Las dos cicatrizan por la ruta del acto unico, y eso se mide abajo.
	//
	// Y EL TIPO DE LA EXCEPCION ES LA MITAD QUE DISCRIMINA: un Assert.Catch pasaria igual con la
	// excepcion del dominio, que es interna y que ninguna caratula puede nombrar. Se afirma
	// InvalidOperationException a proposito.
	[TestFixture]
	public sealed class ASecondEntryIsRefusedFreeAtTheDoorAndScarredOnlyIfItReachesTheDomain
	{
		private const string TheVin = "JH4KA7660MC012345";
		private const string TheModel = "SM-SEDAN-2025";
		private const string TheBranch = "Taller Curridabat";
		private const string TheSecondBranch = "Taller Escazu";
		private const string TheSubject = "Cambio de aceite";
		private const string TheOtherSubject = "Llanta ponchada";

		private static readonly DateTime Entered =
			new DateTime(2026, 6, 10, 8, 30, 0, DateTimeKind.Utc);

		private static readonly DateTime EnteredAgainLater =
			new DateTime(2026, 6, 11, 9, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime EnteredAgainBackdated =
			new DateTime(2026, 6, 9, 9, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime Collected =
			new DateTime(2026, 6, 12, 16, 45, 0, DateTimeKind.Utc);

		private static readonly string[] TheDaysItCovered =
			{ "2026-06-10", "2026-06-11", "2026-06-12" };

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

		// LA MITAD QUE PAGA EL PRE-CHEQUEO: la segunda entrada ordinaria -- la que llega DESPUES de
		// que la primera abrio -- se rechaza en la puerta y NO CUESTA UN SOLO BYTE.
		[Test]
		public void ASecondEntryWhileOneIsOpenIsRefusedAtTheDoorAndCostsNothing()
		{
			api.RecordShopEntry(TheBranch, TheSubject, Entered);

			string before = TheBytesOf(folder);
			long bytesBefore = HowManyBytes(folder);

			InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(
				() => api.RecordShopEntry(TheSecondBranch, TheOtherSubject, EnteredAgainLater),
				"Y es la excepcion DE LA PUERTA. Un Assert.Catch pasaria igual con la del dominio, "
				+ "que es interna: ninguna caratula puede nombrarla, asi que un rechazo que saliera "
				+ "por ahi solo se podria atrapar como Exception pelada -- junto con un fallo de "
				+ "disco y un defecto de programacion.");

			Assert.That(refusal.Message, Does.Contain("IsInsideTheShopAt"),
				"Y nombra la pregunta que habia que hacer antes, que es lo que le deja a quien "
				+ "registra por donde averiguarlo.");

			TestContext.Out.WriteLine(
				"RECHAZO GRATIS EN LA PUERTA -- bytes del journal antes: " + bytesBefore
				+ " | despues: " + HowManyBytes(folder));

			Assert.That(TheBytesOf(folder), Is.EqualTo(before),
				"BYTE A BYTE IGUAL. El rechazo no llego a haber comando, asi que no hay entrada que "
				+ "reventar y no hay cicatriz: es lo que compra el pre-chequeo.");
		}

		// Y LA ESTADIA ABIERTA QUEDO COMO ESTABA. Sin esto, lo de arriba estaria verde igual si el
		// rechazo hubiera entrado a medias.
		[Test]
		public void TheRefusedSecondEntryLeavesTheFirstStayExactlyAsItWas()
		{
			api.RecordShopEntry(TheBranch, TheSubject, Entered);

			Assert.Throws<InvalidOperationException>(
				() => api.RecordShopEntry(TheSecondBranch, TheOtherSubject, EnteredAgainLater));

			string says = api.DescribeTheStayAt(EnteredAgainLater);

			Assert.That(says, Does.Contain(TheBranch), "La abierta sigue siendo la PRIMERA...");
			Assert.That(says, Does.Not.Contain(TheSecondBranch), "...y la segunda no entro a medias.");
		}

		// LA MITAD QUE EL PRE-CHEQUEO NO VE, Y ES LA QUE VALE LA MEDICION: una entrada con fecha
		// ATRASADA mientras hay una abierta. IsInsideTheShopAt en ESE instante es falso -- la abierta
		// empezo despues -- asi que el pre-chequeo la deja pasar, la guarda del dominio revienta
		// adentro del cuerpo, y la entrada ya estaba escrita.
		//
		// EL ORACULO ES LO QUE EL MOTOR DICE AL REABRIR, y no el tamano del archivo: una entrada que
		// revento hace loguear un fallo de rehidratacion en CADA arranque futuro de este journal.
		[Test]
		public void ABackdatedSecondEntryThePreCheckCannotSeeReachesTheDomainAndScarsTheJournal()
		{
			api.RecordShopEntry(TheBranch, TheSubject, Entered);

			string before = TheBytesOf(folder);
			long bytesBefore = HowManyBytes(folder);

			DomainException refusal = Assert.Throws<DomainException>(
				() => api.RecordShopEntry(TheSecondBranch, TheOtherSubject, EnteredAgainBackdated),
				"Esta la refusa el DOMINIO y no la puerta: el pre-chequeo pregunto por el instante "
				+ "que llego, y en ese instante el vehiculo todavia no estaba adentro.");
			Assert.That(refusal.Message, Does.Contain(TheBranch),
				"Y la guarda del dominio hace su trabajo entero: nombra la estadia abierta.");

			TestContext.Out.WriteLine(
				"RECHAZO QUE CICATRIZA -- bytes del journal antes: " + bytesBefore
				+ " | despues: " + HowManyBytes(folder));

			Assert.That(TheBytesOf(folder), Is.Not.EqualTo(before),
				"PERO la entrada ya se habia escrito cuando el cuerpo corrio.");

			Assert.That(WhatTheEngineSaysWhenItReopens(),
				Does.Contain("Rehydration execution failed"),
				"Y eso es lo que cuesta: el motor lo loguea en cada arranque futuro de este journal, "
				+ "para siempre. El pre-chequeo de la puerta es un ACOTAMIENTO de G2, no una "
				+ "equivalencia, y esta prueba es donde eso esta medido en vez de dicho.");
		}

		// CERRAR LO QUE NO ESTA ABIERTO: se pre-chequea con LA MISMA lectura y en el caso ordinario
		// -- no hay ninguna estadia -- se rechaza gratis.
		[Test]
		public void ClosingWhenNothingIsOpenIsRefusedAtTheDoorAndCostsNothing()
		{
			string before = TheBytesOf(folder);

			InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(
				() => api.RecordShopCollection(Collected, TheDaysItCovered));

			Assert.That(refusal.Message, Does.Contain("IsInsideTheShopAt"));

			Assert.That(TheBytesOf(folder), Is.EqualTo(before),
				"Byte a byte igual: un defecto de quien llama convertido en un rechazo gratis.");
		}

		// Y LA MITAD QUE ESE PRE-CHEQUEO TAMPOCO VE: una estadia CERRADA que cubre ese instante hace
		// verdadero a IsInsideTheShopAt, aunque no haya nada que cerrar. El hecho llega al dominio y
		// cicatriza.
		//
		// ESTO CORRIGE UNA PALABRA DEL TRASPASO DE SH-2, que decia que la lectura es falsa
		// "exactamente cuando no hay nada que cerrar". Es un ACOTAMIENTO: atrapa el caso ordinario y
		// deja pasar este.
		[Test]
		public void ClosingWhileAClosedStayCoversTheInstantSlipsPastThePreCheckAndScars()
		{
			api.RecordShopStay(TheBranch, TheSubject, Entered, Collected, TheDaysItCovered);

			string before = TheBytesOf(folder);

			DomainException refusal = Assert.Throws<DomainException>(
				() => api.RecordShopCollection(Collected, TheDaysItCovered),
				"El pre-chequeo la dejo pasar: en ese instante el vehiculo SI estaba adentro, solo que "
				+ "de una estadia que ya cerro.");
			Assert.That(refusal.Message, Does.Contain("no tiene ninguna estadia abierta que cerrar"));

			Assert.That(TheBytesOf(folder), Is.Not.EqualTo(before));
			Assert.That(WhatTheEngineSaysWhenItReopens(),
				Does.Contain("Rehydration execution failed"));
		}

		// LAS DOS QUE NO SE PRE-CHEQUEAN PORQUE VIVEN EN UN CONSTRUCTOR DEL DOMINIO: L6 y la
		// contiguidad de los dias. Las dos llegan por el acto unico, que no lleva pre-chequeo ninguno,
		// y las dos cicatrizan. Nombradas para que se sepa cuales son.
		[Test]
		public void L6AndTheContiguityOfTheDaysTakeTheScarBecauseTheyLiveInAConstructor()
		{
			DomainException backwards = Assert.Throws<DomainException>(() => api.RecordShopStay(
				TheBranch, TheSubject, Collected, Entered, TheDaysItCovered));

			Assert.That(backwards.Message, Does.Contain("se contradice a si mismo"),
				"L6: una salida anterior a su entrada.");

			DomainException broken = Assert.Throws<DomainException>(() => api.RecordShopStay(
				TheBranch, TheSubject, Entered, Collected,
				new[] { "2026-06-10", "2026-06-12" }));

			Assert.That(broken.Message, Does.Contain("2026-06-11"),
				"La contiguidad, y el rechazo NOMBRA el dia que falta.");

			Assert.That(WhatTheEngineSaysWhenItReopens(),
				Does.Contain("Rehydration execution failed"),
				"Las dos dejaron entrada. Adelantarlas pediria armar el tipo del dominio en la "
				+ "frontera, que es la escalada que va adentro de la fachada.");
		}

		// LO QUE LA PUERTA SI DESCARTA GRATIS SIN TOCAR NINGUNA LEY: lo que no se puede DECIR. Un
		// texto en blanco no es una sucursal menos precisa ni un asunto menos preciso -- no es
		// ninguno -- y rechazarlo aca no duplica ninguna ley del dominio.
		[Test]
		public void ATextThatSaysNothingIsRefusedAtTheDoorAndCostsNothing()
		{
			string before = TheBytesOf(folder);

			Assert.Throws<ArgumentException>(
				() => api.RecordShopEntry("   ", TheSubject, Entered),
				"Una sucursal en blanco.");

			Assert.Throws<ArgumentException>(
				() => api.RecordShopEntry(TheBranch, "", Entered),
				"Un asunto en blanco.");

			Assert.Throws<ArgumentException>(
				() => api.RecordShopStay(TheBranch, TheSubject, Entered, Collected, new[] { "" }),
				"Y un dia en blanco entre los dias recibidos.");

			Assert.That(TheBytesOf(folder), Is.EqualTo(before),
				"Los tres, byte a byte igual. Ninguno llego a haber comando.");
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

		// LO QUE EL MOTOR DICE AL REHIDRATAR, escuchando LAS DOS SALIDAS: sus lineas de error van a
		// STDERR, y escuchar solo una dejaria este oraculo verde para siempre. Las dos originales se
		// reponen pase lo que pase.
		private string WhatTheEngineSaysWhenItReopens()
		{
			TextWriter originalOut = Console.Out;
			TextWriter originalError = Console.Error;
			StringWriter heard = new StringWriter();

			try
			{
				Console.SetOut(heard);
				Console.SetError(heard);

				Close();
				Open();
			}
			finally
			{
				Console.SetOut(originalOut);
				Console.SetError(originalError);
			}

			return heard.ToString();
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

		// EL TAMANO, SOLO PARA IMPRIMIRLO. Lo que AFIRMA la ley es TheBytesOf -- el arbol con el hash
		// de cada archivo -- porque un tamano igual se satisface tambien con un byte cambiado.
		private static long HowManyBytes(string folder)
		{
			long total = 0;

			foreach (string file in Directory.GetFiles(folder, "*", SearchOption.AllDirectories))
				using (FileStream open = new FileStream(
					file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
					total = total + open.Length;

			return total;
		}

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
	}
}
