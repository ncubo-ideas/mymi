using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using DealershipCar.App;
using DealershipCar.App.Events;
using DealershipCar.Mcp;
using DealershipCar.Mcp.Tools;
using NUnit.Framework;
using Puppets.Portador;

namespace DealershipCar.Dominio.Pruebas
{
	// LA INVARIANTE DE UN SOLO ESCRITOR, que es lo unico que T9 tiene que demostrar.
	//
	// El servidor MCP y la app instalada comparten la carpeta. La app escribe; este proceso solo
	// lee. Si leer escribiera, dos procesos estarian journalizando sobre el mismo actor y la
	// promesa de que el journal reproduce la misma historia dos veces se caeria.
	//
	// SE MIDE POR LA ENTRADA Y POR EL SEGUNDO PROGRAMA, no por el md5 de la carpeta entera, y esa
	// eleccion esta medida y no elegida por comodidad: el motor NO tiene modo de solo lectura.
	// Abrir un journal existente le crea index/index.bin, le reescribe meta.bin, y a la SEPTIMA
	// apertura le escribe tres bytes de CABECERA al archivo del journal -- un punto de control de
	// replay que lleva la cuenta de entradas (28 -> 0x1c, 29 -> 0x1d, 32 -> 0x20, comprobado). Todo
	// eso converge y nada de eso es un HECHO: la cuenta de entradas no se mueve, el largo del
	// archivo no se mueve, y el segundo programa -- cada define, cada invoke y cada blob de
	// argumentos -- queda igual. Ver CONTRADICTIONS.md C30.
	[TestFixture]
	public sealed class TheAssistantOnlyEverReadsTheJournal
	{
		private const string TheVin = "1HGCM82633A004352";

		[Test]
		public void EveryToolTheAssistantCanCallLeavesTheJournalWithoutANewEntry()
		{
			using (AScheduleOnARealJournal journal = AVehicleWithAPlanAndAnEvaluation())
			{
				long entriesBefore = TheEntryCountOf(journal.Folder);
				long lengthBefore = TheJournalLengthOf(journal.Folder);

				DealershipCarTools tools = TheAssistantsToolsOver(journal.Folder);

				ModelSpecificationTools sheet = TheAssistantsSheetToolsOver(journal.Folder);

				WarrantyEnvelopeTools envelope = TheAssistantsEnvelopeToolsOver(journal.Folder);

				ShopStayTools stays = TheAssistantsStayToolsOver(journal.Folder);

				OdometerSeriesTools driven = TheAssistantsSeriesToolsOver(journal.Folder);

				CostLedgerTools spent = TheAssistantsLedgerToolsOver(journal.Folder);

				// LAS DOCE tools, varias veces, que es como pregunta un asistente de verdad. Las de
				// las cinco clases van en la misma vuelta: la promesa es de la carpeta entera y no de
				// una familia de preguntas.
				//
				// LA DE LAS ESTADIAS Y LA DEL ODOMETRO VAN DE LAS TRES FORMAS -- con los dos limites
				// en blanco, con un periodo nombrado y con un limite que no dice un dia -- porque son
				// recorridos distintos adentro de la tool y el que no se corra no queda probado. El
				// tercero importa: es el unico que NO llega a ninguna lectura de la puerta, asi que
				// sin el la promesa se estaria comprobando sobre el camino largo nada mas.
				for (int round = 0; round < 4; round++)
				{
					tools.Review();
					tools.HowUpToDate();
					tools.WhatIsLeftBeforeEachService();
					tools.WhatHasBeenDoneToMyCar();
					sheet.WhatMyCarTakes();
					sheet.DoesTheWarrantyCover("POWERTRAIN");
					sheet.WhatTheServiceCosts(MaintenanceOperation.FluidChange);
					envelope.IsMyCarStillUnderWarranty();
					stays.DaysMyCarSpentInTheShop();
					stays.DaysMyCarSpentInTheShop("2026-01-01", "2026-12-31");
					driven.HowMuchMyCarIsDriven();
					driven.HowMuchMyCarIsDriven("2026-01-01", "2026-12-31");
					driven.HowMuchMyCarIsDriven("el martes pasado", "");
					spent.WhatIHaveSpentOnMyCar();
					spent.WhatIHaveSpentOnMyCar("2026-01-01", "2026-12-31", "2025-01-01", "2025-12-31");
					spent.WhatIHaveSpentOnMyCar("el martes pasado", "", "", "");

					foreach (MaintenanceOperation each in TheOperationKind.All)
						tools.CheckOne(each);
				}

				Assert.That(TheEntryCountOf(journal.Folder), Is.EqualTo(entriesBefore),
					"Una pregunta de la IA dejo una entrada en el journal de la persona. Eso es "
					+ "un segundo escritor sobre el mismo actor, que es exactamente lo que T9 "
					+ "existe para no hacer.");
				Assert.That(TheJournalLengthOf(journal.Folder), Is.EqualTo(lengthBefore),
					"El archivo del journal crecio. Ninguna lectura puede hacerlo crecer.");
			}
		}

