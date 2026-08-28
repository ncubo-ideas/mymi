using System.Text.Json.Serialization;

namespace Gnothi.Host;

// GNOTHI'S OWN TOOLS, BELOW THE SEAM.
//
// They answer what NO module can answer about itself: how many there are, how much each one
// takes up measured from outside, and how much the assistant asked them. That is the Host's
// fourth verb, and until now it lived ABOVE the seam -- this class carried [McpServerToolType]
// and Gate 1 registered it with .WithTools<HostTools>().
//
// The consequence of that was invisible on the desktop and severe on the phone: HostTools was
// excluded from the Android build because it dragged in the MCP SDK, so on Sonia's phone the
// assistant could not ask what Gnothi knows about itself. The fourth verb simply was not there.
// And a third gate would have needed its own copy of the same thing.
//
// So the tools are declared HERE, in the SAME type a module uses -- DeclaredTool -- and travel
// through the same index every gate already reads. No gate had to change to gain them: Gate 1
// projects them into MCP and Gate 2 into a string for the model, exactly as they already do
// with the modules' tools.
//
// There is deliberately NO tool to enable, disable or change permissions. Consent belongs to
// the person; if the assistant could modify it, the authority over consent would move to the
// model. It is changed by editing installation.json.
internal sealed class HostTools
{
	private readonly Dispatcher dispatcher;

	public HostTools(Dispatcher dispatcher) => this.dispatcher = dispatcher;

	// WHAT THE HOST PUBLISHES, in the modules' own vocabulary.
	//
	// Declaring them as DeclaredTool and not as C# methods with attributes is what puts them
	// below the seam: ToolIndex builds their public name, their schema and their description
	// the same way it does for anybody else's, and it does not need to know these are special.
	public static IReadOnlyList<DeclaredTool> Declared { get; } =
	[
		new DeclaredTool
		{
			Name = "installed_modules",
			Title = "Installed modules",
			ReadOnly = true,
			Description =
				"Which modules the person has installed on this device, how much each one's record takes up "
				+ "as measured by the Host from outside, whether they are enabled, and how many assistant "
				+ "queries each one received. Use it when you are asked what is available or where the data "
				+ "comes from.",
		},
		new DeclaredTool
		{
			Name = "verify_module",
			Title = "Verify a module",
			ReadOnly = true,
			Description =
				"Compares what a module DECLARES in its manifest against what it actually exposes when "
				+ "queried. Useful for finding out whether a module hides capabilities it did not declare.",
			Input =
			[
				new DeclaredParameter
				{
					Name = "module",
					Type = "string",
					Description = "The module's name, for example 'facturas'.",
				},
			],
		},
	];

	// THE SAME SHAPE AS IShell.Invoke, and that is not a coincidence: from the dispatcher's
	// point of view the Host is one more thing that answers by name and returns opaque content.
	public Task<object> Invoke(string tool, IReadOnlyDictionary<string, object?>? args, CancellationToken ct)
		=> tool switch
		{
			"installed_modules" => Task.FromResult<object>(Modules()),
			"verify_module" => Verify(Text(args, "module"), ct).ContinueWith(t => (object)t.Result, ct),
			_ => throw new InvalidOperationException($"the Host has no tool called '{tool}'"),
		};

	private static string Text(IReadOnlyDictionary<string, object?>? args, string name)
		=> args is not null && args.TryGetValue(name, out object? v) ? v?.ToString() ?? "" : "";

	private InstalledModules Modules()
	{
		IReadOnlyDictionary<string, int> queries = dispatcher.Meter.Queries();

		return new InstalledModules
		{
			Count = dispatcher.Modules.Modules.Count,
			Modules = [.. dispatcher.Modules.Modules.Select(m => new ModuleSummary
			{
				Name = m.Name,
				Title = m.Manifest.Title,
				Institution = m.Manifest.Institution,
				Enabled = m.Installation.Enabled,
				PublishedTools = m.EnabledTools().Count(),
				DisabledTools = [.. m.Installation.DisabledTools],
				RecordBytes = m.BytesOnDisk(),
				Running = m.IsRunning,
				AssistantQueries = queries
					.Where(kv => kv.Key.StartsWith(m.Name + "/", StringComparison.Ordinal))
					.Sum(kv => kv.Value),
			})],
		};
	}

	// THE VERIFICATION: declared against real.
	//
	// The catalog is built from the manifest -cheap, waking nothing up. This tool does the
	// expensive thing: it starts the module, asks it for its tools OVER THE SHELL, and
	// compares. A tool the module has and did not declare is the interesting signal.
	private async Task<Verification> Verify(string module, CancellationToken ct)
	{
		Module? m = dispatcher.Modules.Find(module);
		if (m is null)
			return new Verification { Module = module, Matches = false, Detail = $"there is no installed module called '{module}'" };

		string[] declared = [.. m.Manifest.Tools.Select(t => t.Name).Order()];

		try
		{
			string[] actual = [.. (await m.ActualToolNames(ct)).Order()];
			string[] undeclared = [.. actual.Except(declared)];
			string[] missing = [.. declared.Except(actual)];

			return new Verification
			{
				Module = m.Name,
				Declared = declared,
				Actual = actual,
				Undeclared = undeclared,
				DeclaredButMissing = missing,
				Matches = undeclared.Length == 0 && missing.Length == 0,
				Detail = undeclared.Length > 0
					? "the module exposes tools it did not declare in its manifest"
					: missing.Length > 0 ? "the manifest declares tools the module does not have" : "",
			};
		}
		catch (Exception e)
		{
			return new Verification { Module = m.Name, Declared = declared, Matches = false, Detail = $"could not query the module: {e.GetType().Name}: {e.Message}" };
		}
	}
}

internal sealed class InstalledModules
{
	[JsonPropertyName("count")] public int Count { get; init; }
	[JsonPropertyName("modules")] public IReadOnlyList<ModuleSummary> Modules { get; init; } = [];
}

internal sealed class ModuleSummary
{
	[JsonPropertyName("name")] public string Name { get; init; } = "";
	[JsonPropertyName("title")] public string Title { get; init; } = "";
	[JsonPropertyName("institution")] public string Institution { get; init; } = "";
	[JsonPropertyName("enabled")] public bool Enabled { get; init; }
	[JsonPropertyName("publishedTools")] public int PublishedTools { get; init; }
	[JsonPropertyName("disabledTools")] public IReadOnlyList<string> DisabledTools { get; init; } = [];
	// Measured by the Host with stat over the folder it granted, not reported by the module.
	[JsonPropertyName("recordBytes")] public long RecordBytes { get; init; }
	[JsonPropertyName("running")] public bool Running { get; init; }
	[JsonPropertyName("assistantQueries")] public int AssistantQueries { get; init; }
}

internal sealed class Verification
{
	[JsonPropertyName("module")] public string Module { get; init; } = "";
	[JsonPropertyName("matches")] public bool Matches { get; init; }
	[JsonPropertyName("declared")] public IReadOnlyList<string> Declared { get; init; } = [];
	[JsonPropertyName("actual")] public IReadOnlyList<string> Actual { get; init; } = [];
	[JsonPropertyName("undeclared")] public IReadOnlyList<string> Undeclared { get; init; } = [];
	[JsonPropertyName("declaredButMissing")] public IReadOnlyList<string> DeclaredButMissing { get; init; } = [];
	[JsonPropertyName("detail")] public string Detail { get; init; } = "";
}
