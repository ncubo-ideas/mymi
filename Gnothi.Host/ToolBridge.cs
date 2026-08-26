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
	// The index of public name -> who contributes it. It is built from the catalog, which
	// already comes filtered by consent: a disabled tool does not enter the index, so it is not
	// published and cannot be called.
	//
	// The public name is NOT split on the underscore to get back to the pair:
	// 'total_de_facturas' has three. The pair is kept when the name is built, which is the only
	// moment it is known without ambiguity.
	private static Dictionary<string, (Module module, DeclaredTool tool)> Index(Dispatcher dispatcher)
	{
		Dictionary<string, (Module, DeclaredTool)> index = new(StringComparer.Ordinal);

		foreach ((Module m, DeclaredTool t) in dispatcher.Catalog())
			index[Module.PublicName(m.Name, t.Name)] = (m, t);

		return index;
	}

	// tools/list. It returns ONLY the modules' tools: the Host's own live in the server's
	// collection and the SDK adds them to this list.
	public static ValueTask<ListToolsResult> List(
		Dispatcher dispatcher, RequestContext<ListToolsRequestParams> _, CancellationToken __)
	{
		List<Tool> tools = [];

		foreach ((string publicName, (Module module, DeclaredTool declared)) in Index(dispatcher))
		{
			tools.Add(new Tool
			{
				Name = publicName,
				Title = string.IsNullOrWhiteSpace(declared.Title) ? null : declared.Title,
				Description = declared.Description,
				InputSchema = InputSchema(declared),
				Annotations = new ToolAnnotations { ReadOnlyHint = declared.ReadOnly },
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

		if (!Index(dispatcher).TryGetValue(name, out (Module module, DeclaredTool tool) pair))
			throw new McpProtocolException($"Unknown tool: '{name}'");

		Response r = await dispatcher.Serve(
			new Request(pair.module.Name, pair.tool.Name, Arguments(request.Params)), ct);

		if (!r.Authorized)
			return new CallToolResult
			{
				IsError = true,
				Content = [new TextContentBlock { Text = r.Reason }],
			};

		// The module's CallToolResult is returned WHOLE and untouched, with its
		// structuredContent and its _meta. The Host is a postman.
		return (CallToolResult)r.Content!;
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

	// The input schema, built from what the manifest declares.
	//
	// Every parameter ends up required because the manifest has no way to say that one is
	// optional. It is a visible simplification: when an optional is needed, the manifest will
	// have to grow, and it is better for that to show now than to discover it when a tool
	// fails.
	private static JsonElement InputSchema(DeclaredTool declared)
	{
		Dictionary<string, object> properties = [];

		foreach (DeclaredParameter p in declared.Input)
		{
			properties[p.Name] = new Dictionary<string, object>
			{
				// Everything is published as text. The manifest only models simple types, and
				// lying about the type would be worse than simplifying it: a schema that says
				// 'number' about something that is not one makes the client reject the call
				// before the module gets to have an opinion.
				["type"] = "string",
				["description"] = p.Description,
			};
		}

		Dictionary<string, object> schema = new()
		{
			["type"] = "object",
			["properties"] = properties,
		};

		if (declared.Input.Count > 0)
			schema["required"] = declared.Input.Select(p => p.Name).ToArray();

		return JsonSerializer.SerializeToElement(schema);
	}
}
