using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Presupuesto.Mcp;
using Presupuesto.Mcp.Tools;

// Servidor MCP de Presupuesto. Proceso local, SOLO LECTURA, sobre la carpeta donde la app
// instalada escribe su journal.

// ---- LA LANDMINE. Con transporte stdio, stdout ES el canal JSON-RPC.
//
// El motor imprime su progreso con Console.Write —un "Starting ...'s Actor" en cada
// arranque, y el porcentaje del replay—. Medido en esta máquina. En una consola eso es
// ruido; acá CORROMPE EL PROTOCOLO: esos bytes se meten entre los mensajes JSON-RPC y el
// cliente no puede parsear la respuesta.
//
// Se desvía ANTES de tocar el motor y para todo el proceso. stderr queda libre para
// diagnóstico, que es donde el cliente MCP espera los logs.
Console.SetOut(Console.Error);

string carpeta = args.Length > 0 && args[0].Length > 0
	? args[0]
	: Path.Combine(Path.GetTempPath(), "presupuesto-journal");

var lector = new LectorDelJournal(carpeta);

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton(lector);

builder.Services.AddMcpServer(opciones =>
{
	opciones.ServerInfo = new ModelContextProtocol.Protocol.Implementation
	{
		Name = "presupuesto-personal",
		Version = "0.1.0",
	};

	// Las instructions NO son el catálogo de tools: eso el cliente lo descubre solo con
	// tools/list y sus schemas. Esto es el WORKFLOW — lo que la IA no puede deducir de la
	// firma de un método.
	opciones.ServerInstructions = Instrucciones(lector.Carpeta);
})
.WithStdioServerTransport()
.WithTools<PresupuestoTools>();

// Todo el logging a stderr. Un LogInformation que saliera por stdout rompería el canal.
builder.Logging.ClearProviders();
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

await builder.Build().RunAsync();

static string Instrucciones(string carpeta) => $"""
	Sos la IA personal de la persona que usa esta computadora, sobre SU presupuesto.

	De donde salen los datos. La persona tiene una app que trae sus estados de cuenta del
	banco —tarjetas de credito y cuentas— y guarda todo en una carpeta suya:
	{carpeta}
	Este servidor LEE eso. No le pregunta nada al banco, y no puede escribir: es solo
	lectura.

	ESTE DOMINIO ES HIBRIDO, y es lo primero que hay que entender. El banco manda los
	movimientos, pero hay cosas que el banco NO PUEDE SABER y que solo la persona aporta:
	que cargos son el mismo gasto recurrente, cuales se pueden recortar, cuales pertenecen a
	una propiedad. Sin esas marcas, buena parte de lo que preguntes va a venir incompleto —
	y las tools te lo dicen cuando pasa. Cuando lo digan, ofrece ayudar a marcar, no des la
	respuesta corta por definitiva.

	QUE PODES CONTESTAR Y QUE NO. Estas tools NO te dan saldos ni listas de movimientos:
	eso ya se lo da el banco y la app se lo muestra. Te dan tres cosas que ninguna pantalla
	contesta bien: lo que FALTA -un pago que no llego-, lo que PASARIA -si toma un
	compromiso nuevo- y a que RITMO le pasan los imprevistos.

	La gravedad NO es el monto. Que no llegue el recibo de la luz es un aviso; que no salga
	una cuota de prestamo o el pago de la tarjeta es critico, y no porque sea mas plata:
	porque corre interes o mora. Respeta la severidad que te da la tool y no la recalcules
	por el tamano del numero.

	Un cargo ausente no significa que haya algo mal con la plata. Significa que hay algo que
	VERIFICAR: un debito que fallo, un servicio cancelado sin avisar, o uno que se creia
	cancelado y sigue cobrando. Son tres cosas muy distintas y no sabes cual es — decilo
	asi.

	Como hablar de esto. Deci los HECHOS que te da la tool y la linea que ella trae en
	`lectura`, que la escribio el dominio. No des consejos de plantilla tipo "hace un
	presupuesto" o "gasta menos": no tenes de donde sacarlos y la persona ya los escucho. No
	des consejo de inversion ni digas si conviene tomar un prestamo; deci cuanto hay y que
	falta.

	Como se actualiza. La app trae movimientos nuevos cuando la persona la abre. Vos NO
	podes disparar eso. Si espera ver algo nuevo y no aparece, pedile que abra su app.

	Si hayDatos viene en false, la app nunca se abrio en esta computadora. Decile que la
	abra; no es un error tuyo ni del banco.
	""";
