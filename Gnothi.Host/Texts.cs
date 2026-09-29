using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Gnothi.Host;

// WHAT THE HOST SAYS TO THE PERSON, IN ONE PLACE, IN TWO LANGUAGES.
//
// WHY IT LIVES HERE, of all projects. The Host's text does not come out of one file: it comes
// out of five projects at once. The page on the phones (Gnothi.Mobile/Assets/ui.html), the five
// .razor of the desktop, the enrollment sentences of Gnothi.MobileShared, the denial reasons of
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
public static class Texts
{
	// THE TWO LANGUAGES, AND ONLY TWO. The scope is a decision, not a limitation of this code:
	// several of the rules in the guide are written for exactly two, and a third one would
	// reopen them. English is the fallback because it is the one that never has to guess.
	public const string Spanish = "es";
	public const string English = "en";

	private static string? chosen;

	// THE LANGUAGE, RESOLVED ONCE. Every face sets it at startup from the DEVICE -that is the
	// decision: no selector inside Gnothi- and everything downstream reads it from here.
	//
	// The default is not "es": it is whatever the machine says, so a face that forgets to set
	// it still behaves. Anything that is not Spanish falls to English.
	//
	// The page does NOT resolve its own language with navigator.language. It is told. Two
	// resolvers would be two sources of truth, which is the thing this whole file exists to
	// avoid.
	public static string Language
	{
		get => chosen ??= FromTheDevice();
		set => chosen = Normalise(value);
	}

	private static string FromTheDevice() =>
		Normalise(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);

	private static string Normalise(string? tag) =>
		tag is not null && tag.StartsWith(Spanish, StringComparison.OrdinalIgnoreCase)
			? Spanish
			: English;

