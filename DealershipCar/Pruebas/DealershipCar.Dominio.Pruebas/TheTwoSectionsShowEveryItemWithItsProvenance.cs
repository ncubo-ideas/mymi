using System;
using DealershipCar.Wasm;
using DealershipCar.Wasm.Parts;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LO QUE LAS DOS SECCIONES DE T10 MUESTRAN, AFIRMADO DONDE SE PUEDE AFIRMAR. La caratula del
	// browser no se puede renderizar desde aca -- no hay banco de pruebas de componentes en este
	// repo -- pero el markup no lee nada: WhatTheManufacturerPublished.ReadThrough lee la ficha
	// entera por la puerta y las dos secciones pintan lo que traiga. Asi que lo que se defiende aca
	// es la hoja que llega a la pantalla, que es todo lo que la pantalla puede llegar a decir.
	//
	// EL ESTADO EN QUE SE COMPARA ES LA MITAD DE LA PRUEBA (C61). Se publican DOS MIEMBROS DE CADA
	// FAMILIA CON CLAVE EN DOS REVISIONES DISTINTAS y se deja al menos uno sin publicar, y NO se
	// sustituye la primera: al final de la ficha de la consola seis de los siete items contestan
	// "no publicado" en los dos lados y una comparacion ahi coincide, correctamente, en nada. Con
	// esta forma, cada fila tiene sus tres mitades -- presencia, procedencia y frase -- distintas de
	// las de su vecina, que es lo unico que hace roja una lectura con la clave fija (C60).
	[TestFixture]
	public sealed class TheTwoSectionsShowEveryItemWithItsProvenance
	{
		private const string TheVin = "1HGCM82633A004352";

		private const string TheFirstRevision = "MY2025-R1";
		private const string TheSecondRevision = "MY2026-R1";

		private const string TheOilGrade = "5W-30 synthetic";
		private const string TheBrakeFluidGrade = "DOT 4";
		private const string TheFrontTyre = "205/55R16";
		private const string TheRearTyre = "225/50R17";

		private const string TheSystemInside = "POWERTRAIN";

		private static readonly DateTime TheFirstPublication =
			new DateTime(2025, 8, 15, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheSecondPublication =
			new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);

		private AScheduleOnARealJournal journal;

		[SetUp]
		public void ASheetInTwoRevisionsWithGapsInIt()
		{
			journal = new AScheduleOnARealJournal(TheVin, AppState.TheProfileAtDelivery);

			journal.Api.OpenRevision(TheFirstRevision, TheFirstPublication);

			journal.Api.PublishFluidSpecification(
				TheFirstRevision, "EngineOil", TheOilGrade, 4.2m, "UnitA", 10000, "UnitA", 365);

			journal.Api.PublishTyreSpecification(
				TheFirstRevision, "Front", TheFrontTyre, 2.4m, "UnitA", "94V");

			journal.Api.PublishFuelGrade(TheFirstRevision, "91 RON regular", "Minimum");
			journal.Api.PublishFuelGrade(TheFirstRevision, "95 RON Premium", "Permitted");
			journal.Api.PublishFuelGrade(TheFirstRevision, "E85 Flex", "Prohibited");

			journal.Api.PublishCoveredSystem(TheFirstRevision, TheSystemInside, 0, 0, "UnitA");
			journal.Api.PublishCoverageExclusion(
				TheFirstRevision, TheSystemInside, "wear parts", "when the part is a consumable");
			journal.Api.PublishCoverageExclusion(
				TheFirstRevision, "INFOTAINMENT", "screen burn-in", "at any mileage");

			journal.Api.PublishServicingPolicy(
				TheFirstRevision,
				"service may be done at any qualified workshop",
				"keep the invoices and the parts records");

			journal.Api.SealRevision(TheFirstRevision);

			// La SEGUNDA revision agrega OTROS miembros de las dos familias con clave, y la presion
			// entra en la otra unidad a proposito: la unidad se contesta como se publico y esta
			// pantalla no ofrece con que convertirla (L8).
			journal.Api.OpenRevision(TheSecondRevision, TheSecondPublication);

			journal.Api.PublishFluidSpecification(
				TheSecondRevision, "BrakeFluid", TheBrakeFluidGrade, 0.9m, "UnitB", 40000, "UnitA",
				730);

			journal.Api.PublishTyreSpecification(
				TheSecondRevision, "Rear", TheRearTyre, 33m, "UnitB", "98W");

			journal.Api.SealRevision(TheSecondRevision);
		}

		[TearDown]
		public void TearDown()
		{
			journal.Dispose();
		}

		// LAS SEIS FILAS DE FLUIDO SALEN TODAS Y EN EL ORDEN DEL CANON, publicadas o no. Una seccion
		// que solo pintara lo publicado convertiria "de este no nos dijeron nada" en una fila que no
		// existe, que es como una ausencia se lee como un cero.
		[Test]
		public void TheSixFluidsComeOutInTheCanonsOrderWhateverIsPublished()
		{
			WhatTheManufacturerPublished sheet = TheSheetTheScreenShows();

			Assert.That(sheet.Fluids, Has.Count.EqualTo(6));

			for (int i = 0; i < AppState.TheSixFluidKinds.Length; i++)
				Assert.That(sheet.Fluids[i].Kind, Is.EqualTo(AppState.TheSixFluidKinds[i]),
					$"la fila {i} no es la clase que la lista de la caratula pone en ese sitio.");
		}

		// LAS TRES MITADES DE CADA FILA, POR LOS DOS LADOS. Dos clases publicadas en revisiones
		// DISTINTAS y una que nadie publico: presencia, procedencia y frase tienen que ser distintas
		// en las tres. Una lectura con la clave fija en el aceite pone la frase del aceite en las
		// seis filas y la revision de la primera en las seis, y aca se pone roja tres veces.
		[Test]
		public void EachFluidCarriesItsOwnPresenceItsOwnRevisionAndItsOwnSentence()
		{
			WhatTheManufacturerPublished sheet = TheSheetTheScreenShows();

			PublishedFluid oil = TheFluidRow(sheet, "EngineOil");

			Assert.That(oil.Published, Is.True);
			Assert.That(oil.Revision, Is.EqualTo(TheFirstRevision));
			Assert.That(oil.Says, Does.StartWith("Aceite de motor:"));
			Assert.That(oil.Says, Does.Contain(TheOilGrade));

			PublishedFluid brake = TheFluidRow(sheet, "BrakeFluid");

			Assert.That(brake.Published, Is.True);
			Assert.That(brake.Revision, Is.EqualTo(TheSecondRevision),
				"El liquido de frenos salio de la SEGUNDA revision. Una procedencia que contestara "
				+ "siempre la vigente, o siempre la primera, acertaria en una fila y mentiria en la "
				+ "otra.");
			Assert.That(brake.Says, Does.StartWith("Líquido de frenos:"));
			Assert.That(brake.Says, Does.Contain(TheBrakeFluidGrade));

			PublishedFluid coolant = TheFluidRow(sheet, "Coolant");

			Assert.That(coolant.Published, Is.False);
			Assert.That(coolant.Revision, Is.Empty,
				"Sin publicar no hay revision que nombrar, y el vacio no se confunde con un codigo "
				+ "porque ninguna revision tiene el codigo en blanco.");
			Assert.That(coolant.Says, Does.StartWith("Refrigerante:"));
			Assert.That(coolant.Says, Does.Contain("no se ha publicado nada"));
		}

		// LO MISMO EN LAS LLANTAS, y con la unidad de cada una: la delantera se publico en una y la
		// trasera en la otra, y las dos salen como se publicaron. Una conversion escondida en
		// cualquier capa cambia el numero de una de las dos y esta prueba lo ve.
		[Test]
		public void EachTyreCarriesItsOwnPresenceItsOwnRevisionAndThePressureInTheUnitItWasPublishedIn()
		{
			WhatTheManufacturerPublished sheet = TheSheetTheScreenShows();

			Assert.That(sheet.Tyres, Has.Count.EqualTo(3));

			PublishedTyre front = TheTyreRow(sheet, "Front");

			Assert.That(front.Published, Is.True);
			Assert.That(front.Revision, Is.EqualTo(TheFirstRevision));
			Assert.That(front.Says, Does.StartWith("Llanta delantera:"));
			Assert.That(front.Says, Does.Contain(TheFrontTyre));
			Assert.That(front.Says, Does.Contain("2.4 bar"));

			PublishedTyre rear = TheTyreRow(sheet, "Rear");

			Assert.That(rear.Published, Is.True);
			Assert.That(rear.Revision, Is.EqualTo(TheSecondRevision));
			Assert.That(rear.Says, Does.StartWith("Llanta trasera:"));
			Assert.That(rear.Says, Does.Contain(TheRearTyre));
			Assert.That(rear.Says, Does.Contain("33 psi"),
				"La trasera se publico en la otra unidad de presion y sale en ella. Si saliera en la "
				+ "de la delantera, alguien convirtio -- y una presion convertida es la que la "
				+ "persona le pone al carro.");

			PublishedTyre spare = TheTyreRow(sheet, "Spare");

			Assert.That(spare.Published, Is.False);
			Assert.That(spare.Revision, Is.Empty);
			Assert.That(spare.Says, Does.StartWith("Llanta de repuesto:"));
			Assert.That(spare.Says, Does.Contain("no se ha publicado nada"));
		}

		// EL COMBUSTIBLE SALE CON SUS TRES LISTAS SEPARADAS Y CON LA FRASE ENTERA. Las tres van a la
		// pantalla porque "prohibido" y "permitido" no se leen igual y la lista de prohibidos es la
		// que mas importa que salga completa.
		[Test]
		public void TheFuelComesOutWithItsThreeStandingsApartAndWithItsRevision()
		{
			PublishedFuel fuel = TheSheetTheScreenShows().Fuel;

			Assert.That(fuel.Published, Is.True);
			Assert.That(fuel.Revision, Is.EqualTo(TheFirstRevision));
			Assert.That(fuel.MinimumGrade, Is.EqualTo("91 RON regular"));
			Assert.That(fuel.PermittedGrades, Is.EqualTo(new[] { "95 RON Premium" }));
			Assert.That(fuel.ProhibitedGrades, Is.EqualTo(new[] { "E85 Flex" }));
			Assert.That(fuel.Says, Does.Contain("E85 Flex"));
			Assert.That(fuel.Says, Does.Contain("91 RON regular"));
			Assert.That(fuel.Says, Does.Contain("95 RON Premium"));
		}

		// EL ALCANCE SALE ENTERO: los sistemas adentro, LAS DOS exclusiones, y la clausula que el
		// propio dominio compone diciendo que esto es alcance y no la resolucion de un reclamo.
		// Quedarse con una exclusion elegiria por la persona cual le importa.
		[Test]
		public void TheScopeComesOutWholeWithItsExclusionsAndTheClauseTheDomainComposed()
		{
			PublishedCoverage coverage = TheSheetTheScreenShows().Coverage;

			Assert.That(coverage.Published, Is.True);
			Assert.That(coverage.Revision, Is.EqualTo(TheFirstRevision));
			Assert.That(coverage.CoveredSystems, Is.EqualTo(new[] { TheSystemInside }));
			Assert.That(coverage.Exclusions, Has.Length.EqualTo(2));
			Assert.That(coverage.Says, Does.Contain("wear parts"));
			Assert.That(coverage.Says, Does.Contain("screen burn-in"));
			Assert.That(coverage.Says, Does.Contain("nada sobre cómo se resolvería un reclamo"));
		}

		// LA REVISION VIGENTE SE PIDE DESPUES DE PREGUNTAR SI HAY ALGUNA, y es la SELLADA MAS
		// RECIENTE -- no la primera y no la que publico el item que se esta mirando. Por eso cada
		// fila lleva la suya al lado.
		[Test]
		public void TheCurrentRevisionIsTheLatestSealedOneAndCarriesTheDayItWasPublished()
		{
			WhatTheManufacturerPublished sheet = TheSheetTheScreenShows();

			Assert.That(sheet.HasASealedRevision, Is.True);
			Assert.That(sheet.CurrentRevision, Is.EqualTo(TheSecondRevision));
			Assert.That(sheet.CurrentRevisionPublishedAt, Is.EqualTo("2026-02-01"));
			Assert.That(sheet.Model, Is.EqualTo(AScheduleOnARealJournal.TheModelAtDelivery));
		}

		// Y SIN NINGUNA SELLADA LA HOJA SE ARMA IGUAL. Es el estado NORMAL: el dia que se instala la
		// app, y todo el rato mientras se arma una revision. Las dos lecturas de la vigente
		// REVIENTAN ahi -- el control de abajo lo mide -- asi que preguntar primero es la unica
		// forma de que la seccion pueda pintarse.
		[Test]
		public void WithNothingSealedTheSheetStillComesOutAndTheGuardedPairIsAskedBeforeItIsRead()
		{
			using (var bare = new AScheduleOnARealJournal(
				"1FTFW1ET5DFA00001", AppState.TheProfileAtDelivery))
			{
				WhatTheManufacturerPublished sheet =
					WhatTheManufacturerPublished.ReadThrough(bare.Api);

				Assert.That(sheet.HasASealedRevision, Is.False);
				Assert.That(sheet.CurrentRevision, Is.Empty);
				Assert.That(sheet.CurrentRevisionPublishedAt, Is.Empty);
				Assert.That(sheet.Model, Is.EqualTo(AScheduleOnARealJournal.TheModelAtDelivery),
					"El modelo si contesta: es un hecho de la primera apertura y no de una "
					+ "publicacion.");

				Assert.That(sheet.Fluids, Has.Count.EqualTo(6));
				Assert.That(sheet.Coverage.Published, Is.False);
				Assert.That(sheet.ServicingRule.Published, Is.False);
				Assert.That(sheet.ServicingRule.Revision, Is.Empty);

				// Y EL COMBUSTIBLE TAMBIEN CONTESTA QUE NO. Va aparte porque es la unica de las
				// cinco presencias que no tiene una clave con que discriminarse: los fluidos y las
				// llantas tienen un miembro sin publicar al lado de uno publicado, y el
				// combustible es uno solo. Sin este lado, una presencia clavada en true no la ve
				// nadie -- MEDIDO: el defecto corrio verde la primera vez.
				Assert.That(sheet.Fuel.Published, Is.False);
				Assert.That(sheet.Fuel.Revision, Is.Empty);
				Assert.That(sheet.Fuel.MinimumGrade, Is.Empty);
				Assert.That(sheet.Fuel.PermittedGrades, Is.Empty);
				Assert.That(sheet.Fuel.ProhibitedGrades, Is.Empty,
					"Y la lista vacia dice que no hay ninguno publicado, que no es lo mismo que "
					+ "decir que ninguno esta prohibido.");
				Assert.That(sheet.Fuel.Says, Does.Contain("no se ha publicado nada"));

				// EL CONTROL, y sin el lo de arriba estaria verde por la razon equivocada: las dos
				// lecturas de la vigente si revientan cuando no hay ninguna sellada. Lo que la
				// composicion hace es preguntar antes, no que la puerta se haya ablandado.
				Assert.Throws<InvalidOperationException>(() => bare.Api.ReadCurrentRevisionCode());
				Assert.Throws<InvalidOperationException>(
					() => bare.Api.ReadCurrentRevisionPublishedAt());
			}
		}

		// SUSTITUIR RETIRA LO QUE LA SUCESORA NO REPUBLICO, Y LA PANTALLA LO MUESTRA RETIRADO. Es la
		// ley que mas facil se le escapa a una vista que cachee: el aceite estaba publicado hace un
		// momento y despues de la sustitucion la respuesta honesta es que no lo esta.
		[Test]
		public void SupersedingWithdrawsWhatTheSuccessorDidNotRepublishAndTheScreenSaysSo()
		{
			Assert.That(TheFluidRow(TheSheetTheScreenShows(), "EngineOil").Published, Is.True,
				"Antes de sustituir, el aceite de la primera revision si contesta.");

			journal.Api.SupersedeRevision(TheFirstRevision, TheSecondRevision);

			WhatTheManufacturerPublished after = TheSheetTheScreenShows();

			PublishedFluid oil = TheFluidRow(after, "EngineOil");

			Assert.That(oil.Published, Is.False,
				"La sucesora no republico el aceite, asi que se retira con la revision que lo "
				+ "publico. Una pantalla que lo siguiera mostrando estaria citando una revision "
				+ "sustituida.");
			Assert.That(oil.Revision, Is.Empty);
			Assert.That(oil.Says, Does.Contain("no se ha publicado nada"));

			Assert.That(TheFluidRow(after, "BrakeFluid").Published, Is.True,
				"Y lo que si republico la sucesora se queda. Sin este renglon, una hoja que se "
				+ "vaciara entera pasaria la mitad de arriba.");
			Assert.That(TheFluidRow(after, "BrakeFluid").Revision, Is.EqualTo(TheSecondRevision));

			Assert.That(after.Coverage.Published, Is.False,
				"Y el alcance tambien se retira: lo publico la primera revision y la sucesora no lo "
				+ "toco.");
			Assert.That(after.ServicingRule.Published, Is.False);
		}

		// LA ETIQUETA DE LA FILA ES EL NOMBRE DEL CANON DICHO EN EL IDIOMA DE LA PANTALLA, y no el
		// nombre del canon. Lo que cruza la puerta sigue siendo el del canon -- la caja es estricta
		// y la puerta rechaza 'engineoil' -- asi que el mapeo es lo unico que pasa entre el dominio
		// y el ojo de la persona, y es la misma separacion que C67 hizo con las tres unidades.
		[Test]
		public void TheRowLabelIsTheCanonsNameSaidInTheLanguageOfTheScreen()
		{
			Assert.That(HowACanonNameReads.AFluid("EngineOil"), Is.EqualTo("Aceite de motor"));
			Assert.That(
				HowACanonNameReads.AFluid("PowerSteeringFluid"),
				Is.EqualTo("Líquido de dirección asistida"));
			Assert.That(HowACanonNameReads.ATyre("Front"), Is.EqualTo("Delantera"));
			Assert.That(HowACanonNameReads.AnOperation("FluidChange"), Is.EqualTo("Cambio de fluido"));
			Assert.That(HowACanonNameReads.ADueState("Overdue"), Is.EqualTo("Atrasada"));
			Assert.That(HowACanonNameReads.AStanding("NotPublished"), Is.EqualTo("Sin publicar"));
		}

		// EL MAPEO ES EXHAUSTIVO SOBRE LOS CONJUNTOS QUE LA PANTALLA RECORRE. No alcanza con probar
		// los seis que uno se acordo de escribir arriba: el dia que entre un miembro nuevo, nadie le
		// pone etiqueta, cae al respaldo y la persona lee el nombre del codigo en ingles. Esto se
		// pone rojo ese dia.
		[TestCase("AnOperation")]
		[TestCase("ADueState")]
		[TestCase("AFluid")]
		[TestCase("ATyre")]
		[TestCase("AStanding")]
		public void NoMemberOfASetTheScreenWalksIsShownByItsCodeName(string mapeo)
		{
			foreach (string name in TheNamesTheScreenWalks(mapeo))
				Assert.That(TheLabelFor(mapeo, name), Is.Not.EqualTo(AppState.Spaced(name)),
					$"'{name}' cae al respaldo: la pantalla lo muestra con su nombre de codigo "
					+ "separado en mayusculas, que es ingles, y la persona lee eso.");
		}

		// Y EL RESPALDO SIGUE AHI PARA LO QUE NINGUNA LISTA NOMBRA. Un nombre desconocido se separa
		// en vez de desaparecer, que es lo que deja que agregar un miembro al dominio ponga rojo la
		// prueba de arriba en vez de vaciar una fila en silencio.
		[Test]
		public void AnUnknownNameFallsBackToTheSpacedFormInsteadOfVanishing()
		{
			Assert.That(HowACanonNameReads.AFluid("SomethingNew"), Is.EqualTo("Something new"));
			Assert.That(AppState.Spaced(""), Is.Empty);
			Assert.That(AppState.Spaced(null), Is.Null);
		}

		private static string[] TheNamesTheScreenWalks(string mapeo)
		{
			switch (mapeo)
			{
				case "AnOperation": return AppState.TheNineKinds;
				case "ADueState": return TheSevenDueStates;
				case "AFluid": return AppState.TheSixFluidKinds;
				case "ATyre": return AppState.TheThreeTyrePositions;
				case "AStanding": return TheEightStandings;
			}

			throw new ArgumentException("No hay ningún mapeo llamado '" + mapeo + "'.");
		}

		private static string TheLabelFor(string mapeo, string name)
		{
			switch (mapeo)
			{
				case "AnOperation": return HowACanonNameReads.AnOperation(name);
				case "ADueState": return HowACanonNameReads.ADueState(name);
				case "AFluid": return HowACanonNameReads.AFluid(name);
				case "ATyre": return HowACanonNameReads.ATyre(name);
				case "AStanding": return HowACanonNameReads.AStanding(name);
			}

			throw new ArgumentException("No hay ningún mapeo llamado '" + mapeo + "'.");
		}

		// Los dos conjuntos que la pantalla recorre y que AppState no enumera: los estados del
		// calendario y las dos posturas que comparten NotPublished.
		private static readonly string[] TheSevenDueStates =
		{
			"NotDue", "DueSoon", "Due", "Overdue", "Deferred", "Completed", "Superseded"
		};

		private static readonly string[] TheEightStandings =
		{
			"Inside", "Reached", "NotPublished", "Covered", "CoveredWithExclusion", "NotCovered",
			"OutsideEnvelope"
		};

		private WhatTheManufacturerPublished TheSheetTheScreenShows()
		{
			return WhatTheManufacturerPublished.ReadThrough(journal.Api);
		}

		private static PublishedFluid TheFluidRow(
			WhatTheManufacturerPublished sheet, string kind)
		{
			foreach (PublishedFluid each in sheet.Fluids)
				if (each.Kind == kind) return each;

			Assert.Fail($"la hoja no trae fila para '{kind}'.");
			return null;
		}

		private static PublishedTyre TheTyreRow(
			WhatTheManufacturerPublished sheet, string position)
		{
			foreach (PublishedTyre each in sheet.Tyres)
				if (each.Position == position) return each;

			Assert.Fail($"la hoja no trae fila para '{position}'.");
			return null;
		}
	}
}
