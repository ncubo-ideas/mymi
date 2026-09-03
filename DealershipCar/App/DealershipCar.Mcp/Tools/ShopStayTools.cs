using System.ComponentModel;
using System.Text.Json.Serialization;
using DealershipCar.App;
using ModelContextProtocol.Server;

namespace DealershipCar.Mcp.Tools
{
	/// <summary>
	/// La pregunta del asistente sobre los dias que ESTE vehiculo paso adentro de un taller: cuales
	/// dias, en cuantas estadias y con que dijo cada sucursal. Solo lectura, y a nadie se le pide un
	/// dato del carro: los dos parametros son los extremos del periodo por el que se pregunta.
	/// </summary>
	[McpServerToolType]
	internal sealed class ShopStayTools
	{
		private readonly JournalReader reader;

		public ShopStayTools(JournalReader reader)
		{
			ArgumentNullException.ThrowIfNull(reader);

			this.reader = reader;
		}

		[McpServerTool(Name = "days_my_car_spent_in_the_shop", UseStructuredContent = true)]
		[Description("Contesta qué días estuvo el vehículo de esta persona adentro de un taller, en "
			+ "cuántas estadías, y qué dijo cada sucursal de cada una. Úsela para '¿cuántos días me "
			+ "dejaron sin carro?', '¿qué días estuvo en el taller?', '¿cuándo fue la última vez que "
			+ "lo llevaron?' y '¿está en el taller ahora?'. "
			+ "LA RESPUESTA SON LOS DÍAS Y NO UNA DURACIÓN. days trae cada día por separado en "
			+ "yyyy-MM-dd, y eso es a propósito: una duración no se puede cruzar con el calendario de "
			+ "nadie y una lista de fechas sí. Si la persona pregunta algo como qué días de escuela se "
			+ "perdieron o qué semana se quedó sin carro, cruce esas fechas: para eso están. "
			+ "LOS DÍAS LOS DICE LA SUCURSAL Y NO SE CALCULAN de los dos instantes de la estadía. No "
			+ "los derive usted tampoco, y no rellene los días entre dos fechas: si la sucursal dijo "
			+ "tres, son tres. "
			+ "FROM Y TO SON LOS EXTREMOS DEL PERIODO POR EL QUE SE PREGUNTA, los dos inclusive, en "
			+ "yyyy-MM-dd. NO son datos del carro: dicen qué pedazo del registro se está pidiendo. "
			+ "OMÍTALOS LOS DOS si la persona no nombró ningún periodo, y entonces se contesta sobre "
			+ "todo el registro; omita uno solo para dejar ese extremo abierto. La respuesta siempre "
			+ "dice con qué dos límites contestó: cítelos, para que se vea de qué periodo se está "
			+ "hablando. "
			+ "LAS FILAS Y LOS DÍAS SON LAS ESTADÍAS YA CERRADAS DEL PERIODO. Una estadía todavía "
			+ "abierta no tiene ningún día -- los días llegan cuando entregan el carro --, así que no "
			+ "está entre ellas: insideRightNow y currentStaySays son las que la dicen. Si "
			+ "insideRightNow viene en true, el carro está en el taller AHORA y dayCount no lo incluye: "
			+ "dígalo, o estará dando un total cierto que engaña. "
			+ "LO QUE DICE says ES LA RESPUESTA DEL PERIODO, una frase que compuso el registro mismo "
			+ "con los días y en cuántas estadías. Un periodo sin ninguna estadía la dice con palabras "
			+ "y nombra el periodo: eso es una respuesta, no un dato que falte.")]
		public ShopStays DaysMyCarSpentInTheShop(
			[Description("El primer día del periodo por el que se pregunta, en yyyy-MM-dd. Omítalo "
				+ "para no poner límite por ese lado.")]
			string from = "",
			[Description("El último día del periodo, en yyyy-MM-dd, inclusive. Omítalo para no poner "
				+ "límite por ese lado.")]
			string to = "")
		{
			return reader.Read(
				api => TheStaysOfThisVehicle(api, from, to),
				() => new ShopStays
				{
					HasData = false,
					From = TheBoundOr(from, TheFirstDayAnyRecordCouldName),
					To = TheBoundOr(to, TheLastDayAnyRecordCouldName),
					Folder = reader.Folder,
					Detail = TheAssistantIsTold.TheAppHasNotBeenOpened,
				});
		}

