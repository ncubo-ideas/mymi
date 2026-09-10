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

// Un argumento manda: `dotnet run --project Gnothi.Assistant.Tests -- Modulos.local` corre la
// suite contra el banco de ESTA maquina. Sin argumento busca ./Modulos, que esta versionado y
// trae las rutas de la maquina donde se armo el POC: ahi el catalogo se compara igual, pero
// ningun modulo arranca y la primera aserción de la conformance lo dice.
string modulos = args.Length > 0 && Directory.Exists(args[0])
	? Path.GetFullPath(args[0])
	: Gnothi.Conformance.Conformance.FindModules();

if (modulos.Length == 0)
{
	Assert("se encontro el directorio Modulos", false,
		"no se hallo ./Modulos subiendo desde el binario; la suite NO corrio");
}
else
{
	// La suite arranca cada modulo y cruza su manifiesto contra las tools REALES antes de comparar
	// las tres puertas: un catalogo que no coincide con los modulos compararia la copia equivocada.
	// Ese guardian corre PRIMERO dentro de Run (Conformance.cs).
	Console.WriteLine();
	Console.WriteLine("=== LAS TRES PUERTAS CONTESTAN LO MISMO ===");
	await Gnothi.Conformance.Conformance.Run(modulos, Assert);

	// EL APAGADO SE RESPETA POR TODAS LAS PUERTAS. Corre DESPUES de la conformance porque
	// necesita su propio catalogo -- apaga y enciende tools para medir la compuerta -- y no
	// tiene por que mezclarse con el que la comparacion usa. Ver Conformance.Consentimiento.
	Console.WriteLine();
	Console.WriteLine("=== UNA TOOL APAGADA NO SE CUELA POR NINGUNA PUERTA ===");
	await Gnothi.Conformance.Conformance.Consentimiento(modulos, Assert);
}

// ============================================================================================
// THE PACKAGE IS INSPECTED BEFORE ANYBODY ACCEPTS IT, AND THE SAME WAY ON BOTH INSTALLERS.
// Synthetic packages, built in memory: see Enrollment.cs.
// ============================================================================================

Console.WriteLine();
Console.WriteLine("=== THE PACKAGE IS INSPECTED BEFORE SHE DECIDES ===");
Gnothi.Tests.Enrollment.Run(Assert);

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
// EL INSTALADOR ESCRIBE UN installation.json QUE EL HOST PUEDE LEER.
//
// Regresion. El refactor a JSON sin reflexion (88e0b95) dejo a --install-module serializando
// InstallationFile con el type-info de Installation: compilaba, y reventaba con
// InvalidCastException en cada instalacion, en todas las plataformas. Nadie lo vio porque el
// unico camino que lo ejercitaba era la prueba manual. Este bloque instala el .gmod que el repo
// ya versiona (el que sirve la pagina de La Guaria) en una carpeta temporal y lee el resultado
// con el MISMO lector que usa el catalogo. Si el escritor y el lector se separan, esto falla.
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
			// grantBase dentro del temp: la prueba no toca Documents/Gnothi de la persona.
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

Console.WriteLine();
Console.WriteLine(failures == 0
	? "=== TODO PASA: el asistente se puede borrar, la compuerta falla cerrada, y las tres puertas coinciden ==="
	: $"=== {failures} ASSERTION(S) FAIL ===");

return failures == 0 ? 0 : 1;
