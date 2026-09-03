using System;
using System.IO;
using Choreography.Theater;
using DealershipCar.App;
using NUnit.Framework;
using Puppeteer;

namespace DealershipCar.Dominio.Pruebas
{
	// LA MISMA TRAMPA QUE TheFourthGlobalReachesAJournalThatHasTheOtherThree, una global mas tarde.
	// SH-3 le agrega a la app una QUINTA global de raiz -- las estadias en el taller -- al lado de la
	// agenda, la bitacora, la especificacion y el perfil. No va adentro de ninguna de las cuatro: una
	// estadia no es una operacion de mantenimiento, no es un punto de control del feed, no es lo que
	// el FABRICANTE publica sobre un modelo, y no es lo que la AGENCIA registro el dia de la entrega.
	//
	// Y LA GUARDA DE upgrade ES UNA-VEZ-POR-ACTOR. Los cuatro nombres viejos YA CORRIERON en todo
	// journal de DealershipCar que exista. Una linea agregada ADENTRO de cualquiera de los cuatro no
	// le llegaria a ninguno de ellos, en silencio, y editar el cuerpo de un upgrade ya aplicado
	// revienta con LanguageException. Asi que la global nueva llega bajo un NOMBRE NUEVO --
	// 'estadias-en-taller' -- y esta prueba abre un journal con la app de antes de SH-3, lo cierra, y
	// lo vuelve a abrir con la de verdad.
	//
	// P-1 LLAMA A ESA LINEA LA MAS IRREVERSIBLE DE LA REBANADA: un cuerpo de upgrade aplicado no
	// vuelve a correr nunca, asi que un nombre mal escrito no se edita -- solo se sustituye por otro,
	// y el equivocado se queda aplicado en todo journal que ya haya abierto.
	//
	// EL RENGLON QUE HACE QUE ESTE FIXTURE PUEDA DISTINGUIR ALGO es el que mide la ausencia: antes
	// de la migracion, leer la quinta global revienta con "Unknown property or method 'ReadVin' on
	// type 'Object'", porque una global de raiz sin asignar resuelve a un Object pelado. MEDIDO en
	// SPIKES-WarrantyEnvelope S2 fila 3 y reproducido en SPIKES-BuildOrder PROBE 5. Sin ese renglon
	// todo lo de abajo estaria verde aunque 'semilla' hubiera creado las cinco globales desde el
	// principio.
	//
	// EL FALSIFICADOR Y LA MITAD POSITIVA NOMBRAN EL MISMO MIEMBRO, que es lo que decidio DEC-31: si
	// midieran miembros distintos, el falsificador estaria midiendo la ausencia de algo que la mitad
	// positiva nunca lee. Los dos van por AStayReadWithoutTheDoor.Vin.
	//
	// LOS CINCO UPGRADES VIAJAN EN UN SOLO SCRIPT. El costo por apertura lo mide
	// TheSeedStillCostsExactlyOneEntryPerOpening, que sigue en UNA entrada y se pone rojo en DOS el
	// dia que alguien parta el script en dos PerformCommand -- por eso aca no se copia.
	//
	// LO QUE S2 MIDIO Y ACA SE RE-MIDE CONTRA EL ShopStay DE VERDAD, fila por fila: +1 en regimen
	// (2), el falsificador (3), +2 en la migracion (4), la quinta contesta (5), las otras cuatro
	// intactas (6), +1 para siempre despues (7), sobrevive otro replay (8), un journal NUEVO no paga
	// nada (9), y el orden de los upgrades no importa porque llavean por nombre (10).
	//
	// PROBE 5 MIDIO ESTO MISMO CON UN VehicleProfile ADENTRO DE LA QUINTA GLOBAL, porque SH-0 no
	// escribia dominio. Aca la quinta global tiene el ShopStay de verdad, y por eso la fila 5
	// contesta el VIN de las ESTADIAS y no el de un perfil repetido.
	[TestFixture]
	public sealed class TheFifthGlobalReachesAJournalThatHasTheOtherFour
	{
		private const string TheVin = "1HGCM82633A004352";
		private const string TheModel = "SM-SEDAN-2025";

		// Lo que se le escribe a cada una de las cuatro globales viejas antes de reordenar, para que
		// una re-corrida silenciosa tenga por donde verse.
		private const string TheDeviceKey = "device-de-la-prueba-del-orden";
		private const string TheRevision = "R1";
		private const int TheOdometerAtDelivery = 30000;

