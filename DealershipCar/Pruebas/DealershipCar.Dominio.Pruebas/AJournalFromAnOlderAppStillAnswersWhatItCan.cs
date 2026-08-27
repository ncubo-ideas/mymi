using System;
using System.IO;
using Choreography.Theater;
using DealershipCar.App;
using DealershipCar.Mcp;
using DealershipCar.Mcp.Tools;
using NUnit.Framework;
using Puppeteer;

namespace DealershipCar.Dominio.Pruebas
{
	// UN JOURNAL ESCRITO POR UNA VERSION ANTERIOR DE LA APP, y por que un LECTOR tiene que
	// sobrevivirlo de a una global.
	//
	// UN LECTOR NO PUEDE MIGRAR, y ahi esta el nudo. `upgrade('bitacora-de-eventos')` se aplica en
	// OnHydrated, y el de este host esta VACIO a proposito: crear la global que falta seria escribir
	// en el journal de la persona, que es lo unico que T9 promete no hacer. Asi que el estado "tiene
	// agenda pero no tiene bitacora" es alcanzable, es permanente hasta que la persona abra su app, y
	// no lo puede arreglar este proceso.
	//
	// SE ENCONTRO APUNTANDO EL SERVIDOR A UNA CARPETA DE VERDAD y no con una prueba: la carpeta por
	// defecto de este repo -- ~/DealershipCar-journal -- la escribio T7, antes de que la bitacora
	// existiera, y las tres tools volvieron con "An error occurred invoking..." porque
	// `logbook.LastEvent()` sobre un journal sin esa global tira LanguageException. Ninguna prueba
	// podia verlo: toda carpeta de prueba la escribe la app de HOY.
	//
	// El host viejo de aca es el mismo que usa TheSecondGlobalReachesAJournalThatOnlyHasTheSeed, y
	// esta repetido a proposito: es una forma HISTORICA que no va a cambiar nunca, y cada fixture de
	// esta suite es dueño de su propio aparejo.
	[TestFixture]
	public sealed class AJournalFromAnOlderAppStillAnswersWhatItCan
	{
		private const string TheVin = "1HGCM82633A004352";

		private string folder;

		[SetUp]
		public void AJournalWithTheScheduleButNoLogbook()
		{
			folder = Path.Combine(
				AScheduleOnARealJournal.TheRootOfEveryTemporaryJournal, Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(folder);

			var older = new AnAppFromBeforeTheLogbook(TheVin);
			older.ConfigureStorage(DatabaseType.FileSystem, "path=" + folder);
			older.Start();

			var api = new DealershipCarAPI(older);
			api.SeedPlanOperation(
				"FluidChange", "TimeOrDistance", 365, 10000, "UnitA", true, false,
				new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc), 12000, "UnitA");
			api.Evaluate(new DateTime(2027, 3, 1, 0, 0, 0, DateTimeKind.Utc));

			// EL RENGLON QUE HACE QUE ESTA PRUEBA PUEDA DISTINGUIR ALGO: la bitacora falta DE VERDAD.
			// Sin el, todo lo de abajo estaria verde aunque 'semilla' hubiera creado las dos globales.
			Assert.That(() => api.LastEventApplied(), Throws.Exception);

			older.Dispose();
		}

		[TearDown]
		public void SweepIt()
		{
			try { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
			catch (IOException) { }
			catch (UnauthorizedAccessException) { }
		}

		[Test]
		public void TheScheduleStillAnswersAndTheMissingCheckpointIsSaidOutLoud()
		{
			var tools = new DealershipCarTools(new JournalReader(folder, ""));

			MaintenanceReview review = tools.Review();

			Assert.That(review.HasData, Is.True,
				"La agenda SI esta: 'semilla' corrio hace tiempo, y la agenda es justo lo que la "
				+ "persona pregunta. Negarle todo por un campo que le falta seria peor que decirle "
				+ "que ese campo le falta.");
			Assert.That(review.Vin, Is.EqualTo(TheVin));
			Assert.That(review.Operations, Has.Count.EqualTo(9));
			Assert.That(review.Evaluated, Is.True);
			Assert.That(review.AnythingDue, Is.True);
			Assert.That(review.NextToDo, Is.EqualTo("FluidChange"));

			Assert.That(review.LastEventKnown, Is.False,
				"Y el punto de control se declara AUSENTE con una bandera, no con un cero mudo: un "
				+ "cero es un valor legal de esa escala -- 'todavia no se leyo nada' -- asi que sin "
				+ "la bandera el asistente diria 'no entro ningun evento' donde el hecho es 'esta "
				+ "app no llevaba la cuenta'.");
			Assert.That(review.LastEventApplied, Is.EqualTo(0L));
			Assert.That(review.Detail, Does.Contain("older version of the app"));
			Assert.That(review.Detail, Does.Contain("open their app once"));
		}

		[Test]
		public void EveryToolSurvivesItAndNoneOfThemThrows()
		{
			var tools = new DealershipCarTools(new JournalReader(folder, ""));

			Assert.That(tools.Review().HasData, Is.True);
			Assert.That(tools.HowUpToDate().HasData, Is.True);
			Assert.That(tools.HowUpToDate().LastEventKnown, Is.False);

			foreach (MaintenanceOperation each in TheOperationKind.All)
			{
				OperationStanding standing = tools.CheckOne(each);

				Assert.That(standing.HasData, Is.True, $"'{each}' revento.");
				Assert.That(standing.LastEventKnown, Is.False);
				Assert.That(standing.Reason, Is.Not.Empty);
			}
		}

		// EL CONTROL, y es el que impide que la bandera se vuelva un "siempre false". Sobre un journal
		// de la app de HOY el punto de control SI se conoce.
		[Test]
		public void AndOnAJournalTheCurrentAppWroteTheCheckpointIsKnown()
		{
			using (var current = new AScheduleOnARealJournal(TheVin, "Standard"))
			{
				current.Api.SeedPlanOperation(
					"FluidChange", "TimeOrDistance", 365, 10000, "UnitA", true, false,
					new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc), 12000, "UnitA");

				var tools = new DealershipCarTools(new JournalReader(current.Folder, ""));

				Assert.That(tools.Review().LastEventKnown, Is.True,
					"Si esto se pone rojo, la bandera se volvio una constante y la prueba de arriba "
					+ "no prueba nada.");
				Assert.That(tools.Review().Detail, Is.EqualTo(""),
					"Y no se le avisa nada a la persona, porque no hay nada que avisarle.");
			}
		}

		// ABRIR CON LA APP DE HOY LO ARREGLA, que es la salida que el detail le pide a la persona. Se
		// mide para que el consejo no sea una suposicion.
		[Test]
		public void OpeningTheCurrentAppOnceIsWhatFixesIt()
		{
			var tools = new DealershipCarTools(new JournalReader(folder, ""));

			Assert.That(tools.Review().LastEventKnown, Is.False);

			var current = new DealershipCarApp(TheVin, "Standard");
			current.ConfigureStorage(DatabaseType.FileSystem, "path=" + folder);
			current.Start();
			current.Dispose();

			Assert.That(tools.Review().LastEventKnown, Is.True,
				"Una sola apertura de la app de verdad aplica upgrade('bitacora-de-eventos') donde "
				+ "faltaba. Migrar es del ESCRITOR: este proceso no puede y no debe.");
			Assert.That(tools.Review().Detail, Is.EqualTo(""));
		}

		// La app tal como era antes de T8: una sola global de raiz.
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
