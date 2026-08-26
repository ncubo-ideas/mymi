using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DealershipCar.App;
using Puppeteer;
using Puppets.Portador;

namespace DealershipCar.Consola
{
	// LA CARATULA DE CONSOLA. El MISMO actor y los MISMOS performs que va a usar el browser: lo
	// unico que cambia es como llegan las entradas -- argv en vez de clicks -- y donde va la salida.
	//
	// EXISTE PARA MEDIR EL DOMINIO CONTRA UN DISCO DE VERDAD, sin el sandbox del browser encima:
	// ids de entrada del journal, conteo de bytes, y una corrida de muestra determinista.
	//
	// SE ENTRA POR LA PUERTA, siempre. Todo pasa por DealershipCarAPI. La UNICA excepcion es
	// app.Actor.CurrentEntryId y leer los bytes de la carpeta, que son mediciones que la puerta no
	// expone -- ni debe: son del sustrato, no del negocio.
	//
	// NO OFRECE LOS CUATRO COMANDOS QUE CICATRIZAN, y es a proposito. Un perfil retroactivo, una
	// completacion de una clase no agendada, una lectura que va para atras y una revision anterior a
	// la que rige revientan ADENTRO del comando, y el motor ya escribio la entrada antes de correr
	// el cuerpo: la entrada queda para siempre y hace loguear un fallo de rehidratacion en cada
	// arranque futuro (C22, medido en T6). Esta caratula es un instrumento de medicion y un guion,
	// no un formulario de captura: no le da a nadie la forma de dejar esa cicatriz. El dia que haya
	// entrada libre -- T8 la trae, con hechos que manda la agencia -- la guarda tiene que estar en
	// la frontera, y eso necesita que la fachada proyecte lo que cada guarda mira. No es de aca.
	internal static class Program
	{
		private static int Main(string[] args)
		{
			try
			{
				// SIN ARGUMENTOS CUENTA LA HISTORIA. Es el criterio de §T7: "dotnet run imprime una
				// historia de seis meses reproducible desde un journal vacio". La ayuda se pide.
				switch (args.Length == 0 ? "historia" : args[0])
				{
					case "historia": return TheSixMonthStory(AFolder(args, 1));
					case "estado":   return TheBoardFromTheJournal(AFolder(args, 1));
					case "refresco": return WhatTheRefreshLoopStillNeeds();
					default:         Help(); return 1;
				}
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error: {ex.GetType().Name}: {ex.Message}");
				for (Exception inner = ex.InnerException; inner != null; inner = inner.InnerException)
					Console.Error.WriteLine($"  causa: {inner.GetType().FullName}: {inner.Message}");
				return 2;
			}
		}

		// Abre el puppet sobre una carpeta real. Es el mismo par ConfigureStorage + Start que va a
		// hacer la caratula del browser, solo que alla el path apunta a MEMFS.
		private static DealershipCarApp Open(string folder)
		{
			Directory.CreateDirectory(folder);

			var app = new DealershipCarApp(Story.TheVin, Story.TheProfileAtDelivery);
			app.ConfigureStorage(DatabaseType.FileSystem, "path=" + folder);
			app.Start();
			return app;
		}

		// ---------------------------------------------------------------- la historia

