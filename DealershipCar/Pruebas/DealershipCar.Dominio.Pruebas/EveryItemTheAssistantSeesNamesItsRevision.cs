using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using DealershipCar.Mcp;
using DealershipCar.Mcp.Tools;
using DealershipCar.Wasm.Parts;
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
				Assert.That(each.Value, Does.Contain("no se ha publicado nada"), each.Key);

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
				Assert.That(each.Value, Does.Contain("revisión " + TheFirstRevision), each.Key);

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
				Assert.That(each.Says, Does.StartWith(HowACanonNameReads.AFluid(each.Kind)), each.Kind);
				Assert.That(each.Published, Is.EqualTo(each.Kind == "EngineOil"), each.Kind);
				Assert.That(
					each.Revision, Is.EqualTo(each.Kind == "EngineOil" ? TheFirstRevision : ""),
					each.Kind);
				Assert.That(
					each.Says,
					each.Kind == "EngineOil"
						? Does.Contain("revisión " + TheFirstRevision)
						: Does.Contain("no se ha publicado nada"),
					each.Kind);
			}

			foreach (PublishedTyre each in takes.Tyres)
			{
				Assert.That(
					each.Says,
					Does.StartWith(TheTyreSubjectFor(each.Position)),
					each.Position);
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
						? Does.Contain("revisión " + TheSecondRevision)
						: Does.Contain("no se ha publicado nada"),
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
				TheAssistant().WhatTheServiceCosts(MaintenanceOperation.FluidChange);

			Assert.That(pricing.IntervalPublished, Is.False);
			Assert.That(pricing.IntervalRevision, Is.EqualTo(""));
			Assert.That(pricing.Prices, Is.Empty);
		}

		// UN PRECIO NUNCA SALE SIN SU VIGENCIA, y la vigencia se dice respecto del instante en que se
		// pregunto. Los dos lados -- uno vigente y uno vencido -- Y AHORA EN LA MISMA RESPUESTA,
		// porque P-12 hizo que la tool enumere todos los hitos de la operacion en vez de buscar el que
		// alguien nombre. Que los dos convivan es lo que impide que "vigente" sea una constante.
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

			ServicePricing pricing =
				TheAssistant().WhatTheServiceCosts(MaintenanceOperation.FluidChange);

			Assert.That(pricing.Prices, Has.Count.EqualTo(2),
				"LOS DOS HITOS, sin que nadie nombre ninguno. Ahi esta P-12 entero: la tool enumera "
				+ "lo que el fabricante publico en vez de pedirle un numero a quien pregunta.");

			PublishedPrice current = ThePriceAt(pricing, "10000 km");
			PublishedPrice stale = ThePriceAt(pricing, "20000 km");

			Assert.That(current.Says, Does.StartWith("Cambio de fluido a los 10000 km: 89.9 USD"),
				"LA FORMA FUERTE: el hito y el monto en una frase con su conectiva, asi que una fila "
				+ "armada con el hito de una y el monto de otra se ve.");
			Assert.That(current.Says, Does.Contain("vigente del"));
			Assert.That(current.Says, Does.Contain("esa vigencia está rigiendo"));
			Assert.That(current.Says, Does.Contain("revisión " + TheFirstRevision));
			Assert.That(current.Revision, Is.EqualTo(TheFirstRevision));

			Assert.That(pricing.AskedAt, Is.Not.Empty,
				"Y se dice CONTRA QUE INSTANTE se juzgo. Sin el, 'vigente' no es respecto de nada.");

			Assert.That(stale.Says, Does.StartWith("Cambio de fluido a los 20000 km: 149.9 USD"));
			Assert.That(stale.Says, Does.Contain("esa vigencia ya pasó"),
				"Un precio viejo mostrado como vigente es una promesa que la sucursal no hizo.");
			Assert.That(stale.Says, Does.Not.Contain("esa vigencia está rigiendo"));
		}

		// EL HITO SE DICE Y NO SE DIRECCIONA, y esa es la mitad de P-12 que hay que afirmar aparte:
		// devolver el hito en una forma que se pueda volver a pasar seria el mapeo inverso que DEC-24
		// ya paga dos veces, y volveria a poner el numero del fabricante en manos de quien pregunta.
		[Test]
		public void TheMilestoneIsSaidWithItsUnitAndNeverHandedBackAsANumberToPass()
		{
			journal.Api.OpenRevision(TheFirstRevision, TheFirstPublication);
			journal.Api.PublishServicePrice(
				TheFirstRevision, "FluidChange", TheMilestone, "UnitA", 89.90m, "USD",
				OnThe(2000, 1, 1), OnThe(2099, 12, 31));
			journal.Api.SealRevision(TheFirstRevision);

			ServicePricing pricing =
				TheAssistant().WhatTheServiceCosts(MaintenanceOperation.FluidChange);

			Assert.That(pricing.Prices, Has.Count.EqualTo(1));
			Assert.That(pricing.Prices[0].Milestone, Is.EqualTo("10000 km"));

			foreach (PropertyInfo each in typeof(PublishedPrice).GetProperties())
				Assert.That(
					each.PropertyType, Is.EqualTo(typeof(string)),
					"'" + each.Name + "' de una fila de precio no es texto. Un hito numerico de vuelta "
					+ "es una forma direccionable, y quien la reciba la puede pasar como si la hubiera "
					+ "aprendido del fabricante.");
		}

		// LA LISTA VACIA SON DOS RESPUESTAS DISTINTAS, y con una sola frase el asistente no puede
		// saber cual le toco. Reemplaza a la ley vieja -- "sin hito no se busca ningun precio" -- que
		// P-12 retiro: ya no hay ninguna pregunta a medio hacer, porque no hay nada que preguntar.
		[Test]
		public void AnEmptyPriceListIsTwoDifferentAnswersAndTheDetailSaysWhichOne()
		{
			journal.Api.OpenRevision(TheFirstRevision, TheFirstPublication);
			journal.Api.PublishServicePrice(
				TheFirstRevision, "FluidChange", TheMilestone, "UnitA", 89.90m, "USD",
				OnThe(2000, 1, 1), OnThe(2099, 12, 31));
			journal.Api.SealRevision(TheFirstRevision);

			// (a) HAY PRECIOS EN EL REGISTRO, PERO NINGUNO DE ESTA OPERACION.
			ServicePricing another = TheAssistant().WhatTheServiceCosts(MaintenanceOperation.Repair);

			Assert.That(another.AnyPriceOnRecord, Is.True);
			Assert.That(another.PriceCountOnRecord, Is.EqualTo(1),
				"La cuenta es del registro ENTERO y no de la operacion preguntada: es la asimetria "
				+ "que MS-4 dijo que no se ordenara.");
			Assert.That(another.Prices, Is.Empty);
			Assert.That(another.Detail, Does.Contain("No hay ningún precio publicado para esta operación"));
			Assert.That(another.Detail, Does.Not.Contain("no lleva NINGÚN precio publicado"));

			// (b) Y EL REGISTRO SIN NINGUN PRECIO EN ABSOLUTO, en su propia carpeta.
			using (var bare = new AScheduleOnARealJournal("1FTFW1ET5DFA00009", "Standard"))
			{
				ServicePricing nothing =
					new ModelSpecificationTools(new JournalReader(bare.Folder, ""))
						.WhatTheServiceCosts(MaintenanceOperation.FluidChange);

				Assert.That(nothing.AnyPriceOnRecord, Is.False);
				Assert.That(nothing.PriceCountOnRecord, Is.EqualTo(0));
				Assert.That(nothing.Prices, Is.Empty);
				Assert.That(nothing.Detail, Does.Contain("no lleva NINGÚN precio publicado"));
				Assert.That(
					nothing.Detail,
					Does.Not.Contain("No hay ningún precio publicado para esta operación"));
			}
		}

		// EL INTERVALO PUBLICADO SE CITA ENTERO, Y LA POSICION DE ESTE VEHICULO NO. Es la decision de
		// T8 corregida por la ley L7b: lo que faltaba no era el permiso de decir el intervalo, era
		// una lectura que lo dijera sin pedir la distancia que le queda al carro. Ahora existe.
		[Test]
		public void ThePublishedIntervalIsQuotedAndTheVehiclesStandingIsNot()
		{
			journal.Api.OpenRevision(TheFirstRevision, TheFirstPublication);
			journal.Api.PublishServiceInterval(
				TheFirstRevision, "FluidChange", TheMilestone, "UnitA", 365);
			journal.Api.SealRevision(TheFirstRevision);

			ServicePricing pricing =
				TheAssistant().WhatTheServiceCosts(MaintenanceOperation.FluidChange);

			Assert.That(pricing.IntervalPublished, Is.True);
			Assert.That(pricing.IntervalRevision, Is.EqualTo(TheFirstRevision));

			// LA FRASE PUBLICADA LLEGA, con su procedencia adentro.
			Assert.That(pricing.IntervalSays, Does.Contain("se hace cada"));
			Assert.That(pricing.IntervalSays, Does.Contain("365 días"));
			Assert.That(pricing.IntervalSays, Does.Contain(TheFirstRevision));

			Assert.That(pricing.Detail, Does.Contain("El intervalo de servicio está en esta respuesta"));

			// ESTA LINEA AFIRMABA "does not keep the odometer" Y ERA FALSA DESDE QUE EL ODOMETRO SE
			// PUSO EN LA PUERTA. El registro SI lo lleva; lo que no hay es la distancia que le falta
			// a este vehiculo CONTRA ESE INTERVALO, que no es lo mismo. La frase vieja estaba en una
			// cadena que el programa le entrega a un modelo, y esta prueba la sostenia: una prosa
			// vencida fijada por una afirmacion se vuelve carga y deja de poder corregirse sola.
			Assert.That(pricing.Detail, Does.Contain("tiene el odómetro"));
			Assert.That(
				pricing.Detail,
				Does.Contain("distancia que falta contra este intervalo no es un número"));

			// Y LA MITAD RECIBIDA NO LLEGA POR NINGUN CAMPO. El discriminador es la frase con la que
			// el DOMINIO abre la mitad recibida -- "This vehicle has <n> km to go against it" -- y no
			// el "to go" suelto: detail habla de esa distancia para decir que NO esta, asi que un
			// barrido por las tres palabras se pone rojo con el texto correcto adentro. Es C60 sobre
			// una frase en vez de sobre una lectura, y ya paso una vez en este mismo archivo.
			Assert.That(JsonSerializer.Serialize(pricing), Does.Not.Contain("le quedan"));

			// EL CONTROL: la puerta SI compone la frase emparejada cuando le llega la mitad del
			// vehiculo, asi que el renglon de arriba mira una diferencia real.
			Assert.That(
				journal.Api.DescribeServiceInterval("FluidChange", 2000, "UnitA"),
				Does.Contain("le quedan 2000 km"));
		}

		// Y CUANDO NO HAY INTERVALO PUBLICADO NO SE ANUNCIA NADA. Sin esto, la frase de arriba podria
		// ser una constante.
		[Test]
		public void AndWithNoIntervalPublishedNothingIsAnnouncedAboutIt()
		{
			ServicePricing pricing =
				TheAssistant().WhatTheServiceCosts(MaintenanceOperation.FluidChange);

			Assert.That(pricing.IntervalPublished, Is.False);
			Assert.That(pricing.Detail, Does.Not.Contain("Se publicó un intervalo de servicio"));
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
					assistant.WhatTheServiceCosts(MaintenanceOperation.FluidChange));
		}

		// La fila del hito que se nombra, buscada POR SU CELDA de hito: el juego viene enumerado y su
		// orden es el de las revisiones, asi que indexar por posicion afirmaria ese orden y no el hito.
		private static PublishedPrice ThePriceAt(ServicePricing pricing, string milestone)
		{
			foreach (PublishedPrice each in pricing.Prices)
				if (each.Milestone == milestone) return each;

			throw new InvalidOperationException(
				"La respuesta no trajo ningun precio al hito " + milestone + ".");
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
				TheAssistant().WhatTheServiceCosts(MaintenanceOperation.FluidChange);

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
				TheAssistant().WhatTheServiceCosts(MaintenanceOperation.FluidChange);

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

		// COMO EMPIEZA LA FRASE DE UNA LLANTA. El sujeto lo compone el dominio con la etiqueta de la
		// posicion en minuscula, y aca se rearma igual para que el barrido siga distinguiendo una
		// posicion de otra.
		private static string TheTyreSubjectFor(string position)
		{
			return "Llanta " + HowACanonNameReads.ATyre(position).ToLowerInvariant();
		}
	}
}
