using System;
using System.Collections.Generic;
using System.Globalization;

namespace Concesionario.App.Wording
{
	/// <summary>
	/// EL RENDERIZADOR, uno por idioma. Quien pregunta decide QUE decir y CON QUE valores; esta capa
	/// decide COMO se dice. Vive en App y no en Dominio a proposito: una redaccion es texto de
	/// presentacion, y el dominio que la escribiera tendria que elegir un idioma.
	/// <para>
	/// La forma es CLAVE MAS VALORES. La clave es estable y no se traduce; los valores viajan tipados
	/// y entran en los huecos SIN traducirse, porque son del registro del concesionario.
	/// </para>
	/// <para>
	/// El formato de una cifra y el orden de un dia viajan con el renderizador: una coma decimal y un
	/// "12 de junio" contra un "12 June" son decisiones del idioma.
	/// </para>
	/// </summary>
	public sealed class Phrases
	{
		/// <summary>
		/// EL IDIOMA DEL REGISTRO del concesionario, y al que cae toda etiqueta desconocida. Vive aca
		/// y en ningun otro lado: es la subetiqueta con que se elige catalogo, asi que escribirla dos
		/// veces seria dejar que las dos copias dejen de decir lo mismo sin que nada avise.
		/// </summary>
		public const string LanguageOfTheRecord = "es";

		/// <summary>El otro idioma que este modulo habla. El alcance son dos y no hay un tercero.</summary>
		public const string TheOtherLanguage = "en";

		private readonly IReadOnlyDictionary<string, string> catalogue;
		private readonly NumberFormatInfo howNumbersAreWritten;

		private Phrases(IReadOnlyDictionary<string, string> catalogue, NumberFormatInfo howNumbersAreWritten)
		{
			this.catalogue = catalogue;
			this.howNumbersAreWritten = howNumbersAreWritten;
		}

		/// <summary>
		/// Como se escribe un numero grande en cada idioma. Va escrito a mano y NO sale de una
		/// CultureInfo: la cultura de un wasm es la del runtime, no la de la persona, y una publicacion
		/// con globalizacion invariante revienta al pedirsela.
		/// </summary>
		private static readonly NumberFormatInfo AsCostaRicaWritesThem =
			new NumberFormatInfo
			{
				NumberGroupSeparator = ".",
				NumberDecimalSeparator = ",",
				NumberGroupSizes = new[] { 3 },
			};

		private static readonly NumberFormatInfo AsEnglishWritesThem =
			new NumberFormatInfo
			{
				NumberGroupSeparator = ",",
				NumberDecimalSeparator = ".",
				NumberGroupSizes = new[] { 3 },
			};

		private static readonly Phrases InSpanish =
			new Phrases(TheSpanishCatalogue.TheCatalogue(), AsCostaRicaWritesThem);

		private static readonly Phrases InEnglish =
			new Phrases(TheEnglishCatalogue.TheCatalogue(), AsEnglishWritesThem);

		/// <summary>
		/// El renderizador de esa etiqueta. TOTAL: una etiqueta que este modulo no habla cae al idioma
		/// del registro, porque una consulta no se refuta por el nombre de un idioma.
		/// </summary>
		public static Phrases Of(string language)
		{
			return ThePrimarySubtagOf(language) == TheOtherLanguage ? InEnglish : InSpanish;
		}

		/// <summary>Si este modulo habla el idioma de esa etiqueta, para quien quiera ver la caida.</summary>
		public static bool IsALanguageThisModuleSpeaks(string language)
		{
			string primary = ThePrimarySubtagOf(language);
			return primary == LanguageOfTheRecord || primary == TheOtherLanguage;
		}

		/// <summary>
		/// La frase de esa clave con sus huecos llenos. Una clave que falta se dice con su propio
		/// nombre entre llaves y no revienta: una respuesta a la que le falta una palabra sigue
		/// sirviendo, y el hueco se ve.
		/// </summary>
		public string Say(string key, params (string Hole, string Value)[] holes)
		{
			if (!catalogue.TryGetValue(key, out string said)) return "{" + key + "}";

			foreach ((string hole, string value) in holes)
				said = said.Replace("{" + hole + "}", value);

			return said;
		}

