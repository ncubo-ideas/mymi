using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gnothi.Host;

// EL MANIFIESTO ES DEL MODULO. Lo escribe la institucion y algun dia lo firma.
// Declara identidad, que sustrato necesita, que capacidades pide, y -lo que a este POC
// le interesa- SU LISTA DE TOOLS.
//
// Por que la lista viene aca y no se le pregunta al modulo arrancandolo: leer un json
// cuesta microsegundos y no despierta nada. El Host tiene que poder armar el catalogo de
// veinte modulos sin instanciar veinte runtimes. El modulo se arranca recien cuando
// alguien llama una de sus tools.
//
// Y como es una DECLARACION, se puede verificar: mas adelante el Host le pregunta al
// modulo su tools/list de verdad y compara. Lo declarado contra lo que es.
internal sealed record Manifiesto
{
	[JsonPropertyName("manifiesto")] public string Version { get; init; } = "";
	[JsonPropertyName("modulo")] public string Modulo { get; init; } = "";
	[JsonPropertyName("titulo")] public string Titulo { get; init; } = "";
	[JsonPropertyName("institucion")] public string Institucion { get; init; } = "";
	[JsonPropertyName("sustrato")] public Sustrato? Sustrato { get; init; }
	[JsonPropertyName("capacidades")] public Capacidades? Capacidades { get; init; }
	[JsonPropertyName("tools")] public IReadOnlyList<ToolDeclarada> Tools { get; init; } = [];

	public static Manifiesto Leer(string ruta)
	{
		Manifiesto? m = JsonSerializer.Deserialize<Manifiesto>(File.ReadAllText(ruta), Opciones);
		if (m is null) throw new InvalidDataException($"manifiesto vacio o ilegible: {ruta}");
		if (string.IsNullOrWhiteSpace(m.Modulo))
			throw new InvalidDataException($"el manifiesto no declara 'modulo': {ruta}");
		return m;
	}

	internal static readonly JsonSerializerOptions Opciones = new()
	{
		PropertyNameCaseInsensitive = true,
		ReadCommentHandling = JsonCommentHandling.Skip,
		AllowTrailingCommas = true,
	};
}

// El rango, NO una version exacta. Si un modulo pinea una version puntual, cada
// actualizacion del Host huerfana a todos los modulos instalados.
internal sealed record Sustrato
{
	[JsonPropertyName("rango")] public string Rango { get; init; } = "";
}

// Lo que el modulo DECLARA necesitar. El valor de esto no es informativo: es que el Host
// pueda contrastarlo contra lo que el modulo realmente pide en ejecucion. Un modulo que
// declara 'red: false' y abre un socket es un modulo que miente, y eso es detectable.
internal sealed record Capacidades
{
	[JsonPropertyName("red")] public bool Red { get; init; }
	[JsonPropertyName("carpeta")] public string Carpeta { get; init; } = "propia";
	[JsonPropertyName("parlante")] public bool Parlante { get; init; }
	[JsonPropertyName("microfono")] public bool Microfono { get; init; }

	public IEnumerable<string> Excesos()
	{
		if (Red) yield return "red";
		if (Parlante) yield return "parlante";
		if (Microfono) yield return "microfono";
		if (Carpeta != "propia") yield return $"carpeta='{Carpeta}'";
	}
}

// Una tool tal como el modulo la declara. 'entrada' queda como lista de parametros
// simples: este POC cubre tools sin parametros y con parametros de texto, que es lo que
// el modulo de facturas necesita. El caso general -esquema de entrada arbitrario- pide
// manejar tools/call a mano en vez de sintetizar un delegate, y se anota como pendiente.
internal sealed record ToolDeclarada
{
	[JsonPropertyName("nombre")] public string Nombre { get; init; } = "";
	[JsonPropertyName("titulo")] public string Titulo { get; init; } = "";
	[JsonPropertyName("descripcion")] public string Descripcion { get; init; } = "";
	[JsonPropertyName("soloLectura")] public bool SoloLectura { get; init; } = true;
	[JsonPropertyName("entrada")] public IReadOnlyList<ParametroDeclarado> Entrada { get; init; } = [];
}

internal sealed record ParametroDeclarado
{
	[JsonPropertyName("nombre")] public string Nombre { get; init; } = "";
	[JsonPropertyName("tipo")] public string Tipo { get; init; } = "string";
	[JsonPropertyName("descripcion")] public string Descripcion { get; init; } = "";
}
