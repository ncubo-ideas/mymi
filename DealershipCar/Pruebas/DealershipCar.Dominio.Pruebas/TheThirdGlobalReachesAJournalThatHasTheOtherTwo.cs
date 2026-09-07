using System;
using System.IO;
using Choreography.Theater;
using DealershipCar.App;
using NUnit.Framework;
using Puppeteer;

namespace DealershipCar.Dominio.Pruebas
{
	// LA MISMA TRAMPA QUE TheSecondGlobalReachesAJournalThatOnlyHasTheSeed, una global mas tarde.
	// T5 le agrega a la app una TERCERA global de raiz -- la especificacion del modelo -- al lado de
	// la agenda y de la bitacora. No va adentro de ninguna de las dos: lo que el fabricante publica
	// sobre un MODELO no es un hecho de ESTE vehiculo, y meterlo en la agenda seria el error de
	// modelado, no la solucion.
	//
	// Y LA GUARDA DE upgrade ES UNA-VEZ-POR-ACTOR. upgrade('semilla') y upgrade('bitacora-de-eventos')
	// YA CORRIERON en todo journal de DealershipCar que exista. Una linea agregada ADENTRO de
	// cualquiera de los dos no le llegaria a ninguno de ellos, en silencio, y editar el cuerpo de un
	// upgrade ya aplicado revienta con LanguageException. Asi que la global nueva llega bajo un
	// NOMBRE NUEVO, y esta prueba abre un journal con la app de antes de T5, lo cierra, y lo vuelve a
	// abrir con la de verdad.
	//
	// EL RENGLON QUE HACE QUE ESTA PRUEBA PUEDA DISTINGUIR ALGO es el que mide la ausencia: antes de
	// la migracion, leer la tercera global revienta con "Unknown property or method ... on type
	// 'Object'", porque una global de raiz sin asignar resuelve a un Object pelado. Sin ese renglon
	// todo lo de abajo estaria verde aunque 'semilla' hubiera creado las tres globales.
	//
	// LOS TRES UPGRADES VIAJAN EN UN SOLO SCRIPT. El costo por apertura esta medido en
	// TheSeedStillCostsExactlyOneEntryPerOpening, que sigue en UNA entrada; lo que se mide aca es la
	// otra mitad, que es lo que cuesta la migracion en si: una entrada extra, una sola vez.
	[TestFixture]
	public sealed class TheThirdGlobalReachesAJournalThatHasTheOtherTwo
	{
		private const string TheVin = "1HGCM82633A004352";
		private const string TheModel = "YARIS-2019";
		private const string AnotherModel = "SM-COUPE-2027";

		private string folder;

		[SetUp]
		public void Setup()
		{
			folder = Path.Combine(
				AScheduleOnARealJournal.TheRootOfEveryTemporaryJournal,
				Guid.NewGuid().ToString("N"));

			Directory.CreateDirectory(folder);
		}

		[TearDown]
		public void TearDown()
		{
			try
			{
				if (Directory.Exists(folder)) Directory.Delete(folder, true);
			}
			catch (IOException)
			{
			}
			catch (UnauthorizedAccessException)
			{
			}
		}

