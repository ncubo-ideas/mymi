using System.Text.Json;
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
internal sealed class StdioShell : IShell
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
	public async Task<IReadOnlyList<string>> ActualTools(CancellationToken ct)
	{
		try
		{
			return [.. (await (await Start(ct)).ListToolsAsync(cancellationToken: ct)).Select(t => t.Name)];
		}
		finally { await Evict(); }
	}

	// The module's FACTS are returned, with the MCP envelope taken off -- see Facts below.
	//
	// The Host does not parse nor reformat what is INSIDE: it lifts the payload out of the
	// protocol wrapper and hands it over as it came. That distinction is the whole point. If it
	// transcoded the content, it would have to understand the domain, and any loss in the
	// conversion would become a Host bug living inside the person's data. Unwrapping is not
	// transcoding: nobody's data changes, only whose envelope it travels in.
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
	public async Task<object> Invoke(
		string tool, IReadOnlyDictionary<string, object?>? arguments, CancellationToken ct)
	{
		try
		{
			return Facts(await (await Start(ct)).CallToolAsync(tool, arguments, cancellationToken: ct));
		}
		finally { await Evict(); }
	}

	// THE MCP ENVELOPE STOPS HERE, and that is the whole point of IShell: the seam declares
	// that content travels as 'object' with NO MCP type in it, and returning a CallToolResult
	// through it broke that promise in silence.
	//
	// It cost a MEASURED defect. Gate 2 serializes whatever arrives, so with a module over
	// stdio the model received the envelope with the facts escaped INSIDE a string: 525
	// characters where the in-process shell sent 126, for the very same four invoices. Gate 1
	// hid it because ToolBridge carried a branch per shape; Gate 2 had no such branch, and
	// nothing said it needed one.
	//
	// So both shells now return THE SAME THING: the module's facts. Dressing them back up as
	// MCP is Gate 1's job and nobody else's.
	private static object Facts(CallToolResult result)
	{
		// A module's error is THROWN, not returned. Dispatcher.Serve already turns an exception
		// into a Denied that carries the module's own message, so this needs no new path.
		if (result.IsError == true)
			throw new InvalidOperationException(Text(result));

		// structuredContent is the facts as data. The text block carries the same thing
		// escaped into a string, which is exactly why it costs four times as much.
		if (result.StructuredContent is JsonElement structured
			&& structured.ValueKind is not JsonValueKind.Undefined and not JsonValueKind.Null)
			return structured;

		// A module that answers only in text is legitimate: its text IS its facts.
		return Text(result);
	}

	private static string Text(CallToolResult result) =>
		string.Concat(result.Content.OfType<TextContentBlock>().Select(b => b.Text));

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
