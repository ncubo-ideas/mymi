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

				["months.one"] = "1 mes",
				["months.many"] = "{count} meses",

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

				["envelope.published"] = "{months} o {distance}, publicado el {day}.",
				["envelope.notPublished"] =
					"El concesionario no ha publicado ningún sobre de garantía para este vehículo, "
					+ "así que aquí no hay contra qué medir -- que no es lo mismo que este vehículo "
					+ "no tenga garantía.",

				["envelope.said"] = "El sobre de garantía de este vehículo: {reason} {clause} {law}",
				["envelope.law"] =
					"Solo posición: esto dice dónde está parado este vehículo frente al sobre que "
					+ "publicó el concesionario, y nada sobre si un reclamo se pagaría -- eso lo "
					+ "decide el fabricante, no este registro.",

				["envelope.reason.Settled"] = "{standing}.",
				["envelope.reason.NoDelivery"] =
					"no se puede evaluar, porque no hay ninguna entrega registrada para este "
					+ "vehículo, y las dos mitades del sobre se miden desde el día en que se "
					+ "entregó. Eso lo registra el concesionario.",
				["envelope.reason.NoReading"] =
					"no se puede evaluar, porque no hay ninguna lectura del odómetro de este "
					+ "vehículo al instante preguntado ni antes. Una lectura ausente no es una "
					+ "lectura de nada, y la mitad de distancia no se puede medir sin una.",
				["envelope.reason.UnitMismatch"] =
					"su mitad de distancia no se puede evaluar, porque el límite de distancia "
					+ "publicado está en {limit} y el odómetro de este vehículo está en {other}, y "
					+ "las unidades nunca se convierten.",

				["envelope.clause.TheEnvelopeAlone"] = "{published}",
				["envelope.clause.ThePublishedHalfAlone"] = "Sobre publicado: {published}",
				["envelope.clause.Whole"] =
					"Sobre publicado: {published} {remainder} {odometer}",

				["envelope.remainder"] = "{time} {distance} {first}",

				["envelope.timeHalf.AtTheLimit"] = "Tiempo: justo en un límite de {limit}.",
				["envelope.timeHalf.PastTheLimit"] =
					"Tiempo: {amount} pasados de un límite de {limit}.",
				["envelope.timeHalf.BeforeTheLimit.one"] = "Tiempo: queda {amount} de {limit}.",
				["envelope.timeHalf.BeforeTheLimit.many"] = "Tiempo: quedan {amount} de {limit}.",

				["envelope.distanceHalf.AtTheLimit"] = "Distancia: justo en un límite de {limit}.",
				["envelope.distanceHalf.PastTheLimit"] =
					"Distancia: {amount} pasados de un límite de {limit}.",
				["envelope.distanceHalf.BeforeTheLimit.one"] = "Distancia: queda {amount} de {limit}.",
				["envelope.distanceHalf.BeforeTheLimit.many"] =
					"Distancia: quedan {amount} de {limit}.",
				["envelope.distanceHalf.NotMeasured"] = "Distancia: no se puede medir, {why}",

				["envelope.why.AbsentReading"] =
					"porque no hay ninguna lectura del odómetro de este vehículo al instante "
					+ "preguntado ni antes, y la mitad de distancia se mide desde una.",
				["envelope.why.UnitMismatch"] =
					"porque el límite de distancia publicado está en {limit} y {whose} está en "
					+ "{other}, y las unidades nunca se convierten.",

				["envelope.whose.TheDeliveryReading"] =
					"la lectura del odómetro registrada en la entrega",
				["envelope.whose.TheOdometer"] = "el odómetro de este vehículo",

				["envelope.first.NeitherHalfCanBeSaid"] =
					"Cuál de las dos mitades llega primero tampoco se puede decir, porque una de "
					+ "las dos no se midió.",
				["envelope.first.NeitherHalfIsReached"] =
					"Ninguna de las dos mitades se ha alcanzado, y cuál llega primero depende de "
					+ "cuánto se maneje este vehículo de aquí en adelante: las dos mitades no son "
					+ "comparables entre sí, y este registro dice lo que tiene en vez de pronosticar "
					+ "un ritmo.",
				["envelope.first.BothHalvesTogether"] =
					"Alcanzado por tiempo y distancia, que llegaron juntos.",
				["envelope.first.TheDistanceHalf"] = "Alcanzado por distancia, que llegó primero.",
				["envelope.first.TheTimeHalf"] = "Alcanzado por tiempo, que llegó primero.",

				["envelope.odometer.Measured"] =
					"Medido contra un odómetro de {reading} observado el {observedOn}, contando "
					+ "desde los {deliveryReading} registrados cuando el vehículo se entregó el "
					+ "{deliveredOn}.",
				["envelope.odometer.Absent"] =
					"No hay ninguna lectura del odómetro contra la cual medir la mitad de "
					+ "distancia, y quien trae una es el feed del concesionario.",

				["specification.published"] = "{clause} Publicado en la revisión {revision}.",
				["specification.notPublished"] =
					"{subject}: no se ha publicado nada para este modelo, así que no hay nada que "
					+ "citar ni ninguna revisión que nombrar.",

				["coverage.law"] =
					"Solo alcance: esto dice lo que el fabricante publicó sobre el alcance de la "
					+ "garantía, y nada sobre cómo se resolvería un reclamo -- eso lo decide el "
					+ "fabricante, no este registro.",

				["coverage.subject.scope"] = "La cobertura de garantía",
				["coverage.subject.system"] = "La cobertura de garantía del sistema '{system}'",

				["coverage.notPublished"] = "{answer} {law}",

				["coverage.scope"] =
					"Sistemas dentro del alcance publicado: {systems} Exclusiones: {exclusions} "
					+ "{law}",

				["coverage.systems.none"] = "ninguno publicado.",
				["coverage.system"] = "'{code}' está dentro del alcance publicado.",
				["coverage.system.withLimits"] =
					"'{code}' está dentro del alcance publicado, con límites propios de {limits}.",

				["coverage.exclusions.none"] = "ninguna publicada.",
				["coverage.exclusion"] =
					"Exclusión sobre '{system}': {text} Publicada para aplicar cuando: {condition}.",


				["coverage.limits.both"] = "{months} o {distance}",

				["coverage.settled"] = "{subject}: {standing}. {clause}",
				["coverage.unitMismatch"] =
					"{subject}: no se puede evaluar, porque el límite de distancia que rige está en "
					+ "{limit} y el odómetro de este vehículo está en {other}, y las unidades nunca "
					+ "se convierten. {clause}",

				["coverage.clause"] = "{reason} {law}",
				["coverage.clause.withExclusions"] = "{reason} {exclusions} {law}",
				["coverage.clause.reasonless"] = "{exclusions} {law}",

				["coverage.reason.NotInTheVocabulary"] =
					"El alcance publicado no nombra este sistema en ninguna parte, así que aquí no "
					+ "hay nada sobre él -- que no es lo mismo que el alcance lo deje afuera.",
				["coverage.reason.OnlyAmongTheExclusions"] =
					"El alcance publicado nombra este sistema solo entre sus exclusiones, y nunca "
					+ "entre los sistemas que declara adentro.",
				["coverage.reason.InsideTheLimits"] =
					"Límites que rigen: {limits}. Este vehículo está dentro de ellos.",
				["coverage.reason.OutsideTheLimits"] =
					"Límites que rigen: {limits}. Este vehículo está fuera de ellos.",
			};
		}
	}
}
