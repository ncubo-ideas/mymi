using System;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// QUE ES UNA ESPECIFICACION DE LLANTA, segun el canon §4 del brief: medida, presion en frio y
	// clasificaciones, PARA UNA POSICION. Las tres juntas o no es una especificacion, igual que en
	// el fluido: media hoja publicada es lo que L12 existe para que nadie pueda citar.
	//
	// LA POSICION ES LA MITAD QUE MAS IMPORTA, y es lo que separa esta prueba de una copia de la
	// del fluido. Adelante y atras casi nunca llevan la misma presion, y la de repuesto casi nunca
	// lleva ninguna de las dos. Contestar la presion de otra posicion es el "valor prestado" que L2
	// nombra, con la diferencia de que aca la persona esta parada en una gasolinera con el
	// manometro en la mano.
	//
	// LA PRESION SALE EN LA UNIDAD EN QUE SE PUBLICO. Es la primera mitad del test 9 de §11; la
	// segunda -- la comparacion cruzada que contesta "no se puede evaluar" -- vive en
	// UnitsAreNeverConverted, porque es una comparacion y aca no hay ninguna.
	[TestFixture]
	public sealed class ATyreIsPublishedWithItsSizePressureAndRatings
	{
		private const string TheModel = "YARIS-2019";
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

		// La frase entera, clavada. Es lo que el asistente y la pantalla repiten tal cual.
		[Test]
		public void TheSentenceSaysThePositionTheSizeThePressureAndTheRatingsAndThenItsRevision()
		{
			specification.PublishTyreSpecification(
				TheRevision, TyrePositionCode.Front, "205/55R16",
				2.4m, PressureUnitCode.UnitA, "91V");
			specification.SealRevision(TheRevision);

			Assert.That(
				TheSentenceTheDoorWouldCompose.TheTyre(
					specification, TyrePositionCode.Front, TheLanguage.OfTheRecord),
				Is.EqualTo(
				"Llanta delantera: medida 205/55R16, presión en frío 2.4 bar, clasificaciones 91V. "
				+ "Publicado en la revisión REV-2025-01."));
		}

		// PRIMERA MITAD DEL TEST 9 DE §11: publicada en UnitA, contestada en UnitA. Ni se
		// normaliza ni se traduce, y el numero es el que se publico.
		[Test]
		public void TheColdPressureIsAnsweredInTheUnitItWasPublishedIn()
		{
			specification.PublishTyreSpecification(
				TheRevision, TyrePositionCode.Rear, "215/50R17",
				2.6m, PressureUnitCode.UnitB, "94W");
			specification.SealRevision(TheRevision);

			string answer = TheSentenceTheDoorWouldCompose.TheTyre(
				specification, TyrePositionCode.Rear, TheLanguage.OfTheRecord);

			Assert.That(answer, Does.Contain("presión en frío 2.6 psi"));
			Assert.That(answer, Does.Not.Contain("bar"),
				"Ley L8: la unidad publicada es la que sale. Si apareciera la otra, alguien "
				+ "convirtio -- y una presion convertida es alguien inflando una llanta al numero "
				+ "equivocado.");
		}

		// Cada posicion guarda la suya. Contestar la de adelante cuando preguntaron por la de
		// atras es el valor prestado de L2, y aca se paga fisicamente.
		[Test]
		public void EachPositionKeepsItsOwnPressure()
		{
			specification.PublishTyreSpecification(
				TheRevision, TyrePositionCode.Front, "205/55R16",
				2.4m, PressureUnitCode.UnitA, "91V");
			specification.PublishTyreSpecification(
				TheRevision, TyrePositionCode.Spare, "T125/80R16",
				4.2m, PressureUnitCode.UnitA, "97M");
			specification.SealRevision(TheRevision);

			Assert.That(TheSentenceTheDoorWouldCompose.TheTyre(
				specification, TyrePositionCode.Front, TheLanguage.OfTheRecord),
				Does.Contain("2.4 bar"));
			Assert.That(TheSentenceTheDoorWouldCompose.TheTyre(
				specification, TyrePositionCode.Spare, TheLanguage.OfTheRecord),
				Does.Contain("4.2 bar"));
			Assert.That(TheSentenceTheDoorWouldCompose.TheTyre(
				specification, TyrePositionCode.Front, TheLanguage.OfTheRecord),
				Does.Not.Contain("4.2"),
				"La respuesta de una posicion no puede llevar el numero de otra.");
		}

		// LEY L2 para las llantas: una posicion que ninguna revision sellada publico contesta,
		// no revienta, y no dice ningun numero.
		[Test]
		public void APositionThatWasNeverPublishedAnswersNotPublished()
		{
			specification.PublishTyreSpecification(
				TheRevision, TyrePositionCode.Front, "205/55R16",
				2.4m, PressureUnitCode.UnitA, "91V");
			specification.SealRevision(TheRevision);

			Assert.That(specification.IsTyrePublished(TyrePositionCode.Rear), Is.False);
			Assert.That(specification.ReadTyreRevision(TyrePositionCode.Rear), Is.EqualTo(""));

			string answer = TheSentenceTheDoorWouldCompose.TheTyre(
				specification, TyrePositionCode.Rear, TheLanguage.OfTheRecord);

			Assert.That(answer, Does.Contain("trasera"));
			Assert.That(answer, Does.Not.Contain("205/55R16"));
			Assert.That(HayAlgunDigito(answer), Is.False,
				"Una ausencia con un numero adentro se lee como un dato publicado, que es la forma "
				+ "en que un default entra sin que nadie lo escriba.");
		}

		[Test]
		public void ATyreWithoutASizeIsNotAPublishedSpecification()
		{
			Assert.Throws<DomainException>(() => specification.PublishTyreSpecification(
				TheRevision, TyrePositionCode.Front, "   ",
				2.4m, PressureUnitCode.UnitA, "91V"));
		}

		[TestCase(0)]
		[TestCase(-1)]
		public void ATyreWithoutAColdPressureIsNotAPublishedSpecification(int pressure)
		{
			Assert.Throws<DomainException>(() => specification.PublishTyreSpecification(
				TheRevision, TyrePositionCode.Front, "205/55R16",
				pressure, PressureUnitCode.UnitA, "91V"));
		}

		[Test]
		public void ATyreWithoutRatingsIsNotAPublishedSpecification()
		{
			DomainException error = Assert.Throws<DomainException>(
				() => specification.PublishTyreSpecification(
					TheRevision, TyrePositionCode.Front, "205/55R16",
					2.4m, PressureUnitCode.UnitA, ""));

			Assert.That(error.Message, Does.Not.Contain("L8"),
				"C45: el mensaje dice la regla en palabras. Una excepcion que un programa puede "
				+ "imprimir no puede citar un documento.");
		}

		// Un rechazo es INERTE: la revision se queda sin la llanta y sin contenido, asi que
		// sellarla ahi mismo sigue siendo sellar una vacia.
		[Test]
		public void ARefusedPublicationLeavesTheRevisionEmpty()
		{
			Assert.Throws<DomainException>(() => specification.PublishTyreSpecification(
				TheRevision, TyrePositionCode.Front, "205/55R16",
				0m, PressureUnitCode.UnitA, "91V"));

			Assert.Throws<DomainException>(() => specification.SealRevision(TheRevision));
		}

		// Dentro de UN borrador manda la ultima, igual que en el fluido: corregir antes de sellar
		// es la unica ventana que hay, porque el borrador todavia no contesto nada.
		[Test]
		public void WithinOneDraftTheLastPublicationOfAPositionIsTheOneThatStands()
		{
			specification.PublishTyreSpecification(
				TheRevision, TyrePositionCode.Front, "205/55R16",
				2.4m, PressureUnitCode.UnitA, "91V");
			specification.PublishTyreSpecification(
				TheRevision, TyrePositionCode.Front, "205/55R16",
				2.5m, PressureUnitCode.UnitA, "91V");
			specification.SealRevision(TheRevision);

			Assert.That(TheSentenceTheDoorWouldCompose.TheTyre(
				specification, TyrePositionCode.Front, TheLanguage.OfTheRecord),
				Does.Contain("2.5 bar"));
			Assert.That(TheSentenceTheDoorWouldCompose.TheTyre(
				specification, TyrePositionCode.Front, TheLanguage.OfTheRecord),
				Does.Not.Contain("2.4"));
		}

		// Un borrador con llantas adentro sigue sin contestar (L12). El item es nuevo; el gate no.
		[Test]
		public void ATyrePublishedOnlyIntoADraftIsNotAnswered()
		{
			specification.PublishTyreSpecification(
				TheRevision, TyrePositionCode.Front, "205/55R16",
				2.4m, PressureUnitCode.UnitA, "91V");

			Assert.That(specification.IsTyrePublished(TyrePositionCode.Front), Is.False);
			Assert.That(TheSentenceTheDoorWouldCompose.TheTyre(
				specification, TyrePositionCode.Front, TheLanguage.OfTheRecord),
				Does.Not.Contain("205/55R16"));
		}

		// ENTRE REVISIONES manda la ultima sellada que publico ESA posicion. La revision 2 corrige la
		// presion de adelante y no toca la de atras: un item que la sucesora no republico se sigue
		// contestando desde donde se publico, con SU revision.
		[Test]
		public void ALaterSealedRevisionAnswersOnlyForThePositionItRepublished()
		{
			specification.PublishTyreSpecification(
				TheRevision, TyrePositionCode.Front, "205/55R16",
				2.4m, PressureUnitCode.UnitA, "91V");
			specification.PublishTyreSpecification(
				TheRevision, TyrePositionCode.Rear, "215/50R17",
				2.6m, PressureUnitCode.UnitA, "94W");
			specification.SealRevision(TheRevision);

			specification.OpenRevision(TheNextRevision, PublishedAt.AddDays(30));
			specification.PublishTyreSpecification(
				TheNextRevision, TyrePositionCode.Front, "205/55R16",
				2.5m, PressureUnitCode.UnitA, "91V");
			specification.SealRevision(TheNextRevision);

			Assert.That(specification.ReadTyreRevision(TyrePositionCode.Front),
				Is.EqualTo(TheNextRevision));
			Assert.That(TheSentenceTheDoorWouldCompose.TheTyre(
				specification, TyrePositionCode.Front, TheLanguage.OfTheRecord),
				Does.Contain("2.5 bar"));
			Assert.That(specification.ReadTyreRevision(TyrePositionCode.Rear),
				Is.EqualTo(TheRevision),
				"La revision 2 no hablo de las de atras, asi que esas siguen contestando desde la "
				+ "revision 1. Corregir un item no borra la hoja.");
		}

		private static bool HayAlgunDigito(string frase)
		{
			foreach (char letra in frase) if (char.IsDigit(letra)) return true;
			return false;
		}
	}
}
