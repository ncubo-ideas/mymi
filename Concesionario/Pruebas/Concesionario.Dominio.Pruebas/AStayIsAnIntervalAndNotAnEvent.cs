using System;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LEY L1 DEL SLICE. "Una estadia tiene DOS extremos. Un instante suelto no es una estadia, y el
	// concepto que tiene un instante es la completacion de una operacion, que vive en otro lado
	// (§2 del brief)."
	//
	// COMO SE PRUEBA QUE ALGO ES UN INTERVALO Y NO UN EVENTO. No alcanza con leer los dos extremos:
	// un evento con dos campos tambien los tiene. Lo que un evento NO puede contestar es una
	// pregunta hecha en un TERCER instante, uno que no es ninguno de los dos extremos. Por eso el
	// centro de esta prueba es el 11 a mediodia, que no es ni la entrada ni la salida, y por eso
	// C61 pide enumerar los MOMENTOS y no solo los miembros: adentro, antes, despues y los dos
	// extremos son cinco momentos distintos y la respuesta cambia entre ellos.
	//
	// Y LA MITAD ABIERTA ES LA MISMA LEY. Una estadia con entrada y sin salida sigue siendo un
	// intervalo -- con el segundo extremo sin llegar (L2) -- asi que contesta "adentro" en todo
	// instante posterior a la entrada, que es lo que un evento en la entrada no haria.
	[TestFixture]
	public sealed class AStayIsAnIntervalAndNotAnEvent
	{
		private const string TheVin = "1FTFW1ET5DFC10312";
		private const string TheBranch = "Taller Curridabat";
		private const string TheSubject = "Cambio de aceite";

		private static readonly DateTime Entered = new DateTime(2026, 6, 10, 8, 30, 0);
		private static readonly DateTime Collected = new DateTime(2026, 6, 12, 16, 45, 0);
		private static readonly DateTime TheMiddleOfTheStay = new DateTime(2026, 6, 11, 12, 0, 0);
		private static readonly DateTime OneSecondBeforeEntering = new DateTime(2026, 6, 10, 8, 29, 59);
		private static readonly DateTime OneSecondAfterCollecting = new DateTime(2026, 6, 12, 16, 45, 1);

		private static readonly string[] TheDaysItCovered =
			{ "2026-06-10", "2026-06-11", "2026-06-12" };

		private ShopStay stays;

		[SetUp]
		public void Setup()
		{
			stays = new ShopStay(TheVin);
		}

		[Test]
		public void ShopStayWithoutAVinHasNoVehicleToBeAbout()
		{
			Assert.Throws<DomainException>(() => new ShopStay(null));
			Assert.Throws<DomainException>(() => new ShopStay(""));
			Assert.Throws<DomainException>(() => new ShopStay("   "),
				"El agregado es UNO POR VIN. Una identidad en blanco no se puede decir de que "
				+ "vehiculo es.");
		}

		[Test]
		public void TheStaysKeepTheVinTheyWereCreatedWith()
		{
			Assert.That(stays.ReadVin(), Is.EqualTo(TheVin));
		}

		// LA PRUEBA CENTRAL: el tercer instante. Un evento fechado en la entrada no puede contestar
		// que el vehiculo estaba adentro el 11 a mediodia; un intervalo si.
		[Test]
		public void AClosedStayAnswersAtAnInstantThatIsNeitherOfItsTwoEnds()
		{
			ARecordedStay();

			Assert.That(stays.IsInsideTheShopAt(TheMiddleOfTheStay), Is.True,
				"L1: la estadia es un INTERVALO. Un evento en la entrada no tendria nada que "
				+ "contestar en un instante que no es ninguno de sus dos extremos.");
		}

		// Y LOS CINCO MOMENTOS SE DISTINGUEN ENTRE SI (C61). Sin las dos negativas, un
		// IsInsideTheShopAt que contestara siempre true pasaria la de arriba.
		[Test]
		public void TheIntervalIsClosedAtBothEndsAndOpenNowhereElse()
		{
			ARecordedStay();

			Assert.That(stays.IsInsideTheShopAt(Entered), Is.True, "el instante de entrada");
			Assert.That(stays.IsInsideTheShopAt(Collected), Is.True, "el instante de salida");
			Assert.That(stays.IsInsideTheShopAt(OneSecondBeforeEntering), Is.False,
				"un segundo antes de entrar todavia no estaba adentro");
			Assert.That(stays.IsInsideTheShopAt(OneSecondAfterCollecting), Is.False,
				"un segundo despues de salir ya no estaba adentro");
		}

		// LOS DOS EXTREMOS SE DICEN, y se dicen como INSTANTES -- con su hora. Un instante dicho
		// como un dia de calendario suelto seria un dia DERIVADO de un instante, que es lo que L4
		// prohibe: haria falta una zona que este modulo no tiene.
		[Test]
		public void TheSentenceNamesBothEndsAndSaysThemAsInstants()
		{
			ARecordedStay();

			string said = stays.DescribeTheStayAt(TheMiddleOfTheStay);

			Assert.That(said, Does.Contain("2026-06-10 08:30:00"), "la entrada, con su hora");
			Assert.That(said, Does.Contain("2026-06-12 16:45:00"), "la salida, con su hora");
			Assert.That(said, Does.Contain(TheBranch), "y donde estuvo");
		}

		// LA MITAD ABIERTA. Una entrada sin salida no es un evento en la entrada: es un intervalo
		// cuyo segundo extremo no llego, asi que sigue contestando "adentro" mucho despues.
		[Test]
		public void AnEntryWithNoCollectionIsStillAnIntervalAndNotAPointInTime()
		{
			stays.RecordEntry(TheBranch, TheSubject, Entered);

			Assert.That(stays.IsInsideTheShopAt(Entered.AddDays(5)), Is.True,
				"L1 con L2: el segundo extremo es un TODAVIA NO, no un instante que ya paso.");
			Assert.That(stays.IsInsideTheShopAt(OneSecondBeforeEntering), Is.False,
				"y el intervalo sigue empezando donde empezo.");
		}

		// UNA ESTADIA NO ES UNA COMPLETACION DE OPERACION (§2 del brief). El registro no guarda que
		// se le HIZO al carro: el asunto es una linea de texto recibida y nada mas.
		[Test]
		public void NothingInAStaySaysWhatWasDoneToTheCar()
		{
			ARecordedStay();

			string said = stays.DescribeTheStayAt(TheMiddleOfTheStay);

			Assert.That(said, Does.Contain(TheSubject),
				"lo unico que dice por que entro es la linea que llego de la sucursal");
			Assert.That(said, Does.Not.Contain("Completed"),
				"§2: una estadia que cargara lo que se le hizo al carro seria una orden de "
				+ "trabajo, y este modulo no tiene ninguna.");
			Assert.That(said, Does.Not.Contain("FluidChange"));
		}

		private void ARecordedStay()
		{
			stays.RecordStay(TheBranch, TheSubject, Entered, Collected, TheDaysItCovered);
		}
	}
}
