using System;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LEY L8, UsageAdjustsLengthNotBasis, sobre MaintenanceSchedule (§5 del slice, OINV-22):
	// "Un perfil de uso escala el LARGO de los intervalos por un factor declarado. Nunca cambia
	// la base de una operacion ni la quita."
	//
	// Es la prueba 14 de §9, y trae adentro OINV-15: "los cambios de UsageProfile rigen desde el
	// instante del cambio HACIA ADELANTE. Nunca re-fechan retroactivamente una operacion
	// completada", con su columna de rechazo: "rechazar una fecha de vigencia retroactiva".
	//
	// LO QUE HACE FALSIFICABLE "HACIA ADELANTE" es evaluar un instante ANTERIOR a la vigencia
	// DESPUES de haber declarado. Si el agregado guardara solo "el perfil actual", la respuesta
	// seria la del perfil nuevo y nadie lo notaria: los dos contestan lo mismo en casi todos los
	// instantes. Por eso los numeros de abajo estan elegidos para que los dos perfiles NO
	// contesten lo mismo -- 238 dias son NotDue con Standard y DueSoon con SevereDuty.
	//
	// SevereDuty = 70% del largo declarado: 365 dias pasan a 256 y 10000 unidades a 7000.
	[TestFixture]
	public sealed class TheUsageProfileTakesEffectGoingForward
	{
		private const string TheVin = "1HGCM82633A004352";
		private const int TwelveMonthsInDays = 365;
		private const int TenThousandUnits = 10000;

		private static readonly DateTime Delivery = new DateTime(2025, 6, 1);
		private const int OdometerAtDelivery = 30000;

		// 2025-06-01 + 238 dias. Con Standard faltan 127 dias del limite; con SevereDuty ya se
		// paso el anticipo de cortesia de 30 sobre un limite de 256.
		private static readonly DateTime BeforeTheProfileTakesEffect = new DateTime(2026, 1, 25);
		private static readonly DateTime TheProfileTakesEffect = new DateTime(2026, 2, 1);

		// 2025-06-01 + 259 dias: pasado el limite escalado de 256, dentro de la gracia de 14.
		private static readonly DateTime AfterTheProfileTookEffect = new DateTime(2026, 2, 15);

		private MaintenanceSchedule schedule;

		[SetUp]
		public void Setup()
		{
			schedule = new MaintenanceSchedule(TheVin, UsageProfileCode.Standard);
			SeedTheFluidChange();

			schedule.RecordReading(
				33000, DistanceUnitCode.UnitA, Delivery.AddDays(30), ReadingSourceCode.Telemetry);
		}

		// PRUEBA 14 de §9, la mitad de OINV-15: declarado y todo, antes de su instante rige el
		// perfil anterior.
		[Test]
		public void BeforeItsInstantTheProfileInForceIsStillTheOldOne()
		{
			schedule.DeclareUsageProfile(UsageProfileCode.SevereDuty, TheProfileTakesEffect);

			schedule.Evaluate(BeforeTheProfileTakesEffect);

			Assert.That(schedule.ReadDueState(OperationKindCode.FluidChange), Is.EqualTo("NotDue"),
				"238 dias de 365 es NotDue. Con el factor de SevereDuty el limite seria 256 y "
				+ "esto seria DueSoon: si el agregado guardara solo 'el perfil actual', la "
				+ "declaracion habria re-fechado hacia atras un instante que rige el viejo.");
		}

		// PRUEBA 14 de §9, la mitad del escalamiento: desde su instante, los largos se escalan.
		[Test]
		public void FromItsInstantForwardTheDeclaredFactorScalesTheLength()
		{
			schedule.DeclareUsageProfile(UsageProfileCode.SevereDuty, TheProfileTakesEffect);

			schedule.Evaluate(AfterTheProfileTookEffect);

			Assert.That(schedule.ReadDueState(OperationKindCode.FluidChange), Is.EqualTo("Due"),
				"259 dias contra el limite escalado de 256. Sin escalar serian 259 de 365 y la "
				+ "operacion no venceria hasta junio.");
		}

		// El umbral es INCLUSIVO, con la misma regla que los otros del slice: alcanzar un umbral
		// es entrar en su estado (C16). El dia de la vigencia ya rige el perfil nuevo.
		[Test]
		public void TheProfileIsInForceOnTheVeryInstantItTakesEffect()
		{
			schedule.DeclareUsageProfile(
				UsageProfileCode.SevereDuty, AfterTheProfileTookEffect);

			schedule.Evaluate(AfterTheProfileTookEffect);

			Assert.That(schedule.ReadDueState(OperationKindCode.FluidChange), Is.EqualTo("Due"),
				"259 dias contra el limite escalado de 256. Con el umbral exclusivo la vigencia "
				+ "empezaria al dia siguiente y esto seria NotDue.");
		}

		// El perfil con que se CREO la agenda rige hasta que se declare otro. Sin esto, un
		// vehiculo comercial evaluado antes de su primera declaracion se mediria con los
		// intervalos de un uso normal, que son mas largos.
		[Test]
		public void TheProfileTheScheduleWasCreatedWithRulesUntilOneIsDeclared()
		{
			MaintenanceSchedule severeFromTheStart = new MaintenanceSchedule(
				TheVin, UsageProfileCode.SevereDuty);

			severeFromTheStart.SeedPlanOperation(
				OperationKindCode.FluidChange, IntervalBasisCode.TimeOrDistance,
				TwelveMonthsInDays, TenThousandUnits, DistanceUnitCode.UnitA,
				true, false,
				Delivery, OdometerAtDelivery, DistanceUnitCode.UnitA);

			severeFromTheStart.Evaluate(AfterTheProfileTookEffect);

			Assert.That(
				severeFromTheStart.ReadDueState(OperationKindCode.FluidChange), Is.EqualTo("Due"),
				"259 dias contra el limite escalado de 256. Si el perfil de la creacion se "
				+ "ignorara, serian 259 de 365 y la operacion no venceria hasta junio.");
		}

		// "Nunca cambia la BASE": la operacion sigue siendo de dos dimensiones y el motivo sigue
		// nombrando las dos. Un perfil que cambiara la base la volveria otra operacion.
		[Test]
		public void TheProfileScalesTheLengthAndLeavesTheBasisWhereItWas()
		{
			schedule.DeclareUsageProfile(UsageProfileCode.SevereDuty, TheProfileTakesEffect);
			schedule.Evaluate(AfterTheProfileTookEffect);

			string reason = schedule.ReadDueReason(OperationKindCode.FluidChange);

			Assert.That(reason, Does.Contain("tiempo:"));
			Assert.That(reason, Does.Contain("distancia:"),
				"Las dos dimensiones siguen ahi. Si SevereDuty hubiera tocado la base, esta "
				+ "operacion habria dejado de ser TimeOrDistance sin que nadie lo pidiera.");
			Assert.That(reason, Does.Contain("256"),
				"Y el limite que se compara es el ESCALADO, no el declarado.");
		}

		// LowDistance no escala la distancia: la IGNORA (§6). Escalar por cero la haria vencer en
		// el acto, que es lo contrario de lo que el perfil dice.
		[Test]
		public void LowDistanceIgnoresTheDistanceLimitInsteadOfShrinkingIt()
		{
			schedule.DeclareUsageProfile(UsageProfileCode.LowDistance, TheProfileTakesEffect);

			schedule.RecordReading(
				45000, DistanceUnitCode.UnitA, AfterTheProfileTookEffect,
				ReadingSourceCode.Telemetry);
			schedule.Evaluate(AfterTheProfileTookEffect);

			Assert.That(schedule.ReadDueState(OperationKindCode.FluidChange), Is.EqualTo("NotDue"),
				"15000 unidades recorridas de un limite de 10000, y la operacion NO vence: con "
				+ "LowDistance la dimension de distancia no se evalua. Los 259 dias de 365 "
				+ "tampoco alcanzan, porque este perfil deja el tiempo intacto.");
			Assert.That(
				schedule.ReadDueReason(OperationKindCode.FluidChange),
				Does.Contain("de poca distancia"),
				"Y lo dice: OHS-07 exige poder mostrar cual perfil aplico.");
		}

		// OINV-15, columna de rechazo: una fecha de vigencia retroactiva se RECHAZA. Aca si
		// revienta, y la asimetria con L9 es la de E32: quitar lo que no es suyo es la persona
		// tocando un boton; declarar un perfil con fecha anterior a la que rige es la app
		// mandando un hecho que re-escribe el pasado.
		[Test]
		public void ARetroactiveEffectiveDateIsRejected()
		{
			schedule.DeclareUsageProfile(UsageProfileCode.SevereDuty, TheProfileTakesEffect);

			Assert.Throws<DomainException>(() => schedule.DeclareUsageProfile(
				UsageProfileCode.Commercial, TheProfileTakesEffect.AddDays(-1)));
		}

		[Test]
		public void AProfileCannotBeDeclaredBeforeAnInstantAlreadyEvaluated()
		{
			schedule.Evaluate(AfterTheProfileTookEffect);

			Assert.Throws<DomainException>(() => schedule.DeclareUsageProfile(
				UsageProfileCode.SevereDuty, AfterTheProfileTookEffect.AddDays(-1)),
				"Ese instante ya se evaluo con el perfil que regia entonces, y lo que la "
				+ "evaluacion decidio ya es un hecho de la agenda.");
		}

		[Test]
		public void AnUnknownUsageProfileIsACallerBug()
		{
			Assert.Throws<DomainException>(
				() => schedule.DeclareUsageProfile((UsageProfileCode)99, TheProfileTakesEffect));
		}

		// PRUEBA 14 de §9, la cola: "una operacion ya completada no se re-fecha". El ancla de la
		// sucesora es la completacion REAL (ley L4) y el perfil no la mueve -- solo escala el
		// largo que se cuenta DESDE ella. 39800 + 7000 = 46800, no 39800 + 10000 ni 40000 + 7000.
		[Test]
		public void AProfileDeclaredAfterACompletionDoesNotReDateIt()
		{
			MaintenanceSchedule completed = new MaintenanceSchedule(TheVin, UsageProfileCode.Standard);

			completed.SeedPlanOperation(
				OperationKindCode.FluidChange, IntervalBasisCode.TimeOrDistance,
				TwelveMonthsInDays, TenThousandUnits, DistanceUnitCode.UnitA,
				true, false,
				Delivery, OdometerAtDelivery, DistanceUnitCode.UnitA);

			completed.CompleteOperation(
				OperationKindCode.FluidChange, TheProfileTakesEffect, 39800,
				DistanceUnitCode.UnitA);
			completed.DeclareUsageProfile(UsageProfileCode.SevereDuty, TheProfileTakesEffect);

			completed.RecordReading(
				46799, DistanceUnitCode.UnitA, TheProfileTakesEffect.AddDays(28),
				ReadingSourceCode.Telemetry);
			completed.Evaluate(TheProfileTakesEffect.AddDays(28));

			Assert.That(
				completed.ReadDueState(OperationKindCode.FluidChange), Is.EqualTo("DueSoon"),
				"6999 unidades del limite escalado de 7000: una unidad antes.");

			completed.RecordReading(
				46800, DistanceUnitCode.UnitA, TheProfileTakesEffect.AddDays(29),
				ReadingSourceCode.Telemetry);
			completed.Evaluate(TheProfileTakesEffect.AddDays(29));

			Assert.That(completed.ReadDueState(OperationKindCode.FluidChange), Is.EqualTo("Due"),
				"46800 son exactamente 7000 desde 39800, que es donde la operacion se completo "
				+ "DE VERDAD. Si el perfil hubiera re-fechado la completacion a la grilla "
				+ "teorica de 40000, la sucesora venceria en 47000.");
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
