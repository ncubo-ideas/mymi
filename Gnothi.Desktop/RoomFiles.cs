using System.Text;
using System.Text.Json;

namespace Gnothi.Desktop;

// WHAT THE ROOM SERVES, IN ONE PLACE.
//
// `modulo/<m>/indice.json`, `modulo/<m>/dominio/<x>.dll`, `modulo/<m>/journal/...` and the
// substrate. That is a **contract with the substrate**, not an engine's code: the names of those
// fields and paths are read by `Spikes/Lab.Sala/Sustrato/Sala.cs`, which is compiled apart and
// knows nothing of this.
//
// ── WHY THIS CLASS EXISTS, AND WHY ONLY NOW ───────────────────────────────────────────────────
//
// It was born because the desktop had TWO rooms for a day -- one over WebView2 and `PhotinoRoom`
// over a hidden iframe served by loopback -- and writing the same contract twice in the same
// assembly is the mistake this repo paid for twice in one week: a rule written twice has no owner,
// and the two copies diverge the day somebody touches one.
//
// **THE WEBVIEW2 ONE WAS DELETED ON 2026-09-25**: it served `--cara ventana`, which nobody passes.
// So today this class has ONE caller in this assembly, which leaves it without the reason it was
// born for. It stays anyway, for the one below: the other two callers are the phones.
//
// AND THERE ARE TWO MORE COPIES, in `AndroidRoom` and `IosRoom`, that were **not touched**. Not an
// oversight: `IosRoom` was renamed wholesale two days ago. (The note here used to add "and nobody
// has compiled it yet -- there is no Mac"; that was wrong, and it was corrected on 2026-09-28:
// `dotnet build Gnothi.iOS` compiles on Windows in 12,7 s. What still needs a Mac is linking,
// packaging and RUNNING.) Extracting those two stays noted for the day iOS runs once. See F2 · P26.
internal static class RoomFiles
{
	// WHAT THE SUBSTRATE ASKS FOR AND WHAT IT IS ANSWERED. The body, its type, the status and -- only
	// for the document -- the policy BundleServer composes over its content.
	internal sealed record Served(byte[] Body, string Type, int Status, string? Csp);

	// WHERE A MODULE'S BYTES LIVE: the folder of its dlls, which one is the entry, and its journal.
	internal sealed record Prepared(string Domain, string Entry, string Journal);

	// THE SHIM, SERVED AS A SAME-ORIGIN FILE and not embedded in the document.
	//
	// It is the solution this repo already found for the faces: `script-src 'self'` admits a
	// `<script src>` from its own origin WITHOUT a hash, whereas an inline script would force
	// recomposing the CSP that BundleServer has already computed over the document. See
	// FrameWebModule.
	internal const string ShimPath = "__gnothi/sala.js";

	// THE DOCUMENT ONLY ASKS FOR IT IF IT IS INJECTED, because `index.html` is THE SAME FILE the two
	// phones publish, and there the shim comes in through the platform. Here the tag is added when
	// serving it, before BundleServer computes the policy over the result -- computed earlier, the
	// tag would fall outside what is allowed and the document would come out blank.
	private const string ShimTag = "<script src=\"/" + ShimPath + "\"></script>";

	internal static Served Serve(string substrate, string path, Func<string, Prepared?> servable, Action<string> log)
	{
		if (path.StartsWith("modulo/", StringComparison.Ordinal))
		{
			string[] parts = path.Split('/', 4);
			if (parts.Length < 3) return NotFound(path, log);

			string module = parts[1];
			if (servable(module) is not Prepared p) return NotFound(path, log);

			if (parts[2] == "indice.json")
			{
				// ONLY WHAT BELONGS TO THE MODULE. Everything else the substrate lends, and that is
				// not an optimisation: a BCL package is compiled PER PLATFORM, and the one in the
				// module's folder is this machine's.
				string[] files = [.. Directory.EnumerateFiles(p.Domain, "*.dll")
					.Select(Path.GetFileNameWithoutExtension)
					.Where(n => n is not null && !LentByTheRoom(n))
					.Select(n => n!)
					.OrderBy(n => string.Equals(n, p.Entry, StringComparison.Ordinal) ? 1 : 0)];

				// `entrada` and `archivos` are JSON FIELDS, not C# properties: the substrate reads
				// them with GetProperty. They are named explicitly so an automatic rename cannot
				// carry them off -- which already happened on 2026-09-23.
				return Text(JsonSerializer.Serialize(new { entrada = p.Entry, archivos = files }), "application/json");
			}

			if (parts[2] == "dominio" && parts.Length == 4)
				return FromFile(Path.Combine(p.Domain, parts[3]), "application/octet-stream");

			if (parts[2] == "journal" && parts.Length == 4 && parts[3] == "indice.json")
			{
				string[] paths = Directory.Exists(p.Journal)
					? [.. Directory.EnumerateFiles(p.Journal, "*", SearchOption.AllDirectories)
						.Select(f => Path.GetRelativePath(p.Journal, f).Replace(Path.DirectorySeparatorChar, '/'))]
					: [];

				return Text(JsonSerializer.Serialize(new { rutas = paths }), "application/json");
			}

			// Y NO SE SALE DE LA CARPETA CONCEDIDA.
			if (parts[2] == "journal" && parts.Length == 4)
			{
				string root = Path.GetFullPath(p.Journal);
				string asked = Path.GetFullPath(Path.Combine(root, parts[3].Replace('/', Path.DirectorySeparatorChar)));
				if (!asked.StartsWith(root, StringComparison.Ordinal)) return NotFound(path, log);

				return FromFile(asked, "application/octet-stream");
			}

			return NotFound(path, log);
		}

		return FromSubstrate(substrate, path, log);
	}

