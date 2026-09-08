using System;
using System.IO;
using Choreography.Theater;
using Concesionario.App;
using NUnit.Framework;
using Puppeteer;

namespace Concesionario.Dominio.Pruebas
{
	// LA MISMA TRAMPA QUE TheThirdGlobalReachesAJournalThatHasTheOtherTwo, una global mas tarde. T4
	// le agrega a la app una CUARTA global de raiz -- el perfil del vehiculo -- al lado de la
	// agenda, la bitacora y la especificacion. No va adentro de ninguna de las tres: lo que la
	// AGENCIA registra sobre ESTE carro no es una operacion de mantenimiento, no es un punto de
	// control del feed, y no es lo que el FABRICANTE publica sobre un modelo.
	//
	// Y LA GUARDA DE upgrade ES UNA-VEZ-POR-ACTOR. Los tres nombres viejos YA CORRIERON en todo
	// journal de Concesionario que exista. Una linea agregada ADENTRO de cualquiera de los tres no
	// le llegaria a ninguno de ellos, en silencio, y editar el cuerpo de un upgrade ya aplicado
	// revienta con LanguageException. Asi que la global nueva llega bajo un NOMBRE NUEVO --
	// 'perfil-del-vehiculo' -- y esta prueba abre un journal con la app de antes de T4, lo cierra, y
	// lo vuelve a abrir con la de verdad.
	//
	// EL RENGLON QUE HACE QUE ESTE FIXTURE PUEDA DISTINGUIR ALGO es el que mide la ausencia: antes
	// de la migracion, leer la cuarta global revienta con "Unknown property or method 'ReadVin' on
	// type 'Object'", porque una global de raiz sin asignar resuelve a un Object pelado. MEDIDO en
	// SPIKES-WarrantyEnvelope S2, fila 3. Sin ese renglon todo lo de abajo estaria verde aunque
	// 'semilla' hubiera creado las cuatro globales desde el principio.
	//
	// Y SE QUEDA EN ReadVin AUNQUE T5 YA PUSO ONCE LECTURAS DEL PERFIL EN LA PUERTA. El falsificador
	// y las afirmaciones positivas de mas abajo tienen que nombrar EL MISMO miembro, o el
	// falsificador mide la ausencia de algo que la mitad positiva nunca lee. Y ReadVin del perfil es
	// la unica lectura de la cuarta global que contesta algo PROPIO de este vehiculo: con ella, una
	// siembra que creara el perfil del carro equivocado se pone roja; con una lectura de presencia
	// -- que antes revienta y despues contesta que no -- ese defecto pasaria verde.
	//
	// LOS CUATRO UPGRADES VIAJAN EN UN SOLO SCRIPT. El costo por apertura lo mide
	// TheSeedStillCostsExactlyOneEntryPerOpening, que sigue en UNA entrada y se pone rojo en DOS el
	// dia que alguien parta el script en dos PerformCommand -- por eso aca no se copia: la que ya
	// existe falla para el mismo defecto, y una tercera copia seria un barrido de mas.
	//
	// LO QUE S2 MIDIO Y ACA SE REPRODUCE, fila por fila: +1 en regimen (2), el falsificador (3), +2
	// en la migracion (4), la cuarta contesta (5), las otras tres intactas (6), +1 para siempre
	// despues (7), sobrevive otro replay (8), un journal NUEVO no paga nada (9), y el orden de los
	// upgrades no importa porque llavean por nombre (10).
	[TestFixture]
	public sealed class TheFourthGlobalReachesAJournalThatHasTheOtherThree
	{
		private const string TheVin = "1HGCM82633A004352";
		private const string TheModel = "YARIS-2019";

		// Lo que se le escribe a cada una de las tres globales viejas antes de reordenar, para que
		// una re-corrida silenciosa tenga por donde verse.
		// TheDeviceKey SE FUE con la credencial (2026-09-03). Ver el comentario del testigo.
		private const string TheRevision = "R1";
		private const int TheOdometerAtDelivery = 30000;

		private static readonly DateTime TheDelivery =
			new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);

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

