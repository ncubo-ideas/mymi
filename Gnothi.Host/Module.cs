namespace Gnothi.Host;

// ONE MODULE INSTANCE: what the institution declared + what the Host granted it + the shell
// it is run with.
//
// This is the unit of isolation. In this POC the real isolation is whatever the operating
// system gives a child process -which is little- plus the folder of its own. Hard isolation
// (its own linear memory, --dir) arrives with Stage 2. Better not to claim that this POC
// demonstrates confinement: it demonstrates the aggregation link, which is another thing.
public sealed class Module : IAsyncDisposable
{
	private readonly IShell shell;

	public Module(Manifest manifest, Installation installation, string directory)
	{
		Manifest = manifest;
		Installation = installation;
		Directory = directory;
		shell = ChooseShell(manifest.Module, installation);
	}

	public Manifest Manifest { get; }
	// Three things about a module change while it is running, and each has its one named write
	// path: consent (Reconsent), the mark on its pin when the origin retires the key it was
	// accepted with (MarkSignerRevoked), and the note that the origin publishes a newer package
	// (NoteUpdate). Private setter. The shell and the granted folder are not reloaded, because
	// changing those is reinstalling, not reconsenting.
	public Installation Installation { get; private set; }
	public string Directory { get; }
	public string Name => Manifest.Module;
	public bool IsRunning => shell.IsRunning;

	// The name the tool travels under towards the assistant. Prefixed by module because two
	// institutions are going to call two different things 'total', and the assistant picks by
	// name and description.

	// THE FACTORY. This is where `Shell.Type` starts being read -- it was declared in
	// Installation.cs from the beginning and nobody consulted it: the shell was hardcoded.
	// Now the choice is PER MODULE, and it lives in installation.json, which is the Host's.
	//
	// Practical consequence: facturas can stay on stdio while examenes runs in-process, both
	// over the same journals. Migrating is changing one word in a json.
	private static IShell ChooseShell(string name, Installation installation) =>
		// ONE SEAM FOR THE PLATFORM, and only one. It is put there by whoever assembles the Host --
		// today Gnothi.Mobile, so a module can be run inside a box without rewriting the person's
		// installation.json -- and it returns null when it has no opinion, which is the normal case.
		//
		// IT DOES NOT CHANGE THE CONTRACT: the shell still comes out of the Host's file. What this
		// allows is substituting it ON PURPOSE, in one place and by name, instead of putting a lab
		// flag inside the switch.
		ShellFactory?.Invoke(name, installation) ??
		installation.Shell.Type.ToLowerInvariant() switch
		{
			// MOBILE: the module is the institution's PWA inside a WebView. It is the only form that
			// passes Google Play's TEXT for code arriving from outside, which is why on a phone
			// "in-process" probably cannot be published even though it works.
			//
			// THIS COMMENT USED TO SAY MORE THAN WAS KNOWN. It said that "Play and Apple name
			// JavaScript in a webview as THE exception". That came from Apple's old §3.3.2, which NO
			// LONGER EXISTS with that wording. All three texts read on 2026-09-16 (see
			// Documentacion/Jonathan/B-arquitectura/B11-lo-que-dicen-las-tiendas.md):
			//
			//   - Google Play IS clear, and the deciding word is "indirect": the WebView gives
			//     INDIRECT access to Android's APIs and is allowed; a dll loaded by Mono in this
			//     process gives DIRECT access and is not.
			//   - Apple no longer names an engine: it says "interpreted code", with three conditions.
			//     That is wider, and the third one -- not creating a store or storefront for other
			//     applications -- hits the whole of Gnothi, not one form.
			//   - And Apple's 4.7.2 -- not exposing native APIs to software not shipped in the binary
			//     without prior permission -- hits the HOSTED form TOO, which this comment took for
			//     granted. The bridge is exactly that.
			//
			// Which means: "webview" is the best positioned, not the clean one. None of this is a
			// verdict: they are texts that were read, not a review that was passed.
			"webview"    => new WebViewShell(name, installation.Shell, installation.GrantedFolder),

			// HYBRID: the face in the WebView and the surface in process, with the engine lent. It is
			// the only one of the three where the WebView does NOT answer: it asks. It inherits the
			// objection above whole, because the institution's dll runs natively.
			"hybrid"     => new HybridShell(name, installation.Shell, installation.GrantedFolder),

			"in-process" => new InProcessShell(name, installation.Shell, installation.GrantedFolder),

			// THE BOX: the same hybrid package, but the surface runs inside a wasm box instead of in
			// the Host's process. The face never finds out -- it goes on asking through `consultar` --
			// and what changes is that the institution's dll no longer has syscalls: whatever Gnothi
			// does not mount for it DOES NOT EXIST. See B12.
			"caja"       => new BoxShell(name, installation.Shell, installation.GrantedFolder),
			_            => new StdioShell(name, installation.Shell, installation.GrantedFolder),
		};

