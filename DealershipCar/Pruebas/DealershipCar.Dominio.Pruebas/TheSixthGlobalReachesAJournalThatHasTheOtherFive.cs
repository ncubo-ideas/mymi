using System;
using System.IO;
using Choreography.Theater;
using DealershipCar.App;
using NUnit.Framework;
using Puppeteer;

namespace DealershipCar.Dominio.Pruebas
{
	// LA MISMA TRAMPA QUE TheFifthGlobalReachesAJournalThatHasTheOtherFour, una global mas tarde.
	// CL-4 le agrega a la app una SEXTA global de raiz -- el libro de costos -- al lado de la agenda,
	// la bitacora, la especificacion, el perfil y las estadias. No va adentro de ninguna de las
	// cinco: un gasto no es una operacion de mantenimiento, no es un punto de control del feed, no es
	// lo que el FABRICANTE publica sobre un modelo, no es lo que la AGENCIA registro el dia de la
	// entrega, y no es una estadia en el taller. Y sobre todo no es un PRECIO PUBLICADO: L10 dice que
	// un precio y un costo son dos hechos de forma distinta, de autores distintos y con vidas
	// distintas, y ponerlos en la misma global seria la fusion que DEC-48 y DEC-36 existen para
	// evitar.
	//
	// Y LA GUARDA DE upgrade ES UNA-VEZ-POR-ACTOR. Los cinco nombres viejos YA CORRIERON en todo
	// journal de DealershipCar que exista. Una linea agregada ADENTRO de cualquiera de los cinco no
	// le llegaria a ninguno de ellos, en silencio, y editar el cuerpo de un upgrade ya aplicado
	// revienta con LanguageException. Asi que la global nueva llega bajo un NOMBRE NUEVO --
	// 'libro-de-costos' -- y esta prueba abre un journal con la app de antes de CL-4, lo cierra, y lo
	// vuelve a abrir con la de verdad.
	//
	// P-1 LLAMA A ESA LINEA LA MAS IRREVERSIBLE DE LA REBANADA y el plan pide confirmarla antes de
	// escribirla: un cuerpo de upgrade aplicado no vuelve a correr nunca, asi que un nombre mal
	// escrito no se edita -- solo se sustituye por otro, y el equivocado se queda aplicado en todo
	// journal que ya haya abierto. DEC-27 lleva el argumento de por que el nombre es el del AGREGADO.
	//
	// EL RENGLON QUE HACE QUE ESTE FIXTURE PUEDA DISTINGUIR ALGO es el que mide la ausencia: antes de
	// la migracion, leer la sexta global revienta con "Unknown property or method 'ReadVin' on type
	// 'Object'", porque una global de raiz sin asignar resuelve a un Object pelado. MEDIDO en
	// SPIKES-WarrantyEnvelope S2 fila 3 y reproducido en SPIKES-BuildOrder PROBE 5. Sin ese renglon
	// todo lo de abajo estaria verde aunque 'semilla' hubiera creado las seis globales desde el
	// principio.
	//
	// EL FALSIFICADOR Y LA MITAD POSITIVA NOMBRAN EL MISMO MIEMBRO, que es lo que decidio DEC-31: si
	// midieran miembros distintos, el falsificador estaria midiendo la ausencia de algo que la mitad
	// positiva nunca lee. Los dos van por ALedgerReadWithoutTheDoor.Vin.
	//
	// LO QUE S2 MIDIO Y ACA SE RE-MIDE CONTRA EL CostLedger DE VERDAD, fila por fila: +1 en regimen
	// (2), el falsificador (3), +2 en la migracion (4), la sexta contesta (5), las otras cinco
	// intactas (6), +1 para siempre despues (7), sobrevive otro replay (8), un journal NUEVO no paga
	// nada (9), y el orden de los upgrades no importa porque llavean por nombre (10).
	//
	// LO QUE NO SE REPITE, DICHO EN VEZ DE OMITIDO (C56): que la segunda entrada de una migracion la
	// cueste el TEXTO NUEVO DEL SCRIPT y no el cuerpo del upgrade lo midio SH-3 en
	// TheExtraEntryOfAMigrationIsTheNewScriptAndNotTheUpgradeBody, que esta en la suite y verde. Ese
	// fixture ya anoto que la sexta global costaria +2 por ese mismo motivo, y volver a medirlo aca
	// mediria el mismo mecanismo por segunda vez.
	[TestFixture]
	public sealed class TheSixthGlobalReachesAJournalThatHasTheOtherFive
	{
		private const string TheVin = "1HGCM82633A004352";
		private const string TheModel = "YARIS-2019";

