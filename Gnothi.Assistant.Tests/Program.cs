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

string modulos = Gnothi.Conformance.Conformance.FindModules();

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

Console.WriteLine();
Console.WriteLine(failures == 0
	? "=== TODO PASA: el asistente se puede borrar, la compuerta falla cerrada, y las tres puertas coinciden ==="
	: $"=== {failures} ASSERTION(S) FAIL ===");

return failures == 0 ? 0 : 1;
