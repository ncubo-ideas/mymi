using System;
using System.Collections.Generic;
using System.IO;
using Choreography.Theater;
using DealershipCar.Dominio;

namespace DealershipCar.App
{
	// EL MISMO ACTOR Y EL MISMO DOMINIO QUE LA APP, SIN LA SEMILLA. Es el host que abre el journal
	// de una persona para LEERLO y nada mas: el servidor MCP de T9 entra por aca.
	//
	// EL OnHydrated VACIO ES LA PIEZA, no un detalle. DealershipCarApp siembra en OnHydrated, y el
	// PerformCommand que envuelve a los dos upgrade deja una entrada en CADA hidratacion aunque los
	// upgrades ya esten aplicados y no hagan nada (medido en T6, remedido en T9: reabrir con la app
	// lleva el journal de 12 a 13 y de 13 a 14). Si el servidor MCP reusara DealershipCarApp, cada
	// pregunta de la IA escribiria en el journal de la persona y romperia la invariante de UN SOLO
	// ESCRITOR. De ahi que este host exista: mismo actor, mismo dominio, sin sembrar.
	//
	// POR QUE VIVE EN LA APP Y NO EN LA CARATULA MCP. El ensamblado que se le pasa al actor TIENE que
	// ser el del dominio, y esta medido lo que pasa si no lo es: apuntado al ensamblado de la App el
	// motor loguea "Class 'MaintenanceSchedule' is not registered in the actor's library. Loaded
	// assemblies: DealershipCar.App" y TODA lectura revienta. Nombrar MaintenanceSchedule es lo unico
	// que hace falta -- y una caratula no puede: el dominio es internal (C1/S1), las caratulas no
	// referencian Dominio, y AssemblyInfo.cs nombra exactamente dos amigos. Asi que el host de solo
	// lectura es capa de perform, igual que DealershipCarApp, y la caratula sigue sin ver el dominio.
	//
	// SOLO LECTURA SON DOS DISCIPLINAS Y ESTA CLASE APORTA UNA. La otra es de quien la usa: nunca un
	// PerformCommand. Esta clase no puede hacerla cumplir -- PerformanceV2 expone los performs -- asi
	// que la puerta que se le pone encima es la que no debe llamar comandos.
	public sealed class ReadOnlyDealershipCarApp : PerformanceV2
	{
		public ReadOnlyDealershipCarApp(string vin)
			: base(DealershipCarApp.TheActorFor(vin), typeof(MaintenanceSchedule).Assembly)
		{
		}

		// SI LA APP YA ESCRIBIO EL JOURNAL DE ESTE VEHICULO, y hay que preguntarlo ANTES del Start.
		//
		// MEDIDO EN T9, y es la razon de que esta pregunta exista en vez de descubrirse consultando:
		// Start() sobre una carpeta que nunca tuvo actor NO falla -- CREA el actor, con un journal
		// vacio de 32 bytes y un meta.bin de 48. O sea que un host de solo lectura apuntado a una
		// carpeta virgen ESCRIBE, y ademas deja la carpeta en un estado en el que esta misma pregunta
		// contestaria que si para siempre. Preguntar antes es lo unico que lo evita.
		public static bool AJournalExistsFor(string folder, string vin)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(folder);

			return Directory.Exists(
				Path.Combine(folder, DealershipCarApp.TheActorFor(vin), "journal"));
		}

		// QUE VEHICULOS HAY EN ESA CARPETA, por el nombre de sus actores. Ordenados, para que dos
		// corridas elijan el mismo cuando hay mas de uno.
		//
		// Se descubre en vez de configurarse porque el VIN ya esta en el journal: pedirselo tambien
		// al archivo de configuracion seria una segunda fuente de verdad para la identidad del
		// vehiculo, y la que se quedaria vieja es la de afuera. Una carpeta sin ningun vehiculo no es
		// un error: es que la persona todavia no abrio su app.
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

		protected override void OnHydrated()
		{
			// VACIO A PROPOSITO, y es lo unico que separa este host del de la app. Ver el encabezado:
			// sembrar aca haria que cada pregunta de la IA dejara una entrada en el journal.
		}
	}
}
