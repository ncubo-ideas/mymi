using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using DealershipCar.App;
using DealershipCar.App.Events;
using NUnit.Framework;
using Puppets.Portador;

namespace DealershipCar.Dominio.Pruebas
{
	// LAS DOS PROMESAS DE T8, MEDIDAS: la ingesta es idempotente, y el resume tras una caida es
	// exacto. Las dos son del plan §7 y las dos se afirman con la forma FUERTE de la comparacion --
	// byte a byte alrededor de algo que no debe escribir NADA (C23), que es donde esa comparacion si
	// se sostiene.
	//
	// COMO ESTA HECHO. El punto de control de la bitacora avanza DENTRO del mismo comando que aplica
	// el hecho, y la guarda es un Check y no un if en el cuerpo:
	//
	//   Check(@event > logbook.LastEvent()) Error 'evento ya procesado';
	//   { schedule.RecordReading(...); logbook.Advance(@event); }
	//
	// Eso da las dos cosas de una vez. Un evento repetido se rechaza BAJO EL READ LOCK y no deja
	// entrada (medido, SPIKES-OS2 S3), asi que re-entregar un bloque cuesta cero. Y no existe el
	// estado "aplique el hecho pero no avance el checkpoint": o entraron los dos o no entro ninguno,
	// asi que una caida a mitad de bloque no puede duplicar ni saltear.
	//
	// La idempotencia vive en el JOURNAL y no en una ventana en memoria, que es lo que la hace
	// sobrevivir al reinicio -- una ventana de dispatch se vacia al arrancar.
	[TestFixture]
	public sealed class AReceivedEventBlockIsAppliedExactlyOnce
	{
		private AScheduleOnARealJournal journal;

		[SetUp]
		public void Setup()
		{
			journal = new AScheduleOnARealJournal(ADealershipNarrative.TheVin, "Standard");
		}

		[TearDown]
		public void TearDown()
		{
			journal.Dispose();
		}

		[Test]
		public void TheWholeStoryEntersOnceAndTheCheckpointEndsWhereTheNarrativeEnds()
		{
			Ingested first = Ingest(journal.Api, ADealershipNarrative.TheWholeStory());

			Assert.That(first.Applied, Is.EqualTo(ADealershipNarrative.SignalsThatApply));
			Assert.That(first.Discarded, Is.EqualTo(ADealershipNarrative.SignalsThatAreDiscarded));
			Assert.That(first.Rejected, Is.EqualTo(0));

			// El checkpoint queda en el ultimo evento APLICADO, no en el ultimo servido: los dos
			// descartados no lo mueven, porque no se journalizo nada por ellos.
			Assert.That(journal.Api.LastEventApplied(), Is.EqualTo(9L),
				"El 10 se descarto en la frontera, asi que la bitacora queda en el 9. Un descarte "
				+ "no avanza el punto de control: nada se escribio por el.");
		}

		// LA PROMESA 1: RE-ENTREGAR EL MISMO BLOQUE NO DEJA NI UNA ENTRADA.
		[Test]
		public void ARedeliveredBlockLeavesTheJournalByteForByteIdentical()
		{
			Ingest(journal.Api, ADealershipNarrative.TheWholeStory());

			string before = TheTreeOf(journal.Folder);
			string state = journal.EveryDueStateItCanAnswer();

			Ingested again = Ingest(journal.Api, ADealershipNarrative.TheWholeStory());

			Assert.That(again.Applied, Is.EqualTo(0),
				"Ninguno vuelve a entrar.");
			Assert.That(again.Rejected, Is.EqualTo(ADealershipNarrative.SignalsThatApply),
				"Los ocho que ya habian entrado los rechaza el Check, uno por uno.");

			Assert.That(TheTreeOf(journal.Folder), Is.EqualTo(before),
				"Y el arbol queda BYTE A BYTE igual. Es la forma fuerte del criterio: un rechazo "
				+ "bajo el read lock no escribe nada, ni una entrada no-op. Con un if adentro del "
				+ "cuerpo en vez de un Check, este renglon se pondria rojo con ocho entradas nuevas.");

			Assert.That(journal.EveryDueStateItCanAnswer(), Is.EqualTo(state));
		}

