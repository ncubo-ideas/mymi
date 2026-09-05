using System;
using DealershipCar.App;

namespace DealershipCar.Consola
{
	/// <summary>
	/// Lo que el fabricante publica sobre el modelo de este vehiculo, en dos revisiones: la primera
	/// con los siete items de la ficha, y la segunda que solo cambia el grado del aceite. Todo entra
	/// por DealershipCarAPI, que es la unica puerta.
	/// </summary>
	internal static class SpecSheet
	{
		/// <summary>La revision que trae la ficha entera.</summary>
		internal const string TheFirstRevision = "MY2019-R1";

		/// <summary>La revision que solo republica el aceite y sustituye a la primera.</summary>
		internal const string TheSecondRevision = "MY2019-R2";

		/// <summary>La clase de fluido por la que la caratula pregunta.</summary>
		internal const string TheFluidKind = "EngineOil";

		/// <summary>La posicion de llanta por la que la caratula pregunta.</summary>
		internal const string TheTyrePosition = "Front";

		/// <summary>La operacion cuyo intervalo y cuyo precio se publican.</summary>
		internal const string TheOperation = "FluidChange";

		/// <summary>El sistema del alcance de garantia por el que la caratula pregunta.</summary>
		internal const string TheSystem = "POWERTRAIN";

		/// <summary>El grado de aceite que publica la primera revision.</summary>
		internal const string TheFirstOilGrade = "5W-30 synthetic";

		/// <summary>El grado de aceite que publica la segunda, y que es todo lo que cambia.</summary>
		internal const string TheSecondOilGrade = "0W-20 synthetic";

		/// <summary>El hito de distancia en que se publico el precio del servicio.</summary>
		internal const int TheMilestone = 10000;

		/// <summary>El instante en que el fabricante publico la primera revision.</summary>
		internal static readonly DateTime TheFirstPublication = OnThe(2025, 8, 15);

		/// <summary>El instante en que el fabricante publico la segunda.</summary>
		internal static readonly DateTime TheSecondPublication = OnThe(2026, 2, 1);

		/// <summary>El instante en que esta caratula hace todas sus preguntas.</summary>
		internal static readonly DateTime TheDayTheVehicleAsks = OnThe(2026, 9, 1);

		/// <summary>La lectura de odometro con la que este vehiculo pregunta.</summary>
		internal const int TheOdometerToday = 20000;

		/// <summary>Lo que le queda a este vehiculo para el proximo cambio de fluido.</summary>
		internal const int TheDistanceRemaining = 2000;

		/// <summary>La mitad de plazo del sobre de garantia de este vehiculo.</summary>
		internal const int TheEnvelopeMonths = 60;

		/// <summary>La mitad de distancia del sobre de garantia de este vehiculo.</summary>
		internal const int TheEnvelopeDistance = 100000;

		/// <summary>Los servicios en otro taller de los que este vehiculo tiene constancia.</summary>
		internal const int TheExternalServicesWithEvidence = 2;

		/// <summary>Los servicios en otro taller de los que no la tiene.</summary>
		internal const int TheExternalServicesWithoutEvidence = 1;

		/// <summary>Las operaciones de mantenimiento que este vehiculo dejo pasar.</summary>
		internal const int TheMissedMaintenance = 0;

		/// <summary>La unidad en que se publico y en que pregunta este vehiculo.</summary>
		internal const string UnitA = "UnitA";

		/// <summary>
		/// ABRE la primera revision y publica los siete items adentro, SIN sellarla. Los grados de
		/// combustible entran con las tres posturas y el minimo no va primero.
		/// </summary>
		internal static void TheWholeSheetIsPublishedIntoTheFirstRevision(DealershipCarAPI api)
		{
			api.OpenRevision(TheFirstRevision, TheFirstPublication);

			api.PublishFluidSpecification(
				TheFirstRevision, TheFluidKind, TheFirstOilGrade, 4.2m, UnitA,
				TheMilestone, UnitA, 365);

			api.PublishTyreSpecification(
				TheFirstRevision, TheTyrePosition, "205/55R16", 2.4m, UnitA, "94V");

			api.PublishFuelGrade(TheFirstRevision, "95 RON Premium", "Permitted");
			api.PublishFuelGrade(TheFirstRevision, "91 RON regular", "Minimum");
			api.PublishFuelGrade(TheFirstRevision, "E85 Flex", "Prohibited");

			api.PublishServiceInterval(TheFirstRevision, TheOperation, TheMilestone, UnitA, 365);

			api.PublishServicePrice(
				TheFirstRevision, TheOperation, TheMilestone, UnitA, 89.90m, "USD",
				TheFirstPublication, OnThe(2026, 12, 31));

			api.PublishCoveredSystem(TheFirstRevision, TheSystem, 0, 0, UnitA);

			api.PublishCoverageExclusion(
				TheFirstRevision, TheSystem, "wear parts", "when the part is a consumable");

			api.PublishServicingPolicy(
				TheFirstRevision,
				"service may be done at any qualified workshop",
				"keep the invoices and the parts records");
		}

		/// <summary>SELLA la primera revision, que es lo unico que la hace contestar.</summary>
		internal static void TheFirstRevisionIsSealed(DealershipCarAPI api)
		{
			api.SealRevision(TheFirstRevision);
		}

		/// <summary>
		/// Abre la segunda revision, publica adentro el grado de aceite nuevo y la sella. Es lo
		/// unico que republica.
		/// </summary>
		internal static void TheSecondRevisionChangesTheOilGrade(DealershipCarAPI api)
		{
			api.OpenRevision(TheSecondRevision, TheSecondPublication);

			api.PublishFluidSpecification(
				TheSecondRevision, TheFluidKind, TheSecondOilGrade, 4.2m, UnitA,
				TheMilestone, UnitA, 365);

			api.SealRevision(TheSecondRevision);
		}

		/// <summary>SUSTITUYE la primera revision por la segunda.</summary>
		internal static void TheFirstRevisionIsSuperseded(DealershipCarAPI api)
		{
			api.SupersedeRevision(TheFirstRevision, TheSecondRevision);
		}

		private static DateTime OnThe(int year, int month, int day)
		{
			return new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);
		}
	}
}
