using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gnothi.Host;

// IN-PROCESS SHELL. The module is a LIBRARY, not an executable: it is loaded into its own
// context and called by function. No child process, no stdio, and the module does not need
// to bring a transport.
//
// EXPERIMENTAL. It coexists with StdioShell and is chosen per module from installation.json
// ("shell": { "type": "in-process" }). Measured: identical results to stdio on all three POC
// modules. Also measured: it does NOT contain the failures stdio does -- see
// NotasGnothiHost/10.
//
// IT DOES NOT REFERENCE THE MCP SDK. The attributes are read BY NAME, so the day a module
// carries a Gnothi-owned label instead of an MCP one, this file does not change.
internal sealed class InProcessShell : IShell
{
	private const string ClassMark = "McpServerToolTypeAttribute";
	private const string ToolMark = "McpServerToolAttribute";

	private readonly string name;
	private readonly string path;
	private readonly string grantedFolder;
	private readonly SemaphoreSlim gate = new(1, 1);

	private AssemblyLoadContext? context;
	private Type? marked;

	public InProcessShell(string name, Shell shell, string grantedFolder)
	{
		this.name = name;
		this.grantedFolder = grantedFolder;
		// The shell's first argument is the dll. Same field stdio uses, different meaning:
		// there it is what gets executed, here it is what gets loaded.
		path = Path.GetFullPath(shell.Arguments.Count > 0 ? shell.Arguments[0] : "");
	}

	public bool IsRunning => context is not null;

	private Type Start()
	{
		if (marked is not null) return marked;
		gate.Wait();
		try
		{
			if (marked is not null) return marked;

			var ctx = new AssemblyLoadContext($"module:{name}", isCollectible: true);

			// AssemblyDependencyResolver reads the module's .deps.json, and IT DOES NOT EXIST
			// ON ANDROID: it throws PlatformNotSupportedException because it depends on
			// hostpolicy, which mobile does not have. MEASURED on a Galaxy A54, SDK 36, with a
			// step-by-step probe:
			//
			//   1) AssemblyLoadContext(isCollectible: true) .... OK
			//   2) AssemblyDependencyResolver .................. FAILS PlatformNotSupported
			//   3) LoadFromAssemblyPath ........................ OK
			//   4) GetTypes .................................... FAILS: 'Choreography' missing
			//
			// Step 4 failed AS A CONSEQUENCE of step 2: with no resolver, nothing found the
			// dependencies. So the resolver becomes OPTIONAL and the fallback is to look next
			// door: every dependency of the module travels in its own folder.
			AssemblyDependencyResolver? resolver = null;
			try { resolver = new AssemblyDependencyResolver(path); }
			catch (PlatformNotSupportedException) { /* mobile: resolved by adjacency */ }

			string moduleFolder = Path.GetDirectoryName(path)!;

			ctx.Resolving += (c, n) =>
			{
				// If the Host ALREADY has this assembly, share it. Filtering by the "System."
				// prefix does NOT work: there are System.* packages the module brings and the
				// Host does not have (System.IO.Hashing, measured).
				try { _ = AssemblyLoadContext.Default.LoadFromAssemblyName(n); return null; }
				catch { /* private to the module */ }

				string? p = resolver?.ResolveAssemblyToPath(n);

				if (p is null)
				{
					string neighbour = Path.Combine(moduleFolder, (n.Name ?? "") + ".dll");
					if (File.Exists(neighbour)) p = neighbour;
				}

				return p is null ? null : c.LoadFromAssemblyPath(p);
			};

			Assembly asm = ctx.LoadFromAssemblyPath(path);

			// GetTypes and not GetExportedTypes: the modules' tool classes are internal.
			Type? found = asm.GetTypes().FirstOrDefault(t =>
				t.GetCustomAttributes(false).Any(a => a.GetType().Name == ClassMark));

			if (found is null)
				throw new InvalidOperationException($"'{Path.GetFileName(path)}' has no class marked with [{ClassMark}]");

			context = ctx;
			marked = found;
			return marked;
		}
		finally { gate.Release(); }
	}

	public Task<IReadOnlyList<string>> ActualTools(CancellationToken ct)
	{
		try
		{
			Type t = Start();
			return Task.FromResult<IReadOnlyList<string>>([.. t
				.GetMethods(Everything)
				.Select(DeclaredName)
				.Where(n => n is not null)
				.Select(n => n!)]);
		}
		finally { Evict(); }
	}

