using System.ComponentModel;
using System.Text.Json.Serialization;
using Examenes.App;
using Examenes.App.Vistas;
using ModelContextProtocol.Server;

namespace Examenes.Mcp.Tools;

// LA SUPERFICIE QUE VE LA IA.
//
// La prueba que estas tools tienen que pasar: si la IA puede contestar algo que YA ESTA
// EN UNA PANTALLA, el demo no probo nada. Un total, un conteo o el ultimo valor de un
// analito no califican - la app los muestra.
//
// Las dos tools de aca contestan lo que NINGUNA pantalla puede: la lectura de la SERIE.
// Una hoja de resultados de laboratorio trae un valor y una fecha. "Tu antigeno viene
// subiendo desde hace tres anos" no es un dato que se pueda imprimir en una hoja: es una
// conclusion sobre la historia, y necesita las reglas de interpretacion que viven en el
// dominio.
//
// Son intencionales en el sentido fuerte: las escribe el autor de la app, igual que un
// [HttpGet], y la app decide que expone. Deliberadamente NO existe una tool generica
// Query(puppet, operation, parameters): eso le daria a la IA una consola de DSL en vez de
// un dominio, tiraria a la basura el descubrimiento por schema, y expondria los puppets.
// La unidad descubrible es la aplicacion.
//
// El [Description] NO es documentacion: ES la semantica que lee la IA, lo unico que tiene
// para decidir si esta tool responde la pregunta de la persona. Por eso esta redactado
// como la pregunta que una persona se hace sobre sus propios datos.
/// <summary>A que ritmo viene cambiando un analito. Lo que ninguna pantalla muestra.</summary>
internal sealed class VelocidadDelAnalito
{
	[Description("false cuando la app todavia no registro nada; ahi el resto viene en cero.")]
	[JsonPropertyName("hayDatos")] public bool HayDatos { get; init; }

	[Description("El analito medido.")]
	[JsonPropertyName("analito")] public string Analito { get; init; } = "";

	[Description("Cambio por ano, con signo. Positivo si viene subiendo.")]
	[JsonPropertyName("porAno")] public decimal PorAno { get; init; }

	[Description("La unidad del analito, para poder decir '2,20 ng/mL por ano' y no '2,20'.")]
	[JsonPropertyName("unidad")] public string Unidad { get; init; } = "";

	[Description("Dias del tramo sobre el que se midio. Es el MAS RECIENTE, no la serie entera.")]
	[JsonPropertyName("diasDelTramo")] public int DiasDelTramo { get; init; }

	[Description("El umbral clinico de ESTE analito, para poder decir contra que se comparo.")]
	[JsonPropertyName("umbralPorAno")] public decimal UmbralPorAno { get; init; }

	[Description("false si este analito no se interpreta por velocidad, o si no hay dos "
		+ "mediciones. En ese caso NO hay que inventar una lectura del ritmo.")]
	[JsonPropertyName("esAplicable")] public bool EsAplicable { get; init; }

	[Description("true si el ritmo por si solo ya exige atencion, aunque el valor absoluto "
		+ "este en rango. Ahi corresponde recomendar que lo vea su medico, no diagnosticar.")]
	[JsonPropertyName("exigeAtencion")] public bool ExigeAtencion { get; init; }

	[Description("El ritmo ya dicho, por ejemplo '2.20 ng/mL por ano'.")]
	[JsonPropertyName("expresada")] public string Expresada { get; init; } = "";

	[JsonPropertyName("carpeta")] public string Carpeta { get; init; } = "";

	[Description("Hasta que evento leyo la app. Si no sube, no entro nada nuevo.")]
	[JsonPropertyName("ultimoEventoLeido")] public long UltimoEventoLeido { get; init; }
}

[McpServerToolType]
internal sealed class ExamenesTools
{
	private readonly LectorDelJournal lector;

	public ExamenesTools(LectorDelJournal lector)
	{
		ArgumentNullException.ThrowIfNull(lector);
		this.lector = lector;
	}

