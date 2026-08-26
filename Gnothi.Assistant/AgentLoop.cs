using System.Text.Json;
using Gnothi.Host;

namespace Gnothi.Assistant;

// THE AGENT LOOP. An adapter, not a product.
//
// THE ONLY THING IT KNOWS IS THE DISPATCHER. Notice what is NOT in this class:
//   - no reference to module instances
//   - no directory, no path, no runtime
//   - no policy: permission is applied by the dispatcher, not by the loop
//   - and it does NOT build the tool list: it asks the dispatcher, which is the SAME list it
//     would hand to any other caller
//
// That is the "with a seam" shape from NotasGnothi/02-el-host.md. The tangled shape -- the
// loop owning the modules, the permissions and the quotas -- works just as well and makes it
// impossible to delete the loop the day it becomes unnecessary, because it takes the module
// logic with it.
public sealed class AgentLoop
{
	private readonly Dispatcher host;   // <- the only thing it knows
	private readonly IModel model;

	public AgentLoop(Dispatcher host, IModel model)
	{
		this.host = host;
		this.model = model;
	}

	// THE CATALOG AS THE MODEL SEES IT. Public because it is measurable and because it is
	// what gets sent on EVERY question: its size is the fixed cost of one query.
	public IReadOnlyList<ToolForTheModel> CatalogForTheModel()
	{
		List<ToolForTheModel> tools = [];
		foreach ((Module m, DeclaredTool t) in host.Catalog())
			tools.Add(new ToolForTheModel(Module.PublicName(m.Name, t.Name), t.Description, SchemaOf(t)));
		return tools;
	}

	// One question from the person, one answer in words.
	public async Task<string> Answer(string question, Action<string>? trace, CancellationToken ct)
	{
		void Say(string s) => trace?.Invoke(s);

		// 1 · THE CATALOG. Asked of the dispatcher, which already filtered it by consent.
		//     If the person turned a query off, the model never sees it.
		IReadOnlyList<ToolForTheModel> tools = CatalogForTheModel();
		Say($"catalog: {tools.Count} tools");
		if (tools.Count == 0) return "You have no module with available queries.";

		// 2 · THE MODEL DECIDES. The loop has no opinion on which tool fits: that is what the
		//     descriptions each institution wrote are for.
		Decision d = await model.Ask(question, tools, ct);

		if (d.Call is null) return d.DirectAnswer ?? "(the model answered nothing)";

		Say($"the model chose: {d.Call.Module}_{d.Call.Tool}");

		// 3 · THE DISPATCHER EXECUTES. Permission is checked, the query is counted and the
		//     module is invoked here. None of that is the loop's job.
		Response r = await host.Serve(new Request(d.Call.Module, d.Call.Tool, d.Call.Arguments), ct);

		if (!r.Authorized)
		{
			Say($"denied: {r.Reason}");
			return $"That query is not available: {r.Reason}";
		}

		string json = JsonSerializer.Serialize(r.Content);
		Say($"the module answered {json.Length} characters");

		// 4 · THE MODEL COMPOSES. The facts are the module's; the sentence is the assistant's.
		return await model.Compose(question, d.Call, json, ct);
	}

	// The schema the model needs, built from what the manifest declares. Same limitation Gate
	// 1 has: the manifest only models text.
	private static string SchemaOf(DeclaredTool t)
	{
		Dictionary<string, object> props = [];
		foreach (DeclaredParameter p in t.Input)
			props[p.Name] = new Dictionary<string, object>
			{
				["type"] = "string",
				["description"] = p.Description,
			};

		Dictionary<string, object> schema = new()
		{
			["type"] = "object",
			["properties"] = props,
		};
		if (t.Input.Count > 0) schema["required"] = t.Input.Select(p => p.Name).ToArray();

		return JsonSerializer.Serialize(schema);
	}
}
