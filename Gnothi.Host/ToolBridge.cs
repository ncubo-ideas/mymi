using System.Text.Json;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Gnothi.Host;

// THE TRANSLATION: from a tool declared in a manifest to a published MCP tool.
//
// This is the whole "proxy". Gnothi invents no semantics: it takes the name, the title and the
// description the institution wrote, puts the module's prefix on them, and builds a tool whose
// body is a single line -asking the dispatcher to serve it.
//
// THE DESCRIPTION TRAVELS AS IS. It is not documentation: it is the only thing the assistant
// has in order to decide whether this tool answers the person's question. If the Host rewrote
// it, it would be opining about a domain it does not understand.
//
// WHY tools/list AND tools/call ARE ANSWERED BY HAND, instead of synthesizing a delegate:
//
// McpServerTool.Create(delegate, ...) derives the inputSchema from the delegate's SIGNATURE, so
// it can only publish tools whose shape is known at compile time. The Host, by design, knows
// the types of no module: the previous version of this file synthesized a
// Func<CancellationToken, Task<CallToolResult>> -with no parameters- and that is why it skipped,
// with a warning, every tool that had any. The strategy was fighting the goal.
//
// Answering both verbs by hand makes the schema a piece of data instead of a signature: it
// comes out of the manifest and the Host publishes it. It is the path the previous comment
// noted as pending.
//
// WHAT THIS VERSION COVERS: simple text parameters, which is what the manifest models today
// (name, type, description). WHAT IT DOES NOT: enums, optionals, nested. The lab-tests module
// publishes its analyte as an ENUM of four values -measured by asking it for its tools/list-
// and the manifest cannot express that, so the valid values are repeated in the description.
// Reworking the manifest to carry the schema is a format decision, and it is still open.
internal static class ToolBridge
{
	// tools/list. It returns ONLY the modules' tools: the Host's own live in the server's
	// collection and the SDK adds them to this list.
	//
	// THE INDEX IS SHARED WITH GATE 2 -- see ToolIndex. What is this gate's own is the
	// projection: here the schema becomes a JsonElement for MCP; there, a string for the model.
	public static ValueTask<ListToolsResult> List(
		Dispatcher dispatcher, RequestContext<ListToolsRequestParams> _, CancellationToken __)
	{
		List<Tool> tools = [];

		foreach (ToolIndex.Entry e in ToolIndex.Of(dispatcher).All)
		{
			tools.Add(new Tool
			{
				Name = e.PublicName,
				Title = string.IsNullOrWhiteSpace(e.Tool.Title) ? null : e.Tool.Title,
				Description = e.Tool.Description,
				InputSchema = JsonSerializer.SerializeToElement(ToolIndex.InputSchema(e.Tool)),
				Annotations = new ToolAnnotations { ReadOnlyHint = e.Tool.ReadOnly },
			});
		}

		return ValueTask.FromResult(new ListToolsResult { Tools = tools });
	}

	// tools/call. It resolves the public name against the index and delegates to the dispatcher.
	//
	// A name that is not in the index is NOT a Host error: it is a tool from another provider
	// -the Host's own 'gnothi_' ones- or one the person switched off. The answer is that it does
	// not exist, which is what the SDK would answer, and without saying which of the two cases
	// it is: telling them apart would leak that the tool exists and is switched off.
	public static async ValueTask<CallToolResult> Call(
		Dispatcher dispatcher, RequestContext<CallToolRequestParams> request, CancellationToken ct)
	{
		string name = request.Params?.Name ?? "";

		if (!ToolIndex.Of(dispatcher).TryFind(name, out ToolIndex.Entry e))
			throw new McpProtocolException($"Unknown tool: '{name}'");

		Response r = await dispatcher.Serve(
			new Request(e.Module.Name, e.Tool.Name, Arguments(request.Params)), ct);

		if (!r.Authorized)
			return new CallToolResult
			{
				IsError = true,
				Content = [new TextContentBlock { Text = r.Reason }],
			};

		// THE CONTENT ARRIVES OPAQUE, and now it arrives in ONE shape whatever the shell was:
		// the module's facts. Dressing them as MCP is this gate's job and nobody else's, which
		// is why this is the only place in the Host that knows the protocol.
		//
		// There used to be a branch here for stdio, which forwarded the module's own
		// CallToolResult whole. It looked like fidelity and it was an asymmetry: Gate 2 got a
		// different shape depending on the shell and had no branch to notice. StdioShell now
		// unwraps, so the seam keeps the promise it declares.
		return new CallToolResult
		{
			Content = [new TextContentBlock { Text = JsonSerializer.Serialize(r.Content) }],
			StructuredContent = JsonSerializer.SerializeToElement(r.Content),
		};
	}

	// The arguments arrive as JsonElement and are passed straight through: the Host does not
	// interpret them. If the module expects an enum and gets text that does not match, the one
	// who complains is the module, which is the only one that knows what its values are.
	private static IReadOnlyDictionary<string, object?>? Arguments(CallToolRequestParams? p)
	{
		if (p?.Arguments is null || p.Arguments.Count == 0) return null;

		Dictionary<string, object?> args = [];
		foreach (KeyValuePair<string, JsonElement> pair in p.Arguments) args[pair.Key] = pair.Value;
		return args;
	}
}
