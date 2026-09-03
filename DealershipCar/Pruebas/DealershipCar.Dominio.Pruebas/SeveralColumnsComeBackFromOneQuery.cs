using System;
using Choreography.Theater;
using DealershipCar.App;
using NUnit.Framework;
using Puppeteer;

namespace DealershipCar.Dominio.Pruebas
{
	// VARIAS COLUMNAS string[] DE VUELTA DESDE UN SOLO PerformQuery, que es lo que un DTO de registro
	// necesita para que una fila sea una fila y no siete lecturas que alguien vuelve a coser afuera.
	// Nacio midiendo P-6 en SH-0 y se queda: hoy la puerta lee UNA columna por consulta con
	// ATextList, asi que si el motor perdiera esta capacidad nada mas en la suite se pondria rojo.
	//
	// CORRE ENTERA CONTRA TIPOS DE PRODUCCION: la app de verdad, el ensamblado internal del dominio,
	// la puerta, el motor pineado. Las cuatro lecturas son las CUATRO string[] que ModelSpecification
	// ya tiene, sobre la global de raiz 'specification'.
	//
	// LAS CUATRO COLUMNAS LLEVAN CONTENIDO DISTINTO A PROPOSITO (C60). Vacias -- que es lo que las
	// cuatro contestan sin revision sellada -- las cuatro serian iguales, y una prueba en la que el
	// motor ligara UNA sola columna y dejara las otras tres en default saldria verde. Con contenido
	// distinto en cada una, cualquier cruce entre columnas cambia la afirmacion.
	//
	// Y AL LADO, EL CAMINO DE UNA COLUMNA POR CONSULTA, que es lo que la puerta hace hoy con
	// ATextList. Las dos respuestas se comparan.
	[TestFixture]
	public sealed class SeveralColumnsComeBackFromOneQuery
	{
		private const string TheVin = "1HGCM82633A004352";
		private const string TheModel = "SM-SEDAN-2025";
		private const string TheRevision = "R1";
		private const int TheOdometer = 20000;

		private static readonly DateTime ThePublication =
			new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheDayItAsks =
			new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

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

			ASheetWithFourDistinctColumns();
		}

		[TearDown]
		public void TearDown()
		{
			if (app != null) app.Dispose();

			AFreshJournalFolder.Sweep(folder);
		}

