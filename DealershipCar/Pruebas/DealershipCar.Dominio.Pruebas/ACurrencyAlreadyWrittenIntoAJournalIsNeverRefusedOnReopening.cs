using System;
using System.IO;
using System.Text;
using DealershipCar.App;
using NUnit.Framework;
using Puppeteer;

namespace DealershipCar.Dominio.Pruebas
{
	// LA PRUEBA QUE DEC-85 DEBIA Y QUE NADA SOSTENIA. La decision de migrar ServicePriceEntry a un
	// solo Money descansaba en una medicion hecha en un aparejo BORRADO -- SPIKES-BuildOrder.md
	// PROBE 8, cuyos journals vivian bajo un directorio de descarte. Esto la vuelve a hacer, adentro
	// de la suite, donde puede volverse roja el dia que alguien cambie el resolvedor.
	//
	// LA LEY: una moneda que YA quedo escrita en un journal no se rechaza nunca al reabrirlo. Un
	// hecho ya recibido no se puede des-recibir -- rechazarlo deja una entrada fallida que el motor
	// vuelve a loguear en CADA arranque futuro, para siempre, y arrastra consigo los comandos que
	// dependian de su estado.
	//
	// POR QUE ESTE JOURNAL ES UNO DE ANTES DE LA REBANADA. La condicion entera de DEC-85 es que
	// NINGUNA de las tres firmas de afuera se mueva: la de la puerta, la de ModelSpecification y la
	// del mensaje del feed. El journal guarda EL TEXTO DEL SCRIPT y sus argumentos, asi que si las
	// firmas no se movieron, lo que la app migrada escribe es letra por letra lo que la app sin
	// migrar habria escrito. Eso NO SE SUPONE: la primera prueba de abajo lo AFIRMA leyendo el
	// segundo programa del journal.
	//
	// EL FALSIFICADOR ES PROBE 8 VARIANTE A: cambiar Currency.OfPublished por Currency.Of en
	// ModelSpecification.PublishServicePrice. Ahi el journal de XYZ cicatriza con DOS entradas
	// fallidas -- el precio, y detras de el el sello de la revision, que nombra un miembro que existe
	// y esta bien escrito y falla igual porque el estado del que dependia no se aplico. El control en
	// CRC se queda VERDE bajo ese mismo parche, que es lo que hace que el rojo signifique algo (C60).
	//
	// NO SE USA AScheduleOnARealJournal, Y ES A PROPOSITO. CL-0 midio que su Dispose() llama
	// Directory.Delete(folder, true), choca con el journal_000001.bin que el motor tiene tomado, y se
	// come el IOException DESPUES de haber borrado index/index.bin y meta.bin: una carpeta sana tiene
	// TRES archivos y lo que sobrevive tiene UNO, y no se puede reabrir. Aca se abre DealershipCarApp
	// directo, que son las mismas tres lineas que su Open() corre.
	[TestFixture]
	public sealed class ACurrencyAlreadyWrittenIntoAJournalIsNeverRefusedOnReopening
	{
		private const string TheVin = "JH4KA7660MC099001";
		private const string TheModel = "SM-SEDAN-2025";
		private const string TheRevision = "REV-2025-01";

		// La moneda que el conjunto NO nombra. Es la de PROBE 8, y llega igual que ahi: nada en el
		// repo la rechaza -- Interpret solo mira que no venga en blanco y la puerta la manda como
		// texto --, asi que un journal con ella esta a un POST de distancia sin que nadie se equivoque.
		private const string TheCurrencyTheSetDoesNotName = "XYZ";

		private const string TheCurrencyTheSetNames = "CRC";

		private const int TheMilestone = 10000;
		private const string TheUnit = "UnitA";
		private const string TheOperation = "Inspection";

		private static readonly DateTime PublishedAt =
			new DateTime(2025, 3, 14, 10, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime ValidFrom =
			new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime ValidUntil =
			new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime WhileItIsInForce =
			new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc);

		// El monto de PROBE 8, con su escala: 85000.1 despues de que el Normalized de la puerta le
		// quita el cero de mas. Es lo que hace que la frase de la variante sin nombrar se pueda
		// comparar letra por letra con la del arbol sin migrar.
		private const decimal TheAmount = 85000.10m;

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

