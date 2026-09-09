using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Choreography.Theater;
using Concesionario.App;
using NUnit.Framework;
using Puppeteer;

namespace Concesionario.Dominio.Pruebas
{
	// LO QUE LA PUERTA EXIGE EN EL BORDE ES LO QUE SE PUEDE DECIR, y no las leyes del libro. Un monto
	// negativo no es dinero, un gasto sin asunto no dice en que se fue la plata, y una vida util que
	// no es positiva no deja ninguna ventana sobre la que repartir nada: las tres las exige el DOMINIO
	// y las tres se adelantan aca, al unico momento en que rechazarlas todavia es gratis.
	//
	// CUANTO SUMAN LOS GASTOS, EN QUE ORDEN VAN Y COMO SE REPARTE UNO DURADERO ES LEY DEL DOMINIO y no
	// se copia aca. Es la misma linea que SH-3 trazo con los dias de una estadia.
	//
	// POR QUE ADELANTARLAS: para cuando la guarda del dominio corre, la entrada del comando YA SE
	// ESCRIBIO. El rechazo queda en el journal de la persona haciendo loguear un fallo de rehidratacion
	// en cada arranque futuro, aunque el estado siga siendo correcto -- C22/C36/C37, y medido en
	// ARefusedCommandCostsAJournalEntryUnlessItIsGuarded. La misma negativa una llamada antes no cuesta
	// un byte.
	//
	// Y CADA UNA TIENE SU PROPIA PRUEBA DIRECTA QUE AFIRMA LO QUE EL RECHAZO DICE. CL-2 midio TRES
	// guardas que al borrarlas ponian CERO pruebas rojas, y CL-3 midio una CUARTA en codigo escrito el
	// mismo dia: una guarda cuyo rechazo es indistinguible del de al lado no la defiende nada. Aca el
	// discriminador es el MENSAJE.
	//
	// BYTE A BYTE ES EL CHEQUEO CORRECTO Y ES LA EXCEPCION QUE C23 NOMBRA: dos corridas de la misma
	// secuencia recibida NO son byte-identicas, porque cada una anota su instante de ocurrencia.
	// Byte-identico es lo que hay que exigirle a una operacion que tiene que escribir NADA, y un
	// comando rechazado es esa operacion.
	[TestFixture]
	public sealed class TheDoorRefusesACostThatCannotBeSaidBeforePerforming
	{
		private const string TheVin = "5YJ3E1EA7JF000123";
		private const string TheModel = "YARIS-2019";

		private const string TheSubject = "Llantas nuevas";
		private const string TheCurrency = "CRC";
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

			app = new DealershipCarApp(TheVin, "Standard", TheModel);
			app.ConfigureStorage(DatabaseType.FileSystem, "path=" + folder);
			app.Start();

			api = new DealershipCarAPI(app);
		}

		[TearDown]
		public void TearDown()
		{
			if (app != null) app.Dispose();

			AFreshJournalFolder.Sweep(folder);
		}

		// UN MONTO NEGATIVO, POR LOS DOS COMANDOS. Y el mensaje dice POR QUE, que es el discriminador:
		// sin el, esta guarda seria indistinguible de la que exige la vida util.
		[Test]
		public void AnAmountThatIsNotMoneyIsRefusedAndTheRefusalSaysWhyItIsNotMoney()
		{
			string before = TheBytesOf(folder);

			ArgumentOutOfRangeException recurring = Assert.Throws<ArgumentOutOfRangeException>(
				() => api.RecordRecurringCost(TheCostIncurred, -1m, TheCurrency, TheSubject));

			ArgumentOutOfRangeException durable = Assert.Throws<ArgumentOutOfRangeException>(
				() => api.RecordDurableCost(
					TheCostIncurred, -240000m, TheCurrency, TheSubject, FourYearsOfLife));

			TestContext.Out.WriteLine("MONTO NEGATIVO -- " + recurring.Message);

			foreach (ArgumentOutOfRangeException refusal in new[] { recurring, durable })
			{
				Assert.That(refusal.ParamName, Is.EqualTo("amount"));
				Assert.That(refusal.Message, Does.Contain("pagandole a la persona"),
					"EL MENSAJE ES EL DISCRIMINADOR. Las tres guardas de este borde tiran el mismo "
					+ "tipo de excepcion; lo unico que separa 'esto no es dinero' de 'esto no es una "
					+ "vida util' es lo que cada una dice.");
			}

			Assert.That(TheBytesOf(folder), Is.EqualTo(before),
				"Los dos, byte a byte igual: ninguno llego a haber comando.");
		}

		// Y CERO SI ES UN MONTO. Sin este renglon la guarda de arriba podria ser un '<= 0' y nadie lo
		// notaria: un servicio incluido en la garantia tambien es un gasto que hubo, y decir que no se
		// puede registrar seria perder la fila.
		[Test]
		public void AnAmountOfZeroIsMoneyAndIsRecorded()
		{
			api.RecordRecurringCost(TheCostIncurred, 0m, TheCurrency, "Revisión incluida");

			Assert.That(
				api.CostEntryCountBetween(TheYearFrom, TheYearTo),
				Is.EqualTo(1),
				"Cero es un monto. Un servicio incluido tambien se registra, y su fila es lo que "
				+ "distingue 'no costo nada' de 'no paso'.");
		}

