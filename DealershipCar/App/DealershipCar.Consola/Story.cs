using System;
using System.Collections.Generic;
using DealershipCar.App;

namespace DealershipCar.Consola
{
	// EL GUION DE LA DEMO: los hechos que esta caratula le da de comer al dominio, en el orden en
	// que llegan. Nada mas. Quien IMPRIME es Program -- la caratula decide como se ve, el dominio
	// decide que dice.
	//
	// TODOS LOS INSTANTES SON LITERALES, y es obligatorio, no prolijidad. El dominio no lee un
	// reloj: recibe cada instante como parametro (§3.5). Si estas fechas se calcularan con
	// DateTime.Now, el mismo journal reproducido manana daria otra cantidad de dias transcurridos
	// y por lo tanto otros estados de vencimiento. Un guion con fechas moviles no es reproducible,
	// y "reproducible" es justamente lo que esta caratula existe para medir.
	//
	// Y por eso mismo el salto de SEIS MESES es UNA sola llamada -- Evaluate(SixMonthsLater) -- y
	// no una espera. Que se pueda es la consecuencia entera de que el dominio no tenga reloj.
	//
	// LA HISTORIA QUE CUENTAN ESTOS HECHOS:
	//
	//   El vehiculo se entrega el 1/9/2025 con 12000 unidades. El plan del fabricante trae cuatro
	//   operaciones y la persona se agrega una quinta.
	//
	//   Seis meses de lecturas -- que llegan DESORDENADAS, porque asi llegan -- y la primera
	//   evaluacion, al 1/3/2026. Ahi el Diagnostic que la persona se agrego ya esta vencido, y ella
	//   lo aplaza por costo. Despues declara que usa el vehiculo en servicio severo.
	//
	//   Y entonces el salto: se evalua al 1/9/2026, seis meses despues, SIN QUE LLEGUE NINGUN HECHO
	//   NUEVO. Lo que se mueve se movio solo por el paso del tiempo, y eso es lo que se ve:
	//   el tiempo avanzo y la distancia no, porque no entro ninguna lectura.
	internal static class Story
	{
		internal const string TheVin = "1HGCM82633A004352";

		// El perfil con el que nace la agenda. La persona lo cambia despues, en su propio acto.
		internal const string TheProfileAtDelivery = "Standard";

		// Las dos unidades NUNCA se convierten (ley L7): esta demo mide todo en UnitA a proposito,
		// porque una lectura en la otra unidad suspenderia la dimension de distancia y la historia
		// pasaria a ser esa.
		private const string UnitA = "UnitA";

		internal static readonly DateTime Delivery = OnThe(2025, 9, 1);
		internal const int OdometerAtDelivery = 12000;

		// Las dos evaluaciones de la historia. La distancia entre ellas es el punto entero.
		internal static readonly DateTime TheFirstEvaluation = OnThe(2026, 3, 1);
		internal static readonly DateTime SixMonthsLater = OnThe(2026, 9, 1);

		// LAS NUEVE CLASES, escritas a mano como texto. Es el costo de C25 hecho visible: esta
		// caratula NO referencia el dominio -- no puede nombrar OperationKindCode -- asi que un
		// codigo cruza como el NOMBRE de su miembro y un nombre que no existe lo rechaza la puerta.
		// Un nombre mal escrito revienta en la puerta, ruidosamente y sin escribir nada; una clase
		// que el dominio agregue y esta lista no, simplemente no se imprime. Lo segundo es el riesgo
		// real de la lista, y se deja dicho.
		internal static readonly string[] TheNineKinds =
		{
			"Inspection", "FluidChange", "FilterChange", "WearPartReplacement", "TimedComponent",
			"SoftwareUpdate", "RecallRemedy", "Diagnostic", "Repair"
		};

		// ------------------------------------------------------ acto 1: llega el plan

