using System.IO.Compression;

namespace Gnothi.Host;

// WHAT GNOTHI CHECKS ABOUT A PACKAGE BEFORE ANYBODY ACCEPTS IT. One inspection, two installers.
//
// It used to live inside ModuleInstaller.Install -the desktop path- and NOWHERE in the phone's
// enrollment: the phone unpacked whatever the link pointed at, checked that the zip did not
// write outside its folder, and installed it switched ON, while the desktop refused packages
// that carried the engine and switched off the ones asking for capabilities a data module does
// not need. Same idea, two scopes; the guarantee held for one form and not the other
// (Documentacion/Movil/06, open item #2). Pulling the checks here is what makes them the SAME
// checks, and it is what lets the enrollment sheet show the person what was checked BEFORE she
// decides, instead of after.
//
// TWO FORMS, AND THE INSPECTION KNOWS THEM APART. A CARGO module declares `load.entry` and
// carries two small DLLs; the Host lends the engine, so a cargo package that carries the engine
// is refused -that is the "one thing to audit" property. A HOSTED module carries the
// institution's published app under web/, and that app NEEDS its engine inside (a browser runs
// no C# without the runtime next to it), so here the engine is not refused: it is COUNTED and
// reported. The two guarantees are different, and this is where that difference is written
// down, instead of being an accident of which installer happened to run.
//
// WHAT IT DOES NOT DO: authenticate. It says what the package carries, not who stands behind
// it. That is the origin and signature work, and it is deliberately a separate step.
// THE THIRD FORM, and it is the one the phone needs: web/ for the FACE, dominio/ for the
// SURFACE, and no engine anywhere.
//
// Cargo lends the engine but brings no screen -- the Host renders view.json. Hosted brings the
// screen but carries its own engine: measured on examenes, running .NET in a browser is 88,8%
// of the bundle and the engine another 8,6%, against 188 KB for the same module in process.
// HYBRID is the two halves that were never allowed to meet: the institution keeps its screen,
// and the engine is the Host's -- one engine, one owner of the journal.
//
// It used to be refused by name ("a module is cargo or hosted, not both"), with a test holding
// the line. That was a real decision, not an oversight, and what it protected was the shell
// being decidable from the package alone. A third value keeps that: the form is still read off
// what the package carries, it is just no longer a binary.
public enum PackageForm { Cargo, Hosted, Hybrid }

// One file inside the package: its path as the zip names it, and its size.
public sealed record PackageEntry(string Path, long Length);

public sealed record PackageInspection
{
	public PackageForm Form { get; init; }
	public int Files { get; init; }
	public long Bytes { get; init; }

	// Engine files found inside a hosted package. Always zero for cargo AND for hybrid: in
	// both of those they are rejections, not a count.
	public int EngineFiles { get; init; }
	public long EngineBytes { get; init; }

	// What the manifest asks for beyond a data module's needs, straight from Capabilities.
	public IReadOnlyList<string> Excesses { get; init; } = [];

	// Anything here and the package must not be installed. Written to be read.
	public IReadOnlyList<string> Rejections { get; init; } = [];

	public bool Rejected => Rejections.Count > 0;

	// The switch the installer writes. A module that asks for more than it needs is installed
	// OFF, and the person can switch it on once she has read why. The desktop has had this rule
	// since the start; the phone gets it by going through here.
	public bool InstallEnabled => Excesses.Count == 0;
}

public static class PackageInspector
{
	// The substrate the Host lends, by assembly stem. Matched with or without the fingerprint a
	// wasm publish appends (Puppeteer.hfyg9by0r8.wasm), and on .dll or .wasm alike.
	internal static readonly string[] Engine =
	[
		"Puppeteer", "Choreography", "Puppets.Portador", "Gnothi.Contract",
		"ModelContextProtocol", "ModelContextProtocol.Core",
	];

	private const StringComparison OIC = StringComparison.OrdinalIgnoreCase;

