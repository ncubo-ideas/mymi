using System;
using DealershipCar.Mcp;
using DealershipCar.Mcp.Tools;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// EL CONJUNTO CERRADO DE LA FRONTERA DEL ASISTENTE ES EL DEL DOMINIO, MIEMBRO POR MIEMBRO.
	//
	// MaintenanceOperation existe porque un enum pone la lista de valores validos en el SCHEMA de la
	// tool -- un contrato que el cliente MCP no puede violar -- en vez de en la PROSA de su
	// descripcion, que la IA tiene que leer, entender y copiar bien. Con un string suelto,
	// "FluidChange", "fluidchange" y "fluid change" son tres cosas distintas para la misma.
	//
	// Lo que se paga es que el conjunto queda escrito DOS veces: aca y en el dominio. C25 ya habia
	// dejado anotada esta mejora y la condicion con que se acepta -- "un catalogo de nombres, no un
	// segundo conjunto cerrado, mas una prueba por reflexion que lo fije contra el enum del dominio
	// miembro por miembro". Esta es esa prueba. Va roja si al dominio se le agrega una clase, se le
	// quita una, o se le renombra una.
	[TestFixture]
	public sealed class TheClosedSetAtTheAssistantsEdgeIsTheDomainsOwn
	{
		private const string TheVin = "1HGCM82633A004352";

		[Test]
		public void TheOperationsTheAssistantCanAskAboutAreExactlyTheDomainsOperationKinds()
		{
			string[] atTheEdge = Enum.GetNames(typeof(MaintenanceOperation));
			string[] inTheDomain = Enum.GetNames(typeof(OperationKindCode));

			Assert.That(atTheEdge, Is.EqualTo(inTheDomain),
				"El conjunto de la frontera se desincronizo del dominio. No es cosmetico: una clase "
				+ "que el dominio tiene y la frontera no, es una pregunta que la persona no puede "
				+ "hacer; una que la frontera tiene y el dominio no, revienta en la puerta.");
		}

		[Test]
		public void EachOneCrossesTheDoorAsItsOwnName()
		{
			foreach (MaintenanceOperation each in TheOperationKind.All)
				Assert.That(each.WireName(), Is.EqualTo(each.ToString()));

			// Y ese nombre es el que la puerta acepta, letra por letra y con la misma caja: WireName
			// del App es estricto con las mayusculas a proposito (C25).
			Assert.That(MaintenanceOperation.FluidChange.WireName(), Is.EqualTo("FluidChange"));
			Assert.That(MaintenanceOperation.WearPartReplacement.WireName(),
				Is.EqualTo("WearPartReplacement"));
		}

		// LAS NUEVE SE MANEJAN DE PUNTA A PUNTA, que es la version con dientes de la prueba de
		// arriba: que los nombres coincidan no demuestra que la puerta los acepte. Es la misma
		// disciplina que EveryScriptOfTheDoorHasBeenRun -- un nombre mal escrito se descubre en
		// runtime, no al compilar (S5).
		[Test]
		public void EveryOperationTheAssistantCanAskAboutIsAnsweredByTheDoor()
		{
			using (var journal = new AScheduleOnARealJournal(TheVin, "Standard"))
			{
				journal.Api.SeedPlanOperation(
					"FluidChange", "TimeOrDistance", 365, 10000, "UnitA", true, false,
					new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc), 12000, "UnitA");

				var tools = new DealershipCarTools(new JournalReader(journal.Folder, ""));

				foreach (MaintenanceOperation each in TheOperationKind.All)
				{
					OperationStanding standing = tools.CheckOne(each);

					Assert.That(standing.HasData, Is.True);
					Assert.That(standing.Operation, Is.EqualTo(each.ToString()));
					Assert.That(standing.State, Is.Not.Empty,
						$"'{each}' no obtuvo estado de la puerta.");
					Assert.That(standing.Reason, Is.Not.Empty,
						$"'{each}' no obtuvo motivo de la puerta. El motivo lo compone el dominio "
						+ "(OUS-07) y es lo unico que distingue 'no agendada' de 'agendada y "
						+ "tranquila', porque las dos leen NotDue.");
				}

				// Y el tablero completo trae las nueve, incluidas las que este vehiculo no tiene: la
				// ausencia se dice, no se saltea.
				Assert.That(tools.Review().Operations, Has.Count.EqualTo(9));
			}
		}
	}
}
