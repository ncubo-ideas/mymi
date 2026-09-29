using System.Reflection;

// ASSERTS THAT GATE 2 CAN BE DELETED.
//
// The arrow goes ONE way: Gnothi.Assistant -> Gnothi.Host, never back. If somebody put a using of
// the agent loop inside the Host, the build would stay green and nobody would notice -- except
// this test.

int failures = 0;

void Assert(string what, bool condition, string detail)
{
	Console.WriteLine($"  [{(condition ? "OK  " : "FAIL")}] {what}");
	if (!condition) { Console.WriteLine($"          {detail}"); failures++; }
}

Assembly host = typeof(Gnothi.Host.Dispatcher).Assembly;
Assembly assistant = typeof(Gnothi.Assistant.AgentLoop).Assembly;

Console.WriteLine("=== THE ASSISTANT IS DESIGNED TO BE DELETED ===");
Console.WriteLine($"  host      : {host.GetName().Name}");
Console.WriteLine($"  assistant : {assistant.GetName().Name}   <- this is Gate 2's implementation");
Console.WriteLine();

string[] hostRefs = [.. host.GetReferencedAssemblies().Select(a => a.Name ?? "").Order()];
string[] assistantRefs = [.. assistant.GetReferencedAssemblies().Select(a => a.Name ?? "").Order()];

// 1 · THE CENTRAL PROPERTY
Assert("Gnothi.Host does NOT reference Gnothi.Assistant",
	!hostRefs.Contains("Gnothi.Assistant"),
	$"the Host references: {string.Join(", ", hostRefs)}");

// 2 · and the arrow exists in the right direction
Assert("Gnothi.Assistant DOES reference Gnothi.Host",
	assistantRefs.Contains("Gnothi.Host"),
	$"Gate 2 references: {string.Join(", ", assistantRefs)}");

// 3 · the Host does not name a model provider either: that belongs to Gate 2
Assert("Gnothi.Host references no model HTTP client",
	!hostRefs.Any(r => r.Contains("Anthropic", StringComparison.OrdinalIgnoreCase)
		|| r.Contains("OpenAI", StringComparison.OrdinalIgnoreCase)),
	string.Join(", ", hostRefs));