		private static int TheSixMonthStory(string folder)
		{
			Console.WriteLine("DealershipCar - la historia de seis meses, desde un journal vacio");
			Console.WriteLine();
			Console.WriteLine($"folder    : {folder}");
			Console.WriteLine($"vin       : {Story.TheVin}");
			Console.WriteLine($"actor     : {DealershipCarApp.TheActorFor(Story.TheVin)}");

			// DESDE UN JOURNAL VACIO, literalmente: se borra el del vehiculo antes de abrirlo. Solo
			// esa subcarpeta -- la que este proceso esta por escribir -- y nunca la carpeta entera,
			// que es de quien la eligio. Se hace ANTES de abrir y en un proceso nuevo, que es la
			// unica ventana en que se puede: Performance.Dispose() no suelta el archivo del journal
			// mientras el proceso vive (medido en T6).
			//
			// SE INFORMA LA POSTCONDICION Y NO SI HABIA ALGO, y no es pudor: "habia uno anterior" es
			// cierto en la segunda corrida y falso en la primera, asi que imprimirlo haria que dos
			// corridas de la misma historia difirieran en una linea. Lo que la historia afirma es que
			// arranca vacia, y eso es igual siempre.
			EmptyTheJournalOfThisVehicle(folder);
			Console.WriteLine("journal   : vaciado - esta historia SIEMPRE borra el journal de este");
			Console.WriteLine("            vehiculo y arranca de cero, para poder ser reproducible");

			DealershipCarApp app = Open(folder);
			var api = new DealershipCarAPI(app);

			Console.WriteLine($"entrada 0 : {app.Actor.CurrentEntryId}  (la semilla, que deja OnHydrated)");
			Console.WriteLine();

			Act(app, "1. llega el plan del fabricante, y la persona se agrega una operacion",
				() => Story.ThePlanArrives(api));

			// EL TABLERO ANTES DE QUE PASE NADA, y esta aca por dos razones. Cuenta mejor la
			// historia -- se ve el plan como llego -- y es el unico momento en que las dos lecturas
			// GUARDADAS contestan su lado vacio: HasBeenEvaluated es false y HasAnythingDue tambien.
			// Sin este tablero, preguntarlas antes de ReadNextDueKind y de LastEvaluatedAt seria
			// ceremonia que nada ejercita.
			PrintTheBoard(api, TheBoard(api), null);

			// SE IMPRIME EL ORDEN DE LLEGADA, y hay una razon para gastar seis lineas en el. El
			// dominio contesta lo MISMO lleguen ordenadas o no -- ordena por instante observado
			// (OINV-54) -- asi que si no se dice que se mando, que hayan llegado desordenadas no se
			// ve en ninguna parte del tablero. Decirlo es lo unico que hace comprobable la unica
			// afirmacion que esta caratula puede hacer al respecto.
			Console.WriteLine("2. seis meses de odometro. LLEGAN DESORDENADAS: la de diciembre entra");
			Console.WriteLine("   antes que la de noviembre, y la de marzo antes que la de febrero.");
			foreach ((int value, string unit, DateTime observedAt, string source)
				in Story.TheReadingsInTheOrderTheyArrive())
				Console.WriteLine($"     llega {value,6} {unit}  observada el {Day(observedAt)}  de {source}");
			Console.WriteLine();

			Act(app, "   aplicadas al dominio, que las ordena por instante observado",
				() => Story.TheOdometerReportsForSixMonths(api));

			// -------- la primera evaluacion
			Console.WriteLine($"3. primera evaluacion, al {Day(Story.TheFirstEvaluation)}");
			long beforeFirst = app.Actor.CurrentEntryId;
			EvaluationOutcome first = api.Evaluate(Story.TheFirstEvaluation);
			Report(first);
			Console.WriteLine($"   entradas del journal        : {beforeFirst} -> {app.Actor.CurrentEntryId}");
			Console.WriteLine();

			Dictionary<string, string> before = TheBoard(api);
			PrintTheBoard(api, before, null);

			Act(app, "4. la persona aplaza el Diagnostic por costo, y declara servicio severo",
				() => Story.ThePersonAnswersTheFirstEvaluation(api));
			Act(app, "5. el taller informa un cruce de umbral en la Inspection",
				() => Story.AConditionThresholdIsCrossed(api));

			// -------- EL SALTO
			Console.WriteLine("================================================================");
			Console.WriteLine($"  SEIS MESES EN UN SOLO COMANDO: Evaluate({Day(Story.SixMonthsLater)})");
			Console.WriteLine();
			Console.WriteLine("  No hay espera y no hay reloj. El dominio nunca lee la hora: recibe");
			Console.WriteLine("  el instante como parametro, asi que seis meses es un argumento.");
			Console.WriteLine("  Entre esta evaluacion y la anterior NO llego ningun hecho nuevo del");
			Console.WriteLine("  odometro: lo que se mueva, se movio solo por el paso del tiempo.");
			Console.WriteLine("================================================================");
			Console.WriteLine();

			long beforeJump = app.Actor.CurrentEntryId;
			EvaluationOutcome jump = api.Evaluate(Story.SixMonthsLater);
			Report(jump);
			Console.WriteLine($"   entradas del journal        : {beforeJump} -> {app.Actor.CurrentEntryId}");
			Console.WriteLine();

			Dictionary<string, string> after = TheBoard(api);
			PrintTheBoard(api, after, before);

			Measurements(app, api, folder);

			app.Dispose();
			return 0;
		}

