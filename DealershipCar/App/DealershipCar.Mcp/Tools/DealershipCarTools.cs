using System.ComponentModel;
using System.Text.Json.Serialization;
using DealershipCar.App;
using ModelContextProtocol.Server;
using Puppeteer;

namespace DealershipCar.Mcp.Tools
{
	// LA SUPERFICIE QUE VE LA IA.
	//
	// Son intencionales en el sentido fuerte: las escribe el autor de la app, igual que un [HttpGet],
	// y la app decide que expone. Deliberadamente NO existe una tool generica Query(puppet,
	// operation, parameters): eso le daria a la IA una consola de DSL en vez de un dominio, tiraria a
	// la basura el descubrimiento por schema, y expondria los puppets. La unidad descubrible es la
	// aplicacion.
	//
	// EL [Description] NO ES DOCUMENTACION: ES la semantica que lee la IA, lo unico que tiene para
	// decidir si esta tool contesta la pregunta de la persona. Por eso esta redactado como la pregunta
	// que una persona se hace sobre sus propios datos, y no como el nombre de un metodo.
	//
	// EN INGLES, y es la decision 6 de T7 aplicada un piso mas arriba: ReadDueReason y cada nombre de
	// DueState los compone EL DOMINIO en ingles (OUS-07 dice que componer el motivo es del dominio y
	// no de la vista), asi que envolver esa carga util en prosa espanola seria ruido. Los comentarios
	// siguen en espanol, como manda el modulo.
	//
	// LO QUE NO ESTA, Y NO ESTA A PROPOSITO:
	//
	//  - LOS TRES COMANDOS Y LAS TRES CREDENCIALES. StoredToken, StoredRefreshToken y StoredDeviceKey
	//    son la credencial de la persona, guardada EN CLARO en el journal (deuda conocida y aceptada,
	//    plan §8) en una carpeta que este proceso ademas lee. Publicarselas a un asistente seria un
	//    agujero nuevo, no una funcionalidad.
	//  - Evaluate, Apply, RegisterDevice, StoreCredential y los nueve comandos del dominio. Evaluate
	//    parece una lectura y NO lo es: es un comando, transiciona estados y escribe. Ningun camino
	//    desde estas tools llega a un PerformCommand.
	//
	// LAS TOOLS SON PREGUNTAS, NO LECTURAS SUELTAS, y la razon es de correccion y no de gusto: el
	// lector se rehidrata en CADA consulta, asi que una tool por lectura de §4 haria que la IA
	// compusiera una sola respuesta a partir de VARIOS estados del journal -- la app instalada escribe
	// mientras este proceso vive. Cada tool contesta desde UNA rehidratacion. Ver CONTRADICTIONS.md
	// C30; las ocho lecturas de §4 siguen todas alcanzables, y cual las contesta esta dicho ahi.
	[McpServerToolType]
	internal sealed class DealershipCarTools
	{
		private readonly JournalReader reader;

		public DealershipCarTools(JournalReader reader)
		{
			ArgumentNullException.ThrowIfNull(reader);

			this.reader = reader;
		}

		// UseStructuredContent es el ultimo tramo de "sin strings en ninguna frontera semantica". Sin
		// el, el SDK serializa el record a JSON y lo manda como un bloque de TEXTO: la IA recibe un
		// string que tiene que volver a parsear. Con el, el resultado viaja en structuredContent y la
		// tool publica un outputSchema, asi que un int llega como int.
		[McpServerTool(Name = "review_my_maintenance", UseStructuredContent = true)]
		[Description("Answers what this person's vehicle needs right now: which maintenance "
			+ "operations are due, which are past their grace period, which count as missed, and what "
			+ "the schedule says about each one. Use it when they ask something general like 'does my "
			+ "car need anything?', 'what's overdue?', 'what should I do first?' or 'how is my car "
			+ "doing?'. It reports the whole schedule in one read, so prefer it over asking about "
			+ "operations one at a time. Every operation carries the reason the schedule itself "
			+ "composed - say that reason, do not reword it into generic advice. Operations this "
			+ "vehicle never had scheduled say so in their reason; they are not overdue, they simply "
			+ "do not exist for this car.")]
		public MaintenanceReview Review()
		{
			return reader.Read(TheWholeSchedule, () => new MaintenanceReview
			{
				HasData = false,
				Folder = reader.Folder,
				Detail = TheAppHasNotBeenOpened,
			});
		}

