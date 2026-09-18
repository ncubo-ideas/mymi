using System;
using Choreography.Theater;
using Puppeteer;

namespace Concesionario.Dominio.Pruebas
{
	// LO QUE QUEDA DEL PERFIL QUE LA PUERTA TODAVIA NO PUEDE CONTESTAR. Nacio en T4 con ONCE
	// miembros, porque T4 le agrego a la app la cuarta global de raiz y los dos comandos y NINGUNA
	// lectura. T5 puso once lecturas en la puerta y siete de aquellos once dejaron de tener razon de
	// existir. T6 puso las seis compuestas y se llevo el octavo.
	//
	// QUEDA UNO:
	//
	//   Vin              VehicleProfile.ReadVin() no es ReadVin() de la puerta -- esa lee la AGENDA.
	//                    Es la unica lectura que contesta algo propio de la cuarta global, asi que
	//                    es con la que se mide su AUSENCIA antes de la migracion.
	//
	// LO QUE SE FUE EN T6, Y POR QUE. DescribeWarrantyEnvelopeWithNoOdometer era una lectura
	// COMPUESTA sin miembro en la puerta, y por eso vivia aqui. La lectura de la puerta
	// DescribeWarrantyEnvelope(asOf, language) contesta exactamente esa frase cuando el vehiculo
	// no tiene ninguna lectura del odometro --
	// es la rama que su script elige -- asi que dejar el script crudo al lado habria sido una
	// segunda superficie de lectura para lo mismo, que es lo que este archivo no puede ser.
	//
	// LO QUE SIGUE SIN VENIR AQUI. EnvelopeMonths y EnvelopeDistance tampoco tienen miembro en la
	// puerta, y a proposito: los dos limites llegan a un llamador SOLO dentro de la frase que el
	// dominio compone. Envolverlos aqui los convertiria en la superficie paralela que la regla
	// existe para evitar. Son miembros de SCRIPT -- el camino entre globales de las lecturas
	// compuestas -- y ahi es donde se ejercitan.
	//
	// ESTO NO ES UNA SEGUNDA PUERTA: vive en Pruebas/, es internal, y no repite ninguna lectura que
	// la puerta tenga.
	internal static class AProfileReadWithoutTheDoor
	{
		internal static string Vin(PerformanceV2 app)
		{
			return AText(app, "@answer = profile.ReadVin();");
		}

		private static string AText(PerformanceV2 app, string script)
		{
			using (RentedParameter rented = app.Actor.RentedParameters())
			{
				app.Actor.Using(script)
				.WithParameters(rented, p => {
					p[Parameter.Out, "answer", typeof(string)] = default;
				})
				.PerformQuery();

				return rented["answer"].GetValue<string>();
			}
		}
	}
}
