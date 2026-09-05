using System;
using Choreography.Theater;

namespace DealershipCar.App
{
	/// <summary>
	/// El host de escritura de un vehiculo: un actor por Vin, con su journal, y las seis globales
	/// de raiz que la app necesita. Cada una llega bajo su propio nombre de upgrade, y los seis
	/// viajan en un solo comando porque cada comando deja una entrada en CADA apertura.
	/// </summary>
	public sealed class DealershipCarApp : PerformanceV2
	{
		private readonly string vin;
		private readonly string usageProfile;
		private readonly string modelCode;

		/// <summary>
		/// El Vin identifica el actor; el perfil de uso y el codigo de modelo se validan aca y
		/// viajan como parametros del DSL. Los dos son hechos de la PRIMERA apertura: el cuerpo de
		/// un upgrade no vuelve a correr, asi que un codigo de modelo distinto en una apertura
		/// posterior se ignora en silencio, y corregirlo pide un nombre de upgrade nuevo.
		/// </summary>
		public DealershipCarApp(string vin, string usageProfile, string modelCode)
			: base(TheActorFor(vin), TheDomain.Assembly)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(modelCode);

			this.vin = vin.Trim();
			this.usageProfile = WireName.UsageProfile(usageProfile, nameof(usageProfile));
			this.modelCode = modelCode.Trim();
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
				upgrade('especificacion-del-modelo') {
					specification = ModelSpecification(@model);
				}
				upgrade('perfil-del-vehiculo') {
					profile = VehicleProfile(@vin);
				}
				upgrade('estadias-en-taller') {
					stays = ShopStay(@vin);
				}
				upgrade('libro-de-costos') {
					ledger = CostLedger(@vin);
				}
			")
			.WithParameters(p => {
				p["vin",   typeof(string)] = vin;
				p["usage", typeof(Enum)]   = usageProfile;
				p["model", typeof(string)] = modelCode;
			})
			.PerformCommand();
		}
	}
}
