namespace Gnothi.Host;

// DISCOVERY: one directory per module, each with its manifest and its installation.
//
//   Modulos/
//     facturas/
//       manifest.json        <- from the institution
//       installation.json    <- from the Host
//
// A module with an unreadable manifest is SKIPPED with a warning on stderr; it does not bring
// the Host down. A Host that fails to start because one institution published a crooked json
// is a Host that hands a third party the availability of every other module.
public sealed class ModuleCatalog : IAsyncDisposable
{
	private readonly List<Module> modules = [];

	public IReadOnlyList<Module> Modules => modules;
	public string Root { get; }

	public ModuleCatalog(string root) => Root = root;

	public IReadOnlyList<string> Load()
	{
		modules.Clear();
		List<string> warnings = [];

		if (!Directory.Exists(Root))
		{
			warnings.Add($"the modules directory does not exist: {Root}");
			return warnings;
		}

		foreach (string dir in Directory.EnumerateDirectories(Root).OrderBy(d => d))
		{
			string manifestPath = Path.Combine(dir, "manifest.json");
			string installationPath = Path.Combine(dir, "installation.json");

			if (!File.Exists(manifestPath)) { warnings.Add($"{Path.GetFileName(dir)}: no manifest.json, skipped"); continue; }
			if (!File.Exists(installationPath)) { warnings.Add($"{Path.GetFileName(dir)}: no installation.json, skipped"); continue; }

			try
			{
				Manifest m = Manifest.Read(manifestPath);
				Installation i = Installation.Read(installationPath);
				modules.Add(new Module(m, i, dir));

				// Excess capabilities are the cheap alarm signal: an invoices module that asks
				// for a microphone does not need analysis, it needs a warning.
				string[] excesses = [.. m.Capabilities?.Excesses() ?? []];
				if (excesses.Length > 0)
					warnings.Add($"{m.Module}: declares excess capabilities ({string.Join(", ", excesses)})");
			}
			catch (Exception e)
			{
				warnings.Add($"{Path.GetFileName(dir)}: {e.GetType().Name}: {e.Message}");
			}
		}

		return warnings;
	}

	// HOT RELOAD OF CONSENT.
	//
	// It re-reads ONLY the installation.json files -the person's file- and returns which
	// modules changed. It does not re-read manifests nor rebuild modules: recreating a Module
	// would recreate its shell, and a new shell rips the child process out from under a call
	// that could be in flight.
	//
	// If the file cannot be read, the module ends up DISABLED. It is a consent file: when what
	// the person authorized cannot be known, the answer is not to assume they authorized. It
	// costs a blink if someone catches it half written, and that is the right price.
	public IReadOnlyList<string> ReloadConsent()
	{
		List<string> changes = [];

		foreach (Module m in modules)
		{
			string path = Path.Combine(m.Directory, "installation.json");
			bool enabled;
			IReadOnlyList<string> disabled;

			try
			{
				Installation i = Installation.Read(path);
				enabled = i.Enabled;
				disabled = i.DisabledTools;

				// What this method does NOT reload, said out loud instead of in silence.
				bool otherFolder = i.GrantedFolder != m.Installation.GrantedFolder;
				bool otherShell = i.Shell.Command != m.Installation.Shell.Command;
				if (otherFolder || otherShell)
					changes.Add($"{m.Name}: the shell or the folder changed, that calls for restarting the Host");
			}
			catch (Exception e)
			{
				enabled = false;
				disabled = [];
				changes.Add($"{m.Name}: could not read the consent ({e.GetType().Name}), stays disabled");
			}

			if (m.Reconsent(enabled, disabled))
				changes.Add($"{m.Name}: enabled={enabled}, disabled=[{string.Join(", ", disabled)}]");
		}

		return changes;
	}

	public Module? Find(string name)
		=> modules.FirstOrDefault(m => string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase));

	public async ValueTask DisposeAsync()
	{
		foreach (Module m in modules) await m.DisposeAsync();
	}
}
