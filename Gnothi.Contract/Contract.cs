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

// WHAT THE INSTITUTION'S OWN SCREEN ASKS FOR, and it is NOT the same set as [Query].
//
// TWO AUDIENCES, AND MIXING THEM WAS THE MISTAKE THIS EXISTS TO FIX:
//
//   [Query]  outward, to the assistant. Coarse answers, descriptions written FOR A MODEL,
//            declared in the manifest, and switched off one by one by the person.
//   [View]   inward, to the face. The series, the points, the reference lines -- what a screen
//            needs to draw. Measured on examenes: 2 queries against 21 domain reads.
//
// A screen cannot paint a trend out of a summary, and the assistant has no use for the points
// of a chart. Declaring the 21 as [Query] would also make enrollment report 19 'undeclared'
// against a manifest that names two: the system's own guard already says these are different
// sets.
//
// IT IS NOT GATED BY CONSENT, on purpose. Turning a query off decides what the ASSISTANT may
// ask about the person; it is not the institution being forbidden to show her her own record on
// the screen she just opened. The two decisions have different subjects.
//
// AND IT DOES NOT REACH THE MODEL: it is not in the manifest, the Host does not publish it, and
// ActualTools does not count it. A module cannot smuggle a tool in through this door.
[AttributeUsage(AttributeTargets.Method)]
public sealed class ViewAttribute : Attribute
{
	public ViewAttribute(string name) => Name = name;

	// The name the face asks for. Like a query's, it is the real name over the wire.
	public string Name { get; }
}

// WHERE THE COURIER LEAVES WHAT IT BROUGHT, and the only thing on a surface that writes.
//
// It is the cargo counterpart of `window.gnothi.deliver`, which the hosted form has had from
// the start: Gnothi does the network with ITS credential against the endpoint IT fixed, and
// hands the module an envelope already read. The module decides what to do with it. Nothing
// here changes who has the network -- the module still has none.
//
// IT EXISTS FOR THE HYBRID FORM. A hosted module applies events with the engine it carries; a
// cargo module is a reader over a journal someone else writes. The hybrid has no engine in its
// face and no other writer, so without this nobody could ever fill its journal.
//
// WHAT IT COSTS THE MODULE, said here because it is not obvious. A surface with only [Query]
// can use a READ-ONLY actor, and then "answering never writes" is true by construction. One
// that delivers needs a writable actor, so that guarantee becomes a discipline -- unless the
// surface keeps two actors, which is more code and keeps the property. It is the module's
// decision and the Host cannot make it.
//
// THE SIGNATURE the Host looks for is (string) -> string: the envelope in, and whatever the
// module wants to report back out. Returning is not optional -- a delivery that says nothing
// cannot be told apart from one that was refused.
[AttributeUsage(AttributeTargets.Method)]
public sealed class DeliverAttribute : Attribute
{
}

// HASTA DONDE YA LEYO EL MODULO, para que el cartero pida de ahi en adelante y no todo de nuevo.
//
// POR QUE HACE FALTA UN MARCADOR Y NO ALCANZA CON PREGUNTARLE A LA PAGINA. En la forma hospedada
// la pagina ES la superficie: tiene el actor, sabe su cursor, y lo manda. En la HIBRIDA la cara no
// tiene motor ni actor -- no puede saberlo. Mandaba cero, y el cartero bajaba el historial entero
// en cada refresco: medido en el telefono el 2026-09-14, `leidos=114 aplicados=0 rechazados=114`.
// La guarda Check hacia que no se duplicara nada, y por eso el sintoma no era un dato malo sino
// trafico y bateria.
//
// Y HAY UNA SEGUNDA RAZON, que es la que decide el diseño: si el cursor lo pusiera la cara,
// SERIA DE LA CARA. Una pagina podria pedir desde cero -bajarse todo- o desde un numero alto
// -saltearse eventos que nunca se aplicarian-. Con el cursor del lado de la superficie, la cara
// no tiene con que moverlo: pide "traeme lo nuevo" y lo que sea nuevo lo decide quien lleva el
// registro.
//
// LA FIRMA que el Host busca es () -> long. Cero es una respuesta valida y significa "todavia no
// lei nada": la primera vez el cartero trae todo, que es lo correcto.
//
// OPCIONAL. Un modulo que no lo marque sigue funcionando: el Host pide desde cero, que es lo que
// hacia antes de que esto existiera.
[AttributeUsage(AttributeTargets.Method)]
public sealed class CursorAttribute : Attribute
{
}
