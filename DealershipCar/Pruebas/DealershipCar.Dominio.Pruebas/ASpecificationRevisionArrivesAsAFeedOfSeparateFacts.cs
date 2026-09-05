using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using DealershipCar.App;
using DealershipCar.App.Events;
using NUnit.Framework;
using Puppets.Portador;

namespace DealershipCar.Dominio.Pruebas
{
	// LA DECISION DE T9, ESCRITA COMO PRUEBA: una revision de la especificacion llega como ONCE
	// tipos de hecho sueltos, y no como un evento que la traiga entera.
	//
	// POR QUE NO UN EVENTO POR REVISION. Un hecho que trajera la ficha completa necesitaria un
	// script del DSL generado por evento -- tantas sentencias como items venga trayendo -- y un
	// script generado no lo puede enumerar EveryScriptOfTheDoorHasBeenRun. La medicion S5 dice que
	// ahi es exactamente donde un error de tipeo se vuelve una entrada permanente que hace loguear
	// un fallo de rehidratacion en cada arranque futuro de esa agenda. Once scripts fijos y once
	// clases de mensaje son mas codigo y menos riesgo.
	//
	// EL ORDEN ES EL RIESGO ENTERO, y por eso hay pruebas para el. Los once comandos no son
	// independientes: PublishX rechaza una revision que no sea un borrador abierto, SealRevision
	// rechaza un borrador vacio, y SupersedeRevision nombra una sucesora que ya tiene que existir.
	// Un feed que entregue un sello antes de su contenido camina directo a C28 -- el dominio
	// rechaza, el rechazo cicatriza, y la cicatriz se repetiria en cada refresh. Lo que estas
	// pruebas fijan es que no se repite, y que la narrativa no se atasca.
	//
	// LA POLARIDAD, DICHA. Re-entregar un bloque es NORMAL y cuesta cero: lo rechaza el Check de
	// idempotencia bajo el read lock, sin escribir ni una entrada. Un feed que se contradice a si
	// mismo -- que abre dos veces el mismo codigo, que publica en una revision que ya sello, que
	// sella una vacia -- es un defecto DE QUIEN PUBLICA, y ahi el throw del dominio es lo correcto:
	// cuesta una cicatriz, una sola, y la narrativa sigue.
	// NO SE PARALELIZA, Y ESTE FIXTURE NO SE PUSO ROJO -- POR ESO LLEVA EL ATRIBUTO.
	// WhatTheEngineSaysWhenItReopens() captura con Console.SetOut/SetError lo que el motor escribe al
	// reabrir el journal, y esos dos son del PROCESO ENTERO. Medido 2026-09-04 con
	// [assembly: Parallelizable(ParallelScope.Fixtures)]: con 8 workers salieron SIETE rojos en TRES
	// de los CINCO fixtures que redirigen la consola. Este es el otro.
	//
	// UNA CARRERA QUE NO SALTO EN UNA CORRIDA NO ES UNA CARRERA QUE NO EXISTA (C57): lo que se
	// enumera son los CINCO Console.SetOut de Pruebas/, no los tres sintomas de una corrida. Y aca la
	// captura es UNA SOLA afirmacion de las muchas del fixture, lo que la hace mas facil de no ver.
	//
	// Sin Parallelizable en el ensamblado esto no hace nada: es el registro de cuales cinco eran.
	[TestFixture]
	[NonParallelizable]
	public sealed class ASpecificationRevisionArrivesAsAFeedOfSeparateFacts
	{
		private AScheduleOnARealJournal journal;

		[SetUp]
		public void Setup()
		{
			journal = new AScheduleOnARealJournal(ADealershipNarrative.TheVin, "Standard");
		}

		[TearDown]
		public void TearDown()
		{
			journal.Dispose();
		}

		// LA PRUEBA QUE MAS IMPORTA: la ficha que deja el feed es la MISMA que deja la puerta.
		//
		// No se compara contra una frase escrita a mano -- eso mediria lo que uno recuerda -- sino
		// contra la ficha publicada por los once comandos de DealershipCarAPI sobre un segundo
		// journal. Los dos caminos arman sus parametros por separado (es la leccion de
		// TheIngestedFactKeepsEveryPartOfWhatWasReceived), asi que un campo cableado al de al lado
		// en la ingesta cambia esta cadena y ninguna otra prueba tiene por que verlo.
		//
		// SE COMPARA EN LOS CUATRO MOMENTOS, y es lo que T9 tuvo que corregir de su propia primera
		// version. Comparar SOLO al final dejo SEIS defectos deliberados en verde: al final la
		// primera revision esta sustituida y seis de los siete items contestan "no publicado" en LOS
		// DOS LADOS, asi que el grado del combustible cableado a una constante, el intervalo con sus
		// dos mitades cruzadas y la exclusion con su condicion cruzada comparaban iguales -- porque
		// no habia nada que comparar. La enumeracion estaba completa; el MOMENTO no. Queda escrito
		// en CONTRADICTIONS C61.
		//
		// El momento que discrimina es el segundo: la primera revision sellada y nada sustituido
		// todavia, que es el unico en que los siete items contestan a la vez.
		[Test]
		public void TheElevenFactsLeaveTheSameSheetTheDoorLeavesAtEveryMoment()
		{
			Assert.That(TheSameSheetActByAct.Length,
				Is.EqualTo(ADealershipNarrative.TheWholeSpecSheet().Count),
				"Los dos caminos tienen que ir hecho por hecho: una senal en el feed, un comando en "
				+ "la puerta. Si las dos listas se despegan, comparar un prefijo de cada una compara "
				+ "dos fichas distintas y el verde no dice nada.");

			using (AScheduleOnARealJournal throughTheDoor =
				new AScheduleOnARealJournal(ADealershipNarrative.TheVin, "Standard"))
			{
				int soFar = 0;

				foreach ((string moment, int upTo) in TheFourMoments)
				{
					for (; soFar < upTo; soFar++)
					{
						Assert.That(
							Apply(ADealershipNarrative.TheWholeSpecSheet()[soFar]), Is.EqualTo(""),
							"La senal " + soFar + " tenia que entrar.");

						TheSameSheetActByAct[soFar](throughTheDoor.Api);
					}

					Assert.That(journal.EverySpecificationAnswerItCanGive(),
						Is.EqualTo(throughTheDoor.EverySpecificationAnswerItCanGive()),
						"La hoja tiene que ser la misma respuesta por respuesta en el momento '"
						+ moment + "'.");
				}
			}

			Assert.That(journal.Api.LastEventApplied(),
				Is.EqualTo(ADealershipNarrative.TheLastSpecificationEvent),
				"Y las dieciseis entraron: ninguna se descarto ni se rechazo.");
		}

