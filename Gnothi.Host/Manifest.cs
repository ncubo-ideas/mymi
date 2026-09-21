using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gnothi.Host;

// THE MANIFEST BELONGS TO THE MODULE. The institution writes it, and some day will sign it.
// It declares identity, which substrate it needs, which capabilities it asks for, and -the
// part this POC cares about- ITS LIST OF TOOLS.
//
// Why the list comes from here instead of being asked of the module by starting it up:
// reading a json costs microseconds and wakes nothing. The Host has to be able to build the
// catalog of twenty modules without instantiating twenty runtimes. The module only starts
// when somebody calls one of its tools.
//
// And because it is a DECLARATION, it can be verified: later on the Host asks the module for
// its real tools/list and compares. What was declared against what is.
public sealed record Manifest
{
	[JsonPropertyName("manifest")] public string Version { get; init; } = "";
	[JsonPropertyName("module")] public string Module { get; init; } = "";
	[JsonPropertyName("title")] public string Title { get; init; } = "";
	[JsonPropertyName("institution")] public string Institution { get; init; } = "";

	// WHERE THE INSTITUTION PUBLISHES, as an origin: scheme://host[:port]. It declares it; the
	// enrollment compares it with the origin that actually served the bundle, and asks that
	// origin -at /.well-known/gnothi/modules.json- whether the bundle is its own. The three have
	// to agree for the sheet to say the module is backed by its origin. Optional: a manifest
	// without it still installs, and the sheet says it arrived without a verifiable backing.
	[JsonPropertyName("origin")] public string Origin { get; init; } = "";
	[JsonPropertyName("substrate")] public Substrate? Substrate { get; init; }
	[JsonPropertyName("capabilities")] public Capabilities? Capabilities { get; init; }

	// WHAT THE INSTITUTION SAYS ABOUT ITSELF (format 1.1, 2026-09-08). All four groups are OPTIONAL:
	// a manifest that does not carry them installs just the same and looks the way it looked. That is
	// not courtesy, it is what lets the Host introduce them without orphaning anybody.
	//
	// And `brand` is optional in an even stronger sense: if it is absent, the branding comes from the
	// `manifest.webmanifest` the PWA ALREADY carries in its bundle -- theme_color and icons -- so a
	// 1.0 manifest gets painted with its own branding without being touched. Declaring `brand` becomes
	// a way of refining that, not of enabling it.
	//
	// THEY ARE GROUPED AND NOT LOOSE AT THE ROOT because a group can be ignored whole, versioned
	// whole and -- when signing arrives -- signed whole.
	//
	// THE RULE THAT STOPS THE NEXT FIELD, and it is worth reading before adding one: the manifest
	// declares VERIFIABLE FACTS ABOUT THE INSTITUTION. Never instructions for the Host, and never
	// instructions for the model. That is why there is no -- and will be no -- 'assistantHint',
	// 'tone' or 'persona': that would be prompt injection in the shape of a contract, written by a
	// third party and delivered by Gnothi.
	[JsonPropertyName("brand")] public Brand? Brand { get; init; }
	[JsonPropertyName("publisher")] public Publisher? Publisher { get; init; }
	[JsonPropertyName("about")] public About? About { get; init; }
	[JsonPropertyName("journal")] public JournalDeclaration? Journal { get; init; }

	// Where this institution's publisher is, for the postman. Optional: a module can live off
	// nothing but what it already has in its journal.
	[JsonPropertyName("publicador")] public Publicador? Publicador { get; init; }
	[JsonPropertyName("tools")] public IReadOnlyList<DeclaredTool> Tools { get; init; } = [];

	// THE DECLARATIVE VIEW, optional. The name of a json file, shipped inside the module, that
	// describes the screen the institution designed for the PERSON -not for the assistant. The
	// module describes; the Host renders. A module with no view is legitimate: it just answers
	// queries and has no screen of its own. It is a file name, not the view itself, because the
	// catalog is built from manifests alone and must not pay to parse every screen up front.
	[JsonPropertyName("view")] public string View { get; init; } = "";