		// Un acto del guion, con la entrada del journal antes y despues: cuantas entradas costo se
		// mide, no se supone.
		private static void Act(DealershipCarApp app, string title, Action act)
		{
			long before = app.Actor.CurrentEntryId;
			act();
			long after = app.Actor.CurrentEntryId;

			Console.WriteLine($"{title}");
			Console.WriteLine($"   entradas del journal     : {before} -> {after}  ({after - before} escritas)");
			Console.WriteLine();
		}

		// Lo que la evaluacion contesto. WasEvaluated es el hecho que el dominio no puede saber: si
		// la guarda de cadencia la dejo correr (C6).
		private static void Report(EvaluationOutcome outcome)
		{
			if (!outcome.WasEvaluated)
			{
				Console.WriteLine($"   RECHAZADA                : {Unwrapped(outcome.Rejection)}");
				return;
			}

			Console.WriteLine($"   operaciones que se movieron : {outcome.TransitionCount}");
			Console.WriteLine($"   algo paso a vencido         : {outcome.AnythingBecameDue}");
			Console.WriteLine($"   algo paso de la gracia      : {outcome.AnythingBecameOverdue}");
		}

		// ---------------------------------------------------------------- el tablero

		private static Dictionary<string, string> TheBoard(DealershipCarAPI api)
		{
			var board = new Dictionary<string, string>();

			foreach (string kind in Story.TheNineKinds) board[kind] = api.ReadDueState(kind);

			return board;
		}

		// El tablero, y -- si se le pasa el anterior -- QUE SE MOVIO. El motivo lo compone el
		// dominio y no esta vista (OUS-07): se imprime tal cual sale de ReadDueReason.
		private static void PrintTheBoard(
			DealershipCarAPI api, Dictionary<string, string> board, Dictionary<string, string> before)
		{
			Console.WriteLine($"   vin leido del journal    : {api.ReadVin()}");
			Console.WriteLine($"   evaluada hasta           : {(api.HasBeenEvaluated() ? Day(api.LastEvaluatedAt()) : "nunca")}");
			Console.WriteLine();

			foreach (string kind in Story.TheNineKinds)
			{
				string state = board[kind];

				// Las clases que nadie agendo contestan NotDue -- una lectura TOTAL, no una ausencia
				// (E41) -- y se saltean porque son cuatro de nueve y no cuentan nada.
				if (!ItIsScheduled(api, kind)) continue;

				string moved = before != null && before[kind] != state
					? $"   {before[kind]} -> {state}"
					: "";

				Console.WriteLine($"   {kind,-20} {state,-10}{moved}");
				Console.WriteLine($"   {"",-20} {api.ReadDueReason(kind)}");

				int deferrals = api.DeferralsUsed(kind);
				if (deferrals > 0)
					Console.WriteLine($"   {"",-20} aplazamientos usados: {deferrals} de 2");

				Console.WriteLine();
			}

			Console.WriteLine($"   vencidas o pasadas       : {api.CountDue()}");
			Console.WriteLine($"   pasadas de la gracia     : {api.CountOverdue()}");
			Console.WriteLine($"   cuentan como no hechas   : {api.MissedCount()}");
			Console.WriteLine($"   hay algo vencido         : {api.HasAnythingDue()}");

			// GUARDADA: sin nada vencido revienta, y la que avisa es HasAnythingDue (E41).
			Console.WriteLine($"   lo primero que hacer     : {(api.HasAnythingDue() ? api.ReadNextDueKind() : "nada")}");
			Console.WriteLine();
		}

