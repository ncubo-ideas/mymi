using System;
using System.IO;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LO QUE NINGUNA OTRA PRUEBA DE SH-7 PUEDE VER, y por que se mira sobre el MARKUP. Es la forma de
	// TheSectionsAreWiredIntoThePageAndReadNothingOfTheirOwn, aplicada a la sexta pestana.
	//
	// Todo lo demas de SH-7 se defiende sobre el portador que TheStaysOfThisVehicle compone, que es lo
	// unico que la seccion tiene para pintar. Pero tres regresiones viven fuera de el: que la pagina de
	// verdad renderice la seccion -- borrar la etiqueta deja el resto del modulo verde y la pantalla sin
	// el bloque entero --, que la pestana exista para poder llegar a ella, y que la seccion no lea nada
	// por su cuenta, que es la unica garantia estructural de que no pueda componer una respuesta.
	//
	// UN COMPONENTE COMPILADO NO SE PUEDE INSPECCIONAR sin un banco de pruebas de componentes, y en
	// este repo no hay ninguno. Lo que si se puede leer es la fuente, y el csproj la copia al output
	// COMO DATO, por glob, para que una seccion nueva entre sola.
	//
	// LO QUE ESTO NO CUBRE, DICHO: el markup RENDERIZADO. Que las etiquetas esten escritas no prueba
	// que la pantalla se vea bien. Eso lo mide una persona en un browser de verdad, y el informe de
	// SH-7 dice que se vio.
	[TestFixture]
	public sealed class TheHistorialSectionIsWiredIntoThePageAndReadsNothingOfItsOwn
	{
		private const string TheSection = "Historial.razor";

		// LA PAGINA RENDERIZA LA SECCION Y LE PASA EL PORTADOR QUE LEYO EN Reread. Sin la etiqueta, el
		// portador se compone bien, la suite sigue verde y la pantalla no tiene el bloque.
		[Test]
		public void ThePageRendersTheHistorialSectionAndHandsItTheStaysItRead()
		{
			string page = ThePageAsItIsWritten();

			Assert.That(page, Does.Contain("state.ReadTheStaysOfThisVehicle(texts.Language)"),
				"La pagina dejo de leer las estadias, asi que la seccion recibe null y no pinta nada "
				+ "-- sin ningun error y sin ningun aviso.");

			// LA ETIQUETA GANO UN SEGUNDO PORTADOR CON EL BLOQUE DEL HISTORIAL DE MANTENIMIENTO, UN
			// TERCERO CON EL DEL RITMO DE USO Y UN CUARTO CON EL DEL LIBRO DE COSTOS, y esta afirmacion
			// se AMPLIO las tres veces en vez de aflojarse: se sigue afirmando la etiqueta ENTERA, con
			// los cuatro portadores nombrados y en su orden. Aflojarla a un prefijo dejaria pasar una
			// etiqueta que perdio uno de los cuatro.
			Assert.That(
				page,
				Does.Contain(
					"<Historial Stays=\"@stays\" Operations=\"@operations\" Use=\"@use\" "
					+ "Spend=\"@spend\" />"),
				"La pagina dejo de renderizar la seccion Historial, o dejo de pasarle uno de sus cuatro "
				+ "portadores. Sin las estadias no llega el bloque de los dias en el taller; sin la "
				+ "agenda no llega el de lo que le han hecho ni la ausencia por clase; sin el ritmo no "
				+ "llega el de cuanto se maneja; sin el libro no llega el de en que se fue la plata.");
		}

		// Y HAY UNA PESTANA PARA LLEGAR A ELLA. Una seccion renderizada en una rama a la que ningun
		// boton lleva es una seccion que nadie ve, y la suite no lo nota.
		[Test]
		public void TheSixthTabExistsAndItsBranchIsTheOneThatRendersTheSection()
		{
			string page = ThePageAsItIsWritten();

			Assert.That(page, Does.Contain("id=\"go-historial\""),
				"La barra de pestanas no tiene el boton del Historial. La seccion se renderiza en una "
				+ "rama a la que nadie puede llegar.");

			// LA RAMA SE AFIRMA CON EL else if ENTERO Y NO CON 'tab == "historial"' A SECAS. MEDIDO:
			// con la comparacion suelta, borrar la rama que renderiza la seccion deja esta prueba
			// VERDE, porque el propio boton de la pestana lleva la misma comparacion adentro para
			// decidir si va resaltado. La forma corta afirmaba la existencia del boton dos veces.
			Assert.That(page, Does.Contain("else if (tab == \"historial\")"),
				"La pagina no tiene la RAMA de la pestana Historial. El boton existe y no lleva a "
				+ "ningun lado: la seccion se renderiza en una rama que no hay.");

			Assert.That(page, Does.Contain("tab = \"historial\""),
				"Ningun boton pone la pestana en historial.");
		}

		// Y LA SECCION NO LEE NADA POR SU CUENTA. Un componente sin @inject no tiene de donde sacar una
		// respuesta: solo puede pintar lo que le pasaron. Es la garantia estructural detras de "la
		// caratula formatea, no vuelve a derivar".
		[Test]
		public void TheHistorialSectionInjectsNothingAndNamesNoDoor()
		{
			string markup = TheSectionAsItIsWritten();

			Assert.That(markup, Does.Not.Contain("@inject"),
				"La seccion Historial inyecta algo. Una seccion que puede leer puede componer, y "
				+ "entonces lo que la pantalla dice deja de ser lo que el dominio dijo.");

			Assert.That(markup, Does.Not.Contain("DealershipCarAPI"),
				"La seccion Historial nombra la puerta.");

			Assert.That(markup, Does.Not.Contain("@*"),
				"La seccion Historial lleva un comentario de razor, y en este arbol el codigo no lleva "
				+ "prosa fuera de las pruebas.");
		}

		// Y PINTA LAS FRASES DEL DOMINIO Y NINGUNA PROPIA. Es la tercera cosa que ninguna otra prueba
		// ve: todo lo demas se defiende sobre el portador, y una seccion que armara su propia frase con
		// pedazos de el dejaria el portador intacto y la suite entera verde. El barrido de aritmetica
		// tampoco la ve: una suma con un operando de texto es concatenacion y nunca se marca.
		[Test]
		public void TheHistorialSectionPaintsTheDomainsSentencesAndComposesNoneOfItsOwn()
		{
			string markup = TheSectionAsItIsWritten();

			foreach (string sentence in TheDomainsSentencesTheSectionMustQuote)
				Assert.That(markup, Does.Contain(sentence),
					$"La seccion Historial dejo de pintar '{sentence}'. La frase la compone el dominio: "
					+ "una que la caratula arme con pedazos de las otras sale con la procedencia del "
					+ "registro encima de algo que el registro no dijo.");

			foreach (string forbidden in new[]
			{
				"Says +", "+ Says", "Stays.Says +", "+ Stays.Says",
				"TheStayRightNowSays +", "+ TheStayRightNowSays",
			})
				Assert.That(markup, Does.Not.Contain(forbidden),
					$"La seccion concatena algo con una frase del dominio ('{forbidden}'). Lo que sale "
					+ "de la puerta se cita letra por letra o no se cita.");
		}

		// Y LA FRASE DE LA ESTADIA ABIERTA SE PINTA UNA SOLA VEZ Y ADENTRO DE SU RAMA. La lectura por
		// instante contesta TAMBIEN cuando el carro no esta adentro -- "no hay ninguna estadia que
		// cubra el <instante>" -- y esa frase nombra un instante con segundos, que es una respuesta
		// para un modelo y no para una persona. Quien decide si la linea se dibuja es
		// IsInsideTheShopAt, al mismo instante.
		[Test]
		public void TheOpenStaysSentenceIsDrawnOnlyWhenTheCarIsInsideRightNow()
		{
			string markup = TheSectionAsItIsWritten();

			Assert.That(markup, Does.Contain("@if (Stays.InsideRightNow)"),
				"La seccion dejo de preguntar si el carro esta adentro AHORA. Sin esa rama, la frase "
				+ "por instante se pinta tambien cuando no hay ninguna estadia, y dice un instante con "
				+ "segundos que nadie pregunto.");

			Assert.That(TimesItSays(markup, "@Stays.TheStayRightNowSays"), Is.EqualTo(1),
				"La frase de la estadia abierta se pinta una sola vez. Dos sitios son dos ramas, y una "
				+ "de las dos se va a quedar sin la pregunta que la protege.");
		}

		// Las frases del dominio que la seccion tiene que pintar: la del periodo, la de la estadia
		// abierta, y la de cada fila.
		private static readonly string[] TheDomainsSentencesTheSectionMustQuote =
		{
			"@Stays.Says",
			"@Stays.TheStayRightNowSays",
			"@estadia.Says",
		};

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

		private static string ThePageAsItIsWritten()
		{
			string path = Path.Combine(AppContext.BaseDirectory, "MyCar.razor.dato");

			Assert.That(File.Exists(path), Is.True,
				"La pagina no llego al output. La prueba no puede pasar por no haber leido nada.");

			return File.ReadAllText(path);
		}

		private static string TheSectionAsItIsWritten()
		{
			string folder = Path.Combine(AppContext.BaseDirectory, "SeccionesComoDato");

			Assert.That(Directory.Exists(folder), Is.True,
				"Las secciones no llegaron al output.");

			foreach (string file in Directory.GetFiles(folder, "*.razor"))
				if (Path.GetFileName(file) == TheSection) return File.ReadAllText(file);

			Assert.Fail(
				"'" + TheSection + "' no llego al output. Se copia por glob para que una seccion nueva "
				+ "entre sola; si esta no llego, lo que fallo es la copia y no el markup -- y un "
				+ "barrido que leyo cero archivos esta verde por no haber mirado.");

			return "";
		}
	}
}
