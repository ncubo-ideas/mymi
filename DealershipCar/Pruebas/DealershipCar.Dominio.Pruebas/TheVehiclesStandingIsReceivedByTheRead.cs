using System;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// §8 DEL BRIEF, sobre el camino de cobertura: "la situacion del vehiculo LA RECIBE la
	// lectura, no la guarda: una lectura de cobertura toma el odometro actual del vehiculo y sus
	// hechos de entrega como PARAMETROS. Este modulo no guarda estado de ningun vehiculo."
	//
	// Es lo que deja que muchos vehiculos compartan una especificacion, y aca se ve funcionando:
	// la MISMA especificacion contesta distinto para dos carros distintos, porque lo unico que
	// cambia son los hechos que entraron con la pregunta.
	//
	// Y LO RECIBIDO SE VALIDA AL ENTRAR (E12, E32). Un odometro que fue para atras, o un instante
	// anterior a la entrega, no son estados del dominio: son hechos imposibles, y contestar algo
	// razonable a partir de ellos seria inventar. Es la misma polaridad que OS2 ya tiene en
	// LimitStanding.OfDistance -- lo imposible revienta, lo inevaluable se degrada -- y las dos
	// conviven en este slice sin confundirse.
	[TestFixture]
	public sealed class TheVehiclesStandingIsReceivedByTheRead
	{
		private const string TheModel = "YARIS-2019";
		private const string TheRevision = "REV-2025-01";
		private const string TheSystem = "ENG-BLOCK";

		private static readonly DateTime PublishedAt = new DateTime(2025, 3, 14, 10, 0, 0);
		private static readonly DateTime DeliveredAt = new DateTime(2020, 1, 15, 9, 0, 0);
		private static readonly DateTime AsOf = new DateTime(2022, 1, 15, 9, 0, 0);

		private ModelSpecification specification;

		[SetUp]
		public void Setup()
		{
			specification = new ModelSpecification(TheModel);
			specification.OpenRevision(TheRevision, PublishedAt);
			specification.PublishCoveredSystem(TheRevision, TheSystem, 0, 0, DistanceUnitCode.UnitA);
			specification.SealRevision(TheRevision);
		}

		// UNA especificacion, DOS carros, DOS respuestas. Nada se quedo guardado de la primera
		// pregunta.
		[Test]
		public void TheSameSpecificationAnswersDifferentlyForTwoVehicles()
		{
			Assert.That(TheStandingOf(DeliveredAt, 10, 12000, AsOf), Is.EqualTo("Covered"));
			Assert.That(TheStandingOf(DeliveredAt, 10, 400000, AsOf),
				Is.EqualTo("OutsideEnvelope"));
			Assert.That(TheStandingOf(DeliveredAt, 10, 12000, AsOf), Is.EqualTo("Covered"),
				"Y la primera respuesta vuelve a salir igual: si la especificacion se hubiera "
				+ "quedado con el odometro del segundo carro, este ultimo asserto seria el que lo "
				+ "descubre.");
		}

		[Test]
		public void AnOdometerThatWentBackwardsIsACallerBug()
		{
			DomainException error = Assert.Throws<DomainException>(
				() => TheStandingOf(DeliveredAt, 20000, 12000, AsOf));

			Assert.That(error.Message, Is.Not.Empty,
				"No es una dimension suspendida: es un hecho imposible, y cual de las dos lecturas "
				+ "esta mal no lo decide este modulo.");
		}

		[Test]
		public void AnInstantBeforeTheDeliveryIsACallerBug()
		{
			Assert.Throws<DomainException>(
				() => TheStandingOf(DeliveredAt, 10, 12000, DeliveredAt.AddDays(-1)));
		}

		[Test]
		public void ANegativeOdometerReadingIsACallerBug()
		{
			Assert.Throws<DomainException>(() => TheStandingOf(DeliveredAt, -1, 12000, AsOf));
		}

		// La validacion es del hecho recibido y NO depende de lo publicado: un codigo que el
		// catalogo no conoce no convierte un odometro imposible en una pregunta razonable.
		[Test]
		public void TheReceivedFactsAreValidatedBeforeAnythingIsLookedUp()
		{
			Assert.Throws<DomainException>(
				() => specification.ReadCoverageStanding(
					"NO-SUCH", 36, 100000, DistanceUnitCode.UnitA,
					DeliveredAt, 20000, 12000, DistanceUnitCode.UnitA, AsOf));
		}

		// Y las tres lecturas del camino validan igual: una puerta que valida y otra que no es
		// una puerta que no valida.
		[Test]
		public void EveryReadOfTheCoveragePathValidatesWhatItReceived()
		{
			Assert.Throws<DomainException>(
				() => specification.CanCoverageBeEvaluated(
					TheSystem, 36, 100000, DistanceUnitCode.UnitA,
					DeliveredAt, 20000, 12000, DistanceUnitCode.UnitA, AsOf));
			Assert.Throws<DomainException>(
				() => specification.DescribeCoverage(
					TheSystem, 36, 100000, DistanceUnitCode.UnitA,
					DeliveredAt, 20000, 12000, DistanceUnitCode.UnitA, AsOf));
		}

		private string TheStandingOf(
			DateTime deliveredAt, int deliveryReading, int currentReading, DateTime asOf)
		{
			return specification.ReadCoverageStanding(
				TheSystem, 36, 100000, DistanceUnitCode.UnitA,
				deliveredAt, deliveryReading, currentReading, DistanceUnitCode.UnitA, asOf);
		}
	}
}
