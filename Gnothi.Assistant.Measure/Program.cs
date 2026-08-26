using System.Text.Json;
using Gnothi.Host;
using Gnothi.Assistant;

// WHAT ONE OF HER QUESTIONS COSTS, measured WITHOUT sending anything.
//
// The cost per query has two parts:
//   FIXED     the instructions + the tool catalog. It travels on EVERY question.
//   VARIABLE  her question, and then the facts the module returned.
//
// CHARACTERS are measured exactly. Tokens are a DECLARED ESTIMATE: with no key there is no
// tokenizer, and approximating with a ratio is only honest if you say it is an approximation.

string root = args.Length > 0 ? args[0] : "Modulos";
var catalog = new ModuleCatalog(Path.GetFullPath(root));
IReadOnlyList<string> warnings = catalog.Load();
var dispatcher = new Dispatcher(catalog, new Meter());
var loop = new AgentLoop(dispatcher, new MeasuringModel());

foreach (string w in warnings) Console.WriteLine($"warning: {w}");

IReadOnlyList<ToolForTheModel> tools = loop.CatalogForTheModel();
Console.WriteLine($"modules: {catalog.Modules.Count}   published tools: {tools.Count}");
Console.WriteLine();

const double CharsPerToken = 3.7;   // Spanish + JSON. AN ESTIMATE.
string Est(int chars) => $"~{Math.Round(chars / CharsPerToken):N0}";

Console.WriteLine("--- FIXED COST: what travels on EVERY question ---");
int instr = ModelClaude.Instructions.Length;
Console.WriteLine($"  system instructions         {instr,7:N0} chars   {Est(instr),8} tokens");

int toolsTotal = 0;
Console.WriteLine();
Console.WriteLine("  the catalog, tool by tool:");
foreach (ToolForTheModel t in tools)
{
	int n = t.Name.Length + t.Description.Length + t.SchemaJson.Length;
	toolsTotal += n;
	Console.WriteLine($"    {t.Name,-34} {n,6:N0} chars   {Est(n),8}");
}
Console.WriteLine($"  {"whole catalog",-36} {toolsTotal,6:N0} chars   {Est(toolsTotal),8} tokens");

int fixedCost = instr + toolsTotal;
Console.WriteLine();
Console.WriteLine($"  FIXED COST PER QUESTION      {fixedCost,7:N0} chars   {Est(fixedCost),8} tokens");

Console.WriteLine();
Console.WriteLine("--- THE WHOLE REQUEST, serialized as it would go over the wire ---");
string question = "how fast is my prostate antigen rising?";
string json = JsonSerializer.Serialize(ModelClaude.Body("claude-sonnet-5", question, tools));
Console.WriteLine($"  sample question              {question.Length,7:N0} chars");
Console.WriteLine($"  POST body JSON               {json.Length,7:N0} chars   {Est(json.Length),8} tokens");

Console.WriteLine();
Console.WriteLine("--- AND THE SECOND PASS: the facts the module returns ---");
foreach ((Module m, DeclaredTool t) in dispatcher.Catalog())
{
	Dictionary<string, object?>? a = null;
	if (t.Input.Count > 0)
	{
		a = [];
		foreach (DeclaredParameter p in t.Input)
			a[p.Name] = p.Name == "analito" ? "AntigenoProstatico"
				: p.Name == "producto" ? "olla arrocera"
				: p.Name.StartsWith("anio") ? "2026" : "8";
	}
	try
	{
		Response r = await dispatcher.Serve(new Request(m.Name, t.Name, a), CancellationToken.None);
		if (!r.Authorized) { Console.WriteLine($"  {t.Name,-34} DENIED"); continue; }
		int n = JsonSerializer.Serialize(r.Content).Length;
		Console.WriteLine($"  {Module.PublicName(m.Name, t.Name),-34} {n,6:N0} chars   {Est(n),8}");
	}
	catch (Exception e) { Console.WriteLine($"  {t.Name,-34} FAILED {e.GetType().Name}"); }
}

Console.WriteLine();
Console.WriteLine("CHARS are measured. Tokens are an estimate at 3.7 chars/token:");
Console.WriteLine("with no key there is no tokenizer, and saying 'measured' would be a lie.");

// A model that calls nobody: this tool MEASURES the request, it does not send it.
internal sealed class MeasuringModel : IModel
{
	public Task<Decision> Ask(string q, IReadOnlyList<ToolForTheModel> t, CancellationToken ct)
		=> throw new NotSupportedException("this tool measures, it does not ask");
	public Task<string> Compose(string q, ToolCall c, string r, CancellationToken ct)
		=> throw new NotSupportedException("this tool measures, it does not ask");
}
