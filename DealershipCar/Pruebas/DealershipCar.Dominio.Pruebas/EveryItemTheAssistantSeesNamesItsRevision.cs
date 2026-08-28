using System;
using System.Collections.Generic;
using System.Text.Json;
using DealershipCar.Mcp;
using DealershipCar.Mcp.Tools;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LA MISMA LEY QUE DEFIENDE EveryItemOfASheetAnswersOnlyFromASealedRevision, UNA CAPA MAS AFUERA:
	// lo que el ASISTENTE ve. Que la puerta conteste bien no dice nada de lo que la caratula le
	// entrega, y entre las dos hay tres tools que eligen que preguntar y como decirlo.
	//
	// POR QUE UN BARRIDO Y NO LAS DOS QUE UNO SE ACUERDA DE ESCRIBIR. La caratula recorre SEIS
	// fluidos, TRES posiciones, el combustible, el alcance, la regla de taller, el intervalo y el
	// precio -- y cada uno se arma con su propia llamada. La que se pierde es siempre la del recorrido
	// que se escribio al final, y una hoja a la que le falta un item se lee igual de completa.
	//
	// LAS DOS LEYES QUE SE FIJAN: toda respuesta nombra la revision de la que salio, y lo no publicado
	// se DICE -- nunca cero, nunca un default, nunca un valor prestado de otro modelo o de otro ano.
	[TestFixture]
	public sealed class EveryItemTheAssistantSeesNamesItsRevision
	{
		private const string TheVin = "1HGCM82633A004352";

		private const string TheFirstRevision = "MY2025-R1";
		private const string TheSecondRevision = "MY2025-R2";

		private const string TheFirstOilGrade = "5W-30 synthetic";
		private const string TheSecondOilGrade = "0W-20 synthetic";

		private const int TheMilestone = 10000;

		private static readonly DateTime TheFirstPublication =
			new DateTime(2025, 8, 15, 0, 0, 0, DateTimeKind.Utc);
		private static readonly DateTime TheSecondPublication =
			new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);

		private AScheduleOnARealJournal journal;

		[SetUp]
		public void Setup()
		{
			journal = new AScheduleOnARealJournal(TheVin, "Standard");
		}

		[TearDown]
		public void TearDown()
		{
			journal.Dispose();
		}

		// LA FICHA ENTERA EN UN BORRADOR: el asistente no ve NADA de ella, y lo ve dicho. Es la ley de
		// que solo una revision sellada contesta, mirada desde donde la persona lee.
		[Test]
		public void ADraftHoldingTheWholeSheetLeavesTheAssistantWithNothingQuotable()
		{
			TheWholeSheetIsPublishedIntoTheFirstRevision();

			SpecificationSheet takes = TheAssistant().WhatMyCarTakes();

			Assert.That(takes.SpecificationKnown, Is.True);
			Assert.That(takes.CurrentRevision, Is.EqualTo(""));
			Assert.That(takes.Fluids, Has.Count.EqualTo(6),
				"Los seis siguen viniendo. La ausencia se dice, no se saltea.");

			foreach (KeyValuePair<string, bool> each in WhatIsPublished(takes))
				Assert.That(each.Value, Is.False, each.Key);

			foreach (KeyValuePair<string, string> each in TheRevisionOfEveryItem(takes))
				Assert.That(each.Value, Is.EqualTo(""), each.Key);

			foreach (KeyValuePair<string, string> each in TheSentenceOfEveryItem(takes))
				Assert.That(each.Value, Does.Contain("not published"), each.Key);

			// Y EL CONTENIDO NO SE ESCAPA POR NINGUN CAMPO. Se mide sobre la respuesta ENTERA
			// serializada y no sobre los campos que uno se acuerda de mirar: una hoja a medio cargar
			// citada por la puerta de atras es exactamente lo que el sello existe para impedir.
			Assert.That(EverythingTheAssistantIsHanded(), Does.Not.Contain(TheFirstOilGrade));
			Assert.That(EverythingTheAssistantIsHanded(), Does.Not.Contain("205/55R16"));
			Assert.That(EverythingTheAssistantIsHanded(), Does.Not.Contain("91 RON regular"));
		}

		// SELLADA: los items contestan Y CADA UNO NOMBRA LA REVISION -- adentro de la frase y en el
		// campo aparte, porque un lector se puede saltar el segundo.
		[Test]
		public void OnceSealedEveryItemTheAssistantSeesNamesTheRevisionItCameFrom()
		{
			TheWholeSheetIsPublishedIntoTheFirstRevision();
			journal.Api.SealRevision(TheFirstRevision);

			SpecificationSheet takes = TheAssistant().WhatMyCarTakes();

			Assert.That(takes.Model, Is.EqualTo(AScheduleOnARealJournal.TheModelAtDelivery));
			Assert.That(takes.CurrentRevision, Is.EqualTo(TheFirstRevision));
			Assert.That(takes.CurrentRevisionPublishedAt,
				Is.EqualTo("2025-08-15T00:00:00.0000000Z"));

			// Y LAS TRES DICEN DE QUE VEHICULO HABLAN. La ficha es del MODELO, pero la respuesta es
			// de un carro: con dos vehiculos en la carpeta, un Vin en blanco deja a la persona
			// leyendo la ficha del otro sin saberlo.
			Assert.That(takes.Vin, Is.EqualTo(TheVin));
			Assert.That(TheAssistant().DoesTheWarrantyCover("").Vin, Is.EqualTo(TheVin));
			Assert.That(
				TheAssistant().WhatTheServiceCosts(MaintenanceOperation.FluidChange).Vin,
				Is.EqualTo(TheVin));

			foreach (KeyValuePair<string, bool> each in WhatIsPublished(takes))
				Assert.That(each.Value, Is.True, each.Key);

			foreach (KeyValuePair<string, string> each in TheRevisionOfEveryItem(takes))
				Assert.That(each.Value, Is.EqualTo(TheFirstRevision), each.Key);

			foreach (KeyValuePair<string, string> each in TheSentenceOfEveryItem(takes))
				Assert.That(each.Value, Does.Contain("revision " + TheFirstRevision), each.Key);

			Assert.That(takes.Fuel.MinimumGrade, Is.EqualTo("91 RON regular"));
			Assert.That(takes.Fuel.PermittedGrades, Is.EqualTo(new[] { "95 RON Premium" }));
			Assert.That(takes.Fuel.ProhibitedGrades, Is.EqualTo(new[] { "E85 Flex" }));
		}

		// LO NO PUBLICADO CONVIVE CON LO PUBLICADO EN LA MISMA RESPUESTA, y las dos mitades se dicen.
		// Sin esto, una hoja donde todo esta publicado no distingue "contesta" de "contesta que si a
		// todo".
		[Test]
		public void TheItemsNobodyPublishedSayNotPublishedInTheSameAnswer()
		{
			TheWholeSheetIsPublishedIntoTheFirstRevision();
			journal.Api.SealRevision(TheFirstRevision);

			SpecificationSheet takes = TheAssistant().WhatMyCarTakes();

			foreach (PublishedFluid each in takes.Fluids)
			{
				Assert.That(each.Says, Does.StartWith(each.Kind), each.Kind);
				Assert.That(each.Published, Is.EqualTo(each.Kind == "EngineOil"), each.Kind);
				Assert.That(
					each.Revision, Is.EqualTo(each.Kind == "EngineOil" ? TheFirstRevision : ""),
					each.Kind);
				Assert.That(
					each.Says,
					each.Kind == "EngineOil"
						? Does.Contain("revision " + TheFirstRevision)
						: Does.Contain("not published"),
					each.Kind);
			}

			foreach (PublishedTyre each in takes.Tyres)
			{
				Assert.That(each.Says, Does.StartWith(each.Position), each.Position);
				Assert.That(each.Published, Is.EqualTo(each.Position == "Front"), each.Position);
				Assert.That(
					each.Revision, Is.EqualTo(each.Position == "Front" ? TheFirstRevision : ""),
					each.Position);
			}
		}

		// LA SEGUNDA REVISION MUEVE SOLO EL ACEITE, y el resto sigue diciendo de donde sale. Corregir
		// un item no borra los otros, y la revision vigente es la nueva aunque casi nada venga de ella.
		[Test]
		public void ASecondSealedRevisionMovesOnlyTheItemItRepublishedAtTheAssistantsEdgeToo()
		{
			TheWholeSheetIsPublishedIntoTheFirstRevision();
			journal.Api.SealRevision(TheFirstRevision);
			TheSecondRevisionChangesTheOilGrade();

			SpecificationSheet takes = TheAssistant().WhatMyCarTakes();

			Assert.That(takes.CurrentRevision, Is.EqualTo(TheSecondRevision));

			foreach (KeyValuePair<string, string> each in TheRevisionOfEveryItem(takes))
				Assert.That(
					each.Value,
					Is.EqualTo(each.Key == "aceite" ? TheSecondRevision : TheFirstRevision),
					each.Key);

			Assert.That(TheOilOf(takes).Says, Does.Contain(TheSecondOilGrade));
			Assert.That(TheOilOf(takes).Says, Does.Not.Contain(TheFirstOilGrade));
		}

		// SUSTITUIDA LA PRIMERA, se retira todo lo que la sucesora no republico -- y el asistente lo
		// ve como "no publicado", no como un campo en blanco. Medido despues del replay, porque la
		// linea de publicacion se vuelve a armar desde el disco.
		[Test]
		public void SupersedingWithdrawsFromTheAssistantEverythingTheSuccessorDidNotRepublish()
		{
			TheWholeSheetIsPublishedIntoTheFirstRevision();
			journal.Api.SealRevision(TheFirstRevision);
			TheSecondRevisionChangesTheOilGrade();
			journal.Api.SupersedeRevision(TheFirstRevision, TheSecondRevision);

			journal.CloseAndReopenFromTheJournal();

			SpecificationSheet takes = TheAssistant().WhatMyCarTakes();

			foreach (KeyValuePair<string, bool> each in WhatIsPublished(takes))
				Assert.That(each.Value, Is.EqualTo(each.Key == "aceite"), each.Key);

			foreach (KeyValuePair<string, string> each in TheSentenceOfEveryItem(takes))
				Assert.That(
					each.Value,
					each.Key == "aceite"
						? Does.Contain("revision " + TheSecondRevision)
						: Does.Contain("not published"),
					each.Key);

			Assert.That(takes.Fuel.MinimumGrade, Is.EqualTo(""));
			Assert.That(takes.Fuel.PermittedGrades, Is.Empty);
			Assert.That(takes.Fuel.ProhibitedGrades, Is.Empty);

			WarrantyScope scope = TheAssistant().DoesTheWarrantyCover("POWERTRAIN");

			Assert.That(scope.Published, Is.False);
			Assert.That(scope.CoveredSystems, Is.Empty);
			Assert.That(scope.Exclusions, Is.Empty);
			Assert.That(scope.SystemInTheCatalogue, Is.False,
				"Retirado el alcance, el catalogo tampoco conoce el codigo. Sigue siendo 'no nos lo "
				+ "dijeron' y no 'no lo cubre'.");

			ServicePricing pricing =
				TheAssistant().WhatTheServiceCosts(MaintenanceOperation.FluidChange, TheMilestone);

			Assert.That(pricing.IntervalPublished, Is.False);
			Assert.That(pricing.IntervalRevision, Is.EqualTo(""));
			Assert.That(pricing.Prices, Is.Empty);
		}

		// UN PRECIO NUNCA SALE SIN SU VIGENCIA, y la vigencia se dice respecto del instante en que se
		// pregunto. Los dos lados: uno vigente y uno vencido, en la misma ficha, para que "vigente" no
		// pueda ser una constante.
		[Test]
		public void APriceCarriesItsValidityAndSaysWhetherItIsInForceAtTheInstantItWasAsked()
		{
			journal.Api.OpenRevision(TheFirstRevision, TheFirstPublication);
			journal.Api.PublishServicePrice(
				TheFirstRevision, "FluidChange", TheMilestone, "UnitA", 89.90m, "USD",
				OnThe(2000, 1, 1), OnThe(2099, 12, 31));
			journal.Api.PublishServicePrice(
				TheFirstRevision, "FluidChange", 20000, "UnitA", 149.90m, "USD",
				OnThe(2000, 1, 1), OnThe(2001, 1, 1));
			journal.Api.SealRevision(TheFirstRevision);

			ServicePricing current =
				TheAssistant().WhatTheServiceCosts(MaintenanceOperation.FluidChange, TheMilestone);

			Assert.That(current.Prices, Has.Count.EqualTo(1));
			Assert.That(current.Prices[0].Says, Does.Contain("89.9 USD"));
			Assert.That(current.Prices[0].Says, Does.Contain("valid from"));
			Assert.That(current.Prices[0].Says, Does.Contain("that validity is in force"));
			Assert.That(current.Prices[0].Says, Does.Contain("revision " + TheFirstRevision));
			Assert.That(current.AskedAt, Is.Not.Empty,
				"Y se dice CONTRA QUE INSTANTE se juzgo. Sin el, 'vigente' no es respecto de nada.");

			ServicePricing stale =
				TheAssistant().WhatTheServiceCosts(MaintenanceOperation.FluidChange, 20000);

			Assert.That(stale.Prices, Has.Count.EqualTo(1));
			Assert.That(stale.Prices[0].Says, Does.Contain("that validity has passed"),
				"Un precio viejo mostrado como vigente es una promesa que la sucursal no hizo.");
		}

		// SIN HITO NO SE BUSCA NINGUN PRECIO, y se dice por que. Contestar "no hay precio publicado" a
		// quien no nombro un hito es reportar como ausencia del fabricante lo que es una pregunta a
		// medio hacer.
		[Test]
		public void WithNoMilestoneNamedNoPriceIsLookedUpAndTheReasonIsSaid()
		{
			journal.Api.OpenRevision(TheFirstRevision, TheFirstPublication);
			journal.Api.PublishServicePrice(
				TheFirstRevision, "FluidChange", TheMilestone, "UnitA", 89.90m, "USD",
				OnThe(2000, 1, 1), OnThe(2099, 12, 31));
			journal.Api.SealRevision(TheFirstRevision);

			ServicePricing nothingAsked =
				TheAssistant().WhatTheServiceCosts(MaintenanceOperation.FluidChange);

			Assert.That(nothingAsked.MilestoneAsked, Is.EqualTo(0));
			Assert.That(nothingAsked.Prices, Is.Empty);
			Assert.That(nothingAsked.Detail, Does.Contain("No milestone was named"));
			Assert.That(nothingAsked.Detail, Does.Not.Contain("Nothing is published at that"));

			// Y CON UN HITO QUE NADIE PUBLICO, LA OTRA RAZON. Las dos frases son distintas porque los
			// dos hechos lo son, y con una sola el asistente no puede saber cual le toco.
			ServicePricing wrongMilestone =
				TheAssistant().WhatTheServiceCosts(MaintenanceOperation.FluidChange, 15000);

			Assert.That(wrongMilestone.Prices, Is.Empty);
			Assert.That(wrongMilestone.Detail, Does.Contain("Nothing is published at that milestone"));
			Assert.That(wrongMilestone.Detail, Does.Not.Contain("No milestone was named"));
		}

		// EL INTERVALO PUBLICADO SE ANUNCIA Y SE DICE QUE NO SE PUEDE CITAR, que es la otra mitad de la
		// decision de T8: la frase del intervalo empareja lo publicado con la distancia que le queda a
		// ESTE vehiculo, y este registro no guarda el odometro.
		[Test]
		public void ThePublishedIntervalIsAnnouncedWithItsRevisionAndSaidToBeUnquotable()
		{
			journal.Api.OpenRevision(TheFirstRevision, TheFirstPublication);
			journal.Api.PublishServiceInterval(
				TheFirstRevision, "FluidChange", TheMilestone, "UnitA", 365);
			journal.Api.SealRevision(TheFirstRevision);

			ServicePricing pricing =
				TheAssistant().WhatTheServiceCosts(MaintenanceOperation.FluidChange);

			Assert.That(pricing.IntervalPublished, Is.True);
			Assert.That(pricing.IntervalRevision, Is.EqualTo(TheFirstRevision));
			Assert.That(pricing.Detail, Does.Contain("A service interval was published"));
			Assert.That(pricing.Detail, Does.Contain("does not keep the odometer"));

			// Y NO SE FILTRA LA FRASE por ningun campo, que es lo que separa "no se puede citar" de
			// "se cito igual".
			Assert.That(JsonSerializer.Serialize(pricing), Does.Not.Contain("serviced every"));
			Assert.That(JsonSerializer.Serialize(pricing), Does.Not.Contain("365 days"));

			// EL CONTROL: la puerta SI compone la frase cuando le llega la mitad del vehiculo. Lo que
			// falta no es la lectura, es de donde sacar la distancia que le queda.
			Assert.That(
				journal.Api.DescribeServiceInterval("FluidChange", 2000, "UnitA"),
				Does.Contain("2000 UnitA to go"));
		}

		// Y CUANDO NO HAY INTERVALO PUBLICADO NO SE ANUNCIA NADA. Sin esto, la frase de arriba podria
		// ser una constante.
		[Test]
		public void AndWithNoIntervalPublishedNothingIsAnnouncedAboutIt()
		{
			ServicePricing pricing =
				TheAssistant().WhatTheServiceCosts(MaintenanceOperation.FluidChange);

			Assert.That(pricing.IntervalPublished, Is.False);
			Assert.That(pricing.Detail, Does.Not.Contain("A service interval was published"));
		}

		private ModelSpecificationTools TheAssistant()
		{
			return new ModelSpecificationTools(new JournalReader(journal.Folder, ""));
		}

		private string EverythingTheAssistantIsHanded()
		{
			ModelSpecificationTools assistant = TheAssistant();

			return JsonSerializer.Serialize(assistant.WhatMyCarTakes())
				+ JsonSerializer.Serialize(assistant.DoesTheWarrantyCover("POWERTRAIN"))
				+ JsonSerializer.Serialize(
					assistant.WhatTheServiceCosts(MaintenanceOperation.FluidChange, TheMilestone));
		}

		private static PublishedFluid TheOilOf(SpecificationSheet takes)
		{
			foreach (PublishedFluid each in takes.Fluids)
				if (each.Kind == "EngineOil") return each;

			throw new InvalidOperationException("La hoja no trajo el aceite.");
		}

		private static PublishedTyre TheFrontOf(SpecificationSheet takes)
		{
			foreach (PublishedTyre each in takes.Tyres)
				if (each.Position == "Front") return each;

			throw new InvalidOperationException("La hoja no trajo la llanta delantera.");
		}

		// LOS SIETE ITEMS COMO LOS VE EL ASISTENTE. Tres de ellos llegan por otra tool, y por eso el
		// barrido las llama a las tres: la ficha no es lo que una tool contesta, es lo que la persona
		// puede preguntar.
		private Dictionary<string, bool> WhatIsPublished(SpecificationSheet takes)
		{
			WarrantyScope scope = TheAssistant().DoesTheWarrantyCover("POWERTRAIN");
			ServicePricing pricing =
				TheAssistant().WhatTheServiceCosts(MaintenanceOperation.FluidChange, TheMilestone);

			return new Dictionary<string, bool>
			{
				{ "aceite", TheOilOf(takes).Published },
				{ "llanta", TheFrontOf(takes).Published },
				{ "combustible", takes.Fuel.Published },
				{ "intervalo", pricing.IntervalPublished },
				{ "precio", pricing.Prices.Count > 0 },
				{ "garantia", scope.Published },
				{ "regla de taller", scope.ServicingPolicyPublished },
			};
		}

		private Dictionary<string, string> TheRevisionOfEveryItem(SpecificationSheet takes)
		{
			WarrantyScope scope = TheAssistant().DoesTheWarrantyCover("POWERTRAIN");
			ServicePricing pricing =
				TheAssistant().WhatTheServiceCosts(MaintenanceOperation.FluidChange, TheMilestone);

			return new Dictionary<string, string>
			{
				{ "aceite", TheOilOf(takes).Revision },
				{ "llanta", TheFrontOf(takes).Revision },
				{ "combustible", takes.Fuel.Revision },
				{ "intervalo", pricing.IntervalRevision },
				{ "precio", pricing.Prices.Count > 0 ? pricing.Prices[0].Revision : "" },
				{ "garantia", scope.Revision },
				{ "regla de taller", scope.ServicingPolicyRevision },
			};
		}

		// LAS FRASES QUE LA PERSONA LEE, y son CUATRO de los siete items. El intervalo y la regla de
		// taller no traen frase a esta frontera, y el precio solo la trae cuando hay uno publicado --
		// su ausencia se afirma por Prices vacio, arriba. Fabricar aca una frase para el hueco seria
		// comparar el barrido contra si mismo.
		private Dictionary<string, string> TheSentenceOfEveryItem(SpecificationSheet takes)
		{
			return new Dictionary<string, string>
			{
				{ "aceite", TheOilOf(takes).Says },
				{ "llanta", TheFrontOf(takes).Says },
				{ "combustible", takes.Fuel.Says },
				{ "garantia", TheAssistant().DoesTheWarrantyCover("POWERTRAIN").Says },
			};
		}

		// La misma hoja que publica la caratula de consola, en el mismo orden.
		private void TheWholeSheetIsPublishedIntoTheFirstRevision()
		{
			journal.Api.OpenRevision(TheFirstRevision, TheFirstPublication);

			journal.Api.PublishFluidSpecification(
				TheFirstRevision, "EngineOil", TheFirstOilGrade, 4.2m, "UnitA",
				TheMilestone, "UnitA", 365);

			journal.Api.PublishTyreSpecification(
				TheFirstRevision, "Front", "205/55R16", 2.4m, "UnitA", "94V");

			journal.Api.PublishFuelGrade(TheFirstRevision, "95 RON Premium", "Permitted");
			journal.Api.PublishFuelGrade(TheFirstRevision, "91 RON regular", "Minimum");
			journal.Api.PublishFuelGrade(TheFirstRevision, "E85 Flex", "Prohibited");

			journal.Api.PublishServiceInterval(
				TheFirstRevision, "FluidChange", TheMilestone, "UnitA", 365);

			journal.Api.PublishServicePrice(
				TheFirstRevision, "FluidChange", TheMilestone, "UnitA", 89.90m, "USD",
				OnThe(2000, 1, 1), OnThe(2099, 12, 31));

			journal.Api.PublishCoveredSystem(TheFirstRevision, "POWERTRAIN", 0, 0, "UnitA");

			journal.Api.PublishCoverageExclusion(
				TheFirstRevision, "POWERTRAIN", "wear parts", "when the part is a consumable");

			journal.Api.PublishServicingPolicy(
				TheFirstRevision,
				"service may be done at any qualified workshop",
				"keep the invoices and the parts records");
		}

		private void TheSecondRevisionChangesTheOilGrade()
		{
			journal.Api.OpenRevision(TheSecondRevision, TheSecondPublication);

			journal.Api.PublishFluidSpecification(
				TheSecondRevision, "EngineOil", TheSecondOilGrade, 4.2m, "UnitA",
				TheMilestone, "UnitA", 365);

			journal.Api.SealRevision(TheSecondRevision);
		}

		private static DateTime OnThe(int year, int month, int day)
		{
			return new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);
		}
	}
}
