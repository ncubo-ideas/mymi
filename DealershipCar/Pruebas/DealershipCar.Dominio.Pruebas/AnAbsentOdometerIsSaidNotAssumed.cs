using System;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LEY L5, TEST 9 DE §11 DEL BRIEF, y el brief dice que es la prueba QUE MAS IMPORTA DEL SLICE.
	//
	// "Una lectura faltante es una respuesta CON NOMBRE. Nunca cero, nunca la lectura de la entrega
	// haciendo de la de hoy, y nunca 'la ultima que tengamos' sin decir cuando se observo." (Tercer
	// anti-patron de E47; tercero de E48.)
	//
	// EL ESCENARIO ES EXACTAMENTE EL DEL BRIEF: sobre publicado, entrega registrada, y NINGUNA
	// lectura. Los tres numeros que la respuesta NO puede dar son distinguibles a proposito (C60):
	//
	//   cero                          -> recorrido = 0 - 10, imposible; y si se recortara a 0,
	//                                    diria "quedan 60000 km", que es lo mas parecido a una
	//                                    promesa que este modulo puede escribir
	//   la lectura de la entrega (10) -> diria "quedan 60000 km" tambien: cero recorrido bajo garantia
	//   Inside                        -> el miembro benigno, sin haber medido nada
	//
	// Y LA MISMA REGLA D4 SE APLICA AQUI: una lectura ausente tambien deja la mitad de TIEMPO
	// perfectamente medible. Asi que si el plazo ya se cumplio, la respuesta se decide por tiempo y
	// dice que la de distancia no se pudo medir; y si no, se suspende con su nombre. La razon es la
	// misma que en el desajuste de unidades: lo que no se puede medir degrada la respuesta solo
	// cuando todavia podia cambiarla.
	[TestFixture]
	public sealed class AnAbsentOdometerIsSaidNotAssumed
	{
		private const string TheVin = "3VW2K7AJ9FM123456";
		private const int ThirtySixMonths = 36;
		private const int SixtyThousand = 60000;
		private const int TheDeliveryReading = 10;

		private static readonly DateTime PublishedAt = new DateTime(2023, 12, 1);
		private static readonly DateTime DeliveredAt = new DateTime(2024, 1, 1);
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

		[Test]
		public void WithNoReadingAtAllTheAnswerIsTheNamedAbsence()
		{
			EnvelopeAnswer answer = profile.TheStandingWithNoOdometerAsOf(InsideTheTimeHalf);

			Assert.That(answer.SuspendedByAnAbsentReading(), Is.True);
			Assert.That(answer.CanBeEvaluated(), Is.False);
			Assert.Throws<DomainException>(() => { EnvelopeStanding _ = answer.Standing(); });
		}

		// LOS TRES NUMEROS QUE NO APARECEN. Es la mitad de la ley que se rompe sola: cualquiera de
		// los tres produce una frase bien formada, con procedencia real, que afirma como registrado
		// algo que nadie registro -- y nada revienta ni se loguea.
		[Test]
		public void TheAnswerGivesNoNumberInPlaceOfTheMissingReading()
		{
			string said = profile.DescribeWarrantyEnvelopeWithNoOdometer(InsideTheTimeHalf);

			Assert.That(said, Does.Not.Contain("quedan 60000 km"),
				"Ley L5: ni cero ni la lectura de la entrega. Las dos darian 60000 km de sobra, que "
				+ "es lo mas parecido a una promesa que este modulo puede escribir.");
			Assert.That(said, Does.Not.Contain("Inside"),
				"Y tampoco el miembro benigno: ninguno de los tres significa 'no lo se'.");
			Assert.That(said, Does.Contain("ninguna lectura del odómetro"));
		}

		// LA MITAD DE TIEMPO SE SIGUE DICIENDO (L3): que falte el odometro no borra el plazo, que no
		// depende de ninguna lectura.
		[Test]
		public void TheTimeHalfIsStillSaidBecauseItNeedsNoReading()
		{
			string said = profile.DescribeWarrantyEnvelopeWithNoOdometer(InsideTheTimeHalf);

			Assert.That(said, Does.Contain("12 meses de 36 meses"));
			Assert.That(said, Does.Contain("Distancia: no se puede medir"));
		}

		// Y D4 DEL OTRO LADO: cumplido el plazo, la respuesta se DECIDE por tiempo aunque no haya
		// odometro. Negarse aqui seria reportar como incognita algo que el registro sabe.
		[Test]
		public void PastTheTimeHalfTheAnswerIsDecidedEvenWithNoReading()
		{
			EnvelopeAnswer answer = profile.TheStandingWithNoOdometerAsOf(PastTheTimeHalf);

			Assert.That(answer.CanBeEvaluated(), Is.True);
			Assert.That(answer.Standing(), Is.SameAs(EnvelopeStanding.Reached));
			Assert.That(answer.SuspendedByAnAbsentReading(), Is.False);
			Assert.That(answer.Describe(), Does.Contain("Alcanzado por tiempo"));
			Assert.That(answer.Describe(), Does.Contain("Distancia: no se puede medir"));
		}

		// LA LECTURA GUARDADA DEL STANDING REVIENTA para la suspension, y su mensaje manda a
		// preguntar antes. Es el test 12 de §11 visto desde la puerta que T5 va a exponer.
		[Test]
		public void TheGuardedStandingReadRefusesAndSaysWhatToAskFirst()
		{
			Assert.That(profile.CanTheEnvelopeBeEvaluatedWithNoOdometer(InsideTheTimeHalf),
				Is.False);

			DomainException error = Assert.Throws<DomainException>(
				() => profile.ReadEnvelopeStandingWithNoOdometer(InsideTheTimeHalf));

			Assert.That(error.Message, Does.Contain("evaluar"));
		}

		// Y LA LECTURA TOTAL SIGUE CONTESTANDO. La frase es la lectura PRIMARIA (§9): lo que no se
		// pudo evaluar tambien se dice con palabras, o la vista lo pinta en blanco.
		[Test]
		public void TheTotalReadStillAnswersWithWords()
		{
			Assert.DoesNotThrow(
				() => { string _ = profile.DescribeWarrantyEnvelopeWithNoOdometer(InsideTheTimeHalf); });
			Assert.That(profile.DescribeWarrantyEnvelopeWithNoOdometer(InsideTheTimeHalf),
				Is.Not.Empty);
		}
	}
}