		// Y EL RECHAZO DICE POR QUE. El sobre crudo es del motor y desenvolverlo es de la caratula
		// (S3); lo que se afirma aca es que el motivo viaja y no viene vacio, porque un rechazo mudo
		// seria indistinguible de un exito para el bucle.
		[Test]
		public void ARedeliveredEventComesBackWithTheReasonItWasRefused()
		{
			IMensajeDeEvento first =
				DealershipEventRoute.Route(ADealershipNarrative.TheWholeStory()[0]);

			Assert.That(journal.Api.Apply(first), Is.EqualTo(""));

			string rejection = journal.Api.Apply(first);

			Assert.That(rejection, Is.Not.Empty);
			Assert.That(rejection, Does.Contain("evento ya procesado"));
		}

		// LA PROMESA 2: MATAR A MITAD DE BLOQUE Y REABRIR NO DUPLICA NI SALTEA.
		//
		// Se corta despues de cinco senales, se cierra el actor y se vuelve a abrir sobre el MISMO
		// journal -- que es lo mas cerca de una caida que se puede llegar sin matar el proceso, y
		// mide lo mismo: al reabrir, todo lo que hay salio del journal. Y despues se re-entrega el
		// bloque ENTERO desde el principio, que es exactamente lo que hace el proveedor: sirve desde
		// el checkpoint que la app le pide, y la app le pide el que tiene journalizado.
		[Test]
		public void KillingMidBlockAndReopeningProducesNoDuplicateAndNoGap()
		{
			Ingest(journal.Api, ADealershipNarrative.TheFirst(5));

			long checkpointBeforeTheCrash = journal.Api.LastEventApplied();
			Assert.That(checkpointBeforeTheCrash, Is.EqualTo(4L),
				"La quinta senal es el tipo que esta app no consume, asi que el punto de control "
				+ "quedo en la cuarta.");

			journal.CloseAndReopenFromTheJournal();

			Assert.That(journal.Api.LastEventApplied(), Is.EqualTo(checkpointBeforeTheCrash),
				"EL CHECKPOINT SALE DEL JOURNAL, no de un campo en memoria. Un campo estaria en "
				+ "cero al reabrir y el bloque entero volveria a entrar.");

			Ingested rest = Ingest(journal.Api, ADealershipNarrative.TheWholeStory());

			Assert.That(rest.Applied, Is.EqualTo(4),
				"Solo entran los que faltaban: 6, 7, 8 y 9. NO HAY DUPLICADO.");
			Assert.That(rest.Rejected, Is.EqualTo(4),
				"Los cuatro que ya habian entrado los rechaza el Check.");

			Assert.That(journal.Api.LastEventApplied(), Is.EqualTo(9L),
				"Y NO HAY HUECO: la bitacora termina donde termina la narrativa aplicable.");
		}

		// LA PRUEBA DE QUE NO HAY HUECO NO ES SOLO EL NUMERO: el estado tiene que ser el mismo que si
		// nunca hubiera habido corte. Sin esto, "el checkpoint llego a 9" seria compatible con
		// haberse salteado un hecho por el camino.
		[Test]
		public void TheStateAfterACrashIsTheSameAsIfTheBlockHadNeverBeenCut()
		{
			Ingest(journal.Api, ADealershipNarrative.TheFirst(5));
			journal.CloseAndReopenFromTheJournal();
			Ingest(journal.Api, ADealershipNarrative.TheWholeStory());
			journal.Api.Evaluate(new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc));

			string afterTheCrash = journal.EveryDueStateItCanAnswer();

