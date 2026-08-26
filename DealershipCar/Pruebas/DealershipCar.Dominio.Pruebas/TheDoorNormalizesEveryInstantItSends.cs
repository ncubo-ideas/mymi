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

		private static DateTime TruncatedToTheSecond(DateTime instant)
		{
			return new DateTime(
				instant.Ticks - instant.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);
		}
	}
}
