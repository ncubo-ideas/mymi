using System.ComponentModel;
using System.Text.Json.Serialization;
using Concesionario.App;
using ModelContextProtocol.Server;

namespace Concesionario.Mcp.Tools
{
	/// <summary>
	/// La pregunta del asistente sobre LA PLACA de este vehiculo: cual es, desde cuando rige y todas
	/// las que ha tenido. Solo lectura, y sin un solo parametro: la placa sale de aqui hacia afuera y
	/// nunca entra.
	/// </summary>
	[McpServerToolType]
	internal sealed class LicensePlateTools
	{
		/// <summary>
		/// EL IDIOMA EN QUE ESTE SERVIDOR PIDE LAS FRASES, que es el del REGISTRO y no el de quien
		/// pregunta: la caratula MCP es voz de la institucion hablandole al modelo.
		/// </summary>
		private const string TheVoiceOfTheDealership =
			Concesionario.App.Wording.Phrases.LanguageOfTheRecord;

		private readonly JournalReader reader;

		public LicensePlateTools(JournalReader reader)
		{
			ArgumentNullException.ThrowIfNull(reader);

			this.reader = reader;
		}

		[McpServerTool(Name = "what_plate_my_car_has", UseStructuredContent = true)]
		[Description("Contesta cuál es la placa que el concesionario le publicó a este vehículo, "
			+ "desde cuándo rige, y todas las que ha tenido. Úsela para '¿cuál es la placa de mi "
			+ "carro?', '¿desde cuándo tiene esta placa?' y '¿le cambiaron la placa?'. "
			+ "PARA QUÉ SIRVE UNA PLACA, Y ES LA MITAD MÁS ÚTIL DE ESTA TOOL. La placa es el nombre "
			+ "PÚBLICO de este carro: es como lo nombra cualquier registro que no sea el de este "
			+ "concesionario. Si esta persona tiene datos de su vehículo en otra parte, lo más "
			+ "probable es que allá el carro esté identificado por su placa y no por su VIN, porque "
			+ "el VIN afuera casi nadie lo tiene. Cuando quiera juntar lo que dice este registro con "
			+ "lo que diga otro sobre el mismo carro, la placa es por donde se juntan. "
			+ "SON DOS TEXTOS Y CADA UNO TIENE SU OFICIO. asPublished es la placa tal como la "
			+ "publicó el concesionario, y es la que se le DICE a la persona. joinKey es la misma "
			+ "placa normalizada, sin separadores y en mayúsculas, y es la que se COMPARA. Compare "
			+ "siempre por joinKey y nunca por asPublished: una misma placa se escribe de varias "
			+ "formas, con guion, con espacio o pegada, y solo la llave las vuelve una sola. "
			+ "LAS DOS SALEN YA DECIDIDAS Y NINGUNA SE DERIVA DE LA OTRA. No le quite los "
			+ "separadores a asPublished para armarse una llave, y no se los ponga a joinKey para "
			+ "que se vea bonita: estaría inventando el formato de un país que este registro nunca "
			+ "declaró. Cite cada una donde va. "
			+ "ESTA TOOL NO RECIBE NINGUNA PLACA Y NO ES UN OLVIDO. No hay por dónde pasarle una, ni "
			+ "para buscar ni para comparar: este registro es de UN vehículo y ya dice cuál es la "
			+ "suya. Si la persona le dicta una placa, no la tome como un dato de este carro. "
			+ "SIN PLACA PUBLICADA HAY RESPUESTA Y NO HUECO. platePublished en false quiere decir "
			+ "que el concesionario todavía no le ha publicado ninguna, que NO es lo mismo que el "
			+ "carro no tenga: says lo dice con palabras. No invente una y NUNCA conteste el VIN en "
			+ "su lugar. "
			+ "UNA PLACA NUEVA NO CORRIGE A LA ANTERIOR: es un hecho nuevo con su propio instante, y "
			+ "las anteriores siguen siendo ciertas de cuando lo fueron. assignments las trae todas "
			+ "con su fecha y con cuál es la vigente.")]
		public PlateOfThisCar WhatPlateMyCarHas()
		{
			return reader.Read(ThePlateOfThisVehicle, () => new PlateOfThisCar
			{
				HasData = false,
				Folder = reader.Folder,
				Detail = TheAssistantIsTold.TheAppHasNotBeenOpened,
			});
		}