		private ShopStays TheStaysOfThisVehicle(DealershipCarAPI api, string from, string to)
		{
			string fromDay = TheBoundOr(from, TheFirstDayAnyRecordCouldName);
			string toDay = TheBoundOr(to, TheLastDayAnyRecordCouldName);
			bool whole = TheBoundIsBlank(from) && TheBoundIsBlank(to);

			DateTime askedAt = TheClockTheAssistantReads.TheInstantThisWasAsked();

			if (!TheAssistantIsTold.TheVehiclesStaysAreThere(api, askedAt))
				return new ShopStays
				{
					HasData = true,
					Vin = api.ReadVin(),
					StaysKnown = false,
					BoundsUnderstood = true,
					AskedAt = askedAt.ToString("O"),
					From = fromDay,
					To = toDay,
					WholeRecord = whole,
					Folder = reader.Folder,
					Detail = TheAssistantIsTold.EverythingWorthSaying(
						TheAssistantIsTold.TheAppIsOlderThanTheShopStays,
						TheAssistantIsTold.MoreThanOneVehicle(reader)),
				};

			// LAS DOS LECTURAS POR INSTANTE VAN ANTES Y FUERA DE LA GUARDA, y no es orden de escritura:
			// no toman ningun limite, asi que un limite mal escrito no puede dejar sin contestar la
			// unica pregunta que alcanza una estadia abierta. Si estuvieran adentro, la disponibilidad
			// de un periodo bien dicho se volveria precondicion de saber si el carro esta adentro AHORA.
			bool insideRightNow = api.IsInsideTheShopAt(askedAt);
			string currentStaySays = api.DescribeTheStayAt(askedAt);

			try
			{
				ShopStayRecord record = api.StaysBetween(fromDay, toDay);

				var rows = new List<RecordedStay>();

				foreach (int row in Enumerable.Range(0, record.Branch.Length))
					rows.Add(new RecordedStay
					{
						Branch = record.Branch[row],
						Subject = record.Subject[row],
						EnteredAt = record.EnteredAt[row],
						CoveredDays = record.CoveredDays[row],
						Says = record.Says[row],
					});

				return new ShopStays
				{
					HasData = true,
					Vin = api.ReadVin(),
					StaysKnown = true,
					BoundsUnderstood = true,
					AskedAt = askedAt.ToString("O"),
					From = fromDay,
					To = toDay,
					WholeRecord = whole,
					AnyStayInThePeriod = api.HasAnyStayBetween(fromDay, toDay),
					Days = api.DaysInTheShopBetween(fromDay, toDay),
					DayCount = api.DaysInTheShopCountBetween(fromDay, toDay),
					StayCount = api.StayCountBetween(fromDay, toDay),
					Says = api.DescribeDaysInTheShopBetween(fromDay, toDay),
					Stays = rows,
					InsideRightNow = insideRightNow,
					CurrentStaySays = currentStaySays,
					Folder = reader.Folder,
					Detail = TheAssistantIsTold.EverythingWorthSaying(
						insideRightNow ? TheCarIsInAShopRightNow : "",
						TheAssistantIsTold.MoreThanOneVehicle(reader)),
				};
			}
			catch (ArgumentException refused)
			{
				return new ShopStays
				{
					HasData = true,
					Vin = api.ReadVin(),
					StaysKnown = true,
					BoundsUnderstood = false,
					AskedAt = askedAt.ToString("O"),
					From = fromDay,
					To = toDay,
					WholeRecord = whole,
					InsideRightNow = insideRightNow,
					CurrentStaySays = currentStaySays,
					Folder = reader.Folder,
					Detail = TheAssistantIsTold.EverythingWorthSaying(
						TheBoundDoesNotSayACalendarDay,
						refused.Message,
						insideRightNow ? TheCarIsInAShopRightNow : "",
						TheAssistantIsTold.MoreThanOneVehicle(reader)),
				};
			}
		}

		private static bool TheBoundIsBlank(string bound)
		{
			return string.IsNullOrWhiteSpace(bound);
		}

		private static string TheBoundOr(string bound, string whenBlank)
		{
			return TheBoundIsBlank(bound) ? whenBlank : bound.Trim();
		}

