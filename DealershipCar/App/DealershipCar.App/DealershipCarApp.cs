using System;
using Choreography.Theater;
using DealershipCar.Dominio;

namespace DealershipCar.App
{
	// El puppet hospedado. La caratula -- consola, browser, servidor MCP -- construye ESTO una sola
	// vez y lo mantiene vivo; un actor no se crea por request.
	//
	// UN ACTOR POR VIN, y el nombre lo dice. Un segundo vehiculo es un segundo actor y un segundo
	// journal, que es lo que OAGG-03 quiere decir con "su propio journal": nada de esta rebanada
	// itera vehiculos.
	//
	// LA SEMILLA VA POR OnHydrated y no por el constructor porque OnHydrated corre en CADA
	// hidratacion, reinicios incluidos, y la guarda de upgrade('nombre') es una-vez-por-actor. Asi
	// el arranque es idempotente sin un .exe de migracion.
	//
	// La agenda es una GLOBAL DE RAIZ DEL DSL y no un campo de C#. Un campo no se journaliza: tras
	// la rehidratacion estaria vacio y la llamada siguiente fallaria.
	//
	// EL VIN Y EL PERFIL ENTRAN COMO PARAMETROS, no interpolados en el texto del DSL. Interpolarlos
	// dejaria el valor adentro de la sentencia, y lo que hace reproducible al replay es justamente
	// que el argumento viaje aparte de la sentencia. Sobre un journal ya sembrado el upgrade no
	// hace nada, asi que abrir con OTRO vin no lo cambia: manda el que quedo journalizado.
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

		// El nombre del actor lleva el VIN, asi que dos vehiculos no comparten journal ni siquiera
		// por accidente.
		public static string TheActorFor(string vin)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(vin);

			return "maintenance-" + vin.Trim();
		}

		protected override void OnHydrated()
		{
			Using(@"
				upgrade('semilla') {
					schedule = MaintenanceSchedule(@vin, @usage);
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
