using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using DealershipCar.App.Events;
using NUnit.Framework;
using Puppets.Portador;

namespace DealershipCar.Dominio.Pruebas
{
	// QUE CADA CAMPO DEL HECHO RECIBIDO LLEGUE AL CAMPO QUE LE TOCA, y no al de al lado.
	//
	// ESTE ARCHIVO EXISTE POR LA PASADA DE FALSIFICACION DE T8. Cruzar isMandatory con isSafety en
	// la ingesta, y hacer que el ancla tomara la unidad del limite, dejaron las pruebas EN VERDE: la
	// historia que se ingesta no tenia ninguna operacion en la que las dos banderas o las dos
	// unidades fueran distintas, asi que cruzarlas no cambiaba nada. Una historia en la que todo
	// coincide no puede mostrar que las cosas sean distintas.
	//
	// Es la misma clase de hueco que T6 cerro para la puerta -- y no lo hereda: la puerta y la
	// ingesta arman sus parametros por separado, asi que cada una necesita su testigo.
	[TestFixture]
	public sealed class TheIngestedFactKeepsEveryPartOfWhatWasReceived
	{
		private static readonly DateTime AfterSixMonths =
			new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);

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

		// LAS DOS BANDERAS DEL PLAN NO SON INTERCAMBIABLES. "De seguridad" es lo que impide aplazar
		// (ley L6); "obligatoria" no. Una operacion de seguridad que NO es obligatoria distingue las
		// dos: si llegaran cruzadas, el aplazamiento entraria.
		[Test]
		public void AnOperationTheManufacturerClassedAsSafetyCannotBeDeferred()
		{
			Apply(ADealershipNarrative.PlanOperation(
				1, "TimedComponent", "TimeOnly", 100, 0, false, true));

			journal.Api.Evaluate(AfterSixMonths);

			Assert.That(journal.Api.ReadDueState("TimedComponent"), Is.EqualTo("Overdue"),
				"Cien dias desde el 1/9/2025 vencen mucho antes del 1/3/2026.");

			journal.Api.DeferOperation("TimedComponent", "Cost", AfterSixMonths);

			Assert.That(journal.Api.DeferralsUsed("TimedComponent"), Is.EqualTo(0),
				"La ley L6 no aplaza una operacion clasificada de seguridad. Con las dos banderas "
				+ "cruzadas en la ingesta esta llegaria como obligatoria-y-no-de-seguridad, el "
				+ "aplazamiento entraria, y este numero seria 1.");
		}

		// LA UNIDAD DEL LIMITE Y LA DEL ANCLA SON DOS COSAS (C18). Sembrada con el limite en UnitA y
		// el ancla en UnitB, la dimension de distancia nace suspendida por desajuste de unidades
		// (ley L7) -- que es la ley funcionando, no una forma a volver irrepresentable.
		[Test]
		public void TheAnchorCarriesItsOwnUnitAndNotTheLimitsOne()
		{
			Apply(ADealershipNarrative.PlanOperationMeasuredIn(
				1, "FluidChange", "TimeOrDistance", 365, 10000, true, false, "UnitA", "UnitB"));

			Apply(ADealershipNarrative.Reading(
				2, 20000, "UnitA", "2026-02-01T00:00:00Z", "Telemetry"));

			journal.Api.Evaluate(AfterSixMonths);

			Assert.That(journal.Api.ReadDueReason("FluidChange"),
				Does.Contain("units are never converted"),
				"El ancla esta en UnitB y las mediciones en UnitA: dos lineas de medida que nadie "
				+ "convierte. Si el ancla tomara la unidad del limite las dos serian UnitA, no "
				+ "habria desajuste, y esta frase no aparecerian.");
		}

		// LA PROCEDENCIA DE UNA LECTURA LLEGA AL JOURNAL, aunque ninguna ley de esta rebanada la
		// mire todavia.
		//
		// Y por eso se afirma sobre el SEGUNDO PROGRAMA y no sobre una lectura: OdometerReading
		// guarda su ReadingSource y nada en OS2 lo consulta, asi que reemplazarla por una constante
		// no cambia ni un estado de vencimiento -- medido en la pasada de falsificacion, defecto
		// E11. Lo que si tiene que ser cierto es que el hecho quede journalizado COMO LLEGO: la
		// procedencia es un hecho recibido (OINV-54) y la rebanada que la use la va a leer de ahi.
		[Test]
		public void TheProvenanceOfAReadingReachesTheJournalAsItsOwnName()
		{
			Apply(ADealershipNarrative.Reading(
				1, 13200, "UnitA", "2025-10-01T00:00:00Z", "CustomerDeclared"));

			string secondProgram = TheSecondProgramOf(journal.Folder);

			Assert.That(secondProgram, Does.Contain("'CustomerDeclared'"),
				"La procedencia viaja como el NOMBRE de su miembro, al lado de la sentencia y no "
				+ "dentro de ella: asi renumerar el enum no puede reinterpretar la historia (C25).");

			Assert.That(secondProgram, Does.Contain("schedule.RecordReading(value, unit, observedAt, source)"),
				"Y la sentencia lleva los parametros por nombre, no los valores interpolados.");

			Assert.That(secondProgram, Does.Contain("logbook.Advance(event)"),
				"En el MISMO comando que el hecho: es lo que hace exacto el resume tras una caida.");
		}