		// EL SEGUNDO PROGRAMA ES LO QUE TIENE QUE QUEDAR IGUAL, y es la forma de la afirmacion que
		// C23 dejo establecida: no los bytes de la carpeta -- que el motor toca por su cuenta --
		// sino las sentencias que el actor pronuncio y los valores que llevaron.
		[Test]
		public void TheSecondProgramIsIdenticalAfterTheAssistantHasAsked()
		{
			using (AScheduleOnARealJournal journal = AVehicleWithAPlanAndAnEvaluation())
			{
				string before = TheSecondProgramOf(journal.Folder);

				DealershipCarTools tools = TheAssistantsToolsOver(journal.Folder);
				ModelSpecificationTools sheet = TheAssistantsSheetToolsOver(journal.Folder);
				WarrantyEnvelopeTools envelope = TheAssistantsEnvelopeToolsOver(journal.Folder);
				ShopStayTools stays = TheAssistantsStayToolsOver(journal.Folder);
				OdometerSeriesTools driven = TheAssistantsSeriesToolsOver(journal.Folder);
				CostLedgerTools spent = TheAssistantsLedgerToolsOver(journal.Folder);

				for (int round = 0; round < 12; round++)
				{
					tools.Review();
					tools.WhatIsLeftBeforeEachService();
					tools.WhatHasBeenDoneToMyCar();
					sheet.WhatMyCarTakes();
					sheet.WhatTheServiceCosts(MaintenanceOperation.FluidChange);
					envelope.IsMyCarStillUnderWarranty();
					stays.DaysMyCarSpentInTheShop();
					driven.HowMuchMyCarIsDriven();
					driven.HowMuchMyCarIsDriven("2026-01-01", "2026-12-31");
					spent.WhatIHaveSpentOnMyCar();
					spent.WhatIHaveSpentOnMyCar("2026-01-01", "2026-12-31", "2025-01-01", "2025-12-31");
				}

				Assert.That(TheSecondProgramOf(journal.Folder), Is.EqualTo(before),
					"El programa que quedo journalizado cambio despues de doce preguntas. Leer no "
					+ "puede agregar ni una sentencia.");
			}
		}

		// EL GUARDIAN DE LAS DOS DE ARRIBA, Y TIENE QUE SER DISTINTO. Las dos afirman que algo NO
		// cambio; una medicion que no pudiera detectar un cambio las dejaria verdes para siempre --
		// que es la leccion que T6, T7 y T8 aprendieron una vez cada uno. Abrir con el host DE LA
		// APP si escribe: OnHydrated deja la entrada de la semilla en CADA hidratacion, aunque los
		// dos upgrades ya esten aplicados y no hagan nada.
		[Test]
		public void TheAppsOwnHostAddsOneEntryEveryTimeItOpensAndThatIsWhyTheMcpHostIsADifferentOne()
		{
			using (AScheduleOnARealJournal journal = AVehicleWithAPlanAndAnEvaluation())
			{
				long entriesBefore = TheEntryCountOf(journal.Folder);
				string programBefore = TheSecondProgramOf(journal.Folder);

				journal.CloseAndReopenFromTheJournal();

				Assert.That(TheEntryCountOf(journal.Folder), Is.EqualTo(entriesBefore + 1),
					"Reabrir con DealershipCarApp tiene que costar exactamente una entrada. Si no "
					+ "cuesta ninguna, la medicion no distingue leer de escribir y las dos pruebas "
					+ "de arriba no prueban nada.");
				Assert.That(TheSecondProgramOf(journal.Folder), Is.Not.EqualTo(programBefore),
					"Y el segundo programa tiene que CAMBIAR, o el extractor no ve nada.");
			}
		}

