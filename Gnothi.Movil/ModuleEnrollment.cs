using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Gnothi.Host;

namespace Gnothi.Movil;

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
// the same TLS. Nothing is refused for it yet: whether the backing is a requirement or a warning
// is a decision still open (NotasGnothiHost/42 §9), and it lives in ONE place -OriginIsRequired.
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

internal sealed class ModuleEnrollment
{
	// A module's bundle weighs about 11 MB measured. This ceiling is not an optimisation: it is
	// so that a wrong URL cannot fill the device before anybody notices.
	private const long BundleCeiling = 64L * 1024 * 1024;

	// Where an origin says which modules are its own, and which keys sign them. A list, not one
	// file per module: an institution can publish several, and Gnothi looks for the one it holds.
	internal const string WellKnownPath = "/.well-known/gnothi/modules.json";
	private const long WellKnownCeiling = 64 * 1024;

	// Whether a module its origin does not back is refused, or only reported on the sheet. A
	// warning until the decision is taken (NotasGnothiHost/42 §9); flipping this is the change.
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
	internal static string SealOf(byte[] manifestBytes)
		=> Convert.ToBase64String(SHA256.HashData(manifestBytes), 0, 16).TrimEnd('=').Replace('+', '-').Replace('/', '_');

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
	private static List<string> PublishedKeys(string? wellKnownJson)
	{
		if (wellKnownJson is null) return [];
		try
		{
			using JsonDocument published = JsonDocument.Parse(wellKnownJson);
			if (!published.RootElement.TryGetProperty("keys", out JsonElement keys) || keys.ValueKind != JsonValueKind.Array) return [];
			return [.. keys.EnumerateArray()
				.Where(k => k.TryGetProperty("public", out JsonElement p) && p.ValueKind == JsonValueKind.String)
				.Select(k => k.GetProperty("public").GetString()!)];
		}
		catch (JsonException)
		{
			return [];
		}
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
	internal async Task<EnrollmentProposal> Examine(string url, EnrollmentSource source, string? seal, CancellationToken ct, Action<string>? progress = null)
	{
		if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? where)
			|| (where.Scheme != Uri.UriSchemeHttps && where.Scheme != Uri.UriSchemeHttp))
			throw new ArgumentException($"the module's link is not a valid address: {url}");

		log($"alta: bajando el bundle de {where.Host}");
		progress?.Invoke($"Bajando el módulo de {where.Host}…");

		byte[] zip = await Download(where, ct);

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
	internal EnrollmentProposal Examine(byte[] zip, EnrollmentSource source, Uri? servedFrom, string? expectedSeal = null, string? wellKnownJson = null, Action<string>? progress = null)
	{
		progress?.Invoke("Revisando qué trae…");
		(Manifest manifest, string seal, PackageInspection inspection, SignatureVerification signature) = Open(zip);

		if (expectedSeal is not null && !string.Equals(expectedSeal, seal, StringComparison.Ordinal))
			throw new PackageSealMismatchException(
				$"the link was made for a manifest sealed {expectedSeal}; the package served now seals {seal}");

		OriginCheck origin = servedFrom is null ? OriginCheck.NoOrigin : VerifyOrigin(servedFrom, wellKnownJson, manifest.Origin);
		RefuseIfRequired(origin);

		(SignerCheck signer, PackageSigner? pin, bool sameKey) = ResolveSigner(signature, wellKnownJson, ReadPin(manifest.Module), DateTimeOffset.UtcNow);

		var provenance = new Provenance(source, servedFrom?.Host ?? "",
			MatchesLink: expectedSeal is not null,
			Origin: origin,
			Secure: servedFrom?.Scheme == Uri.UriSchemeHttps,
			DeclaredOrigin: manifest.Origin ?? "",
			Signer: signer,
			SignerId: signature.KeyId,
			SignedSince: sameKey ? pin!.Since : "");

		return Propose(zip, manifest, inspection, provenance, pin);
	}

	// A FILE. No address was involved, so there is no origin to compare with and no seal from a
	// link; but the manifest may claim an origin, and that origin is asked whether it publishes
	// exactly this manifest -and which keys it signs with. `fetch` is how the well-known is
	// obtained: the network, or a stand-in for the suite.
	internal async Task<EnrollmentProposal> ExamineFile(byte[] zip, string fileName, CancellationToken ct, Action<string>? progress = null, Func<Uri, CancellationToken, Task<string?>>? fetch = null)
	{
		progress?.Invoke("Revisando qué trae…");
		(Manifest manifest, string seal, PackageInspection inspection, SignatureVerification signature) = Open(zip);

		OriginCheck origin = OriginCheck.NotDeclared;
		bool secure = false;
		string? published = null;
		if (OriginOf(manifest.Origin) is string declared)
		{
			var claimed = new Uri(declared + "/");
			secure = claimed.Scheme == Uri.UriSchemeHttps;
			progress?.Invoke($"Preguntándole a {claimed.Host} si este paquete es suyo…");

			published = await (fetch ?? FetchWellKnown)(claimed, ct);
			origin = VerifyBySeal(seal, published);
			log($"alta: archivo '{fileName}': {claimed.Host} dice {origin}");
		}
		RefuseIfRequired(origin);

		(SignerCheck signer, PackageSigner? pin, bool sameKey) = ResolveSigner(signature, published, ReadPin(manifest.Module), DateTimeOffset.UtcNow);

		var provenance = new Provenance(EnrollmentSource.File, fileName,
			MatchesLink: false,
			Origin: origin,
			Secure: secure,
			DeclaredOrigin: manifest.Origin ?? "",
			Signer: signer,
			SignerId: signature.KeyId,
			SignedSince: sameKey ? pin!.Since : "");

		return Propose(zip, manifest, inspection, provenance, pin);
	}