		/// <summary>
		/// El primer dia que un dia de calendario puede nombrar. Es lo que quiere decir 'todo el
		/// registro' cuando no se pone limite: el registro no tiene extremos propios que preguntar.
		/// </summary>
		private const string TheFirstDayAnyRecordCouldName = "0001-01-01";

		/// <summary>El ultimo, por lo mismo.</summary>
		private const string TheLastDayAnyRecordCouldName = "9999-12-31";

		private const string TheCarIsInAShopRightNow =
			"El vehículo está adentro de un taller AHORA MISMO y esa estadía todavía no tiene ningún "
			+ "día: los días los dice la sucursal cuando entrega el carro. Así que no está en days, no "
			+ "está en las filas, y dayCount NO la cuenta. Diga las dos cosas -- el total del periodo y "
			+ "que el carro está adentro en este momento --, porque dar solo el total es dar un número "
			+ "cierto que engaña. currentStaySays lo dice entero: dónde está, desde cuándo y por qué "
			+ "entró.";

		private const string TheBoundDoesNotSayACalendarDay =
			"Uno de los dos límites del periodo no dice un día de calendario, así que no se pudo listar "
			+ "ningún día y days, las filas y las cuentas vienen vacías. NO es que el carro no haya "
			+ "estado en el taller. Vuelva a preguntar con los dos límites en yyyy-MM-dd, u omítalos "
			+ "los dos para preguntar sobre todo el registro. Lo que sí contesta igual es si el carro "
			+ "está adentro ahora mismo: eso no depende de ningún periodo.";
	}

	/// <summary>Una estadia del periodo como fila: donde, por que, cuando entro y que dias cubrio.</summary>
	internal sealed class RecordedStay
	{
		[JsonPropertyName("branch")]
		[Description("La sucursal donde estuvo, con las palabras que mandó la sucursal misma.")]
		public string Branch { get; init; } = "";

		[JsonPropertyName("subject")]
		[Description("Por qué entró, con las palabras que mandó la sucursal. Es lo que dijeron, no un "
			+ "diagnóstico ni una clasificación.")]
		public string Subject { get; init; } = "";

		[JsonPropertyName("enteredAt")]
		[Description("Cuándo entró, con su hora. Un instante nunca se dice aquí como un día de "
			+ "calendario a secas: recortarlo pediría una zona horaria que este registro no tiene.")]
		public string EnteredAt { get; init; } = "";

		[JsonPropertyName("coveredDays")]
		[Description("Los días que la sucursal dijo que cubrió esta estadía, separados por espacios y "
			+ "en yyyy-MM-dd. Es lo que dijo la sucursal, no el rango entre los dos instantes.")]
		public string CoveredDays { get; init; } = "";

		[JsonPropertyName("says")]
		[Description("Esta estadía en la frase que compuso el registro mismo: dónde, desde cuándo "
			+ "hasta cuándo, qué días cubrió y por qué entró. Dígala en vez de rearmarla.")]
		public string Says { get; init; } = "";
	}

	/// <summary>
	/// Los dias que este vehiculo paso en un taller en un periodo, las estadias que los cubrieron, y si
	/// esta adentro de una en este momento.
	/// </summary>
	internal sealed class ShopStays
	{
		[JsonPropertyName("hasData")]
		[Description("false cuando la app todavía no ha registrado nada; entonces el resto viene "
			+ "vacío.")]
		public bool HasData { get; init; }

		[JsonPropertyName("vin")]
		[Description("El vehículo del que habla esta respuesta.")]
		public string Vin { get; init; } = "";

		[JsonPropertyName("staysKnown")]
		[Description("false cuando este registro lo escribió una versión de la app anterior a las "
			+ "estadías en el taller. Entonces no hay ninguna en él y todo lo de abajo viene vacío: lea "
			+ "detail. NO quiere decir que el carro nunca haya estado en un taller, y nunca se puede "
			+ "reportar como cero días.")]
		public bool StaysKnown { get; init; }

		[JsonPropertyName("boundsUnderstood")]
		[Description("false cuando uno de los dos límites no dice un día de calendario en yyyy-MM-dd. "
			+ "Entonces days, las filas y las cuentas vienen vacías porque no se pudo elegir ningún "
			+ "periodo, no porque no haya estadías: lea detail y vuelva a preguntar.")]
		public bool BoundsUnderstood { get; init; }

