using System.Text;
using System.Text.RegularExpressions;

namespace Gnothi.Movil;

// GNOTHI SERVING THE MODULE'S BUNDLE out of its own container, with the CSP in place.
//
// WHY THIS IS SHARED AND THE INTERCEPTOR IS NOT. What a platform decides is HOW a response
// gets handed back to the engine -- a `WebResourceResponse` on Android, a `NSUrlResponse` plus
// data on iOS. WHAT gets answered -- which file, with which type, with which policy -- is the
// same on both, and it is the part where a divergence would be dangerous rather than merely
// annoying: two copies of the CSP means one of them silently stops being the real one.
//
// So the platform is left with the adapter, and everything that is a decision lives here.
internal sealed class BundleServer
{
	// The CSP minus script-src, which is built per document with the hash of each inline script.
	//
	// `connect-src 'self'` and NOT 'none': with 'none' the module does not start, because the
	// runtime itself fetches its blazor.boot.json. Measured. With 'self' it still cannot reach
	// the publisher -- that is a different origin -- and it does start.
	// `form-action` AND `base-uri` ARE HERE BECAUSE THEY DO NOT FALL BACK TO `default-src`. They
	// are two of the very few directives that do not, so leaving them out leaves a hole that
	// LOOKS closed: `default-src 'self'` reads as if it covered everything, and it does not.
	//
	// WHAT form-action CLOSES, and it was open: a form submit is NOT a fetch. It is a navigation
	// carrying the POST body, so `connect-src` -- which is about ASKING, not about LEAVING --
	// never applies to it. A module holds its own journal in page memory by design, so without
	// this it could put that journal in a hidden field and post it anywhere.
	//
	// base-uri is the same family and, to be fair, defence in depth rather than a live hole:
	// with connect-src 'self' and inline scripts pinned by hash there is no known way through
	// it. It costs one clause on a line that had to be edited anyway.
	//
	// THE OTHER HALF OF THE HOLE IS NOT HERE, because no CSP directive can close it:
	// `window.location = '...'` is a top-level navigation, and `navigate-to` was proposed and
	// never standardised. That half is closed per platform -- OnlyItsOwnOrigin on iOS,
	// ShouldOverrideUrlLoading on Android.
	//
	// img-src ADMITS data: AND blob:, and that is not a hole: an image cannot carry anything OUT.
	// Both schemes are bytes the page already holds -- a photo the person just attached, a QR the
	// module draws, a chart -- shown to her without leaving the page. Without this line the module
	// stores the photo fine and then shows an empty frame: measured 2026-09-11 on an iPhone with
	// Cargas' odometer photo, `REFUSED by img-src -- data` in the log and nothing on screen.
	// Remote images stay refused: 'self' is the module's own origin, and that is all.
	private const string CspWithoutScript =
		"default-src 'self'; " +
		"img-src 'self' data: blob:; " +
		"style-src 'self' 'unsafe-inline'; " +
		"connect-src 'self'; " +
		"form-action 'self'; " +
		"base-uri 'self'; " +
		"frame-src 'none'; " +
		"worker-src 'none'";

	// THE WATCHDOG: it turns a refusal from a console line into a fact Gnothi has.
	//
	// The engine already refuses -- that is the CSP -- but until now Gnothi only learned about it
	// the way a person does: by reading a console message among all the others. It could not
	// count them, could not tell one from an ordinary log line, and could not assert anything.
	// `securitypolicyviolation` fires with the directive, the blocked URI and the line that did
	// it, which is the difference between "something was denied" and evidence.
	//
	// IT GOES IN THE DOCUMENT AND NOT IN A PLATFORM SHIM, and that is the whole reason it lives
	// here: iOS has a `WKUserScript` that runs at document start, and Android has nothing
	// equivalent without adding androidx.webkit. Served inside the document, both platforms get
	// it from ONE piece of code, at the top of the page, before the module's own scripts.
	//
	// ITS OWN HASH IS COMPUTED FOR FREE, because the injection happens BEFORE CspFor runs and the
	// inline-script regex below finds it like any other. Injecting after would produce a document
	// whose CSP forbids the very script Gnothi just added -- a self-inflicted blank page.
	private const string Watchdog =
		"<script>(function(){document.addEventListener('securitypolicyviolation',function(e){" +
		"try{window.gnothiPuente.violation(JSON.stringify({" +
		"directive:e.effectiveDirective||e.violatedDirective," +
		"blocked:e.blockedURI,source:e.sourceFile,line:e.lineNumber}));}catch(x){}});})();</script>";

	private static readonly Regex HeadOpen = new(@"<head\b[^>]*>", RegexOptions.IgnoreCase);