		// Y LOS CUATRO MOMENTOS SON LOS QUE SE DICEN. Sin esto, la comparacion de arriba seguiria
		// verde con los cuatro cortes puestos todos al final, que es exactamente el defecto que
		// tenia.
		[Test]
		public void TheFourMomentsAreADraftASealASecondSealAndASupersession()
		{
			Assert.That(TheFourMoments.Length, Is.EqualTo(4));

			// El primer momento: la ficha entera adentro de un borrador, invisible a toda lectura.
			Ingest(TheFirstOfTheSheet(TheFourMoments[0].UpTo));

			Assert.That(journal.Api.HasASealedRevision(), Is.False,
				"En el primer momento hay una revision con los siete items adentro y NADA sellado: "
				+ "es el momento que L12 existe para hacer incitable.");
			Assert.That(journal.Api.IsFluidPublished("EngineOil"), Is.False);

			// El segundo: sellada, y es el UNICO en que los siete items contestan a la vez.
			Ingest(TheSheetBetween(TheFourMoments[0].UpTo, TheFourMoments[1].UpTo));

			Assert.Multiple(() =>
			{
				Assert.That(journal.Api.IsFluidPublished("EngineOil"), Is.True);
				Assert.That(journal.Api.IsTyrePublished("Front"), Is.True);
				Assert.That(journal.Api.IsFuelPublished(), Is.True);
				Assert.That(journal.Api.IsServiceIntervalPublished("FluidChange"), Is.True);
				Assert.That(
					journal.Api.IsServicePricePublished(
						"FluidChange", ADealershipNarrative.TheMilestone, "UnitA"),
					Is.True);
				Assert.That(journal.Api.IsCoveragePublished(), Is.True);
				Assert.That(journal.Api.IsServicingPolicyPublished(), Is.True);
			});

			// El tercero: dos selladas, cada item diciendo de cual sale.
			Ingest(TheSheetBetween(TheFourMoments[1].UpTo, TheFourMoments[2].UpTo));

			Assert.That(journal.Api.ReadFluidRevision("EngineOil"),
				Is.EqualTo(ADealershipNarrative.TheSecondRevision));
			Assert.That(journal.Api.ReadTyreRevision("Front"),
				Is.EqualTo(ADealershipNarrative.TheFirstRevision));

			// El cuarto: sustituida la primera, y con ella lo que la sucesora no republico.
			Ingest(TheSheetBetween(TheFourMoments[2].UpTo, TheFourMoments[3].UpTo));

			Assert.That(journal.Api.IsTyrePublished("Front"), Is.False);
			Assert.That(journal.Api.IsFluidPublished("EngineOil"), Is.True);
		}

		// Y LA HOJA DEL FEED DICE LO QUE TIENE QUE DECIR, no solo lo mismo que la otra. Sin esto, la
		// comparacion de arriba seguiria verde con dos caminos igualmente roto.
		[Test]
		public void TheSheetTheFeedLeavesQuotesTheSecondRevisionAndWithdrawsTheFirst()
		{
			Ingest(ADealershipNarrative.TheWholeSpecSheet());

			Assert.That(journal.Api.HasASealedRevision(), Is.True);
			Assert.That(journal.Api.ReadCurrentRevisionCode(),
				Is.EqualTo(ADealershipNarrative.TheSecondRevision));

			Assert.That(journal.Api.ReadRevisionState(ADealershipNarrative.TheFirstRevision),
				Is.EqualTo("Superseded"));
			Assert.That(journal.Api.ReadRevisionState(ADealershipNarrative.TheSecondRevision),
				Is.EqualTo("Sealed"));

			Assert.That(journal.Api.DescribeFluid("EngineOil"),
				Does.Contain(ADealershipNarrative.TheSecondOilGrade),
				"El aceite sale de la segunda revision: una revision aplica desde su instante hacia "
				+ "adelante.");
			Assert.That(journal.Api.ReadFluidRevision("EngineOil"),
				Is.EqualTo(ADealershipNarrative.TheSecondRevision));

			Assert.That(journal.Api.IsTyrePublished("Front"), Is.False,
				"Y la llanta se retiro con la primera: lo que la sucesora no volvio a publicar no "
				+ "queda contestando.");
			Assert.That(journal.Api.DescribeTyre("Front"), Does.Contain("no se ha publicado nada"));
		}