		[JsonPropertyName("askedAt")]
		[Description("El instante en que se hizo esta pregunta, en UTC. insideRightNow y "
			+ "currentStaySays son a ESE instante; el periodo de abajo no depende de él.")]
		public string AskedAt { get; init; } = "";

		[JsonPropertyName("from")]
		[Description("El primer día del periodo con el que se contestó, en yyyy-MM-dd. Cítelo: si "
			+ "nadie puso límite, aquí viene el día más antiguo que un calendario puede nombrar, y eso "
			+ "es lo que quiere decir que se contestó sobre todo el registro.")]
		public string From { get; init; } = "";

		[JsonPropertyName("to")]
		[Description("El último día del periodo con el que se contestó, inclusive.")]
		public string To { get; init; } = "";

		[JsonPropertyName("wholeRecord")]
		[Description("true cuando no se puso ningún límite y la respuesta cubre todo el registro. "
			+ "false quiere decir que hay un periodo elegido: entonces un día fuera de él no está "
			+ "ausente del registro, está fuera de la pregunta.")]
		public bool WholeRecord { get; init; }

		[JsonPropertyName("anyStayInThePeriod")]
		[Description("true si alguna estadía ya cerrada se cruza con el periodo. false con "
			+ "boundsUnderstood en true es una respuesta: en ese periodo no hay ninguna registrada.")]
		public bool AnyStayInThePeriod { get; init; }

		[JsonPropertyName("days")]
		[Description("LOS DÍAS, uno por uno, en yyyy-MM-dd, ascendentes y sin repetidos. Son días de "
			+ "calendario que se pueden cruzar con cualquier otro calendario -- el de la escuela, el de "
			+ "un viaje, el de una semana concreta -- y ese cruce es para lo que están. Vacío es una "
			+ "respuesta cuando boundsUnderstood es true.")]
		public string[] Days { get; init; } = new string[0];

		[JsonPropertyName("dayCount")]
		[Description("Cuántos días son, contados por el registro sobre esos mismos días. NO incluye "
			+ "una estadía que siga abierta: mire insideRightNow.")]
		public int DayCount { get; init; }

		[JsonPropertyName("stayCount")]
		[Description("Cuántas estadías cerradas se cruzan con el periodo. No es lo mismo que dayCount: "
			+ "una estadía cubre varios días.")]
		public int StayCount { get; init; }

		[JsonPropertyName("says")]
		[Description("LA RESPUESTA DEL PERIODO, en una frase que compuso el registro: los días, en "
			+ "cuántas estadías y cuáles; o, cuando no hay ninguna, la constancia de que no hay ninguna "
			+ "con el periodo nombrado. Dígala en vez de rearmarla a partir de las cuentas.")]
		public string Says { get; init; } = "";

		[JsonPropertyName("stays")]
		[Description("Una fila por estadía cerrada del periodo, en orden de entrada. Cada fila se dice "
			+ "sola: lleva su sucursal, su asunto, su instante de entrada, sus días y su propia frase.")]
		public IReadOnlyList<RecordedStay> Stays { get; init; } = new List<RecordedStay>();

		[JsonPropertyName("insideRightNow")]
		[Description("true si el carro está adentro de un taller en el instante de askedAt. Es lo "
			+ "único de esta respuesta que ve una estadía todavía abierta, y contesta aunque el periodo "
			+ "no se haya podido entender.")]
		public bool InsideRightNow { get; init; }

		[JsonPropertyName("currentStaySays")]
		[Description("La estadía que cubre askedAt, dicha entera por el registro -- dónde está, desde "
			+ "cuándo y por qué entró --, o la constancia de que ninguna lo cubre. Con insideRightNow "
			+ "en true esta frase es la respuesta a '¿dónde está mi carro?'.")]
		public string CurrentStaySays { get; init; } = "";

		[JsonPropertyName("folder")]
		[Description("La carpeta de la persona, donde su app guarda el registro de este vehículo.")]
		public string Folder { get; init; } = "";

		[JsonPropertyName("detail")]
		[Description("Lo que esta respuesta no dice, y cualquier cosa que a la persona le convenga "
			+ "hacer. Léalo antes de contestar: ahí están los límites de la respuesta.")]
		public string Detail { get; init; } = "";
	}
}
