using System;
using System.Linq;
using System.Reflection;
using Concesionario.App;
using Concesionario.Wasm;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LOS DOS PORTADORES DE MS-7, Y LO QUE CADA CAMPO TIENE QUE SER: la respuesta de la puerta, sin
	// recomponer nada. Diecinueve lecturas existen; estos dos portadores llaman DIECISEIS, y las tres
	// que no se llaman tienen su razon escrita abajo, en
	// TheThreeReadsThisScreenDoesNotCallAndTheReasonForEachIsWritten.
	//
	// DEC-48 SIGUE MANDANDO: los dos se leen en el MISMO Reread que la ficha y la mitad del vehiculo, y
	// NINGUNO se fusiona con esos dos. La ficha es de un MODELO, la mitad del vehiculo se mide contra el
	// sobre, la cuenta atras es de la AGENDA de este carro y los precios se juzgan contra un instante.
	// Cuatro claves distintas, cuatro portadores.
	//
	// LO QUE SE AFIRMA CONTRA LA PUERTA Y LO QUE SE AFIRMA POR LITERAL, y la diferencia importa (E49
	// obligacion 2). Que el portador entregue lo que la puerta contesto se afirma CONTRA la puerta,
	// porque eso es exactamente su oficio. Que la puerta no este uniformemente equivocada se afirma por
	// LITERAL contra lo que se recibio: el ancla de 2025-02-01 con el odometro en 21000, la lectura de
	// 35000 observada el 2026-01-20, el limite de 21345 y el de 620 dias -- preguntado el 2026-02-01, lo
	// que queda es 7345 km y 255 dias, y ninguno de los dos numeros lo puede contestar nada mas de este
	// fixture (C60).
	[TestFixture]
	public sealed class TheMaintenanceCarriersAreReadInTheSameRereadAndHandOverTheStandingsAndTheRows
	{
		private const string TheVin = "5YJ3E1EA7JF000007";
		private const string TheUnitEverythingIsIn = "UnitA";

		private const string TheKindWithACountdown = "FluidChange";
		private const string TheKindThatWasCompletedTwice = "Inspection";
		private const string TheKindMeasuredOnlyInTime = "TimedComponent";
		private const string TheKindThatIsNotOnTheSchedule = "Repair";

		// EL ANCLA Y EL LIMITE DE LA CLASE QUE LLEVA LA CUENTA ATRAS. Los dos limites se eligieron para
		// que las dos restas den numeros que nada mas de este fixture pueda contestar.
		private const int TheDistanceLimitNothingElseCouldAnswer = 21345;
		private const int TheDayLimitNothingElseCouldAnswer = 620;
		private const int TheOdometerAtItsAnchor = 21000;

		private static readonly DateTime TheAnchorOfTheCountdown =
			new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc);

		private const int TheDistanceThatIsLeft = 7345;
		private const int TheDaysThatAreLeft = 255;
		private const string TheUnitAsAPersonReadsIt = "km";

		// LA CLASE COMPLETADA DOS VECES, con sus dos instantes y sus dos lecturas tal como se reciben.
		private static readonly DateTime TheFirstCompletion =
			new DateTime(2025, 4, 11, 12, 0, 0, DateTimeKind.Utc);
		private const int TheOdometerAtTheFirst = 22500;

		private static readonly DateTime TheSecondCompletion =
			new DateTime(2025, 8, 22, 9, 30, 0, DateTimeKind.Utc);
		private const int TheOdometerAtTheSecond = 27300;

		// Y LA QUE SE MIDE SOLO EN TIEMPO, cuya lectura el dominio descarta: la fila dice que no hay
		// ninguna, con palabras, y nunca un cero.
		private static readonly DateTime TheTimedCompletion =
			new DateTime(2025, 11, 5, 7, 45, 0, DateTimeKind.Utc);
		private const int TheOdometerThatWasReceivedAndDiscarded = 29100;

		private static readonly DateTime TheDayTheReadingWasObserved =
			new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc);
		private const int TheOdometerObserved = 35000;

		private static readonly DateTime TheInstantTheScreenAsksAt =
			new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);

		private AScheduleOnARealJournal journal;

		[SetUp]
		public void Setup()
		{
			journal = new AScheduleOnARealJournal(TheVin, AppState.TheProfileAtDelivery);
		}

		[TearDown]
		public void TearDown()
		{
			journal.Dispose();
		}

		// ===== LAS DIEZ RESPUESTAS DE LA PARTE A, POR FILA ================================

		// LAS DIEZ, CADA UNA CONTRA LA DE LA PUERTA, Y LAS DOS RESTAS POR LITERAL. La frase entera se
		// afirma LETRA POR LETRA: es lo unico que atrapa una frase rival armada en la caratula, porque
		// el barrido de aritmetica no la ve -- una suma con un operando de texto es concatenacion y
		// nunca se marca.
		[Test]
		public void TheTenPartAAnswersOfARowAreTheDoorsOwnAndTheSentenceIsLetterForLetter()
		{
			TheStagedRecord();

			HowOneOperationStands row = WhatTheScreenIsHanded().Of(TheKindWithACountdown);

			Assert.That(row.Kind, Is.EqualTo(TheKindWithACountdown));
			Assert.That(row.CountdownRefused, Is.False);

			Assert.That(
				row.Says,
				Is.EqualTo(journal.Api.DescribeCountdown(TheKindWithACountdown, TheInstantTheScreenAsksAt)),
				"La cuenta atras entera es la de la puerta LETRA POR LETRA.");

			Assert.That(row.Says, Does.Contain("distancia: te faltan 7345 km"));
			Assert.That(row.Says, Does.Contain("tiempo: te faltan 255 días"),
				"Y dice LAS DOS dimensiones: elegir la mas cercana dejaria el numero de la pantalla "
				+ "discrepando del motivo del estado para quien lea los dos juntos.");

			Assert.That(row.DistanceCanBeSaid, Is.True);
			Assert.That(row.DistanceRemaining, Is.EqualTo(TheDistanceThatIsLeft),
				"Lo que queda de distancia, POR LITERAL contra lo que se recibio: 21345 menos los 14000 "
				+ "que van del ancla en 21000 a la lectura de 35000.");
			Assert.That(row.DistanceUnit, Is.EqualTo(TheUnitAsAPersonReadsIt),
				"La unidad cruza por su NOMBRE y nunca por su codigo.");
			Assert.That(row.DistanceStanding, Is.EqualTo("StillAhead"));
			Assert.That(
				row.DistanceSays,
				Is.EqualTo(journal.Api.DescribeDistanceRemaining(
					TheKindWithACountdown, TheInstantTheScreenAsksAt)));

			Assert.That(row.DaysCanBeSaid, Is.True);
			Assert.That(row.DaysRemaining, Is.EqualTo(TheDaysThatAreLeft),
				"Y lo que queda de plazo, POR LITERAL: 620 menos los 365 dias que van del ancla al "
				+ "instante preguntado.");
			Assert.That(row.TimeStanding, Is.EqualTo("StillAhead"));
			Assert.That(
				row.DaysSays,
				Is.EqualTo(journal.Api.DescribeDaysRemaining(
					TheKindWithACountdown, TheInstantTheScreenAsksAt)));

			// Y LAS DOS MITADES NO ESTAN CRUZADAS: los dos montos son distintos y las dos frases
			// tambien, asi que cruzarlas cambia cuatro de las afirmaciones de arriba.
			Assert.That(row.DistanceRemaining, Is.Not.EqualTo(row.DaysRemaining));
			Assert.That(row.DistanceSays, Is.Not.EqualTo(row.DaysSays));
		}

		// LAS SIETE SITUACIONES NO SE APLASTAN A UN BOOLEANO, y la fila de una clase que no esta
		// agendada lo prueba: contesta una situacion PROPIA, sus dos montos llegan en cero -- que NO es
		// una distancia ni un plazo -- y su frase se dice UNA sola vez porque no hay dos mitades.
		[Test]
		public void AKindWithNoOperationScheduledIsItsOwnSituationAndItsZeroIsNotAMeasurement()
		{
			TheStagedRecord();

			HowOneOperationStands row = WhatTheScreenIsHanded().Of(TheKindThatIsNotOnTheSchedule);

			Assert.That(row.DistanceStanding, Is.EqualTo("NoOperationScheduled"));
			Assert.That(row.TimeStanding, Is.EqualTo("NoOperationScheduled"));

			Assert.That(row.DistanceCanBeSaid, Is.False);
			Assert.That(row.DaysCanBeSaid, Is.False);

			Assert.That(row.DistanceRemaining, Is.EqualTo(0));
			Assert.That(row.DistanceUnit, Is.Empty,
				"Una unidad para una mitad que nadie midio se lee como que hubo medicion.");
			Assert.That(row.DaysRemaining, Is.EqualTo(0));

			Assert.That(
				row.Says,
				Is.EqualTo("no hay ninguna operación de Reparación agendada para este vehículo"),
				"Sin operacion agendada no hay dos mitades que decir, asi que se dice UNA vez.");

			Assert.That(row.Says, Does.Not.Contain(";"),
				"Y no lleva el separador de las dos mitades. Sin este renglon, una frase que dijera dos "
				+ "veces la misma cosa pasaria igual.");
		}

		// LAS NUEVE CLASES DEL CANON ESTAN, no solo las agendadas. Filtrar las que no lo estan dejaria
		// esa situacion inalcanzable, que se lee exactamente como un hueco.
		[Test]
		public void TheNineKindsOfTheCanonAreAllThereAndInTheOrderTheDomainNamesThem()
		{
			TheStagedRecord();

			WhereEachOperationStands handed = WhatTheScreenIsHanded();

			Assert.That(
				handed.Operations.Select(each => each.Kind).ToArray(),
				Is.EqualTo(AppState.TheNineKinds));

			// Y LA BUSQUEDA POR CLASE CONTESTA LA FILA DE ESA CLASE Y NO OTRA. La pantalla reordena las
			// nueve por urgencia, asi que pinta la fila que la busqueda le da: una busqueda corrida
			// pondria la cuenta atras de una operacion bajo el nombre de otra.
			foreach (string kind in AppState.TheNineKinds)
				Assert.That(handed.Of(kind).Kind, Is.EqualTo(kind));
		}

		// ===== EL HISTORIAL ENTERO, COLUMNA POR COLUMNA ===================================

		// LAS CUATRO COLUMNAS DEL JUEGO DE TODAS LAS CLASES, por valor y contra lo que se recibio. La
		// frase de cada fila se afirma con su CONECTIVA -- "se hizo el <instante> con el odómetro en
		// <lectura>" -- y no como dos valores sueltos que se contienen: una frase que cruzara el
		// instante de una fila con la lectura de otra contendria los dos valores igual.
		[Test]
		public void TheCompletionRowsKeepTheirOwnColumnsNewestFirstAndEachOneSaysItself()
		{
			TheStagedRecord();

			WhereEachOperationStands handed = WhatTheScreenIsHanded();

			Assert.That(handed.WhatHasBeenDone, Has.Count.EqualTo(3),
				"Tres completaciones: dos de la clase completada dos veces y una de la que se mide solo "
				+ "en tiempo.");

			AThingThatWasDone timed = handed.WhatHasBeenDone[0];
			AThingThatWasDone second = handed.WhatHasBeenDone[1];
			AThingThatWasDone first = handed.WhatHasBeenDone[2];

			Assert.That(timed.Kind, Is.EqualTo(TheKindMeasuredOnlyInTime),
				"De la mas reciente a la mas vieja, entre todas las clases.");
			Assert.That(timed.CompletedAt, Is.EqualTo("2025-11-05 07:45:00"));
			Assert.That(timed.Reading, Is.EqualTo("sin lectura registrada"),
				"La que llego con una lectura que el dominio descarta dice que no hay ninguna, con "
				+ "palabras. Nunca un cero y nunca la celda vacia, que se leerian las dos como que hubo "
				+ "medicion.");
			Assert.That(timed.Says, Does.Contain("se hizo el 2025-11-05 07:45:00, sin lectura"));

			Assert.That(second.Kind, Is.EqualTo(TheKindThatWasCompletedTwice));
			Assert.That(second.CompletedAt, Is.EqualTo("2025-08-22 09:30:00"));
			Assert.That(second.Reading, Is.EqualTo("27300 km"));
			Assert.That(
				second.Says,
				Does.Contain("se hizo el 2025-08-22 09:30:00 con el odómetro en 27300 km"),
				"La clausula entera. Contener el instante y la lectura sueltos se queda verde con las "
				+ "dos columnas de dos filas cruzadas.");

			Assert.That(first.Kind, Is.EqualTo(TheKindThatWasCompletedTwice));
			Assert.That(first.CompletedAt, Is.EqualTo("2025-04-11 12:00:00"));
			Assert.That(first.Reading, Is.EqualTo("22500 km"));
			Assert.That(
				first.Says,
				Does.Contain("se hizo el 2025-04-11 12:00:00 con el odómetro en 22500 km"));

			// Y CADA FILA NOMBRA SU PROPIA CLASE, que es lo que la hace autodescriptiva sin la columna
			// al lado.
			Assert.That(timed.Says, Does.StartWith("Componente por tiempo:"));
			Assert.That(second.Says, Does.StartWith("Revisión:"));
		}

		// LAS DOS AUSENCIAS DE LA PARTE B SON DOS FRASES DISTINTAS, y esa es la mitad que el juego de
		// todas las clases NO puede decir: contesta la lista vacia y nada mas. La frase por clase es la
		// unica que separa "agendada y nunca hecha" de "este carro no tiene esa operacion", y solo una
		// de las dos la cura una visita al taller.
		[Test]
		public void TheTwoAbsencesOfPartBArriveAsTwoDifferentSentencesPerKind()
		{
			TheStagedRecord();

			WhereEachOperationStands handed = WhatTheScreenIsHanded();

			HowOneOperationStands scheduled = handed.Of(TheKindWithACountdown);
			HowOneOperationStands notScheduled = handed.Of(TheKindThatIsNotOnTheSchedule);

			Assert.That(scheduled.HasBeenCompleted, Is.False);
			Assert.That(notScheduled.HasBeenCompleted, Is.False);

			Assert.That(
				scheduled.CompletionsSays,
				Is.EqualTo("Cambio de fluido: no hay ninguna completación registrada para este vehículo."));

			Assert.That(
				notScheduled.CompletionsSays,
				Does.Contain("no tiene agendada ninguna operación"),
				"La otra ausencia, que es otra frase: no agendada no la cura ninguna visita al taller.");

			Assert.That(
				notScheduled.CompletionsSays, Is.Not.EqualTo(scheduled.CompletionsSays),
				"Y las dos son distintas, que es lo que hace que las dos afirmaciones de arriba midan "
				+ "dos cosas.");

			// Y LA CLASE QUE SI TIENE ALGO CONTESTA QUE SI, con su propia frase.
			Assert.That(handed.Of(TheKindThatWasCompletedTwice).HasBeenCompleted, Is.True);
			Assert.That(
				handed.Of(TheKindThatWasCompletedTwice).CompletionsSays,
				Does.Contain("2 completaciones registradas"));
		}

		// ===== LOS TRES QUE ESTA PANTALLA NO LLAMA, CON SU RAZON ==========================

		// TRES DE LAS DIECINUEVE NO SE LLAMAN AQUI, y cada una tiene su razon escrita. Se afirma sobre
		// el FUENTE de la caratula, porque lo que hay que ver es que el llamado no existe.
		//
		//   CompletionsOf(kind) -- sus filas son un subconjunto de las del juego entero, filtrado por
		//   la columna Kind. Ofrecer las dos pone DOS FORMAS DE UN MISMO HECHO en una sola superficie,
		//   y esta pantalla ya pinta el juego entero en el bloque del historial.
		//
		//   CompletionCount(kind) -- el numero que contesta ya esta ADENTRO de la frase por clase, y la
		//   unica cuenta que una pantalla querria (cuantas entre todas las clases) no la contesta
		//   ninguna lectura y la caratula no puede sumarla.
		//
		//   DescribePublishedServicePrice -- pide un hito, y ningun llamador aporta un hito que no
		//   aprendio del dominio. La enumeracion es la que contesta sin ninguno.
		[Test]
		public void TheThreeReadsThisScreenDoesNotCallAndTheReasonForEachIsWritten()
		{
			string shell = TheCarriersAsTheyAreWritten();

			foreach (string uncalled in new[]
			{
				"CompletionsOf(", "CompletionCount(", "DescribePublishedServicePrice(",
			})
				Assert.That(shell, Does.Not.Contain("api." + uncalled),
					"La caratula llamo '" + uncalled + "'. Las tres estan sin llamar a proposito y la "
					+ "razon de cada una esta escrita en esta prueba; llamarla es una decision y no un "
					+ "detalle.");

			// EL CONTROL, y sin el lo de arriba estaria verde con una caratula que no llama a nada. Las
			// que SI se llaman aparecen.
			foreach (string called in new[]
			{
				"api.CompletionsOfEveryKind()", "api.DescribeCompletions(", "api.HasAnyCompletion(",
				"api.DescribeCountdown(", "api.PublishedServicePricesAsOf(",
				"api.HasAnyPublishedServicePrice()", "api.PublishedServicePriceCount()",
			})
				Assert.That(shell, Does.Contain(called),
					"La caratula dejo de llamar '" + called + "'.");
		}

		// ===== UN SOLO MOMENTO, Y NINGUN CUARTO RELOJ =====================================

		// LOS DOS PORTADORES TOMAN UN SOLO INSTANTE CADA UNO, que es lo que hace que las dos mitades de
		// la cuenta atras y el historial hablen del mismo momento del registro.
		[Test]
		public void EachCarrierTakesTheDoorAndOneInstantAndNothingElse()
		{
			foreach (Type carrier in new[]
			{
				typeof(WhereEachOperationStands), typeof(WhatTheServicesCost),
			})
			{
				MethodInfo read = carrier.GetMethod("ReadThrough");

				Assert.That(read, Is.Not.Null, carrier.Name + " dejo de ofrecer ReadThrough.");

				Assert.That(
					read.GetParameters().Select(each => each.ParameterType.Name).ToArray(),
					Is.EqualTo(new[] { "DealershipCarAPI", "DateTime" }),
					carrier.Name + " toma la puerta y UN instante. Dos instantes son dos momentos que se "
					+ "pueden separar.");

				Assert.That(
					read.GetParameters().Select(each => each.Name).ToArray(),
					Is.EqualTo(new[] { "api", "asOf" }));
			}
		}

		// Y LA PANTALLA NO LE PIDE EL INSTANTE A NADIE, ni agrega un cuarto reloj. Un parametro aca
		// seria dejar que alguien pregunte por un momento que se invento.
		[Test]
		public void TheTwoShellReadsTakeNoArgumentAndAddNoFourthClock()
		{
			foreach (string read in new[]
			{
				"ReadWhereEachOperationStands", "ReadWhatTheServicesCost",
			})
			{
				MethodInfo found = typeof(AppState).GetMethod(read);

				Assert.That(found, Is.Not.Null,
					"La caratula dejo de ofrecer '" + read + "' y la seccion no tiene de donde sacarlo.");

				Assert.That(found.GetParameters(), Is.Empty,
					"'" + read + "' gano un argumento. El instante sale del reloj de la maquina.");
			}

			Assert.That(
				typeof(AppState).GetMethod("ReadWhereEachOperationStands").ReturnType,
				Is.EqualTo(typeof(WhereEachOperationStands)));
			Assert.That(
				typeof(AppState).GetMethod("ReadWhatTheServicesCost").ReturnType,
				Is.EqualTo(typeof(WhatTheServicesCost)));

			// EL CUARTO RELOJ, DICHO AQUI TAMBIEN. El censo del modulo lo afirma como conjunto exacto;
			// este renglon dice de que tarea seria la culpa.
			var handingOutAnInstant = new System.Collections.Generic.List<string>();

			foreach (MethodInfo each in typeof(AppState).GetMethods(
				BindingFlags.Instance | BindingFlags.Static
				| BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
				if (each.ReturnType == typeof(DateTime) && each.GetParameters().Length == 0)
					handingOutAnInstant.Add(each.Name);

			Assert.That(handingOutAnInstant, Has.Count.EqualTo(3),
				"La cuenta atras agrego un miembro que entrega un instante. No hacia falta: el instante "
				+ "de la LECTURA ya existe y las dos lecturas nuevas lo llaman.");
		}

		// Y LA PAGINA LOS LEE EN EL MISMO Reread QUE LAS OTRAS TRES HOJAS. Es lo unico que ve que las
		// cinco se leen una detras de la otra sin nada en el medio que escriba, que es el sentido en que
		// DEC-48 llama a eso un solo momento.
		[Test]
		public void ThePageReadsBothCarriersInTheSameRereadAsTheOtherThreeSheets()
		{
			string page = ThePageAsItIsWritten();

			foreach (string read in new[]
			{
				"state.ReadTheSpecification()",
				"state.ReadWhereThisVehicleStands()",
				"state.ReadTheStaysOfThisVehicle()",
				"state.ReadWhereEachOperationStands()",
				"state.ReadWhatTheServicesCost()",
				"state.ReadHowThisVehicleIsDriven()",
				"state.ReadWhatThisVehicleHasCost()",
				"state.ReadThePlateOfThisVehicle()",
			})
				Assert.That(page, Does.Contain(read),
					"La pagina no hace la lectura '" + read + "'. Las ocho se leen en el mismo Reread: "
					+ "una que se leyera aparte contestaria sobre otro momento del mismo registro.");
		}

		// Y LAS SEIS ESTAN UNA DETRAS DE LA OTRA, SIN NADA EN EL MEDIO, que es lo que "un solo momento"
		// quiere decir. Esta prueba se escribio DESPUES de que la falsificacion encontrara que nada veia
		// la diferencia: MEDIDO, sacar una de las cinco de Reread y llamarla desde otro metodo DESPUES
		// de un await dejo la suite entera VERDE, porque la afirmacion de arriba busca cada lectura por
		// separado y las cinco seguian escritas en la pagina. Buscarlas sueltas afirma que existen; solo
		// la CONTIGUIDAD afirma que son un momento.
		//
		// SON SEIS DESDE EL BLOQUE DEL RITMO DE USO, y la afirmacion se AMPLIO nombrando la sexta en su
		// sitio en vez de aflojarse a un prefijo: la sexta lee su propio instante, asi que si se separa
		// de las otras cinco su ventana de doce meses queda anclada en otro momento del mismo registro.
		//
		// Y SON SIETE DESDE EL BLOQUE DEL LIBRO DE COSTOS, ampliada por tercera vez de la misma manera.
		// La septima lee su propio instante igual que la sexta, y ademas sus dos ventanas tienen que
		// ser LAS MISMAS que las del ritmo: el costo por distancia cruza los dos globales de raiz, asi
		// que una ventana movida un segundo pondria el gasto de un periodo sobre la distancia de otro.
		//
		// Y SON OCHO DESDE EL BLOQUE DE LA PLACA, ampliada por cuarta vez nombrando la octava en su
		// sitio. La octava lee su propio instante: cual placa rige se decide contra un momento, asi
		// que separada de las otras siete contestaria por otro momento del mismo registro. Y AGREGARLA
		// AL FINAL NO PONIA ROJO NADA -- la afirmacion es por contencion, asi que la secuencia de
		// siete seguia adentro de la de ocho: una lectura nueva puesta al final se queda fuera del
		// instrumento sin que nada avise, que es por lo que se amplia y no se deja.
		//
		// LOS DOS NOMBRES DE ESTE PAR LLEVAN UNA CUENTA Y LAS DOS SON FALSAS DESDE OD-5 -- "TheFive"
		// son ocho, y "BothCarriers ... TheOtherThreeSheets" son dos y seis. Renombrarlos huerfanaria
		// dos citas de PLAN-BuildOrder.md, que son el REGISTRO de lo que hicieron aquellas tareas, asi
		// que la decision es de quien lea esto y no de esta tarea.
		[Test]
		public void AndTheFiveReadsAreContiguousWithNothingBetweenThemThatCouldAwait()
		{
			string page = Squeezed(ThePageAsItIsWritten());

			Assert.That(
				page,
				Does.Contain(
					"sheet = state.ReadTheSpecification(); "
					+ "stands = state.ReadWhereThisVehicleStands(); "
					+ "stays = state.ReadTheStaysOfThisVehicle(); "
					+ "operations = state.ReadWhereEachOperationStands(); "
					+ "costs = state.ReadWhatTheServicesCost(); "
					+ "use = state.ReadHowThisVehicleIsDriven(); "
					+ "spend = state.ReadWhatThisVehicleHasCost(); "
					+ "plate = state.ReadThePlateOfThisVehicle();"),
				"Las ocho lecturas dejaron de estar una detras de la otra. Una que se haga aparte -- en "
				+ "otro metodo, o despues de un await -- contesta sobre otro momento del mismo registro, "
				+ "y la pantalla mezcla dos: eso es exactamente lo que 'un solo Reread' existe para "
				+ "impedir. Buscar cada lectura por separado NO lo ve, porque las ocho siguen escritas.");

			// Y NINGUNA SE HACE DOS VECES. Un segundo sitio de llamada es un segundo momento aunque el
			// bloque de arriba siga intacto.
			foreach (string read in new[]
			{
				"state.ReadTheSpecification()",
				"state.ReadWhereThisVehicleStands()",
				"state.ReadTheStaysOfThisVehicle()",
				"state.ReadWhereEachOperationStands()",
				"state.ReadWhatTheServicesCost()",
				"state.ReadHowThisVehicleIsDriven()",
				"state.ReadWhatThisVehicleHasCost()",
				"state.ReadThePlateOfThisVehicle()",
			})
				Assert.That(TimesItSays(page, read), Is.EqualTo(1),
					"'" + read + "' se hace en mas de un sitio de la pagina.");
		}

		// Y NINGUNO DE LOS DOS SE FUSIONO CON LAS DOS HOJAS QUE YA ESTABAN. La ficha es de un modelo y
		// la mitad del vehiculo se mide contra el sobre; meter la agenda o los precios en cualquiera de
		// las dos vuelve falso el nombre del tipo.
		[Test]
		public void NeitherCarrierWasMergedIntoTheSheetOrIntoTheVehicleHalf()
		{
			foreach (Type older in new[]
			{
				typeof(WhatTheManufacturerPublished), typeof(WhereThisVehicleStands),
			})
				foreach (PropertyInfo each in older.GetProperties())
					Assert.That(
						each.PropertyType,
						Is.Not.EqualTo(typeof(System.Collections.Generic.IReadOnlyList<HowOneOperationStands>))
							.And.Not.EqualTo(
								typeof(System.Collections.Generic.IReadOnlyList<APublishedPriceOnTheSheet>)),
						older.Name + " gano una de las dos colecciones nuevas. Son cuatro claves "
						+ "distintas y por eso son cuatro portadores.");

			foreach (string forbidden in new[]
			{
				"Countdown", "Remaining", "Completion", "ServicePrice",
			})
			{
				foreach (PropertyInfo each in typeof(WhatTheManufacturerPublished).GetProperties())
					Assert.That(each.Name, Does.Not.Contain(forbidden),
						"La ficha del fabricante gano '" + each.Name + "'. Lee un catalogo con clave de "
						+ "MODELO; la agenda y la vigencia de un precio son de este carro y de un "
						+ "instante.");

				foreach (PropertyInfo each in typeof(WhereThisVehicleStands).GetProperties())
					Assert.That(each.Name, Does.Not.Contain(forbidden),
						"La mitad del vehiculo gano '" + each.Name + "'. Se mide contra el SOBRE, no "
						+ "contra la agenda.");
			}
		}

		// ===== EL APAREJO =================================================================

		private WhereEachOperationStands WhatTheScreenIsHanded()
		{
			return WhereEachOperationStands.ReadThrough(journal.Api, TheInstantTheScreenAsksAt);
		}

		/// <summary>El fuente con cada corrida de espacios en blanco vuelta un espacio.</summary>
		private static string Squeezed(string source)
		{
			return System.Text.RegularExpressions.Regex.Replace(source, @"\s+", " ");
		}

		private static int TimesItSays(string source, string what)
		{
			int found = 0;
			int at = source.IndexOf(what, StringComparison.Ordinal);

			while (at >= 0)
			{
				found++;
				at = source.IndexOf(what, at + what.Length, StringComparison.Ordinal);
			}

			return found;
		}

		private static string ThePageAsItIsWritten()
		{
			string path = System.IO.Path.Combine(AppContext.BaseDirectory, "MyCar.razor.dato");

			Assert.That(System.IO.File.Exists(path), Is.True,
				"La pagina no llego al output. La prueba no puede pasar por no haber leido nada.");

			return System.IO.File.ReadAllText(path);
		}

		// EL FUENTE DE LOS DOS PORTADORES, JUNTO. Se lee del grupo <None> de la raiz Wasm, que es el
		// mismo que alimenta el barrido de aritmetica: si no llego, esto revienta con el nombre.
		private static string TheCarriersAsTheyAreWritten()
		{
			var said = new System.Text.StringBuilder();

			foreach (string file in new[]
			{
				"WhereEachOperationStands.cs", "WhatTheServicesCost.cs",
			})
			{
				string path = System.IO.Path.Combine(
					AppContext.BaseDirectory, "CodigoComoDato", "Wasm", file);

				Assert.That(System.IO.File.Exists(path), Is.True,
					"'" + file + "' no llego al output. Un barrido que leyo cero archivos esta verde por "
					+ "no haber mirado.");

				said.AppendLine(System.IO.File.ReadAllText(path));
			}

			return said.ToString();
		}

		// LA AGENDA, LAS TRES COMPLETACIONES Y LA LECTURA, EN ESTE ORDEN. Los odometros suben con los
		// instantes: la agenda rechaza una lectura que no calza en la linea.
		private void TheStagedRecord()
		{
			journal.Api.SeedPlanOperation(
				TheKindThatWasCompletedTwice, "TimeOrDistance", 400, 9000, TheUnitEverythingIsIn,
				true, false, new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Utc), 20000,
				TheUnitEverythingIsIn);

			journal.Api.SeedPlanOperation(
				TheKindWithACountdown, "TimeOrDistance",
				TheDayLimitNothingElseCouldAnswer, TheDistanceLimitNothingElseCouldAnswer,
				TheUnitEverythingIsIn, true, false,
				TheAnchorOfTheCountdown, TheOdometerAtItsAnchor, TheUnitEverythingIsIn);

			journal.Api.SeedPlanOperation(
				TheKindMeasuredOnlyInTime, "TimeOnly", 900, 0, TheUnitEverythingIsIn,
				true, false, new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc), 21500,
				TheUnitEverythingIsIn);

			journal.Api.CompleteOperation(
				TheKindThatWasCompletedTwice, TheFirstCompletion, TheOdometerAtTheFirst,
				TheUnitEverythingIsIn);

			journal.Api.CompleteOperation(
				TheKindThatWasCompletedTwice, TheSecondCompletion, TheOdometerAtTheSecond,
				TheUnitEverythingIsIn);

			journal.Api.CompleteOperation(
				TheKindMeasuredOnlyInTime, TheTimedCompletion, TheOdometerThatWasReceivedAndDiscarded,
				TheUnitEverythingIsIn);

			journal.Api.RecordReading(
				TheOdometerObserved, TheUnitEverythingIsIn, TheDayTheReadingWasObserved, "Telemetry");
		}
	}
}