		private static readonly DateTime TheDelivery =
			new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);

		// La estadia con que se mide si un upgrade volvio a correr: lo unico que se le puede escribir
		// a la quinta global, y lo que desaparece si su cuerpo se vuelve a ejecutar.
		private const string TheBranch = "Taller Curridabat";
		private const string TheSubject = "Cambio de aceite";
		private const string TheYearFrom = "2026-01-01";
		private const string TheYearTo = "2026-12-31";

		private static readonly DateTime TheStayEntered =
			new DateTime(2026, 6, 10, 8, 30, 0, DateTimeKind.Utc);

		private static readonly DateTime TheStayCollected =
			new DateTime(2026, 6, 11, 16, 45, 0, DateTimeKind.Utc);

		private static readonly string[] TheStayDays = { "2026-06-10", "2026-06-11" };

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

		// FILAS 3, 4, 5 Y 6. El journal de antes, el falsificador, la migracion, y las cuatro viejas
		// intactas.
		[Test]
		public void AJournalWrittenBeforeTheStaysExistedGetsThemOnItsNextOpening()
		{
			// Un journal como los que ya existen: tiene los cuatro nombres viejos y ninguno mas.
			AnAppFromBeforeTheStays older = new AnAppFromBeforeTheStays(TheVin, TheModel);
			older.ConfigureStorage(DatabaseType.FileSystem, "path=" + folder);
			older.Start();

			DealershipCarAPI before = new DealershipCarAPI(older);

			Assert.That(before.ReadVin(), Is.EqualTo(TheVin),
				"La agenda si existe: 'semilla' corrio.");
			Assert.That(before.LastEventApplied(), Is.EqualTo(0L),
				"Y la bitacora tambien: 'bitacora-de-eventos' corrio.");
			Assert.That(before.ReadModel(), Is.EqualTo(TheModel),
				"Y la especificacion tambien: 'especificacion-del-modelo' corrio.");
			Assert.That(AProfileReadWithoutTheDoor.Vin(older), Is.EqualTo(TheVin),
				"Y el perfil tambien: 'perfil-del-vehiculo' corrio.");

			Exception missing = Assert.Catch(() => AStayReadWithoutTheDoor.Vin(older));

			TestContext.Out.WriteLine(
				"S2 FILA 3 -- el falsificador, antes de la migracion: " + missing.Message);

			Assert.That(missing.Message,
				Does.Contain("Unknown property or method 'ReadVin' on type 'Object'"),
				"Y las estadias NO existen todavia, que es lo que hace que este fixture pueda "
				+ "distinguir la migracion de no haber hecho nada. Una global de raiz sin asignar "
				+ "resuelve a un Object pelado, asi que preguntarle cualquier cosa revienta NOMBRANDO "
				+ "lo que se le pidio -- y el mensaje va afirmado entero y no solo el throw, porque es "
				+ "lo unico que separa 'la global falta' de 'la global esta y el metodo no'. Sin este "
				+ "renglon, todo lo de abajo estaria verde aunque 'semilla' hubiera creado las cinco.");

			older.Dispose();

			// La app de verdad, sobre el MISMO journal.
			DealershipCarApp current = AJournalTheCurrentAppOpened();

			Assert.That(AStayReadWithoutTheDoor.Vin(current), Is.EqualTo(TheVin),
				"S2 FILA 5: el upgrade nuevo se aplico donde faltaba, y las estadias saben de que "
				+ "vehiculo son.");

			DealershipCarAPI after = new DealershipCarAPI(current);

			Assert.That(after.ReadVin(), Is.EqualTo(TheVin),
				"S2 FILA 6: y las cuatro globales que ya estaban no se tocaron: sus upgrades siguen "
				+ "aplicados y no volvieron a correr.");
			Assert.That(after.LastEventApplied(), Is.EqualTo(0L));
			Assert.That(after.ReadModel(), Is.EqualTo(TheModel));
			Assert.That(AProfileReadWithoutTheDoor.Vin(current), Is.EqualTo(TheVin));

			current.Dispose();
		}

