using System.ComponentModel;
using System.Text.Json.Serialization;
using Compras.App;
using Compras.App.Vistas;
using ModelContextProtocol.Server;

namespace Compras.Mcp.Tools;

// LA SUPERFICIE QUE VE LA IA. El total, la firma de su semana, el mes en que la canasta
// cambio, cada cuanto compra un producto y que cambio en su rutina, la canasta de un mes,
// la comparacion entre meses, el hueco, la serie de un consumo y las garantias.
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
	[Description("Cuanto gasto en total la persona en el supermercado, cuantas compras tiene "
		+ "registradas, y DESDE QUE MES HASTA QUE MES hay compras en su historial. Ese rango "
		+ "-primerMes, ultimoMes, mesesConCompras, en formato aaaa-mm- es el contexto para elegir "
		+ "que meses pedirle a las otras tools: pedir un mes fuera del rango devuelve vacio, y "
		+ "eso no significa que la persona no tenga datos. Devuelve tambien hasta que evento del "
		+ "supermercado leyo la app, que sirve para saber si esta al dia. Los datos salen del "
		+ "registro que la app escribio en la carpeta de la persona, no de ningun servidor.")]
	public TotalDeCompras Total()
	{
		if (!lector.HayJournal) return new TotalDeCompras { HayDatos = false, Carpeta = lector.Carpeta, Detalle = SinJournal };

		// UNA sola rehidratacion para todas las preguntas: separarlas releeria el journal
		// varias veces y podria mezclar dos estados si la app escribe en el medio.
		return lector.Consultar(api =>
		{
			RangoDelHistorialVista rango = api.Rango();
			return new TotalDeCompras
			{
				HayDatos = true,
				Total = api.TotalDeCompras(),
				CantidadDeCompras = api.CantidadDeCompras(),
				UltimoEventoLeido = api.UltimoEventoProcesado(),
				PrimerMes = rango.PrimerMes,
				UltimoMes = rango.UltimoMes,
				MesesConCompras = rango.MesesConCompras,
				Carpeta = lector.Carpeta,
				Detalle = "",
			};
		});
	}

	[McpServerTool(Name = "como_viene_mi_canasta", UseStructuredContent = true)]
	[Description("Como viene la canasta de la persona SIN TENER QUE ELEGIR MESES: compara el ultimo "
		+ "mes que tiene compras contra el mismo mes del anio anterior, que es la lectura que la "
		+ "app le muestra. Usala para preguntas generales como '¿como vengo?', '¿en que se me va "
		+ "la plata en el super?' o '¿que cambio en mis compras?'. Preferila sobre comparar_canastas "
		+ "salvo que la persona nombre dos meses concretos. Trae los dos totales y, en diferencias, "
		+ "DE DONDE sale el cambio pasillo por pasillo: mira las que tienen explicaElCambio en true, "
		+ "que son las que de verdad mueven la aguja, y esNuevo para los pasillos que antes no "
		+ "aparecian. Usa titulo y nunca categoria al hablarle a la persona. Este modulo reporta "
		+ "hechos de compra y NO juzga la alimentacion: si la persona quiere saber que significan "
		+ "para su salud, cruzalos vos con sus otras fuentes. Si hayComparacion viene en false, no "
		+ "hay dos meses comparables todavia y hay que decirlo tal cual.")]
	public CanastasComparadas ComoVieneMiCanasta()
	{
		if (!lector.HayJournal) return new CanastasComparadas();

		return lector.Consultar(api => api.ComparacionConElAnioAnterior());
	}

	[McpServerTool(Name = "la_firma_de_mi_canasta", UseStructuredContent = true)]
	[Description("LO QUE NO FALLA en las compras de la persona: los productos que vuelven compra "
		+ "tras compra. Usala para '¿que compro yo?', '¿que es lo mio?', '¿cual es mi canasta "
		+ "tipica?' — y tambien como CONTEXTO antes de contestar cualquier otra cosa sobre sus "
		+ "compras, porque dice de que esta hecha su rutina. Un producto entra si aparece en al "
		+ "menos la MITAD de las compras miradas, y cada uno trae en cuantas de cuantas aparece; "
		+ "estaEnTodas marca los que no fallaron ni una vez. Si existe viene en false, o no hay "
		+ "compras todavia o no hay nada que se repita lo suficiente, y paraSonia lo dice tal "
		+ "cual: son dos hechos distintos y ninguno es un error.")]
	public FirmaDeLaSemana Firma(
		[Description("Cuantas de sus ultimas compras mirar. Omitilo para usar el valor de la app (8).")]
		int cuantasCompras = ComprasAPI.ComprasQueSeMiranParaLaFirma)
	{
		if (!lector.HayJournal) return new FirmaDeLaSemana();

		return lector.Consultar(api => api.Firma(cuantasCompras));
	}

	[McpServerTool(Name = "que_cambio_en_mi_canasta", UseStructuredContent = true)]
	[Description("QUE SE VOLVIO HABITUAL Y DESDE CUANDO, y que dejo de estar, comparando lo de "
		+ "antes de una fecha con lo de despues. Es la tool que le pone FECHA a un cambio de "
		+ "habitos: usala para '¿que cambio en mis compras?', '¿desde cuando compro tanto helado?' "
		+ "o cuando la persona quiere relacionar sus compras con algo que le paso en un mes "
		+ "concreto. Cada producto que entro trae el mes en que se lo vio por primera vez, en "
		+ "formato aaaa-mm. Ojo: si hayConQueComparar viene en false NO hay cambio que reportar "
		+ "-falta un lado de la comparacion- y es distinto de hayCambio en false, que significa "
		+ "que la canasta viene igual. El campo corte dice POR QUE MES se partio la historia: si no "
		+ "pasaste fecha, es el que el dominio encontro, y conviene nombrarselo a la persona. Este modulo dice QUE cambio y no si esta bien: el helado "
		+ "que pasa de mensual a semanal es un dato, no una falta.")]
	public CambioDeLaCanasta Cambio(
		[Description("OPCIONAL, en formato aaaa-mm-dd. Omitilo y el dominio busca solo el mes del quiebre, que es lo normal; pasalo unicamente si la persona nombro una fecha suya.")]
		DateTime corte = default,
		[Description("Cuantas compras mirar de cada lado. Omitilo para usar el valor de la app (8).")]
		int cuantasCompras = ComprasAPI.ComprasQueSeMiranParaLaFirma)
	{
		if (!lector.HayJournal) return new CambioDeLaCanasta();

		// SIN FECHA, LA BUSCA EL DOMINIO, y el campo corte de la respuesta dice cual eligio.
		return lector.Consultar(api =>
			corte == default ? api.CambioDelQuiebre(cuantasCompras) : api.Cambio(corte, cuantasCompras));
	}

	[McpServerTool(Name = "cada_cuanto_compro", UseStructuredContent = true)]
	[Description("CADA CUANTO compra la persona un producto, ANTES y DESPUES de una fecha. Es la "
		+ "tool para '¿desde cuando compro tanto helado?', '¿cada cuanto compro leche?' o "
		+ "'¿cambio algo en mi rutina?'. La respuesta viene en el lenguaje en que la gente habla "
		+ "de sus habitos -'1 vez/mes', '1 vez/semana', 'de vez en cuando'- y no en dias, porque "
		+ "'cada 6.5 dias' no es como habla nadie. Necesita al menos TRES compras del producto de "
		+ "cada lado de la fecha: si hayConQueComparar viene en false, no hay ritmo que medir y NO "
		+ "es un error. Y ojo con cambio en false: significa que el ritmo viene igual, que tambien "
		+ "es una respuesta. Este modulo dice de que a que, nunca si esta mejor.")]
	public RitmoDeUnProducto Ritmo(
		[Description("El producto, como lo nombraria la persona: 'leche', 'helado'. Se busca por nombre o por parte del nombre; tambien acepta el codigo interno, pero no hace falta.")]
		string producto,
		[Description("La fecha de corte, en formato aaaa-mm-dd. Lo de antes se compara con lo de despues.")]
		DateTime corte)
	{
		if (!lector.HayJournal) return new RitmoDeUnProducto();

		return lector.Consultar(api => api.Ritmo(producto, corte));
	}

	[McpServerTool(Name = "que_cambio_en_mi_rutina", UseStructuredContent = true)]
	[Description("LOS PRODUCTOS A LOS QUE LES CAMBIO EL RITMO, el mas notorio primero: cada uno con "
		+ "cada cuanto se compraba antes y cada cuanto ahora. Usala para '¿que cambio en mi "
		+ "rutina?', '¿que estoy comprando mas seguido?' o como CONTEXTO cuando la persona "
		+ "relaciona sus compras con algo que le paso. Se ordena por cuanto salto la frecuencia y "
		+ "no por plata: lo que cambia una rutina es cada cuanto, no cuanto cuesta — un producto "
		+ "que paso de cada dos meses a todas las semanas dice mas que uno que subio de precio. "
		+ "esMasSeguido distingue lo que entro de lo que se fue apagando. Una lista vacia significa "
		+ "que ningun producto cambio de ritmo, que es una respuesta y no una falla.")]
	public IReadOnlyList<RitmoQueCambio> RitmosQueCambiaron(
		[Description("OPCIONAL, en formato aaaa-mm-dd. Omitilo y el dominio busca solo el mes del quiebre, que es lo normal; pasalo unicamente si la persona nombro una fecha suya.")]
		DateTime corte = default)
	{
		if (!lector.HayJournal) return new List<RitmoQueCambio>();

		// SIN FECHA, LA BUSCA EL DOMINIO. Es lo correcto por defecto: quien pregunta -sobre
		// todo una IA- no tiene forma de saber cuando cambio esta canasta, y una fecha
		// adivinada devuelve "no cambio nada", que se lee como un hecho y no como un fallo.
		return lector.Consultar(api =>
			corte == default ? api.RitmosDelQuiebre() : api.RitmosQueCambiaron(corte));
	}

	[McpServerTool(Name = "mi_ritmo_de_super", UseStructuredContent = true)]
	[Description("COMO VA AL SUPER esta persona: cada cuantos dias vuelve, que dia de la semana "
		+ "suele comprar y en que momento del dia. Usala para '¿cada cuanto voy al super?', "
		+ "'¿que dia compro?' o cuando la persona quiere entender su propia rutina — son cosas "
		+ "que probablemente no habia notado. OJO CON LA CONFUSION: esto es cada cuanto VA AL "
		+ "SUPER, no cada cuanto compra un producto (eso es cada_cuanto_compro) ni cada cuanto "
		+ "entra un departamento (eso lo mira que_dejaste_de_comprar). Una persona puede ir todas "
		+ "las semanas y comprar pollo cada tres. porMomento SIEMPRE trae los tres momentos del "
		+ "dia -manana, tarde, noche- aunque alguno venga en cero. Si esObservado viene en false "
		+ "no hay suficientes compras para medir un ritmo, y no es un error. Y esto no juzga: ir "
		+ "los viernes de noche no es mejor ni peor que ir los martes en la mañana.")]
	public RitmoDelSuperVista RitmoDelSuper()
	{
		if (!lector.HayJournal) return new RitmoDelSuperVista();

		return lector.Consultar(api => api.Ritmo());
	}

	[McpServerTool(Name = "canasta_del_mes", UseStructuredContent = true)]
	[Description("Como se compuso la canasta del supermercado de la persona en UN mes, DEPARTAMENTO "
		+ "POR DEPARTAMENTO: cuanto entro de cada pasillo (proteinas, frutas y verduras, lacteos, "
		+ "granos, aceites, dulces, snacks, limpieza, electrodomesticos), ordenados del que mas pesa "
		+ "al que menos, con la proporcionDelTotal de cada uno entre 0 y 1. Trae aparte el gasto "
		+ "comestible y el no comestible, para que un electrodomestico caro no se coma la lectura de "
		+ "la comida. Usa titulo y nunca categoria al hablarle a la persona: categoria es una llave "
		+ "tecnica. Son hechos de compra para razonar — cruzalos con otras fuentes si la persona lo "
		+ "pide — y no un juicio sobre su alimentacion: eso el supermercado no lo emite.")]
	public CanastaDelMesVista Canasta(
		[Description("El anio, por ejemplo 2026.")] int anio,
		[Description("El mes, de 1 a 12.")] int mes)
	{
		if (!lector.HayJournal) return new CanastaDelMesVista();

		return lector.Consultar(api => api.Canasta(anio, mes));
	}

	[McpServerTool(Name = "comparar_canastas", UseStructuredContent = true)]
	[Description("Como CAMBIO la canasta de la persona entre DOS MESES QUE ELLA NOMBRE. Si no "
		+ "nombro meses concretos, usa como_viene_mi_canasta en vez de esta: los meses validos "
		+ "salen del rango que devuelve total_de_compras, y pedir un mes fuera de ese rango "
		+ "devuelve hayComparacion en false aunque la persona si tenga datos. Devuelve los dos "
		+ "totales y, en diferencias, que pasillos explican el cambio (mira explicaElCambio, "
		+ "esNuevo y desaparecio). El cambio lleva la direccion en el SIGNO; no hay veredicto "
		+ "porque el supermercado no opina sobre la alimentacion de nadie.")]
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

	[McpServerTool(Name = "consumo_de", UseStructuredContent = true)]
	[Description("CUANTO Y CUANDO entro algo a la canasta de la persona, MES A MES: unidades, gasto "
		+ "y en cuantas compras, mas el mes mas alto y como cambio entre el primer mes y el ultimo. "
		+ "Acepta una categoria del catalogo (Azucarados, Proteinas, FrutasYVerduras, LacteosYHuevos, "
		+ "GranosYAbarrotes, AceitesYGrasas, Snacks, Limpieza, Electrohogar) o un texto de producto "
		+ "('helado', 'soda', 'olla'). Trae la serie DOS VECES: meses, mes a mes, y anios, la misma "
		+ "serie agrupada — usa anios cuando la pregunta abarca varios anios y meses cuando necesites "
		+ "una fecha. En un anio, mesesConCompras dice cuantos meses tuvieron alguna: un anio a medias "
		+ "no se compara con uno entero como si midieran lo mismo. Es la serie completa, no un total: es lo que permite decir "
		+ "DESDE QUE MES cambio un consumo, y ninguna pantalla lo muestra. Usala cuando la persona "
		+ "pregunte por un producto o categoria a lo largo del tiempo, o cuando quiera relacionar "
		+ "sus compras con algo mas que le este pasando -por ejemplo un resultado de laboratorio-: "
		+ "ahi lo que sirve es la fecha, no el total. Si sePuedeJuzgar viene en false, entrega los "
		+ "hechos y NO te apoyes en el veredicto: puede ser que haya un solo mes, o que la busqueda "
		+ "cruce categorias con perfiles distintos. IMPORTANTE: esto dice que compro y cuando, NO "
		+ "por que; la relacion con cualquier otra cosa la propone la persona o vos, con su permiso, "
		+ "y siempre como una posibilidad a conversar con su medico, no como una causa.")]
	public ConsumoEnElTiempo Consumo(
		[Description("Una categoria del catalogo o un texto de producto, por ejemplo 'Azucarados' o 'helado'.")] string termino)
	{
		if (!lector.HayJournal) return new ConsumoEnElTiempo();

		return lector.Consultar(api => api.Consumo(termino));
	}

	[McpServerTool(Name = "para_mi_proxima_compra", UseStructuredContent = true)]
	[Description("LA LISTA DE LO QUE LE VA A FALTAR a la persona, armada con el RITMO de cada "
		+ "producto: cada cuanto suele volver y cuanto hace que no entra. Del mas atrasado al que "
		+ "menos falta. Usala para '¿que tengo que comprar?', '¿que me esta faltando?' o cuando "
		+ "arme una lista antes de ir al super. Cada producto trae toca -si ya paso su tiempo-, "
		+ "diasTipicos, diasDesdeLaUltima y diasDeMas, que es negativo cuando todavia no toca: "
		+ "los que NO tocan tambien vienen, por si quiere adelantarse. NO ES que_dejaste_de_comprar: "
		+ "aquella mira lo que salio de la rutina, y esta EXCLUYE justamente eso — un producto que "
		+ "lleva mas de tres ciclos sin entrar ya no cuenta como falta, cuenta como abandonado. "
		+ "IMPORTANTE: es una lista de lo probable, no de lo necesario. La persona puede tener el "
		+ "producto en su casa y esta app no lo sabe, asi que presentala como una sugerencia para "
		+ "confirmar y nunca como una orden. Con cuantosTocan en 0 viene al dia, y eso es una "
		+ "respuesta.")]
	public ListaDeLaProximaCompra ParaMiProximaCompra()
	{
		if (!lector.HayJournal) return new ListaDeLaProximaCompra();

		return lector.Consultar(api => api.Lista(DateTime.Now));
	}

	[McpServerTool(Name = "en_cuales_compras", UseStructuredContent = true)]
	[Description("EN CUALES COMPRAS aparecio un producto o un pasillo entero, de la mas reciente a "
		+ "la mas vieja, con el NUMERO DE TICKET de cada una y cuanto de eso entro ahi. Acepta un "
		+ "texto de producto ('leche', 'helado', 'olla') o una categoria del catalogo (Azucarados, "
		+ "Proteinas, FrutasYVerduras, LacteosYHuevos, GranosYAbarrotes, AceitesYGrasas, Embutidos, "
		+ "ComidasPreparadas, Snacks, Limpieza, Mascotas, Electrohogar). NO ES consumo_de: aquella "
		+ "agrupa por mes y contesta 'cuanto y cuando'; esta devuelve los TICKETS, y su numero es lo "
		+ "que despues le podes pasar a detalle_de_compra para ver la compra entera. Usala cuando la "
		+ "persona pregunte '¿cuando compre esto?', '¿en que compra estaba?' o quiera llegar a un "
		+ "recibo puntual. El campo productos dice que descripciones coincidieron: si trae mas de "
		+ "una, el texto alcanzo a varios productos y conviene decirselo antes de dar los totales. "
		+ "Con cuantasCompras en 0 no hay ninguna, y eso es una respuesta valida, no un error.")]
	public ComprasHalladas EnCualesCompras(
		[Description("Un texto de producto o una categoria del catalogo, por ejemplo 'leche' o 'Limpieza'.")] string termino)
	{
		if (!lector.HayJournal) return new ComprasHalladas();

		return lector.Consultar(api => api.Halladas(termino));
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

	[JsonPropertyName("primerMes")]
	[Description("El mes mas viejo con compras, como aaaa-mm. Vacio si no hay ninguna.")]
	public string PrimerMes { get; init; } = "";

	[JsonPropertyName("ultimoMes")]
	[Description("El mes mas reciente con compras, como aaaa-mm. Vacio si no hay ninguna. Pedir "
		+ "meses fuera del rango primerMes..ultimoMes devuelve vacio, y eso no significa que la "
		+ "persona no tenga datos.")]
	public string UltimoMes { get; init; } = "";

	[JsonPropertyName("mesesConCompras")]
	[Description("Cuantos meses distintos tienen al menos una compra. Con menos de dos no hay dos "
		+ "meses que comparar.")]
	public int MesesConCompras { get; init; }

	[JsonPropertyName("carpeta")]
	[Description("La carpeta de la persona donde la app guarda su registro.")]
	public string Carpeta { get; init; } = "";

	[JsonPropertyName("detalle")]
	[Description("Vacio cuando todo esta bien; si no, que le conviene hacer a la persona.")]
	public string Detalle { get; init; } = "";
}
