using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Municipalidad.Consola
{
	/// <summary>
	/// Los dos catalogos de la cara hibrida, cotejados: mismas claves, ningun valor vacio, mismos
	/// huecos, y toda clave que el markup pide presente en los dos.
	/// <para>
	/// Una clave que falta se dice con su propio nombre entre llaves y nada se pone rojo, asi que sin
	/// esto una asimetria de una letra sale a pantalla sin que nadie se entere.
	/// </para>
	/// </summary>
	internal static class ComprobacionesDeLosCatalogos
	{
		private const string ElDeLaCaraEnElRegistro = "cara.es-CR.js.dato";
		private const string ElDeLaCaraEnIngles = "cara.en.js.dato";
		private const string ElMarkupDeLaCara = "cara.index.html.dato";

		private static readonly Regex UnHueco = new Regex(@"\{(\w+)\}", RegexOptions.Compiled);

		private static readonly Regex UnaClavePedida =
			new Regex(@"data-texto(?:-aria|-html)?=""([^""]+)""", RegexOptions.Compiled);

		/// <summary>El objeto que el archivo asigna, sin el envoltorio de la asignacion.</summary>
		private static readonly Regex LaAsignacion =
			new Regex(@"window\.TextoDeLaCara\[[^\]]+\]\s*=\s*", RegexOptions.Compiled);

		private static int corridas;
		private static int fallas;

		/// <summary>Corre las comprobaciones dentro de un arnes que ya lleva su propio conteo.</summary>
		internal static void Correr(Action<string> titulo, Action<string, string, string> igual)
		{
			ElParDeLaCara(titulo, igual);
			LoQueLaCaraPideExiste(titulo, igual);
		}

		/// <summary>Corre las comprobaciones solas, con su propio conteo. Devuelve 0 si todo pasa.</summary>
		internal static int Correr()
		{
			corridas = 0;
			fallas = 0;

			Correr(Titulo, Igual);

			Console.WriteLine();
			Console.WriteLine(fallas == 0
				? $"TODO OK  ({corridas} comprobaciones)"
				: $"{fallas} FALLA(S) de {corridas} comprobaciones");

			return fallas == 0 ? 0 : 1;
		}

		private static void ElParDeLaCara(Action<string> titulo, Action<string, string, string> igual)
		{
			titulo("los dos catalogos de la cara hibrida dicen las mismas claves");

			IReadOnlyDictionary<string, string> enElRegistro =
				ElCatalogoDeLaCara(ElDeLaCaraEnElRegistro, igual);
			IReadOnlyDictionary<string, string> enElOtro =
				ElCatalogoDeLaCara(ElDeLaCaraEnIngles, igual);

			igual("el catalogo del registro tiene claves", "True", (enElRegistro.Count > 0).ToString());
			igual("  y el del otro idioma tambien", "True", (enElOtro.Count > 0).ToString());

			igual("  ninguna clave del registro falta en el otro idioma", "",
				string.Join(", ", LasQueLeFaltanA(enElRegistro, enElOtro)));
			igual("  ninguna clave sobra en el otro idioma", "",
				string.Join(", ", LasQueLeFaltanA(enElOtro, enElRegistro)));

			igual("  ninguna clave se quedo sin palabras", "",
				string.Join(", ", SinPalabras(enElRegistro).Concat(SinPalabras(enElOtro))));

			igual("  los dos idiomas nombran los mismos huecos", "",
				string.Join(", ", ConHuecosDistintos(enElRegistro, enElOtro)));
		}

		/// <summary>
		/// Cada clave que el markup nombra por atributo, en los dos catalogos. Un
		/// <c>data-texto</c> es una cadena, y una cadena mal escrita compila siempre.
		/// </summary>
		private static void LoQueLaCaraPideExiste(
			Action<string> titulo, Action<string, string, string> igual)
		{
			titulo("cada clave que el markup de la cara pide existe en los dos catalogos");

			string markup = LoQueDiceElArchivo(ElMarkupDeLaCara, igual);
			if (markup.Length == 0) return;

			string[] pedidas = UnaClavePedida.Matches(markup)
				.Select(una => una.Groups[1].Value)
				.Distinct()
				.OrderBy(una => una, StringComparer.Ordinal)
				.ToArray();

			igual("el markup pide claves", "True", (pedidas.Length > 0).ToString());

			IReadOnlyDictionary<string, string> enElRegistro =
				ElCatalogoDeLaCara(ElDeLaCaraEnElRegistro, igual);
			IReadOnlyDictionary<string, string> enElOtro =
				ElCatalogoDeLaCara(ElDeLaCaraEnIngles, igual);

			igual("  ninguna le falta al idioma del registro", "",
				string.Join(", ", pedidas.Where(una => !enElRegistro.ContainsKey(una))));
			igual("  ninguna le falta al otro idioma", "",
				string.Join(", ", pedidas.Where(una => !enElOtro.ContainsKey(una))));
		}

		private static IEnumerable<string> SinPalabras(IReadOnlyDictionary<string, string> catalogo)
		{
			return catalogo
				.Where(una => una.Value.Trim().Length == 0)
				.Select(una => una.Key)
				.OrderBy(una => una, StringComparer.Ordinal);
		}

		private static IEnumerable<string> ConHuecosDistintos(
			IReadOnlyDictionary<string, string> unCatalogo, IReadOnlyDictionary<string, string> elOtro)
		{
			foreach (KeyValuePair<string, string> una in
				unCatalogo.OrderBy(u => u.Key, StringComparer.Ordinal))
			{
				string suTraduccion;
				if (!elOtro.TryGetValue(una.Key, out suTraduccion)) continue;

				if (!LosHuecosDe(suTraduccion).SequenceEqual(LosHuecosDe(una.Value)))
					yield return una.Key;
			}
		}

		private static string[] LasQueLeFaltanA(
			IReadOnlyDictionary<string, string> unCatalogo, IReadOnlyDictionary<string, string> elOtro)
		{
			return unCatalogo.Keys
				.Where(clave => !elOtro.ContainsKey(clave))
				.OrderBy(clave => clave, StringComparer.Ordinal)
				.ToArray();
		}

		private static string[] LosHuecosDe(string dicho)
		{
			return UnHueco.Matches(dicho)
				.Select(uno => uno.Groups[1].Value)
				.OrderBy(uno => uno, StringComparer.Ordinal)
				.ToArray();
		}

		/// <summary>
		/// El catalogo de la cara, aplanado. El archivo es JavaScript y su objeto es JSON valido, asi
		/// que se le quita la asignacion y lo lee el mismo parser que el resto: un lector por linea
		/// aceptaria formas que el navegador no.
		/// </summary>
		private static IReadOnlyDictionary<string, string> ElCatalogoDeLaCara(
			string archivo, Action<string, string, string> igual)
		{
			var plano = new Dictionary<string, string>(StringComparer.Ordinal);

			string crudo = LoQueDiceElArchivo(archivo, igual);
			if (crudo.Length == 0) return plano;

			Match asignacion = LaAsignacion.Match(crudo);
			int abre = asignacion.Success ? crudo.IndexOf('{', asignacion.Index + asignacion.Length) : -1;
			int cierra = crudo.LastIndexOf('}');

			if (abre < 0 || cierra <= abre)
			{
				igual("  " + archivo + " asigna un objeto", "True", "False");
				return plano;
			}

			try
			{
				using (JsonDocument documento =
					JsonDocument.Parse(crudo.Substring(abre, cierra - abre + 1)))
					Aplanar(documento.RootElement, "", plano);
			}
			catch (JsonException rota)
			{
				igual("  " + archivo + " se puede leer", "", rota.Message);
				return plano;
			}

			return plano;
		}

		/// <summary>El catalogo se escribe anidado, para que se lea, y se busca plano.</summary>
		private static void Aplanar(JsonElement nodo, string prefijo, Dictionary<string, string> plano)
		{
			if (nodo.ValueKind != JsonValueKind.Object)
			{
				if (prefijo.Length > 0 && nodo.ValueKind == JsonValueKind.String)
					plano[prefijo] = nodo.GetString();
				return;
			}

			foreach (JsonProperty hoja in nodo.EnumerateObject())
			{
				if (hoja.Name == "//") continue;
				Aplanar(hoja.Value, prefijo.Length == 0 ? hoja.Name : prefijo + "." + hoja.Name, plano);
			}
		}

		/// <summary>
		/// Un archivo que no viajo al output no es un catalogo vacio: es una comprobacion que no
		/// afirma nada, y callada pasaria en verde. Se dice como falla y se sigue.
		/// </summary>
		private static string LoQueDiceElArchivo(string archivo, Action<string, string, string> igual)
		{
			string ruta = Path.Combine(AppContext.BaseDirectory, archivo);

			if (!File.Exists(ruta))
			{
				igual("  " + archivo + " viajo al output", "True", "False");
				return "";
			}

			return File.ReadAllText(ruta);
		}

		private static void Titulo(string texto)
		{
			Console.WriteLine();
			Console.WriteLine($"-- {texto} --");
		}

		private static void Igual(string titulo, string esperado, string obtenido)
		{
			corridas++;
			bool ok = esperado == obtenido;
			if (!ok) fallas++;
			Console.WriteLine($"  [{(ok ? "OK  " : "FALLA")}] {titulo}");
			if (!ok)
			{
				Console.WriteLine($"          esperado: {esperado}");
				Console.WriteLine($"          obtenido: {obtenido}");
			}
		}
	}
}
