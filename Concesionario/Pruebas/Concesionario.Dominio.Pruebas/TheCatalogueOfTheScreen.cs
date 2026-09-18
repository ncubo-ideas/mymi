using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Concesionario.App.Wording;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LOS DOS CATALOGOS DE LA CARATULA, LEIDOS DESDE UNA PRUEBA. Desde que la pantalla habla dos
	// idiomas, su texto ya no vive en una constante de C# sino en un JSON por idioma, y una prueba que
	// afirme lo que la persona lee tiene que leerlo de ahi.
	//
	// EL IDIOMA DEL REGISTRO ES EL QUE SE AFIRMA, y no es una comodidad: el concesionario lleva el
	// registro en espanol, el dominio todavia redacta en espanol, y una prueba sobre lo que la pantalla
	// dice al lado de una frase del dominio tiene que estar en el mismo idioma que esa frase. Cuando el
	// dominio pase a clave-mas-valores, esta clase es donde el idioma se vuelve un parametro.
	//
	// Los dos archivos viajan como DATO -- ver el csproj, que dice por que no se leen del wwwroot por
	// una ruta relativa.
	internal static class TheCatalogueOfTheScreen
	{
		internal const string TheOneOfTheRecord = "texto.es-CR.json.dato";

		internal const string TheOneInEnglish = "texto.en.json.dato";

		/// <summary>El renderizador de la App en el idioma del registro, que es en el que el dominio redacta.</summary>
		internal static Phrases InTheLanguageOfTheRecord => Phrases.Of(Phrases.LanguageOfTheRecord);

		/// <summary>El renderizador de la App en el otro idioma que este modulo habla.</summary>
		internal static Phrases InTheOtherLanguage => Phrases.Of(Phrases.TheOtherLanguage);

		/// <summary>Lo que la pantalla dice en el idioma del registro, plano y por clave.</summary>
		internal static IReadOnlyDictionary<string, string> WhatTheScreenSaysInSpanish() =>
			TheFlattenedCatalogue(TheOneOfTheRecord);

		/// <summary>Lo mismo en el otro idioma.</summary>
		internal static IReadOnlyDictionary<string, string> WhatTheScreenSaysInEnglish() =>
			TheFlattenedCatalogue(TheOneInEnglish);

		/// <summary>La frase de esa clave en el idioma del registro.</summary>
		internal static string InSpanish(string key)
		{
			return TheLineOf(WhatTheScreenSaysInSpanish(), key, "espanol");
		}

		/// <summary>La frase de esa clave en el otro idioma que este modulo habla.</summary>
		internal static string InEnglish(string key)
		{
			return TheLineOf(WhatTheScreenSaysInEnglish(), key, "ingles");
		}

		private static string TheLineOf(
			IReadOnlyDictionary<string, string> catalogue, string key, string language)
		{
			Assert.That(catalogue.ContainsKey(key), Is.True,
				"'" + key + "' no esta en el catalogo de la pantalla en " + language + ", asi que lo "
				+ "que la persona leeria ahi es la clave cruda entre llaves.");

			return catalogue[key];
		}

		// EL MISMO RELLENO DE HUECOS QUE HACE Texts.With, y no se puede llamar al suyo: Texts carga su
		// catalogo por HTTP desde el wwwroot y su diccionario es privado, asi que en una prueba no hay
		// ninguna instancia con palabras adentro. Es una SEGUNDA implementacion del mismo reemplazo, y
		// lo que la mantiene honesta es que deja el hueco INTACTO cuando el nombre no calza -- que es
		// justo lo que una afirmacion por valor ve, porque el '{hueco}' se queda en la frase final.
		internal static string With(string said, params (string Hole, string Value)[] holes)
		{
			foreach ((string hole, string value) in holes)
				said = said.Replace("{" + hole + "}", value);

			return said;
		}

		// EL MISMO APLANADO QUE HACE Texts, y por la misma razon: el catalogo se escribe anidado por
		// pantalla, para que se lea, y se busca plano. La llave "//" es la nota del archivo, no texto.
		internal static IReadOnlyDictionary<string, string> TheFlattenedCatalogue(string file)
		{
			string path = Path.Combine(AppContext.BaseDirectory, file);

			Assert.That(File.Exists(path), Is.True,
				file + ": no viajo al output, asi que esta prueba no esta afirmando nada.");

			using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));

			var flat = new Dictionary<string, string>(StringComparer.Ordinal);
			Flatten(document.RootElement, "", flat);
			return flat;
		}

		private static void Flatten(JsonElement node, string prefix, Dictionary<string, string> flat)
		{
			if (node.ValueKind != JsonValueKind.Object)
			{
				if (prefix.Length > 0 && node.ValueKind == JsonValueKind.String)
					flat[prefix] = node.GetString();
				return;
			}

			foreach (JsonProperty leaf in node.EnumerateObject())
			{
				if (leaf.Name == "//") continue;
				Flatten(leaf.Value, prefix.Length == 0 ? leaf.Name : prefix + "." + leaf.Name, flat);
			}
		}
	}
}
