using System;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LEY L9 de SLICE-TheMaintenanceScreen.md: "una fila de precio lleva la revision de la que salio,
	// como ya la llevan las filas de fluido, llanta y regla de servicio. Un precio sin su procedencia
	// no se puede distinguir de uno viejo."
	//
	// LA PROCEDENCIA SALE DOS VECES y las dos son la misma: la columna Revision, que es la mitad
	// direccionable, y la frase, que ya la trae adentro porque APublishedSpecification.Describe la
	// pega. No es redundancia: quien lee la columna puede volver a preguntar por esa revision, y
	// quien lee la frase la tiene delante sin preguntar aparte.
	//
	// Y LA REVISION DE LA FILA ES LA QUE RIGE, no la que se estaba recorriendo. La fila se compone de
	// la RESPUESTA del resolvedor, que es quien elige la ultima revision sellada que publico esa
	// clave; que las dos coincidan en todo estado alcanzable es una consecuencia del recorrido de mas
	// nueva a mas vieja, y no una segunda forma de decidirlo.
	[TestFixture]
	public sealed class EveryRowNamesItsRevision
	{
		private const string TheModel = "SM-SEDAN-2025";
		private const string TheFirstRevision = "REV-2025-01";
		private const string TheSecondRevision = "REV-2025-02";

		private static readonly DateTime PublishedAt = new DateTime(2025, 3, 14, 10, 0, 0);
		private static readonly DateTime ValidFrom = new DateTime(2025, 1, 1, 0, 0, 0);
		private static readonly DateTime ValidUntil = new DateTime(2025, 12, 31, 0, 0, 0);
		private static readonly DateTime WhileItIsInForce = new DateTime(2025, 6, 15, 0, 0, 0);

		private ModelSpecification specification;

		[SetUp]
		public void Setup()
		{
			specification = new ModelSpecification(TheModel);
			specification.OpenRevision(TheFirstRevision, PublishedAt);
			specification.PublishServicePrice(
				TheFirstRevision, OperationKindCode.Inspection, 10000, DistanceUnitCode.UnitA,
				120.50m, "CRC", ValidFrom, ValidUntil);
			specification.SealRevision(TheFirstRevision);
		}

		[Test]
		public void EveryRowNamesTheRevisionItCameFrom()
		{
			Assert.That(
				specification.PublishedPriceRevisionsAsOf(WhileItIsInForce),
				Is.EqualTo(new[] { TheFirstRevision }));
		}

		// La columna y la frase dicen la MISMA revision. Afirmar solo una dejaria pasar una fila cuya
		// columna nombra una revision y cuya frase nombra otra, que es la unica forma en que esta ley
		// se puede romper sin que nada quede vacio.
		[Test]
		public void TheSentenceNamesTheSameRevisionAsTheColumn()
		{
			string[] revisions = specification.PublishedPriceRevisionsAsOf(WhileItIsInForce);
			string[] said = specification.DescribeEachPublishedPriceAsOf(WhileItIsInForce);

			for (int row = 0; row < revisions.Length; row++)
				Assert.That(said[row], Does.Contain("Publicado en la revisión " + revisions[row] + "."),
					"La fila " + row + " nombra una revision en la columna y otra en la frase.");
		}

		// NINGUNA COLUMNA DE PROCEDENCIA VIENE EN BLANCO. Una revision publicada nunca tiene el codigo
		// en blanco, asi que el vacio es exactamente la firma de una ausencia -- y una fila que salio
		// de una ausencia no es una fila.
		[Test]
		public void NoRowComesBackWithABlankRevision()
		{
			specification.OpenRevision(TheSecondRevision, PublishedAt.AddDays(30));
			specification.PublishServicePrice(
				TheSecondRevision, OperationKindCode.FluidChange, 20000, DistanceUnitCode.UnitA,
				340.00m, "CRC", ValidFrom, ValidUntil);
			specification.SealRevision(TheSecondRevision);

			Assert.That(
				specification.PublishedPriceRevisionsAsOf(WhileItIsInForce),
				Has.All.Not.Empty);
			Assert.That(
				specification.PublishedPriceRevisionsAsOf(WhileItIsInForce),
				Is.EquivalentTo(new[] { TheFirstRevision, TheSecondRevision }),
				"Una revision que corrige un item no borra los que no republico, asi que las dos "
				+ "revisiones contestan, cada una por su clave.");
		}

		// Y LA FILA SE NIEGA A NACER DE UNA AUSENCIA. Es la guarda que hace estructural el resto de la
		// ley: si la enumeracion llegara a inventarse una clave que nadie publico, el resolvedor
		// contestaria "no publicado" y la fila no se podria construir en vez de salir con la columna
		// de procedencia vacia.
		[Test]
		public void ARowRefusesToBeBuiltFromAnUnpublishedAnswer()
		{
			SpecificationAnswer absent = specification.ARevisionAppliesForward(
				OperationKindCode.Repair,
				new PublishedMilestone(90000, DistanceUnit.UnitA),
				WhileItIsInForce);

			Assert.That(absent.IsPublished(), Is.False);

			DomainException error = Assert.Throws<DomainException>(
				() => APublishedPriceRow.Of(
					APublishedPriceKey.Of(
						MaintenanceOperationKind.Repair,
						new PublishedMilestone(90000, DistanceUnit.UnitA)),
					absent));

			Assert.That(error.Message, Does.Contain("revision"),
				"El mensaje nombra la procedencia que falta, no otra cosa que tambien revienta, y lo "
				+ "dice sin acentos como todo mensaje de DomainException.");
		}
	}
}
