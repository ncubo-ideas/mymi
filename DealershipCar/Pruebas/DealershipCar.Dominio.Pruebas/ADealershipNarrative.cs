using System.Collections.Generic;
using Puppets.Portador;

namespace DealershipCar.Dominio.Pruebas
{
	// LA NARRATIVA DE UN CONCESIONARIO, tal como sale de su API de eventos: senales crudas, con su
	// numero, su tag de tipo y su JSON sin interpretar. Es el aparejo de las pruebas de ingesta.
	//
	// LOS INSTANTES SON LITERALES, y es obligatorio y no prolijidad. El dominio no lee un reloj:
	// recibe cada instante como parametro. Una narrativa con fechas calculadas desde hoy daria otra
	// cantidad de dias transcurridos manana, y por lo tanto otros estados de vencimiento -- que es
	// justo lo que estas pruebas afirman. La regla es la misma que C27 le puso al guion de la
	// consola, y vale para todo lo que tenga que reproducir una secuencia recibida.
	//
	// LOS DOS DESCARTES ESTAN A PROPOSITO. Un concesionario publica su narrativa entera y este
	// modulo modela una rebanada: recibir un tipo que no consume es lo NORMAL. Y un evento nuestro
	// con un codigo que el canon no escribe tambien se descarta, en vez de reventar adentro del
	// comando -- que es lo que dejaria una marca permanente en el journal (C22).
	internal static class ADealershipNarrative
	{
		internal const string TheVin = "1HGCM82633A004352";

		// Cuantas senales de la historia se aplican, y cuantas se descartan. Escritas aparte para
		// que una prueba pueda afirmarlas sin volver a contar la lista a mano.
		internal const int SignalsThatApply = 8;
		internal const int SignalsThatAreDiscarded = 2;

		// El numero del ultimo evento de la historia. Es hasta donde tiene que quedar la bitacora
		// cuando la historia entera entro.
		internal const long TheLastEvent = 10;

		internal static IReadOnlyList<SenalDeEvento> TheWholeStory()
		{
			return new List<SenalDeEvento>
			{
				Revision(1, "R1", "2025-09-01T00:00:00Z"),

				PlanOperation(2, "FluidChange", "TimeOrDistance", 365, 10000, true, false),
				PlanOperation(3, "Inspection", "ConditionOnly", 0, 0, true, false),

				Reading(4, 13200, "UnitA", "2025-10-01T00:00:00Z", "Telemetry"),

				// Un tipo que esta app no consume. El concesionario lo publica igual.
				new SenalDeEvento(5, "WarrantyClaimFiled",
					"{\"id\":5,\"tipo\":\"WarrantyClaimFiled\",\"claim\":\"W-9912\"}"),

				Reading(6, 20000, "UnitA", "2026-03-01T00:00:00Z", "Telemetry"),

				Crossing(7, "Inspection", "2026-04-01T00:00:00Z"),

				Completion(8, "FluidChange", "2026-03-05T00:00:00Z", 20000, "UnitA"),

				Revision(9, "R2", "2026-06-01T00:00:00Z"),

				// Nuestro tipo, con una unidad que el canon no escribe. Se descarta en la frontera.
				Reading(10, 21000, "Miles", "2026-07-01T00:00:00Z", "Telemetry"),
			};
		}

		// Los primeros cuantos, para simular una caida a mitad de bloque.
		internal static IReadOnlyList<SenalDeEvento> TheFirst(int howMany)
		{
			List<SenalDeEvento> some = new List<SenalDeEvento>();

			foreach (SenalDeEvento signal in TheWholeStory())
			{
				if (some.Count == howMany) break;
				some.Add(signal);
			}

			return some;
		}

		internal static SenalDeEvento Reading(
			long number, int value, string unit, string observedAt, string source)
		{
			return new SenalDeEvento(number, "OdometerReported",
				"{\"id\":" + number + ",\"tipo\":\"OdometerReported\""
				+ ",\"value\":" + value
				+ ",\"unit\":\"" + unit + "\""
				+ ",\"observedAt\":\"" + observedAt + "\""
				+ ",\"source\":\"" + source + "\"}");
		}

		internal static SenalDeEvento Crossing(long number, string kind, string observedAt)
		{
			return new SenalDeEvento(number, "ConditionThresholdCrossed",
				"{\"id\":" + number + ",\"tipo\":\"ConditionThresholdCrossed\""
				+ ",\"kind\":\"" + kind + "\""
				+ ",\"observedAt\":\"" + observedAt + "\"}");
		}