		[McpServerTool(Name = "check_one_operation", UseStructuredContent = true)]
		[Description("Answers where ONE maintenance operation stands on this person's vehicle - "
			+ "whether it is due, how far past its limit it is, and how many times they have already "
			+ "deferred it. Use it when they ask about a specific operation, like 'is my oil change "
			+ "due?', 'how long can I put off the inspection?' or 'did the recall get done?'. If they "
			+ "are asking about the vehicle in general, use review_my_maintenance instead: it answers "
			+ "for every operation in a single read. deferralsUsed is how many deferrals they have "
			+ "already spent on this operation; a safety operation cannot be deferred at all.")]
		public OperationStanding CheckOne(
			[Description("Which maintenance operation to look up.")] MaintenanceOperation operation)
		{
			// La rama de "vino vacio" no existe, y no es un descuido: con un enum el cliente MCP no
			// PUEDE mandar algo fuera del conjunto. Se borro porque es inalcanzable, que es justamente
			// lo que se compra al tipar.
			return reader.Read(
				api => OneOperation(api, operation),
				() => new OperationStanding
				{
					HasData = false,
					Operation = operation.WireName(),
					Folder = reader.Folder,
					Detail = TheAppHasNotBeenOpened,
				});
		}

		[McpServerTool(Name = "how_up_to_date_is_this", UseStructuredContent = true)]
		[Description("Answers how far this person's app has read the dealership's record, and up to "
			+ "what instant the schedule has been evaluated. Use it when they expect to see something "
			+ "new and it is not there - after a service visit, or after they pressed Refresh - and "
			+ "when you need to tell whether the answer you just gave them is current. lastEventApplied "
			+ "is the dealership event number the app has read up to: if it goes up between two calls, "
			+ "new facts came in. You cannot make it go up; only the person's app can.")]
		public ReadingFreshness HowUpToDate()
		{
			return reader.Read(
				api =>
				{
					bool known = TheEventCheckpoint(api, out long lastEvent);

					return new ReadingFreshness
					{
						HasData = true,
						Vin = api.ReadVin(),
						LastEventApplied = lastEvent,
						LastEventKnown = known,
						Evaluated = api.HasBeenEvaluated(),
						EvaluatedUpTo = TheInstantItWasEvaluatedTo(api),
						Folder = reader.Folder,
						Detail = TheDetailWhen(known),
					};
				},
				() => new ReadingFreshness
				{
					HasData = false,
					Folder = reader.Folder,
					Detail = TheAppHasNotBeenOpened,
				});
		}

		// ------------------------------------------------------ lo que cada tool arma

		// TODA LA AGENDA DESDE UNA SOLA REHIDRATACION. Las nueve clases se recorren aca adentro, con
		// el mismo actor: partirlas en nueve consultas releeria el journal nueve veces y podria
		// mezclar dos estados.
		private MaintenanceReview TheWholeSchedule(DealershipCarAPI api)
		{
			var operations = new List<OperationStanding>();

			foreach (MaintenanceOperation each in TheOperationKind.All)
				operations.Add(new OperationStanding
				{
					HasData = true,
					Operation = each.WireName(),
					State = api.ReadDueState(each.WireName()),
					Reason = api.ReadDueReason(each.WireName()),
					DeferralsUsed = api.DeferralsUsed(each.WireName()),
				});

			bool known = TheEventCheckpoint(api, out long lastEvent);

			return new MaintenanceReview
			{
				HasData = true,
				Vin = api.ReadVin(),
				AnythingDue = api.HasAnythingDue(),
				NextToDo = TheNextThingToDo(api),
				DueCount = api.CountDue(),
				OverdueCount = api.CountOverdue(),
				MissedCount = api.MissedCount(),
				Evaluated = api.HasBeenEvaluated(),
				EvaluatedUpTo = TheInstantItWasEvaluatedTo(api),
				LastEventApplied = lastEvent,
				LastEventKnown = known,
				Operations = operations,
				Folder = reader.Folder,
				Detail = TheDetailWhen(known),
			};
		}

