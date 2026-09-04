using System;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// SLICE-OdometerSeries L3 + L11 sobre MaintenanceSchedule: un periodo cuyas lecturas no estan
	// todas en una unidad contesta una suspension CON NOMBRE que dice cuales dos unidades no calzan
	// y cual lectura introdujo la segunda, con su fecha. No un numero, no un throw, no una eleccion
	// silenciosa.
	//
	// ESTA ES LA MITAD QUE NADA PODIA VER ANTES. La guarda de insercion recorre TODA lectura
	// anterior y TODA posterior -- no solo las vecinas de lista, medido en OD-0 -- pero cada
	// comparacion esta condicionada a arriving.IsExpressedIn(...), identidad sobre DistanceUnit. Una
	// lectura en la otra unidad se SALTEA, y no puede ser de otra forma: sin conversion la
	// comparacion no significa nada, y este modulo no convierte nada. Asi que una serie de unidades
	// mezcladas ya es almacenable hoy, y hasta este slice nada leia la serie de punta a punta, asi
	// que nada podia verlo.
	//
	// TwoUnitsAreTwoTimelinesAndNeitherJudgesTheOther ya fija la mitad ALMACENABLE desde OS2. Lo
	// nuevo de este slice es la otra mitad: y el ritmo la rechaza.
	[TestFixture]
	public sealed class ARateAcrossTwoUnitsIsNotARate
	{
		private const string TheVin = "1HGCM82633A004352";

		private static readonly DateTime Enero10 = new DateTime(2026, 1, 10);
		private static readonly DateTime Febrero10 = new DateTime(2026, 2, 10);
		private static readonly DateTime Marzo10 = new DateTime(2026, 3, 10);

		private static readonly DateTime ElAno = new DateTime(2026, 1, 1);
		private static readonly DateTime FinDelAno = new DateTime(2026, 12, 31);

		private MaintenanceSchedule schedule;

		[SetUp]
		public void Setup()
		{
			schedule = new MaintenanceSchedule(TheVin, UsageProfileCode.Standard);
		}

		// L3: la suspension nombra LAS DOS UNIDADES. Sin las dos, quien lea no sabe que hay que
		// arreglar.
		[Test]
		public void ARateAcrossTwoUnitsNamesBothUnits()
		{
			schedule.RecordReading(
				20000, DistanceUnitCode.UnitA, Enero10, ReadingSourceCode.Telemetry);
			schedule.RecordReading(
				500, DistanceUnitCode.UnitB, Febrero10, ReadingSourceCode.CustomerDeclared);

			Assert.That(schedule.CanARateOfUseBeSaid(ElAno, FinDelAno), Is.False);
			Assert.That(
				schedule.ReadRateOfUseStanding(ElAno, FinDelAno),
				Is.EqualTo("SuspendedByAUnitMismatch"));

			string said = schedule.DescribeRateOfUse(ElAno, FinDelAno);

			Assert.That(said, Does.Contain("km").And.Contains("mi"));
			Assert.That(
				said, Does.Contain("nunca se convierten"),
				"Es la razon, y es la misma que ADimensionSuspendedByAUnitMismatch ya dice.");
			Assert.That(said, Does.Not.Contain("por día"));
		}

		// L11: y nombra LA LECTURA que introdujo la segunda unidad, CON SU FECHA. Es la diferencia
		// entre "hay unidades mezcladas" y "arregle la lectura del 10 de febrero": una de las dos
		// se puede accionar.
		[Test]
		public void ARateAcrossTwoUnitsNamesTheReadingThatIntroducedTheSecondWithItsDate()
		{
			schedule.RecordReading(
				20000, DistanceUnitCode.UnitA, Enero10, ReadingSourceCode.Telemetry);
			schedule.RecordReading(
				21000, DistanceUnitCode.UnitA, Febrero10, ReadingSourceCode.Telemetry);
			schedule.RecordReading(
				500, DistanceUnitCode.UnitB, Marzo10, ReadingSourceCode.CustomerDeclared);

			string said = schedule.DescribeRateOfUse(ElAno, FinDelAno);

			Assert.That(
				said, Does.Contain("500 mi del 2026-03-10 00:00:00"),
				"La que introdujo la segunda unidad es la de marzo, y se nombra con su fecha.");
			Assert.That(
				said, Does.Not.Contain("21000"),
				"La de febrero esta en la unidad del periodo: no es la que rompio nada.");
		}

		// §1.1 HECHO PERMANENTE. La guarda ACEPTA la lectura en la segunda unidad -- porque
		// compararla contra las otras seria convertir sin escribir la palabra convertir -- asi que
		// el registro es almacenable; y el ritmo la rechaza, porque restar km de mi no da una
		// distancia. Las dos mitades en una sola prueba, que es lo que hace de esto un hallazgo y
		// no dos hechos sueltos.
		[Test]
		public void AReadingTheGuardAcceptedInASecondUnitIsStorableAndTheRateRefusesIt()
		{
			schedule.RecordReading(
				20000, DistanceUnitCode.UnitA, Enero10, ReadingSourceCode.Telemetry);
			schedule.RecordReading(
				22000, DistanceUnitCode.UnitA, Marzo10, ReadingSourceCode.Telemetry);

			Assert.That(
				schedule.CanARateOfUseBeSaid(ElAno, FinDelAno), Is.True,
				"Control: antes de la lectura ajena el periodo tiene ritmo.");

			// 500 en UnitB, entre dos de 20000 y 22000 en UnitA. Como NUMERO va para atras; como
			// unidad distinta no se compara con ninguna de las dos.
			Assert.DoesNotThrow(
				() => schedule.RecordReading(
					500, DistanceUnitCode.UnitB, Febrero10, ReadingSourceCode.CustomerDeclared),
				"La guarda la acepta: dos unidades son dos lineas de tiempo y ninguna juzga a la "
				+ "otra.");

			Assert.That(
				schedule.ReadingCountBetween(ElAno, FinDelAno), Is.EqualTo(3),
				"Almacenable: esta en la serie y el registro la muestra.");
			Assert.That(
				schedule.CanARateOfUseBeSaid(ElAno, FinDelAno), Is.False,
				"Y el ritmo la rechaza. Es la primera lectura de la serie entera que alguien hace, "
				+ "asi que es la primera cosa que puede estar equivocada sobre ella.");
			Assert.That(
				schedule.ReadRateOfUseStanding(ElAno, FinDelAno),
				Is.EqualTo("SuspendedByAUnitMismatch"));
		}

		// G5, dicho como prueba y no como parrafo: L3 TAL COMO ESTA ESCRITA suspende el periodo
		// entero, aunque sus DOS EXTREMOS esten en la misma unidad y la ajena este adentro. La
		// alternativa que no se tomo -- el ritmo desde los extremos, ignorando el interior -- daria
		// verde aca, y por eso esta prueba dice cual de las dos lecturas de L3 se construyo.
		//
		// LO QUE SI SOBREVIVE ES EL REGISTRO, no el ritmo: las filas siguen contestando y dicen
		// cual lectura y en que unidad. Esa es la respuesta gruesa que Cargas conserva, en la unica
		// forma que este modulo tiene.
		[Test]
		public void AForeignUnitInsideThePeriodSuspendsItEvenWhenBothEndsShareAUnit()
		{
			schedule.RecordReading(
				20000, DistanceUnitCode.UnitA, Enero10, ReadingSourceCode.Telemetry);
			schedule.RecordReading(
				500, DistanceUnitCode.UnitB, Febrero10, ReadingSourceCode.CustomerDeclared);
			schedule.RecordReading(
				22000, DistanceUnitCode.UnitA, Marzo10, ReadingSourceCode.Inspection);

			Assert.That(
				schedule.ReadRateOfUseStanding(ElAno, FinDelAno),
				Is.EqualTo("SuspendedByAUnitMismatch"),
				"Los dos extremos estan en km y el ritmo se suspende igual: L3 mira TODAS las "
				+ "lecturas del periodo, no sus extremos.");

			string[] units = schedule.ReadingUnitsBetween(ElAno, FinDelAno);

			Assert.That(units, Is.EqualTo(new[] { "km", "mi", "km" }));
			Assert.That(
				schedule.ReadingCountBetween(ElAno, FinDelAno), Is.EqualTo(3),
				"El registro contesta las tres filas mientras el ritmo esta suspendido: es lo que "
				+ "hace que una unidad equivocada cueste el detalle y no la pantalla entera.");
		}
	}
}
