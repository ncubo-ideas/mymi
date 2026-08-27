using Gnothi.Desktop.Components;
using Microsoft.AspNetCore.Components.WebView.WindowsForms;
using Microsoft.Extensions.DependencyInjection;

namespace Gnothi.Desktop;

// THE DESKTOP WINDOW. What the person opens to SEE the modules they installed and whose they
// are, and to run a query without the assistant in the middle.
//
// Usage:  Gnothi.Desktop.exe [modules-directory]
//
// The core is the SAME as in MCP mode and on the phone: catalog and dispatcher are the Host's,
// not a copy for the UI.
internal static class Program
{
	[STAThread]
	private static void Main(string[] args)
	{
		ApplicationConfiguration.Initialize();

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

		var services = new ServiceCollection();
		services.AddWindowsFormsBlazorWebView();
		services.AddSingleton(state);

		// Developer tools ONLY in Debug: in Release nobody gets an inspector opened over their
		// own data.
#if DEBUG
		services.AddBlazorWebViewDeveloperTools();
#endif

		using var form = new Form
		{
			Text = "Gnothi",
			Width = 1000,
			Height = 740,
			MinimumSize = new Size(720, 520),
		};

		var view = new BlazorWebView
		{
			Dock = DockStyle.Fill,
			HostPage = "wwwroot/index.html",
			Services = services.BuildServiceProvider(),
		};

		// NO autostart="false" in index.html. That attribute belongs to the Blazor WASM
		// pattern; in Blazor Hybrid it stops the root component from mounting -the page loads
		// (the CSS background shows) and #app stays empty. A symptom that looks nothing like
		// its cause.
		view.RootComponents.Add<Root>("#app");

		form.Controls.Add(view);
		Application.Run(form);
	}
}
