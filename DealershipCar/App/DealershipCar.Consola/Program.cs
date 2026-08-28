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
	internal static class Program
	{
		private static async Task<int> Main(string[] args)
		{
			try
			{
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

		private static DealershipCarApp Open(string folder)
		{
			Directory.CreateDirectory(folder);

			var app = new DealershipCarApp(
				Story.TheVin, Story.TheProfileAtDelivery, Story.TheModelAtDelivery);
			app.ConfigureStorage(DatabaseType.FileSystem, "path=" + folder);
			app.Start();
			return app;
		}

		private static int TheSixMonthStory(string folder)
		{
			Console.WriteLine("DealershipCar - la historia de seis meses, desde un journal vacio");
			Console.WriteLine();
			Console.WriteLine($"folder    : {folder}");
			Console.WriteLine($"vin       : {Story.TheVin}");
			Console.WriteLine($"actor     : {DealershipCarApp.TheActorFor(Story.TheVin)}");

			EmptyTheJournalOfThisVehicle(folder);
			Console.WriteLine("journal   : vaciado - esta historia SIEMPRE borra el journal de este");
			Console.WriteLine("            vehiculo y arranca de cero, para poder ser reproducible");

			DealershipCarApp app = Open(folder);
			var api = new DealershipCarAPI(app);

			Console.WriteLine($"entrada 0 : {app.Actor.CurrentEntryId}  (la semilla, que deja OnHydrated)");
			Console.WriteLine();

			Act(app, "1. llega el plan del fabricante, y la persona se agrega una operacion",
				() => Story.ThePlanArrives(api));

			PrintTheBoard(api, TheBoard(api), null);

			Console.WriteLine("2. seis meses de odometro. LLEGAN DESORDENADAS: la de diciembre entra");
			Console.WriteLine("   antes que la de noviembre, y la de marzo antes que la de febrero.");
			foreach ((int value, string unit, DateTime observedAt, string source)
				in Story.TheReadingsInTheOrderTheyArrive())
				Console.WriteLine($"     llega {value,6} {unit}  observada el {Day(observedAt)}  de {source}");
			Console.WriteLine();

			Act(app, "   aplicadas al dominio, que las ordena por instante observado",
				() => Story.TheOdometerReportsForSixMonths(api));

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

		private static void Act(DealershipCarApp app, string title, Action act)
		{
			long before = app.Actor.CurrentEntryId;
			act();
			long after = app.Actor.CurrentEntryId;

			Console.WriteLine($"{title}");
			Console.WriteLine($"   entradas del journal     : {before} -> {after}  ({after - before} escritas)");
			Console.WriteLine();
		}

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

		private static Dictionary<string, string> TheBoard(DealershipCarAPI api)
		{
			var board = new Dictionary<string, string>();

			foreach (string kind in Story.TheNineKinds) board[kind] = api.ReadDueState(kind);

			return board;
		}

		private static void PrintTheBoard(
			DealershipCarAPI api, Dictionary<string, string> board, Dictionary<string, string> before)
		{
			Console.WriteLine($"   vin leido del journal    : {api.ReadVin()}");
			Console.WriteLine($"   evaluada hasta           : {(api.HasBeenEvaluated() ? Day(api.LastEvaluatedAt()) : "nunca")}");
			Console.WriteLine();

			foreach (string kind in Story.TheNineKinds)
			{
				string state = board[kind];

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

			Console.WriteLine($"   lo primero que hacer     : {(api.HasAnythingDue() ? api.ReadNextDueKind() : "nada")}");
			Console.WriteLine();
		}

		private static bool ItIsScheduled(DealershipCarAPI api, string kind)
		{
			return !api.ReadDueReason(kind).StartsWith("There is no scheduled", StringComparison.Ordinal);
		}

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

			string bytesBefore = TheTreeOf(actorFolder);
			long entryBefore = app.Actor.CurrentEntryId;
			EvaluationOutcome again = api.Evaluate(Story.SixMonthsLater);

			Console.WriteLine("re-evaluar el mismo instante:");
			Console.WriteLine($"  se evaluo                 : {again.WasEvaluated}");
			Console.WriteLine($"  motivo del rechazo        : {Unwrapped(again.Rejection)}");
			Console.WriteLine($"  entrada del journal       : {entryBefore} -> {app.Actor.CurrentEntryId}");
			Console.WriteLine($"  arbol byte a byte igual   : {TheTreeOf(actorFolder) == bytesBefore}");
			Console.WriteLine();

			bytesBefore = TheTreeOf(actorFolder);
			entryBefore = app.Actor.CurrentEntryId;
			string refusal = WhatTheDoorSaysAbout(() => api.ReadDueState("fluidchange"));

			Console.WriteLine("un codigo que el canon no escribe ('fluidchange'):");
			Console.WriteLine($"  la puerta contesta        : {refusal}");
			Console.WriteLine($"  entrada del journal       : {entryBefore} -> {app.Actor.CurrentEntryId}");
			Console.WriteLine($"  arbol byte a byte igual   : {TheTreeOf(actorFolder) == bytesBefore}");
			Console.WriteLine();

			bytesBefore = TheTreeOf(actorFolder);
			entryBefore = app.Actor.CurrentEntryId;
			EvaluationOutcome later = api.Evaluate(Story.SixMonthsLater.AddDays(1));

			Console.WriteLine("evaluar un instante POSTERIOR:");
			Console.WriteLine($"  se evaluo                 : {later.WasEvaluated}");
			Console.WriteLine($"  entrada del journal       : {entryBefore} -> {app.Actor.CurrentEntryId}");
			Console.WriteLine($"  arbol byte a byte igual   : {TheTreeOf(actorFolder) == bytesBefore}   <- tiene que ser False");
			Console.WriteLine();

			Console.WriteLine("cerrar y reabrir sobre el mismo journal:");
			Console.WriteLine($"  entrada antes de cerrar   : {app.Actor.CurrentEntryId}");
			Console.WriteLine("  (la entrada despues de reabrir se mide con el subcomando 'estado',");
			Console.WriteLine("   que corre en un proceso NUEVO: eso es lo que prueba el replay)");
			Console.WriteLine();
		}

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

				bytesBefore = TheTreeOf(actorFolder);
				entryBefore = app.Actor.CurrentEntryId;
				Ingested whole = await TheWholeBlockAgainFromZero(api, http, resource, id);

				Console.WriteLine("-- el bloque ENTERO re-entregado desde el evento 0 --");
				Console.WriteLine($"   {whole}");
				Console.WriteLine($"   entradas del journal      : {entryBefore} -> {app.Actor.CurrentEntryId}");
				Console.WriteLine($"   arbol byte a byte igual   : {TheTreeOf(actorFolder) == bytesBefore}");
				Console.WriteLine();

				EvaluationOutcome evaluated = api.Evaluate(TheInstantTheBoardIsRead);
				Console.WriteLine($"-- evaluacion al {Day(TheInstantTheBoardIsRead)} --");
				Report(evaluated);
				Console.WriteLine();

				PrintTheBoard(api, TheBoard(api), null);

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

			Environment.Exit(0);
			return 0;
		}

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
			Console.WriteLine("  mantenimiento. MaintenanceSchedule no cambio.");
			Console.WriteLine();
		}

		private static string WhetherItIsAJournalOfTheApp(Type candidate)
		{
			return typeof(IDiarioDeLaApp).IsAssignableFrom(candidate)
				? "implementa IDiarioDeLaApp"
				: "NO implementa IDiarioDeLaApp";
		}

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

		private static async Task<string> RenewTheCredential(
			DealershipCarAPI api, ProveedorDeToken tokens, string resource, string id,
			CancellationToken ct)
		{
			string refresh = api.StoredRefreshToken();

			Credencial renewed = refresh.Length > 0 ? await tokens.Rotar(refresh, ct) : null;
			string how = renewed != null ? "rotada con el refresh token" : "nueva contra el device key";
			if (renewed == null) renewed = await tokens.Pedir(api.StoredDeviceKey(), resource, id, ct);

			Console.WriteLine($"   credencial renovada: {how} (scope {renewed.Scope})");
			api.StoreCredential(renewed.AccessToken, renewed.RefreshToken);
			return renewed.AccessToken;
		}

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
			}

			return rejection;
		}

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

		private static string Day(DateTime instant)
		{
			return instant.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
		}

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
			Console.WriteLine("esta caratula no referencia el dominio y no puede nombrar sus tipos.");
			Console.WriteLine($"  {string.Join(", ", Story.TheNineKinds)}");
		}
	}
}
