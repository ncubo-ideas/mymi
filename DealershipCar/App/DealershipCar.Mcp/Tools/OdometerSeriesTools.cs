using System.ComponentModel;
using System.Globalization;
using System.Text.Json.Serialization;
using DealershipCar.App;
using ModelContextProtocol.Server;

namespace DealershipCar.Mcp.Tools
{
	/// <summary>
	/// La pregunta del asistente sobre CUANTO SE MANEJA este vehiculo: el ritmo de un periodo dicho
	/// entero por el registro, y las lecturas del odometro de ese periodo como filas. Solo lectura, y
	/// a nadie se le pide un dato del carro: los dos parametros son los extremos del periodo.
	/// </summary>
	[McpServerToolType]
	internal sealed class OdometerSeriesTools
	{
		private readonly JournalReader reader;

		public OdometerSeriesTools(JournalReader reader)
		{
			ArgumentNullException.ThrowIfNull(reader);

			this.reader = reader;
		}

		[McpServerTool(Name = "how_much_my_car_is_driven", UseStructuredContent = true)]
		[Description("Contesta cuánto se maneja el vehículo de esta persona en un periodo: el ritmo "
			+ "dicho entero por el registro, y las lecturas del odómetro de ese periodo una por una. "
			+ "Úsela para '¿cuánto manejo?', '¿cuántos kilómetros hago por día?', '¿manejo más que "
			+ "antes?' y '¿qué lecturas del odómetro hay?'. "
			+ "LO QUE DICE says ES LA RESPUESTA, y la compuso el registro. Trae los dos extremos con "
			+ "sus instantes, la distancia entre ellos, el tiempo transcurrido y la distancia por "
			+ "día. Dígala. NO la rearme: no divida el valor de una fila entre el de otra, no reste "
			+ "dos lecturas y no saque un promedio de las filas. Eso daría un ritmo que nadie midió, "
			+ "y el que sí se midió ya viene escrito. "
			+ "UN RITMO SE PUEDE NO PODER DECIR, Y ESO ES UNA RESPUESTA. standing dice en cuál de "
			+ "cinco situaciones está: se pudo medir, o no se pudo por una de cuatro razones con "
			+ "nombre -- lecturas en dos unidades distintas, una sola lectura en el periodo, ninguna "
			+ "lectura en el periodo, o los dos extremos observados en el mismo segundo. NINGUNA de "
			+ "las cuatro quiere decir que el carro no se movió ni que falten datos por su culpa: "
			+ "says dice cuál es y por qué, con la fecha de la lectura que lo explica. "
			+ "LAS FILAS SON EL REGISTRO CRUDO y cada una se dice sola: cuándo se observó, qué "
			+ "marcaba el odómetro, en qué unidad, de qué fuente vino y la frase con que el registro "
			+ "la dice. La fuente importa y no se pondera: una lectura que declaró la persona por "
			+ "teléfono y una que reportó el vehículo pesan lo mismo en el ritmo, y decir cuál es "
			+ "cuál es toda la obligación. Las unidades NUNCA se convierten: si dos filas vienen en "
			+ "unidades distintas no las pase a una sola, dígalo. "
			+ "FROM Y TO SON LOS EXTREMOS DEL PERIODO POR EL QUE SE PREGUNTA, los dos inclusive, en "
			+ "yyyy-MM-dd. NO son datos del carro: dicen qué pedazo del registro se está pidiendo. "
			+ "OMÍTALOS LOS DOS si la persona no nombró ningún periodo, y entonces se contesta sobre "
			+ "todo el registro; omita uno solo para dejar ese extremo abierto. La respuesta siempre "
			+ "dice con qué dos límites contestó: cítelos, para que se vea de qué periodo se está "
			+ "hablando.")]
		public RateOfUse HowMuchMyCarIsDriven(
			[Description("El primer día del periodo por el que se pregunta, en yyyy-MM-dd. Se toma "
				+ "desde el comienzo de ese día. Omítalo para no poner límite por ese lado.")]
			string from = "",
			[Description("El último día del periodo, en yyyy-MM-dd, inclusive: se toma hasta el "
				+ "final de ese día, así que una lectura de esa misma tarde entra. Omítalo para no "
				+ "poner límite por ese lado.")]
			string to = "")
		{
			DateTime start;
			DateTime end;

			bool theFirstIsADay = TheStartOfTheDayIn(from, TheFirstInstantACalendarCanName, out start);
			bool theSecondIsADay = TheEndOfTheDayIn(to, TheLastInstantACalendarCanName, out end);
			bool understood = theFirstIsADay && theSecondIsADay;

			bool whole = TheBoundIsBlank(from) && TheBoundIsBlank(to);

			return reader.Read(
				api => TheRateOfThisVehicle(api, understood, start, end, whole, from, to),
				() => new RateOfUse
				{
					HasData = false,
					BoundsUnderstood = understood,
					From = understood ? TheText(start) : TheBoundAsGiven(from),
					To = understood ? TheText(end) : TheBoundAsGiven(to),
					WholeRecord = whole,
					Folder = reader.Folder,
					Detail = TheAssistantIsTold.EverythingWorthSaying(
						TheAssistantIsTold.TheAppHasNotBeenOpened,
						understood ? "" : TheBoundDoesNotSayACalendarDay),
				});
		}