		// LA CREDENCIAL DE LA PERSONA NO VIAJA EN NINGUNA RESPUESTA, y se comprueba por el valor y
		// no por la lista de campos: una tool nueva que la copiara en un 'detail' pasaria cualquier
		// prueba escrita sobre nombres de propiedad. El token esta EN CLARO en el journal (deuda
		// aceptada, plan §8) en una carpeta que este proceso ademas lee, asi que lo unico que lo
		// separa del asistente es que ninguna tool lo pida.
		[Test]
		public void NoAnswerTheAssistantGetsCarriesThePersonsCredential()
		{
			using (AScheduleOnARealJournal journal = AVehicleWithAPlanAndAnEvaluation())
			{
				journal.Api.RegisterDevice("device-que-no-tiene-que-salir");
				journal.Api.StoreCredential("token-que-no-tiene-que-salir", "refresh-que-tampoco");

				DealershipCarTools tools = TheAssistantsToolsOver(journal.Folder);
				ModelSpecificationTools sheet = TheAssistantsSheetToolsOver(journal.Folder);
				WarrantyEnvelopeTools envelope = TheAssistantsEnvelopeToolsOver(journal.Folder);
				ShopStayTools stays = TheAssistantsStayToolsOver(journal.Folder);
				OdometerSeriesTools driven = TheAssistantsSeriesToolsOver(journal.Folder);
				CostLedgerTools spent = TheAssistantsLedgerToolsOver(journal.Folder);

				var everything = new StringBuilder();
				everything.Append(JsonSerializer.Serialize(tools.Review()));
				everything.Append(JsonSerializer.Serialize(tools.HowUpToDate()));
				everything.Append(JsonSerializer.Serialize(tools.WhatIsLeftBeforeEachService()));
				everything.Append(JsonSerializer.Serialize(tools.WhatHasBeenDoneToMyCar()));
				everything.Append(JsonSerializer.Serialize(sheet.WhatMyCarTakes()));
				everything.Append(JsonSerializer.Serialize(sheet.DoesTheWarrantyCover("POWERTRAIN")));
				everything.Append(JsonSerializer.Serialize(
					sheet.WhatTheServiceCosts(MaintenanceOperation.FluidChange)));
				everything.Append(JsonSerializer.Serialize(envelope.IsMyCarStillUnderWarranty()));
				everything.Append(JsonSerializer.Serialize(stays.DaysMyCarSpentInTheShop()));
				everything.Append(JsonSerializer.Serialize(driven.HowMuchMyCarIsDriven()));
				everything.Append(JsonSerializer.Serialize(spent.WhatIHaveSpentOnMyCar()));
				foreach (MaintenanceOperation each in TheOperationKind.All)
					everything.Append(JsonSerializer.Serialize(tools.CheckOne(each)));

				string answers = everything.ToString();

				Assert.That(answers, Does.Not.Contain("token-que-no-tiene-que-salir"));
				Assert.That(answers, Does.Not.Contain("refresh-que-tampoco"));
				Assert.That(answers, Does.Not.Contain("device-que-no-tiene-que-salir"));

				// Y el control: la puerta SI las tiene guardadas, asi que lo de arriba mide que la
				// caratula no las pide y no que el journal no las tenga.
				Assert.That(journal.Api.StoredToken(), Is.EqualTo("token-que-no-tiene-que-salir"));
			}
		}

