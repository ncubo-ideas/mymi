using System;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// PRUEBA 5 de §9 EN EL NIVEL B (ley L4, OINV-26): "vencida en 40000; completada el 2026-02-01
	// en 39800 -> esa instancia Completed, y la nueva vence en 49800 o el 2027-02-01. No en 50000."
	//
	// El numero que hace la prueba falsificable es 49800 y no 50000: con la grilla teorica el error
	// se acumula hacia adelante y nunca se corrige solo.
	//
	// LO QUE ESTE NIVEL AGREGA es que la completacion viaje entera por el journal -- instante,
	// lectura y unidad -- y que la sucesora salga del REPLAY con el mismo vencimiento. Un ancla que
	// se pierda al rehidratar deja a la persona con una operacion que vuelve a vencer donde vencia
	// antes de haberla hecho.
	[TestFixture]
	public sealed class AJournaledScheduleRebasesFromTheActualCompletion
	{
		private const string TheVin = "1HGCM82633A004352";
		private const int TwelveMonthsInDays = 365;
		private const int TenThousandUnits = 10000;

		private static readonly DateTime Delivery =
			new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);
		private const int OdometerAtDelivery = 30000;

		private static readonly DateTime FellDue =
			new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc);
		private static readonly DateTime CompletedOn =
			new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
		private const int OdometerAtTheActualCompletion = 39800;

		private AScheduleOnARealJournal journal;

		[SetUp]
		public void Setup()
		{
			journal = new AScheduleOnARealJournal(TheVin, "Standard");

			journal.Api.SeedPlanOperation(
				"FluidChange", "TimeOrDistance", TwelveMonthsInDays, TenThousandUnits, "UnitA",
				true, false,
				Delivery, OdometerAtDelivery, "UnitA");

			journal.Api.RecordReading(40000, "UnitA", FellDue, "Telemetry");
			journal.Api.Evaluate(FellDue);
		}

		[TearDown]
		public void TearDown()
		{
			journal.Dispose();
		}

		// La clase sigue en la agenda, ocupada por la SUCESORA: hacer una operacion no la borra del
		// mantenimiento del vehiculo, la vuelve a empezar.
		[Test]
		public void TheCompletedInstanceEndsAndItsSuccessorTakesTheKindsSlot()
		{
			Assert.That(journal.Api.ReadDueState("FluidChange"), Is.EqualTo("Due"));

			journal.Api.CompleteOperation(
				"FluidChange", CompletedOn, OdometerAtTheActualCompletion, "UnitA");

			Assert.That(journal.Api.ReadDueState("FluidChange"), Is.EqualTo("NotDue"));
			Assert.That(journal.Api.CountDue(), Is.EqualTo(0));
			Assert.That(journal.Api.MissedCount(), Is.EqualTo(0));
		}

		// La mitad de la distancia: 39800 + 10000 = 49800, y no 40000 + 10000 = 50000.
		[Test]
		public void TheSuccessorFallsDueAtTheActualCompletionPlusTheLimitAndNotAtTheGrid()
		{
			journal.Api.CompleteOperation(
				"FluidChange", CompletedOn, OdometerAtTheActualCompletion, "UnitA");

			journal.Api.RecordReading(49799, "UnitA", TheDay(2026, 6, 1), "Telemetry");
			journal.Api.Evaluate(TheDay(2026, 6, 1));

			Assert.That(journal.Api.ReadDueState("FluidChange"), Is.EqualTo("DueSoon"),
				"Una unidad antes de 49800.");

			journal.Api.RecordReading(49800, "UnitA", TheDay(2026, 6, 2), "Telemetry");
			journal.Api.Evaluate(TheDay(2026, 6, 2));

			Assert.That(journal.Api.ReadDueState("FluidChange"), Is.EqualTo("Due"),
				"49800 son exactamente 10000 desde donde la operacion se hizo DE VERDAD. Con la "
				+ "grilla teorica el limite seria 50000 y venceria 200 unidades tarde.");
		}

		// La otra mitad de la ley L4: el instante tambien se re-ancla.
		[Test]
		public void TheSuccessorAlsoCountsItsTimeFromTheActualCompletionInstant()
		{
			journal.Api.CompleteOperation(
				"FluidChange", CompletedOn, OdometerAtTheActualCompletion, "UnitA");

			journal.Api.Evaluate(TheDay(2026, 6, 1));

			Assert.That(journal.Api.ReadDueState("FluidChange"), Is.EqualTo("NotDue"),
				"El 2026-06-01 era el vencimiento de la instancia VIEJA. La sucesora lleva 120 "
				+ "dias de 365 desde el 2026-02-01.");

			journal.Api.Evaluate(CompletedOn.AddDays(TwelveMonthsInDays));

			Assert.That(journal.Api.ReadDueState("FluidChange"), Is.EqualTo("Due"),
				"365 dias desde la completacion real: el 2027-02-01.");
		}

		// EL RE-ANCLAJE SOBREVIVE AL REPLAY, y esa es la mitad que solo existe en este nivel. La
		// sucesora no esta en ningun campo: sale de reproducir la completacion que quedo
		// journalizada, con su instante, su lectura y su unidad.
		[Test]
		public void TheSuccessorsAnchorSurvivesTheJournal()
		{
			journal.Api.CompleteOperation(
				"FluidChange", CompletedOn, OdometerAtTheActualCompletion, "UnitA");

			journal.CloseAndReopenFromTheJournal();

			journal.Api.RecordReading(49799, "UnitA", TheDay(2026, 6, 1), "Telemetry");
			journal.Api.Evaluate(TheDay(2026, 6, 1));

			Assert.That(journal.Api.ReadDueState("FluidChange"), Is.EqualTo("DueSoon"));

			journal.Api.RecordReading(49800, "UnitA", TheDay(2026, 6, 2), "Telemetry");
			journal.Api.Evaluate(TheDay(2026, 6, 2));

			Assert.That(journal.Api.ReadDueState("FluidChange"), Is.EqualTo("Due"),
				"Reproducida desde el journal, la sucesora vence en el mismo 49800.");
		}

		// LA COMPLETACION TRAE SU UNIDAD, y esa unidad es la del ancla de la sucesora. Fijarla en la
		// puerta dejaria a la sucesora midiendo contra una escala que nadie uso.
		[Test]
		public void ACompletionMeasuredInTheOtherUnitSuspendsTheSuccessorsDistance()
		{
			journal.Api.CompleteOperation(
				"FluidChange", CompletedOn, OdometerAtTheActualCompletion, "UnitB");

			journal.Api.Evaluate(TheDay(2026, 6, 1));

			Assert.That(
				journal.Api.ReadDueReason("FluidChange", TheLanguage.OfTheRecord),
				Does.Contain("las unidades nunca se convierten"),
				"El limite esta en UnitA y el ancla nueva quedo en UnitB.");
			Assert.That(journal.Api.ReadDueState("FluidChange"), Is.EqualTo("NotDue"),
				"Y degrada a solo-tiempo: 120 dias de 365.");
		}

		private static DateTime TheDay(int year, int month, int day)
		{
			return new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);
		}
	}
}
