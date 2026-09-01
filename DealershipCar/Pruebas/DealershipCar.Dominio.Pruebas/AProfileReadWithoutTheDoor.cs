using System;
using Choreography.Theater;
using Puppeteer;

namespace DealershipCar.Dominio.Pruebas
{
	// LEER EL PERFIL CUANDO LA PUERTA TODAVIA NO TIENE LECTURA. T4 le agrega a la app la cuarta
	// global de raiz y los DOS COMANDOS, y ninguna lectura: las once sin componer son de T5 y las
	// seis compuestas de T6. Asi que lo unico que puede observar que los comandos entraron es un
	// script crudo contra el actor -- que es exactamente lo que
	// TheThirdGlobalReachesAJournalThatHasTheOtherTwo hacia a mano mientras la especificacion no
	// tenia ninguna lectura en la puerta, antes de que T6 le pusiera ReadModel.
	//
	// ESTO NO ES UNA SEGUNDA PUERTA. Vive en Pruebas/, es internal, y desaparece en T5 cuando cada
	// uno de estos scripts tenga su miembro en DealershipCarAPI. Mientras exista, es la unica forma
	// de medir la AUSENCIA de la cuarta global -- el falsificador que S2 midio en su fila 3 -- y sin
	// esa medicion el fixture de la migracion queda verde de cualquier manera.
	//
	// LA UNIDAD DEL SOBRE NO SE LEE ACA, a proposito. VehicleProfile.EnvelopeUnit() devuelve un
	// DistanceUnitCode, y un parametro Out de tipo enum no esta medido en este modulo; la unidad se
	// observa por el NOMBRE que trae la frase publicada y por ReadDeliveryUnit(), que son cadenas.
	// El codigo es para el camino entre globales de T6, no para nadie de afuera.
	internal static class AProfileReadWithoutTheDoor
	{
		internal static string Vin(PerformanceV2 app)
		{
			return AText(app, "@answer = profile.ReadVin();");
		}

		internal static bool IsWarrantyEnvelopePublished(PerformanceV2 app)
		{
			return ATruth(app, "@answer = profile.IsWarrantyEnvelopePublished();");
		}

		internal static bool HasDeliveryBeenRecorded(PerformanceV2 app)
		{
			return ATruth(app, "@answer = profile.HasDeliveryBeenRecorded();");
		}

		internal static string DescribePublishedWarrantyEnvelope(PerformanceV2 app)
		{
			return AText(app, "@answer = profile.DescribePublishedWarrantyEnvelope();");
		}

		internal static int EnvelopeMonths(PerformanceV2 app)
		{
			return ANumber(app, "@answer = profile.EnvelopeMonths();");
		}

		internal static int EnvelopeDistance(PerformanceV2 app)
		{
			return ANumber(app, "@answer = profile.EnvelopeDistance();");
		}

		internal static DateTime ReadEnvelopePublishedAt(PerformanceV2 app)
		{
			return AnInstant(app, "@answer = profile.ReadEnvelopePublishedAt();");
		}

		internal static DateTime ReadDeliveredAt(PerformanceV2 app)
		{
			return AnInstant(app, "@answer = profile.ReadDeliveredAt();");
		}

		internal static int ReadDeliveryReading(PerformanceV2 app)
		{
			return ANumber(app, "@answer = profile.ReadDeliveryReading();");
		}

		internal static string ReadDeliveryUnit(PerformanceV2 app)
		{
			return AText(app, "@answer = profile.ReadDeliveryUnit();");
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

		private static bool ATruth(PerformanceV2 app, string script)
		{
			using (RentedParameter rented = app.Actor.RentedParameters())
			{
				app.Actor.Using(script)
				.WithParameters(rented, p => {
					p[Parameter.Out, "answer", typeof(bool)] = default;
				})
				.PerformQuery();

				return rented["answer"].GetValue<bool>();
			}
		}

		private static int ANumber(PerformanceV2 app, string script)
		{
			using (RentedParameter rented = app.Actor.RentedParameters())
			{
				app.Actor.Using(script)
				.WithParameters(rented, p => {
					p[Parameter.Out, "answer", typeof(int)] = default;
				})
				.PerformQuery();

				return rented["answer"].GetValue<int>();
			}
		}

		private static DateTime AnInstant(PerformanceV2 app, string script)
		{
			using (RentedParameter rented = app.Actor.RentedParameters())
			{
				app.Actor.Using(script)
				.WithParameters(rented, p => {
					p[Parameter.Out, "answer", typeof(DateTime)] = default;
				})
				.PerformQuery();

				return DateTime.SpecifyKind(
					rented["answer"].GetValue<DateTime>(), DateTimeKind.Utc);
			}
		}
	}
}