		// EL BARRIDO DE LOS ONCE, y la asercion NOMBRA EL TIPO en vez de decir que llego algo. Una
		// ruta que devolviera siempre el mismo mensaje pasaria un Is.Not.Null once veces (C60).
		[Test]
		public void TheRouteTellsTheElevenSpecificationFactsApartOneFromAnother()
		{
			Assert.Multiple(() =>
			{
				Assert.That(Route(ADealershipNarrative.RevisionOpened(1, "R1", TheInstant)),
					Is.InstanceOf<SpecificationRevisionOpened>());

				Assert.That(Route(ADealershipNarrative.Fluid(
						2, "R1", "EngineOil", "5W-30", "4.2", "UnitA", 10000, "UnitA", 365)),
					Is.InstanceOf<FluidSpecificationPublished>());

				Assert.That(Route(ADealershipNarrative.Tyre(
						3, "R1", "Front", "205/55R16", "2.4", "UnitA", "94V")),
					Is.InstanceOf<TyreSpecificationPublished>());

				Assert.That(Route(ADealershipNarrative.Fuel(4, "R1", "91 RON", "Minimum")),
					Is.InstanceOf<FuelGradePublished>());

				Assert.That(Route(ADealershipNarrative.Interval(
						5, "R1", "FluidChange", 10000, "UnitA", 365)),
					Is.InstanceOf<ServiceIntervalPublished>());

				Assert.That(Route(ADealershipNarrative.Price(
						6, "R1", "FluidChange", 10000, "UnitA", "89.90", "USD",
						TheInstant, "2026-12-31T00:00:00Z")),
					Is.InstanceOf<ServicePricePublished>());

				Assert.That(Route(ADealershipNarrative.CoveredSystem(
						7, "R1", "POWERTRAIN", 0, 0, "UnitA")),
					Is.InstanceOf<CoveredSystemPublished>());

				Assert.That(Route(ADealershipNarrative.Exclusion(
						8, "R1", "POWERTRAIN", "wear parts", "when consumable")),
					Is.InstanceOf<CoverageExclusionPublished>());

				Assert.That(Route(ADealershipNarrative.ServicingPolicy(
						9, "R1", "service anywhere", "keep the receipts")),
					Is.InstanceOf<ServicingPolicyPublished>());

				Assert.That(Route(ADealershipNarrative.RevisionSealed(10, "R1")),
					Is.InstanceOf<SpecificationRevisionSealed>());

				Assert.That(Route(ADealershipNarrative.RevisionSuperseded(11, "R1", "R2")),
					Is.InstanceOf<SpecificationRevisionSuperseded>());
			});
		}

		// Y CADA UNO LLEVA SU NUMERO Y SU TAG, que es lo unico que el mecanismo le mira: el numero es
		// el punto de control que se journaliza, el tag es como se rutea.
		[Test]
		public void EverySpecificationFactCarriesItsOwnEventNumberAndItsOwnType()
		{
			foreach (SenalDeEvento signal in ADealershipNarrative.TheWholeSpecSheet())
			{
				IMensajeDeEvento message = Route(signal);

				Assert.That(message, Is.Not.Null, signal.Tipo);
				Assert.That(message.Evento, Is.EqualTo(signal.Numero),
					"El numero del mensaje es el de la senal, y no un uno cableado: " + signal.Tipo);
				Assert.That(message.Tipo, Is.EqualTo(signal.Tipo),
					"Y el tag es el suyo, no el del tipo de al lado.");
			}
		}

		// LO QUE C49 PEDIA VERIFICAR EN VEZ DE SUPONER. OpenRevision rechaza un codigo que ya esta
		// tomado, y un feed que re-entrega el bloque que lo abrio es NORMAL. Lo que tiene que pararlo
		// es la guarda de idempotencia -- no el rechazo del dominio -- y la diferencia se ve en el
		// motivo con que vuelve: 'evento ya procesado', no 'ya tiene una revision'.
		[Test]
		public void RedeliveringTheBlockThatOpenedARevisionIsRefusedAsRepeatedAndNotAsTaken()
		{
			Ingest(ADealershipNarrative.TheWholeSpecSheet());

			string before = TheTreeOf(journal.Folder);
			string sheet = journal.EverySpecificationAnswerItCanGive();

			Ingested again = Ingest(ADealershipNarrative.TheWholeSpecSheet());

			Assert.That(again.Applied, Is.EqualTo(0));
			Assert.That(again.Rejected,
				Is.EqualTo(ADealershipNarrative.SpecificationSignalsThatApply));

			Assert.That(TheTreeOf(journal.Folder), Is.EqualTo(before),
				"BYTE A BYTE igual. Un rechazo bajo el read lock no escribe nada, ni una entrada "
				+ "no-op, asi que re-entregar la ficha entera cuesta cero.");

			Assert.That(journal.EverySpecificationAnswerItCanGive(), Is.EqualTo(sheet));

			string rejection = Apply(ADealershipNarrative.RevisionOpened(
				11, ADealershipNarrative.TheFirstRevision, ADealershipNarrative.TheFirstPublication));

			Assert.That(rejection, Does.Contain("evento ya procesado"),
				"Lo para el Check por REPETIDO, y eso es lo que hay que medir: si lo parara el "
				+ "dominio por 'ya tiene una revision', el rechazo seria correcto y la entrada "
				+ "quedaria escrita igual.");
			Assert.That(rejection, Does.Not.Contain("ya tiene una revision"),
				"El comando no llego a correr, asi que la guarda del dominio no se alcanzo.");
		}

		// Y LA OTRA MITAD DEL MISMO ORACULO: cuando el codigo llega DOS VECES CON NUMEROS DE EVENTO
		// DISTINTOS, la guarda de idempotencia no lo ve -- los dos numeros son nuevos -- y ahi si
		// contesta el dominio. Es un feed que perdio la cuenta, no una re-entrega, y por eso la
		// respuesta es otra.
		[Test]
		public void AFeedThatOpensTheSameCodeUnderTwoEventNumbersIsRefusedByTheDomain()
		{
			Apply(ADealershipNarrative.RevisionOpened(1, "R1", TheInstant));

			string rejection = Apply(ADealershipNarrative.RevisionOpened(2, "R1", TheInstant));

			Assert.That(rejection, Does.Contain("ya tiene una revision"),
				"El dominio lo rechaza: un codigo de revision nombra una sola publicacion para "
				+ "siempre. Y como el rechazo pasa adentro del cuerpo, cuesta una entrada -- que es "
				+ "el precio de un feed que se contradice, no de uno que repite.");

			Assert.That(journal.Api.LastEventApplied(), Is.EqualTo(2L),
				"La narrativa igual avanza, asi que el hecho siguiente entra.");
		}

