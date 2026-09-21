using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LAS RESPUESTAS DEL HISTORIAL, DICHAS POR VALOR EN LOS DOS IDIOMAS: el juego de una clase en sus
	// tres situaciones, la fila con lectura y la fila sin ninguna, y la celda de la lectura en las dos
	// formas.
	//
	// POR QUE NO ALCANZA CON NINGUNA PRUEBA DE ACUERDO. Comparar dos rutas -- el ayudante y la
	// puerta -- no dice nada sobre el catalogo: las dos pasan por el mismo renderizador, asi que una
	// clave que falta se dice "{clave}" por las dos y un hueco con el nombre cambiado se queda
	// "{hueco}" por las dos. Las dos coinciden y las dos estan mal, que es E49 dicho sobre un
	// catalogo. Lo unico que lo ve es afirmar la frase.
	//
	// Y TAMPOCO ALCANZA CON LA PARIDAD DE CLAVES. TheTwoCataloguesOfTheAppCarryTheSameKeys ve una
	// clave que falta en un catalogo; no ve una clave que sobra en los dos, ni dos huecos cruzados
	// adentro de una sola clave, porque un hueco no es una clave.
	//
	// LOS INSTANTES NO SE VUELVEN A DECIR. Una completacion trae un INSTANTE con su hora -- de que dia
	// local es necesitaria una zona que este modulo no tiene -- y se cita tal cual en los dos idiomas.
	// Lo que SI se vuelve a decir es la LECTURA: los miles son del idioma, y esa es la unica cifra que
	// el historial pone en una frase.
	[TestFixture]
	public sealed class TheHistoryIsSaidInTheLanguageItIsAskedIn
	{
		private const string TheVin = "1HGCM82633A004352";

		private const int TheTimeLimitInDays = 365;
		private const int TheDistanceLimit = 8000;
		private const int NoDistanceLimit = 0;

		private static readonly DateTime TheSeededAnchor = new DateTime(2025, 1, 10, 7, 30, 0);
		private const int TheSeededAnchorReading = 30000;

		// LAS DOS COMPLETACIONES DE LA CLASE QUE MIDE DISTANCIA y la una de la que no. Ninguna lectura
		// coincide con el ancla ni con los limites (C60).
		private static readonly DateTime TheFirstCompletion = new DateTime(2025, 4, 15, 9, 20, 0);
		private const int TheOdometerAtTheFirst = 34100;
		private static readonly DateTime TheSecondCompletion = new DateTime(2025, 8, 22, 14, 5, 0);
		private const int TheOdometerAtTheSecond = 38700;

		private static readonly DateTime TheTimedCompletion = new DateTime(2025, 6, 11, 16, 40, 0);
		private const int TheOdometerThatIsReceivedAndDiscarded = 51234;

		private static readonly DateTime TheInstantItIsAsked = new DateTime(2026, 2, 1, 0, 0, 0);

		// EL JUEGO CON FILAS, en plural y con la lectura de la mas reciente adentro. Es donde se ve el
		// separador de miles moverse: 38.700 en espanol y 38,700 en ingles, y el numero es el mismo.
		[Test]
		public void AKindWithCompletionsSaysHowManyAndTheNewestInBothLanguages()
		{
			MaintenanceSchedule schedule = ARecordWithBothKindsCompleted();

			Assert.That(
				TheHistoryOf(schedule, OperationKindCode.FluidChange, TheLanguage.OfTheRecord),
				Is.EqualTo(
					"Cambio de fluido: 2 completaciones registradas. La más reciente se hizo el "
					+ "2025-08-22 14:05:00 con el odómetro en 38.700 km."));

			Assert.That(
				TheHistoryOf(schedule, OperationKindCode.FluidChange, TheLanguage.OfAnEnglishReader),
				Is.EqualTo(
					"Fluid change: 2 completions on record. The most recent was done on "
					+ "2025-08-22 14:05:00 with the odometer at 38,700 km."));
		}

		// EL SINGULAR ES DEL IDIOMA Y NO DEL NUMERO. Una sola completacion se dice entera en los dos, y
		// esta es ademas la clase cuya ancla no tiene lectura: las dos mitades de la frase cambian a la
		// vez y ninguna se puede leer de la otra.
		[Test]
		public void OneCompletionIsSaidInTheSingularAndWithoutAReadingInBothLanguages()
		{
			MaintenanceSchedule schedule = ARecordWithBothKindsCompleted();

			Assert.That(
				TheHistoryOf(schedule, OperationKindCode.TimedComponent, TheLanguage.OfTheRecord),
				Is.EqualTo(
					"Componente por tiempo: 1 completación registrada. La más reciente se hizo el "
					+ "2025-06-11 16:40:00, sin lectura del odómetro registrada."));

			Assert.That(
				TheHistoryOf(
					schedule, OperationKindCode.TimedComponent, TheLanguage.OfAnEnglishReader),
				Is.EqualTo(
					"Timed component: 1 completion on record. The most recent was done on "
					+ "2025-06-11 16:40:00, with no odometer reading recorded."));
		}

		// LAS DOS AUSENCIAS, cada una nombrando su propia razon. Un solo marco para las dos juntaria
		// una ausencia que una visita al taller cura con una que no la cura ninguna.
		[Test]
		public void TheTwoAbsencesNameTheirOwnReasonInBothLanguages()
		{
			MaintenanceSchedule schedule = ARecordWithBothKindsCompleted();

			Assert.That(
				TheHistoryOf(schedule, OperationKindCode.Inspection, TheLanguage.OfTheRecord),
				Is.EqualTo("Revisión: no hay ninguna completación registrada para este vehículo."));

			Assert.That(
				TheHistoryOf(schedule, OperationKindCode.Inspection, TheLanguage.OfAnEnglishReader),
				Is.EqualTo("Inspection: there is no completion on record for this vehicle."));

			Assert.That(
				TheHistoryOf(schedule, OperationKindCode.Repair, TheLanguage.OfTheRecord),
				Is.EqualTo(
					"Este vehículo no tiene agendada ninguna operación de reparación, así que no hay "
					+ "ninguna completación que registrar."));

			Assert.That(
				TheHistoryOf(schedule, OperationKindCode.Repair, TheLanguage.OfAnEnglishReader),
				Is.EqualTo(
					"This vehicle has no repair scheduled, so there is no completion to record."));
		}

		// CADA FILA EN SU PROPIA FRASE, con su conectiva: una frase que cruzara el instante de una fila
		// con la lectura de otra contendria los dos valores igual, y la conectiva es lo que lo delata.
		[Test]
		public void EveryRowSaysItselfWithItsConnectiveInBothLanguages()
		{
			MaintenanceSchedule schedule = ARecordWithBothKindsCompleted();

			Assert.That(
				TheRowsOf(schedule, OperationKindCode.FluidChange, TheLanguage.OfTheRecord),
				Is.EqualTo(new[]
				{
					"Cambio de fluido: se hizo el 2025-08-22 14:05:00 con el odómetro en 38.700 km.",
					"Cambio de fluido: se hizo el 2025-04-15 09:20:00 con el odómetro en 34.100 km.",
				}));

			Assert.That(
				TheRowsOf(schedule, OperationKindCode.FluidChange, TheLanguage.OfAnEnglishReader),
				Is.EqualTo(new[]
				{
					"Fluid change: done on 2025-08-22 14:05:00 with the odometer at 38,700 km.",
					"Fluid change: done on 2025-04-15 09:20:00 with the odometer at 34,100 km.",
				}));

			Assert.That(
				TheRowsOf(schedule, OperationKindCode.TimedComponent, TheLanguage.OfTheRecord),
				Is.EqualTo(new[]
				{
					"Componente por tiempo: se hizo el 2025-06-11 16:40:00, sin lectura del odómetro "
					+ "registrada.",
				}));

			Assert.That(
				TheRowsOf(schedule, OperationKindCode.TimedComponent, TheLanguage.OfAnEnglishReader),
				Is.EqualTo(new[]
				{
					"Timed component: done on 2025-06-11 16:40:00, with no odometer reading recorded.",
				}));
		}

		// LA CELDA DE LA LECTURA EN SUS DOS FORMAS. La ausencia es una SITUACION y se dice con palabras
		// en los dos idiomas: una celda vacia y un cero se leerian las dos como que hubo medicion.
		[Test]
		public void TheReadingCellIsSaidInBothFormsAndBothLanguages()
		{
			MaintenanceSchedule schedule = ARecordWithBothKindsCompleted();

			Assert.That(
				TheReadingsOf(schedule, OperationKindCode.FluidChange, TheLanguage.OfTheRecord),
				Is.EqualTo(new[] { "38.700 km", "34.100 km" }));

			Assert.That(
				TheReadingsOf(schedule, OperationKindCode.FluidChange, TheLanguage.OfAnEnglishReader),
				Is.EqualTo(new[] { "38,700 km", "34,100 km" }));

			Assert.That(
				TheReadingsOf(schedule, OperationKindCode.TimedComponent, TheLanguage.OfTheRecord),
				Is.EqualTo(new[] { "sin lectura registrada" }));

			Assert.That(
				TheReadingsOf(
					schedule, OperationKindCode.TimedComponent, TheLanguage.OfAnEnglishReader),
				Is.EqualTo(new[] { "no reading recorded" }));
		}

		// EL JUEGO ENTERO, INTERCALADO, con la clase de cada fila dicha en el idioma que se pregunto y
		// la columna de la clase intacta. La columna es identidad y no se traduce; la frase si.
		[Test]
		public void TheWholeRecordSaysEachRowsKindInBothLanguagesAndTheColumnStaysTheMembersName()
		{
			MaintenanceSchedule schedule = ARecordWithBothKindsCompleted();

			Assert.That(
				schedule.CompletionKindsOnRecord(),
				Is.EqualTo(new[] { "FluidChange", "TimedComponent", "FluidChange" }),
				"La columna dice el NOMBRE del miembro y no se traduce: es lo que quien lee puede "
				+ "volver a pasar.");

			Assert.That(
				TheSentenceTheDoorWouldCompose.EachCompletionOnRecord(
					schedule, TheLanguage.OfTheRecord)[1],
				Is.EqualTo(
					"Componente por tiempo: se hizo el 2025-06-11 16:40:00, sin lectura del odómetro "
					+ "registrada."));

			Assert.That(
				TheSentenceTheDoorWouldCompose.EachCompletionOnRecord(
					schedule, TheLanguage.OfAnEnglishReader)[1],
				Is.EqualTo(
					"Timed component: done on 2025-06-11 16:40:00, with no odometer reading "
					+ "recorded."));
		}

		// LA OPERACION QUE NO ESTA AGENDADA SE NOMBRA POR SU MIEMBRO Y NO POR SU ETIQUETA EN ESPANOL,
		// y esta es la mitad que valia medir: hasta este turno la agenda le pasaba a la clausula del
		// resto el DICHO de la clase en vez de su nombre, asi que la frase pedia al catalogo la clave
		// "canon.operation.Reparación", no la encontraba y caia al nombre separado en mayusculas. En
		// espanol acertaba por casualidad; en ingles la cuenta atras de una clase no agendada decia
		// "Reparación" en medio de una frase en ingles.
		//
		// SE AFIRMAN LAS DOS FAMILIAS JUNTAS. El motivo del estado ya pasaba el NOMBRE y estaba bien;
		// la clausula del resto pasaba el DICHO. Son la misma ausencia vista por dos marcos, y ponerlas
		// una al lado de la otra es lo que dice que ahora concuerdan.
		[Test]
		public void TheAbsentOperationIsNamedByItsMemberAndNotByItsSpanishLabel()
		{
			MaintenanceSchedule schedule = ARecordWithBothKindsCompleted();

			Assert.That(
				TheSentenceTheDoorWouldCompose.TheCountdown(
					schedule, OperationKindCode.Repair, TheInstantItIsAsked,
					TheLanguage.OfAnEnglishReader),
				Is.EqualTo("no Repair operation is scheduled for this vehicle"));

			Assert.That(
				TheSentenceTheDoorWouldCompose.TheDueReason(
					schedule, OperationKindCode.Repair, TheLanguage.OfAnEnglishReader),
				Is.EqualTo(
					"This vehicle has no repair scheduled, so there is nothing that could be "
					+ "overdue."));

			Assert.That(
				TheSentenceTheDoorWouldCompose.TheCountdown(
					schedule, OperationKindCode.Repair, TheInstantItIsAsked, TheLanguage.OfTheRecord),
				Is.EqualTo("no hay ninguna operación de Reparación agendada para este vehículo"),
				"Y en espanol no se movio nada: la etiqueta y el nombre separado en mayusculas dicen "
				+ "lo mismo para las nueve clases, que es por lo que el defecto vivio un turno entero "
				+ "sin que ninguna prueba lo viera.");
		}

		// NINGUNA RESPUESTA DEL HISTORIAL SE QUEDA CON UN HUECO SIN LLENAR NI CON UNA CLAVE CRUDA. Es
		// el barrido que hace que las pruebas de arriba no dependan de que a alguien se le ocurriera el
		// caso: Say() devuelve "{clave}" cuando no la tiene y deja "{hueco}" cuando el nombre no calza,
		// y las dos cosas se ven con una llave en la frase final.
		[Test]
		public void NoAnswerOfTheHistoryComesBackWithAHoleOrARawKeyInIt()
		{
			foreach (string language in TheTwoLanguages())
				foreach (string said in EverySentenceOfTheHistory(language))
					Assert.That(said, Does.Not.Contain("{").And.Not.Contain("}"),
						"Una llave en la frase final es una clave que el catalogo no tiene o un hueco "
						+ "cuyo nombre no calza con el que quien compone le pasa. En " + language
						+ ": " + said);
		}

		private IEnumerable<string> EverySentenceOfTheHistory(string language)
		{
			MaintenanceSchedule schedule = ARecordWithBothKindsCompleted();

			List<string> said = new List<string>
			{
				TheHistoryOf(schedule, OperationKindCode.FluidChange, language),
				TheHistoryOf(schedule, OperationKindCode.TimedComponent, language),
				TheHistoryOf(schedule, OperationKindCode.Inspection, language),
				TheHistoryOf(schedule, OperationKindCode.Repair, language),
				TheSentenceTheDoorWouldCompose.TheCountdown(
					schedule, OperationKindCode.Repair, TheInstantItIsAsked, language),
			};

			said.AddRange(TheRowsOf(schedule, OperationKindCode.FluidChange, language));
			said.AddRange(TheRowsOf(schedule, OperationKindCode.TimedComponent, language));
			said.AddRange(TheReadingsOf(schedule, OperationKindCode.FluidChange, language));
			said.AddRange(TheReadingsOf(schedule, OperationKindCode.TimedComponent, language));
			said.AddRange(
				TheSentenceTheDoorWouldCompose.EachCompletionOnRecord(schedule, language));
			said.AddRange(
				TheSentenceTheDoorWouldCompose.EachCompletionReadingOnRecord(schedule, language));

			return said;
		}

		private static string TheHistoryOf(
			MaintenanceSchedule schedule, OperationKindCode kind, string language)
		{
			return TheSentenceTheDoorWouldCompose.TheCompletions(schedule, kind, language);
		}

		private static string[] TheRowsOf(
			MaintenanceSchedule schedule, OperationKindCode kind, string language)
		{
			return TheSentenceTheDoorWouldCompose.EachCompletionOf(schedule, kind, language);
		}

		private static string[] TheReadingsOf(
			MaintenanceSchedule schedule, OperationKindCode kind, string language)
		{
			return TheSentenceTheDoorWouldCompose.EachCompletionReadingOf(schedule, kind, language);
		}

		// UNA CLASE QUE MIDE DISTANCIA COMPLETADA DOS VECES, una que solo mide tiempo completada una, y
		// una sembrada que nunca se completo. La cuarta clase que las pruebas nombran -- la reparacion
		// -- no se siembra: esa es la otra ausencia.
		private static MaintenanceSchedule ARecordWithBothKindsCompleted()
		{
			MaintenanceSchedule schedule =
				new MaintenanceSchedule(TheVin, UsageProfileCode.Standard);

			schedule.SeedPlanOperation(
				OperationKindCode.FluidChange, IntervalBasisCode.TimeAndDistance,
				TheTimeLimitInDays, TheDistanceLimit, DistanceUnitCode.UnitA,
				true, false,
				TheSeededAnchor, TheSeededAnchorReading, DistanceUnitCode.UnitA);

			schedule.SeedPlanOperation(
				OperationKindCode.TimedComponent, IntervalBasisCode.TimeOnly,
				TheTimeLimitInDays, NoDistanceLimit, DistanceUnitCode.UnitA,
				true, false,
				TheSeededAnchor, TheSeededAnchorReading, DistanceUnitCode.UnitA);

			schedule.SeedPlanOperation(
				OperationKindCode.Inspection, IntervalBasisCode.TimeAndDistance,
				TheTimeLimitInDays, TheDistanceLimit, DistanceUnitCode.UnitA,
				true, false,
				TheSeededAnchor, TheSeededAnchorReading, DistanceUnitCode.UnitA);

			schedule.CompleteOperation(
				OperationKindCode.FluidChange, TheFirstCompletion, TheOdometerAtTheFirst,
				DistanceUnitCode.UnitA);
			schedule.CompleteOperation(
				OperationKindCode.TimedComponent, TheTimedCompletion,
				TheOdometerThatIsReceivedAndDiscarded, DistanceUnitCode.UnitA);
			schedule.CompleteOperation(
				OperationKindCode.FluidChange, TheSecondCompletion, TheOdometerAtTheSecond,
				DistanceUnitCode.UnitA);

			return schedule;
		}

		private static string[] TheTwoLanguages()
		{
			return new[] { TheLanguage.OfTheRecord, TheLanguage.OfAnEnglishReader };
		}
	}
}