	// HOW A CARGO MODULE IS LOADED. Present only for cargo modules (two small DLLs + the Host
	// lends the engine); absent for stdio ones, which carry their own server. It says which
	// assembly holds the surface -the Host loads that one and finds the [ModuleSurface] class in
	// it, resolving the domain next door. It belongs to the manifest because "which DLL is me"
	// is identity, declared by the institution, not something the Host grants.
	[JsonPropertyName("load")] public Load? Load { get; init; }

	// THE LINE THAT IDENTIFIES THIS INSTITUTION ON THE SCREEN, resolved ONCE.
	//
	// It lives here and not in each face for the same reason the icon should never have lived in a
	// switch: there are three faces -- the two phones and the desktop -- and a fallback implemented
	// three times is a fallback that diverges. The rule is simple and the institution does not decide
	// it: if it declared a short name, that one is used, and if not, the 'institution' that was
	// always there.
	//
	// What it does NOT do is invent: if the manifest says nothing, this returns empty and the face
	// decides what to put. The Host gives nobody a name.
	public string Identity
		=> Brand?.ShortName is { Length: > 0 } s ? s : Institution;

	// EVERYTHING THAT OVERRUNS THE LIMITS, as a list. Same verb as Capabilities.Excesses: the Host
	// does not reject the module over a long title -- that would leave the person without their
	// laboratory over one line -- but it SAYS SO, and says it where these things already get said, in
	// the catalogue's warnings.
	//
	// Que sea una lista y no un bool es la parte que importa: un tope que no se puede nombrar no
	// se puede arreglar, y quien tiene que arreglarlo es la institucion, que no ve este proceso.
	//
	// ESTOS MENSAJES VAN EN INGLES Y NO PASAN POR Textos.cs, y es una decision, no un olvido.
	// Hablan de 'brand.shortName' y de 'about.category': son diagnostico para QUIEN EMPAQUETA EL
	// MODULO, no copy para la persona que lo usa. Traducirlos seria traducir un mensaje de
	// compilador.
	//
	// Estaban en espanol y ModuleCatalog.Load los envuelve en un prefijo ingles, asi que salian
	// mezclados con los suyos EN LA MISMA LISTA -- y esa lista se pinta entera en la ventana del
	// escritorio (ModuleList.razor). Un idioma por lista; el que ya hablaba el resto de este
	// proyecto.
	//
	// Que la persona los vea es una decision aparte y sigue abierta: ver el comentario de
	// ModuleCatalog.Load donde se toma.
	public IEnumerable<string> Problems()
	{
		if (Txt(Title).Length > MaxTitle)
			yield return $"title is {Txt(Title).Length} characters and the cap is {MaxTitle}";

		foreach (string p in Brand?.Problems() ?? []) yield return p;
		foreach (string p in Publisher?.Problems() ?? []) yield return p;
		foreach (string p in About?.Problems() ?? []) yield return p;
	}

	public const int MaxTitle = 32;

	// A PROPERTY THE JSON DOES NOT CARRY ARRIVES NULL, NOT WITH ITS INITIALISER, which is why
	// everything this file touches of a manifest goes through here.
	//
	// Measured on 2026-09-08: a manifest with "about": { "category": "..." } and no "description"
	// blew About.Problems() up with a NullReferenceException on Description.Length, even though the
	// property declares = "". The catalogue caught it -- a manifest that blows up is skipped with a
	// warning -- so the symptom was not a crash, it was A MODULE DISAPPEARING from the screen for not
	// having written an OPTIONAL field. The worse of the two.
	//
	// And there is a hint this had been hit before: Manifest.Read validates the module with
	// IsNullOrWhiteSpace and not with Length == 0. Now it is said out loud.
	//
	// THE RULE FOR THE NEXT FIELD: in a manifest, no string can be trusted. Not even with an
	// initialiser, because the JSON can also carry an EXPLICIT null -- and no default can prevent that.
	internal static string Txt(string? s) => s ?? "";

