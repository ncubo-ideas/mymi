using System;
using System.Collections.Generic;
using System.Reflection;
using DealershipCar.App;
using DealershipCar.Mcp;
using DealershipCar.Mcp.Tools;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// EL RITMO LLEGA AL ASISTENTE COMO LA FRASE DEL DOMINIO Y LAS LECTURAS COMO FILAS, Y NINGUN CAMPO
	// DE LA RESPUESTA ES EL RITMO (OD-4).
	//
	// LAS DOS MITADES VIENEN SIEMPRE Y CON NOMBRE, Y ES LO CONTRARIO DE LO QUE HACE EL TABLERO. E48
	// dice que a una PERSONA que lee una sola columna no se le imprimen las dos mitades de una
	// respuesta compuesta, porque la clausula sale dos veces; a un MODELO que lee campos con nombre
	// hay que darle las dos, porque de un solo campo no puede saber cual de las dos preguntas
	// contesto. Asi que aca canBeSaid, standing, says, readingCount y readings vienen los cinco
	// siempre, y el tablero de la consola no es el precedente.
	//
	// Y NO HAY NINGUN try/catch NI NINGUNA BANDERA DE RECHAZO. Las seis lecturas de la serie son
	// TOTALES -- medido en OD-2 y otra vez en OD-3 -- asi que copiar la forma de DEC-71, que atrapa
	// por operacion el rechazo del ancla en la cuenta atras, habria agregado un campo que un modelo
	// tiene que leer antes de una frase que siempre esta. La unica bandera es boundsUnderstood, y no
	// habla del registro: habla del texto que llego.
	//
	// LOS DOS LIMITES SON TEXTO, Y ESO ES LO QUE NO SE PODIA EQUIVOCAR. DEC-78 ensancho las claves de
	// catalogo de LA PUERTA a ocho y dejo el borde del asistente quieto a proposito; un 'DateTime
	// from' aca vuelve a tipar un limite que el censo de tools SI ve, y las dos listas de exencion
	// dejarian de estar vacias. El censo esta en
	// TheStandingReachesTheAssistantAndNoToolAsksForIt; lo que se afirma aca es la otra mitad, que
	// las dos puntas del periodo las arma ESTA tool y no la puerta.
	[TestFixture]
	public sealed class TheRateReachesTheAssistantAsASentenceAndTheRowsAsRows
	{
		private const string TheVin = "1HGCM82633A004352";

		// TRES LECTURAS, TRES FUENTES DISTINTAS, Y LA ULTIMA A LAS 08:30 A PROPOSITO (C60/L5). La hora
		// es lo que hace que la decision del limite superior se pueda medir: un dia como limite que
		// llegara solo hasta su medianoche dejaria esa lectura AFUERA, y el ritmo del periodo cambia de
		// Measured a una suspension. Con las tres a medianoche las dos formas contestarian igual y no
		// habria dato que las distinga.
		private static readonly DateTime TheFirstReading =
			new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc);
		private static readonly DateTime TheSecondReading =
			new DateTime(2026, 2, 15, 6, 0, 0, DateTimeKind.Utc);
		private static readonly DateTime TheThirdReading =
			new DateTime(2026, 3, 10, 8, 30, 0, DateTimeKind.Utc);

		private const int TheFirstOdometer = 10000;
		private const int TheSecondOdometer = 11000;
		private const int TheThirdOdometer = 12000;

		private const string TheUnitEverythingIsIn = "UnitA";

		private AScheduleOnARealJournal journal;

		[SetUp]
		public void ThreeReadingsFromThreeSources()
		{
			journal = new AScheduleOnARealJournal(TheVin, "Standard");

			journal.Api.RecordReading(
				TheFirstOdometer, TheUnitEverythingIsIn, TheFirstReading, "Telemetry");
			journal.Api.RecordReading(
				TheSecondOdometer, TheUnitEverythingIsIn, TheSecondReading, "CustomerDeclared");
			journal.Api.RecordReading(
				TheThirdOdometer, TheUnitEverythingIsIn, TheThirdReading, "HumanEntry");
		}

		[TearDown]
		public void TearDown()
		{
			journal.Dispose();
		}

		// ===== LA RESPUESTA: LA FRASE, LAS FILAS, Y NINGUN NUMERO QUE SEA EL RITMO ============

		// LAS CINCO COSAS QUE VIENEN SIEMPRE, Y CADA FRASE ES LA DE LA PUERTA LETRA POR LETRA. Si la
		// caratula recompusiera el ritmo con pedazos de las filas, o formateara la frase, esto se pone
		// rojo. Los dos instantes con los que se le pregunta a la puerta estan escritos aca: son la
		// regla de esta tool dicha por segunda vez, asi que cambiarla de un lado la pone roja.
		[Test]
		public void TheRateComesAsTheDoorsOwnSentenceAndTheReadingsComeAsRows()
		{
			RateOfUse answer = TheAssistantAsks("2026-01-01", "2026-03-10");

			DateTime start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			DateTime end = new DateTime(2026, 3, 10, 23, 59, 59, DateTimeKind.Utc);

			Assert.That(answer.HasData, Is.True);
			Assert.That(answer.Vin, Is.EqualTo(TheVin));
			Assert.That(answer.BoundsUnderstood, Is.True);

			Assert.That(answer.CanBeSaid, Is.EqualTo(journal.Api.CanARateOfUseBeSaid(start, end)));
			Assert.That(answer.CanBeSaid, Is.True);

			Assert.That(answer.Standing, Is.EqualTo(journal.Api.ReadRateOfUseStanding(start, end)));
			Assert.That(answer.Standing, Is.EqualTo("Measured"));

			Assert.That(answer.Says, Is.EqualTo(journal.Api.DescribeRateOfUse(start, end)),
				"La frase del ritmo es la de la puerta, letra por letra. Rearmarla en la caratula "
				+ "seria una frase rival que el dominio nunca dijo, y el barrido de aritmetica NO la "
				+ "puede ver: una concatenacion contra un texto no es aritmetica para el detector.");
			Assert.That(answer.Says, Does.Contain("10000 km"));
			Assert.That(answer.Says, Does.Contain("12000 km"));
			Assert.That(answer.Says, Does.Contain("por día"));

			Assert.That(answer.ReadingCount, Is.EqualTo(journal.Api.ReadingCountBetween(start, end)));
			Assert.That(answer.ReadingCount, Is.EqualTo(3));
			Assert.That(answer.Readings, Has.Count.EqualTo(answer.ReadingCount),
				"La cuenta y las filas son la misma cosa contada dos veces, y tienen que coincidir: "
				+ "de las lecturas sale la cuenta y de la cuenta no salen las lecturas.");

			// LAS CINCO COLUMNAS, FILA POR FILA, CONTRA EL REGISTRO DE LA PUERTA. Se afirman las cinco
			// por separado para que un cruce diga CUAL columna perdio sus datos (C60).
			OdometerSeriesRecord record = journal.Api.ReadingsBetween(start, end);

			for (int row = 0; row < record.Says.Length; row++)
			{
				Assert.That(answer.Readings[row].ObservedAt, Is.EqualTo(record.ObservedAt[row]));
				Assert.That(answer.Readings[row].Value, Is.EqualTo(record.Value[row]));
				Assert.That(answer.Readings[row].Unit, Is.EqualTo(record.Unit[row]));
				Assert.That(answer.Readings[row].Source, Is.EqualTo(record.Source[row]));
				Assert.That(answer.Readings[row].Says, Is.EqualTo(record.Says[row]));
			}

			// L5 EN LA FRONTERA DEL ASISTENTE: cada fila dice de que fuente vino, y son TRES distintas.
			// Con una sola fuente en el registro, una caratula que escribiera 'Telemetry' fijo pasaria.
			Assert.That(
				new[]
				{
					answer.Readings[0].Source, answer.Readings[1].Source, answer.Readings[2].Source,
				},
				Is.EqualTo(new[] { "Telemetry", "CustomerDeclared", "HumanEntry" }));

			// Y EL RITMO NO CAMBIA POR LAS FUENTES: L5 es una ley de divulgacion y no de ponderacion.
			Assert.That(answer.Says, Is.EqualTo(journal.Api.DescribeRateOfUse(start, end)));
		}

		// NINGUN CAMPO DE ESTA SUPERFICIE ES EL RITMO, NI UNA MITAD DE EL, y se comprueba de las dos
		// formas que la puerta ya usa: por CENSO de los campos numericos y por VOCABULARIO de los
		// nombres. L2 dice que los dos extremos no salen sueltos y L4 que el cambio no sale solo; en la
		// puerta eso lo fija AHalfOfAComposedAnswerNeverLeavesTheDoorAsANumber, y esta superficie es
		// otra: un campo nuevo en un DTO de la caratula no lo ve ningun censo de la puerta.
		//
		// LOS DOS NUMEROS QUE SI CRUZAN, con su razon escrita:
		//   readingCount  una cuenta de FILAS, no una medida del vehiculo;
		//   value         el odometro de una fila, un hecho RECIBIDO que el registro guarda tal como
		//                 llego. Es la primera columna de numeros de esta frontera y es una RESPUESTA,
		//                 no un parametro: no toca ninguna de las dos listas de exencion del censo de
		//                 tools, que solo miran lo que una tool RECIBE.
		[Test]
		public void NoFieldOfTheAnswerIsTheRateOrAHalfOfIt()
		{
			var numeric = new List<string>();

			foreach (Type each in new[] { typeof(RateOfUse), typeof(OdometerReadingRow) })
				foreach (PropertyInfo property in each.GetProperties())
					if (IsANumber(property.PropertyType))
						numeric.Add(each.Name + "." + property.Name);

			Assert.That(numeric, Is.EquivalentTo(new[]
			{
				"RateOfUse.ReadingCount", "OdometerReadingRow.Value",
			}), "Un numero nuevo cruza hacia el asistente. Los dos que hay son una cuenta de filas y "
				+ "un hecho recibido; la distancia recorrida, los dias transcurridos, la distancia por "
				+ "dia y el cambio entre dos periodos son MITADES de una respuesta compuesta y salen "
				+ "solo adentro de la frase que el dominio compuso.");

			// Y POR VOCABULARIO, porque quien agregue el campo elige el tipo tanto como el nombre: un
			// ritmo dicho como TEXTO pasaria el censo de arriba entero.
			foreach (Type each in new[] { typeof(RateOfUse), typeof(OdometerReadingRow) })
				foreach (PropertyInfo property in each.GetProperties())
					foreach (string banned in new[]
					{
						"Rate", "PerDay", "Distance", "Travelled", "Elapsed", "Change", "Percent",
						"Delta", "Average", "Trend", "Slope", "Projection", "Forecast",
					})
						Assert.That(
							property.Name, Does.Not.Contain(banned),
							"'" + each.Name + "." + property.Name + "' nombra una mitad suelta.");
		}

		// ===== LOS DOS LIMITES ================================================================

		// UN DIA COMO LIMITE SUPERIOR LLEGA HASTA EL FINAL DE ESE DIA, Y EL DATO ES EL QUE HACE QUE LA
		// OTRA ELECCION SE VEA. La puerta toma INSTANTES y esta tool recibe DIAS, asi que alguien tiene
		// que decidir en que instante del dia cae cada punta. Los dos extremos son inclusive, asi que
		// el segundo es el ULTIMO SEGUNDO ENTERO del dia -- entero porque el journal guarda segundos
		// enteros.
		//
		// MEDIDO: con el periodo 2026-02-01 .. 2026-03-10, la tercera lectura es de ese mismo 10 a las
		// 08:30. Hasta el final del dia entran DOS lecturas y el ritmo se PUEDE decir; hasta la
		// medianoche entra UNA y el ritmo queda suspendido por lectura unica. Las dos elecciones
		// contestan cosas distintas, y esta prueba se pone roja con la equivocada.
		[Test]
		public void ADayAsTheUpperBoundReachesTheEndOfThatDayAndNotOnlyItsMidnight()
		{
			RateOfUse answer = TheAssistantAsks("2026-02-01", "2026-03-10");

			Assert.That(answer.To, Is.EqualTo("2026-03-10 23:59:59"),
				"El limite superior es el ultimo segundo entero del dia que se nombro.");
			Assert.That(answer.From, Is.EqualTo("2026-02-01 00:00:00"),
				"Y el inferior el primero, que es la otra mitad de 'los dos inclusive'.");

			Assert.That(answer.ReadingCount, Is.EqualTo(2));
			Assert.That(answer.Standing, Is.EqualTo("Measured"));
			Assert.That(answer.CanBeSaid, Is.True);
			Assert.That(answer.Says, Does.Contain("12000 km"),
				"La lectura de las 08:30 de ese mismo dia esta ADENTRO de la respuesta.");

			// EL CONTROL QUE HACE QUE ESTO SEA UNA MEDICION Y NO UNA AFIRMACION: la otra eleccion
			// contesta otra cosa, y se le pregunta a la puerta directamente para verlo.
			DateTime from = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
			DateTime midnight = new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc);

			Assert.That(journal.Api.ReadingCountBetween(from, midnight), Is.EqualTo(1));
			Assert.That(journal.Api.ReadRateOfUseStanding(from, midnight),
				Is.EqualTo("SuspendedByASingleReading"),
				"Hasta la medianoche del dia nombrado el ritmo NO se puede decir. Si esta linea y la "
				+ "de arriba contestaran lo mismo, la eleccion del extremo no estaria medida por "
				+ "nada.");
		}

		// CON LOS DOS LIMITES EN BLANCO SE CONTESTA SOBRE TODO EL REGISTRO, Y LA RESPUESTA NOMBRA LAS
		// DOS PUNTAS QUE USO. 'Todo' son los dos extremos que un calendario puede nombrar, porque el
		// registro no tiene extremos propios que preguntar: ninguna de las seis lecturas contesta cual
		// fue la primera lectura ni la ultima, y agregar una seria una operacion nueva en la puerta que
		// esta tarea no agrega.
		[Test]
		public void WithBothBoundsBlankTheAnswerCoversTheWholeRecordAndNamesTheBoundsItUsed()
		{
			RateOfUse whole = TheAssistantAsks("", "");

			Assert.That(whole.WholeRecord, Is.True);
			Assert.That(whole.BoundsUnderstood, Is.True);
			Assert.That(whole.From, Is.EqualTo("0001-01-01 00:00:00"));
			Assert.That(whole.To, Is.EqualTo("9999-12-31 23:59:59"),
				"El ultimo SEGUNDO ENTERO, no el ultimo tick: la puerta trunca cada instante al "
				+ "segundo que el journal guarda, asi que un limite con fraccion volveria distinto "
				+ "del que se dijo.");
			Assert.That(whole.ReadingCount, Is.EqualTo(3));
			Assert.That(whole.Standing, Is.EqualTo("Measured"));

			// Y 'EN BLANCO' ES EXACTAMENTE EL DIA MAS ANCHO QUE SE PUEDE ESCRIBIR, no otro centinela.
			// Sin esta linea, en blanco podria resolver a cualquier cosa que diera las tres lecturas.
			RateOfUse written = TheAssistantAsks("0001-01-01", "9999-12-31");

			Assert.That(written.From, Is.EqualTo(whole.From));
			Assert.That(written.To, Is.EqualTo(whole.To));
			Assert.That(written.Says, Is.EqualTo(whole.Says));
			Assert.That(written.ReadingCount, Is.EqualTo(whole.ReadingCount));
			Assert.That(written.WholeRecord, Is.False,
				"Y lo unico que cambia es wholeRecord: escribir los dos extremos es una pregunta con "
				+ "periodo elegido, aunque el periodo sea todo.");
		}

		// UN SOLO LIMITE EN BLANCO DEJA ESE LADO ABIERTO, decidido por extremo y no por el par.
		[Test]
		public void OneBlankBoundLeavesThatSideOpenAndTheOtherStillNarrows()
		{
			RateOfUse openStart = TheAssistantAsks("", "2026-02-01");

			Assert.That(openStart.From, Is.EqualTo("0001-01-01 00:00:00"));
			Assert.That(openStart.To, Is.EqualTo("2026-02-01 23:59:59"));
			Assert.That(openStart.WholeRecord, Is.False);
			Assert.That(openStart.ReadingCount, Is.EqualTo(1));
			Assert.That(openStart.Standing, Is.EqualTo("SuspendedByASingleReading"));

			RateOfUse openEnd = TheAssistantAsks("2026-02-01", "");

			Assert.That(openEnd.From, Is.EqualTo("2026-02-01 00:00:00"));
			Assert.That(openEnd.To, Is.EqualTo("9999-12-31 23:59:59"));
			Assert.That(openEnd.WholeRecord, Is.False);
			Assert.That(openEnd.ReadingCount, Is.EqualTo(2));
		}

		// UN PERIODO QUE TERMINA ANTES DE EMPEZAR SE CONTESTA Y NO SE RECHAZA, y esta es la mitad de
		// OD-4 que se podia perder copiando el analizador equivocado. La puerta tiene DOS
		// normalizadores de periodo que se portan distinto ante la misma equivocacion: el de DIAS
		// RECHAZA y el de INSTANTES CONTESTA (DEC-79), porque un dia llega como texto y puede no decir
		// ningun dia, mientras que dos instantes ya dicen un periodo cualquiera sea su orden. Esta tool
		// convierte dias en instantes, asi que estaba a un paso de heredar el rechazo -- y heredarlo
		// habria vuelto PARCIALES en esta frontera seis lecturas que son totales.
		[Test]
		public void APeriodThatEndsBeforeItBeginsIsAnsweredWithBothBoundsNamedAndIsNeverRefused()
		{
			RateOfUse inverted = TheAssistantAsks("2026-03-10", "2026-01-01");

			Assert.That(inverted.BoundsUnderstood, Is.True,
				"Los dos limites dicen un dia cada uno. Que el segundo sea anterior al primero es "
				+ "otra cosa, y no es un error de forma.");
			Assert.That(inverted.CanBeSaid, Is.False);
			Assert.That(inverted.Standing, Is.EqualTo("SuspendedByAnAbsentReading"));
			Assert.That(inverted.ReadingCount, Is.EqualTo(0));
			Assert.That(inverted.Readings, Is.Empty);

			// LA RESPUESTA NOMBRA LOS DOS LIMITES EN EL ORDEN EN QUE LLEGARON, que es lo que hace que
			// un limite inventado produzca una respuesta visiblemente acotada y nunca un hecho falso.
			Assert.That(inverted.From, Is.EqualTo("2026-03-10 00:00:00"));
			Assert.That(inverted.To, Is.EqualTo("2026-01-01 23:59:59"));
			Assert.That(inverted.Says, Does.Contain(inverted.From));
			Assert.That(inverted.Says, Does.Contain(inverted.To),
				"Los dos limites de la respuesta salen letra por letra adentro de la frase del "
				+ "dominio. Decirlos con otra forma dejaria al modelo con dos versiones del mismo "
				+ "periodo.");

			// EL DATO QUE HACE QUE LA OTRA ELECCION SE VEA: ordenar los dos limites en silencio
			// contestaria el ritmo de las tres lecturas, que es una respuesta a una pregunta que nadie
			// hizo.
			RateOfUse ordered = TheAssistantAsks("2026-01-01", "2026-03-10");

			Assert.That(ordered.Standing, Is.EqualTo("Measured"));
			Assert.That(ordered.ReadingCount, Is.EqualTo(3));

			// Y EL CONTROL DE QUE NO SE REUSO EL ANALIZADOR DE DIAS: el de la puerta SI rechaza la
			// misma inversion, con excepcion. Si dejara de hacerlo, esta prueba estaria comparando
			// contra nada.
			Assert.Throws<ArgumentException>(
				() => journal.Api.StaysBetween("2026-03-10", "2026-01-01"),
				"El normalizador de periodos dichos en DIAS rechaza un periodo invertido. Es el que "
				+ "esta tool NO usa, y por eso su rechazo no llega al asistente.");
		}

		// UN LIMITE QUE NO DICE UN DIA SE CONTESTA CON PALABRAS Y NUNCA REVIENTA, y lo que se contesta
		// NO es un periodo vacio: es que no se eligio ningun periodo. Las dos cosas se leen igual en
		// una lista vacia, y confundirlas es reportar 'este carro no tiene lecturas' cuando lo que
		// paso es que el texto del limite no se entendio.
		[Test]
		public void ABoundThatDoesNotSayADayIsAnsweredInWordsAndNotThrown()
		{
			RateOfUse broken = TheAssistantAsks("el martes pasado", "2026-03-10");

			Assert.That(broken.HasData, Is.True);
			Assert.That(broken.BoundsUnderstood, Is.False);
			Assert.That(broken.Vin, Is.EqualTo(TheVin));

			Assert.That(broken.Standing, Is.Empty);
			Assert.That(broken.Says, Is.Empty);
			Assert.That(broken.CanBeSaid, Is.False);
			Assert.That(broken.ReadingCount, Is.Zero);
			Assert.That(broken.Readings, Is.Empty);

			Assert.That(broken.From, Is.EqualTo("el martes pasado"),
				"El texto que no se entendio vuelve tal cual, para que se vea cual de los dos fue.");
			Assert.That(broken.To, Is.EqualTo("2026-03-10"));

			Assert.That(broken.Detail, Does.Contain("no dice un día de calendario"));
			Assert.That(broken.Detail, Does.Contain("no se eligió ningún periodo"));

			// LA DIFERENCIA QUE ESTE CAMPO EXISTE PARA MARCAR: un periodo bien dicho y sin lecturas
			// contesta con una frase del dominio y con su situacion; un limite que no se entendio no
			// contesta ninguna de las dos.
			RateOfUse empty = TheAssistantAsks("2025-01-01", "2025-06-01");

			Assert.That(empty.BoundsUnderstood, Is.True,
				"Y el control de que la bandera no es un false constante.");
			Assert.That(empty.Standing, Is.EqualTo("SuspendedByAnAbsentReading"));
			Assert.That(empty.Says, Is.Not.Empty);
			Assert.That(empty.ReadingCount, Is.Zero);
		}

		// LA FORMA ES EXACTA, y las tres que se prueban son las tres que un modelo escribe cuando se
		// equivoca: el mes sin cero, el orden de otro pais, y un instante completo donde va un dia.
		[Test]
		public void TheOnlyShapeAcceptedIsTheOneTheDescriptionNames()
		{
			foreach (string each in new[] { "2026-3-10", "10/03/2026", "2026-03-10T08:00:00Z", "hoy" })
				Assert.That(TheAssistantAsks(each, "").BoundsUnderstood, Is.False,
					"'" + each + "' no dice un dia en la unica forma que esta tool acepta.");

			Assert.That(TheAssistantAsks("2026-03-10", "").BoundsUnderstood, Is.True);
			Assert.That(TheAssistantAsks("  2026-03-10  ", "").BoundsUnderstood, Is.True,
				"Los espacios de los extremos si se recortan, que es lo que hace que quien pregunta "
				+ "y quien contesta nombren el mismo dia.");
		}

		// ===== LAS CINCO SITUACIONES ==========================================================

		// LAS CINCO SITUACIONES DEL RITMO LLEGAN AL ASISTENTE Y SON EXACTAMENTE LAS DEL DOMINIO. El
		// tablero de la consola alcanza DOS de las cinco -- las otras tres necesitan montar un registro
		// -- asi que esta es la primera vez que las cinco se ven en una frontera.
		//
		// SE AFIRMA COMO IGUALDAD DE CONJUNTOS contra el conjunto cerrado del dominio leido por
		// reflexion, no como 'esta entre ellas': una sexta situacion que el dominio declarara y esta
		// frontera nunca alcanzara se pondria roja aca y en ningun otro sitio.
		//
		// Y RateStanding NO SE ESCRIBE DOS VECES. TheClosedSetAtTheAssistantsEdgeIsTheDomainsOwn fija
		// los conjuntos que la caratula copia -- las clases de operacion, los fluidos, las posiciones
		// de llanta, las unidades -- porque los OFRECE o los RECORRE. Este no se copia: la caratula no
		// nombra ni un miembro, y la unica bifurcacion que hace es sobre canBeSaid, que es un bool. Asi
		// que el conjunto se escribe una sola vez, en el dominio, y esta prueba es la que lo comprueba.
		[Test]
		public void TheFiveStandingsAllReachTheAssistantAndTheyAreExactlyTheDomainsOwn()
		{
			var reached = new List<string>
			{
				TheAssistantAsks("", "").Standing,
				TheAssistantAsks("2026-03-01", "2026-03-31").Standing,
				TheAssistantAsks("2025-01-01", "2025-06-01").Standing,
			};

			using (AScheduleOnARealJournal mixed = AVehicleWhoseReadingsAreInTwoUnits())
				reached.Add(TheAssistantAsksOver(mixed).Standing);

			using (AScheduleOnARealJournal sameSecond = AVehicleWithTwoReadingsInOneSecond())
				reached.Add(TheAssistantAsksOver(sameSecond).Standing);

			Assert.That(reached, Is.EquivalentTo(TheFiveStandingsTheDomainDeclares()),
				"Las cinco situaciones del dominio tienen que llegar todas a esta frontera, y ninguna "
				+ "otra. Una que nunca llegue es una respuesta que la persona no puede recibir; una "
				+ "que llegue y el dominio no declare salio de la caratula.");

			Assert.That(reached, Is.EqualTo(new[]
			{
				"Measured", "SuspendedByASingleReading", "SuspendedByAnAbsentReading",
				"SuspendedByAUnitMismatch", "SuspendedByAZeroLengthSpan",
			}), "Y cada registro contesta LA SUYA: sin esta linea, cinco respuestas cualesquiera que "
				+ "cubrieran el conjunto pasarian la de arriba.");
		}

		// UNA SUSPENSION ES UNA RESPUESTA, Y LAS CINCO COSAS SIGUEN VINIENDO. La mitad que un modelo se
		// come si no se la dicen: que 'no se pudo medir' no quiere decir que falten datos, y que las
		// filas contestan igual. G5 del brief visto desde esta frontera -- una lectura en otra unidad
		// suspende el ritmo del periodo y las filas SIGUEN estando.
		[Test]
		public void ASuspendedRateStillCarriesItsSentenceItsStandingAndItsRows()
		{
			using (AScheduleOnARealJournal mixed = AVehicleWhoseReadingsAreInTwoUnits())
			{
				RateOfUse answer = TheAssistantAsksOver(mixed);

				Assert.That(answer.CanBeSaid, Is.False);
				Assert.That(answer.Standing, Is.EqualTo("SuspendedByAUnitMismatch"));
				Assert.That(answer.Says, Is.Not.Empty);
				Assert.That(answer.Says, Does.Contain("km"));
				Assert.That(answer.Says, Does.Contain("mi"),
					"La frase nombra LAS DOS unidades que chocaron, que es lo unico que deja a la "
					+ "persona saber cual lectura lo explica.");
				Assert.That(answer.Says, Does.Contain("nunca se convierten"));

				Assert.That(answer.ReadingCount, Is.EqualTo(3));
				Assert.That(answer.Readings, Has.Count.EqualTo(3),
					"Las filas contestan igual con el ritmo suspendido. Una caratula que escondiera "
					+ "la tabla cuando la frase dice 'suspendido' dejaria a la persona sin lo unico "
					+ "que le dice cual lectura fue.");
				Assert.That(answer.Readings[1].Unit, Is.EqualTo("mi"),
					"Y la fila que la frase nombra se ve en la columna.");

				// Y EL DETALLE LO DICE, que es la mitad que E48 pone donde un modelo actua: no es
				// 'pidale el dato a alguien', es 'no conteste cero y no estime'.
				Assert.That(answer.Detail, Does.Contain("NO se pudo decir"));
				Assert.That(answer.Detail, Does.Contain("NO conteste cero"));
			}
		}

		// Y CUANDO EL RITMO SI SE PUDO DECIR, ESA MITAD DEL DETALLE NO SALE. Sin este control, la
		// frase de arriba podria ser una constante que sale siempre y no diria nada del registro.
		[Test]
		public void AndWithAMeasuredRateTheDetailSaysNothingAboutASuspension()
		{
			RateOfUse answer = TheAssistantAsks("", "");

			Assert.That(answer.CanBeSaid, Is.True);
			Assert.That(answer.Detail, Does.Not.Contain("NO se pudo decir"));
			Assert.That(answer.Detail, Does.Not.Contain("NO conteste cero"));

			// Lo que si sale siempre: que la frase ya trae el ritmo y que las filas no se dividen.
			Assert.That(answer.Detail, Does.Contain("dividir una entre otra"));
			Assert.That(answer.Detail, Does.Contain("nadie midió"));
		}

		// ===== EL APAREJO =====================================================================

		private RateOfUse TheAssistantAsks(string from, string to)
		{
			return new OdometerSeriesTools(new JournalReader(journal.Folder, ""))
				.HowMuchMyCarIsDriven(from, to);
		}

		private static RateOfUse TheAssistantAsksOver(AScheduleOnARealJournal other)
		{
			return new OdometerSeriesTools(new JournalReader(other.Folder, ""))
				.HowMuchMyCarIsDriven();
		}

		// UNA LECTURA EN OTRA UNIDAD ADENTRO DEL PERIODO. La guarda de insercion no la rechaza -- solo
		// compara contra vecinos de la MISMA unidad -- asi que el registro la acepta y el ritmo es lo
		// primero de este modulo que la puede ver.
		private static AScheduleOnARealJournal AVehicleWhoseReadingsAreInTwoUnits()
		{
			var other = new AScheduleOnARealJournal("JTDKN3DU0A0123456", "Standard");

			other.Api.RecordReading(
				10000, "UnitA", new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc), "Telemetry");
			other.Api.RecordReading(
				7000, "UnitB", new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc), "Inspection");
			other.Api.RecordReading(
				12000, "UnitA", new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc), "HumanEntry");

			return other;
		}

		// DOS LECTURAS EN EL MISMO SEGUNDO ENTERO. El journal guarda segundos enteros, asi que un
		// periodo puede tener dos extremos y cero tiempo transcurrido por el que dividir.
		private static AScheduleOnARealJournal AVehicleWithTwoReadingsInOneSecond()
		{
			var other = new AScheduleOnARealJournal("1FTFW1ET5DFA00001", "Standard");

			DateTime theSameSecond = new DateTime(2026, 5, 5, 12, 0, 0, DateTimeKind.Utc);

			other.Api.RecordReading(10000, "UnitA", theSameSecond, "Telemetry");
			other.Api.RecordReading(10005, "UnitA", theSameSecond, "Correction");

			return other;
		}

		// Los cinco nombres, sacados del propio conjunto cerrado del dominio.
		private static List<string> TheFiveStandingsTheDomainDeclares()
		{
			var names = new List<string>();

			foreach (FieldInfo each in typeof(RateStanding).GetFields(
				BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
				if (each.FieldType == typeof(RateStanding))
					names.Add(((RateStanding)each.GetValue(null)).Name);

			Assert.That(names, Has.Count.EqualTo(5),
				"Si no son cinco, este barrido dejo de enumerar el conjunto y no defiende nada.");

			return names;
		}

		private static bool IsANumber(Type type)
		{
			return type == typeof(int) || type == typeof(long) || type == typeof(decimal)
				|| type == typeof(double) || type == typeof(float) || type == typeof(short);
		}
	}
}