		// EL ANCLA, Y VA PRIMERA. Sin ella las otras miden un journal cualquiera: lo que las vuelve
		// una prueba sobre un journal DE ANTES es que el texto que quedo escrito es el que la app sin
		// migrar escribia. Se afirman las dos mitades del segundo programa -- el miembro con su lista
		// de ocho, y la moneda viajando como TEXTO suelto en los argumentos.
		[Test]
		public void TheJournalHoldsThePreSliceScriptAndCarriesTheCurrencyAsBareText()
		{
			APriceIsPublishedIn(TheCurrencyTheSetDoesNotName);

			string program = TheSecondProgramOf(folder);

			Assert.That(program, Does.Contain("specification.PublishServicePrice("),
				"El miembro que las entradas viejas nombran sigue existiendo con ese nombre. Si se "
				+ "hubiera renombrado, PROBE 7 midio que el journal pierde el precio Y el sello.");

			foreach (string slot in new[]
				{
					"revisionCode", "kind", "milestoneDistance", "milestoneUnit",
					"amount", "currency", "validFrom", "validUntil"
				})
				Assert.That(program, Does.Contain(slot),
					"La lista de OCHO ranuras es la que las entradas viejas nombran, y '" + slot
					+ "' es una de ellas. Migrar el wire es la opcion (c), que CL-0 midio y que "
					+ "pierde la revision entera.");

			Assert.That(program, Does.Contain(TheCurrencyTheSetDoesNotName),
				"Y la moneda quedo escrita como TEXTO, no como un miembro de ningun conjunto. Eso "
				+ "es lo que el journal fijo y lo que ninguna migracion puede des-hacer.");
		}

		// LA LEY, sobre la moneda que el conjunto no nombra. CERO lineas de error del motor: ni una
		// entrada fallida, ni una sola vez.
		[Test]
		public void ReopeningAJournalWithACurrencyTheSetDoesNotNameLogsNoFailureAtAll()
		{
			APriceIsPublishedIn(TheCurrencyTheSetDoesNotName);

			string heard = WhatTheEngineSaysWhenItReopens();

			TestContext.Out.WriteLine(
				"REAPERTURA CON " + TheCurrencyTheSetDoesNotName + " -- el motor dijo: <"
				+ heard.Trim() + ">");

			Assert.That(HowManyTimesItSays(heard, "[Puppeteer ERROR]"), Is.EqualTo(0),
				"CERO. La variante ingenua de PROBE 8 -- resolver con Currency.Of, total con "
				+ "excepcion -- deja DOS aca: el precio y el sello detras de el.");
			Assert.That(heard, Does.Not.Contain("Rehydration execution failed"));
			Assert.That(heard, Does.Not.Contain(TheCurrencyTheSetDoesNotName),
				"Y el motor no la nombra en ningun lado, que es la unica forma de estar seguro de "
				+ "que no la esta rechazando por un camino que no es una entrada fallida.");
		}

		// Y NO ALCANZA CON QUE NO SE QUEJE: el precio tiene que seguir CONTESTANDO despues de la
		// reapertura. Un rechazo silencioso deja el journal limpio y la respuesta vacia.
		[Test]
		public void AndAfterTheReopenThePriceStillAnswersWithItsRevision()
		{
			APriceIsPublishedIn(TheCurrencyTheSetDoesNotName);

			CloseAndReopen();

			Assert.That(api.HasAnyPublishedServicePrice(), Is.True);
			Assert.That(api.PublishedServicePriceCount(), Is.EqualTo(1));
			Assert.That(
				api.IsServicePricePublished(TheOperation, TheMilestone, TheUnit), Is.True);
			Assert.That(
				api.ReadServicePriceRevision(TheOperation, TheMilestone, TheUnit),
				Is.EqualTo(TheRevision),
				"Y LA REVISION SIGUE SELLADA. Es la mitad que PROBE 8 encontro debajo de la otra: la "
				+ "cascada no pierde un precio, pierde el sello, y una revision sin sellar no "
				+ "contesta nada de lo que publico adentro.");
		}

		// LA FRASE, LETRA POR LETRA LA DEL ARBOL SIN MIGRAR. Una moneda sin nombrar no tiene simbolo
		// ni decimales publicados, asi que el monto sale tal como llego y el codigo va detras --
		// exactamente lo que ServicePriceEntry decia antes de tener un Money adentro.
		[Test]
		public void AndTheSentenceIsLetterForLetterWhatTheUnmigratedTreeSaid()
		{
			APriceIsPublishedIn(TheCurrencyTheSetDoesNotName);

			CloseAndReopen();

			string said = api.DescribeServicePrice(
				TheOperation, TheMilestone, TheUnit, WhileItIsInForce);

			TestContext.Out.WriteLine("LA FRASE DESPUES DE REABRIR -- " + said);

			Assert.That(said, Is.EqualTo(
				"Revisión a los 10000 km: 85000.1 XYZ, vigente del 2025-01-01 al 2026-12-31. "
				+ "Al 2025-06-15 esa vigencia está rigiendo. "
				+ "Publicado en la revisión REV-2025-01."));
		}

