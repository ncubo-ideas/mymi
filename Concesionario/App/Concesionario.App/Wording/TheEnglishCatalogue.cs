using System.Collections.Generic;

namespace Concesionario.App.Wording
{
	/// <summary>
	/// LO MISMO DICHO EN INGLES, clave por clave. Un juego de claves que no coincida con el del idioma
	/// del registro deja media pantalla con la clave cruda entre llaves y nada se pone rojo, asi que la
	/// paridad se afirma por prueba y no por cuidado.
	/// <para>
	/// Los nombres del canon -- OperationKindCode, FluidKindCode -- NO se traducen aqui: lo que se
	/// traduce es la etiqueta con que se leen. El codigo sigue siendo identidad y cruza intacto.
	/// </para>
	/// </summary>
	internal static class TheEnglishCatalogue
	{
		internal static IReadOnlyDictionary<string, string> TheCatalogue()
		{
			return new Dictionary<string, string>
			{
				["days.one"] = "1 day",
				["days.many"] = "{count} days",

				["months.one"] = "1 month",
				["months.many"] = "{count} months",

				["day.long"] = "{day} {month} {year}",
				["day.short"] = "{day} {month} {year}",

				["month.1"] = "January",
				["month.2"] = "February",
				["month.3"] = "March",
				["month.4"] = "April",
				["month.5"] = "May",
				["month.6"] = "June",
				["month.7"] = "July",
				["month.8"] = "August",
				["month.9"] = "September",
				["month.10"] = "October",
				["month.11"] = "November",
				["month.12"] = "December",

				["month.short.1"] = "Jan",
				["month.short.2"] = "Feb",
				["month.short.3"] = "Mar",
				["month.short.4"] = "Apr",
				["month.short.5"] = "May",
				["month.short.6"] = "Jun",
				["month.short.7"] = "Jul",
				["month.short.8"] = "Aug",
				["month.short.9"] = "Sep",
				["month.short.10"] = "Oct",
				["month.short.11"] = "Nov",
				["month.short.12"] = "Dec",

				["canon.operation.Inspection"] = "Inspection",
				["canon.operation.FluidChange"] = "Fluid change",
				["canon.operation.FilterChange"] = "Filter change",
				["canon.operation.WearPartReplacement"] = "Wear part replacement",
				["canon.operation.TimedComponent"] = "Timed component",
				["canon.operation.SoftwareUpdate"] = "Software update",
				["canon.operation.RecallRemedy"] = "Recall remedy",
				["canon.operation.Diagnostic"] = "Diagnostic",
				["canon.operation.Repair"] = "Repair",

				["canon.dueState.NotDue"] = "Not due",
				["canon.dueState.DueSoon"] = "Due soon",
				["canon.dueState.Due"] = "Due",
				["canon.dueState.Overdue"] = "Overdue",
				["canon.dueState.Deferred"] = "Deferred",
				["canon.dueState.Completed"] = "Completed",
				["canon.dueState.Superseded"] = "Superseded",

				["canon.fluid.EngineOil"] = "Engine oil",
				["canon.fluid.BrakeFluid"] = "Brake fluid",
				["canon.fluid.Coolant"] = "Coolant",
				["canon.fluid.TransmissionFluid"] = "Transmission fluid",
				["canon.fluid.PowerSteeringFluid"] = "Power steering fluid",
				["canon.fluid.WasherFluid"] = "Washer fluid",

				["canon.tyre.Front"] = "Front",
				["canon.tyre.Rear"] = "Rear",
				["canon.tyre.Spare"] = "Spare",

				["canon.usageProfile.Standard"] = "standard",
				["canon.usageProfile.SevereDuty"] = "severe-duty",
				["canon.usageProfile.LowDistance"] = "low-distance",
				["canon.usageProfile.Commercial"] = "commercial",

				["canon.deferralReason.Cost"] = "the cost",
				["canon.deferralReason.Timing"] = "the timing",
				["canon.deferralReason.SecondOpinion"] = "a second opinion",
				["canon.deferralReason.NotConvinced"] = "doubts that it is needed",
				["canon.deferralReason.WillDoElsewhere"] = "having it done elsewhere",

				["canon.standing.Inside"] = "Inside",
				["canon.standing.Reached"] = "Reached",
				["canon.standing.NotPublished"] = "Not published",
				["canon.standing.Covered"] = "Covered",
				["canon.standing.CoveredWithExclusion"] = "Covered with an exclusion",
				["canon.standing.NotCovered"] = "Not covered",
				["canon.standing.OutsideEnvelope"] = "Outside the limits",

				["board.operation.Inspection"] = "Inspection",
				["board.operation.FluidChange"] = "Fluid change",
				["board.operation.FilterChange"] = "Filter change",
				["board.operation.WearPartReplacement"] = "Wear parts",
				["board.operation.TimedComponent"] = "Timed component",
				["board.operation.SoftwareUpdate"] = "Software update",
				["board.operation.RecallRemedy"] = "Recall remedy",
				["board.operation.Diagnostic"] = "Diagnostic",
				["board.operation.Repair"] = "Repair",

				["board.operation.short.Inspection"] = "Inspection",
				["board.operation.short.FluidChange"] = "Fluids",
				["board.operation.short.FilterChange"] = "Filter",
				["board.operation.short.WearPartReplacement"] = "Wear",
				["board.operation.short.TimedComponent"] = "Timed",
				["board.operation.short.SoftwareUpdate"] = "Software",
				["board.operation.short.RecallRemedy"] = "Recall",
				["board.operation.short.Diagnostic"] = "Diagnostic",
				["board.operation.short.Repair"] = "Repair",

				["board.dueState.Overdue"] = "Overdue",
				["board.dueState.Due"] = "Due now",
				["board.dueState.DueSoon"] = "Coming up",
				["board.dueState.NotDue"] = "Up to date",
				["board.dueState.Deferred"] = "Put off",
				["board.dueState.Completed"] = "Done",
				["board.dueState.Missed"] = "Never done",
				["board.dueState.Superseded"] = "Replaced",

				["board.standing.Inside"] = "In force",
				["board.standing.Reached"] = "Reached",
				["board.standing.NotPublished"] = "Not published",
				["board.standing.Covered"] = "Covered",
				["board.standing.CoveredWithExclusion"] = "Covered with an exclusion",
				["board.standing.NotCovered"] = "Not covered",
				["board.standing.OutsideEnvelope"] = "Outside the limits",

				["board.fluid.EngineOil"] = "Engine oil",
				["board.fluid.BrakeFluid"] = "Brake fluid",
				["board.fluid.Coolant"] = "Coolant",
				["board.fluid.TransmissionFluid"] = "Transmission fluid",
				["board.fluid.PowerSteeringFluid"] = "Steering fluid",
				["board.fluid.WasherFluid"] = "Washer fluid",

				["board.tyre.Front"] = "Front",
				["board.tyre.Rear"] = "Rear",
				["board.tyre.Spare"] = "Spare",

				["dimension.Time"] = "time",
				["dimension.Distance"] = "distance",

				["quantity.distance"] = "{amount} {unit}",

				["clause.BeforeTheLimit"] = "{dimension}: {amount} of {limit} since the last one",
				["clause.AtTheLimit"] = "{dimension}: exactly on a limit of {limit}",
				["clause.PastTheLimit"] = "{dimension}: {amount} past a limit of {limit}",
				["clause.AbsentReading"] =
					"{dimension}: cannot be evaluated because there is no usable reading",
				["clause.UnitMismatch"] =
					"{dimension}: cannot be evaluated because the limit is in {unit} and the "
					+ "measurements are in {measured}, and units are never converted",
				["clause.UsageProfile"] =
					"{dimension}: cannot be evaluated because the {profile} usage profile ignores "
					+ "distance limits",

				["clauses.two"] = "{first}; {second}",

				["remainder.StillAhead.one"] = "{dimension}: {amount} to go",
				["remainder.StillAhead.many"] = "{dimension}: {amount} to go",
				["remainder.AlreadyPassed"] = "{dimension}: {amount} over",
				["remainder.AtTheLimit"] = "{dimension}: exactly on the limit",
				["remainder.OutsideTheLaw"] = "{dimension}: this operation has no {dimension} limit",
				["remainder.NoOperationScheduled"] =
					"no {kind} operation is scheduled for this vehicle",

				["countdown.both"] = "{distance}; {time}",

				["reason.NoOperationScheduled"] =
					"This vehicle has no {kind} scheduled, so there is nothing that could be "
					+ "overdue.",
				["reason.NotEvaluatedYet"] =
					"{state}, but this operation has not been evaluated yet, so this is no evidence "
					+ "that the operation is not overdue.",
				["reason.Deferred"] =
					"{state} until {until} because of {reason}. {used} of {allowed} deferrals used.",
				["reason.Ended"] = "{state}.",
				["reason.ACrossingArrived"] =
					"{state} because an observed threshold crossing arrived for this operation.",
				["reason.WaitsForACrossing"] =
					"{state} because this operation has no time limit and no distance limit: it "
					+ "falls due only when an observed threshold crossing arrives.",
				["reason.NothingCouldBeEvaluated"] =
					"{state}, but nothing could be evaluated, so this is no evidence that the "
					+ "operation is not overdue. {clauses}.",
				["reason.Decided"] = "{state}, decided by {who}. {clauses}.",

				["reason.decidedBy.Time"] = "time",
				["reason.decidedBy.Distance"] = "distance",
				["reason.decidedBy.TimeAndDistance"] = "time and distance",

				["envelope.published"] = "{months} or {distance}, published on {day}.",
				["envelope.notPublished"] =
					"The dealership has published no warranty envelope for this vehicle, so there "
					+ "is nothing here to measure against -- which is not the same as this vehicle "
					+ "having no warranty.",

				["envelope.said"] = "This vehicle's warranty envelope: {reason} {clause} {law}",
				["envelope.law"] =
					"Position only: this says where this vehicle stands against the envelope the "
					+ "dealership published, and nothing about whether a claim would be paid -- the "
					+ "manufacturer decides that, not this record.",

				["envelope.reason.Settled"] = "{standing}.",
				["envelope.reason.NoDelivery"] =
					"cannot be evaluated, because no delivery has been recorded for this vehicle, "
					+ "and both halves of the envelope are measured from the day it was handed "
					+ "over. The dealership records that.",
				["envelope.reason.NoReading"] =
					"cannot be evaluated, because there is no odometer reading for this vehicle at "
					+ "the instant asked or before it. An absent reading is not a reading of "
					+ "nothing, and the distance half cannot be measured without one.",
				["envelope.reason.UnitMismatch"] =
					"its distance half cannot be evaluated, because the published distance limit is "
					+ "in {limit} and this vehicle's odometer is in {other}, and units are never "
					+ "converted.",

				["envelope.clause.TheEnvelopeAlone"] = "{published}",
				["envelope.clause.ThePublishedHalfAlone"] = "Envelope published: {published}",
				["envelope.clause.Whole"] =
					"Envelope published: {published} {remainder} {odometer}",

				["envelope.remainder"] = "{time} {distance} {first}",

				["envelope.timeHalf.AtTheLimit"] = "Time: exactly on a limit of {limit}.",
				["envelope.timeHalf.PastTheLimit"] = "Time: {amount} past a limit of {limit}.",
				["envelope.timeHalf.BeforeTheLimit.one"] = "Time: {amount} left of {limit}.",
				["envelope.timeHalf.BeforeTheLimit.many"] = "Time: {amount} left of {limit}.",

				["envelope.distanceHalf.AtTheLimit"] = "Distance: exactly on a limit of {limit}.",
				["envelope.distanceHalf.PastTheLimit"] = "Distance: {amount} past a limit of {limit}.",
				["envelope.distanceHalf.BeforeTheLimit.one"] = "Distance: {amount} left of {limit}.",
				["envelope.distanceHalf.BeforeTheLimit.many"] = "Distance: {amount} left of {limit}.",
				["envelope.distanceHalf.NotMeasured"] = "Distance: cannot be measured, {why}",

				["envelope.why.AbsentReading"] =
					"because there is no odometer reading for this vehicle at the instant asked or "
					+ "before it, and the distance half is measured from one.",
				["envelope.why.UnitMismatch"] =
					"because the published distance limit is in {limit} and {whose} is in {other}, "
					+ "and units are never converted.",

				["envelope.whose.TheDeliveryReading"] =
					"the odometer reading recorded at delivery",
				["envelope.whose.TheOdometer"] = "this vehicle's odometer",

				["envelope.first.NeitherHalfCanBeSaid"] =
					"Which of the two halves arrives first cannot be said either, because one of "
					+ "them was not measured.",
				["envelope.first.NeitherHalfIsReached"] =
					"Neither half has been reached, and which arrives first depends on how much "
					+ "this vehicle is driven from here on: the two halves are not comparable with "
					+ "each other, and this record says what it holds instead of forecasting a rate.",
				["envelope.first.BothHalvesTogether"] =
					"Reached by time and distance, which arrived together.",
				["envelope.first.TheDistanceHalf"] = "Reached by distance, which arrived first.",
				["envelope.first.TheTimeHalf"] = "Reached by time, which arrived first.",

				["envelope.odometer.Measured"] =
					"Measured against an odometer of {reading} observed on {observedOn}, counting "
					+ "from the {deliveryReading} recorded when the vehicle was handed over on "
					+ "{deliveredOn}.",
				["envelope.odometer.Absent"] =
					"There is no odometer reading to measure the distance half against, and the "
					+ "dealership's feed is what brings one.",
			};
		}
	}
}