// 4 · the loop names no shell and no engine: only the dispatcher
string[] fieldTypes = [.. assistant.GetTypes()
	.SelectMany(t => t.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
	.Select(f => f.FieldType.Name)
	.Distinct()
	.Order()];

Assert("the loop has no shell-typed or engine-typed fields",
	!fieldTypes.Any(n => n.Contains("Shell") || n == "PerformanceV2" || n.Contains("Assembly")),
	$"fields: {string.Join(", ", fieldTypes)}");

// ============================================================================================
// THE CARATULA GATE MUST FAIL CLOSED.
//
// Gate 3 lets an APK owned by a module reach Dispatcher.Serve from ANOTHER PROCESS. What keeps
// that from being a hole is the declaration in installation.json plus a signature the Host
// looks up itself. The verification is Android's business; the DECISION is here, and here is
// where a mistake would be silent -- a gate that accepts everyone still returns green in every
// functional test, because functional tests only try the caller that is supposed to work.
//
// So these assertions try the callers that are supposed to FAIL.
// ============================================================================================

Console.WriteLine();
Console.WriteLine("=== THE CARATULA GATE FAILS CLOSED ===");

const string real = "4f07f90c4633f3906c9a55889fbc33568bded70e0c81957798e9d9a5d6c85cf1";
var declared = new Gnothi.Host.AllowedCaller
{
	Package = "com.ncubo.lab.caratula",
	SignatureSha256 = real,
};

Assert("the declared caratula accepts its own package and signature",
	declared.Accepts("com.ncubo.lab.caratula", real),
	"the legitimate caller was rejected");

// The whole point: a package name is a string. Anyone can name their app the same.
Assert("an impostor with the SAME package name but another signature is rejected",
	!declared.Accepts("com.ncubo.lab.caratula", new string('A', 64)),
	"an impostor got in with a forged package name");

Assert("the right signature under another package name is rejected",
	!declared.Accepts("com.evil.app", real),
	"a package the module never named got in");

// Every tool prints digests differently -- keytool uses colons, apksigner does not, and both
// mix case. Comparing raw would reject a correct signature over punctuation, and the person
// debugging it would 'fix' it by loosening the check.
Assert("the same digest with colons and different case still matches",
	declared.Accepts("com.ncubo.lab.caratula",
		string.Join(":", Enumerable.Range(0, 32).Select(i => real.Substring(i * 2, 2)).Select(s => s.ToUpperInvariant()))),
	"formatting alone changed the verdict");

// ABSENT MEANS NOBODY. This is the assertion that matters most, because the failure mode is
// somebody adding a module and forgetting the block.
Assert("a caratula with no signature declared accepts nobody",
	!new Gnothi.Host.AllowedCaller { Package = "com.ncubo.lab.caratula" }.Accepts("com.ncubo.lab.caratula", real),
	"a half-declared caratula let a caller through");

Assert("an empty caratula accepts nobody",
	!new Gnothi.Host.AllowedCaller().Accepts("", ""),
	"an empty declaration matched an empty caller");

// And the seam: the core declares WHO may enter and never learns WHO DID. If Dispatcher or
// Request ever grow a caller field, Gate 3 stops being interchangeable with the others.
string[] requestMembers = [.. typeof(Gnothi.Host.Request)
	.GetProperties().Select(p => p.Name.ToLowerInvariant())];

Assert("Request carries no caller identity -- Dispatcher.Serve still does not know who called",
	!requestMembers.Any(n => n.Contains("caller") || n.Contains("uid")
		|| n.Contains("package") || n.Contains("signature")),
	$"Request exposes: {string.Join(", ", requestMembers)}");

// ============================================================================================
// THE CONFORMANCE SUITE — the same tool call through the three doors.
//
// It needs REAL modules, so it points at the repo's Modulos directory. If it is not there
// -- somebody runs this from elsewhere -- the suite says so and does not silently pass, which
// would be the worst outcome: a green CI asserting nothing.
// ============================================================================================

// An argument wins: `dotnet run --project Gnothi.Assistant.Tests -- Modulos.local` runs the suite
// against a bench of one's own (for instance, one pointing at the person's real journals). With no
// argument it looks for ./Modulos, which is versioned and points, RELATIVE to each folder, at each
// module's bin/Debug: it starts on any machine that has built them. If they are not built, the
// conformance's first assertion says so and says what to build.
// AND WITH NO ARGUMENT IT PREFERS `Modulos.suite`, which is the bench the csproj above builds and
// maintains. `Modulos/` still exists and is the one the MCP servers in .mcp.json read -- which is
// precisely why the suite cannot use it: those servers hold their dlls OPEN, nobody can rebuild
// them, and the suite ended up comparing the manifest against an old binary. That looked like a
// module declaring tools it does not have, when the only old thing was the .dll.
string modulos = args.Length > 0 && Directory.Exists(args[0])
	? Path.GetFullPath(args[0])
	: DeLaSuiteOElVersionado();

static string DeLaSuiteOElVersionado()
{
	string versionado = Gnothi.Conformance.Conformance.FindModules();
	if (versionado.Length == 0) return versionado;

	string propio = Path.Combine(Path.GetDirectoryName(versionado)!, "Modulos.suite");
	return Directory.Exists(propio) && Directory.GetDirectories(propio).Length > 0 ? propio : versionado;
}

if (modulos.Length == 0)
{
	Assert("se encontro el directorio Modulos", false,
		"no se hallo ./Modulos subiendo desde el binario; la suite NO corrio");
}
else
{
	// The suite starts every module and crosses its manifest against the REAL tools before
	// comparing the three gates: a catalogue that does not match the modules would compare the
	// wrong copy. That guard runs FIRST inside Run (Conformance.cs).
	Console.WriteLine();
	Console.WriteLine("=== LAS TRES PUERTAS CONTESTAN LO MISMO ===");
	await Gnothi.Conformance.Conformance.Run(modulos, Assert);

	// SWITCHING OFF IS HONOURED BY EVERY GATE. It runs AFTER the conformance because it needs its
	// own catalogue -- it switches tools off and on to measure the gate -- and has no reason to mix
	// with the one the comparison uses. See Conformance.Consentimiento.
	Console.WriteLine();
	Console.WriteLine("=== UNA TOOL APAGADA NO SE CUELA POR NINGUNA PUERTA ===");
	await Gnothi.Conformance.Conformance.Consentimiento(modulos, Assert);

	// A VALUE OUTSIDE THE ENUM COMES BACK WITH THE LIST OF VALID ONES. It does not need the modules
	// to start either: the gate runs before invoking. See Conformance.EnumFueraDelConjunto.
	Console.WriteLine();
	Console.WriteLine("=== UN ENUM FUERA DEL CONJUNTO VUELVE CON LOS VALORES VALIDOS ===");
	await Gnothi.Conformance.Conformance.EnumFueraDelConjunto(modulos, Assert);
}

// ============================================================================================
// THE PACKAGE IS INSPECTED BEFORE ANYBODY ACCEPTS IT, AND THE SAME WAY ON BOTH INSTALLERS.
// Synthetic packages, built in memory: see Enrollment.cs.
// ============================================================================================

Console.WriteLine();
Console.WriteLine("=== THE PACKAGE IS INSPECTED BEFORE SHE DECIDES ===");
Gnothi.Tests.Enrollment.Run(Assert);

// ============================================================================================
// A MODULE CANNOT WRITE IN ANOTHER'S FOLDER, AND THE BRIDGE DOES NOT ACCEPT BEING TOLD WHO IT IS
// TALKING ABOUT. Today the second half holds by itself because there is one bridge per WebView;
// the form under evaluation (note 49) takes that crutch away. See Aislamiento.cs.
// ============================================================================================

Console.WriteLine();
Console.WriteLine("=== UN MODULO NO ALCANZA EL JOURNAL DE OTRO ===");
Gnothi.Tests.Aislamiento.Run(Assert);

// ============================================================================================
// AND IN THE SALA, WHERE EVERY MODULE SHARES ONE DOCUMENT, THE SEPARATION IS SUSTAINED BY THE
// SHELL: one call in flight, one module loaded at a time, and the identity comes out of the call's
// frame and never out of the message. See Sala.cs.
// ============================================================================================

Console.WriteLine();
Console.WriteLine("=== LA SALA SOSTIENE LA SEPARACION QUE EL PUENTE YA NO REGALA ===");
Gnothi.Tests.Room.Run(Assert);

// ============================================================================================
// Y LA COSTURA QUE ELIGE LA CASCARA ADMITE MAS DE UN DUEÑO SIN QUE NINGUNO DESAPAREZCA. Es un
// campo estatico con dos platformas escribiendole; hasta el 2026-09-23 la segunda borraba a la
// primera EN SILENCIO. Ver Seam.cs.
// ============================================================================================

Console.WriteLine();
Console.WriteLine("=== DOS FABRICAS DE CASCARAS CONVIVEN, Y SE DICE QUE CONVIVEN ===");
Gnothi.Tests.Seam.Run(Assert);

// ============================================================================================
// --install WRITES THE CODEX TABLE WITHOUT TOUCHING THE REST OF config.toml.
//
// The ChatGPT desktop app (Codex and Work tabs) reads ~/.codex/config.toml. Measured
// 2026-09-09 (NotasGnothiHost/45): two of the four lines are not optional -without `required`
// the first answer goes out before the Host starts, without `omit_tools_from` the agent reads
// the journal by hand instead of calling the tool. CodexTable is pure text, so this checks the
// splice: other tables intact, our table replaced and not duplicated, the two lines present.
// ============================================================================================

Console.WriteLine();
Console.WriteLine("=== --install WRITES THE CODEX TABLE AND KEEPS THE REST ===");
{
	const string others =
		"[desktop]\nfollowUpQueueMode = \"steer\"\n\n[mcp_servers.other]\ncommand = \"node\"\n\n[plugins.\"x@y\"]\nenabled = true\n";
	string exe = @"C:\Gnothi\Gnothi.Host.exe";
	string modules = @"C:\Gnothi\Modulos";

	string once = Gnothi.Host.CodexTable.Upsert(others, exe, modules);
	Assert("appends the table when it is not there",
		once.Contains("[mcp_servers.gnothi]") && once.StartsWith(others.TrimEnd('\n')),
		once);
	Assert("keeps the other tables byte for byte",
		once.Contains("[mcp_servers.other]\ncommand = \"node\"") && once.Contains("[plugins.\"x@y\"]\nenabled = true"),
		once);
	Assert("the two lines Codex needs are there",
		once.Contains("required = true") && once.Contains("omit_tools_from = [\"code_mode\", \"deferred\"]"),
		once);
	Assert("windows paths go as TOML literal strings, unescaped",
		once.Contains("command = 'C:\\Gnothi\\Gnothi.Host.exe'") && once.Contains("args = ['C:\\Gnothi\\Modulos']"),
		once);

	// Old table in the middle, with a subtable and a stale cwd: replaced whole, nothing doubled.
	string stale = others.Replace("[mcp_servers.other]",
		"[mcp_servers.gnothi]\ncommand = \"dotnet\"\ncwd = 'C:\\old'\n\n[mcp_servers.gnothi.env]\nX = \"1\"\n\n[mcp_servers.other]");
	string replaced = Gnothi.Host.CodexTable.Upsert(stale, exe, modules);
	int headers = replaced.Split("[mcp_servers.gnothi]").Length - 1;
	Assert("replaces an existing table instead of adding a second one", headers == 1, replaced);
	Assert("the stale lines and our old subtable are gone",
		!replaced.Contains("cwd = 'C:\\old'") && !replaced.Contains("[mcp_servers.gnothi.env]"),
		replaced);
	Assert("what surrounded the old table survives",
		replaced.Contains("[desktop]\nfollowUpQueueMode") && replaced.Contains("[mcp_servers.other]\ncommand = \"node\"") && replaced.Contains("[plugins.\"x@y\"]"),
		replaced);

	string twice = Gnothi.Host.CodexTable.Upsert(once, exe, modules);
	Assert("upsert is idempotent", twice == once, twice);
	Assert("an empty file gets just the table",
		Gnothi.Host.CodexTable.Upsert("", exe, modules).StartsWith("[mcp_servers.gnothi]"), "");
}

// ============================================================================================
// --install READS AN EMPTY JSON CONFIG AS EMPTY, NOT AS BROKEN.
//
// Antigravity leaves a 0-byte mcp_config.json on a fresh install (seen on a Mac, 2026-09-10).
// JsonNode.Parse("") throws, and the installer took that for a corrupt file and sent the person
// to register by hand. Whitespace is {}; comments and trailing commas are tolerated because
// Claude Desktop's own file may carry them; garbage still throws, so the file is not touched.
// ============================================================================================

Console.WriteLine();
Console.WriteLine("=== --install READS AN EMPTY JSON CONFIG AS EMPTY, NOT AS BROKEN ===");
{
	Assert("a 0-byte file reads as {}", Gnothi.Host.HostInstaller.ReadConfig("").Count == 0, "");
	Assert("whitespace reads as {}", Gnothi.Host.HostInstaller.ReadConfig(" \r\n\t\n").Count == 0, "");

	var kept = Gnothi.Host.HostInstaller.ReadConfig("{ \"mcpServers\": { \"other\": { \"command\": \"node\" } }, \"theme\": \"dark\" }");
	Assert("what is there is kept, key by key",
		kept.Count == 2 && kept["mcpServers"]?["other"]?["command"]?.GetValue<string>() == "node" && kept["theme"]?.GetValue<string>() == "dark",
		kept.ToJsonString());

	var lenient = Gnothi.Host.HostInstaller.ReadConfig("{ // the app's own comment\n \"a\": 1, }");
	Assert("comments and trailing commas are tolerated", lenient["a"]?.GetValue<int>() == 1, lenient.ToJsonString());

	bool threw = false;
	try { Gnothi.Host.HostInstaller.ReadConfig("{ not json"); } catch (System.Text.Json.JsonException) { threw = true; }
	Assert("garbage still throws, so the file is left alone", threw, "");
}

// ============================================================================================
// THE INSTALLER WRITES AN installation.json THE HOST CAN READ.
//
// A regression. The refactor to reflection-free JSON (88e0b95) left --install-module serialising
// InstallationFile with Installation's type-info: it compiled, and blew up with
// InvalidCastException on every install, on every platform. Nobody saw it because the only path
// exercising it was the manual test. This block installs the .gmod the repo already versions (the
// one La Guaria's page serves) into a temporary folder and reads the result with the SAME reader
// the catalogue uses. If the writer and the reader drift apart, this fails.
// ============================================================================================

if (modulos.Length > 0)
{
	Console.WriteLine();
	Console.WriteLine("=== EL INSTALADOR ESCRIBE LO QUE EL CATALOGO LEE ===");
	string gmod = Path.Combine(Path.GetDirectoryName(modulos)!,
		"Compras", "PaginaDeDescarga", "descarga", "descargas", "compras.gmod");
	if (!File.Exists(gmod))
		Assert("existe el .gmod versionado de La Guaria", false, $"no esta: {gmod}");
	else
	{
		string temp = Path.Combine(Path.GetTempPath(), "gnothi-tests-" + Guid.NewGuid().ToString("N"));
		try
		{
			// grantBase inside the temp folder: the test does not touch the person Documents/Gnothi.
			(bool ok, string message) = Gnothi.Host.ModuleInstaller.Install(
				gmod, Path.Combine(temp, "Modulos"), Path.Combine(temp, "datos"));
			Assert("--install-module termina bien", ok, message);

			string written = Path.Combine(temp, "Modulos", "compras", "installation.json");
			Assert("escribio installation.json", File.Exists(written), $"no existe: {written}");

			var read = Gnothi.Host.Installation.Read(written);
			Assert("el catalogo lee lo que el instalador escribio",
				read.Shell.Type == "in-process" && read.Shell.Arguments.Count == 1
					&& read.GrantedFolder.Length > 0 && read.Enabled,
				$"shell={read.Shell.Type} args={read.Shell.Arguments.Count} folder='{read.GrantedFolder}' enabled={read.Enabled}");
			Assert("el dll de entrada que apunta existe",
				File.Exists(read.Shell.Arguments.FirstOrDefault() ?? ""),
				$"apunta a: {read.Shell.Arguments.FirstOrDefault()}");
		}
		catch (Exception ex)
		{
			Assert("--install-module no revienta", false, $"{ex.GetType().Name}: {ex.Message}");
		}
		finally
		{
			try { Directory.Delete(temp, recursive: true); } catch { /* temp, best effort */ }
		}
	}
}

// ============================================================================================
// THE LANGUAGE OF THE RECORD TRAVELS WITH EVERY FREE-TEXT PARAMETER, AND ONLY WITH THOSE.
//
// Measured 2026-09-11 (NotasGnothiHost/46): a free-text value in the person's language ("milk")
// against a journal in the institution's language ("leche") finds nothing, and the model had no
// way to know which language the record is in. about.locale is that way. The hint lives in the
// shared schema, so asserting it here covers Gate 1, Gate 2 and the on-device catalog at once.
Console.WriteLine();
Console.WriteLine("=== THE RECORD'S LANGUAGE RIDES ON FREE-TEXT PARAMETERS ===");
{
	var tool = new Gnothi.Host.DeclaredTool
	{
		Name = "consumo_de",
		Input =
		[
			new Gnothi.Host.DeclaredParameter { Name = "termino", Type = "string", Description = "A product text." },
			new Gnothi.Host.DeclaredParameter { Name = "analito", Type = "string", Description = "Which analyte.", Enum = ["ColesterolTotal", "Hemoglobina"] },
			new Gnothi.Host.DeclaredParameter { Name = "cuantas", Type = "number", Description = "How many." },
			new Gnothi.Host.DeclaredParameter { Name = "vacio", Type = "string", Description = "" },
		],
	};

	string Description(IReadOnlyDictionary<string, object> schema, string parameter) =>
		(string)((Dictionary<string, object>)((Dictionary<string, object>)schema["properties"])[parameter])["description"];

	string hint = Gnothi.Host.ToolIndex.RecordLanguageHint("es-CR");
	var withLocale = Gnothi.Host.ToolIndex.InputSchema(tool, "es-CR");
	var without = Gnothi.Host.ToolIndex.InputSchema(tool, null);
	var legacy = Gnothi.Host.ToolIndex.InputSchema(tool);

	Assert("a free-text parameter carries the record's locale, after its own description",
		Description(withLocale, "termino") == "A product text. " + hint,
		Description(withLocale, "termino"));
	Assert("the hint names the module's locale, not a language of the Host",
		hint.Contains("es-CR") && Gnothi.Host.ToolIndex.RecordLanguageHint("en-US").Contains("en-US"),
		hint);
	Assert("an enum parameter carries NO hint: its values are already closed",
		Description(withLocale, "analito") == "Which analyte.",
		Description(withLocale, "analito"));
	Assert("a numeric parameter carries NO hint",
		Description(withLocale, "cuantas") == "How many.",
		Description(withLocale, "cuantas"));
	Assert("an empty description becomes just the hint, without a stray period",
		Description(withLocale, "vacio") == hint,
		Description(withLocale, "vacio"));
	Assert("a module that declares no locale gets nothing: the model keeps guessing, as before",
		Description(without, "termino") == "A product text." && Description(legacy, "termino") == "A product text.",
		Description(without, "termino"));
	Assert("a Host tool has no module and therefore no record locale",
		Gnothi.Host.ToolIndex.RecordLocaleOf(null) is null,
		"expected null");
}

// THE CATALOG OF TEXTS IS THERE, IN BOTH LANGUAGES.
//
// This is the assertion that pays for itself. texts.json is an EmbeddedResource declared in
// THREE csproj with the same LogicalName -- Gnothi.Host for the desktop, Gnothi.Mobile and
// Gnothi.iOS because the phones compile the Host's sources by glob instead of referencing it.
// Forgetting one of the three does NOT break the build: Texts returns each key as its own text,
// so the symptom is a screen that says 'instituciones.titulo', found on a phone.
//
// What this can check is the assembly it runs in, which is the desktop's. The two phone csproj
// are checked by reading them: they are text, and a missing line is a missing line.
Console.WriteLine();
Console.WriteLine("=== THE HOST'S TEXTS ARE THERE, IN BOTH LANGUAGES ===");
{
	Gnothi.Host.Texts.Language = "es";
	string es = Gnothi.Host.Texts.T("instituciones.titulo");
	Gnothi.Host.Texts.Language = "en";
	string en = Gnothi.Host.Texts.T("instituciones.titulo");

	Assert("the embedded catalog loads: a known key is not its own name",
		es != "instituciones.titulo", $"got '{es}' -- the EmbeddedResource is missing from this csproj");
	Assert("the two languages are different phrases",
		es == "Mis conexiones" && en == "My Connections", $"es='{es}' en='{en}'");

	Gnothi.Host.Texts.Language = "es";
	Assert("a value travels by name, not by position",
		Gnothi.Host.Texts.T("instituciones.consultas.varias", ("n", "3")) == "3 consultas",
		Gnothi.Host.Texts.T("instituciones.consultas.varias", ("n", "3")));

	// A missing key has to be UGLY, not blank: a blank looks like a design decision and ships.
	Assert("a key that does not exist renders as itself",
		Gnothi.Host.Texts.T("no.existe.esta.clave") == "no.existe.esta.clave",
		Gnothi.Host.Texts.T("no.existe.esta.clave"));

	string[] faltantes = [.. Gnothi.Host.Texts.Missing()];
	Assert("no key is missing one of its two languages",
		faltantes.Length == 0, string.Join(" | ", faltantes));

	// THE ONE A COMPILER CANNOT SEE. A translator drops a {titulo} or writes {title}, and the
	// phrase ships either with a raw marker on screen or -worse- without the datum and looking
	// fine. Sets, not order: order is exactly why the values travel by name.
	string[] discrepancias = [.. Gnothi.Host.Texts.Mismatches()];
	Assert("the two languages of a key ask for the same values",
		discrepancias.Length == 0, string.Join(" | ", discrepancias));

	// A KEY WHOSE TWO LANGUAGES SAY THE SAME THING is almost always a half-finished paste. It
	// happened while writing this very catalog: the English went into both columns and only one
	// got translated. Neither of the two checks above sees it -- both columns are full and no
	// marker is missing -- so the phrase ships in the wrong language looking finished.
	string[] identicas = [.. Gnothi.Host.Texts.Identical()];
	Assert("no key says exactly the same in both languages",
		identicas.Length == 0, string.Join(" | ", identicas));

	// THE ENROLLMENT PHRASE WITH THE MOST SUBSTITUTION, end to end: three values, and one of
	// them is itself a phrase from the catalog (what an institution with no declared name is
	// called). Nothing may be left raw.
	Gnothi.Host.Texts.Language = "es";
	string colision = Gnothi.Host.Texts.T("alta.error.colision",
		("modulo", "compras"), ("instalada", "La Guaria"),
		("entrante", Gnothi.Host.Texts.T("alta.error.otra-institucion")));
	Assert("a phrase with three values leaves no marker raw",
		!colision.Contains('{') && colision.Contains("compras")
			&& colision.Contains("La Guaria") && colision.Contains("otra institución"),
		colision);

	// THE REPO ROOT, for the two checks that read files no project on this machine compiles.
	string raiz = AppContext.BaseDirectory;
	while (raiz.Length > 1 && !Directory.Exists(Path.Combine(raiz, "Gnothi.Host")))
		raiz = Path.GetDirectoryName(raiz.TrimEnd(Path.DirectorySeparatorChar)) ?? "/";

	// WHAT THE TWO PLATFORM SHELLS ASK OF Shell, CHECKED WITHOUT COMPILING THEM.
	//
	// MainActivity.cs and ViewController.cs are compiled by NO project that runs on an ordinary
	// Mac: Android needs a workload that may not be installed, and the iOS SDK demands an exact
	// version of Xcode. But both call Shell, which this suite does compile -- so the border can be
	// checked even though the shells cannot.
	//
	// What it catches: renaming a method of Shell and updating only one of the two, which is
	// exactly what happens when the compiler is not watching. It already happened in the opposite
	// direction: ReportEnrollmentError took the SENTENCE and both wrote it identically.
	{
		string[] caras = ["Gnothi.Mobile/MainActivity.cs", "Gnothi.iOS/ViewController.cs"];
		var deShell = typeof(Gnothi.MobileShared.Shell)
			.GetMembers(BindingFlags.Public | BindingFlags.Instance)
			.Select(m => m.Name).ToHashSet(StringComparer.Ordinal);

		foreach (string cara in caras)
		{
			string archivo = Path.Combine(raiz, cara);
			string texto = File.Exists(archivo) ? File.ReadAllText(archivo) : "";
			string[] pedidos = [.. System.Text.RegularExpressions.Regex
				.Matches(texto, @"shell\??\.([A-Z][A-Za-z0-9]*)")
				.Select(m => m.Groups[1].Value).Distinct().Order()];
			string[] huerfanos = [.. pedidos.Where(n => !deShell.Contains(n))];

			Assert($"{cara.Split('/')[1]} only asks Shell for members it has",
				texto.Length > 0 && huerfanos.Length == 0,
				texto.Length == 0 ? $"could not read {archivo}"
					: $"calls that do not exist on Shell: {string.Join(", ", huerfanos)}");
		}
	}

	// EVERY data-i18n KEY IN ui.html EXISTS IN THE CATALOGUE.
	//
	// It is what makes the fallback strategy safe: the page carries the Spanish written inside the
	// markup so the mock-up stays readable, and the catalogue OVERWRITES it when it arrives. A
	// misspelt key breaks nothing visible -- the node simply stays in Spanish -- so in English that
	// one sentence would come out in Spanish and nobody would notice until seeing it on a phone.
	// Here it shows at build time.
	{
		string pagina = Path.Combine(raiz, "Gnothi.Mobile", "Assets", "ui.html");
		string html = File.Exists(pagina) ? File.ReadAllText(pagina) : "";
		string[] claves = [.. System.Text.RegularExpressions.Regex
			.Matches(html, @"data-i18n=""([^""]+)""")
			.Select(m => m.Groups[1].Value).Distinct().Order()];

		Gnothi.Host.Texts.Language = "es";
		string[] sinCatalogo = [.. claves.Where(c => Gnothi.Host.Texts.T(c) == c)];

		Assert("ui.html tags at least the static markup",
			claves.Length >= 40, $"only {claves.Length} data-i18n keys found in {pagina}");
		Assert("every data-i18n key in ui.html exists in the catalog",
			sinCatalogo.Length == 0, $"keys with no phrase: {string.Join(", ", sinCatalogo)}");

		// AND EVERY KEY THE JAVASCRIPT ASKS FOR, which is the other half and the one that does not
		// show.
		//
		// The data-i18n ones fail softly -- the node keeps the Spanish the markup brings -- but a
		// misspelt T('key') PAINTS THE KEY on the screen, and only in the branch that reaches it:
		// the sheet of a package whose key changed, the notice of an origin that does not answer.
		// Branches nobody opens by hand.
		//
		// It is static and that is why it is worth more than a run: it covers EVERY call site, not
		// the ones some particular render happens to touch.
		// The T('key') literals, PLUS the keys declared as data and resolved at run time -- today
		// each provider's note, `nota:'prov.nota.groq'`, which reaches T(p.nota) and which a regex
		// over the call cannot see. A dynamic key nobody declares nearby is one this suite cannot
		// look after: that is why they are declared like this.
		string[] pedidas = [.. System.Text.RegularExpressions.Regex
			.Matches(html, @"\bT\('([a-z0-9.-]+)'|\bnota:\s*'([a-z0-9.-]+)'")
			.Select(m => m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value)
			.Distinct().Order()];
		string[] huerfanas = [.. pedidas.Where(c => Gnothi.Host.Texts.T(c) == c)];

		Assert("ui.html asks the catalog for a good number of phrases",
			pedidas.Length >= 60, $"only {pedidas.Length} T() call sites found");
		Assert("every key the page's JavaScript asks for exists in the catalog",
			huerfanas.Length == 0, $"keys with no phrase: {string.Join(", ", huerfanas)}");
	}

	// AND EVERY KEY THE C# ASKS FOR, which is the third surface and the least looked at.
	//
	// The reader returns the key when it does not find it -- ugly on purpose -- so a
	// `Texts.T("ajustes.carpta")` compiles, runs, and paints «ajustes.carpta» on the screen of
	// whoever has that branch. The compiler sees nothing: it is a string. The two assertions above
	// cover ui.html; this one covers the .cs and the .razor, which are the other two faces.
	{
		string[] fuentes = [.. new[] { "Gnothi.Host", "Gnothi.MobileShared", "Gnothi.Assistant", "Gnothi.Desktop", "Gnothi.Mobile", "Gnothi.iOS" }
			.Select(d => Path.Combine(raiz, d))
			.Where(Directory.Exists)
			.SelectMany(d => Directory.EnumerateFiles(d, "*.cs", SearchOption.AllDirectories)
				.Concat(Directory.EnumerateFiles(d, "*.razor", SearchOption.AllDirectories)))
			.Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
			         && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))];

		// `Texts.T("x")` in the .cs, and `T("x")` in the .razor, which call it through the helper.
		// WITHOUT COMMENTS: `_Imports.razor` documents the helper by writing T("clave") in a
		// comment, and reading that as a call site fails the assertion over a key nobody asks for.
		// Razor's @* *@ blocks and C#'s // lines are stripped.
		static string SinComentarios(string s)
		{
			s = System.Text.RegularExpressions.Regex.Replace(s, @"@\*.*?\*@", " ",
				System.Text.RegularExpressions.RegexOptions.Singleline);
			return System.Text.RegularExpressions.Regex.Replace(s, @"^[ \t]*//.*$", " ",
				System.Text.RegularExpressions.RegexOptions.Multiline);
		}

		string[] pedidasCs = [.. fuentes
			.SelectMany(f => System.Text.RegularExpressions.Regex
				.Matches(SinComentarios(File.ReadAllText(f)), @"\bTextos\.T\(""([a-z0-9.-]+)""|@?\bT\(""([a-z0-9.-]+)""")
				.Select(m => m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value))
			.Distinct().Order()];

		Gnothi.Host.Texts.Language = "es";
		string[] sinFrase = [.. pedidasCs.Where(c => Gnothi.Host.Texts.T(c) == c)];

		Assert("the C# and .razor faces ask the catalog for a good number of phrases",
			pedidasCs.Length >= 60, $"only {pedidasCs.Length} Texts.T() call sites found");
		Assert("every key the C# and .razor faces ask for exists in the catalog",
			sinFrase.Length == 0, $"keys with no phrase: {string.Join(", ", sinFrase)}");
	}

	// AND THAT NO TEXT MEANT FOR THE PERSON IS STILL HARD-WIRED IN THE JAVASCRIPT.
	//
	// The two assertions above check that every KEY USED exists. Neither checks the opposite --
	// that every LITERAL has been turned into a key -- and thirty-one labels went through that
	// hole: they were seen on 2026-09-15 on an iPhone with the system in English and Settings
	// entirely in Spanish. Neither the compiler nor a run sees them: the page paints perfectly, in
	// the wrong language. The ones that escaped were all in the JS that builds HTML by
	// concatenating, which is exactly where there is no data-i18n to review.
	//
	// THE RULE IS EXACT AND NOT A LANGUAGE HEURISTIC -- looking for Spanish words does not see
	// «Proveedor» or «Modelo», which were two of the thirty-one. What is demanded is this: IN THE
	// MARKUP THE JAVASCRIPT BUILDS, NO TEXT NODE CARRIES LETTERS. Everything visible comes in
	// through T(...) or through esc(data).
	//
	// It looks only from <script to `const MOCK`: the desktop's mock imitates the voice of
	// fictional institutions -«Laboratorio Clinico Delfos», «Tu salud, en tus manos»- and that is
	// never translated (rule 4).
	{
		string pagina = Path.Combine(raiz, "Gnothi.Mobile", "Assets", "ui.html");
		string html = File.Exists(pagina) ? File.ReadAllText(pagina) : "";
		int desde = html.IndexOf("<script", StringComparison.Ordinal);
		int hasta = html.IndexOf("const MOCK", StringComparison.Ordinal);
		string js = desde >= 0 && hasta > desde ? html[desde..hasta] : "";

		List<string> cableados = [];
		foreach (System.Text.RegularExpressions.Match lit in System.Text.RegularExpressions.Regex
			.Matches(js, @"'((?:[^'\\\r\n]|\\.)*)'|""((?:[^""\\\r\n]|\\.)*)"""))
		{
			string s = lit.Groups[1].Success ? lit.Groups[1].Value : lit.Groups[2].Value;

			// A literal with no markup is code: a protocol id, a selector, a CSS rule.
			if (!s.Contains('<') && !s.Contains('>')) continue;

			// The fragments cut tags in half: the tail of a tag that opens at the end is dropped,
			// and so is the attribute head of one left open by the previous literal.
			string limpio = System.Text.RegularExpressions.Regex.Replace(s, @"<[^>]*$", "");
			limpio = System.Text.RegularExpressions.Regex.Replace(limpio, @"^[^<]*>", "");

			foreach (string fuera in System.Text.RegularExpressions.Regex.Split(limpio, @"<[^>]*>"))
			{
				string txt = System.Text.RegularExpressions.Regex
					.Replace(fuera, @"&[a-z]+;|&#\d+;", " ").Trim();
				if (System.Text.RegularExpressions.Regex.IsMatch(txt, @"\p{L}\p{L}"))
					cableados.Add(txt.Length > 45 ? txt[..45] + "..." : txt);
			}
		}

		Assert("no text for the person is hardcoded in ui.html's JavaScript",
			cableados.Count == 0,
			$"hardcoded: {string.Join(" | ", cableados.Distinct())}");
	}

	// THE OTHER TWO CSPROJ, read as text. Cheap, and it is the exact failure this suite exists
	// to catch: the comment in Gnothi.iOS.csproj invokes this suite, and until now the suite
	// said nothing about what each face declares.
	foreach (string cara in new[] { "Gnothi.Mobile", "Gnothi.iOS" })
	{
		string csproj = Path.Combine(raiz, cara, cara + ".csproj");
		string texto = File.Exists(csproj) ? File.ReadAllText(csproj) : "";
		Assert($"{cara} declares the catalog with the shared LogicalName",
			texto.Contains("gnothi.texts.json", StringComparison.Ordinal),
			File.Exists(csproj)
				? $"{cara}.csproj has no EmbeddedResource for texts.json: that face would start with no catalog"
				: $"could not read {csproj}");
	}
}

Console.WriteLine();
Console.WriteLine(failures == 0
	? "=== TODO PASA: el asistente se puede borrar, la compuerta falla cerrada, y las tres puertas coinciden ==="
	: $"=== {failures} ASSERTION(S) FAIL ===");

return failures == 0 ? 0 : 1;
