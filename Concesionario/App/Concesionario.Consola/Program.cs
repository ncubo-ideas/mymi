using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Concesionario.App;
using Concesionario.App.Events;
using Puppeteer;
using Puppets.Portador;

namespace Concesionario.Consola
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
					case "ficha":    return TheSpecSheetPublishedAndReadBack(AFolder(args, 1));
					case "garantia": return TheWarrantyEnvelopePublishedAndReadBack(AFolder(args, 1));
					case "taller":   return TheShopStaysRecordedAndReadBack(AFolder(args, 1));
					case "costos":   return TheCostLedgerRecordedAndReadBack(AFolder(args, 1));
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
			Console.WriteLine("Concesionario - la historia de seis meses, desde un journal vacio");
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

			TheCountdownOfEveryKind(api);
			TheHistoryOfEveryKind(api);
			ThePublishedServicePrices(api);
			TheRateOfUseOverTheStorysSpan(api);
			TheTwoHalvesOfTheStoryCompared(api);

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
				Console.WriteLine($"   {"",-20} {api.ReadDueReason(kind, TheVoiceOfTheDealership)}");

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
			return api.ReadDistanceRemainderStanding(kind, TheInstantTheBoardIsRead)
				!= TheStandingOfAKindWithNoOperation;
		}

		private static void TheCountdownOfEveryKind(DealershipCarAPI api)
		{
			Console.WriteLine("================================================================");
			Console.WriteLine($"  CUANTO ME FALTA: LA CUENTA ATRAS DE CADA CLASE, AL {Day(TheInstantTheBoardIsRead)}");
			Console.WriteLine();
			Console.WriteLine("  El instante esta ESCRITO, como todos los de esta corrida: ni el dominio");
			Console.WriteLine("  ni esta caratula leen la hora, asi que dos corridas contestan lo mismo.");
			Console.WriteLine();
			Console.WriteLine("  La frase la compone el dominio y dice LAS DOS dimensiones sin elegir");
			Console.WriteLine("  ninguna. La distancia y el plazo rara vez coinciden, y quedarse con la");
			Console.WriteLine("  mas cercana dejaria el numero discrepando del motivo del estado para");
			Console.WriteLine("  quien lea los dos juntos. Las dos mitades se pueden pedir por separado");
			Console.WriteLine("  ademas de juntas, y la caratula no las divide ni las suma.");
			Console.WriteLine();
			Console.WriteLine("  Las diez lecturas heredan una guarda del dominio: un instante ANTERIOR");
			Console.WriteLine("  al ancla de la ocurrencia se rechaza, y la puerta lo TRADUCE a un tipo");
			Console.WriteLine("  que una caratula puede nombrar. No se puede preguntar antes, porque");
			Console.WriteLine("  ninguna lectura contesta el ancla, asi que se atrapa POR CLASE: una");
			Console.WriteLine("  sola con el plan anclado adelante no se lleva las otras ocho. En esta");
			Console.WriteLine("  corrida las cinco agendadas estan ancladas el dia de la entrega, que es");
			Console.WriteLine("  anterior al instante del tablero, asi que no salta.");
			Console.WriteLine("================================================================");
			Console.WriteLine();

			foreach (string kind in Story.TheNineKinds) TheCountdownOfOneKind(api, kind);
		}

		private static void TheCountdownOfOneKind(DealershipCarAPI api, string kind)
		{
			Console.WriteLine($"   {kind}");

			try
			{
				Console.WriteLine($"   la cuenta atras          : {api.DescribeCountdown(kind, TheInstantTheBoardIsRead, TheVoiceOfTheDealership)}");
				Console.WriteLine($"   situacion en distancia   : {api.ReadDistanceRemainderStanding(kind, TheInstantTheBoardIsRead)}");
				Console.WriteLine($"   la distancia en palabras : {api.DescribeDistanceRemaining(kind, TheInstantTheBoardIsRead, TheVoiceOfTheDealership)}");

				bool distance = api.CanDistanceRemainingBeSaid(kind, TheInstantTheBoardIsRead);
				Console.WriteLine($"   se puede decir cuanta    : {distance}");

				if (distance)
				{
					Console.WriteLine($"   distancia que queda      : {api.ReadDistanceRemaining(kind, TheInstantTheBoardIsRead)}");
					Console.WriteLine($"   y en que unidad          : {api.ReadDistanceRemainingUnit(kind, TheInstantTheBoardIsRead)}");
				}

				Console.WriteLine($"   situacion en el plazo    : {api.ReadTimeRemainderStanding(kind, TheInstantTheBoardIsRead)}");
				Console.WriteLine($"   el plazo en palabras     : {api.DescribeDaysRemaining(kind, TheInstantTheBoardIsRead, TheVoiceOfTheDealership)}");

				bool days = api.CanDaysRemainingBeSaid(kind, TheInstantTheBoardIsRead);
				Console.WriteLine($"   se puede decir cuantos   : {days}");

				if (days)
					Console.WriteLine($"   dias que quedan          : {api.ReadDaysRemaining(kind, TheInstantTheBoardIsRead)}");
			}
			catch (InvalidOperationException refused)
			{
				Console.WriteLine($"   la puerta contesta       : {refused.GetType().Name} - {FirstLineOf(refused.Message)}");
			}

			Console.WriteLine();
		}

		private static void TheHistoryOfEveryKind(DealershipCarAPI api)
		{
			Console.WriteLine("================================================================");
			Console.WriteLine("  QUE ME HAN HECHO: EL HISTORIAL, CAMINADO EN CADA LECTURA");
			Console.WriteLine();
			Console.WriteLine("  Ninguna de las cinco lecturas toma un instante: cada fila trae el suyo");
			Console.WriteLine("  y la cadena es la que es. Nada de esto esta guardado como lista -- la");
			Console.WriteLine("  completacion de una ocurrencia es el ancla de su sucesora, y la cadena");
			Console.WriteLine("  se camina hacia atras en cada pregunta.");
			Console.WriteLine();
			Console.WriteLine("  Esta corrida guiona el plan, seis meses de odometro, un aplazo, un");
			Console.WriteLine("  perfil severo y un cruce de umbral, y NINGUNA completacion. Lo que se");
			Console.WriteLine("  ve abajo son las DOS AUSENCIAS de la parte B, que no son la misma:");
			Console.WriteLine("  cuatro clases no estan agendadas, y cinco lo estan y nunca se");
			Console.WriteLine("  completaron. Un booleano juntaria las dos.");
			Console.WriteLine("================================================================");
			Console.WriteLine();

			foreach (string kind in Story.TheNineKinds) TheHistoryOfOneKind(api, kind);

			TheWholeHistoryAsRows(api);
		}

		private static void TheHistoryOfOneKind(DealershipCarAPI api, string kind)
		{
			CompletionRecord record = api.CompletionsOf(kind);

			Console.WriteLine($"   {kind}");
			Console.WriteLine($"   hay alguna               : {api.HasAnyCompletion(kind)}");
			Console.WriteLine($"   cuantas                  : {api.CompletionCount(kind)}");
			Console.WriteLine($"   el historial en palabras : {api.DescribeCompletions(kind)}");

			if (record.Says.Length == 0) Console.WriteLine("   filas                    : (ninguna)");

			foreach (int row in Enumerable.Range(0, record.Says.Length))
			{
				Console.WriteLine($"   hecha el                 : {record.CompletedAt[row]}");
				Console.WriteLine($"   con el odometro en       : {record.Reading[row]}");
				Console.WriteLine($"   dice la fila             : {record.Says[row]}");
			}

			Console.WriteLine();
		}

		private static void TheWholeHistoryAsRows(DealershipCarAPI api)
		{
			EveryKindCompletionRecord record = api.CompletionsOfEveryKind();

			Console.WriteLine("   -- el historial ENTERO, de la completacion mas reciente a la mas vieja");
			Console.WriteLine("      entre todas las clases, en columnas paralelas --");
			Console.WriteLine();
			Console.WriteLine("   Esta lectura no tiene frase de ausencia y no la va a tener: sin ninguna");
			Console.WriteLine("   completacion contesta CUATRO COLUMNAS VACIAS y nada mas. Es una");
			Console.WriteLine("   decision de alcance y no un hueco -- la que nombra una clase es la de");
			Console.WriteLine("   arriba, y componer una frase aca la pondria a decir algo que el dominio");
			Console.WriteLine("   no dijo. Y trae la columna de la CLASE, que la de una sola clase no");
			Console.WriteLine("   trae: alla la puso quien pregunta.");
			Console.WriteLine();

			if (record.Kind.Length == 0) Console.WriteLine("   (ninguna fila)");

			foreach (int row in Enumerable.Range(0, record.Kind.Length))
			{
				Console.WriteLine($"   clase                    : {record.Kind[row]}");
				Console.WriteLine($"   hecha el                 : {record.CompletedAt[row]}");
				Console.WriteLine($"   con el odometro en       : {record.Reading[row]}");
				Console.WriteLine($"   dice la fila             : {record.Says[row]}");
				Console.WriteLine();
			}

			Console.WriteLine();
		}

		private static void ThePublishedServicePrices(DealershipCarAPI api)
		{
			PublishedPriceRecord record = api.PublishedServicePricesAsOf(
				TheInstantTheBoardIsRead, TheVoiceOfTheDealership);

			Console.WriteLine("================================================================");
			Console.WriteLine($"  CUANTO ME VA A COSTAR: LOS PRECIOS PUBLICADOS, AL {Day(TheInstantTheBoardIsRead)}");
			Console.WriteLine();
			Console.WriteLine("  Las filas se ENUMERAN, y eso es todo el punto: ninguna pide un hito a");
			Console.WriteLine("  quien pregunta, porque nadie puede nombrar un hito que no aprendio del");
			Console.WriteLine("  dominio. El instante mueve la FRASE de cada fila -- si la vigencia rige");
			Console.WriteLine("  hoy o ya paso -- y nunca el juego de filas, y por eso las dos primeras");
			Console.WriteLine("  lecturas no lo toman. Esa asimetria es la afirmacion y no un descuido.");
			Console.WriteLine("================================================================");
			Console.WriteLine();
			Console.WriteLine($"   hay alguno               : {api.HasAnyPublishedServicePrice()}");
			Console.WriteLine($"   cuantos                  : {api.PublishedServicePriceCount()}");
			Console.WriteLine();

			if (record.Operation.Length == 0)
			{
				Console.WriteLine("   (ninguna fila, y no es un defecto: esta corrida guiona el plan del");
				Console.WriteLine("    fabricante y el odometro, y no publica ninguna REVISION suya. La que");
				Console.WriteLine("    las publica es 'ficha', que borra el journal por otro motivo que");
				Console.WriteLine("    'historia': un codigo de revision se toma una sola vez)");
				Console.WriteLine();
			}

			foreach (int row in Enumerable.Range(0, record.Operation.Length))
			{
				Console.WriteLine($"   operacion                : {record.Operation[row]}");
				Console.WriteLine($"   hito                     : {record.Milestone[row]}");
				Console.WriteLine($"   revision                 : {record.Revision[row]}");
				Console.WriteLine($"   dice la fila             : {record.Says[row]}");
				Console.WriteLine();
			}
		}

		private static void TheRateOfUseOverTheStorysSpan(DealershipCarAPI api)
		{
			OdometerSeriesRecord record = api.ReadingsBetween(Story.Delivery, TheInstantTheBoardIsRead);

			Console.WriteLine("================================================================");
			Console.WriteLine($"  CUANTO SE MANEJA: EL RITMO DEL {Day(Story.Delivery)} AL {Day(TheInstantTheBoardIsRead)}");
			Console.WriteLine();
			Console.WriteLine("  El tramo va del dia de la entrega al instante del tablero, que son los");
			Console.WriteLine("  dos extremos de esta historia y los dos estan ESCRITOS. Ninguno se");
			Console.WriteLine("  deriva del otro: restarle un ano al instante del tablero daria el mismo");
			Console.WriteLine("  dia y traeria un largo de ventana que nadie le publico a esta caratula.");
			Console.WriteLine();
			Console.WriteLine("  El ritmo lo compone el dominio entero y llega dicho: los dos extremos");
			Console.WriteLine("  con sus instantes, la distancia y el tiempo EXACTOS, y recien despues la");
			Console.WriteLine("  distancia por dia, que es el unico numero redondeado y sale introducido");
			Console.WriteLine("  por 'unos'. Quien lea la frase puede rehacer la division; la caratula no");
			Console.WriteLine("  divide nada, ni siquiera para comprobarla.");
			Console.WriteLine();
			Console.WriteLine("  Y las filas van al lado de la frase, no en vez de ella: la tabla de");
			Console.WriteLine("  lecturas y el ritmo son DOS lecturas separadas de la misma puerta, y las");
			Console.WriteLine("  dos se imprimen enteras. Cada fila se dice sola -- valor, unidad,");
			Console.WriteLine("  instante y fuente -- asi que no hay nada que recombinar.");
			Console.WriteLine("================================================================");
			Console.WriteLine();
			Console.WriteLine($"   se puede decir           : {api.CanARateOfUseBeSaid(Story.Delivery, TheInstantTheBoardIsRead)}");
			Console.WriteLine($"   situacion del ritmo      : {api.ReadRateOfUseStanding(Story.Delivery, TheInstantTheBoardIsRead)}");
			Console.WriteLine($"   el ritmo en palabras     : {api.DescribeRateOfUse(Story.Delivery, TheInstantTheBoardIsRead)}");
			Console.WriteLine($"   cuantas lecturas         : {api.ReadingCountBetween(Story.Delivery, TheInstantTheBoardIsRead)}");
			Console.WriteLine();

			if (record.Says.Length == 0) Console.WriteLine("   filas                    : (ninguna)");

			foreach (int row in Enumerable.Range(0, record.Says.Length))
			{
				Console.WriteLine($"   observada el             : {record.ObservedAt[row]}");
				Console.WriteLine($"   el odometro marcaba      : {record.Value[row]}");
				Console.WriteLine($"   en unidad                : {record.Unit[row]}");
				Console.WriteLine($"   de fuente                : {record.Source[row]}");
				Console.WriteLine($"   dice la fila             : {record.Says[row]}");
				Console.WriteLine();
			}
		}

		private static void TheTwoHalvesOfTheStoryCompared(DealershipCarAPI api)
		{
			Console.WriteLine("================================================================");
			Console.WriteLine($"  LAS DOS MITADES DE LA HISTORIA, PARTIDA EL {Day(Story.TheFirstEvaluation)}");
			Console.WriteLine();
			Console.WriteLine("  Las dos mitades son las de la propia historia y el corte es su bisagra:");
			Console.WriteLine("  antes de la primera evaluacion llegan el plan y seis meses de odometro,");
			Console.WriteLine("  y despues no llega ningun hecho nuevo del odometro -- esta misma corrida");
			Console.WriteLine("  lo dice mas arriba, antes del salto de seis meses. No se eligio un corte");
			Console.WriteLine("  que dejara datos de los dos lados: el corte es un momento del guion.");
			Console.WriteLine();
			Console.WriteLine("  La mitad TARDIA va primera porque el dominio dice el cambio del PRIMER");
			Console.WriteLine("  periodo respecto al SEGUNDO, asi que cruzarlas daria la frase al reves.");
			Console.WriteLine();
			Console.WriteLine("  Y por eso esta mitad no tiene ritmo, que es la respuesta y no un hueco:");
			Console.WriteLine("  una sola lectura es un TODAVIA NO y nunca un cero. Cuando a alguno de");
			Console.WriteLine("  los dos periodos le falta el ritmo, la frase sigue diciendo los dos y no");
			Console.WriteLine("  dice ningun cambio.");
			Console.WriteLine("================================================================");
			Console.WriteLine();
			Console.WriteLine($"   primer periodo, del      : {Day(Story.TheFirstEvaluation)} al {Day(TheInstantTheBoardIsRead)}");
			Console.WriteLine($"   situacion del ritmo      : {api.ReadRateOfUseStanding(Story.TheFirstEvaluation, TheInstantTheBoardIsRead)}");
			Console.WriteLine($"   cuantas lecturas         : {api.ReadingCountBetween(Story.TheFirstEvaluation, TheInstantTheBoardIsRead)}");
			Console.WriteLine($"   se puede decir           : {api.CanARateOfUseBeSaid(Story.TheFirstEvaluation, TheInstantTheBoardIsRead)}");
			Console.WriteLine();
			Console.WriteLine($"   segundo periodo, del     : {Day(Story.Delivery)} al {Day(Story.TheFirstEvaluation)}");
			Console.WriteLine($"   situacion del ritmo      : {api.ReadRateOfUseStanding(Story.Delivery, Story.TheFirstEvaluation)}");
			Console.WriteLine($"   cuantas lecturas         : {api.ReadingCountBetween(Story.Delivery, Story.TheFirstEvaluation)}");
			Console.WriteLine($"   se puede decir           : {api.CanARateOfUseBeSaid(Story.Delivery, Story.TheFirstEvaluation)}");
			Console.WriteLine();
			Console.WriteLine($"   los dos comparados       : {api.DescribeTwoPeriodsOfUseCompared(Story.TheFirstEvaluation, TheInstantTheBoardIsRead, Story.Delivery, Story.TheFirstEvaluation)}");
			Console.WriteLine();
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
			Console.WriteLine("Concesionario - el mismo journal, reabierto en un proceso nuevo");
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

		private static int TheSpecSheetPublishedAndReadBack(string folder)
		{
			Console.WriteLine("Concesionario - la ficha del fabricante, publicada y leida de vuelta");
			Console.WriteLine();
			Console.WriteLine($"folder    : {folder}");
			Console.WriteLine($"vin       : {Story.TheVin}");
			Console.WriteLine($"actor     : {DealershipCarApp.TheActorFor(Story.TheVin)}");

			EmptyTheJournalOfThisVehicle(folder);
			Console.WriteLine("journal   : vaciado - esta corrida SIEMPRE borra el journal de este");
			Console.WriteLine("            vehiculo, porque un codigo de revision se toma una sola vez");

			DealershipCarApp app = Open(folder);
			var api = new DealershipCarAPI(app);

			Console.WriteLine($"modelo    : {api.ReadModel()}");
			Console.WriteLine($"entrada 0 : {app.Actor.CurrentEntryId}  (la semilla, que deja OnHydrated)");
			Console.WriteLine();

			Act(app, $"1. el fabricante abre {SpecSheet.TheFirstRevision} y publica adentro los siete items",
				() => SpecSheet.TheWholeSheetIsPublishedIntoTheFirstRevision(api));

			PrintTheSheet(api, "LA FICHA ENTERA, ADENTRO DE UN BORRADOR", TheFirstOnly);

			Console.WriteLine("   Los diez comandos de publicacion ya escribieron sus entradas y la ficha");
			Console.WriteLine("   esta en el disco. Ninguna lectura la deja salir: una revision sin sellar");
			Console.WriteLine("   se esta armando, y media hoja cargada no se puede citar. El grado del");
			Console.WriteLine("   aceite tampoco se filtra por la frase.");
			Console.WriteLine();

			Act(app, $"2. el fabricante SELLA {SpecSheet.TheFirstRevision}",
				() => SpecSheet.TheFirstRevisionIsSealed(api));

			PrintTheSheet(api, "LA MISMA FICHA, YA CITABLE", TheFirstOnly);

			Act(app, $"3. abre {SpecSheet.TheSecondRevision}, republica SOLO el aceite y la sella",
				() => SpecSheet.TheSecondRevisionChangesTheOilGrade(api));

			PrintTheSheet(api, "DOS REVISIONES SELLADAS, Y CADA ITEM DICE DE CUAL SALE", TheBoth);

			Console.WriteLine("   Una revision aplica desde su instante HACIA ADELANTE y no llega para atras:");
			Console.WriteLine("   el aceite pasa a salir de la segunda, y lo que ella no toco sigue saliendo");
			Console.WriteLine("   de la primera. Corregir un item no borra los demas.");
			Console.WriteLine();

			Act(app, $"4. SUSTITUYE {SpecSheet.TheFirstRevision} por {SpecSheet.TheSecondRevision}",
				() => SpecSheet.TheFirstRevisionIsSuperseded(api));

			string actorFolder = Path.Combine(folder, DealershipCarApp.TheActorFor(Story.TheVin));
			string bytesBeforeReading = TheTreeOf(actorFolder);
			long entryBeforeReading = app.Actor.CurrentEntryId;

			PrintTheSheet(api, "SUSTITUIDA LA PRIMERA: SOLO CONTESTA LO QUE LA SEGUNDA REPUBLICO", TheBoth);

			Console.WriteLine("   Y esta es la otra mitad: sustituida la primera, lo que la sucesora no");
			Console.WriteLine("   volvio a publicar se retira con ella. No queda en blanco por un error de");
			Console.WriteLine("   lectura -- se dice que no hay nada publicado, que es una respuesta.");
			Console.WriteLine();

			Console.WriteLine("---- mediciones contra el disco -------------------------------");
			Console.WriteLine();
			Console.WriteLine("la ficha de arriba, leida entera:");
			Console.WriteLine(
				$"  entrada del journal       : {entryBeforeReading} -> {app.Actor.CurrentEntryId}");
			Console.WriteLine($"  arbol byte a byte igual   : {TheTreeOf(actorFolder) == bytesBeforeReading}");
			Console.WriteLine("  (las lecturas son consultas y no comandos: no dejan entrada. Con un");
			Console.WriteLine("   PerformCommand donde va PerformQuery, esta linea diria False y el");
			Console.WriteLine("   journal de la persona creceria en cada apertura de la app)");
			Console.WriteLine();

			string oil = api.DescribeFluid(SpecSheet.TheFluidKind, TheVoiceOfTheDealership);
			string current = TheCurrentRevisionCodeOf(api);
			string firstState = api.ReadRevisionState(SpecSheet.TheFirstRevision);

			app.Dispose();

			TheSameAnswersAfterReopening(folder, oil, current, firstState);
			return 0;
		}

		private static readonly string[] TheFirstOnly = { SpecSheet.TheFirstRevision };

		private static readonly string[] TheBoth =
			{ SpecSheet.TheFirstRevision, SpecSheet.TheSecondRevision };

		private static void PrintTheSheet(
			DealershipCarAPI api, string moment, string[] revisionsOpenedSoFar)
		{
			Console.WriteLine("================================================================");
			Console.WriteLine($"  {moment}");
			Console.WriteLine("================================================================");
			Console.WriteLine();

			Console.WriteLine($"   modelo                   : {api.ReadModel()}");
			Console.WriteLine($"   hay revision sellada     : {api.HasASealedRevision()}");

			if (api.HasASealedRevision())
			{
				Console.WriteLine($"   revision vigente         : {api.ReadCurrentRevisionCode()}");
				Console.WriteLine(
					$"   publicada el             : {Day(api.ReadCurrentRevisionPublishedAt())}");
			}
			else
			{
				Console.WriteLine("   revision vigente         : ninguna - pedir su codigo o su instante");
				Console.WriteLine("                              aca REVIENTA, y por eso se pregunta antes");
			}

			foreach (string revision in revisionsOpenedSoFar)
				Console.WriteLine($"   {revision,-24} : {api.ReadRevisionState(revision)}");

			Console.WriteLine();

			TheFluid(api);
			TheTyre(api);
			TheFuel(api);
			TheServiceInterval(api);
			TheServicePrice(api);
			TheCoverage(api);
			TheServicingPolicy(api);
		}

		private static void TheFluid(DealershipCarAPI api)
		{
			AnAnswer(
				"aceite de motor",
				api.IsFluidPublished(SpecSheet.TheFluidKind),
				api.ReadFluidRevision(SpecSheet.TheFluidKind),
				api.DescribeFluid(SpecSheet.TheFluidKind, TheVoiceOfTheDealership));
		}

		private static void TheTyre(DealershipCarAPI api)
		{
			AnAnswer(
				"llanta delantera",
				api.IsTyrePublished(SpecSheet.TheTyrePosition),
				api.ReadTyreRevision(SpecSheet.TheTyrePosition),
				api.DescribeTyre(SpecSheet.TheTyrePosition, TheVoiceOfTheDealership));
		}

		private static void TheFuel(DealershipCarAPI api)
		{
			AnAnswer(
				"combustible", api.IsFuelPublished(), api.ReadFuelRevision(),
				api.DescribeFuel(TheVoiceOfTheDealership));

			AListOfIts("grado minimo", TheOneOrNone(api.ReadMinimumFuelGrade()));
			AListOfIts("permitidos", api.PermittedFuelGrades());
			AListOfIts("prohibidos", api.ProhibitedFuelGrades());
			Console.WriteLine();
		}

		private static void TheServiceInterval(DealershipCarAPI api)
		{
			AnAnswer(
				"intervalo de servicio",
				api.IsServiceIntervalPublished(SpecSheet.TheOperation),
				api.ReadServiceIntervalRevision(SpecSheet.TheOperation),
				api.DescribeServiceInterval(
					SpecSheet.TheOperation, SpecSheet.TheDistanceRemaining, SpecSheet.UnitA,
					TheVoiceOfTheDealership));
		}

		private static void TheServicePrice(DealershipCarAPI api)
		{
			AnAnswer(
				"precio del servicio",
				api.IsServicePricePublished(
					SpecSheet.TheOperation, SpecSheet.TheMilestone, SpecSheet.UnitA),
				api.ReadServicePriceRevision(
					SpecSheet.TheOperation, SpecSheet.TheMilestone, SpecSheet.UnitA),
				api.DescribeServicePrice(
					SpecSheet.TheOperation, SpecSheet.TheMilestone, SpecSheet.UnitA,
					SpecSheet.TheDayTheVehicleAsks, TheVoiceOfTheDealership));
		}

		private static void TheCoverage(DealershipCarAPI api)
		{
			AnAnswer(
				"garantia - el alcance",
				api.IsCoveragePublished(),
				api.ReadCoverageRevision(),
				api.DescribeCoverageSummary(TheVoiceOfTheDealership));

			AListOfIts("sistemas adentro", api.CoveredSystems());
			AListOfIts("exclusiones", api.CoverageExclusions(TheVoiceOfTheDealership));
			Console.WriteLine(
				$"   {"lo conoce el catalogo",-24} : "
				+ $"{api.TheVocabularyIsTheManufacturers(SpecSheet.TheSystem)}");

			bool canBeEvaluated = TheCoverageOfTheSystem(api.CanCoverageBeEvaluated);

			Console.WriteLine($"   {"se puede evaluar",-24} : {canBeEvaluated}");
			Console.WriteLine(
				$"   {"donde esta parado",-24} : "
				+ (canBeEvaluated
					? TheCoverageOfTheSystem(api.ReadCoverageStanding)
					: "no se pudo evaluar - pedir la postura aca REVIENTA"));

			Console.WriteLine($"     {TheCoverageOfTheSystemSaid(api)}");
			Console.WriteLine();
		}

		private static void TheServicingPolicy(DealershipCarAPI api)
		{
			AnAnswer(
				"servicio en otro taller",
				api.IsServicingPolicyPublished(),
				api.ReadServicingPolicyRevision(),
				api.DescribeServicingElsewhereRule(
					SpecSheet.TheExternalServicesWithEvidence,
					SpecSheet.TheExternalServicesWithoutEvidence,
					SpecSheet.TheMissedMaintenance, TheVoiceOfTheDealership));
		}

		private static T TheCoverageOfTheSystem<T>(
			Func<string, int, int, string, DateTime, int, int, string, DateTime, T> read)
		{
			return read(
				SpecSheet.TheSystem,
				SpecSheet.TheEnvelopeMonths, SpecSheet.TheEnvelopeDistance, SpecSheet.UnitA,
				Story.Delivery, Story.OdometerAtDelivery,
				SpecSheet.TheOdometerToday, SpecSheet.UnitA,
				SpecSheet.TheDayTheVehicleAsks);
		}

		private static string TheCoverageOfTheSystemSaid(DealershipCarAPI api)
		{
			return api.DescribeCoverage(
				SpecSheet.TheSystem,
				SpecSheet.TheEnvelopeMonths, SpecSheet.TheEnvelopeDistance, SpecSheet.UnitA,
				Story.Delivery, Story.OdometerAtDelivery,
				SpecSheet.TheOdometerToday, SpecSheet.UnitA,
				SpecSheet.TheDayTheVehicleAsks, TheVoiceOfTheDealership);
		}

		private static void AnAnswer(string label, bool published, string revision, string sentence)
		{
			Console.WriteLine($"   {label,-24} : {(published ? "publicado" : "no publicado"),-13}"
				+ (revision.Length > 0 ? "de la revision " + revision : "sin revision que nombrar"));
			Console.WriteLine($"     {sentence}");
			Console.WriteLine();
		}

		private static void AListOfIts(string label, string[] values)
		{
			Console.WriteLine($"   {label,-24} : "
				+ (values.Length == 0 ? "(lista vacia)" : string.Join(" | ", values)));
		}

		private static string[] TheOneOrNone(string value)
		{
			return value.Length == 0 ? new string[0] : new[] { value };
		}

		private static string TheCurrentRevisionCodeOf(DealershipCarAPI api)
		{
			return api.HasASealedRevision() ? api.ReadCurrentRevisionCode() : "ninguna";
		}

		private static void TheSameAnswersAfterReopening(
			string folder, string oil, string current, string firstState)
		{
			DealershipCarApp reopened = Open(folder);
			var again = new DealershipCarAPI(reopened);

			Console.WriteLine("reabrir el journal y volver a preguntar lo mismo:");
			Console.WriteLine($"  entrada del journal       : {reopened.Actor.CurrentEntryId}"
				+ "  (crecio una: OnHydrated deja la semilla en CADA arranque)");
			Console.WriteLine($"  revision vigente          : {TheCurrentRevisionCodeOf(again)}"
				+ $"  (antes {current})");
			Console.WriteLine($"  {SpecSheet.TheFirstRevision,-24}  : "
				+ $"{again.ReadRevisionState(SpecSheet.TheFirstRevision)}  (antes {firstState})");
			Console.WriteLine($"  la frase del aceite igual : "
				+ $"{again.DescribeFluid(SpecSheet.TheFluidKind, TheVoiceOfTheDealership) == oil}  <- letra por letra");
			Console.WriteLine();
			Console.WriteLine("  La procedencia no es un campo de C#: sale del journal, y la linea de");
			Console.WriteLine("  publicacion se vuelve a armar corriendo otra vez los mismos comandos.");
			Console.WriteLine();

			reopened.Dispose();
		}

		private static int TheWarrantyEnvelopePublishedAndReadBack(string folder)
		{
			Console.WriteLine("Concesionario - el sobre de garantia, publicado y leido de vuelta");
			Console.WriteLine();
			Console.WriteLine($"folder    : {folder}");
			Console.WriteLine($"vin       : {Story.TheVin}");
			Console.WriteLine($"actor     : {DealershipCarApp.TheActorFor(Story.TheVin)}");

			EmptyTheJournalOfThisVehicle(folder);
			Console.WriteLine("journal   : vaciado - esta corrida SIEMPRE borra el journal de este");
			Console.WriteLine("            vehiculo, porque el alcance de garantia se publica adentro");
			Console.WriteLine("            de una revision y un codigo de revision se toma una sola vez");

			DealershipCarApp app = Open(folder);
			var api = new DealershipCarAPI(app);

			Console.WriteLine($"modelo    : {api.ReadModel()}");
			Console.WriteLine($"entrada 0 : {app.Actor.CurrentEntryId}  (la semilla, que deja OnHydrated)");
			Console.WriteLine();
			Console.WriteLine("Ningun instante de esta corrida sale de un reloj: los cuatro momentos y");
			Console.WriteLine("las fechas de las lecturas estan escritos, para que dos corridas contesten");
			Console.WriteLine("lo mismo. Todos los hechos los dice la agencia; a la persona no se le");
			Console.WriteLine("pregunta ni uno solo sobre su propio vehiculo.");
			Console.WriteLine();

			PrintTheStanding(api, "ANTES DE QUE LA AGENCIA DIGA NADA", Warranty.TheInstantInside);

			Console.WriteLine("   El registro vacio no suspende la pregunta del sobre: que nadie haya");
			Console.WriteLine("   publicado uno es algo que el registro sabe con certeza, asi que se");
			Console.WriteLine("   puede evaluar y la respuesta tiene nombre. Se lee raro una sola vez.");
			Console.WriteLine();

			Act(app, "1. la agencia registra la entrega y publica el sobre",
				() => Warranty.TheDeliveryIsRecordedAndTheEnvelopeIsPublished(api));

			Act(app, $"2. el fabricante publica y SELLA el alcance en {Warranty.TheRevision}",
				() => Warranty.TheWarrantyScopeIsPublishedAndSealed(api));

			Console.WriteLine("3. el feed trae cuatro lecturas del odometro. La primera es ANTERIOR a la");
			Console.WriteLine("   entrega, que es lo que hace un vehiculo de demostracion");
			foreach ((int value, DateTime observedAt, string source)
				in Warranty.TheReadingsTheFeedBrings())
				Console.WriteLine($"     llega {value,6} {Warranty.TheUnit}  observada el "
					+ $"{Day(observedAt)}  de {source}");
			Console.WriteLine($"   (la caratula manda '{Warranty.TheUnit}', que es el nombre del miembro,");
			Console.WriteLine("    porque no referencia el dominio y no puede nombrar sus tipos. El");
			Console.WriteLine("    dominio la dice 'mi' cuando compone una frase: es la misma unidad");
			Console.WriteLine("    dicha dos veces, una a la maquina y otra a la persona)");
			Console.WriteLine();

			Act(app, "   aplicadas al dominio, que resuelve cual sirve para cada instante",
				() => Warranty.TheOdometerReportsArrive(api));

			PrintTheStanding(api, "ADENTRO DE LAS DOS MITADES", Warranty.TheInstantInside);

			TheSameSentenceAskedBothWays(api);

			PrintTheStanding(
				api, "PASADA LA MITAD DE DISTANCIA", Warranty.TheInstantPastTheDistanceHalf);

			Console.WriteLine("   La de distancia llego primero y la de tiempo todavia no: en un solo");
			Console.WriteLine("   registro esas dos no se pueden invertir mas tarde, porque el odometro");
			Console.WriteLine("   no baja y un limite alcanzado sigue alcanzado. Para que la de tiempo");
			Console.WriteLine("   llegue sola, lo que cambia es el sobre.");
			Console.WriteLine();

			Act(app,
				$"4. la agencia publica el sobre otra vez: {Warranty.TheMonths} meses igual, "
					+ $"{Warranty.TheSecondDistance} {Warranty.TheUnit} de distancia",
				() => Warranty.TheEnvelopeIsPublishedAgainWithMoreDistance(api));

			return TheLastMomentAndWhatTheDiskSays(app, api, folder);
		}

		private static int TheLastMomentAndWhatTheDiskSays(
			DealershipCarApp app, DealershipCarAPI api, string folder)
		{
			string actorFolder = Path.Combine(folder, DealershipCarApp.TheActorFor(Story.TheVin));
			string bytesBeforeReading = TheTreeOf(actorFolder);
			long entryBeforeReading = app.Actor.CurrentEntryId;

			PrintTheStanding(api, "PASADA LA MITAD DE TIEMPO", Warranty.TheInstantPastTheTimeHalf);

			Console.WriteLine("   El plazo se sigue contando desde la ENTREGA y no desde la publicacion,");
			Console.WriteLine("   asi que el sobre nuevo no lo movio. Cada frase nombra el sobre contra");
			Console.WriteLine("   el que se midio, con su fecha de publicacion adentro: una publicacion");
			Console.WriteLine("   rige hacia adelante y no alcanza a la respuesta que ya se dio.");
			Console.WriteLine();

			Console.WriteLine("---- mediciones contra el disco -------------------------------");
			Console.WriteLine();
			Console.WriteLine("el momento de arriba, leido entero:");
			Console.WriteLine(
				$"  entrada del journal       : {entryBeforeReading} -> {app.Actor.CurrentEntryId}");
			Console.WriteLine($"  arbol byte a byte igual   : {TheTreeOf(actorFolder) == bytesBeforeReading}");
			Console.WriteLine("  (las seis lecturas compuestas son consultas y no comandos, aunque cada");
			Console.WriteLine("   una nombre dos o tres raices globales adentro de un mismo script:");
			Console.WriteLine("   preguntar donde esta parado el carro no deja entrada en su journal)");
			Console.WriteLine();

			TheOneHoleInTheSentence(api);

			string standing = api.DescribeWarrantyEnvelope(
				Warranty.TheInstantPastTheTimeHalf, TheVoiceOfTheDealership);
			string published = api.DescribePublishedWarrantyEnvelope(TheVoiceOfTheDealership);

			app.Dispose();

			TheSameEnvelopeAnswersAfterReopening(folder, standing, published);
			return 0;
		}

		private const string ANothingThatBlowsUp =
			"no hay - pedirlo aca REVIENTA, y por eso se pregunta antes";

		private static void PrintTheStanding(DealershipCarAPI api, string moment, DateTime asOf)
		{
			Console.WriteLine("================================================================");
			Console.WriteLine($"  {moment}");
			Console.WriteLine($"  preguntado al {Day(asOf)}");
			Console.WriteLine("================================================================");
			Console.WriteLine();

			WhatTheRecordHolds(api, asOf);
			ThePublishedHalfAlone(api);
			WhereThisVehicleStands(api, asOf);
			WhatTheWarrantyCoversForThisVehicle(api, asOf);
		}

		private static void WhatTheRecordHolds(DealershipCarAPI api, DateTime asOf)
		{
			bool published = api.IsWarrantyEnvelopePublished();
			bool delivered = api.HasDeliveryBeenRecorded();
			bool measured = api.HasAnOdometerReading(asOf);

			Console.WriteLine("   -- lo que el registro tiene. Las tres preguntas son TOTALES --");
			Console.WriteLine($"   {"sobre publicado",-24} : {published}");
			Console.WriteLine($"   {"publicado el",-24} : "
				+ (published ? Day(api.ReadEnvelopePublishedAt()) : ANothingThatBlowsUp));
			Console.WriteLine($"   {"entrega registrada",-24} : {delivered}");
			Console.WriteLine($"   {"entregado el",-24} : "
				+ (delivered ? Day(api.ReadDeliveredAt()) : ANothingThatBlowsUp));
			Console.WriteLine($"   {"odometro de la entrega",-24} : "
				+ (delivered
					? api.ReadDeliveryReading() + " " + api.ReadDeliveryUnit()
					: ANothingThatBlowsUp));
			Console.WriteLine($"   {"hay lectura al instante",-24} : {measured}");
			Console.WriteLine($"   {"odometro",-24} : "
				+ (measured
					? api.ReadOdometer(asOf) + " " + api.ReadOdometerUnit(asOf)
					: ANothingThatBlowsUp));
			Console.WriteLine($"   {"observado el",-24} : "
				+ (measured ? Day(api.ReadOdometerObservedAt(asOf)) : ANothingThatBlowsUp));
			Console.WriteLine();
		}

		private static void ThePublishedHalfAlone(DealershipCarAPI api)
		{
			Console.WriteLine("   -- la mitad publicada, que cita quien no tiene el odometro de nadie --");
			Console.WriteLine($"     {api.DescribePublishedWarrantyEnvelope(TheVoiceOfTheDealership)}");
			Console.WriteLine();
		}

		private static void WhereThisVehicleStands(DealershipCarAPI api, DateTime asOf)
		{
			bool canBeEvaluated = api.CanTheEnvelopeBeEvaluated(asOf);

			Console.WriteLine("   -- donde esta parado ESTE vehiculo contra su sobre --");
			Console.WriteLine($"   {"se puede evaluar",-24} : {canBeEvaluated}");
			Console.WriteLine($"   {"donde esta parado",-24} : "
				+ (canBeEvaluated
					? api.ReadEnvelopeStanding(asOf)
					: "no se pudo decidir - pedir el nombre aca REVIENTA"));
			Console.WriteLine($"     {api.DescribeWarrantyEnvelope(asOf, TheVoiceOfTheDealership)}");
			Console.WriteLine();
		}

		private static void WhatTheWarrantyCoversForThisVehicle(
			DealershipCarAPI api, DateTime asOf)
		{
			Console.WriteLine($"   -- la cobertura de {Warranty.TheSystem} para ESTE vehiculo --");

			if (!api.IsWarrantyEnvelopePublished()
				|| !api.HasDeliveryBeenRecorded()
				|| !api.HasAnOdometerReading(asOf))
			{
				Console.WriteLine($"   {"frase compuesta",-24} : no se puede pedir. Al registro le falta");
				Console.WriteLine("                              el sobre, la entrega o la lectura, y el");
				Console.WriteLine("                              catalogo del fabricante es de un MODELO:");
				Console.WriteLine("                              no sabe que le falta a un carro. Las dos");
				Console.WriteLine("                              mitades si se contestan, cada una entera");
				Console.WriteLine("                              y dicha por el dominio, sin armar aca una");
				Console.WriteLine("                              tercera con pedazos de las dos:");
				Console.WriteLine($"     [lo publicado] {api.DescribeCoverageSummary(TheVoiceOfTheDealership)}");
				Console.WriteLine($"     [este carro]   {api.DescribeWarrantyEnvelope(asOf, TheVoiceOfTheDealership)}");
				Console.WriteLine();
				return;
			}

			bool canBeEvaluated = api.CanCoverageBeEvaluatedForThisVehicle(Warranty.TheSystem, asOf);

			Console.WriteLine($"   {"se puede evaluar",-24} : {canBeEvaluated}");
			Console.WriteLine($"   {"donde esta parado",-24} : "
				+ (canBeEvaluated
					? api.ReadCoverageStandingForThisVehicle(Warranty.TheSystem, asOf)
					: "no se pudo evaluar - pedir el nombre aca REVIENTA"));
			Console.WriteLine($"     {api.DescribeCoverageForThisVehicle(
				Warranty.TheSystem, asOf, TheVoiceOfTheDealership)}");
			Console.WriteLine();
		}

		private static void TheSameSentenceAskedBothWays(DealershipCarAPI api)
		{
			string composed = api.DescribeCoverageForThisVehicle(
				Warranty.TheSystem, Warranty.TheInstantInside, TheVoiceOfTheDealership);
			string byHand = TheCoverageWithTheNineFactsSaid(api);

			Console.WriteLine("----------------------------------------------------------------");
			Console.WriteLine("  LA MISMA FRASE, PEDIDA DE LAS DOS MANERAS");
			Console.WriteLine("----------------------------------------------------------------");
			Console.WriteLine();
			Console.WriteLine("   Arriba la puerta contesto con el sistema y el instante y nada mas: los");
			Console.WriteLine("   otros siete hechos los saco del registro. Aca se le entregan a mano los");
			Console.WriteLine("   nueve, los mismos que esta caratula publico -- que es lo que puede");
			Console.WriteLine("   hacer quien monta un escenario, y no quien solo tiene el carro de una");
			Console.WriteLine("   persona enfrente.");
			Console.WriteLine();
			Console.WriteLine("   con dos argumentos:");
			Console.WriteLine($"     {composed}");
			Console.WriteLine("   con los nueve hechos a mano:");
			Console.WriteLine($"     {byHand}");
			Console.WriteLine($"   {"iguales letra por letra",-24} : {composed == byHand}");
			Console.WriteLine($"   {"el nombre, de las dos",-24} : "
				+ api.ReadCoverageStandingForThisVehicle(
					Warranty.TheSystem, Warranty.TheInstantInside)
				+ " / "
				+ TheCoverageWithTheNineFactsHandedOver(api, api.ReadCoverageStanding));
			Console.WriteLine();
			Console.WriteLine("   Esa igualdad es lo que prueba que la puerta COMPUSO en vez de inventar:");
			Console.WriteLine("   si hubiera puesto un cero por un hecho que no tiene, o la lectura de la");
			Console.WriteLine("   entrega en lugar de la de hoy, la linea diria False y la frase seguiria");
			Console.WriteLine("   leyendose igual de bien.");
			Console.WriteLine();
		}

		private static string TheCoverageWithTheNineFactsSaid(DealershipCarAPI api)
		{
			return api.DescribeCoverage(
				Warranty.TheSystem,
				Warranty.TheMonths, Warranty.TheFirstDistance, Warranty.TheUnit,
				Warranty.TheDelivery, Warranty.TheOdometerAtDelivery,
				Warranty.TheOdometerInside, Warranty.TheUnit,
				Warranty.TheInstantInside, TheVoiceOfTheDealership);
		}

		private static T TheCoverageWithTheNineFactsHandedOver<T>(
			DealershipCarAPI api,
			Func<string, int, int, string, DateTime, int, int, string, DateTime, T> read)
		{
			return read(
				Warranty.TheSystem,
				Warranty.TheMonths, Warranty.TheFirstDistance, Warranty.TheUnit,
				Warranty.TheDelivery, Warranty.TheOdometerAtDelivery,
				Warranty.TheOdometerInside, Warranty.TheUnit,
				Warranty.TheInstantInside);
		}

		private static void TheOneHoleInTheSentence(DealershipCarAPI api)
		{
			DateTime asOf = Warranty.TheInstantBeforeTheDelivery;

			Console.WriteLine("---- el unico hueco conocido de la superficie ------------------");
			Console.WriteLine();
			Console.WriteLine($"preguntado al {Day(asOf)}, que es ANTES de la entrega. Al registro no le");
			Console.WriteLine("falta nada: tiene el sobre, la entrega y una lectura de ese instante.");
			Console.WriteLine();
			Console.WriteLine("  la frase del sobre CONTESTA:");
			Console.WriteLine($"    {api.DescribeWarrantyEnvelope(asOf, TheVoiceOfTheDealership)}");
			Console.WriteLine();
			Console.WriteLine("  la de cobertura RECHAZA:");
			Console.WriteLine($"    {WhatCameBackInsteadOfTheCoverageSentence(api, asOf)}");
			Console.WriteLine();
			Console.WriteLine("  Quedan mas meses de los que el plazo tiene, y eso no es una respuesta:");
			Console.WriteLine("  es el registro contradiciendose consigo mismo, con una lectura anterior");
			Console.WriteLine("  a la entrega que el dominio si acepta. Las dos lecturas se comportan");
			Console.WriteLine("  distinto y ninguna de las dos es del que pregunta: la entrega la dijo la");
			Console.WriteLine("  agencia y el instante es del calendario, asi que no hay a quien");
			Console.WriteLine("  devolverle el defecto. Y esta caratula no puede nombrar por su tipo lo");
			Console.WriteLine("  que la segunda le devuelve. Esta medido y anotado como hueco abierto.");
			Console.WriteLine();
		}

		private static string WhatCameBackInsteadOfTheCoverageSentence(
			DealershipCarAPI api, DateTime asOf)
		{
			try
			{
				return api.DescribeCoverageForThisVehicle(
				Warranty.TheSystem, asOf, TheVoiceOfTheDealership);
			}
			catch (Exception ex)
			{
				return $"{ex.GetType().Name}: {ex.Message}";
			}
		}

		private static void TheSameEnvelopeAnswersAfterReopening(
			string folder, string standing, string published)
		{
			DealershipCarApp reopened = Open(folder);
			var again = new DealershipCarAPI(reopened);

			Console.WriteLine("reabrir el journal y volver a preguntar lo mismo:");
			Console.WriteLine($"  entrada del journal       : {reopened.Actor.CurrentEntryId}"
				+ "  (crecio una: OnHydrated deja la semilla en CADA arranque)");
			Console.WriteLine("  la frase del sobre igual  : "
				+ $"{again.DescribeWarrantyEnvelope(
					Warranty.TheInstantPastTheTimeHalf, TheVoiceOfTheDealership) == standing}"
				+ "  <- letra por letra");
			Console.WriteLine("  la mitad publicada igual  : "
				+ $"{again.DescribePublishedWarrantyEnvelope(TheVoiceOfTheDealership) == published}  <- letra por letra");
			Console.WriteLine();
			Console.WriteLine("  El sobre no es un campo de C#: sale del journal, y las dos");
			Console.WriteLine("  publicaciones se vuelven a aplicar corriendo otra vez los mismos");
			Console.WriteLine("  comandos, en el mismo orden.");
			Console.WriteLine();

			reopened.Dispose();
		}

		private static int TheShopStaysRecordedAndReadBack(string folder)
		{
			Console.WriteLine("Concesionario - las estadias en el taller, registradas y leidas de vuelta");
			Console.WriteLine();
			Console.WriteLine($"folder    : {folder}");
			Console.WriteLine($"vin       : {Story.TheVin}");
			Console.WriteLine($"actor     : {DealershipCarApp.TheActorFor(Story.TheVin)}");

			EmptyTheJournalOfThisVehicle(folder);
			Console.WriteLine("journal   : vaciado - esta corrida SIEMPRE borra el journal de este");
			Console.WriteLine("            vehiculo, y es por el motivo de 'historia' y no por el de");
			Console.WriteLine("            'ficha': ningun hecho de una estadia toma una clave que se");
			Console.WriteLine("            gaste, asi que los tres se podrian volver a mandar. Lo que no");
			Console.WriteLine("            se repite es el ESTADO que deja la ultima, que queda abierta:");
			Console.WriteLine("            una segunda corrida sin borrar chocaria con ella");

			DealershipCarApp app = Open(folder);
			var api = new DealershipCarAPI(app);

			Console.WriteLine($"entrada 0 : {app.Actor.CurrentEntryId}  (la semilla, que deja OnHydrated)");
			Console.WriteLine();
			Console.WriteLine("Ningun instante y ningun dia de esta corrida sale de un reloj: los cinco");
			Console.WriteLine("instantes y los cinco dias estan escritos, para que dos corridas contesten");
			Console.WriteLine("lo mismo. Los dias los DICE la sucursal y no se sacan de los instantes:");
			Console.WriteLine("sacarlos pediria una zona horaria que este registro no lleva.");
			Console.WriteLine();

			PrintTheStays(api, "ANTES DE QUE NINGUNA SUCURSAL DIGA NADA");

			foreach ((string what, Action<DealershipCarAPI> act) in ShopStays.TheTwoClosedStays())
				Act(app, "   llega " + what, () => act(api));

			PrintTheStays(api, "DOS ESTADIAS CERRADAS: CINCO DIAS EN DOS ESTADIAS");

			Console.WriteLine("   La respuesta son los DIAS y no una duracion. Los cinco se pueden nombrar");
			Console.WriteLine("   uno por uno y cruzar con el calendario de cualquiera -- el de la escuela,");
			Console.WriteLine("   el de un viaje --, que es lo que una duracion no puede hacer.");
			Console.WriteLine();

			Act(app, "   llega una TERCERA entrada que nadie cierra: el carro queda adentro",
				() => ShopStays.TheThirdStayEntersAndStaysOpen(api));

			PrintTheStays(api, "Y UNA TERCERA ABIERTA, QUE SEIS DE LAS OCHO LECTURAS NO VEN");

			Console.WriteLine("   Las seis lecturas por DIAS contestan igual que antes: una estadia abierta");
			Console.WriteLine("   todavia no tiene ningun dia -- los dias llegan con la entrega -- asi que");
			Console.WriteLine("   ningun periodo dicho en dias la alcanza. Las dos por INSTANTE si.");
			Console.WriteLine();

			TheSecondEntryTheDoorRefusesForFree(app, api, folder);
			TheStayThatThePeriodCutsInHalf(api);

			return TheLastMomentOfTheStaysAndWhatTheDiskSays(app, api, folder);
		}

		private static void PrintTheStays(DealershipCarAPI api, string moment)
		{
			Console.WriteLine("================================================================");
			Console.WriteLine($"  {moment}");
			Console.WriteLine($"  el periodo preguntado va del {ShopStays.TheYearFrom} al {ShopStays.TheYearTo}");
			Console.WriteLine("================================================================");
			Console.WriteLine();

			TheSixPeriodReads(api);
			TheTwoInstantReads(api);
			TheRowsOfThePeriod(api);
		}

		private static void TheSixPeriodReads(DealershipCarAPI api)
		{
			string from = ShopStays.TheYearFrom;
			string to = ShopStays.TheYearTo;

			Console.WriteLine("   -- las SEIS lecturas por DIAS. Las seis son TOTALES --");
			Console.WriteLine($"   {"hay alguna estadia",-24} : {api.HasAnyStayBetween(from, to)}");
			Console.WriteLine($"   {"cuantos dias",-24} : {api.DaysInTheShopCountBetween(from, to)}");
			Console.WriteLine($"   {"cuantas estadias",-24} : {api.StayCountBetween(from, to)}");
			Console.WriteLine($"   {"los dias, uno por uno",-24} : "
				+ ADayList(api.DaysInTheShopBetween(from, to)));
			Console.WriteLine($"     {api.DescribeDaysInTheShopBetween(from, to)}");
			Console.WriteLine();
		}

		private static void TheTwoInstantReads(DealershipCarAPI api)
		{
			Console.WriteLine("   -- las DOS lecturas por INSTANTE, que son las unicas que ven una");
			Console.WriteLine("      estadia todavia abierta --");

			foreach (DateTime asOf in new[] { ShopStays.TheFirstEntry, TheInstantTheStaysAreRead })
			{
				Console.WriteLine($"   al {Day(asOf)}");
				Console.WriteLine($"   {"esta adentro",-24} : {api.IsInsideTheShopAt(asOf)}");
				Console.WriteLine($"     {api.DescribeTheStayAt(asOf)}");
			}

			Console.WriteLine();
		}

		private static void TheRowsOfThePeriod(DealershipCarAPI api)
		{
			ShopStayRecord record = api.StaysBetween(ShopStays.TheYearFrom, ShopStays.TheYearTo);

			Console.WriteLine("   -- el registro crudo: una fila por estadia CERRADA, en columnas");
			Console.WriteLine("      paralelas y en orden de entrada --");

			if (record.Branch.Length == 0) Console.WriteLine("   (ninguna fila todavia)");

			foreach (int row in Enumerable.Range(0, record.Branch.Length))
			{
				Console.WriteLine($"   {record.EnteredAt[row],-21} {record.Branch[row]}");
				Console.WriteLine($"   {"",-21} {record.Subject[row]}");
				Console.WriteLine($"   {"",-21} dias: {record.CoveredDays[row]}");
				Console.WriteLine($"   {"",-21} {record.Says[row]}");
			}

			Console.WriteLine();
		}

		private static void TheSecondEntryTheDoorRefusesForFree(
			DealershipCarApp app, DealershipCarAPI api, string folder)
		{
			string actorFolder = Path.Combine(folder, DealershipCarApp.TheActorFor(Story.TheVin));
			string bytesBefore = TheTreeOf(actorFolder);
			long entryBefore = app.Actor.CurrentEntryId;

			string refusal = WhatThePreCheckSaysAbout(
				() => api.RecordShopEntry("Taller de otra ciudad", "Lo que sea", TheInstantTheStaysAreRead));

			Console.WriteLine("una SEGUNDA entrada con una estadia ya abierta:");
			Console.WriteLine($"  la puerta contesta        : {refusal}");
			Console.WriteLine($"  entrada del journal       : {entryBefore} -> {app.Actor.CurrentEntryId}");
			Console.WriteLine($"  arbol byte a byte igual   : {TheTreeOf(actorFolder) == bytesBefore}");
			Console.WriteLine("  (la puerta pregunta IsInsideTheShopAt ANTES de performar nada, que es el");
			Console.WriteLine("   unico momento en que rechazar todavia es gratis. La invariante sigue");
			Console.WriteLine("   siendo del dominio; dejar que la rechace EL sale caro, porque adentro de");
			Console.WriteLine("   un comando la entrada del journal ya esta escrita y el rechazo queda");
			Console.WriteLine("   ahi para siempre. Por el feed no hay a quien preguntarle antes, y por");
			Console.WriteLine("   eso esa es la unica ruta por la que esta misma ley cicatriza)");
			Console.WriteLine();
		}

		private static void TheStayThatThePeriodCutsInHalf(DealershipCarAPI api)
		{
			string from = ShopStays.TheCutFrom;
			string to = ShopStays.TheCutTo;

			ShopStayRecord record = api.StaysBetween(from, to);

			Console.WriteLine($"un periodo que CORTA una estadia, del {from} al {to}:");
			Console.WriteLine($"  dias del periodo          : {ADayList(api.DaysInTheShopBetween(from, to))}");
			Console.WriteLine($"  cuantos                   : {api.DaysInTheShopCountBetween(from, to)}");

			foreach (int row in Enumerable.Range(0, record.Branch.Length))
				Console.WriteLine($"  dias de la FILA           : {record.CoveredDays[row]}");

			Console.WriteLine("  (la fila trae la estadia ENTERA y el total viene recortado al periodo, y");
			Console.WriteLine("   tiene que ser asi: la frase de la propia fila nombra los tres dias, y una");
			Console.WriteLine("   celda recortada contradiria a su vecina en la misma fila)");
			Console.WriteLine();
		}

		private static int TheLastMomentOfTheStaysAndWhatTheDiskSays(
			DealershipCarApp app, DealershipCarAPI api, string folder)
		{
			string actorFolder = Path.Combine(folder, DealershipCarApp.TheActorFor(Story.TheVin));
			string bytesBeforeReading = TheTreeOf(actorFolder);
			long entryBeforeReading = app.Actor.CurrentEntryId;

			string days = ADayList(
				api.DaysInTheShopBetween(ShopStays.TheYearFrom, ShopStays.TheYearTo));
			string period = api.DescribeDaysInTheShopBetween(
				ShopStays.TheYearFrom, ShopStays.TheYearTo);
			string open = api.DescribeTheStayAt(TheInstantTheStaysAreRead);

			Console.WriteLine("---- mediciones contra el disco -------------------------------");
			Console.WriteLine();
			Console.WriteLine("las ocho lecturas, leidas enteras:");
			Console.WriteLine(
				$"  entrada del journal       : {entryBeforeReading} -> {app.Actor.CurrentEntryId}");
			Console.WriteLine($"  arbol byte a byte igual   : {TheTreeOf(actorFolder) == bytesBeforeReading}");
			Console.WriteLine("  (las ocho son consultas y no comandos: preguntar que dias estuvo el carro");
			Console.WriteLine("   en el taller no deja entrada en su journal)");
			Console.WriteLine();

			app.Dispose();

			DealershipCarApp reopened = Open(folder);
			var again = new DealershipCarAPI(reopened);

			Console.WriteLine("reabrir el journal y volver a preguntar lo mismo:");
			Console.WriteLine($"  entrada del journal       : {reopened.Actor.CurrentEntryId}"
				+ "  (crecio una: OnHydrated deja la semilla en CADA arranque)");
			Console.WriteLine("  los dias iguales          : "
				+ $"{ADayList(again.DaysInTheShopBetween(ShopStays.TheYearFrom, ShopStays.TheYearTo)) == days}"
				+ "  <- uno por uno");
			Console.WriteLine("  la frase del periodo igual: "
				+ $"{again.DescribeDaysInTheShopBetween(ShopStays.TheYearFrom, ShopStays.TheYearTo) == period}"
				+ "  <- letra por letra");
			Console.WriteLine("  la estadia abierta igual  : "
				+ $"{again.DescribeTheStayAt(TheInstantTheStaysAreRead) == open}  <- letra por letra");
			Console.WriteLine();
			Console.WriteLine("  Las estadias no son un campo de C#: salen del journal, y los tres hechos");
			Console.WriteLine("  se vuelven a aplicar corriendo otra vez los mismos comandos, en el mismo");
			Console.WriteLine("  orden. La tercera sigue abierta despues del replay.");
			Console.WriteLine();

			reopened.Dispose();
			return 0;
		}

		private static int TheCostLedgerRecordedAndReadBack(string folder)
		{
			Console.WriteLine("Concesionario - el libro de costos, registrado y leido de vuelta");
			Console.WriteLine();
			Console.WriteLine($"folder    : {folder}");
			Console.WriteLine($"vin       : {Story.TheVin}");
			Console.WriteLine($"actor     : {DealershipCarApp.TheActorFor(Story.TheVin)}");

			EmptyTheJournalOfThisVehicle(folder);
			Console.WriteLine("journal   : vaciado - esta corrida SIEMPRE borra el journal de este");
			Console.WriteLine("            vehiculo, y es por el motivo de 'historia' y no por el de");
			Console.WriteLine("            'ficha': ningun hecho del libro toma una clave que se gaste,");
			Console.WriteLine("            asi que los tres se podrian volver a mandar. Borrar es lo que");
			Console.WriteLine("            hace la corrida reproducible, y aca ni siquiera hay un estado");
			Console.WriteLine("            con que chocar: una entrada del libro no tiene clave, asi que");
			Console.WriteLine("            una segunda corrida sin borrar dejaria seis gastos, que es lo");
			Console.WriteLine("            correcto. Mas abajo esta medido");

			DealershipCarApp app = Open(folder);
			var api = new DealershipCarAPI(app);

			Console.WriteLine($"entrada 0 : {app.Actor.CurrentEntryId}  (la semilla, que deja OnHydrated)");
			Console.WriteLine();
			Console.WriteLine("Ningun instante y ninguna cifra de esta corrida sale de un reloj ni de una");
			Console.WriteLine("cuenta: los tres montos, sus tres instantes, la vida util de las llantas y");
			Console.WriteLine("los cuatro extremos de los dos periodos estan escritos, para que dos");
			Console.WriteLine("corridas contesten lo mismo.");
			Console.WriteLine();

			PrintTheLedger(api, "ANTES DE QUE NINGUNA SUCURSAL REGISTRE NADA");

			Act(app, "   llegan las TRES lecturas del odometro del anio",
				() => Costs.TheThreeReadingsOfTheYear(api));

			Console.WriteLine("   Las lecturas no son un gasto: son el tramo de uso contra el que se puede");
			Console.WriteLine("   dividir el gasto del periodo. Sin ellas el costo por distancia contesta la");
			Console.WriteLine("   razon con nombre de que no hay tramo, que es una respuesta y no un hueco.");
			Console.WriteLine();

			foreach ((string what, Action<DealershipCarAPI> act) in Costs.TheThreeCosts())
				Act(app, "   llega " + what, () => act(api));

			PrintTheLedger(api, "TRES GASTOS, UNO DURADERO Y DOS RECURRENTES, EN DOS MONEDAS");

			Console.WriteLine("   La respuesta son LAS DOS VISTAS en una sola frase, siempre juntas. Lo que");
			Console.WriteLine("   salio del banco adentro del periodo y lo que el periodo consumio: sin");
			Console.WriteLine("   separar lo duradero de lo recurrente, un total dice 'el carro se puso");
			Console.WriteLine("   caro' cuando la verdad es 'compro llantas'. Ninguna lectura contesta una");
			Console.WriteLine("   vista sola, y no hay ninguna que diga si vale la pena: las cifras con la");
			Console.WriteLine("   distincion que las hace legibles son toda la respuesta.");
			Console.WriteLine();

			TheSameLedgerAskedOverTheColonesHalf(api);
			TheTwoPeriodsOfSpendCompared(api);
			TheSecondEntryOfTheSameCostThatNobodyRefuses(app, api, folder);

			return TheLastMomentOfTheLedgerAndWhatTheDiskSays(app, api, folder);
		}

		private static void PrintTheLedger(DealershipCarAPI api, string moment)
		{
			Console.WriteLine("================================================================");
			Console.WriteLine($"  {moment}");
			Console.WriteLine($"  el periodo preguntado va del {Instant(Costs.TheYearFrom)}");
			Console.WriteLine($"  al {Instant(Costs.TheYearTo)}");
			Console.WriteLine("================================================================");
			Console.WriteLine();

			TheSevenPeriodReads(api, Costs.TheYearFrom, Costs.TheYearTo);
			TheCostPerDistanceOfThePeriod(api, Costs.TheYearFrom, Costs.TheYearTo);
			TheRowsOfTheLedger(api, Costs.TheYearFrom, Costs.TheYearTo);
		}

		private static void TheSevenPeriodReads(DealershipCarAPI api, DateTime from, DateTime to)
		{
			Console.WriteLine("   -- las lecturas del periodo. TODAS son TOTALES: la ausencia es un tipo y");
			Console.WriteLine("      dos monedas son un estado, asi que ninguna rechaza nada --");
			Console.WriteLine($"   {"hay algun gasto",-24} : {api.HasAnyCostBetween(from, to)}");
			Console.WriteLine($"   {"en una sola moneda",-24} : {api.IsSpendSaidInOneCurrencyBetween(from, to)}");
			Console.WriteLine($"   {"cuantos gastos",-24} : {api.CostEntryCountBetween(from, to)}");
			Console.WriteLine($"     el gasto, entero  : {api.DescribeSpendBetween(from, to, TheVoiceOfTheDealership)}");
			Console.WriteLine($"     moneda por moneda : {api.DescribeSpendPerCurrencyBetween(from, to, TheVoiceOfTheDealership)}");
			Console.WriteLine();
		}

		private static void TheCostPerDistanceOfThePeriod(
			DealershipCarAPI api, DateTime from, DateTime to)
		{
			Console.WriteLine("   -- lo que costo la distancia, cruzando DOS globales de raiz adentro del");
			Console.WriteLine("      dominio: el tramo lo dice la agenda y el dinero el libro. Ni la");
			Console.WriteLine("      distancia ni el ancla salen por ninguna lectura como numero --");
			Console.WriteLine($"   {"se puede decir",-24} : {api.CanCostPerDistanceBeSaid(from, to)}");
			Console.WriteLine($"     {api.DescribeCostPerDistanceBetween(from, to, TheVoiceOfTheDealership)}");
			Console.WriteLine();
		}

		private static void TheRowsOfTheLedger(DealershipCarAPI api, DateTime from, DateTime to)
		{
			CostLedgerRecord record = api.CostEntriesBetween(from, to, TheVoiceOfTheDealership);

			Console.WriteLine("   -- el registro crudo: una fila por gasto incurrido adentro, en columnas");
			Console.WriteLine("      paralelas y en orden del instante en que se incurrio --");

			if (record.Says.Length == 0) Console.WriteLine("   (ninguna fila todavia)");

			foreach (int row in Enumerable.Range(0, record.Says.Length))
			{
				Console.WriteLine($"   {record.IncurredAt[row],-21} {record.Subject[row]}");
				Console.WriteLine($"   {"",-21} {record.Money[row]}   {record.Classification[row]}"
					+ (record.ExpectedLife[row].Length == 0 ? "" : "   " + record.ExpectedLife[row]));
				Console.WriteLine($"   {"",-21} {record.Says[row]}");
			}

			Console.WriteLine();
			Console.WriteLine("   (el dinero llega DICHO por su propia moneda, con su signo y sus");
			Console.WriteLine("    decimales. Esta caratula no lo vuelve a escribir: un segundo formateador");
			Console.WriteLine("    seria un segundo autor de como se dice una cifra)");
			Console.WriteLine();
		}

		private static void TheSameLedgerAskedOverTheColonesHalf(DealershipCarAPI api)
		{
			Console.WriteLine("================================================================");
			Console.WriteLine("  EL MISMO LIBRO, PREGUNTADO POR EL SEMESTRE QUE SOLO TIENE COLONES");
			Console.WriteLine($"  del {Instant(Costs.TheColonesHalfFrom)} al {Instant(Costs.TheColonesHalfTo)}");
			Console.WriteLine();
			Console.WriteLine("  Es donde las dos lecturas del gasto DIFIEREN: con dos monedas la de por");
			Console.WriteLine("  moneda contesta letra por letra lo mismo que la entera, porque el total");
			Console.WriteLine("  que las separaria es justo el que no hay. Con una sola, la de por moneda");
			Console.WriteLine("  agrega su propia clausula.");
			Console.WriteLine("================================================================");
			Console.WriteLine();

			TheSevenPeriodReads(api, Costs.TheColonesHalfFrom, Costs.TheColonesHalfTo);
			TheRowsOfTheLedger(api, Costs.TheColonesHalfFrom, Costs.TheColonesHalfTo);
		}

		private static void TheTwoPeriodsOfSpendCompared(DealershipCarAPI api)
		{
			Console.WriteLine("================================================================");
			Console.WriteLine("  LOS DOS PERIODOS, EL ANIO CONTRA EL ANTERIOR");
			Console.WriteLine();
			Console.WriteLine("  El PRIMER par de extremos es el primer periodo, y ese orden es parte del");
			Console.WriteLine("  contrato: el dominio dice el segundo contra el primero y cruzarlos daria");
			Console.WriteLine("  la frase al reves. El anio anterior no tiene ningun gasto, y tambien se");
			Console.WriteLine("  dice: una constancia con nombre y nunca un cero, porque un cero de dinero");
			Console.WriteLine("  se lee como un gasto que hubo.");
			Console.WriteLine("================================================================");
			Console.WriteLine();
			Console.WriteLine($"   primer periodo, del      : {Instant(Costs.TheYearFrom)} al {Instant(Costs.TheYearTo)}");
			Console.WriteLine($"   segundo periodo, del     : {Instant(Costs.TheEarlierYearFrom)} al {Instant(Costs.TheEarlierYearTo)}");
			Console.WriteLine();
			Console.WriteLine($"   los dos comparados       : {api.DescribeTwoPeriodsOfSpendCompared(Costs.TheYearFrom, Costs.TheYearTo, Costs.TheEarlierYearFrom, Costs.TheEarlierYearTo, TheVoiceOfTheDealership)}");
			Console.WriteLine();
			Console.WriteLine($"   el anio anterior solo    : {api.DescribeSpendBetween(Costs.TheEarlierYearFrom, Costs.TheEarlierYearTo, TheVoiceOfTheDealership)}");
			Console.WriteLine();
		}

		private static void TheSecondEntryOfTheSameCostThatNobodyRefuses(
			DealershipCarApp app, DealershipCarAPI api, string folder)
		{
			string actorFolder = Path.Combine(folder, DealershipCarApp.TheActorFor(Story.TheVin));
			string bytesBefore = TheTreeOf(actorFolder);
			long entryBefore = app.Actor.CurrentEntryId;
			int countBefore = api.CostEntryCountBetween(Costs.TheYearFrom, Costs.TheYearTo);

			string answer = WhatTheDoorSaysAboutASecondCost(() => Costs.TheOilChangeArrives(api));

			Console.WriteLine("el MISMO gasto registrado una SEGUNDA vez, identico en monto, asunto e");
			Console.WriteLine("instante:");
			Console.WriteLine($"  la puerta contesta        : {answer}");
			Console.WriteLine($"  cuantos gastos            : {countBefore} -> {api.CostEntryCountBetween(Costs.TheYearFrom, Costs.TheYearTo)}");
			Console.WriteLine($"  entrada del journal       : {entryBefore} -> {app.Actor.CurrentEntryId}");
			Console.WriteLine($"  arbol byte a byte igual   : {TheTreeOf(actorFolder) == bytesBefore}");
			Console.WriteLine("  (es lo contrario de lo que pasa con una estadia, y a proposito: una");
			Console.WriteLine("   entrada del libro no tiene clave. Dos gastos del mismo monto en el mismo");
			Console.WriteLine("   asunto y el mismo dia son DOS gastos y no una correccion, asi que no hay");
			Console.WriteLine("   nada que rechazar y no hay ningun estado con que chocar. Por eso esta");
			Console.WriteLine("   corrida no tiene ninguna pre-consulta: no habria nada que preguntar)");
			Console.WriteLine();
		}

		private static int TheLastMomentOfTheLedgerAndWhatTheDiskSays(
			DealershipCarApp app, DealershipCarAPI api, string folder)
		{
			string actorFolder = Path.Combine(folder, DealershipCarApp.TheActorFor(Story.TheVin));
			string bytesBeforeReading = TheTreeOf(actorFolder);
			long entryBeforeReading = app.Actor.CurrentEntryId;

			string spend = api.DescribeSpendBetween(Costs.TheYearFrom, Costs.TheYearTo, TheVoiceOfTheDealership);
			string perCurrency =
				api.DescribeSpendPerCurrencyBetween(Costs.TheColonesHalfFrom, Costs.TheColonesHalfTo, TheVoiceOfTheDealership);
			string perDistance = api.DescribeCostPerDistanceBetween(
				Costs.TheYearFrom, Costs.TheYearTo, TheVoiceOfTheDealership);
			string bothPeriods = api.DescribeTwoPeriodsOfSpendCompared(
				Costs.TheYearFrom, Costs.TheYearTo, Costs.TheEarlierYearFrom, Costs.TheEarlierYearTo,
				TheVoiceOfTheDealership);

			Console.WriteLine("---- mediciones contra el disco -------------------------------");
			Console.WriteLine();
			Console.WriteLine("las nueve lecturas, leidas enteras:");
			Console.WriteLine(
				$"  entrada del journal       : {entryBeforeReading} -> {app.Actor.CurrentEntryId}");
			Console.WriteLine($"  arbol byte a byte igual   : {TheTreeOf(actorFolder) == bytesBeforeReading}");
			Console.WriteLine("  (las nueve son consultas y no comandos: preguntar en que se fue la plata");
			Console.WriteLine("   no deja entrada en el journal de este vehiculo)");
			Console.WriteLine();

			app.Dispose();

			DealershipCarApp reopened = Open(folder);
			var again = new DealershipCarAPI(reopened);

			Console.WriteLine("reabrir el journal y volver a preguntar lo mismo:");
			Console.WriteLine($"  entrada del journal       : {reopened.Actor.CurrentEntryId}"
				+ "  (crecio una: OnHydrated deja la semilla en CADA arranque)");
			Console.WriteLine("  el gasto igual            : "
				+ $"{again.DescribeSpendBetween(Costs.TheYearFrom, Costs.TheYearTo, TheVoiceOfTheDealership) == spend}"
				+ "  <- letra por letra");
			Console.WriteLine("  por moneda igual          : "
				+ $"{again.DescribeSpendPerCurrencyBetween(Costs.TheColonesHalfFrom, Costs.TheColonesHalfTo, TheVoiceOfTheDealership) == perCurrency}"
				+ "  <- letra por letra");
			Console.WriteLine("  por distancia igual       : "
				+ $"{again.DescribeCostPerDistanceBetween(Costs.TheYearFrom, Costs.TheYearTo, TheVoiceOfTheDealership) == perDistance}"
				+ "  <- letra por letra");
			Console.WriteLine("  los dos periodos iguales  : "
				+ $"{again.DescribeTwoPeriodsOfSpendCompared(Costs.TheYearFrom, Costs.TheYearTo, Costs.TheEarlierYearFrom, Costs.TheEarlierYearTo, TheVoiceOfTheDealership) == bothPeriods}"
				+ "  <- letra por letra");
			Console.WriteLine();
			Console.WriteLine("  Los gastos no son un campo de C#: salen del journal, y los cuatro hechos");
			Console.WriteLine("  se vuelven a aplicar corriendo otra vez los mismos comandos, en el mismo");
			Console.WriteLine("  orden. La parte que a cada periodo le toca de la duradera se deriva en");
			Console.WriteLine("  cada lectura y no se guarda en ningun lado.");
			Console.WriteLine();

			reopened.Dispose();
			return 0;
		}

		/// <summary>Un instante dicho como instante, con su hora, igual que lo dice el dominio.</summary>
		private static string Instant(DateTime instant)
		{
			return instant.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// Lo que la puerta contesta a un comando que ESTA corrida espera que pase. Es el gemelo de
		/// WhatThePreCheckSaysAbout y dice lo contrario: alla el rechazo es el resultado y aca lo es
		/// que no lo haya. Solo atrapa la forma en que la puerta rechaza antes de performar nada.
		/// </summary>
		private static string WhatTheDoorSaysAboutASecondCost(Action call)
		{
			try
			{
				call();
			}
			catch (InvalidOperationException refused)
			{
				return $"LO RECHAZO: {refused.GetType().Name} - {FirstLineOf(refused.Message)}";
			}

			return "lo registro, y no hay nada que rechazar";
		}

		/// <summary>
		/// El instante al que esta caratula pregunta si el carro esta adentro. Esta ESCRITO y no sale
		/// de ningun reloj, como todo lo demas de esta corrida.
		/// </summary>
		private static readonly DateTime TheInstantTheStaysAreRead =
			new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);

		private static string ADayList(string[] days)
		{
			return days.Length == 0 ? "(ninguno)" : string.Join(" ", days);
		}

		private static string WhatThePreCheckSaysAbout(Action call)
		{
			try
			{
				call();
			}
			catch (InvalidOperationException refused)
			{
				return $"{refused.GetType().Name} - {FirstLineOf(refused.Message)}";
			}

			return "NO LO RECHAZO (y deberia)";
		}

		private static async Task<int> TheRefreshLoopAgainstTheDealership(string[] args)
		{
			string folder  = AFolder(args, 1);
			string baseUrl = AnArgument(args, 2, "http://localhost:8092/");
			string resource = AnArgument(args, 3, "vehicle");
			string id      = AnArgument(args, 4, Story.TheVin);

			Console.WriteLine("Concesionario - el refresh contra la API de eventos del concesionario");
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
				var credential = new TheCredentialThisRunHolds();

				await TheAppGetsItsOwnCredential(credential, tokens, resource, id);

				var loop = new BucleDeRefresh(
					api,
					new FuenteHttpDeEventos(http, resource, id,
						ct => TheAppGetsItsOwnCredential(credential, tokens, resource, id, ct),
						ct => RenewTheCredential(credential, tokens, resource, id, ct)),
					DealershipEventRoute.Route);

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
				Ingested whole = await TheWholeBlockAgainFromZero(api, http, credential, tokens, resource, id);

				Console.WriteLine("-- el bloque ENTERO re-entregado desde el evento 0 --");
				Console.WriteLine($"   {whole}");
				Console.WriteLine($"   entradas del journal      : {entryBefore} -> {app.Actor.CurrentEntryId}");
				Console.WriteLine($"   arbol byte a byte igual   : {TheTreeOf(actorFolder) == bytesBefore}");
				Console.WriteLine();

				WhatTheFeedPublishedAboutTheModel(api);

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

				await TheDecoyAnswers403(http, credential, resource, id);

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

			Console.WriteLine("Concesionario - un corte a mitad de bloque");
			Console.WriteLine();
			Console.WriteLine($"folder    : {folder}");

			using (var http = new HttpClient { BaseAddress = new Uri(baseUrl) })
			{
				DealershipCarApp app = Open(folder);
				var api = new DealershipCarAPI(app);
				var tokens = new ProveedorDeToken(http);
				var credential = new TheCredentialThisRunHolds();

				await TheAppGetsItsOwnCredential(credential, tokens, resource, id);

				var source = new FuenteHttpDeEventos(http, resource, id,
					ct => TheAppGetsItsOwnCredential(credential, tokens, resource, id, ct));
				IReadOnlyList<SenalDeEvento> block =
					(await source.LeerDesde(api.LastEventApplied(), CancellationToken.None)).Senales;

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

		private static void WhatTheFeedPublishedAboutTheModel(DealershipCarAPI api)
		{
			Console.WriteLine("-- la ficha del fabricante, tal como la dejo el feed --");
			Console.WriteLine();
			Console.WriteLine($"   {"modelo",-24} : {api.ReadModel()}");
			Console.WriteLine($"   {"hay revision sellada",-24} : {api.HasASealedRevision()}");

			if (!api.HasASealedRevision())
			{
				Console.WriteLine();
				Console.WriteLine("   Este feed no publico ninguna revision, o publico una sin sellar.");
				Console.WriteLine("   Para que llegue una: dotnet run --project InyectorDeEventosDemo");
				Console.WriteLine("   -- ficha, y despues este mismo refresco otra vez.");
				Console.WriteLine();
				return;
			}

			Console.WriteLine($"   {"revision vigente",-24} : {api.ReadCurrentRevisionCode()}");
			Console.WriteLine(
				$"   {"publicada el",-24} : {Day(api.ReadCurrentRevisionPublishedAt())}");
			Console.WriteLine();

			TheFluid(api);
			TheTyre(api);
			TheFuel(api);
			TheServiceInterval(api);
			TheServicePrice(api);
			TheServicingPolicy(api);

			Console.WriteLine("   La garantia NO se imprime aca, y se dice en vez de omitirla en");
			Console.WriteLine("   silencio: su lectura toma nueve hechos de ESTE vehiculo -- el sobre,");
			Console.WriteLine("   la entrega, el odometro -- y esta caratula no tiene ninguno. Los que");
			Console.WriteLine("   usa 'ficha' son del escenario que ella misma monta, y ponerlos aca");
			Console.WriteLine("   seria afirmar hechos inventados sobre este carro.");
			Console.WriteLine();
			Console.WriteLine("   Los once comandos de publicacion los corrio la INGESTA, no un script");
			Console.WriteLine("   nuestro: cada hecho de la ficha llego como su propia senal, con su");
			Console.WriteLine("   propio numero de evento, y avanzo el punto de control en el mismo");
			Console.WriteLine("   comando que lo aplico.");
			Console.WriteLine();
		}

		private static void WhatTheRefreshLoopNeededAndWhetherTheDoorAnswersItNow()
		{
			Console.WriteLine("BucleDeRefresh(diario, fuente, ruta, flush)");
			Console.WriteLine();
			Console.WriteLine("  flush     : null, y es lo correcto para esta caratula: existe para bajar");
			Console.WriteLine("              de MEMFS a la carpeta del usuario en wasm, y aca el journal");
			Console.WriteLine("              ya esta en disco.");
			Console.WriteLine("  fuente    : FuenteHttpDeEventos - la API de eventos del concesionario, y");
			Console.WriteLine("              es ELLA la que sostiene la credencial y la rota: quien lee");
			Console.WriteLine("              por HTTP necesita un token, quien lee por otro medio no.");
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
			Console.WriteLine("  tras una caida -- y vive en Logbook, una SEGUNDA global de raiz al lado de");
			Console.WriteLine("  la agenda y nunca adentro: el punto de control no es un hecho de");
			Console.WriteLine("  mantenimiento. MaintenanceSchedule no cambio.");
			Console.WriteLine();
			Console.WriteLine("  LO QUE YA NO ESTA: TokenGuardado. Hasta el 2026-09-03 el bucle le pedia la");
			Console.WriteLine("  credencial AL JOURNAL, y por eso quedaba en claro en la carpeta y viajaba");
			Console.WriteLine("  con cualquier copia. Un token es contexto de sesion del host, no historia");
			Console.WriteLine("  de la persona: ahora la sostiene la fuente y el diario no la conoce.");
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
			DealershipCarAPI api, HttpClient http, TheCredentialThisRunHolds credential,
			ProveedorDeToken tokens, string resource, string id)
		{
			var source = new FuenteHttpDeEventos(http, resource, id,
				ct => TheAppGetsItsOwnCredential(credential, tokens, resource, id, ct));
			IReadOnlyList<SenalDeEvento> block =
				(await source.LeerDesde(0, CancellationToken.None)).Senales;

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

		// LA CREDENCIAL DE ESTA CORRIDA, en memoria y no en el journal. Hasta el 2026-09-03
		// la sostenia el Logbook y se journalizaba: quedaba en claro en la carpeta y viajaba
		// con cualquier copia del expediente. Un token es contexto de sesion de quien hospeda
		// la app -- aca, esta consola -- y no historia de la persona, asi que muere con el
		// proceso, que es exactamente lo que se quiere de el.
		private sealed class TheCredentialThisRunHolds
		{
			internal const string DeviceKey = "device-consola-de-medicion";

			internal string AccessToken { get; private set; } = "";

			internal string RefreshToken { get; private set; } = "";

			internal void Store(Credencial granted)
			{
				AccessToken = granted.AccessToken;
				RefreshToken = granted.RefreshToken ?? "";
			}
		}

		private static async Task<string> TheAppGetsItsOwnCredential(
			TheCredentialThisRunHolds credential, ProveedorDeToken tokens, string resource, string id,
			CancellationToken ct = default)
		{
			if (credential.AccessToken.Length > 0) return credential.AccessToken;

			Credencial granted = await tokens.Pedir(TheCredentialThisRunHolds.DeviceKey, resource, id, ct);
			Console.WriteLine($"credencial: scope={granted.Scope} expiresIn={granted.ExpiresIn}s");
			Console.WriteLine();
			credential.Store(granted);
			return credential.AccessToken;
		}

		private static async Task<string> RenewTheCredential(
			TheCredentialThisRunHolds credential, ProveedorDeToken tokens, string resource, string id,
			CancellationToken ct)
		{
			string refresh = credential.RefreshToken;

			Credencial renewed = refresh.Length > 0 ? await tokens.Rotar(refresh, ct) : null;
			string how = renewed != null ? "rotada con el refresh token" : "nueva contra el device key";
			if (renewed == null)
				renewed = await tokens.Pedir(TheCredentialThisRunHolds.DeviceKey, resource, id, ct);

			Console.WriteLine($"   credencial renovada: {how} (scope {renewed.Scope})");
			credential.Store(renewed);
			return credential.AccessToken;
		}

		private static async Task TheDecoyAnswers403(
			HttpClient http, TheCredentialThisRunHolds credential, string resource, string id)
		{
			Console.WriteLine("-- el senuelo: la app NO es cliente de los servicios del concesionario --");

			foreach (string route in new[] { $"{resource}/{id}/events?after=0", $"{resource}/{id}/customer" })
			{
				using (var request = new HttpRequestMessage(HttpMethod.Get, route))
				{
					request.Headers.Authorization =
						new AuthenticationHeaderValue("Bearer", credential.AccessToken);

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

		/// <summary>
		/// EL IDIOMA EN QUE ESTE ARNES PIDE LAS FRASES DEL DOMINIO: el del registro, porque lo que
		/// mide es el dominio del concesionario y no la pantalla de nadie.
		/// </summary>
		private const string TheVoiceOfTheDealership = Concesionario.App.Wording.Phrases.LanguageOfTheRecord;

		/// <summary>La situacion con que la puerta nombra una clase que no tiene operacion agendada.</summary>
		private const string TheStandingOfAKindWithNoOperation = "NoOperationScheduled";

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
			Console.WriteLine("Concesionario - caratula de consola, para medir el dominio contra un disco de verdad");
			Console.WriteLine();
			Console.WriteLine("  historia [carpeta]   la historia de seis meses, desde un journal vacio");
			Console.WriteLine("                       (es lo que corre 'dotnet run' sin argumentos)");
			Console.WriteLine("  estado   [carpeta]   reabre ese journal y reporta - correr en proceso NUEVO");
			Console.WriteLine("  ficha    [carpeta]   publica la ficha del fabricante en dos revisiones y la");
			Console.WriteLine("                       lee de vuelta, cada respuesta con su procedencia");
			Console.WriteLine("                       (TAMBIEN borra el journal de este vehiculo)");
			Console.WriteLine("  garantia [carpeta]   publica el sobre de garantia de este vehiculo y lee");
			Console.WriteLine("                       donde esta parado en cuatro momentos: antes de que la");
			Console.WriteLine("                       agencia diga nada, adentro, pasada la distancia y");
			Console.WriteLine("                       pasado el plazo");
			Console.WriteLine("                       (TAMBIEN borra el journal de este vehiculo)");
			Console.WriteLine("  taller   [carpeta]   registra tres estadias en el taller -- una entera de un");
			Console.WriteLine("                       solo acto, una en dos hechos y una que queda ABIERTA --");
			Console.WriteLine("                       y lee las ocho lecturas en cada momento");
			Console.WriteLine("                       (TAMBIEN borra el journal de este vehiculo)");
			Console.WriteLine("  costos   [carpeta]   registra tres gastos -- uno DURADERO con su vida util y");
			Console.WriteLine("                       dos RECURRENTES, en DOS monedas -- y lee las nueve");
			Console.WriteLine("                       lecturas del libro en cada momento");
			Console.WriteLine("                       (TAMBIEN borra el journal de este vehiculo)");
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
