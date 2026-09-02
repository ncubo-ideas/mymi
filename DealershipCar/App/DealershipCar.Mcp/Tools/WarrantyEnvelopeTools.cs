using System.ComponentModel;
using System.Text.Json.Serialization;
using DealershipCar.App;
using ModelContextProtocol.Server;

namespace DealershipCar.Mcp.Tools
{
	/// <summary>
	/// La pregunta del asistente sobre el sobre de garantia que LA AGENCIA publico para ESTE
	/// vehiculo: si todavia esta adentro y cuanto le queda de cada mitad. Solo lectura, y a nadie se
	/// le pide un dato del carro: los siete hechos salen del registro de la persona.
	/// </summary>
	[McpServerToolType]
	internal sealed class WarrantyEnvelopeTools
	{
		private readonly JournalReader reader;

		public WarrantyEnvelopeTools(JournalReader reader)
		{
			ArgumentNullException.ThrowIfNull(reader);

			this.reader = reader;
		}

		[McpServerTool(Name = "is_my_car_still_under_warranty", UseStructuredContent = true)]
		[Description("Answers whether this person's vehicle is still inside the warranty envelope "
			+ "the dealership published for it, and how much of each half is left. Use it for 'is my "
			+ "car still under warranty?', 'until when am I covered?', 'how much warranty do I have "
			+ "left?'. Everything it needs comes out of the person's own record - the envelope, the "
			+ "date the car was handed over, the odometer reading taken that day and the latest one "
			+ "the dealership's feed has brought - so never ask them for a mileage or a date, and "
			+ "never accept one to feed back in. "
			+ "AN ENVELOPE HAS TWO HALVES AND BOTH ARE ALWAYS SAID: a term in months and a distance. "
			+ "Cover ends when the FIRST of the two runs out, so quoting only the comfortable half is "
			+ "the way to give someone a wrong answer that sounds right. Which half will arrive first "
			+ "is deliberately not predicted - that would need a rate this record does not have - so "
			+ "say both and say that it depends on how much they drive. "
			+ "IT STATES STANDING AND NEVER ADJUDICATES A CLAIM. Being inside the envelope is not a "
			+ "promise that a repair gets paid; that is the manufacturer's to decide. "
			+ "says IS THE ANSWER. It is one sentence composed by the record itself and it already "
			+ "carries the standing, both halves, which one arrived first, the envelope it was "
			+ "measured against with its publication date, and the odometer it was measured against "
			+ "with the day that reading was observed. Say it rather than rebuilding it out of the "
			+ "fields, and say the observation date: if it is months old, the distance half is as old "
			+ "as it is, and presenting it as today's is the mistake worth avoiding. "
			+ "WHEN SOMETHING IS MISSING IT IS NAMED, NOT ESTIMATED. The says sentence words the gap "
			+ "itself and names which fact would decide it; the dealership is who records these, never "
			+ "the person and never you. An envelope that was never published is not a car without a "
			+ "warranty. "
			+ "For what the MANUFACTURER published about the warranty's scope - which systems are in "
			+ "it and which exclusions bite - use does_the_warranty_cover; that one also answers "
			+ "where this vehicle stands for a system the person names.")]
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
				says = api.DescribeWarrantyEnvelope(askedAt);
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
				PublishedSays = api.DescribePublishedWarrantyEnvelope(),
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
			"Part of what decides this is not in the record: the envelope itself, or the date the car "
			+ "was handed over. says names which one in the record's own words - quote it. The "
			+ "dealership is who states both of them, by the same feed that brings everything else "
			+ "here, so the person cannot supply them and neither can you. An envelope that is not "
			+ "recorded is NOT a vehicle without a warranty, and reporting it as one is the mistake "
			+ "worth avoiding here. Tell them to ask their dealership to record it.";

		private const string NoReadingHasArrivedForTheDistanceHalf =
			"The envelope and the delivery are both recorded, but no odometer reading has arrived at "
			+ "or before the instant asked, so only the time half could be measured. Say the time "
			+ "half and say that the distance half is unmeasured - not that it is fine, and not zero. "
			+ "The reading comes from the dealership's feed when the person opens their app.";

		private const string TheDistanceHalfIsAsOldAsItsReading =
			"The distance half was measured against the reading the record holds, taken on the date in "
			+ "odometerObservedAt - not against where the car is today. Say that date. If it is old, "
			+ "the distance left is at least that stale and the car may already be past the limit; ask "
			+ "the person to open their app so the feed brings a current reading.";
	}

	/// <summary>
	/// Donde esta parado este vehiculo contra el sobre que la agencia le publico, con los hechos del
	/// registro contra los que se midio.
	/// </summary>
	internal sealed class WarrantyStanding
	{
		[JsonPropertyName("hasData")]
		[Description("false when the app has not recorded anything yet; then the rest comes empty.")]
		public bool HasData { get; init; }

		[JsonPropertyName("vin")]
		[Description("The vehicle this answer is about.")]
		public string Vin { get; init; } = "";

		[JsonPropertyName("envelopeKnown")]
		[Description("false when this record was written by a version of the app older than what the "
			+ "dealership records about this vehicle. Then nothing below is filled - read detail. It "
			+ "does NOT mean the car has no warranty.")]
		public bool EnvelopeKnown { get; init; }