	public static Manifest Read(string path)
	{
		Manifest? m = JsonSerializer.Deserialize(File.ReadAllText(path), HostJson.Default.Manifest);
		if (m is null) throw new InvalidDataException($"empty or unreadable manifest: {path}");
		if (string.IsNullOrWhiteSpace(m.Module))
			throw new InvalidDataException($"the manifest does not declare 'module': {path}");
		return m;
	}

	internal static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNameCaseInsensitive = true,
		ReadCommentHandling = JsonCommentHandling.Skip,
		AllowTrailingCommas = true,
	};
}

// A RANGE, NOT an exact version. If a module pins one precise version, every Host update
// orphans every installed module.

// WHERE THIS INSTITUTION'S NARRATIVE LIVES. IT declares it in its manifest, and Gnothi records it
// when the module is enrolled: from then on the endpoint is FIXED and the module neither chooses it
// nor can change it (specification 11 §6, 13 §7).
//
// The module never uses it: it has no network. The postman uses it, and the postman is Gnothi.
public sealed record Publicador
{
	// The base of the institution's event service.
	[JsonPropertyName("eventos")] public string Eventos { get; init; } = "";

	// Which of the person's resources, and which one. The app asks for ONE single URL shape over
	// this: {recurso}/{id}/events?after=N, and nothing else.
	[JsonPropertyName("recurso")] public string Recurso { get; init; } = "expediente";

	[JsonPropertyName("id")] public string Id { get; init; } = "1";
}

// THE BRANDING, AND IT IS THE ONLY THING IN THIS FILE THAT REACHES THE EYE WITHOUT BEING READ. Until
// now the icon was picked by the Host with a switch on the module's name -- three copies of the same
// switch -- so a module from an institution the Host did not know came in anonymous. That contradicts
// dynamic enrolment: adding a module is two json files, not two json files and a Host release.
//
// THE DIVIDING LINE: the institution DECLARES, the Host COMPOSES. It provides the material; the Host
// decides the placement, the size and the contrast. That is why there is not one measurement nor one
// style rule here: there is a file and a colour.
public sealed record Brand
{
	// THE NAME PEOPLE KNOW IT BY -- 'La Guaria', not 'Supermercado La Guaria S.A.'. It is the line
	// that IDENTIFIES on the card, which is why it carries a limit: a line of 28 characters is what
	// fits without cutting in the phone's two-column grid.
	[JsonPropertyName("shortName")] public string ShortName { get; init; } = "";

	// A FILE NAME INSIDE THE BUNDLE -- relative to it, like «web/icon-192.png» --, not bytes and NOT
	// A URL. A URL would be a beacon: it would tell the institution every time the person opens their
	// list of modules, and that breaks the central property -- Gnothi asks no company anything -- over
	// a drawing.
	//
	// The bytes are read by the Host, validated, and passed to the screen already validated.
	//
	// SVG YES, and this corrects an argument that used to be written here: «the format executes» is
	// true for an svg INLINE in the DOM, and false for one travelling as a data URI in an `<img>`,
	// which is how it arrives -- there the browser isolates it and runs no scripts. What does change
	// with svg is that it has no pixels: it is vectorial and the container scales it, so the dimension
	// limit does not apply to it and the weight limit does.
	[JsonPropertyName("logo")] public string Logo { get; init; } = "";

	// ONE COLOUR, just one, in #rrggbb. The Host uses it where it decides -- today the icon's tint --
	// and guarantees the contrast. Never CSS: that would let the institution write styling on the very
	// screen where the person grants and revokes its permissions.
	[JsonPropertyName("color")] public string Color { get; init; } = "";

	// A SHORT LINE IN THE INSTITUTION'S OWN VOICE, which goes under its name on the card instead of
	// the module's title: «My invoices» is not card prose. It is what the person reads to know who is
	// talking to them, which is why it carries a limit like everything that reaches the eye.
	[JsonPropertyName("slogan")] public string Slogan { get; init; } = "";

