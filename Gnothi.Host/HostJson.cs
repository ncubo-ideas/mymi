using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gnothi.Host;

// THE HOST'S JSON WITHOUT REFLECTION, generated at compile time.
//
// WHY IT EXISTS. `JsonSerializer.Serialize<T>` and `Deserialize<T>` discover a type's shape BY
// REFLECTION, at runtime. That works on the desktop and on Android -- where the linker is off on
// purpose, «reflection IS the mechanism, not a detail» -- and does NOT work under NativeAOT, which
// is the only documented path for embedding the Host as a framework inside a Swift app. See
// NotasGnothiHost/33: whether iOS can have Door 3 depends on it.
//
// WHAT THE GENERATOR DOES. It reads these attributes at compile time and writes each type's reader
// and writer as ordinary C# code. There is no runtime reflection, the trimmer cannot delete anything
// that is needed, and the analyser stops complaining -- IL2026 and IL3050 disappear from the site.
//
// WHAT ELSE IS GAINED, and it is not minor: if somebody adds a property to Manifest and the
// generator cannot handle it, they find out AT COMPILE TIME. With reflection they found out on the
// phone, with a field arriving empty and no error.
//
// WHAT BELONGS HERE AND WHAT DOES NOT. Only CONCRETE Host types. What travels as `object` -- the
// facts a module returns -- cannot be here by definition: its shape is decided by the module and the
// Host does not know it. Gnothi.Assistant/Json.cs takes care of that, writing the tree by hand.
[JsonSourceGenerationOptions(
	PropertyNameCaseInsensitive = true,
	ReadCommentHandling = JsonCommentHandling.Skip,
	AllowTrailingCommas = true)]
[JsonSerializable(typeof(Manifest))]
[JsonSerializable(typeof(Installation))]
public partial class HostJson : JsonSerializerContext
{
	// The same shape, but indented. It is for the files A PERSON READS -- the installation.json is
	// theirs and they are going to open it in an editor -- and that is why the formatting is not a
	// cosmetic detail.
	public static readonly HostJson Legible = new(new JsonSerializerOptions
	{
		WriteIndented = true,
		PropertyNameCaseInsensitive = true,
		ReadCommentHandling = JsonCommentHandling.Skip,
		AllowTrailingCommas = true,
	});

	// THE RESULTS OF THE HOST'S OWN TOOLS, which are the only case where a type of ours travels
	// through `Response.Content` instead of a dictionary.
	//
	// Everything else coming out of a tool is a dict/list/primitive (projected by InProcessShell) or
	// a JsonElement (returned by StdioShell), and Json.Write writes that on its own. These two are
	// the exception, and the list is HERE -- next to the generator -- so that whoever adds a fifth
	// tool to the Host trips over it.
	//
	// Returns null if it does not know the type, and the caller decides what to do. It does not guess.
	//
	// The method is public -- Gnothi.Assistant is a separate assembly on the desktop -- but the TYPES
	// stay internal, which is why their context is the one below and not this one. The signature only
	// mentions object and string, so nothing leaks.
	public static string? TryWrite(object value) => value switch
	{
		InstalledModules m => JsonSerializer.Serialize(m, HostToolsJson.Default.InstalledModules),
		Verification v => JsonSerializer.Serialize(v, HostToolsJson.Default.Verification),
		_ => null,
	};
}

// THE HOST'S TOOL RESULTS, in their own context and INTERNAL.
//
// They are separate because `InstalledModules` and `Verification` are internal on purpose: they are
// the projection the Host makes of itself, and nobody on the outside should be building one. A
// public JsonSerializerContext cannot expose a JsonTypeInfo for an internal type -- the compiler
// rejects it with CS0053 -- so the context follows the visibility of what it describes, which is
// also the right thing.
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(InstalledModules))]
[JsonSerializable(typeof(Verification))]
internal partial class HostToolsJson : JsonSerializerContext;

// WHAT THE INSTALLER WRITES: the write-shape of installation.json, with its '//' note.
//
// It is a separate context rather than one more entry in HostJson for two reasons. The first is
// visibility: InstallationFile is internal (nobody outside the Host assembles an installation) and
// HostJson is public, so it cannot expose it (CS0053), just like HostToolsJson. The second is that
// this file is opened by a person in an editor, which is why it is indented.
//
// The lesson that brought it here: serialising InstallationFile with Installation's type-info
// compiles -- Serialize(object, JsonTypeInfo) accepts any object -- and blows up at runtime. Every
// type that gets written has to have ITS OWN entry in some context in this file.
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(ModuleInstaller.InstallationFile))]
internal partial class InstallerJson : JsonSerializerContext;