	// See ChooseShell. Null means «I have no opinion».
	internal static Func<string, Installation, IShell?>? ShellFactory { get; set; }

	public static string PublicName(string module, string tool) => $"{module}_{tool}";

	// The tools this module CONTRIBUTES to the catalog, already filtered by consent.
	//
	// Disabled returns empty: the module never finds out, and cannot tell "I was switched off"
	// apart from "nobody asked". That indistinguishability is the property, not a side effect.
	public IEnumerable<DeclaredTool> EnabledTools()
	{
		if (!Installation.Enabled) return [];
		return Manifest.Tools.Where(t => !Installation.DisabledTools.Contains(t.Name));
	}

	// RECONSENT: the person moved a switch and the Host found out by reading their file.
	//
	// Returns whether anything really changed, because a text editor writes the same file
	// three times and the assistant does not need to be told three times that its catalog
	// changed.
	//
	// Only the two consent fields come in. That the signature does not accept a whole
	// Installation is on purpose: it makes it IMPOSSIBLE for a shell change to sneak in
	// through here and pull the floor out from under a module that is already running.
	public bool Reconsent(bool enabled, IReadOnlyList<string> disabledTools)
	{
		bool sameSwitch = Installation.Enabled == enabled;
		bool sameDisabled = Installation.DisabledTools.Count == disabledTools.Count
			&& !Installation.DisabledTools.Except(disabledTools, StringComparer.Ordinal).Any();

		if (sameSwitch && sameDisabled) return false;

		Installation = Installation with { Enabled = enabled, DisabledTools = disabledTools };
		return true;
	}

	// THE ORIGIN RETIRED THE KEY THIS MODULE WAS ACCEPTED WITH, and Gnothi learnt it now: the mark
	// goes on the pin, in memory, mirroring what was just written to installation.json. Nothing
	// else moves -the switches stay as the person left them- and the screen repaints from here.
	// False when there is no pin, or it was already marked.
	public bool MarkSignerRevoked(string at)
	{
		if (Installation.Signer is not { RevokedAt: null } signer) return false;
		Installation = Installation with { Signer = signer with { RevokedAt = at } };
		return true;
	}

	// THE ORIGIN PUBLISHES A NEWER PACKAGE -or stopped doing so-, and Gnothi learnt it now: the note
	// goes on the installation in memory, mirroring what was just written to installation.json, and
	// the card shows it. Nothing downloads, nothing installs: that is hers to start. False when the
	// note did not change -the same version and seal already noted, or nothing to clear.
	public bool NoteUpdate(AvailableUpdate? update)
	{
		if (update is null ? Installation.Update is null : update.SameAs(Installation.Update)) return false;
		Installation = Installation with { Update = update };
		return true;
	}

	public bool Allows(string tool)
		=> Installation.Enabled && !Installation.DisabledTools.Contains(tool);

	public Task<object> Invoke(
		string tool, IReadOnlyDictionary<string, object?>? args, Accepts accepts, CancellationToken ct)
		=> shell.Invoke(tool, args, accepts, ct);

	// For the declared-against-real verification.
	public Task<IReadOnlyList<string>> ActualToolNames(CancellationToken ct)
		=> shell.ActualTools(ct);

