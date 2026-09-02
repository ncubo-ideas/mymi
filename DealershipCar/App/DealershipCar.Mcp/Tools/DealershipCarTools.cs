using System.ComponentModel;
using System.Text.Json.Serialization;
using DealershipCar.App;
using ModelContextProtocol.Server;
using Puppeteer;

namespace DealershipCar.Mcp.Tools
{
	[McpServerToolType]
	internal sealed class DealershipCarTools
	{
		private readonly JournalReader reader;

		public DealershipCarTools(JournalReader reader)
		{
			ArgumentNullException.ThrowIfNull(reader);

			this.reader = reader;
		}

		[McpServerTool(Name = "review_my_maintenance", UseStructuredContent = true)]
		[Description("Contesta qué necesita ahora mismo el vehículo de esta persona: qué operaciones "
			+ "de mantenimiento están vencidas, cuáles pasaron su periodo de gracia, cuáles cuentan "
			+ "como no hechas, y qué dice el plan sobre cada una. Úsela cuando pregunten algo general "
			+ "como '¿mi carro necesita algo?', '¿qué está atrasado?', '¿qué hago primero?' o '¿cómo "
			+ "va mi carro?'. Reporta el plan entero en una sola lectura, así que prefiérala a "
			+ "preguntar operación por operación. Cada operación lleva el motivo que compuso el plan "
			+ "mismo: diga ese motivo, no lo reescriba como consejo genérico. Las operaciones que "
			+ "este vehículo nunca tuvo agendadas lo dicen en su motivo; no están atrasadas, "
			+ "simplemente no existen para este carro.")]
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
		[Description("Contesta dónde está parada UNA operación de mantenimiento en el vehículo de "
			+ "esta persona: si está vencida, cuánto se pasó de su límite, y cuántas veces la ha "
			+ "aplazado ya. Úsela cuando pregunten por una operación concreta, como '¿me toca el "
			+ "cambio de aceite?', '¿cuánto puedo aplazar la revisión?' o '¿ya hicieron el llamado a "
			+ "revisión?'. Si preguntan por el vehículo en general, use review_my_maintenance: esa "
			+ "contesta por todas las operaciones en una sola lectura. deferralsUsed es cuántos "
			+ "aplazamientos ha gastado ya en esta operación; una operación de seguridad no se puede "
			+ "aplazar en absoluto.")]
		public OperationStanding CheckOne(
			[Description("Qué operación de mantenimiento hay que consultar.")]
			MaintenanceOperation operation)
		{
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
		[Description("Contesta hasta dónde ha leído la app de esta persona el registro de la agencia, "
			+ "y hasta qué instante se ha evaluado el plan. Úsela cuando esperen ver algo nuevo y no "
			+ "esté ahí -- después de una visita al taller, o después de apretar Actualizar -- y "
			+ "cuando necesite saber si la respuesta que acaba de darles está al día. "
			+ "lastEventApplied es el número de evento de la agencia hasta el que la app ha leído: si "
			+ "sube entre dos llamadas, entraron hechos nuevos. Usted NO puede hacerlo subir; solo la "
			+ "app de la persona puede.")]
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

		private static string TheNextThingToDo(DealershipCarAPI api)
		{
			return api.HasAnythingDue() ? api.ReadNextDueKind() : "";
		}

		private static string TheInstantItWasEvaluatedTo(DealershipCarAPI api)
		{
			return api.HasBeenEvaluated() ? api.LastEvaluatedAt().ToString("O") : "";
		}

		private string TheDetailWhen(bool theEventCheckpointIsKnown)
		{
			return TheAssistantIsTold.EverythingWorthSaying(
				theEventCheckpointIsKnown ? "" : TheAppIsOlderThanTheLogbook,
				TheAssistantIsTold.MoreThanOneVehicle(reader));
		}

		private const string TheAppIsOlderThanTheLogbook =
			"Este registro lo escribió una versión anterior de la app, que no llevaba la cuenta de "
			+ "hasta dónde había leído los eventos de la agencia, así que lastEventApplied no está "
			+ "disponible. El plan de mantenimiento de abajo está completo y al día. Pídale a la "
			+ "persona que abra su app una vez y empezará a llevar esa cuenta; no se pierde nada, y "
			+ "no es un error.";

		private const string TheAppHasNotBeenOpened = TheAssistantIsTold.TheAppHasNotBeenOpened;
	}

