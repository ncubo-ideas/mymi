using System;
using System.Collections.Generic;
using Concesionario.App;

namespace Concesionario.Consola
{
	/// <summary>
	/// Lo que la agencia registra sobre ESTE carro y lo que el fabricante publica sobre su garantia:
	/// la entrega, el sobre -- publicado dos veces --, las lecturas que trae el feed y el alcance
	/// sellado que las preguntas de cobertura necesitan. Todo entra por DealershipCarAPI, que es la
	/// unica puerta, y a la persona no se le pregunta ni un dato de su propio vehiculo.
	/// </summary>
	internal static class Warranty
	{
		/// <summary>
		/// La unidad en que la agencia publica el sobre y en que llegan las lecturas. NO es la
		/// primera de las dos: una unidad que nunca hubiera cruzado se leeria igual que la primera.
		/// </summary>
		internal const string TheUnit = "UnitB";

		/// <summary>El sistema por cuya cobertura pregunta esta caratula.</summary>
		internal const string TheSystem = "POWERTRAIN";

		/// <summary>La revision en que el fabricante publica el alcance de garantia.</summary>
		internal const string TheRevision = "WE2025-R1";

		/// <summary>El instante en que la agencia entrego el carro.</summary>
		internal static readonly DateTime TheDelivery = OnThe(2025, 3, 1);

		/// <summary>
		/// El odometro del dia de la entrega, que es el ancla desde donde se cuenta lo recorrido.
		/// </summary>
		internal const int TheOdometerAtDelivery = 120;

		/// <summary>El plazo del sobre, que las dos publicaciones dejan igual.</summary>
		internal const int TheMonths = 36;

		/// <summary>La distancia del PRIMER sobre.</summary>
		internal const int TheFirstDistance = 20000;

		/// <summary>La distancia del SEGUNDO, que es lo unico que la agencia cambia.</summary>
		internal const int TheSecondDistance = 60000;

		/// <summary>Cuando se publico el primer sobre.</summary>
		internal static readonly DateTime TheFirstPublication = OnThe(2025, 3, 1);

		/// <summary>Cuando se publico el segundo, que rige desde ahi hacia adelante.</summary>
		internal static readonly DateTime TheSecondPublication = OnThe(2026, 11, 1);

		/// <summary>El instante en que las dos mitades todavia tienen de sobra.</summary>
		internal static readonly DateTime TheInstantInside = OnThe(2025, 10, 15);

		/// <summary>El instante en que la de distancia ya se paso y la de tiempo todavia no.</summary>
		internal static readonly DateTime TheInstantPastTheDistanceHalf = OnThe(2026, 10, 15);

		/// <summary>El instante en que la de tiempo ya se paso y la de distancia todavia no.</summary>
		internal static readonly DateTime TheInstantPastTheTimeHalf = OnThe(2028, 6, 15);

		/// <summary>
		/// Un instante ANTERIOR a la entrega, con el que esta caratula muestra el unico hueco
		/// conocido de la frase del sobre. No es parte de los cuatro momentos.
		/// </summary>
		internal static readonly DateTime TheInstantBeforeTheDelivery = OnThe(2025, 1, 1);

		/// <summary>
		/// El odometro que el feed reporto ANTES de que el carro se entregara, que es lo que hace
		/// un vehiculo de demostracion. Es lo que deja al registro contradiciendose consigo mismo
		/// para los instantes anteriores a la entrega.
		/// </summary>
		internal const int TheOdometerBeforeTheDelivery = 150;

		/// <summary>El odometro que el feed ya habia traido para el instante de adentro.</summary>
		internal const int TheOdometerInside = 6000;

		/// <summary>El que ya paso la distancia del primer sobre.</summary>
		internal const int TheOdometerPastTheDistanceHalf = 22000;

		/// <summary>El que no llega a la distancia del segundo, cuando el plazo ya se paso.</summary>
		internal const int TheOdometerPastTheTimeHalf = 31000;

		/// <summary>
		/// Registra la entrega y publica el sobre, que son los dos hechos que la agencia dice sobre
		/// este carro en particular.
		/// </summary>
		internal static void TheDeliveryIsRecordedAndTheEnvelopeIsPublished(DealershipCarAPI api)
		{
			api.RecordDelivery(TheDelivery, TheOdometerAtDelivery, TheUnit);

			api.PublishWarrantyEnvelope(
				TheMonths, TheFirstDistance, TheUnit, TheFirstPublication);
		}

		/// <summary>
		/// Abre la revision del alcance, publica adentro el sistema y una exclusion, y la SELLA. El
		/// sistema entra sin limites propios, que es como se dice que rige el sobre del vehiculo.
		/// </summary>
		internal static void TheWarrantyScopeIsPublishedAndSealed(DealershipCarAPI api)
		{
			api.OpenRevision(TheRevision, TheFirstPublication);

			api.PublishCoveredSystem(TheRevision, TheSystem, 0, 0, TheUnit);

			api.PublishCoverageExclusion(
				TheRevision, TheSystem, "wear parts", "when the part is a consumable");

			api.SealRevision(TheRevision);
		}

		/// <summary>Las cuatro lecturas del odometro, en el orden en que el feed las trae.</summary>
		internal static IEnumerable<(int Value, DateTime ObservedAt, string Source)>
			TheReadingsTheFeedBrings()
		{
			yield return (TheOdometerBeforeTheDelivery, OnThe(2024, 12, 15), "Telemetry");
			yield return (TheOdometerInside, OnThe(2025, 10, 1), "Telemetry");
			yield return (TheOdometerPastTheDistanceHalf, OnThe(2026, 10, 1), "Telemetry");
			yield return (TheOdometerPastTheTimeHalf, OnThe(2028, 6, 1), "Telemetry");
		}

		/// <summary>Aplica esas cuatro lecturas al dominio.</summary>
		internal static void TheOdometerReportsArrive(DealershipCarAPI api)
		{
			foreach ((int value, DateTime observedAt, string source) in TheReadingsTheFeedBrings())
				api.RecordReading(value, TheUnit, observedAt, source);
		}

		/// <summary>
		/// Publica el sobre otra vez, con el mismo plazo y mas distancia. Rige desde su instante de
		/// publicacion hacia adelante y no alcanza a las respuestas ya dadas, que nombran cada una
		/// el sobre contra el que se midio.
		/// </summary>
		internal static void TheEnvelopeIsPublishedAgainWithMoreDistance(DealershipCarAPI api)
		{
			api.PublishWarrantyEnvelope(
				TheMonths, TheSecondDistance, TheUnit, TheSecondPublication);
		}

		private static DateTime OnThe(int year, int month, int day)
		{
			return new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);
		}
	}
}
