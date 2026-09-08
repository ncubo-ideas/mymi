using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LA MITAD GUARDADA DE E41 EN LA PUERTA, para las once lecturas que trajo T5. El gemelo de nivel
	// A es AProfileToldNothingStillAnswers.EveryGuardedAccessorOfAnUntoldProfileRefusesInsteadOfInventing,
	// que afirma que el AGREGADO revienta. Lo que se agrega aca es el rechazo de la PUERTA, que no es
	// el mismo hecho.
	//
	// POR QUE LA PUERTA PREGUNTA LA PRESENCIA ANTES EN VEZ DE DEJAR PASAR EL RECHAZO DEL DOMINIO. La
	// excepcion del dominio es INTERNA a la asamblea del dominio: la ve la app y la ven estas pruebas,
	// y no la ve ninguna caratula. El navegador y el asistente son los que van a llamar estas once, y
	// una excepcion que no pueden nombrar solo se puede atrapar como Exception pelada -- que es
	// atrapar tambien el fallo del motor, el de disco y el defecto de programacion. Asi que la puerta
	// pregunta primero y rechaza con una excepcion que cualquiera puede nombrar.
	//
	// EL PRECIO, DICHO: cada lectura guardada son DOS consultas y la regla queda escrita dos veces --
	// una en el dominio, una en la puerta. Ninguna de las dos sostiene a la otra: borrar la de la
	// puerta no rompe el dominio, y por eso hace falta esta prueba, que afirma el TIPO y el MENSAJE.
	// Con Assert.Catch pasaria igual con el rechazo del dominio, que es exactamente el defecto.
	//
	// Y SE AFIRMA TAMBIEN QUE LAS CUATRO TOTALES CONTESTAN. Una puerta que reventara siempre pasaria
	// la mitad de este fixture, y un vehiculo del que la agencia todavia no dijo nada no es un error:
	// es el estado normal del primer dia.
	[TestFixture]
	public sealed class EveryGuardedProfileReadRefusesInsteadOfInventing
	{
		private const string TheVin = "1HGCM82633A004352";

		private static readonly DateTime TheInstantAsked =
			new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc);

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

		// LAS SIETE GUARDADAS, una por una, con el nombre del par que hay que preguntar antes. El
		// mensaje va afirmado porque es lo que separa "la puerta pregunto la presencia" de "el
		// dominio reviento y la excepcion paso de largo": las dos revientan, y solo una de las dos le
		// sirve a una caratula.
		[Test]
		public void EachOfTheSevenGuardedReadsThrowsAndNamesThePresenceReadToAskFirst()
		{
			foreach (KeyValuePair<string, TestDelegate> each in EveryGuardedRead())
			{
				InvalidOperationException refused =
					Assert.Throws<InvalidOperationException>(each.Value, each.Key);

				Assert.That(refused.Message, Does.Contain(ThePresenceReadOf(each.Key)),
					$"El rechazo de '{each.Key}' tiene que nombrar la pregunta de presencia que hay "
					+ "que hacer antes. Sin el nombre adentro, quien lo lee sabe que no hay respuesta "
					+ "y no sabe que preguntar.");
			}
		}

		// Y NO REVIENTA CON LA DEL DOMINIO. Es el mismo renglon dicho al reves y hace falta escrito:
		// si alguien quita la pre-pregunta de la puerta, el rechazo del dominio sale igual, las siete
		// siguen reventando, y lo unico que cambia es que ninguna caratula puede nombrar el tipo.
		[Test]
		public void NoneOfTheSevenLetsTheDomainsOwnExceptionOutThroughTheDoor()
		{
			foreach (KeyValuePair<string, TestDelegate> each in EveryGuardedRead())
				Assert.That(Assert.Catch(each.Value), Is.Not.InstanceOf<DomainException>(),
					$"'{each.Key}' dejo salir la excepcion del dominio. Es interna a la asamblea del "
					+ "dominio, asi que una caratula solo la puede atrapar como Exception pelada -- y "
					+ "eso es atrapar tambien el fallo de disco y el defecto de programacion.");
		}

		// LAS CUATRO TOTALES CONTESTAN SIN REVENTAR, que es la otra mitad del par. Con las siete de
		// arriba solas, una puerta que reventara en las once pasaria este fixture entero.
		[Test]
		public void TheFourTotalReadsAnswerOnARecordThatHoldsNothing()
		{
			Assert.That(journal.Api.IsWarrantyEnvelopePublished(), Is.False);
			Assert.That(journal.Api.HasDeliveryBeenRecorded(), Is.False);
			Assert.That(journal.Api.HasAnOdometerReading(TheInstantAsked), Is.False);

			Assert.That(
				journal.Api.DescribePublishedWarrantyEnvelope(),
				Does.Contain("no ha publicado"),
				"Y la mitad publicada contesta CON PALABRAS. Es la lectura que puede citar quien no "
				+ "tiene ni un dato de ningun carro, y que no la haya haria que faltar el odometro de "
				+ "alguien volviera incitable lo que la agencia publico para todos.");
		}

		// LA GUARDA DEL ODOMETRO SE PREGUNTA CON EL MISMO INSTANTE QUE LA LECTURA. Con una lectura
		// observada en marzo, preguntar en enero rechaza y preguntar en junio contesta -- la MISMA
		// agenda, la MISMA lectura, dos instantes. Una guarda llaveada a otro instante deja pasar la
		// de enero, y entonces la lectura revienta despues con el mensaje del dominio y no con este.
		//
		// SE MIRAN LOS DOS LADOS, y el segundo salio de la falsificacion de T5: una guarda corrida
		// HACIA ADELANTE deja pasar una pregunta que no se puede contestar, y una corrida HACIA ATRAS
		// rechaza una que si. Con solo el primer lado escrito, la version hacia atras la atrapaban
		// otras pruebas y no esta -- que es la que NOMBRA la regla. El instante de la observacion
		// exacto es el borde que ve la segunda.
		[Test]
		public void TheOdometerGuardIsAskedAtTheSameInstantTheReadWillUse()
		{
			DateTime observedAt = new DateTime(2026, 3, 10, 7, 15, 20, DateTimeKind.Utc);

			journal.Api.RecordReading(18450, "UnitB", observedAt, "Telemetry");

			DateTime beforeItWasObserved = new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc);

			Assert.That(journal.Api.HasAnOdometerReading(beforeItWasObserved), Is.False);
			Assert.Throws<InvalidOperationException>(
				() => journal.Api.ReadOdometer(beforeItWasObserved));
			Assert.Throws<InvalidOperationException>(
				() => journal.Api.ReadOdometerUnit(beforeItWasObserved));
			Assert.Throws<InvalidOperationException>(
				() => journal.Api.ReadOdometerObservedAt(beforeItWasObserved));

			Assert.That(journal.Api.HasAnOdometerReading(TheInstantAsked), Is.True);
			Assert.That(journal.Api.ReadOdometer(TheInstantAsked), Is.EqualTo(18450));

			// EL BORDE: preguntado EXACTAMENTE en el instante de la observacion, la lectura ya
			// cuenta. Una guarda corrida aunque sea un dia hacia atras rechaza aca.
			Assert.That(journal.Api.HasAnOdometerReading(observedAt), Is.True);
			Assert.That(journal.Api.ReadOdometer(observedAt), Is.EqualTo(18450));
			Assert.That(journal.Api.ReadOdometerUnit(observedAt), Is.EqualTo("mi"));
			Assert.That(journal.Api.ReadOdometerObservedAt(observedAt), Is.EqualTo(observedAt));
		}

		// Y EL RECHAZO NO ESCRIBE NADA. Una consulta no journaliza, pero la pre-pregunta es una
		// segunda consulta y este renglon es lo que dice que sigue siendo una consulta.
		[Test]
		public void ARefusedReadCostsNoJournalEntry()
		{
			string before = TheBytesOf(journal.Folder);

			foreach (KeyValuePair<string, TestDelegate> each in EveryGuardedRead())
				Assert.Throws<InvalidOperationException>(each.Value, each.Key);

			Assert.That(TheBytesOf(journal.Folder), Is.EqualTo(before));
		}

		private Dictionary<string, TestDelegate> EveryGuardedRead()
		{
			return new Dictionary<string, TestDelegate>
			{
				{ "ReadEnvelopePublishedAt",
					() => journal.Api.ReadEnvelopePublishedAt() },
				{ "ReadDeliveredAt",
					() => journal.Api.ReadDeliveredAt() },
				{ "ReadDeliveryReading",
					() => journal.Api.ReadDeliveryReading() },
				{ "ReadDeliveryUnit",
					() => journal.Api.ReadDeliveryUnit() },
				{ "ReadOdometer",
					() => journal.Api.ReadOdometer(TheInstantAsked) },
				{ "ReadOdometerUnit",
					() => journal.Api.ReadOdometerUnit(TheInstantAsked) },
				{ "ReadOdometerObservedAt",
					() => journal.Api.ReadOdometerObservedAt(TheInstantAsked) },
			};
		}

		private static string ThePresenceReadOf(string guarded)
		{
			if (guarded == "ReadEnvelopePublishedAt") return "IsWarrantyEnvelopePublished";
			if (guarded.StartsWith("ReadOdometer", StringComparison.Ordinal))
				return "HasAnOdometerReading";

			return "HasDeliveryBeenRecorded";
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
