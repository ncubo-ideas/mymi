using System.Diagnostics;
using System.Text.Json;
using Gnothi.Assistant;
using Gnothi.Host;

// GATE 2 WITH ENGLISH QUESTIONS AGAINST SPANISH MODULES. Three measurements per question, and
// the middle one is the one that decides:
//
//   1. SELECTION   which tools the model picked -- reading Spanish descriptions from English.
//   2. ARGUMENTS   what VALUES it passed. Free-text parameters (compras: 'termino', 'producto')
//                  are matched against the journal with IndexOf, so "milk" finds nothing where
//                  "leche" finds two months. Enum parameters (examenes: 'analito') cannot miss:
//                  the schema lists the exact Spanish values and the model copies one.
//   3. ANSWER      in which language it composed, given that the encuadre says "en su idioma"
//                  and the domain's own sentences (paraElAsistente, lectura) arrive in Spanish.
//
// Nothing here is a fixture: the catalog is the installed Modulos/ and the data is the journal
// on this machine. That is on purpose -- a lexical miss has to be a real miss.

string root = args.Length > 0 && !args[0].StartsWith("--") ? args[0] : DefaultRoot();
var catalog = new ModuleCatalog(root);
foreach (string w in catalog.Load()) Console.Error.WriteLine($"warning: {w}");
var dispatcher = new Dispatcher(catalog, new Meter(), Accepts.JsonOnly);

IReadOnlyList<ToolForTheModel> tools = AgentLoop.CatalogFor(dispatcher);
Console.WriteLine($"modules   : {catalog.Modules.Count} in {root}");
Console.WriteLine($"catalog   : {tools.Count} tools");

// --catalog NEEDS NO KEY. It prints what the model is shown: the name, the enum values if the
// parameter is closed, and the head of the description. That alone shows the mixed catalog --
// Host instructions in English, module descriptions in Spanish, enum values in Spanish.
if (args.Contains("--catalog"))
{
	foreach (ToolForTheModel t in tools)
	{
		string head = t.Description.Length > 90 ? t.Description[..90] + "..." : t.Description;
		Console.WriteLine($"\n{t.Name}");
		Console.WriteLine($"  description : {head}");
		foreach (string line in ParameterSummary(t.SchemaJson)) Console.WriteLine($"  parameter   : {line}");
	}
	return 0;
}

string keyFile = Environment.GetEnvironmentVariable("GNOTHI_KEYFILE")
	?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".gnothi-model-key");

IModel model;
try { model = new Tracing(new ModelClaude(keyFile)); }
catch (InvalidOperationException e) { Console.WriteLine($"\nNO KEY: {e.Message}"); return 2; }

// THE QUESTIONS. Each one targets a different place where the language lives. A single
// question passed as argument replaces the set.
string[] questions = args.Length > 1 && !args[1].StartsWith("--") ? [args[1]]
	: args.Length == 1 && !args[0].StartsWith("--") && !Directory.Exists(args[0]) ? [args[0]]
	: [
		// free text against a Spanish journal: does the model translate "milk" to "leche"?
		"How often do I buy milk, and has it changed?",
		// free text, warranty: "rice cooker" vs the journal's "Olla arrocera 1.8L"
		"Is my rice cooker still under warranty?",
		// closed enum in Spanish: the model has to pick ColesterolTotal from "cholesterol"
		"Is my cholesterol going up?",
		// the cross, in English, no module named
		"Could my cholesterol have anything to do with what I buy at the supermarket?",
		// a Host tool with an English description, to see it is not the language that routes
		"What modules do I have installed?",
	];

var loop = new AgentLoop(dispatcher, model);
int i = 0;
foreach (string q in questions)
{
	i++;
	Console.WriteLine($"\n=== Q{i}: {q}");
	var clock = Stopwatch.StartNew();
	string answer;
	try { answer = await loop.Answer(q, s => Console.WriteLine($"    {s}"), CancellationToken.None); }
	catch (Exception e) { Console.WriteLine($"FAILED: {e.GetType().Name}: {e.Message}"); continue; }
	Console.WriteLine($"--- answer ({clock.Elapsed.TotalSeconds:F1} s):\n{answer}");
}

return 0;

static string DefaultRoot()
{
	string dir = AppContext.BaseDirectory;
	// Walk up until a Modulos/ appears: the build output may be redirected with BaseOutputPath.
	for (int up = 0; up < 8; up++)
	{
		string candidate = Path.Combine(dir, "Modulos");
		if (Directory.Exists(candidate)) return Path.GetFullPath(candidate);
		dir = Path.GetFullPath(Path.Combine(dir, ".."));
	}
	return Path.GetFullPath("Modulos");
}

static IEnumerable<string> ParameterSummary(string schemaJson)
{
	using JsonDocument doc = JsonDocument.Parse(schemaJson);
	if (!doc.RootElement.TryGetProperty("properties", out JsonElement props)) yield break;
	foreach (JsonProperty p in props.EnumerateObject())
	{
		string type = p.Value.TryGetProperty("type", out JsonElement t) ? t.ToString() : "?";
		string kind = p.Value.TryGetProperty("enum", out JsonElement e)
			? "closed  [" + string.Join(", ", e.EnumerateArray().Select(x => x.GetString())) + "]"
			: type == "string" ? "free text" : $"open {type}";
		string desc = p.Value.TryGetProperty("description", out JsonElement d) ? d.GetString() ?? "" : "";
		yield return $"{p.Name}: {kind}\n                {(desc)}";
	}
}

// PRINTS EVERY CALL WITH ITS ARGUMENTS. AgentLoop's trace only names the tool; the argument
// value is exactly what this spike measures, so it is read here, before the loop runs it.
sealed class Tracing(IModel inner) : IModel
{
	private int seen;   // Returned entries already printed

	public async Task<Said> Next(
		IReadOnlyList<Exchange> conversation, IReadOnlyList<ToolForTheModel> tools, CancellationToken ct)
	{
		if (conversation.Count == 1) seen = 0;   // a new question, a new exchange
		// What the modules answered since the previous step, so a miss shows up right under
		// the argument value that caused it.
		foreach (Returned r in conversation.OfType<Returned>().Skip(seen))
		{
			Console.WriteLine($"    got  {(r.Json.Length > 200 ? r.Json[..200] + "..." : r.Json)}");
			seen++;
		}
		Said said = await inner.Next(conversation, tools, ct);
		foreach (ToolCall c in said.Calls)
		{
			string a = string.Join(", ", c.Arguments.Select(kv => $"{kv.Key}={JsonSerializer.Serialize(kv.Value)}"));
			Console.WriteLine($"    call {c.PublicName}({a})");
		}
		return said;
	}
}
