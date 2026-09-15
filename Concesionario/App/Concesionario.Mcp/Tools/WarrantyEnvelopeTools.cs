using System.ComponentModel;
using System.Text.Json.Serialization;
using Concesionario.App;
using ModelContextProtocol.Server;

namespace Concesionario.Mcp.Tools
{
	/// <summary>
	/// La pregunta del asistente sobre el sobre de garantia que LA AGENCIA publico para ESTE
	/// vehiculo: si todavia esta adentro y cuanto le queda de cada mitad. Solo lectura, y a nadie se
	/// le pide un dato del carro: los siete hechos salen del registro de la persona.
	/// </summary>
	[McpServerToolType]
	internal sealed class WarrantyEnvelopeTools
	{
		/// <summary>
		/// EL IDIOMA EN QUE ESTA CARA PIDE LAS FRASES DEL DOMINIO, que es el del registro y no el de
		/// quien pregunta: esta superficie es la voz del concesionario hablandole al modelo, igual
		/// que el titulo y las descripciones de estas mismas tools.
		/// </summary>
		private const string TheVoiceOfTheDealership = Concesionario.App.Wording.Phrases.LanguageOfTheRecord;

		private readonly JournalReader reader;

		public WarrantyEnvelopeTools(JournalReader reader)
		{
			ArgumentNullException.ThrowIfNull(reader);

			this.reader = reader;
		}

		[McpServerTool(Name = "is_my_car_still_under_warranty", UseStructuredContent = true)]
		[Description("Contesta si el vehículo de esta persona sigue dentro del sobre de garantía "
			+ "que el concesionario le publicó, y cuánto le queda de cada mitad. Úsela para '¿mi carro todavía "
			+ "tiene garantía?', '¿hasta cuándo estoy cubierto?', '¿cuánta garantía me queda?'. Todo "
			+ "lo que necesita sale del registro propio de la persona -- el sobre, la fecha en que se "
			+ "entregó el carro, la lectura del odómetro tomada ese día y la última que ha traído el "
			+ "feed del concesionario --, así que nunca les pida un kilometraje ni una fecha, y nunca "
			+ "acepte uno para devolverlo como dato. "
			+ "UN SOBRE TIENE DOS MITADES Y LAS DOS SE DICEN SIEMPRE: un plazo en meses y una "
			+ "distancia. La cobertura termina cuando se acaba la PRIMERA de las dos, así que citar "
			+ "solo la mitad cómoda es la forma de darle a alguien una respuesta equivocada que suena "
			+ "bien. Cuál mitad va a llegar primero no se pronostica a propósito -- eso necesitaría un "
			+ "ritmo que este registro no tiene --, así que diga las dos y diga que depende de cuánto "
			+ "manejen. "
			+ "DICE POSICIÓN Y NUNCA ADJUDICA UN RECLAMO. Estar dentro del sobre no es una promesa de "
			+ "que una reparación se pague; eso lo decide el fabricante. "
			+ "LO QUE DICE says ES LA RESPUESTA. Es una frase que compuso el registro mismo y ya lleva "
			+ "adentro la posición, las dos mitades, cuál llegó primero, el sobre contra el que se "
			+ "midió con su fecha de publicación, y el odómetro contra el que se midió con el día en "
			+ "que se observó esa lectura. Dígala en vez de rearmarla a partir de los campos, y diga "
			+ "la fecha de observación: si tiene meses, la mitad de distancia es igual de vieja, y "
			+ "presentarla como la de hoy es el error que hay que evitar. "
			+ "CUANDO FALTA ALGO SE NOMBRA, NO SE ESTIMA. La frase de says pone en palabras el hueco "
			+ "mismo y nombra qué hecho lo decidiría; el concesionario es quien registra estos, nunca la "
			+ "persona y nunca usted. Un sobre que nunca se publicó no es un carro sin garantía. "
			+ "Para lo que el FABRICANTE publicó sobre el alcance de la garantía -- qué sistemas están "
			+ "adentro y qué exclusiones muerden -- use does_the_warranty_cover; esa además contesta "
			+ "dónde está parado este vehículo en un sistema que la persona nombre.")]
		public WarrantyStanding IsMyCarStillUnderWarranty()
		{
			return reader.Read(TheEnvelopeOfThisVehicle, () => new WarrantyStanding
			{
				HasData = false,
				Folder = reader.Folder,
				Detail = TheAssistantIsTold.TheAppHasNotBeenOpened,
			});
		}

