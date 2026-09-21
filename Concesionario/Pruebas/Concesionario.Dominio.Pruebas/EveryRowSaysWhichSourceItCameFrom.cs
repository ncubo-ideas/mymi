using System;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// SLICE-OdometerSeries L5 sobre ASeriesRow: cada fila del registro nombra SU FUENTE, y ninguna
	// fila se pesa, se promedia ni se descarta por ella. Decirla es la obligacion entera.
	//
	// POR QUE ES UNA LEY Y NO UN CAMPO MAS. ReadingSource tiene un miembro declarado por la persona
	// y otro correctivo. Una serie que muestre un punto declarado por la persona como si fuera
	// medido es un registro que se tergiversa a si mismo. Y hasta este slice NADA leia la fuente de
	// vuelta: cruzaba hacia adentro con la lectura y ahi se quedaba, asi que L5 es su primer
	// consumidor.
	//
	// L5 ES UNA LEY DE DIVULGACION, NO DE PESO. El ritmo del periodo es el mismo con las cinco
	// fuentes presentes que con una, y eso se prueba comparando -- no se supone.
	[TestFixture]
	public sealed class EveryRowSaysWhichSourceItCameFrom
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

		// Tres fuentes presentes, tres nombres distintos en la columna, en el orden de observacion.
		[Test]
		public void ThreeSourcesPresentAreThreeNamesOnTheRows()
		{
			schedule.RecordReading(
				20000, DistanceUnitCode.UnitA, Enero10, ReadingSourceCode.Telemetry);
			schedule.RecordReading(
				21000, DistanceUnitCode.UnitA, Febrero10, ReadingSourceCode.CustomerDeclared);
			schedule.RecordReading(
				22000, DistanceUnitCode.UnitA, Marzo10, ReadingSourceCode.Correction);

			Assert.That(
				schedule.ReadingSourcesBetween(ElAno, FinDelAno),
				Is.EqualTo(new[] { "Telemetry", "CustomerDeclared", "Correction" }),
				"El NOMBRE del miembro, que es la mitad direccionable: quien lea la columna puede "
				+ "volver a preguntar con el.");
		}

		// Y la frase de cada fila la dice en palabras, sin que ninguna pantalla tenga que llevar su
		// propia tabla. Las dos formas conviven: el nombre para la maquina, las palabras para la
		// persona.
		[Test]
		public void EachRowSaysItsSourceInWordsAsWellAsByName()
		{
			schedule.RecordReading(
				20000, DistanceUnitCode.UnitA, Enero10, ReadingSourceCode.Telemetry);
			schedule.RecordReading(
				21000, DistanceUnitCode.UnitA, Febrero10, ReadingSourceCode.CustomerDeclared);

			string[] said = TheSentenceTheDoorWouldCompose.EachReadingOnTheRecord(
				schedule, ElAno, FinDelAno, TheLanguage.OfTheRecord);

			Assert.That(
				said,
				Is.EqualTo(new[]
				{
					"20.000 km el 2026-01-10 00:00:00, reportada por el vehículo",
					"21.000 km el 2026-02-10 00:00:00, declarada por la persona",
				}),
				"La fila es autodescriptiva: valor, unidad, instante y de donde vino. Nada de eso "
				+ "significa algo sin lo demas.");
		}

		// LOS CINCO MIEMBROS dicen algo distinto. Un conjunto cerrado donde dos miembros dijeran lo
		// mismo dejaria una diferencia sin decir, y eso es lo que L5 existe para evitar.
		[Test]
		public void EachOfTheFiveSourcesSaysSomethingOfItsOwn()
		{
			ReadingSourceCode[] losCinco =
			{
				ReadingSourceCode.HumanEntry, ReadingSourceCode.Telemetry,
				ReadingSourceCode.Inspection, ReadingSourceCode.CustomerDeclared,
				ReadingSourceCode.Correction,
			};

			System.Collections.Generic.List<string> dichas =
				new System.Collections.Generic.List<string>();

			int value = 10000;
			DateTime observedAt = Enero10;

			foreach (ReadingSourceCode source in losCinco)
			{
				MaintenanceSchedule una =
					new MaintenanceSchedule(TheVin, UsageProfileCode.Standard);

				una.RecordReading(value, DistanceUnitCode.UnitA, observedAt, source);

				string[] filas = TheSentenceTheDoorWouldCompose.EachReadingOnTheRecord(
				una, ElAno, FinDelAno, TheLanguage.OfTheRecord);

				Assert.That(filas.Length, Is.EqualTo(1));
				dichas.Add(filas[0]);
			}

			Assert.That(
				dichas, Is.Unique,
				"Dos de las cinco fuentes se dicen igual: " + string.Join(" | ", dichas));
		}

		// L5 es divulgacion y no peso: el ritmo del periodo NO cambia por cuales fuentes esten
		// presentes. Se compara contra el mismo periodo con las mismas lecturas y otras fuentes.
		[Test]
		public void TheRateIsUnchangedByWhichSourcesArePresent()
		{
			schedule.RecordReading(
				20000, DistanceUnitCode.UnitA, Enero10, ReadingSourceCode.Telemetry);
			schedule.RecordReading(
				22000, DistanceUnitCode.UnitA, Marzo10, ReadingSourceCode.Telemetry);

			string conTelemetria = TheSentenceTheDoorWouldCompose.TheRateOfUse(
				schedule, ElAno, FinDelAno, TheLanguage.OfTheRecord);

			MaintenanceSchedule declaradas =
				new MaintenanceSchedule(TheVin, UsageProfileCode.Standard);

			declaradas.RecordReading(
				20000, DistanceUnitCode.UnitA, Enero10, ReadingSourceCode.CustomerDeclared);
			declaradas.RecordReading(
				22000, DistanceUnitCode.UnitA, Marzo10, ReadingSourceCode.HumanEntry);

			Assert.That(
				TheSentenceTheDoorWouldCompose.TheRateOfUse(
				declaradas, ElAno, FinDelAno, TheLanguage.OfTheRecord), Is.EqualTo(conTelemetria),
				"Mismas lecturas, otras fuentes, mismo ritmo. Pesar una fuente seria el dominio "
				+ "juzgando el registro de la persona, y §6 lo prohibe por nombre.");
		}

		// Una lectura CORRECTIVA cambia el pasado -- ese es su trabajo -- y la fila dice que es una
		// correccion. Las dos mitades juntas: el ritmo del periodo que la cubre cambia, y quien mire
		// las filas ve por que.
		[Test]
		public void ACorrectiveReadingChangesThePastAndTheRowSaysItIsACorrection()
		{
			schedule.RecordReading(
				20000, DistanceUnitCode.UnitA, Enero10, ReadingSourceCode.Telemetry);
			schedule.RecordReading(
				22000, DistanceUnitCode.UnitA, Marzo10, ReadingSourceCode.Telemetry);

			string antes = TheSentenceTheDoorWouldCompose.TheRateOfUse(
				schedule, Enero10, Marzo10, TheLanguage.OfTheRecord);

			Assert.That(antes, Does.Contain("2.000 km en 59 días"));

			// La enmienda entra como un hecho MAS, en el mismo segundo que la de marzo pero
			// despues: no borra la que estaba.
			schedule.RecordReading(
				23000, DistanceUnitCode.UnitA, Marzo10, ReadingSourceCode.Correction);

			Assert.That(
				schedule.ReadingCountBetween(Enero10, Marzo10), Is.EqualTo(3),
				"No borro la que estaba: entra como un hecho mas.");
			Assert.That(
				TheSentenceTheDoorWouldCompose.TheRateOfUse(
				schedule, Enero10, Marzo10, TheLanguage.OfTheRecord),
				Does.Contain("3.000 km en 59 días"),
				"Y el ritmo del periodo que la cubre cambio.");

			string[] filas = TheSentenceTheDoorWouldCompose.EachReadingOnTheRecord(
				schedule, Enero10, Marzo10, TheLanguage.OfTheRecord);

			Assert.That(
				filas[2], Does.Contain("corrección de una lectura anterior"),
				"Y la fila lo dice, que es la mitad que hace legible el cambio.");
		}
	}
}
