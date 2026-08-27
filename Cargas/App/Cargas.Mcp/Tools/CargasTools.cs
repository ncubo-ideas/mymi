using System.ComponentModel;
using System.Text.Json.Serialization;
using Cargas.App;
using Cargas.App.Vistas;
using ModelContextProtocol.Server;

namespace Cargas.Mcp.Tools;

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
internal sealed class CargasTools
{
	private readonly LectorDelJournal lector;

	public CargasTools(LectorDelJournal lector)
	{
		ArgumentNullException.ThrowIfNull(lector);
		this.lector = lector;
	}

	// EL TOGGLE SE HACE CUMPLIR ACA, y por eso no es decoracion.
	//
	// La persona decide en su app; queda como un comando en SU journal; este proceso lo
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
					// un Actualizar.
					Detalle = $"El expediente todavia no tiene ninguna medicion de '{nombre}'. "
						+ "Pedile que abra su app y apriete Actualizar.",
				};

			return Armar(t, api.Interpretacion(nombre), api.UltimoEventoProcesado(), api.EdadDelPaciente());
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

	// LA MISMA PREGUNTA PARA LA OTRA FORMA, y va aparte porque no devuelve lo mismo.
	//
	// Un binario no tiene tendencia: no hay "mas positivo". Lo que su serie contesta es si
	// el hallazgo APARECIO, si PERSISTE -o sea, si ya se repitio y volvio a salir- o si
	// cedio. Y esa distincion cambia la conducta: una sangre oculta en heces positiva una
	// vez pide repetir el examen, dos veces seguidas ya no.
	//
	// LA QUE MAS APORTA ES LA INVERSA. Un positivo que en la prueba siguiente no vuelve a
	// aparecer NO quedo confirmado, y sin la serie eso no se puede decir: la hoja de
	// laboratorio de aquel dia sigue diciendo "positivo" para siempre.
	[McpServerTool(Name = "presencia_de", UseStructuredContent = true)]
	[Description("Contesta como viene un analito que se informa como PRESENTE o AUSENTE — sangre "
		+ "oculta, nitritos — mirando toda su historia y no la ultima prueba. Usala cuando "
		+ "pregunte '¿me sigue saliendo sangre?', '¿la infeccion cedio?' o '¿esto ya me habia "
		+ "pasado?'. Devuelve si aparecio, si PERSISTE (dos o mas pruebas positivas seguidas: ya "
		+ "se repitio y volvio a salir), si no se confirmo (aparecio una vez y la siguiente dio "
		+ "negativa) o si cedio. Es distinto de evaluar_tendencia, que solo sirve para analitos "
		+ "con numero: un nitrito no viene subiendo. Presta atencion a positivasSeguidas: un "
		+ "hallazgo confirmado y uno aislado NO piden lo mismo, y la accion correcta viene en "
		+ "el campo accion.")]
	public PresenciaDelAnalito Presencia(
		[Description("Cual analito binario evaluar.")] AnalitoBinario analito)
	{
		if (!lector.HayJournal)
			return new PresenciaDelAnalito
			{
				HayDatos = false, Carpeta = lector.Carpeta, Detalle = DetalleSinJournal,
			};

		string nombre = analito.Nombre();

		return lector.Consultar(api =>
		{
			PresenciaDeAnalito p = api.Presencia(nombre);

			// Sin pruebas no se inventa una lectura: no es lo mismo "nunca aparecio" que
			// "nunca te lo midieron", y confundirlos es como una app termina diciendo que
			// alguien esta bien cuando nadie lo midio.
			if (p.Mediciones == 0)
				return new PresenciaDelAnalito
				{
					HayDatos = true,
					Analito = nombre,
					Presencia = "sin pruebas",
					Carpeta = lector.Carpeta,
					UltimoEventoLeido = api.UltimoEventoProcesado(),
					Detalle = $"El expediente todavia no tiene ninguna prueba de '{nombre}'. "
						+ "Pedile que abra su app y apriete Actualizar.",
				};

			InterpretacionDeAnalito i = api.Interpretacion(nombre);

			return new PresenciaDelAnalito
			{
				HayDatos = true,
				Analito = p.ParaMostrar,
				Presencia = p.Presencia,
				Pruebas = p.Mediciones,
				Positivas = p.Positivas,
				PositivasSeguidas = p.PositivasSeguidas,
				DiasEntreLaPrimeraYLaUltima = p.Dias,
				UltimoResultado = p.UltimoResultado,
				EstaPresente = p.EstaPresente,
				HaySerie = p.HaySerie,
				Veredicto = i.Veredicto,
				Severidad = i.Severidad,
				Lectura = i.ParaSonia,
				Accion = i.Accion,
				PideAlgo = i.PideAlgo,
				UltimoEventoLeido = api.UltimoEventoProcesado(),
				Carpeta = lector.Carpeta,
				Detalle = "",
			};
		});
	}

	// LA TOOL QUE MIRA EL HUECO, no el dato.
	//
	// Reemplaza a `revisar_mis_analitos`, que recorria los analitos y devolvia la tendencia
	// de cada uno — o sea, exactamente lo que la app renderiza. Su propia descripcion tenia
	// que aclarar que "no es la lista que la app ya le muestra", y cuando una tool necesita
	// defenderse de eso, ya se contesto sola. Es el mismo reclamo que Alvaro le hizo al
	// `total` de Jonathan: si la IA contesta algo que ya esta en una pantalla, el demo no
	// probo nada.
	//
	// Esta contesta lo unico que una pantalla NO puede: una ausencia. Una pantalla solo
	// renderiza datos que tiene; "hace cuatro anos que no te medis el colesterol" no es un
	// dato del expediente, es una conclusion sobre el expediente MAS el calendario MAS la
	// periodicidad que cada analito define para si mismo.
	[McpServerTool(Name = "que_me_falta_medir", UseStructuredContent = true)]
	[Description("Que analitos estan vencidos: cuanto hace que no se miden y cada cuanto conviene "
		+ "repetir cada uno. Usala cuando pregunte '¿que cargas me toca hacerme?', '¿hace "
		+ "cuanto que no me chequeo el colesterol?' o '¿estoy al dia?'. Es lo que NINGUNA "
		+ "pantalla puede mostrar: la app muestra lo que hay, esto dice lo que FALTA. La "
		+ "periodicidad la decide cada analito y no es fija — un colesterol en rango se repite "
		+ "cada cinco anos y uno alto todos los anos. Solo aparecen analitos que ya se midieron "
		+ "alguna vez: de uno que nunca se hizo no hay nada que repetir.")]
	public ControlesEnEspera QueMeFaltaMedir()
	{
		if (!lector.HayJournal)
			return new ControlesEnEspera
			{
				HayDatos = false,
				Carpeta = lector.Carpeta,
				Detalle = DetalleSinJournal,
			};

		// HOY lo pone ESTE proceso, no el dominio. El modelo no lee el reloj: si lo
		// hiciera, el mismo journal reproducido manana daria otra respuesta. Saber que dia
		// es hoy es derecho de la superficie.
		DateTime hoy = DateTime.Now;

		return lector.Consultar(api =>
		{
			var controles = new List<ControlVencido>();
			int vencidos = 0;

			foreach (ControlEnEspera c in api.Controles(hoy))
			{
				if (c.EstaVencido) vencidos++;
				controles.Add(new ControlVencido
				{
					Analito = c.ParaMostrar,
					UltimaMedicion = c.UltimaMedicion,
					DiasDesdeLaUltima = c.DiasDesdeLaUltima,
					CadaCuantosMeses = c.MesesRecomendados,
					EstaVencido = c.EstaVencido,
					Severidad = c.Severidad,
					Accion = c.Accion,
					Lectura = c.ParaSonia,
				});
			}

			return new ControlesEnEspera
			{
				HayDatos = true,
				Controles = controles,
				CuantosVencidos = vencidos,
				EvaluadoAl = hoy.Date,
				UltimoEventoLeido = api.UltimoEventoProcesado(),
				Carpeta = lector.Carpeta,
				Detalle = "",
			};
		});
	}

	private TendenciaDelAnalito Armar(TendenciaDeAnalito t, InterpretacionDeAnalito i,
		long ultimoEvento, int edad) =>
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
			Veredicto = i.Veredicto,
			Severidad = i.Severidad,
			Lectura = i.ParaSonia,
			Accion = i.Accion,
			PideAlgo = i.PideAlgo,
			SugiereRepetirElExamen = t.SugiereRepetirElExamen,
			HaySerie = t.HaySerie,
			EdadDelPaciente = edad,
			UltimoEventoLeido = ultimoEvento,
			Carpeta = lector.Carpeta,
			Detalle = "",
		};

	private const string DetalleSinJournal =
		"La app todavia no registro nada en esta carpeta. Pedile a la persona que abra su app de "
		+ "cargas y apriete Actualizar.";

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

	[JsonPropertyName("veredicto")]
	[Description("Lo que ESTE analito concluye de su serie: estable, mejorando, empeorando, "
		+ "indeterminado o probable error de medicion. Ojo: 'mejorando' puede venir con un valor "
		+ "todavia fuera de rango, y eso es buena noticia, no una alarma.")]
	public string Veredicto { get; init; } = "";

	[JsonPropertyName("severidad")]
	[Description("Que tan urgente es: informativo, vigilar, consulte pronto o urgente. Lo "
		+ "decide el analito, no el tamano del numero: dos hallazgos identicos en forma "
		+ "pueden tener severidades distintas.")]
	public string Severidad { get; init; } = "";

	[JsonPropertyName("lectura")]
	[Description("La lectura de la serie en una linea, escrita por el propio analito. Deci esto, "
		+ "no lo reformules en un consejo generico.")]
	public string Lectura { get; init; } = "";

	[JsonPropertyName("accion")]
	[Description("Que corresponde hacer, si algo. Vacio significa que NO hay nada que hacer: no "
		+ "inventes una recomendacion para llenar el hueco.")]
	public string Accion { get; init; } = "";

	[JsonPropertyName("pideAlgo")]
	[Description("true si este analito pide alguna accion de la persona.")]
	public bool PideAlgo { get; init; }

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
	[Description("Hasta que evento del laboratorio leyo la app. Si la persona apreta Actualizar y "
		+ "entraron cargas nuevos, este numero sube.")]
	public long UltimoEventoLeido { get; init; }

	[JsonPropertyName("carpeta")]
	[Description("La carpeta de la persona donde la app guarda su expediente.")]
	public string Carpeta { get; init; } = "";

	[JsonPropertyName("detalle")]
	[Description("Vacio cuando todo esta bien; si no, que le conviene hacer a la persona.")]
	public string Detalle { get; init; } = "";
}

