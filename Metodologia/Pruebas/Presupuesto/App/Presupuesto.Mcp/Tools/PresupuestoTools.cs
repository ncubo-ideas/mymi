using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using ModelContextProtocol.Server;

namespace Presupuesto.Mcp.Tools;

// LA SUPERFICIE QUE VE LA IA.
//
// La prueba que estas tools tienen que pasar: si la IA puede contestar algo que YA ESTÁ EN
// UNA PANTALLA, no se probó nada. Un total, un saldo o el último movimiento no califican —
// el banco los muestra, y la app también los va a mostrar.
//
// SON INTENCIONALES, en el sentido fuerte: las escribe el autor de la app, igual que un
// [HttpGet], y la app decide qué expone. Deliberadamente NO existe una tool genérica
// Query(puppet, operacion, parametros): eso le daría a la IA una consola de DSL en vez de
// un dominio, tiraría a la basura el descubrimiento por schema, y expondría los puppets. La
// unidad descubrible es la APLICACIÓN.
//
// El [Description] NO es documentación: ES la semántica que lee la IA, lo único que tiene
// para decidir si esta tool responde la pregunta de la persona. Por eso está redactado como
// la pregunta que una persona se hace sobre su propia plata.
//
// TRES TOOLS, Y LAS DOS QUE NO ESTÁN. De las cinco preguntas de Preguntas.md, quedaron
// afuera dos a propósito:
//
//   · "¿se movió el total del mes?" — la app lo va a mostrar, y el banco ya lo muestra.
//   · "¿vale la pena el bien?" — es una pantalla con un número grande.
//
// Las tres que quedaron contestan cosas que una pantalla no puede o no le conviene: una
// AUSENCIA, una pregunta CONTRAFÁCTICA que depende de un dato que la persona trae en el
// momento, y una TASA sobre eventos agrupados a mano.
[McpServerToolType]
internal sealed class PresupuestoTools
{
	private readonly LectorDelJournal lector;

	public PresupuestoTools(LectorDelJournal lector)
	{
		ArgumentNullException.ThrowIfNull(lector);
		this.lector = lector;
	}

	// ---------------------------------------------------------------- la ausencia

	// LA TOOL QUE MIRA EL HUECO, no el dato. Es la única de las tres que una pantalla
	// literalmente NO PUEDE mostrar: una pantalla renderiza los movimientos que existen, y
	// esto es una conclusión sobre los movimientos MÁS el calendario MÁS la periodicidad
	// que cada gasto declara para sí mismo.
	//
	// No recibe argumentos. `hoy` lo pone ESTE proceso, no la IA y no el dominio: el modelo
	// no lee el reloj, porque si lo hiciera el mismo journal reproducido mañana daría otra
	// respuesta.
	[McpServerTool(Name = "que_no_me_cobraron", UseStructuredContent = true)]
	[Description("Qué pagos fijos no aparecieron cuando debían: cuánto llevan sin cobrarse y qué "
		+ "tan grave es cada uno. Usala cuando pregunte '¿me falta pagar algo?', '¿se me pasó "
		+ "alguna cuota?' o '¿está todo al día?'. Es lo que NINGUNA pantalla puede mostrar: una "
		+ "pantalla muestra los movimientos que hay, y esto dice lo que FALTA. La gravedad la "
		+ "decide la clase de compromiso y NO el monto: que no llegue el recibo de la luz es un "
		+ "aviso, que no salga una cuota de préstamo o el pago de la tarjeta es crítico, porque "
		+ "corre interés o mora. Un cargo ausente NO significa que haya algo mal con la plata: "
		+ "significa que hay algo que verificar — puede ser un débito que falló, un servicio "
		+ "cancelado sin avisar, o uno que se creía cancelado y sigue cobrando.")]
	public PagosQueNoLlegaron QueNoMeCobraron()
	{
		if (!lector.HayJournal) return new PagosQueNoLlegaron { HayDatos = false, Detalle = SinJournal };

		DateTime hoy = DateTime.Now;

		return lector.Consultar(api =>
		{
			var filas = Filas<PagoPendiente>(api.QueNoMeCobraron(hoy), "pendiente");
			int vencidos = filas.Count(f => f.EstaVencido);

			return new PagosQueNoLlegaron
			{
				HayDatos = true,
				Pagos = filas.OrderByDescending(f => f.OrdenDeLaSeveridad)
					.ThenByDescending(f => f.DiasDeAtraso).ToList(),
				CuantosVencidos = vencidos,
				EvaluadoAl = hoy.Date,
				Carpeta = lector.Carpeta,
			};
		});
	}

