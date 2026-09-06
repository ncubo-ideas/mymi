using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Choreography.Theater;
using DealershipCar.App;
using DealershipCar.Mcp;
using DealershipCar.Mcp.Tools;
using NUnit.Framework;
using Puppeteer;

namespace DealershipCar.Dominio.Pruebas
{
	// LA UNICA COSA QUE UNA SUITE VERDE NO PODIA PROBAR, DEL BRIEF §14 ITEM 6: que los dias salgan por
	// la tool y sean DIRECCIONABLES COMO FECHAS por un lector que no sabe nada de este modulo. Es la
	// ley L3 -- la respuesta son los dias y no su cuenta -- medida en el unico borde donde importa: una
	// duracion no se puede cruzar con el calendario de nadie, y una lista de fechas si.
	//
	// COMO SE MIDE, Y POR QUE ASI. El lector de la prueba no llama a ninguna lectura de la puerta ni
	// nombra ningun tipo del dominio: toma el string[] que vino en la respuesta, lo parsea con
	// DateTime.ParseExact y el formato ISO, y lo CRUZA con un registro escolar que se arma aparte, con
	// aritmetica de calendario de .NET y nada de aqui. La interseccion se afirma POR VALOR (C60): un
	// Is.Not.Empty probaria que llego una lista, nunca que sus elementos son fechas que alguien puede
	// direccionar una por una.
	//
	// EL ESCENARIO ESTA EN 2020 A PROPOSITO, y no es un detalle de estilo. La tool lee el RELOJ DEL
	// ANFITRION -- la posicion 'esta adentro ahora mismo' se contesta a ese instante y no a uno que
	// alguien pase por parametro (DEC-43, y la tercera invencion prohibida) -- asi que la prueba no
	// puede elegir el instante y tiene que mover el REGISTRO en vez del reloj. Una estadia abierta en
	// 2020 sigue abierta en cualquier corrida futura; una fechada el ano que viene contestaria 'no
	// esta adentro' hasta que llegue, y la prueba se pondria verde por la razon equivocada.
	//
	// EL JOURNAL SE LEVANTA CON LA APP QUE ESCRIBE, Y NO CON EL HOST DE LA CARATULA.
	// ReadOnlyDealershipCarApp.OnHydrated esta VACIO a proposito: el host MCP nunca siembra y nunca
	// migra, asi que la quinta global de raiz llega por el upgrade de la app que escribe y por ningun
	// otro camino. Es lo que mide la ultima prueba de este fixture.
	[TestFixture]
	public sealed class TheAssistantIsToldWhichDaysAndCanAddressEachOne
	{
		private const string TheVin = "1FTFW1ET5DFC10312";
		private const string TheModel = "YARIS-2019";

		// LA ESTADIA DE MAYO: TRES dias, en un solo acto, que es la forma correctiva.
		private const string TheMayBranch = "Taller Central";
		private const string TheMaySubject = "Cambio de frenos";

		private static readonly DateTime MayEntered =
			new DateTime(2020, 5, 11, 8, 30, 0, DateTimeKind.Utc);

		private static readonly DateTime MayCollected =
			new DateTime(2020, 5, 13, 16, 45, 0, DateTimeKind.Utc);

		private static readonly string[] TheMayDays =
			{ "2020-05-11", "2020-05-12", "2020-05-13" };

		// LA DE JULIO: DOS dias donde la otra tiene tres, y en DOS hechos -- la entrada y la entrega --
		// asi que las dos formas de cerrar una estadia estan en el escenario.
		private const string TheJulyBranch = "Taller Escazú";
		private const string TheJulySubject = "Revisión de los 20 mil";

		private static readonly DateTime JulyEntered =
			new DateTime(2020, 7, 6, 7, 15, 0, DateTimeKind.Utc);

		private static readonly DateTime JulyCollected =
			new DateTime(2020, 7, 7, 17, 0, 0, DateTimeKind.Utc);

		private static readonly string[] TheJulyDays = { "2020-07-06", "2020-07-07" };

