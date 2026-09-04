using System.IO.Compression;
using System.Text.Json;

namespace Gnothi.Movil;

// ENROLLING A MODULE FROM A LINK. This is how a person really gets a module: the institution
// puts a button on its page, Sonia taps it, and Gnothi downloads the bundle and installs it
// (specification 11 §3, 13 §7). Until this existed modules only got in by pushing files with
// adb, which is not a path that exists for anybody.
//
// THE BUNDLE IS A ZIP and carries two things: the institution's `manifest.json` and the `web/`
// folder with its published app. Nothing else. No executable, no installation: the
// installation is written by GNOTHI, because it is Gnothi's file and Gnothi's authority.
//
// WHAT GNOTHI DECIDES AND THE INSTITUTION DOES NOT: where the journal is kept, which shell the
// module runs under, and that the publisher's endpoint is fixed. The manifest DECLARES where
// its publisher is; Gnothi registers it once and from then on the module cannot change it.
//
// NOTHING HERE IS PLATFORM-SPECIFIC and that is not an accident: downloading a zip, refusing a
// zip-slip, reading a manifest and writing an installation are the same on any phone. It lived
// in the Android project until iOS needed it, and moving it cost one parameter -- where the log
// goes.
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

	// ENROLLING IS TWO ACTS, AND THE PERSON DECIDES IN BETWEEN (specification 13 §7).
	//
	//     Examine   downloads the bundle and reads its manifest. Installs NOTHING.
	//     Accept    only then does the module take up a place on the device.
	//
	// Why it is split: installing a module is the biggest act a person performs in Gnothi -- she
	// gives an institution a folder of hers and a registered endpoint -- and until 2026-09-04 it
	// was a SIDE EFFECT of tapping a link. For the assistant to see one query there is a switch
	// per tool; for the whole module to exist there was nothing.
	//
	// And downloading is not installing: to be able to tell the person WHO the institution is
	// and WHAT it offers, the manifest has to be read, and the manifest comes inside the zip. So
	// it is downloaded, unpacked into a temporary folder, shown, and installed only if she
	// accepts. If she says no, the temporary folder is deleted and nothing was left behind.
	internal async Task<EnrollmentProposal> Examine(string url, CancellationToken ct, Action<string>? progress = null)
	{
		if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? where))
			throw new ArgumentException($"the module's link is not a valid address: {url}");

		log($"alta: bajando el bundle de {where.Host}");
		progress?.Invoke($"Bajando el módulo de {where.Host}…");

		byte[] zip = await Download(where, ct);
		string temporary = Path.Combine(Path.GetTempPath(), "alta-" + Guid.NewGuid().ToString("N")[..8]);

		try
		{
			progress?.Invoke("Revisando qué trae…");
			Unpack(zip, temporary);

			Gnothi.Host.Manifest manifest = ReadManifest(temporary);
			string module = manifest.Module;

			// The institution names its module, and that name is a path: it cannot escape the
			// modules folder nor step on anything else.
			if (module.Length == 0 || module.Contains('/') || module.Contains('\\') || module.Contains(".."))
				throw new InvalidOperationException($"the manifest declares an invalid module name: '{module}'");

			return new EnrollmentProposal(temporary, manifest, where.Host,
				Path.Combine(journalsRoot, module), Directory.Exists(Path.Combine(modulesRoot, module)));
		}
		catch
		{
			// A bundle that cannot even be read does not leave rubbish waiting for a decision
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

		WriteInstallation(destination, module, proposal.Manifest);

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
	// thinks. Every path is resolved and required to land inside. That is Gnothi's
	// responsibility, not trust in the institution.
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

	private static Gnothi.Host.Manifest ReadManifest(string folder)
	{
		string path = Path.Combine(folder, "manifest.json");
		if (!File.Exists(path))
			throw new InvalidOperationException("the bundle carries no manifest.json: it does not say who it is nor what it offers");

		Gnothi.Host.Manifest? manifest = JsonSerializer.Deserialize<Gnothi.Host.Manifest>(File.ReadAllText(path));
		return manifest ?? throw new InvalidOperationException("the bundle's manifest.json could not be read");
	}

	// THIS FILE BELONGS TO THE HOST, which is why Gnothi writes it and it does not come in the
	// bundle. Here is where it is decided which shell the module runs under, where it keeps its
	// journal, and against which endpoint the courier talks. The institution declared the
	// endpoint; choosing to fix it is Gnothi's.
	private void WriteInstallation(string folder, string module, Gnothi.Host.Manifest manifest)
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
			enabled = true,
			disabledTools = Array.Empty<string>(),
		};

		File.WriteAllText(Path.Combine(folder, "installation.json"),
			JsonSerializer.Serialize(installation, new JsonSerializerOptions { WriteIndented = true }));
	}
}

// WHAT GNOTHI SHOWS THE PERSON BEFORE INSTALLING ANYTHING. It comes out of the bundle's
// manifest -- that is, out of what the institution DECLARES about itself -- plus the two things
// Gnothi decides and she has a right to know before accepting: where the journal will live, and
// against which endpoint the courier will talk.
//
// `Folder` is the temporary place where the bundle is already unpacked. It exists until she
// decides: Accept moves it where it belongs, Discard deletes it.
internal sealed record EnrollmentProposal(
	string Folder,
	Gnothi.Host.Manifest Manifest,
	string ServedBy,
	string JournalGoesTo,
	bool AlreadyThere)
{
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
