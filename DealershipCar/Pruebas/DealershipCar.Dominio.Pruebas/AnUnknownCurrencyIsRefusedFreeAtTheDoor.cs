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
	// EL RECHAZO QUE DEC-96 DEJO PENDIENTE, COBRADO. El comando del dominio resuelve la moneda
	// recibida con Currency.OfPublished, que es TOTAL: un codigo que el conjunto no nombra entra al
	// libro como la variante recibida-y-sin-nombrar en vez de reventar. Eso es a proposito -- PROBE 8
	// midio que reventar ADENTRO del cuerpo CICATRIZA el journal, porque para cuando el cuerpo corre
	// la entrada ya se escribio -- y deja el rechazo donde todavia es gratis: la puerta, antes de que
	// haya nada que performar.
	//
	// ESTE ES EL PRIMER LLAMADOR DE PRODUCCION DE WireName.Currency. CL-1 lo escribio con su
	// recogniser y los dos pasaron tres tareas sin que nada de produccion los llamara; el resolutor
	// entra aca y el recogniser entra en CL-7, con la mitad de L14 que le toca al feed.
	//
	// BYTE A BYTE ES EL CHEQUEO CORRECTO AQUI, y es la excepcion que C23 nombra: C23 dice que dos
	// corridas de la misma secuencia recibida NO son byte-identicas -- cada una anota su propio
	// instante de ocurrencia -- y que byte-identico es lo que hay que exigirle a una operacion que
	// tiene que escribir NADA. Un comando rechazado es exactamente esa operacion.
	//
	// Y EL ORACULO NO ES EL TAMANO DEL ARCHIVO: se vuelve a abrir el journal escuchando LAS DOS
	// SALIDAS de la consola. C70 afirma que el rechazo del motor llega por el stream de ERRORES y esa
	// afirmacion sigue SIN VERIFICAR; escuchar una sola dejaria este oraculo verde para siempre,
	// cualquiera de las dos que sea la buena.
	//
	// LO QUE ESTA PRUEBA NO CONTRADICE: que una moneda sin nombrar SI puede llegar al libro. DEC-96
	// nombra ese costo y DEC-88 lo pinta -- dos gastos en un codigo que este modulo no nombra son DOS
	// grupos de moneda -- y las dos cosas son ciertas a la vez porque hablan de dos sitios distintos.
	// El renglon de abajo las mide juntas en vez de dejar que alguien las descubra en tension.
	// NO SE PARALELIZA, Y ES UNA MEDICION. WhatTheEngineSaysWhenItReopens() captura con
	// Console.SetOut/SetError lo que el motor escribe al reabrir el journal, y esos dos son del
	// PROCESO ENTERO: en paralelo, el arranque del actor de otra prueba escribe en la misma captura y
	// la afirmacion lee lo que dijo otra.
	//
	// MEDIDO 2026-09-04 con [assembly: Parallelizable(ParallelScope.Fixtures)]: en serie 2063/2063
	// verde en 49.8 s, con 8 workers 16.0 s y SIETE rojos, todos en los CINCO fixtures que redirigen
	// la consola. Aca el rojo fue TheRefusedCommandLeavesNothingForTheEngineToComplainAboutOnReopening,
	// que es justo la afirmacion mas fragil de todas: espera NO oir nada.
	//
	// Y ESO SE CONECTA CON C70, que sigue SIN VERIFICAR: su afirmacion central es sobre en cual de los
	// dos streams llega el rechazo. La captura escucha los dos a proposito; lo que no puede es
	// escucharlos mientras otra prueba escribe en ellos.
	//
	// Son CINCO fixtures y no los tres que se pusieron rojos (C57). Sin Parallelizable en el
	// ensamblado esto no hace nada.
	[TestFixture]
	[NonParallelizable]
	public sealed class AnUnknownCurrencyIsRefusedFreeAtTheDoor
	{
		private const string TheVin = "1FTFW1ET5DFA12345";
		private const string TheModel = "YARIS-2019";

		private const string TheCodeTheSetDoesNotName = "XYZ";
		private const string TheSubject = "Llantas nuevas";
		private const decimal TheAmount = 240000m;
		private const int FourYearsOfLife = 48;

		private static readonly DateTime TheCostIncurred =
			new DateTime(2026, 3, 12, 14, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheYearFrom =
			new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheYearTo =
			new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc);

		private string folder;
		private DealershipCarApp app;
		private DealershipCarAPI api;

		[SetUp]
		public void Setup()
		{
			folder = AFreshJournalFolder.Make();
			Open();
		}

		[TearDown]
		public void TearDown()
		{
			Close();
			AFreshJournalFolder.Sweep(folder);
		}

		// LOS DOS COMANDOS, con un codigo que el conjunto no nombra. Se afirma el TIPO de la
		// excepcion a proposito: un Assert.Catch pasaria igual con la del dominio, que es interna y
		// que ninguna caratula puede nombrar -- un rechazo que saliera por ahi solo se podria atrapar
		// como Exception pelada, junto con un fallo de disco y un defecto de programacion.
		[Test]
		public void NeitherCommandLetsACodeTheSetDoesNotNameReachTheLedger()
		{
			string before = TheBytesOf(folder);

			ArgumentException refusedRecurring = Assert.Throws<ArgumentException>(
				() => api.RecordRecurringCost(
					TheCostIncurred, TheAmount, TheCodeTheSetDoesNotName, TheSubject));

			ArgumentException refusedDurable = Assert.Throws<ArgumentException>(
				() => api.RecordDurableCost(
					TheCostIncurred, TheAmount, TheCodeTheSetDoesNotName, TheSubject,
					FourYearsOfLife));

			TestContext.Out.WriteLine("RECURRENTE -- " + refusedRecurring.Message);
			TestContext.Out.WriteLine("DURADERO   -- " + refusedDurable.Message);

			foreach (ArgumentException refusal in new[] { refusedRecurring, refusedDurable })
			{
				Assert.That(refusal.Message, Does.Contain(TheCodeTheSetDoesNotName),
					"El rechazo dice QUE codigo llego.");
				Assert.That(refusal.Message, Does.Contain(Currency.TheOnesThereAre()),
					"Y contra que se miro, pedida la lista al dominio: Currency.TheOnesThereAre es el "
					+ "unico sitio donde los codigos se escriben, y una segunda copia se despega sin "
					+ "que nada se ponga rojo.");
				Assert.That(refusal.ParamName, Is.EqualTo("currency"),
					"Y nombra el parametro, que es lo que le deja a quien registra por donde "
					+ "arreglarlo.");
			}

			Assert.That(TheBytesOf(folder), Is.EqualTo(before),
				"BYTE A BYTE IGUAL, y aca ese es el chequeo correcto: un comando rechazado tiene que "
				+ "escribir NADA. El rechazo no llego a haber comando, asi que no hay entrada que "
				+ "reventar y no hay cicatriz -- que es lo que compra resolver la moneda en la puerta "
				+ "en vez de dentro del cuerpo.");
		}

		// Y EL JOURNAL SIGUE SANO, que es lo que el tamano no puede decir. Una entrada que revento
		// hace loguear un fallo de rehidratacion en CADA arranque futuro; esta se reabre callada.
		[Test]
		public void TheRefusedCommandLeavesNothingForTheEngineToComplainAboutOnReopening()
		{
			Assert.Throws<ArgumentException>(
				() => api.RecordRecurringCost(
					TheCostIncurred, TheAmount, TheCodeTheSetDoesNotName, TheSubject));

			string heard = WhatTheEngineSaysWhenItReopens();

			TestContext.Out.WriteLine("EL MOTOR AL REABRIR -- [" + heard.Trim() + "]");

			Assert.That(heard, Does.Not.Contain("Rehydration execution failed"),
				"Nada que rehidratar mal: no se escribio ninguna entrada.");
			Assert.That(heard, Does.Not.Contain("[Puppeteer ERROR]"));

			Assert.That(
				api.CostEntryCountBetween(TheYearFrom, TheYearTo),
				Is.EqualTo(0),
				"Y el libro sigue vacio: el rechazo no entro a medias.");
		}

		// UN CODIGO EN BLANCO TAMBIEN, y por los dos comandos. WireName.Currency lo exige antes de
		// preguntarle nada al dominio, porque ninguna entrada lo pudo guardar asi.
		//
		// SE ATRAPA POR LA FAMILIA Y SE DISCRIMINA POR EL PARAMETRO. MEDIDO: un null sale como
		// ArgumentNullException y los otros dos como ArgumentException, porque las dos las tira el
		// mismo ThrowIfNullOrWhiteSpace del BCL y la primera DERIVA de la segunda. Assert.Throws exige
		// el tipo exacto y dejaria el null rojo por una razon que no es la ley; lo que separa este
		// rechazo del de al lado no es el tipo sino QUE parametro nombra, y eso si se afirma.
		[Test]
		public void ACurrencyThatSaysNothingIsRefusedFreeToo()
		{
			string before = TheBytesOf(folder);

			foreach (string blank in new[] { null, "", "   " })
			{
				string said = blank;

				Assert.That(
					Assert.Catch<ArgumentException>(
						() => api.RecordRecurringCost(TheCostIncurred, TheAmount, said, TheSubject))
						.ParamName,
					Is.EqualTo("currency"));

				Assert.That(
					Assert.Catch<ArgumentException>(
						() => api.RecordDurableCost(
							TheCostIncurred, TheAmount, said, TheSubject, FourYearsOfLife))
						.ParamName,
					Is.EqualTo("currency"));
			}

			Assert.That(TheBytesOf(folder), Is.EqualTo(before),
				"Los seis, byte a byte igual. Ninguno llego a haber comando.");
		}

		// Y EL CODIGO SE EXIGE EXACTO, sin arreglarle la ortografia a nadie. Currency.IsNamed compara
		// por Ordinal; recortar el codigo en la puerta lo dejaria pasar aca y lo escribiria distinto
		// de como llego, que es lo contrario de lo que un codigo recibido es.
		[Test]
		public void ACodeWithItsEdgesPaddedIsNotTheCodeAndIsRefusedToo()
		{
			string before = TheBytesOf(folder);

			Assert.Throws<ArgumentException>(
				() => api.RecordRecurringCost(TheCostIncurred, TheAmount, " CRC ", TheSubject));

			Assert.That(TheBytesOf(folder), Is.EqualTo(before));

			api.RecordRecurringCost(TheCostIncurred, TheAmount, "CRC", TheSubject);

			Assert.That(
				api.CostEntryCountBetween(TheYearFrom, TheYearTo),
				Is.EqualTo(1),
				"Y el codigo tal como el conjunto lo nombra si pasa, que es lo que hace que el "
				+ "renglon de arriba mida el padding y no la puerta entera.");
		}

		// LAS DOS COSAS QUE SON CIERTAS A LA VEZ, MEDIDAS JUNTAS. Por la PUERTA el codigo se rechaza
		// gratis; en el AGREGADO entra, porque el comando del dominio resuelve con OfPublished y eso
		// es DEC-96. No estan en tension: son dos sitios distintos y el segundo existe para que un
		// hecho YA ESCRITO -- por un journal viejo o, a partir de CL-7, por el feed -- rehidrate sin
		// cicatrizar. Lo que CL-4 cierra es UNA de las dos entradas, y decir cual es la mitad honesta.
		[Test]
		public void TheSameCodeTheDoorRefusesStillReachesTheAggregateAndThatIsDEC96()
		{
			Assert.Throws<ArgumentException>(
				() => api.RecordRecurringCost(
					TheCostIncurred, TheAmount, TheCodeTheSetDoesNotName, TheSubject),
				"Por la puerta, NO.");

			CostLedger direct = new CostLedger(TheVin);

			direct.RecordRecurringCost(
				TheCostIncurred, TheAmount, TheCodeTheSetDoesNotName, TheSubject);

			Assert.That(direct.CostEntryCountBetween(TheYearFrom, TheYearTo), Is.EqualTo(1),
				"En el agregado, SI. Currency.OfPublished es total, y lo es para que una entrada que "
				+ "ya existe vuelva a leerse en vez de reventar en cada arranque.");
			Assert.That(
				direct.DescribeEachCostEntryBetween(TheYearFrom, TheYearTo)[0],
				Does.Contain("240000 " + TheCodeTheSetDoesNotName),
				"Y se dice con su codigo detras y el monto tal como llego: sin que el conjunto la "
				+ "nombre no se sabe su signo ni cuantos decimales tiene.");
		}

		private void Open()
		{
			Directory.CreateDirectory(folder);

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

		// LO QUE EL MOTOR DICE AL REHIDRATAR, escuchando LAS DOS SALIDAS. Las dos originales se
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

		// EL ARBOL DEL JOURNAL con el hash de cada archivo. Un tamano igual se satisface tambien con
		// un byte cambiado; esto no.
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
