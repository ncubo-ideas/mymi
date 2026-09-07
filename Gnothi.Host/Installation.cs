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

	// WHO MAY SPEAK FOR THIS MODULE FROM OUTSIDE THE PROCESS. Only meaningful where a gate
	// lets another app in -today Gate 3 on Android, where a per-module "caratula" APK declares
	// the app functions to the OS and forwards here. Everywhere else it is inert.
	//
	// It lives in the INSTALLATION and not in the manifest for the same reason the granted
	// folder does: the module does not get to say who may impersonate it. Granting is an act
	// of the granter.
	//
	// ABSENT MEANS NOBODY. A module with no caratula declared cannot be reached from another
	// process at all. That default is deliberate: forgetting to declare it must fail closed,
	// never open.
	[JsonPropertyName("caratula")] public AllowedCaller? AllowedCaller { get; init; }

	// Por HostJson y no por Deserialize<T>: el lector se genera al compilar, sin reflexion.
	// Ver HostJson.cs -- de eso depende que el Host pueda embeberse bajo NativeAOT.
	public static Installation Read(string path)
		=> JsonSerializer.Deserialize(File.ReadAllText(path), HostJson.Default.Installation)
		   ?? throw new InvalidDataException($"empty or unreadable installation: {path}");
}

// THE FRONT THAT IS ALLOWED TO SPEAK FOR A MODULE, as data. The Host core declares WHO is
// allowed; it does NOT verify -verifying means asking the platform's package manager, which
// is Android's business and would drag the OS into a core that has to stay portable.
//
// The split matters: the same declaration will serve iOS, where the check is a different API
// and the same three fields still name the same thing.
public sealed record AllowedCaller
{
	// The calling app's package name. Necessary and NOT sufficient: a package name is a
	// string anyone can claim by naming their app the same.
	[JsonPropertyName("package")] public string Package { get; init; } = "";

	// SHA-256 of the caller's signing certificate, hex, uppercase, no separators.
	//
	// THIS is the half that cannot be faked: the Host asks the system for the signature of the
	// package that actually made the call. An impostor can copy the package name; it cannot
	// produce the institution's signature without its private key.
	//
	// Empty means the check cannot pass. Same reasoning as an absent caratula: fail closed.
	[JsonPropertyName("signatureSha256")] public string SignatureSha256 { get; init; } = "";

	// What the front was allowed to publish when it was installed, so a later release that
	// silently grew tools does not inherit the grant. Optional today, and left here because
	// leaving room is cheaper than migrating the file later.
	[JsonPropertyName("releaseId")] public string ReleaseId { get; init; } = "";

	public bool IsDeclared => Package.Length > 0 && SignatureSha256.Length > 0;

	// Case and separators are normalized because every tool prints digests differently:
	// keytool uses colons, apksigner does not, and both mix case. Comparing raw would reject
	// a correct signature over punctuation.
	public bool Accepts(string package, string signatureSha256) =>
		IsDeclared
		&& string.Equals(Package, package, StringComparison.Ordinal)
		&& string.Equals(Normalize(SignatureSha256), Normalize(signatureSha256), StringComparison.OrdinalIgnoreCase);

	public static string Normalize(string digest) =>
		string.Concat(digest.Where(char.IsAsciiHexDigit)).ToUpperInvariant();
}

public sealed record Shell
{
	[JsonPropertyName("type")] public string Type { get; init; } = "stdio";
	[JsonPropertyName("command")] public string Command { get; init; } = "";
	[JsonPropertyName("arguments")] public IReadOnlyList<string> Arguments { get; init; } = [];
}
