using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Concesionario.App;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// PROBE 9 MIDIO QUE LA ESCALA DE UN decimal NO SOBREVIVE AL JOURNAL: 145.00 escrito vuelve 145
	// leido. El Normalized de la puerta esta en el camino de ESCRITURA y le da al decimal la MISMA
	// forma que va a tener despues de pasar por el journal, antes de que el journal la vea -- asi que
	// el valor vivo y el reproducido son el mismo valor y no dos que apenas comparan iguales.
	//
	// POR QUE ESTA PRUEBA MIRA EL FUENTE Y NO UNA RESPUESTA, dicho antes de que alguien lo tome por
	// pereza. CL-2 §3.1 midio que Money.Expressed tapa esa perdida SOLO para una moneda que el
	// conjunto NOMBRA: la vuelve a decir con los decimales de esa moneda, asi que 145.00 y 145 salen
	// los dos como ₡145,00. Para una que no nombra, la escala SE VE -- y es ahi donde el Normalized de
	// la puerta es lo unico que impide que vivo y replayado discrepen.
	//
	// Y CL-4 CIERRA ESA MISMA PUERTA: WireName.Currency rechaza en el borde todo codigo que el
	// conjunto no nombre, asi que por ESTOS DOS COMANDOS ya no entra ninguna moneda sin nombrar. La
	// consecuencia es incomoda y se dice entera: por la puerta del libro, hoy, el efecto de Normalized
	// sobre lo que alguien lee es INVISIBLE. Ninguna afirmacion por valor lo puede ver.
	//
	// LO QUE LO MANTIENE OBLIGATORIO SON DOS CAMINOS QUE SIGUEN ABIERTOS. El comando del dominio
	// resuelve con Currency.OfPublished -- DEC-96 -- asi que una moneda sin nombrar puede estar YA
	// ESCRITA en un libro; y CL-7 le abre al feed su propia entrada, donde L14 pone la mitad del
	// recogniser. Con cualquiera de las dos, un monto que hubiera cruzado crudo dice una cosa vivo y
	// otra despues del primer reinicio.
	//
	// ASI QUE EL INSTRUMENTO ES EL FUENTE, y es el unico que ve una decision de la caratula que
	// ninguna respuesta expone: CL-1 lo midio en cero rojos por el lado del comportamiento. El
	// chequeo [REPLAY]/C53 de CL-5 sobre decimal es el otro, y es el unico que llegaria a ver un monto
	// que se salto Normalized.
	[TestFixture]
	public sealed class TheAmountOfACostIsGivenTheJournalsShapeBeforeItCrosses
	{
		// LOS DOS COMANDOS ATAN SU MONTO POR Normalized Y SU INSTANTE POR InUtc. Se lee el texto del
		// miembro, no el archivo entero: una llamada a Normalized en cualquier otro sitio dejaria esto
		// verde si se buscara en todo el fuente.
		[TestCase("RecordRecurringCost")]
		[TestCase("RecordDurableCost")]
		public void TheCommandBindsItsAmountThroughTheDoorsNormalization(string command)
		{
			string body = TheBodyOf(command);

			TestContext.Out.WriteLine(command + " -- " + body);

			Assert.That(body, Does.Contain("p[\"amount\", typeof(decimal)] = Normalized(amount);"),
				"El monto tiene que cruzar con la forma que el journal le va a dar. Un comando que lo "
				+ "manda crudo esta verde el dia que se escribe y discrepa consigo mismo despues del "
				+ "primer reinicio -- y no hay ninguna respuesta de esta puerta que lo vea.");

			Assert.That(body, Does.Contain("p[\"incurredAt\", typeof(DateTime)] = InUtc(incurredAt);"),
				"Y el instante con la suya: el journal lo trunca al segundo entero y le borra el Kind. "
				+ "Es la otra normalizacion del borde y esta por el mismo motivo.");
		}

		// Y NINGUNO DE LOS DOS MANDA EL MONTO CRUDO POR OTRO LADO. Sin este renglon, el de arriba
		// pasaria igual si alguien dejara la linea buena y agregara una segunda atadura debajo.
		[TestCase("RecordRecurringCost")]
		[TestCase("RecordDurableCost")]
		public void TheCommandBindsItsAmountExactlyOnce(string command)
		{
			Assert.That(
				Regex.Matches(TheBodyOf(command), Regex.Escape("p[\"amount\"")).Count,
				Is.EqualTo(1),
				"Dos ataduras del mismo nombre son dos formas de mandar el mismo valor, y la que "
				+ "manda es la ultima.");
		}

		// Y LOS DOS SCRIPTS DE INGESTA DEL FEED HACEN LO MISMO, que es la mitad que este fixture debia
		// desde que se escribio: su propio encabezado dice que CL-7 le abre al feed su entrada, y esa
		// entrada tiene sus propias dos ataduras del mismo monto y del mismo instante.
		//
		// Y ACA EL INSTRUMENTO ES EL FUENTE POR UNA RAZON MAS FUERTE TODAVIA. Por el feed el borde
		// tambien descarta toda moneda que el conjunto no nombre -- L14 -- asi que tampoco por aca hay
		// un monto cuya escala se pueda ver en una respuesta. Los dos caminos quedaron cerrados, y lo
		// que sigue abierto es el unico que ninguno de los dos toca: un libro que una app anterior ya
		// escribio.
		[TestCase("ApplyRecurringCostRecorded")]
		[TestCase("ApplyDurableCostRecorded")]
		public void TheIngestionScriptBindsItsAmountThroughTheDoorsNormalizationToo(string script)
		{
			string body = TheIngestionBodyOf(script);

			TestContext.Out.WriteLine(script + " -- " + body);

			Assert.That(
				body, Does.Contain("p[\"amount\", typeof(decimal)] = Normalized(message.Amount);"),
				"El monto recibido tiene que cruzar con la forma que el journal le va a dar. Un script "
				+ "de ingesta que lo manda crudo esta verde el dia que se escribe y discrepa consigo "
				+ "mismo despues del primer reinicio.");

			Assert.That(
				body,
				Does.Contain("p[\"incurredAt\", typeof(DateTime)] = InUtc(message.IncurredAt);"),
				"Y el instante con la suya: el journal lo trunca al segundo entero y le borra el Kind.");

			Assert.That(
				Regex.Matches(body, Regex.Escape("p[\"amount\"")).Count, Is.EqualTo(1),
				"Y una sola vez: dos ataduras del mismo nombre son dos formas de mandar el mismo valor, "
				+ "y la que manda es la ultima.");
		}

		// LO QUE Normalized HACE, medido en el propio miembro y no supuesto. Sin esto, las dos pruebas
		// de arriba afirmarian que se llama a algo que podria no hacer nada.
		[TestCase("145.00", "145")]
		[TestCase("85000.10", "85000.1")]
		[TestCase("0.100", "0.1")]
		[TestCase("240000.000", "240000")]
		public void TheDoorsNormalizationDropsTheZerosTheJournalWouldDropAnyway(
			string given, string expected)
		{
			decimal shaped = TheDoorsNormalization(decimal.Parse(
				given, System.Globalization.CultureInfo.InvariantCulture));

			Assert.That(
				shaped.ToString(System.Globalization.CultureInfo.InvariantCulture),
				Is.EqualTo(expected),
				"PROBE 9: el journal escribe el decimal sin los ceros que sobran a la derecha. Esto le "
				+ "da esa forma ANTES, que es lo que hace que el valor vivo y el reproducido sean el "
				+ "mismo.");
		}

		// Y EL VALOR NO CAMBIA, solo como se dice. Un Normalized que redondeara seria una correccion
		// de un hecho recibido y no una normalizacion.
		[Test]
		public void TheNormalizationChangesHowTheAmountIsSaidAndNeverWhatItIs()
		{
			Assert.That(TheDoorsNormalization(145.00m), Is.EqualTo(145m));
			Assert.That(TheDoorsNormalization(85000.10m), Is.EqualTo(85000.1m));
			Assert.That(TheDoorsNormalization(0.055m), Is.EqualTo(0.055m),
				"Un monto que no tiene ceros de mas cruza intacto: normalizar no es redondear.");
		}

		// EL FUENTE DE LA PUERTA COMO DATO, y el miembro recortado de el con los espacios colapsados,
		// para que la afirmacion no dependa de como esten alineadas las columnas.
		private static string TheBodyOf(string command)
		{
			string source = File.ReadAllText(Path.Combine(
				AppContext.BaseDirectory, "CodigoComoDato", "App", "DealershipCarAPI.cs"));

			int opens = source.IndexOf("public void " + command + "(", StringComparison.Ordinal);

			Assert.That(opens, Is.GreaterThan(-1),
				$"'{command}' no esta en el fuente de la puerta. Si se renombro, esta prueba mide una "
				+ "ausencia en vez de una atadura.");

			int closes = source.IndexOf(".PerformCommand();", opens, StringComparison.Ordinal);

			Assert.That(closes, Is.GreaterThan(opens));

			return Regex.Replace(source.Substring(opens, closes - opens), @"\s+", " ");
		}

		// Y EL DE UN SCRIPT DE INGESTA, que es privado y cierra con otro Perform. Se recorta igual y por
		// el mismo motivo: buscar en el archivo entero dejaria verde una llamada hecha en otro miembro.
		private static string TheIngestionBodyOf(string script)
		{
			string source = File.ReadAllText(Path.Combine(
				AppContext.BaseDirectory, "CodigoComoDato", "App", "DealershipCarAPI.cs"));

			int opens = source.IndexOf("private string " + script + "(", StringComparison.Ordinal);

			Assert.That(opens, Is.GreaterThan(-1),
				$"'{script}' no esta en el fuente de la puerta. Si se renombro, esta prueba mide una "
				+ "ausencia en vez de una atadura.");

			int closes = source.IndexOf(".PerformCheckThenCommand();", opens, StringComparison.Ordinal);

			Assert.That(closes, Is.GreaterThan(opens));

			return Regex.Replace(source.Substring(opens, closes - opens), @"\s+", " ");
		}

		// Normalized es PRIVADO de la puerta, como InUtc y TheText, y se alcanza por reflexion: es la
		// misma forma que TheDoorRefusesADayThatIsNotADayBeforePerforming usa para ADay.
		private static decimal TheDoorsNormalization(decimal value)
		{
			MethodInfo member = typeof(DealershipCarAPI).GetMethod(
				"Normalized", BindingFlags.Static | BindingFlags.NonPublic);

			Assert.That(member, Is.Not.Null,
				"La puerta perdio su Normalized. Las dos pruebas del fuente seguirian verdes "
				+ "afirmando una llamada a un miembro que no existe.");

			return (decimal)member.Invoke(null, new object[] { value });
		}
	}
}