	internal sealed class OperationStanding
	{
		[JsonPropertyName("hasData")]
		[Description("false cuando la app todavía no ha registrado nada; entonces el resto viene "
			+ "vacío.")]
		public bool HasData { get; init; }

		[JsonPropertyName("vin")]
		[Description("El vehículo del que habla esta respuesta.")]
		public string Vin { get; init; } = "";

		[JsonPropertyName("operation")]
		[Description("La operación de mantenimiento de la que habla esta entrada.")]
		public string Operation { get; init; } = "";

		[JsonPropertyName("state")]
		[Description("Dónde está parada esta operación: NotDue, DueSoon (dentro del aviso de "
			+ "cortesía, que NUNCA es una parada obligatoria), Due, Overdue (pasado el periodo de "
			+ "gracia), Deferred (la persona la aplazó a propósito), Completed o Superseded (una "
			+ "revisión del plan la reemplazó). Una operación que este vehículo nunca tuvo agendada "
			+ "también lee NotDue: el motivo es lo que distingue las dos.")]
		public string State { get; init; } = "";

		[JsonPropertyName("reason")]
		[Description("Por qué está parada ahí, en una frase que compuso el plan mismo. Diga esta "
			+ "frase; no la reescriba como consejo genérico. Si empieza con 'Este vehículo no tiene "
			+ "agendada', este carro simplemente no tiene esa operación.")]
		public string Reason { get; init; } = "";

		[JsonPropertyName("deferralsUsed")]
		[Description("Cuántos aplazamientos ha gastado ya la persona en esta operación. Hay un tope, "
			+ "y una operación de seguridad no se puede aplazar en absoluto.")]
		public int DeferralsUsed { get; init; }

		[JsonPropertyName("evaluated")]
		[Description("true si el plan se ha evaluado alguna vez. Hasta que se evalúe, cada estado es "
			+ "solo como se sembró el plan.")]
		public bool Evaluated { get; init; }

		[JsonPropertyName("evaluatedUpTo")]
		[Description("El instante hasta el que se evaluó el plan, en UTC. Vacío quiere decir que "
			+ "nunca se ha evaluado: eso lo hace la app cuando la persona la abre, y usted no puede.")]
		public string EvaluatedUpTo { get; init; } = "";

		[JsonPropertyName("lastEventApplied")]
		[Description("El número de evento de la agencia hasta el que la app ha leído.")]
		public long LastEventApplied { get; init; }

		[JsonPropertyName("lastEventKnown")]
		[Description("false cuando este registro lo escribió una versión anterior de la app, que no "
			+ "llevaba esa cuenta. Entonces lastEventApplied es cero y no significa nada: no lo "
			+ "reporte como 'cero eventos leídos'. Todo lo demás en la respuesta está completo; lea "
			+ "detail.")]
		public bool LastEventKnown { get; init; }

		[JsonPropertyName("folder")]
		[Description("La carpeta de la persona, donde su app guarda el registro de este vehículo.")]
		public string Folder { get; init; } = "";

		[JsonPropertyName("detail")]
		[Description("Vacío cuando todo está bien; si no, lo que a la persona le conviene hacer.")]
		public string Detail { get; init; } = "";
	}

	internal sealed class MaintenanceReview
	{
		[JsonPropertyName("hasData")]
		[Description("false cuando la app todavía no ha registrado nada; entonces el resto viene "
			+ "vacío.")]
		public bool HasData { get; init; }

		[JsonPropertyName("vin")]
		[Description("El vehículo del que habla esta respuesta.")]
		public string Vin { get; init; } = "";

		[JsonPropertyName("anythingDue")]
		[Description("true si hay algo vencido o pasado de vencido en este vehículo.")]
		public bool AnythingDue { get; init; }

		[JsonPropertyName("nextToDo")]
		[Description("La operación que hay que hacer primero. VACÍO quiere decir que no hay nada "
			+ "vencido: eso es una respuesta, no un valor que falta, y no se puede reportar como una "
			+ "falla.")]
		public string NextToDo { get; init; } = "";