		// LA DE OCTUBRE QUEDA ABIERTA PARA SIEMPRE, que es lo unico que separa las dos lecturas por
		// INSTANTE de las seis por DIAS.
		private const string TheOpenBranch = "Taller Curridabat";
		private const string TheOpenSubject = "Ruido en la suspensión";

		private static readonly DateTime TheOpenEntry =
			new DateTime(2020, 10, 5, 9, 0, 0, DateTimeKind.Utc);

		private AScheduleOnARealJournal journal;

		[SetUp]
		public void ThreeStaysRecordedThroughTheDoor()
		{
			journal = new AScheduleOnARealJournal(TheVin, "Standard");

			journal.Api.RecordShopStay(
				TheMayBranch, TheMaySubject, MayEntered, MayCollected, TheMayDays);

			journal.Api.RecordShopEntry(TheJulyBranch, TheJulySubject, JulyEntered);
			journal.Api.RecordShopCollection(JulyCollected, TheJulyDays);

			journal.Api.RecordShopEntry(TheOpenBranch, TheOpenSubject, TheOpenEntry);
		}

		[TearDown]
		public void TearDown()
		{
			journal.Dispose();
		}

		// ===== EL BRIEF §14 ITEM 6, COMO MEDICION ========================================

		// EL LECTOR QUE NO SABE NADA DE ESTE MODULO. Parsea cada dia con el formato ISO y lo cruza con
		// un registro escolar armado aparte -- los lunes y miercoles entre mayo y julio de 2020 --
		// usando aritmetica de calendario de .NET y nada de aqui. Si esto pasa, los dias son fechas
		// para cualquiera; si la respuesta fuera una duracion, el cruce no existiria.
		[Test]
		public void AReaderThatKnowsNothingOfThisModuleParsesTheDaysAndCrossesThemWithASchoolRegister()
		{
			ShopStays answered = TheAssistantAsksForTheWholeRecord();

			var parsed = new List<DateTime>();

			foreach (string each in answered.Days)
				parsed.Add(DateTime.ParseExact(
					each, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None));

			Assert.That(parsed, Is.EqualTo(new[]
			{
				new DateTime(2020, 5, 11), new DateTime(2020, 5, 12), new DateTime(2020, 5, 13),
				new DateTime(2020, 7, 6), new DateTime(2020, 7, 7),
			}), "Los CINCO dias, parseados con ParseExact y afirmados por valor. Un largo de cinco lo "
				+ "cumplen igual cinco fechas equivocadas.");

			// EL CRUCE, que es lo que el brief llama el punto entero de L3: los dias en que el carro
			// estuvo en el taller Y habia que llevar a alguien a la escuela.
			var crossed = new List<DateTime>();

			foreach (DateTime each in parsed)
				if (TheSchoolRegister().Contains(each)) crossed.Add(each);

			Assert.That(crossed, Is.EqualTo(new[]
			{
				new DateTime(2020, 5, 11), new DateTime(2020, 5, 13), new DateTime(2020, 7, 6),
			}), "La INTERSECCION por valor. Tres de los cinco dias del taller caen en dias de escuela, y "
				+ "las tres fechas se pueden nombrar una por una: eso es lo que una duracion no puede "
				+ "hacer y lo que este slice existe para dar.");

			// Y EL DISCRIMINADOR: el martes 12 esta en los dias del taller y NO en el registro. Sin
			// este renglon, un cruce que devolviera los cinco dias pasaria igual de verde.
			Assert.That(parsed, Does.Contain(new DateTime(2020, 5, 12)));
			Assert.That(crossed, Does.Not.Contain(new DateTime(2020, 5, 12)),
				"El martes 12 estuvo en el taller y no era dia de escuela. Si el cruce lo trajera, no "
				+ "estaria cruzando nada.");
		}