// LO QUE CONCLUYE LA SERIE DE UN BINARIO. Tipo aparte de TendenciaDelAnalito porque no
// trae lo mismo: no hay valor inicial ni final ni unidad. Hay cuantas veces se busco,
// cuantas aparecio y cuantas SEGUIDAS, que es la unica que cambia la conducta.
internal sealed class PresenciaDelAnalito
{
	[JsonPropertyName("hayDatos")]
	[Description("false cuando la app todavia no registro nada; ahi el resto viene en cero.")]
	public bool HayDatos { get; init; }

	[JsonPropertyName("analito")]
	[Description("El analito evaluado.")]
	public string Analito { get; init; } = "";

	[JsonPropertyName("presencia")]
	[Description("La lectura de la serie: 'nunca apareció', 'apareció' (positiva la ultima y "
		+ "negativa la anterior: todavia nadie lo confirmo), 'persiste' (dos o mas positivas "
		+ "seguidas: ya se repitio y volvio a salir), 'no confirmado' (aparecio una vez y la "
		+ "prueba siguiente dio negativa), 'cedió' (estuvo varias veces y ya no esta), "
		+ "'indeterminada' (una sola prueba) o 'sin pruebas'.")]
	public string Presencia { get; init; } = "";

	[JsonPropertyName("pruebas")]
	[Description("Cuantas veces se corrio la prueba. Las no realizadas no cuentan.")]
	public int Pruebas { get; init; }

