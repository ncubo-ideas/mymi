namespace Gnothi.Host;

// ONE MODULE INSTANCE: what the institution declared + what the Host granted it + the shell
// it is run with.
//
// This is the unit of isolation. In this POC the real isolation is whatever the operating
// system gives a child process -which is little- plus the folder of its own. Hard isolation
// (its own linear memory, --dir) arrives with Stage 2. Better not to claim that this POC
// demonstrates confinement: it demonstrates the aggregation link, which is another thing.
public sealed class Module : IAsyncDisposable
{
	private readonly IShell shell;

	public Module(Manifest manifest, Installation installation, string directory)
	{
		Manifest = manifest;
		Installation = installation;
		Directory = directory;
		shell = ChooseShell(manifest.Module, installation);
	}

	public Manifest Manifest { get; }
	// Consent is the ONLY thing about a module that is mutable while it is running: private
	// setter and a single write path, Reconsent. The shell and the granted folder are not
	// reloaded, because changing those is reinstalling, not reconsenting.
	public Installation Installation { get; private set; }
	public string Directory { get; }
	public string Name => Manifest.Module;
	public bool IsRunning => shell.IsRunning;

	// The name the tool travels under towards the assistant. Prefixed by module because two
	// institutions are going to call two different things 'total', and the assistant picks by
	// name and description.

	// THE FACTORY. This is where `Shell.Type` starts being read -- it was declared in
	// Installation.cs from the beginning and nobody consulted it: the shell was hardcoded.
	// Now the choice is PER MODULE, and it lives in installation.json, which is the Host's.
	//
	// Practical consequence: facturas can stay on stdio while examenes runs in-process, both
	// over the same journals. Migrating is changing one word in a json.
	private static IShell ChooseShell(string name, Installation installation) =>
		installation.Shell.Type.ToLowerInvariant() switch
		{
			"in-process" => new InProcessShell(name, installation.Shell, installation.GrantedFolder),
			_            => new StdioShell(name, installation.Shell, installation.GrantedFolder),
		};

	public static string PublicName(string module, string tool) => $"{module}_{tool}";

	// The tools this module CONTRIBUTES to the catalog, already filtered by consent.
	//
	// Disabled returns empty: the module never finds out, and cannot tell "I was switched off"
	// apart from "nobody asked". That indistinguishability is the property, not a side effect.
	public IEnumerable<DeclaredTool> EnabledTools()
	{
		if (!Installation.Enabled) return [];
		return Manifest.Tools.Where(t => !Installation.DisabledTools.Contains(t.Name));
	}

	// RECONSENT: the person moved a switch and the Host found out by reading their file.
	//
	// Returns whether anything really changed, because a text editor writes the same file
	// three times and the assistant does not need to be told three times that its catalog
	// changed.
	//
	// Only the two consent fields come in. That the signature does not accept a whole
	// Installation is on purpose: it makes it IMPOSSIBLE for a shell change to sneak in
	// through here and pull the floor out from under a module that is already running.
	public bool Reconsent(bool enabled, IReadOnlyList<string> disabledTools)
	{
		bool sameSwitch = Installation.Enabled == enabled;
		bool sameDisabled = Installation.DisabledTools.Count == disabledTools.Count
			&& !Installation.DisabledTools.Except(disabledTools, StringComparer.Ordinal).Any();

		if (sameSwitch && sameDisabled) return false;

		Installation = Installation with { Enabled = enabled, DisabledTools = disabledTools };
		return true;
	}

	public bool Allows(string tool)
		=> Installation.Enabled && !Installation.DisabledTools.Contains(tool);

	public Task<object> Invoke(
		string tool, IReadOnlyDictionary<string, object?>? args, CancellationToken ct)
		=> shell.Invoke(tool, args, ct);

	// For the declared-against-real verification.
	public Task<IReadOnlyList<string>> ActualToolNames(CancellationToken ct)
		=> shell.ActualTools(ct);

	// Measured FROM OUTSIDE, with stat over the folder the Host granted. The module is not
	// asked how much it takes up: a module that reports 4 MB while taking up 400 cannot exist
	// if nobody asks it.
	public long BytesOnDisk()
	{
		var granted = new DirectoryInfo(Installation.GrantedFolder);
		if (!granted.Exists) return 0;
		return granted
			.EnumerateFiles("*", SearchOption.AllDirectories)
			.Sum(f => f.Length);
	}

	public ValueTask DisposeAsync() => shell.DisposeAsync();
}
