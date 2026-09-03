using System;
using Choreography.Theater;
using Puppeteer;

namespace DealershipCar.Dominio.Pruebas
{
	// LO QUE LA PUERTA TODAVIA NO PUEDE CONTESTAR DE LA QUINTA GLOBAL. Es la misma forma que
	// AProfileReadWithoutTheDoor tuvo en su primer dia y por la misma razon: T4 le agrego a la app la
	// CUARTA global de raiz, sus dos comandos y NINGUNA lectura, y nacio con once miembros. SH-3 le
	// agrego la QUINTA, sus tres comandos y NINGUNA lectura, y nacio con cuatro.
	//
	// Y SE ENCOGIO EN SH-4, como se encogio el del perfil en T5 y en T6. SH-4 puso las ocho lecturas
	// en la puerta, y ese mismo dia Inside, Says y Days dejaron de tener razon de existir: el script
	// crudo que contesta lo que la puerta ya contesta es la superficie de lectura PARALELA que DEC-31
	// y DEC-35 refusaron. Sus llamadores se mudaron a IsInsideTheShopAt, DescribeTheStayAt y
	// DaysInTheShopBetween.
	//
	// QUEDA UNO:
	//
	//   Vin              ShopStay.ReadVin() no es ReadVin() de la puerta, que lee la AGENDA. Es la
	//                    unica lectura que contesta algo PROPIO de la quinta global, asi que es con
	//                    la que se mide su AUSENCIA antes de la migracion (DEC-31), y ninguna de las
	//                    ocho lecturas de SH-4 la contesta.
	//
	// LAS DOS CONDICIONES QUE LO HACEN LEGAL SIGUEN SIENDO LAS MISMAS: vive en Pruebas/, es internal,
	// y NO REPITE NINGUNA LECTURA QUE LA PUERTA TENGA. ESTO NO ES UNA SEGUNDA PUERTA.
	internal static class AStayReadWithoutTheDoor
	{
		internal static string Vin(PerformanceV2 app)
		{
			using (RentedParameter rented = app.Actor.RentedParameters())
			{
				app.Actor.Using("@answer = stays.ReadVin();")
				.WithParameters(rented, p => {
					p[Parameter.Out, "answer", typeof(string)] = default;
				})
				.PerformQuery();

				return rented["answer"].GetValue<string>();
			}
		}
	}
}
