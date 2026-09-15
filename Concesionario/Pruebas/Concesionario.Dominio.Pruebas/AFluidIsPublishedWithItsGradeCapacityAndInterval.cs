using System;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// EL PRIMER ITEM PUBLICADO DE LA REBANADA, y esta aca por una razon de tarea: UN item alcanza
	// para probar el gate de sellado, la procedencia y la ausencia. Los otros cinco -- llantas,
	// combustible, cobertura, precios, politica de servicio -- llegan en T3 por el mismo camino.
	//
	// QUE ES UNA ESPECIFICACION DE FLUIDO, segun el canon §4 del brief: de que fluido habla, con
	// que grado, cuanta capacidad y en que unidad, y cada cuanto se cambia. Las cuatro juntas o no
	// es una especificacion: media especificacion publicada es exactamente lo que L12 existe para
	// que nadie pueda citar, y dejarla entrar por el constructor seria colarla por debajo del
	// gate.
	//
	// LA CAPACIDAD Y EL INTERVALO LLEVAN SU UNIDAD Y SE CONTESTAN EN ESA UNIDAD. La ley L8 --
	// UnitsAreNeverConverted -- se nombra y se prueba entera en T3, con el test 9 de §11 y la
	// comparacion cruzada; lo que se afirma aca es lo minimo que ya se puede afirmar: la unidad en
	// que se publico es la que sale, y ningun tipo de esta tarea tiene por donde convertirla.
	//
	// PUBLICAR DOS VECES LA MISMA CLASE DENTRO DE UN BORRADOR: manda la ultima. §10 del brief dice
	// "el contenido se acumula" y no dice nada de repetidos. Se decide asi y no reventando porque
	// un borrador se esta ARMANDO y todavia no contesto nada -- corregir antes de sellar es la
	// unica ventana que hay para corregir, ya que despues de sellar no se puede. Y no alcanza
	// hacia atras, porque nadie recibio nunca una respuesta de un borrador.
	[TestFixture]
	public sealed class AFluidIsPublishedWithItsGradeCapacityAndInterval
	{
		private const string TheModel = "YARIS-2019";
		private const string TheRevision = "REV-2025-01";

		private static readonly DateTime PublishedAt = new DateTime(2025, 3, 14, 10, 0, 0);

		private ModelSpecification specification;

		[SetUp]
		public void Setup()
		{
			specification = new ModelSpecification(TheModel);
			specification.OpenRevision(TheRevision, PublishedAt);
		}

		// La frase entera, clavada. Es lo que el asistente y la pantalla repiten tal cual, asi que
		// vale mas fijarla y tener que actualizarla que dejarla derivar sin que nadie se entere.
		[Test]
		public void TheSentenceSaysTheKindTheGradeTheCapacityAndTheIntervalAndThenItsRevision()
		{
			specification.PublishFluidSpecification(
				TheRevision, FluidKindCode.EngineOil, "5W-30",
				4.6m, VolumeUnitCode.UnitA, 15000, DistanceUnitCode.UnitA, 365);
			specification.SealRevision(TheRevision);

			Assert.That(
				TheSentenceTheDoorWouldCompose.TheFluid(
					specification, FluidKindCode.EngineOil, TheLanguage.OfTheRecord),
				Is.EqualTo(
				"Aceite de motor: grado 5W-30, capacidad 4.6 L, se cambia cada 15000 km o 365 "
				+ "días. Publicado en la revisión REV-2025-01."));
		}

		// Un intervalo puede tener UNA sola mitad: "cada 15000" sin plazo, o "cada 365 dias" sin
		// distancia. Las dos son intervalos publicados de verdad y las dos se dicen sin inventar
		// la mitad que falta -- inventar un cero ahi seria "cada 0 dias", que se lee como vencido
		// siempre.
		[Test]
		public void AnIntervalWithOnlyADistanceSaysOnlyTheDistance()
		{
			specification.PublishFluidSpecification(
				TheRevision, FluidKindCode.Coolant, "G12",
				6.0m, VolumeUnitCode.UnitA, 60000, DistanceUnitCode.UnitA, 0);
			specification.SealRevision(TheRevision);

			Assert.That(TheSentenceTheDoorWouldCompose.TheFluid(
				specification, FluidKindCode.Coolant, TheLanguage.OfTheRecord),
				Does.Contain("se cambia cada 60000 km."));
			Assert.That(TheSentenceTheDoorWouldCompose.TheFluid(
				specification, FluidKindCode.Coolant, TheLanguage.OfTheRecord),
				Does.Not.Contain("days"));
		}

		[Test]
		public void AnIntervalWithOnlyATermSaysOnlyTheTerm()
		{
			specification.PublishFluidSpecification(
				TheRevision, FluidKindCode.BrakeFluid, "DOT 4",
				1.0m, VolumeUnitCode.UnitA, 0, DistanceUnitCode.UnitA, 730);
			specification.SealRevision(TheRevision);

			Assert.That(TheSentenceTheDoorWouldCompose.TheFluid(
				specification, FluidKindCode.BrakeFluid, TheLanguage.OfTheRecord),
				Does.Contain("se cambia cada 730 días."));
		}

		// La unidad en que se publico es la que sale. Ni se normaliza ni se traduce: dos valores
		// en unidades distintas no son comparables, y el dia que alguien convierta, la ley L8 se
		// queda sin nada que suspender.
		[Test]
		public void TheCapacityIsAnsweredInTheUnitItWasPublishedIn()
		{
			specification.PublishFluidSpecification(
				TheRevision, FluidKindCode.TransmissionFluid, "ATF",
				7.5m, VolumeUnitCode.UnitB, 90000, DistanceUnitCode.UnitB, 0);
			specification.SealRevision(TheRevision);

			string answer = TheSentenceTheDoorWouldCompose.TheFluid(
				specification, FluidKindCode.TransmissionFluid, TheLanguage.OfTheRecord);

			Assert.That(answer, Does.Contain("capacidad 7.5 gal"));
			Assert.That(answer, Does.Contain("cada 90000 mi"));
		}

		[Test]
		public void AFluidWithoutAGradeIsNotAPublishedSpecification()
		{
			Assert.Throws<DomainException>(() => specification.PublishFluidSpecification(
				TheRevision, FluidKindCode.EngineOil, "   ",
				4.6m, VolumeUnitCode.UnitA, 15000, DistanceUnitCode.UnitA, 365));
		}

		[TestCase(0)]
		[TestCase(-1)]
		public void AFluidWithoutACapacityIsNotAPublishedSpecification(int capacity)
		{
			Assert.Throws<DomainException>(() => specification.PublishFluidSpecification(
				TheRevision, FluidKindCode.EngineOil, "5W-30",
				capacity, VolumeUnitCode.UnitA, 15000, DistanceUnitCode.UnitA, 365));
		}

		// Un intervalo que no es ni una distancia ni un plazo no es un intervalo. Publicarlo
		// dejaria una hoja que dice "se cambia cada nada", y sellarla la haria citable.
		[Test]
		public void AnIntervalThatIsNeitherADistanceNorATermIsRefused()
		{
			DomainException error = Assert.Throws<DomainException>(
				() => specification.PublishFluidSpecification(
					TheRevision, FluidKindCode.EngineOil, "5W-30",
					4.6m, VolumeUnitCode.UnitA, 0, DistanceUnitCode.UnitA, 0));

			Assert.That(error.Message, Does.Not.Contain("L8"),
				"C45: el mensaje dice la regla en palabras. Una excepcion que un programa puede "
				+ "imprimir no puede citar un documento.");
		}

		[TestCase(-1, 365)]
		[TestCase(15000, -1)]
		public void AnIntervalWithANegativeHalfIsRefused(int distance, int days)
		{
			Assert.Throws<DomainException>(() => specification.PublishFluidSpecification(
				TheRevision, FluidKindCode.EngineOil, "5W-30",
				4.6m, VolumeUnitCode.UnitA, distance, DistanceUnitCode.UnitA, days));
		}

		// Y un rechazo cualquiera de los de arriba es INERTE: la revision se queda sin ese fluido
		// y sin haber ganado contenido, asi que sellarla ahi mismo sigue siendo sellar una vacia.
		[Test]
		public void ARefusedPublicationLeavesTheRevisionEmpty()
		{
			Assert.Throws<DomainException>(() => specification.PublishFluidSpecification(
				TheRevision, FluidKindCode.EngineOil, "",
				4.6m, VolumeUnitCode.UnitA, 15000, DistanceUnitCode.UnitA, 365));

			Assert.Throws<DomainException>(() => specification.SealRevision(TheRevision));
		}

		// Dentro de UN borrador, la ultima publicacion de una clase es la que queda. Es una
		// correccion antes de sellar, no una revision alcanzando hacia atras: el borrador nunca
		// contesto nada.
		[Test]
		public void WithinOneDraftTheLastPublicationOfAKindIsTheOneThatStands()
		{
			specification.PublishFluidSpecification(
				TheRevision, FluidKindCode.EngineOil, "5W-30",
				4.6m, VolumeUnitCode.UnitA, 15000, DistanceUnitCode.UnitA, 365);
			specification.PublishFluidSpecification(
				TheRevision, FluidKindCode.EngineOil, "0W-20",
				4.6m, VolumeUnitCode.UnitA, 15000, DistanceUnitCode.UnitA, 365);
			specification.SealRevision(TheRevision);

			Assert.That(TheSentenceTheDoorWouldCompose.TheFluid(
				specification, FluidKindCode.EngineOil, TheLanguage.OfTheRecord),
				Does.Contain("0W-20"));
			Assert.That(TheSentenceTheDoorWouldCompose.TheFluid(
				specification, FluidKindCode.EngineOil, TheLanguage.OfTheRecord),
				Does.Not.Contain("5W-30"));
		}

		// Las seis clases del canon se publican y se contestan por separado: la respuesta de una
		// no es la de otra, que es lo que la ley L2 llama "un valor prestado".
		[Test]
		public void EachFluidKindKeepsItsOwnSpecification()
		{
			specification.PublishFluidSpecification(
				TheRevision, FluidKindCode.EngineOil, "5W-30",
				4.6m, VolumeUnitCode.UnitA, 15000, DistanceUnitCode.UnitA, 365);
			specification.PublishFluidSpecification(
				TheRevision, FluidKindCode.WasherFluid, "Winter mix",
				3.0m, VolumeUnitCode.UnitA, 0, DistanceUnitCode.UnitA, 180);
			specification.SealRevision(TheRevision);

			Assert.That(TheSentenceTheDoorWouldCompose.TheFluid(
				specification, FluidKindCode.EngineOil, TheLanguage.OfTheRecord),
				Does.Contain("5W-30"));
			Assert.That(TheSentenceTheDoorWouldCompose.TheFluid(
				specification, FluidKindCode.WasherFluid, TheLanguage.OfTheRecord),
				Does.Contain("Winter mix"));
			Assert.That(TheSentenceTheDoorWouldCompose.TheFluid(
				specification, FluidKindCode.WasherFluid, TheLanguage.OfTheRecord),
				Does.Not.Contain("5W-30"));
		}
	}
}