		// FILAS 3, 4, 5 Y 6. El journal de antes, el falsificador, la migracion, y las tres viejas
		// intactas.
		[Test]
		public void AJournalWrittenBeforeTheProfileExistedGetsItOnItsNextOpening()
		{
			// Un journal como los que ya existen: tiene los tres nombres viejos y ninguno mas.
			AnAppFromBeforeTheProfile older = new AnAppFromBeforeTheProfile(TheVin, TheModel);
			older.ConfigureStorage(DatabaseType.FileSystem, "path=" + folder);
			older.Start();

			DealershipCarAPI before = new DealershipCarAPI(older);

			Assert.That(before.ReadVin(), Is.EqualTo(TheVin),
				"La agenda si existe: 'semilla' corrio.");
			Assert.That(before.LastEventApplied(), Is.EqualTo(0L),
				"Y la bitacora tambien: 'bitacora-de-eventos' corrio.");
			Assert.That(before.ReadModel(), Is.EqualTo(TheModel),
				"Y la especificacion tambien: 'especificacion-del-modelo' corrio.");

			Exception missing = Assert.Catch(() => AProfileReadWithoutTheDoor.Vin(older));

			Assert.That(missing.Message,
				Does.Contain("Unknown property or method 'ReadVin' on type 'Object'"),
				"Y el perfil NO existe todavia, que es lo que hace que este fixture pueda distinguir "
				+ "la migracion de no haber hecho nada. Una global de raiz sin asignar resuelve a un "
				+ "Object pelado, asi que preguntarle cualquier cosa revienta NOMBRANDO lo que se le "
				+ "pidio -- y el mensaje va afirmado entero y no solo el throw, porque es lo unico "
				+ "que separa 'la global falta' de 'la global esta y el metodo no'. Sin este renglon, "
				+ "todo lo de abajo estaria verde aunque 'semilla' hubiera creado las cuatro.");

			older.Dispose();

			// La app de verdad, sobre el MISMO journal.
			DealershipCarApp current = AJournalTheCurrentAppOpened();

			Assert.That(AProfileReadWithoutTheDoor.Vin(current), Is.EqualTo(TheVin),
				"El upgrade nuevo se aplico donde faltaba, y el perfil sabe de que vehiculo es.");

			DealershipCarAPI after = new DealershipCarAPI(current);

			Assert.That(after.ReadVin(), Is.EqualTo(TheVin),
				"Y las tres globales que ya estaban no se tocaron: sus upgrades siguen aplicados y no "
				+ "volvieron a correr.");
			Assert.That(after.LastEventApplied(), Is.EqualTo(0L));
			Assert.That(after.ReadModel(), Is.EqualTo(TheModel));

			current.Dispose();
		}

		// FILA 8: LA GLOBAL NUEVA SOBREVIVE AL REPLAY, que es lo que la separa de un campo de C#: se
		// vuelve a abrir por tercera vez y sigue contestando sin que nadie la haya vuelto a sembrar.
		[Test]
		public void TheFourthGlobalStillAnswersAfterAnotherReopening()
		{
			AJournalTheCurrentAppOpened().Dispose();

			DealershipCarApp reopened = AJournalTheCurrentAppOpened();

			Assert.That(AProfileReadWithoutTheDoor.Vin(reopened), Is.EqualTo(TheVin));

			reopened.Dispose();
		}

