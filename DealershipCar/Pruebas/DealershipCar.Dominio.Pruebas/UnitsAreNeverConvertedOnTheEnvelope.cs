using System;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LEY L4, y tests 7 y 8 de §11 del brief. DECISION D4 DE T2, que es la mas grande de la tarea,
	// y esta prueba es donde vive.
	//
	// EL BRIEF SE CONTRADICE A SI MISMO Y HAY QUE ELEGIR. Su L4 y su test 7 dicen que la mitad de
	// distancia se SUSPENDE y "la respuesta degrada al tiempo"; su test 8 dice que la respuesta ES
	// AnEnvelopeSuspendedByAUnitMismatch "y no una respuesta decidida con la mitad de distancia
	// omitida en silencio". Las dos juntas, leidas literales, no se pueden: una respuesta suspendida
	// tiene el Standing() GUARDADO, asi que no puede ademas contestar "Inside por tiempo".
	//
	// VEREDICTO: SUSPENDE LA RESPUESTA ENTERA, PERO SOLO CUANDO LA MITAD DE DISTANCIA TODAVIA PODIA
	// CAMBIAR EL RESULTADO. La regla no es nueva y no se invento aca: es la que el propio modulo ya
	// escribio en el /// de WarrantyCoverage.TheStandingFor -- "lo que no se puede medir degrada la
	// respuesta solo cuando todavia podia cambiarla" -- y el orden de sus pasos la implementa (la
	// mitad de tiempo se pregunta ANTES del desajuste de unidades). Aplicada al sobre:
	//
	//   la mitad de tiempo YA LLEGO   -> respuesta DECIDIDA, Reached por tiempo, y la mitad de
	//                                    distancia se dice como no medible y CON SU MOTIVO
	//                                    (esto es el "degrada al tiempo" del test 7)
	//   la mitad de tiempo NO LLEGO   -> AnEnvelopeSuspendedByAUnitMismatch
	//                                    (esto es el tipo del test 8)
	//
	// POR QUE NO SUSPENDER SIEMPRE, que es la lectura literal del test 8 sola: L3 exige que las dos
	// mitades se digan SIEMPRE, asi que la frase de la suspension tiene que decir donde esta parada
	// la de tiempo. Con un vehiculo de 41 meses sobre un sobre de 36, esa frase diria "5 meses
	// pasado el limite" mientras Standing() revienta diciendo que no se puede saber -- DOS
	// RESPUESTAS PARA UNA PREGUNTA dentro de un mismo objeto, que es el defecto C63 · 2 metido
	// adentro del dominio.
	//
	// POR QUE NO DEGRADAR SIEMPRE, que es lo que TimeOrDistance hace: devolveria Inside para un
	// vehiculo cuya mitad de distancia nadie pudo medir, y ese es el tercer anti-patron de E47 con
	// nombre y apellido -- el miembro BENIGNO devuelto como si fuera una medicion. DueAssessment
	// puede permitirselo porque NotDue no le promete nada a nadie; Inside si: dice "tu carro tiene
	// garantia".
	[TestFixture]
	public sealed class UnitsAreNeverConvertedOnTheEnvelope
	{
		private const string TheVin = "3VW2K7AJ9FM123456";
		private const int ThirtySixMonths = 36;
		private const int SixtyThousand = 60000;
		private const int TheDeliveryReading = 10;
		private const int AReadingFarPastTheLimitIfItWereConverted = 75000;

		private static readonly DateTime PublishedAt = new DateTime(2023, 12, 1);
		private static readonly DateTime DeliveredAt = new DateTime(2024, 1, 1);
		private static readonly DateTime ObservedAt = new DateTime(2026, 1, 1);
		private static readonly DateTime InsideTheTimeHalf = new DateTime(2026, 1, 1);
		private static readonly DateTime PastTheTimeHalf = new DateTime(2027, 6, 1);

		private VehicleProfile profile;

		[SetUp]
		public void Setup()
		{
			profile = new VehicleProfile(TheVin);
			profile.RecordDelivery(DeliveredAt, TheDeliveryReading, DistanceUnitCode.UnitA);
			profile.PublishWarrantyEnvelope(
				ThirtySixMonths, SixtyThousand, DistanceUnitCode.UnitA, PublishedAt);
		}

		// ===== TEST 8: EL TIPO, CUANDO LA DISTANCIA TODAVIA PODIA DECIDIR ==================

		[Test]
		public void AMismatchInsideTheTimeHalfIsTheNamedSuspensionAndNotASettledAnswer()
		{
			EnvelopeAnswer answer = TheAnswerWith(DistanceUnitCode.UnitB, InsideTheTimeHalf);

			Assert.That(answer.SuspendedByAUnitMismatch(), Is.True);
			Assert.That(answer.CanBeEvaluated(), Is.False,
				"Adentro del plazo, la mitad de distancia todavia podia volver la respuesta "
				+ "Reached. Contestar Inside sin haberla medido es entregar el miembro benigno.");
			Assert.Throws<DomainException>(() => { EnvelopeStanding _ = answer.Standing(); });
		}

		// Y LA FRASE NOMBRA LAS DOS UNIDADES Y DICE QUE NO SE CONVIERTEN. Sin las dos, quien lea
		// sabe que algo no calzo y no sabe que arreglar.
		[Test]
		public void TheSuspendedAnswerNamesBothUnitsAndSaysTheyAreNeverConverted()
		{
			string said = TheSentenceWith(DistanceUnitCode.UnitB, InsideTheTimeHalf);

			Assert.That(said, Does.Contain(DistanceUnit.UnitA.Name));
			Assert.That(said, Does.Contain(DistanceUnit.UnitB.Name));
			Assert.That(said, Does.Contain("never converted"));
		}

		// LA AFIRMACION DE QUE NO SE CONVIRTIO NADA, que es lo que el test 7 del brief pide medir:
		// 74990 UnitB serian ~120000 UnitA, muy pasado el limite de 60000. El vehiculo NO se reporta
		// pasado un limite de distancia que no se pudo evaluar.
		[Test]
		public void TheVehicleIsNotReportedPastADistanceLimitItCannotEvaluate()
		{
			string said = TheSentenceWith(DistanceUnitCode.UnitB, InsideTheTimeHalf);

			Assert.That(said, Does.Not.Contain("past a limit of 60000"),
				"Ley L4: si alguien hubiera convertido, 74990 mi contra un limite de 60000 km "
				+ "saldria como pasado. No se convierte: se deja de comparar.");
			Assert.That(said, Does.Not.Contain("Reached"));
			Assert.That(said, Does.Not.Contain("Inside"),
				"Y tampoco sale el miembro benigno. Ninguno de los tres significa 'no lo se'.");
		}

		// ===== TEST 7: LA DEGRADACION, CUANDO EL TIEMPO YA DECIDIO =========================

		[Test]
		public void AMismatchPastTheTimeHalfDegradesToTimeAndStillAnswers()
		{
			EnvelopeAnswer answer = TheAnswerWith(DistanceUnitCode.UnitB, PastTheTimeHalf);

			Assert.That(answer.CanBeEvaluated(), Is.True,
				"D4: la mitad de tiempo ya llego, asi que la de distancia no puede cambiar la "
				+ "respuesta. Negarse a contestar aqui seria reportar una ausencia curable sobre "
				+ "una pregunta que si tiene respuesta.");
			Assert.That(answer.Standing(), Is.SameAs(EnvelopeStanding.Reached));
			Assert.That(answer.SuspendedByAUnitMismatch(), Is.False,
				"No esta suspendida: se decidio. Que una mitad no se pudiera medir se dice en la "
				+ "frase, no en la polaridad de la respuesta.");
		}

		// Y LA FRASE DICE LAS DOS MITADES IGUAL (L3): la de tiempo con su numero, la de distancia
		// diciendo que no se pudo medir y por que. Esto es lo que hace que "degrada al tiempo" y
		// "no una mitad omitida en silencio" puedan ser las dos ciertas.
		[Test]
		public void TheDegradedAnswerSaysBothHalvesAndWhyOneCouldNotBeMeasured()
		{
			string said = TheSentenceWith(DistanceUnitCode.UnitB, PastTheTimeHalf);

			Assert.That(said, Does.Contain("Reached by time"));
			Assert.That(said, Does.Contain("5 months past a limit of 36 months"));
			Assert.That(said, Does.Contain("Distance: cannot be measured"));
			Assert.That(said, Does.Contain("never converted"));
			Assert.That(said, Does.Not.Contain("past a limit of 60000"));
		}

		// ===== LA UNIDAD DE LA ENTREGA ES LA OTRA MITAD DEL DESAJUSTE ======================

		// La lectura de la ENTREGA tambien tiene unidad, y tambien se compara por identidad contra
		// la del limite. Sin este chequeo, una entrega en mi y un limite en km restarian numeros de
		// dos unidades distintas y nadie veria nada raro -- que es la conversion sin nombre.
		[Test]
		public void ADeliveryReadingInAnotherUnitSuspendsTheDistanceHalfToo()
		{
			VehicleProfile mismatched = new VehicleProfile(TheVin);
			mismatched.RecordDelivery(DeliveredAt, TheDeliveryReading, DistanceUnitCode.UnitB);
			mismatched.PublishWarrantyEnvelope(
				ThirtySixMonths, SixtyThousand, DistanceUnitCode.UnitA, PublishedAt);

			EnvelopeAnswer answer = mismatched.TheStandingAsOf(
				25000, DistanceUnitCode.UnitA, ObservedAt, InsideTheTimeHalf);

			Assert.That(answer.SuspendedByAUnitMismatch(), Is.True);
			Assert.That(answer.Describe(), Does.Contain("recorded at delivery"),
				"La frase dice CUAL de las dos distancias del registro no calzo. Sin eso, la "
				+ "agencia sale a corregir el odometro y el problema estaba en la entrega.");
		}

		// ===== Y LA AUSENCIA DE CONVERSION SIGUE SIENDO ESTRUCTURAL ========================
		//
		// AQUI NO VA UN SEGUNDO BARRIDO, Y ESO ES LA DECISION. §8 del plan exige que los allowlists
		// que sostienen la ausencia de Convert queden intactos, y ya hay dos barridos que lo hacen:
		// TheDistanceUnitIsNeverConverted.DistanceUnitExposesNothingOutsideTheAllowlist tiene la
		// lista blanca, y ThePressureAndVolumeUnitsAreNeverConverted.
		// NoTypeInTheDomainDeclaresAMemberThatConvertsBetweenUnits recorre el ensamblado ENTERO --
		// asi que los tipos nuevos de este slice ya caen dentro de el. Una tercera copia aqui es una
		// que se puede separar de las otras dos, que es la mitad de C57 que casi nadie aplica:
		// escribir un barrido de mas es tan defecto como dejar uno corto.

		private EnvelopeAnswer TheAnswerWith(DistanceUnitCode odometerUnit, DateTime asOf)
		{
			return profile.TheStandingAsOf(
				AReadingFarPastTheLimitIfItWereConverted, odometerUnit, ObservedAt, asOf);
		}

		private string TheSentenceWith(DistanceUnitCode odometerUnit, DateTime asOf)
		{
			return profile.DescribeWarrantyEnvelope(
				AReadingFarPastTheLimitIfItWereConverted, odometerUnit, ObservedAt, asOf);
		}
	}
}