	// WHAT THE POSTMAN BROUGHT. Gnothi did the networking and the module records it -- or cannot, and
	// the shell says so. It does not go through the tools' consent on purpose: switching off a query
	// decides WHAT GETS ANSWERED, not whether the person's records keep filling up.
	public Task<string> Deliver(string envelopeJson, CancellationToken ct)
		=> shell.Deliver(envelopeJson, ct);

	// HOW FAR IT HAS ALREADY READ. It does not go through consent either, for the same reason as the
	// delivery: it is the cursor of the person's records, not a query about them.
	public Task<long> Cursor(CancellationToken ct) => shell.Cursor(ct);

	// WHAT ITS OWN SCREEN ASKS FOR. It does not go through Allows and that is not an oversight:
	// switching off a query decides what THE ASSISTANT may ask about the person, it does not forbid
	// the institution from showing them their own file on the screen they just opened. They are
	// decisions with different subjects.
	public Task<object> View(
		string name, IReadOnlyDictionary<string, object?>? arguments, Accepts accepts, CancellationToken ct)
		=> shell.View(name, arguments, accepts, ct);

	// ── THE LOGO'S CUSTOMS HOUSE ────────────────────────────────────────────────────────
	//
	// THE INSTITUTION'S LOGO, READ AND VALIDATED BY THE HOST, returned as a data: URI ready for a
	// screen -- or empty, and then the face puts up the category's icon.
	//
	// WHY IT LIVES HERE: it is the same verb as BytesOnDisk. The module does not say how much it
	// weighs nor hand over its logo; the Host goes, measures it and looks at it. And it is in
	// Gnothi.Host and not in the phone's shell because all THREE faces -- the two phones and the
	// desktop -- need exactly this customs house, and a customs house implemented three times is a
	// customs house with two holes.
	//
	// WHY A data: URI AND NOT A PATH: the UI's page is served as file:// and cannot read other
	// file:// -- and even if it could, handing the screen a path of the module's would be letting the
	// module choose which file gets opened. This way the bytes that reach the screen are the ones
	// this method already approved, and nothing else.
	//
	// FAIL CLOSED, and it is the decision that keeps this code short: if something cannot be
	// determined -- the type, the width, the height -- IT DOES NOT PASS. A parser covering every
	// exotic WebP case is an attack surface; one that rejects what it does not understand is a line.
	// ── THE INSTITUTION'S FACE, RESOLVED ────────────────────────────────────────────────
	//
	// What the screen needs in order to paint a module: its logo -- already validated, as a data URI
	// -- and its colour. In this order of preference:
	//
	//   1. `brand` from the manifest: the explicit declaration, and it wins.
	//   2. THE `manifest.webmanifest` THE PWA ALREADY CARRIES IN ITS BUNDLE: `icons` and `theme_color`.
	//
	// THE SECOND ONE IS THE BEST PART AND IT IS NOT OUR IDEA: it comes from the branding branch, and
	// it does something the version that demanded declaring `brand.logo` did not -- a 1.0 bundle gets
	// painted with its own branding WITHOUT BEING TOUCHED, because every PWA already declares its
	// icons and its colour so the browser can install it. Declaring `brand` becomes a way of refining
	// that, not of enabling it.
	//
	// What this version adds on the other side: the webmanifest's icons go through the SAME customs
	// house as a declared logo. The `type` the webmanifest declares is an assertion by whoever wrote
	// it, and here the type comes from the bytes.
	public static BrandLook LookOf(Manifest manifest, string directory, out string problem)
	{
		string color = Brand.ValidColor(manifest.Brand?.Color) ?? "";
		string logo = LogoDataUri(manifest, directory, out problem);

		// The banner and the typeface only come declared: the webmanifest has nothing to say about
		// them. A file that does not pass leaves the card without it, and the reason is kept.
		string banner = BannerDataUri(manifest, directory, out string bannerProblem);
		string family = "", regular = "", bold = "";
		if (manifest.Brand?.Font is { } font && Manifest.Txt(font.Family).Length > 0)
		{
			regular = FontDataUri(directory, font.File, out string fontProblem);
			bold = FontDataUri(directory, font.Bold, out _);
			if (regular.Length > 0) family = Manifest.Txt(font.Family);
			else if (fontProblem.Length > 0) bannerProblem = (bannerProblem.Length > 0 ? bannerProblem + "; " : "") + "brand.font " + fontProblem;
		}
		if (problem.Length == 0) problem = bannerProblem;

		if (logo.Length > 0 && color.Length > 0) return new BrandLook(logo, color, banner, family, regular, bold);

		// Whatever `brand` did not say -- or said wrong -- is completed by the PWA's webmanifest.
		(string? webLogo, string? webColor) = FromWebManifest(directory);
		if (logo.Length == 0 && webLogo is not null)
		{
			logo = webLogo;
			// A declared logo that did not clear customs AND a webmanifest that did: the first one's
			// problem stops mattering to the screen, but it is not erased -- the institution has to be
			// able to find out that its file did not get in.
		}
		if (color.Length == 0 && webColor is not null) color = webColor;

		return new BrandLook(logo, color, banner, family, regular, bold);
	}