	// ---------------------------------------------------------------- la contrafáctica

	// LA ÚNICA PREGUNTA HACIA ADELANTE. No pregunta qué pasó: pregunta qué pasaría.
	//
	// Una pantalla podría tener un formulario para esto, pero el dato que hace falta —cuánto
	// va a costar la mensualidad nueva— la persona lo trae en el momento de la conversación,
	// no lo tiene guardado en ningún lado. Es la clase de pregunta que se hace hablando.
	[McpServerTool(Name = "que_puedo_reducir", UseStructuredContent = true)]
	[Description("Si la persona va a tomar un compromiso nuevo con una mensualidad fija —un "
		+ "préstamo, una cuota, una suscripción cara— dice cuánto hay realmente disponible para "
		+ "recortar y si alcanza. Usala cuando pregunte '¿me alcanza si tomo este préstamo?', "
		+ "'¿de dónde saco 50 mil al mes?' o '¿qué tendría que dejar de gastar?'. Devuelve TRES "
		+ "montos y los tres importan: lo DISCRECIONAL es lo que se sabe recortable; lo "
		+ "COMPROMETIDO no depende de la voluntad de este mes; y lo POR DEFINIR es plata que "
		+ "nadie clasificó todavía. Si respuestaCompleta viene en false, la respuesta se queda "
		+ "corta a propósito porque falta marcar gastos — decilo, y ofrecé ayudar a marcarlos, "
		+ "en vez de dar por cerrado que no alcanza.")]
	public MargenParaUnCompromiso QuePuedoReducir(
		[Description("Cuánto habría que pagar por mes por el compromiso nuevo.")] decimal mensualidadNueva)
	{
		if (mensualidadNueva <= 0)
			throw new ArgumentOutOfRangeException(nameof(mensualidadNueva),
				"Un compromiso nuevo cuesta algo. Cero no es una mensualidad.");

		if (!lector.HayJournal) return new MargenParaUnCompromiso { HayDatos = false, Detalle = SinJournal };

		// El mes de referencia es el ÚLTIMO COMPLETO, y lo decide esta superficie. Preguntar
		// por el mes en curso daría un total a medias y una respuesta optimista.
		DateTime mesDeReferencia = DateTime.Now.Date.AddDays(1 - DateTime.Now.Day).AddMonths(-1);

		return lector.Consultar(api =>
		{
			var m = Objeto<MargenParaUnCompromiso>(
				api.MargenPara(mensualidadNueva, mesDeReferencia.Year, mesDeReferencia.Month));

			m.HayDatos = true;
			m.MesDeReferencia = mesDeReferencia;
			m.Carpeta = lector.Carpeta;
			return m;
		});
	}

	// ---------------------------------------------------------------- la tasa

