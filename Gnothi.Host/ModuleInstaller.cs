using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gnothi.Host;

// PACK AND INSTALL A CARGO MODULE. The two halves of the .gmod format live together so they
// cannot drift.
//
//   pack:    a source folder { manifest.json, view.json?, dominio/*.dll }  ->  <name>.gmod (a zip)
//   install: <name>.gmod  ->  Modulos/<name>/ + a granted data folder + installation.json
//
// A .gmod is just a zip. The clean cargo shape is two small DLLs plus the manifest and the
// view; the engine is NOT in it -the Host lends it. Both sides check that: pack refuses to
// build a package that carries the engine, and install refuses to accept one, because the
// guarantee cannot depend only on the publisher's tool.
//
// PUBLIC because installing is one of the Host's authorities and the desktop window invokes it
// (the person imports a module from the UI). That is decision B1: the window may write
// installation.json -through this one path, which serializes it and never lets the module
// choose its own shell.
//
// WHAT IS CHECKED lives in PackageInspector, not here: the phone's enrollment runs the same
// inspection over the same kind of package. While the list lived in this file the guarantee
// held on the desktop and not on the phone (Documentacion/Movil/06, open item #2).
public static class ModuleInstaller
{
	// Written, never string-interpolated. installation.json is assembled from records and
	// serialized: a manifest field with a quote in it comes out escaped and stays a value,
	// instead of being able to inject a second key. (A templated version of this once let a
	// module choose its own shell and switch itself on.)
	private static readonly JsonSerializerOptions Indented = new()
	{
		WriteIndented = true,
		Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
	};

	// ─────────────────────────────────────────────────────────────────────────── PACK

	// A publisher-side helper. The Host does not pack in production -the institution does- but
	// the demo needs to produce a .gmod, and building it HERE means the same forbidden-list
	// guards both ends.
	//
	//   Gnothi.Host.exe --pack <source-folder> [<output.gmod>]
	public static int Pack(string[] args)
	{
		if (args.Length < 1)
		{
			Console.Error.WriteLine("[gnothi] usage: --pack <source-folder> [<output.gmod>]");
			return 2;
		}

		string source = Path.GetFullPath(args[0]);
		if (!Directory.Exists(source))
		{
			Console.Error.WriteLine($"[gnothi] no such source folder: {source}");
			return 2;
		}

		string manifestPath = Path.Combine(source, "manifest.json");
		if (!File.Exists(manifestPath))
		{
			Console.Error.WriteLine("[gnothi] the source folder has no manifest.json.");
			return 1;
		}

		Manifest manifest;
		try { manifest = Manifest.Read(manifestPath); }
		catch (Exception e) { Console.Error.WriteLine($"[gnothi] manifest unreadable: {e.Message}"); return 1; }

		List<string> files = [.. Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories)];

		// The same inspection the installers run, over the folder about to be zipped: a package
		// that would be refused on arrival is not built in the first place.
		PackageInspection inspection = PackageInspector.Inspect(
			files.Select(f => new PackageEntry(Path.GetRelativePath(source, f).Replace('\\', '/'), new FileInfo(f).Length)),
			manifest);
		if (inspection.Rejected)
		{
			Console.Error.WriteLine("[gnothi] NOT packing:");
			foreach (string r in inspection.Rejections) Console.Error.WriteLine($"[gnothi]   x {r}");
			return 1;
		}

		string output = args.Length > 1
			? Path.GetFullPath(args[1])
			: Path.Combine(Directory.GetParent(source)!.FullName, $"{manifest.Module}.gmod");

		if (File.Exists(output)) File.Delete(output);
		using (var zip = ZipFile.Open(output, ZipArchiveMode.Create))
			foreach (string f in files)
				zip.CreateEntryFromFile(f, Path.GetRelativePath(source, f).Replace('\\', '/'));

