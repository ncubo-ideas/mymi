using System;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// OINV-54 sobre MaintenanceSchedule: "las senales de odometro obedecen la linea de tiempo de
	// OBSERVACION, no el orden de llegada. Una lectura que llega tarde y calza en la linea es un
	// relleno valido; una que no calza se rechaza."
	//
	// §8 le pide a esta tarea la mitad del ORDEN. La otra mitad -- rechazar la que no calza --
	// se construyo igual, y la razon esta escrita en CONTRADICTIONS C20: T3 dejo dicho, en
	// LimitStanding.OfDistance, que un odometro que va para atras "no es una dimension
	// suspendida, es un hecho imposible, y cual de los dos esta mal lo decide QUIEN ORDENA LAS
	// LECTURAS". Ese es este agregado. Sin la guarda, la lectura entra bien y es EVALUATE el que
	// revienta -- para siempre, hasta que llegue otra lectura -- que es la peor forma de tener
	// razon.
	//
	// LAS DOS UNIDADES SON DOS LINEAS DE TIEMPO. Comparar un valor en UnitA contra uno en UnitB
	// seria convertir por la puerta de atras (OINV-21), asi que la guarda solo compara lecturas
	// que ya estan en la misma unidad.
	[TestFixture]
	public sealed class TheScheduleOrdersItsReadingsByObservationInstant
	{
		private const string TheVin = "1HGCM82633A004352";
		private const int TenThousandUnits = 10000;

		private static readonly DateTime Delivery = new DateTime(2025, 6, 1);
		private const int OdometerAtDelivery = 30000;

		private static readonly DateTime ObservedEarlier = new DateTime(2025, 12, 1);
		private static readonly DateTime ObservedLater = new DateTime(2026, 1, 20);

		private MaintenanceSchedule schedule;

		[SetUp]
		public void Setup()
		{
			schedule = new MaintenanceSchedule(TheVin, UsageProfileCode.Standard);

			schedule.SeedPlanOperation(
				OperationKindCode.WearPartReplacement, IntervalBasisCode.DistanceOnly,
				0, TenThousandUnits, DistanceUnitCode.UnitA,
				true, false,
				Delivery, OdometerAtDelivery, DistanceUnitCode.UnitA);
		}

		// La lectura que manda es la ultima OBSERVADA, aunque haya llegado primero. Aca la de
		// 40000 llega antes que la de 35000 y sigue siendo la que decide, porque se observo
		// despues.
		[Test]
		public void TheReadingThatDecidesIsTheLatestObservedAndNotTheLatestArrived()
		{
			schedule.RecordReading(
				40000, DistanceUnitCode.UnitA, ObservedLater, ReadingSourceCode.Telemetry);
			schedule.RecordReading(
				35000, DistanceUnitCode.UnitA, ObservedEarlier, ReadingSourceCode.HumanEntry);

			schedule.Evaluate(ObservedLater);

			Assert.That(
				schedule.ReadDueState(OperationKindCode.WearPartReplacement), Is.EqualTo("Due"),
				"OINV-54: con el orden de LLEGADA la ultima seria 35000, la operacion llevaria "
				+ "5000 de 10000 y no venceria. La linea que manda es la de observacion.");
		}

		// "As of" quiere decir as of: una lectura observada DESPUES del instante que se evalua
		// todavia no es un hecho de esa evaluacion. Sin esto, Evaluate no seria funcion pura de
		// los hechos recibidos hasta asOf y la prueba 16 no podria significar nada.
		[Test]
		public void AReadingObservedAfterTheInstantEvaluatedIsNotUsedYet()
		{
			schedule.RecordReading(
				40000, DistanceUnitCode.UnitA, ObservedLater, ReadingSourceCode.Telemetry);

			schedule.Evaluate(ObservedLater.AddDays(-1));

			Assert.That(
				schedule.ReadDueState(OperationKindCode.WearPartReplacement), Is.EqualTo("NotDue"));
			Assert.That(
				schedule.ReadDueReason(OperationKindCode.WearPartReplacement),
				Does.Contain("no usable reading"),
				"Y lo dice: ese NotDue NO es evidencia de que la operacion no venza (GAP G2). Es "
				+ "que todavia no se observo nada con que medirla.");

			schedule.Evaluate(ObservedLater);

			Assert.That(
				schedule.ReadDueState(OperationKindCode.WearPartReplacement), Is.EqualTo("Due"));
		}

		[Test]
		public void AReadingLowerThanOneObservedEarlierIsRefused()
		{
			schedule.RecordReading(
				40000, DistanceUnitCode.UnitA, ObservedEarlier, ReadingSourceCode.Telemetry);

			Assert.Throws<DomainException>(() => schedule.RecordReading(
				39000, DistanceUnitCode.UnitA, ObservedLater, ReadingSourceCode.HumanEntry),
				"El odometro no va para atras. Si entrara, la evaluacion siguiente reventaria "
				+ "contra el punto base y nadie sabria por que.");
		}

		// El relleno tardio: una lectura observada ANTES que otra que ya esta, pero mayor que
		// ella, tampoco calza.
		[Test]
		public void ABackfillHigherThanAReadingObservedLaterIsRefused()
		{
			schedule.RecordReading(
				40000, DistanceUnitCode.UnitA, ObservedLater, ReadingSourceCode.Telemetry);

			Assert.Throws<DomainException>(() => schedule.RecordReading(
				41000, DistanceUnitCode.UnitA, ObservedEarlier, ReadingSourceCode.HumanEntry));
		}

		[Test]
		public void ABackfillThatFitsTheTimelineIsAccepted()
		{
			schedule.RecordReading(
				40000, DistanceUnitCode.UnitA, ObservedLater, ReadingSourceCode.Telemetry);

			Assert.DoesNotThrow(() => schedule.RecordReading(
				35000, DistanceUnitCode.UnitA, ObservedEarlier, ReadingSourceCode.HumanEntry),
				"OINV-54 lo llama relleno valido, y lo es: 35000 antes de 40000 es una linea de "
				+ "tiempo coherente.");
		}

		// Dos unidades son dos lineas de tiempo. 500 UnitB despues de 40000 UnitA no es un
		// odometro que retrocedio: son dos escalas que no se comparan (OINV-21, ley L7).
		[Test]
		public void TwoUnitsAreTwoTimelinesAndNeitherJudgesTheOther()
		{
			schedule.RecordReading(
				40000, DistanceUnitCode.UnitA, ObservedEarlier, ReadingSourceCode.Telemetry);

			Assert.DoesNotThrow(() => schedule.RecordReading(
				500, DistanceUnitCode.UnitB, ObservedLater, ReadingSourceCode.Telemetry),
				"Si esto reventara, el agregado estaria comparando UnitA contra UnitB -- que es "
				+ "convertir sin escribir la palabra convertir.");
		}

		// Dos lecturas dentro del MISMO instante observado quedan en orden de llegada, que es el
		// desempate que el journal si conserva: mide en segundos y borra lo de abajo (spike S4).
		[Test]
		public void TwoReadingsObservedInTheSameInstantKeepTheirArrivalOrder()
		{
			schedule.RecordReading(
				35000, DistanceUnitCode.UnitA, ObservedLater, ReadingSourceCode.Telemetry);
			schedule.RecordReading(
				40000, DistanceUnitCode.UnitA, ObservedLater, ReadingSourceCode.Correction);

			schedule.Evaluate(ObservedLater);

			Assert.That(
				schedule.ReadDueState(OperationKindCode.WearPartReplacement), Is.EqualTo("Due"),
				"La que llego ultima dentro del mismo segundo es la ultima de la linea. Sin este "
				+ "desempate, dos lecturas del mismo instante quedarian indistinguibles despues "
				+ "de un replay.");
		}

		[Test]
		public void ANegativeReadingIsRefused()
		{
			Assert.Throws<DomainException>(() => schedule.RecordReading(
				-1, DistanceUnitCode.UnitA, ObservedLater, ReadingSourceCode.HumanEntry));
		}

		[Test]
		public void TheUnitAndTheSourceOfAReadingAreResolvedAtTheEdge()
		{
			Assert.Throws<DomainException>(() => schedule.RecordReading(
				40000, (DistanceUnitCode)99, ObservedLater, ReadingSourceCode.HumanEntry));

			Assert.Throws<DomainException>(() => schedule.RecordReading(
				40000, DistanceUnitCode.UnitA, ObservedLater, (ReadingSourceCode)99));
		}
	}
}