		[JsonPropertyName("askedAt")]
		[Description("The instant this question was asked, in UTC. Every answer here is as of that "
			+ "instant and not as of any other.")]
		public string AskedAt { get; init; } = "";

		[JsonPropertyName("envelopePublished")]
		[Description("true when the dealership has published a warranty envelope for this vehicle. "
			+ "false means none is recorded here - which is not the same as the car having no "
			+ "warranty, and must never be reported as 'you are out of warranty'.")]
		public bool EnvelopePublished { get; init; }

		[JsonPropertyName("publishedSays")]
		[Description("The envelope as the dealership published it - both limits, their unit and the "
			+ "date it was published - and nothing about this vehicle. It answers even when nothing "
			+ "is published, so it is always quotable, and it is the half to quote when the standing "
			+ "cannot be measured.")]
		public string PublishedSays { get; init; } = "";

		[JsonPropertyName("publishedAt")]
		[Description("When the dealership published the envelope that governs. EMPTY means none is "
			+ "published. A dealership can publish a new envelope, and a later one applies from its "
			+ "own date forward, so this date is the provenance of every number here.")]
		public string PublishedAt { get; init; } = "";

		[JsonPropertyName("deliveryRecorded")]
		[Description("true when the date the car was handed over is in the record. The term is "
			+ "counted from that date, so without it the time half cannot be measured at all.")]
		public bool DeliveryRecorded { get; init; }

		[JsonPropertyName("deliveredAt")]
		[Description("The date the dealership handed the car over, in UTC. EMPTY means it is not "
			+ "recorded. If this date is LATER than askedAt, the record's delivery date has not "
			+ "happened yet and the term is being counted from a date in the future: report that as "
			+ "a record the dealership has to correct, never as warranty remaining.")]
		public string DeliveredAt { get; init; } = "";

		[JsonPropertyName("deliveryOdometer")]
		[Description("The odometer reading taken the day the car was handed over. It is the anchor "
			+ "the distance half counts from. ZERO with deliveryRecorded false means it is not "
			+ "recorded - that is not 'the car had zero on it'.")]
		public int DeliveryOdometer { get; init; }

		[JsonPropertyName("deliveryOdometerUnit")]
		[Description("The unit that anchor reading is in. Units are never converted here, so say the "
			+ "value with the unit exactly as it comes.")]
		public string DeliveryOdometerUnit { get; init; } = "";

		[JsonPropertyName("odometerKnown")]
		[Description("true when the dealership's feed has brought a reading observed at or before "
			+ "askedAt. false means the distance half could not be measured at all - which is not "
			+ "the same as the car having travelled nothing.")]
		public bool OdometerKnown { get; init; }

		[JsonPropertyName("odometer")]
		[Description("The reading the distance half was measured against. ZERO with odometerKnown "
			+ "false means there is no reading, not a reading of zero.")]
		public int Odometer { get; init; }

		[JsonPropertyName("odometerUnit")]
		[Description("The unit that reading is in. It is never converted, and a reading in a unit "
			+ "the envelope was not published in suspends the distance half rather than being "
			+ "converted - says names both units when that happens.")]
		public string OdometerUnit { get; init; } = "";

		[JsonPropertyName("odometerObservedAt")]
		[Description("When that reading was OBSERVED - not when it was asked for. This is the date "
			+ "the distance half is really as of, and saying it is what stops an old reading from "
			+ "being repeated as today's mileage.")]
		public string OdometerObservedAt { get; init; } = "";

		[JsonPropertyName("canBeEvaluated")]
		[Description("true when the record could decide where this vehicle stands at askedAt. Note "
			+ "that a vehicle whose dealership never published an envelope answers TRUE here: that "
			+ "nobody published one is something the record knows for certain, and standing then "
			+ "reads NotPublished. false means no name applies at all - read says.")]
		public bool CanBeEvaluated { get; init; }

		[JsonPropertyName("standing")]
		[Description("Where the vehicle stands in one word. EMPTY means it could not be decided; "
			+ "there is no word here meaning 'unknown', which is why it comes back empty instead of "
			+ "guessing one. Read says either way - it is the answer, this is only its name.")]
		public string Standing { get; init; } = "";

		[JsonPropertyName("says")]
		[Description("THE ANSWER, in one sentence the record composed: the standing, BOTH halves "
			+ "with what is left or how far past, which half arrived first, the envelope it was "
			+ "measured against with its publication date, and the odometer it was measured against "
			+ "with the day it was observed - or, when something is missing, which fact is missing "
			+ "and why that half could not be measured. Say this sentence. Do not rebuild it out of "
			+ "the other fields and do not shorten it to the half that sounds better.")]
		public string Says { get; init; } = "";

		[JsonPropertyName("recordContradictsItself")]
		[Description("true when two facts the dealership recorded cannot both be true at askedAt, so "
			+ "nothing could be measured and standing and says come back empty. It is a fault in the "
			+ "record and not in the question - read detail and tell the person to raise it with "
			+ "their dealership.")]
		public bool RecordContradictsItself { get; init; }

		[JsonPropertyName("folder")]
		[Description("The person's folder, where their app keeps this vehicle's record.")]
		public string Folder { get; init; } = "";

		[JsonPropertyName("detail")]
		[Description("What this answer does not say, and anything the person had better do. Read it "
			+ "before answering: it is where the limits of the answer are.")]
		public string Detail { get; init; } = "";
	}
}