		private OperationStanding OneOperation(DealershipCarAPI api, MaintenanceOperation operation)
		{
			string kind = operation.WireName();
			bool known = TheEventCheckpoint(api, out long lastEvent);

			return new OperationStanding
			{
				HasData = true,
				Vin = api.ReadVin(),
				Operation = kind,
				State = api.ReadDueState(kind),
				Reason = api.ReadDueReason(kind),
				DeferralsUsed = api.DeferralsUsed(kind),
				Evaluated = api.HasBeenEvaluated(),
				EvaluatedUpTo = TheInstantItWasEvaluatedTo(api),
				LastEventApplied = lastEvent,
				LastEventKnown = known,
				Folder = reader.Folder,
				Detail = TheDetailWhen(known),
			};
		}

		// EL PUNTO DE CONTROL PUEDE NO EXISTIR, y no es un defecto nuestro ni un journal roto: es un
		// journal escrito por una version de la app ANTERIOR a la bitacora de eventos. Se encontro
		// apuntando el servidor a una carpeta de verdad -- ~/DealershipCar-journal, escrita en T7 --
		// y no lo podia encontrar ninguna prueba de las de aca, porque toda carpeta de prueba la
		// escribe la app de hoy.
		//
		// UN LECTOR NO PUEDE MIGRAR, y ahi esta el nudo. upgrade('bitacora-de-eventos') se aplica en
		// OnHydrated, y el OnHydrated de este host esta VACIO a proposito: crear la global que falta
		// seria escribir en el journal de la persona, que es exactamente lo que T9 promete no hacer.
		// Asi que la global sigue faltando por diseño hasta que la persona abra su app una vez.
		//
		// Y NO SE PUEDE CONTESTAR "no hay datos": la agenda SI esta -- upgrade('semilla') corrio hace
		// tiempo -- y la agenda es justo lo que la persona pregunta. Negarle todo por un campo que le
		// falta seria peor que decirle que ese campo le falta. Asi que se degrada de a una global.
		//
		// SE ATRAPA LANGUAGEEXCEPTION EN UNA SOLA LLAMADA, y eso es lo que lo separa de C28. Ahi la
		// regla es que un LanguageException NO se traga en un contador de rechazos, porque seria
		// nuestro propio error de tipeo del DSL disfrazado de hecho recibido. Aca el catch abraza UNA
		// linea, cuyo script ejercitan EveryScriptOfTheDoorHasBeenRun y las pruebas de T8 contra un
		// journal de verdad -- y la bandera se afirma en TRUE sobre un journal actual, asi que un
		// tipeo que la dejara siempre en false se pone rojo.
		private static bool TheEventCheckpoint(DealershipCarAPI api, out long lastEvent)
		{
			try
			{
				lastEvent = api.LastEventApplied();
				return true;
			}
			catch (LanguageException)
			{
				lastEvent = 0;
				return false;
			}
		}

		// LAS DOS LECTURAS GUARDADAS, PREGUNTADAS CON SU GUARDA. ReadNextDueKind y LastEvaluatedAt
		// tiran InvalidOperationException cuando no hay nada vencido / nunca se evaluo (E41, decision
		// 6 de T6), y una tool que dejara escapar esa excepcion la convertiria en un error del
		// PROTOCOLO: la IA veria "la herramienta fallo" donde el hecho es "no hay nada vencido". Se
		// contesta el lado vacio -- "" -- y el [Description] del campo dice que significa.
		private static string TheNextThingToDo(DealershipCarAPI api)
		{
			return api.HasAnythingDue() ? api.ReadNextDueKind() : "";
		}

		private static string TheInstantItWasEvaluatedTo(DealershipCarAPI api)
		{
			// El "O" lleva la Z que C24 peleo: la puerta vuelve a estampar DateTimeKind.Utc al salir
			// porque el journal lo borra, y perderla aca haria que la IA leyera un instante sin huso.
			return api.HasBeenEvaluated() ? api.LastEvaluatedAt().ToString("O") : "";
		}

		// Lo que hay que decirle a la persona, si algo. Las dos advertencias se acumulan en vez de
		// pisarse: las dos son ciertas a la vez y las dos cambian lo que corresponde contestar.
		private string TheDetailWhen(bool theEventCheckpointIsKnown)
		{
			var says = new List<string>();

			if (!theEventCheckpointIsKnown) says.Add(TheAppIsOlderThanTheLogbook);

			string many = MoreThanOneVehicle();
			if (many.Length > 0) says.Add(many);

			return string.Join(" ", says);
		}

