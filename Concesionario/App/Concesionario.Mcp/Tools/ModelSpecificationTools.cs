using System.ComponentModel;
using System.Text.Json.Serialization;
using Concesionario.App;
using ModelContextProtocol.Server;
using Puppeteer;

namespace Concesionario.Mcp.Tools
{
	/// <summary>
	/// Las tres preguntas del asistente sobre lo que el FABRICANTE publico del modelo de este
	/// vehiculo. Solo lectura, igual que las de la agenda: ninguna performa un comando.
	/// <para>
	/// La de cobertura ademas dice donde esta parado ESTE carro contra ese alcance, y los siete
	/// hechos que eso necesita los saca del registro de la persona: no se le piden a nadie.
	/// </para>
	/// </summary>
	[McpServerToolType]
	internal sealed class ModelSpecificationTools
	{
		private readonly JournalReader reader;

		public ModelSpecificationTools(JournalReader reader)
		{
			ArgumentNullException.ThrowIfNull(reader);

			this.reader = reader;
		}

		[McpServerTool(Name = "what_my_car_takes", UseStructuredContent = true)]
		[Description("Contesta lo que el fabricante publicó que lleva el vehículo de esta persona: el "
			+ "aceite de motor y los otros cinco fluidos con su grado, capacidad e intervalo de "
			+ "cambio; la medida y la presión en frío de las llantas en cada posición; y los grados de "
			+ "combustible, separados en el mínimo, los permitidos y los prohibidos. Úsela para '¿qué "
			+ "aceite lleva mi carro?', '¿a qué presión van mis llantas?', '¿le puedo echar regular?'. "
			+ "Reporta la ficha entera en una sola lectura, así que prefiérala a preguntar ítem por "
			+ "ítem. Cada respuesta lleva la revisión de la ficha de la que salió: diga esa revisión, "
			+ "porque un número sin ella es un rumor y una revisión posterior puede haberlo cambiado. "
			+ "Un ítem cuyo published es false nunca se publicó para este modelo: eso es una respuesta, "
			+ "no un hueco, y nunca se puede llenar con un modelo parecido ni con un año anterior. Las "
			+ "presiones, las capacidades y las distancias llevan una unidad que nunca se convierte: "
			+ "diga el valor con la unidad exactamente como viene. De las tres listas de combustible, "
			+ "la de prohibidos es la que vale la pena plantear primero.")]
		public SpecificationSheet WhatMyCarTakes()
		{
			return reader.Read(TheWholeSpecSheet, () => new SpecificationSheet
			{
				HasData = false,
				Folder = reader.Folder,
				Detail = TheAssistantIsTold.TheAppHasNotBeenOpened,
			});
		}

		[McpServerTool(Name = "does_the_warranty_cover", UseStructuredContent = true)]
		[Description("Contesta lo que el fabricante publicó sobre el ALCANCE de la garantía del modelo "
			+ "de esta persona -- qué sistemas declara adentro el alcance, qué exclusiones publicó y "
			+ "bajo qué condición muerde cada una, y si un sistema que la persona nombre está siquiera "
			+ "en el catálogo del fabricante -- Y dónde está parado ESTE vehículo frente a ese alcance "
			+ "en el sistema que nombraron. Úsela para '¿mi caja tiene garantía?' o '¿qué no cubre la "
			+ "garantía?'. "
			+ "DICE ALCANCE Y POSICIÓN, Y NUNCA ADJUDICA UN RECLAMO. Que una reparación se pague lo "
			+ "decide el fabricante y nada de aquí dice que se vaya a pagar, así que nunca convierta un "
			+ "sistema que está dentro del alcance, ni un vehículo que está dentro de su sobre, en "
			+ "'usted está cubierto'. "
			+ "LA POSICIÓN SALE DEL REGISTRO PROPIO DE LA PERSONA y nunca de usted ni de ella. "
			+ "Necesita el sobre de garantía del vehículo, la fecha en que se entregó, el odómetro "
			+ "tomado ese día y el último que haya traído el feed; los cuatro se leen aquí. Nunca le "
			+ "pida a la persona un kilometraje ni una fecha para contestar esto, y nunca acepte uno: "
			+ "un número que no salió del registro volvería como una medición que nadie hizo. Cuando "
			+ "falta uno de los cuatro, standingSays viene vacío y says y thisVehicleSays contestan "
			+ "cada uno su propia mitad entera: cite las dos, y lea detail para saber qué hecho falta "
			+ "y quién lo registra. "
			+ "'NO NOS LO DIJERON' NO ES 'NO ESTÁ CUBIERTO', y confundir los dos es la falla que hay "
			+ "que evitar: systemInTheCatalogue en false quiere decir que el alcance publicado nunca "
			+ "nombra ese sistema, lo cual no dice nada ni a favor ni en contra de que el fabricante lo "
			+ "cubra. Un código que está en coveredSystems es uno que el alcance declara adentro; un "
			+ "código que aparece solo entre las exclusiones es uno que el alcance nombra y deja fuera. "
			+ "El código de sistema es el vocabulario DEL FABRICANTE y no una lista de esta app, así "
			+ "que tómelo de coveredSystems o de una exclusión y pásela letra por letra: la coincidencia "
			+ "es exacta, y un código escrito con otra ortografía o con otras mayúsculas contesta 'no "
			+ "nos lo dijeron'. No invente un código, y llame a la tool sin ningún sistema para ver "
			+ "primero el alcance publicado entero.")]
		public WarrantyScope DoesTheWarrantyCover(
			[Description("El código del fabricante para el sistema por el que se pregunta, copiado letra "
				+ "por letra de coveredSystems o de una exclusión. Omítalo para leer el alcance "
				+ "publicado entero, que es de donde salen los códigos.")]
			string system = "")
		{
			string asked = system == null ? "" : system.Trim();

			return reader.Read(
				api => ThePublishedScope(api, asked),
				() => new WarrantyScope
				{
					HasData = false,
					System = asked,
					Folder = reader.Folder,
					Detail = TheAssistantIsTold.TheAppHasNotBeenOpened,
				});
		}