		// UN GASTO SIN ASUNTO, POR LOS DOS COMANDOS Y EN LAS TRES FORMAS DE NO DECIR NADA. El asunto
		// es texto RECIBIDO -- G3 -- asi que la puerta exige que diga algo y no le toca lo que dice:
		// no hay ninguna categoria por defecto que ponerle, porque en que se gasto lo sabe quien lo
		// gasto.
		//
		// SE ATRAPA POR LA FAMILIA Y SE DISCRIMINA POR EL PARAMETRO, por lo mismo que la moneda: un
		// null sale como ArgumentNullException, que DERIVA de ArgumentException, y las dos las tira el
		// mismo ThrowIfNullOrWhiteSpace del BCL. Lo que separa este rechazo del de al lado es QUE
		// parametro nombra.
		[Test]
		public void ACostWithNoSubjectDoesNotSayWhereTheMoneyWentAndIsRefused()
		{
			string before = TheBytesOf(folder);

			foreach (string blank in new[] { null, "", "   " })
			{
				string said = blank;

				Assert.That(
					Assert.Catch<ArgumentException>(
						() => api.RecordRecurringCost(
							TheCostIncurred, TheAmount, TheCurrency, said)).ParamName,
					Is.EqualTo("subject"));

				Assert.That(
					Assert.Catch<ArgumentException>(
						() => api.RecordDurableCost(
							TheCostIncurred, TheAmount, TheCurrency, said, FourYearsOfLife))
						.ParamName,
					Is.EqualTo("subject"));
			}

			Assert.That(TheBytesOf(folder), Is.EqualTo(before),
				"Los seis, byte a byte igual.");
		}

		// Y EL ASUNTO CRUZA TAL COMO LLEGO, SIN RECORTAR. Es lo contrario de lo que la puerta le hace
		// a un codigo de revision o a un codigo de sistema: aquellos son CLAVES y quien publica y
		// quien pregunta tienen que nombrar la misma cosa, asi que se recortan. Este es la frase de
		// una persona y nadie le arregla la ortografia.
		[Test]
		public void TheSubjectCrossesExactlyAsItArrivedAndTheDoorDoesNotTidyIt()
		{
			api.RecordRecurringCost(TheCostIncurred, TheAmount, TheCurrency, "  Llantas nuevas  ");

			Assert.That(
				api.CostEntriesBetween(TheYearFrom, TheYearTo).Says[0],
				Does.Contain("  Llantas nuevas  "),
				"Con sus espacios. Recortarlo seria corregir un hecho recibido, que es lo que la "
				+ "puerta hace con una CLAVE y lo que no hace con una frase.");
		}

		// UNA VIDA UTIL QUE NO ES UNA VIDA UTIL. Es la prueba 5 del brief con el rechazo puesto donde
		// es gratis, y el mensaje nombra la entrada -- que le falta la vida util -- y por donde va lo
		// que no la lleva.
		[Test]
		public void ADurableCostWhoseExpectedLifeIsNotPositiveIsRefusedAndTheRefusalSaysWhatIsMissing()
		{
			string before = TheBytesOf(folder);

			foreach (int months in new[] { 0, -1, -48 })
			{
				int said = months;

				ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
					() => api.RecordDurableCost(
						TheCostIncurred, TheAmount, TheCurrency, TheSubject, said));

				Assert.That(refusal.ParamName, Is.EqualTo("expectedLifeMonths"));
				Assert.That(refusal.Message, Does.Contain("ninguna ventana"),
					"EL MENSAJE ES EL DISCRIMINADOR, igual que arriba: dice que es lo que una vida "
					+ "util no positiva no deja, que es lo unico que esta guarda compra.");
				Assert.That(refusal.Message, Does.Contain("el otro comando"),
					"Y dice por donde va lo que no lleva vida util, que es lo que le deja a quien "
					+ "registra por donde arreglarlo sin adivinar.");
			}

			TestContext.Out.WriteLine(
				"VIDA UTIL NO POSITIVA -- " + Assert.Throws<ArgumentOutOfRangeException>(
					() => api.RecordDurableCost(
						TheCostIncurred, TheAmount, TheCurrency, TheSubject, 0)).Message);

			Assert.That(TheBytesOf(folder), Is.EqualTo(before),
				"Los cuatro, byte a byte igual.");
		}

		// UN MES SI ES UNA VIDA UTIL. El mismo motivo que el cero del monto: sin este renglon la
		// guarda podria exigir mas de uno y nadie lo notaria.
		[Test]
		public void AnExpectedLifeOfOneMonthIsAnExpectedLifeAndIsRecorded()
		{
			api.RecordDurableCost(TheCostIncurred, TheAmount, TheCurrency, TheSubject, 1);

			Assert.That(
				api.CostEntriesBetween(TheYearFrom, TheYearTo).Says[0],
				Does.Contain("1 mes"),
				"Y el dominio lo dice en singular, que es suyo y no de la puerta.");
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
