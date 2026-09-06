using System;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LEY L4 del brief, AKnownExclusionIsNamed: "una respuesta a la que una exclusion publicada
	// afectaria NOMBRA la exclusion. Callar una exclusion que el concesionario conoce es la peor
	// respuesta posible a una pregunta de cobertura."
	//
	// TEST 11 DE §11 del brief, y el brief dice por que: es el que impide que la app sea
	// TRANQUILIZADORA Y EQUIVOCADA. "Covered" a secas, con una exclusion publicada encima, manda
	// a alguien al taller creyendo una cosa que el fabricante ya dijo por escrito que no.
	//
	// LA CONDICION ES TEXTO PUBLICADO, NO UN PREDICADO (ley L3). Este modulo no decide si la
	// condicion se cumple para este carro: decir que se cumple seria adjudicar un reclamo, que es
	// justo lo que L3 prohibe. Lo que hace es NOMBRARLA -- la exclusion y la condicion bajo la
	// cual muerde -- para que la persona la lea. Una exclusion publicada CONTRA ESE SISTEMA
	// aplica al sistema; que aplique al caso lo decide el fabricante.
	//
	// Y SE NOMBRA JUNTO A CUALQUIER RESPUESTA (departure D7 del plan): tambien con
	// OutsideEnvelope y con NotCovered. L4 gobierna la RESPUESTA, no el miembro -- callar la
	// exclusion porque la respuesta ya era negativa deja a la persona sin saber que hay letra
	// chica publicada sobre ese sistema.
	[TestFixture]
	public sealed class AKnownExclusionIsNamed
	{
		private const string TheModel = "YARIS-2019";
		private const string TheRevision = "REV-2025-01";
		private const string TheSystem = "TRANSMISSION";
		private const string TheExclusion = "damage from towing above the rated weight";
		private const string TheCondition = "when the vehicle has towed above its rated weight";
		private const string ASecondExclusion = "wear of the clutch friction surfaces";
		private const string ASecondCondition = "at any time, as a wear item";

		private static readonly DateTime PublishedAt = new DateTime(2025, 3, 14, 10, 0, 0);
		private static readonly DateTime DeliveredAt = new DateTime(2024, 1, 15, 9, 0, 0);
		private static readonly DateTime AsOf = new DateTime(2024, 7, 15, 9, 0, 0);
		private static readonly DateTime LongAfterTheEnvelope = new DateTime(2034, 7, 15, 9, 0, 0);

		private ModelSpecification specification;

		[SetUp]
		public void Setup()
		{
			specification = new ModelSpecification(TheModel);
			specification.OpenRevision(TheRevision, PublishedAt);
			specification.PublishCoveredSystem(
				TheRevision, TheSystem, 0, 0, DistanceUnitCode.UnitA);
		}

		// TEST 11 DE §11.
		[Test]
		public void ACoveredSystemWithAPublishedExclusionIsCoveredWithExclusionAndNamesIt()
		{
			TheExclusionIsPublished();
			specification.SealRevision(TheRevision);

			Assert.That(TheStandingAt(AsOf, 12000), Is.EqualTo("CoveredWithExclusion"),
				"Ley L9: la respuesta honesta no es 'Covered' ni 'NotCovered', es que hay una "
				+ "exclusion publicada que la persona deberia leer.");

			string sentence = TheSentenceAt(AsOf, 12000);

			Assert.That(sentence, Does.Contain(TheExclusion),
				"Ley L4: la exclusion se NOMBRA. Este es el test que impide que la app sea "
				+ "tranquilizadora y equivocada.");
			Assert.That(sentence, Does.Contain(TheCondition),
				"Y con la condicion bajo la cual muerde: sin ella la persona no puede saber si le "
				+ "toca, y este modulo no lo decide por ella.");
		}

		// Sin exclusion publicada la respuesta es Covered a secas: CoveredWithExclusion no puede
		// ser el estado comodo al que todo cae, o dejaria de decir nada.
		[Test]
		public void TheSameSystemWithNoExclusionPublishedIsSimplyCovered()
		{
			specification.SealRevision(TheRevision);

			Assert.That(TheStandingAt(AsOf, 12000), Is.EqualTo("Covered"));
		}

		// Una exclusion publicada contra OTRO sistema no toca esta respuesta.
		[Test]
		public void AnExclusionPublishedAgainstAnotherSystemDoesNotReachThisOne()
		{
			specification.PublishCoverageExclusion(
				TheRevision, "PAINT", "paintwork is outside the warranty", "from delivery onwards");
			specification.SealRevision(TheRevision);

			Assert.That(TheStandingAt(AsOf, 12000), Is.EqualTo("Covered"));
			Assert.That(TheSentenceAt(AsOf, 12000), Does.Not.Contain("paintwork"),
				"Nombrar una exclusion ajena es tan malo como callar la propia: la persona la leeria "
				+ "como suya.");
		}

		// DEPARTURE D7. Fuera del sobre, la exclusion se sigue nombrando. La respuesta es
		// OutsideEnvelope -- el sobre decide antes -- y la letra chica publicada sigue siendo un
		// hecho que la persona tiene derecho a ver.
		[Test]
		public void AnApplicableExclusionIsNamedAlongsideOutsideEnvelopeToo()
		{
			TheExclusionIsPublished();
			specification.SealRevision(TheRevision);

			Assert.That(TheStandingAt(LongAfterTheEnvelope, 12000), Is.EqualTo("OutsideEnvelope"),
				"Departure D7: el sobre decide primero. Fuera de el no hay cobertura, digan lo que "
				+ "digan las exclusiones.");
			Assert.That(TheSentenceAt(LongAfterTheEnvelope, 12000), Does.Contain(TheExclusion),
				"Y aun asi la exclusion se nombra: L4 gobierna la RESPUESTA, no el miembro.");
		}

		// D7 otra vez, del lado de NotCovered: el sistema esta en el catalogo solo por su
		// exclusion, y la exclusion es justamente lo que hay que leer.
		[Test]
		public void AnExclusionIsNamedAlongsideNotCoveredToo()
		{
			specification.PublishCoverageExclusion(
				TheRevision, "PAINT", "paintwork is outside the warranty", "from delivery onwards");
			specification.SealRevision(TheRevision);

			Assert.That(
				specification.ReadCoverageStanding(
					"PAINT", 36, 100000, DistanceUnitCode.UnitA,
					DeliveredAt, 10, 12000, DistanceUnitCode.UnitA, AsOf),
				Is.EqualTo("NotCovered"));
			Assert.That(
				specification.DescribeCoverage(
					"PAINT", 36, 100000, DistanceUnitCode.UnitA,
					DeliveredAt, 10, 12000, DistanceUnitCode.UnitA, AsOf),
				Does.Contain("paintwork"));
		}

		// Varias exclusiones sobre un mismo sistema se nombran TODAS. Quedarse con la primera
		// seria elegir por la persona cual de las dos le importa.
		[Test]
		public void EveryApplicableExclusionIsNamedAndNotJustTheFirst()
		{
			TheExclusionIsPublished();
			specification.PublishCoverageExclusion(
				TheRevision, TheSystem, ASecondExclusion, ASecondCondition);
			specification.SealRevision(TheRevision);

			string sentence = TheSentenceAt(AsOf, 12000);

			Assert.That(sentence, Does.Contain(TheExclusion));
			Assert.That(sentence, Does.Contain(ASecondExclusion));
		}

		// Dentro de un mismo borrador, republicar la MISMA exclusion la corrige en vez de
		// duplicarla -- es la decision 5 de T2 aplicada a este item. Y publicar otra distinta
		// sobre el mismo sistema la agrega: corregir no es reemplazar el conjunto.
		[Test]
		public void WithinOneDraftRepublishingAnExclusionCorrectsItInsteadOfDuplicatingIt()
		{
			TheExclusionIsPublished();
			specification.PublishCoverageExclusion(
				TheRevision, TheSystem, TheExclusion, "when the towed weight exceeds 1500 UnitA");
			specification.SealRevision(TheRevision);

			string sentence = TheSentenceAt(AsOf, 12000);

			Assert.That(sentence, Does.Contain("1500 UnitA"),
				"La ultima publicacion de una exclusion dentro de un borrador es la que queda: es "
				+ "la unica ventana de correccion que hay, porque despues de sellar no hay ninguna.");
			Assert.That(sentence, Does.Not.Contain(TheCondition),
				"Y la version corregida reemplaza a la anterior en vez de quedar las dos, que se "
				+ "leerian como dos exclusiones distintas sobre lo mismo.");
		}

		// La lista de exclusiones publicadas sale entera por su propia lectura, con el texto: una
		// lista de codigos de sistema no diria nada de que esta excluido.
		[Test]
		public void ThePublishedExclusionsAreListedWithWhatTheyExclude()
		{
			TheExclusionIsPublished();
			specification.SealRevision(TheRevision);

			Assert.That(specification.CoverageExclusions().Length, Is.EqualTo(1));
			Assert.That(specification.CoverageExclusions()[0], Does.Contain(TheSystem));
			Assert.That(specification.CoverageExclusions()[0], Does.Contain(TheExclusion));
			Assert.That(specification.CoverageExclusions()[0], Does.Contain(TheCondition));
		}

		private void TheExclusionIsPublished()
		{
			specification.PublishCoverageExclusion(
				TheRevision, TheSystem, TheExclusion, TheCondition);
		}

		private string TheStandingAt(DateTime asOf, int currentReading)
		{
			return specification.ReadCoverageStanding(
				TheSystem, 36, 100000, DistanceUnitCode.UnitA,
				DeliveredAt, 10, currentReading, DistanceUnitCode.UnitA, asOf);
		}

		private string TheSentenceAt(DateTime asOf, int currentReading)
		{
			return specification.DescribeCoverage(
				TheSystem, 36, 100000, DistanceUnitCode.UnitA,
				DeliveredAt, 10, currentReading, DistanceUnitCode.UnitA, asOf);
		}
	}
}
