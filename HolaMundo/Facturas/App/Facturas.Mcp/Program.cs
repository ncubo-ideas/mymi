using Facturas.Mcp;
using Facturas.Mcp.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// Servidor MCP de APP1. Proceso local, solo lectura, sobre la carpeta donde la app
// instalada escribe su journal.
//
// Por que hace falta un proceso: una pagina en el browser no puede hospedar un servidor
// MCP, porque no puede escuchar conexiones entrantes. No hay forma de evitarlo. Pero NO
// necesita hablar con el browser: lee la misma carpeta.

// ---- LA LANDMINE. Con transporte stdio, stdout ES el canal JSON-RPC.
//
// El replay de Puppeteer imprime su progreso con Console.Write ("1%2%3%...", y un
// "Starting ...'s Actor" en cada arranque). Medido en esta maquina. En una consola eso
// es ruido; aca CORROMPE EL PROTOCOLO: esos bytes se meten entre los mensajes JSON-RPC y
// el cliente no puede parsear la respuesta.
//
// Se desvia ANTES de tocar el motor y para todo el proceso. stderr queda libre para
// diagnostico, que es donde el cliente MCP espera los logs.
Console.SetOut(Console.Error);

string carpeta = args.Length > 0 && args[0].Length > 0
    ? args[0]
    : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "APP1-facturas");

var lector = new LectorDelJournal(carpeta);

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton(lector);

builder.Services.AddMcpServer(opciones =>
{
    opciones.ServerInfo = new ModelContextProtocol.Protocol.Implementation
    {
        Name = "app1-facturas",
        Version = "0.1.0",
    };

    // Las instructions NO son el catalogo de tools: eso el cliente lo descubre solo con
    // tools/list y sus schemas. Esto es el WORKFLOW - lo que la IA no puede deducir de la
    // firma de un metodo.
    opciones.ServerInstructions = Instrucciones(lector.Carpeta);
})
.WithStdioServerTransport()
.WithTools<FacturasTools>();

// Todo el logging a stderr. Un LogInformation que saliera por stdout romperia el canal.
builder.Logging.ClearProviders();
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

await builder.Build().RunAsync();

static string Instrucciones(string carpeta) => $"""
    Sos la IA personal de la persona que usa esta computadora, sobre SUS facturas de compra.

    De donde salen los datos. La persona tiene instalada una app que le dio su proveedor.
    Esa app guarda su propio registro de facturas en una carpeta de ella:
    {carpeta}
    Este servidor LEE ese registro. No le pregunta nada al proveedor, y no puede escribir:
    es solo lectura.

    Que significa eso para vos. Cuando contestas, no estas reportando lo que dice un
    servidor de una empresa: estas leyendo el registro propio de la persona. Si algo no
    aparece, lo mas probable es que la app todavia no lo trajo, no que no exista.

    Como se actualiza. La app trae facturas nuevas del proveedor cuando la persona la abre
    o aprieta Refresh. Vos NO podes disparar eso. Si la persona espera ver algo nuevo y el
    total no cambio, pedile que abra su app y apriete Refresh, y volve a consultar.
    El campo ultimoEventoLeido te dice hasta donde leyo: si sube, entraron cosas nuevas.

    Si hayDatos viene en false, la app nunca se abrio en esta computadora. Decile que la
    abra; no es un error tuyo ni del proveedor.
    """;
