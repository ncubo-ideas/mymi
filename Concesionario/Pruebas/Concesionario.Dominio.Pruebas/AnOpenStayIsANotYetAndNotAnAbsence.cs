using System;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LEY L2 DEL SLICE, tests 1 y 3 de §11 del brief. "Que el carro este adentro AHORA es un ESTADO
	// CON TIPO, nunca una salida nula ni una fecha centinela. Toda lectura dice cual de los dos esta
	// mirando."
	//
	// EL TIPO ES EL ESTADO (E29, E36). No hay un campo de estado ni un booleano guardado: la estadia
	// abierta y la cerrada son DOS TIPOS, y cerrar devuelve otro objeto. Por eso las dos mitades que
	// solo la cerrada puede contestar -- la salida y los dias -- son accesores GUARDADOS en la base y
	// revientan nombrando la pregunta que hay que hacer primero (E41, E45).
	//
	// LA MITAD QUE MAS FACIL SE PIERDE: una estadia abierta NO ES UNA AUSENCIA. El vehiculo esta
	// adentro, y una lectura que contestara "no hay estadia" mientras el carro esta en el taller
	// seria la tercera antipatron de E47 -- una respuesta benigna que se lee como un hecho.
	//
	// Y AQUI MUERDE G1 POR TERCERA VEZ, asi que se afirma en vez de dejarse implicito: una estadia
	// abierta NO TIENE DIAS todavia -- los dias llegan con la salida (L4) -- y decidir si su
	// instante de entrada cae dentro de un periodo dicho en DIAS necesitaria la zona que este modulo
	// se niega a inventar. Entonces las lecturas acotadas por DIAS no la ven, y las acotadas por
	// INSTANTE si. Las dos frases juntas son verdad; ninguna sola lo seria.
	[TestFixture]
	public sealed class AnOpenStayIsANotYetAndNotAnAbsence
	{
		private const string TheVin = "1FTFW1ET5DFC10312";
		private const string TheBranch = "Taller Curridabat";
		private const string TheSubject = "Diagnostico de ruido";

		private const string TheFirstOfJune = "2026-06-01";
		private const string TheLastOfJune = "2026-06-30";

		private static readonly DateTime Entered = new DateTime(2026, 6, 10, 8, 30, 0);
		private static readonly DateTime StillInside = new DateTime(2026, 6, 11, 12, 0, 0);

		private ShopStay stays;

		[SetUp]
		public void Setup()
		{
			stays = new ShopStay(TheVin);
			stays.RecordEntry(TheBranch, TheSubject, Entered);
		}

		[Test]
		public void TheVehicleIsInsideAndTheOpenStaySaysSo()
		{
			Assert.That(stays.IsInsideTheShopAt(StillInside), Is.True);
			Assert.That(TheSentenceTheDoorWouldCompose.TheStay(stays, StillInside,
				TheLanguage.OfTheRecord), Does.Contain(TheBranch));
		}

		// NINGUNA SALIDA SE INVENTA. Ni una fecha centinela, ni el minimo, ni el maximo: la frase
		// dice con palabras que la estadia sigue abierta.
		[Test]
		public void NoExitIsInventedAndNoSentinelDateAppears()
		{
			string said = TheSentenceTheDoorWouldCompose.TheStay(stays, StillInside,
				TheLanguage.OfTheRecord);

			Assert.That(said, Does.Contain("sigue abierta"),
				"L2: el todavia-no se dice con palabras.");
			Assert.That(said, Does.Not.Contain("0001"),
				"una fecha centinela seria una salida que nadie registro");
			Assert.That(said, Does.Not.Contain("9999"));
			Assert.That(said, Does.Not.Contain("2026-06-11"),
				"y no se inventa una salida en el instante en que se pregunta");
		}

		// LAS DOS MITADES GUARDADAS, contra el objeto directo. Cada una nombra la pregunta que hay
		// que hacer primero (E41): contestar cualquier cosa aqui seria inventar la salida.
		[Test]
		public void TheTwoHalvesOnlyTheClosedOneAnswersRefuseInsteadOfInventing()
		{
			AnOpenStay open = new AnOpenStay(TheBranch, new StaySubject(TheSubject), Entered);

			Assert.That(open.IsOpen(), Is.True);

			DomainException noDays = Assert.Throws<DomainException>(() => open.TheDaysItCovered());
			Assert.That(noDays.Message, Does.Contain("sigue abierta"));
			Assert.That(noDays.Message, Does.Contain("preguntar"),
				"E41: el mensaje nombra la pregunta que hay que hacer antes.");

			DomainException noExit = Assert.Throws<DomainException>(() =>
			{
				DateTime unused = open.CollectedAt;
			});
			Assert.That(noExit.Message, Does.Contain("sigue abierta"));
		}

		// L2 CONTRA UNA CERRADA, que es la mitad que dice que las dos son estados distintos y no un
		// booleano con dos nombres.
		[Test]
		public void AClosedStayAnswersBothHalvesAndSaysItIsNotOpen()
		{
			stays.RecordCollection(new DateTime(2026, 6, 12, 16, 45, 0), new[] { "2026-06-10", "2026-06-11", "2026-06-12" });

			Assert.That(stays.IsInsideTheShopAt(new DateTime(2026, 6, 13)), Is.False);
			Assert.That(stays.DaysInTheShopCountBetween(TheFirstOfJune, TheLastOfJune), Is.EqualTo(3),
				"test 3 de §11: cerrada, los dias YA se pueden contestar.");
		}

		// G1 POR TERCERA VEZ, y las dos frases se afirman JUNTAS a proposito. La acotada por dias no
		// ve la estadia abierta -- no tiene dias, y derivarlos del instante pedia una zona -- y la
		// acotada por instante SI la ve. Lo que la primera NO dice es que el vehiculo no estuvo
		// adentro: dice que no hay dias REGISTRADOS en ese periodo.
		[Test]
		public void ADayBoundedPeriodDoesNotSeeAnOpenStayAndDoesNotClaimTheVehicleWasNeverInside()
		{
			Assert.That(stays.HasAnyStayBetween(TheFirstOfJune, TheLastOfJune), Is.False,
				"L4 y G1: los dias llegan con la salida, asi que una abierta no tiene ninguno.");
			Assert.That(stays.DaysInTheShopCountBetween(TheFirstOfJune, TheLastOfJune), Is.EqualTo(0));

			string byDays = TheSentenceTheDoorWouldCompose.TheStayPeriod(
				stays, TheFirstOfJune, TheLastOfJune, TheLanguage.OfTheRecord);

			Assert.That(byDays, Does.Contain("días registrados"),
				"E47 antipatron 3: la frase dice de que habla -- de dias REGISTRADOS -- para no "
				+ "leerse como que el carro nunca estuvo adentro.");

			// LOS DOS LIMITES SON DIAS: el registro los lleva en yyyy-MM-dd y la frase los dice en
			// palabras, asi que lo que se espera se compone con el mismo renderizador.
			Assert.That(byDays, Does.Contain(ADay(TheFirstOfJune)));
			Assert.That(byDays, Does.Contain(ADay(TheLastOfJune)));

			Assert.That(stays.IsInsideTheShopAt(StillInside), Is.True,
				"y la lectura acotada por INSTANTE si la ve. Las dos frases son verdad.");
		}

		private static string ADay(string yyyyMMdd)
		{
			return TheSentenceTheDoorWouldCompose.ADayAsTheSentenceSaysIt(
				yyyyMMdd, TheLanguage.OfTheRecord);
		}
	}
}
