using System;
using System.IO;
using Choreography.Theater;
using DealershipCar.App;
using NUnit.Framework;
using Puppeteer;

namespace DealershipCar.Dominio.Pruebas
{
	// LOS DOS COMANDOS DE LA REBANADA, DE PUNTA A PUNTA. Es el nivel B de S5: un error de tipeo en un
	// script del DSL no se descubre al compilar, se descubre al ejecutarlo, y deja una entrada
	// permanente que hace loguear un fallo de rehidratacion en cada arranque futuro. Estos dos
	// scripts se ejecutan aca antes de tocar el journal de nadie.
	//
	// LA CLASIFICACION LA RECIBE CUAL COMANDO SE LLAMO, y esta es la mitad de eso que se ve por la
	// PUERTA. La mitad estructural la afirma
	// ARecurringCostTakesNoLifeAndADurableOneRequiresIt del lado de la superficie y
	// TheClassificationIsReceivedByWhichCommandWasCalled del lado del dominio; lo que se afirma aqui
	// es que un gasto registrado por el comando recurrente SALE recurrente y uno registrado por el
	// duradero sale duradero, sin que nadie lo haya dicho por ninguna ranura.
	//
	// SE LEE POR ALedgerReadWithoutTheDoor Y NO POR LA PUERTA, porque la puerta todavia no tiene
	// ninguna lectura del libro: CL-5 escribe las siete. Un comando que escribe donde nadie puede
	// leer esta verde tanto si escribio como si no, y ese es el unico motivo por el que ese ayudante
	// existe.
	//
	// Y SE VUELVE A ABRIR EL JOURNAL, que es lo que separa un campo de C# de un hecho journalizado.
	// Las dos frases tienen que decir letra por letra lo mismo despues del replay: el instante cruza
	// truncado al segundo entero por InUtc y el monto normalizado por Normalized, las dos
	// normalizaciones del borde, y las dos estan ahi para que vivo y reproducido no discrepen.
	[TestFixture]
	public sealed class ARecurringAndADurableCostArriveRecordedThroughTheDoor
	{
		private const string TheVin = "JH4KA7660MC012345";
		private const string TheModel = "SM-SEDAN-2025";

		private const string TheTyres = "Llantas nuevas";
		private const string TheOilChange = "Cambio de aceite";
		private const string TheColon = "CRC";
		private const string TheDollar = "USD";

		private const decimal TheTyresCost = 240000m;
		private const decimal TheOilChangeCost = 25000m;
		private const int FourYearsOfLife = 48;

		private static readonly DateTime TheTyresIncurred =
			new DateTime(2026, 3, 12, 14, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheOilChangeIncurred =
			new DateTime(2026, 5, 4, 9, 15, 0, DateTimeKind.Utc);

		private static readonly DateTime TheYearFrom =
			new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheYearTo =
			new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc);

		private string folder;
		private DealershipCarApp app;
		private DealershipCarAPI api;

		[SetUp]
		public void Setup()
		{
			folder = AFreshJournalFolder.Make();
			Open();
		}

		[TearDown]
		public void TearDown()
		{
			Close();
			AFreshJournalFolder.Sweep(folder);
		}

		// EL COMANDO RECURRENTE. Cuatro parametros y ninguna vida util: la clase la dice el comando.
		[Test]
		public void ARecurringCostArrivesAndComesBackClassifiedByTheCommandThatRecordedIt()
		{
			api.RecordRecurringCost(TheOilChangeIncurred, TheOilChangeCost, TheColon, TheOilChange);

			Assert.That(
				ALedgerReadWithoutTheDoor.CostCountBetween(app, TheYearFrom, TheYearTo),
				Is.EqualTo(1),
				"El script parseo, ato sus cuatro parametros y el gasto llego al libro.");

			string[] rows = ALedgerReadWithoutTheDoor.EachCostEntryBetween(app, TheYearFrom, TheYearTo);

			TestContext.Out.WriteLine("RECURRENTE -- " + rows[0]);

			Assert.That(rows[0], Does.Contain(TheOilChange), "Dice en que se fue, con las palabras "
				+ "que llegaron.");
			Assert.That(rows[0], Does.Contain("₡25000.00"),
				"Y cuanto, dicho por el dominio con el signo y los decimales de su moneda.");
			Assert.That(rows[0], Does.Contain("2026-05-04 09:15:00"),
				"Y cuando, como INSTANTE con su hora: de que dia local es un instante necesitaria una "
				+ "zona que este libro no tiene.");
			Assert.That(rows[0], Does.Contain("Recurrente"),
				"Y RECURRENTE, sin que nadie lo haya dicho: el comando que se llamo es la unica cosa "
				+ "que lo decidio.");
			Assert.That(rows[0], Does.Not.Contain("Duradero"));
		}

