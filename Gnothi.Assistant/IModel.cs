namespace Gnothi.Assistant;

// THE MODEL, AS A DEPENDENCY. The loop does not know which one it is or how to talk to it:
// it hands over the question and the catalog, and receives a decision.
//
// Being an interface is not ceremony. It is what allows switching providers without touching
// the loop, and it is what makes the BYOK decision a matter of configuration, not of code.
public interface IModel
{
	Task<Decision> Ask(string question, IReadOnlyList<ToolForTheModel> tools, CancellationToken ct);

	// Second pass: the tool's result is handed back and the model writes the sentence.
	Task<string> Compose(string question, ToolCall call, string resultJson, CancellationToken ct);
}

// The tool as the model needs to see it: name, what it is for, and the input schema.
// It comes from the Dispatcher's catalog -- the loop does NOT build it.
public sealed record ToolForTheModel(string Name, string Description, string SchemaJson);

// What the model decided. Either it calls a tool, or it answers directly.
public sealed record Decision(ToolCall? Call, string? DirectAnswer)
{
	public static Decision CallTool(string module, string tool, IReadOnlyDictionary<string, object?> args)
		=> new(new ToolCall(module, tool, args), null);

	public static Decision Answer(string text) => new(null, text);
}

public sealed record ToolCall(string Module, string Tool, IReadOnlyDictionary<string, object?> Arguments);
