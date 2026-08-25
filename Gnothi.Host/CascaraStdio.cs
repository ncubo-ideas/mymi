using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Gnothi.Host;

// LA CASCARA: como el Host levanta un modulo y le habla.
//
// En este POC el modulo trae su propio servidor MCP, asi que la cascara es proceso hijo +
// stdio, y el Host actua de CLIENTE MCP. Eso hace el POC honesto en el eslabon que
// importa -Claude no sabe que hay dos saltos- a costa de que el modulo tenga transporte,
// que es lo que la arquitectura destino le quita.
//
// LO QUE ESTA CLASE ES: el UNICO lugar de Gnothi que sabe COMO se ejecuta un modulo. Nada
// mas la nombra. La Etapa 2 reemplaza este archivo por uno que instancia un runtime wasm
// con --dir y no toca ninguna otra linea del Host. Si para cambiar de hospedaje hubiera
// que editar el despacho o el catalogo, la abstraccion estaria mal puesta.
//
// ARRANQUE PEREZOSO, a proposito: leer veinte manifiestos es leer veinte json; arrancar
// veinte modulos es veinte replays de journal. El catalogo NO necesita el modulo vivo.
internal sealed class CascaraStdio : IAsyncDisposable
{
	private readonly string nombre;
	private readonly Cascara cascara;
	private readonly string carpetaConcedida;
	private readonly SemaphoreSlim candado = new(1, 1);

	private McpClient? cliente;

	public CascaraStdio(string nombre, Cascara cascara, string carpetaConcedida)
	{
		this.nombre = nombre;
		this.cascara = cascara;
		this.carpetaConcedida = carpetaConcedida;
	}

	public bool Levantado => cliente is not null;

	// Un solo candado alrededor del arranque: dos tool calls concurrentes sobre un modulo
	// dormido levantarian dos procesos, y el segundo se quedaria colgado sin que nadie lo
	// cierre.
	private async Task<McpClient> Levantar(CancellationToken ct)
	{
		if (cliente is not null) return cliente;

		await candado.WaitAsync(ct);
		try
		{
			if (cliente is not null) return cliente;

			// La carpeta concedida se pasa como ARGUMENTO, no como variable de ambiente ni
			// como algo que el modulo pueda mirar del sistema. Es el Host el que dice donde.
			string[] argumentos = [.. cascara.Argumentos, carpetaConcedida];

			var transporte = new StdioClientTransport(new StdioClientTransportOptions
			{
				Name = nombre,
				Command = cascara.Comando,
				Arguments = argumentos,
				// El stderr del hijo NO es el nuestro: se reenvia al nuestro con prefijo, para
				// que el ruido de un modulo sea rastreable hasta el modulo y, sobre todo, para
				// que nunca toque stdout — que es el canal del protocolo.
				StandardErrorLines = linea => Console.Error.WriteLine($"[modulo:{nombre}] {linea}"),
			});

			cliente = await McpClient.CreateAsync(transporte, cancellationToken: ct);
			return cliente;
		}
		finally { candado.Release(); }
	}

	// Lo que el modulo dice de si mismo POR PROTOCOLO. No se usa para armar el catalogo
	// -para eso esta el manifiesto- sino para VERIFICARLO: declarado contra real.
	//
	// Tambien desaloja: preguntarle al modulo quien es no es razon para dejarlo con la
	// carpeta de la persona abierta.
	public async Task<IReadOnlyList<McpClientTool>> ToolsReales(CancellationToken ct)
	{
		try
		{
			return [.. await (await Levantar(ct)).ListToolsAsync(cancellationToken: ct)];
		}
		finally { await Desalojar(); }
	}

	// Se devuelve el CallToolResult DEL MODULO, entero y sin tocar.
	//
	// El Host no parsea ni reformatea la respuesta, y no es pereza: si transcodificara
	// tendria que entender el dominio, y cualquier perdida en la conversion se volveria un
	// bug del Host adentro de los datos de la persona.
	//
	// SE DESALOJA DESPUES DE CADA LLAMADA, y esto costo un bug para entenderlo.
	//
	// Antes el modulo quedaba levantado para siempre desde la primera llamada. Parecia
	// eficiente: una sola hidratacion, respuestas rapidas. Pero un modulo vivo TIENE LA
	// CARPETA DEL JOURNAL ABIERTA, y hidratar un actor no es una lectura pasiva: crea el
	// layout completo (outbox, materialization, skips, reactions, followers, index,
	// elision) y toca meta.bin.
	//
	// Con la app de la persona abierta en el browser sobre la MISMA carpeta, eso le
	// invalida los handles que tenia cacheados, y su siguiente flush muere con
	// InvalidStateError: "el estado cambio desde que se leyo del disco". Son dos
	// escritores sobre un journal, que es exactamente lo que el modelo prohibe.
	//
	// Desalojar no cierra la ventana del todo -- durante la llamada los dos coexisten --
	// pero la reduce de "para siempre" a "los milisegundos de una consulta". La ventana se
	// cierra de verdad recien cuando el modulo deja de ser un proceso aparte y lo carga el
	// Host, que es la arquitectura destino.
	//
	// Y de paso es lo correcto por otra razon: N modulos vivos en un dispositivo es RAM
	// real, y el modulo no inicia nada por su cuenta, asi que no hay nada que mantener
	// despierto.
	public async Task<CallToolResult> Invocar(
		string tool, IReadOnlyDictionary<string, object?>? argumentos, CancellationToken ct)
	{
		try
		{
			return await (await Levantar(ct)).CallToolAsync(tool, argumentos, cancellationToken: ct);
		}
		finally { await Desalojar(); }
	}

	// Cierra el proceso del modulo y suelta la carpeta. No puede tirar: se llama desde un
	// finally, y tapar el error original con uno de limpieza seria peor que el problema.
	private async Task Desalojar()
	{
		await candado.WaitAsync();
		try
		{
			if (cliente is null) return;
			try { await cliente.DisposeAsync(); } catch { /* ya se estaba muriendo */ }
			cliente = null;
		}
		finally { candado.Release(); }
	}

	public async ValueTask DisposeAsync()
	{
		if (cliente is not null) await cliente.DisposeAsync();
		candado.Dispose();
	}
}
