namespace Gnothi.Assistant;

// THE MODEL, AS A DEPENDENCY. The loop does not know which one it is or how to talk to it:
// it hands over the exchange so far plus the catalog, and receives one step.
//
// Being an interface is not ceremony. It is what allows switching providers without touching
// the loop, and it is what makes the BYOK decision a matter of configuration, not of code.
//
// IT IS CONVERSATIONAL, and it has to be. The earlier shape -- Ask once, then Compose --
// could only ever express ONE hop, and that is not a detail: crossing two modules in a single
// turn is the whole reason this project exists. A model asked to look at lab tests AND
// invoices answers with TWO tool calls, and a contract with room for one silently drops the
// second. Composition is not a separate method either: the step with no tool calls IS the
// composition.
public interface IModel
{
	Task<Said> Next(
		IReadOnlyList<Exchange> conversation, IReadOnlyList<ToolForTheModel> tools, CancellationToken ct);
}

// The tool as the model needs to see it: name, what it is for, and the input schema.
// It comes from the Host's shared index -- the loop does NOT build it.
public sealed record ToolForTheModel(string Name, string Description, string SchemaJson);

// ONE ENTRY OF THE EXCHANGE. Three kinds, because three parties speak: the person, the model,
// and the modules.
public abstract record Exchange;

// The person.
public sealed record Asked(string Question) : Exchange;

// The model: words, tool calls, or both in the same step.
public sealed record Said(string? Text, IReadOnlyList<ToolCall> Calls) : Exchange;

// A module, answering one call. CallId is not decoration: it is what pairs the facts with the
// call that asked for them, and without it a step with two calls cannot be answered at all.
public sealed record Returned(string CallId, string Json, bool Failed) : Exchange;

public sealed record ToolCall(string Id, string PublicName, IReadOnlyDictionary<string, object?> Arguments);
