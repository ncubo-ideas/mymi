using System;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LA MITAD DE PUERTA DEL TEST 13 DEL BRIEF, y de la ley L6 del slice del sobre. El nivel A ya la
	// tiene por consecuencia y por estructura, sobre el agregado
	// (TheOdometerReadIsTheNumberTheScheduleEvaluatesAgainst). Lo que se agrega aca es que los DOS
	// CAMINOS SIGUEN DE ACUERDO CRUZANDO LA PUERTA: el tablero -- que sale de Evaluate -- y la
	// lectura del odometro, al mismo instante, sobre un journal de verdad.
	//
	// POR QUE HACE FALTA CRUZANDO LA PUERTA Y NO BASTA EL NIVEL A. Entre el agregado y el llamador
	// hay dos traducciones que el nivel A no toca: el instante se trunca al segundo en la frontera, y
	// las dos lecturas viajan por DOS consultas distintas. Si la lectura del odometro y la guarda de
	// presencia recibieran instantes distintos -- o si una de las dos se olvidara de normalizar -- el
	// numero de la seccion de garantia y el del tablero podrian separarse sin que nada del nivel A lo
	// viera.
	//
	// DE QUE LADO DE Evaluate ESTA ESTE FIXTURE, que es la trampa que el spike T0 pago. Evaluate pasa
	// por la guarda de cadencia: re-evaluar a un instante ya evaluado se RECHAZA y no escribe nada.
	// Asi que cada journal se evalua UNA sola vez, al instante preguntado, y despues solo se lee. Una
	// segunda llamada a Evaluate en el mismo instante afirmaria contra una agenda que no volvio a
	// mirar nada.
	//
	// LOS NUMEROS ESTAN ELEGIDOS PARA QUE LA RESPUESTA EQUIVOCADA SE VEA DISTINTA (C60): ninguna
	// lectura es cero, las dos caen a lados opuestos del limite, y el instante preguntado no coincide
	// con ninguna observacion.
	[TestFixture]
	public sealed class TheOdometerReadHandsBackTheNumberTheBoardWasComputedAgainst
	{
		private const string TheVin = "1HGCM82633A004352";
		private const int TenThousandUnits = 10000;
		private const int OdometerAtDelivery = 30000;

		// 35000 - 30000 = 5000 contra un limite de 10000: NotDue con holgura.
		private const int TheReadingBeforeTheLimit = 35000;

		// 40200 - 30000 = 10200: Due, y no Overdue.
		private const int TheReadingPastTheLimit = 40200;

		private static readonly DateTime Delivery =
			new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheEarlierObservation =
			new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheInstantAsked =
			new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime AfterTheInstantAsked =
			new DateTime(2026, 9, 20, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime BeforeTheInstantAsked =
			new DateTime(2026, 5, 20, 0, 0, 0, DateTimeKind.Utc);

		// LOS DOS CAMINOS SE DAN VUELTA JUNTOS, POR LA PUERTA. Las dos agendas son identicas salvo en
		// CUANDO se observo la segunda lectura, cada una se evalua una vez al mismo instante, y la
		// lectura del odometro y el estado del tablero cambian a la par.
		[Test]
		public void TheBoardAndTheReadMoveOnTheSameReadingThroughTheDoor()
		{
			using (AScheduleOnARealJournal theBackfillIsDatedAhead =
				AJournalWhoseSecondReadingWasObservedOn(AfterTheInstantAsked))
			using (AScheduleOnARealJournal theBackfillIsAlreadyObserved =
				AJournalWhoseSecondReadingWasObservedOn(BeforeTheInstantAsked))
			{
				Assert.That(
					theBackfillIsDatedAhead.Api.Evaluate(TheInstantAsked).WasEvaluated, Is.True);
				Assert.That(
					theBackfillIsAlreadyObserved.Api.Evaluate(TheInstantAsked).WasEvaluated, Is.True);

				Assert.That(
					theBackfillIsDatedAhead.Api.ReadOdometer(TheInstantAsked),
					Is.EqualTo(TheReadingBeforeTheLimit),
					"El relleno esta fechado hacia adelante: Evaluate no lo miro, y la lectura tampoco "
					+ "lo puede mirar. Devolver la ultima registrada dejaria la seccion de garantia "
					+ "diciendo 40200 mientras el tablero decidio con 35000, y ninguna de las dos "
					+ "pantallas lo diria.");

				Assert.That(
					theBackfillIsDatedAhead.Api.ReadDueState("FilterChange"),
					Is.EqualTo("NotDue"),
					"El tablero decidio con 35000. La lectura tiene que decir 35000.");

				Assert.That(
					theBackfillIsAlreadyObserved.Api.ReadOdometer(TheInstantAsked),
					Is.EqualTo(TheReadingPastTheLimit));

				Assert.That(
					theBackfillIsAlreadyObserved.Api.ReadDueState("FilterChange"),
					Is.EqualTo("Due"),
					"Y con 40200 el tablero decide distinto. Las dos respuestas se dan vuelta JUNTAS, "
					+ "que es la unica forma de ver que las dos leen el mismo numero.");
			}
		}

		// LA MISMA AGENDA CONTESTA DOS COSAS SEGUN EL INSTANTE, tambien por la puerta. Aca la clave
		// es visiblemente el instante y no el orden en que llegaron las lecturas -- y no hace falta
		// evaluar, porque lo que se compara son dos lecturas de la misma agenda.
		[Test]
		public void TheSameJournalAnswersDifferentlyAtTwoInstantsBecauseTheKeyIsTheInstant()
		{
			using (AScheduleOnARealJournal journal =
				AJournalWhoseSecondReadingWasObservedOn(AfterTheInstantAsked))
			{
				Assert.That(
					journal.Api.ReadOdometer(TheInstantAsked),
					Is.EqualTo(TheReadingBeforeTheLimit));

				Assert.That(
					journal.Api.ReadOdometer(AfterTheInstantAsked.AddDays(1)),
					Is.EqualTo(TheReadingPastTheLimit),
					"Pasada la observacion, la MISMA agenda contesta la otra lectura. La respuesta es "
					+ "del INSTANTE preguntado y no de la agenda.");
			}
		}

		// TEST 14: LA LECTURA DEVUELVE CUANDO SE OBSERVO, no el instante en que se pregunto. Es la
		// linea de honestidad del slice: sin ella, una frase compuesta con una lectura de marzo se
		// lee como tomada hoy, y el riesgo R2 del plan -- que el tablero y la seccion de garantia
		// puedan quedar en momentos distintos -- no tiene ninguna mitigacion.
		[Test]
		public void TheReadHandsBackWhenItWasObservedAndNotWhenItWasAsked()
		{
			using (AScheduleOnARealJournal journal =
				AJournalWhoseSecondReadingWasObservedOn(AfterTheInstantAsked))
			{
				Assert.That(
					journal.Api.ReadOdometerObservedAt(TheInstantAsked),
					Is.EqualTo(TheEarlierObservation));

				Assert.That(
					journal.Api.ReadOdometerObservedAt(TheInstantAsked),
					Is.Not.EqualTo(TheInstantAsked),
					"Los dos instantes son distintos a proposito: si la lectura devolviera el "
					+ "instante preguntado, una lectura de hace tres meses se leeria como de hoy y "
					+ "nadie podria saberlo.");

				Assert.That(
					journal.Api.ReadOdometerObservedAt(AfterTheInstantAsked.AddDays(1)),
					Is.EqualTo(AfterTheInstantAsked),
					"Y con la otra lectura contesta la otra observacion: el instante que sale es el "
					+ "de la lectura que salio, y no uno fijo.");
			}
		}

		// Y LA UNIDAD ES LA DE ESA MISMA LECTURA. Va aparte porque es lo que decide, en T6, si el
		// limite del sobre y el odometro se pueden comparar -- y una unidad que viniera de otra
		// lectura compararia contra un numero que no es el que salio.
		[Test]
		public void TheUnitBelongsToTheSameReadingTheNumberCameFrom()
		{
			using (AScheduleOnARealJournal journal = ANewJournal())
			{
				journal.Api.RecordReading(
					TheReadingBeforeTheLimit, "UnitB", TheEarlierObservation, "Telemetry");
				journal.Api.RecordReading(
					TheReadingPastTheLimit, "UnitA", AfterTheInstantAsked, "Telemetry");

				Assert.That(journal.Api.ReadOdometer(TheInstantAsked),
					Is.EqualTo(TheReadingBeforeTheLimit));
				Assert.That(journal.Api.ReadOdometerUnit(TheInstantAsked), Is.EqualTo("mi"));

				Assert.That(journal.Api.ReadOdometer(AfterTheInstantAsked.AddDays(1)),
					Is.EqualTo(TheReadingPastTheLimit));
				Assert.That(
					journal.Api.ReadOdometerUnit(AfterTheInstantAsked.AddDays(1)),
					Is.EqualTo("km"),
					"La segunda lectura llego en la otra unidad, y la unidad que sale es la SUYA. Una "
					+ "unidad tomada de otra lectura es como un limite se compara contra un numero que "
					+ "no esta en esa unidad sin que nada se vea mal.");
			}
		}

		private static AScheduleOnARealJournal AJournalWhoseSecondReadingWasObservedOn(
			DateTime theSecondObservation)
		{
			AScheduleOnARealJournal journal = ANewJournal();

			journal.Api.SeedPlanOperation(
				"FilterChange", "DistanceOnly", 0, TenThousandUnits, "UnitA",
				true, false,
				Delivery, OdometerAtDelivery, "UnitA");

			journal.Api.RecordReading(
				TheReadingBeforeTheLimit, "UnitA", TheEarlierObservation, "Telemetry");
			journal.Api.RecordReading(
				TheReadingPastTheLimit, "UnitA", theSecondObservation, "Telemetry");

			return journal;
		}

		private static AScheduleOnARealJournal ANewJournal()
		{
			return new AScheduleOnARealJournal(TheVin, "Standard");
		}
	}
}