		// EL CONTROL, y es lo que hace que el rojo de arriba signifique algo. Con la misma migracion
		// y el mismo camino, una moneda que el conjunto SI nombra tambien reabre limpia -- y se dice
		// distinto, con su signo delante y sus dos decimales. Bajo el falsificador este control se
		// queda VERDE: el rojo es de la moneda sin nombrar y no de la migracion.
		[Test]
		public void ACurrencyTheSetNamesReopensCleanTooAndIsSaidWithItsOwnSign()
		{
			APriceIsPublishedIn(TheCurrencyTheSetNames);

			string heard = WhatTheEngineSaysWhenItReopens();

			Assert.That(HowManyTimesItSays(heard, "[Puppeteer ERROR]"), Is.EqualTo(0));
			Assert.That(api.PublishedServicePriceCount(), Is.EqualTo(1));
			Assert.That(
				api.DescribeServicePrice(TheOperation, TheMilestone, TheUnit, WhileItIsInForce),
				Does.Contain("₡85000.10"),
				"Y aca SI hay simbolo y decimales, porque el conjunto la nombra: es la unica "
				+ "diferencia entre los dos caminos, y esta del lado de la frase y no del journal.");
		}

		// LAS DOS MONEDAS EN UN MISMO JOURNAL, que es el caso que ninguna de las dos de arriba mide:
		// que la sin nombrar no contamine a la que si. Dos hitos, dos monedas, una sola reapertura.
		[Test]
		public void BothCurrenciesInOneJournalReopenCleanAndEachIsSaidItsOwnWay()
		{
			api.OpenRevision(TheRevision, PublishedAt);
			api.PublishServicePrice(
				TheRevision, TheOperation, TheMilestone, TheUnit,
				TheAmount, TheCurrencyTheSetDoesNotName, ValidFrom, ValidUntil);
			api.PublishServicePrice(
				TheRevision, "FluidChange", 20000, TheUnit,
				TheAmount, TheCurrencyTheSetNames, ValidFrom, ValidUntil);
			api.SealRevision(TheRevision);

			string heard = WhatTheEngineSaysWhenItReopens();

			Assert.That(HowManyTimesItSays(heard, "[Puppeteer ERROR]"), Is.EqualTo(0));
			Assert.That(api.PublishedServicePriceCount(), Is.EqualTo(2));
			Assert.That(
				api.DescribeServicePrice(TheOperation, TheMilestone, TheUnit, WhileItIsInForce),
				Does.Contain("85000.1 XYZ"));
			Assert.That(
				api.DescribeServicePrice("FluidChange", 20000, TheUnit, WhileItIsInForce),
				Does.Contain("₡85000.10"));
		}

