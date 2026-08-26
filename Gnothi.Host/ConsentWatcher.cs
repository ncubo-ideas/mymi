using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Gnothi.Host;

// GATE 1'S HALF OF THE WATCH: telling the assistant that the catalog moved.
//
// The watching and the reloading are NOT here -- they are in ConsentWatch, in the core, where
// a host without MCP can use them too. What is left here is the only part that is the
// protocol's: the tap on the shoulder.
//
// The shape is the protocol's. The server does NOT send the new catalog to anyone: it sends
// 'notifications/tools/list_changed', and the client asks for tools/list again when it wants
// to. That is why the catalog is reloaded in the core and the one who answers is still
// ToolBridge, reading the same Dispatcher. A single source.
internal sealed class ConsentWatcher : BackgroundService
{
	private readonly Dispatcher dispatcher;
	private readonly IServiceProvider services;

	public ConsentWatcher(Dispatcher dispatcher, IServiceProvider services)
	{
		this.dispatcher = dispatcher;
		this.services = services;
	}

	protected override async Task ExecuteAsync(CancellationToken ct)
	{
		using var watch = new ConsentWatch(dispatcher) { Changed = Tap };
		await watch.Run(s => Console.Error.WriteLine($"[gnothi] {s}"), ct);
	}

	private async Task Tap(IReadOnlyList<string> changes, CancellationToken ct)
	{
		// The server is resolved HERE and not in the constructor: at start-up it does not exist
		// yet, and asking for it too early orders the construction backwards.
		McpServer server = services.GetRequiredService<McpServer>();
		await server.SendNotificationAsync(NotificationMethods.ToolListChangedNotification, ct);

		int published = dispatcher.Catalog().Count();
		Console.Error.WriteLine(
			$"[gnothi] tools/list_changed sent; the catalog now stands at {published} module tools");
	}
}