		// FILAS 2, 4 Y 7: LO QUE CUESTA LA MIGRACION, en entradas. DOS en la apertura que migra, UNA
		// en toda apertura anterior y posterior. Los DELTAS son la medicion; los ids absolutos no
		// significan nada y no se comparan contra los de S2, que corrio con otra semilla.
		//
		// SE AFIRMA EL DELTA EXACTO Y NO QUE HAYA CRECIDO (C60): un "afterTheMigration > before"
		// pasa igual con +1 que con +2, y +1 querria decir que el cuerpo del upgrade nuevo no corrio.
		[Test]
		public void TheMigrationCostsTwoEntriesOnceAndThenOne()
		{
			AnAppFromBeforeTheProfile older = new AnAppFromBeforeTheProfile(TheVin, TheModel);
			older.ConfigureStorage(DatabaseType.FileSystem, "path=" + folder);
			older.Start();

			long onTheFirstOpening = older.Actor.CurrentEntryId;
			older.Dispose();

			AnAppFromBeforeTheProfile again = new AnAppFromBeforeTheProfile(TheVin, TheModel);
			again.ConfigureStorage(DatabaseType.FileSystem, "path=" + folder);
			again.Start();

			long beforeTheMigration = again.Actor.CurrentEntryId;
			again.Dispose();

			Assert.That(beforeTheMigration, Is.EqualTo(onTheFirstOpening + 1),
				"En regimen, con tres globales: UNA entrada por apertura, la del comando de la "
				+ "semilla, aunque los tres upgrades ya esten aplicados y no hagan nada.");

			DealershipCarApp migrating = AJournalTheCurrentAppOpened();

			long afterTheMigration = migrating.Actor.CurrentEntryId;
			migrating.Dispose();

			Assert.That(afterTheMigration, Is.EqualTo(beforeTheMigration + 2),
				"DOS: la entrada que el comando de la semilla deja en cada apertura, mas la del "
				+ "cuerpo del upgrade nuevo corriendo por primera y unica vez.");

			DealershipCarApp afterwards = AJournalTheCurrentAppOpened();

			Assert.That(afterwards.Actor.CurrentEntryId, Is.EqualTo(afterTheMigration + 1),
				"Y a partir de ahi, UNA por apertura otra vez. Si esto creciera de a dos, el upgrade "
				+ "estaria corriendo en cada arranque en vez de una sola vez.");

			afterwards.Dispose();
		}

		// FILA 9, Y ES LA MITAD QUE MAS IMPORTA: UN JOURNAL NUEVO NO PAGA NADA. Nacido con cuatro
		// globales cuesta lo mismo que nacido con tres, asi que la entrada extra de la migracion la
		// paga una poblacion FIJA Y DECRECIENTE -- los journals que existan el dia que T4 salga, una
		// vez cada uno -- y no es un impuesto permanente sobre nadie.
		//
		// Va comparado contra un journal nuevo de TRES globales y no contra un numero escrito a
		// mano: el id absoluto depende de la semilla y de la version del motor, y una constante aca
		// envejeceria sin avisar.
		[Test]
		public void AFreshJournalBornWithFourGlobalsPaysWhatOneBornWithThreePays()
		{
			string another = ATemporaryFolder();

			try
			{
				AnAppFromBeforeTheProfile withThree =
					new AnAppFromBeforeTheProfile(TheVin, TheModel);
				withThree.ConfigureStorage(DatabaseType.FileSystem, "path=" + another);
				withThree.Start();

				long bornWithThree = withThree.Actor.CurrentEntryId;
				withThree.Dispose();

				DealershipCarApp withFour = AJournalTheCurrentAppOpened();

				long bornWithFour = withFour.Actor.CurrentEntryId;
				withFour.Dispose();

				Assert.That(bornWithFour, Is.EqualTo(bornWithThree),
					"Lo MISMO. Una global mas en un journal que nace no cuesta ni una entrada: los "
					+ "cuatro upgrades corren dentro del unico comando que la semilla ya escribia.");
			}
			finally
			{
				Sweep(another);
			}
		}

