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
internal static class PuenteDeTools
{
	public static IReadOnlyList<McpServerTool> Sintetizar(Despacho despacho, out List<string> avisos)
	{
		List<McpServerTool> tools = [];
		avisos = [];

		foreach ((Modulo modulo, ToolDeclarada declarada) in despacho.Catalogo())
		{
			// LIMITE CONOCIDO DE ESTE POC: solo tools sin parametros.
			//
			// Sintetizar un delegate con N parametros tipados en runtime pide arboles de
			// expresion, y el camino correcto para el caso general es manejar tools/call a
			// mano (McpServerHandlers.CallToolHandler) en vez de sintetizar delegates. Se
			// deja afuera con aviso en vez de aceptarlo y fallar en la invocacion: una tool
			// publicada que no se puede llamar es peor que una tool ausente.
			if (declarada.Entrada.Count > 0)
			{
				avisos.Add($"{modulo.Nombre}/{declarada.Nombre}: tiene parametros; este POC solo publica tools sin parametros");
				continue;
			}

			string nombreModulo = modulo.Nombre;
			string nombreTool = declarada.Nombre;

			// El cuerpo. Notese que NO toca el contenido: recibe el CallToolResult del
			// modulo y lo devuelve. El Host es cartero.
			Func<CancellationToken, Task<CallToolResult>> cuerpo = async ct =>
			{
				Respuesta r = await despacho.Atender(new Peticion(nombreModulo, nombreTool, null), ct);

				if (!r.Autorizada)
					return new CallToolResult
					{
						IsError = true,
						Content = [new TextContentBlock { Text = r.Motivo }],
					};

				return (CallToolResult)r.Contenido!;
			};

			tools.Add(McpServerTool.Create(cuerpo, new McpServerToolCreateOptions
			{
				Name = Modulo.NombrePublico(nombreModulo, nombreTool),
				Title = string.IsNullOrWhiteSpace(declarada.Titulo) ? null : declarada.Titulo,
				Description = declarada.Descripcion,
				ReadOnly = declarada.SoloLectura,
			}));
		}

		return tools;
	}
}
