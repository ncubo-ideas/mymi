using System;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LEY L8 DEL SLICE, test 9 de §11 del brief. "Un periodo sin estadias contesta NO HAY ESTADIA
	// REGISTRADA, con el periodo nombrado. Nunca un cero, nunca una cadena vacia, nunca un throw."
	//
	// LA AUSENCIA ES UN TIPO (E45, E47): NoStayOnRecord, como NoUsableReading. No es un
	// StaysInAPeriod vacio con una frase generica -- es la variante que sabe que no hay ninguna y
	// dice el periodo por el que se pregunto.
	//
	// LA FRASE NOMBRA LOS DOS DIAS, y eso es lo que la separa de un cero. "0" no dice entre cuando y
	// cuando se pregunto, asi que quien lo lea no sabe si pregunto por el periodo que queria.
	//
	// Y LA FRASE DICE DE QUE HABLA: de DIAS REGISTRADOS. Una que dijera "no estuvo en el taller"
	// seria falsa mientras hay una estadia abierta sin dias todavia -- la tercera antipatron de E47,
	// una respuesta benigna que se lee como un hecho. Esa mitad la afirma
	// AnOpenStayIsANotYetAndNotAnAbsence; aca se fija la forma de la frase.
	[TestFixture]
	public sealed class NoStayIsAnAnswer
	{
		private const string TheVin = "1FTFW1ET5DFC10312";
		private const string TheBranch = "Taller Curridabat";
		private const string TheSubject = "Cambio de aceite";

		private const string TheFirstOfJune = "2026-06-01";
		private const string TheLastOfJune = "2026-06-30";
		private const string TheFirstOfJuly = "2026-07-01";
		private const string TheLastOfJuly = "2026-07-31";

		private ShopStay stays;

		[SetUp]
		public void Setup()
		{
			stays = new ShopStay(TheVin);
		}

		[Test]
		public void APeriodWithNoStayAnswersInWordsAndNamesItsTwoDays()
		{
			string said = stays.DescribeDaysInTheShopBetween(TheFirstOfJune, TheLastOfJune);

			Assert.That(said, Does.Contain(TheFirstOfJune), "L8: la frase nombra el periodo...");
			Assert.That(said, Does.Contain(TheLastOfJune), "...los DOS dias.");
			Assert.That(said, Does.Contain("días registrados"),
				"y dice de que habla: de dias REGISTRADOS, no de si el carro estuvo adentro.");
		}

		[Test]
		public void ItIsNeitherAZeroNorAnEmptyStringNorAThrow()
		{
			string said = stays.DescribeDaysInTheShopBetween(TheFirstOfJune, TheLastOfJune);

			Assert.That(said, Is.Not.Empty);
			Assert.That(said, Does.Not.Contain("0 días"),
				"L8: nunca un cero. Un cero no dice por que periodo se pregunto.");
			Assert.That(stays.HasAnyStayBetween(TheFirstOfJune, TheLastOfJune), Is.False);
			Assert.That(stays.DaysInTheShopBetween(TheFirstOfJune, TheLastOfJune), Is.Empty,
				"la lista de dias SI puede venir vacia: es la FRASE la que no puede ser un cero, "
				+ "y la lista tiene su hermana en palabras al lado (E48).");
		}

		// EL CONTROL QUE DA LA MEDIDA (C61): con una estadia en junio, preguntar por JULIO sigue
		// contestando la ausencia -- y la nombra con los dias de JULIO, no con los de junio. Sin
		// este renglon, una frase que dijera siempre el mismo periodo pasaria la primera.
		[Test]
		public void APeriodWithNoStayNamesItsOwnDaysAndNotAnotherPeriods()
		{
			stays.RecordStay(TheBranch, TheSubject,
				new DateTime(2026, 6, 10, 8, 30, 0), new DateTime(2026, 6, 12, 16, 45, 0),
				new[] { "2026-06-10", "2026-06-11", "2026-06-12" });

			string july = stays.DescribeDaysInTheShopBetween(TheFirstOfJuly, TheLastOfJuly);

			Assert.That(july, Does.Contain(TheFirstOfJuly));
			Assert.That(july, Does.Contain(TheLastOfJuly));
			Assert.That(july, Does.Not.Contain("2026-06-10"),
				"la ausencia de julio no puede nombrar los dias de junio");
			Assert.That(stays.HasAnyStayBetween(TheFirstOfJuly, TheLastOfJuly), Is.False);
			Assert.That(stays.StayCountBetween(TheFirstOfJuly, TheLastOfJuly), Is.EqualTo(0));

			Assert.That(stays.HasAnyStayBetween(TheFirstOfJune, TheLastOfJune), Is.True,
				"y junio si tiene: la ausencia es del periodo que se pregunto, no del registro.");
		}

		// UNA PREGUNTA POR UN INSTANTE SIN ESTADIA TAMBIEN CONTESTA. Es la misma ley en la lectura
		// acotada por instante, que es la otra mitad de la superficie.
		[Test]
		public void AnInstantWithNoStayAnswersInWordsToo()
		{
			string said = stays.DescribeTheStayAt(new DateTime(2026, 6, 11, 12, 0, 0));

			Assert.That(said, Is.Not.Empty);
			Assert.That(said, Does.Contain("2026-06-11 12:00:00"),
				"nombra el instante por el que se pregunto");
			Assert.That(stays.IsInsideTheShopAt(new DateTime(2026, 6, 11, 12, 0, 0)), Is.False);
		}
	}
}
