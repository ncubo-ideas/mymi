using System.Text.Json;

namespace Gnothi.Host;

// QUERY A MODULE WITHOUT AN ASSISTANT, FROM THE CONSOLE.
//
// It is not a developer convenience: it is the only way to check, from a script, that a module
// answers the same through any shell -stdio, in-process, cargo- and for the person to see their
// data with no AI in the middle.
//
//   Gnothi.Host.exe --query <modules-folder> <module> <query> [name=value ...]
//   Gnothi.Host.exe --query <modules-folder> <module> --tools
//
// It goes THROUGH the Dispatcher, so consent and metering apply exactly as they do for the MCP
// front end. It is one more caller of the same single execution path.
internal static class QueryConsole
{
	public static async Task<int> Run(string[] args)
	{
		if (args.Length < 3)
		{
			Console.Error.WriteLine("[gnothi] usage: --query <modules-folder> <module> <query> [name=value ...]");
			Console.Error.WriteLine("[gnothi]        --query <modules-folder> <module> --tools");
			return 2;
		}

		string root = Path.GetFullPath(args[0]);
		string moduleName = args[1];
		string query = args[2];

		var catalog = new ModuleCatalog(root);
		foreach (string w in catalog.Load())
			Console.Error.WriteLine($"[gnothi] warning: {w}");

		await using (catalog)
		{
			// THE HOST ANSWERING ABOUT ITSELF is asked for by name like anything else, and there
			// is no Module behind it: no shell, no granted folder, nothing to look up. Without
			// this branch the console would say "no module called 'gnothi' is installed", which
			// is true and useless -- the tools ARE published, just not by a module.
			bool isHost = string.Equals(moduleName, Dispatcher.HostModule, StringComparison.OrdinalIgnoreCase);

			Module? module = isHost
				? null
				: catalog.Modules.FirstOrDefault(
					m => string.Equals(m.Name, moduleName, StringComparison.OrdinalIgnoreCase));

			if (!isHost && module is null)
			{
				Console.Error.WriteLine($"[gnothi] no module called '{moduleName}' is installed.");
				Console.Error.WriteLine($"[gnothi] installed: {string.Join(", ", catalog.Modules.Select(m => m.Name))}");
				Console.Error.WriteLine($"[gnothi] the Host answers about itself under '{Dispatcher.HostModule}'.");
				return 1;
			}

			if (module is null)
				Console.Error.WriteLine("[gnothi] module : the Host, about itself");
			else
			{
				Console.Error.WriteLine($"[gnothi] module : {module.Name}");
				Console.Error.WriteLine($"[gnothi] shell  : {module.Installation.Shell.Type}");
				Console.Error.WriteLine($"[gnothi] folder : {module.Installation.GrantedFolder}");
			}

			// The declared-against-real check, runnable by hand. It asks the MODULE what it
			// really exposes, so it makes no sense for the Host: there is no shell to ask.
			if (query is "--tools" && module is null)
			{
				Console.Error.WriteLine("[gnothi] --tools asks a module over its shell; the Host has none.");
				return 2;
			}

			if (query is "--tools" && module is not null)
			{
				IReadOnlyList<string> real = await module.ActualToolNames(CancellationToken.None);
				Console.WriteLine($"queries the module HAS ({real.Count}):");
				foreach (string t in real.Order()) Console.WriteLine($"  {t}");
				return 0;
			}

			// name=value, split at the FIRST '=' so a value may carry equals signs.
			var arguments = new Dictionary<string, object?>(StringComparer.Ordinal);
			foreach (string pair in args.Skip(3))
			{
				int cut = pair.IndexOf('=', StringComparison.Ordinal);
				if (cut <= 0)
				{
					Console.Error.WriteLine($"[gnothi] I do not understand the argument '{pair}'. It goes as name=value.");
					return 2;
				}
				arguments[pair[..cut]] = pair[(cut + 1)..];
			}

			var dispatcher = new Dispatcher(catalog, new Meter());

			// THE ENGINE'S REPLAY NOISE IS NOT OURS. With an in-process/cargo shell the engine
			// runs in THIS process and writes "Starting ...Actor" and a "1%2%3%" progress bar to
			// the console on its own. If that lands on stdout, the output is no longer json and
			// cannot be piped or compared. stdout is saved and pointed at stderr for the call,
			// then the json is written through the saved handle. (The window does not have this
			// problem: it gets the Response object, never stdout.)
			TextWriter stdout = Console.Out;
			Console.SetOut(Console.Error);
			Response r = await dispatcher.Serve(new Request(module?.Name ?? Dispatcher.HostModule, query, arguments), CancellationToken.None);

			if (!r.Authorized)
			{
				Console.Error.WriteLine($"[gnothi] denied: {r.Reason}");
				return 1;
			}

			stdout.WriteLine(JsonSerializer.Serialize(r.Content, Pretty));
			return 0;
		}
	}

	private static readonly JsonSerializerOptions Pretty = new()
	{
		WriteIndented = true,
		Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
	};
}
