using System;
using System.Collections.Generic;
using DealershipCar.App;

namespace DealershipCar.Consola
{
	/// <summary>
	/// Las estadias en el taller que esta caratula guiona: una entera de un solo acto, una que se abre
	/// y se cierra en dos hechos, y una que queda abierta. Todo entra por DealershipCarAPI, que es la
	/// unica puerta, y ningun instante y ningun dia sale de un reloj.
	/// </summary>
	internal static class ShopStays
	{
		/// <summary>La sucursal de la estadia que llega entera, en un solo hecho.</summary>
		internal const string TheFirstBranch = "Taller Central";

		/// <summary>Por que entro, con las palabras de la sucursal.</summary>
		internal const string TheFirstSubject = "Cambio de frenos";

		/// <summary>Cuando entro a la primera.</summary>
		internal static readonly DateTime TheFirstEntry =
			new DateTime(2026, 5, 11, 8, 30, 0, DateTimeKind.Utc);

		/// <summary>Cuando la entregaron.</summary>
		internal static readonly DateTime TheFirstCollection =
			new DateTime(2026, 5, 13, 16, 45, 0, DateTimeKind.Utc);

		/// <summary>
		/// Los TRES dias que la sucursal dice que cubrio. No salen de los dos instantes: sacarlos
		/// pediria una zona horaria que este registro no lleva, y con la zona equivocada el 11 podria
		/// salir siendo el 10.
		/// </summary>
		internal static readonly string[] TheFirstDays =
			{ "2026-05-11", "2026-05-12", "2026-05-13" };

		/// <summary>La sucursal de la estadia que se abre y se cierra en dos hechos.</summary>
		internal const string TheSecondBranch = "Taller Escazú";

		/// <summary>Por que entro a la segunda.</summary>
		internal const string TheSecondSubject = "Revisión de los 20 mil";

		/// <summary>Cuando entro a la segunda.</summary>
		internal static readonly DateTime TheSecondEntry =
			new DateTime(2026, 7, 6, 7, 15, 0, DateTimeKind.Utc);

		/// <summary>Cuando la entregaron.</summary>
		internal static readonly DateTime TheSecondCollection =
			new DateTime(2026, 7, 7, 17, 0, 0, DateTimeKind.Utc);

		/// <summary>Los DOS dias que cubrio, distintos en cantidad de los de la primera.</summary>
		internal static readonly string[] TheSecondDays = { "2026-07-06", "2026-07-07" };

		/// <summary>La sucursal de la estadia que queda abierta.</summary>
		internal const string TheOpenBranch = "Taller Curridabat";

		/// <summary>Por que entro a la que queda abierta.</summary>
		internal const string TheOpenSubject = "Ruido en la suspensión";

		/// <summary>
		/// Cuando entro a la que nadie cierra. Desde aca el vehiculo queda adentro, que es el unico
		/// estado que alcanzan las dos lecturas por instante y ninguna de las seis por dias.
		/// </summary>
		internal static readonly DateTime TheOpenEntry =
			new DateTime(2026, 10, 5, 9, 0, 0, DateTimeKind.Utc);

		/// <summary>El periodo del ano en que caen las tres, para preguntar por el.</summary>
		internal const string TheYearFrom = "2026-01-01";

		/// <summary>El otro extremo del mismo periodo.</summary>
		internal const string TheYearTo = "2026-12-31";

		/// <summary>Un periodo que corta la primera estadia por la mitad, para el renglon de la regla 2.</summary>
		internal const string TheCutFrom = "2026-05-12";

		/// <summary>El otro extremo del periodo que corta.</summary>
		internal const string TheCutTo = "2026-05-31";

		/// <summary>Registra de un solo acto la estadia que ya termino, que es la forma correctiva.</summary>
		internal static void TheFirstStayArrivesWhole(DealershipCarAPI api)
		{
			api.RecordShopStay(
				TheFirstBranch, TheFirstSubject, TheFirstEntry, TheFirstCollection, TheFirstDays);
		}

		/// <summary>Registra la entrada de la segunda, que desde aca queda abierta.</summary>
		internal static void TheSecondStayEnters(DealershipCarAPI api)
		{
			api.RecordShopEntry(TheSecondBranch, TheSecondSubject, TheSecondEntry);
		}

		/// <summary>La cierra con los dias que la sucursal dice que cubrio.</summary>
		internal static void TheSecondStayIsCollected(DealershipCarAPI api)
		{
			api.RecordShopCollection(TheSecondCollection, TheSecondDays);
		}

		/// <summary>Registra la entrada que nadie cierra.</summary>
		internal static void TheThirdStayEntersAndStaysOpen(DealershipCarAPI api)
		{
			api.RecordShopEntry(TheOpenBranch, TheOpenSubject, TheOpenEntry);
		}

		/// <summary>Los tres hechos de las dos estadias cerradas, en el orden en que un taller los manda.</summary>
		internal static IEnumerable<(string What, Action<DealershipCarAPI> Act)> TheTwoClosedStays()
		{
			yield return ("una estadia entera DE UN SOLO ACTO, con sus TRES dias", TheFirstStayArrivesWhole);
			yield return ("la ENTRADA de la segunda, que desde aca queda abierta", TheSecondStayEnters);
			yield return ("la ENTREGA de la segunda, que la cierra con los DOS dias que dijo la sucursal", TheSecondStayIsCollected);
		}
	}
}
