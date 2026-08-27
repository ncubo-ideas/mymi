using System;
using System.Collections.Generic;
using System.IO;
using Choreography.Theater;
using DealershipCar.Dominio;

namespace DealershipCar.App
{
	public sealed class ReadOnlyDealershipCarApp : PerformanceV2
	{
		public ReadOnlyDealershipCarApp(string vin)
			: base(DealershipCarApp.TheActorFor(vin), typeof(MaintenanceSchedule).Assembly)
		{
		}

		public static bool AJournalExistsFor(string folder, string vin)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(folder);

			return Directory.Exists(
				Path.Combine(folder, DealershipCarApp.TheActorFor(vin), "journal"));
		}

		public static IReadOnlyList<string> TheVehiclesIn(string folder)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(folder);

			var vehicles = new List<string>();
			if (!Directory.Exists(folder)) return vehicles;

			foreach (string each in Directory.GetDirectories(folder))
			{
				string vin = DealershipCarApp.TheVinOf(Path.GetFileName(each));

				if (vin.Length > 0 && AJournalExistsFor(folder, vin)) vehicles.Add(vin);
			}

			vehicles.Sort(StringComparer.Ordinal);
			return vehicles;
		}

		/// <summary>
		/// Empty on purpose: seeding here would make every question the AI asks leave a journal entry
		/// and break the single-writer invariant. It is what separates this host from the app's.
		/// </summary>
		protected override void OnHydrated()
		{
		}
	}
}
