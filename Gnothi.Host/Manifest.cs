using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gnothi.Host;

// THE MANIFEST BELONGS TO THE MODULE. The institution writes it, and some day will sign it.
// It declares identity, which substrate it needs, which capabilities it asks for, and -the
// part this POC cares about- ITS LIST OF TOOLS.
//
// Why the list comes from here instead of being asked of the module by starting it up:
// reading a json costs microseconds and wakes nothing. The Host has to be able to build the
// catalog of twenty modules without instantiating twenty runtimes. The module only starts
// when somebody calls one of its tools.
//
// And because it is a DECLARATION, it can be verified: later on the Host asks the module for
// its real tools/list and compares. What was declared against what is.
public sealed record Manifest
{
	[JsonPropertyName("manifest")] public string Version { get; init; } = "";
	[JsonPropertyName("module")] public string Module { get; init; } = "";
	[JsonPropertyName("title")] public string Title { get; init; } = "";
	[JsonPropertyName("institution")] public string Institution { get; init; } = "";

	// WHERE THE INSTITUTION PUBLISHES, as an origin: scheme://host[:port]. It declares it; the
	// enrollment compares it with the origin that actually served the bundle, and asks that
	// origin -at /.well-known/gnothi/modules.json- whether the bundle is its own. The three have
	// to agree for the sheet to say the module is backed by its origin. Optional: a manifest
	// without it still installs, and the sheet says it arrived without a verifiable backing.
	[JsonPropertyName("origin")] public string Origin { get; init; } = "";
	[JsonPropertyName("substrate")] public Substrate? Substrate { get; init; }
	[JsonPropertyName("capabilities")] public Capabilities? Capabilities { get; init; }

	// Donde esta el publicador de esta institucion, para el cartero. Opcional: un modulo
	// puede vivir solo de lo que ya tiene en su journal.
	[JsonPropertyName("publicador")] public Publicador? Publicador { get; init; }
	[JsonPropertyName("tools")] public IReadOnlyList<DeclaredTool> Tools { get; init; } = [];

	// THE DECLARATIVE VIEW, optional. The name of a json file, shipped inside the module, that
	// describes the screen the institution designed for the PERSON -not for the assistant. The
	// module describes; the Host renders. A module with no view is legitimate: it just answers
	// queries and has no screen of its own. It is a file name, not the view itself, because the
	// catalog is built from manifests alone and must not pay to parse every screen up front.
	[JsonPropertyName("view")] public string View { get; init; } = "";

	// HOW A CARGO MODULE IS LOADED. Present only for cargo modules (two small DLLs + the Host
	// lends the engine); absent for stdio ones, which carry their own server. It says which
	// assembly holds the surface -the Host loads that one and finds the [ModuleSurface] class in
	// it, resolving the domain next door. It belongs to the manifest because "which DLL is me"
	// is identity, declared by the institution, not something the Host grants.
	[JsonPropertyName("load")] public Load? Load { get; init; }

	public static Manifest Read(string path)
	{
		Manifest? m = JsonSerializer.Deserialize(File.ReadAllText(path), HostJson.Default.Manifest);
		if (m is null) throw new InvalidDataException($"empty or unreadable manifest: {path}");
		if (string.IsNullOrWhiteSpace(m.Module))
			throw new InvalidDataException($"the manifest does not declare 'module': {path}");
		return m;
	}

	internal static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNameCaseInsensitive = true,
		ReadCommentHandling = JsonCommentHandling.Skip,
		AllowTrailingCommas = true,
	};
}

// A RANGE, NOT an exact version. If a module pins one precise version, every Host update
// orphans every installed module.

// DONDE VIVE LA NARRATIVA de esta institucion. Lo declara ELLA en su manifiesto, y Gnothi lo
// registra al dar de alta el modulo: a partir de ahi el endpoint es FIJO y el modulo no lo
// elige ni lo puede cambiar (especificacion 11 §6, 13 §7).
//
// El modulo nunca lo usa: no tiene red. Lo usa el cartero, que es Gnothi.
public sealed record Publicador
{
	// La base del servicio de eventos de la institucion.
	[JsonPropertyName("eventos")] public string Eventos { get; init; } = "";

	// Que recurso de la persona, y cual. La app pide UNA sola forma de URL sobre esto:
	// {recurso}/{id}/events?after=N, y nada mas.
	[JsonPropertyName("recurso")] public string Recurso { get; init; } = "expediente";

	[JsonPropertyName("id")] public string Id { get; init; } = "1";
}

public sealed record Substrate
{
	[JsonPropertyName("range")] public string Range { get; init; } = "";
}

// WHERE THE CARGO MODULE'S SURFACE LIVES. A path relative to the module's own folder, e.g.
// "dominio/Examenes.App.dll". The Host loads exactly this assembly; its dependencies -the
// domain- are resolved next to it, and the engine is lent by the Host.
public sealed record Load
{
	[JsonPropertyName("entry")] public string Entry { get; init; } = "";
}

// What the module DECLARES it needs. The value of this is not informational: it is that the
// Host can check it against what the module actually asks for at run time. A module that
// declares 'network: false' and opens a socket is a module that lies, and that is detectable.
public sealed record Capabilities
{
	[JsonPropertyName("network")] public bool Network { get; init; }
	[JsonPropertyName("folder")] public string Folder { get; init; } = "own";
	[JsonPropertyName("speaker")] public bool Speaker { get; init; }
	[JsonPropertyName("microphone")] public bool Microphone { get; init; }

	public IEnumerable<string> Excesses()
	{
		if (Network) yield return "network";
		if (Speaker) yield return "speaker";
		if (Microphone) yield return "microphone";
		if (Folder != "own") yield return $"folder='{Folder}'";
	}
}

// A tool exactly as the module declares it. 'input' stays a list of simple parameters: this
// POC covers tools with no parameters and tools with text parameters, which is what the
// invoices module needs. The general case -an arbitrary input schema- calls for handling
// tools/call by hand instead of synthesizing a delegate, and is noted as pending.
public sealed record DeclaredTool
{
	[JsonPropertyName("name")] public string Name { get; init; } = "";
	[JsonPropertyName("title")] public string Title { get; init; } = "";
	[JsonPropertyName("description")] public string Description { get; init; } = "";
	[JsonPropertyName("readOnly")] public bool ReadOnly { get; init; } = true;
	[JsonPropertyName("input")] public IReadOnlyList<DeclaredParameter> Input { get; init; } = [];
}

public sealed record DeclaredParameter
{
	[JsonPropertyName("name")] public string Name { get; init; } = "";
	[JsonPropertyName("type")] public string Type { get; init; } = "string";
	[JsonPropertyName("description")] public string Description { get; init; } = "";

	// THE CLOSED SET OF VALUES, when the parameter is an enum. Optional: a text search or a free
	// number leaves it null and nothing changes. When present, ToolIndex puts it in the schema
	// as a real JSON Schema "enum", which is a far stronger signal to the model than the same
	// values buried in the prose of the description -- the exact gap the InputSchema comment
	// used to name as "a manifest decision". This is that decision: the values are DATA now.
	[JsonPropertyName("enum")] public IReadOnlyList<string>? Enum { get; init; }
}