	// ANTICIPAR LO IMPREVISTO suena contradictorio y no lo es: no se puede predecir CUÁL,
	// pero sí CUÁNTO POR AÑO. Es razonamiento de seguro.
	//
	// LA TASA SE CUENTA SOBRE EVENTOS, NO SOBRE CARGOS, y es lo único que la hace útil. Una
	// hospitalización produce el hospital, la farmacia y el laboratorio: tres cargos, UN
	// imprevisto. "Tuve 47 cargos no presupuestados" no es información.
	[McpServerTool(Name = "cuanto_apartar_para_imprevistos", UseStructuredContent = true)]
	[Description("Cuánto conviene apartar por mes para gastos imprevistos, calculado sobre lo que "
		+ "de verdad le ha pasado a esta persona. Usala cuando pregunte '¿cuánto debería tener "
		+ "ahorrado?', '¿cuánto aparto por mes?' o '¿cada cuánto me pasa algo así?'. La cuenta se "
		+ "hace sobre EVENTOS y no sobre cargos: una hospitalización con tres facturas es UN "
		+ "imprevisto, no tres. Si hayTasa viene en false hay menos de dos eventos registrados, y "
		+ "entonces NO hay con qué estimar — decilo así, no inventes un promedio a partir de una "
		+ "anécdota.")]
	public TasaDeImprevistosParaLaIA CuantoApartar(
		[Description("Sobre qué tramo de la historia se calcula.")] Periodo periodo)
	{
		if (!lector.HayJournal)
			return new TasaDeImprevistosParaLaIA { HayDatos = false, Detalle = SinJournal };

		DateTime hasta = DateTime.Now.Date;
		DateTime desde = hasta.AddMonths(-periodo.EnMeses());

		return lector.Consultar(api =>
		{
			var t = Objeto<TasaDeImprevistosParaLaIA>(api.CuantoApartarPorMes(desde, hasta));
			t.HayDatos = true;
			t.Desde = desde;
			t.Hasta = hasta;
			t.Carpeta = lector.Carpeta;
			return t;
		});
	}

	// ---------------------------------------------------------------- del wire al tipo

	// El `print` del actor emite JSON, así que la proyección es una deserialización directa:
	// las etiquetas del print SON los nombres de las propiedades. Un objeto plano sale como
	// `{...}`; un `foreach` sale envuelto en una llave con el nombre de la variable del
	// bucle — de ahí que esas variables se llamen `pendiente` y no `p`.
	private static T Objeto<T>(string json) where T : new() =>
		string.IsNullOrWhiteSpace(json)
			? new T()
			: JsonSerializer.Deserialize<T>(json) ?? new T();

	private static List<T> Filas<T>(string json, string llave)
	{
		if (string.IsNullOrWhiteSpace(json)) return new List<T>();

		using var doc = JsonDocument.Parse(json);
		if (!doc.RootElement.TryGetProperty(llave, out var arreglo)) return new List<T>();

		return arreglo.ValueKind == JsonValueKind.Array
			? JsonSerializer.Deserialize<List<T>>(arreglo.GetRawText()) ?? new List<T>()
			: new List<T> { JsonSerializer.Deserialize<T>(arreglo.GetRawText()) };
	}

	private const string SinJournal =
		"La app de presupuesto todavia no registro nada en esta carpeta. Pedile a la persona que "
		+ "la abra y traiga sus movimientos del banco.";
}

// SOBRE QUÉ TRAMO. Conjunto CERRADO, y por eso es un enum y no un int de meses.
//
// Con un entero la IA puede pedir "17 meses", que no es una pregunta que alguien se haga, y
// la lista de valores razonables tendría que vivir en la prosa de la descripción. Con un
// enum vive en el inputSchema, que es un contrato: el cliente no puede mandar algo que no
// exista.
internal enum Periodo
{
	UltimoAno,
	UltimosDosAnos,
	TodaLaHistoria,
}

internal static class DelPeriodo
{
	internal static int EnMeses(this Periodo periodo) => periodo switch
	{
		Periodo.UltimoAno => 12,
		Periodo.UltimosDosAnos => 24,
		Periodo.TodaLaHistoria => 600,
		_ => throw new ArgumentOutOfRangeException(nameof(periodo), periodo,
			"Se agrego un periodo al enum y no se lo mapeo a meses."),
	};
}