	// THE BANNER (2026-09-09): a wide picture the Host puts BEHIND the card, with the institution's
	// name and slogan over it. Same rules as the logo -a file inside the bundle, never a URL, read
	// and validated by the Host- with wider tops because it is a photo, not an icon. No svg: a
	// banner has pixels or it is not a banner.
	[JsonPropertyName("banner")] public string Banner { get; init; } = "";

	// THE TYPEFACE (2026-09-09): the family the institution's name and slogan are set in, on its
	// own card and detail, so La Guaria reads like La Guaria and Sotaventos like Sotaventos. A font
	// file inside the bundle (woff2, woff, ttf or otf), optionally a second one for bold. The Host
	// embeds it as a data URI under a name of its own: no CSS from the institution ever runs.
	[JsonPropertyName("font")] public BrandFont? Font { get; init; }

	public const int MaxShortName = 28;
	public const int MaxSlogan = 40;
	public const int MaxLogoBytes = 64 * 1024;
	public const int MaxLogoPixels = 512;
	public const int MaxBannerBytes = 200 * 1024;
	public const int MaxBannerPixels = 1600;
	public const int MaxFontBytes = 160 * 1024;
	public const int MaxFontFamily = 40;

	private static readonly string[] AllowedBanner = [".png", ".jpg", ".jpeg", ".webp"];
	private static readonly string[] AllowedFont = [".woff2", ".woff", ".ttf", ".otf"];

	// A path that stays inside the bundle: relative, no '..', no drive, no scheme.
	public static bool InsideBundle(string path)
		=> !(path.Contains("..", StringComparison.Ordinal) || Path.IsPathRooted(path) || path.Contains(':'));

	// The svg gets in for the reason given above: it travels in an `<img>`, not inline. And a PWA's
	// webmanifest usually declares png, so the default path does not use it.
	private static readonly string[] AllowedLogo = [".png", ".jpg", ".jpeg", ".webp", ".svg"];

	// A COLOUR OR NOTHING, and the caller decides what to do with the nothing. It returns the same
	// string if it is a real #rrggbb and null if not, because the colour is going to end up in a
	// `style` attribute of the page that holds the consent bridge: nothing gets in there unlooked at.
	//
	// It lives here -- and not in the page nor in the webmanifest reader -- because all three paths
	// that bring a colour have to apply the same rule. One that relaxes it is the hole.
	public static string? ValidColor(string? c)
		=> c is { Length: 7 } s && s[0] == '#' && s[1..].All(char.IsAsciiHexDigit) ? s : null;