		// EL COMANDO DURADERO. Cinco parametros, y el quinto es la vida util, que es OBLIGATORIA.
		[Test]
		public void ADurableCostArrivesWithItsExpectedLifeAndSaysTheRuleItIsSpreadBy()
		{
			api.RecordDurableCost(
				TheTyresIncurred, TheTyresCost, TheColon, TheTyres, FourYearsOfLife);

			string[] rows = ALedgerReadWithoutTheDoor.EachCostEntryBetween(app, TheYearFrom, TheYearTo);

			TestContext.Out.WriteLine("DURADERO -- " + rows[0]);

			Assert.That(rows[0], Does.Contain(TheTyres));
			Assert.That(rows[0], Does.Contain("₡240000.00"));
			Assert.That(rows[0], Does.Contain("Duradero"),
				"DURADERO, y por el mismo motivo: el comando que se llamo.");
			Assert.That(rows[0], Does.Contain("48 meses"),
				"Y la vida util con que llego, que es lo que hace visible la regla con que se reparte "
				+ "-- G2 pide que la persona pueda VER la regla en vez de confiar en ella.");
			Assert.That(rows[0], Does.Not.Contain("Recurrente"));
		}

		// LOS DOS JUNTOS, Y EN DOS MONEDAS. El periodo dice sus dos vistas y no las mezcla, que es
		// L6 y L3 llegando por la puerta en vez de por el agregado.
		[Test]
		public void BothCommandsWriteIntoTheSameLedgerAndThePeriodSaysThemApart()
		{
			api.RecordDurableCost(
				TheTyresIncurred, TheTyresCost, TheColon, TheTyres, FourYearsOfLife);
			api.RecordRecurringCost(TheOilChangeIncurred, TheOilChangeCost, TheColon, TheOilChange);
			api.RecordRecurringCost(
				new DateTime(2026, 7, 20, 10, 0, 0, DateTimeKind.Utc), 120.50m, TheDollar,
				"Escáner de diagnóstico");

			Assert.That(
				ALedgerReadWithoutTheDoor.CostCountBetween(app, TheYearFrom, TheYearTo),
				Is.EqualTo(3));

			string period = ALedgerReadWithoutTheDoor.SpendBetween(app, TheYearFrom, TheYearTo);

			TestContext.Out.WriteLine("EL PERIODO -- " + period);

			Assert.That(period, Does.Contain("₡265000.00"),
				"La caja del colon: 240000 mas 25000, sumados por el DOMINIO.");
			Assert.That(period, Does.Contain("$120.50"),
				"Y el dolar por su lado, con su propio signo.");
			Assert.That(period, Does.Not.Contain("385"),
				"Y NINGUN total entre las dos monedas: no hay factor con que sumarlas, y no lo hay a "
				+ "proposito.");
		}

		// EL REPLAY, Y ES LO QUE SEPARA UN CAMPO DE C# DE UN HECHO JOURNALIZADO. Los dos gastos se
		// vuelven a leer despues de cerrar y reabrir, y las frases tienen que ser LAS MISMAS letra
		// por letra: si el instante o el monto cruzaran sin normalizar, la frase viva y la
		// reproducida dirian numeros distintos y solo se veria despues del primer reinicio.
		[Test]
		public void TheTwoCostsSayTheSameThingAfterTheJournalIsReplayed()
		{
			api.RecordDurableCost(
				TheTyresIncurred, TheTyresCost, TheColon, TheTyres, FourYearsOfLife);
			api.RecordRecurringCost(TheOilChangeIncurred, TheOilChangeCost, TheColon, TheOilChange);

			string[] live = ALedgerReadWithoutTheDoor.EachCostEntryBetween(app, TheYearFrom, TheYearTo);
			string livePeriod = ALedgerReadWithoutTheDoor.SpendBetween(app, TheYearFrom, TheYearTo);

			Close();
			Open();

			string[] replayed =
				ALedgerReadWithoutTheDoor.EachCostEntryBetween(app, TheYearFrom, TheYearTo);

			TestContext.Out.WriteLine("VIVO      -- " + live[0]);
			TestContext.Out.WriteLine("REPLAYADO -- " + replayed[0]);

			Assert.That(replayed, Is.EqualTo(live));
			Assert.That(
				ALedgerReadWithoutTheDoor.SpendBetween(app, TheYearFrom, TheYearTo),
				Is.EqualTo(livePeriod));
		}

