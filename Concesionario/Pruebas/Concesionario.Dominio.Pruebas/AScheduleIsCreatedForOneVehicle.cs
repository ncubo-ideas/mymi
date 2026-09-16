using System;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LA CREACION Y LA SIEMBRA DEL AGREGADO (§8 del slice, C18).
	//
	// La creacion es el CONSTRUCTOR (E20): no hay operacion aparte de creacion, y lo que recibe
	// es exactamente lo que §8 dice -- el Vin y el perfil de uso inicial, nada mas.
	//
	// EL ANCLA VIAJA CON LA SIEMBRA, y esa es la resolucion de C18 confirmada con el usuario.
	// Una operacion que nunca se completo igual tiene que medirse desde algun lado (OINV-18 dice
	// que desde la entrega), y §7 le prohibe al dominio inventar un instante que no le dieron. El
	// ancla no fue a la creacion porque C5 ya lo impedia: el reemplazo de una revision se siembra
	// un ano despues y tiene que anclar EN LA REVISION, no en la entrega.
	//
	// LA LECTURA DEL ANCLA LLEVA SU PROPIA UNIDAD, que es la otra mitad de C18. Que las dos
	// unidades puedan diferir no es una forma que haya que hacer imposible: produce una operacion
	// con la distancia suspendida desde que nace, y eso es la ley L7 funcionando -- exactamente el
	// escenario de OINV-21 alrededor del cual se escribio el slice.
	[TestFixture]
	public sealed class AScheduleIsCreatedForOneVehicle
	{
		private const string TheVin = "1HGCM82633A004352";
		private const int TwelveMonthsInDays = 365;
		private const int TenThousandUnits = 10000;

		private static readonly DateTime Delivery = new DateTime(2025, 6, 1);
		private const int OdometerAtDelivery = 30000;

		private static readonly DateTime TheLimitIsReached = new DateTime(2026, 1, 20);

		private MaintenanceSchedule schedule;

		[SetUp]
		public void Setup()
		{
			schedule = new MaintenanceSchedule(TheVin, UsageProfileCode.Standard);
		}

		[Test]
		public void AScheduleWithoutAVinHasNoVehicleToBeAbout()
		{
			Assert.Throws<DomainException>(
				() => new MaintenanceSchedule(null, UsageProfileCode.Standard));
			Assert.Throws<DomainException>(
				() => new MaintenanceSchedule("", UsageProfileCode.Standard));
			Assert.Throws<DomainException>(
				() => new MaintenanceSchedule("   ", UsageProfileCode.Standard),
				"OAGG-03: la agenda es UNA POR VIN. Una identidad en blanco no es una agenda sin "
				+ "nombre todavia: es una agenda que no se puede decir de que vehiculo es.");
		}

		[Test]
		public void AnUnknownUsageProfileIsACallerBugAndNotAFourthProfile()
		{
			Assert.Throws<DomainException>(
				() => new MaintenanceSchedule(TheVin, (UsageProfileCode)99),
				"E32 y la polaridad de conjunto cerrado de E41: afuera del conjunto no hay "
				+ "miembro, asi que un codigo desconocido no es una respuesta vacia.");
		}

		[Test]
		public void TheScheduleKeepsTheVinItWasCreatedWith()
		{
			Assert.That(schedule.ReadVin(), Is.EqualTo(TheVin));
		}

		// El ancla RECIBIDA es desde donde se mide. Si la siembra la ignorara y anclara en
		// cualquier otro lado, 40000 no seria el limite de esta operacion y esto no seria Due.
		[Test]
		public void ASeededOperationIsMeasuredFromTheAnchorItWasSeededWith()
		{
			SeedAFluidChangeAnchoredIn(DistanceUnitCode.UnitA);

			schedule.RecordReading(
				40000, DistanceUnitCode.UnitA, TheLimitIsReached, ReadingSourceCode.Telemetry);
			schedule.Evaluate(TheLimitIsReached);

			Assert.That(schedule.ReadDueState(OperationKindCode.FluidChange), Is.EqualTo("Due"),
				"30000 en la entrega mas 10000 de limite son 40000. El ancla no se infiere ni se "
				+ "inventa: llega con la siembra (C18).");
		}

		// C18, la mitad del odometro: la lectura del ancla lleva SU unidad, no la del limite.
		// Cuando difieren, la operacion nace con la distancia suspendida y LO DICE (GAP G2), en
		// vez de parecer al dia.
		[Test]
		public void AnAnchorMeasuredInTheOtherUnitSuspendsTheDistanceFromBirth()
		{
			SeedAFluidChangeAnchoredIn(DistanceUnitCode.UnitB);

			schedule.RecordReading(
				40000, DistanceUnitCode.UnitA, TheLimitIsReached, ReadingSourceCode.Telemetry);
			schedule.Evaluate(TheLimitIsReached);

			ADueReason reason = schedule.TheDueReasonFor(OperationKindCode.FluidChange);

			Assert.That(reason.ClauseCodes, Does.Contain(LimitClause.UnitMismatch),
				"El limite esta en UnitA y el ancla en UnitB. Las unidades NO se convierten "
				+ "(OINV-21, ley L7), asi que la distancia no se compara: se suspende.");
			Assert.That(schedule.ReadDueState(OperationKindCode.FluidChange), Is.EqualTo("NotDue"),
				"Y degrada a solo-tiempo: 233 dias de 365. Si aca dijera Due, la distancia se "
				+ "habria convertido por debajo.");
		}

		// La guarda de OINV-20 vive en IntervalRule y la siembra la delega. Lo que esta prueba
		// agrega es que una siembra rechazada NO deja media operacion en la agenda.
		[Test]
		public void ASeedWhoseLimitsDoNotMatchItsBasisNeverEntersTheSchedule()
		{
			Assert.Throws<DomainException>(() => schedule.SeedPlanOperation(
				OperationKindCode.TimedComponent, IntervalBasisCode.TimeOnly,
				TwelveMonthsInDays, TenThousandUnits, DistanceUnitCode.UnitA,
				true, false,
				Delivery, OdometerAtDelivery, DistanceUnitCode.UnitA));

			Assert.That(
				schedule.TheDueReasonFor(OperationKindCode.TimedComponent).Shape,
				Is.EqualTo(ADueReason.NoOperationScheduled),
				"E12, mutar al final: la operacion se arma y se valida ANTES de entrar. Si "
				+ "quedara sembrada a medias, la agenda tendria una operacion cuya ley nadie "
				+ "acepto.");
		}

		[Test]
		public void AnUnknownOperationKindIsACallerBugOnTheSeedingPath()
		{
			Assert.Throws<DomainException>(() => schedule.SeedPlanOperation(
				(OperationKindCode)99, IntervalBasisCode.TimeOnly,
				TwelveMonthsInDays, 0, DistanceUnitCode.UnitA,
				true, false,
				Delivery, OdometerAtDelivery, DistanceUnitCode.UnitA));
		}

		// La siembra resuelve DOS unidades: la del limite y la de la lectura del ancla. Las dos
		// se resuelven una sola vez, en la frontera (E32), asi que las dos revientan igual.
		[Test]
		public void BothUnitsOfASeedAreResolvedAtTheEdge()
		{
			Assert.Throws<DomainException>(() => schedule.SeedPlanOperation(
				OperationKindCode.FluidChange, IntervalBasisCode.TimeOrDistance,
				TwelveMonthsInDays, TenThousandUnits, (DistanceUnitCode)99,
				true, false,
				Delivery, OdometerAtDelivery, DistanceUnitCode.UnitA));

			Assert.Throws<DomainException>(() => schedule.SeedPlanOperation(
				OperationKindCode.FluidChange, IntervalBasisCode.TimeOrDistance,
				TwelveMonthsInDays, TenThousandUnits, DistanceUnitCode.UnitA,
				true, false,
				Delivery, OdometerAtDelivery, (DistanceUnitCode)99));
		}

		private void SeedAFluidChangeAnchoredIn(DistanceUnitCode anchorUnit)
		{
			schedule.SeedPlanOperation(
				OperationKindCode.FluidChange, IntervalBasisCode.TimeOrDistance,
				TwelveMonthsInDays, TenThousandUnits, DistanceUnitCode.UnitA,
				true, false,
				Delivery, OdometerAtDelivery, anchorUnit);
		}
	}
}
