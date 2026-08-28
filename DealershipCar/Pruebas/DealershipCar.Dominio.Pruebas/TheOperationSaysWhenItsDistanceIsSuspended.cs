using System;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LEY L7, UnitMismatchSuspendsDistance, sobre ScheduledOperation (§5 del slice, OINV-21):
	// "Una lectura en una unidad distinta a la del limite SUSPENDE la evaluacion de distancia
	// para esa operacion y la deja cayendo a solo-tiempo. Nunca se convierte."
	//
	// LA CUENTA ya es de la regla y se fijo en T3 (AUnitMismatchSuspendsTheDistance). Lo que
	// le toca a la OCURRENCIA es la otra mitad de la ley, la que la prueba 7 de §9 pide
	// explicitamente: que la discrepancia sea VISIBLE. Sin eso, "no vencio" es indistinguible
	// de "no lo pude saber", y una operacion que nadie puede medir se ve exactamente igual que
	// una que esta al dia.
	//
	// LA SUSPENSION ES DERIVADA, NO GUARDADA (GAP G4), y aca se prueba un piso mas arriba que
	// en T3: no es que la REGLA la recalcule, es que la OPERACION no la latchea. Llega una
	// lectura en la unidad del plan, la operacion se evalua, y la suspension desaparece sin
	// que nadie la levante.
	[TestFixture]
	public sealed class TheOperationSaysWhenItsDistanceIsSuspended
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
				MaintenanceOperationKind.WearPartReplacement,
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
		public void AReadingInTheOtherUnitLeavesTheOperationOnTimeAlone()
		{
			EvaluateWith(45000, DistanceUnit.UnitB);

			Assert.That(operation.State, Is.SameAs(DueState.NotDue),
				"15000 unidades pasarian el limite de 10000 con holgura, pero estan en la otra "
				+ "unidad. Si la operacion vence, alguien resto dos numeros en unidades "
				+ "distintas -- y no hace falta un Convert para eso, basta con no mirar.");
			Assert.That(operation.UnitMismatchSuspendsDistance(), Is.True);
		}

		[Test]
		public void TheOperationSaysWhyItIsNotComparingTheDistance()
		{
			EvaluateWith(45000, DistanceUnit.UnitB);

			string reason = operation.Describe();

			Assert.That(reason, Does.Contain("cannot be evaluated"));
			Assert.That(reason, Does.Contain("the limit is in km"));
			Assert.That(reason, Does.Contain("the measurements are in mi"));
			Assert.That(reason, Does.Contain("never converted"),
				"OUS-07: la persona quiere saber POR QUE, no solo QUE. Un 'no vencio' a secas "
				+ "sobre una dimension que el sistema no puede mirar es una operacion que "
				+ "parece al dia sin estarlo.");
		}

		// GAP G4 a nivel de ocurrencia: la suspension se cura sola.
		[Test]
		public void TheSuspensionHealsItselfOnTheNextEvaluation()
		{
			EvaluateWith(45000, DistanceUnit.UnitB);

			Assert.That(operation.UnitMismatchSuspendsDistance(), Is.True);

			EvaluateWith(45000, DistanceUnit.UnitA);

			Assert.That(operation.UnitMismatchSuspendsDistance(), Is.False,
				"Si la operacion hubiera GUARDADO la suspension en vez de derivarla de su "
				+ "ultima evaluacion, alguien tendria que acordarse de apagarla. El dia que se "
				+ "olvide, la distancia queda muerta para esa operacion para siempre.");
			Assert.That(operation.State, Is.SameAs(DueState.Overdue));
		}

		// Una operacion que nunca se evaluo no tiene suspension que reportar, y lo dice en
		// lugar de inventar un estado. La ausencia de evaluacion es un hecho, no un NotDue.
		[Test]
		public void AnOperationThatWasNeverEvaluatedSaysExactlyThat()
		{
			Assert.That(operation.UnitMismatchSuspendsDistance(), Is.False);
			Assert.That(operation.Describe(), Does.Contain("not been evaluated"),
				"Un motivo vacio, o uno copiado de una evaluacion que nunca ocurrio, es la "
				+ "misma mentira que el NotDue del GAP G2: parece una respuesta y no lo es.");
		}

		// El motivo de una operacion aplazada es el aplazamiento, no la ultima cuenta de la
		// ley: lo que la persona necesita saber es hasta cuando y cuantos le quedan (OUS-12).
		[Test]
		public void ADeferredOperationSaysUntilWhenAndHowManyAreLeft()
		{
			EvaluateWith(40000, DistanceUnit.UnitA);
			operation.Defer(DeferralReason.Cost, Observed);

			string reason = operation.Describe();

			Assert.That(reason, Does.StartWith("Deferred"));
			Assert.That(reason, Does.Contain("2026-03-21"), "El fin de la ventana.");
			Assert.That(reason, Does.Contain("Cost"));
			Assert.That(reason, Does.Contain("1 of 2"));
		}

		private void EvaluateWith(int reading, DistanceUnit unit)
		{
			operation.Evaluate(
				Observed,
				UsableReading.Of(new OdometerReading(
					reading, unit, Observed, ReadingSource.Telemetry)),
				noScaling);
		}
	}
}
