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
// IT DOES NOT REFERENCE THE MCP SDK. The attributes are read BY NAME, so a module can carry a
// Gnothi-owned label instead of an MCP one and this file does not change -- that day arrived:
// both are recognized now. A cargo module wears [ModuleSurface]/[Query] from Gnothi.Contract
// (tiny, no SDK) and an MCP-style module wears [McpServerToolType]/[McpServerTool]; the shell
// matches either, so the SAME loader serves both without knowing which it is.
internal sealed class InProcessShell : IShell
{
	private static readonly string[] ClassMarks = ["McpServerToolTypeAttribute", "ModuleSurfaceAttribute"];
	private static readonly string[] ToolMarks = ["McpServerToolAttribute", "QueryAttribute"];

	// AND THE OTHER PUBLIC. What the institution's screen asks for, which is not the same as what is
	// published towards the assistant: measured over exámenes, 2 declared tools against 21 reads of
	// the domain. See ViewAttribute in Gnothi.Contract.
	private static readonly string[] ViewMarks = ["ViewAttribute"];

	private readonly string name;
	private readonly string path;
	private readonly string grantedFolder;
	private readonly SemaphoreSlim gate = new(1, 1);

	private AssemblyLoadContext? context;
	private Type? marked;

	// THE SURFACE, ONE ONLY, ALIVE FOR AS LONG AS THE ASSEMBLY IS -- and this is a correction, not an
	// optimisation. It used to do `Build(t, 0)` ON EVERY CALL: the institution's class was rebuilt to
	// answer each question.
	//
	// AND THAT COST EVERY INSTITUTION A LOCK. If the instance does not survive between calls, what
	// does have to survive -- the actor, because starting it means rehydrating the whole journal --
	// is forced to be `static`. A `static` built lazily from several threads forces a double-checked
	// lock, and ALL FIVE surfaces ended up copying the same patch by hand. That it got copied five
	// times is the proof it was nobody's decision: it was a hole in this shell.
	//
	// With the instance here, a surface goes back to being an ordinary object with ordinary fields,
	// and writes not a single line of concurrency. What is still divided sits where it belongs: this
	// shell decides there is ONE surface, and the actor defends itself from its callers with its own
	// ReaderWriterLockSlim, which already exists (Puppeteer, ActorHandler).
	private object? live;

	// STAYING LOADED WHILE THE FACE IS OPEN, and only the hybrid form asks for it.
	//
	// By default this shell evicts after answering, just like stdio: a live module holds the journal
	// folder open, and the assistant asks now and then. But a hybrid face asks SEVERAL TIMES IN A ROW
	// while the person watches the screen, and receives deliveries in between. Loading and unloading
	// between paints is expensive and, worse, puts two actors over the same journal: the Unload is
	// COOPERATIVE -- it does not complete if the previous one left live references --, so the new
	// reader opens while the old one has still not let go.
	//
	// Symptom measured on 2026-09-11 on the phone: `FileNotFoundException: Temp file not found` right
	// after a delivery, on the next query.
	private readonly bool keepLoaded;

	public InProcessShell(string name, Shell shell, string grantedFolder, bool keepLoaded = false)
	{
		this.name = name;
		this.grantedFolder = grantedFolder;
		this.keepLoaded = keepLoaded;
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
				t.GetCustomAttributes(false).Any(a => ClassMarks.Contains(a.GetType().Name)));

			if (found is null)
				throw new InvalidOperationException(
					$"'{Path.GetFileName(path)}' has no class marked with [{string.Join("] or [", ClassMarks)}]");