	[JsonPropertyName("positivas")]
	[Description("De esas, cuantas dieron positivas en total.")]
	public int Positivas { get; init; }

	[JsonPropertyName("positivasSeguidas")]
	[Description("Cuantas positivas hay AL FINAL, una detras de otra. Es el numero que "
		+ "distingue un hallazgo aislado de uno confirmado, y por lo tanto el que cambia que "
		+ "hacer. Dos o mas significa que se repitio la prueba y volvio a salir.")]
	public int PositivasSeguidas { get; init; }

	[JsonPropertyName("diasEntreLaPrimeraYLaUltima")]
	public int DiasEntreLaPrimeraYLaUltima { get; init; }

	[JsonPropertyName("ultimoResultado")]
	[Description("El resultado de la ultima prueba, con el vocabulario de ese analito: "
		+ "'positivo', 'negativo'. En una serologia seria 'reactivo'.")]
	public string UltimoResultado { get; init; } = "";

	[JsonPropertyName("estaPresente")]
	[Description("Si hoy el hallazgo esta. Es lo unico que se puede saber sin la historia.")]
	public bool EstaPresente { get; init; }

	[JsonPropertyName("haySerie")]
	[Description("false con una sola prueba: ahi no se puede decir si esto se repite.")]
	public bool HaySerie { get; init; }

