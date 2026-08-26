namespace Gnothi.Host;

// THE SHELL, as a shape. It used to be a concrete class -- StdioShell -- named directly by
// Module. Now it is a shape with implementations, and the choice lives in each module's
// installation.json, which belongs to the Host.
//
// Notice what this interface does NOT mention: any MCP type. The response content travels
// as `object` and the front interprets it. That is the property that keeps a new shell from
// inheriting the previous one's protocol -- and it had to be fixed before adding the second
// one, because Module.Invoke used to return a CallToolResult.
internal interface IShell : IAsyncDisposable
{
	bool IsRunning { get; }

	// The content comes back OPAQUE. The stdio shell returns the module's CallToolResult as
	// it is; the in-process shell returns the projection of whatever the method returned.
	// The Host looks at neither: it forwards them.
	Task<object> Invoke(string tool, IReadOnlyDictionary<string, object?>? arguments, CancellationToken ct);

	// The NAMES of what the module actually exposes, for the declared-versus-actual check.
	// Names and not protocol objects: names are the only thing being compared.
	Task<IReadOnlyList<string>> ActualTools(CancellationToken ct);
}