		internal static SenalDeEvento Completion(
			long number, string kind, string completedAt, int completionReading, string unit)
		{
			return new SenalDeEvento(number, "OperationCompleted",
				"{\"id\":" + number + ",\"tipo\":\"OperationCompleted\""
				+ ",\"kind\":\"" + kind + "\""
				+ ",\"completedAt\":\"" + completedAt + "\""
				+ ",\"completionReading\":" + completionReading
				+ ",\"unit\":\"" + unit + "\"}");
		}

		internal static SenalDeEvento Revision(long number, string code, string adoptedAt)
		{
			return new SenalDeEvento(number, "PlanRevisionAdopted",
				"{\"id\":" + number + ",\"tipo\":\"PlanRevisionAdopted\""
				+ ",\"revisionCode\":\"" + code + "\""
				+ ",\"adoptedAt\":\"" + adoptedAt + "\"}");
		}

		internal static SenalDeEvento PlanOperation(
			long number, string kind, string basis, int timeLimitDays, int distanceLimit,
			bool isMandatory, bool isSafety)
		{
			return PlanOperationMeasuredIn(
				number, kind, basis, timeLimitDays, distanceLimit, isMandatory, isSafety,
				"UnitA", "UnitA");
		}

		// La misma siembra con las DOS unidades sueltas. La del limite y la del ancla son dos cosas
		// distintas (C18) y una historia en la que siempre coinciden no puede mostrar que lo sean.
		internal static SenalDeEvento PlanOperationMeasuredIn(
			long number, string kind, string basis, int timeLimitDays, int distanceLimit,
			bool isMandatory, bool isSafety, string unit, string anchorUnit)
		{
			return new SenalDeEvento(number, "PlanOperationSeeded",
				"{\"id\":" + number + ",\"tipo\":\"PlanOperationSeeded\""
				+ ",\"kind\":\"" + kind + "\""
				+ ",\"basis\":\"" + basis + "\""
				+ ",\"timeLimitDays\":" + timeLimitDays
				+ ",\"distanceLimit\":" + distanceLimit
				+ ",\"unit\":\"" + unit + "\""
				+ ",\"isMandatory\":" + (isMandatory ? "true" : "false")
				+ ",\"isSafety\":" + (isSafety ? "true" : "false")
				+ ",\"anchoredAt\":\"2025-09-01T00:00:00Z\""
				+ ",\"anchorReading\":12000"
				+ ",\"anchorUnit\":\"" + anchorUnit + "\"}");
		}

		// ------------------------------------------ la ficha del fabricante, como feed
		//
		// LA MISMA FICHA QUE LA CONSOLA PUBLICA POR LA PUERTA, pero llegando de a un hecho por
		// senal: ONCE tipos de mensaje, uno por comando de publicacion, y ninguno que traiga una
		// revision entera. Un evento por revision necesitaria un script generado por evento, y un
		// script generado es exactamente donde un error de tipeo se vuelve una entrada permanente
		// que loguea un fallo de rehidratacion en cada arranque futuro (S5).
		//
		// EL ORDEN NO ES UN DETALLE. PublishX rechaza una revision que no sea un borrador abierto,
		// SealRevision rechaza un borrador sin contenido, y SupersedeRevision nombra una sucesora
		// que ya tiene que existir. Por eso los numeros de evento arrancan en el 11 y siguen la
		// historia de arriba: el proveedor sirve en orden desde el punto de control, y esta lista es
		// ese orden escrito a mano.
		internal const string TheFirstRevision = "MY2025-R1";
		internal const string TheSecondRevision = "MY2025-R2";

		internal const string TheFirstOilGrade = "5W-30 synthetic";
		internal const string TheSecondOilGrade = "0W-20 synthetic";

		internal const string TheSystem = "POWERTRAIN";
		internal const int TheMilestone = 10000;

		internal const string TheFirstPublication = "2025-08-15T00:00:00Z";
		internal const string TheSecondPublication = "2026-02-01T00:00:00Z";

		// Cuantas senales de la ficha se aplican, y hasta donde queda la bitacora cuando entro
		// entera. Escritas aparte para que una prueba pueda afirmarlas sin contar la lista a mano.
		internal const int SpecificationSignalsThatApply = 16;
		internal const long TheLastSpecificationEvent = 26;