			context = ctx;
			marked = found;
			return marked;
		}
		finally { gate.Release(); }
	}

	// THE INSTANCE IS PUBLISHED EXACTLY ONCE, and that is the part not to get wrong: if it were built
	// lazily WITHOUT the lock, the first-use race -- two simultaneous calls on a freshly opened
	// module, building two surfaces and with them two actors over one journal -- would not disappear
	// from the system: it would move out of the module and into this class, which is worse, because
	// here nobody is looking at it any more.
	//
	// Same double check as `Start()` and the SAME lock, without nesting them: `Start()` has already
	// released it by the time this runs.
	//
	// IT IS NOT BUILT INSIDE `Start()`, on purpose. `ActualTools` also calls `Start()`, and that is
	// what runs the module check at install and at start-up: building there would make LISTING THE
	// TOOL NAMES rehydrate every hybrid module's journal. The instance is born when somebody actually
	// asks.
	private object Instance(Type t)
	{
		if (live is not null) return live;
		gate.Wait();
		try { return live ??= Build(t, 0); }
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
		finally { if (!keepLoaded) Evict(); }
	}

	public Task<object> Invoke(string tool, IReadOnlyDictionary<string, object?>? arguments, Accepts accepts, CancellationToken ct)
		=> Ask(tool, arguments, accepts, ToolMarks);

	// WHAT THE FACE ASKS FOR, and it is ANOTHER SET: the methods marked [View].
	//
	// Same path, different marker. It is not merged with Invoke on purpose: if both doors searched
	// the same set, a screen query could arrive through the assistant's door -- and the other way
	// round, which is worse -- with nothing to stop it.
	public Task<object> View(string name, IReadOnlyDictionary<string, object?>? arguments, Accepts accepts)
		=> Ask(name, arguments, accepts, ViewMarks);

	private Task<object> Ask(
		string asked, IReadOnlyDictionary<string, object?>? arguments, Accepts accepts, string[] marks)
	{
		try
		{
			Type t = Start();

			MethodInfo? method = t.GetMethods(Everything)
				.FirstOrDefault(m => string.Equals(NameUnder(m, marks), asked, StringComparison.Ordinal));

			if (method is null) throw new InvalidOperationException($"module '{name}' does not expose '{asked}'");

			object? instance = method.IsStatic ? null : Instance(t);
			object?[] values = [.. method.GetParameters().Select(p => Bind(p, arguments, accepts))];

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
		finally { if (!keepLoaded) Evict(); }
	}

	// THE DELIVERY, BY THE SAME PATH AS A QUERY: the marked method is found, the instance is built
	// with the granted folder, it is invoked. The only differences are the marker and that the
	// signature is fixed -- (string) -> string -- so there is nothing to bind.
	//
	// AND IT DOES NOT EVICT WHEN IT FINISHES, unlike Invoke and ActualTools. There, unloading the
	// context after answering is right: the question is over. A delivery leaves the journal written
	// and the likeliest next thing is a query coming to read it; unloading the assembly in between
	// would make one single interaction pay for the load twice.
	public Task<string> Deliver(string envelopeJson, CancellationToken ct)
	{
		ArgumentNullException.ThrowIfNull(envelopeJson);

		Type t = Start();

		MethodInfo? method = t.GetMethods(Everything).FirstOrDefault(m =>
			m.GetCustomAttributes(false).Any(a => a.GetType().Name == "DeliverAttribute"));

		if (method is null)
			throw new NotSupportedException(
				$"module '{name}' has no method marked with [Deliver]: it is a surface that only reads");

		object? instance = method.IsStatic ? null : Instance(t);

		object? raw;
		try { raw = method.Invoke(instance, [envelopeJson]); }
		catch (TargetInvocationException e) when (e.InnerException is not null) { throw e.InnerException; }

		return Task.FromResult(raw as string ?? "");
	}

	// HOW FAR IT HAS ALREADY READ, by the same path as the delivery: the marked method is found, the
	// instance is built with the granted folder, it is invoked. The signature is fixed -- () -> long
	// -- so there is nothing to bind here either.
	//
	// ZERO WHEN THERE IS NO MARKER, and that is not an error: a module that does not declare its
	// cursor receives the whole history, which is exactly what happened before this existed. Same
	// when the journal is empty and the module itself answers zero.
	//
	// AND IT DOES NOT EVICT EITHER, for the same reason as Deliver: asking for the cursor is the
	// first step of a refresh, and what follows is the delivery. Unloading the assembly in between
	// would make one single interaction pay for the load twice.
	public Task<long> Cursor(CancellationToken ct)
	{
		Type t = Start();

		MethodInfo? method = t.GetMethods(Everything).FirstOrDefault(m =>
			m.GetCustomAttributes(false).Any(a => a.GetType().Name == "CursorAttribute"));

		if (method is null) return Task.FromResult(0L);

		object? instance = method.IsStatic ? null : Instance(t);

		object? raw;
		try { raw = method.Invoke(instance, []); }
		catch (TargetInvocationException e) when (e.InnerException is not null) { throw e.InnerException; }

		return Task.FromResult(raw is long ya && ya > 0 ? ya : 0L);
	}

	private const BindingFlags Everything =
		BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

	// THE NAME IT TRAVELS UNDER, for whichever set of markers it is given.
	//
	// That it takes the markers instead of looking at all of them is what keeps the two publics
	// apart: ActualTools and the assistant ask through ToolMarks, the face through ViewMarks, and a
	// screen query CANNOT arrive through the assistant's door nor the other way round.
	private static string? NameUnder(MethodInfo m, string[] marks)
	{
		object? mark = m.GetCustomAttributes(false).FirstOrDefault(a => marks.Contains(a.GetType().Name));
		if (mark is null) return null;
		// Every mark carries a 'Name'. McpServerTool may leave it null (then the method name is
		// used); Query and View always have one.
		return mark.GetType().GetProperty("Name")?.GetValue(mark) as string ?? m.Name;
	}

	private static string? DeclaredName(MethodInfo m) => NameUnder(m, ToolMarks);

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
	private static object? Bind(ParameterInfo p, IReadOnlyDictionary<string, object?>? arguments, Accepts accepts)
	{
		// THE RESERVED PARAMETER. A module surface that wants to choose its output format
		// declares a parameter called 'accepts' and the HOST fills it -- never the assistant.
		//
		// Checked BEFORE reading `arguments`, and that order is the whole point: if the
		// assistant's dictionary were read first, a question carrying accepts="toon" would
		// pick its own wrapper, and the format is not the question's to choose. A module that
		// does not declare this parameter never learns it exists, which is the same
		// indistinguishability the rest of the design relies on.
		if (string.Equals(p.Name, Accepts.ParameterName, StringComparison.Ordinal))
			return p.ParameterType == typeof(string)
				? accepts.ToString()
				: p.ParameterType.IsAssignableFrom(typeof(string[]))
					? accepts.Formats.ToArray()
					: accepts;

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
		// A DICTIONARY IS AN OBJECT, NOT A LIST -- and it goes BEFORE the IEnumerable below, which is
		// where it used to land. A Dictionary<K,V> implements IEnumerable<KeyValuePair<K,V>>, so the
		// next line projected it as a LIST OF PAIRS: `[{key:"Inspection", value:"Due"}, ...]` instead
		// of `{Inspection:"Due", ...}`.
		//
		// And it fails in silence, which is the worst thing about it. Measured on the phone on
		// 2026-09-14: the dealership's face looked for `dueStates[nextDueKind]`, found `undefined`,
		// and its fallback painted the GREEN TICK next to a pill saying «1 due now». A value that
		// never arrives looks exactly like a value saying «all good».
		//
		// The keys are NOT camelCased. The ones below are PROPERTY NAMES -- the programmer's, and the
		// serializer's policy is theirs --; these are DATA: «Inspection» is a member of the domain's
		// closed set, and lowering its initial would be rewriting a value.
		if (v is System.Collections.IDictionary map)
		{
			Dictionary<string, object?> porLlave = [];
			foreach (System.Collections.DictionaryEntry e in map)
				porLlave[e.Key?.ToString() ?? ""] = Project(e.Value);
			return porLlave;
		}

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
			// AND THE SURFACE IS CLOSED FIRST, because this shell owns it: it built it, it kept it
			// between calls, and letting go of it is part of evicting. Without this, «evicting»
			// unloaded nothing -- the Unload is cooperative and the institution's ACTOR is precisely
			// what leaves the live references.
			//
			// Through IDisposable and not through a new contract marker: an institution that has
			// something to close already knows how to say so in C#, and one that does not implements
			// nothing. Gnothi's contract does not grow for this.
			//
			// AND IT DOES NOT THROW: it is going away anyway. A Dispose that fails cannot stop the
			// unload.
			if (live is IDisposable cerrable)
			{
				try { cerrable.Dispose(); } catch { /* it was already leaving */ }
			}
			live = null;

			marked = null;
			try { context?.Unload(); } catch { /* already going away */ }
			context = null;
		}
		finally { gate.Release(); }
	}

	// AND IT CAN BE CALLED TWICE, which now genuinely happens: an update closes the module BEFORE
	// moving its folder, and afterwards the catalogue re-reads and releases whatever left the list --
	// the same object. Without this guard the second `Evict()` would do `gate.Wait()` on an already
	// disposed semaphore and an ObjectDisposedException would come out of a call that does nothing.
	private bool disposed;

	public ValueTask DisposeAsync()
	{
		if (disposed) return ValueTask.CompletedTask;
		disposed = true;

		Evict();
		gate.Dispose();
		return ValueTask.CompletedTask;
	}
}
