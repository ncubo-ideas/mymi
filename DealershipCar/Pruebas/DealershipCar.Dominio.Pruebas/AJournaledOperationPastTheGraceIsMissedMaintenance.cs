using System;
using DealershipCar.App;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// PRUEBA 15 de §9 EN EL NIVEL B: "una operacion pasa Due -> OverdueGrace -> Overdue;
	// MissedCount sube". Y con ella el primitivo que el host maneja (E30): lo que Evaluate DEVUELVE
	// es lo que la caratula actua, asi que tiene que llegar entero por la puerta.
	//
	// LO QUE ESTE NIVEL AGREGA: los tres campos de la respuesta vuelven por Out TIPADOS, no
	// parseados de un print. Un conteo que se pierda en la puerta deja a la app sin saber que aviso
	// dar -- y no hay ninguna lectura que lo delate despues, porque el conteo es de ESA evaluacion
	// y no del estado.
	//
	// MissedCount es DERIVADO (E35), y la diferencia con un contador se ve en una sola prueba: la
	// operacion se completa y deja de contar SOLA.
	[TestFixture]
	public sealed class AJournaledOperationPastTheGraceIsMissedMaintenance
	{
		private const string TheVin = "1HGCM82633A004352";
		private const int TwelveMonthsInDays = 365;
		private const int TenThousandUnits = 10000;

		private static readonly DateTime Delivery =
			new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);
		private const int OdometerAtDelivery = 30000;

		private static readonly DateTime TheLimitIsReached =
			new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc);
		private static readonly DateTime TheGraceIsSpent =
			new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);

		private AScheduleOnARealJournal journal;

		[SetUp]
		public void Setup()
		{
			journal = new AScheduleOnARealJournal(TheVin, "Standard");
			SeedTheFluidChange();
		}

		[TearDown]
		public void TearDown()
		{
			journal.Dispose();
		}

		// PRUEBA 15 de §9: la operacion cruza la gracia y MissedCount sube.
		[Test]
		public void AnOperationThatSpendsItsGraceBecomesOverdueAndCountsAsMissed()
		{
			ReachTheLimit();

			Assert.That(journal.Api.MissedCount(), Is.EqualTo(0),
				"Vencida dentro de la gracia todavia no es mantenimiento perdido: Due y Overdue le "
				+ "dicen cosas distintas a la persona (OINV-24).");

			journal.Api.RecordReading(40500, "UnitA", TheGraceIsSpent, "Telemetry");
			EvaluationOutcome spent = journal.Api.Evaluate(TheGraceIsSpent);

			Assert.That(spent.AnythingBecameOverdue, Is.True);
			Assert.That(journal.Api.ReadDueState("FluidChange"), Is.EqualTo("Overdue"));
			Assert.That(journal.Api.CountOverdue(), Is.EqualTo(1));
			Assert.That(journal.Api.MissedCount(), Is.EqualTo(1));
		}

		// La diferencia entre un DERIVADO y un contador: el derivado baja solo cuando el hecho que
		// lo sostenia deja de ser cierto -- incluso despues de reproducir el journal, donde un
		// contador guardado si habria sobrevivido.
		[Test]
		public void MissedCountFallsWhenTheOperationIsActuallyDone()
		{
			ReachTheLimit();
			journal.Api.RecordReading(40500, "UnitA", TheGraceIsSpent, "Telemetry");
			journal.Api.Evaluate(TheGraceIsSpent);

			Assert.That(journal.Api.MissedCount(), Is.EqualTo(1));

			journal.Api.CompleteOperation("FluidChange", TheGraceIsSpent, 40500, "UnitA");

			Assert.That(journal.Api.MissedCount(), Is.EqualTo(0));
			Assert.That(journal.Api.CountOverdue(), Is.EqualTo(0));
			Assert.That(journal.Api.ReadDueState("FluidChange"), Is.EqualTo("NotDue"),
				"Y la clase queda ocupada por la SUCESORA, no vacia (ley L4).");

			journal.CloseAndReopenFromTheJournal();

			Assert.That(journal.Api.MissedCount(), Is.EqualTo(0),
				"Reproducido el journal sigue en cero: lo derivado se recalcula, un contador "
				+ "journalizado habria vuelto con su ultimo valor.");
		}

		// EVALUATE ES EL PRIMITIVO QUE EL HOST MANEJA (E30) y su respuesta es lo que el host actua.
		[Test]
		public void AnEvaluationReportsHowManyOperationsMoved()
		{
			journal.Api.SeedPlanOperation(
				"FilterChange", "DistanceOnly", 0, TenThousandUnits, "UnitA",
				true, false,
				Delivery, OdometerAtDelivery, "UnitA");

			journal.Api.RecordReading(40000, "UnitA", TheLimitIsReached, "Telemetry");

			EvaluationOutcome moved = journal.Api.Evaluate(TheLimitIsReached);

			Assert.That(moved.WasEvaluated, Is.True);
			Assert.That(moved.TransitionCount, Is.EqualTo(2),
				"Las dos operaciones vencieron en la misma evaluacion, y cada una es UNA transicion "
				+ "aunque el camino del diagrama pase por DueSoon (ley L10).");
			Assert.That(moved.AnythingBecameDue, Is.True);
			Assert.That(moved.AnythingBecameOverdue, Is.False);
			Assert.That(journal.Api.CountDue(), Is.EqualTo(2));
		}

		// CountDue cuenta lo VENCIDO O PASADO y CountOverdue solo lo pasado. Dos sentidos distintos
		// de "vencida" en un mismo agregado seria el off-by-one que nadie encuentra.
		[Test]
		public void CountDueCountsWhatIsDueOrPastAndCountOverdueOnlyWhatIsPast()
		{
			journal.Api.SeedPlanOperation(
				"FilterChange", "DistanceOnly", 0, 9000, "UnitA",
				true, false,
				Delivery, OdometerAtDelivery, "UnitA");

			ReachTheLimit();

			Assert.That(journal.Api.ReadDueState("FluidChange"), Is.EqualTo("Due"));
			Assert.That(journal.Api.ReadDueState("FilterChange"), Is.EqualTo("Overdue"),
				"10000 recorridas contra un limite de 9000 son 1000 pasadas, y la gracia son 500.");

			Assert.That(journal.Api.CountDue(), Is.EqualTo(2));
			Assert.That(journal.Api.CountOverdue(), Is.EqualTo(1));
			Assert.That(journal.Api.MissedCount(), Is.EqualTo(1));
			Assert.That(journal.Api.ReadNextDueKind(), Is.EqualTo("FilterChange"),
				"Lo primero es lo mas pasado.");
		}

		// Una aplazada NO esta parada en Due -- la persona decidio -- pero sigue contando como
		// mantenimiento perdido si la ley dice que se paso de la gracia (OINV-28).
		[Test]
		public void ADeferredOperationLeavesTheDueCountButNotTheMissedCount()
		{
			ReachTheLimit();
			journal.Api.DeferOperation("FluidChange", "Cost", TheLimitIsReached);

			Assert.That(journal.Api.CountDue(), Is.EqualTo(0));
			Assert.That(journal.Api.HasAnythingDue(), Is.False);

			journal.Api.RecordReading(40500, "UnitA", TheGraceIsSpent, "Telemetry");
			journal.Api.Evaluate(TheGraceIsSpent);

			Assert.That(journal.Api.ReadDueState("FluidChange"), Is.EqualTo("Deferred"),
				"La ventana de 60 dias no se cumplio.");
			Assert.That(journal.Api.CountOverdue(), Is.EqualTo(0));
			Assert.That(journal.Api.MissedCount(), Is.EqualTo(1),
				"OINV-28: aplazar es la decision de la persona y no la saca de la cuenta.");
		}

		// La ley L3 llega hasta la puerta: un cruce recibido adelanta la operacion de esa clase, y
		// sigue adelantada despues de reproducir el journal.
		[Test]
		public void AReceivedConditionCrossingBringsItsOwnKindForward()
		{
			journal.Api.SeedPlanOperation(
				"Inspection", "ConditionOnly", 0, 0, "UnitA",
				true, false,
				Delivery, OdometerAtDelivery, "UnitA");

			journal.Api.Evaluate(TheLimitIsReached);

			Assert.That(journal.Api.ReadDueState("Inspection"), Is.EqualTo("NotDue"));

			journal.Api.RecordConditionThreshold("Inspection", TheLimitIsReached);

			Assert.That(journal.Api.ReadDueState("Inspection"), Is.EqualTo("Due"));

			journal.CloseAndReopenFromTheJournal();

			Assert.That(journal.Api.ReadDueState("Inspection"), Is.EqualTo("Due"),
				"El cruce quedo journalizado: no es un estado que alguien puso, es un hecho "
				+ "recibido que se vuelve a recibir al reproducir.");
		}

		// El instante SE RECIBE (§7): avanzar seis meses es un parametro, no una espera. Es lo que
		// le paga la pureza del reloj a la demo de T7.
		[Test]
		public void AdvancingTheClockIsAParameterAndNotAWait()
		{
			journal.Api.Evaluate(Delivery);

			Assert.That(journal.Api.ReadDueState("FluidChange"), Is.EqualTo("NotDue"));

			journal.Api.Evaluate(Delivery.AddDays(TwelveMonthsInDays));

			Assert.That(journal.Api.ReadDueState("FluidChange"), Is.EqualTo("Due"));
		}

		private void SeedTheFluidChange()
		{
			journal.Api.SeedPlanOperation(
				"FluidChange", "TimeOrDistance", TwelveMonthsInDays, TenThousandUnits, "UnitA",
				true, false,
				Delivery, OdometerAtDelivery, "UnitA");
		}

		private void ReachTheLimit()
		{
			journal.Api.RecordReading(40000, "UnitA", TheLimitIsReached, "Telemetry");
			journal.Api.Evaluate(TheLimitIsReached);
		}
	}
}