	public Task<object> Invoke(string tool, IReadOnlyDictionary<string, object?>? arguments, CancellationToken ct)
	{
		try
		{
			Type t = Start();

			MethodInfo? method = t.GetMethods(Everything)
				.FirstOrDefault(m => string.Equals(DeclaredName(m), tool, StringComparison.Ordinal));

			if (method is null) throw new InvalidOperationException($"module '{name}' does not expose '{tool}'");

			object? instance = method.IsStatic ? null : Build(t, 0);
			object?[] values = [.. method.GetParameters().Select(p => Bind(p, arguments))];

			object? raw;
			try { raw = method.Invoke(instance, values); }
			catch (TargetInvocationException e) when (e.InnerException is not null)
			{
				// With no protocol in between, the module's REAL exception arrives intact.
				// Over stdio the module's own server translates it into a generic message and
				// the detail is lost.
				throw e.InnerException;
			}

			return Task.FromResult<object>(Project(raw) ?? new Dictionary<string, object?>());
		}
		finally { Evict(); }
	}

	private const BindingFlags Everything =
		BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

	private static string? DeclaredName(MethodInfo m)
	{
		object? mark = m.GetCustomAttributes(false).FirstOrDefault(a => a.GetType().Name == ToolMark);
		if (mark is null) return null;
		return mark.GetType().GetProperty("Name")?.GetValue(mark) as string ?? m.Name;
	}

	// THE CONVENTION, and it has to be said out loud because it is the weakest part of this
	// shell: any string a constructor asks for receives the granted folder. It works on all
	// three POC modules, but it is NOT declared in any label -- the Host is guessing. With
	// stdio the problem did not exist, because the folder went as a command-line argument.
	// This is the exact point where the convention has to become a label.
	private object Build(Type t, int depth)
	{
		if (depth > 4) throw new InvalidOperationException($"constructor chain too deep at {t.Name}");

		ConstructorInfo[] ctors = [.. t
			.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
			.OrderBy(c => c.GetParameters().Length)];

		if (ctors.Length == 0) throw new InvalidOperationException($"{t.Name} has no accessible constructor");

		ConstructorInfo c = ctors[0];
		object?[] args = [.. c.GetParameters().Select(p =>
			p.ParameterType == typeof(string) ? grantedFolder : Build(p.ParameterType, depth + 1))];

		return c.Invoke(args)!;
	}

	// The enum is resolved FROM THE TYPE. That is what the manifest cannot express.
	private static object? Bind(ParameterInfo p, IReadOnlyDictionary<string, object?>? arguments)
	{
		object? raw = null;
		arguments?.TryGetValue(p.Name ?? "", out raw);

		if (raw is null)
			return p.HasDefaultValue ? p.DefaultValue : throw new ArgumentException($"missing parameter '{p.Name}'");

		string text = raw is JsonElement je
			? je.ValueKind == JsonValueKind.String ? je.GetString() ?? "" : je.GetRawText()
			: raw.ToString() ?? "";

		Type t = p.ParameterType;
		return t.IsEnum ? Enum.Parse(t, text, ignoreCase: true) : Convert.ChangeType(text, t);
	}

	// PROJECT BY READING THE LABEL BY NAME, and finding this cost a bug: crossing a load
	// context boundary, the module's [JsonPropertyName] may NOT be the same type the Host's
	// serializer knows. When it does not match, the json names are ignored IN SILENCE and the
	// response comes out with the wrong shape.
	private static object? Project(object? v)
	{
		if (v is null) return null;
		Type t = v.GetType();
		if (v is Enum) return v.ToString();
		if (t.IsPrimitive || v is string || v is decimal || v is DateTime || v is DateTimeOffset) return v;
		if (v is System.Collections.IEnumerable list) return list.Cast<object?>().Select(Project).ToList();

		Dictionary<string, object?> d = [];
		foreach (PropertyInfo p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
		{
			object? mark = p.GetCustomAttributes(false).FirstOrDefault(a => a.GetType().Name == "JsonPropertyNameAttribute");
			string? declared = mark?.GetType().GetProperty("Name")?.GetValue(mark) as string;

			// WITHOUT a label, camelCase -- and this cost a bug that only showed up with the
			// THIRD module. A return type with no [JsonPropertyName] came out PascalCase by
			// reflection and camelCase over stdio, so the SAME tool returned different field
			// names depending on the shell. The SDK applies that policy in the SERIALIZER,
			// which is not an attribute, so reflection cannot see it: it has to be replicated.
			//
			// Facturas and examenes hid it because they label every field. Compras returns
			// domain types with no labels, and there it surfaced.
			string label = declared ?? JsonNamingPolicy.CamelCase.ConvertName(p.Name);
			d[label] = Project(p.GetValue(v));
		}
		return d;
	}

	// Evicted the same way stdio does, for the same reason: a live module holds the journal
	// folder open. BUT Unload is COOPERATIVE -- measured: it does not complete if the actor
	// left live references. With stdio, evicting meant killing a process and it was a
	// guarantee; here it is a request.
	private void Evict()
	{
		gate.Wait();
		try
		{
			marked = null;
			try { context?.Unload(); } catch { /* already going away */ }
			context = null;
		}
		finally { gate.Release(); }
	}

	public ValueTask DisposeAsync()
	{
		Evict();
		gate.Dispose();
		return ValueTask.CompletedTask;
	}
}
