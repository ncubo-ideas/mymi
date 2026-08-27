using System;
using Choreography.Theater;
using DealershipCar.Dominio;

namespace DealershipCar.App
{
	public sealed class DealershipCarApp : PerformanceV2
	{
		private readonly string vin;
		private readonly string usageProfile;

		public DealershipCarApp(string vin, string usageProfile)
			: base(TheActorFor(vin), typeof(MaintenanceSchedule).Assembly)
		{
			this.vin = vin.Trim();
			this.usageProfile = WireName.UsageProfile(usageProfile, nameof(usageProfile));
		}

		public static string TheActorFor(string vin)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(vin);

			return ThePrefixOfEveryActor + vin.Trim();
		}

		public static string TheVinOf(string actorName)
		{
			if (string.IsNullOrWhiteSpace(actorName)) return "";

			if (!actorName.StartsWith(ThePrefixOfEveryActor, StringComparison.Ordinal)) return "";

			return actorName.Substring(ThePrefixOfEveryActor.Length);
		}

		private const string ThePrefixOfEveryActor = "maintenance-";

		protected override void OnHydrated()
		{
			Using(@"
				upgrade('semilla') {
					schedule = MaintenanceSchedule(@vin, @usage);
				}
				upgrade('bitacora-de-eventos') {
					logbook = Logbook();
				}
			")
			.WithParameters(p => {
				p["vin",   typeof(string)] = vin;
				p["usage", typeof(Enum)]   = usageProfile;
			})
			.PerformCommand();
		}
	}
}