	// ONE PHRASE, WHOLE, WITH ITS VALUES PUT IN.
	//
	// The values travel BY NAME and never by position. Word order changes between Spanish and
	// English, so a {0} that moves is the classic localisation bug: it compiles, it runs, and it
	// says something false. A name cannot move.
	//
	// A key that is not in the catalog RETURNS THE KEY. That is on purpose: a missing phrase has
	// to be ugly on screen, because a blank looks like a design decision and gets shipped.
	public static string T(string key, params (string Name, string Value)[] values)
	{
		string phrase = Phrase(key);
		if (values.Length == 0) return phrase;

		StringBuilder sb = new(phrase);
		foreach ((string nombre, string valor) in values)
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
			// THE FIRST KEY IS THE LANGUAGE ITSELF. The page needs it for what is NOT a sentence
			// -- how a date and a number are written -- and asking the catalogue for it stops the
			// page guessing on its own, which would be two truths about the same thing.
			StringBuilder sb = new("{\"language\":");
			sb.Append(JsonSerializer.Serialize(Language)).Append(',');
			bool primera = true;
			foreach ((string key, string phrase) in OfTheLanguage())
			{
				if (!primera) sb.Append(',');
				primera = false;
				sb.Append(JsonSerializer.Serialize(key)).Append(':').Append(JsonSerializer.Serialize(phrase));
			}
			return sb.Append('}').ToString();
		}
	}

	// EVERY KEY THAT IS MISSING A LANGUAGE. Nothing calls this in production: it is for the
	// suite and for whoever does a copy pass, so that an untranslated key is something a build
	// can point at instead of something a person finds on a phone.
	public static IEnumerable<string> Missing()
	{
		foreach ((string key, (string es, string en)) in Catalogue())
		{
			if (es.Length == 0) yield return $"{key}: falta 'es'";
			if (en.Length == 0) yield return $"{key}: falta 'en'";
		}
	}

	// EVERY KEY WHOSE TWO LANGUAGES DO NOT ASK FOR THE SAME VALUES.
	//
	// It is a catalogue classic mistake and no compiler sees it: somebody translates
	// «ya tienes «{titulo}» en Mymi» and writes «{title}», or eats the placeholder. The sentence
	// comes out with a raw {titulo} on the screen, or worse, without the datum and looking right.
	//
	// It compares SETS and not order: that the order changes between languages is precisely why
	// the values travel by name.
	public static IEnumerable<string> Mismatches()
	{
		foreach ((string key, (string es, string en)) in Catalogue())
		{
			var enEs = Placeholders(es);
			var enEn = Placeholders(en);
			if (enEs.SetEquals(enEn)) continue;

			string faltanEnIngles = string.Join(", ", enEs.Except(enEn));
			string faltanEnEspanol = string.Join(", ", enEn.Except(enEs));
			yield return $"{key}: "
				+ (faltanEnIngles.Length > 0 ? $"'en' no usa {faltanEnIngles}. " : "")
				+ (faltanEnEspanol.Length > 0 ? $"'es' no usa {faltanEnEspanol}." : "");
		}
	}

	// EVERY KEY WHOSE TWO LANGUAGES SAY EXACTLY THE SAME THING.
	//
	// Almost always a half-finished copy-paste: somebody pasted the English sentence into both
	// columns and translated only one. Neither Missing() nor Mismatches() sees it -- both
	// columns are full and neither is short of placeholders -- so the sentence comes out in the
	// wrong language looking finished. It happened while writing this very catalogue.
	//
	// It is not automatically a mistake: a proper noun or a symbol can be identical in both. That
	// is why it RETURNS THE LIST and not a bool: whoever reads it decides.
	public static IEnumerable<string> Identical()
	{
		foreach ((string key, (string es, string en)) in Catalogue())
			if (es.Length > 0 && es == en && !Declared(key)) yield return key;
	}

	// A KEY MAY DECLARE THAT ITS TWO LANGUAGES ARE THE SAME ON PURPOSE, and it does so with an
	// "igual" field WHOSE VALUE IS THE REASON: «el lema de la marca no se traduce, igual que el
	// nombre». The exception lives beside the sentence and not in a separate list, which is where
	// exceptions go stale without anybody finding out.
	//
	// A bare `true` would have been enough for the compiler and not for whoever reads it in a year.
	private static bool Declared(string key) => sameOnPurpose.Contains(key);

	private static readonly HashSet<string> sameOnPurpose = new(StringComparer.Ordinal);

	private static HashSet<string> Placeholders(string phrase)
	{
		HashSet<string> nombres = new(StringComparer.Ordinal);
		foreach (System.Text.RegularExpressions.Match m in
			System.Text.RegularExpressions.Regex.Matches(phrase, @"\{([A-Za-z][A-Za-z0-9]*)\}"))
			nombres.Add(m.Groups[1].Value);
		return nombres;
	}

	private static string Phrase(string key) =>
		OfTheLanguage().TryGetValue(key, out string? phrase) && phrase.Length > 0 ? phrase : key;

	private static Dictionary<string, string>? flat;
	private static string? flatOf;

	private static Dictionary<string, string> OfTheLanguage()
	{
		string language = Language;
		if (flat is not null && flatOf == language) return flat;

		Dictionary<string, string> d = new(StringComparer.Ordinal);
		foreach ((string key, (string es, string en)) in Catalogue())
		{
			// A key with no phrase in this language falls back to the other one rather than
			// disappearing: half-translated is a state this catalog will be in for a while, and
			// a missing sentence is worse than a sentence in the wrong language.
			string phrase = language == Spanish ? es : en;
			d[key] = phrase.Length > 0 ? phrase : (language == Spanish ? en : es);
		}

		flatOf = language;
		return flat = d;
	}

	private static Dictionary<string, (string Es, string En)>? catalogue;

	// READ WITH JsonDocument AND NOT Deserialize<T>, and that is not a style choice. Reflection
	// based deserialisation does not survive NativeAOT, which is the documented path for
	// embedding the Host in a Swift app -the whole reason HostJson.cs exists. JsonDocument walks
	// the tree without reflection, so the trimmer cannot take anything this needs.
	private static Dictionary<string, (string, string)> Catalogue()
	{
		if (catalogue is not null) return catalogue;

		Dictionary<string, (string, string)> d = new(StringComparer.Ordinal);
		using Stream? s = typeof(Texts).Assembly.GetManifestResourceStream(Resource);

		// A face whose csproj forgot the EmbeddedResource arrives here. It does not throw: every
		// key would then render as itself, which is a screen full of 'instituciones.titulo' --
		// unmistakable, and better than a Host that will not start because of a text file.
		if (s is null) return catalogue = d;

		using JsonDocument doc = JsonDocument.Parse(s);
		foreach (JsonProperty entry in doc.RootElement.EnumerateObject())
		{
			// The '//' notes of the file, same convention as every other json in this repo.
			if (entry.Name.StartsWith("//", StringComparison.Ordinal)) continue;
			d[entry.Name] = (Text(entry.Value, Spanish), Text(entry.Value, English));
			if (Text(entry.Value, "igual").Length > 0) sameOnPurpose.Add(entry.Name);
		}

		return catalogue = d;
	}

	private static string Text(JsonElement entry, string language) =>
		entry.ValueKind == JsonValueKind.Object && entry.TryGetProperty(language, out JsonElement v)
			? v.GetString() ?? ""
			: "";

	// THE SAME NAME IN THE THREE ASSEMBLIES. Declared with LogicalName in Gnothi.Host.csproj,
	// Gnothi.Mobile.csproj and Gnothi.iOS.csproj; without the fixed name each one would compute
	// its own from its RootNamespace and this lookup would need three cases.
	private const string Resource = "gnothi.texts.json";
}
