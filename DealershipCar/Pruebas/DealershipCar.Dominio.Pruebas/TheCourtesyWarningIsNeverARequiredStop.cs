using System;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LEY L10, DueSoonIsACourtesy, sobre ScheduledOperation (§5 del slice, OINV-23, OEX-06):
	// "DueSoon es un estado de cortesia, nunca una parada obligatoria. Una operacion cuyo
	// vencimiento se alcanza en la misma evaluacion va DERECHO a Due."
	//
	// Es la prueba 6 de §9, la mitad que le toca a la OCURRENCIA. La mitad de la regla ya se
	// fijo en T3 (TheFirstDimensionToArriveDecides): Assess nunca contesta DueSoon con el
	// limite alcanzado. Lo que se fija aca es lo otro que dice OEX-06 -- "UN solo cambio de
	// estado, al estado mas avanzado" -- y eso es de la ocurrencia, porque es la que tiene
	// estado que cambiar y la que reporta la transicion que el host cuenta.
	//
	// EL DEFECTO QUE ESTO ATRAPA es el de leer el diagrama de estados como un camino
	// obligatorio: NotDue -> DueSoon -> Due, un paso por evaluacion. Esa implementacion es
	// correcta en el estado final y MIENTE en el conteo -- una operacion que se salto el
	// aviso reportaria dos transiciones, y con la cadencia de C6 eso son dos entradas de
	// journal por un solo hecho del negocio.
	[TestFixture]
	public sealed class TheCourtesyWarningIsNeverARequiredStop
	{
		private const int TwelveMonthsInDays = 365;
		private const int TenThousandUnits = 10000;

		private static readonly DateTime LastCompletion = new DateTime(2025, 6, 1);
		private const int ReadingAtTheLastCompletion = 30000;

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

		// PRUEBA 6 de §9. 40200 esta a la vez dentro del anticipo y pasado el limite. Desde
		// NotDue, UNA evaluacion, UNA transicion, y el destino es Due.
		[Test]
		public void AnOperationThatReachesItsDuePointGoesStraightToDue()
		{
			Assert.That(operation.State, Is.SameAs(DueState.NotDue),
				"Nace NotDue: es la genesis de la tabla de transiciones.");

			DueStateTransition transition = EvaluateAt(
				new DateTime(2026, 1, 20), 40200);

			Assert.That(operation.State, Is.SameAs(DueState.Due));
			Assert.That(operation.State, Is.Not.SameAs(DueState.DueSoon),
				"El aviso es cortesia. Si aparece aca, la operacion esta obligando a una parada "
				+ "que la ley no pide.");
			Assert.That(transition.Happened, Is.True);
			Assert.That(transition.BecameDue, Is.True,
				"El host tiene que poder decir 'algo vencio'. Si la transicion solo dijera que "
				+ "hubo cambio, saltarse el aviso se veria igual que entrar en el.");
		}

		// LA CORTESIA SI SE EMITE CUANDO ES LA RESPUESTA CORRECTA. Sin esto, "nunca es una
		// parada obligatoria" se podria satisfacer no emitiendola nunca, que es otra cosa.
		[Test]
		public void TheWarningIsStillEmittedWhenItIsTheRightAnswer()
		{
			DueStateTransition transition = EvaluateAt(new DateTime(2026, 1, 15), 39000);

			Assert.That(operation.State, Is.SameAs(DueState.DueSoon),
				"9000 de 10000, con un anticipo de 1000: esto es exactamente el aviso.");
			Assert.That(transition.Happened, Is.True);
			Assert.That(transition.BecameDue, Is.False,
				"Avisar no es vencer. Si BecameDue fuera true aca, el host contaria como "
				+ "vencida una operacion que todavia no lo esta.");
		}

		[Test]
		public void TheWarningIsOneTransitionAndTheDueDateIsAnother()
		{
			DueStateTransition toTheWarning = EvaluateAt(new DateTime(2026, 1, 15), 39000);
			DueStateTransition toTheDueDate = EvaluateAt(new DateTime(2026, 1, 20), 40000);

			Assert.That(toTheWarning.Happened, Is.True);
			Assert.That(toTheDueDate.Happened, Is.True);
			Assert.That(operation.State, Is.SameAs(DueState.Due));
			Assert.That(toTheDueDate.BecameDue, Is.True);
		}

		// Evaluar de nuevo con los mismos hechos no mueve nada, y decirlo importa: es sobre
		// esta respuesta que se apoya la cadencia de Evaluate (C6). Una transicion inventada
		// por evaluacion seria una entrada de journal por cada vez que la app se abre.
		[Test]
		public void EvaluatingTwiceWithTheSameFactsReportsNoSecondTransition()
		{
			EvaluateAt(new DateTime(2026, 1, 20), 40200);

			DueStateTransition again = EvaluateAt(new DateTime(2026, 1, 20), 40200);

			Assert.That(again.Happened, Is.False);
			Assert.That(again.BecameDue, Is.False);
			Assert.That(operation.State, Is.SameAs(DueState.Due));
		}

		// Pasar de golpe de NotDue a Overdue -- la app estuvo meses sin abrirse -- es UNA
		// transicion y reporta LAS DOS cosas: que vencio y que ademas se paso.
		[Test]
		public void JumpingStraightToOverdueIsOneTransitionThatReportsBoth()
		{
			DueStateTransition transition = EvaluateAt(new DateTime(2026, 1, 20), 40500);

			Assert.That(operation.State, Is.SameAs(DueState.Overdue));
			Assert.That(transition.Happened, Is.True);
			Assert.That(transition.BecameDue, Is.True,
				"Overdue es vencida y ademas pasada. Si BecameDue fuera false, la operacion "
				+ "habria vencido sin que el host se enterara nunca.");
			Assert.That(transition.BecameOverdue, Is.True);
		}

		private DueStateTransition EvaluateAt(DateTime asOf, int reading)
		{
			return operation.Evaluate(
				asOf,
				UsableReading.Of(new OdometerReading(
					reading, DistanceUnit.UnitA, asOf, ReadingSource.Telemetry)),
				noScaling);
		}
	}
}
