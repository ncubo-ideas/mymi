using System;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// PRUEBAS 5 Y 6 DE §11 DEL BRIEF, EN LA PUERTA. El nivel A ya afirma que el perfil compone la
	// respuesta; lo que solo se puede ver aca es que la lectura de UN argumento la arme sacando los
	// cuatro hechos del registro -- el sobre y la entrega del perfil, el odometro y su unidad de la
	// agenda -- y que las dos mitades sigan diciendose las dos.
	//
	// LOS DOS CASOS SON ESPEJOS Y ESO ES LO QUE DISCRIMINA. Un vehiculo pasado de su limite de
	// distancia y adentro de su plazo nombra la DISTANCIA; uno pasado de su plazo y adentro de su
	// distancia nombra el TIEMPO. Una lectura que contestara siempre por la misma mitad pasaria uno
	// de los dos y se pondria roja en el otro.
	//
	// TODO EN UnitB (C60): UnitA es el primer miembro del enum, asi que una unidad que no cruzara
	// daria la misma respuesta.
	[TestFixture]
	public sealed class AnEnvelopeIsReachedByWhicheverHalfArrivesFirstThroughTheDoor
	{
		private const string TheVin = "JTDKN3DU0A0123456";

		private const int ThirtySixMonths = 36;
		private const int FifteenThousand = 15000;
		private const string TheUnitEverythingIsIn = "UnitB";

		private const int TheOdometerAtDelivery = 30000;

		private static readonly DateTime ThePublication =
			new DateTime(2023, 11, 15, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheDayItWasHandedOver =
			new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheDayItWasObserved =
			new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc);

		// OCHO MESES DESPUES DE LA ENTREGA: bien adentro del plazo de treinta y seis.
		private static readonly DateTime InsideTheTimeHalf =
			new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);

		// CUARENTA Y TRES MESES DESPUES: pasado el plazo de treinta y seis.
		private static readonly DateTime PastTheTimeHalf =
			new DateTime(2029, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		// PRUEBA 6: pasado el limite de distancia y adentro del plazo -> alcanzado, y nombra la
		// DISTANCIA. Las dos mitades van dichas igual.
		[Test]
		public void AVehiclePastItsDistanceLimitAndInsideItsMonthsNamesTheDistance()
		{
			using (AScheduleOnARealJournal journal = AVehicleWhoseOdometerReads(46000))
			{
				Assert.That(journal.Api.CanTheEnvelopeBeEvaluated(InsideTheTimeHalf), Is.True);
				Assert.That(
					journal.Api.ReadEnvelopeStanding(InsideTheTimeHalf), Is.EqualTo("Reached"));

				string said = journal.Api.DescribeWarrantyEnvelope(InsideTheTimeHalf);

				Assert.That(said, Does.Contain("Alcanzado por distancia, que llegó primero."));
				Assert.That(said, Does.Not.Contain("Alcanzado por tiempo"));

				Assert.That(said, Does.Contain("Tiempo: quedan 28 meses de 36 meses."),
					"Y LA MITAD COMODA VA IGUAL. Callarla dejaria a la persona sin saber que su plazo "
					+ "todavia no se cumplio.");
				Assert.That(said, Does.Contain("Distancia: 1000 mi pasados de un límite de 15000 mi."));
			}
		}

		// EL ESPEJO: pasado el plazo y adentro del limite de distancia -> alcanzado, y nombra el
		// TIEMPO. Es el renglon que hace discriminar al de arriba.
		[Test]
		public void AVehiclePastItsMonthsAndInsideItsDistanceNamesTheTime()
		{
			using (AScheduleOnARealJournal journal = AVehicleWhoseOdometerReads(35000))
			{
				Assert.That(journal.Api.ReadEnvelopeStanding(PastTheTimeHalf), Is.EqualTo("Reached"));

				string said = journal.Api.DescribeWarrantyEnvelope(PastTheTimeHalf);

				Assert.That(said, Does.Contain("Alcanzado por tiempo, que llegó primero."));
				Assert.That(said, Does.Not.Contain("Alcanzado por distancia"));

				Assert.That(said, Does.Contain("Tiempo: 7 meses pasados de un límite de 36 meses."));
				Assert.That(said, Does.Contain("Distancia: quedan 10000 mi de 15000 mi."),
					"Y LA MITAD COMODA VA IGUAL, tambien de este lado.");
			}
		}

		// PRUEBA 5: TODA respuesta nombra las dos mitades y cual llega primero, tambien cuando
		// ninguna de las dos se alcanzo. La frase dice ademas que cual llega primero NO se predice:
		// compararlas entre si necesitaria una tasa, y esa tasa es la conversion que aca no se hace.
		[Test]
		public void EveryAnswerNamesBothHalvesAndWhichArrivesFirst()
		{
			using (AScheduleOnARealJournal journal = AVehicleWhoseOdometerReads(35000))
			{
				string said = journal.Api.DescribeWarrantyEnvelope(InsideTheTimeHalf);

				Assert.That(journal.Api.ReadEnvelopeStanding(InsideTheTimeHalf), Is.EqualTo("Inside"));

				Assert.That(said, Does.Contain("Tiempo: quedan 28 meses de 36 meses."));
				Assert.That(said, Does.Contain("Distancia: quedan 10000 mi de 15000 mi."));
				Assert.That(said, Does.Contain("Ninguna de las dos mitades se ha alcanzado"));
				Assert.That(said, Does.Contain("en vez de pronosticar un ritmo"));
			}
		}

		// Y LAS TRES RESPUESTAS SON TRES FRASES DISTINTAS. Sin este renglon, los tres casos de arriba
		// podrian estar pasando contra una lectura que contesta siempre lo mismo y cuyas palabras
		// resultan contener las tres cosas que se le piden.
		[Test]
		public void TheThreeSituationsAreThreeDifferentSentences()
		{
			using (AScheduleOnARealJournal past = AVehicleWhoseOdometerReads(46000))
			using (AScheduleOnARealJournal inside = AVehicleWhoseOdometerReads(35000))
			{
				string byDistance = past.Api.DescribeWarrantyEnvelope(InsideTheTimeHalf);
				string byTime = inside.Api.DescribeWarrantyEnvelope(PastTheTimeHalf);
				string neither = inside.Api.DescribeWarrantyEnvelope(InsideTheTimeHalf);

				Assert.That(byDistance, Is.Not.EqualTo(byTime));
				Assert.That(byDistance, Is.Not.EqualTo(neither));
				Assert.That(byTime, Is.Not.EqualTo(neither));
			}
		}

		// Y EL ODOMETRO CONTRA EL QUE SE MIDIO VA EN LA FRASE, con el dia en que se OBSERVO. Es la
		// linea de honestidad del slice: sin ella, una frase compuesta con una lectura de enero se
		// leeria como tomada el dia en que se pregunto.
		[Test]
		public void TheSentenceNamesTheOdometerItWasMeasuredAgainstAndWhenItWasObserved()
		{
			using (AScheduleOnARealJournal journal = AVehicleWhoseOdometerReads(35000))
			{
				string said = journal.Api.DescribeWarrantyEnvelope(InsideTheTimeHalf);

				Assert.That(said, Does.Contain("odómetro de 35000 mi observado el 2026-01-20"));
				Assert.That(said, Does.Contain("30000 mi registrados cuando el vehículo se entregó"));
			}
		}

		private static AScheduleOnARealJournal AVehicleWhoseOdometerReads(int odometer)
		{
			AScheduleOnARealJournal journal = new AScheduleOnARealJournal(TheVin, "Standard");

			journal.Api.PublishWarrantyEnvelope(
				ThirtySixMonths, FifteenThousand, TheUnitEverythingIsIn, ThePublication);
			journal.Api.RecordDelivery(
				TheDayItWasHandedOver, TheOdometerAtDelivery, TheUnitEverythingIsIn);
			journal.Api.RecordReading(
				odometer, TheUnitEverythingIsIn, TheDayItWasObserved, "Telemetry");

			return journal;
		}
	}
}
