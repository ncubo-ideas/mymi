using System;
using Choreography.Theater;
using Puppeteer;

namespace DealershipCar.Dominio.Pruebas
{
	// LO QUE QUEDA DEL PERFIL QUE LA PUERTA TODAVIA NO PUEDE CONTESTAR. Nacio en T4 con ONCE
	// miembros, porque T4 le agrego a la app la cuarta global de raiz y los dos comandos y NINGUNA
	// lectura. T5 puso once lecturas en la puerta, y con eso SIETE de aquellos once dejaron de tener
	// razon de existir: cada uno tiene ahora su miembro en DealershipCarAPI y todos sus llamadores
	// pasaron a llamarlo ahi.
	//
	// QUEDAN DOS, Y LOS DOS SON DEL CAMINO QUE LA PUERTA NO TIENE:
	//
	//   Vin              VehicleProfile.ReadVin() no es ReadVin() de la puerta -- esa lee la AGENDA.
	//                    Es la unica lectura que contesta algo propio de la cuarta global, asi que
	//                    es con la que se mide su AUSENCIA antes de la migracion.
	//
	//   DescribeWarranty…WithNoOdometer   una lectura COMPUESTA, y las compuestas son de T6.
	//
	// LO QUE NO SE QUEDO, Y POR QUE. EnvelopeMonths y EnvelopeDistance tambien siguen sin miembro en
	// la puerta, y a proposito: los dos limites llegan a un llamador SOLO dentro de la frase que el
	// dominio compone, porque quien arme "36 meses o 60000" con escalares se llevo la composicion
	// fuera del dominio. Envolverlos aca los convertia en una superficie de lectura paralela, que es
	// exactamente lo que este archivo no puede ser. Son miembros de SCRIPT -- el camino entre globales
	// de T6 -- y ahi es donde se van a ejercitar.
	//
	// ESTO NO ES UNA SEGUNDA PUERTA: vive en Pruebas/, es internal, y hoy no repite ni una lectura
	// que la puerta tenga.
	internal static class AProfileReadWithoutTheDoor
	{
		internal static string Vin(PerformanceV2 app)
		{
			return AText(app, "@answer = profile.ReadVin();");
		}

		// LA RESPUESTA COMPUESTA QUE NO PIDE ODOMETRO. Es la unica de las compuestas que se puede
		// hacer con un solo argumento, asi que es la que prueba de punta a punta que los DOS
		// comandos entraron: sin sobre contesta NotPublished y sin entrega contesta por el ancla
		// ausente, y las dos frases son distintas de la que sale con los dos puestos.
		internal static string DescribeWarrantyEnvelopeWithNoOdometer(
			PerformanceV2 app, DateTime asOf)
		{
			return AText(
				app,
				"@answer = profile.DescribeWarrantyEnvelopeWithNoOdometer(@asOf);",
				p => { p["asOf", typeof(DateTime)] = asOf; });
		}

		private static string AText(PerformanceV2 app, string script)
		{
			return AText(app, script, p => { });
		}

		private static string AText(
			PerformanceV2 app, string script, Action<Parameters> arguments)
		{
			using (RentedParameter rented = app.Actor.RentedParameters())
			{
				app.Actor.Using(script)
				.WithParameters(rented, p => {
					p[Parameter.Out, "answer", typeof(string)] = default;
					arguments(p);
				})
				.PerformQuery();

				return rented["answer"].GetValue<string>();
			}
		}
	}
}
