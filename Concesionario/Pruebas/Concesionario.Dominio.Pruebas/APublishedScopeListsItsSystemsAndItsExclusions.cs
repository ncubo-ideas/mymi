using System;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// EL RESUMEN DEL ALCANCE -- la pregunta Q6 de §9 del brief: "los sistemas cubiertos y las
	// exclusiones, con la revision". Es la lectura de la que sale la lista que la persona usa
	// para ELEGIR un sistema: §3 del brief dice que la pregunta de cobertura toma un sistema que
	// el dueno elige de la lista publicada, nunca un sintoma que describe.
	//
	// LAS TRES LEYES QUE SE CRUZAN ACA:
	//
	//   L12  el alcance de un borrador no se lista. Es la misma ley que el fluido de T2, y se
	//        repite en este item porque la cobertura acumula DENTRO de la revision en vez de
	//        estar indexada por un codigo, y una acumulacion es justo donde es facil olvidarse
	//        del filtro.
	//   L1   el resumen nombra la revision de la que salio, en la frase y en su lectura aparte.
	//   D4   el alcance contesta desde la ULTIMA revision sellada que publico alcance. Una
	//        revision que solo corrige un fluido no borra el alcance; una que republica alcance
	//        lo reemplaza entero, porque el alcance es UN item y no una coleccion de items.
	//
	// LAS DOS LISTAS SALEN DISTINTAS A PROPOSITO. Los sistemas cubiertos salen como CODIGOS --
	// son el vocabulario del que la persona elige. Las exclusiones salen con su TEXTO: una lista
	// de codigos de sistema no diria que esta excluido ni bajo que condicion, que es justamente
	// lo que L4 existe para que no se calle.
	[TestFixture]
	public sealed class APublishedScopeListsItsSystemsAndItsExclusions
	{
		private const string TheModel = "YARIS-2019";
		private const string TheRevision = "REV-2025-01";
		private const string TheNextRevision = "REV-2025-02";

		private static readonly DateTime PublishedAt = new DateTime(2025, 3, 14, 10, 0, 0);
		private static readonly DateTime LaterOn = new DateTime(2025, 9, 14, 10, 0, 0);

		private ModelSpecification specification;

		[SetUp]
		public void Setup()
		{
			specification = new ModelSpecification(TheModel);
			specification.OpenRevision(TheRevision, PublishedAt);
			specification.PublishCoveredSystem(
				TheRevision, "ENG-BLOCK", 0, 0, DistanceUnitCode.UnitA);
			specification.PublishCoveredSystem(
				TheRevision, "HV-BATTERY", 96, 160000, DistanceUnitCode.UnitA);
			specification.PublishCoverageExclusion(
				TheRevision, "PAINT", "paintwork and exterior trim are outside the warranty",
				"from delivery onwards");
		}

		// L12 sobre este item: el alcance de un borrador no se lista ni se resume.
		[Test]
		public void ADraftScopeIsNotListedAndNotSummarised()
		{
			Assert.That(specification.IsCoveragePublished(), Is.False);
			Assert.That(specification.CoveredSystems(), Is.Empty);
			Assert.That(specification.CoverageExclusions(), Is.Empty);
			Assert.That(specification.ReadCoverageRevision(), Is.EqualTo(""));
			Assert.That(specification.DescribeCoverageSummary(), Does.Not.Contain("ENG-BLOCK"),
				"Ley L12: una hoja a medio cargar no se puede citar, y el resumen del alcance es "
				+ "una cita como cualquier otra.");
		}

		[Test]
		public void OnceSealedTheScopeListsItsSystemsAsCodes()
		{
			specification.SealRevision(TheRevision);

			Assert.That(specification.IsCoveragePublished(), Is.True);
			Assert.That(specification.CoveredSystems(),
				Is.EqualTo(new[] { "ENG-BLOCK", "HV-BATTERY" }),
				"Salen los CODIGOS, en el orden en que se publicaron: es la lista de la que la "
				+ "persona elige, y §3 del brief dice que elige de la lista y nunca describe un "
				+ "sintoma.");
		}

		[Test]
		public void OnceSealedTheScopeListsItsExclusionsWithTheirText()
		{
			specification.SealRevision(TheRevision);

			Assert.That(specification.CoverageExclusions().Length, Is.EqualTo(1));
			Assert.That(specification.CoverageExclusions()[0], Does.Contain("paintwork"));
		}

		// L1: el resumen nombra su revision en la frase Y tiene su lectura de procedencia.
		[Test]
		public void TheSummaryNamesTheRevisionItCameFrom()
		{
			specification.SealRevision(TheRevision);

			Assert.That(specification.DescribeCoverageSummary(), Does.Contain("ENG-BLOCK"));
			Assert.That(specification.DescribeCoverageSummary(), Does.Contain("paintwork"));
			Assert.That(specification.DescribeCoverageSummary(), Does.Contain(TheRevision));
			Assert.That(specification.ReadCoverageRevision(), Is.EqualTo(TheRevision));
		}

		// Y la respuesta por sistema nombra la MISMA revision: el alcance es un item, asi que las
		// dos frases salen de la misma publicacion o una de las dos esta mintiendo.
		[Test]
		public void TheStandingNamesTheSameRevisionAsTheSummary()
		{
			specification.SealRevision(TheRevision);

			Assert.That(
				specification.DescribeCoverage(
					"ENG-BLOCK", 36, 100000, DistanceUnitCode.UnitA,
					PublishedAt, 10, 12000, DistanceUnitCode.UnitA, LaterOn),
				Does.Contain(TheRevision));
		}

		// Publicar alcance en una revision sellada se rechaza, y el rechazo es INERTE: lo que se
		// quiso colar no entro.
		[Test]
		public void PublishingScopeIntoASealedRevisionIsRefused()
		{
			specification.SealRevision(TheRevision);

			Assert.Throws<DomainException>(
				() => specification.PublishCoveredSystem(
					TheRevision, "GEARBOX", 0, 0, DistanceUnitCode.UnitA));
			Assert.Throws<DomainException>(
				() => specification.PublishCoverageExclusion(
					TheRevision, "GEARBOX", "excluded", "always"));

			Assert.That(specification.TheVocabularyIsTheManufacturers("GEARBOX"), Is.False);
		}

		// D4: una revision que solo corrige OTRO item no borra el alcance.
		[Test]
		public void ARevisionThatRepublishesSomethingElseDoesNotBlankTheScope()
		{
			specification.SealRevision(TheRevision);

			specification.OpenRevision(TheNextRevision, LaterOn);
			specification.PublishFluidSpecification(
				TheNextRevision, FluidKindCode.EngineOil, "5W-30",
				4.6m, VolumeUnitCode.UnitA, 15000, DistanceUnitCode.UnitA, 365);
			specification.SealRevision(TheNextRevision);

			Assert.That(specification.IsCoveragePublished(), Is.True);
			Assert.That(specification.ReadCoverageRevision(), Is.EqualTo(TheRevision),
				"El alcance sigue contestando desde la revision que lo publico: una revision no "
				+ "alcanza hacia atras ni borra lo que no republico.");
		}

		// D4 del otro lado: una revision que SI republica alcance lo reemplaza entero. El alcance
		// es un item, no una coleccion que se acumula entre revisiones -- si se acumulara, un
		// sistema retirado del alcance seguiria contestando para siempre.
		[Test]
		public void ARevisionThatRepublishesTheScopeReplacesItWhole()
		{
			specification.SealRevision(TheRevision);

			specification.OpenRevision(TheNextRevision, LaterOn);
			specification.PublishCoveredSystem(
				TheNextRevision, "GEARBOX", 0, 0, DistanceUnitCode.UnitA);
			specification.SealRevision(TheNextRevision);

			Assert.That(specification.CoveredSystems(), Is.EqualTo(new[] { "GEARBOX" }));
			Assert.That(specification.TheVocabularyIsTheManufacturers("ENG-BLOCK"), Is.False,
				"El alcance de la revision nueva es el alcance. Acumular entre revisiones dejaria a "
				+ "un sistema retirado contestando desde una publicacion que ya nadie sostiene.");
			Assert.That(specification.ReadCoverageRevision(), Is.EqualTo(TheNextRevision));
		}

		// Sin nada sellado: listas vacias, procedencia vacia, y la frase lo dice con palabras.
		[Test]
		public void WithNothingPublishedTheScopeSaysSoInWords()
		{
			ModelSpecification empty = new ModelSpecification(TheModel);

			Assert.That(empty.IsCoveragePublished(), Is.False);
			Assert.That(empty.CoveredSystems(), Is.Empty);
			Assert.That(empty.CoverageExclusions(), Is.Empty);
			Assert.That(empty.ReadCoverageRevision(), Is.EqualTo(""));
			Assert.That(empty.DescribeCoverageSummary(), Is.Not.Empty);
			Assert.That(empty.DescribeCoverageSummary(), Does.Contain("no se ha publicado nada"));
		}

		// Un alcance que solo publico exclusiones es un alcance: el catalogo lo conoce y el
		// resumen lo dice. Exigir un sistema cubierto para que exista alcance dejaria a las
		// exclusiones sin donde vivir.
		[Test]
		public void AScopeWithOnlyExclusionsIsStillAPublishedScope()
		{
			ModelSpecification onlyExclusions = new ModelSpecification(TheModel);
			onlyExclusions.OpenRevision(TheRevision, PublishedAt);
			onlyExclusions.PublishCoverageExclusion(
				TheRevision, "PAINT", "paintwork is outside the warranty", "from delivery onwards");
			onlyExclusions.SealRevision(TheRevision);

			Assert.That(onlyExclusions.IsCoveragePublished(), Is.True);
			Assert.That(onlyExclusions.CoveredSystems(), Is.Empty);
			Assert.That(onlyExclusions.CoverageExclusions().Length, Is.EqualTo(1));
		}

		// Y publicar alcance le da CONTENIDO a la revision: sellar deja de estar prohibido, sin
		// que haga falta publicar tambien un fluido. Sin esto, una revision de pura garantia no
		// se podria sellar y el modulo no contestaria nunca sobre cobertura.
		[Test]
		public void PublishingScopeAloneIsEnoughContentToSealARevision()
		{
			ModelSpecification onlyScope = new ModelSpecification(TheModel);
			onlyScope.OpenRevision(TheRevision, PublishedAt);
			onlyScope.PublishCoveredSystem(TheRevision, "ENG-BLOCK", 0, 0, DistanceUnitCode.UnitA);

			Assert.DoesNotThrow(() => onlyScope.SealRevision(TheRevision));
		}

		// Dentro de un mismo borrador, republicar un sistema lo corrige en vez de duplicarlo --
		// decision 5 de T2 aplicada a este item.
		[Test]
		public void WithinOneDraftRepublishingASystemCorrectsItInsteadOfDuplicatingIt()
		{
			specification.PublishCoveredSystem(
				TheRevision, "HV-BATTERY", 120, 200000, DistanceUnitCode.UnitA);
			specification.SealRevision(TheRevision);

			Assert.That(specification.CoveredSystems(),
				Is.EqualTo(new[] { "ENG-BLOCK", "HV-BATTERY" }));
			Assert.That(specification.DescribeCoverageSummary(), Does.Contain("120 meses"));
			Assert.That(specification.DescribeCoverageSummary(), Does.Not.Contain("96 meses"));
		}

		// Publicar un sistema sin codigo, o una exclusion sin texto, es un defecto de quien
		// publica: un item que no dice de que habla no se puede citar ni elegir.
		[Test]
		public void AnItemThatDoesNotSayWhatItIsAboutIsRefused()
		{
			Assert.Throws<DomainException>(
				() => specification.PublishCoveredSystem(
					TheRevision, "   ", 0, 0, DistanceUnitCode.UnitA));
			Assert.Throws<DomainException>(
				() => specification.PublishCoverageExclusion(
					TheRevision, "PAINT", "   ", "from delivery onwards"));
			Assert.Throws<DomainException>(
				() => specification.PublishCoverageExclusion(
					TheRevision, "PAINT", "paintwork is outside the warranty", "   "));
		}
	}
}