		[Test]
		public void AJournalWrittenBeforeTheSpecificationExistedGetsItOnItsNextOpening()
		{
			// Un journal como los que ya existen: tiene 'semilla' y 'bitacora-de-eventos'.
			AnAppFromBeforeTheSpecification older = new AnAppFromBeforeTheSpecification(TheVin);
			older.ConfigureStorage(DatabaseType.FileSystem, "path=" + folder);
			older.Start();

			DealershipCarAPI before = new DealershipCarAPI(older);

			Assert.That(before.ReadVin(), Is.EqualTo(TheVin),
				"La agenda si existe: 'semilla' corrio.");
			Assert.That(before.LastEventApplied(), Is.EqualTo(0L),
				"Y la bitacora tambien: 'bitacora-de-eventos' corrio.");

			Exception missing = Assert.Catch(() => TheModelTheJournalAnswers(older));

			Assert.That(missing.Message, Does.Contain("ReadModel"),
				"Y la especificacion NO existe todavia, que es lo que hace que esta prueba pueda "
				+ "distinguir la migracion de no haber hecho nada. Una global de raiz sin asignar "
				+ "resuelve a un Object pelado, asi que preguntarle cualquier cosa revienta "
				+ "nombrando lo que se le pidio. Sin este renglon, el de abajo estaria verde aunque "
				+ "'semilla' hubiera creado las tres globales.");

			older.Dispose();

			// La app de verdad, sobre el MISMO journal.
			DealershipCarApp current = new DealershipCarApp(TheVin, "Standard", TheModel);
			current.ConfigureStorage(DatabaseType.FileSystem, "path=" + folder);
			current.Start();

			Assert.That(TheModelTheJournalAnswers(current), Is.EqualTo(TheModel),
				"El upgrade nuevo se aplico donde faltaba.");

			DealershipCarAPI after = new DealershipCarAPI(current);

			Assert.That(after.ReadVin(), Is.EqualTo(TheVin),
				"Y las dos globales que ya estaban no se tocaron: sus upgrades siguen aplicados y no "
				+ "volvieron a correr.");
			Assert.That(after.LastEventApplied(), Is.EqualTo(0L));

			current.Dispose();
		}

		// LA GLOBAL NUEVA SOBREVIVE AL REPLAY, que es lo que la separa de un campo de C#: se vuelve a
		// abrir por tercera vez y sigue contestando sin que nadie la haya vuelto a sembrar.
		[Test]
		public void TheThirdGlobalStillAnswersAfterAnotherReopening()
		{
			AJournalTheCurrentAppOpened().Dispose();

			DealershipCarApp reopened = new DealershipCarApp(TheVin, "Standard", TheModel);
			reopened.ConfigureStorage(DatabaseType.FileSystem, "path=" + folder);
			reopened.Start();

			Assert.That(TheModelTheJournalAnswers(reopened), Is.EqualTo(TheModel));

			reopened.Dispose();
		}

		// EL MODELO ES UN HECHO DE LA PRIMERA APERTURA, y se ignora en silencio despues. MEDIDO en
		// T0 y escrito aca para que la nota del /// no sea una suposicion: el cuerpo de un upgrade
		// aplicado no vuelve a correr, asi que un codigo de modelo distinto en una apertura posterior
		// no reemplaza nada, no avisa nada y no se detecta. Corregirlo pide un nombre de upgrade
		// nuevo, no un argumento distinto.
		[Test]
		public void ADifferentModelCodeOnALaterOpeningIsSilentlyIgnored()
		{
			AJournalTheCurrentAppOpened().Dispose();

			DealershipCarApp wrong = new DealershipCarApp(TheVin, "Standard", AnotherModel);
			wrong.ConfigureStorage(DatabaseType.FileSystem, "path=" + folder);
			wrong.Start();

			Assert.That(TheModelTheJournalAnswers(wrong), Is.EqualTo(TheModel),
				"Contesta el que se sembro, no el que se acaba de pasar. Nada revienta y nada avisa.");

			wrong.Dispose();
		}

		// UN CODIGO DE MODELO EN BLANCO SE RECHAZA AL CONSTRUIR, antes de que haya actor y antes de
		// que haya journal. El dominio tambien lo rechaza -- Model no se deja construir sin valor --
		// pero para entonces la entrada de la semilla ya estaria escrita, y esa es la unica que se
		// vuelve a correr en CADA arranque.
		[Test]
		public void ABlankModelCodeIsRefusedBeforeAnyActorIsStarted()
		{
			Assert.That(
				Assert.Catch(() => new DealershipCarApp(TheVin, "Standard", "   ")),
				Is.InstanceOf<ArgumentException>());

			Assert.That(
				Assert.Catch(() => new DealershipCarApp(TheVin, "Standard", null)),
				Is.InstanceOf<ArgumentException>());
		}

