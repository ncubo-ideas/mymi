using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// PRUEBA 14 DE §12. La misma secuencia recibida dos veces deja las MISMAS respuestas y el MISMO
	// segundo programa -- el texto del DSL y sus argumentos.
	//
	// NO BYTE A BYTE, Y LA DISTINCION IMPORTA (C23): dos corridas registran dos instantes de
	// ocurrencia y TIENEN que diferir ahi. Lo que se compara son las cadenas imprimibles del journal,
	// que es donde vive el texto del script y sus argumentos. Byte a byte es el chequeo correcto para
	// una operacion que no debe escribir NADA, no para dos corridas de la misma historia.
	//
	// Y ADEMAS UNA RESPUESTA VIVA CONTRA UNA REPRODUCIDA, porque el segundo programa es CIEGO a toda
	// normalizacion que el journal tambien hace (C53). El instante de asignacion es exactamente esa
	// clase de valor: la puerta lo trunca al segundo entero y le pone UTC antes de mandarlo, el journal
	// lo trunca otra vez, y comparar dos journals no puede ver ninguno de los dos truncados porque los
	// dos los sufren igual. La unica forma de verlos es preguntarle a un actor vivo y a uno rehidratado.
	[TestFixture]
	public sealed class TheSameReceivedPlateSequenceAlwaysReplaysTheSame
	{
		private const string TheVin = ADealershipNarrative.TheVin;

		private static readonly DateTime TheDayItAsks =
			new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);

		// LA SECUENCIA: tres asignaciones, una de ellas FUERA DE ORDEN, y dos que dicen la misma
		// placa. Es a proposito -- una secuencia en orden y sin repetidos replaya igual con casi
		// cualquier implementacion.
		private static readonly string[][] TheSequence =
		{
			new[] { "BQR-482", "2025-03-10T14:25:31Z" },
			new[] { "XKD-119", "2025-11-05T08:00:00Z" },
			new[] { "MPL-007", "2025-06-20T19:41:07Z" },
			new[] { "BQR-482", "2026-02-01T00:00:00Z" },
		};

		[Test]
		public void TwoRunsOfTheSameSequenceAnswerTheSameAndLeaveTheSameSecondProgram()
		{
			using (AScheduleOnARealJournal first = AJournal())
			using (AScheduleOnARealJournal second = AJournal())
			{
				Drive(first);
				Drive(second);

				foreach (KeyValuePair<string, string> each in TheAnswersOf(second))
					Assert.That(TheAnswersOf(first)[each.Key], Is.EqualTo(each.Value),
						"'" + each.Key + "' contesta distinto en dos corridas de la MISMA secuencia.");

				Assert.That(
					TheSecondProgramOf(second.Folder), Is.EqualTo(TheSecondProgramOf(first.Folder)),
					"El segundo programa difiere entre dos corridas de la misma secuencia. Lo que se "
					+ "compara es el texto del script y sus argumentos, no los bytes: los instantes "
					+ "de ocurrencia difieren siempre y eso esta bien.");

				Assert.That(TheSecondProgramOf(first.Folder), Is.Not.Empty,
					"Y el extractor tiene que haber sacado algo: dos vacios son iguales.");
			}
		}

		// EL SEGUNDO PROGRAMA LLEVA EL COMANDO POR SU NOMBRE Y LAS PLACAS COMO ARGUMENTOS. Sin esto,
		// la comparacion de arriba pasa igual comparando dos journals que no tienen ninguna placa.
		[Test]
		public void AndTheSecondProgramNamesTheCommandAndCarriesEachPlateAsAnArgument()
		{
			using (AScheduleOnARealJournal journal = AJournal())
			{
				Drive(journal);

				string program = TheSecondProgramOf(journal.Folder);

				Assert.That(program, Does.Contain(APlateOnTheDoorAndInTheDomain.TheCommand),
					"El journal guarda el TEXTO del script, y el texto nombra el miembro del "
					+ "DOMINIO. Es lo que queda congelado: renombrarlo despues deja cada entrada "
					+ "vieja logueando un fallo de rehidratacion en cada arranque futuro.");

				foreach (string[] each in TheSequence)
					Assert.That(program, Does.Contain(each[0]),
						"'" + each[0] + "' no esta en el journal. Lo que se guarda es el texto TAL "
						+ "COMO LLEGO; la llave se vuelve a derivar de el en cada rehidratacion, y "
						+ "por eso NO tiene que estar.");
			}
		}

		// LA MITAD QUE EL SEGUNDO PROGRAMA NO PUEDE VER. El instante cruza truncado al segundo y en
		// UTC; los dos journals lo sufren igual, asi que compararlos entre si es ciego a eso. Comparar
		// una respuesta VIVA contra una REPRODUCIDA no lo es.
		[Test]
		public void AndALiveAnswerEqualsAReplayedOne()
		{
			using (AScheduleOnARealJournal journal = AJournal())
			{
				Drive(journal);

				Dictionary<string, string> live = TheAnswersOf(journal);

				journal.CloseAndReopenFromTheJournal();

				foreach (KeyValuePair<string, string> each in TheAnswersOf(journal))
					Assert.That(live[each.Key], Is.EqualTo(each.Value),
						"'" + each.Key + "' contesta distinto vivo que rehidratado. Es donde se ve "
						+ "una normalizacion del borde que falta: el journal trunca a segundos "
						+ "enteros y borra el Kind, asi que un instante que no salga ya truncado y "
						+ "en UTC vuelve distinto de como se fue.");
			}
		}

		// ===== EL APAREJO =================================================================

		private static void Drive(AScheduleOnARealJournal journal)
		{
			foreach (string[] each in TheSequence)
				APlateOnTheDoorAndInTheDomain.RecordAssignment(
					journal.Api, each[0], TheInstant(each[1]));
		}

		// LAS RESPUESTAS, cada una nombrada, y en TRES momentos de la historia y no en uno: una
		// comparacion vale lo que vale el momento en que se toma (C61), y con un solo instante cinco
		// de estas contestarian lo mismo que contestarian con cualquier secuencia.
		private static Dictionary<string, string> TheAnswersOf(AScheduleOnARealJournal journal)
		{
			var answers = new Dictionary<string, string>();

			foreach (DateTime moment in new[]
			{
				new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc),
				new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc),
				TheDayItAsks,
			})
			{
				string when = moment.ToString("yyyy-MM-dd");

				answers[when + " published"] =
					APlateOnTheDoorAndInTheDomain.IsPublished(journal.Api, moment).ToString();
				answers[when + " asPublished"] =
					APlateOnTheDoorAndInTheDomain.AsPublished(journal.Api, moment);
				answers[when + " joinKey"] =
					APlateOnTheDoorAndInTheDomain.JoinKeyAsOf(journal.Api, moment);
				answers[when + " sentence"] =
					APlateOnTheDoorAndInTheDomain.Describe(journal.Api, moment);
			}

			foreach (string column in new[] { "AsPublished", "JoinKey", "AssignedAt", "IsCurrent" })
				answers["rows " + column] = string.Join(
					"|", APlateOnTheDoorAndInTheDomain.AssignmentColumn(journal.Api, column));

			return answers;
		}

		private static DateTime TheInstant(string said)
		{
			return DateTime.Parse(
				said, System.Globalization.CultureInfo.InvariantCulture,
				System.Globalization.DateTimeStyles.RoundtripKind);
		}

		// LAS CADENAS IMPRIMIBLES DEL JOURNAL. Es donde vive el texto del script y sus argumentos; el
		// resto de los bytes son del motor y llevan los instantes de ocurrencia, que TIENEN que
		// diferir entre dos corridas.
		private static string TheSecondProgramOf(string folder)
		{
			var program = new StringBuilder();

			var files = new List<string>(
				Directory.GetFiles(folder, "*", SearchOption.AllDirectories));
			files.Sort(StringComparer.Ordinal);

			foreach (string file in files)
			{
				byte[] content;

				using (var open = new FileStream(
					file, FileMode.Open, FileAccess.Read,
					FileShare.ReadWrite | FileShare.Delete))
				{
					content = new byte[open.Length];
					open.ReadExactly(content, 0, content.Length);
				}

				var run = new StringBuilder();

				foreach (byte each in content)
				{
					if (each >= 0x20 && each <= 0x7E) { run.Append((char)each); continue; }

					if (run.Length >= 6) program.AppendLine(run.ToString());

					run.Clear();
				}

				if (run.Length >= 6) program.AppendLine(run.ToString());
			}

			return program.ToString();
		}

		private static AScheduleOnARealJournal AJournal()
		{
			return new AScheduleOnARealJournal(TheVin, "Standard");
		}
	}
}
