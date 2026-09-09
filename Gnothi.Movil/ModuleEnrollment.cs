using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

namespace Gnothi.Movil;

// ENROLLING A MODULE. This is how a person really gets a module: the institution puts a button
// and a code on its page, Sonia taps the button -or pastes the link, or points Gnothi's camera
// at the code- and Gnothi downloads the bundle, INSPECTS it, shows her what it found, and
// installs it only if she says yes (specification 11 §3, 13 §7, 13 §10.4). Until this existed
// modules only got in by pushing files with adb, which is not a path that exists for anybody.
//
// THE BUNDLE IS A ZIP and carries two things: the institution's `manifest.json` and the `web/`
// folder with its published app. Nothing else. No executable, no installation: the
// installation is written by GNOTHI, because it is Gnothi's file and Gnothi's authority.
//
// WHAT GNOTHI DECIDES AND THE INSTITUTION DOES NOT: where the journal is kept, which shell the
// module runs under, and that the publisher's endpoint is fixed. The manifest DECLARES where
// its publisher is; Gnothi registers it once and from then on the module cannot change it.
//
// WHERE THE BYTES CAME FROM IS RECORDED, NOT ASSUMED. Every way in -the institution's link, a
// pasted link, a scanned code, later a file- ends in the same Examine over the same bytes,
// carrying a Provenance that says which way it was and who served it. The sheet reads it: a
// package that arrived from a link has a host to name; one that arrives as a file will not, and
// the sheet has to say so instead of pretending.
//
// THE SEAL. A link can carry the hash of the manifest it was made for (&h=). When it does, a
// package whose manifest no longer hashes to it is refused: whoever made the link -or printed
// the code- fixed WHICH package it points at, and a swap on the server afterwards is caught.
// That is integrity, not authenticity: the one who makes the link chooses both the address and
// the seal, so a forged code is not caught by this. The sheet says the first and not the second.
//
// WHAT GNOTHI CHECKS BEFORE SHOWING ANYTHING is PackageInspector -the same inspection the
// desktop installer runs (Gnothi.Host/PackageInspector.cs): the package's form, whether it
// carries an engine, executables or a sample journal, and which capabilities it asks for. It
// runs over the zip BEFORE a byte is written to disk, and its result travels in the proposal so
// the sheet can say what was checked. What it does NOT check is who stands behind the package:
// that is the origin and signature work, not done yet, and the sheet says that too.
//
// NOTHING HERE IS PLATFORM-SPECIFIC and that is not an accident: downloading a zip, refusing a
// zip-slip, reading a manifest and writing an installation are the same on any phone. It lived
// in the Android project until iOS needed it, and moving it cost one parameter -- where the log
// goes.

// HOW THE BUNDLE REACHED GNOTHI. The next way in -a file shared to Gnothi- adds its value here,
// and nothing downstream has to change to carry it.
public enum EnrollmentSource
{
	// The institution's button: an App Link (Android) or universal link (iOS) landed on Gnothi.
	Link,

	// The person pasted the institution's link into Gnothi herself.
	PastedLink,

	// The person pointed Gnothi's camera at the institution's code.
	Qr,
}

// WHERE THE BYTES CAME FROM. ServedBy is the host that answered the download: a fact about
// transport, not a verified identity, and the sheet says exactly that. MatchesLink is true when
// the link carried a seal and the package hashed to it -- integrity against the link, nothing
// more; false when the link carried none.
public sealed record Provenance(EnrollmentSource Source, string ServedBy, bool MatchesLink = false);

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

