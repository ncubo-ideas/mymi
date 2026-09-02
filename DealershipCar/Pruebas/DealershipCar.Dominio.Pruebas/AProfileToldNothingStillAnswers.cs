using System;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// TEST 1 DE §11 DEL BRIEF, y las leyes L1 y L8.
	//
	// L8: "el constructor toma el Vin y nada mas. Todo hecho corregible llega por comando, porque un
	// argumento de siembra queda fijo en la primera apertura y silenciosamente sin arreglo"
	// (medido: SPIKES-ModelSpecification S3, re-medido como S2 de este slice).
	//
	// LO QUE ESTA PRUEBA DEFIENDE ES QUE UN PERFIL RECIEN CREADO CONTESTA. Las dos preguntas de
	// presencia son TOTALES (E41): un vehiculo del que la agencia todavia no dijo nada no es un
	// error, es el estado normal del primer dia. Si alguna de las dos reventara, cada llamador
	// tendria que envolverla en un try, y ese try es el "!= null" que E45 saca del dominio.
	//
	// Y LOS ACCESORES SI REVIENTAN, que es la otra mitad del par de E41: no hay instante de
	// publicacion de un sobre que nadie publico, y un DateTime.MinValue devuelto aqui se parece a
	// un instante y no lo es.
	//
	// LA CUENTA DEL CONSTRUCTOR (L8) se afirma por reflexion y no leyendo el codigo: el dia que
	// alguien le agregue el sobre o la entrega como argumento de siembra, esto se pone rojo. Es la
	// unica forma de defender una AUSENCIA de parametro.
	[TestFixture]
	public sealed class AProfileToldNothingStillAnswers
	{
		private const string TheVin = "3VW2K7AJ9FM123456";

		private static readonly DateTime TheInstantAsked = new DateTime(2026, 1, 1);

		private VehicleProfile profile;

		[SetUp]
		public void Setup()
		{
			profile = new VehicleProfile(TheVin);
		}

		// ===== LAS DOS PREGUNTAS DE PRESENCIA SON TOTALES ==================================

		[Test]
		public void NeitherPresenceReadThrowsOnAProfileThatWasToldNothing()
		{
			Assert.DoesNotThrow(() => { bool _ = profile.IsWarrantyEnvelopePublished(); });
			Assert.DoesNotThrow(() => { bool _ = profile.HasDeliveryBeenRecorded(); });
		}

		[Test]
		public void BothPresenceReadsAnswerNo()
		{
			Assert.That(profile.IsWarrantyEnvelopePublished(), Is.False);
			Assert.That(profile.HasDeliveryBeenRecorded(), Is.False);
		}

		// Y el perfil sabe de que vehiculo es, que es lo unico que se le sembro.
		[Test]
		public void TheProfileNamesTheVehicleItIsAbout()
		{
			Assert.That(profile.ReadVin(), Is.EqualTo(TheVin));
		}

		[Test]
		public void AProfileWithoutAVinIsRefused()
		{
			Assert.Throws<DomainException>(() => new VehicleProfile(string.Empty));
			Assert.Throws<DomainException>(() => new VehicleProfile(null));
		}

		// ===== Y LOS ACCESORES REVIENTAN ===================================================

		[Test]
		public void EveryGuardedAccessorOfAnUntoldProfileRefusesInsteadOfInventing()
		{
			Assert.Throws<DomainException>(() => { DateTime _ = profile.ReadEnvelopePublishedAt(); });
			Assert.Throws<DomainException>(() => { int _ = profile.EnvelopeMonths(); });
			Assert.Throws<DomainException>(() => { int _ = profile.EnvelopeDistance(); });
			Assert.Throws<DomainException>(() => { DistanceUnitCode _ = profile.EnvelopeUnit(); });
			Assert.Throws<DomainException>(() => { DateTime _ = profile.ReadDeliveredAt(); });
			Assert.Throws<DomainException>(() => { int _ = profile.ReadDeliveryReading(); });
			Assert.Throws<DomainException>(() => { string _ = profile.ReadDeliveryUnit(); });
		}

		// LA MITAD PUBLICADA SIGUE CONTESTANDO, y con palabras (L7, E48). Que no haya sobre no
		// vuelve incitable lo que la agencia publico -- o no publico -- porque esta lectura no
		// necesita ni un hecho del vehiculo.
		[Test]
		public void ThePublishedHalfStillAnswersWithWordsAndWithoutADigit()
		{
			string said = profile.DescribePublishedWarrantyEnvelope();

			Assert.That(said, Does.Contain("not published"));
			Assert.That(said, Does.Not.Match(@"\d"),
				"Un cero o un numero cualquiera en la frase de una ausencia es como entra un default "
				+ "sin que nadie lo escriba: se lee como un limite que alguien publico.");
		}

		// Y LA RESPUESTA COMPUESTA TAMBIEN CONTESTA -- D1: un sobre no publicado es una respuesta
		// DECIDIDA, con el miembro NotPublished, no una suspension. CanBeEvaluated da TRUE para un
		// vehiculo sin sobre, que se lee raro una vez y es correcto.
		[Test]
		public void TheComposedAnswerIsSettledAsNotPublishedAndNotSuspended()
		{
			EnvelopeAnswer answer = profile.TheStandingWithNoOdometerAsOf(TheInstantAsked);

			Assert.That(answer.CanBeEvaluated(), Is.True,
				"D1: se pudo evaluar, y lo que se evaluo es que nadie publico un sobre.");
			Assert.That(answer.Standing(), Is.SameAs(EnvelopeStanding.NotPublished));
			Assert.That(answer.SuspendedByAnAbsentDelivery(), Is.False,
				"La entrega tambien falta, y NO es la respuesta: sin sobre no hay nada que medir, "
				+ "asi que el ancla ausente no llega ni a ser la razon.");
		}

		// EL ORDEN DE LAS GUARDAS ES LA LEY (T1, departure 2): el sobre se pregunta ANTES que la
		// entrega. Con las dos ausentes, la respuesta es NotPublished y no la del ancla.
		[Test]
		public void TheEnvelopeIsAskedBeforeTheDeliveryWhenBothAreMissing()
		{
			EnvelopeAnswer answer = profile.TheStandingAsOf(
				25000, DistanceUnitCode.UnitA, TheInstantAsked, TheInstantAsked);

			Assert.That(answer.Standing(), Is.SameAs(EnvelopeStanding.NotPublished));
		}

		// ===== L8: EL CONSTRUCTOR TOMA EL VIN Y NADA MAS ==================================

		[Test]
		public void TheConstructorTakesTheVinAndNothingElse()
		{
			System.Reflection.ConstructorInfo[] ctores = typeof(VehicleProfile).GetConstructors(
				System.Reflection.BindingFlags.Instance
				| System.Reflection.BindingFlags.Public
				| System.Reflection.BindingFlags.NonPublic);

			Assert.That(ctores.Length, Is.EqualTo(1),
				"Un segundo constructor es un segundo camino de siembra, y la mitad de L8 es que "
				+ "haya uno solo.");

			System.Reflection.ParameterInfo[] parametros = ctores[0].GetParameters();

			Assert.That(parametros.Length, Is.EqualTo(1),
				"L8: el sobre, el instante de entrega y la lectura de entrega llegan por COMANDO. "
				+ "Un argumento de siembra queda fijo en la primera apertura del journal y no se "
				+ "puede corregir despues -- el cuerpo del upgrade no vuelve a correr.");
			Assert.That(parametros[0].ParameterType, Is.EqualTo(typeof(string)));
		}
	}
}
