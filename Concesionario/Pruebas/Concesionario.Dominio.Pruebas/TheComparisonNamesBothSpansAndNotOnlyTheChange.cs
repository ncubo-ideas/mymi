using System;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// SLICE-OdometerSeries L4 sobre MaintenanceSchedule: "desde enero maneja ~60% mas" se contesta
	// con LOS DOS periodos, cada uno nombrado y con su propio ritmo, y el cambio dicho DENTRO de esa
	// misma frase. Una lectura que devolviera el porcentaje solo seria una mitad de una respuesta
	// compuesta, que es justo lo que AHalfOfAComposedAnswerNeverLeavesTheDoorAsANumber prohibe.
	//
	// EL ORDEN DE LOS DOS PERIODOS ES PARTE DEL CONTRATO. El cambio se dice del PRIMERO respecto al
	// SEGUNDO, porque asi lo va a pedir la pantalla: los doce meses que terminan en el instante
	// preguntado y los doce anteriores (P-11). La frase nombra las fechas de los dos, asi que quien
	// lea no depende de recordar el orden.
	[TestFixture]
	public sealed class TheComparisonNamesBothSpansAndNotOnlyTheChange
	{
		private const string TheVin = "1HGCM82633A004352";

		// El periodo VIEJO: 200 km en 10 dias, 20 km por dia.
		private static readonly DateTime ViejoDesde = new DateTime(2025, 1, 1);
		private static readonly DateTime ViejoHasta = new DateTime(2025, 1, 11);

		// El periodo NUEVO: 320 km en 10 dias, 32 km por dia. Un 60% mas.
		private static readonly DateTime NuevoDesde = new DateTime(2026, 1, 1);
		private static readonly DateTime NuevoHasta = new DateTime(2026, 1, 11);

		private MaintenanceSchedule schedule;

		[SetUp]
		public void Setup()
		{
			schedule = new MaintenanceSchedule(TheVin, UsageProfileCode.Standard);

			Llega(10000, ViejoDesde);
			Llega(10200, ViejoHasta);
			Llega(20000, NuevoDesde);
			Llega(20320, NuevoHasta);
		}

		private void Llega(int value, DateTime observedAt)
		{
			schedule.RecordReading(
				value, DistanceUnitCode.UnitA, observedAt, ReadingSourceCode.Telemetry);
		}

		// L4: los dos periodos nombrados, los dos ritmos dichos, y el cambio adentro.
		[Test]
		public void TheComparisonNamesBothSpansWithTheirRatesAndSaysTheChangeInside()
		{
			string said = TheSentenceTheDoorWouldCompose.TheTwoPeriodsOfUseCompared(
				schedule, NuevoDesde, NuevoHasta, ViejoDesde, ViejoHasta, TheLanguage.OfTheRecord);

			Assert.That(
				said,
				Does.Contain("de 20.000 km el 2026-01-01 00:00:00 a 20.320 km el 2026-01-11 00:00:00")
					.And.Contains("320 km en 10 días, unos 32 km por día"),
				"El primer periodo, dicho entero por su propia frase.");
			Assert.That(
				said,
				Does.Contain("de 10.000 km el 2025-01-01 00:00:00 a 10.200 km el 2025-01-11 00:00:00")
					.And.Contains("200 km en 10 días, unos 20 km por día"),
				"Y el segundo, igual de entero.");
			Assert.That(
				said, Does.Contain("un 60% más"),
				"El cambio, dentro de la misma frase que ya dijo los dos ritmos.");
		}

		// Y no hay ninguna lectura que entregue el cambio solo. Censo por nombre sobre la agenda:
		// ningun miembro dice Change, Delta ni PercentChange (§4 los prohibe por nombre).
		[Test]
		public void NoMemberHandsOutTheChangeAlone()
		{
			string[] prohibidos = { "Change", "Delta", "PercentChange", "Percentage" };

			foreach (System.Reflection.MethodInfo miembro in typeof(MaintenanceSchedule).GetMethods(
				System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public
					| System.Reflection.BindingFlags.NonPublic
					| System.Reflection.BindingFlags.DeclaredOnly))
				foreach (string prohibido in prohibidos)
					Assert.That(
						miembro.Name, Does.Not.Contain(prohibido),
						"'" + miembro.Name + "' nombra el cambio suelto. El cambio se dice dentro "
						+ "de la frase que trae los dos ritmos, y en ningun otro lado.");
		}

		// Cuando el cambio es a la baja lo dice a la baja, y cuando no hay cambio lo dice tambien:
		// las tres ramas de una comparacion, no dos y un silencio.
		[Test]
		public void TheComparisonSaysADropAndSaysNoChangeToo()
		{
			string alaBaja = TheSentenceTheDoorWouldCompose.TheTwoPeriodsOfUseCompared(
				schedule, ViejoDesde, ViejoHasta, NuevoDesde, NuevoHasta, TheLanguage.OfTheRecord);

			Assert.That(
				alaBaja, Does.Contain("un 38% menos"),
				"20 contra 32 es un 37,5% menos, redondeado a 38 y dicho como menos.");

			MaintenanceSchedule igual = new MaintenanceSchedule(TheVin, UsageProfileCode.Standard);

			igual.RecordReading(
				10000, DistanceUnitCode.UnitA, ViejoDesde, ReadingSourceCode.Telemetry);
			igual.RecordReading(
				10200, DistanceUnitCode.UnitA, ViejoHasta, ReadingSourceCode.Telemetry);
			igual.RecordReading(
				20000, DistanceUnitCode.UnitA, NuevoDesde, ReadingSourceCode.Telemetry);
			igual.RecordReading(
				20200, DistanceUnitCode.UnitA, NuevoHasta, ReadingSourceCode.Telemetry);

			Assert.That(
				TheSentenceTheDoorWouldCompose.TheTwoPeriodsOfUseCompared(
				igual, NuevoDesde, NuevoHasta, ViejoDesde, ViejoHasta, TheLanguage.OfTheRecord),
				Does.Contain("el mismo ritmo"));
		}

		// Cuando UNO de los dos esta suspendido la frase dice cual y por que, y NO dice ningun
		// cambio. Los dos periodos siguen nombrados: la ausencia de uno no borra al otro.
		[Test]
		public void WhenEitherSpanIsSuspendedTheSentenceSaysWhichAndSaysNoChange()
		{
			string said = TheSentenceTheDoorWouldCompose.TheTwoPeriodsOfUseCompared(
				schedule, NuevoDesde, NuevoHasta, new DateTime(2020, 1, 1),
				new DateTime(2020, 12, 31), TheLanguage.OfTheRecord);

			Assert.That(
				said, Does.Contain("320 km en 10 días"),
				"El periodo que si tiene ritmo lo sigue diciendo.");
			Assert.That(
				said, Does.Contain("no hay ninguna lectura del odómetro en ese periodo"),
				"Y el que no, dice por que -- la razon con nombre, no un hueco.");
			Assert.That(said, Does.Not.Contain("%"));
			Assert.That(said, Does.Not.Contain("el mismo ritmo"));
		}

		// EL PORCENTAJE SALE DE LOS NUMEROS QUE LA FRASE DICE, no de los ritmos exactos. Los dos
		// redondeos de este slice tienen que cooperar: si el cambio se sacara de los ritmos exactos
		// podria no cuadrar con las dos cifras impresas al lado, y quien lea las tres no podria
		// revisar ninguna.
		//
		// LOS DATOS ESTAN ELEGIDOS PARA QUE LAS DOS RUTAS NO COINCIDAN. 100 km en 3 dias son
		// 33,33 exactos y 33 dichos; 200 km en 3 dias son 66,67 exactos y 67 dichos. Desde los
		// exactos el cambio es 100%; desde los dichos es 103%, que es el que cuadra con 33 y 67.
		// Sin este par de datos las dos rutas dan lo mismo y la decision no la defiende nada.
		[Test]
		public void ThePercentageIsTakenFromTheFiguresTheSentenceSaysAndNotFromTheExactRates()
		{
			DateTime viejoDesde = new DateTime(2025, 1, 1);
			DateTime viejoHasta = new DateTime(2025, 1, 4);
			DateTime nuevoDesde = new DateTime(2026, 1, 1);
			DateTime nuevoHasta = new DateTime(2026, 1, 4);

			MaintenadaATercios();

			string said = TheSentenceTheDoorWouldCompose.TheTwoPeriodsOfUseCompared(
				aTercios, nuevoDesde, nuevoHasta, viejoDesde, viejoHasta, TheLanguage.OfTheRecord);

			Assert.That(
				said, Does.Contain("200 km en 3 días, unos 67 km por día"),
				"El ritmo del primero, redondeado y dicho.");
			Assert.That(
				said, Does.Contain("100 km en 3 días, unos 33 km por día"),
				"Y el del segundo.");
			Assert.That(
				said, Does.Contain("un 103% más"),
				"103 es el cambio entre 67 y 33, que son las dos cifras que la frase dice. Desde "
				+ "los ritmos exactos -- 66,67 contra 33,33 -- daria 100%, y quedaria en "
				+ "desacuerdo con los dos numeros impresos al lado.");
		}

		private MaintenanceSchedule aTercios;

		private void MaintenadaATercios()
		{
			aTercios = new MaintenanceSchedule(TheVin, UsageProfileCode.Standard);

			aTercios.RecordReading(
				10000, DistanceUnitCode.UnitA, new DateTime(2025, 1, 1),
				ReadingSourceCode.Telemetry);
			aTercios.RecordReading(
				10100, DistanceUnitCode.UnitA, new DateTime(2025, 1, 4),
				ReadingSourceCode.Telemetry);
			aTercios.RecordReading(
				20000, DistanceUnitCode.UnitA, new DateTime(2026, 1, 1),
				ReadingSourceCode.Telemetry);
			aTercios.RecordReading(
				20200, DistanceUnitCode.UnitA, new DateTime(2026, 1, 4),
				ReadingSourceCode.Telemetry);
		}

		// El caso que ni el brief ni el plan contemplan: los dos medidos y el SEGUNDO en cero. Un
		// porcentaje de cero no existe, y el reflejo es devolver cero o infinito. Se dice en
		// palabras, con los dos ritmos igual dichos.
		[Test]
		public void AChangeAgainstAPeriodThatDidNotMoveIsSaidInWordsAndNotAsAPercentage()
		{
			MaintenanceSchedule quieto =
				new MaintenanceSchedule(TheVin, UsageProfileCode.Standard);

			quieto.RecordReading(
				10000, DistanceUnitCode.UnitA, ViejoDesde, ReadingSourceCode.Telemetry);
			quieto.RecordReading(
				10000, DistanceUnitCode.UnitA, ViejoHasta, ReadingSourceCode.Telemetry);
			quieto.RecordReading(
				10000, DistanceUnitCode.UnitA, NuevoDesde, ReadingSourceCode.Telemetry);
			quieto.RecordReading(
				10320, DistanceUnitCode.UnitA, NuevoHasta, ReadingSourceCode.Telemetry);

			string said = TheSentenceTheDoorWouldCompose.TheTwoPeriodsOfUseCompared(
				quieto, NuevoDesde, NuevoHasta, ViejoDesde, ViejoHasta, TheLanguage.OfTheRecord);

			Assert.That(
				said, Does.Contain("0 km en 10 días, unos 0 km por día"),
				"Cero SI es un ritmo medido: quiere decir que el vehiculo no se movio, que es un "
				+ "hecho y hasta puede ser cierto (L6).");
			Assert.That(
				said, Does.Contain("no se puede decir como porcentaje"),
				"Y el cambio contra cero se dice en palabras.");
			Assert.That(said, Does.Not.Contain("%"));
		}
	}
}
