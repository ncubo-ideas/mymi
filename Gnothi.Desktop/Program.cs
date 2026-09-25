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

		// THE PERSON LANGUAGE, RESOLVED ONCE AND HERE. It comes from the DEVICE: there is no
		// picker inside Gnothi, and that is the decision, not a stage.
		//
		// It goes before HostState.Reload() because from that moment there is text being composed.
		// And it goes in ONE place per face: two resolvers are two sources of truth, which is
		// exactly what the shared catalogue exists to avoid.
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

		// EL CONTADOR DE LA HIBRIDA, CABLEADO — lo hace `MainActivity` desde el 2026-09-18 y el
		// escritorio no lo hacia. La cascara cuenta y cronometra cada vuelta al borde, pero no sabe
		// escribir en el log de nadie: se lo dan al arrancar. Sin esta linea, la forma que corre EN
		// PROCESO —la que es el punto de comparacion de las otras dos— era la unica de las tres que
		// no se podia medir en esta maquina.
		Gnothi.Host.HybridShell.Log = HostState.Log;

		// IT CHAINS, LIKE THE TWO PHONES DO, even though on the desktop this is the only factory
		// and always will be while there is one window. Uniform on purpose: `ShellFactory` is a
		// static field that took two owners without saying so, and the shape that hid it was the
		// flat assignment. There is no second owner here to protect -- what is being protected is
		// that nobody reads this file and copies the shape back into a place that does have one.
		// See Module.ChainShellFactory.
		Gnothi.Host.Module.ChainShellFactory((name, installation) => installation.Shell.Type.ToLowerInvariant() switch
		{
			"hybrid" => new Gnothi.Host.HybridShell(name, installation.Shell, installation.GrantedFolder),
			"webview" => new Gnothi.Host.WebViewShell(name, installation.Shell, installation.GrantedFolder),
			_ => null,
		});

		// AND THE BOX, AFTER the one above and not before, which is the whole reason the seam
		// chains: `ChainShellFactory` asks the LAST one registered first, so the box gets first
		// refusal on the hybrids and everything it does not take falls back to the factory above,
		// which goes on building them in process exactly as before.
		//
		// It is attempted always and gated by whether it can be done -- native library, substrate,
		// and the canary. See TheDesktopBox: if any of those is missing, nothing is registered and
		// one line in the log says why.
		TheDesktopBox.Prepare(HostState.Log);

		// Y LA SALA, que vive como un iframe oculto de esta página servido por loopback -y no por un
		// esquema propio, porque el de Photino NO recibe los pedidos de un frame: medido el
		// 2026-09-14, cero llamadas-. Ver PhotinoRoom.
		//
		// Registra la cáscara siempre que el sustrato esté; sin él, una línea en el log y los
		// módulos corren como siempre. No hay bandera, por el mismo criterio que la caja.
		// Se lee aca y no con las demas ayudas de prueba porque la fabrica tiene que quedar
		// registrada ANTES de que el catalogo abra un modulo, y eso pasa antes de ese bloque.
		string? salaSolo = null;
		for (int i = 0; i < args.Length - 1; i++)
			if (args[i] == "--sala-solo") salaSolo = args[i + 1];

		Func<CancellationToken, Task>? sala = TheDesktopRoom.Prepare(HostState.Log, caraVentana, salaSolo);

		// TEST AIDS, all optional, so a run can be driven and read from a terminal without a hand
		// on the mouse:
		//   --journals <dir>   where granted folders go (default: Documents/Gnothi, like --install-module)
		//   --alta <enlace>    runs the enrollment on that link -sheet to stderr, then Instalar- at startup
		//   --abrir <modulo>   opens that module's face and, eight seconds later, reads its screen
		//   --auto <segundos>  closes the whole app after that many seconds
		//   --sala-solo <a,b>  those hybrids run IN THE ROOM instead of in the box -- a decision of
