using System;
using Choreography.Theater;
using Puppeteer;

namespace DealershipCar.Dominio.Pruebas
{
	// LO QUE LA PUERTA TODAVIA NO PUEDE CONTESTAR DE LA QUINTA GLOBAL. Es la misma forma que
	// AProfileReadWithoutTheDoor tuvo en su primer dia y por la misma razon: T4 le agrego a la app la
	// CUARTA global de raiz, sus dos comandos y NINGUNA lectura, y nacio con once miembros. SH-3 le
	// agrega la QUINTA, sus tres comandos y NINGUNA lectura: las ocho son de SH-4.
	//
	// LAS DOS CONDICIONES QUE LO HACEN LEGAL SIGUEN SIENDO LAS MISMAS: vive en Pruebas/, es internal,
	// y NO REPITE NINGUNA LECTURA QUE LA PUERTA TENGA -- hoy la puerta no tiene ni un miembro que le
	// pregunte nada a 'stays'. ESTO NO ES UNA SEGUNDA PUERTA.
	//
	// Y SE ENCOGE EN SH-4, como se encogio el del perfil en T5 y en T6: el dia que la puerta conteste
	// una de estas preguntas, el script crudo que contesta lo mismo se borra, porque dejarlo al lado
	// seria la superficie de lectura paralela que DEC-31 y DEC-35 refusaron.
	//
	// POR QUE SON ESTOS CUATRO Y NO OTROS. Vin mide la AUSENCIA de la global antes de la migracion, y
	// es el unico que sigue vivo despues de SH-4 -- ShopStay.ReadVin() no es ReadVin() de la puerta,
	// que lee la AGENDA, y es la unica lectura que contesta algo PROPIO de este vehiculo (DEC-31).
	// Los otros tres son el ORACULO de los tres comandos: sin ellos, una prueba de que un hecho entro
	// solo podria afirmar que el journal crecio, que es lo mismo que decir que se escribio algo.
	// Days es el que ancla a lo RECIBIDO (E49): contesta los dias por valor, no un derivado de ellos.
	internal static class AStayReadWithoutTheDoor
	{
		internal static string Vin(PerformanceV2 app)
		{
			return AText(app, "@answer = stays.ReadVin();", p => { });
		}

		internal static bool Inside(PerformanceV2 app, DateTime asOf)
		{
			using (RentedParameter rented = app.Actor.RentedParameters())
			{
				app.Actor.Using("@answer = stays.IsInsideTheShopAt(@asOf);")
				.WithParameters(rented, p => {
					p[Parameter.Out, "answer", typeof(bool)] = default;
					p["asOf", typeof(DateTime)] = asOf;
				})
				.PerformQuery();

				return rented["answer"].GetValue<bool>();
			}
		}

		internal static string Says(PerformanceV2 app, DateTime asOf)
		{
			return AText(app, "@answer = stays.DescribeTheStayAt(@asOf);", p => {
				p["asOf", typeof(DateTime)] = asOf;
			});
		}

		internal static string[] Days(PerformanceV2 app, string fromDay, string toDay)
		{
			using (RentedParameter rented = app.Actor.RentedParameters())
			{
				app.Actor.Using("@answer = stays.DaysInTheShopBetween(@fromDay, @toDay);")
				.WithParameters(rented, p => {
					p[Parameter.Out, "answer", typeof(string[])] = default;
					p["fromDay", typeof(string)] = fromDay;
					p["toDay",   typeof(string)] = toDay;
				})
				.PerformQuery();

				return rented["answer"].GetValue<string[]>();
			}
		}

		private static string AText(PerformanceV2 app, string script, Action<Parameters> arguments)
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
