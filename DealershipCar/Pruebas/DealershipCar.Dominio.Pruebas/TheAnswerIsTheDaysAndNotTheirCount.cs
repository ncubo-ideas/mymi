using System;
using System.Globalization;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LEY L3 DEL SLICE, y §1 del brief dice por que es la ley que le da valor a la rebanada: "Estuvo
	// en el taller el 10, 11 y 12 de junio" no es una duracion, son TRES FECHAS -- y esas tres
	// fechas son exactamente las tres llegadas tarde de Mateo al colegio. Un conteo no se puede
	// cruzar con nada.
	//
	// POR ESO SE AFIRMA EL TERCER DIA POR VALOR y no la cantidad. Un conteo de 3 lo cumple igual una
	// lista con los dias equivocados o en otro orden (C60): la afirmacion tiene que nombrar el dia.
	//
	// Y POR ESO SE AFIRMA QUE CADA DIA ES DIRECCIONABLE por separado y legible por alguien que no
	// sabe nada de este modulo -- que es lo unico que un cruce necesita. La mitad de esa afirmacion
	// que una suite no puede dar es sacarlos por la caratula MCP, y eso lo debe SH-6.
	//
	// LA POLARIDAD INVERSA TAMBIEN SE AFIRMA: el conteo se deriva de los dias y los dias no se
	// derivan del conteo. Aqui eso es que las dos lecturas coinciden SIEMPRE, con la de dias como
	// primaria (E35).
	[TestFixture]
	public sealed class TheAnswerIsTheDaysAndNotTheirCount
	{
		private const string TheVin = "1FTFW1ET5DFC10312";
		private const string TheBranch = "Taller Curridabat";
		private const string TheSubject = "Cambio de aceite";

		private const string TheFirstOfJune = "2026-06-01";
		private const string TheLastOfJune = "2026-06-30";

		private static readonly DateTime Entered = new DateTime(2026, 6, 10, 8, 30, 0);
		private static readonly DateTime Collected = new DateTime(2026, 6, 12, 16, 45, 0);

		private static readonly string[] TheDaysItCovered =
			{ "2026-06-10", "2026-06-11", "2026-06-12" };

		private ShopStay stays;

		[SetUp]
		public void Setup()
		{
			stays = new ShopStay(TheVin);
			stays.RecordStay(TheBranch, TheSubject, Entered, Collected, TheDaysItCovered);
		}

		// EL TERCER DIA, POR VALOR. Es la afirmacion de la ley: no "son tres", sino "el tercero es
		// el 12".
		[Test]
		public void TheThirdDayIsTheTwelfthOfJuneAndIsAddressableOnItsOwn()
		{
			string[] days = stays.DaysInTheShopBetween(TheFirstOfJune, TheLastOfJune);

			Assert.That(days, Has.Length.EqualTo(3));
			Assert.That(days[2], Is.EqualTo("2026-06-12"),
				"L3: la respuesta son los DIAS. Un conteo de 3 lo cumpliria igual una lista con "
				+ "los dias equivocados (C60).");
			Assert.That(days[0], Is.EqualTo("2026-06-10"));
			Assert.That(days[1], Is.EqualTo("2026-06-11"));
		}

		// Y CADA UNO ES UNA FECHA QUE UN LECTOR AJENO PUEDE LEER. Es lo que un cruce necesita: el
		// colegio de Mateo no sabe nada de este modulo y tiene que poder emparejar las fechas.
		[Test]
		public void EachDayIsADateAReaderThatKnowsNothingAboutThisModuleCanParse()
		{
			foreach (string day in stays.DaysInTheShopBetween(TheFirstOfJune, TheLastOfJune))
			{
				DateOnly parsed;

				Assert.That(
					DateOnly.TryParseExact(day, "yyyy-MM-dd", CultureInfo.InvariantCulture,
						DateTimeStyles.None, out parsed),
					Is.True,
					$"'{day}' no es una fecha que otro sistema pueda leer, y cruzarla es el punto "
					+ "entero de L3.");
			}
		}

		// EL CONTEO SE DERIVA DE LOS DIAS. Las dos lecturas coinciden, y la de dias es la primaria:
		// de la lista sale el numero y del numero no sale la lista.
		[Test]
		public void TheCountAgreesWithTheDaysAndTheDaysAreThePrimaryAnswer()
		{
			string[] days = stays.DaysInTheShopBetween(TheFirstOfJune, TheLastOfJune);

			Assert.That(stays.DaysInTheShopCountBetween(TheFirstOfJune, TheLastOfJune),
				Is.EqualTo(days.Length));
		}

		// Y LA FRASE NOMBRA LOS DIAS, no solo cuantos son. Sin esto, una frase que dijera "3 dias"
		// pasaria las de arriba y dejaria al lector sin las fechas.
		[Test]
		public void TheSentenceNamesEveryDayAndNotJustHowManyThereAre()
		{
			string said = stays.DescribeDaysInTheShopBetween(TheFirstOfJune, TheLastOfJune);

			Assert.That(said, Does.Contain("2026-06-10"));
			Assert.That(said, Does.Contain("2026-06-11"));
			Assert.That(said, Does.Contain("2026-06-12"));
		}

		// EL CONTROL QUE HACE DISCRIMINAR A TODO LO DE ARRIBA (C61): con otros dias recibidos, la
		// respuesta es otra. Sin este renglon, una lista fija de tres dias pasaria la prueba entera.
		[Test]
		public void AStayWithOtherDaysAnswersWithThoseOtherDays()
		{
			ShopStay another = new ShopStay(TheVin);
			another.RecordStay(TheBranch, TheSubject,
				new DateTime(2026, 6, 20, 7, 0, 0), new DateTime(2026, 6, 21, 9, 0, 0),
				new[] { "2026-06-20", "2026-06-21" });

			string[] days = another.DaysInTheShopBetween(TheFirstOfJune, TheLastOfJune);

			Assert.That(days, Is.EqualTo(new[] { "2026-06-20", "2026-06-21" }));
		}
	}
}