		[McpServerTool(Name = "what_the_service_costs", UseStructuredContent = true)]
		[Description("Contesta lo que el fabricante publicó sobre el servicio de UNA operación de "
			+ "mantenimiento en este modelo: si se publicó un intervalo de servicio para ella, y TODOS "
			+ "los precios que hay publicados para ella, cada uno con el hito al que corresponde, su "
			+ "vigencia y si esa vigencia rige al instante en que se hizo la pregunta. Úsela para "
			+ "'¿cuánto cuesta el servicio?', '¿cuánto cuesta el de los 10.000?' o '¿ese precio sigue "
			+ "vigente?'. "
			+ "NO HAY QUE NOMBRAR NINGÚN HITO Y NO SE PUEDE. Los hitos los publica el fabricante y esta "
			+ "respuesta los ENUMERA: vienen todos, con el suyo escrito en cada fila. Si la persona "
			+ "nombró uno, búsquelo entre las filas que volvieron; si nombró uno que no está, dígale "
			+ "cuáles sí hay en vez de reportar que no hay precio. "
			+ "EL HITO DE CADA FILA SE DICE Y NO SE DIRECCIONA: viene con su unidad como la lee una "
			+ "persona, porque el registro nunca convierte entre sus dos unidades de distancia. Cítelo "
			+ "como viene. "
			+ "UN PRECIO NUNCA SE DICE SIN SU VIGENCIA: la frase lleva la ventana y si askedAt cae "
			+ "dentro de ella. Un precio viejo presentado como el de hoy es una promesa que la sucursal "
			+ "nunca hizo, y en la misma lista puede haber uno vigente y uno vencido. "
			+ "UNA LISTA VACÍA SON DOS RESPUESTAS DISTINTAS y anyPriceOnRecord dice cuál: con "
			+ "anyPriceOnRecord en false a este registro nunca le dijeron ningún precio de nada, y con "
			+ "anyPriceOnRecord en true hay precios publicados pero ninguno de ESTA operación. Lea "
			+ "detail. "
			+ "Los números del intervalo de servicio publicado no están en esta respuesta: citarlos "
			+ "necesita cuánto le falta a este vehículo contra ese intervalo, y este registro sí tiene "
			+ "el odómetro pero no devuelve esa distancia restante. Use what_is_left_before_each_service "
			+ "para cuánto le falta y review_my_maintenance para dónde está parado el plan. Los "
			+ "intervalos de cambio de fluido se publican como parte del fluido mismo, así que esos los "
			+ "lleva what_my_car_takes.")]
		public ServicePricing WhatTheServiceCosts(
			[Description("Por qué operación de mantenimiento está preguntando la persona.")]
			MaintenanceOperation operation)
		{
			return reader.Read(
				api => ThePublishedCostOf(api, operation),
				() => new ServicePricing
				{
					HasData = false,
					Operation = operation.WireName(),
					Folder = reader.Folder,
					Detail = TheAssistantIsTold.TheAppHasNotBeenOpened,
				});
		}

		private SpecificationSheet TheWholeSpecSheet(DealershipCarAPI api)
		{
			string model;

			if (!TheSpecificationIsThere(api, out model))
				return new SpecificationSheet
				{
					HasData = true,
					Vin = api.ReadVin(),
					SpecificationKnown = false,
					Folder = reader.Folder,
					Detail = TheDetailWhen(false),
				};

			var fluids = new List<PublishedFluid>();

			foreach (string kind in TheClosedSetsOfTheSheet.Fluids)
				fluids.Add(new PublishedFluid
				{
					Kind = kind,
					Published = api.IsFluidPublished(kind),
					Says = api.DescribeFluid(kind),
					Revision = api.ReadFluidRevision(kind),
				});

			var tyres = new List<PublishedTyre>();

			foreach (string position in TheClosedSetsOfTheSheet.TyrePositions)
				tyres.Add(new PublishedTyre
				{
					Position = position,
					Published = api.IsTyrePublished(position),
					Says = api.DescribeTyre(position),
					Revision = api.ReadTyreRevision(position),
				});

			return new SpecificationSheet
			{
				HasData = true,
				Vin = api.ReadVin(),
				SpecificationKnown = true,
				Model = model,
				CurrentRevision = TheCurrentRevisionCode(api),
				CurrentRevisionPublishedAt = TheInstantTheCurrentRevisionWasPublished(api),
				Fluids = fluids,
				Tyres = tyres,
				Fuel = new PublishedFuel
				{
					Published = api.IsFuelPublished(),
					Says = api.DescribeFuel(),
					MinimumGrade = api.ReadMinimumFuelGrade(),
					PermittedGrades = api.PermittedFuelGrades(),
					ProhibitedGrades = api.ProhibitedFuelGrades(),
					Revision = api.ReadFuelRevision(),
				},
				Folder = reader.Folder,
				Detail = TheDetailWhen(true),
			};
		}