	// What can be checked WITHOUT opening the file. The bytes -- the weight, the dimensions, whether
	// the header really is what the extension claims -- are checked by whoever reads them, because
	// this type does not know where the module's folder is.
	public IEnumerable<string> Problems()
	{
		if (Manifest.Txt(ShortName).Length > MaxShortName)
			yield return $"brand.shortName is {Manifest.Txt(ShortName).Length} characters and the cap is {MaxShortName}";

		if (Manifest.Txt(Slogan).Length > MaxSlogan)
			yield return $"brand.slogan is {Manifest.Txt(Slogan).Length} characters and the cap is {MaxSlogan}";

		string logo = Manifest.Txt(Logo);
		if (logo.Length > 0)
		{
			if (logo.Contains("..", StringComparison.Ordinal) || Path.IsPathRooted(logo) || logo.Contains(':'))
				yield return $"brand.logo must be a path inside the module, and '{logo}' is not";
			else if (!AllowedLogo.Contains(Path.GetExtension(logo), StringComparer.OrdinalIgnoreCase))
				yield return $"brand.logo is '{Path.GetExtension(logo)}' and only png, jpg and webp are accepted -- an svg executes";
		}

		string color = Manifest.Txt(Color);
		if (color.Length > 0 && ValidColor(color) is null)
			yield return $"brand.color must be #rrggbb and it is '{color}'";

		string banner = Manifest.Txt(Banner);
		if (banner.Length > 0)
		{
			if (!InsideBundle(banner))
				yield return $"brand.banner must be a path inside the module, and '{banner}' is not";
			else if (!AllowedBanner.Contains(Path.GetExtension(banner), StringComparer.OrdinalIgnoreCase))
				yield return $"brand.banner is '{Path.GetExtension(banner)}' and only png, jpg and webp are accepted";
		}

		if (Font is not null)
		{
			if (Manifest.Txt(Font.Family).Length == 0)
				yield return "brand.font declares a file without saying the family (family)";
			else if (Manifest.Txt(Font.Family).Length > MaxFontFamily)
				yield return $"brand.font.family is {Manifest.Txt(Font.Family).Length} characters and the cap is {MaxFontFamily}";
			foreach ((string key, string file) in new[] { ("file", Manifest.Txt(Font.File)), ("bold", Manifest.Txt(Font.Bold)) })
			{
				if (file.Length == 0) { if (key == "file") yield return "brand.font does not say which file (file)"; continue; }
				if (!InsideBundle(file))
					yield return $"brand.font.{key} must be a path inside the module, and '{file}' is not";
				else if (!AllowedFont.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
					yield return $"brand.font.{key} is '{Path.GetExtension(file)}' and only woff2, woff, ttf and otf are accepted";
			}
		}
	}
}

// The institution's typeface: a family name and the file(s) inside the bundle that carry it. A
// variable font covers every weight with one file; a static family gives `file` for regular and
// `bold` for the name.
public sealed record BrandFont
{
	[JsonPropertyName("family")] public string Family { get; init; } = "";
	[JsonPropertyName("file")] public string File { get; init; } = "";
	[JsonPropertyName("bold")] public string Bold { get; init; } = "";
}

// WHERE THIS MODULE COMES FROM AND WHICH VERSION IT IS. It is the group that gives the branding its
// meaning: a logo with no provenance is a drawing anybody can copy.
//
// THE LEGAL NAME LIVES HERE, and not in 'institution', on purpose. Nobody says "I'm going to
// Supermercado La Guaria S.A.": the legal name and the tax id are not what a person recognises anyone
// by, they are WHAT GETS CHECKED. That is why they sit next to the domain, which is the check.
public sealed record Publisher
{
	// THE ONLY CONTRASTABLE FIELD IN THE WHOLE GROUP, and therefore the most valuable. Gnothi already
	// knows which host the bundle came down from -- it OBSERVED it, nobody declared it -- so it can
	// compare. It is the same pattern as gnothi_verify_module: declared against real.
	[JsonPropertyName("domain")] public string Domain { get; init; } = "";

	[JsonPropertyName("legal")] public string Legal { get; init; } = "";
	[JsonPropertyName("taxId")] public string TaxId { get; init; } = "";
	[JsonPropertyName("jurisdiction")] public string Jurisdiction { get; init; } = "";

	// THE MODULE'S VERSION, which is not the format's: 'manifest' is the format and this is the
	// module. Without it, on reinstalling the person can learn that they already have it but not
	// whether what is coming down is newer, which is the question they are asking.
	[JsonPropertyName("version")] public string Version { get; init; } = "";
	[JsonPropertyName("released")] public string Released { get; init; } = "";
	[JsonPropertyName("license")] public string License { get; init; } = "";

	public const int MaxLegal = 64;

	// WHETHER THE DECLARED DOMAIN MATCHES WHOEVER SERVED THE BUNDLE. An empty domain asserts nothing,
	// and asserting nothing is not lying: it returns true and the screen simply has nothing to show.
	//
	// It is compared by label suffix -- and not with Contains -- so that 'delfos.cr' accepts
	// 'eventos.delfos.cr' and rejects 'delfos.cr.malo.com', which is precisely the case a Contains
	// would approve.
	public bool MatchesServer(string servedBy)
	{
		string dom = Manifest.Txt(Domain);
		if (dom.Length == 0 || Manifest.Txt(servedBy).Length == 0) return true;
		return servedBy.Equals(dom, StringComparison.OrdinalIgnoreCase)
			|| servedBy.EndsWith("." + dom, StringComparison.OrdinalIgnoreCase);
	}

