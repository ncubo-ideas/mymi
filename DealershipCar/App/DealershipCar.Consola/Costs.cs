using System;
using System.Collections.Generic;
using DealershipCar.App;

namespace DealershipCar.Consola
{
	/// <summary>
	/// Los gastos del libro de costos que esta caratula guiona: uno DURADERO con su vida util y dos
	/// RECURRENTES, en DOS monedas, mas las lecturas del odometro que hacen que la distancia del
	/// periodo se pueda medir. Todo entra por DealershipCarAPI, que es la unica puerta, y ningun
	/// instante y ninguna cifra sale de un reloj ni de una cuenta.
	/// </summary>
	internal static class Costs
	{
		/// <summary>La moneda de los dos gastos de colones.</summary>
		internal const string TheColon = "CRC";

		/// <summary>La moneda del tercero, que es la que hace que el periodo tenga dos.</summary>
		internal const string TheDollar = "USD";

		/// <summary>En que se fue el gasto que dura mas que su propio periodo.</summary>
		internal const string TheTyres = "Llantas nuevas";

		/// <summary>Lo que costo, en colones.</summary>
		internal const decimal TheTyresCost = 240000.00m;

		/// <summary>Cuanto se espera que duren, en meses, dicho por quien las vendio.</summary>
		internal const int TheTyresLifeInMonths = 48;

		/// <summary>Cuando se incurrio, escrito y no derivado de ningun otro instante.</summary>
		internal static readonly DateTime TheTyresIncurred =
			new DateTime(2026, 3, 12, 9, 0, 0, DateTimeKind.Utc);

		/// <summary>En que se fue el primero de los que se consumen en su propio periodo.</summary>
		internal const string TheOilChange = "Cambio de aceite";

		/// <summary>Lo que costo, en la misma moneda que las llantas.</summary>
		internal const decimal TheOilChangeCost = 45000.00m;

		/// <summary>Cuando se incurrio.</summary>
		internal static readonly DateTime TheOilChangeIncurred =
			new DateTime(2026, 5, 20, 10, 30, 0, DateTimeKind.Utc);

		/// <summary>En que se fue el que llega en la OTRA moneda.</summary>
		internal const string TheWash = "Lavado premium";

		/// <summary>Lo que costo, en dolares.</summary>
		internal const decimal TheWashCost = 120.50m;

		/// <summary>Cuando se incurrio.</summary>
		internal static readonly DateTime TheWashIncurred =
			new DateTime(2026, 8, 5, 14, 15, 0, DateTimeKind.Utc);

		/// <summary>El comienzo del anio en que caen los tres, para preguntar por el.</summary>
		internal static readonly DateTime TheYearFrom =
			new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		/// <summary>El otro extremo del mismo anio, en su ultimo segundo entero.</summary>
		internal static readonly DateTime TheYearTo =
			new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc);

		/// <summary>El comienzo del semestre que solo alcanza los dos gastos de colones.</summary>
		internal static readonly DateTime TheColonesHalfFrom = TheYearFrom;

		/// <summary>El otro extremo de ese semestre.</summary>
		internal static readonly DateTime TheColonesHalfTo =
			new DateTime(2026, 6, 30, 23, 59, 59, DateTimeKind.Utc);

		/// <summary>El comienzo del anio anterior, que no tiene ningun gasto ni ninguna lectura.</summary>
		internal static readonly DateTime TheEarlierYearFrom =
			new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		/// <summary>El otro extremo del anio anterior.</summary>
		internal static readonly DateTime TheEarlierYearTo =
			new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc);

		/// <summary>La unidad de las tres lecturas, la misma en las tres para que el tramo se mida.</summary>
		internal const string TheUnit = "UnitA";

		/// <summary>
		/// Las tres lecturas del odometro del anio, en orden de observacion y creciendo. Son las que
		/// le dan al periodo un tramo de uso contra el que dividir el gasto; sin ellas el costo por
		/// distancia contesta la razon con nombre de que no hay tramo.
		/// </summary>
		internal static void TheThreeReadingsOfTheYear(DealershipCarAPI api)
		{
			api.RecordReading(20000, TheUnit, new DateTime(2026, 1, 10, 8, 0, 0, DateTimeKind.Utc), "Telemetry");
			api.RecordReading(22000, TheUnit, new DateTime(2026, 3, 1, 8, 0, 0, DateTimeKind.Utc), "Telemetry");
			api.RecordReading(29000, TheUnit, new DateTime(2026, 8, 20, 8, 0, 0, DateTimeKind.Utc), "Telemetry");
		}

		/// <summary>Registra el gasto duradero, con la vida util que dijo quien lo vendio.</summary>
		internal static void TheDurableCostArrives(DealershipCarAPI api)
		{
			api.RecordDurableCost(
				TheTyresIncurred, TheTyresCost, TheColon, TheTyres, TheTyresLifeInMonths);
		}

		/// <summary>Registra el recurrente de colones, que se consume en su propio periodo.</summary>
		internal static void TheOilChangeArrives(DealershipCarAPI api)
		{
			api.RecordRecurringCost(TheOilChangeIncurred, TheOilChangeCost, TheColon, TheOilChange);
		}

		/// <summary>Registra el recurrente de la OTRA moneda.</summary>
		internal static void TheWashArrives(DealershipCarAPI api)
		{
			api.RecordRecurringCost(TheWashIncurred, TheWashCost, TheDollar, TheWash);
		}

		/// <summary>
		/// Los tres gastos, en el orden en que una sucursal los manda, que NO es el orden en que se
		/// incurrieron: el de agosto llega antes que el de mayo a proposito, y las filas salen igual
		/// ordenadas por el instante en que se incurrieron.
		/// </summary>
		internal static IEnumerable<(string What, Action<DealershipCarAPI> Act)> TheThreeCosts()
		{
			yield return (
				"un gasto DURADERO: las llantas de marzo, con su vida util en meses",
				TheDurableCostArrives);
			yield return (
				"un RECURRENTE en la OTRA moneda: el lavado de agosto, fuera de orden",
				TheWashArrives);
			yield return (
				"otro RECURRENTE de colones: el cambio de aceite de mayo",
				TheOilChangeArrives);
		}
	}
}
