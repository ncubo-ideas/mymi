using System;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// EVALUATE ES EL PRIMITIVO QUE EL HOST MANEJA (§7 del slice, E30), y su RESPUESTA es lo que
	// el host actua: cuantas operaciones se movieron, si alguna vencio y si alguna se paso de la
	// gracia. Un Evaluate mudo esconderia justo el hecho por el que se llamo (E9).
	//
	// Aca vive tambien la prueba 15 de §9: una operacion cruza Due -> gracia -> Overdue y
	// MissedCount sube. MissedCount es DERIVADO (E35) de donde estan paradas las operaciones, no
	// un contador que alguien incrementa: por eso BAJA cuando la operacion se hace, que es lo
	// unico que distingue un derivado de un latch.
	//
	// EL INSTANTE SE RECIBE Y NO RETROCEDE. C6 diseño la cadencia de Evaluate como un
	// PerformCheckThenCommand guardado sobre un LastEvaluatedAt monotono, para que re-evaluar un
	// instante ya evaluado no deje entrada en el journal. Esa mitad es de T6; la mitad que le
	// toca al dominio es que "no se re-evalua el pasado" sea una VERDAD DEL DOMINIO y no una
	// convencion de quien llama.
	[TestFixture]
	public sealed class TheScheduleReportsWhatOneEvaluationDid
	{
		private const string TheVin = "1HGCM82633A004352";
		private const int TwelveMonthsInDays = 365;
		private const int TenThousandUnits = 10000;

		private static readonly DateTime Delivery = new DateTime(2025, 6, 1);
		private const int OdometerAtDelivery = 30000;

		private static readonly DateTime TheLimitIsReached = new DateTime(2026, 1, 20);
		private static readonly DateTime TheGraceIsSpent = new DateTime(2026, 2, 1);

		private MaintenanceSchedule schedule;

		[SetUp]
		public void Setup()
		{
			schedule = new MaintenanceSchedule(TheVin, UsageProfileCode.Standard);
			SeedTheFluidChange();
		}

		[Test]
		public void AScheduleNotYetEvaluatedSaysSoInsteadOfInventingAnInstant()
		{
			Assert.That(schedule.HasBeenEvaluated(), Is.False);
			Assert.Throws<DomainException>(() => schedule.LastEvaluatedAt(),
				"E41 y E45: leer una opcional ausente es un defecto de quien llama. Un "
				+ "DateTime.MinValue aca seria un centinela magico -- un instante que parece un "
				+ "instante y no lo es.");

			schedule.Evaluate(TheLimitIsReached);

			Assert.That(schedule.HasBeenEvaluated(), Is.True);
			Assert.That(schedule.LastEvaluatedAt(), Is.EqualTo(TheLimitIsReached));
		}

		[Test]
		public void AnEvaluationThatMovesNothingReportsNothing()
		{
			ReachTheLimit();

			EvaluationResult again = schedule.Evaluate(TheLimitIsReached);

			Assert.That(again.TransitionCount, Is.EqualTo(0));
			Assert.That(again.AnythingBecameDue, Is.False);
			Assert.That(again.AnythingBecameOverdue, Is.False,
				"Re-evaluar el mismo instante no mueve nada, asi que no hay nada que "
				+ "journalizar. Es la mitad del dominio de la cadencia de C6.");
		}

		[Test]
		public void AnEvaluationReportsHowManyOperationsMoved()
		{
			schedule.SeedPlanOperation(
				OperationKindCode.FilterChange, IntervalBasisCode.DistanceOnly,
				0, TenThousandUnits, DistanceUnitCode.UnitA,
				true, false,
				Delivery, OdometerAtDelivery, DistanceUnitCode.UnitA);

			schedule.RecordReading(
				40000, DistanceUnitCode.UnitA, TheLimitIsReached, ReadingSourceCode.Telemetry);

			EvaluationResult result = schedule.Evaluate(TheLimitIsReached);

			Assert.That(result.TransitionCount, Is.EqualTo(2),
				"Las dos operaciones vencieron en la misma evaluacion, y cada una es UNA "
				+ "transicion aunque el camino del diagrama pase por DueSoon (ley L10).");
			Assert.That(result.AnythingBecameDue, Is.True);
			Assert.That(result.AnythingBecameOverdue, Is.False);
			Assert.That(schedule.CountDue(), Is.EqualTo(2));
		}

		// PRUEBA 15 de §9: la operacion pasa Due -> gracia -> Overdue y MissedCount sube.
		[Test]
		public void AnOperationThatSpendsItsGraceBecomesOverdueAndCountsAsMissed()
		{
			ReachTheLimit();

			Assert.That(schedule.MissedCount(), Is.EqualTo(0),
				"Vencida dentro de la gracia todavia no es mantenimiento perdido: Due y Overdue "
				+ "le dicen cosas distintas a la persona (OINV-24).");

			schedule.RecordReading(
				40500, DistanceUnitCode.UnitA, TheGraceIsSpent, ReadingSourceCode.Telemetry);
			EvaluationResult result = schedule.Evaluate(TheGraceIsSpent);

			Assert.That(result.AnythingBecameOverdue, Is.True);
			Assert.That(schedule.ReadDueState(OperationKindCode.FluidChange), Is.EqualTo("Overdue"));
			Assert.That(schedule.CountOverdue(), Is.EqualTo(1));
			Assert.That(schedule.MissedCount(), Is.EqualTo(1));
		}

		// La diferencia entre un DERIVADO y un contador: el derivado baja solo cuando el hecho
		// que lo sostenia deja de ser cierto. Si MissedCount fuera un campo que alguien
		// incrementa, hacer la operacion no lo bajaria y la persona arrastraria para siempre un
		// mantenimiento que ya hizo.
		[Test]
		public void MissedCountFallsWhenTheOperationIsActuallyDone()
		{
			ReachTheLimit();
			schedule.RecordReading(
				40500, DistanceUnitCode.UnitA, TheGraceIsSpent, ReadingSourceCode.Telemetry);
			schedule.Evaluate(TheGraceIsSpent);

			Assert.That(schedule.MissedCount(), Is.EqualTo(1));

			schedule.CompleteOperation(
				OperationKindCode.FluidChange, TheGraceIsSpent, 40500, DistanceUnitCode.UnitA);

			Assert.That(schedule.MissedCount(), Is.EqualTo(0));
			Assert.That(schedule.CountOverdue(), Is.EqualTo(0));
			Assert.That(schedule.ReadDueState(OperationKindCode.FluidChange), Is.EqualTo("NotDue"),
				"Y la clase queda ocupada por la SUCESORA, no vacia: hacer una operacion no la "
				+ "saca de la agenda, la vuelve a empezar (ley L4).");
		}

		// CountDue cuenta lo VENCIDO O PASADO y CountOverdue solo lo pasado, con el mismo
		// criterio con que DueStateTransition dice BecameDue: Overdue cuenta, porque es vencida y
		// ademas pasada. Dos sentidos distintos de "vencida" adentro de un mismo agregado seria
		// el off-by-one que nadie encuentra.
		[Test]
		public void CountDueCountsWhatIsDueOrPastAndCountOverdueOnlyWhatIsPast()
		{
			schedule.SeedPlanOperation(
				OperationKindCode.FilterChange, IntervalBasisCode.DistanceOnly,
				0, 9000, DistanceUnitCode.UnitA,
				true, false,
				Delivery, OdometerAtDelivery, DistanceUnitCode.UnitA);

			ReachTheLimit();

			Assert.That(
				schedule.ReadDueState(OperationKindCode.FluidChange), Is.EqualTo("Due"));
			Assert.That(
				schedule.ReadDueState(OperationKindCode.FilterChange), Is.EqualTo("Overdue"),
				"10000 recorridas contra un limite de 9000 son 1000 pasadas, y la gracia son "
				+ "500.");

			Assert.That(schedule.CountDue(), Is.EqualTo(2));
			Assert.That(schedule.CountOverdue(), Is.EqualTo(1));
			Assert.That(schedule.MissedCount(), Is.EqualTo(1));
		}

		// Una aplazada NO esta parada en Due: la persona decidio. Pero sigue contando como
		// mantenimiento perdido si la ley dice que se paso de la gracia (OINV-28), y esa
		// asimetria es a proposito: CountDue dice DONDE ESTAN las operaciones y MissedCount dice
		// QUE NO SE HIZO.
		[Test]
		public void ADeferredOperationLeavesTheDueCountButNotTheMissedCount()
		{
			ReachTheLimit();
			schedule.DeferOperation(
				OperationKindCode.FluidChange, DeferralReasonCode.Cost, TheLimitIsReached);

			Assert.That(schedule.CountDue(), Is.EqualTo(0));
			Assert.That(schedule.HasAnythingDue(), Is.False);

			schedule.RecordReading(
				40500, DistanceUnitCode.UnitA, TheGraceIsSpent, ReadingSourceCode.Telemetry);
			schedule.Evaluate(TheGraceIsSpent);

			Assert.That(
				schedule.ReadDueState(OperationKindCode.FluidChange), Is.EqualTo("Deferred"),
				"La ventana de 60 dias no se cumplio.");
			Assert.That(schedule.CountOverdue(), Is.EqualTo(0));
			Assert.That(schedule.MissedCount(), Is.EqualTo(1),
				"OINV-28: aplazar es la decision de la persona y no la saca de la cuenta.");
		}

		[Test]
		public void TheScheduleRefusesToReEvaluateThePast()
		{
			schedule.Evaluate(TheGraceIsSpent);

			Assert.Throws<DomainException>(() => schedule.Evaluate(TheLimitIsReached),
				"C6: 'no se re-evalua el pasado' es una verdad del dominio y no una convencion "
				+ "de quien llama. Un estado calculado con hechos posteriores no se puede "
				+ "des-calcular volviendo el reloj.");

			Assert.That(schedule.LastEvaluatedAt(), Is.EqualTo(TheGraceIsSpent),
				"Y el rechazo no movio el punto de control.");
		}

		[Test]
		public void EvaluatingAnEmptyScheduleReportsNothingAndDoesNotThrow()
		{
			MaintenanceSchedule empty = new MaintenanceSchedule(TheVin, UsageProfileCode.Standard);

			EvaluationResult result = empty.Evaluate(TheLimitIsReached);

			Assert.That(result.TransitionCount, Is.EqualTo(0));
			Assert.That(empty.CountDue(), Is.EqualTo(0));
			Assert.That(empty.MissedCount(), Is.EqualTo(0));
		}

		// El instante SE RECIBE (§7): avanzar seis meses es un parametro, no una espera. Es lo
		// que le paga la pureza del reloj a la demo de T7.
		[Test]
		public void AdvancingTheClockIsAParameterAndNotAWait()
		{
			schedule.Evaluate(Delivery);

			Assert.That(schedule.ReadDueState(OperationKindCode.FluidChange), Is.EqualTo("NotDue"));

			schedule.Evaluate(Delivery.AddDays(TwelveMonthsInDays));

			Assert.That(schedule.ReadDueState(OperationKindCode.FluidChange), Is.EqualTo("Due"));
		}

		[Test]
		public void CompletingAKindThatIsNotScheduledIsACallerBug()
		{
			Assert.Throws<DomainException>(() => schedule.CompleteOperation(
				OperationKindCode.RecallRemedy, TheLimitIsReached, 40000, DistanceUnitCode.UnitA),
				"Completar lo que no esta agendado no produce ninguna sucesora, asi que el "
				+ "hecho mas consecuente del dominio -- el que re-ancla todo el futuro -- se "
				+ "perderia en silencio. E32: esto no puede pasar.");
		}

		[Test]
		public void DeferringAKindThatIsNotScheduledDoesNothing()
		{
			Assert.DoesNotThrow(() => schedule.DeferOperation(
				OperationKindCode.RecallRemedy, DeferralReasonCode.Cost, TheLimitIsReached));
		}

		[Test]
		public void AConditionCrossingForAKindThatIsNotScheduledDoesNothing()
		{
			Assert.DoesNotThrow(() => schedule.RecordConditionThreshold(
				OperationKindCode.RecallRemedy, TheLimitIsReached));
		}

		// La ley L3 llega hasta la fachada: un cruce recibido adelanta la operacion de esa clase.
		[Test]
		public void AReceivedConditionCrossingBringsItsOwnKindForward()
		{
			schedule.SeedPlanOperation(
				OperationKindCode.Inspection, IntervalBasisCode.ConditionOnly,
				0, 0, DistanceUnitCode.UnitA,
				true, false,
				Delivery, OdometerAtDelivery, DistanceUnitCode.UnitA);

			schedule.Evaluate(TheLimitIsReached);

			Assert.That(schedule.ReadDueState(OperationKindCode.Inspection), Is.EqualTo("NotDue"));

			schedule.RecordConditionThreshold(OperationKindCode.Inspection, TheLimitIsReached);

			Assert.That(schedule.ReadDueState(OperationKindCode.Inspection), Is.EqualTo("Due"));
			Assert.That(schedule.HasAnythingDue(), Is.True);
		}

		[Test]
		public void AnUnknownKindIsACallerBugOnEveryReceivedFact()
		{
			Assert.Throws<DomainException>(() => schedule.RecordConditionThreshold(
				(OperationKindCode)99, TheLimitIsReached));

			Assert.Throws<DomainException>(() => schedule.DeferOperation(
				(OperationKindCode)99, DeferralReasonCode.Cost, TheLimitIsReached));

			Assert.Throws<DomainException>(() => schedule.CompleteOperation(
				(OperationKindCode)99, TheLimitIsReached, 40000, DistanceUnitCode.UnitA));
		}

		[Test]
		public void AnUnknownDeferralReasonIsACallerBug()
		{
			ReachTheLimit();

			Assert.Throws<DomainException>(() => schedule.DeferOperation(
				OperationKindCode.FluidChange, (DeferralReasonCode)99, TheLimitIsReached));
		}

		private void SeedTheFluidChange()
		{
			schedule.SeedPlanOperation(
				OperationKindCode.FluidChange, IntervalBasisCode.TimeOrDistance,
				TwelveMonthsInDays, TenThousandUnits, DistanceUnitCode.UnitA,
				true, false,
				Delivery, OdometerAtDelivery, DistanceUnitCode.UnitA);
		}

		private void ReachTheLimit()
		{
			schedule.RecordReading(
				40000, DistanceUnitCode.UnitA, TheLimitIsReached, ReadingSourceCode.Telemetry);
			schedule.Evaluate(TheLimitIsReached);
		}
	}
}
