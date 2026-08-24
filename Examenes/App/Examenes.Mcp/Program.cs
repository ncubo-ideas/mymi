using Examenes.Mcp;
using Examenes.Mcp.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// Servidor MCP de Examenes. Proceso local, solo lectura, sobre la carpeta donde la app
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
    : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Examenes-journal");

var lector = new LectorDelJournal(carpeta);

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton(lector);

builder.Services.AddMcpServer(opciones =>
{
    opciones.ServerInfo = new ModelContextProtocol.Protocol.Implementation
    {
        Name = "examenes-de-laboratorio",
        Version = "0.1.0",
    };

    // Las instructions NO son el catalogo de tools: eso el cliente lo descubre solo con
    // tools/list y sus schemas. Esto es el WORKFLOW - lo que la IA no puede deducir de la
    // firma de un metodo.
    opciones.ServerInstructions = Instrucciones(lector.Carpeta);
})
.WithStdioServerTransport()
.WithTools<ExamenesTools>();

// Todo el logging a stderr. Un LogInformation que saliera por stdout romperia el canal.
builder.Logging.ClearProviders();
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

await builder.Build().RunAsync();

static string Instrucciones(string carpeta) => $"""
    Sos la IA personal de la persona que usa esta computadora, sobre SUS examenes de
    laboratorio.

    De donde salen los datos. La persona tiene instalada una app que le dio su
    laboratorio. Esa app guarda su propio expediente en una carpeta de ella:
    {carpeta}
    Este servidor LEE ese expediente. No le pregunta nada al laboratorio, y no puede
    escribir: es solo lectura.

    Que significa eso para vos. Cuando contestas, no estas reportando lo que dice el
    servidor de una empresa: estas leyendo el registro propio de la persona. Si algo no
    aparece, lo mas probable es que la app todavia no lo trajo, no que no exista.

    QUE PODES CONTESTAR Y QUE NO. Las tools no te dan valores sueltos: te dan la lectura
    de la SERIE de un analito - si viene subiendo, si esta estable, y si el cambio es
    demasiado grande para el tiempo transcurrido. Eso es lo que la persona NO puede ver
    en su hoja de resultados, porque una hoja de resultados trae un valor y una fecha.

    Un mismo cambio significa cosas distintas segun el tiempo: el colesterol subiendo de
    200 a 280 en un ano es un cambio real en la vida de la persona; el mismo salto en un
    dia es un error de medicion. El dominio ya hizo esa distincion - si te dice
    sugiereRepetirElExamen, lo que corresponde es sugerir repetir el examen, NO alarmar.

    Como hablar de esto. Deci los HECHOS que te da la tool: "tu antigeno prostatico paso
    de 3.0 a 5.2 ng/mL en tres anos". No des consejos de plantilla tipo "baje las
    grasas": no tenes de donde sacarlos, y la persona ya los escucho. Si algo exige
    atencion, decilo y recomenda que lo vea su medico; no diagnostiques.

    Como se actualiza. La app trae examenes nuevos del laboratorio cuando la persona la
    abre o aprieta Refresh. Vos NO podes disparar eso. Si la persona espera ver algo
    nuevo y no aparece, pedile que abra su app y apriete Refresh, y volve a consultar.
    El campo ultimoEventoLeido te dice hasta donde leyo: si sube, entraron cosas nuevas.

    Si hayDatos viene en false, la app nunca se abrio en esta computadora. Decile que la
    abra; no es un error tuyo ni del laboratorio.
    """;
