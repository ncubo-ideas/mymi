using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using DealershipCar.App;
using DealershipCar.Mcp;
using DealershipCar.Mcp.Tools;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LA PARTE B DEL SLICE EN EL BORDE DEL ASISTENTE. "Que me han hecho" le llega como FILAS, cada una
	// con su clase, su instante y su lectura del odometro O la constancia de que no hay -- nunca un
	// cero y nunca una celda vacia.
	//
	// NINGUNA DE LAS CINCO LECTURAS DE LA PARTE B TOMA UN INSTANTE, asi que esta respuesta no depende
	// del reloj y todo se afirma POR LITERAL contra lo que se le paso a CompleteOperation. Es lo
	// contrario de la parte A, donde la mitad de tiempo baja con el calendario.
	//
	// LAS FILAS SALEN DE LA LECTURA DE TODAS LAS CLASES Y NO DE LA DE UNA, y esa es la decision de
	// esta tarea (DEC-68). La de una clase contesta un SUBCONJUNTO de las mismas filas y le falta la
	// columna Kind -- que es justamente la mitad direccionable con la que el asistente puede filtrar
	// --, asi que ofrecer las dos pondria dos formas del mismo hecho en una superficie. Lo que la de
	// todas las clases NO da es la frase por clase, y para eso estan las tres lecturas por clase de
	// byOperation, donde nunca completada y no agendada son dos respuestas distintas (L7).
	[TestFixture]
	public sealed class TheAssistantGetsEveryCompletionAsARowWithItsInstantAndItsReading
	{
		private const string TheVin = "WVWZZZ1KZAW000008";

		private const string TheKindThatMeasuresDistance = "FluidChange";
		private const string TheKindMeasuredOnlyInTime = "TimedComponent";
		private const string TheKindScheduledAndNeverCompleted = "Diagnostic";
		private const string TheKindThatIsNotOnTheSchedule = "Inspection";

		private const string TheUnitEverythingIsIn = "UnitA";

		private const int TwelveMonthsInDays = 365;
		private const int TheDistanceLimit = 12345;

		private const int TheOdometerAtTheAnchor = 15000;

		// LAS TRES COMPLETACIONES, tal como se reciben. Los instantes y las lecturas no se cruzan
		// entre si ni con nada mas de este fixture (C60).
		private static readonly DateTime TheFirstCompletion =
			new DateTime(2025, 2, 18, 8, 15, 0, DateTimeKind.Utc);
		private const int TheOdometerAtTheFirst = 18200;

		private static readonly DateTime TheSecondCompletion =
			new DateTime(2025, 7, 9, 14, 40, 0, DateTimeKind.Utc);
		private const int TheOdometerAtTheSecond = 24700;

		private static readonly DateTime TheThirdCompletion =
			new DateTime(2026, 3, 3, 11, 20, 0, DateTimeKind.Utc);
		private const int TheOdometerAtTheThird = 33100;

		// Y LA DE LA CLASE QUE NO MIDE DISTANCIA, que llega con una lectura que el dominio descarta.
		private static readonly DateTime TheTimedCompletion =
			new DateTime(2025, 5, 21, 6, 30, 0, DateTimeKind.Utc);
		private const int TheOdometerThatWasReceivedAndDiscarded = 21000;

		private static readonly DateTime TheAnchor =
			new DateTime(2024, 11, 5, 0, 0, 0, DateTimeKind.Utc);

		private AScheduleOnARealJournal journal;

		[SetUp]
		public void Setup()
		{
			journal = new AScheduleOnARealJournal(TheVin, "Standard");

			journal.Api.SeedPlanOperation(
				TheKindThatMeasuresDistance, "TimeOrDistance",
				TwelveMonthsInDays, TheDistanceLimit, TheUnitEverythingIsIn, true, false,
				TheAnchor, TheOdometerAtTheAnchor, TheUnitEverythingIsIn);

			journal.Api.SeedPlanOperation(
				TheKindMeasuredOnlyInTime, "TimeOnly",
				TwelveMonthsInDays, 0, TheUnitEverythingIsIn, true, false,
				TheAnchor, TheOdometerAtTheAnchor, TheUnitEverythingIsIn);

			journal.Api.SeedPlanOperation(
				TheKindScheduledAndNeverCompleted, "TimeOnly",
				TwelveMonthsInDays, 0, TheUnitEverythingIsIn, false, false,
				TheAnchor, TheOdometerAtTheAnchor, TheUnitEverythingIsIn);
		}

		[TearDown]
		public void TearDown()
		{
			journal.Dispose();
		}

		// ===== LAS FILAS, POR LITERAL Y CON SU CONECTIVA ==================================

		[Test]
		public void EveryCompletionArrivesAsARowWithItsKindItsInstantAndItsReading()
		{
			FourCompletionsAcrossTwoKinds();

			MaintenanceHistory history = TheAssistantAsksWhatHasBeenDone();

			Assert.That(history.HasData, Is.True);
			Assert.That(history.Vin, Is.EqualTo(TheVin));
			Assert.That(history.Completions, Has.Count.EqualTo(4));

			// LA CLASE VIAJA POR EL NOMBRE DEL MIEMBRO -- la mitad direccionable -- y la frase la dice
			// en palabras.
			// EL ORDEN ES POR INSTANTE Y NO POR CLASE: la completacion de julio de la clase que mide
			// distancia va ANTES de la de mayo de la que no, que es lo que hace que las cuatro filas
			// sean un solo juego y no dos listas pegadas.
			Assert.That(TheColumn(history, each => each.Operation), Is.EqualTo(new[]
			{
				"FluidChange", "FluidChange", "TimedComponent", "FluidChange",
			}));

			Assert.That(TheColumn(history, each => each.CompletedAt), Is.EqualTo(new[]
			{
				"2026-03-03 11:20:00", "2025-07-09 14:40:00",
				"2025-05-21 06:30:00", "2025-02-18 08:15:00",
			}));

			Assert.That(TheColumn(history, each => each.Reading), Is.EqualTo(new[]
			{
				"33100 km", "24700 km", "sin lectura registrada", "18200 km",
			}));

			// LA FORMA FUERTE: cada frase lleva su conectiva, asi que una fila armada con el instante
			// de una y la lectura de otra se ve. Dos valores sueltos que se contienen no lo verian.
			Assert.That(TheColumn(history, each => each.Says), Is.EqualTo(new[]
			{
				"Cambio de fluido: se hizo el 2026-03-03 11:20:00 con el odómetro en 33100 km.",
				"Cambio de fluido: se hizo el 2025-07-09 14:40:00 con el odómetro en 24700 km.",
				"Componente por tiempo: se hizo el 2025-05-21 06:30:00, sin lectura del odómetro registrada.",
				"Cambio de fluido: se hizo el 2025-02-18 08:15:00 con el odómetro en 18200 km.",
			}));
		}

		// UNA COMPLETACION SIN LECTURA ES UNA FILA Y NO UN CERO, y la lectura que se recibio no se
		// recupera de ninguna parte: el ancla de solo instante no la guarda.
		[Test]
		public void ACompletionWithNoReadingSaysSoAndIsNeverAZero()
		{
			FourCompletionsAcrossTwoKinds();

			CompletedWork timed = TheRowOf(TheKindMeasuredOnlyInTime);

			Assert.That(timed.Reading, Is.EqualTo("sin lectura registrada"));
			Assert.That(timed.Reading, Is.Not.EqualTo("0"));
			Assert.That(timed.Reading, Is.Not.EqualTo("0 km"));
			Assert.That(timed.Reading, Is.Not.Empty);

			Assert.That(
				JsonSerializer.Serialize(TheAssistantAsksWhatHasBeenDone()),
				Does.Not.Contain(TheOdometerThatWasReceivedAndDiscarded.ToString()));

			// Y LA AUSENCIA NO ES GENERAL: en la misma respuesta, la clase que si mide distancia sigue
			// trayendo su lectura.
			Assert.That(TheRowOf(TheKindThatMeasuresDistance).Reading, Is.EqualTo("33100 km"));
		}

		// ===== LAS DOS AUSENCIAS DE L7, Y NO SON LA MISMA ================================

		// NUNCA COMPLETADA LA CURA UNA VISITA AL TALLER Y NO AGENDADA NO LA CURA NINGUNA. Es la
		// distincion que un booleano juntaria, y llega por el says de cada clase.
		[Test]
		public void TheTwoAbsencesArriveAsTwoDifferentSentencesAndNeitherIsInvented()
		{
			FourCompletionsAcrossTwoKinds();

			WorkOnOneKind never = TheSummaryOf(TheKindScheduledAndNeverCompleted);

			Assert.That(never.AnyOnRecord, Is.False);
			Assert.That(never.Count, Is.EqualTo(0));
			Assert.That(
				never.Says,
				Is.EqualTo("Diagnóstico: no hay ninguna completación registrada para este vehículo."));

			WorkOnOneKind notScheduled = TheSummaryOf(TheKindThatIsNotOnTheSchedule);

			Assert.That(notScheduled.AnyOnRecord, Is.False);
			Assert.That(
				notScheduled.Says,
				Is.EqualTo(
					"Este vehículo no tiene agendada ninguna operación de revisión, así que no hay "
					+ "ninguna completación que registrar."));

			Assert.That(notScheduled.Says, Is.Not.EqualTo(never.Says),
				"Las dos ausencias son dos respuestas: la de arriba la cura una visita al taller y "
				+ "esta no la cura ninguna.");
		}

		// Y LA CLASE COMPLETADA TRAE SU PROPIA FRASE, con su conectiva. Sin este renglon, una caratula
		// que contestara la misma frase para las nueve pasaria las dos de arriba.
		[Test]
		public void AndTheKindThatWasCompletedCarriesItsOwnSentenceWithItsConnective()
		{
			FourCompletionsAcrossTwoKinds();

			WorkOnOneKind fluid = TheSummaryOf(TheKindThatMeasuresDistance);

			Assert.That(fluid.AnyOnRecord, Is.True);
			Assert.That(fluid.Count, Is.EqualTo(3));
			Assert.That(
				fluid.Says,
				Does.Contain("La más reciente se hizo el 2026-03-03 11:20:00 con el odómetro en 33100 km"),
				"La frase del juego nombra la MISMA completacion que la primera fila: si el camino se "
				+ "corriera un eslabon, las dos se corren juntas y ninguna lo delata sola.");

			Assert.That(TheSummaryOf(TheKindMeasuredOnlyInTime).Count, Is.EqualTo(1));
			Assert.That(TheSummaryOf(TheKindMeasuredOnlyInTime).Says, Is.Not.EqualTo(fluid.Says));
		}

		// LAS NUEVE CLASES TRAEN SU RESUMEN, agendadas o no. Filtrar por agendada dejaria la segunda
		// ausencia sin llegar, y es la unica que dice que este carro no tiene esa parte.
		[Test]
		public void EveryOneOfTheNineCanonKindsBringsItsOwnSummary()
		{
			MaintenanceHistory history = TheAssistantAsksWhatHasBeenDone();

			Assert.That(history.ByOperation, Has.Count.EqualTo(9));

			var named = new List<string>();
			foreach (WorkOnOneKind each in history.ByOperation) named.Add(each.Operation);

			Assert.That(named, Is.EquivalentTo(new[]
			{
				"Inspection", "FluidChange", "FilterChange", "WearPartReplacement", "TimedComponent",
				"SoftwareUpdate", "RecallRemedy", "Diagnostic", "Repair",
			}));

			foreach (WorkOnOneKind each in history.ByOperation)
				Assert.That(each.Says, Is.Not.Empty, each.Operation);
		}

		// LA CUENTA DEL DOMINIO Y LAS FILAS CONCUERDAN, sumadas EN LA PRUEBA (regla 4). Las dos salen
		// de lo que se recibio y no una de la otra.
		[Test]
		public void TheCountsAgreeWithTheRowsAndBothComeFromWhatWasReceived()
		{
			FourCompletionsAcrossTwoKinds();

			MaintenanceHistory history = TheAssistantAsksWhatHasBeenDone();

			int overTheKinds = 0;

			foreach (WorkOnOneKind each in history.ByOperation) overTheKinds += each.Count;

			Assert.That(history.Completions, Has.Count.EqualTo(overTheKinds));
			Assert.That(overTheKinds, Is.EqualTo(4));

			var fromTheRows = new List<string>();

			foreach (CompletedWork each in history.Completions)
				if (each.Operation == TheKindThatMeasuresDistance) fromTheRows.Add(each.CompletedAt);

			Assert.That(fromTheRows, Has.Count.EqualTo(TheSummaryOf(TheKindThatMeasuresDistance).Count));
		}

		// ===== NADA COMPLETADO ES UNA RESPUESTA, Y NO SE LE INVENTA UNA FRASE ============

		// LA LECTURA DE TODAS LAS CLASES NO TIENE FRASE DE AUSENCIA, POR DECISION DE ALCANCE, y NI EL
		// DTO NI LA TOOL COMPONEN UNA. Lo que se dice es donde esta la razon -- por clase -- y eso es
		// una frase sobre la RESPUESTA y no sobre el vehiculo.
		[Test]
		public void NothingCompletedAnswersAnEmptyListAndNoSentenceAboutTheVehicleIsInvented()
		{
			MaintenanceHistory history = TheAssistantAsksWhatHasBeenDone();

			Assert.That(history.HasData, Is.True);
			Assert.That(history.Completions, Is.Empty);

			Assert.That(history.Detail, Does.Contain("una lista vacía es una respuesta"));
			Assert.That(history.Detail, Does.Contain("POR OPERACIÓN"));

			// Y NINGUN MIEMBRO DE LA PUERTA CONTESTA UNA FRASE DEL HISTORIAL ENTERO, que es lo que
			// impide que alguien la fabrique mas arriba. Es la mitad que MS-2 dejo fijada, re-derivada
			// aca sobre la superficie que la habria querido.
			foreach (MethodInfo each in typeof(DealershipCarAPI).GetMethods(
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
				if (each.Name.Contains("Completion", StringComparison.Ordinal)
					&& each.ReturnType == typeof(string))
					Assert.That(each.GetParameters(), Is.Not.Empty,
						"'" + each.Name + "' contesta una frase del historial sin que nadie nombre una "
						+ "clase. La lectura de todas las clases no tiene frase propia a proposito.");
		}

		// ===== LA DECISION: LA FILA DE UNA CLASE NO SE OFRECE DOS VECES ==================

		// LA RESPUESTA OFRECE UNA SOLA FORMA DE CADA FILA (DEC-68). CompletionsOf(kind) contesta las
		// MISMAS filas filtradas y sin la columna Kind, asi que ofrecerlas al lado seria una segunda
		// forma del mismo hecho en una superficie -- lo que DEC-61 decide del otro lado para los dias
		// de una estadia.
		//
		// SE MIDE POR LA FUENTE Y NO POR LA RESPUESTA, porque una respuesta con las dos formas es
		// indistinguible de una con una sola cuando el registro tiene una sola clase completada.
		[Test]
		public void TheSingleKindRowReadIsNotCalledAtTheAssistantsEdgeAndTheReasonIsWritten()
		{
			Assert.That(
				TheSourceOfTheAssistantsTools(), Does.Not.Contain("CompletionsOf("),
				"La caratula del asistente llamo CompletionsOf(kind). Sus filas son las mismas que ya "
				+ "trae CompletionsOfEveryKind(), filtradas y sin la columna Kind: dos formas del "
				+ "mismo hecho en una superficie. Si la decision cambio, se escribe y se dice por que.");

			Assert.That(
				TheSourceOfTheAssistantsTools(), Does.Contain("CompletionsOfEveryKind()"),
				"EL CONTROL: la que si se llama tiene que estar. Sin esto, un barrido que dejo de leer "
				+ "la fuente cumpliria el renglon de arriba.");

			// Y LA PUERTA SIGUE TENIENDO LAS DOS. No se retira ninguna: la de una clase es de la
			// consola y del tablero, que preguntan por una.
			Assert.That(
				typeof(DealershipCarAPI).GetMethod("CompletionsOf"), Is.Not.Null,
				"La lectura por clase se dejo de ofrecer AL ASISTENTE, no se retiro de la puerta.");
		}

		// ------------------------------------------------------ el aparejo

		private MaintenanceHistory TheAssistantAsksWhatHasBeenDone()
		{
			return new DealershipCarTools(new JournalReader(journal.Folder, ""))
				.WhatHasBeenDoneToMyCar();
		}

		private void FourCompletionsAcrossTwoKinds()
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
			journal.Api.CompleteOperation(
				TheKindMeasuredOnlyInTime, TheTimedCompletion,
				TheOdometerThatWasReceivedAndDiscarded, TheUnitEverythingIsIn);
		}

		private CompletedWork TheRowOf(string kind)
		{
			foreach (CompletedWork each in TheAssistantAsksWhatHasBeenDone().Completions)
				if (each.Operation == kind) return each;

			throw new InvalidOperationException("La respuesta no trajo ninguna fila de " + kind + ".");
		}

		private WorkOnOneKind TheSummaryOf(string kind)
		{
			foreach (WorkOnOneKind each in TheAssistantAsksWhatHasBeenDone().ByOperation)
				if (each.Operation == kind) return each;

			throw new InvalidOperationException("La respuesta no trajo el resumen de " + kind + ".");
		}

		private static string[] TheColumn(
			MaintenanceHistory history, Func<CompletedWork, string> cell)
		{
			var column = new List<string>();

			foreach (CompletedWork each in history.Completions) column.Add(cell(each));

			return column.ToArray();
		}

		// La fuente de la caratula del asistente, leida COMO TEXTO: lo que hay que mirar es cual
		// lectura se llama, y eso no sobrevive a la compilacion.
		private static string TheSourceOfTheAssistantsTools()
		{
			var whole = new System.Text.StringBuilder();

			foreach (string file in TheCodeOfTheShellsAsData.EveryFileOf(TheCodeOfTheShellsAsData.Mcp))
				whole.Append(System.IO.File.ReadAllText(file));

			return whole.ToString();
		}
	}
}
