using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Gnothi.Host;

// GNOTHI PRESENTS ITSELF TO THE PERSON'S ASSISTANT.
//
// Today this is done by hand editing a json, and it was the only RED step of the install
// flow. The person has no reason to know a config file exists, let alone where it lives.
//
// Run it once, from the PUBLISHED executable:  Gnothi.Host.exe --install [modules-folder]
//
// ═══ THREE CARES, AND ALL THREE COME FROM HAVING PAID FOR THEM ═══
//
// 1. THE FILE IS NOT OVERWRITTEN. The assistant's config holds many things that are not ours
//    -preferences, paths, account settings. It is read, ONE key is added, and it is written
//    back. A config was already lost in this project by rewriting it whole.
//
// 2. THE PATH IS NOT GUESSED, IT IS SEARCHED. On Windows the packaged app redirects its
//    %APPDATA% to %LOCALAPPDATA%\Packages\Claude_<hash>\..., and the hash changes per install.
//    Hardcoding it guarantees it works on exactly one machine.
//
// 3. IT BACKS UP BEFORE WRITING. We are modifying another application's file on someone's
//    machine. If something goes wrong, it has to be able to come back.
internal static class HostInstaller
{
	// No BOM, two-space indent: that is how the app writes it, so the diff the person sees if
	// they open the file is only our key.
	private static readonly JsonSerializerOptions AsTheAppWritesIt = new() { WriteIndented = true };

	private sealed record Assistant(string Name, string Config, string Key);

	public static int Install(string[] args)
	{
		string? self = Environment.ProcessPath;
		if (self is null || Path.GetFileNameWithoutExtension(self)
				.Equals("dotnet", StringComparison.OrdinalIgnoreCase))
		{
			Console.Error.WriteLine("[gnothi] --install needs the published executable, not 'dotnet Gnothi.Host.dll'.");
			return 2;
		}

		// The modules folder, next to the executable by default. That it lives beside Gnothi and
		// not under Documents is not a whim: the person does not administer it by hand.
		string modulesFolder = args.Length > 0
			? Path.GetFullPath(args[0])
			: Path.Combine(Path.GetDirectoryName(self)!, "Modulos");

		Console.Error.WriteLine("-- Gnothi --");
		Console.Error.WriteLine($"executable    : {self}");
		Console.Error.WriteLine($"modules folder: {modulesFolder}");

		// THE SPACE IN THE PATH. On Windows the assistant launches the command through cmd.exe,
		// and with a space the launch dies with two messages that look nothing like the cause. It
		// is warned HERE, the only moment anything can be done about it. On macOS the command is
		// spawned with an argument vector and a space is just a character, so the check is
		// Windows-only: "Application Support" has one and is where things live there.
		if (OperatingSystem.IsWindows() && self.Contains(' '))
		{
			Console.Error.WriteLine();
			Console.Error.WriteLine("!  Gnothi's path has a SPACE. The assistant launches it through cmd, and with a");
			Console.Error.WriteLine("   space the launch fails. Move it to a folder with no spaces -- e.g. C:\\Gnothi\\ --");
			Console.Error.WriteLine("   and run --install again.");
			return 1;
		}

		Directory.CreateDirectory(modulesFolder);

		// SAY HOW MANY MODULES ARE THERE, NOW. An empty folder is legitimate -a fresh install
		// gets its modules later, from each institution's page- but it is also what a mangled
		// path looks like. Paid for: a pasted command lost its line break and the folder came
		// out as "Modulos.local~/Gnothi/Gnothi.Host"; the install went through, the assistant
		// reported zero modules, and nothing had said so. Now this line does.
		int modules = Directory.EnumerateDirectories(modulesFolder)
			.Count(d => File.Exists(Path.Combine(d, "manifest.json")));
		Console.Error.WriteLine($"modules there : {modules}"
			+ (modules == 0 ? "   <- empty. Fine for a fresh install; if you expected modules, check the path." : ""));

		IReadOnlyList<Assistant> found = Find();
		if (found.Count == 0)
		{
			Console.Error.WriteLine();
			Console.Error.WriteLine("Found no installed assistant I know how to configure.");
			Console.Error.WriteLine("Gnothi is installed anyway: you can register it by hand whenever you want.");
			return 0;
		}

		Console.Error.WriteLine();
		Console.Error.WriteLine("Assistants I found:");
		for (int i = 0; i < found.Count; i++)
			Console.Error.WriteLine($"  {i + 1}. {found[i].Name}");
		Console.Error.WriteLine($"  {found.Count + 1}. none for now");

		int chosen = Ask(found.Count + 1);
		if (chosen > found.Count)
		{
			Console.Error.WriteLine("Done. Gnothi is installed and unregistered.");
			return 0;
		}

		return Register(found[chosen - 1], self, modulesFolder);
	}

	// ─────────────────────────────────────────────────────────── where each config lives