		// EL NUMERO DE EVENTO VIAJA COMO ARGUMENTO, que es lo que permite que el replay reuse el
		// punto de control recibido en vez de inventar uno.
		[Test]
		public void TheEventNumberRidesAlongsideTheSentenceAndNotInsideIt()
		{
			Apply(ADealershipNarrative.Reading(
				107, 13200, "UnitA", "2025-10-01T00:00:00Z", "Telemetry"));

			string secondProgram = TheSecondProgramOf(journal.Folder);

			Assert.That(secondProgram, Does.Contain("107,13200,'UnitA'"),
				"Los cinco valores del hecho, en orden, como argumentos.");

			Assert.That(secondProgram, Does.Not.Contain("logbook.Advance(107)"),
				"Y NUNCA el numero metido en el texto del comando: ahi el replay reproduciria el "
				+ "TEXTO en vez de reusar el dato que se recibio.");
		}

		// EL INSTANTE SE NORMALIZA TAMBIEN EN LA INGESTA, y hace falta decirlo aparte: la puerta
		// tiene su propio testigo desde T6 (TheDoorNormalizesEveryInstantItSends), pero prueba
		// RecordReading, no Apply, y los dos arman sus parametros por separado.
		//
		// LA SENAL TRAE UN DESPLAZAMIENTO HORARIO, que es lo unico que hace visible la mitad "a UTC"
		// de C24. Un instante terminado en Z ya viene en UTC, y la fraccion la trunca el journal de
		// todas formas (S4), asi que con la historia normal quitar la normalizacion no cambia NADA
		// -- medido en la pasada de falsificacion, defecto E12. Con un desplazamiento si cambia.
		//
		// EL LIMITE, DICHO: en una maquina que ya corra en UTC este renglon no puede fallar, porque
		// no habria conversion que hacer. Es correcto igual -- lo esperado se calcula con el mismo
		// ToUniversalTime que hace la puerta -- pero discrimina solo donde hay desplazamiento.
		[Test]
		public void AnInstantThatArrivesWithAnOffsetIsJournalizedInUtc()
		{
			const string WithAnOffset = "2025-10-01T20:30:45.987-06:00";

			Apply(ADealershipNarrative.Reading(1, 13200, "UnitA", WithAnOffset, "Telemetry"));

			DateTime received = DateTime.Parse(
				WithAnOffset, System.Globalization.CultureInfo.InvariantCulture);
			DateTime inUtc = received.ToUniversalTime();
			DateTime toTheWholeSecond = new DateTime(
				inUtc.Ticks - inUtc.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);

			Assert.That(
				TheSecondProgramOf(journal.Folder),
				Does.Contain(toTheWholeSecond.ToString(
					"MM/dd/yyyy HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture)),
				"El journal guarda el instante ya convertido a UTC y ya truncado al segundo. Sin la "
				+ "normalizacion en la ingesta guardaria la hora local, y la agenda mediria seis "
				+ "horas de diferencia sobre un hecho que llego bien.");
		}

		// EL DESTINO DE ESTA PRUEBA ES DEFENDER A LAS DOS DE ARRIBA. Un extractor que devolviera
		// vacio -- o cualquier cosa constante -- las dejaria verdes para siempre, que es la leccion
		// que T6 y T7 aprendieron cada uno por su lado.
		[Test]
		public void TheExtractorUsedByThisFixtureCanTellTwoJournalsApart()
		{
			string empty = TheSecondProgramOf(journal.Folder);

			Apply(ADealershipNarrative.Reading(
				1, 13200, "UnitA", "2025-10-01T00:00:00Z", "Telemetry"));

			Assert.That(TheSecondProgramOf(journal.Folder), Is.Not.EqualTo(empty));
			Assert.That(empty, Does.Not.Contain("'Telemetry'"));
		}

		private void Apply(SenalDeEvento signal)
		{
			Assert.That(journal.Api.Apply(DealershipEventRoute.Route(signal)), Is.EqualTo(""),
				"El hecho tenia que entrar; si no entro, lo que sigue no mide lo que dice.");
		}

		// EL SEGUNDO PROGRAMA: las tiras imprimibles del binario del journal, que son las sentencias
		// del DSL y los blobs de argumentos. Es la misma tecnica que
		// TheJournalReplaysTheSameReceivedSequenceTheSame usa desde T6.
		private static string TheSecondProgramOf(string folder)
		{
			StringBuilder program = new StringBuilder();

			List<string> files = new List<string>(
				Directory.GetFiles(folder, "*.bin", SearchOption.AllDirectories));
			files.Sort(StringComparer.Ordinal);

			foreach (string file in files)
			{
				byte[] content;

				using (FileStream open = new FileStream(
					file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
				{
					content = new byte[open.Length];
					open.ReadExactly(content, 0, content.Length);
				}

				StringBuilder run = new StringBuilder();

				foreach (byte each in content)
				{
					bool printable = each >= 0x20 && each <= 0x7e;

					if (printable) run.Append((char)each);
					else
					{
						if (run.Length >= 6) program.Append(run).Append('\n');
						run.Clear();
					}
				}

				if (run.Length >= 6) program.Append(run).Append('\n');
			}

			return program.ToString();
		}
	}
}
