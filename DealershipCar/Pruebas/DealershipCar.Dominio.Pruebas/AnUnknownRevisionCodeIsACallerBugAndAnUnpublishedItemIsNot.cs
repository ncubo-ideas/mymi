using System;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LA POLARIDAD DE LAS LECTURAS DE ESTE AGREGADO, decidida y escrita en vez de heredada por
	// costumbre. E41 tiene dos reglas opuestas y el agregado usa las dos, con clave en la mano
	// las dos veces:
	//
	//   CLAVE SOBRE UN CONJUNTO CERRADO -> TOTAL. Una clase de fluido que nadie publico no es un
	//   error de nadie: es la ausencia, y la ausencia es una respuesta legal (L2). Contesta.
	//
	//   CLAVE QUE ES UN ID EN UN REGISTRO -> REVIENTA. Un codigo de revision no es un punto de un
	//   rango: nombra una entrada que existe o no existe. Contestar "false" o "" ahi MENTIRIA --
	//   confundiria "esa revision no existe" con un estado real de una revision que si existe. La
	//   clausula de registro de E41 dice justo esto, y E32 dice por que: es un defecto de quien
	//   llama.
	//
	// ABRIR UNA REVISION CON UN CODIGO QUE YA EXISTE ES LA MISMA PREGUNTA DEL OTRO LADO, y se
	// decide igual: revienta. Reabrir un codigo tomado o lo pisa -- y entonces una respuesta que
	// nombro esa revision pasa a nombrar otra cosa, que es L11 al reves -- o lo ignora en
	// silencio, y el feed cree que publico donde no publico. Las dos son peores que el rechazo.
	//
	// LO QUE EL AGREGADO AGREGA ES ESTO Y NADA MAS: resolver el codigo, y la polaridad del "no
	// existe". Las transiciones y sus guardas son de PublishedRevision desde T1 y no se
	// reescriben aca -- dos copias de una regla es como empiezan a discrepar.
	[TestFixture]
	public sealed class AnUnknownRevisionCodeIsACallerBugAndAnUnpublishedItemIsNot
	{
		private const string TheModel = "YARIS-2019";
		private const string TheRevision = "REV-2025-01";
		private const string TheRevisionNobodyOpened = "REV-QUE-NADIE-ABRIO";

		private static readonly DateTime PublishedAt = new DateTime(2025, 3, 14, 10, 0, 0);

		private ModelSpecification specification;

		[SetUp]
		public void Setup()
		{
			specification = new ModelSpecification(TheModel);
			specification.OpenRevision(TheRevision, PublishedAt);
		}

		[Test]
		public void PublishingIntoARevisionNobodyOpenedIsACallerBug()
		{
			DomainException error = Assert.Throws<DomainException>(
				() => specification.PublishFluidSpecification(
					TheRevisionNobodyOpened, FluidKindCode.EngineOil, "5W-30",
					4.6m, VolumeUnitCode.UnitA, 15000, DistanceUnitCode.UnitA, 365));

			Assert.That(error.Message, Does.Contain(TheRevisionNobodyOpened),
				"El rechazo nombra el codigo que no encontro: es lo unico con lo que quien publica "
				+ "puede encontrar el error en su feed.");
		}

		// El mensaje tiene que decir que NO ENCONTRO el codigo. Sin eso la prueba pasaria igual si
		// el registro fabricara una revision en blanco al vuelo: sellar una vacia revienta
		// tambien, por otro motivo, y nadie notaria la diferencia.
		[Test]
		public void SealingARevisionNobodyOpenedIsACallerBug()
		{
			DomainException error = Assert.Throws<DomainException>(
				() => specification.SealRevision(TheRevisionNobodyOpened));

			Assert.That(error.Message, Does.Contain(TheRevisionNobodyOpened));
			Assert.That(error.Message, Does.Contain("no tiene ninguna revision"));
		}

		[Test]
		public void SupersedingARevisionNobodyOpenedIsACallerBug()
		{
			DomainException error = Assert.Throws<DomainException>(
				() => specification.SupersedeRevision(TheRevisionNobodyOpened, TheRevision));

			Assert.That(error.Message, Does.Contain(TheRevisionNobodyOpened));
			Assert.That(error.Message, Does.Contain("no tiene ninguna revision"));
		}

		// Y tambien del otro lado: sustituir POR una revision que nadie abrio. Es el mismo
		// registro y el mismo defecto, y si este pasara la revision quedaria apagada nombrando un
		// sitio donde no hay nada.
		[Test]
		public void SupersedingByARevisionNobodyOpenedIsACallerBugToo()
		{
			DomainException error = Assert.Throws<DomainException>(
				() => specification.SupersedeRevision(TheRevision, TheRevisionNobodyOpened));

			Assert.That(error.Message, Does.Contain(TheRevisionNobodyOpened));
			Assert.That(error.Message, Does.Contain("no tiene ninguna revision"));
		}

		[Test]
		public void ReadingTheStateOfARevisionNobodyOpenedIsACallerBug()
		{
			DomainException error = Assert.Throws<DomainException>(
				() => specification.ReadRevisionState(TheRevisionNobodyOpened));

			Assert.That(error.Message, Does.Contain(TheRevisionNobodyOpened),
				"Contestar 'Draft' o cadena vacia aca seria inventar una revision. Un id que el "
				+ "registro no tiene no es un estado, es una pregunta mal hecha.");
		}

		[Test]
		public void OpeningARevisionWithACodeThatIsAlreadyTakenIsACallerBug()
		{
			DomainException error = Assert.Throws<DomainException>(
				() => specification.OpenRevision(TheRevision, PublishedAt.AddDays(30)));

			Assert.That(error.Message, Does.Contain(TheRevision));
		}

		// Y el rechazo es INERTE: la revision que ya estaba abierta no se piso ni se re-fecho.
		[Test]
		public void ARefusedOpeningLeavesTheRevisionThatWasAlreadyThereUntouched()
		{
			specification.PublishFluidSpecification(
				TheRevision, FluidKindCode.EngineOil, "5W-30",
				4.6m, VolumeUnitCode.UnitA, 15000, DistanceUnitCode.UnitA, 365);
			specification.SealRevision(TheRevision);

			Assert.Throws<DomainException>(
				() => specification.OpenRevision(TheRevision, PublishedAt.AddDays(30)));

			Assert.That(specification.ReadRevisionState(TheRevision), Is.EqualTo("Sealed"),
				"Si reabrir pisara, una revision sellada volveria a borrador por la puerta de atras "
				+ "-- que es la transicion que §10 del brief NO tiene.");
			Assert.That(specification.ReadFluidRevision(FluidKindCode.EngineOil),
				Is.EqualTo(TheRevision));
		}

		// EL OTRO LADO DE LA POLARIDAD, en el mismo archivo a proposito: la misma especificacion,
		// una clave que es miembro de un conjunto cerrado, y la respuesta es la ausencia. No
		// revienta y no tiene por que: afuera de un rango no hay miembro, pero adentro de un
		// conjunto cerrado la ausencia es un hecho del negocio.
		[Test]
		public void AFluidKindThatNobodyPublishedIsNotACallerBug()
		{
			Assert.DoesNotThrow(() => specification.DescribeFluid(FluidKindCode.Coolant));
			Assert.That(specification.IsFluidPublished(FluidKindCode.Coolant), Is.False);
		}

		// Pero un CODIGO que no nombra ningun miembro del conjunto cerrado si revienta, y esa es
		// la tercera polaridad de E41: un conjunto cerrado no tiene miembro afuera, asi que "no
		// existe" no es una respuesta vacia sino un defecto de quien llama. La resuelve el
		// resolutor del conjunto, una sola vez al entrar.
		[Test]
		public void AFluidCodeThatNamesNoMemberIsACallerBug()
		{
			Assert.Throws<DomainException>(
				() => specification.DescribeFluid((FluidKindCode)99));
			Assert.Throws<DomainException>(
				() => specification.IsFluidPublished((FluidKindCode)99));
			Assert.Throws<DomainException>(
				() => specification.ReadFluidRevision((FluidKindCode)99));
		}

		// Un codigo de revision en blanco no llega a ser un id: no se puede nombrar despues, asi
		// que ninguna respuesta podria decir de donde salio (L1). Lo rechaza la revision misma
		// desde T1, y la fachada la deja rechazar.
		[Test]
		public void ARevisionCodeInBlankIsRefusedAtTheDoorOfTheRegistry()
		{
			Assert.Throws<DomainException>(() => specification.OpenRevision(null, PublishedAt));
			Assert.Throws<DomainException>(() => specification.OpenRevision("", PublishedAt));
			Assert.Throws<DomainException>(() => specification.OpenRevision("   ", PublishedAt));
		}
	}
}
