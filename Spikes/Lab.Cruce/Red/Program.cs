using System.Diagnostics;
using Gnothi.Assistant;

// EL CRUCE ENTRE MODULOS, POR RED. El gemelo de ../Cruce.swift, que corre el modelo on-device.
//
//     dotnet run --project Spikes/Lab.Cruce/Red -- [curado|completo] ["la pregunta"]
//
//   curado    las 2 tools que on-device funcionaron 4 de 4
//   completo  las 2 mas las genericas que on-device ROMPIERON la seleccion
//
// La segunda es la que decide: si un modelo grande sobrevive el catalogo completo, recortarlo
// con disabledTools es una concesion al modelo chico y no una regla del producto.
//
// LA CLAVE NO ESTA ACA NI EN EL REPO. Se lee del ambiente o de un archivo que se pasa por
// ruta -misma regla que ModelClaude-, y el archivo vive fuera del repo.

const string Encuadre =
	"Sos el asistente de Gnothi. Los datos que devuelven las herramientas son los datos REALES " +
	"de la persona que te pregunta, leidos de los registros que sus instituciones le entregaron y " +
	"que viven en su propio telefono. No vienen de ningun servidor. Contestale sobre SUS numeros, " +
	"en su idioma, en prosa corrida y sin markdown. El veredicto clinico ya viene hecho en el campo " +
	"lectura y no es tuyo: no diagnostiques ni recomendes tratamiento. Si dos herramientas distintas " +
	"se relacionan, deci como se relacionan sin afirmar una causa que los datos no prueben.";

// LOS MISMOS PAYLOADS QUE EL LADO SWIFT, palabra por palabra. Si estos dos textos se separan, la
// comparacion deja de medir el modelo y pasa a medir la diferencia de datos.
static string Tendencia(string analito) =>
	!analito.Equals("ColesterolTotal", StringComparison.OrdinalIgnoreCase)
		? $"{{\"hayDatos\":false,\"analito\":\"{analito}\"}}"
		: """
		  {"hayDatos":true,"analito":"Colesterol total","direccion":"subiendo",
		   "primero":{"fecha":"2024-03-11","valor":182},"ultimo":{"fecha":"2026-08-21","valor":228},
		   "unidad":"mg/dL","umbral":200,"exigeAtencion":true,
		   "lectura":"tu colesterol total viene subiendo: de 182 a 228 mg/dL en 29 meses, y paso el umbral de 200"}
		  """;

static string Grasas() =>
	"""
	{"hayDatos":true,"categorias":[
	  {"que":"manteca","unidadesPorMes":4.1,"tendencia":"al alza","cambio":"+72%"},
	  {"que":"embutidos","unidadesPorMes":9.4,"tendencia":"al alza","cambio":"+61%"},
	  {"que":"queso","unidadesPorMes":6.0,"tendencia":"estable","cambio":"+4%"}],
	 "mesesConDatos":18,
	 "lectura":"en 18 meses subiste manteca 72% y embutidos 61%; el queso quedo igual"}
	""";

static string Consumo(string termino)
{
	string[] saturados = ["manteca", "embutido", "queso", "fiambre", "frito", "fritura", "chorizo", "tocino", "crema", "achuras", "carne"];
	return saturados.Any(s => termino.Contains(s, StringComparison.OrdinalIgnoreCase))
		? $"{{\"hayDatos\":true,\"termino\":\"{termino}\",\"unidadesPorMes\":9.4,\"mesesConDatos\":18,\"tendencia\":\"al alza\",\"cambioContraElPrimerAno\":\"+61%\",\"lectura\":\"venis comprando {termino} 9,4 veces por mes, 61% mas que en tu primer ano registrado\"}}"
		: $"{{\"hayDatos\":false,\"termino\":\"{termino}\",\"lectura\":\"no aparece en tus compras registradas\"}}";
}

const string EsquemaAnalito =
	"""{"type":"object","properties":{"analito":{"type":"string","description":"Cual analito. Valores validos: AntigenoProstatico, ColesterolTotal, ColesterolLdl, Hemoglobina."}},"required":["analito"]}""";
