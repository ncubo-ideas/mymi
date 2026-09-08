using System;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LAS PRUEBAS 11 y 12 de §9 EN EL NIVEL B (ley L5, OINV-25, OEX-10).
	//
	// Son DOS LLAMADAS y no una, que es la resolucion de C5: AdoptPlanRevision ABRE la revision y
	// la operacion entra despues, por SeedPlanOperation, que es el unico camino por el que una
	// operacion entra a la agenda.
	//
	// LO QUE SE PUEDE OBSERVAR Y LO QUE NO. §8 prohibe que cruce cualquier coleccion, asi que la
	// instancia sustituida no es alcanzable desde la puerta y la cadena PrecededBy vive adentro.
	// Que la vieja quede Superseded lo prueba el nivel A sobre la ocurrencia; aca se prueba la
	// CONSECUENCIA que si se ve, y se prueba que sobrevive al journal: la clase tiene una sola
	// operacion activa, medida desde SU ancla, y CountDue no la cuenta dos veces.
	[TestFixture]
	public sealed class AJournaledRevisionSupersedesInsteadOfDuplicating
	{
		private const string TheVin = "1HGCM82633A004352";
		private const int TwelveMonthsInDays = 365;
		private const int TenThousandUnits = 10000;

		private static readonly DateTime Delivery =
			new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);
		private const int OdometerAtDelivery = 30000;

		private static readonly DateTime TheLimitIsReached =
			new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc);
		private static readonly DateTime TheRevisionIsAdopted =
			new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
		private const int OdometerAtTheRevision = 41000;

		private AScheduleOnARealJournal journal;

		[SetUp]
		public void Setup()
		{
			journal = new AScheduleOnARealJournal(TheVin, "Standard");
			SeedAFluidChangeOf(TenThousandUnits, Delivery, OdometerAtDelivery);
		}

		[TearDown]
		public void TearDown()
		{
			journal.Dispose();
		}

		// PRUEBA 11 de §9: la revision reemplaza una operacion de la misma clase; la nueva queda
		// NotDue y CountDue no cuenta dos veces.
		[Test]
		public void ARevisionsReplacementTakesTheKindsSingleActiveSlot()
		{
			ReachTheLimit();

			Assert.That(journal.Api.CountDue(), Is.EqualTo(1));
			Assert.That(journal.Api.ReadDueState("FluidChange"), Is.EqualTo("Due"));

			AdoptTheRevisionAndSeed(8000);

			Assert.That(journal.Api.ReadDueState("FluidChange"), Is.EqualTo("NotDue"),
				"El reemplazo es una ocurrencia NUEVA: nace NotDue.");
			Assert.That(journal.Api.CountDue(), Is.EqualTo(0),
				"Y la sustituida ya no cuenta. Si la revision DUPLICARA, esto seria 1 y la persona "
				+ "veria dos cambios de fluido donde hay uno.");

			journal.Api.Evaluate(TheRevisionIsAdopted.AddDays(1));

			Assert.That(journal.Api.CountDue(), Is.EqualTo(0));
			Assert.That(journal.Api.MissedCount(), Is.EqualTo(0));
		}

		// PRUEBA 12 de §9 / OEX-10: la sustituida estaba APLAZADA; el reemplazo empieza NotDue y
		// con la cuenta de aplazamientos en cero.
		[Test]
		public void AReplacementOfADeferredOperationStartsWithItsOwnEmptyCount()
		{
			ReachTheLimit();
			journal.Api.DeferOperation("FluidChange", "Cost", TheLimitIsReached);

			Assert.That(journal.Api.ReadDueState("FluidChange"), Is.EqualTo("Deferred"));
			Assert.That(journal.Api.DeferralsUsed("FluidChange"), Is.EqualTo(1));

			AdoptTheRevisionAndSeed(8000);

			Assert.That(journal.Api.ReadDueState("FluidChange"), Is.EqualTo("NotDue"));
			Assert.That(journal.Api.DeferralsUsed("FluidChange"), Is.EqualTo(0),
				"Y no porque alguien la haya reseteado: porque es otra ocurrencia, con su propia "
				+ "lista, que nace vacia.");
		}

		// El reemplazo se mide desde SU ancla, y eso es lo que obliga a que el ancla viaje con la
		// siembra y no con la creacion (C18). Aca se prueba ademas que el ancla sobreviva al
		// journal: son tres parametros que cruzan juntos y cualquiera puede perderse.
		[Test]
		public void TheReplacementIsMeasuredFromItsOwnAnchorEvenAfterTheJournalIsReplayed()
		{
			ReachTheLimit();
			AdoptTheRevisionAndSeed(TenThousandUnits);

			journal.CloseAndReopenFromTheJournal();

			journal.Api.RecordReading(
				51000, "UnitA", TheRevisionIsAdopted.AddDays(30), "Telemetry");
			journal.Api.Evaluate(TheRevisionIsAdopted.AddDays(30));

			Assert.That(journal.Api.ReadDueState("FluidChange"), Is.EqualTo("Due"),
				"41000 del ancla mas 10000 son 51000. Con el ancla vieja -- 30000 -- ya habria "
				+ "vencido en 40000 y naceria vencido.");
		}

		// L5 no depende de que haya una revision abierta: la ley es sobre la AGENDA, no sobre el
		// tramite.
		[Test]
		public void ReSeedingAKindSupersedesEvenWithNoRevisionOpen()
		{
			ReachTheLimit();

			journal.Api.RecordReading(
				OdometerAtTheRevision, "UnitA", TheRevisionIsAdopted, "Inspection");
			SeedAFluidChangeOf(8000, TheRevisionIsAdopted, OdometerAtTheRevision);

			Assert.That(journal.Api.ReadDueState("FluidChange"), Is.EqualTo("NotDue"));
			Assert.That(journal.Api.CountDue(), Is.EqualTo(0));
		}

		// El codigo de la revision es la procedencia del plan que la persona esta corriendo, asi
		// que una revision sin codigo se rechaza EN LA PUERTA -- antes de que el motor escriba --
		// y no adentro (OINV-19).
		[Test]
		public void APlanRevisionWithoutACodeIsRefusedAtTheDoor()
		{
			Assert.Throws<ArgumentNullException>(
				() => journal.Api.AdoptPlanRevision(null, TheRevisionIsAdopted));
			Assert.Throws<ArgumentException>(
				() => journal.Api.AdoptPlanRevision("  ", TheRevisionIsAdopted));
		}

		// EL CRUCE DE ORIGENES (C20): la cadena continua solo entre operaciones del MISMO origen.
		// El plan del fabricante desplaza al recordatorio de la persona y entra como genesis suya,
		// porque heredar el origen cruzado dejaria a la persona quitando una operacion del plan.
		[Test]
		public void ThePlanTakesOverTheKindWithoutBecomingTheOwners()
		{
			journal.Api.AddOwnerOperation(
				"Diagnostic", "TimeOnly", TwelveMonthsInDays, 0, "UnitA",
				Delivery, OdometerAtDelivery, "UnitA");

			journal.Api.SeedPlanOperation(
				"Diagnostic", "TimeOnly", 180, 0, "UnitA",
				true, false,
				TheRevisionIsAdopted, OdometerAtTheRevision, "UnitA");

			journal.Api.RemoveOwnerOperation("Diagnostic");

			Assert.That(
				journal.Api.ReadDueReason("Diagnostic"), Does.Not.Contain("no tiene agendada"),
				"La clase quedo en manos del plan, asi que la ley L9 la protege: la persona ya no "
				+ "la puede quitar.");
		}

		private void SeedAFluidChangeOf(int distanceLimit, DateTime anchoredAt, int anchorReading)
		{
			journal.Api.SeedPlanOperation(
				"FluidChange", "TimeOrDistance", TwelveMonthsInDays, distanceLimit, "UnitA",
				true, false,
				anchoredAt, anchorReading, "UnitA");
		}

		private void AdoptTheRevisionAndSeed(int distanceLimit)
		{
			journal.Api.AdoptPlanRevision("R2", TheRevisionIsAdopted);
			journal.Api.RecordReading(
				OdometerAtTheRevision, "UnitA", TheRevisionIsAdopted, "Inspection");
			SeedAFluidChangeOf(distanceLimit, TheRevisionIsAdopted, OdometerAtTheRevision);
		}

		private void ReachTheLimit()
		{
			journal.Api.RecordReading(40000, "UnitA", TheLimitIsReached, "Telemetry");
			journal.Api.Evaluate(TheLimitIsReached);
		}
	}
}
