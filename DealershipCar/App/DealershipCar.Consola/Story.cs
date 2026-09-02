using System;
using System.Collections.Generic;
using DealershipCar.App;

namespace DealershipCar.Consola
{
	internal static class Story
	{
		internal const string TheVin = "1HGCM82633A004352";

		internal const string TheProfileAtDelivery = "Standard";

		internal const string TheModelAtDelivery = "SM-SEDAN-2025";

		private const string UnitA = "UnitA";

		internal static readonly DateTime Delivery = OnThe(2025, 9, 1);
		internal const int OdometerAtDelivery = 12000;

		internal static readonly DateTime TheFirstEvaluation = OnThe(2026, 3, 1);
		internal static readonly DateTime SixMonthsLater = OnThe(2026, 9, 1);

		internal static readonly string[] TheNineKinds =
		{
			"Inspection", "FluidChange", "FilterChange", "WearPartReplacement", "TimedComponent",
			"SoftwareUpdate", "RecallRemedy", "Diagnostic", "Repair"
		};

		internal static void ThePlanArrives(DealershipCarAPI api)
		{
			api.AdoptPlanRevision("R1", Delivery);

			api.SeedPlanOperation(
				"FluidChange", "TimeOrDistance", 365, 10000, UnitA,
				true, false,
				Delivery, OdometerAtDelivery, UnitA);

			api.SeedPlanOperation(
				"FilterChange", "DistanceOnly", 0, 15000, UnitA,
				true, false,
				Delivery, OdometerAtDelivery, UnitA);

			api.SeedPlanOperation(
				"TimedComponent", "TimeOnly", 550, 0, UnitA,
				true, true,
				Delivery, OdometerAtDelivery, UnitA);

			api.SeedPlanOperation(
				"Inspection", "ConditionOnly", 0, 0, UnitA,
				true, false,
				Delivery, OdometerAtDelivery, UnitA);

			api.AddOwnerOperation(
				"Diagnostic", "TimeOnly", 180, 0, UnitA,
				Delivery, OdometerAtDelivery, UnitA);
		}

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

		internal static void ThePersonAnswersTheFirstEvaluation(DealershipCarAPI api)
		{
			api.DeferOperation("Diagnostic", "Cost", TheFirstEvaluation);
			api.DeclareUsageProfile("SevereDuty", OnThe(2026, 3, 15));
		}

		internal static void AConditionThresholdIsCrossed(DealershipCarAPI api)
		{
			api.RecordConditionThreshold("Inspection", OnThe(2026, 4, 1));
		}

		private static DateTime OnThe(int year, int month, int day)
		{
			return new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);
		}
	}
}
