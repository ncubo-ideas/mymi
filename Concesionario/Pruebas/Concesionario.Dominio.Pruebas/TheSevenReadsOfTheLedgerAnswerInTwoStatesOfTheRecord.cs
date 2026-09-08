using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Concesionario.App;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LAS SIETE LECTURAS DE CL-5, CADA UNA EN LOS DOS ESTADOS DEL REGISTRO. Es la obligacion S5 -- un
	// error de tipeo en un script del DSL no se descubre al compilar, se descubre al ejecutarlo, y
	// deja una entrada permanente que hace loguear un fallo de rehidratacion en cada arranque futuro
	// -- aplicada a los siete scripts que esta tarea le agrega a la puerta.
	//
	// POR QUE DOS ESTADOS Y NO UNO (C61). Un libro sin ninguna entrada contesta la ausencia a las
	// siete, asi que un barrido tomado ahi compara siete ausencias que se ponen de acuerdo
	// correctamente sobre nada. El estado que hace que el barrido mida algo es el SEGUNDO: UNA
	// duradera y DOS recurrentes, en DOS monedas.
	//
	// LAS TRES ENTRADAS Y SUS PERIODOS ESTAN ELEGIDOS PARA QUE UN SOLO ESTADO DEL LIBRO DE LAS TRES
	// SITUACIONES QUE DEC-95 nombra. El anio entero tiene las tres y esta en dos monedas, asi que es
	// ASpendNotSaidAcrossCurrencies; el primer semestre tiene las dos de colones, asi que es
	// ASpendSaidInOneCurrency; 2025 no tiene ninguna y la ventana de la duradera todavia no habia
	// empezado, asi que es NoCostOnRecord.
	//
	// Y CADA RESPUESTA VA CON EL NOMBRE DE SU MIEMBRO AL LADO (C60). Is.Not.Empty prueba que llego
	// una respuesta, nunca que es la de ESE miembro. El discriminador mas barato es el ASUNTO que el
	// dominio ya escribe adentro de su propia frase, y por eso las afirmaciones de abajo lo buscan
	// adentro de la clausula que lo nombra -- "en <asunto> el <instante>" -- y no suelto.
	//
	// LOS VALORES ESTAN ELEGIDOS PARA QUE UNA RANURA SIN LIGAR NO PASE. Una cuenta de cero es lo que
	// contesta un int que el motor dejo por defecto, asi que en el segundo estado las cuentas de los
	// tres periodos valen numeros DISTINTOS entre si -- 3, 2, 0 -- y cruzar dos periodos cambia la
	// afirmacion.
	[TestFixture]
	public sealed class TheSevenReadsOfTheLedgerAnswerInTwoStatesOfTheRecord
	{
		private const string TheVin = "1FTFW1ET5DFC12345";

		// EL ANIO ENTERO. Los dos limites son INSTANTES y los dos son inclusivos: los del libro no son
		// dias de calendario, que es G4 y lo que separa APeriodOfInstants de ADayRange.
		private static readonly DateTime TheYearFrom =
			new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheYearTo =
			new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc);

		// EL PRIMER SEMESTRE: agarra las dos de colones -- la duradera y el cambio de aceite.
		private static readonly DateTime TheColonesHalfFrom =
			new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheColonesHalfTo =
			new DateTime(2026, 6, 30, 23, 59, 59, DateTimeKind.Utc);

		// EL ANIO ANTERIOR: ninguna entrada se incurrio adentro y la ventana de la duradera empieza
		// despues, asi que tampoco consume nada adentro.
		private static readonly DateTime TheEmptyYearFrom =
			new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheEmptyYearTo =
			new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc);

		// LAS TRES ENTRADAS, en orden del instante en que se incurrieron.
		private const string TheColon = "CRC";
		private const string TheDollar = "USD";

		private const string TheTyres = "Llantas nuevas";
		private const decimal TheTyresCost = 240000m;
		private const int FourYearsOfLife = 48;

		private static readonly DateTime TheTyresIncurred =
			new DateTime(2026, 3, 12, 9, 0, 0, DateTimeKind.Utc);

		private const string TheOilChange = "Cambio de aceite";
		private const decimal TheOilChangeCost = 45000m;

		private static readonly DateTime TheOilChangeIncurred =
			new DateTime(2026, 5, 20, 10, 30, 0, DateTimeKind.Utc);

		private const string TheWash = "Lavado premium";
		private const decimal TheWashCost = 120.50m;

		private static readonly DateTime TheWashIncurred =
			new DateTime(2026, 8, 5, 14, 15, 0, DateTimeKind.Utc);

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

		// ===== ESTADO UNO: NINGUNA ENTRADA EN EL LIBRO ====================================

		// LAS SIETE CONTESTAN SIN NINGUNA ENTRADA, y ninguna revienta. Es la polaridad que §7.2 pide
		// TOTAL en las siete: la ausencia es un TIPO -- NoCostOnRecord -- y el periodo de dos monedas
		// es un ESTADO (DEC-95), asi que no hay nada que rechazar y ninguna de las siete necesita el
		// pre-chequeo de DEC-30. Escribirle uno seria un hallazgo sobre el dominio y no un arreglo.
		[Test]
		public void EveryOneOfTheSevenAnswersWhenNoCostIsOnRecord()
		{
			Assert.That(journal.Api.HasAnyCostBetween(TheYearFrom, TheYearTo), Is.False,
				"HasAnyCostBetween sin ninguna entrada.");

			Assert.That(
				journal.Api.DescribeSpendBetween(TheYearFrom, TheYearTo),
				Is.EqualTo(
					"Entre el 2026-01-01 00:00:00 y el 2026-12-31 23:59:59 no hay ningún gasto "
					+ "registrado para este vehículo."),
				"DescribeSpendBetween nombra LOS DOS EXTREMOS con su hora: un cero no diria entre "
				+ "cuando y cuando se pregunto, y un cero de dinero se lee como un gasto que hubo.");

			Assert.That(
				journal.Api.IsSpendSaidInOneCurrencyBetween(TheYearFrom, TheYearTo), Is.False,
				"IsSpendSaidInOneCurrencyBetween contesta NO sin ninguna entrada: no hay ninguna "
				+ "moneda en la que este dicho, y contestar que si seria devolver el miembro benigno "
				+ "del conjunto de salida.");

			Assert.That(
				journal.Api.DescribeSpendPerCurrencyBetween(TheYearFrom, TheYearTo),
				Does.Contain("no hay ninguna moneda en la que decirlo"),
				"DescribeSpendPerCurrencyBetween dice ademas que no hay moneda: es su propia frase y "
				+ "no la de DescribeSpendBetween.");

			Assert.That(
				journal.Api.DescribeTwoPeriodsOfSpendCompared(
					TheYearFrom, TheYearTo, TheEmptyYearFrom, TheEmptyYearTo),
				Does.Contain("Primer período").And.Contains("Segundo período")
					.And.Contains("2025-01-01 00:00:00"),
				"DescribeTwoPeriodsOfSpendCompared nombra los DOS periodos, cada uno con su propia "
				+ "constancia: los CUATRO extremos atan por separado.");

			Assert.That(journal.Api.CostEntryCountBetween(TheYearFrom, TheYearTo), Is.EqualTo(0),
				"CostEntryCountBetween sin ninguna entrada.");

			CostLedgerRecord record = journal.Api.CostEntriesBetween(TheYearFrom, TheYearTo);

			Assert.That(record.IncurredAt, Is.Empty, "CostEntriesBetween: la columna IncurredAt, vacia.");
			Assert.That(record.Subject, Is.Empty);
			Assert.That(record.Money, Is.Empty);
			Assert.That(record.Classification, Is.Empty);
			Assert.That(record.ExpectedLife, Is.Empty);
			Assert.That(record.Says, Is.Empty);
		}

		// ===== ESTADO DOS: UNA DURADERA Y DOS RECURRENTES, EN DOS MONEDAS =================

		// EL ESTADO QUE HACE QUE EL BARRIDO MIDA ALGO. Las siete contestan, cada una lo suyo y por
		// valor, y ninguna de las respuestas se parece a otra.
		[Test]
		public void EveryOneOfTheSevenAnswersWhenOneDurableAndTwoRecurringInTwoCurrenciesAreOnRecord()
		{
			TheThreeEntriesInTwoCurrencies();

			Assert.That(journal.Api.HasAnyCostBetween(TheYearFrom, TheYearTo), Is.True,
				"HasAnyCostBetween en el anio que tiene las tres.");
			Assert.That(journal.Api.HasAnyCostBetween(TheEmptyYearFrom, TheEmptyYearTo), Is.False,
				"Y NO en 2025, que no tiene ninguna y donde la ventana de la duradera todavia no "
				+ "empezaba: es lo que separa esta lectura de 'el libro tiene algo'.");

			Assert.That(
				journal.Api.IsSpendSaidInOneCurrencyBetween(TheColonesHalfFrom, TheColonesHalfTo),
				Is.True,
				"IsSpendSaidInOneCurrencyBetween en el semestre que solo tiene colones.");
			Assert.That(
				journal.Api.IsSpendSaidInOneCurrencyBetween(TheYearFrom, TheYearTo), Is.False,
				"Y NO en el anio entero, que mezcla monedas. Las TRES situaciones de DEC-95, cada una "
				+ "por su lado, sobre las mismas tres entradas.");

			// LAS DOS VISTAS EN UNA SOLA RESPUESTA, QUE ES L6. La caja del semestre son las dos
			// entradas de colones enteras; lo consumido es el cambio de aceite entero mas la parte
			// que a ese semestre le toca de las llantas, y las dos cifras TIENEN que diferir -- ese es
			// el caso de las llantas de marzo que el brief pide en su prueba 8.
			string theColonesHalf =
				journal.Api.DescribeSpendBetween(TheColonesHalfFrom, TheColonesHalfTo);

			Assert.That(theColonesHalf, Does.Contain("Salieron ₡285000.00 del banco"),
				"DescribeSpendBetween: la caja del semestre, sumada por el DOMINIO -- 240000 mas "
				+ "45000.");
			Assert.That(theColonesHalf, Does.Contain("el período consumió ₡"),
				"Y lo consumido en la MISMA respuesta: ningun miembro contesta una vista sola.");
			Assert.That(theColonesHalf, Does.Not.Contain("consumió ₡285000.00"),
				"Y las dos cifras DIFIEREN: la duradera se reparte a lo largo de 48 meses, asi que "
				+ "'compró llantas' no es 'el carro se puso caro'.");
			Assert.That(
				theColonesHalf,
				Does.Contain(
					"₡240000.00 en Llantas nuevas, en línea recta a lo largo de 48 meses desde el "
					+ "2026-03-12 09:00:00"),
				"Y la REGLA con la que se reparte va dicha adentro, con el entero, el asunto, la vida "
				+ "util y el instante de anclaje: G2 pide que la persona pueda ver la regla en vez de "
				+ "confiar en ella.");

			// LA LECTURA POR MONEDA DICE LO SUYO Y NO LO DE SU VECINA, Y ESTE RENGLON SE GANO CON UNA
			// FALSIFICACION. Con DescribeSpendPerCurrencyBetween llamando al miembro de
			// DescribeSpendBetween, esta prueba se quedaba VERDE: sobre el anio de dos monedas
			// ASpendNotSaidAcrossCurrencies.DescribePerCurrency() devuelve Describe() letra por letra,
			// asi que las dos contestan lo mismo y ahi no hay nada que discriminar. Donde SI difieren
			// es en el periodo de UNA sola moneda, que es donde la de por moneda agrega su propia
			// clausula -- y por eso la afirmacion se toma aca. Es C60 en la forma que SH-4 §3 encontro
			// primero: el discriminador tiene que ser algo que solo ESE miembro dice.
			Assert.That(
				journal.Api.DescribeSpendPerCurrencyBetween(TheColonesHalfFrom, TheColonesHalfTo),
				Does.Contain("todo el gasto está dicho en una sola moneda"),
				"DescribeSpendPerCurrencyBetween en el periodo de una sola moneda dice su propia "
				+ "clausula, que DescribeSpendBetween no dice.");
			Assert.That(
				journal.Api.DescribeSpendBetween(TheColonesHalfFrom, TheColonesHalfTo),
				Does.Not.Contain("todo el gasto está dicho en una sola moneda"),
				"Y la otra NO la dice: sin este renglon, la de arriba pasaria tambien si las dos "
				+ "lecturas llamaran al mismo miembro del dominio.");

			// EL ANIO ENTERO ESTA EN DOS MONEDAS, asi que cada una se dice por su lado y no hay total
			// entre ellas.
			string theYear = journal.Api.DescribeSpendPerCurrencyBetween(TheYearFrom, TheYearTo);

			Assert.That(theYear, Does.Contain("2 monedas distintas"));
			Assert.That(theYear, Does.Contain("no hay un total entre ellas"));
			Assert.That(theYear, Does.Contain("₡285000.00"), "El colon por su lado.");
			Assert.That(theYear, Does.Contain("$120.50"), "Y el dolar con su propio signo.");
			Assert.That(theYear, Does.Not.Contain("285120"),
				"Y NINGUN total entre las dos: no hay factor con que sumarlas, y no lo hay a "
				+ "proposito.");

			Assert.That(
				journal.Api.DescribeTwoPeriodsOfSpendCompared(
					TheColonesHalfFrom, TheColonesHalfTo, TheEmptyYearFrom, TheEmptyYearTo),
				Does.Contain("Primer período: Entre el 2026-01-01 00:00:00")
					.And.Contains("Segundo período: Entre el 2025-01-01 00:00:00")
					.And.Contains("no hay ningún gasto registrado"),
				"DescribeTwoPeriodsOfSpendCompared: el PRIMER par de extremos es el primer periodo y "
				+ "el SEGUNDO es el segundo, y ese orden es parte del contrato. El que no tiene gasto "
				+ "tambien se dice, con su propia constancia.");

			Assert.That(journal.Api.CostEntryCountBetween(TheYearFrom, TheYearTo), Is.EqualTo(3),
				"CostEntryCountBetween: las TRES del anio.");
			Assert.That(
				journal.Api.CostEntryCountBetween(TheColonesHalfFrom, TheColonesHalfTo),
				Is.EqualTo(2),
				"Y DOS en el semestre, que es lo que hace que la cuenta sea del periodo y no del libro "
				+ "entero.");
			Assert.That(
				journal.Api.CostEntryCountBetween(TheEmptyYearFrom, TheEmptyYearTo), Is.EqualTo(0));

			CostLedgerRecord record = journal.Api.CostEntriesBetween(TheYearFrom, TheYearTo);

			Assert.That(
				record.IncurredAt,
				Is.EqualTo(new[]
				{
					"2026-03-12 09:00:00", "2026-05-20 10:30:00", "2026-08-05 14:15:00",
				}),
				"CostEntriesBetween: las tres filas en orden del instante en que se INCURRIERON, y "
				+ "los instantes con su hora -- un instante nunca se dice como un dia de calendario a "
				+ "secas, porque de que dia local es necesitaria una zona que este libro no tiene.");

			Assert.That(record.Subject, Is.EqualTo(new[] { TheTyres, TheOilChange, TheWash }),
				"Los asuntos con las palabras que llegaron, sin tocar: G3 es texto recibido.");

			Assert.That(
				record.Money, Is.EqualTo(new[] { "₡240000.00", "₡45000.00", "$120.50" }),
				"El dinero dicho por el DOMINIO, con el signo y los decimales de cada moneda, y la "
				+ "tercera fila en OTRA moneda: un cruce entre columnas cambia esta afirmacion.");

			Assert.That(
				record.Classification,
				Is.EqualTo(new[] { "Durable", "Recurring", "Recurring" }),
				"La clase por el NOMBRE del miembro, y la puso el comando que registro cada una: no "
				+ "hay ninguna ranura por donde decirla.");

			Assert.That(record.ExpectedLife, Is.EqualTo(new[] { "48 meses", "", "" }),
				"La vida util solo la lleva la duradera. La celda vacia dice que no tiene; un cero se "
				+ "leeria como una vida de cero meses.");

			Assert.That(
				record.Says[0],
				Does.Contain("₡240000.00 en Llantas nuevas el 2026-03-12 09:00:00")
					.And.Contains("Duradero"),
				"Y la frase de cada fila la dice entera. EL ASUNTO SE AFIRMA ADENTRO DE SU PROPIA "
				+ "CLAUSULA (C60): un Does.Contain del asunto suelto pasaria con la columna cruzada, "
				+ "porque la frase tambien lo nombra.");
			Assert.That(
				record.Says[2],
				Does.Contain("$120.50 en Lavado premium el 2026-08-05 14:15:00")
					.And.Contains("Recurrente"),
				"Y la tercera fila dice lo suyo: otra moneda, otra clase y otro asunto que la "
				+ "primera.");
		}

		// ===== REGLA 2 DEL §8 DEL BRIEF: UNA FILA SIRVE SOLA ==============================

		// CADA FILA SE DESCRIBE A SI MISMA -- su asunto, su dinero con su moneda, su instante y su
		// clase -- Y LAS SEIS COLUMNAS SON PARALELAS. El paralelismo esta garantizado por
		// construccion en las dos puntas: en el dominio, seis miembros que abren con el mismo
		// TheSpendBetween (DEC-53), y en la puerta, CostLedgerRecord.Of, que refusa columnas de
		// largos distintos.
		//
		// LO QUE ESE PARALELISMO NO PUEDE VER ES UN CRUCE ENTRE DOS COLUMNAS: los largos siguen
		// coincidiendo perfectamente. Por eso cada celda se afirma ADENTRO de la clausula que la
		// nombra en la frase de su propia fila, que es el discriminador mas barato (C60).
		[Test]
		public void EveryRowOfTheRecordSaysItselfInsideItsOwnSentence()
		{
			TheThreeEntriesInTwoCurrencies();

			CostLedgerRecord record = journal.Api.CostEntriesBetween(TheYearFrom, TheYearTo);

			int rows = journal.Api.CostEntryCountBetween(TheYearFrom, TheYearTo);

			Assert.That(rows, Is.EqualTo(3),
				"El periodo tiene que traer tres filas para que el barrido de abajo mida algo.");

			foreach (string column in new[]
			{
				"IncurredAt", "Subject", "Money", "Classification", "ExpectedLife", "Says",
			})
				Assert.That(TheLengthOf(record, column), Is.EqualTo(rows),
					"La columna '" + column + "' no mide lo mismo que CostEntryCountBetween. Una "
					+ "columna corta por uno emparejaria en silencio cada fila posterior con otra "
					+ "entrada.");

			for (int row = 0; row < rows; row++)
			{
				Assert.That(
					record.Says[row],
					Does.Contain(record.Money[row] + " en " + record.Subject[row] + " el "
						+ record.IncurredAt[row]),
					"La fila " + row + " no se dice a si misma. Las tres celdas se afirman DENTRO de "
					+ "la clausula que las nombra -- '<dinero> en <asunto> el <instante>' -- porque "
					+ "un Does.Contain de cada una suelta pasaria con dos columnas cruzadas.");

				Assert.That(
					record.Says[row],
					Does.Contain(TheWordTheDomainUsesFor(record.Classification[row])),
					"Y la fila " + row + " dice de que CLASE es, con la palabra que el dominio usa "
					+ "para el nombre que trae su propia columna.");
			}

			Assert.That(record.Subject[0], Is.Not.EqualTo(record.Subject[1]),
				"Y las tres filas son distintas entre si: con tres filas iguales, copiar la fila cero "
				+ "sobre las otras dos pasaria todo lo de arriba.");
			Assert.That(record.Money[0], Is.Not.EqualTo(record.Money[2]));
			Assert.That(record.Classification[0], Is.Not.EqualTo(record.Classification[1]));
		}

		// EL PARALELISMO ES POR CONSTRUCCION. Se le pide al DTO que arme una fila con una columna
		// corta por uno y refusa, asi que la ley no depende de que los seis miembros del dominio
		// caminen el mismo resolvedor por costumbre.
		[Test]
		public void TheRecordRefusesColumnsOfDifferentLengthsAndAnAbsentColumn()
		{
			MethodInfo build = typeof(CostLedgerRecord).GetMethod(
				"Of", BindingFlags.Static | BindingFlags.NonPublic);

			Assert.That(build, Is.Not.Null,
				"El DTO se arma por una fabrica interna: sin ella, la puerta podria construir una "
				+ "fila desalineada y nadie lo notaria hasta la pantalla.");

			TargetInvocationException ragged = Assert.Throws<TargetInvocationException>(
				() => build.Invoke(null, new object[]
				{
					new[] { "a", "b" },
					new[] { "a", "b" },
					new[] { "a", "b" },
					new[] { "a", "b" },
					new[] { "a" },
					new[] { "a", "b" },
				}));

			Assert.That(ragged.InnerException, Is.TypeOf<InvalidOperationException>());
			Assert.That(ragged.InnerException.Message, Does.Contain("2").And.Contains("1"),
				"Y el rechazo dice los dos largos que no calzan.");

			TargetInvocationException absent = Assert.Throws<TargetInvocationException>(
				() => build.Invoke(null, new object[]
				{
					new[] { "a" }, new[] { "a" }, new[] { "a" }, new[] { "a" }, null, new[] { "a" },
				}));

			Assert.That(absent.InnerException, Is.TypeOf<InvalidOperationException>());
			Assert.That(
				absent.InnerException.Message, Does.Contain("ausente"),
				"Y una columna AUSENTE no es una columna vacia: un Out que el script declara y nunca "
				+ "asigna revienta al leerse, asi que una columna nula es otra cosa y se dice.");
		}

		// REGLA 4 DEL §8 DEL BRIEF: EL TOTAL DEL DOMINIO ES IGUAL A LA SUMA SOBRE LAS FILAS. No
		// porque una caratula pueda sumarlas -- eso lo prohibe la regla 3 y lo barre el barrido de
		// aritmetica -- sino para que nunca sea tentador ni correcto hacerlo.
		//
		// SE AFIRMA DEL SEMESTRE DE COLONES Y NO DEL ANIO, y por una razon: en el anio hay dos
		// monedas y no hay UN total que comparar contra UNA suma, que es justamente lo que
		// ASpendNotSaidAcrossCurrencies dice.
		//
		// Y SE AFIRMA DE LA VISTA DE CAJA SOLAMENTE, dicho como LIMITE y no tapado con una prueba que
		// se le parezca (C56). Las filas son las entradas INCURRIDAS adentro, que es exactamente la
		// caja; lo CONSUMIDO no tiene columna contra la que compararse, porque una parte por fila
		// seria la mitad de la respuesta de L6 con una tabla encima -- CL-3 lo argumento y afirma su
		// ausencia, y agregarla pone esa afirmacion roja a proposito.
		[Test]
		public void TheDomainsCashOutEqualsTheSumOverTheRowsOfTheSamePeriod()
		{
			TheThreeEntriesInTwoCurrencies();

			CostLedgerRecord record =
				journal.Api.CostEntriesBetween(TheColonesHalfFrom, TheColonesHalfTo);

			Assert.That(record.Money, Has.Length.EqualTo(2),
				"El semestre tiene que traer dos filas para que haya una suma que hacer.");

			decimal summedOverTheRows = 0m;

			foreach (string cell in record.Money) summedOverTheRows += TheAmountInside(cell);

			Assert.That(
				journal.Api.DescribeSpendBetween(TheColonesHalfFrom, TheColonesHalfTo),
				Does.Contain("Salieron ₡" + summedOverTheRows.ToString("0.00") + " del banco"),
				"La caja que el dominio dice y la suma de las filas del MISMO periodo tienen que ser "
				+ "la misma cifra. Las dos mitades se arman desde las columnas del registro y no "
				+ "desde literales: armada con literales, esta afirmacion pasaria contra un registro "
				+ "que trajera otras filas.");
		}

		// ===== EL BORDE DE LA PUERTA: EL INSTANTE SE RECORTA A SEGUNDOS ENTEROS ============

		// LAS SIETE PASAN SUS EXTREMOS POR InUtc, igual que toda la puerta. Se ve porque un limite con
		// milisimas RECORTA HACIA ABAJO: un limite de 09:00:00.999 incluye la entrada de 09:00:00, y
		// uno de 08:59:59.999 la deja afuera.
		//
		// SIN ESTE RENGLON, UN LIMITE ATADO CRUDO PASARIA TODAS LAS DEMAS PRUEBAS DE ESTE ARCHIVO,
		// porque todas preguntan con instantes que ya son segundos enteros.
		[Test]
		public void TheSevenNormalizeEachBoundToAWholeSecondBeforeAsking()
		{
			TheThreeEntriesInTwoCurrencies();

			Assert.That(
				journal.Api.CostEntryCountBetween(
					TheTyresIncurred.AddMilliseconds(999), TheYearTo),
				Is.EqualTo(3),
				"Un extremo de arranque de 09:00:00.999 se recorta a 09:00:00, asi que la entrada de "
				+ "las nueve en punto queda ADENTRO. Atado crudo, quedaria afuera y la cuenta seria "
				+ "dos.");

			Assert.That(
				journal.Api.CostEntryCountBetween(
					TheYearFrom, TheTyresIncurred.AddMilliseconds(-1)),
				Is.EqualTo(0),
				"Y un extremo de llegada de 08:59:59.999 se recorta a 08:59:59, que sigue siendo "
				+ "antes de la entrada: el recorte va HACIA ABAJO en los dos extremos.");

			Assert.That(
				journal.Api.HasAnyCostBetween(TheTyresIncurred.AddMilliseconds(999), TheYearTo),
				Is.True,
				"Y el mismo recorte en los extremos de las otras seis.");
		}

		// ===== [REPLAY], MITAD C23: EL SEGUNDO PROGRAMA DE DOS CORRIDAS ===================

		// DOS CORRIDAS DE LA MISMA SECUENCIA RECIBIDA DEJAN EL MISMO SEGUNDO PROGRAMA -- el texto del
		// DSL y sus argumentos -- Y CONTESTAN LO MISMO. NO byte a byte: cada corrida anota su propio
		// instante de ocurrencia, asi que dos journals de la misma secuencia difieren ahi y tienen
		// que diferir (C23). Byte a byte es la comprobacion correcta para una operacion que no debe
		// escribir NADA, y esa es la de abajo.
		[Test]
		public void TwoRunsOfTheSameSequenceLeaveTheSameSecondProgramAndAnswerTheSame()
		{
			TheThreeEntriesInTwoCurrencies();

			string firstProgram = TheSecondProgramOf(journal.Folder);
			Dictionary<string, string> firstAnswers = TheSevenAnswers();

			journal.Dispose();
			journal = new AScheduleOnARealJournal(TheVin, "Standard");

			TheThreeEntriesInTwoCurrencies();

			Assert.That(TheSecondProgramOf(journal.Folder), Is.EqualTo(firstProgram),
				"Las dos corridas dijeron dos programas distintos. Lo que tiene que ser identico es "
				+ "el TEXTO del DSL y sus argumentos: los siete scripts son consultas y no escriben, "
				+ "asi que lo unico que este programa lleva son los dos comandos del libro.");

			foreach (KeyValuePair<string, string> each in firstAnswers)
				Assert.That(TheSevenAnswers()[each.Key], Is.EqualTo(each.Value),
					"'" + each.Key + "' contesto distinto en la segunda corrida de la misma "
					+ "secuencia.");
		}

		// ===== [REPLAY], MITAD C53: UNA RESPUESTA VIVA CONTRA UNA REPRODUCIDA =============

		// LO QUE NINGUNA COMPARACION DEL JOURNAL PUEDE TOCAR (C53): las siete respuestas VIVAS contra
		// las REPRODUCIDAS. El segundo programa no ve una normalizacion del borde que el journal
		// tambien hace; la unica que la ve es esta.
		[Test]
		public void TheSevenAnswerTheSameAfterTheJournalIsClosedAndReopened()
		{
			TheThreeEntriesInTwoCurrencies();

			Dictionary<string, string> live = TheSevenAnswers();

			journal.CloseAndReopenFromTheJournal();

			Dictionary<string, string> replayed = TheSevenAnswers();

			foreach (KeyValuePair<string, string> each in live)
				Assert.That(replayed[each.Key], Is.EqualTo(each.Value),
					"'" + each.Key + "' contesto distinto despues de reabrir el journal.");
		}

		// Y LAS SIETE ESTABAN CONTESTANDO ALGO EN ESE ESTADO DEL LIBRO (C61). Sin este renglon la
		// comparacion de arriba pasaria con las siete contestando la ausencia de acuerdo sobre nada.
		[Test]
		public void AndTheSevenAreAnsweringSomethingInThatStateOfTheRecord()
		{
			TheThreeEntriesInTwoCurrencies();

			foreach (KeyValuePair<string, string> each in TheSevenAnswers())
				Assert.That(each.Value, Is.Not.Empty,
					"'" + each.Key + "' no contesto nada en el estado en que se toma la comparacion.");

			Assert.That(TheSevenAnswers()["CostEntryCountBetween"], Is.EqualTo("3"));
			Assert.That(TheSevenAnswers()["HasAnyCostBetween"], Is.EqualTo("True"),
				"Y la comparacion se toma en un estado donde el libro SI tiene gasto: tomada sobre un "
				+ "libro vacio, no compararia ninguna cifra.");
		}

		// LA MITAD DE C53 QUE ESTE MODULO NO PUEDE PONER ROJA, DICHA COMO LIMITE Y NO TAPADA (C56).
		//
		// El journal le borra la ESCALA a un decimal: 145.00 escrito vuelve 145. La pregunta que esta
		// prueba hace es si un monto dicho vivo y el mismo monto dicho despues de reabrir pueden
		// discrepar, y CONTRA ESTA PUERTA NO PUEDEN, por dos razones que se afirman aca al lado en
		// vez de creerse:
		//
		//   Money.Expressed() redondea a las unidades menores de la moneda, asi que para una moneda
		//   que el conjunto NOMBRA la escala perdida no se ve.
		//
		//   Y WireName.Currency, que CL-4 le puso a los dos comandos, refusa todo codigo que el
		//   conjunto no nombre, asi que por esta puerta no entra ninguna moneda sin nombrar.
		//
		// LAS DOS JUNTAS CIERRAN EL CAMINO: no hay monto construible por esta puerta cuyo dicho vivo
		// difiera del reproducido. Se afirma igual, con los dos motivos al lado, porque el dia que
		// CL-7 abra el feed -- donde L14 pone el reconocedor y donde Currency.OfPublished sigue
		// siendo total por DEC-96 -- este renglon es el unico instrumento que lo veria.
		[Test]
		public void TheAmountsSayTheSameLiveAndReplayedAndTheTwoReasonsThatMakeThatTrueAreAssertedHere()
		{
			// UNA ESCALA QUE EL JOURNAL DEJA CAER: 45000.00 escrito, 45000 guardado.
			journal.Api.RecordRecurringCost(
				TheOilChangeIncurred, 45000.00m, TheColon, TheOilChange);

			string live = journal.Api.CostEntriesBetween(TheYearFrom, TheYearTo).Money[0];

			journal.CloseAndReopenFromTheJournal();

			Assert.That(
				journal.Api.CostEntriesBetween(TheYearFrom, TheYearTo).Money[0], Is.EqualTo(live),
				"El monto se dijo distinto vivo y reproducido. Contra esta puerta eso no puede pasar; "
				+ "el dia que pase, este es el renglon que lo dice.");

			Assert.That(live, Is.EqualTo("₡45000.00"),
				"Y la razon por la que no puede: el dominio DICE el monto en las unidades menores de "
				+ "su moneda, asi que la escala que el journal deja caer se vuelve a poner al "
				+ "decirlo.");

			Assert.That(
				Assert.Catch<ArgumentException>(
					() => journal.Api.RecordRecurringCost(
						TheOilChangeIncurred, 45000.00m, "XYZ", TheOilChange)),
				Is.Not.Null,
				"Y la segunda razon: por esta puerta no entra una moneda que el conjunto no nombre, "
				+ "que es la unica para la que Expressed() no tendria precision a la que volver. "
				+ "MIENTRAS LAS DOS SIGAN AHI, ESTE RENGLON NO SE PUEDE PONER ROJO por un monto -- y "
				+ "eso es lo que dice, en vez de un verde que parezca cobertura.");
		}

		// ===== Y NINGUNA DE LAS SIETE ESCRIBE ============================================

		// NINGUNA DE LAS SIETE ESCRIBE UNA ENTRADA. El error que esto atrapa es de una sola letra --
		// PerformCommand donde iba PerformQuery -- y el estado seguiria siendo correcto, asi que
		// ninguna otra prueba se pondria roja. Aca SI es byte a byte, que es la excepcion que C23
		// nombra: una operacion que no debe escribir nada.
		//
		// Y COMO ESTA TAREA NO AGREGA NINGUN COMANDO, NINGUNA GLOBAL DE RAIZ Y NINGUN NOMBRE DE
		// UPGRADE, este renglon es tambien la prueba de que NO HAY MIGRACION que hacer, y no la de
		// que se hizo una: la del libro es de CL-4 y la afirma TheSixthGlobalReachesAJournalThatHasTheOtherFive.
		[Test]
		public void NotOneOfTheSevenWritesAnEntrySoThereIsNothingToMigrate()
		{
			TheThreeEntriesInTwoCurrencies();

			string before = TheBytesOf(journal.Folder);

			TheSevenAnswers();

			Assert.That(TheBytesOf(journal.Folder), Is.EqualTo(before),
				"El journal quedo byte a byte como estaba. Una lectura que escribe le agrega una "
				+ "entrada al journal de la persona cada vez que abre la app, para siempre, y el "
				+ "estado sigue siendo correcto -- asi que ninguna otra prueba se pondria roja.");
		}

		// Las siete, cada una con su nombre al lado, reducidas a texto para poder compararlas de a
		// pares. El registro entero va en la cadena -- las SEIS columnas -- porque comparar solo la
		// cuenta dejaria pasar una columna que cambiara de contenido al reproducirse.
		private Dictionary<string, string> TheSevenAnswers()
		{
			CostLedgerRecord record = journal.Api.CostEntriesBetween(TheYearFrom, TheYearTo);

			return new Dictionary<string, string>
			{
				{
					"HasAnyCostBetween",
					journal.Api.HasAnyCostBetween(TheYearFrom, TheYearTo).ToString()
				},
				{
					"DescribeSpendBetween",
					journal.Api.DescribeSpendBetween(TheColonesHalfFrom, TheColonesHalfTo)
					+ " // " + journal.Api.DescribeSpendBetween(TheYearFrom, TheYearTo)
				},
				{
					"IsSpendSaidInOneCurrencyBetween",
					journal.Api.IsSpendSaidInOneCurrencyBetween(
						TheColonesHalfFrom, TheColonesHalfTo).ToString()
					+ " // "
					+ journal.Api.IsSpendSaidInOneCurrencyBetween(TheYearFrom, TheYearTo)
				},
				{
					"DescribeSpendPerCurrencyBetween",
					journal.Api.DescribeSpendPerCurrencyBetween(TheYearFrom, TheYearTo)
				},
				{
					"DescribeTwoPeriodsOfSpendCompared",
					journal.Api.DescribeTwoPeriodsOfSpendCompared(
						TheColonesHalfFrom, TheColonesHalfTo, TheEmptyYearFrom, TheEmptyYearTo)
				},
				{
					"CostEntryCountBetween",
					journal.Api.CostEntryCountBetween(TheYearFrom, TheYearTo).ToString()
				},
				{
					"CostEntriesBetween",
					string.Join("|", record.IncurredAt) + " / " + string.Join("|", record.Subject)
					+ " / " + string.Join("|", record.Money)
					+ " / " + string.Join("|", record.Classification)
					+ " / " + string.Join("|", record.ExpectedLife)
					+ " / " + string.Join("|", record.Says)
				},
			};
		}

		// El largo de una columna por su nombre, para que el rojo del barrido de columnas diga CUAL
		// columna se quedo sin filas en vez de solo que una lo hizo.
		private static int TheLengthOf(CostLedgerRecord record, string column)
		{
			if (column == "IncurredAt") return record.IncurredAt.Length;
			if (column == "Subject") return record.Subject.Length;
			if (column == "Money") return record.Money.Length;
			if (column == "Classification") return record.Classification.Length;
			if (column == "ExpectedLife") return record.ExpectedLife.Length;
			if (column == "Says") return record.Says.Length;

			throw new ArgumentException("El registro no tiene una columna '" + column + "'.", column);
		}

		// La palabra con la que el dominio dice cada clase en su frase. Escrita aca a proposito: la
		// columna lleva el NOMBRE del miembro y la frase lleva como se dice, y son dos cosas.
		private static string TheWordTheDomainUsesFor(string name)
		{
			if (name == "Durable") return "Duradero";
			if (name == "Recurring") return "Recurrente";

			throw new ArgumentException("La columna trajo una clase que no es ninguna de las dos: '"
				+ name + "'.", nameof(name));
		}

		// El monto de una celda de dinero, sin el signo de su moneda. Es aritmetica de la PRUEBA y no
		// de una caratula: lo que la regla 3 prohibe es que una caratula sume dos valores del
		// dominio, y lo que la regla 4 pide es exactamente esta comparacion.
		private static decimal TheAmountInside(string cell)
		{
			return decimal.Parse(
				cell.Substring(1), System.Globalization.NumberStyles.Number,
				System.Globalization.CultureInfo.InvariantCulture);
		}

		// El SEGUNDO PROGRAMA que el journal guarda: cada define, cada invoke y cada blob de
		// argumentos, en orden. Se saca leyendo los tramos imprimibles del binario, que es
		// exactamente lo que el motor escribe como texto -- los ticks de cada entrada y su checksum
		// son binarios y no forman tramos de este largo. Copiado letra por letra del que ya existe:
		// una variante propia compararia otra cosa y se veria igual.
		private static string TheSecondProgramOf(string folder)
		{
			StringBuilder program = new StringBuilder();

			List<string> files = new List<string>(
				Directory.GetFiles(folder, "*", SearchOption.AllDirectories));
			files.Sort(StringComparer.Ordinal);

			foreach (string file in files)
			{
				using (FileStream open = new FileStream(
					file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
				{
					byte[] content = new byte[open.Length];
					open.ReadExactly(content, 0, content.Length);

					StringBuilder run = new StringBuilder();

					foreach (byte each in content)
					{
						if (each >= 0x20 && each <= 0x7E) { run.Append((char)each); continue; }

						if (run.Length >= 6) program.AppendLine(run.ToString());

						run.Clear();
					}

					if (run.Length >= 6) program.AppendLine(run.ToString());
				}
			}

			return program.ToString();
		}

		// El arbol entero del journal reducido a una cadena: nombre relativo mas hash del contenido,
		// en orden. Repetido a proposito -- cada fixture de esta suite es dueño de su aparejo.
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

		// LAS TRES, EN ORDEN DEL INSTANTE EN QUE SE INCURRIERON. Con LITERALES y sin ningun reloj:
		// cualquier cosa que guione una secuencia recibida no lee ninguno (C27).
		private void TheThreeEntriesInTwoCurrencies()
		{
			journal.Api.RecordDurableCost(
				TheTyresIncurred, TheTyresCost, TheColon, TheTyres, FourYearsOfLife);

			journal.Api.RecordRecurringCost(
				TheOilChangeIncurred, TheOilChangeCost, TheColon, TheOilChange);

			journal.Api.RecordRecurringCost(TheWashIncurred, TheWashCost, TheDollar, TheWash);
		}
	}
}