internal sealed class ModuleEnrollment
{
	// A module's bundle weighs about 11 MB measured. This ceiling is not an optimisation: it is
	// so that a wrong URL cannot fill the device before anybody notices.
	private const long BundleCeiling = 64L * 1024 * 1024;

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
		return Examine(zip, source, where.Host, seal, progress);
	}

	// THE SAME EXAMINATION FOR EVERY WAY IN. Bytes and where they came from come in; the
	// manifest is read, checked against the seal if the link carried one, and the package
	// inspected BEFORE anything touches the disk; only then is it unpacked into a temporary
	// folder that lives until she decides.
	internal EnrollmentProposal Examine(byte[] zip, EnrollmentSource source, string servedBy, string? expectedSeal = null, Action<string>? progress = null)
	{
		progress?.Invoke("Revisando qué trae…");

		Gnothi.Host.Manifest manifest;
		string seal;
		Gnothi.Host.PackageInspection inspection;
		using (var archive = new ZipArchive(new MemoryStream(zip), ZipArchiveMode.Read))
		{
			(manifest, seal) = ReadManifest(archive);
			inspection = Gnothi.Host.PackageInspector.Inspect(archive, manifest);
		}

		if (expectedSeal is not null && !string.Equals(expectedSeal, seal, StringComparison.Ordinal))
			throw new PackageSealMismatchException(
				$"the link was made for a manifest sealed {expectedSeal}; the package served now seals {seal}");

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
			return new EnrollmentProposal(temporary, manifest,
				new Provenance(source, servedBy, MatchesLink: expectedSeal is not null),
				inspection, Path.Combine(journalsRoot, module), Directory.Exists(Path.Combine(modulesRoot, module)));
		}
		catch
		{
			// A bundle that cannot even be unpacked does not leave rubbish waiting for a decision
			// that is never going to come.
			if (Directory.Exists(temporary)) Directory.Delete(temporary, recursive: true);
			throw;
		}
	}

	// THE PERSON SAID YES. Only here does the module take up a place: its folder, its journal
	// and its fixed endpoint.
	internal string Accept(EnrollmentProposal proposal)
	{
		string module = proposal.Manifest.Module;
		string destination = Path.Combine(modulesRoot, module);

		if (Directory.Exists(destination)) Directory.Delete(destination, recursive: true);
		Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
		Directory.Move(proposal.Folder, destination);

		WriteInstallation(destination, module, proposal.Manifest, proposal.Inspection.InstallEnabled);

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
	private static (Gnothi.Host.Manifest manifest, string seal) ReadManifest(ZipArchive archive)
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
			Gnothi.Host.Manifest? manifest = JsonSerializer.Deserialize(bytes, Gnothi.Host.HostJson.Default.Manifest);
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
	private void WriteInstallation(string folder, string module, Gnothi.Host.Manifest manifest, bool enabled)
	{
		Gnothi.Host.Publicador? p = manifest.Publicador;
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
		};

		File.WriteAllText(Path.Combine(folder, "installation.json"),
			JsonSerializer.Serialize(installation, new JsonSerializerOptions { WriteIndented = true }));
	}
}

// WHAT GNOTHI SHOWS THE PERSON BEFORE INSTALLING ANYTHING. It comes out of the bundle's
// manifest -- that is, out of what the institution DECLARES about itself -- plus what Gnothi
// found out by itself and she has a right to know before accepting: where the bytes came from,
// whether they match the link, what the inspection saw, where the journal will live, and
// against which endpoint the courier will talk.
//
// `Folder` is the temporary place where the bundle is already unpacked. It exists until she
// decides: Accept moves it where it belongs, Discard deletes it.
internal sealed record EnrollmentProposal(
	string Folder,
	Gnothi.Host.Manifest Manifest,
	Provenance Provenance,
	Gnothi.Host.PackageInspection Inspection,
	string JournalGoesTo,
	bool AlreadyThere)
{
	// The host that answered the download, for the log line the test scripts grep.
	internal string ServedBy => Provenance.ServedBy;

	// The publisher's endpoint, as it would end up fixed. It is said on screen because it is the
	// only network that module will ever be able to touch -- by Gnothi's hand, not its own.
	internal string Publisher
	{
		get
		{
			Gnothi.Host.Publicador? p = Manifest.Publicador;
			return p is null || p.Eventos.Length == 0 ? "" : $"{p.Eventos}{p.Recurso}/{p.Id}";
		}
	}
}