	// THE PWA'S WEBMANIFEST, which belongs to the institution and already travels in the bundle. A
	// crooked file brings nothing down: that module gets painted with its sector's drawing.
	private static (string? logo, string? color) FromWebManifest(string directory)
	{
		string web = Path.Combine(directory, "web");
		string path = Path.Combine(web, "manifest.webmanifest");
		if (!File.Exists(path)) return (null, null);

		try
		{
			using System.Text.Json.JsonDocument doc = System.Text.Json.JsonDocument.Parse(
				File.ReadAllText(path),
				new System.Text.Json.JsonDocumentOptions
				{
					CommentHandling = System.Text.Json.JsonCommentHandling.Skip,
					AllowTrailingCommas = true,
				});

			System.Text.Json.JsonElement root = doc.RootElement;

			string? color = root.TryGetProperty("theme_color", out System.Text.Json.JsonElement c)
				&& c.ValueKind == System.Text.Json.JsonValueKind.String
					? Brand.ValidColor(c.GetString())
					: null;

			return (PickIcon(root, web), color);
		}
		catch
		{
			return (null, null);
		}
	}

	// THE ICON IT PICKS: the 192 one with no `purpose` or with `any`, which is the one the browser
	// uses for the tab and the install dialogue. The `maskable` ones come out square and with the
	// symbol small because Android applies its own mask to them, and here there is no mask. If there
	// is no 192, the first one that clears customs.
	private static string? PickIcon(System.Text.Json.JsonElement root, string web)
	{
		if (!root.TryGetProperty("icons", out System.Text.Json.JsonElement icons)
			|| icons.ValueKind != System.Text.Json.JsonValueKind.Array) return null;

		List<(string src, bool any, bool is192)> candidates = [];
		foreach (System.Text.Json.JsonElement i in icons.EnumerateArray())
		{
			if (!i.TryGetProperty("src", out System.Text.Json.JsonElement s)
				|| s.ValueKind != System.Text.Json.JsonValueKind.String) continue;

			string src = s.GetString() ?? "";
			if (src.Length == 0) continue;

			string purpose = i.TryGetProperty("purpose", out System.Text.Json.JsonElement p)
				&& p.ValueKind == System.Text.Json.JsonValueKind.String ? p.GetString() ?? "" : "";
			string sizes = i.TryGetProperty("sizes", out System.Text.Json.JsonElement z)
				&& z.ValueKind == System.Text.Json.JsonValueKind.String ? z.GetString() ?? "" : "";

			bool any = purpose.Length == 0
				|| purpose.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains("any");
			candidates.Add((src, any, sizes.Contains("192", StringComparison.Ordinal)));
		}

		foreach ((string src, _, _) in candidates.OrderByDescending(x => x.any).ThenByDescending(x => x.is192))
		{
			// THROUGH THE SAME CUSTOMS HOUSE. The webmanifest's `type` is declared by whoever wrote
			// it; the real type comes out of the bytes.
			string uri = ReadImage(web, src, out _);
			if (uri.Length > 0) return uri;
		}
		return null;
	}

