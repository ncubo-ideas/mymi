using System;
using System.Collections.Generic;
using System.Reflection;
using Concesionario.App;
using Concesionario.Mcp;
using Concesionario.Mcp.Tools;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// EL GASTO LLEGA AL ASISTENTE CON LAS DOS VISTAS ADENTRO, Y NINGUNO DE SUS CUATRO LIMITES ES UNA
	// MEDICION (CL-8). Es §15 item 7 del brief hecho prueba: si el gasto de un periodo NO fuera
	// contestable en este borde sin pedirle un hecho del carro a quien pregunta, seria la forma de C58
	// otra vez y habria que nombrarla.
	//
	// TODAS LAS MITADES VIENEN SIEMPRE Y CON NOMBRE, Y ES LO CONTRARIO DE LO QUE HACE LA PANTALLA. E48
	// dice que a una PERSONA que lee una sola columna no se le imprimen las dos frases del gasto,
	// porque la de por moneda ya contiene lo que dice la otra; a un MODELO que lee campos con nombre
	// hay que darle las dos, porque de un solo campo no puede saber cual de las dos preguntas
	// contesto. Asi que aca says y perCurrencySays vienen las dos, y la pantalla pinta una.
	//
	// Y EL CANAL LATERAL SE INVIERTE CON ELLAS. Lo que una persona hace con "quien registra un gasto"
	// es ir a pedirlo; un modelo no puede hacer eso, y lo que necesita es la INSTRUCCION -- no sume,
	// no invente un total entre monedas, no conteste cero, no de un veredicto -- porque si no llena el
	// hueco por su cuenta. Por eso detail lleva instrucciones y no la frase de la pantalla.
	//
	// LOS CUATRO LIMITES SON TEXTO, y eso es lo que no se podia equivocar: un DateTime aca vuelve a
	// tipar un limite que el censo de tools SI ve, y las dos listas de exencion dejarian de estar
	// vacias. El censo esta en TheStandingReachesTheAssistantAndNoToolAsksForIt; lo que se afirma aca
	// es la otra mitad, que las cuatro puntas de los dos periodos las arma ESTA tool.
	[TestFixture]
	public sealed class TheSpendReachesTheAssistantAsBothViewsAndNoBoundIsAMeasurement
	{
		private const string TheVin = "WBA3A5C56DF000001";

		private const string TheColon = "CRC";
		private const string TheDollar = "USD";

		private const string TheTyres = "Llantas nuevas";
		private const string TheOilChange = "Cambio de aceite";
		private const string TheWash = "Lavado premium";

		private static readonly DateTime TheTyresIncurred =
			new DateTime(2026, 3, 12, 9, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheOilChangeIncurred =
			new DateTime(2026, 5, 20, 10, 30, 0, DateTimeKind.Utc);

		// EL LAVADO CAE A LAS 14:15 DEL 5 DE AGOSTO A PROPOSITO. Es lo que hace que la decision del
		// limite superior se pueda medir: un dia como limite que llegara solo hasta su medianoche
		// dejaria ese gasto AFUERA, y el periodo pasaria de dos monedas a una.
		private static readonly DateTime TheWashIncurred =
			new DateTime(2026, 8, 5, 14, 15, 0, DateTimeKind.Utc);

		private const string TheUnit = "UnitA";

		private static readonly DateTime TheFirstReading =
			new DateTime(2026, 1, 10, 8, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheLastReading =
			new DateTime(2026, 8, 20, 8, 0, 0, DateTimeKind.Utc);

		private AScheduleOnARealJournal journal;

		[SetUp]
		public void ThreeCostsInTwoCurrenciesAndTwoReadings()
		{
			journal = new AScheduleOnARealJournal(TheVin, "Standard");

			journal.Api.RecordReading(20000, TheUnit, TheFirstReading, "Telemetry");
			journal.Api.RecordReading(29000, TheUnit, TheLastReading, "Telemetry");

			journal.Api.RecordDurableCost(TheTyresIncurred, 240000.00m, TheColon, TheTyres, 48);
			journal.Api.RecordRecurringCost(TheWashIncurred, 120.50m, TheDollar, TheWash);
			journal.Api.RecordRecurringCost(TheOilChangeIncurred, 45000.00m, TheColon, TheOilChange);
		}

		[TearDown]
		public void TearDown()
		{
			journal.Dispose();
		}

		// LAS DOS VISTAS LLEGAN ADENTRO DE LA FRASE, Y CADA FRASE ES LA DE LA PUERTA LETRA POR LETRA.
		// Los cuatro instantes con los que se le pregunta a la puerta estan escritos aca: son la regla
		// de esta tool dicha por segunda vez, asi que cambiarla de un lado la pone roja.
		[Test]
		public void TheSpendComesAsTheDoorsOwnSentencesWithBothViewsInsideThem()
		{
			SpendOnThisCar answer =
				TheAssistantAsks("2026-01-01", "2026-12-31", "2025-01-01", "2025-12-31");

			DateTime start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			DateTime end = new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc);
			DateTime earlierStart = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			DateTime earlierEnd = new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc);

			Assert.That(answer.HasData, Is.True);
			Assert.That(answer.Vin, Is.EqualTo(TheVin));
			Assert.That(answer.BoundsUnderstood, Is.True);
			Assert.That(answer.WholeRecord, Is.False);

			Assert.That(answer.From, Is.EqualTo("2026-01-01 00:00:00"));
			Assert.That(answer.To, Is.EqualTo("2026-12-31 23:59:59"));
			Assert.That(answer.From2, Is.EqualTo("2025-01-01 00:00:00"));
			Assert.That(answer.To2, Is.EqualTo("2025-12-31 23:59:59"),
				"La respuesta nombra los CUATRO limites con que contesto. Un limite que no vuelve es "
				+ "un periodo que quien lee no puede revisar.");

			Assert.That(answer.AnyCost, Is.EqualTo(journal.Api.HasAnyCostBetween(start, end)));
			Assert.That(answer.AnyCost, Is.True);

			Assert.That(
				answer.SaidInOneCurrency,
				Is.EqualTo(journal.Api.IsSpendSaidInOneCurrencyBetween(start, end)));
			Assert.That(answer.SaidInOneCurrency, Is.False,
				"El anio mezcla colones y dolares. Es un estado con nombre y no un error.");

			Assert.That(answer.Says, Is.EqualTo(journal.Api.DescribeSpendBetween(start, end)));
			Assert.That(
				answer.PerCurrencySays,
				Is.EqualTo(journal.Api.DescribeSpendPerCurrencyBetween(start, end)));
			Assert.That(
				answer.BothPeriodsSay,
				Is.EqualTo(journal.Api.DescribeTwoPeriodsOfSpendCompared(
					start, end, earlierStart, earlierEnd)));
			Assert.That(
				answer.CostPerDistanceCanBeSaid,
				Is.EqualTo(journal.Api.CanCostPerDistanceBeSaid(start, end)));
			Assert.That(
				answer.CostPerDistanceSays,
				Is.EqualTo(journal.Api.DescribeCostPerDistanceBetween(start, end)));
			Assert.That(
				answer.EntryCount, Is.EqualTo(journal.Api.CostEntryCountBetween(start, end)));
			Assert.That(answer.EntryCount, Is.EqualTo(3));
			Assert.That(answer.Entries, Has.Count.EqualTo(answer.EntryCount),
				"La cuenta y las filas son la misma cosa contada dos veces, y tienen que coincidir.");

			// LAS DOS VISTAS, POR VALOR Y ADENTRO DE LA MISMA FRASE. L6 en el borde del asistente.
			Assert.That(answer.Says, Does.Contain("Salieron ₡285000.00 del banco"));
			Assert.That(answer.Says, Does.Contain("el período consumió ₡"));
			Assert.That(answer.Says, Does.Contain("Salieron $120.50 del banco"));

			// Y NINGUN TOTAL ENTRE LAS DOS MONEDAS, que es L3 en el mismo borde.
			Assert.That(answer.Says, Does.Contain("2 monedas distintas"));
			Assert.That(answer.Says, Does.Contain("no hay un total entre ellas"));
			Assert.That(answer.Says, Does.Not.Contain("285120"));

			// LAS SEIS COLUMNAS, FILA POR FILA, CONTRA EL REGISTRO DE LA PUERTA. Se afirman por
			// separado para que un cruce diga CUAL columna perdio sus datos (C60).
			CostLedgerRecord record = journal.Api.CostEntriesBetween(start, end);

			for (int row = 0; row < record.Says.Length; row++)
			{
				Assert.That(answer.Entries[row].IncurredAt, Is.EqualTo(record.IncurredAt[row]));
				Assert.That(answer.Entries[row].Subject, Is.EqualTo(record.Subject[row]));
				Assert.That(answer.Entries[row].Money, Is.EqualTo(record.Money[row]));
				Assert.That(
					answer.Entries[row].Classification, Is.EqualTo(record.Classification[row]));
				Assert.That(answer.Entries[row].ExpectedLife, Is.EqualTo(record.ExpectedLife[row]));
				Assert.That(answer.Entries[row].Says, Is.EqualTo(record.Says[row]));
			}

			// Y EL DINERO LLEGA DICHO POR SU MONEDA, con DOS signos distintos: con una sola moneda en
			// el registro, una caratula que escribiera el signo fijo pasaria.
			Assert.That(answer.Entries[0].Money, Is.EqualTo("₡240000.00"));
			Assert.That(answer.Entries[2].Money, Is.EqualTo("$120.50"));
		}

		// NINGUNO DE LOS CUATRO PARAMETROS ES UNA MEDICION, Y SE MIRA POR LA FORMA Y NO POR EL NOMBRE.
		// Es §15 item 7: el gasto de un periodo tiene que ser contestable en este borde sin pedirle a
		// quien pregunta ni un monto, ni un odometro, ni una fecha de entrega.
		[Test]
		public void TheFourBoundsAreTextAndNotOneOfThemIsAFactAboutTheCar()
		{
			MethodInfo tool = typeof(CostLedgerTools).GetMethod("WhatIHaveSpentOnMyCar");

			Assert.That(tool, Is.Not.Null);

			var taken = new List<string>();

			foreach (ParameterInfo each in tool.GetParameters())
			{
				taken.Add(each.Name + ":" + each.ParameterType.Name);

				Assert.That(each.ParameterType, Is.EqualTo(typeof(string)),
					"'" + each.Name + "' dejo de ser una cadena. Un numero o un instante aca es una "
					+ "MEDICION: el asistente no es fuente de ningun hecho de un carro que nunca vio.");

				Assert.That(each.HasDefaultValue, Is.True,
					"'" + each.Name + "' dejo de tener valor por defecto, asi que preguntar sin "
					+ "nombrar un periodo dejo de ser posible.");
				Assert.That(each.DefaultValue, Is.EqualTo(""));
			}

			Assert.That(
				taken,
				Is.EqualTo(new[]
				{
					"from:String", "to:String", "from2:String", "to2:String",
				}),
				"El conjunto exacto de parametros de esta tool cambio. Los cuatro son claves de "
				+ "catalogo -- eligen CUAL pedazo del registro se pide y cual se compara contra cual -- "
				+ "y ninguno afirma nada de este carro.");
		}

		// UN DIA COMO LIMITE SUPERIOR LLEGA HASTA SU ULTIMO SEGUNDO, Y ESO SE MIDE CON DATOS. Es la
		// misma decision que OD-4 tomo para el ritmo, y aca el discriminador es el lavado de las 14:15:
		// con el limite a medianoche queda AFUERA y el periodo pasa de dos monedas a una.
		[Test]
		public void ADayAsTheUpperBoundReachesTheEndOfThatDayAndNotOnlyItsMidnight()
		{
			SpendOnThisCar toTheDay = TheAssistantAsks("2026-01-01", "2026-08-05");

			Assert.That(toTheDay.To, Is.EqualTo("2026-08-05 23:59:59"),
				"El limite superior dejo de llegar al final de su dia.");
			Assert.That(toTheDay.EntryCount, Is.EqualTo(3),
				"El lavado de las 14:15 del 5 de agosto quedo afuera, asi que un dia como limite no "
				+ "dura un dia.");
			Assert.That(toTheDay.SaidInOneCurrency, Is.False,
				"Y con el lavado adentro el periodo tiene DOS monedas, que es lo que hace que las dos "
				+ "formas del limite contesten cosas distintas y no solo cuenten distinto.");

			// EL CONTROL: un dia antes, el lavado NO esta y el periodo queda en una sola moneda.
			SpendOnThisCar theDayBefore = TheAssistantAsks("2026-01-01", "2026-08-04");

			Assert.That(theDayBefore.EntryCount, Is.EqualTo(2));
			Assert.That(theDayBefore.SaidInOneCurrency, Is.True);
		}

		// LOS CUATRO EN BLANCO CONTESTAN SOBRE TODO EL REGISTRO, Y LA COMPARACION ES EL REGISTRO CONTRA
		// SI MISMO. Es verdadero y dice poco, asi que detail lo dice: nombrar el segundo periodo es lo
		// que hace la comparacion util, y esta tool no inventa uno.
		[Test]
		public void WithEveryBoundBlankTheAnswerIsTheWholeRecordComparedWithItself()
		{
			SpendOnThisCar answer = TheAssistantAsks();

			Assert.That(answer.WholeRecord, Is.True);
			Assert.That(answer.From, Is.EqualTo("0001-01-01 00:00:00"));
			Assert.That(answer.To, Is.EqualTo("9999-12-31 23:59:59"));
			Assert.That(answer.From2, Is.EqualTo(answer.From),
				"Con el segundo par en blanco, el segundo periodo es todo el registro. Es la unica "
				+ "respuesta honesta: esta tool no deriva una ventana que nadie pidio.");
			Assert.That(answer.To2, Is.EqualTo(answer.To));

			Assert.That(answer.EntryCount, Is.EqualTo(3));
			Assert.That(answer.Detail, Does.Contain("nómbrelos"),
				"detail dejo de decir que para comparar dos periodos de verdad hay que nombrar el "
				+ "segundo. Sin eso, un modelo lee una comparacion vacia como si fuera un resultado.");

			// Y ES EXACTAMENTE EL PERIODO MAS ANCHO ESCRITO. Solo wholeRecord difiere, que es la
			// diferencia honesta: uno nombro un periodo y el otro no.
			SpendOnThisCar written =
				TheAssistantAsks("0001-01-01", "9999-12-31", "0001-01-01", "9999-12-31");

			Assert.That(written.From, Is.EqualTo(answer.From));
			Assert.That(written.To, Is.EqualTo(answer.To));
			Assert.That(written.Says, Is.EqualTo(answer.Says));
			Assert.That(written.EntryCount, Is.EqualTo(answer.EntryCount));
			Assert.That(written.WholeRecord, Is.False);
		}

		// UN LIMITE QUE NO DICE UN DIA SE CONTESTA EN PALABRAS Y NO SE PREGUNTA NADA. No es que no haya
		// gastos: es que no se eligio ningun periodo, y las dos cosas se leen igual si la respuesta no
		// las separa.
		[Test]
		public void ABoundThatDoesNotSayADayIsAnsweredInWordsAndNoReadIsAsked()
		{
			SpendOnThisCar answer = TheAssistantAsks("el martes pasado", "2026-12-31");

			Assert.That(answer.BoundsUnderstood, Is.False);
			Assert.That(answer.From, Is.EqualTo("el martes pasado"),
				"La respuesta muestra el texto que no se entendio, para que se vea cual fue.");
			Assert.That(answer.Says, Is.Empty);
			Assert.That(answer.PerCurrencySays, Is.Empty);
			Assert.That(answer.BothPeriodsSay, Is.Empty);
			Assert.That(answer.CostPerDistanceSays, Is.Empty);
			Assert.That(answer.EntryCount, Is.Zero);
			Assert.That(answer.Entries, Is.Empty);
			Assert.That(answer.Detail, Does.Contain("no se eligió ningún período"));

			// Y CUALQUIERA DE LOS CUATRO LO HACE, no solo el primero: los cuatro se parsean y el
			// resultado es la conjuncion de los cuatro.
			foreach (string[] each in new[]
			{
				new[] { "2026-01-01", "manana", "", "" },
				new[] { "2026-01-01", "2026-12-31", "ayer", "" },
				new[] { "2026-01-01", "2026-12-31", "", "el mes pasado" },
			})
				Assert.That(
					TheAssistantAsks(each[0], each[1], each[2], each[3]).BoundsUnderstood,
					Is.False,
					"Un limite que no dice un dia paso sin que nadie lo mirara: '"
					+ string.Join("' '", each) + "'.");
		}

		// EL CANAL LATERAL LLEVA INSTRUCCIONES Y NO LA FRASE DE LA PANTALLA, que es la inversion de E48
		// dicha por valor. Un modelo llena un hueco del que no le avisan; una persona va y pide el dato.
		[Test]
		public void TheSideChannelTellsTheModelWhatNotToDoAndNeverWhoToAskForIt()
		{
			SpendOnThisCar answer = TheAssistantAsks("2026-01-01", "2026-12-31");

			Assert.That(answer.Detail, Does.Contain("Diga las dos"),
				"detail dejo de decir que las dos vistas van juntas, que es la instruccion que impide "
				+ "que un modelo conteste con una sola.");
			Assert.That(answer.Detail, Does.Contain("no sume las filas"));
			Assert.That(answer.Detail, Does.Contain("MÁS DE UNA MONEDA"),
				"Con dos monedas, detail dice que no hay total entre ellas y que no se invente uno.");
			Assert.That(answer.Detail, Does.Contain("No invente un total"));

			// Y NO DICE QUIEN REGISTRA UN GASTO, que es lo que la PANTALLA si dice: una persona puede
			// ir a pedirlo y un modelo no.
			Assert.That(answer.Detail, Does.Not.Contain("pídale a"),
				"detail le dice al modelo a quien pedirle un gasto que falta. Esa es la frase de la "
				+ "pantalla: quien puede ir a pedirlo es la persona.");

			// EL CONTROL: la clausula de las dos monedas NO aparece en un periodo de una sola, asi que
			// detail habla de ESTE periodo y no es un parrafo fijo.
			Assert.That(
				TheAssistantAsks("2026-01-01", "2026-06-30").Detail,
				Does.Not.Contain("MÁS DE UNA MONEDA"));
		}

		// LA TOOL NO ESCRIBE NADA Y NO LEE NINGUN RELOJ. Las nueve lecturas del libro son consultas, y
		// el instrumento del reloj de esta caratula es el del ensamblado Mcp y no el de la pantalla.
		[Test]
		public void TheToolWritesNothingAndAsksNoClock()
		{
			long before = TheJournalLengthOf(journal.Folder);

			TheAssistantAsks("2026-01-01", "2026-12-31");
			TheAssistantAsks();
			TheAssistantAsks("el martes pasado", "");

			Assert.That(TheJournalLengthOf(journal.Folder), Is.EqualTo(before),
				"Preguntar en que se fue la plata hizo crecer el journal de esta persona. Las nueve "
				+ "lecturas son consultas: ninguna puede hacerlo crecer.");

			// EL CONTROL, y sin el lo de arriba lo cumple una medicion que no mide: un comando de
			// verdad sobre la misma carpeta SI la hace crecer.
			journal.Api.RecordRecurringCost(
				TheOilChangeIncurred, 1000m, TheColon, "Un gasto de control");

			Assert.That(TheJournalLengthOf(journal.Folder), Is.GreaterThan(before),
				"El journal no crecio ni con un comando, asi que la afirmacion de arriba esta verde "
				+ "por no estar mirando nada.");
		}

		// UNA CARPETA QUE LA APP NUNCA ABRIO NO ES UN ERROR, Y ESTA TOOL LO CONTESTA. Se afirma en su
		// propia fixture y no en AFolderTheAppNeverOpenedIsNotAnError, cuyo nombre dice "todas" y
		// alcanza ocho de doce desde SH-6: agregar un noveno caso ahi dejaria el nombre igual de falso
		// mientras parece un arreglo, y once tareas lo han declinado por esa razon.
		[Test]
		public void AFolderThisAppNeverOpenedAnswersThatThereIsNoDataYetInsteadOfThrowing()
		{
			string empty = System.IO.Path.Combine(
				System.IO.Path.GetTempPath(), "cl8-" + Guid.NewGuid().ToString("N"));

			System.IO.Directory.CreateDirectory(empty);

			try
			{
				SpendOnThisCar answer =
					new CostLedgerTools(new JournalReader(empty, "")).WhatIHaveSpentOnMyCar();

				Assert.That(answer.HasData, Is.False);
				Assert.That(answer.Detail, Does.Contain("no se ha abierto"));
				Assert.That(answer.Says, Is.Empty);
				Assert.That(answer.Entries, Is.Empty);
				Assert.That(answer.EntryCount, Is.Zero);

				// Y LO QUE SE PREGUNTO VUELVE IGUAL, para que el asistente sepa de que estaba hablando
				// aunque no haya datos.
				Assert.That(answer.From, Is.EqualTo("0001-01-01 00:00:00"));
				Assert.That(answer.WholeRecord, Is.True);
			}
			finally
			{
				System.IO.Directory.Delete(empty, true);
			}
		}

		// ===== EL APAREJO =====================================================================

		private SpendOnThisCar TheAssistantAsks(
			string from = "", string to = "", string from2 = "", string to2 = "")
		{
			return new CostLedgerTools(new JournalReader(journal.Folder, ""))
				.WhatIHaveSpentOnMyCar(from, to, from2, to2);
		}

		// Cuanto pesa el journal de esa carpeta, sumado sobre todos sus archivos.
		// CUANTO PESA EL JOURNAL DE ESE VEHICULO, y el SUJETO es journal/*.bin y no la carpeta entera.
		// MEDIDO EN ESTA MISMA TAREA: sumar todos los archivos del arbol da 1691 -> 1721 con solo TRES
		// preguntas, porque abrir un segundo host de solo lectura sobre la misma carpeta toca
		// contabilidad del motor que vive FUERA de journal/. Esa contabilidad no es el sujeto de esta
		// ley: lo que ninguna lectura puede hacer crecer es el journal. Es el mismo sujeto que mira
		// TheAssistantOnlyEverReadsTheJournal, re-derivado aca y no copiado de memoria.
		private static long TheJournalLengthOf(string folder)
		{
			long bytes = 0;

			string journalFolder = System.IO.Path.Combine(
				folder, DealershipCarApp.TheActorFor(TheVin), "journal");

			foreach (string file in System.IO.Directory.GetFiles(journalFolder, "*.bin"))
				bytes += new System.IO.FileInfo(file).Length;

			return bytes;
		}
	}
}
