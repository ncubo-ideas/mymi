using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using DealershipCar.App;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LAS SEIS LECTURAS DE OD-2, CADA UNA EN LOS DOS ESTADOS DEL REGISTRO. Es la obligacion S5 -- un
	// error de tipeo en un script del DSL no se descubre al compilar, se descubre al ejecutarlo, y
	// deja una entrada permanente que hace loguear un fallo de rehidratacion en cada arranque futuro
	// -- aplicada a los seis scripts que esta tarea le agrega a la puerta.
	//
	// POR QUE DOS ESTADOS Y NO UNO (C61). Un registro sin ninguna lectura contesta la ausencia a las
	// seis, asi que un barrido tomado ahi compara seis ausencias que se ponen de acuerdo
	// correctamente sobre nada. El estado que hace que el barrido mida algo es el SEGUNDO: CINCO
	// lecturas en DOS unidades, con fuentes distintas y valores distintos.
	//
	// LAS CINCO LECTURAS Y SUS PERIODOS ESTAN ELEGIDOS PARA QUE UN SOLO ESTADO DEL REGISTRO DE LAS
	// CUATRO SITUACIONES QUE LA PUERTA PUEDE ALCANZAR. El anio entero mezcla unidades y queda
	// suspendido; enero-abril tiene dos lecturas en km y SI tiene ritmo; junio-diciembre tiene otras
	// dos y otro ritmo, asi que los dos se pueden comparar; mayo tiene una sola; 2025 no tiene
	// ninguna. La quinta situacion -- dos lecturas en el mismo segundo entero -- la cubre el nivel A
	// en TwoReadingsAtTheSameSecondAreAZeroLengthSpanAndNoConstantExists, y no se monta aca porque
	// agregarle al registro un par en el mismo segundo cambiaria los cuatro periodos de arriba.
	//
	// Y CADA RESPUESTA VA CON EL NOMBRE DE SU MIEMBRO AL LADO (C60). Is.Not.Empty prueba que llego
	// una respuesta, nunca que es la de ESE miembro.
	//
	// LOS VALORES ESTAN ELEGIDOS PARA QUE UNA RANURA SIN LIGAR NO PASE. Una cuenta de cero es lo que
	// contesta un int que el motor dejo por defecto, asi que en el segundo estado ninguna cuenta vale
	// cero, y las cuentas de los periodos valen numeros DISTINTOS entre si -- 5, 2, 2, 1, 0 -- para
	// que cruzar dos periodos cambie la afirmacion.
	[TestFixture]
	public sealed class TheSixReadsOfTheSeriesAnswerInTwoStatesOfTheRecord
	{
		private const string TheVin = "3VWDX7AJ5DM123456";

		// EL ANIO ENTERO. Los dos limites son INCLUSIVOS, igual que los del historial de estadias.
		private static readonly DateTime TheYearFrom =
			new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheYearTo =
			new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc);

		// EL PRIMER SEMESTRE: agarra las dos primeras lecturas, las dos en km.
		private static readonly DateTime TheFirstHalfFrom =
			new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheFirstHalfTo =
			new DateTime(2026, 4, 30, 23, 59, 59, DateTimeKind.Utc);

		// EL SEGUNDO SEMESTRE: agarra las dos ultimas, tambien en km y a otro ritmo.
		private static readonly DateTime TheSecondHalfFrom =
			new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheSecondHalfTo =
			new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc);

		// MAYO: agarra la unica lectura en millas, y una sola lectura no es un ritmo.
		private static readonly DateTime TheMonthOfTheLoneReadingFrom =
			new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheMonthOfTheLoneReadingTo =
			new DateTime(2026, 5, 31, 23, 59, 59, DateTimeKind.Utc);

		// EL ANIO ANTERIOR: ninguna lectura cae adentro.
		private static readonly DateTime TheEmptyYearFrom =
			new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheEmptyYearTo =
			new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc);

		// LAS CINCO LECTURAS, en orden de observacion. La tercera esta en MILLAS y el guardia de
		// insercion no la puede comparar con sus vecinas en km, asi que entra: §1.1 del brief, y es
		// por eso que el ritmo del anio entero queda suspendido y las filas siguen contestando.
		private const int TheJanuaryReading = 20000;
		private const int TheMarchReading = 22000;
		private const int TheForeignReading = 15000;
		private const int TheJulyReading = 26000;
		private const int TheSeptemberReading = 30000;

		private const string TheUnitOfThePeriod = "UnitA";
		private const string TheForeignUnit = "UnitB";

		private static readonly DateTime January =
			new DateTime(2026, 1, 10, 8, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime March =
			new DateTime(2026, 3, 10, 8, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime May =
			new DateTime(2026, 5, 15, 8, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime July =
			new DateTime(2026, 7, 20, 8, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime September =
			new DateTime(2026, 9, 25, 8, 0, 0, DateTimeKind.Utc);

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

		// ===== ESTADO UNO: NINGUNA LECTURA EN EL REGISTRO =================================

		// LAS SEIS CONTESTAN SIN NINGUNA LECTURA, y ninguna revienta. Es la polaridad que §9 del
		// brief pide TOTAL en las seis: la ausencia se dice, no se rechaza y no se inventa. Ninguna
		// de las seis tiene hermana guardada, asi que no hay pre-chequeo que hacer.
		[Test]
		public void EveryOneOfTheSixAnswersWhenNoReadingIsOnRecord()
		{
			Assert.That(journal.Api.CanARateOfUseBeSaid(TheYearFrom, TheYearTo), Is.False,
				"CanARateOfUseBeSaid sin ninguna lectura.");

			Assert.That(
				journal.Api.ReadRateOfUseStanding(TheYearFrom, TheYearTo),
				Is.EqualTo("SuspendedByAnAbsentReading"),
				"ReadRateOfUseStanding proyecta el NOMBRE del miembro, y la ausencia tiene el suyo.");

			Assert.That(
				journal.Api.DescribeRateOfUse(TheYearFrom, TheYearTo),
				Does.Contain("2026-01-01 00:00:00").And.Contains("2026-12-31 23:59:59")
					.And.Contains("ninguna lectura del odómetro"),
				"DescribeRateOfUse nombra LOS DOS EXTREMOS del periodo con su hora: un cero no diria "
				+ "entre cuando y cuando se pregunto.");

			Assert.That(
				journal.Api.DescribeTwoPeriodsOfUseCompared(
					TheSecondHalfFrom, TheSecondHalfTo, TheFirstHalfFrom, TheFirstHalfTo),
				Does.Contain("Primer periodo").And.Contains("Segundo periodo")
					.And.Contains("No se puede decir el cambio"),
				"DescribeTwoPeriodsOfUseCompared nombra los DOS periodos igual, y dice que el cambio "
				+ "no se puede decir en vez de callarse uno de los dos.");

			Assert.That(journal.Api.ReadingCountBetween(TheYearFrom, TheYearTo), Is.EqualTo(0),
				"ReadingCountBetween sin ninguna lectura.");

			OdometerSeriesRecord record = journal.Api.ReadingsBetween(TheYearFrom, TheYearTo);

			Assert.That(record.ObservedAt, Is.Empty, "ReadingsBetween: la columna ObservedAt, vacia.");
			Assert.That(record.Value, Is.Empty);
			Assert.That(record.Unit, Is.Empty);
			Assert.That(record.Source, Is.Empty);
			Assert.That(record.Says, Is.Empty);
		}

		// ===== ESTADO DOS: CINCO LECTURAS EN DOS UNIDADES =================================

		// EL ESTADO QUE HACE QUE EL BARRIDO MIDA ALGO. Las seis contestan, cada una lo suyo y por
		// valor, y ninguna de las respuestas se parece a otra.
		[Test]
		public void EveryOneOfTheSixAnswersWhenFiveReadingsInTwoUnitsAreOnRecord()
		{
			TheFiveReadingsInTwoUnits();

			Assert.That(journal.Api.CanARateOfUseBeSaid(TheFirstHalfFrom, TheFirstHalfTo), Is.True,
				"CanARateOfUseBeSaid en un periodo con dos lecturas en una sola unidad.");
			Assert.That(journal.Api.CanARateOfUseBeSaid(TheYearFrom, TheYearTo), Is.False,
				"Y NO en el anio entero, que mezcla unidades: es lo que separa esta lectura de "
				+ "'hay alguna lectura'.");

			Assert.That(
				journal.Api.ReadRateOfUseStanding(TheFirstHalfFrom, TheFirstHalfTo),
				Is.EqualTo("Measured"));
			Assert.That(
				journal.Api.ReadRateOfUseStanding(TheYearFrom, TheYearTo),
				Is.EqualTo("SuspendedByAUnitMismatch"));
			Assert.That(
				journal.Api.ReadRateOfUseStanding(
					TheMonthOfTheLoneReadingFrom, TheMonthOfTheLoneReadingTo),
				Is.EqualTo("SuspendedByASingleReading"));
			Assert.That(
				journal.Api.ReadRateOfUseStanding(TheEmptyYearFrom, TheEmptyYearTo),
				Is.EqualTo("SuspendedByAnAbsentReading"),
				"Las CUATRO situaciones que un solo estado del registro alcanza, cada una por su "
				+ "nombre: cuatro periodos distintos sobre las mismas cinco lecturas.");

			// LA FRASE DEL RITMO, ENTERA Y LETRA POR LETRA. El barrido de aritmetica defiende los
			// numeros; solo una afirmacion letra por letra defiende la frase.
			Assert.That(
				journal.Api.DescribeRateOfUse(TheFirstHalfFrom, TheFirstHalfTo),
				Is.EqualTo(
					"de 20000 km el 2026-01-10 08:00:00 a 22000 km el 2026-03-10 08:00:00: "
					+ "2000 km en 59 días, unos 34 km por día"),
				"DescribeRateOfUse: los dos extremos con sus instantes, la distancia exacta, el "
				+ "tiempo exacto, y la distancia por dia introducida por 'unos'.");

			Assert.That(
				journal.Api.DescribeRateOfUse(TheSecondHalfFrom, TheSecondHalfTo),
				Is.EqualTo(
					"de 26000 km el 2026-07-20 08:00:00 a 30000 km el 2026-09-25 08:00:00: "
					+ "4000 km en 67 días, unos 60 km por día"),
				"Y el otro semestre a OTRO ritmo, asi que cruzar los dos periodos cambia las dos "
				+ "afirmaciones.");

			Assert.That(
				journal.Api.DescribeTwoPeriodsOfUseCompared(
					TheSecondHalfFrom, TheSecondHalfTo, TheFirstHalfFrom, TheFirstHalfTo),
				Is.EqualTo(
					"Primer periodo: de 26000 km el 2026-07-20 08:00:00 a 30000 km el "
					+ "2026-09-25 08:00:00: 4000 km en 67 días, unos 60 km por día. Segundo periodo: "
					+ "de 20000 km el 2026-01-10 08:00:00 a 22000 km el 2026-03-10 08:00:00: "
					+ "2000 km en 59 días, unos 34 km por día. El cambio del primero respecto al "
					+ "segundo: un 76% más."),
				"DescribeTwoPeriodsOfUseCompared: los DOS periodos con sus DOS ritmos y el cambio "
				+ "adentro de la misma frase. El cambio es del PRIMERO respecto al SEGUNDO, y ese "
				+ "orden es parte del contrato.");

			Assert.That(journal.Api.ReadingCountBetween(TheYearFrom, TheYearTo), Is.EqualTo(5),
				"ReadingCountBetween: las CINCO del anio.");
			Assert.That(
				journal.Api.ReadingCountBetween(TheFirstHalfFrom, TheFirstHalfTo), Is.EqualTo(2),
				"Y DOS en el primer semestre, que es lo que hace que la cuenta sea del periodo y no "
				+ "de la serie entera.");
			Assert.That(
				journal.Api.ReadingCountBetween(
					TheMonthOfTheLoneReadingFrom, TheMonthOfTheLoneReadingTo),
				Is.EqualTo(1));
			Assert.That(
				journal.Api.ReadingCountBetween(TheEmptyYearFrom, TheEmptyYearTo), Is.EqualTo(0));

			OdometerSeriesRecord record = journal.Api.ReadingsBetween(TheYearFrom, TheYearTo);

			Assert.That(
				record.ObservedAt,
				Is.EqualTo(new[]
				{
					"2026-01-10 08:00:00", "2026-03-10 08:00:00", "2026-05-15 08:00:00",
					"2026-07-20 08:00:00", "2026-09-25 08:00:00",
				}),
				"ReadingsBetween: las cinco filas en orden de OBSERVACION, y los instantes con su "
				+ "hora -- un instante nunca se dice como un dia de calendario a secas.");

			Assert.That(
				record.Value,
				Is.EqualTo(new[]
				{
					TheJanuaryReading, TheMarchReading, TheForeignReading, TheJulyReading,
					TheSeptemberReading,
				}),
				"La columna de VALORES es int[] y vuelve por valor: un valor del odometro es el "
				+ "numero que es, y decirlo como texto obligaria a quien lee a volver a parsearlo.");

			Assert.That(record.Unit, Is.EqualTo(new[] { "km", "km", "mi", "km", "km" }),
				"Y las unidades por su nombre de mundo real, con la tercera DISTINTA: un cruce entre "
				+ "columnas cambia esta afirmacion.");

			Assert.That(
				record.Source,
				Is.EqualTo(new[]
				{
					"Telemetry", "HumanEntry", "CustomerDeclared", "Inspection", "Correction",
				}),
				"Cada fila dice de que FUENTE vino, por el nombre del miembro, y las cinco son "
				+ "distintas.");

			Assert.That(
				record.Says[2],
				Is.EqualTo("15000 mi el 2026-05-15 08:00:00, declarada por la persona"),
				"Y la frase de cada fila la dice entera: el valor con su unidad, cuando se observo y "
				+ "de donde vino, dicho en palabras.");
			Assert.That(
				record.Says[4],
				Is.EqualTo("30000 km el 2026-09-25 08:00:00, corrección de una lectura anterior"),
				"Una lectura correctiva lo dice en su propia fila.");
		}

		// ===== LA LEY CENTRAL DEL SLICE, EN EL BORDE DONDE SE MUERE EN SILENCIO ===========

		// LOS EXTREMOS DEL RITMO SON LA PRIMERA Y LA ULTIMA FILA DEL REGISTRO DEL MISMO PERIODO
		// (§7 regla 4). El nivel A lo fija sobre el dominio; la puerta es donde los dos podrian
		// separarse, porque son DOS lecturas distintas y cada una arma su propio script.
		//
		// LAS DOS MITADES SE ARMAN DESDE LAS COLUMNAS DEL REGISTRO y no desde literales: asi la
		// afirmacion no puede pasar con un registro que traiga otras filas.
		[Test]
		public void TheEndsOfTheRateAreTheFirstAndLastRowsOfTheSamePeriodsRecord()
		{
			TheFiveReadingsInTwoUnits();

			OdometerSeriesRecord record =
				journal.Api.ReadingsBetween(TheFirstHalfFrom, TheFirstHalfTo);

			Assert.That(record.Value, Has.Length.EqualTo(2),
				"El periodo tiene que traer dos filas para que haya dos extremos que comparar.");

			string said = journal.Api.DescribeRateOfUse(TheFirstHalfFrom, TheFirstHalfTo);

			int last = record.Value.Length - 1;

			Assert.That(
				said,
				Does.Contain(
					"de " + record.Value[0] + " " + record.Unit[0] + " el " + record.ObservedAt[0]),
				"El extremo de ARRANQUE del ritmo es la PRIMERA fila del registro del mismo periodo, "
				+ "con su valor, su unidad y su instante.");

			Assert.That(
				said,
				Does.Contain(
					"a " + record.Value[last] + " " + record.Unit[last] + " el "
					+ record.ObservedAt[last]),
				"Y el extremo de LLEGADA es la ULTIMA. Si los dos se separaran, la pantalla pintaria "
				+ "una tabla y una frase que no hablan del mismo tramo.");
		}

		// G5 EN LA PUERTA: EL RITMO SE SUSPENDE Y LAS CINCO COLUMNAS SIGUEN CONTESTANDO. L3 tal como
		// esta escrita hace que UNA lectura en otra unidad adentro del periodo suspenda el ritmo del
		// periodo entero; lo que hace que eso sobreviva a datos de verdad es que el REGISTRO
		// sobrevive cuando el RITMO no, y que las filas dicen CUAL lectura fue.
		//
		// LAS DOS MITADES VAN EN LA MISMA PRUEBA a proposito: separadas, una pantalla que esconda la
		// tabla cuando la frase dice "suspendido" pasaria las dos.
		[Test]
		public void AForeignUnitInsideThePeriodSuspendsTheRateAndTheFiveColumnsStillAnswer()
		{
			TheFiveReadingsInTwoUnits();

			Assert.That(journal.Api.CanARateOfUseBeSaid(TheYearFrom, TheYearTo), Is.False);

			Assert.That(
				journal.Api.DescribeRateOfUse(TheYearFrom, TheYearTo),
				Is.EqualTo(
					"no se puede decir cuánto se maneja: el periodo tiene lecturas en km y en mi, y "
					+ "las unidades nunca se convierten. La segunda unidad la introdujo la lectura de "
					+ "15000 mi del 2026-05-15 08:00:00"),
				"La suspension nombra LAS DOS unidades y la lectura que introdujo la segunda, con su "
				+ "instante: 'hay unidades mezcladas' es una categoria, y esa lectura es algo que "
				+ "alguien puede ir a arreglar.");

			OdometerSeriesRecord record = journal.Api.ReadingsBetween(TheYearFrom, TheYearTo);

			Assert.That(journal.Api.ReadingCountBetween(TheYearFrom, TheYearTo), Is.EqualTo(5),
				"Y la cuenta del periodo contesta igual con el ritmo suspendido.");

			foreach (string column in new[] { "ObservedAt", "Value", "Unit", "Source", "Says" })
				Assert.That(TheLengthOf(record, column), Is.EqualTo(5),
					"La columna '" + column + "' del registro tenia que seguir contestando con el "
					+ "ritmo suspendido. Que las filas se pierdan cuando la frase se suspende es lo "
					+ "que dejaria el slice inservible sobre datos de verdad.");

			Assert.That(record.Unit[2], Is.EqualTo("mi"),
				"Y la fila que trajo la segunda unidad se ve en la columna: es la que la frase "
				+ "nombra.");
		}

		// ===== EL PERIODO AL REVES: UNA RESPUESTA VISIBLEMENTE ACOTADA =====================

		// UN PERIODO CUYO SEGUNDO EXTREMO ES ANTERIOR AL PRIMERO NO SE RECHAZA EN LA PUERTA, y esa
		// es una decision y no una omision: las seis lecturas contestan la ausencia NOMBRANDO LOS DOS
		// EXTREMOS que se le dieron, asi que un limite inventado produce una respuesta visiblemente
		// acotada y nunca un hecho falso. Rechazarlo dejaria a las seis de ser totales.
		//
		// ES LO CONTRARIO DE ADayRange, Y LA DIFERENCIA ES EL TIPO. Un dia llega como CADENA y puede
		// no decir ningun dia -- ahi el rechazo es lo unico honesto, porque no hay periodo que
		// nombrar. Un DateTime ya dice un instante: los dos extremos existen, y con los dos se puede
		// contestar.
		//
		// LOS DATOS SON LOS QUE HACEN QUE LA ELECCION EQUIVOCADA SE VEA: el periodo al reves cubre
		// justo el tramo donde SI hay cinco lecturas, asi que un extremo puesto en orden por la
		// puerta contestaria un ritmo en vez de la ausencia.
		[Test]
		public void APeriodThatEndsBeforeItBeginsIsAnsweredWithBothBoundsNamedAndIsNotRefused()
		{
			TheFiveReadingsInTwoUnits();

			Assert.That(journal.Api.CanARateOfUseBeSaid(TheYearTo, TheYearFrom), Is.False,
				"CanARateOfUseBeSaid al reves: contesta, y contesta que no.");

			Assert.That(
				journal.Api.ReadRateOfUseStanding(TheYearTo, TheYearFrom),
				Is.EqualTo("SuspendedByAnAbsentReading"));

			Assert.That(
				journal.Api.DescribeRateOfUse(TheYearTo, TheYearFrom),
				Is.EqualTo(
					"no se puede decir cuánto se maneja entre el 2026-12-31 23:59:59 y el "
					+ "2026-01-01 00:00:00: no hay ninguna lectura del odómetro en ese periodo"),
				"Y la frase nombra los dos extremos EN EL ORDEN EN QUE LLEGARON. Si la puerta los "
				+ "ordenara, esta misma pregunta contestaria el ritmo de las cinco lecturas y quien "
				+ "pregunto no sabria que le cambiaron el periodo.");

			Assert.That(journal.Api.ReadingCountBetween(TheYearTo, TheYearFrom), Is.EqualTo(0));
			Assert.That(journal.Api.ReadingsBetween(TheYearTo, TheYearFrom).Says, Is.Empty);

			Assert.That(
				journal.Api.DescribeTwoPeriodsOfUseCompared(
					TheYearTo, TheYearFrom, TheYearTo, TheYearFrom),
				Does.Contain("No se puede decir el cambio"));
		}

		// ===== EL BORDE DE LA PUERTA: EL INSTANTE SE RECORTA A SEGUNDOS ENTEROS ============

		// LOS SEIS PASAN SUS EXTREMOS POR InUtc, igual que toda la puerta. Se ve porque un limite con
		// milisimas RECORTA HACIA ABAJO: un limite de 08:00:00.999 incluye la lectura de 08:00:00, y
		// uno de 07:59:59.999 la deja afuera.
		//
		// SIN ESTE RENGLON, UN LIMITE ATADO CRUDO PASARIA TODAS LAS DEMAS PRUEBAS DE ESTE ARCHIVO,
		// porque todas preguntan con instantes que ya son segundos enteros.
		[Test]
		public void TheSixNormalizeEachBoundToAWholeSecondBeforeAsking()
		{
			TheFiveReadingsInTwoUnits();

			DateTime justAfterTheJanuaryReading =
				January.AddMilliseconds(999);

			Assert.That(
				journal.Api.ReadingCountBetween(justAfterTheJanuaryReading, TheYearTo),
				Is.EqualTo(5),
				"Un extremo de arranque de 08:00:00.999 se recorta a 08:00:00, asi que la lectura de "
				+ "las ocho en punto queda ADENTRO. Atado crudo, quedaria afuera y la cuenta seria "
				+ "cuatro.");

			Assert.That(
				journal.Api.ReadingCountBetween(TheYearFrom, January.AddMilliseconds(-1)),
				Is.EqualTo(0),
				"Y un extremo de llegada de 07:59:59.999 se recorta a 07:59:59, que sigue siendo "
				+ "antes de la lectura: el recorte va HACIA ABAJO en los dos extremos.");

			Assert.That(
				journal.Api.ReadRateOfUseStanding(
					justAfterTheJanuaryReading, TheFirstHalfTo.AddMilliseconds(999)),
				Is.EqualTo("Measured"),
				"Y el mismo recorte en los cuatro extremos de las otras cinco lecturas.");
		}

		// ===== Y LOS SEIS SCRIPTS SOBREVIVEN AL JOURNAL ===================================

		// LO QUE NINGUNA PRUEBA DEL NIVEL A PUEDE TOCAR (C53): las seis respuestas VIVAS contra las
		// REPRODUCIDAS. Es la unica comparacion que ve una normalizacion del borde de la puerta -- el
		// segundo programa no la muestra, porque el journal normaliza tambien.
		//
		// Y ES LA COMPARACION QUE ESTE SLICE MAS NECESITA: sus respuestas estan HECHAS DE INSTANTES
		// -- cinco filas con su instante cada una, y dos extremos por frase -- y el journal recorta
		// el instante a segundos enteros y le borra el Kind. Una lectura que dijera el instante de
		// otra forma antes y despues de reabrir lo diria aca y en ningun otro lado.
		[Test]
		public void TheSixAnswerTheSameAfterTheJournalIsClosedAndReopened()
		{
			TheFiveReadingsInTwoUnits();

			Dictionary<string, string> live = TheSixAnswers();

			journal.CloseAndReopenFromTheJournal();

			Dictionary<string, string> replayed = TheSixAnswers();

			foreach (KeyValuePair<string, string> each in live)
				Assert.That(replayed[each.Key], Is.EqualTo(each.Value),
					"'" + each.Key + "' contesto distinto despues de reabrir el journal.");
		}

		// Y LOS SEIS ESTABAN CONTESTANDO ALGO EN ESE ESTADO DEL REGISTRO (C61). Sin este renglon la
		// comparacion de arriba pasaria con las seis contestando la ausencia de acuerdo sobre nada.
		[Test]
		public void AndTheSixAreAnsweringSomethingInThatStateOfTheRecord()
		{
			TheFiveReadingsInTwoUnits();

			foreach (KeyValuePair<string, string> each in TheSixAnswers())
				Assert.That(each.Value, Is.Not.Empty,
					"'" + each.Key + "' no contesto nada en el estado en que se toma la comparacion.");

			Assert.That(TheSixAnswers()["ReadingCountBetween"], Is.EqualTo("5"));
			Assert.That(TheSixAnswers()["CanARateOfUseBeSaid"], Is.EqualTo("True"),
				"Y la comparacion se toma en un estado donde el ritmo SI se puede decir: tomada solo "
				+ "sobre el anio suspendido, no compararia ninguna frase de ritmo.");
		}

		// NINGUNA DE LAS SEIS ESCRIBE UNA ENTRADA. El error que esto atrapa es de una sola letra --
		// PerformCommand donde iba PerformQuery -- y el estado seguiria siendo correcto, asi que
		// ninguna otra prueba se pondria roja. Y como este slice no agrega ningun comando, ninguna
		// global de raiz y ningun nombre de upgrade, este renglon es tambien la prueba de que NO HAY
		// MIGRACION que hacer y no la de que se hizo una.
		[Test]
		public void NotOneOfTheSixWritesAnEntrySoThereIsNothingToMigrate()
		{
			TheFiveReadingsInTwoUnits();

			string before = TheBytesOf(journal.Folder);

			TheSixAnswers();

			Assert.That(TheBytesOf(journal.Folder), Is.EqualTo(before),
				"El journal quedo byte a byte como estaba. Una lectura que escribe le agrega una "
				+ "entrada al journal de la persona cada vez que abre la app, para siempre, y el "
				+ "estado sigue siendo correcto -- asi que ninguna otra prueba se pondria roja.");
		}

		// Las seis, cada una con su nombre al lado, reducidas a texto para poder compararlas de a
		// pares. El registro entero va en la cadena -- las cinco columnas -- porque comparar solo la
		// cuenta dejaria pasar una columna que cambiara de contenido al reproducirse.
		private Dictionary<string, string> TheSixAnswers()
		{
			OdometerSeriesRecord record = journal.Api.ReadingsBetween(TheYearFrom, TheYearTo);

			return new Dictionary<string, string>
			{
				{
					"CanARateOfUseBeSaid",
					journal.Api.CanARateOfUseBeSaid(TheFirstHalfFrom, TheFirstHalfTo).ToString()
				},
				{
					"ReadRateOfUseStanding",
					journal.Api.ReadRateOfUseStanding(TheYearFrom, TheYearTo)
				},
				{
					"DescribeRateOfUse",
					journal.Api.DescribeRateOfUse(TheFirstHalfFrom, TheFirstHalfTo)
					+ " // " + journal.Api.DescribeRateOfUse(TheYearFrom, TheYearTo)
				},
				{
					"DescribeTwoPeriodsOfUseCompared",
					journal.Api.DescribeTwoPeriodsOfUseCompared(
						TheSecondHalfFrom, TheSecondHalfTo, TheFirstHalfFrom, TheFirstHalfTo)
				},
				{
					"ReadingCountBetween",
					journal.Api.ReadingCountBetween(TheYearFrom, TheYearTo).ToString()
				},
				{
					"ReadingsBetween",
					string.Join("|", record.ObservedAt) + " / " + string.Join("|", record.Value)
					+ " / " + string.Join("|", record.Unit) + " / " + string.Join("|", record.Source)
					+ " / " + string.Join("|", record.Says)
				},
			};
		}

		// El largo de una columna por su nombre, para que el rojo del barrido de columnas diga CUAL
		// columna se quedo sin filas en vez de solo que una lo hizo.
		private static int TheLengthOf(OdometerSeriesRecord record, string column)
		{
			if (column == "ObservedAt") return record.ObservedAt.Length;
			if (column == "Value") return record.Value.Length;
			if (column == "Unit") return record.Unit.Length;
			if (column == "Source") return record.Source.Length;
			if (column == "Says") return record.Says.Length;

			throw new ArgumentException("El registro no tiene una columna '" + column + "'.", column);
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

		// LAS CINCO, EN ORDEN DE OBSERVACION. La de mayo va en MILLAS y el guardia de insercion no la
		// compara con sus vecinas en km, asi que entra: el registro es almacenable y el ritmo la
		// refusa, que es §1.1 del brief hecho permanente del lado de la puerta.
		private void TheFiveReadingsInTwoUnits()
		{
			journal.Api.RecordReading(
				TheJanuaryReading, TheUnitOfThePeriod, January, "Telemetry");

			journal.Api.RecordReading(
				TheMarchReading, TheUnitOfThePeriod, March, "HumanEntry");

			journal.Api.RecordReading(
				TheForeignReading, TheForeignUnit, May, "CustomerDeclared");

			journal.Api.RecordReading(
				TheJulyReading, TheUnitOfThePeriod, July, "Inspection");

			journal.Api.RecordReading(
				TheSeptemberReading, TheUnitOfThePeriod, September, "Correction");
		}
	}
}
