using System.ComponentModel;
using System.Globalization;
using System.Text.Json.Serialization;
using Concesionario.App;
using ModelContextProtocol.Server;

namespace Concesionario.Mcp.Tools
{
	/// <summary>
	/// La pregunta del asistente sobre EN QUE SE LE FUE LA PLATA a esta persona con este vehiculo: el
	/// gasto de un periodo dicho entero por el registro con sus dos vistas, ese periodo contra otro,
	/// lo que costo la distancia, y los gastos de ese periodo como filas. Solo lectura, y a nadie se
	/// le pide un dato del carro: los cuatro parametros son los extremos de los dos periodos.
	/// </summary>
	[McpServerToolType]
	internal sealed class CostLedgerTools
	{
		/// <summary>
		/// EL IDIOMA EN QUE ESTE SERVIDOR PIDE LAS FRASES, que es el del REGISTRO y no el de quien
		/// pregunta: la caratula MCP es voz de la institucion hablandole al modelo.
		/// </summary>
		private const string TheVoiceOfTheDealership =
			Concesionario.App.Wording.Phrases.LanguageOfTheRecord;

		private readonly JournalReader reader;

		public CostLedgerTools(JournalReader reader)
		{
			ArgumentNullException.ThrowIfNull(reader);

			this.reader = reader;
		}

		[McpServerTool(Name = "what_i_have_spent_on_my_car", UseStructuredContent = true)]
		[Description("Contesta en qué se le fue la plata a esta persona con su vehículo en un "
			+ "periodo: lo que salió del banco y lo que el periodo consumió, dicho entero por el "
			+ "registro, ese periodo contra otro, lo que costó la distancia recorrida, y los gastos "
			+ "de ese periodo uno por uno. Úsela para '¿cuánto llevo gastado?', '¿en qué se me fue "
			+ "la plata?', '¿vale la pena seguir arreglando el carro?', '¿este año contra el año "
			+ "pasado?' y '¿cuánto me cuesta el kilómetro?'. "
			+ "LAS DOS VISTAS VAN SIEMPRE JUNTAS Y NINGUNA SE DICE SOLA. Lo que salió del banco en "
			+ "el periodo y lo que el periodo consumió son dos cifras distintas, y separarlas es "
			+ "toda la respuesta: sin la distinción, un total dice 'el carro se puso caro' cuando la "
			+ "verdad es 'compró llantas'. says y perCurrencySays traen las dos adentro. Dígalas las "
			+ "dos. NO conteste una sola, NO las sume y NO elija la más alta. "
			+ "EL REGISTRO NO DA VEREDICTOS Y USTED TAMPOCO. Si la persona pregunta si vale la pena "
			+ "seguir arreglando el carro, la respuesta son las cifras con la distinción que las "
			+ "hace legibles, y el juicio es de ella. No diga 'caro', no diga 'barato', no puntúe y "
			+ "no invente un umbral: no hay ninguno. "
			+ "DOS MONEDAS NO SE SUMAN Y ESO NO ES UN ERROR. Cuando el periodo tiene gastos en más "
			+ "de una moneda, cada una se dice por su lado y NO hay un total entre ellas: no existe "
			+ "ningún factor con que sumarlas, y reconvertir un histórico sería falsear el pasado. "
			+ "saidInOneCurrency dice en cuál de los dos casos está. Nunca invente un total, un tipo "
			+ "de cambio ni una 'moneda base'. "
			+ "EL DINERO YA VIENE DICHO, con el signo de su moneda y sus decimales. Cítelo tal cual: "
			+ "un monto sin su moneda no se puede leer, y el registro nunca entrega el número solo. "
			+ "LAS FILAS SON EL REGISTRO CRUDO y cada una se dice sola: cuándo se incurrió, en qué se "
			+ "fue, cuánto costó dicho por su moneda, si es de los que se consumen en su periodo o de "
			+ "los que duran, y su vida útil si la tiene. NO las sume: el total ya está compuesto "
			+ "arriba, y sumarlas a mano da un número que además cruzaría monedas. "
			+ "LOS CUATRO LÍMITES SON LOS EXTREMOS DE LOS DOS PERIODOS, los cuatro inclusive, en "
			+ "yyyy-MM-dd. NO son datos del carro: dicen qué pedazos del registro se están pidiendo. "
			+ "from y to son el PRIMER periodo -- del que hablan says, la distancia y las filas -- y "
			+ "from2 y to2 el SEGUNDO, con el que se compara. Omítalos para no poner límite por ese "
			+ "lado; con los cuatro en blanco la comparación es del registro entero contra sí mismo, "
			+ "que es una respuesta verdadera y poco útil: nombre el segundo periodo para comparar "
			+ "dos de verdad. La respuesta siempre dice con qué cuatro límites contestó: cítelos.")]
		public SpendOnThisCar WhatIHaveSpentOnMyCar(
			[Description("El primer día del primer periodo, en yyyy-MM-dd. Se toma desde el comienzo "
				+ "de ese día. Omítalo para no poner límite por ese lado.")]
			string from = "",
			[Description("El último día del primer periodo, en yyyy-MM-dd, inclusive: se toma hasta "
				+ "el final de ese día, así que un gasto de esa misma tarde entra. Omítalo para no "
				+ "poner límite por ese lado.")]
			string to = "",
			[Description("El primer día del segundo periodo, el que se usa para comparar, en "
				+ "yyyy-MM-dd. Omítalo para no poner límite por ese lado.")]
			string from2 = "",
			[Description("El último día del segundo periodo, en yyyy-MM-dd, inclusive. Omítalo para "
				+ "no poner límite por ese lado.")]
			string to2 = "")
		{
			DateTime start;
			DateTime end;
			DateTime earlierStart;
			DateTime earlierEnd;

			bool understood =
				TheStartOfTheDayIn(from, TheFirstInstantACalendarCanName, out start)
				& TheEndOfTheDayIn(to, TheLastInstantACalendarCanName, out end)
				& TheStartOfTheDayIn(from2, TheFirstInstantACalendarCanName, out earlierStart)
				& TheEndOfTheDayIn(to2, TheLastInstantACalendarCanName, out earlierEnd);

			bool whole = TheBoundIsBlank(from) && TheBoundIsBlank(to);

			var asked = new TheFourBoundsAsGiven
			{
				From = from,
				To = to,
				From2 = from2,
				To2 = to2,
				Start = start,
				End = end,
				EarlierStart = earlierStart,
				EarlierEnd = earlierEnd,
				Understood = understood,
				WholeRecord = whole,
			};

			return reader.Read(
				api => TheSpendOfThisVehicle(api, asked),
				() => new SpendOnThisCar
				{
					HasData = false,
					BoundsUnderstood = understood,
					From = understood ? TheText(start) : TheBoundAsGiven(from),
					To = understood ? TheText(end) : TheBoundAsGiven(to),
					From2 = understood ? TheText(earlierStart) : TheBoundAsGiven(from2),
					To2 = understood ? TheText(earlierEnd) : TheBoundAsGiven(to2),
					WholeRecord = whole,
					Folder = reader.Folder,
					Detail = TheAssistantIsTold.EverythingWorthSaying(
						TheAssistantIsTold.TheAppHasNotBeenOpened,
						understood ? "" : TheBoundDoesNotSayACalendarDay),
				});
		}

