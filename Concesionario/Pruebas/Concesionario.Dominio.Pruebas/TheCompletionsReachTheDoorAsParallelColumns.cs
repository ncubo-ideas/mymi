using System;
using System.Collections.Generic;
using System.Reflection;
using Concesionario.App;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LA PARTE B DEL SLICE, DEL LADO DEL WIRE. Las cinco operaciones de la puerta que dicen lo que se
	// le hizo al vehiculo: dos preguntas, una frase y DOS registros de columnas paralelas.
	//
	// SON DOS REGISTROS Y NO UNO, Y ESA ES LA DECISION QUE ESTA TAREA TOMA. El dominio contesta TRES
	// columnas cuando la clase la puso quien pregunta y CUATRO cuando el juego es de todas las clases,
	// porque para la lectura de una sola clase la columna Kind es CONSTANTE POR CONSTRUCCION. La
	// puerta podia repetir el argumento que recibio dentro de una columna constante; no lo hace, y el
	// motivo esta abajo, en TheSingleKindRecordHasNoKindColumnAndItsRowsStillNameTheirKind.
	//
	// TODO SE AFIRMA CONTRA LO QUE SE RECIBIO, POR LITERAL (E49 obligacion 2): los instantes y las
	// lecturas que se le pasaron a CompleteOperation, y no lo que otra respuesta derivada diga. Dos
	// consumidores de un mismo resolvedor se ponen de acuerdo por construccion, incluso cuando el
	// resolvedor esta uniformemente equivocado.
	//
	// Y LAS FRASES SE AFIRMAN CON SU CONECTIVA, no como dos valores sueltos que se contienen: una
	// frase que cruzara el instante de una fila con la lectura de otra contendria los dos valores
	// igual. La forma fuerte es "se hizo el <instante> con el odómetro en <lectura>".
	[TestFixture]
	public sealed class TheCompletionsReachTheDoorAsParallelColumns
	{
		private const string TheVin = "JN1AZ4EH7DM000002";
		private const string TheKindThatMeasuresDistance = "FluidChange";
		private const string TheKindMeasuredOnlyInTime = "TimedComponent";
		private const string TheUnitEverythingIsIn = "UnitA";

		private const int TwelveMonthsInDays = 365;
		private const int TheDistanceLimit = 12345;

		private const int TheOdometerAtTheAnchor = 20000;

		// LAS TRES COMPLETACIONES DE LA CLASE QUE MIDE DISTANCIA, tal como se reciben. Los instantes y
		// las lecturas no se cruzan entre si ni con nada mas de este fixture (C60).
		private static readonly DateTime TheFirstCompletion =
			new DateTime(2025, 4, 11, 12, 0, 0, DateTimeKind.Utc);
		private const int TheOdometerAtTheFirst = 22500;

		private static readonly DateTime TheSecondCompletion =
			new DateTime(2025, 8, 22, 9, 30, 0, DateTimeKind.Utc);
		private const int TheOdometerAtTheSecond = 27300;

		private static readonly DateTime TheThirdCompletion =
			new DateTime(2026, 1, 15, 16, 5, 0, DateTimeKind.Utc);
		private const int TheOdometerAtTheThird = 31900;

		// Y LA DE LA CLASE QUE NO MIDE DISTANCIA, que llega con una lectura que el dominio descarta.
		private static readonly DateTime TheTimedCompletion =
			new DateTime(2025, 11, 5, 7, 45, 0, DateTimeKind.Utc);
		private const int TheOdometerThatWasReceivedAndDiscarded = 29100;

		private static readonly DateTime TheAnchor =
			new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Utc);

		private AScheduleOnARealJournal journal;

		[SetUp]
		public void Setup()
		{
			journal = new AScheduleOnARealJournal(TheVin, "Standard");
		}

		[TearDown]
		public void TearDown()
		{
			journal.Dispose();
		}

		// ===== EL PRIMER ESTADO DEL REGISTRO: NADA COMPLETADO =============================

		[Test]
		public void NothingCompletedAnswersTheAbsenceAndEmptyParallelColumns()
		{
			AnOperationThatMeasuresDistance();

			Assert.That(journal.Api.HasAnyCompletion(TheKindThatMeasuresDistance), Is.False);
			Assert.That(journal.Api.CompletionCount(TheKindThatMeasuresDistance), Is.EqualTo(0));

			Assert.That(
				journal.Api.DescribeCompletions(TheKindThatMeasuresDistance),
				Is.EqualTo("Cambio de fluido: no hay ninguna completación registrada para este vehículo."));

			CompletionRecord none = journal.Api.CompletionsOf(TheKindThatMeasuresDistance);

			Assert.That(none.CompletedAt, Is.Empty);
			Assert.That(none.Reading, Is.Empty);
			Assert.That(none.Says, Is.Empty);
		}

		// LA CLASE QUE NO ESTA EN LA AGENDA CONTESTA OTRA FRASE, y no es la misma ausencia: nunca
		// completada la cura una visita al taller y no agendada no la cura ninguna.
		[Test]
		public void AKindThatIsNotOnTheScheduleSaysThatInsteadAndItIsNotTheSameAbsence()
		{
			Assert.That(journal.Api.HasAnyCompletion("Inspection"), Is.False);

			Assert.That(
				journal.Api.DescribeCompletions("Inspection"),
				Is.EqualTo(
					"Este vehículo no tiene agendada ninguna operación de revisión, así que no hay "
					+ "ninguna completación que registrar."));

			Assert.That(
				journal.Api.DescribeCompletions("Inspection"),
				Is.Not.EqualTo(journal.Api.DescribeCompletions(TheKindThatMeasuresDistance)));
		}

		// ===== EL SEGUNDO ESTADO: TRES COMPLETACIONES =====================================

		[Test]
		public void ThreeCompletionsComeBackAsThreeParallelRowsNewestFirst()
		{
			AnOperationThatMeasuresDistance();
			ThreeCompletionsOfIt();

			Assert.That(journal.Api.HasAnyCompletion(TheKindThatMeasuresDistance), Is.True);
			Assert.That(journal.Api.CompletionCount(TheKindThatMeasuresDistance), Is.EqualTo(3));

			CompletionRecord rows = journal.Api.CompletionsOf(TheKindThatMeasuresDistance);

			Assert.That(rows.CompletedAt, Is.EqualTo(new[]
			{
				"2026-01-15 16:05:00", "2025-08-22 09:30:00", "2025-04-11 12:00:00",
			}));

			Assert.That(rows.Reading, Is.EqualTo(new[]
			{
				"31900 km", "27300 km", "22500 km",
			}));

			// LA FORMA FUERTE: cada frase lleva su conectiva, asi que una columna cruzada con la de al
			// lado se ve. Dos valores sueltos que se contienen no lo verian.
			Assert.That(rows.Says, Is.EqualTo(new[]
			{
				"Cambio de fluido: se hizo el 2026-01-15 16:05:00 con el odómetro en 31900 km.",
				"Cambio de fluido: se hizo el 2025-08-22 09:30:00 con el odómetro en 27300 km.",
				"Cambio de fluido: se hizo el 2025-04-11 12:00:00 con el odómetro en 22500 km.",
			}));
		}

		// LAS TRES COLUMNAS TIENEN EL MISMO LARGO SIEMPRE, y no porque tres miembros caminen la misma
		// lista por costumbre: el registro se niega a nacer con columnas de largos distintos.
		[Test]
		public void TheThreeColumnsAreParallelInBothStatesOfTheRecord()
		{
			AnOperationThatMeasuresDistance();

			CompletionRecord empty = journal.Api.CompletionsOf(TheKindThatMeasuresDistance);

			Assert.That(empty.Reading, Has.Length.EqualTo(empty.CompletedAt.Length));
			Assert.That(empty.Says, Has.Length.EqualTo(empty.CompletedAt.Length));

			ThreeCompletionsOfIt();

			CompletionRecord full = journal.Api.CompletionsOf(TheKindThatMeasuresDistance);

			Assert.That(full.CompletedAt, Has.Length.EqualTo(3));
			Assert.That(full.Reading, Has.Length.EqualTo(3));
			Assert.That(full.Says, Has.Length.EqualTo(3));
		}

		// ===== LA DECISION DE ESTA TAREA: LA COLUMNA Kind =================================

		// EL REGISTRO DE UNA SOLA CLASE NO LLEVA COLUMNA Kind, y sus filas siguen diciendo de que
		// clase son -- adentro de su frase, que es donde la fila se vuelve autodescriptiva. Repetir el
		// argumento recibido en una columna constante habria puesto en el registro una columna que el
		// dominio no contesta: seria la unica columna del modulo cuyo contenido lo compone la puerta,
		// y el chequeo de paralelismo del DTO no diria nada sobre ella.
		[Test]
		public void TheSingleKindRecordHasNoKindColumnAndItsRowsStillNameTheirKind()
		{
			AnOperationThatMeasuresDistance();
			ThreeCompletionsOfIt();

			Assert.That(TheColumnsOf(typeof(CompletionRecord)), Is.EquivalentTo(new[]
			{
				"CompletedAt", "Reading", "Says",
			}),
				"El registro de una sola clase gano o perdio una columna. La clase la puso quien "
				+ "pregunta: una columna que repitiera el argumento recibido no la contesta el dominio.");

			foreach (string said in journal.Api.CompletionsOf(TheKindThatMeasuresDistance).Says)
				Assert.That(said, Does.StartWith("Cambio de fluido: "));
		}

		// Y EL DE TODAS LAS CLASES SI LA LLEVA, porque ahi la clase cambia de fila en fila y ninguna
		// otra columna la dice de forma direccionable. La columna trae el NOMBRE del miembro -- lo que
		// quien lee puede volver a preguntar -- y la frase lo dice en palabras.
		[Test]
		public void TheEveryKindRecordCarriesTheKindColumnAndTheSentenceSaysItInWords()
		{
			AnOperationThatMeasuresDistance();
			AnOperationMeasuredOnlyInTime();
			ThreeCompletionsOfIt();
			TheTimedCompletionOfIt();

			Assert.That(TheColumnsOf(typeof(EveryKindCompletionRecord)), Is.EquivalentTo(new[]
			{
				"Kind", "CompletedAt", "Reading", "Says",
			}));

			EveryKindCompletionRecord rows = journal.Api.CompletionsOfEveryKind();

			Assert.That(rows.Kind, Is.EqualTo(new[]
			{
				"FluidChange", "TimedComponent", "FluidChange", "FluidChange",
			}));

			foreach (string each in rows.Kind)
				Assert.That(each, Is.Not.EqualTo("Cambio de fluido").And.Not.EqualTo("Componente por tiempo"),
					"La columna dice el NOMBRE del miembro y no como se dice: es la mitad "
					+ "direccionable, la que quien lee puede volver a pasar.");

			Assert.That(rows.Says[0], Does.StartWith("Cambio de fluido: "));
			Assert.That(rows.Says[1], Does.StartWith("Componente por tiempo: "));
		}

		// ===== LAS CUATRO COLUMNAS DEL JUEGO ENTERO, SIN CRUZARSE =========================

		[Test]
		public void TheEveryKindRowsCrossNoColumnWithTheOneBesideIt()
		{
			AnOperationThatMeasuresDistance();
			AnOperationMeasuredOnlyInTime();
			ThreeCompletionsOfIt();
			TheTimedCompletionOfIt();

			EveryKindCompletionRecord rows = journal.Api.CompletionsOfEveryKind();

			Assert.That(rows.CompletedAt, Is.EqualTo(new[]
			{
				"2026-01-15 16:05:00", "2025-11-05 07:45:00",
				"2025-08-22 09:30:00", "2025-04-11 12:00:00",
			}));

			Assert.That(rows.Reading, Is.EqualTo(new[]
			{
				"31900 km", "sin lectura registrada", "27300 km", "22500 km",
			}));

			Assert.That(rows.Says, Is.EqualTo(new[]
			{
				"Cambio de fluido: se hizo el 2026-01-15 16:05:00 con el odómetro en 31900 km.",
				"Componente por tiempo: se hizo el 2025-11-05 07:45:00, sin lectura del odómetro registrada.",
				"Cambio de fluido: se hizo el 2025-08-22 09:30:00 con el odómetro en 27300 km.",
				"Cambio de fluido: se hizo el 2025-04-11 12:00:00 con el odómetro en 22500 km.",
			}));
		}

		// UNA COMPLETACION SIN LECTURA ES UNA FILA Y NO UN CERO, y la ausencia no es general: en el
		// mismo juego, la clase que si mide distancia sigue trayendo su lectura.
		[Test]
		public void ACompletionWithNoReadingSaysSoAndIsNeverAZero()
		{
			AnOperationMeasuredOnlyInTime();
			TheTimedCompletionOfIt();

			CompletionRecord rows = journal.Api.CompletionsOf(TheKindMeasuredOnlyInTime);

			Assert.That(rows.Reading, Is.EqualTo(new[] { "sin lectura registrada" }));
			Assert.That(rows.Reading[0], Is.Not.EqualTo("0"));
			Assert.That(rows.Reading[0], Is.Not.EqualTo("0 km"));
			Assert.That(rows.Reading[0], Is.Not.Empty);

			// Y LA LECTURA QUE SE RECIBIO NO SE RECUPERA DE NINGUNA PARTE: el dominio la descarto.
			foreach (string said in rows.Says)
				Assert.That(said, Does.Not.Contain(TheOdometerThatWasReceivedAndDiscarded.ToString()));
		}

		// ===== LA CUENTA Y LAS FILAS SE PONEN DE ACUERDO ==================================

		// La regla 4 de la convencion: el total del dominio y la suma de las filas concuerdan, sumadas
		// EN LA PRUEBA. Las dos salen de lo que se recibio y no una de la otra.
		[Test]
		public void TheCountAgreesWithTheRowsAndBothComeFromWhatWasReceived()
		{
			AnOperationThatMeasuresDistance();
			AnOperationMeasuredOnlyInTime();
			ThreeCompletionsOfIt();
			TheTimedCompletionOfIt();

			Assert.That(journal.Api.CompletionCount(TheKindThatMeasuresDistance), Is.EqualTo(3));
			Assert.That(
				journal.Api.CompletionsOf(TheKindThatMeasuresDistance).Says, Has.Length.EqualTo(3));

			Assert.That(journal.Api.CompletionCount(TheKindMeasuredOnlyInTime), Is.EqualTo(1));

			int overTheKinds = 0;

			foreach (string kind in Enum.GetNames(typeof(OperationKindCode)))
				overTheKinds += journal.Api.CompletionCount(kind);

			Assert.That(
				journal.Api.CompletionsOfEveryKind().Says, Has.Length.EqualTo(overTheKinds),
				"El juego entero tiene tantas filas como completaciones cuentan las clases una por una.");
		}

		// ===== NINGUNA DE LAS CINCO TOMA UN INSTANTE =====================================

		// La cadena es la que es y cada fila trae su propio instante: no hay ningun momento contra el
		// que preguntarla, asi que no hay nada que guardar en la puerta tampoco. Es lo contrario de la
		// parte A, que hereda un rechazo del dominio en cada una de sus diez.
		[Test]
		public void NoReadOfTheHistoryTakesAnInstantAtTheDoorEither()
		{
			foreach (string named in new[]
			{
				"HasAnyCompletion", "CompletionCount", "DescribeCompletions",
				"CompletionsOf", "CompletionsOfEveryKind",
			})
				foreach (ParameterInfo each in TheDoorsMember(named).GetParameters())
					Assert.That(each.ParameterType, Is.Not.EqualTo(typeof(DateTime)),
						$"'{named}' gano un instante. El historial no depende de ninguno: cada fila trae "
						+ "el suyo.");
		}

		// ===== EL APAREJO =================================================================

		private void AnOperationThatMeasuresDistance()
		{
			journal.Api.SeedPlanOperation(
				TheKindThatMeasuresDistance, "TimeOrDistance",
				TwelveMonthsInDays, TheDistanceLimit, TheUnitEverythingIsIn, true, false,
				TheAnchor, TheOdometerAtTheAnchor, TheUnitEverythingIsIn);
		}

		private void AnOperationMeasuredOnlyInTime()
		{
			journal.Api.SeedPlanOperation(
				TheKindMeasuredOnlyInTime, "TimeOnly",
				TwelveMonthsInDays, 0, TheUnitEverythingIsIn, true, false,
				TheAnchor, TheOdometerAtTheAnchor, TheUnitEverythingIsIn);
		}

		private void ThreeCompletionsOfIt()
		{
			journal.Api.CompleteOperation(
				TheKindThatMeasuresDistance, TheFirstCompletion, TheOdometerAtTheFirst,
				TheUnitEverythingIsIn);
			journal.Api.CompleteOperation(
				TheKindThatMeasuresDistance, TheSecondCompletion, TheOdometerAtTheSecond,
				TheUnitEverythingIsIn);
			journal.Api.CompleteOperation(
				TheKindThatMeasuresDistance, TheThirdCompletion, TheOdometerAtTheThird,
				TheUnitEverythingIsIn);
		}

		private void TheTimedCompletionOfIt()
		{
			journal.Api.CompleteOperation(
				TheKindMeasuredOnlyInTime, TheTimedCompletion,
				TheOdometerThatWasReceivedAndDiscarded, TheUnitEverythingIsIn);
		}

		private static List<string> TheColumnsOf(Type record)
		{
			var columns = new List<string>();

			foreach (PropertyInfo each in record.GetProperties(
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
				columns.Add(each.Name);

			return columns;
		}

		private static MethodInfo TheDoorsMember(string name)
		{
			var found = new List<MethodInfo>();

			foreach (MethodInfo each in typeof(DealershipCarAPI).GetMethods(
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
				if (each.Name == name) found.Add(each);

			Assert.That(found, Has.Count.EqualTo(1),
				$"La puerta tiene {found.Count} miembros llamados '{name}'.");

			return found[0];
		}
	}
}
