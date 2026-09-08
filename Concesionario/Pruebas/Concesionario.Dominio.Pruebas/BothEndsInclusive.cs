using System;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LEY L5 DEL SLICE, tests 4 y 5 de §11 del brief. "Una estadia que entro el 10 y se recogio el
	// 12 cubrio TRES dias. La manana en que entro y la manana en que salio son las dos mananas en
	// que alguien llevo a alguien al colegio."
	//
	// LOS DIAS SE RECIBEN, asi que "ambos extremos inclusive" no es una cuenta que el dominio hace:
	// es lo que la sucursal manda y lo que la ley L5 de AClosedStay exige que sea CONTIGUO. Lo que
	// esta prueba fija es que el conteo compuesto no le quita ni le pone un dia a lo que llego.
	//
	// TRES ESCENARIOS Y NO UNO (C61): tres dias, un dia y dos dias. Con un solo caso, un conteo
	// constante -- o un conteo que sumara uno "por ser inclusivo" -- pasaria igual.
	[TestFixture]
	public sealed class BothEndsInclusive
	{
		private const string TheVin = "1FTFW1ET5DFC10312";
		private const string TheBranch = "Taller Curridabat";
		private const string TheSubject = "Cambio de aceite";

		private const string TheFirstOfJune = "2026-06-01";
		private const string TheLastOfJuly = "2026-07-31";

		private ShopStay stays;

		[SetUp]
		public void Setup()
		{
			stays = new ShopStay(TheVin);
		}

		// TEST 4 DE §11: entro el 10, se recogio el 12, llegaron el 10, el 11 y el 12 -> TRES dias,
		// y la lectura los NOMBRA.
		[Test]
		public void EnteredOnTheTenthAndCollectedOnTheTwelfthCoversThreeNamedDays()
		{
			stays.RecordStay(TheBranch, TheSubject,
				new DateTime(2026, 6, 10, 8, 30, 0), new DateTime(2026, 6, 12, 16, 45, 0),
				new[] { "2026-06-10", "2026-06-11", "2026-06-12" });

			Assert.That(stays.DaysInTheShopBetween(TheFirstOfJune, TheLastOfJuly),
				Is.EqualTo(new[] { "2026-06-10", "2026-06-11", "2026-06-12" }));
			Assert.That(stays.DaysInTheShopCountBetween(TheFirstOfJune, TheLastOfJuly), Is.EqualTo(3));
		}

		// TEST 5 DE §11: entro y salio el mismo dia -> UN dia. Es el caso que un "salida menos
		// entrada" daria como cero, y la manana en que entro sigue siendo una manana.
		[Test]
		public void EnteredAndCollectedOnTheSameDayCoversOneDay()
		{
			stays.RecordStay(TheBranch, TheSubject,
				new DateTime(2026, 7, 5, 9, 0, 0), new DateTime(2026, 7, 5, 17, 0, 0),
				new[] { "2026-07-05" });

			Assert.That(stays.DaysInTheShopBetween(TheFirstOfJune, TheLastOfJuly),
				Is.EqualTo(new[] { "2026-07-05" }));
			Assert.That(stays.DaysInTheShopCountBetween(TheFirstOfJune, TheLastOfJuly), Is.EqualTo(1));
		}

		// EL TERCER ESCENARIO, que es el control: dos dias contestan dos. Sin este renglon, un
		// conteo que devolviera siempre tres pasaria el primero y uno que devolviera siempre uno
		// pasaria el segundo.
		[Test]
		public void TwoReceivedDaysAnswerTwo()
		{
			stays.RecordStay(TheBranch, TheSubject,
				new DateTime(2026, 6, 20, 7, 0, 0), new DateTime(2026, 6, 21, 9, 0, 0),
				new[] { "2026-06-20", "2026-06-21" });

			Assert.That(stays.DaysInTheShopCountBetween(TheFirstOfJune, TheLastOfJuly), Is.EqualTo(2));
		}

		// Y LA FRASE DE LA ESTADIA DICE LOS MISMOS DIAS que la lectura del periodo. Dos frases que
		// contaran distinto serian dos verdades sobre un mismo hecho.
		[Test]
		public void TheStaysOwnSentenceSaysTheSameDaysThePeriodReadDoes()
		{
			stays.RecordStay(TheBranch, TheSubject,
				new DateTime(2026, 6, 10, 8, 30, 0), new DateTime(2026, 6, 12, 16, 45, 0),
				new[] { "2026-06-10", "2026-06-11", "2026-06-12" });

			string said = stays.DescribeTheStayAt(new DateTime(2026, 6, 11, 12, 0, 0));

			Assert.That(said, Does.Contain("3 días"));
			Assert.That(said, Does.Contain("2026-06-10"));
			Assert.That(said, Does.Contain("2026-06-12"));
		}

		// Y EL SINGULAR SE DICE EN SINGULAR. Es la unica decision de forma que el dominio toma aqui,
		// y la toma el dominio porque la frase es suya.
		[Test]
		public void OneDayIsSaidInTheSingular()
		{
			stays.RecordStay(TheBranch, TheSubject,
				new DateTime(2026, 7, 5, 9, 0, 0), new DateTime(2026, 7, 5, 17, 0, 0),
				new[] { "2026-07-05" });

			string said = stays.DescribeTheStayAt(new DateTime(2026, 7, 5, 12, 0, 0));

			Assert.That(said, Does.Contain("1 día"));
			Assert.That(said, Does.Not.Contain("1 días"));
		}
	}
}
