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
			// THIS HALF OF THE FILE DOES NOT GO TO THE CATALOGUE. Everything printed to Console.Error
			// with the [gnothi] prefix is the command line -- Pack and InstallFromCli -- diagnostics
			// for whoever packages a module, not copy for whoever installs it. What IS a face is what
			// Install() returns, which ModuleList.razor shows the person in the window.
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
			return (false, Textos.T("instalar.no-existe", ("paquete", package)));

		using ZipArchive zip = ZipFile.OpenRead(package);

		ZipArchiveEntry? manifestEntry = zip.GetEntry("manifest.json");
		if (manifestEntry is null)
			return (false, Textos.T("instalar.sin-manifiesto"));

		Manifest manifest;
		try
		{
			using var reader = new StreamReader(manifestEntry.Open());
			Manifest? m = JsonSerializer.Deserialize(reader.ReadToEnd(), HostJson.Default.Manifest);
			if (m is null || string.IsNullOrWhiteSpace(m.Module))
				return (false, Textos.T("instalar.manifiesto-ilegible"));
			manifest = m;
		}
		catch (JsonException e)
		{
			return (false, Textos.T("instalar.manifiesto-invalido", ("detalle", e.Message)));
		}

		// Everything is checked BEFORE writing anything, by the inspection the phone runs too. It
		// also guarantees the module name is an identifier: a name like '../../x' would escape
		// the modules root, and the traversal guard below would compare two paths built on the
		// same fake root and approve itself.
		PackageInspection inspection = PackageInspector.Inspect(zip, manifest);
		if (inspection.Rejected)
			// THE REFUSALS PASS STRAIGHT THROUGH, untranslated: they speak of manifest.json, of web/
			// and of paths, and are diagnostics for whoever packages the module, not copy for whoever
			// installs it. The FRAME is translated. It is the same the phone already does in alta.no-instalaria.
			return (false, Textos.T("instalar.rechazado",
				("detalle", string.Join("\n", inspection.Rejections.Select(r => "  x " + r)))));

		// This installer runs cargo only: the Host lends the engine and loads the surface DLL. A
		// hosted module -the institution's app under web/- needs a WebView shell, which is the
		// phone's enrollment, not this path.
		if (inspection.Form != PackageForm.Cargo)
			return (false, Textos.T("instalar.hospedado"));

		// A signed package whose signature does not hold is not installed, whatever its manifest
		// says: somebody touched it after it was signed. An unsigned one still installs, and the
		// line below says so.
		SignatureVerification signature = PackageSignature.Verify(zip);
		if (signature.State == SignatureState.Invalid)
			return (false, Textos.T("instalar.firma-no-sostiene", ("detalle", signature.Detail)));

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
			return (false, Textos.T("instalar.rechazado", ("detalle", string.Join("\n", rejects.Distinct().Select(r => "  x " + r)))));

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
			return (false, Textos.T("instalar.colision",
				("modulo", manifest.Module), ("instalada", update.InstalledInstitution ?? ""), ("entrante", update.IncomingInstitution ?? "")));

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

			// THE ENROLMENT RECORD, and here 'servedBy' goes EMPTY on purpose: a .gmod is imported
			// from a file, not off the network, so there is no host to witness. Empty is not a
			// missing value -- it is the fact that it did not come from any server, which is why the
			// screen cannot and should not contrast anything. Different from contrasting and not
			// matching, which is what happens when the bundle did come down from somewhere.
			Provenance = new ProvenanceFile
			{
				ServedBy = "",
				At = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
				Version = manifest.Publisher?.Version ?? "",
			},
			Signer = signer,
		};

		// Legible and not Default: this file is opened by the person in an editor.
		File.WriteAllText(installationPath, JsonSerializer.Serialize(installation, InstallerJson.Default.InstallationFile));

		string firma = signature.State == SignatureState.Valid
			? Textos.T("instalar.firma.valida",
					("llave", signature.KeyId ?? ""),
					("huella", PackageSignature.Fingerprint(signature.Public)))
				+ " — " + (priorSigner is null
					? Textos.T("instalar.firma.primera")
					: string.Equals(priorSigner.Public, signature.Public, StringComparison.Ordinal)
						? Textos.T("instalar.firma.misma", ("desde", priorSigner.Since))
						: Textos.T("instalar.firma.cambio",
							("llave", priorSigner.KeyId ?? ""),
							("huella", PackageSignature.Fingerprint(priorSigner.Public))))
			: Textos.T("instalar.firma.ninguna");

		// LAS ETIQUETAS SE ALINEAN AL RENDER Y NO AL LITERAL. Antes el padding venia escrito a
		// by hand inside each string -- «from     :», «checked  :» -- and that only lines up in one
		// language: «signature» is nine and «firma» five. The longest of the labels about to be
		// painted is measured and padded to that width, so the column lines up in both.
		(string Rotulo, string Valor)[] campos = [
			(Textos.T("instalar.campo.desde"), Path.GetFileName(package)),
			(Textos.T("instalar.campo.en"), moduleDir),
			(Textos.T("instalar.campo.revisado"), Textos.T("instalar.revisado",
				("archivos", inspection.Files.ToString()),
				("kb", $"{inspection.Bytes / 1024.0:F1}"))),
			(Textos.T("instalar.campo.firma"), firma),
			(Textos.T("instalar.campo.actualizacion"), update.Describe()),
			(Textos.T("instalar.campo.datos"), granted),
			(Textos.T("instalar.campo.encendido"), enabled.ToString()),
		];
		int ancho = campos.Max(c => c.Rotulo.Length);

		var line = new System.Text.StringBuilder();
		line.Append(Textos.T("instalar.hecho", ("modulo", manifest.Module), ("titulo", manifest.Title)));
		foreach ((string rotulo, string valor) in campos)
			line.Append($"\n  {rotulo.PadRight(ancho)}: {valor}");
		if (!enabled)
			line.Append("  " + Textos.T("instalar.apagado"));
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

	// INTERNAL FOR THE SAME REASON AS InstallationFile, and the compiler is the one that said so:
	// bringing in the type-info fix moved InstallationFile from private to internal and this one
	// stayed private -- CS0053, "inconsistent accessibility". It is the warning HostJson.cs's comment
	// promises: with the generator you find out AT COMPILE TIME, and with reflection you found out on
	// the phone, with a field arriving empty and no error.
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
