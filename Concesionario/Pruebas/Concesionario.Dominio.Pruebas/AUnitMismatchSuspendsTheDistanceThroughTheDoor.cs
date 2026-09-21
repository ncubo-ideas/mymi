using System;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// PRUEBAS 7 Y 8 DE §11 DEL BRIEF, EN LA PUERTA, y de paso el ORACULO de que el codigo de la
	// unidad cruzo entre globales como VALOR TIPADO y no como un nombre.
	//
	// POR QUE ESTE FIXTURE ES EL QUE LO PRUEBA. La unidad del odometro sale de la agenda y entra
	// como argumento a un metodo del perfil, dentro del mismo script: es el unico camino del modulo
	// por donde un enum cruza de una global de raiz a otra. Si ese valor llegara como el PRIMER
	// miembro del conjunto -- que es lo que pasaria si se erosionara a un nombre y alguien lo
	// resolviera mal -- el limite en UnitB y el odometro en UnitA CALZARIAN, y la respuesta seria un
	// numero en vez de una suspension. La ley del dominio es el oraculo: aca no se convierte nada.
	//
	// LAS DOS MITADES DE LA LEY, que el brief escribe como dos pruebas que parecen contradecirse y
	// no lo son:
	//
	//   plazo TODAVIA NO cumplido  -> la respuesta se SUSPENDE por desajuste de unidades
	//   plazo YA cumplido          -> la respuesta se DECIDE por tiempo, y la distancia se dice
	//                                 no medible y por que
	//
	// Lo que no se puede medir degrada la respuesta SOLO cuando todavia podia cambiarla.
	[TestFixture]
	public sealed class AUnitMismatchSuspendsTheDistanceThroughTheDoor
	{
		private const string TheVin = "5YJ3E1EA7JF123456";

		private const int ThirtySixMonths = 36;
		private const int FifteenThousand = 15000;
		private const string TheUnitTheLimitIsIn = "UnitB";
		private const string TheOtherUnit = "UnitA";

		private const int TheOdometerAtDelivery = 30000;
		private const int TheOdometerToday = 35000;

		private static readonly DateTime ThePublication =
			new DateTime(2023, 11, 15, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheDayItWasHandedOver =
			new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheDayItWasObserved =
			new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime InsideTheTimeHalf =
			new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime PastTheTimeHalf =
			new DateTime(2029, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		// PRUEBA 8: con el plazo sin cumplir, la respuesta queda SUSPENDIDA -- no es una respuesta
		// decidida a la que le falte la mitad de distancia -- y la lectura guardada rechaza.
		[Test]
		public void WithTheTimeHalfStillOpenTheWholeAnswerIsSuspended()
		{
			using (AScheduleOnARealJournal journal = AVehicleWhoseOdometerIsInTheOtherUnit())
			{
				Assert.That(journal.Api.CanTheEnvelopeBeEvaluated(InsideTheTimeHalf), Is.False);

				Assert.Throws<InvalidOperationException>(
					() => journal.Api.ReadEnvelopeStanding(InsideTheTimeHalf));

				string said = journal.Api.DescribeWarrantyEnvelope(
					InsideTheTimeHalf, TheLanguage.OfTheRecord);

				Assert.That(
					said,
					Does.Contain("el límite de distancia publicado está en mi y el odómetro de este "
						+ "vehículo está en km, y las unidades nunca se convierten"),
					"DICE CUAL CONTRA CUAL. Sin las dos unidades nombradas, quien lea sabe que algo "
					+ "no calzo y no sabe que arreglar. Y 'mi' contra 'km' es lo que prueba que el "
					+ "codigo cruzo entre globales como VALOR y no como el primer miembro.");
			}
		}

		// PRUEBA 7: con el plazo YA cumplido, la respuesta se DECIDE por tiempo -- degrada -- y sigue
		// diciendo que la mitad de distancia no se pudo medir y por que.
		[Test]
		public void WithTheTimeHalfAlreadyPastTheAnswerDegradesToTimeAndSaysSo()
		{
			using (AScheduleOnARealJournal journal = AVehicleWhoseOdometerIsInTheOtherUnit())
			{
				Assert.That(journal.Api.CanTheEnvelopeBeEvaluated(PastTheTimeHalf), Is.True);
				Assert.That(journal.Api.ReadEnvelopeStanding(PastTheTimeHalf), Is.EqualTo("Reached"));

				string said = journal.Api.DescribeWarrantyEnvelope(
					PastTheTimeHalf, TheLanguage.OfTheRecord);

				Assert.That(said, Does.Contain("Tiempo: 7 meses pasados de un límite de 36 meses."));
				Assert.That(said, Does.Contain("las unidades nunca se convierten"));
				Assert.That(said, Does.Contain("Alcanzado por tiempo, que llegó primero."));
			}
		}

		// NINGUNA CONVERSION OCURRIO, y esto lo mide por CONSECUENCIA en vez de por ausencia. El
		// vehiculo recorrio 5000 en la otra unidad; convertidas a la del limite serian ~3100 y
		// tambien estaria adentro, asi que "adentro" no discriminaria. Lo que discrimina es que
		// ningun NUMERO de distancia aparezca en la frase: una conversion habria producido uno.
		[Test]
		public void NoDistanceNumberIsProducedForAHalfThatCouldNotBeMeasured()
		{
			using (AScheduleOnARealJournal journal = AVehicleWhoseOdometerIsInTheOtherUnit())
			{
				string said = journal.Api.DescribeWarrantyEnvelope(
					InsideTheTimeHalf, TheLanguage.OfTheRecord);

				Assert.That(said, Does.Not.Contain("left of 15000"));
				Assert.That(said, Does.Not.Contain("pasados de un límite de 15000"));
			}
		}

		// EL ESPEJO EN LA MISMA UNIDAD, que es lo que hace discriminar a todo lo de arriba: con el
		// odometro en la unidad del limite, la misma historia contesta un numero.
		[Test]
		public void TheSameVehicleWithItsOdometerInTheLimitsUnitAnswersWithANumber()
		{
			using (AScheduleOnARealJournal journal = AVehicleWhoseOdometerIsInTheLimitsUnit())
			{
				Assert.That(journal.Api.CanTheEnvelopeBeEvaluated(InsideTheTimeHalf), Is.True);
				Assert.That(
					journal.Api.ReadEnvelopeStanding(InsideTheTimeHalf), Is.EqualTo("Inside"));
				Assert.That(
					journal.Api.DescribeWarrantyEnvelope(
						InsideTheTimeHalf, TheLanguage.OfTheRecord),
					Does.Contain("Distancia: quedan 10.000 mi de 15.000 mi."));
			}
		}

		private static AScheduleOnARealJournal AVehicleWhoseOdometerIsInTheOtherUnit()
		{
			return AVehicleWhoseOdometerIsIn(TheOtherUnit);
		}

		private static AScheduleOnARealJournal AVehicleWhoseOdometerIsInTheLimitsUnit()
		{
			return AVehicleWhoseOdometerIsIn(TheUnitTheLimitIsIn);
		}

		// El sobre y la ENTREGA van en la unidad del limite las dos. Es a proposito: la entrega se
		// pregunta ANTES que el odometro, asi que una entrega en la otra unidad nombraria la
		// ausencia equivocada y este fixture estaria midiendo otra cosa.
		private static AScheduleOnARealJournal AVehicleWhoseOdometerIsIn(string odometerUnit)
		{
			AScheduleOnARealJournal journal = new AScheduleOnARealJournal(TheVin, "Standard");

			journal.Api.PublishWarrantyEnvelope(
				ThirtySixMonths, FifteenThousand, TheUnitTheLimitIsIn, ThePublication);
			journal.Api.RecordDelivery(
				TheDayItWasHandedOver, TheOdometerAtDelivery, TheUnitTheLimitIsIn);
			journal.Api.RecordReading(
				TheOdometerToday, odometerUnit, TheDayItWasObserved, "Telemetry");

			return journal;
		}
	}
}
