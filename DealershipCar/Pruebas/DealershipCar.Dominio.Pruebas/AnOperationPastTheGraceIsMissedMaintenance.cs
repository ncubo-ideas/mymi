using System;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// PRUEBA 15 de §9: "Una operacion pasa Due -> OverdueGrace -> Overdue; MissedCount sube.
	// (La bandera de exposicion de garantia es de un slice posterior -- este solo cuenta.)"
	//
	// MissedCount es del agregado (T5). La mitad que le toca a la OCURRENCIA es la de aportar
	// el hecho, y es donde muerde E35: el aporte es DERIVADO del estado, nunca un contador que
	// alguien incrementa al cruzar la gracia.
	//
	// LA DIFERENCIA SE VE EN UNA SOLA PRUEBA, y es la ultima de este archivo: la operacion se
	// completa y DEJA DE CONTAR SOLA. Con un contador, alguien tendria que acordarse de
	// restarlo -- y el dia que se olvide, el vehiculo arrastra un mantenimiento perdido que ya
	// se hizo, para siempre, sin que nada lo diga. Es la misma forma del GAP G4 un piso mas
	// arriba: lo derivado se cura solo, lo guardado hay que apagarlo a mano.
	//
	// La escalera de C16 es la de siempre: alcanzar un umbral es ENTRAR en su estado. Con una
	// gracia de 500, la unidad 500 pasada del limite es la que la consume entera.
	[TestFixture]
	public sealed class AnOperationPastTheGraceIsMissedMaintenance
	{
		private const int TwelveMonthsInDays = 365;
		private const int TenThousandUnits = 10000;

		private static readonly DateTime LastCompletion = new DateTime(2025, 6, 1);
		private const int ReadingAtTheLastCompletion = 30000;

		private static readonly DateTime Observed = new DateTime(2026, 1, 20);

		private ScheduledOperation operation;
		private IntervalLengthFactor noScaling;

		[SetUp]
		public void Setup()
		{
			noScaling = IntervalLengthFactor.For(UsageProfile.Standard);

			MaintenanceOperationDefinition definition = new MaintenanceOperationDefinition(
				MaintenanceOperationKind.FluidChange,
				IntervalRule.Of(
					IntervalBasisCode.TimeOrDistance, TwelveMonthsInDays, TenThousandUnits,
					DistanceUnitCode.UnitA),
				true,
				false);

			operation = ScheduledOperation.FromTheManufacturerPlan(
				definition,
				BasePoint.At(LastCompletion, ReadingAtTheLastCompletion, DistanceUnit.UnitA));
		}

		[Test]
		public void AnOperationThatIsMerelyDueIsNotMissedYet()
		{
			EvaluateAt(40000);

			Assert.That(operation.State, Is.SameAs(DueState.Due));
			Assert.That(operation.CountsAsMissedMaintenance(), Is.False,
				"OINV-24: Due y Overdue son estados distintos porque le dicen cosas distintas a "
				+ "la persona. Contar la vencida como perdida borra esa diferencia justo donde "
				+ "importa.");
		}

		[Test]
		public void InsideTheGraceItIsStillOnlyDue()
		{
			EvaluateAt(40499);

			Assert.That(operation.State, Is.SameAs(DueState.Due));
			Assert.That(operation.CountsAsMissedMaintenance(), Is.False);
		}

		[Test]
		public void OnceTheGraceIsSpentTheOperationIsMissedMaintenance()
		{
			DueStateTransition transition = EvaluateAt(40500);

			Assert.That(operation.State, Is.SameAs(DueState.Overdue),
				"500 unidades pasadas del limite son la gracia entera (C16).");
			Assert.That(operation.CountsAsMissedMaintenance(), Is.True);
			Assert.That(transition.BecameOverdue, Is.True,
				"El host tiene que poder decir 'algo se paso', que es lo que hace subir "
				+ "MissedCount sin recorrer nada.");
		}

		[Test]
		public void TheOperationWalksDueThenTheGraceThenOverdue()
		{
			EvaluateAt(40000);
			Assert.That(operation.CountsAsMissedMaintenance(), Is.False);

			EvaluateAt(40499);
			Assert.That(operation.CountsAsMissedMaintenance(), Is.False);

			EvaluateAt(40500);
			Assert.That(operation.CountsAsMissedMaintenance(), Is.True);
		}

		// E35, Y ES LA PRUEBA QUE DISTINGUE LO DERIVADO DE UN CONTADOR. Se completa la
		// operacion y deja de contar SOLA, sin que nadie reste nada.
		[Test]
		public void CompletingAMissedOperationStopsItCountingWithoutAnybodySubtracting()
		{
			EvaluateAt(40500);

			Assert.That(operation.CountsAsMissedMaintenance(), Is.True);

			operation.Complete(new DateTime(2026, 2, 1), 40600, DistanceUnit.UnitA);

			Assert.That(operation.CountsAsMissedMaintenance(), Is.False,
				"Si esto fuera un contador incrementado al cruzar la gracia, el vehiculo "
				+ "arrastraria para siempre un mantenimiento perdido que ya se hizo. Derivado "
				+ "del estado, la completacion lo apaga sin que nadie se acuerde.");
		}

		// Una operacion superseded tampoco cuenta: dejo de existir como obligacion, y el
		// reemplazo empieza NotDue con su propia cuenta.
		[Test]
		public void ASupersededOperationStopsCountingToo()
		{
			EvaluateAt(40500);
			operation.Supersede();

			Assert.That(operation.CountsAsMissedMaintenance(), Is.False);
		}

		// Una operacion que nunca se evaluo no cuenta como perdida. Suena obvio y es
		// exactamente lo que un contador inicializado mal rompe.
		[Test]
		public void AnOperationThatWasNeverEvaluatedIsNotMissed()
		{
			Assert.That(operation.CountsAsMissedMaintenance(), Is.False);
		}

		private DueStateTransition EvaluateAt(int reading)
		{
			return operation.Evaluate(
				Observed,
				UsableReading.Of(new OdometerReading(
					reading, DistanceUnit.UnitA, Observed, ReadingSource.Telemetry)),
				noScaling);
		}
	}
}
