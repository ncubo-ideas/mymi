using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gnothi.Host;

// LA INSTALACION ES DEL HOST. Deliberadamente separada del manifiesto, y no es
// cosmetica: son dos autoridades distintas escribiendo dos archivos distintos.
//
//   manifiesto.json   lo escribe la INSTITUCION: quien soy, que necesito, que ofrezco.
//   instalacion.json  lo escribe el HOST:        donde puse tu binario, que carpeta te
//                                               concedi, y si estas encendido.
//
// Si la cascara y la carpeta vinieran en el manifiesto, el modulo elegiria como se lo
// ejecuta y donde escribe. Las dos cosas son del Host por definicion: conceder es un acto
// del que concede.
internal sealed record Instalacion
{
	// Como el Host levanta este modulo. En este POC: proceso hijo + stdio.
	// La Etapa 2 cambia SOLO esto por un runtime wasm con --dir, y ni el manifiesto ni el
	// modulo se enteran. Eso es lo que se quiere demostrar: un artefacto, dos hospedajes.
	[JsonPropertyName("cascara")] public Cascara Cascara { get; init; } = new();

	// La carpeta que el Host le CONCEDIO. El modulo no la elige.
	[JsonPropertyName("carpetaConcedida")] public string CarpetaConcedida { get; init; } = "";

	// El interruptor. Es del Host: si lo implementara el modulo, no seria un interruptor.
	[JsonPropertyName("encendido")] public bool Encendido { get; init; } = true;

	// Tools apagadas una por una. Apagar el modulo entero es el caso grueso; esto es el
	// fino, que es el que de verdad hace falta -"el total si, el detalle no".
	[JsonPropertyName("toolsApagadas")] public IReadOnlyList<string> ToolsApagadas { get; init; } = [];

	public static Instalacion Leer(string ruta)
		=> JsonSerializer.Deserialize<Instalacion>(File.ReadAllText(ruta), Manifiesto.Opciones)
		   ?? throw new InvalidDataException($"instalacion vacia o ilegible: {ruta}");
}

internal sealed record Cascara
{
	[JsonPropertyName("tipo")] public string Tipo { get; init; } = "stdio";
	[JsonPropertyName("comando")] public string Comando { get; init; } = "";
	[JsonPropertyName("argumentos")] public IReadOnlyList<string> Argumentos { get; init; } = [];
}