		private RateOfUse TheRateOfThisVehicle(
			DealershipCarAPI api, bool understood, DateTime start, DateTime end, bool whole,
			string from, string to)
		{
			if (!understood)
				return new RateOfUse
				{
					HasData = true,
					Vin = api.ReadVin(),
					BoundsUnderstood = false,
					From = TheBoundAsGiven(from),
					To = TheBoundAsGiven(to),
					WholeRecord = whole,
					Folder = reader.Folder,
					Detail = TheAssistantIsTold.EverythingWorthSaying(
						TheBoundDoesNotSayACalendarDay,
						TheAssistantIsTold.MoreThanOneVehicle(reader)),
				};

			OdometerSeriesRecord record = api.ReadingsBetween(start, end);

			var rows = new List<OdometerReadingRow>();

			foreach (int row in Enumerable.Range(0, record.Says.Length))
				rows.Add(new OdometerReadingRow
				{
					ObservedAt = record.ObservedAt[row],
					Value = record.Value[row],
					Unit = record.Unit[row],
					Source = record.Source[row],
					Says = record.Says[row],
				});

			bool canBeSaid = api.CanARateOfUseBeSaid(start, end);

			return new RateOfUse
			{
				HasData = true,
				Vin = api.ReadVin(),
				BoundsUnderstood = true,
				From = TheText(start),
				To = TheText(end),
				WholeRecord = whole,
				CanBeSaid = canBeSaid,
				Standing = api.ReadRateOfUseStanding(start, end),
				Says = api.DescribeRateOfUse(start, end),
				ReadingCount = api.ReadingCountBetween(start, end),
				Readings = rows,
				Folder = reader.Folder,
				Detail = TheAssistantIsTold.EverythingWorthSaying(
					TheRateIsSaidAndNeverRecomputed,
					canBeSaid ? "" : TheSuspensionIsAnAnswer,
					TheAssistantIsTold.MoreThanOneVehicle(reader)),
			};
		}

		private static bool TheBoundIsBlank(string bound)
		{
			return string.IsNullOrWhiteSpace(bound);
		}

		/// <summary>
		/// El comienzo del dia que dice ese texto, o el instante dado cuando el limite viene en
		/// blanco. Contesta false cuando el texto no dice un dia de calendario, y entonces no hay
		/// ningun instante que preguntar.
		/// </summary>
		private static bool TheStartOfTheDayIn(string bound, DateTime whenBlank, out DateTime start)
		{
			if (TheBoundIsBlank(bound))
			{
				start = whenBlank;
				return true;
			}

			DateTime day;

			if (!ADayOfTheCalendar(bound, out day))
			{
				start = whenBlank;
				return false;
			}

			start = new DateTime(day.Year, day.Month, day.Day, 0, 0, 0, DateTimeKind.Utc);
			return true;
		}

		/// <summary>
		/// El final del dia que dice ese texto -- su ultimo segundo entero, porque los dos extremos
		/// son inclusive y un dia dura un dia. Las mismas dos salidas que el de arriba.
		/// </summary>
		private static bool TheEndOfTheDayIn(string bound, DateTime whenBlank, out DateTime end)
		{
			if (TheBoundIsBlank(bound))
			{
				end = whenBlank;
				return true;
			}

			DateTime day;

			if (!ADayOfTheCalendar(bound, out day))
			{
				end = whenBlank;
				return false;
			}

			end = new DateTime(day.Year, day.Month, day.Day, 23, 59, 59, DateTimeKind.Utc);
			return true;
		}