		private WarrantyScope ThePublishedScope(DealershipCarAPI api, string system)
		{
			string model;

			if (!TheSpecificationIsThere(api, out model))
				return new WarrantyScope
				{
					HasData = true,
					Vin = api.ReadVin(),
					System = system,
					SpecificationKnown = false,
					Folder = reader.Folder,
					Detail = TheDetailWhen(false),
				};

			bool servicingPolicyPublished = api.IsServicingPolicyPublished();

			bool envelopeKnown = TheAssistantIsTold.TheVehiclesOwnRecordIsThere(api);
			DateTime askedAt = TheClockTheAssistantReads.TheInstantThisWasAsked();

			bool published = envelopeKnown && api.IsWarrantyEnvelopePublished();
			bool delivered = envelopeKnown && api.HasDeliveryBeenRecorded();
			bool measured = envelopeKnown && api.HasAnOdometerReading(askedAt);
			bool wholeRecord = published && delivered && measured;

			bool contradicts = false;
			bool standingCanBeEvaluated = false;
			string standing = "";
			string standingSays = "";
			string thisVehicleSays = "";

			if (envelopeKnown)
				try
				{
					thisVehicleSays = api.DescribeWarrantyEnvelope(askedAt);

					if (wholeRecord && system.Length > 0)
					{
						standingCanBeEvaluated =
							api.CanCoverageBeEvaluatedForThisVehicle(system, askedAt);
						standing = standingCanBeEvaluated
							? api.ReadCoverageStandingForThisVehicle(system, askedAt)
							: "";
						standingSays = api.DescribeCoverageForThisVehicle(system, askedAt);
					}
				}
				catch (Exception thrown) when (TheAssistantIsTold.TheRecordContradictsItself(thrown))
				{
					contradicts = true;
					standingCanBeEvaluated = false;
					standing = "";
					standingSays = "";
					thisVehicleSays = "";
				}

			return new WarrantyScope
			{
				HasData = true,
				Vin = api.ReadVin(),
				SpecificationKnown = true,
				Model = model,
				CurrentRevision = TheCurrentRevisionCode(api),
				System = system,
				SystemInTheCatalogue =
					system.Length > 0 && api.TheVocabularyIsTheManufacturers(system),
				Published = api.IsCoveragePublished(),
				Says = api.DescribeCoverageSummary(),
				Revision = api.ReadCoverageRevision(),
				CoveredSystems = api.CoveredSystems(),
				Exclusions = api.CoverageExclusions(),
				ServicingPolicyPublished = servicingPolicyPublished,
				ServicingPolicySays = api.DescribePublishedServicingElsewhereRule(),
				ServicingPolicyRevision = api.ReadServicingPolicyRevision(),
				EnvelopeKnown = envelopeKnown,
				AskedAt = askedAt.ToString("O"),
				EnvelopePublished = published,
				DeliveryRecorded = delivered,
				OdometerKnown = measured,
				ThisVehicleSays = thisVehicleSays,
				StandingCanBeEvaluated = standingCanBeEvaluated,
				Standing = standing,
				StandingSays = standingSays,
				RecordContradictsItself = contradicts,
				Folder = reader.Folder,
				Detail = TheAssistantIsTold.EverythingWorthSaying(
					TheDetailWhen(true),
					TheWordOnTheStanding(envelopeKnown, contradicts, wholeRecord, system),
					servicingPolicyPublished ? TheServicingRuleStandingIsNotInThisRecord : ""),
			};
		}

		/// <summary>
		/// Lo que hay que decirle al asistente sobre la mitad de ESTE vehiculo: que no esta en el
		/// registro, que el registro se contradice, que hay que nombrar un sistema, o que la posicion
		/// si esta y contra que se midio.
		/// </summary>
		private static string TheWordOnTheStanding(
			bool envelopeKnown, bool contradicts, bool wholeRecord, string system)
		{
			if (!envelopeKnown) return TheAssistantIsTold.TheAppIsOlderThanTheVehiclesOwnRecord;

			if (contradicts) return TheAssistantIsTold.TheRecordSaysTwoThingsThatCannotBothBeTrue;

			if (!wholeRecord) return TheStandingNeedsAFactTheRecordDoesNotHaveYet;

			return system.Length > 0 ? TheStandingIsHereAndItIsAsOfTheReading : NoSystemWasNamed;
		}

		private ServicePricing ThePublishedCostOf(
			DealershipCarAPI api, MaintenanceOperation operation)
		{
			string kind = operation.WireName();
			string model;

			if (!TheSpecificationIsThere(api, out model))
				return new ServicePricing
				{
					HasData = true,
					Vin = api.ReadVin(),
					Operation = kind,
					SpecificationKnown = false,
					Folder = reader.Folder,
					Detail = TheDetailWhen(false),
				};

			DateTime askedAt = TheClockTheAssistantReads.TheInstantThisWasAsked();

			PublishedPriceRecord record = api.PublishedServicePricesAsOf(askedAt);

			var prices = new List<PublishedPrice>();

			foreach (int row in Enumerable.Range(0, record.Operation.Length))
				if (record.Operation[row] == kind)
					prices.Add(new PublishedPrice
					{
						Milestone = record.Milestone[row],
						Says = record.Says[row],
						Revision = record.Revision[row],
					});

			bool intervalPublished = api.IsServiceIntervalPublished(kind);
			bool anyOnRecord = api.HasAnyPublishedServicePrice();

			return new ServicePricing
			{
				HasData = true,
				Vin = api.ReadVin(),
				SpecificationKnown = true,
				Model = model,
				CurrentRevision = TheCurrentRevisionCode(api),
				Operation = kind,
				IntervalPublished = intervalPublished,
				IntervalSays = api.DescribePublishedServiceInterval(kind),
				IntervalRevision = api.ReadServiceIntervalRevision(kind),
				AnyPriceOnRecord = anyOnRecord,
				PriceCountOnRecord = api.PublishedServicePriceCount(),
				Prices = prices,
				AskedAt = askedAt.ToString("O"),
				Folder = reader.Folder,
				Detail = TheAssistantIsTold.EverythingWorthSaying(
					TheDetailWhen(true),
					TheWordOnThePrices(anyOnRecord, prices.Count),
					intervalPublished ? TheIntervalStandingIsNotInThisRecord : ""),
			};
		}

