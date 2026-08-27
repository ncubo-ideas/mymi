using System;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LEY L3, ConditionOnlyBringsForward, sobre ScheduledOperation (§5 del slice, OINV-22,
	// OHS-08): "Un cruce de umbral recibido puede ADELANTAR una operacion a Due. Nunca puede
	// empujar un vencimiento hacia afuera."
	//
	// EL NOMBRE DE LA LEY SE LEE COMO ADVERBIO, no como el subtipo de regla: un cruce de
	// condicion SOLO ADELANTA. Por eso la ley vive en la ocurrencia y no en ConditionOnly --
	// un cruce llega para CUALQUIER operacion, no solo para las que no tienen limites
	// (OPOL-15, y la tabla de transiciones NotDue/DueSoon -> Due).
	//
	// LO QUE DE VERDAD SE PRUEBA ACA es la MITAD NEGATIVA, que es la que nadie escribe. Que un
	// cruce adelante es facil y sale solo. Que un cruce NO retrase es lo que OHS-08 existe
	// para decidir: el intervalo es el piso, y un sensor que dice "esto todavia esta bien" no
	// tiene autoridad para desvencer nada. Una implementacion que ponga Due a secas al recibir
	// el cruce pasa la mitad facil y baja de Overdue a Due sin que nadie lo note.
	//
	// Y LA PERSISTENCIA: el cruce se RECUERDA. Si solo moviera el estado en el momento, la
	// siguiente evaluacion -- que recalcula desde la ley, como manda el GAP G4 -- lo pisaria y
	// la operacion volveria a NotDue. El adelanto habria durado hasta la proxima vez que la
	// app se abre.
	[TestFixture]
	public sealed class AConditionCrossingOnlyBringsForward
	{
		private const int TwelveMonthsInDays = 365;
		private const int TenThousandUnits = 10000;

		private static readonly DateTime LastCompletion = new DateTime(2025, 6, 1);
		private const int ReadingAtTheLastCompletion = 30000;

		private IntervalLengthFactor noScaling;

		[SetUp]
		public void Setup()
		{
			noScaling = IntervalLengthFactor.For(UsageProfile.Standard);
		}

		// Una operacion sin limites no vence sola: espera el cruce y nada mas.
		[Test]
		public void AConditionOnlyOperationIsNotDueUntilACrossingIsReceived()
		{
			ScheduledOperation operation = AConditionOnlyOperation();

			operation.Evaluate(
				new DateTime(2030, 1, 1), AReadingOf(90000, DistanceUnit.UnitA), noScaling);

			Assert.That(operation.State, Is.SameAs(DueState.NotDue),
				"Cinco anos y sesenta mil unidades despues sigue sin vencer, porque esta ley no "
				+ "mide nada. Si vence, alguien le puso un limite por defecto.");
		}

		[Test]
		public void TheReceivedCrossingBringsTheOperationForwardToDue()
		{
			ScheduledOperation operation = AConditionOnlyOperation();

			operation.RecordConditionThresholdCrossing(new DateTime(2025, 9, 1));

			Assert.That(operation.State, Is.SameAs(DueState.Due));
		}

		// LA PERSISTENCIA. El cruce sobrevive a la siguiente evaluacion, que es la que
		// recalcula todo desde la ley.
		[Test]
		public void ALaterEvaluationDoesNotUndoTheCrossing()
		{
			ScheduledOperation operation = AConditionOnlyOperation();

			operation.RecordConditionThresholdCrossing(new DateTime(2025, 9, 1));
			operation.Evaluate(
				new DateTime(2025, 10, 1), AReadingOf(33000, DistanceUnit.UnitA), noScaling);

			Assert.That(operation.State, Is.SameAs(DueState.Due),
				"La ley por si sola contesta NotDue -- no mide nada -- asi que si la evaluacion "
				+ "pisa el cruce, el adelanto duro hasta que la app se abrio de nuevo.");
		}

		// El cruce llega para cualquier operacion, no solo para las que no tienen limites.
		[Test]
		public void ACrossingAlsoBringsForwardAnOperationThatHasLimits()
		{
			ScheduledOperation operation = ATimeOrDistanceOperation();

			operation.Evaluate(
				new DateTime(2025, 12, 1), AReadingOf(31000, DistanceUnit.UnitA), noScaling);

			Assert.That(operation.State, Is.SameAs(DueState.NotDue));

			operation.RecordConditionThresholdCrossing(new DateTime(2025, 12, 1));

			Assert.That(operation.State, Is.SameAs(DueState.Due),
				"OPOL-15: un cruce ADELANTA una operacion existente. Que tenga limites propios "
				+ "no la deja afuera.");
		}

		// LA MITAD NEGATIVA, y la que decide OHS-08. Un cruce sobre una operacion que ya se
		// paso de la gracia NO la baja a Due: el intervalo es el piso.
		[Test]
		public void ACrossingNeverPushesAnOverdueOperationBack()
		{
			ScheduledOperation operation = ATimeOrDistanceOperation();

			operation.Evaluate(
				new DateTime(2026, 1, 20), AReadingOf(40500, DistanceUnit.UnitA), noScaling);

			Assert.That(operation.State, Is.SameAs(DueState.Overdue));

			operation.RecordConditionThresholdCrossing(new DateTime(2026, 1, 20));

			Assert.That(operation.State, Is.SameAs(DueState.Overdue),
				"OHS-08: el intervalo es el PISO. Un cruce puede adelantar; bajar de Overdue a "
				+ "Due seria empujar el vencimiento hacia afuera, que es exactamente lo que la "
				+ "ley prohibe.");
		}

		[Test]
		public void AndALaterEvaluationOfACrossedOperationStillReportsTheWorseState()
		{
			ScheduledOperation operation = ATimeOrDistanceOperation();

			operation.RecordConditionThresholdCrossing(new DateTime(2025, 12, 1));
			operation.Evaluate(
				new DateTime(2026, 1, 20), AReadingOf(40500, DistanceUnit.UnitA), noScaling);

			Assert.That(operation.State, Is.SameAs(DueState.Overdue),
				"Cruzada y ademas pasada de la gracia: el cruce no le pone un techo de Due.");
		}

		// El cruce no le gana a la decision de la persona: mientras corre la ventana del
		// aplazamiento la operacion sigue aplazada (la tabla de transiciones solo admite el
		// cruce desde NotDue y DueSoon). Cumplida la ventana, el cruce si sostiene el Due.
		[Test]
		public void ACrossingDoesNotCancelADeferralInsideItsWindow()
		{
			ScheduledOperation operation = ATimeOrDistanceOperation();

			operation.Evaluate(
				new DateTime(2026, 1, 20), AReadingOf(40000, DistanceUnit.UnitA), noScaling);
			operation.Defer(DeferralReason.Cost, new DateTime(2026, 1, 20));

			operation.RecordConditionThresholdCrossing(new DateTime(2026, 1, 25));

			Assert.That(operation.State, Is.SameAs(DueState.Deferred),
				"Aplazar es la decision de la persona (OINV-28). Un cruce que la revirtiera "
				+ "haria que el sistema le discuta una decision que ya tomo en regla.");
		}

		// Un cruce observado ANTES del ancla de la operacion es un defecto de quien llama, por
		// la misma razon que evaluar antes del ancla lo es: no hay operacion todavia sobre la
		// que el cruce pueda decir nada.
		[Test]
		public void ACrossingObservedBeforeTheBasePointThrows()
		{
			ScheduledOperation operation = ATimeOrDistanceOperation();

			Assert.Throws<DomainException>(
				() => operation.RecordConditionThresholdCrossing(LastCompletion.AddDays(-1)));
		}

		private ScheduledOperation AConditionOnlyOperation()
		{
			return AnOperationRuledBy(IntervalRule.Of(
				IntervalBasisCode.ConditionOnly, 0, 0, DistanceUnitCode.UnitA));
		}

		private ScheduledOperation ATimeOrDistanceOperation()
		{
			return AnOperationRuledBy(IntervalRule.Of(
				IntervalBasisCode.TimeOrDistance, TwelveMonthsInDays, TenThousandUnits,
				DistanceUnitCode.UnitA));
		}

		private ScheduledOperation AnOperationRuledBy(IntervalRule rule)
		{
			MaintenanceOperationDefinition definition = new MaintenanceOperationDefinition(
				MaintenanceOperationKind.WearPartReplacement, rule, true, false);

			return ScheduledOperation.FromTheManufacturerPlan(
				definition,
				BasePoint.At(LastCompletion, ReadingAtTheLastCompletion, DistanceUnit.UnitA));
		}

		private static UsableReading AReadingOf(int value, DistanceUnit unit)
		{
			return UsableReading.Of(new OdometerReading(
				value, unit, new DateTime(2026, 1, 1), ReadingSource.Telemetry));
		}
	}
}
