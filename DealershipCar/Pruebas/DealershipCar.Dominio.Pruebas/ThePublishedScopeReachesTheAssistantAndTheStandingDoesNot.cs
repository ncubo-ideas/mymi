using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using DealershipCar.Mcp;
using DealershipCar.Mcp.Tools;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LA DECISION DE T8, AFIRMADA COMO LEY: al asistente le llega el ALCANCE PUBLICADO y no le llega
	// donde esta parado este vehiculo frente a el.
	//
	// POR QUE. Las tres lecturas de cobertura de la puerta piden los mismos NUEVE hechos --
	// el codigo del sistema, las dos mitades del sobre con su unidad, el instante de la entrega, la
	// lectura de odometro de la entrega, la de hoy con su unidad, y el instante en que se pregunta --
	// porque la especificacion no guarda NADA de ningun carro y la lectura los recibe. Y
	// DealershipCarAPI no tiene lectura para ninguno: ni odometro, ni instante de entrega, ni lectura
	// de entrega, ni sobre de garantia.
	//
	// LA CONSOLA SI PUEDE INVENTARLOS porque una caratula de consola inventa un escenario; una tool
	// MCP no. Un asistente que adivine el odometro de la persona o la fecha en que le entregaron el
	// carro es exactamente el fallo que este modulo existe para evitar, y un parametro de la tool que
	// se los pida AL ASISTENTE es el mismo defecto con un esquema encima.
	//
	// Asi que la tool dice el alcance, dice que la posicion no esta en este registro, y no la
	// estima. Lo que estas pruebas fijan es que sigue siendo asi.
	[TestFixture]
	public sealed class ThePublishedScopeReachesTheAssistantAndTheStandingDoesNot
	{
		private const string TheVin = "1HGCM82633A004352";

		private const string TheRevision = "MY2025-R1";
		private const string TheSystemInside = "POWERTRAIN";
		private const string TheSystemOnlyExcluded = "INFOTAINMENT";

		private static readonly DateTime ThePublication =
			new DateTime(2025, 8, 15, 0, 0, 0, DateTimeKind.Utc);
		private static readonly DateTime TheDelivery =
			new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc);
		private static readonly DateTime TheDayTheVehicleAsks =
			new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);

		private AScheduleOnARealJournal journal;

		[SetUp]
		public void ASealedScope()
		{
			journal = new AScheduleOnARealJournal(TheVin, "Standard");

			journal.Api.OpenRevision(TheRevision, ThePublication);
			journal.Api.PublishCoveredSystem(TheRevision, TheSystemInside, 0, 0, "UnitA");
			journal.Api.PublishCoverageExclusion(
				TheRevision, TheSystemInside, "wear parts", "when the part is a consumable");
			journal.Api.PublishCoverageExclusion(
				TheRevision, TheSystemOnlyExcluded, "screen burn-in", "at any mileage");
			journal.Api.PublishServicingPolicy(
				TheRevision,
				"service may be done at any qualified workshop",
				"keep the invoices and the parts records");
			journal.Api.SealRevision(TheRevision);
		}

		[TearDown]
		public void TearDown()
		{
			journal.Dispose();
		}

		// EL ALCANCE SI LLEGA, Y ENTERO: los sistemas adentro, TODAS las exclusiones con su condicion,
		// la revision, y la clausula del propio dominio que dice que esto es alcance y no la
		// resolucion de un reclamo. Callar una exclusion que el registro conoce es la peor respuesta
		// posible a una pregunta de cobertura, asi que se comprueba que salgan las dos.
		[Test]
		public void ThePublishedScopeArrivesWholeWithItsExclusionsAndItsRevision()
		{
			WarrantyScope scope = TheAssistantAsksAbout(TheSystemInside);

			Assert.That(scope.Published, Is.True);
			Assert.That(scope.Revision, Is.EqualTo(TheRevision));
			Assert.That(scope.CurrentRevision, Is.EqualTo(TheRevision));
			Assert.That(scope.CoveredSystems, Is.EqualTo(new[] { TheSystemInside }));
			Assert.That(scope.Exclusions, Has.Count.EqualTo(2),
				"Las DOS exclusiones. Quedarse con la primera elegiria por la persona cual le "
				+ "importa.");
			Assert.That(scope.Says, Does.Contain("wear parts"));
			Assert.That(scope.Says, Does.Contain("screen burn-in"));
			Assert.That(scope.Says, Does.Contain("revision " + TheRevision));
			Assert.That(scope.Says, Does.Contain("nothing about how a claim would be resolved"),
				"La clausula la compone el dominio y viaja adentro de la frase, para que la ley se "
				+ "diga a si misma y no se pierda en la primera capa que reformule la respuesta.");
		}

		// Y LA POSICION NO LLEGA -- NI POR UN CAMPO NI POR UNA FRASE. Se mide por el VALOR y no por la
		// lista de campos: una tool que metiera el nombre de la posicion en un 'detail' pasaria
		// cualquier prueba escrita sobre nombres de propiedad. Los cinco nombres se enumeran DEL
		// DOMINIO y no se escriben aca, porque una lista escrita a mano envejece sin avisar.
		[Test]
		public void NoneOfTheFiveStandingsEverAppearsInAnythingTheAssistantIsHanded()
		{
			string answers = JsonSerializer.Serialize(TheAssistantAsksAbout(TheSystemInside))
				+ JsonSerializer.Serialize(TheAssistantAsksAbout(TheSystemOnlyExcluded))
				+ JsonSerializer.Serialize(TheAssistantAsksAbout(""));

			foreach (string standing in TheFiveStandingsTheDomainDeclares())
				Assert.That(answers, Does.Not.Contain(standing),
					$"'{standing}' salio hacia el asistente. Esa respuesta se decide con nueve "
					+ "hechos del vehiculo que este registro no tiene, asi que lo unico que puede "
					+ "haberla producido es alguno inventado -- y una medicion que nunca se hizo "
					+ "viaja despues como un hecho.");
		}

		// EL CONTROL, Y ES EL QUE DA SENTIDO AL DE ARRIBA: la puerta SI contesta la posicion cuando le
		// llegan los nueve hechos. Lo que falta no es la lectura, es de donde sacarlos. Sin este
		// renglon, una lectura rota dejaria la prueba anterior verde por la razon equivocada.
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
				+ "ninguna lectura de DealershipCarAPI.");
		}

		// Y LAS TRES LECTURAS SIGUEN PIDIENDO LOS NUEVE. El dia que alguna acepte menos, esta prueba
		// se pone roja y la decision de arriba se vuelve a mirar en vez de quedar como costumbre.
		[Test]
		public void TheThreeCoverageReadsStillTakeTheWholeVehicleWithThem()
		{
			foreach (string each in new[]
			{
				"CanCoverageBeEvaluated", "ReadCoverageStanding", "DescribeCoverage",
			})
			{
				MethodInfo[] found = TheDoorsOverloadsOf(each);

				Assert.That(found, Has.Length.EqualTo(1), $"'{each}'");
				Assert.That(found[0].GetParameters(), Has.Length.EqualTo(9),
					$"'{each}' cambio de forma. Si ahora pide menos, o hay una lectura nueva que "
					+ "trae la mitad del vehiculo, la tool puede contestar la posicion y tiene que "
					+ "hacerlo.");
			}
		}

		// Y SE LE DICE AL ASISTENTE, en palabras, que la posicion no esta. Callarlo seria dejarle
		// leer "el sistema esta adentro del alcance" como "estas cubierto", que es el unico error de
		// esta tool que le cuesta dinero a la persona.
		[Test]
		public void TheAnswerSaysOutLoudThatTheStandingIsNotInThisRecord()
		{
			WarrantyScope scope = TheAssistantAsksAbout(TheSystemInside);

			Assert.That(scope.Detail, Does.Contain("published SCOPE and not this vehicle's standing"));
			Assert.That(scope.Detail, Does.Contain("odometer"));
			Assert.That(scope.Detail, Does.Contain("Do not estimate it"));
		}

		// LA REGLA DE SERVICIO EN OTRO TALLER TIENE LA MISMA FORMA Y SE DICE IGUAL: hay regla
		// publicada, y su texto no se puede citar sin los conteos de este vehiculo, que tampoco estan.
		[Test]
		public void ThePublishedServicingRuleIsAnnouncedAndSaidToBeUnquotable()
		{
			WarrantyScope scope = TheAssistantAsksAbout(TheSystemInside);

			Assert.That(scope.ServicingPolicyPublished, Is.True);
			Assert.That(scope.ServicingPolicyRevision, Is.EqualTo(TheRevision));
			Assert.That(scope.Detail, Does.Contain("servicing elsewhere"));
			Assert.That(scope.Detail, Does.Contain("does not keep that count"));
		}

		// Y CUANDO NO HAY REGLA PUBLICADA NO SE ANUNCIA NADA. Sin esto, la frase de arriba podria ser
		// una constante que sale siempre y no diria nada de lo publicado.
		[Test]
		public void AndWithNoServicingRulePublishedNothingIsAnnouncedAboutIt()
		{
			using (var bare = new AScheduleOnARealJournal("1FTFW1ET5DFA00001", "Standard"))
			{
				WarrantyScope scope =
					new ModelSpecificationTools(new JournalReader(bare.Folder, ""))
						.DoesTheWarrantyCover("");

				Assert.That(scope.ServicingPolicyPublished, Is.False);
				Assert.That(scope.Detail, Does.Not.Contain("servicing elsewhere"));
				Assert.That(scope.Detail, Does.Contain("published SCOPE"),
					"La otra frase si sale siempre, y tiene que salir: la posicion nunca esta.");
			}
		}

		// "NO NOS LO DIJERON" NO ES "NO LO CUBRE", que es el punto entero del conjunto de cinco y lo
		// unico que le queda al asistente de esa distincion cuando la posicion no viaja. Los tres
		// casos, en una prueba, porque la ley esta en la diferencia entre ellos.
		[Test]
		public void NeverToldAboutItIsNotTheScopeLeavingItOut()
		{
			Assert.That(TheAssistantAsksAbout(TheSystemInside).SystemInTheCatalogue, Is.True,
				"Declarado adentro: en el catalogo, y ademas en coveredSystems.");
			Assert.That(TheAssistantAsksAbout(TheSystemInside).CoveredSystems,
				Does.Contain(TheSystemInside));

			Assert.That(TheAssistantAsksAbout(TheSystemOnlyExcluded).SystemInTheCatalogue, Is.True,
				"Nombrado SOLO por una exclusion: sigue estando en el catalogo. Sin la union de los "
				+ "dos, un sistema que el fabricante excluyo expresamente contestaria que nadie "
				+ "publico nada sobre el, que es lo contrario de lo que pasa.");
			Assert.That(TheAssistantAsksAbout(TheSystemOnlyExcluded).CoveredSystems,
				Does.Not.Contain(TheSystemOnlyExcluded),
				"Y NO esta entre los que el alcance declara adentro. Esa es la diferencia que el "
				+ "asistente tiene que poder ver.");

			Assert.That(TheAssistantAsksAbout("SUSPENSION").SystemInTheCatalogue, Is.False,
				"Y de este no dijeron nada. Reportarlo como que no esta cubierto es como se manda a "
				+ "alguien de vuelta a su casa sin una reparacion a la que tenia derecho a lo "
				+ "menos a preguntar.");
		}

		// EL VOCABULARIO ES DEL FABRICANTE Y LA COMPARACION ES EXACTA, asi que un codigo con otra caja
		// contesta que no lo conocen -- no revienta, y no acierta de casualidad. Se comprueban las
		// dos mitades porque las dos son formas de mentir.
		[Test]
		public void AnUnknownOrDifferentlyCasedCodeIsAnsweredAndNeverThrown()
		{
			Assert.That(TheAssistantAsksAbout("powertrain").SystemInTheCatalogue, Is.False,
				"Minusculas: el codigo es del fabricante y aca no se le arregla la ortografia a "
				+ "nadie.");
			Assert.That(TheAssistantAsksAbout("POWER TRAIN").SystemInTheCatalogue, Is.False);
			Assert.That(TheAssistantAsksAbout("  " + TheSystemInside + "  ").SystemInTheCatalogue,
				Is.True,
				"Los espacios de los extremos si se recortan, que es lo que hace que quien publica y "
				+ "quien pregunta nombren la misma cosa.");

			// Y EL CODIGO EN BLANCO NO LLEGA A LA PUERTA. TheText de DealershipCarAPI revienta con
			// una cadena vacia, y un asistente que llame sin sistema esta preguntando por el alcance
			// entero, no cometiendo un error.
			WarrantyScope whole = TheAssistantAsksAbout("   ");

			Assert.That(whole.System, Is.EqualTo(""));
			Assert.That(whole.SystemInTheCatalogue, Is.False);
			Assert.That(whole.Published, Is.True, "Y el alcance entero contesta igual.");

			Assert.Throws<ArgumentException>(
				() => journal.Api.TheVocabularyIsTheManufacturers(""),
				"EL CONTROL: la puerta SI revienta con el codigo en blanco. Si dejara de hacerlo, la "
				+ "guarda de la tool estaria protegiendo de nada.");
		}

		private WarrantyScope TheAssistantAsksAbout(string system)
		{
			return new ModelSpecificationTools(new JournalReader(journal.Folder, ""))
				.DoesTheWarrantyCover(system);
		}

		// Los cinco nombres, sacados del propio conjunto cerrado del dominio.
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

		private static MethodInfo[] TheDoorsOverloadsOf(string name)
		{
			var found = new List<MethodInfo>();

			foreach (MethodInfo each in typeof(DealershipCar.App.DealershipCarAPI).GetMethods())
				if (each.Name == name) found.Add(each);

			return found.ToArray();
		}
	}
}
