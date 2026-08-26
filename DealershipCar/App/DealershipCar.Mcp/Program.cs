using DealershipCar.Mcp;
using DealershipCar.Mcp.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// Servidor MCP de DealershipCar. Proceso local, SOLO LECTURA, sobre la misma carpeta de
// journal que escribe la app instalada.
//
// Por que hace falta un proceso: una pagina en el browser no puede hospedar un servidor MCP,
// porque no puede escuchar conexiones entrantes. No hay forma de evitarlo. Pero NO necesita
// hablar con el browser: lee la misma carpeta, y el journal YA es el protocolo.

// ---- LA LANDMINE. Con transporte stdio, STDOUT ES EL CANAL JSON-RPC.
//
// El replay de Puppeteer imprime su progreso con Console.Write ("1%2%3%...", y un
// "Starting ...'s Actor" en cada arranque). Medido en T0 y vuelto a medir en T7. En una consola
// eso es ruido; aca CORROMPE EL PROTOCOLO: esos bytes se meten entre los mensajes JSON-RPC y el
// cliente no puede parsear la respuesta.
//
// Se desvia ANTES de tocar el motor y para todo el proceso. Es la primera linea a proposito.
// stderr queda libre para diagnostico, que es donde el cliente MCP espera los logs.
Console.SetOut(Console.Error);

string folder = args.Length > 0 && args[0].Length > 0
	? args[0]
	: Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "DealershipCar-journal");

// EL VIN ES OPCIONAL Y NO SE PIDE SI NO HACE FALTA. El nombre del actor lo lleva, pero el VIN ya
// esta adentro del journal: exigirlo en la configuracion seria una segunda fuente de verdad para
// la identidad del vehiculo, y la que quedaria vieja es la de afuera. Sin el, se descubre de la
// carpeta. Se acepta igual para el caso en que una carpeta tenga mas de un vehiculo.
string vin = args.Length > 1 ? args[1] : "";

var reader = new JournalReader(folder, vin);

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton(reader);

builder.Services.AddMcpServer(options =>
{
	options.ServerInfo = new ModelContextProtocol.Protocol.Implementation
	{
		Name = "mantenimiento-del-vehiculo",
		Version = "0.1.0",
	};

	// Las instructions NO son el catalogo de tools: eso el cliente lo descubre solo con tools/list
	// y sus schemas. Esto es el WORKFLOW -- lo que la IA no puede deducir de la firma de un metodo:
	// de donde salen los datos, que significa un campo vacio, y que ella NO puede disparar un
	// refresh.
	options.ServerInstructions = Instructions(reader.Folder);
})
.WithStdioServerTransport()
.WithTools<DealershipCarTools>();

// Todo el logging a stderr. Un LogInformation que saliera por stdout romperia el canal.
builder.Logging.ClearProviders();
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

await builder.Build().RunAsync();

static string Instructions(string folder) => $"""
	You are the personal AI of the person using this computer, about THEIR OWN vehicle's
	maintenance schedule.

	Where the data comes from. The person has an app their dealership gave them. That app keeps
	its own record of their vehicle in a folder of theirs:
	{folder}
	This server READS that record. It never asks the dealership anything, and it cannot write:
	it is read-only, and that is structural, not a promise.

	What that means for you. When you answer, you are not reporting what some company's server
	says: you are reading the person's own record of their own car. If something is not there,
	the likeliest reason is that their app has not brought it in yet, not that it does not exist.

	WHAT YOU CAN SAY AND WHAT YOU CANNOT. The tools do not hand you raw numbers to interpret:
	they hand you where each maintenance operation STANDS and the reason the schedule itself
	composed for it - "the fluid change is past its distance limit and its deferral window has
	elapsed". That sentence is written by the domain that owns the rules. Say it. Do not reword
	it into template advice like "keep up with your maintenance": you have nowhere to get that
	from, and the person has heard it already.

	Two things the schedule distinguishes that look alike, and getting them backwards is the
	failure worth avoiding. DueSoon is a courtesy warning and is NEVER a required stop - do not
	present it as one. Overdue means past the grace period, and that is the one worth raising.
	An operation this vehicle never had scheduled reads NotDue as well, and its reason says so in
	plain words; that is not a gap in the data, it is a car that does not have that part.

	Distances carry a unit and the schedule never converts between them. Say the value with its
	unit, exactly as it comes.

	Everything is AS OF an instant, not as of now. evaluatedUpTo says when the schedule was last
	evaluated. If that instant is old, say so instead of presenting the answer as today's. The app
	evaluates when the person opens it; you cannot.

	How it updates. The app pulls new facts from the dealership when the person opens it or
	presses Refresh. You CANNOT trigger that, and it is not an oversight: this folder has exactly
	one writer - their app - and a second one would break the guarantee that makes their record
	replay the same way twice. If they expect to see something new and it is not there, ask them
	to open their app and press Refresh, then ask again. lastEventApplied tells you how far it has
	read: if it goes up, new facts came in.

	If hasData comes back false, the app has never been opened on this machine. Tell them to open
	it. It is not an error of yours, and not the dealership's.

	If lastEventKnown comes back false, this record was written by an older version of their app,
	which did not keep that count. lastEventApplied is then zero and means NOTHING - never report
	it as "no events read". The maintenance schedule itself is complete and current; read detail
	and tell them what it says. This server cannot fix it, because fixing it would mean writing to
	their record, and it does not write.

	If something needs attention, say so and recommend they take it to their workshop or
	dealership. Do not tell them it is safe to keep driving on an overdue safety operation.
	""";