		// HASTA DONDE LEYO LA APP LA NARRATIVA DEL CONCESIONARIO, que es la unica pregunta con la que
		// el asistente puede saber si lo que acaba de contestar esta al dia. Las instructions se
		// apoyan en ella: "preguntala dos veces alrededor de un Refresh; si subio, entraron cosas
		// nuevas".
		//
		// SE MIDE CONTRA UNA NARRATIVA DE VERDAD Y CONTRA UNA A MEDIAS, y hacen falta las dos. La
		// historia de la consola nunca ingiere eventos, asi que el punto de control vale CERO ahi y
		// una constante cero pasaria inadvertida -- corrio LIMPIO en la falsificacion de T9. Es la
		// misma forma que T8 encontro tres veces: todo en el guion coincide, asi que cruzarlo no
		// cambia nada.
		[Test]
		public void TheAssistantSeesHowFarTheAppHasReadTheDealershipsNarrative()
		{
			using (var journal = new AScheduleOnARealJournal(ADealershipNarrative.TheVin, "Standard"))
			{
				DealershipCarTools tools = TheAssistantsToolsOver(journal.Folder);

				Assert.That(tools.HowUpToDate().LastEventApplied, Is.EqualTo(0),
					"Nada entro todavia.");

				TheNarrativeUpTo(journal, 4);
				long halfway = tools.HowUpToDate().LastEventApplied;

				Assert.That(halfway, Is.EqualTo(4));

				TheNarrativeUpTo(journal, ADealershipNarrative.TheLastEvent);
				long whole = tools.HowUpToDate().LastEventApplied;

				// NUEVE Y NO DIEZ, que es el numero que ya fija AReceivedEventBlockIsAppliedExactlyOnce:
				// el evento 10 trae una unidad que el canon no escribe y la frontera lo DESCARTA, asi
				// que nunca llega a la puerta y no puede mover el punto de control.
				Assert.That(whole, Is.EqualTo(9L));
				Assert.That(whole, Is.GreaterThan(halfway),
					"Y SUBE. Es lo unico que hace util el campo: el asistente lo pregunta antes y "
					+ "despues de que la persona apriete Refresh.");

				// El tablero trae el mismo numero que la tool dedicada: dos respuestas distintas a la
				// misma pregunta serian peor que ninguna.
				Assert.That(tools.Review().LastEventApplied, Is.EqualTo(whole));
				Assert.That(tools.CheckOne(MaintenanceOperation.FluidChange).LastEventApplied,
					Is.EqualTo(whole));
			}
		}

		// Aplica la narrativa del concesionario hasta ese numero de evento, por la misma puerta y la
		// misma ruta que usa el bucle de refresh.
		private static void TheNarrativeUpTo(AScheduleOnARealJournal journal, long lastEvent)
		{
			foreach (SenalDeEvento signal in ADealershipNarrative.TheWholeStory())
			{
				if (signal.Numero > lastEvent) break;

				IMensajeDeEvento message = DealershipEventRoute.Route(signal);
				if (message != null) journal.Api.Apply(message);
			}
		}

		// LO QUE SE LE OFRECE A LA IA ES UNA LISTA CERRADA DE PREGUNTAS, no una consola de DSL.
		//
		// SE BARRE EL ENSAMBLADO Y NO UNA CLASE. Con typeof(DealershipCarTools).GetMethods() la lista
		// solo veia las tools de esa clase, y despues llegaron una segunda y una tercera: un barrido
		// por la clase que uno se acuerda de escribir habria reportado tres y dejado cuatro afuera. Es
		// C57 en esta frontera -- se enumera el sitio donde la regla se decide, no el sustantivo del
		// que habla.
		//
		// LO QUE ESTE BARRIDO NO PUEDE VER, dicho en vez de omitido: que Program.cs REGISTRE todas las
		// clases que la prueba de mas abajo enumera. Una clase con [McpServerToolType] que nadie pasa a
		// .WithTools sale igual en esta lista y no llega a ningun asistente. Program.cs son sentencias
		// de nivel superior de un Exe y armar aca su contenedor seria copiarlo, no probarlo. Lo unico
		// que atrapa un .WithTools que falta es un tools/list corrido a mano contra el servidor.
		//
		// ESTE NOMBRE DECIA OCHO Y MS-6 LO VOLVIO FALSO: la pantalla de mantenimiento agrega dos tools
		// a DealershipCarTools -- la cuenta atras y el historial. El nombre se mueve con la ley, que es
		// lo que SH-6 ya hizo una vez con el mismo nombre; aflojarlo a "unas cuantas" habria dejado de
		// obligar a mirar la lista.
		//
		// Y OD-4 LO VOLVIO A MOVER, a ONCE: how_much_my_car_is_driven, el ritmo de uso de un periodo.
		//
		// Y CL-8 A DOCE: what_i_have_spent_on_my_car, en que se le fue la plata a esta persona con este
		// carro. Es la primera tool que pregunta por DOS periodos, y por eso es la primera que toma
		// cuatro limites.
		//
		// Y LP-4 A TRECE: what_plate_my_car_has, la placa de este vehiculo. Es la primera que NO toma
		// ningun parametro y a la vez es la que mas facil los habria tomado -- el cruce es "compara mi
		// placa contra las de otro registro" y el camino corto es recibirla --, asi que su renglon en
		// esta lista es tambien donde se ve que se decidio que no.
		[Test]
		public void TheAssistantIsOfferedThirteenToolsAndNoneOfThemIsACommand()
		{
			var offered = new List<string>();

			foreach (Type each in TheToolTypesOfTheServer())
				foreach (System.Reflection.MethodInfo method in each.GetMethods())
					foreach (object attribute in method.GetCustomAttributes(true))
						if (attribute.GetType().Name == "McpServerToolAttribute")
							offered.Add(
								(string)attribute.GetType().GetProperty("Name").GetValue(attribute));

			offered.Sort(StringComparer.Ordinal);

			Assert.That(offered, Is.EqualTo(new[]
			{
				"check_one_operation", "days_my_car_spent_in_the_shop", "does_the_warranty_cover",
				"how_much_my_car_is_driven", "how_up_to_date_is_this",
				"is_my_car_still_under_warranty", "review_my_maintenance",
				"what_has_been_done_to_my_car", "what_i_have_spent_on_my_car",
				"what_is_left_before_each_service", "what_my_car_takes", "what_plate_my_car_has",
				"what_the_service_costs",
			}), "Una tool que se agrega sin pensarla es como entra un comando a una superficie de "
				+ "solo lectura. La lista se escribe aca para que agregar una obligue a mirarla.");
		}

