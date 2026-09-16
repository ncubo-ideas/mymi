using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LAS RESPUESTAS DE LA SERIE DEL ODOMETRO, DICHAS POR VALOR EN LOS DOS IDIOMAS: el ritmo de un
	// periodo en sus cinco situaciones, el tramo de tiempo en sus cuatro formas, los dos periodos
	// comparados con las cinco situaciones del cambio, y la frase de cada fila con su fuente.
	//
	// POR QUE NO ALCANZA CON NINGUNA PRUEBA DE ACUERDO. Comparar dos rutas -- el ayudante y la
	// puerta -- no dice nada sobre el catalogo: las dos pasan por el mismo renderizador, asi que una
	// clave que falta se dice "{clave}" por las dos y un hueco con el nombre cambiado se queda
	// "{hueco}" por las dos. Las dos coinciden y las dos estan mal, que es E49 dicho sobre un
	// catalogo. Lo unico que lo ve es afirmar la frase.
	//
	// Y TAMPOCO ALCANZA CON LA PARIDAD DE CLAVES. TheTwoCataloguesOfTheAppCarryTheSameKeys ve una
	// clave que falta en un catalogo; no ve una clave que sobra en los dos, ni dos huecos cruzados
	// adentro de una sola clave, porque un hueco no es una clave. Medido dos veces en CL-9 y en este
	// turno: cruzar dos huecos dentro de una clave pone rojas EXACTAMENTE las pruebas por valor.
	//
	// LOS INSTANTES NO SE VUELVEN A DECIR. Un instante con su hora es la forma canonica en que esta
	// serie nombra un momento -- de que dia local es necesitaria una zona que este modulo no tiene --
	// y se cita tal cual en los dos idiomas. Lo que SI se vuelve a decir es cada CIFRA: los miles y la
	// coma decimal son del idioma, y el tramo de un dia y medio es la unica frase del modulo donde se
	// puede ver el separador decimal moverse.
	[TestFixture]
	public sealed class TheRateOfUseIsSaidInTheLanguageItIsAskedIn
	{
		private const string TheVin = "1HGCM82633A004352";

		private static readonly DateTime TheYearFrom = new DateTime(2026, 1, 1);
		private static readonly DateTime TheYearTo = new DateTime(2026, 12, 31);

		private static readonly DateTime TheEmptyYearFrom = new DateTime(2024, 1, 1);
		private static readonly DateTime TheEmptyYearTo = new DateTime(2024, 12, 31);

		private static readonly DateTime TheTenthOfJanuary = new DateTime(2026, 1, 10);
		private static readonly DateTime TheTenthOfFebruary = new DateTime(2026, 2, 10);
		private static readonly DateTime TheTenthOfMarch = new DateTime(2026, 3, 10);

		private static readonly DateTime TheEleventhOfJanuary = new DateTime(2026, 1, 11);
		private static readonly DateTime TheEleventhOfJanuaryAtNoon =
			new DateTime(2026, 1, 11, 12, 0, 0);

		private static readonly DateTime TheSameSecond = new DateTime(2026, 1, 10, 8, 0, 0);
		private static readonly DateTime OneSecondLater = new DateTime(2026, 1, 10, 8, 0, 1);
		private static readonly DateTime FortySecondsLater = new DateTime(2026, 1, 10, 8, 0, 40);

		// LOS DOS PERIODOS DE LA COMPARACION: el viejo a 20 km por dia y el nuevo a 32, que es un 60%
		// mas. Son los mismos numeros con que TheComparisonNamesBothSpansAndNotOnlyTheChange fija la
		// DECISION; aqui se fija la REDACCION.
		private static readonly DateTime TheOldFrom = new DateTime(2025, 1, 1);
		private static readonly DateTime TheOldTo = new DateTime(2025, 1, 11);
		private static readonly DateTime TheNewFrom = new DateTime(2026, 1, 1);
		private static readonly DateTime TheNewTo = new DateTime(2026, 1, 11);

		// EL RITMO MEDIDO: los dos extremos, la distancia exacta, el tramo y la distancia por dia.
		[Test]
		public void AMeasuredRateIsSaidWholeInBothLanguages()
		{
			MaintenanceSchedule schedule = AnEmptySchedule();

			Arrives(schedule, 20000, TheTenthOfJanuary);
			Arrives(schedule, 22000, TheTenthOfMarch);

			Assert.That(
				TheRateSaidIn(schedule, TheLanguage.OfTheRecord),
				Is.EqualTo(
					"de 20.000 km el 2026-01-10 00:00:00 a 22.000 km el 2026-03-10 00:00:00: "
					+ "2.000 km en 59 días, unos 34 km por día"));

			Assert.That(
				TheRateSaidIn(schedule, TheLanguage.OfAnEnglishReader),
				Is.EqualTo(
					"from 20,000 km on 2026-01-10 00:00:00 to 22,000 km on 2026-03-10 00:00:00: "
					+ "2,000 km in 59 days, about 34 km per day"));
		}

		// LAS CUATRO SUSPENSIONES, cada una nombrando el hecho que la explica: la lectura que hay, la
		// que introdujo la segunda unidad, el segundo compartido o el periodo vacio. Ninguna dice cero.
		[Test]
		public void TheFourSuspensionsNameTheirOwnFactInBothLanguages()
		{
			MaintenanceSchedule one = AnEmptySchedule();

			Arrives(one, 20000, TheTenthOfJanuary);

			Assert.That(
				TheRateSaidIn(one, TheLanguage.OfTheRecord),
				Is.EqualTo(
					"todavía no se puede decir cuánto se maneja: en el periodo hay una sola lectura, "
					+ "20.000 km del 2026-01-10 00:00:00, y un ritmo necesita dos"));

			Assert.That(
				TheRateSaidIn(one, TheLanguage.OfAnEnglishReader),
				Is.EqualTo(
					"how much it is driven cannot be said yet: the period has a single reading, "
					+ "20,000 km on 2026-01-10 00:00:00, and a rate needs two"));

			Assert.That(
				TheSentenceTheDoorWouldCompose.TheRateOfUse(
					one, TheEmptyYearFrom, TheEmptyYearTo, TheLanguage.OfTheRecord),
				Is.EqualTo(
					"no se puede decir cuánto se maneja entre el 2024-01-01 00:00:00 y el "
					+ "2024-12-31 00:00:00: no hay ninguna lectura del odómetro en ese periodo"));

			Assert.That(
				TheSentenceTheDoorWouldCompose.TheRateOfUse(
					one, TheEmptyYearFrom, TheEmptyYearTo, TheLanguage.OfAnEnglishReader),
				Is.EqualTo(
					"how much it is driven between 2024-01-01 00:00:00 and 2024-12-31 00:00:00 "
					+ "cannot be said: there is no odometer reading in that period"));

			MaintenanceSchedule mixed = AnEmptySchedule();

			Arrives(mixed, 20000, TheTenthOfJanuary);
			mixed.RecordReading(
				500, DistanceUnitCode.UnitB, TheTenthOfFebruary, ReadingSourceCode.CustomerDeclared);

			Assert.That(
				TheRateSaidIn(mixed, TheLanguage.OfTheRecord),
				Is.EqualTo(
					"no se puede decir cuánto se maneja: el periodo tiene lecturas en km y en mi, y "
					+ "las unidades nunca se convierten. La segunda unidad la introdujo la lectura de "
					+ "500 mi del 2026-02-10 00:00:00"));

			Assert.That(
				TheRateSaidIn(mixed, TheLanguage.OfAnEnglishReader),
				Is.EqualTo(
					"how much it is driven cannot be said: the period has readings in km and in mi, "
					+ "and units are never converted. The second unit was introduced by the reading "
					+ "of 500 mi on 2026-02-10 00:00:00"));

			MaintenanceSchedule together = AnEmptySchedule();

			Arrives(together, 20000, TheSameSecond);
			Arrives(together, 20000, TheSameSecond);

			Assert.That(
				TheRateSaidIn(together, TheLanguage.OfTheRecord),
				Is.EqualTo(
					"no se puede decir cuánto se maneja: las dos lecturas del periodo se observaron "
					+ "en el mismo segundo, el 2026-01-10 08:00:00, así que no hay tiempo "
					+ "transcurrido que dividir"));

			Assert.That(
				TheRateSaidIn(together, TheLanguage.OfAnEnglishReader),
				Is.EqualTo(
					"how much it is driven cannot be said: the period's two readings were observed "
					+ "in the same second, 2026-01-10 08:00:00, so there is no elapsed time to "
					+ "divide by"));
		}

		// LAS CUATRO FORMAS DEL TRAMO. El segundo entero y el dia entero tienen su propio codigo
		// porque los dos idiomas los dicen en SINGULAR, y quien redacta no puede elegir entre singular
		// y plural mirando unos digitos.
		[Test]
		public void TheFourShapesOfTheSpanAreSaidInBothLanguages()
		{
			Assert.That(
				TheSpanOf(1, OneSecondLater, TheLanguage.OfTheRecord),
				Does.Contain("1 km en 1 segundo, unos 86.400 km por día"));

			Assert.That(
				TheSpanOf(1, OneSecondLater, TheLanguage.OfAnEnglishReader),
				Does.Contain("1 km in 1 second, about 86,400 km per day"));

			Assert.That(
				TheSpanOf(1, FortySecondsLater, TheLanguage.OfTheRecord),
				Does.Contain("1 km en 40 segundos, unos 2.160 km por día"));

			Assert.That(
				TheSpanOf(1, FortySecondsLater, TheLanguage.OfAnEnglishReader),
				Does.Contain("1 km in 40 seconds, about 2,160 km per day"));
		}

		// EL DIA ENTERO Y EL DIA Y MEDIO, que es donde se ve la COMA DECIMAL moverse: un tramo de un
		// dia y medio se dice "1,5 días" en espanol y "1.5 days" en ingles, y el numero es el mismo.
		[Test]
		public void AWholeDayAndAFractionOfADayAreSaidInBothLanguagesAndTheDecimalMarkMoves()
		{
			MaintenanceSchedule aDay = AnEmptySchedule();

			Arrives(aDay, 20000, TheTenthOfJanuary);
			Arrives(aDay, 20100, TheEleventhOfJanuary);

			Assert.That(
				TheRateSaidIn(aDay, TheLanguage.OfTheRecord),
				Does.Contain("100 km en 1 día, unos 100 km por día"));

			Assert.That(
				TheRateSaidIn(aDay, TheLanguage.OfAnEnglishReader),
				Does.Contain("100 km in 1 day, about 100 km per day"));

			MaintenanceSchedule andAHalf = AnEmptySchedule();

			Arrives(andAHalf, 20000, TheTenthOfJanuary);
			Arrives(andAHalf, 20150, TheEleventhOfJanuaryAtNoon);

			Assert.That(
				TheRateSaidIn(andAHalf, TheLanguage.OfTheRecord),
				Does.Contain("150 km en 1,5 días, unos 100 km por día"));

			Assert.That(
				TheRateSaidIn(andAHalf, TheLanguage.OfAnEnglishReader),
				Does.Contain("150 km in 1.5 days, about 100 km per day"));
		}

		// LOS DOS PERIODOS COMPARADOS, con los dos ritmos dichos enteros y el cambio adentro.
		[Test]
		public void TwoPeriodsComparedSayBothRatesAndTheChangeInBothLanguages()
		{
			MaintenanceSchedule schedule = TwoPeriodsOfUse();

			Assert.That(
				TheComparisonSaidIn(schedule, TheLanguage.OfTheRecord),
				Is.EqualTo(
					"Primer periodo: de 20.000 km el 2026-01-01 00:00:00 a 20.320 km el "
					+ "2026-01-11 00:00:00: 320 km en 10 días, unos 32 km por día. Segundo periodo: "
					+ "de 10.000 km el 2025-01-01 00:00:00 a 10.200 km el 2025-01-11 00:00:00: "
					+ "200 km en 10 días, unos 20 km por día. El cambio del primero respecto al "
					+ "segundo: un 60% más."));

			Assert.That(
				TheComparisonSaidIn(schedule, TheLanguage.OfAnEnglishReader),
				Is.EqualTo(
					"First period: from 20,000 km on 2026-01-01 00:00:00 to 20,320 km on "
					+ "2026-01-11 00:00:00: 320 km in 10 days, about 32 km per day. Second period: "
					+ "from 10,000 km on 2025-01-01 00:00:00 to 10,200 km on 2025-01-11 00:00:00: "
					+ "200 km in 10 days, about 20 km per day. The change of the first against the "
					+ "second: 60% more."));
		}

		// LAS OTRAS CUATRO SITUACIONES DEL CAMBIO, que son las que no son un porcentaje hacia arriba.
		// Ninguna dice cero: un cero por ciento se leeria como que el ritmo no se movio, que es
		// exactamente una de las otras cuatro.
		[Test]
		public void TheOtherFourSituationsOfTheChangeAreSaidInBothLanguages()
		{
			MaintenanceSchedule schedule = TwoPeriodsOfUse();

			Assert.That(
				TheSentenceTheDoorWouldCompose.TheTwoPeriodsOfUseCompared(
					schedule, TheOldFrom, TheOldTo, TheNewFrom, TheNewTo, TheLanguage.OfTheRecord),
				Does.EndWith("El cambio del primero respecto al segundo: un 38% menos."));

			Assert.That(
				TheSentenceTheDoorWouldCompose.TheTwoPeriodsOfUseCompared(
					schedule, TheOldFrom, TheOldTo, TheNewFrom, TheNewTo,
					TheLanguage.OfAnEnglishReader),
				Does.EndWith("The change of the first against the second: 38% less."));

			Assert.That(
				TheSentenceTheDoorWouldCompose.TheTwoPeriodsOfUseCompared(
					schedule, TheNewFrom, TheNewTo, TheEmptyYearFrom, TheEmptyYearTo,
					TheLanguage.OfTheRecord),
				Does.EndWith(
					"No se puede decir el cambio: uno de los dos periodos no tiene ritmo que decir."));

			Assert.That(
				TheSentenceTheDoorWouldCompose.TheTwoPeriodsOfUseCompared(
					schedule, TheNewFrom, TheNewTo, TheEmptyYearFrom, TheEmptyYearTo,
					TheLanguage.OfAnEnglishReader),
				Does.EndWith(
					"The change cannot be said: one of the two periods has no rate to say."));

			MaintenanceSchedule still = AStillPeriodAndAMovingOne();

			Assert.That(
				TheSentenceTheDoorWouldCompose.TheTwoPeriodsOfUseCompared(
					still, TheNewFrom, TheNewTo, TheOldFrom, TheOldTo, TheLanguage.OfTheRecord),
				Does.EndWith(
					"El cambio del primero respecto al segundo: no se puede decir como porcentaje, "
					+ "porque en el otro periodo el vehículo no se movió."));

			Assert.That(
				TheSentenceTheDoorWouldCompose.TheTwoPeriodsOfUseCompared(
					still, TheNewFrom, TheNewTo, TheOldFrom, TheOldTo,
					TheLanguage.OfAnEnglishReader),
				Does.EndWith(
					"The change of the first against the second: it cannot be said as a percentage, "
					+ "because the vehicle did not move in the other period."));

			MaintenanceSchedule same = TheSameRateTwice();

			Assert.That(
				TheSentenceTheDoorWouldCompose.TheTwoPeriodsOfUseCompared(
					same, TheNewFrom, TheNewTo, TheOldFrom, TheOldTo, TheLanguage.OfTheRecord),
				Does.EndWith("El cambio del primero respecto al segundo: el mismo ritmo."));

			Assert.That(
				TheSentenceTheDoorWouldCompose.TheTwoPeriodsOfUseCompared(
					same, TheNewFrom, TheNewTo, TheOldFrom, TheOldTo,
					TheLanguage.OfAnEnglishReader),
				Does.EndWith("The change of the first against the second: the same rate."));
		}

		// LA FRASE DE CADA FILA, con su fuente dicha por el catalogo. La fuente es un conjunto cerrado
		// -- lo elige el dominio de una lista suya, no salio del feed -- asi que se traduce.
		[Test]
		public void EveryRowIsSaidWithItsSourceInBothLanguages()
		{
			MaintenanceSchedule schedule = AnEmptySchedule();

			schedule.RecordReading(
				20000, DistanceUnitCode.UnitA, TheTenthOfJanuary, ReadingSourceCode.Telemetry);
			schedule.RecordReading(
				21000, DistanceUnitCode.UnitA, TheTenthOfFebruary,
				ReadingSourceCode.CustomerDeclared);
			schedule.RecordReading(
				22000, DistanceUnitCode.UnitA, TheTenthOfMarch, ReadingSourceCode.Correction);

			Assert.That(
				TheSentenceTheDoorWouldCompose.EachReadingOnTheRecord(
					schedule, TheYearFrom, TheYearTo, TheLanguage.OfTheRecord),
				Is.EqualTo(new[]
				{
					"20.000 km el 2026-01-10 00:00:00, reportada por el vehículo",
					"21.000 km el 2026-02-10 00:00:00, declarada por la persona",
					"22.000 km el 2026-03-10 00:00:00, corrección de una lectura anterior",
				}));

			Assert.That(
				TheSentenceTheDoorWouldCompose.EachReadingOnTheRecord(
					schedule, TheYearFrom, TheYearTo, TheLanguage.OfAnEnglishReader),
				Is.EqualTo(new[]
				{
					"20,000 km on 2026-01-10 00:00:00, reported by the vehicle",
					"21,000 km on 2026-02-10 00:00:00, declared by the person",
					"22,000 km on 2026-03-10 00:00:00, a correction of an earlier reading",
				}));
		}

		// NINGUNA RESPUESTA DE LA SERIE SE QUEDA CON UN HUECO SIN LLENAR NI CON UNA CLAVE CRUDA. Es el
		// barrido que hace que las pruebas de arriba no dependan de que a alguien se le ocurriera el
		// caso: Say() devuelve "{clave}" cuando no la tiene y deja "{hueco}" cuando el nombre no
		// calza, y las dos cosas se ven con una llave en la frase final.
		[Test]
		public void NoAnswerOfTheSeriesComesBackWithAHoleOrARawKeyInIt()
		{
			foreach (string language in TheTwoLanguages())
				foreach (string said in EverySentenceOfTheSeries(language))
					Assert.That(said, Does.Not.Contain("{").And.Not.Contain("}"),
						"Una llave en la frase final es una clave que el catalogo no tiene o un hueco "
						+ "cuyo nombre no calza con el que quien compone le pasa. En " + language
						+ ": " + said);
		}

		private IEnumerable<string> EverySentenceOfTheSeries(string language)
		{
			MaintenanceSchedule measured = AnEmptySchedule();

			Arrives(measured, 20000, TheTenthOfJanuary);
			Arrives(measured, 22000, TheTenthOfMarch);

			MaintenanceSchedule one = AnEmptySchedule();

			Arrives(one, 20000, TheTenthOfJanuary);

			MaintenanceSchedule mixed = AnEmptySchedule();

			Arrives(mixed, 20000, TheTenthOfJanuary);
			mixed.RecordReading(
				500, DistanceUnitCode.UnitB, TheTenthOfFebruary, ReadingSourceCode.CustomerDeclared);

			MaintenanceSchedule together = AnEmptySchedule();

			Arrives(together, 20000, TheSameSecond);
			Arrives(together, 20000, TheSameSecond);

			MaintenanceSchedule two = TwoPeriodsOfUse();

			List<string> said = new List<string>
			{
				TheRateSaidIn(measured, language),
				TheRateSaidIn(one, language),
				TheRateSaidIn(mixed, language),
				TheRateSaidIn(together, language),
				TheSentenceTheDoorWouldCompose.TheRateOfUse(
					one, TheEmptyYearFrom, TheEmptyYearTo, language),
				TheSpanOf(1, OneSecondLater, language),
				TheSpanOf(1, FortySecondsLater, language),
				TheComparisonSaidIn(two, language),
				TheSentenceTheDoorWouldCompose.TheTwoPeriodsOfUseCompared(
					two, TheOldFrom, TheOldTo, TheNewFrom, TheNewTo, language),
				TheSentenceTheDoorWouldCompose.TheTwoPeriodsOfUseCompared(
					two, TheNewFrom, TheNewTo, TheEmptyYearFrom, TheEmptyYearTo, language),
				TheSentenceTheDoorWouldCompose.TheTwoPeriodsOfUseCompared(
					AStillPeriodAndAMovingOne(), TheNewFrom, TheNewTo, TheOldFrom, TheOldTo,
					language),
				TheSentenceTheDoorWouldCompose.TheTwoPeriodsOfUseCompared(
					TheSameRateTwice(), TheNewFrom, TheNewTo, TheOldFrom, TheOldTo, language),
			};

			said.AddRange(TheSentenceTheDoorWouldCompose.EachReadingOnTheRecord(
				measured, TheYearFrom, TheYearTo, language));

			return said;
		}

		private static string TheRateSaidIn(MaintenanceSchedule schedule, string language)
		{
			return TheSentenceTheDoorWouldCompose.TheRateOfUse(
				schedule, TheYearFrom, TheYearTo, language);
		}

		private static string TheComparisonSaidIn(MaintenanceSchedule schedule, string language)
		{
			return TheSentenceTheDoorWouldCompose.TheTwoPeriodsOfUseCompared(
				schedule, TheNewFrom, TheNewTo, TheOldFrom, TheOldTo, language);
		}

		// UN TRAMO MAS CORTO QUE UN DIA: dos lecturas del mismo dia separadas por esos segundos.
		private static string TheSpanOf(int distance, DateTime laterEnd, string language)
		{
			MaintenanceSchedule schedule = AnEmptySchedule();

			Arrives(schedule, 20000, TheSameSecond);
			Arrives(schedule, 20000 + distance, laterEnd);

			return TheSentenceTheDoorWouldCompose.TheRateOfUse(
				schedule, TheYearFrom, TheYearTo, language);
		}

		private static MaintenanceSchedule TwoPeriodsOfUse()
		{
			MaintenanceSchedule schedule = AnEmptySchedule();

			Arrives(schedule, 10000, TheOldFrom);
			Arrives(schedule, 10200, TheOldTo);
			Arrives(schedule, 20000, TheNewFrom);
			Arrives(schedule, 20320, TheNewTo);

			return schedule;
		}

		// EL PERIODO VIEJO QUIETO Y EL NUEVO EN MOVIMIENTO: cero SI es un ritmo medido -- quiere decir
		// que el vehiculo no se movio -- y un porcentaje contra el no existe.
		private static MaintenanceSchedule AStillPeriodAndAMovingOne()
		{
			MaintenanceSchedule schedule = AnEmptySchedule();

			Arrives(schedule, 10000, TheOldFrom);
			Arrives(schedule, 10000, TheOldTo);
			Arrives(schedule, 10000, TheNewFrom);
			Arrives(schedule, 10320, TheNewTo);

			return schedule;
		}

		private static MaintenanceSchedule TheSameRateTwice()
		{
			MaintenanceSchedule schedule = AnEmptySchedule();

			Arrives(schedule, 10000, TheOldFrom);
			Arrives(schedule, 10200, TheOldTo);
			Arrives(schedule, 20000, TheNewFrom);
			Arrives(schedule, 20200, TheNewTo);

			return schedule;
		}

		private static MaintenanceSchedule AnEmptySchedule()
		{
			return new MaintenanceSchedule(TheVin, UsageProfileCode.Standard);
		}

		private static void Arrives(MaintenanceSchedule schedule, int value, DateTime observedAt)
		{
			schedule.RecordReading(
				value, DistanceUnitCode.UnitA, observedAt, ReadingSourceCode.Telemetry);
		}

		private static string[] TheTwoLanguages()
		{
			return new[] { TheLanguage.OfTheRecord, TheLanguage.OfAnEnglishReader };
		}
	}
}
