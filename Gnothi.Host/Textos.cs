using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Gnothi.Host;

// WHAT THE HOST SAYS TO THE PERSON, IN ONE PLACE, IN TWO LANGUAGES.
//
// WHY IT LIVES HERE, of all projects. The Host's text does not come out of one file: it comes
// out of five projects at once. The page on the phones (Gnothi.Mobile/Assets/ui.html), the five
// .razor of the desktop, the enrollment sentences of Gnothi.Movil, the denial reasons of
// Dispatcher -which reach Siri through Snapshot- and two UIKit labels on iOS that never touch a
// WebView. A catalog on the HTML side would have covered less than half of that.
//
// Gnothi.Host is the one place all of them can reach. The desktop references it as a project;
// the phones compile its sources by glob (Gnothi.Mobile.csproj, Gnothi.iOS.csproj). That second
// half is why the json is declared as an EmbeddedResource in THREE csproj with the SAME
// LogicalName: the phones do not reference this assembly, so the resource has to be re-declared
// where the glob compiled this file, and a fixed logical name is what lets the ONE line below
// find it in all three.
//
// WHY NOT .resx. Because a .resx is per-assembly and this text has to be readable by a person
// doing a copy pass, without opening Visual Studio and without touching C#.
//
// WHAT DOES NOT GO IN HERE, and the rules are in Metodologia/Guias/reglas-de-localizacion.md:
// nothing the institution wrote (its name, its slogan, the title and description of its tools),
// nothing that came out of the person's journal, and nothing that is written for the model
// rather than for the eye. Also not the catalog warnings: those speak of 'brand.shortName' and
// 'about.category', and they are diagnostics for whoever packages a module.
public static class Textos
{
	// THE TWO LANGUAGES, AND ONLY TWO. The scope is a decision, not a limitation of this code:
	// several of the rules in the guide are written for exactly two, and a third one would
	// reopen them. English is the fallback because it is the one that never has to guess.
	public const string Espanol = "es";
	public const string Ingles = "en";

	private static string? elegido;

	// THE LANGUAGE, RESOLVED ONCE. Every face sets it at startup from the DEVICE -that is the
	// decision: no selector inside Gnothi- and everything downstream reads it from here.
	//
	// The default is not "es": it is whatever the machine says, so a face that forgets to set
	// it still behaves. Anything that is not Spanish falls to English.
	//
	// The page does NOT resolve its own language with navigator.language. It is told. Two
	// resolvers would be two sources of truth, which is the thing this whole file exists to
	// avoid.
	public static string Idioma
	{
		get => elegido ??= DelAparato();
		set => elegido = Normalizar(value);
	}

	private static string DelAparato() =>
		Normalizar(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);

	private static string Normalizar(string? tag) =>
		tag is not null && tag.StartsWith(Espanol, StringComparison.OrdinalIgnoreCase)
			? Espanol
			: Ingles;

	// ONE PHRASE, WHOLE, WITH ITS VALUES PUT IN.
	//
	// The values travel BY NAME and never by position. Word order changes between Spanish and
	// English, so a {0} that moves is the classic localisation bug: it compiles, it runs, and it
	// says something false. A name cannot move.
	//
	// A key that is not in the catalog RETURNS THE KEY. That is on purpose: a missing phrase has
	// to be ugly on screen, because a blank looks like a design decision and gets shipped.
	public static string T(string clave, params (string Nombre, string Valor)[] valores)
	{
		string frase = Frase(clave);
		if (valores.Length == 0) return frase;

		StringBuilder sb = new(frase);
		foreach ((string nombre, string valor) in valores)
			sb.Replace("{" + nombre + "}", valor);
		return sb.ToString();
	}

	// THE CATALOG OF THE CURRENT LANGUAGE, FLAT, AS JSON. This is what the shell pushes into
	// ui.html at startup -through the bridge that already exists- so the page never has to go
	// looking for a file. It cannot: ui.html is loaded as file://, so a fetch to another file://
	// is refused by CORS on Android (AllowFileAccessFromFileURLs is false and nobody turns it
	// on) and WKWebView is stricter still.
	public static string Json
	{
		get
		{
			// LA PRIMERA CLAVE ES EL IDIOMA MISMO. La pagina la necesita para lo que NO es una
			// frase -- como se escribe una fecha y un numero -- y pedirsela al catalogo evita que
			// la adivine por su cuenta, que serian dos verdades sobre lo mismo.
			StringBuilder sb = new("{\"idioma\":");
			sb.Append(JsonSerializer.Serialize(Idioma)).Append(',');
			bool primera = true;
			foreach ((string clave, string frase) in DelIdioma())
			{
				if (!primera) sb.Append(',');
				primera = false;
				sb.Append(JsonSerializer.Serialize(clave)).Append(':').Append(JsonSerializer.Serialize(frase));
			}
			return sb.Append('}').ToString();
		}
	}

	// EVERY KEY THAT IS MISSING A LANGUAGE. Nothing calls this in production: it is for the
	// suite and for whoever does a copy pass, so that an untranslated key is something a build
	// can point at instead of something a person finds on a phone.
	public static IEnumerable<string> Faltantes()
	{
		foreach ((string clave, (string es, string en)) in Catalogo())
		{
			if (es.Length == 0) yield return $"{clave}: falta 'es'";
			if (en.Length == 0) yield return $"{clave}: falta 'en'";
		}
	}

