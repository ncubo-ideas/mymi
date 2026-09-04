using System;
using System.IO;
using Choreography.Theater;
using DealershipCar.App;
using NUnit.Framework;
using Puppeteer;

namespace DealershipCar.Dominio.Pruebas
{
	// LA PRUEBA 12 DE SLICE-OdometerSeries.md, Y ES LA PRUEBA DE UNA AUSENCIA. Un journal escrito
	// por la app de ANTES de esta rebanada se abre con la app de HOY y las SEIS lecturas de la serie
	// contestan sobre ese mismo registro, sin que se le aplique nada primero. NO HAY MIGRACION AQUI:
	// esta prueba demuestra que no hay ninguna, no ejecuta una. OD- no agrega global de raiz, ni
	// nombre de upgrade, ni comando, ni clase de evento.
	//
	// POR QUE HACE FALTA SI YA EXISTE NothingMovedAndThereWasNoMigrationToPerform. Porque esa es la
	// prueba 18 de MS-, y su mitad de "y las lecturas NUEVAS contestan" enumera las diecinueve de
	// MS-. Las seis de esta rebanada CAEN del otro lado de esa resta: no estan en esa lista, asi que
	// el barrido de "lo que ya existia" SI las invoca -- y ahi esta el punto -- pero con los dos
	// limites armados por TIPO, que le pasa el mismo instante dos veces. La unica lectura de aquel
	// fixture se observo el 2026-01-20 y el barrido pregunta con el 2026-02-01, asi que el periodo
	// [2026-02-01, 2026-02-01] no tiene ninguna lectura y las seis contestan SU AUSENCIA. La
	// comparacion de aquel test seguiria pasando aunque las seis devolvieran constantes: es la forma
	// de C60, dos respuestas derivadas comparadas entre si sin que nada las ancle al miembro que las
	// dio. Lo que falta, y es lo que se escribe aca, es la mitad que afirma cada una sobre un valor
	// que solo ella puede dar.
	//
	// EL INSTRUMENTO DEL COSTO DE LA APERTURA ES EL MISMO Y NO SE VUELVE A MEDIR AQUI. +1 es lo que
	// cuesta una apertura en regimen y +2 lo que cuesta un texto de script que este actor nunca vio
	// -- SH-3, quinta global. Sus dos limites ya estan medidos y escritos en el encabezado de
	// NothingMovedAndThereWasNoMigrationToPerform: no ve un cuerpo de upgrade REFORMATEADO, y una
	// comparacion entre dos hosts no ve una lectura de la puerta que cambio, porque los dos lados se
	// mueven juntos. Aca se toma el mismo +1 sobre OTRA secuencia recibida, y el control positivo va
	// en el mismo test con una forma distinta a proposito: un SEXTO upgrade, que es exactamente la
	// forma que tendria la migracion que esta rebanada promete no tener.
	//
	// LA SECUENCIA RECIBIDA ES LA QUE §1.1 DESCRIBE, Y NO UNA CUALQUIERA. Cinco lecturas escritas
	// por la app vieja, cuatro en UnitA y una en UnitB, aceptadas todas: la guarda de insercion
	// compara toda lectura anterior y toda posterior, pero SOLO contra las que ya estan en la misma
	// unidad, asi que la de UnitB no se compara con ninguna. Una serie de unidades mezcladas ya era
	// almacenable antes de esta rebanada y nada la habia leido nunca -- estas seis son lo primero
	// que la mira entera, y esta prueba es donde eso cruza la frontera del journal viejo.
	[TestFixture]
	public sealed class TheSeriesWasAlreadyInTheJournalAndThereWasNoMigrationToPerform
	{
		private const string TheVin = "JH4KA7561PC008270";
		private const string TheModel = "SM-SEDAN-2025";

		private const string TheUnitOfThePeriod = "UnitA";
		private const string TheOtherUnit = "UnitB";
		private const string TheDeviceKey = "device-de-la-ausencia-de-la-serie";

