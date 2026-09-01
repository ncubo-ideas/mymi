using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LEY L3. Tests 5 y 6 de §11 del brief.
	//
	// "Una respuesta de sobre dice LAS DOS mitades, lo que queda de cada una, y cual llega primero.
	// Una dimension sola no es la respuesta: '36 meses o 60000' no le dice nada a un dueno que no
	// sabe cuanto maneja." (OSD-09, OUS-25.)
	//
	// DECISION D5 DE T2, y esta prueba es donde vive: L3 ES ESTRUCTURAL UN PISO MAS ABAJO, sobre
	// RemainingEnvelope, y NO sobre la respuesta.
	//
	// LA RAZON ES D1 y no una preferencia. Un sobre no publicado es una respuesta DECIDIDA
	// (NotPublished) que NO TIENE NINGUNA MITAD: no hay limites, asi que no queda nada de nada.
	// Si las dos mitades fueran argumentos del constructor de ASettledEnvelopeAnswer, ese caso
	// tendria que inventarselas, y un cero en la frase de una ausencia es exactamente como entra un
	// default sin que nadie lo escriba (T1, departure 4). Asi que la estructura se pone donde las
	// mitades EXISTEN: RemainingEnvelope no se puede construir sin las dos, y su Describe() las
	// dice siempre, incluida la que no se pudo medir.
	//
	// LO QUE QUEDA CONVENCIONAL, dicho en vez de escondido: la clausula sigue siendo un string en
	// la juntura con ASettledEnvelopeAnswer, exactamente como en ASettledCoverageAnswer. Lo que la
	// defiende es el barrido de abajo, que recorre TODAS las respuestas que el perfil compone.
	[TestFixture]
	public sealed class BothDimensionsAreAlwaysSaid
	{
		private const string TheVin = "3VW2K7AJ9FM123456";
		private const int ThirtySixMonths = 36;
		private const int SixtyThousand = 60000;
		private const int TheDeliveryReading = 10;

		private static readonly DateTime PublishedAt = new DateTime(2023, 12, 1);
		private static readonly DateTime DeliveredAt = new DateTime(2024, 1, 1);
		private static readonly DateTime ObservedAt = new DateTime(2026, 1, 1);
		private static readonly DateTime InsideBothHalves = new DateTime(2026, 1, 1);
		private static readonly DateTime PastTheTimeHalf = new DateTime(2027, 6, 1);

		private VehicleProfile profile;

		[SetUp]
		public void Setup()
		{
			profile = new VehicleProfile(TheVin);
			profile.RecordDelivery(DeliveredAt, TheDeliveryReading, DistanceUnitCode.UnitA);
			profile.PublishWarrantyEnvelope(
				ThirtySixMonths, SixtyThousand, DistanceUnitCode.UnitA, PublishedAt);
		}

		// ===== EL PAR DERIVADO NO SE PUEDE ARMAR A MEDIAS ==================================

		// La mitad estructural de D5: las dos fabricas del par exigen las dos mitades, y la que
		// deja la de distancia sin medir EXIGE DECIR POR QUE. Una mitad sin medir que no diga el
		// motivo es E47 vuelto un string vacio.
		[Test]
		public void TheDerivedPairCannotBeBuiltWithoutBothHalves()
		{
			Assert.Throws<DomainException>(() => RemainingEnvelope.InBothHalves(
				12, ThirtySixMonths, 35010, SixtyThousand, null),
				"Una distancia sin unidad no se puede decir ni comparar.");

			Assert.Throws<DomainException>(() => RemainingEnvelope.MeasuredOnlyInTime(
				12, ThirtySixMonths, string.Empty),
				"Una mitad de distancia sin medir y sin motivo no le deja a nadie arreglarla.");
		}

		// Y las dos formas del par dicen LAS DOS mitades: la medida las dice con su numero, la
		// otra dice que no se pudo medir. Ninguna se calla una.
		[Test]
		public void EveryShapeOfTheDerivedPairNamesBothHalves()
		{
			foreach (RemainingEnvelope pair in LasDosFormasDelPar())
			{
				Assert.That(pair.Describe(), Does.Contain("Time:"), pair.GetType().Name);
				Assert.That(pair.Describe(), Does.Contain("Distance:"), pair.GetType().Name);
			}
		}

		// TEST 5: LAS DOS MITADES, TAMBIEN CUANDO UNA ESTA COMODA Y LA OTRA NO. El escenario esta
		// elegido para que las dos se vean distintas (C60): 12 meses de sobra y 10 km de sobra.
		[Test]
		public void OneComfortableHalfAndOneTightHalfAreBothSaid()
		{
			string said = profile.DescribeWarrantyEnvelope(
				SixtyThousand + TheDeliveryReading - 10, DistanceUnitCode.UnitA,
				ObservedAt, InsideBothHalves);

			Assert.That(said, Does.Contain("12 months left of 36 months"));
			Assert.That(said, Does.Contain("10 km left of 60000 km"),
				"La mitad apretada se dice con su numero. Decir solo la comoda es la mitad de la "
				+ "respuesta y la mitad equivocada.");
			Assert.That(said, Does.Contain("Inside"));
		}

		// LEY L9: toda respuesta lleva el instante en que la agencia publico el sobre. Un sobre
		// viejo tiene que poder VERSE viejo.
		[Test]
		public void EveryAnswerNamesWhenTheEnvelopeWasPublished()
		{
			foreach (EnvelopeAnswer answer in LasRespuestasQueElPerfilCompone())
				Assert.That(answer.Describe(), Does.Contain("2023-12-01"),
					answer.GetType().Name + " no nombra cuando se publico el sobre contra el que "
					+ "midio. Sin ese instante nadie puede ver que el sobre esta viejo.");
		}

		// EL BARRIDO QUE DEFIENDE LO QUE QUEDO CONVENCIONAL: toda respuesta que el perfil compone y
		// que TIENE mitades las dice las dos. Las dos que no las dicen son las dos que no las
		// tienen -- sin sobre no hay limites, y sin entrega no hay desde donde medir.
		[Test]
		public void EveryComposedAnswerThatHasHalvesSaysBothOfThem()
		{
			foreach (EnvelopeAnswer answer in LasRespuestasQueElPerfilCompone())
			{
				Assert.That(answer.Describe(), Does.Contain("Time:"),
					answer.GetType().Name + " no dice la mitad de tiempo.");
				Assert.That(answer.Describe(), Does.Contain("Distance:"),
					answer.GetType().Name + " no dice la mitad de distancia.");
			}
		}

		// Y LA CUENTA (C57): el barrido de arriba enumera las respuestas a mano, asi que hace falta
		// saber cuantas variantes hay para que una quinta no quede invisible. Las que se excluyen
		// se excluyen POR NOMBRE, con su razon, y no por olvido.
		[Test]
		public void TheOnlyAnswersWithoutHalvesAreTheTwoThatHaveNone()
		{
			List<string> todas = typeof(EnvelopeAnswer).Assembly.GetTypes()
				.Where(tipo => tipo.IsSubclassOf(typeof(EnvelopeAnswer)) && !tipo.IsAbstract)
				.Select(tipo => tipo.Name)
				.OrderBy(nombre => nombre, StringComparer.Ordinal)
				.ToList();

			Assert.That(todas, Is.EqualTo(new[]
			{
				"ASettledEnvelopeAnswer",
				"AnEnvelopeSuspendedByAUnitMismatch",
				"AnEnvelopeSuspendedByAnAbsentDelivery",
				"AnEnvelopeSuspendedByAnAbsentReading",
			}),
				"Entro o salio una variante de EnvelopeAnswer. Cada una tiene que decidir si lleva "
				+ "las dos mitades o si es una de las dos que no tienen ninguna.");

			Assert.That(SinSobre().Describe(), Does.Not.Contain("Time:"),
				"Sin sobre publicado no hay limites, asi que no queda nada de nada: la frase lo "
				+ "dice con palabras en vez de con dos ceros.");
			Assert.That(SinEntrega().Describe(), Does.Not.Contain("Time:"),
				"Sin entrega no hay ancla, y las dos mitades se miden desde ahi.");
		}

		// Y las dos que no llevan mitades TAMPOCO llevan un digito inventado en su lugar. Es la
		// otra mitad de la misma ley: la ausencia se dice, no se rellena.
		[Test]
		public void TheTwoAnswersWithoutHalvesCarryNoInventedNumberEither()
		{
			Assert.That(SinSobre().Describe(), Does.Not.Contain(" 0 "));
			Assert.That(SinEntrega().Describe(), Does.Not.Contain(" 0 "));
		}

		// L3 ESTRUCTURAL, LA AFIRMACION DIRECTA: el par derivado dice las dos mitades desde UN
		// SOLO metodo, asi que no hay dos caminos de los cuales uno pueda olvidarse de una.
		[Test]
		public void TheDerivedPairSaysBothHalvesFromOnePlace()
		{
			MethodInfo describe = typeof(RemainingEnvelope).GetMethod("Describe",
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
				| BindingFlags.DeclaredOnly);

			Assert.That(describe, Is.Not.Null,
				"D5: la garantia estructural de L3 es que el par compone su propia frase. Si "
				+ "Describe() se volviera abstracto, cada variante podria decir una sola mitad.");
			Assert.That(describe.IsAbstract, Is.False,
				"Describe() no es abstracto a proposito: la base dice las dos mitades y cada "
				+ "variante solo aporta la clausula de la suya.");
		}

		private EnvelopeAnswer SinSobre()
		{
			return new VehicleProfile(TheVin).TheStandingWithNoOdometerAsOf(InsideBothHalves);
		}

		private EnvelopeAnswer SinEntrega()
		{
			VehicleProfile sinEntrega = new VehicleProfile(TheVin);
			sinEntrega.PublishWarrantyEnvelope(
				ThirtySixMonths, SixtyThousand, DistanceUnitCode.UnitA, PublishedAt);

			return sinEntrega.TheStandingWithNoOdometerAsOf(InsideBothHalves);
		}

		// Las cuatro respuestas que el perfil compone Y QUE TIENEN MITADES: adentro, alcanzada por
		// tiempo, suspendida por unidades, y suspendida por falta de lectura.
		private List<EnvelopeAnswer> LasRespuestasQueElPerfilCompone()
		{
			return new List<EnvelopeAnswer>
			{
				profile.TheStandingAsOf(25000, DistanceUnitCode.UnitA, ObservedAt, InsideBothHalves),
				profile.TheStandingAsOf(25000, DistanceUnitCode.UnitA, ObservedAt, PastTheTimeHalf),
				profile.TheStandingAsOf(25000, DistanceUnitCode.UnitB, ObservedAt, InsideBothHalves),
				profile.TheStandingWithNoOdometerAsOf(InsideBothHalves),
			};
		}

		private static List<RemainingEnvelope> LasDosFormasDelPar()
		{
			return new List<RemainingEnvelope>
			{
				RemainingEnvelope.InBothHalves(
					12, ThirtySixMonths, 35010, SixtyThousand, DistanceUnit.UnitA),
				RemainingEnvelope.MeasuredOnlyInTime(
					12, ThirtySixMonths, "because this is a test of the shape and not of the words."),
			};
		}
	}
}