		// UN SELLO QUE LLEGA ANTES DE SU CONTENIDO. Es la entrega desordenada, y la decision es que
		// se pierde ese hecho y no la ficha: el dominio rechaza el borrador vacio, el punto de
		// control pasa, y el contenido que viene detras entra normal.
		[Test]
		public void ASealThatArrivesBeforeItsContentIsLostAndTheContentBehindItStillEnters()
		{
			Apply(ADealershipNarrative.RevisionOpened(1, "R1", TheInstant));

			string rejection = Apply(ADealershipNarrative.RevisionSealed(2, "R1"));

			Assert.That(rejection, Does.Contain("sin que se haya publicado nada adentro"),
				"Sellada quiere decir citable: una revision vacia y sellada contestaria 'no "
				+ "publicado' a todo, firmado con una procedencia.");

			Assert.That(journal.Api.LastEventApplied(), Is.EqualTo(2L),
				"El punto de control paso el sello perdido. Sin esto el proveedor lo volveria a "
				+ "servir en cada refresh y cada refresh dejaria otra marca.");

			Assert.That(
				Apply(ADealershipNarrative.Fluid(
					3, "R1", "EngineOil", "5W-30", "4.2", "UnitA", 10000, "UnitA", 365)),
				Is.EqualTo(""),
				"Y el contenido que venia detras entra: la narrativa no quedo atascada en el sello.");

			Assert.That(Apply(ADealershipNarrative.RevisionSealed(4, "R1")), Is.EqualTo(""),
				"El sello siguiente, ya con contenido, si sella.");

			Assert.That(journal.Api.IsFluidPublished("EngineOil"), Is.True);
			Assert.That(journal.Api.ReadFluidRevision("EngineOil"), Is.EqualTo("R1"));
		}

		// UNA PUBLICACION QUE NOMBRA UNA REVISION QUE NADIE ABRIO. El codigo nombra una entrada del
		// registro y no un punto de un rango, asi que no hay estado que contestar: el dominio
		// revienta, se pierde el hecho, y la ficha sigue como estaba.
		[Test]
		public void APublishNamingARevisionNobodyOpenedIsLostAndTheSheetIsUnchanged()
		{
			Ingest(ADealershipNarrative.TheWholeSpecSheet());

			string sheet = journal.EverySpecificationAnswerItCanGive();

			string rejection = Apply(ADealershipNarrative.Fluid(
				27, "MY2025-R9", "BrakeFluid", "DOT 4", "0.5", "UnitA", 20000, "UnitA", 730));

			Assert.That(rejection, Does.Contain("no tiene ninguna revision 'MY2025-R9'"));

			Assert.That(journal.EverySpecificationAnswerItCanGive(), Is.EqualTo(sheet),
				"Nada de la hoja se movio: el comando revento antes de publicar nada.");
			Assert.That(journal.Api.LastEventApplied(),
				Is.EqualTo(27L), "Y la narrativa avanzo igual.");
		}

		// PUBLICAR EN UNA REVISION QUE YA SELLO. Es la correccion que el feed manda sin abrir una
		// revision nueva, y es lo que el sello existe para negar: desde que sella ya contesto, y toda
		// respuesta que dio la nombra.
		[Test]
		public void APublishIntoASealedRevisionIsLostAndWhatThatRevisionAlreadySaidStands()
		{
			Apply(ADealershipNarrative.RevisionOpened(1, "R1", TheInstant));
			Apply(ADealershipNarrative.Fluid(
				2, "R1", "EngineOil", "5W-30", "4.2", "UnitA", 10000, "UnitA", 365));
			Apply(ADealershipNarrative.RevisionSealed(3, "R1"));

			string rejection = Apply(ADealershipNarrative.Fluid(
				4, "R1", "EngineOil", "0W-20", "4.2", "UnitA", 10000, "UnitA", 365));

			Assert.That(rejection, Does.Contain("deja de aceptar contenido cuando se sella"));

			Assert.That(journal.Api.DescribeFluid("EngineOil"), Does.Contain("5W-30"),
				"La revision sigue diciendo lo que dijo. Corregir un item publicado necesita una "
				+ "revision nueva, que es de lo que se trata la procedencia.");
		}

		// UN SEGUNDO SELLO DE LA MISMA REVISION, con otro numero de evento. Es el feed que perdio la
		// cuenta, y la ficha no se entera.
		[Test]
		public void ASecondSealOfTheSameRevisionIsLostAndTheSheetKeepsAnswering()
		{
			Ingest(ADealershipNarrative.TheWholeSpecSheet());

			string sheet = journal.EverySpecificationAnswerItCanGive();

			string rejection = Apply(
				ADealershipNarrative.RevisionSealed(27, ADealershipNarrative.TheSecondRevision));

			Assert.That(rejection, Does.Contain("Sellar es un hecho que pasa una sola vez"));

			Assert.That(journal.EverySpecificationAnswerItCanGive(), Is.EqualTo(sheet));
		}

