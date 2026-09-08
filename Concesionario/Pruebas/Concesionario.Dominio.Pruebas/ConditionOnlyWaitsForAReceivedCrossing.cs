using System;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// ConditionOnly, la quinta base (§4 del slice): "vence cuando se recibe un cruce de
	// umbral; no lleva limite de tiempo ni de distancia".
	//
	// LA MITAD QUE LA REGLA SOSTIENE. La ley L3, ConditionOnlyBringsForward, vive en
	// ScheduledOperation y llega en T4: "un cruce recibido puede ADELANTAR una operacion a
	// Due; nunca puede empujar un vencimiento hacia afuera" (OHS-08). Lo que la regla aporta
	// para que esa ley se pueda sostener es una propiedad estructural, no una cuenta:
	//
	//   ConditionOnly NUNCA CALCULA UN PUNTO DE VENCIMIENTO.
	//
	// Y por eso no puede empujar ninguno. Una base que calculara un vencimiento propio podria
	// contestar "esta no vence hasta marzo" y estaria posponiendo por la puerta de atras algo
	// que un cruce ya adelanto. Esta no tiene con que: contesta NotDue, siempre, y el unico
	// que la mueve es un hecho recibido de afuera (E44).
	//
	// Que no lleve limites no es una carencia: es la guarda de OINV-20, que revienta si
	// alguien le manda uno. Aca se prueba la otra mitad -- que ademas de no admitirlos,
	// tampoco los inventa.
	[TestFixture]
	public sealed class ConditionOnlyWaitsForAReceivedCrossing
	{
		private IntervalRule porCondicion;
		private BasePoint desde;
		private IntervalLengthFactor sinEscalar;

		[SetUp]
		public void Setup()
		{
			porCondicion = IntervalRule.Of(
				IntervalBasisCode.ConditionOnly, 0, 0, DistanceUnitCode.UnitA);

			desde = BasePoint.At(new DateTime(2025, 6, 1), 30000, DistanceUnit.UnitA);
			sinEscalar = IntervalLengthFactor.For(UsageProfile.Standard);
		}

		[Test]
		public void ItDoesNotFallDueOnItsOwn()
		{
			DueAssessment vencimiento = porCondicion.Assess(
				desde, new DateTime(2026, 1, 20), Lectura(45000), sinEscalar);

			Assert.That(vencimiento.State, Is.SameAs(DueState.NotDue));
		}

		// La propiedad que sostiene a L3: por mucho que pase el tiempo y por mucha distancia
		// que se recorra, la respuesta no se mueve. No hay punto de vencimiento que empujar.
		[Test]
		public void NoAmountOfTimeOrDistanceMakesItFallDue()
		{
			DueAssessment aDiezAnosYCienMilUnidades = porCondicion.Assess(
				desde, new DateTime(2035, 6, 1), Lectura(300000), sinEscalar);

			Assert.That(aDiezAnosYCienMilUnidades.State, Is.SameAs(DueState.NotDue),
				"Si ConditionOnly vence sola, se invento un limite que el plan no dio -- y con "
				+ "el, un vencimiento propio que podria empujar hacia afuera uno que un cruce "
				+ "recibido ya adelanto (L3, OHS-08).");
			Assert.That(aDiezAnosYCienMilUnidades.DecidedByTime, Is.False);
			Assert.That(aDiezAnosYCienMilUnidades.DecidedByDistance, Is.False);
		}

		// Su NotDue SI es evaluable, y ahi esta la diferencia con el NotDue de una distancia
		// suspendida: aca la respuesta es firme -- no vence -- y alla es "no lo se".
		[Test]
		public void ItsNotDueIsAFirmAnswerAndNotIgnorance()
		{
			DueAssessment vencimiento = porCondicion.Assess(
				desde, new DateTime(2026, 1, 20), Lectura(45000), sinEscalar);

			Assert.That(vencimiento.CanBeEvaluated, Is.True);
			Assert.That(vencimiento.DistanceIsSuspended, Is.False,
				"No hay dimension de distancia que suspender: esta base no tiene ninguna de las "
				+ "dos. Suspendida y ausente no son lo mismo.");
			Assert.That(vencimiento.Describe(), Does.Contain("cruce de umbral observado"));
		}

		// No necesita ancla ni lecturas para contestar, porque no mide nada desde ningun lado.
		[Test]
		public void ItAnswersWithNoBasePointAndNoReadings()
		{
			DueAssessment vencimiento = porCondicion.Assess(
				null, new DateTime(2026, 1, 20), UsableReading.None, sinEscalar);

			Assert.That(vencimiento.State, Is.SameAs(DueState.NotDue));
		}

		private static UsableReading Lectura(int valor)
		{
			return UsableReading.Of(new OdometerReading(
				valor, DistanceUnit.UnitA, new DateTime(2026, 1, 1), ReadingSource.Telemetry));
		}
	}
}