	public IEnumerable<string> Problems()
	{
		if (Manifest.Txt(Legal).Length > MaxLegal)
			yield return $"publisher.legal is {Manifest.Txt(Legal).Length} characters and the cap is {MaxLegal}";
		string dom = Manifest.Txt(Domain);
		if (dom.Contains('/') || dom.Contains(':'))
			yield return $"publisher.domain is a domain, not a url: '{dom}'";
	}
}

// WHAT THIS MODULE IS, IN WORDS FOR THE PERSON. Today the only thing they can read are the tool
// descriptions, which are written for the model: dense, in the third person, and talking about "the
// person" instead of to them. This group is the first text in the manifest written for whoever is
// going to read it.
public sealed record About
{
	// THE FALLBACK ICON COMES FROM HERE when there is no logo, and that is what lets the Host stop
	// knowing institutions by name. A closed list: a free-form category would go back to being a text
	// of the institution's on the screen, and the other fields are already there for that.
	[JsonPropertyName("category")] public string Category { get; init; } = "";

	[JsonPropertyName("description")] public string Description { get; init; } = "";

	// THE ANTI-CLAIM, and it is an idea from this very repo: the compras module already says "this
	// module does not judge anybody's diet", but it says it BURIED in a tool's description, where only
	// the AI reads it. Here the person reads it.
	[JsonPropertyName("doesNotDo")] public string DoesNotDo { get; init; } = "";

	// THE LANGUAGE OF THE RECORD -- a BCP 47 tag such as es-CR or en-US. It is a fact about the
	// institution's journal, not an instruction: the products, the analytes and the sentences
	// the domain composes are written in it. ToolIndex reads it and tells the model, on every
	// free-text parameter, in which language to pass its values; without it the model guesses
	// from the prose, and a guess of "milk" against a journal that says "leche" finds nothing.
	[JsonPropertyName("locale")] public string Locale { get; init; } = "";

	// THE CURRENCY, and it is the most concrete hole this group closes. The single facturas tool
	// carries, written into its description, "THE TOTAL HAS NO CURRENCY: the institution does not
	// report it in its events, so this module does not know it and neither do you". One field closes
	// that, and it also stops spending that paragraph in the person's tokens every turn.
	[JsonPropertyName("currency")] public string Currency { get; init; } = "";
	[JsonPropertyName("units")] public string Units { get; init; } = "";

	public const int MaxDescription = 180;
	public const int MaxDoesNotDo = 120;

	// THE CATEGORIES THE HOST KNOWS. It grows when a business turns up that does not fit, and one
	// that is not on the list falls back to the generic icon -- which is exactly what happens today
	// with ALL of them, because there are no categories.
	public static readonly string[] Known =
	[
		"laboratorio", "salud", "supermercado", "comercio", "banca", "seguros",
		"combustible", "taller", "telecom", "educacion", "gobierno", "servicios",
	];

	public IEnumerable<string> Problems()
	{
		if (Manifest.Txt(Description).Length > MaxDescription)
			yield return $"about.description is {Manifest.Txt(Description).Length} characters and the cap is {MaxDescription}";
		if (Manifest.Txt(DoesNotDo).Length > MaxDoesNotDo)
			yield return $"about.doesNotDo is {Manifest.Txt(DoesNotDo).Length} characters and the cap is {MaxDoesNotDo}";
		string cat = Manifest.Txt(Category);
		if (cat.Length > 0 && !Known.Contains(cat, StringComparer.OrdinalIgnoreCase))
			yield return $"about.category '{cat}' is not in the list the Host knows, so it falls to the generic icon";
	}
}

// WHAT THE INSTITUTION PROMISES ABOUT THE RECORD that is going to live on the person's device.
//
// THE FIRST THREE ARE FIXED WHEN THE JOURNAL IS CREATED and asking for another mode later throws.
// They are read by whoever creates the journal, not by the Host, and they are here because if the
// specification does not say so, some institution is going to discover two years in that switching
// encryption on means STARTING A NEW JOURNAL. Arriving late to these three is what is expensive.
//
// THE NEXT THREE are for the enrolment screen: it already tells the person WHERE the record is going
// to live, and not how much it is going to weigh, how often it updates or how long they keep it --
// which is what they ask before accepting.
public sealed record JournalDeclaration
{
	[JsonPropertyName("compression")] public string Compression { get; init; } = "None";
	[JsonPropertyName("encryption")] public string Encryption { get; init; } = "None";
	[JsonPropertyName("maxFileSize")] public long MaxFileSize { get; init; }

