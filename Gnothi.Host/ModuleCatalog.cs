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

				// A MODULE NAME MAY NOT CONTAIN '_', and this is not style: the public name a
				// tool is published under is '{module}_{tool}', so a module called 'mi_super'
				// with a tool 'x' and a module 'mi' with a tool 'super_x' produce THE SAME
				// name. The index that both gates share is a dictionary, so the second one in
				// would silently take the first one's place and a tool would just stop
				// existing -- with nothing said to anyone.
				//
				// Rejected at load and NOT resolved with a suffix: the name is what the
				// institution declared and what the assistant sees, so renaming it behind its
				// back would be the Host having an opinion about somebody else's contract.
				// Saying no here is loud, once, and at the moment it can still be fixed.
				if (m.Module.Contains('_'))
				{
					warnings.Add(
						$"{m.Module}: a module name cannot contain '_' -- it would collide with " +
						"the public names of its own tools. Skipped");
					continue;
				}

				// AND IT MAY NOT BE THE HOST'S OWN NAME. The Host publishes its tools under
				// 'gnothi': how many modules there are, how much each takes up, how often the
				// assistant asked them. Those are the answers the person uses to check up on
				// the modules, so a module able to publish under that name could answer FOR
				// the Host about itself. Rejected at load, which is the only moment it can
				// still be rejected loudly.
				if (string.Equals(m.Module, Dispatcher.HostModule, StringComparison.OrdinalIgnoreCase))
				{
					warnings.Add(
						$"{m.Module}: '{Dispatcher.HostModule}' is reserved for the Host's own tools -- " +
						"a module cannot publish under it. Skipped");
					continue;
				}

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
