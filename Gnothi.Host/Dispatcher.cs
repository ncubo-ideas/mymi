using System.Text.Json;

namespace Gnothi.Host;

// THE DISPATCHER: the core of the Host. Catalog and Serve, and nothing else.
//
// It does NOT KNOW that MCP exists, nor an assistant, nor an agent loop. It takes a Request
// and returns a Response. Who calls it is the caller's business.
//
// Why this shape matters so much: the day a second front end exists -in this project it will
// be our own agent loop for mobile- it has to do THE SAME THING. If the catalog, permission
// and quota logic lived inside the class that speaks MCP, the second front end would force us
// to duplicate it, or to extract by hand half of something already in production. And worse:
// the first front end could not be DELETED the day it becomes redundant, because it would take
// the modules with it.
//
// The direction of the arrows is the property, and it is verifiable: front end -> dispatcher
// -> modules. Never the other way around, never sideways.
public sealed class Dispatcher
{
	// THE NAME THE HOST PUBLISHES ITS OWN TOOLS UNDER. Reserved: ModuleCatalog refuses to load
	// an institution's module called like this, because it would shadow the Host's own answers
	// about itself -- and those are the ones the person uses to check up on everybody else.
	public const string HostModule = "gnothi";

	private readonly ModuleCatalog catalog;
	private readonly Meter meter;
	private readonly HostTools host;

	// The gate declares what it accepts ONCE, when it builds the dispatcher: that is the
	// "negotiation per session". Optional, and the default is JSON only, so a gate that has no
	// opinion behaves exactly as it did before this existed.
	public Dispatcher(ModuleCatalog catalog, Meter meter, Accepts? accepts = null)
	{
		this.catalog = catalog;
		this.meter = meter;
		Accepts = accepts ?? Accepts.JsonOnly;
		host = new HostTools(this);
	}

	public ModuleCatalog Modules => catalog;
	public Meter Meter => meter;
	public Accepts Accepts { get; }

	// THE CATALOG. The same list for any caller; the front end does not build it.
	// It comes from the MANIFEST, not from asking the module: building the catalog wakes
	// nobody up.
	public IEnumerable<(Module module, DeclaredTool tool)> Catalog()
		=> catalog.Modules.SelectMany(m => m.EnabledTools().Select(t => (m, t)));

	// SERVE. The only execution path, for any front end.
	public async Task<Response> Serve(Request r, CancellationToken ct)
	{
		// THE HOST ANSWERING ABOUT ITSELF goes through here like anything else, so it is
		// metered like anything else and no gate needs a second path for it. What it does NOT
		// go through is the consent check: there is nothing to consent to -- these tools read
		// what the Host already knows, and the person cannot be asked to authorise the Host
		// against itself.
		if (string.Equals(r.Module, HostModule, StringComparison.OrdinalIgnoreCase))
		{
			meter.Count(HostModule, r.Tool);
			try { return Response.Ok(await host.Invoke(r.Tool, r.Arguments, ct)); }
			catch (Exception e) { return Response.Denied($"the Host failed: {e.GetType().Name}: {e.Message}"); }
		}

		// THE DENIAL REASONS STAY IN ENGLISH, and that was checked before leaving it that way.
		//
		// Every consumer of Response.Reason was reviewed: AgentLoop hands it back to the model as
		// the tool's result, ToolBridge puts it in an MCP TextContentBlock, IpcService sends it
		// through the intent's bridge, Snapshot writes it into the snapshot Siri hands the model,
		// and QueryConsole prints it to stderr. NONE of them paints it on a screen: they are all
		// input to a model, which then answers the person in THEIR language.
		//
		// The localisation plan counted them as text for the person because «they reach as far as
		// Siri». They do, but as input to the model and not as a spoken sentence. Translating them
		// would change what the model understands, which is a separate decision and is not taken.
		Module? m = catalog.Find(r.Module);
		if (m is null) return Response.Denied($"there is no installed module called '{r.Module}'");

		// The permission is checked HERE, not in the front end. A new front end cannot forget
		// to check it because it is not the one doing the invoking.
		if (!m.Allows(r.Tool))
			return Response.Denied(
				$"'{r.Tool}' is not available: the person switched off that query or the whole module");

		meter.Count(r.Module, r.Tool);

		// A VALUE OUTSIDE THE DECLARED SET IS REFUSED HERE, with the whole list, and not in the
		// module. Measured on 2026-09-21 with Apple's model in the desktop window (docs/16,
		// finding 2): it called evaluar_tendencia with analito="colesterol", the enum asks for
		// ColesterolTotal or ColesterolLdl, and what came back was the module's raw Enum.Parse:
		// «Requested value 'colesterol' was not found». A large model re-reads the schema and
		// retries; the small one does not: it moves to another tool or invents. It needs the error
		// to tell it the values.
		//
		// WHY IN THE HOST AND NOT IN EACH MODULE. The valid values are already the Host's DATA: the
		// manifest brings them in `enum` and ToolIndex publishes them in the schema. Checking
		// against that same list does not take the ownership of its rules away from the module --
		// the list is its own, the Host adds and removes no value -- and it covers the four shells
		// at once: in-process (Enum.Parse), stdio (the SDK's binding inside the child process), and
		// the WebView and the hybrid, where the module is JavaScript and there is not even an enum.
		// Doing it in each module would mean repeating it per module AND per shell, and the first
		// one to forget goes back to sending the model a text with no list.
		//
		// The reason goes in English like all the others (above). It does not translate the value's
		// name -`ColesterolTotal` travels as the module declared it-: that the schema should speak
		// in everyday names is the module's decision, not this gate's.
		//
		// A value in another CASE passes: in-process already accepted it (Enum.Parse with
		// ignoreCase) and closing that here would harden a rule that is not ours. The value goes on
		// travelling as it arrived; the module decides.
		string? outside = OutsideTheDeclaredSet(m.Manifest.Tools, r.Tool, r.Arguments);
		if (outside is not null) return Response.Denied(outside);

		try
		{
			object content = await m.Invoke(r.Tool, r.Arguments, Accepts, ct);
			return Response.Ok(content);
		}
		catch (Exception e)
		{
			// A module's error is the module's error, not a Host crash. Twenty installed
			// modules means twenty ways for something outside our control to fail.
			return Response.Denied($"the module '{r.Module}' failed: {e.GetType().Name}: {e.Message}");
		}
	}

