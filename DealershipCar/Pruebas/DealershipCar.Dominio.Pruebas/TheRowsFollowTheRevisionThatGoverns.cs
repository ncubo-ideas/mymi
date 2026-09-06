using System;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// ES EL TEST 15 DE §11: "una revision sustituida por otra que publica un hito distinto -> las
	// filas siguen a la revision que rige, y no se ve ni un hito rancio ni una tarjeta vacia."
	//
	// LAS DOS MITADES SON UNA SOLA PRUEBA. Que el hito viejo desaparezca es facil de conseguir por el
	// camino equivocado -- devolviendo nada --, y que nada este vacio es facil de conseguir por el
	// otro -- devolviendo todo. Afirmar las dos juntas es lo unico que separa las dos formas de
	// romperla.
	//
	// Y SUSTITUIR NO ES LO MISMO QUE PUBLICAR OTRA REVISION. Una revision sellada al lado de otra
	// sellada deja las dos contestando, cada una por las claves que la otra no republico; una
	// SUSTITUIDA deja de estar sellada y se apaga entera. Es la ley que el agregado ya tenia, y la
	// enumeracion la hereda por recorrer las mismas revisiones con el mismo filtro.
	[TestFixture]
	public sealed class TheRowsFollowTheRevisionThatGoverns
	{
		private const string TheModel = "YARIS-2019";
		private const string TheOldRevision = "REV-2025-01";
		private const string TheNewRevision = "REV-2025-02";

		private static readonly DateTime PublishedAt = new DateTime(2025, 3, 14, 10, 0, 0);
		private static readonly DateTime ValidFrom = new DateTime(2025, 1, 1, 0, 0, 0);
		private static readonly DateTime ValidUntil = new DateTime(2025, 12, 31, 0, 0, 0);
		private static readonly DateTime WhileItIsInForce = new DateTime(2025, 6, 15, 0, 0, 0);

		private ModelSpecification specification;

		// La revision vieja publica el precio a los 10.000 y queda sellada. Cada prueba decide que le
		// pasa despues.
		[SetUp]
		public void Setup()
		{
			specification = new ModelSpecification(TheModel);
			specification.OpenRevision(TheOldRevision, PublishedAt);
			specification.PublishServicePrice(
				TheOldRevision, OperationKindCode.Inspection, 10000, DistanceUnitCode.UnitA,
				120.50m, "CRC", ValidFrom, ValidUntil);
			specification.SealRevision(TheOldRevision);
		}

		// TEST 15 DE §11. La sucesora publica el MISMO servicio en OTRO hito y sustituye: el hito
		// viejo se fue, el nuevo esta, y la fila nombra la revision nueva.
		[Test]
		public void ASupersedingRevisionWithADifferentMilestoneLeavesNothingStaleAndNothingEmpty()
		{
			TheNewRevisionPublishesAt(15000);
			specification.SupersedeRevision(TheOldRevision, TheNewRevision);

			Assert.That(
				specification.PublishedPriceMilestonesAsOf(WhileItIsInForce),
				Is.EqualTo(new[] { "15000 km" }),
				"El hito de la revision apagada no puede seguir en la lista: quien lo lea estaria "
				+ "citando un precio que la sucursal ya reemplazo.");
			Assert.That(specification.HasAnyPublishedServicePrice(), Is.True,
				"Y no puede quedar vacio: la sucursal SI publico un precio, en otro hito.");
			Assert.That(
				specification.PublishedPriceRevisionsAsOf(WhileItIsInForce),
				Is.EqualTo(new[] { TheNewRevision }));
			Assert.That(
				specification.DescribeEachPublishedPriceAsOf(WhileItIsInForce),
				Has.All.Contains("₡145.00"));
		}

		// LA OTRA MITAD, y es la que separa sustituir de publicar: dos revisiones SELLADAS dejan las
		// dos claves en la lista, porque una revision que corrige un item no borra los que no
		// republico. Sin esta prueba la anterior pasaria con una enumeracion que solo mira la ultima
		// revision, que es lo que el brief dice y el agregado no hace.
		[Test]
		public void TwoSealedRevisionsBothAnswerForTheKeysTheOtherDidNotRepublish()
		{
			TheNewRevisionPublishesAt(15000);

			Assert.That(
				specification.PublishedPriceMilestonesAsOf(WhileItIsInForce),
				Is.EquivalentTo(new[] { "15000 km", "10000 km" }));
			Assert.That(specification.PublishedServicePriceCount(), Is.EqualTo(2));
		}

		// Y EL ORDEN ES DE LA REVISION MAS NUEVA A LA MAS VIEJA. Recorrer al reves dejaria la lista
		// abriendo con el precio del ano pasado, y no lo veria ninguna otra prueba: las filas serian
		// las mismas y cada una seguiria siendo correcta.
		[Test]
		public void TheNewestRevisionsRowsComeFirst()
		{
			TheNewRevisionPublishesAt(15000);

			Assert.That(
				specification.PublishedPriceMilestonesAsOf(WhileItIsInForce),
				Is.EqualTo(new[] { "15000 km", "10000 km" }));
		}

		// UN PRECIO REPUBLICADO SALE UNA VEZ, y nombra la revision nueva. Sin la pregunta de "ya se
		// emitio" la lista traeria la misma clave dos veces, las dos filas identicas -- porque las
		// dos las compone el resolvedor --, y el conteo diria dos donde el fabricante publico un
		// precio.
		[Test]
		public void APriceRepublishedByALaterRevisionAppearsOnceAndNamesTheLaterOne()
		{
			TheNewRevisionPublishesAt(10000);

			Assert.That(specification.PublishedServicePriceCount(), Is.EqualTo(1));
			Assert.That(
				specification.PublishedPriceMilestonesAsOf(WhileItIsInForce),
				Is.EqualTo(new[] { "10000 km" }));
			Assert.That(
				specification.PublishedPriceRevisionsAsOf(WhileItIsInForce),
				Is.EqualTo(new[] { TheNewRevision }));
			Assert.That(
				specification.DescribeEachPublishedPriceAsOf(WhileItIsInForce),
				Has.All.Contains("₡145.00"));
		}

		// SUSTITUIR RETIRA lo que la sucesora no volvio a publicar, y aca eso deja la lista vacia. La
		// ausencia se pregunta -- HasAny contesta que no -- y no se rellena con la fila de la
		// revision apagada.
		[Test]
		public void SupersedingWithARevisionThatPublishesNoPriceLeavesNothingToEnumerate()
		{
			specification.OpenRevision(TheNewRevision, PublishedAt.AddDays(30));
			specification.PublishServiceInterval(
				TheNewRevision, OperationKindCode.Inspection, 10000, DistanceUnitCode.UnitA, 365);
			specification.SealRevision(TheNewRevision);
			specification.SupersedeRevision(TheOldRevision, TheNewRevision);

			Assert.That(specification.HasAnyPublishedServicePrice(), Is.False);
			Assert.That(specification.PublishedServicePriceCount(), Is.EqualTo(0));
			Assert.That(specification.PublishedPriceMilestonesAsOf(WhileItIsInForce), Is.Empty);
		}

		private void TheNewRevisionPublishesAt(int milestone)
		{
			specification.OpenRevision(TheNewRevision, PublishedAt.AddDays(30));
			specification.PublishServicePrice(
				TheNewRevision, OperationKindCode.Inspection, milestone, DistanceUnitCode.UnitA,
				145.00m, "CRC", ValidFrom, ValidUntil);
			specification.SealRevision(TheNewRevision);
		}
	}
}