	// EL TOGGLE SE HACE CUMPLIR ACA, y por eso no es decoracion.
	//
	// La persona decide en su app; queda como un comando en SU journal; este proceso lo
	// lee al rehidratar y se niega ANTES de mirar ningun dato. No hay canal entre el
	// browser y este servidor: el journal ES el protocolo, y esto es lo que lo demuestra.
	//
	// El mensaje dice que fue una decision de la persona y no una falla, para que la IA
	// no insista ni lo reporte como error del sistema.
	private static string NegadaPorLaPersona(string queEs) =>
		$"La persona deshabilito esta consulta ({queEs}) desde los ajustes de su app. "
		+ "No es un error: es una decision suya. Si quiere habilitarla, puede hacerlo ahi.";

	// UseStructuredContent es el ultimo tramo de "sin strings en ninguna frontera
	// semantica". Sin el, el SDK serializa el record a JSON y lo manda como un bloque de
	// TEXTO: la IA recibe un string que tiene que volver a parsear. Con el, el resultado
	// viaja en structuredContent y la tool publica un outputSchema, asi que los decimales
	// llegan como decimales.
	[McpServerTool(Name = "evaluar_tendencia", UseStructuredContent = true)]
	[Description("Contesta si un analito de la persona VIENE SUBIENDO, viene bajando o esta "
		+ "estable, mirando toda su historia y no el ultimo resultado. Es la pregunta que su hoja "
		+ "de laboratorio no puede contestar, porque una hoja trae un valor y una fecha. Usala "
		+ "cuando pregunte cosas como '¿mi antigeno prostatico viene subiendo o esta estable?' o "
		+ "'¿como viene mi colesterol?'. Devuelve la tendencia, los valores inicial y final con su "
		+ "unidad, y cuantos dias pasaron entre los dos. Si sugiereRepetirElExamen viene en true, "
		+ "el cambio es demasiado grande para el tiempo transcurrido y lo mas probable es un error "
		+ "de medicion: hay que sugerir repetir el examen, no alarmar.")]
	public TendenciaDelAnalito Tendencia(
		[Description("Cual analito evaluar.")] Analito analito)
	{
		// La guarda de "vino vacio" ya no existe, y no es un descuido: con un enum el
		// cliente MCP no PUEDE mandar algo fuera del conjunto. Esa rama se borro porque
		// se volvio inalcanzable, que es justamente lo que se compra al tipar.
		if (!lector.HayJournal) return SinJournal();

		if (!lector.Consultar(api => api.ConsultaDeLaIAPermitida("tendencia")))
			return new TendenciaDelAnalito
			{
				HayDatos = false,
				Carpeta = lector.Carpeta,
				Detalle = NegadaPorLaPersona("la tendencia de un analito"),
			};

		string nombre = analito.Nombre();

		return lector.Consultar(api =>
		{
			TendenciaDeAnalito t = api.Tendencia(nombre);

			// Sin mediciones no se inventa una tendencia: se dice que no hay datos de ese
			// analito, que es distinto de "esta estable".
			if (t.Mediciones == 0)
				return new TendenciaDelAnalito
				{
					HayDatos = true,
					Analito = nombre,
					Tendencia = "sin mediciones",
					Carpeta = lector.Carpeta,
					UltimoEventoLeido = api.UltimoEventoProcesado(),
					// Ya no puede ser "el nombre esta mal": el enum lo impide. Si no hay
					// mediciones es que la app todavia no las trajo, y eso se arregla con
					// un Refresh.
					Detalle = $"El expediente todavia no tiene ninguna medicion de '{nombre}'. "
						+ "Pedile que abra su app y apriete Refresh.",
				};

			return Armar(t, api.UltimoEventoProcesado(), api.EdadDelPaciente());
		});
	}