		// Y LAS CLASES QUE LAS LLEVAN TAMBIEN SE ESCRIBEN, por la misma razon: son los sitios que
		// Program.cs tiene que registrar, y una que aparezca sin que nadie la registre queda invisible
		// para la prueba de arriba y para el asistente. La cuenta va en el NOMBRE de la prueba, que es
		// donde se puede mover a proposito; en esta prosa se quedaba vieja sin ponerse roja.
		//
		// LA QUINTA ES OD-4 Y ES UNA DECISION, NO UN SITIO DONDE CAYO. DEC-40 dice que la frontera de
		// clase en esta caratula es DE QUE HABLA LA RESPUESTA, y cuanto se maneja un carro no es una
		// pregunta del plan de mantenimiento: las cinco de DealershipCarTools contestan donde esta
		// parado el PLAN -- que falta, que se hizo, que tan al dia esta -- y esta contesta un hecho de
		// USO que el plan resulta guardar. Meterla ahi habria hecho de esa clase dos sujetos sin que
		// nada se pusiera rojo, porque esa clase no lleva ningun /// que se pudiera volver falso.
		//
		// LA SEXTA ES CL-8 Y ES LA MISMA DECISION TOMADA OTRA VEZ. En que se le fue la plata a esta
		// persona no es una pregunta del plan de mantenimiento, ni de la ficha del fabricante, ni del
		// sobre, ni de las estadias, ni del odometro: es un hecho de GASTO, que ninguna de las cinco
		// clases que ya estaban contesta. Y a diferencia de DealershipCarTools, esta clase SI lleva un
		// /// que decir de que habla, asi que meterle una segunda pregunta lo volveria falso.
		//
		// LA SEPTIMA ES LP-4 Y LA DECISION SE TOMO POR ESCRITO EN EL PLAN. La placa NO es un campo de
		// la tool del sobre ni de ninguna otra: doblarla adentro de una pregunta de garantia haria que
		// poder leer la placa dependiera de una pregunta de garantia, y ademas la descripcion de la
		// tool ES el mecanismo con el que el asistente se entera de para que sirve una placa -- y una
		// descripcion necesita una tool de la que colgar.
		[Test]
		public void TheToolsLiveInExactlySevenClassesAndAllOfThemAreAboutReading()
		{
			var named = new List<string>();

			foreach (Type each in TheToolTypesOfTheServer()) named.Add(each.Name);

			named.Sort(StringComparer.Ordinal);

			Assert.That(named, Is.EqualTo(new[]
			{
				"CostLedgerTools", "DealershipCarTools", "LicensePlateTools",
				"ModelSpecificationTools", "OdometerSeriesTools", "ShopStayTools",
				"WarrantyEnvelopeTools",
			}), "Una clase de tools nueva tiene que pasar por aca, porque tiene que pasar tambien "
				+ "por el .WithTools de Program.cs y esto es lo unico que lo recuerda.");
		}

