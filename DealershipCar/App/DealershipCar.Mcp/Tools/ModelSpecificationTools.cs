using System.ComponentModel;
using System.Text.Json.Serialization;
using DealershipCar.App;
using ModelContextProtocol.Server;
using Puppeteer;

namespace DealershipCar.Mcp.Tools
{
	/// <summary>
	/// Las tres preguntas del asistente sobre lo que el FABRICANTE publico del modelo de este
	/// vehiculo. Solo lectura, igual que las de la agenda: ninguna performa un comando.
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
		[Description("Answers what the manufacturer published that this person's vehicle takes: the "
			+ "engine oil and the five other fluids with their grade, capacity and change interval; "
			+ "the size and cold pressure of the tyres at each position; and the fuel grades, split "
			+ "into the minimum, the permitted and the prohibited ones. Use it for 'what oil does my "
			+ "car take?', 'what pressure should my tyres be?', 'can I put regular in it?'. It reports "
			+ "the whole spec sheet in one read, so prefer it over asking item by item. Every answer "
			+ "carries the revision of the spec sheet it came from - say that revision, because a "
			+ "figure without one is a rumour and a later revision may have changed it. An item whose "
			+ "published is false was never published for this model: that is an answer, not a gap, "
			+ "and it must never be filled from a similar model or a previous model year. Pressures, "
			+ "capacities and distances carry a unit that is never converted - say the value with the "
			+ "unit exactly as it comes. Of the three fuel lists the prohibited one is the one worth "
			+ "raising first.")]
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
		[Description("Answers what the manufacturer published about the SCOPE of the warranty on this "
			+ "person's model: which systems the scope declares inside, which exclusions it published "
			+ "and the condition under which each one bites, and whether a system the person names is "
			+ "in the manufacturer's catalogue at all. Use it for 'is my gearbox under warranty?' or "
			+ "'what does the warranty not cover?'. "
			+ "IT STATES SCOPE AND NEVER ADJUDICATES A CLAIM. Whether a repair gets paid is the "
			+ "manufacturer's to decide and nothing here says it will be, so never turn a system that "
			+ "is inside the scope into 'you are covered'. "
			+ "AND IT CANNOT SAY WHERE THIS VEHICLE STANDS against that scope. Deciding that needs "
			+ "the vehicle's warranty envelope, its delivery date, its odometer at delivery and its "
			+ "odometer today, and this record keeps none of the four. Say the scope, say that the "
			+ "standing is not in this record, and do not estimate it. "
			+ "'WE WERE NEVER TOLD ABOUT IT' IS NOT 'IT IS NOT COVERED', and collapsing the two is the "
			+ "failure worth avoiding: systemInTheCatalogue false means the published scope never "
			+ "names that system, which says nothing either way about whether the manufacturer covers "
			+ "it. A code listed in coveredSystems is one the scope declares inside; a code that "
			+ "appears only among the exclusions is one the scope names and leaves out. "
			+ "The system code is the MANUFACTURER'S vocabulary and not a list this app owns, so take "
			+ "it from coveredSystems or from an exclusion and pass it letter for letter: the match is "
			+ "exact, and a code spelled or cased differently answers 'never told about it'. Do not "
			+ "invent a code, and call the tool with no system at all to see the whole published "
			+ "scope first.")]
		public WarrantyScope DoesTheWarrantyCover(
			[Description("The manufacturer's code for the system being asked about, copied letter for "
				+ "letter from coveredSystems or from an exclusion. Leave it out to read the whole "
				+ "published scope, which is where the codes come from.")]
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
		[Description("Answers what the manufacturer published about servicing ONE maintenance "
			+ "operation on this model: whether a service interval was published for it, and what the "
			+ "service costs at a milestone the person names, with the validity of that price and "
			+ "whether the validity is in force at the instant the question was asked. Use it for "
			+ "'how much is the 10,000 service?' or 'is that price still good?'. "
			+ "MILESTONEDISTANCE IS THE MILESTONE THE PERSON NAMED. Leave it out if they did not name "
			+ "one and ask them which service they mean: this record does not list the milestones the "
			+ "manufacturer published, so a guessed milestone comes back with no price and that reads "
			+ "as 'the manufacturer never published one', which would be wrong. The milestone is "
			+ "looked up in each of the two distance units this record uses and answered in the unit "
			+ "it was published in - nothing is ever converted between them, so say the unit that "
			+ "comes back rather than assuming which one the person meant. "
			+ "A PRICE IS NEVER STATED WITHOUT ITS VALIDITY: the sentence carries the window and "
			+ "whether askedAt falls inside it. A stale price presented as current is a promise the "
			+ "branch never made. "
			+ "The published service interval's own numbers are not in this answer - quoting them "
			+ "needs how far this vehicle still has to go, and this record does not keep the odometer. "
			+ "The fluid change intervals are published as part of the fluid itself, so "
			+ "what_my_car_takes carries those.")]
		public ServicePricing WhatTheServiceCosts(
			[Description("Which maintenance operation the person is asking about.")]
			MaintenanceOperation operation,
			[Description("The service milestone the person named, such as 10000. Leave it out when "
				+ "they did not name one; do not guess a milestone.")]
			int milestoneDistance = 0)
		{
			return reader.Read(
				api => ThePublishedCostOf(api, operation, milestoneDistance),
				() => new ServicePricing
				{
					HasData = false,
					Operation = operation.WireName(),
					MilestoneAsked = milestoneDistance,
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
				ServicingPolicyRevision = api.ReadServicingPolicyRevision(),
				Folder = reader.Folder,
				Detail = TheAssistantIsTold.EverythingWorthSaying(
					TheDetailWhen(true),
					TheStandingIsNotInThisRecord,
					servicingPolicyPublished ? TheServicingRuleIsPublishedButNotQuotable : ""),
			};
		}

		private ServicePricing ThePublishedCostOf(
			DealershipCarAPI api, MaintenanceOperation operation, int milestoneDistance)
		{
			string kind = operation.WireName();
			string model;

			if (!TheSpecificationIsThere(api, out model))
				return new ServicePricing
				{
					HasData = true,
					Vin = api.ReadVin(),
					Operation = kind,
					MilestoneAsked = milestoneDistance,
					SpecificationKnown = false,
					Folder = reader.Folder,
					Detail = TheDetailWhen(false),
				};

			DateTime askedAt = TheInstantThisWasAsked();
			var prices = new List<PublishedPrice>();

			if (milestoneDistance > 0)
				foreach (string unit in TheClosedSetsOfTheSheet.DistanceUnits)
					if (api.IsServicePricePublished(kind, milestoneDistance, unit))
						prices.Add(new PublishedPrice
						{
							MilestoneDistance = milestoneDistance,
							MilestoneUnit = unit,
							Says = api.DescribeServicePrice(
								kind, milestoneDistance, unit, askedAt),
							Revision = api.ReadServicePriceRevision(kind, milestoneDistance, unit),
						});

			bool intervalPublished = api.IsServiceIntervalPublished(kind);

			return new ServicePricing
			{
				HasData = true,
				Vin = api.ReadVin(),
				SpecificationKnown = true,
				Model = model,
				CurrentRevision = TheCurrentRevisionCode(api),
				Operation = kind,
				IntervalPublished = intervalPublished,
				IntervalRevision = api.ReadServiceIntervalRevision(kind),
				MilestoneAsked = milestoneDistance,
				Prices = prices,
				AskedAt = askedAt.ToString("O"),
				Folder = reader.Folder,
				Detail = TheAssistantIsTold.EverythingWorthSaying(
					TheDetailWhen(true),
					TheWordOnThePrices(milestoneDistance, prices.Count),
					intervalPublished ? TheIntervalIsPublishedButNotQuotable : ""),
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

		/// <summary>
		/// El instante en que se hizo la pregunta, en UTC y truncado al segundo, que es la resolucion
		/// con la que el journal guarda un instante. Una vigencia se dice respecto de un instante o no
		/// se dice.
		/// </summary>
		private static DateTime TheInstantThisWasAsked()
		{
			DateTime now = DateTime.UtcNow;

			return new DateTime(
				now.Ticks - now.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);
		}

		private static string TheWordOnThePrices(int milestoneDistance, int howManyPrices)
		{
			if (milestoneDistance <= 0) return NoMilestoneWasNamed;

			return howManyPrices > 0 ? "" : NothingIsPublishedAtThatMilestone;
		}

		private string TheDetailWhen(bool theSpecificationIsThere)
		{
			return TheAssistantIsTold.EverythingWorthSaying(
				theSpecificationIsThere ? "" : TheAppIsOlderThanTheSpecification,
				TheAssistantIsTold.MoreThanOneVehicle(reader));
		}

		private const string TheAppIsOlderThanTheSpecification =
			"This record was written by an older version of the app, which did not carry the "
			+ "manufacturer's specification at all, so there is nothing here about what this model "
			+ "takes, what the warranty's scope is, or what a service costs. Their maintenance "
			+ "schedule is complete and current - the other tools still answer in full. Ask the "
			+ "person to open their app once and it will bring the specification in; nothing is lost, "
			+ "and it is not an error.";

		private const string TheStandingIsNotInThisRecord =
			"This answer is the published SCOPE and not this vehicle's standing against it. Deciding "
			+ "the standing needs the warranty envelope, the delivery date and the odometer readings "
			+ "at delivery and today, and this record keeps none of them - so say what the scope "
			+ "covers and say that where this car stands is not in it. Do not estimate it, and do not "
			+ "report a system that is inside the scope as a claim that will be paid.";

		private const string TheServicingRuleIsPublishedButNotQuotable =
			"The manufacturer also published a rule about servicing elsewhere and what preserves "
			+ "cover, and its text is not in this answer: stating it means pairing it with how many "
			+ "services this vehicle has had elsewhere, and this record does not keep that count.";

		private const string TheIntervalIsPublishedButNotQuotable =
			"A service interval was published for this operation and its numbers are not in this "
			+ "answer: stating them means pairing them with how far this vehicle still has to go, and "
			+ "this record does not keep the odometer. Say that an interval exists and which revision "
			+ "it came from, not what it says.";

		private const string NoMilestoneWasNamed =
			"No milestone was named, so no price was looked up. Ask the person which service they "
			+ "mean - the 10,000 one, the 20,000 one - because this record cannot list the milestones "
			+ "the manufacturer published and guessing one would come back as 'no price published'.";

		private const string NothingIsPublishedAtThatMilestone =
			"Nothing is published at that milestone in either distance unit. That means this record "
			+ "was never told a price for it, not that the service is free and not that it does not "
			+ "exist; the milestone the manufacturer did publish may simply be a different number.";
	}

	/// <summary>Lo publicado sobre una clase de fluido, con la revision de la que sale.</summary>
	internal sealed class PublishedFluid
	{
		[JsonPropertyName("kind")]
		[Description("Which fluid this entry is about.")]
		public string Kind { get; init; } = "";

		[JsonPropertyName("published")]
		[Description("false when nothing was ever published about this fluid for this model. That is "
			+ "an answer: do not fill it from another model or another year.")]
		public bool Published { get; init; }

		[JsonPropertyName("says")]
		[Description("What the manufacturer published, in one sentence composed by the record itself, "
			+ "with the revision inside it. Say this sentence; do not reword it into advice. When "
			+ "nothing was published it says so in words.")]
		public string Says { get; init; } = "";

		[JsonPropertyName("revision")]
		[Description("The revision of the spec sheet this answer came from. EMPTY means nothing is "
			+ "published, and no revision is ever blank.")]
		public string Revision { get; init; } = "";
	}

	/// <summary>Lo publicado sobre la llanta de una posicion, con la revision de la que sale.</summary>
	internal sealed class PublishedTyre
	{
		[JsonPropertyName("position")]
		[Description("Which wheels this entry is about.")]
		public string Position { get; init; } = "";

		[JsonPropertyName("published")]
		[Description("false when nothing was ever published about the tyres at this position.")]
		public bool Published { get; init; }

		[JsonPropertyName("says")]
		[Description("Size, cold pressure and ratings in one sentence with its revision. The pressure "
			+ "carries a unit that is never converted - say it exactly as it comes.")]
		public string Says { get; init; } = "";

		[JsonPropertyName("revision")]
		[Description("The revision this answer came from. EMPTY means nothing is published.")]
		public string Revision { get; init; } = "";
	}

	/// <summary>Lo publicado sobre el combustible: el minimo, los permitidos y los prohibidos.</summary>
	internal sealed class PublishedFuel
	{
		[JsonPropertyName("published")]
		[Description("false when no fuel grade was ever published for this model.")]
		public bool Published { get; init; }

		[JsonPropertyName("says")]
		[Description("Every published grade with the manufacturer's standing on it, in one sentence "
			+ "with its revision.")]
		public string Says { get; init; } = "";

		[JsonPropertyName("minimumGrade")]
		[Description("The lowest grade the manufacturer accepts, which is how fuel is asked for at a "
			+ "pump. EMPTY means no minimum was published - not that any grade will do.")]
		public string MinimumGrade { get; init; } = "";

		[JsonPropertyName("permittedGrades")]
		[Description("The grades published as permitted. An empty list says none was published as "
			+ "permitted, which is not the same as saying every grade is.")]
		public IReadOnlyList<string> PermittedGrades { get; init; } = new List<string>();

		[JsonPropertyName("prohibitedGrades")]
		[Description("The grades published as prohibited. This is the list worth raising first: a "
			+ "prohibited grade kept quiet is a damaged engine.")]
		public IReadOnlyList<string> ProhibitedGrades { get; init; } = new List<string>();

		[JsonPropertyName("revision")]
		[Description("The revision this answer came from. EMPTY means nothing is published.")]
		public string Revision { get; init; } = "";
	}

	/// <summary>Un precio publicado para un hito, en la unidad en que se publico.</summary>
	internal sealed class PublishedPrice
	{
		[JsonPropertyName("milestoneDistance")]
		[Description("The milestone this price is for.")]
		public int MilestoneDistance { get; init; }

		[JsonPropertyName("milestoneUnit")]
		[Description("The distance unit the manufacturer published that milestone in. Say it: the "
			+ "record never converts between the two units, so the number alone is not the answer.")]
		public string MilestoneUnit { get; init; } = "";

		[JsonPropertyName("says")]
		[Description("The amount with its currency, the window the price is valid in, whether that "
			+ "window is in force at askedAt, and the revision it came from. Say the whole sentence: "
			+ "a price without its validity is a promise the branch never made.")]
		public string Says { get; init; } = "";

		[JsonPropertyName("revision")]
		[Description("The revision this price came from.")]
		public string Revision { get; init; } = "";
	}

	/// <summary>La ficha del fabricante entera: fluidos, llantas y combustible.</summary>
	internal sealed class SpecificationSheet
	{
		[JsonPropertyName("hasData")]
		[Description("false when the app has not recorded anything yet; then the rest comes empty.")]
		public bool HasData { get; init; }

		[JsonPropertyName("vin")]
		[Description("The vehicle this answer is about.")]
		public string Vin { get; init; } = "";

		[JsonPropertyName("specificationKnown")]
		[Description("false when this record was written by a version of the app older than the "
			+ "manufacturer's spec sheet. Then nothing below is filled and it means NOTHING was "
			+ "published - read detail. Their maintenance schedule is unaffected.")]
		public bool SpecificationKnown { get; init; }

		[JsonPropertyName("model")]
		[Description("The model this spec sheet is about. The sheet describes the model, not this "
			+ "particular car.")]
		public string Model { get; init; } = "";

		[JsonPropertyName("currentRevision")]
		[Description("The most recent sealed revision of the spec sheet. EMPTY means none has been "
			+ "sealed yet, so nothing at all can be quoted. An individual item may name an older "
			+ "revision than this one: a revision that corrects one item leaves the rest alone.")]
		public string CurrentRevision { get; init; } = "";

		[JsonPropertyName("currentRevisionPublishedAt")]
		[Description("The instant the manufacturer published that revision, in UTC. Empty when none "
			+ "is sealed.")]
		public string CurrentRevisionPublishedAt { get; init; } = "";

		[JsonPropertyName("fluids")]
		[Description("The six kinds of fluid, including the ones nothing was published about, which "
			+ "say so. Absence is stated, not skipped.")]
		public IReadOnlyList<PublishedFluid> Fluids { get; init; } = new List<PublishedFluid>();

		[JsonPropertyName("tyres")]
		[Description("The three wheel positions, including the ones nothing was published about.")]
		public IReadOnlyList<PublishedTyre> Tyres { get; init; } = new List<PublishedTyre>();

		[JsonPropertyName("fuel")]
		[Description("What fuel the model accepts, and what it must not be given.")]
		public PublishedFuel Fuel { get; init; } = new PublishedFuel();

		[JsonPropertyName("folder")]
		[Description("The person's folder, where their app keeps this vehicle's record.")]
		public string Folder { get; init; } = "";

		[JsonPropertyName("detail")]
		[Description("Empty when everything is fine; otherwise what the person had better do.")]
		public string Detail { get; init; } = "";
	}

	/// <summary>El alcance de garantia publicado, y donde cae un sistema dentro del catalogo.</summary>
	internal sealed class WarrantyScope
	{
		[JsonPropertyName("hasData")]
		[Description("false when the app has not recorded anything yet; then the rest comes empty.")]
		public bool HasData { get; init; }

		[JsonPropertyName("vin")]
		[Description("The vehicle this answer is about.")]
		public string Vin { get; init; } = "";

		[JsonPropertyName("specificationKnown")]
		[Description("false when this record was written by a version of the app older than the "
			+ "manufacturer's spec sheet. Then nothing below is filled - read detail.")]
		public bool SpecificationKnown { get; init; }

		[JsonPropertyName("model")]
		[Description("The model whose warranty scope this is.")]
		public string Model { get; init; } = "";

		[JsonPropertyName("currentRevision")]
		[Description("The most recent sealed revision of the spec sheet. EMPTY means none is sealed.")]
		public string CurrentRevision { get; init; } = "";

		[JsonPropertyName("system")]
		[Description("The system code that was asked about, exactly as it was passed. Empty when none "
			+ "was named and this is the whole scope.")]
		public string System { get; init; } = "";

		[JsonPropertyName("systemInTheCatalogue")]
		[Description("true when the published scope names that code at all - either among the systems "
			+ "it declares inside or in one of its exclusions. false means the scope never mentions "
			+ "it, which is 'we were never told about it' and NOT 'it is not covered'. Never report "
			+ "false as a refusal of cover, and check the spelling before reporting it at all: the "
			+ "match is exact.")]
		public bool SystemInTheCatalogue { get; init; }

		[JsonPropertyName("published")]
		[Description("false when no warranty scope was ever published for this model.")]
		public bool Published { get; init; }

		[JsonPropertyName("says")]
		[Description("The whole published scope in one sentence - the systems inside it, the "
			+ "exclusions, and the manufacturer's own words that this is scope and not the resolution "
			+ "of a claim. Say that last part too.")]
		public string Says { get; init; } = "";

		[JsonPropertyName("revision")]
		[Description("The revision the scope came from. EMPTY means nothing is published.")]
		public string Revision { get; init; } = "";

		[JsonPropertyName("coveredSystems")]
		[Description("The codes the scope declares INSIDE it. These are the codes to pass back as "
			+ "system, letter for letter. A code being here does not mean this vehicle's warranty is "
			+ "in force - that is not in this record.")]
		public IReadOnlyList<string> CoveredSystems { get; init; } = new List<string>();

		[JsonPropertyName("exclusions")]
		[Description("Every published exclusion with the system it names and the condition under "
			+ "which it bites. An exclusion the record knows about and does not mention is the worst "
			+ "possible answer to a coverage question, so read them out.")]
		public IReadOnlyList<string> Exclusions { get; init; } = new List<string>();

		[JsonPropertyName("servicingPolicyPublished")]
		[Description("true when the manufacturer published a rule about servicing elsewhere and what "
			+ "preserves cover. Its text is not in this answer - read detail.")]
		public bool ServicingPolicyPublished { get; init; }

		[JsonPropertyName("servicingPolicyRevision")]
		[Description("The revision that rule came from. EMPTY means none is published.")]
		public string ServicingPolicyRevision { get; init; } = "";

		[JsonPropertyName("folder")]
		[Description("The person's folder, where their app keeps this vehicle's record.")]
		public string Folder { get; init; } = "";

		[JsonPropertyName("detail")]
		[Description("What this answer does not say, and anything the person had better do. Read it "
			+ "before answering a coverage question: it is where the limits of the answer are.")]
		public string Detail { get; init; } = "";
	}

	/// <summary>Lo publicado sobre el servicio de una operacion: si hay intervalo y que cuesta.</summary>
	internal sealed class ServicePricing
	{
		[JsonPropertyName("hasData")]
		[Description("false when the app has not recorded anything yet; then the rest comes empty.")]
		public bool HasData { get; init; }

		[JsonPropertyName("vin")]
		[Description("The vehicle this answer is about.")]
		public string Vin { get; init; } = "";

		[JsonPropertyName("specificationKnown")]
		[Description("false when this record was written by a version of the app older than the "
			+ "manufacturer's spec sheet. Then nothing below is filled - read detail.")]
		public bool SpecificationKnown { get; init; }

		[JsonPropertyName("model")]
		[Description("The model these figures are published for.")]
		public string Model { get; init; } = "";

		[JsonPropertyName("currentRevision")]
		[Description("The most recent sealed revision of the spec sheet. EMPTY means none is sealed.")]
		public string CurrentRevision { get; init; } = "";

		[JsonPropertyName("operation")]
		[Description("The maintenance operation this answer is about.")]
		public string Operation { get; init; } = "";

		[JsonPropertyName("intervalPublished")]
		[Description("true when the manufacturer published how often this operation is done. Its "
			+ "numbers are not in this answer - read detail.")]
		public bool IntervalPublished { get; init; }

		[JsonPropertyName("intervalRevision")]
		[Description("The revision that interval came from. EMPTY means none is published.")]
		public string IntervalRevision { get; init; } = "";

		[JsonPropertyName("milestoneAsked")]
		[Description("The milestone that was looked up. ZERO means none was named and no price was "
			+ "looked up at all - that is not 'the price is zero'.")]
		public int MilestoneAsked { get; init; }

		[JsonPropertyName("prices")]
		[Description("One entry per distance unit in which a price is published at that milestone, "
			+ "usually one. An empty list means no price is published there - read detail for which "
			+ "of the two reasons it is.")]
		public IReadOnlyList<PublishedPrice> Prices { get; init; } = new List<PublishedPrice>();

		[JsonPropertyName("askedAt")]
		[Description("The instant this question was asked, in UTC, and the instant every validity in "
			+ "prices was judged against. Everything here is as of that instant.")]
		public string AskedAt { get; init; } = "";

		[JsonPropertyName("folder")]
		[Description("The person's folder, where their app keeps this vehicle's record.")]
		public string Folder { get; init; } = "";

		[JsonPropertyName("detail")]
		[Description("What this answer does not say, and anything the person had better do.")]
		public string Detail { get; init; } = "";
	}
}