	// SEARCHED, not guessed. The package hash changes per install, so the MSIX path comes from a
	// glob and not from a constant.
	private static IReadOnlyList<Assistant> Find()
	{
		List<Assistant> hits = [];

		string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		string roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

		// Packaged Claude Desktop (MSIX). This is the path that is hard to find, and the one the
		// app itself opens from its own "edit config" button.
		string packages = Path.Combine(local, "Packages");
		if (Directory.Exists(packages))
			foreach (string dir in Directory.EnumerateDirectories(packages, "Claude_*"))
			{
				string cfg = Path.Combine(dir, "LocalCache", "Roaming", "Claude", "claude_desktop_config.json");
				if (File.Exists(cfg)) hits.Add(new Assistant("Claude Desktop", cfg, "mcpServers"));
			}

		// Claude Desktop on macOS. There is ONE place, and it is not what SpecialFolder gives:
		// ApplicationData on macOS resolves to ~/.config, which the app never reads. The file
		// may not exist yet on a fresh install -the app creates it on first "edit config"-, so
		// the FOLDER is the evidence that the app is there, and the file gets created.
		if (OperatingSystem.IsMacOS())
		{
			string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			string macFolder = Path.Combine(home, "Library", "Application Support", "Claude");
			string macCfg = Path.Combine(macFolder, "claude_desktop_config.json");
			if (Directory.Exists(macFolder))
				hits.Add(new Assistant("Claude Desktop", macCfg, "mcpServers"));
		}

		// Unpackaged Claude Desktop (Windows), and ~/.config/Claude on Linux.
		//
		// TRAP PAID FOR: if a packaged config was ALREADY found, this one under %APPDATA% exists
		// but the app does NOT read it. Editing it does nothing, and the symptom is "I configured
		// Gnothi and the assistant does not see it" -- hours of looking in the wrong place. It is
		// offered anyway, but last and saying what it is.
		string unpackaged = Path.Combine(roaming, "Claude", "claude_desktop_config.json");
		if (File.Exists(unpackaged) && !hits.Any(a => a.Config == unpackaged))
		{
			bool packagedExists = hits.Count > 0;
			hits.Add(new Assistant(
				packagedExists
					? "Claude Desktop in %APPDATA% -- note: if the app is the Store one, it does NOT read this file"
					: "Claude Desktop",
				unpackaged, "mcpServers"));
		}

		return hits;
	}

	// ─────────────────────────────────────────────────────────────────────── the registration

	private static int Register(Assistant assistant, string executable, string modulesFolder)
	{
		Console.Error.WriteLine();
		Console.Error.WriteLine($"I am going to add Gnothi to: {assistant.Config}");

		JsonObject root;
		string original = "";
		try
		{
			if (File.Exists(assistant.Config))
			{
				original = File.ReadAllText(assistant.Config);
				// EVERYTHING IS READ AND KEPT. What is not ours is not touched.
				root = JsonNode.Parse(original, documentOptions: new JsonDocumentOptions
				{
					CommentHandling = JsonCommentHandling.Skip,
					AllowTrailingCommas = true,
				}) as JsonObject ?? [];
			}
			else root = [];
		}
		catch (JsonException e)
		{
			Console.Error.WriteLine($"I could not read the existing config: {e.Message}");
			Console.Error.WriteLine("I will not touch it. Register it by hand.");
			return 1;
		}

		if (root[assistant.Key] is not JsonObject servers)
		{
			servers = [];
			root[assistant.Key] = servers;
		}

		bool wasThere = servers["gnothi"] is not null;

		// THE MODULES FOLDER TRAVELS AS AN EXPLICIT ARGUMENT. So the installed server and the
		// desktop window both look at the same folder without either knowing about the other,
		// and without depending on where the executable happens to sit relative to the repo.
		servers["gnothi"] = new JsonObject
		{
			["command"] = executable,
			["args"] = new JsonArray(modulesFolder),
		};

		Console.Error.WriteLine();
		Console.Error.WriteLine(wasThere ? "  gnothi was already there: I update it." : "  I add the 'gnothi' entry.");
		Console.Error.WriteLine($"  I keep the other {root.Count - 1} keys in the file intact.");
		Console.Error.WriteLine();
		Console.Error.Write("Write it? [y/N] ");

		string? yes = Console.ReadLine();
		if (yes is null || !yes.Trim().StartsWith("y", StringComparison.OrdinalIgnoreCase))
		{
			Console.Error.WriteLine("I wrote nothing.");
			return 0;
		}

		// THE BACKUP BEFORE WRITING. We are modifying another application's file.
		if (original.Length > 0)
		{
			string backup = assistant.Config + ".before-gnothi";
			File.WriteAllText(backup, original, new UTF8Encoding(false));
			Console.Error.WriteLine($"  backup: {backup}");
		}

		File.WriteAllText(assistant.Config, root.ToJsonString(AsTheAppWritesIt), new UTF8Encoding(false));

		Console.Error.WriteLine();
		Console.Error.WriteLine($"Done. Restart {assistant.Name} and ask it what modules you have installed.");
		Console.Error.WriteLine("(The assistant reads its query list at startup, so the restart is needed.)");
		return 0;
	}

	private static int Ask(int howMany)
	{
		while (true)
		{
			Console.Error.WriteLine();
			Console.Error.Write($"Which one? [1-{howMany}] ");
			string? read = Console.ReadLine();

			// With no interactive console -running from an installer or a script- take the first
			// option. Hanging waiting for a key nobody will press is worse.
			if (read is null) return 1;

			if (int.TryParse(read.Trim(), out int n) && n >= 1 && n <= howMany) return n;
		}
	}
}