		// ------------------------------------------------------ el aparejo

		private static List<Type> TheToolTypesOfTheServer()
		{
			var found = new List<Type>();

			foreach (Type each in typeof(DealershipCarTools).Assembly.GetTypes())
				foreach (object attribute in each.GetCustomAttributes(true))
					if (attribute.GetType().Name == "McpServerToolTypeAttribute")
						found.Add(each);

			return found;
		}

		private static DealershipCarTools TheAssistantsToolsOver(string folder)
		{
			return new DealershipCarTools(new JournalReader(folder, ""));
		}

		private static ModelSpecificationTools TheAssistantsSheetToolsOver(string folder)
		{
			return new ModelSpecificationTools(new JournalReader(folder, ""));
		}

		private static WarrantyEnvelopeTools TheAssistantsEnvelopeToolsOver(string folder)
		{
			return new WarrantyEnvelopeTools(new JournalReader(folder, ""));
		}

		private static ShopStayTools TheAssistantsStayToolsOver(string folder)
		{
			return new ShopStayTools(new JournalReader(folder, ""));
		}

		private static OdometerSeriesTools TheAssistantsSeriesToolsOver(string folder)
		{
			return new OdometerSeriesTools(new JournalReader(folder, ""));
		}

		private static CostLedgerTools TheAssistantsLedgerToolsOver(string folder)
		{
			return new CostLedgerTools(new JournalReader(folder, ""));
		}

