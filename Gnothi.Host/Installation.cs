using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gnothi.Host;

// THE INSTALLATION BELONGS TO THE HOST. Deliberately kept apart from the manifest, and it is
// not cosmetic: these are two different authorities writing two different files.
//
//   manifest.json      the INSTITUTION writes it: who I am, what I need, what I offer.
//   installation.json  the HOST writes it:        where I put your binary, which folder I
//                                                granted you, and whether you are enabled.
//
// If the shell and the folder came in the manifest, the module would choose how it is run and
// where it writes. Both belong to the Host by definition: granting is an act of the granter.
public sealed record Installation
{
	// How the Host brings this module up. In this POC: child process + stdio.
	// Stage 2 changes ONLY this for a wasm runtime with --dir, and neither the manifest nor
	// the module notices. That is what we want to show: one artifact, two hostings.
	[JsonPropertyName("shell")] public Shell Shell { get; init; } = new();

	// The folder the Host GRANTED it. The module does not choose it.
	[JsonPropertyName("grantedFolder")] public string GrantedFolder { get; init; } = "";

	// The switch. It belongs to the Host: if the module implemented it, it would not be a
	// switch.
	[JsonPropertyName("enabled")] public bool Enabled { get; init; } = true;

	// Tools disabled one by one. Disabling the whole module is the coarse case; this is the
	// fine one, which is the one actually needed -"the total yes, the detail no".
	[JsonPropertyName("disabledTools")] public IReadOnlyList<string> DisabledTools { get; init; } = [];

	public static Installation Read(string path)
		=> JsonSerializer.Deserialize<Installation>(File.ReadAllText(path), Manifest.JsonOptions)
		   ?? throw new InvalidDataException($"empty or unreadable installation: {path}");
}

public sealed record Shell
{
	[JsonPropertyName("type")] public string Type { get; init; } = "stdio";
	[JsonPropertyName("command")] public string Command { get; init; } = "";
	[JsonPropertyName("arguments")] public IReadOnlyList<string> Arguments { get; init; } = [];
}
