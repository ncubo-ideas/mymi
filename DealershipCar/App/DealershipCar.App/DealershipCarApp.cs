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

			return ThePrefixOfEveryActor + vin.Trim();
		}

		// EL INVERSO, y vive pegado al directo a proposito: son una sola regla de nombres, y
		// separarlas es como se desincronizan. Contesta "" -- no revienta -- cuando el nombre no es
		// de los nuestros, porque quien pregunta esta MIRANDO UNA CARPETA sin saber todavia que hay
		// adentro, y encontrarse una subcarpeta ajena es normal, no un defecto de quien llama.
		//
		// Lo necesita el servidor MCP de T9: para abrir el journal hace falta el nombre del actor, el
		// nombre del actor lleva el VIN, y el VIN esta justamente adentro del journal que todavia no
		// se abrio. Descubrirlo del nombre de la carpeta es como se sale de esa vuelta.
		public static string TheVinOf(string actorName)
		{
			if (string.IsNullOrWhiteSpace(actorName)) return "";

			if (!actorName.StartsWith(ThePrefixOfEveryActor, StringComparison.Ordinal)) return "";

			return actorName.Substring(ThePrefixOfEveryActor.Length);
		}

		private const string ThePrefixOfEveryActor = "maintenance-";

		protected override void OnHydrated()
		{
			// UN UPGRADE APARTE, y no una linea mas adentro de 'semilla'. La guarda de upgrade es
			// una-vez-por-actor: 'semilla' YA CORRIO en todo journal de DealershipCar que exista --
			// los de la historia de T7 y los de cualquier persona -- asi que agregarle una linea no
			// le llegaria a ninguno, en silencio. Una clave nueva si se aplica donde falta y no hace
			// nada donde ya esta. Es exactamente para lo que sirve el mecanismo: migrar sin un .exe
			// de migracion.
			//
			// VAN LOS DOS EN UN SOLO SCRIPT a proposito: cada PerformCommand deja una entrada en
			// CADA arranque, aunque el upgrade ya este aplicado y no haga nada (medido en T6). Dos
			// comandos costarian dos entradas por apertura, para siempre, por una global que se crea
			// una vez. Que un script pueda llevar dos upgrades con nombres distintos esta medido por
			// TheSecondGlobalReachesAJournalThatOnlyHasTheSeed.
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
