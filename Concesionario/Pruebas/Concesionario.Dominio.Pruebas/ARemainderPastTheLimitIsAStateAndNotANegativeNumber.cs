using System;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// G2 DE SLICE-TheMaintenanceScreen: "te faltan -300 km" no es una frase. El caso pasado es un
	// ESTADO distinto, no un numero negativo -- y NO se acota a cero, que le diria "te faltan 0 km"
	// a alguien 300 unidades pasado.
	//
	// LA RESOLUCION DE G2 SE APARTA DEL UNICO PRECEDENTE DEL MODULO, y MS-0 lo midio: el resto que
	// ya existia -- AWarrantyEnvelope.MonthsLeftAsOf / DistanceLeftFrom, con RemainingEnvelope
	// encima -- es un int CON SIGNO, y su propio /// lo declara: "Cero es el limite alcanzado exacto
	// y negativo es cuanto se paso; las dos son respuestas, no defectos". Las dos posturas son
	// defendibles y esta es la contraria. La razon por la que part A toma la otra es la PUERTA:
	// el sobre nunca entrega sus mitades como numero -- MonthsLeft, DistanceLeft y RemainingEnvelope
	// no aparecen en NINGUN archivo de App/ -- y part A si, asi que el signo saldria a una pantalla
	// que lo pinta como venga. Adentro de una frase un negativo es legible; como escalar no.
	//
	// EL CERO NO ES UN ACOTE. Es el exceso verdadero en el limite exacto, IsAlreadyPassed() da que
	// si, y la frase tiene su propia forma -- el mismo reparto en tres ramas que
	// AMeasuredDimension.Describe() y ARemainingEnvelope.TheDistanceHalfInWords() ya hacen, y por
	// la misma razon que sus /// dan: "0 pasadas del limite" se leeria como defecto y no como
	// respuesta.
	[TestFixture]
	public sealed class ARemainderPastTheLimitIsAStateAndNotANegativeNumber
	{
		private const string TheVin = "1HGCM82633A004352";
		private const int TenThousandUnits = 10000;
		private const int TheOdometerAtDelivery = 30000;
		private const int TheOvershoot = 300;

		private static readonly DateTime Delivery = new DateTime(2025, 6, 1);
		private static readonly DateTime TheObservation = new DateTime(2025, 9, 9);
		private static readonly DateTime TheInstantAsked = new DateTime(2026, 5, 1);

		[Test]
		public void TheAmountIsNeverNegative()
		{
			MaintenanceSchedule schedule = AScheduleReading(
				TheOdometerAtDelivery + TenThousandUnits + TheOvershoot);

			Assert.That(
				schedule.ReadDistanceRemaining(OperationKindCode.FluidChange, TheInstantAsked),
				Is.GreaterThanOrEqualTo(0));
		}

		// Y NO ESTA ACOTADO: el monto es el exceso REAL, distinguible de cero y del limite.
		[Test]
		public void ItIsNotClampedToZero()
		{
			MaintenanceSchedule schedule = AScheduleReading(
				TheOdometerAtDelivery + TenThousandUnits + TheOvershoot);

			Assert.That(
				schedule.ReadDistanceRemaining(OperationKindCode.FluidChange, TheInstantAsked),
				Is.EqualTo(TheOvershoot),
				"Acotar a cero es la otra mitad de G2 y es la que se cuela sola: el numero deja de "
				+ "ser negativo y encima deja de ser verdad.");

			Assert.That(
				schedule.ReadDistanceRemainderStanding(
					OperationKindCode.FluidChange, TheInstantAsked),
				Is.EqualTo("AlreadyPassed"),
				"El signo elige la VARIANTE. Sin eso, 300 por delante y 300 pasado dirian el mismo "
				+ "numero y solo la frase los separaria.");

			Assert.That(
				schedule.DescribeDistanceRemaining(
					OperationKindCode.FluidChange, TheInstantAsked),
				Does.Not.Contain("te faltan"),
				"Pasado el limite no falta nada.");
		}

		// EL LIMITE EXACTO TIENE FRASE PROPIA, y el exceso ahi es 0 -- que es la verdad, no un
		// acote. IsAlreadyPassed() da que si: el umbral es INCLUSIVO, como los demas del modulo.
		[Test]
		public void AtTheLimitItSaysJustAtTheLimit()
		{
			MaintenanceSchedule schedule = AScheduleReading(
				TheOdometerAtDelivery + TenThousandUnits);

			Assert.That(
				schedule.ReadDistanceRemainderStanding(
					OperationKindCode.FluidChange, TheInstantAsked),
				Is.EqualTo("AlreadyPassed"));

			Assert.That(
				schedule.ReadDistanceRemaining(OperationKindCode.FluidChange, TheInstantAsked),
				Is.EqualTo(0));

			string said = schedule.DescribeDistanceRemaining(
				OperationKindCode.FluidChange, TheInstantAsked);

			Assert.That(said, Does.Contain("justo en el límite"));
			Assert.That(said, Does.Not.Contain("0"),
				"'0 de mas' se leeria como un defecto y no como una respuesta: el limite exacto "
				+ "tiene frase propia, igual que en AMeasuredDimension y en ARemainingEnvelope.");
		}

		// Y UN PASO ANTES DEL LIMITE SIGUE FALTANDO, para que las dos ramas se vean juntas y el
		// borde quede fijado por los dos lados (C61: la comparacion vale por el momento en que se
		// toma).
		[Test]
		public void OneUnitBeforeTheLimitItStillSaysWhatIsLeft()
		{
			MaintenanceSchedule schedule = AScheduleReading(
				TheOdometerAtDelivery + TenThousandUnits - 1);

			Assert.That(
				schedule.ReadDistanceRemainderStanding(
					OperationKindCode.FluidChange, TheInstantAsked),
				Is.EqualTo("StillAhead"));

			Assert.That(
				schedule.ReadDistanceRemaining(OperationKindCode.FluidChange, TheInstantAsked),
				Is.EqualTo(1));

			Assert.That(
				schedule.DescribeDistanceRemaining(
					OperationKindCode.FluidChange, TheInstantAsked),
				Does.Contain("te falta "),
				"Uno solo se dice en singular, como ARemainingEnvelope ya hace con 'queda' y "
				+ "'quedan'.");
		}

		private static MaintenanceSchedule AScheduleReading(int odometer)
		{
			MaintenanceSchedule schedule =
				new MaintenanceSchedule(TheVin, UsageProfileCode.Standard);

			schedule.SeedPlanOperation(
				OperationKindCode.FluidChange, IntervalBasisCode.DistanceOnly,
				0, TenThousandUnits, DistanceUnitCode.UnitA,
				true, false,
				Delivery, TheOdometerAtDelivery, DistanceUnitCode.UnitA);

			schedule.RecordReading(
				odometer, DistanceUnitCode.UnitA, TheObservation, ReadingSourceCode.Telemetry);

			return schedule;
		}
	}
}
