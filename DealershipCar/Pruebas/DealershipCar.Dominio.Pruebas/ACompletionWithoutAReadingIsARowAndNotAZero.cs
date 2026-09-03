using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LEY L6 DE SLICE-TheMaintenanceScreen: una definicion de solo tiempo se ancla con un instante y
	// sin lectura. La fila dice que la lectura esta AUSENTE, con su propio tipo. Nunca cero, nunca
	// null, nunca un numero inventado.
	//
	// L6 ES ALCANZABLE POR DOS DE LAS CINCO REGLAS, re-derivado leyendo las cinco:
	// TimeOnly.cs:39-43 y ConditionOnly.cs:42-46 devuelven BasePoint.AtInstantOnly(completedAt) y
	// DESCARTAN la lectura; DistanceOnly, TimeAndDistance y TimeOrDistance devuelven
	// BasePoint.At(completedAt, reading, unit). (MS-0 §3 dice "dos de las CUATRO" y hay CINCO
	// subclases de IntervalRule.)
	//
	// Y LA TENTACION QUE HAY QUE NOMBRAR: CompleteOperation recibe una completionReading TAMBIEN para
	// una operacion de solo tiempo, y el dominio la tira. La lectura SE RECIBIO y NO SE GUARDA, asi
	// que part B no la puede recuperar -- y no debe: alcanzarla por otro lado seria decir que el
	// ancla tiene una lectura que el ancla no tiene.
	[TestFixture]
	public sealed class ACompletionWithoutAReadingIsARowAndNotAZero
	{
		private const string TheVin = "1HGCM82633A004352";
		private const int TheTimeLimitInDays = 365;
		private const int NoDistanceLimit = 0;
		private const int TheDistanceLimit = 8000;

		private static readonly DateTime TheSeededAnchor = new DateTime(2025, 1, 10, 7, 30, 0);
		private const int TheSeededAnchorReading = 30000;

		private static readonly DateTime TheCompletion = new DateTime(2025, 6, 11, 16, 40, 0);

		// LA LECTURA QUE SE RECIBE Y SE TIRA. No coincide con ninguna otra de este fixture, asi que
		// si reapareciera en cualquier columna se sabria de donde salio.
		private const int TheReadingThatIsReceivedAndDiscarded = 51234;

		// LA LECTURA DE LA OPERACION QUE SI MIDE DISTANCIA, para que la ausencia no sea general.
		private const int TheDistanceCompletionReading = 34100;

		[Test]
		public void ATimeOnlyCompletionSaysTheReadingIsAbsent()
		{
			MaintenanceSchedule schedule = ATimedComponentCompletedOnce();

			Assert.That(schedule.CompletionCount(OperationKindCode.TimedComponent), Is.EqualTo(1),
				"Sin lectura sigue habiendo fila: la ausencia es de la mitad de odometro, no de la "
				+ "completacion.");

			Assert.That(
				schedule.CompletionReadingsOf(OperationKindCode.TimedComponent),
				Is.EqualTo(new[] { "sin lectura registrada" }));

			Assert.That(
				schedule.DescribeEachCompletionOf(OperationKindCode.TimedComponent)[0],
				Does.Contain("2025-06-11 16:40:00").And.Contain("sin lectura del odómetro"),
				"La frase de la fila dice el instante Y dice que no hay lectura: callarla haria "
				+ "indistinguible 'no se midio' de 'no se dijo'.");
		}

		// NI CERO, NI VACIO, NI null. Las tres son las formas en que un lector confundiria la
		// ausencia con una medicion, y la celda no es ninguna de las tres.
		[Test]
		public void TheCellIsNeitherAZeroNorAnEmptyString()
		{
			MaintenanceSchedule schedule = ATimedComponentCompletedOnce();

			string[] readings = schedule.CompletionReadingsOf(OperationKindCode.TimedComponent);

			Assert.That(readings[0], Is.Not.Empty,
				"Una celda vacia se lee como que la columna no aplica. La ausencia se DICE.");
			Assert.That(readings[0], Is.Not.EqualTo("0"));
			Assert.That(readings[0], Is.Not.EqualTo("0 km"),
				"Un cero contra un limite de distancia se leeria como un vehiculo que no ha recorrido "
				+ "nada, que es lo que el /// de BasePoint.Reading existe para refusar.");
			Assert.That(readings[0], Does.Not.Contain("0 km"));
		}

		// LA LECTURA RECIBIDA NO SE RECUPERA DE NINGUN LADO. Se paso 51234 al completar y no aparece
		// en ninguna columna ni en ninguna frase, de ninguna de las dos lecturas del historial.
		[Test]
		public void TheReceivedReadingIsNotRecoveredFromAnywhere()
		{
			MaintenanceSchedule schedule = ATimedComponentCompletedOnce();

			string received = TheReadingThatIsReceivedAndDiscarded.ToString();

			Assert.That(
				schedule.CompletionReadingsOf(OperationKindCode.TimedComponent),
				Has.None.Contains(received));
			Assert.That(
				schedule.DescribeEachCompletionOf(OperationKindCode.TimedComponent),
				Has.None.Contains(received));
			Assert.That(
				schedule.DescribeCompletions(OperationKindCode.TimedComponent),
				Does.Not.Contain(received));
			Assert.That(
				schedule.CompletionReadingsOnRecord(), Has.None.Contains(received),
				"El ancla de solo instante no tiene lectura. Alcanzar la que se recibio seria "
				+ "componer una fila con un hecho que el ancla no guarda.");
		}

		// LA AUSENCIA NO ES GENERAL. En la MISMA agenda, la operacion que mide distancia trae su
		// lectura: si las dos dijeran ausente, esta prueba pasaria por la razon equivocada.
		[Test]
		public void TheOperationThatMeasuresDistanceStillCarriesItsReading()
		{
			MaintenanceSchedule schedule = ATimedComponentCompletedOnce();

			Assert.That(
				schedule.CompletionReadingsOf(OperationKindCode.FluidChange),
				Is.EqualTo(new[] { "34100 km" }));

			Assert.That(
				schedule.CompletionReadingsOnRecord(),
				Is.EqualTo(new[] { "sin lectura registrada", "34100 km" }),
				"Las dos filas en el mismo juego, cada una diciendo lo suyo: la de junio sin lectura y "
				+ "la de abril con la de ella.");
		}

		// L6 HECHO ESTRUCTURA: la fila no tiene ningun miembro que entregue un numero, asi que un
		// cero no es ni representable. Tampoco entrega el par AnchorReading, que es de BasePoint y no
		// suyo de repartir, ni el instante crudo -- comparar es una operacion con nombre (E23).
		[Test]
		public void NoMemberOfTheRowHandsOutANumberOrTheCarrier()
		{
			List<string> numbers = EveryMemberOfTheRow()
				.Where(member => member.ReturnType == typeof(int)
					|| member.ReturnType == typeof(DateTime))
				.Select(member => member.Name)
				.ToList();

			Assert.That(numbers, Is.Empty,
				"ACompletionRow entrega un numero o un instante crudo: " + string.Join(", ", numbers)
				+ ". Con un accesor numerico la ausencia vuelve a necesitar un centinela, y el "
				+ "centinela es el cero que L6 prohibe.");

			List<string> carriers = EveryMemberOfTheRow()
				.Where(member => member.ReturnType == typeof(AnchorReading)
					|| member.ReturnType == typeof(BasePoint)
					|| member.ReturnType == typeof(MaintenanceOperationKind))
				.Select(member => member.Name)
				.ToList();

			Assert.That(carriers, Is.Empty,
				"ACompletionRow entrega un portador: " + string.Join(", ", carriers)
				+ ". Lo que sale de la fila son palabras y una comparacion.");
		}

		private static IEnumerable<MethodInfo> EveryMemberOfTheRow()
		{
			return typeof(ACompletionRow).GetMethods(
				BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public
					| BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
		}

		private static MaintenanceSchedule ATimedComponentCompletedOnce()
		{
			MaintenanceSchedule schedule =
				new MaintenanceSchedule(TheVin, UsageProfileCode.Standard);

			schedule.SeedPlanOperation(
				OperationKindCode.TimedComponent, IntervalBasisCode.TimeOnly,
				TheTimeLimitInDays, NoDistanceLimit, DistanceUnitCode.UnitA,
				true, false,
				TheSeededAnchor, TheSeededAnchorReading, DistanceUnitCode.UnitA);

			schedule.SeedPlanOperation(
				OperationKindCode.FluidChange, IntervalBasisCode.TimeAndDistance,
				TheTimeLimitInDays, TheDistanceLimit, DistanceUnitCode.UnitA,
				true, false,
				TheSeededAnchor, TheSeededAnchorReading, DistanceUnitCode.UnitA);

			schedule.CompleteOperation(
				OperationKindCode.TimedComponent, TheCompletion,
				TheReadingThatIsReceivedAndDiscarded, DistanceUnitCode.UnitA);

			schedule.CompleteOperation(
				OperationKindCode.FluidChange, new DateTime(2025, 4, 15, 9, 20, 0),
				TheDistanceCompletionReading, DistanceUnitCode.UnitA);

			return schedule;
		}
	}
}
