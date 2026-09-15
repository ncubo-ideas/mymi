using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Concesionario.App;
using Concesionario.App.Events;
using NUnit.Framework;
using Puppets.Portador;

namespace Concesionario.Dominio.Pruebas
{
	// LAS DOS PROMESAS DE T8, MEDIDAS: la ingesta es idempotente, y el resume tras una caida es
	// exacto. Las dos son del plan §7 y las dos se afirman con la forma FUERTE de la comparacion --
	// byte a byte alrededor de algo que no debe escribir NADA (C23), que es donde esa comparacion si
	// se sostiene.
	//
	// COMO ESTA HECHO. El punto de control de la bitacora avanza DENTRO del mismo comando que aplica
	// el hecho, y la guarda es un Check y no un if en el cuerpo:
	//
	//   Check(@event > logbook.LastEvent()) Error 'evento ya procesado';
	//   { schedule.RecordReading(...); logbook.Advance(@event); }
	//
	// Eso da las dos cosas de una vez. Un evento repetido se rechaza BAJO EL READ LOCK y no deja
	// entrada (medido, SPIKES-OS2 S3), asi que re-entregar un bloque cuesta cero. Y no existe el
	// estado "aplique el hecho pero no avance el checkpoint": o entraron los dos o no entro ninguno,
	// asi que una caida a mitad de bloque no puede duplicar ni saltear.
	//
	// La idempotencia vive en el JOURNAL y no en una ventana en memoria, que es lo que la hace
	// sobrevivir al reinicio -- una ventana de dispatch se vacia al arrancar.
	[TestFixture]
	public sealed class AReceivedEventBlockIsAppliedExactlyOnce
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

		[Test]
		public void TheWholeStoryEntersOnceAndTheCheckpointEndsWhereTheNarrativeEnds()
		{
			Ingested first = Ingest(journal.Api, ADealershipNarrative.TheWholeStory());

			Assert.That(first.Applied, Is.EqualTo(ADealershipNarrative.SignalsThatApply));
			Assert.That(first.Discarded, Is.EqualTo(ADealershipNarrative.SignalsThatAreDiscarded));
			Assert.That(first.Rejected, Is.EqualTo(0));

			// El checkpoint queda en el ultimo evento APLICADO, no en el ultimo servido: los dos
			// descartados no lo mueven, porque no se journalizo nada por ellos.
			Assert.That(journal.Api.LastEventApplied(), Is.EqualTo(9L),
				"El 10 se descarto en la frontera, asi que la bitacora queda en el 9. Un descarte "
				+ "no avanza el punto de control: nada se escribio por el.");
		}

		// LA PROMESA 1: RE-ENTREGAR EL MISMO BLOQUE NO DEJA NI UNA ENTRADA.
		[Test]
		public void ARedeliveredBlockLeavesTheJournalByteForByteIdentical()
		{
			Ingest(journal.Api, ADealershipNarrative.TheWholeStory());

			string before = TheTreeOf(journal.Folder);
			string state = journal.EveryDueStateItCanAnswer();

			Ingested again = Ingest(journal.Api, ADealershipNarrative.TheWholeStory());

			Assert.That(again.Applied, Is.EqualTo(0),
				"Ninguno vuelve a entrar.");
			Assert.That(again.Rejected, Is.EqualTo(ADealershipNarrative.SignalsThatApply),
				"Los ocho que ya habian entrado los rechaza el Check, uno por uno.");

			Assert.That(TheTreeOf(journal.Folder), Is.EqualTo(before),
				"Y el arbol queda BYTE A BYTE igual. Es la forma fuerte del criterio: un rechazo "
				+ "bajo el read lock no escribe nada, ni una entrada no-op. Con un if adentro del "
				+ "cuerpo en vez de un Check, este renglon se pondria rojo con ocho entradas nuevas.");

			Assert.That(journal.EveryDueStateItCanAnswer(), Is.EqualTo(state));
		}

		// Y EL RECHAZO DICE POR QUE. El sobre crudo es del motor y desenvolverlo es de la caratula
		// (S3); lo que se afirma aca es que el motivo viaja y no viene vacio, porque un rechazo mudo
		// seria indistinguible de un exito para el bucle.
		[Test]
		public void ARedeliveredEventComesBackWithTheReasonItWasRefused()
		{
			IMensajeDeEvento first =
				DealershipEventRoute.Route(ADealershipNarrative.TheWholeStory()[0]);

			Assert.That(journal.Api.Apply(first), Is.EqualTo(""));

			string rejection = journal.Api.Apply(first);

			Assert.That(rejection, Is.Not.Empty);
			Assert.That(rejection, Does.Contain("evento ya procesado"));
		}

