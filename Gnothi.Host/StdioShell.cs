using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Gnothi.Host;

// THE SHELL: how the Host brings a module up and talks to it.
//
// In this POC the module ships its own MCP server, so the shell is child process + stdio, and
// the Host acts as an MCP CLIENT. That makes the POC honest in the link that matters -Claude
// does not know there are two hops- at the cost of the module carrying a transport, which is
// exactly what the target architecture takes away from it.
//
// WHAT THIS CLASS IS: the ONLY place in Gnothi that knows HOW a module is executed. Nothing
// else names it. Stage 2 replaces this file with one that instantiates a wasm runtime with
// --dir and touches no other line of the Host. If changing the hosting meant editing the
// dispatcher or the catalog, the abstraction would be in the wrong place.
//
// LAZY START, on purpose: reading twenty manifests is reading twenty json files; starting
// twenty modules is twenty journal replays. The catalog does NOT need the module alive.
internal sealed class StdioShell : IAsyncDisposable
{
	private readonly string name;
	private readonly Shell shell;
	private readonly string grantedFolder;
	private readonly SemaphoreSlim gate = new(1, 1);

	private McpClient? client;

	public StdioShell(string name, Shell shell, string grantedFolder)
	{
		this.name = name;
		this.shell = shell;
		this.grantedFolder = grantedFolder;
	}

	public bool IsRunning => client is not null;

	// A single gate around the start: two concurrent tool calls on a sleeping module would
	// bring up two processes, and the second one would hang there with nobody to close it.
	private async Task<McpClient> Start(CancellationToken ct)
	{
		if (client is not null) return client;

		await gate.WaitAsync(ct);
		try
		{
			if (client is not null) return client;

			// The granted folder is passed as an ARGUMENT, not as an environment variable nor
			// as something the module can look up from the system. The Host is the one who
			// says where.
			string[] arguments = [.. shell.Arguments, grantedFolder];

			var transport = new StdioClientTransport(new StdioClientTransportOptions
			{
				Name = name,
				Command = shell.Command,
				Arguments = arguments,
				// The child's stderr is NOT ours: it is forwarded to ours with a prefix, so
				// that a module's noise is traceable back to the module and, above all, so
				// that it never touches stdout — which is the protocol's channel.
				StandardErrorLines = line => Console.Error.WriteLine($"[module:{name}] {line}"),
			});

			client = await McpClient.CreateAsync(transport, cancellationToken: ct);
			return client;
		}
		finally { gate.Release(); }
	}

	// What the module says about itself OVER THE PROTOCOL. It is not used to build the catalog
	// -that is what the manifest is for- but to VERIFY it: declared against real.
	//
	// It also evicts: asking a module who it is is no reason to leave it holding the person's
	// folder open.
	public async Task<IReadOnlyList<McpClientTool>> ActualTools(CancellationToken ct)
	{
		try
		{
			return [.. await (await Start(ct)).ListToolsAsync(cancellationToken: ct)];
		}
		finally { await Evict(); }
	}

	// The module's CallToolResult is returned whole and untouched.
	//
	// The Host does not parse nor reformat the response, and that is not laziness: if it
	// transcoded, it would have to understand the domain, and any loss in the conversion would
	// become a Host bug living inside the person's data.
	//
	// IT EVICTS AFTER EVERY CALL, and it cost a bug to understand why.
	//
	// The module used to stay up forever from the first call on. It looked efficient: a single
	// hydration, fast answers. But a live module HAS THE JOURNAL FOLDER OPEN, and hydrating an
	// actor is not a passive read: it creates the whole layout (outbox, materialization, skips,
	// reactions, followers, index, elision) and touches meta.bin.
	//
	// With the person's app open in the browser over the SAME folder, that invalidates the
	// handles it had cached, and its next flush dies with InvalidStateError: "the state changed
	// since it was read from disk". That is two writers on one journal, which is exactly what
	// the model forbids.
	//
	// Evicting does not close the window entirely -- during the call both coexist -- but it
	// shrinks it from "forever" to "the milliseconds of one query". The window really closes
	// only once the module stops being a separate process and the Host loads it, which is the
	// target architecture.
	//
	// And it happens to be right for another reason too: N live modules on a device is real
	// RAM, and the module starts nothing on its own, so there is nothing to keep awake.
	public async Task<CallToolResult> Invoke(
		string tool, IReadOnlyDictionary<string, object?>? arguments, CancellationToken ct)
	{
		try
		{
			return await (await Start(ct)).CallToolAsync(tool, arguments, cancellationToken: ct);
		}
		finally { await Evict(); }
	}

	// Closes the module's process and lets go of the folder. It must not throw: it is called
	// from a finally, and covering the original error with a cleanup one would be worse than
	// the problem.
	private async Task Evict()
	{
		await gate.WaitAsync();
		try
		{
			if (client is null) return;
			try { await client.DisposeAsync(); } catch { /* it was already dying */ }
			client = null;
		}
		finally { gate.Release(); }
	}

	public async ValueTask DisposeAsync()
	{
		if (client is not null) await client.DisposeAsync();
		gate.Dispose();
	}
}