	// LA TOOL QUE NINGUNA PANTALLA DUPLICA, y ese era el criterio de Alvaro: si la IA
	// puede contestar algo que ya esta a la vista, el demo no probo nada.
	//
	// La app muestra la tendencia -de cuanto a cuanto, en cuantos dias-. El RITMO es otra
	// cosa y se mide sobre otro tramo: el mas reciente, no la serie entera. Con los datos
	// del guion la diferencia decide el caso -la serie completa da 0,73 por ano y no
	// alarmaria; el ultimo tramo da 2,20 contra un umbral de 0,75- y es exactamente el
	// criterio con el que se lee un antigeno prostatico en la practica.
	[McpServerTool(Name = "velocidad_de", UseStructuredContent = true)]
	[Description("Contesta A QUE RITMO viene cambiando un analito, en unidades por ano, y si ese "
		+ "ritmo por si solo ya exige atencion aunque el valor absoluto todavia este en rango. Es "
		+ "distinto de evaluar_tendencia: la tendencia mira la serie entera y esto mira el tramo "
		+ "MAS RECIENTE, que es como se lee un antigeno prostatico en la practica clinica. Usala "
		+ "cuando pregunte '¿que tan rapido esta subiendo?', '¿deberia preocuparme por la "
		+ "velocidad?' o cuando evaluar_tendencia haya dado 'en ascenso' y haga falta el ritmo. Si "
		+ "esAplicable viene en false, ese analito no se interpreta por velocidad -el colesterol se "
		+ "lee por nivel, no por pendiente- y no hay que inventar una lectura.")]
	public VelocidadDelAnalito Velocidad(
		[Description("Cual analito medir.")] Analito analito)
	{
		if (!lector.HayJournal)
			return new VelocidadDelAnalito { HayDatos = false, Carpeta = lector.Carpeta };

		if (!lector.Consultar(api => api.ConsultaDeLaIAPermitida("velocidad")))
			return new VelocidadDelAnalito
			{
				HayDatos = false,
				Carpeta = lector.Carpeta,
				Expresada = NegadaPorLaPersona("la velocidad de un analito"),
			};

		string nombre = analito.Nombre();

		return lector.Consultar(api =>
		{
			VelocidadDeAnalito v = api.Velocidad(nombre);

			return new VelocidadDelAnalito
			{
				HayDatos = true,
				Analito = nombre,
				PorAno = v.PorAno,
				Unidad = v.Unidad,
				DiasDelTramo = v.Dias,
				UmbralPorAno = v.Umbral,
				EsAplicable = v.EsAplicable,
				ExigeAtencion = v.ExigeAtencion,
				Expresada = v.Expresada,
				Carpeta = lector.Carpeta,
				UltimoEventoLeido = api.UltimoEventoProcesado(),
			};
		});
	}

	[McpServerTool(Name = "revisar_mis_analitos", UseStructuredContent = true)]
	[Description("Revisa TODOS los analitos de los que la persona tiene historia y devuelve la "
		+ "tendencia de cada uno, marcando cuales exigen atencion. Usala cuando pregunte algo "
		+ "general como '¿hay algo en mis examenes que venga empeorando?', '¿como vengo?' o "
		+ "'¿tengo que preocuparme por algo?'. No es la lista de examenes que la app ya le "
		+ "muestra: es la LECTURA de cada serie a lo largo del tiempo, que ninguna pantalla trae. "
		+ "Los que tengan exigeAtencion en true son los que conviene que vea su medico.")]
	public RevisionDeAnalitos Revision()
	{
		if (!lector.HayJournal)
			return new RevisionDeAnalitos
			{
				HayDatos = false,
				Carpeta = lector.Carpeta,
				Detalle = DetalleSinJournal,
			};

		if (!lector.Consultar(api => api.ConsultaDeLaIAPermitida("revision")))
			return new RevisionDeAnalitos
			{
				HayDatos = false,
				Carpeta = lector.Carpeta,
				Detalle = NegadaPorLaPersona("el repaso de todos los analitos"),
			};

		// UNA sola rehidratacion para todas las preguntas. Separarlas releeria el journal
		// varias veces y, peor, podria mezclar dos estados si la app escribe en el medio.
		return lector.Consultar(api =>
		{
			var tendencias = new List<TendenciaDelAnalito>();
			int exigenAtencion = 0;
			long ultimoEvento = api.UltimoEventoProcesado();
			int edad = api.EdadDelPaciente();

			foreach (string analito in api.Analitos())
			{
				TendenciaDelAnalito t = Armar(api.Tendencia(analito), ultimoEvento, edad);
				if (t.ExigeAtencion) exigenAtencion++;
				tendencias.Add(t);
			}

			return new RevisionDeAnalitos
			{
				HayDatos = true,
				Analitos = tendencias,
				CuantosExigenAtencion = exigenAtencion,
				EdadDelPaciente = edad,
				CantidadDeExamenes = api.CantidadDeExamenes(),
				UltimoEventoLeido = ultimoEvento,
				Carpeta = lector.Carpeta,
				Detalle = "",
			};
		});
	}

