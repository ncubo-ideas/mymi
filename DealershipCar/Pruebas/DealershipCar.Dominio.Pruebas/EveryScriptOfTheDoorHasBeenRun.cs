using System;
using DealershipCar.App;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LA PRUEBA QUE EXISTE POR LA MEDICION S5. Un error de tipeo en un script del DSL NO se
	// descubre al compilar: se descubre al ejecutarlo, y deja una entrada permanente que hace al
	// motor loguear un fallo de rehidratacion en CADA arranque futuro de esa agenda, para siempre,
	// aunque el estado siga siendo correcto.
	//
	// Asi que ninguna linea de DSL de DealershipCarAPI puede llegar al journal de una persona sin
	// haberse ejecutado antes aca. Esta prueba corre CADA UNA de las veintiocho operaciones de la
	// puerta al menos una vez -- las veintiuna de T6 mas las siete que trajo T8: los dos comandos de
	// la bitacora, sus cuatro lecturas, y el Apply de la ingesta.
	// No mira reglas de negocio -- de eso se ocupan las otras pruebas del nivel B -- mira que el
	// script exista, parsee, ate sus parametros y devuelva lo que dijo que devolvia.
	[TestFixture]
	public sealed class EveryScriptOfTheDoorHasBeenRun
	{
		private const string TheVin = "1HGCM82633A004352";
		private const int TwelveMonthsInDays = 365;
		private const int TenThousandUnits = 10000;

		private static readonly DateTime Delivery = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);
		private const int OdometerAtDelivery = 30000;

		private AScheduleOnARealJournal journal;

		[SetUp]
		public void Setup()
		{
			journal = new AScheduleOnARealJournal(TheVin, "Standard");
		}

		[TearDown]
		public void TearDown()
		{
			journal.Dispose();
		}

		// La semilla corre en OnHydrated, asi que para cuando la prueba arranca la agenda ya
		// existe. Si el upgrade no hubiera corrido, la primera lectura reventaria con "schedule no
		// existe" en vez de contestar el VIN.
		[Test]
		public void TheSeedRanAndLeftTheScheduleAsTheRootGlobal()
		{
			Assert.That(journal.Api.ReadVin(), Is.EqualTo(TheVin));
		}

		[Test]
		public void EveryCommandOfTheDoorParsesBindsAndRuns()
		{
			journal.Api.SeedPlanOperation(
				"FluidChange", "TimeOrDistance", TwelveMonthsInDays, TenThousandUnits, "UnitA",
				true, false,
				Delivery, OdometerAtDelivery, "UnitA");

			journal.Api.AdoptPlanRevision("R2", new DateTime(2025, 7, 1, 0, 0, 0, DateTimeKind.Utc));

			journal.Api.AddOwnerOperation(
				"Diagnostic", "TimeOnly", TwelveMonthsInDays, 0, "UnitA",
				Delivery, OdometerAtDelivery, "UnitA");

			journal.Api.DeclareUsageProfile(
				"SevereDuty", new DateTime(2025, 8, 1, 0, 0, 0, DateTimeKind.Utc));

			journal.Api.RecordReading(
				40000, "UnitA", new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc), "Telemetry");

			journal.Api.RecordConditionThreshold(
				"Diagnostic", new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc));

			EvaluationOutcome moved = journal.Api.Evaluate(
				new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc));

			Assert.That(moved.WasEvaluated, Is.True);
			Assert.That(moved.TransitionCount, Is.GreaterThan(0));
			Assert.That(moved.AnythingBecameDue, Is.True);

			journal.Api.DeferOperation(
				"FluidChange", "Cost", new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc));

			journal.Api.CompleteOperation(
				"Diagnostic", new DateTime(2026, 1, 21, 0, 0, 0, DateTimeKind.Utc), 40100, "UnitA");

			journal.Api.RemoveOwnerOperation("Diagnostic");

			Assert.That(journal.Api.ReadDueState("FluidChange"), Is.EqualTo("Deferred"));
		}

		[Test]
		public void EveryReadOfTheDoorParsesBindsAndComesBackTyped()
		{
			journal.Api.SeedPlanOperation(
				"FluidChange", "TimeOrDistance", TwelveMonthsInDays, TenThousandUnits, "UnitA",
				true, false,
				Delivery, OdometerAtDelivery, "UnitA");

			journal.Api.RecordReading(
				40000, "UnitA", new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc), "Telemetry");
			journal.Api.Evaluate(new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc));

			Assert.That(journal.Api.ReadVin(), Is.EqualTo(TheVin));
			Assert.That(journal.Api.ReadDueState("FluidChange"), Is.EqualTo("Due"));
			Assert.That(journal.Api.CountDue(), Is.EqualTo(1));
			Assert.That(journal.Api.CountOverdue(), Is.EqualTo(0));
			Assert.That(journal.Api.DeferralsUsed("FluidChange"), Is.EqualTo(0));
			Assert.That(journal.Api.HasAnythingDue(), Is.True);
			Assert.That(journal.Api.ReadNextDueKind(), Is.EqualTo("FluidChange"));
			Assert.That(journal.Api.ReadDueReason("FluidChange"), Does.Contain("Due"));
			Assert.That(journal.Api.MissedCount(), Is.EqualTo(0));
			Assert.That(journal.Api.HasBeenEvaluated(), Is.True);
			Assert.That(
				journal.Api.LastEvaluatedAt(),
				Is.EqualTo(new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc)));
		}

		// Las dos lecturas guardadas de E41, del lado del wire. El DomainException del dominio es
		// internal y ninguna caratula lo puede nombrar, asi que la puerta lo traduce a una
		// excepcion que si -- y la polaridad se conserva: no se inventa una respuesta vacia.
		[Test]
		public void TheTwoGuardedReadsThrowInsteadOfInventingAnEmptyAnswer()
		{
			Assert.That(journal.Api.HasAnythingDue(), Is.False);
			Assert.Throws<InvalidOperationException>(() => journal.Api.ReadNextDueKind());

			Assert.That(journal.Api.HasBeenEvaluated(), Is.False);
			Assert.Throws<InvalidOperationException>(() => journal.Api.LastEvaluatedAt());
		}

		// LOS NUEVE SCRIPTS DE LA BITACORA: dos comandos y siete lecturas. Son siete y no cuatro
		// porque las tres lecturas de credencial son GUARDADAS del lado del dominio -- la puerta
		// pregunta la presencia y despues el valor -- asi que cada una son dos scripts, y hay que
		// correrlas con y sin credencial guardada para tocar los dos caminos.
		[Test]
		public void EveryScriptOfTheEventLogParsesBindsAndRuns()
		{
			Assert.That(journal.Api.LastEventApplied(), Is.EqualTo(0L));
			Assert.That(journal.Api.StoredDeviceKey(), Is.EqualTo(""));
			Assert.That(journal.Api.StoredToken(), Is.EqualTo(""));
			Assert.That(journal.Api.StoredRefreshToken(), Is.EqualTo(""));

			journal.Api.RegisterDevice("device-de-prueba");
			journal.Api.StoreCredential("at_1234", "rt_5678");

			Assert.That(journal.Api.StoredDeviceKey(), Is.EqualTo("device-de-prueba"));
			Assert.That(journal.Api.StoredToken(), Is.EqualTo("at_1234"));
			Assert.That(journal.Api.StoredRefreshToken(), Is.EqualTo("rt_5678"));
		}

		// Y LOS SEIS DE LA INGESTA: los cinco comandos con Check de cada tipo de hecho recibido, mas
		// el comando suelto que avanza el punto de control cuando el dominio rechaza el hecho.
		[Test]
		public void EveryIngestionScriptOfTheDoorParsesBindsAndRuns()
		{
			Assert.That(Apply(ADealershipNarrative.Revision(1, "R1", "2025-09-01T00:00:00Z")),
				Is.EqualTo(""));

			Assert.That(Apply(ADealershipNarrative.PlanOperation(
				2, "FluidChange", "TimeOrDistance", 365, 10000, true, false)), Is.EqualTo(""));

			Assert.That(Apply(ADealershipNarrative.Reading(
				3, 40000, "UnitA", "2026-01-20T00:00:00Z", "Telemetry")), Is.EqualTo(""));

			Assert.That(Apply(ADealershipNarrative.Crossing(
				4, "FluidChange", "2026-01-20T00:00:00Z")), Is.EqualTo(""));

			Assert.That(Apply(ADealershipNarrative.Completion(
				5, "FluidChange", "2026-01-21T00:00:00Z", 40000, "UnitA")), Is.EqualTo(""));

			Assert.That(journal.Api.LastEventApplied(), Is.EqualTo(5L));

			// El sexto script: el dominio rechaza el hecho adentro del cuerpo y el punto de control
			// avanza igual, en un comando aparte.
			Assert.That(Apply(ADealershipNarrative.Reading(
				6, 100, "UnitA", "2026-02-01T00:00:00Z", "Telemetry")), Is.Not.Empty);

			Assert.That(journal.Api.LastEventApplied(), Is.EqualTo(6L));
		}

		// Un mensaje que la puerta no sabe aplicar es un defecto de quien llama: la ruta debio
		// devolver null para descartarlo, y por eso NO se descarta aca en silencio.
		[Test]
		public void AMessageTheDoorHasNoCommandForIsACallerBug()
		{
			Assert.Throws<ArgumentException>(
				() => journal.Api.Apply(new AMessageThisAppDoesNotApply()));
		}

		private string Apply(Puppets.Portador.SenalDeEvento signal)
		{
			return journal.Api.Apply(DealershipCar.App.Events.DealershipEventRoute.Route(signal));
		}

		private sealed class AMessageThisAppDoesNotApply : Puppets.Portador.IMensajeDeEvento
		{
			public long Evento { get { return 1; } }

			public string Tipo { get { return "AlgoQueNadieAplica"; } }
		}

		// La lectura por clave es TOTAL: una clase que nunca se sembro contesta, no revienta.
		[Test]
		public void AKindThatWasNeverScheduledStillGetsAnAnswer()
		{
			Assert.That(journal.Api.ReadDueState("RecallRemedy"), Is.EqualTo("NotDue"));
			Assert.That(journal.Api.DeferralsUsed("RecallRemedy"), Is.EqualTo(0));
			Assert.That(journal.Api.ReadDueReason("RecallRemedy"), Does.Contain("no scheduled"));
		}
	}
}
