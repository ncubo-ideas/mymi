using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DealershipCar.App;
using DealershipCar.Wasm;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// EL BLOQUE "CUANTO SE MANEJA", QUE ES EL CASO MAS FILOSO DEL BARRIDO DE ARITMETICA. Una tabla de
	// lecturas al lado de un ritmo es exactamente donde alguien escribe una division en un .razor:
	// tiene los dos extremos, tiene el tiempo, y la cuenta cabe en una linea. El brief lo dice de si
	// mismo en §7 y PLAN-BuildOrder.md §2.2 nombra a OD-5 entre las tres tareas que deben un control
	// negativo.
	//
	// DOS INSTRUMENTOS, Y CADA UNO ATRAPA LO QUE EL OTRO NO VE:
	//
	//   el BARRIDO defiende los NUMEROS -- una resta o una division entre dos campos del portador;
	//   la afirmacion LETRA POR LETRA defiende la FRASE -- una rival armada por CONCATENACION, que
	//   el barrido no ve nunca porque un '+' con un operando de texto es concatenacion y no una cuenta.
	//
	// SH-6 §11 item 2, MS-7 §2, MS-8 §7 y OD-4 falsificacion 4 midieron ese punto ciego cuatro veces,
	// desde cuatro sitios. Este bloque lo mide una quinta y trae los dos instrumentos.
	//
	// LA VENTANA ES DERIVADA, Y ES LO CONTRARIO DE LO QUE DECIDIO EL TABLERO DE LA CONSOLA. Alla los
	// limites son momentos ESCRITOS de la historia, porque una fecha derivada de otra deja de decir
	// cual es. Aca no hay ningun momento escrito que nombrar: P-11 pide los doce meses que terminan en
	// el instante en que la pantalla pregunta, y ese instante lo lee la pantalla de su propio reloj.
	// Un literal seria una fecha que envejece al dia siguiente y contestaria por un periodo que la
	// persona no pidio. Y el barrido no defiende ninguna de las dos cosas: AddYears(-1) pasa limpio,
	// porque el detector pide un caracter de operando antes del operador y encuentra un parentesis
	// (OD-3 §3). Asi que la ventana se fija POR VALOR, aca abajo.
	[TestFixture]
	public sealed class TheUseBlockPrintsTheRateSentenceWholeAndTheRowsBesideItAndNothingDividesThem
	{
		private const string TheVin = "5YJ3E1EA7JF006487";

		private const string TheSection = "Historial.razor";

		private const string TheUnitOfTheRecord = "UnitA";
		private const string TheForeignUnit = "UnitB";

		// LA VENTANA DE ARRIBA: los doce meses que terminan en el instante en que la pantalla pregunta.
		private static readonly DateTime TheWindowFrom =
			new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheWindowTo =
			new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc);

		// LA DE ABAJO: los doce anteriores. Su limite de arriba ES el limite de abajo de la otra, que
		// es como el tablero de la consola parte sus dos mitades: comparten la bisagra.
		private static readonly DateTime TheEarlierWindowFrom =
			new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheEarlierWindowTo = TheWindowFrom;

		// LAS CINCO LECTURAS. DOS caen en la ventana anterior y TRES en la de arriba, asi que los dos
		// periodos tienen ritmo y la comparacion SI dice un cambio -- que es lo que ninguna corrida de
		// la consola alcanza (OD-3 §12 item 1).
		private static readonly DateTime TheFirstOfTheEarlierWindow =
			new DateTime(2025, 3, 1, 8, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheLastOfTheEarlierWindow =
			new DateTime(2025, 9, 1, 8, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheFirstOfTheWindow =
			new DateTime(2026, 1, 10, 8, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheMiddleOfTheWindow =
			new DateTime(2026, 3, 10, 8, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheLastOfTheWindow =
			new DateTime(2026, 6, 15, 8, 0, 0, DateTimeKind.Utc);

		// UN MES CON UNA SOLA LECTURA Y UN ANIO CON NINGUNA, para las dos suspensiones que este mismo
		// registro alcanza.
		private static readonly DateTime TheLoneReadingMonthFrom =
			new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheLoneReadingMonthTo =
			new DateTime(2026, 6, 30, 23, 59, 59, DateTimeKind.Utc);

		private static readonly DateTime TheEmptyYearFrom =
			new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheEmptyYearTo =
			new DateTime(2024, 12, 31, 23, 59, 59, DateTimeKind.Utc);

		// EL INSTANTE ESCRITO CON QUE SE MIDE LA REGLA DE LA VENTANA. Cae en enero a proposito: los
		// veinticuatro meses antes de el atraviesan el 29 de febrero de 2024, asi que doce meses de
		// calendario y 730 dias NO dan el mismo dia, y la eleccion se puede medir.
		private static readonly DateTime TheInstantTheWindowIsMeasuredFrom =
			new DateTime(2026, 1, 15, 9, 30, 0, DateTimeKind.Utc);

		private AScheduleOnARealJournal journal;

		[SetUp]
		public void Setup()
		{
			journal = new AScheduleOnARealJournal(TheVin, AppState.TheProfileAtDelivery);
		}

		[TearDown]
		public void TearDown()
		{
			journal.Dispose();
		}

		// LAS CUATRO RESPUESTAS DEL PERIODO DE ARRIBA, CADA UNA CONTRA LA DE LA PUERTA Y POR VALOR, y
		// los cuatro limites devueltos tal como se preguntaron. Un limite que la respuesta no nombra es
		// un periodo que quien lee no puede revisar.
		[Test]
		public void TheFourBoundsComeBackNamedAndTheFourAnswersAreTheDoorsOwn()
		{
			TheFiveReadings();

			HowThisVehicleIsDriven use = WhatTheScreenIsHanded();

			Assert.That(use.From, Is.EqualTo("2026-01-01 00:00:00Z"));
			Assert.That(use.To, Is.EqualTo("2026-12-31 23:59:59Z"));
			Assert.That(use.EarlierFrom, Is.EqualTo("2025-01-01 00:00:00Z"));
			Assert.That(use.EarlierTo, Is.EqualTo("2026-01-01 00:00:00Z"),
				"El limite de arriba del periodo anterior ES el de abajo del de arriba: las dos "
				+ "ventanas son contiguas y comparten la bisagra.");

			Assert.That(use.CanBeSaid, Is.True,
				"CanBeSaid: el periodo de arriba tiene tres lecturas en una sola unidad.");

			Assert.That(use.Standing, Is.EqualTo("Measured"),
				"Standing proyecta el NOMBRE del miembro del conjunto cerrado.");

			Assert.That(
				use.Says,
				Is.EqualTo(journal.Api.DescribeRateOfUse(TheWindowFrom, TheWindowTo)),
				"La frase del ritmo es la de la puerta LETRA POR LETRA. Es lo unico que atrapa una "
				+ "frase rival armada en la caratula: el barrido de aritmetica no la ve, porque una "
				+ "suma con un operando de texto es concatenacion y nunca se marca.");

			Assert.That(use.ReadingCount, Is.EqualTo(3),
				"ReadingCount: las TRES lecturas del periodo de arriba, y no las cinco del registro.");
		}

		// Y LA COMPARACION, QUE ES LA SEXTA LECTURA DE LA SERIE Y LLEGA A UNA SUPERFICIE POR PRIMERA
		// VEZ. Ninguna tool del asistente la alcanza -- toma CUATRO limites y la tool tiene dos -- y la
		// consola la imprime sin cambio, porque su historia no manda ningun hecho del odometro despues
		// de la primera evaluacion. Aca los dos periodos tienen ritmo, asi que el CAMBIO se dice.
		[Test]
		public void TheComparisonIsTheDoorsOwnAndThisIsTheFirstSurfaceWhereAChangeIsPrinted()
		{
			TheFiveReadings();

			HowThisVehicleIsDriven use = WhatTheScreenIsHanded();

			Assert.That(
				use.BothPeriodsSay,
				Is.EqualTo(journal.Api.DescribeTwoPeriodsOfUseCompared(
					TheWindowFrom, TheWindowTo, TheEarlierWindowFrom, TheEarlierWindowTo)),
				"La frase de los dos periodos es la de la puerta letra por letra, y con los CUATRO "
				+ "limites en el orden en que se preguntaron.");

			Assert.That(
				use.BothPeriodsSay,
				Does.Contain("Primer periodo").And.Contains("Segundo periodo"),
				"Nombra los dos periodos: un cambio suelto es la mitad de una respuesta compuesta.");

			Assert.That(
				use.BothPeriodsSay,
				Does.Contain("El cambio del primero respecto al segundo: un ").And.Contains("% más."),
				"Y DICE EL CAMBIO. Es la primera superficie de este repo que lo imprime, y esta "
				+ "afirmacion es lo que separa 'la comparacion llego' de 'la comparacion contesto'.");

			// Y EL ORDEN NO ES DECORACION: el dominio dice el cambio del PRIMER periodo respecto al
			// SEGUNDO, asi que cruzar los dos pares da la frase al reves.
			Assert.That(
				use.BothPeriodsSay,
				Is.Not.EqualTo(journal.Api.DescribeTwoPeriodsOfUseCompared(
					TheEarlierWindowFrom, TheEarlierWindowTo, TheWindowFrom, TheWindowTo)),
				"Cruzar los dos pares da la misma frase, asi que el orden de los cuatro limites no "
				+ "esta defendido por nada.");
		}

		// CADA FILA CON SUS PROPIAS COLUMNAS Y CON SU PROPIA FRASE, y la frase NOMBRA SU FUENTE. L5 es
		// una ley de divulgacion: la fuente se dice, y nada se pesa ni se descarta por ella.
		[Test]
		public void EveryRowKeepsItsOwnColumnsAndItsOwnSentenceNamesItsSource()
		{
			TheFiveReadings();

			HowThisVehicleIsDriven use = WhatTheScreenIsHanded();

			Assert.That(use.Rows, Has.Count.EqualTo(3));

			Assert.That(
				use.Rows.Select(each => each.Value).ToArray(),
				Is.EqualTo(new[] { 20000, 22000, 26000 }),
				"Los tres valores, por valor y en orden de observacion.");

			Assert.That(
				use.Rows.Select(each => each.ObservedAt).ToArray(),
				Is.EqualTo(new[]
				{
					"2026-01-10 08:00:00", "2026-03-10 08:00:00", "2026-06-15 08:00:00",
				}),
				"Los instantes llegan con su HORA: un instante nunca se dice como un dia pelado.");

			Assert.That(
				use.Rows.Select(each => each.Source).ToArray(),
				Is.EqualTo(new[] { "Telemetry", "HumanEntry", "CustomerDeclared" }),
				"La columna de fuente es la mitad DIRECCIONABLE, por el nombre del miembro.");

			foreach (AReadingOnTheRecord each in use.Rows)
				Assert.That(each.Unit, Is.EqualTo("km"),
					"Y la unidad por su nombre de mundo real, que es lo que la persona lee.");

			// LA FUENTE VA DICHA ADENTRO DE LA FRASE DE LA FILA, EN LAS PALABRAS DEL DOMINIO, y por eso
			// la pantalla no tiene tabla propia para este conjunto cerrado. El dia que el dominio deje
			// de decirla, el silencio de la pantalla es lo que se pone rojo aca.
			Assert.That(use.Rows[0].Says, Does.Contain("reportada por el vehículo"));
			Assert.That(use.Rows[1].Says, Does.Contain("anotada a mano"));
			Assert.That(use.Rows[2].Says, Does.Contain("declarada por la persona"));

			foreach (int row in Enumerable.Range(0, use.Rows.Count))
				Assert.That(
					use.Rows[row].Says,
					Is.EqualTo(journal.Api.ReadingsBetween(TheWindowFrom, TheWindowTo).Says[row]),
					"Y cada frase es la de la puerta letra por letra.");
		}

		// LA REGLA 4 DE LA CONVENCION: la cuenta que compone el dominio y las filas se ponen de
		// acuerdo. No porque la caratula pueda sumarlas -- no puede -- sino para que nunca sea util
		// intentarlo.
		[Test]
		public void TheDomainsCountAndTheRowsAgreeAndTheEndsOfTheRateAreTheFirstAndLastRow()
		{
			TheFiveReadings();

			HowThisVehicleIsDriven use = WhatTheScreenIsHanded();

			Assert.That(use.ReadingCount, Is.EqualTo(use.Rows.Count),
				"La cuenta del dominio y el numero de filas no coinciden.");

			Assert.That(use.Says, Does.Contain(use.Rows[0].Value + " " + use.Rows[0].Unit),
				"El extremo de abajo de la frase del ritmo es la PRIMERA fila del mismo periodo.");

			Assert.That(
				use.Says,
				Does.Contain(use.Rows[use.Rows.Count - 1].Value + " " + use.Rows[use.Rows.Count - 1].Unit),
				"Y el de arriba es la ULTIMA. Los extremos y las filas son dos lecturas de la misma "
				+ "puerta y tienen que decir lo mismo.");
		}

		// LA FRASE YA DICE LO QUE DICEN CanBeSaid Y Standing, MEDIDO EN LAS CINCO VARIANTES. Es lo que
		// decide que el bloque dibuje la frase y no los dos campos: para un modelo, dos campos siempre
		// presentes son lo correcto, porque de un campo solo no puede saber que pregunta contesto; para
		// una persona que lee UNA columna, ponerlos al lado repite la misma clausula dos veces.
		[Test]
		public void TheSentenceAlreadySaysWhatCanBeSaidAndTheStandingSayInAllFiveVariants()
		{
			TheFiveReadings();

			// MEDIDA: la variante que SI tiene ritmo no dice que no se pueda decir, y dice el ritmo.
			Assert.That(journal.Api.CanARateOfUseBeSaid(TheWindowFrom, TheWindowTo), Is.True);
			Assert.That(
				journal.Api.DescribeRateOfUse(TheWindowFrom, TheWindowTo),
				Does.Not.Contain("no se puede decir").And.Contains(" por día"));

			// Y LAS CUATRO SUSPENSIONES DICEN LAS DOS COSAS: que no se puede decir, y CUAL es la razon
			// -- que es exactamente lo que el nombre del miembro nombra.
			foreach (ASuspensionAsItWasRead each in TheFourSuspensionsAndTheClauseEachOneSays())
			{
				Assert.That(each.CanBeSaid, Is.False, "CanBeSaid de la variante " + each.Variant);

				Assert.That(each.Says, Does.Contain("no se puede decir cuánto se maneja"),
					"La variante " + each.Variant + " no dice en palabras que no se puede decir, asi "
					+ "que el campo CanBeSaid seria la unica forma de saberlo y el bloque tendria que "
					+ "dibujarlo.");

				Assert.That(each.Says, Does.Contain(each.Clause),
					"La variante " + each.Variant + " no dice su propia razon, asi que el campo "
					+ "Standing seria la unica forma de saberla.");
			}
		}

		// Y EL BLOQUE NO DIBUJA NINGUNO DE LOS DOS, que es la otra mitad de lo de arriba. Los dos
		// siguen en el portador -- la superficie los ofrece siempre -- y la pantalla no los pinta.
		[Test]
		public void AndTheBlockPaintsNeitherOfThemBecauseTheSentenceCarriesBoth()
		{
			string markup = TheSectionAsItIsWritten();

			foreach (string field in new[] { "Use.CanBeSaid", "Use.Standing" })
				Assert.That(markup, Does.Not.Contain(field),
					"El bloque pinta '" + field + "' al lado de una frase que ya lo dice. La misma "
					+ "clausula dos veces en una columna se lee como un error.");

			// Y EL CONTROL: el portador SI los lleva. Sin este renglon, lo de arriba se cumpliria
			// tambien borrandolos de la superficie, que es lo contrario de lo que hay que hacer.
			Assert.That(typeof(HowThisVehicleIsDriven).GetProperty("CanBeSaid"), Is.Not.Null);
			Assert.That(typeof(HowThisVehicleIsDriven).GetProperty("Standing"), Is.Not.Null);
		}

		// EL ENCABEZADO NOMBRA LA VENTANA PORQUE LA FRASE DEL DOMINIO NO LA NOMBRA, y eso se MIDIO en
		// los dos estados del registro antes de escribirlo. Es la tercera vez que P-11 se mide y la
		// respuesta es distinta de la del bloque de estadias: alla la frase del periodo nombraba sus
		// dos limites en los dos estados, asi que el encabezado nombraba el bloque y nada mas.
		[Test]
		public void TheHeadingNamesTheWindowBecauseTheRateSentenceOnlyNamesItInOneOfTheTwoStates()
		{
			// ESTADO UNO: con lecturas. La frase nombra los dos EXTREMOS -- que son lecturas -- y NO
			// nombra ninguno de los dos limites de la ventana.
			TheFiveReadings();

			string measured = journal.Api.DescribeRateOfUse(TheWindowFrom, TheWindowTo);

			Assert.That(measured, Does.Contain("2026-01-10 08:00:00"),
				"La frase medida nombra el instante de su primera lectura.");
			Assert.That(measured, Does.Not.Contain("2026-01-01 00:00:00"),
				"Y NO nombra el limite de abajo de la ventana.");
			Assert.That(measured, Does.Not.Contain("2026-12-31 23:59:59"),
				"Ni el de arriba. Asi que si el encabezado no dice la ventana, nadie la dice.");

			// ESTADO DOS: sin ninguna lectura. Ahi SI nombra los dos limites, que es lo que hace de
			// esto una medicion y no una preferencia: la frase nombra la ventana en UNO de los dos
			// estados, asi que el encabezado tiene que nombrarla en los dos.
			string absent = journal.Api.DescribeRateOfUse(TheEmptyYearFrom, TheEmptyYearTo);

			Assert.That(
				absent,
				Does.Contain("2024-01-01 00:00:00").And.Contains("2024-12-31 23:59:59"),
				"La frase de la ausencia SI nombra los dos limites del periodo.");

			// Y EL ENCABEZADO LOS DICE, LOS CUATRO.
			string markup = TheSectionAsItIsWritten();

			foreach (string bound in new[]
			{
				"@Use.From", "@Use.To", "@Use.EarlierFrom", "@Use.EarlierTo",
			})
				Assert.That(markup, Does.Contain(bound),
					"El bloque no dice '" + bound + "'. P-11 pide que la ventana este nombrada, y la "
					+ "frase del ritmo no la nombra cuando hay lecturas, que es el caso normal.");
		}

		// LA VENTANA SE DERIVA DE UN SOLO INSTANTE, Y LA REGLA SE FIJA POR VALOR. El barrido de
		// aritmetica no la defiende -- AddYears(-1) pasa limpio, medido en OD-3 -- asi que esto es el
		// unico instrumento que hay.
		[Test]
		public void TheWindowIsTwelveCalendarMonthsAndTheOneBeforeItIsTwelveMore()
		{
			Assert.That(
				TheWindowRule("TheTwelveMonthsBefore", TheInstantTheWindowIsMeasuredFrom),
				Is.EqualTo(new DateTime(2025, 1, 15, 9, 30, 0, DateTimeKind.Utc)),
				"El comienzo de la ventana de arriba son DOCE MESES DE CALENDARIO antes del instante "
				+ "en que la pantalla pregunta.");

			Assert.That(
				TheWindowRule("TheTwentyFourMonthsBefore", TheInstantTheWindowIsMeasuredFrom),
				Is.EqualTo(new DateTime(2024, 1, 15, 9, 30, 0, DateTimeKind.Utc)),
				"Y el de la anterior, veinticuatro.");

			// EL CONTROL QUE HACE DE ESTO UNA MEDICION: contar 730 dias en vez de veinticuatro meses da
			// OTRO DIA, porque el 29 de febrero de 2024 cae adentro. Sin este renglon las dos formas
			// serian indistinguibles y la afirmacion de arriba no mediria nada.
			Assert.That(
				TheInstantTheWindowIsMeasuredFrom.AddDays(-730),
				Is.Not.EqualTo(TheInstantTheWindowIsMeasuredFrom.AddYears(-2)),
				"Los datos de esta prueba dejaron de separar los doce meses de calendario de una "
				+ "cuenta de dias, asi que la afirmacion de arriba se cumple sola.");
		}

		// EL PORTADOR TOMA LOS CUATRO LIMITES YA CALCULADOS Y NINGUN RELOJ. La derivacion vive en la
		// caratula, que es el unico sitio del ensamblado que puede leer el instante de la LECTURA: es
		// privado y estatico en AppState. Un DateTime sin argumentos aca seria un cuarto reloj.
		[Test]
		public void TheCarrierTakesTheFourBoundsAlreadyComputedAndReadsNoClock()
		{
			MethodInfo read = typeof(HowThisVehicleIsDriven).GetMethod("ReadThrough");

			Assert.That(read, Is.Not.Null, "El portador dejo de ofrecer ReadThrough.");

			ParameterInfo[] taken = read.GetParameters();

			Assert.That(
				taken.Select(each => each.ParameterType.Name).ToArray(),
				Is.EqualTo(new[] { "DealershipCarAPI", "DateTime", "DateTime", "DateTime", "DateTime" }),
				"El portador toma la puerta y los CUATRO limites. Un instante de mas seria un segundo "
				+ "momento que se puede separar del primero.");

			Assert.That(
				taken.Select(each => each.Name).ToArray(),
				Is.EqualTo(new[] { "api", "from", "to", "earlierFrom", "earlierTo" }),
				"Y en ESE orden: el dominio dice el cambio del PRIMER periodo respecto al SEGUNDO.");

			var handingOutAnInstant = new List<string>();

			foreach (MethodInfo each in typeof(HowThisVehicleIsDriven).GetMethods(
				BindingFlags.Instance | BindingFlags.Static
				| BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
				if (each.ReturnType == typeof(DateTime) && each.GetParameters().Length == 0)
					handingOutAnInstant.Add(each.Name);

			Assert.That(handingOutAnInstant, Is.Empty,
				"El portador lee un reloj. La ventana la fija la caratula con el instante de la "
				+ "LECTURA, que ya existe, y el portador recibe los cuatro limites ya calculados.");
		}

		// Y LA PANTALLA NO LE PIDE NINGUNO DE LOS CUATRO A NADIE, NI AGREGA UN CUARTO RELOJ. Un
		// parametro aca seria un selector de periodo, y esta app no tiene ningun control que le pida un
		// valor a una persona.
		[Test]
		public void TheShellsReadOfTheUseTakesNoArgumentAndAddsNoFourthClock()
		{
			MethodInfo read = typeof(AppState).GetMethod("ReadHowThisVehicleIsDriven");

			Assert.That(read, Is.Not.Null,
				"La caratula dejo de ofrecer el ritmo de uso y la seccion Historial no tiene de donde "
				+ "sacarlo.");

			Assert.That(read.GetParameters(), Is.Empty,
				"La lectura de la pantalla gano un argumento. La ventana la fija la pantalla con su "
				+ "propio reloj; pedirla seria dejar que alguien pregunte por un periodo que se invento.");

			Assert.That(read.ReturnType, Is.EqualTo(typeof(HowThisVehicleIsDriven)));

			var handingOutAnInstant = new List<string>();

			foreach (MethodInfo each in typeof(AppState).GetMethods(
				BindingFlags.Instance | BindingFlags.Static
				| BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
				if (each.ReturnType == typeof(DateTime) && each.GetParameters().Length == 0)
					handingOutAnInstant.Add(each.Name);

			Assert.That(handingOutAnInstant, Has.Count.EqualTo(3),
				"El bloque del ritmo agrego un miembro que entrega un instante. No hacia falta: el "
				+ "instante de la LECTURA ya existe y de el salen los cuatro limites.");
		}

		// LOS CUATRO LIMITES SALEN DE UNA SOLA LECTURA DEL RELOJ, y eso se mira sobre la FUENTE porque
		// no sobrevive a la compilacion. Dos llamadas al reloj serian dos momentos, y las dos ventanas
		// podrian quedar separadas por el segundo que pasa entre ellas.
		[Test]
		public void TheFourBoundsAllComeFromOneReadOfTheScreensClock()
		{
			string shell = TheShellAsItIsWritten();

			Assert.That(
				Squeezed(shell),
				Does.Contain(
					"public HowThisVehicleIsDriven ReadHowThisVehicleIsDriven() { Require(); "
					+ "DateTime asOf = TheInstantTheScreenAsksAt(); "
					+ "return HowThisVehicleIsDriven.ReadThrough( api, TheTwelveMonthsBefore(asOf), "
					+ "asOf, TheTwentyFourMonthsBefore(asOf), TheTwelveMonthsBefore(asOf));"),
				"La lectura del ritmo dejo de derivar sus cuatro limites de UN solo asOf. Dos lecturas "
				+ "del reloj son dos momentos, y las dos ventanas quedarian separadas por lo que pase "
				+ "entre ellas.");
		}

		// NINGUNA LINEA DEL BLOQUE LLEVA UN OPERADOR ARITMETICO. Es el control negativo de §2.2 hecho
		// prueba: se escribe una linea que divide dos campos del portador, esta se pone roja con el
		// nombre del archivo, se borra, y vuelve a verde. Y se escribe CON ESPACIOS alrededor del
		// operador, porque en un .razor el guion pide espacio de un lado -- ahi la mitad del archivo es
		// markup y un guion es parte de un nombre.
		[Test]
		public void NoLineOfTheUseBlockHoldsAnArithmeticOperator()
		{
			List<ArithmeticHit> found = TheCodeOfTheShellsAsData.TheArithmeticInSource(
				"Wasm", "Sections/" + TheSection, TheSectionAsItIsWritten());

			Assert.That(found, Is.Empty, TheHitsInWords(found));

			// EL CONTROL DEL DETECTOR SOBRE ESTE ARCHIVO, en las DOS formas del guion. Sin el, lo de
			// arriba estaria vacio por no detectar y no por no haber.
			foreach (string shape in new[]
			{
				"<div>@(Use.ReadingCount / Use.Rows.Count)</div>",
				"<div>@(Use.ReadingCount - Use.Rows.Count)</div>",
			})
				Assert.That(
					TheCodeOfTheShellsAsData.TheArithmeticInSource("Wasm", "Sections/" + TheSection, shape),
					Is.Not.Empty,
					"El detector no ve '" + shape + "' en un .razor, asi que la afirmacion de arriba "
					+ "esta verde por no detectar.");

			// Y LA FORMA QUE NO VE, DICHA AQUI TAMBIEN: la resta PEGADA. En un .razor el guion es parte
			// de un nombre -- aria-label, d-pill--on -- asi que solo cuenta con espacio de un lado.
			// Quien escriba el control negativo tiene que escribirlo espaciado o pasa por la razon
			// equivocada.
			Assert.That(
				TheCodeOfTheShellsAsData.TheArithmeticInSource(
					"Wasm", "Sections/" + TheSection, "<div>@(Use.ReadingCount-Use.Rows.Count)</div>"),
				Is.Empty,
				"El detector empezo a ver una resta pegada en un .razor. Es una buena noticia y esta "
				+ "prueba es la que hay que borrar, no la que hay que arreglar.");
		}

		// LA SECCION PINTA LAS FRASES DEL DOMINIO Y NINGUNA PROPIA. Es el segundo instrumento, y es el
		// unico que atrapa una frase rival armada por CONCATENACION: el barrido de arriba no la ve
		// nunca, porque un '+' con un operando de texto es concatenacion y no una cuenta.
		[Test]
		public void TheUseBlockPaintsTheDomainsSentencesAndComposesNoRivalOfItsOwn()
		{
			string markup = TheSectionAsItIsWritten();

			foreach (string sentence in new[] { "@Use.Says", "@Use.BothPeriodsSay", "@lectura.Says" })
				Assert.That(markup, Does.Contain(sentence),
					"El bloque dejo de pintar '" + sentence + "'. La frase la compone el dominio: una "
					+ "que la caratula arme con pedazos de las otras sale con la procedencia del "
					+ "registro encima de algo que el registro no dijo.");

			foreach (string forbidden in new[]
			{
				"Use.Says +", "+ Use.Says", "BothPeriodsSay +", "+ BothPeriodsSay",
				"lectura.Says +", "+ lectura.Says",
			})
				Assert.That(markup, Does.Not.Contain(forbidden),
					"El bloque concatena algo con una frase del dominio ('" + forbidden + "'). Lo que "
					+ "sale de la puerta se cita letra por letra o no se cita.");
		}

		// NINGUN CONTROL DEL BLOQUE RECOGE UN VALOR DE UNA PERSONA, Y NO HAY COMO MOVER LA VENTANA. Un
		// selector de periodo no tiene por que ser un <input>: un boton que dijera "el ano pasado"
		// pondria a la persona a elegir por que periodo se pregunta.
		[Test]
		public void TheUseBlockCollectsNoValueFromAPersonAndOffersNoWayToMoveItsWindow()
		{
			string markup = TheSectionAsItIsWritten();

			foreach (string shape in new[] { "<input", "<select", "<textarea", "<form", "@bind-Value" })
				Assert.That(markup, Does.Not.Contain(shape),
					"La seccion Historial lleva un control que recoge un valor ('" + shape + "'). La "
					+ "app del dueno es de SOLO LECTURA y eso trata de quien DICE un hecho.");

			foreach (string shape in new[] { "<button", "@onclick" })
				Assert.That(markup, Does.Not.Contain(shape),
					"La seccion Historial gano un control que se puede apretar ('" + shape + "'). La "
					+ "ventana es fija a proposito: elegir el periodo es elegir la pregunta.");
		}

		// EL CONJUNTO CERRADO DE LAS CINCO SITUACIONES DEL RITMO NO SE ESCRIBE DOS VECES. La pantalla
		// no nombra ni un solo miembro: pinta la frase, que ya dice cual es. Por eso no entra a
		// TheClosedSetsInTheBrowserAreTheDomainsOwn, que es donde viven los conjuntos que la caratula
		// SI copia -- y lo que cuesta copiar uno esta medido: la palabra de la pantalla y la del
		// dominio ya no coinciden para dos de las nueve clases de operacion.
		[Test]
		public void TheShellNamesNoMemberOfTheRateStandingSetAnywhere()
		{
			foreach (string member in new[]
			{
				"Measured", "SuspendedByAUnitMismatch", "SuspendedByASingleReading",
				"SuspendedByAnAbsentReading", "SuspendedByAZeroLengthSpan",
			})
			{
				Assert.That(TheSectionAsItIsWritten(), Does.Not.Contain(member),
					"El bloque nombra el miembro '" + member + "'. Nombrar uno escribe el conjunto "
					+ "cerrado por segunda vez, y a partir de ahi las dos copias pueden separarse.");

				Assert.That(TheCarrierAsItIsWritten(), Does.Not.Contain(member),
					"El portador nombra el miembro '" + member + "'.");
			}
		}

		// Y LA RAMA VACIA DICE QUIEN MANDA UNA LECTURA. Esta prueba se escribio DESPUES de que la
		// falsificacion encontrara que nada lo veia: MEDIDO, borrar esa linea del markup dejo la suite
		// ENTERA verde, porque el censo de TheOwnerIsTold cuenta las frases DECLARADAS y no las
		// PINTADAS. Es la misma forma que la linea de registro de una tool que nada defiende.
		//
		// Y LA FRASE HACE FALTA porque ninguna del dominio la dice: la del ritmo dice que no se puede
		// decir y por que, y "no hay ninguna lectura" no es "el carro no se movio" -- confundirlas es
		// el cero que la ley de una sola lectura prohibe, dicho por la pantalla.
		[Test]
		public void TheEmptyBranchSaysWhoSendsAReadingAndNeverThatTheCarDidNotMove()
		{
			string markup = TheSectionAsItIsWritten();

			const string line = "TheOwnerIsTold.TheOdometerReadingsAreSentByTheDealershipAndTheVehicle";

			Assert.That(TimesItSays(markup, line), Is.EqualTo(1),
				"El bloque no pinta quien manda una lectura, o la pinta en mas de un sitio. Declararla "
				+ "no la pone en la pantalla: el censo de TheOwnerIsTold cuenta las frases escritas y "
				+ "no las pintadas.");

			Assert.That(
				markup.IndexOf(line, StringComparison.Ordinal),
				Is.GreaterThan(markup.IndexOf("id=\"sin-lecturas\"", StringComparison.Ordinal)),
				"La frase quedo fuera de la rama vacia, que es la unica en que hace falta: con filas "
				+ "en pantalla, quien manda las lecturas se ve en cada una.");

			Assert.That(
				TheOwnerIsTold.TheOdometerReadingsAreSentByTheDealershipAndTheVehicle,
				Does.Contain("no quiere decir que el carro no se haya movido"),
				"La frase dejo de decir la lectura equivocada que existe para impedir.");

			Assert.That(
				TheOwnerIsTold.TheOdometerReadingsAreSentByTheDealershipAndTheVehicle,
				Does.Contain("no hay nada que tengas que llenar vos"),
				"Y dejo de decir que no hay nada que la persona ponga, que es la mitad que un "
				+ "formulario contradice.");
		}

		// EL PORTADOR NO SE FUSIONO CON NINGUNA DE LAS DOS HOJAS QUE YA ESTABAN, y es la quinta vez que
		// esa prohibicion se aplica. La de MS-7 esta tipada sobre SUS dos colecciones y sobre CUATRO
		// fragmentos de nombre suyos, asi que no ve nada de este bloque: un barrido solo alcanza lo que
		// enumera, y esto enumera lo de esta tarea.
		//
		// LA RAZON ES LA CLAVE Y NO EL TAMANIO: la ficha lee un catalogo con clave de MODELO, la mitad
		// del vehiculo se mide contra el SOBRE al instante preguntado, y esto es un PERIODO. Tres
		// preguntas con tres claves son tres portadores, y fusionarlos volveria falso el nombre de uno.
		[Test]
		public void TheUseCarrierWasMergedIntoNeitherTheSheetNorTheVehicleHalf()
		{
			foreach (Type older in new[]
			{
				typeof(WhatTheManufacturerPublished), typeof(WhereThisVehicleStands),
			})
				foreach (PropertyInfo each in older.GetProperties())
				{
					Assert.That(
						each.PropertyType,
						Is.Not.EqualTo(typeof(IReadOnlyList<AReadingOnTheRecord>))
							.And.Not.EqualTo(typeof(HowThisVehicleIsDriven)),
						older.Name + " gano el portador del ritmo o sus filas. Son tres claves "
						+ "distintas -- un modelo, un instante y un periodo -- y por eso son tres "
						+ "portadores.");

					foreach (string forbidden in new[] { "Rate", "Driven", "PerDay", "Series" })
						Assert.That(each.Name, Does.Not.Contain(forbidden),
							older.Name + " gano '" + each.Name + "'. El ritmo es de un PERIODO, y esa "
							+ "hoja no tiene ninguno.");
				}

			// Y AL REVES: el portador del ritmo no absorbio ninguna de las dos. Fusionar en cualquiera
			// de las dos direcciones deja el mismo defecto.
			foreach (PropertyInfo each in typeof(HowThisVehicleIsDriven).GetProperties())
				Assert.That(
					each.PropertyType,
					Is.Not.EqualTo(typeof(WhatTheManufacturerPublished))
						.And.Not.EqualTo(typeof(WhereThisVehicleStands)),
					"El portador del ritmo gano una de las dos hojas que ya estaban.");
		}

		// ===== lo que hace falta para las de arriba =======================================

		private HowThisVehicleIsDriven WhatTheScreenIsHanded()
		{
			return HowThisVehicleIsDriven.ReadThrough(
				journal.Api, TheWindowFrom, TheWindowTo, TheEarlierWindowFrom, TheEarlierWindowTo);
		}

		// LAS CINCO, EN ORDEN DE OBSERVACION Y CRECIENDO: el guardia de insercion mira TODA lectura
		// anterior y posterior de la misma unidad, no solo las vecinas.
		private void TheFiveReadings()
		{
			journal.Api.RecordReading(10000, TheUnitOfTheRecord, TheFirstOfTheEarlierWindow, "Telemetry");
			journal.Api.RecordReading(13000, TheUnitOfTheRecord, TheLastOfTheEarlierWindow, "Telemetry");
			journal.Api.RecordReading(20000, TheUnitOfTheRecord, TheFirstOfTheWindow, "Telemetry");
			journal.Api.RecordReading(22000, TheUnitOfTheRecord, TheMiddleOfTheWindow, "HumanEntry");
			journal.Api.RecordReading(26000, TheUnitOfTheRecord, TheLastOfTheWindow, "CustomerDeclared");
		}

		// LAS CUATRO SUSPENSIONES, CADA UNA CON LA CLAUSULA QUE DICE SU PROPIA RAZON. Dos salen de este
		// mismo registro y las otras dos necesitan otro: una mezcla de unidades y dos lecturas en el
		// mismo segundo cambiarian los periodos de todas las demas pruebas.
		private List<ASuspensionAsItWasRead> TheFourSuspensionsAndTheClauseEachOneSays()
		{
			var said = new List<ASuspensionAsItWasRead>
			{
				Read(journal, TheLoneReadingMonthFrom, TheLoneReadingMonthTo,
					"SuspendedByASingleReading", "una sola lectura"),
				Read(journal, TheEmptyYearFrom, TheEmptyYearTo,
					"SuspendedByAnAbsentReading", "no hay ninguna lectura del odómetro"),
			};

			using (var mixed = new AScheduleOnARealJournal(
				"WAUZZZ8V7JA000001", AppState.TheProfileAtDelivery))
			{
				mixed.Api.RecordReading(10000, TheUnitOfTheRecord, TheFirstOfTheWindow, "Telemetry");
				mixed.Api.RecordReading(7000, TheForeignUnit, TheMiddleOfTheWindow, "Telemetry");
				mixed.Api.RecordReading(12000, TheUnitOfTheRecord, TheLastOfTheWindow, "Telemetry");

				said.Add(Read(mixed, TheWindowFrom, TheWindowTo,
					"SuspendedByAUnitMismatch", "las unidades nunca se convierten"));
			}

			using (var same = new AScheduleOnARealJournal(
				"WAUZZZ8V7JA000002", AppState.TheProfileAtDelivery))
			{
				same.Api.RecordReading(10000, TheUnitOfTheRecord, TheFirstOfTheWindow, "Telemetry");
				same.Api.RecordReading(10000, TheUnitOfTheRecord, TheFirstOfTheWindow, "Correction");

				said.Add(Read(same, TheWindowFrom, TheWindowTo,
					"SuspendedByAZeroLengthSpan", "en el mismo segundo"));
			}

			return said;
		}

		private static ASuspensionAsItWasRead Read(
			AScheduleOnARealJournal from, DateTime start, DateTime end, string variant, string clause)
		{
			return new ASuspensionAsItWasRead
			{
				CanBeSaid = from.Api.CanARateOfUseBeSaid(start, end),
				Says = from.Api.DescribeRateOfUse(start, end),
				Variant = variant,
				Clause = clause,
			};
		}

		// UNA de las cuatro suspensiones tal como se leyo, con la clausula que su propia frase tiene
		// que decir. Es una clase y no una tupla: el barrido de claves de tupla del cierre del modulo
		// cuenta CERO tuplas en las siete carpetas, y una tupla aca lo movia sin que nadie lo hubiera
		// decidido.
		private sealed class ASuspensionAsItWasRead
		{
			internal bool CanBeSaid { get; init; }

			internal string Says { get; init; } = "";

			internal string Variant { get; init; } = "";

			internal string Clause { get; init; } = "";
		}

		private static DateTime TheWindowRule(string member, DateTime instant)
		{
			MethodInfo rule = typeof(AppState).GetMethod(
				member, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

			Assert.That(rule, Is.Not.Null,
				"La caratula no tiene '" + member + "'. La regla de la ventana dejo de estar en un "
				+ "miembro con nombre, asi que no hay nada que medir por valor.");

			return (DateTime)rule.Invoke(null, new object[] { instant });
		}

		private static string TheHitsInWords(List<ArithmeticHit> found)
		{
			var said = new System.Text.StringBuilder("EL BLOQUE LLEVA ARITMETICA:\n");

			foreach (ArithmeticHit hit in found) said.Append("  ").Append(hit).Append('\n');

			return said.ToString();
		}

		private static int TimesItSays(string markup, string what)
		{
			int found = 0;
			int at = markup.IndexOf(what, StringComparison.Ordinal);

			while (at >= 0)
			{
				found++;
				at = markup.IndexOf(what, at + what.Length, StringComparison.Ordinal);
			}

			return found;
		}

		private static string Squeezed(string text)
		{
			return System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");
		}

		private static string TheSectionAsItIsWritten()
		{
			return TheFileAtOutput(Path.Combine("SeccionesComoDato", TheSection));
		}

		private static string TheShellAsItIsWritten()
		{
			return TheFileAtOutput(Path.Combine("CodigoComoDato", "Wasm", "AppState.cs"));
		}

		private static string TheCarrierAsItIsWritten()
		{
			return TheFileAtOutput(
				Path.Combine("CodigoComoDato", "Wasm", "HowThisVehicleIsDriven.cs"));
		}

		private static string TheFileAtOutput(string relative)
		{
			string path = Path.Combine(AppContext.BaseDirectory, relative);

			Assert.That(File.Exists(path), Is.True,
				"'" + relative + "' no llego al output. Se copia por glob para que un archivo nuevo "
				+ "entre solo; si este no llego, lo que fallo es la copia y no el codigo -- y un "
				+ "barrido que leyo cero esta verde por no haber mirado.");

			return File.ReadAllText(path);
		}
	}
}
