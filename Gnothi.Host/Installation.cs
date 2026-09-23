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

	// WHAT THE HOST OBSERVED WHEN IT ENROLLED THE MODULE, which is why it lives here and not in the
	// manifest: nobody declares it, Gnothi SAW it. Same reason as the granted folder and the
	// caratula -- granting, and witnessing, are acts of the granter.
	//
	// WHY IT IS NEEDED. The enrolment screen can compare the domain the institution declares against
	// the host the bundle was downloaded from, because at that moment it holds both. Afterwards it
	// CANNOT: 'servedBy' was a fact of the instant and was lost, so the module's detail showed the
	// provenance with no way to contrast it -- and for a while it painted it green, which asserted a
	// check that had never happened. Storing it turns that strip into what it claimed to be.
	//
	// ALL THREE BELONG TO THE SAME ACT and once written they never change. That is what separates
	// them from 'enabled' and 'disabledTools', the only mutable things in this file: ReloadConsent
	// re-reads those two and does not touch this.
	[JsonPropertyName("provenance")] public Provenance? Provenance { get; init; }

	// THE ACTS ACCUMULATE (NotasGnothiHost/44 §3.7). 'provenance' above is the LATEST act and keeps
	// its shape, so every reader that knows only that field goes on working; this list is every
	// act so far, oldest first, the latest one repeated at the end. It is what lets the detail say
	// "you accepted 2.4.0 on September 9 and this is 2.5.0". Empty for modules installed before
	// the list existed: absent in the JSON reads as empty, never as null (HostJson.cs, the trap
	// noted in the memory of the .deps.json).
	[JsonPropertyName("acts")] public IReadOnlyList<Provenance> Acts { get; init; } = [];

	// THE KEY THIS MODULE WAS ACCEPTED WITH, when its package came signed: the pin. Written at
	// the first install and kept while the key stays the same, so the next package can be
	// answered with "signed with the same key as last time" -- or with "the key changed", which
	// is the one thing an origin check cannot say. Absent for an unsigned package. It lives here
	// and not in the manifest for the usual reason: what Gnothi remembers about a module is
	// Gnothi's, not the module's to rewrite. Unlike the provenance above it is not frozen: the
	// origin can retire the key later, and the mark of that (revokedAt) is written here too.
	[JsonPropertyName("signer")] public PackageSigner? Signer { get; init; }

	// A NEWER PACKAGE THE ORIGIN PUBLISHES, if Gnothi saw one when it last asked: the version,
	// where to download it and its seal, and when it was seen. Written by the startup review of
	// the origins (NotasGnothiHost/44 §4) and cleared when the installed package matches what is
	// published again -after she updates. It is a NOTE, not an action: nothing downloads or
	// installs until she taps the card. Absent when nothing newer is published, or the origin
	// publishes no list.
	[JsonPropertyName("update")] public AvailableUpdate? Update { get; init; }

	// Through HostJson and not through Deserialize<T>: the reader is generated at compile time, with
	// no reflection. See HostJson.cs -- whether the Host can be embedded under NativeAOT depends on it.
	public static Installation Read(string path)
	{
		Installation read = JsonSerializer.Deserialize(File.ReadAllText(path), HostJson.Default.Installation)
			?? throw new InvalidDataException($"empty or unreadable installation: {path}");
		return read.AnchoredAt(Path.GetDirectoryName(Path.GetFullPath(path)) ?? "");
	}

	// A NON-ROOTED shell.arguments[0] IS RELATIVE TO THE FOLDER OF THE installation.json THAT
	// NAMES IT. The first argument is the thing the shell runs or loads -the dll for stdio and
	// in-process, the web folder for webview- and it is the one path in this file that points at
	// code rather than at the person's data.
	//
	// Why this rule and not "relative to the working directory": the shells used to call
	// Path.GetFullPath on it, which resolves against wherever the process happened to start, so
	// a relative path worked from one directory and not from the next. And why relative at all:
	// the versioned Modulos/ bench carried the absolute paths of the machine where the POC was
	// assembled, so on every other machine -and in CI- no module could start and the conformance
	// suite verified nothing. The installer keeps writing absolute paths (the dll lives inside
	// the module's folder, so both forms name the same file); the bench in the repo writes
	// relative ones, and this is where the two meet.
	//
	// Only arguments[0] is anchored. The rest are the shell's own flags, and the granted folder
	// never travels here: the shells append it themselves.
	private Installation AnchoredAt(string folder)
	{
		if (Shell.Arguments.Count == 0) return this;
		string first = Shell.Arguments[0];
		if (first.Length == 0 || Path.IsPathRooted(first)) return this;

		string[] arguments = [.. Shell.Arguments];
		arguments[0] = Path.GetFullPath(Path.Combine(folder, first));
		return this with { Shell = Shell with { Arguments = arguments } };
	}
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

// WHERE THIS MODULE CAME FROM, witnessed by the Host at enrolment time.
public sealed record Provenance
{
	// THE HOST THE BUNDLE WAS DOWNLOADED FROM. Empty is legitimate and means "it did not come off
	// the network": a .gmod imported from a file has no server, and in that case there is nothing to
	// contrast -- which is different from contrasting and not matching.
	[JsonPropertyName("servedBy")] public string ServedBy { get; init; } = "";

	// When the person accepted. ISO 8601 in UTC.
	[JsonPropertyName("at")] public string At { get; init; } = "";

	// WHICH VERSION THEY ACCEPTED, which is what makes it possible to say later "you accepted 2.4.0
	// and this bundle declares 2.5.0". Without it, reinstalling can only say "you already have this
	// module".
	[JsonPropertyName("version")] public string Version { get; init; } = "";

	// WHETHER WHAT THE INSTITUTION DECLARES MATCHES WHOEVER HANDED OVER THE BUNDLE.
	//
	// Returns null -- NOT true -- when there is nothing to compare against, and that distinction is
	// the whole point: "matches", "does not match" and "cannot be known" are three things, and the
	// screen has to be able to say the third instead of painting a tick.
	public bool? Matches(Publisher? publisher)
	{
		if (ServedBy.Length == 0) return null;
		if (publisher is null || Manifest.Txt(publisher.Domain).Length == 0) return null;
		return publisher.MatchesServer(ServedBy);
	}
}

// WHAT THE ORIGIN PUBLISHES AND GNOTHI DOES NOT HAVE YET. Version and seal as the well-known lists
// them, the bundle's absolute address to fetch it from when she asks, and the moment it was seen.
public sealed record AvailableUpdate
{
	[JsonPropertyName("version")] public string Version { get; init; } = "";
	[JsonPropertyName("bundle")] public string Bundle { get; init; } = "";
	[JsonPropertyName("seal")] public string Seal { get; init; } = "";
	[JsonPropertyName("seenAt")] public string SeenAt { get; init; } = "";

	// The same thing published: version and seal, not the time it was seen.
	public bool SameAs(AvailableUpdate? other)
		=> other is not null && string.Equals(Seal, other.Seal, StringComparison.Ordinal) && string.Equals(Version, other.Version, StringComparison.Ordinal);
}

public sealed record Shell
{
	[JsonPropertyName("type")] public string Type { get; init; } = "stdio";
	[JsonPropertyName("command")] public string Command { get; init; } = "";
	[JsonPropertyName("arguments")] public IReadOnlyList<string> Arguments { get; init; } = [];
}
