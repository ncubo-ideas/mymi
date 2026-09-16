using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Concesionario.App;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LO QUE ESTE SLICE NO HIZO, AFIRMADO EN VEZ DE OMITIDO. No hay comando nuevo, no hay global de
	// raiz nueva, no hay nombre de upgrade nuevo y no hay ningun hecho recibido nuevo: las diecinueve
	// operaciones que la puerta gano son PREGUNTAS sobre lo que el registro ya tenia.
	//
	// ASI QUE NO HAY MIGRACION, Y ESTA PRUEBA ES LA QUE LO PRUEBA -- no una que migre. Un journal
	// escrito antes de este slice se abre y contesta las diecinueve sin que se le aplique nada, y lo
	// que hace eso cierto es que ninguna de las diecinueve escribe: el journal queda byte a byte como
	// estaba. Una lectura que escribiera le agregaria una entrada al journal de la persona cada vez
	// que abre la app, para siempre, y el estado seguiria siendo correcto -- asi que ninguna otra
	// prueba se pondria roja.
	//
	// Y LA COMPARACION VIVO-CONTRA-REPRODUCIDO ES LA OTRA MITAD. La comprobacion del segundo programa
	// es ciega a toda normalizacion que el journal tambien hace, y las filas de la parte B estan
	// HECHAS DE INSTANTES: el journal trunca un DateTime a segundos enteros y le borra el Kind. Lo
	// unico que ve esa diferencia es una respuesta viva contra una reproducida.
	[TestFixture]
	public sealed class TheNineteenReadsWriteNothingAndAnOlderJournalNeedsNoMigration
	{
		private const string TheVin = "5YJ3E1EA7JF000005";
		private const string TheKindThatMeasuresDistance = "FluidChange";
		private const string TheKindMeasuredOnlyInTime = "TimedComponent";
		private const string TheUnitEverythingIsIn = "UnitA";
		private const string TheRevision = "REV-2025-01";

		private const int TwelveMonthsInDays = 365;
		private const int TheDistanceLimit = 12345;
		private const int TheOdometerAtTheAnchor = 20000;

		private static readonly DateTime TheAnchor =
			new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheFirstCompletion =
			new DateTime(2025, 4, 11, 12, 0, 0, DateTimeKind.Utc);
		private static readonly DateTime TheSecondCompletion =
			new DateTime(2025, 8, 22, 9, 30, 0, DateTimeKind.Utc);
		private static readonly DateTime TheTimedCompletion =
			new DateTime(2025, 11, 5, 7, 45, 0, DateTimeKind.Utc);

		private static readonly DateTime TheDayItWasObserved =
			new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheDayItIsAsked =
			new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime ThePublication =
			new DateTime(2025, 3, 14, 10, 0, 0, DateTimeKind.Utc);
		private static readonly DateTime ValidFrom =
			new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		private static readonly DateTime ValidUntil =
			new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc);

		private AScheduleOnARealJournal journal;

		[SetUp]
		public void Setup()
		{
			journal = new AScheduleOnARealJournal(TheVin, "Standard");
		}

		[TearDown]
		public void TearDown()
		{
			journal.Dispose();
		}

		[Test]
		public void NoneOfTheNineteenReadsWritesAnEntrySoThereIsNothingToMigrate()
		{
			TheWholeReceivedSequence();

			string before = TheBytesOf(journal.Folder);

			EveryAnswerTheNineteenGive();

			Assert.That(TheBytesOf(journal.Folder), Is.EqualTo(before),
				"El journal quedo byte a byte como estaba. Las diecinueve son consultas: el error que "
				+ "esta prueba existe para atrapar es de una sola letra, PerformCommand donde iba "
				+ "PerformQuery.");
		}

		// LA COMPARACION QUE C53 PIDE Y QUE LA DEL SEGUNDO PROGRAMA NO PUEDE HACER. Las filas de la
		// parte B son instantes y las de la parte C llevan fechas de vigencia: si algo de eso se
		// normalizara distinto al reproducir, solo esta comparacion lo veria.
		[Test]
		public void EveryOneOfTheNineteenAnswersTheSameAfterTheJournalIsReopened()
		{
			TheWholeReceivedSequence();

			string live = EveryAnswerTheNineteenGive();

			journal.CloseAndReopenFromTheJournal();

			Assert.That(EveryAnswerTheNineteenGive(), Is.EqualTo(live),
				"Una respuesta viva y la misma respuesta reproducida desde el journal tienen que ser "
				+ "la misma respuesta. Las filas del historial estan hechas de instantes, que es "
				+ "justamente lo que el journal normaliza.");
		}

		// Y NINGUNA DE LAS DIECINUEVE ESTA VACIA en ese estado del registro: sin esto, las dos de
		// arriba se cumplirian tambien con un juego de respuestas que no contestara nada.
		[Test]
		public void AndTheNineteenAreAnsweringSomethingInThatStateOfTheRecord()
		{
			TheWholeReceivedSequence();

			Assert.That(journal.Api.CanDistanceRemainingBeSaid(TheKindThatMeasuresDistance, TheDayItIsAsked), Is.True);
			Assert.That(journal.Api.HasAnyCompletion(TheKindThatMeasuresDistance), Is.True);
			Assert.That(journal.Api.CompletionCount(TheKindThatMeasuresDistance), Is.EqualTo(2));
			Assert.That(
				journal.Api.CompletionsOfEveryKind(TheLanguage.OfTheRecord).Says,
				Has.Length.EqualTo(3));
			Assert.That(journal.Api.HasAnyPublishedServicePrice(), Is.True);
			Assert.That(
				journal.Api.PublishedServicePricesAsOf(
					TheDayItIsAsked, TheLanguage.OfTheRecord).Says,
				Has.Length.EqualTo(1));
		}

		// ===== LA SECUENCIA RECIBIDA, Y LAS DIECINUEVE RESPUESTAS =========================

		private void TheWholeReceivedSequence()
		{
			journal.Api.SeedPlanOperation(
				TheKindThatMeasuresDistance, "TimeOrDistance",
				TwelveMonthsInDays, TheDistanceLimit, TheUnitEverythingIsIn, true, false,
				TheAnchor, TheOdometerAtTheAnchor, TheUnitEverythingIsIn);

			journal.Api.SeedPlanOperation(
				TheKindMeasuredOnlyInTime, "TimeOnly",
				TwelveMonthsInDays, 0, TheUnitEverythingIsIn, true, false,
				TheAnchor, TheOdometerAtTheAnchor, TheUnitEverythingIsIn);

			journal.Api.CompleteOperation(
				TheKindThatMeasuresDistance, TheFirstCompletion, 22500, TheUnitEverythingIsIn);
			journal.Api.CompleteOperation(
				TheKindThatMeasuresDistance, TheSecondCompletion, 27300, TheUnitEverythingIsIn);
			journal.Api.CompleteOperation(
				TheKindMeasuredOnlyInTime, TheTimedCompletion, 29100, TheUnitEverythingIsIn);

			journal.Api.RecordReading(
				31900, TheUnitEverythingIsIn, TheDayItWasObserved, "Telemetry");

			journal.Api.OpenRevision(TheRevision, ThePublication);
			journal.Api.PublishServicePrice(
				TheRevision, "Inspection", 10000, "UnitA", 120.50m, "CRC", ValidFrom, ValidUntil);
			journal.Api.SealRevision(TheRevision);
		}

		// LAS DIECINUEVE, TODAS, EN UNA SOLA CADENA COMPARABLE. Cada renglon NOMBRA la clase o el
		// miembro del que habla: una lectura que contestara siempre lo mismo dejaria todas las clases
		// con la misma frase, y una comparacion que solo mirara que la respuesta no viene vacia no lo
		// podria ver.
		private string EveryAnswerTheNineteenGive()
		{
			StringBuilder every = new StringBuilder();

			foreach (string kind in Enum.GetNames(typeof(OperationKindCode)))
			{
				bool distance = journal.Api.CanDistanceRemainingBeSaid(kind, TheDayItIsAsked);
				bool days = journal.Api.CanDaysRemainingBeSaid(kind, TheDayItIsAsked);

				every.Append("\ncountdown ").Append(kind)
					.Append(" : canDistance=").Append(distance)
					.Append(" canDays=").Append(days)
					.Append(" | distanceStanding=")
					.Append(journal.Api.ReadDistanceRemainderStanding(kind, TheDayItIsAsked))
					.Append(" | timeStanding=")
					.Append(journal.Api.ReadTimeRemainderStanding(kind, TheDayItIsAsked))
					.Append(" | ").Append(journal.Api.DescribeDistanceRemaining(kind, TheDayItIsAsked,
						TheLanguage.OfTheRecord))
					.Append(" | ").Append(journal.Api.DescribeDaysRemaining(kind, TheDayItIsAsked,
						TheLanguage.OfTheRecord))
					.Append(" | ").Append(journal.Api.DescribeCountdown(kind, TheDayItIsAsked,
						TheLanguage.OfTheRecord));

				if (distance)
					every.Append(" | left=")
						.Append(journal.Api.ReadDistanceRemaining(kind, TheDayItIsAsked))
						.Append(' ')
						.Append(journal.Api.ReadDistanceRemainingUnit(kind, TheDayItIsAsked));

				if (days)
					every.Append(" | daysLeft=")
						.Append(journal.Api.ReadDaysRemaining(kind, TheDayItIsAsked));

				every.Append("\nhistory ").Append(kind)
					.Append(" : any=").Append(journal.Api.HasAnyCompletion(kind))
					.Append(" count=").Append(journal.Api.CompletionCount(kind))
					.Append(" | ").Append(journal.Api.DescribeCompletions(kind, TheLanguage.OfTheRecord));

				CompletionRecord rows = journal.Api.CompletionsOf(kind, TheLanguage.OfTheRecord);

				every.Append("\nrows ").Append(kind)
					.Append(" : ").Append(string.Join(" / ", rows.CompletedAt))
					.Append(" | ").Append(string.Join(" / ", rows.Reading))
					.Append(" | ").Append(string.Join(" / ", rows.Says));

				every.Append("\nprice ").Append(kind)
					.Append(" : ")
					.Append(journal.Api.DescribePublishedServicePrice(
						kind, 10000, "UnitA", TheLanguage.OfTheRecord));
			}

			EveryKindCompletionRecord everyKind =
				journal.Api.CompletionsOfEveryKind(TheLanguage.OfTheRecord);

			every.Append("\nevery kind : ").Append(string.Join(" / ", everyKind.Kind))
				.Append(" | ").Append(string.Join(" / ", everyKind.CompletedAt))
				.Append(" | ").Append(string.Join(" / ", everyKind.Reading))
				.Append(" | ").Append(string.Join(" / ", everyKind.Says));

			PublishedPriceRecord prices = journal.Api.PublishedServicePricesAsOf(
				TheDayItIsAsked, TheLanguage.OfTheRecord);

			every.Append("\nprices : any=").Append(journal.Api.HasAnyPublishedServicePrice())
				.Append(" count=").Append(journal.Api.PublishedServicePriceCount())
				.Append(" | ").Append(string.Join(" / ", prices.Operation))
				.Append(" | ").Append(string.Join(" / ", prices.Milestone))
				.Append(" | ").Append(string.Join(" / ", prices.Revision))
				.Append(" | ").Append(string.Join(" / ", prices.Says));

			return every.ToString();
		}

		private static string TheBytesOf(string folder)
		{
			List<string> files = new List<string>(
				Directory.GetFiles(folder, "*", SearchOption.AllDirectories));
			files.Sort(StringComparer.Ordinal);

			StringBuilder tree = new StringBuilder();

			foreach (string file in files)
			{
				using (FileStream open = new FileStream(
					file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
				{
					byte[] content = new byte[open.Length];
					open.ReadExactly(content, 0, content.Length);

					tree.Append(Path.GetRelativePath(folder, file).Replace('\\', '/'));
					tree.Append(' ');
					tree.Append(content.Length);
					tree.Append(' ');
					tree.AppendLine(Convert.ToHexString(MD5.HashData(content)));
				}
			}

			return tree.ToString();
		}
	}
}