		[Test]
		public void FourStringArrayOutsBindInOneQueryAndNoneOfThemGetsAnotherColumnsContent()
		{
			string[] permitted;
			string[] prohibited;
			string[] systems;
			string[] exclusions;

			using (RentedParameter rented = app.Actor.RentedParameters())
			{
				app.Actor.Using(@"
					@permitted  = specification.PermittedFuelGrades();
					@prohibited = specification.ProhibitedFuelGrades();
					@systems    = specification.CoveredSystems();
					@exclusions = specification.CoverageExclusions();
				")
				.WithParameters(rented, p => {
					p[Parameter.Out, "permitted",  typeof(string[])] = default;
					p[Parameter.Out, "prohibited", typeof(string[])] = default;
					p[Parameter.Out, "systems",    typeof(string[])] = default;
					p[Parameter.Out, "exclusions", typeof(string[])] = default;
				})
				.PerformQuery();

				permitted  = rented["permitted"].GetValue<string[]>();
				prohibited = rented["prohibited"].GetValue<string[]>();
				systems    = rented["systems"].GetValue<string[]>();
				exclusions = rented["exclusions"].GetValue<string[]>();
			}

			Assert.That(permitted, Is.EqualTo(new[] { "95 RON" }),
				"Cada columna trae LO SUYO. Se afirma el contenido y no que no venga vacia: "
				+ "Is.Not.Empty pasaria igual si las cuatro trajeran la misma lista.");
			Assert.That(prohibited, Is.EqualTo(new[] { "E85" }));
			Assert.That(systems, Is.EqualTo(new[] { "POWERTRAIN", "SUSPENSION" }),
				"Y esta trae DOS elementos donde las otras traen uno, asi que un cruce entre "
				+ "columnas cambia hasta el largo.");

			Assert.That(exclusions.Length, Is.EqualTo(1));
			Assert.That(exclusions[0], Does.Contain("SUSPENSION"));
			Assert.That(exclusions[0], Does.Contain("uso fuera de camino"));
		}

		// Y LAS MISMAS CUATRO, DE A UNA, QUE ES EL PLAN B. Se comparan contra la respuesta de la
		// puerta, que es la que un llamador de verdad recibe hoy.
		[Test]
		public void TheOneQueryPerColumnFallbackAnswersExactlyTheSame()
		{
			Assert.That(api.PermittedFuelGrades(), Is.EqualTo(new[] { "95 RON" }));
			Assert.That(api.ProhibitedFuelGrades(), Is.EqualTo(new[] { "E85" }));
			Assert.That(api.CoveredSystems(), Is.EqualTo(new[] { "POWERTRAIN", "SUSPENSION" }));
			Assert.That(api.CoverageExclusions().Length, Is.EqualTo(1));
		}

		// LA FILA MIXTA, que es la forma de verdad de un DTO de registro: columnas string[] al lado
		// de un numero, una frase y una bandera, EN UN SOLO VIAJE. Si esto no ligara,
		// StayCountBetween tendria que salir en su propia consulta y ShopStayRecord seria dos.
		//
		// Y CRUZA DOS GLOBALES DE RAIZ en el mismo script -- 'specification' y 'schedule' -- asi que
		// de paso vuelve a derivar G1 de SPIKES-WarrantyEnvelope contra el motor pineado de hoy, que
		// es lo que C69 pide antes de apoyarse en el.
		//
		// EL NUMERO ES EL ODOMETRO Y NO UN CONTEO, a proposito: CountDue() y MissedCount() valen 0
		// en una agenda recien nacida, y un slot que el motor dejara sin ligar tambien vale 0 (C60,
		// y es el defecto exacto que SPIKES-WarrantyEnvelope tuvo que corregir). 20000 no.
		[Test]
		public void AMixedRowBindsToo_ArraysBesideAnIntBesideAStringAcrossTwoGlobals()
		{
			string[] systems;
			int odometer;
			string revision;
			bool sealedRevision;

			using (RentedParameter rented = app.Actor.RentedParameters())
			{
				app.Actor.Using(@"
					@systems        = specification.CoveredSystems();
					@exclusions     = specification.CoverageExclusions();
					@revision       = specification.ReadCurrentRevisionCode();
					@sealedRevision = specification.HasASealedRevision();
					@odometer       = schedule.ReadOdometer(@asOf);
				")
				.WithParameters(rented, p => {
					p["asOf", typeof(DateTime)] = TheDayItAsks;
					p[Parameter.Out, "systems",        typeof(string[])] = default;
					p[Parameter.Out, "exclusions",     typeof(string[])] = default;
					p[Parameter.Out, "revision",       typeof(string)]   = default;
					p[Parameter.Out, "sealedRevision", typeof(bool)]     = default;
					p[Parameter.Out, "odometer",       typeof(int)]      = default;
				})
				.PerformQuery();

				systems        = rented["systems"].GetValue<string[]>();
				odometer       = rented["odometer"].GetValue<int>();
				revision       = rented["revision"].GetValue<string>();
				sealedRevision = rented["sealedRevision"].GetValue<bool>();
			}

			Assert.That(systems, Is.EqualTo(new[] { "POWERTRAIN", "SUSPENSION" }));
			Assert.That(odometer, Is.EqualTo(TheOdometer),
				"El numero, de OTRA global de raiz, en el MISMO viaje que las filas.");
			Assert.That(revision, Is.EqualTo(TheRevision));
			Assert.That(sealedRevision, Is.True);
		}

		// LA FICHA CON CUATRO COLUMNAS DISTINTAS. Cada una con contenido propio, para que ninguna
		// afirmacion de arriba pueda pasar leyendo la columna equivocada.
		private void ASheetWithFourDistinctColumns()
		{
			api.OpenRevision(TheRevision, ThePublication);

			api.PublishFuelGrade(TheRevision, "95 RON", "Permitted");
			api.PublishFuelGrade(TheRevision, "E85", "Prohibited");

			api.PublishCoveredSystem(TheRevision, "POWERTRAIN", 36, 60000, "UnitA");
			api.PublishCoveredSystem(TheRevision, "SUSPENSION", 0, 0, "UnitA");

			api.PublishCoverageExclusion(
				TheRevision, "SUSPENSION", "Amortiguadores", "uso fuera de camino");

			api.SealRevision(TheRevision);

			// Y una lectura del odometro, para que la fila mixta tenga un numero que NO sea cero.
			api.RecordReading(TheOdometer, "UnitA", ThePublication, "HumanEntry");
		}
	}
}
