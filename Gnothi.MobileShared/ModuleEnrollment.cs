using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Gnothi.Host;

namespace Gnothi.MobileShared;

// ENROLLING A MODULE. This is how a person really gets a module: the institution puts a button
// and a code on its page, Sonia taps the button -or pastes the link, or points Gnothi's camera
// at the code, or opens the file it sent her- and Gnothi downloads or reads the bundle, INSPECTS
// it, shows her what it found, and installs it only if she says yes (specification 11 §3, 13 §7,
// 13 §10.4). Until this existed modules only got in by pushing files with adb, which is not a
// path that exists for anybody.
//
// THE BUNDLE IS A ZIP -named <modulo>.gmod, like the desktop's cargo- and carries two things: the
// institution's `manifest.json` and the `web/` folder with its published app. Nothing else. No
// executable, no installation: the installation is written by GNOTHI, because it is Gnothi's
// file and Gnothi's authority.
//
// WHAT GNOTHI DECIDES AND THE INSTITUTION DOES NOT: where the journal is kept, which shell the
// module runs under, and that the publisher's endpoint is fixed. The manifest DECLARES where
// its publisher is; Gnothi registers it once and from then on the module cannot change it.
//
// WHERE THE BYTES CAME FROM IS RECORDED, NOT ASSUMED. Every way in -the institution's link, a
// pasted link, a scanned code, a file- ends in the same examination over the same bytes,
// carrying a Provenance that says which way it was and who served it. The sheet reads it: a
// package that arrived from a link has a host to name; one that arrived as a file has only its
// name, and the sheet says so instead of pretending.
//
// THE SEAL. A link can carry the hash of the manifest it was made for (&h=). When it does, a
// package whose manifest no longer hashes to it is refused: whoever made the link -or printed
// the code- fixed WHICH package it points at, and a swap on the server afterwards is caught.
// That is integrity, not authenticity: the one who makes the link chooses both the address and
// the seal, so a forged code is not caught by this. The sheet says the first and not the second.
//
// THE ORIGIN. The origin that served the bundle is asked whether the bundle is its own -at
// /.well-known/gnothi/modules.json- and the manifest is asked which origin it claims. When both
// agree with the origin that actually served it, TLS is what stands behind the answer, and the
// sheet says the module is BACKED by its origin. When they do not, the sheet says which of the
// three failed. A FILE came from no origin, but its manifest still claims one: that origin is
// asked whether it publishes exactly this manifest -its seal- and the same backing follows, over
// the same TLS. A CONTRADICTION is refused -the origin lists its modules and this is not one, or
// the package claims an origin other than the one that served it-; SILENCE is described on the
// sheet, and whether it becomes a refusal too lives in ONE place -OriginIsRequired (NotasGnothiHost/43).
//
// THE SIGNATURE. A package may come signed (Gnothi.Host/PackageSignature.cs): every file digested,
// the list signed, the public key inside. Integrity is checked with nothing else in hand, and a
// signed package whose signature does not hold is REFUSED -somebody touched it. Whose key it is,
// the origin's well-known says (it publishes its keys) or the PIN says: the key the module was
// accepted with the first time, kept in installation.json. That is what the origin check cannot
// give: continuity -"the same key as last time"- with the origin down, and a loud word when the
// key changed and nobody published the new one.
//
// WHAT GNOTHI CHECKS BEFORE SHOWING ANYTHING is PackageInspector -the same inspection the
// desktop installer runs (Gnothi.Host/PackageInspector.cs): the package's form, whether it
// carries an engine, executables or a sample journal, and which capabilities it asks for. It
// runs over the zip BEFORE a byte is written to disk, and its result travels in the proposal so
// the sheet can say what was checked.
//
// NOTHING HERE IS PLATFORM-SPECIFIC and that is not an accident: downloading a zip, refusing a
// zip-slip, reading a manifest and writing an installation are the same on any phone. It lived
// in the Android project until iOS needed it, and moving it cost one parameter -- where the log
// goes.

// HOW THE BUNDLE REACHED GNOTHI.
public enum EnrollmentSource
{
	// The institution's button: an App Link (Android) or universal link (iOS) landed on Gnothi.
	Link,

	// The person pasted the institution's link into Gnothi herself.
	PastedLink,

	// The person pointed Gnothi's camera at the institution's code.
	Qr,

	// The person opened a .gmod, shared it to Gnothi, or picked it from her phone.
	File,
}

// WHAT THE ORIGIN CHECK FOUND. The origin that served the bundle is asked -at its well-known-
// whether the bundle is its own, and the manifest is asked which origin it claims. Only when both
// answers agree with what actually served it is the module BACKED by its origin; every other
// outcome is named, because the sheet tells the person which one it was. For a file, the origin
// asked is the one the manifest claims, and "its own" means "publishes exactly this manifest".
public enum OriginCheck
{
	// The origin publishes this bundle and the manifest declares that origin.
	Verified,

	// No check was made: the bytes came from no origin and none was asked.
	NoOrigin,

	// The origin publishes nothing Gnothi can read at /.well-known/gnothi/modules.json.
	NoWellKnown,

	// The origin publishes its modules, and this one is not among them.
	NotListed,

	// The manifest declares no origin: there is nobody to ask, and it cannot be tied down.
	NotDeclared,

	// The manifest claims an origin other than the one that served it.
	DeclaresAnother,
}

// WHOSE KEY SIGNED THE PACKAGE, once the signature itself holds. A package that comes unsigned is
// just that; an invalid one never gets this far.
public enum SignerCheck
{
	Unsigned,

	// The key is among those the origin publishes. First time, or the same key as last time.
	Published,

	// The origin did not answer -or publishes no keys-, but the key is the one Gnothi accepted
	// last time: continuity, offline.
	Pinned,

	// A new key, but the origin publishes it: the institution rotated. Remembered from now.
	Rotated,

	// The origin does not publish this key and Gnothi never saw it: nobody vouches for it.
	Unknown,

	// Not the key of last time, and the origin does not publish the new one. The loudest word.
	Changed,

	// The origin lists this key among the ones it RETIRED. The institution itself says no: the
	// package is refused, pinned or not, and a module already accepted with it is marked.
	Revoked,
}

// WHERE THE BYTES CAME FROM. ServedBy is the host that answered the download -or the file's name-
// a fact about transport, and Origin says how far that fact could be tied to the institution.
// MatchesLink is true when the link carried a seal and the package hashed to it -- integrity
// against the link, nothing more. Secure says whether TLS stood behind the answer: over plain
// http (development) the origin check proves consistency, not authenticity, and the sheet says so.
// Signer says whose key signed the package, SignerId names it, and SignedSince is when that same
// key was first accepted -- empty the first time.
public sealed record Provenance(
	EnrollmentSource Source,
	string ServedBy,
	bool MatchesLink = false,
	OriginCheck Origin = OriginCheck.NoOrigin,
	bool Secure = false,
	string DeclaredOrigin = "",
	SignerCheck Signer = SignerCheck.Unsigned,
	string SignerId = "",
	string SignedSince = "");

