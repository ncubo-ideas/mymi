using Gnothi.Host;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;

// THE MCP FRONT END: Door 1. It translates and delegates, nothing else.
//
// Usage:  Gnothi.Host <modules-directory>
//
// THE CONSOLE COMMANDS RUN FIRST, before stdout is touched. They are NOT the MCP server, and
// they manage their own output: --install writes only to stderr, and --query needs REAL stdout
// for its json result. If the redirect below ran first, --query's result would land on stderr.
//
// GNOTHI PRESENTS ITSELF TO THE ASSISTANT. Registers this executable in the assistant's config
// -reading, adding one key, backing up- and returns. --instalar accepted too, as the team typed.
if (args.Length > 0 && (args[0] is "--install" or "--instalar"))
	return HostInstaller.Install(args[1..]);

// QUERY A MODULE FROM THE CONSOLE, no assistant. The only way to check that a module answers
// the same through any shell, and for the person to read their data with no AI in the middle.
if (args.Length > 0 && (args[0] is "--query" or "--consultar"))
	return await QueryConsole.Run(args[1..]);

// PACK a cargo module into a .gmod (publisher-side helper), and INSTALL one. Install is also
// what the desktop window calls; the CLI exists so the whole loop is testable without a UI.
if (args.Length > 0 && args[0] is "--pack")
	return ModuleInstaller.Pack(args[1..]);
if (args.Length > 0 && (args[0] is "--install-module" or "--instalar-modulo"))
	return ModuleInstaller.InstallFromCli(args[1..]);

// WITH THE STDIO TRANSPORT, STDOUT IS THE JSON-RPC CHANNEL. Any Console.Write of our own -or of
// a library's- corrupts the protocol, and worse than breaking it: if it does not close the
// line, those characters get glued in front of the next message and the client desynchronizes
// without a clear error. stdout is redirected to stderr before the server path touches anything.
Console.SetOut(Console.Error);

string modulesRoot = args.Length > 0
	? args[0]
	: Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Modulos");
modulesRoot = Path.GetFullPath(modulesRoot);

// The catalog is loaded by reading MANIFESTS. No module is brought up here: twenty modules are
// twenty json files, not twenty journal replays.
var catalog = new ModuleCatalog(modulesRoot);
IReadOnlyList<string> warnings = catalog.Load();

Console.Error.WriteLine($"[gnothi] modules in {modulesRoot}: {catalog.Modules.Count}");
foreach (string w in warnings) Console.Error.WriteLine($"[gnothi] warning: {w}");

// THIS GATE ACCEPTS JSON AND NOTHING ELSE, and that is not a limitation we chose: MCP carries
// structured output, so what leaves here has to be parseable by the client, not a text
// projection. Declaring it explicitly instead of relying on the default is the point of the
// hint -- the next person to read this line learns that the format was decided, not forgotten.
var dispatcher = new Dispatcher(catalog, new Meter(), Accepts.JsonOnly);

foreach ((Module m, DeclaredTool t) in dispatcher.Catalog())
	Console.Error.WriteLine($"[gnothi] publishing: {Module.PublicName(m.Name, t.Name)}");

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton(dispatcher);

builder.Services.AddMcpServer(options =>
{
	options.ServerInfo = new() { Name = "gnothi", Version = "0.1.0" };

	// The instructions are FOR THE AI, and they arrive in the handshake. This is where it is
	// explained that it is not talking to some company's server.
	options.ServerInstructions = """
		You are the personal AI of the person using this device.

		Gnothi is the Host they installed. The modules are what each institution handed them
		-their lab, their supermarket- and each one keeps their record in a folder of its own,
		on this device. Gnothi asks no company anything: it reads what is already here.

		The tools prefixed with 'gnothi_' belong to the Host and speak about the modules, not
		about the data. The rest come prefixed by the module that contributes them.

		What Gnothi shows you is what the person authorized. If you expect a query that does not
		appear in the list, most likely they switched it off; do not insist and do not try to
		work around it. You cannot switch anything on: consent is theirs.

		If a total did not change and they expected it to, ask them to open the module and update
		it, then query again.
		""";

	// IT IS DECLARED that this server knows how to announce when its catalog changes. Without
	// this capability the client has no reason to ask for tools/list again, and the notification
	// drops on the floor without an error: the Host thinks it announced and the assistant keeps
	// the old list.
	options.Capabilities ??= new();
	options.Capabilities.Tools ??= new();
	options.Capabilities.Tools.ListChanged = true;
})
	.WithStdioServerTransport()
	// The Host's own tools USED TO BE REGISTERED HERE, with .WithTools<HostTools>(), because
	// their signature is known at compile time. That was convenient and it tied the fourth verb
	// to this gate: on the phone the class could not even be compiled -- it dragged in the MCP
	// SDK -- so the assistant there could not ask what Gnothi knows about itself.
	//
	// They are now declared below the seam and travel through the same index as everybody
	// else's, so ToolBridge picks them up along with the modules' and every gate inherits them.
	//
	// The modules' tools are answered BY HAND. The input schema comes from the manifest, which
	// is a piece of data: synthesizing a delegate would tie the tool to a signature the Host
	// cannot know, and that is why the previous version skipped the ones that had parameters.
	.WithListToolsHandler((request, ct) => ToolBridge.List(dispatcher, request, ct))
	.WithCallToolHandler((request, ct) => ToolBridge.Call(dispatcher, request, ct));

// THE CONSENT WATCHER. It goes in as a hosted service -and not as a loose thread- so that it
// starts and shuts down together with the server, and so that the McpServer can be asked of the
// injection only when an announcement is actually needed.
builder.Services.AddHostedService<ConsentWatcher>();

IHost host = builder.Build();

// The modules' child processes are closed when the catalog is closed.
await using (catalog)
{
	await host.RunAsync();
}

// EOF on stdin is the assistant saying "we are done": RunAsync returns and we exit 0. The
// explicit return is here because the --install branch above returns an int, so every path of
// the entry point must.
return 0;