	[JsonPropertyName("veredicto")]
	[Description("Que significa: 'empeorando', 'mejorando', 'estable', 'hallazgo no confirmado' "
		+ "(aparecio y en la prueba siguiente ya no estaba) o 'indeterminado'.")]
	public string Veredicto { get; init; } = "";

	[JsonPropertyName("severidad")]
	[Description("'informativo', 'vigilar', 'consulte pronto' o 'urgente'.")]
	public string Severidad { get; init; } = "";

	[JsonPropertyName("lectura")]
	[Description("La lectura en una linea, escrita por el propio analito. Decila tal cual: no "
		+ "la reemplaces por una frase generica.")]
	public string Lectura { get; init; } = "";

	[JsonPropertyName("accion")]
	[Description("Que corresponde hacer Y CUANDO, ya dicho entero: 'mostrále esto a tu médico "
		+ "este mes'. Sale de un conjunto cerrado del dominio, siempre con plazo; vacio cuando "
		+ "no hay nada que hacer, que es la mayoria de las veces.")]
	public string Accion { get; init; } = "";

	[JsonPropertyName("pideAlgo")]
	public bool PideAlgo { get; init; }

	[JsonPropertyName("ultimoEventoLeido")]
	public long UltimoEventoLeido { get; init; }

	[JsonPropertyName("carpeta")]
	public string Carpeta { get; init; } = "";

	[JsonPropertyName("detalle")]
	public string Detalle { get; init; } = "";
}

internal sealed class ControlesEnEspera
{
	[JsonPropertyName("hayDatos")]
	[Description("false cuando la app todavia no registro nada.")]
	public bool HayDatos { get; init; }

	[JsonPropertyName("controles")]
	[Description("Un renglon por analito con historia, LO MAS VENCIDO PRIMERO. El que esta al "
		+ "dia igual aparece, para poder decirlo.")]
	public IReadOnlyList<ControlVencido> Controles { get; init; } = new List<ControlVencido>();

	[JsonPropertyName("cuantosVencidos")]
	[Description("Cuantos pasaron su periodicidad. Cero significa al dia con todo lo que alguna "
		+ "vez se midio.")]
	public int CuantosVencidos { get; init; }

	[JsonPropertyName("evaluadoAl")]
	[Description("Contra que fecha se calculo. El dominio no lee el reloj: esta fecha la puso "
		+ "este servidor, asi que la respuesta vale para ese dia.")]
	public DateTime EvaluadoAl { get; init; }

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

internal sealed class ControlVencido
{
	[JsonPropertyName("analito")]
	[Description("Como se llama para la persona, con acentos.")]
	public string Analito { get; init; } = "";

	[JsonPropertyName("ultimaMedicion")]
	[Description("La fecha de la ultima vez que se midio.")]
	public DateTime UltimaMedicion { get; init; }

	[JsonPropertyName("diasDesdeLaUltima")]
	[Description("Cuantos dias pasaron desde esa medicion.")]
	public int DiasDesdeLaUltima { get; init; }

	[JsonPropertyName("cadaCuantosMeses")]
	[Description("Cada cuanto conviene repetir ESTE analito. NO es fijo: varios lo resuelven "
		+ "contra su ultimo valor -un colesterol en rango cada 5 anos, uno alto cada ano-.")]
	public int CadaCuantosMeses { get; init; }

	[JsonPropertyName("estaVencido")]
	[Description("true si ya paso su periodicidad.")]
	public bool EstaVencido { get; init; }

	[JsonPropertyName("severidad")]
	[Description("informativo, vigilar o consulte pronto. Un control vencido NO es un "
		+ "hallazgo clinico: no dice que haya nada mal, dice que hay algo que hacer.")]
	public string Severidad { get; init; } = "";

	[JsonPropertyName("accion")]
	[Description("Que corresponde hacer, si algo. Vacio significa que esta al dia.")]
	public string Accion { get; init; } = "";

	[JsonPropertyName("lectura")]
	[Description("La frase que arma el dominio. Deci esto, no lo reformules en un consejo "
		+ "generico.")]
	public string Lectura { get; init; } = "";
}