		internal static IReadOnlyList<SenalDeEvento> TheWholeSpecSheet()
		{
			return new List<SenalDeEvento>
			{
				RevisionOpened(11, TheFirstRevision, TheFirstPublication),

				// LOS TRES DECIMALES LLEVAN UN CERO A LA DERECHA -- 4.20, 2.40, 89.90 -- y no es
				// prolijidad: el journal escribe un decimal sin los ceros que sobran, asi que con
				// 4.2 y 2.4 la normalizacion de la frontera no cambia nada y quitarla no se nota en
				// ninguna comparacion. Con el cero, si.
				Fluid(12, TheFirstRevision, "EngineOil", TheFirstOilGrade, "4.20", "UnitA",
					TheMilestone, "UnitA", 365),

				Tyre(13, TheFirstRevision, "Front", "205/55R16", "2.40", "UnitA", "94V"),

				// El minimo NO va primero, a proposito: una lectura que devolviera "el primer grado"
				// en vez del que lleva esa postura acertaria por casualidad.
				Fuel(14, TheFirstRevision, "95 RON Premium", "Permitted"),
				Fuel(15, TheFirstRevision, "91 RON regular", "Minimum"),
				Fuel(16, TheFirstRevision, "E85 Flex", "Prohibited"),

				Interval(17, TheFirstRevision, "FluidChange", TheMilestone, "UnitA", 365),

				Price(18, TheFirstRevision, "FluidChange", TheMilestone, "UnitA", "89.90", "USD",
					TheFirstPublication, "2026-12-31T00:00:00Z"),

				// Un cero en las DOS mitades es como el wire dice que el sistema no publico limites
				// propios, y la traduccion es del dominio.
				CoveredSystem(19, TheFirstRevision, TheSystem, 0, 0, "UnitA"),

				Exclusion(20, TheFirstRevision, TheSystem, "wear parts",
					"when the part is a consumable"),

				ServicingPolicy(21, TheFirstRevision,
					"service may be done at any qualified workshop",
					"keep the invoices and the parts records"),

				RevisionSealed(22, TheFirstRevision),

				// La segunda revision republica SOLO el aceite y sustituye a la primera: es lo que
				// hace visible que una revision aplica hacia adelante y que sustituir retira lo que
				// la sucesora no volvio a publicar.
				RevisionOpened(23, TheSecondRevision, TheSecondPublication),

				Fluid(24, TheSecondRevision, "EngineOil", TheSecondOilGrade, "4.20", "UnitA",
					TheMilestone, "UnitA", 365),

				RevisionSealed(25, TheSecondRevision),

				RevisionSuperseded(26, TheFirstRevision, TheSecondRevision),
			};
		}

		internal static SenalDeEvento RevisionOpened(long number, string code, string publishedAt)
		{
			return new SenalDeEvento(number, "SpecificationRevisionOpened",
				"{\"id\":" + number + ",\"tipo\":\"SpecificationRevisionOpened\""
				+ ",\"revisionCode\":\"" + code + "\""
				+ ",\"publishedAt\":\"" + publishedAt + "\"}");
		}

		internal static SenalDeEvento RevisionSealed(long number, string code)
		{
			return new SenalDeEvento(number, "SpecificationRevisionSealed",
				"{\"id\":" + number + ",\"tipo\":\"SpecificationRevisionSealed\""
				+ ",\"revisionCode\":\"" + code + "\"}");
		}

		internal static SenalDeEvento RevisionSuperseded(
			long number, string code, string supersededByCode)
		{
			return new SenalDeEvento(number, "SpecificationRevisionSuperseded",
				"{\"id\":" + number + ",\"tipo\":\"SpecificationRevisionSuperseded\""
				+ ",\"revisionCode\":\"" + code + "\""
				+ ",\"supersededByCode\":\"" + supersededByCode + "\"}");
		}

		// La capacidad llega como TEXTO del numero y no como decimal de C#, porque lo que la senal
		// trae es JSON: escribirlo aca deja que una prueba mande "4.20" y mire que el journal
		// guarde 4.2.
		internal static SenalDeEvento Fluid(
			long number, string revisionCode, string kind, string grade,
			string capacity, string capacityUnit,
			int intervalDistance, string intervalUnit, int intervalDays)
		{
			return new SenalDeEvento(number, "FluidSpecificationPublished",
				"{\"id\":" + number + ",\"tipo\":\"FluidSpecificationPublished\""
				+ ",\"revisionCode\":\"" + revisionCode + "\""
				+ ",\"kind\":\"" + kind + "\""
				+ ",\"grade\":\"" + grade + "\""
				+ ",\"capacity\":" + capacity
				+ ",\"capacityUnit\":\"" + capacityUnit + "\""
				+ ",\"intervalDistance\":" + intervalDistance
				+ ",\"intervalUnit\":\"" + intervalUnit + "\""
				+ ",\"intervalDays\":" + intervalDays + "}");
		}

