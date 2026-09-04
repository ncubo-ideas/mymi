using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gnothi.Host;

// JSON DEL HOST SIN REFLEXION, generado al compilar.
//
// POR QUE EXISTE. `JsonSerializer.Serialize<T>` y `Deserialize<T>` descubren la forma del tipo
// POR REFLEXION, en runtime. Eso anda en escritorio y en Android -- donde el linker esta
// apagado a proposito, «reflection IS the mechanism, not a detail»-- y NO anda bajo NativeAOT,
// que es el unico camino documentado para embeber el Host como framework en una app de Swift.
// Ver NotasGnothiHost/33: de eso depende que en iOS pueda haber Puerta 3.
//
// QUE HACE EL GENERADOR. Lee estos atributos al compilar y escribe el lector y el escritor de
// cada tipo como codigo C# comun. No hay reflexion en runtime, el trimmer no puede borrar nada
// que haga falta, y el analizador deja de quejarse -- IL2026 e IL3050 desaparecen del sitio.
//
// LO QUE SE GANA ADEMAS, y no es menor: si alguien agrega una propiedad a Manifest y el
// generador no la puede manejar, se entera AL COMPILAR. Con reflexion se enteraba en el
// telefono, con un campo que llegaba vacio y sin error.
//
// QUE VA ACA Y QUE NO. Solo tipos CONCRETOS del Host. Lo que viaja como `object` -- los hechos
// que devuelve un modulo -- no puede estar aca por definicion: su forma la decide el modulo y
// el Host no la conoce. De eso se encarga Gnothi.Assistant/Json.cs, escribiendo el arbol a
// mano.
[JsonSourceGenerationOptions(
	PropertyNameCaseInsensitive = true,
	ReadCommentHandling = JsonCommentHandling.Skip,
	AllowTrailingCommas = true)]
[JsonSerializable(typeof(Manifest))]
[JsonSerializable(typeof(Installation))]
public partial class HostJson : JsonSerializerContext
{
	// La misma forma, pero indentada. Es para los archivos que LEE UNA PERSONA -- el
	// installation.json es de ella y lo va a abrir con un editor -- y por eso el formato no es
	// un detalle cosmetico.
	public static readonly HostJson Legible = new(new JsonSerializerOptions
	{
		WriteIndented = true,
		PropertyNameCaseInsensitive = true,
		ReadCommentHandling = JsonCommentHandling.Skip,
		AllowTrailingCommas = true,
	});

	// LOS RESULTADOS DE LAS TOOLS DEL PROPIO HOST, que son el unico caso donde por
	// `Response.Content` viaja un tipo nuestro y no un diccionario.
	//
	// Todo lo demas que sale de una tool es dict/lista/primitivo (lo proyecta InProcessShell) o
	// un JsonElement (lo devuelve StdioShell), y eso Json.Write lo escribe solo. Estos dos son
	// la excepcion, y la lista esta ACA -- al lado del generador -- para que quien agregue una
	// quinta tool al Host tropiece con ella.
	//
	// Devuelve null si no lo conoce, y el que llama decide que hacer. No adivina.
	//
	// El metodo es publico -- Gnothi.Assistant es otro ensamblado en escritorio -- pero los
	// TIPOS siguen internos, y por eso su contexto es el de abajo y no este. La firma solo
	// menciona object y string, asi que nada se escapa.
	public static string? TryWrite(object value) => value switch
	{
		InstalledModules m => JsonSerializer.Serialize(m, HostToolsJson.Default.InstalledModules),
		Verification v => JsonSerializer.Serialize(v, HostToolsJson.Default.Verification),
		_ => null,
	};
}

// LOS RESULTADOS DE LAS TOOLS DEL HOST, en su propio contexto y INTERNO.
//
// Estan aparte porque `InstalledModules` y `Verification` son internos a proposito: son la
// proyeccion que el Host hace de si mismo, y nadie de afuera deberia construir una. Un
// JsonSerializerContext publico no puede exponer un JsonTypeInfo de un tipo interno -- el
// compilador lo rechaza con CS0053 -- asi que el contexto acompaña la visibilidad de lo que
// describe, que ademas es lo correcto.
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(InstalledModules))]
[JsonSerializable(typeof(Verification))]
internal partial class HostToolsJson : JsonSerializerContext;