		// Una clase que el plan no trajo contesta con la frase que lo dice. Es la unica forma que
		// tiene esta caratula de distinguir "no agendada" de "agendada y tranquila", porque el
		// estado es NotDue en los dos casos -- que es E41 funcionando, no un hueco.
		private static bool ItIsScheduled(DealershipCarAPI api, string kind)
		{
			return !api.ReadDueReason(kind).StartsWith("There is no scheduled", StringComparison.Ordinal);
		}

		// ---------------------------------------------------------------- mediciones

		// Lo que solo se puede medir contra un disco de verdad. Tres de estas cuatro son criterios
		// que ya defiende una prueba de nivel B; se vuelven a medir ACA porque una prueba corre en
		// una carpeta temporal y esto corre donde la persona guarda su vehiculo.
		private static void Measurements(DealershipCarApp app, DealershipCarAPI api, string folder)
		{
			Console.WriteLine("---- mediciones contra el disco -------------------------------");
			Console.WriteLine();

			string actorFolder = Path.Combine(folder, DealershipCarApp.TheActorFor(Story.TheVin));

			Console.WriteLine($"entrada final del journal   : {app.Actor.CurrentEntryId}");
			long total = 0;
			foreach (string file in TheFilesOf(actorFolder))
			{
				long size = new FileInfo(file).Length;
				total += size;
				Console.WriteLine($"  {Path.GetFileName(file),-24} {size,8} bytes");
			}
			Console.WriteLine($"  {"total",-24} {total,8} bytes");
			Console.WriteLine();

			// 1. RE-EVALUAR UN INSTANTE YA EVALUADO NO ESCRIBE. Es la cadencia de C6 y es lo unico
			//    que hace barato abrir la app todos los dias. Aca "byte a byte" SI se sostiene, y es
			//    la forma fuerte del criterio: se compara antes y despues de algo que no debe
			//    escribir nada (C23).
			string bytesBefore = TheTreeOf(actorFolder);
			long entryBefore = app.Actor.CurrentEntryId;
			EvaluationOutcome again = api.Evaluate(Story.SixMonthsLater);

			Console.WriteLine("re-evaluar el mismo instante:");
			Console.WriteLine($"  se evaluo                 : {again.WasEvaluated}");
			Console.WriteLine($"  motivo del rechazo        : {Unwrapped(again.Rejection)}");
			Console.WriteLine($"  entrada del journal       : {entryBefore} -> {app.Actor.CurrentEntryId}");
			Console.WriteLine($"  arbol byte a byte igual   : {TheTreeOf(actorFolder) == bytesBefore}");
			Console.WriteLine();

			// 2. UN CODIGO QUE NO NOMBRA NINGUN MIEMBRO SE RECHAZA EN LA PUERTA, antes de escribir.
			//    Si llegara al script, el motor escribiria la entrada y RECIEN despues reventaria:
			//    una cicatriz permanente por un dropdown mal escrito (C22, C25).
			bytesBefore = TheTreeOf(actorFolder);
			entryBefore = app.Actor.CurrentEntryId;
			string refusal = WhatTheDoorSaysAbout(() => api.ReadDueState("fluidchange"));

			Console.WriteLine("un codigo que el canon no escribe ('fluidchange'):");
			Console.WriteLine($"  la puerta contesta        : {refusal}");
			Console.WriteLine($"  entrada del journal       : {entryBefore} -> {app.Actor.CurrentEntryId}");
			Console.WriteLine($"  arbol byte a byte igual   : {TheTreeOf(actorFolder) == bytesBefore}");
			Console.WriteLine();

			// 3. EL OTRO LADO DE LA MISMA GUARDA: un instante POSTERIOR si pasa, y SI escribe. Esta
			//    aca por dos motivos y el segundo pesa mas que el primero. Muestra que la guarda de
			//    cadencia deja pasar lo que tiene que dejar pasar -- rechazar todo tambien daria
			//    "no escribio" en las dos mediciones de arriba. Y sobre todo: es lo que hace
			//    FALSIFICABLE a la comparacion de arbol. Un TheTreeOf que devolviera una constante
			//    dejaria en verde para siempre las dos de arriba; esta se pone roja.
			bytesBefore = TheTreeOf(actorFolder);
			entryBefore = app.Actor.CurrentEntryId;
			EvaluationOutcome later = api.Evaluate(Story.SixMonthsLater.AddDays(1));

			Console.WriteLine("evaluar un instante POSTERIOR:");
			Console.WriteLine($"  se evaluo                 : {later.WasEvaluated}");
			Console.WriteLine($"  entrada del journal       : {entryBefore} -> {app.Actor.CurrentEntryId}");
			Console.WriteLine($"  arbol byte a byte igual   : {TheTreeOf(actorFolder) == bytesBefore}   <- tiene que ser False");
			Console.WriteLine();

			// 4. CADA Start() DEJA LA ENTRADA DE LA SEMILLA, aunque el upgrade ya este aplicado y no
			//    haga nada. Se mide aca porque es lo que obliga a T9 a dejar el OnHydrated del
			//    servidor MCP vacio: con la semilla puesta, cada pregunta de la IA escribiria en el
			//    journal de la persona.
			Console.WriteLine("cerrar y reabrir sobre el mismo journal:");
			Console.WriteLine($"  entrada antes de cerrar   : {app.Actor.CurrentEntryId}");
			Console.WriteLine("  (la entrada despues de reabrir se mide con el subcomando 'estado',");
			Console.WriteLine("   que corre en un proceso NUEVO: eso es lo que prueba el replay)");
			Console.WriteLine();
		}