		// FILA 8: LA GLOBAL NUEVA SOBREVIVE AL REPLAY, que es lo que la separa de un campo de C#: se
		// vuelve a abrir por tercera vez y sigue contestando sin que nadie la haya vuelto a sembrar.
		[Test]
		public void TheFifthGlobalStillAnswersAfterAnotherReopening()
		{
			AJournalTheCurrentAppOpened().Dispose();

			DealershipCarApp reopened = AJournalTheCurrentAppOpened();

			Assert.That(AStayReadWithoutTheDoor.Vin(reopened), Is.EqualTo(TheVin));

			reopened.Dispose();
		}

		// FILAS 2, 4 Y 7: LO QUE CUESTA LA MIGRACION, en entradas. DOS en la apertura que migra, UNA
		// en toda apertura anterior y posterior. Los DELTAS son la medicion; los ids absolutos no
		// significan nada y no se comparan contra los de S2 ni contra los de PROBE 5.
		//
		// SE AFIRMA EL DELTA EXACTO Y NO QUE HAYA CRECIDO (C60): un "afterTheMigration > before"
		// pasa igual con +1 que con +2, y +1 querria decir que el cuerpo del upgrade nuevo no corrio.
		[Test]
		public void TheMigrationCostsTwoEntriesOnceAndThenOne()
		{
			AnAppFromBeforeTheStays older = new AnAppFromBeforeTheStays(TheVin, TheModel);
			older.ConfigureStorage(DatabaseType.FileSystem, "path=" + folder);
			older.Start();

			long onTheFirstOpening = older.Actor.CurrentEntryId;
			older.Dispose();

			AnAppFromBeforeTheStays again = new AnAppFromBeforeTheStays(TheVin, TheModel);
			again.ConfigureStorage(DatabaseType.FileSystem, "path=" + folder);
			again.Start();

			long beforeTheMigration = again.Actor.CurrentEntryId;
			again.Dispose();

			Assert.That(beforeTheMigration, Is.EqualTo(onTheFirstOpening + 1),
				"S2 FILA 2. En regimen, con cuatro globales: UNA entrada por apertura, la del comando "
				+ "de la semilla, aunque los cuatro upgrades ya esten aplicados y no hagan nada.");

			DealershipCarApp migrating = AJournalTheCurrentAppOpened();

			long afterTheMigration = migrating.Actor.CurrentEntryId;
			migrating.Dispose();

			Assert.That(afterTheMigration, Is.EqualTo(beforeTheMigration + 2),
				"S2 FILA 4. DOS: la entrada que el comando de la semilla deja en cada apertura, mas "
				+ "la del cuerpo del upgrade nuevo corriendo por primera y unica vez.");

			DealershipCarApp afterwards = AJournalTheCurrentAppOpened();

			long steadyAgain = afterwards.Actor.CurrentEntryId;

			Assert.That(steadyAgain, Is.EqualTo(afterTheMigration + 1),
				"S2 FILA 7. Y a partir de ahi, UNA por apertura otra vez. Si esto creciera de a dos, "
				+ "el upgrade estaria corriendo en cada arranque en vez de una sola vez.");

			TestContext.Out.WriteLine(
				"S2 FILAS 2/4/7 -- CurrentEntryId por apertura, con la quinta global: "
				+ onTheFirstOpening + " -> " + beforeTheMigration + " -> " + afterTheMigration
				+ " -> " + steadyAgain);

			afterwards.Dispose();
		}

		// FILA 9, Y ES LA MITAD QUE MAS IMPORTA: UN JOURNAL NUEVO NO PAGA NADA. Nacido con cinco
		// globales cuesta lo mismo que nacido con cuatro, asi que la entrada extra de la migracion la
		// paga una poblacion FIJA Y DECRECIENTE -- los journals que existan el dia que SH-3 salga, una
		// vez cada uno -- y no es un impuesto permanente sobre nadie.
		//
		// Va comparado contra un journal nuevo de CUATRO globales y no contra un numero escrito a
		// mano: el id absoluto depende de la semilla y de la version del motor, y una constante aca
		// envejeceria sin avisar.
		[Test]
		public void AFreshJournalBornWithFiveGlobalsPaysWhatOneBornWithFourPays()
		{
			string another = ATemporaryFolder();

			try
			{
				AnAppFromBeforeTheStays withFour = new AnAppFromBeforeTheStays(TheVin, TheModel);
				withFour.ConfigureStorage(DatabaseType.FileSystem, "path=" + another);
				withFour.Start();

				long bornWithFour = withFour.Actor.CurrentEntryId;
				withFour.Dispose();

				DealershipCarApp withFive = AJournalTheCurrentAppOpened();

				long bornWithFive = withFive.Actor.CurrentEntryId;
				withFive.Dispose();

				TestContext.Out.WriteLine(
					"S2 FILA 9 -- journal recien nacido: con cuatro globales " + bornWithFour
					+ ", con cinco " + bornWithFive);

				Assert.That(bornWithFive, Is.EqualTo(bornWithFour),
					"Lo MISMO. Una global mas en un journal que nace no cuesta ni una entrada: los "
					+ "cinco upgrades corren dentro del unico comando que la semilla ya escribia.");
			}
			finally
			{
				Sweep(another);
			}
		}