			using (AScheduleOnARealJournal uninterrupted =
				new AScheduleOnARealJournal(ADealershipNarrative.TheVin, "Standard"))
			{
				Ingest(uninterrupted.Api, ADealershipNarrative.TheWholeStory());
				uninterrupted.Api.Evaluate(new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc));

				Assert.That(afterTheCrash, Is.EqualTo(uninterrupted.EveryDueStateItCanAnswer()),
					"El corte no dejo rastro en el estado. Es lo que quiere decir 'el resume es "
					+ "exacto': no que el numero coincida, sino que la agenda sea la misma.");
			}
		}

		// LA HISTORIA SOBREVIVE AL REPLAY. Todo lo que contesta despues de reabrir salio del journal:
		// la agenda y la bitacora son globales de raiz del DSL, no campos de C#, y un campo estaria
		// vacio despues de rehidratar.
		[Test]
		public void TheIngestedStoryAnswersTheSameAfterReplayingFromTheJournal()
		{
			Ingest(journal.Api, ADealershipNarrative.TheWholeStory());
			journal.Api.Evaluate(new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc));

			string live = journal.EveryDueStateItCanAnswer();
			long checkpoint = journal.Api.LastEventApplied();

			journal.CloseAndReopenFromTheJournal();

			Assert.That(journal.EveryDueStateItCanAnswer(), Is.EqualTo(live));
			Assert.That(journal.Api.LastEventApplied(), Is.EqualTo(checkpoint));
		}

		// LO QUE LA HISTORIA DEJA EN LA AGENDA, afirmado en negocio y no solo en contadores. Sin
		// esto, todo lo de arriba seguiria verde con una ingesta que aplicara los ocho comandos
		// equivocados.
		[Test]
		public void TheDealershipsFactsLandOnTheScheduleAsTheFactsTheyWere()
		{
			Ingest(journal.Api, ADealershipNarrative.TheWholeStory());
			journal.Api.Evaluate(new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc));

			Assert.That(journal.Api.ReadDueState("Inspection"), Is.EqualTo("Due"),
				"El cruce de umbral del 1/4/2026 la vencio (ley L3). Y se queda en Due por mas que "
				+ "pasen cinco meses: una operacion ConditionOnly no mide desde ningun lado, asi "
				+ "que no tiene punto de vencimiento del que la gracia pueda pasarse.");

			Assert.That(journal.Api.ReadDueState("FluidChange"), Is.EqualTo("NotDue"),
				"La completacion del 5/3/2026 re-anclo su sucesora ahi (ley L4): al 1/9/2026 lleva "
				+ "180 dias de 365 y 0 unidades de 10000.");

			Assert.That(journal.Api.ReadDueState("RecallRemedy"), Is.EqualTo("NotDue"),
				"Y una clase que la narrativa nunca menciono contesta igual: la ausencia es una "
				+ "respuesta legal (E41).");
		}

		// ------------------------------------------------------ el aparejo

		// El bucle del Portador, a mano. Se hace aca en vez de usar BucleDeRefresh porque el bucle
		// de verdad necesita una fuente HTTP, y lo que estas pruebas miden es la ingesta y no el
		// transporte: la senal ya esta, y lo unico que falta es rutearla y aplicarla. Es literalmente
		// el cuerpo del foreach de BucleDeRefresh.Ejecutar.
		private static Ingested Ingest(
			DealershipCarAPI api, IReadOnlyList<SenalDeEvento> signals)
		{
			Ingested counted = new Ingested();

			foreach (SenalDeEvento signal in signals)
			{
				IMensajeDeEvento message = DealershipEventRoute.Route(signal);

				if (message == null) { counted.Discarded++; continue; }

				if (api.Apply(message).Length > 0) { counted.Rejected++; continue; }

				counted.Applied++;
			}

			return counted;
		}

		private sealed class Ingested
		{
			internal int Applied;
			internal int Discarded;
			internal int Rejected;
		}

		private static string TheTreeOf(string folder)
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
