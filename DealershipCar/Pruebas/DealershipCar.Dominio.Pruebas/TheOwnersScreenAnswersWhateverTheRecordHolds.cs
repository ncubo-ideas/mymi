using System;
using DealershipCar.Wasm;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LO QUE LA PANTALLA MUESTRA CUANDO AL REGISTRO LE FALTA UN HECHO, Y CUANDO SE CONTRADICE.
	//
	// UNA PERSONA Y UN MODELO FALLAN DISTINTO ANTE UN HUECO, y T10 volvio a adjudicar esto en vez de
	// heredar lo que T8 decidio para el asistente. Lo que T8 dio a un modelo son DOS campos de frase
	// siempre presentes, cada uno del dominio, y un campo aparte diciendo quien registra lo que falta:
	// un modelo lee campos con nombre y nunca los ve pegados, asi que repetir no le cuesta nada, y un
	// modelo que lee "no se pudo" rellena el hueco.
	//
	// UNA PANTALLA TIENE EL PROBLEMA CONTRARIO Y SE MIDIO CUAL ES. La frase compuesta CONTIENE la
	// publicada palabra por palabra en todos los estados menos uno, asi que dos parrafos siempre
	// presentes imprimirian la misma clausula dos veces en la misma columna -- que un modelo no ve y
	// una persona lee como un defecto. Lo que hace en una pantalla el trabajo que el nombre del campo
	// hace para un modelo es EL ENCABEZADO de cada bloque. Y el campo 'detail', que nadie lee en una
	// pantalla, se convierte en lo unico que una persona si necesita y que la frase del dominio no
	// dice: QUIEN manda el hecho que falta.
	//
	// Y ES EL CASO QUE AHORA ES RARO Y ANTES ERA EL NORMAL. Hasta T5 el registro no llevaba ninguno de
	// los cuatro hechos; despues de T9 los lleva todos, por comando y por feed. Un sobre vacio es un
	// carro cuya agencia todavia no lo mando, y asi se dice.
	[TestFixture]
	public sealed class TheOwnersScreenAnswersWhateverTheRecordHolds
	{
		private const string TheUnit = "UnitB";
		private const string TheSystem = "POWERTRAIN";
		private const string TheRevision = "WE2025-R1";

		private const int TheMonths = 36;
		private const int TheDistance = 60000;

		private static readonly DateTime ThePublication =
			new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheSecondPublication =
			new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheDelivery =
			new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		// UNA ENTREGA FECHADA EN EL FUTURO. Registrar la entrega es CORRECTIVO a proposito, asi que un
		// ano mal tecleado deja el registro contradiciendose para todo instante anterior a esa fecha.
		private static readonly DateTime TheDeliveryThatHasNotHappenedYet =
			new DateTime(2099, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheDayItWasObserved =
			new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheDayTheScreenAsks =
			new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);

		// ===== AL REGISTRO NO LE HA LLEGADO NADA =========================================

		// LAS DOS MITADES CONTESTAN Y NINGUNA HABLA DEL FALLO. Es lo que hace que esto se lea como una
		// respuesta y no como una disculpa: cada frase habla de su propio sujeto.
		[Test]
		public void WithNothingRecordedBothHalvesStillAnswerAndNeitherIsAboutTheFailure()
		{
			using (AScheduleOnARealJournal bare = ASealedScope("1FTFW1ET5DFA00001"))
			{
				WhereThisVehicleStands stands = WhatTheScreenIsHanded(bare);

				Assert.That(stands.EnvelopePublished, Is.False);
				Assert.That(stands.DeliveryRecorded, Is.False);
				Assert.That(stands.OdometerKnown, Is.False);
				Assert.That(stands.TheRecordIsMissingAFact, Is.True);

				// LA POSTURA SI SE DECIDE: que nadie publico un sobre es algo que el registro sabe con
				// certeza, y NotPublished es uno de los tres nombres y no un hueco.
				Assert.That(stands.CanBeEvaluated, Is.True);
				Assert.That(stands.Standing, Is.EqualTo("NotPublished"));

				Assert.That(
					stands.Says,
					Does.Contain("not the same as this vehicle having no warranty"),
					"La frase que mas facil se lee al reves, dicha por el dominio y no por la pantalla.");

				Assert.That(stands.PublishedSays, Is.Not.Empty);
				Assert.That(stands.Says, Does.Contain(stands.PublishedSays));

				Assert.That(stands.Systems, Is.Empty,
					"Sin los tres hechos no hay postura por sistema que mostrar: el catalogo del "
					+ "fabricante es de un MODELO y no sabe que le falta a un carro.");

				// Y LA MITAD PUBLICADA DEL ALCANCE CONTESTA ENTERA, porque no depende de ningun hecho
				// de este carro. Es la mitad que se pierde entera si la pantalla exigiera la personal.
				WhatTheManufacturerPublished sheet =
					WhatTheManufacturerPublished.ReadThrough(bare.Api);

				Assert.That(sheet.Coverage.Published, Is.True);
				Assert.That(sheet.Coverage.CoveredSystems, Is.EqualTo(new[] { TheSystem }));
				Assert.That(sheet.Coverage.Revision, Is.EqualTo(TheRevision));

				Assert.That(stands.Says, Does.Not.Contain("could not"));
				Assert.That(sheet.Coverage.Says, Does.Not.Contain("could not"));
			}
		}

		// EL SOBRE Y LA ENTREGA ESTAN Y LA LECTURA NO: la mitad de tiempo se mide igual y la de
		// distancia se dice AUSENTE en vez de cero. Un cero seria una lectura, y no hay ninguna.
		[Test]
		public void WithNoOdometerReadingTheTimeHalfIsMeasuredAndTheDistanceHalfIsNamedAbsent()
		{
			using (AScheduleOnARealJournal noReading = ASealedScope("1HGCM82633A004352"))
			{
				noReading.Api.PublishWarrantyEnvelope(240, 20000, TheUnit, ThePublication);
				noReading.Api.RecordDelivery(TheDelivery, 120, TheUnit);

				WhereThisVehicleStands stands = WhatTheScreenIsHanded(noReading);

				Assert.That(stands.EnvelopePublished, Is.True);
				Assert.That(stands.DeliveryRecorded, Is.True);
				Assert.That(stands.OdometerKnown, Is.False);
				Assert.That(stands.TheRecordIsMissingAFact, Is.True);

				Assert.That(stands.Odometer, Is.EqualTo(0),
					"Cero con OdometerKnown en false NO es una lectura de cero, y por eso la pantalla "
					+ "no pinta ese renglon.");
				Assert.That(stands.OdometerObservedAt, Is.Empty);

				Assert.That(stands.CanBeEvaluated, Is.False);
				Assert.That(stands.Standing, Is.Empty,
					"Ninguno de los tres nombres quiere decir 'no se pudo medir', asi que no se "
					+ "devuelve uno -- y la pantalla no pinta el chip.");

				Assert.That(stands.Says, Does.Contain("months left of 240 months."),
					"La mitad de tiempo SI se midio: no tener odometro no la degrada.");
				Assert.That(
					stands.Says,
					Does.Contain("Distance: cannot be measured, because there is no odometer reading"));

				// Y LOS DOS HECHOS QUE SI ESTAN SIGUEN SALIENDO, que es lo que la pantalla pinta al
				// lado de la frase. El renglon del odometro no.
				Assert.That(stands.PublishedAt, Is.EqualTo("2025-01-01"));
				Assert.That(stands.DeliveredAt, Is.EqualTo("2025-01-01"));
				Assert.That(stands.DeliveryReading, Is.EqualTo(120));
				Assert.That(stands.DeliveryUnit, Is.EqualTo("mi"));
			}
		}

		// EL SOBRE ESTA Y LA ENTREGA NO: la respuesta se suspende nombrando el ancla que falta, que es
		// desde donde se cuentan las DOS mitades. Sin este caso, "le falta un hecho" seria una sola
		// forma con tres nombres.
		[Test]
		public void WithNoDeliveryRecordedTheAnswerIsSuspendedAndNamesTheMissingAnchor()
		{
			using (AScheduleOnARealJournal noDelivery = ASealedScope("JTDKN3DU0A0123456"))
			{
				noDelivery.Api.PublishWarrantyEnvelope(
					TheMonths, TheDistance, TheUnit, ThePublication);
				noDelivery.Api.RecordReading(9000, TheUnit, TheDayItWasObserved, "Telemetry");

				WhereThisVehicleStands stands = WhatTheScreenIsHanded(noDelivery);

				Assert.That(stands.EnvelopePublished, Is.True);
				Assert.That(stands.DeliveryRecorded, Is.False);
				Assert.That(stands.OdometerKnown, Is.True);
				Assert.That(stands.TheRecordIsMissingAFact, Is.True);

				Assert.That(stands.CanBeEvaluated, Is.False);
				Assert.That(stands.Standing, Is.Empty);
				Assert.That(stands.Says, Does.Contain(stands.PublishedSays),
					"Y la mitad publicada sigue adentro de la frase suspendida: la pantalla no la "
					+ "pierde por faltarle un hecho personal.");

				Assert.That(stands.Systems, Is.Empty);

				// LA LECTURA SI ESTA Y SALE, aunque no se pueda medir con ella. Sin este renglon, los
				// tres casos de ausencia se verian iguales desde la pantalla.
				Assert.That(stands.Odometer, Is.EqualTo(9000));
				Assert.That(stands.OdometerObservedAt, Is.EqualTo("2025-06-01"));
				Assert.That(stands.DeliveredAt, Is.Empty);
			}
		}

		// ===== EL REGISTRO ESTA COMPLETO =================================================

		// LA MITAD DEL VEHICULO CONTESTA ENTERA Y CADA SISTEMA DEL ALCANCE TRAE LA SUYA. Es Q1, Q2 y Q7
		// en una sola pantalla y sin pedirle nada a nadie.
		[Test]
		public void WithEverythingRecordedTheStandingAndEverySystemAnswer()
		{
			using (AScheduleOnARealJournal whole = AVehicleWithEverything())
			{
				WhereThisVehicleStands stands = WhatTheScreenIsHanded(whole);

				Assert.That(stands.TheRecordIsMissingAFact, Is.False);
				Assert.That(stands.RecordContradictsItself, Is.False);
				Assert.That(stands.CanBeEvaluated, Is.True);
				Assert.That(stands.Standing, Is.EqualTo("Inside"));

				Assert.That(stands.Systems, Has.Count.EqualTo(1));
				Assert.That(stands.Systems[0].SystemCode, Is.EqualTo(TheSystem));
				Assert.That(stands.Systems[0].CanBeEvaluated, Is.True);
				Assert.That(stands.Systems[0].Standing, Is.EqualTo("CoveredWithExclusion"));
				Assert.That(stands.Systems[0].Says, Does.Contain("'POWERTRAIN'"));
			}
		}

		// DOS SOBRES, Y EL QUE RIGE ES EL PUBLICADO DESPUES. Sin un segundo sobre, "la pantalla nombra
		// la fecha de publicacion" pasaria contra el unico que hay y no distinguiria nada: lo que se
		// afirma aca es que nombra EL QUE RIGE, con sus limites y con SU fecha.
		[Test]
		public void WithTwoEnvelopesTheScreenNamesTheOneThatGovernsAndItsOwnPublicationDay()
		{
			using (AScheduleOnARealJournal twice = AVehicleWithEverything())
			{
				Assert.That(WhatTheScreenIsHanded(twice).PublishedAt, Is.EqualTo("2025-01-01"));

				twice.Api.PublishWarrantyEnvelope(120, 200000, TheUnit, TheSecondPublication);

				WhereThisVehicleStands after = WhatTheScreenIsHanded(twice);

				Assert.That(after.PublishedAt, Is.EqualTo("2026-01-01"),
					"Un sobre publicado despues rige desde su propia fecha hacia adelante, y la "
					+ "procedencia que la pantalla nombra es la suya.");
				Assert.That(after.Says, Does.Contain("120 months or 200000 mi"));
				Assert.That(after.Says, Does.Not.Contain("60000 mi"),
					"Y los limites del primero ya no salen. Si salieran, la pantalla estaria midiendo "
					+ "contra un sobre que dejo de regir.");
			}
		}

		// ===== UN DEFECTO DEL DOMINIO QUE ESTA PANTALLA IMPRIME PALABRA POR PALABRA =======

		// LA FRASE DE LA SUSPENSION POR UNIDADES NOMBRA DOS HECHOS DISTINTOS Y SOLO UNO ES EL QUE NO
		// CALZO. Lo encontro T9 corriendo la demo y lo dejo sin arreglar; T10 lo PINTA, asi que ahora
		// una persona lo lee.
		//
		// SON TRES UNIDADES LAS QUE TIENEN QUE CALZAR -- la del limite del sobre, la de la lectura del
		// dia de la ENTREGA, y la del odometro de HOY -- y la clausula de adentro nombra bien cual
		// choco, porque se la pasan desde una capa arriba. La frase de AFUERA la tiene ESCRITA A MANO
		// como "this vehicle's odometer", asi que cuando la que choca es la de la entrega, una sola
		// oracion nombra dos hechos y exactamente uno es el correcto.
		//
		// POR QUE NINGUNA PRUEBA LO ATRAPO: las UNIDADES que nombra son las correctas, y todas las
		// pruebas afirman las unidades. Ninguna afirma el SUSTANTIVO.
		//
		// SE FIJA COMO ESTA, no arreglado: arreglarlo es un cambio de un campo en Dominio/ y esta
		// tarea deja ese directorio intacto. Quien lo arregle se pone rojo aca a proposito, y el rojo
		// dice que la frase mejoro.
		[Test]
		public void TheUnitMismatchSentenceNamesTwoDifferentFactsAndTheScreenPrintsItWhole()
		{
			using (AScheduleOnARealJournal clashing = AVehicleWhoseDeliveryReadingIsInTheOtherUnit())
			{
				WhereThisVehicleStands stands = WhatTheScreenIsHanded(clashing);

				Assert.That(stands.CanBeEvaluated, Is.False);
				Assert.That(stands.OdometerUnit, Is.EqualTo("km"),
					"El odometro de HOY esta en la misma unidad que el limite. La que no calza es la de "
					+ "la entrega, y es el unico dato que hace que este renglon signifique algo.");
				Assert.That(stands.DeliveryUnit, Is.EqualTo("mi"));

				// LA CLAUSULA DE ADENTRO, QUE ES LA CORRECTA.
				Assert.That(
					stands.Says,
					Does.Contain("the odometer reading recorded at delivery is in mi"));

				// Y LA DE AFUERA, QUE NOMBRA OTRO HECHO. Las dos van a la pantalla en la misma frase.
				Assert.That(
					stands.Says,
					Does.Contain("this vehicle's odometer is in mi"),
					"ESTE RENGLON FIJA UN DEFECTO, no una ley. El odometro de este vehiculo esta en km "
					+ "-- la misma unidad que el limite -- y la frase dice que esta en mi. Si esto se "
					+ "pone rojo, alguien arreglo la frase del dominio: borrar este renglon y decirlo.");
			}
		}

		// ===== EL REGISTRO SE CONTRADICE =================================================

		// LA PRIMERA FILA: el odometro de hoy por debajo de la lectura de la entrega, en la misma
		// unidad. La mitad del vehiculo se borra ENTERA -- una frase que mide algo al lado de un aviso
		// que dice que no se pudo medir nada es peor que cualquiera de las dos sola -- y lo que el
		// fabricante publico sigue contestando, porque no depende de ningun hecho de este carro.
		[Test]
		public void AnOdometerBelowTheDeliveryReadingWipesTheVehicleHalfAndThePublishedScopeSurvives()
		{
			using (AScheduleOnARealJournal broken = AVehicleWhoseOdometerWentBackwards())
			{
				WhereThisVehicleStands stands = WhatTheScreenIsHanded(broken);

				Assert.That(stands.RecordContradictsItself, Is.True);
				Assert.That(stands.Says, Is.Empty);
				Assert.That(stands.Standing, Is.Empty);
				Assert.That(stands.CanBeEvaluated, Is.False);
				Assert.That(stands.Systems, Is.Empty);

				// Y ESTE ES EL UNICO ESTADO EN QUE LA MITAD PUBLICADA TIENE QUE SALIR SOLA, porque es
				// el unico en que no hay ninguna frase compuesta que la lleve adentro.
				Assert.That(stands.PublishedSays, Is.Not.Empty);
				Assert.That(stands.PublishedSays, Does.Contain("60000 mi"));

				// LOS HECHOS SUELTOS SIGUEN SALIENDO, que es lo que le deja a la persona ver en la
				// pantalla los dos numeros que no pueden ser los dos.
				Assert.That(stands.DeliveryReading, Is.EqualTo(50000));
				Assert.That(stands.Odometer, Is.EqualTo(9000));

				WhatTheManufacturerPublished sheet =
					WhatTheManufacturerPublished.ReadThrough(broken.Api);

				Assert.That(sheet.Coverage.Published, Is.True);
				Assert.That(sheet.Coverage.Revision, Is.EqualTo(TheRevision));
			}
		}

		// EL CONTROL, Y SIN EL LO DE ARRIBA NO PRUEBA NADA: la puerta SI revienta, y revienta con la
		// excepcion del DOMINIO. Esta caratula no puede nombrar ese tipo -- es interno a su ensamblado
		// y el browser no es amigo de el -- asi que lo reconoce por el NOMBRE, y esto es lo que fija
		// que el nombre sigue siendo ese.
		//
		// Y REVIENTAN LAS DOS, no solo la que compone la frase: por eso la caratula envuelve el bloque
		// entero y no la ultima llamada.
		[Test]
		public void TheDoorReallyThrowsAndItIsTheDomainsOwnExceptionTheScreenRecognises()
		{
			using (AScheduleOnARealJournal broken = AVehicleWhoseOdometerWentBackwards())
			{
				Exception thrown = Assert.Catch(
					() => broken.Api.DescribeWarrantyEnvelope(TheDayTheScreenAsks));

				Assert.That(thrown.GetType().Name, Is.EqualTo("DomainException"),
					"Si el dominio le cambia el nombre a su excepcion, la pantalla deja de "
					+ "reconocerla y el reventon le llega a la persona como una pagina de error.");

				Assert.Throws<DomainException>(
					() => broken.Api.CanTheEnvelopeBeEvaluated(TheDayTheScreenAsks),
					"La pregunta de si se puede evaluar tambien revienta, asi que preguntar primero "
					+ "no salva de nada y hay que envolver el bloque entero.");
			}
		}

		// LA SEGUNDA FILA, Y EN ESTA PANTALLA SE PARTE EN DOS POR ALGO QUE LA PANTALLA NO ELIGIO.
		//
		// Con la entrega fechada en el futuro las dos preguntas se separan: la del SOBRE contesta -- y
		// contesta como una postura DECIDIDA, con mas meses de los que el plazo tiene -- y la de
		// COBERTURA revienta. La seccion hace las dos en el mismo bloque, asi que lo que la persona ve
		// depende de si el fabricante publico un alcance con algun sistema adentro. Los dos casos se
		// miden, porque la proteccion del primero es INCIDENTAL y no estructural.
		//
		// ESTE ES EL PRIMERO: con alcance publicado, el reventon de la cobertura arrastra la mitad
		// entera y la persona ve el aviso de contradiccion. Sale MEJOR parada que el asistente, que
		// recibe el Inside y solo tiene la descripcion de un campo para dudarlo.
		[Test]
		public void ADeliveryDatedInTheFutureIsCaughtWhenTheCoverageReadRefusesInTheSameBlock()
		{
			using (AScheduleOnARealJournal ahead = AVehicleDeliveredInTheFuture(ASealedScope))
			{
				WhereThisVehicleStands stands = WhatTheScreenIsHanded(ahead);

				Assert.That(stands.RecordContradictsItself, Is.True,
					"La pregunta del SOBRE no revienta con este registro. Lo que revienta es la de "
					+ "COBERTURA, en el mismo bloque, y por eso la mitad entera se borra.");
				Assert.That(stands.Says, Is.Empty);
				Assert.That(stands.Standing, Is.Empty);
				Assert.That(stands.Systems, Is.Empty);

				// EL CONTROL DE QUE LAS DOS PREGUNTAS SE PARTEN DE VERDAD, y sin el lo de arriba se
				// leeria como si el sobre tambien rechazara -- que es lo que pasa en la primera fila y
				// NO es lo que pasa aca.
				Assert.That(
					ahead.Api.DescribeWarrantyEnvelope(TheDayTheScreenAsks),
					Does.Contain("months left of 36 months."),
					"El sobre contesta, con MAS plazo del que publico, porque los meses cumplidos "
					+ "salen negativos. No miente: dice exactamente lo que el registro implica.");

				Assert.Throws<DomainException>(
					() => ahead.Api.DescribeCoverageForThisVehicle(TheSystem, TheDayTheScreenAsks));
			}
		}

		// Y ESTE ES EL SEGUNDO, QUE ES EL QUE NO ESTA ARREGLADO: sin alcance publicado, el recorrido
		// por sistemas no da ninguna vuelta, nada revienta, y el Inside con setenta y tres anos de
		// plazo llega a la pantalla. Lo unico que lo delata son las dos fechas que la seccion pinta al
		// lado: el dia de la entrega es POSTERIOR al instante en que se pregunto.
		//
		// Una persona se encoge de hombros ante un hueco y un modelo lo rellena; ante un disparate
		// bien formado los dos lo repiten. Esto esta MEDIDO Y NO ESTA ARREGLADO: arreglarlo es una
		// decision del dominio, y esta prueba se pone roja el dia que alguien la tome.
		[Test]
		public void AndWithNoScopePublishedTheSameRecordReachesTheScreenAsASettledInside()
		{
			using (AScheduleOnARealJournal ahead = AVehicleDeliveredInTheFuture(AVehicleWithNoScope))
			{
				WhereThisVehicleStands stands = WhatTheScreenIsHanded(ahead);

				Assert.That(stands.RecordContradictsItself, Is.False,
					"Sin ningun sistema que preguntar, nada revienta y la contradiccion pasa entera.");
				Assert.That(stands.Standing, Is.EqualTo("Inside"));
				Assert.That(stands.Says, Does.Contain("months left of 36 months."));

				// LO UNICO QUE LO DELATA EN LA PANTALLA: los dos instantes, uno al lado del otro, en
				// el mismo bloque. Si alguno de los dos dejara de pintarse, el disparate se quedaria
				// sin nada que lo contradiga.
				Assert.That(stands.DeliveredAt, Is.EqualTo("2099-01-01"));
				Assert.That(stands.AskedAt, Is.EqualTo("2026-09-01 00:00:00Z"));
				Assert.That(
					string.Compare(stands.DeliveredAt, stands.AskedAt, StringComparison.Ordinal),
					Is.GreaterThan(0),
					"La fecha de entrega es POSTERIOR al instante en que se pregunto, y las dos estan "
					+ "en la pantalla. Es lo mas que una caratula puede hacer honestamente sin volver "
					+ "a derivar una regla del dominio.");
			}
		}

		// ------------------------------------------------------ el aparejo

		private static WhereThisVehicleStands WhatTheScreenIsHanded(AScheduleOnARealJournal journal)
		{
			return WhereThisVehicleStands.ReadThrough(journal.Api, TheDayTheScreenAsks);
		}

		private static AScheduleOnARealJournal ASealedScope(string vin)
		{
			var journal = new AScheduleOnARealJournal(vin, AppState.TheProfileAtDelivery);

			journal.Api.OpenRevision(TheRevision, ThePublication);
			journal.Api.PublishCoveredSystem(TheRevision, TheSystem, 0, 0, TheUnit);
			journal.Api.PublishCoverageExclusion(
				TheRevision, TheSystem, "wear parts", "when the part is a consumable");
			journal.Api.SealRevision(TheRevision);

			return journal;
		}

		private static AScheduleOnARealJournal AVehicleWithEverything()
		{
			AScheduleOnARealJournal journal = ASealedScope("WAUZZZ8V5KA000001");

			journal.Api.PublishWarrantyEnvelope(TheMonths, TheDistance, TheUnit, ThePublication);
			journal.Api.RecordDelivery(TheDelivery, 120, TheUnit);
			journal.Api.RecordReading(9000, TheUnit, TheDayItWasObserved, "Telemetry");

			return journal;
		}

		// EL SOBRE Y EL ODOMETRO DE HOY EN UNA UNIDAD, Y LA LECTURA DEL DIA DE LA ENTREGA EN LA OTRA.
		// El plazo es largo a proposito para que la mitad de tiempo no alcance a decidir sola y la
		// respuesta quede SUSPENDIDA, que es donde vive la frase que este caso mira.
		private static AScheduleOnARealJournal AVehicleWhoseDeliveryReadingIsInTheOtherUnit()
		{
			var journal = new AScheduleOnARealJournal("3VWFE21C04M000001", AppState.TheProfileAtDelivery);

			journal.Api.PublishWarrantyEnvelope(240, 20000, "UnitA", ThePublication);
			journal.Api.RecordDelivery(TheDelivery, 120, "UnitB");
			journal.Api.RecordReading(9000, "UnitA", TheDayItWasObserved, "Telemetry");

			return journal;
		}

		private static AScheduleOnARealJournal AVehicleWhoseOdometerWentBackwards()
		{
			AScheduleOnARealJournal journal = ASealedScope("5YJ3E1EA7JF000001");

			journal.Api.PublishWarrantyEnvelope(TheMonths, TheDistance, TheUnit, ThePublication);
			journal.Api.RecordDelivery(TheDelivery, 50000, TheUnit);
			journal.Api.RecordReading(9000, TheUnit, TheDayItWasObserved, "Telemetry");

			return journal;
		}

		// UN VEHICULO SIN NINGUN ALCANCE PUBLICADO. Existe para el segundo caso de la segunda fila de
		// R6: sin sistemas que preguntar, la mitad de cobertura no revienta porque no se hace.
		private static AScheduleOnARealJournal AVehicleWithNoScope(string vin)
		{
			return new AScheduleOnARealJournal(vin, AppState.TheProfileAtDelivery);
		}

		private static AScheduleOnARealJournal AVehicleDeliveredInTheFuture(
			Func<string, AScheduleOnARealJournal> withOrWithoutAScope)
		{
			AScheduleOnARealJournal journal = withOrWithoutAScope("2T1BURHE0JC000001");

			journal.Api.PublishWarrantyEnvelope(TheMonths, TheDistance, TheUnit, ThePublication);
			journal.Api.RecordDelivery(TheDeliveryThatHasNotHappenedYet, 120, TheUnit);
			journal.Api.RecordReading(9000, TheUnit, TheDayItWasObserved, "Telemetry");

			return journal;
		}
	}
}
