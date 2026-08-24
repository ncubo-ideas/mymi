using System.ComponentModel;
using System.Text.Json.Serialization;
using Facturas.App;
using ModelContextProtocol.Server;

namespace Facturas.Mcp.Tools;

// LA SUPERFICIE QUE VE LA IA. Una sola tool: el total.
//
// Es intencional en el sentido fuerte: la escribe el autor de la app, igual que un
// [HttpGet], y la app decide que expone. Deliberadamente NO existe una tool generica
// Query(puppet, operation, parameters): eso le daria a la IA una consola de DSL en vez
// de un dominio, tiraria a la basura el descubrimiento por schema, y expondria los
// puppets - desde afuera nadie tiene por que saber si el total salio de uno o de cinco.
// La unidad descubrible es la aplicacion.
//
// El [Description] NO es documentacion: ES la semantica que lee la IA, lo unico que
// tiene para decidir si esta tool responde la pregunta de la persona. Por eso esta
// redactado como la pregunta que una persona se hace sobre sus propios datos, no como
// el nombre de un metodo.
[McpServerToolType]
internal sealed class FacturasTools
{
	private readonly LectorDelJournal lector;

	public FacturasTools(LectorDelJournal lector)
	{
		ArgumentNullException.ThrowIfNull(lector);
		this.lector = lector;
	}

	// UseStructuredContent es el ultimo tramo de "sin strings en ninguna frontera
	// semantica". Sin el, el SDK serializa el record a JSON y lo manda como un bloque de
	// TEXTO: la IA recibe un string que tiene que volver a parsear, justo lo que el diseno
	// pide evitar. Con el, el resultado viaja en structuredContent y la tool publica un
	// outputSchema, asi que el decimal llega como decimal.
	[McpServerTool(Name = "total_de_facturas", UseStructuredContent = true)]
	[Description("Cuanto gasto en total la persona, sumando todas las facturas de compra que su "
		+ "app tiene registradas en esta computadora. Devuelve tambien cuantas facturas son y "
		+ "hasta que evento del proveedor leyo la app, que sirve para saber si esta al dia. "
		+ "Los datos salen del registro que la app escribio en la carpeta de la persona, no de "
		+ "ningun servidor.")]
	public TotalDeFacturas Total()
	{
		if (!lector.HayJournal)
			return new TotalDeFacturas
			{
				HayDatos = false,
				Carpeta = lector.Carpeta,
				Detalle = "La app todavia no registro nada en esta carpeta. Pedile a la persona que "
					+ "abra su app de facturas y apriete Refresh.",
			};

		// UNA sola rehidratacion para las tres preguntas. Separarlas en tres llamadas a
		// Consultar releeria el journal tres veces y, peor, podria mezclar dos estados si
		// la app escribe en el medio: el total de antes con el conteo de despues.
		return lector.Consultar(api => new TotalDeFacturas
		{
			HayDatos = true,
			Total = api.TotalDeFacturas(),
			CantidadDeFacturas = api.CantidadDeFacturas(),
			UltimoEventoLeido = api.UltimoEventoProcesado(),
			Carpeta = lector.Carpeta,
			Detalle = "",
		});
	}
}

// Tipo de retorno con forma, no un string: el SDK genera el schema de salida desde esto
// y la IA recibe structuredContent con un decimal de verdad. Es el ultimo tramo de "sin
// strings en ninguna frontera semantica".
internal sealed class TotalDeFacturas
{
	[JsonPropertyName("hayDatos")]
	[Description("false cuando la app todavia no registro nada; ahi el resto viene en cero.")]
	public bool HayDatos { get; init; }

	[JsonPropertyName("total")]
	[Description("Suma de todas las facturas registradas.")]
	public decimal Total { get; init; }

	[JsonPropertyName("cantidadDeFacturas")]
	[Description("Cuantas facturas hay registradas.")]
	public int CantidadDeFacturas { get; init; }

	[JsonPropertyName("ultimoEventoLeido")]
	[Description("Hasta que evento del proveedor leyo la app. Si la persona apreta Refresh y "
		+ "entraron facturas nuevas, este numero sube.")]
	public long UltimoEventoLeido { get; init; }

	[JsonPropertyName("carpeta")]
	[Description("La carpeta de la persona donde la app guarda su registro.")]
	public string Carpeta { get; init; } = "";

	[JsonPropertyName("detalle")]
	[Description("Vacio cuando todo esta bien; si no, que le conviene hacer a la persona.")]
	public string Detalle { get; init; } = "";
}