		/// <summary>
		/// Si este journal lleva la especificacion. Un journal que escribio una app anterior a ella no
		/// tiene la global de raiz, y este proceso NO la puede crear: crearla seria escribir.
		/// </summary>
		private static bool TheSpecificationIsThere(DealershipCarAPI api, out string model)
		{
			try
			{
				model = api.ReadModel();
				return true;
			}
			catch (LanguageException)
			{
				model = "";
				return false;
			}
		}

		private static string TheCurrentRevisionCode(DealershipCarAPI api)
		{
			return api.HasASealedRevision() ? api.ReadCurrentRevisionCode() : "";
		}

		private static string TheInstantTheCurrentRevisionWasPublished(DealershipCarAPI api)
		{
			return api.HasASealedRevision()
				? api.ReadCurrentRevisionPublishedAt().ToString("O")
				: "";
		}

		private static string TheWordOnThePrices(bool anyPriceOnRecord, int howManyPrices)
		{
			if (howManyPrices > 0) return "";

			return anyPriceOnRecord ? NoPriceIsPublishedForThisOperation : NoPriceIsOnThisRecordAtAll;
		}

		private string TheDetailWhen(bool theSpecificationIsThere)
		{
			return TheAssistantIsTold.EverythingWorthSaying(
				theSpecificationIsThere ? "" : TheAppIsOlderThanTheSpecification,
				TheAssistantIsTold.MoreThanOneVehicle(reader));
		}

		private const string TheAppIsOlderThanTheSpecification =
			"Este registro lo escribió una versión anterior de la app, que no llevaba la "
			+ "especificación del fabricante en absoluto, así que aquí no hay nada sobre lo que lleva "
			+ "este modelo, cuál es el alcance de la garantía, ni cuánto cuesta un servicio. Su plan de "
			+ "mantenimiento está completo y al día: las otras tools siguen contestando enteras. Pídale "
			+ "a la persona que abra su app una vez y eso traerá la especificación; no se pierde nada, "
			+ "y no es un error.";

		private const string TheStandingIsHereAndItIsAsOfTheReading =
			"standingSays es dónde está parado ESTE vehículo frente al alcance publicado, compuesto "
			+ "por el registro mismo a partir del sobre que publicó el concesionario, la fecha en que se "
			+ "entregó el carro y el odómetro que trajo el feed: diga esa frase en vez de rearmarla. Se "
			+ "midió contra la lectura que tiene el registro, cuya fecha de observación está en "
			+ "thisVehicleSays, y no contra donde está el carro hoy: diga esa fecha, porque una lectura "
			+ "de hace meses deja la mitad de distancia exactamente así de vieja. Estar dentro del "
			+ "alcance y dentro del sobre a la vez sigue sin ser una promesa de que un reclamo se "
			+ "vaya a pagar.";

		private const string TheStandingNeedsAFactTheRecordDoesNotHaveYet =
			"Dónde está parado ESTE vehículo no está en standingSays, porque al menos uno de los tres "
			+ "hechos que necesita falta en el registro: el sobre de garantía, la fecha en que se "
			+ "entregó el carro, o una lectura del odómetro al instante preguntado o antes. Las dos "
			+ "mitades siguen contestando por separado y enteras: says es lo que el fabricante publicó "
			+ "sobre el alcance, y thisVehicleSays es lo que el registro tiene sobre este carro y "
			+ "nombra qué hecho falta con sus propias palabras. Cite las dos. Los tres hechos vienen "
			+ "del concesionario, así que ni la persona ni usted pueden aportar uno, y componer la posición "
			+ "contra un número adivinado declararía como medido algo que nadie midió.";

		private const string NoSystemWasNamed =
			"No se nombró ningún sistema, así que no se buscó la posición de ninguno. thisVehicleSays "
			+ "sigue contestando dónde está parado este vehículo frente a su propio sobre, que es la "
			+ "mitad que no depende de por qué sistema se pregunte. Para obtener la posición de un "
			+ "sistema, vuelva a llamar con un código de coveredSystems o de una exclusión.";

		private const string TheServicingRuleStandingIsNotInThisRecord =
			"La regla de servicio en otro taller está en esta respuesta tal como la publicó el "
			+ "fabricante. Dónde está parado este vehículo frente a ella no: eso necesita cuántos "
			+ "servicios ha tenido en otro lado, cuántos de esos llevan comprobante, y su cuenta de "
			+ "mantenimiento no hecho, y este registro guarda solo el último de los tres. Cite la regla "
			+ "y diga que la posición no está aquí; no deduzca cumplimiento del silencio.";