	public static PackageInspection Inspect(ZipArchive zip, Manifest manifest)
		=> Inspect(
			zip.Entries.Where(e => e.Name.Length > 0).Select(e => new PackageEntry(e.FullName, e.Length)),
			manifest);

	public static PackageInspection Inspect(IEnumerable<PackageEntry> entries, Manifest manifest)
	{
		// Windows tools write backslashes into zips; on Android those do not separate folders.
		// Everything below reasons over forward slashes.
		List<PackageEntry> files = [.. entries.Select(e => e with { Path = e.Path.Replace('\\', '/') })];
		List<string> rejections = [];

		if (!ValidName(manifest.Module))
			rejections.Add($"'module' = '{manifest.Module}' is not an identifier (letters/digits . _ -, up to 64)");

		// THE FORM, decided by what the manifest declares and what the package carries. The two
		// have to agree: a package that says "load this DLL" and also brings a web app is not
		// one module, and one that says neither has nothing Gnothi could run.
		bool declaresLoad = manifest.Load is not null && !string.IsNullOrWhiteSpace(manifest.Load.Entry);
		bool carriesWeb = files.Any(f => f.Path.StartsWith("web/", OIC));
		// WHAT IT CARRIES IS WHAT IT IS. Declaring load.entry AND carrying web/ used to be the
		// refusal; now it is the hybrid, and the two checks below stop being alternatives: a
		// hybrid has to satisfy BOTH, because it has both halves.
		PackageForm form = (declaresLoad, carriesWeb) switch
		{
			(true, true) => PackageForm.Hybrid,
			(true, false) => PackageForm.Cargo,
			_ => PackageForm.Hosted,
		};

		if (!declaresLoad && !carriesWeb)
			rejections.Add("declares no 'load.entry' and carries no web/ folder: there is nothing to load");

		if (declaresLoad && !files.Any(f => f.Path.Equals(manifest.Load!.Entry.Replace('\\', '/'), OIC)))
			rejections.Add($"'load.entry' names {manifest.Load!.Entry}, which is not in the package");

		// AND FOR A HYBRID IT HAS TO SIT DIRECTLY IN dominio/, not in a folder under it. This used
		// to be tidiness; with the wasm box it is containment and loading, both.
		//
		// LOADING: whoever opens the entry resolves its dependencies BY ADJACENCY -- the .deps.json
		// does not travel in a bundle, and AssemblyDependencyResolver needs hostpolicy, which mobile
		// does not have. So `dominio/a/entry.dll` next to `dominio/b.dll` cannot find b: it looks in
		// `dominio/a`. That break is not new -- InProcessShell had it too -- but nothing said so.
		//
		// CONTAINMENT: in the box that folder is not just where lookups happen, it is WHAT GETS
		// MOUNTED (Cage.Package, read only). The entry's path therefore decides the module's own
		// read scope. The rule below is what makes that scope always exactly dominio/ -- known in
		// advance, the same for every module, and never something the manifest can move around.
		//
		// The five POC modules already declare `dominio/<X>.App.dll`, so this rejects nothing that
		// exists today; it stops the shape that would fail later and far from here.
		if (form == PackageForm.Hybrid && declaresLoad)
		{
			string entry = manifest.Load!.Entry.Replace('\\', '/');
			if (!entry.StartsWith("dominio/", OIC) || entry.IndexOf('/', "dominio/".Length) >= 0)
				rejections.Add(
					$"'load.entry' = '{entry}': a hybrid module's entry has to sit directly in dominio/, "
					+ "because that folder is what the Host mounts for it and where its dependencies are looked up");
		}

		if (carriesWeb && !files.Any(f => f.Path.Equals("web/index.html", OIC)))
			rejections.Add("carries a web/ folder with no web/index.html: the app cannot start");

		int engineFiles = 0;
		long engineBytes = 0;
		foreach (PackageEntry f in files)
		{
			string path = f.Path;
			string[] segments = path.Split('/');
			string name = segments[^1];

			// Portable half of the zip-slip guard: the installer still checks against the real
			// destination, but a package that even NAMES a path outside itself is refused here,
			// before any destination exists.
			if (path.StartsWith('/') || path.Contains(':') || segments.Any(s => s == ".."))
				rejections.Add($"entry points outside its folder: {path}");

			if (name.EndsWith(".exe", OIC))
				rejections.Add($"carries an executable: {path} (a module is cargo, not a program)");

			// The Host writes installation.json: the shell, the granted folder and the switch are
			// its acts. A package that brings one is trying to decide those for itself.
			if (path.Equals("installation.json", OIC))
				rejections.Add("carries an installation.json: that file is the Host's to write, never the package's");

			if (name.Equals("meta.bin", OIC) || segments[..^1].Any(s => s.Equals("journal", OIC)))
				rejections.Add($"carries a sample journal: {path}");

			bool insideWeb = path.StartsWith("web/", OIC);
			bool insideDomain = path.StartsWith("dominio/", OIC);

			if (IsEngine(name))
			{
				// HOSTED IS THE ONLY FORM THAT MAY CARRY IT, and only inside web/: there the
				// engine runs in the browser, where the Host has none to lend.
				//
				// HYBRID EXISTS PRECISELY TO NOT CARRY IT -- that is the whole promise of the
				// form -- so there it is refused even inside web/. A hybrid whose face brought
				// its own engine would have two of them over one journal, which is the thing
				// the form was invented to end.
				if (form == PackageForm.Hosted && insideWeb) { engineFiles++; engineBytes += f.Length; }
				else rejections.Add($"carries the engine: {path}");
			}
			else if (IsBcl(name) && !insideWeb)
			{
				// Unchanged in meaning: cargo cannot carry a web/ at all, so asking about the
				// form here never said anything the path did not already say.
				rejections.Add($"carries the BCL: {path}");
			}
			else if (!insideWeb && name.EndsWith(".dll", OIC))
			{
				if (form == PackageForm.Hosted)
					rejections.Add($"a hosted module loads nothing outside web/: {path}");
				else if (form == PackageForm.Hybrid && !insideDomain)
					rejections.Add($"a hybrid module loads nothing outside web/ and dominio/: {path}");
			}
		}

		return new PackageInspection
		{
			Form = form,
			Files = files.Count,
			Bytes = files.Sum(f => f.Length),
			EngineFiles = engineFiles,
			EngineBytes = engineBytes,
			Excesses = [.. manifest.Capabilities?.Excesses() ?? []],
			Rejections = [.. rejections.Distinct()],
		};
	}

