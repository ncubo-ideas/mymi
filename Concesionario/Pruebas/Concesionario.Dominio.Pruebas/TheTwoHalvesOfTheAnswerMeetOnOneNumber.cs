using System;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LA PRIMERA VEZ QUE LOS DOS AGREGADOS SE TOCAN, y todavia en tier A: los dos objetos se
	// construyen directo, sin journal y sin puerta. Es lo que T6 va a reproducir con UN SOLO
	// script del DSL, y G1 ya midio que un script puede pasar la lectura de un global como
	// argumento al metodo de otro.
	//
	// LO QUE SE PRUEBA ES QUE LAS DOS MITADES DEL PRODUCTOR ENCAJAN: el sobre esta en el perfil y
	// el odometro en la agenda, y la frase sale de componerlos. Los cuatro argumentos del
	// compositor salen de tres lecturas de la agenda mas el instante preguntado, y ninguno sale de
	// ningun otro lado -- que es la ley L1 dicha como forma de la llamada.
	//
	// TODO EL ESCENARIO ESTA EN UnitB A PROPOSITO (C60): con UnitA, que es el primer miembro del
	// enum, un codigo defaulteado por el motor o por un mapeo mal escrito daria exactamente la
	// misma respuesta. En UnitB, un codigo equivocado convierte la respuesta decidida en una
	// suspension por desajuste de unidades, que se ve distinto a simple vista.
	//
	// Y LA RAMA DE LA AUSENCIA es la que T6 va a ramificar: la pregunta de presencia de la agenda
	// es lo que elige entre los DOS compositores del perfil, y por eso tiene que ser total.
	[TestFixture]
	public sealed class TheTwoHalvesOfTheAnswerMeetOnOneNumber
	{
		private const string TheVin = "3VW2K7AJ9FM123456";
		private const int ThirtySixMonths = 36;
		private const int SixtyThousand = 60000;
		private const int TheDeliveryReading = 10;
		private const int TheOdometerToday = 25010;

		private static readonly DateTime PublishedAt = new DateTime(2023, 12, 1);
		private static readonly DateTime DeliveredAt = new DateTime(2024, 1, 1);
		private static readonly DateTime ObservedAt = new DateTime(2025, 11, 20);
		private static readonly DateTime TheInstantAsked = new DateTime(2026, 1, 1);

		private VehicleProfile profile;
		private MaintenanceSchedule schedule;

		[SetUp]
		public void Setup()
		{
			profile = new VehicleProfile(TheVin);
			profile.RecordDelivery(DeliveredAt, TheDeliveryReading, DistanceUnitCode.UnitB);
			profile.PublishWarrantyEnvelope(
				ThirtySixMonths, SixtyThousand, DistanceUnitCode.UnitB, PublishedAt);

			schedule = new MaintenanceSchedule(TheVin, UsageProfileCode.Standard);
		}

		[Test]
		public void BothAggregatesAreAboutTheSameVehicle()
		{
			Assert.That(schedule.ReadVin(), Is.EqualTo(profile.ReadVin()),
				"Los dos globales se llavean por el mismo Vin. Componer dos carros distintos "
				+ "daria una frase perfectamente formada sobre nadie.");
		}

		[Test]
		public void TheProfileComposesTheAnswerFromTheSchedulesOwnReads()
		{
			TheFeedBringsAReadingIn(DistanceUnitCode.UnitB);

			Assert.That(schedule.HasAnOdometerReading(TheInstantAsked), Is.True);

			string said = TheSentenceTheDoorWouldCompose.TheWarrantyEnvelope(
				profile, schedule.ReadOdometer(TheInstantAsked),
				schedule.OdometerUnit(TheInstantAsked),
				schedule.ReadOdometerObservedAt(TheInstantAsked),
				TheInstantAsked, TheLanguage.OfTheRecord);

			Assert.That(said, Does.Contain("Tiempo: quedan 12 meses de 36 meses."));
			Assert.That(said, Does.Contain("Distancia: quedan 35.000 mi de 60.000 mi."),
				"35000 = 60000 - (25010 - 10). El numero que la agenda entrego es el que el sobre "
				+ "resto de su limite: un hecho, un numero, dos agregados.");
			Assert.That(said, Does.Contain("observado el 20 de noviembre de 2025"),
				"Y la frase NOMBRA cuando se observo la lectura contra la que se midio, que es la "
				+ "linea de honestidad de este slice mientras la frescura no exista.");
		}

		[Test]
		public void TheStandingIsDecidedFromThatSameNumber()
		{
			TheFeedBringsAReadingIn(DistanceUnitCode.UnitB);

			Assert.That(
				profile.CanTheEnvelopeBeEvaluated(
					schedule.ReadOdometer(TheInstantAsked),
					schedule.OdometerUnit(TheInstantAsked),
					schedule.ReadOdometerObservedAt(TheInstantAsked),
					TheInstantAsked),
				Is.True);

			Assert.That(
				profile.ReadEnvelopeStanding(
					schedule.ReadOdometer(TheInstantAsked),
					schedule.OdometerUnit(TheInstantAsked),
					schedule.ReadOdometerObservedAt(TheInstantAsked),
					TheInstantAsked),
				Is.EqualTo("Inside"));
		}

		// EL CODIGO DE LA UNIDAD CRUZA DE VERDAD Y DE VERDAD SE COMPARA. La misma agenda, la misma
		// distancia, la otra unidad: la respuesta deja de estar decidida y se suspende con nombre.
		// Si la agenda entregara siempre el primer miembro del enum, este caso daria "Inside".
		[Test]
		public void AReadingInTheOtherUnitSuspendsTheAnswerInsteadOfBeingConverted()
		{
			TheFeedBringsAReadingIn(DistanceUnitCode.UnitA);

			EnvelopeAnswer answer = profile.TheStandingAsOf(
				schedule.ReadOdometer(TheInstantAsked),
				schedule.OdometerUnit(TheInstantAsked),
				schedule.ReadOdometerObservedAt(TheInstantAsked),
				TheInstantAsked);

			Assert.That(answer.SuspendedByAUnitMismatch(), Is.True,
				"El sobre esta en mi y el odometro en km. Que la respuesta cambie es la prueba de "
				+ "que el codigo que la agenda entrego es el de SU lectura y no una constante.");
			Assert.That(answer.CanBeEvaluated(), Is.False);
		}

		// LA RAMA QUE T6 VA A RAMIFICAR: sin lectura usable, la pregunta de presencia contesta que
		// no y el compositor que se usa es el otro. Los dos son operaciones distintas y no una con
		// un valor especial adentro: una lectura por defecto seria una medicion que nadie tomo.
		[Test]
		public void WithNoReadingThePresenceQuestionChoosesTheOtherComposer()
		{
			Assert.That(schedule.HasAnOdometerReading(TheInstantAsked), Is.False);

			string said = TheSentenceTheDoorWouldCompose.TheWarrantyEnvelopeWithNoOdometer(
				profile, TheInstantAsked, TheLanguage.OfTheRecord);

			Assert.That(said, Does.Contain("Tiempo: quedan 12 meses de 36 meses."),
				"La mitad de tiempo se sigue diciendo: no depende de ninguna lectura.");
			Assert.That(said, Does.Contain("ninguna lectura del odómetro"));
			Assert.That(said, Does.Not.Contain("quedan 60000 mi"),
				"Y ningun numero ocupa el lugar de la lectura que falta.");
		}

		private void TheFeedBringsAReadingIn(DistanceUnitCode unit)
		{
			schedule.RecordReading(
				TheOdometerToday, unit, ObservedAt, ReadingSourceCode.Telemetry);
		}
	}
}
