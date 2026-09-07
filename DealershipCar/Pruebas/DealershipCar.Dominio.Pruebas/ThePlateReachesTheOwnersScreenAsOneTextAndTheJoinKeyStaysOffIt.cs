using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using DealershipCar.App;
using DealershipCar.Wasm;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// EL BLOQUE DE LA PLACA EN LA PANTALLA DEL DUENO, QUE ES LA OTRA MITAD DE E48 Y APUNTA AL LADO
	// CONTRARIO QUE LA TOOL. LP-4 le manda al asistente LOS DOS TEXTOS como campos con nombre, y tiene
	// razon: un modelo lee campos sueltos y no puede saber, de uno solo, cual pregunta contesto. Una
	// PERSONA lee UNA COLUMNA, y la misma placa escrita de dos formas en la misma columna se lee como
	// un defecto. Las dos superficies no son el mismo contenido y ninguna se copia sobre la otra.
	//
	// LO QUE LA PANTALLA MUESTRA ES EL TEXTO PUBLICADO Y NO LA LLAVE. La llave es la mitad que se
	// COMPARA contra otro registro y no la que se le DICE a nadie; ponerla en la pantalla "por
	// completitud" es imprimir el mismo hecho dos veces.
	//
	// Y LO QUE SE PINTA ES LA FRASE DEL DOMINIO, no el texto suelto, y la razon esta MEDIDA aca abajo y
	// no supuesta: la frase CONTIENE el texto publicado letra por letra, asi que pintar los dos dice la
	// misma placa dos veces. El texto publicado se queda en la superficie del portador -- para quien lo
	// necesite sin volver a partir la frase -- y la pagina dibuja la que dice mas. Lo que hace para una
	// persona lo que un nombre de campo hace para un modelo es la ETIQUETA del renglon.
	//
	// LOS DOS INSTRUMENTOS QUE ESTA REBANADA YA TENIA Y QUE ESTE ARCHIVO NO DUPLICA:
	//
	//   NoShellDoesStringSurgeryOnAPlate barre las OCHO raices por una linea que nombre una placa Y
	//   parta un texto. Ninguna afirmacion de aca lo repite;
	//
	//   TheStandingReachesTheOwnersScreenAndNoControlAsksForIt barre TODO el markup por FORMAS que
	//   recogen un valor de una persona. Una forma no depende del hecho que recoja, asi que ya cubre la
	//   placa: APlateIsReceivedAndNeverAsked afirma que ese censo SIGUE AHI en vez de escribir uno
	//   paralelo, y este archivo hace lo mismo -- no escribe ninguno.
	//
	// Y LO QUE SE AFIRMA POR VALOR ES POR QUE E49. Comparar cada campo del portador contra la lectura
	// de la puerta que ese mismo campo llama es comparar DOS CONSUMIDORES DE UN SOLO RESOLVEDOR: se
	// ponen de acuerdo por construccion, tambien cuando el resolvedor esta uniformemente mal. LP-4 lo
	// midio en su propia superficie. Por eso las afirmaciones de abajo van contra los LITERALES que se
	// recibieron, y no contra lo que la puerta conteste.
	[TestFixture]
	public sealed class ThePlateReachesTheOwnersScreenAsOneTextAndTheJoinKeyStaysOffIt
	{
		private const string TheVin = "1HGCM82633A004352";

		private const string TheSection = "Vehiculo.razor";

		// LA PLACA RECIBIDA Y SU LLAVE, las dos escritas como LITERAL. La llave no se deriva de la
		// placa aca: derivarla seria pedirle a la prueba la misma cuenta que esta midiendo.
		private const string ThePlateAsPublished = "BQR-482";
		private const string ItsJoinKey = "BQR482";

		private static readonly DateTime TheDayItWasAssigned =
			new DateTime(2026, 1, 15, 9, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheDayTheScreenAsks =
			new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

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

		// ===== EL PORTADOR CONTESTA, Y CONTESTA LO QUE SE RECIBIO =========================

		// LAS CUATRO RESPUESTAS, ANCLADAS A LO QUE LLEGO. El instante que la pantalla nombra, la
		// presencia, el texto publicado y la frase: cada uno contra su LITERAL y no contra otra lectura
		// de la misma puerta.
		[Test]
		public void TheFourAnswersAreAnchoredToThePlateThatWasReceivedAndNotToAnotherRead()
		{
			ThePlateArrives();

			ThePlateOfThisVehicle plate = WhatTheScreenIsHanded();

			Assert.That(plate.AskedAt, Is.EqualTo("2026-06-01 00:00:00Z"),
				"El instante en que la pantalla pregunto. Cual placa rige es a ese momento y a ningun "
				+ "otro.");

			Assert.That(plate.PlatePublished, Is.True);

			Assert.That(plate.AsPublished, Is.EqualTo(ThePlateAsPublished),
				"El texto publicado es el que LLEGO, letra por letra y con su guion. Comparar contra "
				+ "otra lectura de la puerta no dice nada: son dos consumidores de un solo resolvedor y "
				+ "se ponen de acuerdo tambien cuando el resolvedor esta mal.");

			Assert.That(plate.Says,
				Does.Contain(ThePlateAsPublished).And.Contains("2026-01-15"),
				"La frase dice CUAL es la placa y DESDE CUANDO rige. Sin el instante adentro, la frase "
				+ "no dice cual pregunta contesto.");
		}

		// Y EL TEXTO QUE VIAJA ES EL PUBLICADO Y NO LA LLAVE. Es el discriminador que LP-4 midio en su
		// propia superficie: atar una de las dos lecturas a la otra deja TODO verde, porque las dos
		// contestan y las dos son de la misma placa. Lo unico que lo atrapa es afirmar que el que llego
		// es el que lleva el separador.
		[Test]
		public void TheTextTheScreenIsHandedIsThePublishedOneAndNotTheJoinKey()
		{
			ThePlateArrives();

			ThePlateOfThisVehicle plate = WhatTheScreenIsHanded();

			Assert.That(plate.AsPublished, Is.Not.EqualTo(ItsJoinKey),
				"El portador trae la LLAVE donde va el texto publicado. Las dos son de la misma placa y "
				+ "las dos contestan, asi que ninguna afirmacion de presencia lo ve: la publicada lleva "
				+ "el separador que el concesionario escribio y la llave no.");

			Assert.That(plate.Says, Does.Not.Contain(ItsJoinKey),
				"Y la frase tampoco cita la llave. Una frase que dijera las dos formas le pondria a la "
				+ "persona la misma placa escrita de dos maneras en el mismo renglon.");

			// Y LAS DOS FORMAS SON DISTINTAS EN ESTE ESCENARIO, que es lo unico que hace que la
			// afirmacion de arriba sirva de algo. Con una placa sin separador las dos coinciden y no
			// habria nada que discriminar.
			Assert.That(ThePlateAsPublished, Is.Not.EqualTo(ItsJoinKey));
		}

		// ===== LA LLAVE NO LLEGA A LA PANTALLA, NI POR EL PORTADOR NI POR EL MARKUP ========

		// EL PORTADOR NO TIENE POR DONDE ENTREGARLA. Se enumeran sus miembros y no se buscan nombres
		// prohibidos: un miembro nuevo con CUALQUIER nombre se pone rojo, que es lo que un censo de
		// vocabulario no hace.
		[Test]
		public void TheCarrierHasExactlyTheseFourMembersAndNoneOfThemIsTheJoinKey()
		{
			var names = new List<string>();

			foreach (PropertyInfo each in typeof(ThePlateOfThisVehicle).GetProperties())
				names.Add(each.Name);

			Assert.That(names, Is.EquivalentTo(new[]
			{
				"AskedAt", "PlatePublished", "AsPublished", "Says",
			}),
				"El portador de la placa cambio de forma. La llave de cruce NO va aqui: es la mitad que "
				+ "se COMPARA contra otro registro, y una persona que lee una columna veria la misma "
				+ "placa escrita de dos formas. El asistente si se lleva las dos, por su propia tool.");

			// Y LA PUERTA SI LAS CONTESTA LAS DOS, que es lo que separa "la pantalla no la dibuja" de
			// "no hay ninguna llave". Sin este renglon, borrar la lectura de la llave dejaria lo de
			// arriba verde y mas fuerte de lo que es.
			ThePlateArrives();

			Assert.That(
				APlateOnTheDoorAndInTheDomain.JoinKeyAsOf(journal.Api, TheDayTheScreenAsks),
				Is.EqualTo(ItsJoinKey));
		}

		// Y EL MARKUP NO NOMBRA LA LLAVE, NI DICE EL TEXTO PUBLICADO DOS VECES. El portador no lleva la
		// llave, asi que esa mitad no se puede escribir hoy -- y esa es la razon de escribirla: el dia
		// que alguien le agregue el campo al portador "por completitud", este renglon es el que dice
		// que la pantalla sigue sin dibujarlo.
		//
		// EL TEXTO PUBLICADO SI SE PINTA, UNA SOLA VEZ Y EN LA CARATULA, donde vivia el VIN: la placa
		// es el nombre PUBLICO del carro y es lo que se lee de un vistazo. El renglon de la hoja sigue
		// pintando la frase del dominio, que es la que dice DESDE CUANDO rige y la unica que contesta
		// cuando no hay ninguna. Son dos sitios y UNA sola forma de escribirla, que es como el VIN ya
		// vivia en los dos. Lo prohibido sigue siendo la SEGUNDA FORMA -- la llave -- y decir el texto
		// publicado dos veces, que es lo que la cuenta de aca abajo mide.
		//
		// SE MIRA RENGLON POR RENGLON Y LOS COMENTARIOS NO CUENTAN, que es la misma forma que
		// NoShellDoesStringSurgeryOnAPlate le dio a su barrido y por la misma razon: el '///' que
		// explica por que la llave no esta ES prosa sobre la decision, y una prueba que lo cuente como
		// una infraccion castiga escribir la razon. Medido: la primera version de esta prueba se puso
		// roja por su propio comentario.
		[Test]
		public void AndNoLineOfTheSectionPrintsAJoinKeyOrASecondSpellingOfThePlate()
		{
			string live = TheLivePartOfTheSection();

			foreach (string forbidden in new[] { "JoinKey", "llave" })
				Assert.That(live, Does.Not.Contain(forbidden),
					"La seccion del vehiculo imprime '" + forbidden + "'. La llave existe para comparar "
					+ "contra otro registro y no para leerla: una persona que lee una columna veria la "
					+ "misma placa escrita de dos formas.");

			Assert.That(TimesItSays(live, "Placa.AsPublished"), Is.EqualTo(1),
				"El texto publicado de la placa se pinta una cantidad de veces distinta de UNA. La "
				+ "frase del dominio ya lo lleva adentro letra por letra, asi que pintarlo AL LADO de "
				+ "ella imprime la misma placa dos veces en una columna que se lee de un vistazo.");

			// Y ESA UNICA VEZ ESTA EN LA CARATULA Y NO ENTRE LOS RENGLONES. La barra de hojas es la
			// frontera, asi que se afirma que existe antes de medir contra ella: sin ese renglon un
			// rename la deja en -1 y la comparacion se pone roja por la razon equivocada.
			int theSubtabs = live.IndexOf("<div class=\"d-subtabs\"", StringComparison.Ordinal);

			Assert.That(theSubtabs, Is.GreaterThan(0),
				"La seccion dejo de tener la barra de hojas, que es la frontera entre la caratula y los "
				+ "renglones. Sin ella la afirmacion de abajo no sabe donde esta mirando.");

			Assert.That(live.IndexOf("Placa.AsPublished", StringComparison.Ordinal),
				Is.LessThan(theSubtabs),
				"El texto publicado de la placa se fue de la caratula. Va arriba, donde vivia el VIN, y "
				+ "no entre los renglones de la hoja: ahi abajo caeria pegado a la frase, que ya lo "
				+ "dice.");

			// Y NINGUNA CONCATENACION CON LA FRASE. Es lo que el barrido de aritmetica no ve nunca,
			// porque un '+' con un operando de texto es concatenacion y no una cuenta.
			foreach (string forbidden in new[] { "Placa.Says +", "+ Placa.Says" })
				Assert.That(TheLivePartOfTheSection(), Does.Not.Contain(forbidden),
					"La seccion concatena algo con la frase de la placa ('" + forbidden + "'). Lo que "
					+ "sale de la puerta se cita letra por letra o no se cita.");

			// EL CONTROL: el mismo predicado SI ve cada forma escrita como codigo vivo. Sin esto,
			// apagar los comentarios de mas dejaria las afirmaciones de arriba verdes por no mirar.
			// 'Placa.AsPublished' se queda en la lista: lo que el control mide es el APAGADO de
			// comentarios, no si la forma esta permitida, y arriba se cuenta cuantas veces aparece.
			foreach (string planted in new[]
			{
				"<span>@Placa.JoinKey</span>", "<span>@Placa.AsPublished</span>",
				"<span>llave @Placa.Says</span>", "<span>@(Placa.Says + \" \")</span>",
			})
				Assert.That(TheLivePartOf(planted), Is.Not.Empty.And.EqualTo(planted),
					"El predicado apago un renglon de codigo vivo: '" + planted + "'.");
		}

		// ===== POR QUE SE PINTA LA FRASE Y NO EL TEXTO SUELTO =============================

		// LA FRASE CONTIENE EL TEXTO PUBLICADO, MEDIDO. Es la segunda cara de E48: el hermano esta en la
		// SUPERFICIE siempre, y si la frase compuesta ya lo lleva adentro, pintar los dos imprime la
		// misma placa dos veces en una columna que la persona lee de un vistazo.
		[Test]
		public void TheSentenceCarriesThePublishedTextInsideItAndTheRowPaintsOnlyTheSentence()
		{
			ThePlateArrives();

			ThePlateOfThisVehicle plate = WhatTheScreenIsHanded();

			Assert.That(plate.Says, Does.Contain(plate.AsPublished),
				"La frase dejo de llevar el texto publicado adentro. Si eso cambio, la decision de "
				+ "pintar una sola cosa se vuelve a mirar en vez de heredarse.");

			string markup = TheSectionAsItIsWritten();

			Assert.That(markup, Does.Contain("Placa.Says"),
				"El renglon dejo de pintar la frase de la placa.");

			Assert.That(markup, Does.Contain("<span class=\"d-row__k\">Placa</span>"),
				"La etiqueta del renglon dejo de nombrar de que habla. Es lo que hace para una persona "
				+ "lo que un nombre de campo hace para un modelo, y sin ella la frase queda suelta "
				+ "entre el VIN y la fecha de entrega.");
		}

		// ===== SIN PLACA HAY FRASE Y NO HUECO, Y EL VIN NO SE METE EN SU LUGAR ============

		// §12 PRUEBA 10. La ausencia la dice el DOMINIO -- la pantalla no pone ninguna frase suya -- y
		// se afirma que el VIN NO aparece: un VIN puesto donde va una placa se cruzaria contra los
		// registros de otro sistema y no coincidiria con nada, sin error y sin aviso.
		[Test]
		public void WithNoPlatePublishedTheRowPrintsTheDomainsOwnSentenceAndNeverTheVin()
		{
			ThePlateOfThisVehicle plate = WhatTheScreenIsHanded();

			Assert.That(plate.PlatePublished, Is.False);

			Assert.That(plate.AsPublished, Is.Empty,
				"Sin placa publicada el texto viene VACIO, y por eso el portador lleva la presencia al "
				+ "lado: una cadena vacia sola parece una respuesta.");

			Assert.That(plate.Says, Is.Not.Empty,
				"Y la frase contesta igual: la ausencia tiene tipo y tiene palabras.");

			Assert.That(plate.Says, Does.Not.Contain(TheVin),
				"El VIN se metio en la respuesta de la placa. No es una placa y no se cruza como una.");

			Assert.That(plate.Says, Does.Contain("concesionario"),
				"Y la frase dejo de nombrar QUIEN publica una placa, que es lo unico accionable que "
				+ "tiene quien la lee.");

			// Y ES LA DEL DOMINIO Y NO UNA DE LA PANTALLA: TheOwnerIsTold no gano ninguna frase por
			// este bloque, y la que se pinta es la misma que contesta la puerta.
			Assert.That(
				plate.Says,
				Is.EqualTo(APlateOnTheDoorAndInTheDomain.Describe(journal.Api, TheDayTheScreenAsks)));

			foreach (FieldInfo each in typeof(TheOwnerIsTold).GetFields(
				BindingFlags.Static | BindingFlags.Public))
				if (each.FieldType == typeof(string))
					Assert.That((string)each.GetValue(null), Does.Not.Contain("placa").IgnoreCase,
						"La caratula escribio una frase suya sobre la placa. El dominio ya la dice, y "
						+ "dos autores de la misma frase se separan.");
		}

		// Y EL PORTADOR PREGUNTA SI LA HAY ANTES DE PEDIR EL TEXTO. Sin ese pre-chequeo la lectura
		// guardada revienta, y una excepcion aqui no deja una frase: se lleva por delante el Reread
		// entero y la pagina queda en el aviso de error sobre una pantalla que estaba completa.
		[Test]
		public void TheCarrierAsksWhetherThereIsOneBeforeItAsksForTheGuardedText()
		{
			string carrier = TheCarrierAsItIsWritten();

			Assert.That(carrier, Does.Contain("bool published = api.IsAPlatePublished(asOf);"),
				"El portador dejo de preguntar si hay placa antes de nada.");

			Assert.That(
				carrier,
				Does.Contain("AsPublished = published ? api.ReadPlateAsPublished(asOf) : \"\","),
				"El portador pide el texto publicado sin preguntar antes. Esa lectura es GUARDADA: sin "
				+ "placa revienta, y el rechazo del dominio le llega a la persona como el aviso de "
				+ "error del navegador encima de una pagina que por lo demas esta bien.");

			// EL CONTROL: la lectura SI revienta sin placa, asi que el pre-chequeo esta impidiendo algo
			// de verdad. Sin este renglon, lo de arriba pasaria tambien si la puerta contestara vacio.
			Assert.That(
				() => APlateOnTheDoorAndInTheDomain.AsPublished(journal.Api, TheDayTheScreenAsks),
				Throws.Exception,
				"La lectura guardada dejo de rechazar sin placa, asi que el pre-chequeo no defiende "
				+ "nada y esta prueba esta verde por eso.");
		}

		// ===== LA FORMA DEL PORTADOR Y DE LA LECTURA DE LA CARATULA =======================

		// EL PORTADOR ES APARTE Y NO SE FUSIONO CON LA MITAD DEL VEHICULO. Es la quinta vez que esa
		// prohibicion se aplica, y la razon es la MISMA de siempre y no el tamanio: aquel tipo mide
		// este carro contra su SOBRE de garantia, y la placa no es eso. Fusionarlos volveria falso el
		// nombre de aquel tipo.
		[Test]
		public void ThePlateCarrierWasMergedIntoNeitherTheVehicleHalfNorTheSheet()
		{
			foreach (Type older in new[]
			{
				typeof(WhereThisVehicleStands), typeof(WhatTheManufacturerPublished),
			})
				foreach (PropertyInfo each in older.GetProperties())
				{
					Assert.That(each.PropertyType, Is.Not.EqualTo(typeof(ThePlateOfThisVehicle)),
						older.Name + " gano el portador de la placa.");

					foreach (string forbidden in new[] { "Plate", "Placa", "JoinKey" })
						Assert.That(each.Name, Does.Not.Contain(forbidden),
							older.Name + " gano '" + each.Name + "'. La placa es el nombre PUBLICO de "
							+ "este carro y no una medida contra el sobre.");
				}

			// Y AL REVES: el portador de la placa no absorbio ninguna de las dos.
			foreach (PropertyInfo each in typeof(ThePlateOfThisVehicle).GetProperties())
				Assert.That(
					each.PropertyType,
					Is.Not.EqualTo(typeof(WhereThisVehicleStands))
						.And.Not.EqualTo(typeof(WhatTheManufacturerPublished)),
					"El portador de la placa gano una de las hojas que ya estaban.");
		}

		// LA LECTURA DE LA CARATULA NO TOMA NINGUN ARGUMENTO Y NO AGREGA UN CUARTO RELOJ. Un DateTime en
		// un miembro publico de AppState seria una MEDICION, y el instante de la pantalla ya existe.
		[Test]
		public void TheShellsReadOfThePlateTakesNoArgumentAndAddsNoFourthClock()
		{
			MethodInfo read = typeof(AppState).GetMethod("ReadThePlateOfThisVehicle");

			Assert.That(read, Is.Not.Null);
			Assert.That(read.GetParameters(), Is.Empty,
				"La lectura de la caratula gano un parametro. Un instante ahi es una medicion: el "
				+ "instante lo lee ella del reloj de la maquina y no se lo pide a nadie.");

			var handingOutAnInstant = new List<string>();

			foreach (MethodInfo each in typeof(AppState).GetMethods(
				BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public
				| BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
				if (each.ReturnType == typeof(DateTime) && each.GetParameters().Length == 0)
					handingOutAnInstant.Add(each.Name);

			Assert.That(handingOutAnInstant, Has.Count.EqualTo(3),
				"La caratula gano un miembro que entrega un instante. El bloque de la placa no hacia "
				+ "falta uno: la lectura llama al que ya existe, que es el del INSTANTE EN QUE LA "
				+ "PANTALLA PREGUNTA y no es monotono a proposito.");
		}

		// Y EL PORTADOR NO LEE NINGUN RELOJ. El instante llega de afuera, porque el unico que lo puede
		// leer es la caratula.
		[Test]
		public void TheCarrierTakesTheInstantFromOutsideAndReadsNoClock()
		{
			MethodInfo read = typeof(ThePlateOfThisVehicle).GetMethod("ReadThrough");

			var said = new List<string>();

			foreach (ParameterInfo each in read.GetParameters())
				said.Add(each.Name + ":" + each.ParameterType.Name);

			Assert.That(said, Is.EqualTo(new[] { "api:DealershipCarAPI", "asOf:DateTime" }));

			foreach (string clock in new[] { "DateTime.UtcNow", "DateTime.Now", "DateTime.Today" })
				Assert.That(TheCarrierAsItIsWritten(), Does.Not.Contain(clock),
					"El portador lee un reloj ('" + clock + "'). El instante llega calculado.");
		}

		// ===== LA PAGINA LE PASA EL PORTADOR A LA SECCION =================================

		// EL SITIO DE LLAMADA. Es lo que ninguna otra prueba ve: la lectura puede estar en el Reread y
		// el portador no llegar a la seccion, y entonces la pantalla no dice nada -- sin error y sin
		// aviso. Se afirma el ARGUMENTO y no solo el nombre del parametro: pasarle otro portador
		// compila igual de bien.
		[Test]
		public void ThePageHandsThePlateDownToTheVehicleSectionBesideTheVinItAlreadyTook()
		{
			string page = Squeezed(ThePageAsItIsWritten());

			Assert.That(page, Does.Contain("Placa=\"@plate\""),
				"La pagina no le pasa la placa a la seccion del vehiculo. La lectura puede seguir en el "
				+ "Reread y la pantalla no mostrar nada.");

			Assert.That(page, Does.Contain("Placa=\"@plate\" Modelo="),
				"La placa dejo de ir al lado del VIN en el mismo sitio de llamada.");

			string vehicle = Squeezed(TheSectionAsItIsWritten());

			Assert.That(
				vehicle,
				Does.Contain("[Parameter] public ThePlateOfThisVehicle Placa { get; set; }"),
				"La seccion dejo de declarar el parametro de la placa, o le cambio el tipo.");
		}

		// ===== NINGUNA CUENTA Y NINGUNA CIRUGIA EN EL BLOQUE ==============================

		// EL BARRIDO DE ARITMETICA SOBRE ESTA SECCION, con su control. No es el de la placa -- ese es
		// NoShellDoesStringSurgeryOnAPlate y barre las ocho raices -- sino el de los numeros, sobre el
		// archivo en que este bloque entro.
		[Test]
		public void NoLineOfTheVehicleSectionHoldsAnArithmeticOperator()
		{
			List<ArithmeticHit> found = TheCodeOfTheShellsAsData.TheArithmeticInSource(
				"Wasm", "Sections/" + TheSection, TheSectionAsItIsWritten());

			Assert.That(found, Is.Empty, TheHitsInWords(found));

			// EL CONTROL DEL DETECTOR SOBRE ESTE ARCHIVO. Con espacios alrededor del operador: en un
			// .razor un guion pegado es parte de un nombre y pasaria por la razon equivocada.
			Assert.That(
				TheCodeOfTheShellsAsData.TheArithmeticInSource(
					"Wasm", "Sections/" + TheSection,
					"<div>@(Stands.Odometer - Stands.DeliveryReading)</div>"),
				Is.Not.Empty,
				"El detector no ve una resta en un .razor, asi que la afirmacion de arriba esta verde "
				+ "por no detectar.");
		}

		// ===== lo que hace falta para las de arriba =======================================

		private ThePlateOfThisVehicle WhatTheScreenIsHanded()
		{
			return ThePlateOfThisVehicle.ReadThrough(journal.Api, TheDayTheScreenAsks);
		}

		private void ThePlateArrives()
		{
			APlateOnTheDoorAndInTheDomain.RecordAssignment(
				journal.Api, ThePlateAsPublished, TheDayItWasAssigned);
		}

		private static string TheHitsInWords(List<ArithmeticHit> found)
		{
			var said = new System.Text.StringBuilder("EL BLOQUE LLEVA ARITMETICA:\n");

			foreach (ArithmeticHit hit in found) said.Append("  ").Append(hit).Append('\n');

			return said.ToString();
		}

		private static string Squeezed(string text)
		{
			return System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");
		}

		private static int TimesItSays(string markup, string what)
		{
			int found = 0;
			int at = markup.IndexOf(what, StringComparison.Ordinal);

			while (at >= 0)
			{
				found++;
				at = markup.IndexOf(what, at + what.Length, StringComparison.Ordinal);
			}

			return found;
		}

		private static string TheSectionAsItIsWritten()
		{
			return TheFileAtOutput(Path.Combine("SeccionesComoDato", TheSection));
		}

		// La seccion sin sus renglones de comentario. El apagado es CRUDO a proposito -- un renglon
		// entero que empieza con marca de comentario -- que es lo mismo que hace el barrido de cirugia
		// sobre las ocho raices: codigo vivo no empieza con '//'.
		private static string TheLivePartOfTheSection()
		{
			return TheLivePartOf(TheSectionAsItIsWritten());
		}

		private static string TheLivePartOf(string source)
		{
			var live = new List<string>();

			foreach (string line in source.Replace("\r\n", "\n").Split('\n'))
			{
				string start = line.TrimStart();

				if (start.StartsWith("//", StringComparison.Ordinal)) continue;
				if (start.StartsWith("@*", StringComparison.Ordinal)) continue;
				if (start.StartsWith("<!--", StringComparison.Ordinal)) continue;

				live.Add(line);
			}

			return string.Join("\n", live);
		}

		private static string TheCarrierAsItIsWritten()
		{
			return TheFileAtOutput(
				Path.Combine("CodigoComoDato", "Wasm", "ThePlateOfThisVehicle.cs"));
		}

		private static string ThePageAsItIsWritten()
		{
			return TheFileAtOutput("MyCar.razor.dato");
		}

		private static string TheFileAtOutput(string relative)
		{
			string path = Path.Combine(AppContext.BaseDirectory, relative);

			Assert.That(File.Exists(path), Is.True,
				"'" + relative + "' no llego al output. Se copia por glob para que un archivo nuevo "
				+ "entre solo; si este no llego, lo que fallo es la copia y no el codigo -- y un "
				+ "barrido que leyo cero esta verde por no haber mirado.");

			return File.ReadAllText(path);
		}
	}
}