		// Y LA FRASE VIENE AL LADO, ENTERA, para quien no puede hacer nada con una lista de fechas. Son
		// las dos mitades de E48 en una sola respuesta, y las dos las compone el dominio.
		[Test]
		public void TheSentenceComesBesideTheDaysAndTheDomainComposedBoth()
		{
			ShopStays answered = TheAssistantAsksForTheWholeRecord();

			Assert.That(answered.Says, Is.EqualTo(
				journal.Api.DescribeDaysInTheShopBetween(answered.From, answered.To)),
				"La frase es la de la puerta, letra por letra y con los mismos dos limites. Una que la "
				+ "caratula rearmara seria una frase rival que el dominio nunca dijo.");

			Assert.That(answered.Says, Does.Contain("5 días").And.Contains("2 estadías"));
		}

		// ===== LOS DOS LIMITES EN BLANCO, Y QUE QUIERE DECIR 'TODO EL REGISTRO' ===========

		// CON LOS DOS EN BLANCO SE CONTESTA SOBRE TODO EL REGISTRO, Y LA RESPUESTA DICE CON QUE LIMITES
		// LO HIZO. El registro no tiene extremos naturales -- ninguna lectura de la puerta contesta el
		// primer ni el ultimo dia con estadia -- asi que 'todo' se dice con el periodo mas ancho que un
		// dia de calendario puede nombrar, y ese periodo VIAJA en la respuesta. Un limite inventado
		// produce entonces una respuesta visiblemente acotada y nunca un hecho falso.
		[Test]
		public void WithBothBoundsBlankTheAnswerCoversTheWholeRecordAndNamesTheBoundsItUsed()
		{
			ShopStays answered = TheAssistantAsksForTheWholeRecord();

			Assert.That(answered.WholeRecord, Is.True);
			Assert.That(answered.From, Is.EqualTo("0001-01-01"));
			Assert.That(answered.To, Is.EqualTo("9999-12-31"));

			Assert.That(answered.DayCount, Is.EqualTo(5), "Los cinco dias de las dos estadias cerradas.");
			Assert.That(answered.StayCount, Is.EqualTo(2),
				"DOS estadias y no cinco: estadias y dias son dos cuentas distintas, y cruzarlas cambia "
				+ "esta afirmacion y la de arriba.");

			Assert.That(answered.Stays, Has.Count.EqualTo(2));

			Assert.That(answered.Stays[0].Branch, Is.EqualTo(TheMayBranch));
			Assert.That(answered.Stays[0].Subject, Is.EqualTo(TheMaySubject));
			Assert.That(answered.Stays[0].EnteredAt, Is.EqualTo("2020-05-11 08:30:00"),
				"El instante de entrada con su HORA: un instante nunca se dice como un dia a secas.");
			Assert.That(answered.Stays[0].CoveredDays,
				Is.EqualTo("2020-05-11 2020-05-12 2020-05-13"));
			Assert.That(answered.Stays[0].Says, Does.Contain("Entró por " + TheMaySubject));

			Assert.That(answered.Stays[1].Branch, Is.EqualTo(TheJulyBranch));
			Assert.That(answered.Stays[1].Subject, Is.EqualTo(TheJulySubject));
			Assert.That(answered.Stays[1].CoveredDays, Is.EqualTo("2020-07-06 2020-07-07"));

			Assert.That(answered.AnyStayInThePeriod, Is.True);

			Assert.That(answered.Vin, Is.EqualTo(TheVin));
			Assert.That(answered.HasData, Is.True);
			Assert.That(answered.StaysKnown, Is.True);
			Assert.That(answered.BoundsUnderstood, Is.True);
			Assert.That(answered.Folder, Is.EqualTo(Path.GetFullPath(journal.Folder)),
				"Y la respuesta dice de que carpeta salio. Sin esto, una respuesta correcta sobre OTRO "
				+ "vehiculo de la maquina pasaria igual.");

			// EL INSTANTE EN QUE SE PREGUNTO, afirmado por su FORMA y no por su valor: lo lee el reloj
			// del anfitrion, asi que la prueba no lo puede elegir. Lo que si se puede afirmar es que
			// viaja, que es UTC y que esta truncado al segundo entero que el journal guarda.
			DateTime askedAt = DateTime.Parse(
				answered.AskedAt, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

			Assert.That(askedAt.Kind, Is.EqualTo(DateTimeKind.Utc));
			Assert.That(askedAt.Ticks % TimeSpan.TicksPerSecond, Is.EqualTo(0),
				"El instante llega truncado al segundo, que es la resolucion con la que el journal "
				+ "guarda un instante.");
		}

		// Y LOS DOS LIMITES DE VERDAD ACOTAN. Sin este renglon, una tool que ignorara los dos
		// parametros y contestara siempre todo el registro pasaria la prueba de arriba.
		[Test]
		public void TwoBoundsNarrowTheAnswerToThePeriodTheyName()
		{
			ShopStays may = TheAssistantAsksAbout("2020-05-01", "2020-05-31");

			Assert.That(may.WholeRecord, Is.False);
			Assert.That(may.From, Is.EqualTo("2020-05-01"));
			Assert.That(may.To, Is.EqualTo("2020-05-31"));
			Assert.That(may.Days, Is.EqualTo(TheMayDays));
			Assert.That(may.DayCount, Is.EqualTo(3));
			Assert.That(may.StayCount, Is.EqualTo(1));
			Assert.That(may.Stays, Has.Count.EqualTo(1));
			Assert.That(may.Stays[0].Branch, Is.EqualTo(TheMayBranch));

			// UN PERIODO SIN NINGUNA ESTADIA CONTESTA, y contesta la ausencia en palabras con el
			// periodo nombrado: nunca un cero pelado y nunca un error (L8).
			ShopStays quiet = TheAssistantAsksAbout("2020-01-01", "2020-01-31");

			Assert.That(quiet.Days, Is.Empty);
			Assert.That(quiet.DayCount, Is.EqualTo(0));
			Assert.That(quiet.Stays, Is.Empty);
			Assert.That(quiet.AnyStayInThePeriod, Is.False,
				"Y lo dice tambien como respuesta y no solo como una lista vacia: false con los limites "
				+ "entendidos quiere decir que en ESE periodo no hay ninguna registrada.");
			Assert.That(quiet.Says,
				Does.Contain("2020-01-01").And.Contains("2020-01-31").And.Contains("ninguna estadía"));
		}

		// UN LIMITE QUE NO DICE UN DIA NO REVIENTA EL PROTOCOLO. La puerta lo rechaza con un
		// ArgumentException antes de performar nada, y una tool que lo dejara salir le devolveria al
		// asistente un error de transporte en vez de una respuesta.
		[Test]
		public void ABoundThatDoesNotSayADayIsAnsweredInWordsAndNotThrown()
		{
			ShopStays refused = TheAssistantAsksAbout("10/06/2020", "2020-12-31");

			Assert.That(refused.HasData, Is.True);
			Assert.That(refused.BoundsUnderstood, Is.False);
			Assert.That(refused.Days, Is.Empty);
			Assert.That(refused.Detail, Does.Contain("yyyy-MM-dd"));

			// EL CONTROL: la puerta SI revienta con ese limite. Si dejara de hacerlo, la guarda de la
			// tool estaria protegiendo de nada.
			Assert.Throws<ArgumentException>(
				() => journal.Api.DaysInTheShopBetween("10/06/2020", "2020-12-31"));
		}

		// ===== LA ESTADIA ABIERTA, QUE SEIS DE LAS OCHO LECTURAS NO VEN ==================

		// DEC-52 DICHO EN LA RESPUESTA. Las filas y los dias son las estadias CERRADAS del periodo: una
		// estadia abierta todavia no tiene ningun dia, asi que ningun periodo dicho en dias la alcanza.
		// La tool NO la calla -- la dice aparte, con la unica lectura que la ve -- porque un modelo que
		// leyera '5 dias en 2 estadias' mientras el carro esta adentro AHORA MISMO contestaria una cosa
		// cierta que engana.
		[Test]
		public void TheStayThatIsOpenRightNowIsSaidByItselfBecauseNoDayBoundedReadCanSeeIt()
		{
			ShopStays answered = TheAssistantAsksForTheWholeRecord();

			Assert.That(answered.InsideRightNow, Is.True,
				"El carro entro en 2020 y nadie lo ha entregado, asi que sigue adentro a cualquier "
				+ "instante que el reloj del anfitrion diga hoy.");

			// LAS DOS CLAUSULAS DEL DOMINIO, CADA UNA CON SU SUJETO PEGADO. Un Does.Contain(branch) y un
			// Does.Contain(subject) sueltos los cumple igual la frase con las dos columnas cruzadas:
			// medido por parche en SH-5, sobre esta misma forma.
			Assert.That(
				answered.CurrentStaySays,
				Does.Contain("en " + TheOpenBranch + " desde el"),
				"La sucursal va en la clausula de ENTRADA y no en cualquier parte de la frase.");
			Assert.That(
				answered.CurrentStaySays,
				Does.Contain("Entró por " + TheOpenSubject),
				"Y el asunto en la clausula del ASUNTO.");
			Assert.That(answered.CurrentStaySays, Does.Contain("todavía no lo han entregado"));

			// Y NO ESTA EN LAS FILAS NI EN LOS DIAS, que es la mitad que hace honesta a la de arriba.
			foreach (RecordedStay each in answered.Stays)
				Assert.That(each.Branch, Is.Not.EqualTo(TheOpenBranch),
					"Una estadia abierta no tiene dias, asi que no puede ser fila de un periodo dicho "
					+ "en dias. Si aparece, la ley de DEC-52 se movio.");

			Assert.That(answered.Days, Does.Not.Contain("2020-10-05"));

			// Y SE LE DICE AL MODELO, porque es lo unico que ninguna frase del dominio dice: que la
			// cuenta de arriba no lo incluye.
			Assert.That(answered.Detail, Does.Contain("adentro"),
				"El detail es donde vive la instruccion que un modelo necesita y una persona no: sin "
				+ "ella, '5 dias en 2 estadias' se reporta como el total y el carro esta en el taller.");
		}

		// EL CONTROL DE LA DE ARRIBA, Y SIN EL NO MIDE NADA. Sobre un vehiculo cuya unica estadia esta
		// CERRADA, las mismas dos respuestas dicen lo contrario: no esta adentro, y la frase del
		// instante es la constancia de que ninguna estadia lo cubre.
		[Test]
		public void AndWithNoOpenStayTheSameTwoAnswersSayTheOpposite()
		{
			using (var other = new AScheduleOnARealJournal("JTDKN3DU0A0123456", "Standard"))
			{
				other.Api.RecordShopStay(
					TheMayBranch, TheMaySubject, MayEntered, MayCollected, TheMayDays);

				ShopStays answered = new ShopStayTools(new JournalReader(other.Folder, ""))
					.DaysMyCarSpentInTheShop();

				Assert.That(answered.InsideRightNow, Is.False);
				Assert.That(answered.CurrentStaySays, Does.Contain("ninguna estadía"));
				Assert.That(answered.DayCount, Is.EqualTo(3));
				Assert.That(answered.Detail, Does.Not.Contain("está adentro"));
			}
		}

		// ===== LA TRAMPA DE LA SIEMBRA ===================================================

		// UN JOURNAL QUE LA APP QUE ESCRIBE NO HA VUELTO A ABRIR NO TIENE LA QUINTA GLOBAL, Y LA TOOL LO
		// DICE EN VEZ DE CONTESTAR CERO DIAS. ReadOnlyDealershipCarApp.OnHydrated esta vacio a
		// proposito: el host MCP nunca siembra y nunca migra -- crear la global seria escribir -- asi
		// que 'estadias-en-taller' llega por el upgrade de la app que escribe y por ningun otro camino.
		//
		// CERO DIAS Y 'NO ESTA EN ESTE REGISTRO' NO SON LA MISMA RESPUESTA, y confundirlas es como
		// alguien reporta que el carro nunca estuvo en el taller.
		[Test]
		public void AJournalWrittenBeforeTheFifthGlobalSaysSoInsteadOfAnsweringZeroDays()
		{
			string folder = Path.Combine(
				AScheduleOnARealJournal.TheRootOfEveryTemporaryJournal,
				"sin-estadias-" + Guid.NewGuid().ToString("N"));

			Directory.CreateDirectory(folder);

			var before = new AnAppFromBeforeTheStays(TheVin, TheModel);
			before.ConfigureStorage(DatabaseType.FileSystem, "path=" + folder);
			before.Start();
			try { before.Actor.GracefulExit(); } catch { }

			ShopStays answered = new ShopStayTools(new JournalReader(folder, ""))
				.DaysMyCarSpentInTheShop();

			Assert.That(answered.HasData, Is.True, "El journal existe y tiene entradas.");
			Assert.That(answered.Vin, Is.EqualTo(TheVin), "Y el resto del registro sigue contestando.");

			Assert.That(answered.StaysKnown, Is.False,
				"La quinta global no esta en este journal. Si esto diera true, la tool estaria leyendo "
				+ "algo que la app que escribe todavia no creo.");
			Assert.That(answered.Days, Is.Empty);
			Assert.That(answered.Says, Is.Empty,
				"Y no se inventa ninguna frase: la ausencia de la global no es un periodo sin estadias.");
			Assert.That(answered.Detail, Does.Contain("abra su app"),
				"Lo unico que lo arregla es que la persona abra su app una vez, y es lo unico que hay "
				+ "que decirle: este proceso no lo puede arreglar porque arreglarlo seria escribir.");
		}

		// ------------------------------------------------------ el aparejo

		private ShopStays TheAssistantAsksForTheWholeRecord()
		{
			return new ShopStayTools(new JournalReader(journal.Folder, ""))
				.DaysMyCarSpentInTheShop();
		}

		private ShopStays TheAssistantAsksAbout(string from, string to)
		{
			return new ShopStayTools(new JournalReader(journal.Folder, ""))
				.DaysMyCarSpentInTheShop(from, to);
		}

		// EL REGISTRO ESCOLAR, ARMADO SIN NADA DE ESTE MODULO: los lunes y miercoles entre mayo y julio
		// de 2020, con aritmetica de calendario de .NET. Es el lector que no sabe nada.
		private static List<DateTime> TheSchoolRegister()
		{
			var days = new List<DateTime>();

			for (DateTime each = new DateTime(2020, 5, 1);
				each <= new DateTime(2020, 7, 31);
				each = each.AddDays(1))
				if (each.DayOfWeek == DayOfWeek.Monday || each.DayOfWeek == DayOfWeek.Wednesday)
					days.Add(each);

			return days;
		}

		// La app tal como era antes de SH-3: CUATRO globales de raiz. Copiada palabra por palabra de
		// DealershipCarApp.OnHydrated menos la quinta, y copiada y no heredada porque DealershipCarApp
		// es sealed y sobreescribir OnHydrated reemplazaria las cinco. Cada fixture de esta suite es
		// dueño de su aparejo.
		private sealed class AnAppFromBeforeTheStays : PerformanceV2
		{
			private readonly string vin;
			private readonly string modelCode;

			internal AnAppFromBeforeTheStays(string vin, string modelCode)
				: base(DealershipCarApp.TheActorFor(vin), typeof(MaintenanceSchedule).Assembly)
			{
				this.vin = vin;
				this.modelCode = modelCode;
			}

			protected override void OnHydrated()
			{
				Using(@"
					upgrade('semilla') {
						schedule = MaintenanceSchedule(@vin, @usage);
					}
					upgrade('bitacora-de-eventos') {
						logbook = Logbook();
					}
					upgrade('especificacion-del-modelo') {
						specification = ModelSpecification(@model);
					}
					upgrade('perfil-del-vehiculo') {
						profile = VehicleProfile(@vin);
					}
				")
				.WithParameters(p => {
					p["vin",   typeof(string)] = vin;
					p["usage", typeof(Enum)]   = "Standard";
					p["model", typeof(string)] = modelCode;
				})
				.PerformCommand();
			}
		}
	}
}
