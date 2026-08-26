using System.ComponentModel;
using System.Text.Json.Serialization;
using ModelContextProtocol.Server;

namespace Gnothi.Host;

// GNOTHI'S OWN TOOLS. They answer what NO module can answer about itself: how many there are,
// how much they take up measured from outside, and how much the assistant asked them.
//
// There is deliberately NO tool to enable, disable or change permissions. Consent belongs to
// the person; if the assistant could modify it over MCP, the authority over consent would move
// to the model. It is changed by editing installation.json.
[McpServerToolType]
internal sealed class HostTools
{
	private readonly Dispatcher dispatcher;

	public HostTools(Dispatcher dispatcher) => this.dispatcher = dispatcher;

	[McpServerTool(Name = "gnothi_installed_modules", UseStructuredContent = true, ReadOnly = true)]
	[Description("Which modules the person has installed on this device, how much each one's record takes up "
		+ "as measured by the Host from outside, whether they are enabled, and how many assistant queries each one "
		+ "received. Use it when you are asked what is available or where the data comes from.")]
	public InstalledModules Modules()
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
	// expensive thing: it starts the module, asks it for its tools/list OVER THE PROTOCOL, and
	// compares. A tool the module has and did not declare is the interesting signal.
	[McpServerTool(Name = "gnothi_verify_module", UseStructuredContent = true, ReadOnly = true)]
	[Description("Compares what a module DECLARES in its manifest against what it actually exposes when queried. "
		+ "Useful for finding out whether a module hides capabilities it did not declare. Takes the module's name.")]
	public async Task<Verification> Verify(
		[Description("The module's name, for example 'facturas'.")] string module,
		CancellationToken ct)
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
	[JsonPropertyName("recordBytes")]
	[Description("Measured by the Host with stat over the folder it granted, not reported by the module.")]
	public long RecordBytes { get; init; }
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
