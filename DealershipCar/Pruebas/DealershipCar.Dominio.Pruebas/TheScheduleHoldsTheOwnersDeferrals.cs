using System;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LAS PRUEBAS 8, 9 y 10 de §9, CORRIDAS POR LA FACHADA (ley L6, OINV-27, OINV-52). T4 ya las
	// probo sobre ScheduledOperation; lo que se prueba aca es que el agregado le lleva a la
	// operacion los hechos correctos -- el motivo resuelto del wire, el instante recibido, la
	// lectura usable -- y que DeferralsUsed sale por la lectura con clave de §4.
	//
	// EL ESCENARIO ES DistanceOnly A PROPOSITO, por la misma razon que en T4: una ley que mide
	// tiempo no puede quedarse quieta mientras corre una ventana de 60 dias, porque al vencerla
	// ya consumio la gracia de 14 y contesta Overdue. Con distancia y sin lecturas nuevas, la ley
	// contesta lo mismo antes y despues de la ventana (C19).
	[TestFixture]
	public sealed class TheScheduleHoldsTheOwnersDeferrals
	{
		private const string TheVin = "1HGCM82633A004352";
		private const int TenThousandUnits = 10000;

		private static readonly DateTime Delivery = new DateTime(2025, 6, 1);
		private const int OdometerAtDelivery = 30000;

		private static readonly DateTime FellDue = new DateTime(2026, 1, 20);
		private static readonly DateTime TheWindowEnds = new DateTime(2026, 3, 21);
		private static readonly DateTime TheSecondWindowEnds = new DateTime(2026, 5, 20);

		private MaintenanceSchedule schedule;

		[SetUp]
		public void Setup()
		{
			schedule = new MaintenanceSchedule(TheVin, UsageProfileCode.Standard);
			SeedAWearPartOf(false);

			schedule.RecordReading(
				40000, DistanceUnitCode.UnitA, FellDue, ReadingSourceCode.Telemetry);
		}

		// PRUEBA 8 de §9: aplazar una vez -> Deferred, DeferralsUsed = 1. Cumplida la ventana, la
		// ley vuelve a decidir y la cuenta se conserva.
		[Test]
		public void DeferringOnceHoldsTheOperationUntilTheWindowIsSpent()
		{
			schedule.Evaluate(FellDue);

			Assert.That(
				schedule.ReadDueState(OperationKindCode.WearPartReplacement), Is.EqualTo("Due"),
				"Tiene que llegar vencida al aplazamiento, o esta prueba estaria mirando el "
				+ "no-op de E36 sin darse cuenta.");

			schedule.DeferOperation(
				OperationKindCode.WearPartReplacement, DeferralReasonCode.Cost, FellDue);

			Assert.That(
				schedule.ReadDueState(OperationKindCode.WearPartReplacement),
				Is.EqualTo("Deferred"));
			Assert.That(
				schedule.DeferralsUsed(OperationKindCode.WearPartReplacement), Is.EqualTo(1));

			schedule.Evaluate(TheWindowEnds.AddDays(-1));

			Assert.That(
				schedule.ReadDueState(OperationKindCode.WearPartReplacement),
				Is.EqualTo("Deferred"),
				"Un dia antes sigue aplazada.");

			schedule.Evaluate(TheWindowEnds);

			Assert.That(
				schedule.ReadDueState(OperationKindCode.WearPartReplacement), Is.EqualTo("Due"),
				"Cumplida la ventana la ley vuelve a decidir, y esta ley sigue diciendo Due: "
				+ "10000 unidades de un limite de 10000, sin lecturas nuevas (C19).");
			Assert.That(
				schedule.DeferralsUsed(OperationKindCode.WearPartReplacement), Is.EqualTo(1),
				"LA CUENTA SE CONSERVA. Si se reseteara al salir de la ventana, el tope de dos "
				+ "no seria un tope: serian dos por ventana, para siempre.");
		}

		// PRUEBA 9 de §9: dos aplazamientos, y el tercero se rechaza sin subir la cuenta.
		[Test]
		public void TheThirdDeferralIsRefusedAndTheCountStaysAtTheCap()
		{
			schedule.Evaluate(FellDue);
			schedule.DeferOperation(
				OperationKindCode.WearPartReplacement, DeferralReasonCode.Cost, FellDue);

			schedule.Evaluate(TheWindowEnds);
			schedule.DeferOperation(
				OperationKindCode.WearPartReplacement, DeferralReasonCode.Timing, TheWindowEnds);

			Assert.That(
				schedule.DeferralsUsed(OperationKindCode.WearPartReplacement), Is.EqualTo(2));

			schedule.Evaluate(TheSecondWindowEnds);

			Assert.That(
				schedule.ReadDueState(OperationKindCode.WearPartReplacement), Is.EqualTo("Due"));

			schedule.DeferOperation(
				OperationKindCode.WearPartReplacement, DeferralReasonCode.SecondOpinion,
				TheSecondWindowEnds);

			Assert.That(
				schedule.ReadDueState(OperationKindCode.WearPartReplacement), Is.EqualTo("Due"),
				"Rechazado quiere decir que la operacion se queda donde estaba: vencida. Si "
				+ "quedara Deferred, el tercer aplazamiento habria pasado sin contarse.");
			Assert.That(
				schedule.DeferralsUsed(OperationKindCode.WearPartReplacement), Is.EqualTo(2),
				"Ni sube a 3 ni se resetea: DeferralsUsed cuenta los ADMITIDOS (E35).");
		}

		[Test]
		public void ADeferralRefusedAtTheCapIsAStateAndNotAnException()
		{
			schedule.Evaluate(FellDue);
			schedule.DeferOperation(
				OperationKindCode.WearPartReplacement, DeferralReasonCode.Cost, FellDue);
			schedule.Evaluate(TheWindowEnds);
			schedule.DeferOperation(
				OperationKindCode.WearPartReplacement, DeferralReasonCode.Timing, TheWindowEnds);
			schedule.Evaluate(TheSecondWindowEnds);

			Assert.DoesNotThrow(() => schedule.DeferOperation(
				OperationKindCode.WearPartReplacement, DeferralReasonCode.Cost,
				TheSecondWindowEnds),
				"E32: llegar al tope es un RESULTADO ESPERADO -- la politica da dos y la persona "
				+ "pidio tres -- y un throw obligaria a la app a usar excepciones para el flujo "
				+ "normal de alguien que simplemente no puede ahora.");
		}

		// OINV-28 journaliza el aplazamiento CON SU MOTIVO: lo que la persona decidio es la mitad
		// del hecho. La fachada recibe un codigo del wire, lo resuelve y se lo pasa a la
		// operacion; esta prueba existe porque, sin ella, la fachada podia resolver el codigo y
		// despues pasar cualquier otro motivo -- verde en 284 pruebas, y el registro diria que la
		// persona aplazo por costo cuando en realidad queria una segunda opinion.
		[Test]
		public void TheReasonThePersonGaveIsTheReasonThatGetsRecorded()
		{
			schedule.Evaluate(FellDue);
			schedule.DeferOperation(
				OperationKindCode.WearPartReplacement, DeferralReasonCode.SecondOpinion, FellDue);

			Assert.That(
				schedule.ReadDueReason(OperationKindCode.WearPartReplacement),
				Does.Contain("una segunda opinión"));
		}

		// PRUEBA 10 de §9: una operacion clasificada de seguridad rechaza el PRIMER intento, y
		// con el tope entero. No es un tope mas bajo: es que la puerta no existe (OINV-52).
		[Test]
		public void ASafetyClassifiedOperationRefusesTheFirstDeferral()
		{
			MaintenanceSchedule withSafety = new MaintenanceSchedule(
				TheVin, UsageProfileCode.Standard);
			schedule = withSafety;

			SeedAWearPartOf(true);
			withSafety.RecordReading(
				40000, DistanceUnitCode.UnitA, FellDue, ReadingSourceCode.Telemetry);
			withSafety.Evaluate(FellDue);

			Assert.That(
				withSafety.ReadDueState(OperationKindCode.WearPartReplacement), Is.EqualTo("Due"));

			withSafety.DeferOperation(
				OperationKindCode.WearPartReplacement, DeferralReasonCode.Cost, FellDue);

			Assert.That(
				withSafety.ReadDueState(OperationKindCode.WearPartReplacement), Is.EqualTo("Due"));
			Assert.That(
				withSafety.DeferralsUsed(OperationKindCode.WearPartReplacement), Is.EqualTo(0),
				"Y el intento rechazado no gasta un aplazamiento: si contara, la clasificacion de "
				+ "seguridad se leeria como 'cero de dos usados' en vez de 'ninguno'.");
		}

		private void SeedAWearPartOf(bool isSafety)
		{
			schedule.SeedPlanOperation(
				OperationKindCode.WearPartReplacement, IntervalBasisCode.DistanceOnly,
				0, TenThousandUnits, DistanceUnitCode.UnitA,
				true, isSafety,
				Delivery, OdometerAtDelivery, DistanceUnitCode.UnitA);
		}
	}
}