		// EL NOMBRE, LETRA POR LETRA, Y ES LA LINEA MAS IRREVERSIBLE DE LA REBANADA. Un cuerpo de
		// upgrade aplicado no vuelve a correr nunca: un nombre mal escrito no se edita -- editarlo
		// revienta -- solo se sustituye por otro, y el equivocado se queda aplicado en todo journal
		// que ya haya abierto.
		//
		// SE MIDE CONTRA ESTADO ESCRITO, que es el unico instrumento que sirve. Un journal se abre
		// con una app que declara 'estadias-en-taller' escrito a mano aca, se le registra una estadia,
		// y despues lo abre la app DE VERDAD. Si los dos nombres calzan letra por letra, el upgrade de
		// la app de verdad no vuelve a correr y la estadia sigue ahi; si difieren en un solo caracter,
		// la app de verdad aplica un upgrade que a este journal le falta, 'stays' se reemplaza por un
		// ShopStay recien hecho, y la estadia desaparece.
		//
		// EL CONTEO DE ENTRADAS NO SIRVE PARA ESTO, Y ESTA PRUEBA EMPEZO USANDOLO. Dos apps con
		// scripts distintos cuestan +2 aunque los cinco nombres calcen, porque el texto nuevo es una
		// PLANTILLA nueva -- ver TheExtraEntryOfAMigrationIsTheNewScriptAndNotTheUpgradeBody, que es
		// donde eso quedo medido.
		//
		// LAS OTRAS PRUEBAS DE ESTE FIXTURE NO VEN ESTO: todas migran un journal al que la quinta
		// global le falta, y ahi cualquier nombre nuevo funciona igual de bien. Este es el unico
		// renglon que se pone rojo si alguien reescribe la linea de P-1.
		[Test]
		public void TheFifthUpgradeIsNamedExactlyEstadiasEnTaller()
		{
			AnAppWithTheStaysDeclaredFirst spelledOutByHand =
				new AnAppWithTheStaysDeclaredFirst(TheVin, TheModel);
			spelledOutByHand.ConfigureStorage(DatabaseType.FileSystem, "path=" + folder);
			spelledOutByHand.Start();

			new DealershipCarAPI(spelledOutByHand).RecordShopStay(
				TheBranch, TheSubject, TheStayEntered, TheStayCollected, TheStayDays);

			Assert.That(
				AStayReadWithoutTheDoor.Days(spelledOutByHand, TheYearFrom, TheYearTo),
				Is.EqualTo(TheStayDays),
				"La estadia quedo escrita en la global que creo el nombre escrito a mano aqui.");

			spelledOutByHand.Dispose();

			DealershipCarApp real = AJournalTheCurrentAppOpened();

			Assert.That(AStayReadWithoutTheDoor.Days(real, TheYearFrom, TheYearTo),
				Is.EqualTo(TheStayDays),
				"Y SIGUE AHI despues de que la app de verdad abrio el journal. Eso solo pasa si el "
				+ "nombre de su quinto upgrade calza letra por letra con 'estadias-en-taller': un "
				+ "nombre distinto seria un upgrade que a este journal le falta, correria al final "
				+ "del replay, y reemplazaria 'stays' por un ShopStay recien hecho y vacio.");

			real.Dispose();
		}