		private PlateOfThisCar ThePlateOfThisVehicle(DealershipCarAPI api)
		{
			if (!TheAssistantIsTold.TheVehiclesOwnRecordIsThere(api))
				return new PlateOfThisCar
				{
					HasData = true,
					Vin = api.ReadVin(),
					PlateKnown = false,
					Folder = reader.Folder,
					Detail = TheAssistantIsTold.EverythingWorthSaying(
						TheAssistantIsTold.TheAppIsOlderThanTheVehiclesOwnRecord,
						ThePlateIsWrittenIntoThatSamePartOfTheRecord,
						TheAssistantIsTold.MoreThanOneVehicle(reader)),
				};

			DateTime askedAt = TheClockTheAssistantReads.TheInstantThisWasAsked();

			bool published = api.IsAPlatePublished(askedAt);

			PlateAssignmentRecord record = api.PlateAssignments();

			var rows = new List<RecordedPlate>();

			foreach (int row in Enumerable.Range(0, record.AsPublished.Length))
				rows.Add(new RecordedPlate
				{
					AsPublished = record.AsPublished[row],
					JoinKey = record.JoinKey[row],
					AssignedAt = record.AssignedAt[row],
					IsCurrent = record.IsCurrent[row],
				});

			return new PlateOfThisCar
			{
				HasData = true,
				Vin = api.ReadVin(),
				PlateKnown = true,
				AskedAt = askedAt.ToString("O"),
				PlatePublished = published,
				AsPublished = published ? api.ReadPlateAsPublished(askedAt) : "",
				JoinKey = published ? api.ReadPlateJoinKey(askedAt) : "",
				Says = api.DescribePlate(askedAt, TheVoiceOfTheDealership),
				Assignments = rows,
				Folder = reader.Folder,
				Detail = TheAssistantIsTold.EverythingWorthSaying(
					published ? TheKeyIsWhatCrossesAndTheOtherTextIsWhatIsSaid : "",
					published ? "" : NoPlateHasBeenPublishedAndThatIsNotACarWithoutOne,
					rows.Count > 1 ? ASecondPlateIsANewFactAndNotACorrection : "",
					ThePlateGoesOutAndNeverComesIn,
					TheAssistantIsTold.MoreThanOneVehicle(reader)),
			};
		}

		private const string ThePlateIsWrittenIntoThatSamePartOfTheRecord =
			"La placa la registra el concesionario en esa misma parte del registro, así que tampoco "
			+ "está: aquí no hay ninguna placa que leer. Eso no quiere decir que el carro no tenga "
			+ "una, y el VIN nunca va en su lugar.";

		private const string TheKeyIsWhatCrossesAndTheOtherTextIsWhatIsSaid =
			"SON DOS TEXTOS Y CADA UNO TIENE SU OFICIO. asPublished es la placa tal como la publicó "
			+ "el concesionario y es la que se le dice a la persona; joinKey es la misma placa "
			+ "normalizada y es la que se COMPARA contra cualquier otro registro de esta persona "
			+ "que identifique carros por placa. Las dos salen de aquí YA DECIDIDAS: no derive una "
			+ "de la otra ni le arregle el formato a ninguna, porque eso inventaría el formato de un "
			+ "país que este registro nunca declaró.";

		private const string NoPlateHasBeenPublishedAndThatIsNotACarWithoutOne =
			"El concesionario todavía no le ha publicado ninguna placa a este vehículo al instante "
			+ "preguntado, así que asPublished y joinKey vienen VACÍOS y says lo dice con palabras. "
			+ "Eso NO es un carro sin placa y no es un hueco que usted pueda llenar: no la invente, "
			+ "no la deduzca de nada y NUNCA conteste el VIN en su lugar, porque un VIN comparado "
			+ "contra registros que identifican carros por placa no coincide con nada y no avisa. "
			+ "La trae el feed del concesionario cuando la persona abre su app.";

		private const string ASecondPlateIsANewFactAndNotACorrection =
			"Este vehículo ha tenido MÁS DE UNA placa. Una placa nueva no corrige a la anterior: es "
			+ "un hecho nuevo con su propio instante, y las anteriores siguen siendo ciertas de "
			+ "cuando lo fueron. asPublished y joinKey son las de la que rige al instante "
			+ "preguntado; assignments las trae todas con su fecha y con cuál es la vigente. Si va a "
			+ "cruzar contra algo de hace tiempo, cruce con la llave que regía entonces.";

		private const string ThePlateGoesOutAndNeverComesIn =
			"Esta tool no recibe ninguna placa y no hay por dónde pasarle una. Lo que se lleva hacia "
			+ "afuera para juntar este registro con otro es joinKey; si la persona le dicta una "
			+ "placa, no la tome como un dato de este carro.";
	}