		// LA HORA DEL INSTANTE SOBREVIVE, Y NO TRUNCADA A UN DIA. Sin esto lo de arriba estaria verde
		// igual si los dos instantes se hubieran vuelto medianoche: dos frases iguales entre si y las
		// dos equivocadas.
		[Test]
		public void TheInstantThatCrossedKeepsItsHourAndIsTruncatedOnlyToTheWholeSecond()
		{
			api.RecordRecurringCost(
				new DateTime(2026, 5, 4, 9, 15, 42, 987, DateTimeKind.Utc), TheOilChangeCost,
				TheColon, TheOilChange);

			string[] rows = ALedgerReadWithoutTheDoor.EachCostEntryBetween(app, TheYearFrom, TheYearTo);

			Assert.That(rows[0], Does.Contain("2026-05-04 09:15:42"),
				"El segundo entero llega; los milisegundos no, porque el journal los deja caer y el "
				+ "InUtc de la puerta le da al instante la forma que va a tener despues de pasar por "
				+ "el. Un dia con la hora en cero seria otro hecho.");
		}

		// Y LA FRACCION DE SEGUNDO CAMBIA DE QUE PERIODO ES EL GASTO, que es lo que hace que el
		// truncado del borde sea una ley y no un detalle. Un gasto incurrido en el ultimo segundo del
		// periodo, con milisegundos encima, esta ADENTRO -- porque el journal va a guardar ese segundo
		// entero y no la fraccion. Sin el InUtc de la puerta, el valor vivo lleva la fraccion, cae
		// FUERA del periodo, y el reproducido cae adentro: la misma pregunta contestaria dos cosas
		// distintas antes y despues del primer reinicio.
		//
		// MEDIDO: es lo unico que ve el truncado por VALOR. La frase de la fila se dice con formato de
		// segundos, asi que ahi la fraccion es invisible; lo que la ve es la COMPARACION contra el
		// extremo del periodo.
		[Test]
		public void ACostIncurredInTheLastSecondOfThePeriodIsInsideItLiveAndReplayed()
		{
			api.RecordRecurringCost(
				new DateTime(2026, 12, 31, 23, 59, 59, 500, DateTimeKind.Utc), TheOilChangeCost,
				TheColon, TheOilChange);

			int live = ALedgerReadWithoutTheDoor.CostCountBetween(app, TheYearFrom, TheYearTo);

			Close();
			Open();

			int replayed = ALedgerReadWithoutTheDoor.CostCountBetween(app, TheYearFrom, TheYearTo);

			TestContext.Out.WriteLine(
				"EL ULTIMO SEGUNDO DEL PERIODO -- vivo " + live + ", replayado " + replayed);

			Assert.That(live, Is.EqualTo(1),
				"ADENTRO. El extremo del periodo es 23:59:59 y el instante llego con medio segundo "
				+ "encima; el journal guarda el segundo entero, asi que la puerta se lo quita antes "
				+ "de mandarlo y las dos respuestas hablan del mismo instante.");
			Assert.That(replayed, Is.EqualTo(live),
				"Y la respuesta reproducida es la misma. Si el instante cruzara crudo, esta seria 1 y "
				+ "la de arriba 0, y nadie lo veria hasta el primer reinicio.");
		}

		private void Open()
		{
			Directory.CreateDirectory(folder);

			app = new DealershipCarApp(TheVin, "Standard", TheModel);
			app.ConfigureStorage(DatabaseType.FileSystem, "path=" + folder);
			app.Start();

			api = new DealershipCarAPI(app);
		}

		private void Close()
		{
			if (app == null) return;

			app.Dispose();
			app = null;
			api = null;
		}
	}
}
