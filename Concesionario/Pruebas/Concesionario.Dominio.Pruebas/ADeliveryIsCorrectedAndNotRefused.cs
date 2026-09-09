using System;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// DECISION D6 (§7 item 5 del plan, abierta desde que el plan se escribio): un segundo
	// RecordDelivery para un vehiculo es una CORRECCION -- gana la ultima -- y no un defecto de
	// quien llama.
	//
	// EL ARGUMENTO MAS FUERTE ESTA EN EL RAZONAMIENTO DE L8, y no hubo que buscarlo lejos: "todo
	// hecho corregible llega por comando, PORQUE un argumento de siembra queda fijo en la primera
	// apertura y silenciosamente sin arreglo" (medido: S2/S3). Una entrega que reventara en la
	// segunda llamada quedaria tan sin arreglo como si estuviera en la siembra -- y entonces
	// haberla sacado de ahi no compro nada.
	//
	// Y LA SEGUNDA RAZON DECIDE EL MECANISMO, no solo el resultado: C22, una guarda que revienta
	// ADENTRO de un comando deja un asiento de fallo permanente en el journal, que se reproduce en
	// cada arranque futuro y para siempre. Corregir la fecha de entrega de un carro es una operacion
	// normal de una agencia, y E46 dice que una llamada legitima y repetida no se rechaza con una
	// excepcion.
	//
	// OINV-14 NO TRANSFIERE, y es lo que el plan pedia sopesar: su forma "declarado contra
	// verificado" es sobre hechos que declara EL DUENO, y este es un comando de superficie T. La
	// agencia no declara, registra.
	//
	// EL PRECIO, que T1 ya habia medido y no es la decision: Delivery.Of es una FABRICA, asi que
	// "gana la ultima" es un reemplazo de objeto entero y no cuesta nada estructuralmente.
	//
	// Y LA ASIMETRIA QUE SALE DE AQUI, que vale la pena decir porque no es obvia: L10 -- aplicacion
	// hacia adelante -- rige sobre el SOBRE y NO sobre la ENTREGA. Un sobre es una REGLA publicada
	// con un instante desde el que vale, asi que uno fechado antes no desplaza al que rige. Una
	// entrega es UN HECHO HISTORICO -- el dia en que el carro se entrego -- asi que corregirla
	// corrige la historia y no hay contra que ordenarla.
	[TestFixture]
	public sealed class ADeliveryIsCorrectedAndNotRefused
	{
		private const string TheVin = "3VW2K7AJ9FM123456";
		private const int ThirtySixMonths = 36;
		private const int SixtyThousand = 60000;
		private const int TheReadingFirstRecorded = 500;
		private const int TheCorrectedReading = 10;
		private const int AReading = 25000;

		private static readonly DateTime PublishedAt = new DateTime(2023, 12, 1);
		private static readonly DateTime TheDateFirstRecorded = new DateTime(2024, 6, 1);
		private static readonly DateTime TheCorrectedDate = new DateTime(2024, 1, 1);
		private static readonly DateTime ObservedAt = new DateTime(2026, 1, 1);
		private static readonly DateTime TheInstantAsked = new DateTime(2026, 1, 1);

		private VehicleProfile profile;

		[SetUp]
		public void Setup()
		{
			profile = new VehicleProfile(TheVin);
			profile.PublishWarrantyEnvelope(
				ThirtySixMonths, SixtyThousand, DistanceUnitCode.UnitA, PublishedAt);
			profile.RecordDelivery(
				TheDateFirstRecorded, TheReadingFirstRecorded, DistanceUnitCode.UnitA);
		}

		[Test]
		public void ASecondDeliveryIsAcceptedAndNotRefused()
		{
			Assert.DoesNotThrow(() => profile.RecordDelivery(
				TheCorrectedDate, TheCorrectedReading, DistanceUnitCode.UnitA));
		}

		// GANA LA ULTIMA, y las dos mitades del ancla se mueven juntas: es un reemplazo de objeto
		// entero, no un parche campo por campo.
		[Test]
		public void TheLastDeliveryRecordedIsTheOneThatAnswers()
		{
			TheDeliveryIsCorrected();

			Assert.That(profile.ReadDeliveredAt(), Is.EqualTo(TheCorrectedDate));
			Assert.That(profile.ReadDeliveryReading(), Is.EqualTo(TheCorrectedReading));
		}

		// Y LA CORRECCION SE VE EN LAS DOS MITADES DEL SOBRE, que es la prueba de que corrigio el
		// ancla y no solo lo que dice un accesor. Los cuatro numeros son distintos entre si (C60):
		//
		//   antes  -> entrega 2024-06-01 en 500:   19 meses cumplidos, quedan 17; recorridos 24500
		//   despues-> entrega 2024-01-01 en  10:   24 meses cumplidos, quedan 12; recorridos 24990
		[Test]
		public void CorrectingTheDeliveryMovesBothHalvesOfTheAnswer()
		{
			string before = TheSentence();

			TheDeliveryIsCorrected();

			string after = TheSentence();

			Assert.That(before, Does.Contain("17 meses de 36 meses"));
			Assert.That(before, Does.Contain("35500 km de 60000 km"));

			Assert.That(after, Does.Contain("12 meses de 36 meses"));
			Assert.That(after, Does.Contain("35010 km de 60000 km"),
				"Las dos mitades se miden desde el ancla, asi que corregirla las mueve las dos. Si "
				+ "solo se moviera una, algo estaria guardando la otra.");
		}

		// UNA CORRECCION PUEDE MOVER LA RESPUESTA DE ADENTRO A ALCANZADO, que es el caso que hace
		// necesaria la correccion: una fecha de entrega mal puesta un ano tarde deja al vehiculo
		// adentro de un sobre que ya se le vencio.
		[Test]
		public void ACorrectionCanMoveTheStandingItself()
		{
			VehicleProfile misdated = new VehicleProfile(TheVin);
			misdated.PublishWarrantyEnvelope(
				ThirtySixMonths, SixtyThousand, DistanceUnitCode.UnitA, PublishedAt);
			misdated.RecordDelivery(new DateTime(2024, 1, 1), 10, DistanceUnitCode.UnitA);

			Assert.That(TheStandingOf(misdated), Is.EqualTo("Inside"));

			misdated.RecordDelivery(new DateTime(2022, 1, 1), 10, DistanceUnitCode.UnitA);

			Assert.That(TheStandingOf(misdated), Is.EqualTo("Reached"),
				"Entregado en 2022, el sobre de 36 meses ya se alcanzo. Si la primera entrega "
				+ "hubiera quedado fija, el registro seguiria diciendo que el carro tiene garantia.");
		}

		// LO QUE SIGUE SIENDO UN DEFECTO DE QUIEN LLAMA (E32): una lectura negativa. Corregir es
		// normal; una entrega con odometro negativo no es una correccion, es un hecho imposible.
		[Test]
		public void ANegativeDeliveryReadingIsStillACallerBug()
		{
			Assert.Throws<DomainException>(
				() => profile.RecordDelivery(TheCorrectedDate, -1, DistanceUnitCode.UnitA));
		}

		// Y LA CORRECCION RECHAZADA DEJA EL ANCLA INTACTA: las guardas van ANTES de mover nada
		// (E12). Si no, una llamada mal hecha dejaria al perfil sin entrega y sin correccion.
		[Test]
		public void ARefusedCorrectionLeavesTheAnchorUntouched()
		{
			Assert.Throws<DomainException>(
				() => profile.RecordDelivery(TheCorrectedDate, -1, DistanceUnitCode.UnitA));

			Assert.That(profile.HasDeliveryBeenRecorded(), Is.True);
			Assert.That(profile.ReadDeliveredAt(), Is.EqualTo(TheDateFirstRecorded));
			Assert.That(profile.ReadDeliveryReading(), Is.EqualTo(TheReadingFirstRecorded));
		}

		// LA ASIMETRIA, HECHA EJECUTABLE: la entrega se corrige hacia atras y el sobre no. Sin esta
		// prueba, la diferencia entre "un hecho historico" y "una regla con vigencia" queda solo
		// escrita en un plan.
		[Test]
		public void ADeliveryReachesBackwardsAndAPublicationDoesNot()
		{
			profile.RecordDelivery(
				TheDateFirstRecorded.AddYears(-2), TheReadingFirstRecorded, DistanceUnitCode.UnitA);

			Assert.That(profile.ReadDeliveredAt(), Is.EqualTo(TheDateFirstRecorded.AddYears(-2)),
				"La entrega es un hecho historico: corregirla corrige la historia.");

			profile.PublishWarrantyEnvelope(
				12, 20000, DistanceUnitCode.UnitA, PublishedAt.AddYears(-2));

			Assert.That(profile.EnvelopeMonths(), Is.EqualTo(ThirtySixMonths),
				"El sobre es una REGLA con un instante desde el que vale, asi que uno fechado antes "
				+ "no desplaza al que rige.");
		}

		private void TheDeliveryIsCorrected()
		{
			profile.RecordDelivery(
				TheCorrectedDate, TheCorrectedReading, DistanceUnitCode.UnitA);
		}

		private string TheSentence()
		{
			return profile.DescribeWarrantyEnvelope(
				AReading, DistanceUnitCode.UnitA, ObservedAt, TheInstantAsked);
		}

		private static string TheStandingOf(VehicleProfile subject)
		{
			return subject.ReadEnvelopeStanding(
				AReading, DistanceUnitCode.UnitA, ObservedAt, TheInstantAsked);
		}
	}
}