//                      code, per run, that never touches the person file. Same shape as the
//                      box `--es caja-solo`. Ver TheDesktopRoom.
//   --sala             warms the room up WITH NO MODULE, which is how the question that
		//                      decides this form gets answered on its own: whether the substrate
		//                      comes up at all under this engine. Measured together with an
		//                      enrolment, a failed install reads the same as an origin that does not
		//                      serve. Same shape as iOS's empty switch file.
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

		// EL CALENTADO DE LA SALA, SIN NINGUN MODULO. Va fuera del bucle porque no lleva valor, y
		// `args.Length - 1` lo dejaría afuera si fuera el último argumento.
		//
		// No tumba nada si falla: lo que se busca es que el log diga si el sustrato levanta bajo
		// este motor, que es la pregunta que decide la forma. Ver PhotinoRoom.
		//
		// Y CALENTAR NO ES CARGAR UN MODULO, que es justo lo que confundió el 2026-09-25: con esto
		// solo se sabe que el sustrato levanta. La sala de WebView2 pasaba esta prueba y **no cargaba
		// un módulo de verdad**, y se dio por verificada igual. Ver `--sala-solo`.
		if (sala is not null && Array.Exists(args, a => a == "--sala"))
			_ = Task.Run(async () =>
			{
				// LA LINEA DICE LO QUE **NO** PRUEBA, y eso es el arreglo de un defecto real: esta
				// misma línea se leyó como «la sala anda» y con ella se dio por verificada una sala
				// que no cargaba un módulo ni una vez. Ver TheDesktopRoom.
				try
				{
					await sala(CancellationToken.None);
					HostState.Log("sala: arranco sin modulos -- esto NO prueba que un modulo cargue; "
						+ "para eso  --sala-solo <modulo> --abrir <modulo>");
				}
				catch (Exception e) { HostState.Log($"sala: NO arranco -- {e.GetType().Name}: {e.Message}"); }
			});

		// Before anybody touches the model: the resolver has to be in place when the first
		// DllImport of ModelFoundation is bound.
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
			// THE WINDOW ICON, the same drawing as the header and as the phone. Without this the
			// title bar and the task bar show the runtime default icon, which belongs to nobody.
			// The .ico carries several sizes inside and Windows chooses.
			// By absolute path: Photino looks for it from the working directory, which is the
			// project one with `dotnet run` but anything else when an icon or a script launches it.
			// Without this the window dies on start-up with "WindowIconFile: mymi.ico cannot be found".
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
				if (!await WaitForThePage(frames, "--modulo")) return;
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
						// THE RELOAD TEST. If the module was already there, it is asked something BEFORE the
						// enrolment so its surface has the actor open, and after Reload() it is checked whether
						// that old Module is still running. With a new ModuleCatalog per reload it was (two
						// actors over one journal); with the same catalogue, Publish disposes of it.
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

					// EL `&&` Y NO UN `return` ADENTRO, que es lo que había acá por un rato: este lambda
					// tiene el `--auto` DESPUÉS del bloque, así que salir temprano dejaba la ventana
					// abierta para siempre y la corrida colgada. Una guarda que se lleva puesto el
					// cierre automático es peor que la espera que vino a arreglar.
					if (abrir is not null && await WaitForThePage(frames, "--abrir", TimeSpan.FromSeconds(2)))
					{
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
	// ESPERAR A QUE LA PAGINA ESTE, Y DECIRLO SI NO LLEGA. Las dos ayudas de prueba que abren una
	// cara —`--abrir` y `--modulo`— necesitan que Root haya cableado el puente de los marcos; antes
	// de eso `OpenFace` tira «la página de Gnothi todavía no está lista».
	//
	// ESTABA COMO UN BUCLE DE 10 s COPIADO EN DOS LADOS, Y SE COBRO EL 2026-09-25: al repetir una
	// medición, dos de las tres corridas fallaron ahí. **No porque esperara poco, sino porque al
	// agotarse SEGUIA IGUAL**, sin decir nada, y lo que llegaba al log era la excepción de más
	// abajo — un mensaje verdadero sobre la página que no dice que el problema fue la espera. Media
	// hora de corridas leyendo el síntoma equivocado.
	//
	// Dos cosas cambian: la paciencia sube a 60 s, y **un plazo agotado se dice**. Lo segundo vale
	// más que lo primero: 60 s tampoco alcanzarían en una máquina cargada, y lo que hace falta
	// entonces es saber QUE fue eso.
	private static async Task<bool> WaitForThePage(FrameWebModuleHost? frames, string who, TimeSpan settle = default)
	{
		if (settle > TimeSpan.Zero) await Task.Delay(settle);

		if (frames is null) return true;   // con `--cara ventana` pinta WebView2 y no hay puente que esperar

		for (int i = 0; i < 600 && !frames.PageReady; i++) await Task.Delay(100);

		if (frames.PageReady) return true;

		HostState.Log($"{who}: la página de Gnothi no estuvo lista en 60 s; no se abre nada. "
			+ "Es la ventana que no levantó, no el módulo.");
		return false;
	}

	// What Eval hands back: WebView2 answers with a JSON string, the frame with the text itself.
	private static string Plain(string text) =>
		text.StartsWith('"') ? (System.Text.Json.JsonSerializer.Deserialize<string>(text) ?? "") : text;

}