		// SUSTITUIR NOMBRANDO UNA SUCESORA QUE TODAVIA ES BORRADOR. Si un borrador pudiera
		// sustituir, la especificacion se quedaria sin ninguna revision contestando: la vieja
		// apagada y la nueva invisible.
		[Test]
		public void SupersedingByARevisionThatIsStillADraftIsLostAndTheSealedOneKeepsAnswering()
		{
			Apply(ADealershipNarrative.RevisionOpened(1, "R1", TheInstant));
			Apply(ADealershipNarrative.Fluid(
				2, "R1", "EngineOil", "5W-30", "4.2", "UnitA", 10000, "UnitA", 365));
			Apply(ADealershipNarrative.RevisionSealed(3, "R1"));
			Apply(ADealershipNarrative.RevisionOpened(4, "R2", "2026-02-01T00:00:00Z"));

			string rejection = Apply(ADealershipNarrative.RevisionSuperseded(5, "R1", "R2"));

			Assert.That(rejection, Does.Contain("La sucesora tiene que estar sellada"));

			Assert.That(journal.Api.HasASealedRevision(), Is.True);
			Assert.That(journal.Api.ReadCurrentRevisionCode(), Is.EqualTo("R1"),
				"La primera sigue contestando, que es exactamente lo que la guarda protege.");
		}

		// LA CICATRIZ ES UNA, NO UNA POR REFRESH. Es C28 sobre los hechos de la especificacion: el
		// punto de control avanzo en un comando aparte, asi que re-entregar el hecho rechazado lo
		// para el Check y no vuelve a escribir.
		[Test]
		public void RedeliveringARefusedSpecificationFactDoesNotScarASecondTime()
		{
			Apply(ADealershipNarrative.RevisionOpened(1, "R1", TheInstant));
			Apply(ADealershipNarrative.RevisionSealed(2, "R1"));

			string afterTheScar = TheTreeOf(journal.Folder);

			string rejection = Apply(ADealershipNarrative.RevisionSealed(2, "R1"));

			Assert.That(rejection, Does.Contain("evento ya procesado"),
				"Y lo rechaza por REPETIDO, no por vacio: el hecho ya no vuelve a ejecutarse.");

			Assert.That(TheTreeOf(journal.Folder), Is.EqualTo(afterTheScar),
				"Byte a byte igual.");
		}

		// Y EL HECHO RECHAZADO SI DEJA LA MARCA LA PRIMERA VEZ, medida por lo que duele: el motor
		// loguea un fallo de rehidratacion en cada arranque futuro de esta agenda. Sin este renglon,
		// el de arriba podria quedar verde por no medir nada.
		[Test]
		public void ASpecificationFactRefusedInsideTheBodyScarsOnEveryFutureOpening()
		{
			Apply(ADealershipNarrative.RevisionOpened(1, "R1", TheInstant));
			Apply(ADealershipNarrative.RevisionSealed(2, "R1"));

			Assert.That(WhatTheEngineSaysWhenItReopens(),
				Does.Contain("Rehydration execution failed"),
				"El motor escribio la entrada antes de correr el cuerpo. Es el costo de C22 llegando "
				+ "por los hechos de la especificacion, y es el precio de un feed que se contradice.");
		}

		// LA FICHA SOBREVIVE AL REPLAY. Todo lo que contesta despues de reabrir salio del journal: la
		// especificacion es una global de raiz del DSL, no un campo de C#.
		[Test]
		public void TheSheetTheFeedLeftAnswersTheSameAfterReplayingFromTheJournal()
		{
			Ingest(ADealershipNarrative.TheWholeSpecSheet());

			string live = journal.EverySpecificationAnswerItCanGive();
			long checkpoint = journal.Api.LastEventApplied();

			journal.CloseAndReopenFromTheJournal();

			Assert.That(journal.EverySpecificationAnswerItCanGive(), Is.EqualTo(live));
			Assert.That(journal.Api.LastEventApplied(), Is.EqualTo(checkpoint));
			Assert.That(journal.Api.LastEventApplied(),
				Is.EqualTo(ADealershipNarrative.TheLastSpecificationEvent));
		}

		// LA AGENDA Y LA ESPECIFICACION SON DOS COSAS QUE VIAJAN POR EL MISMO FEED. Ingerir la ficha
		// no mueve ningun estado de vencimiento, y la historia de mantenimiento no publica nada: es
		// lo que sostiene que la especificacion sea una tercera global de raiz al lado de la agenda y
		// nunca adentro.
		[Test]
		public void TheSheetAndTheScheduleTravelOnOneFeedWithoutTouchingEachOther()
		{
			Ingest(ADealershipNarrative.TheWholeStory());

			string scheduleBefore = journal.EveryDueStateItCanAnswer();

			Ingest(ADealershipNarrative.TheWholeSpecSheet());

			Assert.That(journal.EveryDueStateItCanAnswer(), Is.EqualTo(scheduleBefore),
				"La ficha del fabricante no mueve un solo estado de vencimiento de este vehiculo.");

			Assert.That(journal.Api.HasASealedRevision(), Is.True,
				"Y la ficha si entro: el punto de control es UNO para los dos, y por eso los "
				+ "numeros de la ficha siguen a los de la historia.");

			Assert.That(journal.Api.LastEventApplied(),
				Is.EqualTo(ADealershipNarrative.TheLastSpecificationEvent));
		}

