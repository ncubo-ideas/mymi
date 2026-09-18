using System;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LEY L5 del brief, TheMoreSpecificLimitGoverns: "donde un sistema lleva sus propios limites,
	// esos rigen, no el sobre del vehiculo. Lo mas especifico gana."
	//
	// SON LOS TESTS 10 Y 12 DE §11 del brief. El 12 es el que muerde: un sistema con limites
	// propios mas cortos, un vehiculo pasado de esos y todavia dentro del sobre general. La
	// respuesta correcta es OutsideEnvelope POR LOS LIMITES DEL SISTEMA. Si ganara el sobre, la
	// app le diria a alguien que su bateria de traccion sigue cubierta a los cinco anos porque el
	// vehiculo entero tiene siete, que es exactamente la letra chica que el fabricante publico
	// para que no pase.
	//
	// Y LOS LIMITES QUE RIGEN SE NOMBRAN EN LA RESPUESTA (test 10). Decir "Covered" sin decir
	// contra que se midio deja a la persona sin nada que verificar: la respuesta seria una
	// afirmacion en vez de una lectura.
	//
	// LAS DOS MITADES SE MIDEN POR SEPARADO, y basta que UNA se pase. Un sistema con limite de
	// meses y sin mitad de distancia no se puede pasar por kilometraje, y al reves: la mitad que
	// no se publico no decide nada, y un cero ahi se leeria como un limite cumplido desde
	// siempre.
	[TestFixture]
	public sealed class TheMoreSpecificLimitGoverns
	{
		private const string TheModel = "YARIS-2019";
		private const string TheRevision = "REV-2025-01";
		private const string TheSystem = "HV-BATTERY";

		private static readonly DateTime PublishedAt = new DateTime(2025, 3, 14, 10, 0, 0);
		private static readonly DateTime DeliveredAt = new DateTime(2020, 1, 15, 9, 0, 0);
		private static readonly DateTime FiveYearsLater = new DateTime(2025, 1, 15, 9, 0, 0);

		private ModelSpecification specification;

		[SetUp]
		public void Setup()
		{
			specification = new ModelSpecification(TheModel);
			specification.OpenRevision(TheRevision, PublishedAt);
		}

		// TEST 10 DE §11. Dentro del alcance, dentro del sobre, sin exclusiones.
		[Test]
		public void ASystemInsideTheScopeAndInsideTheEnvelopeIsCoveredWithItsLimitsNamed()
		{
			ASystemWithNoLimitsOfItsOwn();

			Assert.That(TheStandingAt(FiveYearsLater, 40000), Is.EqualTo("Covered"));
			Assert.That(TheSentenceAt(FiveYearsLater, 40000), Does.Contain("84 meses"),
				"Test 10 de §11: la respuesta nombra los limites que rigen. Un 'Covered' sin decir "
				+ "contra que se midio no se puede verificar.");
			Assert.That(TheSentenceAt(FiveYearsLater, 40000), Does.Contain("160.000 km"));
		}

		// TEST 12 DE §11. Pasado de los limites propios, dentro del sobre del vehiculo.
		[Test]
		public void PastTheSystemsOwnLimitsIsOutsideTheEnvelopeEvenInsideTheVehiclesOwn()
		{
			ASystemWithItsOwnLimitsOf(months: 36, distance: 60000);

			Assert.That(TheStandingAt(FiveYearsLater, 40000), Is.EqualTo("OutsideEnvelope"),
				"Ley L5: lo mas especifico gana. Sesenta meses de vehiculo no alargan los treinta y "
				+ "seis que el fabricante publico para este sistema.");
			Assert.That(TheSentenceAt(FiveYearsLater, 40000), Does.Contain("36 meses"),
				"Y los limites que decidieron son los del sistema, asi que son esos los que la "
				+ "respuesta nombra.");
			Assert.That(TheSentenceAt(FiveYearsLater, 40000), Does.Not.Contain("84 meses"),
				"El sobre del vehiculo no rige aca, asi que nombrarlo confundiria contra que se "
				+ "midio.");
		}

		// El otro lado de la misma ley: dentro de los limites PROPIOS y pasado del sobre del
		// vehiculo. Lo especifico gana en las dos direcciones, no solo cuando recorta.
		[Test]
		public void InsideTheSystemsOwnLimitsIsCoveredEvenPastTheVehiclesEnvelope()
		{
			ASystemWithItsOwnLimitsOf(months: 120, distance: 400000);

			Assert.That(TheStandingAt(TheVehicleIsWellPastItsEnvelope(), 300000),
				Is.EqualTo("Covered"),
				"Ley L5: si los limites propios son mas largos, tambien son los que rigen. Una ley "
				+ "que solo recorta seria una ley de minimos, no de especificidad.");
		}

		// La mitad de distancia decide sola.
		[Test]
		public void PastTheDistanceHalfAloneIsOutsideTheEnvelope()
		{
			ASystemWithItsOwnLimitsOf(months: 120, distance: 60000);

			Assert.That(TheStandingAt(FiveYearsLater, 60009), Is.EqualTo("Covered"),
				"Recorrido 59999 desde la lectura de entrega: todavia adentro.");
			Assert.That(TheStandingAt(FiveYearsLater, 60010), Is.EqualTo("OutsideEnvelope"),
				"Recorrido 60000: el limite se cumple AL LLEGAR, no al pasarlo.");
		}

		// Y se mide desde la LECTURA DE ENTREGA, no desde cero: un vehiculo entregado con
		// kilometraje de traslado no llega al limite antes por haberlo tenido.
		[Test]
		public void TheDistanceIsCountedFromTheDeliveryReadingAndNotFromZero()
		{
			ASystemWithItsOwnLimitsOf(months: 120, distance: 60000);

			Assert.That(TheStandingFrom(3000, 62000), Is.EqualTo("Covered"),
				"Recorrido 59000 desde la entrega: dentro. Contar desde cero lo dejaria afuera por "
				+ "un kilometraje que el vehiculo ya traia.");
			Assert.That(TheStandingFrom(3000, 63001), Is.EqualTo("OutsideEnvelope"));
		}

		// La mitad que no se publico no decide. Un sistema con limite de meses y sin distancia no
		// se sale por kilometraje.
		[Test]
		public void AHalfThatWasNotPublishedDecidesNothing()
		{
			ASystemWithItsOwnLimitsOf(months: 120, distance: 0);

			Assert.That(TheStandingAt(FiveYearsLater, 900000), Is.EqualTo("Covered"),
				"El sistema publico un limite de tiempo y ninguno de distancia. Un cero leido como "
				+ "limite lo dejaria afuera desde el primer metro.");
		}

		// Y al reves.
		[Test]
		public void ASystemWithOnlyADistanceHalfDoesNotAgeOut()
		{
			ASystemWithItsOwnLimitsOf(months: 0, distance: 60000);

			Assert.That(TheStandingAt(TheVehicleIsWellPastItsEnvelope(), 40000),
				Is.EqualTo("Covered"));
		}

		// El limite se cumple AL LLEGAR, no al pasarlo: treinta y seis meses quiere decir que el
		// mes treinta y seis ya esta afuera.
		[Test]
		public void TheLimitBitesOnTheDayItIsReached()
		{
			ASystemWithItsOwnLimitsOf(months: 36, distance: 0);

			Assert.That(TheStandingAt(DeliveredAt.AddMonths(35), 10), Is.EqualTo("Covered"));
			Assert.That(TheStandingAt(DeliveredAt.AddMonths(36), 10), Is.EqualTo("OutsideEnvelope"));
		}

		// Un mes empezado no es un mes cumplido: el dia anterior al aniversario todavia esta
		// adentro.
		[Test]
		public void AMonthIsCountedWholeAndNotStarted()
		{
			ASystemWithItsOwnLimitsOf(months: 36, distance: 0);

			Assert.That(TheStandingAt(DeliveredAt.AddMonths(36).AddDays(-1), 10),
				Is.EqualTo("Covered"));
		}

		private void ASystemWithNoLimitsOfItsOwn()
		{
			specification.PublishCoveredSystem(TheRevision, TheSystem, 0, 0, DistanceUnitCode.UnitA);
			specification.SealRevision(TheRevision);
		}

		private void ASystemWithItsOwnLimitsOf(int months, int distance)
		{
			specification.PublishCoveredSystem(
				TheRevision, TheSystem, months, distance, DistanceUnitCode.UnitA);
			specification.SealRevision(TheRevision);
		}

		private static DateTime TheVehicleIsWellPastItsEnvelope()
		{
			return DeliveredAt.AddMonths(96);
		}

		private string TheStandingAt(DateTime asOf, int currentReading)
		{
			return specification.ReadCoverageStanding(
				TheSystem, 84, 160000, DistanceUnitCode.UnitA,
				DeliveredAt, 10, currentReading, DistanceUnitCode.UnitA, asOf);
		}

		private string TheStandingFrom(int deliveryReading, int currentReading)
		{
			return specification.ReadCoverageStanding(
				TheSystem, 84, 160000, DistanceUnitCode.UnitA,
				DeliveredAt, deliveryReading, currentReading, DistanceUnitCode.UnitA,
				FiveYearsLater);
		}

		private string TheSentenceAt(DateTime asOf, int currentReading)
		{
			return TheSentenceTheDoorWouldCompose.TheCoverage(
				specification, TheSystem, 84, 160000, DistanceUnitCode.UnitA,
				DeliveredAt, 10, currentReading, DistanceUnitCode.UnitA, asOf, TheLanguage.OfTheRecord);
		}
	}
}
