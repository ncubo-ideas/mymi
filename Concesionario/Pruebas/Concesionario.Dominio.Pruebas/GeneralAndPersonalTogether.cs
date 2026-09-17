using System;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LEY L7 del brief, GeneralAndPersonalTogether: "toda respuesta empareja la VERDAD PUBLICADA
	// con la POSICION DE ESTE VEHICULO frente a ella. La mitad publicada sola es un folleto; la
	// posicion sola no significa nada. Esto es el producto, dicho como ley."
	//
	// SON LOS TESTS 17 Y 18 DE §11. Y la mitad recibida ENTRA COMO PARAMETRO y SE VA CON LA
	// RESPUESTA: el modulo no guarda nada de ningun vehiculo, que es lo que deja que muchos
	// compartan una sola especificacion (§8 del brief). La prueba de que no se queda es leer dos
	// veces con numeros distintos y volver a leer con los primeros.
	//
	// LA AUSENCIA NO SE EMPAREJA, A PROPOSITO. Si no hay mitad publicada no hay contra que parar la
	// del vehiculo, y pegarle la distancia recibida meteria un numero adentro de una frase de "no
	// publicado" -- que es exactamente la forma en que un default se lee como dato (L2).
	[TestFixture]
	public sealed class GeneralAndPersonalTogether
	{
		private const string TheModel = "YARIS-2019";
		private const string TheRevision = "REV-2025-01";
		private const string TheRule = "Work may be done at any workshop.";
		private const string WhatPreservesCover = "Keep the invoice and the parts numbers.";

		private static readonly DateTime PublishedAt = new DateTime(2025, 3, 14, 10, 0, 0);

		private ModelSpecification specification;

		[SetUp]
		public void Setup()
		{
			specification = new ModelSpecification(TheModel);
			specification.OpenRevision(TheRevision, PublishedAt);
		}

		// TEST 17 DE §11: publicado 10000, recibido 8400, y la respuesta lleva LOS DOS.
		[Test]
		public void TheIntervalAnswerCarriesThePublishedIntervalAndTheReceivedDistanceRemaining()
		{
			TheFluidChangeIntervalEnters();
			specification.SealRevision(TheRevision);

			string answer = TheSentenceTheDoorWouldCompose.TheServiceInterval(
				specification, OperationKindCode.FluidChange, 8400, DistanceUnitCode.UnitA,
				TheLanguage.OfTheRecord);

			Assert.That(answer, Does.Contain("10.000"),
				"La mitad publicada. Sin ella la respuesta es la posicion de un carro contra nada.");
			Assert.That(answer, Does.Contain("8.400"),
				"La mitad recibida. Sin ella la respuesta es un folleto -- que es la palabra que la "
				+ "ley usa.");
			Assert.That(answer, Does.Contain(TheRevision),
				"Y sigue nombrando su revision: L7 empareja, no reemplaza a L1.");
		}

		// TEST 18 DE §11: dos servicios externos con evidencia y cero mantenimientos perdidos, y la
		// respuesta dice las dos mitades -- la regla publicada y esos numeros.
		[Test]
		public void TheServicingElsewhereAnswerCarriesThePublishedRuleAndTheReceivedStanding()
		{
			ThePolicyEnters();
			specification.SealRevision(TheRevision);

			string answer = TheSentenceTheDoorWouldCompose.TheServicingElsewhereRule(
				specification, 2, 0, 0, TheLanguage.OfTheRecord);

			Assert.That(answer, Does.Contain(TheRule));
			Assert.That(answer, Does.Contain(WhatPreservesCover));
			Assert.That(answer, Does.Contain("servicios externos con comprobante 2"));
			Assert.That(answer, Does.Contain("mantenimientos no hechos 0"));
			Assert.That(answer, Does.Contain(TheRevision));
		}

		[Test]
		public void TheServicingElsewhereSentenceIsExactlyThis()
		{
			ThePolicyEnters();
			specification.SealRevision(TheRevision);

			Assert.That(
				TheSentenceTheDoorWouldCompose.TheServicingElsewhereRule(
					specification, 2, 1, 0, TheLanguage.OfTheRecord),
				Is.EqualTo(
				"Servicio en otro taller: \"Work may be done at any workshop.\" Qué conserva la cobertura: "
				+ "\"Keep the invoice and the parts numbers.\" Este vehículo: servicios externos con "
				+ "comprobante 2, servicios externos sin comprobante 1, mantenimientos no hechos "
				+ "0. Publicado en la revisión REV-2025-01."));
		}

		// LA MITAD RECIBIDA NO SE QUEDA. Se lee con unos numeros, despues con otros, y despues otra
		// vez con los primeros: si el modulo hubiera guardado algo, la tercera lectura no volveria
		// a decir lo mismo que la primera.
		[Test]
		public void TheReceivedHalfLeavesWithTheAnswerAndIsNeverKept()
		{
			TheFluidChangeIntervalEnters();
			ThePolicyEnters();
			specification.SealRevision(TheRevision);

			string first = TheSentenceTheDoorWouldCompose.TheServiceInterval(
				specification, OperationKindCode.FluidChange, 8400, DistanceUnitCode.UnitA,
				TheLanguage.OfTheRecord);
			string second = TheSentenceTheDoorWouldCompose.TheServiceInterval(
				specification, OperationKindCode.FluidChange, 200, DistanceUnitCode.UnitA,
				TheLanguage.OfTheRecord);
			string again = TheSentenceTheDoorWouldCompose.TheServiceInterval(
				specification, OperationKindCode.FluidChange, 8400, DistanceUnitCode.UnitA,
				TheLanguage.OfTheRecord);

			Assert.That(second, Is.Not.EqualTo(first));
			Assert.That(again, Is.EqualTo(first),
				"El modulo no guarda nada de ningun vehiculo: la misma pregunta contesta lo mismo "
				+ "sin importar que le preguntaron en el medio.");

			Assert.That(TheSentenceTheDoorWouldCompose.TheServicingElsewhereRule(
				specification, 2, 0, 0, TheLanguage.OfTheRecord),
				Is.Not.EqualTo(TheSentenceTheDoorWouldCompose.TheServicingElsewhereRule(
					specification, 0, 0, 3, TheLanguage.OfTheRecord)));
		}

		// LA MITAD PUBLICADA SOLA NO SALE. Es el reverso de la ley y es lo que la hace una ley y no
		// una preferencia de redaccion: una respuesta que trae el intervalo y no dice donde esta
		// parado este carro es un folleto.
		//
		// LA LEY CAMBIO DE FORMA Y NO DE CONTENIDO. Antes las dos mitades eran dos textos que
		// GeneralAndPersonalTogether pegaba, y las dos guardas miraban si alguno venia en blanco.
		// Ahora el emparejamiento lo lleva el portador: emparejar una ausencia sigue REVENTANDO, y
		// la otra mitad -- una respuesta emparejada a la que le falte la del vehiculo -- dejo de ser
		// representable, porque el portador siempre lleva la forma del remanente y sus valores. Se
		// afirma lo que queda falsificable, y la pareja que ya no lo es se dice aqui en vez de
		// desaparecer sin que nadie lo note.
		[Test]
		public void ThePublishedHalfAloneIsRefusedWhenBothHalvesAreBeingPaired()
		{
			Assert.Throws<DomainException>(
				() => AServicingClause.OfNothingPublished().PairedWith(2, 1, 3));

			Assert.Throws<DomainException>(
				() => AServiceIntervalClause
					.OfNothingPublished(
						MaintenanceOperationKind.OfCode(OperationKindCode.FluidChange))
					.PairedWith(TheIntervalPublished(), 8400, DistanceUnit.OfCode(
						DistanceUnitCode.UnitA)));

			Assert.That(
				AServicingClause.Published(TheRevision, TheRule, WhatPreservesCover)
					.PairedWith(0, 0, 0)
					.TheHalvesItAnswers(),
				Is.EqualTo("GeneralAndPersonalTogether"),
				"Y una emparejada lo dice aunque los conteos del vehiculo sean cero: un cero es una "
				+ "posicion, y callarla seria devolver el folleto que la ley prohibe.");
		}

		private static PublishedInterval TheIntervalPublished()
		{
			return new PublishedInterval(
				10000, DistanceUnit.OfCode(DistanceUnitCode.UnitA), 365);
		}

		// LA AUSENCIA NO SE EMPAREJA. Sin regla publicada no hay contra que parar los numeros del
		// vehiculo, y meterlos igual convertiria una ausencia en una frase con datos.
		[Test]
		public void AnUnpublishedItemAnswersItsAbsenceAndNotTheReceivedHalf()
		{
			specification.PublishTyreSpecification(
				TheRevision, TyrePositionCode.Front, "205/55R16",
				2.4m, PressureUnitCode.UnitA, "91V");
			specification.SealRevision(TheRevision);

			string answer = TheSentenceTheDoorWouldCompose.TheServicingElsewhereRule(
				specification, 2, 1, 3, TheLanguage.OfTheRecord);

			Assert.That(specification.IsServicingPolicyPublished(), Is.False);
			Assert.That(answer, Does.Not.Contain("external services"));
			Assert.That(HayAlgunDigito(answer), Is.False,
				"Sin mitad publicada la respuesta es la ausencia y nada mas. Los numeros recibidos "
				+ "adentro de un 'no publicado' se leen como si algo se hubiera publicado.");
		}

		private void TheFluidChangeIntervalEnters()
		{
			specification.PublishServiceInterval(
				TheRevision, OperationKindCode.FluidChange, 10000, DistanceUnitCode.UnitA, 365);
		}

		private void ThePolicyEnters()
		{
			specification.PublishServicingPolicy(TheRevision, TheRule, WhatPreservesCover);
		}

		private static bool HayAlgunDigito(string frase)
		{
			foreach (char letra in frase) if (char.IsDigit(letra)) return true;
			return false;
		}
	}
}