		long size = new FileInfo(output).Length;
		Console.Error.WriteLine($"[gnothi] packed {manifest.Module}: {files.Count} files, {size / 1024.0:F1} KB");
		Console.Error.WriteLine($"[gnothi] -> {output}");
		return 0;
	}

	// ────────────────────────────────────────────────────────────────────────── INSTALL

	//   Gnothi.Host.exe --install-module <package.gmod> [<modules-folder>]
	public static int InstallFromCli(string[] args)
	{
		if (args.Length < 1)
		{
			Console.Error.WriteLine("[gnothi] usage: --install-module <package.gmod> [<modules-folder>]");
			return 2;
		}

		string package = Path.GetFullPath(args[0]);
		string modulesRoot = args.Length > 1
			? Path.GetFullPath(args[1])
			: Path.Combine(AppContext.BaseDirectory, "Modulos");

		(bool ok, string message) = Install(package, modulesRoot);
		Console.Error.WriteLine(message);
		return ok ? 0 : 1;
	}

	// The one the window calls too. Returns a human line instead of writing to the console, so
	// the window can show it. It does the whole flow: verify, extract, grant, write installation.
	public static (bool ok, string message) Install(string package, string modulesRoot, string? grantBase = null)
	{
		if (!File.Exists(package))
			return (false, $"no such package: {package}");

		using ZipArchive zip = ZipFile.OpenRead(package);

		ZipArchiveEntry? manifestEntry = zip.GetEntry("manifest.json");
		if (manifestEntry is null)
			return (false, "the package has no manifest.json at its root. Not installed.");

		Manifest manifest;
		try
		{
			using var reader = new StreamReader(manifestEntry.Open());
			Manifest? m = JsonSerializer.Deserialize(reader.ReadToEnd(), HostJson.Default.Manifest);
			if (m is null || string.IsNullOrWhiteSpace(m.Module))
				return (false, "the manifest is unreadable or declares no 'module'. Not installed.");
			manifest = m;
		}
		catch (JsonException e)
		{
			return (false, $"the manifest is not valid json: {e.Message}. Not installed.");
		}

		// Everything is checked BEFORE writing anything, by the inspection the phone runs too. It
		// also guarantees the module name is an identifier: a name like '../../x' would escape
		// the modules root, and the traversal guard below would compare two paths built on the
		// same fake root and approve itself.
		PackageInspection inspection = PackageInspector.Inspect(zip, manifest);
		if (inspection.Rejected)
			return (false, "REJECTED, not installed:\n" + string.Join("\n", inspection.Rejections.Select(r => "  x " + r)));

		// This installer runs cargo only: the Host lends the engine and loads the surface DLL. A
		// hosted module -the institution's app under web/- needs a WebView shell, which is the
		// phone's enrollment, not this path.
		if (inspection.Form != PackageForm.Cargo)
			return (false, "the package is a hosted module (web/); this installer takes cargo modules only. Not installed.");

		// A signed package whose signature does not hold is not installed, whatever its manifest
		// says: somebody touched it after it was signed. An unsigned one still installs, and the
		// line below says so.
		SignatureVerification signature = PackageSignature.Verify(zip);
		if (signature.State == SignatureState.Invalid)
			return (false, "REJECTED, not installed: the package is signed and the signature does not hold -- " + signature.Detail);

		string moduleDir = Path.Combine(modulesRoot, manifest.Module);
		string moduleRootFull = Path.GetFullPath(moduleDir);

		// The last guard, against the REAL destination, on top of the inspection's portable one:
		// this is the path that is actually written.
		List<string> rejects = [];
		foreach (ZipArchiveEntry e in zip.Entries)
		{
			if (Path.GetFileName(e.FullName).Length == 0) continue;
			string dest = Path.GetFullPath(Path.Combine(moduleDir, e.FullName));
			if (!dest.StartsWith(moduleRootFull + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
				rejects.Add($"entry points outside its folder: {e.FullName}");
		}

		if (rejects.Count > 0)
			return (false, "REJECTED, not installed:\n" + string.Join("\n", rejects.Distinct().Select(r => "  x " + r)));

		// A reinstall keeps the person's earlier consent decisions: reenabling on update would
		// be a back door.
		string installationPath = Path.Combine(moduleDir, "installation.json");
		bool enabled = inspection.InstallEnabled;
		IReadOnlyList<string> disabledTools = [];
		string? priorGranted = null;
		PackageSigner? priorSigner = null;
		if (File.Exists(installationPath))
		{
			try
			{
				Installation prior = Installation.Read(installationPath);
				enabled = prior.Enabled;
				disabledTools = prior.DisabledTools;
				if (!string.IsNullOrWhiteSpace(prior.GrantedFolder)) priorGranted = prior.GrantedFolder;
				priorSigner = prior.Signer;
			}
			catch { /* unreadable prior: rewritten */ }
		}

		// WHAT THIS PACKAGE IS NEXT TO WHAT IS INSTALLED (UpdatePlan.cs): the same, newer, older, or
		// another institution's under the same name -which is refused, here as on the phone. The
		// desktop has no sheet, so the plan is said in the message below.
		UpdatePlan update = UpdatePlan.Fresh(manifest);
		string installedManifestPath = Path.Combine(moduleDir, "manifest.json");
		if (File.Exists(installedManifestPath))
		{
			try
			{
				byte[] installedBytes = File.ReadAllBytes(installedManifestPath);
				Manifest? installedManifest = JsonSerializer.Deserialize(installedBytes, HostJson.Default.Manifest);
				byte[] incomingBytes;
				using (Stream s = zip.GetEntry("manifest.json")!.Open())
				using (var m = new MemoryStream()) { s.CopyTo(m); incomingBytes = m.ToArray(); }
				update = UpdatePlan.Compare(installedManifest, PackageSeal.Of(installedBytes), priorSigner, manifest, PackageSeal.Of(incomingBytes),
					signature.State == SignatureState.Valid ? signature.Public : null);
			}
			catch { /* unreadable prior manifest: compared as a first install */ }
		}
		if (update.Kind == UpdateKind.Collision)
			return (false, $"REJECTED, not installed: '{manifest.Module}' is already installed from '{update.InstalledInstitution}' and this package says it is from '{update.IncomingInstitution}'. Not an update: another institution under the same name.");

		// On an update the person's switches are kept for the tools that survive; the ones that
		// went are dropped, and the new ones enter on -she is installing the package that lists them.
		if (update.Kind != UpdateKind.Fresh)
			disabledTools = [.. disabledTools.Where(t => manifest.Tools.Any(x => string.Equals(x.Name, t, StringComparison.Ordinal)))];

		// THE PIN. The same key as before keeps its date; a new key -first install, or a change-
		// is remembered from now. The change itself is said in the line below: the desktop has no
		// sheet to warn on, so the message is where the person reads it.
		PackageSigner? signer = signature.State != SignatureState.Valid ? null
			: priorSigner is not null && string.Equals(priorSigner.Public, signature.Public, StringComparison.Ordinal) ? priorSigner
			: new PackageSigner { KeyId = signature.KeyId, Public = signature.Public, Since = DateTimeOffset.UtcNow.ToString("O") };

		Directory.CreateDirectory(moduleDir);
		foreach (ZipArchiveEntry e in zip.Entries)
		{
			if (Path.GetFileName(e.FullName).Length == 0) continue;
			string dest = Path.Combine(moduleDir, e.FullName);
			Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
			e.ExtractToFile(dest, overwrite: true);
		}

		// The GRANTED folder is the Host's act, never the module's. Default: the person's data
		// lives with the person, under Documents. Kept across reinstalls.
		grantBase ??= Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Gnothi");
		string granted = priorGranted ?? Path.Combine(grantBase, manifest.Module);
		Directory.CreateDirectory(granted);

		var installation = new InstallationFile
		{
			Shell = new ShellFile
			{
				Type = "in-process",
				Command = "dotnet",
				Arguments = [Path.GetFullPath(Path.Combine(moduleDir, manifest.Load!.Entry))],
			},
			GrantedFolder = granted.Replace("\\", "/"),
			Enabled = enabled,
			DisabledTools = disabledTools,

			// EL ACTA DEL ALTA, y aca 'servedBy' va VACIO a proposito: un .gmod se importa de
			// un archivo, no de la red, asi que no hay ningun host que atestiguar. Vacio no es
			// un dato que falta -- es el hecho de que no vino de ningun servidor, y por eso la
			// pantalla no puede ni debe contrastar nada. Distinto de contrastar y que no
			// coincida, que es lo que pasa cuando el bundle si bajo de algun lado.
			Provenance = new ProvenanceFile
			{
				ServedBy = "",
				At = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
				Version = manifest.Publisher?.Version ?? "",
			},
			Signer = signer,
		};

		// Legible y no Default: este archivo lo abre la persona con un editor.
		File.WriteAllText(installationPath, JsonSerializer.Serialize(installation, InstallerJson.Default.InstallationFile));

		var line = new System.Text.StringBuilder();
		line.Append($"installed '{manifest.Module}' ({manifest.Title})");
		line.Append($"\n  from     : {Path.GetFileName(package)}");
		line.Append($"\n  into     : {moduleDir}");
		line.Append($"\n  checked  : cargo, {inspection.Files} files, {inspection.Bytes / 1024.0:F1} KB, no engine of its own");
		line.Append("\n  signature: " + (signature.State == SignatureState.Valid
			? $"valid, key '{signature.KeyId}' ({PackageSignature.Fingerprint(signature.Public)})"
				+ (priorSigner is null ? " -- first time, remembered"
					: string.Equals(priorSigner.Public, signature.Public, StringComparison.Ordinal) ? $" -- the same key as before, since {priorSigner.Since}"
					: $" -- KEY CHANGED: the earlier package was signed by '{priorSigner.KeyId}' ({PackageSignature.Fingerprint(priorSigner.Public)})")
			: "none (the package is not signed)"));
		line.Append($"\n  update   : {update.Describe()}");
		line.Append($"\n  data     : {granted}");
		line.Append($"\n  enabled  : {enabled}");
		if (!enabled)
			line.Append("  (switched off: it asked for capabilities a data module does not need)");
		return (true, line.ToString());
	}

	// Local write shapes for installation.json. Separate from the read-side Installation record
	// so writing carries the '//' notes and omits nulls without the reader needing them.
	//
	// INTERNAL, NOT PRIVATE, because the source generator has to see it: InstallerJson in
	// HostJson.cs is the writer. It used to be serialized through HostJson.Legible.Installation
	// -- the type-info of the OTHER record -- and that compiled fine and threw InvalidCastException
	// on every --install-module, on every platform. The generator only protects a type it knows.
	internal sealed record InstallationFile
	{
		[JsonPropertyName("//")]
		public string Note { get; init; } =
			"GNOTHI WROTE THIS AT INSTALL TIME. It is not in the package and cannot be: the shell, "
			+ "the granted folder and consent are the Host's acts, not the institution's.";

		[JsonPropertyName("shell")] public ShellFile Shell { get; init; } = new();
		[JsonPropertyName("grantedFolder")] public string GrantedFolder { get; init; } = "";
		[JsonPropertyName("enabled")] public bool Enabled { get; init; }
		[JsonPropertyName("disabledTools")] public IReadOnlyList<string> DisabledTools { get; init; } = [];
		[JsonPropertyName("provenance")] public ProvenanceFile? Provenance { get; init; }

		// The pin: the key the package was signed with, when it was. See Installation.Signer.
		[JsonPropertyName("signer")] public PackageSigner? Signer { get; init; }
	}

	// INTERNAL POR LO MISMO QUE InstallationFile, y esto lo dijo el compilador: al traer el fix
	// del type-info, InstallationFile paso de private a internal y esta quedo private -- CS0053,
	// "inconsistent accessibility". Es el aviso que el comentario de HostJson.cs promete: con el
	// generador uno se entera AL COMPILAR, y con reflexion se enteraba en el telefono, con un
	// campo que llegaba vacio y sin error.
	internal sealed record ProvenanceFile
	{
		[JsonPropertyName("servedBy")] public string ServedBy { get; init; } = "";
		[JsonPropertyName("at")] public string At { get; init; } = "";
		[JsonPropertyName("version")] public string Version { get; init; } = "";
	}

	internal sealed record ShellFile
	{
		[JsonPropertyName("type")] public string Type { get; init; } = "in-process";
		[JsonPropertyName("command")] public string Command { get; init; } = "dotnet";
		[JsonPropertyName("arguments")] public IReadOnlyList<string> Arguments { get; init; } = [];
	}
}
