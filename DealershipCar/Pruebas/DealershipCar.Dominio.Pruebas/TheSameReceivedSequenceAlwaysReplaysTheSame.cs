using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// PRUEBA 16 de §9: "dos corridas de la misma secuencia de instantes y lecturas recibidos
	// producen estados identicos. Esta es la prueba que atrapa un DateTime.Now colado."
	//
	// El grep de §12 encuentra la palabra; esta prueba encuentra el HECHO. Un reloj leido adentro
	// del dominio no tiene por que llamarse DateTime.Now -- puede llegar por una dependencia, por
	// un TimeProvider, por un default que alguien puso "para no romper una firma" -- y todos se
	// ven igual desde afuera: dos corridas de la misma historia dejan de contestar lo mismo.
	//
	// LAS DOS AGENDAS SE CONSTRUYEN CON LA MISMA SECUENCIA Y SE COMPARAN LECTURA POR LECTURA,
	// incluida una clase que nunca se sembro: la respuesta vacia tambien tiene que ser la misma.
	// Es lo que E14 promete del replay, y lo que le paga a T6 el derecho de correr las mismas
	// dieciseis afirmaciones contra un actor journalizado de verdad.
	[TestFixture]
	public sealed class TheSameReceivedSequenceAlwaysReplaysTheSame
	{
		private const string TheVin = "1HGCM82633A004352";
		private const int TwelveMonthsInDays = 365;
		private const int TenThousandUnits = 10000;

		private static readonly DateTime Delivery = new DateTime(2025, 6, 1);
		private const int OdometerAtDelivery = 30000;

		[Test]
		public void TwoRunsOfTheSameStoryAgreeOnEveryRead()
		{
			MaintenanceSchedule first = AScheduleThatLivedTheWholeStory();
			MaintenanceSchedule second = AScheduleThatLivedTheWholeStory();

			foreach (OperationKindCode kind in Enum.GetValues(typeof(OperationKindCode)))
			{
				Assert.That(second.ReadDueState(kind), Is.EqualTo(first.ReadDueState(kind)),
					"El estado de " + kind + " difiere entre dos corridas de la misma historia.");
				Assert.That(second.ReadDueReason(kind), Is.EqualTo(first.ReadDueReason(kind)),
					"El motivo de " + kind + " difiere entre dos corridas de la misma historia.");
				Assert.That(second.DeferralsUsed(kind), Is.EqualTo(first.DeferralsUsed(kind)));
			}

			Assert.That(second.CountDue(), Is.EqualTo(first.CountDue()));
			Assert.That(second.CountOverdue(), Is.EqualTo(first.CountOverdue()));
			Assert.That(second.MissedCount(), Is.EqualTo(first.MissedCount()));
			Assert.That(second.HasAnythingDue(), Is.EqualTo(first.HasAnythingDue()));
			Assert.That(second.ReadNextDueKind(), Is.EqualTo(first.ReadNextDueKind()));
			Assert.That(second.LastEvaluatedAt(), Is.EqualTo(first.LastEvaluatedAt()));
			Assert.That(second.ReadVin(), Is.EqualTo(first.ReadVin()));
		}

		// La historia tiene que MOVER algo, o dos agendas vacias tambien coincidirian. Estas son
		// las lecturas que la historia deja distintas de sus valores de nacimiento.
		[Test]
		public void TheStoryUsedByThisFixtureActuallyMovesTheSchedule()
		{
			MaintenanceSchedule schedule = AScheduleThatLivedTheWholeStory();

			Assert.That(schedule.HasAnythingDue(), Is.True);
			Assert.That(schedule.CountDue(), Is.GreaterThan(0));
			Assert.That(schedule.MissedCount(), Is.GreaterThan(0));
			Assert.That(schedule.DeferralsUsed(OperationKindCode.FluidChange), Is.EqualTo(1));
		}

		// El mismo hecho visto por el otro lado: la historia no depende del reloj de la maquina,
		// asi que correrla "mas tarde" no la cambia. Si el dominio leyera un reloj, el intervalo
		// entre las dos construcciones de arriba se colaria en alguna de las dos respuestas.
		[Test]
		public void TheAnswerDependsOnlyOnTheInstantsThatWereReceived()
		{
			MaintenanceSchedule tenYearsAhead = new MaintenanceSchedule(
				TheVin, UsageProfileCode.Standard);

			tenYearsAhead.SeedPlanOperation(
				OperationKindCode.FluidChange, IntervalBasisCode.TimeOrDistance,
				TwelveMonthsInDays, TenThousandUnits, DistanceUnitCode.UnitA,
				true, false,
				Delivery.AddYears(10), OdometerAtDelivery, DistanceUnitCode.UnitA);

			tenYearsAhead.Evaluate(Delivery.AddYears(10).AddDays(TwelveMonthsInDays));

			Assert.That(
				tenYearsAhead.ReadDueState(OperationKindCode.FluidChange), Is.EqualTo("Due"),
				"Una agenda anclada diez anos en el futuro vence exactamente igual: lo que "
				+ "decide es el instante RECIBIDO, no en que ano esta corriendo la prueba.");
		}

		private static MaintenanceSchedule AScheduleThatLivedTheWholeStory()
		{
			MaintenanceSchedule schedule = new MaintenanceSchedule(
				TheVin, UsageProfileCode.Standard);

			schedule.SeedPlanOperation(
				OperationKindCode.FluidChange, IntervalBasisCode.TimeOrDistance,
				TwelveMonthsInDays, TenThousandUnits, DistanceUnitCode.UnitA,
				true, false,
				Delivery, OdometerAtDelivery, DistanceUnitCode.UnitA);

			schedule.SeedPlanOperation(
				OperationKindCode.FilterChange, IntervalBasisCode.DistanceOnly,
				0, 9000, DistanceUnitCode.UnitA,
				true, false,
				Delivery, OdometerAtDelivery, DistanceUnitCode.UnitA);

			schedule.SeedPlanOperation(
				OperationKindCode.Inspection, IntervalBasisCode.ConditionOnly,
				0, 0, DistanceUnitCode.UnitA,
				true, false,
				Delivery, OdometerAtDelivery, DistanceUnitCode.UnitA);

			schedule.AddOwnerOperation(
				OperationKindCode.Diagnostic, IntervalBasisCode.TimeOnly,
				TwelveMonthsInDays, 0, DistanceUnitCode.UnitA,
				Delivery, OdometerAtDelivery, DistanceUnitCode.UnitA);

			foreach (KeyValuePair<DateTime, int> reading in TheReadingsInTheOrderTheyArrived())
				schedule.RecordReading(
					reading.Value, DistanceUnitCode.UnitA, reading.Key,
					ReadingSourceCode.Telemetry);

			schedule.Evaluate(new DateTime(2026, 1, 20));
			schedule.DeferOperation(
				OperationKindCode.FluidChange, DeferralReasonCode.Cost, new DateTime(2026, 1, 20));

			schedule.DeclareUsageProfile(UsageProfileCode.SevereDuty, new DateTime(2026, 2, 1));
			schedule.RecordConditionThreshold(
				OperationKindCode.Inspection, new DateTime(2026, 2, 1));

			schedule.Evaluate(new DateTime(2026, 2, 10));

			schedule.AdoptPlanRevision("R2", new DateTime(2026, 2, 20));
			schedule.Evaluate(new DateTime(2026, 3, 1));

			return schedule;
		}

		// Llegan DESORDENADAS a proposito: la de diciembre entra despues de la de enero. Si el
		// agregado ordenara por llegada, las dos corridas seguirian coincidiendo entre si, pero
		// coincidirian en la respuesta equivocada -- por eso la prueba de orden vive aparte, en
		// TheScheduleOrdersItsReadingsByObservationInstant.
		private static IEnumerable<KeyValuePair<DateTime, int>> TheReadingsInTheOrderTheyArrived()
		{
			yield return new KeyValuePair<DateTime, int>(new DateTime(2026, 1, 20), 40000);
			yield return new KeyValuePair<DateTime, int>(new DateTime(2025, 12, 1), 35000);
			yield return new KeyValuePair<DateTime, int>(new DateTime(2026, 2, 10), 41000);
			yield return new KeyValuePair<DateTime, int>(new DateTime(2026, 3, 1), 42000);
		}
	}
}
