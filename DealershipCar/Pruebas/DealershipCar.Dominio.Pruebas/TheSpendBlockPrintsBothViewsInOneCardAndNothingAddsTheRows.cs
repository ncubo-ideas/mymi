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
	// EL BLOQUE "EN QUE SE FUE LA PLATA", QUE ES LA PRIMERA SUPERFICIE QUE UNA PERSONA LEE DE TODA
	// ESTA REBANADA. Seis tareas de dominio, dos de puerta y una de feed no pusieron ni una frase en
	// ninguna pantalla: aca L6 -- las dos vistas siempre juntas --, L3 -- ningun total entre monedas
	// --, L11 -- los dos periodos nombrados --, L12 -- ningun veredicto -- y el ancla de L13 o
	// sobreviven al contacto con una maqueta o no.
	//
	// DOS INSTRUMENTOS, Y CADA UNO ATRAPA LO QUE EL OTRO NO VE, igual que en el bloque del ritmo:
	//
	//   el BARRIDO defiende los NUMEROS -- una suma de dos montos de dos filas, que es exactamente lo
	//   que una tabla de dinero al lado de un total invita a escribir;
	//   la afirmacion LETRA POR LETRA defiende la FRASE -- una rival armada por CONCATENACION, que el
	//   barrido no ve nunca porque un '+' con un operando de texto es concatenacion y no una cuenta.
	//
	// EL CONTROL NEGATIVO VA CON ESPACIOS ALREDEDOR DEL OPERADOR. En un .razor la mitad del archivo es
	// markup y ahi un guion es parte de un nombre, asi que una resta PEGADA pasa por la razon
	// equivocada. OD-5 lo midio sobre un archivo enviado y este bloque va en el mismo archivo.
	//
	// Y LA CARA DE E48 QUE DECIDE LA MAQUETA ES LA SEGUNDA. Las dos lecturas del gasto dicen lo mismo
	// mas una clausula: en un periodo de DOS monedas contestan letra por letra igual, y en uno de UNA
	// la de por moneda agrega su propia clausula y nada mas. Pintar las dos imprime las dos vistas dos
	// veces en una columna que la persona lee de un vistazo, asi que la pantalla pinta UNA -- la que
	// dice mas en los tres estados -- y la otra se queda en la superficie. Medido aca abajo y no
	// supuesto.
	[TestFixture]
	public sealed class TheSpendBlockPrintsBothViewsInOneCardAndNothingAddsTheRows
	{
		private const string TheVin = "3VWFE21C04M000001";

		private const string TheSection = "Historial.razor";

		private const string TheColon = "CRC";
		private const string TheDollar = "USD";

		// LA VENTANA DE ARRIBA: los doce meses que terminan en el instante en que la pantalla pregunta.
		private static readonly DateTime TheWindowFrom =
			new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheWindowTo =
			new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc);

		// LA DE ABAJO: los doce anteriores. Su limite de arriba ES el de abajo de la otra: las dos
		// ventanas comparten la bisagra, igual que las del ritmo.
		private static readonly DateTime TheEarlierWindowFrom =
			new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheEarlierWindowTo = TheWindowFrom;

		// EL SEMESTRE DE UNA SOLA MONEDA. Es el UNICO estado en que las dos lecturas del gasto
		// DIFIEREN, asi que toda afirmacion que las quiera discriminar tiene que tomarse aca: en el
		// anio de dos monedas contestan letra por letra lo mismo, y ahi no hay nada que discriminar.
		private static readonly DateTime TheColonesHalfFrom = TheWindowFrom;

		private static readonly DateTime TheColonesHalfTo =
			new DateTime(2026, 6, 30, 23, 59, 59, DateTimeKind.Utc);

		// LOS TRES GASTOS, en orden del instante en que se incurrieron y en DOS monedas.
		private const string TheTyres = "Llantas nuevas";
		private const decimal TheTyresCost = 240000.00m;
		private const int FourYearsOfLife = 48;

		private static readonly DateTime TheTyresIncurred =
			new DateTime(2026, 3, 12, 9, 0, 0, DateTimeKind.Utc);

		private const string TheOilChange = "Cambio de aceite";
		private const decimal TheOilChangeCost = 45000.00m;

		private static readonly DateTime TheOilChangeIncurred =
			new DateTime(2026, 5, 20, 10, 30, 0, DateTimeKind.Utc);

		private const string TheWash = "Lavado premium";
		private const decimal TheWashCost = 120.50m;

		private static readonly DateTime TheWashIncurred =
			new DateTime(2026, 8, 5, 14, 15, 0, DateTimeKind.Utc);

		// LAS TRES LECTURAS DEL ODOMETRO que le dan al periodo un tramo de uso contra el que dividir.
		// La primera es el ANCLA de L13: un gasto incurrido en su mismo instante no cuenta.
		private const string TheUnit = "UnitA";

		private static readonly DateTime TheFirstReading =
			new DateTime(2026, 1, 10, 8, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheMiddleReading =
			new DateTime(2026, 3, 1, 8, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheLastReading =
			new DateTime(2026, 8, 20, 8, 0, 0, DateTimeKind.Utc);

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

		// LOS CUATRO LIMITES VUELVEN NOMBRADOS Y LAS NUEVE RESPUESTAS SON LAS DE LA PUERTA, POR VALOR.
		// Un limite que la respuesta no nombra es un periodo que quien lee no puede revisar, y una
		// respuesta que no es la de la puerta letra por letra es una que compuso la caratula.
		[Test]
		public void TheFourBoundsComeBackNamedAndTheNineAnswersAreTheDoorsOwn()
		{
			TheThreeCostsAndTheThreeReadings();

			WhatThisVehicleHasCost spend = WhatTheScreenIsHanded();

			Assert.That(spend.From, Is.EqualTo("2026-01-01 00:00:00Z"));
			Assert.That(spend.To, Is.EqualTo("2026-12-31 23:59:59Z"));
			Assert.That(spend.EarlierFrom, Is.EqualTo("2025-01-01 00:00:00Z"));
			Assert.That(spend.EarlierTo, Is.EqualTo("2026-01-01 00:00:00Z"),
				"El limite de arriba del periodo anterior ES el de abajo del de arriba: las dos "
				+ "ventanas son contiguas y comparten la bisagra.");

			Assert.That(spend.AnyCost, Is.True,
				"AnyCost: el anio tiene los tres gastos.");

			Assert.That(spend.SaidInOneCurrency, Is.False,
				"SaidInOneCurrency: el anio mezcla colones y dolares, asi que NO esta dicho en una "
				+ "sola. Eso es un estado con nombre y no un error.");

			Assert.That(
				spend.Says,
				Is.EqualTo(journal.Api.DescribeSpendBetween(TheWindowFrom, TheWindowTo)),
				"La frase del gasto es la de la puerta LETRA POR LETRA.");

			Assert.That(
				spend.PerCurrencySays,
				Is.EqualTo(
					journal.Api.DescribeSpendPerCurrencyBetween(TheWindowFrom, TheWindowTo)),
				"Y la de por moneda tambien.");

			Assert.That(
				spend.BothPeriodsSay,
				Is.EqualTo(journal.Api.DescribeTwoPeriodsOfSpendCompared(
					TheWindowFrom, TheWindowTo, TheEarlierWindowFrom, TheEarlierWindowTo)),
				"Y la de los dos periodos, con los cuatro limites en el orden en que se pasaron: "
				+ "cruzar los dos pares da otra frase.");

			Assert.That(
				spend.CostPerDistanceCanBeSaid,
				Is.EqualTo(journal.Api.CanCostPerDistanceBeSaid(TheWindowFrom, TheWindowTo)));

			Assert.That(
				spend.CostPerDistanceSays,
				Is.EqualTo(
					journal.Api.DescribeCostPerDistanceBetween(TheWindowFrom, TheWindowTo)));

			Assert.That(spend.EntryCount, Is.EqualTo(3),
				"EntryCount: los TRES del anio, contados por el dominio.");
		}

		// LAS DOS VISTAS ESTAN EN UNA SOLA TARJETA Y NINGUNA LECTURA CONTESTA UNA SOLA. Es §15 item 8
		// hecho prueba: la ley central de la rebanada o llega a la pantalla o falla en silencio.
		//
		// SE AFIRMA EN DOS MITADES QUE NO SE CUBREN. La primera es por VALOR sobre el semestre de una
		// sola moneda, donde las dos cifras existen y son DISTINTAS -- la caja son las dos entradas de
		// colones enteras y lo consumido es el aceite entero mas la parte que a ese semestre le toca de
		// las llantas. La segunda es por CENSO sobre la superficie del portador: ningun miembro suyo
		// contesta una vista sola, asi que ni la pantalla ni nadie puede pedirle la mitad.
		[Test]
		public void BothViewsReachTheScreenInOneSentenceAndNoMemberAnswersOneAlone()
		{
			TheThreeCostsAndTheThreeReadings();

			string half = journal.Api.DescribeSpendPerCurrencyBetween(
				TheColonesHalfFrom, TheColonesHalfTo);

			Assert.That(half, Does.Contain("Salieron ₡285000.00 del banco"),
				"La caja del semestre, sumada por el DOMINIO -- 240000 mas 45000.");
			Assert.That(half, Does.Contain("el período consumió ₡"),
				"Y lo consumido en la MISMA frase: ningun miembro contesta una vista sola.");
			Assert.That(half, Does.Not.Contain("consumió ₡285000.00"),
				"Y las dos cifras DIFIEREN, que es el caso que hace que la distincion sirva de algo: "
				+ "la duradera se reparte a lo largo de 48 meses, asi que 'compró llantas' no es 'el "
				+ "carro se puso caro'.");

			// Y NINGUN MIEMBRO DEL PORTADOR ENTREGA UNA VISTA SOLA. Se enumeran los miembros y no los
			// sustantivos: quien agregue el atajo elige el nombre, asi que reconocer nombres es un
			// proxy que se rompe justo cuando importa. Lo que se afirma es que ninguno devuelve un
			// numero ni una cifra suelta -- las nueve respuestas son textos, dos bool y una cuenta.
			var handingOutSomethingElse = new List<string>();

			foreach (PropertyInfo each in typeof(WhatThisVehicleHasCost).GetProperties())
				if (each.PropertyType != typeof(string)
					&& each.PropertyType != typeof(bool)
					&& each.PropertyType != typeof(int)
					&& each.PropertyType != typeof(IReadOnlyList<ACostOnTheRecord>))
					handingOutSomethingElse.Add(each.Name + ":" + each.PropertyType.Name);

			Assert.That(handingOutSomethingElse, Is.Empty,
				"El portador gano un miembro de otra forma. Las dos vistas viajan ADENTRO de una "
				+ "frase, a proposito: un decimal, un Money o un par de cifras serian la mitad de L6 "
				+ "con un nombre encima.");

			foreach (string forbidden in new[]
			{
				"CashOut", "Consumed", "Amortized", "Share", "Total", "Amount",
			})
				foreach (PropertyInfo each in typeof(WhatThisVehicleHasCost).GetProperties())
					Assert.That(each.Name, Does.Not.Contain(forbidden),
						"El portador gano '" + each.Name + "'. Una vista sola es el numero que "
						+ "engana, y por eso el dominio no la contesta: pedirsela a la caratula seria "
						+ "componerla aca.");

			// Y LA UNICA CUENTA QUE ENTREGA ES CUANTAS FILAS HAY, que no es una cifra de dinero.
			Assert.That(
				typeof(WhatThisVehicleHasCost).GetProperty("EntryCount").PropertyType,
				Is.EqualTo(typeof(int)));
		}

		// LA TARJETA PINTA UNA SOLA DE LAS DOS FRASES DEL GASTO, Y CUAL SE MIDE EN LOS TRES ESTADOS.
		// Es la segunda mitad de E48: el hermano esta en la SUPERFICIE siempre, y si la frase compuesta
		// ya contiene la general, pintar las dos imprime una clausula dos veces.
		[Test]
		public void ThePerCurrencySentenceSaysEverythingTheOtherOneDoesAndTheCardPaintsOnlyIt()
		{
			TheThreeCostsAndTheThreeReadings();

			// (a) EL ESTADO DE DOS MONEDAS: las dos contestan LETRA POR LETRA lo mismo. Con mas de una
			// moneda no existe una respuesta "entera" distinta de la lista por moneda, porque el total
			// que las separaria es justo el que no hay.
			Assert.That(
				journal.Api.DescribeSpendPerCurrencyBetween(TheWindowFrom, TheWindowTo),
				Is.EqualTo(journal.Api.DescribeSpendBetween(TheWindowFrom, TheWindowTo)),
				"En un periodo de dos monedas las dos lecturas dejaron de decir lo mismo. Si eso "
				+ "cambio, la decision de pintar una sola se vuelve a mirar.");

			// (b) EL ESTADO DE UNA MONEDA: la de por moneda CONTIENE lo que dice la otra menos su
			// primera clausula, y le agrega la suya. Se afirma por CONTENCION del resumen y no por
			// igualdad, porque lo que cambia es justo la clausula que las une.
			string whole = journal.Api.DescribeSpendBetween(TheColonesHalfFrom, TheColonesHalfTo);
			string perCurrency =
				journal.Api.DescribeSpendPerCurrencyBetween(TheColonesHalfFrom, TheColonesHalfTo);

			Assert.That(perCurrency, Does.Contain("todo el gasto está dicho en una sola moneda"),
				"La de por moneda dejo de decir su propia clausula, que es lo unico que agrega en "
				+ "este estado.");
			Assert.That(whole, Does.Not.Contain("todo el gasto está dicho en una sola moneda"),
				"Y la otra NO la dice: sin este renglon, la de arriba pasaria tambien si las dos "
				+ "lecturas llamaran al mismo miembro del dominio.");
			Assert.That(perCurrency, Does.Contain(TheSummaryInside(whole)),
				"La de por moneda dejo de contener el resumen entero de la otra. Mientras lo "
				+ "contenga, pintar las dos imprime las dos vistas dos veces.");

			// (c) EL ESTADO SIN NINGUN GASTO: lo mismo, contra un anio que no tiene ninguno.
			string emptyWhole =
				journal.Api.DescribeSpendBetween(TheEarlierWindowFrom, TheEarlierWindowTo);
			string emptyPerCurrency = journal.Api.DescribeSpendPerCurrencyBetween(
				TheEarlierWindowFrom, TheEarlierWindowTo);

			Assert.That(
				emptyPerCurrency,
				Does.Contain("no hay ningún gasto registrado para este vehículo"),
				"La ausencia se dice con palabras y nunca con un cero.");
			Assert.That(
				emptyPerCurrency, Does.Contain("no hay ninguna moneda en la que decirlo"),
				"Y la de por moneda agrega su clausula tambien aca.");
			Assert.That(emptyWhole, Does.Not.Contain("no hay ninguna moneda en la que decirlo"));

			// (d) Y LA MAQUETA PINTA LA DE POR MONEDA Y NO LA OTRA. La superficie lleva las dos --
			// afirmado arriba, en el portador -- y la pagina dibuja la que dice mas en los tres
			// estados. Lo que hace para una persona lo que un nombre de campo hace para un modelo es
			// el TITULO del bloque, y por eso la etiqueta de la linea nombra las dos vistas.
			string markup = TheSectionAsItIsWritten();

			Assert.That(markup, Does.Contain("@Spend.PerCurrencySays"),
				"El bloque dejo de pintar la frase del gasto.");
			Assert.That(markup, Does.Not.Contain("@Spend.Says"),
				"El bloque pinta LAS DOS frases del gasto, y en una columna que la persona lee de un "
				+ "vistazo eso imprime las dos vistas dos veces. Las dos siguen en la superficie: la "
				+ "que no se dibuja es la que dice menos.");
			Assert.That(
				markup,
				Does.Contain("Lo que salió del banco y lo que el período consumió"),
				"La etiqueta de la linea dejo de nombrar LAS DOS vistas. Es lo que hace para una "
				+ "persona lo que un nombre de campo hace para un modelo.");
		}

		// NINGUNA LINEA DEL BLOQUE LLEVA UN OPERADOR ARITMETICO. Es el control negativo de §2.2 hecho
		// prueba, y el caso mas filoso de esta rebanada: una tabla de montos al lado de un total es
		// exactamente donde alguien suma dos filas. Se escribe CON ESPACIOS alrededor del operador,
		// porque en un .razor el guion pide espacio de un lado.
		[Test]
		public void NoLineOfTheSpendBlockHoldsAnArithmeticOperator()
		{
			List<ArithmeticHit> found = TheCodeOfTheShellsAsData.TheArithmeticInSource(
				"Wasm", "Sections/" + TheSection, TheSectionAsItIsWritten());

			Assert.That(found, Is.Empty, TheHitsInWords(found));

			// EL CONTROL DEL DETECTOR SOBRE ESTE ARCHIVO, en la forma que esta rebanada invita: SUMAR
			// dos montos de dos filas. Sin el, lo de arriba estaria vacio por no detectar.
			foreach (string shape in new[]
			{
				"<div>@(Spend.Rows[0].Money + Spend.Rows[1].Money)</div>",
				"<div>@(Spend.EntryCount / Spend.Rows.Count)</div>",
				"<div>@(Spend.EntryCount - Spend.Rows.Count)</div>",
			})
				Assert.That(
					TheCodeOfTheShellsAsData.TheArithmeticInSource(
						"Wasm", "Sections/" + TheSection, shape),
					Is.Not.Empty,
					"El detector no ve '" + shape + "' en un .razor, asi que la afirmacion de arriba "
					+ "esta verde por no detectar.");

			// Y LA FORMA QUE NO VE, DICHA AQUI TAMBIEN: la resta PEGADA. Quien escriba el proximo
			// control tiene que espaciarlo o pasa por la razon equivocada.
			Assert.That(
				TheCodeOfTheShellsAsData.TheArithmeticInSource(
					"Wasm", "Sections/" + TheSection,
					"<div>@(Spend.EntryCount-Spend.Rows.Count)</div>"),
				Is.Empty,
				"El detector empezo a ver una resta pegada en un .razor. Es una buena noticia y esta "
				+ "prueba es la que hay que borrar, no la que hay que arreglar.");
		}

		// EL BLOQUE PINTA LAS FRASES DEL DOMINIO Y NINGUNA PROPIA. Es el segundo instrumento, y es el
		// unico que atrapa una frase rival armada por CONCATENACION: el barrido no la ve nunca, porque
		// un '+' con un operando de texto es concatenacion y no una cuenta.
		[Test]
		public void TheSpendBlockPaintsTheDomainsSentencesAndComposesNoRivalOfItsOwn()
		{
			string markup = TheSectionAsItIsWritten();

			foreach (string sentence in new[]
			{
				"@Spend.PerCurrencySays", "@Spend.BothPeriodsSay", "@Spend.CostPerDistanceSays",
				"@gasto.Says",
			})
				Assert.That(markup, Does.Contain(sentence),
					"El bloque dejo de pintar '" + sentence + "'. La frase la compone el dominio: una "
					+ "que la caratula arme con pedazos de las otras sale con la procedencia del "
					+ "registro encima de algo que el registro no dijo.");

			foreach (string forbidden in new[]
			{
				"PerCurrencySays +", "+ PerCurrencySays", "BothPeriodsSay +", "+ BothPeriodsSay",
				"CostPerDistanceSays +", "+ CostPerDistanceSays",
				"gasto.Says +", "+ gasto.Says", "gasto.Money +", "+ gasto.Money",
			})
				Assert.That(markup, Does.Not.Contain(forbidden),
					"El bloque concatena algo con una frase del dominio ('" + forbidden + "'). Lo que "
					+ "sale de la puerta se cita letra por letra o no se cita.");
		}

		// EL DINERO SE PINTA COMO LO DIJO EL DOMINIO Y NO HAY UN SEGUNDO FORMATEADOR. Money.Expressed()
		// ya trae el signo de la moneda y sus decimales, asi que un formateador consciente de la moneda
		// en la caratula seria un SEGUNDO AUTOR de como se dice una cifra -- y los dos podrian
		// separarse, que es lo que ya paso con la palabra de una clase de operacion.
		[Test]
		public void TheMoneyIsPrintedAsTheDomainSaidItAndNoSecondFormatterWasWritten()
		{
			TheThreeCostsAndTheThreeReadings();

			WhatThisVehicleHasCost spend = WhatTheScreenIsHanded();

			Assert.That(
				spend.Rows.Select(row => row.Money),
				Is.EqualTo(new[] { "₡240000.00", "₡45000.00", "$120.50" }),
				"El dinero llega DICHO por el dominio, con el signo y los decimales de cada moneda, y "
				+ "la tercera fila en OTRA moneda: un cruce entre columnas cambia esta afirmacion.");

			string markup = TheSectionAsItIsWritten();

			Assert.That(markup, Does.Contain("@gasto.Money"),
				"El bloque dejo de pintar el dinero de la fila.");

			// Y NI EL BLOQUE NI EL PORTADOR PASAN EL DINERO POR NINGUN FORMATEADOR. HowANumberReads
			// agrupa miles y su propio /// dice que es formato y no una cuenta; aplicarselo a una cifra
			// que ya viene con su signo la escribiria por segunda vez, con otras reglas.
			foreach (string formatter in new[]
			{
				"ComoSeLee.Miles(gasto.Money)", "HowANumberReads", "ToString(\"C",
				"NumberFormatInfo", "Currencysymbol", "CurrencySymbol",
			})
			{
				Assert.That(markup, Does.Not.Contain(formatter),
					"El bloque pasa el dinero por '" + formatter + "'. El dominio ya lo dijo.");
				Assert.That(TheCarrierAsItIsWritten(), Does.Not.Contain(formatter),
					"El portador pasa el dinero por '" + formatter + "'.");
			}

			// EL CONTROL: el mismo bloque SI formatea un numero que NO es dinero -- el odometro de una
			// fila del ritmo, en el mismo archivo -- asi que la ausencia de arriba no es que este
			// archivo no formatee nada.
			Assert.That(markup, Does.Contain("ComoSeLee.Miles(lectura.Value)"),
				"El archivo dejo de agrupar el odometro, asi que la ausencia de un formateador de "
				+ "dinero no dice nada: podria ser que aca no se formatee nunca.");
		}

		// LAS FILAS SON EL REGISTRO CRUDO Y CADA UNA SE DICE SOLA. Seis columnas, en orden del instante
		// en que se incurrieron, y la vida util solo en la duradera: una celda vacia dice que no tiene,
		// y un cero se leeria como una vida de cero meses.
		[Test]
		public void EveryRowKeepsItsOwnColumnsAndItsOwnSentenceNamesItsClassification()
		{
			TheThreeCostsAndTheThreeReadings();

			WhatThisVehicleHasCost spend = WhatTheScreenIsHanded();

			Assert.That(spend.Rows, Has.Count.EqualTo(3));

			Assert.That(
				spend.Rows.Select(row => row.IncurredAt),
				Is.EqualTo(new[]
				{
					"2026-03-12 09:00:00", "2026-05-20 10:30:00", "2026-08-05 14:15:00",
				}),
				"En orden del instante en que se INCURRIERON, con su hora: un instante nunca se dice "
				+ "como un dia de calendario a secas.");

			Assert.That(
				spend.Rows.Select(row => row.Subject),
				Is.EqualTo(new[] { TheTyres, TheOilChange, TheWash }),
				"Los asuntos con las palabras que llegaron, sin tocar.");

			Assert.That(
				spend.Rows.Select(row => row.Classification),
				Is.EqualTo(new[] { "Durable", "Recurring", "Recurring" }));

			Assert.That(
				spend.Rows.Select(row => row.ExpectedLife),
				Is.EqualTo(new[] { "48 meses", "", "" }),
				"La vida util solo la lleva la duradera.");

			// Y LA FRASE DE CADA FILA NOMBRA SU PROPIO ASUNTO ADENTRO DE SU PROPIA CLAUSULA (C60). Un
			// Does.Contain del asunto suelto pasaria con la columna cruzada.
			Assert.That(
				spend.Rows[0].Says,
				Does.Contain("₡240000.00 en " + TheTyres + " el 2026-03-12 09:00:00")
					.And.Contains("Duradero"));
			Assert.That(
				spend.Rows[2].Says,
				Does.Contain("$120.50 en " + TheWash + " el 2026-08-05 14:15:00")
					.And.Contains("Recurrente"));

			// Y LA MAQUETA DIBUJA LA VIDA UTIL SOLO DONDE LA HAY. Una pastilla vacia en las dos filas
			// recurrentes se leeria como una vida que no se sabe.
			Assert.That(
				TheSectionAsItIsWritten(),
				Does.Contain("@if (gasto.ExpectedLife.Length > 0)"),
				"El bloque dibuja la vida util sin preguntar si la hay, asi que las dos recurrentes "
				+ "salen con una pastilla vacia.");
		}

		// LA VENTANA DE ESTE BLOQUE ES LA MISMA QUE LA DEL RITMO, Y NO PUEDE NO SERLO. El costo por
		// distancia cruza los dos globales de raiz adentro del dominio: el gasto lo pone el libro y el
		// tramo la agenda. Dos ventanas distintas en la misma pantalla pondrian el gasto de un periodo
		// sobre la distancia de otro, y la frase no tendria como decirlo.
		[Test]
		public void TheSpendWindowIsTheSameOneTheRateBlockAsksForAndBothComeFromOneReadOfTheClock()
		{
			string shell = Squeezed(TheShellAsItIsWritten());

			Assert.That(
				shell,
				Does.Contain(
					"public WhatThisVehicleHasCost ReadWhatThisVehicleHasCost() { Require(); "
					+ "DateTime asOf = TheInstantTheScreenAsksAt(); "
					+ "return WhatThisVehicleHasCost.ReadThrough( api, TheTwelveMonthsBefore(asOf), "
					+ "asOf, TheTwentyFourMonthsBefore(asOf), TheTwelveMonthsBefore(asOf));"),
				"La lectura del libro dejo de derivar sus cuatro limites de UN solo asOf, o dejo de "
				+ "usar las mismas dos reglas de ventana que la del ritmo. Dos reglas distintas "
				+ "pondrian el gasto de un periodo sobre la distancia de otro.");

			// Y LAS DOS LECTURAS USAN LAS MISMAS DOS REGLAS, afirmado por nombre: si alguien escribe
			// una tercera regla de ventana para este bloque, esto se pone rojo.
			Assert.That(
				TimesItSays(shell, "TheTwelveMonthsBefore(asOf)"), Is.EqualTo(4),
				"Las dos lecturas usan la regla de los doce meses dos veces cada una -- el limite de "
				+ "abajo de la ventana de arriba y el de arriba de la anterior. Si no son cuatro, una "
				+ "de las dos dejo de compartir la bisagra.");
			Assert.That(
				TimesItSays(shell, "TheTwentyFourMonthsBefore(asOf)"), Is.EqualTo(2));
		}

		// EL PORTADOR NO LEE NINGUN RELOJ Y NO PIDE NINGUNA MEDICION. Los cuatro limites llegan
		// calculados, porque el instante del que salen lo lee la caratula, que es la unica que puede.
		[Test]
		public void TheCarrierTakesTheFourBoundsAlreadyComputedAndReadsNoClock()
		{
			MethodInfo read = typeof(WhatThisVehicleHasCost).GetMethod("ReadThrough");

			Assert.That(
				read.GetParameters().Select(each => each.Name + ":" + each.ParameterType.Name),
				Is.EqualTo(new[]
				{
					"api:DealershipCarAPI", "from:DateTime", "to:DateTime",
					"from2:DateTime", "to2:DateTime",
				}),
				"La firma del portador cambio. El ORDEN es contrato: el dominio dice el primer "
				+ "periodo respecto al segundo, asi que cruzar los dos pares da la frase al reves, y "
				+ "los nombres son los que la puerta le da a los cuatro limites de su propia lectura "
				+ "de comparacion.");

			Assert.That(TheCarrierAsItIsWritten(), Does.Not.Contain("DateTime.UtcNow"),
				"El portador lee un reloj. Los cuatro limites llegan calculados a proposito.");
			Assert.That(TheCarrierAsItIsWritten(), Does.Not.Contain("DateTime.Now"));
			Assert.That(TheCarrierAsItIsWritten(), Does.Not.Contain("DateTime.Today"));
		}

		// LA LECTURA DE LA CARATULA NO TOMA NINGUN ARGUMENTO Y NO AGREGA UN CUARTO RELOJ. Un parametro
		// DateTime en un miembro publico de AppState seria una MEDICION, y el instante de la pantalla
		// ya existe: TheShellHasExactlyTheseThreeMembersThatReadAClock tiene que quedarse en TRES.
		[Test]
		public void TheShellsReadOfTheSpendTakesNoArgumentAndAddsNoFourthClock()
		{
			MethodInfo read = typeof(AppState).GetMethod("ReadWhatThisVehicleHasCost");

			Assert.That(read, Is.Not.Null);
			Assert.That(read.GetParameters(), Is.Empty,
				"La lectura de la caratula gano un parametro. Un instante o un numero ahi es una "
				+ "medicion: el instante lo lee ella del reloj de la maquina y no se lo pide a nadie.");

			var handingOutAnInstant = new List<string>();

			foreach (MethodInfo each in typeof(AppState).GetMethods(
				BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public
				| BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
				if (each.ReturnType == typeof(DateTime) && each.GetParameters().Length == 0)
					handingOutAnInstant.Add(each.Name);

			Assert.That(handingOutAnInstant, Has.Count.EqualTo(3),
				"La caratula gano un miembro que entrega un instante. El bloque del libro no hacia "
				+ "falta uno: la lectura llama al que ya existe.");
		}

		// EL BLOQUE NO RECOGE NINGUN VALOR DE UNA PERSONA Y NO HAY COMO MOVER LA VENTANA. Es la tercera
		// invencion prohibida, y un selector de periodo no tiene por que ser un <input>: un boton que
		// dijera "el ano pasado" pondria a la persona a elegir por que periodo se pregunta.
		[Test]
		public void TheSpendBlockCollectsNoValueFromAPersonAndOffersNoWayToMoveItsWindow()
		{
			string markup = TheSectionAsItIsWritten();

			foreach (string shape in new[] { "<input", "<select", "<textarea", "<form", "@bind-Value" })
				Assert.That(markup, Does.Not.Contain(shape),
					"La seccion Historial lleva un control que recoge un valor ('" + shape + "').");

			foreach (string shape in new[] { "<button", "@onclick" })
				Assert.That(markup, Does.Not.Contain(shape),
					"La seccion Historial gano un control que se puede apretar ('" + shape + "'). La "
					+ "ventana es fija a proposito: elegir el periodo es elegir la pregunta.");
		}

		// Y LA RAMA VACIA DICE QUIEN REGISTRA UN GASTO. Ninguna frase del dominio la dice: la del gasto
		// dice que no hay ninguno REGISTRADO, y "no hay ninguno registrado" no es "no se gasto nada" --
		// confundirlas es el cero que L8 prohibe, dicho por la pantalla.
		//
		// SE AFIRMA QUE ESTA PINTADA Y NO SOLO DECLARADA, que es la leccion de OD-5 §14 item 2: el
		// censo de TheOwnerIsTold cuenta las frases ESCRITAS y una declarada que nadie dibuja pasa.
		[Test]
		public void TheEmptyBranchSaysWhoRecordsACostAndNeverThatTheCarCostNothing()
		{
			string markup = TheSectionAsItIsWritten();

			const string line = "TheOwnerIsTold.TheCostsAreRecordedByTheBranchThatDoesTheWork";

			Assert.That(TimesItSays(markup, line), Is.EqualTo(1),
				"El bloque no pinta quien registra un gasto, o lo pinta en mas de un sitio.");

			Assert.That(
				markup.IndexOf(line, StringComparison.Ordinal),
				Is.GreaterThan(markup.IndexOf("id=\"sin-gastos\"", StringComparison.Ordinal)),
				"La frase quedo fuera de la rama vacia, que es la unica en que hace falta: con filas "
				+ "en pantalla, quien registra los gastos se ve en cada una.");

			Assert.That(
				TheOwnerIsTold.TheCostsAreRecordedByTheBranchThatDoesTheWork,
				Does.Contain("no quiere decir que este carro no te haya costado nada"),
				"La frase dejo de decir la lectura equivocada que existe para impedir.");

			Assert.That(
				TheOwnerIsTold.TheCostsAreRecordedByTheBranchThatDoesTheWork,
				Does.Contain("no hay nada que tengas que llenar vos"),
				"Y dejo de decir que no hay nada que la persona ponga, que es la mitad que un "
				+ "formulario contradice.");
		}

		// NINGUNA PALABRA DE VEREDICTO LLEGA A LA PANTALLA, NI DEL DOMINIO NI DE LA CARATULA. Es L12, y
		// se mira en los dos sitios porque son dos autores: las frases que el dominio compuso para este
		// periodo, y el markup que la caratula escribio alrededor de ellas.
		[Test]
		public void NeitherTheSentencesNorTheBlockSaysWhetherItIsWorthIt()
		{
			TheThreeCostsAndTheThreeReadings();

			WhatThisVehicleHasCost spend = WhatTheScreenIsHanded();

			var everythingOnTheScreen = new List<string>
			{
				spend.PerCurrencySays, spend.Says, spend.BothPeriodsSay, spend.CostPerDistanceSays,
			};

			foreach (ACostOnTheRecord row in spend.Rows) everythingOnTheScreen.Add(row.Says);

			everythingOnTheScreen.Add(TheSectionAsItIsWritten());

			foreach (string verdict in new[]
			{
				"vale la pena", "caro", "barato", "conviene", "excesivo", "razonable",
			})
				foreach (string said in everythingOnTheScreen)
					Assert.That(said, Does.Not.Contain(verdict).IgnoreCase,
						"Alguien dijo '" + verdict + "'. Las cifras con la distincion que las hace "
						+ "legibles son toda la respuesta, y el juicio es de la persona: no hay "
						+ "ningun umbral en el que se vuelva del registro.");
		}

		// EL PORTADOR NO SE FUSIONO CON NINGUNA DE LAS HOJAS QUE YA ESTABAN, y es la sexta vez que esa
		// prohibicion se aplica. La razon es la CLAVE y no el tamanio: la ficha lee un catalogo con
		// clave de MODELO, la mitad del vehiculo se mide contra el SOBRE al instante preguntado, el
		// ritmo es de un PERIODO de lecturas y esto es de un PERIODO de gastos.
		[Test]
		public void TheSpendCarrierWasMergedIntoNeitherTheSheetNorTheVehicleHalfNorTheRate()
		{
			foreach (Type older in new[]
			{
				typeof(WhatTheManufacturerPublished), typeof(WhereThisVehicleStands),
				typeof(HowThisVehicleIsDriven),
			})
				foreach (PropertyInfo each in older.GetProperties())
				{
					Assert.That(
						each.PropertyType,
						Is.Not.EqualTo(typeof(IReadOnlyList<ACostOnTheRecord>))
							.And.Not.EqualTo(typeof(WhatThisVehicleHasCost)),
						older.Name + " gano el portador del libro o sus filas.");

					foreach (string forbidden in new[] { "Spend", "Cost", "Money", "Currency" })
						Assert.That(each.Name, Does.Not.Contain(forbidden),
							older.Name + " gano '" + each.Name + "'. El gasto es de un PERIODO del "
							+ "libro, y ninguna de esas tres hojas lo tiene.");
				}

			// Y AL REVES: el portador del libro no absorbio ninguna de las tres.
			foreach (PropertyInfo each in typeof(WhatThisVehicleHasCost).GetProperties())
				Assert.That(
					each.PropertyType,
					Is.Not.EqualTo(typeof(WhatTheManufacturerPublished))
						.And.Not.EqualTo(typeof(WhereThisVehicleStands))
						.And.Not.EqualTo(typeof(HowThisVehicleIsDriven)),
					"El portador del libro gano una de las hojas que ya estaban.");
		}

		// ===== lo que hace falta para las de arriba =======================================

		private WhatThisVehicleHasCost WhatTheScreenIsHanded()
		{
			return WhatThisVehicleHasCost.ReadThrough(
				journal.Api, TheWindowFrom, TheWindowTo, TheEarlierWindowFrom, TheEarlierWindowTo);
		}

		// LOS TRES GASTOS Y LAS TRES LECTURAS. Los gastos llegan DESORDENADOS -- el de agosto antes que
		// el de mayo -- para que las filas prueben que las ordena el dominio; las lecturas llegan en
		// orden y creciendo, porque el guardia de insercion mira toda lectura anterior y posterior.
		private void TheThreeCostsAndTheThreeReadings()
		{
			journal.Api.RecordReading(20000, TheUnit, TheFirstReading, "Telemetry");
			journal.Api.RecordReading(22000, TheUnit, TheMiddleReading, "Telemetry");
			journal.Api.RecordReading(29000, TheUnit, TheLastReading, "Telemetry");

			journal.Api.RecordDurableCost(
				TheTyresIncurred, TheTyresCost, TheColon, TheTyres, FourYearsOfLife);
			journal.Api.RecordRecurringCost(TheWashIncurred, TheWashCost, TheDollar, TheWash);
			journal.Api.RecordRecurringCost(
				TheOilChangeIncurred, TheOilChangeCost, TheColon, TheOilChange);
		}

		// El resumen que las dos frases comparten: lo que va despues de la clausula del periodo, que es
		// la unica parte en que difieren.
		private static string TheSummaryInside(string sentence)
		{
			int at = sentence.IndexOf("Salieron", StringComparison.Ordinal);

			Assert.That(at, Is.GreaterThan(0),
				"La frase del gasto dejo de empezar su resumen con 'Salieron', asi que esta prueba no "
				+ "sabe que pedazo comparar.");

			return sentence.Substring(at);
		}

		private static string TheHitsInWords(List<ArithmeticHit> found)
		{
			var said = new System.Text.StringBuilder("EL BLOQUE LLEVA ARITMETICA:\n");

			foreach (ArithmeticHit hit in found) said.Append("  ").Append(hit).Append('\n');

			return said.ToString();
		}

		private static int TimesItSays(string text, string what)
		{
			int found = 0;
			int at = text.IndexOf(what, StringComparison.Ordinal);

			while (at >= 0)
			{
				found++;
				at = text.IndexOf(what, at + what.Length, StringComparison.Ordinal);
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
				Path.Combine("CodigoComoDato", "Wasm", "WhatThisVehicleHasCost.cs"));
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
