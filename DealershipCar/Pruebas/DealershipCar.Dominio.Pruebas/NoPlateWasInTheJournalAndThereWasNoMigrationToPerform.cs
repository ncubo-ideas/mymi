using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// PRUEBA 13 DE §12, Y PRUEBA UNA AUSENCIA. Esta rebanada NO agrega ninguna global de raiz: la
	// placa es un miembro nuevo sobre VehicleProfile, que ya existe bajo su propio nombre de upgrade.
	// Asi que no hay nombre de upgrade nuevo, no hay migracion, y lo que hay que demostrar es
	// justamente que no la hay.
	//
	// UN JOURNAL DE ANTES DE LA REBANADA es uno donde el comando de la placa NUNCA se llamo. Se arma
	// con los hechos que ya existian, se cierra y se vuelve a abrir con esta misma app, y se afirman
	// tres cosas que no son la misma:
	//
	//   1. TODO lo que contestaba antes contesta IGUAL. Un miembro nuevo sobre una global vieja puede
	//      romper la rehidratacion sin tocar ninguna respuesta nueva;
	//   2. la placa contesta "no hay placa registrada" EN VEZ DE FALLAR. Es la diferencia entre una
	//      ausencia modelada y un campo que nadie escribio;
	//   3. ABRIRLO NO ESCRIBIO NADA. Una migracion silenciosa es exactamente esto: una entrada que
	//      aparece al abrir, sin que nadie haya mandado un hecho.
	//
	// LA TERCERA SE MIDE SOBRE journal/*.bin Y NO SOBRE EL ARBOL ENTERO, y con un control de que un
	// comando SI lo hace crecer. Una suma de bytes de todo el arbol se mueve por cosas que no son
	// entradas, y un piso que nunca se probo contra una escritura real no distingue "no escribio" de
	// "no estoy midiendo".
	[TestFixture]
	public sealed class NoPlateWasInTheJournalAndThereWasNoMigrationToPerform
	{
		private const string TheVin = ADealershipNarrative.TheVin;

		private const int ThirtySixMonths = 36;
		private const int SixtyThousand = 60000;
		private const string TheUnitEverythingIsIn = "UnitB";
		private const int TheOdometerAtDelivery = 25;

		private static readonly DateTime TheDayItWasPublished =
			new DateTime(2023, 11, 15, 9, 30, 45, DateTimeKind.Utc);

		private static readonly DateTime TheDayItWasHandedOver =
			new DateTime(2024, 1, 1, 8, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheDayItAsks =
			new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

		[Test]
		public void AJournalWrittenBeforeThisSliceStillAnswersEverythingItAnsweredBefore()
		{
			using (AScheduleOnARealJournal journal = AJournalWithOnlyTheOlderFacts())
			{
				Dictionary<string, string> live = TheOlderAnswersOf(journal);

				journal.CloseAndReopenFromTheJournal();

				foreach (KeyValuePair<string, string> each in TheOlderAnswersOf(journal))
					Assert.That(live[each.Key], Is.EqualTo(each.Value),
						"'" + each.Key + "' contesta distinto despues de reabrir un journal escrito "
						+ "antes de esta rebanada. Un miembro nuevo sobre una global vieja rompe la "
						+ "rehidratacion sin tocar ninguna respuesta nueva.");

				Assert.That(live, Is.Not.Empty,
					"Y la comparacion tiene que haber leido algo: cero respuestas comparadas son "
					+ "cero diferencias.");
			}
		}

		[Test]
		public void AndThePlateReadsAsNoPlateOnRecordInsteadOfFailing()
		{
			using (AScheduleOnARealJournal journal = AJournalWithOnlyTheOlderFacts())
			{
				journal.CloseAndReopenFromTheJournal();

				Assert.That(
					APlateOnTheDoorAndInTheDomain.IsPublished(journal.Api, TheDayItAsks), Is.False);

				Assert.That(
					APlateOnTheDoorAndInTheDomain.Describe(journal.Api, TheDayItAsks), Is.Not.Empty,
					"La placa contesta en palabras. Un journal viejo no tiene el hecho, que es una "
					+ "AUSENCIA y no una falla: si esto revienta, la rebanada si necesitaba migracion "
					+ "y nadie la escribio.");

				Assert.That(
					APlateOnTheDoorAndInTheDomain.AssignmentColumn(journal.Api, "AsPublished"),
					Is.Empty);
			}
		}

		[Test]
		public void AndOpeningItWroteNoEntryOfItsOwn()
		{
			using (AScheduleOnARealJournal journal = AJournalWithOnlyTheOlderFacts())
			{
				long before = TheJournalBytesOf(journal.Folder);

				Assert.That(before, Is.GreaterThan(0),
					"El journal llego vacio al medidor. Un piso de cero no puede ver nada crecer.");

				journal.CloseAndReopenFromTheJournal();
				APlateOnTheDoorAndInTheDomain.Describe(journal.Api, TheDayItAsks);

				Assert.That(TheJournalBytesOf(journal.Folder), Is.EqualTo(before),
					"Abrir un journal viejo y leerle la placa ESCRIBIO. Una migracion silenciosa es "
					+ "exactamente eso: una entrada que aparece sin que nadie haya mandado un hecho.");

				// EL CONTROL. Sin el, la igualdad de arriba pasa igual si el medidor esta mirando el
				// sitio equivocado.
				APlateOnTheDoorAndInTheDomain.RecordAssignment(
					journal.Api, "BQR-482", TheDayItAsks);

				Assert.That(TheJournalBytesOf(journal.Folder), Is.GreaterThan(before),
					"Y un COMANDO si lo hace crecer. Si esto no crece, el medidor no esta mirando el "
					+ "journal y la afirmacion de arriba no dice nada.");
			}
		}

		// ===== EL APAREJO =================================================================

		// SOLO HECHOS ANTERIORES A LA REBANADA. Ninguno es la placa, a proposito: eso es lo que hace
		// que este journal sea "de antes".
		private static AScheduleOnARealJournal AJournalWithOnlyTheOlderFacts()
		{
			var journal = new AScheduleOnARealJournal(TheVin, "Standard");

			journal.Api.PublishWarrantyEnvelope(
				ThirtySixMonths, SixtyThousand, TheUnitEverythingIsIn, TheDayItWasPublished);

			journal.Api.RecordDelivery(
				TheDayItWasHandedOver, TheOdometerAtDelivery, TheUnitEverythingIsIn);

			journal.Api.RecordReading(31000, TheUnitEverythingIsIn, TheDayItAsks, "Telemetry");

			return journal;
		}

		// CADA RESPUESTA NOMBRADA EN SU RENGLON. Comparar solo que no vienen vacias no distinguiria
		// una lectura que contesta siempre lo mismo.
		private static Dictionary<string, string> TheOlderAnswersOf(AScheduleOnARealJournal journal)
		{
			return new Dictionary<string, string>
			{
				{ "vin", journal.Api.ReadVin() },
				{ "envelopePublished", journal.Api.IsWarrantyEnvelopePublished().ToString() },
				{ "envelopeSaid", journal.Api.DescribePublishedWarrantyEnvelope() },
				{ "envelopeAt", journal.Api.ReadEnvelopePublishedAt().ToString("O") },
				{ "delivered", journal.Api.HasDeliveryBeenRecorded().ToString() },
				{ "deliveredAt", journal.Api.ReadDeliveredAt().ToString("O") },
				{ "deliveryReading", journal.Api.ReadDeliveryReading().ToString() },
				{ "deliveryUnit", journal.Api.ReadDeliveryUnit() },
				{ "odometer", journal.Api.ReadOdometer(TheDayItAsks).ToString() },
				{ "odometerUnit", journal.Api.ReadOdometerUnit(TheDayItAsks) },
				{ "envelopeStanding", journal.Api.ReadEnvelopeStanding(TheDayItAsks) },
				{ "envelopeSentence", journal.Api.DescribeWarrantyEnvelope(TheDayItAsks) },
				{ "dueStates", journal.EveryDueStateItCanAnswer() },
			};
		}

		// SOLO LOS .bin DEL JOURNAL. Una suma del arbol entero se mueve por cosas que no son entradas.
		private static long TheJournalBytesOf(string folder)
		{
			long total = 0;

			foreach (string each in Directory.GetFiles(folder, "*.bin", SearchOption.AllDirectories))
				total += new FileInfo(each).Length;

			return total;
		}
	}
}
