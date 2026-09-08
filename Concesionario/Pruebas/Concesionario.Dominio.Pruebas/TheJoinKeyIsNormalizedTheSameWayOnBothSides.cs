using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// L2, Y ES LA UNICA LEY DE ESTA REBANADA CUYO FALLO ES SILENCIOSO. El cruce pasa en el dispositivo
	// de una persona, entre dos modulos que no se hablan, por una IA que compara dos textos
	// publicados. Si los dos normalizan distinto NADA revienta: el cruce simplemente no devuelve nada,
	// y se ve igual que un carro que nunca visito una estacion.
	//
	// LA REGLA DEL OTRO LADO, LEIDA CARACTER POR CARACTER EN Cargas/Dominio/Placa.cs:30-36 y no en la
	// descripcion que el brief hace de ella:
	//
	//     Trim()  ->  ToUpperInvariant()  ->  se queda solo lo que char.IsLetterOrDigit(c) acepta
	//
	// Y LA TRAMPA ESTA EN EL TERCER PASO. char.IsLetterOrDigit ES UNICODE: la A con tilde, la enye y
	// la O con barra son LETRAS y SOBREVIVEN; un digito arabigo-indio es un digito y sobrevive. Un
	// regex [^A-Z0-9] coincide en BQR-482 y DISCREPA en cada uno de esos, sin error y sin match. Abajo
	// esta medido: la variante ASCII discrepa en CUATRO de las trece entradas.
	//
	// Y NO ES LO MISMO QUE char.IsLetter || char.IsNumber. Un superindice es OtherNumber y un numeral
	// romano es LetterNumber: los dos pasan IsNumber y NINGUNO pasa IsDigit, asi que esa variante se
	// queda con caracteres que el otro lado tira. Discrepa en UNA de las trece, y esta abajo tambien.
	//
	// LAS TRES COSAS QUE ESTE ARCHIVO AFIRMA, Y SON TRES Y NO UNA:
	//
	//   1. la transcripcion de la regla del otro modulo produce ESTOS literales. Es el ancla (E49): sin
	//      ella, comparar nuestra llave contra la transcripcion compara dos consumidores de una misma
	//      idea, que se ponen de acuerdo incluso estando los dos igual de equivocados;
	//   2. nuestra llave es igual a esos MISMOS literales;
	//   3. las dos variantes plausiblemente equivocadas NO los producen, y se dice en cuales entradas
	//      discrepan. Una prueba cuyas entradas no discriminan esta verde sin medir nada.
	//
	// LOS LITERALES SE MIDIERON, NO SE PREDIJERON. LP-0 corrio las trece entradas contra el runtime
	// antes de escribirlas. Importa porque ToUpperInvariant es mapeo SIMPLE y no completo, y porque
	// los dos modulos corren sobre el MISMO runtime: lo que hay que espejar es lo que ese runtime
	// hace, no lo que Unicode dice que estaria bien.
	//
	// Cargas NO NORMALIZA NINGUNA FORMA UNICODE, y este archivo lo fija a proposito: una tilde
	// combinante y una precompuesta dan CADENAS DISTINTAS, en los dos lados por igual. La regla a
	// espejar es IDENTICA, no CORRECTA -- y meter un Normalize(FormC) de este lado para "arreglarlo"
	// rompe el cruce en silencio, que es exactamente el fallo que esta ley existe para prevenir.
	[TestFixture]
	public sealed class TheJoinKeyIsNormalizedTheSameWayOnBothSides
	{
		// LAS NUEVE ENTRADAS QUE NO SON ASCII SE ARMAN POR PUNTO DE CODIGO, Y NO ES PROLIJIDAD. Dos de
		// ellas se ven IDENTICAS en pantalla -- la A con tilde precompuesta y la a seguida de tilde
		// combinante -- y son cadenas distintas con llaves distintas: escritas como caracteres, el
		// renglon que separa las dos reglas se lee como un duplicado y alguien lo borra. Con el punto
		// de codigo escrito, cada una dice cual es.
		private static readonly string APrecomposedAccentedLetter = Chars(0x00E1) + "bc-12";
		private static readonly string ItsKey = Chars(0x00C1) + "BC12";

		private static readonly string ALetterOutsideAscii = Chars(0x00F1) + "u-9";
		private static readonly string ItsKeyToo = Chars(0x00D1) + "U9";

		private static readonly string AnotherLetterOutsideAscii = Chars(0x00F8) + "l-77";
		private static readonly string ItsKeyAsWell = Chars(0x00D8) + "L77";

		private static readonly string ACombiningAccent = "a" + Chars(0x0301) + "bc-1";

		private static readonly string ASuperscript = "A" + Chars(0x00B2) + "-9";

		private static readonly string ArabicIndicDigits = Chars(0x0664, 0x0665) + "-A";
		private static readonly string TheirKey = Chars(0x0664, 0x0665) + "A";

		private static readonly string ALetterOutsideTheBmp = "a" + Chars(0xD835, 0xDC00) + "b-1";

		// LAS CUATRO GRAFIAS DE UNA MISMA PLACA que el brief nombra, y las nueve donde las dos reglas
		// podrian discrepar. Cada renglon: lo que llega, y la llave -- MEDIDA contra el runtime.
		private static readonly string[][] TheThirteenInputsAndTheirKeys =
		{
			// Las cuatro grafias de §12 prueba 4. Una sola llave para las cuatro.
			new[] { "BQR-482",  "BQR482" },
			new[] { "bqr 482",  "BQR482" },
			new[] { " BQR482 ", "BQR482" },
			new[] { "bQr-482",  "BQR482" },

			// UNA LETRA ACENTUADA, PRECOMPUESTA. U+00E1 sube a U+00C1 y SOBREVIVE, porque es una
			// letra. La variante ASCII la borra.
			new[] { APrecomposedAccentedLetter, ItsKey },

			// DOS LETRAS QUE NO SON ASCII. U+00F1 y U+00F8 son letras.
			new[] { ALetterOutsideAscii,        ItsKeyToo },
			new[] { AnotherLetterOutsideAscii,  ItsKeyAsWell },

			// UN CERO A LA IZQUIERDA. Se queda: nada aca parsea un numero, y una regla que lo hiciera
			// perderia el cero sin decirlo.
			new[] { "0BQ-482", "0BQ482" },
			new[] { "007-a",   "007A" },

			// UNA TILDE COMBINANTE. U+0301 es NonSpacingMark, no letra: se BORRA. Asi que
			// 'a' + U+0301 da ABC1 mientras que la U+00C1 precompuesta de arriba se queda como letra.
			// Los dos lados hacen lo mismo, y por eso el cruce funciona igual: lo que hay que espejar
			// es la regla IDENTICA y no la CORRECTA.
			new[] { ACombiningAccent, "ABC1" },

			// UN SUPERINDICE. U+00B2 es OtherNumber y no DecimalDigitNumber: se BORRA. Es la entrada
			// donde IsLetter||IsNumber discrepa.
			new[] { ASuperscript, "A9" },

			// DIGITOS ARABIGO-INDIOS. U+0664 y U+0665 son DecimalDigitNumber: se QUEDAN. La variante
			// ASCII los borra.
			new[] { ArabicIndicDigits, TheirKey },

			// UNA LETRA FUERA DEL BMP. U+1D400 cruza como par subrogado y char.IsLetterOrDigit dice
			// que no a cada mitad, asi que se BORRA entera -- en los dos lados igual.
			new[] { ALetterOutsideTheBmp, "AB1" },
		};

		// LAS CUATRO ENTRADAS DONDE LA VARIANTE ASCII DISCREPA. Escritas aparte para que la
		// falsificacion de abajo afirme un CONJUNTO EXACTO: si alguien borra una de las trece, esto se
		// pone rojo en vez de dejar la prueba verde midiendo menos.
		private static readonly string[] TheFourTheAsciiRuleGetsWrong =
		{
			APrecomposedAccentedLetter, ALetterOutsideAscii, AnotherLetterOutsideAscii,
			ArabicIndicDigits,
		};

		// LOS TEXTOS QUE NO DEJAN NADA. Ninguno es blanco: los blancos los rechaza el otro lado un
		// paso antes, con otra excepcion, y aca los dos rechazos son DomainException por convencion de
		// la casa -- un guard que reviente con otro tipo se escapa de Apply y tumba el bloque entero
		// del refresh.
		private static readonly string[] TheTextsThatLeaveNothing = { "---", ".", "-.-", "###" };

		// ===== 1 · EL ANCLA: LA TRANSCRIPCION PRODUCE ESTOS LITERALES ======================

		// SIN ESTO, TODO LO DEMAS COMPARA DOS COPIAS DE UNA MISMA IDEA. La transcripcion se deriva del
		// OTRO modulo y los literales se midieron; si el runtime cambiara de opinion sobre alguna de
		// las trece, ESTA prueba lo dice, y lo dice antes que las otras -- que es el orden util.
		//
		// ES LA UNICA PRUEBA DEL ARCHIVO QUE ESTA VERDE EN LP-0. Las demas necesitan el dominio.
		[Test]
		public void TheTranscriptionOfTheOtherModulesRuleProducesTheLiteralsThisFilePins()
		{
			foreach (string[] each in TheThirteenInputsAndTheirKeys)
				Assert.That(AsTheOtherModuleNormalizes(each[0]), Is.EqualTo(each[1]),
					"La transcripcion de Cargas/Dominio/Placa.cs:30-36 no produce el literal que este "
					+ "archivo fija para '" + Shown(each[0]) + "'. O la transcripcion se copio mal, o "
					+ "el runtime cambio de opinion: las dos son hallazgos, y ninguna se arregla "
					+ "moviendo el literal.");

			foreach (string each in TheTextsThatLeaveNothing)
				Assert.That(AsTheOtherModuleNormalizes(each), Is.Empty,
					"'" + each + "' tiene que no dejar nada al normalizar.");
		}

		// ===== 2 · NUESTRA LLAVE ES ESA MISMA, CARACTER POR CARACTER =======================

		// PRUEBA 4 DE §12. Las cuatro grafias de una placa dan UNA llave.
		[Test]
		public void TheFourSpellingsOfOnePlateProduceOneJoinKey()
		{
			foreach (string spelling in new[] { "BQR-482", "bqr 482", " BQR482 ", "bQr-482" })
				Assert.That(APlateOnTheDoorAndInTheDomain.TheKeyOf(spelling), Is.EqualTo("BQR482"),
					"'" + spelling + "' es la misma placa que BQR-482 y tiene que dar la misma llave.");
		}

		// Y LA LLAVE ES LA DEL OTRO LADO PARA LAS TRECE, incluidas las cuatro donde un regex ASCII
		// discreparia y la una donde IsNumber discreparia.
		[Test]
		public void EachInputYieldsTheKeyTheOtherModulesRuleProducesForIt()
		{
			foreach (string[] each in TheThirteenInputsAndTheirKeys)
			{
				Assert.That(APlateOnTheDoorAndInTheDomain.TheKeyOf(each[0]), Is.EqualTo(each[1]),
					"La llave de '" + Shown(each[0]) + "' no es la que produce la regla del otro "
					+ "modulo. UN CRUCE QUE NO COINCIDE NO DA ERROR: devuelve nada, y se lee como un "
					+ "carro que nunca cargo combustible.");

				Assert.That(
					APlateOnTheDoorAndInTheDomain.TheKeyOf(each[0]),
					Is.EqualTo(AsTheOtherModuleNormalizes(each[0])));
			}
		}

		// ===== 3 · LAS DOS VARIANTES EQUIVOCADAS, Y DONDE DISCREPAN =========================

		// LA VARIANTE ASCII DISCREPA EN CUATRO Y COINCIDE EN NUEVE, y las nueve incluyen BQR-482. Esa
		// es toda la falsificacion: una prueba escrita solo con BQR-482 estaria verde con la regla
		// equivocada adentro, y el defecto se enviaria a esperar.
		[Test]
		public void AnAsciiOnlyRuleAgreesOnNineOfTheThirteenAndDisagreesOnExactlyFour()
		{
			var disagreed = new List<string>();

			foreach (string[] each in TheThirteenInputsAndTheirKeys)
				if (KeepingOnlyAsciiLettersAndDigits(each[0]) != each[1]) disagreed.Add(each[0]);

			Assert.That(disagreed, Is.EquivalentTo(TheFourTheAsciiRuleGetsWrong),
				"Las entradas que separan la regla Unicode de la ASCII cambiaron. Si una se borro, "
				+ "esta prueba dejo de discriminar y las de arriba estan verdes por casualidad.");

			Assert.That(KeepingOnlyAsciiLettersAndDigits("BQR-482"), Is.EqualTo("BQR482"),
				"Y coincide justo en la placa del canon, que es por lo que el defecto se envia.");
		}

		// Y LA VARIANTE IsLetter||IsNumber DISCREPA EN UNA. Es el error mas facil de cometer leyendo
		// la regla en prosa -- "letras y digitos" -- y el mas dificil de ver.
		[Test]
		public void AnIsNumberRuleDisagreesOnExactlyTheSuperscript()
		{
			var disagreed = new List<string>();

			foreach (string[] each in TheThirteenInputsAndTheirKeys)
				if (KeepingEveryLetterOrNumber(each[0]) != each[1]) disagreed.Add(each[0]);

			Assert.That(disagreed, Is.EquivalentTo(new[] { ASuperscript }));
		}

		// ===== 4 · UN TEXTO QUE NO DEJA NADA SE RECHAZA, Y EL RECHAZO DICE QUE LLEGO =======

		// PRUEBA 5 DE §12. Y "dice que llego" es literal: el otro lado escribe el texto TAL COMO
		// LLEGO -- sin trim y sin mayusculas -- en su propio mensaje, y aca tiene que pasar lo mismo,
		// porque el unico dato util para quien lee el rechazo es lo que se mando.
		[Test]
		public void ATextThatNormalizesToNothingIsRefusedAndTheRefusalSaysWhatArrived()
		{
			foreach (string each in TheTextsThatLeaveNothing)
			{
				Exception refused = APlateOnTheDoorAndInTheDomain.TheRefusalOf(each);

				Assert.That(refused.GetType().Name, Is.EqualTo("DomainException"),
					"El rechazo tiene que ser el del dominio y ninguno otro: un guard que reviente "
					+ "con otro tipo se escapa de Apply y tumba el bloque entero del refresh.");

				Assert.That(refused.Message, Does.Contain(each),
					"El rechazo de '" + each + "' no dice que llego. Quien lo lea no tiene otra forma "
					+ "de saber que texto mando la sucursal.");
			}
		}

		// EL PREDICADO ES TOTAL, y contesta tambien para los que el constructor rechaza. Es la mitad
		// que una propiedad sobre LicensePlate no podria contestar -- para llegar a ella habria que
		// construir una placa, y esas entradas son justo las que no construyen ninguna (DEC-100).
		[Test]
		public void TheTotalPredicateAnswersForEveryInputIncludingTheOnesTheConstructorRefuses()
		{
			foreach (string[] each in TheThirteenInputsAndTheirKeys)
				Assert.That(APlateOnTheDoorAndInTheDomain.YieldsAKey(each[0]), Is.True,
					"'" + Shown(each[0]) + "' deja una llave y el predicado dice que no.");

			foreach (string each in TheTextsThatLeaveNothing)
				Assert.That(APlateOnTheDoorAndInTheDomain.YieldsAKey(each), Is.False,
					"'" + each + "' no deja nada y el predicado tiene que contestar, no reventar.");

			Assert.That(APlateOnTheDoorAndInTheDomain.YieldsAKey(""), Is.False);
			Assert.That(APlateOnTheDoorAndInTheDomain.YieldsAKey("   "), Is.False);
			Assert.That(APlateOnTheDoorAndInTheDomain.YieldsAKey(null), Is.False,
				"TOTAL quiere decir que contesta para todo, y el feed puede traer un campo ausente.");
		}

		// ===== LA REGLA DEL OTRO MODULO, TRANSCRITA =======================================

		// Cargas/Dominio/Placa.cs:30-36, tal cual. NO se importa: los modulos no se hablan y Dominio
		// lleva cero referencias. Es una COPIA, y por eso arriba se afirma contra literales medidos y
		// no solo contra nuestra llave: dos copias del mismo error se ponen de acuerdo.
		private static string AsTheOtherModuleNormalizes(string text)
		{
			StringBuilder clean = new StringBuilder(text.Length);

			foreach (char each in text.Trim().ToUpperInvariant())
				if (char.IsLetterOrDigit(each)) clean.Append(each);

			return clean.ToString();
		}

		// La variante que un regex [^A-Z0-9] produciria.
		private static string KeepingOnlyAsciiLettersAndDigits(string text)
		{
			StringBuilder clean = new StringBuilder(text.Length);

			foreach (char each in text.Trim().ToUpperInvariant())
				if ((each >= 'A' && each <= 'Z') || (each >= '0' && each <= '9')) clean.Append(each);

			return clean.ToString();
		}

		// La variante que "letras y numeros" produciria si se leyera IsNumber en vez de IsDigit.
		private static string KeepingEveryLetterOrNumber(string text)
		{
			StringBuilder clean = new StringBuilder(text.Length);

			foreach (char each in text.Trim().ToUpperInvariant())
				if (char.IsLetter(each) || char.IsNumber(each)) clean.Append(each);

			return clean.ToString();
		}

		// Una cadena armada por punto de codigo. Cada argumento es una UNIDAD UTF-16, no un punto de
		// codigo entero: la entrada fuera del BMP se escribe con sus dos mitades a proposito, porque
		// lo que la regla del otro lado mira es cada mitad por separado.
		private static string Chars(params int[] units)
		{
			StringBuilder made = new StringBuilder(units.Length);

			foreach (int each in units) made.Append((char)each);

			return made.ToString();
		}

		// Un mensaje de rojo con caracteres invisibles adentro no dice nada. Esto los muestra.
		private static string Shown(string text)
		{
			StringBuilder said = new StringBuilder();

			foreach (char each in text)
				if (each >= 0x20 && each <= 0x7E) said.Append(each);
				else said.Append("U+").Append(((int)each).ToString("X4"));

			return said.ToString();
		}
	}
}