		// LAS CINCO LECTURAS, escritas por la app de antes con el comando que ya existia.
		private const int TheFirstReading = 13200;
		private const int TheSecondReading = 14500;
		private const int TheThirdReading = 17000;
		private const int TheReadingInTheOtherUnit = 9000;
		private const int TheLastReading = 20000;

		private static readonly DateTime TheFirstObservation =
			new DateTime(2025, 10, 1, 0, 0, 0, DateTimeKind.Utc);
		private static readonly DateTime TheSecondObservation =
			new DateTime(2025, 11, 15, 0, 0, 0, DateTimeKind.Utc);
		private static readonly DateTime TheThirdObservation =
			new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc);
		private static readonly DateTime TheObservationInTheOtherUnit =
			new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc);
		private static readonly DateTime TheLastObservation =
			new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);

		// LOS DOS PERIODOS, escritos como literales. El primero tiene las tres lecturas de UnitA y
		// contesta un ritmo medido; el segundo abarca la de UnitB y contesta la suspension por
		// unidades. Los dos limites son inclusivos.
		private static readonly DateTime TheMeasurablePeriodFrom = TheFirstObservation;
		private static readonly DateTime TheMeasurablePeriodTo = TheThirdObservation;
		private static readonly DateTime TheMixedPeriodFrom = TheFirstObservation;
		private static readonly DateTime TheMixedPeriodTo = TheLastObservation;

		private string folder;

		[SetUp]
		public void Setup()
		{
			folder = ATemporaryFolder();
		}

		[TearDown]
		public void TearDown()
		{
			Sweep(folder);
		}

		// LA AUSENCIA DE LA MIGRACION, MEDIDA EN ENTRADAS. Sobre un journal que YA TIENE la serie
		// escrita por la app de antes, abrir con la de hoy cuesta lo que cuesta cualquier apertura.
		// El control positivo es un SEXTO upgrade: la forma que tendria la migracion que esta
		// rebanada no tiene, y que cuesta +2 sin que ninguno de los cinco cuerpos viejos vuelva a
		// correr.
		[Test]
		public void AJournalWithTheSeriesAlreadyInItCostsTheOpeningItAlwaysCostAndNothingMore()
		{
			long afterTheOlderAppWrote = TheSeriesWrittenByTheOlderApp();

			DealershipCarApp today = TheAppOfTodayOpening(folder);
			long afterTodaysApp = today.Actor.CurrentEntryId;
			today.Dispose();

			TestContext.Out.WriteLine(
				"COSTO DE ABRIR UN JOURNAL DE ANTES DE OD- CON LA APP DE HOY -- CurrentEntryId: "
				+ afterTheOlderAppWrote + " -> " + afterTodaysApp);

			Assert.That(afterTodaysApp, Is.EqualTo(afterTheOlderAppWrote + 1),
				"UNA entrada, la que toda apertura paga por el comando de la semilla. DOS querria "
				+ "decir que corrio un cuerpo de upgrade nuevo o que el texto del script cambio, y "
				+ "las dos cosas son una migracion. OD- no agrega global, ni nombre de upgrade, ni "
				+ "comando: los dos textos son el mismo, y este numero es lo que lo dice.");

			DealershipCarApp again = TheAppOfTodayOpening(folder);
			long afterASecondOpening = again.Actor.CurrentEntryId;
			again.Dispose();

			Assert.That(afterASecondOpening, Is.EqualTo(afterTodaysApp + 1),
				"Y sigue costando una. Si la primera hubiera migrado algo, la segunda seria mas "
				+ "barata que la primera y este renglon lo mostraria.");

			// EL CONTROL POSITIVO, y sin el un +1 constante pasaria igual. Un SEXTO upgrade sobre el
			// mismo texto de los cinco es otra plantilla, y otra plantilla cuesta +2. Se escoge esta
			// forma y no un reordenamiento porque es LA QUE TENDRIA esta rebanada si hubiera
			// necesitado una global -- que es lo que §8 del brief dice que no necesita.
			string elsewhere = ATemporaryFolder();

			try
			{
				AnAppFromBeforeTheOdometerSeries older =
					new AnAppFromBeforeTheOdometerSeries(TheVin, TheModel);
				older.ConfigureStorage(DatabaseType.FileSystem, "path=" + elsewhere);
				older.Start();

				long beforeTheSixth = older.Actor.CurrentEntryId;
				older.Dispose();

				AnAppThatAddsASixthGlobal migrating =
					new AnAppThatAddsASixthGlobal(TheVin, TheModel);
				migrating.ConfigureStorage(DatabaseType.FileSystem, "path=" + elsewhere);
				migrating.Start();

				long afterTheSixth = migrating.Actor.CurrentEntryId;
				migrating.Dispose();

				TestContext.Out.WriteLine(
					"EL CONTROL -- una sexta global sobre el mismo journal: " + beforeTheSixth
					+ " -> " + afterTheSixth);

				Assert.That(afterTheSixth, Is.EqualTo(beforeTheSixth + 2),
					"+2 ES LO QUE CUESTA MIGRAR, y es lo que hace que el +1 de arriba signifique "
					+ "algo: el instrumento SI distingue una apertura que aplica algo de una que no.");
			}
			finally
			{
				Sweep(elsewhere);
			}
		}

		// LA MITAD QUE FALTA: "y las lecturas nuevas contestan", sobre el journal que escribio la app
		// de antes y SIN aplicarle nada. Cada una de las seis nombrada, y cada afirmacion sobre un
		// valor que solo esa lectura puede dar (C60): una de por si no vacia pasaria aunque las seis
		// contestaran todas lo mismo, que es exactamente lo que pasa en el barrido por tipo de la
		// prueba 18 de MS-.
		[Test]
		public void AndTheSixReadsOfTheSeriesAnswerOnThatSameUnmigratedJournal()
		{
			TheSeriesWrittenByTheOlderApp();

			DealershipCarApp today = TheAppOfTodayOpening(folder);
			DealershipCarAPI api = new DealershipCarAPI(today);

			try
			{
				// 1. LA GUARDA -- puede decirse en el periodo de una sola unidad y no en el mixto.
				Assert.That(
					api.CanARateOfUseBeSaid(TheMeasurablePeriodFrom, TheMeasurablePeriodTo),
					Is.True,
					"Tres lecturas en una sola unidad dentro del periodo: el ritmo se puede decir.");
				Assert.That(
					api.CanARateOfUseBeSaid(TheMixedPeriodFrom, TheMixedPeriodTo),
					Is.False,
					"El periodo largo abarca la lectura en la otra unidad, y las unidades nunca se "
					+ "convierten.");

				// 2. LA SITUACION CON NOMBRE -- dos de las cinco, sobre el mismo registro.
				Assert.That(
					api.ReadRateOfUseStanding(TheMeasurablePeriodFrom, TheMeasurablePeriodTo),
					Is.EqualTo("Measured"));
				Assert.That(
					api.ReadRateOfUseStanding(TheMixedPeriodFrom, TheMixedPeriodTo),
					Is.EqualTo("SuspendedByAUnitMismatch"),
					"§1.1 CRUZANDO LA FRONTERA DEL JOURNAL VIEJO: la serie de unidades mezcladas la "
					+ "escribio la app de antes, la guarda de insercion la acepto porque no compara "
					+ "a traves de unidades, y esta lectura es lo primero que la mira entera.");

				// 3. LA FRASE -- los dos extremos, la distancia entre ellos y el ritmo por dia.
				Assert.That(
					api.DescribeRateOfUse(TheMeasurablePeriodFrom, TheMeasurablePeriodTo),
					Does.Contain("de " + TheFirstReading + " km el 2025-10-01 00:00:00")
						.And.Contain("a " + TheThirdReading + " km el 2026-01-20 00:00:00")
						.And.Contain((TheThirdReading - TheFirstReading) + " km en 111 días")
						.And.Contain("unos 34 km por día"),
					"L7: el tramo se dice con el ritmo. 3800 km en 111 dias son 34,23 por dia, "
					+ "redondeados a 34 y presentados con 'unos' -- DEC-77.");
				Assert.That(
					api.DescribeRateOfUse(TheMixedPeriodFrom, TheMixedPeriodTo),
					Does.Contain("lecturas en km y en mi")
						.And.Contain(TheReadingInTheOtherUnit + " mi del 2026-02-10 00:00:00"),
					"L3 y L11: nombra las dos unidades Y la lectura que introdujo la segunda, con "
					+ "su fecha.");

				// 4. LOS DOS PERIODOS COMPARADOS -- la unica lectura de las seis que toma cuatro
				// limites, y la unica que puede decir que uno de los dos periodos no tiene ritmo.
				string compared = api.DescribeTwoPeriodsOfUseCompared(
					TheMixedPeriodFrom, TheMixedPeriodTo,
					TheMeasurablePeriodFrom, TheMeasurablePeriodTo);

				Assert.That(compared,
					Does.Contain("Primer periodo:").And.Contain("Segundo periodo:")
						.And.Contain("No se puede decir el cambio"),
					"L4: nombra los dos periodos y no dice un cambio cuando uno de los dos no tiene "
					+ "ritmo que decir. El orden es contrato: el cambio se dice del PRIMERO respecto "
					+ "al segundo.");

				// 5. LAS FILAS -- las cinco columnas, con la unidad y la fuente de cada fila. La
				// serie sigue contestando entera aunque el ritmo del periodo este suspendido: un
				// registro que escondiera las filas cuando la frase dice 'suspendido' deshace G5 en
				// silencio.
				OdometerSeriesRecord rows =
					api.ReadingsBetween(TheMixedPeriodFrom, TheMixedPeriodTo);

				Assert.That(rows.Value, Is.EqualTo(new[]
				{
					TheFirstReading, TheSecondReading, TheThirdReading,
					TheReadingInTheOtherUnit, TheLastReading,
				}),
					"En orden de OBSERVACION, que es el orden en que la agenda las guarda.");
				Assert.That(rows.Unit,
					Is.EqualTo(new[] { "km", "km", "km", "mi", "km" }),
					"C67: la unidad se dice dos veces -- UnitA a la maquina y km a la persona -- y "
					+ "la cuarta fila es la que esta en la otra.");
				Assert.That(rows.Source,
					Is.EqualTo(new[]
					{
						"Telemetry", "HumanEntry", "CustomerDeclared", "Inspection", "Correction",
					}),
					"L5: cada fila dice de que fuente vino, y las cinco fuentes del conjunto llegaron "
					+ "escritas por la app de antes.");
				Assert.That(rows.Says[3],
					Is.EqualTo(TheReadingInTheOtherUnit + " mi el 2026-02-10 00:00:00, leída en una "
						+ "revisión"),
					"La frase de la fila la compone el dominio entera, con la fuente adentro.");
				Assert.That(rows.ObservedAt[0], Is.EqualTo("2025-10-01 00:00:00"));

				// 6. EL CONTEO -- la unica de las seis que contesta con un numero, y es un conteo de
				// filas y no una mitad de nada.
				Assert.That(
					api.ReadingCountBetween(TheMixedPeriodFrom, TheMixedPeriodTo), Is.EqualTo(5));
				Assert.That(
					api.ReadingCountBetween(TheMeasurablePeriodFrom, TheMeasurablePeriodTo),
					Is.EqualTo(3),
					"Y discrimina el periodo: dos limites distintos dan dos conteos distintos.");
			}
			finally
			{
				today.Dispose();
			}
		}

		// QUE LAS SEIS LEEN EL JOURNAL Y NO EL PROCESO QUE LAS CONTESTA. La MISMA clase de app sobre
		// una carpeta vacia contesta la ausencia en las seis, y su CurrentEntryId es menor. Sin este
		// control, "se reabrio y contesto" es una suposicion sobre un proceso que pudo sembrarse
		// solo.
		[Test]
		public void TheSeriesAnswersAreReadFromTheJournalAndNotFromTheAppThatIsAnsweringThem()
		{
			long afterTheOlderAppWrote = TheSeriesWrittenByTheOlderApp();

			DealershipCarApp today = TheAppOfTodayOpening(folder);
			long onTheWrittenJournal = today.Actor.CurrentEntryId;
			today.Dispose();

			string empty = ATemporaryFolder();

			try
			{
				DealershipCarApp fresh = TheAppOfTodayOpening(empty);
				DealershipCarAPI api = new DealershipCarAPI(fresh);

				long onAFreshJournal = fresh.Actor.CurrentEntryId;

				TestContext.Out.WriteLine(
					"CurrentEntryId sobre el journal escrito: " + onTheWrittenJournal
					+ " -- sobre una carpeta vacia: " + onAFreshJournal
					+ " -- lo que dejo escrito la app de antes: " + afterTheOlderAppWrote);

				Assert.That(
					api.CanARateOfUseBeSaid(TheMixedPeriodFrom, TheMixedPeriodTo), Is.False);
				Assert.That(
					api.ReadRateOfUseStanding(TheMixedPeriodFrom, TheMixedPeriodTo),
					Is.EqualTo("SuspendedByAnAbsentReading"),
					"Sin journal la suspension es OTRA: por ausencia y no por unidades. Las dos "
					+ "situaciones se distinguen, asi que la de arriba salio de los bytes.");
				Assert.That(
					api.ReadingCountBetween(TheMixedPeriodFrom, TheMixedPeriodTo), Is.EqualTo(0));
				Assert.That(
					api.ReadingsBetween(TheMixedPeriodFrom, TheMixedPeriodTo).Value, Is.Empty);
				Assert.That(
					api.DescribeRateOfUse(TheMixedPeriodFrom, TheMixedPeriodTo),
					Does.Not.Contain("por día"),
					"Y ninguna de las seis revienta sobre una carpeta vacia: son TOTALES.");
				Assert.That(
					api.DescribeTwoPeriodsOfUseCompared(
						TheMixedPeriodFrom, TheMixedPeriodTo,
						TheMeasurablePeriodFrom, TheMeasurablePeriodTo),
					Does.Contain("No se puede decir el cambio"));

				fresh.Dispose();

				Assert.That(onAFreshJournal, Is.LessThan(onTheWrittenJournal),
					"Y el actor de hoy sabe cuantas entradas traia el journal, que es un numero que "
					+ "solo se puede saber leyendolo.");
			}
			finally
			{
				Sweep(empty);
			}
		}

		// ===== LA SECUENCIA RECIBIDA, ESCRITA POR LA APP DE ANTES ==========================

		// RecordReading existe desde antes de esta rebanada y su firma no se toco (L10). Devuelve el
		// CurrentEntryId con que quedo el journal.
		private long TheSeriesWrittenByTheOlderApp()
		{
			AnAppFromBeforeTheOdometerSeries older =
				new AnAppFromBeforeTheOdometerSeries(TheVin, TheModel);
			older.ConfigureStorage(DatabaseType.FileSystem, "path=" + folder);
			older.Start();

			DealershipCarAPI api = new DealershipCarAPI(older);

			api.RegisterDevice(TheDeviceKey);

			api.RecordReading(
				TheFirstReading, TheUnitOfThePeriod, TheFirstObservation, "Telemetry");
			api.RecordReading(
				TheSecondReading, TheUnitOfThePeriod, TheSecondObservation, "HumanEntry");
			api.RecordReading(
				TheThirdReading, TheUnitOfThePeriod, TheThirdObservation, "CustomerDeclared");

			// LA LECTURA EN LA OTRA UNIDAD, y entra. La guarda compara toda anterior y toda
			// posterior, pero solo contra las que ya estan en la misma unidad: contra esta no hay
			// ninguna, asi que no se compara con nada. Es §1.1 escrito con el comando de antes.
			api.RecordReading(
				TheReadingInTheOtherUnit, TheOtherUnit, TheObservationInTheOtherUnit, "Inspection");

			api.RecordReading(
				TheLastReading, TheUnitOfThePeriod, TheLastObservation, "Correction");

			long written = older.Actor.CurrentEntryId;

			older.Dispose();

			return written;
		}

		private static DealershipCarApp TheAppOfTodayOpening(string where)
		{
			DealershipCarApp app = new DealershipCarApp(TheVin, "Standard", TheModel);
			app.ConfigureStorage(DatabaseType.FileSystem, "path=" + where);
			app.Start();

			return app;
		}

		private static string ATemporaryFolder()
		{
			string made = Path.Combine(
				AScheduleOnARealJournal.TheRootOfEveryTemporaryJournal, Guid.NewGuid().ToString("N"));

			Directory.CreateDirectory(made);

			return made;
		}

		private static void Sweep(string where)
		{
			try
			{
				if (Directory.Exists(where)) Directory.Delete(where, true);
			}
			catch (IOException)
			{
			}
			catch (UnauthorizedAccessException)
			{
			}
		}

		// LA APP TAL COMO ERA ANTES DE ESTA REBANADA, y son los MISMOS cinco upgrades de hoy porque
		// OD- no agrego ninguno. Los cinco cuerpos estan copiados palabra por palabra de
		// DealershipCarApp.OnHydrated, y esa copia ES la prueba: lo que se mide es que los dos textos
		// calzan letra por letra. Copiar y no heredar tambien es a proposito: DealershipCarApp es
		// sealed, y sobreescribir OnHydrated reemplazaria los cinco.
		private sealed class AnAppFromBeforeTheOdometerSeries : PerformanceV2
		{
			private readonly string vin;
			private readonly string modelCode;

			internal AnAppFromBeforeTheOdometerSeries(string vin, string modelCode)
				: base(DealershipCarApp.TheActorFor(vin), typeof(MaintenanceSchedule).Assembly)
			{
				this.vin = vin;
				this.modelCode = modelCode;
			}

			protected override void OnHydrated()
			{
				Using(@"
					upgrade('semilla') {
						schedule = MaintenanceSchedule(@vin, @usage);
					}
					upgrade('bitacora-de-eventos') {
						logbook = Logbook();
					}
					upgrade('especificacion-del-modelo') {
						specification = ModelSpecification(@model);
					}
					upgrade('perfil-del-vehiculo') {
						profile = VehicleProfile(@vin);
					}
					upgrade('estadias-en-taller') {
						stays = ShopStay(@vin);
					}
				")
				.WithParameters(p => {
					p["vin",   typeof(string)] = vin;
					p["usage", typeof(Enum)]   = "Standard";
					p["model", typeof(string)] = modelCode;
				})
				.PerformCommand();
			}
		}

		// EL CONTROL POSITIVO: los cinco de siempre MAS uno. Es la forma exacta que tendria la
		// migracion que esta rebanada promete no tener -- una global de raiz nueva con su nombre de
		// upgrade -- y cuesta +2 sobre un journal que ya aplico los cinco.
		private sealed class AnAppThatAddsASixthGlobal : PerformanceV2
		{
			private readonly string vin;
			private readonly string modelCode;

			internal AnAppThatAddsASixthGlobal(string vin, string modelCode)
				: base(DealershipCarApp.TheActorFor(vin), typeof(MaintenanceSchedule).Assembly)
			{
				this.vin = vin;
				this.modelCode = modelCode;
			}

			protected override void OnHydrated()
			{
				Using(@"
					upgrade('semilla') {
						schedule = MaintenanceSchedule(@vin, @usage);
					}
					upgrade('bitacora-de-eventos') {
						logbook = Logbook();
					}
					upgrade('especificacion-del-modelo') {
						specification = ModelSpecification(@model);
					}
					upgrade('perfil-del-vehiculo') {
						profile = VehicleProfile(@vin);
					}
					upgrade('estadias-en-taller') {
						stays = ShopStay(@vin);
					}
					upgrade('una-sexta-global-que-esta-rebanada-no-agrega') {
						second = Logbook();
					}
				")
				.WithParameters(p => {
					p["vin",   typeof(string)] = vin;
					p["usage", typeof(Enum)]   = "Standard";
					p["model", typeof(string)] = modelCode;
				})
				.PerformCommand();
			}
		}
	}
}