		// Lo que se le escribe a cada una de las cinco globales viejas antes de reordenar, para que
		// una re-corrida silenciosa tenga por donde verse.
		private const string TheDeviceKey = "device-de-la-prueba-del-orden";
		private const string TheRevision = "R1";
		private const int TheOdometerAtDelivery = 30000;

		private static readonly DateTime TheDelivery =
			new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);

		private const string TheBranch = "Taller Curridabat";
		private const string TheStaySubject = "Cambio de aceite";

		private static readonly DateTime TheStayEntered =
			new DateTime(2026, 6, 10, 8, 30, 0, DateTimeKind.Utc);

		private static readonly DateTime TheStayCollected =
			new DateTime(2026, 6, 11, 16, 45, 0, DateTimeKind.Utc);

		private static readonly string[] TheStayDays = { "2026-06-10", "2026-06-11" };

		// El gasto con que se mide si el upgrade del libro volvio a correr: lo unico que se le puede
		// escribir a la sexta global, y lo que desaparece si su cuerpo se vuelve a ejecutar.
		private const string TheCostSubject = "Llantas nuevas";
		private const decimal TheAmount = 240000m;
		private const string TheCurrency = "CRC";

		private static readonly DateTime TheCostIncurred =
			new DateTime(2026, 3, 12, 14, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheYearFrom =
			new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheYearTo =
			new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc);

		private string folder;

		[SetUp]
		public void Setup()
		{
			folder = AFreshJournalFolder.Make();
		}

		[TearDown]
		public void TearDown()
		{
			AFreshJournalFolder.Sweep(folder);
		}

		// FILAS 3, 4, 5 Y 6. El journal de antes, el falsificador, la migracion, y las cinco viejas
		// intactas.
		[Test]
		public void AJournalWrittenBeforeTheLedgerExistedGetsItOnItsNextOpening()
		{
			// Un journal como los que ya existen: tiene los cinco nombres viejos y ninguno mas.
			AnAppFromBeforeTheLedger older = new AnAppFromBeforeTheLedger(TheVin, TheModel);
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
			Assert.That(AStayReadWithoutTheDoor.Vin(older), Is.EqualTo(TheVin),
				"Y las estadias tambien: 'estadias-en-taller' corrio.");

			Exception missing = Assert.Catch(() => ALedgerReadWithoutTheDoor.Vin(older));

			TestContext.Out.WriteLine(
				"S2 FILA 3 -- el falsificador, antes de la migracion: " + missing.Message);

			Assert.That(missing.Message,
				Does.Contain("Unknown property or method 'ReadVin' on type 'Object'"),
				"Y el libro de costos NO existe todavia, que es lo que hace que este fixture pueda "
				+ "distinguir la migracion de no haber hecho nada. Una global de raiz sin asignar "
				+ "resuelve a un Object pelado, asi que preguntarle cualquier cosa revienta NOMBRANDO "
				+ "lo que se le pidio -- y el mensaje va afirmado entero y no solo el throw, porque es "
				+ "lo unico que separa 'la global falta' de 'la global esta y el metodo no'. Sin este "
				+ "renglon, todo lo de abajo estaria verde aunque 'semilla' hubiera creado las seis.");

			older.Dispose();

			// La app de verdad, sobre el MISMO journal.
			DealershipCarApp current = AJournalTheCurrentAppOpened();

			Assert.That(ALedgerReadWithoutTheDoor.Vin(current), Is.EqualTo(TheVin),
				"S2 FILA 5: el upgrade nuevo se aplico donde faltaba, y el libro de costos sabe de que "
				+ "vehiculo son sus gastos.");

			DealershipCarAPI after = new DealershipCarAPI(current);

			Assert.That(after.ReadVin(), Is.EqualTo(TheVin),
				"S2 FILA 6: y las cinco globales que ya estaban no se tocaron: sus upgrades siguen "
				+ "aplicados y no volvieron a correr.");
			Assert.That(after.LastEventApplied(), Is.EqualTo(0L));
			Assert.That(after.ReadModel(), Is.EqualTo(TheModel));
			Assert.That(AProfileReadWithoutTheDoor.Vin(current), Is.EqualTo(TheVin));
			Assert.That(AStayReadWithoutTheDoor.Vin(current), Is.EqualTo(TheVin));

			current.Dispose();
		}

