using System;
using System.Linq;
using System.Reflection;
using Concesionario.App;
using Concesionario.App.Views;
using Concesionario.Wasm;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// EL PORTADOR DE LAS ESTADIAS, Y LAS DOS MITADES QUE TIENE QUE LEER EN UN SOLO MOMENTO.
	//
	// SEIS DE LAS OCHO LECTURAS NO VEN UNA ESTADIA ABIERTA (DEC-52): las de PERIODO hablan en dias, y
	// una estadia abierta no tiene ningun dia hasta que la entregan. Asi que un bloque armado solo con
	// ellas contesta "no hay dias" con el carro adentro del taller, sin decirlo. La otra mitad son las
	// DOS lecturas por INSTANTE, y son las unicas que la alcanzan.
	//
	// LAS DOS MITADES SE LEEN EN UN SOLO MOMENTO, Y ESO SE SOSTIENE POR LA FORMA (DEC-48). El portador
	// recibe UN instante y los dos limites de dia, asi que las dos mitades no pueden estar preguntando
	// por momentos distintos: no hay dos instantes que puedan separarse. Un segundo parametro de
	// instante se pone rojo aca, y al escribirlo alguien tiene que decir por que la pantalla pregunta
	// dos veces.
	//
	// Y NO SE AGREGA UN CUARTO RELOJ. La pantalla ya lee el instante de una LECTURA
	// (TheInstantTheScreenAsksAt) y de ahi salen tambien los dos limites del periodo, que es la
	// ventana fija de P-11: la app del dueno no tiene selector de periodo porque no tiene ningun
	// control que le pida un valor a una persona.
	//
	// LA FORMA FUERTE DE LA AFIRMACION SOBRE UNA FRASE, y no la debil que SH-5 midio: se afirma sobre
	// las CLAUSULAS del dominio -- "en <sucursal> desde el" y "Entro por <asunto>" -- porque
	// Does.Contain(sucursal).And.Contains(asunto) se queda verde con las dos columnas cruzadas.
	[TestFixture]
	public sealed class TheStaysCarrierIsReadInTheSameRereadAndHandsOverRowsAndTheSentence
	{
		private const string TheVin = "1FTFW1ET5DFC20450";

		private const string TheYearFrom = "2026-01-01";
		private const string TheYearTo = "2026-12-31";

		private const string TheMarchBranch = "Taller Escazú";
		private const string TheMarchSubject = "Frenos";

		private static readonly DateTime MarchEntered =
			new DateTime(2026, 3, 2, 7, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime MarchCollected =
			new DateTime(2026, 3, 4, 17, 0, 0, DateTimeKind.Utc);

		private static readonly string[] TheMarchDays =
			{ "2026-03-02", "2026-03-03", "2026-03-04" };

		// LA DE JULIO TIENE DOS DIAS DONDE LA DE MARZO TIENE TRES, asi que cruzar las dos filas cambia
		// hasta el largo de la celda de dias.
		private const string TheJulyBranch = "Taller Curridabat";
		private const string TheJulySubject = "Cambio de aceite";

		private static readonly DateTime JulyEntered =
			new DateTime(2026, 7, 20, 9, 15, 0, DateTimeKind.Utc);

		private static readonly DateTime JulyCollected =
			new DateTime(2026, 7, 21, 16, 0, 0, DateTimeKind.Utc);

		private static readonly string[] TheJulyDays = { "2026-07-20", "2026-07-21" };

		private const string TheOctoberBranch = "Taller Alajuela";
		private const string TheOctoberSubject = "Revisión de garantía";

		private static readonly DateTime OctoberEntered =
			new DateTime(2026, 10, 5, 8, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheInstantInsideTheOpenStay =
			new DateTime(2026, 10, 7, 12, 0, 0, DateTimeKind.Utc);

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

		// LAS SEIS RESPUESTAS POR DIAS, CADA UNA CONTRA LA DE LA PUERTA Y POR VALOR. El portador no
		// compone ninguna: lo que entrega es lo que la puerta contesto, y la frase letra por letra.
		[Test]
		public void TheSixDayBoundedAnswersAreTheDoorsOwnAndTheSentenceIsLetterForLetter()
		{
			TheTwoClosedStaysAndTheOpenOne();

			TheStaysOfThisVehicle stays = WhatTheScreenIsHanded();

			Assert.That(stays.FromDay, Is.EqualTo(TheYearFrom));
			Assert.That(stays.ToDay, Is.EqualTo(TheYearTo));

			Assert.That(stays.AnyStayInThePeriod, Is.True,
				"AnyStayInThePeriod: la puerta dice que si hay estadias con dias en el periodo.");

			Assert.That(
				stays.Says,
				Is.EqualTo(journal.Api.DescribeDaysInTheShopBetween(TheYearFrom, TheYearTo,
					TheLanguage.OfTheRecord)),
				"La frase del periodo es la de la puerta LETRA POR LETRA. Es lo unico que atrapa una "
				+ "frase rival armada en la caratula: el barrido de aritmetica no la ve, porque una "
				+ "suma con un operando de texto es concatenacion y nunca se marca.");

			Assert.That(stays.Says, Does.Contain("5 días").And.Contains("2 estadías"),
				"Y la frase es la del periodo y no otra: nombra los dias y en cuantas estadias.");

			Assert.That(
				stays.Days,
				Is.EqualTo(new[]
				{
					"2026-03-02", "2026-03-03", "2026-03-04", "2026-07-20", "2026-07-21",
				}),
				"Days: los cinco dias recibidos, por valor. Un largo de cinco se satisface igual con "
				+ "cinco dias equivocados.");

			Assert.That(stays.DayCount, Is.EqualTo(5), "DayCount: cinco dias.");

			Assert.That(stays.StayCount, Is.EqualTo(2),
				"StayCount: DOS estadias, y no cinco. Dias y estadias son dos cuentas distintas, y "
				+ "cruzarlas cambia esta afirmacion y la de arriba.");

			Assert.That(stays.Rows, Has.Count.EqualTo(2),
				"Y las filas son las estadias CERRADAS del periodo: la abierta no esta entre ellas.");
		}

		// LAS FILAS, COLUMNA POR COLUMNA Y CON LA CLAUSULA DEL DOMINIO AL LADO. Afirmar la sucursal y
		// el asunto sueltos deja pasar dos columnas cruzadas; afirmar la clausula en que el dominio
		// mete cada uno no.
		[Test]
		public void EachRowKeepsItsOwnColumnsAndItsOwnSentence()
		{
			TheTwoClosedStaysAndTheOpenOne();

			TheStaysOfThisVehicle stays = WhatTheScreenIsHanded();

			AStayOnTheRecord march = stays.Rows[0];
			AStayOnTheRecord july = stays.Rows[1];

			Assert.That(march.Branch, Is.EqualTo(TheMarchBranch));
			Assert.That(march.Subject, Is.EqualTo(TheMarchSubject));
			Assert.That(march.EnteredAt, Is.EqualTo("2026-03-02 07:00:00"),
				"El instante de entrada llega con su HORA: un instante nunca se dice como un dia de "
				+ "calendario a secas.");

			Assert.That(march.Says, Does.Contain("en " + TheMarchBranch + " desde el"),
				"La frase de la fila mete la sucursal EN SU CLAUSULA. Contenerla suelta se queda verde "
				+ "con la sucursal de la otra fila.");
			Assert.That(march.Says, Does.Contain("Entró por " + TheMarchSubject),
				"Y el asunto en la suya.");

			Assert.That(july.Branch, Is.EqualTo(TheJulyBranch));
			Assert.That(july.Subject, Is.EqualTo(TheJulySubject));
			Assert.That(july.Says, Does.Contain("en " + TheJulyBranch + " desde el"));
			Assert.That(july.Says, Does.Contain("Entró por " + TheJulySubject));

			Assert.That(march.Says, Is.Not.EqualTo(july.Says),
				"Y las dos frases son distintas, que es lo que hace que las dos afirmaciones de arriba "
				+ "midan dos cosas.");
		}

		// LA CELDA DE DIAS SE PARTE EN DIAS, Y ES UNA DECISION DE RENDERIZADO. La puerta manda los dias
		// de una fila en UNA cadena separada por espacios porque un registro de columnas paralelas es
		// rectangular por construccion y una estadia de tres dias no tiene donde poner tres celdas. La
		// pantalla no es un cable: los devuelve a la forma que la propia puerta usa en
		// DaysInTheShopBetween, un dia por elemento. No es una cuenta -- no hay ningun operador -- y no
		// compone nada: sale exactamente lo que el dominio metio.
		[Test]
		public void TheDaysOfARowComeBackAsDaysAndNotAsOneRunOnCell()
		{
			TheTwoClosedStaysAndTheOpenOne();

			TheStaysOfThisVehicle stays = WhatTheScreenIsHanded();

			Assert.That(stays.Rows[0].CoveredDays, Is.EqualTo(TheMarchDays),
				"Los TRES dias de marzo, uno por elemento y en orden.");

			Assert.That(stays.Rows[1].CoveredDays, Is.EqualTo(TheJulyDays),
				"Y los DOS de julio: los largos son distintos, asi que partir mal cambia esta "
				+ "afirmacion y no la otra.");

			// Y EL CONTROL DE QUE SE PARTIO DE LA CELDA DE LA PUERTA Y NO DE OTRA COSA: la celda cruda
			// pegada con espacios vuelve a ser exactamente lo que la puerta mando.
			ShopStayRecord record = journal.Api.StaysBetween(TheYearFrom, TheYearTo,
				TheLanguage.OfTheRecord);

			Assert.That(
				string.Join(" ", stays.Rows[0].CoveredDays), Is.EqualTo(record.CoveredDays[0]),
				"Partir y volver a pegar tiene que dar la celda de la puerta. Si no, la pantalla "
				+ "esta mostrando dias que la puerta no mando.");
		}

		// LA MITAD POR INSTANTE, QUE ES LA UNICA QUE VE LA ESTADIA ABIERTA. Las seis de arriba
		// contestaron sobre las dos cerradas sin mencionarla, que es correcto y es exactamente por lo
		// que esta mitad tiene que estar.
		[Test]
		public void TheInstantKeyedHalfSeesTheOpenStayThatTheSixDayBoundedReadsCannot()
		{
			TheTwoClosedStaysAndTheOpenOne();

			TheStaysOfThisVehicle stays = WhatTheScreenIsHanded();

			Assert.That(stays.InsideRightNow, Is.True,
				"InsideRightNow al instante en que la pantalla pregunta, que cae adentro de la abierta.");

			Assert.That(
				stays.TheStayRightNowSays, Does.Contain("en " + TheOctoberBranch + " desde el"),
				"La frase de la estadia abierta, con la sucursal en su clausula.");
			Assert.That(
				stays.TheStayRightNowSays, Does.Contain("Entró por " + TheOctoberSubject),
				"Y con el asunto en la suya.");

			Assert.That(
				stays.TheStayRightNowSays,
				Is.EqualTo(journal.Api.DescribeTheStayAt(TheInstantInsideTheOpenStay,
					TheLanguage.OfTheRecord)),
				"Y es la de la puerta letra por letra, al MISMO instante.");

			// Y LA ABIERTA NO ESTA EN NINGUNA DE LAS SEIS: ni en los dias, ni en las filas, ni en las
			// cuentas. Sin este renglon, un bloque que la contara dos veces pasaria.
			Assert.That(stays.Days, Does.Not.Contain("2026-10-05"));
			Assert.That(stays.AskedAt, Is.EqualTo("2026-10-07 12:00:00Z"));

			foreach (AStayOnTheRecord each in stays.Rows)
			{
				Assert.That(each.Branch, Is.Not.EqualTo(TheOctoberBranch),
					"La estadia abierta aparecio como fila. Las filas son las estadias CERRADAS del "
					+ "periodo, y decir lo contrario en la pantalla seria inventarle dias.");
				Assert.That(each.CoveredDays, Is.Not.Empty,
					"Una fila sin dias es una estadia abierta que se colo al registro.");
			}
		}

		// LAS DOS MITADES SON UN SOLO MOMENTO, POR LA FORMA. Un segundo parametro de instante seria
		// dos momentos que pueden separarse, y esta prueba es donde alguien tendria que escribir por
		// que la pantalla pregunta dos veces.
		[Test]
		public void TheCarrierTakesOneInstantAndTwoDayBoundsSoBothHalvesAreOneMoment()
		{
			MethodInfo read = typeof(TheStaysOfThisVehicle).GetMethod("ReadThrough");

			Assert.That(read, Is.Not.Null, "El portador dejo de ofrecer ReadThrough.");

			ParameterInfo[] taken = read.GetParameters();

			Assert.That(
				taken.Select(each => each.ParameterType.Name).ToArray(),
				Is.EqualTo(new[] { "DealershipCarAPI", "String", "String", "DateTime", "String" }),
				"El portador toma la puerta, los DOS limites de dia, UN instante y el idioma. Dos "
				+ "instantes son dos momentos que se pueden separar, y las dos mitades de este bloque "
				+ "contestarian sobre estados distintos del mismo registro.");

			Assert.That(
				taken.Select(each => each.Name).ToArray(),
				Is.EqualTo(new[] { "api", "fromDay", "toDay", "asOf", "language" }),
				"Y el quinto es el IDIOMA y ningun otro: sin este renglon, un segundo instante o un "
				+ "limite mas entrarian con el mismo conteo y la ley de arriba seguiria verde.");
		}

		// Y LA PANTALLA NO LE PIDE NINGUNO DE LOS TRES A NADIE. Los dos limites y el instante salen del
		// reloj de la maquina, adentro de AppState: un parametro aca seria un selector de periodo, y
		// esta app no tiene ningun control que le pida un valor a una persona.
		[Test]
		public void TheShellsReadOfTheStaysTakesNoArgumentAndAddsNoFourthClock()
		{
			MethodInfo read = typeof(AppState).GetMethod("ReadTheStaysOfThisVehicle");

			Assert.That(read, Is.Not.Null,
				"La caratula dejo de ofrecer las estadias y la seccion Historial no tiene de donde "
				+ "sacarlas.");

			Assert.That(
				read.GetParameters().Select(each => each.Name + ":" + each.ParameterType.Name),
				Is.EqualTo(new[] { "language:String" }),
				"La lectura de la pantalla gano un parametro que no es el idioma. La ventana la fija "
				+ "la pantalla con su propio reloj; pedirla seria dejar que alguien pregunte por un "
				+ "periodo que se invento. El IDIOMA si entra, y no es una medicion: es la etiqueta "
				+ "con la que se elige catalogo.");

			Assert.That(read.ReturnType, Is.EqualTo(typeof(TheStaysOfThisVehicle)));

			// EL CUARTO RELOJ, DICHO AQUI TAMBIEN. TheShellHasExactlyTheseThreeMembersThatReadAClock lo
			// afirma como conjunto exacto; este renglon dice de que tarea seria la culpa.
			var handingOutAnInstant = new System.Collections.Generic.List<string>();

			foreach (MethodInfo each in typeof(AppState).GetMethods(
				BindingFlags.Instance | BindingFlags.Static
				| BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
				if (each.ReturnType == typeof(DateTime) && each.GetParameters().Length == 0)
					handingOutAnInstant.Add(each.Name);

			Assert.That(handingOutAnInstant, Has.Count.EqualTo(3),
				"El bloque de estadias agrego un miembro que entrega un instante. No hacia falta: el "
				+ "instante de la LECTURA ya existe y de el salen tambien los dos limites del periodo.");
		}

		// Y LA PAGINA LO LEE EN EL MISMO Reread QUE LAS OTRAS DOS HOJAS. Es lo unico que ve que las
		// tres se leen una detras de la otra sin nada en el medio que escriba, que es el sentido en
		// que DEC-48 llama a eso un solo momento.
		[Test]
		public void ThePageReadsTheStaysInTheSameRereadAsTheOtherTwoSheets()
		{
			string page = ThePageAsItIsWritten();

			foreach (string read in new[]
			{
				"state.ReadTheSpecification(texts.Language)",
				"state.ReadWhereThisVehicleStands(texts.Language)",
				"state.ReadTheStaysOfThisVehicle(texts.Language)",
			})
				Assert.That(page, Does.Contain(read),
					"La pagina no hace la lectura '" + read + "'. Las tres se leen en el mismo Reread: "
					+ "una que se leyera aparte contestaria sobre otro momento del mismo registro.");
		}

		private static string ThePageAsItIsWritten()
		{
			string path = System.IO.Path.Combine(AppContext.BaseDirectory, "MyCar.razor.dato");

			Assert.That(System.IO.File.Exists(path), Is.True,
				"La pagina no llego al output. La prueba no puede pasar por no haber leido nada.");

			return System.IO.File.ReadAllText(path);
		}

		private TheStaysOfThisVehicle WhatTheScreenIsHanded()
		{
			return TheStaysOfThisVehicle.ReadThrough(
				journal.Api, TheYearFrom, TheYearTo, TheInstantInsideTheOpenStay, TheLanguage.OfTheRecord);
		}

		// LAS TRES, EN ESTE ORDEN Y NO EN OTRO. La abierta va de ULTIMA porque la puerta pre-chequea la
		// entrada: con ella abierta desde octubre, registrar la de julio despues seria una entrada
		// fechada antes de la abierta.
		private void TheTwoClosedStaysAndTheOpenOne()
		{
			journal.Api.RecordShopStay(
				TheMarchBranch, TheMarchSubject, MarchEntered, MarchCollected, TheMarchDays);

			journal.Api.RecordShopStay(
				TheJulyBranch, TheJulySubject, JulyEntered, JulyCollected, TheJulyDays);

			journal.Api.RecordShopEntry(TheOctoberBranch, TheOctoberSubject, OctoberEntered);
		}
	}
}