// THE LINK, TAKEN APART. The institution's page links to Gnothi's domain with the bundle's
// address as a parameter and, since 2026-09-08, the seal of its manifest:
//
//     https://go.gnothi.app/alta?bundle=<url>&h=<seal>
//
// A pasted link or a scanned code can be that, or the bundle's address itself. Both are
// accepted. Text that is not an address comes back as the bundle untouched, for Examine to
// refuse with its own words.
public sealed record EnrollmentLink(string Bundle, string? Seal);

// The link carried a seal and the package does not hash to it. Its own type so the shell can
// say it in the person's words: this is not a broken zip, it is a package that changed.
internal sealed class PackageSealMismatchException(string message) : InvalidOperationException(message);

// The package is signed and the signature does not hold: a file changed, or the signature is
// not the key's. Its own type for the same reason: somebody touched it, and that is what to say.
internal sealed class PackageSignatureInvalidException(string message) : InvalidOperationException(message);

// The package is signed with a key its origin retired. The institution said so itself, over TLS;
// there is no "warning" reading of that, and the sheet is never shown.
internal sealed class PackageKeyRevokedException(string message) : InvalidOperationException(message);

// The origin CONTRADICTS the package: it publishes its modules and this one is not among them, or
// the package claims an origin other than the one that served it. Somebody spoke and said no, so
// it is refused in every mode; silence -no well-known, no origin declared- stays a warning until
// OriginIsRequired says otherwise (NotasGnothiHost/43 §8, the middle road, taken 2026-09-09).
internal sealed class PackageNotBackedException(OriginCheck origin, string message) : InvalidOperationException(message)
{
	public OriginCheck Origin { get; } = origin;
}

// The package carries the name of a module already installed, and it is not that module: another
// institution under the same name. Not an update, not a reinstall: refused before any sheet, so a
// stranger's package can never replace what she has (NotasGnothiHost/44 §2).
internal sealed class PackageCollisionException(string module, string installedInstitution, string incomingInstitution, string message) : InvalidOperationException(message)
{
	public string Module { get; } = module;
	public string InstalledInstitution { get; } = installedInstitution;
	public string IncomingInstitution { get; } = incomingInstitution;
}

internal sealed class ModuleEnrollment
{
	// A module's bundle weighs about 11 MB measured. This ceiling is not an optimisation: it is
	// so that a wrong URL cannot fill the device before anybody notices.
	private const long BundleCeiling = 64L * 1024 * 1024;

	// Where an origin says which modules are its own, and which keys sign them. A list, not one
	// file per module: an institution can publish several, and Gnothi looks for the one it holds.
	internal const string WellKnownPath = "/.well-known/gnothi/modules.json";
	private const long WellKnownCeiling = 64 * 1024;

	// Whether SILENCE from the origin -no well-known, no origin declared- refuses the module or is
	// only reported on the sheet. A contradiction is refused regardless (RefuseIfRequired). Flipping
	// this is the whole change to the full requirement (NotasGnothiHost/43).
	private static readonly bool OriginIsRequired = false;

	private static readonly HttpClient http = new() { Timeout = TimeSpan.FromMinutes(2) };

	private readonly string modulesRoot;
	private readonly string journalsRoot;
	private readonly Action<string> log;

	internal ModuleEnrollment(string modulesRoot, string journalsRoot, Action<string> log)
	{
		this.modulesRoot = modulesRoot;
		this.journalsRoot = journalsRoot;
		this.log = log;
	}

