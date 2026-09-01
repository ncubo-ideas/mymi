using System;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LEYES L9 Y L10, y test 16 de §11 del brief.
	//
	// L10: "un sobre republicado rige desde SU instante de publicacion hacia adelante. Una respuesta
	// ya dada conserva el sobre que nombro; una publicacion nunca alcanza hacia atras." Es la forma
	// de la L11 de ModelSpecification, y de ahi se toma la lectura de "hacia adelante": el sobre que
	// contesta es el ULTIMO PUBLICADO por instante de publicacion, no el ultimo en llegar.
	//
	// DOS MITADES, y la segunda es la que muerde:
	//
	//   1. republicar cambia lo que contesta una lectura NUEVA, y la respuesta nombra el instante
	//      nuevo (L9);
	//   2. una respuesta YA CAPTURADA sigue diciendo lo que decia -- porque es una PROYECCION del
	//      sobre del que salio, no una ventana al agregado. Es el mismo test que
	//      ARevisionAppliesForward.TheAnswerObjectItselfIsAProjectionAndNotALiveView hace del otro
	//      lado, y por la misma razon: si siguiera al sobre nuevo, lo que alguien recibio el mes
	//      pasado cambiaria retroactivamente.
	//
	// Y LA TERCERA, que es una DECISION de T2 y no del brief: UN SOBRE FECHADO ANTES DEL QUE RIGE NO
	// LO DESPLAZA, y no revienta. Hay dos precedentes opuestos en el modulo y se eligio el que L10
	// nombra: MaintenanceSchedule.AdoptPlanRevision REVIENTA con una revision fechada antes, pero
	// ModelSpecification -- que es de quien L10 toma la forma -- la ACEPTA y simplemente no la deja
	// regir (ItIsLatestByPublicationInstantAndNotTheLastToArrive). Se sigue el segundo, y la razon
	// que decide es C22: una guarda que revienta ADENTRO de un comando deja un asiento de fallo
	// permanente que se reproduce en cada arranque futuro, y un feed que entrega un sobre viejo
	// fuera de orden es una llamada legitima, no un defecto de quien llama (E46).
	//
	// DENTRO DEL MISMO INSTANTE gana el ultimo en llegar, porque el journal trunca a segundos
	// enteros (C13) y no queda nada mas con que desempatar. Es lo mismo que
	// ARevisionAppliesForward.WithinTheSameInstantTheOneThatArrivedLastAnswers ya decide.
	[TestFixture]
	public sealed class AnEnvelopeAppliesForward
	{
		private const string TheVin = "3VW2K7AJ9FM123456";
		private const int TheFirstMonths = 36;
		private const int TheFirstDistance = 60000;
		private const int TheSecondMonths = 60;
		private const int TheSecondDistance = 100000;
		private const int TheDeliveryReading = 10;
		private const int AReading = 25000;

		private static readonly DateTime DeliveredAt = new DateTime(2024, 1, 1);
		private static readonly DateTime TheFirstPublication = new DateTime(2023, 12, 1);
		private static readonly DateTime TheSecondPublication = new DateTime(2024, 4, 10);
		private static readonly DateTime ObservedAt = new DateTime(2026, 1, 1);
		private static readonly DateTime TheInstantAsked = new DateTime(2026, 1, 1);

		private VehicleProfile profile;

		[SetUp]
		public void Setup()
		{
			profile = new VehicleProfile(TheVin);
			profile.RecordDelivery(DeliveredAt, TheDeliveryReading, DistanceUnitCode.UnitA);
			profile.PublishWarrantyEnvelope(
				TheFirstMonths, TheFirstDistance, DistanceUnitCode.UnitA, TheFirstPublication);
		}

		// MITAD 1: la lectura nueva trae los limites nuevos y NOMBRA el instante nuevo (L9).
		[Test]
		public void AnswersAfterTheSecondPublicationUseTheNewLimitsAndNameTheNewInstant()
		{
			ASecondEnvelopeIsPublished();

			string said = TheSentence();

			Assert.That(said, Does.Contain("of 60 months"));
			Assert.That(said, Does.Contain("of 100000 km"));
			Assert.That(said, Does.Contain("published on 2024-04-10"));
			Assert.That(said, Does.Not.Contain("published on 2023-12-01"),
				"El instante viejo no puede seguir apareciendo: la segunda publicacion lo reemplazo.");
		}

		// Y los accesores de la mitad publicada tambien: son el mismo sobre visto sin ningun hecho
		// del vehiculo (L7).
		[Test]
		public void ThePublishedHalfMovesWithTheSecondPublicationToo()
		{
			ASecondEnvelopeIsPublished();

			Assert.That(profile.EnvelopeMonths(), Is.EqualTo(TheSecondMonths));
			Assert.That(profile.EnvelopeDistance(), Is.EqualTo(TheSecondDistance));
			Assert.That(profile.ReadEnvelopePublishedAt(), Is.EqualTo(TheSecondPublication));
			Assert.That(profile.DescribePublishedWarrantyEnvelope(),
				Does.Contain("60 months or 100000 km"));
		}

		// MITAD 2, LA QUE MUERDE: la respuesta capturada ANTES sigue nombrando el primer sobre. Se
		// captura el OBJETO -- no su texto -- y se le vuelve a pedir la frase despues de republicar.
		[Test]
		public void AnAnswerCapturedBeforeTheSecondPublicationKeepsTheFirstEnvelope()
		{
			EnvelopeAnswer captured = profile.TheStandingAsOf(
				AReading, DistanceUnitCode.UnitA, ObservedAt, TheInstantAsked);

			ASecondEnvelopeIsPublished();

			Assert.That(captured.Describe(), Does.Contain("of 36 months"));
			Assert.That(captured.Describe(), Does.Contain("published on 2023-12-01"));
			Assert.That(captured.Describe(), Does.Not.Contain("100000"),
				"L10: una publicacion no alcanza hacia atras. La respuesta se compuso con el sobre "
				+ "que la dio y se quedo con el; una que se re-resuelve cada vez que se lee no es "
				+ "una respuesta, es una consulta.");

			Assert.That(TheSentence(), Does.Contain("of 100000 km"),
				"Y una lectura NUEVA si trae el sobre nuevo: hacia adelante si alcanza.");
		}

		// LA TERCERA MITAD: un sobre fechado ANTES del que rige no lo desplaza, y no revienta.
		[Test]
		public void AnEnvelopePublishedEarlierThanTheOneInForceDoesNotDisplaceIt()
		{
			ASecondEnvelopeIsPublished();

			Assert.DoesNotThrow(() => profile.PublishWarrantyEnvelope(
				12, 20000, DistanceUnitCode.UnitA, new DateTime(2023, 6, 1)),
				"C22: una guarda que revienta adentro de un comando deja un asiento de fallo "
				+ "permanente, y un feed que entrega un sobre viejo fuera de orden es una llamada "
				+ "legitima.");

			Assert.That(profile.EnvelopeMonths(), Is.EqualTo(TheSecondMonths));
			Assert.That(profile.ReadEnvelopePublishedAt(), Is.EqualTo(TheSecondPublication),
				"Rige desde SU instante de publicacion, y ese instante ya paso hace tiempo.");
		}

		// Y DENTRO DEL MISMO INSTANTE gana el ultimo en llegar: el journal trunca a segundos enteros
		// (C13) y no hay nada mas con que desempatar.
		[Test]
		public void WithinTheSameInstantTheOneThatArrivedLastGoverns()
		{
			profile.PublishWarrantyEnvelope(
				TheSecondMonths, TheSecondDistance, DistanceUnitCode.UnitA, TheFirstPublication);

			Assert.That(profile.EnvelopeMonths(), Is.EqualTo(TheSecondMonths));
		}

		// Un sobre mal formado se rechaza ANTES de comparar instantes, asi que una publicacion
		// invalida no puede colarse por venir con fecha nueva -- ni tirar la que rige.
		[Test]
		public void AMalformedEnvelopeIsRefusedAndLeavesTheOneInForceUntouched()
		{
			Assert.Throws<DomainException>(() => profile.PublishWarrantyEnvelope(
				0, TheFirstDistance, DistanceUnitCode.UnitA, TheSecondPublication));
			Assert.Throws<DomainException>(() => profile.PublishWarrantyEnvelope(
				TheFirstMonths, 0, DistanceUnitCode.UnitA, TheSecondPublication));

			Assert.That(profile.EnvelopeMonths(), Is.EqualTo(TheFirstMonths));
			Assert.That(profile.ReadEnvelopePublishedAt(), Is.EqualTo(TheFirstPublication));
		}

		private void ASecondEnvelopeIsPublished()
		{
			profile.PublishWarrantyEnvelope(
				TheSecondMonths, TheSecondDistance, DistanceUnitCode.UnitA, TheSecondPublication);
		}

		private string TheSentence()
		{
			return profile.DescribeWarrantyEnvelope(
				AReading, DistanceUnitCode.UnitA, ObservedAt, TheInstantAsked);
		}
	}
}
