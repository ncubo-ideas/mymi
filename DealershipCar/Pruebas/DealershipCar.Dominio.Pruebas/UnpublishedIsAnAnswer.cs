using System;
using System.Linq;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LEY L2 del brief, UnpublishedIsAnAnswer: "un item no publicado contesta 'no publicado'.
	// Nunca cero, nunca un default, nunca un valor prestado de un modelo parecido o de un ano
	// anterior."
	//
	// ES EL TEST 7 DE §11 del brief. Y las tres cosas que la ley prohibe son tres formas
	// distintas de mentir con cara de dato: un cero se lee como una capacidad de cero, una cadena
	// vacia se lee como un grado sin nombre, y el valor de otra clase de fluido se lee como el de
	// esta. Las tres pasan un test que solo mire "no revento".
	//
	// D6 DEL PLAN (§4.3) ES LA OTRA MITAD, Y ES DELIBERADA: aca la ausencia tiene EXACTAMENTE UN
	// TIPO. E47 manda nombrar las CLASES de ausencia cuando hay varias, y este modulo hace lo
	// contrario a proposito -- "nunca se publico" y "se publico solo dentro de un borrador" tienen
	// que ser INDISTINGUIBLES. Distinguirlas seria filtrar que existe una revision sin sellar, que
	// es exactamente lo que L12 esconde; una frase que dijera "esta en preparacion" ya conto lo
	// que no se podia contar. Es el reverso de D3, que si aplica E47 a la respuesta de cobertura,
	// y el par vale mas que cualquiera de las dos mitades sola.
	[TestFixture]
	public sealed class UnpublishedIsAnAnswer
	{
		private const string TheModel = "SM-SEDAN-2025";
		private const string TheRevision = "REV-2025-01";
		private const string TheGrade = "5W-30";

		private static readonly DateTime PublishedAt = new DateTime(2025, 3, 14, 10, 0, 0);

		private ModelSpecification specification;

		[SetUp]
		public void Setup()
		{
			specification = new ModelSpecification(TheModel);
		}

		// TEST 7 DE §11. El modelo tiene una hoja sellada con un aceite y nada mas.
		[Test]
		public void AFluidKindThatWasNeverPublishedAnswersNotPublished()
		{
			ASealedSheetWithTheEngineOilOnly();

			Assert.That(specification.IsFluidPublished(FluidKindCode.BrakeFluid), Is.False);
			Assert.That(specification.ReadFluidRevision(FluidKindCode.BrakeFluid), Is.EqualTo(""),
				"No hay procedencia porque no hay respuesta. Un codigo de revision publicado nunca "
				+ "es vacio -- se guarda al abrirla -- asi que esto no se puede confundir con una.");
		}

		// "Ni cero, ni vacio, ni el valor de otra clase": la frase lo dice CON PALABRAS y no
		// devolviendo un numero que alguien va a leer como un dato.
		[Test]
		public void TheAnswerSaysItInWordsAndNotWithAZeroAnEmptyStringOrAnotherKindsValue()
		{
			ASealedSheetWithTheEngineOilOnly();

			string answer = specification.DescribeFluid(FluidKindCode.BrakeFluid);

			Assert.That(answer, Is.Not.Empty,
				"Una cadena vacia no es 'no publicado': es una respuesta que no dice nada y que la "
				+ "vista va a renderizar como un espacio en blanco.");
			Assert.That(answer, Does.Contain("Líquido de frenos"),
				"La respuesta nombra lo que se pregunto. Si no lo nombrara, la misma frase serviria "
				+ "para los seis fluidos y nadie sabria de cual habla.");
			Assert.That(answer, Does.Not.Contain(TheGrade),
				"Ley L2: nunca un valor prestado de otra clase. Es el peor de los tres, porque es "
				+ "el unico que se lee como una respuesta correcta.");
			Assert.That(answer.Any(char.IsDigit), Is.False,
				"Y ni un numero adentro de la frase: 'capacidad 0' o 'cada 0 dias' se leen como "
				+ "datos publicados, que es la forma en que un default entra sin que nadie lo "
				+ "escriba.");
		}

		// Todas las clases de fluido de un modelo sin ninguna revision contestan, y ninguna
		// revienta. La lectura con clave sobre un conjunto CERRADO es TOTAL: afuera del conjunto
		// no hay miembro, pero adentro la ausencia es una respuesta legal.
		[Test]
		public void AModelWithNoRevisionAtAllStillAnswersEveryFluidKind()
		{
			foreach (FluidKindCode kind in Enum.GetValues(typeof(FluidKindCode)))
			{
				Assert.That(specification.IsFluidPublished(kind), Is.False);
				Assert.That(specification.DescribeFluid(kind), Is.Not.Empty);
				Assert.That(specification.ReadFluidRevision(kind), Is.EqualTo(""));
			}
		}

		// D6: LAS DOS AUSENCIAS SON LA MISMA AUSENCIA. Nunca publicado y publicado solo dentro de
		// un borrador dan la MISMA frase, caracter por caracter. Cualquier diferencia -- una
		// palabra, un matiz, un "todavia" -- delata que hay una revision sin sellar, y eso es
		// justo lo que L12 esconde.
		[Test]
		public void NeverPublishedAndPublishedOnlyIntoADraftAreIndistinguishable()
		{
			ModelSpecification neverPublished = new ModelSpecification(TheModel);

			ModelSpecification onlyInADraft = new ModelSpecification(TheModel);
			onlyInADraft.OpenRevision(TheRevision, PublishedAt);
			onlyInADraft.PublishFluidSpecification(
				TheRevision, FluidKindCode.EngineOil, TheGrade,
				4.6m, VolumeUnitCode.UnitA, 15000, DistanceUnitCode.UnitA, 365);

			Assert.That(onlyInADraft.DescribeFluid(FluidKindCode.EngineOil),
				Is.EqualTo(neverPublished.DescribeFluid(FluidKindCode.EngineOil)),
				"D6: la ausencia tiene UNA sola clase aca, a proposito. Una ausencia distinguible "
				+ "contaria que existe una revision en preparacion, que es lo unico que el gate de "
				+ "sellado existe para no contar.");
			Assert.That(onlyInADraft.IsFluidPublished(FluidKindCode.EngineOil),
				Is.EqualTo(neverPublished.IsFluidPublished(FluidKindCode.EngineOil)));
			Assert.That(onlyInADraft.ReadFluidRevision(FluidKindCode.EngineOil),
				Is.EqualTo(neverPublished.ReadFluidRevision(FluidKindCode.EngineOil)));
		}

		// Y la frase de la ausencia no nombra ninguna revision, ni la del borrador ni ninguna
		// otra: nombrar una seria darle procedencia a lo que no la tiene (L1 al reves).
		[Test]
		public void TheAbsentAnswerNamesNoRevisionAtAll()
		{
			ASealedSheetWithTheEngineOilOnly();

			Assert.That(specification.DescribeFluid(FluidKindCode.BrakeFluid),
				Does.Not.Contain(TheRevision),
				"Una respuesta sin valor no puede llevar procedencia: la procedencia diria que "
				+ "esa revision contesto por los frenos, y no contesto.");
		}

		private void ASealedSheetWithTheEngineOilOnly()
		{
			specification.OpenRevision(TheRevision, PublishedAt);
			specification.PublishFluidSpecification(
				TheRevision, FluidKindCode.EngineOil, TheGrade,
				4.6m, VolumeUnitCode.UnitA, 15000, DistanceUnitCode.UnitA, 365);
			specification.SealRevision(TheRevision);
		}
	}
}
