using System;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LEY L10 del brief, TheVocabularyIsTheManufacturers: "los codigos de sistema son de afuera:
	// una cadena con catalogo, nunca un tipo cerrado."
	//
	// SON LOS TESTS 13 Y 14 DE §11 del brief, y la distancia entre los dos es el punto entero de
	// la ley L9. Un sistema que el alcance publicado NOMBRA y no cubre contesta NotCovered. Un
	// codigo que el catalogo no vio nunca contesta NotPublished. "No nos lo dijeron" y "no lo
	// cubre" son dos respuestas distintas, y una app que las colapsa le dice a alguien que su
	// pantalla no esta cubierta cuando lo unico cierto es que el fabricante no publico nada sobre
	// ella.
	//
	// EL CATALOGO ES LA UNION (departure D5 del plan): los codigos de los sistemas cubiertos MAS
	// los codigos que nombran las exclusiones publicadas. Sin la union, un sistema que aparece
	// SOLO como exclusion se leeria NotPublished -- o sea, el fabricante lo excluyo expresamente
	// y la app contestaria que no le dijeron nada. Por eso el test de la exclusion sola esta
	// aca y no en el de L4.
	//
	// Y LA CONSULTA ES TOTAL (polaridad decidida en T2, decision 1). Un codigo de sistema no es
	// miembro de un conjunto cerrado ni identidad de un registro: es un vocabulario ajeno. Asi
	// que un codigo desconocido NO revienta y NO contesta NotCovered: contesta que no esta
	// publicado, que es lo unico que este modulo sabe de el.
	[TestFixture]
	public sealed class TheVocabularyIsTheManufacturers
	{
		private const string TheModel = "YARIS-2019";
		private const string TheRevision = "REV-2025-01";
		private const string TheCoveredSystem = "ENG-BLOCK";
		private const string TheSystemOnlyExcluded = "PAINT";
		private const string ASystemNobodyPublished = "INFOTAINMENT";

		private static readonly DateTime PublishedAt = new DateTime(2025, 3, 14, 10, 0, 0);
		private static readonly DateTime DeliveredAt = new DateTime(2025, 4, 1, 9, 0, 0);
		private static readonly DateTime AsOf = new DateTime(2025, 6, 1, 9, 0, 0);

		private ModelSpecification specification;

		[SetUp]
		public void Setup()
		{
			specification = new ModelSpecification(TheModel);
			specification.OpenRevision(TheRevision, PublishedAt);
			specification.PublishCoveredSystem(
				TheRevision, TheCoveredSystem, 0, 0, DistanceUnitCode.UnitA);
			specification.PublishCoverageExclusion(
				TheRevision, TheSystemOnlyExcluded,
				"paintwork and exterior trim are outside the warranty",
				"from delivery onwards");
			specification.SealRevision(TheRevision);
		}

		[Test]
		public void ACodePublishedAsACoveredSystemIsInTheVocabulary()
		{
			Assert.That(specification.TheVocabularyIsTheManufacturers(TheCoveredSystem), Is.True);
		}

		// LA MITAD DE D5 QUE SE OLVIDA. El codigo entra al catalogo por la exclusion, aunque
		// ninguna publicacion lo haya nombrado como sistema cubierto.
		[Test]
		public void ACodeNamedOnlyByAnExclusionIsStillInTheVocabulary()
		{
			Assert.That(specification.TheVocabularyIsTheManufacturers(TheSystemOnlyExcluded),
				Is.True,
				"Departure D5: el catalogo es la UNION de los sistemas cubiertos y los codigos que "
				+ "nombran las exclusiones. Sin la union, un sistema que el fabricante excluyo "
				+ "expresamente contestaria que no nos dijeron nada de el.");
		}

		[Test]
		public void ACodeNobodyPublishedIsNotInTheVocabulary()
		{
			Assert.That(specification.TheVocabularyIsTheManufacturers(ASystemNobodyPublished),
				Is.False);
		}

		// TEST 13 DE §11. El alcance lo nombra y no lo cubre.
		[Test]
		public void ASystemTheScopeNamesOnlyAmongItsExclusionsIsNotCovered()
		{
			Assert.That(TheStandingOf(TheSystemOnlyExcluded), Is.EqualTo("NotCovered"));
		}

		// TEST 14 DE §11. "La diferencia entre 'no nos lo dijeron' y 'no esta cubierto' es el
		// punto entero de L9."
		[Test]
		public void ACodeTheCatalogueHasNeverSeenIsNotPublishedAndNeverNotCovered()
		{
			Assert.That(TheStandingOf(ASystemNobodyPublished), Is.EqualTo("NotPublished"),
				"Ley L9: un codigo que el catalogo no vio nunca no es un sistema sin cobertura, es "
				+ "un sistema del que nadie publico nada.");
			Assert.That(TheStandingOf(ASystemNobodyPublished), Is.Not.EqualTo("NotCovered"));
		}

		// Y LA FRASE LO DICE CON PALABRAS, no solo el miembro: quien lea la respuesta tiene que
		// poder ver que la diferencia es de donde viene el silencio.
		[Test]
		public void TheSentenceForAnUnknownCodeSaysNothingWasPublishedAboutIt()
		{
			string sentence = TheSentenceFor(ASystemNobodyPublished);

			Assert.That(sentence, Does.Contain(ASystemNobodyPublished));
			Assert.That(sentence, Does.Contain("Sin publicar"));
			Assert.That(sentence, Does.Not.Contain("NotCovered"));
		}

		// La consulta es TOTAL: un codigo que nadie publico no es un defecto de quien llama. Es
		// literalmente lo que la persona va a escribir cuando se equivoque de nombre, y reventar
		// ahi convertiria una pregunta razonable en un error del protocolo.
		[Test]
		public void AnUnknownCodeIsAnAnswerAndNotACallerBug()
		{
			Assert.DoesNotThrow(() => specification.TheVocabularyIsTheManufacturers("NO-SUCH"));
			Assert.DoesNotThrow(() => TheStandingOf("NO-SUCH"));
			Assert.DoesNotThrow(() => TheSentenceFor("NO-SUCH"));
		}

		// Un codigo en blanco es un codigo desconocido, no una llamada mal hecha: la consulta
		// total toma el primitivo tal cual y trata lo vacio como la respuesta vacia (E41).
		[Test]
		public void ABlankCodeIsSimplyAnUnknownCode()
		{
			Assert.That(specification.TheVocabularyIsTheManufacturers("   "), Is.False);
			Assert.That(TheStandingOf("   "), Is.EqualTo("NotPublished"));
		}

		// El codigo se compara ORDINAL y sin recortar, como el registro de revisiones (T2,
		// decision 7): el vocabulario es ajeno y este modulo no le arregla la ortografia a nadie.
		[Test]
		public void TheCodeIsComparedExactlyAsItWasPublished()
		{
			Assert.That(specification.TheVocabularyIsTheManufacturers("eng-block"), Is.False,
				"Ley L10: el vocabulario es del fabricante. Adivinar que 'eng-block' quiso decir "
				+ "'ENG-BLOCK' es este modulo inventando un hecho del fabricante.");
		}

		// Sin nada sellado no hay catalogo, y entonces TODO codigo -- hasta uno que un borrador
		// nombra -- contesta que no esta publicado. Es L12 sobre el camino de cobertura.
		[Test]
		public void WithNothingSealedEveryCodeIsNotPublished()
		{
			ModelSpecification draftOnly = new ModelSpecification(TheModel);
			draftOnly.OpenRevision(TheRevision, PublishedAt);
			draftOnly.PublishCoveredSystem(
				TheRevision, TheCoveredSystem, 0, 0, DistanceUnitCode.UnitA);

			Assert.That(draftOnly.TheVocabularyIsTheManufacturers(TheCoveredSystem), Is.False,
				"Ley L12: una revision que se esta armando es invisible a toda lectura, y el "
				+ "catalogo de codigos es una lectura como cualquier otra.");
			Assert.That(
				draftOnly.ReadCoverageStanding(
					TheCoveredSystem, 36, 100000, DistanceUnitCode.UnitA,
					DeliveredAt, 10, 20000, DistanceUnitCode.UnitA, AsOf),
				Is.EqualTo("NotPublished"));
		}

		private string TheStandingOf(string systemCode)
		{
			return specification.ReadCoverageStanding(
				systemCode, 36, 100000, DistanceUnitCode.UnitA,
				DeliveredAt, 10, 20000, DistanceUnitCode.UnitA, AsOf);
		}

		private string TheSentenceFor(string systemCode)
		{
			return TheSentenceTheDoorWouldCompose.TheCoverage(
				specification, systemCode, 36, 100000, DistanceUnitCode.UnitA,
				DeliveredAt, 10, 20000, DistanceUnitCode.UnitA, AsOf, TheLanguage.OfTheRecord);
		}
	}
}
