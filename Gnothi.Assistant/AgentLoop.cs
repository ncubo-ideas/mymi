using System.Text.Json;
using Gnothi.Host;

namespace Gnothi.Assistant;

// THE AGENT LOOP. An adapter, not a product.
//
// THE ONLY THING IT KNOWS IS THE DISPATCHER. Notice what is NOT in this class:
//   - no reference to module instances
//   - no directory, no path, no runtime
//   - no policy: permission is applied by the dispatcher, not by the loop
//   - and it does NOT build the tool list: it asks the Host's index, which is the SAME one
//     Gate 1 publishes from
//
// That is the "with a seam" shape from NotasGnothi/02-el-host.md. The tangled shape -- the
// loop owning the modules, the permissions and the quotas -- works just as well and makes it
// impossible to delete the loop the day it becomes unnecessary, because it takes the module
// logic with it.
//
// AND IT IS A LOOP, which it was not. The first version asked once, ran one tool and composed.
// That cannot cross two modules in one turn, which is the thesis of the whole project: asked
// to look at lab tests and invoices together, the model returns two calls, the old code took
// the first and the answer read as if those were all the facts. It failed in SILENCE, which is
// the worst way to fail.
public sealed class AgentLoop
{
	private readonly Dispatcher host;   // <- the only thing it knows
	private readonly IModel model;

	// Bounded on purpose. A model that keeps asking for queries is a real failure mode, and an
	// unbounded loop spends the person's money while it happens.
	private const int MaxRounds = 6;

	public AgentLoop(Dispatcher host, IModel model)
	{
		this.host = host;
		this.model = model;
	}

	// THE CATALOG AS THE MODEL SEES IT. Public because it is measurable and because it is
	// what gets sent on EVERY question: its size is the fixed cost of one query.
	public IReadOnlyList<ToolForTheModel> CatalogForTheModel() => CatalogFor(host);

	// The same catalog, WITHOUT needing a model. It is what makes the cost of a question
	// measurable before there is a key -- and on a phone, where the key is exactly what has not
	// been decided, it is the only part of Gate 2 that can be shown working.
	public static IReadOnlyList<ToolForTheModel> CatalogFor(Dispatcher host) => ForTheModel(ToolIndex.Of(host));

	// The projection that belongs to THIS gate. Gate 1 turns the same schema into a JsonElement
	// for MCP; here it becomes a string for the model. The index underneath is shared.
	private static IReadOnlyList<ToolForTheModel> ForTheModel(ToolIndex index) =>
		[.. index.All.Select(e => new ToolForTheModel(
			e.PublicName,
			e.Tool.Description,
			JsonSerializer.Serialize(ToolIndex.InputSchema(e.Tool))))];

	// One question from the person, one answer in words.
	public async Task<string> Answer(string question, Action<string>? trace, CancellationToken ct)
	{
		void Say(string s) => trace?.Invoke(s);

		// THE INDEX IS BUILT ONCE PER QUESTION, and both things come out of it: the catalog the
		// model is shown, and the resolution of whatever it picks. So a name that was published
		// always resolves, and one that was not never does -- without cutting strings.
		//
		// Per question and not cached, because the catalog is already filtered by consent and
		// the person can switch a query off between one question and the next.
		ToolIndex index = ToolIndex.Of(host);
		IReadOnlyList<ToolForTheModel> tools = ForTheModel(index);

		Say($"catalog: {tools.Count} tools");
		if (tools.Count == 0) return "You have no module with available queries.";

		List<Exchange> conversation = [new Asked(question)];

		for (int round = 1; round <= MaxRounds; round++)
		{
			Said said = await model.Next(conversation, tools, ct);
			conversation.Add(said);

			// No calls means the model is done: this step IS the composition. The facts are the
			// modules'; the sentence is the assistant's.
			if (said.Calls.Count == 0)
				return said.Text ?? "(the model answered nothing)";

			Say($"round {round}: {said.Calls.Count} " + (said.Calls.Count == 1 ? "query" : "queries"));

			// EVERY call of the round. This is the line the cross lives on.
			foreach (ToolCall call in said.Calls)
				conversation.Add(await Run(index, call, Say, ct));
		}

		return "I could not finish the answer: the assistant kept asking for queries and I stopped it.";
	}

	// ONE CALL. A failure is handed BACK TO THE MODEL as a result, not thrown: if one of the two
	// modules in a cross is switched off, the right answer is the other module's facts plus the
	// news that this one is not available -- not an exception that loses both.
	private async Task<Returned> Run(ToolIndex index, ToolCall call, Action<string> say, CancellationToken ct)
	{
		// RESOLVED AGAINST THE INDEX, never by cutting the name. 'facturas_total_de_facturas'
		// has three underscores and only the index knows which prefix is a module. The previous
		// version cut on the first one, which happens to work as long as no module is ever
		// called 'mi_super' -- and nothing enforces that.
		if (!index.TryFind(call.PublicName, out ToolIndex.Entry e))
		{
			say($"  {call.PublicName}: does not exist");
			return new Returned(call.Id, $"there is no query called '{call.PublicName}'", Failed: true);
		}

		Response r = await host.Serve(new Request(e.ModuleName, e.Tool.Name, call.Arguments), ct);

		if (!r.Authorized)
		{
			say($"  {call.PublicName}: denied");
			return new Returned(call.Id, r.Reason, Failed: true);
		}

		// Serialized plainly, because BOTH shells now hand back the module's facts. This line
		// used to send the model the MCP envelope whenever the module ran over stdio: the facts
		// escaped inside a string, 525 characters where in-process sent 126.
		string json = JsonSerializer.Serialize(r.Content);
		say($"  {call.PublicName}: {json.Length} characters");
		return new Returned(call.Id, json, Failed: false);
	}
}