	public string LogoDataUri() => LogoDataUri(Manifest, Directory, out _);

	// THE STATIC FORM EXISTS BECAUSE OF ENROLMENT. When Gnothi shows the person what an
	// institution is proposing, the module is NOT INSTALLED YET: the bundle is unpacked into a
	// temporary folder and there is no Module at all. And that screen -- the yes-or-no one -- is
	// precisely where the logo matters most.
	//
	// It also says WHY it did not pass, so the reason reaches the warning and from there the
	// institution, which is the only one that can fix it.
	public static string LogoDataUri(Manifest manifest, string directory, out string problem)
	{
		problem = "";
		string declared = Manifest.Txt(manifest.Brand?.Logo);
		if (declared.Length == 0) return "";

		string uri = ReadImage(directory, declared, out problem);
		if (problem.Length > 0) problem = "brand.logo " + problem;
		return uri;
	}

	// THE CUSTOMS HOUSE, over any file in the module's folder. Both entrances use it -- the declared
	// logo and the webmanifest's icons -- because the risk is the same and a customs house with two
	// doors is a customs house with one door.
	private static string ReadImage(string baseDir, string src, out string problem)
		=> ReadImage(baseDir, src, Brand.MaxLogoBytes, Brand.MaxLogoPixels, out problem);

	// THE BANNER, through the same customs as the logo but with a photo's tops. Empty when the
	// institution declared none, and `problem` says why one that was declared did not get in.
	public static string BannerDataUri(Manifest manifest, string directory, out string problem)
	{
		problem = "";
		string declared = Manifest.Txt(manifest.Brand?.Banner);
		if (declared.Length == 0) return "";
		string uri = ReadImage(directory, declared, Brand.MaxBannerBytes, Brand.MaxBannerPixels, out problem);
		if (problem.Length > 0) problem = "brand.banner " + problem;
		return uri;
	}

	// THE TYPEFACE. The file is resolved inside the bundle like any other asset, weighed, and
	// its type comes from its first bytes -woff2, woff, TrueType or OpenType- not from its
	// extension. It leaves as a data URI the page can put in an @font-face of its own.
	public static string FontDataUri(string directory, string src, out string problem)
	{
		problem = "";
		if (Manifest.Txt(src).Length == 0) return "";
		string full = ResolveInside(directory, src, out problem);
		if (full.Length == 0) return "";

		var info = new FileInfo(full);
		if (info.Length == 0 || info.Length > Brand.MaxFontBytes)
		{
			problem = $"pesa {info.Length / 1024} KB y el tope es {Brand.MaxFontBytes / 1024} KB";
			return "";
		}

		byte[] bytes = File.ReadAllBytes(full);
		string? mime = SniffFont(bytes);
		if (mime is null)
		{
			problem = "no es un woff2, woff, ttf ni otf legible -- se rechaza por lo que dicen sus bytes";
			return "";
		}
		return $"data:{mime};base64,{Convert.ToBase64String(bytes)}";
	}

	private static string? SniffFont(byte[] b)
	{
		if (b.Length < 4) return null;
		string tag = System.Text.Encoding.ASCII.GetString(b, 0, 4);
		return tag switch
		{
			"wOF2" => "font/woff2",
			"wOFF" => "font/woff",
			"OTTO" => "font/otf",
			"true" => "font/ttf",
			_ => b[0] == 0 && b[1] == 1 && b[2] == 0 && b[3] == 0 ? "font/ttf" : null,
		};
	}

