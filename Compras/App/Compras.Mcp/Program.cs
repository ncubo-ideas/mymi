using Compras.Mcp;
using Compras.Mcp.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// Servidor MCP de Compras. Proceso local, solo lectura, sobre la carpeta donde la app
// instalada escribe su journal.
//
// Por que hace falta un proceso: una pagina en el browser no puede hospedar un servidor
// MCP, porque no puede escuchar conexiones entrantes. No hay forma de evitarlo. Pero NO
// necesita hablar con el browser: lee la misma carpeta.

// ---- LA LANDMINE. Con transporte stdio, stdout ES el canal JSON-RPC.
//
// El replay de Puppeteer imprime su progreso con Console.Write ("1%2%3%...", y un
// "Starting ...'s Actor" en cada arranque). En una consola eso es ruido; aca CORROMPE
// EL PROTOCOLO. Se desvia ANTES de tocar el motor y para todo el proceso. stderr queda
// libre para diagnostico, que es donde el cliente MCP espera los logs.
Console.SetOut(Console.Error);

string carpeta = args.Length > 0 && args[0].Length > 0
    ? args[0]
    : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Compras-journal");

var lector = new LectorDelJournal(carpeta);

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton(lector);

builder.Services.AddMcpServer(opciones =>
{
    opciones.ServerInfo = new ModelContextProtocol.Protocol.Implementation
    {
        Name = "compras-del-supermercado",
        Version = "0.1.0",
    };

    // Las instructions NO son el catalogo de tools: eso el cliente lo descubre solo con
    // tools/list y sus schemas. Esto es el WORKFLOW - lo que la IA no puede deducir de la
    // firma de un metodo.
    opciones.ServerInstructions = Instrucciones(lector.Carpeta);
})
.WithStdioServerTransport()
.WithTools<ComprasTools>();

// Todo el logging a stderr. Un LogInformation que saliera por stdout romperia el canal.
builder.Logging.ClearProviders();
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

await builder.Build().RunAsync();

static string Instrucciones(string carpeta) => $"""
    Sos la IA personal de la persona que usa esta computadora, sobre SUS compras del
    supermercado.

    De donde salen los datos. La persona tiene instalada una app que le dio su
    supermercado. Esa app guarda su propio historial de compras en una carpeta de ella:
    {carpeta}
    Este servidor LEE ese historial. No le pregunta nada al supermercado, y no puede
    escribir: es solo lectura.

    Que significa eso para vos. Cuando contestas, no estas reportando lo que dice el
    servidor de una empresa: estas leyendo el registro propio de la persona. Si algo no
    aparece, lo mas probable es que la app todavia no lo trajo, no que no exista.

    QUE PODES CONTESTAR Y QUE NO. Las tools no te dan tickets sueltos: te dan la CANASTA
    de un mes sumada por perfil de consumo (recomendado / neutro / ocasional), como
    cambio entre dos meses, y la garantia de un producto con su respaldo completo. Eso
    es lo que la persona NO puede ver en sus tickets de papel.

    Sobre la canasta: los perfiles son del catalogo del supermercado (el helado y las
    sodas son "ocasional", el pollo y las verduras "recomendado"). Deci los HECHOS:
    "tu canasta de agosto 2025 era 9% ocasional; la de agosto 2026 es 97%". Si la
    persona ademas te da acceso a otros negocios (por ejemplo sus examenes de
    laboratorio), podes poner los hechos uno al lado del otro — pero el cruce hacelo
    solo si la persona lo pide, y sin diagnosticar: recomendale hablarlo con su medico.
    No des consejos de plantilla tipo "compra menos helado": la persona ya los escucho.

    Sobre la garantia: la tool devuelve el respaldo completo (compra, fecha, meses,
    vencimiento, vigencia). Si encontrada viene en false, el producto no esta en el
    registro o no tiene garantia — decilo tal cual.

    Como se actualiza. La app trae compras nuevas del supermercado cuando la persona la
    abre o aprieta Refresh. Vos NO podes disparar eso. Si la persona espera ver algo
    nuevo y no aparece, pedile que abra su app y aprete Refresh, y volve a consultar.
    El campo ultimoEventoLeido te dice hasta donde leyo: si sube, entraron cosas nuevas.

    Si hayDatos viene en false, la app nunca se abrio en esta computadora. Decile que la
    abra; no es un error tuyo ni del supermercado.
    """;
