using System.ComponentModel;
using System.Text.Json.Serialization;
using ModelContextProtocol.Server;

namespace Gnothi.Host;

// LAS TOOLS PROPIAS DE GNOTHI. Contestan lo que NINGUN modulo puede contestar de si mismo:
// cuantos hay, cuanto ocupan medidos desde afuera, y cuanto les pregunto el asistente.
//
// Deliberadamente NO hay una tool para encender, apagar ni cambiar permisos. El
// consentimiento es de la persona; si el asistente pudiera modificarlo por MCP, la
// autoridad de consentimiento pasaria al modelo. Se cambia editando instalacion.json.
[McpServerToolType]
internal sealed class ToolsDelHost
{
	private readonly Despacho despacho;

	public ToolsDelHost(Despacho despacho) => this.despacho = despacho;

	[McpServerTool(Name = "gnothi_modulos_instalados", UseStructuredContent = true, ReadOnly = true)]
	[Description("Que modulos tiene instalados la persona en este dispositivo, cuanto ocupa el registro de cada uno "
		+ "medido por el Host desde afuera, si estan encendidos, y cuantas consultas de asistente recibio cada uno. "
		+ "Usala cuando te pregunten que hay disponible o de donde salen los datos.")]
	public ModulosInstalados Modulos()
	{
		IReadOnlyDictionary<string, int> consultas = despacho.Medidor.Consultas();

		return new ModulosInstalados
		{
			Cantidad = despacho.Modulos.Modulos.Count,
			Modulos = [.. despacho.Modulos.Modulos.Select(m => new ModuloResumen
			{
				Nombre = m.Nombre,
				Titulo = m.Manifiesto.Titulo,
				Institucion = m.Manifiesto.Institucion,
				Encendido = m.Instalacion.Encendido,
				ToolsPublicadas = m.ToolsHabilitadas().Count(),
				ToolsApagadas = [.. m.Instalacion.ToolsApagadas],
				BytesDelRegistro = m.BytesEnDisco(),
				Levantado = m.Levantado,
				ConsultasDelAsistente = consultas
					.Where(kv => kv.Key.StartsWith(m.Nombre + "/", StringComparison.Ordinal))
					.Sum(kv => kv.Value),
			})],
		};
	}

	// LA VERIFICACION: declarado contra real.
	//
	// El catalogo se arma del manifiesto -barato, sin despertar nada. Esta tool hace lo
	// caro: levanta el modulo, le pide su tools/list POR PROTOCOLO, y compara. Una tool que
	// el modulo tiene y no declaro es la senal interesante.
	[McpServerTool(Name = "gnothi_verificar_modulo", UseStructuredContent = true, ReadOnly = true)]
	[Description("Compara lo que un modulo DECLARA en su manifiesto contra lo que realmente expone al ser consultado. "
		+ "Sirve para saber si un modulo esconde capacidades que no declaro. Recibe el nombre del modulo.")]
	public async Task<Verificacion> Verificar(
		[Description("El nombre del modulo, por ejemplo 'facturas'.")] string modulo,
		CancellationToken ct)
	{
		Modulo? m = despacho.Modulos.Buscar(modulo);
		if (m is null)
			return new Verificacion { Modulo = modulo, Coincide = false, Detalle = $"no hay un modulo instalado llamado '{modulo}'" };

		string[] declaradas = [.. m.Manifiesto.Tools.Select(t => t.Nombre).Order()];

		try
		{
			string[] reales = [.. (await m.ToolsRealesDelModulo(ct)).Order()];
			string[] noDeclaradas = [.. reales.Except(declaradas)];
			string[] inexistentes = [.. declaradas.Except(reales)];

			return new Verificacion
			{
				Modulo = m.Nombre,
				Declaradas = declaradas,
				Reales = reales,
				NoDeclaradas = noDeclaradas,
				DeclaradasQueNoExisten = inexistentes,
				Coincide = noDeclaradas.Length == 0 && inexistentes.Length == 0,
				Detalle = noDeclaradas.Length > 0
					? "el modulo expone tools que no declaro en su manifiesto"
					: inexistentes.Length > 0 ? "el manifiesto declara tools que el modulo no tiene" : "",
			};
		}
		catch (Exception e)
		{
			return new Verificacion { Modulo = m.Nombre, Declaradas = declaradas, Coincide = false, Detalle = $"no se pudo consultar al modulo: {e.GetType().Name}: {e.Message}" };
		}
	}
}

internal sealed class ModulosInstalados
{
	[JsonPropertyName("cantidad")] public int Cantidad { get; init; }
	[JsonPropertyName("modulos")] public IReadOnlyList<ModuloResumen> Modulos { get; init; } = [];
}

internal sealed class ModuloResumen
{
	[JsonPropertyName("nombre")] public string Nombre { get; init; } = "";
	[JsonPropertyName("titulo")] public string Titulo { get; init; } = "";
	[JsonPropertyName("institucion")] public string Institucion { get; init; } = "";
	[JsonPropertyName("encendido")] public bool Encendido { get; init; }
	[JsonPropertyName("toolsPublicadas")] public int ToolsPublicadas { get; init; }
	[JsonPropertyName("toolsApagadas")] public IReadOnlyList<string> ToolsApagadas { get; init; } = [];
	[JsonPropertyName("bytesDelRegistro")]
	[Description("Medido por el Host con stat sobre la carpeta que el concedio, no reportado por el modulo.")]
	public long BytesDelRegistro { get; init; }
	[JsonPropertyName("levantado")] public bool Levantado { get; init; }
	[JsonPropertyName("consultasDelAsistente")] public int ConsultasDelAsistente { get; init; }
}

internal sealed class Verificacion
{
	[JsonPropertyName("modulo")] public string Modulo { get; init; } = "";
	[JsonPropertyName("coincide")] public bool Coincide { get; init; }
	[JsonPropertyName("declaradas")] public IReadOnlyList<string> Declaradas { get; init; } = [];
	[JsonPropertyName("reales")] public IReadOnlyList<string> Reales { get; init; } = [];
	[JsonPropertyName("noDeclaradas")] public IReadOnlyList<string> NoDeclaradas { get; init; } = [];
	[JsonPropertyName("declaradasQueNoExisten")] public IReadOnlyList<string> DeclaradasQueNoExisten { get; init; } = [];
	[JsonPropertyName("detalle")] public string Detalle { get; init; } = "";
}
