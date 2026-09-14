using System;
using Concesionario.Mcp;
using Concesionario.Wasm.Parts;
using Concesionario.Mcp.Tools;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
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
	//
	// T8 TRAJO TRES CONJUNTOS MAS a esta frontera, y NINGUNO es un enum: los fluidos, las posiciones
	// de llanta y las dos unidades de distancia solo se RECORREN aca -- el asistente no elige uno --
	// asi que lo que compra un enum no se cobra y alcanza un catalogo de nombres. La prueba por
	// reflexion es la misma y sigue siendo la condicion con que se acepta la segunda escritura.
	[TestFixture]
	public sealed class TheClosedSetAtTheAssistantsEdgeIsTheDomainsOwn
	{
		private const string TheVin = "1HGCM82633A004352";

		private static readonly DateTime ThePublication =
			new DateTime(2025, 8, 15, 0, 0, 0, DateTimeKind.Utc);

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

		// LOS TRES CONJUNTOS DE LA FICHA, uno por assert, contra el enum del dominio miembro por
		// miembro y EN ORDEN. El orden importa: la ficha se imprime en el orden del canon y una
		// permutacion silenciosa cambiaria de que habla la primera linea de la respuesta.
		[Test]
		public void TheFluidsTheSheetSpeaksAreExactlyTheDomainsFluidKinds()
		{
			Assert.That(
				TheClosedSetsOfTheSheet.Fluids,
				Is.EqualTo(Enum.GetNames(typeof(FluidKindCode))),
				"El catalogo de fluidos de la frontera se desincronizo del dominio. Uno que el "
				+ "dominio tiene y la frontera no es un fluido sobre el que la persona nunca ve lo "
				+ "publicado; uno que la frontera tiene y el dominio no revienta en la puerta.");
		}

		[Test]
		public void TheTyrePositionsTheSheetSpeaksAreExactlyTheDomainsTyrePositions()
		{
			Assert.That(
				TheClosedSetsOfTheSheet.TyrePositions,
				Is.EqualTo(Enum.GetNames(typeof(TyrePositionCode))),
				"El catalogo de posiciones de llanta se desincronizo del dominio.");
		}

		[Test]
		public void TheDistanceUnitsTheSheetSpeaksAreExactlyTheDomainsDistanceUnits()
		{
			Assert.That(
				TheClosedSetsOfTheSheet.DistanceUnits,
				Is.EqualTo(Enum.GetNames(typeof(DistanceUnitCode))),
				"El catalogo de unidades de distancia se desincronizo del dominio. Una unidad que "
				+ "falta aca es un precio publicado que la tool nunca encuentra y reporta como no "
				+ "publicado, que es la respuesta equivocada mas cara de esa tool.");
		}

		// LOS SEIS FLUIDOS Y LAS TRES POSICIONES SE MANEJAN DE PUNTA A PUNTA, que es la version con
		// dientes: que los nombres coincidan no demuestra que la puerta los acepte, y un nombre mal
		// escrito se descubre en runtime y no al compilar (S5).
		[Test]
		public void EveryFluidAndEveryTyrePositionTheSheetSpeaksIsAnsweredByTheDoor()
		{
			using (var journal = new AScheduleOnARealJournal(TheVin, "Standard"))
			{
				ARevisionWithOneFluidAndOneTyre(journal);

				SpecificationSheet sheet =
					new ModelSpecificationTools(new JournalReader(journal.Folder, ""))
						.WhatMyCarTakes();

				Assert.That(sheet.Fluids, Has.Count.EqualTo(6));
				Assert.That(sheet.Tyres, Has.Count.EqualTo(3));

				// Y LA FRASE ES DE SU PROPIO ITEM, que es lo que "no vino vacia" no dice. Toda
				// respuesta de la ficha empieza por el nombre de aquello de lo que habla -- lo
				// publicado y lo no publicado -- asi que una caratula que le pasara la MISMA clave a
				// las seis lecturas se ve aca y en ningun otro lado. Medido: ese defecto dejaba la
				// suite entera verde.
				foreach (PublishedFluid each in sheet.Fluids)
				{
					Assert.That(each.Says, Does.StartWith(HowACanonNameReads.AFluid(TheCatalogueOfTheScreen.InTheLanguageOfTheRecord, each.Kind)),
						$"'{each.Kind}' contesto con la frase de otro fluido.");
					Assert.That(each.Published, Is.EqualTo(each.Kind == "EngineOil"),
						$"'{each.Kind}': solo el aceite se publico en esta revision.");
				}

				foreach (PublishedTyre each in sheet.Tyres)
				{
					Assert.That(each.Says, Does.StartWith("Llanta " + HowACanonNameReads.ATyre(TheCatalogueOfTheScreen.InTheLanguageOfTheRecord, each.Position).ToLowerInvariant()),
						$"'{each.Position}' contesto con la frase de otra posicion.");
					Assert.That(each.Published, Is.EqualTo(each.Position == "Front"),
						$"'{each.Position}': solo la delantera se publico en esta revision.");
				}
			}
		}

		// LAS DOS UNIDADES CRUZAN, y hace falta comprobarlo con la SEGUNDA: aca se publica en UnitB a
		// proposito, y lo que vuelve tiene que decir la unidad en que se publico -- no se convierte
		// ninguna unidad a ninguna otra en ningun punto.
		//
		// P-12 CAMBIO EL DISCRIMINADOR Y HAY QUE DECIRLO. Antes la tool preguntaba por el hito en cada
		// una de las dos unidades, y este renglon existia porque una tool que solo mirara UnitA pasaria
		// cualquier prueba escrita con UnitA. Ahora la tool no pregunta por ninguna unidad: ENUMERA los
		// precios publicados, asi que no puede perder una unidad y lo que queda por comprobar es que la
		// que vuelve es la publicada.
		//
		// Y ESTA SUPERFICIE PERDIO SU UNICO CODIGO DE WIRE. MilestoneUnit -- 'UnitB', el nombre del
		// miembro -- se fue con el parametro, y era el unico sitio del borde del asistente donde una
		// unidad de distancia cruzaba como CODIGO: en las demas cruza dentro de la frase, como la lee
		// una persona. Asi que la unidad ya no se dice dos veces aca, se dice una.
		[Test]
		public void APricePublishedInTheSecondUnitIsFoundAndAnsweredInThatUnit()
		{
			using (var journal = new AScheduleOnARealJournal(TheVin, "Standard"))
			{
				journal.Api.OpenRevision("MY2019-R1", ThePublication);
				journal.Api.PublishServicePrice(
					"MY2019-R1", "FluidChange", 6000, "UnitB", 74.50m, "USD",
					ThePublication, new DateTime(2099, 12, 31, 0, 0, 0, DateTimeKind.Utc));
				journal.Api.SealRevision("MY2019-R1");

				ServicePricing pricing =
					new ModelSpecificationTools(new JournalReader(journal.Folder, ""))
						.WhatTheServiceCosts(MaintenanceOperation.FluidChange);

				Assert.That(pricing.Prices, Has.Count.EqualTo(1));

				Assert.That(pricing.Prices[0].Milestone, Is.EqualTo("6000 mi"),
					"El hito vuelve en la unidad en que se publico. Contestar km aca seria haber "
					+ "convertido, que es lo unico que este dominio nunca hace.");
				Assert.That(
					pricing.Prices[0].Milestone,
					Does.Not.Contain("UnitA").And.Not.Contain("UnitB"),
					"Y se dice como la lee una persona, no como la nombra el wire: el codigo se fue "
					+ "con el parametro que P-12 retiro.");

				Assert.That(pricing.Prices[0].Says, Does.Contain("6000 mi"));
				Assert.That(pricing.Prices[0].Says, Does.Not.Contain("km"));
				Assert.That(pricing.Prices[0].Revision, Is.EqualTo("MY2019-R1"));

				// Y NINGUN CAMPO DE LA RESPUESTA ENTERA TRAE UN CODIGO DE UNIDAD. Sin este renglon,
				// un campo nuevo podria reintroducir el mapeo inverso sin que nadie lo mire.
				Assert.That(
					System.Text.Json.JsonSerializer.Serialize(pricing),
					Does.Not.Contain("UnitA").And.Not.Contain("UnitB"));
			}
		}

		private static void ARevisionWithOneFluidAndOneTyre(AScheduleOnARealJournal journal)
		{
			journal.Api.OpenRevision("MY2019-R1", ThePublication);

			journal.Api.PublishFluidSpecification(
				"MY2019-R1", "EngineOil", "5W-30 synthetic", 4.2m, "UnitA", 10000, "UnitA", 365);

			journal.Api.PublishTyreSpecification(
				"MY2019-R1", "Front", "205/55R16", 2.4m, "UnitA", "94V");

			journal.Api.SealRevision("MY2019-R1");
		}
	}
}