		// FILA 8: LA GLOBAL NUEVA SOBREVIVE AL REPLAY, que es lo que la separa de un campo de C#: se
		// vuelve a abrir por tercera vez y sigue contestando sin que nadie la haya vuelto a sembrar.
		[Test]
		public void TheSixthGlobalStillAnswersAfterAnotherReopening()
		{
			AJournalTheCurrentAppOpened().Dispose();

			DealershipCarApp reopened = AJournalTheCurrentAppOpened();

			Assert.That(ALedgerReadWithoutTheDoor.Vin(reopened), Is.EqualTo(TheVin));

			reopened.Dispose();
		}

		// FILAS 2, 4 Y 7: LO QUE CUESTA LA MIGRACION, en entradas. DOS en la apertura que migra, UNA
		// en toda apertura anterior y posterior. Los DELTAS son la medicion; los ids absolutos no
		// significan nada y no se comparan contra los de S2 ni contra los de PROBE 5.
		//
		// SE AFIRMA EL DELTA EXACTO Y NO QUE HAYA CRECIDO (C60): un "afterTheMigration > before" pasa
		// igual con +1 que con +2, y +1 querria decir que el cuerpo del upgrade nuevo no corrio.
		[Test]
		public void TheMigrationCostsTwoEntriesOnceAndThenOne()
		{
			AnAppFromBeforeTheLedger older = new AnAppFromBeforeTheLedger(TheVin, TheModel);
			older.ConfigureStorage(DatabaseType.FileSystem, "path=" + folder);
			older.Start();

			long onTheFirstOpening = older.Actor.CurrentEntryId;
			older.Dispose();

			AnAppFromBeforeTheLedger again = new AnAppFromBeforeTheLedger(TheVin, TheModel);
			again.ConfigureStorage(DatabaseType.FileSystem, "path=" + folder);
			again.Start();

			long beforeTheMigration = again.Actor.CurrentEntryId;
			again.Dispose();

			Assert.That(beforeTheMigration, Is.EqualTo(onTheFirstOpening + 1),
				"S2 FILA 2. En regimen, con cinco globales: UNA entrada por apertura, la del comando "
				+ "de la semilla, aunque los cinco upgrades ya esten aplicados y no hagan nada.");

			DealershipCarApp migrating = AJournalTheCurrentAppOpened();

			long afterTheMigration = migrating.Actor.CurrentEntryId;
			migrating.Dispose();

			Assert.That(afterTheMigration, Is.EqualTo(beforeTheMigration + 2),
				"S2 FILA 4. DOS en la apertura que migra. La magnitud es la que S2 y PROBE 5 midieron; "
				+ "lo que la causa lo corrigio SH-3 y es el TEXTO NUEVO del script, no el cuerpo del "
				+ "upgrade -- ver TheExtraEntryOfAMigrationIsTheNewScriptAndNotTheUpgradeBody.");

			DealershipCarApp afterwards = AJournalTheCurrentAppOpened();

			long steadyAgain = afterwards.Actor.CurrentEntryId;

			Assert.That(steadyAgain, Is.EqualTo(afterTheMigration + 1),
				"S2 FILA 7. Y a partir de ahi, UNA por apertura otra vez. Si esto creciera de a dos, "
				+ "el upgrade estaria corriendo en cada arranque en vez de una sola vez.");

			TestContext.Out.WriteLine(
				"S2 FILAS 2/4/7 -- CurrentEntryId por apertura, con la sexta global: "
				+ onTheFirstOpening + " -> " + beforeTheMigration + " -> " + afterTheMigration
				+ " -> " + steadyAgain);

			afterwards.Dispose();
		}