		// LO QUE CUESTA LA ENTRADA EXTRA DE UNA MIGRACION, Y NO ES LO QUE ESTABA ESCRITO. S2 y PROBE
		// 5 midieron +2 en la apertura que migra y lo atribuyeron a "una por el comando de la semilla
		// y una por el cuerpo del upgrade nuevo". La MAGNITUD es correcta y se re-mide arriba; la
		// ATRIBUCION no lo es.
		//
		// LO QUE CUESTA LA SEGUNDA ENTRADA ES LA PLANTILLA. El journal guarda un comando como
		// 'define action N' mas sus argumentos, y un texto de script que este actor nunca vio es una
		// plantilla NUEVA. Aca las dos aperturas declaran LOS MISMOS CINCO upgrades, con los mismos
		// nombres, en distinto orden: ningun cuerpo vuelve a correr -- la estadia escrita sobrevive,
		// que es lo que lo prueba -- y la apertura cuesta +2 igual.
		//
		// IMPORTA PARA QUIEN VENGA DESPUES: la sexta global de CostLedger va a costar +2 por el mismo
		// motivo, y lo costaria igual si alguien reordenara las cinco lineas sin agregar ninguna.
		[Test]
		public void TheExtraEntryOfAMigrationIsTheNewScriptAndNotTheUpgradeBody()
		{
			AnAppWithTheStaysDeclaredFirst spelledOutByHand =
				new AnAppWithTheStaysDeclaredFirst(TheVin, TheModel);
			spelledOutByHand.ConfigureStorage(DatabaseType.FileSystem, "path=" + folder);
			spelledOutByHand.Start();

			new DealershipCarAPI(spelledOutByHand).RecordShopStay(
				TheBranch, TheSubject, TheStayEntered, TheStayCollected, TheStayDays);

			long beforeTheOtherScript = spelledOutByHand.Actor.CurrentEntryId;
			spelledOutByHand.Dispose();

			DealershipCarApp real = AJournalTheCurrentAppOpened();

			long afterTheOtherScript = real.Actor.CurrentEntryId;

			TestContext.Out.WriteLine(
				"PLANTILLA NUEVA SIN UPGRADE NUEVO -- CurrentEntryId: " + beforeTheOtherScript
				+ " -> " + afterTheOtherScript);

			Assert.That(afterTheOtherScript, Is.EqualTo(beforeTheOtherScript + 2),
				"+2 CON CERO CUERPOS DE UPGRADE NUEVOS. Los cinco nombres ya estaban aplicados; lo "
				+ "unico nuevo es el TEXTO del script, que es una plantilla que este actor nunca "
				+ "habia visto.");

			Assert.That(AStayReadWithoutTheDoor.Days(real, TheYearFrom, TheYearTo),
				Is.EqualTo(TheStayDays),
				"Y el control que lo prueba: ningun cuerpo volvio a correr, porque la estadia que se "
				+ "escribio antes sigue ahi. Si alguno hubiera corrido, su global estaria recien "
				+ "hecha y vacia.");

			real.Dispose();
		}

