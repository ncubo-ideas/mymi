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
public static class ModuleInstaller
{
	// The substrate the Host lends. A package that carries any of these is trying to bring its
	// own engine, which defeats the two-DLL shape and the single-thing-to-audit property.
	private static readonly string[] Forbidden =
	[
		"Puppeteer.dll", "Choreography.dll", "Puppets.Portador.dll", "Gnothi.Contract.dll",
		"ModelContextProtocol.dll", "ModelContextProtocol.Core.dll",
	];

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

		if (!ValidName(manifest.Module))
		{
			Console.Error.WriteLine($"[gnothi] 'module' = '{manifest.Module}' is not an identifier. Not packing.");
			return 1;
		}

		string output = args.Length > 1
			? Path.GetFullPath(args[1])
			: Path.Combine(Directory.GetParent(source)!.FullName, $"{manifest.Module}.gmod");

		List<string> files = [.. Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories)];
		List<string> rejects = [];
		foreach (string f in files)
		{
			string name = Path.GetFileName(f);
			if (Forbidden.Contains(name, StringComparer.OrdinalIgnoreCase))
				rejects.Add($"carries the engine: {name}");
			if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
				rejects.Add($"carries an executable: {name} (a module is cargo, not a program)");
			if (name.StartsWith("System.", StringComparison.OrdinalIgnoreCase)
				&& name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
				rejects.Add($"carries the BCL: {name}");
		}

		if (rejects.Count > 0)
		{
			Console.Error.WriteLine("[gnothi] NOT packing:");
			foreach (string r in rejects.Distinct()) Console.Error.WriteLine($"[gnothi]   x {r}");
			return 1;
		}

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

		// The module name is used as a folder component: it must be an identifier, or a name
		// like '../../x' escapes the modules root and the traversal guard would compare two
		// paths built on the same fake root and approve itself.
		if (!ValidName(manifest.Module))
			return (false, $"'module' = '{manifest.Module}' is not an identifier (letters/digits . _ -, up to 64). Not installed.");

		if (manifest.Load is null || string.IsNullOrWhiteSpace(manifest.Load.Entry))
			return (false, "the manifest declares no 'load.entry' (which dll is the surface). Not installed as cargo.");

		string moduleDir = Path.Combine(modulesRoot, manifest.Module);
		string moduleRootFull = Path.GetFullPath(moduleDir);

		// Verify every entry BEFORE writing anything.
		List<string> rejects = [];
		foreach (ZipArchiveEntry e in zip.Entries)
		{
			string name = Path.GetFileName(e.FullName);
			if (name.Length == 0) continue;

			if (Forbidden.Contains(name, StringComparer.OrdinalIgnoreCase))
				rejects.Add($"carries the engine: {e.FullName}");
			if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
				rejects.Add($"carries an executable: {e.FullName}");
			if (name.StartsWith("System.", StringComparison.OrdinalIgnoreCase)
				&& name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
				rejects.Add($"carries the BCL: {e.FullName}");
			if (name.Equals("meta.bin", StringComparison.OrdinalIgnoreCase)
				|| e.FullName.Contains("/journal/", StringComparison.OrdinalIgnoreCase))
				rejects.Add($"carries a sample journal: {e.FullName}");

			string dest = Path.GetFullPath(Path.Combine(moduleDir, e.FullName));
			if (!dest.StartsWith(moduleRootFull + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
				rejects.Add($"entry points outside its folder: {e.FullName}");
		}

		if (rejects.Count > 0)
			return (false, "REJECTED, not installed:\n" + string.Join("\n", rejects.Distinct().Select(r => "  x " + r)));

		// A reinstall keeps the person's earlier consent decisions: reenabling on update would
		// be a back door.
		string installationPath = Path.Combine(moduleDir, "installation.json");
		bool enabled = (manifest.Capabilities?.Excesses().Any() ?? false) == false;
		IReadOnlyList<string> disabledTools = [];
		string? priorGranted = null;
		if (File.Exists(installationPath))
		{
			try
			{
				Installation prior = Installation.Read(installationPath);
				enabled = prior.Enabled;
				disabledTools = prior.DisabledTools;
				if (!string.IsNullOrWhiteSpace(prior.GrantedFolder)) priorGranted = prior.GrantedFolder;
			}
			catch { /* unreadable prior: rewritten */ }
		}

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
				Arguments = [Path.GetFullPath(Path.Combine(moduleDir, manifest.Load.Entry))],
			},
			GrantedFolder = granted.Replace("\\", "/"),
			Enabled = enabled,
			DisabledTools = disabledTools,
		};

		// Legible y no Default: este archivo lo abre la persona con un editor.
		File.WriteAllText(installationPath, JsonSerializer.Serialize(installation, InstallerJson.Default.InstallationFile));

		var line = new System.Text.StringBuilder();
		line.Append($"installed '{manifest.Module}' ({manifest.Title})");
		line.Append($"\n  from     : {Path.GetFileName(package)}");
		line.Append($"\n  into     : {moduleDir}");
		line.Append($"\n  data     : {granted}");
		line.Append($"\n  enabled  : {enabled}");
		if (!enabled)
			line.Append("  (switched off: it asked for capabilities a data module does not need)");
		return (true, line.ToString());
	}

	// An identifier, not a path. Allow-list, because a deny-list of dangerous characters always
	// falls short -separators, a drive colon, reserved Windows names, unicode that normalizes
	// to a separator.
	private static bool ValidName(string name)
		=> name.Length is > 0 and <= 64
			&& char.IsAsciiLetterOrDigit(name[0])
			&& name.All(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '_' or '-')
			&& !name.Contains("..", StringComparison.Ordinal);

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
	}

	internal sealed record ShellFile
	{
		[JsonPropertyName("type")] public string Type { get; init; } = "in-process";
		[JsonPropertyName("command")] public string Command { get; init; } = "dotnet";
		[JsonPropertyName("arguments")] public IReadOnlyList<string> Arguments { get; init; } = [];
	}
}
