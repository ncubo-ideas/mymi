using Gnothi.Desktop.Components;
using Microsoft.Extensions.DependencyInjection;
using Photino.Blazor;

namespace Gnothi.Desktop;

// THE DESKTOP WINDOW. What the person opens to SEE the modules they installed and whose they
// are, and to run a query without the assistant in the middle.
//
// Usage:  Gnothi.Desktop [modules-directory]
//
// The core is the SAME as in MCP mode and on the phone: catalog and dispatcher are the Host's,
// not a copy for the UI. This file is the ONLY one that knows which toolkit paints the window.
internal static class Program
{
	[STAThread]
	private static void Main(string[] args)
	{
		// THE SAME MODULES FOLDER THE MCP SERVER USES, resolved so the window never shows a
		// different set than the assistant sees:
		//   1. an explicit argument                       -for running a second instance apart
		//   2. Modulos/ NEXT TO THE EXECUTABLE, if present -the INSTALLED layout, the same folder
		//      HostInstaller registers and the server is launched with
		//   3. Modulos/ four levels up                     -the DEV layout, run from the repo
		string beside = Path.Combine(AppContext.BaseDirectory, "Modulos");
		string modulesRoot =
			args.Length > 0 ? args[0]
			: Directory.Exists(beside) ? beside
			: Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Modulos");
		modulesRoot = Path.GetFullPath(modulesRoot);

		var state = new HostState(modulesRoot);
		state.Reload();

		PhotinoBlazorAppBuilder builder = PhotinoBlazorAppBuilder.CreateDefault(args);
		builder.Services.AddSingleton(state);
		// The assistant's provider + BYOK key, read from the person's private area. One instance:
		// Ajustes writes it, Asistente reads it. The key never rides through here.
		builder.Services.AddSingleton<ModelConfig>();

		// NO autostart="false" in index.html. That attribute belongs to the Blazor WASM
		// pattern; in Blazor Hybrid it stops the root component from mounting -the page loads
		// (the CSS background shows) and #app stays empty. A symptom that looks nothing like
		// its cause.
		builder.RootComponents.Add<Root>("#app");

		PhotinoBlazorApp app = builder.Build();

		app.MainWindow
			.SetTitle("Gnothi")
			.SetUseOsDefaultLocation(true)
			.SetSize(1000, 740)
			.SetMinSize(720, 520)
			// Developer tools ONLY in Debug: in Release nobody gets an inspector opened over
			// their own data. Same for Photino's own log: at its default level it echoes every
			// render batch to stderr, which buries the one line that matters when it breaks.
#if DEBUG
			.SetDevToolsEnabled(true)
			.SetLogVerbosity(1);
#else
			.SetDevToolsEnabled(false)
			.SetLogVerbosity(0);
#endif

		// An unhandled exception inside the WebView would otherwise die silently behind a blank
		// page. Surfacing it on stderr is the least we owe whoever is debugging.
		AppDomain.CurrentDomain.UnhandledException += (_, e) =>
			Console.Error.WriteLine($"[gnothi.desktop] {e.ExceptionObject}");

		app.Run();
	}
}