		// FILA 10: LOS UPGRADES LLAVEAN POR NOMBRE, NO POR ORDEN. Declarar el nuevo PRIMERO migra
		// igual -- los tres viejos siguen aplicados y no vuelven a correr -- y cuesta lo mismo.
		//
		// VALE SU PROPIA PRUEBA porque lo contrario habria sido una re-corrida silenciosa: si el
		// orden importara, mover una linea en OnHydrated volveria a sembrar tres globales encima de
		// las que ya estan, sin que nada reventara y sin que nada avisara. Deja dicho, en rojo el
		// dia que cambie, que el orden de las cuatro lineas de OnHydrated no es carga.
		//
		// Y SE MIDE CONTRA ESTADO ESCRITO, no contra lo que las globales contestan recien nacidas.
		// LA PRIMERA VERSION DE ESTA PRUEBA NO PODIA ATRAPAR EL DEFECTO QUE DEFIENDE: afirmaba el
		// VIN, el modelo y el punto de control, y los tres son argumentos de la SIEMBRA -- una global
		// re-sembrada contesta exactamente lo mismo. El conteo de entradas tampoco lo veria, porque
		// tres cuerpos que vuelven a correr adentro de un comando siguen costando una entrada.
		//
		// Lo que SI se ve es lo que se le escribio DESPUES de sembrarla: la credencial del
		// dispositivo en la bitacora, una operacion en la agenda y una revision sellada en la
		// especificacion. El replay las vuelve a aplicar sobre las globales que la siembra creo; si
		// un upgrade volviera a correr, reemplazaria esa global por una recien hecha y las tres
		// desaparecerian.
		[Test]
		public void TheOrderOfTheFourUpgradesInTheScriptDoesNotMatter()
		{
			AnAppFromBeforeTheProfile older = new AnAppFromBeforeTheProfile(TheVin, TheModel);
			older.ConfigureStorage(DatabaseType.FileSystem, "path=" + folder);
			older.Start();

			DealershipCarAPI before = new DealershipCarAPI(older);

			before.SeedPlanOperation(
				"FluidChange", "TimeOrDistance", 365, 10000, "UnitA", true, false,
				TheDelivery, TheOdometerAtDelivery, "UnitA");
			before.OpenRevision(TheRevision, TheDelivery);
			before.PublishFuelGrade(TheRevision, "91 RON", "Minimum");
			before.SealRevision(TheRevision);

			long beforeTheMigration = older.Actor.CurrentEntryId;
			older.Dispose();

			AnAppWithTheProfileDeclaredFirst reordered =
				new AnAppWithTheProfileDeclaredFirst(TheVin, TheModel);
			reordered.ConfigureStorage(DatabaseType.FileSystem, "path=" + folder);
			reordered.Start();

			Assert.That(reordered.Actor.CurrentEntryId, Is.EqualTo(beforeTheMigration + 2),
				"+2, igual que con el nuevo al final: una por el comando y una por el cuerpo nuevo.");

			Assert.That(AProfileReadWithoutTheDoor.Vin(reordered), Is.EqualTo(TheVin),
				"La cuarta global contesta.");

			DealershipCarAPI after = new DealershipCarAPI(reordered);

			// LA BITACORA SE QUEDO SIN TESTIGO, y se dice en vez de taparlo. Aca iba
			// `Assert.That(after.StoredDeviceKey(), Is.EqualTo(TheDeviceKey))`, con esta razon:
			// «la bitacora sigue siendo la MISMA: 'bitacora-de-eventos' no volvio a correr; si
			// hubiera vuelto, esta credencial estaria en blanco y nada mas lo diria».
			//
			// El 2026-09-03 la credencial salio del journal, asi que ese testigo ya no existe. Lo
			// unico observable que le queda a la bitacora es su punto de control, y esta prueba
			// nunca lo avanza; fabricarselo aplicando un OdometerReported ESCRIBIRIA una lectura de
			// odometro, que es justo lo que puede mover el ReadDueState de dos renglones mas abajo:
			// se cambiaria lo que la prueba mide para conservar una linea.
			//
			// SIGUE CUBIERTO que ningun upgrade volvio a correr -lo dice el +2 de arriba- y que las
			// otras globales sobrevivieron -la agenda y la especificacion, abajo-. SE PERDIO el
			// testigo propio de la bitacora, y es lo unico de este merge que quedo mas debil: es de
			// quien hizo el traspaso (PR 10064) decidir con que reemplazarlo.
			Assert.That(after.ReadDueState("FluidChange"), Is.Not.EqualTo("NotScheduled"),
				"Y la agenda tambien: la operacion que se le sembro sigue ahi.");
			Assert.That(after.HasASealedRevision(), Is.True,
				"Y la especificacion: la revision sellada sigue sellada.");
			Assert.That(after.ReadCurrentRevisionCode(), Is.EqualTo(TheRevision));

			Assert.That(after.ReadVin(), Is.EqualTo(TheVin));
			Assert.That(after.ReadModel(), Is.EqualTo(TheModel));

			reordered.Dispose();
		}

