using System;
using System.Collections.Generic;
using System.Reflection;
using DealershipCar.App;
using DealershipCar.Wasm;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LA MISMA ADJUDICACION QUE T8, VUELTA A TOMAR PARA UNA CARATULA DISTINTA, y con el mismo
	// resultado por una razon que no es la misma.
	//
	// EN T8 EL ARGUMENTO FUE QUE UN ASISTENTE NO ES FUENTE de los nueve hechos que las tres
	// lecturas de cobertura piden, y que un parametro de la tool que se los pidiera seria la misma
	// invencion con un esquema encima. El browser NO esta en esa posicion: tiene a la persona
	// delante y podria, en principio, PREGUNTARLE su sobre de garantia, la fecha de entrega y el
	// odometro. Asi que la pregunta se volvio a hacer entera, y la respuesta sigue siendo que no,
	// por tres razones que un formulario no arregla:
	//
	//   la app del dueno es de SOLO LECTURA -- los 75 comandos del dueno se retiraron -- y un
	//   formulario devuelve al dueno como FUENTE de hechos de su vehiculo, que es de lo que la
	//   regla trata y no de si algo se journaliza;
	//
	//   el odometro YA ESTA en el registro y la puerta no lo devuelve, asi que preguntarselo a la
	//   persona pondria DOS numeros para el mismo hecho en la misma pantalla -- uno contra el que
	//   se decide la agenda y otro contra el que se decidiria la garantia -- y en silencio;
	//
	//   y la frase que compone el dominio FUNDE las dos mitades. DescribeServicingElsewhereRule
	//   devuelve la regla publicada Y los conteos recibidos en UNA cadena, sellada con la revision
	//   del fabricante. Lo que la persona tecleo saldria en la pantalla con la procedencia del
	//   fabricante encima, y separarlo pediria que la caratula compusiera su propia frase, que es
	//   justo lo que T10 no hace.
	//
	// De los tres conteos que la regla pide, el registro SI lleva uno -- MissedCount -- y dos no
	// existen en ninguna parte. Uno de tres no es la frase.
	//
	// LO QUE ESTAS PRUEBAS FIJAN es que sigue siendo asi: que ninguna de las cinco posturas llega a
	// la hoja, que ningun conteo llega, y que la caratula no tiene por donde conseguirlos.
	[TestFixture]
	public sealed class TheCoverageStandingNeverReachesTheOwnersScreen
	{
		private const string TheVin = "1HGCM82633A004352";

		private const string TheRevision = "MY2025-R1";
		private const string TheSystemInside = "POWERTRAIN";

		private const string TheRuleAsPublished = "service may be done at any qualified workshop";
		private const string WhatPreservesCover = "keep the invoices and the parts records";

		private static readonly DateTime ThePublication =
			new DateTime(2025, 8, 15, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheDelivery =
			new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheDayTheVehicleAsks =
			new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);

		private AScheduleOnARealJournal journal;

		[SetUp]
		public void ASealedScopeAndAPublishedRule()
		{
			journal = new AScheduleOnARealJournal(TheVin, AppState.TheProfileAtDelivery);

			journal.Api.OpenRevision(TheRevision, ThePublication);
			journal.Api.PublishCoveredSystem(TheRevision, TheSystemInside, 0, 0, "UnitA");
			journal.Api.PublishCoverageExclusion(
				TheRevision, TheSystemInside, "wear parts", "when the part is a consumable");
			journal.Api.PublishServicingPolicy(TheRevision, TheRuleAsPublished, WhatPreservesCover);
			journal.Api.SealRevision(TheRevision);
		}

		[TearDown]
		public void TearDown()
		{
			journal.Dispose();
		}

		// NINGUNA DE LAS CINCO POSTURAS APARECE EN NINGUN VALOR DE LA HOJA. Se barren los VALORES y
		// no los nombres de propiedad, que es lo que hace que 'CoveredSystems' no ensucie el
		// barrido: lo que interesa no es como se llama un campo sino que ninguna RESPUESTA sea una
		// postura. Los cinco nombres se enumeran DEL DOMINIO, porque una lista escrita a mano aca
		// envejece sin avisar.
		[Test]
		public void NoneOfTheFiveStandingsEverAppearsInAnythingTheScreenIsHanded()
		{
			List<string> words = EveryWordTheSheetCarries(TheSheetTheScreenShows());

			foreach (string standing in TheFiveStandingsTheDomainDeclares())
				foreach (string said in words)
					Assert.That(said, Does.Not.Contain(standing),
						$"'{standing}' salio hacia la pantalla. Esa respuesta se decide con nueve "
						+ "hechos del vehiculo que este registro no tiene, asi que lo unico que "
						+ "puede haberla producido es alguno inventado -- y una medicion que nunca "
						+ "se hizo viaja despues como un hecho.");
		}

		// EL BARRIDO DE ARRIBA LLEGA A LAS HOJAS DEL ARBOL, y sin este renglon no se sabria: un
		// recorredor que no entrara a las listas ni a los objetos anidados devolveria tres cadenas
		// y pasaria la prueba anterior sin haber mirado nada.
		[Test]
		public void TheSweepReachesTheLeavesAndNotOnlyTheTopOfTheSheet()
		{
			List<string> words = EveryWordTheSheetCarries(TheSheetTheScreenShows());

			Assert.That(words, Has.Count.GreaterThanOrEqualTo(30),
				"La hoja lleva al menos seis fluidos por tres, tres llantas por tres, y las cadenas "
				+ "del encabezado, el combustible, el alcance y la regla.");

			Assert.That(words, Has.Some.Contains("EngineOil"),
				"No entro a la lista de fluidos.");
			Assert.That(words, Has.Some.Contains("Spare tyre"),
				"No entro a la lista de llantas.");
			Assert.That(words, Has.Some.Contains(TheSystemInside),
				"No entro al arreglo de sistemas del alcance.");
			Assert.That(words, Has.Some.Contains("wear parts"),
				"No entro al arreglo de exclusiones.");
			Assert.That(words, Has.Some.Contains(TheRevision),
				"No leyo ninguna procedencia.");
		}

		// NI EL TEXTO DE LA REGLA NI NINGUN CONTEO LLEGAN. Se anuncia que hay regla y de que
		// revision, y nada mas. Si alguien hiciera la lectura con los tres conteos en cero para
		// sacarle la frase al dominio, el texto publicado Y la frase de los conteos saldrian los
		// dos, y los dos renglones de abajo se ponen rojos.
		[Test]
		public void TheServicingRuleIsAnnouncedAndNeitherItsTextNorAnyCountIsOnTheSheet()
		{
			WhatTheManufacturerPublished sheet = TheSheetTheScreenShows();

			Assert.That(sheet.ServicingRule.Published, Is.True);
			Assert.That(sheet.ServicingRule.Revision, Is.EqualTo(TheRevision));

			List<string> words = EveryWordTheSheetCarries(sheet);

			foreach (string said in words)
			{
				Assert.That(said, Does.Not.Contain(TheRuleAsPublished),
					"El texto de la regla llego a la pantalla. Citarlo pide los tres conteos de "
					+ "este vehiculo, y el registro solo lleva uno de los tres.");
				Assert.That(said, Does.Not.Contain("external services with evidence"),
					"Un conteo llego a la pantalla. Dos de los tres no existen en ninguna lectura, "
					+ "asi que un numero ahi lo invento alguien.");
			}

			// EL CONTROL: la puerta SI compone la frase entera cuando le llegan los tres conteos.
			// Lo que falta no es la lectura, es de donde sacar dos de sus tres numeros.
			string whole = journal.Api.DescribeServicingElsewhereRule(2, 1, 0);

			Assert.That(whole, Does.Contain(TheRuleAsPublished));
			Assert.That(whole, Does.Contain("external services with evidence 2"));
		}

		// Y LA FORMA DEL PORTADOR LO SOSTIENE: la regla viaja como presencia y procedencia, sin sitio
		// donde meter una frase. Un campo nuevo ahi es la puerta por la que entrarian los conteos, y
		// esta prueba obliga a abrirla a proposito.
		[Test]
		public void TheServicingRuleCarrierHasNowhereToPutASentence()
		{
			var names = new List<string>();

			foreach (PropertyInfo each in typeof(PublishedServicingRule).GetProperties())
				names.Add(each.Name);

			Assert.That(names, Is.EquivalentTo(new[] { "Published", "Revision" }));
		}

		// Y SIN REGLA PUBLICADA NO SE ANUNCIA NADA. Sin esto, el anuncio de arriba podria ser una
		// constante que sale siempre y no diria nada de lo publicado.
		[Test]
		public void AndWithNoServicingRulePublishedNothingIsAnnouncedAboutIt()
		{
			using (var bare = new AScheduleOnARealJournal(
				"1FTFW1ET5DFA00001", AppState.TheProfileAtDelivery))
			{
				WhatTheManufacturerPublished sheet =
					WhatTheManufacturerPublished.ReadThrough(bare.Api);

				Assert.That(sheet.ServicingRule.Published, Is.False);
				Assert.That(sheet.ServicingRule.Revision, Is.Empty);
			}
		}

		// LAS PALABRAS QUE LA PANTALLA LE DICE A LA PERSONA NO CONTIENEN NINGUNA POSTURA TAMPOCO. Es
		// la otra mitad de lo que se ve: la hoja trae las frases del dominio y TheOwnerIsTold trae
		// las de la caratula, y las dos van a la misma pantalla.
		[Test]
		public void TheWordsTheScreenAddsOfItsOwnCarryNoStandingEither()
		{
			List<string> said = EveryWordTheScreenAddsOfItsOwn();

			Assert.That(said, Has.Count.EqualTo(5),
				"Si TheOwnerIsTold gana o pierde una frase, este barrido se vuelve a mirar en vez de "
				+ "seguir contando las de antes.");

			foreach (string standing in TheFiveStandingsTheDomainDeclares())
				foreach (string sentence in said)
					Assert.That(sentence, Does.Not.Contain(standing));
		}

		// Y SE LE DICE A LA PERSONA, EN PALABRAS, QUE LA POSICION NO ESTA Y POR QUE. Callarlo dejaria
		// leer "POWERTRAIN esta en la lista" como "estas cubierto", que es el unico error de esta
		// seccion que le cuesta dinero.
		//
		// Y CADA RENGLON AFIRMA LA CLAUSULA ENTERA Y NO EL SUSTANTIVO. Nombrar el sustantivo no
		// defiende nada: "Your dealership is not asked for them" contiene "Your dealership" y dice
		// lo contrario de lo que hay que decir. MEDIDO -- ese defecto corrio verde la primera vez, y
		// es C60 apuntado a una frase en vez de a una lectura.
		[Test]
		public void TheScreenSaysOutLoudThatTheStandingIsNotInThisRecordAndWhatWouldPutItThere()
		{
			Assert.That(
				TheOwnerIsTold.TheStandingIsNotInThisRecord,
				Does.Contain("not where your car stands against it"));
			Assert.That(
				TheOwnerIsTold.TheStandingIsNotInThisRecord,
				Does.Contain("the day the car was handed over, its odometer then and its odometer "
					+ "now, and this record holds none of the four"),
				"Los cuatro hechos se nombran como LO QUE DECIDIRIA la posicion. Con solo la "
				+ "palabra 'odometer' adentro, una frase que dijera que el odometro no hace falta "
				+ "pasaria igual.");
			Assert.That(
				TheOwnerIsTold.TheStandingIsNotInThisRecord,
				Does.Contain("Your dealership is who puts them here"),
				"Y quien los pone, dicho entero: es la mitad accionable, y es la que se pierde si "
				+ "esto solo comprobara que la palabra 'dealership' aparece en alguna parte.");

			Assert.That(
				TheOwnerIsTold.TheServicingRuleIsPublishedButNotQuotable,
				Does.Contain("does not count them"));

			// Y QUE UN SISTEMA QUE LA LISTA NO NOMBRA NO ES UNO QUE EL ALCANCE DEJE AFUERA. En una
			// pantalla sin buscador esa distincion no la puede hacer un control: solo la frase.
			Assert.That(
				TheOwnerIsTold.ASystemThatIsNotListedWasNotRuledOut,
				Does.Contain("was not ruled out here"));
		}

		// LA CARATULA NO TIENE POR DONDE CONSEGUIR UNA POSTURA, y eso es lo que hace que lo de arriba
		// no dependa de la buena voluntad de quien escriba el markup. La puerta es UNA: AppState la
		// guarda privada, asi que un componente solo puede llamar lo que AppState expone.
		//
		// SE ENUMERAN LOS SITIOS POR DONDE LA REGLA PODRIA ROMPERSE, no los sustantivos de los que
		// trata (C57): todo miembro publico del ensamblado que ENTREGUE un DealershipCarAPI abriria
		// una segunda puerta, y todo miembro publico de AppState que nombre una de las cinco
		// lecturas emparejadas seria la primera.
		[Test]
		public void TheShellHandsOutNoDoorAndOffersNoReadThatCouldProduceAStanding()
		{
			int swept = 0;

			foreach (Type each in typeof(AppState).Assembly.GetTypes())
			{
				if (!each.IsPublic) continue;

				swept++;

				foreach (PropertyInfo property in each.GetProperties(ThePublicOnes))
					Assert.That(property.PropertyType, Is.Not.EqualTo(typeof(DealershipCarAPI)),
						$"'{each.Name}.{property.Name}' entrega la puerta.");

				foreach (FieldInfo field in each.GetFields(ThePublicOnes))
					Assert.That(field.FieldType, Is.Not.EqualTo(typeof(DealershipCarAPI)),
						$"'{each.Name}.{field.Name}' entrega la puerta.");

				foreach (MethodInfo method in each.GetMethods(ThePublicOnes))
					Assert.That(method.ReturnType, Is.Not.EqualTo(typeof(DealershipCarAPI)),
						$"'{each.Name}.{method.Name}' entrega la puerta.");
			}

			Assert.That(swept, Is.GreaterThanOrEqualTo(8),
				"El barrido dejo de recorrer los tipos publicos de la caratula y no defiende nada.");

			// Y LA PUERTA QUE SI EXISTE ESTA GUARDADA. Sin este renglon, el barrido de arriba pasaria
			// aunque AppState tuviera el campo publico, porque un campo publico no es una propiedad.
			foreach (FieldInfo each in typeof(AppState).GetFields(
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
				if (each.FieldType == typeof(DealershipCarAPI))
					Assert.That(each.IsPrivate, Is.True, $"'{each.Name}' dejo de ser privado.");

			var offered = new List<string>();

			foreach (MethodInfo each in typeof(AppState).GetMethods(ThePublicOnes))
				offered.Add(each.Name);

			foreach (string read in ThePairedReadsTheScreenMayNotCall)
				Assert.That(offered, Does.Not.Contain(read),
					$"AppState ofrece '{read}'. Esa lectura empareja lo publicado con hechos de este "
					+ "vehiculo que el registro no lleva, asi que quien la llame va a tener que "
					+ "inventarlos. Si esta decision cambio, se cambia aca a proposito.");
		}

		// EL CONTROL DE TODO LO ANTERIOR: la puerta SI contesta la postura cuando le llegan los nueve
		// hechos. Lo que falta no es la lectura -- falta de donde sacar ocho de sus nueve
		// argumentos, y un formulario que se los pida a la persona es de donde sacarlos, no de donde
		// saberlos.
		[Test]
		public void TheDoorDoesAnswerTheStandingWhenTheNineFactsArriveAndThatIsWhatIsMissing()
		{
			Assert.That(
				journal.Api.CanCoverageBeEvaluated(
					TheSystemInside, 60, 100000, "UnitA", TheDelivery, 12000, 20000, "UnitA",
					TheDayTheVehicleAsks),
				Is.True);

			Assert.That(
				journal.Api.ReadCoverageStanding(
					TheSystemInside, 60, 100000, "UnitA", TheDelivery, 12000, 20000, "UnitA",
					TheDayTheVehicleAsks),
				Is.EqualTo("CoveredWithExclusion"),
				"La lectura funciona. Lo que no hay es odometro, entrega ni sobre de garantia en "
				+ "ninguna lectura de DealershipCarAPI, ni en la agenda que el browser si lee.");
		}

		private const BindingFlags ThePublicOnes =
			BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly;

		private static readonly string[] ThePairedReadsTheScreenMayNotCall =
		{
			"CanCoverageBeEvaluated", "ReadCoverageStanding", "DescribeCoverage",
			"DescribeServicingElsewhereRule", "DescribeServiceInterval",
		};

		private WhatTheManufacturerPublished TheSheetTheScreenShows()
		{
			return WhatTheManufacturerPublished.ReadThrough(journal.Api);
		}

		// Cada CADENA que la hoja lleva, entrando a las listas y a los portadores anidados. Los
		// nombres de propiedad no entran: lo que se afirma es que ninguna RESPUESTA es una postura.
		private static List<string> EveryWordTheSheetCarries(WhatTheManufacturerPublished sheet)
		{
			var words = new List<string>();

			Collect(sheet, words);

			return words;
		}

		private static void Collect(object carrier, List<string> words)
		{
			foreach (PropertyInfo each in carrier.GetType().GetProperties())
			{
				object value = each.GetValue(carrier);

				if (value == null) continue;

				if (value is string text) { words.Add(text); continue; }

				if (value is string[] many) { words.AddRange(many); continue; }

				if (value is IEnumerable<object> list)
				{
					foreach (object item in list) Collect(item, words);
					continue;
				}

				if (value.GetType().Assembly == typeof(AppState).Assembly) Collect(value, words);
			}
		}

		// Las frases que la caratula pone de su cosecha, enumeradas del propio tipo para que una
		// nueva entre al barrido sola.
		private static List<string> EveryWordTheScreenAddsOfItsOwn()
		{
			var said = new List<string>();

			foreach (FieldInfo each in typeof(TheOwnerIsTold).GetFields(
				BindingFlags.Static | BindingFlags.Public))
				if (each.FieldType == typeof(string)) said.Add((string)each.GetValue(null));

			return said;
		}

		private static List<string> TheFiveStandingsTheDomainDeclares()
		{
			var names = new List<string>();

			foreach (FieldInfo each in typeof(CoverageStanding).GetFields(
				BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
				if (each.FieldType == typeof(CoverageStanding))
					names.Add(((CoverageStanding)each.GetValue(null)).Name);

			Assert.That(names, Has.Count.EqualTo(5),
				"Si no son cinco, este barrido dejo de enumerar el conjunto y no defiende nada.");

			return names;
		}
	}
}