		private SpendOnThisCar TheSpendOfThisVehicle(
			DealershipCarAPI api, TheFourBoundsAsGiven asked)
		{
			if (!asked.Understood)
				return new SpendOnThisCar
				{
					HasData = true,
					Vin = api.ReadVin(),
					BoundsUnderstood = false,
					From = TheBoundAsGiven(asked.From),
					To = TheBoundAsGiven(asked.To),
					From2 = TheBoundAsGiven(asked.From2),
					To2 = TheBoundAsGiven(asked.To2),
					WholeRecord = asked.WholeRecord,
					Folder = reader.Folder,
					Detail = TheAssistantIsTold.EverythingWorthSaying(
						TheBoundDoesNotSayACalendarDay,
						TheAssistantIsTold.MoreThanOneVehicle(reader)),
				};

			CostLedgerRecord record = api.CostEntriesBetween(
				asked.Start, asked.End, TheVoiceOfTheDealership);

			var rows = new List<CostEntryRow>();

			foreach (int row in Enumerable.Range(0, record.Says.Length))
				rows.Add(new CostEntryRow
				{
					IncurredAt = record.IncurredAt[row],
					Subject = record.Subject[row],
					Money = record.Money[row],
					Classification = record.Classification[row],
					ExpectedLife = record.ExpectedLife[row],
					Says = record.Says[row],
				});

			bool oneCurrency = api.IsSpendSaidInOneCurrencyBetween(asked.Start, asked.End);
			bool perDistance = api.CanCostPerDistanceBeSaid(asked.Start, asked.End);

			return new SpendOnThisCar
			{
				HasData = true,
				Vin = api.ReadVin(),
				BoundsUnderstood = true,
				From = TheText(asked.Start),
				To = TheText(asked.End),
				From2 = TheText(asked.EarlierStart),
				To2 = TheText(asked.EarlierEnd),
				WholeRecord = asked.WholeRecord,
				AnyCost = api.HasAnyCostBetween(asked.Start, asked.End),
				SaidInOneCurrency = oneCurrency,
				Says = api.DescribeSpendBetween(
					asked.Start, asked.End, TheVoiceOfTheDealership),
				PerCurrencySays = api.DescribeSpendPerCurrencyBetween(
					asked.Start, asked.End, TheVoiceOfTheDealership),
				BothPeriodsSay = api.DescribeTwoPeriodsOfSpendCompared(
					asked.Start, asked.End, asked.EarlierStart, asked.EarlierEnd,
					TheVoiceOfTheDealership),
				CostPerDistanceCanBeSaid = perDistance,
				CostPerDistanceSays = api.DescribeCostPerDistanceBetween(
					asked.Start, asked.End, TheVoiceOfTheDealership),
				EntryCount = api.CostEntryCountBetween(asked.Start, asked.End),
				Entries = rows,
				Folder = reader.Folder,
				Detail = TheAssistantIsTold.EverythingWorthSaying(
					TheTwoViewsAreTheAnswerAndNeitherIsSaidAlone,
					oneCurrency ? "" : TheTwoCurrenciesAreAStateAndNotAGap,
					perDistance ? "" : TheCostPerDistanceIsSuspendedAndThatIsAnAnswer,
					TheSecondPeriodIsTheOneToName,
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

		/// <summary>
		/// Los cuatro limites tal como llegaron y tal como quedaron resueltos, para que el cuerpo no
		/// tome ocho argumentos sueltos que se pueden cruzar entre si.
		/// </summary>
		private sealed class TheFourBoundsAsGiven
		{
			internal string From { get; init; } = "";

			internal string To { get; init; } = "";

			internal string From2 { get; init; } = "";

			internal string To2 { get; init; } = "";

			internal DateTime Start { get; init; }

			internal DateTime End { get; init; }

			internal DateTime EarlierStart { get; init; }

			internal DateTime EarlierEnd { get; init; }

			internal bool Understood { get; init; }

			internal bool WholeRecord { get; init; }
		}

		private const string TheOnlyShapeADayIsWrittenIn = "yyyy-MM-dd";

		private const string TheShapeAnInstantIsSaidIn = "yyyy-MM-dd HH:mm:ss";

		/// <summary>
		/// El primer instante que un calendario puede nombrar. Es lo que quiere decir 'todo el
		/// registro' cuando no se pone limite por ese lado: el registro no tiene extremos propios que
		/// preguntar, y ninguna lectura contesta cual fue el primer gasto ni el ultimo.
		/// </summary>
		private static readonly DateTime TheFirstInstantACalendarCanName =
			new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		/// <summary>
		/// El ultimo, por lo mismo, en su ultimo segundo entero: el journal guarda segundos enteros,
		/// asi que este es el instante mas tarde que sobrevive de ida y de vuelta sin recortarse.
		/// </summary>
		private static readonly DateTime TheLastInstantACalendarCanName =
			new DateTime(9999, 12, 31, 23, 59, 59, DateTimeKind.Utc);

		private const string TheTwoViewsAreTheAnswerAndNeitherIsSaidAlone =
			"says y perCurrencySays traen LAS DOS VISTAS adentro -- lo que salió del banco y lo que "
			+ "el período consumió -- y el registro no tiene ninguna lectura que conteste una sola. "
			+ "Diga las dos: la distinción ES la respuesta, porque sin ella un total dice que el "
			+ "carro se puso caro cuando lo que pasó fue que compró llantas. Y no sume las filas: el "
			+ "total ya está compuesto, y el suyo saldría distinto.";

		private const string TheTwoCurrenciesAreAStateAndNotAGap =
			"En este período el gasto está en MÁS DE UNA MONEDA, así que cada una se dice por su "
			+ "lado y NO hay un total entre ellas. Eso es un estado con nombre y no un dato que "
			+ "falte: no existe ningún factor con que sumarlas, y este registro no convierte moneda "
			+ "nunca. No invente un total, no elija la mayor y no conteste cero.";

		private const string TheCostPerDistanceIsSuspendedAndThatIsAnAnswer =
			"En este período NO se puede decir cuánto costó la distancia, y eso es una respuesta y "
			+ "no un hueco: costPerDistanceSays dice por qué -- o no hay ningún gasto, o no hay un "
			+ "tramo de uso que medir. NO conteste cero, NO estime y NO divida el gasto entre "
			+ "kilómetros que usted saque de otra respuesta.";

		private const string TheSecondPeriodIsTheOneToName =
			"bothPeriodsSay compara el primer período contra el segundo, y los nombra los dos con "
			+ "sus límites. Si from2 y to2 vinieron en blanco, el segundo período es todo el "
			+ "registro: la comparación sigue siendo verdadera y dice poco. Para comparar dos "
			+ "períodos de verdad, nómbrelos.";

		private const string TheBoundDoesNotSayACalendarDay =
			"Uno de los cuatro límites de los períodos no dice un día de calendario, así que no se "
			+ "preguntó nada y el gasto, la comparación, las filas y la cuenta vienen vacíos. NO es "
			+ "que no haya gastos: es que no se eligió ningún período. Vuelva a preguntar con los "
			+ "límites en yyyy-MM-dd, u omítalos para preguntar sobre todo el registro.";
	}

	/// <summary>Un gasto del periodo como fila: cuando, en que, cuanto, de que clase y por cuanto.</summary>
	internal sealed class CostEntryRow
	{
		[JsonPropertyName("incurredAt")]
		[Description("Cuándo se incurrió este gasto, con su hora. Es el instante que llegó con el "
			+ "hecho, no el día en que alguien lo anotó.")]
		public string IncurredAt { get; init; } = "";

		[JsonPropertyName("subject")]
		[Description("En qué se fue, con las palabras que mandó quien lo registró. Es texto "
			+ "recibido y no una categoría: no hay ninguna lista cerrada de asuntos, así que no "
			+ "agrupe por él ni lo traduzca a una clase suya.")]
		public string Subject { get; init; } = "";

		[JsonPropertyName("money")]
		[Description("Lo que costó, dicho por su propia moneda: con su signo y sus decimales. "
			+ "Cítelo tal cual. No lo sume con el de otra fila -- el total ya está compuesto arriba "
			+ "-- y menos con el de una fila en otra moneda.")]
		public string Money { get; init; } = "";

		[JsonPropertyName("classification")]
		[Description("Si es de los que se consumen en el período en que se pagaron o de los que "
			+ "siguen dando valor después. La dijo quien lo registró y el registro nunca la deduce "
			+ "del asunto ni del monto.")]
		public string Classification { get; init; } = "";

		[JsonPropertyName("expectedLife")]
		[Description("Cuánto se espera que dure este gasto, cuando es de los que duran. Viene "
			+ "vacío en los que se consumen en su propio período: eso quiere decir que no tiene "
			+ "vida útil, no que sea cero.")]
		public string ExpectedLife { get; init; } = "";

		[JsonPropertyName("says")]
		[Description("Este gasto en la frase que compuso el registro mismo: cuánto, en qué, "
			+ "cuándo, de qué clase, y con qué regla se repartió si es de los que duran. Dígala en "
			+ "vez de rearmarla con los otros campos.")]
		public string Says { get; init; } = "";
	}

	/// <summary>
	/// En que se le fue la plata a esta persona con este vehiculo en un periodo: las dos vistas
	/// dichas juntas, ese periodo contra otro, lo que costo la distancia, y los gastos como filas.
	/// </summary>
	internal sealed class SpendOnThisCar
	{
		[JsonPropertyName("hasData")]
		[Description("false cuando la app todavía no se ha abierto en esta máquina; entonces el "
			+ "resto viene vacío.")]
		public bool HasData { get; init; }

		[JsonPropertyName("vin")]
		[Description("El vehículo del que habla esta respuesta.")]
		public string Vin { get; init; } = "";

		[JsonPropertyName("boundsUnderstood")]
		[Description("false cuando uno de los cuatro límites no dice un día en yyyy-MM-dd. Entonces "
			+ "no se preguntó nada y todo lo de abajo viene vacío porque no se eligió ningún "
			+ "período, no porque no haya gastos: lea detail y vuelva a preguntar.")]
		public bool BoundsUnderstood { get; init; }

		[JsonPropertyName("from")]
		[Description("El primer instante del PRIMER período con el que se contestó. Cítelo: si "
			+ "nadie puso límite por ese lado, aquí viene el instante más antiguo que un calendario "
			+ "puede nombrar. Con boundsUnderstood en false aquí viene el texto que no se entendió.")]
		public string From { get; init; } = "";

		[JsonPropertyName("to")]
		[Description("El último instante del primer período, inclusive. Un día escrito como límite "
			+ "llega hasta su último segundo, así que un gasto de esa misma tarde entra.")]
		public string To { get; init; } = "";

		[JsonPropertyName("from2")]
		[Description("El primer instante del SEGUNDO período, que es con el que se compara. Solo "
			+ "bothPeriodsSay lo usa: says, las filas y el costo por distancia son del primero.")]
		public string From2 { get; init; } = "";

		[JsonPropertyName("to2")]
		[Description("El último instante del segundo período, inclusive.")]
		public string To2 { get; init; } = "";

		[JsonPropertyName("wholeRecord")]
		[Description("true cuando el PRIMER período no tiene ningún límite puesto y cubre todo el "
			+ "registro. false quiere decir que hay un período elegido: entonces un gasto fuera de "
			+ "él no está ausente del registro, está fuera de la pregunta.")]
		public bool WholeRecord { get; init; }

		[JsonPropertyName("anyCost")]
		[Description("true si el primer período tiene algún gasto, salido del banco adentro o "
			+ "consumido adentro. false NO es un cero: es que no hay ningún gasto registrado, que "
			+ "es otra cosa que gastar cero.")]
		public bool AnyCost { get; init; }

		[JsonPropertyName("saidInOneCurrency")]
		[Description("true si todo el gasto del primer período está dicho en una sola moneda. false "
			+ "quiere decir que hay más de una y que NO hay un total entre ellas, o que no hay "
			+ "ningún gasto y entonces no hay moneda en la que decirlo. Ninguno de los dos casos es "
			+ "un error.")]
		public bool SaidInOneCurrency { get; init; }

		[JsonPropertyName("says")]
		[Description("EL GASTO DEL PRIMER PERÍODO, en una frase que compuso el registro y que trae "
			+ "LAS DOS VISTAS adentro: lo que salió del banco y lo que el período consumió, con la "
			+ "regla con que se repartió cada gasto de los que duran. Dígala entera en vez de "
			+ "quedarse con una de las dos cifras.")]
		public string Says { get; init; } = "";

		[JsonPropertyName("perCurrencySays")]
		[Description("EL MISMO GASTO DICHO MONEDA POR MONEDA, con las dos vistas adentro también y "
			+ "diciendo además en cuántas monedas está. Con una sola moneda lo dice; con más de "
			+ "una, dice que no hay total entre ellas y da cada una por su lado.")]
		public string PerCurrencySays { get; init; } = "";

		[JsonPropertyName("bothPeriodsSay")]
		[Description("LOS DOS PERÍODOS EN UNA SOLA FRASE, cada uno nombrado con sus límites y con "
			+ "sus dos vistas. Ninguna diferencia entre ellos llega como número, a propósito: una "
			+ "resta entre dos períodos en monedas distintas no querría decir nada.")]
		public string BothPeriodsSay { get; init; } = "";

		[JsonPropertyName("costPerDistanceCanBeSaid")]
		[Description("true si se puede decir cuánto costó la distancia del primer período. Hacen "
			+ "falta las dos cosas: un tramo de uso que medir y algún gasto. false no es un error y "
			+ "la frase de abajo dice cuál de las dos falta.")]
		public bool CostPerDistanceCanBeSaid { get; init; }

		[JsonPropertyName("costPerDistanceSays")]
		[Description("LO QUE COSTÓ LA DISTANCIA del primer período, moneda por moneda y con las dos "
			+ "unidades dichas, o la razón con nombre que impide decirlo. Solo cuentan los gastos "
			+ "posteriores a la primera lectura del odómetro del período, y la frase nombra esa "
			+ "lectura: lo que se pagó en ella movió los kilómetros del período anterior.")]
		public string CostPerDistanceSays { get; init; } = "";

		[JsonPropertyName("entryCount")]
		[Description("Cuántos gastos se incurrieron dentro del primer período. Es la cuenta de las "
			+ "filas de abajo y no una medida del carro.")]
		public int EntryCount { get; init; }

		[JsonPropertyName("entries")]
		[Description("Una fila por gasto incurrido dentro del primer período, en orden del instante "
			+ "en que se incurrió. Cada fila se dice sola: lleva su instante, su asunto, su dinero "
			+ "con su moneda, su clase, su vida útil si tiene y su propia frase.")]
		public IReadOnlyList<CostEntryRow> Entries { get; init; } = new List<CostEntryRow>();

		[JsonPropertyName("folder")]
		[Description("La carpeta de la persona, donde su app guarda el registro de este vehículo.")]
		public string Folder { get; init; } = "";

		[JsonPropertyName("detail")]
		[Description("Lo que esta respuesta no dice, y lo que no hay que hacer con ella. Léalo "
			+ "antes de contestar.")]
		public string Detail { get; init; } = "";
	}
}