		private WarrantyStanding TheEnvelopeOfThisVehicle(DealershipCarAPI api)
		{
			if (!TheAssistantIsTold.TheVehiclesOwnRecordIsThere(api))
				return new WarrantyStanding
				{
					HasData = true,
					Vin = api.ReadVin(),
					EnvelopeKnown = false,
					Folder = reader.Folder,
					Detail = TheAssistantIsTold.EverythingWorthSaying(
						TheAssistantIsTold.TheAppIsOlderThanTheVehiclesOwnRecord,
						TheAssistantIsTold.MoreThanOneVehicle(reader)),
				};

			DateTime askedAt = TheClockTheAssistantReads.TheInstantThisWasAsked();

			bool published = api.IsWarrantyEnvelopePublished();
			bool delivered = api.HasDeliveryBeenRecorded();
			bool measured = api.HasAnOdometerReading(askedAt);

			bool contradicts = false;
			bool canBeEvaluated = false;
			string standing = "";
			string says = "";

			try
			{
				canBeEvaluated = api.CanTheEnvelopeBeEvaluated(askedAt);
				standing = canBeEvaluated ? api.ReadEnvelopeStanding(askedAt) : "";
				says = api.DescribeWarrantyEnvelope(askedAt, TheVoiceOfTheDealership);
			}
			catch (Exception thrown) when (TheAssistantIsTold.TheRecordContradictsItself(thrown))
			{
				contradicts = true;
				canBeEvaluated = false;
				standing = "";
				says = "";
			}

			return new WarrantyStanding
			{
				HasData = true,
				Vin = api.ReadVin(),
				EnvelopeKnown = true,
				AskedAt = askedAt.ToString("O"),
				EnvelopePublished = published,
				PublishedSays = api.DescribePublishedWarrantyEnvelope(TheVoiceOfTheDealership),
				PublishedAt = published ? api.ReadEnvelopePublishedAt().ToString("O") : "",
				DeliveryRecorded = delivered,
				DeliveredAt = delivered ? api.ReadDeliveredAt().ToString("O") : "",
				DeliveryOdometer = delivered ? api.ReadDeliveryReading() : 0,
				DeliveryOdometerUnit = delivered ? api.ReadDeliveryUnit() : "",
				OdometerKnown = measured,
				Odometer = measured ? api.ReadOdometer(askedAt) : 0,
				OdometerUnit = measured ? api.ReadOdometerUnit(askedAt) : "",
				OdometerObservedAt =
					measured ? api.ReadOdometerObservedAt(askedAt).ToString("O") : "",
				CanBeEvaluated = canBeEvaluated,
				Standing = standing,
				Says = says,
				RecordContradictsItself = contradicts,
				Folder = reader.Folder,
				Detail = TheAssistantIsTold.EverythingWorthSaying(
					contradicts ? TheAssistantIsTold.TheRecordSaysTwoThingsThatCannotBothBeTrue : "",
					!contradicts && (!published || !delivered) ? TheDealershipHasNotSaidItYet : "",
					!contradicts && published && delivered && !measured
						? NoReadingHasArrivedForTheDistanceHalf
						: "",
					!contradicts && measured ? TheDistanceHalfIsAsOldAsItsReading : "",
					TheAssistantIsTold.MoreThanOneVehicle(reader)),
			};
		}

		private const string TheDealershipHasNotSaidItYet =
			"Parte de lo que decide esto no está en el registro: el sobre mismo, o la fecha en que se "
			+ "entregó el carro. says nombra cuál de los dos con las palabras del registro: cítela. "
			+ "El concesionario es quien declara los dos, por el mismo feed que trae todo lo demás aquí, así "
			+ "que la persona no los puede aportar y usted tampoco. Un sobre que no está registrado NO "
			+ "es un vehículo sin garantía, y reportarlo como tal es el error que hay que evitar aquí. "
			+ "Dígales que le pidan a su concesionario que lo registre.";