		// FILA 9, Y ES LA MITAD QUE MAS IMPORTA: UN JOURNAL NUEVO NO PAGA NADA. Nacido con seis
		// globales cuesta lo mismo que nacido con cinco, asi que la entrada extra de la migracion la
		// paga una poblacion FIJA Y DECRECIENTE -- los journals que existan el dia que CL-4 salga, una
		// vez cada uno -- y no es un impuesto permanente sobre nadie.
		//
		// Va comparado contra un journal nuevo de CINCO globales y no contra un numero escrito a mano:
		// el id absoluto depende de la semilla y de la version del motor, y una constante aca
		// envejeceria sin avisar.
		[Test]
		public void AFreshJournalBornWithSixGlobalsPaysWhatOneBornWithFivePays()
		{
			string another = AFreshJournalFolder.Make();

			try
			{
				AnAppFromBeforeTheLedger withFive = new AnAppFromBeforeTheLedger(TheVin, TheModel);
				withFive.ConfigureStorage(DatabaseType.FileSystem, "path=" + another);
				withFive.Start();

				long bornWithFive = withFive.Actor.CurrentEntryId;
				withFive.Dispose();

				DealershipCarApp withSix = AJournalTheCurrentAppOpened();

				long bornWithSix = withSix.Actor.CurrentEntryId;
				withSix.Dispose();

				TestContext.Out.WriteLine(
					"S2 FILA 9 -- journal recien nacido: con cinco globales " + bornWithFive
					+ ", con seis " + bornWithSix);

				Assert.That(bornWithSix, Is.EqualTo(bornWithFive),
					"Lo MISMO. Una global mas en un journal que nace no cuesta ni una entrada: los "
					+ "seis upgrades corren dentro del unico comando que la semilla ya escribia.");
			}
			finally
			{
				AFreshJournalFolder.Sweep(another);
			}
		}

		// EL NOMBRE, LETRA POR LETRA, Y ES LA LINEA MAS IRREVERSIBLE DE LA REBANADA. Un cuerpo de
		// upgrade aplicado no vuelve a correr nunca: un nombre mal escrito no se edita -- editarlo
		// revienta -- solo se sustituye por otro, y el equivocado se queda aplicado en todo journal
		// que ya haya abierto.
		//
		// SE MIDE CONTRA ESTADO ESCRITO, que es el unico instrumento que sirve. Un journal se abre con
		// una app que declara 'libro-de-costos' escrito a mano aca, se le registra un gasto por la
		// puerta, y despues lo abre la app DE VERDAD. Si los dos nombres calzan letra por letra, el
		// upgrade de la app de verdad no vuelve a correr y el gasto sigue ahi; si difieren en un solo
		// caracter, la app de verdad aplica un upgrade que a este journal le falta, 'ledger' se
		// reemplaza por un CostLedger recien hecho, y el gasto desaparece.
		//
		// EL CONTEO DE ENTRADAS NO SIRVE PARA ESTO. Dos apps con scripts distintos cuestan +2 aunque
		// los seis nombres calcen, porque el texto nuevo es una PLANTILLA nueva -- ver
		// TheExtraEntryOfAMigrationIsTheNewScriptAndNotTheUpgradeBody, que es donde eso quedo medido.
		//
		// LAS OTRAS PRUEBAS DE ESTE FIXTURE NO VEN ESTO: todas migran un journal al que la sexta
		// global le falta, y ahi cualquier nombre nuevo funciona igual de bien. Este es el unico
		// renglon que se pone rojo si alguien reescribe la linea de P-1.
		[Test]
		public void TheSixthUpgradeIsNamedExactlyLibroDeCostos()
		{
			AnAppWithTheLedgerDeclaredFirst spelledOutByHand =
				new AnAppWithTheLedgerDeclaredFirst(TheVin, TheModel);
			spelledOutByHand.ConfigureStorage(DatabaseType.FileSystem, "path=" + folder);
			spelledOutByHand.Start();

			new DealershipCarAPI(spelledOutByHand).RecordRecurringCost(
				TheCostIncurred, TheAmount, TheCurrency, TheCostSubject);

			Assert.That(
				new DealershipCarAPI(spelledOutByHand).CostEntryCountBetween(
					TheYearFrom, TheYearTo),
				Is.EqualTo(1),
				"El gasto quedo escrito en la global que creo el nombre escrito a mano aqui.");

			spelledOutByHand.Dispose();

			DealershipCarApp real = AJournalTheCurrentAppOpened();

			Assert.That(
				new DealershipCarAPI(real).CostEntryCountBetween(TheYearFrom, TheYearTo),
				Is.EqualTo(1),
				"Y SIGUE AHI despues de que la app de verdad abrio el journal. Eso solo pasa si el "
				+ "nombre de su sexto upgrade calza letra por letra con 'libro-de-costos': un nombre "
				+ "distinto seria un upgrade que a este journal le falta, correria al final del "
				+ "replay, y reemplazaria 'ledger' por un CostLedger recien hecho y vacio.");

			Assert.That(
				new DealershipCarAPI(real).CostEntriesBetween(TheYearFrom, TheYearTo).Says[0],
				Does.Contain(TheCostSubject),
				"Y es EL MISMO gasto y no otro: un libro recien hecho tendria cero filas, pero una "
				+ "cuenta sola no distingue un uno de otro uno.");

			real.Dispose();
		}

