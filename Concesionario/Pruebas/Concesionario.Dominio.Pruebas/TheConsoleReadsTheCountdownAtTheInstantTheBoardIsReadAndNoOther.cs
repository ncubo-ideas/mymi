using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// EL TABLERO DE 'historia' PREGUNTA LA PANTALLA DE MANTENIMIENTO A UN INSTANTE ESCRITO Y A NINGUN
	// OTRO. La consola GUIONA una secuencia recibida, asi que no lee reloj (C27) -- eso ya lo afirma
	// TheConsoleStagesFromLiteralsAndReadsNoClock, y esta fixture NO lo repite. Lo que agrega es la otra
	// mitad, la que ese barrido no puede ver: CUAL de los instantes ESCRITOS que la consola tiene usa
	// cada lectura del tablero.
	//
	// LA PANTALLA DE MANTENIMIENTO USA UNO SOLO, y es TheInstantTheBoardIsRead: sus once lecturas
	// preguntan POR UN INSTANTE. LOS DOS BLOQUES DEL ODOMETRO USAN TRES, porque sus seis lecturas
	// preguntan POR UN PERIODO, y un periodo tiene dos extremos: el instante del tablero es uno de
	// ellos y los otros dos son momentos que la propia historia ya guiona -- el dia de la entrega y
	// la primera evaluacion. Son dos leyes distintas y estan afirmadas por separado.
	//
	// POR QUE TIENE QUE SER UNA PRUEBA SOBRE EL TEXTO Y NO SOBRE UNA CORRIDA. Dos razones, y la segunda
	// es la que decide:
	//
	//   1. Pruebas/ no referencia Concesionario.Consola -- solo su FUENTE viaja al output como dato, en
	//      el grupo <None> de CodigoComoDato\Consola. No hay forma de invocar un bloque de impresion.
	//   2. TheInstantTheBoardIsRead y Story.SixMonthsLater VALEN LO MISMO: los dos son el 2026-09-01.
	//      Una prueba que corriera la consola y comparara respuestas no podria distinguirlos NUNCA. La
	//      unica superficie donde se ven distintos es el texto de la llamada, que es donde se mira.
	//
	// Y POR ESO LAS LLAMADAS NOMBRAN LA CONSTANTE ENTERA en cada sitio, sin un local intermedio: un
	// 'DateTime at = TheInstantTheBoardIsRead;' seria un alias, y un alias es exactamente la forma que
	// la cabecera de TheConsoleStagesFromLiteralsAndReadsNoClock declara como su punto ciego. Se paga
	// con once lineas largas y se cobra en que el instrumento ve lo que tiene que ver.
	//
	// LO QUE ESTA FIXTURE NO VE, dicho y no disimulado: una clase pedida a traves de una variable que no
	// sea la del recorrido. Hoy no hay ninguna otra en los cinco bloques, y por eso lo que se afirma es
	// que ninguna lectura recibe una clase ENTRE COMILLAS -- que es la unica forma en que la consola
	// podria nombrar una por su cuenta, porque no referencia el dominio y no tiene sus enums.
	[TestFixture]
	public sealed class TheConsoleReadsTheCountdownAtTheInstantTheBoardIsReadAndNoOther
	{
		private const string TheWrittenInstant = "TheInstantTheBoardIsRead";

		private const string TheBoardsFile = "Program.cs";

		// LOS CINCO BLOQUES. El piso de abajo cuenta archivos; esto nombra los miembros: un barrido que
		// leyo Program.cs y no encontro ninguno esta verde por no haber nada que juzgar.
		//
		// ERAN TRES Y SON CINCO DESDE OD-3, y el nombre lleva la cuenta a proposito: agregar un bloque
		// al tablero obliga a renombrar esto y los dos metodos de abajo, que es algo que se ve en un
		// diff. Los dos nuevos son los del odometro -- el ritmo del tramo de la historia y las dos
		// mitades comparadas -- y entran aca por lo que midio la fila 10 de la tabla de falsificacion
		// de MS-5: borrada la LLAMADA a un bloque, el bloque queda declarado y nunca invocado, C# no
		// avisa nada y todas las demas afirmaciones de esta fixture miran el CUERPO.
		private static readonly string[] TheFiveBlocks =
		{
			"TheCountdownOfEveryKind", "TheHistoryOfEveryKind", "ThePublishedServicePrices",
			"TheRateOfUseOverTheStorysSpan", "TheTwoHalvesOfTheStoryCompared",
		};

		// LAS SEIS LECTURAS DE LA SERIE, todas con periodo y ninguna guardada: las seis son TOTALES y
		// no hay pre-chequeo que escribir. CanARateOfUseBeSaid es la mitad de MEDIBILIDAD de un par
		// compuesto y no una guarda delante de un rechazo, asi que no entra a TheGuardOfEachRead.
		private static readonly string[] TheSixReadsOfTheSeries =
		{
			"CanARateOfUseBeSaid",
			"ReadRateOfUseStanding",
			"DescribeRateOfUse",
			"DescribeTwoPeriodsOfUseCompared",
			"ReadingsBetween",
			"ReadingCountBetween",
		};

		// LOS TRES INSTANTES ESCRITOS CON QUE LOS BLOQUES DEL ODOMETRO PUEDEN PREGUNTAR, y son los
		// momentos que la propia historia ya guiona: el dia de la entrega, la primera evaluacion y el
		// instante del tablero. Ninguno se deriva de otro.
		//
		// Story.SixMonthsLater NO ESTA, y ese es el filo: vale lo mismo que TheInstantTheBoardIsRead
		// -- los dos son el 2026-09-01 -- asi que ninguna corrida podria notar el cambio. El tablero
		// pregunta con el instante DEL TABLERO y lo dice con ese nombre.
		private static readonly string[] TheWrittenMomentsTheUseBlocksMayAskWith =
		{
			"TheInstantTheBoardIsRead", "Story.Delivery", "Story.TheFirstEvaluation",
		};

		// LA CONSTANTE CON QUE EL TABLERO PIDE LA REDACCION. No es un limite de periodo y no se
		// censa como uno: dice en que idioma se quiere la frase, no que pedazo del registro.
		private const string TheLanguageTheBoardAsksIn = "TheVoiceOfTheDealership";

		// EL ORDEN DE LOS CUATRO LIMITES DE LA COMPARACION, y es contrato: el cambio se dice del
		// PRIMER periodo respecto al SEGUNDO, cosa que OD-2 fijo por falsificacion en la puerta. La
		// mitad TARDIA va primera -- la del semestre que termina en el instante del tablero -- que es
		// la misma direccion que va a querer la ventana de la pantalla.
		private const string TheComparisonsFourBoundsInOrder =
			"Story.TheFirstEvaluation, TheInstantTheBoardIsRead, Story.Delivery, "
			+ "Story.TheFirstEvaluation";

		// LAS CUATRO FORMAS DE DERIVAR UNA FECHA DE OTRA. Un instante derivado sigue siendo
		// reproducible y deja de decir cual es, que es el argumento que ya hace
		// EveryInstantOfTheStagedStaysIsWrittenDownAsALiteral sobre el guion de las estadias.
		private static readonly string[] TheWaysOfDerivingAnInstant =
		{
			"AddDays", "AddMonths", "AddYears", "AddHours",
		};

		// LAS ONCE LECTURAS DEL TABLERO QUE TOMAN UN INSTANTE: las DIEZ de la parte A, que lo toman
		// todas, y la de las FILAS de precios, que es la unica de la parte C que lo toma.
		private static readonly string[] TheReadsThatTakeAnInstant =
		{
			"CanDistanceRemainingBeSaid",
			"ReadDistanceRemaining",
			"ReadDistanceRemainingUnit",
			"ReadDistanceRemainderStanding",
			"DescribeDistanceRemaining",
			"CanDaysRemainingBeSaid",
			"ReadDaysRemaining",
			"ReadTimeRemainderStanding",
			"DescribeDaysRemaining",
			"DescribeCountdown",
			"PublishedServicePricesAsOf",
		};

		// LAS SIETE QUE NO TOMAN NINGUNO: las cinco de la parte B, que no toman instante porque cada
		// fila trae el suyo, y las dos de la parte C que preguntan por la EXISTENCIA y por la CUENTA.
		// La asimetria es la afirmacion (MS-4 §15 item 4) y no se ordena.
		private static readonly string[] TheReadsThatTakeNone =
		{
			"HasAnyCompletion",
			"CompletionCount",
			"DescribeCompletions",
			"CompletionsOf",
			"CompletionsOfEveryKind",
			"HasAnyPublishedServicePrice",
			"PublishedServicePriceCount",
		};

		// LA DECIMONOVENA, Y EL TABLERO NO LA LLAMA A PROPOSITO. DescribePublishedServicePrice es la
		// hermana E48 para quien no tenga ningun instante que aportar, y la consola SI tiene uno: el
		// suyo esta escrito. Llamarla obligaria a la caratula a nombrar un HITO, y un hito es un numero
		// que nadie le publico -- la forma exacta que P-12 retira del borde de la IA. La celda Milestone
		// de la fila no sirve para eso: es el hito DICHO y deliberadamente no direccionable.
		private const string TheOneTheBoardDoesNotCall = "DescribePublishedServicePrice";

		// LA REGION QUE ESTA FIXTURE JUZGA PARA LAS ETIQUETAS: los cinco bloques, que van seguidos y
		// terminan donde empiezan las mediciones contra el disco. Se acota A PROPOSITO y no por
		// comodidad: 'taller' imprime su propia columna Says y la pinta en una linea de continuacion sin
		// etiqueta, que es otra rebanada y otra decision (DEC-52/DEC-61). Un barrido global tendria que
		// llevarla en una lista de excepciones, y una lista de excepciones sobre codigo ajeno crece.
		//
		// Y LA REGION SE DEFIENDE SOLA: si alguien saca uno de los printers de aca, la columna que
		// ese printer pinta deja de aparecer y el 'right' de abajo queda vacio, que es rojo.
		private const string TheFirstLineOfTheBlocks = "private static void TheCountdownOfEveryKind(";

		private const string TheFirstLineAfterTheBlocks = "private static void Measurements(";

		// CADA COLUMNA IMPRESA VA BAJO SU PROPIA ETIQUETA, y esta es la tabla. Se afirma en UNA sola
		// direccion -- de la columna a la etiqueta -- y no en la otra: palabras como 'revision' u
		// 'operacion' aparecen por todo Program.cs, en la salida de 'ficha' y de 'historia', asi que ir
		// de la etiqueta a la columna daria rojos que no dicen nada. En esta direccion alcanza: cruzar
		// dos columnas deja una linea con la columna equivocada bajo una etiqueta que no es la suya.
		private static readonly object[] TheLabelOfEachColumn =
		{
			new object[] { ".Kind[", "clase" },
			new object[] { ".CompletedAt[", "hecha el" },
			new object[] { ".Reading[", "con el odometro en" },
			new object[] { ".Says[", "dice la fila" },
			new object[] { ".Operation[", "operacion" },
			new object[] { ".Milestone[", "hito" },
			new object[] { ".Revision[", "revision" },
			// LAS CUATRO COLUMNAS QUE OD-3 AGREGA AL TABLERO. La quinta del registro de la serie es
			// Says, que ya esta arriba con la misma etiqueta que las otras tres filas del tablero.
			// Value es la PRIMERA columna numerica del modulo y entra igual que las de texto: lo que
			// esta afirmacion ve es la etiqueta, no el tipo.
			new object[] { ".ObservedAt[", "observada el" },
			new object[] { ".Value[", "el odometro marcaba" },
			new object[] { ".Unit[", "en unidad" },
			new object[] { ".Source[", "de fuente" },
		};

		[Test]
		public void TheFiveBlocksArrivedAndTheSweepIsReadingTheFileTheyAreIn()
		{
			var named = new List<string>();

			foreach (string each in TheCodeOfTheShellsAsData.EveryFileOf(
				TheCodeOfTheShellsAsData.Consola))
				named.Add(TheCodeOfTheShellsAsData.InsideItsRoot(
					TheCodeOfTheShellsAsData.Consola, each));

			Assert.That(named, Does.Contain(TheBoardsFile),
				"El fuente del tablero no llego al barrido, asi que todo lo de abajo es una lista "
				+ "vacia que cumple cualquier cosa.");

			string live = TheLiveBoard();

			foreach (string block in TheFiveBlocks)
				Assert.That(live, Does.Contain(block),
					"El bloque '" + block + "' no esta en el tablero. Las afirmaciones de esta fixture "
					+ "recorren LLAMADAS: sin los cinco bloques no hay ninguna, y no haber ninguna es "
					+ "verde.");
		}

		// Y LOS CINCO SE LLAMAN, UNA VEZ CADA UNO, DESDE LA HISTORIA DE SEIS MESES. Esta prueba tambien
		// existe porque un parche no puso nada rojo: borrada la LLAMADA al bloque de precios -- el
		// bloque entero declarado y nunca invocado --, la suite quedo verde y el tablero imprimia dos
		// tercios de si mismo. Un metodo privado que nadie llama no lo avisa el compilador de C#, y
		// ninguna de las otras afirmaciones de esta fixture mira las LLAMADAS: todas miran el cuerpo.
		//
		// UNA VEZ CADA UNO Y DESDE ADENTRO DE 'historia', que es lo que PLAN-BuildOrder.md §8 dice --
		// cinco bloques y UN llamador. Llamarlos tambien desde 'estado' o desde 'refresco' no esta
		// prohibido, esta obligado a decirse: ahi los instantes son otros y P-13 reserva el guionado a
		// las rebanadas que reciben un hecho nuevo.
		private const string TheCallerOfTheBlocks = "private static int TheSixMonthStory(";

		[Test]
		public void TheFiveBlocksAreCalledOnceEachFromTheSixMonthStoryAndFromNowhereElse()
		{
			string[] live = TheLiveBoard().Split('\n');

			int from = -1;
			int to = -1;

			for (int line = 0; line < live.Length; line++)
			{
				if (live[line].Contains(TheCallerOfTheBlocks)) from = line;
				else if (from >= 0 && to < 0 && live[line].StartsWith("\t\tprivate static ")) to = line;
			}

			Assert.That(from, Is.GreaterThanOrEqualTo(0),
				"No esta '" + TheCallerOfTheBlocks + "', que es el subcomando que arma el tablero.");
			Assert.That(to, Is.GreaterThan(from), "No se encontro el final de ese subcomando.");

			foreach (string block in TheFiveBlocks)
			{
				var calls = new List<int>();
				var call = new Regex(@"^\s*" + block + @"\s*\(");

				for (int line = 0; line < live.Length; line++)
					if (call.IsMatch(live[line])) calls.Add(line);

				Assert.That(calls, Has.Count.EqualTo(1),
					"'" + block + "' se llama " + calls.Count + " veces. Cero quiere decir que el "
					+ "bloque esta escrito y el tablero no lo imprime, y eso no lo avisa el compilador "
					+ "ni ninguna otra afirmacion de aca: todas miran el CUERPO del bloque. Mas de una "
					+ "quiere decir que se imprime en otro momento, con otro instante, y eso se dice.");

				Assert.That(calls[0], Is.InRange(from, to),
					"'" + block + "' no se llama desde la historia de seis meses. El instante del "
					+ "tablero es el de ESA corrida: 'estado' y 'refresco' abren otro journal y "
					+ "preguntan en otro momento.");
			}
		}

		// LA LEY. Las once se preguntan al instante escrito del tablero y a ningun otro. Story.Delivery,
		// Story.TheFirstEvaluation, Story.SixMonthsLater, TheInstantTheStaysAreRead y los cuatro
		// momentos de 'garantia' estan todos a mano en el mismo archivo: cualquiera de ellos en
		// cualquiera de estas llamadas pone esto rojo.
		[Test]
		public void EveryReadOfTheBoardThatTakesAnInstantIsAskedAtTheInstantTheBoardIsRead()
		{
			string live = TheLiveBoard();

			foreach (string read in TheReadsThatTakeAnInstant)
			{
				List<string> calls = TheArgumentsOfEveryCallTo(live, read);

				Assert.That(calls, Is.Not.Empty,
					"El tablero no llama a '" + read + "'. Las once lecturas con instante de la "
					+ "pantalla de mantenimiento se imprimen, y una que no se llama es una que la "
					+ "consola nunca ejercito -- que es la unica defensa contra un script del DSL con "
					+ "un error de tipeo.");

				foreach (string arguments in calls)
					Assert.That(arguments, Does.Contain(TheWrittenInstant),
						"'" + read + "(" + arguments + ")' no pregunta al instante del tablero. "
						+ "TheInstantTheBoardIsRead y Story.SixMonthsLater VALEN LO MISMO, asi que "
						+ "ninguna corrida podria notar el cambio: esta linea es el unico sitio donde "
						+ "se ve. El tablero pregunta a UN instante y lo dice en su propia salida.");
			}
		}

		[Test]
		public void TheSevenThatTakeNoInstantAreAskedWithNoneAndTheBoardInventsNoMilestone()
		{
			string live = TheLiveBoard();

			foreach (string read in TheReadsThatTakeNone)
			{
				List<string> calls = TheArgumentsOfEveryCallTo(live, read);

				Assert.That(calls, Is.Not.Empty,
					"El tablero no llama a '" + read + "'.");

				foreach (string arguments in calls)
					Assert.That(arguments, Does.Not.Contain(TheWrittenInstant),
						"'" + read + "' recibio un instante y no toma ninguno. La asimetria entre las "
						+ "que lo toman y las que no ES la afirmacion: el juego de filas no depende del "
						+ "instante, y ordenarlo en cuatro firmas iguales borraria eso.");
			}

			Assert.That(TheArgumentsOfEveryCallTo(live, TheOneTheBoardDoesNotCall), Is.Empty,
				"El tablero llamo a " + TheOneTheBoardDoesNotCall + ", que es la hermana SIN instante, "
				+ "y la consola tiene uno escrito. Para llamarla hay que darle un HITO, y un hito es un "
				+ "numero que nadie le publico a esta caratula: es la misma invencion que P-12 le quita "
				+ "a what_the_service_costs, con otra ropa.");
		}

		// LAS SEIS LECTURAS DE LA SERIE SE PIDEN EN EL TABLERO, Y SOLO CON LOS MOMENTOS ESCRITOS DE LA
		// PROPIA HISTORIA. Dos mitades, y la primera es un PISO: una lectura que la consola nunca
		// ejercito es la unica defensa que hay contra un error de tipeo en un script del DSL, porque
		// un typo del DSL se descubre corriendo y no compilando.
		//
		// LA SEGUNDA MITAD ES LA QUE DECIDE ALGO. Los limites del tramo se pueden escribir de dos
		// maneras que en esta historia dan EL MISMO INSTANTE: Story.Delivery es el 2025-09-01 y
		// TheInstantTheBoardIsRead.AddYears(-1) tambien. La derivada no la atrapa el barrido de
		// aritmetica -- el detector pide un caracter de operando antes del operador y ahi hay un
		// parentesis -- asi que si nadie afirma esto, la eleccion vive en la prosa que el bloque
		// imprime y en ningun otro lado. Lo que se pide es el nombre del momento, que es lo que dice
		// CUAL es; un -1 al lado de un AddYears es un largo de ventana que nadie publico.
		[Test]
		public void EveryReadOfTheSeriesIsAskedOnTheBoardAndOnlyWithTheStorysOwnWrittenMoments()
		{
			string region = TheRegionOfTheBlocks();

			foreach (string read in TheSixReadsOfTheSeries)
			{
				List<string> calls = TheArgumentsOfEveryCallTo(region, read);

				Assert.That(calls, Is.Not.Empty,
					"El tablero no llama a '" + read + "'. Las seis lecturas de la serie son TOTALES y "
					+ "ninguna rechaza, asi que no hay pre-chequeo que las cubra: lo unico que las "
					+ "ejercita es que la caratula las pida, y un script del DSL con un error de tipeo "
					+ "se descubre corriendo.");

				foreach (string arguments in calls)
					foreach (string bound in TheBoundsOf(arguments).Split(','))
						Assert.That(TheWrittenMomentsTheUseBlocksMayAskWith, Does.Contain(bound.Trim()),
							"'" + read + "(" + arguments + ")' pregunta con un limite que no es ninguno "
							+ "de los tres momentos escritos de la historia. Story.SixMonthsLater vale "
							+ "lo mismo que TheInstantTheBoardIsRead y ninguna corrida notaria el "
							+ "cambio; una fecha derivada de otra sigue siendo reproducible y deja de "
							+ "decir cual es.");
			}

			foreach (string derived in TheWaysOfDerivingAnInstant)
				Assert.That(region, Does.Not.Contain(derived),
					"'" + derived + "' en los bloques del tablero. Un tramo de doce meses escrito como "
					+ "'el instante del tablero menos un ano' trae un largo de ventana que nadie le "
					+ "publico a esta caratula, y en esta historia da exactamente el mismo instante que "
					+ "el dia de la entrega, que si es un hecho del guion.");
		}

		// EL ORDEN DE LOS CUATRO LIMITES DE LA COMPARACION, QUE ES CONTRATO Y NO PRESENTACION. El
		// cambio se dice del PRIMER periodo respecto al SEGUNDO -- OD-2 lo fijo cruzando los cuatro en
		// la puerta -- asi que cual mitad va primera decide el signo de la frase.
		//
		// SE MIRA EL TEXTO ENTERO DE LOS ARGUMENTOS Y NO CADA UNO POR SEPARADO: los cuatro limites
		// salen de un juego de tres nombres, asi que una permutacion pasa cualquier afirmacion que
		// mire los nombres sin el orden.
		[Test]
		public void TheComparisonAsksTheLaterHalfFirstAndTheEarlierHalfSecond()
		{
			List<string> calls = TheArgumentsOfEveryCallTo(
				TheRegionOfTheBlocks(), "DescribeTwoPeriodsOfUseCompared");

			Assert.That(calls, Has.Count.EqualTo(1),
				"La comparacion de las dos mitades se pide " + calls.Count + " veces en los bloques "
				+ "del tablero, y se pide una.");

			Assert.That(TheOneSpaced(TheBoundsOf(calls[0])), Is.EqualTo(TheComparisonsFourBoundsInOrder),
				"Los cuatro limites de la comparacion no son los cuatro esperados en ese orden. La "
				+ "mitad TARDIA -- de la primera evaluacion al instante del tablero -- va PRIMERA, y "
				+ "la temprana -- de la entrega a la primera evaluacion -- va segunda: el dominio dice "
				+ "el cambio del primer periodo respecto al segundo, asi que cruzarlos da la frase al "
				+ "reves sin que nada mas se entere. Lo escrito: " + TheOneSpaced(calls[0]));
		}

		// NINGUNA CLASE LA PONE LA CONSOLA. Las nueve salen de Story.TheNineKinds y cada lectura recibe
		// la del recorrido. Una clase entre comillas adentro de un bloque seria una fila de una clase
		// impresa bajo el titulo de otra, que es el rojo que esto existe para dar.
		[Test]
		public void NoReadOfTheBoardIsHandedAKindTheConsoleNamedItself()
		{
			string live = TheLiveBoard();

			var quoted = new List<string>();

			foreach (string read in TheReadsThatTakeAnInstant) TheQuotedFirstArguments(live, read, quoted);
			foreach (string read in TheReadsThatTakeNone) TheQuotedFirstArguments(live, read, quoted);

			Assert.That(quoted, Is.Empty,
				"Una lectura del tablero recibio una clase escrita por la caratula. La clase es la del "
				+ "recorrido, siempre: un juego de filas de otra clase impreso bajo el titulo de la que "
				+ "el tablero esta recorriendo no lo nota nadie leyendo la salida. Lo encontrado:\n  "
				+ string.Join("\n  ", quoted));
		}

		// CADA LECTURA GUARDADA SE PIDE ADENTRO DE LA RAMA QUE ABRIO SU PRE-CHEQUEO, y esta prueba
		// EXISTE PORQUE UN PARCHE NO PUSO NADA ROJO. Borrarle al tablero el 'if' sobre CanDistance...
		// dejo la suite entera verde: el catch por CLASE atrapa el rechazo traducido y lo imprime, asi
		// que la corrida sigue y las otras ocho clases no se enteran -- que es para lo que el catch
		// esta. Lo que se pierde en silencio son las cinco lineas siguientes de ESA clase, y ninguna
		// prueba de la suite mira la salida de la consola.
		//
		// ES EL HALLAZGO DE DEC-65 EN UN CUARTO DISFRAZ: el pre-chequeo no sostiene el TIPO -- la
		// traduccion da el mismo -- sino el MENSAJE y el orden de las preguntas. El instrumento que
		// nombra un comportamiento no ve, por si mismo, el defecto que su nombre describe.
		//
		// SE MIDE POR SANGRIA porque es lo que hay: el fuente viaja como texto y no hay arbol
		// sintactico. Las tres condiciones juntas -- el local ligado a su Can, un 'if' sobre ese local
		// a la misma sangria, y la lectura guardada mas adentro y despues -- son exactamente lo que un
		// lector comprueba, y las tres tienen que darse.
		private static readonly object[] TheGuardOfEachRead =
		{
			new object[] { "CanDistanceRemainingBeSaid", "distance", "ReadDistanceRemaining" },
			new object[] { "CanDistanceRemainingBeSaid", "distance", "ReadDistanceRemainingUnit" },
			new object[] { "CanDaysRemainingBeSaid", "days", "ReadDaysRemaining" },
		};

		[Test]
		[TestCaseSource(nameof(TheGuardOfEachRead))]
		public void EachGuardedReadIsAskedInsideTheBranchItsPreCheckOpened(
			string preCheck, string local, string guarded)
		{
			string[] live = TheLiveBoard().Split('\n');
			string[] written = File.ReadAllText(TheBoardsSource()).Split('\n');

			var bound = new Regex(@"bool\s+" + local + @"\s*=\s*api\s*\.\s*" + preCheck + @"\s*\(");
			var branch = new Regex(@"^\s*if\s*\(\s*" + local + @"\s*\)\s*\r?$");
			var asked = new Regex(@"api\s*\.\s*" + guarded + @"\s*\(");

			int atBound = -1;
			int atBranch = -1;
			int atAsked = -1;

			foreach (int line in TheLinesOfTheBlocks(written))
			{
				if (bound.IsMatch(live[line])) atBound = line;
				if (branch.IsMatch(live[line])) atBranch = line;
				if (asked.IsMatch(live[line])) atAsked = line;
			}

			Assert.That(atBound, Is.GreaterThanOrEqualTo(0),
				"El tablero no liga '" + preCheck + "' a un local llamado '" + local + "'. Sin eso no "
				+ "hay condicion que mirar, y una condicion que no esta no se puede comprobar.");

			Assert.That(atAsked, Is.GreaterThanOrEqualTo(0),
				"El tablero no pide '" + guarded + "'.");

			Assert.That(atBranch, Is.InRange(atBound, atAsked),
				"'" + guarded + "' no se pide adentro de un 'if (" + local + ")'. La pregunta es "
				+ "'se puede decir' y despues 'decilo': sin el if, la puerta rechaza, el catch por "
				+ "clase imprime el rechazo y las lineas que faltaban de ESA clase se pierden sin que "
				+ "nada se ponga rojo. Ese silencio es lo que esta prueba compra.");

			Assert.That(TheIndentOf(live[atBranch]), Is.EqualTo(TheIndentOf(live[atBound])),
				"El 'if (" + local + ")' no esta a la sangria del local que lo condiciona, asi que no "
				+ "es su rama.");

			Assert.That(TheIndentOf(live[atAsked]), Is.GreaterThan(TheIndentOf(live[atBound])),
				"'" + guarded + "' esta a la misma sangria que su pre-chequeo o mas afuera, asi que "
				+ "se pide siempre y no solo cuando se puede decir.");
		}

		private static int TheIndentOf(string line)
		{
			int tabs = 0;

			while (tabs < line.Length && line[tabs] == '\t') tabs++;

			return tabs;
		}

		[Test]
		[TestCaseSource(nameof(TheLabelOfEachColumn))]
		public void EveryPrintedCellIsUnderItsOwnLabelAndTheColumnsAreNotCrossed(
			string column, string label)
		{
			string[] live = TheLiveBoard().Split('\n');
			string[] written = File.ReadAllText(TheBoardsSource()).Split('\n');

			var wrong = new List<string>();
			var right = new List<string>();

			foreach (int line in TheLinesOfTheBlocks(written))
			{
				if (!live[line].Contains(column)) continue;

				if (written[line].Contains(label)) right.Add(written[line].Trim());
				else wrong.Add((line + 1) + "  " + written[line].Trim());
			}

			Assert.That(right, Is.Not.Empty,
				"Ninguna linea del tablero imprime la columna '" + column + "'. Una columna que no se "
				+ "imprime no se puede cruzar, y tampoco se puede leer.");

			Assert.That(wrong, Is.Empty,
				"La columna '" + column + "' se imprimio en una linea que no lleva su etiqueta ('"
				+ label + "'). Una celda bajo la etiqueta de su vecina es el defecto que ninguna "
				+ "afirmacion sobre el LARGO de las columnas puede ver: los largos siguen cuadrando. "
				+ "Lo encontrado:\n  " + string.Join("\n  ", wrong));
		}

		// LO QUE §8 DE PLAN-BuildOrder.md PIDE POR SU NOMBRE, dicho aparte de la tabla de arriba aunque
		// la tabla ya lo cubra: cada fila de precios impresa nombra su revision. L9 -- una fila nombra
		// la revision que gobierna su clave -- no vale nada si la caratula tiene la columna y no la
		// pinta.
		[Test]
		public void EveryPrintedPriceRowNamesItsRevision()
		{
			string[] live = TheLiveBoard().Split('\n');
			string[] written = File.ReadAllText(TheBoardsSource()).Split('\n');

			// SE MIRA LA REGION Y NO EL ARCHIVO: 'taller' tambien imprime una columna Says, asi que un
			// Does.Contain global pasaria con la linea de otra rebanada.
			var inTheBlocks = new List<string>();

			foreach (int line in TheLinesOfTheBlocks(written)) inTheBlocks.Add(live[line]);

			string region = string.Join("\n", inTheBlocks);

			Assert.That(region, Does.Contain(".Revision["),
				"El tablero imprime filas de precios y no nombra la revision de ninguna. La revision "
				+ "es la procedencia de la fila: sin ella la fila dice un precio y no de donde salio, y "
				+ "L9 -- una fila nombra la revision que gobierna su clave -- no vale nada si la "
				+ "caratula tiene la columna y no la pinta.");

			foreach (string each in new[] { ".Operation[", ".Milestone[", ".Says[" })
				Assert.That(region, Does.Contain(each),
					"El registro de precios tiene cuatro columnas y el tablero no imprime '" + each
					+ "'. Una fila que llega entera y se pinta a medias es media respuesta con otro "
					+ "nombre.");
		}

		// EL AUTOCONTROL. Sin esto, las cuatro leyes de arriba son cuatro listas vacias, y una lista
		// vacia cumple cualquier cosa: es la forma exacta en que SH-0 se gano un rojo.
		[Test]
		public void TheReaderSeesACallAtAnotherInstantAndAKindTheShellNamedWhenThereIsOne()
		{
			const string atAnother =
				"Console.WriteLine($\"   x : {api.DescribeCountdown(kind, Story.SixMonthsLater, "
				+ "TheVoiceOfTheDealership)}\");";

			List<string> found = TheArgumentsOfEveryCallTo(
				TheCodeOfTheShellsAsData.OnlyWhatTheCompilerReads(atAnother, "cs"), "DescribeCountdown");

			Assert.That(found, Has.Count.EqualTo(1), "El lector no vio la llamada.");
			Assert.That(found[0], Does.Not.Contain(TheWrittenInstant),
				"El lector dijo que una llamada a Story.SixMonthsLater pregunta al instante del "
				+ "tablero.");

			var quoted = new List<string>();
			TheQuotedFirstArguments(
				TheCodeOfTheShellsAsData.OnlyWhatTheCompilerReads(
					"CompletionRecord r = api.CompletionsOf(\"Repair\");", "cs"),
				"CompletionsOf", quoted);

			Assert.That(quoted, Has.Count.EqualTo(1),
				"El lector no vio una clase escrita por la caratula.");

			var notQuoted = new List<string>();
			TheQuotedFirstArguments(
				TheCodeOfTheShellsAsData.OnlyWhatTheCompilerReads(
					"CompletionRecord r = api.CompletionsOf(kind);", "cs"),
				"CompletionsOf", notQuoted);

			Assert.That(notQuoted, Is.Empty,
				"El lector conto la clase del recorrido como una escrita por la caratula.");

			// Y QUE LA CONSTANTE ES UN LITERAL ESCRITO, que es la otra mitad de 'no lee reloj': un
			// instante armado restandole dias a otro sigue sin leer reloj y ya no dice cual es. El
			// \s* cruza el salto de linea porque el fuente se lee entero y no linea por linea.
			Assert.That(
				new Regex(TheWrittenInstant
					+ @"\s*=\s*new DateTime\(\s*\d{4}\s*,\s*\d{1,2}\s*,\s*\d{1,2}\s*,")
					.IsMatch(TheLiveBoard()),
				Is.True,
				"TheInstantTheBoardIsRead no se declara con un new DateTime de argumentos constantes.");
		}

		// LOS INDICES DE LAS LINEAS DE LOS BLOQUES, y revienta si no encuentra los dos bordes: un
		// borde no encontrado devolveria una region vacia, y una region vacia deja las etiquetas verdes
		// por no haber mirado ninguna linea.
		private static IEnumerable<int> TheLinesOfTheBlocks(string[] written)
		{
			int from = -1;
			int to = -1;

			for (int line = 0; line < written.Length; line++)
			{
				if (written[line].Contains(TheFirstLineOfTheBlocks)) from = line;
				if (written[line].Contains(TheFirstLineAfterTheBlocks)) to = line;
			}

			Assert.That(from, Is.GreaterThanOrEqualTo(0),
				"No esta '" + TheFirstLineOfTheBlocks + "', que es donde empiezan los bloques.");
			Assert.That(to, Is.GreaterThan(from),
				"'" + TheFirstLineAfterTheBlocks + "' no esta despues de los bloques, asi que la "
				+ "region que se juzga esta vacia o al reves.");

			var lines = new List<int>();

			for (int line = from; line < to; line++) lines.Add(line);

			return lines;
		}

		// LA REGION ENTERA COMO TEXTO VIVO, para las afirmaciones que recorren LLAMADAS en vez de
		// lineas etiquetadas. Se acota igual que las etiquetas y por la misma razon: 'taller' y
		// 'garantia' preguntan con sus propios instantes escritos, que son correctos ahi y serian un
		// rojo aca.
		private static string TheRegionOfTheBlocks()
		{
			string[] live = TheLiveBoard().Split('\n');
			string[] written = File.ReadAllText(TheBoardsSource()).Split('\n');

			var inTheBlocks = new List<string>();

			foreach (int line in TheLinesOfTheBlocks(written)) inTheBlocks.Add(live[line]);

			return string.Join("\n", inTheBlocks);
		}

		// LOS ARGUMENTOS DICHOS CON UN SOLO ESPACIO ENTRE COMAS, para poder compararlos letra por
		// letra sin que el ancho de una linea partida decida el resultado.
		private static string TheOneSpaced(string arguments)
		{
			return new Regex(@"\s+").Replace(arguments, " ").Trim();
		}

		// LOS LIMITES DE PERIODO DE UNA LLAMADA, SIN EL IDIOMA. Desde el turno del idioma las tres
		// lecturas que componen frase reciben ademas la clave del catalogo con que se pide la
		// redaccion, y esa no es un momento de la historia: es una constante de esta caratula y ya la
		// afirma NoReadOfTheBoardIsHandedAKindTheConsoleNamedItself por el lado de las clases.
		private static string TheBoundsOf(string arguments)
		{
			int language = arguments.IndexOf(TheLanguageTheBoardAsksIn, StringComparison.Ordinal);

			if (language < 0) return arguments;

			return arguments.Substring(0, language).TrimEnd().TrimEnd(',');
		}

		private static string TheBoardsSource()
		{
			return Path.Combine(
				TheCodeOfTheShellsAsData.FolderOf(TheCodeOfTheShellsAsData.Consola), TheBoardsFile);
		}

		private static string TheLiveBoard()
		{
			return TheCodeOfTheShellsAsData.OnlyWhatTheCompilerReads(
				File.ReadAllText(TheBoardsSource()), "cs");
		}

		// LOS ARGUMENTOS DE CADA LLAMADA A ESE MIEMBRO. Sin parentesis anidados adentro, que es lo que
		// las llamadas de los bloques tienen: (kind, TheInstantTheBoardIsRead) o (kind) o ().
		// El \b de adelante y el ( de atras separan ReadDistanceRemaining de
		// ReadDistanceRemainingUnit y CompletionsOf de CompletionsOfEveryKind.
		private static List<string> TheArgumentsOfEveryCallTo(string live, string member)
		{
			var found = new List<string>();

			foreach (Match each in new Regex(@"\b" + member + @"\s*\(([^()]*)\)").Matches(live))
				found.Add(each.Groups[1].Value.Trim());

			return found;
		}

		private static void TheQuotedFirstArguments(string live, string member, List<string> into)
		{
			foreach (Match each in new Regex(@"\b" + member + @"\s*\(\s*""[^""]*""").Matches(live))
				into.Add(member + "  " + each.Value.Trim());
		}
	}
}
