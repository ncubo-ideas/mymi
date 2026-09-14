using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LA REGLA 3 DE LA CONVENCION DE REGISTRO CRUDO, HECHA BARRIDO. "Todo agregado en pantalla lo
	// compone el dominio: totales, conteos, porcentajes, diferencias, adentro y leidos enteros. La
	// caratula no hace cuentas con un valor del dominio" -- SLICE-ShopStay.md §7, regla 3, identica en
	// las cuatro rebanadas.
	//
	// NINGUN CENSO EXISTENTE LA PUEDE VER. El censo de la puerta mira lo que SALE de la puerta; el de
	// las tools mira lo que ENTRA a la caratula MCP; el de markup mira los CONTROLES de la pantalla.
	// Una resta escrita en un .razor entre dos valores que ya cruzaron la puerta no toca ninguno de
	// los tres, y construye una frase rival que el dominio nunca dijo.
	//
	// ESTE BARRIDO NO JUZGA: OBLIGA A QUE EL JUICIO QUEDE ESCRITO. Es el mismo principio de
	// TheShellHasExactlyTheseThreeMembersThatReadAClock -- un cuarto reloj no esta prohibido, esta
	// obligado a decir de que es argumento antes de que la suite vuelva a verde. Aca un operador nuevo
	// no esta prohibido: esta obligado a entrar a la lista de permitidos con el miembro sobre el que
	// esta y la razon por la que es formato o infraestructura y no una cuenta.
	//
	// EL DISCRIMINADOR ES EL QUE NOMBRAN LAS REBANADAS: Wasm/Parts/HowANumberReads.cs dice de si mismo
	// "Es formato y no una cuenta". FORMATO SE PERMITE; ARITMETICA NO.
	//
	// LAS SIETE CARPETAS DEL MODULO Y COMO LAS TRATA:
	//
	//   App\ (cinco proyectos: App, Frontera, Consola, Mcp, Wasm)  JUZGADA
	//   EndPointDeEventos\                                          JUZGADA -- hermana de App\, no hija
	//   InyectorDeEventosDemo\                                      JUZGADA -- hermana de App\, no hija
	//   PaginaDeDescarga\                                           ENUMERADA, publicado\ excluido
	//   Dominio\                                                    FUERA DEL SUJETO -- componer es su oficio
	//   Pruebas\                                                    FUERA DEL SUJETO -- una prueba calcula lo que espera
	//   Mecanismo\                                                  EXCLUIDA, la regla de todo barrido del repo
	//
	// LO QUE EL DETECTOR NO VE esta en la cabecera de TheCodeOfTheShellsAsData, y los dos puntos
	// ciegos principales -- una cuenta partida en dos lineas y un operador pegado en un lenguaje donde
	// un guion es parte de un nombre -- estan AFIRMADOS por prueba mas abajo, para que sean una
	// medicion y no una promesa.
	[TestFixture]
	public sealed class TheShellPerformsNoArithmeticOnADomainValue
	{
		// LOS PISOS VAN ANTES QUE LA LISTA DE PERMITIDOS, y no es orden de lectura: un barrido que
		// leyo cero archivos tiene cero aciertos y una lista vacia lo declara limpio. SH-0 se gano un
		// rojo por exactamente esa forma. Estos numeros estan POR DEBAJO de lo que hay hoy: suben
		// cuando alguien agrega, y solo se ponen rojos cuando algo dejo de llegar.
		private static readonly object[] TheFloorOfEachRoot =
		{
			// EL PISO DE App SUBIO 20 -> 21 CON EL DTO DEL LIBRO DE COSTOS, que es UN archivo: la raiz
			// paso de 33 a 34 y el margen sigue siendo TRECE. Es la misma convencion que Wasm dejo
			// escrita abajo -- se mueve POR LO QUE SE AGREGO y no hasta donde alcance -- y subirlo es
			// acto de la tarea que ESCRIBE el archivo, nunca de la que cierra.
			//
			// Y SUBIO 21 -> 23 CON LOS DOS MENSAJES DEL FEED DEL LIBRO, que son DOS archivos: la raiz
			// paso de 34 a 36 y el margen sigue siendo TRECE. Los dos van en Events/, que es de esta
			// misma raiz: el barrido lee la raiz entera y no solo el archivo de la puerta.
			//
			// Y SUBIO 23 -> 24 CON EL DTO DE LAS ASIGNACIONES DE PLACA, que es UN archivo: la raiz paso
			// de 36 a 37 y el margen sigue siendo TRECE.
			//
			// Y SUBIO 24 -> 25 CON EL MENSAJE DEL FEED DE LA PLACA, que es UN archivo: la raiz paso de
			// 37 a 38 y el margen sigue siendo TRECE. Va en Events/, que es de esta misma raiz.
			new object[] { TheCodeOfTheShellsAsData.App, 25 },
			new object[] { TheCodeOfTheShellsAsData.Frontera, 2 },
			// EL PISO DE Consola SUBIO 4 -> 5 CON EL GUION DE LOS GASTOS, que es UN archivo: la raiz
			// paso de 5 a 6 y el margen sigue siendo UNO, que es el mas apretado de las ocho raices
			// con piso real. Se mueve POR LO QUE SE AGREGO y no hasta donde alcance.
			new object[] { TheCodeOfTheShellsAsData.Consola, 5 },
			// EL PISO DE Mcp SUBIO 8 -> 9 CON LA TOOL DEL LIBRO DE COSTOS, que es UN archivo: la raiz
			// paso de 12 a 13 y el margen sigue siendo CUATRO. OD-4 agrego un archivo a esta raiz y
			// dejo el piso quieto; aca se mueve, que es la convencion que Wasm dejo escrita abajo.
			//
			// Y SUBIO 9 -> 10 CON LA TOOL DE LA PLACA, que es UN archivo: la raiz paso de 13 a 14 y el
			// margen sigue siendo CUATRO. Subirlo es acto de la tarea que ESCRIBE el archivo.
			new object[] { TheCodeOfTheShellsAsData.Mcp, 10 },
			// EL PISO DE Wasm SUBIO 22 -> 24 CON LOS DOS PORTADORES DE LA PANTALLA DE MANTENIMIENTO,
			// para conservar el margen de cinco archivos que le dio la rebanada anterior. Un piso que
			// se queda quieto mientras la raiz crece es un piso que deja de decir cuando algo dejo de
			// llegar.
			//
			// Y SUBIO 24 -> 25 CON EL PORTADOR DEL RITMO DE USO, que es UN archivo: la raiz paso de 27
			// a 28 y el margen sigue siendo TRES. Se mueve por lo que se agrego y no hasta donde
			// alcance -- un piso pegado al conteo se pone rojo por cualquier reordenamiento, y uno
			// lejos deja de ver una raiz que se vacio.
			//
			// Y SUBIO 25 -> 26 CON EL PORTADOR DEL LIBRO DE COSTOS, que es UN archivo: la raiz paso de
			// 28 a 29 y el margen sigue siendo TRES. El bloque nuevo no trajo ningun .razor: va en el
			// archivo que SH-7 creo, que es donde P-10 pone los cuatro bloques.
			//
			// Y SUBIO 26 -> 27 CON EL PORTADOR DE LA PLACA, que es UN archivo: la raiz paso de 29 a 30
			// y el margen sigue siendo TRES. Tampoco trajo ningun .razor: el bloque es un renglon mas
			// de la tarjeta de informacion que Vehiculo.razor ya tenia.
			new object[] { TheCodeOfTheShellsAsData.Wasm, 27 },
			new object[] { TheCodeOfTheShellsAsData.EndPointDeEventos, 1 },
			new object[] { TheCodeOfTheShellsAsData.InyectorDeEventosDemo, 1 },
			new object[] { TheCodeOfTheShellsAsData.PaginaDeDescarga, 1 },
		};

		// UN ARCHIVO CON NOMBRE POR RAIZ. El piso cuenta; esto nombra. Un glob que se rompio y trajo
		// otra cosa pasa el piso y no pasa esto.
		private static readonly object[] TheFileEachRootIsNamedBy =
		{
			new object[] { TheCodeOfTheShellsAsData.App, "DealershipCarAPI.cs" },
			new object[] { TheCodeOfTheShellsAsData.Frontera, "WireName.cs" },
			new object[] { TheCodeOfTheShellsAsData.Consola, "Program.cs" },
			new object[] { TheCodeOfTheShellsAsData.Mcp, "Program.cs" },
			new object[] { TheCodeOfTheShellsAsData.Wasm, "Pages/MyCar.razor" },
			new object[] { TheCodeOfTheShellsAsData.EndPointDeEventos, "Program.cs" },
			new object[] { TheCodeOfTheShellsAsData.InyectorDeEventosDemo, "Program.cs" },
			new object[] { TheCodeOfTheShellsAsData.PaginaDeDescarga, "nginx.conf" },
		};

		// LA LISTA DE PERMITIDOS. Cada renglon: raiz, archivo, EL MIEMBRO SOBRE EL QUE ESTA, la linea
		// tal como esta escrita, y por que no es una cuenta con un valor del dominio.
		private static readonly AllowedArithmetic[] TheArithmeticThatIsAllowed =
		{
			new AllowedArithmetic
			{
				Root = TheCodeOfTheShellsAsData.App, File = "DealershipCarAPI.cs", Member = "InUtc",
				Snippet = "utc.Ticks - utc.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);",
				Reason = "Normalizacion del borde: trunca un instante al segundo entero que el journal "
					+ "guarda. El instante todavia NO cruzo la puerta, asi que no es un valor del "
					+ "dominio: es la forma en que se manda.",
			},
			new AllowedArithmetic
			{
				Root = TheCodeOfTheShellsAsData.App, File = "DealershipCarApp.cs", Member = "TheActorFor",
				Snippet = "return ThePrefixOfEveryActor + vin.Trim();",
				Reason = "Pega dos textos para armar el nombre del actor. Es un '+' entre dos "
					+ "identificadores, que el detector marca a proposito, y los dos son texto: no "
					+ "hay ningun numero.",
			},
			new AllowedArithmetic
			{
				Root = TheCodeOfTheShellsAsData.Consola, File = "Program.cs", Member = "Act",
				Snippet = "Console.WriteLine($\" entradas del journal : {before} -> {after} ({after - before} escritas)\");",
				Reason = "Diagnostico: la diferencia entre dos ids de entrada del journal. El id es "
					+ "del motor y no un hecho del vehiculo.",
			},
			new AllowedArithmetic
			{
				Root = TheCodeOfTheShellsAsData.Consola, File = "Program.cs", Member = "Measurements",
				Snippet = "total += size;",
				Reason = "Suma tamanos de archivo en disco.",
			},
			new AllowedArithmetic
			{
				Root = TheCodeOfTheShellsAsData.Consola, File = "Program.cs", Member = "ACrashInTheMiddleOfABlock",
				Snippet = "applied++;",
				Reason = "Cuenta mensajes aplicados en esta corrida.",
			},
			new AllowedArithmetic
			{
				Root = TheCodeOfTheShellsAsData.Consola, File = "Program.cs", Member = "WhatAnswers",
				Snippet = "for (int member = 0; member < map.InterfaceMethods.Length; member++)",
				Reason = "Contador de un recorrido por reflexion.",
			},
			new AllowedArithmetic
			{
				Root = TheCodeOfTheShellsAsData.Consola, File = "Program.cs", Member = "TheWholeBlockAgainFromZero",
				Snippet = "if (message == null) { counted.Discarded++; continue; }",
				Reason = "Cuenta de la corrida: cuantos mensajes se descartaron.",
			},
			new AllowedArithmetic
			{
				Root = TheCodeOfTheShellsAsData.Consola, File = "Program.cs", Member = "TheWholeBlockAgainFromZero",
				Snippet = "if (api.Apply(message).Length > 0) { counted.Rejected++; continue; }",
				Reason = "Cuenta de la corrida: cuantos rechazo el dominio.",
			},
			new AllowedArithmetic
			{
				Root = TheCodeOfTheShellsAsData.Consola, File = "Program.cs", Member = "TheWholeBlockAgainFromZero",
				Snippet = "counted.Applied++;",
				Reason = "Cuenta de la corrida: cuantos entraron.",
			},
			// NO ES UNA CUENTA: ES PROSA, Y EL DETECTOR LA LEE COMO CODIGO. Las instructions del
			// servidor MCP viven en un RAW STRING LITERAL ($"""..."""), y el apagado de literales no
			// conoce esa forma -- conoce "...", @"..." y $"..." -- asi que el texto entero llega vivo al
			// detector y 'yyyy-MM-dd' se lee como una resta entre dos identificadores. El punto ciego
			// esta ahora declarado en la cabecera del instrumento y medido en
			// TheDetectorIsBlindToTheShapesItsHeaderNames.
			new AllowedArithmetic
			{
				Root = TheCodeOfTheShellsAsData.Mcp, File = "Program.cs", Member = "Instructions",
				Snippet = "día por separado, en yyyy-MM-dd, porque una duración no se puede cruzar con el calendario de",
				Reason = "Texto de las instructions, adentro de un raw string literal que el detector no "
					+ "apaga. 'yyyy-MM-dd' es el formato en que este modulo dice un dia de calendario, "
					+ "no una resta, y esa linea no ejecuta nada.",
			},
			new AllowedArithmetic
			{
				Root = TheCodeOfTheShellsAsData.Mcp, File = "Tools/TheClockTheAssistantReads.cs",
				Member = "TheInstantThisWasAsked",
				Snippet = "now.Ticks - now.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);",
				Reason = "Normalizacion del borde, la misma forma que InUtc en la puerta: el reloj de "
					+ "la maquina truncado al segundo entero antes de preguntar nada.",
			},
			new AllowedArithmetic
			{
				Root = TheCodeOfTheShellsAsData.Wasm, File = "AppState.cs", Member = "Spaced",
				Snippet = "var spaced = new System.Text.StringBuilder(name.Length + 4);",
				Reason = "Capacidad inicial de un StringBuilder, sobre el largo de un nombre de clase.",
			},
			new AllowedArithmetic
			{
				Root = TheCodeOfTheShellsAsData.Wasm, File = "AppState.cs", Member = "Spaced",
				Snippet = "for (int i = 1; i < name.Length; i++)",
				Reason = "Contador de un recorrido por los caracteres de un nombre.",
			},
			new AllowedArithmetic
			{
				Root = TheCodeOfTheShellsAsData.Wasm, File = "JournalBridge.cs", Member = "PreloadIntoMemfsAsync",
				Snippet = "copied++;",
				Reason = "Cuenta de archivos copiados al sistema de archivos del browser.",
			},
			new AllowedArithmetic
			{
				Root = TheCodeOfTheShellsAsData.Wasm, File = "JournalBridge.cs", Member = "FlushFromMemfsAsync",
				Snippet = "if (result.Ok) written++;",
				Reason = "Cuenta de archivos escritos a la carpeta de la persona.",
			},
			new AllowedArithmetic
			{
				Root = TheCodeOfTheShellsAsData.Wasm, File = "JournalBridge.cs", Member = "FlushFromMemfsAsync",
				Snippet = "+ $\"{failed.Count + written} archivos. {string.Join(\"; \", failed)}\");",
				Reason = "Las dos cuentas de archivos, sumadas para decir sobre cuantos se intento. "
					+ "Infraestructura de disco, no un hecho del vehiculo.",
			},
			new AllowedArithmetic
			{
				Root = TheCodeOfTheShellsAsData.Wasm, File = "JournalBridge.cs", Member = "MemfsState",
				Snippet = "files++;",
				Reason = "Cuenta de archivos del journal en memoria.",
			},
			new AllowedArithmetic
			{
				Root = TheCodeOfTheShellsAsData.Wasm, File = "JournalBridge.cs", Member = "MemfsState",
				Snippet = "bytes += new FileInfo(file).Length;",
				Reason = "Suma de bytes del journal en memoria.",
			},
			new AllowedArithmetic
			{
				Root = TheCodeOfTheShellsAsData.Wasm, File = "Parts/ComoSeLee.cs", Member = "Dia",
				Snippet = "return day.Day + \" de \" + LosMeses[day.Month - 1] + \" de \" + day.Year;",
				Reason = "Pone en base cero el indice del nombre del mes. Es FORMATO, que es el "
					+ "discriminador que las cuatro rebanadas nombran: HowANumberReads dice de si "
					+ "mismo 'Es formato y no una cuenta'.",
			},
			new AllowedArithmetic
			{
				Root = TheCodeOfTheShellsAsData.Wasm, File = "Parts/ComoSeLee.cs", Member = "DiaCorto",
				Snippet = "return day.Day + \" \" + LosMeses[day.Month - 1][..3] + \" \" + day.Year;",
				Reason = "La misma base cero del indice del mes, en la forma corta.",
			},
			new AllowedArithmetic
			{
				Root = TheCodeOfTheShellsAsData.EndPointDeEventos, File = "Program.cs",
				Member = "Concesion.ComoRespuesta",
				Snippet = "expiresIn = (int)Math.Max(0, (Expira - DateTime.UtcNow).TotalSeconds),",
				Reason = "Cuanto le queda de vida a un token del feed simulado. Es del protocolo de "
					+ "la sesion y no del vehiculo.",
			},
			new AllowedArithmetic
			{
				Root = TheCodeOfTheShellsAsData.EndPointDeEventos, File = "Program.cs",
				Member = "ExpirarAccessTokens",
				Snippet = "expirados++;",
				Reason = "Cuenta de tokens vencidos a mano por la ruta de prueba del mock.",
			},
			// LA TRAJO EL MERGE DEL 2026-09-08, y es un cruce y no un descuido: `Revocar` es de
			// fase0/modulos-hospedados-en-ios -- de ahi los parametros salvoGnothi/soloGnothi, que
			// existen para quitarle el acceso a la copia suelta sin tocar el de Gnothi -- y esta
			// prueba con su lista de permitidos es de master. Ninguna de las dos ramas la vio: la
			// linea nunca convivio con el barrido hasta que se juntaron.
			new AllowedArithmetic
			{
				Root = TheCodeOfTheShellsAsData.EndPointDeEventos, File = "Program.cs",
				Member = "Revocar",
				Snippet = "revocadas++;",
				Reason = "Cuenta de sesiones revocadas por la ruta de prueba del mock. Es del "
					+ "protocolo de la sesion y no del vehiculo, igual que expirados++.",
			},
			new AllowedArithmetic
			{
				Root = TheCodeOfTheShellsAsData.EndPointDeEventos, File = "Program.cs", Member = "Agregar",
				Snippet = "long siguiente = (lista.Count == 0 ? 100 : lista.Max(e => e.Id)) + 1;",
				Reason = "La numeracion de eventos del mock. El numero es del feed, y el modulo lo "
					+ "recibe: nadie lo compone a partir de un hecho del vehiculo.",
			},
			new AllowedArithmetic
			{
				Root = TheCodeOfTheShellsAsData.InyectorDeEventosDemo, File = "Program.cs", Member = "Arg",
				Snippet = "for (int i = 0; i < args.Length - 1; i++)",
				Reason = "Recorrido de la linea de comandos.",
			},
			new AllowedArithmetic
			{
				Root = TheCodeOfTheShellsAsData.InyectorDeEventosDemo, File = "Program.cs", Member = "Arg",
				Snippet = "if (args[i] == nombre) return args[i + 1];",
				Reason = "El argumento siguiente al nombre de la bandera, en la linea de comandos.",
			},
		};

		[Test]
		[TestCaseSource(nameof(TheFloorOfEachRoot))]
		public void EachRootBroughtEnoughFilesToJudge(string root, int floor)
		{
			string[] files = TheCodeOfTheShellsAsData.EveryFileOf(root);

			Assert.That(files.Length, Is.GreaterThanOrEqualTo(floor),
				"La raiz '" + root + "' trajo " + files.Length + " archivos y el piso es " + floor
				+ ". Un barrido que leyo de menos esta verde por no haber mirado, que es la forma "
				+ "exacta en que un catch alrededor de una lectura deja una afirmacion vacia.");
		}

		[Test]
		[TestCaseSource(nameof(TheFileEachRootIsNamedBy))]
		public void EachRootBroughtTheFileItIsNamedBy(string root, string named)
		{
			var arrived = new List<string>();

			foreach (string file in TheCodeOfTheShellsAsData.EveryFileOf(root))
				arrived.Add(TheCodeOfTheShellsAsData.InsideItsRoot(root, file));

			Assert.That(arrived, Contains.Item(named),
				"'" + named + "' no llego a la raiz '" + root + "'. El piso de arriba se satisface "
				+ "con cualquier archivo; esto pide EL archivo, que es lo que separa un glob que "
				+ "trajo lo que debia de uno que trajo algo.");
		}

		// LA LEY. El conjunto de lineas con aritmetica en las siete raices juzgadas es EXACTAMENTE la
		// lista escrita arriba.
		[Test]
		public void TheJudgedRootsHoldExactlyTheArithmeticThatIsWrittenDown()
		{
			var found = new List<ArithmeticHit>();

			foreach (string root in TheCodeOfTheShellsAsData.TheJudgedRoots())
				found.AddRange(TheCodeOfTheShellsAsData.TheArithmeticIn(root));

			var keys = new List<string>();
			foreach (ArithmeticHit hit in found)
				if (!keys.Contains(hit.Key())) keys.Add(hit.Key());

			var allowed = new List<string>();
			foreach (AllowedArithmetic each in TheArithmeticThatIsAllowed)
				allowed.Add(each.Key());

			Assert.That(keys, Is.EquivalentTo(allowed), TheWholeSweep(found, allowed));
		}

		// EL AUTOCONTROL DEL DETECTOR. Sin esto, una lista de permitidos vacia y un detector que no
		// detecta nada son el mismo verde.
		[Test]
		public void TheDetectorSeesEveryShapeTheConventionIsAbout()
		{
			ItSees("a.cs", "int gap = Stands.Odometer - Stands.DeliveryReading;");
			ItSees("Inicio.razor", "<span>@(Stands.Odometer - Stands.DeliveryReading)</span>");
			ItSees("a.cs", "int p = (int)Math.Round(100.0 * AlDia / Nueve.Count);");
			ItSees("a.cs", "total += row.Amount;");
			ItSees("a.cs", "written++;");
			ItSees("a.cs", "string said = $\"{failed.Count + written} archivos\";");
			ItSees("a.js", "h: Math.round(16 + (x.km / maxKm) * 62) + \"px\",");
			ItSees("nginx.conf", "set $gap 3 - 1;");
			ItSees("Inicio.razor", "<span>@(Nueve.Count / Board.Count)</span>");
		}

		[Test]
		public void TheDetectorDoesNotSeeAThingThatMerelyLooksLikeOne()
		{
			ItDoesNotSee("a.cs", "// Stands.Odometer - Stands.DeliveryReading");
			ItDoesNotSee("a.cs", "/// <summary>Lo que falta: Stands.Odometer - Stands.DeliveryReading</summary>");
			ItDoesNotSee("a.cs", "string when = \"2026-06-10\";");
			ItDoesNotSee("a.cs", "string said = \"Te faltan \" + kilometres;");
			ItDoesNotSee("Inicio.razor", "<div class=\"d-bar__fill d-bar__fill--ok\" id=\"cuenta-al-dia\"></div>");
			ItDoesNotSee("Inicio.razor", "<span class=\"@(\"d-pill d-pill--\" + Tono)\">@Titular</span>");
			ItDoesNotSee("Inicio.razor", "role=\"img\" aria-label=\"Sotaventos Motors\" focusable=\"false\">");
			ItDoesNotSee("nginx.conf", "proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;");
			ItDoesNotSee("a.js", "/* aca habria un x - y y no cuenta */");
			ItDoesNotSee("a.cs", "return $\"{Kind} hecho el {CompletedAt:yyyy-MM-dd}\";");
		}

		// LOS PUNTOS CIEGOS, AFIRMADOS. Uno declarado y no medido es una promesa; medido es un limite,
		// y quien escriba la proxima rebanada sabe cual de las formas el barrido no va a atajar.
		[Test]
		public void TheDetectorIsBlindToTheShapesItsHeaderNames()
		{
			ItDoesNotSee("a.cs", "int gap = Stands.Odometer\n\t\t- Stands.DeliveryReading;");
			ItDoesNotSee("a.js", "const gap = nextService-odometer;");
			ItDoesNotSee("Inicio.razor", "<span>@(nextService-odometer)</span>");
			ItDoesNotSee("a.cs", "for (int i = n; i > 0; --i) { }");

			// EL QUINTO PUNTO CIEGO, Y ES AL REVES QUE LOS CUATRO DE ARRIBA: el detector NO ve una
			// cuenta que ellos esconden, y aca VE una que no existe. Un raw string literal ("""...""")
			// no se apaga -- el apagado conoce "...", @"..." y $"..." -- asi que su texto llega vivo, y
			// cualquier palabra con guion adentro se lee como una resta. Se paga con un renglon en la
			// lista de permitidos, que es lo que hoy tiene la unica de este modulo: las instructions
			// del servidor MCP.
			ItSees("a.cs", "static string Says() => $\"\"\"\n\ten yyyy-MM-dd, y nada mas\n\t\"\"\";");

			// Y EL CONTROL QUE LO CONVIERTE EN UNA MEDICION: EL MISMO TEXTO, en un literal de los que
			// el detector si apaga, no se ve. La diferencia entre los dos renglones es la comilla, y
			// nada mas.
			ItDoesNotSee("a.cs", "string says = \"en yyyy-MM-dd, y nada mas\";");

			// Y EL CONTROL DE LOS CUATRO DE ARRIBA: la misma cuenta en una sola linea, la misma
			// pegada pero en C#, y la misma con el otro operador en un .razor, si se ven. Sin este
			// renglon lo de arriba estaria verde aunque el detector no viera nada de nada.
			ItSees("a.cs", "int gap = Stands.Odometer - Stands.DeliveryReading;");
			ItSees("a.cs", "int gap = nextService-odometer;");
			ItSees("a.js", "const gap = nextService - odometer;");
			ItSees("Inicio.razor", "<span>@(nextService/odometer)</span>");
			ItSees("a.cs", "for (int i = n; i > 0; i--) { }");
		}

		// LA PAGINA DE DESCARGA SE ENUMERA. Es hosting: nginx no lee ningun valor del dominio, y en un
		// archivo de configuracion una barra es un separador de camino y no un operador. Lo que se
		// afirma es que lo que aparece ahi es exactamente eso.
		[Test]
		public void TheDownloadPageHoldsOnlyDirectivesAndTheirPaths()
		{
			List<ArithmeticHit> found =
				TheCodeOfTheShellsAsData.TheArithmeticIn(TheCodeOfTheShellsAsData.PaginaDeDescarga);

			var keys = new List<string>();
			foreach (ArithmeticHit hit in found)
				if (!keys.Contains(hit.Key())) keys.Add(hit.Key());

			var allowed = new List<string>();
			foreach (AllowedArithmetic each in TheDownloadPagesDirectives)
				allowed.Add(each.Key());

			Assert.That(keys, Is.EquivalentTo(allowed), TheWholeSweep(found, allowed));
		}

		// PLAN-BuildOrder.md §2.2 PREDIJO CERO ACIERTOS AQUI Y LA PREDICCION ES FALSA, medido: son
		// diez, y los diez son una directiva de nginx seguida de un camino. No hay forma de afinar el
		// detector que los quite sin quitar tambien una division de verdad: "root /usr/share" y
		// "a / b" son la misma forma. Quedan enumerados uno por uno, que es lo que hace que un
		// archivo NUEVO en esta raiz se ponga rojo y alguien lo lea.
		private static readonly AllowedArithmetic[] TheDownloadPagesDirectives =
		{
			Nginx("ssl_certificate", "ssl_certificate_key /etc/nginx/certs/localhost.key;"),
			Nginx("location = /", "root /usr/share/nginx/descarga;"),
			Nginx("location = /", "try_files /index.html =404;"),
			Nginx("server", "location = /app { return 301 /app/; }"),
			Nginx("server", "location /app/eventos/ {"),
			Nginx("location = /app/service-worker.js", "alias /usr/share/nginx/app/service-worker.js;"),
			Nginx("location = /app/service-worker.js", "add_header Service-Worker-Allowed /app/;"),
			Nginx("location = /app/service-worker-assets.js", "alias /usr/share/nginx/app/service-worker-assets.js;"),
			Nginx("server", "location /app/ {"),
			Nginx("location /app/", "alias /usr/share/nginx/app/;"),
		};

		private static AllowedArithmetic Nginx(string member, string snippet)
		{
			return new AllowedArithmetic
			{
				Root = TheCodeOfTheShellsAsData.PaginaDeDescarga,
				File = "nginx.conf",
				Member = member,
				Snippet = snippet,
				Reason = "Una directiva de nginx y su camino. En un archivo de configuracion una "
					+ "barra separa un camino y un guion es parte del nombre de una cabecera; un "
					+ "detector de expresiones no puede juzgar eso. Y no tiene que: nginx no lee "
					+ "ningun valor del dominio, porque no entra por la puerta.",
			};
		}

		// LA MAQUETA YA NO SE BARRE. Estuvo enumerada al reves -- sus cuentas afirmadas PRESENTES, como
		// registro de lo que la caratula se niega a copiar -- y se borro del modulo por obsoleta. La
		// raiz no podia quedar nombrada aca sin la carpeta: la carpeta es gitignorada, asi que en un
		// clon nuevo nunca llego, y cinco pruebas de estas dos fixtures se ponian rojas en master por
		// una carpeta que solo existia en la maquina de quien la diseno.
		//
		// LO QUE SE FUE CON ELLA ES EL SUJETO, NO LA LEY. Que la caratula no construya la resta del
		// heroe ni la division de las alturas de las barras lo sigue afirmando, sin necesitar la
		// maqueta, TheJudgedRootsHoldExactlyTheArithmeticThatIsWrittenDown: ninguna de las dos formas
		// esta en la lista de permitidos, asi que escribirla en un .razor pone rojo ese renglon. Y las
		// dos siguen siendo lo que el detector se prueba a si mismo en
		// TheDetectorSeesEveryShapeTheConventionIsAbout, donde son un texto de la prueba y no un
		// archivo que tenga que llegar.

		private static void ItSees(string file, string source)
		{
			Assert.That(
				TheCodeOfTheShellsAsData.TheArithmeticInSource("probe", file, source),
				Is.Not.Empty,
				"El detector no vio '" + source + "'. Un barrido que no ve esta forma esta verde por "
				+ "no detectar, no por no haber.");
		}

		private static void ItDoesNotSee(string file, string source)
		{
			Assert.That(
				TheCodeOfTheShellsAsData.TheArithmeticInSource("probe", file, source),
				Is.Empty,
				"El detector vio '" + source + "', que no es una cuenta con un valor del dominio. "
				+ "Cada falso positivo se paga con un renglon en la lista de permitidos, y una lista "
				+ "de ruido deja de leerse.");
		}

		/// <summary>
		/// El barrido entero, impreso. C71: lo que se reporta son las coincidencias, no cuantas hay.
		/// </summary>
		private static string TheWholeSweep(List<ArithmeticHit> found, List<string> allowed)
		{
			var said = new StringBuilder();
			said.Append("EL BARRIDO, COMPLETO:\n");

			foreach (ArithmeticHit hit in found)
			{
				said.Append(allowed.Contains(hit.Key()) ? "  ok   " : "  NUEVO ");
				said.Append(hit.ToString());
				said.Append('\n');
			}

			said.Append("PERMITIDOS QUE YA NO APARECEN:\n");

			foreach (string each in allowed)
			{
				bool still = false;

				foreach (ArithmeticHit hit in found)
					if (hit.Key() == each) still = true;

				if (!still) said.Append("  SOBRA ").Append(each).Append('\n');
			}

			return said.ToString();
		}
	}

	/// <summary>
	/// Una linea de una caratula con un operador aritmetico que se permite, con el miembro sobre el
	/// que esta y la razon por la que es formato o infraestructura y no una cuenta con un valor del
	/// dominio.
	/// </summary>
	internal sealed class AllowedArithmetic
	{
		internal string Root { get; set; }

		internal string File { get; set; }

		internal string Member { get; set; }

		internal string Snippet { get; set; }

		internal string Reason { get; set; }

		internal string Key()
		{
			return Root + " | " + File + " | " + Snippet;
		}
	}
}
