using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// QUE ES UN RECHAZO DEL DOMINIO, Y EN QUE IDIOMA HABLA. Casi todo el espanol que le queda a
	// Dominio/ esta adentro de un throw new DomainException, y la regla 2 de la guia de localizacion
	// no tiene casilla para el: no es UI de la caratula, no es dato del journal -- nadie lo asento
	// --, no es voz de la institucion -- nadie lo declaro -- y no es una frase que el dominio
	// COMPONE como respuesta, porque no es una respuesta: es el dominio diciendo que no.
	//
	// LA CASILLA FALTA PORQUE LAS CUATRO CLASIFICAN TEXTO QUE ALGUIEN LEE COMO RESPUESTA, Y ESTO NO
	// LO ES. El propio DomainException lo dice: "algo que el negocio dice que nunca puede pasar, y
	// que si paso es un defecto de quien llama". Su destinatario es quien mantiene el modulo, igual
	// que un ///, y por eso esta en el idioma en que se mantiene el modulo. No se traduce: se
	// mantiene fuera de la respuesta.
	//
	// Y ESO NO ES UNA DECISION NUEVA: WhereEachOperationStands ya la tomo para una sola lectura, en
	// el /// de RefusalSays -- "NO va a la pantalla ... es una respuesta para quien programa y no
	// para quien maneja". Lo que hace esta fixture es fijarla, porque hasta ahora no habia nada rojo
	// si alguien pintaba ese campo.
	//
	// LAS TRES CARAS, LEIDAS Y NO SUPUESTAS:
	//
	//   LA CARA DE LA PERSONA contesta un rechazo con frases de su catalogo -- la etiqueta de la fila
	//   rechazada y la frase de a quien le toca arreglarlo -- y el mensaje del dominio no llega ahi.
	//   Donde SI llega es al detalle de diagnostico, que es el mismo sitio donde caen un fallo de
	//   disco y uno del navegador: ese catch no distingue un rechazo del dominio de nada, y no tiene
	//   por que. Lo que faltaba era decirle a la persona QUE es lo que esta leyendo, en su idioma, y
	//   eso es page.detailIsTheAppsOwnReport.
	//
	//   LA CARA DEL ASISTENTE habla el idioma del REGISTRO a proposito -- regla 4: la caratula MCP es
	//   voz de la institucion hablandole al modelo -- asi que ahi el mensaje del dominio ya esta
	//   en el idioma que le toca y no hay nada que hacer.
	//
	//   LA CONSOLA es el arnes del dominio y su lector es quien mide. Fuera de alcance por lo mismo.
	//
	// LO QUE ESTA FIXTURE NO VE, dicho aca y no descubierto despues:
	//
	//   1. EL DOM. Se lee el markup servido, no la pagina. Que la etiqueta se pinte de verdad depende
	//      de que el bloque del detalle se abra, y no hay banco de pruebas de browser en este repo.
	//   2. QUE LA ETIQUETA SEA LA CORRECTA PARA QUIEN MIRA. Eso depende de que la negociacion haya
	//      ocurrido, que es lo mismo que no ve la fixture del cascaron.
	//   3. QUE LOS 311 MENSAJES DEL DOMINIO DIGAN LO QUE DEBEN. Esto afirma DONDE llegan y COMO se
	//      enmarcan, no que cada uno este bien redactado.
	//   4. UNA CARA NUEVA. El censo de quien reconoce el rechazo se mide sobre las raices que el
	//      csproj ya manda como dato; una raiz nueva entra sola al glob, pero una cara que reconozca
	//      el rechazo de otra manera -- por un codigo, por una propiedad -- no la ve este patron.
	[TestFixture]
	public sealed class ADomainRefusalIsDiagnosticsAndNeverTheAnswerAPersonReads
	{
		// LA CLAVE QUE LE DICE A LA PERSONA QUE ESTA LEYENDO DIAGNOSTICO y no una respuesta sobre su
		// carro. Es lo unico que esta etapa agrega a la pantalla.
		private const string TheLabelOfTheDetail = "page.detailIsTheAppsOwnReport";

		// EL CAMPO QUE LLEVA EL MENSAJE DEL DOMINIO dentro del portador de la caratula, y que su
		// propio /// declara que no va a la pantalla.
		private const string TheFieldThatCarriesTheRefusal = "RefusalSays";

		private const string ThePageServed = "MyCar.razor.dato";

		// COMO SE RECONOCE UN RECHAZO DEL DOMINIO DESDE AFUERA: por el NOMBRE del tipo, porque la
		// excepcion es interna a su ensamblado y ninguna cara puede nombrarla.
		private static readonly Regex TheNameOfTheDomainsRefusal =
			new Regex(@"""(?:[\w.]*\.)?DomainException""");

		private static readonly Regex AMessageBeingRead = new Regex(@"\w+\.Message\b");

		// EL CAMPO NOMBRADO, NO EL CAMPO PINTADO, y es a proposito mas estricto: un markup lo puede
		// alcanzar por @x.RefusalSays y tambien por @(x.RefusalSays), y un patron que solo conociera
		// la primera forma dejaria la segunda pasar entera. Que un .razor no lo nombre en absoluto no
		// cuesta nada: la fila rechazada ya contesta con su catalogo.
		private static readonly Regex TheFieldBeingNamed = new Regex(@"(?<![\w])RefusalSays\b");

		// ===== LA CARA DE LA PERSONA: EL DETALLE ESTA ETIQUETADO =========================

		[Test]
		public void TheDetailThatCarriesARefusalSaysWhatItIsInThePersonsLanguage()
		{
			string page = TheFileAsData(ThePageServed);

			Assert.Multiple(() =>
			{
				Assert.That(page, Does.Contain("texts[\"" + TheLabelOfTheDetail + "\"]"),
					"La pantalla dejo de decirle a la persona que el detalle es el reporte de la app. "
					+ "Ahi adentro cae el mensaje del dominio, que esta en el idioma del registro, asi "
					+ "que quien lea en el otro idioma recibe una frase que nadie le explico de donde "
					+ "salio.");

				Assert.That(TheLineOf(page, TheLabelOfTheDetail),
					Is.LessThan(TheLineOf(page, "<span class=\"detail\">@error</span>")),
					"La etiqueta dejo de ir ANTES del detalle, asi que la persona lee el reporte crudo "
					+ "primero y la explicacion despues, que es cuando ya no sirve.");
			});
		}

		// ===== LA ETIQUETA ESTA EN LOS DOS IDIOMAS Y NO ES LA MISMA FRASE =================

		// LA PARIDAD DE CLAVES YA LA AFIRMA TheTwoScreenCataloguesCarryExactlyTheSameKeys, asi que no
		// se repite. Lo que ese censo NO ve es una clave copiada SIN traducir: esta en los dos
		// archivos, con el mismo texto, y la paridad queda verde.
		[Test]
		public void TheLabelIsWrittenInBothLanguagesAndIsNotTheSameSentenceInThem()
		{
			string said = TheCatalogueOfTheScreen.InSpanish(TheLabelOfTheDetail);
			string alsoSaid = TheCatalogueOfTheScreen.InEnglish(TheLabelOfTheDetail);

			Assert.Multiple(() =>
			{
				Assert.That(said, Is.Not.Empty,
					"La etiqueta del detalle quedo sin palabras en el catalogo del registro.");

				Assert.That(alsoSaid, Is.Not.Empty,
					"La etiqueta del detalle quedo sin palabras en el otro idioma.");

				Assert.That(alsoSaid, Is.Not.EqualTo(said),
					"La etiqueta dice lo mismo en los dos catalogos, asi que se copio sin traducir. La "
					+ "paridad de claves no lo ve: la clave esta en los dos archivos.");
			});
		}

		// ===== NINGUNA CARA CONTESTA CON EL MENSAJE DEL DOMINIO ===========================

		// EL CENSO ES SOBRE QUIEN RECONOCE EL RECHAZO POR EL NOMBRE DEL TIPO. Quien lo reconoce puede
		// contestarle a alguien, y lo que se afirma es que no contesta CON EL MENSAJE: la cara de la
		// persona lo cambia por frases de su catalogo, la del asistente por frases suyas. La puerta
		// queda afuera a proposito y se nombra: ella SI lee el mensaje, porque lo unico que hace con
		// el es envolverlo en un tipo que quien lee pueda nombrar.
		[Test]
		public void NoFaceThatRecognisesTheDomainsRefusalAnswersWithItsMessage()
		{
			string[] faces = { TheCodeOfTheShellsAsData.Wasm, TheCodeOfTheShellsAsData.Mcp };

			List<string> recognisers = new List<string>();

			foreach (string face in faces)
				foreach (string file in TheCodeOfTheShellsAsData.EveryFileOf(face))
				{
					string source = File.ReadAllText(file);

					if (!TheNameOfTheDomainsRefusal.IsMatch(source)) continue;

					recognisers.Add(face + "/" + TheCodeOfTheShellsAsData.InsideItsRoot(face, file));

					Assert.That(AMessageBeingRead.IsMatch(source), Is.False,
						TheCodeOfTheShellsAsData.InsideItsRoot(face, file) + ": reconoce el rechazo del "
						+ "dominio por el nombre del tipo y ademas lee su mensaje. Quien reconoce el "
						+ "rechazo esta contestando, y una respuesta lleva las palabras de su propio "
						+ "catalogo: el mensaje del dominio es diagnostico y esta en el idioma en que "
						+ "se mantiene el modulo.");
				}

			Assert.That(recognisers, Is.Not.Empty,
				"Ninguna cara reconoce ya el rechazo del dominio por el nombre del tipo, asi que esta "
				+ "prueba esta verde por no haber encontrado a nadie. O el reconocimiento cambio de "
				+ "forma, o un catch de Exception a secas volvio a confundir un registro contradictorio "
				+ "con un disco que fallo.");
		}

		// ===== EL MENSAJE DEL DOMINIO NO SE PINTA ========================================

		// LA DECISION QUE YA ESTABA TOMADA EN UN ///, Y QUE NADA PONIA ROJA. El portador de la
		// caratula guarda el rechazo en un campo para dejar constancia de que la fila es un rechazo y
		// no una ausencia; la pantalla lee el BOOLEANO de al lado y contesta con su catalogo.
		[Test]
		public void TheScreenNeverPaintsTheFieldThatCarriesTheRefusal()
		{
			var painted = new List<string>();
			int markupRead = 0;

			foreach (string file in TheCodeOfTheShellsAsData.EveryFileOf(TheCodeOfTheShellsAsData.Wasm))
			{
				if (!file.EndsWith(".razor", StringComparison.Ordinal)) continue;

				markupRead++;

				if (TheFieldBeingNamed.IsMatch(File.ReadAllText(file)))
					painted.Add(TheCodeOfTheShellsAsData.InsideItsRoot(
						TheCodeOfTheShellsAsData.Wasm, file));
			}

			Assert.That(markupRead, Is.GreaterThan(0),
				"No llego ningun .razor de la caratula al output, asi que este barrido esta verde por "
				+ "no haber leido nada.");

			Assert.That(painted, Is.Empty,
				"El markup pinta el campo que lleva el rechazo del dominio: "
				+ string.Join(", ", painted.Distinct(StringComparer.Ordinal))
				+ ". Ese texto nombra la clase por el nombre del canon y el instante con siete "
				+ "decimales, y esta en el idioma del registro: es una respuesta para quien programa. "
				+ "La fila rechazada ya tiene su frase en el catalogo.");
		}

		// ===== EL CONTROL DE LOS EXTRACTORES =============================================

		// SIN ESTO, UN PATRON QUE NO LEYERA NADA DEJA TODO LO DE ARRIBA VERDE. Cada uno se mide contra
		// un texto plantado -- que a proposito no es el que se embarca -- y contra uno donde lo que
		// busca no esta.
		[Test]
		public void EachSweepSeesWhatItLooksForAndComesBackEmptyWhenItIsNotThere()
		{
			Assert.Multiple(() =>
			{
				Assert.That(
					TheNameOfTheDomainsRefusal.IsMatch("thrown.GetType().Name == \"DomainException\""),
					Is.True,
					"El patron no ve el reconocimiento por nombre corto, asi que el censo de arriba "
					+ "esta verde por no haber mirado.");
				Assert.That(
					TheNameOfTheDomainsRefusal.IsMatch("= \"Concesionario.Dominio.DomainException\""),
					Is.True, "El patron no ve el reconocimiento por nombre completo.");
				Assert.That(TheNameOfTheDomainsRefusal.IsMatch("catch (InvalidOperationException x)"),
					Is.False, "El patron marca un catch que no es el del dominio.");

				Assert.That(AMessageBeingRead.IsMatch("RefusalSays = refused.Message,"), Is.True,
					"El patron no ve una lectura del mensaje, que es justo lo que se prohibe.");
				Assert.That(AMessageBeingRead.IsMatch("un mensaje para la persona"), Is.False,
					"El patron marca la palabra suelta, asi que cualquier prosa lo dispararia.");

				Assert.That(TheFieldBeingNamed.IsMatch("@parada." + TheFieldThatCarriesTheRefusal),
					Is.True,
					"El patron no ve el campo alcanzado de frente, asi que el barrido del markup esta "
					+ "verde por no haber leido.");
				Assert.That(
					TheFieldBeingNamed.IsMatch("@(parada." + TheFieldThatCarriesTheRefusal + ")"),
					Is.True,
					"El patron no ve el campo alcanzado dentro de una expresion entre parentesis, que "
					+ "es la otra forma en que un markup lo alcanza.");
				Assert.That(TheFieldBeingNamed.IsMatch("@parada.CountdownRefused"), Is.False,
					"El patron marca el booleano de al lado, que es justo lo que la pantalla SI lee.");
			});
		}

		// ===== el instrumento ============================================================

		// EL RENGLON EN QUE APARECE ALGO, para poder afirmar un ORDEN y no solo una presencia. Lo que
		// no aparece contesta el largo del archivo, que es mayor que cualquier renglon: asi la
		// comparacion de orden no se pone verde por una ausencia.
		private static int TheLineOf(string text, string what)
		{
			string[] lines = text.Split('\n');

			for (int line = 0; line < lines.Length; line++)
				if (lines[line].Contains(what, StringComparison.Ordinal)) return line + 1;

			return lines.Length + 1;
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