		// La revision ABRE el plan; cada operacion entra despues, una por una (C5). El ancla completa
		// -- instante, lectura y la unidad de esa lectura -- viaja con cada operacion porque una
		// operacion nunca completada se mide desde algun lado y el dominio no puede inventarlo (C18).
		internal static void ThePlanArrives(DealershipCarAPI api)
		{
			api.AdoptPlanRevision("R1", Delivery);

			// Tiempo O distancia: vence con la primera de las dos que se alcance.
			api.SeedPlanOperation(
				"FluidChange", "TimeOrDistance", 365, 10000, UnitA,
				true, false,
				Delivery, OdometerAtDelivery, UnitA);

			// Solo distancia: sin lecturas no se mueve, y eso se ve en el salto.
			api.SeedPlanOperation(
				"FilterChange", "DistanceOnly", 0, 15000, UnitA,
				true, false,
				Delivery, OdometerAtDelivery, UnitA);

			// Solo tiempo, y CLASIFICADA DE SEGURIDAD: la ley L6 no la deja aplazar.
			api.SeedPlanOperation(
				"TimedComponent", "TimeOnly", 550, 0, UnitA,
				true, true,
				Delivery, OdometerAtDelivery, UnitA);

			// Solo condicion: no la mueve ni el tiempo ni la distancia, solo un cruce observado.
			api.SeedPlanOperation(
				"Inspection", "ConditionOnly", 0, 0, UnitA,
				true, false,
				Delivery, OdometerAtDelivery, UnitA);

			// La de la persona. Nunca obligatoria ni de seguridad: eso lo declara el fabricante, y
			// por eso esas dos banderas no estan en la firma en vez de estar en falso (ley L9).
			api.AddOwnerOperation(
				"Diagnostic", "TimeOnly", 180, 0, UnitA,
				Delivery, OdometerAtDelivery, UnitA);
		}

		// ------------------------------------------------------ acto 2: seis meses de odometro

		// LLEGAN DESORDENADAS a proposito. La de diciembre entra antes que la de noviembre, y la de
		// marzo antes que la de febrero: el orden de LLEGADA no es el orden de OBSERVACION, y
		// OINV-54 ordena por el instante observado. Los valores si suben con el instante observado,
		// porque una lectura que contradice la linea de observacion se rechaza adentro (C21) -- y
		// rechazar adentro cuesta una entrada permanente en el journal (C22), asi que esta demo no
		// la manda.
		// SE ENTREGAN COMO DATO y no como seis llamadas sueltas, para que la caratula pueda IMPRIMIR
		// el orden en que llegaron. Sin eso, el orden de llegada no se ve en ningun lado: el dominio
		// contesta lo mismo lo hayan mandado ordenadas o no -- que es OINV-54 funcionando -- asi que
		// la unica forma de que la historia MUESTRE que llegaron desordenadas es decir que mando.
		internal static IEnumerable<(int Value, string Unit, DateTime ObservedAt, string Source)>
			TheReadingsInTheOrderTheyArrive()
		{
			yield return (13200, UnitA, OnThe(2025, 10, 1), "Telemetry");
			yield return (16000, UnitA, OnThe(2025, 12, 1), "Telemetry");
			yield return (14500, UnitA, OnThe(2025, 11, 1), "HumanEntry");
			yield return (17300, UnitA, OnThe(2026, 1, 1), "Telemetry");
			yield return (20000, UnitA, OnThe(2026, 3, 1), "Telemetry");
			yield return (18600, UnitA, OnThe(2026, 2, 1), "CustomerDeclared");
		}

		internal static void TheOdometerReportsForSixMonths(DealershipCarAPI api)
		{
			foreach ((int value, string unit, DateTime observedAt, string source)
				in TheReadingsInTheOrderTheyArrive())
				api.RecordReading(value, unit, observedAt, source);
		}

		// ------------------------------------------------------ acto 3: contesta la persona

		// Lo que la persona hace DESPUES de ver la primera evaluacion. El aplazamiento se journaliza
		// CON su motivo, porque lo que decidio es la mitad del hecho (OINV-28).
		//
		// El perfil rige desde el 15/3, DESPUES de la evaluacion del 1/3, y no es un detalle de
		// gusto: declarar un perfil vigente antes de lo ya evaluado revienta adentro (OINV-15) y esa
		// excepcion deja una entrada permanente en el journal de la persona (C22).
		internal static void ThePersonAnswersTheFirstEvaluation(DealershipCarAPI api)
		{
			api.DeferOperation("Diagnostic", "Cost", TheFirstEvaluation);
			api.DeclareUsageProfile("SevereDuty", OnThe(2026, 3, 15));
		}

		// ------------------------------------------------------ acto 4: un cruce observado

		// Lo unico que mueve una operacion de base ConditionOnly (ley L3). Llega del taller, no del
		// calendario.
		internal static void AConditionThresholdIsCrossed(DealershipCarAPI api)
		{
			api.RecordConditionThreshold("Inspection", OnThe(2026, 4, 1));
		}

		// Un instante de la historia. UTC siempre: la puerta normaliza igual, y escribirlo aca deja
		// dicho que la convencion es del guion y no un efecto de la maquina donde corra.
		private static DateTime OnThe(int year, int month, int day)
		{
			return new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);
		}
	}
}
