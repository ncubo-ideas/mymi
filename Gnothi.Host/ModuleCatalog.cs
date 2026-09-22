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
	// THE LIST IS REPLACED, NOT EMPTIED. And this is not a matter of style: it is what makes walking
	// it safe while the catalogue reloads.
	//
	// Load() used to Clear() and refill THIS SAME list, so anyone walking it -- the screen assembling
	// its state, VerifyNewModules with its awaits inside -- got an `InvalidOperationException:
	// Collection was modified`. Measured on the emulator on 2026-09-08: accepting an enrolment
	// reloads the catalogue, and the page was asking for its state at that very instant.
	//
	// With the replacement, whoever took the old reference finishes walking it whole and without an
	// exception -- an immutable list in practice -- and whoever arrives later sees the new one. It
	// costs one assignment and closes the whole class of bugs, instead of one walk at a time.
	private List<Module> modules = [];

	public IReadOnlyList<Module> Modules => modules;
	public string Root { get; }

	// The folder an update parks the previous version in: `<module>.anterior`, next to the live
	// one. Named here because the catalog must skip it and the enrollment must write it.
	public const string PreviousSuffix = ".anterior";

	public ModuleCatalog(string root) => Root = root;

	public IReadOnlyList<string> Load()
	{
		// A NEW LIST IS FILLED and published in one go at the end. See the comment on `modules`:
		// while this runs, whoever is walking the previous one never finds out.
		List<Module> cargados = [];
		List<string> warnings = [];

		if (!Directory.Exists(Root))
		{
			// THE EMPTY LIST GETS PUBLISHED ALL THE SAME. This path used to run after the Clear(),
			// so it left the catalogue empty; with the replacement it has to be said explicitly, or
			// a folder that disappears would leave the previous list standing -- modules that are no
			// longer there, answering.
			Publish(cargados);
			warnings.Add($"the modules directory does not exist: {Root}");
			return warnings;
		}

		foreach (string dir in Directory.EnumerateDirectories(Root).OrderBy(d => d))
		{
			// THE PREVIOUS VERSION AN UPDATE KEPT (NotasGnothiHost/44 §3.5). It is a complete module
			// folder -manifest, installation, web- parked next to the live one until the next update,
			// so that a new version that does not answer can be put back. It is NOT a module: loading
			// it would publish the same tools twice under the same name. Skipped without a warning,
			// because it is not a mistake; it is the safety net doing its job.
			if (Path.GetFileName(dir).EndsWith(PreviousSuffix, StringComparison.Ordinal)) continue;

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
				cargados.Add(new Module(m, i, dir));

				// Excess capabilities are the cheap alarm signal: an invoices module that asks
				// for a microphone does not need analysis, it needs a warning.
				string[] excesses = [.. m.Capabilities?.Excesses() ?? []];
				if (excesses.Length > 0)
					warnings.Add($"{m.Module}: declares excess capabilities ({string.Join(", ", excesses)})");

				// And the same for the limits on what the institution declares about itself -- the
				// short name, the logo, the colour, the texts. They do NOT bring the module down:
				// leaving the person without their laboratory because somebody overran a line would
				// be worse than the line. They are said, and said here, which is where these things
				// already get said.
				foreach (string problem in m.Problems())
					warnings.Add($"{m.Module}: {problem}");

				// AND THE LOGO, WHICH IS THE ONLY ONE THAT HAS TO BE OPENED TO BE CHECKED. It is
				// done here on purpose, even though the catalogue is assembled by reading json:
				// without this, a rejected logo is SILENCE -- the institution sees the sector's box,
				// believes its file never arrived, and has no way of knowing it arrived and did not
				// pass.
				//
				// The cost is reading a file of a few KB per module, and that is not what the lazy
				// start-up rule protects: that rule is about not WAKING twenty modules. A stat and an
				// 8 KB read wake nothing.
				if (Manifest.Txt(m.Brand?.Logo).Length > 0)
				{
					Module.LogoDataUri(m, dir, out string logoProblem);
					if (logoProblem.Length > 0) warnings.Add($"{m.Module}: {logoProblem}");
				}
			}
			catch (Exception e)
			{
				warnings.Add($"{Path.GetFileName(dir)}: {e.GetType().Name}: {e.Message}");
			}
		}

		Publish(cargados);

		return warnings;
	}

	// THE PUBLICATION, and it is the only line the rest of the program sees: up to here the old list
	// was still the good one.
	//
	// AND THE ONES THAT LEFT GET RELEASED, which is what was missing. Replacing the list took them
	// out of circulation but did not close them: a hybrid Module holds the institution's
	// AssemblyLoadContext inside and, with `keepLoaded`, its actor open over the journal folder.
	// Without this, UPDATING A MODULE left the old DLL loaded with its actor on top of the SAME
	// journal the new one had just opened -- the granted folder is deliberately kept across versions
	// --, which is exactly the two-actors-over-one-journal case `keepLoaded` exists to avoid.
	//
	// COMPARED BY REFERENCE, not by name: a reload builds new Modules even when the folder has not
	// changed, so ALL the previous ones are released. It is the same price already paid for re-reading
	// the whole catalogue, and the alternative -- guessing which ones are still «the same» -- is how
	// two truths get started.
	//
	// OUTSIDE THE PUBLICATION AND WITHOUT WAITING: the new list is already the good one and whoever
	// took the old one finishes walking it. Closing can take a while -- the Unload is COOPERATIVE and
	// the actor also has to be given a GracefulExit -- and this is called by a screen right after an
	// enrolment.
	private void Publish(List<Module> cargados)
	{
		List<Module> salientes = modules;
		modules = cargados;

		Module[] cerrando = [.. salientes.Where(v => !cargados.Any(n => ReferenceEquals(n, v)))];
		if (cerrando.Length == 0) return;

		_ = Task.Run(async () =>
		{
			foreach (Module m in cerrando)
			{
				try { await m.DisposeAsync(); } catch { /* it was already leaving */ }
			}
		});
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
