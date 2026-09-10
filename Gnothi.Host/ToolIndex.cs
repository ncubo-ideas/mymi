namespace Gnothi.Host;

// THE INDEX OF PUBLISHED TOOLS, SHARED BY BOTH GATES.
//
// It exists because the two gates need THE SAME two answers -- "what is published" and "who
// contributes this name" -- and each one having its own copy is how they drift. They already
// had: Gate 1 kept a dictionary built at the moment the name was composed, and Gate 2 rebuilt
// the pair by cutting the string on the first underscore. That works only as long as no
// module is ever called 'mi_super', and NOTHING enforces that.
//
// What is shared is the index and the schema AS DATA. What each gate does with them is its
// own: Gate 1 serializes the schema into a JsonElement for MCP, Gate 2 into a string for the
// model. That is the line -- shared index, projection per gate.
//
// It is built PER QUESTION, not cached: the catalog it comes from is already filtered by
// consent, and the person can switch a query off between one question and the next.
public sealed class ToolIndex
{
	// The pair, kept together from the moment the public name is composed, which is the only
	// moment it is known without ambiguity.
	//
	// ModuleName and not Module.Name because the Host publishes tools of its own and those
	// have no Module behind them. Every gate resolves through this field and none of them had
	// to learn the difference: what they need in order to call Serve is the NAME.
	public readonly record struct Entry(string PublicName, string ModuleName, Module? Module, DeclaredTool Tool);

	private readonly Dictionary<string, Entry> byPublicName;

	private ToolIndex(Dictionary<string, Entry> byPublicName) => this.byPublicName = byPublicName;

	public static ToolIndex Of(Dispatcher dispatcher)
	{
		Dictionary<string, Entry> index = new(StringComparer.Ordinal);

		foreach ((Module m, DeclaredTool t) in dispatcher.Catalog())
		{
			string publicName = Module.PublicName(m.Name, t.Name);
			index[publicName] = new Entry(publicName, m.Name, m, t);
		}

		// THE HOST'S OWN TOOLS, published like anybody else's.
		//
		// They go in here and not in Dispatcher.Catalog() because that method means "what the
		// modules contribute", and it is read by things that are counting modules. The index
		// means "what is published", which is a different question and the one the gates ask.
		//
		// Added LAST and without overwriting: if an institution ever managed to install a
		// module under the reserved name, its tools would already be in the dictionary and the
		// Host's would silently take their place. ModuleCatalog rejects that name at load, so
		// this is the second lock on the same door.
		foreach (DeclaredTool t in HostTools.Declared)
		{
			string publicName = Module.PublicName(Dispatcher.HostModule, t.Name);
			if (!index.ContainsKey(publicName))
				index[publicName] = new Entry(publicName, Dispatcher.HostModule, null, t);
		}

		return new ToolIndex(index);
	}

	public int Count => byPublicName.Count;

	public IEnumerable<Entry> All => byPublicName.Values;

	// A name that is not here is NOT reported as a different case from one the person switched
	// off. Telling them apart would leak that the tool exists and is off.
	public bool TryFind(string publicName, out Entry entry) =>
		byPublicName.TryGetValue(publicName, out entry);

	// THE INPUT SCHEMA, AS DATA, built from what the manifest declares.
	//
	// It honours the declared TYPE and, when the parameter is an enum, its declared VALUES.
	// This used to flatten everything to "string" -- worse for Gate 2 than for Gate 1, where
	// the model picks by schema and a numeric month or a closed enum written only in prose is a
	// weak signal. The manifest now carries both, so the schema carries both.
	//
	// The execution side already tolerated this: InProcessShell.Bind runs Convert.ChangeType
	// for a numeric C# parameter and Enum.Parse for an enum one, whatever the schema said. So
	// this change only sharpens what the model SEES; it cannot break how a value is bound.
	public static IReadOnlyDictionary<string, object> InputSchema(DeclaredTool declared)
	{
		Dictionary<string, object> properties = [];

		foreach (DeclaredParameter p in declared.Input)
		{
			Dictionary<string, object> property = new()
			{
				["type"] = JsonSchemaType(p.Type),
				["description"] = p.Description,
			};

			// A real enum, not the values hidden in the prose. The C# side still Enum.Parses
			// whatever arrives, so a value outside the list is caught there, not here.
			if (p.Enum is { Count: > 0 })
				property["enum"] = p.Enum.ToArray();

			properties[p.Name] = property;
		}

		Dictionary<string, object> schema = new()
		{
			["type"] = "object",
			["properties"] = properties,
		};

		if (declared.Input.Count > 0)
			schema["required"] = declared.Input.Select(p => p.Name).ToArray();

		return schema;
	}

	// The manifest's 'type' string, mapped to a JSON Schema primitive. Anything unknown or empty
	// falls to "string" -- the most permissive, and the one the binding tolerates for any input.
	// Kept deliberately small: these are the only types a POC module has declared, and widening
	// it is a manifest decision, not a silent default.
	private static string JsonSchemaType(string declared) => declared switch
	{
		"number" or "integer" or "boolean" or "string" => declared,
		_ => "string",
	};
}