		private static AScheduleOnARealJournal AVehicleWithAPlanAndAnEvaluation()
		{
			var journal = new AScheduleOnARealJournal(TheVin, "Standard");

			journal.Api.SeedPlanOperation(
				"FluidChange", "TimeOrDistance", 365, 10000, "UnitA", true, false,
				new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc), 12000, "UnitA");
			journal.Api.SeedPlanOperation(
				"Inspection", "ConditionOnly", 0, 0, "UnitA", true, true,
				new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc), 12000, "UnitA");
			journal.Api.RecordReading(
				24000, "UnitA", new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), "Telemetry");
			journal.Api.Evaluate(new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc));

			// Y UNA REVISION SELLADA CON CONTENIDO, para que las tres tools de la ficha recorran algo
			// publicado. Sobre una ficha vacia todas ellas cortan temprano y la promesa de que leer no
			// escribe se estaria comprobando sobre los recorridos que nunca se hacen.
			ARevisionWithSomethingInEveryPlaceTheSheetToolsLook(journal);

			// Y EL SOBRE DE ESTE VEHICULO, POR LO MISMO. Sin sobre y sin entrega, la tool de garantia
			// no llega a ninguna de sus lecturas guardadas ni a las compuestas, y la promesa se
			// estaria comprobando sobre el camino corto.
			journal.Api.PublishWarrantyEnvelope(
				240, 100000, "UnitA", new DateTime(2025, 8, 15, 0, 0, 0, DateTimeKind.Utc));
			journal.Api.RecordDelivery(
				new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc), 12000, "UnitA");

			// Y DOS ESTADIAS, UNA CERRADA Y UNA ABIERTA, POR LO MISMO. Sin ninguna, la tool de las
			// estadias contesta la ausencia por el camino corto y la promesa de que leer no escribe se
			// estaria comprobando sobre los recorridos que nunca se hacen. La abierta ademas es el
			// unico estado que llega a las dos lecturas por instante, y esta fechada en 2026 -- antes
			// del reloj de cualquier corrida -- porque la tool lee el reloj del anfitrion.
			journal.Api.RecordShopStay(
				"Taller Central", "Cambio de frenos",
				new DateTime(2026, 5, 11, 8, 30, 0, DateTimeKind.Utc),
				new DateTime(2026, 5, 13, 16, 45, 0, DateTimeKind.Utc),
				new[] { "2026-05-11", "2026-05-12", "2026-05-13" });

			journal.Api.RecordShopEntry(
				"Taller Escazú", "Ruido en la suspensión",
				new DateTime(2026, 5, 20, 9, 0, 0, DateTimeKind.Utc));

			return journal;
		}

		private static void ARevisionWithSomethingInEveryPlaceTheSheetToolsLook(
			AScheduleOnARealJournal journal)
		{
			DateTime published = new DateTime(2025, 8, 15, 0, 0, 0, DateTimeKind.Utc);

			journal.Api.OpenRevision("MY2019-R1", published);
			journal.Api.PublishFluidSpecification(
				"MY2019-R1", "EngineOil", "5W-30 synthetic", 4.2m, "UnitA", 10000, "UnitA", 365);
			journal.Api.PublishTyreSpecification(
				"MY2019-R1", "Front", "205/55R16", 2.4m, "UnitA", "94V");
			journal.Api.PublishFuelGrade("MY2019-R1", "91 RON regular", "Minimum");
			journal.Api.PublishServiceInterval("MY2019-R1", "FluidChange", 10000, "UnitA", 365);
			journal.Api.PublishServicePrice(
				"MY2019-R1", "FluidChange", 10000, "UnitA", 89.90m, "USD",
				published, new DateTime(2099, 12, 31, 0, 0, 0, DateTimeKind.Utc));
			journal.Api.PublishCoveredSystem("MY2019-R1", "POWERTRAIN", 0, 0, "UnitA");
			journal.Api.PublishCoverageExclusion(
				"MY2019-R1", "POWERTRAIN", "wear parts", "when the part is a consumable");
			journal.Api.PublishServicingPolicy(
				"MY2019-R1",
				"service may be done at any qualified workshop",
				"keep the invoices and the parts records");
			journal.Api.SealRevision("MY2019-R1");
		}

		// LA CUENTA DE ENTRADAS SE LEE ABRIENDO EN SOLO LECTURA, no del actor que la prueba tiene
		// abierto: preguntarle al escritor mediria al escritor. Esto abre como abre la caratula.
		private static long TheEntryCountOf(string folder)
		{
			var reader = new ReadOnlyDealershipCarApp(TheVin);
			try
			{
				reader.ConfigureStorage(Puppeteer.DatabaseType.FileSystem, "path=" + folder);
				reader.Start();

				return reader.Actor.CurrentEntryId;
			}
			finally
			{
				try { reader.Actor.GracefulExit(); } catch { }
			}
		}

		private static long TheJournalLengthOf(string folder)
		{
			long total = 0;

			foreach (string file in Directory.GetFiles(
				Path.Combine(folder, DealershipCarApp.TheActorFor(TheVin), "journal"), "*.bin"))
				total += new FileInfo(file).Length;

			return total;
		}

		// Las sentencias que el actor pronuncio y los valores que llevaron, extraidas del binario:
		// las corridas imprimibles de seis caracteres o mas. Es el mismo extractor que usa
		// TheJournalReplaysTheSameReceivedSequenceTheSame, y por la misma razon (C23).
		//
		// SE SALTEA LA CABECERA DEL ARCHIVO a proposito -- los primeros 32 bytes -- porque es
		// justamente donde el motor deja su punto de control de replay a la septima apertura, y eso
		// no es una sentencia. No hay riesgo de tapar nada: ninguna sentencia empieza ahi.
		private static string TheSecondProgramOf(string folder)
		{
			var program = new StringBuilder();

			var files = new List<string>(
				Directory.GetFiles(folder, "*", SearchOption.AllDirectories));
			files.Sort(StringComparer.Ordinal);

			foreach (string file in files)
			{
				using (var open = new FileStream(
					file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
				{
					byte[] content = new byte[open.Length];
					open.ReadExactly(content, 0, content.Length);

					var run = new StringBuilder();

					for (int at = Math.Min(32, content.Length); at < content.Length; at++)
					{
						byte each = content[at];

						if (each >= 0x20 && each <= 0x7E) { run.Append((char)each); continue; }

						if (run.Length >= 6) program.AppendLine(run.ToString());

						run.Clear();
					}

					if (run.Length >= 6) program.AppendLine(run.ToString());
				}
			}

			return program.ToString();
		}
	}
}
