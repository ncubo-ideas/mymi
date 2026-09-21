namespace Gnothi.Host;

// WATCHING THE PERSON'S SWITCH, WITH NO PROTOCOL ATTACHED.
//
// Without this, consent is read once at start-up. The person switches a query off in the
// middle of a conversation and the assistant keeps seeing the old list until somebody closes
// the Host. A switch you have to restart for it to work is not a switch: it is a
// configuration file.
//
// WHY IT IS ITS OWN CLASS. It used to live fused with the MCP notification, in one
// BackgroundService. That fusion cost something real and measurable: the Android app does not
// compile Gate 1 -- it does not speak MCP -- so it left the whole file out and the phone ended
// up with no hot reload at all. Sonia could switch a query off and the app kept offering it
// until she killed it.
//
// Watching and reloading is NOT the protocol's business. Telling somebody about it is. So the
// watch is here, in the core, and each gate decides what to do when it fires: Gate 1 taps the
// assistant on the shoulder with 'tools/list_changed'; the phone redraws its cards.
//
// What is watched is THE PERSON'S file -- installation.json -- and not the manifest. The
// manifest belongs to the institution and does not change while the Host runs; if it ever did,
// what the module OFFERS would change, and that is a reinstallation, not a change of mind.
public sealed class ConsentWatch : IDisposable
{
	// A text editor -- and any UI that writes through a temp file and a rename -- fires several
	// events for a single save. The burst is coalesced into one reload.
	private static readonly TimeSpan Burst = TimeSpan.FromMilliseconds(300);

	private readonly Dispatcher dispatcher;
	private readonly SemaphoreSlim signal = new(0, 1);

	private FileSystemWatcher? peephole;

	public ConsentWatch(Dispatcher dispatcher) => this.dispatcher = dispatcher;

	// WHAT CHANGED, handed to whoever is listening. It is an awaitable hook and not an event on
	// purpose: reacting means doing I/O -- sending a notification, redrawing a screen -- and an
	// event handler that returns void swallows the failure of exactly that. One listener, which
	// is what there is: one gate per process.
	public Func<IReadOnlyList<string>, CancellationToken, Task>? Changed { get; set; }

	// Runs until cancelled. It never throws: a watch that brings the Host down is worse than a
	// switch that lags.
	public async Task Run(Action<string>? trace, CancellationToken ct)
	{
		string root = dispatcher.Modules.Root;
		if (!Directory.Exists(root)) return;

		// HAY PLATAFORMAS QUE NO MIRAN ARCHIVOS, y en ellas esto NO ES UNA FALLA: es que no hay
		// nada que mirar. iOS no implementa System.IO.FileSystemWatcher -- lanza
		// PlatformNotSupportedException -- y hasta el 2026-09-15 eso subia hasta el arranque del
		// shell, que lo registraba como «shell fallo». Se veia como un Host roto y no lo era.
		//
		// LO QUE SE PIERDE AHI, Y ES NADA: este watch existe para UN solo caso -- que installation
		// .json lo edite algo de AFUERA de la app. El switch que la persona mueve DENTRO ya no
		// pasa por aca: Shell.SetConsent escribe el archivo, llama a ReloadConsent() en el acto y
		// rehace la foto, justamente porque esperar la rafaga de 300 ms seria un switch que tarda.
		//
		// Y en iOS el caso de afuera no existe: los modulos viven en Library/Application Support,
		// y UIFileSharingEnabled solo expone Documents/. No hay editor, no hay adb, no hay como
		// alcanzar ese archivo. En el escritorio si importa -- HostTools documenta editarlo a mano
		// -- y en Android tambien, donde ademas inotify funciona.
		//
		// SE PREGUNTA CAZANDO LA EXCEPCION Y NO CON OperatingSystem.IsIOS(). Que la plataforma
		// soporte o no mirar archivos lo contesta el runtime; una lista de plataformas escrita a
		// mano se queda vieja sin que nadie se entere, y falla en la direccion peor -- silencio
		// donde si habia algo que vigilar.
		try
		{
			peephole = new FileSystemWatcher(root, "installation.json")
			{
				IncludeSubdirectories = true,
				NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
				EnableRaisingEvents = true,
			};

			peephole.Changed += Wake;
			peephole.Created += Wake;
			peephole.Deleted += Wake;
			peephole.Renamed += Wake;
		}
		catch (PlatformNotSupportedException)
		{
			trace?.Invoke(
				"consent: this platform cannot watch files, so an edit from outside the app will "
				+ "not be noticed. The person's own switch does not depend on this: it reloads as "
				+ "it is moved.");
			return;
		}

		trace?.Invoke($"watching consent in {root}");

		while (!ct.IsCancellationRequested)
		{
			try
			{
				await signal.WaitAsync(ct);
				await Task.Delay(Burst, ct);   // the burst is coalesced

				IReadOnlyList<string> changes = dispatcher.Modules.ReloadConsent();
				if (changes.Count == 0) continue;   // the file was touched but said the same thing

				foreach (string c in changes) trace?.Invoke($"consent: {c}");

				if (Changed is not null) await Changed(changes, ct);
			}
			catch (OperationCanceledException) { break; }
			catch (Exception e)
			{
				// If a reload fails, the catalog that was already being served keeps being served.
				trace?.Invoke($"the watch failed: {e.GetType().Name}: {e.Message}");
			}
		}
	}

	private void Wake(object sender, FileSystemEventArgs e)
	{
		try { signal.Release(); } catch (SemaphoreFullException) { /* a reload is already pending */ }
	}

	public void Dispose()
	{
		peephole?.Dispose();
		peephole = null;
		signal.Dispose();
	}
}