	private static readonly Regex InlineScripts =
		new(@"<script\b(?![^>]*\bsrc\s*=)[^>]*>(.*?)</script\s*>",
			RegexOptions.Singleline | RegexOptions.IgnoreCase);

	private readonly string bundle;
	private readonly string basePath;

	internal BundleServer(string bundle, string basePath)
	{
		this.bundle = bundle;
		this.basePath = basePath;
	}

	// A DONDE VA EL 404 QUE NO SE VE. Lo pone quien construye el servidor -hoy WebModule, hacia
	// el log del Host-. Nulo significa el comportamiento de antes: callarse. Ver Serve.
	internal Action<string>? NotFound { get; set; }

	// The path as the engine asked for it, with the bundle's base still in front. Returns what
	// to answer; it never throws for a missing file, because a 404 is an answer.
	internal ServedFile Serve(string requestedPath)
	{
		string asked = requestedPath.Length == 0 ? "/" : requestedPath;

		// LA RAIZ DEL BUNDLE, PEDIDA SIN LA BARRA FINAL. El origen es gnothi-mod://<modulo>/app/
		// y WebKit pide "/app", no "/app/". Sin este caso el StartsWith contra basePath -que SI
		// lleva la barra- no matchea, no se recorta nada, y se busca <bundle>/app: 404 en el
		// documento inicial, window.gnothi nunca se define y la pantalla queda en blanco, sin un
		// solo error. Medido en el simulador el 2026-09-07 con los cuatro modulos del paquete.
		if (basePath.Length > 1 && asked == basePath.TrimEnd('/')) asked = "/";
		else if (asked.StartsWith(basePath, StringComparison.Ordinal)) asked = asked[basePath.Length..];

		string path = asked.TrimStart('/');
		if (path.Length == 0 || path.EndsWith("/", StringComparison.Ordinal)) path += "index.html";

		string full = Resolve(path);

		// UN 404 QUE SE DICE. Es silencioso a proposito -"un 404 es una respuesta"-, y eso es
		// cierto para el protocolo y falso para diagnosticar: hace indistinguible "no encuentro
		// el archivo" de "la CSP lo rechazo", y las dos dejan la pantalla igual de blanca.
		//
		// ES LA MITAD QUE EL WATCHDOG NO CUBRE. El de arriba reporta lo que la CSP RECHAZO; este
		// reporta lo que NO ESTA. Con los dos, una pantalla en blanco se parte en los dos casos
		// que antes se confundian. El costo de no tenerlo se pago dos veces el 2026-09-07: los
		// dos bugs de esta tanda -la ruta bin/web y el base pedido sin barra- eran invisibles, y
		// cada uno se leyo en un renglon apenas esta linea existio.
		if (!File.Exists(full)) { NotFound?.Invoke($"404 {requestedPath} -> {full}"); return ServedFile.NotFound(); }

		// The document is read whole because its CSP depends on its content. Everything else
		// travels as a file the platform streams.
		if (path.EndsWith(".html", StringComparison.Ordinal))
		{
			byte[] document = WithWatchdog(File.ReadAllBytes(full));
			return ServedFile.Document(document, CspFor(document));
		}

		return ServedFile.Stream(full, MimeOf(path));
	}

	// The watchdog goes right after <head>, so it is listening before anything the institution
	// wrote. A document with no <head> is left ALONE: guessing where to put a script in a
	// document that does not have the shape we expect is how a page gets broken by its host.
	private static byte[] WithWatchdog(byte[] document)
	{
		string html = Encoding.UTF8.GetString(document);

		Match head = HeadOpen.Match(html);
		if (!head.Success) return document;

		int at = head.Index + head.Length;
		return Encoding.UTF8.GetBytes(html[..at] + Watchdog + html[at..]);
	}

