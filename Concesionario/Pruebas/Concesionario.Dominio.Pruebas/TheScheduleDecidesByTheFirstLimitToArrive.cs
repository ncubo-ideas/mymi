using System;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LAS PRUEBAS 1, 2, 3, 4, 6 y 7 de §9, CORRIDAS POR LA FACHADA. T3 ya las probo sobre
	// IntervalRule y T4 sobre ScheduledOperation, cada una en su nivel; aca se prueban entrando
	// por MaintenanceSchedule, que es lo que pide el "hecho cuando" de T5.
	//
	// NO ES REPETICION: lo que se prueba aca es el CABLEADO, que en los otros dos niveles no
	// existe. El agregado tiene que elegir la lectura usable, resolver el factor del perfil y
	// pasarle los dos a la ley -- tres decisiones que ninguna prueba de T3 o T4 puede tocar,
	// porque en esos niveles esos valores llegan a mano desde la prueba.
	//
	// Intervalo: 12 meses o 10000 unidades, anclado el 2025-06-01 en 30000 UnitA. Vence el
	// 2026-06-01 o en 40000, lo que llegue primero (ley L1).
	[TestFixture]
	public sealed class TheScheduleDecidesByTheFirstLimitToArrive
	{
		private const string TheVin = "1HGCM82633A004352";
		private const int TwelveMonthsInDays = 365;
		private const int TenThousandUnits = 10000;

		private static readonly DateTime Delivery = new DateTime(2025, 6, 1);
		private const int OdometerAtDelivery = 30000;

		private MaintenanceSchedule schedule;

		[SetUp]
		public void Setup()
		{
			schedule = new MaintenanceSchedule(TheVin, UsageProfileCode.Standard);
			SeedTheFluidChange(DistanceUnitCode.UnitA, false);
		}

		// PRUEBA 1 de §9: 39000 el 2026-01-15 -> DueSoon, y el motivo nombra la DISTANCIA.
		[Test]
		public void TheCourtesyWarningArrivesByDistanceWhenDistanceIsAhead()
		{
			ReadingOf(39000, new DateTime(2026, 1, 15));
			schedule.Evaluate(new DateTime(2026, 1, 15));

			Assert.That(schedule.ReadDueState(OperationKindCode.FluidChange), Is.EqualTo("DueSoon"));
			Assert.That(
				schedule.TheDueReasonFor(OperationKindCode.FluidChange).WhoDecided,
				Is.EqualTo(IntervalDimension.Distance),
				"9000 de 10000 esta dentro del anticipo de 1000; 228 dias de 365 no. Decide la "
				+ "dimension mas avanzada, y el motivo dice cual fue.");
		}

		// PRUEBA 2 de §9: 33000 el 2026-05-02 -> DueSoon, y el motivo nombra el TIEMPO.
		[Test]
		public void TheCourtesyWarningArrivesByTimeWhenTimeIsAhead()
		{
			ReadingOf(33000, new DateTime(2026, 5, 2));
			schedule.Evaluate(new DateTime(2026, 5, 2));

			Assert.That(schedule.ReadDueState(OperationKindCode.FluidChange), Is.EqualTo("DueSoon"));
			Assert.That(
				schedule.TheDueReasonFor(OperationKindCode.FluidChange).WhoDecided,
				Is.EqualTo(IntervalDimension.Time),
				"335 dias de 365 esta dentro del anticipo de 30; 3000 de 10000 no. La misma ley, "
				+ "la otra dimension: ninguna de las dos domina (OINV-20).");
		}

		// PRUEBA 3 de §9: el limite es INCLUSIVO, y se mira desde los dos lados del borde.
		[Test]
		public void TheLimitValueIsInclusiveSeenFromBothSidesOfTheEdge()
		{
			ReadingOf(39999, new DateTime(2026, 1, 19));
			schedule.Evaluate(new DateTime(2026, 1, 19));

			Assert.That(schedule.ReadDueState(OperationKindCode.FluidChange), Is.EqualTo("DueSoon"),
				"Una unidad antes del limite todavia es aviso.");

			ReadingOf(40000, new DateTime(2026, 1, 20));
			schedule.Evaluate(new DateTime(2026, 1, 20));

			Assert.That(schedule.ReadDueState(OperationKindCode.FluidChange), Is.EqualTo("Due"),
				"Y alcanzar el limite es entrar en su estado (C16): 40000 vence, no 40001.");
		}

		// PRUEBA 4 de §9: las dos dimensiones llegan el mismo dia -> Due, UNA transicion, y el
		// motivo nombra las DOS.
		[Test]
		public void WhenBothLimitsArriveTogetherTheReasonNamesThemBoth()
		{
			DateTime bothArrive = Delivery.AddDays(TwelveMonthsInDays);

			ReadingOf(40000, bothArrive);
			EvaluationResult result = schedule.Evaluate(bothArrive);

			Assert.That(schedule.ReadDueState(OperationKindCode.FluidChange), Is.EqualTo("Due"));
			Assert.That(result.TransitionCount, Is.EqualTo(1),
				"UNA transicion, no dos: la operacion vencio una vez, aunque hayan llegado las "
				+ "dos dimensiones juntas.");
			Assert.That(
				schedule.TheDueReasonFor(OperationKindCode.FluidChange).WhoDecided,
				Is.EqualTo(ADueReason.TimeAndDistance),
				"Empatadas deciden las dos, y decirlo importa: la persona necesita saber que "
				+ "esta operacion vencio por las dos razones y no por una.");
		}

		// PRUEBA 6 de §9, la ley L10: el aviso de cortesia NUNCA es una parada obligatoria. Si el
		// vencimiento se alcanza en la evaluacion que habria puesto DueSoon, la operacion va
		// DIRECTO a Due, en una sola transicion.
		[Test]
		public void TheOperationGoesStraightToDueWithoutStoppingAtTheCourtesyWarning()
		{
			ReadingOf(40000, new DateTime(2026, 1, 20));
			EvaluationResult result = schedule.Evaluate(new DateTime(2026, 1, 20));

			Assert.That(schedule.ReadDueState(OperationKindCode.FluidChange), Is.EqualTo("Due"),
				"Nunca paso por DueSoon: la agenda no habia sido evaluada desde la entrega, asi "
				+ "que la primera evaluacion la encuentra ya vencida.");
			Assert.That(result.TransitionCount, Is.EqualTo(1),
				"Y es UNA transicion. Si la operacion caminara el diagrama -- NotDue, DueSoon, "
				+ "Due -- el host recibiria dos avisos por un solo hecho (OEX-06).");
		}

		// PRUEBA 7 de §9, la ley L7: el limite esta en UnitA y llega una lectura en UnitB. NO se
		// hace ninguna comparacion de distancia, la operacion cae a solo-tiempo, y se DICE.
		//
		// La afirmacion que prueba que no hubo conversion es la de abajo: 40000 en UnitB, tomadas
		// como si fueran UnitA, serian exactamente el limite y la operacion venceria. No vence.
		[Test]
		public void AReadingInTheOtherUnitSuspendsTheDistanceInsteadOfBeingConverted()
		{
			schedule.RecordReading(
				40000, DistanceUnitCode.UnitB, new DateTime(2026, 1, 20),
				ReadingSourceCode.Telemetry);
			schedule.Evaluate(new DateTime(2026, 1, 20));

			Assert.That(schedule.ReadDueState(OperationKindCode.FluidChange), Is.EqualTo("NotDue"),
				"OINV-21: si alguien hubiera convertido, 40000 seria el limite y esto seria Due. "
				+ "La operacion degrada a solo-tiempo -- 233 dias de 365 -- y no vence.");
			Assert.That(
				schedule.TheDueReasonFor(OperationKindCode.FluidChange).ClauseCodes,
				Does.Contain(LimitClause.UnitMismatch),
				"Y el motivo lo dice con todas las letras. Ese NotDue no es evidencia de que la "
				+ "operacion este al dia por distancia: es que no se pudo mirar (GAP G2).");
		}

		private void SeedTheFluidChange(DistanceUnitCode limitUnit, bool isSafety)
		{
			schedule.SeedPlanOperation(
				OperationKindCode.FluidChange, IntervalBasisCode.TimeOrDistance,
				TwelveMonthsInDays, TenThousandUnits, limitUnit,
				true, isSafety,
				Delivery, OdometerAtDelivery, DistanceUnitCode.UnitA);
		}

		private void ReadingOf(int value, DateTime observedAt)
		{
			schedule.RecordReading(
				value, DistanceUnitCode.UnitA, observedAt, ReadingSourceCode.Telemetry);
		}
	}
}
