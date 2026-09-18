using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Examenes.Consola
{
	// EL DEFECTO QUE ESTO EXISTE PARA IMPEDIR, y es el mismo que LosDosCatalogosDicenLasMismasClaves
	// atrapa del lado de las FRASES DEL DOMINIO: una clave que se agrega a un catalogo y se olvida en
	// el otro. Del lado de las CARATULAS no lo atrapaba nadie.
	//
	// Y se paga en pantalla: Textos.this[clave] y el texto() de idioma.js contestan "{clave}" cuando
	// la clave no esta, asi que una asimetria de una sola letra sale a la vista de la persona con las
	// llaves puestas, y nada se pone rojo. Los catalogos estaban en paridad exacta el dia que se
	// escribio esto; lo que faltaba era algo que los mantuviera asi.
	//
	// SON DOS PARES Y NINGUNO ES EL OTRO: la carátula del navegador lee JSON y la hibrida lee JS, y
	// cada par se mantiene por su lado. La hibrida agrega una tercera pregunta que el JSON no tiene:
	// su markup nombra las claves por atributo, asi que se puede comprobar que cada clave que la
	// pantalla PIDE exista de verdad -- lo que del lado del JSON solo lo sabe el razor al compilar.
	//
	// LAS TRES DE CADA PAR SE NECESITAN Y NINGUNA SOBRA:
	//
	//   - Comparar los dos juegos de claves atrapa la asimetria.
	//   - Que ningun valor sea vacio atrapa la clave que se agrego para traducir despues: la clave
	//     esta en los dos, la comparacion pasa, y la pantalla dice nada.
	//   - Comparar los HUECOS de cada clave atrapa la traduccion que se comio un {hueco} o le cambio
	//     el nombre. Uno que sobra se queda en pantalla con las llaves puestas; uno que falta borra un
	//     dato de la frase sin dejar rastro, que es peor porque se lee bien.
	//
	// NO se afirma que los dos textos sean distintos: hay claves que son plantilla pura -- "· {x}" --
	// y se dicen igual en los dos idiomas a proposito.
	//
	// LOS ARCHIVOS VIAJAN COMO DATO -- ver el csproj, que dice por que.
	internal static class ComprobacionesDeLosCatalogos
	{
		private const string ElDeLaPantallaEnElRegistro = "texto.es-CR.json.dato";
		private const string ElDeLaPantallaEnIngles = "texto.en.json.dato";
		private const string ElDeLaCaraEnElRegistro = "cara.es-CR.js.dato";
		private const string ElDeLaCaraEnIngles = "cara.en.js.dato";
		private const string ElMarkupDeLaCara = "cara.index.html.dato";

		private static readonly Regex UnHueco = new Regex(@"\{(\w+)\}", RegexOptions.Compiled);

		private static readonly Regex AbreGrupo =
			new Regex(@"^([A-Za-z]\w*)\s*:\s*\{", RegexOptions.Compiled);
		private static readonly Regex UnDicho =
			new Regex(@"^([A-Za-z]\w*)\s*:\s*'(.*)'\s*,?\s*$", RegexOptions.Compiled);
		private static readonly Regex UnaLista =
			new Regex(@"^([A-Za-z]\w*)\s*:\s*\[(.*)\]\s*,?\s*$", RegexOptions.Compiled);
		private static readonly Regex UnaClavePedida =
			new Regex(@"data-texto(?:-aria)?=""([^""]+)""", RegexOptions.Compiled);

		internal static void Correr(Action<string> titulo, Action<string, string, string> igual)
		{
			ElParDeLaPantalla(titulo, igual);
			ElParDeLaCara(titulo, igual);
			LoQueLaCaraPideExiste(titulo, igual);
		}

		private static void ElParDeLaPantalla(Action<string> titulo, Action<string, string, string> igual)
		{
			titulo("los dos catalogos de la pantalla dicen las mismas claves");

			IReadOnlyDictionary<string, string> enElRegistro =
				ElCatalogoEnJson(ElDeLaPantallaEnElRegistro, igual);
			IReadOnlyDictionary<string, string> enElOtro =
				ElCatalogoEnJson(ElDeLaPantallaEnIngles, igual);

			LosDosSeDicenIgual(igual, enElRegistro, enElOtro);
		}

		private static void ElParDeLaCara(Action<string> titulo, Action<string, string, string> igual)
		{
			titulo("los dos catalogos de la cara hibrida dicen las mismas claves");

			IReadOnlyDictionary<string, string> enElRegistro =
				ElCatalogoEnJs(ElDeLaCaraEnElRegistro, igual);
			IReadOnlyDictionary<string, string> enElOtro =
				ElCatalogoEnJs(ElDeLaCaraEnIngles, igual);

			LosDosSeDicenIgual(igual, enElRegistro, enElOtro);
		}

		// CADA CLAVE QUE EL MARKUP PIDE, en los dos catalogos. Es la pregunta que el par de JSON no
		// necesita -- ahi las claves las escribe el razor y el compilador las ve -- y que aca no la
		// hace nadie: un data-texto es una cadena, y una cadena mal escrita compila siempre.
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
				ElCatalogoEnJs(ElDeLaCaraEnElRegistro, igual);
			IReadOnlyDictionary<string, string> enElOtro =
				ElCatalogoEnJs(ElDeLaCaraEnIngles, igual);

			igual("  ninguna le falta al idioma del registro", "",
				string.Join(", ", pedidas.Where(una => !enElRegistro.ContainsKey(una))));
			igual("  ninguna le falta al otro idioma", "",
				string.Join(", ", pedidas.Where(una => !enElOtro.ContainsKey(una))));
		}

		private static void LosDosSeDicenIgual(Action<string, string, string> igual,
			IReadOnlyDictionary<string, string> enElRegistro,
			IReadOnlyDictionary<string, string> enElOtro)
		{
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
			foreach (KeyValuePair<string, string> una in unCatalogo.OrderBy(u => u.Key, StringComparer.Ordinal))
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

		// EL MISMO APLANADO QUE HACE Textos, y por la misma razon: el catalogo se escribe anidado por
		// pantalla, para que se lea, y se busca plano. La llave "//" es la nota del archivo, no texto.
		private static IReadOnlyDictionary<string, string> ElCatalogoEnJson(
			string archivo, Action<string, string, string> igual)
		{
			var plano = new Dictionary<string, string>(StringComparer.Ordinal);

			string crudo = LoQueDiceElArchivo(archivo, igual);
			if (crudo.Length == 0) return plano;

			using (JsonDocument documento = JsonDocument.Parse(crudo))
				Aplanar(documento.RootElement, "", plano);

			return plano;
		}

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

		// EL CATALOGO DE LA CARA SE LEE POR LINEA, y no con un parser de JavaScript: es un literal de
		// dos niveles -- grupo y clave -- y de una forma que este mismo repo escribe. Si algun dia se
		// anida mas, el conteo de claves cae y la comprobacion de arriba lo dice, que es exactamente lo
		// que se quiere de un lector asi.
		//
		// El mes es una lista y no una frase: se cuenta como clave, porque faltar en un idioma es el
		// mismo defecto, y su valor se dice con sus elementos para que el vacio se detecte igual.
		private static IReadOnlyDictionary<string, string> ElCatalogoEnJs(
			string archivo, Action<string, string, string> igual)
		{
			var plano = new Dictionary<string, string>(StringComparer.Ordinal);

			string crudo = LoQueDiceElArchivo(archivo, igual);
			if (crudo.Length == 0) return plano;

			string grupo = "";

			foreach (string linea in crudo.Split('\n'))
			{
				string limpia = linea.Trim();
				if (limpia.StartsWith("//", StringComparison.Ordinal)) continue;

				if (limpia.StartsWith("}", StringComparison.Ordinal)) { grupo = ""; continue; }

				Match abre = AbreGrupo.Match(limpia);
				if (abre.Success) { grupo = abre.Groups[1].Value; continue; }

				Match dicho = UnDicho.Match(limpia);
				if (dicho.Success)
				{
					plano[ConSuGrupo(grupo, dicho.Groups[1].Value)] = dicho.Groups[2].Value;
					continue;
				}

				Match lista = UnaLista.Match(limpia);
				if (lista.Success)
					plano[ConSuGrupo(grupo, lista.Groups[1].Value)] = lista.Groups[2].Value;
			}

			return plano;
		}

		private static string ConSuGrupo(string grupo, string clave)
		{
			return grupo.Length == 0 ? clave : grupo + "." + clave;
		}

		// UN ARCHIVO QUE NO VIAJO AL OUTPUT NO ES UN CATALOGO VACIO: es una comprobacion que no esta
		// afirmando nada, y callada pasaria en verde. Se dice como falla y se sigue.
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
	}
}
