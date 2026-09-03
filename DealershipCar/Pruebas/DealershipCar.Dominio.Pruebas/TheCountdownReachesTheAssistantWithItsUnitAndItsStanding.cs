using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using DealershipCar.Mcp;
using DealershipCar.Mcp.Tools;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LA PARTE A DEL SLICE EN EL BORDE DEL ASISTENTE. "Cuanto me falta" le llega al asistente entero
	// -- el monto, su unidad, en cual de las siete situaciones esta cada mitad, y la frase que compuso
	// el dominio -- y NINGUN parametro de la tool le pide una lectura, un odometro ni un instante.
	//
	// EL ESCENARIO SE MONTA EN EL REGISTRO Y NO EN EL RELOJ, y no es una comodidad: la tool lee el
	// reloj del anfitrion (DEC-43), asi que la prueba no puede elegir el instante. La mitad de
	// DISTANCIA no se mueve con el calendario -- lo recorrido es la lectura menos el ancla -- asi que
	// se afirma POR LITERAL; la de TIEMPO baja sola todos los dias, asi que se afirma por su FORMA y
	// su limite se pone en cien anos para que la situacion siga siendo StillAhead. Es la misma forma
	// que TheStandingReachesTheAssistantAndNoToolAsksForIt usa para los meses del sobre.
	//
	// LOS NUMEROS SON SENTINELAS (C60): 23456 de limite, 40000 en el ancla, 46000 de lectura, y lo que
	// queda es 17456. Ninguno se cruza con otro de este fixture ni con un conteo.
	[TestFixture]
	public sealed class TheCountdownReachesTheAssistantWithItsUnitAndItsStanding
	{
		private const string TheVin = "5NPE24AF8FH000006";

		private const string TheKindMeasuredBothWays = "FluidChange";
		private const string TheKindMeasuredOnlyInTime = "TimedComponent";
		private const string TheKindOutsideEveryLimit = "SoftwareUpdate";
		private const string TheKindThatIsNotOnTheSchedule = "Inspection";

		private const string TheUnitEverythingIsIn = "UnitA";
		private const string TheUnitAsAPersonReadsIt = "km";

		private const int TheDistanceLimitNothingElseCouldAnswer = 23456;
		private const int TheOdometerAtTheAnchor = 40000;
		private const int TheOdometerObservedLater = 46000;

		// Lo que la resta tiene que dar, escrito y no calculado: repetir la resta afirmaria la
		// aritmetica de la prueba y no la del dominio.
		private const int TheDistanceThatIsLeft = 17456;

		// CIEN ANOS. El plazo tiene que sobrevivir al reloj de cualquier corrida futura, porque la
		// tool no acepta un instante. Si esto se pone rojo despues de 2125, el escenario cumplio su
		// plazo y hay que volver a montarlo.
		private const int TheDayLimitThatOutlivesEveryRun = 36500;

		private static readonly DateTime TheAnchor =
			new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheDayItWasObserved =
			new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);

		private AScheduleOnARealJournal journal;

		[SetUp]
		public void Setup()
		{
			journal = new AScheduleOnARealJournal(TheVin, "Standard");

			journal.Api.SeedPlanOperation(
				TheKindMeasuredBothWays, "TimeOrDistance",
				TheDayLimitThatOutlivesEveryRun, TheDistanceLimitNothingElseCouldAnswer,
				TheUnitEverythingIsIn, true, false,
				TheAnchor, TheOdometerAtTheAnchor, TheUnitEverythingIsIn);

			journal.Api.SeedPlanOperation(
				TheKindMeasuredOnlyInTime, "TimeOnly",
				TheDayLimitThatOutlivesEveryRun, 0,
				TheUnitEverythingIsIn, true, false,
				TheAnchor, TheOdometerAtTheAnchor, TheUnitEverythingIsIn);

			journal.Api.SeedPlanOperation(
				TheKindOutsideEveryLimit, "ConditionOnly", 0, 0,
				TheUnitEverythingIsIn, false, false,
				TheAnchor, TheOdometerAtTheAnchor, TheUnitEverythingIsIn);

			journal.Api.RecordReading(
				TheOdometerObservedLater, TheUnitEverythingIsIn, TheDayItWasObserved, "Telemetry");
		}

		[TearDown]
		public void TearDown()
		{
			journal.Dispose();
		}

		// ===== LA CUENTA ATRAS LLEGA ENTERA ===============================================

		// EL MONTO CON SU UNIDAD Y SU SITUACION AL LADO, que es la condicion exacta con la que DEC-63
		// admitio un resto como numero en la puerta. La misma condicion se cumple aca o el numero no
		// tendria que cruzar: un monto sin su unidad es ambiguo entre las dos unidades de distancia, y
		// un monto sin su medibilidad se lee como medido.
		[Test]
		public void TheCountdownArrivesWithItsAmountItsUnitAndItsStanding()
		{
			ServiceCountdown row = TheRowFor(TheKindMeasuredBothWays);

			Assert.That(row.Operation, Is.EqualTo(TheKindMeasuredBothWays),
				"La operacion viaja por el NOMBRE del miembro, que es la mitad direccionable.");

			Assert.That(row.DistanceCanBeSaid, Is.True);
			Assert.That(row.DistanceRemaining, Is.EqualTo(TheDistanceThatIsLeft));
			Assert.That(row.DistanceUnit, Is.EqualTo(TheUnitAsAPersonReadsIt),
				"Y la unidad como la lee una persona, no como la nombra el wire.");
			Assert.That(row.DistanceStanding, Is.EqualTo("StillAhead"));
			Assert.That(
				row.DistanceSays,
				Is.EqualTo("distancia: te faltan " + TheDistanceThatIsLeft + " km"),
				"La frase entera y no dos pedazos: dos valores sueltos que se contienen no "
				+ "distinguen una frase cruzada de una correcta.");

			// LA MITAD DE TIEMPO POR SU FORMA, porque los dias que quedan bajan solos con el
			// calendario y afirmar el numero seria afirmar el dia en que corre la suite.
			Assert.That(row.DaysCanBeSaid, Is.True);
			Assert.That(row.TimeStanding, Is.EqualTo("StillAhead"));
			Assert.That(row.DaysRemaining, Is.GreaterThan(0));
			Assert.That(row.DaysSays, Does.StartWith("tiempo: te faltan "));
			Assert.That(row.DaysSays, Does.Contain(" días"));
		}

		// G1 SOBREVIVE HASTA EL BORDE DEL ASISTENTE: la frase dice LAS DOS dimensiones y la caratula no
		// elige ninguna. Elegir dejaria el numero que el asistente cita discrepando del motivo del
		// estado para quien lea los dos juntos.
		[Test]
		public void TheCountdownSaysBothDimensionsAndNoFieldOfTheAnswerPicksOne()
		{
			ServiceCountdown row = TheRowFor(TheKindMeasuredBothWays);

			Assert.That(row.Says, Does.Contain("distancia: te faltan " + TheDistanceThatIsLeft + " km"));
			Assert.That(row.Says, Does.Contain("tiempo: te faltan "));
			Assert.That(row.Says, Does.Contain(";"),
				"Las dos mitades en UNA frase, separadas por la conectiva del dominio.");

			foreach (string tempting in new[] { "nearer", "nearest", "moreAdvanced", "whichDecides" })
				Assert.That(
					JsonSerializer.Serialize(TheAssistantAsksWhatIsLeft()),
					Does.Not.Contain(tempting),
					"La respuesta gano un campo que ELIGE una dimension: '" + tempting + "'. Los dos "
					+ "miembros que ordenan dimensiones existen en el dominio y ordenan para el "
					+ "ESTADO, no para la frase.");
		}

		// LAS NUEVE CLASES, NO LAS AGENDADAS. Una clase que este vehiculo nunca tuvo agendada es la
		// SEPTIMA situacion y el dominio la dice en palabras; filtrarla la dejaria sin llegar, que se
		// lee igual que un hueco.
		[Test]
		public void EveryOneOfTheNineCanonKindsGetsARowIncludingTheOnesNobodyScheduled()
		{
			MaintenanceCountdown answer = TheAssistantAsksWhatIsLeft();

			Assert.That(answer.HasData, Is.True);
			Assert.That(answer.Vin, Is.EqualTo(TheVin));
			Assert.That(answer.Operations, Has.Count.EqualTo(9));

			var named = new List<string>();
			foreach (ServiceCountdown each in answer.Operations) named.Add(each.Operation);

			Assert.That(named, Is.EquivalentTo(new[]
			{
				"Inspection", "FluidChange", "FilterChange", "WearPartReplacement", "TimedComponent",
				"SoftwareUpdate", "RecallRemedy", "Diagnostic", "Repair",
			}));

			ServiceCountdown notScheduled = TheRowFor(TheKindThatIsNotOnTheSchedule);

			Assert.That(notScheduled.DistanceStanding, Is.EqualTo("NoOperationScheduled"));
			Assert.That(notScheduled.TimeStanding, Is.EqualTo("NoOperationScheduled"));
			Assert.That(
				notScheduled.Says,
				Is.EqualTo("no hay ninguna operación de Revisión agendada para este vehículo"),
				"Sin operacion agendada no hay dos mitades que decir, asi que la cuenta atras lo dice "
				+ "UNA vez y no dos.");
		}

		// LAS SIETE SITUACIONES NO SON UN BOOLEANO, y esta es la prueba que se pone roja el dia que la
		// caratula las junte en uno. Cuatro estados distintos en una sola respuesta, cada uno con su
		// propia frase, y ninguno igual a otro.
		[Test]
		public void FourDifferentStandingsArriveInOneAnswerAndNoneOfThemIsTheSameAsAnother()
		{
			Assert.That(TheRowFor(TheKindMeasuredBothWays).DistanceStanding, Is.EqualTo("StillAhead"));
			Assert.That(
				TheRowFor(TheKindMeasuredOnlyInTime).DistanceStanding, Is.EqualTo("OutsideTheLaw"),
				"Una operacion que no se mide en distancia esta FUERA DE LA LEY de esa dimension, que "
				+ "no es lo mismo que no haberla podido medir.");
			Assert.That(
				TheRowFor(TheKindOutsideEveryLimit).DistanceStanding, Is.EqualTo("OutsideTheLaw"));
			Assert.That(
				TheRowFor(TheKindThatIsNotOnTheSchedule).DistanceStanding,
				Is.EqualTo("NoOperationScheduled"));

			// Y LA MITAD DE TIEMPO DE LA MISMA OPERACION CONTESTA OTRA COSA: son dos mitades y no una.
			Assert.That(TheRowFor(TheKindMeasuredOnlyInTime).TimeStanding, Is.EqualTo("StillAhead"));
			Assert.That(
				TheRowFor(TheKindOutsideEveryLimit).TimeStanding, Is.EqualTo("OutsideTheLaw"));

			Assert.That(
				TheRowFor(TheKindMeasuredOnlyInTime).DistanceSays,
				Is.Not.EqualTo(TheRowFor(TheKindThatIsNotOnTheSchedule).DistanceSays),
				"Fuera de la ley y no agendada son dos respuestas distintas: un booleano las juntaria.");
		}

		// ===== LAS TRES GUARDADAS: LA CARATULA PREGUNTA LA GUARDA Y CONTESTA EL LADO VACIO =====

		// LA LEY DE TheGuardedReadsNeverReachTheAssistant, aplicada a las tres guardadas de la parte A:
		// una excepcion que escapa de una tool le llega al asistente como "la herramienta fallo", y
		// "esta operacion no se mide en kilometros" es una RESPUESTA.
		//
		// Y EL CERO NO ES UN MONTO: viene con DistanceCanBeSaid en false al lado y la unidad vacia, que
		// es la unica forma en que un numero puede acompanar a una mitad que nadie midio.
		[Test]
		public void AHalfThatCannotBeSaidComesBackWithItsReasonAndNeverAsAProtocolError()
		{
			ServiceCountdown onlyTime = TheRowFor(TheKindMeasuredOnlyInTime);

			Assert.That(onlyTime.DistanceCanBeSaid, Is.False);
			Assert.That(onlyTime.DistanceRemaining, Is.EqualTo(0));
			Assert.That(onlyTime.DistanceUnit, Is.EqualTo(""),
				"Y sin unidad, porque no hay ningun monto al que pertenezca.");

			Assert.That(onlyTime.DistanceStanding, Is.EqualTo("OutsideTheLaw"));
			Assert.That(onlyTime.DistanceSays, Is.Not.Empty);
			Assert.That(onlyTime.CountdownRefused, Is.False,
				"Que una mitad no se pueda decir NO es un rechazo: es una de las siete situaciones.");

			// EL CONTROL: la puerta SI revienta con esas tres. Si dejara de hacerlo, lo de arriba
			// pasaria por la razon equivocada.
			DateTime asOf = DateTime.Parse(
				TheAssistantAsksWhatIsLeft().AskedAt, null,
				System.Globalization.DateTimeStyles.RoundtripKind);

			Assert.Throws<InvalidOperationException>(
				() => journal.Api.ReadDistanceRemaining(TheKindMeasuredOnlyInTime, asOf));
			Assert.Throws<InvalidOperationException>(
				() => journal.Api.ReadDistanceRemainingUnit(TheKindMeasuredOnlyInTime, asOf));
			Assert.Throws<InvalidOperationException>(
				() => journal.Api.ReadDaysRemaining(TheKindThatIsNotOnTheSchedule, asOf));
		}

		// Y LA MITAD QUE SI SE PUEDE DECIR SIGUE TRAYENDO SU MONTO EN LA MISMA RESPUESTA. Sin este
		// renglon, una caratula que contestara cero y vacio SIEMPRE pasaria la prueba de arriba -- y
		// eso es exactamente lo que pasa si alguien borra el pre-chequeo: el catch por clase se come
		// el rechazo y la fila vuelve sin numero. ES LA PRUEBA FUNCIONAL QUE LA CONSOLA NO PUDO TENER.
		[Test]
		public void AndTheHalfThatCanBeSaidStillCarriesItsAmountInTheSameAnswer()
		{
			MaintenanceCountdown answer = TheAssistantAsksWhatIsLeft();

			var withANumber = new List<string>();

			foreach (ServiceCountdown each in answer.Operations)
				if (each.DistanceCanBeSaid) withANumber.Add(each.Operation);

			Assert.That(withANumber, Is.EqualTo(new[] { TheKindMeasuredBothWays }),
				"Exactamente una de las nueve puede decir su distancia en este registro. Si esto se "
				+ "vacia, el pre-chequeo dejo de abrir la rama y el catch por clase lo tapo.");

			Assert.That(
				TheRowFor(TheKindMeasuredBothWays).DistanceRemaining,
				Is.EqualTo(TheDistanceThatIsLeft));

			var withDays = new List<string>();

			foreach (ServiceCountdown each in answer.Operations)
				if (each.DaysCanBeSaid) withDays.Add(each.Operation);

			Assert.That(
				withDays, Is.EquivalentTo(new[] { TheKindMeasuredBothWays, TheKindMeasuredOnlyInTime }),
				"Y dos pueden decir su plazo. Las dos listas son distintas a proposito: la guarda de "
				+ "la distancia y la del plazo son dos y no una.");
		}

		// ===== EL RECHAZO POR ANCLA ADELANTADA, QUE NO SE PUEDE PRE-CHEQUEAR ==============

		// LLEGA POR UNA ACCION ORDINARIA DE LA AGENCIA y no por un llamador que monta un escenario: un
		// plan anclado ADELANTE deja el AHORA de cualquier tool antes del ancla, y ninguna lectura de
		// la puerta contesta cual es el ancla, asi que no hay nada que preguntar primero (DEC-65).
		//
		// LA FILA LO DICE Y LAS OTRAS OCHO CONTESTAN. Una tool contesta UN objeto: sin el catch la
		// excepcion sale como "la herramienta fallo" y el asistente pierde las nueve; con un catch
		// alrededor de todo pierde ocho por culpa de una.
		[Test]
		public void APlanAnchoredInTheFutureIsRefusedInItsOwnRowAndTheOtherEightStillAnswer()
		{
			using (AScheduleOnARealJournal ahead = AVehicleWhosePlanIsAnchoredInTheFuture())
			{
				MaintenanceCountdown answer =
					new DealershipCarTools(new JournalReader(ahead.Folder, ""))
						.WhatIsLeftBeforeEachService();

				Assert.That(answer.Operations, Has.Count.EqualTo(9));

				ServiceCountdown refused = TheRowOf(answer, TheKindAnchoredAhead);

				Assert.That(refused.CountdownRefused, Is.True);
				Assert.That(refused.RefusalSays, Does.Contain("ancla"),
					"El rechazo llega con las palabras de la puerta, que nombran el ancla: es lo unico "
					+ "que dice de que se trata, y un campo vacio se lee como un hueco.");
				Assert.That(refused.Says, Is.EqualTo(""));
				Assert.That(refused.DistanceStanding, Is.EqualTo(""));
				Assert.That(refused.DistanceCanBeSaid, Is.False);
				Assert.That(refused.DistanceRemaining, Is.EqualTo(0));

				Assert.That(answer.SomeCountdownWasRefused, Is.True);
				Assert.That(answer.Detail, Does.Contain("anclada en el futuro"));

				// Y LAS OTRAS OCHO CONTESTAN, que es la mitad por la que el catch va por clase.
				var answered = new List<string>();

				foreach (ServiceCountdown each in answer.Operations)
					if (!each.CountdownRefused) answered.Add(each.Operation);

				Assert.That(answered, Has.Count.EqualTo(8));

				foreach (ServiceCountdown each in answer.Operations)
					if (!each.CountdownRefused)
						Assert.That(each.Says, Is.Not.Empty, each.Operation);
			}
		}

		// Y SIN NINGUNA ANCLA ADELANTADA LA MISMA RESPUESTA DICE LO CONTRARIO. Sin esto, una caratula
		// que marcara todo como rechazado pasaria la prueba de arriba.
		[Test]
		public void AndWithNoAnchorAheadNothingIsRefusedAndTheDetailSaysNothingAboutIt()
		{
			MaintenanceCountdown answer = TheAssistantAsksWhatIsLeft();

			Assert.That(answer.SomeCountdownWasRefused, Is.False);
			Assert.That(answer.Detail, Does.Not.Contain("anclada en el futuro"));

			foreach (ServiceCountdown each in answer.Operations)
			{
				Assert.That(each.CountdownRefused, Is.False, each.Operation);
				Assert.That(each.RefusalSays, Is.EqualTo(""), each.Operation);
				Assert.That(each.Says, Is.Not.Empty, each.Operation);
			}
		}

		// ===== NINGUN PARAMETRO, Y EL RELOJ ES EL DEL ANFITRION ===========================

		// LA TOOL NO TOMA NINGUN PARAMETRO, que es la respuesta a la pregunta del §14 item 10 del
		// brief: la clase es una clave de catalogo y el instante lo lee el reloj, asi que no queda
		// ningun argumento que pedirle a nadie. El censo exacto de parametros es la otra mitad.
		[Test]
		public void NeitherNewToolAsksForAnythingAtAll()
		{
			foreach (string named in new[]
			{
				"WhatIsLeftBeforeEachService", "WhatHasBeenDoneToMyCar",
			})
				Assert.That(
					typeof(DealershipCarTools).GetMethod(named).GetParameters(), Is.Empty,
					"'" + named + "' gano un parametro. Un odometro, una lectura o un instante pedidos "
					+ "al asistente son el mismo invento con un esquema encima.");
		}

		// EL INSTANTE VIENE DEL RELOJ DEL ANFITRION, TRUNCADO AL SEGUNDO, Y NO ES MONOTONO (C64). Un
		// argumento de CONSULTA no escribe nada, asi que no hay nada que proteger, y exigir que
		// avance rechazaria una segunda pregunta legitima un segundo despues.
		[Test]
		public void TheAssistantsClockIsStillTheOnlyClockAndItIsNotMonotonic()
		{
			MaintenanceCountdown first = TheAssistantAsksWhatIsLeft();
			MaintenanceCountdown second = TheAssistantAsksWhatIsLeft();

			Assert.That(first.AskedAt, Is.Not.Empty);
			Assert.That(second.AskedAt, Is.Not.Empty,
				"La segunda pregunta contesta igual. Un punto de control monotono rechazaria una "
				+ "consulta hecha dentro del mismo segundo, y una consulta no escribe nada.");

			Assert.That(first.AskedAt, Does.EndWith("Z"),
				"Con la Z: el journal borra el Kind (C24) y perderla dejaria al asistente leyendo un "
				+ "instante sin huso.");
			Assert.That(first.AskedAt, Does.EndWith(".0000000Z"),
				"Truncado al segundo entero, que es la resolucion con la que el journal guarda un "
				+ "instante: la fraccion viene en cero y no se pierde nada al escribirla.");

			// Y EL RELOJ SE LEE EN UN SOLO SITIO DE TODA LA CARATULA. Se mide sobre la FUENTE porque
			// una segunda lectura en otra tool contestaria un instante igual de valido y ninguna
			// respuesta podria distinguirlas.
			var read = new List<string>();

			foreach (string file in TheCodeOfTheShellsAsData.EveryFileOf(TheCodeOfTheShellsAsData.Mcp))
				foreach (string line in File.ReadAllLines(file))
					if (Regex.IsMatch(line, @"DateTime(Offset)?\.(UtcNow|Now|Today)"))
						read.Add(
							TheCodeOfTheShellsAsData.InsideItsRoot(TheCodeOfTheShellsAsData.Mcp, file)
							+ ": " + line.Trim());

			Assert.That(read, Has.Count.EqualTo(1),
				"La caratula del asistente lee el reloj en mas de un sitio. Es UNO, y esta escrito "
				+ "aca: " + string.Join(" | ", read));

			Assert.That(read[0], Does.StartWith("Tools/TheClockTheAssistantReads.cs"));
		}

		// ------------------------------------------------------ el aparejo

		private const string TheKindAnchoredAhead = "FilterChange";

		private MaintenanceCountdown TheAssistantAsksWhatIsLeft()
		{
			return new DealershipCarTools(new JournalReader(journal.Folder, ""))
				.WhatIsLeftBeforeEachService();
		}

		private ServiceCountdown TheRowFor(string kind)
		{
			return TheRowOf(TheAssistantAsksWhatIsLeft(), kind);
		}

		private static ServiceCountdown TheRowOf(MaintenanceCountdown answer, string kind)
		{
			foreach (ServiceCountdown each in answer.Operations)
				if (each.Operation == kind) return each;

			throw new InvalidOperationException("La respuesta no trajo la fila de " + kind + ".");
		}

		// Un plan sembrado con el ancla en 2099, en su propia carpeta: el AHORA de cualquier corrida
		// futura sigue siendo anterior a esa ancla.
		private static AScheduleOnARealJournal AVehicleWhosePlanIsAnchoredInTheFuture()
		{
			var ahead = new AScheduleOnARealJournal("KMHD35LH5EU000007", "Standard");

			ahead.Api.SeedPlanOperation(
				TheKindAnchoredAhead, "DistanceOnly", 0, 9000,
				TheUnitEverythingIsIn, true, false,
				new DateTime(2099, 1, 1, 0, 0, 0, DateTimeKind.Utc), 50000, TheUnitEverythingIsIn);

			return ahead;
		}
	}
}