		// Con mas de un vehiculo en la carpeta se contesta por el primero y SE DICE. Callarlo haria
		// que la persona leyera la agenda de otro auto creyendo que es la de este.
		private string MoreThanOneVehicle()
		{
			return reader.HowManyVehicles > 1
				? $"There are {reader.HowManyVehicles} vehicles in this folder. This answer is about "
					+ "the first one; say which VIN it is so the person can tell you if they meant "
					+ "another."
				: "";
		}

		private const string TheAppIsOlderThanTheLogbook =
			"This record was written by an older version of the app, which did not keep track of how "
			+ "far it had read the dealership's events, so lastEventApplied is not available. The "
			+ "maintenance schedule below is complete and current. Ask the person to open their app "
			+ "once and it will start keeping that count; nothing is lost, and it is not an error.";

		private const string TheAppHasNotBeenOpened =
			"The app has not been opened on this machine yet, so there is nothing recorded in this "
			+ "folder. Ask the person to open their DealershipCar app once; it is not an error, and "
			+ "it is not something you or the dealership can fix from here.";
	}

	// Tipos de retorno CON FORMA, no un string: el SDK genera el schema de salida desde esto y la IA
	// recibe structuredContent con enteros de verdad.
	internal sealed class OperationStanding
	{
		[JsonPropertyName("hasData")]
		[Description("false when the app has not recorded anything yet; then the rest comes empty.")]
		public bool HasData { get; init; }

		[JsonPropertyName("vin")]
		[Description("The vehicle this answer is about.")]
		public string Vin { get; init; } = "";

		[JsonPropertyName("operation")]
		[Description("The maintenance operation this entry is about.")]
		public string Operation { get; init; } = "";

		[JsonPropertyName("state")]
		[Description("Where this operation stands: NotDue, DueSoon (inside the courtesy warning, "
			+ "which is never a required stop), Due, Overdue (past the grace period), Deferred (the "
			+ "person put it off on purpose), Completed or Superseded (a plan revision replaced it). "
			+ "An operation this vehicle never had scheduled also reads NotDue - the reason is what "
			+ "tells the two apart.")]
		public string State { get; init; } = "";

		[JsonPropertyName("reason")]
		[Description("Why it stands there, in one sentence composed by the schedule itself. Say "
			+ "this; do not reword it into generic advice. If it begins 'There is no scheduled', this "
			+ "vehicle simply does not have that operation.")]
		public string Reason { get; init; } = "";

		[JsonPropertyName("deferralsUsed")]
		[Description("How many deferrals the person has already spent on this operation. There is a "
			+ "cap, and a safety operation cannot be deferred at all.")]
		public int DeferralsUsed { get; init; }

		[JsonPropertyName("evaluated")]
		[Description("true if the schedule has ever been evaluated. Until it has, every state is "
			+ "just how the plan was seeded.")]
		public bool Evaluated { get; init; }

		[JsonPropertyName("evaluatedUpTo")]
		[Description("The instant the schedule was evaluated up to, in UTC. Empty means it has never "
			+ "been evaluated - the app does that when the person opens it, and you cannot.")]
		public string EvaluatedUpTo { get; init; } = "";

		[JsonPropertyName("lastEventApplied")]
		[Description("The dealership event number the app has read up to.")]
		public long LastEventApplied { get; init; }

		[JsonPropertyName("lastEventKnown")]
		[Description("false when this record was written by an older version of the app, which did "
			+ "not keep that count. Then lastEventApplied is zero and means nothing - do not report "
			+ "it as 'zero events read'. Everything else in the answer is complete; read detail.")]
		public bool LastEventKnown { get; init; }

		[JsonPropertyName("folder")]
		[Description("The person's folder, where their app keeps this vehicle's record.")]
		public string Folder { get; init; } = "";

		[JsonPropertyName("detail")]
		[Description("Empty when everything is fine; otherwise what the person had better do.")]
		public string Detail { get; init; } = "";
	}