	// THE SUBSTRATE, AND THE DOCUMENT WITH ITS POLICY. The order matters and is explained above:
	// first the shim's tag is added, then the CSP is computed over the resulting document.
	private static Served FromSubstrate(string substrate, string path, Action<string> log)
	{
		string full = Path.Combine(substrate, path.Replace('/', Path.DirectorySeparatorChar));
		if (!File.Exists(full)) return NotFound(path, log);

		byte[] body = File.ReadAllBytes(full);

		if (!path.EndsWith(".html", StringComparison.Ordinal))
			return new Served(body, MimeOf(path), 200, null);

		return new Served(body, MimeOf(path), 200, Gnothi.MobileShared.BundleServer.CspFor(body));
	}

	// THE SAME DOCUMENT, WITH THE SHIM INSIDE. There is no native way here to inject a script
	// before the document, so it travels in the markup.
	internal static Served WithShim(string substrate, Action<string> log)
	{
		string full = Path.Combine(substrate, "index.html");
		if (!File.Exists(full)) return NotFound("index.html", log);

		string html = File.ReadAllText(full);

		// BEFORE ANY OTHER SCRIPT, because the substrate decides whether it is hosted by looking at
		// `window.SalaPuente` the moment it starts. If the shim arrived later, it would run the
		// standalone laboratory sweep instead of sitting there waiting for requests.
		int head = html.IndexOf("<head", StringComparison.OrdinalIgnoreCase);
		int close = head >= 0 ? html.IndexOf('>', head) : -1;

		html = close >= 0
			? html[..(close + 1)] + ShimTag + html[(close + 1)..]
			: ShimTag + html;

		byte[] body = Encoding.UTF8.GetBytes(html);
		return new Served(body, "text/html", 200, Gnothi.MobileShared.BundleServer.CspFor(body));
	}

	private static Served FromFile(string path, string type) =>
		File.Exists(path)
			? new Served(File.ReadAllBytes(path), type, 200, null)
			: new Served([], "text/plain", 404, null);

	private static Served Text(string body, string type) =>
		new(Encoding.UTF8.GetBytes(body), type, 200, null);

	private static Served NotFound(string path, Action<string> log)
	{
		log($"sala: 404 {path}");
		return new Served([], "text/plain", 404, null);
	}

	// THE LIST OF WHAT IS LENT, the other face of the installer's forbidden list. **It has to stay
	// the same as AndroidRoom's and IosRoom's**: what is here the substrate provides in its
	// browser-wasm build, and what is not travels inside the module.
	private static bool LentByTheRoom(string assembly) =>
		assembly.StartsWith("System.", StringComparison.OrdinalIgnoreCase)
		|| assembly.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase)
		|| assembly is "Puppeteer" or "Choreography" or "Puppets.Portador" or "Gnothi.Contract"
		|| assembly.StartsWith("ModelContextProtocol", StringComparison.OrdinalIgnoreCase)
		|| assembly is "netstandard" or "mscorlib";

	internal static string MimeOf(string path) => Path.GetExtension(path).ToLowerInvariant() switch
	{
		".html" => "text/html",
		".js" or ".mjs" => "text/javascript",
		".css" => "text/css",
		".wasm" => "application/wasm",
		".json" => "application/json",
		_ => "application/octet-stream",
	};
}
