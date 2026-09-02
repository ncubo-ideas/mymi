using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// TESTS 10, 11 Y 12 DE §11 DEL BRIEF, y la ley E47 vista desde el COMPOSITOR en vez de desde
	// los tipos: T1 probo que las tres suspensiones existen y se dicen distinto;
	// esto prueba que el perfil PRODUCE la correcta en cada caso.
	//
	// LAS CUATRO SITUACIONES, cada una con su respuesta y ninguna confundible con otra:
	//
	//   sin sobre                         -> DECIDIDA, EnvelopeStanding.NotPublished        (D1)
	//   sobre pero sin entrega            -> AnEnvelopeSuspendedByAnAbsentDelivery
	//   sobre y entrega pero sin lectura  -> AnEnvelopeSuspendedByAnAbsentReading
	//   los tres presentes, unidades mal  -> AnEnvelopeSuspendedByAUnitMismatch
	//
	// EL ORDEN DE LAS GUARDAS ES LA LEY y no es prolijidad (T1, departure 2): el sobre se pregunta
	// antes que la entrega, la entrega antes que la lectura, y las unidades al final. Delivery.
	// IsExpressedIn y WarrantyEnvelope.MeasuresDistanceIn REVIENTAN sobre la ausencia en vez de
	// contestar que no, precisamente para que un sobre inexistente no pueda salir reportado como un
	// desajuste de unidades. Las pruebas de abajo recorren el orden al reves -- quitando un hecho
	// por vez -- que es como se ve si el orden se rompio.
	//
	// Y NINGUN LLAMADOR RAMIFICA POR EL TIPO CONCRETO: las cuatro se distinguen preguntando a la
	// BASE, sin un 'is', sin cast y sin switch. Eso lo afirma la ultima prueba, por reflexion sobre
	// los predicados, no leyendo el compositor.
	[TestFixture]
	public sealed class ThreeAbsencesAreThreeDifferentAnswers
	{
		private const string TheVin = "3VW2K7AJ9FM123456";
		private const int ThirtySixMonths = 36;
		private const int SixtyThousand = 60000;
		private const int TheDeliveryReading = 10;
		private const int AReading = 25000;

		private static readonly DateTime PublishedAt = new DateTime(2023, 12, 1);
		private static readonly DateTime DeliveredAt = new DateTime(2024, 1, 1);
		private static readonly DateTime ObservedAt = new DateTime(2026, 1, 1);
		private static readonly DateTime InsideTheTimeHalf = new DateTime(2026, 1, 1);

		// TEST 10: sin sobre -> DECIDIDA con NotPublished, que es una respuesta DISTINTA de las tres
		// suspensiones y no una cuarta suspension (D1).
		[Test]
		public void WithNoEnvelopeTheAnswerIsSettledAsNotPublished()
		{
			EnvelopeAnswer answer = NoEnvelope().TheStandingAsOf(
				AReading, DistanceUnitCode.UnitA, ObservedAt, InsideTheTimeHalf);

			Assert.That(answer.CanBeEvaluated(), Is.True);
			Assert.That(answer.Standing(), Is.SameAs(EnvelopeStanding.NotPublished));
			Assert.That(NumberOfReasonsDeclaredBy(answer), Is.EqualTo(0),
				"No esta suspendida por nada: se decidio. Lo que se decidio es que nadie publico.");
		}

		// TEST 11: sobre sin entrega -> el ancla ausente, con su nombre.
		[Test]
		public void WithNoDeliveryTheAnswerIsTheAbsentAnchor()
		{
			EnvelopeAnswer answer = NoDeliveryRecorded().TheStandingAsOf(
				AReading, DistanceUnitCode.UnitA, ObservedAt, InsideTheTimeHalf);

			Assert.That(answer.SuspendedByAnAbsentDelivery(), Is.True);
			Assert.That(NumberOfReasonsDeclaredBy(answer), Is.EqualTo(1),
				"Un motivo y solo uno. Dos a la vez es un motivo inventado.");
		}

		// Y con la entrega ausente NO sale el desajuste de unidades, aunque la lectura venga en la
		// otra unidad. Es la prueba de que el orden de las guardas se respeta: la unidad de una
		// entrega que no existe no se puede ni preguntar.
		[Test]
		public void AnAbsentDeliveryIsNeverReportedAsAUnitMismatch()
		{
			EnvelopeAnswer answer = NoDeliveryRecorded().TheStandingAsOf(
				AReading, DistanceUnitCode.UnitB, ObservedAt, InsideTheTimeHalf);

			Assert.That(answer.SuspendedByAnAbsentDelivery(), Is.True);
			Assert.That(answer.SuspendedByAUnitMismatch(), Is.False,
				"T1, departure 2: por esto Delivery.IsExpressedIn revienta en vez de contestar que "
				+ "no. Un 'no esta en km' benigno mandaria a nombrar la ausencia equivocada.");
		}

		// Y sin sobre tampoco: sin nada publicado no hay unidad de limite contra la que desajustar.
		[Test]
		public void AnUnpublishedEnvelopeIsNeverReportedAsAUnitMismatchEither()
		{
			EnvelopeAnswer answer = NoEnvelope().TheStandingAsOf(
				AReading, DistanceUnitCode.UnitB, ObservedAt, InsideTheTimeHalf);

			Assert.That(answer.Standing(), Is.SameAs(EnvelopeStanding.NotPublished));
			Assert.That(answer.SuspendedByAUnitMismatch(), Is.False);
		}

		// TEST 12: LAS TRES SUSPENSIONES NIEGAN EL STANDING, y ninguno de los tres miembros sale
		// nunca para una pregunta inevaluable.
		[Test]
		public void NoneOfTheThreeStandingsIsEverReturnedForAnUnevaluableQuestion()
		{
			foreach (EnvelopeAnswer suspended in LasTresSuspensionesQueElPerfilCompone())
			{
				Assert.That(suspended.CanBeEvaluated(), Is.False, suspended.GetType().Name);
				Assert.Throws<DomainException>(() => { EnvelopeStanding _ = suspended.Standing(); },
					suspended.GetType().Name);
			}
		}

		// LAS TRES SON TRES RESPUESTAS DISTINTAS: tres tipos, tres motivos, tres frases. Dos que se
		// dijeran igual serian un solo tipo con dos nombres, y quien lea no sabria cual de los tres
		// arreglos le toca.
		[Test]
		public void TheThreeAbsencesAreThreeTypesThreeReasonsAndThreeSentences()
		{
			List<EnvelopeAnswer> tres = LasTresSuspensionesQueElPerfilCompone();

			Assert.That(tres.Select(answer => answer.GetType()).Distinct().Count(), Is.EqualTo(3));
			Assert.That(tres.Select(answer => answer.Describe()).Distinct().Count(), Is.EqualTo(3));

			foreach (EnvelopeAnswer answer in tres)
				Assert.That(NumberOfReasonsDeclaredBy(answer), Is.EqualTo(1),
					answer.GetType().Name);
		}

		// Y CADA UNA DICE QUE ARREGLARLA A QUIEN: es lo que separa tres tipos de un solo tipo con
		// un codigo de motivo. La entrega la registra la agencia, la lectura la trae el feed, y las
		// unidades NO LAS ARREGLA NADIE CONVIRTIENDO (L4).
		[Test]
		public void EachAbsenceAsksForDifferentWork()
		{
			Assert.That(TheAbsentDelivery().Describe(), Does.Contain("ninguna entrega"));
			Assert.That(TheAbsentReading().Describe(), Does.Contain("ninguna lectura del odómetro"));
			Assert.That(TheUnitMismatch().Describe(), Does.Contain("nunca se convierten"));
		}

		// NINGUN LLAMADOR RAMIFICA POR EL TIPO CONCRETO, y esto lo hace ejecutable: cada una de las
		// cuatro respuestas del perfil se distingue de todas las demas usando SOLO los predicados
		// totales de la base. Si dos casos dieran el mismo vector de respuestas, distinguirlos
		// exigiria preguntar el tipo -- que es exactamente lo que la base existe para evitar.
		[Test]
		public void TheFourCasesAreToldApartWithoutAskingAnyoneItsConcreteType()
		{
			List<string> vectores = LasCuatroRespuestasDelPerfil()
				.Select(TheVectorOfTotalAnswers)
				.ToList();

			Assert.That(vectores.Distinct().Count(), Is.EqualTo(vectores.Count),
				"Dos casos contestan lo mismo a todas las preguntas totales: "
				+ string.Join(" | ", vectores));
		}

		private static string TheVectorOfTotalAnswers(EnvelopeAnswer answer)
		{
			return answer.CanBeEvaluated() + "/"
				+ answer.SuspendedByAnAbsentDelivery() + "/"
				+ answer.SuspendedByAnAbsentReading() + "/"
				+ answer.SuspendedByAUnitMismatch();
		}

		private static int NumberOfReasonsDeclaredBy(EnvelopeAnswer answer)
		{
			return typeof(EnvelopeAnswer)
				.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
					| BindingFlags.DeclaredOnly)
				.Where(metodo => metodo.Name.StartsWith("SuspendedBy", StringComparison.Ordinal))
				.Where(metodo => metodo.ReturnType == typeof(bool)
					&& metodo.GetParameters().Length == 0)
				.Count(metodo => (bool)metodo.Invoke(answer, null));
		}

		private static List<EnvelopeAnswer> LasCuatroRespuestasDelPerfil()
		{
			return new List<EnvelopeAnswer>
			{
				NoEnvelope().TheStandingAsOf(
					AReading, DistanceUnitCode.UnitA, ObservedAt, InsideTheTimeHalf),
				TheAbsentDelivery(),
				TheAbsentReading(),
				TheUnitMismatch(),
			};
		}

		private static List<EnvelopeAnswer> LasTresSuspensionesQueElPerfilCompone()
		{
			return new List<EnvelopeAnswer>
			{
				TheAbsentDelivery(),
				TheAbsentReading(),
				TheUnitMismatch(),
			};
		}

		private static EnvelopeAnswer TheAbsentDelivery()
		{
			return NoDeliveryRecorded().TheStandingAsOf(
				AReading, DistanceUnitCode.UnitA, ObservedAt, InsideTheTimeHalf);
		}

		private static EnvelopeAnswer TheAbsentReading()
		{
			return Everything().TheStandingWithNoOdometerAsOf(InsideTheTimeHalf);
		}

		private static EnvelopeAnswer TheUnitMismatch()
		{
			return Everything().TheStandingAsOf(
				AReading, DistanceUnitCode.UnitB, ObservedAt, InsideTheTimeHalf);
		}

		private static VehicleProfile NoEnvelope()
		{
			VehicleProfile profile = new VehicleProfile(TheVin);
			profile.RecordDelivery(DeliveredAt, TheDeliveryReading, DistanceUnitCode.UnitA);

			return profile;
		}

		private static VehicleProfile NoDeliveryRecorded()
		{
			VehicleProfile profile = new VehicleProfile(TheVin);
			profile.PublishWarrantyEnvelope(
				ThirtySixMonths, SixtyThousand, DistanceUnitCode.UnitA, PublishedAt);

			return profile;
		}

		private static VehicleProfile Everything()
		{
			VehicleProfile profile = NoDeliveryRecorded();
			profile.RecordDelivery(DeliveredAt, TheDeliveryReading, DistanceUnitCode.UnitA);

			return profile;
		}
	}
}