	internal static EnrollmentLink ParseLink(string link)
	{
		string trimmed = link.Trim();
		if (!Uri.TryCreate(trimmed, UriKind.Absolute, out Uri? uri)) return new EnrollmentLink(trimmed, null);

		string? bundle = null, seal = null;
		foreach (string pair in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
		{
			int eq = pair.IndexOf('=');
			if (eq <= 0) continue;

			string key = pair[..eq];
			string value = Uri.UnescapeDataString(pair[(eq + 1)..].Replace('+', ' '));
			if (value.Length == 0) continue;

			if (key.Equals("bundle", StringComparison.OrdinalIgnoreCase)) bundle = value;
			else if (key.Equals("h", StringComparison.OrdinalIgnoreCase)) seal = value;
		}

		// A seal only means something next to the bundle it was made for.
		return bundle is null ? new EnrollmentLink(trimmed, null) : new EnrollmentLink(bundle, seal);
	}

	// THE SEAL: the first 16 bytes of the SHA-256 of manifest.json exactly as it sits in the
	// zip, base64url without padding. PaginaDeLaInstitucion computes the same thing for its
	// link, over the same bytes; neither side normalises anything, so they agree byte for byte.
	internal static string SealOf(byte[] manifestBytes) => PackageSeal.Of(manifestBytes);

	// THE ORIGIN CHECK, over what was fetched: the bundle's address, what its origin publishes
	// (null when it publishes nothing readable) and the origin the manifest declares. Pure, so
	// the suite can try every outcome without a network.
	//
	// The order is the order of how bad it is: a manifest claiming an origin other than the one
	// that served it is the loudest signal, and it is reported before anything about the list.
	internal static OriginCheck VerifyOrigin(Uri bundle, string? wellKnownJson, string? declaredOrigin)
	{
		string served = OriginOf(bundle);
		string? declared = OriginOf(declaredOrigin);
		if (declared is not null && !declared.Equals(served, StringComparison.OrdinalIgnoreCase))
			return OriginCheck.DeclaresAnother;

		if (wellKnownJson is null) return OriginCheck.NoWellKnown;

		bool listed;
		try
		{
			using JsonDocument published = JsonDocument.Parse(wellKnownJson);
			listed = Modules(published).Any(m =>
				m.TryGetProperty("bundle", out JsonElement b) && b.ValueKind == JsonValueKind.String
				// Relative or absolute, resolved against the origin: an absolute address on
				// another origin resolves to that other origin and does not match.
				&& Uri.TryCreate(new Uri(served + "/"), b.GetString(), out Uri? listedBundle)
				&& SameResource(listedBundle, bundle));
		}
		catch (JsonException)
		{
			return OriginCheck.NoWellKnown;
		}

		if (!listed) return OriginCheck.NotListed;
		return declared is null ? OriginCheck.NotDeclared : OriginCheck.Verified;
	}

	// THE SAME QUESTION FOR A FILE, which has no address to look up: does the origin the manifest
	// claims publish exactly this manifest? The seal is the binding -the institution publishes it
	// next to each module- so a file that went through chat or mail is backed by its origin as
	// much as a download is, with the same residue: the person still judges the origin's name.
	internal static OriginCheck VerifyBySeal(string seal, string? wellKnownJson)
	{
		if (wellKnownJson is null) return OriginCheck.NoWellKnown;

		try
		{
			using JsonDocument published = JsonDocument.Parse(wellKnownJson);
			bool listed = Modules(published).Any(m =>
				m.TryGetProperty("seal", out JsonElement s) && s.ValueKind == JsonValueKind.String
				&& string.Equals(s.GetString(), seal, StringComparison.Ordinal));
			return listed ? OriginCheck.Verified : OriginCheck.NotListed;
		}
		catch (JsonException)
		{
			return OriginCheck.NoWellKnown;
		}
	}

	// WHOSE KEY. The signature already holds (or there is none); this decides what to tell the
	// person and what to remember. Pure: what the origin publishes, what Gnothi pinned, the clock.
	//
	//   published  the origin lists this public key           |  pinned  the key of last time
	//   sameKey    the pinned key is this one -continuity-     |  fresh   a new pin, from now
	internal static (SignerCheck check, PackageSigner? pin, bool sameKey) ResolveSigner(SignatureVerification signature, string? wellKnownJson, PackageSigner? pinned, DateTimeOffset now)
	{
		if (signature.State != SignatureState.Valid) return (SignerCheck.Unsigned, pinned, false);

		// Retired beats everything, the pin included: the institution said this key is no more.
		if (IsRevoked(wellKnownJson, signature.Public)) return (SignerCheck.Revoked, pinned, false);

		bool published = PublishedKeys(wellKnownJson).Contains(signature.Public, StringComparer.Ordinal);
		bool same = pinned is not null && string.Equals(pinned.Public, signature.Public, StringComparison.Ordinal);
		var fresh = new PackageSigner { KeyId = signature.KeyId, Public = signature.Public, Since = now.ToString("O") };

		if (same) return (published ? SignerCheck.Published : SignerCheck.Pinned, pinned, true);
		if (pinned is not null) return (published ? SignerCheck.Rotated : SignerCheck.Changed, fresh, false);
		return (published ? SignerCheck.Published : SignerCheck.Unknown, fresh, false);
	}

	private static IEnumerable<JsonElement> Modules(JsonDocument published)
		=> published.RootElement.TryGetProperty("modules", out JsonElement modules) && modules.ValueKind == JsonValueKind.Array
			? modules.EnumerateArray()
			: [];

	// The public keys an origin publishes, as base64url SPKI strings. Empty when it publishes
	// none, or nothing readable at all.
	private static List<string> PublishedKeys(string? wellKnownJson) => Keys(wellKnownJson, "keys");

	// Whether the origin lists this key among the ones it retired. Pure; the startup check and the
	// enrollment ask the same question.
	internal static bool IsRevoked(string? wellKnownJson, string publicKey)
		=> Keys(wellKnownJson, "revoked").Contains(publicKey, StringComparer.Ordinal);

	private static List<string> Keys(string? wellKnownJson, string list)
	{
		if (wellKnownJson is null) return [];
		try
		{
			using JsonDocument published = JsonDocument.Parse(wellKnownJson);
			if (!published.RootElement.TryGetProperty(list, out JsonElement keys) || keys.ValueKind != JsonValueKind.Array) return [];
			return [.. keys.EnumerateArray()
				.Where(k => k.TryGetProperty("public", out JsonElement p) && p.ValueKind == JsonValueKind.String)
				.Select(k => k.GetProperty("public").GetString()!)];
		}
		catch (JsonException)
		{
			return [];
		}
	}

	// The origin a manifest declares, as an address to ask -or null when it declares none.
	internal static Uri? DeclaredOrigin(string? declared)
		=> OriginOf(declared) is string origin ? new Uri(origin + "/") : null;

	// WHAT THE ORIGIN PUBLISHES FOR THIS MODULE, NEXT TO WHAT IS INSTALLED (NotasGnothiHost/44 §4).
	// The well-known lists each module with its bundle, seal and version. Nothing newer when the
	// module is not listed, when the published seal is the installed one, or when both sides
	// declare a version and the published one is not greater -an origin that rolled back is not
	// announced as news. Otherwise the answer names the version, the bundle to fetch -resolved
	// against the origin when the list gives a relative address- and the seal, dated now. Pure.
	internal static AvailableUpdate? UpdatePublished(string? wellKnownJson, string module, string? installedSeal, string? installedVersion, Uri origin, DateTimeOffset now)
	{
		if (wellKnownJson is null) return null;
		try
		{
			using JsonDocument published = JsonDocument.Parse(wellKnownJson);
			foreach (JsonElement m in Modules(published))
			{
				if (!m.TryGetProperty("module", out JsonElement name) || name.ValueKind != JsonValueKind.String
					|| !string.Equals(name.GetString(), module, StringComparison.Ordinal)) continue;

				string seal = m.TryGetProperty("seal", out JsonElement s) && s.ValueKind == JsonValueKind.String ? s.GetString() ?? "" : "";
				string version = m.TryGetProperty("version", out JsonElement v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";
				string bundle = m.TryGetProperty("bundle", out JsonElement b) && b.ValueKind == JsonValueKind.String ? b.GetString() ?? "" : "";
				if (seal.Length == 0 || bundle.Length == 0) return null;
				if (string.Equals(seal, installedSeal, StringComparison.Ordinal)) return null;

				Version? was = UpdatePlan.ParseVersion(installedVersion);
				Version? announced = UpdatePlan.ParseVersion(version);
				if (was is not null && announced is not null && announced.CompareTo(was) <= 0) return null;

				if (!Uri.TryCreate(origin, bundle, out Uri? where)) return null;
				return new AvailableUpdate { Version = version, Bundle = where.ToString(), Seal = seal, SeenAt = now.ToString("O") };
			}
			return null;
		}
		catch (JsonException)
		{
			return null;
		}
	}

	// The seal of the manifest installed in a module's folder, or null when it cannot be read.
	internal static string? InstalledSeal(string moduleDirectory)
	{
		try
		{
			string path = Path.Combine(moduleDirectory, "manifest.json");
			return File.Exists(path) ? PackageSeal.Of(File.ReadAllBytes(path)) : null;
		}
		catch (Exception) { return null; }
	}

	// WRITE THE NOTE -or clear it- in installation.json, touching nothing else: the switches, the
	// endpoint, the act and the pin stay as they are. False when the file already said the same
	// (the same version and seal noted, or nothing noted and nothing to note), so a launch that
	// finds nothing new rewrites nothing.
	internal static bool WriteUpdateNote(string installationPath, AvailableUpdate? update)
	{
		Installation installation = Installation.Read(installationPath);
		if (update is null ? installation.Update is null : update.SameAs(installation.Update)) return false;

		File.WriteAllText(installationPath, JsonSerializer.Serialize(installation with { Update = update }, HostJson.Legible.Installation));
		return true;
	}

	// MARK AN INSTALLED MODULE whose pinned key the origin retired: the date goes into the pin, the
	// rest of the file -switches included- stays exactly as the person left it. Returns the date
	// written, so the module in memory can take the same one; null when there was nothing to mark,
	// or it was already marked. Read and written through the Host's generated context, the same way
	// the consent switches are.
	internal static string? MarkRevoked(string installationPath, DateTimeOffset now)
	{
		Installation installation = Installation.Read(installationPath);
		if (installation.Signer is not { RevokedAt: null } signer) return null;

		string at = now.ToString("O");
		Installation marked = installation with { Signer = signer with { RevokedAt = at } };
		File.WriteAllText(installationPath, JsonSerializer.Serialize(marked, HostJson.Legible.Installation));
		return at;
	}

	// scheme://host[:port], lower case, default port dropped: what "the same origin" means.
	private static string OriginOf(Uri uri) => uri.GetLeftPart(UriPartial.Authority).ToLowerInvariant();

	// Null for "declares nothing": a missing key, an empty string, or text that is not an origin.
	// A manifest written before 2026-09-09 has no `origin` at all, and the generated reader hands
	// that back as null, not as the property's initializer.
	private static string? OriginOf(string? declared)
		=> !string.IsNullOrWhiteSpace(declared)
			&& Uri.TryCreate(declared.Trim(), UriKind.Absolute, out Uri? uri)
			&& (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
			? OriginOf(uri)
			: null;

	private static bool SameResource(Uri a, Uri b)
		=> Uri.Compare(a, b, UriComponents.SchemeAndServer | UriComponents.PathAndQuery, UriFormat.SafeUnescaped, StringComparison.OrdinalIgnoreCase) == 0;

	// ENROLLING IS TWO ACTS, AND THE PERSON DECIDES IN BETWEEN (specification 13 §7).
	//
	//     Examine   downloads the bundle, inspects it and reads its manifest. Installs NOTHING.
	//     Accept    only then does the module take up a place on the device.
	//
	// Why it is split: installing a module is the biggest act a person performs in Gnothi -- she
	// gives an institution a folder of hers and a registered endpoint -- and until 2026-09-04 it
	// was a SIDE EFFECT of tapping a link. For the assistant to see one query there is a switch
	// per tool; for the whole module to exist there was nothing.
	//
	// And downloading is not installing: to be able to tell the person WHO the institution is
	// and WHAT it offers, the manifest has to be read, and the manifest comes inside the zip. So
	// it is downloaded, inspected, unpacked into a temporary folder, shown, and installed only if
	// she accepts. If she says no, the temporary folder is deleted and nothing was left behind.
	internal async Task<EnrollmentProposal> Examine(string url, EnrollmentSource source, string? seal, CancellationToken ct, Action<EnrollmentProgress>? progress = null)
	{
		if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? where)
			|| (where.Scheme != Uri.UriSchemeHttps && where.Scheme != Uri.UriSchemeHttp))
			throw new ArgumentException($"the module's link is not a valid address: {url}");

		log($"alta: bajando el bundle de {where.Host}");
		string downloading = $"Bajando el módulo de {where.Host}…";
		progress?.Invoke(new EnrollmentProgress(downloading));

		byte[] zip = await Download(where, ct, (bytes, total) => progress?.Invoke(new EnrollmentProgress(downloading, bytes, total)));

		// What the origin says about itself, fetched on top of the bundle. Its absence is an
		// outcome the sheet reports, not an error that stops the enrollment.
		string? published = await FetchWellKnown(where, ct);

		EnrollmentProposal proposal = Examine(zip, source, where, seal, published, progress);
		log($"alta: respaldo de {where.Host}: {proposal.Provenance.Origin}; firma: {proposal.Provenance.Signer}");
		return proposal;
	}

	// THE SAME EXAMINATION FOR EVERY WAY IN. Bytes and where they came from come in; the
	// manifest is read, checked against the seal if the link carried one and against its origin,
	// the signature checked and its key resolved, and the package inspected BEFORE anything
	// touches the disk; only then is it unpacked into a temporary folder that lives until she
	// decides. `servedFrom` is null for bytes that came from no origin at all, and
	// `wellKnownJson` is what that origin publishes, if anything.
	internal EnrollmentProposal Examine(byte[] zip, EnrollmentSource source, Uri? servedFrom, string? expectedSeal = null, string? wellKnownJson = null, Action<EnrollmentProgress>? progress = null)
	{
		progress?.Invoke(new EnrollmentProgress("Revisando qué trae…"));
		(Manifest manifest, string seal, PackageInspection inspection, SignatureVerification signature) = Open(zip);

		if (expectedSeal is not null && !string.Equals(expectedSeal, seal, StringComparison.Ordinal))
			throw new PackageSealMismatchException(
				$"the link was made for a manifest sealed {expectedSeal}; the package served now seals {seal}");

		OriginCheck origin = servedFrom is null ? OriginCheck.NoOrigin : VerifyOrigin(servedFrom, wellKnownJson, manifest.Origin);
		RefuseIfRequired(origin, servedFrom?.Host ?? "", manifest.Origin ?? "", file: false);

		(Manifest? installed, string? installedSeal, Installation? prior) = ReadInstalled(manifest.Module);
		UpdatePlan update = PlanAgainstInstalled(installed, installedSeal, prior, manifest, seal, signature);

		(SignerCheck signer, PackageSigner? pin, bool sameKey) = ResolveSigner(signature, wellKnownJson, prior?.Signer, DateTimeOffset.UtcNow);
		RefuseIfRevoked(signer, signature);

		var provenance = new Provenance(source, servedFrom?.Host ?? "",
			MatchesLink: expectedSeal is not null,
			Origin: origin,
			Secure: servedFrom?.Scheme == Uri.UriSchemeHttps,
			DeclaredOrigin: manifest.Origin ?? "",
			Signer: signer,
			SignerId: signature.KeyId,
			SignedSince: sameKey ? pin!.Since : "");

		return Propose(zip, manifest, inspection, provenance, pin, update, prior);
	}

	// A FILE. No address was involved, so there is no origin to compare with and no seal from a
	// link; but the manifest may claim an origin, and that origin is asked whether it publishes
	// exactly this manifest -and which keys it signs with. `fetch` is how the well-known is
	// obtained: the network, or a stand-in for the suite.
	internal async Task<EnrollmentProposal> ExamineFile(byte[] zip, string fileName, CancellationToken ct, Action<EnrollmentProgress>? progress = null, Func<Uri, CancellationToken, Task<string?>>? fetch = null)
	{
		progress?.Invoke(new EnrollmentProgress("Revisando qué trae…"));
		(Manifest manifest, string seal, PackageInspection inspection, SignatureVerification signature) = Open(zip);

		OriginCheck origin = OriginCheck.NotDeclared;
		bool secure = false;
		string? published = null;
		if (OriginOf(manifest.Origin) is string declared)
		{
			var claimed = new Uri(declared + "/");
			secure = claimed.Scheme == Uri.UriSchemeHttps;
			progress?.Invoke(new EnrollmentProgress($"Preguntándole a {claimed.Host} si este paquete es suyo…"));

			published = await (fetch ?? FetchWellKnown)(claimed, ct);
			origin = VerifyBySeal(seal, published);
			log($"alta: archivo '{fileName}': {claimed.Host} dice {origin}");
		}
		RefuseIfRequired(origin, fileName, manifest.Origin ?? "", file: true);

		(Manifest? installed, string? installedSeal, Installation? prior) = ReadInstalled(manifest.Module);
		UpdatePlan update = PlanAgainstInstalled(installed, installedSeal, prior, manifest, seal, signature);

		(SignerCheck signer, PackageSigner? pin, bool sameKey) = ResolveSigner(signature, published, prior?.Signer, DateTimeOffset.UtcNow);
		RefuseIfRevoked(signer, signature);

		var provenance = new Provenance(EnrollmentSource.File, fileName,
			MatchesLink: false,
			Origin: origin,
			Secure: secure,
			DeclaredOrigin: manifest.Origin ?? "",
			Signer: signer,
			SignerId: signature.KeyId,
			SignedSince: sameKey ? pin!.Since : "");

		return Propose(zip, manifest, inspection, provenance, pin, update, prior);
	}

	// WHAT THIS PACKAGE IS NEXT TO WHAT IS INSTALLED UNDER ITS NAME (Gnothi.Host/UpdatePlan.cs):
	// the same, newer, older, or somebody else's. The last one is refused right here: another
	// institution's package must never get as far as a sheet that offers to replace hers.
	private static UpdatePlan PlanAgainstInstalled(Manifest? installed, string? installedSeal, Installation? prior, Manifest incoming, string seal, SignatureVerification signature)
	{
		UpdatePlan plan = UpdatePlan.Compare(installed, installedSeal, prior?.Signer, incoming, seal,
			signature.State == SignatureState.Valid ? signature.Public : null);
		if (plan.Kind == UpdateKind.Collision)
			throw new PackageCollisionException(incoming.Module, plan.InstalledInstitution, plan.IncomingInstitution,
				$"'{incoming.Module}' is already installed from '{plan.InstalledInstitution}'; this package says it is from '{plan.IncomingInstitution}'");
		return plan;
	}

	// THE MIDDLE ROAD (NotasGnothiHost/43 §8, decided 2026-09-09): a contradiction is refused in every
	// mode -the origin spoke and does not recognise the package, or the package claims an origin other
	// than the one that served it-; silence -nothing published, nothing declared- is described on the
	// sheet, and OriginIsRequired is the one line that turns it into a refusal too.
	private static void RefuseIfRequired(OriginCheck origin, string servedBy, string declaredOrigin, bool file)
	{
		switch (origin)
		{
			case OriginCheck.NotListed:
				throw new PackageNotBackedException(origin, file
					? $"the origin the package claims ({declaredOrigin}) does not publish this package"
					: $"the origin that served the package ({servedBy}) does not list it among its modules");
			case OriginCheck.DeclaresAnother:
				throw new PackageNotBackedException(origin, $"the package claims origin {declaredOrigin} but {servedBy} served it");
		}

		if (OriginIsRequired && origin != OriginCheck.Verified)
			throw new PackageNotBackedException(origin, $"the origin does not back the package ({origin})");
	}

	// Not a policy: the institution retired the key itself, and a retired key signs nothing that
	// gets installed.
	private static void RefuseIfRevoked(SignerCheck signer, SignatureVerification signature)
	{
		if (signer == SignerCheck.Revoked)
			throw new PackageKeyRevokedException($"the origin retired the key '{signature.KeyId}' ({PackageSignature.Fingerprint(signature.Public)}) this package is signed with");
	}

	// Read, inspect and check the signature, writing nothing. A signed package whose signature
	// does not hold stops here: whatever else it says, somebody touched it after it was signed.
	// A LOOK WITHOUT UNPACKING, for the list of what the person picked when she picked several: the
	// manifest, the inspection and whether it comes signed -or why it is not a module at all. Nothing
	// touches the disk and nobody is asked anything; the origin, the signature's owner and the pin
	// wait for the one she chooses, which goes through the whole examination like any other file.
	internal static PackagePeek Peek(byte[] zip, string fileName)
	{
		try
		{
			(Manifest manifest, _, PackageInspection inspection, SignatureVerification signature) = Open(zip);
			return new PackagePeek(fileName, zip.LongLength, manifest, inspection, signature.State, null);
		}
		catch (Exception ex)
		{
			return new PackagePeek(fileName, zip.LongLength, null, null, SignatureState.Unsigned, ex);
		}
	}

	private static (Manifest manifest, string seal, PackageInspection inspection, SignatureVerification signature) Open(byte[] zip)
	{
		using var archive = new ZipArchive(new MemoryStream(zip), ZipArchiveMode.Read);
		(Manifest manifest, string seal) = ReadManifest(archive);

		SignatureVerification signature = PackageSignature.Verify(archive);
		if (signature.State == SignatureState.Invalid)
			throw new PackageSignatureInvalidException("the package is signed and the signature does not hold: " + signature.Detail);

		return (manifest, seal, PackageInspector.Inspect(archive, manifest), signature);
	}

	// The key this module was accepted with last time, if it is installed and was signed.
	private PackageSigner? ReadPin(string module) => ReadInstalled(module).installation?.Signer;

	// WHAT IS INSTALLED UNDER THIS NAME, if anything: its manifest as the institution wrote it, the
	// seal of that manifest, and Gnothi's installation with the switches and the pin. Null when the
	// module is not there or its files cannot be read -then the incoming package is a first install
	// as far as the comparison goes, and the rewrite is what it always was.
	private (Manifest? manifest, string? seal, Installation? installation) ReadInstalled(string module)
	{
		string folder = Path.Combine(modulesRoot, module);
		if (!Directory.Exists(folder)) return (null, null, null);

		Manifest? manifest = null;
		string? seal = null;
		Installation? installation = null;
		try
		{
			string manifestPath = Path.Combine(folder, "manifest.json");
			if (File.Exists(manifestPath))
			{
				byte[] bytes = File.ReadAllBytes(manifestPath);
				manifest = JsonSerializer.Deserialize(bytes, HostJson.Default.Manifest);
				seal = PackageSeal.Of(bytes);
			}
		}
		catch (Exception) { /* unreadable manifest: compared as a first install */ }
		try
		{
			string installationPath = Path.Combine(folder, "installation.json");
			if (File.Exists(installationPath)) installation = Installation.Read(installationPath);
		}
		catch (Exception) { /* unreadable installation: nothing to keep */ }

		return (manifest, seal, installation);
	}

	// The proposal, once everything is known: a refused package never gets here to be shown, and
	// an accepted one is unpacked into a temporary folder that lives until she decides.
	private EnrollmentProposal Propose(byte[] zip, Manifest manifest, PackageInspection inspection, Provenance provenance, PackageSigner? signer, UpdatePlan update, Installation? prior)
	{
		// A refused package never reaches the sheet: there is nothing to decide about a package
		// that would not be installed anyway. The reasons are written to be read, and they are
		// what the person sees.
		if (inspection.Rejected)
			throw new InvalidOperationException("Gnothi rechazó el paquete: " + string.Join("; ", inspection.Rejections));

		string temporary = Path.Combine(Path.GetTempPath(), "alta-" + Guid.NewGuid().ToString("N")[..8]);
		try
		{
			Unpack(zip, temporary);

			string module = manifest.Module;
			return new EnrollmentProposal(temporary, manifest, provenance, inspection,
				Path.Combine(journalsRoot, module), Directory.Exists(Path.Combine(modulesRoot, module)), signer, update, prior);
		}
		catch
		{
			// A bundle that cannot even be unpacked does not leave rubbish waiting for a decision
			// that is never going to come.
			if (Directory.Exists(temporary)) Directory.Delete(temporary, recursive: true);
			throw;
		}
	}

	// THE PERSON SAID YES. Only here does the module take up a place: its folder, its journal,
	// its fixed endpoint -and the key it came signed with, remembered for next time.
	//
	// ON AN UPDATE (NotasGnothiHost/44 §3) what she decided before stays hers: the switches she set
	// are kept for the tools that survive, the ones that went are dropped, the new ones enter on
	// -she accepted the sheet that listed them-, and the publisher's endpoint Gnothi fixed the first
	// time does not move unless she said yes to the new one (`acceptNewPublisher`).
	//
	// AND THE REPLACEMENT IS SAFE (44 §3.4-3.6). It used to delete the installed folder and then
	// move the new one in: a failure between the two left an empty folder and nothing to go back
	// to. Now the installed version is MOVED ASIDE to `<module>.anterior` -whole, with its
	// manifest, installation and web- and the new one takes its place; if that second move fails
	// the previous one comes back and nothing happened. The previous version stays parked until
	// the next update, so that a new version that does not answer can be put back (Rollback). The
	// catalog knows to skip that folder. Before any of it, the journal is copied when the
	// institution did not say it can be rebuilt, and the module is closed if it is open: its
	// WebView is sitting on the folder about to move.
	internal Action<string>? BeforeReplacing { get; set; }

	internal string Accept(EnrollmentProposal proposal, bool acceptNewPublisher = false)
	{
		string module = proposal.Manifest.Module;
		string destination = Path.Combine(modulesRoot, module);
		string previous = destination + ModuleCatalog.PreviousSuffix;

		Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
		if (Directory.Exists(destination))
		{
			BeforeReplacing?.Invoke(module);
			KeepJournalCopy(module, proposal.Manifest);

			if (Directory.Exists(previous)) Directory.Delete(previous, recursive: true);
			Directory.Move(destination, previous);
			try { Directory.Move(proposal.Folder, destination); }
			catch
			{
				Directory.Move(previous, destination);
				throw;
			}
			log($"alta: '{module}': la versión anterior queda en {Path.GetFileName(previous)} hasta la próxima actualización");
		}
		else Directory.Move(proposal.Folder, destination);

		// The act records the HOST that served the bundle; a file came from no server, and there
		// the act says so with an empty string rather than the file's name.
		string servedBy = proposal.Provenance.Source == EnrollmentSource.File ? "" : proposal.Provenance.ServedBy;

		Installation? prior = proposal.Update.Kind == UpdateKind.Fresh ? null : proposal.Prior;
		bool enabled = proposal.Inspection.InstallEnabled && (prior?.Enabled ?? true);
		IReadOnlyList<string> disabledTools = prior is null ? []
			: [.. prior.DisabledTools.Where(t => proposal.Manifest.Tools.Any(x => string.Equals(x.Name, t, StringComparison.Ordinal)))];
		string? keptEndpoint = prior is not null && proposal.Update.PublisherChanged && !acceptNewPublisher && prior.Shell.Arguments.Count > 1
			? prior.Shell.Arguments[1] : null;

		WriteInstallation(destination, module, proposal.Manifest, enabled, disabledTools, keptEndpoint, proposal.Signer, servedBy, prior?.Acts ?? []);

		log($"alta: '{module}' instalado en {destination}");
		if (proposal.Update.Kind != UpdateKind.Fresh)
			log($"alta: '{module}': {proposal.Update.Describe()}" + (keptEndpoint is null ? "" : "; el publicador fijado se conserva"));
		return module;
	}

	// THE JOURNAL IS NEVER TOUCHED BY AN UPDATE, and still it gets a copy when the institution did
	// not say it can be rebuilt from the publisher (`journal.reconstruible` true). Null is "the
	// institution did not say", and that is not the same as "it can be rebuilt": a copy is cheap,
	// and it is the only net against a new version that misreads its own record. It sits next to
	// the journal as `<module>.anterior`, one copy, replaced at the next update. Nothing restores
	// it on its own: the new version may have appended events that a blind restore would lose.
	private void KeepJournalCopy(string module, Manifest manifest)
	{
		if (manifest.Journal?.Reconstruible == true) return;

		string journal = Path.Combine(journalsRoot, module);
		if (!Directory.Exists(journal) || !Directory.EnumerateFileSystemEntries(journal).Any()) return;

		string copy = journal + ModuleCatalog.PreviousSuffix;
		if (Directory.Exists(copy)) Directory.Delete(copy, recursive: true);
		CopyTree(journal, copy);
		log($"alta: '{module}': copia del registro en {Path.GetFileName(copy)} (la institución no dijo que se pueda reconstruir)");
	}

	private static void CopyTree(string from, string to)
	{
		Directory.CreateDirectory(to);
		foreach (string file in Directory.EnumerateFiles(from, "*", SearchOption.AllDirectories))
		{
			string target = Path.Combine(to, Path.GetRelativePath(from, file));
			Directory.CreateDirectory(Path.GetDirectoryName(target)!);
			File.Copy(file, target, overwrite: true);
		}
	}

	// THE NEW VERSION DID NOT ANSWER: PUT THE PREVIOUS ONE BACK (44 §3.5). Only possible while the
	// parked folder exists, which is until the next update. Static, because the check that finds
	// out runs at start-up too, when no enrollment is in flight. The journal copy is left where it
	// is: the folder that comes back reads the same journal it always did.
	internal static bool Rollback(string modulesRoot, string module, Action<string> log)
	{
		string destination = Path.Combine(modulesRoot, module);
		string previous = destination + ModuleCatalog.PreviousSuffix;
		if (!Directory.Exists(previous)) return false;

		if (Directory.Exists(destination)) Directory.Delete(destination, recursive: true);
		Directory.Move(previous, destination);
		log($"alta: '{module}': la versión nueva no contestó; volvió la anterior");
		return true;
	}

	internal static bool HasPrevious(string modulesRoot, string module) =>
		Directory.Exists(Path.Combine(modulesRoot, module) + ModuleCatalog.PreviousSuffix);

	// THE PERSON SAID NO, and then nothing is left: no folder, no journal, no entry in the
	// catalog. Downloading the bundle was not installing it.
	internal void Discard(EnrollmentProposal proposal)
	{
		if (Directory.Exists(proposal.Folder)) Directory.Delete(proposal.Folder, recursive: true);
		log($"alta: '{proposal.Manifest.Module}' descartado por la persona");
	}

	// `downloading` hears how far it got -bytes so far, and the total if the server announced one-
	// about every quarter megabyte and once at the end, which is enough for a bar and few enough
	// calls not to flood the bridge to the screen.
	private static async Task<byte[]> Download(Uri where, CancellationToken ct, Action<long, long?>? downloading = null)
	{
		using HttpResponseMessage response = await http.GetAsync(where, HttpCompletionOption.ResponseHeadersRead, ct);
		response.EnsureSuccessStatusCode();

		long? total = response.Content.Headers.ContentLength;
		if (total is long length && length > BundleCeiling)
			throw new InvalidOperationException($"the bundle weighs {length / 1048576} MB and the ceiling is {BundleCeiling / 1048576} MB");

		using var buffer = new MemoryStream();
		await using (Stream body = await response.Content.ReadAsStreamAsync(ct))
		{
			var chunk = new byte[81920];
			int read;
			long reportedAt = 0;
			while ((read = await body.ReadAsync(chunk, ct)) > 0)
			{
				buffer.Write(chunk, 0, read);

				// Content-Length can lie or not be there at all; the ceiling is enforced anyway
				// while it downloads.
				if (buffer.Length > BundleCeiling)
					throw new InvalidOperationException($"the bundle went past the {BundleCeiling / 1048576} MB ceiling while downloading");

				if (buffer.Length - reportedAt >= 256 * 1024)
				{
					reportedAt = buffer.Length;
					downloading?.Invoke(buffer.Length, total);
				}
			}
		}

		downloading?.Invoke(buffer.Length, total ?? buffer.Length);
		return buffer.ToArray();
	}

	// WHAT THE ORIGIN PUBLISHES, or null: a 404, a timeout, a body too big or a broken connection
	// all mean the same thing to the sheet -- the origin does not say which modules are its own --
	// and none of them stops the enrollment. Ten seconds, so a slow origin does not hold the
	// person hostage for a check that is a warning. `at` may be the bundle's address or the
	// origin itself: the well-known path is absolute either way.
	internal static async Task<string?> FetchWellKnown(Uri at, CancellationToken ct)
	{
		try
		{
			using var brief = CancellationTokenSource.CreateLinkedTokenSource(ct);
			brief.CancelAfter(TimeSpan.FromSeconds(10));

			using HttpResponseMessage response = await http.GetAsync(new Uri(at, WellKnownPath), HttpCompletionOption.ResponseHeadersRead, brief.Token);
			if (!response.IsSuccessStatusCode) return null;
			if (response.Content.Headers.ContentLength is long length && length > WellKnownCeiling) return null;

			using var buffer = new MemoryStream();
			await using (Stream body = await response.Content.ReadAsStreamAsync(brief.Token))
			{
				var chunk = new byte[8192];
				int read;
				while ((read = await body.ReadAsync(chunk, brief.Token)) > 0)
				{
					buffer.Write(chunk, 0, read);
					if (buffer.Length > WellKnownCeiling) return null;
				}
			}

			return Encoding.UTF8.GetString(buffer.ToArray());
		}
		catch (Exception e) when (e is HttpRequestException or OperationCanceledException or IOException)
		{
			return null;
		}
	}

	// ZIP SLIP: an entry in the zip can be called "../../something" and write outside where one
	// thinks. The inspection already refused a package that names such a path; this is the last
	// guard, against the real destination, and it stays. That is Gnothi's responsibility, not
	// trust in the institution.
	private static void Unpack(byte[] zip, string destination)
	{
		Directory.CreateDirectory(destination);
		string root = Path.GetFullPath(destination) + Path.DirectorySeparatorChar;

		using var stream = new MemoryStream(zip);
		using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

		foreach (ZipArchiveEntry entry in archive.Entries)
		{
			// The ZIP standard says forward slash, but Windows tools write backslashes and on
			// Android that does NOT separate folders: without normalising, the bundle unpacks as
			// files with slashes in their names. Measured with a zip made by Compress-Archive.
			string name = entry.FullName.Replace("\\", "/");
			if (name.EndsWith('/') || entry.Name.Length == 0) continue;

			string full = Path.GetFullPath(Path.Combine(destination, name.Replace('/', Path.DirectorySeparatorChar)));
			if (!full.StartsWith(root, StringComparison.Ordinal))
				throw new InvalidOperationException($"the bundle tries to write outside its folder: '{entry.FullName}'");

			Directory.CreateDirectory(Path.GetDirectoryName(full)!);
			entry.ExtractToFile(full, overwrite: true);
		}
	}

	// Read straight from the archive, before unpacking: the inspection needs the manifest, and
	// nothing should be written to disk for a package that is about to be refused. The raw bytes
	// are hashed for the seal BEFORE parsing -- the seal is over the file, not over a rendering
	// of it. Through the Host's generated JSON context, not reflection: this file also compiles
	// into the iOS app.
	private static (Manifest manifest, string seal) ReadManifest(ZipArchive archive)
	{
		ZipArchiveEntry? entry = archive.Entries.FirstOrDefault(e =>
			e.FullName.Replace('\\', '/').Equals("manifest.json", StringComparison.OrdinalIgnoreCase));
		if (entry is null)
			throw new InvalidOperationException("the bundle carries no manifest.json: it does not say who it is nor what it offers");

		byte[] bytes;
		using (var buffer = new MemoryStream())
		{
			using Stream content = entry.Open();
			content.CopyTo(buffer);
			bytes = buffer.ToArray();
		}

		try
		{
			Manifest? manifest = JsonSerializer.Deserialize(bytes, HostJson.Default.Manifest);
			if (manifest is null || string.IsNullOrWhiteSpace(manifest.Module))
				throw new InvalidOperationException("the bundle's manifest.json could not be read, or declares no 'module'");
			return (manifest, SealOf(bytes));
		}
		catch (JsonException e)
		{
			throw new InvalidOperationException($"the bundle's manifest.json is not valid json: {e.Message}");
		}
	}

	// THIS FILE BELONGS TO THE HOST, which is why Gnothi writes it and it does not come in the
	// bundle. Here is where it is decided which shell the module runs under, where it keeps its
	// journal, and against which endpoint the courier talks. The institution declared the
	// endpoint; choosing to fix it is Gnothi's.
	//
	// THE SWITCH comes from the inspection: a module that asks for capabilities a data module
	// does not need starts OFF, and the person switches it on once she has read why. Same rule
	// the desktop installer has had since the start; until 2026-09-08 the phone wrote `true`.
	//
	// THE PIN is the key the package came signed with -kept from last time when it is the same,
	// new when it is not- so the next package can be compared with it. Null when unsigned.
	//
	// THE ACT (provenance) is what Gnothi SAW: the host that served the bundle -empty for a file,
	// which came from no server-, when the person accepted, and which version. Written once.
	//
	// `keptEndpoint` is the publisher's endpoint fixed at an earlier install when the new manifest
	// moved it and the person did not accept the move: what Gnothi fixed stays fixed.
	//
	// `priorActs` are the acts of the versions accepted before: the list accumulates (44 §3.7),
	// oldest first, and `provenance` stays the latest one so that every reader of that field goes
	// on working.
	private void WriteInstallation(string folder, string module, Manifest manifest, bool enabled, IReadOnlyList<string> disabledTools, string? keptEndpoint, PackageSigner? signer, string servedBy, IReadOnlyList<Gnothi.Host.Provenance> priorActs)
	{
		Publicador? p = manifest.Publicador;
		string endpoint = keptEndpoint ?? (p is null || p.Eventos.Length == 0 ? "" : $"{p.Eventos}|{p.Recurso}|{p.Id}");

		// QUE CASCARA, Y SALE DE LO QUE EL PAQUETE TRAJO -- la misma regla que la aduana: lo que
		// trae es lo que es. Hasta acá el teléfono escribía "webview" quemado, y no era un
		// descuido: en un teléfono no había otra forma posible. El híbrido agrega una.
		bool traeCara = Directory.Exists(Path.Combine(folder, "web"));
		bool traeSuperficie = manifest.Load is not null && !string.IsNullOrWhiteSpace(manifest.Load.Entry);

		string tipo = (traeSuperficie, traeCara) switch
		{
			(true, true) => "hybrid",
			(true, false) => "in-process",
			_ => "webview",
		};

		// EL ORDEN DE LOS ARGUMENTOS, y en el híbrido no es el que uno escribiría primero.
		//
		// La tentación es poner el dll en [0] porque es lo que contesta. Pero [0] y [1] ya los
		// lee TODO el lado web -- WebViewShell y, sobre todo, WebModuleHost.Open, que abre la
		// pantalla desde el catálogo-- y ninguno de los dos sabe de formas. Poner el dll ahí
		// haría que abrir el módulo intentara servir un .dll como si fuera la carpeta del
		// bundle, y fallaría lejos de acá.
		//
		// Así que la cara conserva su lugar y la superficie va al final: es el consumidor nuevo
		// el que se acomoda, no los que ya andaban.
		string dll = traeSuperficie
			? Path.Combine(folder, manifest.Load!.Entry.Replace('/', Path.DirectorySeparatorChar))
			: "";
		string web = Path.Combine(folder, "web");

		string[] argumentos = tipo switch
		{
			"hybrid" => [web, endpoint, dll],
			"in-process" => [dll],
			_ => [web, endpoint],
		};

		// EL ACTA DE ESTA ALTA. Se perdio en la resolucion del merge 64bee72 -- la linea que la
		// usa quedo, la declaracion no, y la rama no compilaba. Repuesta tal cual estaba en
		// 85551e1.
		var act = new
		{
			servedBy,
			at = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
			version = manifest.Publisher?.Version ?? "",
		};

		var installation = new
		{
			shell = new
			{
				type = tipo,
				command = "",
				arguments = argumentos,
			},
			grantedFolder = Path.Combine(journalsRoot, module),
			enabled,
			disabledTools = disabledTools.ToArray(),

			// EL ACTA DEL ALTA. De que host bajo el bundle -- que Gnothi VIO, nadie se lo
			// declaro --, cuando la persona acepto, y que version acepto. Se escribe una vez y
			// no se vuelve a tocar: es lo que le permite al detalle del modulo contrastar
			// despues lo que la institucion declara contra lo que de verdad paso.
			provenance = act,

			// Y EL ACTA SE ACUMULA: las anteriores tal como quedaron escritas, y esta al final.
			acts = priorActs.Select(a => new { servedBy = a.ServedBy, at = a.At, version = a.Version }).Append(act).ToArray(),
			signer = signer is null ? null : new { keyId = signer.KeyId, @public = signer.Public, since = signer.Since },
		};

		File.WriteAllText(Path.Combine(folder, "installation.json"),
			JsonSerializer.Serialize(installation, new JsonSerializerOptions { WriteIndented = true }));
	}
}

// WHAT THE SCREEN IS TOLD WHILE AN ENROLLMENT WORKS: a line in the person's words and, while the
// bundle downloads, how far it got -the bytes so far and the total when the server announced it-
// so the screen can draw a bar and say "4,1 de 12,6 MB" instead of spinning in silence over
// twelve megabytes. Fraction is null before the total is known and for the steps that are not
// a download.
public sealed record EnrollmentProgress(string Text, long Bytes = 0, long? Total = null)
{
	public double? Fraction => Total is > 0 ? Math.Min(1, (double)Bytes / Total.Value) : null;
}

// WHAT A QUICK LOOK AT A PICKED FILE FOUND, for the list the person chooses from. Manifest and
// Inspection are null when the file is not a module at all, and Problem says why; a module the
// inspection rejects keeps its manifest -the list can name it- and its rejections.
internal sealed record PackagePeek(string FileName, long Bytes, Manifest? Manifest, PackageInspection? Inspection, SignatureState Signature, Exception? Problem)
{
	internal bool IsModule => Manifest is not null;
	internal bool Installable => Manifest is not null && Inspection is { Rejected: false };
}

// WHAT GNOTHI SHOWS THE PERSON BEFORE INSTALLING ANYTHING. It comes out of the bundle's
// manifest -- that is, out of what the institution DECLARES about itself -- plus what Gnothi
// found out by itself and she has a right to know before accepting: where the bytes came from,
// whether they match the link and whether their origin backs them, who signed them, what the
// inspection saw, where the journal will live, and against which endpoint the courier will talk.
//
// `Folder` is the temporary place where the bundle is already unpacked. It exists until she
// decides: Accept moves it where it belongs, Discard deletes it. `Signer` is the pin Accept
// writes: the key to remember, or null for an unsigned package.
internal sealed record EnrollmentProposal(
	string Folder,
	Manifest Manifest,
	Provenance Provenance,
	PackageInspection Inspection,
	string JournalGoesTo,
	bool AlreadyThere,
	PackageSigner? Signer = null,
	// What this package is next to what is installed under its name, and the installation it
	// would replace -the switches and the endpoint to keep. Fresh, with no prior, the first time.
	UpdatePlan? Update = null,
	Installation? Prior = null)
{
	public UpdatePlan Update { get; init; } = Update ?? UpdatePlan.Fresh(Manifest);

	// The host that answered the download -or the file's name-, for the log line the test scripts grep.
	internal string ServedBy => Provenance.ServedBy;

	// The publisher's endpoint, as it would end up fixed. It is said on screen because it is the
	// only network that module will ever be able to touch -- by Gnothi's hand, not its own.
	internal string Publisher
	{
		get
		{
			Publicador? p = Manifest.Publicador;
			return p is null || p.Eventos.Length == 0 ? "" : $"{p.Eventos}{p.Recurso}/{p.Id}";
		}
	}
}