	[JsonPropertyName("retention")] public string Retention { get; init; } = "";
	[JsonPropertyName("expectedSize")] public string ExpectedSize { get; init; } = "";
	[JsonPropertyName("updates")] public string Updates { get; init; } = "";

	// AND THE ONE THAT CARRIES WEIGHT, which already existed in Cargas/manifiesto.json before the Host
	// read it: true if the journal can be fetched whole from the publisher again; false if it carries
	// contributions from the person that exist nowhere else, in which case deleting it LOSES DATA and
	// the Host cannot empty it for free.
	//
	// bool? and not bool: null is "the institution did not say", and that is not the same as "it can
	// be rebuilt". An optimistic default here would authorise emptying an unrecoverable journal.
	[JsonPropertyName("reconstruible")] public bool? Reconstruible { get; init; }
}

public sealed record Substrate
{
	[JsonPropertyName("range")] public string Range { get; init; } = "";
}

// WHERE THE CARGO MODULE'S SURFACE LIVES. A path relative to the module's own folder, e.g.
// "dominio/Examenes.App.dll". The Host loads exactly this assembly; its dependencies -the
// domain- are resolved next to it, and the engine is lent by the Host.
public sealed record Load
{
	[JsonPropertyName("entry")] public string Entry { get; init; } = "";
}

// What the module DECLARES it needs. The value of this is not informational: it is that the
// Host can check it against what the module actually asks for at run time. A module that
// declares 'network: false' and opens a socket is a module that lies, and that is detectable.
public sealed record Capabilities
{
	[JsonPropertyName("network")] public bool Network { get; init; }
	[JsonPropertyName("folder")] public string Folder { get; init; } = "own";
	[JsonPropertyName("speaker")] public bool Speaker { get; init; }
	[JsonPropertyName("microphone")] public bool Microphone { get; init; }

	public IEnumerable<string> Excesses()
	{
		if (Network) yield return "network";
		if (Speaker) yield return "speaker";
		if (Microphone) yield return "microphone";
		if (Folder != "own") yield return $"folder='{Folder}'";
	}
}

// A tool exactly as the module declares it. 'input' stays a list of simple parameters: this
// POC covers tools with no parameters and tools with text parameters, which is what the
// invoices module needs. The general case -an arbitrary input schema- calls for handling
// tools/call by hand instead of synthesizing a delegate, and is noted as pending.
public sealed record DeclaredTool
{
	[JsonPropertyName("name")] public string Name { get; init; } = "";
	[JsonPropertyName("title")] public string Title { get; init; } = "";
	[JsonPropertyName("description")] public string Description { get; init; } = "";
	[JsonPropertyName("readOnly")] public bool ReadOnly { get; init; } = true;
	[JsonPropertyName("input")] public IReadOnlyList<DeclaredParameter> Input { get; init; } = [];
}

public sealed record DeclaredParameter
{
	[JsonPropertyName("name")] public string Name { get; init; } = "";
	[JsonPropertyName("type")] public string Type { get; init; } = "string";
	[JsonPropertyName("description")] public string Description { get; init; } = "";

	// THE CLOSED SET OF VALUES, when the parameter is an enum. Optional: a text search or a free
	// number leaves it null and nothing changes. When present, ToolIndex puts it in the schema
	// as a real JSON Schema "enum", which is a far stronger signal to the model than the same
	// values buried in the prose of the description -- the exact gap the InputSchema comment
	// used to name as "a manifest decision". This is that decision: the values are DATA now.
	[JsonPropertyName("enum")] public IReadOnlyList<string>? Enum { get; init; }
}
