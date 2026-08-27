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
	}
}
