using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// EL TABLERO DE 'historia' PREGUNTA LA PANTALLA DE MANTENIMIENTO A UN INSTANTE ESCRITO Y A NINGUN
	// OTRO. La consola GUIONA una secuencia recibida, asi que no lee reloj (C27) -- eso ya lo afirma
	// TheConsoleStagesFromLiteralsAndReadsNoClock, y esta fixture NO lo repite. Lo que agrega es la otra
	// mitad, la que ese barrido no puede ver: que de los instantes ESCRITOS que la consola tiene, los
	// tres bloques nuevos usan UNO, y es TheInstantTheBoardIsRead.
	//
	// POR QUE TIENE QUE SER UNA PRUEBA SOBRE EL TEXTO Y NO SOBRE UNA CORRIDA. Dos razones, y la segunda
	// es la que decide:
	//
	//   1. Pruebas/ no referencia DealershipCar.Consola -- solo su FUENTE viaja al output como dato, en
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
	// sea la del recorrido. Hoy no hay ninguna otra en los tres bloques, y por eso lo que se afirma es
	// que ninguna lectura recibe una clase ENTRE COMILLAS -- que es la unica forma en que la consola
	// podria nombrar una por su cuenta, porque no referencia el dominio y no tiene sus enums.
	[TestFixture]
	public sealed class TheConsoleReadsTheCountdownAtTheInstantTheBoardIsReadAndNoOther
	{
		private const string TheWrittenInstant = "TheInstantTheBoardIsRead";

		private const string TheBoardsFile = "Program.cs";

		// LOS TRES BLOQUES. El piso de abajo cuenta archivos; esto nombra los miembros: un barrido que
		// leyo Program.cs y no encontro ninguno de los tres esta verde por no haber nada que juzgar.
		private static readonly string[] TheThreeBlocks =
		{
			"TheCountdownOfEveryKind", "TheHistoryOfEveryKind", "ThePublishedServicePrices",
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

		// LA REGION QUE ESTA FIXTURE JUZGA PARA LAS ETIQUETAS: los tres bloques, que van seguidos y
		// terminan donde empiezan las mediciones contra el disco. Se acota A PROPOSITO y no por
		// comodidad: 'taller' imprime su propia columna Says y la pinta en una linea de continuacion sin
		// etiqueta, que es otra rebanada y otra decision (DEC-52/DEC-61). Un barrido global tendria que
		// llevarla en una lista de excepciones, y una lista de excepciones sobre codigo ajeno crece.
		//
		// Y LA REGION SE DEFIENDE SOLA: si alguien saca uno de los tres printers de aca, la columna que
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
		};

		[Test]
		public void TheThreeBlocksArrivedAndTheSweepIsReadingTheFileTheyAreIn()
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

			foreach (string block in TheThreeBlocks)
				Assert.That(live, Does.Contain(block),
					"El bloque '" + block + "' no esta en el tablero. Las afirmaciones de esta fixture "
					+ "recorren LLAMADAS: sin los tres bloques no hay ninguna, y no haber ninguna es "
					+ "verde.");
		}

		// Y LOS TRES SE LLAMAN, UNA VEZ CADA UNO, DESDE LA HISTORIA DE SEIS MESES. Esta prueba tambien
		// existe porque un parche no puso nada rojo: borrada la LLAMADA al bloque de precios -- el
		// bloque entero declarado y nunca invocado --, la suite quedo verde y el tablero imprimia dos
		// tercios de si mismo. Un metodo privado que nadie llama no lo avisa el compilador de C#, y
		// ninguna de las otras afirmaciones de esta fixture mira las LLAMADAS: todas miran el cuerpo.
		//
		// UNA VEZ CADA UNO Y DESDE ADENTRO DE 'historia', que es lo que PLAN-BuildOrder.md §8 dice --
		// tres bloques y UN llamador. Llamarlos tambien desde 'estado' o desde 'refresco' no esta
		// prohibido, esta obligado a decirse: ahi los instantes son otros y P-13 reserva el guionado a
		// las rebanadas que reciben un hecho nuevo.
		private const string TheCallerOfTheBlocks = "private static int TheSixMonthStory(";

		[Test]
		public void TheThreeBlocksAreCalledOnceEachFromTheSixMonthStoryAndFromNowhereElse()
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

			foreach (string block in TheThreeBlocks)
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

			foreach (int line in TheLinesOfTheThreeBlocks(written))
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

			foreach (int line in TheLinesOfTheThreeBlocks(written))
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

			foreach (int line in TheLinesOfTheThreeBlocks(written)) inTheBlocks.Add(live[line]);

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
				"Console.WriteLine($\"   x : {api.DescribeCountdown(kind, Story.SixMonthsLater)}\");";

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

		// LOS INDICES DE LAS LINEAS DE LOS TRES BLOQUES, y revienta si no encuentra los dos bordes: un
		// borde no encontrado devolveria una region vacia, y una region vacia deja las etiquetas verdes
		// por no haber mirado ninguna linea.
		private static IEnumerable<int> TheLinesOfTheThreeBlocks(string[] written)
		{
			int from = -1;
			int to = -1;

			for (int line = 0; line < written.Length; line++)
			{
				if (written[line].Contains(TheFirstLineOfTheBlocks)) from = line;
				if (written[line].Contains(TheFirstLineAfterTheBlocks)) to = line;
			}

			Assert.That(from, Is.GreaterThanOrEqualTo(0),
				"No esta '" + TheFirstLineOfTheBlocks + "', que es donde empiezan los tres bloques.");
			Assert.That(to, Is.GreaterThan(from),
				"'" + TheFirstLineAfterTheBlocks + "' no esta despues de los tres bloques, asi que la "
				+ "region que se juzga esta vacia o al reves.");

			var lines = new List<int>();

			for (int line = from; line < to; line++) lines.Add(line);

			return lines;
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
		// las llamadas de los tres bloques tienen: (kind, TheInstantTheBoardIsRead) o (kind) o ().
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