	// A path declared by the institution, resolved inside its own folder or refused. Shared by
	// every asset that comes in through the manifest: one door, one rule.
	private static string ResolveInside(string baseDir, string src, out string problem)
	{
		problem = "";
		if (Uri.TryCreate(src, UriKind.Absolute, out Uri? u) && !u.IsFile)
		{
			problem = $"apunta a la red ('{src}') y por aca no sale nada a la red";
			return "";
		}
		string full = Path.GetFullPath(Path.Combine(baseDir, src.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)));
		string root = Path.GetFullPath(baseDir) + Path.DirectorySeparatorChar;
		if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
		{
			problem = $"apunta fuera de la carpeta del modulo: '{src}'";
			return "";
		}
		if (!File.Exists(full)) { problem = $"declara '{src}' y ese archivo no esta en el bundle"; return ""; }
		return full;
	}

	private static string ReadImage(string baseDir, string src, long maxBytes, int maxPixels, out string problem)
	{
		problem = "";

		// A remote logo is not downloaded: nothing goes out to the network through here.
		if (Uri.TryCreate(src, UriKind.Absolute, out Uri? u) && !u.IsFile)
		{
			problem = $"apunta a la red ('{src}') y por aca no sale nada a la red";
			return "";
		}

		// THE PATH IS VALIDATED AGAIN HERE, even though Brand.Problems() already looks at it. It is not
		// redundancy: Problems only WARNS -- a limit does not bring a module down -- so a
		// `../../something/else.png` still gets this far. Whoever reads the bytes is the one who has to
		// say no. And the webmanifest, which does not even go through Problems, comes in the same door.
		string full = Path.GetFullPath(Path.Combine(baseDir,
			src.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)));
		string root = Path.GetFullPath(baseDir) + Path.DirectorySeparatorChar;
		if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
		{
			problem = $"apunta fuera de la carpeta del modulo: '{src}'";
			return "";
		}

		if (!File.Exists(full)) { problem = $"declara '{src}' y ese archivo no esta en el bundle"; return ""; }

		var info = new FileInfo(full);
		if (info.Length == 0 || info.Length > maxBytes)
		{
			problem = $"pesa {info.Length / 1024} KB y el tope es {maxBytes / 1024} KB";
			return "";
		}

		byte[] bytes = File.ReadAllBytes(full);

		// THE TYPE COMES OUT OF THE BYTES, NOT OUT OF THE EXTENSION NOR THE WEBMANIFEST'S `type`. Both
		// are declared by whoever wrote the file, and this is precisely the place where they are not
		// believed.
		string? mime = SniffImage(bytes, out int width, out int height);
		if (mime is null)
		{
			problem = "no es un png, jpeg, webp ni svg legible -- se rechaza por lo que dicen sus bytes";
			return "";
		}

		// THE PIXEL LIMIT IS NOT COSMETIC: a 3000x3000 png of a single colour fits in 25 KB and asks for
		// gigabytes when decoded. The weight limit does not catch it; this one does.
		//
		// An SVG has no pixels -- it is vectorial and the container scales it -- so the limit does not
		// apply there and SniffImage returns 0x0 on purpose.
		if (width > maxPixels || height > maxPixels)
		{
			problem = $"mide {width}x{height} y el tope es {maxPixels} de lado";
			return "";
		}

		return $"data:{mime};base64,{Convert.ToBase64String(bytes)}";
	}

	// WHAT THIS IS AND HOW BIG IT IS, read from the header. Returns null for anything that is not one
	// of the accepted formats, or whose size cannot be read.
	private static string? SniffImage(byte[] b, out int width, out int height)
	{
		width = height = 0;

		// PNG: an 8-byte signature and then the IHDR chunk, which is ALWAYS first and carries the width
		// and height in big-endian.
		if (b.Length >= 24
			&& b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47
			&& b[12] == 'I' && b[13] == 'H' && b[14] == 'D' && b[15] == 'R')
		{
			width = Be32(b, 16);
			height = Be32(b, 20);
			return width > 0 && height > 0 ? "image/png" : null;
		}

		// JPEG: the segments have to be walked until the SOF, which is the one carrying the dimensions.
		if (b.Length >= 4 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF)
		{
			int i = 2;
			while (i + 9 < b.Length)
			{
				if (b[i] != 0xFF) return null;              // out of sync: nothing is guessed
				byte marker = b[i + 1];
				if (marker == 0xFF) { i++; continue; }        // legitimate padding
				int len = Be16(b, i + 2);
				if (len < 2) return null;

				// SOF0..SOF15 minus the four that are not SOF (C4 DHT, C8, CC DAC).
				if (marker >= 0xC0 && marker <= 0xCF
					&& marker != 0xC4 && marker != 0xC8 && marker != 0xCC)
				{
					height = Be16(b, i + 5);
					width = Be16(b, i + 7);
					return width > 0 && height > 0 ? "image/jpeg" : null;
				}

				i += 2 + len;
			}
			return null;                                     // no SOF: its size cannot be known
		}

		// WebP: RIFF....WEBP and then the chunk, which says which of the three flavours it comes in.
		if (b.Length >= 30
			&& b[0] == 'R' && b[1] == 'I' && b[2] == 'F' && b[3] == 'F'
			&& b[8] == 'W' && b[9] == 'E' && b[10] == 'B' && b[11] == 'P')
		{
			string chunk = System.Text.Encoding.ASCII.GetString(b, 12, 4);

			if (chunk == "VP8X")                              // extended: 24 bits, minus one
			{
				width = (b[24] | b[25] << 8 | b[26] << 16) + 1;
				height = (b[27] | b[28] << 8 | b[29] << 16) + 1;
			}
			else if (chunk == "VP8 ")                         // lossy: 14 usable bits
			{
				width = (b[26] | b[27] << 8) & 0x3FFF;
				height = (b[28] | b[29] << 8) & 0x3FFF;
			}
			else if (chunk == "VP8L")                         // lossless: 14 bits, minus one
			{
				int bits = b[21] | b[22] << 8 | b[23] << 16 | b[24] << 24;
				width = (bits & 0x3FFF) + 1;
				height = (bits >> 14 & 0x3FFF) + 1;
			}
			else return null;

			return width > 0 && height > 0 ? "image/webp" : null;
		}

		// SVG: it is text, so there are no magic bytes -- what is looked at is that it STARTS like an
		// svg document, skipping the BOM and the whitespace. It comes out 0x0 on purpose: it has no
		// pixels, and the dimension limit does not apply to it.
		//
		// And it travels inside an `<img>`, not inline: there the browser isolates it and runs no
		// scripts. That is all the permission it has.
		int at = b.Length >= 3 && b[0] == 0xEF && b[1] == 0xBB && b[2] == 0xBF ? 3 : 0;
		while (at < b.Length && (b[at] == ' ' || b[at] == '\n' || b[at] == '\r' || b[at] == '\t')) at++;
		if (at + 5 <= b.Length)
		{
			string head = System.Text.Encoding.ASCII.GetString(b, at, Math.Min(512, b.Length - at));
			if (head.StartsWith("<svg", StringComparison.OrdinalIgnoreCase)
				|| (head.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase)
					&& head.Contains("<svg", StringComparison.OrdinalIgnoreCase)))
				return "image/svg+xml";
		}

		return null;
	}

	private static int Be32(byte[] b, int at) => b[at] << 24 | b[at + 1] << 16 | b[at + 2] << 8 | b[at + 3];
	private static int Be16(byte[] b, int at) => b[at] << 8 | b[at + 1];

	// The logo and the colour the screen is going to use, already resolved. Empty in either of the two
	// means "there is none", and the face falls back to whatever it has -- the sector's drawing, the
	// Host's accent. The Host does not invent branding the institution did not give.
	public sealed record BrandLook(string Logo, string Color, string Banner = "", string FontFamily = "",
		string FontRegular = "", string FontBold = "")
	{
		public bool Any => Logo.Length > 0 || Color.Length > 0 || Banner.Length > 0 || FontFamily.Length > 0;
	}

	// Measured FROM OUTSIDE, with stat over the folder the Host granted. The module is not
	// asked how much it takes up: a module that reports 4 MB while taking up 400 cannot exist
	// if nobody asks it.
	public long BytesOnDisk()
	{
		var granted = new DirectoryInfo(Installation.GrantedFolder);
		if (!granted.Exists) return 0;
		return granted
			.EnumerateFiles("*", SearchOption.AllDirectories)
			.Sum(f => f.Length);
	}

	public ValueTask DisposeAsync() => shell.DisposeAsync();
}
