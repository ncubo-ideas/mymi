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
	// EL HALLAZGO DE T8, Y ES SOBRE C22. Escrito como prueba y no como prosa por la misma razon que
	// ARefusedCommandCostsAJournalEntryUnlessItIsGuarded: para que deje de ser cierto en rojo.
	//
	// T6 midio que una guarda del dominio que revienta ADENTRO del cuerpo deja una entrada
	// permanente, porque el motor escribe antes de ejecutar. T7 lo esquivo no ofreciendo los cuatro
	// comandos que lo hacen. T8 los RECIBE de un concesionario y no puede esquivarlos.
	//
	// LO QUE T8 DESCUBRE ES QUE LA MARCA SE MULTIPLICA, y eso no estaba en C22. El punto de control
	// vive en el mismo comando que revento, asi que no avanzo; el proveedor sirve desde ese punto; y
	// el refresh siguiente trae el MISMO hecho y deja OTRA entrada. Un solo hecho contradictorio
	// dejaria la app atascada en el, marcando el journal en cada apertura y sin volver a recibir
	// nada nuevo nunca. De una marca permanente a una marca por refresh, para siempre. Queda escrito
	// en CONTRADICTIONS C28.
	//
	// LA RESPUESTA: el punto de control avanza en un comando aparte, y el hecho se pierde. Perder UN
	// hecho que el dominio declaro imposible es estrictamente mejor que perder todos los que vengan
	// detras -- incluida la correccion que el concesionario mande despues. Y no es silencioso: el
	// motivo vuelve como rechazo y el bucle lo cuenta.
	//
	// LO QUE ESTO NO ES: un arreglo de C22. La entrada de la primera vez sigue quedando, porque
	// evitarla necesita que la fachada de §4 proyecte lo que cada guarda mira, y eso es un cambio de
	// dominio que T8 no hace. Esto acota el dano; no lo elimina.
	[TestFixture]
	public sealed class AFactTheDomainRefusesDoesNotBlockTheNarrative
	{
		private AScheduleOnARealJournal journal;

		[SetUp]
		public void Setup()
		{
			journal = new AScheduleOnARealJournal(ADealershipNarrative.TheVin, "Standard");

			Apply(ADealershipNarrative.Revision(1, "R1", "2025-09-01T00:00:00Z"));
			Apply(ADealershipNarrative.PlanOperation(
				2, "FluidChange", "TimeOrDistance", 365, 10000, true, false));
			Apply(ADealershipNarrative.Reading(
				3, 20000, "UnitA", "2026-03-01T00:00:00Z", "Telemetry"));
		}

		[TearDown]
		public void TearDown()
		{
			journal.Dispose();
		}

		// LOS TRES HECHOS RECIBIDOS QUE TODAVIA CICATRIZAN, nombrados. Ninguno se puede adelantar a
		// la frontera sin que MaintenanceSchedule proyecte lo que la guarda mira: cual revision
		// rige, si una clase esta agendada, y cual es la ultima lectura de esa unidad.
		[Test]
		public void TheseAreTheThreeReceivedFactsThatStillCostAPermanentEntry()
		{
			string before = TheTreeOf(journal.Folder);

			Assert.That(
				Apply(ADealershipNarrative.Reading(
					4, 100, "UnitA", "2026-04-01T00:00:00Z", "Telemetry")),
				Does.Contain("El odometro no va para atras"),
				"Una lectura que contradice la linea de observacion (C21).");

			Assert.That(
				Apply(ADealershipNarrative.Completion(
					5, "RecallRemedy", "2026-04-01T00:00:00Z", 20100, "UnitA")),
				Does.Contain("que esta agenda no tiene"),
				"Una completacion de una clase que no esta agendada (E32).");

			Assert.That(
				Apply(ADealershipNarrative.Revision(6, "R0", "2025-01-01T00:00:00Z")),
				Does.Contain("Las revisiones van hacia adelante"),
				"Una revision fechada antes de la que rige (OINV-19).");

			Assert.That(TheTreeOf(journal.Folder), Is.Not.EqualTo(before),
				"Y los tres dejaron entrada: el motor ya la habia escrito cuando el cuerpo corrio. "
				+ "Esto dice que hoy es asi, no que este bien -- ver C22.");
		}

		// LA GUARDA QUE SI SE PUDO ADELANTAR, y la diferencia se mide con el oraculo que importa: si
		// el motor loguea un fallo de rehidratacion al reabrir.
		//
		// La combinacion base/limites no mira nada de la agenda, solo los tres valores de la
		// llamada, asi que la puerta se la pregunta a la guarda DEL DOMINIO -- la misma, no una
		// copia -- ANTES de performar. El hecho se rechaza sin que llegue a haber comando, asi que
		// no hay entrada que reventar y no hay cicatriz.
		[Test]
		public void ASeededOperationWhoseBasisContradictsItsLimitsIsRefusedBeforeItCanScar()
		{
			string rejection = Apply(ADealershipNarrative.PlanOperation(
				4, "TimedComponent", "TimeOnly", 550, 15000, true, true));

			Assert.That(rejection, Does.Contain("TimeOnly no admite limite de distancia"),
				"La base no se infiere: se rechaza.");

			Assert.That(journal.Api.LastEventApplied(), Is.EqualTo(4L),
				"Y la narrativa igual avanza, como con cualquier otro rechazo.");

			Assert.That(WhatTheEngineSaysWhenItReopens(),
				Does.Not.Contain("Rehydration execution failed"),
				"NO HAY CICATRIZ. Es la unica de las guardas de C22 que se puede adelantar sin "
				+ "tocar el agregado, porque es la unica que no necesita preguntarle nada a la "
				+ "agenda.");
		}

		// Y EL OTRO LADO DEL MISMO ORACULO, para que el de arriba no pueda quedar verde por no
		// medir nada: un hecho que revienta ADENTRO del cuerpo SI deja la marca, y el motor la
		// loguea en cada arranque futuro de esta agenda. Para siempre.
		[Test]
		public void AFactRefusedInsideTheCommandBodyScarsTheJournalOnEveryFutureOpening()
		{
			Apply(ADealershipNarrative.Reading(4, 100, "UnitA", "2026-04-01T00:00:00Z", "Telemetry"));

			Assert.That(WhatTheEngineSaysWhenItReopens(),
				Does.Contain("Rehydration execution failed"),
				"El motor escribio la entrada antes de correr el cuerpo, asi que el rechazo quedo "
				+ "journalizado. Es el costo de C22, medido por su consecuencia y no por el "
				+ "tamano del archivo.");
		}

		// EL PUNTO DE CONTROL AVANZA IGUAL, que es lo que impide el atasco.
		[Test]
		public void TheCheckpointMovesPastARefusedFactSoTheNextOneCanArrive()
		{
			Apply(ADealershipNarrative.Reading(4, 100, "UnitA", "2026-04-01T00:00:00Z", "Telemetry"));

			Assert.That(journal.Api.LastEventApplied(), Is.EqualTo(4L),
				"La bitacora paso el 4 aunque el hecho no entro. Sin esto el proveedor lo volveria "
				+ "a servir en cada refresh y cada refresh dejaria otra marca.");

			Assert.That(
				Apply(ADealershipNarrative.Reading(
					5, 21000, "UnitA", "2026-05-01T00:00:00Z", "Telemetry")),
				Is.EqualTo(""),
				"Y el hecho siguiente entra normal: la narrativa no quedo atascada.");

			Assert.That(journal.Api.LastEventApplied(), Is.EqualTo(5L));
		}

		// LA MARCA NO SE MULTIPLICA. Re-entregar el bloque que contiene el hecho rechazado no vuelve
		// a escribir: la guarda de idempotencia lo rechaza bajo el read lock, como a cualquier otro
		// evento ya procesado.
		[Test]
		public void RedeliveringARefusedFactDoesNotScarTheJournalASecondTime()
		{
			Apply(ADealershipNarrative.Reading(4, 100, "UnitA", "2026-04-01T00:00:00Z", "Telemetry"));

			string afterTheScar = TheTreeOf(journal.Folder);

			string rejection = Apply(
				ADealershipNarrative.Reading(4, 100, "UnitA", "2026-04-01T00:00:00Z", "Telemetry"));

			Assert.That(rejection, Does.Contain("evento ya procesado"),
				"Y lo rechaza por REPETIDO, no por imposible: ya nadie vuelve a ejecutar el hecho.");

			Assert.That(TheTreeOf(journal.Folder), Is.EqualTo(afterTheScar),
				"Byte a byte igual. La cicatriz es UNA, no una por refresh.");
		}

		// Y EL ESTADO SIGUE SIENDO CORRECTO. Es la mitad que S5 corrigio de su propia primera
		// lectura: el journal queda marcado, no envenenado.
		[Test]
		public void TheScarredJournalStillRehydratesToTheRightState()
		{
			Apply(ADealershipNarrative.Reading(4, 100, "UnitA", "2026-04-01T00:00:00Z", "Telemetry"));
			journal.Api.Evaluate(new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc));

			string live = journal.EveryDueStateItCanAnswer();
			long checkpoint = journal.Api.LastEventApplied();

			journal.CloseAndReopenFromTheJournal();

			Assert.That(journal.EveryDueStateItCanAnswer(), Is.EqualTo(live),
				"La entrada marcada se saltea y el replay sigue.");
			Assert.That(journal.Api.LastEventApplied(), Is.EqualTo(checkpoint),
				"Y el punto de control tambien sobrevivio: el comando que lo avanzo es otro, y ese "
				+ "no revento.");
		}

		private string Apply(SenalDeEvento signal)
		{
			return journal.Api.Apply(DealershipEventRoute.Route(signal));
		}

		// LO QUE EL MOTOR DICE AL REHIDRATAR. Es el oraculo de la cicatriz, y es el unico que la
		// mide por lo que DUELE en vez de por el tamano del archivo: una entrada que revento hace
		// loguear "Rehydration execution failed. EntryId=N" en cada arranque futuro de esta agenda.
		//
		// SE ESCUCHAN LAS DOS SALIDAS, y esa es una medicion de T8 que corrige a T7 por la mitad. T7
		// midio que el motor manda "Starting ... Actor" y el avance del replay a la SALIDA ESTANDAR
		// -- cierto -- pero sus lineas de ERROR, incluida la del fallo de rehidratacion, van a
		// STDERR. Escuchar solo una dejaria este oraculo verde para siempre.
		//
		// Las dos originales se reponen pase lo que pase: dejarlas desviadas haria desaparecer la
		// salida del resto de la corrida.
		private string WhatTheEngineSaysWhenItReopens()
		{
			TextWriter originalOut = Console.Out;
			TextWriter originalError = Console.Error;
			StringWriter heard = new StringWriter();

			try
			{
				Console.SetOut(heard);
				Console.SetError(heard);
				journal.CloseAndReopenFromTheJournal();
			}
			finally
			{
				Console.SetOut(originalOut);
				Console.SetError(originalError);
			}

			return heard.ToString();
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