		private const string TheIntervalStandingIsNotInThisRecord =
			"El intervalo de servicio está en esta respuesta tal como lo publicó el fabricante. "
			+ "Cuánto le falta a este vehículo contra él no: el registro tiene el odómetro, pero la "
			+ "distancia que falta contra este intervalo no es un número que devuelva. Cite el "
			+ "intervalo y diga que dónde está parado el carro frente a él no está aquí; las tools de "
			+ "mantenimiento contestan cuándo toca el próximo servicio.";

		private const string NoPriceIsPublishedForThisOperation =
			"No hay ningún precio publicado para esta operación, y sí hay precios publicados para "
			+ "otras: a este registro nunca le dijeron uno para ella. Eso no quiere decir que el "
			+ "servicio sea gratis ni que no exista. Pregunte por otra operación para ver los que sí "
			+ "hay; no hay ningún hito que nombrar, porque los hitos los enumera esta misma respuesta.";

		private const string NoPriceIsOnThisRecordAtAll =
			"Este registro no lleva NINGÚN precio publicado, de ninguna operación: el fabricante no ha "
			+ "publicado precios en ninguna revisión sellada, o los publicó en una revisión que todavía "
			+ "está abierta. No es que el servicio sea gratis, y no es un hueco que usted pueda llenar "
			+ "con otro modelo ni con otro año. Los precios llegan por el feed del concesionario cuando "
			+ "la persona abre su app.";
	}

	/// <summary>Lo publicado sobre una clase de fluido, con la revision de la que sale.</summary>
	internal sealed class PublishedFluid
	{
		[JsonPropertyName("kind")]
		[Description("De qué fluido habla esta entrada.")]
		public string Kind { get; init; } = "";

		[JsonPropertyName("published")]
		[Description("false cuando nunca se publicó nada sobre este fluido para este modelo. Eso es "
			+ "una respuesta: no lo llene con otro modelo ni con otro año.")]
		public bool Published { get; init; }

		[JsonPropertyName("says")]
		[Description("Lo que publicó el fabricante, en una frase que compuso el registro mismo, con la "
			+ "revisión adentro. Diga esta frase; no la reescriba como consejo. Cuando no se publicó "
			+ "nada, lo dice con palabras.")]
		public string Says { get; init; } = "";

		[JsonPropertyName("revision")]
		[Description("La revisión de la ficha de la que salió esta respuesta. VACÍO quiere decir que no "
			+ "hay nada publicado, y ninguna revisión tiene el código en blanco.")]
		public string Revision { get; init; } = "";
	}

	/// <summary>Lo publicado sobre la llanta de una posicion, con la revision de la que sale.</summary>
	internal sealed class PublishedTyre
	{
		[JsonPropertyName("position")]
		[Description("De qué ruedas habla esta entrada.")]
		public string Position { get; init; } = "";

		[JsonPropertyName("published")]
		[Description("false cuando nunca se publicó nada sobre las llantas de esta posición.")]
		public bool Published { get; init; }

		[JsonPropertyName("says")]
		[Description("Medida, presión en frío y clasificaciones en una frase con su revisión. La "
			+ "presión lleva una unidad que nunca se convierte: dígala exactamente como viene.")]
		public string Says { get; init; } = "";

		[JsonPropertyName("revision")]
		[Description("La revisión de la que salió esta respuesta. VACÍO quiere decir que no hay nada "
			+ "publicado.")]
		public string Revision { get; init; } = "";
	}

	/// <summary>Lo publicado sobre el combustible: el minimo, los permitidos y los prohibidos.</summary>
	internal sealed class PublishedFuel
	{
		[JsonPropertyName("published")]
		[Description("false cuando nunca se publicó ningún grado de combustible para este modelo.")]
		public bool Published { get; init; }

		[JsonPropertyName("says")]
		[Description("Cada grado publicado con la postura que le puso el fabricante, en una frase con "
			+ "su revisión.")]
		public string Says { get; init; } = "";

		[JsonPropertyName("minimumGrade")]
		[Description("El grado más bajo que acepta el fabricante, que es como se pide el combustible "
			+ "en una bomba. VACÍO quiere decir que no se publicó ningún mínimo, no que valga "
			+ "cualquier grado.")]
		public string MinimumGrade { get; init; } = "";

		[JsonPropertyName("permittedGrades")]
		[Description("Los grados publicados como permitidos. Una lista vacía dice que no se publicó "
			+ "ninguno como permitido, que no es lo mismo que decir que todos lo están.")]
		public IReadOnlyList<string> PermittedGrades { get; init; } = new List<string>();

		[JsonPropertyName("prohibitedGrades")]
		[Description("Los grados publicados como prohibidos. Esta es la lista que vale la pena "
			+ "plantear primero: un grado prohibido callado es un motor dañado.")]
		public IReadOnlyList<string> ProhibitedGrades { get; init; } = new List<string>();

		[JsonPropertyName("revision")]
		[Description("La revisión de la que salió esta respuesta. VACÍO quiere decir que no hay nada "
			+ "publicado.")]
		public string Revision { get; init; } = "";
	}

	/// <summary>Un precio publicado, con el hito al que corresponde dicho como lo lee una persona.</summary>
	internal sealed class PublishedPrice
	{
		[JsonPropertyName("milestone")]
		[Description("El hito para el que es este precio, con su unidad como la lee una persona. Es el "
			+ "hito DICHO y no una forma que se pueda volver a pasar: el registro nunca convierte "
			+ "entre sus dos unidades de distancia, así que cítelo como viene.")]
		public string Milestone { get; init; } = "";

