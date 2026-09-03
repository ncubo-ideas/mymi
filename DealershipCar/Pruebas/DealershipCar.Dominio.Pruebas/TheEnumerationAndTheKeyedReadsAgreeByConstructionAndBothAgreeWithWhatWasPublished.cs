using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// E49: LA CONCORDANCIA ENTRE DOS CONSUMIDORES DE UN RESOLVEDOR NO ES EVIDENCIA SOBRE EL
	// RESOLVEDOR. La enumeracion y las tres lecturas llaveadas pasan las dos por
	// ARevisionAppliesForward(operacion, hito, instante), asi que concuerdan POR CONSTRUCCION --
	// incluido el caso en que el resolvedor esta uniformemente equivocado.
	//
	// OBLIGACION 1, estructural: hay UN cuerpo que contesta el precio en un instante, y la
	// sobrecarga que recibe el codigo del wire lo envuelve. Se afirma por la via que la reflexion
	// alcanza -- que los dos resolvedores privados de part C sigan privados, censado en
	// APriceIsFoundByEnumerationAndNotByGuessing -- y por comportamiento en varios estados, aca.
	//
	// OBLIGACION 2, la que se salta: algo tiene que anclar UN lado a lo que se RECIBIO. Los montos,
	// la moneda, la vigencia y el codigo de revision de estas pruebas se afirman POR LITERAL contra
	// lo que se le paso a PublishServicePrice, no contra la otra respuesta. Reproducido y no
	// heredado: borrar la comparacion de vigencia dentro de APriceCarriesItsValidity(asOf) deja las
	// dos mitades en perfecto acuerdo sobre una frase que ninguna de las dos deberia estar diciendo,
	// y solo los literales lo ven.
	[TestFixture]
	public sealed class TheEnumerationAndTheKeyedReadsAgreeByConstructionAndBothAgreeWithWhatWasPublished
	{
		private const string TheModel = "SM-SEDAN-2025";
		private const string TheFirstRevision = "REV-2025-01";
		private const string TheSecondRevision = "REV-2025-02";

		private static readonly DateTime PublishedAt = new DateTime(2025, 3, 14, 10, 0, 0);
		private static readonly DateTime ValidFrom = new DateTime(2025, 1, 1, 0, 0, 0);
		private static readonly DateTime ValidUntil = new DateTime(2025, 12, 31, 0, 0, 0);
		private static readonly DateTime TheFluidPriceEndsOn = new DateTime(2025, 6, 30, 0, 0, 0);

		private static readonly DateTime WhileBothAreInForce = new DateTime(2025, 6, 15, 0, 0, 0);
		private static readonly DateTime AfterTheFluidPriceLapsed = new DateTime(2025, 9, 1, 0, 0, 0);

		private ModelSpecification specification;

		// DOS revisiones selladas y TRES claves, para que el estado no sea el facil: la segunda
		// revision republica el precio de la revision y publica uno nuevo, asi que la clave que rige
		// no es la primera de la lista de ninguna de las dos.
		[SetUp]
		public void Setup()
		{
			specification = new ModelSpecification(TheModel);

			specification.OpenRevision(TheFirstRevision, PublishedAt);
			specification.PublishServicePrice(
				TheFirstRevision, OperationKindCode.Inspection, 10000, DistanceUnitCode.UnitA,
				120.50m, "CRC", ValidFrom, ValidUntil);
			specification.PublishServicePrice(
				TheFirstRevision, OperationKindCode.FluidChange, 20000, DistanceUnitCode.UnitA,
				340.00m, "CRC", ValidFrom, TheFluidPriceEndsOn);
			specification.SealRevision(TheFirstRevision);

			specification.OpenRevision(TheSecondRevision, PublishedAt.AddDays(30));
			specification.PublishServicePrice(
				TheSecondRevision, OperationKindCode.Inspection, 10000, DistanceUnitCode.UnitA,
				145.00m, "CRC", ValidFrom, ValidUntil);
			specification.PublishServicePrice(
				TheSecondRevision, OperationKindCode.Repair, 30000, DistanceUnitCode.UnitB,
				900.00m, "USD", ValidFrom, ValidUntil);
			specification.SealRevision(TheSecondRevision);
		}

		// LA MITAD ANCLADA A LO RECIBIDO, y es la que mide. Cada fila se afirma por LITERAL contra los
		// argumentos que se le pasaron a PublishServicePrice: el monto, la moneda, la vigencia, el
		// veredicto de vigencia en el instante preguntado y el codigo de la revision que rige.
		[Test]
		public void EveryRowSaysWhatWasPublishedByLiteral()
		{
			string[] milestones = specification.PublishedPriceMilestonesAsOf(AfterTheFluidPriceLapsed);
			string[] said = specification.DescribeEachPublishedPriceAsOf(AfterTheFluidPriceLapsed);
			string[] revisions = specification.PublishedPriceRevisionsAsOf(AfterTheFluidPriceLapsed);

			int inspection = Array.IndexOf(milestones, "10000 km");
			int fluid = Array.IndexOf(milestones, "20000 km");
			int repair = Array.IndexOf(milestones, "30000 mi");

			Assert.That(said[inspection], Is.EqualTo(
				"Revisión a los 10000 km: 145.00 CRC, vigente del 2025-01-01 al 2025-12-31. "
				+ "Al 2025-09-01 esa vigencia está rigiendo. "
				+ "Publicado en la revisión REV-2025-02."));
			Assert.That(revisions[inspection], Is.EqualTo(TheSecondRevision));

			Assert.That(said[fluid], Is.EqualTo(
				"Cambio de fluido a los 20000 km: 340.00 CRC, vigente del 2025-01-01 al 2025-06-30. "
				+ "Al 2025-09-01 esa vigencia ya pasó, así que este precio no es el vigente. "
				+ "Publicado en la revisión REV-2025-01."));
			Assert.That(revisions[fluid], Is.EqualTo(TheFirstRevision));

			Assert.That(said[repair], Is.EqualTo(
				"Reparación a los 30000 mi: 900.00 USD, vigente del 2025-01-01 al 2025-12-31. "
				+ "Al 2025-09-01 esa vigencia está rigiendo. "
				+ "Publicado en la revisión REV-2025-02."));
			Assert.That(revisions[repair], Is.EqualTo(TheSecondRevision));
		}

		// Y LA MITAD QUE CONCUERDA POR CONSTRUCCION, dicha como lo que es. Se afirma en DOS instantes
		// -- uno con todo vigente y otro con una vigencia pasada -- porque una comparacion vale lo que
		// vale el momento en que se toma, y con una sola vigencia en curso las tres claves contestan
		// la misma clausula.
		[TestCase(0)]
		[TestCase(1)]
		public void TheEnumerationAndTheKeyedReadsSayTheSameThingForEveryKey(int which)
		{
			DateTime asOf = which == 0 ? WhileBothAreInForce : AfterTheFluidPriceLapsed;

			string[] operations = specification.PublishedPriceOperationsAsOf(asOf);
			string[] milestones = specification.PublishedPriceMilestonesAsOf(asOf);
			string[] said = specification.DescribeEachPublishedPriceAsOf(asOf);
			string[] revisions = specification.PublishedPriceRevisionsAsOf(asOf);

			for (int row = 0; row < operations.Length; row++)
			{
				OperationKindCode kind = TheCodeNamed(operations[row]);
				int distance = TheDistanceOf(milestones[row]);
				DistanceUnitCode unit = TheUnitOf(milestones[row]);

				Assert.That(specification.IsServicePricePublished(kind, distance, unit), Is.True,
					"La enumeracion trajo una clave que la lectura llaveada no reconoce.");
				Assert.That(
					specification.DescribeServicePrice(kind, distance, unit, asOf),
					Is.EqualTo(said[row]));
				Assert.That(
					specification.ReadServicePriceRevision(kind, distance, unit),
					Is.EqualTo(revisions[row]));
			}
		}

		// TEST 16 DE §11: las tres lecturas que ya existian contestan EXACTAMENTE como antes, por sus
		// nombres y sus firmas de hoy. Es la unica prueba de esta tarea que mira hacia atras, y lo que
		// defiende es que el cuerpo del resolvedor se movio detras de una envoltura sin cambiar lo que
		// contesta.
		[Test]
		public void TheThreeExistingPriceReadsAnswerExactlyAsBefore()
		{
			Assert.That(
				specification.IsServicePricePublished(
					OperationKindCode.Inspection, 10000, DistanceUnitCode.UnitA),
				Is.True);
			Assert.That(
				specification.IsServicePricePublished(
					OperationKindCode.Inspection, 10000, DistanceUnitCode.UnitB),
				Is.False,
				"El mismo numero en la otra unidad sigue siendo otro hito.");
			Assert.That(
				specification.IsServicePricePublished(
					OperationKindCode.Inspection, 0, DistanceUnitCode.UnitA),
				Is.False,
				"Y un hito que no es una distancia positiva sigue contestando sin reventar.");

			Assert.That(
				specification.DescribeServicePrice(
					OperationKindCode.Inspection, 10000, DistanceUnitCode.UnitA, WhileBothAreInForce),
				Is.EqualTo(
					"Revisión a los 10000 km: 145.00 CRC, vigente del 2025-01-01 al 2025-12-31. "
					+ "Al 2025-06-15 esa vigencia está rigiendo. "
					+ "Publicado en la revisión REV-2025-02."));
			Assert.That(
				specification.DescribeServicePrice(
					OperationKindCode.Repair, 10000, DistanceUnitCode.UnitA, WhileBothAreInForce),
				Does.Contain("no se ha publicado nada para este modelo"));

			Assert.That(
				specification.ReadServicePriceRevision(
					OperationKindCode.FluidChange, 20000, DistanceUnitCode.UnitA),
				Is.EqualTo(TheFirstRevision));
			Assert.That(
				specification.ReadServicePriceRevision(
					OperationKindCode.FluidChange, 90000, DistanceUnitCode.UnitA),
				Is.EqualTo(""));
		}

		// OBLIGACION 1 POR ESTRUCTURA, por la via que la reflexion alcanza: hay UNA sola sobrecarga de
		// ARevisionAppliesForward que recibe un instante y contesta por un hito, y es la que recibe la
		// clase de operacion resuelta. La que recibe el codigo del wire existe para las tres lecturas
		// llaveadas y no puede tener cuerpo propio sin que este censo diga cuantas hay.
		[Test]
		public void OneMemberResolvesAPriceAtAnInstantAndTheWireOverloadWrapsIt()
		{
			MethodInfo[] resolvers = typeof(ModelSpecification)
				.GetMethods(BindingFlags.Instance | BindingFlags.Public
					| BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
				.Where(member => member.Name == "ARevisionAppliesForward")
				.Where(member => member.GetParameters()
					.Any(each => each.ParameterType == typeof(PublishedMilestone)))
				.ToArray();

			Assert.That(resolvers, Has.Length.EqualTo(3),
				"Se esperan tres: la sin instante, la del wire con instante y la del hito resuelto "
				+ "con instante. Una cuarta es un segundo camino al precio.");

			MethodInfo[] withAnInstant = resolvers
				.Where(member => member.GetParameters()
					.Any(each => each.ParameterType == typeof(DateTime)))
				.ToArray();

			Assert.That(withAnInstant, Has.Length.EqualTo(2));
			Assert.That(
				withAnInstant.Count(member => member.GetParameters()[0].ParameterType
					== typeof(MaintenanceOperationKind)),
				Is.EqualTo(1),
				"Exactamente una recibe la clase resuelta: es el cuerpo, y la otra la envuelve.");
		}

		private static OperationKindCode TheCodeNamed(string name)
		{
			return (OperationKindCode)Enum.Parse(typeof(OperationKindCode), name);
		}

		private static int TheDistanceOf(string milestone)
		{
			return int.Parse(milestone.Split(' ')[0]);
		}

		private static DistanceUnitCode TheUnitOf(string milestone)
		{
			return milestone.Split(' ')[1] == "km" ? DistanceUnitCode.UnitA : DistanceUnitCode.UnitB;
		}
	}
}
