using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace DealershipCar.Dominio.Pruebas
{
	// EL CODIGO DE LAS CARATULAS, LEIDO COMO TEXTO Y NO POR REFLEXION. Lo que hay que mirar para la
	// regla 3 de la convencion de registro crudo -- "la caratula no hace cuentas con un valor del
	// dominio" -- es un OPERADOR ENTRE DOS OPERANDOS, y eso no sobrevive a la compilacion: un
	// ensamblado no dice si el numero que devuelve salio de una resta o de la puerta.
	//
	// ESTE ARCHIVO ES EL INSTRUMENTO, no la ley. La ley la afirma
	// TheShellPerformsNoArithmeticOnADomainValue; aca solo se localizan los archivos, se apaga lo que
	// el compilador no lee, y se marcan las lineas con un operador. NADA de esto juzga: el barrido no
	// decide si un acierto esta bien, obliga a que alguien lo escriba y diga por que.
	//
	// LO QUE EL DETECTOR NO VE, dicho aca y afirmado por prueba en el fixture:
	//
	//   1. UNA CUENTA PARTIDA EN DOS LINEAS. Se mira linea por linea, asi que "a" en una linea y
	//      "- b" en la siguiente no se ve. Es el punto ciego que PLAN-BuildOrder.md §2.2 ya declara.
	//   2. UN INCREMENTO PREFIJO (++i, --i). Solo se mira el postfijo, porque "--" seguido de una
	//      letra es una variable de CSS en tres de los cinco lenguajes que se barren.
	//   3. UN OPERADOR PEGADO EN LOS LENGUAJES DONDE UN GUION ES PARTE DE UN NOMBRE. En .cs un guion
	//      nunca es parte de un identificador, asi que "a-b" cuenta entero; en .razor la mitad del
	//      archivo es markup y ahi un guion es un nombre -- aria-label, d-pill--on -- asi que EL
	//      GUION pide espacio de un lado y los otros cuatro operadores siguen pegados; en .js, .html
	//      y .conf "X-Forwarded-For" y "application/wasm" son nombres y TODO operador pide espacio.
	//   4. UNA CUENTA DENTRO DEL TEXTO DE UN LITERAL. El contenido de "...", de @"..." y el TEXTO de
	//      un interpolado se apaga, porque si no toda fecha "2026-06-10" y todo especificador de
	//      formato {x:yyyy-MM-dd} serian aciertos. Los AGUJEROS de un interpolado quedan vivos: ahi
	//      la cuenta si se ejecuta.
	//   5. UNA CONCATENACION CONTRA UN LITERAL. "a" + b no se marca: en C# un "+" con un operando de
	//      texto es concatenacion SIEMPRE, no aritmetica, y marcarlo llenaria la lista de permitidos
	//      con cada frase que cualquier caratula compone. Una concatenacion entre dos IDENTIFICADORES
	//      si se marca.
	//   6. UN "//" EN TEXTO DE MARKUP SIN COMILLAS. Se toma por comentario y apaga el resto de la
	//      linea. Una URL suelta en markup entra en ese caso; una entre comillas no.
	internal static class TheCodeOfTheShellsAsData
	{
		internal const string App = "App";
		internal const string Frontera = "Frontera";
		internal const string Consola = "Consola";
		internal const string Mcp = "Mcp";
		internal const string Wasm = "Wasm";
		internal const string EndPointDeEventos = "EndPointDeEventos";
		internal const string InyectorDeEventosDemo = "InyectorDeEventosDemo";
		internal const string PaginaDeDescarga = "PaginaDeDescarga";
		internal const string MiCarroAppMockup = "MiCarroAppMockup";

		private const string Sharp = "cs";
		private const string Razor = "razor";
		private const string Js = "js";
		private const string Html = "html";
		private const string Conf = "conf";

		// UN OPERANDO SE RECONOCE POR EL CARACTER QUE LO TERMINA O LO EMPIEZA, y no por el token
		// entero: alcanza para decidir si hay algo a los dos lados del operador. La comilla NO esta,
		// que es el punto 5 de la cabecera.
		private const string EndsAnOperand = @"[A-Za-z0-9_)\]]";
		private const string StartsAnOperand = @"(?:[A-Za-z0-9_(]|@[A-Za-z_])";

		private static readonly Regex TightBinary = new Regex(
			"(?<=" + EndsAnOperand + @"\s{0,4})[-+*/%]=?(?=\s{0,4}" + StartsAnOperand + ")");

		// EN UN .razor EL GUION ES OTRA COSA. La mitad de un .razor es markup, y ahi un guion es parte
		// de un nombre -- aria-label, stroke-width, d-pill--on -- no un operador. Los otros cuatro
		// operadores siguen pegados, que es donde caen los casos que las rebanadas llaman los mas
		// filosos: una division en una tarjeta, una suma al lado de un total.
		private static readonly Regex RazorBinary = new Regex(
			"(?<=" + EndsAnOperand + @"\s{0,4})[+*/%]=?(?=\s{0,4}" + StartsAnOperand + ")"
			+ "|(?<=" + EndsAnOperand + @"\s{1,4})-=?(?=\s{0,4}" + StartsAnOperand + ")"
			+ "|(?<=" + EndsAnOperand + @"\s{0,4})-=?(?=\s{1,4}" + StartsAnOperand + ")");

		private static readonly Regex LooseBinary = new Regex(
			"(?<=" + EndsAnOperand + @"\s{1,4})[-+*/%]=?(?=\s{0,4}" + StartsAnOperand + ")"
			+ "|(?<=" + EndsAnOperand + @"\s{0,4})[-+*/%]=?(?=\s{1,4}" + StartsAnOperand + ")");

		private static readonly Regex SharpStep = new Regex(
			"(?<=" + EndsAnOperand + @")(\+\+|--)");

		private static readonly Regex OtherStep = new Regex(
			"(?<=" + EndsAnOperand + @")\+\+");

		private static readonly Regex RunsOfWhitespace = new Regex(@"\s+");

		/// <summary>Las siete raices de codigo enviado, que se JUZGAN contra la lista de permitidos.</summary>
		internal static string[] TheJudgedRoots()
		{
			return new[]
			{
				App, Frontera, Consola, Mcp, Wasm, EndPointDeEventos, InyectorDeEventosDemo,
			};
		}

		/// <summary>Las dos que se ENUMERAN: hosting y maqueta, ninguna de las dos codigo de caratula.</summary>
		internal static string[] TheEnumeratedRoots()
		{
			return new[] { PaginaDeDescarga, MiCarroAppMockup };
		}

		internal static string[] EveryRoot()
		{
			var all = new List<string>();
			all.AddRange(TheJudgedRoots());
			all.AddRange(TheEnumeratedRoots());

			return all.ToArray();
		}

		internal static string FolderOf(string root)
		{
			return Path.Combine(AppContext.BaseDirectory, "CodigoComoDato", root);
		}

		// SIN NINGUN catch. Un catch alrededor de una lectura de archivo deja el barrido verde
		// habiendo leido cero, que es exactamente como SH-0 se gano un rojo. Si un archivo no llego,
		// esto revienta con el nombre de la carpeta.
		internal static string[] EveryFileOf(string root)
		{
			string folder = FolderOf(root);

			if (!Directory.Exists(folder))
				throw new DirectoryNotFoundException(
					"La raiz '" + root + "' no llego al output: no existe '" + folder + "'. El grupo "
					+ "<None> de esa raiz en el csproj es lo que la trae.");

			string[] files = Directory.GetFiles(folder, "*", SearchOption.AllDirectories);
			Array.Sort(files, StringComparer.Ordinal);

			return files;
		}

		/// <summary>El camino del archivo dentro de su raiz, con barras hacia adelante.</summary>
		internal static string InsideItsRoot(string root, string file)
		{
			return file.Substring(FolderOf(root).Length + 1).Replace('\\', '/');
		}

		internal static List<ArithmeticHit> TheArithmeticIn(string root)
		{
			var hits = new List<ArithmeticHit>();

			foreach (string file in EveryFileOf(root))
			{
				string source = File.ReadAllText(file);

				if (source.Length == 0)
					throw new InvalidDataException(
						"'" + file + "' llego vacio al output. Un archivo vacio no tiene aciertos, y "
						+ "un barrido que lo cuenta como limpio esta verde por no haber leido nada.");

				hits.AddRange(TheArithmeticInSource(root, InsideItsRoot(root, file), source));
			}

			return hits;
		}

		/// <summary>
		/// Las lineas de un fuente con un operador aritmetico entre dos operandos. Una linea con tres
		/// operadores es UN acierto: lo que se permite o se prohibe es la linea, que es lo que una
		/// persona lee.
		/// </summary>
		internal static List<ArithmeticHit> TheArithmeticInSource(string root, string file, string source)
		{
			string language = TheLanguageOf(file);
			Regex binary = language == Sharp ? TightBinary : language == Razor ? RazorBinary : LooseBinary;
			Regex step = language == Sharp ? SharpStep : OtherStep;

			string[] live = OnlyWhatTheCompilerReads(source, language).Split('\n');
			string[] written = source.Split('\n');

			var hits = new List<ArithmeticHit>();

			for (int i = 0; i < live.Length; i++)
			{
				if (!binary.IsMatch(live[i]) && !step.IsMatch(live[i])) continue;

				hits.Add(new ArithmeticHit
				{
					Root = root,
					File = file,
					Line = i + 1,
					Snippet = RunsOfWhitespace.Replace(written[i].Trim(), " "),
				});
			}

			return hits;
		}

		// UNA EXTENSION QUE NADIE CLASIFICO REVIENTA A PROPOSITO. Un lenguaje nuevo en una raiz
		// barrida se clasifica aca antes de que el barrido opine de el: apagar comentarios de un
		// lenguaje con las reglas de otro es leer mal en silencio.
		private static string TheLanguageOf(string file)
		{
			if (file.EndsWith(".cs", StringComparison.Ordinal)) return Sharp;
			if (file.EndsWith(".razor", StringComparison.Ordinal)) return Razor;
			if (file.EndsWith(".js", StringComparison.Ordinal)) return Js;
			if (file.EndsWith(".html", StringComparison.Ordinal)) return Html;
			if (file.EndsWith(".conf", StringComparison.Ordinal)) return Conf;

			throw new NotSupportedException(
				"'" + file + "' llego al barrido y su extension no esta clasificada. Un lenguaje "
				+ "nuevo dice como se apagan sus comentarios y si un guion puede ser parte de un "
				+ "nombre; barrerlo con las reglas de otro es leer mal en silencio.");
		}

		/// <summary>
		/// El fuente con los comentarios y el contenido de los literales de texto normales apagados a
		/// espacios, dejando cada caracter en su posicion para que las lineas sigan cuadrando. Los
		/// literales INTERPOLADOS quedan vivos: ahi la cuenta se ejecuta de verdad.
		/// </summary>
		internal static string OnlyWhatTheCompilerReads(string source, string language)
		{
			bool slashSlash = language != Conf;
			bool slashStar = language != Conf;
			bool hash = language == Conf;
			bool razor = language == Razor;
			bool htmlComment = language == Razor || language == Html;
			bool verbatim = language == Sharp || language == Razor;
			bool backtick = language == Js || language == Html;

			char[] live = source.ToCharArray();
			int i = 0;

			while (i < source.Length)
			{
				char c = source[i];

				if (hash && c == '#') { i = BlankToEndOfLine(live, source, i); continue; }
				if (slashSlash && c == '/' && At(source, i + 1) == '/') { i = BlankToEndOfLine(live, source, i); continue; }
				if (slashStar && c == '/' && At(source, i + 1) == '*') { i = BlankThrough(live, source, i, "*/"); continue; }
				if (razor && c == '@' && At(source, i + 1) == '*') { i = BlankThrough(live, source, i, "*@"); continue; }
				if (htmlComment && c == '<' && Starts(source, i, "<!--")) { i = BlankThrough(live, source, i, "-->"); continue; }

				if (verbatim && c == '@' && At(source, i + 1) == '"') { i = TakeVerbatim(live, source, i + 1); continue; }
				if (verbatim && c == '@' && At(source, i + 1) == '$' && At(source, i + 2) == '"') { i = TakeInterpolated(live, source, i + 2, true); continue; }
				if (verbatim && c == '$' && At(source, i + 1) == '@' && At(source, i + 2) == '"') { i = TakeInterpolated(live, source, i + 2, true); continue; }
				if (c == '$' && At(source, i + 1) == '"') { i = TakeInterpolated(live, source, i + 1, false); continue; }

				// UN ATRIBUTO DE MARKUP QUE ABRE UNA EXPRESION DE RAZOR NO ES UN LITERAL. En
				// class="@("d-pill--" + Tono)" las comillas de afuera son del atributo y las de
				// adentro son del literal de C#; emparejarlas de izquierda a derecha deja el literal
				// VIVO y su clase CSS se lee como una resta. La comilla que trae un '@' pegado se
				// emite y ya: la de adentro es la que abre el literal.
				if (c == '"' && razor && At(source, i + 1) == '@') { i++; continue; }

				if (c == '"') { i = TakeQuoted(live, source, i, '"', true); continue; }
				if (c == '\'') { i = TakeQuoted(live, source, i, '\'', true); continue; }
				if (backtick && c == '`') { i = TakeQuoted(live, source, i, '`', false); continue; }

				i++;
			}

			return new string(live);
		}

		private static char At(string source, int i)
		{
			return i < source.Length ? source[i] : '\0';
		}

		private static bool Starts(string source, int i, string what)
		{
			return i + what.Length <= source.Length
				&& string.CompareOrdinal(source, i, what, 0, what.Length) == 0;
		}

		private static int BlankToEndOfLine(char[] live, string source, int from)
		{
			int i = from;

			while (i < source.Length && source[i] != '\n' && source[i] != '\r') live[i++] = ' ';

			return i;
		}

		private static int BlankThrough(char[] live, string source, int from, string terminator)
		{
			int end = source.IndexOf(terminator, from, StringComparison.Ordinal);
			int stop = end < 0 ? source.Length : end + terminator.Length;

			for (int i = from; i < stop; i++)
				if (source[i] != '\n' && source[i] != '\r') live[i] = ' ';

			return stop;
		}

		// UN DELIMITADOR SIN PAREJA EN SU PROPIA LINEA NO ES UN LITERAL. Un apostrofo suelto en texto
		// de markup apagaria el resto del archivo, y un barrido que apago el archivo esta verde por no
		// haber leido nada -- la misma trampa que el catch.
		private static int TakeQuoted(char[] live, string source, int quote, char delimiter, bool blank)
		{
			int i = quote + 1;

			while (i < source.Length && source[i] != '\n' && source[i] != '\r')
			{
				if (source[i] == '\\') { i += 2; continue; }
				if (source[i] == delimiter) break;
				i++;
			}

			if (i >= source.Length || source[i] != delimiter) return quote + 1;

			if (blank)
				for (int k = quote + 1; k < i; k++) live[k] = ' ';

			return i + 1;
		}

		private static int TakeVerbatim(char[] live, string source, int quote)
		{
			int i = quote + 1;

			while (i < source.Length)
			{
				if (source[i] != '"') { i++; continue; }
				if (At(source, i + 1) == '"') { i += 2; continue; }
				break;
			}

			if (i >= source.Length) return quote + 1;

			for (int k = quote + 1; k < i; k++)
				if (source[k] != '\n' && source[k] != '\r') live[k] = ' ';

			return i + 1;
		}

		// UN LITERAL INTERPOLADO ES DOS COSAS PEGADAS: texto, que es literal como cualquier otro, y
		// AGUJEROS, que son expresiones que de verdad se ejecutan. Dejarlo entero vivo convierte cada
		// especificador de formato -- {ObservedAt:yyyy-MM-dd} -- en una resta; apagarlo entero pierde
		// la unica cuenta que un texto puede llevar adentro. Se apaga el texto y quedan los agujeros,
		// que es exactamente lo que el compilador compila.
		//
		// Y DENTRO DEL AGUJERO SE APAGA EL FORMATO: en C# un ':' a profundidad cero abre el
		// especificador y una ',' la alineacion. Es la misma regla del lenguaje, y por eso un
		// condicional adentro de un agujero tiene que ir entre parentesis alla y aca tambien.
		private static int TakeInterpolated(char[] live, string source, int quote, bool verbatim)
		{
			var blank = new List<int>();
			int i = quote + 1;
			int end = -1;
			int holes = 0;
			int nesting = 0;
			bool format = false;

			while (i < source.Length)
			{
				char c = source[i];

				if (!verbatim && (c == '\n' || c == '\r')) break;

				if (holes == 0)
				{
					if (!verbatim && c == '\\') { blank.Add(i); blank.Add(i + 1); i += 2; continue; }
					if (verbatim && c == '"' && At(source, i + 1) == '"') { blank.Add(i); blank.Add(i + 1); i += 2; continue; }
					if (c == '"') { end = i; break; }
					if ((c == '{' || c == '}') && At(source, i + 1) == c) { blank.Add(i); blank.Add(i + 1); i += 2; continue; }
					if (c == '{') { blank.Add(i); holes = 1; nesting = 0; format = false; i++; continue; }

					blank.Add(i);
					i++;
					continue;
				}

				if (c == '(' || c == '[' || c == '{') { nesting++; if (format) blank.Add(i); i++; continue; }
				if (c == ')' || c == ']') { nesting--; if (format) blank.Add(i); i++; continue; }

				if (c == '}')
				{
					if (nesting > 0) { nesting--; if (format) blank.Add(i); i++; continue; }

					blank.Add(i);
					holes = 0;
					i++;
					continue;
				}

				if (nesting == 0 && !format && (c == ':' || c == ',')) { format = true; blank.Add(i); i++; continue; }

				if (format) blank.Add(i);
				i++;
			}

			if (end < 0) return quote + 1;

			foreach (int k in blank)
				if (k < source.Length && source[k] != '\n' && source[k] != '\r') live[k] = ' ';

			return end + 1;
		}
	}

	/// <summary>Una linea de una caratula con un operador aritmetico entre dos operandos.</summary>
	internal sealed class ArithmeticHit
	{
		internal string Root { get; set; }

		internal string File { get; set; }

		internal int Line { get; set; }

		internal string Snippet { get; set; }

		/// <summary>
		/// La llave con que se compara contra la lista de permitidos. Lleva la raiz, el archivo y el
		/// TEXTO de la linea, y NO el numero: un numero de linea cambia cada vez que alguien escribe
		/// un comentario mas arriba, y un barrido que se pone rojo por eso deja de leerse.
		/// </summary>
		internal string Key()
		{
			return Root + " | " + File + " | " + Snippet;
		}

		public override string ToString()
		{
			return Root + "/" + File + ":" + Line + "  " + Snippet;
		}
	}
}