		[JsonPropertyName("says")]
		[Description("El monto con su moneda, la ventana en que el precio rige, si esa ventana está "
			+ "rigiendo a askedAt, y la revisión de la que salió. Diga la frase entera: un precio sin "
			+ "su vigencia es una promesa que la sucursal nunca hizo.")]
		public string Says { get; init; } = "";

		[JsonPropertyName("revision")]
		[Description("La revisión de la que salió este precio.")]
		public string Revision { get; init; } = "";
	}

	/// <summary>La ficha del fabricante entera: fluidos, llantas y combustible.</summary>
	internal sealed class SpecificationSheet
	{
		[JsonPropertyName("hasData")]
		[Description("false cuando la app todavía no ha registrado nada; entonces el resto viene "
			+ "vacío.")]
		public bool HasData { get; init; }

		[JsonPropertyName("vin")]
		[Description("El vehículo del que habla esta respuesta.")]
		public string Vin { get; init; } = "";

		[JsonPropertyName("specificationKnown")]
		[Description("false cuando este registro lo escribió una versión de la app anterior a la ficha "
			+ "del fabricante. Entonces no viene lleno nada de lo de abajo y quiere decir que NO se "
			+ "publicó nada: lea detail. Su plan de mantenimiento no se ve afectado.")]
		public bool SpecificationKnown { get; init; }

		[JsonPropertyName("model")]
		[Description("El modelo del que habla esta ficha. La ficha describe el modelo, no este carro "
			+ "en particular.")]
		public string Model { get; init; } = "";

		[JsonPropertyName("currentRevision")]
		[Description("La revisión sellada más reciente de la ficha. VACÍO quiere decir que todavía no "
			+ "se ha sellado ninguna, así que no se puede citar nada en absoluto. Un ítem suelto puede "
			+ "nombrar una revisión más vieja que esta: una revisión que corrige un ítem deja los demás "
			+ "en paz.")]
		public string CurrentRevision { get; init; } = "";

		[JsonPropertyName("currentRevisionPublishedAt")]
		[Description("El instante en que el fabricante publicó esa revisión, en UTC. Vacío cuando no "
			+ "hay ninguna sellada.")]
		public string CurrentRevisionPublishedAt { get; init; } = "";

		[JsonPropertyName("fluids")]
		[Description("Las seis clases de fluido, incluidas aquellas sobre las que no se publicó nada, "
			+ "que lo dicen. La ausencia se declara, no se saltea.")]
		public IReadOnlyList<PublishedFluid> Fluids { get; init; } = new List<PublishedFluid>();

		[JsonPropertyName("tyres")]
		[Description("Las tres posiciones de rueda, incluidas aquellas sobre las que no se publicó "
			+ "nada.")]
		public IReadOnlyList<PublishedTyre> Tyres { get; init; } = new List<PublishedTyre>();

		[JsonPropertyName("fuel")]
		[Description("Qué combustible acepta el modelo, y cuál no se le puede echar.")]
		public PublishedFuel Fuel { get; init; } = new PublishedFuel();

		[JsonPropertyName("folder")]
		[Description("La carpeta de la persona, donde su app guarda el registro de este vehículo.")]
		public string Folder { get; init; } = "";

		[JsonPropertyName("detail")]
		[Description("Vacío cuando todo está bien; si no, lo que a la persona le conviene hacer.")]
		public string Detail { get; init; } = "";
	}

	/// <summary>El alcance de garantia publicado, y donde cae un sistema dentro del catalogo.</summary>
	internal sealed class WarrantyScope
	{
		[JsonPropertyName("hasData")]
		[Description("false cuando la app todavía no ha registrado nada; entonces el resto viene "
			+ "vacío.")]
		public bool HasData { get; init; }

		[JsonPropertyName("vin")]
		[Description("El vehículo del que habla esta respuesta.")]
		public string Vin { get; init; } = "";

		[JsonPropertyName("specificationKnown")]
		[Description("false cuando este registro lo escribió una versión de la app anterior a la ficha "
			+ "del fabricante. Entonces no viene lleno nada de lo de abajo: lea detail.")]
		public bool SpecificationKnown { get; init; }

		[JsonPropertyName("model")]
		[Description("El modelo del que es este alcance de garantía.")]
		public string Model { get; init; } = "";

		[JsonPropertyName("currentRevision")]
		[Description("La revisión sellada más reciente de la ficha. VACÍO quiere decir que no hay "
			+ "ninguna sellada.")]
		public string CurrentRevision { get; init; } = "";

		[JsonPropertyName("system")]
		[Description("El código del sistema por el que se preguntó, exactamente como se pasó. Vacío "
			+ "cuando no se nombró ninguno y esto es el alcance entero.")]
		public string System { get; init; } = "";

		[JsonPropertyName("systemInTheCatalogue")]
		[Description("true cuando el alcance publicado nombra ese código de alguna de las dos formas: "
			+ "entre los sistemas que declara adentro o en una de sus exclusiones. false quiere decir "
			+ "que el alcance nunca lo menciona, que es 'no nos lo dijeron' y NO 'no está cubierto'. "
			+ "Nunca reporte false como una negativa de cobertura, y revise la ortografía antes de "
			+ "reportarlo siquiera: la coincidencia es exacta.")]
		public bool SystemInTheCatalogue { get; init; }

		[JsonPropertyName("published")]
		[Description("false cuando nunca se publicó ningún alcance de garantía para este modelo.")]
		public bool Published { get; init; }