	private TendenciaDelAnalito Armar(TendenciaDeAnalito t, long ultimoEvento, int edad) =>
		new TendenciaDelAnalito
		{
			HayDatos = true,
			Analito = t.Analito,
			Tendencia = t.Tendencia,
			Mediciones = t.Mediciones,
			ValorInicial = t.ValorInicial,
			ValorFinal = t.ValorFinal,
			Unidad = t.Unidad,
			DiasEntreLaPrimeraYLaUltima = t.Dias,
			Cambio = t.Cambio,
			ExigeAtencion = t.ExigeAtencion,
			SugiereRepetirElExamen = t.SugiereRepetirElExamen,
			HaySerie = t.HaySerie,
			EdadDelPaciente = edad,
			UltimoEventoLeido = ultimoEvento,
			Carpeta = lector.Carpeta,
			Detalle = "",
		};

	private const string DetalleSinJournal =
		"La app todavia no registro nada en esta carpeta. Pedile a la persona que abra su app de "
		+ "examenes y apriete Refresh.";

	private TendenciaDelAnalito SinJournal() =>
		new TendenciaDelAnalito { HayDatos = false, Carpeta = lector.Carpeta, Detalle = DetalleSinJournal };
}

// Tipos de retorno CON FORMA, no un string: el SDK genera el schema de salida desde esto
// y la IA recibe structuredContent con decimales de verdad. Es el ultimo tramo de "sin
// strings en ninguna frontera semantica".
internal sealed class TendenciaDelAnalito
{
	[JsonPropertyName("hayDatos")]
	[Description("false cuando la app todavia no registro nada; ahi el resto viene en cero.")]
	public bool HayDatos { get; init; }

	[JsonPropertyName("analito")]
	[Description("El analito evaluado.")]
	public string Analito { get; init; } = "";

	[JsonPropertyName("tendencia")]
	[Description("La lectura de la serie completa: 'estable', 'en ascenso', 'en descenso', "
		+ "'salto inverosimil' (el cambio no da para el tiempo transcurrido: probable error de "
		+ "medicion), 'indeterminada' (una sola medicion, todavia no hay serie) o 'sin mediciones'.")]
	public string Tendencia { get; init; } = "";

	[JsonPropertyName("mediciones")]
	[Description("Cuantas mediciones de este analito tiene el expediente. Con menos de dos no hay "
		+ "serie que evaluar.")]
	public int Mediciones { get; init; }

	[JsonPropertyName("valorInicial")]
	[Description("La primera medicion de la serie. Cuando la tendencia es 'salto inverosimil' este "
		+ "y el final son los dos extremos DEL SALTO sospechoso, no de toda la serie: es el hecho "
		+ "que importa para decidir repetir el examen.")]
	public decimal ValorInicial { get; init; }

	[JsonPropertyName("valorFinal")]
	[Description("La ultima medicion de la serie.")]
	public decimal ValorFinal { get; init; }

