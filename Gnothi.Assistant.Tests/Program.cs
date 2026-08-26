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

Console.WriteLine();
Console.WriteLine(failures == 0
	? "=== ALL 4 ASSERTIONS PASS: the assistant can be deleted without rewriting anything ==="
	: $"=== {failures} ASSERTION(S) FAIL ===");

return failures == 0 ? 0 : 1;
