using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gnothi.Host;

// THE MANIFEST BELONGS TO THE MODULE. The institution writes it, and some day will sign it.
// It declares identity, which substrate it needs, which capabilities it asks for, and -the
// part this POC cares about- ITS LIST OF TOOLS.
//
// Why the list comes from here instead of being asked of the module by starting it up:
// reading a json costs microseconds and wakes nothing. The Host has to be able to build the
// catalog of twenty modules without instantiating twenty runtimes. The module only starts
// when somebody calls one of its tools.
//
// And because it is a DECLARATION, it can be verified: later on the Host asks the module for
// its real tools/list and compares. What was declared against what is.
internal sealed record Manifest
{
	[JsonPropertyName("manifest")] public string Version { get; init; } = "";
	[JsonPropertyName("module")] public string Module { get; init; } = "";
	[JsonPropertyName("title")] public string Title { get; init; } = "";
	[JsonPropertyName("institution")] public string Institution { get; init; } = "";
	[JsonPropertyName("substrate")] public Substrate? Substrate { get; init; }
	[JsonPropertyName("capabilities")] public Capabilities? Capabilities { get; init; }
	[JsonPropertyName("tools")] public IReadOnlyList<DeclaredTool> Tools { get; init; } = [];

	public static Manifest Read(string path)
	{
		Manifest? m = JsonSerializer.Deserialize<Manifest>(File.ReadAllText(path), JsonOptions);
		if (m is null) throw new InvalidDataException($"empty or unreadable manifest: {path}");
		if (string.IsNullOrWhiteSpace(m.Module))
			throw new InvalidDataException($"the manifest does not declare 'module': {path}");
		return m;
	}

	internal static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNameCaseInsensitive = true,
		ReadCommentHandling = JsonCommentHandling.Skip,
		AllowTrailingCommas = true,
	};
}

// A RANGE, NOT an exact version. If a module pins one precise version, every Host update
// orphans every installed module.
internal sealed record Substrate
{
	[JsonPropertyName("range")] public string Range { get; init; } = "";
}

// What the module DECLARES it needs. The value of this is not informational: it is that the
// Host can check it against what the module actually asks for at run time. A module that
// declares 'network: false' and opens a socket is a module that lies, and that is detectable.
internal sealed record Capabilities
{
	[JsonPropertyName("network")] public bool Network { get; init; }
	[JsonPropertyName("folder")] public string Folder { get; init; } = "own";
	[JsonPropertyName("speaker")] public bool Speaker { get; init; }
	[JsonPropertyName("microphone")] public bool Microphone { get; init; }

	public IEnumerable<string> Excesses()
	{
		if (Network) yield return "network";
		if (Speaker) yield return "speaker";
		if (Microphone) yield return "microphone";
		if (Folder != "own") yield return $"folder='{Folder}'";
	}
}

// A tool exactly as the module declares it. 'input' stays a list of simple parameters: this
// POC covers tools with no parameters and tools with text parameters, which is what the
// invoices module needs. The general case -an arbitrary input schema- calls for handling
// tools/call by hand instead of synthesizing a delegate, and is noted as pending.
internal sealed record DeclaredTool
{
	[JsonPropertyName("name")] public string Name { get; init; } = "";
	[JsonPropertyName("title")] public string Title { get; init; } = "";
	[JsonPropertyName("description")] public string Description { get; init; } = "";
	[JsonPropertyName("readOnly")] public bool ReadOnly { get; init; } = true;
	[JsonPropertyName("input")] public IReadOnlyList<DeclaredParameter> Input { get; init; } = [];
}

internal sealed record DeclaredParameter
{
	[JsonPropertyName("name")] public string Name { get; init; } = "";
	[JsonPropertyName("type")] public string Type { get; init; } = "string";
	[JsonPropertyName("description")] public string Description { get; init; } = "";
}
