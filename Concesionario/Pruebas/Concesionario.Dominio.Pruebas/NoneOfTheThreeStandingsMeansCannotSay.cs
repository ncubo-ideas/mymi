using System;
using System.Collections.Generic;
using System.Linq;
using Concesionario.App.Wording;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// E47, y es la mitad de la entrada que cuesta plata cuando se pierde: "cannot-evaluate es una
	// respuesta de primera clase, y el conjunto de resultados NO TIENE un miembro que diga 'no lo
	// se'. Devolver el benigno para una entrada inevaluable es el anti-patron que se lee como una
	// medicion que nadie tomo."
	//
	// EL CONJUNTO TIENE TRES MIEMBROS Y LA PREGUNTA TIENE CUATRO RESPUESTAS HONESTAS:
	//
	//   dentro del sobre              -> EnvelopeStanding.Inside
	//   alcanzado                     -> EnvelopeStanding.Reached
	//   la agencia no publico ninguno -> EnvelopeStanding.NotPublished   (D1: es una respuesta)
	//   no se puede saber             -> NO ES MIEMBRO. Es una de las tres variantes suspendidas.
	//
	// LA CUARTA ES LA QUE TIENE QUE QUEDARSE AFUERA. Si Inside saliera para un vehiculo sin
	// odometro, la app le diria a alguien que su carro esta en garantia sin haber medido nada, y
	// rio abajo -- pantalla, asistente, resumen -- todos lo repiten como hecho. Nada revienta y
	// nada se loguea: es el tercer anti-patron de E47, y por eso el accesor esta GUARDADO.
	//
	// POR QUE NO ALCANZA CON PROBAR LAS TRES SUSPENSIONES A MANO (C57): la ley no es sobre tres
	// tipos, es sobre TODA variante que no se pueda evaluar. La ultima prueba enumera las
	// variantes del ensamblado y exige que las cuentas cuadren, para que una quinta agregada mas
	// adelante sin decidir su polaridad se ponga roja aqui.
	[TestFixture]
	public sealed class NoneOfTheThreeStandingsMeansCannotSay
	{
		[Test]
		public void TheStandingsAreTheThreeOfTheCanon()
		{
			Assert.That(EnvelopeStanding.Inside.Name, Is.EqualTo("Inside"));
			Assert.That(EnvelopeStanding.Reached.Name, Is.EqualTo("Reached"));
			Assert.That(EnvelopeStanding.NotPublished.Name, Is.EqualTo("NotPublished"));
		}

		// Los tres son objetos DISTINTOS. Con comparacion por referencia y sin igualdad escrita
		// (E23), dos nombres apuntando al mismo objeto los vuelve indistinguibles -- y "todavia
		// adentro" y "nadie publico nada" son dos respuestas, no una.
		[Test]
		public void TheThreeStandingsAreThreeDifferentAnswers()
		{
			Assert.That(
				ReferenceEquals(EnvelopeStanding.Inside, EnvelopeStanding.Reached), Is.False);
			Assert.That(
				ReferenceEquals(EnvelopeStanding.Inside, EnvelopeStanding.NotPublished), Is.False);
			Assert.That(
				ReferenceEquals(EnvelopeStanding.Reached, EnvelopeStanding.NotPublished), Is.False);
		}

		// EL ACCESOR ESTA GUARDADO. Cada suspension revienta en vez de entregar un miembro.
		[Test]
		public void EverySuspendedAnswerRefusesToNameAStanding()
		{
			foreach (EnvelopeAnswer suspended in LasTresSuspensiones())
				Assert.Throws<DomainException>(() => { EnvelopeStanding _ = suspended.Standing(); },
					suspended.GetType().Name + " entrego un standing sin haber podido evaluar.");
		}

		// Y el mensaje del guard DICE LA REGLA, no la cita. Un mensaje que solo diga "no se pudo"
		// deja al que lo lee sin saber que tenia que haber preguntado antes.
		[Test]
		public void TheRefusalSaysWhatToAskFirst()
		{
			DomainException error = Assert.Throws<DomainException>(
				() => {
					EnvelopeStanding _ =
						new AnEnvelopeSuspendedByAnAbsentReading(
							AClauseUnderTest.WithNoEnvelope()).Standing();
				});

			Assert.That(error.Message, Does.Contain("evaluar"),
				"El mensaje manda preguntar primero si se podia evaluar. Sin eso, quien llame "
				+ "vuelve a intentar la misma llamada.");
		}

		// La pregunta de presencia es TOTAL en las cuatro variantes: contesta siempre, tambien en
		// las que no pueden dar resultado. Es la que hay que hacer antes, asi que no puede ser
		// ella la que reviente.
		[Test]
		public void AskingWhetherItCouldBeEvaluatedNeverThrows()
		{
			foreach (EnvelopeAnswer answer in LasCuatroVariantes())
				Assert.DoesNotThrow(() => { bool _ = answer.CanBeEvaluated(); });
		}

		// Y toda variante contesta con palabras, tambien la que no pudo evaluar: lo inevaluable se
		// dice, no se calla (E47). Un vacio aqui es una respuesta que la vista pinta en blanco.
		[Test]
		public void EveryAnswerSaysSomethingIncludingTheOnesThatCouldNotDecide()
		{
			foreach (EnvelopeAnswer answer in LasCuatroVariantes())
				Assert.That(
					TheSentenceTheDoorWouldCompose.TheWarrantyEnvelope(
						answer, TheLanguage.OfTheRecord),
					Is.Not.Empty, answer.GetType().Name);
		}

		// LEY L11, y estructural en vez de por costumbre: la clausula no la pone quien compone --
		// que puede olvidarse -- sino el marco de la respuesta, el mismo para las cuatro variantes.
		// Desde que la redaccion vive en el catalogo, el marco es la clave 'envelope.said' y la ley
		// es su hueco {law}: por eso la frase se compara contra la clausula del catalogo y no contra
		// una copia escrita aqui, que es la unica forma de que las dos no dejen de decir lo mismo.
		[Test]
		public void EveryAnswerSaysItIsNotAnAdjudication()
		{
			foreach (string language in new[]
				{ TheLanguage.OfTheRecord, TheLanguage.OfAnEnglishReader })
				foreach (EnvelopeAnswer answer in LasCuatroVariantes())
					Assert.That(
						TheSentenceTheDoorWouldCompose.TheWarrantyEnvelope(answer, language),
						Does.Contain(Phrases.Of(language).Say("envelope.law")),
						answer.GetType().Name + " no dice que esto no es la adjudicacion de un "
						+ "reclamo. Estar adentro del sobre no es una promesa de que le paguen.");
		}

		// LA CUENTA (C57). Cuatro variantes, exactamente una que contesta. Sin esto, las pruebas
		// de arriba siguen verdes el dia que entre una quinta variante que devuelva Inside sin
		// haber medido: nadie la agrego a la lista de a mano y nadie lo nota leyendo el diff.
		[Test]
		public void ExactlyOneOfTheVariantsAnswersAndTheRestAreEnumerated()
		{
			List<Type> declaradas = typeof(EnvelopeAnswer).Assembly.GetTypes()
				.Where(tipo => tipo.IsSubclassOf(typeof(EnvelopeAnswer)) && !tipo.IsAbstract)
				.ToList();

			List<EnvelopeAnswer> construidas = LasCuatroVariantes();

			Assert.That(
				construidas.Select(variante => variante.GetType()).OrderBy(t => t.Name).ToList(),
				Is.EqualTo(declaradas.OrderBy(t => t.Name).ToList()),
				"El ensamblado declara variantes de EnvelopeAnswer que esta prueba no construye, "
				+ "o al reves. Una variante sin probar es una variante cuya polaridad nadie "
				+ "decidio.");

			Assert.That(construidas.Count(variante => variante.CanBeEvaluated()), Is.EqualTo(1),
				"Exactamente una variante contesta. Dos serian dos formas de decidir lo mismo, y "
				+ "cero dejaria la pregunta sin respuesta posible.");
		}

		private static List<EnvelopeAnswer> LasCuatroVariantes()
		{
			List<EnvelopeAnswer> todas = new List<EnvelopeAnswer>
			{
				new ASettledEnvelopeAnswer(
					EnvelopeStanding.Inside, AClauseUnderTest.WithNoEnvelope()),
			};

			todas.AddRange(LasTresSuspensiones());

			return todas;
		}

		private static List<EnvelopeAnswer> LasTresSuspensiones()
		{
			return new List<EnvelopeAnswer>
			{
				new AnEnvelopeSuspendedByAnAbsentDelivery(AClauseUnderTest.WithNoEnvelope()),
				new AnEnvelopeSuspendedByAnAbsentReading(AClauseUnderTest.WithNoEnvelope()),
				new AnEnvelopeSuspendedByAUnitMismatch(
					DistanceUnit.UnitA, DistanceUnit.UnitB, AClauseUnderTest.WithAnEnvelopeIn(DistanceUnit.UnitA)),
			};
		}
	}
}
