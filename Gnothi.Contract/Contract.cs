namespace Gnothi.Contract;

// THE MODULE'S SURFACE, DECLARED WITH A TINY LABEL.
//
// A module marks one class with [ModuleSurface] and its query methods with [Query("name")].
// The Host discovers them by reflection -reading the attribute BY NAME, exactly as it already
// does for the MCP attributes- so the module does NOT carry the MCP SDK. That is what makes
// the cargo package two small DLLs (the domain + the surface) instead of a megabyte.
//
// These attributes carry no behaviour. They are a naming convention the compiler can check:
// the class the Host builds, and the methods it may call, are the ones wearing these.

[AttributeUsage(AttributeTargets.Class)]
public sealed class ModuleSurfaceAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class QueryAttribute : Attribute
{
	public QueryAttribute(string name) => Name = name;

	// The name the query travels under. It is the module's REAL name over the wire, the same
	// one the manifest declares; it is not translated.
	public string Name { get; }
}