		// LA MITAD DE LA PRUEBA 1 DEL BRIEF QUE VIVE EN EL JOURNAL. El nivel A ya afirma que un
		// perfil recien creado contesta las dos preguntas de presencia sin reventar
		// (AProfileToldNothingStillAnswers); lo que no puede afirmar es que un perfil SEMBRADO POR EL
		// upgrade, sobre disco, rehidratado desde el journal, conteste igual. Eso es lo unico que se
		// agrega aca.
		//
		// SE LEE POR LA PUERTA DESDE T5. Cuando esto se escribio, la puerta no tenia ni una lectura
		// del perfil y las tres salian por un script crudo; hoy las tres tienen su miembro y este es
		// el camino que un llamador de verdad toma. La version completa -- las once, miembro por
		// miembro y estado por estado -- es AProfileIsReadBackThroughTheDoor.
		[Test]
		public void AProfileToldNothingStillAnswersOnAMigratedJournal()
		{
			AnAppFromBeforeTheProfile older = new AnAppFromBeforeTheProfile(TheVin, TheModel);
			older.ConfigureStorage(DatabaseType.FileSystem, "path=" + folder);
			older.Start();
			older.Dispose();

			DealershipCarApp migrated = AJournalTheCurrentAppOpened();

			DealershipCarAPI after = new DealershipCarAPI(migrated);

			Assert.That(after.IsWarrantyEnvelopePublished(), Is.False,
				"La agencia no publico sobre. Es una respuesta legal del primer dia, no un error.");
			Assert.That(after.HasDeliveryBeenRecorded(), Is.False,
				"Ni registro la entrega.");

			Assert.That(
				after.DescribePublishedWarrantyEnvelope(),
				Does.Contain("no ha publicado"),
				"Y la mitad publicada contesta con palabras igual, que es la ley L7: que a este "
				+ "vehiculo le falte todo no vuelve incitable lo que la agencia publico -- o no "
				+ "publico -- para cualquiera.");

			migrated.Dispose();
		}

		private DealershipCarApp AJournalTheCurrentAppOpened()
		{
			DealershipCarApp app = new DealershipCarApp(TheVin, "Standard", TheModel);
			app.ConfigureStorage(DatabaseType.FileSystem, "path=" + folder);
			app.Start();

			return app;
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

		// La app tal como era antes de T4: TRES globales de raiz. Existe SOLO para que la migracion
		// tenga contra que medirse -- un journal en el que el perfil falta de verdad.
		//
		// LOS TRES CUERPOS ESTAN COPIADOS PALABRA POR PALABRA de DealershipCarApp.OnHydrated, y esa
		// copia es la prueba: lo que se mide es que los tres NOMBRES calcen exacto para que NO
		// vuelvan a correr. Un cuerpo parafraseado o un nombre mal escrito mediria otro journal que
		// el que la gente tiene, y lo mediria en verde. Copiar y no heredar tambien es a proposito:
		// DealershipCarApp es sealed, y sobreescribir OnHydrated reemplazaria los cuatro.
		private sealed class AnAppFromBeforeTheProfile : PerformanceV2
		{
			private readonly string vin;
			private readonly string modelCode;

			internal AnAppFromBeforeTheProfile(string vin, string modelCode)
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
				")
				.WithParameters(p => {
					p["vin",   typeof(string)] = vin;
					p["usage", typeof(Enum)]   = "Standard";
					p["model", typeof(string)] = modelCode;
				})
				.PerformCommand();
			}
		}

		// Los MISMOS cuatro upgrades de la app de verdad, con el nuevo declarado PRIMERO. Existe solo
		// para la fila 10: que el orden no sea carga.
		private sealed class AnAppWithTheProfileDeclaredFirst : PerformanceV2
		{
			private readonly string vin;
			private readonly string modelCode;

			internal AnAppWithTheProfileDeclaredFirst(string vin, string modelCode)
				: base(DealershipCarApp.TheActorFor(vin), typeof(MaintenanceSchedule).Assembly)
			{
				this.vin = vin;
				this.modelCode = modelCode;
			}

			protected override void OnHydrated()
			{
				Using(@"
					upgrade('perfil-del-vehiculo') {
						profile = VehicleProfile(@vin);
					}
					upgrade('semilla') {
						schedule = MaintenanceSchedule(@vin, @usage);
					}
					upgrade('bitacora-de-eventos') {
						logbook = Logbook();
					}
					upgrade('especificacion-del-modelo') {
						specification = ModelSpecification(@model);
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