		// LA PROMESA 2: MATAR A MITAD DE BLOQUE Y REABRIR NO DUPLICA NI SALTEA.
		//
		// Se corta despues de cinco senales, se cierra el actor y se vuelve a abrir sobre el MISMO
		// journal -- que es lo mas cerca de una caida que se puede llegar sin matar el proceso, y
		// mide lo mismo: al reabrir, todo lo que hay salio del journal. Y despues se re-entrega el
		// bloque ENTERO desde el principio, que es exactamente lo que hace el proveedor: sirve desde
		// el checkpoint que la app le pide, y la app le pide el que tiene journalizado.
		[Test]
		public void KillingMidBlockAndReopeningProducesNoDuplicateAndNoGap()
		{
			Ingest(journal.Api, ADealershipNarrative.TheFirst(5));

			long checkpointBeforeTheCrash = journal.Api.LastEventApplied();
			Assert.That(checkpointBeforeTheCrash, Is.EqualTo(4L),
				"La quinta senal es el tipo que esta app no consume, asi que el punto de control "
				+ "quedo en la cuarta.");

			journal.CloseAndReopenFromTheJournal();

			Assert.That(journal.Api.LastEventApplied(), Is.EqualTo(checkpointBeforeTheCrash),
				"EL CHECKPOINT SALE DEL JOURNAL, no de un campo en memoria. Un campo estaria en "
				+ "cero al reabrir y el bloque entero volveria a entrar.");

			Ingested rest = Ingest(journal.Api, ADealershipNarrative.TheWholeStory());

			Assert.That(rest.Applied, Is.EqualTo(4),
				"Solo entran los que faltaban: 6, 7, 8 y 9. NO HAY DUPLICADO.");
			Assert.That(rest.Rejected, Is.EqualTo(4),
				"Los cuatro que ya habian entrado los rechaza el Check.");

			Assert.That(journal.Api.LastEventApplied(), Is.EqualTo(9L),
				"Y NO HAY HUECO: la bitacora termina donde termina la narrativa aplicable.");
		}

		// LA PRUEBA DE QUE NO HAY HUECO NO ES SOLO EL NUMERO: el estado tiene que ser el mismo que si
		// nunca hubiera habido corte. Sin esto, "el checkpoint llego a 9" seria compatible con
		// haberse salteado un hecho por el camino.
		[Test]
		public void TheStateAfterACrashIsTheSameAsIfTheBlockHadNeverBeenCut()
		{
			Ingest(journal.Api, ADealershipNarrative.TheFirst(5));
			journal.CloseAndReopenFromTheJournal();
			Ingest(journal.Api, ADealershipNarrative.TheWholeStory());
			journal.Api.Evaluate(new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc));

			string afterTheCrash = journal.EveryDueStateItCanAnswer();

			using (AScheduleOnARealJournal uninterrupted =
				new AScheduleOnARealJournal(ADealershipNarrative.TheVin, "Standard"))
			{
				Ingest(uninterrupted.Api, ADealershipNarrative.TheWholeStory());
				uninterrupted.Api.Evaluate(new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc));