		// EL BARRIDO QUE USAN LAS COMPARACIONES DE ARRIBA TIENE QUE PODER DISTINGUIR, y esta prueba
		// existe porque nada mas lo defiende. La comparacion feed-contra-puerta seguiria VERDE con un
		// barrido que leyera los seis fluidos desde una clave fija: las dos hojas dirian lo mismo,
		// mal, y la igualdad se cumpliria igual. Es C60 aplicado al instrumento y no al codigo.
		//
		// SE AFIRMA EN TRES MITADES, y la tercera es la que la pasada de falsificacion de T9 forzo.
		// La primera version pedia que el barrido NOMBRARA cada miembro y cada bloque -- y una
		// etiqueta sobrevive a que su lectura se reemplace por una constante, asi que "el barrido se
		// salta el bloque de la garantia" quedaba VERDE. Nombrar el bloque prueba que la etiqueta
		// esta, no que el valor de al lado salga de la lectura. Asi que ademas se exige que CADA
		// BLOQUE CAMBIE al pasar de no-publicado a publicado.
		//
		// Y LOS CUATRO ITEMS CON CLAVE PUBLICAN DOS MIEMBROS EN DOS REVISIONES DISTINTAS, por la misma
		// razon y en las cuatro familias: con los dos miembros en la misma revision, leer la
		// procedencia de todos desde una clave fija contesta lo mismo que leerla bien. La pasada de
		// falsificacion lo encontro tres veces -- las llantas, los intervalos y los precios -- y las
		// tres eran el mismo agujero en distinto sitio.
		[Test]
		public void TheSheetSweepWalksEveryMemberAndCanTellThemApart()
		{
			Apply(ADealershipNarrative.RevisionOpened(1, "R1", TheInstant));
			Apply(ADealershipNarrative.Fluid(
				2, "R1", "EngineOil", "5W-30 synthetic", "4.2", "UnitA", 10000, "UnitA", 365));
			Apply(ADealershipNarrative.Tyre(3, "R1", "Front", "205/55R16", "2.4", "UnitA", "94V"));
			Apply(ADealershipNarrative.Interval(
				4, "R1", "FluidChange", ADealershipNarrative.TheMilestone, "UnitA", 365));
			Apply(ADealershipNarrative.Price(
				5, "R1", "FluidChange", ADealershipNarrative.TheMilestone, "UnitA", "89.90", "USD",
				TheInstant, "2026-12-31T00:00:00Z"));
			Apply(ADealershipNarrative.RevisionSealed(6, "R1"));

			string withOneRevision = journal.EverySpecificationAnswerItCanGive();

			// La segunda revision publica el OTRO miembro de cada familia, asi que la procedencia de
			// cada item con clave es distinta de la de su vecino.
			Apply(ADealershipNarrative.RevisionOpened(7, "R2", "2026-02-01T00:00:00Z"));
			Apply(ADealershipNarrative.Fluid(
				8, "R2", "BrakeFluid", "DOT 4", "0.5", "UnitA", 20000, "UnitA", 730));
			Apply(ADealershipNarrative.Tyre(9, "R2", "Rear", "225/50R17", "2.6", "UnitA", "98W"));
			Apply(ADealershipNarrative.Interval(
				10, "R2", "FilterChange", ADealershipNarrative.TheMilestone, "UnitA", 730));
			Apply(ADealershipNarrative.Price(
				11, "R2", "FilterChange", ADealershipNarrative.TheMilestone, "UnitA", "45.50", "USD",
				TheInstant, "2026-12-31T00:00:00Z"));
			Apply(ADealershipNarrative.RevisionSealed(12, "R2"));

			string sheet = journal.EverySpecificationAnswerItCanGive();

			Assert.Multiple(() =>
			{
				foreach (string kind in Enum.GetNames(typeof(FluidKindCode)))
					Assert.That(sheet, Does.Contain("fluid " + kind),
						"El barrido tiene que recorrer los SEIS fluidos por nombre.");

				foreach (string position in Enum.GetNames(typeof(TyrePositionCode)))
					Assert.That(sheet, Does.Contain("tyre " + position),
						"Y las TRES posiciones de llanta.");

				foreach (string operation in Enum.GetNames(typeof(OperationKindCode)))
				{
					Assert.That(sheet, Does.Contain("interval " + operation),
						"Y el intervalo de cada operacion.");
					Assert.That(sheet, Does.Contain("price " + operation),
						"Y el precio de cada operacion.");
				}
			});

			// LAS CUATRO FAMILIAS CON CLAVE, cada una con sus dos miembros en dos revisiones: la
			// presencia, la procedencia Y la frase, porque una clave fija puede acertar en una de las
			// tres y errar en las otras dos.
			//
			// Y CADA UNA CON UN MIEMBRO QUE NADIE PUBLICO, que es la mitad que faltaba: con la
			// presencia leida desde una clave fija que SI esta publicada, los siete miembros que no
			// lo estan contestan que si, y afirmar solo los publicados no lo ve.
			(string Family, string One, string Other, string OneSays, string OtherSays,
				string Absent)[] keyed =
			{
				("fluidos",    "fluid EngineOil : True | R1",     "fluid BrakeFluid : True | R2",
					"5W-30 synthetic", "DOT 4",
					"fluid Coolant : False |  | Refrigerante: no se ha publicado nada"),
				("llantas",    "tyre Front : True | R1",          "tyre Rear : True | R2",
					"205/55R16", "225/50R17",
					"tyre Spare : False |  | Llanta de repuesto: no se ha publicado nada"),
				("intervalos", "interval FluidChange : True | R1", "interval FilterChange : True | R2",
					"Cambio de fluido: se hace cada 10000", "Cambio de filtro: se hace cada 10000",
					"interval Inspection : False |  | Intervalo de servicio de revisión: no se ha "
					+ "publicado nada"),
				("precios",    "price FluidChange : True | R1",   "price FilterChange : True | R2",
					"$89.90", "$45.50",
					"price Inspection : False |  | Precio del servicio de revisión"),
			};

			Assert.That(keyed.Length, Is.EqualTo(4),
				"Son CUATRO familias del barrido indexadas por un conjunto cerrado: los fluidos, las "
				+ "llantas, los intervalos y los precios.");

			Assert.Multiple(() =>
			{
				foreach ((string family, string one, string other, string oneSays, string otherSays,
					string absent) in keyed)
				{
					Assert.That(sheet, Does.Contain(one).And.Contain(other),
						"Los dos miembros publicados de los " + family + " llevan CADA UNO su "
						+ "revision: con una clave fija los dos dirian la misma, y las dos "
						+ "respuestas serian legales.");

					Assert.That(sheet, Does.Contain(oneSays).And.Contain(otherSays),
						"Y cada uno su frase, que es la otra mitad de la misma cosa: " + family);

					Assert.That(sheet, Does.Contain(absent),
						"Y un miembro de los " + family + " que nadie publico contesta que NO, sin "
						+ "procedencia y diciendolo con palabras: con la presencia leida desde una "
						+ "clave publicada, este renglon diria que si.");
				}
			});

			// CADA BLOQUE TIENE QUE CAMBIAR al publicarse. Es lo que una etiqueta no prueba.
			TheRestOfTheSheetIsPublishedInto("R3", 13);

			string whole = journal.EverySpecificationAnswerItCanGive();

			(string Block, string Says)[] blocks =
			{
				("la revision vigente", "current=R3"),
				("el combustible", "fuel : True | R3"),
				("la garantia", "coverage : True | R3"),
				("el catalogo de sistemas", "vocabulary POWERTRAIN : True"),
				("la regla de otro taller", "servicingPolicy : True | R3"),
			};

			Assert.That(blocks.Length, Is.EqualTo(5),
				"Son CINCO bloques del barrido que no se indexan por un conjunto cerrado y cambian al "
				+ "publicarse: la revision vigente, el combustible, la garantia, su catalogo y la "
				+ "regla de otro taller. Los cuatro que si se indexan estan defendidos arriba, por "
				+ "sus dos miembros en dos revisiones.");

			Assert.Multiple(() =>
			{
				foreach ((string block, string says) in blocks)
					Assert.That(whole, Does.Contain(says),
						"El renglon de " + block + " tiene que llevar lo que SU lectura contesta, no "
						+ "solo su etiqueta: una etiqueta sobrevive a que la lectura de al lado se "
						+ "reemplace por una constante.");

				foreach ((string block, string says) in blocks)
					Assert.That(withOneRevision, Does.Not.Contain(says),
						"Y antes de publicarse tiene que decir otra cosa: si el renglon de " + block
						+ " dice lo mismo publicado y sin publicar, el barrido no lo esta leyendo.");
			});

			// EL OCTAVO BLOQUE ES EL MODELO, Y NO SE PUEDE DEFENDER ASI. Se dice en vez de dejarlo
			// pareciendo cubierto: el codigo de modelo es un hecho de la PRIMERA apertura de la app
			// -- lo trae el constructor de DealershipCarApp y el cuerpo de un upgrade no vuelve a
			// correr -- asi que su renglon contesta lo mismo antes y despues de publicar cualquier
			// cosa, y un barrido que lo tuviera cableado como constante seria indistinguible de uno
			// que lo lee. Ninguna prueba de esta suite puede ver esa diferencia, porque todos los
			// aparejos del nivel B abren su host con el mismo modelo.
			Assert.That(whole,
				Does.Contain("model=" + AScheduleOnARealJournal.TheModelAtDelivery),
				"El renglon del modelo esta, y eso es todo lo que se puede afirmar de el.");
		}