		private const string NoReadingHasArrivedForTheDistanceHalf =
			"El sobre y la entrega están los dos registrados, pero no ha llegado ninguna lectura del "
			+ "odómetro al instante preguntado ni antes, así que solo se pudo medir la mitad de "
			+ "tiempo. Diga la mitad de tiempo y diga que la de distancia está sin medir: no que está "
			+ "bien, y no cero. La lectura viene del feed del concesionario cuando la persona abre su app.";

		private const string TheDistanceHalfIsAsOldAsItsReading =
			"La mitad de distancia se midió contra la lectura que tiene el registro, tomada en la "
			+ "fecha que está en odometerObservedAt, y no contra donde está el carro hoy. Diga esa "
			+ "fecha. Si es vieja, la distancia que queda está al menos así de desactualizada y el "
			+ "carro puede haber pasado ya el límite; pídale a la persona que abra su app para que el "
			+ "feed traiga una lectura al día.";
	}

	/// <summary>
	/// Donde esta parado este vehiculo contra el sobre que el concesionario le publico, con los hechos del
	/// registro contra los que se midio.
	/// </summary>
	internal sealed class WarrantyStanding
	{
		[JsonPropertyName("hasData")]
		[Description("false cuando la app todavía no ha registrado nada; entonces el resto viene "
			+ "vacío.")]
		public bool HasData { get; init; }

		[JsonPropertyName("vin")]
		[Description("El vehículo del que habla esta respuesta.")]
		public string Vin { get; init; } = "";

		[JsonPropertyName("envelopeKnown")]
		[Description("false cuando este registro lo escribió una versión de la app anterior a lo que "
			+ "el concesionario registra sobre este vehículo. Entonces no viene lleno nada de lo de abajo: "
			+ "lea detail. NO quiere decir que el carro no tenga garantía.")]
		public bool EnvelopeKnown { get; init; }

		[JsonPropertyName("askedAt")]
		[Description("El instante en que se hizo esta pregunta, en UTC. Toda respuesta de aquí es a "
			+ "ese instante y no a ningún otro.")]
		public string AskedAt { get; init; } = "";

		[JsonPropertyName("envelopePublished")]
		[Description("true cuando el concesionario ha publicado un sobre de garantía para este vehículo. "
			+ "false quiere decir que aquí no hay ninguno registrado, que no es lo mismo que el carro "
			+ "no tenga garantía, y nunca se puede reportar como 'se le acabó la garantía'.")]
		public bool EnvelopePublished { get; init; }

		[JsonPropertyName("publishedSays")]
		[Description("El sobre tal como lo publicó el concesionario -- los dos límites, su unidad y la "
			+ "fecha en que se publicó -- y nada de este vehículo. Contesta incluso cuando no hay nada "
			+ "publicado, así que siempre se puede citar, y es la mitad que hay que citar cuando la "
			+ "posición no se pudo medir.")]
		public string PublishedSays { get; init; } = "";

		[JsonPropertyName("publishedAt")]
		[Description("Cuándo publicó el concesionario el sobre que rige. VACÍO quiere decir que no hay "
			+ "ninguno publicado. Un concesionario puede publicar un sobre nuevo, y uno posterior aplica "
			+ "desde su propia fecha hacia adelante, así que esta fecha es la procedencia de todos los "
			+ "números de aquí.")]
		public string PublishedAt { get; init; } = "";

		[JsonPropertyName("deliveryRecorded")]
		[Description("true cuando la fecha en que se entregó el carro está en el registro. El plazo "
			+ "se cuenta desde esa fecha, así que sin ella la mitad de tiempo no se puede medir en "
			+ "absoluto.")]
		public bool DeliveryRecorded { get; init; }

		[JsonPropertyName("deliveredAt")]
		[Description("La fecha en que el concesionario entregó el carro, en UTC. VACÍO quiere decir que no "
			+ "está registrada. Si esta fecha es POSTERIOR a askedAt, la fecha de entrega del registro "
			+ "todavía no ha ocurrido y el plazo se está contando desde una fecha futura: repórtelo "
			+ "como un registro que el concesionario tiene que corregir, nunca como garantía restante.")]
		public string DeliveredAt { get; init; } = "";

		[JsonPropertyName("deliveryOdometer")]
		[Description("La lectura del odómetro tomada el día en que se entregó el carro. Es el ancla "
			+ "desde la que cuenta la mitad de distancia. CERO con deliveryRecorded en false quiere "
			+ "decir que no está registrada: eso no es 'el carro tenía cero encima'.")]
		public int DeliveryOdometer { get; init; }

