using System;
using System.Collections.Generic;
using System.IO;
using DealershipCar.App;
using DealershipCar.Mcp;
using DealershipCar.Mcp.Tools;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// "LA APP NUNCA SE ABRIO EN ESTA COMPUTADORA" NO ES UN ERROR, es un estado, y contestarlo es la
	// pieza nueva de T9.
	//
	// Con el OnHydrated vacio, un journal que nunca se escribio no tiene NINGUNA global de raiz: ni
	// schedule ni logbook. Toda lectura revienta con LanguageException del motor -- medido: "Unknown
	// property or method 'ReadVin' on type 'Object'" -- y una excepcion asi, saliendo de una tool,
	// le llega al asistente como un fallo del protocolo. La persona leeria "la herramienta fallo"
	// cuando el hecho es "todavia no abriste tu app".
	[TestFixture]
	public sealed class AFolderTheAppNeverOpenedIsNotAnError
	{
		private const string TheVin = "1HGCM82633A004352";

		private string folder;

		[SetUp]
		public void ATemporaryFolderWithNothingInIt()
		{
			folder = Path.Combine(
				AScheduleOnARealJournal.TheRootOfEveryTemporaryJournal, Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(folder);
		}

		[TearDown]
		public void SweepIt()
		{
			try { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
			catch (IOException) { }
			catch (UnauthorizedAccessException) { }
		}

		[Test]
		public void EveryToolAnswersThatThereIsNoDataYetInsteadOfThrowing()
		{
			var tools = new DealershipCarTools(new JournalReader(folder, ""));
			var sheet = new ModelSpecificationTools(new JournalReader(folder, ""));

			MaintenanceReview review = tools.Review();
			OperationStanding one = tools.CheckOne(MaintenanceOperation.FluidChange);
			ReadingFreshness fresh = tools.HowUpToDate();
			SpecificationSheet takes = sheet.WhatMyCarTakes();
			WarrantyScope scope = sheet.DoesTheWarrantyCover("POWERTRAIN");
			ServicePricing pricing =
				sheet.WhatTheServiceCosts(MaintenanceOperation.FluidChange, 10000);

			Assert.That(review.HasData, Is.False);
			Assert.That(one.HasData, Is.False);
			Assert.That(fresh.HasData, Is.False);
			Assert.That(takes.HasData, Is.False);
			Assert.That(scope.HasData, Is.False);
			Assert.That(pricing.HasData, Is.False);

			// Y LO DICE, en vez de devolver ceros mudos: un tablero en cero es indistinguible de un
			// vehiculo al dia, que es la lectura equivocada mas cara que este servidor puede inducir.
			Assert.That(review.Detail, Does.Contain("has not been opened"));
			Assert.That(one.Detail, Does.Contain("has not been opened"));
			Assert.That(fresh.Detail, Does.Contain("has not been opened"));
			Assert.That(takes.Detail, Does.Contain("has not been opened"));
			Assert.That(scope.Detail, Does.Contain("has not been opened"));
			Assert.That(pricing.Detail, Does.Contain("has not been opened"));

			// Y NINGUNA LISTA VACIA SE LEE COMO UNA RESPUESTA: sin datos no hay seis fluidos que
			// contestaron que no, hay una carpeta que nadie abrio.
			Assert.That(takes.Fluids, Is.Empty);
			Assert.That(takes.Tyres, Is.Empty);
			Assert.That(scope.CoveredSystems, Is.Empty);
			Assert.That(pricing.Prices, Is.Empty);

			// Lo que se pregunto vuelve igual, para que el asistente sepa de que estaba hablando
			// aunque no haya datos.
			Assert.That(one.Operation, Is.EqualTo("FluidChange"));
			Assert.That(scope.System, Is.EqualTo("POWERTRAIN"));
			Assert.That(pricing.Operation, Is.EqualTo("FluidChange"));
			Assert.That(pricing.MilestoneAsked, Is.EqualTo(10000));
		}

		// LA PREGUNTA VA ANTES DE ABRIR EL ACTOR, Y ESTA ES LA MEDICION QUE LO OBLIGA. Start() sobre
		// una carpeta que nunca tuvo actor NO falla: CREA el actor, con un journal vacio en disco. O
		// sea que un host de solo lectura mal ordenado ESCRIBE en la carpeta de la persona la primera
		// vez que se le pregunta algo -- y deja la carpeta en un estado donde "hay journal" contesta
		// que si para siempre.
		[Test]
		public void OpeningTheActorIsWhatCreatesTheJournalSoTheQuestionHasToComeFirst()
		{
			Assert.That(ReadOnlyDealershipCarApp.AJournalExistsFor(folder, TheVin), Is.False);

			var reader = new ReadOnlyDealershipCarApp(TheVin);
			try
			{
				reader.ConfigureStorage(Puppeteer.DatabaseType.FileSystem, "path=" + folder);
				reader.Start();
			}
			finally
			{
				try { reader.Actor.GracefulExit(); } catch { }
			}

			Assert.That(ReadOnlyDealershipCarApp.AJournalExistsFor(folder, TheVin), Is.True,
				"Abrir el actor CREA el journal aunque el host no siembre nada. Por eso la caratula "
				+ "pregunta AJournalExistsFor antes de abrir: preguntar despues seria haber escrito "
				+ "ya en la carpeta de una persona que ni siquiera instalo la app.");
		}

		// Y COMO ESE ESTADO ES ALCANZABLE, TAMBIEN SE CONTESTA. Una carpeta con el actor creado pero
		// sin ninguna entrada -- la deja una version anterior de esta caratula, o una app que murio
		// antes de sembrar -- pasa la pregunta del directorio y despues no tiene globals: sin la
		// segunda guarda, cada tool reventaria con LanguageException.
		[Test]
		public void AJournalWithNoEntriesAtAllIsStillNoDataYet()
		{
			var reader = new ReadOnlyDealershipCarApp(TheVin);
			try
			{
				reader.ConfigureStorage(Puppeteer.DatabaseType.FileSystem, "path=" + folder);
				reader.Start();

				Assert.That(reader.Actor.CurrentEntryId, Is.EqualTo(0),
					"Un actor recien creado por un host que no siembra no tiene ni una entrada. Ese "
					+ "cero es la segunda guarda.");
			}
			finally
			{
				try { reader.Actor.GracefulExit(); } catch { }
			}

			Assert.That(ReadOnlyDealershipCarApp.AJournalExistsFor(folder, TheVin), Is.True,
				"La carpeta ya existe, asi que la primera guarda deja pasar.");

			var tools = new DealershipCarTools(new JournalReader(folder, ""));
			var sheet = new ModelSpecificationTools(new JournalReader(folder, ""));

			Assert.That(tools.Review().HasData, Is.False);
			Assert.That(tools.CheckOne(MaintenanceOperation.Inspection).HasData, Is.False);
			Assert.That(tools.HowUpToDate().HasData, Is.False);
			Assert.That(sheet.WhatMyCarTakes().HasData, Is.False);
			Assert.That(sheet.DoesTheWarrantyCover("POWERTRAIN").HasData, Is.False);
			Assert.That(
				sheet.WhatTheServiceCosts(MaintenanceOperation.FluidChange, 10000).HasData,
				Is.False);
		}

		// EL VEHICULO SE DESCUBRE DE LA CARPETA. Pedirle tambien el VIN al archivo de configuracion
		// seria una segunda fuente de verdad para la identidad del vehiculo, y la de afuera es la que
		// se queda vieja.
		[Test]
		public void TheVehicleIsDiscoveredFromTheFolderAndNotConfigured()
		{
			Assert.That(ReadOnlyDealershipCarApp.TheVehiclesIn(folder), Is.Empty);

			using (var journal = new AScheduleOnARealJournal(TheVin, "Standard", folder))
			{
				IReadOnlyList<string> found = ReadOnlyDealershipCarApp.TheVehiclesIn(folder);

				Assert.That(found, Is.EqualTo(new[] { TheVin }));

				var tools = new DealershipCarTools(new JournalReader(folder, ""));

				Assert.That(tools.Review().Vin, Is.EqualTo(TheVin),
					"Y el VIN que se contesta sale del JOURNAL, no del nombre de la carpeta: "
					+ "ReadVin lo lee de la agenda rehidratada.");
			}
		}

		// LA PERSONA PUEDE INSTALAR Y ABRIR SU APP MIENTRAS EL ASISTENTE YA ESTA CONECTADO, y eso
		// tiene que verse sin reiniciar nada. Es la misma razon por la que se rehidrata en cada
		// consulta, un paso antes: si el VIN se descubriera UNA vez al arrancar el proceso, este
		// servidor contestaria "todavia no hay datos" para siempre, incluso con el journal ya escrito
		// al lado.
		//
		// Sin esta prueba nada lo cubre, porque toda carpeta de prueba existe antes de que se le
		// pregunte: el defecto de cachear el VIN corrio LIMPIO en la falsificacion de T9.
		[Test]
		public void TheAppCanBeOpenedForTheFirstTimeWhileTheAssistantIsAlreadyConnected()
		{
			var tools = new DealershipCarTools(new JournalReader(folder, ""));

			Assert.That(tools.Review().HasData, Is.False,
				"Antes de que la persona abra su app.");

			using (var journal = new AScheduleOnARealJournal(TheVin, "Standard", folder))
			{
				journal.Api.SeedPlanOperation(
					"FluidChange", "TimeOrDistance", 365, 10000, "UnitA", true, false,
					new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc), 12000, "UnitA");

				// LA MISMA INSTANCIA de tools, sin reconstruir nada: es literalmente el proceso MCP
				// que quedo vivo mientras la persona instalaba su app.
				MaintenanceReview afterwards = tools.Review();

				Assert.That(afterwards.HasData, Is.True,
					"El servidor no se entero de que la app se abrio. Un VIN descubierto una sola vez "
					+ "al arrancar deja al asistente diciendole a la persona que abra una app que ya "
					+ "abrio.");
				Assert.That(afterwards.Vin, Is.EqualTo(TheVin));
			}
		}

		// DOS VEHICULOS EN LA MISMA CARPETA: se contesta por uno, siempre el mismo, y SE DICE cual.
		//
		// Nada mas en esta rebanada pone dos vehiculos en una carpeta -- la consola tiene un VIN, las
		// pruebas una carpeta por prueba -- asi que sin esto ni el orden ni el aviso estan cubiertos:
		// los dos defectos corrieron LIMPIOS en la falsificacion de T9.
		[Test]
		public void TwoVehiclesInOneFolderAreAnsweredDeterministicallyAndSaidSo()
		{
			const string AnotherVin = "1FTFW1ET5DFA00001";

			using (var first = new AScheduleOnARealJournal(TheVin, "Standard", folder))
			using (var second = new AScheduleOnARealJournal(AnotherVin, "Standard", folder))
			{
				Assert.That(ReadOnlyDealershipCarApp.TheVehiclesIn(folder),
					Is.EqualTo(new[] { AnotherVin, TheVin }),
					"Ordenados: sin orden, cual vehiculo contesta depende de como el sistema de "
					+ "archivos devuelva la lista, y la misma pregunta daria dos respuestas.");

				MaintenanceReview review = new DealershipCarTools(new JournalReader(folder, "")).Review();

				Assert.That(review.Vin, Is.EqualTo(AnotherVin), "El primero por orden, siempre.");
				Assert.That(review.Detail, Does.Contain("2 vehicles"),
					"Y se avisa. Contestar en silencio por un vehiculo haria que la persona leyera la "
					+ "agenda del otro auto creyendo que es la de este.");

				// Y con el VIN fijado se contesta por ese, sin aviso.
				MaintenanceReview pinned =
					new DealershipCarTools(new JournalReader(folder, TheVin)).Review();

				Assert.That(pinned.Vin, Is.EqualTo(TheVin));
				Assert.That(pinned.Detail, Is.EqualTo(""));
			}
		}

		// EL VIN SALE DEL JOURNAL, NO DEL NOMBRE DE LA CARPETA, y hacen falta las dos cosas: la
		// carpeta es como se ENCUENTRA el vehiculo, y el journal es lo que DICE quien es.
		//
		// Para poder distinguirlas hay que hacerlas discrepar, y en la vida no discrepan: el nombre
		// del actor se arma del VIN. Asi que se copia el journal de un vehiculo a una carpeta con el
		// nombre de otro. Sin este aparejo, leer el VIN del nombre de la carpeta pasaria todas las
		// pruebas -- corrio LIMPIO en la falsificacion de T9.
		[Test]
		public void TheVinComesFromTheJournalAndNotFromTheFolderName()
		{
			const string TheNameOnTheFolder = "1FTFW1ET5DFA00001";

			using (var journal = new AScheduleOnARealJournal(TheVin, "Standard", folder))
			{
				journal.Api.SeedPlanOperation(
					"FluidChange", "TimeOrDistance", 365, 10000, "UnitA", true, false,
					new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc), 12000, "UnitA");

				string elsewhere = Path.Combine(folder, "copia");
				CopyTheActorFolder(
					Path.Combine(folder, DealershipCarApp.TheActorFor(TheVin)),
					Path.Combine(elsewhere, DealershipCarApp.TheActorFor(TheNameOnTheFolder)));

				Assert.That(ReadOnlyDealershipCarApp.TheVehiclesIn(elsewhere),
					Is.EqualTo(new[] { TheNameOnTheFolder }),
					"La carpeta dice un vehiculo...");

				Assert.That(new DealershipCarTools(new JournalReader(elsewhere, "")).Review().Vin,
					Is.EqualTo(TheVin),
					"...y el journal dice el otro, que es el que manda. La identidad del vehiculo la "
					+ "lleva la agenda journalizada, no como se llame la carpeta que la contiene.");
			}
		}

		// Se copia con FileShare.ReadWrite porque el actor tiene el journal ABIERTO mientras el
		// proceso vive: Performance.Dispose() no lo suelta (medido en T6).
		private static void CopyTheActorFolder(string from, string to)
		{
			foreach (string each in Directory.GetFiles(from, "*", SearchOption.AllDirectories))
			{
				string landing = Path.Combine(to, Path.GetRelativePath(from, each));
				Directory.CreateDirectory(Path.GetDirectoryName(landing));

				using (var open = new FileStream(
					each, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
				using (var copy = new FileStream(landing, FileMode.Create, FileAccess.Write))
					open.CopyTo(copy);
			}
		}

		// UNA SUBCARPETA AJENA NO ES UN VEHICULO. La carpeta es de la persona y puede tener
		// cualquier cosa adentro; encontrarse algo que no es nuestro es normal, no un defecto.
		[Test]
		public void SomethingElseInThePersonsFolderIsNotMistakenForAVehicle()
		{
			Directory.CreateDirectory(Path.Combine(folder, "fotos-del-auto"));
			Directory.CreateDirectory(Path.Combine(folder, "examenes"));
			Directory.CreateDirectory(Path.Combine(folder, "maintenance-sin-journal"));

			Assert.That(ReadOnlyDealershipCarApp.TheVehiclesIn(folder), Is.Empty,
				"Ni una carpeta cualquiera ni una que se llame como un actor pero no tenga journal.");
			Assert.That(DealershipCarApp.TheVinOf("examenes"), Is.EqualTo(""));
			Assert.That(DealershipCarApp.TheVinOf("maintenance-" + TheVin), Is.EqualTo(TheVin),
				"Y el inverso del nombre del actor si reconoce el suyo.");
			Assert.That(new DealershipCarTools(new JournalReader(folder, "")).Review().HasData,
				Is.False);
		}
	}
}