		private static bool ADayOfTheCalendar(string bound, out DateTime day)
		{
			return DateTime.TryParseExact(
				bound.Trim(), TheOnlyShapeADayIsWrittenIn, CultureInfo.InvariantCulture,
				DateTimeStyles.None, out day);
		}

		/// <summary>Un instante como lo dice el registro en sus propias frases.</summary>
		private static string TheText(DateTime instant)
		{
			return instant.ToString(TheShapeAnInstantIsSaidIn, CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// El limite tal como lo escribio quien pregunto, para que una respuesta a un limite que no
		/// se entendio muestre cual fue el texto.
		/// </summary>
		private static string TheBoundAsGiven(string bound)
		{
			return TheBoundIsBlank(bound) ? "" : bound.Trim();
		}

		private const string TheOnlyShapeADayIsWrittenIn = "yyyy-MM-dd";

		private const string TheShapeAnInstantIsSaidIn = "yyyy-MM-dd HH:mm:ss";

		/// <summary>
		/// El primer instante que un calendario puede nombrar. Es lo que quiere decir 'todo el
		/// registro' cuando no se pone limite por ese lado: el registro no tiene extremos propios que
		/// preguntar, y ninguna lectura contesta cual fue la primera lectura ni la ultima.
		/// </summary>
		private static readonly DateTime TheFirstInstantACalendarCanName =
			new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		/// <summary>
		/// El ultimo, por lo mismo, en su ultimo segundo entero: el journal guarda segundos enteros,
		/// asi que este es el instante mas tarde que sobrevive de ida y de vuelta sin recortarse.
		/// </summary>
		private static readonly DateTime TheLastInstantACalendarCanName =
			new DateTime(9999, 12, 31, 23, 59, 59, DateTimeKind.Utc);

		private const string TheRateIsSaidAndNeverRecomputed =
			"El ritmo de says lo compuso el registro y ya trae la distancia, el tiempo y la distancia "
			+ "por día. Dígalo tal cual. Las filas están para que la persona vea de dónde salió, no "
			+ "para volver a sacarlo: restar dos filas o dividir una entre otra daría un número que "
			+ "nadie midió, y las unidades no se convierten en ningún caso.";

		private const string TheSuspensionIsAnAnswer =
			"En este periodo el ritmo NO se pudo decir, y eso es una respuesta y no un hueco: says "
			+ "dice cuál de las cuatro razones es y nombra la lectura o el periodo que lo explica. "
			+ "Dígala completa. NO conteste cero, NO estime, y NO amplíe el periodo por su cuenta "
			+ "para que dé un número: vuelva a preguntar por otro periodo solo si la persona lo pide. "
			+ "Las lecturas que sí hay siguen viniendo en las filas.";

		private const string TheBoundDoesNotSayACalendarDay =
			"Uno de los dos límites del periodo no dice un día de calendario, así que no se preguntó "
			+ "nada y el ritmo, las filas y la cuenta vienen vacíos. NO es que no haya lecturas, y NO "
			+ "es un periodo sin ritmo: es que no se eligió ningún periodo. Vuelva a preguntar con "
			+ "los dos límites en yyyy-MM-dd, u omítalos los dos para preguntar sobre todo el "
			+ "registro.";
	}

	/// <summary>Una lectura del odometro del periodo como fila: cuando, cuanto, en que y de donde.</summary>
	internal sealed class OdometerReadingRow
	{
		[JsonPropertyName("observedAt")]
		[Description("Cuándo se observó esta lectura, con su hora. Es el instante que llegó con el "
			+ "hecho, no el día en que alguien la anotó.")]
		public string ObservedAt { get; init; } = "";

		[JsonPropertyName("value")]
		[Description("Lo que marcaba el odómetro en esa lectura, tal como llegó. Es un hecho "
			+ "recibido y no una cuenta: dígalo con la unidad de al lado, y no lo reste ni lo divida "
			+ "contra otra fila.")]
		public int Value { get; init; }

		[JsonPropertyName("unit")]
		[Description("La unidad en que se midió esta lectura, como la lee una persona. Nunca se "
			+ "convierte a otra: si dos filas traen unidades distintas, dígalo.")]
		public string Unit { get; init; } = "";

		[JsonPropertyName("source")]
		[Description("De dónde vino esta lectura: la reportó el vehículo, la anotó alguien a mano, "
			+ "salió de una inspección, la declaró la persona, o corrige una anterior. Se dice y no "
			+ "se pondera: ninguna fuente pesa más ni menos en el ritmo.")]
		public string Source { get; init; } = "";

		[JsonPropertyName("says")]
		[Description("Esta lectura en la frase que compuso el registro mismo: cuánto marcaba, cuándo "
			+ "y de dónde vino. Dígala en vez de rearmarla con los otros campos.")]
		public string Says { get; init; } = "";
	}

	/// <summary>
	/// Cuanto se manejo este vehiculo en un periodo: el ritmo dicho entero, en cual de las cinco
	/// situaciones esta, y las lecturas del odometro de ese periodo como filas.
	/// </summary>
	internal sealed class RateOfUse
	{
		[JsonPropertyName("hasData")]
		[Description("false cuando la app todavía no se ha abierto en esta máquina; entonces el "
			+ "resto viene vacío.")]
		public bool HasData { get; init; }

		[JsonPropertyName("vin")]
		[Description("El vehículo del que habla esta respuesta.")]
		public string Vin { get; init; } = "";

		[JsonPropertyName("boundsUnderstood")]
		[Description("false cuando uno de los dos límites no dice un día en yyyy-MM-dd. Entonces no "
			+ "se preguntó nada y todo lo de abajo viene vacío porque no se eligió ningún periodo, "
			+ "no porque no haya lecturas: lea detail y vuelva a preguntar.")]
		public bool BoundsUnderstood { get; init; }

		[JsonPropertyName("from")]
		[Description("El primer instante del periodo con el que se contestó. Cítelo: si nadie puso "
			+ "límite por ese lado, aquí viene el instante más antiguo que un calendario puede "
			+ "nombrar, y eso es lo que quiere decir que se contestó sobre todo el registro. Con "
			+ "boundsUnderstood en false aquí viene el texto que no se entendió.")]
		public string From { get; init; } = "";

		[JsonPropertyName("to")]
		[Description("El último instante del periodo con el que se contestó, inclusive. Un día "
			+ "escrito como límite llega hasta su último segundo, así que una lectura de esa misma "
			+ "tarde entra en la respuesta.")]
		public string To { get; init; } = "";

		[JsonPropertyName("wholeRecord")]
		[Description("true cuando no se puso ningún límite y la respuesta cubre todo el registro. "
			+ "false quiere decir que hay un periodo elegido: entonces una lectura fuera de él no "
			+ "está ausente del registro, está fuera de la pregunta.")]
		public bool WholeRecord { get; init; }

		[JsonPropertyName("canBeSaid")]
		[Description("true si el ritmo de este periodo se pudo medir. false NO es un error ni un "
			+ "dato que falte: standing dice cuál de las cuatro razones con nombre es y says la dice "
			+ "con palabras. Las filas contestan igual en los dos casos.")]
		public bool CanBeSaid { get; init; }

		[JsonPropertyName("standing")]
		[Description("En cuál de cinco situaciones está el ritmo de este periodo, por su nombre: se "
			+ "midió, o se suspendió por unidades distintas, por una sola lectura, por ninguna "
			+ "lectura, o porque los dos extremos se observaron en el mismo segundo. Ninguna de las "
			+ "cinco quiere decir 'no se sabe'.")]
		public string Standing { get; init; } = "";

		[JsonPropertyName("says")]
		[Description("LA RESPUESTA DEL PERIODO, en una frase que compuso el registro: los dos "
			+ "extremos con sus instantes, la distancia, el tiempo y la distancia por día; o la "
			+ "razón con nombre que impide decirlo, con la lectura o el periodo que la explica. "
			+ "Dígala en vez de rearmarla a partir de las filas.")]
		public string Says { get; init; } = "";

		[JsonPropertyName("readingCount")]
		[Description("Cuántas lecturas del odómetro cayeron dentro del periodo. Es la cuenta de las "
			+ "filas de abajo y no una medida del carro.")]
		public int ReadingCount { get; init; }

		[JsonPropertyName("readings")]
		[Description("Una fila por lectura del periodo, en orden de observación. Cada fila se dice "
			+ "sola: lleva su instante, su valor, su unidad, su fuente y su propia frase.")]
		public IReadOnlyList<OdometerReadingRow> Readings { get; init; } =
			new List<OdometerReadingRow>();

		[JsonPropertyName("folder")]
		[Description("La carpeta de la persona, donde su app guarda el registro de este vehículo.")]
		public string Folder { get; init; } = "";

		[JsonPropertyName("detail")]
		[Description("Lo que esta respuesta no dice, y lo que no hay que hacer con ella. Léalo antes "
			+ "de contestar.")]
		public string Detail { get; init; } = "";
	}
}
