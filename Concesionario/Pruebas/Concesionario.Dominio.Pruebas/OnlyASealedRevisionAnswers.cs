using System;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LEY L12 del brief, OnlyASealedRevisionAnswers: "una revision que se esta armando es
	// invisible a toda lectura. Una respuesta solo sale de una revision Sealed, asi que una hoja
	// de especificaciones a medio cargar nunca se puede citar."
	//
	// SON LOS TESTS 1 A 4 DE §11 del brief, y el 1 es EL QUE MAS IMPORTA de la rebanada entera.
	// Un borrador con contenido adentro tiene que dejar TODA lectura contestando "no publicado":
	// la mitad de una hoja de especificaciones no es una hoja de especificaciones a medias, es
	// algo que no se puede citar. Si el gate se cayera, el modulo empezaria a contestar con
	// grados de aceite que el fabricante todavia no termino de publicar, firmados con una
	// procedencia que todavia no existe.
	//
	// LA LEY ES ESTRUCTURAL Y NO UNA GUARDA (§3.2 del plan, GAP G3 del brief): el contenido
	// publicado no es campo del agregado, es campo de la revision en la que se publico. Un
	// borrador no esconde su contenido -- lo TIENE, y el agregado no lo alcanza porque el filtro
	// que recorre las revisiones solo mira las selladas. Nadie tiene que acordarse de esconder
	// nada.
	//
	// LOS TESTS 3 Y 4 SON LA MISMA LEY DEL LADO DEL COMANDO, y aca se ejercen POR LA FACHADA:
	// T1 ya los probo sobre PublishedRevision suelta. Lo que se afirma aca es que el agregado
	// resuelve el codigo y DELEGA -- no vuelve a escribir la guarda, que es como dos copias de
	// una regla empiezan a discrepar.
	[TestFixture]
	public sealed class OnlyASealedRevisionAnswers
	{
		private const string TheModel = "YARIS-2019";
		private const string TheRevision = "REV-2025-01";
		private const string TheGrade = "5W-30";

		private static readonly DateTime PublishedAt = new DateTime(2025, 3, 14, 10, 0, 0);

		private ModelSpecification specification;

		[SetUp]
		public void Setup()
		{
			specification = new ModelSpecification(TheModel);
			specification.OpenRevision(TheRevision, PublishedAt);
		}

		// TEST 1 DE §11. El que mas importa.
		[Test]
		public void ADraftWithContentPublishedIntoItStillAnswersNotPublished()
		{
			TheEngineOilOf(TheRevision);

			Assert.That(specification.IsFluidPublished(FluidKindCode.EngineOil), Is.False,
				"Ley L12: una revision que se esta armando es invisible a toda lectura. El "
				+ "contenido entro, y aun asi no hay nada que citar.");
			Assert.That(specification.DescribeFluid(FluidKindCode.EngineOil),
				Does.Not.Contain(TheGrade),
				"La frase no puede filtrar el grado que se publico adentro del borrador: eso seria "
				+ "citar la hoja a medio cargar por la puerta de atras.");
			Assert.That(specification.ReadFluidRevision(FluidKindCode.EngineOil), Is.EqualTo(""),
				"Y no nombra ninguna procedencia, porque no hay ninguna: la revision todavia no "
				+ "contesta.");
		}

		// TEST 2 DE §11. Sellar es lo unico que cambia, y lo cambia todo.
		[Test]
		public void OnceSealedTheSameReadsAnswerAndEachOneNamesTheRevision()
		{
			TheEngineOilOf(TheRevision);

			specification.SealRevision(TheRevision);

			Assert.That(specification.IsFluidPublished(FluidKindCode.EngineOil), Is.True);
			Assert.That(specification.DescribeFluid(FluidKindCode.EngineOil),
				Does.Contain(TheGrade));
			Assert.That(specification.DescribeFluid(FluidKindCode.EngineOil),
				Does.Contain(TheRevision),
				"Ley L1: toda respuesta nombra la revision de la que salio, en la frase misma y no "
				+ "solo en una lectura aparte que alguien puede no hacer.");
			Assert.That(specification.ReadFluidRevision(FluidKindCode.EngineOil),
				Is.EqualTo(TheRevision));
		}

		// TEST 3 DE §11, por la fachada. La guarda vive en PublishedRevision desde T1; lo que se
		// afirma aca es que el agregado la deja pasar hasta ahi en vez de tener una copia.
		[Test]
		public void SealingARevisionWithNothingPublishedIntoItIsRefused()
		{
			DomainException error = Assert.Throws<DomainException>(
				() => specification.SealRevision(TheRevision));

			Assert.That(error.Message, Does.Contain(TheRevision),
				"El rechazo nombra la revision: sin eso, un feed con varias revisiones abiertas no "
				+ "sabe cual rechazo.");
			Assert.That(specification.IsFluidPublished(FluidKindCode.EngineOil), Is.False);
		}

		// TEST 4 DE §11, por la fachada.
		[Test]
		public void PublishingIntoASealedRevisionIsRefused()
		{
			TheEngineOilOf(TheRevision);
			specification.SealRevision(TheRevision);

			DomainException error = Assert.Throws<DomainException>(
				() => specification.PublishFluidSpecification(
					TheRevision, FluidKindCode.BrakeFluid, "DOT 4",
					1.0m, VolumeUnitCode.UnitA, 30000, DistanceUnitCode.UnitA, 730));

			Assert.That(error.Message, Does.Contain("Sealed"));
			Assert.That(specification.IsFluidPublished(FluidKindCode.BrakeFluid), Is.False,
				"Y el rechazo es INERTE: el fluido que se quiso colar no entro, ni al borrador ni a "
				+ "la sellada.");
		}

		// Sellar dos veces por la fachada. Sigue reventando: el agregado no convirtio la guarda de
		// T1 en un no-op idempotente al resolver el codigo.
		[Test]
		public void SealingTwiceThroughTheFacadeIsStillRefused()
		{
			TheEngineOilOf(TheRevision);
			specification.SealRevision(TheRevision);

			DomainException error = Assert.Throws<DomainException>(
				() => specification.SealRevision(TheRevision));

			Assert.That(error.Message, Does.Contain("Sealed"));
			Assert.That(specification.ReadRevisionState(TheRevision), Is.EqualTo("Sealed"));
		}

		// Una revision SUSTITUIDA tampoco contesta. Es la otra mitad del filtro y es la que se
		// olvida: es facil escribir "si no es borrador, contesta" y dejar que una revision
		// apagada siga hablando.
		[Test]
		public void ASupersededRevisionStopsAnswering()
		{
			TheEngineOilOf(TheRevision);
			specification.SealRevision(TheRevision);

			specification.OpenRevision("REV-2025-02", PublishedAt.AddDays(30));
			specification.PublishFluidSpecification(
				"REV-2025-02", FluidKindCode.BrakeFluid, "DOT 4",
				1.0m, VolumeUnitCode.UnitA, 30000, DistanceUnitCode.UnitA, 730);
			specification.SealRevision("REV-2025-02");
			specification.SupersedeRevision(TheRevision, "REV-2025-02");

			Assert.That(specification.IsFluidPublished(FluidKindCode.EngineOil), Is.False,
				"Ley L12: solo una revision SELLADA contesta. Sustituida es terminal, y lo que solo "
				+ "ella publico se retira con ella.");
			Assert.That(specification.IsFluidPublished(FluidKindCode.BrakeFluid), Is.True,
				"Y la sucesora sigue contestando por lo suyo: apagar la vieja no apaga la nueva.");
		}

		private void TheEngineOilOf(string revisionCode)
		{
			specification.PublishFluidSpecification(
				revisionCode, FluidKindCode.EngineOil, TheGrade,
				4.6m, VolumeUnitCode.UnitA, 15000, DistanceUnitCode.UnitA, 365);
		}
	}
}
