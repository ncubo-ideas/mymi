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

		// LOS MOTIVOS DE DENEGACION SE QUEDAN EN INGLES, y eso se midio antes de dejarlo asi.
		//
		// Se reviso cada consumidor de Response.Reason: AgentLoop se lo devuelve al modelo como
		// resultado de la tool, ToolBridge lo mete en un TextContentBlock de MCP, IpcService lo
		// manda por el puente del intent, Snapshot lo escribe en la foto que Siri le entrega al
		// modelo, y QueryConsole lo saca por stderr. NINGUNO lo pinta en una pantalla: todos son
		// insumo de un modelo, que despues le contesta a la persona en SU idioma.
		//
		// El plan de localizacion los daba por texto para la persona porque «llegan hasta Siri».
		// Llegan, pero como entrada del modelo y no como frase hablada. Traducirlos cambiaria lo
		// que el modelo entiende, que es una decision aparte y no esta tomada.
		Module? m = catalog.Find(r.Module);
		if (m is null) return Response.Denied($"there is no installed module called '{r.Module}'");

		// The permission is checked HERE, not in the front end. A new front end cannot forget
		// to check it because it is not the one doing the invoking.
		if (!m.Allows(r.Tool))
			return Response.Denied(
				$"'{r.Tool}' is not available: the person switched off that query or the whole module");

		meter.Count(r.Module, r.Tool);

		// UN VALOR FUERA DEL CONJUNTO DECLARADO SE RECHAZA ACA, con la lista entera, y no en
		// el modulo. Medido el 2026-09-21 con el modelo de Apple en la ventana de escritorio
		// (docs/16, hallazgo 2): llamo evaluar_tendencia con analito="colesterol", el enum pide
		// ColesterolTotal o ColesterolLdl, y lo que le volvio fue el Enum.Parse crudo del
		// modulo: «Requested value 'colesterol' was not found». Un modelo grande relee el
		// esquema y reintenta; el chico no: se va a otra tool o inventa. Necesita que el error
		// le diga los valores.
		//
		// POR QUE EN EL HOST Y NO EN CADA MODULO. Los valores validos ya son DATO del Host: el
		// manifiesto los trae en `enum` y ToolIndex los publica en el esquema. Comprobar contra
		// esa misma lista no le quita al modulo la propiedad de sus reglas -- la lista es suya,
		// el Host no agrega ni quita un valor -- y cubre las cuatro cascaras de una vez: el
		// in-process (Enum.Parse), el stdio (la union del SDK dentro del proceso hijo), y el
		// WebView y el hibrido, donde el modulo es JavaScript y ni siquiera hay un enum. Hacerlo
		// en cada modulo seria repetirlo por modulo Y por cascara, y el primero que lo olvide
		// vuelve a mandarle al modelo un texto sin la lista.
		//
		// El motivo va en ingles como todos los demas (arriba). No traduce el nombre del valor
		// -`ColesterolTotal` viaja como el modulo lo declaro-: que el esquema hable en nombres
		// corrientes es una decision del modulo, no de esta compuerta.
		//
		// Un valor con otra CAJA pasa: el in-process ya lo aceptaba (Enum.Parse con ignoreCase)
		// y cerrarlo aca seria endurecer una regla que no es nuestra. El valor sigue viajando
		// como llego; el modulo decide.
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
