using System;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LEY L2, BothLimitsRequired, sobre TimeAndDistance (§5 del slice, OINV-20):
	// "La operacion vence SOLO cuando se alcanzan LOS DOS limites. Alcanzar uno no es vencer."
	//
	// Es la ley espejo de L1, y las dos juntas son la razon por la que OINV-20 termina
	// diciendo que LA BASE NUNCA SE INFIERE: TimeOrDistance y TimeAndDistance reciben
	// exactamente los mismos dos numeros y deciden al reves. Ningun dato los distingue; solo
	// el tipo.
	//
	// QUIEN DECIDE, ACA, ES EL QUE VA ATRASADO. En L1 decide la dimension mas avanzada -- la
	// que llego primero. En L2 decide la que FALTA, porque es la que esta reteniendo la
	// operacion. El motivo tiene que nombrar esa, no la otra: decirle a la persona "faltan
	// 2000 unidades" es util; decirle "ya pasaron los 365 dias" cuando igual no vence es
	// confundirla.
	[TestFixture]
	public sealed class BothDimensionsAreRequiredTogether
	{
		private const int DoceMesesEnDias = 365;
		private const int DiezMilUnidades = 10000;

		private static readonly DateTime UltimaCompletacion = new DateTime(2025, 6, 1);
		private const int LecturaDeLaUltimaCompletacion = 30000;

		private IntervalRule regla;
		private BasePoint desde;
		private IntervalLengthFactor sinEscalar;

		[SetUp]
		public void Setup()
		{
			regla = IntervalRule.Of(
				IntervalBasisCode.TimeAndDistance, DoceMesesEnDias, DiezMilUnidades,
				DistanceUnitCode.UnitA);

			desde = BasePoint.At(
				UltimaCompletacion, LecturaDeLaUltimaCompletacion, DistanceUnit.UnitA);

			sinEscalar = IntervalLengthFactor.For(UsageProfile.Standard);
		}

		// El mismo escenario que hace vencer a TimeOrDistance por distancia -- 40000 el
		// 2026-01-20 -- deja a TimeAndDistance sin vencer, porque el tiempo va en 233 de 365.
		[Test]
		public void WithOnlyOneLimitReachedItDoesNotFallDue()
		{
			DueAssessment vencimiento = regla.Assess(
				desde, new DateTime(2026, 1, 20), Lectura(40000, DistanceUnit.UnitA), sinEscalar);

			Assert.That(vencimiento.State, Is.SameAs(DueState.NotDue),
				"La distancia llego al limite y el tiempo no. L2 exige LOS DOS: alcanzar uno no "
				+ "es vencer.");
			Assert.That(vencimiento.DecidedByTime, Is.True,
				"Decide el que va atrasado, que es el que esta reteniendo la operacion.");
			Assert.That(vencimiento.DecidedByDistance, Is.False);
		}

		[Test]
		public void WithBothLimitsReachedItFallsDue()
		{
			DueAssessment vencimiento = regla.Assess(
				desde, new DateTime(2026, 6, 1), Lectura(40000, DistanceUnit.UnitA), sinEscalar);

			Assert.That(vencimiento.State, Is.SameAs(DueState.Due));
			Assert.That(vencimiento.DecidedByTime, Is.True);
			Assert.That(vencimiento.DecidedByDistance, Is.True);
		}

		// La misma pareja de numeros, las dos bases: una vence y la otra no. Es la prueba de
		// que la base ES el subtipo y no un dato compartido.
		[Test]
		public void TheSameNumbersDecideTheOppositeWayDependingOnTheBasis()
		{
			IntervalRule laPrimeraQueLlegue = IntervalRule.Of(
				IntervalBasisCode.TimeOrDistance, DoceMesesEnDias, DiezMilUnidades,
				DistanceUnitCode.UnitA);

			DateTime mismoInstante = new DateTime(2026, 1, 20);
			UsableReading mismaLectura = Lectura(40000, DistanceUnit.UnitA);

			Assert.That(laPrimeraQueLlegue.Assess(desde, mismoInstante, mismaLectura, sinEscalar).State,
				Is.SameAs(DueState.Due));

			Assert.That(regla.Assess(desde, mismoInstante, mismaLectura, sinEscalar).State,
				Is.SameAs(DueState.NotDue),
				"Mismos limites, misma lectura, mismo instante, y respuestas opuestas. Eso es "
				+ "OINV-20: la base no se puede inferir de los datos porque los datos son iguales.");
		}

		// El vencimiento de L2 es el del limite que falta, asi que Overdue tambien se cuenta
		// desde ahi: la operacion no puede estar pasada de gracia mientras una dimension ni
		// siquiera llego.
		[Test]
		public void TheGraceIsCountedFromTheLimitStillMissing()
		{
			DueAssessment vencimiento = regla.Assess(
				desde, new DateTime(2026, 1, 20), Lectura(60000, DistanceUnit.UnitA), sinEscalar);

			Assert.That(vencimiento.State, Is.SameAs(DueState.NotDue),
				"La distancia esta 20000 unidades pasada y el tiempo no llego. Con L2 la "
				+ "operacion no vencio, asi que menos todavia esta Overdue.");
		}

		private static UsableReading Lectura(int valor, DistanceUnit unidad)
		{
			return UsableReading.Of(new OdometerReading(
				valor, unidad, new DateTime(2026, 1, 1), ReadingSource.Telemetry));
		}
	}
}