	// TODA CLAVE CUYOS DOS IDIOMAS NO PIDEN LOS MISMOS VALORES.
	//
	// Es el error clasico de un catalogo y no lo ve ningun compilador: alguien traduce
	// «ya tienes «{titulo}» en Mymi» y escribe «{title}», o se come el marcador. La frase sale
	// con un {titulo} crudo en la pantalla, o peor, sale sin el dato y parece correcta.
	//
	// Compara CONJUNTOS y no orden: que el orden cambie entre idiomas es justamente por lo que
	// los valores viajan con nombre.
	public static IEnumerable<string> Discrepancias()
	{
		foreach ((string clave, (string es, string en)) in Catalogo())
		{
			var enEs = Marcadores(es);
			var enEn = Marcadores(en);
			if (enEs.SetEquals(enEn)) continue;

			string faltanEnIngles = string.Join(", ", enEs.Except(enEn));
			string faltanEnEspanol = string.Join(", ", enEn.Except(enEs));
			yield return $"{clave}: "
				+ (faltanEnIngles.Length > 0 ? $"'en' no usa {faltanEnIngles}. " : "")
				+ (faltanEnEspanol.Length > 0 ? $"'es' no usa {faltanEnEspanol}." : "");
		}
	}

	// TODA CLAVE CUYOS DOS IDIOMAS DICEN EXACTAMENTE LO MISMO.
	//
	// Casi siempre es un copiar-pegar a medio terminar: alguien pego la frase en ingles en las
	// dos columnas y tradujo una sola. Ni Faltantes() ni Discrepancias() lo ven -- las dos
	// columnas estan llenas y ninguna tiene marcadores de menos -- asi que la frase sale en el
	// idioma equivocado y con cara de estar hecha. Paso escribiendo este mismo catalogo.
	//
	// No es un error automatico: un nombre propio o un simbolo pueden ser identicos en los dos.
	// Por eso DEVUELVE LA LISTA y no un bool: quien la lea decide.
	public static IEnumerable<string> Identicas()
	{
		foreach ((string clave, (string es, string en)) in Catalogo())
			if (es.Length > 0 && es == en) yield return clave;
	}

	private static HashSet<string> Marcadores(string frase)
	{
		HashSet<string> nombres = new(StringComparer.Ordinal);
		foreach (System.Text.RegularExpressions.Match m in
			System.Text.RegularExpressions.Regex.Matches(frase, @"\{([A-Za-z][A-Za-z0-9]*)\}"))
			nombres.Add(m.Groups[1].Value);
		return nombres;
	}

	private static string Frase(string clave) =>
		DelIdioma().TryGetValue(clave, out string? frase) && frase.Length > 0 ? frase : clave;

	private static Dictionary<string, string>? plano;
	private static string? planoDe;

	private static Dictionary<string, string> DelIdioma()
	{
		string idioma = Idioma;
		if (plano is not null && planoDe == idioma) return plano;

		Dictionary<string, string> d = new(StringComparer.Ordinal);
		foreach ((string clave, (string es, string en)) in Catalogo())
		{
			// A key with no phrase in this language falls back to the other one rather than
			// disappearing: half-translated is a state this catalog will be in for a while, and
			// a missing sentence is worse than a sentence in the wrong language.
			string frase = idioma == Espanol ? es : en;
			d[clave] = frase.Length > 0 ? frase : (idioma == Espanol ? en : es);
		}

		planoDe = idioma;
		return plano = d;
	}

	private static Dictionary<string, (string Es, string En)>? catalogo;

	// READ WITH JsonDocument AND NOT Deserialize<T>, and that is not a style choice. Reflection
	// based deserialisation does not survive NativeAOT, which is the documented path for
	// embedding the Host in a Swift app -the whole reason HostJson.cs exists. JsonDocument walks
	// the tree without reflection, so the trimmer cannot take anything this needs.
	private static Dictionary<string, (string, string)> Catalogo()
	{
		if (catalogo is not null) return catalogo;

		Dictionary<string, (string, string)> d = new(StringComparer.Ordinal);
		using Stream? s = typeof(Textos).Assembly.GetManifestResourceStream(Recurso);

		// A face whose csproj forgot the EmbeddedResource arrives here. It does not throw: every
		// key would then render as itself, which is a screen full of 'instituciones.titulo' --
		// unmistakable, and better than a Host that will not start because of a text file.
		if (s is null) return catalogo = d;

		using JsonDocument doc = JsonDocument.Parse(s);
		foreach (JsonProperty entrada in doc.RootElement.EnumerateObject())
		{
			// The '//' notes of the file, same convention as every other json in this repo.
			if (entrada.Name.StartsWith("//", StringComparison.Ordinal)) continue;
			d[entrada.Name] = (Texto(entrada.Value, Espanol), Texto(entrada.Value, Ingles));
		}

		return catalogo = d;
	}

	private static string Texto(JsonElement entrada, string idioma) =>
		entrada.ValueKind == JsonValueKind.Object && entrada.TryGetProperty(idioma, out JsonElement v)
			? v.GetString() ?? ""
			: "";

	// THE SAME NAME IN THE THREE ASSEMBLIES. Declared with LogicalName in Gnothi.Host.csproj,
	// Gnothi.Mobile.csproj and Gnothi.iOS.csproj; without the fixed name each one would compute
	// its own from its RootNamespace and this lookup would need three cases.
	private const string Recurso = "gnothi.textos.json";
}
