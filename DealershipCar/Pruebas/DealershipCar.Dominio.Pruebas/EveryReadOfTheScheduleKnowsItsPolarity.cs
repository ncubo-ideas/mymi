using System;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LA POLARIDAD PARTIDA DE E41, sobre las ocho lecturas de §4. Es la doctrina que se equivoca
	// sola, porque las tres polaridades viven a dos lineas una de otra:
	//
	//   TOTAL           una lectura CON CLAVE sobre un espacio contesta siempre. Una clase que no
	//                   esta agendada NO ESTA VENCIDA -- la ausencia es una respuesta legal.
	//   GUARDADA        una lectura NULARIA sobre una opcional revienta cuando no hay. "Nada
	//                   vencido" no es un vencimiento vacio: es la ausencia de la cosa que se
	//                   pide, y se pregunta antes con HasAnythingDue.
	//   RESUELVE Y      un codigo del wire que no nombra ningun miembro del conjunto cerrado es
	//   REVIENTA        un defecto de quien llama, no una respuesta vacia. Es la polaridad
	//                   OPUESTA a la primera, sobre el mismo parametro.
	//
	// §12 pide que ReadDueState se AFIRME devolviendo la respuesta vacia para una clase no
	// agendada, y que ReadNextDueKind se afirme reventando. Estan las dos aca.
	[TestFixture]
	public sealed class EveryReadOfTheScheduleKnowsItsPolarity
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

		// §12, literal: "ReadDueState devuelve la respuesta vacia para una clase no agendada --
		// AFIRMADO, no supuesto".
		[Test]
		public void AnUnscheduledKindIsNotDueAndThatIsALegalAnswer()
		{
			Assert.That(schedule.ReadDueState(OperationKindCode.FilterChange), Is.EqualTo("NotDue"),
				"§8: 'una clase sin operacion agendada NO ESTA VENCIDA; la ausencia es una "
				+ "respuesta legal'. Es la misma polaridad con que una celda fuera del tablero "
				+ "contesta 'no ocupada' y no revienta (E41).");

			Assert.DoesNotThrow(() => schedule.ReadDueState(OperationKindCode.FilterChange));
		}

		[Test]
		public void AnUnscheduledKindHasNoDeferralsUsed()
		{
			Assert.That(schedule.DeferralsUsed(OperationKindCode.FilterChange), Is.EqualTo(0));
		}

		// El motivo es total tambien, pero con una FRASE de verdad y no con una cadena vacia: es
		// la unica lectura que distingue "no esta agendada" de "esta agendada y no vencio", y §8
		// dice que componerla es conocimiento del dominio y no de la vista (OUS-07).
		[Test]
		public void AnUnscheduledKindSaysThereIsNothingOfThatKindOnTheSchedule()
		{
			string reason = schedule.ReadDueReason(OperationKindCode.FilterChange);

			Assert.That(reason, Does.Contain("no scheduled"));
			Assert.That(reason, Does.Contain("FilterChange"),
				"Y nombra la clase: una frase generica obligaria a la vista a saber sobre que "
				+ "clase estaba preguntando.");
		}

		// §12, literal: "ReadNextDueKind revienta cuando no hay nada vencido, y HasAnythingDue
		// existe".
		[Test]
		public void WithNothingDueThePresenceQuestionAnswersAndTheGuardedReadThrows()
		{
			SeedTheFluidChange();
			schedule.Evaluate(Delivery);

			Assert.That(schedule.HasAnythingDue(), Is.False);
			Assert.Throws<DomainException>(() => schedule.ReadNextDueKind(),
				"Nada vencido es la AUSENCIA de la cosa que se lee, no una clase vacia. "
				+ "Devolver \"\" aca dejaria a un llamador que no pregunto siguiendo con una "
				+ "ficcion, y el hecho 'no hay nada que hacer' se perderia.");
		}

		[Test]
		public void ThePresenceQuestionItselfIsTotalOnAnEmptySchedule()
		{
			Assert.That(schedule.HasAnythingDue(), Is.False);
			Assert.That(schedule.CountDue(), Is.EqualTo(0));
			Assert.That(schedule.CountOverdue(), Is.EqualTo(0));
			Assert.That(schedule.MissedCount(), Is.EqualTo(0));
		}

		// La siguiente vencida es la MAS AVANZADA: una pasada de la gracia va antes que una
		// vencida, aunque se haya agendado despues.
		[Test]
		public void TheNextDueKindIsTheMostAdvancedOne()
		{
			SeedTheFluidChange();
			SeedAFilterChangeOf(9000);

			ReachTheLimit();

			Assert.That(schedule.ReadDueState(OperationKindCode.FluidChange), Is.EqualTo("Due"));
			Assert.That(
				schedule.ReadDueState(OperationKindCode.FilterChange), Is.EqualTo("Overdue"));

			Assert.That(schedule.ReadNextDueKind(), Is.EqualTo("FilterChange"),
				"Se sembro segunda y va primera: lo que ordena es cuanto se paso, no en que "
				+ "orden entro.");
		}

		// EL DESEMPATE ES EL ORDEN EN QUE ENTRARON A LA AGENDA, y tiene que ser un hecho del
		// agregado y no un detalle de un Dictionary: la prueba 16 exige que dos corridas de la
		// misma secuencia contesten lo mismo, y un orden de enumeracion que nadie prometio no
		// sirve para eso. Las dos agendas de abajo tienen las mismas dos operaciones, empatadas,
		// y contestan distinto SOLO por el orden en que se sembraron.
		[Test]
		public void AmongEquallyStandingOperationsTheOneScheduledFirstIsNext()
		{
			SeedTheFluidChange();
			SeedAFilterChangeOf(TenThousandUnits);
			ReachTheLimit();

			Assert.That(schedule.ReadNextDueKind(), Is.EqualTo("FluidChange"));

			MaintenanceSchedule theOtherOrder =
				new MaintenanceSchedule(TheVin, UsageProfileCode.Standard);
			schedule = theOtherOrder;

			SeedAFilterChangeOf(TenThousandUnits);
			SeedTheFluidChange();
			ReachTheLimit();

			Assert.That(theOtherOrder.ReadNextDueKind(), Is.EqualTo("FilterChange"),
				"Mismas dos operaciones, mismo empate, respuesta distinta. Si el desempate "
				+ "fuera el valor del enum -- FluidChange antes que FilterChange -- las dos "
				+ "agendas contestarian FluidChange, y renumerar el enum cambiaria la "
				+ "respuesta sin tocar una linea del dominio (C12).");
		}

		// Un aplazamiento saca la operacion de la cuenta de vencidas: la persona ya decidio, asi
		// que no es lo siguiente que hay que hacer.
		[Test]
		public void ADeferredOperationIsNotWhatComesNext()
		{
			SeedTheFluidChange();
			SeedAFilterChangeOf(TenThousandUnits);
			ReachTheLimit();

			schedule.DeferOperation(
				OperationKindCode.FluidChange, DeferralReasonCode.Timing, TheLimitIsReached);

			Assert.That(schedule.ReadNextDueKind(), Is.EqualTo("FilterChange"));
		}

		// La polaridad OPUESTA, sobre el mismo parametro: un codigo que no nombra ningun miembro
		// del conjunto cerrado revienta en TODAS las lecturas con clave. Afuera de un rango hay
		// respuesta vacia; afuera de un conjunto cerrado no hay miembro (E32, E41).
		[Test]
		public void AnUnknownKindIsACallerBugOnEveryKeyedRead()
		{
			Assert.Throws<DomainException>(() => schedule.ReadDueState((OperationKindCode)99));
			Assert.Throws<DomainException>(() => schedule.DeferralsUsed((OperationKindCode)99));
			Assert.Throws<DomainException>(() => schedule.ReadDueReason((OperationKindCode)99));
		}

		// Una operacion agendada y todavia no evaluada tampoco miente: no dice NotDue a secas,
		// dice que nadie la evaluo (T4). La fachada lo deja pasar tal cual.
		[Test]
		public void AScheduledButNeverEvaluatedOperationSaysItHasNotBeenEvaluated()
		{
			SeedTheFluidChange();

			Assert.That(
				schedule.ReadDueReason(OperationKindCode.FluidChange),
				Does.Contain("has not been evaluated yet"));
		}

		private void SeedTheFluidChange()
		{
			schedule.SeedPlanOperation(
				OperationKindCode.FluidChange, IntervalBasisCode.TimeOrDistance,
				TwelveMonthsInDays, TenThousandUnits, DistanceUnitCode.UnitA,
				true, false,
				Delivery, OdometerAtDelivery, DistanceUnitCode.UnitA);
		}

		private void SeedAFilterChangeOf(int distanceLimit)
		{
			schedule.SeedPlanOperation(
				OperationKindCode.FilterChange, IntervalBasisCode.DistanceOnly,
				0, distanceLimit, DistanceUnitCode.UnitA,
				true, false,
				Delivery, OdometerAtDelivery, DistanceUnitCode.UnitA);
		}

		private void ReachTheLimit()
		{
			schedule.RecordReading(
				40000, DistanceUnitCode.UnitA, TheLimitIsReached, ReadingSourceCode.Telemetry);
			schedule.Evaluate(TheLimitIsReached);
		}
	}
}
