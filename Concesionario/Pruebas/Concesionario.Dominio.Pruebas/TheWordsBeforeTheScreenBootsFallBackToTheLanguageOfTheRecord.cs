using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LO QUE UNA PERSONA LEE ANTES DE QUE HAYA PANTALLA. Dos textos se renderizan antes de que
	// Texts.NegotiateAsync corra y ningun catalogo los alcanza donde estan escritos: la palabra que
	// acompana la carga, que es un content: de CSS, y el aviso de falla, que esta en el documento
	// servido. Los dos son UI propia de la caratula -- regla 2, categoria 1 -- y no voz de la
	// institucion: son las palabras que esta app le dice a la persona, no las que el concesionario
	// declara. El NOMBRE de la app si es voz, y lo fija otra fixture.
	//
	// Y NO SE LOCALIZAN IGUAL, porque no son alcanzables igual:
	//
	//   EL AVISO SI SE ALCANZA. app.css tiene #blazor-error-ui { display: none }, asi que su texto
	//   esta en el DOM desde el primer byte y no se VE hasta que el arranque de Blazor lo muestra,
	//   que es despues de negociar. De ahi que llegue por language-interop.js, que ya existia: Texts
	//   le escribe las dos palabras del catalogo cuando ya hay idioma. No hace falta mecanismo nuevo.
	//
	//   LA PALABRA DE LA CARGA NO. Es el fallback de var(--blazor-load-percentage-text) y lo resuelve
	//   el browser sin que corra nada nuestro, asi que es UN idioma por construccion. Decia
	//   "Loading", que no lo eligio nadie: es el default de la plantilla de Blazor. El idioma que
	//   le toca es el del registro, que es a lo que esta app ya cae cuando no puede atender la
	//   eleccion de la persona -- Texts lo dice con todas las letras, y media pantalla en un idioma
	//   que nadie eligio es peor que una entera en el de la institucion. No se deja vacia: importa
	//   justamente cuando el arranque se traba y el porcentaje no llega nunca.
	//
	// POR ESO LA PALABRA DE LA CARGA NO TIENE CLAVE DE CATALOGO. Una clave que ningun codigo lee se
	// traduce y no pasa nada, que es peor que no tenerla. Se afirma contra el literal, y este
	// comentario es donde se dice por que hay uno solo.
	//
	// EL FALLBACK DEL AVISO ES ESE MISMO IDIOMA, Y SE AFIRMA. Lo que el documento trae escrito es lo
	// unico que se ve si el arranque falla antes de negociar, o antes de llegar a Texts, asi que
	// tiene que decir lo que dice el catalogo del registro o no lo dice nadie.
	//
	// C70 SIGUE ABIERTO A PROPOSITO Y ESTO NO LO TOCA. El aviso aparece igual, en los mismos casos y
	// por la misma razon; lo unico que cambia son las palabras. Lo que C70 afirma y nadie verifico es
	// en cual stream sale el rechazo, y eso no se lee en ninguno de estos archivos.
	//
	// LO QUE ESTA FIXTURE NO VE, dicho aca y no descubierto despues:
	//
	//   1. EL DOM. Se leen los archivos servidos, no la pagina. Un shim que corra y no escriba nada,
	//      o un elemento que Blazor nunca muestre, pasan enteros. Lo que si se ve es que el nombre
	//      que Texts invoca sea uno que el modulo declara, que es como este shim se rompe en
	//      silencio: Texts se traga la JSException y el aviso se queda en el idioma del registro.
	//   2. DONDE esta el llamado. Se afirma que el renglon existe en el archivo, no que siga adentro
	//      de NegotiateAsync: moverlo a un metodo que nadie llama pasa entero.
	//   3. QUE LAS PALABRAS SEAN LAS CORRECTAS PARA QUIEN MIRA. Eso depende de que la negociacion
	//      haya ocurrido, y no hay banco de pruebas de browser en este repo.
	[TestFixture]
	public sealed class TheWordsBeforeTheScreenBootsFallBackToTheLanguageOfTheRecord
	{
		// LA PALABRA DE LA CARGA, tal cual, y el unico literal que la sostiene.
		private const string TheWordWhileItLoads = "Cargando";

		// EL DEFAULT DE LA PLANTILLA, que es lo que decia y lo que no debe volver a decir.
		private const string TheFrameworksOwnWord = "Loading";

		private const string TheShim = "./language-interop.js";

		private const string TheNameThatWritesTheBanner = "setTheWordsOfTheFailureBanner";

		private const string TheCallThatWritesTheBanner =
			"await SetTheWordsOfTheFailureBannerAsync(js);";

		private const string TheDocumentServed = "index.html.dato";

		private const string TheStyleSheet = "app.css.dato";

		private const string TheShimAsData = "language-interop.js.dato";

		private const string TheScreensWords = "Texts.cs.dato";

		private static readonly Regex WhatTheBannerSays =
			new Regex(@"<span class=""said"">(.*?)</span>");

		private static readonly Regex WhatItSaysToReload =
			new Regex(@"<a[^>]*class=""reload""[^>]*>(.*?)</a>");

		private static readonly Regex TheWordTheLoaderFallsBackTo =
			new Regex(@"--blazor-load-percentage-text\s*,\s*""(.*?)""\s*\)");

		private static readonly Regex AModuleTheScreenImports =
			new Regex(@"""import""\s*,\s*""([^""]+)""");

		private static readonly Regex ANameInvokedOnTheModule =
			new Regex(@"module\.Invoke(?:Void)?Async(?:<[^>]*>)?\(\s*""([^""]+)""");

		private static readonly Regex ANameTheShimDeclares = new Regex(@"export\s+function\s+(\w+)");

		// ===== EL AVISO: EL DOCUMENTO Y EL CATALOGO DEL REGISTRO DICEN LO MISMO ===========

		[Test]
		public void TheBannerTheDocumentCarriesSaysWhatTheCatalogueOfTheRecordSays()
		{
			string document = TheFileAsData(TheDocumentServed);

			Assert.Multiple(() =>
			{
				Assert.That(TheOneIn(WhatTheBannerSays, document),
					Is.EqualTo(TheCatalogueOfTheScreen.InSpanish("shell.wentWrong")),
					"El aviso que trae el documento dejo de decir lo que dice el catalogo del registro. "
					+ "Es lo unico que se lee si el arranque falla antes de negociar, asi que ahi la "
					+ "persona veria una frase que no esta en ningun catalogo.");

				Assert.That(TheOneIn(WhatItSaysToReload, document),
					Is.EqualTo(TheCatalogueOfTheScreen.InSpanish("shell.reload")),
					"El enlace de recargar que trae el documento dejo de decir lo que dice el catalogo "
					+ "del registro, y es la unica salida que el aviso le ofrece a la persona.");
			});
		}

		// ===== LA PALABRA DE LA CARGA: UN SOLO IDIOMA, Y ES EL DEL REGISTRO ===============

		[Test]
		public void TheWordWhileItLoadsIsTheLanguageOfTheRecordAndNotTheFrameworksDefault()
		{
			string word = TheOneIn(TheWordTheLoaderFallsBackTo, TheFileAsData(TheStyleSheet));

			Assert.Multiple(() =>
			{
				Assert.That(word, Is.Not.EqualTo(TheFrameworksOwnWord),
					"La palabra de la carga volvio al default de la plantilla de Blazor, que esta en "
					+ "ingles y no lo eligio nadie. Es el unico texto de esta app que no puede negociar, "
					+ "asi que le toca el idioma al que la app ya cae: el del registro.");

				Assert.That(word, Is.EqualTo(TheWordWhileItLoads),
					"La palabra de la carga cambio. La escribe el browser desde un content: de CSS, sin "
					+ "que corra nada nuestro, asi que no tiene clave de catalogo y este literal es lo "
					+ "unico que la sostiene.");
			});
		}

		// ===== EL SHIM NO SE PUEDE ROMPER EN SILENCIO ====================================

		// EL UNICO MECANISMO NUEVO, Y SU FALLA ES MUDA. Se afirma primero que la pantalla importa UN
		// solo modulo, que es lo que hace que todo nombre invocado sobre 'module' sea de ese archivo.
		[Test]
		public void EveryNameTheScreenInvokesOnTheShimIsOneTheShimDeclares()
		{
			string words = TheFileAsData(TheScreensWords);
			string[] invoked = TheOnesIn(ANameInvokedOnTheModule, words);
			string[] declared = TheOnesIn(ANameTheShimDeclares, TheFileAsData(TheShimAsData));

			Assert.Multiple(() =>
			{
				Assert.That(TheOnesIn(AModuleTheScreenImports, words), Is.EqualTo(new[] { TheShim }),
					"La pantalla dejo de importar un solo modulo de JS, asi que un nombre invocado sobre "
					+ "'module' ya no se puede atribuir a este archivo y el resto de esta prueba mide "
					+ "otra cosa.");

				Assert.That(invoked, Does.Contain(TheNameThatWritesTheBanner),
					"La pantalla dejo de escribirle sus palabras al aviso de falla, asi que el aviso se "
					+ "queda en el idioma del registro para quien lea en el otro.");

				foreach (string name in invoked)
					Assert.That(declared, Does.Contain(name),
						"'" + name + "': la pantalla lo invoca y el modulo no lo declara. La llamada "
						+ "revienta con una JSException que Texts se traga, asi que no se ve al compilar "
						+ "ni en pantalla: el texto se queda como estaba, para siempre y sin avisar.");
			});
		}

		// EL LLAMADO ES OTRA COSA QUE EL NOMBRE, Y ESTA MEDIDO. Con el renglon borrado y el metodo en
		// pie, todo lo de arriba se queda verde: los nombres calzan, el modulo los declara, y el aviso
		// no se escribe nunca. Se afirma que el renglon esta, no DONDE esta: que siga adentro de
		// NegotiateAsync es de lo que esta fixture no ve.
		[Test]
		public void NegotiatingTheLanguageIsWhatWritesTheWordsOfTheBanner()
		{
			Assert.That(TheFileAsData(TheScreensWords), Does.Contain(TheCallThatWritesTheBanner),
				"La pantalla todavia declara el metodo que le escribe sus palabras al aviso y ya no lo "
				+ "llama, asi que el aviso se queda en el idioma del registro para quien lea en el otro "
				+ "y ninguna otra prueba lo ve.");
		}

		// ===== EL CONTROL DE LOS EXTRACTORES =============================================

		// SIN ESTO, UN EXTRACTOR QUE NO LEYERA NADA DEJA TODO LO DE ARRIBA VERDE. Cada patron se mide
		// contra un texto plantado -- que a proposito no es el que se embarca -- y contra uno donde lo
		// que busca no esta. El del CSS planta justo la regresion: el default de la plantilla.
		[Test]
		public void EachSweepSeesWhatItLooksForAndComesBackEmptyWhenItIsNotThere()
		{
			Assert.Multiple(() =>
			{
				Assert.That(
					TheOneIn(WhatTheBannerSays, "<span class=\"said\">lo que el documento diga</span>"),
					Is.EqualTo("lo que el documento diga"),
					"El extractor no lee el aviso del documento, asi que el censo de arriba esta verde "
					+ "por no haber mirado.");
				Assert.That(TheOneIn(WhatTheBannerSays, "<div id=\"blazor-error-ui\"></div>"), Is.Empty,
					"Un aviso sin su texto devuelve algo, asi que borrarlo pasaria por correcto.");

				Assert.That(
					TheOneIn(WhatItSaysToReload, "<a href=\".\" class=\"reload\">lo que diga el enlace</a>"),
					Is.EqualTo("lo que diga el enlace"));
				Assert.That(TheOneIn(WhatItSaysToReload, "<a href=\".\">sin su clase</a>"), Is.Empty);

				Assert.That(
					TheOneIn(TheWordTheLoaderFallsBackTo,
						"content: var(--blazor-load-percentage-text, \"" + TheFrameworksOwnWord + "\");"),
					Is.EqualTo(TheFrameworksOwnWord),
					"El extractor no ve el default de la plantilla, que es la regresion que la prueba "
					+ "de arriba existe para atrapar.");
				Assert.That(
					TheOneIn(TheWordTheLoaderFallsBackTo, "content: var(--blazor-load-percentage-text);"),
					Is.Empty, "Una declaracion sin fallback devuelve algo.");

				Assert.That(
					TheOnesIn(AModuleTheScreenImports, "js.InvokeAsync<T>(\"import\", \"./otro.js\")"),
					Is.EqualTo(new[] { "./otro.js" }));

				Assert.That(
					TheOnesIn(ANameInvokedOnTheModule, "module.InvokeVoidAsync(\"unNombre\", tag)"),
					Is.EqualTo(new[] { "unNombre" }));
				Assert.That(
					TheOnesIn(ANameInvokedOnTheModule, "js.InvokeAsync<T>(\"import\", \"./x.js\")"),
					Is.Empty,
					"El patron cuenta el 'import' como un nombre invocado, asi que pediria que el modulo "
					+ "lo declarara y la prueba de arriba estaria roja por su propio patron.");

				Assert.That(TheOnesIn(ANameTheShimDeclares, "export function unExport(a) { }"),
					Is.EqualTo(new[] { "unExport" }));
				Assert.That(TheOnesIn(ANameTheShimDeclares, "function noExportada(a) { }"), Is.Empty);
			});
		}

		// ===== el instrumento ============================================================

		private static string TheOneIn(Regex sweep, string text)
		{
			Match found = sweep.Match(text);

			return found.Success ? found.Groups[1].Value : "";
		}

		private static string[] TheOnesIn(Regex sweep, string text)
		{
			return sweep.Matches(text)
				.Select(one => one.Groups[1].Value)
				.Distinct(StringComparer.Ordinal)
				.OrderBy(one => one, StringComparer.Ordinal)
				.ToArray();
		}

		private static string TheFileAsData(string inside)
		{
			string path = Path.Combine(AppContext.BaseDirectory, inside);

			Assert.That(File.Exists(path), Is.True,
				inside + ": no viajo al output, asi que esta prueba no esta afirmando nada.");

			return File.ReadAllText(path);
		}
	}
}