		// ---------------------------------------------------------------- el replay

		// Se corre en un proceso NUEVO. Todo lo que salga aca vino del journal: la agenda es una
		// global de raiz del DSL, no un campo de C#, y un campo estaria vacio despues de rehidratar.
		private static int TheBoardFromTheJournal(string folder)
		{
			Console.WriteLine("DealershipCar - el mismo journal, reabierto en un proceso nuevo");
			Console.WriteLine();
			Console.WriteLine($"folder    : {folder}");

			DealershipCarApp app = Open(folder);
			var api = new DealershipCarAPI(app);

			Console.WriteLine($"entrada del journal      : {app.Actor.CurrentEntryId}");
			Console.WriteLine("  (crecio una: OnHydrated deja la entrada de la semilla en CADA");
			Console.WriteLine("   arranque, aunque upgrade('semilla') ya este aplicado y no haga nada)");
			Console.WriteLine();

			PrintTheBoard(api, TheBoard(api), null);

			app.Dispose();
			return 0;
		}

		// ---------------------------------------------------------------- el bucle de refresh

		// LO QUE FALTA PARA QUE BucleDeRefresh CORRA, medido y no opinado. §T7 pide cablearlo con
		// renovacion de credencial y flush nulo; el flush nulo es correcto y es el default -- el
		// journal de esta consola YA esta en un disco de verdad, asi que no hay nada que bajar --
		// pero los tres argumentos OBLIGATORIOS del constructor son artefactos de T8, y dos de ellos
		// necesitan superficie de dominio que no existe. Se deja MEDIDO en vez de escrito: el dia
		// que T8 la agregue, esto lo dice solo.
		private static int WhatTheRefreshLoopStillNeeds()
		{
			Console.WriteLine("DealershipCar - que le falta al BucleDeRefresh para poder correr");
			Console.WriteLine();
			Console.WriteLine("BucleDeRefresh(diario, fuente, ruta, flush, renovarCredencial)");
			Console.WriteLine();
			Console.WriteLine($"  flush              : null, y es lo correcto para esta caratula.");
			Console.WriteLine("                       El flush existe para bajar de MEMFS a la carpeta");
			Console.WriteLine("                       del usuario en wasm. Aca el journal ya esta en");
			Console.WriteLine("                       disco: no hay nada que bajar.");
			Console.WriteLine();
			Console.WriteLine("  fuente             : IFuenteDeEventos - la API de eventos de la agencia.");
			Console.WriteLine("                       No existe todavia; la trae T8.");
			Console.WriteLine("  ruta               : Ruta - senal cruda -> IMensajeDeEvento.");
			Console.WriteLine("                       No existe todavia; la trae T8.");
			Console.WriteLine();
			Console.WriteLine("  diario             : IDiarioDeLaApp, y aca esta lo que no es solo");
			Console.WriteLine("                       cuestion de escribirlo. DealershipCarAPI no lo");
			Console.WriteLine("                       implementa, y estos son sus miembros:");
			Console.WriteLine();

			// Se pregunta por reflexion contra los dos tipos de verdad, no contra una lista escrita
			// a mano: una lista se queda vieja en silencio y esto no puede.
			//
			// EL CONTROL VA PRIMERO, y no es adorno. Hoy los tres miembros del diario dan "NO ESTA"
			// contra CUALQUIER tipo, asi que tres respuestas negativas no prueban que se le este
			// preguntando a la puerta: probarian lo mismo preguntandole a esta misma clase. El
			// control pregunta por un miembro que la puerta SI tiene, y es lo que distingue "no lo
			// implementa" de "estoy mirando el tipo equivocado".
			Console.WriteLine($"    {"(control) Evaluate",-24} {WhetherTheDoorAnswers("Evaluate")}");
			Console.WriteLine();

			foreach (MethodInfo required in typeof(IDiarioDeLaApp).GetMethods())
				Console.WriteLine($"    {required.Name,-24} {WhetherTheDoorAnswers(required.Name)}");

			Console.WriteLine();
			Console.WriteLine("  Aplicar() es cableado: rutea un mensaje tipado a un comando que la");
			Console.WriteLine("  puerta YA tiene. Se escribe en T8 y no necesita nada nuevo.");
			Console.WriteLine();
			Console.WriteLine("  Los otros dos no se escriben y ya. UltimoEventoProcesado() tiene que");
			Console.WriteLine("  salir DEL JOURNAL -- es lo que hace exacto el resume tras una caida --");
			Console.WriteLine("  y TokenGuardado() es una credencial guardada. MaintenanceSchedule no");
			Console.WriteLine("  lleva ni checkpoint de eventos ni credencial: no hay de donde");
			Console.WriteLine("  contestarlos sin cambiar el dominio, y el dominio esta completo en T6.");
			Console.WriteLine("  Un adaptador de esta consola que los contestara de memoria seria peor");
			Console.WriteLine("  que no tenerlo: se veria igual que el de verdad y perderia eventos en");
			Console.WriteLine("  el primer corte.");
			Console.WriteLine();
			Console.WriteLine("  Es el hallazgo C26. Lo cierra T8, que es donde la agenda gana su");
			Console.WriteLine("  checkpoint y su credencial junto con los hechos que las necesitan.");
			return 0;
		}