		// Y se recorta, igual que el Vin: el modelo entra una sola vez y para siempre, asi que unos
		// espacios pegados quedarian dentro de toda respuesta que nombre el modelo.
		[Test]
		public void TheModelCodeIsTrimmedBeforeItIsSown()
		{
			DealershipCarApp app = new DealershipCarApp(TheVin, "Standard", "  " + TheModel + "  ");
			app.ConfigureStorage(DatabaseType.FileSystem, "path=" + folder);
			app.Start();

			Assert.That(TheModelTheJournalAnswers(app), Is.EqualTo(TheModel));

			app.Dispose();
		}

		// LO QUE CUESTA LA MIGRACION, en entradas: UNA EXTRA, UNA SOLA VEZ. Es la entrada del cuerpo
		// del upgrade nuevo corriendo de verdad, y se paga en la proxima apertura de cada journal que
		// ya existe. Despues vuelve a una por apertura, para siempre. Va medido porque es el numero
		// con el que se decide si una global nueva se puede agregar o no.
		[Test]
		public void TheMigrationCostsOneExtraEntryOnceAndThenNothing()
		{
			AnAppFromBeforeTheSpecification older = new AnAppFromBeforeTheSpecification(TheVin);
			older.ConfigureStorage(DatabaseType.FileSystem, "path=" + folder);
			older.Start();

			long beforeTheMigration = older.Actor.CurrentEntryId;
			older.Dispose();

			DealershipCarApp migrating = new DealershipCarApp(TheVin, "Standard", TheModel);
			migrating.ConfigureStorage(DatabaseType.FileSystem, "path=" + folder);
			migrating.Start();

			long afterTheMigration = migrating.Actor.CurrentEntryId;
			migrating.Dispose();

			Assert.That(afterTheMigration, Is.EqualTo(beforeTheMigration + 2),
				"DOS: la entrada que deja el comando de la semilla en cada apertura, mas la del "
				+ "cuerpo del upgrade nuevo corriendo por primera y unica vez.");

			DealershipCarApp again = new DealershipCarApp(TheVin, "Standard", TheModel);
			again.ConfigureStorage(DatabaseType.FileSystem, "path=" + folder);
			again.Start();

			Assert.That(again.Actor.CurrentEntryId, Is.EqualTo(afterTheMigration + 1),
				"Y a partir de ahi, UNA por apertura otra vez. Si esto creciera de a dos, el upgrade "
				+ "estaria corriendo en cada arranque en vez de una sola vez.");

			again.Dispose();
		}

		private DealershipCarApp AJournalTheCurrentAppOpened()
		{
			DealershipCarApp app = new DealershipCarApp(TheVin, "Standard", TheModel);
			app.ConfigureStorage(DatabaseType.FileSystem, "path=" + folder);
			app.Start();

			return app;
		}

		// LA LECTURA DE LA PUERTA, que T6 construyo: la misma que se hacia a mano contra el actor
		// mientras la especificacion no tenia ninguna. Es tambien la unica forma de medir la
		// AUSENCIA de la global, que es el falsificador de este fixture -- y por eso se le pasa la
		// app VIEJA tambien, que es un PerformanceV2 cualquiera y no un DealershipCarApp.
		private static string TheModelTheJournalAnswers(PerformanceV2 app)
		{
			return new DealershipCarAPI(app).ReadModel();
		}

		// La app tal como era antes de T5: dos globales de raiz. Existe SOLO para que la migracion
		// tenga contra que medirse -- un journal en el que la especificacion falta de verdad.
		private sealed class AnAppFromBeforeTheSpecification : PerformanceV2
		{
			private readonly string vin;

			internal AnAppFromBeforeTheSpecification(string vin)
				: base(DealershipCarApp.TheActorFor(vin), typeof(MaintenanceSchedule).Assembly)
			{
				this.vin = vin;
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
				")
				.WithParameters(p => {
					p["vin",   typeof(string)] = vin;
					p["usage", typeof(Enum)]   = "Standard";
				})
				.PerformCommand();
			}
		}
	}
}