		[JsonPropertyName("says")]
		[Description("El alcance publicado entero en una frase: los sistemas que están adentro, las "
			+ "exclusiones, y las palabras del fabricante mismo diciendo que esto es alcance y no la "
			+ "resolución de un reclamo. Diga también esa última parte.")]
		public string Says { get; init; } = "";

		[JsonPropertyName("revision")]
		[Description("La revisión de la que salió el alcance. VACÍO quiere decir que no hay nada "
			+ "publicado.")]
		public string Revision { get; init; } = "";

		[JsonPropertyName("coveredSystems")]
		[Description("Los códigos que el alcance declara ADENTRO. Estos son los códigos que hay que "
			+ "devolver como system, letra por letra. Que un código esté aquí no quiere decir que la "
			+ "garantía de este vehículo esté rigiendo: eso no está en este registro.")]
		public IReadOnlyList<string> CoveredSystems { get; init; } = new List<string>();

		[JsonPropertyName("exclusions")]
		[Description("Cada exclusión publicada con el sistema que nombra y la condición bajo la cual "
			+ "muerde. Una exclusión que el registro conoce y no se menciona es la peor respuesta "
			+ "posible a una pregunta de cobertura, así que léalas en voz alta.")]
		public IReadOnlyList<string> Exclusions { get; init; } = new List<string>();

		[JsonPropertyName("servicingPolicyPublished")]
		[Description("true cuando el fabricante publicó una regla sobre el servicio en otro taller y "
			+ "qué conserva la cobertura.")]
		public bool ServicingPolicyPublished { get; init; }

		[JsonPropertyName("servicingPolicySays")]
		[Description("La regla tal como la publicó el fabricante, con la revisión de la que salió, o "
			+ "que no hay ninguna publicada. Diga esta frase; es la respuesta, no el resumen de una. "
			+ "Dónde está parado ESTE vehículo frente a la regla no está aquí: lea detail.")]
		public string ServicingPolicySays { get; init; } = "";

		[JsonPropertyName("servicingPolicyRevision")]
		[Description("La revisión de la que salió esa regla. VACÍO quiere decir que no hay ninguna "
			+ "publicada.")]
		public string ServicingPolicyRevision { get; init; } = "";

		[JsonPropertyName("envelopeKnown")]
		[Description("false cuando este registro lo escribió una versión de la app anterior a lo que "
			+ "el concesionario registra sobre este vehículo. Entonces no viene lleno nada de la posición de "
			+ "este carro y solo contesta el alcance publicado: lea detail. NO quiere decir que el "
			+ "carro no tenga garantía.")]
		public bool EnvelopeKnown { get; init; }

		[JsonPropertyName("askedAt")]
		[Description("El instante en que se hizo esta pregunta, en UTC, y el instante al que se midió "
			+ "la posición. Todo lo de este vehículo aquí es a ese instante.")]
		public string AskedAt { get; init; } = "";

		[JsonPropertyName("envelopePublished")]
		[Description("true cuando el concesionario ha publicado un sobre de garantía para este vehículo. Uno "
			+ "de los tres hechos que necesita la posición. false no es 'sin garantía'.")]
		public bool EnvelopePublished { get; init; }

		[JsonPropertyName("deliveryRecorded")]
		[Description("true cuando la fecha en que se entregó el carro está en el registro. Uno de los "
			+ "tres hechos que necesita la posición; el plazo se cuenta desde esa fecha.")]
		public bool DeliveryRecorded { get; init; }

		[JsonPropertyName("odometerKnown")]
		[Description("true cuando el feed del concesionario ha traído una lectura observada a askedAt o "
			+ "antes. Uno de los tres hechos que necesita la posición; la mitad de distancia se mide "
			+ "contra ella.")]
		public bool OdometerKnown { get; init; }

		[JsonPropertyName("thisVehicleSays")]
		[Description("Dónde está parado este vehículo frente a SU PROPIO sobre de garantía, en una "
			+ "frase que compuso el registro: las dos mitades, cuál llegó primero, y el odómetro "
			+ "contra el que se midió con el día en que se observó esa lectura. Contesta incluso "
			+ "cuando falta un hecho, nombrando cuál, así que es la mitad que hay que citar siempre "
			+ "que standingSays venga vacío. Habla del sobre y no de ningún sistema en particular; "
			+ "is_my_car_still_under_warranty contesta esta pregunta por su cuenta con la misma frase.")]
		public string ThisVehicleSays { get; init; } = "";

		[JsonPropertyName("standingCanBeEvaluated")]
		[Description("true cuando la cobertura del sistema nombrado se pudo decidir para ESTE vehículo "
			+ "a askedAt. false cuando falta un hecho, cuando no se nombró ningún sistema, o cuando el "
			+ "límite del sobre y el odómetro llegaron en unidades distintas, que nunca se "
			+ "convierten.")]
		public bool StandingCanBeEvaluated { get; init; }

		[JsonPropertyName("standing")]
		[Description("Dónde está parado este vehículo en el sistema nombrado, en una palabra. VACÍO "
			+ "quiere decir que no se pudo decidir; aquí ninguna palabra quiere decir 'no se sabe', y "
			+ "por eso viene vacío en vez de adivinar una. Lea standingSays: esa es la respuesta, esto "
			+ "es solo su nombre.")]
		public string Standing { get; init; } = "";

