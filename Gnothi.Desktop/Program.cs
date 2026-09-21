using System.Globalization;
using Gnothi.Desktop.Components;
using Gnothi.Host;
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
			args.Length > 0 && !args[0].StartsWith("--") ? args[0]
			: Directory.Exists(beside) ? beside
			: Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Modulos");
		modulesRoot = Path.GetFullPath(modulesRoot);

		// EL IDIOMA DE LA PERSONA, RESUELTO UNA VEZ Y AQUI. Sale del APARATO: no hay selector
		// adentro de Gnothi, y esa es la decision, no una etapa.
		//
		// Va antes que HostState.Reload() porque desde ese momento hay texto que se compone.
		// Y va en UN solo lugar por cara: dos resolutores son dos fuentes de verdad, que es
		// exactamente lo que el catalogo compartido existe para evitar.
		Textos.Idioma = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

		// THE HYBRID FORM ON THE DESKTOP. Before the catalog
		// loads -ChooseShell runs in Module's constructor- the window registers the two shells the
		// Host only has as stubs here, the same two the phones compile in place of them. Their
		// engine is the frame inside this window (FrameWebModule.cs), on the three systems; the
		// courier's credential goes to a file under the person's local application data, never
		// the journal.
		var secrets = new FileSecretStore(Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Gnothi", "credenciales.json"));

		// THE FACE: by default a frame inside this window (the phone's shape, on the three
		// systems); with `--cara ventana`, on Windows, the WebView2 window that ran first.
		bool caraVentana = Array.IndexOf(args, "--cara") is int ci && ci >= 0 && ci + 1 < args.Length && args[ci + 1] == "ventana";
		FrameWebModuleHost? frames = null;
		IFaces faces;
#if WINDOWS
		if (caraVentana) faces = new WebView2WebModuleHost(secrets, HostState.Log);
		else faces = frames = new FrameWebModuleHost(secrets, HostState.Log);
#else
		faces = frames = new FrameWebModuleHost(secrets, HostState.Log);
#endif
		Gnothi.Host.WebViewShell.Host = (Gnothi.Host.IWebModuleHost)faces;
		Gnothi.Host.Module.ShellFactory = (name, installation) => installation.Shell.Type.ToLowerInvariant() switch
		{
			"hybrid" => new Gnothi.Host.HybridShell(name, installation.Shell, installation.GrantedFolder),
			"webview" => new Gnothi.Host.WebViewShell(name, installation.Shell, installation.GrantedFolder),
			_ => null,
		};

		// TEST AIDS, all optional, so a run can be driven and read from a terminal without a hand
		// on the mouse:
		//   --journals <dir>   where granted folders go (default: Documents/Gnothi, like --install-module)
		//   --alta <enlace>    runs the enrollment on that link -sheet to stderr, then Instalar- at startup
		//   --abrir <modulo>   opens that module's face and, eight seconds later, reads its screen
		//   --auto <segundos>  closes the whole app after that many seconds
		//   --modulo <nombre>  NOT a test aid: what the desktop icon of a module passes, to open its
		//                      face straight away. The person went to the institution, not to Gnothi.
		string? abrir = null, alta = null, journals = null, modulo = null;
		int auto = 0;
		for (int i = 0; i < args.Length - 1; i++)
		{
			if (args[i] == "--abrir") abrir = args[i + 1];
			if (args[i] == "--modulo") modulo = args[i + 1];
			if (args[i] == "--alta") alta = args[i + 1];
			if (args[i] == "--journals") journals = Path.GetFullPath(args[i + 1]);
			if (args[i] == "--auto" && int.TryParse(args[i + 1], out int s)) auto = s;
		}

		// Antes de que nadie toque el modelo: el resolver tiene que estar puesto cuando el
		// primer DllImport de ModelFoundation se enlace.
		AppleModel.Wire();

		var state = new HostState(modulesRoot, journals) { Faces = faces, Frames = frames };
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
			.SetTitle("Mymi")
			// EL ICONO DE LA VENTANA, el mismo dibujo que el encabezado y que el telefono. Sin
			// esto la barra de titulo y la de tareas muestran el icono por omision del runtime,
			// que no es de nadie. El .ico lleva varios tamanos adentro y Windows elige.
			// Por ruta absoluta: Photino lo busca desde el directorio de trabajo, y ese es el del
			// proyecto con `dotnet run` pero cualquier otro cuando lo lanza un icono o un guion.
			// Sin esto la ventana muere al arrancar con "WindowIconFile: mymi.ico cannot be found".
			.SetIconFile(Path.Combine(AppContext.BaseDirectory, "mymi.ico"))
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

		if (modulo is not null)
		{
			_ = Task.Run(async () =>
			{
				// The page has to be up before a frame can be shown: wait for Root to wire the bridge.
				for (int i = 0; i < 100 && frames is { PageReady: false }; i++) await Task.Delay(100);
				Gnothi.Host.Module? m = state.Catalog.Modules.FirstOrDefault(x => x.Name == modulo);
				if (m is null) { HostState.Log($"--modulo: no hay módulo '{modulo}' instalado"); return; }
				try { await faces.OpenFace(m, CancellationToken.None); }
				catch (Exception ex) { HostState.Log($"--modulo: no se pudo abrir la cara de '{modulo}': {ex.GetType().Name}: {ex.Message}"); }
			});
		}

		if (abrir is not null || alta is not null || auto > 0)
		{
			_ = Task.Run(async () =>
			{
				try
				{
					if (alta is not null)
					{
						await Task.Delay(TimeSpan.FromSeconds(2));
						Gnothi.MobileShared.EnrollmentProposal p;
						if (File.Exists(alta))
						{
							// A package on disk: the "Elegir el archivo" path, without the picker.
							HostState.Log($"--alta: examinando el archivo {alta}");
							p = await state.Enrollment.ExamineFile(await File.ReadAllBytesAsync(alta), Path.GetFileName(alta),
								CancellationToken.None, progress => HostState.Log($"--alta: {progress.Text}"));
						}
						else
						{
							Gnothi.MobileShared.EnrollmentLink parsed = Gnothi.MobileShared.ModuleEnrollment.ParseLink(alta);
							HostState.Log($"--alta: examinando {parsed.Bundle}" + (parsed.Seal is null ? "" : $" (sello {parsed.Seal})"));
							p = await state.Enrollment.Examine(
								parsed.Bundle, Gnothi.MobileShared.EnrollmentSource.PastedLink, parsed.Seal, CancellationToken.None,
								progress => HostState.Log($"--alta: {progress.Text}"));
						}
						HostState.Log($"--alta: propuesta de '{p.Manifest.Module}' ({p.Manifest.Institution}): forma={p.Inspection.Form}"
							+ $" archivos={p.Inspection.Files} bytes={p.Inspection.Bytes} motor={p.Inspection.EngineFiles}"
							+ $" excesos=[{string.Join(",", p.Inspection.Excesses)}] origen={p.Provenance.Origin} firma={p.Provenance.Signer}"
							+ $" ({p.Provenance.SignerId}) seguro={p.Provenance.Secure} update={p.Update.Kind} yaEstaba={p.AlreadyThere}"
							+ $" journal={p.JournalGoesTo} publicador={p.Publisher}");
						// LA PRUEBA DEL RELOAD. Si el módulo ya estaba, se le pregunta algo ANTES del alta para
						// que su superficie tenga el actor abierto, y después de Reload() se mira si ese Module
						// viejo sigue corriendo. Con un ModuleCatalog nuevo por recarga seguía (dos actores sobre
						// el mismo journal); con el mismo catálogo, Publish lo desecha.
						Gnothi.Host.Module? viejo = state.Catalog.Find(p.Manifest.Module);
						if (viejo is not null)
						{
							Gnothi.Host.DeclaredTool? t = viejo.Manifest.Tools.FirstOrDefault(x => x.ReadOnly && x.Input.Count == 0);
							if (t is not null)
							{
								await state.Dispatcher.Serve(new Gnothi.Host.Request(viejo.Name, t.Name, null), CancellationToken.None);
								HostState.Log($"--alta: '{viejo.Name}' ya estaba; {t.Name} contestó antes del alta (superficie viva: {viejo.IsRunning})");
							}
						}
						string module = state.Enrollment.Accept(p);
						HostState.Log($"--alta: '{module}' instalado; releyendo el catálogo");
						state.Reload();
						if (viejo is not null)
						{
							await Task.Delay(TimeSpan.FromSeconds(3));
							HostState.Log($"--alta: el Module viejo de '{viejo.Name}' "
								+ (viejo.IsRunning ? "SIGUE CORRIENDO: dos actores sobre el journal" : "quedó desechado: un solo actor")
								+ $"; el catálogo trae {state.Catalog.Modules.Count} módulos");
						}
						foreach (string line in await state.VerifyNewModules(CancellationToken.None)) HostState.Log($"--alta: {line}");
					}

					if (abrir is not null)
					{
						await Task.Delay(TimeSpan.FromSeconds(2));
						for (int i = 0; i < 100 && frames is { PageReady: false }; i++) await Task.Delay(100);
						Gnothi.Host.Module? m = state.Catalog.Modules.FirstOrDefault(x => x.Name == abrir);
						if (m is null) { HostState.Log($"--abrir: no hay módulo '{abrir}'"); }
						else
						{
							HostState.Log($"--abrir: abriendo la cara de '{abrir}'");
							IFace face = await faces.OpenFace(m, CancellationToken.None);
							HostState.Log($"--abrir: '{abrir}' dijo listo(); la cara está viva");
							await Task.Delay(TimeSpan.FromSeconds(8));
							string text = await face.Eval("document.body.innerText");
							string plain = Plain(text);
							HostState.Log("---- pantalla de la cara ----");
							foreach (string line in plain.Split('\n').Where(l => l.Trim().Length > 0).Take(24))
								HostState.Log("  " + line.Trim());
							HostState.Log("-----------------------------");
							string png = Path.Combine(modulesRoot, "..", "salida", $"cara-desktop-{abrir}.png");
							Directory.CreateDirectory(Path.GetDirectoryName(png)!);
							await face.Screenshot(png);
							HostState.Log($"captura: {Path.GetFullPath(png)}");
							HostState.Log($"violaciones de CSP vistas por Gnothi: {face.Violations}");

							// Close and reopen, which is what a person does: the second open has to build
							// the face again, not hand back the one whose window is gone.
							faces.Close(abrir);
							await Task.Delay(TimeSpan.FromSeconds(2));
							IFace again = await faces.OpenFace(m, CancellationToken.None);
							HostState.Log($"--abrir: '{abrir}' reabierta tras cerrarla; la cara está viva de nuevo");
							await Task.Delay(TimeSpan.FromSeconds(4));
							string second = Plain(await again.Eval("document.body.innerText"));
							HostState.Log($"--abrir: la segunda cara dice: {string.Join(" · ", second.Split('\n').Where(l => l.Trim().Length > 0).Take(4).Select(l => l.Trim()))}");

							// And the icon on the desktop, the phone's pinned shortcut.
							HostState.Log($"--abrir: icono creado: {faces.PinToDesktop(m, modulesRoot, state.JournalsRoot)}");
							var after = await state.Dispatcher.Serve(new Gnothi.Host.Request(abrir, "resumen", null), CancellationToken.None);
							HostState.Log($"resumen por el Dispatcher tras abrir la cara: {System.Text.Json.JsonSerializer.Serialize(after.Content)}");
							HostState.Log("meter: " + string.Join(", ", state.Dispatcher.Meter.Queries().Select(kv => $"{kv.Key}={kv.Value}")));
						}
					}
				}
				catch (Exception ex) { HostState.Log($"--abrir FALLÓ: {ex.GetType().Name}: {ex.Message}"); }

				if (auto > 0)
				{
					await Task.Delay(TimeSpan.FromSeconds(auto));
					HostState.Log("--auto: cerrando");
					app.MainWindow.Invoke(() => app.MainWindow.Close());
				}
			});
		}

		app.Run();
	}
	// What Eval hands back: WebView2 answers with a JSON string, the frame with the text itself.
	private static string Plain(string text) =>
		text.StartsWith('"') ? (System.Text.Json.JsonSerializer.Deserialize<string>(text) ?? "") : text;

}