	// THE HASH IS COMPUTED, NOT WRITTEN DOWN. One written by hand goes silently out of sync the
	// first time the institution rebuilds, and the symptom is a blank screen with no clue.
	// Gnothi has the file in its hand: it computes it here.
	//
	// AND IT HASHES WHAT THE PARSER WILL SEE, NOT WHAT THE FILE HOLDS -- which is a distinction
	// that cost a real blank screen. The HTML input stream is preprocessed before parsing: CRLF
	// becomes LF, and any lone CR becomes LF. So the engine hashes text with LF, and a file saved
	// with CRLF hashed as-is produces a DIFFERENT digest.
	//
	// MEASURED 2026-09-07 by Renan, running the script on a Mac. Three of the four bundles ship
	// their inline script minified on ONE line -- no newlines, so nothing to normalise, and their
	// hash matched. DealershipCar ships the same script indented over five lines with CRLF: its
	// hash did not match, the engine refused it, and the watchdog is what made it visible:
	//
	//     dealership: REFUSED by script-src-elem -- inline (from :34); 1 so far
	//
	// The failure is nasty in both directions: Gnothi ends up forbidding the very script it just
	// hashed, and it only shows on documents that have a multi-line inline script -- so it hides
	// behind whatever formatting the institution happens to use.
	private static string CspFor(byte[] document)
	{
		string html = Encoding.UTF8.GetString(document);

		var hashes = new List<string>();
		foreach (Match m in InlineScripts.Matches(html))
		{
			string body = AsTheParserSeesIt(m.Groups[1].Value);
			if (body.Length == 0) continue;

			byte[] digest = System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(body));
			hashes.Add($"'sha256-{Convert.ToBase64String(digest)}'");
		}

		string script = string.Join(" ", new[] { "'self'", "'wasm-unsafe-eval'" }.Concat(hashes));
		return $"{CspWithoutScript}; script-src {script}";
	}

	// The HTML input stream preprocessing, which is the only part of it that changes a hash:
	// CRLF -> LF first, then any surviving lone CR -> LF. Order matters -- doing CR first would
	// turn every CRLF into two newlines.
	private static string AsTheParserSeesIt(string body) =>
		body.Replace("\r\n", "\n").Replace('\r', '\n');

	// A module's path CANNOT climb out of its bundle. That is Gnothi's responsibility, not
	// trust in the module.
	private string Resolve(string path)
	{
		string full = Path.GetFullPath(
			Path.Combine(bundle, path.Replace('/', Path.DirectorySeparatorChar)));

		if (!full.StartsWith(bundle, StringComparison.Ordinal))
			throw new UnauthorizedAccessException($"path outside the module: {path}");

		return full;
	}

	// WHERE THE BUNDLE SAYS IT LIVES. Its index.html carries a `<base href="…">` baked in at
	// publish time, and every one of its paths hangs off that. Gnothi has to serve it AT THAT
	// PLACE: serving it at the root when the bundle was built for `/app/` gives 404 on
	// everything and a blank page, with no hint whatsoever. Measured, and it cost a whole run.
	//
	// It is read once, from the file, and it is not configurable: it is a fact about the
	// bundle, not a decision of the Host.
	internal static string BaseOf(string bundle)
	{
		string index = Path.Combine(bundle, "index.html");
		if (!File.Exists(index)) return "/";

		Match m = Regex.Match(File.ReadAllText(index),
			@"<base\s[^>]*href\s*=\s*[""']([^""']+)[""']", RegexOptions.IgnoreCase);

		if (!m.Success) return "/";

		string href = m.Groups[1].Value;
		if (!href.StartsWith('/')) href = "/" + href;
		if (!href.EndsWith('/')) href += "/";
		return href;
	}

	// The .wasm NEEDS its type or the engine will not compile it in streaming mode.
	private static string MimeOf(string path) => Path.GetExtension(path).ToLowerInvariant() switch
	{
		".html" => "text/html",
		".js" or ".mjs" => "text/javascript",
		".css" => "text/css",
		".wasm" => "application/wasm",
		".json" => "application/json",
		".webmanifest" => "application/manifest+json",
		".png" => "image/png",
		".ico" => "image/x-icon",
		".svg" => "image/svg+xml",
		".woff2" => "font/woff2",
		_ => "application/octet-stream",
	};
}

// WHAT TO ANSWER, said in a way neither engine owns. `Body` when the bytes are already in
// hand -- the document, because its CSP was computed from them -- and `Path` when the platform
// should stream the file itself, which is what keeps a 20 MB wasm from going through memory.
internal sealed record ServedFile(
	int Status, string Mime, IReadOnlyDictionary<string, string> Headers, byte[]? Body, string? Path)
{
	// Nothing the module serves may be cached: the bundle is replaced by reinstalling, and a
	// stale entry would show the previous institution's build with no way to tell.
	private static Dictionary<string, string> NoStore() => new() { ["Cache-Control"] = "no-store" };

	internal static ServedFile NotFound() =>
		new(404, "text/plain", NoStore(), [], null);

	internal static ServedFile Document(byte[] body, string csp)
	{
		Dictionary<string, string> headers = NoStore();
		headers["Content-Security-Policy"] = csp;
		return new ServedFile(200, "text/html", headers, body, null);
	}

	internal static ServedFile Stream(string path, string mime) =>
		new(200, mime, NoStore(), null, path);
}