		[JsonPropertyName("standingSays")]
		[Description("LA RESPUESTA COMPUESTA para el sistema nombrado: dónde está parado este "
			+ "vehículo, los límites que rigen, las exclusiones que aplican, y la revisión de la que "
			+ "salió. Diga esta frase. VACÍO quiere decir que no se pudo componer: entonces says y "
			+ "thisVehicleSays contestan cada uno su propia mitad entera, y detail dice qué hecho "
			+ "falta. Nunca arme un reemplazo a partir de los otros dos.")]
		public string StandingSays { get; init; } = "";

		[JsonPropertyName("recordContradictsItself")]
		[Description("true cuando dos hechos que el concesionario registró no pueden ser ciertos los dos a "
			+ "askedAt, así que no se pudo medir nada de este vehículo y thisVehicleSays y "
			+ "standingSays vienen vacíos. El alcance publicado sigue contestando. Es una falla del "
			+ "registro y no de la pregunta: lea detail.")]
		public bool RecordContradictsItself { get; init; }

		[JsonPropertyName("folder")]
		[Description("La carpeta de la persona, donde su app guarda el registro de este vehículo.")]
		public string Folder { get; init; } = "";

		[JsonPropertyName("detail")]
		[Description("Lo que esta respuesta no dice, y cualquier cosa que a la persona le convenga "
			+ "hacer. Léalo antes de contestar una pregunta de cobertura: ahí están los límites de la "
			+ "respuesta.")]
		public string Detail { get; init; } = "";
	}

	/// <summary>Lo publicado sobre el servicio de una operacion: si hay intervalo y que cuesta.</summary>
	internal sealed class ServicePricing
	{
		[JsonPropertyName("hasData")]
		[Description("false cuando la app todavía no ha registrado nada; entonces el resto viene "
			+ "vacío.")]
		public bool HasData { get; init; }

		[JsonPropertyName("vin")]
		[Description("El vehículo del que habla esta respuesta.")]
		public string Vin { get; init; } = "";

		[JsonPropertyName("specificationKnown")]
		[Description("false cuando este registro lo escribió una versión de la app anterior a la ficha "
			+ "del fabricante. Entonces no viene lleno nada de lo de abajo: lea detail.")]
		public bool SpecificationKnown { get; init; }

		[JsonPropertyName("model")]
		[Description("El modelo para el que están publicados estos números.")]
		public string Model { get; init; } = "";

		[JsonPropertyName("currentRevision")]
		[Description("La revisión sellada más reciente de la ficha. VACÍO quiere decir que no hay "
			+ "ninguna sellada.")]
		public string CurrentRevision { get; init; } = "";

		[JsonPropertyName("operation")]
		[Description("La operación de mantenimiento de la que habla esta respuesta.")]
		public string Operation { get; init; } = "";

		[JsonPropertyName("intervalPublished")]
		[Description("true cuando el fabricante publicó cada cuánto se hace esta operación.")]
		public bool IntervalPublished { get; init; }

		[JsonPropertyName("intervalSays")]
		[Description("Cada cuánto publica el fabricante que se hace esta operación, con la revisión de "
			+ "la que salió, o que no hay ningún intervalo publicado. Diga esta frase; es la "
			+ "respuesta, no el resumen de una. Cuánto le falta a ESTE vehículo no está aquí: lea "
			+ "detail.")]
		public string IntervalSays { get; init; } = "";

		[JsonPropertyName("intervalRevision")]
		[Description("La revisión de la que salió ese intervalo. VACÍO quiere decir que no hay ninguno "
			+ "publicado.")]
		public string IntervalRevision { get; init; } = "";

		[JsonPropertyName("anyPriceOnRecord")]
		[Description("true si alguna revisión sellada publicó algún precio, de cualquier operación. Es "
			+ "lo que separa las dos razones por las que prices puede venir vacía: en false a este "
			+ "registro nunca le dijeron ningún precio de nada.")]
		public bool AnyPriceOnRecord { get; init; }

		[JsonPropertyName("priceCountOnRecord")]
		[Description("Cuántos precios publicados hay en el registro ENTERO, de todas las operaciones "
			+ "juntas. NO es cuántos trae prices: esas son solo las de la operación por la que se "
			+ "preguntó. No toma el instante de la pregunta, a propósito, porque el juego de precios "
			+ "publicados no depende de él: solo su vigencia.")]
		public int PriceCountOnRecord { get; init; }

		[JsonPropertyName("prices")]
		[Description("Una entrada por cada precio publicado PARA ESTA OPERACIÓN, uno por hito, de la "
			+ "revisión más nueva a la más vieja. Los hitos los enumera el registro y nadie los "
			+ "nombra. Una lista vacía es una respuesta: lea anyPriceOnRecord y detail para saber cuál "
			+ "de los dos motivos es.")]
		public IReadOnlyList<PublishedPrice> Prices { get; init; } = new List<PublishedPrice>();

		[JsonPropertyName("askedAt")]
		[Description("El instante en que se hizo esta pregunta, en UTC, y el instante contra el que se "
			+ "juzgó cada vigencia de prices. Todo lo de aquí es a ese instante.")]
		public string AskedAt { get; init; } = "";

		[JsonPropertyName("folder")]
		[Description("La carpeta de la persona, donde su app guarda el registro de este vehículo.")]
		public string Folder { get; init; } = "";

		[JsonPropertyName("detail")]
		[Description("Lo que esta respuesta no dice, y cualquier cosa que a la persona le convenga "
			+ "hacer.")]
		public string Detail { get; init; } = "";
	}
}
