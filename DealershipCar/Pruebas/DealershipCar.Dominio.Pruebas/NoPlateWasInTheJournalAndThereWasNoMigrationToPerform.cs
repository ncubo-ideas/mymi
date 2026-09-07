using System;
using System.Collections.Generic;
using DealershipCar.App;
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
	//   3. ABRIRLO NO APLICO NINGUNA MIGRACION. Una migracion silenciosa es exactamente esto: una
	//      entrada que aparece al abrir, sin que nadie haya mandado un hecho.
	//
	// LA TERCERA SE MIDE EN ENTRADAS DEL JOURNAL Y NO EN BYTES, y la forma por bytes se probo primero
	// y no aguanta. Abrir con el host DE LA APP escribe SIEMPRE: OnHydrated es un PerformCommand y
	// deja la entrada de la semilla en CADA hidratacion, aunque los seis upgrades ya esten aplicados y
	// ningun cuerpo corra -- que es exactamente por que DealershipCar.Mcp usa otro host, con el
	// OnHydrated vacio. Asi que "abrirlo no escribio nada" es FALSO bajo cualquier puerta, y lo movia
	// algo que ni siquiera era la placa: el blob de argumentos de la semilla, que se encogio tres
	// bytes el dia que el modelo del aparejo paso de "SM-SEDAN-2025" a "YARIS-2019". Medido en LP-2,
	// rojo desde entonces, y resuelto en DEC-105.
	//
	// EL INSTRUMENTO ES EL COSTO DE LA APERTURA, EN ENTRADAS. La medicion la dejo SH-3 en
	// TheExtraEntryOfAMigrationIsTheNewScriptAndNotTheUpgradeBody: una apertura en regimen cuesta UNA
	// entrada, y cuesta DOS en cuanto el TEXTO del script cambia -- aunque no haya ningun cuerpo de
	// upgrade nuevo, porque un texto que este actor nunca vio es una PLANTILLA nueva. Asi que +1 dice
	// las dos cosas a la vez: ningun cuerpo corrio Y el texto es identico, que es lo que "no hay
	// migracion" significa. Es la misma forma que NothingMovedAndThereWasNoMigrationToPerform, que
	// rechazo los bytes por escrito antes de escribirse asi.
	//
	// LO QUE EL INSTRUMENTO NO VE, MEDIDO Y NO SUPUESTO (C56): un ESPACIO de mas adentro de un cuerpo
	// de upgrade no cuesta nada -- lo falsifico NothingMovedAndThereWasNoMigrationToPerform. La
	// plantilla no se llavea por el texto crudo. Que ese fixture SI vea un nombre de upgrade agregado,
	// quitado o movido de lugar es merito de su copia congelada, no del contador, y abajo esta medido
	// que aqui NO se ve.
	//
	// Y LOS DOS DELTAS SE AFIRMAN EXACTOS, no "crecio" (C60): +1 por la reapertura y +2 por el comando
	// de la placa, que es un texto de script que este journal nunca vio. Es la regla que
	// TheFifthGlobalReachesAJournalThatHasTheOtherFour se escribio para cumplir.
	//
	// FALSIFICADO EN DEC-105, Y LAS DOS MITADES SALIERON DISTINTO. Con un SEGUNDO PerformCommand
	// metido en OnHydrated la reapertura pasa de 10 a 12 y esta prueba se pone ROJA, mientras las
	// otras dos del fixture siguen verdes -- que es lo correcto: ellas afirman respuestas y esta
	// afirma el costo. Asi que el instrumento discrimina justo por donde llegaria una migracion que
	// se aplicara al abrir.
	//
	// PERO REORDENAR LOS SEIS upgrade DE OnHydrated NO LA PONE ROJA, y eso es un LIMITE de este
	// fixture y no del instrumento. El journal lo escribe la app de HOY y se reabre con la app de HOY,
	// asi que la plantilla se acuna en la primera apertura y la segunda cuesta +1 con cualquier texto:
	// es la forma "verde y vacia" que NothingMovedAndThereWasNoMigrationToPerform nombra en su propia
	// cabecera, y la resuelve con una copia CONGELADA a mano del OnHydrated historico. Aqui no se
	// repite (C56): esta rebanada no toca OnHydrated -- la placa es un miembro sobre una global que ya
	// estaba --, asi que la copia congelada seria hoy un duplicado byte a byte de la de hoy, y quien
	// ya vigila que ese texto no cambie es el fixture de MS-, para la app entera. Lo que esta prueba
	// afirma es su propia rebanada: reabrir y leer la placa cuesta la entrada de siempre y ni una mas.
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
		public void AndOpeningItCostsTheOpeningItAlwaysCostAndNothingMore()
		{
			using (AScheduleOnARealJournal journal = AJournalWithOnlyTheOlderFacts())
			{
				long before = TheEntriesOf(journal.Folder);

				Assert.That(before, Is.GreaterThan(0),
					"El journal llego vacio al medidor. Un lector que no encuentra ni una entrada no "
					+ "distingue un journal escrito antes de la rebanada de una carpeta que nadie "
					+ "abrio, y un piso de cero no puede ver nada crecer.");

				journal.CloseAndReopenFromTheJournal();
				APlateOnTheDoorAndInTheDomain.Describe(journal.Api, TheDayItAsks);

				long afterReopening = TheEntriesOf(journal.Folder);

				TestContext.Out.WriteLine(
					"COSTO DE REABRIR UN JOURNAL SIN PLACA Y LEERLE LA PLACA -- CurrentEntryId: "
					+ before + " -> " + afterReopening);

				Assert.That(afterReopening, Is.EqualTo(before + 1),
					"UNA, y es la que toda apertura paga por el comando de la semilla. DOS seria el "
					+ "texto de un script que este actor nunca vio, que es como se ve una migracion "
					+ "desde el journal. Esta rebanada no agrega global de raiz, ni nombre de upgrade, "
					+ "ni cuerpo: la placa es un miembro nuevo sobre una global que ya estaba.");

				// EL CONTROL. Sin el, un +1 constante pasa igual si el medidor esta mirando el sitio
				// equivocado o si dejo de contar.
				APlateOnTheDoorAndInTheDomain.RecordAssignment(
					journal.Api, "BQR-482", TheDayItAsks);

				long afterTheCommand = TheEntriesOf(journal.Folder);

				TestContext.Out.WriteLine(
					"EL CONTROL -- el comando de la placa: " + afterReopening + " -> "
					+ afterTheCommand);

				Assert.That(afterTheCommand, Is.EqualTo(afterReopening + 2),
					"Y UN COMANDO CUESTA DOS: el texto de su script, que este journal nunca vio, mas "
					+ "su invocacion. Es lo que hace que el +1 de arriba signifique algo -- el "
					+ "instrumento SI distingue, y distingue justamente por donde llegaria una "
					+ "migracion.");
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

		// LA CUENTA DE ENTRADAS SE LEE ABRIENDO EN SOLO LECTURA, no preguntandole al actor que esta
		// prueba tiene abierto: preguntarle al escritor mediria al escritor. ReadOnlyDealershipCarApp
		// lleva un OnHydrated VACIO, asi que abrir con el no mueve la cuenta -- es la forma de
		// TheAssistantOnlyEverReadsTheJournal.TheEntryCountOf, que ademas abre asi mientras el
		// escritor de la misma carpeta sigue vivo.
		private static long TheEntriesOf(string folder)
		{
			var reader = new ReadOnlyDealershipCarApp(TheVin);

			try
			{
				reader.ConfigureStorage(Puppeteer.DatabaseType.FileSystem, "path=" + folder);
				reader.Start();

				return reader.Actor.CurrentEntryId;
			}
			finally
			{
				try { reader.Actor.GracefulExit(); } catch { }
			}
		}
	}
}
