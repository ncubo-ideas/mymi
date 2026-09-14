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
			};
		}
	}
}
