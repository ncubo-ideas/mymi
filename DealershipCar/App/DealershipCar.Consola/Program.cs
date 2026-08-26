using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DealershipCar.App;
using DealershipCar.App.Events;
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
		private static async Task<int> Main(string[] args)
		{
			try
			{
				// SIN ARGUMENTOS CUENTA LA HISTORIA. Es el criterio de §T7: "dotnet run imprime una
				// historia de seis meses reproducible desde un journal vacio". La ayuda se pide.
				switch (args.Length == 0 ? "historia" : args[0])
				{
					case "historia": return TheSixMonthStory(AFolder(args, 1));
					case "estado":   return TheBoardFromTheJournal(AFolder(args, 1));
					case "refresco": return await TheRefreshLoopAgainstTheDealership(args);
					case "corte":    return await ACrashInTheMiddleOfABlock(args);
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

		// EL REFRESH DE VERDAD: los eventos del concesionario, aplicados de a uno por el mecanismo
		// compartido. Cierra C26, que T7 dejo MEDIDO en vez de escrito.
		//
		// El flush sigue siendo null y sigue siendo lo correcto para esta caratula: existe para
		// bajar de MEMFS a la carpeta del usuario en wasm, y aca el journal ya esta en disco.
		//
		// LA UNICA PIEZA QUE TOCA LA RED ES EL MECANISMO, y el HttpClient lo crea esta caratula --
		// que es el invariante que §7 del plan pide poder grepear. FuenteHttpDeEventos arma UNA sola
		// forma de URL y no tiene otro metodo: la app no es cliente de los servicios generales del
		// concesionario, y el senuelo de mas abajo lo demuestra en vez de afirmarlo.
		private static async Task<int> TheRefreshLoopAgainstTheDealership(string[] args)
		{
			string folder  = AFolder(args, 1);
			string baseUrl = AnArgument(args, 2, "http://localhost:8092/");
			string resource = AnArgument(args, 3, "vehicle");
			string id      = AnArgument(args, 4, Story.TheVin);

			Console.WriteLine("DealershipCar - el refresh contra la API de eventos del concesionario");
			Console.WriteLine();
			Console.WriteLine($"folder    : {folder}");
			Console.WriteLine($"url2      : {baseUrl}{resource}/{id}/events?after=N");
			Console.WriteLine();

			WhatTheRefreshLoopNeededAndWhetherTheDoorAnswersItNow();

			using (var http = new HttpClient { BaseAddress = new Uri(baseUrl) })
			{
				DealershipCarApp app = Open(folder);
				var api = new DealershipCarAPI(app);
				var tokens = new ProveedorDeToken(http);

				await TheAppGetsItsOwnCredential(api, tokens, resource, id);

				var loop = new BucleDeRefresh(
					api,
					new FuenteHttpDeEventos(http, resource, id),
					DealershipEventRoute.Route,
					null,
					ct => RenewTheCredential(api, tokens, resource, id, ct));

				string actorFolder = Path.Combine(folder, DealershipCarApp.TheActorFor(Story.TheVin));

				Console.WriteLine("-- refresh 1 --");
				Console.WriteLine($"   punto de control antes    : {api.LastEventApplied()}");
				long entryBefore = app.Actor.CurrentEntryId;
				ResultadoDeRefresh first = await loop.Ejecutar();
				Console.WriteLine($"   {first}");
				Console.WriteLine($"   entradas del journal      : {entryBefore} -> {app.Actor.CurrentEntryId}");
				Console.WriteLine($"   punto de control despues  : {api.LastEventApplied()}");
				Console.WriteLine();

				// EL CRITERIO 1 DE §T8: re-entregar el mismo bloque no deja NI UNA entrada. Aca "byte
				// a byte" SI se sostiene, y es la forma fuerte de la afirmacion: se compara antes y
				// despues de algo que no debe escribir nada (C23).
				string bytesBefore = TheTreeOf(actorFolder);
				entryBefore = app.Actor.CurrentEntryId;
				ResultadoDeRefresh second = await loop.Ejecutar();

				Console.WriteLine("-- refresh 2, el MISMO bloque re-entregado --");
				Console.WriteLine($"   {second}");
				Console.WriteLine($"   entradas del journal      : {entryBefore} -> {app.Actor.CurrentEntryId}");
				Console.WriteLine($"   arbol byte a byte igual   : {TheTreeOf(actorFolder) == bytesBefore}");
				Console.WriteLine("   (el Check rechaza cada evento repetido BAJO EL READ LOCK, asi que no");
				Console.WriteLine("    escribe ni una entrada no-op. Con un if adentro del cuerpo en vez de");
				Console.WriteLine("    un Check, esta linea diria False y el journal habria crecido)");
				Console.WriteLine();

				// Y EL CRITERIO 1 EN SU FORMA COMPLETA: el bloque ENTERO, desde el evento cero,
				// como si el proveedor hubiera olvidado el checkpoint y re-sirviera todo. El
				// refresh de arriba solo vuelve a leer lo que hay despues del punto de control, asi
				// que por si solo no demuestra lo que §T8 pide. Esto si.
				bytesBefore = TheTreeOf(actorFolder);
				entryBefore = app.Actor.CurrentEntryId;
				Ingested whole = await TheWholeBlockAgainFromZero(api, http, resource, id);

				Console.WriteLine("-- el bloque ENTERO re-entregado desde el evento 0 --");
				Console.WriteLine($"   {whole}");
				Console.WriteLine($"   entradas del journal      : {entryBefore} -> {app.Actor.CurrentEntryId}");
				Console.WriteLine($"   arbol byte a byte igual   : {TheTreeOf(actorFolder) == bytesBefore}");
				Console.WriteLine();

				// EL SALTO DE T7, ahora sobre hechos que llegaron por la red. El instante es un
				// LITERAL: el dominio no lee un reloj, asi que evaluar seis meses despues es un
				// argumento y no una espera.
				EvaluationOutcome evaluated = api.Evaluate(TheInstantTheBoardIsRead);
				Console.WriteLine($"-- evaluacion al {Day(TheInstantTheBoardIsRead)} --");
				Report(evaluated);
				Console.WriteLine();

				PrintTheBoard(api, TheBoard(api), null);

				// EL GUARDIAN DE LAS DOS COMPARACIONES DE ARRIBA, y tiene que dar False. Las dos
				// afirman que el arbol NO cambio, asi que un TheTreeOf que devolviera una constante
				// las dejaria verdes para siempre -- que es la leccion de T6 repitiendose, y en T7
				// costo agregar exactamente esta linea. Evaluar un instante POSTERIOR si escribe.
				bytesBefore = TheTreeOf(actorFolder);
				entryBefore = app.Actor.CurrentEntryId;
				api.Evaluate(TheInstantTheBoardIsRead.AddDays(1));

				Console.WriteLine("-- evaluar un instante POSTERIOR, que SI tiene que escribir --");
				Console.WriteLine($"   entradas del journal      : {entryBefore} -> {app.Actor.CurrentEntryId}");
				Console.WriteLine($"   arbol byte a byte igual   : {TheTreeOf(actorFolder) == bytesBefore}   <- tiene que ser False");
				Console.WriteLine();

				await TheDecoyAnswers403(http, api, resource, id);

				app.Dispose();
			}

			return 0;
		}

		// EL CORTE A MITAD DE BLOQUE, de verdad: se aplican los primeros N eventos y el proceso se
		// MATA -- sin Dispose, sin GracefulExit, sin flush -- que es lo mas parecido a que se corte
		// la luz que se puede escribir. El criterio 2 de §T8 se comprueba corriendo despues un
		// 'refresco' sobre la misma carpeta: no tiene que duplicar ni saltear nada.
		//
		// El bloque se recorre a mano y no con BucleDeRefresh porque el bucle aplica el bloque
		// entero: cortarlo es justo lo que hay que poder hacer. El cuerpo es identico al suyo.
		private static async Task<int> ACrashInTheMiddleOfABlock(string[] args)
		{
			string folder  = AFolder(args, 1);
			string baseUrl = AnArgument(args, 2, "http://localhost:8092/");
			string resource = AnArgument(args, 3, "vehicle");
			string id      = AnArgument(args, 4, Story.TheVin);
			int howMany    = int.Parse(AnArgument(args, 5, "6"), CultureInfo.InvariantCulture);

			Console.WriteLine("DealershipCar - un corte a mitad de bloque");
			Console.WriteLine();
			Console.WriteLine($"folder    : {folder}");

			using (var http = new HttpClient { BaseAddress = new Uri(baseUrl) })
			{
				DealershipCarApp app = Open(folder);
				var api = new DealershipCarAPI(app);

				await TheAppGetsItsOwnCredential(api, new ProveedorDeToken(http), resource, id);

				var source = new FuenteHttpDeEventos(http, resource, id);
				IReadOnlyList<SenalDeEvento> block =
					await source.LeerDesde(api.LastEventApplied(), api.StoredToken(), CancellationToken.None);

				Console.WriteLine($"bloque    : {block.Count} eventos desde el {api.LastEventApplied()}");
				Console.WriteLine($"se aplican: {howMany}, y despues se MATA el proceso");
				Console.WriteLine();

				int applied = 0;
				foreach (SenalDeEvento signal in block)
				{
					if (applied == howMany) break;

					IMensajeDeEvento message = DealershipEventRoute.Route(signal);
					if (message == null)
					{
						Console.WriteLine($"   #{signal.Numero,-4} {signal.Tipo,-26} DESCARTADO");
						continue;
					}

					string rejection = api.Apply(message);
					Console.WriteLine($"   #{signal.Numero,-4} {signal.Tipo,-26} "
						+ (rejection.Length > 0 ? "RECHAZADO: " + rejection : "aplicado"));
					applied++;
				}

				Console.WriteLine();
				Console.WriteLine($"punto de control : {api.LastEventApplied()}");
				Console.WriteLine("MATANDO EL PROCESO (sin Dispose, sin flush)");
				Console.Out.Flush();
			}

			// Environment.Exit y no un return: se salta todo cierre ordenado, que es el punto.
			Environment.Exit(0);
			return 0;
		}

		// LA MEDICION QUE T7 DEJO, contestada. Se pregunta por reflexion y no contra una lista
		// escrita a mano: una lista se queda vieja en silencio y esto no puede.
		//
		// SE PREGUNTA POR EL MAPA DE LA INTERFAZ y no por un metodo publico con ese nombre, y eso es
		// un cambio respecto de T7. Los tres miembros de IDiarioDeLaApp se implementan EXPLICITAMENTE
		// -- la interfaz es del mecanismo compartido y sus nombres estan en espanol, los de este
		// modulo en ingles (C29) -- asi que un GetMethod publico por nombre daria "NO ESTA" para
		// siempre, midiendo la convencion de nombres en vez del cableado.
		//
		// EL CONTROL SIGUE ESTANDO y ahora es un NEGATIVO que tiene que quedar negativo: se le hace
		// la misma pregunta a un tipo que seguro NO implementa la interfaz. Si ese renglon dijera que
		// si, la reflexion estaria rota y las tres respuestas de arriba no probarian nada.
		private static void WhatTheRefreshLoopNeededAndWhetherTheDoorAnswersItNow()
		{
			Console.WriteLine("BucleDeRefresh(diario, fuente, ruta, flush, renovarCredencial)");
			Console.WriteLine();
			Console.WriteLine("  flush     : null, y es lo correcto para esta caratula: existe para bajar");
			Console.WriteLine("              de MEMFS a la carpeta del usuario en wasm, y aca el journal");
			Console.WriteLine("              ya esta en disco.");
			Console.WriteLine("  fuente    : FuenteHttpDeEventos - la API de eventos del concesionario.");
			Console.WriteLine("  ruta      : DealershipEventRoute.Route - senal cruda -> IMensajeDeEvento.");
			Console.WriteLine("  diario    : DealershipCarAPI, que ahora ES un IDiarioDeLaApp:");
			Console.WriteLine();

			Console.WriteLine($"    {"(control) EvaluationOutcome",-30} "
				+ $"{WhetherItIsAJournalOfTheApp(typeof(EvaluationOutcome))}");
			Console.WriteLine($"    {"DealershipCarAPI",-30} "
				+ $"{WhetherItIsAJournalOfTheApp(typeof(DealershipCarAPI))}");
			Console.WriteLine();

			foreach (MethodInfo required in typeof(IDiarioDeLaApp).GetMethods())
				Console.WriteLine($"    {required.Name,-30} {WhatAnswers(required)}");

			Console.WriteLine();
			Console.WriteLine("  UltimoEventoProcesado sale DEL JOURNAL -- es lo que hace exacto el resume");
			Console.WriteLine("  tras una caida -- y TokenGuardado de una credencial journalizada. Las dos");
			Console.WriteLine("  viven en Logbook, una SEGUNDA global de raiz al lado de la agenda y nunca");
			Console.WriteLine("  adentro: ni el punto de control ni la credencial son hechos de");
			Console.WriteLine("  mantenimiento. MaintenanceSchedule no cambio. Es C26, cerrado.");
			Console.WriteLine();
		}

		private static string WhetherItIsAJournalOfTheApp(Type candidate)
		{
			return typeof(IDiarioDeLaApp).IsAssignableFrom(candidate)
				? "implementa IDiarioDeLaApp"
				: "NO implementa IDiarioDeLaApp";
		}

		// Que miembro de la puerta contesta cada miembro de la interfaz. El mapa lo da el runtime, y
		// ve tambien las implementaciones explicitas, que es justo lo que hay que ver.
		private static string WhatAnswers(MethodInfo required)
		{
			if (!typeof(IDiarioDeLaApp).IsAssignableFrom(typeof(DealershipCarAPI)))
				return "NO ESTA en la puerta";

			InterfaceMapping map = typeof(DealershipCarAPI).GetInterfaceMap(typeof(IDiarioDeLaApp));

			for (int member = 0; member < map.InterfaceMethods.Length; member++)
				if (map.InterfaceMethods[member] == required)
					return "lo contesta " + map.TargetMethods[member].Name;

			return "NO ESTA en la puerta";
		}

		// EL BLOQUE ENTERO, PEDIDO DESDE CERO. Se recorre a mano y no con BucleDeRefresh porque el
		// bucle pide desde el punto de control journalizado -- que es lo correcto y es justo lo que
		// hay que puentear para poder afirmar el criterio. El cuerpo del foreach es el suyo.
		private static async Task<Ingested> TheWholeBlockAgainFromZero(
			DealershipCarAPI api, HttpClient http, string resource, string id)
		{
			var source = new FuenteHttpDeEventos(http, resource, id);
			IReadOnlyList<SenalDeEvento> block =
				await source.LeerDesde(0, api.StoredToken(), CancellationToken.None);

			var counted = new Ingested { Read = block.Count };

			foreach (SenalDeEvento signal in block)
			{
				IMensajeDeEvento message = DealershipEventRoute.Route(signal);
				if (message == null) { counted.Discarded++; continue; }

				if (api.Apply(message).Length > 0) { counted.Rejected++; continue; }

				counted.Applied++;
			}

			return counted;
		}

		private sealed class Ingested
		{
			internal int Read;
			internal int Applied;
			internal int Discarded;
			internal int Rejected;

			public override string ToString()
			{
				return $"leidos={Read} aplicados={Applied} descartados={Discarded} rechazados={Rejected}";
			}
		}

		// LA APP CONSIGUE SU CREDENCIAL SOLA, contra su clave de device. Ni la caratula ni la persona
		// pegan un token: el concesionario lo emite para ESTA instalacion, y desde ahi vive en el
		// journal.
		private static async Task TheAppGetsItsOwnCredential(
			DealershipCarAPI api, ProveedorDeToken tokens, string resource, string id)
		{
			if (api.StoredDeviceKey().Length == 0)
				api.RegisterDevice("device-consola-de-medicion");

			if (api.StoredToken().Length > 0) return;

			Credencial credential = await tokens.Pedir(api.StoredDeviceKey(), resource, id);
			Console.WriteLine($"credencial: scope={credential.Scope} expiresIn={credential.ExpiresIn}s");
			Console.WriteLine();
			api.StoreCredential(credential.AccessToken, credential.RefreshToken);
		}

		// Que hacer cuando el concesionario rechaza el access token: rotar con el refresh token, que
		// es para lo que existe. El access es corto a proposito, asi que sin rotacion la app
		// instalada se rompe sola al rato.
		private static async Task<string> RenewTheCredential(
			DealershipCarAPI api, ProveedorDeToken tokens, string resource, string id,
			CancellationToken ct)
		{
			string refresh = api.StoredRefreshToken();

			// Rotar devuelve null si el refresh ya no vale; ahi se pide una credencial nueva con la
			// clave de device, que es lo que la instalacion siempre puede hacer.
			Credencial renewed = refresh.Length > 0 ? await tokens.Rotar(refresh, ct) : null;
			string how = renewed != null ? "rotada con el refresh token" : "nueva contra el device key";
			if (renewed == null) renewed = await tokens.Pedir(api.StoredDeviceKey(), resource, id, ct);

			Console.WriteLine($"   credencial renovada: {how} (scope {renewed.Scope})");
			api.StoreCredential(renewed.AccessToken, renewed.RefreshToken);
			return renewed.AccessToken;
		}

		// EL SENUELO, con la credencial DE LA APP. Lo pide esta caratula y no el mecanismo, y esa es
		// la diferencia entera: FuenteHttpDeEventos no tiene con que pedir esto -- arma una sola forma
		// de URL y no tiene otro metodo -- asi que hay que salirse de ella para intentarlo. El 403 no
		// es cortesia del mock: el scope del token es {recurso}.events.read y el senuelo exige otro.
		private static async Task TheDecoyAnswers403(
			HttpClient http, DealershipCarAPI api, string resource, string id)
		{
			Console.WriteLine("-- el senuelo: la app NO es cliente de los servicios del concesionario --");

			foreach (string route in new[] { $"{resource}/{id}/events?after=0", $"{resource}/{id}/customer" })
			{
				using (var request = new HttpRequestMessage(HttpMethod.Get, route))
				{
					request.Headers.Authorization =
						new AuthenticationHeaderValue("Bearer", api.StoredToken());

					using (HttpResponseMessage answer = await http.SendAsync(request))
					{
						Console.WriteLine($"   GET {route,-46} -> {(int)answer.StatusCode}");
						if (!answer.IsSuccessStatusCode)
							Console.WriteLine($"       {await answer.Content.ReadAsStringAsync()}");
					}
				}
			}

			Console.WriteLine();
			Console.WriteLine("   El 403 es ESTRUCTURAL: el token esta acotado a leer eventos de ESTE");
			Console.WriteLine("   vehiculo, y el expediente del cliente exige un scope que no lleva.");
			Console.WriteLine("   Es lo que vuelve falsable el invariante en vez de solo afirmarlo.");
			Console.WriteLine();
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

		// EL INSTANTE EN QUE SE LEE EL TABLERO DESPUES DEL REFRESH, y es un LITERAL. El dominio no lee
		// un reloj: recibe el instante como parametro. Con DateTime.UtcNow aca, la misma narrativa
		// aplicada manana daria otros vencimientos y esta caratula dejaria de ser reproducible -- y
		// dos corridas del mismo dia NO lo notarian, que es exactamente C27.
		private static readonly DateTime TheInstantTheBoardIsRead =
			new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);

		private static string AnArgument(string[] args, int i, string byDefault)
		{
			return args.Length > i && args[i].Length > 0 ? args[i] : byDefault;
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
			Console.WriteLine("  refresco [carpeta] [url] [recurso] [id]");
			Console.WriteLine("                       trae los eventos del concesionario y los aplica de a uno");
			Console.WriteLine("  corte    [carpeta] [url] [recurso] [id] [n]");
			Console.WriteLine("                       aplica n eventos y MATA el proceso, para comprobar que");
			Console.WriteLine("                       el 'refresco' siguiente no duplica ni saltea");
			Console.WriteLine();
			Console.WriteLine("carpeta por defecto: ~/DealershipCar-journal");
			Console.WriteLine("url2    por defecto: http://localhost:8092/  (recurso 'vehicle', id = el VIN)");
			Console.WriteLine();
			Console.WriteLine("Las clases de operacion cruzan como el NOMBRE de su miembro, no como enum:");
			Console.WriteLine("esta caratula no referencia el dominio y no puede nombrar sus tipos (C25).");
			Console.WriteLine($"  {string.Join(", ", Story.TheNineKinds)}");
		}
	}
}
