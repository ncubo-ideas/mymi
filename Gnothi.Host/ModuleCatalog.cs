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
	// LA LISTA SE REEMPLAZA, NO SE VACIA. Y no es un detalle de estilo: es lo que hace que
	// recorrerla sea seguro mientras el catalogo se recarga.
	//
	// Load() hacia Clear() y volvia a llenar ESTA MISMA lista, asi que cualquiera que la
	// estuviera recorriendo -- la pantalla armando su estado, VerifyNewModules con sus await
	// adentro -- se llevaba un `InvalidOperationException: Collection was modified`. Medido en
	// el emulador el 2026-09-08: aceptar un alta recarga el catalogo, y la pagina pedia su
	// estado en ese mismo instante.
	//
	// Con el reemplazo, quien tomo la referencia vieja termina de recorrerla completa y sin
	// excepcion -- una lista inmutable de hecho -- y el que llega despues ve la nueva. Cuesta
	// una asignacion y cierra la clase entera de bugs, en vez de un recorrido a la vez.
	private List<Module> modules = [];

	public IReadOnlyList<Module> Modules => modules;
	public string Root { get; }

	// The folder an update parks the previous version in: `<module>.anterior`, next to the live
	// one. Named here because the catalog must skip it and the enrollment must write it.
	public const string PreviousSuffix = ".anterior";

	public ModuleCatalog(string root) => Root = root;

	public IReadOnlyList<string> Load()
	{
		// SE LLENA UNA LISTA NUEVA y al final se publica de un tiro. Ver el comentario de
		// `modules`: mientras esto corre, quien este recorriendo la anterior no se entera.
		List<Module> cargados = [];
		List<string> warnings = [];

		if (!Directory.Exists(Root))
		{
			// LA LISTA VACIA SE PUBLICA IGUAL. Antes este camino salia despues del Clear(), asi
			// que dejaba el catalogo vacio; con el reemplazo hay que decirlo explicitamente o
			// una carpeta que desaparece dejaria en pie la lista anterior -- modulos que ya no
			// estan, contestando.
			modules = cargados;
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

				// Y lo mismo con los topes de lo que la institucion declara de si misma -- el
				// nombre corto, el logo, el color, los textos. NO tumban el modulo: dejar a la
				// persona sin su laboratorio porque a alguien se le fue un renglon seria peor
				// que el renglon. Se dicen, y se dicen aca, que es donde ya se dicen estas cosas.
				foreach (string problem in m.Problems())
					warnings.Add($"{m.Module}: {problem}");

				// Y EL LOGO, QUE ES EL UNICO QUE HAY QUE ABRIR PARA REVISAR. Se hace aca a
				// proposito, aunque el catalogo se arma leyendo json: sin esto, un logo
				// rechazado es un SILENCIO -- la institucion ve la caja del rubro, cree que su
				// archivo no llego, y no tiene forma de saber que llego y no paso.
				//
				// El costo es leer un archivo de pocos KB por modulo, y no es lo que la regla
				// del arranque perezoso protege: eso es no DESPERTAR veinte modulos. Un stat y
				// un read de 8 KB no despiertan nada.
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

		// LA PUBLICACION, y es la unica linea que el resto del programa ve: hasta aca la lista
		// vieja seguia siendo la buena.
		modules = cargados;

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
