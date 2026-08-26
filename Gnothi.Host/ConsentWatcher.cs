using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Gnothi.Host;

// THE WATCHER: it makes moving the switch take effect NOW, without restarting anything.
//
// Without this, consent was read once, at start-up. The person would switch a query off in the
// middle of a conversation and the assistant kept seeing the old list until somebody closed the
// Host. A switch you have to restart for it to work is not a switch: it is a configuration
// file.
//
// The shape is the protocol's. The server does NOT send the new catalog to anyone: it sends
// 'notifications/tools/list_changed', which is a tap on the shoulder, and the client asks for
// tools/list again when it wants to. That is why the catalog is reloaded here and the one who
// answers is still ToolBridge, reading the same Dispatcher. A single source.
//
// What is watched is THE PERSON'S file -installation.json- and not the manifest. The manifest
// belongs to the institution and does not change while the Host runs; if it ever did, what the
// module OFFERS would change, and that is a reinstallation, not a change of mind.
internal sealed class ConsentWatcher : BackgroundService
{
	// A text editor -and any UI that writes through a temp file and a rename- fires several
	// events for a single save. The burst is coalesced into one reload.
	private static readonly TimeSpan Burst = TimeSpan.FromMilliseconds(300);

	private readonly Dispatcher dispatcher;
	private readonly IServiceProvider services;
	private readonly SemaphoreSlim signal = new(0, 1);

	public ConsentWatcher(Dispatcher dispatcher, IServiceProvider services)
	{
		this.dispatcher = dispatcher;
		this.services = services;
	}

	protected override async Task ExecuteAsync(CancellationToken ct)
	{
		string root = dispatcher.Modules.Root;
		if (!Directory.Exists(root)) return;

		using var peephole = new FileSystemWatcher(root, "installation.json")
		{
			IncludeSubdirectories = true,
			NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
			EnableRaisingEvents = true,
		};

		peephole.Changed += OnChanged;
		peephole.Created += OnChanged;
		peephole.Deleted += OnChanged;
		peephole.Renamed += OnChanged;

		Console.Error.WriteLine($"[gnothi] watching consent in {root}");

		while (!ct.IsCancellationRequested)
		{
			try
			{
				await signal.WaitAsync(ct);
				await Task.Delay(Burst, ct);   // the burst is coalesced
				await React(ct);
			}
			catch (OperationCanceledException) { break; }
			catch (Exception e)
			{
				// The watcher must not bring the Host down. If a reload fails, the catalog that
				// was already being served keeps being served.
				Console.Error.WriteLine($"[gnothi] the watcher failed: {e.GetType().Name}: {e.Message}");
			}
		}
	}

	private void OnChanged(object sender, FileSystemEventArgs e)
	{
		try { signal.Release(); } catch (SemaphoreFullException) { /* a reload is already pending */ }
	}

	private async Task React(CancellationToken ct)
	{
		IReadOnlyList<string> changes = dispatcher.Modules.ReloadConsent();
		if (changes.Count == 0) return;   // the file was touched but said the same thing

		foreach (string c in changes) Console.Error.WriteLine($"[gnothi] consent: {c}");

		// The server is resolved HERE and not in the constructor: at start-up it does not exist
		// yet, and asking for it too early orders the construction backwards.
		McpServer server = services.GetRequiredService<McpServer>();
		await server.SendNotificationAsync(NotificationMethods.ToolListChangedNotification, ct);

		int published = dispatcher.Catalog().Count();
		Console.Error.WriteLine($"[gnothi] tools/list_changed sent; the catalog now stands at {published} module tools");
	}
}