		// FILA 10: LOS UPGRADES LLAVEAN POR NOMBRE, NO POR ORDEN. Declarar el nuevo PRIMERO migra
		// igual -- los cuatro viejos siguen aplicados y no vuelven a correr -- y cuesta lo mismo.
		//
		// PROBE 5 NO REPITIO ESTA FILA y dijo por que: el mecanismo ya lo fija
		// TheOrderOfTheFourUpgradesInTheScriptDoesNotMatter, que esta verde. Aca se re-mide igual,
		// porque lo que aquella prueba fija es que CUATRO upgrades llavean por nombre y esto es lo que
		// se le pidio a SH-3: las mismas filas 3-10 contra el ShopStay de verdad.
		//
		// Y SE MIDE CONTRA ESTADO ESCRITO, no contra lo que las globales contestan recien nacidas:
		// el VIN, el modelo y el punto de control son argumentos de la SIEMBRA, y una global
		// re-sembrada los contesta exactamente igual. Lo que SI se ve es lo que se le escribio DESPUES.
		[Test]
		public void TheOrderOfTheFiveUpgradesInTheScriptDoesNotMatter()
		{
			AnAppFromBeforeTheStays older = new AnAppFromBeforeTheStays(TheVin, TheModel);
			older.ConfigureStorage(DatabaseType.FileSystem, "path=" + folder);
			older.Start();

			DealershipCarAPI before = new DealershipCarAPI(older);

			before.RegisterDevice(TheDeviceKey);
			before.SeedPlanOperation(
				"FluidChange", "TimeOrDistance", 365, 10000, "UnitA", true, false,
				TheDelivery, TheOdometerAtDelivery, "UnitA");
			before.OpenRevision(TheRevision, TheDelivery);
			before.PublishFuelGrade(TheRevision, "91 RON", "Minimum");
			before.SealRevision(TheRevision);
			before.RecordDelivery(TheDelivery, TheOdometerAtDelivery, "UnitA");

			long beforeTheMigration = older.Actor.CurrentEntryId;
			older.Dispose();

			AnAppWithTheStaysDeclaredFirst reordered =
				new AnAppWithTheStaysDeclaredFirst(TheVin, TheModel);
			reordered.ConfigureStorage(DatabaseType.FileSystem, "path=" + folder);
			reordered.Start();

			Assert.That(reordered.Actor.CurrentEntryId, Is.EqualTo(beforeTheMigration + 2),
				"+2, igual que con el nuevo al final: una por el comando y una por el cuerpo nuevo.");

			Assert.That(AStayReadWithoutTheDoor.Vin(reordered), Is.EqualTo(TheVin),
				"La quinta global contesta.");

			DealershipCarAPI after = new DealershipCarAPI(reordered);

			Assert.That(after.StoredDeviceKey(), Is.EqualTo(TheDeviceKey),
				"La bitacora sigue siendo la MISMA: 'bitacora-de-eventos' no volvio a correr. Si "
				+ "hubiera vuelto, esta credencial estaria en blanco y nada mas lo diria.");
			Assert.That(after.ReadDueState("FluidChange"), Is.Not.EqualTo("NotScheduled"),
				"Y la agenda tambien: la operacion que se le sembro sigue ahi.");
			Assert.That(after.HasASealedRevision(), Is.True,
				"Y la especificacion: la revision sellada sigue sellada.");
			Assert.That(after.ReadCurrentRevisionCode(), Is.EqualTo(TheRevision));
			Assert.That(after.HasDeliveryBeenRecorded(), Is.True,
				"Y el perfil: la entrega registrada sigue registrada.");

			Assert.That(after.ReadVin(), Is.EqualTo(TheVin));
			Assert.That(after.ReadModel(), Is.EqualTo(TheModel));

			reordered.Dispose();
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

		// La app tal como era antes de SH-3: CUATRO globales de raiz. Existe SOLO para que la
		// migracion tenga contra que medirse -- un journal en el que las estadias faltan de verdad.
		//
		// LOS CUATRO CUERPOS ESTAN COPIADOS PALABRA POR PALABRA de DealershipCarApp.OnHydrated, y esa
		// copia es la prueba: lo que se mide es que los cuatro NOMBRES calcen exacto para que NO
		// vuelvan a correr. Un cuerpo parafraseado o un nombre mal escrito mediria otro journal que
		// el que la gente tiene, y lo mediria en verde. Copiar y no heredar tambien es a proposito:
		// DealershipCarApp es sealed, y sobreescribir OnHydrated reemplazaria los cinco.
		private sealed class AnAppFromBeforeTheStays : PerformanceV2
		{
			private readonly string vin;
			private readonly string modelCode;

			internal AnAppFromBeforeTheStays(string vin, string modelCode)
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
				")
				.WithParameters(p => {
					p["vin",   typeof(string)] = vin;
					p["usage", typeof(Enum)]   = "Standard";
					p["model", typeof(string)] = modelCode;
				})
				.PerformCommand();
			}
		}

		// Los MISMOS cinco upgrades de la app de verdad, con el nuevo declarado PRIMERO. Existe solo
		// para la fila 10: que el orden no sea carga.
		private sealed class AnAppWithTheStaysDeclaredFirst : PerformanceV2
		{
			private readonly string vin;
			private readonly string modelCode;

			internal AnAppWithTheStaysDeclaredFirst(string vin, string modelCode)
				: base(DealershipCarApp.TheActorFor(vin), typeof(MaintenanceSchedule).Assembly)
			{
				this.vin = vin;
				this.modelCode = modelCode;
			}

			protected override void OnHydrated()
			{
				Using(@"
					upgrade('estadias-en-taller') {
						stays = ShopStay(@vin);
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
					upgrade('perfil-del-vehiculo') {
						profile = VehicleProfile(@vin);
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