		// LA MITIGACION QUE DEC-87 RECLAMA ES PARCIAL, y esto lo mide en vez de discutirlo. PROBE 9
		// midio que la ESCALA de un decimal no sobrevive al journal: 145.00 escrito sale 145 leido.
		// Money.Expressed tapa esa perdida para una moneda que el conjunto NOMBRA, porque la vuelve a
		// decir con los decimales de esa moneda -- y NO la tapa para una que no nombra, porque ahi el
		// monto sale tal como llego y no hay decimales publicados a los que volver.
		//
		// ASI QUE PARA UNA MONEDA SIN NOMBRAR, LO UNICO QUE IMPIDE QUE VIVO Y REPLAYADO DISCREPEN ES
		// EL Normalized DE LA PUERTA, que esta en el camino de ESCRITURA: le da al decimal la misma
		// forma que va a tener despues de pasar por el journal, antes de que el journal la vea. Por
		// eso las dos frases de abajo coinciden Y dicen la forma normalizada.
		[Test]
		public void AndOnlyTheDoorsNormalizationKeepsTheUnnamedSentenceAgreeingWithItself()
		{
			Currency unnamed = Currency.OfPublished(TheCurrencyTheSetDoesNotName);

			Assert.That(
				new Money(145.00m, unnamed).Expressed(),
				Is.Not.EqualTo(new Money(145m, unnamed).Expressed()),
				"LA MITAD QUE NADIE HABIA ESCRITO: con una moneda sin nombrar la escala SE VE. Los "
				+ "dos montos son el mismo numero y se dicen distinto, asi que si el monto crudo "
				+ "llegara al dominio, vivo diria uno y replayado el otro.");
			Assert.That(
				new Money(145.00m, Currency.CRC).Expressed(),
				Is.EqualTo(new Money(145m, Currency.CRC).Expressed()),
				"Y con una que el conjunto nombra NO se ve, que es la mitigacion que DEC-87 reclama: "
				+ "vale para esta frase y solo para las monedas nombradas.");

			api.OpenRevision(TheRevision, PublishedAt);
			api.PublishServicePrice(
				TheRevision, TheOperation, TheMilestone, TheUnit,
				145.00m, TheCurrencyTheSetDoesNotName, ValidFrom, ValidUntil);
			api.SealRevision(TheRevision);

			string live = api.DescribeServicePrice(
				TheOperation, TheMilestone, TheUnit, WhileItIsInForce);

			CloseAndReopen();

			string replayed = api.DescribeServicePrice(
				TheOperation, TheMilestone, TheUnit, WhileItIsInForce);

			TestContext.Out.WriteLine("VIVO     -- " + live);
			TestContext.Out.WriteLine("REPLAYADO -- " + replayed);

			Assert.That(replayed, Is.EqualTo(live));
			Assert.That(live, Does.Contain("145 " + TheCurrencyTheSetDoesNotName),
				"Y COINCIDEN EN LA FORMA NORMALIZADA, no en la publicada: el Normalized de la puerta "
				+ "ya le quito el cero de mas antes de que hubiera journal. Un comando nuevo que "
				+ "manda el monto crudo esta verde el dia que se escribe y discrepa consigo mismo "
				+ "despues del primer reinicio.");
		}

		private void APriceIsPublishedIn(string currency)
		{
			api.OpenRevision(TheRevision, PublishedAt);
			api.PublishServicePrice(
				TheRevision, TheOperation, TheMilestone, TheUnit,
				TheAmount, currency, ValidFrom, ValidUntil);
			api.SealRevision(TheRevision);
		}

		private void CloseAndReopen()
		{
			Close();
			Open();
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

		// LO QUE EL MOTOR DICE AL REHIDRATAR, escuchando LAS DOS SALIDAS. C70 dice que el rechazo
		// llega por el stream de ERRORES y esa afirmacion sigue SIN VERIFICAR; escuchar una sola
		// dejaria este oraculo verde para siempre, cualquiera de las dos que sea la buena.
		private string WhatTheEngineSaysWhenItReopens()
		{
			TextWriter originalOut = Console.Out;
			TextWriter originalError = Console.Error;
			StringWriter heard = new StringWriter();

			try
			{
				Console.SetOut(heard);
				Console.SetError(heard);

				CloseAndReopen();
			}
			finally
			{
				Console.SetOut(originalOut);
				Console.SetError(originalError);
			}

			return heard.ToString();
		}

		private static int HowManyTimesItSays(string heard, string needle)
		{
			int found = 0;
			int at = heard.IndexOf(needle, StringComparison.Ordinal);

			while (at >= 0)
			{
				found = found + 1;
				at = heard.IndexOf(needle, at + needle.Length, StringComparison.Ordinal);
			}

			return found;
		}

		// El SEGUNDO PROGRAMA que el journal guarda: cada define, cada invoke y cada blob de
		// argumentos, en orden. Se saca leyendo los tramos imprimibles del binario, que es
		// exactamente lo que el motor escribe como texto.
		private static string TheSecondProgramOf(string folder)
		{
			StringBuilder program = new StringBuilder();

			string[] files = Directory.GetFiles(folder, "*", SearchOption.AllDirectories);
			Array.Sort(files, StringComparer.Ordinal);

			foreach (string file in files)
			{
				using (FileStream open = new FileStream(
					file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
				{
					byte[] content = new byte[open.Length];
					open.ReadExactly(content, 0, content.Length);

					StringBuilder run = new StringBuilder();

					foreach (byte each in content)
					{
						if (each >= 0x20 && each <= 0x7E) { run.Append((char)each); continue; }

						if (run.Length >= 3) program.AppendLine(run.ToString());

						run.Clear();
					}

					if (run.Length >= 3) program.AppendLine(run.ToString());
				}
			}

			return program.ToString();
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