		[JsonPropertyName("deliveryOdometerUnit")]
		[Description("La unidad en que está esa lectura ancla. Aquí las unidades nunca se convierten, "
			+ "así que diga el valor con la unidad exactamente como viene.")]
		public string DeliveryOdometerUnit { get; init; } = "";

		[JsonPropertyName("odometerKnown")]
		[Description("true cuando el feed del concesionario ha traído una lectura observada a askedAt o "
			+ "antes. false quiere decir que la mitad de distancia no se pudo medir en absoluto, que "
			+ "no es lo mismo que el carro no haya recorrido nada.")]
		public bool OdometerKnown { get; init; }

		[JsonPropertyName("odometer")]
		[Description("La lectura contra la que se midió la mitad de distancia. CERO con odometerKnown "
			+ "en false quiere decir que no hay lectura, no una lectura de cero.")]
		public int Odometer { get; init; }

		[JsonPropertyName("odometerUnit")]
		[Description("La unidad en que está esa lectura. Nunca se convierte, y una lectura en una "
			+ "unidad distinta de aquella en que se publicó el sobre suspende la mitad de distancia en "
			+ "vez de convertirse: says nombra las dos unidades cuando eso pasa.")]
		public string OdometerUnit { get; init; } = "";

		[JsonPropertyName("odometerObservedAt")]
		[Description("Cuándo se OBSERVÓ esa lectura, no cuándo se pidió. Esta es la fecha a la que "
			+ "está realmente la mitad de distancia, y decirla es lo que impide que una lectura vieja "
			+ "se repita como el kilometraje de hoy.")]
		public string OdometerObservedAt { get; init; } = "";

		[JsonPropertyName("canBeEvaluated")]
		[Description("true cuando el registro pudo decidir dónde está parado este vehículo a askedAt. "
			+ "Ojo: un vehículo cuyo concesionario nunca publicó un sobre contesta TRUE aquí: que nadie "
			+ "haya publicado uno es algo que el registro sabe con certeza, y entonces standing lee "
			+ "NotPublished. false quiere decir que no aplica ningún nombre: lea says.")]
		public bool CanBeEvaluated { get; init; }

		[JsonPropertyName("standing")]
		[Description("Dónde está parado el vehículo, en una palabra. VACÍO quiere decir que no se "
			+ "pudo decidir; aquí no hay ninguna palabra que quiera decir 'no se sabe', y por eso "
			+ "viene vacío en vez de adivinar una. Lea says de todos modos: esa es la respuesta, esto "
			+ "es solo su nombre.")]
		public string Standing { get; init; } = "";

		[JsonPropertyName("says")]
		[Description("LA RESPUESTA, en una frase que compuso el registro: la posición, LAS DOS mitades "
			+ "con lo que queda o cuánto se pasó, cuál mitad llegó primero, el sobre contra el que se "
			+ "midió con su fecha de publicación, y el odómetro contra el que se midió con el día en "
			+ "que se observó; o, cuando falta algo, qué hecho falta y por qué esa mitad no se pudo "
			+ "medir. Diga esta frase. No la rearme a partir de los otros campos y no la recorte a la "
			+ "mitad que suena mejor.")]
		public string Says { get; init; } = "";

		[JsonPropertyName("recordContradictsItself")]
		[Description("true cuando dos hechos que el concesionario registró no pueden ser ciertos los dos a "
			+ "askedAt, así que no se pudo medir nada y standing y says vienen vacíos. Es una falla "
			+ "del registro y no de la pregunta: lea detail y dígale a la persona que lo plantee con "
			+ "su concesionario.")]
		public bool RecordContradictsItself { get; init; }

		[JsonPropertyName("folder")]
		[Description("La carpeta de la persona, donde su app guarda el registro de este vehículo.")]
		public string Folder { get; init; } = "";

		[JsonPropertyName("detail")]
		[Description("Lo que esta respuesta no dice, y cualquier cosa que a la persona le convenga "
			+ "hacer. Léalo antes de contestar: ahí están los límites de la respuesta.")]
		public string Detail { get; init; } = "";
	}
}
