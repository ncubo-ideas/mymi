using System;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// E45, regla 2: un singleton opcional es un PAR DE OBJETO NULO, no un campo anulable. Este
	// slice trae DOS, y los dos son el mismo hecho visto desde dos lados: un vehiculo del que la
	// agencia todavia no dijo nada.
	//
	//   Delivery          -- no se registro la entrega, asi que no hay ancla desde donde medir
	//   WarrantyEnvelope  -- la agencia no publico ningun sobre
	//
	// LOS DOS SON HECHOS LEGITIMOS DEL NEGOCIO, no defectos: un perfil recien creado no tiene
	// ninguno de los dos, y las dos preguntas de presencia tienen que contestar sin reventar
	// (prueba 1 de §11 del brief).
	//
	// POLARIDAD, Y AQUI HAY UNA DECISION QUE EL MODULO NO TOMA IGUAL EN TODOS LADOS. AnchorReading
	// deja su IsExpressedIn TOTAL -- un ancla sin lectura no esta medida en ninguna unidad, y eso
	// es una respuesta. NoSystemLimits hace lo contrario y GUARDA su MeasuresDistanceIn. Estos dos
	// pares copian a NoSystemLimits, y la razon es concreta: aqui un false benigno nombraria LA
	// AUSENCIA EQUIVOCADA. Un sobre que no existe contestando "no esta en km" mandaria al que
	// compone hacia AnEnvelopeSuspendedByAUnitMismatch cuando la verdad es NotPublished, que es
	// una respuesta decidida (D1). Es el tercer anti-patron de E47 con un booleano en vez de un
	// miembro.
	//
	// LA UNICA TOTAL DEL SOBRE ES Describe, y tambien por una razon: "nadie publico un sobre" ES
	// la respuesta (ley L2 del slice anterior, UnpublishedIsAnAnswer), y es la mitad publicada que
	// L7 pide que se pueda citar sin tener ningun hecho del vehiculo. Un throw ahi haria
	// incitable la mitad publicada por no tener la personal, que es exactamente C63 y E48.
	[TestFixture]
	public sealed class AnAbsentDeliveryIsATypeAndNotANull
	{
		private const int ThirtySixMonths = 36;
		private const int SixtyThousand = 60000;
		private const int TenThousand = 10000;

		private static readonly DateTime DeliveredAt = new DateTime(2024, 1, 1, 9, 0, 0);
		private static readonly DateTime PublishedAt = new DateTime(2024, 1, 1, 10, 0, 0);

		// ===== LA ENTREGA ==================================================================

		// La pregunta de presencia es TOTAL: contesta siempre, tambien cuando no hay nada.
		[Test]
		public void TheAbsenceAnswersThatThereIsNoDelivery()
		{
			Assert.That(Delivery.None.Exists(), Is.False);
		}

		// Los accesores son GUARDADOS (E41). Una lectura de entrega en cero seria el mismo defecto
		// que AnchorReading evita del otro lado: un odometro que nadie leyo apareceria en cero, y
		// todo lo recorrido desde entonces se contaria como recorrido bajo garantia.
		[Test]
		public void ReadingTheAbsentDeliveryThrows()
		{
			Assert.Throws<DomainException>(() => { DateTime _ = Delivery.None.DeliveredAt; });
			Assert.Throws<DomainException>(() => { int _ = Delivery.None.Reading; });
			Assert.Throws<DomainException>(() => { DistanceUnit _ = Delivery.None.Unit; });
		}

		// Y la pregunta de unidad tambien esta GUARDADA, a diferencia de la de AnchorReading. Ver
		// la cabecera: un false benigno aqui nombra la ausencia equivocada.
		[Test]
		public void AskingTheAbsentDeliveryForItsUnitThrowsInsteadOfAnsweringNo()
		{
			Assert.Throws<DomainException>(
				() => Delivery.None.IsExpressedIn(DistanceUnit.UnitA),
				"Un false aqui se leeria como 'la entrega esta en otra unidad', y quien componga "
				+ "la respuesta contestaria un desajuste de unidades sobre una entrega que no "
				+ "existe. La ausencia de la entrega tiene su propio tipo con nombre.");
		}

		[Test]
		public void APresentDeliveryAnswersWithWhatItWasToldOnce()
		{
			Delivery delivery = Delivery.Of(DeliveredAt, TenThousand, DistanceUnit.UnitA);

			Assert.That(delivery.Exists(), Is.True);
			Assert.That(delivery.DeliveredAt, Is.EqualTo(DeliveredAt));
			Assert.That(delivery.Reading, Is.EqualTo(TenThousand));
			Assert.That(delivery.Unit, Is.SameAs(DistanceUnit.UnitA),
				"La unidad se compara por IDENTIDAD, nunca por nombre (E23).");
		}

		// Por IDENTIDAD, y con la unidad que NO es la primera del conjunto: con UnitA las dos
		// mitades de la comparacion coincidirian tambien si el tipo devolviera un default (C60).
		[Test]
		public void ADeliveryIsExpressedInItsOwnUnitAndNotInTheOther()
		{
			Delivery delivery = Delivery.Of(DeliveredAt, TenThousand, DistanceUnit.UnitB);

			Assert.That(delivery.IsExpressedIn(DistanceUnit.UnitB), Is.True);
			Assert.That(delivery.IsExpressedIn(DistanceUnit.UnitA), Is.False);
		}

		[Test]
		public void ADeliveryWithoutAUnitIsRefused()
		{
			Assert.Throws<DomainException>(() => Delivery.Of(DeliveredAt, TenThousand, null));
		}

		[Test]
		public void ADeliveryReadingCannotBeNegative()
		{
			Assert.Throws<DomainException>(
				() => Delivery.Of(DeliveredAt, -1, DistanceUnit.UnitA));
		}

		// ===== EL SOBRE ====================================================================

		[Test]
		public void TheAbsenceAnswersThatNoEnvelopeWasPublished()
		{
			Assert.That(WarrantyEnvelope.None.Exists(), Is.False);
		}

		[Test]
		public void ReadingTheUnpublishedEnvelopeThrows()
		{
			Assert.Throws<DomainException>(() => { int _ = WarrantyEnvelope.None.Months; });
			Assert.Throws<DomainException>(() => { int _ = WarrantyEnvelope.None.Distance; });
			Assert.Throws<DomainException>(
				() => { DateTime _ = WarrantyEnvelope.None.PublishedAt; });
			Assert.Throws<DomainException>(
				() => { DistanceUnit _ = WarrantyEnvelope.None.TheUnitItsDistanceIsMeasuredIn; });
			Assert.Throws<DomainException>(
				() => { DistanceUnitCode _ = WarrantyEnvelope.None.TheCodeOfItsDistanceUnit; });
			Assert.Throws<DomainException>(
				() => WarrantyEnvelope.None.MeasuresDistanceIn(DistanceUnit.UnitA));
		}

		// LA EXCEPCION, Y ES LEY L7. La mitad publicada se contesta sola: quien no tenga ni un
		// hecho del vehiculo -- ni odometro, ni entrega, ni nada -- igual puede citar que no hay
		// sobre publicado. Una frase, no un throw y no un vacio.
		[Test]
		public void TheUnpublishedEnvelopeStillSaysSoInWords()
		{
			string said = TheSentenceTheDoorWouldCompose.ThePublishedWarrantyEnvelope(
				WarrantyEnvelope.None, TheLanguage.OfTheRecord);

			Assert.That(said, Is.Not.Empty,
				"Una cadena vacia no es 'no publicado': la vista la renderiza como un blanco.");
			Assert.That(said, Does.Contain("no ha publicado"),
				"La ausencia se dice diciendo QUIEN no publico que. Callarlo deja a la persona "
				+ "creyendo que le contestaron.");

			foreach (char letra in said)
				Assert.That(char.IsDigit(letra), Is.False,
					"Ni un numero adentro de la frase de la ausencia: '0 meses' se lee como un "
					+ "limite publicado, que es como un default entra sin que nadie lo escriba.");
		}

		[Test]
		public void APublishedEnvelopeAnswersWithBothLimitsItsUnitAndItsInstant()
		{
			WarrantyEnvelope envelope = APublishedEnvelopeIn(DistanceUnit.UnitA);

			Assert.That(envelope.Exists(), Is.True);
			Assert.That(envelope.Months, Is.EqualTo(ThirtySixMonths));
			Assert.That(envelope.Distance, Is.EqualTo(SixtyThousand));
			Assert.That(envelope.TheUnitItsDistanceIsMeasuredIn, Is.SameAs(DistanceUnit.UnitA));
			Assert.That(envelope.PublishedAt, Is.EqualTo(PublishedAt));
		}

		// LEY L9: la frase del sobre nombra CUANDO se publico. Un sobre viejo tiene que verse
		// viejo, igual que una hoja de especificacion vieja.
		[Test]
		public void ThePublishedEnvelopeSaysBothLimitsAndWhenItWasPublished()
		{
			string said = TheSentenceTheDoorWouldCompose.ThePublishedWarrantyEnvelope(
				APublishedEnvelopeIn(DistanceUnit.UnitA), TheLanguage.OfTheRecord);

			Assert.That(said, Does.Contain("36"));
			Assert.That(said, Does.Contain("60.000"));
			Assert.That(said, Does.Contain("km"),
				"La unidad se dice con su nombre del mundo real, no con 'UnitA'.");
			Assert.That(said, Does.Contain("1 de enero de 2024"),
				"Ley L9: sin el instante de publicacion nadie puede ver que el sobre esta viejo.");
		}

		[Test]
		public void APublishedEnvelopeMeasuresItsDistanceInItsOwnUnitAndNotInTheOther()
		{
			WarrantyEnvelope envelope = APublishedEnvelopeIn(DistanceUnit.UnitB);

			Assert.That(envelope.MeasuresDistanceIn(DistanceUnit.UnitB), Is.True);
			Assert.That(envelope.MeasuresDistanceIn(DistanceUnit.UnitA), Is.False,
				"Ley L4: dos unidades distintas no se comparan, se dejan de comparar.");
		}

		// EL PRIMER MIEMBRO DEL MODULO QUE DEVUELVE UN ENUM (G1 del spike, §2.3 del plan). Existe
		// porque un argumento que cruza DESDE OTRO GLOBAL lleva el VALOR TIPADO y no el nombre del
		// miembro -- al reves de como cruza por @param (C68, C12/C25) -- y el slot de la lectura de
		// cobertura toma el enum concreto.
		//
		// SE PRUEBA CON UnitB A PROPOSITO: con UnitA, el primer miembro del enum, un default del
		// motor daria exactamente el mismo resultado y la prueba estaria verde por nada (C60).
		[Test]
		public void TheEnvelopeHandsBackTheCodeOfItsOwnUnitAndNotTheFirstMember()
		{
			WarrantyEnvelope envelope = APublishedEnvelopeIn(DistanceUnit.UnitB);

			Assert.That(envelope.TheCodeOfItsDistanceUnit, Is.EqualTo(DistanceUnitCode.UnitB));
			Assert.That(envelope.TheCodeOfItsDistanceUnit,
				Is.Not.EqualTo(DistanceUnitCode.UnitA));
		}

		// Y el codigo y la unidad son las dos caras de lo mismo: el resolutor los vuelve a juntar.
		// Si se despegaran, el script que cruza el codigo mediria contra otra unidad que la que la
		// frase nombra, y nada se pondria rojo.
		[Test]
		public void TheCodeAndTheUnitAreTheSameMemberSaidTwice()
		{
			foreach (DistanceUnit unit in new[] { DistanceUnit.UnitA, DistanceUnit.UnitB })
			{
				WarrantyEnvelope envelope = APublishedEnvelopeIn(unit);

				Assert.That(
					DistanceUnit.OfCode(envelope.TheCodeOfItsDistanceUnit),
					Is.SameAs(envelope.TheUnitItsDistanceIsMeasuredIn));
			}
		}

		[Test]
		public void AnEnvelopeWithoutAUnitIsRefused()
		{
			Assert.Throws<DomainException>(() => WarrantyEnvelope.PublishedAs(
				ThirtySixMonths, SixtyThousand, null, PublishedAt));
		}

		// LEY L3 SOSTENIDA POR ESTRUCTURA. El sobre del VEHICULO trae siempre sus dos mitades --
		// es lo que OSD-09 pide que el evento diga -- a diferencia de SystemLimits, donde un
		// sistema puede publicar una sola porque solo estrecha una dimension. Aceptar un cero aqui
		// dejaria dicho "0 km" en la frase, que se lee como un sobre alcanzado el dia de la
		// entrega; refusarlo revienta en el comando, donde alguien lo puede corregir.
		[TestCase(0, SixtyThousand)]
		[TestCase(ThirtySixMonths, 0)]
		[TestCase(-1, SixtyThousand)]
		[TestCase(ThirtySixMonths, -1)]
		public void AnEnvelopeMissingOneOfItsTwoHalvesIsRefused(int months, int distance)
		{
			Assert.Throws<DomainException>(() => WarrantyEnvelope.PublishedAs(
				months, distance, DistanceUnit.UnitA, PublishedAt));
		}

		private static WarrantyEnvelope APublishedEnvelopeIn(DistanceUnit unit)
		{
			return WarrantyEnvelope.PublishedAs(
				ThirtySixMonths, SixtyThousand, unit, PublishedAt);
		}
	}
}