	[JsonPropertyName("unidad")]
	[Description("La unidad en que se mide este analito, por ejemplo mg/dL o ng/mL. Siempre hay que "
		+ "decir el valor CON su unidad.")]
	public string Unidad { get; init; } = "";

	[JsonPropertyName("diasEntreLaPrimeraYLaUltima")]
	[Description("Cuantos dias pasaron entre las dos mediciones reportadas. Es la mitad de la "
		+ "historia: el mismo cambio en un dia y en un ano significan cosas distintas.")]
	public int DiasEntreLaPrimeraYLaUltima { get; init; }

	[JsonPropertyName("cambio")]
	[Description("Cuanto cambio, con signo: positivo si subio.")]
	public decimal Cambio { get; init; }

	[JsonPropertyName("exigeAtencion")]
	[Description("true si el cambio va en la direccion que preocupa para ESTE analito y alcanzo el "
		+ "umbral clinico. Conviene que lo vea su medico. No diagnostiques.")]
	public bool ExigeAtencion { get; init; }

	[JsonPropertyName("sugiereRepetirElExamen")]
	[Description("true cuando el cambio es demasiado grande para el tiempo transcurrido. Lo mas "
		+ "probable es un error de medicion: hay que sugerir repetir el examen, NO alarmar.")]
	public bool SugiereRepetirElExamen { get; init; }

	[JsonPropertyName("haySerie")]
	[Description("true si hay al menos dos mediciones, que es lo minimo para hablar de tendencia.")]
	public bool HaySerie { get; init; }

	[JsonPropertyName("edadDelPaciente")]
	[Description("La edad con la que el dominio elige los rangos de referencia: lo normal a los 60 "
		+ "no es lo normal a los 30.")]
	public int EdadDelPaciente { get; init; }

	[JsonPropertyName("ultimoEventoLeido")]
	[Description("Hasta que evento del laboratorio leyo la app. Si la persona apreta Refresh y "
		+ "entraron examenes nuevos, este numero sube.")]
	public long UltimoEventoLeido { get; init; }

	[JsonPropertyName("carpeta")]
	[Description("La carpeta de la persona donde la app guarda su expediente.")]
	public string Carpeta { get; init; } = "";

	[JsonPropertyName("detalle")]
	[Description("Vacio cuando todo esta bien; si no, que le conviene hacer a la persona.")]
	public string Detalle { get; init; } = "";
}

internal sealed class RevisionDeAnalitos
{
	[JsonPropertyName("hayDatos")]
	[Description("false cuando la app todavia no registro nada.")]
	public bool HayDatos { get; init; }

	[JsonPropertyName("analitos")]
	[Description("La tendencia de cada analito del que hay historia.")]
	public IReadOnlyList<TendenciaDelAnalito> Analitos { get; init; } = new List<TendenciaDelAnalito>();

	[JsonPropertyName("cuantosExigenAtencion")]
	[Description("Cuantos analitos vienen cambiando en la direccion que preocupa. Si es cero, no "
		+ "hay nada que la serie este marcando.")]
	public int CuantosExigenAtencion { get; init; }

	[JsonPropertyName("edadDelPaciente")]
	[Description("La edad con la que el dominio elige los rangos de referencia.")]
	public int EdadDelPaciente { get; init; }

	[JsonPropertyName("cantidadDeExamenes")]
	[Description("Cuantos examenes tiene registrados el expediente en total.")]
	public int CantidadDeExamenes { get; init; }

	[JsonPropertyName("ultimoEventoLeido")]
	[Description("Hasta que evento del laboratorio leyo la app.")]
	public long UltimoEventoLeido { get; init; }

	[JsonPropertyName("carpeta")]
	[Description("La carpeta de la persona donde la app guarda su expediente.")]
	public string Carpeta { get; init; } = "";

	[JsonPropertyName("detalle")]
	[Description("Vacio cuando todo esta bien; si no, que le conviene hacer a la persona.")]
	public string Detalle { get; init; } = "";
}