	internal sealed class MaintenanceReview
	{
		[JsonPropertyName("hasData")]
		[Description("false when the app has not recorded anything yet; then the rest comes empty.")]
		public bool HasData { get; init; }

		[JsonPropertyName("vin")]
		[Description("The vehicle this answer is about.")]
		public string Vin { get; init; } = "";

		[JsonPropertyName("anythingDue")]
		[Description("true if anything at all is due or past due on this vehicle.")]
		public bool AnythingDue { get; init; }

		[JsonPropertyName("nextToDo")]
		[Description("The operation to do first. EMPTY means nothing is due - that is an answer, not "
			+ "a missing value, and it must not be reported as a failure.")]
		public string NextToDo { get; init; } = "";

		[JsonPropertyName("dueCount")]
		[Description("How many operations are due or past due.")]
		public int DueCount { get; init; }

		[JsonPropertyName("overdueCount")]
		[Description("How many are past their grace period. These are the ones worth raising first.")]
		public int OverdueCount { get; init; }

		[JsonPropertyName("missedCount")]
		[Description("How many count as maintenance that was not done. It is derived from where the "
			+ "operations stand, so it goes back down when the work is actually done.")]
		public int MissedCount { get; init; }

		[JsonPropertyName("evaluated")]
		[Description("true if the schedule has ever been evaluated.")]
		public bool Evaluated { get; init; }

		[JsonPropertyName("evaluatedUpTo")]
		[Description("The instant the schedule was evaluated up to, in UTC. Empty means never. "
			+ "Everything below is as of that instant, not as of now: if it is old, say so rather "
			+ "than presenting the answer as today's.")]
		public string EvaluatedUpTo { get; init; } = "";

		[JsonPropertyName("lastEventApplied")]
		[Description("The dealership event number the app has read up to. If it does not go up, "
			+ "nothing new came in.")]
		public long LastEventApplied { get; init; }

		[JsonPropertyName("lastEventKnown")]
		[Description("false when this record was written by an older version of the app, which did "
			+ "not keep that count. Then lastEventApplied is zero and means nothing - do not report "
			+ "it as 'zero events read'. Everything else in the answer is complete; read detail.")]
		public bool LastEventKnown { get; init; }

		[JsonPropertyName("operations")]
		[Description("Where every maintenance operation stands. Operations this vehicle never had "
			+ "scheduled are listed too, and say so in their reason.")]
		public IReadOnlyList<OperationStanding> Operations { get; init; } = new List<OperationStanding>();

		[JsonPropertyName("folder")]
		[Description("The person's folder, where their app keeps this vehicle's record.")]
		public string Folder { get; init; } = "";

		[JsonPropertyName("detail")]
		[Description("Empty when everything is fine; otherwise what the person had better do.")]
		public string Detail { get; init; } = "";
	}

	internal sealed class ReadingFreshness
	{
		[JsonPropertyName("hasData")]
		[Description("false when the app has not recorded anything yet.")]
		public bool HasData { get; init; }

		[JsonPropertyName("vin")]
		[Description("The vehicle this answer is about.")]
		public string Vin { get; init; } = "";

		[JsonPropertyName("lastEventApplied")]
		[Description("The dealership event number the app has read up to. Ask twice around a Refresh: "
			+ "if it went up, new facts came in.")]
		public long LastEventApplied { get; init; }

		[JsonPropertyName("lastEventKnown")]
		[Description("false when this record was written by an older version of the app, which did "
			+ "not keep that count. Then lastEventApplied is zero and means nothing - do not report "
			+ "it as 'zero events read'. Everything else in the answer is complete; read detail.")]
		public bool LastEventKnown { get; init; }

		[JsonPropertyName("evaluated")]
		[Description("true if the schedule has ever been evaluated.")]
		public bool Evaluated { get; init; }

		[JsonPropertyName("evaluatedUpTo")]
		[Description("The instant the schedule was evaluated up to, in UTC. Empty means never.")]
		public string EvaluatedUpTo { get; init; } = "";

		[JsonPropertyName("folder")]
		[Description("The person's folder, where their app keeps this vehicle's record.")]
		public string Folder { get; init; } = "";

		[JsonPropertyName("detail")]
		[Description("Empty when everything is fine; otherwise what the person had better do.")]
		public string Detail { get; init; } = "";
	}
}
