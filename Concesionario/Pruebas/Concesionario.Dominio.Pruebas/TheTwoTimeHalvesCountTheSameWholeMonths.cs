using System;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// UN HALLAZGO DE T2, y esta prueba es lo unico que lo vuelve barato: DOS AGREGADOS MIDEN LA
	// MISMA MITAD DE TIEMPO DESDE LA MISMA ENTREGA, y la regla "meses ENTEROS, el dia anterior al
	// aniversario todavia esta adentro" tiene ahora DOS implementaciones.
	//
	//   ASystemLimits.TheTimeHalfIsPast(deliveredAt, asOf)   -- ModelSpecification, contesta un si/no
	//   WarrantyEnvelope.MonthsLeftAsOf(deliveredAt, asOf)   -- este slice, contesta cuantos quedan
	//
	// POR QUE NO SE UNIFICO, dicho en vez de escondido: el kernel compartido es un
	// WholeMonthsBetween PRIVADO de ASystemLimits, y §8 del plan exige que los tres reads de
	// cobertura y el dominio de ModelSpecification queden INTACTOS. Ensanchar un miembro privado de
	// ese agregado lo mueve. Asi que se duplico el calculo y se ATO EL ACUERDO con esta prueba: el
	// dia que las dos se separen, esto se pone rojo en vez de que las dos respuestas de un mismo
	// carro empiecen a discrepar en los bordes que nadie prueba.
	//
	// ES LA DISCIPLINA DE LA LEY L6 -- "un numero para un hecho" -- aplicada a un calculo en vez de
	// a una lectura: los dos caminos se comparan DIRECTAMENTE, y el borde del aniversario es
	// exactamente el caso en que dos implementaciones se separan sin que nadie lo note.
	[TestFixture]
	public sealed class TheTwoTimeHalvesCountTheSameWholeMonths
	{
		private const int ThirtySixMonths = 36;
		private const int ADistance = 60000;

		private static readonly DateTime DeliveredAt = new DateTime(2024, 1, 31);
		private static readonly DateTime PublishedAt = new DateTime(2023, 12, 1);

		// LOS BORDES SON LOS QUE IMPORTAN, y estan elegidos alrededor del aniversario y alrededor
		// del fin de mes -- 31 de enero es donde dos aritmeticas de meses se separan primero.
		[TestCase(2024, 1, 31)]
		[TestCase(2024, 2, 29)]
		[TestCase(2024, 3, 1)]
		[TestCase(2026, 12, 30)]
		[TestCase(2026, 12, 31)]
		[TestCase(2027, 1, 1)]
		[TestCase(2027, 1, 30)]
		[TestCase(2027, 1, 31)]
		[TestCase(2027, 2, 1)]
		[TestCase(2030, 6, 15)]
		public void TheEnvelopeAndTheSystemLimitsAgreeOnWhetherTheTimeHalfIsPast(
			int year, int month, int day)
		{
			DateTime asOf = new DateTime(year, month, day);

			SystemLimits limits = SystemLimits.OfTheVehicleEnvelope(
				ThirtySixMonths, ADistance, DistanceUnit.UnitA);
			WarrantyEnvelope envelope = WarrantyEnvelope.PublishedAs(
				ThirtySixMonths, ADistance, DistanceUnit.UnitA, PublishedAt);

			bool pastForTheSpecification = limits.TheTimeHalfIsPast(DeliveredAt, asOf);
			bool pastForTheEnvelope = envelope.MonthsLeftAsOf(DeliveredAt, asOf) <= 0;

			Assert.That(pastForTheEnvelope, Is.EqualTo(pastForTheSpecification),
				"Los dos agregados miden la misma mitad de tiempo desde la misma entrega y "
				+ "contestaron distinto para " + asOf.ToString("yyyy-MM-dd")
				+ ". Un carro tendria dos respuestas sobre un hecho -- alcanzado para uno y adentro "
				+ "para el otro -- y ninguna pantalla diria que son dos numeros.");
		}

		// Y LA COMPARACION TIENE QUE VER LOS DOS RESULTADOS. Sin esto, los diez casos de arriba
		// podrian estar todos del mismo lado del limite y el acuerdo seria vacio (C60: una
		// comparacion cuyos dos lados siempre iban a coincidir no mide nada).
		[Test]
		public void TheCasesAboveStraddleTheLimitInsteadOfAllFallingOnOneSide()
		{
			WarrantyEnvelope envelope = WarrantyEnvelope.PublishedAs(
				ThirtySixMonths, ADistance, DistanceUnit.UnitA, PublishedAt);

			Assert.That(envelope.MonthsLeftAsOf(DeliveredAt, new DateTime(2026, 12, 31)),
				Is.GreaterThan(0), "Antes del aniversario todavia queda plazo.");
			Assert.That(envelope.MonthsLeftAsOf(DeliveredAt, new DateTime(2027, 1, 31)),
				Is.LessThanOrEqualTo(0), "Cumplido el aniversario, no queda.");
		}

		// Y el mes se cuenta ENTERO: el dia anterior al aniversario todavia esta adentro, que es la
		// misma frase que el /// de ASystemLimits ya lleva.
		[Test]
		public void TheDayBeforeTheAnniversaryStillHasOneMonthLeft()
		{
			WarrantyEnvelope envelope = WarrantyEnvelope.PublishedAs(
				ThirtySixMonths, ADistance, DistanceUnit.UnitA, PublishedAt);

			Assert.That(envelope.MonthsLeftAsOf(DeliveredAt, new DateTime(2027, 1, 30)),
				Is.EqualTo(1));
			Assert.That(envelope.MonthsLeftAsOf(DeliveredAt, new DateTime(2027, 1, 31)),
				Is.EqualTo(0));
		}

		// Y NINGUNA DE LAS DOS SE LE PUEDE PREGUNTAR A UN SOBRE QUE NO EXISTE: sin limites
		// publicados no hay cuantos meses quedan, y un cero aqui pondria al vehiculo alcanzado el
		// dia de la entrega.
		[Test]
		public void AnUnpublishedEnvelopeRefusesToCountMonthsOrDistance()
		{
			Assert.Throws<DomainException>(
				() => WarrantyEnvelope.None.MonthsLeftAsOf(DeliveredAt, new DateTime(2027, 1, 1)));
			Assert.Throws<DomainException>(
				() => WarrantyEnvelope.None.DistanceLeftFrom(10, 25000));
		}
	}
}
