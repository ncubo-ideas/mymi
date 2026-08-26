using System.ComponentModel;
using System.Text.Json.Serialization;
using Compras.App;
using Compras.App.Vistas;
using ModelContextProtocol.Server;

namespace Compras.Mcp.Tools;

// LA SUPERFICIE QUE VE LA IA. Cuatro tools: el total, la canasta de un mes, la
// comparacion entre meses y la garantia de un producto.
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
// redactado como la pregunta que una persona se hace sobre sus propias compras, no como
// el nombre de un metodo.
[McpServerToolType]
internal sealed class ComprasTools
{
	private readonly LectorDelJournal lector;

	public ComprasTools(LectorDelJournal lector)
	{
		ArgumentNullException.ThrowIfNull(lector);
		this.lector = lector;
	}

	[McpServerTool(Name = "total_de_compras", UseStructuredContent = true)]
	[Description("Cuanto gasto en total la persona en el supermercado, sumando todas las compras que "
		+ "su app tiene registradas en esta computadora. Devuelve tambien cuantas compras son y "
		+ "hasta que evento del supermercado leyo la app, que sirve para saber si esta al dia. "
		+ "Los datos salen del registro que la app escribio en la carpeta de la persona, no de "
		+ "ningun servidor.")]
	public TotalDeCompras Total()
	{
		if (!lector.HayJournal) return new TotalDeCompras { HayDatos = false, Carpeta = lector.Carpeta, Detalle = SinJournal };

		// UNA sola rehidratacion para las tres preguntas: separarlas releeria el journal
		// tres veces y podria mezclar dos estados si la app escribe en el medio.
		return lector.Consultar(api => new TotalDeCompras
		{
			HayDatos = true,
			Total = api.TotalDeCompras(),
			CantidadDeCompras = api.CantidadDeCompras(),
			UltimoEventoLeido = api.UltimoEventoProcesado(),
			Carpeta = lector.Carpeta,
			Detalle = "",
		});
	}

	[McpServerTool(Name = "canasta_del_mes", UseStructuredContent = true)]
	[Description("Como se compuso la canasta del supermercado de la persona en UN mes: cuanto gasto "
		+ "en alimentos recomendados (proteinas, frutas y verduras), cuanto en neutros (granos, "
		+ "lacteos) y cuanto en ocasionales (azucarados, snacks, aceites y grasas), mas lo no "
		+ "comestible aparte. La proporcionOcasional va de 0 a 1 y se calcula SOLO sobre lo "
		+ "comestible. Son hechos para razonar — por ejemplo, cruzarlos con examenes de salud si "
		+ "la persona lo pide — no un juicio sobre la persona.")]
	public CanastaDelMesVista Canasta(
		[Description("El anio, por ejemplo 2026.")] int anio,
		[Description("El mes, de 1 a 12.")] int mes)
	{
		if (!lector.HayJournal) return new CanastaDelMesVista();

		return lector.Consultar(api => api.Canasta(anio, mes));
	}

	[McpServerTool(Name = "comparar_canastas", UseStructuredContent = true)]
	[Description("Como CAMBIO la canasta de la persona entre dos meses: la proporcion de gasto "
		+ "ocasional (azucarados, snacks, grasas) antes y despues, y si crecio de verdad. Es la "
		+ "pregunta que ningun ticket suelto contesta. Si hayComparacion viene en false, alguno "
		+ "de los dos meses no tiene compras comestibles y no hay lectura que dar — decilo tal "
		+ "cual, no lo inventes.")]
	public CanastasComparadas Comparar(
		[Description("El anio del mes de referencia (antes).")] int anioAntes,
		[Description("El mes de referencia (antes), de 1 a 12.")] int mesAntes,
		[Description("El anio del mes a comparar (despues).")] int anioDespues,
		[Description("El mes a comparar (despues), de 1 a 12.")] int mesDespues)
	{
		if (!lector.HayJournal) return new CanastasComparadas();

		return lector.Consultar(api => api.Comparacion(anioAntes, mesAntes, anioDespues, mesDespues));
	}

	[McpServerTool(Name = "garantia_de_producto", UseStructuredContent = true)]
	[Description("Cuando compro la persona un producto y hasta cuando tiene garantia. Busca por "
		+ "nombre o sku (por ejemplo 'olla arrocera') en las compras registradas y devuelve el "
		+ "respaldo completo: numero de compra, fecha, meses de cobertura, fecha de vencimiento, "
		+ "cuantos dias le quedan y si esta vigente HOY. Si encontrada viene en false, o el "
		+ "producto no esta en el registro o no tiene garantia — son dos hechos legitimos, no un "
		+ "error. El campo paraSonia trae la lectura ya redactada por el dominio; queHacer viene "
		+ "vacio cuando no hay nada que hacer, y eso tambien es una respuesta.")]
	public GarantiaDeProducto Garantia(
		[Description("Nombre (o parte del nombre) o sku del producto, por ejemplo 'olla arrocera'.")] string producto)
	{
		if (!lector.HayJournal) return new GarantiaDeProducto();

		// El "hoy" lo pone la superficie: el dominio no lee el reloj.
		return lector.Consultar(api => api.Garantia(producto, DateTime.Now));
	}