	// THE FIRST ARGUMENT THAT FALLS OUTSIDE ITS DECLARED ENUM, worded for the model, or null when
	// every closed parameter carries a declared value. A tool the manifest does not declare, or
	// a parameter it declares open, is not looked at: the module answers for those as before.
	// Public so the suite can assert the exact text the model reads.
	public static string? OutsideTheDeclaredSet(
		IReadOnlyList<DeclaredTool> declared, string tool, IReadOnlyDictionary<string, object?>? arguments)
	{
		if (arguments is null) return null;

		DeclaredTool? t = declared.FirstOrDefault(x => string.Equals(x.Name, tool, StringComparison.Ordinal));
		if (t is null) return null;

		foreach (DeclaredParameter p in t.Input)
		{
			if (p.Enum is not { Count: > 0 } values) continue;
			if (!arguments.TryGetValue(p.Name, out object? raw) || raw is null) continue;

			string text = ArgumentText(raw);
			if (values.Any(v => string.Equals(v, text, StringComparison.OrdinalIgnoreCase))) continue;

			return $"{p.Name} '{text}' is not one of: {string.Join(", ", values)}";
		}

		return null;
	}

	// The argument as the model wrote it. Gate 1 hands a JsonElement, Gate 2 a string; either
	// way the enum compares against TEXT, same as InProcessShell.Bind reads it before parsing.
	private static string ArgumentText(object raw) =>
		raw is JsonElement je
			? je.ValueKind == JsonValueKind.String ? je.GetString() ?? "" : je.GetRawText()
			: raw.ToString() ?? "";
}

public sealed record Request(string Module, string Tool, IReadOnlyDictionary<string, object?>? Arguments);

// WHAT THE GATE ACCEPTS. Negotiated per SESSION, chosen per CALL.
//
// The split is deliberate and it is not ours: the gate declares the MENU and the module picks
// from it, because the best format depends on the SHAPE of each answer -- TOON wins on uniform
// arrays of objects (ten years of a series), minified JSON usually wins on nested or non-uniform
// structures (one lab test's record). Whoever knows that shape is the module, not the gate, and
// it only knows it at the moment it is about to answer.
//
// Negotiating on every tool call would be pure overhead; hardcoding one format for the whole
// session would throw away the win. Hence: a list up front, a choice per answer.
//
// The output format is a PROJECTION, never a fact of Sonia's life, and it does not enter the
// journal -- same criterion as cursors and tokens.
public sealed record Accepts(IReadOnlyList<string> Formats)
{
	public const string Json = "json";
	public const string Toon = "toon";

	// JSON IS MANDATORY in every module: it is the baseline that guarantees interoperability,
	// and it is why this -and not an empty list- is the default. A gate that says nothing gets
	// the behaviour it had before this existed.
	public static readonly Accepts JsonOnly = new([Json]);

	// What a gate that pays for its own tokens declares. On a phone the key is Sonia's, so this
	// is exactly where the difference is felt.
	public static readonly Accepts JsonOrToon = new([Json, Toon]);

	// The name the hint travels under towards the module. Reserved: an assistant cannot set it
	// by sending an argument with this name -- see InProcessShell.Bind.
	public const string ParameterName = "accepts";

	public override string ToString() => string.Join(",", Formats);
}

// The content travels OPAQUE on purpose. The dispatcher does not look at it, does not parse it
// and does not reformat it: if it did, it would have to understand the domain, and that is
// where the layer gets contaminated. The front end returns it exactly as it came from the
// module.
public sealed record Response(bool Authorized, object? Content, string Reason)
{
	public static Response Ok(object content) => new(true, content, "");
	public static Response Denied(string reason) => new(false, null, reason);
}

// METERING is one of the Host's four verbs, and what gets metered is the ASSISTANT: how many
// times it asked and what for. It is not a quota to protect the device -it is so the person can
// see how much was taken from them.
public sealed class Meter
{
	private readonly Dictionary<string, int> perTool = [];
	private readonly Lock gate = new();

	public void Count(string module, string tool)
	{
		lock (gate) { string k = $"{module}/{tool}"; perTool[k] = perTool.GetValueOrDefault(k) + 1; }
	}

	public IReadOnlyDictionary<string, int> Queries()
	{
		lock (gate) { return new Dictionary<string, int>(perTool); }
	}
}
