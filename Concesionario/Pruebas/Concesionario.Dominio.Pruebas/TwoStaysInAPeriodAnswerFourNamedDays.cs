using System;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// TEST 7 DE §11 DEL BRIEF, que es L3 sobre mas de una estadia: "Dos estadias en un periodo, una
	// de tres dias y una de uno -> la lectura de dias contesta CUATRO DIAS NOMBRADOS, y la de conteo
	// contesta 4. Afirmar que los dias son direccionables uno por uno; eso es lo que un cruce
	// necesita."
	//
	// TRES COSAS QUE SOLO SE VEN CON MAS DE UNA ESTADIA, y por eso este test esta aparte del de una:
	//
	//   EL ORDEN. Los dias del periodo salen ascendentes aunque las estadias hayan llegado al reves.
	//   Sin el control que las registra en orden invertido, un orden que dependiera del orden de
	//   llegada pasaria igual.
	//
	//   LOS REPETIDOS. Dos estadias que cubren el mismo dia lo dicen UNA vez. Es la mitad que
	//   ninguna estadia sola puede probar, y se hace por operacion con nombre y no por HashSet:
	//   StayDay no tiene igualdad escrita (E23), asi que una clave de valor no existe.
	//
	//   QUE EL CONTEO DE ESTADIAS Y EL DE DIAS SON DOS PREGUNTAS. Cuatro dias en dos estadias, y
	//   despues cuatro dias en tres.
	[TestFixture]
	public sealed class TwoStaysInAPeriodAnswerFourNamedDays
	{
		private const string TheVin = "1FTFW1ET5DFC10312";
		private const string TheBranch = "Taller Curridabat";
		private const string TheOtherBranch = "Taller Escazu";
		private const string TheSubject = "Cambio de aceite";
		private const string TheOtherSubject = "Llanta ponchada";

		private const string TheFirstOfJune = "2026-06-01";
		private const string TheLastOfJune = "2026-06-30";

		private static readonly string[] TheFourDays =
			{ "2026-06-10", "2026-06-11", "2026-06-12", "2026-06-20" };

		private ShopStay stays;

		[SetUp]
		public void Setup()
		{
			stays = new ShopStay(TheVin);
		}

		[Test]
		public void ThreeDaysAndOneDayAnswerFourNamedDays()
		{
			TheStayOfThreeDays();
			TheStayOfOneDay();

			Assert.That(stays.DaysInTheShopBetween(TheFirstOfJune, TheLastOfJune),
				Is.EqualTo(TheFourDays));
			Assert.That(stays.DaysInTheShopCountBetween(TheFirstOfJune, TheLastOfJune), Is.EqualTo(4));
			Assert.That(stays.StayCountBetween(TheFirstOfJune, TheLastOfJune), Is.EqualTo(2));
		}

		// CADA UNO DIRECCIONABLE POR SEPARADO, por valor y en su posicion. Es lo que el brief pide
		// afirmar y es lo unico que un cruce con otra institucion puede usar.
		[Test]
		public void EachOfTheFourDaysIsAddressableOnItsOwn()
		{
			TheStayOfThreeDays();
			TheStayOfOneDay();

			string[] days = stays.DaysInTheShopBetween(TheFirstOfJune, TheLastOfJune);

			Assert.That(days[0], Is.EqualTo("2026-06-10"));
			Assert.That(days[1], Is.EqualTo("2026-06-11"));
			Assert.That(days[2], Is.EqualTo("2026-06-12"));
			Assert.That(days[3], Is.EqualTo("2026-06-20"));
		}

		// EL ORDEN NO ES EL DE LLEGADA. Registradas al reves, los dias salen igual de ascendentes.
		[Test]
		public void TheDaysComeOutAscendingEvenWhenTheStaysArrivedInTheOtherOrder()
		{
			TheStayOfOneDay();
			TheStayOfThreeDays();

			Assert.That(stays.DaysInTheShopBetween(TheFirstOfJune, TheLastOfJune),
				Is.EqualTo(TheFourDays));
		}

		// LOS REPETIDOS SE DICEN UNA VEZ. Una tercera estadia, el mismo 12, y siguen siendo cuatro
		// dias -- en TRES estadias.
		[Test]
		public void ADayCoveredByTwoStaysIsSaidOnce()
		{
			TheStayOfThreeDays();
			TheStayOfOneDay();

			stays.RecordStay(TheOtherBranch, "Segunda visita el mismo dia",
				new DateTime(2026, 6, 12, 18, 0, 0), new DateTime(2026, 6, 12, 20, 0, 0),
				new[] { "2026-06-12" });

			Assert.That(stays.DaysInTheShopBetween(TheFirstOfJune, TheLastOfJune),
				Is.EqualTo(TheFourDays),
				"el 12 lo cubren dos estadias y es UN dia en el taller");
			Assert.That(stays.DaysInTheShopCountBetween(TheFirstOfJune, TheLastOfJune), Is.EqualTo(4));
			Assert.That(stays.StayCountBetween(TheFirstOfJune, TheLastOfJune), Is.EqualTo(3),
				"y las estadias si son tres: son dos preguntas distintas.");
		}

		// LA FRASE NOMBRA LOS CUATRO. Sin esto, la lectura de dias podria estar bien y la frase que
		// una persona lee decir otra cosa.
		[Test]
		public void TheSentenceNamesAllFourDaysAndBothCounts()
		{
			TheStayOfThreeDays();
			TheStayOfOneDay();

			string said = stays.DescribeDaysInTheShopBetween(TheFirstOfJune, TheLastOfJune);

			foreach (string day in TheFourDays) Assert.That(said, Does.Contain(day));

			Assert.That(said, Does.Contain("4 días"));
			Assert.That(said, Does.Contain("2 estadías"));
			Assert.That(said, Does.Contain(TheFirstOfJune));
			Assert.That(said, Does.Contain(TheLastOfJune));
		}

		// EL CONTROL DEL PERIODO (C61): un periodo mas angosto ve menos. Sin el, una lectura que
		// ignorara los limites pasaria todo lo de arriba.
		[Test]
		public void ANarrowerPeriodSeesOnlyTheDaysInsideIt()
		{
			TheStayOfThreeDays();
			TheStayOfOneDay();

			Assert.That(stays.DaysInTheShopBetween("2026-06-11", "2026-06-19"),
				Is.EqualTo(new[] { "2026-06-11", "2026-06-12" }));
			Assert.That(stays.StayCountBetween("2026-06-11", "2026-06-19"), Is.EqualTo(1));
		}

		private void TheStayOfThreeDays()
		{
			stays.RecordStay(TheBranch, TheSubject,
				new DateTime(2026, 6, 10, 8, 30, 0), new DateTime(2026, 6, 12, 16, 45, 0),
				new[] { "2026-06-10", "2026-06-11", "2026-06-12" });
		}

		private void TheStayOfOneDay()
		{
			stays.RecordStay(TheOtherBranch, TheOtherSubject,
				new DateTime(2026, 6, 20, 7, 0, 0), new DateTime(2026, 6, 20, 11, 0, 0),
				new[] { "2026-06-20" });
		}
	}
}
