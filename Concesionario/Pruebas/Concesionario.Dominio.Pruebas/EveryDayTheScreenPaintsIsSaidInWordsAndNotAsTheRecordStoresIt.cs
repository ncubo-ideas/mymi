using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Concesionario.App.Wording;
using Concesionario.Wasm.Parts;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LOS DIAS QUE LA CARATULA DICE POR SU CUENTA, QUE ES LA MITAD QUE LA PUERTA NO DICE. El dominio
	// ya compone clave-mas-valores, asi que sus FRASES llegan dichas en el idioma que se le pide -- y
	// la puerta sigue entregando COLUMNAS crudas, que es como viaja un registro de columnas
	// paralelas. Una columna que es un DIA la tiene que decir la pantalla, porque el ORDEN de las
	// partes de una fecha es del idioma: "15 de diciembre de 2025" contra "15 December 2025".
	//
	// EL DEFECTO QUE ESTA ETAPA CIERRA NO ERA UNA COLUMNA SIN DECIR: eran DOS CAMPOS dichos de dos
	// maneras a la vez. WhereThisVehicleStands.DeliveredAt se decia en palabras en la hoja del
	// vehiculo y se imprimia 2025-12-15 en el detalle de la garantia; OdometerObservedAt igual,
	// repartido en tres secciones. El mismo campo, dos formas, en la misma pantalla. Ninguna prueba
	// lo veia porque las dos contestaban: un "2025-12-15" no esta vacio, no lleva llaves y no
	// revienta.
	//
	// UN INSTANTE NO PASA POR AQUI, Y LA DIFERENCIA ES DEL REGISTRO Y NO DE LA PRESENTACION. Un
	// instante se registro con su hora y se cita tal cual en los dos idiomas, que es la decision que
	// el turno de las estadias tomo; un dia de calendario lo dijo la sucursal aparte y el orden en
	// que se lee es del idioma. Por eso el censo de abajo los separa LEYENDO EL FUENTE DE LOS
	// PORTADORES: cual ayudante arma cada columna es lo unico que dice cual de las dos cosas es, y a
	// ojo son indistinguibles.
	//
	// Y POR QUE NO ALCANZA CON NINGUNA PRUEBA DE ACUERDO (E49). Comparar lo que pinta la pantalla
	// contra lo que contesta ComoSeLee es comparar dos consumidores de UN solo renderizador: se ponen
	// de acuerdo tambien cuando el renderizador esta uniformemente mal. Por eso cada redaccion de
	// abajo se afirma contra un LITERAL, y el barrido mira el markup como TEXTO.
	[TestFixture]
	public sealed class EveryDayTheScreenPaintsIsSaidInWordsAndNotAsTheRecordStoresIt
	{
		// LOS DOS DIAS CON QUE SE AFIRMA POR VALOR. Son literales y no salen de ningun journal: lo que
		// se mide es la redaccion, no de donde vino el dia.
		private const string TheDayOfTheDelivery = "2025-12-15";
		private const string TheDayOfAStay = "2026-06-10";

		// LAS DOS LLAMADAS QUE CUENTAN COMO DECIR UN DIA. La larga para un limite que se nombra una
		// vez; la corta para una lista de largo desconocido, porque la larga repetida cuatro veces deja
		// de leerse como una lista.
		private const string TheLongForm = "ComoSeLee.Dia";
		private const string TheShortForm = "ComoSeLee.DiaCorto";

		// COMO SE CLASIFICA CADA MENCION DE UNA COLUMNA-DIA EN EL MARKUP. 'CRUDO' va en mayuscula
		// porque es el unico que pone la prueba roja y es lo que se lee primero en el mensaje.
		private const string Said = "dicho por el renderizador";
		private const string Raw = "CRUDO";
		private const string Guard = "guarda de presencia";
		private const string HandedOn = "pasado a otra seccion";
		private const string Declared = "declarado como parametro";
		private const string LoopHeader = "encabezado de un recorrido";

		// COMO LOS PORTADORES ARMAN CADA CLASE DE COLUMNA. Los tres nombres salen del fuente de las
		// caratulas, no de esta prueba: son los ayudantes privados que cada portador ya tenia.
		private const string TheDayHelper = "TheDay(";
		private const string TheDayListHelper = "TheDaysOf(";
		private const string TheInstantHelper = "TheMoment(";

		private static readonly Regex ACarriedColumn = new Regex(
			@"public\s+(string(?:\[\])?)\s+(\w+)\s*\{\s*get;\s*init;\s*\}");

		private static readonly Regex AnAssignment = new Regex(@"^\s*(\w+)\s*=(?!=|>)\s*(.*)$");

		private static readonly Regex ANameThatEndsInDays = new Regex(@"Days?$");

		private static readonly Regex ARecorridoOverAColumn = new Regex(
			@"foreach\s*\(\s*string\s+(\w+)\s+in\s+[\w.]*");

		private static readonly Regex AComponentParameter = new Regex("^\\s*([A-Z]\\w*)=\"@");

		private static readonly Regex RunsOfWhitespace = new Regex(@"\s+");

		private static Phrases InSpanish => TheCatalogueOfTheScreen.InTheLanguageOfTheRecord;

		private static Phrases InEnglish => TheCatalogueOfTheScreen.InTheOtherLanguage;

		// ===== CUALES COLUMNAS SON UN DIA, LEIDO DEL FUENTE DE LOS PORTADORES =============

		// EL CENSO, CON LA REGLA QUE ENCONTRO CADA UNA. Se afirma el PAR y no solo el nombre: un censo
		// que dijera cuales son sin decir por que se puede quedar verde con las tres reglas rotas menos
		// una, y ahi el conjunto sigue completo por casualidad.
		//
		// TRES REGLAS Y NO UNA, porque los portadores arman sus dias de tres maneras: con el ayudante
		// de dia, con el de lista de dias, y -- las tres del periodo de estadias -- recibiendo un dia
		// que ya venia hecho, donde lo unico que queda es el nombre.
		[Test]
		public void TheDayColumnsOfTheCarriersAreTheOnesTheirOwnSourceBuildsAsDays()
		{
			var expected = new SortedDictionary<string, string>(StringComparer.Ordinal)
			{
				["CoveredDays"] = TheDayListHelper,
				["CurrentRevisionPublishedAt"] = TheDayHelper,
				["Days"] = ANameThatEndsInDays.ToString(),
				["DeliveredAt"] = TheDayHelper,
				["FromDay"] = ANameThatEndsInDays.ToString(),
				["OdometerObservedAt"] = TheDayHelper,
				["PublishedAt"] = TheDayHelper,
				["ToDay"] = ANameThatEndsInDays.ToString(),
			};

			Assert.That(InWords(TheDayColumnsAndWhy()), Is.EqualTo(InWords(expected)),
				"Las columnas-dia de los portadores cambiaron. Una columna nueva que sea un DIA la tiene "
				+ "que decir la pantalla, porque el orden de las partes de una fecha es del idioma; una "
				+ "que ya no lo sea deja de pedirlo. Las dos cosas se miran a mano.");

			// Y LAS TRES REGLAS APORTAN. Sin este renglon, borrar una de las tres deja el censo corto y
			// el barrido de abajo ciego para esas columnas, con todo verde.
			foreach (string rule in new[]
			{
				TheDayHelper, TheDayListHelper, ANameThatEndsInDays.ToString(),
			})
				Assert.That(TheDayColumnsAndWhy().Values, Does.Contain(rule),
					"Ninguna columna-dia la encontro la regla '" + rule + "'. Una regla que no aporta "
					+ "nada es una regla que se puede borrar sin que nada se ponga rojo.");
		}

		// Y UN INSTANTE NO ES UN DIA, dicho por el mismo fuente que los separa. Es el control del
		// censo: un ayudante que dijera los instantes como dias dejaria todo verde y le pondria a la
		// persona una hora de registro escrita en palabras.
		[Test]
		public void AnInstantIsNotADayAndTheCarriersOwnSourceIsWhatTellsThemApart()
		{
			List<string> instants = TheColumnsBuiltWith(TheInstantHelper);

			Assert.That(instants, Is.EquivalentTo(new[]
			{
				"AskedAt", "EarlierFrom", "EarlierTo", "From", "To",
			}),
				"Las columnas que los portadores arman como INSTANTE cambiaron. Un instante se cita tal "
				+ "cual en los dos idiomas: se registro con su hora, y decirlo en palabras pediria una "
				+ "zona que este registro no lleva.");

			foreach (string each in instants)
				Assert.That(TheDayColumnsAndWhy().Keys, Does.Not.Contain(each),
					"'" + each + "' quedo contado como dia Y como instante. A ojo son indistinguibles "
					+ "en la pantalla, y lo unico que los separa es con cual ayudante se armo.");
		}

		// ===== NINGUNA SECCION PINTA UNA COLUMNA-DIA CRUDA ================================

		// EL BARRIDO, Y EL CENSO ENTERO POR VALOR AL LADO. No lleva numero de linea a proposito: una
		// linea se mueve sola cuando alguien reindenta, y un sitio no. Lo que queda fijo es el archivo,
		// la columna y que se hizo con ella.
		//
		// SE SIGUEN DOS SALTOS DE NOMBRE, porque sin ellos el barrido no ve donde el dia se pinta de
		// verdad: la variable de un recorrido sobre una lista de dias, y el parametro con que una
		// seccion se lo pasa a otra. El dia del odometro llega a la portada llamandose ObservadoEl.
		[Test]
		public void NoRazorOfTheCaratulaPaintsADayColumnWithoutSayingItThroughTheRenderer()
		{
			List<string> sites = TheDaySitesInTheMarkup();

			var raw = new List<string>();

			foreach (string each in sites)
				if (each.EndsWith(Raw, StringComparison.Ordinal)) raw.Add(each);

			Assert.That(raw, Is.Empty,
				"La caratula imprime un dia tal como el registro lo guarda:\n  "
				+ string.Join("\n  ", raw)
				+ "\nEso deja la mitad de la pantalla en el idioma de la persona y la otra mitad en el "
				+ "formato del registro, y el MISMO campo dicho de dos formas donde dos secciones lo "
				+ "pintan.");

			Assert.That(sites, Is.EquivalentTo(new[]
			{
				"Pages/MyCar.razor OdometerObservedAt " + HandedOn,
				"Pages/MyCar.razor ObservadoEl " + HandedOn,
				"Sections/Historial.razor CoveredDays " + LoopHeader,
				"Sections/Historial.razor dia " + LoopHeader,
				"Sections/Historial.razor dia " + Said,
				"Sections/Inicio.razor ObservadoEl " + Guard,
				"Sections/Inicio.razor ObservadoEl " + Said,
				"Sections/Inicio.razor ObservadoEl " + Declared,
				"Sections/MyVehicle.razor CurrentRevisionPublishedAt " + Said,
				"Sections/Vehiculo.razor OdometerObservedAt " + Guard,
				"Sections/Vehiculo.razor OdometerObservedAt " + Said,
				"Sections/Vehiculo.razor DeliveredAt " + Said,
				"Sections/Warranty.razor PublishedAt " + Said,
				"Sections/Warranty.razor DeliveredAt " + Said,
				"Sections/Warranty.razor OdometerObservedAt " + Said,
			}),
				"Los sitios en que la caratula toca una columna-dia cambiaron:\n  "
				+ string.Join("\n  ", sites)
				+ "\nUn sitio nuevo se mira a mano: que se diga no alcanza, hay que elegir la forma -- "
				+ "larga para un limite que se nombra una vez, corta para una lista.");

			// Y DOS CAMPOS LLEGAN DESDE MAS DE UNA SECCION, que es de donde salio el defecto de esta
			// etapa. Sin este renglon, que las tres secciones digan el dia se puede cumplir con una sola
			// de ellas viva.
			Assert.That(TheSectionsThatTouch("DeliveredAt"), Is.EquivalentTo(new[]
			{
				"Sections/Vehiculo.razor", "Sections/Warranty.razor",
			}));

			Assert.That(TheSectionsThatTouch("OdometerObservedAt"), Is.EquivalentTo(new[]
			{
				"Pages/MyCar.razor", "Sections/Vehiculo.razor", "Sections/Warranty.razor",
			}));
		}

		// EL CONTROL DEL DETECTOR, y el tercer renglon es el que obliga a contar parentesis. Preguntar
		// si la LINEA nombra al renderizador da por dicho al dia crudo que va al lado del dicho, y una
		// linea asi es exactamente la forma en que este defecto se escribe sin querer.
		[Test]
		public void TheSweepSeesADayPaintedRawAndSeesOneSaidThroughTheRenderer()
		{
			Assert.That(
				TheDaySitesIn("control.razor", "<span>@Stands.DeliveredAt</span>"),
				Is.EquivalentTo(new[] { "control.razor DeliveredAt " + Raw }),
				"El detector no ve un dia pintado crudo, asi que el barrido de arriba esta verde por no "
				+ "haber detectado.");

			Assert.That(
				TheDaySitesIn(
					"control.razor",
					"<span>@ComoSeLee.Dia(Texts.ThePhrases, Stands.DeliveredAt)</span>"),
				Is.EquivalentTo(new[] { "control.razor DeliveredAt " + Said }),
				"El detector da por crudo un dia que SI pasa por el renderizador, asi que el barrido de "
				+ "arriba se pondria rojo contra markup correcto.");

			Assert.That(
				TheDaySitesIn(
					"control.razor",
					"<span>@ComoSeLee.Dia(Texts.ThePhrases, Stands.PublishedAt) "
					+ "@Stands.DeliveredAt</span>"),
				Is.EquivalentTo(new[]
				{
					"control.razor PublishedAt " + Said, "control.razor DeliveredAt " + Raw,
				}),
				"Dos dias en una linea, uno dicho y el otro crudo, y el detector los da por iguales. "
				+ "Un detector que solo pregunte si la linea nombra al renderizador esta ciego a la "
				+ "forma en que este defecto se escribe.");

			// Y UN GUION NO ES UNA MENCION. La mitad de un .razor es markup y ahi un guion es parte de un
			// nombre: sin este renglon, cada id="cuenta-al-dia" seria un dia pintado crudo.
			Assert.That(
				TheDaySitesIn("control.razor", "<span id=\"cuenta-al-dia\">@Texts[\"x\"]</span>"),
				Is.Empty,
				"El detector lee un pedazo de un nombre con guion como si fuera una columna-dia.");
		}

		// ===== LO QUE LA PERSONA LEE, POR VALOR Y EN LOS DOS IDIOMAS ======================

		// LAS DOS FORMAS, CONTRA SUS LITERALES. Ninguna se compara contra la otra ni contra lo que
		// conteste la puerta: eso serian dos consumidores de un solo renderizador (E49).
		[Test]
		public void TheDaysTheScreenPaintsAreSaidInWordsInBothLanguages()
		{
			Assert.That(
				ComoSeLee.Dia(InSpanish, TheDayOfTheDelivery), Is.EqualTo("15 de diciembre de 2025"));

			Assert.That(ComoSeLee.Dia(InEnglish, TheDayOfTheDelivery), Is.EqualTo("15 December 2025"));

			Assert.That(ComoSeLee.DiaCorto(InSpanish, TheDayOfAStay), Is.EqualTo("10 jun 2026"));

			Assert.That(ComoSeLee.DiaCorto(InEnglish, TheDayOfAStay), Is.EqualTo("10 Jun 2026"));

			// Y NINGUNA DE LAS CUATRO ES EL DIA COMO EL REGISTRO LO GUARDA. El discriminador es el guion:
			// un yyyy-MM-dd lleva dos y ninguna redaccion lleva ninguno. Sin este renglon, un
			// renderizador que devolviera su argumento intacto pasaria las cuatro afirmaciones el dia que
			// alguien tradujera mal los literales de arriba.
			foreach (string said in new[]
			{
				ComoSeLee.Dia(InSpanish, TheDayOfTheDelivery),
				ComoSeLee.Dia(InEnglish, TheDayOfTheDelivery),
				ComoSeLee.DiaCorto(InSpanish, TheDayOfAStay),
				ComoSeLee.DiaCorto(InEnglish, TheDayOfAStay),
			})
				Assert.That(said, Does.Not.Contain("-"),
					"'" + said + "' salio con el guion del registro adentro, asi que el dia no se dijo: "
					+ "se copio.");
		}

		// LA MISMA COLUMNA DICHA LARGA EN UN RENGLON Y CORTA EN UNA BALDOSA, y las dos son palabras. Es
		// una decision de la pantalla y no un descuido: el dia de la ultima lectura entra en un renglon
		// de la hoja y tambien en una baldosa de la portada, donde la forma larga no cabe. Se afirma
		// que las dos formas DIFIEREN para que unificarlas se vea.
		[Test]
		public void TheObservationDayIsSaidLongInARowAndShortInATileAndBothAreWords()
		{
			Assert.That(
				ComoSeLee.Dia(InSpanish, TheDayOfTheDelivery),
				Is.Not.EqualTo(ComoSeLee.DiaCorto(InSpanish, TheDayOfTheDelivery)),
				"Las dos formas del dia dejaron de diferir, asi que elegir entre ellas no dice nada y "
				+ "los sitios que eligieron la corta lo hicieron por nada.");

			Assert.That(
				ComoSeLee.DiaCorto(InSpanish, TheDayOfTheDelivery).Length,
				Is.LessThan(ComoSeLee.Dia(InSpanish, TheDayOfTheDelivery).Length),
				"La forma corta dejo de ser la corta.");

			// Y LA PORTADA SIGUE ELIGIENDO LA CORTA, que es donde la baldosa la pide, mientras las dos
			// secciones de la hoja eligen la larga. Sin este renglon el par de arriba es cierto sin que
			// nadie lo use, y unificar las dos formas no pondria nada rojo.
			Assert.That(
				TheDaySitesInTheMarkup(),
				Does.Contain("Sections/Inicio.razor ObservadoEl " + Said));

			Assert.That(TheMarkupOf("Sections/Inicio.razor"), Does.Contain(TheShortForm));

			foreach (string section in new[] { "Sections/Vehiculo.razor", "Sections/Warranty.razor" })
				Assert.That(TheMarkupOf(section), Does.Not.Contain(TheShortForm),
					section + " paso a decir un dia en la forma corta. Los renglones de la hoja tienen "
					+ "el ancho para la larga, y es la que el dia de la entrega ya usa al lado.");
		}

		// LAS PASTILLAS DE UNA ESTADIA DICEN SUS DIAS CON LAS MISMAS PALABRAS QUE LA FRASE DE ABAJO.
		// Las dos van en la misma tarjeta, asi que una pastilla que dijera "10 de junio de 2026" encima
		// de una frase que dice "10 jun 2026" se lee como dos fechas.
		//
		// LAS DOS SE AFIRMAN CONTRA EL LITERAL Y NO UNA CONTRA LA OTRA: pasan por el mismo
		// renderizador, asi que coinciden tambien cuando el renderizador esta mal (E49).
		[Test]
		public void ThePillsOfAStaySayItsDaysInTheSameWordsAsTheDomainsOwnSentenceBesideThem()
		{
			Assert.That(ComoSeLee.DiaCorto(InSpanish, TheDayOfAStay), Is.EqualTo("10 jun 2026"));

			Assert.That(
				TheSentenceTheDoorWouldCompose.AShortDayAsTheSentenceSaysIt(
					TheDayOfAStay, TheLanguage.OfTheRecord),
				Is.EqualTo("10 jun 2026"));

			Assert.That(ComoSeLee.DiaCorto(InEnglish, TheDayOfAStay), Is.EqualTo("10 Jun 2026"));

			Assert.That(
				TheSentenceTheDoorWouldCompose.AShortDayAsTheSentenceSaysIt(
					TheDayOfAStay, TheLanguage.OfAnEnglishReader),
				Is.EqualTo("10 Jun 2026"));

			// Y LA LARGA NO COINCIDE, que es lo que hace que la eleccion de la pastilla importe.
			Assert.That(ComoSeLee.Dia(InSpanish, TheDayOfAStay), Is.Not.EqualTo("10 jun 2026"));
		}

		// LAS DOS LINEAS QUE LA CARATULA COMPONE ALREDEDOR DE UN DIA, enteras y en los dos idiomas. Es
		// lo que ninguna afirmacion sobre el renderizador solo puede ver: el hueco se llama 'day' en
		// las dos, y un hueco con el nombre cambiado deja la frase con '{day}' a la vista sin reventar.
		[Test]
		public void TheTwoLinesTheCaratulaComposesAroundADaySayItInWordsInBothLanguages()
		{
			Assert.That(
				TheCatalogueOfTheScreen.With(
					TheCatalogueOfTheScreen.InSpanish("warranty.observedOn"),
					("day", ComoSeLee.Dia(InSpanish, TheDayOfTheDelivery))),
				Is.EqualTo("observada el 15 de diciembre de 2025"));

			Assert.That(
				TheCatalogueOfTheScreen.With(
					TheCatalogueOfTheScreen.InEnglish("warranty.observedOn"),
					("day", ComoSeLee.Dia(InEnglish, TheDayOfTheDelivery))),
				Is.EqualTo("observed on 15 December 2025"));

			Assert.That(
				TheCatalogueOfTheScreen.With(
					TheCatalogueOfTheScreen.InSpanish("sheet.scopeRevision"),
					("revision", "MY2019-R1"),
					("day", ComoSeLee.Dia(InSpanish, TheDayOfTheDelivery))),
				Is.EqualTo(
					"La ficha vigente es la revisión <span class=\"mono\">MY2019-R1</span>, publicada "
					+ "el 15 de diciembre de 2025. Cada línea de abajo nombra la revisión de la que "
					+ "salió, que no siempre es esta."));

			Assert.That(
				TheCatalogueOfTheScreen.With(
					TheCatalogueOfTheScreen.InEnglish("sheet.scopeRevision"),
					("revision", "MY2019-R1"),
					("day", ComoSeLee.Dia(InEnglish, TheDayOfTheDelivery))),
				Is.EqualTo(
					"The sheet in force is revision <span class=\"mono\">MY2019-R1</span>, published "
					+ "on 15 December 2025. Every line below names the revision it came from, which is "
					+ "not always this one."));
		}

		// Y NINGUNA DE ESAS LINEAS VUELVE CON UN HUECO SIN LLENAR NI CON UNA CLAVE CRUDA. Con su
		// control, que es lo que hace que el barrido no este verde por no mirar: con el nombre del
		// hueco cruzado el '{day}' SE QUEDA, y eso es exactamente lo que una persona leeria.
		[Test]
		public void NoLineTheCaratulaComposesAroundADayComesBackWithAHoleOrARawKeyInIt()
		{
			foreach (string said in EveryLineTheCaratulaComposesAroundADay())
				Assert.That(said, Does.Not.Contain("{").And.Not.Contain("}"),
					"Una llave en la linea final es una clave que el catalogo no tiene o un hueco cuyo "
					+ "nombre no calza con el que el markup le pasa: " + said);

			Assert.That(
				TheCatalogueOfTheScreen.With(
					TheCatalogueOfTheScreen.InSpanish("warranty.observedOn"),
					("instant", ComoSeLee.Dia(InSpanish, TheDayOfTheDelivery))),
				Does.Contain("{day}"),
				"El relleno de huecos tapa un nombre que no calza, asi que el barrido de arriba no "
				+ "puede ver nada.");
		}

		private static IEnumerable<string> EveryLineTheCaratulaComposesAroundADay()
		{
			foreach (string said in TheLinesComposedAround(
				InSpanish,
				TheCatalogueOfTheScreen.InSpanish("warranty.observedOn"),
				TheCatalogueOfTheScreen.InSpanish("sheet.scopeRevision")))
				yield return said;

			foreach (string said in TheLinesComposedAround(
				InEnglish,
				TheCatalogueOfTheScreen.InEnglish("warranty.observedOn"),
				TheCatalogueOfTheScreen.InEnglish("sheet.scopeRevision")))
				yield return said;
		}

		private static IEnumerable<string> TheLinesComposedAround(
			Phrases phrases, string observedOn, string scopeRevision)
		{
			string day = ComoSeLee.Dia(phrases, TheDayOfTheDelivery);

			yield return TheCatalogueOfTheScreen.With(observedOn, ("day", day));

			yield return TheCatalogueOfTheScreen.With(
				scopeRevision, ("revision", "MY2019-R1"), ("day", day));
		}

		// ===== el instrumento: cuales columnas son un dia, y donde las toca el markup ====

		// CADA COLUMNA-DIA CON LA REGLA QUE LA ENCONTRO. Se lee el fuente de los portadores y no se
		// escribe una lista: una columna nueva armada con el ayudante de dia entra sola, y lo unico que
		// queda a mano es el par de arriba, que es el que obliga a mirarla.
		private static SortedDictionary<string, string> TheDayColumnsAndWhy()
		{
			var found = new SortedDictionary<string, string>(StringComparer.Ordinal);
			List<string> instants = TheColumnsBuiltWith(TheInstantHelper);
			var byName = new List<string>();

			foreach (string file in EveryCarrierOfTheShell())
			{
				string source = File.ReadAllText(file);
				IDictionary<string, string> columns = TheCarriedColumnsIn(source);

				foreach (KeyValuePair<string, string> each in TheAssignmentsIn(source, columns))
				{
					if (each.Value.Contains(TheDayHelper, StringComparison.Ordinal))
						found[each.Key] = TheDayHelper;
					else if (each.Value.Contains(TheDayListHelper, StringComparison.Ordinal))
						found[each.Key] = TheDayListHelper;
				}

				foreach (string name in columns.Keys)
					if (!instants.Contains(name) && ANameThatEndsInDays.IsMatch(name))
						byName.Add(name);
			}

			foreach (string name in byName)
				if (!found.ContainsKey(name)) found[name] = ANameThatEndsInDays.ToString();

			return found;
		}

		// Las columnas que ese ayudante arma, en todos los portadores. Es la misma lectura que la de
		// arriba, con el ayudante como argumento, para que el control del instante no sea una copia.
		private static List<string> TheColumnsBuiltWith(string helper)
		{
			var found = new List<string>();

			foreach (string file in EveryCarrierOfTheShell())
			{
				string source = File.ReadAllText(file);

				foreach (KeyValuePair<string, string> each
					in TheAssignmentsIn(source, TheCarriedColumnsIn(source)))
					if (each.Value.Contains(helper, StringComparison.Ordinal)
						&& !found.Contains(each.Key))
						found.Add(each.Key);
			}

			found.Sort(StringComparer.Ordinal);

			return found;
		}

		// Las propiedades de texto que un portador entrega, por nombre. Se miran las de 'get; init;' y
		// no todas: lo que la seccion pinta es lo que el portador le entrego armado.
		private static IDictionary<string, string> TheCarriedColumnsIn(string source)
		{
			var columns = new Dictionary<string, string>(StringComparer.Ordinal);

			foreach (Match each in ACarriedColumn.Matches(RunsOfWhitespace.Replace(source, " ")))
				columns[each.Groups[2].Value] = each.Groups[1].Value;

			return columns;
		}

		// Con que se arma cada una de esas columnas, renglon por renglon. Un renglon que termina en '='
		// se pega con el siguiente: una asignacion partida en dos lineas es la forma en que la ficha
		// del fabricante escribe la suya, y leyendo linea por linea no se ve.
		private static IDictionary<string, string> TheAssignmentsIn(
			string source, IDictionary<string, string> columns)
		{
			var assignments = new Dictionary<string, string>(StringComparer.Ordinal);

			foreach (string line in TheLinesWithTheirContinuationsJoined(source))
			{
				Match assignment = AnAssignment.Match(line);

				if (!assignment.Success) continue;
				if (!columns.ContainsKey(assignment.Groups[1].Value)) continue;

				assignments[assignment.Groups[1].Value] = assignment.Groups[2].Value;
			}

			return assignments;
		}

		private static List<string> TheLinesWithTheirContinuationsJoined(string source)
		{
			string[] written = source.Replace("\r\n", "\n").Split('\n');
			var joined = new List<string>();

			for (int i = 0; i < written.Length; i++)
			{
				string line = written[i];

				while (line.TrimEnd().EndsWith("=", StringComparison.Ordinal) && i + 1 < written.Length)
					line = line.TrimEnd() + " " + written[++i].Trim();

				joined.Add(line);
			}

			return joined;
		}

		// CADA SITIO EN QUE EL MARKUP TOCA UNA COLUMNA-DIA, con que se hizo con ella. Dos pasadas: la
		// primera sigue los saltos de nombre -- la variable de un recorrido y el parametro con que una
		// seccion le pasa un dia a otra -- y la segunda clasifica.
		private static List<string> TheDaySitesInTheMarkup()
		{
			List<string> names = TheNamesADayIsKnownByAcrossTheWholeCaratula();
			var sites = new List<string>();

			foreach (string file in EveryMarkupFileTheShellHas())
				sites.AddRange(TheDaySitesIn(InsideTheShell(file), File.ReadAllText(file), names));

			return sites;
		}

		// LOS SALTOS SE SIGUEN SOBRE LA CARATULA ENTERA Y NO ARCHIVO POR ARCHIVO, porque las dos puntas
		// del salto viven en archivos distintos: la pagina le pasa el dia del odometro a la portada, y
		// la portada -- que no nombra ninguna columna -- es la que lo pinta. Seguido por archivo, el
		// barrido mira la punta que no pinta nada.
		private static List<string> TheNamesADayIsKnownByAcrossTheWholeCaratula()
		{
			var names = new List<string>(TheDayColumnsAndWhy().Keys);

			foreach (string file in EveryMarkupFileTheShellHas())
				foreach (string each in TheNamesADayTravelsUnder(File.ReadAllText(file), names))
					if (!names.Contains(each)) names.Add(each);

			return names;
		}

		// El barrido sobre UN markup con los nombres ya resueltos. El control le pasa los suyos, que se
		// resuelven sobre su propia linea: una linea de control no tiene de donde recibir un salto.
		private static List<string> TheDaySitesIn(string file, string markup)
		{
			var names = new List<string>(TheDayColumnsAndWhy().Keys);

			names.AddRange(TheNamesADayTravelsUnder(markup, names));

			return TheDaySitesIn(file, markup, names);
		}

		private static List<string> TheDaySitesIn(string file, string markup, List<string> names)
		{
			var sites = new List<string>();

			foreach (string line in markup.Replace("\r\n", "\n").Split('\n'))
				foreach (string name in names)
					foreach (int at in WhereItIsNamedIn(line, name))
						sites.Add(file + " " + name + " " + HowItIsTreated(line, name, at));

			return sites;
		}

		// LOS DOS SALTOS DE NOMBRE QUE UN DIA DA EN EL MARKUP, y sin seguirlos el barrido mira el lugar
		// equivocado: sobre una lista lo que se pinta es la variable del recorrido, y una portada
		// recibe el dia del odometro llamandose ObservadoEl.
		private static List<string> TheNamesADayTravelsUnder(string markup, List<string> columns)
		{
			var found = new List<string>();

			foreach (string line in markup.Replace("\r\n", "\n").Split('\n'))
				foreach (string column in columns)
				{
					if (WhereItIsNamedIn(line, column).Count == 0) continue;

					Match recorrido = ARecorridoOverAColumn.Match(line);

					if (recorrido.Success && !found.Contains(recorrido.Groups[1].Value))
						found.Add(recorrido.Groups[1].Value);

					Match handed = AComponentParameter.Match(line);

					if (handed.Success && !found.Contains(handed.Groups[1].Value))
						found.Add(handed.Groups[1].Value);
				}

			return found;
		}

		// DONDE ESA LINEA NOMBRA ESA COLUMNA. Un identificador en un .razor no admite un guion a ningun
		// lado: la mitad del archivo es markup y ahi un guion es parte de un nombre --
		// id="cuenta-al-dia" lleva 'dia' adentro. Y un nombre seguido de '(' es una llamada, no una
		// columna: Texts.Days(n) cuenta dias y no dice ninguno.
		private static List<int> WhereItIsNamedIn(string line, string name)
		{
			var found = new List<int>();
			int at = line.IndexOf(name, StringComparison.Ordinal);

			while (at >= 0)
			{
				int after = at + name.Length;

				if (!(at > 0 && IsPartOfAName(line[at - 1]))
					&& !(after < line.Length && (IsPartOfAName(line[after]) || line[after] == '(')))
					found.Add(at);

				at = line.IndexOf(name, at + 1, StringComparison.Ordinal);
			}

			return found;
		}

		private static bool IsPartOfAName(char letter)
		{
			return char.IsLetterOrDigit(letter) || letter == '_' || letter == '-';
		}

		private static string HowItIsTreated(string line, string name, int at)
		{
			if (line.Substring(at + name.Length).StartsWith(".Length", StringComparison.Ordinal))
				return Guard;

			if (line.Contains("[Parameter]", StringComparison.Ordinal)) return Declared;

			if (AComponentParameter.IsMatch(line)) return HandedOn;

			if (ARecorridoOverAColumn.IsMatch(line)) return LoopHeader;

			return IsSaidAt(line, at) ? Said : Raw;
		}

		// SI ESA MENCION ESTA ADENTRO DE UNA LLAMADA AL RENDERIZADOR, contando parentesis desde el
		// principio de la linea. Preguntar si la LINEA nombra al renderizador no alcanza: una linea que
		// diga un dia y pinte otro crudo pasaria entera, y eso esta medido en el control.
		//
		// SE MIRA UNA LINEA A LA VEZ, y de ahi sale el unico punto ciego: una llamada al renderizador
		// PARTIDA en dos lineas deja la columna en una linea sin renderizador y se lee como cruda.
		// Falla del lado seguro -- el barrido se pone rojo y alguien mira -- y por eso el markup deja
		// cada llamada entera en su linea.
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
			foreach (string call in new[] { TheShortForm, TheLongForm })
				if (paren >= call.Length
					&& string.CompareOrdinal(line, paren - call.Length, call, 0, call.Length) == 0)
					return true;

			return false;
		}

		private static List<string> TheSectionsThatTouch(string column)
		{
			var found = new List<string>();

			foreach (string site in TheDaySitesInTheMarkup())
			{
				string[] said = site.Split(' ');

				if (said[1] == column && !found.Contains(said[0])) found.Add(said[0]);
			}

			return found;
		}

		// ===== lo que hace falta para las de arriba =======================================

		// SIN NINGUN catch y con la carpeta afirmada. Un barrido que leyo cero archivos esta verde por
		// no haber mirado, que es como una rebanada anterior se gano un rojo.
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
				"No llego ningun .cs de la caratula al output, asi que el censo de columnas-dia esta "
				+ "vacio por no haber leido.");

			return found;
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

		private static string InWords(SortedDictionary<string, string> columns)
		{
			var said = new StringBuilder();

			foreach (KeyValuePair<string, string> each in columns)
				said.Append(each.Key).Append(" <- ").Append(each.Value).Append('\n');

			return said.ToString();
		}
	}
}