	private static void RefuseIfRequired(OriginCheck origin)
	{
		if (OriginIsRequired && origin != OriginCheck.Verified)
			throw new InvalidOperationException($"Gnothi rechazó el paquete: su origen no lo respalda ({origin})");
	}

	// Read, inspect and check the signature, writing nothing. A signed package whose signature
	// does not hold stops here: whatever else it says, somebody touched it after it was signed.
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
	private PackageSigner? ReadPin(string module)
	{
		string path = Path.Combine(modulesRoot, module, "installation.json");
		if (!File.Exists(path)) return null;
		try { return Installation.Read(path).Signer; }
		catch (Exception) { return null; }
	}

	// The proposal, once everything is known: a refused package never gets here to be shown, and
	// an accepted one is unpacked into a temporary folder that lives until she decides.
	private EnrollmentProposal Propose(byte[] zip, Manifest manifest, PackageInspection inspection, Provenance provenance, PackageSigner? signer)
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
				Path.Combine(journalsRoot, module), Directory.Exists(Path.Combine(modulesRoot, module)), signer);
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
	internal string Accept(EnrollmentProposal proposal)
	{
		string module = proposal.Manifest.Module;
		string destination = Path.Combine(modulesRoot, module);

		if (Directory.Exists(destination)) Directory.Delete(destination, recursive: true);
		Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
		Directory.Move(proposal.Folder, destination);

		WriteInstallation(destination, module, proposal.Manifest, proposal.Inspection.InstallEnabled, proposal.Signer);

		log($"alta: '{module}' instalado en {destination}");
		return module;
	}

	// THE PERSON SAID NO, and then nothing is left: no folder, no journal, no entry in the
	// catalog. Downloading the bundle was not installing it.
	internal void Discard(EnrollmentProposal proposal)
	{
		if (Directory.Exists(proposal.Folder)) Directory.Delete(proposal.Folder, recursive: true);
		log($"alta: '{proposal.Manifest.Module}' descartado por la persona");
	}

	private static async Task<byte[]> Download(Uri where, CancellationToken ct)
	{
		using HttpResponseMessage response = await http.GetAsync(where, HttpCompletionOption.ResponseHeadersRead, ct);
		response.EnsureSuccessStatusCode();

		if (response.Content.Headers.ContentLength is long length && length > BundleCeiling)
			throw new InvalidOperationException($"the bundle weighs {length / 1048576} MB and the ceiling is {BundleCeiling / 1048576} MB");

		using var buffer = new MemoryStream();
		await using (Stream body = await response.Content.ReadAsStreamAsync(ct))
		{
			var chunk = new byte[81920];
			int read;
			while ((read = await body.ReadAsync(chunk, ct)) > 0)
			{
				buffer.Write(chunk, 0, read);

				// Content-Length can lie or not be there at all; the ceiling is enforced anyway
				// while it downloads.
				if (buffer.Length > BundleCeiling)
					throw new InvalidOperationException($"the bundle went past the {BundleCeiling / 1048576} MB ceiling while downloading");
			}
		}

		return buffer.ToArray();
	}

	// WHAT THE ORIGIN PUBLISHES, or null: a 404, a timeout, a body too big or a broken connection
	// all mean the same thing to the sheet -- the origin does not say which modules are its own --
	// and none of them stops the enrollment. Ten seconds, so a slow origin does not hold the
	// person hostage for a check that is a warning. `at` may be the bundle's address or the
	// origin itself: the well-known path is absolute either way.
	private static async Task<string?> FetchWellKnown(Uri at, CancellationToken ct)
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
	private void WriteInstallation(string folder, string module, Manifest manifest, bool enabled, PackageSigner? signer)
	{
		Publicador? p = manifest.Publicador;
		string endpoint = p is null || p.Eventos.Length == 0 ? "" : $"{p.Eventos}|{p.Recurso}|{p.Id}";

		var installation = new
		{
			shell = new
			{
				type = "webview",
				command = "",
				arguments = new[] { Path.Combine(folder, "web"), endpoint },
			},
			grantedFolder = Path.Combine(journalsRoot, module),
			enabled,
			disabledTools = Array.Empty<string>(),
			signer = signer is null ? null : new { keyId = signer.KeyId, @public = signer.Public, since = signer.Since },
		};

		File.WriteAllText(Path.Combine(folder, "installation.json"),
			JsonSerializer.Serialize(installation, new JsonSerializerOptions { WriteIndented = true }));
	}
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
	PackageSigner? Signer = null)
{
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
