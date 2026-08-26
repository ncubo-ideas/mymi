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
	// haberse ejecutado antes aca. Esta prueba corre CADA UNA de las veintiuna operaciones de la
	// puerta al menos una vez. No mira reglas de negocio -- de eso se ocupan las otras quince
	// pruebas del nivel B -- mira que el script exista, parsee, ate sus parametros y devuelva lo
	// que dijo que devolvia.
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