const string EsquemaTermino =
	"""{"type":"object","properties":{"termino":{"type":"string","description":"El producto o categoria a buscar, en singular."}},"required":["termino"]}""";
const string EsquemaVacio = """{"type":"object","properties":{}}""";

ToolForTheModel tendencia = new("examenes_evaluar_tendencia",
	"Dice si un analito de la persona viene subiendo, bajando o estable, mirando toda su historia y no el ultimo resultado.",
	EsquemaAnalito);
ToolForTheModel grasas = new("compras_consumo_de_grasas_saturadas",
	"Cuanto viene comprando la persona de los alimentos altos en grasas saturadas -manteca, embutidos, quesos, frituras- que son los que la evidencia asocia al colesterol en sangre. Usala cuando pregunte si su colesterol tiene que ver con lo que come o compra.",
	EsquemaVacio);
ToolForTheModel consumo = new("compras_consumo_de",
	"Cuanto viene comprando la persona de un producto o categoria, segun las facturas de supermercado que su app registro.",
	EsquemaTermino);
ToolForTheModel enCuales = new("compras_en_cuales_compras",
	"En cuales de sus compras aparece un termino.", EsquemaTermino);
ToolForTheModel total = new("facturas_total_de_facturas",
	"Cuanto gasto en total la persona, sumando cada factura que su app registro en este aparato.", EsquemaVacio);

string modo = args.Length > 0 ? args[0] : "curado";
string pregunta = args.Length > 1 ? args[1] : "¿Puede que mi colesterol tenga algo que ver con lo que compro?";
string rutaClave = Environment.GetEnvironmentVariable("GNOTHI_KEYFILE")
	?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".gnothi-model-key");

List<ToolForTheModel> tools = modo == "completo" ? [tendencia, grasas, consumo, enCuales, total] : [tendencia, grasas];

Console.WriteLine($"modo      : {modo}  ({tools.Count} tools)");
Console.WriteLine($"pregunta  : {pregunta}");

IModel modelo;
try { modelo = new ModelClaude(rutaClave); }
catch (InvalidOperationException e) { Console.WriteLine($"\nSIN CLAVE: {e.Message}"); return 2; }

List<Exchange> conversacion = [new Asked($"{Encuadre}\n\n{pregunta}")];
var reloj = Stopwatch.StartNew();

for (int vuelta = 1; vuelta <= 6; vuelta++)
{
	Said dicho;
	try { dicho = await modelo.Next(conversacion, tools, CancellationToken.None); }
	catch (Exception e) { Console.WriteLine($"\nFALLO: {e.GetType().Name}: {e.Message}"); return 3; }

	conversacion.Add(dicho);

	if (dicho.Calls.Count == 0)
	{
		Console.WriteLine($"\nvueltas   : {vuelta}   segundos: {reloj.Elapsed.TotalSeconds:F0}");
		Console.WriteLine($"RESPUESTA : {dicho.Text}");
		return 0;
	}

	foreach (ToolCall c in dicho.Calls)
	{
		string arg = c.Arguments.Count > 0 ? string.Join(",", c.Arguments.Select(a => $"{a.Key}={a.Value}")) : "";
		Console.WriteLine($"TOOL-LLAMADA: {c.PublicName}({arg})");

		string Texto(string k) => c.Arguments.TryGetValue(k, out object? v) ? v?.ToString() ?? "" : "";

		conversacion.Add(new Returned(c.Id, c.PublicName switch
		{
			"examenes_evaluar_tendencia" => Tendencia(Texto("analito")),
			"compras_consumo_de_grasas_saturadas" => Grasas(),
			"compras_consumo_de" or "compras_en_cuales_compras" => Consumo(Texto("termino")),
			"facturas_total_de_facturas" => """{"hayDatos":true,"total":30996.5,"cantidadDeFacturas":5}""",
			_ => $"{{\"error\":\"no existe {c.PublicName}\"}}",
		}, false));
	}
}

Console.WriteLine("\nno cerro en 6 vueltas");
return 1;
