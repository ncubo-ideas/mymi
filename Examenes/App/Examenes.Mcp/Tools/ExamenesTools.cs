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
[McpServerToolType]
internal sealed class ExamenesTools
{
	private readonly LectorDelJournal lector;

	public ExamenesTools(LectorDelJournal lector)
	{
		ArgumentNullException.ThrowIfNull(lector);
		this.lector = lector;
	}

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
		+ "de medicion: hay que sugerir repetir el examen, no alarmar. Nombres de analito validos: "
		+ "'Antigeno prostatico', 'Colesterol total', 'Colesterol LDL', 'Hemoglobina'.")]
	public TendenciaDelAnalito Tendencia(
		[Description("El analito a evaluar, por ejemplo 'Antigeno prostatico' o 'Colesterol total'.")]
		string analito)
	{
		if (string.IsNullOrWhiteSpace(analito))
			return new TendenciaDelAnalito
			{
				HayDatos = false,
				Carpeta = lector.Carpeta,
				Detalle = "Hay que decir cual analito evaluar.",
			};

		if (!lector.HayJournal) return SinJournal();

		return lector.Consultar(api =>
		{
			TendenciaDeAnalito t = api.Tendencia(analito);

			// Sin mediciones no se inventa una tendencia: se dice que no hay datos de ese
			// analito, que es distinto de "esta estable".
			if (t.Mediciones == 0)
				return new TendenciaDelAnalito
				{
					HayDatos = true,
					Analito = analito,
					Tendencia = "sin mediciones",
					Carpeta = lector.Carpeta,
					UltimoEventoLeido = api.UltimoEventoProcesado(),
					Detalle = $"El expediente no tiene ninguna medicion de '{analito}'. "
						+ "Puede ser que el nombre no sea el correcto, o que la app todavia no lo trajo.",
				};

			return Armar(t, api.UltimoEventoProcesado(), api.EdadDelPaciente());
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