		private static string WhetherTheDoorAnswers(string member)
		{
			return typeof(DealershipCarAPI).GetMethod(
				member, BindingFlags.Public | BindingFlags.Instance) != null
				? "la puerta ya lo contesta"
				: "NO ESTA en la puerta";
		}

		// ---------------------------------------------------------------- utilidades

		// El motivo del rechazo viene envuelto -- {"EWI":[{"Error":"..."}]} -- y la puerta lo deja
		// crudo a proposito: desenvolver es de la vista (medido, SPIKES-OS2 S3). Esta ES la vista.
		private static string Unwrapped(string rejection)
		{
			if (string.IsNullOrEmpty(rejection)) return "(sin rechazo)";

			try
			{
				using (JsonDocument envelope = JsonDocument.Parse(rejection))
				{
					JsonElement ewi = envelope.RootElement.GetProperty("EWI");

					foreach (JsonElement each in ewi.EnumerateArray())
						if (each.TryGetProperty("Error", out JsonElement error)) return error.GetString();
				}
			}
			catch (JsonException)
			{
				// Un sobre que no es el que S3 midio se muestra tal cual. Inventarle una forma seria
				// esconder justo el caso que hay que ver.
			}

			return rejection;
		}

		// Lo que la puerta dice cuando rechaza. Se atrapa ArgumentException y no Exception: es la
		// mitad "defecto de quien llama" de E32, en la frontera que C3 le asigno al BCL.
		private static string WhatTheDoorSaysAbout(Action call)
		{
			try
			{
				call();
			}
			catch (ArgumentException refused)
			{
				return $"{refused.GetType().Name} sobre '{refused.ParamName}' - {FirstLineOf(refused.Message)}";
			}

			return "NO LO RECHAZO (y deberia)";
		}

