using System;
using System.IO;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// OAGG-03: una MaintenanceSchedule POR VIN, con su propio journal. En este modulo eso no es una
	// coleccion de vehiculos adentro de un actor: es que el nombre del actor LLEVA el VIN, asi que
	// un segundo vehiculo es un segundo actor y un segundo journal. Nada de esta rebanada itera
	// vehiculos, y esta prueba es la razon por la que no hace falta que lo haga.
	//
	// SIN ESTO, la peor version del defecto es silenciosa: con un nombre de actor fijo, dos
	// vehiculos sobre la misma carpeta comparten journal. El segundo encuentra la semilla ya
	// aplicada -- upgrade es una-vez-por-actor -- asi que no revienta ni avisa: simplemente
	// contesta la agenda del PRIMER vehiculo, con sus lecturas y sus operaciones.
	[TestFixture]
	public sealed class EachVehicleGetsItsOwnActorAndItsOwnJournal
	{
		private const string TheOwnersVin = "1HGCM82633A004352";
		private const string TheOtherVin = "5YJ3E1EA7HF000337";

		private static readonly DateTime Delivery =
			new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);

		[Test]
		public void TwoVehiclesOnTheSameFolderDoNotShareASchedule()
		{
			string shared = Path.Combine(
				Path.GetTempPath(), "dealership-car-nivel-b", Guid.NewGuid().ToString("N"));

			AScheduleOnARealJournal owners = null;
			AScheduleOnARealJournal other = null;

			try
			{
				owners = new AScheduleOnARealJournal(TheOwnersVin, "Standard", shared);
				owners.Api.SeedPlanOperation(
					"FluidChange", "DistanceOnly", 0, 10000, "UnitA",
					true, false,
					Delivery, 30000, "UnitA");

				other = new AScheduleOnARealJournal(TheOtherVin, "Commercial", shared);

				Assert.That(other.Api.ReadVin(), Is.EqualTo(TheOtherVin),
					"Cada uno contesta SU vin. Con un nombre de actor fijo, este segundo habria "
					+ "rehidratado el journal del primero y contestado el vin ajeno.");
				Assert.That(
					other.Api.ReadDueReason("FluidChange"), Does.Contain("no scheduled"),
					"Y no hereda sus operaciones: el plan que sembro el primer vehiculo no es el "
					+ "plan del segundo.");

				Assert.That(owners.Api.ReadVin(), Is.EqualTo(TheOwnersVin));
				Assert.That(
					owners.Api.ReadDueReason("FluidChange"), Does.Not.Contain("no scheduled"),
					"Y el primero conserva la suya: abrir el segundo no le toco nada.");
			}
			finally
			{
				if (other != null) other.Dispose();
				if (owners != null) owners.Dispose();
			}
		}

		// El nombre del actor se compone del VIN, y un VIN en blanco no puede nombrar un actor. Se
		// rechaza en la frontera, antes de que exista carpeta ni journal.
		[Test]
		public void AVehicleWithoutAVinCannotOpenAJournalAtAll()
		{
			Assert.Throws<ArgumentNullException>(
				() => DealershipCar.App.DealershipCarApp.TheActorFor(null));
			Assert.Throws<ArgumentException>(
				() => DealershipCar.App.DealershipCarApp.TheActorFor("   "));

			Assert.That(
				DealershipCar.App.DealershipCarApp.TheActorFor(TheOwnersVin),
				Is.EqualTo("maintenance-" + TheOwnersVin));
		}
	}
}
