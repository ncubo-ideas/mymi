using System.ComponentModel;
using System.Text.Json.Serialization;
using Concesionario.App;
using ModelContextProtocol.Server;
using Puppeteer;

namespace Concesionario.Mcp.Tools
{
	[McpServerToolType]
	internal sealed class DealershipCarTools
	{
		/// <summary>
		/// EL IDIOMA EN QUE ESTA CARA PIDE LAS FRASES DEL DOMINIO, que es el del registro y no el de
		/// quien pregunta: esta superficie es la voz del concesionario hablandole al modelo, igual
		/// que el titulo y las descripciones de estas mismas tools.
		/// </summary>
		private const string TheVoiceOfTheDealership = Concesionario.App.Wording.Phrases.LanguageOfTheRecord;

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
		[Description("Contesta hasta dónde ha leído la app de esta persona el registro del concesionario, "
			+ "y hasta qué instante se ha evaluado el plan. Úsela cuando esperen ver algo nuevo y no "
			+ "esté ahí -- después de una visita al taller, o después de apretar Actualizar -- y "
			+ "cuando necesite saber si la respuesta que acaba de darles está al día. "
			+ "lastEventApplied es el número de evento del concesionario hasta el que la app ha leído: si "
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

		[McpServerTool(Name = "what_is_left_before_each_service", UseStructuredContent = true)]
		[Description("Contesta CUÁNTO LE FALTA al vehículo de esta persona antes de cada operación de "
			+ "mantenimiento: cuánta distancia y cuántos días, con la unidad de cada monto y en cuál "
			+ "de sus siete situaciones está cada mitad. Úsela para '¿cuánto me falta para el próximo "
			+ "servicio?', '¿cuántos kilómetros me quedan?', '¿cuánto tiempo tengo?'. Contesta por las "
			+ "nueve clases en una sola lectura, así que prefiérala a preguntar una por una. "
			+ "CADA OPERACIÓN TIENE DOS MITADES Y LAS DOS SE DICEN SIEMPRE: una de distancia y una de "
			+ "plazo. Lo que dice says ya las lleva las dos y no elige ninguna, a propósito: cuál va a "
			+ "llegar primero depende de cuánto manejen, y este registro no tiene ese ritmo. Dígala "
			+ "entera. "
			+ "NUNCA COMBINE LAS DOS MITADES. Dividir la distancia que falta entre los días que faltan "
			+ "da un ritmo que nadie midió, y multiplicar o restar entre dos operaciones compone una "
			+ "cuenta que el registro no hizo. Los montos están para citarlos con su unidad, y para "
			+ "cruzarlos con lo que la persona le cuente de su propio manejo diciendo que ese cruce es "
			+ "suyo y no del registro. "
			+ "distanceStanding Y timeStanding SON SIETE RESPUESTAS Y NO DOS. StillAhead todavía "
			+ "falta; AlreadyPassed ya se pasó, y entonces el monto es EL EXCESO y nunca un número "
			+ "negativo; OutsideTheLaw esa operación no se mide en esa dimensión, que NO es lo mismo "
			+ "que no haberla podido medir; SuspendedByAnAbsentReading falta una lectura del odómetro, "
			+ "que es la única de las siete que arregla una visita al taller; SuspendedByAUnitMismatch "
			+ "dos unidades que no coinciden; SuspendedByTheUsageProfile el perfil de uso; y "
			+ "NoOperationScheduled este vehículo nunca tuvo esa operación agendada, que no es un "
			+ "hueco en los datos sino un carro que no tiene esa parte. Diga la situación con la frase "
			+ "que la acompaña; no la reescriba como consejo genérico. "
			+ "CUANDO UNA MITAD NO SE PUEDE DECIR, su canBeSaid viene en false y su monto viene en "
			+ "CERO, y ese cero NO es una distancia: quiere decir que no hay ningún monto. Lea la "
			+ "situación y la frase.")]
		public MaintenanceCountdown WhatIsLeftBeforeEachService()
		{
			return reader.Read(TheCountdownOfEveryKind, () => new MaintenanceCountdown
			{
				HasData = false,
				Folder = reader.Folder,
				Detail = TheAppHasNotBeenOpened,
			});
		}

		[McpServerTool(Name = "what_has_been_done_to_my_car", UseStructuredContent = true)]
		[Description("Contesta QUÉ LE HAN HECHO al vehículo de esta persona: cada completación "
			+ "registrada como una fila con su operación, cuándo se hizo y con qué odómetro, de la más "
			+ "reciente a la más vieja. Úsela para '¿qué le han hecho a mi carro?', '¿cuándo fue el "
			+ "último cambio de aceite?', '¿ya me hicieron la revisión?'. "
			+ "EL HISTORIAL NO SE GUARDA COMO UNA LISTA: se camina en cada lectura por la cadena de "
			+ "ocurrencias del plan, así que es lo que la cadena diga y no hay ningún límite ni "
			+ "paginado. "
			+ "UNA COMPLETACIÓN SIN LECTURA DEL ODÓMETRO LO DICE CON PALABRAS y nunca viene como cero: "
			+ "una operación que se mide solo en tiempo no guarda odómetro, y reportar cero ahí sería "
			+ "decir que el carro tenía cero kilómetros. "
			+ "LO QUE DICE says DE CADA FILA ES LA RESPUESTA, compuesta por el registro mismo: nombra "
			+ "la operación, el instante y la lectura o su ausencia. Dígala en vez de rearmarla. "
			+ "NADA DE ESTO DEPENDE DEL INSTANTE EN QUE PREGUNTE: cada fila trae el suyo. "
			+ "DOS AUSENCIAS QUE SE PARECEN Y NO SON LA MISMA, y están en el says de cada entrada de "
			+ "byOperation: 'no hay ninguna completación registrada' quiere decir que la operación SÍ "
			+ "está agendada y nunca se ha hecho, que es lo que arregla una visita al taller; 'este "
			+ "vehículo no tiene agendada ninguna operación de' quiere decir que este carro no tiene "
			+ "esa operación en absoluto. Confundirlas es mandar a alguien al taller por algo que su "
			+ "carro no lleva, o dejar de mandarlo por algo que sí. Cite ese says y no lo resuma en un "
			+ "sí o un no.")]
		public MaintenanceHistory WhatHasBeenDoneToMyCar()
		{
			return reader.Read(TheWholeHistory, () => new MaintenanceHistory
			{
				HasData = false,
				Folder = reader.Folder,
				Detail = TheAppHasNotBeenOpened,
			});
		}

		private MaintenanceCountdown TheCountdownOfEveryKind(DealershipCarAPI api)
		{
			DateTime askedAt = TheClockTheAssistantReads.TheInstantThisWasAsked();

			var operations = new List<ServiceCountdown>();
			bool someRefused = false;

			foreach (MaintenanceOperation each in TheOperationKind.All)
			{
				ServiceCountdown row = TheCountdownOfOneKind(api, each, askedAt);

				operations.Add(row);

				if (row.CountdownRefused) someRefused = true;
			}

			return new MaintenanceCountdown
			{
				HasData = true,
				Vin = api.ReadVin(),
				AskedAt = askedAt.ToString("O"),
				Operations = operations,
				SomeCountdownWasRefused = someRefused,
				Folder = reader.Folder,
				Detail = TheAssistantIsTold.EverythingWorthSaying(
					TheTwoHalvesAreNeverCombined,
					someRefused ? AnOperationIsAnchoredAfterTheInstantAsked : "",
					TheAssistantIsTold.MoreThanOneVehicle(reader)),
			};
		}

		private static ServiceCountdown TheCountdownOfOneKind(
			DealershipCarAPI api, MaintenanceOperation operation, DateTime askedAt)
		{
			string kind = operation.WireName();

			try
			{
				bool distanceCanBeSaid = api.CanDistanceRemainingBeSaid(kind, askedAt);
				bool daysCanBeSaid = api.CanDaysRemainingBeSaid(kind, askedAt);

				return new ServiceCountdown
				{
					Operation = kind,
					Says = api.DescribeCountdown(kind, askedAt, TheVoiceOfTheDealership),
					DistanceStanding = api.ReadDistanceRemainderStanding(kind, askedAt),
					DistanceSays = api.DescribeDistanceRemaining(kind, askedAt, TheVoiceOfTheDealership),
					DistanceCanBeSaid = distanceCanBeSaid,
					DistanceRemaining =
						distanceCanBeSaid ? api.ReadDistanceRemaining(kind, askedAt) : 0,
					DistanceUnit =
						distanceCanBeSaid ? api.ReadDistanceRemainingUnit(kind, askedAt) : "",
					TimeStanding = api.ReadTimeRemainderStanding(kind, askedAt),
					DaysSays = api.DescribeDaysRemaining(kind, askedAt, TheVoiceOfTheDealership),
					DaysCanBeSaid = daysCanBeSaid,
					DaysRemaining = daysCanBeSaid ? api.ReadDaysRemaining(kind, askedAt) : 0,
				};
			}
			catch (InvalidOperationException refused)
			{
				return new ServiceCountdown
				{
					Operation = kind,
					CountdownRefused = true,
					RefusalSays = refused.Message,
				};
			}
		}

		private MaintenanceHistory TheWholeHistory(DealershipCarAPI api)
		{
			EveryKindCompletionRecord record = api.CompletionsOfEveryKind(TheVoiceOfTheDealership);

			var completions = new List<CompletedWork>();

			foreach (int row in Enumerable.Range(0, record.Kind.Length))
				completions.Add(new CompletedWork
				{
					Operation = record.Kind[row],
					CompletedAt = record.CompletedAt[row],
					Reading = record.Reading[row],
					Says = record.Says[row],
				});

			var byOperation = new List<WorkOnOneKind>();

			foreach (MaintenanceOperation each in TheOperationKind.All)
				byOperation.Add(new WorkOnOneKind
				{
					Operation = each.WireName(),
					AnyOnRecord = api.HasAnyCompletion(each.WireName()),
					Count = api.CompletionCount(each.WireName()),
					Says = api.DescribeCompletions(each.WireName(), TheVoiceOfTheDealership),
				});

			return new MaintenanceHistory
			{
				HasData = true,
				Vin = api.ReadVin(),
				Completions = completions,
				ByOperation = byOperation,
				Folder = reader.Folder,
				Detail = TheAssistantIsTold.EverythingWorthSaying(
					TheAbsenceOfAHistoryIsSaidPerOperation,
					TheAssistantIsTold.MoreThanOneVehicle(reader)),
			};
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
					Reason = api.ReadDueReason(each.WireName(), TheVoiceOfTheDealership),
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
				Reason = api.ReadDueReason(kind, TheVoiceOfTheDealership),
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
			+ "hasta dónde había leído los eventos del concesionario, así que lastEventApplied no está "
			+ "disponible. El plan de mantenimiento de abajo está completo y al día. Pídale a la "
			+ "persona que abra su app una vez y empezará a llevar esa cuenta; no se pierde nada, y "
			+ "no es un error.";

		private const string TheAppHasNotBeenOpened = TheAssistantIsTold.TheAppHasNotBeenOpened;

		private const string TheTwoHalvesAreNeverCombined =
			"Cada operación trae DOS montos, uno de distancia y uno de plazo, y el registro no los "
			+ "combina nunca: dividir uno entre el otro daría un ritmo de manejo que nadie midió, y "
			+ "cuál de las dos mitades va a llegar primero depende de eso. Diga las dos, o diga la "
			+ "frase de says que ya las lleva las dos. Si la persona le cuenta cuánto maneja, puede "
			+ "cruzarlo con el monto diciendo que ese cruce es de ella y no del registro.";

		private const string AnOperationIsAnchoredAfterTheInstantAsked =
			"Al menos una operación de este plan está anclada en el futuro -- después del instante en "
			+ "que se hizo esta pregunta --, así que su cuenta atrás no se pudo medir y su fila viene "
			+ "con countdownRefused en true y sus montos vacíos. NO es que a esa operación no le falte "
			+ "nada, y no es un error suyo ni de la persona: el ancla la puso el concesionario al sembrar "
			+ "el plan, y solo el concesionario la puede corregir. Todas las demás operaciones de la "
			+ "lista contestaron enteras: dígalas, y diga aparte cuál no se pudo medir y por qué.";

		private const string TheAbsenceOfAHistoryIsSaidPerOperation =
			"El historial entero viene en completions, de la completación más reciente a la más vieja, "
			+ "y una lista vacía es una respuesta: quiere decir que la cadena de este vehículo no "
			+ "tiene ninguna completación alcanzable. No hay ninguna frase de ausencia para el "
			+ "historial entero y no se debe componer una, porque la razón es POR OPERACIÓN y está en "
			+ "el says de cada entrada de byOperation: ahí 'agendada y nunca hecha' y 'este carro no "
			+ "tiene esa operación' son dos respuestas distintas. Cite ese says.";
	}

	/// <summary>
	/// Lo que le falta a UNA operacion antes de su proximo servicio: las dos mitades con su unidad y
	/// su situacion, y la frase con que el registro lo dice.
	/// </summary>
	internal sealed class ServiceCountdown
	{
		[JsonPropertyName("operation")]
		[Description("La operación de mantenimiento de la que habla esta fila. Es el nombre con el que "
			+ "se puede volver a preguntar por ella.")]
		public string Operation { get; init; } = "";

		[JsonPropertyName("says")]
		[Description("LO QUE LE FALTA, en una frase que compuso el registro mismo y que lleva LAS DOS "
			+ "mitades sin elegir ninguna. Dígala en vez de rearmarla a partir de los montos. Una "
			+ "operación que este vehículo nunca tuvo agendada lo dice aquí una sola vez, porque no "
			+ "hay dos mitades que decir. VACÍO solo cuando countdownRefused es true.")]
		public string Says { get; init; } = "";

		[JsonPropertyName("distanceStanding")]
		[Description("En cuál de las siete situaciones está la mitad de DISTANCIA: StillAhead, "
			+ "AlreadyPassed, OutsideTheLaw, SuspendedByAnAbsentReading, SuspendedByAUnitMismatch, "
			+ "SuspendedByTheUsageProfile o NoOperationScheduled. No es un sí o un no, y las siete "
			+ "quieren decir cosas distintas: lea distanceSays.")]
		public string DistanceStanding { get; init; } = "";

		[JsonPropertyName("distanceSays")]
		[Description("La mitad de distancia en palabras del registro, incluida la razón cuando no se "
			+ "pudo medir. Es una respuesta y nunca un hueco.")]
		public string DistanceSays { get; init; } = "";

		[JsonPropertyName("distanceCanBeSaid")]
		[Description("true cuando hay un monto de distancia que decir. En false, distanceRemaining "
			+ "viene en cero y distanceUnit vacía, y ese cero NO es una distancia.")]
		public bool DistanceCanBeSaid { get; init; }

		[JsonPropertyName("distanceRemaining")]
		[Description("Cuánta distancia falta, en la unidad de distanceUnit. Pasado el límite este "
			+ "número es EL EXCESO y no lo que falta -- distanceStanding dice AlreadyPassed y la frase "
			+ "lo dice en palabras --, así que nunca es negativo y nunca se recorta a cero. CERO con "
			+ "distanceCanBeSaid en false quiere decir que no hay ningún monto.")]
		public int DistanceRemaining { get; init; }

		[JsonPropertyName("distanceUnit")]
		[Description("La unidad de ese monto, como la lee una persona. Dígala: el registro nunca "
			+ "convierte entre sus dos unidades de distancia, así que el número solo no es la "
			+ "respuesta.")]
		public string DistanceUnit { get; init; } = "";

		[JsonPropertyName("timeStanding")]
		[Description("En cuál de las siete situaciones está la mitad de PLAZO, con los mismos siete "
			+ "nombres. Las dos mitades se miden por separado: una puede contestar y la otra no.")]
		public string TimeStanding { get; init; } = "";

		[JsonPropertyName("daysSays")]
		[Description("La mitad de plazo en palabras del registro, incluida la razón cuando no se pudo "
			+ "medir.")]
		public string DaysSays { get; init; } = "";

		[JsonPropertyName("daysCanBeSaid")]
		[Description("true cuando hay un monto de días que decir. En false, daysRemaining viene en "
			+ "cero y ese cero no es un plazo.")]
		public bool DaysCanBeSaid { get; init; }

		[JsonPropertyName("daysRemaining")]
		[Description("Cuántos días faltan. Su unidad está en el nombre: el plazo se mide en una sola, "
			+ "a diferencia de la distancia. Pasado el plazo es el EXCESO, igual que la distancia.")]
		public int DaysRemaining { get; init; }

		[JsonPropertyName("countdownRefused")]
		[Description("true cuando esta operación está anclada DESPUÉS del instante en que se preguntó, "
			+ "así que no había nada que medir todavía y los demás campos de la fila vienen vacíos. NO "
			+ "quiere decir que no le falte nada: lea refusalSays y detail. Que una mitad no se pueda "
			+ "decir NO es esto, y viene en su propio canBeSaid.")]
		public bool CountdownRefused { get; init; }

		[JsonPropertyName("refusalSays")]
		[Description("Por qué no se pudo medir esta operación, con las palabras del registro. Es un "
			+ "diagnóstico sobre el plan y no una frase para decirle a la persona tal cual: lo que se "
			+ "le dice está en detail.")]
		public string RefusalSays { get; init; } = "";
	}

	/// <summary>
	/// Lo que le falta al vehiculo antes de cada una de las nueve clases de operacion, al instante en
	/// que se pregunto.
	/// </summary>
	internal sealed class MaintenanceCountdown
	{
		[JsonPropertyName("hasData")]
		[Description("false cuando la app todavía no ha registrado nada; entonces el resto viene "
			+ "vacío.")]
		public bool HasData { get; init; }

		[JsonPropertyName("vin")]
		[Description("El vehículo del que habla esta respuesta.")]
		public string Vin { get; init; } = "";

		[JsonPropertyName("askedAt")]
		[Description("El instante en que se hizo esta pregunta, en UTC, y el instante contra el que se "
			+ "midió cada mitad. Todo lo de aquí es A ESE INSTANTE. Usted no puede preguntar por otro: "
			+ "un instante que no saliera del reloj sería una fecha inventada, y la posición del "
			+ "vehículo se mediría contra ella.")]
		public string AskedAt { get; init; } = "";

		[JsonPropertyName("operations")]
		[Description("Una fila por cada una de las nueve clases de operación, agendadas o no. Las que "
			+ "este vehículo nunca tuvo agendadas también vienen, y lo dicen en su frase: eso es una "
			+ "respuesta y no un hueco.")]
		public IReadOnlyList<ServiceCountdown> Operations { get; init; } = new List<ServiceCountdown>();

		[JsonPropertyName("someCountdownWasRefused")]
		[Description("true cuando al menos una fila no se pudo medir porque su operación está anclada "
			+ "en el futuro. Las demás filas contestaron enteras; lea detail.")]
		public bool SomeCountdownWasRefused { get; init; }

		[JsonPropertyName("folder")]
		[Description("La carpeta de la persona, donde su app guarda el registro de este vehículo.")]
		public string Folder { get; init; } = "";

		[JsonPropertyName("detail")]
		[Description("Lo que esta respuesta no dice, y cualquier cosa que a la persona le convenga "
			+ "hacer. Léalo antes de contestar: ahí están los límites de la respuesta.")]
		public string Detail { get; init; } = "";
	}

	/// <summary>Una completacion como fila: de que operacion, cuando y con que odometro.</summary>
	internal sealed class CompletedWork
	{
		[JsonPropertyName("operation")]
		[Description("La operación que se completó. Es el nombre con el que se puede volver a "
			+ "preguntar por ella.")]
		public string Operation { get; init; } = "";

		[JsonPropertyName("completedAt")]
		[Description("Cuándo se hizo, con su hora.")]
		public string CompletedAt { get; init; } = "";

		[JsonPropertyName("reading")]
		[Description("Con qué odómetro se hizo, con su unidad, o la constancia en palabras de que no "
			+ "hay ninguno registrado. NUNCA un cero y nunca una celda vacía: una operación que se "
			+ "mide solo en tiempo no guarda odómetro.")]
		public string Reading { get; init; } = "";

		[JsonPropertyName("says")]
		[Description("Esta completación en la frase que compuso el registro mismo: qué operación, "
			+ "cuándo y con qué odómetro o su ausencia. Dígala en vez de rearmarla.")]
		public string Says { get; init; } = "";
	}

	/// <summary>Lo que el registro dice del historial de UNA operacion: si hay, cuantas y su frase.</summary>
	internal sealed class WorkOnOneKind
	{
		[JsonPropertyName("operation")]
		[Description("La operación de la que habla esta entrada.")]
		public string Operation { get; init; } = "";

		[JsonPropertyName("anyOnRecord")]
		[Description("true si esta operación tiene alguna completación registrada. false son DOS "
			+ "respuestas distintas y says dice cuál: agendada y nunca hecha, o no agendada en "
			+ "absoluto.")]
		public bool AnyOnRecord { get; init; }

		[JsonPropertyName("count")]
		[Description("Cuántas completaciones hay de esta operación. Se cuenta caminando la cadena en "
			+ "cada lectura, así que es la cuenta de las filas de completions que llevan este nombre.")]
		public int Count { get; init; }

		[JsonPropertyName("says")]
		[Description("El historial de esta operación en una frase que compuso el registro. AQUÍ ESTÁN "
			+ "LAS DOS AUSENCIAS y son distintas: 'no hay ninguna completación registrada' es una "
			+ "operación agendada que nunca se ha hecho, y 'este vehículo no tiene agendada ninguna "
			+ "operación de' es un carro que no lleva esa operación. Cite esta frase; no la resuma en "
			+ "un sí o un no.")]
		public string Says { get; init; } = "";
	}

	/// <summary>
	/// Lo que le han hecho a este vehiculo: las completaciones de todas las clases como filas, y lo
	/// que el registro dice de cada clase por separado.
	/// </summary>
	internal sealed class MaintenanceHistory
	{
		[JsonPropertyName("hasData")]
		[Description("false cuando la app todavía no ha registrado nada; entonces el resto viene "
			+ "vacío.")]
		public bool HasData { get; init; }

		[JsonPropertyName("vin")]
		[Description("El vehículo del que habla esta respuesta.")]
		public string Vin { get; init; } = "";

		[JsonPropertyName("completions")]
		[Description("Una fila por completación registrada, de todas las operaciones juntas y de la "
			+ "más reciente a la más vieja. Cada fila se dice sola: lleva su operación, su instante, "
			+ "su odómetro o su ausencia y su propia frase. Una lista VACÍA es una respuesta -- este "
			+ "vehículo no tiene ninguna completación alcanzable en su cadena -- y la razón es por "
			+ "operación: está en byOperation.")]
		public IReadOnlyList<CompletedWork> Completions { get; init; } = new List<CompletedWork>();

		[JsonPropertyName("byOperation")]
		[Description("Una entrada por cada una de las nueve clases de operación, agendadas o no, con "
			+ "lo que el registro dice del historial de cada una. Es donde están las dos ausencias que "
			+ "se parecen y no son la misma.")]
		public IReadOnlyList<WorkOnOneKind> ByOperation { get; init; } = new List<WorkOnOneKind>();

		[JsonPropertyName("folder")]
		[Description("La carpeta de la persona, donde su app guarda el registro de este vehículo.")]
		public string Folder { get; init; } = "";

		[JsonPropertyName("detail")]
		[Description("Lo que esta respuesta no dice, y cualquier cosa que a la persona le convenga "
			+ "hacer. Léalo antes de contestar: ahí están los límites de la respuesta.")]
		public string Detail { get; init; } = "";
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
		[Description("El número de evento del concesionario hasta el que la app ha leído.")]
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
		[Description("El número de evento del concesionario hasta el que la app ha leído. Si no sube, no "
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
		[Description("El número de evento del concesionario hasta el que la app ha leído. Pregúntelo dos "
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