				Assert.That(afterTheCrash, Is.EqualTo(uninterrupted.EveryDueStateItCanAnswer()),
					"El corte no dejo rastro en el estado. Es lo que quiere decir 'el resume es "
					+ "exacto': no que el numero coincida, sino que la agenda sea la misma.");
			}
		}

		// LA HISTORIA SOBREVIVE AL REPLAY. Todo lo que contesta despues de reabrir salio del journal:
		// la agenda y la bitacora son globales de raiz del DSL, no campos de C#, y un campo estaria
		// vacio despues de rehidratar.
		[Test]
		public void TheIngestedStoryAnswersTheSameAfterReplayingFromTheJournal()
		{
			Ingest(journal.Api, ADealershipNarrative.TheWholeStory());
			journal.Api.Evaluate(new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc));

			string live = journal.EveryDueStateItCanAnswer();
			long checkpoint = journal.Api.LastEventApplied();

			journal.CloseAndReopenFromTheJournal();

			Assert.That(journal.EveryDueStateItCanAnswer(), Is.EqualTo(live));
			Assert.That(journal.Api.LastEventApplied(), Is.EqualTo(checkpoint));
		}

		// CADA HECHO QUE ENTRA DEJA EL PUNTO DE CONTROL EN SU PROPIO NUMERO DE EVENTO, y son
		// VEINTICUATRO scripts los que lo prometen: cinco de la agenda, once de la especificacion,
		// TRES del perfil de este vehiculo, TRES de las estadias en el taller y DOS del libro de
		// costos. Es la promesa que hace exacto el resume, y no se hereda de un script al siguiente.
		//
		// ESTA PRUEBA EXISTE POR LA PASADA DE FALSIFICACION DE LA INGESTA DE LA FICHA. Quitarle
		// logbook.Advance al script del fluido -- y al del intervalo de servicio -- dejo la suite EN
		// VERDE, porque el checkpoint solo se atrasaba UNO y el hecho siguiente traia un numero mas
		// alto de todas formas. El sello y la sustitucion si se veian, y por una razon que no se
		// generaliza: el dominio los rechaza al repetirse, mientras que publicar dos veces en un
		// borrador es idempotente por contenido. Asi que la mitad de los scripts tenia testigo por
		// accidente.
		//
		// Y PASO OTRA VEZ, EXACTO, CON EL SCRIPT DE LA ENTREGA. La falsificacion de la ingesta del
		// perfil quito logbook.Advance del script de la ENTREGA y la suite quedo EN VERDE, mientras
		// que quitarlo del script del SOBRE puso cuatro en rojo -- porque la entrega es el primero de
		// los tres hechos de su lista y los dos sobres de atras avanzan el checkpoint igual. La misma
		// asimetria, un agregado mas alla, y el arreglo es el mismo: barrer LA TERCERA LISTA tambien.
		// Es la razon por la que este barrido es por script y no por bloque.
		//
		// LO QUE SE PIERDE SIN ESTO. Una caida entre el hecho y el flush deja el checkpoint atras del
		// hecho, el proveedor re-sirve, y el hecho vuelve a ejecutarse. Para un publish en borrador
		// eso no cambia el estado -- por eso nada se rompia -- pero deja una segunda entrada en el
		// journal de la persona por cada refresh que ocurra antes del siguiente hecho, y es
		// exactamente el atasco que C28 existe para evitar.
		[Test]
		public void EverySignalThatEntersLeavesTheCheckpointOnItsOwnEventNumber()
		{
			HashSet<string> swept = new HashSet<string>();
			List<SenalDeEvento> everything = new List<SenalDeEvento>();

			everything.AddRange(ADealershipNarrative.TheWholeStory());
			everything.AddRange(ADealershipNarrative.TheWholeSpecSheet());
			everything.AddRange(ADealershipNarrative.TheDeliveryTheEnvelopeAndThePlate());
			everything.AddRange(ADealershipNarrative.TheStays());
			everything.AddRange(ADealershipNarrative.TheCosts());

			foreach (SenalDeEvento signal in everything)
			{
				IMensajeDeEvento message = DealershipEventRoute.Route(signal);

				if (message == null)
				{
					Assert.That(journal.Api.LastEventApplied(), Is.LessThan(signal.Numero),
						"Un descarte NO mueve el punto de control: nada se escribio por el. "
						+ signal.Tipo);
					continue;
				}

				Assert.That(journal.Api.Apply(message), Is.EqualTo(""), signal.Tipo);

				Assert.That(journal.Api.LastEventApplied(), Is.EqualTo(signal.Numero),
					"El script de " + signal.Tipo + " tiene que avanzar el punto de control EN EL "
					+ "MISMO comando que el hecho. Atrasado uno, una caida antes del flush "
					+ "siguiente hace que el proveedor lo re-sirva y el hecho se ejecute otra vez.");

				swept.Add(signal.Tipo);
			}

			Assert.That(swept.Count, Is.EqualTo(24),
				"Son VEINTICUATRO scripts de ingesta con hecho -- cinco de la agenda, once de la "
				+ "especificacion, tres del perfil del vehiculo, tres de las estadias en el taller y dos "
				+ "del libro de costos -- mas el comando suelto que avanza el punto de control cuando el "
				+ "dominio rechaza uno, que es el veinticinco y no tiene hecho que aplicar.");
		}

		// LAS DOS PROMESAS VUELVEN A MEDIRSE SOBRE LOS ONCE HECHOS DE LA ESPECIFICACION, y hace falta
		// medirlas otra vez: cada tipo de hecho tiene su propio script con su propio Check, y una
		// promesa que se cumple en cinco scripts no se hereda al sexto.
		//
		// EL CORTE SE HACE JUSTO DESPUES DEL SELLO de la primera revision, que es el sitio donde una
		// re-entrega es mas peligrosa: al reabrir, el proveedor sirve desde el punto de control y el
		// bloque vuelve a traer el OpenRevision de esa revision. Si lo que lo parara fuera el dominio
		// -- 'esta especificacion ya tiene una revision' -- el rechazo seria correcto y la entrada
		// quedaria escrita igual. Lo que lo para es el Check, y eso es lo que este renglon fija.
		[Test]
		public void KillingMidSheetAndReopeningNeitherDuplicatesTheRevisionNorSkipsItsContent()
		{
			Ingest(journal.Api, TheFirstOfTheSheet(12));

			long checkpointBeforeTheCrash = journal.Api.LastEventApplied();
			Assert.That(checkpointBeforeTheCrash, Is.EqualTo(22L),
				"Doce senales de la ficha llegan hasta el sello de la primera revision.");

			journal.CloseAndReopenFromTheJournal();

			Assert.That(journal.Api.LastEventApplied(), Is.EqualTo(checkpointBeforeTheCrash),
				"EL CHECKPOINT SALE DEL JOURNAL, y con el la ficha: la especificacion es una global "
				+ "de raiz, no un campo de C#.");

			Ingested rest = Ingest(journal.Api, ADealershipNarrative.TheWholeSpecSheet());

			Assert.That(rest.Applied, Is.EqualTo(4),
				"Solo entran los cuatro que faltaban: la segunda revision, su aceite, su sello y la "
				+ "sustitucion. NO HAY DUPLICADO.");
			Assert.That(rest.Rejected, Is.EqualTo(12),
				"Y los doce que ya habian entrado los rechaza el Check, uno por uno -- incluido el "
				+ "OpenRevision, que el dominio habria rechazado por otra razon y con otro costo.");

			Assert.That(journal.Api.LastEventApplied(),
				Is.EqualTo(ADealershipNarrative.TheLastSpecificationEvent),
				"Y NO HAY HUECO: la ficha termina donde termina.");

			Assert.That(journal.Api.ReadCurrentRevisionCode(),
				Is.EqualTo(ADealershipNarrative.TheSecondRevision));
			Assert.That(journal.Api.DescribeFluid("EngineOil", TheLanguage.OfTheRecord),
				Does.Contain(ADealershipNarrative.TheSecondOilGrade));
		}

		// Y LA FICHA QUEDA IGUAL QUE SI NUNCA HUBIERA HABIDO CORTE. Sin esto, "el checkpoint llego al
		// 26" seria compatible con haberse salteado un item por el camino.
		[Test]
		public void TheSheetAfterACrashIsTheSameAsIfTheBlockHadNeverBeenCut()
		{
			Ingest(journal.Api, TheFirstOfTheSheet(12));
			journal.CloseAndReopenFromTheJournal();
			Ingest(journal.Api, ADealershipNarrative.TheWholeSpecSheet());

			string afterTheCrash = journal.EverySpecificationAnswerItCanGive();

			using (AScheduleOnARealJournal uninterrupted =
				new AScheduleOnARealJournal(ADealershipNarrative.TheVin, "Standard"))
			{
				Ingest(uninterrupted.Api, ADealershipNarrative.TheWholeSpecSheet());

				Assert.That(afterTheCrash,
					Is.EqualTo(uninterrupted.EverySpecificationAnswerItCanGive()),
					"El corte no dejo rastro en la hoja: no que el numero coincida, sino que la ficha "
					+ "sea la misma respuesta por respuesta.");
			}
		}

		// LO QUE LA HISTORIA DEJA EN LA AGENDA, afirmado en negocio y no solo en contadores. Sin
		// esto, todo lo de arriba seguiria verde con una ingesta que aplicara los ocho comandos
		// equivocados.
		[Test]
		public void TheDealershipsFactsLandOnTheScheduleAsTheFactsTheyWere()
		{
			Ingest(journal.Api, ADealershipNarrative.TheWholeStory());
			journal.Api.Evaluate(new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc));

			Assert.That(journal.Api.ReadDueState("Inspection"), Is.EqualTo("Due"),
				"El cruce de umbral del 1/4/2026 la vencio (ley L3). Y se queda en Due por mas que "
				+ "pasen cinco meses: una operacion ConditionOnly no mide desde ningun lado, asi "
				+ "que no tiene punto de vencimiento del que la gracia pueda pasarse.");

			Assert.That(journal.Api.ReadDueState("FluidChange"), Is.EqualTo("NotDue"),
				"La completacion del 5/3/2026 re-anclo su sucesora ahi (ley L4): al 1/9/2026 lleva "
				+ "180 dias de 365 y 0 unidades de 10000.");

			Assert.That(journal.Api.ReadDueState("RecallRemedy"), Is.EqualTo("NotDue"),
				"Y una clase que la narrativa nunca menciono contesta igual: la ausencia es una "
				+ "respuesta legal (E41).");
		}

		// LO QUE LAS ESTADIAS DEJAN EN EL REGISTRO, afirmado en negocio y no solo en contadores. Es el
		// hermano del renglon de arriba, un global de raiz mas alla, y hace falta por lo mismo: todo lo
		// demas de este fixture seguiria verde con una ingesta que aplicara los cuatro comandos
		// equivocados, o los tres correctos con los campos cruzados.
		//
		// EL ORACULO SON LAS OCHO LECTURAS DE LA PUERTA, que es lo que SH-3 no tenia cuando escribio
		// los tres comandos: se lee de vuelta lo que la sucursal dijo, por valor, y no que algo llego.
		//
		// LOS DIAS VAN POR VALOR Y NO POR LARGO (C60). Cinco dias equivocados satisfacen un largo de
		// cinco igual de bien que los cinco que la sucursal mando, y el largo es lo unico que una
		// ingesta con los campos cruzados no cambiaria.
		[Test]
		public void TheStaysLandOnTheRecordAsTheFactsTheyWere()
		{
			Ingested entered = Ingest(journal.Api, ADealershipNarrative.TheStays());

			Assert.That(entered.Applied, Is.EqualTo(ADealershipNarrative.StaySignalsThatApply));
			Assert.That(entered.Discarded, Is.EqualTo(0));
			Assert.That(entered.Rejected, Is.EqualTo(0));

			Assert.That(journal.Api.LastEventApplied(),
				Is.EqualTo(ADealershipNarrative.TheLastStayEvent));

			// LAS DOS POR INSTANTE, que son las unicas que alcanzan la estadia que quedo abierta.
			Assert.That(journal.Api.IsInsideTheShopAt(InsideTheOpenStay), Is.True,
				"IsInsideTheShopAt: la estadia del 5 de octubre entro por la entrada del 33 y nadie la "
				+ "cerro, asi que el vehiculo sigue adentro.");

			Assert.That(journal.Api.IsInsideTheShopAt(BetweenTheTwoClosedStays), Is.False,
				"Y en junio no estaba adentro de ninguna: la de mayo ya habia salido y la de julio no "
				+ "habia entrado.");

			// CADA UNO ADENTRO DE SU PROPIA CLAUSULA, y esto se corrigio despues de MEDIRLO: con la
			// sucursal y el asunto cruzados en el script de la entrada, un
			// Does.Contain(sucursal).And.Contains(asunto) quedaba VERDE, porque la frase nombra a los
			// dos y no dice cual esta donde. Para la estadia ABIERTA la frase es el unico oraculo que
			// hay -- ninguna lectura de periodo la alcanza, asi que no tiene fila ni columnas -- de
			// modo que si esta afirmacion no discrimina, nada discrimina.
			Assert.That(
				journal.Api.DescribeTheStayAt(InsideTheOpenStay),
				Does.Contain("en " + ADealershipNarrative.TheOpenShopBranch + " desde el")
					.And.Contains("Entró por " + ADealershipNarrative.TheOpenShopSubject),
				"DescribeTheStayAt dice la estadia ABIERTA con la sucursal en la clausula de la "
				+ "sucursal y el asunto en la del asunto: si el script cruzara los dos campos, esta "
				+ "frase los diria al reves.");

			Assert.That(
				journal.Api.DescribeTheStayAt(InsideTheFirstClosedStay),
				Does.Contain("en " + ADealershipNarrative.TheFirstShopBranch + " desde el")
					.And.Contains("Entró por " + ADealershipNarrative.TheFirstShopSubject),
				"Y la del acto unico cuando se pregunta por un instante que ELLA cubre.");

			// LAS SEIS POR DIAS, y ninguna de ellas ve la abierta.
			Assert.That(journal.Api.HasAnyStayBetween(TheYearFrom, TheYearTo), Is.True);

			Assert.That(
				journal.Api.DaysInTheShopBetween(TheYearFrom, TheYearTo),
				Is.EqualTo(new[]
				{
					"2026-05-11", "2026-05-12", "2026-05-13", "2026-07-06", "2026-07-07",
				}),
				"Los CINCO dias que las dos sucursales dijeron, por valor y ascendentes. Tres los mando "
				+ "el acto unico y dos la entrega, asi que un camino roto se ve en cual falta.");

			Assert.That(journal.Api.DaysInTheShopCountBetween(TheYearFrom, TheYearTo), Is.EqualTo(5));

			Assert.That(
				journal.Api.DescribeDaysInTheShopBetween(TheYearFrom, TheYearTo),
				Does.Contain("5 días").And.Contains("2 estadías"));

			Assert.That(journal.Api.StayCountBetween(TheYearFrom, TheYearTo), Is.EqualTo(2),
				"DOS estadias con dias registrados, no tres: la abierta no entra en un periodo dicho "
				+ "en dias, y no cinco: dias y estadias son dos cuentas distintas.");

			ShopStayRecord record = journal.Api.StaysBetween(TheYearFrom, TheYearTo);

			Assert.That(
				record.Branch,
				Is.EqualTo(new[]
				{
					ADealershipNarrative.TheFirstShopBranch, ADealershipNarrative.TheSecondShopBranch,
				}),
				"Las dos filas en orden de ENTRADA, con la sucursal de cada una.");

			Assert.That(
				record.Subject,
				Is.EqualTo(new[]
				{
					ADealershipNarrative.TheFirstShopSubject, ADealershipNarrative.TheSecondShopSubject,
				}));

			Assert.That(
				record.EnteredAt,
				Is.EqualTo(new[] { "2026-05-11 08:30:00", "2026-07-06 07:15:00" }),
				"Y los instantes de entrada con su HORA, tal como llegaron por el cable.");

			Assert.That(
				record.CoveredDays,
				Is.EqualTo(new[]
				{
					"2026-05-11 2026-05-12 2026-05-13", "2026-07-06 2026-07-07",
				}));

			// CADA FILA SE DICE A SI MISMA, y el asunto se afirma ADENTRO DE LA CLAUSULA DEL ASUNTO y
			// la sucursal adentro de la suya. Un Does.Contain pelado se satisface con la frase entera:
			// SH-4 lo midio -- con la columna del asunto trayendo la sucursal, la prueba quedaba verde.
			Assert.That(
				record.Says[0],
				Does.Contain("Entró por " + ADealershipNarrative.TheFirstShopSubject)
					.And.Contains("en " + ADealershipNarrative.TheFirstShopBranch + " desde el"));

			Assert.That(
				record.Says[1],
				Does.Contain("Entró por " + ADealershipNarrative.TheSecondShopSubject)
					.And.Contains("en " + ADealershipNarrative.TheSecondShopBranch + " desde el"));
		}

		// LO QUE LOS GASTOS DEJAN EN EL LIBRO, afirmado en negocio y no solo en contadores. Es el hermano
		// de los dos renglones de arriba, una global de raiz mas alla, y hace falta por lo mismo: todo lo
		// demas de este fixture seguiria verde con una ingesta que aplicara el comando equivocado de los
		// dos, o el correcto con los campos cruzados.
		//
		// EL ORACULO SON LAS SIETE LECTURAS DE LA PUERTA, y las cifras van POR VALOR: un monto que llego
		// a otra columna suma igual de bien, y la clase que un feed no puede decir la pone el comando.
		//
		// Y LAS FILAS SALEN EN ORDEN DEL INSTANTE EN QUE SE INCURRIERON Y NO EN EL QUE LLEGARON, que es
		// lo que esta lista mide y las otras cuatro no: la narrativa manda el lavado de AGOSTO antes que
		// el cambio de aceite de MAYO, asi que una ingesta que apilara en orden de llegada pondria estas
		// tres filas en otro orden.
		[Test]
		public void TheCostsLandOnTheLedgerAsTheFactsTheyWere()
		{
			Ingested entered = Ingest(journal.Api, ADealershipNarrative.TheCosts());

			Assert.That(entered.Applied, Is.EqualTo(ADealershipNarrative.CostSignalsThatApply));
			Assert.That(entered.Discarded, Is.EqualTo(0));
			Assert.That(entered.Rejected, Is.EqualTo(0));

			Assert.That(journal.Api.LastEventApplied(),
				Is.EqualTo(ADealershipNarrative.TheLastCostEvent));

			// LAS TRES SITUACIONES QUE UN PERIODO PUEDE TENER, cada una sobre el mismo libro: el anio
			// tiene las tres entradas en DOS monedas, el semestre las dos de colones en UNA, y 2025
			// ninguna.
			Assert.That(journal.Api.HasAnyCostBetween(TheCostYearFrom, TheCostYearTo), Is.True);
			Assert.That(journal.Api.HasAnyCostBetween(TheYearBeforeFrom, TheYearBeforeTo), Is.False,
				"Y NO en 2025, donde la ventana de la duradera todavia no empezaba.");

			Assert.That(
				journal.Api.IsSpendSaidInOneCurrencyBetween(TheColonesHalfFrom, TheColonesHalfTo),
				Is.True);
			Assert.That(
				journal.Api.IsSpendSaidInOneCurrencyBetween(TheCostYearFrom, TheCostYearTo), Is.False,
				"El anio entero mezcla monedas, asi que no esta dicho en una sola.");

			Assert.That(journal.Api.CostEntryCountBetween(TheCostYearFrom, TheCostYearTo), Is.EqualTo(3));
			Assert.That(
				journal.Api.CostEntryCountBetween(TheColonesHalfFrom, TheColonesHalfTo), Is.EqualTo(2),
				"DOS en el semestre, que es lo que hace que la cuenta sea del periodo y no del libro.");

			// LAS DOS VISTAS EN UNA SOLA RESPUESTA (L6), y TIENEN que diferir: es el caso de las llantas
			// de marzo. La caja del semestre son las dos entradas de colones enteras; lo consumido es el
			// cambio de aceite entero mas la parte que a ese semestre le toca de las llantas.
			string theColonesHalf =
				journal.Api.DescribeSpendBetween(TheColonesHalfFrom, TheColonesHalfTo);

			Assert.That(theColonesHalf, Does.Contain("Salieron ₡285000.00 del banco"),
				"La caja del semestre, sumada por el DOMINIO: 240000 mas 45000, los dos montos como el "
				+ "feed los mando.");
			Assert.That(theColonesHalf, Does.Not.Contain("consumió ₡285000.00"),
				"Y lo consumido DIFIERE de la caja: la duradera se reparte a lo largo de 48 meses.");
			Assert.That(
				theColonesHalf,
				Does.Contain(
					"₡240000.00 en " + ADealershipNarrative.TheTyresSubject
					+ ", en línea recta a lo largo de 48 meses desde el 2026-03-12 09:00:00"),
				"Y la vida util que llego por el feed va dicha adentro de la regla, con el asunto y el "
				+ "instante de anclaje: cruzar la vida con el monto cambia esta frase.");

			string theYear = journal.Api.DescribeSpendPerCurrencyBetween(TheCostYearFrom, TheCostYearTo);

			Assert.That(theYear, Does.Contain("2 monedas distintas"));
			Assert.That(theYear, Does.Contain("₡285000.00"), "El colon por su lado.");
			Assert.That(theYear, Does.Contain("$120.50"),
				"Y el dolar con su propio signo: las dos monedas del feed llegaron distintas, asi que una "
				+ "moneda cruzada entre filas cambia esta frase.");
			Assert.That(theYear, Does.Not.Contain("285120"), "Y NINGUN total entre las dos.");

			Assert.That(
				journal.Api.DescribeTwoPeriodsOfSpendCompared(
					TheColonesHalfFrom, TheColonesHalfTo, TheYearBeforeFrom, TheYearBeforeTo),
				Does.Contain("Primer período: Entre el 2026-01-01 00:00:00")
					.And.Contains("no hay ningún gasto registrado"));

			CostLedgerRecord record = journal.Api.CostEntriesBetween(TheCostYearFrom, TheCostYearTo);

			Assert.That(
				record.IncurredAt,
				Is.EqualTo(new[]
				{
					"2026-03-12 09:00:00", "2026-05-20 10:30:00", "2026-08-05 14:15:00",
				}),
				"Las tres filas en orden del instante en que se INCURRIERON, con su hora tal como llego "
				+ "por el cable -- y NO en el orden en que la narrativa las mando, que pone el lavado de "
				+ "agosto en medio.");

			Assert.That(
				record.Subject,
				Is.EqualTo(new[]
				{
					ADealershipNarrative.TheTyresSubject, ADealershipNarrative.TheOilChangeSubject,
					ADealershipNarrative.TheWashSubject,
				}),
				"Los asuntos con las palabras que la sucursal mando, sin tocar.");

			Assert.That(
				record.Money, Is.EqualTo(new[] { "₡240000.00", "₡45000.00", "$120.50" }),
				"El dinero dicho por el DOMINIO, con el signo y los decimales de cada moneda. LOS TRES "
				+ "MONTOS LLEGARON CON UN CERO A LA DERECHA que el journal deja caer, asi que esta "
				+ "afirmacion tambien dice que Normalized y Expressed lo reponen.");

			Assert.That(
				record.Classification, Is.EqualTo(new[] { "Durable", "Recurring", "Recurring" }),
				"La clase por el NOMBRE del miembro, y la puso CUAL de los dos comandos aplico la "
				+ "ingesta: ninguna de las dos senales trae un campo de clasificacion.");

			Assert.That(record.ExpectedLife, Is.EqualTo(new[] { "48 meses", "", "" }),
				"La vida util solo la lleva la duradera: la senal del recurrente no tiene por donde "
				+ "decirla, y las dos celdas vacias son eso y no una vida de cero meses.");

			Assert.That(
				record.Says[0],
				Does.Contain("₡240000.00 en " + ADealershipNarrative.TheTyresSubject
					+ " el 2026-03-12 09:00:00")
					.And.Contains("Duradero"),
				"Y cada fila se dice entera, con el asunto ADENTRO de su propia clausula (C60): un "
				+ "Does.Contain suelto pasaria con dos columnas cruzadas.");

			Assert.That(
				record.Says[2],
				Does.Contain("$120.50 en " + ADealershipNarrative.TheWashSubject
					+ " el 2026-08-05 14:15:00")
					.And.Contains("Recurrente"),
				"La tercera fila dice lo suyo: otra moneda, otra clase y otro asunto que la primera.");
		}

		private static readonly DateTime TheCostYearFrom =
			new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheCostYearTo =
			new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc);

		private static readonly DateTime TheColonesHalfFrom =
			new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheColonesHalfTo =
			new DateTime(2026, 6, 30, 23, 59, 59, DateTimeKind.Utc);

		private static readonly DateTime TheYearBeforeFrom =
			new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheYearBeforeTo =
			new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc);

		private static readonly DateTime InsideTheOpenStay =
			new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime BetweenTheTwoClosedStays =
			new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime InsideTheFirstClosedStay =
			new DateTime(2026, 5, 12, 12, 0, 0, DateTimeKind.Utc);

		private const string TheYearFrom = "2026-01-01";
		private const string TheYearTo = "2026-12-31";

		// ------------------------------------------------------ el aparejo

		// El bucle del Portador, a mano. Se hace aca en vez de usar BucleDeRefresh porque el bucle
		// de verdad necesita una fuente HTTP, y lo que estas pruebas miden es la ingesta y no el
		// transporte: la senal ya esta, y lo unico que falta es rutearla y aplicarla. Es literalmente
		// el cuerpo del foreach de BucleDeRefresh.Ejecutar.
		private static Ingested Ingest(
			DealershipCarAPI api, IReadOnlyList<SenalDeEvento> signals)
		{
			Ingested counted = new Ingested();

			foreach (SenalDeEvento signal in signals)
			{
				IMensajeDeEvento message = DealershipEventRoute.Route(signal);

				if (message == null) { counted.Discarded++; continue; }

				if (api.Apply(message).Length > 0) { counted.Rejected++; continue; }

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

		// Los primeros cuantos de la ficha, para simular una caida a mitad de la publicacion.
		private static IReadOnlyList<SenalDeEvento> TheFirstOfTheSheet(int howMany)
		{
			List<SenalDeEvento> some = new List<SenalDeEvento>();

			foreach (SenalDeEvento signal in ADealershipNarrative.TheWholeSpecSheet())
			{
				if (some.Count == howMany) break;
				some.Add(signal);
			}

			return some;
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