		// FILA 10: LOS UPGRADES LLAVEAN POR NOMBRE, NO POR ORDEN. Declarar el nuevo PRIMERO migra
		// igual -- los cinco viejos siguen aplicados y no vuelven a correr -- y cuesta lo mismo.
		//
		// Y SE MIDE CONTRA ESTADO ESCRITO, no contra lo que las globales contestan recien nacidas: el
		// VIN, el modelo y el punto de control son argumentos de la SIEMBRA, y una global re-sembrada
		// los contesta exactamente igual. Lo que SI se ve es lo que se le escribio DESPUES.
		[Test]
		public void TheOrderOfTheSixUpgradesInTheScriptDoesNotMatter()
		{
			AnAppFromBeforeTheLedger older = new AnAppFromBeforeTheLedger(TheVin, TheModel);
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
			before.RecordShopStay(
				TheBranch, TheStaySubject, TheStayEntered, TheStayCollected, TheStayDays);

			long beforeTheMigration = older.Actor.CurrentEntryId;
			older.Dispose();

			AnAppWithTheLedgerDeclaredFirst reordered =
				new AnAppWithTheLedgerDeclaredFirst(TheVin, TheModel);
			reordered.ConfigureStorage(DatabaseType.FileSystem, "path=" + folder);
			reordered.Start();

			Assert.That(reordered.Actor.CurrentEntryId, Is.EqualTo(beforeTheMigration + 2),
				"+2, igual que con el nuevo al final.");

			Assert.That(ALedgerReadWithoutTheDoor.Vin(reordered), Is.EqualTo(TheVin),
				"La sexta global contesta.");

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
			Assert.That(after.StayCountBetween("2026-01-01", "2026-12-31"), Is.EqualTo(1),
				"Y las estadias: la que se registro sigue registrada.");

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

		// La app tal como era antes de CL-4: CINCO globales de raiz. Existe SOLO para que la
		// migracion tenga contra que medirse -- un journal en el que el libro de costos falta de
		// verdad.
		//
		// LOS CINCO CUERPOS ESTAN COPIADOS PALABRA POR PALABRA de DealershipCarApp.OnHydrated, y esa
		// copia es la prueba: lo que se mide es que los cinco NOMBRES calcen exacto para que NO
		// vuelvan a correr. Un cuerpo parafraseado o un nombre mal escrito mediria otro journal que el
		// que la gente tiene, y lo mediria en verde. Copiar y no heredar tambien es a proposito:
		// DealershipCarApp es sealed, y sobreescribir OnHydrated reemplazaria los seis.
		private sealed class AnAppFromBeforeTheLedger : PerformanceV2
		{
			private readonly string vin;
			private readonly string modelCode;

			internal AnAppFromBeforeTheLedger(string vin, string modelCode)
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

		// Los MISMOS seis upgrades de la app de verdad, con el nuevo declarado PRIMERO. Existe solo
		// para la fila 10 -- que el orden no sea carga -- y para el renglon del nombre letra por
		// letra.
		private sealed class AnAppWithTheLedgerDeclaredFirst : PerformanceV2
		{
			private readonly string vin;
			private readonly string modelCode;

			internal AnAppWithTheLedgerDeclaredFirst(string vin, string modelCode)
				: base(DealershipCarApp.TheActorFor(vin), typeof(MaintenanceSchedule).Assembly)
			{
				this.vin = vin;
				this.modelCode = modelCode;
			}

			protected override void OnHydrated()
			{
				Using(@"
					upgrade('libro-de-costos') {
						ledger = CostLedger(@vin);
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
	}
}
