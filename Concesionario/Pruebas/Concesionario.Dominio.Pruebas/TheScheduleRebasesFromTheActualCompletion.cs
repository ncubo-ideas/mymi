using System;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// PRUEBA 5 de §9, CORRIDA POR LA FACHADA (ley L4, OINV-26): "vencida en 40000; completada el
	// 2026-02-01 en 39800 -> esa instancia Completed, y la nueva vence en 49800 o el 2027-02-01.
	// No en 50000."
	//
	// El numero que hace la prueba falsificable es 49800 y no 50000. Si la sucesora se anclara en
	// la grilla teorica -- 40000, donde la operacion DEBIA haberse hecho -- venceria 200 unidades
	// tarde, y otra vez 200 en la siguiente, y otra vez: el error se acumula hacia adelante y
	// nunca se corrige solo.
	//
	// T4 probo esto sobre ScheduledOperation. Lo que agrega este nivel es que la SUCESORA ocupe
	// el lugar de la clase en la agenda: sin eso, la operacion completada desapareceria de la
	// vista de la persona en vez de volver a empezar.
	[TestFixture]
	public sealed class TheScheduleRebasesFromTheActualCompletion
	{
		private const string TheVin = "1HGCM82633A004352";
		private const int TwelveMonthsInDays = 365;
		private const int TenThousandUnits = 10000;

		private static readonly DateTime Delivery = new DateTime(2025, 6, 1);
		private const int OdometerAtDelivery = 30000;

		private static readonly DateTime FellDue = new DateTime(2026, 1, 20);
		private static readonly DateTime CompletedOn = new DateTime(2026, 2, 1);
		private const int OdometerAtTheActualCompletion = 39800;

		private MaintenanceSchedule schedule;

		[SetUp]
		public void Setup()
		{
			schedule = new MaintenanceSchedule(TheVin, UsageProfileCode.Standard);
			SeedTheFluidChange();

			schedule.RecordReading(
				40000, DistanceUnitCode.UnitA, FellDue, ReadingSourceCode.Telemetry);
			schedule.Evaluate(FellDue);
		}

		// "Esa instancia Completed" no lo ve ninguna de las ocho lecturas de §4 -- la sucesora le
		// quita el lugar y la vieja deja de ser alcanzable -- asi que esta prueba instala la
		// operacion por la ley L5, que devuelve la que quedo activa, y despues la mira.
		[Test]
		public void TheCompletedInstanceEndsAndItsSuccessorTakesTheKindsSlot()
		{
			MaintenanceSchedule fresh = new MaintenanceSchedule(TheVin, UsageProfileCode.Standard);

			ScheduledOperation installed = fresh.OneActiveOperationPerKind(
				OperationKindCode.FluidChange,
				AFluidChangeDefinition(),
				BasePoint.At(Delivery, OdometerAtDelivery, DistanceUnit.UnitA),
				false);

			fresh.RecordReading(
				40000, DistanceUnitCode.UnitA, FellDue, ReadingSourceCode.Telemetry);
			fresh.Evaluate(FellDue);

			Assert.That(fresh.ReadDueState(OperationKindCode.FluidChange), Is.EqualTo("Due"));

			fresh.CompleteOperation(
				OperationKindCode.FluidChange, CompletedOn, OdometerAtTheActualCompletion,
				DistanceUnitCode.UnitA);

			Assert.That(installed.State, Is.SameAs(DueState.Completed));
			Assert.That(installed.HasEnded(), Is.True);

			Assert.That(fresh.ReadDueState(OperationKindCode.FluidChange), Is.EqualTo("NotDue"),
				"La clase sigue en la agenda, ocupada por la SUCESORA. Si quedara vacia, hacer "
				+ "una operacion la borraria del mantenimiento del vehiculo.");
			Assert.That(fresh.CountDue(), Is.EqualTo(0));
		}

		// La mitad de la distancia: 39800 + 10000 = 49800, y no 40000 + 10000 = 50000.
		[Test]
		public void TheSuccessorFallsDueAtTheActualCompletionPlusTheLimitAndNotAtTheGrid()
		{
			schedule.CompleteOperation(
				OperationKindCode.FluidChange, CompletedOn, OdometerAtTheActualCompletion,
				DistanceUnitCode.UnitA);

			schedule.RecordReading(
				49799, DistanceUnitCode.UnitA, new DateTime(2026, 6, 1),
				ReadingSourceCode.Telemetry);
			schedule.Evaluate(new DateTime(2026, 6, 1));

			Assert.That(schedule.ReadDueState(OperationKindCode.FluidChange), Is.EqualTo("DueSoon"),
				"Una unidad antes de 49800.");

			schedule.RecordReading(
				49800, DistanceUnitCode.UnitA, new DateTime(2026, 6, 2),
				ReadingSourceCode.Telemetry);
			schedule.Evaluate(new DateTime(2026, 6, 2));

			Assert.That(schedule.ReadDueState(OperationKindCode.FluidChange), Is.EqualTo("Due"),
				"49800 son exactamente 10000 desde donde la operacion se hizo DE VERDAD. Con la "
				+ "grilla teorica el limite seria 50000 y la operacion venceria 200 unidades "
				+ "tarde -- y otras 200 la vez siguiente.");
		}

		// La otra mitad de la ley L4 sobre la misma completacion: el instante tambien se re-ancla,
		// asi que la sucesora vence el 2027-02-01 y no el 2026-06-01, que era la grilla vieja.
		[Test]
		public void TheSuccessorAlsoCountsItsTimeFromTheActualCompletionInstant()
		{
			schedule.CompleteOperation(
				OperationKindCode.FluidChange, CompletedOn, OdometerAtTheActualCompletion,
				DistanceUnitCode.UnitA);

			schedule.Evaluate(new DateTime(2026, 6, 1));

			Assert.That(schedule.ReadDueState(OperationKindCode.FluidChange), Is.EqualTo("NotDue"),
				"El 2026-06-01 era el vencimiento de la instancia VIEJA. La sucesora se mide "
				+ "desde el 2026-02-01 y ahi lleva 120 dias de 365.");

			schedule.Evaluate(CompletedOn.AddDays(TwelveMonthsInDays));

			Assert.That(schedule.ReadDueState(OperationKindCode.FluidChange), Is.EqualTo("Due"),
				"365 dias desde la completacion real: el 2027-02-01.");
		}

		// LA COMPLETACION TRAE SU UNIDAD, y esa unidad es la del ancla de la sucesora. Una
		// completacion medida en la otra unidad deja a la sucesora con la distancia suspendida
		// desde que nace -- la ley L7 funcionando, no un defecto -- y la agenda lo dice.
		//
		// Esta prueba existe porque la falsificacion mostro que fijar la unidad a mano en la
		// fachada dejaba las 284 pruebas en verde: ninguna completaba en la otra unidad, asi que
		// una lectura de un taller que mide distinto habria entrado como si midiera igual.
		[Test]
		public void ACompletionMeasuredInTheOtherUnitSuspendsTheSuccessorsDistance()
		{
			schedule.CompleteOperation(
				OperationKindCode.FluidChange, CompletedOn, OdometerAtTheActualCompletion,
				DistanceUnitCode.UnitB);

			schedule.Evaluate(new DateTime(2026, 6, 1));

			Assert.That(
				schedule.ReadDueReason(OperationKindCode.FluidChange),
				Does.Contain("las unidades nunca se convierten"),
				"El limite esta en UnitA y el ancla nueva quedo en UnitB. Si la fachada hubiera "
				+ "puesto UnitA por su cuenta, la sucesora mediria 200 unidades desde una lectura "
				+ "que nadie tomo en esa escala.");
			Assert.That(schedule.ReadDueState(OperationKindCode.FluidChange), Is.EqualTo("NotDue"),
				"Y degrada a solo-tiempo: 120 dias de 365.");
		}

		private static MaintenanceOperationDefinition AFluidChangeDefinition()
		{
			return new MaintenanceOperationDefinition(
				MaintenanceOperationKind.FluidChange,
				IntervalRule.Of(
					IntervalBasisCode.TimeOrDistance, TwelveMonthsInDays, TenThousandUnits,
					DistanceUnitCode.UnitA),
				true,
				false);
		}

		private void SeedTheFluidChange()
		{
			schedule.SeedPlanOperation(
				OperationKindCode.FluidChange, IntervalBasisCode.TimeOrDistance,
				TwelveMonthsInDays, TenThousandUnits, DistanceUnitCode.UnitA,
				true, false,
				Delivery, OdometerAtDelivery, DistanceUnitCode.UnitA);
		}
	}
}
