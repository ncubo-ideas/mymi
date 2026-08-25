using Gnothi.Host;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;

// EL FRENTE MCP: la Puerta 1. Traduce y delega, nada mas.
//
// Uso:  Gnothi.Host <directorio-de-modulos>
//
// CON TRANSPORTE STDIO, STDOUT ES EL CANAL JSON-RPC. Cualquier Console.Write propio -o de
// una libreria- corrompe el protocolo, y peor que romperlo: si no cierra la linea, esos
// caracteres se pegan adelante del siguiente mensaje y el cliente se desincroniza sin un
// error claro. Se desvia stdout a stderr ANTES de tocar cualquier otra cosa.
Console.SetOut(Console.Error);

string raizDeModulos = args.Length > 0
	? args[0]
	: Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Modulos");
raizDeModulos = Path.GetFullPath(raizDeModulos);

// El catalogo se carga leyendo MANIFIESTOS. Ningun modulo se levanta acá: veinte modulos
// son veinte json, no veinte replays de journal.
var catalogo = new CatalogoDeModulos(raizDeModulos);
IReadOnlyList<string> avisos = catalogo.Cargar();

Console.Error.WriteLine($"[gnothi] modulos en {raizDeModulos}: {catalogo.Modulos.Count}");
foreach (string a in avisos) Console.Error.WriteLine($"[gnothi] aviso: {a}");

var despacho = new Despacho(catalogo, new Medidor());

foreach ((Modulo m, ToolDeclarada t) in despacho.Catalogo())
	Console.Error.WriteLine($"[gnothi] publica: {Modulo.NombrePublico(m.Nombre, t.Nombre)}");

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton(despacho);

builder.Services.AddMcpServer(opciones =>
{
	opciones.ServerInfo = new() { Name = "gnothi", Version = "0.1.0" };

	// Las instrucciones son PARA LA IA, y llegan en el handshake. Es donde se le explica
	// que no esta hablando con el servidor de una empresa.
	opciones.ServerInstructions = """
		Sos la IA personal de la persona que usa este dispositivo.

		Gnothi es el Host que ella instalo. Los modulos son lo que cada institucion le
		entrego -su laboratorio, su supermercado- y cada uno guarda el registro de ella en
		una carpeta suya, en este dispositivo. Gnothi no le pregunta nada a ninguna empresa:
		lee lo que ya esta aca.

		Las tools con prefijo 'gnothi_' son del Host y hablan de los modulos, no de los
		datos. Las demas vienen prefijadas por el modulo que las aporta.

		Lo que Gnothi te muestra es lo que la persona autorizo. Si esperas una consulta que
		no aparece en la lista, lo mas probable es que ella la haya apagado; no insistas ni
		intentes rodearla. No podes encender nada: el consentimiento es de ella.

		Si un total no cambio y ella esperaba que si, pedile que abra el modulo y lo
		actualice, y volve a consultar.
		""";

	// SE DECLARA que este servidor sabe avisar cuando su catalogo cambia. Sin esta
	// capacidad el cliente no tiene ningun motivo para volver a pedir tools/list, y la
	// notificacion se cae al piso sin error: el Host cree que aviso y el asistente sigue
	// con la lista vieja.
	opciones.Capabilities ??= new();
	opciones.Capabilities.Tools ??= new();
	opciones.Capabilities.Tools.ListChanged = true;
})
	.WithStdioServerTransport()
	.WithTools<ToolsDelHost>()      // las propias del Host, con firma conocida en compilacion
	// Las de los modulos se contestan A MANO. El esquema de entrada sale del manifiesto, que
	// es un dato: sintetizar un delegate ataria la tool a una firma que el Host no puede
	// conocer, y por eso la version anterior salteaba las que tenian parametros.
	.WithListToolsHandler((peticion, ct) => PuenteDeTools.Listar(despacho, peticion, ct))
	.WithCallToolHandler((peticion, ct) => PuenteDeTools.Atender(despacho, peticion, ct));

// EL VIGILANTE del consentimiento. Va como servicio del host -y no como un hilo suelto-
// para arrancar y apagarse junto con el servidor, y para poder pedirle el McpServer a la
// inyeccion recien cuando hace falta avisar.
builder.Services.AddHostedService<VigilanteDeConsentimiento>();

IHost host = builder.Build();

// Los procesos hijos de los modulos se cierran cuando se cierra el catalogo.
await using (catalogo)
{
	await host.RunAsync();
}
