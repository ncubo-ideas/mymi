using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Concesionario.App.Wording;
using Concesionario.Wasm.Parts;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LOS NUMEROS QUE LA CARATULA ESCRIBE POR SU CUENTA, que es la otra mitad de lo que la puerta no
	// dice. El dominio compone clave-mas-valores, asi que sus FRASES llegan con sus cifras ya
	// agrupadas por el renderizador del idioma en que se pidieron; la puerta sigue entregando
	// COLUMNAS crudas -- un entero es un entero -- y donde va el separador de miles es del idioma:
	// 172.630 en el registro de esta agencia y 172,630 para quien lee en ingles.
	//
	// ES LA REGLA 8 DE LA GUIA DE LOCALIZACION Y YA MORDIO UNA VEZ: una app de esta misma familia
	// mostraba 172,630 en un lado y 32.910 en el otro. Por eso el separador viaja con el render y
	// ninguna caratula escribe uno propio.
	//
	// EL DEFECTO QUE ESTA ETAPA CIERRA ES EL MISMO QUE CERRO LA DE LOS DIAS: el mismo campo dicho de
	// dos maneras a la vez. AlDia y Programadas se pintaban crudos en el marcador de la portada y
	// AGRUPADOS en las pastillas de tres renglones mas abajo. Ninguna prueba lo veia, y a ojo tampoco
	// se ve: son cuentas sobre las clases que la agencia programo, asi que las dos formas dicen "7"
	// hasta que alguien tenga mil de algo. Que hoy coincidan es la razon por la que el defecto
	// sobrevivio, no una razon para dejarlo.
	//
	// UN NUMERO ES DEL REGISTRO CUANDO LA PUERTA LO ENTREGO, y el censo esta anclado ahi y no en el
	// tipo int: el lado del cuadrito de un dibujo y cuantos archivos movio el puente de
	// almacenamiento tambien son enteros, y ninguno es una cantidad que una persona lea como
	// cantidad. Por eso las dos reglas del censo leen la SUPERFICIE que la puerta llena -- las
	// columnas 'init' de los portadores y los miembros que contestan un numero sin portador -- y no
	// toda declaracion de entero de la caratula.
	//
	// Y POR QUE NO ALCANZA CON NINGUNA PRUEBA DE ACUERDO (E49). Comparar lo que pinta la pantalla
	// contra lo que conteste el renderizador es comparar dos consumidores de UN solo renderizador: se
	// ponen de acuerdo tambien cuando el renderizador esta uniformemente mal. Por eso cada redaccion
	// de abajo se afirma contra un LITERAL, y el barrido mira el markup como TEXTO.
	[TestFixture]
	public sealed class EveryNumberTheScreenPaintsIsGroupedAsTheLanguageWritesIt
	{
		// EL NUMERO CON QUE SE AFIRMA POR VALOR, y no sale de ningun journal: es el de la medicion que
		// la regla 8 nombra, el mismo que una app de esta familia escribio de dos formas.
		private const int TheOdometerOfTheMeasuredPrecedent = 172630;

		// Y UNA CUENTA CHICA, que es donde los dos idiomas coinciden. Sirve para decir por que el
		// marcador de la portada pudo estar mal tanto tiempo sin que se viera.
		private const int TheCountOnTheScoreTile = 7;

		private const int TheDaysThatAreLeft = 3;

		// COMO SE CLASIFICA CADA MENCION DE UN NUMERO EN EL MARKUP. 'CRUDO' va en mayuscula porque es
		// el unico que pone la prueba roja y es lo que se lee primero en el mensaje.
		private const string Said = "dicho por el renderizador";
		private const string Raw = "CRUDO";
		private const string Compared = "comparado, no pintado";
		private const string Named = "nombrado, no pintado";
		private const string Declared = "declarado";
		private const string HandedOn = "pasado a otra seccion";
		private const string Assigned = "asignado desde la puerta";

		// EL IDIOMA CON QUE SE APAGAN LOS LITERALES DE ESTE BARRIDO, dicho como lo clasifica el
		// instrumento compartido: en un .razor un '"' pegado a un '@' abre una expresion y no un
		// literal, y esa regla ya esta medida ahi.
		private const string TheMarkupLanguage = "razor";

		// LAS CINCO LLAMADAS QUE CUENTAN COMO DECIR UN NUMERO. Las tres primeras son las que el
		// markup usa hoy; las dos ultimas son los envoltorios que delegan en el mismo renderizador.
		// Un envoltorio nuevo que no este aqui se lee CRUDO y el barrido se pone rojo, que es el lado
		// seguro: alguien lo mira y lo agrega a proposito.
		private static readonly string[] TheCallsThatSayANumber =
		{
			"Texts.Grouped", "texts.Grouped", "Texts.Days", "HowANumberReads.Grouped",
			"ComoSeLee.Miles",
		};

		// COMO LOS PORTADORES DECLARAN UNA COLUMNA-NUMERO, y como la caratula contesta un numero sin
		// portador. Los dos moldes salen del fuente de las caratulas, no de esta prueba.
		private const string ANumberOrAnAwaitedOne = @"(?:int|long|(?:Value)?Task<(?:int|long)>)";

		private static readonly Regex ACarriedNumber = new Regex(
			@"public\s+(?:int|long)\s+(\w+)\s*\{\s*get;\s*init;\s*\}");

		private static readonly Regex ANumberAnswered = new Regex(
			@"public\s+(?:static\s+|async\s+)*" + ANumberOrAnAwaitedOne + @"\s+(\w+)\s*\(");

		private static readonly Regex AnAssignment = new Regex(@"^\s*(\w+)\s*=(?!=|>)");

		private static readonly Regex AComponentParameter = new Regex("^\\s*([A-Z]\\w*)=\"@");

		private static readonly Regex ADeclaration = new Regex(
			@"^\s*(?:\[[^\]]*\]\s*)*(?:private|public|protected|internal)\s");

		private static readonly Regex ASizeOfACollection = new Regex(
			@"(\w+)\.(?:Count|Length)(?![A-Za-z0-9_])");

		private static readonly Regex AComparisonFollows = new Regex(@"^\s+(?:==|!=|>=|<=|>|<)");

		private static readonly Regex RunsOfWhitespace = new Regex(@"\s+");

		private static Phrases InSpanish => TheCatalogueOfTheScreen.InTheLanguageOfTheRecord;

		private static Phrases InEnglish => TheCatalogueOfTheScreen.InTheOtherLanguage;

		// ===== CUALES NUMEROS ENTREGA LA PUERTA, LEIDO DEL FUENTE DE LAS CARATULAS =========

		// LAS COLUMNAS QUE UN PORTADOR LLENA CON UN NUMERO. Se miran las de 'get; init;' y no toda
		// propiedad entera: lo que la seccion pinta es lo que el portador le entrego armado, y el
		// estado propio de una pantalla -- el lado de un dibujo, el grosor de un trazo -- no es eso.
		[Test]
		public void TheNumberColumnsOfTheCarriersAreTheOnesTheirOwnSourceDeclares()
		{
			Assert.That(TheNumberColumnsOfTheCarriers(), Is.EqualTo(new[]
			{
				"DayCount", "DaysRemaining", "DeliveryReading", "DistanceRemaining", "EntryCount",
				"Odometer", "PriceCountOnRecord", "ReadingCount", "StayCount", "Value",
			}),
				"Las columnas-numero de los portadores cambiaron. Una columna nueva que sea una "
				+ "cantidad la tiene que agrupar la pantalla, porque el separador de miles es del "
				+ "idioma; una que deje de serlo deja de pedirlo. Las dos cosas se miran a mano.");
		}

		// Y LOS NUMEROS QUE LA CARATULA CONTESTA SIN NINGUN PORTADOR, que es por donde se colaron dos
		// de los cuatro sitios crudos de esta etapa: las cuentas de la portada no bajan en ninguna
		// columna, bajan por un miembro de AppState, un campo de la pagina y un parametro.
		//
		// LOS DOS DEL PUENTE DE ALMACENAMIENTO ESTAN A PROPOSITO. Cuentan archivos movidos y no
		// hechos del carro, y hoy no llegan a ninguna pantalla; van en el censo para que el dia que
		// alguno se pinte el barrido lo vea, y no porque sean una cantidad que alguien lea.
		[Test]
		public void TheNumbersTheCaratulaAnswersWithNoCarrierAreTheOnesItsOwnSourceDeclares()
		{
			Assert.That(TheNumbersTheCaratulaAnswers(), Is.EqualTo(new[]
			{
				"CountDue", "CountOverdue", "CountScheduled", "CountUpToDate",
				"CountUpToDatePerHundred", "DeferralsUsed",
				"FlushFromMemfsAsync", "JournalEntries", "LastEventApplied", "MissedCount",
				"PreloadIntoMemfsAsync",
			}),
				"Los numeros que la caratula contesta sin portador cambiaron. Uno nuevo puede llegar "
				+ "a la pantalla sin pasar por ninguna columna, que es como dos cuentas de la portada "
				+ "se quedaron crudas hasta esta etapa.");

			// Y EL CONTROL: un miembro que contesta TEXTO no es un numero. Sin este renglon, un molde
			// que se tragara cualquier miembro dejaria el censo enorme y el barrido lleno de ruido.
			foreach (string said in new[] { "ReadVin", "ReadDueState", "NextDueKind", "EvaluatedUpTo" })
				Assert.That(TheNumbersTheCaratulaAnswers(), Does.Not.Contain(said),
					"'" + said + "' contesta texto y quedo contado como numero.");
		}

		// ===== NINGUNA SECCION PINTA UN NUMERO CRUDO ======================================

		// EL BARRIDO, Y EL CENSO ENTERO POR VALOR AL LADO. No lleva numero de linea a proposito: una
		// linea se mueve sola cuando alguien reindenta, y un sitio no. Lo que queda fijo es el
		// archivo, el nombre y que se hizo con el.
		//
		// LO COMPARADO, LO NOMBRADO, LO DECLARADO Y LO ASIGNADO NO ENTRAN EN LA LISTA: un '> 0' que
		// decide una rama no es un numero en pantalla, y enumerarlos dejaria una lista de cuarenta
		// renglones que cambia con cada guarda nueva. Se afirma lo que se MUEVE hacia la pantalla.
		//
		// SE SIGUEN LOS SALTOS DE NOMBRE SOBRE LA CARATULA ENTERA, porque las dos puntas del salto
		// viven en archivos distintos: la pagina lee CountUpToDate en un campo llamado upToDate y se
		// lo pasa a la portada llamandose AlDia, que es donde se pintaba crudo. Seguido archivo por
		// archivo, el barrido mira la punta que no pinta nada.
		[Test]
		public void NoRazorOfTheCaratulaPaintsANumberWithoutSayingItThroughTheRenderer()
		{
			List<string> sites = TheNumberSitesInTheMarkup();

			Assert.That(TheRawOnesIn(sites), Is.Empty,
				"La caratula imprime un numero tal como la puerta lo entrega:\n  "
				+ string.Join("\n  ", TheRawOnesIn(sites))
				+ "\nEso deja la mitad de la pantalla con el separador del idioma de la persona y la "
				+ "otra mitad sin ninguno, y el MISMO campo dicho de dos formas donde dos sitios lo "
				+ "pintan.");

			Assert.That(TheOnesThatMoveIn(sites), Is.EquivalentTo(new[]
			{
				"Pages/MyCar.razor overdue " + HandedOn,
				"Pages/MyCar.razor due " + HandedOn,
				"Pages/MyCar.razor missed " + HandedOn,
				"Pages/MyCar.razor upToDate " + HandedOn,
				"Pages/MyCar.razor scheduled " + HandedOn,
				"Pages/MyCar.razor upToDatePerHundred " + HandedOn,
				"Pages/MyCar.razor Odometer " + Said,
				"Pages/MyCar.razor Odometer " + Said,
				"Sections/Historial.razor DayCount " + Said,
				"Sections/Historial.razor ReadingCount " + Said,
				"Sections/Historial.razor Value " + Said,
				"Sections/Historial.razor EntryCount " + Said,
				"Sections/Inicio.razor AlDia " + Said,
				"Sections/Inicio.razor Programadas " + Said,
				"Sections/Inicio.razor Overdue " + Said,
				"Sections/Inicio.razor Due " + Said,
				"Sections/Inicio.razor Missed " + Said,
				"Sections/Inicio.razor AlDia " + Said,
				"Sections/Inicio.razor Programadas " + Said,
				"Sections/Servicios.razor DistanceRemaining " + Said,
				"Sections/Servicios.razor DaysRemaining " + Said,
				"Sections/Vehiculo.razor DeliveryReading " + Said,
				"Sections/Vehiculo.razor PriceCountOnRecord " + Said,
				"Sections/Warranty.razor DeliveryReading " + Said,
				"Sections/Warranty.razor Odometer " + Said,
			}),
				"Los sitios en que la caratula mueve un numero hacia la pantalla cambiaron:\n  "
				+ string.Join("\n  ", TheOnesThatMoveIn(sites))
				+ "\nUn sitio nuevo se mira a mano: que se diga no alcanza, hay que elegir la forma -- "
				+ "agrupado para una cantidad, entero para una cuenta cuyo sustantivo concuerda.");

			// Y DOS CUENTAS LLEGAN A LA PORTADA DESDE LA PAGINA, que es de donde salio el defecto de
			// esta etapa. Sin este renglon, que la portada las diga se puede cumplir con la pagina
			// muerta, y el salto de nombre -- que es lo que escondio el defecto -- deja de estar
			// cubierto.
			foreach (string count in new[] { "AlDia", "Programadas" })
				Assert.That(TheFilesThatTouch(count), Is.EquivalentTo(new[]
				{
					"Pages/MyCar.razor", "Sections/Inicio.razor",
				}),
					"'" + count + "' dejo de cruzar de la pagina a la portada, que es el salto de "
					+ "nombre por el que este barrido tiene que mirar la caratula entera.");
		}

		// EL TAMANO DE UNA COLECCION TAMBIEN ES UN NUMERO, y no lo encuentra ninguna regla sobre
		// columnas: 'CoveredSystems' es un arreglo de texto y lo que se pinta es su largo. Uno de los
		// cuatro sitios crudos de esta etapa era exactamente eso.
		//
		// AQUI SE AFIRMA LO QUE NO ES UNA COMPARACION, y esa es la mayoria: en esta caratula un
		// '.Length' suele ser una guarda de presencia sobre un texto. Enumerarlas todas seria una
		// lista que cambia con cada guarda nueva; el control de abajo prueba que 'comparado' no se
		// traga un pintado.
		[Test]
		public void NoRazorOfTheCaratulaPaintsTheSizeOfACollectionWithoutSayingItThroughTheRenderer()
		{
			List<string> sites = TheSizeSitesInTheMarkup();

			Assert.That(TheRawOnesIn(sites), Is.Empty,
				"La caratula imprime el tamano de una coleccion sin agruparlo:\n  "
				+ string.Join("\n  ", TheRawOnesIn(sites)));

			Assert.That(TheOnesThatMoveIn(sites), Is.EquivalentTo(new[]
			{
				"Sections/Cobertura.razor CoveredSystems " + Said,
				"Sections/Historial.razor Operations " + Said,
				"Sections/MyVehicle.razor unpublishedFluids " + Said,
				"Sections/MyVehicle.razor Fluids " + Said,
				"Sections/MyVehicle.razor unpublishedTyres " + Said,
				"Sections/MyVehicle.razor Tyres " + Said,
				"Sections/Servicios.razor Nueve " + Said,
			}),
				"Los tamanos de coleccion que la caratula pinta cambiaron:\n  "
				+ string.Join("\n  ", TheOnesThatMoveIn(sites)));

			// Y LAS COMPARACIONES SON MUCHAS Y EXISTEN. Sin este renglon, un detector que devolviera
			// la lista vacia dejaria las dos afirmaciones de arriba verdes por no haber mirado.
			Assert.That(sites, Has.Count.GreaterThan(TheOnesThatMoveIn(sites).Count + 20),
				"El detector de tamanos dejo de ver las guardas de presencia, que son la mayoria de "
				+ "los '.Length' de esta caratula. Un detector que ve poco es un barrido verde.");
		}

		// ===== EL CONTROL DEL DETECTOR ====================================================

		// LOS DOS RENGLONES QUE OBLIGAN A CONTAR PARENTESIS Y A MIRAR POSICION, y son el tercero y el
		// quinto. Preguntar si la LINEA nombra al renderizador da por dicho al numero crudo que va al
		// lado del dicho; preguntar si la LINEA tiene una comparacion da por comparado al numero
		// crudo que va al lado del comparado. Las dos son exactamente la forma en que este defecto se
		// escribe sin querer.
		[Test]
		public void TheSweepSeesANumberPaintedRawBesideOneSaidAndBesideOneMerelyCompared()
		{
			Assert.That(
				TheNumberSitesIn("control.razor", "<span>@Costs.PriceCountOnRecord</span>"),
				Is.EquivalentTo(new[] { "control.razor PriceCountOnRecord " + Raw }),
				"El detector no ve un numero pintado crudo, asi que el barrido esta verde por no "
				+ "haber detectado.");

			Assert.That(
				TheNumberSitesIn(
					"control.razor", "<span>@Texts.Grouped(Costs.PriceCountOnRecord)</span>"),
				Is.EquivalentTo(new[] { "control.razor PriceCountOnRecord " + Said }),
				"El detector da por crudo un numero que SI pasa por el renderizador.");

			Assert.That(
				TheNumberSitesIn(
					"control.razor",
					"<span>@Texts.Grouped(Stands.Odometer) @Costs.PriceCountOnRecord</span>"),
				Is.EquivalentTo(new[]
				{
					"control.razor Odometer " + Said, "control.razor PriceCountOnRecord " + Raw,
				}),
				"Dos numeros en una linea, uno dicho y el otro crudo, y el detector los da por "
				+ "iguales.");

			Assert.That(
				TheNumberSitesIn("control.razor", "@if (Costs.PriceCountOnRecord > 0)"),
				Is.EquivalentTo(new[] { "control.razor PriceCountOnRecord " + Compared }),
				"El detector cuenta como pintado un numero que solo decide una rama.");

			Assert.That(
				TheNumberSitesIn(
					"control.razor",
					"@if (Stands.Odometer > 0) { <span>@Costs.PriceCountOnRecord</span> }"),
				Is.EquivalentTo(new[]
				{
					"control.razor Odometer " + Compared,
					"control.razor PriceCountOnRecord " + Raw,
				}),
				"Una comparacion en la linea da por comparado al numero crudo que va al lado, que es "
				+ "el otro camino por el que este defecto entra sin que nada se ponga rojo.");

			// Y UN NUMERO PINTADO DIRECTO DE LA CARATULA, SIN NINGUN PORTADOR. Es la mitad del censo
			// que no lee columnas, y sin este renglon nada prueba que se barra.
			Assert.That(
				TheNumberSitesIn("control.razor", "<span>@state.CountOverdue()</span>"),
				Is.EquivalentTo(new[] { "control.razor CountOverdue " + Raw }),
				"El detector no ve un numero pintado directo del estado de la caratula.");

			// Y EL NOMBRE A LA IZQUIERDA DE UN '=' NO ES UNA CANTIDAD, que es como un atributo que se
			// llama igual que una columna -- CascadingValue tiene un parametro Value -- deja de
			// leerse como un numero pintado crudo. El de la derecha si se ve.
			Assert.That(
				TheNumberSitesIn("control.razor", "<CascadingValue Value=\"texts\">",
					new[] { "Value" }),
				Is.EquivalentTo(new[] { "control.razor Value " + Named }),
				"El nombre de un atributo se leyo como un numero pintado crudo.");

			Assert.That(
				TheNumberSitesIn("control.razor", "        Odometer=\"@Odometer\"",
					new[] { "Odometer" }),
				Is.EquivalentTo(new[]
				{
					"control.razor Odometer " + Named, "control.razor Odometer " + HandedOn,
				}),
				"El numero que un atributo RECIBE dejo de verse, y es el unico de los dos lados que "
				+ "cruza sin decirse.");
		}

		// EL CONTROL DEL TAMANO, con su punto ciego declarado: '.Count' pegado a otra palabra no es
		// un tamano, y es un caso vivo -- CountdownRefused vive en el mismo markup.
		[Test]
		public void TheSweepSeesASizePaintedRawAndDoesNotReadCountdownAsACount()
		{
			Assert.That(
				TheSizeSitesIn(
					"control.razor",
					"<span>@Sheet.Coverage.CoveredSystems.Length</span>"),
				Is.EquivalentTo(new[] { "control.razor CoveredSystems " + Raw }));

			Assert.That(
				TheSizeSitesIn(
					"control.razor",
					"<span>@Texts.Grouped(Sheet.Coverage.CoveredSystems.Length)</span>"),
				Is.EquivalentTo(new[] { "control.razor CoveredSystems " + Said }));

			Assert.That(
				TheSizeSitesIn("control.razor", "@if (Sheet.Coverage.Exclusions.Length > 0)"),
				Is.EquivalentTo(new[] { "control.razor Exclusions " + Compared }));

			Assert.That(
				TheSizeSitesIn("control.razor", "if (parada.CountdownRefused)"), Is.Empty,
				"El detector lee 'Countdown' como si fuera un conteo.");
		}

		// UN NOMBRE ADENTRO DE UN TEXTO NO ES UN NUMERO, y esta es la mitad que el apagado de
		// literales compra. Servicios.razor mapea "Missed" a su clave y la portada pide
		// "home.missed" del catalogo: los dos llevan adentro el nombre con que una cuenta cruza a la
		// portada, y sin apagar el texto cada uno se lee como un numero pintado crudo. Los dos
		// renglones de abajo estan medidos: asi se puso rojo este barrido la primera vez que corrio.
		[Test]
		public void TheSweepDoesNotReadANameWrittenInsideATextAsANumber()
		{
			Assert.That(
				TheNumberSitesIn(
					"control.razor", "\"Missed\" => \"services.summaryMissed\",", new[] { "Missed" }),
				Is.Empty,
				"El nombre de un miembro de un conjunto cerrado, escrito como texto, se leyo como un "
				+ "numero pintado crudo.");

			Assert.That(
				TheNumberSitesIn(
					"control.razor",
					"@Texts.With(\"home.missed\", (\"count\", Texts.Grouped(Missed)))",
					new[] { "missed", "Missed" }),
				Is.EquivalentTo(new[] { "control.razor Missed " + Said }),
				"La clave del catalogo lleva el nombre de la cuenta adentro, y se leyo como un "
				+ "numero pintado crudo al lado del que si se dice.");

			// Y EL CONTROL DEL CONTROL: el mismo nombre FUERA de un texto si se ve.
			Assert.That(
				TheNumberSitesIn("control.razor", "<span>@Missed</span>", new[] { "Missed" }),
				Is.EquivalentTo(new[] { "control.razor Missed " + Raw }));
		}

		// ===== LO QUE LA PERSONA LEE, POR VALOR Y EN LOS DOS IDIOMAS ======================

		// LOS TRES ENVOLTORIOS, CONTRA SUS LITERALES. Ninguno se compara contra otro: los tres
		// delegan en el mismo renderizador, asi que coinciden tambien cuando ese esta mal (E49).
		[Test]
		public void TheNumbersTheScreenPaintsAreGroupedAsEachLanguageWritesThem()
		{
			Assert.That(InSpanish.Grouped(TheOdometerOfTheMeasuredPrecedent), Is.EqualTo("172.630"));

			Assert.That(InEnglish.Grouped(TheOdometerOfTheMeasuredPrecedent), Is.EqualTo("172,630"));

			Assert.That(
				HowANumberReads.Grouped(InSpanish, TheOdometerOfTheMeasuredPrecedent),
				Is.EqualTo("172.630"));

			Assert.That(
				HowANumberReads.Grouped(InEnglish, TheOdometerOfTheMeasuredPrecedent),
				Is.EqualTo("172,630"));

			Assert.That(
				ComoSeLee.Miles(InSpanish, TheOdometerOfTheMeasuredPrecedent),
				Is.EqualTo("172.630"));

			Assert.That(
				ComoSeLee.Miles(InEnglish, TheOdometerOfTheMeasuredPrecedent),
				Is.EqualTo("172,630"));

			// Y LA FRASE DEL DOMINIO DICE EL MISMO NUMERO IGUAL, afirmada contra el mismo literal y no
			// contra las de arriba: una pastilla que dijera 172.630 encima de una frase que dice
			// 172,630 se lee como dos numeros.
			Assert.That(
				TheSentenceTheDoorWouldCompose.ANumberAsTheSentenceSaysIt(
					TheOdometerOfTheMeasuredPrecedent, TheLanguage.OfTheRecord),
				Is.EqualTo("172.630"));

			Assert.That(
				TheSentenceTheDoorWouldCompose.ANumberAsTheSentenceSaysIt(
					TheOdometerOfTheMeasuredPrecedent, TheLanguage.OfAnEnglishReader),
				Is.EqualTo("172,630"));

			// Y NINGUNA ES EL NUMERO COMO LA PUERTA LO ENTREGA. Sin este renglon, un renderizador que
			// devolviera su argumento intacto pasaria todo lo de arriba el dia que alguien escribiera
			// mal los literales.
			foreach (string said in new[]
			{
				InSpanish.Grouped(TheOdometerOfTheMeasuredPrecedent),
				InEnglish.Grouped(TheOdometerOfTheMeasuredPrecedent),
			})
				Assert.That(said, Is.Not.EqualTo("172630"),
					"'" + said + "' salio como la puerta lo entrega, asi que el numero no se dijo: se "
					+ "copio.");
		}

		// POR QUE EL MARCADOR DE LA PORTADA PUDO ESTAR MAL TANTO TIEMPO: bajo mil los dos idiomas
		// escriben el mismo numero, asi que el sitio crudo y el dicho decian lo mismo y la pantalla
		// se veia bien. Es la medida de cuanto vale mirar el fuente en vez de la pantalla.
		[Test]
		public void UnderAThousandTheTwoLanguagesWriteTheSameNumberAndThatIsWhyTheTileLookedRight()
		{
			Assert.That(
				InSpanish.Grouped(TheCountOnTheScoreTile),
				Is.EqualTo(InEnglish.Grouped(TheCountOnTheScoreTile)));

			Assert.That(InSpanish.Grouped(TheCountOnTheScoreTile), Is.EqualTo("7"));

			// Y ARRIBA DE MIL DEJAN DE COINCIDIR, que es lo que hace que la eleccion importe.
			Assert.That(
				InSpanish.Grouped(TheOdometerOfTheMeasuredPrecedent),
				Is.Not.EqualTo(InEnglish.Grouped(TheOdometerOfTheMeasuredPrecedent)));
		}

		// UNA CUENTA NO SIEMPRE ES UN NUMERO, y la pastilla de la cuenta atras es el sitio donde no
		// lo es: los dias van dichos ENTEROS porque "1 dias" no aparece en ningun idioma y el
		// singular es del idioma, no del digito. La pastilla de al lado, la de distancia, si es un
		// numero y va agrupada. Se afirma que las dos formas DIFIEREN para que unificarlas se vea.
		[Test]
		public void TheDaysThatAreLeftAreSaidWholeAndTheDistanceBesideThemIsSaidAsANumber()
		{
			Assert.That(InSpanish.Days(1), Is.EqualTo("1 día"));

			Assert.That(InSpanish.Days(TheDaysThatAreLeft), Is.EqualTo("3 días"));

			Assert.That(InEnglish.Days(1), Is.EqualTo("1 day"));

			Assert.That(InEnglish.Days(TheDaysThatAreLeft), Is.EqualTo("3 days"));

			Assert.That(InSpanish.Grouped(TheDaysThatAreLeft), Is.EqualTo("3"),
				"La forma entera y el numero pelado dejaron de diferir, asi que elegir entre ellas no "
				+ "dice nada y la pastilla de los dias eligio por nada.");

			// Y EL MARKUP SIGUE ELIGIENDO UNA FORMA POR PASTILLA, en el mismo renglon. Sin este par,
			// lo de arriba es cierto sin que nadie lo use y unificar las dos formas no pondria nada
			// rojo.
			Assert.That(
				TheMarkupOf("Sections/Servicios.razor"),
				Does.Contain("Texts.Days(parada.DaysRemaining)"));

			Assert.That(
				TheMarkupOf("Sections/Servicios.razor"),
				Does.Contain("Texts.Grouped(parada.DistanceRemaining)"));
		}

		// NINGUN ARCHIVO DE LA CARATULA ESCRIBE UN SEPARADOR DE MILES PROPIO. Es la frase rival en
		// pequeno: un segundo sitio que agrupe es un segundo sitio que puede quedar en el idioma
		// equivocado, y esta es la unica forma de ver que los tres envoltorios DELEGAN en vez de
		// formatear -- ninguna afirmacion por valor distingue delegar de reimplementar bien.
		[Test]
		public void NoFileOfTheCaratulaWritesAThousandsSeparatorOfItsOwn()
		{
			var wrote = new List<string>();

			foreach (string file in EveryCarrierOfTheShell())
				foreach (string shape in TheShapesThatWriteASeparator)
					if (File.ReadAllText(file).Contains(shape, StringComparison.Ordinal))
						wrote.Add(Path.GetFileName(file) + " escribe " + shape);

			Assert.That(wrote, Is.Empty,
				"La caratula escribe un separador de miles por su cuenta:\n  "
				+ string.Join("\n  ", wrote)
				+ "\nDonde va el separador es del idioma y lo pone el renderizador; un segundo sitio "
				+ "que lo escriba es un segundo sitio que puede quedar en el idioma equivocado.");

			// Y EL CONTROL: el renderizador SI lo escribe, y es el unico. Sin este renglon el barrido
			// de arriba esta verde el dia que alguien cambie los moldes por tres cadenas que no
			// aparecen en ningun lado.
			foreach (string shape in TheShapesThatWriteASeparator)
				Assert.That(TheSourceOfTheRenderer(), Does.Contain(shape),
					"El renderizador dejo de escribir '" + shape + "', asi que el barrido de arriba "
					+ "no esta buscando la forma que hoy se usa.");
		}

		private static readonly string[] TheShapesThatWriteASeparator =
		{
			"#,##0", "NumberGroupSeparator", "NumberFormatInfo",
		};

		// ===== el instrumento: cuales numeros hay, y donde los toca el markup ============

		private static List<string> TheNumberColumnsOfTheCarriers()
		{
			return TheNamesMatching(ACarriedNumber);
		}

		private static List<string> TheNumbersTheCaratulaAnswers()
		{
			return TheNamesMatching(ANumberAnswered);
		}

		// Los nombres que ese molde encuentra en el fuente de la caratula. Los dos moldes se leen con
		// la misma funcion para que el segundo censo no sea una copia del primero.
		private static List<string> TheNamesMatching(Regex shape)
		{
			var found = new List<string>();

			foreach (string file in EveryCarrierOfTheShell())
				foreach (Match each in shape.Matches(
					RunsOfWhitespace.Replace(File.ReadAllText(file), " ")))
					if (!found.Contains(each.Groups[1].Value)) found.Add(each.Groups[1].Value);

			found.Sort(StringComparer.Ordinal);

			return found;
		}

		private static List<string> TheNumberSitesInTheMarkup()
		{
			List<string> names = TheNamesANumberIsKnownByAcrossTheWholeCaratula();
			List<string> calls = TheNumbersTheCaratulaAnswers();
			var sites = new List<string>();

			foreach (string file in EveryMarkupFileTheShellHas())
				sites.AddRange(
					TheNumberSitesIn(InsideTheShell(file), File.ReadAllText(file), names, calls));

			return sites;
		}

		private static List<string> TheSizeSitesInTheMarkup()
		{
			var sites = new List<string>();

			foreach (string file in EveryMarkupFileTheShellHas())
				sites.AddRange(TheSizeSitesIn(InsideTheShell(file), File.ReadAllText(file)));

			return sites;
		}

		// El barrido sobre UNA linea de control, con los nombres del censo. El control le pasa los
		// suyos cuando quiere probar un nombre que el censo no lleva.
		private static List<string> TheNumberSitesIn(string file, string markup)
		{
			return TheNumberSitesIn(
				file, markup, TheNumberColumnsOfTheCarriers(), TheNumbersTheCaratulaAnswers());
		}

		private static List<string> TheNumberSitesIn(string file, string markup, string[] names)
		{
			return TheNumberSitesIn(file, markup, new List<string>(names), new List<string>());
		}

		private static List<string> TheNumberSitesIn(
			string file, string markup, List<string> names, List<string> calls)
		{
			var sites = new List<string>();

			foreach (string line in TheLinesAsTheCompilerReadsThem(markup))
			{
				foreach (string name in names)
					foreach (int at in WhereItIsNamedIn(line, name, false))
						sites.Add(file + " " + name + " " + HowItIsTreated(line, at, at + name.Length));

				foreach (string name in calls)
					foreach (int at in WhereItIsNamedIn(line, name, true))
						sites.Add(file + " " + name + " " + HowItIsTreated(line, at, at + name.Length));
			}

			return sites;
		}

		private static List<string> TheSizeSitesIn(string file, string markup)
		{
			var sites = new List<string>();

			foreach (string line in TheLinesAsTheCompilerReadsThem(markup))
				foreach (Match each in ASizeOfACollection.Matches(line))
					sites.Add(
						file + " " + each.Groups[1].Value + " "
						+ HowItIsTreated(line, each.Index, each.Index + each.Length));

			return sites;
		}

		// EL TEXTO DE UN LITERAL SE APAGA ANTES DE BUSCAR, con el mismo instrumento que ya lo hace
		// para el barrido de aritmetica y no con un molde escrito aqui: un molde propio se equivoca
		// con una comilla escapada, y eso ya se pago una vez. Cada caracter se queda en su posicion,
		// que es lo que deja clasificar contando parentesis.
		//
		// SIN ESTO, TRES NOMBRES DEL CENSO SE LEEN COMO NUMEROS PINTADOS: la clave "home.missed"
		// lleva 'missed' adentro, "home.upToDate" lleva 'upToDate', y CascadingValue tiene un
		// parametro llamado Value, que es tambien la columna de la lectura del odometro.
		private static string[] TheLinesAsTheCompilerReadsThem(string markup)
		{
			return TheCodeOfTheShellsAsData
				.OnlyWhatTheCompilerReads(markup, TheMarkupLanguage)
				.Replace("\r\n", "\n")
				.Split('\n');
		}

		// LOS SALTOS DE NOMBRE QUE UN NUMERO DA EN EL MARKUP, hasta que no aparezca ninguno nuevo:
		// aqui son DOS seguidos -- CountUpToDate se asigna a un campo llamado upToDate y ese campo se
		// pasa como AlDia -- asi que una sola pasada mira el lugar equivocado.
		//
		// UN SALTO SOLO SE SIGUE CUANDO EL NUMERO CRUZA SIN DECIR. El odometro se dice en la pagina
		// misma y cruza a la seccion ya hecho texto; seguirlo mas alla haria que la seccion que lo
		// pinta se leyera como si pintara un numero crudo.
		private static List<string> TheNamesANumberIsKnownByAcrossTheWholeCaratula()
		{
			var names = new List<string>(TheNumberColumnsOfTheCarriers());
			List<string> calls = TheNumbersTheCaratulaAnswers();
			bool grew = true;

			while (grew)
			{
				grew = false;

				foreach (string file in EveryMarkupFileTheShellHas())
					foreach (string each
						in TheNamesANumberTravelsUnder(File.ReadAllText(file), names, calls))
						if (!names.Contains(each)) { names.Add(each); grew = true; }
			}

			return names;
		}

		private static List<string> TheNamesANumberTravelsUnder(
			string markup, List<string> names, List<string> calls)
		{
			var found = new List<string>();

			foreach (string line in TheLinesAsTheCompilerReadsThem(markup))
			{
				if (!ANumberCrossesUnsaidOn(line, names, calls)) continue;

				Match handed = AComponentParameter.Match(line);

				if (handed.Success && !found.Contains(handed.Groups[1].Value))
					found.Add(handed.Groups[1].Value);

				Match assigned = AnAssignment.Match(line);

				if (assigned.Success && !found.Contains(assigned.Groups[1].Value))
					found.Add(assigned.Groups[1].Value);
			}

			return found;
		}

		private static bool ANumberCrossesUnsaidOn(
			string line, List<string> names, List<string> calls)
		{
			foreach (string name in names)
				foreach (int at in WhereItIsNamedIn(line, name, false))
					if (!IsSaidAt(line, at)) return true;

			foreach (string name in calls)
				foreach (int at in WhereItIsNamedIn(line, name, true))
					if (!IsSaidAt(line, at)) return true;

			return false;
		}

		// DONDE ESA LINEA -- YA CON SUS LITERALES APAGADOS -- NOMBRA ESE NUMERO. Un identificador en
		// un .razor no admite un guion a ningun lado: la mitad del archivo es markup y ahi un guion
		// es parte de un nombre. Y un nombre seguido de '(' es una LLAMADA: las columnas piden que no
		// lo sea y los miembros que contestan un numero piden que lo sea, que es lo que separa una
		// columna de una pregunta.
		private static List<int> WhereItIsNamedIn(string line, string name, bool mustBeACall)
		{
			var found = new List<int>();
			int at = line.IndexOf(name, StringComparison.Ordinal);

			while (at >= 0)
			{
				int after = at + name.Length;
				bool call = after < line.Length && line[after] == '(';

				if (!(at > 0 && IsPartOfAName(line[at - 1]))
					&& !(after < line.Length && IsPartOfAName(line[after]))
					&& call == mustBeACall)
					found.Add(at);

				at = line.IndexOf(name, at + 1, StringComparison.Ordinal);
			}

			return found;
		}

		private static bool IsPartOfAName(char letter)
		{
			return char.IsLetterOrDigit(letter) || letter == '_' || letter == '-';
		}

		// UN NOMBRE PEGADO A UN '=' ES UN NOMBRE Y NO UNA CANTIDAD: es el atributo que RECIBE o la
		// variable que se ASIGNA, y el numero -- si lo hay -- esta del otro lado. El numero de la
		// derecha se barre por su cuenta, que es como AlDia="@upToDate" queda cubierto por upToDate.
		// El '==' no cuenta, que ya es una comparacion.
		private static bool IsTheNameOnTheLeftOfAnAssignment(string line, int after)
		{
			return after < line.Length
				&& line[after] == '='
				&& (after + 1 >= line.Length || line[after + 1] != '=');
		}

		// EL ORDEN IMPORTA Y LAS TRES PRIMERAS SON POR POSICION. Una linea que diga un numero y pinte
		// otro crudo, y una que compare uno y pinte otro crudo, son las dos formas en que este
		// defecto se escribe; preguntarle a la LINEA en vez de a la POSICION las da por limpias.
		private static string HowItIsTreated(string line, int at, int after)
		{
			if (IsSaidAt(line, at)) return Said;

			if (AComparisonFollows.IsMatch(line.Substring(after))) return Compared;

			if (IsTheNameOnTheLeftOfAnAssignment(line, after)) return Named;

			if (ADeclaration.IsMatch(line)) return Declared;

			if (AComponentParameter.IsMatch(line)) return HandedOn;

			if (AnAssignment.IsMatch(line)) return Assigned;

			return Raw;
		}

		// SI ESA MENCION ESTA ADENTRO DE UNA LLAMADA AL RENDERIZADOR, contando parentesis desde el
		// principio de la linea.
		//
		// SE MIRA UNA LINEA A LA VEZ, y de ahi salen los dos puntos ciegos, los dos del lado seguro.
		// Una llamada al renderizador PARTIDA en dos lineas deja el numero en una linea sin
		// renderizador y se lee como crudo; y una comparacion escrita sin espacio antes del operador
		// se lee como crudo tambien. Los dos ponen el barrido rojo y alguien mira, que es por que el
		// markup deja cada llamada entera en su linea.
		private static bool IsSaidAt(string line, int at)
		{
			int depth = 0;
			int sayingFrom = -1;

			for (int i = 0; i < at; i++)
			{
				if (line[i] == '(')
				{
					depth++;

					if (sayingFrom < 0 && OpensTheRenderer(line, i)) sayingFrom = depth;

					continue;
				}

				if (line[i] != ')') continue;

				depth--;

				if (sayingFrom > depth) sayingFrom = -1;
			}

			return sayingFrom > 0;
		}

		private static bool OpensTheRenderer(string line, int paren)
		{
			foreach (string call in TheCallsThatSayANumber)
				if (paren >= call.Length
					&& string.CompareOrdinal(line, paren - call.Length, call, 0, call.Length) == 0)
					return true;

			return false;
		}

		private static List<string> TheRawOnesIn(List<string> sites)
		{
			var raw = new List<string>();

			foreach (string each in sites)
				if (each.EndsWith(Raw, StringComparison.Ordinal)) raw.Add(each);

			return raw;
		}

		private static List<string> TheOnesThatMoveIn(List<string> sites)
		{
			var moving = new List<string>();

			foreach (string each in sites)
				if (!each.EndsWith(Compared, StringComparison.Ordinal)
					&& !each.EndsWith(Named, StringComparison.Ordinal)
					&& !each.EndsWith(Declared, StringComparison.Ordinal)
					&& !each.EndsWith(Assigned, StringComparison.Ordinal))
					moving.Add(each);

			return moving;
		}

		private static List<string> TheFilesThatTouch(string name)
		{
			var found = new List<string>();

			foreach (string site in TheNumberSitesInTheMarkup())
			{
				string[] said = site.Split(' ');

				if (said[1] == name && !found.Contains(said[0])) found.Add(said[0]);
			}

			return found;
		}

		// ===== lo que hace falta para las de arriba =======================================

		// SIN NINGUN catch y con la carpeta afirmada. Un barrido que leyo cero archivos esta verde por
		// no haber mirado.
		private static string[] EveryMarkupFileTheShellHas()
		{
			string folder = Path.Combine(AppContext.BaseDirectory, "MarcadoComoDato");

			Assert.That(Directory.Exists(folder), Is.True,
				"El markup de la caratula no llego al output, asi que este barrido no afirma nada.");

			string[] files = Directory.GetFiles(folder, "*.razor", SearchOption.AllDirectories);

			Assert.That(files, Is.Not.Empty, "No llego ningun .razor al output.");

			Array.Sort(files, StringComparer.Ordinal);

			return files;
		}

		private static string[] EveryCarrierOfTheShell()
		{
			string[] found = TheCodeOfTheShellsAsData.EveryCarrierFile();

			Assert.That(found, Is.Not.Empty,
				"No llego ningun .cs de la caratula al output, asi que el censo de numeros esta vacio "
				+ "por no haber leido.");

			return found;
		}

		// EL FUENTE DEL RENDERIZADOR, que vive en la App y no en la caratula: es el unico sitio del
		// modulo que decide donde va el separador de miles.
		private static string TheSourceOfTheRenderer()
		{
			foreach (string file in TheCodeOfTheShellsAsData.EveryFileOf(TheCodeOfTheShellsAsData.App))
				if (file.EndsWith("Phrases.cs", StringComparison.Ordinal))
					return File.ReadAllText(file);

			Assert.Fail("El fuente del renderizador no llego al output.");

			return "";
		}

		private static string InsideTheShell(string file)
		{
			string folder = Path.Combine(AppContext.BaseDirectory, "MarcadoComoDato");

			return file.Substring(folder.Length + 1).Replace('\\', '/');
		}

		private static string TheMarkupOf(string inside)
		{
			string path = Path.Combine(
				AppContext.BaseDirectory, "MarcadoComoDato",
				inside.Replace('/', Path.DirectorySeparatorChar));

			Assert.That(File.Exists(path), Is.True, "'" + inside + "' no llego al output.");

			return File.ReadAllText(path);
		}
	}
}
