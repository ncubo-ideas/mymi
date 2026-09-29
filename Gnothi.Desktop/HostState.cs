using System.Text.Json;
using Gnothi.Host;

namespace Gnothi.Desktop;

// THE ONE PLACE THE WINDOW HOLDS THE HOST. Catalog and Dispatcher are the Host's own types;
// the components only read them. Reload() re-reads installation.json from disk -that is how a
// switch the person flipped in her file shows up here.
internal sealed class HostState
{
	private readonly string root;
	private Gnothi.MobileShared.ModuleEnrollment? enrollment;

	public HostState(string root, string? journalsRoot = null)
	{
		this.root = root;
		// THE SAME BASE ModuleInstaller GRANTS ON THE DESKTOP -Documents/Gnothi/<modulo>- so a
		// module that arrives through the enrollment and one that arrived through --install-module
		// keep their journals side by side.
		JournalsRoot = journalsRoot ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Gnothi");
	}

	public string Root => root;

	public string JournalsRoot { get; }

	public ModuleCatalog Catalog { get; private set; } = null!;

	public Dispatcher Dispatcher { get; private set; } = null!;

	public IReadOnlyList<string> Warnings { get; private set; } = [];

	// THE FACES OF HYBRID MODULES, created on demand. Program registers it as WebViewShell.Host
	// before the catalog loads, so a module whose installation.json says "hybrid" gets the real
	// shells.
	public IFaces Faces { get; init; } = new NoFaces();

	// The default face host: the module's screen as a frame inside this window. Root wires its
	// page side; null when the faces run in their own WebView2 window (--cara ventana).
	public FrameWebModuleHost? Frames { get; init; }

	// THE ENROLLMENT, the same one the phone runs: download or read the package, inspect it,
	// check its signature and its origin, propose, and only then write installation.json. It
	// replaces ModuleInstaller as the window's way in because ModuleInstaller takes cargo only,
	// and a hybrid is not cargo.
	public Gnothi.MobileShared.ModuleEnrollment Enrollment => enrollment ??=
		new Gnothi.MobileShared.ModuleEnrollment(root, JournalsRoot, Log)
		{
			BeforeReplacing = m =>
			{
				Faces.Close(m);
				Log($"alta: '{m}' estaba abierto; se cerró antes de reemplazarlo");
			},
		};

	// ONE CATALOG, RELOADED -never a second one. Load() publishes the new list and DISPOSES the
	// Module objects it replaced (ModuleCatalog.Publish): for a hybrid that is the institution's
	// AssemblyLoadContext and, with keepLoaded, its actor over the journal folder. A fresh
	// ModuleCatalog per Reload defeated that: Publish compares against ITS OWN previous list,
	// which for a new catalog is empty, so the old catalog stayed alive with its shells and every
	// alta or update left two actors on the same journal -the "Temp file not found" case the
	// phone already closed. Found 2026-09-14 reviewing the desktop against Jonathan's diagram.
	// The Dispatcher reads the catalog live (no copy), so it is built once too; the Meter keeps
	// counting across reloads, which is what a meter should do.
	public void Reload()
	{
		if (Catalog is null)
		{
			Catalog = new ModuleCatalog(root);
			Dispatcher = new Dispatcher(Catalog, new Meter());
		}
		Warnings = Catalog.Load();
	}

	// THE CHECK AFTER AN INSTALL, the same the phone runs (Gnothi.MobileShared/Shell.VerifyNewModules):
	// declared against actual, and one real answer. Once per module, marked in its folder, so
	// reinstalling checks again and restarting does not. Returns what it found, to show.
	public async Task<IReadOnlyList<string>> VerifyNewModules(CancellationToken ct)
	{
		List<string> lines = [];
		foreach (Module m in Catalog.Modules)
		{
			string mark = Path.Combine(m.Directory, "comprobado.json");
			if (File.Exists(mark)) continue;

			try
			{
				Response said = await Dispatcher.Serve(
					new Request(Dispatcher.HostModule, "verify_module", new Dictionary<string, object?> { ["module"] = m.Name }), ct);
				string verdict = JsonSerializer.Serialize(said.Content);
				// What is shown comes from the catalog; the log line stays a log line.
				lines.Add(Texts.T("alta.escritorio.comprobacion", ("quien", m.Manifest.Identity), ("veredicto", verdict)));
				Log($"comprobación de {m.Name}: {verdict}");

				DeclaredTool? first = m.Manifest.Tools.FirstOrDefault(t => t.ReadOnly && t.Input.Count == 0);
				if (first is not null)
				{
					Response answer = await Dispatcher.Serve(new Request(m.Name, first.Name, null), ct);
					string text = answer.Content as string ?? JsonSerializer.Serialize(answer.Content);
					lines.Add(Texts.T("alta.escritorio.contesto",
						("consulta", first.Title.Length > 0 ? first.Title : first.Name), ("texto", text[..Math.Min(200, text.Length)])));
					Log($"  {first.Name} contestó {text.Length} bytes");
				}

				File.WriteAllText(mark, JsonSerializer.Serialize(new { cuando = DateTime.UtcNow.ToString("O") }));
			}
			catch (Exception ex)
			{
				lines.Add(Texts.T("alta.escritorio.comprobacion-fallo", ("quien", m.Manifest.Identity), ("detalle", $"{ex.GetType().Name}: {ex.Message}")));
				Log($"comprobación de {m.Name} FALLÓ: {ex.GetType().Name}: {ex.Message}");
			}
		}
		return lines;
	}

	public static void Log(string s) => Console.Error.WriteLine($"[gnothi.desktop] {s}");

	public static string Icon(Manifest m) => (m.About?.Category ?? "") switch
	{
		"laboratorio" or "salud" => "\U0001FA7A",   // stethoscope
		"supermercado" => "\U0001F6D2",             // trolley
		"comercio" => "\U0001F9FE",                 // receipt
		"banca" or "seguros" => "\U0001F3E6",       // bank
		"combustible" => "⛽",                  // pump
		"taller" => "\U0001F527",                   // wrench
		"telecom" => "\U0001F4F1",                  // phone
		"educacion" => "\U0001F393",                // cap
		"gobierno" => "\U0001F3DB",                 // classical building
		_ => "\U0001F4E6",                          // box
	};
}