		internal static SenalDeEvento Tyre(
			long number, string revisionCode, string position, string size,
			string coldPressure, string pressureUnit, string ratings)
		{
			return new SenalDeEvento(number, "TyreSpecificationPublished",
				"{\"id\":" + number + ",\"tipo\":\"TyreSpecificationPublished\""
				+ ",\"revisionCode\":\"" + revisionCode + "\""
				+ ",\"position\":\"" + position + "\""
				+ ",\"size\":\"" + size + "\""
				+ ",\"coldPressure\":" + coldPressure
				+ ",\"pressureUnit\":\"" + pressureUnit + "\""
				+ ",\"ratings\":\"" + ratings + "\"}");
		}

		internal static SenalDeEvento Fuel(
			long number, string revisionCode, string grade, string standing)
		{
			return new SenalDeEvento(number, "FuelGradePublished",
				"{\"id\":" + number + ",\"tipo\":\"FuelGradePublished\""
				+ ",\"revisionCode\":\"" + revisionCode + "\""
				+ ",\"grade\":\"" + grade + "\""
				+ ",\"standing\":\"" + standing + "\"}");
		}

		internal static SenalDeEvento Interval(
			long number, string revisionCode, string kind,
			int intervalDistance, string intervalUnit, int intervalDays)
		{
			return new SenalDeEvento(number, "ServiceIntervalPublished",
				"{\"id\":" + number + ",\"tipo\":\"ServiceIntervalPublished\""
				+ ",\"revisionCode\":\"" + revisionCode + "\""
				+ ",\"kind\":\"" + kind + "\""
				+ ",\"intervalDistance\":" + intervalDistance
				+ ",\"intervalUnit\":\"" + intervalUnit + "\""
				+ ",\"intervalDays\":" + intervalDays + "}");
		}

		internal static SenalDeEvento Price(
			long number, string revisionCode, string kind,
			int milestoneDistance, string milestoneUnit,
			string amount, string currency, string validFrom, string validUntil)
		{
			return new SenalDeEvento(number, "ServicePricePublished",
				"{\"id\":" + number + ",\"tipo\":\"ServicePricePublished\""
				+ ",\"revisionCode\":\"" + revisionCode + "\""
				+ ",\"kind\":\"" + kind + "\""
				+ ",\"milestoneDistance\":" + milestoneDistance
				+ ",\"milestoneUnit\":\"" + milestoneUnit + "\""
				+ ",\"amount\":" + amount
				+ ",\"currency\":\"" + currency + "\""
				+ ",\"validFrom\":\"" + validFrom + "\""
				+ ",\"validUntil\":\"" + validUntil + "\"}");
		}

		internal static SenalDeEvento CoveredSystem(
			long number, string revisionCode, string systemCode,
			int ownLimitMonths, int ownLimitDistance, string ownLimitUnit)
		{
			return new SenalDeEvento(number, "CoveredSystemPublished",
				"{\"id\":" + number + ",\"tipo\":\"CoveredSystemPublished\""
				+ ",\"revisionCode\":\"" + revisionCode + "\""
				+ ",\"systemCode\":\"" + systemCode + "\""
				+ ",\"ownLimitMonths\":" + ownLimitMonths
				+ ",\"ownLimitDistance\":" + ownLimitDistance
				+ ",\"ownLimitUnit\":\"" + ownLimitUnit + "\"}");
		}

		internal static SenalDeEvento Exclusion(
			long number, string revisionCode, string systemCode, string exclusion, string condition)
		{
			return new SenalDeEvento(number, "CoverageExclusionPublished",
				"{\"id\":" + number + ",\"tipo\":\"CoverageExclusionPublished\""
				+ ",\"revisionCode\":\"" + revisionCode + "\""
				+ ",\"systemCode\":\"" + systemCode + "\""
				+ ",\"exclusion\":\"" + exclusion + "\""
				+ ",\"condition\":\"" + condition + "\"}");
		}

		internal static SenalDeEvento ServicingPolicy(
			long number, string revisionCode, string rule, string whatPreservesCover)
		{
			return new SenalDeEvento(number, "ServicingPolicyPublished",
				"{\"id\":" + number + ",\"tipo\":\"ServicingPolicyPublished\""
				+ ",\"revisionCode\":\"" + revisionCode + "\""
				+ ",\"rule\":\"" + rule + "\""
				+ ",\"whatPreservesCover\":\"" + whatPreservesCover + "\"}");
		}
	}
}