		[JsonPropertyName("dueCount")]
		[Description("Cuántas operaciones están vencidas o pasadas de vencidas.")]
		public int DueCount { get; init; }

		[JsonPropertyName("overdueCount")]
		[Description("Cuántas pasaron su periodo de gracia. Son las que vale la pena plantear "
			+ "primero.")]
		public int OverdueCount { get; init; }

		[JsonPropertyName("missedCount")]
		[Description("Cuántas cuentan como mantenimiento que no se hizo. Se deriva de dónde están "
			+ "paradas las operaciones, así que vuelve a bajar cuando el trabajo se hace de verdad.")]
		public int MissedCount { get; init; }

		[JsonPropertyName("evaluated")]
		[Description("true si el plan se ha evaluado alguna vez.")]
		public bool Evaluated { get; init; }

		[JsonPropertyName("evaluatedUpTo")]
		[Description("El instante hasta el que se evaluó el plan, en UTC. Vacío quiere decir nunca. "
			+ "Todo lo de abajo es A ESE INSTANTE y no al de ahora: si es viejo, dígalo en vez de "
			+ "presentar la respuesta como la de hoy.")]
		public string EvaluatedUpTo { get; init; } = "";

		[JsonPropertyName("lastEventApplied")]
		[Description("El número de evento de la agencia hasta el que la app ha leído. Si no sube, no "
			+ "entró nada nuevo.")]
		public long LastEventApplied { get; init; }

		[JsonPropertyName("lastEventKnown")]
		[Description("false cuando este registro lo escribió una versión anterior de la app, que no "
			+ "llevaba esa cuenta. Entonces lastEventApplied es cero y no significa nada: no lo "
			+ "reporte como 'cero eventos leídos'. Todo lo demás en la respuesta está completo; lea "
			+ "detail.")]
		public bool LastEventKnown { get; init; }

		[JsonPropertyName("operations")]
		[Description("Dónde está parada cada operación de mantenimiento. Las que este vehículo nunca "
			+ "tuvo agendadas también vienen en la lista, y lo dicen en su motivo.")]
		public IReadOnlyList<OperationStanding> Operations { get; init; } = new List<OperationStanding>();

		[JsonPropertyName("folder")]
		[Description("La carpeta de la persona, donde su app guarda el registro de este vehículo.")]
		public string Folder { get; init; } = "";

		[JsonPropertyName("detail")]
		[Description("Vacío cuando todo está bien; si no, lo que a la persona le conviene hacer.")]
		public string Detail { get; init; } = "";
	}

	internal sealed class ReadingFreshness
	{
		[JsonPropertyName("hasData")]
		[Description("false cuando la app todavía no ha registrado nada.")]
		public bool HasData { get; init; }

		[JsonPropertyName("vin")]
		[Description("El vehículo del que habla esta respuesta.")]
		public string Vin { get; init; } = "";

		[JsonPropertyName("lastEventApplied")]
		[Description("El número de evento de la agencia hasta el que la app ha leído. Pregúntelo dos "
			+ "veces alrededor de un Actualizar: si subió, entraron hechos nuevos.")]
		public long LastEventApplied { get; init; }

		[JsonPropertyName("lastEventKnown")]
		[Description("false cuando este registro lo escribió una versión anterior de la app, que no "
			+ "llevaba esa cuenta. Entonces lastEventApplied es cero y no significa nada: no lo "
			+ "reporte como 'cero eventos leídos'. Todo lo demás en la respuesta está completo; lea "
			+ "detail.")]
		public bool LastEventKnown { get; init; }

		[JsonPropertyName("evaluated")]
		[Description("true si el plan se ha evaluado alguna vez.")]
		public bool Evaluated { get; init; }

		[JsonPropertyName("evaluatedUpTo")]
		[Description("El instante hasta el que se evaluó el plan, en UTC. Vacío quiere decir nunca.")]
		public string EvaluatedUpTo { get; init; } = "";

		[JsonPropertyName("folder")]
		[Description("La carpeta de la persona, donde su app guarda el registro de este vehículo.")]
		public string Folder { get; init; } = "";

		[JsonPropertyName("detail")]
		[Description("Vacío cuando todo está bien; si no, lo que a la persona le conviene hacer.")]
		public string Detail { get; init; } = "";
	}
}