	// ---------------------------------------------------- lo que NO esta (el hueco)
	//
	// Las dos de aca son las que justifican conectar una IA a este modulo. Las cuatro de
	// arriba contestan lo que la app YA muestra en pantalla: sirven de contexto, pero si
	// la IA solo pudiera eso, el demo no probaria nada. Estas dos miran lo que la
	// pantalla NO PUEDE renderizar, porque no es un dato del historial sino una
	// conclusion sobre el historial mas el calendario.

	[McpServerTool(Name = "que_dejaste_de_comprar", UseStructuredContent = true)]
	[Description("Que categorias de las que la persona reponia con regularidad HACE DEMASIADO QUE "
		+ "NO COMPRA — proteinas, frutas y verduras, lacteos, granos, limpieza — segun la "
		+ "periodicidad propia de cada una y la fecha de hoy. Es lo que NO esta en su historial, "
		+ "asi que no es la lista de compras que la app le muestra: ninguna pantalla puede "
		+ "renderizar una ausencia. Usala cuando pregunte cosas como '¿que me esta faltando?', "
		+ "'¿estoy comiendo peor?' o '¿que deje de comprar?'. Devuelve solo las atrasadas, de la "
		+ "mas atrasada a la menos; una lista vacia significa que esta al dia con todo. Los "
		+ "azucarados y los snacks NO se reportan aca a proposito: este modulo no le dice a nadie "
		+ "que hace un mes que no compra helado.")]
	public IReadOnlyList<AbastecimientoEnEspera> QueDejasteDeComprar()
	{
		if (!lector.HayJournal) return new List<AbastecimientoEnEspera>();

		return lector.Consultar(api => api.AbastecimientosPendientes(DateTime.Now));
	}

	[McpServerTool(Name = "garantias_por_vencer", UseStructuredContent = true)]
	[Description("Las garantias de la persona cuya VENTANA PARA RECLAMAR se esta cerrando, de la "
		+ "mas urgente a la menos, con cuantos dias le quedan y que le conviene hacer. No es la "
		+ "lista de garantias vigentes que la app muestra: son solo las que ya piden algo, y eso "
		+ "sale del plazo de cada una contra la fecha de hoy. Usala cuando pregunte '¿se me vence "
		+ "alguna garantia?', '¿tengo algo por reclamar?' o antes de que tire una factura. Una "
		+ "lista vacia significa que ninguna esta cerca de vencer.")]
	public IReadOnlyList<GarantiaDeProducto> GarantiasPorVencer()
	{
		if (!lector.HayJournal) return new List<GarantiaDeProducto>();

		return lector.Consultar(api => api.GarantiasPorVencer(DateTime.Now));
	}

	private const string SinJournal =
		"La app todavia no registro nada en esta carpeta. Pedile a la persona que abra su app "
		+ "del supermercado y aprete Refresh.";
}

// Tipo de retorno con forma, no un string: el SDK genera el schema de salida desde esto
// y la IA recibe structuredContent con un decimal de verdad.
internal sealed class TotalDeCompras
{
	[JsonPropertyName("hayDatos")]
	[Description("false cuando la app todavia no registro nada; ahi el resto viene en cero.")]
	public bool HayDatos { get; init; }

	[JsonPropertyName("total")]
	[Description("Suma de todas las compras registradas.")]
	public decimal Total { get; init; }

	[JsonPropertyName("cantidadDeCompras")]
	[Description("Cuantas compras hay registradas.")]
	public int CantidadDeCompras { get; init; }

	[JsonPropertyName("ultimoEventoLeido")]
	[Description("Hasta que evento del supermercado leyo la app. Si la persona aprieta Refresh y "
		+ "entraron compras nuevas, este numero sube.")]
	public long UltimoEventoLeido { get; init; }

	[JsonPropertyName("carpeta")]
	[Description("La carpeta de la persona donde la app guarda su registro.")]
	public string Carpeta { get; init; } = "";

	[JsonPropertyName("detalle")]
	[Description("Vacio cuando todo esta bien; si no, que le conviene hacer a la persona.")]
	public string Detalle { get; init; } = "";
}
