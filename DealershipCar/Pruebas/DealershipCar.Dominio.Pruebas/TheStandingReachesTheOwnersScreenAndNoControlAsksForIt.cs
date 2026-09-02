using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using DealershipCar.App;
using DealershipCar.Wasm;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// SE LLAMABA TheCoverageStandingNeverReachesTheOwnersScreen, Y ESE NOMBRE ENUNCIA UNA LEY QUE T10
	// VUELVE FALSA. En esta suite el archivo se llama como su clase y la clase es la ley, asi que el
	// nombre se movio con el hecho: la postura SI llega a la pantalla, y lo que sigue siendo cierto es
	// la otra mitad -- que ningun control de la pantalla le pide a la persona un dato de su carro. Se
	// movio con git mv y ninguna prueba se borro: unas sobreviven enteras, otras se dieron vuelta.
	//
	// LO QUE CAMBIO DEBAJO DEL NOMBRE. La adjudicacion vieja decia que el browser no podia contestar
	// donde esta parado el vehiculo por tres razones, y la tercera -- que la frase del dominio FUNDE
	// las dos mitades -- era la que decidia. Ninguna de las tres se retiro. Lo que cambio es que los
	// hechos que a la mitad personal le faltaban YA ESTAN EN EL REGISTRO: la agencia los manda por
	// comando y por feed, y la puerta compone la frase sin pedirle nada a nadie. El arreglo que la
	// adjudicacion vieja nombraba no era "preguntarle a la persona" sino "registrar el hecho", y
	// alguien lo registro.
	//
	// LO QUE NO CAMBIO, Y ESTAS PRUEBAS SON LO QUE LO SOSTIENE:
	//
	//   ningun control de la pantalla recoge un hecho del vehiculo -- ni un campo, ni un desplegable,
	//   ni un selector de fecha. La app del dueno es de SOLO LECTURA y esa regla trata de quien DICE
	//   un hecho, no de quien escribe un journal, asi que un formulario que guardara su valor en el
	//   estado de la pagina la violaria igual;
	//
	//   ningun miembro publico de AppState toma una MEDICION. El censo de la puerta enumera los
	//   parametros de DealershipCarAPI y se quedaria verde con un formulario en la pantalla, porque es
	//   otra superficie -- exactamente el hueco que T8 tuvo que tapar con un censo propio en la suya;
	//
	//   y las cinco lecturas emparejadas de nueve argumentos siguen sin tener quien las llame aca. Se
	//   quedan en la puerta porque quien MONTA UN ESCENARIO las puede pedir, que es la consola. Esta
	//   pantalla contesta sobre un carro de verdad y usa las compuestas, que sacan los hechos del
	//   registro.
	//
	// Y DE LOS TRES CONTEOS DE LA REGLA DE SERVICIO EN OTRO TALLER SIGUEN FALTANDO DOS, asi que esa
	// mitad se queda exactamente donde estaba: el texto publicado se cita entero y la posicion de este
	// vehiculo no viaja.
	[TestFixture]
	public sealed class TheStandingReachesTheOwnersScreenAndNoControlAsksForIt
	{
		private const string TheVin = "1HGCM82633A004352";

		private const string TheRevision = "MY2025-R1";
		private const string TheSystemInside = "POWERTRAIN";

		private const string TheRuleAsPublished = "service may be done at any qualified workshop";
		private const string WhatPreservesCover = "keep the invoices and the parts records";

		// SE PUBLICA EN UnitB A PROPOSITO: 'mi' en la pantalla es la prueba visible de que la unidad
		// cruzo tal cual, y una conversion escondida en cualquier capa cambia ese texto.
		private const string TheUnit = "UnitB";

		private const int TheMonths = 36;
		private const int TheDistance = 60000;

		private static readonly DateTime ThePublication =
			new DateTime(2025, 8, 15, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheDelivery =
			new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheDayTheReadingWasObserved =
			new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheDayTheVehicleAsks =
			new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);

		private AScheduleOnARealJournal journal;

		[SetUp]
		public void ASealedScopeAPublishedEnvelopeADeliveryAndAReading()
		{
			journal = new AScheduleOnARealJournal(TheVin, AppState.TheProfileAtDelivery);

			journal.Api.OpenRevision(TheRevision, ThePublication);
			journal.Api.PublishCoveredSystem(TheRevision, TheSystemInside, 0, 0, TheUnit);
			journal.Api.PublishCoverageExclusion(
				TheRevision, TheSystemInside, "wear parts", "when the part is a consumable");
			journal.Api.PublishServicingPolicy(TheRevision, TheRuleAsPublished, WhatPreservesCover);
			journal.Api.SealRevision(TheRevision);

			journal.Api.PublishWarrantyEnvelope(TheMonths, TheDistance, TheUnit, ThePublication);
			journal.Api.RecordDelivery(TheDelivery, 120, TheUnit);
			journal.Api.RecordReading(11200, TheUnit, TheDayTheReadingWasObserved, "Telemetry");
		}

		[TearDown]
		public void TearDown()
		{
			journal.Dispose();
		}

		// ===== MITAD 1: LA POSTURA SI LLEGA ===============================================

		// LA PRUEBA QUE SE DIO VUELTA. Antes afirmaba que NINGUNA de las cinco posturas aparecia en
		// nada de lo que la pantalla recibe. Ahora afirma que si aparece -- y no alcanza con que
		// aparezca una: se comprueba que la que llega sea la DEL REGISTRO, montando el espejo. Un
		// vehiculo pasado de su limite de distancia contesta OTRA, asi que esto no puede estar pasando
		// contra una constante.
		[Test]
		public void TheStandingTheScreenIsHandedIsTheRecordsOwnAndNotAConstant()
		{
			WhereThisVehicleStands stands = WhatTheScreenIsHandedAt(TheDayTheVehicleAsks);

			Assert.That(stands.CanBeEvaluated, Is.True);
			Assert.That(stands.Standing, Is.EqualTo("Inside"));

			Assert.That(TheFiveCoverageStandingsTheDomainDeclares(),
				Does.Contain(TheSystemRow(stands, TheSystemInside).Standing));
			Assert.That(TheSystemRow(stands, TheSystemInside).Standing,
				Is.EqualTo("CoveredWithExclusion"));

			using (var past = AnotherVehicleWhoseOdometerReads(90000))
			{
				WhereThisVehicleStands mirror = WhereThisVehicleStands.ReadThrough(
					past.Api, TheDayTheVehicleAsks);

				Assert.That(mirror.Standing, Is.EqualTo("Reached"));
				Assert.That(mirror.Standing, Is.Not.EqualTo(stands.Standing),
					"Dos registros distintos, dos posturas distintas. Sin este renglon, una lectura "
					+ "clavada en un nombre pasaria la mitad de arriba.");

				Assert.That(TheSystemRow(mirror, TheSystemInside).Standing,
					Is.EqualTo("OutsideEnvelope"),
					"Y la de cobertura se mueve con el mismo registro.");
			}
		}

		// CADA FRASE QUE LA PANTALLA MUESTRA ES LA DEL DOMINIO, LETRA POR LETRA. La caratula formatea y
		// no vuelve a derivar: si compusiera una tercera con pedazos de las otras, o recalculara una
		// mitad, esto se pondria rojo. Es la unica garantia que hay de que lo que la persona lee salio
		// del dominio y no de un renglon de markup.
		[Test]
		public void EverySentenceTheScreenShowsIsTheDoorsOwnLetterForLetter()
		{
			WhereThisVehicleStands stands = WhatTheScreenIsHandedAt(TheDayTheVehicleAsks);

			Assert.That(stands.Says,
				Is.EqualTo(journal.Api.DescribeWarrantyEnvelope(TheDayTheVehicleAsks)));
			Assert.That(stands.PublishedSays,
				Is.EqualTo(journal.Api.DescribePublishedWarrantyEnvelope()));
			Assert.That(TheSystemRow(stands, TheSystemInside).Says,
				Is.EqualTo(journal.Api.DescribeCoverageForThisVehicle(
					TheSystemInside, TheDayTheVehicleAsks)));
		}

		// LA UNIDAD CRUZA TAL CUAL Y LA PANTALLA NO OFRECE CON QUE CONVERTIRLA. El sobre se publico en
		// la otra unidad a proposito, asi que 'mi' en la frase y en el renglon de procedencia es la
		// prueba visible; si saliera en km, alguien convirtio, y una distancia convertida es contra la
		// que la persona decide si todavia esta cubierta.
		[Test]
		public void TheDistanceIsSaidInTheUnitItWasPublishedInAndNeverConverted()
		{
			WhereThisVehicleStands stands = WhatTheScreenIsHandedAt(TheDayTheVehicleAsks);

			Assert.That(stands.OdometerUnit, Is.EqualTo("mi"));
			Assert.That(stands.DeliveryUnit, Is.EqualTo("mi"));
			Assert.That(stands.Says, Does.Contain("60000 mi"));
			Assert.That(stands.Says, Does.Not.Contain(" km"),
				"Una sola aparicion de la otra unidad en esta frase quiere decir que se convirtio.");
		}

		// Y LA PANTALLA DICE CONTRA QUE SE MIDIO Y DE QUE DIA ES ESA LECTURA. Es la linea de honestidad
		// de la seccion: sin la fecha de observacion, una lectura de junio se lee como la de hoy, y la
		// distancia que queda es al menos tan vieja como ella.
		[Test]
		public void TheScreenNamesTheMomentItAskedAtAndTheDayTheReadingWasObserved()
		{
			WhereThisVehicleStands stands = WhatTheScreenIsHandedAt(TheDayTheVehicleAsks);

			Assert.That(stands.AskedAt, Is.EqualTo("2026-09-01 00:00:00Z"));
			Assert.That(stands.OdometerObservedAt, Is.EqualTo("2026-06-01"));
			Assert.That(stands.OdometerObservedAt, Is.Not.EqualTo(stands.AskedAt),
				"Y los dos instantes son distintos, que es lo unico que hace que nombrar los dos "
				+ "sirva de algo.");

			Assert.That(stands.Odometer, Is.EqualTo(11200));
			Assert.That(stands.DeliveredAt, Is.EqualTo("2025-09-01"));
			Assert.That(stands.PublishedAt, Is.EqualTo("2025-08-15"));
		}

		// LA MITAD PUBLICADA NUNCA SE PIERDE, Y AQUI ESTA MEDIDO POR QUE LA PANTALLA NO LA REPITE. La
		// frase compuesta CONTIENE la publicada, palabra por palabra, en todos los estados menos uno --
		// el registro que se contradice, donde la compuesta sale vacia. Por eso la seccion la muestra
		// suelta SOLO ahi: dos parrafos siempre presentes imprimirian la misma clausula dos veces en la
		// misma columna, que es lo que un modelo no ve y una persona si.
		[Test]
		public void TheComposedSentenceCarriesThePublishedOneInsideItInEveryStateButOne()
		{
			WhereThisVehicleStands whole = WhatTheScreenIsHandedAt(TheDayTheVehicleAsks);

			Assert.That(whole.Says, Does.Contain(whole.PublishedSays));

			using (var bare = ABareVehicle("1FTFW1ET5DFA00001"))
			{
				WhereThisVehicleStands nothing = WhereThisVehicleStands.ReadThrough(
					bare.Api, TheDayTheVehicleAsks);

				Assert.That(nothing.Says, Does.Contain(nothing.PublishedSays),
					"Sin sobre publicado tambien: la respuesta es NotPublished y su clausula ES la "
					+ "frase publicada.");
			}

			using (AScheduleOnARealJournal broken = AVehicleWhoseOdometerWentBackwards())
			{
				WhereThisVehicleStands contradictory = WhereThisVehicleStands.ReadThrough(
					broken.Api, TheDayTheVehicleAsks);

				Assert.That(contradictory.Says, Is.Empty);
				Assert.That(contradictory.PublishedSays, Is.Not.Empty,
					"Y es el unico estado en que la publicada tiene que salir sola, porque es el unico "
					+ "en que no hay ninguna compuesta que la lleve adentro.");
			}
		}

		// ===== MITAD 2: NINGUN CONTROL PIDE UN HECHO DEL VEHICULO =========================

		// EL CENSO QUE HACIA FALTA EN ESTA CARATULA, Y POR QUE NO ALCANZA EL DE LA PUERTA.
		// TheOwnerIsNeverASourceOfAFactAboutTheirOwnVehicle enumera los parametros de
		// DealershipCarAPI, y se quedaria VERDE con un campo de odometro recien puesto en esta
		// pantalla: un formulario guarda su valor en el estado de la pagina y no cruza ninguna puerta.
		// Es el mismo hueco que T8 midio en la suya y tapo con un censo de su propia superficie.
		//
		// SE ARMA POR ELIMINACION SOBRE LA FORMA Y NO POR VOCABULARIO DE NOMBRES: un control que
		// RECOGE UN VALOR de una persona es uno de los elementos de abajo -- los cuatro de HTML, el
		// enlace de dos vias con que MudBlazor ata el valor de cualquiera de sus campos, y la familia
		// de componentes de entrada por su nombre. Quien agregue el formulario elige la etiqueta, no
		// el tipo de control.
		//
		// LO QUE ESTE INSTRUMENTO NO VE, dicho y no disimulado: un control atado con el par
		// Value/ValueChanged en vez de con @bind-Value queda fuera de esa mitad de la regla -- lo
		// atrapa la lista de componentes -- y un control escrito a mano que no sea ninguno de los
		// cuatro de HTML ni lleve nombre de MudBlazor no lo ve ninguna de las dos. Queda escrito en vez
		// de tapado con un vocabulario que tendria mas huecos.
		private static readonly string[] EveryShapeThatCollectsAValueFromAPerson =
		{
			"<input", "<select", "<textarea", "<form",
			"@bind-Value",
			"MudTextField", "MudNumericField", "MudSelect", "MudAutocomplete",
			"MudDatePicker", "MudDateRangePicker", "MudTimePicker", "MudColorPicker",
			"MudCheckBox", "MudSwitch", "MudSlider", "MudRadio", "MudRating",
			"MudFileUpload", "MudForm", "MudToggleGroup",
		};

		[Test]
		public void NoControlAnywhereOnTheScreenCollectsAFactAboutThisVehicle()
		{
			var swept = new List<string>();

			foreach (string file in EveryMarkupFileTheShellHas())
			{
				string markup = File.ReadAllText(file);
				string name = Path.GetFileName(file);

				swept.Add(name);

				foreach (string shape in EveryShapeThatCollectsAValueFromAPerson)
					Assert.That(markup, Does.Not.Contain(shape),
						$"'{name}' lleva un control que recoge un valor ('{shape}'). La app del dueno "
						+ "es de SOLO LECTURA, y esa regla trata de quien DICE un hecho y no de quien "
						+ "escribe un journal: un formulario que guarde su valor en el estado de la "
						+ "pagina la viola igual. Los tres hechos que decidirian la garantia -- el "
						+ "sobre, la entrega y el odometro -- YA ESTAN en el registro y la puerta los "
						+ "contesta; si falta alguno, el arreglo es que la agencia lo mande.");
			}

			Assert.That(swept, Has.Count.GreaterThanOrEqualTo(7),
				"El barrido no encontro el markup. Se copia por glob para que una pagina nueva entre "
				+ "sola; si no hay ninguna, lo que fallo es la copia y no el markup.");

			Assert.That(swept, Contains.Item("Warranty.razor"));
			Assert.That(swept, Contains.Item("MyCar.razor"));

			// EL CONTROL DEL DETECTOR. Sin este renglon, un barrido con la lista vacia o con un
			// comparador roto pasaria diciendo exactamente lo mismo.
			int caught = 0;

			foreach (string shape in EveryShapeThatCollectsAValueFromAPerson)
				if (("<div>" + shape + " Label=\"Your odometer\" /></div>").Contains(shape)) caught++;

			Assert.That(caught, Is.EqualTo(EveryShapeThatCollectsAValueFromAPerson.Length));
		}

		// Y NINGUN MIEMBRO PUBLICO DE AppState TOMA UNA MEDICION. Es la otra puerta por la que un hecho
		// inventado entraria: no un control en la pantalla sino un argumento que la pagina le pasa a la
		// unica cosa que habla con la puerta. Se discrimina por el TIPO y no por el nombre, igual que
		// en la puerta: los NOMBRES de esta superficie son cadenas -- una clase de operacion, un
		// fluido, una posicion de llanta -- y las MEDICIONES son numeros e instantes.
		//
		// EL CONJUNTO SE AFIRMA VACIO, asi que un parametro nuevo se pone rojo con CUALQUIER nombre. Y
		// el instante en que la pantalla pregunta NO es una excepcion: lo lee AppState del reloj de la
		// maquina y no se lo pide a nadie, que es la diferencia entre preguntar por hoy y dejar que
		// alguien elija el dia.
		[Test]
		public void NoPublicMemberOfTheShellAsksForAMeasurement()
		{
			Assert.That(EveryMeasurementTheShellAsksFor(), Is.Empty,
				"Un miembro publico de AppState gano un parametro que es una MEDICION -- un numero o "
				+ "un instante. ESTA ES LA PRUEBA QUE SE PONE ROJA EL DIA QUE ALGUIEN AGREGA EL "
				+ "PARAMETRO DE COMODIDAD, con cualquier nombre. El registro ya lleva los tres hechos; "
				+ "la lectura los saca de ahi en vez de pedirlos.");

			// EL CONTROL, y sin el lo de arriba estaria vacio por estar mirando mal. El mismo
			// predicado sobre la lectura de nueve argumentos de la puerta tiene que encontrar SEIS.
			var found = new List<string>();

			foreach (ParameterInfo each in typeof(DealershipCarAPI)
				.GetMethod("DescribeCoverage").GetParameters())
				if (IsAMeasurement(each)) found.Add(each.Name);

			Assert.That(found, Has.Count.EqualTo(6),
				"El predicado dejo de reconocer una medicion, asi que el conjunto vacio de arriba no "
				+ "dice nada.");
		}

		// LA CARATULA NO TIENE POR DONDE CONSEGUIR UNA POSTURA CON HECHOS QUE ELLA MISMA INVENTE. La
		// puerta es UNA: AppState la guarda privada, asi que un componente solo puede llamar lo que
		// AppState expone -- y las cinco lecturas emparejadas de nueve argumentos no estan expuestas.
		//
		// SE ENUMERAN LOS SITIOS POR DONDE LA REGLA PODRIA ROMPERSE y no los sustantivos de los que
		// trata: todo miembro publico del ensamblado que ENTREGUE un DealershipCarAPI abriria una
		// segunda puerta, y todo miembro publico de AppState que nombre una de las cinco emparejadas
		// seria la primera.
		[Test]
		public void TheShellHandsOutNoDoorAndOffersNoneOfTheFivePairedReads()
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
					+ "vehiculo que quien la llama tiene que traer, y esta pantalla contesta sobre un "
					+ "carro de verdad. La compuesta del mismo nombre terminada en ForThisVehicle es "
					+ "la que saca los hechos del registro, y es la que se usa.");

			// Y LAS CINCO SIGUEN EN LA PUERTA, contestando, para quien MONTA UN ESCENARIO -- que es la
			// consola. Sin este renglon, borrarlas dejaria la prueba de arriba verde y mas fuerte de lo
			// que es.
			Assert.That(
				journal.Api.ReadCoverageStanding(
					TheSystemInside, TheMonths, TheDistance, TheUnit, TheDelivery, 120, 11200, TheUnit,
					TheDayTheVehicleAsks),
				Is.EqualTo("CoveredWithExclusion"));
		}

		// ===== LO QUE SIGUE FALTANDO: LOS DOS CONTEOS DE SERVICIO EXTERNO =================

		// EL TEXTO DE LA REGLA LLEGA Y NINGUN CONTEO. Es la mitad que NO se movio en T10: dos de los
		// tres conteos no existen en ninguna lectura del modulo, asi que la posicion de este vehiculo
		// frente a la regla sigue sin viajar. Uno de tres no es la frase.
		[Test]
		public void TheServicingRuleTextIsOnTheSheetAndNoCountIs()
		{
			WhatTheManufacturerPublished sheet = TheSheetTheScreenShows();

			Assert.That(sheet.ServicingRule.Published, Is.True);
			Assert.That(sheet.ServicingRule.Revision, Is.EqualTo(TheRevision));

			Assert.That(sheet.ServicingRule.Says, Does.Contain(TheRuleAsPublished),
				"La mitad publicada tiene que llegar entera: es la que la pantalla dejo de poder "
				+ "mostrar cuando la unica lectura que la componia pedia los tres conteos.");
			Assert.That(sheet.ServicingRule.Says, Does.Contain(TheRevision),
				"Y con su procedencia adentro, que es lo que la separa de un folleto.");

			foreach (string said in EveryWordTheScreenIsHanded())
				Assert.That(said, Does.Not.Contain("servicios externos con comprobante"),
					"Un conteo llego a la pantalla. Dos de los tres no existen en ninguna lectura, "
					+ "asi que un numero ahi lo invento alguien.");

			// EL CONTROL: la puerta SI compone la frase CON los conteos cuando se los mandan, asi que
			// el barrido de arriba esta mirando una diferencia real y no una imposibilidad.
			string whole = journal.Api.DescribeServicingElsewhereRule(2, 1, 0);

			Assert.That(whole, Does.Contain(TheRuleAsPublished));
			Assert.That(whole, Does.Contain("servicios externos con comprobante 2"));
			Assert.That(sheet.ServicingRule.Says, Is.Not.EqualTo(whole),
				"Y las dos frases son distintas. Si fueran iguales, la hoja estaria trayendo la "
				+ "emparejada y el barrido no tendria nada que encontrar.");
		}

		// Y LA FORMA DEL PORTADOR LO SIGUE SOSTENIENDO: hay sitio para UNA frase, la publicada, y no lo
		// hay para un conteo. Un cuarto campo aca es la puerta por la que entrarian, y esta prueba
		// obliga a abrirla a proposito.
		[Test]
		public void TheServicingRuleCarrierHasRoomForTheSentenceAndNoneForACount()
		{
			var names = new List<string>();

			foreach (PropertyInfo each in typeof(PublishedServicingRule).GetProperties())
				names.Add(each.Name);

			Assert.That(names, Is.EquivalentTo(new[] { "Published", "Says", "Revision" }));
		}

		// Y SIN REGLA PUBLICADA NO SE ANUNCIA NADA. Sin esto, el anuncio de arriba podria ser una
		// constante que sale siempre y no diria nada de lo publicado.
		[Test]
		public void AndWithNoServicingRulePublishedNothingIsAnnouncedAboutIt()
		{
			using (var bare = ABareVehicle("1FTFW1ET5DFA00001"))
			{
				WhatTheManufacturerPublished sheet =
					WhatTheManufacturerPublished.ReadThrough(bare.Api);

				Assert.That(sheet.ServicingRule.Published, Is.False);
				Assert.That(sheet.ServicingRule.Revision, Is.Empty);
			}
		}

		// ===== LAS PALABRAS QUE LA PANTALLA PONE DE SU COSECHA ============================

		// NINGUNA DE LAS CINCO POSTURAS APARECE EN LO QUE LA CARATULA DICE POR SU CUENTA. Es la mitad
		// que sigue igual y por la misma razon: una postura es una palabra del DOMINIO, citada de la
		// puerta, y una caratula que escribiera una la estaria inventando.
		[Test]
		public void TheWordsTheScreenAddsOfItsOwnCarryNoStandingEither()
		{
			List<string> said = EveryWordTheScreenAddsOfItsOwn();

			// SE MIRO DE NUEVO, que es lo que este renglon pide. Eran cuatro y son CINCO: la frase que
			// decia que la posicion no estaba en el registro se fue con la ley que la sostenia, y
			// entraron dos -- quien manda un hecho que todavia no llego, y que hacer cuando el registro
			// se contradice consigo mismo.
			Assert.That(said, Has.Count.EqualTo(5),
				"Si TheOwnerIsTold gana o pierde una frase, este barrido se vuelve a mirar en vez de "
				+ "seguir contando las de antes.");

			foreach (string standing in TheFiveCoverageStandingsTheDomainDeclares())
				foreach (string sentence in said)
					Assert.That(sentence, Does.Not.Contain(standing));

			foreach (string standing in TheThreeEnvelopeStandingsTheDomainDeclares())
				foreach (string sentence in said)
					Assert.That(sentence, Does.Not.Contain(standing));
		}

		// LA FRASE VIEJA SE FUE CON LA LEY VIEJA, Y LA NUEVA DICE LO QUE UNA PANTALLA TIENE QUE DECIR.
		// La vieja afirmaba que el registro no llevaba NINGUNO de los cuatro hechos, y eso es falso
		// desde que la agencia los manda; peor, esta prueba afirmaba la frase entera, asi que la
		// obsolescencia estaba SOSTENIDA por una prueba.
		//
		// Y CADA RENGLON AFIRMA LA CLAUSULA ENTERA Y NO EL SUSTANTIVO. Nombrar el sustantivo no
		// defiende nada: "Your dealership is not asked for them" contiene "Your dealership" y dice lo
		// contrario de lo que hay que decir.
		[Test]
		public void TheScreenSaysWhoSendsAFactThatHasNotArrivedAndNeverGuessesIt()
		{
			foreach (string sentence in EveryWordTheScreenAddsOfItsOwn())
				Assert.That(sentence, Does.Not.Contain("no lleva ninguno de los cuatro"),
					"La frase que decia que el registro no lleva ninguno de los cuatro hechos sigue "
					+ "ahi, y es falsa desde que la agencia los manda por comando y por feed.");

			Assert.That(
				TheOwnerIsTold.TheDealershipHasNotSentTheseFactsYet,
				Does.Contain("el sobre de garantía mismo, el día en que se entregó el carro, o una "
					+ "lectura del odómetro tomada hoy o antes"),
				"Los tres hechos, dichos enteros. Con solo la palabra 'odometer' adentro, una frase "
				+ "que dijera que el odometro no hace falta pasaria igual.");
			Assert.That(
				TheOwnerIsTold.TheDealershipHasNotSentTheseFactsYet,
				Does.Contain("no hay nada que tengas que llenar"),
				"Y que no hay nada que la persona ponga, dicho entero: es la mitad que un formulario "
				+ "contradice, y es la que se pierde si esto solo comprobara que aparece la palabra "
				+ "'dealership'.");
			Assert.That(
				TheOwnerIsTold.TheDealershipHasNotSentTheseFactsYet,
				Does.Contain("no es lo mismo que un carro sin garantía"),
				"Y la lectura equivocada mas cara de todas, dicha en voz alta.");

			// LO QUE SE LE DICE CUANDO EL REGISTRO SE CONTRADICE: que ninguno de los dos hechos lo puso
			// la persona, y que solo la agencia los corrige. Sin la primera mitad, la pantalla le pide
			// a la persona que arregle algo que no rompio.
			Assert.That(
				TheOwnerIsTold.TheRecordSaysTwoThingsThatCannotBothBeTrue,
				Does.Contain("Ninguno de los dos hechos vino de vos"));
			Assert.That(
				TheOwnerIsTold.TheRecordSaysTwoThingsThatCannotBothBeTrue,
				Does.Contain("Solo tu concesionario puede corregirlos"));
			Assert.That(
				TheOwnerIsTold.TheRecordSaysTwoThingsThatCannotBothBeTrue,
				Does.Contain("nada de lo de arriba es una estimación de eso"),
				"Y que lo que se muestra no es una estimacion de la postura, que es la unica forma de "
				+ "fallar aca que le cuesta dinero a la persona.");

			// LA REGLA DE SERVICIO EN OTRO TALLER NO SE MOVIO: le sigue faltando la POSICION, y la
			// pantalla lo sigue diciendo con los tres conteos nombrados enteros.
			Assert.That(
				TheOwnerIsTold.TheServicingRuleStandingIsNotInThisRecord,
				Does.Contain("no dónde está parado tu carro frente a ella"));
			Assert.That(
				TheOwnerIsTold.TheServicingRuleStandingIsNotInThisRecord,
				Does.Contain("cuántos servicios ha tenido este carro en otro lado, cuántos de esos "
					+ "tienen comprobante, y cuánto mantenimiento programado se dejó de hacer"));
			Assert.That(
				TheOwnerIsTold.TheServicingRuleStandingIsNotInThisRecord,
				Does.Contain("Quien los pone aquí es tu concesionario"));

			// Y QUE UN SISTEMA QUE LA LISTA NO NOMBRA NO ES UNO QUE EL ALCANCE DEJE AFUERA. En una
			// pantalla sin buscador esa distincion no la puede hacer un control: solo la frase.
			Assert.That(
				TheOwnerIsTold.ASystemThatIsNotListedWasNotRuledOut,
				Does.Contain("no quedó descartado aquí"));
		}

		// ===== EL BARRIDO LLEGA A LAS HOJAS ==============================================

		// SIN ESTE RENGLON NO SE SABRIA: un recorredor que no entrara a las listas ni a los portadores
		// anidados devolveria un punado de cadenas y pasaria los barridos de arriba sin haber mirado
		// nada.
		[Test]
		public void TheSweepReachesTheLeavesOfBothCarriersAndNotOnlyTheirTops()
		{
			List<string> words = EveryWordTheScreenIsHanded();

			Assert.That(words, Has.Count.GreaterThanOrEqualTo(35),
				"La hoja lleva seis fluidos por tres, tres llantas por tres, las cadenas del "
				+ "encabezado, el combustible, el alcance y la regla, y ademas la mitad del vehiculo "
				+ "con su fila por sistema.");

			Assert.That(words, Has.Some.Contains("EngineOil"), "No entro a la lista de fluidos.");
			Assert.That(words, Has.Some.Contains("Llanta de repuesto"), "No entro a la lista de llantas.");
			Assert.That(words, Has.Some.Contains(TheSystemInside), "No entro al arreglo de sistemas.");
			Assert.That(words, Has.Some.Contains("wear parts"), "No entro al arreglo de exclusiones.");
			Assert.That(words, Has.Some.Contains(TheRevision), "No leyo ninguna procedencia.");
			Assert.That(words, Has.Some.Contains("CoveredWithExclusion"),
				"No entro a la lista de sistemas de la mitad del vehiculo, que es la que T10 agrego.");
		}

		// ------------------------------------------------------ el aparejo

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

		private WhereThisVehicleStands WhatTheScreenIsHandedAt(DateTime asOf)
		{
			return WhereThisVehicleStands.ReadThrough(journal.Api, asOf);
		}

		private static CoverageStandingForThisVehicle TheSystemRow(
			WhereThisVehicleStands stands, string systemCode)
		{
			foreach (CoverageStandingForThisVehicle each in stands.Systems)
				if (each.SystemCode == systemCode) return each;

			Assert.Fail($"la mitad del vehiculo no trae fila para '{systemCode}'.");
			return null;
		}

		private AScheduleOnARealJournal AnotherVehicleWhoseOdometerReads(int reading)
		{
			var another = new AScheduleOnARealJournal("JTDKN3DU0A0123456", AppState.TheProfileAtDelivery);

			another.Api.OpenRevision(TheRevision, ThePublication);
			another.Api.PublishCoveredSystem(TheRevision, TheSystemInside, 0, 0, TheUnit);
			another.Api.PublishCoverageExclusion(
				TheRevision, TheSystemInside, "wear parts", "when the part is a consumable");
			another.Api.SealRevision(TheRevision);

			another.Api.PublishWarrantyEnvelope(TheMonths, TheDistance, TheUnit, ThePublication);
			another.Api.RecordDelivery(TheDelivery, 120, TheUnit);
			another.Api.RecordReading(reading, TheUnit, TheDayTheReadingWasObserved, "Telemetry");

			return another;
		}

		// LA LECTURA DE LA ENTREGA POR ENCIMA DEL ODOMETRO DE HOY. Se llega con UN comando legitimo:
		// registrar la entrega es CORRECTIVO, asi que una agencia que corrige hacia arriba la lectura
		// del dia de la entrega deja el registro asi, y desde T9 tambien lo deja asi un feed.
		private AScheduleOnARealJournal AVehicleWhoseOdometerWentBackwards()
		{
			var broken = new AScheduleOnARealJournal("WAUZZZ8V5KA000001", AppState.TheProfileAtDelivery);

			broken.Api.PublishWarrantyEnvelope(TheMonths, TheDistance, TheUnit, ThePublication);
			broken.Api.RecordDelivery(TheDelivery, 50000, TheUnit);
			broken.Api.RecordReading(9000, TheUnit, TheDayTheReadingWasObserved, "Telemetry");

			return broken;
		}

		private static AScheduleOnARealJournal ABareVehicle(string vin)
		{
			return new AScheduleOnARealJournal(vin, AppState.TheProfileAtDelivery);
		}

		// Cada CADENA que la pantalla recibe, de las DOS hojas, entrando a las listas y a los
		// portadores anidados. Los nombres de propiedad no entran: lo que se afirma es sobre las
		// RESPUESTAS y no sobre como se llama un campo.
		private List<string> EveryWordTheScreenIsHanded()
		{
			var words = new List<string>();

			Collect(TheSheetTheScreenShows(), words);
			Collect(WhatTheScreenIsHandedAt(TheDayTheVehicleAsks), words);

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

		// Las frases que la caratula pone de su cosecha, enumeradas del propio tipo para que una nueva
		// entre al barrido sola.
		private static List<string> EveryWordTheScreenAddsOfItsOwn()
		{
			var said = new List<string>();

			foreach (FieldInfo each in typeof(TheOwnerIsTold).GetFields(
				BindingFlags.Static | BindingFlags.Public))
				if (each.FieldType == typeof(string)) said.Add((string)each.GetValue(null));

			return said;
		}

		private static List<string> TheFiveCoverageStandingsTheDomainDeclares()
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

		private static List<string> TheThreeEnvelopeStandingsTheDomainDeclares()
		{
			var names = new List<string>();

			foreach (FieldInfo each in typeof(EnvelopeStanding).GetFields(
				BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
				if (each.FieldType == typeof(EnvelopeStanding))
					names.Add(((EnvelopeStanding)each.GetValue(null)).Name);

			Assert.That(names, Has.Count.EqualTo(3),
				"Si no son tres, este barrido dejo de enumerar el conjunto que T10 puso en la pantalla.");

			return names;
		}

		private static List<string> EveryMeasurementTheShellAsksFor()
		{
			var found = new List<string>();

			foreach (MethodInfo each in typeof(AppState).GetMethods(ThePublicOnes))
			{
				ParameterInfo[] parameters = each.GetParameters();

				foreach (ParameterInfo parameter in parameters)
					if (IsAMeasurement(parameter))
						found.Add(each.Name + "/" + parameters.Length + "(" + parameter.Name + ")");
			}

			return found;
		}

		private static bool IsAMeasurement(ParameterInfo parameter)
		{
			return parameter.ParameterType == typeof(int)
				|| parameter.ParameterType == typeof(DateTime);
		}

		private static string[] EveryMarkupFileTheShellHas()
		{
			string folder = Path.Combine(AppContext.BaseDirectory, "MarcadoComoDato");

			Assert.That(Directory.Exists(folder), Is.True,
				"El markup de la caratula no llego al output.");

			return Directory.GetFiles(folder, "*.razor", SearchOption.AllDirectories);
		}
	}
}
