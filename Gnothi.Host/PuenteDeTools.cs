using System.Text.Json;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Gnothi.Host;

// LA TRADUCCION: de una tool declarada en un manifiesto a una tool MCP publicada.
//
// Esto es todo el "proxy". Gnothi no inventa semantica: toma el nombre, el titulo y la
// descripcion que la institucion escribio, les pone el prefijo del modulo, y arma una
// tool cuyo cuerpo es una sola linea -pedirle al despacho que atienda.
//
// LA DESCRIPCION VIAJA TAL CUAL. No es documentacion: es lo unico que el asistente tiene
// para decidir si esta tool contesta la pregunta de la persona. Si el Host la reescribiera
// estaria opinando sobre un dominio que no entiende.
//
// POR QUE SE CONTESTA tools/list Y tools/call A MANO, y no se sintetiza un delegate:
//
// McpServerTool.Create(delegate, ...) deriva el inputSchema de la FIRMA del delegate, asi
// que solo puede publicar tools cuya forma se conozca en compilacion. El Host, por diseño,
// no conoce los tipos de ningun modulo: la version anterior de este archivo sintetizaba un
// Func<CancellationToken, Task<CallToolResult>> -sin parametros- y por eso salteaba con
// aviso toda tool que tuviera alguno. La estrategia peleaba con el objetivo.
//
// Contestando los dos verbos a mano, el esquema es un dato y no una firma: sale del
// manifiesto y el Host lo publica. Es el camino que el comentario anterior anotaba como
// pendiente.
//
// LO QUE ESTA VERSION CUBRE: parametros simples de texto, que es lo que el manifiesto
// modela hoy (nombre, tipo, descripcion). LO QUE NO: enums, opcionales, anidados. El
// modulo de examenes publica su analito como un ENUM de cuatro valores -medido pidiendole
// su tools/list- y el manifiesto no lo sabe expresar, asi que los valores validos se
// repiten en la descripcion. Reformular el manifiesto para llevar el esquema es una
// decision de formato, y esta abierta.
internal static class PuenteDeTools
{
	// El indice de nombre publico -> quien lo aporta. Se arma desde el catalogo, que ya
	// viene filtrado por consentimiento: una tool apagada no entra al indice, asi que no
	// se publica y no se puede llamar.
	//
	// NO se parte el nombre publico por el guion bajo para volver al par: 'total_de_facturas'
	// tiene tres. El par se guarda cuando se arma el nombre, que es el unico momento en que
	// se lo sabe sin ambiguedad.
	private static Dictionary<string, (Modulo modulo, ToolDeclarada tool)> Indice(Despacho despacho)
	{
		Dictionary<string, (Modulo, ToolDeclarada)> indice = new(StringComparer.Ordinal);

		foreach ((Modulo m, ToolDeclarada t) in despacho.Catalogo())
			indice[Modulo.NombrePublico(m.Nombre, t.Nombre)] = (m, t);

		return indice;
	}

	// tools/list. Devuelve SOLO las tools de los modulos: las propias del Host viven en la
	// coleccion del servidor y el SDK las agrega a esta lista.
	public static ValueTask<ListToolsResult> Listar(
		Despacho despacho, RequestContext<ListToolsRequestParams> _, CancellationToken __)
	{
		List<Tool> tools = [];

		foreach ((string nombrePublico, (Modulo modulo, ToolDeclarada declarada)) in Indice(despacho))
		{
			tools.Add(new Tool
			{
				Name = nombrePublico,
				Title = string.IsNullOrWhiteSpace(declarada.Titulo) ? null : declarada.Titulo,
				Description = declarada.Descripcion,
				InputSchema = EsquemaDeEntrada(declarada),
				Annotations = new ToolAnnotations { ReadOnlyHint = declarada.SoloLectura },
			});
		}

		return ValueTask.FromResult(new ListToolsResult { Tools = tools });
	}

	// tools/call. Resuelve el nombre publico contra el indice y delega en el despacho.
	//
	// Un nombre que no esta en el indice NO es un error del Host: es una tool de otro
	// proveedor -las 'gnothi_' propias- o una que la persona apago. Se contesta que no
	// existe, que es lo mismo que contestaria el SDK, y sin decir cual de las dos cosas es:
	// distinguirlas filtraria que la tool existe y esta apagada.
	public static async ValueTask<CallToolResult> Atender(
		Despacho despacho, RequestContext<CallToolRequestParams> peticion, CancellationToken ct)
	{
		string nombre = peticion.Params?.Name ?? "";

		if (!Indice(despacho).TryGetValue(nombre, out (Modulo modulo, ToolDeclarada tool) par))
			throw new McpProtocolException($"Unknown tool: '{nombre}'");

		Respuesta r = await despacho.Atender(
			new Peticion(par.modulo.Nombre, par.tool.Nombre, Argumentos(peticion.Params)), ct);

		if (!r.Autorizada)
			return new CallToolResult
			{
				IsError = true,
				Content = [new TextContentBlock { Text = r.Motivo }],
			};

		// El CallToolResult del modulo se devuelve ENTERO y sin tocar, con su structuredContent
		// y su _meta. El Host es cartero.
		return (CallToolResult)r.Contenido!;
	}

	// Los argumentos llegan como JsonElement y se pasan tal cual: el Host no los interpreta.
	// Si el modulo espera un enum y le llega un texto que no corresponde, el que se queja es
	// el modulo, que es el unico que sabe cuales son sus valores.
	private static IReadOnlyDictionary<string, object?>? Argumentos(CallToolRequestParams? p)
	{
		if (p?.Arguments is null || p.Arguments.Count == 0) return null;

		Dictionary<string, object?> args = [];
		foreach (KeyValuePair<string, JsonElement> par in p.Arguments) args[par.Key] = par.Value;
		return args;
	}

	// El esquema de entrada, armado desde lo que el manifiesto declara.
	//
	// Todos los parametros quedan requeridos porque el manifiesto no tiene forma de decir
	// que uno es opcional. Es una simplificacion visible: cuando haga falta un opcional, el
	// manifiesto va a tener que crecer, y es mejor que se note ahora que descubrirlo cuando
	// una tool falle.
	private static JsonElement EsquemaDeEntrada(ToolDeclarada declarada)
	{
		Dictionary<string, object> propiedades = [];

		foreach (ParametroDeclarado p in declarada.Entrada)
		{
			propiedades[p.Nombre] = new Dictionary<string, object>
			{
				// Todo se publica como texto. El manifiesto solo modela tipos simples, y
				// mentir sobre el tipo seria peor que simplificarlo: un esquema que dice
				// 'number' sobre algo que no lo es hace que el cliente rechace la llamada
				// antes de que el modulo pueda opinar.
				["type"] = "string",
				["description"] = p.Descripcion,
			};
		}

		Dictionary<string, object> esquema = new()
		{
			["type"] = "object",
			["properties"] = propiedades,
		};

		if (declarada.Entrada.Count > 0)
			esquema["required"] = declarada.Entrada.Select(p => p.Nombre).ToArray();

		return JsonSerializer.SerializeToElement(esquema);
	}
}
