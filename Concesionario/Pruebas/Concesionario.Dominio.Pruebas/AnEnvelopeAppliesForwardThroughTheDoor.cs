using System;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// PRUEBA 16 DE §11 DEL BRIEF, EN LA PUERTA, y con ella las dos leyes de procedencia del sobre:
	// toda respuesta nombra CUANDO se publico el sobre con que se midio, y un sobre re-publicado
	// rige DESDE SU PUBLICACION HACIA ADELANTE.
	//
	// LA MITAD QUE NECESITA LA LECTURA COMPUESTA. Que el perfil se quede con el ultimo sobre ya lo
	// afirma el nivel A; lo que solo se ve aca es que la frase COMPUESTA -- la que saca el odometro
	// de la agenda y los limites del perfil en un solo script -- se mueva con la republicacion y
	// nombre el instante nuevo. Una lectura que se hubiera quedado con el sobre viejo daria una
	// respuesta bien formada y equivocada, y nada se veria mal.
	[TestFixture]
	public sealed class AnEnvelopeAppliesForwardThroughTheDoor
	{
		private const string TheVin = "1FTFW1ET5DF123456";
		private const string TheUnitEverythingIsIn = "UnitB";

		private const int TheOdometerAtDelivery = 30000;
		private const int TheOdometerToday = 35000;

		private static readonly DateTime DayOne =
			new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime DayOneHundred = DayOne.AddDays(100);

		private static readonly DateTime TheDayItWasHandedOver =
			new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheDayItWasObserved =
			new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheDayItIsAsked =
			new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);

		private AScheduleOnARealJournal journal;

		[SetUp]
		public void Setup()
		{
			journal = new AScheduleOnARealJournal(TheVin, "Standard");

			journal.Api.RecordDelivery(
				TheDayItWasHandedOver, TheOdometerAtDelivery, TheUnitEverythingIsIn);
			journal.Api.RecordReading(
				TheOdometerToday, TheUnitEverythingIsIn, TheDayItWasObserved, "Telemetry");
		}

		[TearDown]
		public void TearDown()
		{
			journal.Dispose();
		}

		// LOS LIMITES NUEVOS RIGEN Y EL INSTANTE NUEVO SE NOMBRA. Y la respuesta que ya se habia dado
		// no cambia: una publicacion no alcanza hacia atras lo que alguien ya leyo.
		[Test]
		public void AfterARepublicationTheAnswerUsesTheNewLimitsAndNamesTheNewInstant()
		{
			journal.Api.PublishWarrantyEnvelope(36, 60000, TheUnitEverythingIsIn, DayOne);

			string beforeTheSecondPublication =
				journal.Api.DescribeWarrantyEnvelope(TheDayItIsAsked, TheLanguage.OfTheRecord);

			Assert.That(beforeTheSecondPublication, Does.Contain("36 meses o 60.000 mi"));
			Assert.That(
				beforeTheSecondPublication, Does.Contain("publicado el 1 de enero de 2025"));

			journal.Api.PublishWarrantyEnvelope(24, 20000, TheUnitEverythingIsIn, DayOneHundred);

			string afterwards = journal.Api.DescribeWarrantyEnvelope(
				TheDayItIsAsked, TheLanguage.OfTheRecord);

			Assert.That(afterwards, Does.Contain("24 meses o 20.000 mi"));
			Assert.That(afterwards, Does.Contain("publicado el 11 de abril de 2025"));
			Assert.That(afterwards, Does.Not.Contain("60.000"),
				"Y EL SOBRE VIEJO NO DEJA NI UN NUMERO. Una respuesta que mezclara los dos seria una "
				+ "medicion contra un limite que ya no rige.");

			Assert.That(beforeTheSecondPublication, Does.Contain("60.000"),
				"La respuesta que ya se habia dado se quedo con el sobre que nombro: una publicacion "
				+ "no alcanza hacia atras.");
			Assert.That(journal.Api.ReadEnvelopePublishedAt(), Is.EqualTo(DayOneHundred));
		}

		// Y LA POSICION SE MUEVE CON ELLA, que es lo que separa "la frase cita otro numero" de "la
		// respuesta se calculo contra otro limite". Recorrio 5000: adentro de 60000 y pasado de 4000.
		[Test]
		public void TheStandingItselfMovesWithTheRepublishedLimits()
		{
			journal.Api.PublishWarrantyEnvelope(36, 60000, TheUnitEverythingIsIn, DayOne);

			Assert.That(journal.Api.ReadEnvelopeStanding(TheDayItIsAsked), Is.EqualTo("Inside"));

			journal.Api.PublishWarrantyEnvelope(36, 4000, TheUnitEverythingIsIn, DayOneHundred);

			Assert.That(journal.Api.ReadEnvelopeStanding(TheDayItIsAsked), Is.EqualTo("Reached"));
			Assert.That(
				journal.Api.DescribeWarrantyEnvelope(TheDayItIsAsked, TheLanguage.OfTheRecord),
				Does.Contain("Alcanzado por distancia, que llegó primero."));
		}

		// Y LA COBERTURA COMPUESTA TAMBIEN, porque el sobre del vehiculo es uno de los siete hechos
		// que saca del registro. Sin este renglon, la ruta de tres globales podria estar leyendo un
		// sobre viejo y las dos pruebas de arriba seguirian verdes.
		[Test]
		public void TheComposedCoverageAnswerMovesWithTheRepublishedEnvelopeToo()
		{
			journal.Api.OpenRevision("R1", DayOne);
			journal.Api.PublishCoveredSystem("R1", "POWERTRAIN", 0, 0, TheUnitEverythingIsIn);
			journal.Api.SealRevision("R1");

			journal.Api.PublishWarrantyEnvelope(36, 60000, TheUnitEverythingIsIn, DayOne);

			Assert.That(
				journal.Api.ReadCoverageStandingForThisVehicle("POWERTRAIN", TheDayItIsAsked),
				Is.EqualTo("Covered"));

			journal.Api.PublishWarrantyEnvelope(36, 4000, TheUnitEverythingIsIn, DayOneHundred);

			Assert.That(
				journal.Api.ReadCoverageStandingForThisVehicle("POWERTRAIN", TheDayItIsAsked),
				Is.EqualTo("OutsideEnvelope"));
			Assert.That(
				journal.Api.DescribeCoverageForThisVehicle(
					"POWERTRAIN", TheDayItIsAsked, TheLanguage.OfTheRecord),
				Does.Contain("4.000 mi"));
		}
	}
}
