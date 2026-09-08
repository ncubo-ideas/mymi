using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Concesionario.App;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LO QUE CUESTA UN RECHAZO, MEDIDO EN ENTRADAS DE JOURNAL. Es un hallazgo de T6 y esta escrito
	// como prueba y no como prosa para que deje de ser cierto en rojo el dia que cambie.
	//
	// HAY TRES MANERAS DE RECHAZAR y las tres se ven iguales desde afuera -- la operacion no entro
	// -- pero le cuestan cosas distintas al journal de la persona:
	//
	//   1. EN LA PUERTA, antes de performar. Un codigo que no nombra ningun miembro lo rechaza
	//      WireName con una ArgumentException y no se escribe nada. Cuesta cero.
	//   2. CON UN Check, bajo el read lock. Es lo que hace Evaluate por la cadencia de C6: el
	//      instante ya evaluado se rechaza y no se escribe nada. Cuesta cero (SPIKES-OS2 S3).
	//   3. CON UN THROW DEL DOMINIO, adentro del cuerpo del comando. MEDIDO aca: la entrada YA SE
	//      ESCRIBIO cuando el cuerpo corre, asi que el rechazo deja una entrada permanente. El
	//      estado queda correcto y el actor rehidrata bien, pero el motor loguea
	//      "Rehydration execution failed. EntryId=N" en CADA arranque futuro de esa agenda, para
	//      siempre. Es la misma cicatriz que midio S5 para un error de tipeo, llegando por la
	//      puerta legitima de una guarda del dominio.
	//
	// CUATRO GUARDAS DEL AGREGADO ESTAN EN EL CASO 3 y ninguna se puede mover al caso 2 sin tocar
	// el dominio, que T6 no toca: un perfil retroactivo, una completacion de una clase que no esta
	// agendada, una lectura que contradice la linea de observacion y una revision que retrocede.
	// Las cuatro dependen de un estado que la fachada no proyecta, asi que un Check no tiene que
	// preguntarle. Evaluate SI se pudo guardar porque T5 dejo el par HasBeenEvaluated /
	// LastEvaluatedAt -- ese par es exactamente lo que a las otras cuatro les falta.
	//
	// Queda escrito en CONTRADICTIONS C22.
	[TestFixture]
	public sealed class ARefusedCommandCostsAJournalEntryUnlessItIsGuarded
	{
		private const string TheVin = "1HGCM82633A004352";
		private const int TwelveMonthsInDays = 365;
		private const int TenThousandUnits = 10000;

		private static readonly DateTime Delivery =
			new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);
		private const int OdometerAtDelivery = 30000;

		private static readonly DateTime TheLimitIsReached =
			new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc);

		private AScheduleOnARealJournal journal;

		[SetUp]
		public void Setup()
		{
			journal = new AScheduleOnARealJournal(TheVin, "Standard");

			journal.Api.SeedPlanOperation(
				"FluidChange", "TimeOrDistance", TwelveMonthsInDays, TenThousandUnits, "UnitA",
				true, false,
				Delivery, OdometerAtDelivery, "UnitA");

			journal.Api.RecordReading(40000, "UnitA", TheLimitIsReached, "Telemetry");
			journal.Api.Evaluate(TheLimitIsReached);
		}

		[TearDown]
		public void TearDown()
		{
			journal.Dispose();
		}

		// CASO 1: rechazado en la puerta. Ni siquiera se performa.
		[Test]
		public void ARefusalAtTheDoorCostsNothing()
		{
			string before = TheBytesOf(journal.Folder);

			Assert.Throws<ArgumentException>(
				() => journal.Api.CompleteOperation(
					"CambioDeAceite", TheLimitIsReached, 40000, "UnitA"));

			Assert.That(TheBytesOf(journal.Folder), Is.EqualTo(before));
		}

		// CASO 2: rechazado por el Check, bajo el read lock. Es la cadencia de C6.
		[Test]
		public void ARefusalUnderTheReadLockCostsNothing()
		{
			string before = TheBytesOf(journal.Folder);

			EvaluationOutcome again = journal.Api.Evaluate(TheLimitIsReached);

			Assert.That(again.WasEvaluated, Is.False);
			Assert.That(TheBytesOf(journal.Folder), Is.EqualTo(before));
		}

		// CASO 3: rechazado por una guarda del dominio, adentro del cuerpo. La entrada ya esta
		// escrita, asi que el rechazo SE JOURNALIZA aunque no haya cambiado nada.
		//
		// Esta prueba dice que hoy es asi, no que este bien. Va en rojo el dia que estas cuatro
		// guardas se muevan al caso 2 -- y ese rojo es la senal de que el arreglo entro.
		[Test]
		public void ARefusalThrownInsideTheCommandBodyCostsAPermanentEntry()
		{
			string before = TheBytesOf(journal.Folder);

			Assert.Throws<DomainException>(
				() => journal.Api.CompleteOperation(
					"RecallRemedy", TheLimitIsReached, 40000, "UnitA"),
				"La clase no esta agendada: completarla es un defecto de quien llama (E32) y el "
				+ "dominio revienta, como debe.");

			Assert.That(TheBytesOf(journal.Folder), Is.Not.EqualTo(before),
				"PERO la entrada ya se habia escrito. El motor loguea 'Script execution failed at "
				+ "write-time' y despues 'Rehydration execution failed. EntryId=N' en cada arranque "
				+ "futuro. Ese es el costo, y esta medido -- ver CONTRADICTIONS C22.");
		}

		// Las otras tres guardas del caso 3, nombradas para que se sepa cuales son.
		[Test]
		public void TheFourGuardsThatStillThrowInsideTheBodyAreTheseFour()
		{
			Assert.Throws<DomainException>(
				() => journal.Api.CompleteOperation(
					"RecallRemedy", TheLimitIsReached, 40000, "UnitA"),
				"Completar una clase que no esta agendada.");

			Assert.Throws<DomainException>(
				() => journal.Api.RecordReading(
					100, "UnitA", TheLimitIsReached.AddDays(1), "Telemetry"),
				"Una lectura que contradice la linea de observacion (C21).");

			journal.Api.DeclareUsageProfile("SevereDuty", TheLimitIsReached.AddDays(10));

			Assert.Throws<DomainException>(
				() => journal.Api.DeclareUsageProfile(
					"Commercial", TheLimitIsReached.AddDays(9)),
				"Un perfil de uso con vigencia retroactiva (OINV-15).");

			journal.Api.AdoptPlanRevision("R2", TheLimitIsReached.AddDays(20));

			Assert.Throws<DomainException>(
				() => journal.Api.AdoptPlanRevision("R1", TheLimitIsReached.AddDays(19)),
				"Una revision del plan fechada antes de la que rige (OINV-19).");
		}

		// Y lo que NO cuesta la cicatriz: el estado sigue siendo correcto y el actor rehidrata. Es
		// la mitad que S5 corrigio de su propia primera lectura -- el journal queda marcado, no
		// envenenado.
		[Test]
		public void TheScarredJournalStillRehydratesToTheRightState()
		{
			string state = journal.EveryDueStateItCanAnswer();

			Assert.Throws<DomainException>(
				() => journal.Api.CompleteOperation(
					"RecallRemedy", TheLimitIsReached, 40000, "UnitA"));

			Assert.That(journal.EveryDueStateItCanAnswer(), Is.EqualTo(state),
				"El comando que revento no cambio nada.");

			journal.CloseAndReopenFromTheJournal();

			Assert.That(journal.EveryDueStateItCanAnswer(), Is.EqualTo(state),
				"Y reproducido desde el journal contesta lo mismo: la entrada marcada se salta y "
				+ "el replay sigue.");
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
					file, FileMode.Open, FileAccess.Read,
					FileShare.ReadWrite | FileShare.Delete))
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
