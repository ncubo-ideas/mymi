using System.Collections.Generic;

namespace Concesionario.App.Wording
{
	/// <summary>
	/// LO QUE SE DICE EN ESPANOL, que es el idioma del registro de este concesionario. Palabra por
	/// palabra lo que las dos tablas de la caratula decian antes de que existiera un catalogo: mover
	/// una redaccion no es reescribirla.
	/// <para>
	/// Cada clave es una ETIQUETA ENTERA o una FRASE ENTERA y nunca un pedazo. El espanol concuerda en
	/// genero y el ingles no, asi que un catalogo que pegara fragmentos daria concordancias rotas que
	/// ninguna prueba ve.
	/// </para>
	/// <para>
	/// DOS FAMILIAS DICEN LOS MISMOS CONJUNTOS CERRADOS y no se fusionan aqui: "canon" es como los
	/// nombra la hoja del fabricante y "board" como los nombra el tablero. Hoy no coinciden -- el
	/// tablero dice "Vencido" donde la hoja dice "Atrasada" -- y unificarlas cambiaria lo que la
	/// persona lee hoy, que es otra decision y no esta.
	/// </para>
	/// </summary>
	internal static class TheSpanishCatalogue
	{
		internal static IReadOnlyDictionary<string, string> TheCatalogue()
		{
			return new Dictionary<string, string>
			{
				["days.one"] = "1 día",
				["days.many"] = "{count} días",

				["day.long"] = "{day} de {month} de {year}",
				["day.short"] = "{day} {month} {year}",

				["month.1"] = "enero",
				["month.2"] = "febrero",
				["month.3"] = "marzo",
				["month.4"] = "abril",
				["month.5"] = "mayo",
				["month.6"] = "junio",
				["month.7"] = "julio",
				["month.8"] = "agosto",
				["month.9"] = "setiembre",
				["month.10"] = "octubre",
				["month.11"] = "noviembre",
				["month.12"] = "diciembre",

				["month.short.1"] = "ene",
				["month.short.2"] = "feb",
				["month.short.3"] = "mar",
				["month.short.4"] = "abr",
				["month.short.5"] = "may",
				["month.short.6"] = "jun",
				["month.short.7"] = "jul",
				["month.short.8"] = "ago",
				["month.short.9"] = "set",
				["month.short.10"] = "oct",
				["month.short.11"] = "nov",
				["month.short.12"] = "dic",

				["canon.operation.Inspection"] = "Revisión",
				["canon.operation.FluidChange"] = "Cambio de fluido",
				["canon.operation.FilterChange"] = "Cambio de filtro",
				["canon.operation.WearPartReplacement"] = "Cambio de pieza de desgaste",
				["canon.operation.TimedComponent"] = "Componente por tiempo",
				["canon.operation.SoftwareUpdate"] = "Actualización de software",
				["canon.operation.RecallRemedy"] = "Llamado a revisión",
				["canon.operation.Diagnostic"] = "Diagnóstico",
				["canon.operation.Repair"] = "Reparación",

				["canon.dueState.NotDue"] = "Sin vencer",
				["canon.dueState.DueSoon"] = "Por vencer",
				["canon.dueState.Due"] = "Vencida",
				["canon.dueState.Overdue"] = "Atrasada",
				["canon.dueState.Deferred"] = "Aplazada",
				["canon.dueState.Completed"] = "Hecha",
				["canon.dueState.Superseded"] = "Sustituida",

				["canon.fluid.EngineOil"] = "Aceite de motor",
				["canon.fluid.BrakeFluid"] = "Líquido de frenos",
				["canon.fluid.Coolant"] = "Refrigerante",
				["canon.fluid.TransmissionFluid"] = "Líquido de transmisión",
				["canon.fluid.PowerSteeringFluid"] = "Líquido de dirección asistida",
				["canon.fluid.WasherFluid"] = "Líquido de limpiaparabrisas",

				["canon.tyre.Front"] = "Delantera",
				["canon.tyre.Rear"] = "Trasera",
				["canon.tyre.Spare"] = "De repuesto",

				["canon.usageProfile.Standard"] = "normal",
				["canon.usageProfile.SevereDuty"] = "de uso severo",
				["canon.usageProfile.LowDistance"] = "de poca distancia",
				["canon.usageProfile.Commercial"] = "comercial",

				["canon.deferralReason.Cost"] = "el costo",
				["canon.deferralReason.Timing"] = "la fecha",
				["canon.deferralReason.SecondOpinion"] = "una segunda opinión",
				["canon.deferralReason.NotConvinced"] = "dudas de que haga falta",
				["canon.deferralReason.WillDoElsewhere"] = "que se va a hacer en otro lado",

				["canon.standing.Inside"] = "Adentro",
				["canon.standing.Reached"] = "Alcanzado",
				["canon.standing.NotPublished"] = "Sin publicar",
				["canon.standing.Covered"] = "Cubierto",
				["canon.standing.CoveredWithExclusion"] = "Cubierto con exclusión",
				["canon.standing.NotCovered"] = "No cubierto",
				["canon.standing.OutsideEnvelope"] = "Fuera de los límites",

				["board.operation.Inspection"] = "Revisión",
				["board.operation.FluidChange"] = "Cambio de líquidos",
				["board.operation.FilterChange"] = "Cambio de filtro",
				["board.operation.WearPartReplacement"] = "Piezas de desgaste",
				["board.operation.TimedComponent"] = "Componente por tiempo",
				["board.operation.SoftwareUpdate"] = "Actualización de software",
				["board.operation.RecallRemedy"] = "Llamado a revisión",
				["board.operation.Diagnostic"] = "Diagnóstico",
				["board.operation.Repair"] = "Reparación",

				["board.operation.short.Inspection"] = "Revisión",
				["board.operation.short.FluidChange"] = "Líquidos",
				["board.operation.short.FilterChange"] = "Filtro",
				["board.operation.short.WearPartReplacement"] = "Desgaste",
				["board.operation.short.TimedComponent"] = "Por tiempo",
				["board.operation.short.SoftwareUpdate"] = "Software",
				["board.operation.short.RecallRemedy"] = "Llamado",
				["board.operation.short.Diagnostic"] = "Diagnóstico",
				["board.operation.short.Repair"] = "Reparación",

				["board.dueState.Overdue"] = "Vencido",
				["board.dueState.Due"] = "Toca ahora",
				["board.dueState.DueSoon"] = "Se acerca",
				["board.dueState.NotDue"] = "Al día",
				["board.dueState.Deferred"] = "Pospuesto",
				["board.dueState.Completed"] = "Realizado",
				["board.dueState.Missed"] = "No se hizo",
				["board.dueState.Superseded"] = "Reemplazado",

				["board.standing.Inside"] = "Vigente",
				["board.standing.Reached"] = "Alcanzada",
				["board.standing.NotPublished"] = "Sin publicar",
				["board.standing.Covered"] = "Cubierto",
				["board.standing.CoveredWithExclusion"] = "Cubierto con exclusión",
				["board.standing.NotCovered"] = "No cubierto",
				["board.standing.OutsideEnvelope"] = "Fuera de los límites",

				["board.fluid.EngineOil"] = "Aceite de motor",
				["board.fluid.BrakeFluid"] = "Líquido de frenos",
				["board.fluid.Coolant"] = "Refrigerante",
				["board.fluid.TransmissionFluid"] = "Líquido de transmisión",
				["board.fluid.PowerSteeringFluid"] = "Líquido de dirección",
				["board.fluid.WasherFluid"] = "Líquido limpiaparabrisas",

				["board.tyre.Front"] = "Delanteras",
				["board.tyre.Rear"] = "Traseras",
				["board.tyre.Spare"] = "Repuesto",

				["dimension.Time"] = "tiempo",
				["dimension.Distance"] = "distancia",

				["quantity.distance"] = "{amount} {unit}",

				["clause.BeforeTheLimit"] = "{dimension}: {amount} de {limit} desde la última",
				["clause.AtTheLimit"] = "{dimension}: justo en un límite de {limit}",
				["clause.PastTheLimit"] = "{dimension}: {amount} pasados de un límite de {limit}",
				["clause.AbsentReading"] =
					"{dimension}: no se puede evaluar porque no hay ninguna lectura usable",
				["clause.UnitMismatch"] =
					"{dimension}: no se puede evaluar porque el límite está en {unit} y las "
					+ "mediciones están en {measured}, y las unidades nunca se convierten",
				["clause.UsageProfile"] =
					"{dimension}: no se puede evaluar porque el perfil de uso {profile} ignora los "
					+ "límites de distancia",

				["clauses.two"] = "{first}; {second}",

				["remainder.StillAhead.one"] = "{dimension}: te falta {amount}",
				["remainder.StillAhead.many"] = "{dimension}: te faltan {amount}",
				["remainder.AlreadyPassed"] = "{dimension}: lleva {amount} de más",
				["remainder.AtTheLimit"] = "{dimension}: justo en el límite",
				["remainder.OutsideTheLaw"] =
					"{dimension}: esta operación no tiene límite de {dimension}",
				["remainder.NoOperationScheduled"] =
					"no hay ninguna operación de {kind} agendada para este vehículo",

				["countdown.both"] = "{distance}; {time}",

				["reason.NoOperationScheduled"] =
					"Este vehículo no tiene agendada ninguna operación de {kind}, así que no hay "
					+ "nada que pueda estar vencido.",
				["reason.NotEvaluatedYet"] =
					"{state}, pero esta operación todavía no se ha evaluado, así que esto no es "
					+ "evidencia de que la operación no esté vencida.",
				["reason.Deferred"] =
					"{state} hasta el {until} por {reason}. {used} de {allowed} aplazamientos "
					+ "usados.",
				["reason.Ended"] = "{state}.",
				["reason.ACrossingArrived"] =
					"{state} porque llegó un cruce de umbral observado para esta operación.",
				["reason.WaitsForACrossing"] =
					"{state} porque esta operación no tiene límite de tiempo ni de distancia: vence "
					+ "solo cuando llega un cruce de umbral observado.",
				["reason.NothingCouldBeEvaluated"] =
					"{state}, pero no se pudo evaluar nada, así que esto no es evidencia de que la "
					+ "operación no esté vencida. {clauses}.",
				["reason.Decided"] = "{state}, decidido por {who}. {clauses}.",

				["reason.decidedBy.Time"] = "tiempo",
				["reason.decidedBy.Distance"] = "distancia",
				["reason.decidedBy.TimeAndDistance"] = "tiempo y distancia",
			};
		}
	}
}