		/// <summary>Si este catalogo tiene escrita esa clave.</summary>
		public bool Knows(string key)
		{
			return catalogue.ContainsKey(key);
		}

		/// <summary>Todas las claves que este catalogo tiene escritas.</summary>
		public IEnumerable<string> EveryKey()
		{
			return catalogue.Keys;
		}

		/// <summary>
		/// COMO SE DICE EL NOMBRE DE UN MIEMBRO DE UN CONJUNTO CERRADO. El codigo es identidad, cruza
		/// el wire intacto y no se toca; esto es lo que una persona lee. Un codigo sin dicho se dice
		/// separado en cada mayuscula, que es lo que la pantalla mostraba antes de esta tabla.
		/// </summary>
		public string HowItIsSaid(string family, string code)
		{
			if (string.IsNullOrWhiteSpace(code)) return "";

			string said = Say(family + "." + code);
			return said.StartsWith("{", StringComparison.Ordinal) ? Spaced(code) : said;
		}

		/// <summary>Un numero entero con el separador de miles del idioma. Es formato, no una cuenta.</summary>
		public string Grouped(int value)
		{
			return value.ToString("#,##0", howNumbersAreWritten);
		}

		/// <summary>Cuantos dias, dicho entero para que "1 dias" no aparezca en ningun idioma.</summary>
		public string Days(int days)
		{
			return days == 1
				? Say("days.one")
				: Say("days.many", ("count", Grouped(days)));
		}

		/// <summary>Cuantos meses, dicho entero por la misma razon que los dias.</summary>
		public string Months(int months)
		{
			return months == 1
				? Say("months.one")
				: Say("months.many", ("count", Grouped(months)));
		}

		/// <summary>
		/// Un dia del registro -- yyyy-MM-dd, tal como lo componen las caratulas -- dicho en palabras.
		/// El ORDEN de las partes lo pone el catalogo, porque no es el mismo en los dos idiomas. Un
		/// texto que no calce con ese molde se devuelve intacto: inventarle un dia seria peor.
		/// </summary>
		public string ADay(string yyyyMMdd)
		{
			return TheDayIn("day.long", "month", yyyyMMdd);
		}

		/// <summary>El mismo dia, corto, para una linea de tiempo.</summary>
		public string AShortDay(string yyyyMMdd)
		{
			return TheDayIn("day.short", "month.short", yyyyMMdd);
		}

		private string TheDayIn(string shape, string monthFamily, string yyyyMMdd)
		{
			if (string.IsNullOrWhiteSpace(yyyyMMdd)) return "";

			string firstTen = yyyyMMdd.Length > 10 ? yyyyMMdd.Substring(0, 10) : yyyyMMdd;

			if (!DateTime.TryParseExact(firstTen, "yyyy-MM-dd", CultureInfo.InvariantCulture,
					DateTimeStyles.None, out DateTime day))
				return yyyyMMdd;

			return Say(shape,
				("day", day.Day.ToString(CultureInfo.InvariantCulture)),
				("month", Say(monthFamily + "." + day.Month.ToString(CultureInfo.InvariantCulture))),
				("year", day.Year.ToString(CultureInfo.InvariantCulture)));
		}

		/// <summary>
		/// Un nombre del canon escrito como lo lee una persona: se separa en cada mayuscula y solo la
		/// primera queda en alta. Es etiqueta, no identidad -- lo que cruza la puerta sigue siendo el
		/// nombre tal cual.
		/// </summary>
		public static string Spaced(string name)
		{
			if (string.IsNullOrEmpty(name)) return name;

			var spaced = new System.Text.StringBuilder();
			bool first = true;

			foreach (char letter in name)
			{
				if (first) { spaced.Append(letter); first = false; continue; }

				if (char.IsUpper(letter)) spaced.Append(' ').Append(char.ToLowerInvariant(letter));
				else spaced.Append(letter);
			}

			return spaced.ToString();
		}

		private static string ThePrimarySubtagOf(string tag)
		{
			if (string.IsNullOrWhiteSpace(tag)) return "";

			string clean = tag.Trim().ToLowerInvariant();
			int dash = clean.IndexOf('-');

			return dash < 0 ? clean : clean.Substring(0, dash);
		}
	}
}
