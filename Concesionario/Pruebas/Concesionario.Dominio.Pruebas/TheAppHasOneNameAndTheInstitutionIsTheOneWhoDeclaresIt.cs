using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Concesionario.App.Wording;
using Concesionario.Wasm;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// EL NOMBRE DE LA APP ES UNO SOLO Y LO DECLARA LA INSTITUCION. Cinco superficies lo nombran y
	// ninguna deriva de otra: la constante de la caratula, el titulo del manifiesto del modulo, la
	// pestana del navegador, y el nombre y el nombre corto del manifiesto con que el telefono la
	// instala. No hay compilador que las ate y no hay catalogo que las alcance -- las tres ultimas se
	// leen ANTES de que Texts.NegotiateAsync corra -- asi que se separan en silencio.
	//
	// Y YA SE HABIAN SEPARADO: el <title> decia "Mi carro" seguido de la institucion, un
	// nombre que no estaba declarado en ningun lado, mientras el <h1> dos renglones mas abajo
	// -- que si lee la constante -- decia el declarado. Nada se ponia rojo porque un titulo
	// distinto no esta vacio, no lleva llaves y no revienta.
	//
	// POR QUE EL NOMBRE ES VOZ DE LA INSTITUCION Y NO UI DE ESTA CARATULA (regla 4). El nombre y el
	// nombre corto del manifiesto SE HORNEAN AL INSTALAR y quedan bajo el icono de la pantalla de
	// inicio para siempre. Una etiqueta que siguiera el idioma del aparato congelaria un momento:
	// quien instala en ingles y despues pasa a espanol se queda con el rotulo en ingles, y un aparato
	// compartido le muestra uno solo a dos personas. UNA ETIQUETA QUE NO PUEDE SEGUIR A LA PERSONA NO
	// DEBE APARENTAR QUE LO HACE, y el unico dueno que queda es la institucion. De ahi sale lo que
	// afirma la ultima prueba: 'lang' dice el idioma del REGISTRO, y hay un solo manifiesto.
	//
	// SE AFIRMA CONTRA LITERALES Y NO UNA SUPERFICIE CONTRA OTRA A SECAS. Dos declaraciones
	// vacias son iguales entre si, asi que una igualdad sola se cumple tambien con las dos
	// borradas.
	//
	// LO QUE ESTA PRUEBA NO VE, dicho aca y no descubierto despues:
	//
	//   1. UN SHIM QUE REESCRIBA document.title DESPUES DE ARRANCAR. Se lee el archivo servido
	//      y no el DOM, asi que un .js que cambie el titulo pasa entero. Es otra ley -- que
	//      exista un shim -- y no esta escrita en ningun lado todavia.
	//   2. LA PAGINA DE DESCARGA, QUE ES UN CUARTO NOMBRE Y ESTA EN INGLES A PROPOSITO.
	//      PaginaDeDescarga/descarga/index.html dice "Your maintenance schedule" seguido de la
	//      institucion, con <html lang="en">. Es otra superficie y de otro dueno -- la vitrina, que
	//      elige en que idioma la escribe (regla 1) -- asi que queda AFUERA a proposito y no por
	//      descuido: meterla aqui pondria esta prueba roja contra una decision que no es de esta
	//      caratula.
	[TestFixture]
	public sealed class TheAppHasOneNameAndTheInstitutionIsTheOneWhoDeclaresIt
	{
		// EL NOMBRE DECLARADO, tal cual, y el literal contra el que se mide todo lo de abajo.
		private const string TheNameTheInstitutionDeclares = "Mi plan de mantenimiento";

		// EL NOMBRE ENTERO, que es el declarado seguido de quien lo publica. La institucion se escribe
		// aqui como la escriben las dos superficies que la nombran, y NO se compara contra el
		// manifiesto del modulo: ahi va con su parentesis de mock, que es otra cosa.
		private const string TheWholeName = "Mi plan de mantenimiento · Sotaventos Motors";

		// EL QUE CABE DEBAJO DEL ICONO. Es una abreviatura del declarado y no un nombre aparte, que es
		// lo que afirma la relacion de la segunda prueba.
		private const string TheNameUnderTheIcon = "Mi plan";

		private const string TheOnlyManifestThereIs = "manifest.webmanifest";

		private const string TheModulesManifest = "manifiesto.json.dato";

		private const string TheDocumentServed = "index.html.dato";

		private const string WhereTheManifestsAre = "ManifiestoComoDato";

		private static readonly Regex TheTitleElement = new Regex(@"<title>(.*?)</title>");

		// ===== LAS DOS DECLARACIONES, QUE NO SE TOCAN ENTRE SI ============================

		// EL TITULO SE DECLARA DOS VECES Y NADA LAS ATA. Una la lee el <h1> de la pantalla, la otra la
		// lee Gnothi para armar su catalogo sin arrancar el modulo. Son dos copias a mano, asi que la
		// igualdad entre ellas SI es evidencia -- ninguna deriva de la otra -- y el literal esta para
		// que borrar las dos no cumpla la igualdad por vacio.
		[Test]
		public void TheTitleIsDeclaredTwiceAndBothDeclarationsSayTheSameWords()
		{
			Assert.That(Texts.TheModuleTitle, Is.EqualTo(TheNameTheInstitutionDeclares),
				"La constante que pinta el <h1> de la pantalla dejo de decir el nombre declarado. No se "
				+ "traduce y no sale de ningun catalogo: es voz de la institucion.");

			Assert.That(TheKeyOfTheModulesManifest("titulo"), Is.EqualTo(TheNameTheInstitutionDeclares),
				"El manifiesto del modulo dejo de decir el mismo nombre que la pantalla. Gnothi arma su "
				+ "catalogo con este titulo SIN arrancar el modulo, asi que la lista de la persona y la "
				+ "pantalla que abre desde ahi dirian dos nombres distintos.");
		}

		// ===== TODA SUPERFICIE QUE NOMBRA LA APP CITA LA DECLARACION ======================

		// EL CENSO ENTERO POR VALOR, Y LAS TRES RELACIONES QUE LO HACEN UN NOMBRE Y NO CINCO. Sin las
		// relaciones el censo es una lista de cinco cadenas que alguien puede cambiar de a una; sin el
		// censo, las relaciones se cumplen con cinco nombres nuevos que empiecen igual.
		[Test]
		public void EverySurfaceThatNamesTheAppQuotesTheNameTheInstitutionDeclared()
		{
			var expected = new SortedDictionary<string, string>(StringComparer.Ordinal)
			{
				["Texts.TheModuleTitle"] = TheNameTheInstitutionDeclares,
				["index.html <title>"] = TheWholeName,
				["manifest.webmanifest name"] = TheWholeName,
				["manifest.webmanifest short_name"] = TheNameUnderTheIcon,
				["manifiesto.json titulo"] = TheNameTheInstitutionDeclares,
			};

			Assert.That(InWords(TheNamesTheAppIsKnownBy()), Is.EqualTo(InWords(expected)),
				"Las superficies que nombran la app dejaron de decir lo mismo. Una superficie nueva se "
				+ "mira a mano: el nombre es voz de la institucion, y quien agregue una tiene que "
				+ "CITAR el declarado en vez de escribir uno.");

			// Y LAS TRES RELACIONES. La primera es la que faltaba cuando el <title> decia otra cosa.
			foreach (string said in new[] { TheTabTitle(), TheKeyOfTheAppsManifest("name") })
				Assert.That(said, Does.StartWith(Texts.TheModuleTitle),
					"'" + said + "' no empieza por el nombre declarado, asi que es un nombre nuevo y no "
					+ "una cita del que la institucion publica.");

			Assert.That(TheTabTitle(), Is.EqualTo(TheKeyOfTheAppsManifest("name")),
				"La pestana del navegador y el manifiesto con que el telefono instala dicen dos cosas "
				+ "distintas. Son la misma app vista desde dos lados y la persona las ve juntas: el "
				+ "aviso de instalar sale encima de la pestana que lo pide.");

			Assert.That(Texts.TheModuleTitle, Does.StartWith(TheKeyOfTheAppsManifest("short_name")),
				"El nombre corto dejo de ser una abreviatura del declarado. Es el rotulo de abajo del "
				+ "icono, y abreviar lo que la institucion publica sigue siendo citarla; inventar uno "
				+ "aparte no.");
		}

		// EL CONTROL DEL EXTRACTOR. Sin estos tres renglones, un extractor que devolviera siempre lo
		// mismo -- o siempre nada -- dejaria el censo de arriba verde por no haber leido el documento.
		[Test]
		public void TheSweepSeesATitleThatDoesNotQuoteTheDeclarationAndSeesOneThatDoes()
		{
			Assert.That(
				TheTitleIn("<head><title>Mi carro · Sotaventos Motors</title></head>"),
				Is.EqualTo("Mi carro · Sotaventos Motors"),
				"El extractor no lee el titulo del documento, asi que el censo de arriba esta verde por "
				+ "no haber mirado. Ese es el nombre que el <title> decia de verdad.");

			Assert.That(
				TheTitleIn("<head><title>" + TheWholeName + "</title></head>"),
				Is.EqualTo(TheWholeName),
				"El extractor da por distinto un titulo que SI cita la declaracion.");

			// Y UN DOCUMENTO SIN <title> DEVUELVE VACIO, que es lo que hace que borrarlo se ponga rojo:
			// el vacio no empieza por el nombre declarado y la relacion de arriba lo ve.
			Assert.That(
				TheTitleIn("<head><base href=\"/app/\" /></head>"), Is.Empty,
				"Un documento sin <title> devuelve algo, asi que un <title> borrado pasaria por uno "
				+ "correcto.");
		}

		// ===== EL MANIFIESTO NO SIGUE A LA PERSONA, Y ESO SE DECLARA ======================

		// LA DECISION, ESCRITA COMO PRUEBA. El manifiesto se hornea al instalar, asi que no puede
		// seguir a la persona; por eso sus palabras son del idioma del registro y hay UNO solo. Un
		// segundo manifiesto por idioma es revertir esto, y esta prueba es donde alguien lo vuelve a
		// decidir a proposito en vez de agregarlo sin que nadie mire.
		[Test]
		public void TheManifestOfTheAppDeclaresTheLanguageOfTheRecordBecauseItDoesNotFollowThePerson()
		{
			Assert.That(TheKeyOfTheAppsManifest("lang"), Is.EqualTo(Phrases.LanguageOfTheRecord),
				"El manifiesto dejo de declarar el idioma del registro. Sus palabras son voz de la "
				+ "institucion y no siguen a quien mira, asi que la etiqueta que las describe es la del "
				+ "registro y no la del aparato.");

			Assert.That(TheManifestsOfTheApp(), Is.EqualTo(new[] { TheOnlyManifestThereIs }),
				"Los manifiestos de la caratula cambiaron. Uno por idioma seria hacer que el rotulo de "
				+ "la pantalla de inicio siga a la persona, y ese rotulo se congela al instalar: quien "
				+ "instale en un idioma y despues cambie se queda con el viejo para siempre.");
		}

		// ===== el instrumento: que dice cada superficie ===================================

		// CADA SUPERFICIE CON LO QUE DICE. Las cinco se leen de su propio archivo o de su propia
		// constante, nunca una de otra: dos superficies que se leyeran del mismo lado estarian de
		// acuerdo tambien con ese lado equivocado.
		private static SortedDictionary<string, string> TheNamesTheAppIsKnownBy()
		{
			return new SortedDictionary<string, string>(StringComparer.Ordinal)
			{
				["Texts.TheModuleTitle"] = Texts.TheModuleTitle,
				["index.html <title>"] = TheTabTitle(),
				["manifest.webmanifest name"] = TheKeyOfTheAppsManifest("name"),
				["manifest.webmanifest short_name"] = TheKeyOfTheAppsManifest("short_name"),
				["manifiesto.json titulo"] = TheKeyOfTheModulesManifest("titulo"),
			};
		}

		private static string TheTabTitle()
		{
			return TheTitleIn(TheFileAsData(TheDocumentServed));
		}

		private static string TheTitleIn(string document)
		{
			Match found = TheTitleElement.Match(document);

			return found.Success ? found.Groups[1].Value : "";
		}

		private static string TheKeyOfTheAppsManifest(string key)
		{
			return TheKeyOf(
				TheFileAsData(Path.Combine(WhereTheManifestsAre, TheOnlyManifestThereIs)), key);
		}

		private static string TheKeyOfTheModulesManifest(string key)
		{
			return TheKeyOf(TheFileAsData(TheModulesManifest), key);
		}

		private static string TheKeyOf(string json, string key)
		{
			using JsonDocument document = JsonDocument.Parse(json);

			Assert.That(document.RootElement.TryGetProperty(key, out JsonElement said), Is.True,
				"'" + key + "' no esta en el manifiesto, asi que la superficie que lo dice se quedo sin "
				+ "nombre y nadie se entera hasta que alguien la instala.");

			return said.GetString();
		}

		// SIN NINGUN catch y con la carpeta afirmada. Un censo que leyo cero archivos esta verde por no
		// haber mirado.
		private static string[] TheManifestsOfTheApp()
		{
			string folder = Path.Combine(AppContext.BaseDirectory, WhereTheManifestsAre);

			Assert.That(Directory.Exists(folder), Is.True,
				"El manifiesto de la caratula no llego al output, asi que esta prueba no afirma nada.");

			string[] files = Directory.GetFiles(folder, "*.webmanifest");

			Assert.That(files, Is.Not.Empty, "No llego ningun manifiesto al output.");

			var names = new List<string>();

			foreach (string each in files) names.Add(Path.GetFileName(each));

			names.Sort(StringComparer.Ordinal);

			return names.ToArray();
		}

		private static string TheFileAsData(string inside)
		{
			string path = Path.Combine(AppContext.BaseDirectory, inside);

			Assert.That(File.Exists(path), Is.True,
				inside + ": no viajo al output, asi que esta prueba no esta afirmando nada.");

			return File.ReadAllText(path);
		}

		private static string InWords(SortedDictionary<string, string> names)
		{
			var said = new StringBuilder();

			foreach (KeyValuePair<string, string> each in names)
				said.Append(each.Key).Append(" dice ").Append(each.Value).Append('\n');

			return said.ToString();
		}
	}
}
