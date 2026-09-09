using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// SLICE-OdometerSeries L1, L2, L6, L7, L8 sobre MaintenanceSchedule: un ritmo necesita DOS
	// lecturas, el dominio recorre la serie entera y la respuesta trae los dos extremos Y el ritmo
	// en un solo valor, y nada de eso se guarda.
	//
	// POR QUE LOS EXTREMOS NO SALEN SUELTOS. AHalfOfAComposedAnswerNeverLeavesTheDoorAsANumber ya
	// prohibe entregar las dos mitades de una respuesta compuesta: quien tenga las dos lecturas
	// resta y divide y dice un ritmo que el dominio nunca dijo. L2 es esa misma ley llegando por el
	// otro lado -- la peticion dice "el dominio calcula la serie completa y la consulta devuelve
	// solo los extremos", y los extremos salen DENTRO de la frase que ya trae el ritmo.
	//
	// LA GRANULARIDAD DEL JOURNAL DECIDE EL ESTADO VACIO. C13: el journal trunca al segundo entero,
	// asi que dos lecturas dentro del mismo segundo son un tramo de largo cero y eso es un estado
	// con nombre, no una division por cero ni una constante de tramo minimo (§6).
	[TestFixture]
	public sealed class ARateIsASpanAndNotAReading
	{
		private const string TheVin = "1HGCM82633A004352";

		private static readonly DateTime Enero10 = new DateTime(2026, 1, 10);
		private static readonly DateTime Marzo10 = new DateTime(2026, 3, 10);
		private static readonly DateTime Febrero10 = new DateTime(2026, 2, 10);

		private static readonly DateTime ElAno = new DateTime(2026, 1, 1);
		private static readonly DateTime FinDelAno = new DateTime(2026, 12, 31);

		private MaintenanceSchedule schedule;

		[SetUp]
		public void Setup()
		{
			schedule = new MaintenanceSchedule(TheVin, UsageProfileCode.Standard);
		}

		private void Llega(int value, DateTime observedAt)
		{
			schedule.RecordReading(
				value, DistanceUnitCode.UnitA, observedAt, ReadingSourceCode.Telemetry);
		}

		// L1 + L7: dos lecturas en el periodo dan UN ritmo, y la frase nombra los dos extremos, los
		// dos instantes, la unidad y el ritmo. Letra por letra, porque la ley es sobre la FRASE:
		// una asercion que solo mire que no viene vacia no distingue esta frase de cualquier otra
		// (C60).
		[Test]
		public void ARateNamesBothEndsBothInstantsTheUnitAndTheRate()
		{
			Llega(20000, Enero10);
			Llega(21000, Febrero10);
			Llega(22000, Marzo10);

			Assert.That(schedule.CanARateOfUseBeSaid(ElAno, FinDelAno), Is.True);
			Assert.That(
				schedule.ReadRateOfUseStanding(ElAno, FinDelAno), Is.EqualTo("Measured"));
			Assert.That(
				schedule.DescribeRateOfUse(ElAno, FinDelAno),
				Is.EqualTo(
					"de 20000 km el 2026-01-10 00:00:00 a 22000 km el 2026-03-10 00:00:00: "
					+ "2000 km en 59 días, unos 34 km por día"));
		}

		// L2: los extremos de la respuesta son la PRIMERA y la ULTIMA fila del registro del mismo
		// periodo. No porque la pantalla pueda calcularlo, sino para que nunca le convenga.
		[Test]
		public void TheDomainComposesTheSeriesAndTheAnswerIsItsExtremes()
		{
			Llega(20000, Enero10);
			Llega(21000, Febrero10);
			Llega(22000, Marzo10);

			int[] values = schedule.ReadingValuesBetween(ElAno, FinDelAno);
			string[] instants = schedule.ReadingInstantsBetween(ElAno, FinDelAno);

			Assert.That(values.Length, Is.EqualTo(3));
			Assert.That(schedule.ReadingCountBetween(ElAno, FinDelAno), Is.EqualTo(3));

			string said = schedule.DescribeRateOfUse(ElAno, FinDelAno);

			Assert.That(said, Does.StartWith("de " + values[0] + " km el " + instants[0]));
			Assert.That(
				said,
				Does.Contain("a " + values[values.Length - 1] + " km el "
					+ instants[instants.Length - 1]));
			Assert.That(
				said, Does.Not.Contain(values[1].ToString()),
				"La del medio no es un extremo, y decirla haria de la frase una tabla.");
		}

		// L2, la otra mitad: el periodo recorta. Una lectura fuera de los limites no es un extremo
		// de este periodo aunque este en la serie.
		[Test]
		public void APeriodTakesItsEndsFromInsideItsOwnBounds()
		{
			Llega(20000, Enero10);
			Llega(21000, Febrero10);
			Llega(22000, Marzo10);

			Assert.That(
				schedule.ReadingCountBetween(Enero10, Febrero10), Is.EqualTo(2),
				"Los dos limites son inclusive.");
			Assert.That(
				schedule.DescribeRateOfUse(Enero10, Febrero10),
				Does.Contain("a 21000 km el 2026-02-10 00:00:00"));
			Assert.That(
				schedule.DescribeRateOfUse(Enero10, Febrero10), Does.Not.Contain("22000"));
		}

		// L6: una sola lectura es un TODAVIA NO con la lectura nombrada, nunca un cero. Cero
		// querria decir "el carro no se movio", que es otro hecho y hasta podria ser cierto.
		[Test]
		public void OneReadingIsANotYetAndNotAZero()
		{
			Llega(20000, Enero10);

			Assert.That(schedule.CanARateOfUseBeSaid(ElAno, FinDelAno), Is.False);
			Assert.That(
				schedule.ReadRateOfUseStanding(ElAno, FinDelAno),
				Is.EqualTo("SuspendedByASingleReading"));

			string said = schedule.DescribeRateOfUse(ElAno, FinDelAno);

			Assert.That(said, Does.Contain("una sola lectura"));
			Assert.That(
				said, Does.Contain("20000 km del 2026-01-10 00:00:00"),
				"L11: la suspension nombra el hecho que hay y su fecha, no solo su categoria.");
			Assert.That(
				said, Does.Not.Contain("0 km por día"),
				"Un cero aqui se leeria como que el vehiculo no se movio.");
		}

		// Ausencia: ninguna lectura en el periodo es una ausencia CON NOMBRE que dice el periodo.
		[Test]
		public void NoReadingInAPeriodIsANamedAbsence()
		{
			Llega(20000, new DateTime(2025, 6, 1));

			Assert.That(schedule.CanARateOfUseBeSaid(ElAno, FinDelAno), Is.False);
			Assert.That(
				schedule.ReadRateOfUseStanding(ElAno, FinDelAno),
				Is.EqualTo("SuspendedByAnAbsentReading"));
			Assert.That(schedule.ReadingCountBetween(ElAno, FinDelAno), Is.EqualTo(0));

			string said = schedule.DescribeRateOfUse(ElAno, FinDelAno);

			Assert.That(
				said, Does.Contain("2026-01-01 00:00:00").And.Contains("2026-12-31 00:00:00"),
				"L11: nombra el periodo por el que se pregunto.");
			Assert.That(said, Does.Not.Contain("0 km"));
		}

		// §6: dos lecturas en el MISMO segundo son un tramo de largo cero, que es un estado con
		// nombre. Y la segunda mitad de la ley: no existe ninguna constante de tramo minimo. Se
		// barre Dominio/ y se leen las coincidencias, no se cuentan (C71).
		[Test]
		public void TwoReadingsAtTheSameSecondAreAZeroLengthSpanAndNoConstantExists()
		{
			DateTime elMismoSegundo = new DateTime(2026, 1, 10, 8, 0, 0);

			Llega(20000, elMismoSegundo);
			Llega(20000, elMismoSegundo);

			Assert.That(schedule.CanARateOfUseBeSaid(ElAno, FinDelAno), Is.False);
			Assert.That(
				schedule.ReadRateOfUseStanding(ElAno, FinDelAno),
				Is.EqualTo("SuspendedByAZeroLengthSpan"));
			Assert.That(
				schedule.DescribeRateOfUse(ElAno, FinDelAno),
				Does.Contain("el mismo segundo").And.Contains("2026-01-10 08:00:00"));

			// La segunda mitad: ninguno de los tipos de este slice declara un umbral de tramo
			// minimo. Se enumeran los campos constantes por reflexion y se nombran.
			string[] losTiposDelSlice =
			{
				"ASpanOfUse", "RateAnswer", "AMeasuredRate", "ATwoSpanComparison", "ASeriesRow",
				"RateStanding", "ARateSuspendedByAUnitMismatch", "ARateSuspendedByASingleReading",
				"ARateSuspendedByAnAbsentReading", "ARateSuspendedByAZeroLengthSpan",
			};

			foreach (string nombre in losTiposDelSlice)
			{
				Type tipo = typeof(MaintenanceSchedule).Assembly.GetType(
					"Concesionario.Dominio." + nombre);

				Assert.That(tipo, Is.Not.Null, nombre + " no existe en el dominio.");

				FieldInfo[] constantes = tipo
					.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
						| BindingFlags.DeclaredOnly)
					.Where(campo => campo.IsLiteral)
					.ToArray();

				Assert.That(
					constantes.Select(campo => campo.Name), Is.Empty,
					nombre + " declara una constante: "
					+ string.Join(", ", constantes.Select(campo => campo.Name))
					+ ". §6 dice que aca no se elige ningun numero, y un tramo minimo es el que se "
					+ "va a querer agregar.");
			}
		}

		// L8: el ritmo se deriva en cada lectura y NO se guarda. La mitad estructural: el agregado
		// no gano ningun campo, y eso se enumera por reflexion en vez de leerse.
		[Test]
		public void ARateIsDerivedAndNeverStored()
		{
			string[] losCamposDeLaAgenda = typeof(MaintenanceSchedule)
				.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
					| BindingFlags.DeclaredOnly)
				.Select(campo => campo.Name)
				.OrderBy(nombre => nombre, StringComparer.Ordinal)
				.ToArray();

			Assert.That(
				losCamposDeLaAgenda,
				Is.EqualTo(new[]
				{
					"_declarations", "_initialProfile", "_lastEvaluation", "_operations", "_order",
					"_readings", "_revisions", "_vin",
				}),
				"La agenda gano o perdio un campo. Un tramo guardado no es un cache: es una segunda "
				+ "respuesta que puede no coincidir con la primera, porque una lectura que llega "
				+ "tarde redefine tramos ya vistos.");
		}

		// L8 vivo: una lectura que llega DESPUES pero se observo ANTES cambia todos los ritmos que
		// la cubren, y no hay nada guardado en el medio que pueda quedar viejo.
		[Test]
		public void AReadingArrivingOutOfOrderChangesEveryRateThatCoversIt()
		{
			Llega(20000, Enero10);
			Llega(22000, Marzo10);

			string antes = schedule.DescribeRateOfUse(ElAno, FinDelAno);

			Assert.That(antes, Does.Contain("2000 km en 59 días"));

			// Un relleno tardio que calza en la linea: se observo en febrero y llega ahora.
			schedule.RecordReading(
				21000, DistanceUnitCode.UnitA, Febrero10, ReadingSourceCode.HumanEntry);

			Assert.That(
				schedule.ReadingCountBetween(ElAno, FinDelAno), Is.EqualTo(3),
				"Entro en su lugar de la linea de observacion, no al final.");
			Assert.That(
				schedule.DescribeRateOfUse(ElAno, FinDelAno), Is.EqualTo(antes),
				"Los extremos del ANO no se movieron, asi que su ritmo tampoco.");
			Assert.That(
				schedule.DescribeRateOfUse(Enero10, Febrero10),
				Does.Contain("1000 km en 31 días"),
				"Y el ritmo del periodo que la cubre SI cambio: antes ese periodo tenia una sola "
				+ "lectura.");
		}
	}
}
