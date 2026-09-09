using System;
using System.IO;
using Choreography.Theater;
using Concesionario.App;
using NUnit.Framework;
using Puppeteer;

namespace Concesionario.Dominio.Pruebas
{
	// LA TRAMPA DE C26, HECHA FALSABLE. T8 le agrega a la app una SEGUNDA global de raiz -- la
	// bitacora de eventos -- al lado de la agenda y nunca adentro: ni el punto de control ni la
	// credencial son hechos de mantenimiento, asi que ponerlos en MaintenanceSchedule seria el
	// error de modelado, no la solucion.
	//
	// Y LA GUARDA DE upgrade ES UNA-VEZ-POR-ACTOR. upgrade('semilla') YA CORRIO en todo journal de
	// Concesionario que exista: los de la historia de T7 y los de cualquier persona que haya
	// abierto la app. Una linea agregada ADENTRO de 'semilla' no le llegaria a ninguno de ellos, en
	// silencio -- el actor ya tiene la marca y el cuerpo no vuelve a correr. Peor: editar el cuerpo
	// de un upgrade ya aplicado revienta con LanguageException.
	//
	// Asi que la global nueva llega bajo un NOMBRE NUEVO. Esta prueba abre un journal con una app
	// que solo sabe de 'semilla' -- la de antes de T8 -- lo cierra, y lo vuelve a abrir con la de
	// verdad. Si la migracion no llegara, la bitacora no contestaria.
	//
	// LOS DOS UPGRADES VIAJAN EN UN SOLO SCRIPT, y eso tambien se mide aca: cada PerformCommand deja
	// una entrada en CADA arranque aunque el upgrade ya este aplicado y no haga nada (medido en T6),
	// asi que dos comandos costarian dos entradas por apertura para siempre.
	[TestFixture]
	public sealed class TheSecondGlobalReachesAJournalThatOnlyHasTheSeed
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

		[Test]
		public void AJournalWrittenBeforeTheLogbookExistedGetsItOnItsNextOpening()
		{
			// Un journal como los que ya existen: solo tiene 'semilla'.
			AnAppFromBeforeTheLogbook older = new AnAppFromBeforeTheLogbook(TheVin);
			older.ConfigureStorage(DatabaseType.FileSystem, "path=" + folder);
			older.Start();

			DealershipCarAPI before = new DealershipCarAPI(older);

			Assert.That(before.ReadVin(), Is.EqualTo(TheVin),
				"La agenda si existe: 'semilla' corrio.");

			Assert.That(() => before.LastEventApplied(), Throws.Exception,
				"Y la bitacora NO existe todavia, que es lo que hace que esta prueba pueda "
				+ "distinguir la migracion de no haber hecho nada. Sin este renglon, el de abajo "
				+ "estaria verde aunque 'semilla' hubiera creado las dos globales.");

			older.Dispose();

			// La app de verdad, sobre el MISMO journal.
			DealershipCarApp current = new DealershipCarApp(
				TheVin, "Standard", AScheduleOnARealJournal.TheModelAtDelivery);
			current.ConfigureStorage(DatabaseType.FileSystem, "path=" + folder);
			current.Start();

			DealershipCarAPI after = new DealershipCarAPI(current);

			Assert.That(after.LastEventApplied(), Is.EqualTo(0L),
				"El upgrade nuevo se aplico donde faltaba. Cero es 'todavia no se leyo nada', que "
				+ "es un valor legal de esta escala: los eventos se numeran desde uno.");

			Assert.That(after.ReadVin(), Is.EqualTo(TheVin),
				"Y la agenda que ya estaba no se toco: el upgrade viejo sigue aplicado y no volvio "
				+ "a correr.");

			current.Dispose();
		}

		// LA SEMILLA CUESTA UNA ENTRADA POR ARRANQUE, no dos. Es lo que compra meter los dos
		// upgrades en un mismo script, y va medido porque es facil de perder sin que nada se rompa:
		// el costo se pagaria en cada apertura de cada persona, para siempre.
		[Test]
		public void TheSeedStillCostsExactlyOneEntryPerOpening()
		{
			DealershipCarApp app = new DealershipCarApp(
				TheVin, "Standard", AScheduleOnARealJournal.TheModelAtDelivery);
			app.ConfigureStorage(DatabaseType.FileSystem, "path=" + folder);
			app.Start();

			long afterFirstOpening = app.Actor.CurrentEntryId;
			app.Dispose();

			DealershipCarApp again = new DealershipCarApp(
				TheVin, "Standard", AScheduleOnARealJournal.TheModelAtDelivery);
			again.ConfigureStorage(DatabaseType.FileSystem, "path=" + folder);
			again.Start();

			Assert.That(again.Actor.CurrentEntryId, Is.EqualTo(afterFirstOpening + 1),
				"Reabrir crece UNA entrada: la de la semilla, que OnHydrated deja en cada "
				+ "hidratacion aunque los dos upgrades ya esten aplicados y no hagan nada.");

			again.Dispose();
		}

		// La app tal como era antes de T8: una sola global de raiz. Existe SOLO para que la
		// migracion tenga contra que medirse -- un journal en el que la bitacora falta de verdad.
		private sealed class AnAppFromBeforeTheLogbook : PerformanceV2
		{
			private readonly string vin;

			internal AnAppFromBeforeTheLogbook(string vin)
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
