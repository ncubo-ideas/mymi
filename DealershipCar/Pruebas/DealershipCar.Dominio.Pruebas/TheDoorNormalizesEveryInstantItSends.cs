using System;
using DealershipCar.App;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LA REGLA 1 DE §3.5, PROBADA EN VEZ DE CONFIADA: todos los instantes son UTC por convencion, y
	// se normalizan en la frontera de DealershipCarAPI. El dominio no lee un reloj y tampoco lo
	// interpreta -- nunca llama a ToLocalTime, ToUniversalTime ni mira .Kind -- asi que si la
	// frontera no normaliza, no normaliza nadie.
	//
	// NORMALIZAR SON DOS COSAS, y la segunda no estaba prevista en el plan:
	//
	//   AL ENTRAR, a UTC y truncado al SEGUNDO. El journal escribe el DateTime como texto con
	//   granularidad de segundo (SPIKES-OS2 S4), asi que un instante con fraccion contesta una cosa
	//   en memoria y otra despues del replay. Truncar en la frontera es lo que hace que la corrida
	//   viva y la reproducida sean la misma corrida.
	//
	//   AL SALIR, volviendo a estampar Utc. El journal ademas BORRA el DateTimeKind, asi que el
	//   mismo instante vuelve Utc vivo y Unspecified reproducido. El valor no cambia -- DateTime
	//   compara por ticks -- pero una caratula que lo formatee o lo pase a hora local obtiene dos
	//   respuestas para el mismo hecho.
	//
	// Estas pruebas son las unicas del nivel B que mandan instantes que no son medianoche exacta en
	// UTC, y por eso son las unicas que pueden ver cualquiera de las dos mitades.
	//
	// T5 LE AGREGO LOS CUATRO INSTANTES DEL SOBRE Y DEL ODOMETRO, y con ellos la mitad de ENTRADA
	// dejo de ser incomprobable. Hasta aca, el instante que entraba por una LECTURA se comparaba
	// siempre contra otro que ya habia pasado por el mismo truncado, asi que quitarlo no ponia roja
	// ninguna prueba -- esta escrito en ASpecificationIsReadBackThroughTheDoor, junto con la promesa
	// de escribirla el dia que algo comparara ese instante con resolucion de segundo. La lectura del
	// odometro es ese dia.
	[TestFixture]
	public sealed class TheDoorNormalizesEveryInstantItSends
	{
		private const string TheVin = "1HGCM82633A004352";
		private const int TwelveMonthsInDays = 365;
		private const int TenThousandUnits = 10000;

		private static readonly DateTime Delivery =
			new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);
		private const int OdometerAtDelivery = 30000;

		private AScheduleOnARealJournal journal;

		[SetUp]
		public void Setup()
		{
			journal = new AScheduleOnARealJournal(TheVin, "Standard");

			journal.Api.SeedPlanOperation(
				"FluidChange", "TimeOrDistance", TwelveMonthsInDays, TenThousandUnits, "UnitA",
				true, false,
				Delivery, OdometerAtDelivery, "UnitA");
		}

		[TearDown]
		public void TearDown()
		{
			journal.Dispose();
		}

		// La fraccion se pierde en el journal, asi que se pierde tambien en memoria: la agenda
		// contesta el instante TRUNCADO desde el primer momento, y no uno vivo y otro reproducido.
		[Test]
		public void AnInstantWithAFractionAnswersTheSameLiveAndReplayed()
		{
			DateTime withMilliseconds = new DateTime(
				2026, 1, 20, 13, 45, 30, 123, DateTimeKind.Utc);

			journal.Api.Evaluate(withMilliseconds);

			DateTime live = journal.Api.LastEvaluatedAt();

			Assert.That(live, Is.EqualTo(new DateTime(2026, 1, 20, 13, 45, 30, DateTimeKind.Utc)),
				"Truncado al segundo YA en la frontera. Sin truncar, esto contestaria .123 vivo.");

			journal.CloseAndReopenFromTheJournal();

			Assert.That(journal.Api.LastEvaluatedAt(), Is.EqualTo(live),
				"Y reproducido contesta lo mismo. Sin truncar en la frontera, el journal lo "
				+ "truncaria igual y las dos respuestas dejarian de coincidir -- en silencio, "
				+ "porque no hay excepcion ni aviso (SPIKES-OS2 S4).");
		}

		// Y LA LECTURA COMPUESTA CONVIERTE SU INSTANTE TAMBIEN, con la misma forma que la de arriba:
		// un instante en hora local nombra el mismo momento que su UTC. Es lo que T7 va a necesitar
		// -- la consola tiene DateTime.Now a mano, que es Local -- y ademas es la UNICA mitad de la
		// normalizacion de una lectura que se puede observar: truncar al segundo un instante que ya
		// viene en segundos enteros no cambia ninguna respuesta, porque se compara siempre contra
		// otro que paso por el mismo truncado.
		//
		// LA LECTURA DEL ODOMETRO ESTA PUESTA DENTRO DE LA VENTANA DEL DESPLAZAMIENTO -- dos horas
		// despues del valor de reloj -- para que la comparacion discrimine: sin convertir, la
		// pregunta cae del otro lado de esa lectura y la frase pasa a ser la de "no hay lectura".
		//
		// SU LIMITE, DICHO Y NO DISIMULADO: el desplazamiento es el de la maquina que corre la
		// prueba. En una maquina en UTC no hay ninguno, las dos llamadas son la misma y esta prueba
		// no mide nada. Es la misma condicion que ALocalInstantNamesTheSameMomentAsItsUtc, que es el
		// precedente de la casa para el lado de los comandos.
		[Test]
		public void AComposedReadTakesALocalInstantAsTheSameMomentAsItsUtc()
		{
			journal.Api.PublishWarrantyEnvelope(
				36, 60000, "UnitB", new DateTime(2023, 11, 15, 0, 0, 0, DateTimeKind.Utc));
			journal.Api.RecordDelivery(Delivery, OdometerAtDelivery, "UnitB");
			journal.Api.RecordReading(
				40000, "UnitB", new DateTime(2026, 1, 20, 10, 0, 0, DateTimeKind.Utc), "Telemetry");

			DateTime local = new DateTime(2026, 1, 20, 8, 0, 0, DateTimeKind.Local);

			Assert.That(
				journal.Api.DescribeWarrantyEnvelope(local),
				Is.EqualTo(journal.Api.DescribeWarrantyEnvelope(local.ToUniversalTime())),
				"El mismo momento, dicho de dos maneras, es una sola respuesta. Sin convertir en la "
				+ "frontera, la seccion de garantia leeria las 8:00 de una zona como las 8:00 UTC y "
				+ "preguntaria por un momento distinto del que la persona quiso.");

			Assert.That(
				journal.Api.CanTheEnvelopeBeEvaluated(local),
				Is.EqualTo(journal.Api.CanTheEnvelopeBeEvaluated(local.ToUniversalTime())));
		}

		// La consecuencia de truncar, dicha en voz alta: dos instantes del mismo segundo son EL
		// MISMO instante para esta app, asi que el segundo se rechaza por la cadencia de C6. Mejor
		// que se vea aca que en el journal de alguien.
		[Test]
		public void TwoInstantsInsideTheSameSecondAreTheSameInstant()
		{
			DateTime first = new DateTime(2026, 1, 20, 13, 45, 30, 100, DateTimeKind.Utc);

			Assert.That(journal.Api.Evaluate(first).WasEvaluated, Is.True);

			EvaluationOutcome sameSecond = journal.Api.Evaluate(first.AddMilliseconds(500));

			Assert.That(sameSecond.WasEvaluated, Is.False,
				"No es un capricho de la guarda: los dos instantes se escriben iguales, asi que "
				+ "dejarlos pasar journalizaria dos evaluaciones del mismo instante.");
		}

		// Un instante en hora local nombra el mismo momento que su UTC. La caratula de consola
		// (T7) va a tener DateTime.Now a mano, que es Local: si la frontera no convirtiera, la
		// agenda leeria las 14:00 de Costa Rica como las 14:00 UTC y todo se correria seis horas.
		[Test]
		public void ALocalInstantNamesTheSameMomentAsItsUtc()
		{
			DateTime local = new DateTime(2026, 1, 20, 8, 0, 0, DateTimeKind.Local);

			journal.Api.Evaluate(local);

			Assert.That(journal.Api.LastEvaluatedAt(), Is.EqualTo(TruncatedToTheSecond(
				local.ToUniversalTime())));
		}

		// Un instante sin Kind se toma como UTC ya: es la convencion de §3.5 y no hay de donde
		// sacar otra cosa. Dicho aca para que sea una decision y no un descuido.
		[Test]
		public void AnInstantWithoutAKindIsTakenAsUtcAlready()
		{
			DateTime unspecified = new DateTime(2026, 1, 20, 8, 0, 0, DateTimeKind.Unspecified);

			journal.Api.Evaluate(unspecified);

			Assert.That(
				journal.Api.LastEvaluatedAt(),
				Is.EqualTo(new DateTime(2026, 1, 20, 8, 0, 0, DateTimeKind.Utc)));
		}

		// Y el instante VUELVE etiquetado, vivo y reproducido. Sin esto, la misma respuesta se
		// formatea distinto segun si la app se acaba de abrir o no.
		[Test]
		public void AnInstantComesBackLabelledUtcBothLiveAndReplayed()
		{
			journal.Api.Evaluate(new DateTime(2026, 1, 20, 13, 45, 30, DateTimeKind.Utc));

			Assert.That(journal.Api.LastEvaluatedAt().Kind, Is.EqualTo(DateTimeKind.Utc));

			journal.CloseAndReopenFromTheJournal();

			Assert.That(journal.Api.LastEvaluatedAt().Kind, Is.EqualTo(DateTimeKind.Utc),
				"El journal borra el Kind (SPIKES-OS2 S4). Reponerlo en la frontera no inventa "
				+ "nada: repone la etiqueta que la convencion ya afirmaba al entrar.");
		}

		// ===== LOS CUATRO INSTANTES QUE TRAJO T5 ==========================================

		// LA MITAD DE ENTRADA, Y ES LA QUE POR FIN SE PUEDE FALSIFICAR. Hasta T5 el truncado de un
		// instante que entra por una LECTURA no tenia consecuencia observable: se comparaba contra
		// otro que ya habia pasado por el mismo truncado, y quitarlo no ponia roja ninguna prueba.
		// Con la lectura del odometro si la tiene: el instante de OBSERVACION entra por comando y se
		// compara con resolucion de segundo, asi que sin truncar en la frontera la lectura queda
		// invisible medio segundo VIVA y visible REPRODUCIDA -- las dos respuestas distintas para el
		// mismo hecho, en silencio.
		[Test]
		public void AReadingObservedWithAFractionIsVisibleAtTheWholeSecondBothLiveAndReplayed()
		{
			DateTime withMilliseconds = new DateTime(
				2026, 2, 14, 11, 30, 45, 750, DateTimeKind.Utc);

			DateTime itsWholeSecond = new DateTime(2026, 2, 14, 11, 30, 45, DateTimeKind.Utc);

			journal.Api.RecordReading(38400, "UnitB", withMilliseconds, "Telemetry");

			Assert.That(journal.Api.HasAnOdometerReading(itsWholeSecond), Is.True,
				"Preguntada al segundo entero, la lectura YA esta ahi. Sin truncar al entrar, el "
				+ "dominio la tendria fechada .750 y esto contestaria que no hay ninguna -- y despues "
				+ "de reabrir contestaria que si, porque el journal la trunca igual.");

			Assert.That(
				journal.Api.ReadOdometerObservedAt(itsWholeSecond), Is.EqualTo(itsWholeSecond));

			journal.CloseAndReopenFromTheJournal();

			Assert.That(journal.Api.HasAnOdometerReading(itsWholeSecond), Is.True);
			Assert.That(
				journal.Api.ReadOdometerObservedAt(itsWholeSecond), Is.EqualTo(itsWholeSecond));
		}

		// LOS DOS HECHOS DE LA AGENCIA ENTRAN EN HORA LOCAL Y CON FRACCION, que es lo unico que hace
		// que esta prueba mire la normalizacion y no se ponga verde por casualidad: un instante que
		// ya viene en UTC y en segundo entero deja a InUtc sin nada que hacer.
		[Test]
		public void TheTwoProfileInstantsArriveLocalWithAFractionAndComeBackAsTheirUtcWholeSecond()
		{
			DateTime handedOver =
				new DateTime(2024, 1, 1, 8, 0, 0, DateTimeKind.Local).AddTicks(1234567);

			DateTime publishedAt =
				new DateTime(2023, 11, 15, 9, 30, 45, DateTimeKind.Local).AddTicks(9876543);

			journal.Api.RecordDelivery(handedOver, 1200, "UnitB");
			journal.Api.PublishWarrantyEnvelope(36, 60000, "UnitB", publishedAt);

			Assert.That(
				journal.Api.ReadDeliveredAt(),
				Is.EqualTo(TruncatedToTheSecond(handedOver.ToUniversalTime())));

			Assert.That(
				journal.Api.ReadEnvelopePublishedAt(),
				Is.EqualTo(TruncatedToTheSecond(publishedAt.ToUniversalTime())));
		}

		// Y LOS TRES INSTANTES QUE SALEN VUELVEN ETIQUETADOS Utc, vivos y reproducidos. El journal
		// borra el Kind, asi que sin reponerlo en la frontera la misma respuesta se formatea distinto
		// segun si la app se acaba de abrir o no.
		[Test]
		public void TheThreeInstantsTheProfileAndTheOdometerHandBackComeBackLabelledUtc()
		{
			DateTime handedOver = new DateTime(2024, 1, 1, 8, 0, 0, DateTimeKind.Utc);
			DateTime publishedAt = new DateTime(2023, 11, 15, 9, 30, 45, DateTimeKind.Utc);
			DateTime observedAt = new DateTime(2026, 2, 14, 11, 30, 45, DateTimeKind.Utc);

			journal.Api.RecordDelivery(handedOver, 1200, "UnitB");
			journal.Api.PublishWarrantyEnvelope(36, 60000, "UnitB", publishedAt);
			journal.Api.RecordReading(38400, "UnitB", observedAt, "Telemetry");

			EveryKindIsUtc(observedAt);

			journal.CloseAndReopenFromTheJournal();

			EveryKindIsUtc(observedAt);
		}

		private void EveryKindIsUtc(DateTime asOf)
		{
			Assert.That(journal.Api.ReadDeliveredAt().Kind, Is.EqualTo(DateTimeKind.Utc));
			Assert.That(journal.Api.ReadEnvelopePublishedAt().Kind, Is.EqualTo(DateTimeKind.Utc));
			Assert.That(
				journal.Api.ReadOdometerObservedAt(asOf).Kind, Is.EqualTo(DateTimeKind.Utc));
		}

		private static DateTime TruncatedToTheSecond(DateTime instant)
		{
			return new DateTime(
				instant.Ticks - instant.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);
		}
	}
}