		private static string FirstLineOf(string message)
		{
			int end = message.IndexOf(" Los que hay son:", StringComparison.Ordinal);

			return end < 0 ? message : message.Substring(0, end);
		}

		// Borra el journal DE ESTE VEHICULO, y nada mas. La carpeta que rodea es de quien la eligio.
		private static bool EmptyTheJournalOfThisVehicle(string folder)
		{
			string actorFolder = Path.Combine(folder, DealershipCarApp.TheActorFor(Story.TheVin));
			if (!Directory.Exists(actorFolder)) return false;

			Directory.Delete(actorFolder, true);
			return true;
		}

		private static List<string> TheFilesOf(string folder)
		{
			var files = new List<string>(
				Directory.GetFiles(folder, "*", SearchOption.AllDirectories));
			files.Sort(StringComparer.Ordinal);

			return files;
		}

		// El arbol entero reducido a nombre, largo y hash. El actor tiene el journal ABIERTO
		// mientras vive, asi que hay que pedir el archivo compartido: cerrarlo para poder mirarlo
		// cambiaria lo que se esta midiendo (medido en T6, Performance.Dispose no lo suelta).
		private static string TheTreeOf(string folder)
		{
			var tree = new StringBuilder();

			foreach (string file in TheFilesOf(folder))
			{
				using (var open = new FileStream(
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

		// Con cultura INVARIANTE. Sin ella el calendario de la maquina decide el ano, y la misma
		// historia impresa en otra PC dejaria de ser la misma historia -- que es justo lo unico que
		// esta caratula promete.
		private static string Day(DateTime instant)
		{
			return instant.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
		}

		private static string AFolder(string[] args, int i)
		{
			return Path.GetFullPath(args.Length > i && args[i].Length > 0
				? args[i]
				: Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
					"DealershipCar-journal"));
		}

		private static void Help()
		{
			Console.WriteLine("DealershipCar - caratula de consola, para medir el dominio contra un disco de verdad");
			Console.WriteLine();
			Console.WriteLine("  historia [carpeta]   la historia de seis meses, desde un journal vacio");
			Console.WriteLine("                       (es lo que corre 'dotnet run' sin argumentos)");
			Console.WriteLine("  estado   [carpeta]   reabre ese journal y reporta - correr en proceso NUEVO");
			Console.WriteLine("  refresco             que le falta al BucleDeRefresh para poder correr");
			Console.WriteLine();
			Console.WriteLine("carpeta por defecto: ~/DealershipCar-journal");
			Console.WriteLine();
			Console.WriteLine("Las clases de operacion cruzan como el NOMBRE de su miembro, no como enum:");
			Console.WriteLine("esta caratula no referencia el dominio y no puede nombrar sus tipos (C25).");
			Console.WriteLine($"  {string.Join(", ", Story.TheNineKinds)}");
		}
	}
}