	/// <summary>Una asignacion de placa como fila: los dos textos, desde cuando rige y si es la vigente.</summary>
	internal sealed class RecordedPlate
	{
		[JsonPropertyName("asPublished")]
		[Description("La placa de esta asignación tal como la publicó el concesionario. Es la que se "
			+ "le dice a la persona.")]
		public string AsPublished { get; init; } = "";

		[JsonPropertyName("joinKey")]
		[Description("La misma placa normalizada, que es la que se compara contra otro registro. "
			+ "Sale ya decidida: no se deriva de la de arriba.")]
		public string JoinKey { get; init; } = "";

		[JsonPropertyName("assignedAt")]
		[Description("Desde cuándo rige esta asignación, en yyyy-MM-dd. Llegó con el hecho: no es "
			+ "el día en que alguien lo anotó.")]
		public string AssignedAt { get; init; } = "";

		[JsonPropertyName("isCurrent")]
		[Description("'true' en la asignación que rige hoy y 'false' en las demás. Lo decidió el "
			+ "registro por la línea de tiempo, así que no lo deduzca de las fechas.")]
		public string IsCurrent { get; init; } = "";
	}

	/// <summary>
	/// La placa de este vehiculo: los dos textos de la que rige, la frase del registro, y todas las
	/// asignaciones como filas.
	/// </summary>
	internal sealed class PlateOfThisCar
	{
		[JsonPropertyName("hasData")]
		[Description("false cuando la app todavía no se ha abierto en esta máquina; entonces el "
			+ "resto viene vacío.")]
		public bool HasData { get; init; }

		[JsonPropertyName("vin")]
		[Description("El vehículo del que habla esta respuesta. Es su identidad interna y NO se "
			+ "puede usar en lugar de la placa: afuera casi nadie lo tiene.")]
		public string Vin { get; init; } = "";

		[JsonPropertyName("plateKnown")]
		[Description("false cuando este registro lo escribió una versión de la app anterior a lo "
			+ "que el concesionario registra sobre este vehículo. Entonces no hay ninguna placa que "
			+ "leer: lea detail. NO quiere decir que el carro no tenga placa.")]
		public bool PlateKnown { get; init; }

		[JsonPropertyName("askedAt")]
		[Description("El instante en que se hizo esta pregunta, en UTC. Cuál placa rige es a ese "
			+ "instante y no a ningún otro.")]
		public string AskedAt { get; init; } = "";

		[JsonPropertyName("platePublished")]
		[Description("true cuando el concesionario ha publicado una placa para este vehículo a "
			+ "askedAt o antes. false quiere decir que aquí no hay ninguna registrada, que no es lo "
			+ "mismo que el carro no tenga una.")]
		public bool PlatePublished { get; init; }

		[JsonPropertyName("asPublished")]
		[Description("LA PLACA COMO LA PUBLICÓ EL CONCESIONARIO, que es la que se le dice a la "
			+ "persona. VACÍO quiere decir que no hay ninguna publicada. No le quite ni le ponga "
			+ "separadores: para comparar está la de abajo.")]
		public string AsPublished { get; init; } = "";

		[JsonPropertyName("joinKey")]
		[Description("LA MISMA PLACA NORMALIZADA, que es la que se COMPARA contra cualquier otro "
			+ "registro de esta persona que identifique carros por placa. Sale ya decidida y no se "
			+ "deriva de asPublished. VACÍO quiere decir que no hay ninguna publicada.")]
		public string JoinKey { get; init; } = "";

		[JsonPropertyName("says")]
		[Description("LA RESPUESTA, en una frase que compuso el registro: cuál es la placa y desde "
			+ "cuándo rige, o la constancia en palabras de que no hay ninguna. Dígala en vez de "
			+ "rearmarla a partir de los otros campos.")]
		public string Says { get; init; } = "";

		[JsonPropertyName("assignments")]
		[Description("Una fila por asignación de placa, en orden de la línea de tiempo. Una placa "
			+ "nueva no borra a la anterior: las de antes siguen aquí con su fecha, y son las que "
			+ "hay que usar para cruzar contra algo de aquella época.")]
		public IReadOnlyList<RecordedPlate> Assignments { get; init; } = new List<RecordedPlate>();

		[JsonPropertyName("folder")]
		[Description("La carpeta de la persona, donde su app guarda el registro de este vehículo.")]
		public string Folder { get; init; } = "";

		[JsonPropertyName("detail")]
		[Description("Lo que esta respuesta no dice, y lo que no hay que hacer con ella. Léalo "
			+ "antes de contestar.")]
		public string Detail { get; init; } = "";
	}
}
