using System;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// QUE ES UNA ESPECIFICACION DE COMBUSTIBLE, segun el canon §4 del brief: el grado minimo, los
	// permitidos y los prohibidos. NO es una lista: son tres posturas distintas sobre grados, y la
	// que de verdad importa decir es la tercera. Un grado prohibido callado es un motor danado, y
	// callarlo es lo que pasa cuando alguien colapsa las tres en "los combustibles compatibles".
	//
	// TEST 8 DE §11: un modelo sin ninguna especificacion de combustible contesta "no publicado" y
	// la lectura NO revienta. Es la lectura sin clave de la rebanada -- no hay conjunto cerrado que
	// recorrer, se pregunta por el item entero -- y sigue siendo total.
	//
	// EL ITEM ES LA ESPECIFICACION ENTERA Y NO CADA GRADO. Los fluidos y las llantas se publican
	// por clase y por posicion, asi que una revision que corrige el aceite no borra el refrigerante.
	// El combustible no tiene esa clave: una revision publica LA especificacion de combustible, y la
	// ultima sellada que la publico es la que contesta, entera. Publicar un solo grado en una
	// revision nueva reemplaza la lista, no la agrega -- que es lo que "superseder retira lo que la
	// sucesora no republico" quiere decir cuando el item no se subdivide.
	[TestFixture]
	public sealed class TheFuelGradesArePublishedWithTheirStandings
	{
		private const string TheModel = "SM-SEDAN-2025";
		private const string TheRevision = "REV-2025-01";
		private const string TheNextRevision = "REV-2025-02";

		private static readonly DateTime PublishedAt = new DateTime(2025, 3, 14, 10, 0, 0);

		private ModelSpecification specification;

		[SetUp]
		public void Setup()
		{
			specification = new ModelSpecification(TheModel);
			specification.OpenRevision(TheRevision, PublishedAt);
		}

		// La frase entera, clavada, con los tres grados en el orden en que se publicaron y cada uno
		// diciendo su postura.
		[Test]
		public void TheSentenceNamesEveryGradeWithItsStandingAndThenItsRevision()
		{
			TheThreeGradesEnter();
			specification.SealRevision(TheRevision);

			Assert.That(specification.DescribeFuel(), Is.EqualTo(
				"Grados de combustible: RON 95 (mínimo), RON 98 (permitido), E85 (prohibido). "
				+ "Publicado en la revisión REV-2025-01."));
		}

		// La postura viaja pegada al grado. Un grado sin su postura seria "los combustibles que
		// aparecen en la hoja", que es exactamente la lectura que hace que alguien eche el
		// prohibido.
		[Test]
		public void AProhibitedGradeIsSaidToBeProhibited()
		{
			TheThreeGradesEnter();
			specification.SealRevision(TheRevision);

			Assert.That(specification.DescribeFuel(), Does.Contain("E85 (prohibido)"));
		}

		// TEST 8 DE §11. Sin ninguna revision no hay especificacion de combustible, y la lectura
		// contesta con palabras en vez de reventar.
		[Test]
		public void AModelWithNoFuelSpecificationAtAllAnswersNotPublishedAndDoesNotThrow()
		{
			ModelSpecification withoutAnything = new ModelSpecification(TheModel);

			Assert.That(withoutAnything.IsFuelPublished(), Is.False);
			Assert.That(withoutAnything.ReadFuelRevision(), Is.EqualTo(""));

			string answer = withoutAnything.DescribeFuel();

			Assert.That(answer, Is.Not.Empty);
			Assert.That(answer, Does.Contain("Grados de combustible"),
				"La ausencia nombra lo que se pregunto. Sin eso la misma frase serviria para todo.");
			Assert.That(HayAlgunDigito(answer), Is.False,
				"Ni un numero adentro: un grado inventado o un cero se leen como dato publicado.");
		}

		// Y una hoja sellada que publico OTRA cosa tampoco inventa un combustible.
		[Test]
		public void ASealedSheetWithoutFuelStillAnswersNotPublishedForFuel()
		{
			specification.PublishTyreSpecification(
				TheRevision, TyrePositionCode.Front, "205/55R16",
				2.4m, PressureUnitCode.UnitA, "91V");
			specification.SealRevision(TheRevision);

			Assert.That(specification.IsFuelPublished(), Is.False);
			Assert.That(specification.DescribeFuel(), Does.Not.Contain("REV-2025-01"),
				"Una respuesta sin valor no puede llevar procedencia: diria que esa revision "
				+ "contesto por el combustible, y no contesto.");
		}

		[Test]
		public void AGradeWithoutATextIsNotAPublishedGrade()
		{
			Assert.Throws<DomainException>(() => specification.PublishFuelGrade(
				TheRevision, "   ", FuelGradeStandingCode.Minimum));
		}

		// Dentro de UN borrador, la ultima publicacion de un grado es la que queda: se publica el
		// mismo texto con otra postura y manda la segunda. Es una correccion antes de sellar.
		[Test]
		public void WithinOneDraftTheLastPublicationOfAGradeIsTheOneThatStands()
		{
			specification.PublishFuelGrade(TheRevision, "E85", FuelGradeStandingCode.Permitted);
			specification.PublishFuelGrade(TheRevision, "E85", FuelGradeStandingCode.Prohibited);
			specification.SealRevision(TheRevision);

			Assert.That(specification.DescribeFuel(), Does.Contain("E85 (prohibido)"));
			Assert.That(specification.DescribeFuel(), Does.Not.Contain("E85 (permitido)"));
		}

		// EL ITEM ES LA ESPECIFICACION ENTERA. La revision 2 publica un solo grado y contesta ella
		// sola: la lista de la revision 1 no se arrastra. Republicar es reemplazar.
		[Test]
		public void ALaterRevisionPublishesTheWholeFuelSpecificationAndNotAnAddition()
		{
			TheThreeGradesEnter();
			specification.SealRevision(TheRevision);

			specification.OpenRevision(TheNextRevision, PublishedAt.AddDays(30));
			specification.PublishFuelGrade(
				TheNextRevision, "RON 91", FuelGradeStandingCode.Minimum);
			specification.SealRevision(TheNextRevision);

			Assert.That(specification.ReadFuelRevision(), Is.EqualTo(TheNextRevision));
			Assert.That(specification.DescribeFuel(), Does.Contain("RON 91 (mínimo)"));
			Assert.That(specification.DescribeFuel(), Does.Not.Contain("E85"),
				"La revision 2 publico LA especificacion de combustible, no un anexo a la de la "
				+ "revision 1. Arrastrar lo viejo mezclaria dos procedencias en una sola frase.");
		}

		// Y un borrador con grados adentro sigue sin contestar (L12).
		[Test]
		public void FuelPublishedOnlyIntoADraftIsNotAnswered()
		{
			TheThreeGradesEnter();

			Assert.That(specification.IsFuelPublished(), Is.False);
			Assert.That(specification.DescribeFuel(), Does.Not.Contain("RON 95"));
		}

		private void TheThreeGradesEnter()
		{
			specification.PublishFuelGrade(TheRevision, "RON 95", FuelGradeStandingCode.Minimum);
			specification.PublishFuelGrade(TheRevision, "RON 98", FuelGradeStandingCode.Permitted);
			specification.PublishFuelGrade(TheRevision, "E85", FuelGradeStandingCode.Prohibited);
		}

		private static bool HayAlgunDigito(string frase)
		{
			foreach (char letra in frase) if (char.IsDigit(letra)) return true;
			return false;
		}
	}
}
