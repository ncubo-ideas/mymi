using System;
using System.IO;
using Choreography.Theater;
using Concesionario.App;
using Concesionario.App.Views;
using Concesionario.Wasm;
using NUnit.Framework;
using Puppeteer;

namespace Concesionario.Dominio.Pruebas
{
	// EL CASO QUE UN FIXTURE NUNCA ES Y UNA CARATULA SI SE ENCUENTRA: el journal de una persona
	// escrito por una version de la app anterior a la especificacion.
	//
	// LA CARATULA MCP NO PUEDE MIGRARLO (C33). Su OnHydrated esta vacio a proposito -- si sembrara,
	// cada pregunta del asistente escribiria en el journal de la persona -- asi que ahi la ausencia
	// de la tercera global es PERMANENTE hasta que la persona abra su app, y la respuesta se degrada
	// global por global, con un flag al lado del valor.
	//
	// EL BROWSER ESTA EN LA POSICION CONTRARIA Y POR ESO NO LLEVA FLAG: es el ESCRITOR. AppState
	// abre con DealershipCarApp, cuyo OnHydrated aplica el upgrade que falte, asi que para cuando
	// una seccion lee, la tercera global existe. Eso se COMPRUEBA aca en vez de suponerse, que es
	// justo lo que C33 pide: la forma solo aparece en un journal que sobrevivio a una version del
	// software.
	//
	// Y SE COMPRUEBA CON LAS CONSTANTES DE LA CARATULA, no con las del aparejo: el perfil y el
	// modelo con que el browser siembra son los que de verdad cruzan en cada apertura de un journal
	// sin sembrar.
	[TestFixture]
	public sealed class TheBrowserMigratesAnOlderJournalInsteadOfDegrading
	{
		private const string TheVin = "1HGCM82633A004352";

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

		// LA MEDICION ENTERA, EN UN SOLO METODO PORQUE LA LEY ESTA EN LA DIFERENCIA ENTRE LOS DOS
		// MOMENTOS: antes de abrir con la app de verdad, componer la hoja REVIENTA; despues, sale
		// entera y sin ningun flag. Separarlos en dos pruebas dejaria la segunda verde aunque la
		// especificacion hubiera estado ahi desde el principio.
		[Test]
		public void AJournalFromBeforeTheSpecificationIsMigratedByTheBrowsersOwnOpening()
		{
			AnAppFromBeforeTheSpecification older = new AnAppFromBeforeTheSpecification(TheVin);
			older.ConfigureStorage(DatabaseType.FileSystem, "path=" + folder);
			older.Start();

			var beforeTheApp = new DealershipCarAPI(older);

			Assert.That(beforeTheApp.ReadVin(), Is.EqualTo(TheVin),
				"La agenda si esta: es un journal completo y usable, no uno roto.");

			Exception missing = Assert.Catch(
				() => WhatTheManufacturerPublished.ReadThrough(
					beforeTheApp, TheLanguage.OfTheRecord));

			Assert.That(missing, Is.InstanceOf<LanguageException>(),
				"EL FALSIFICADOR DE ESTA PRUEBA: sin la tercera global, componer la hoja revienta. "
				+ "Sin este renglon, lo de abajo estaria verde aunque la especificacion hubiera "
				+ "existido siempre y no se hubiera migrado nada.");

			older.Dispose();

			// La app del browser, con SUS constantes, sobre el MISMO journal.
			DealershipCarApp asTheBrowserOpensIt = new DealershipCarApp(
				TheVin, AppState.TheProfileAtDelivery, AppState.TheModelAtDelivery);
			asTheBrowserOpensIt.ConfigureStorage(DatabaseType.FileSystem, "path=" + folder);
			asTheBrowserOpensIt.Start();

			WhatTheManufacturerPublished sheet = WhatTheManufacturerPublished.ReadThrough(
				new DealershipCarAPI(asTheBrowserOpensIt), TheLanguage.OfTheRecord);

			Assert.That(sheet.Model, Is.EqualTo(AppState.TheModelAtDelivery));
			Assert.That(sheet.HasASealedRevision, Is.False,
				"Migrar crea la especificacion vacia; no publica nada. La seccion dice que todavia "
				+ "no hay nada publicado, que es una respuesta y no un error.");
			Assert.That(sheet.Fluids, Has.Count.EqualTo(6));
			Assert.That(sheet.Tyres, Has.Count.EqualTo(3));
			Assert.That(sheet.Coverage.Published, Is.False);

			asTheBrowserOpensIt.Dispose();
		}

		// Y EL MODELO QUE LA HOJA CONTESTA ES EL QUE SE SEMBRO Y NO UNO FIJO. Hace falta decirlo
		// aparte: el aparejo del nivel B siembra la MISMA cadena que la caratula, asi que la
		// afirmacion de arriba no puede distinguir una lectura de una constante. Con un modelo
		// distinto en una carpeta distinta, si.
		[Test]
		public void TheModelOnTheSheetIsTheOneThatWasSownAndNotAFixedString()
		{
			const string AnotherModel = "SM-COUPE-2027";

			DealershipCarApp another = new DealershipCarApp(
				TheVin, AppState.TheProfileAtDelivery, AnotherModel);
			another.ConfigureStorage(DatabaseType.FileSystem, "path=" + folder);
			another.Start();

			Assert.That(
				WhatTheManufacturerPublished.ReadThrough(
					new DealershipCarAPI(another), TheLanguage.OfTheRecord).Model,
				Is.EqualTo(AnotherModel));

			Assert.That(AnotherModel, Is.Not.EqualTo(AppState.TheModelAtDelivery),
				"Y los dos modelos son distintos, que es lo unico que hace que el renglon de arriba "
				+ "distinga algo.");

			another.Dispose();
		}

		// Y EL PERFIL CON QUE EL BROWSER SIEMBRA CRUZA DE VERDAD. Es la otra constante que viaja en
		// cada apertura de un journal vacio, y un nombre que el canon no escribe deja la app sin
		// poder abrir -- no muestra una fila de menos, no abre.
		[Test]
		public void TheProfileTheBrowserSowsWithIsAcceptedWhenTheJournalIsEmpty()
		{
			DealershipCarApp fresh = new DealershipCarApp(
				TheVin, AppState.TheProfileAtDelivery, AppState.TheModelAtDelivery);
			fresh.ConfigureStorage(DatabaseType.FileSystem, "path=" + folder);

			Assert.DoesNotThrow(() => fresh.Start());
			Assert.That(new DealershipCarAPI(fresh).ReadVin(), Is.EqualTo(TheVin));

			fresh.Dispose();
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
					p["usage", typeof(Enum)]   = AppState.TheProfileAtDelivery;
				})
				.PerformCommand();
			}
		}
	}
}