		// Los cinco items que no se indexan por un conjunto cerrado, mas el catalogo de sistemas,
		// publicados en una tercera revision. Es lo que lleva cada bloque del barrido de
		// no-publicado a publicado.
		private void TheRestOfTheSheetIsPublishedInto(string revisionCode, long from)
		{
			Apply(ADealershipNarrative.RevisionOpened(from, revisionCode, "2026-06-01T00:00:00Z"));
			Apply(ADealershipNarrative.Fuel(from + 1, revisionCode, "91 RON regular", "Minimum"));
			Apply(ADealershipNarrative.Interval(
				from + 2, revisionCode, "FluidChange", ADealershipNarrative.TheMilestone, "UnitA", 365));
			Apply(ADealershipNarrative.Price(
				from + 3, revisionCode, "FluidChange", ADealershipNarrative.TheMilestone, "UnitA",
				"89.90", "USD", TheInstant, "2026-12-31T00:00:00Z"));
			Apply(ADealershipNarrative.CoveredSystem(
				from + 4, revisionCode, ADealershipNarrative.TheSystem, 0, 0, "UnitA"));
			Apply(ADealershipNarrative.Exclusion(
				from + 5, revisionCode, ADealershipNarrative.TheSystem, "wear parts",
				"when the part is a consumable"));
			Apply(ADealershipNarrative.ServicingPolicy(
				from + 6, revisionCode, "service anywhere", "keep the receipts"));
			Apply(ADealershipNarrative.RevisionSealed(from + 7, revisionCode));
		}

		// ------------------------------------------------------ el aparejo

		private const string TheInstant = "2025-08-15T00:00:00Z";

		// LOS CUATRO MOMENTOS en que se compara, dichos como cuantos hechos van entrados. Son los
		// mismos cuatro que la caratula 'ficha' imprime, y el segundo es el que discrimina: es el
		// unico en que los siete items de la hoja contestan a la vez.
		private static readonly (string Moment, int UpTo)[] TheFourMoments =
		{
			("la ficha entera adentro de un borrador", 11),
			("la primera revision sellada", 12),
			("una segunda revision sellada sobre ella", 15),
			("la primera sustituida", 16),
		};