	// An identifier, not a path. Allow-list, because a deny-list of dangerous characters always
	// falls short -separators, a drive colon, reserved Windows names, unicode that normalizes
	// to a separator.
	public static bool ValidName(string name)
		=> name.Length is > 0 and <= 64
			&& char.IsAsciiLetterOrDigit(name[0])
			&& name.All(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '_' or '-')
			&& !name.Contains("..", StringComparison.Ordinal);

	// Puppeteer.dll, Puppeteer.hfyg9by0r8.wasm, Puppeteer.hfyg9by0r8.wasm.br: same engine.
	private static bool IsEngine(string fileName)
	{
		string? stem = Stem(fileName);
		if (stem is null) return false;
		foreach (string e in Engine)
		{
			if (stem.Equals(e, OIC)) return true;
			if (stem.StartsWith(e + ".", OIC) && !stem.AsSpan(e.Length + 1).Contains('.')) return true;
		}
		return false;
	}

	private static bool IsBcl(string fileName)
		=> Stem(fileName) is string stem && stem.StartsWith("System.", OIC);

	// The assembly name without its fingerprint's neighbours: strips a compressed copy's suffix
	// and requires an assembly extension, or says it is not an assembly at all.
	private static string? Stem(string fileName)
	{
		string name = fileName;
		if (name.EndsWith(".br", OIC) || name.EndsWith(".gz", OIC)) name = name[..^3];
		if (!name.EndsWith(".dll", OIC) && !name.EndsWith(".wasm", OIC)) return null;
		return name[..name.LastIndexOf('.')];
	}
}