		// La MISMA ficha, publicada por los once comandos de la puerta, UN ACTO POR SENAL. Es el otro
		// lado de la comparacion, y esta escrita aparte a proposito: si la ingesta y esto compartieran
		// el armado de parametros, comparar los dos no probaria nada.
		//
		// LOS DECIMALES LLEVAN UN CERO A LA DERECHA en los dos lados, y es lo que hace visible la
		// normalizacion de la frontera: con 4.2 y 2.4 la normalizacion no cambia nada y quitarla no se
		// nota en ninguna comparacion.
		private static readonly Action<DealershipCarAPI>[] TheSameSheetActByAct =
		{
			api => api.OpenRevision(ADealershipNarrative.TheFirstRevision, OnThe(2025, 8, 15)),

			api => api.PublishFluidSpecification(
				ADealershipNarrative.TheFirstRevision, "EngineOil",
				ADealershipNarrative.TheFirstOilGrade, 4.20m, "UnitA",
				ADealershipNarrative.TheMilestone, "UnitA", 365),

			api => api.PublishTyreSpecification(
				ADealershipNarrative.TheFirstRevision, "Front", "205/55R16", 2.40m, "UnitA", "94V"),

			api => api.PublishFuelGrade(
				ADealershipNarrative.TheFirstRevision, "95 RON Premium", "Permitted"),
			api => api.PublishFuelGrade(
				ADealershipNarrative.TheFirstRevision, "91 RON regular", "Minimum"),
			api => api.PublishFuelGrade(
				ADealershipNarrative.TheFirstRevision, "E85 Flex", "Prohibited"),

			api => api.PublishServiceInterval(
				ADealershipNarrative.TheFirstRevision, "FluidChange",
				ADealershipNarrative.TheMilestone, "UnitA", 365),

			api => api.PublishServicePrice(
				ADealershipNarrative.TheFirstRevision, "FluidChange",
				ADealershipNarrative.TheMilestone, "UnitA", 89.90m, "USD",
				OnThe(2025, 8, 15), OnThe(2026, 12, 31)),

			api => api.PublishCoveredSystem(
				ADealershipNarrative.TheFirstRevision, ADealershipNarrative.TheSystem, 0, 0, "UnitA"),

			api => api.PublishCoverageExclusion(
				ADealershipNarrative.TheFirstRevision, ADealershipNarrative.TheSystem,
				"wear parts", "when the part is a consumable"),

			api => api.PublishServicingPolicy(
				ADealershipNarrative.TheFirstRevision,
				"service may be done at any qualified workshop",
				"keep the invoices and the parts records"),

			api => api.SealRevision(ADealershipNarrative.TheFirstRevision),

			api => api.OpenRevision(ADealershipNarrative.TheSecondRevision, OnThe(2026, 2, 1)),

			api => api.PublishFluidSpecification(
				ADealershipNarrative.TheSecondRevision, "EngineOil",
				ADealershipNarrative.TheSecondOilGrade, 4.20m, "UnitA",
				ADealershipNarrative.TheMilestone, "UnitA", 365),

			api => api.SealRevision(ADealershipNarrative.TheSecondRevision),

			api => api.SupersedeRevision(
				ADealershipNarrative.TheFirstRevision, ADealershipNarrative.TheSecondRevision),
		};

		private static DateTime OnThe(int year, int month, int day)
		{
			return new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);
		}

		private static IReadOnlyList<SenalDeEvento> TheFirstOfTheSheet(int howMany)
		{
			return TheSheetBetween(0, howMany);
		}

		private static IReadOnlyList<SenalDeEvento> TheSheetBetween(int from, int upTo)
		{
			List<SenalDeEvento> some = new List<SenalDeEvento>();
			IReadOnlyList<SenalDeEvento> whole = ADealershipNarrative.TheWholeSpecSheet();

			for (int signal = from; signal < upTo; signal++) some.Add(whole[signal]);

			return some;
		}

		private static IMensajeDeEvento Route(SenalDeEvento signal)
		{
			return DealershipEventRoute.Route(signal);
		}

		private string Apply(SenalDeEvento signal)
		{
			return journal.Api.Apply(Route(signal));
		}

		// El cuerpo del foreach de BucleDeRefresh.Ejecutar, a mano: la senal ya esta y lo que estas
		// pruebas miden es la ingesta, no el transporte.
		private Ingested Ingest(IReadOnlyList<SenalDeEvento> signals)
		{
			Ingested counted = new Ingested();

			foreach (SenalDeEvento signal in signals)
			{
				IMensajeDeEvento message = Route(signal);

				if (message == null) { counted.Discarded++; continue; }

				if (journal.Api.Apply(message).Length > 0) { counted.Rejected++; continue; }

				counted.Applied++;
			}

			return counted;
		}

		private sealed class Ingested
		{
			internal int Applied;
			internal int Discarded;
			internal int Rejected;
		}

		// LO QUE EL MOTOR DICE AL REHIDRATAR, escuchando LAS DOS SALIDAS: sus lineas de error van a
		// stderr y el avance del replay a stdout, y escuchar solo una dejaria el oraculo verde para
		// siempre.
		private string WhatTheEngineSaysWhenItReopens()
		{
			TextWriter originalOut = Console.Out;
			TextWriter originalError = Console.Error;
			StringWriter heard = new StringWriter();

			try
			{
				Console.SetOut(heard);
				Console.SetError(heard);
				journal.CloseAndReopenFromTheJournal();
			}
			finally
			{
				Console.SetOut(originalOut);
				Console.SetError(originalError);
			}

			return heard.ToString();
		}

		private static string TheTreeOf(string folder)
		{
			List<string> files = new List<string>(
				Directory.GetFiles(folder, "*", SearchOption.AllDirectories));
			files.Sort(StringComparer.Ordinal);

			StringBuilder tree = new StringBuilder();

			foreach (string file in files)
			{
				using (FileStream open = new FileStream(
					file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
				{
					byte[] content = new byte[open.Length];
					open.ReadExactly(content, 0, content.Length);

					tree.Append(Path.GetRelativePath(folder, file).Replace('\\', '/'));
					tree.Append(' ');
					tree.Append(content.Length);
					tree.Append(' ');
					tree.AppendLine(Convert.ToHexString(MD5.HashData(content)));
				}
			}

			return tree.ToString();
		}
	}
}
