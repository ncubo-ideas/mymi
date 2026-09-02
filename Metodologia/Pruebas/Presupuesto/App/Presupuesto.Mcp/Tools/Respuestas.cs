using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Presupuesto.Mcp.Tools;

// LOS TIPOS DE RETORNO, con forma y no un string.
//
// El SDK genera el outputSchema desde esto y la IA recibe structuredContent con decimales
// de verdad. Sin `UseStructuredContent`, el resultado viaja como un bloque de TEXTO que la
// IA tiene que volver a parsear — y ahí se pierden los tipos y se gana ambigüedad.
//
// Cada [Description] es semántica que la IA lee, no documentación para un humano. Los que
// dicen "no inventes" están ahí porque son las tres formas conocidas de que una respuesta
// correcta se convierta en un consejo equivocado.

internal sealed class PagosQueNoLlegaron
{
	[Description("false cuando la app todavia no registro nada; ahi el resto viene vacio.")]
	[JsonPropertyName("hayDatos")] public bool HayDatos { get; set; }

	[Description("Un renglon por gasto recurrente, LO MAS GRAVE PRIMERO. El que esta al dia "
		+ "igual aparece, para poder decirlo.")]
	[JsonPropertyName("pagos")] public List<PagoPendiente> Pagos { get; set; } = new();

	[Description("Cuantos pasaron su fecha esperada. Cero significa al dia con todo.")]
	[JsonPropertyName("cuantosVencidos")] public int CuantosVencidos { get; set; }

	[Description("Contra que dia se evaluo. El dominio no lee el reloj: esta fecha la puso el "
		+ "servidor, asi que la respuesta vale para ese dia.")]
	[JsonPropertyName("evaluadoAl")] public DateTime EvaluadoAl { get; set; }

	[JsonPropertyName("carpeta")] public string Carpeta { get; set; } = "";

	[Description("Vacio cuando todo esta bien; si no, que le conviene hacer a la persona.")]
	[JsonPropertyName("detalle")] public string Detalle { get; set; } = "";
}

internal sealed class PagoPendiente
{
	[Description("Como llama la persona a este gasto.")]
	[JsonPropertyName("gasto")] public string Gasto { get; set; } = "";

	[Description("false si nunca se cobro: de eso no hay nada que echar de menos, y es distinto "
		+ "de estar al dia.")]
	[JsonPropertyName("hayHistoria")] public bool HayHistoria { get; set; }

	[JsonPropertyName("estaVencido")] public bool EstaVencido { get; set; }

	[Description("Cuando se esperaba este cargo.")]
	[JsonConverter(typeof(Presupuesto.App.FechaDelPrint))]
	[JsonPropertyName("esperado")] public DateTime Esperado { get; set; }

	[Description("Cuantos dias lleva sin aparecer. Negativo significa que todavia no le toca.")]
	[JsonPropertyName("diasDeAtraso")] public int DiasDeAtraso { get; set; }

	[Description("informativo, seguimiento, atencion o critico. LA DECIDE LA CLASE DE "
		+ "COMPROMISO, no el monto: dos ausencias del mismo tamano pueden ser muy distintas.")]
	[JsonPropertyName("severidad")] public string Severidad { get; set; } = "";

	[JsonPropertyName("ordenDeLaSeveridad")] public int OrdenDeLaSeveridad { get; set; }

	[Description("Que corresponde hacer. Vacio significa que NO hay nada que hacer: no inventes "
		+ "una recomendacion para llenar el hueco.")]
	[JsonPropertyName("accion")] public string Accion { get; set; } = "";

	[Description("La lectura en una linea, escrita por el propio dominio. Deci esto, no lo "
		+ "reformules en un consejo generico.")]
	[JsonPropertyName("lectura")] public string Lectura { get; set; } = "";
}

internal sealed class MargenParaUnCompromiso
{
	[JsonPropertyName("hayDatos")] public bool HayDatos { get; set; }

	[Description("La mensualidad nueva que se esta evaluando.")]
	[JsonPropertyName("mensualidad")] public decimal Mensualidad { get; set; }

	[Description("Lo que se SABE recortable: consumo discrecional.")]
	[JsonPropertyName("discrecional")] public decimal Discrecional { get; set; }

	[Description("Compromisos ya adquiridos. No es que cueste recortarlos: es que no dependen de "
		+ "la voluntad de este mes.")]
	[JsonPropertyName("comprometido")] public decimal Comprometido { get; set; }

	[Description("Plata que NADIE clasifico todavia. NO es cero y NO es irrecortable: es "
		+ "DESCONOCIDA. Si este numero es grande, la respuesta se queda corta.")]
	[JsonPropertyName("porDefinir")] public decimal PorDefinir { get; set; }

	[Description("Cuantos gastos estan sin marcar.")]
	[JsonPropertyName("cuantosSinMarcar")] public int CuantosSinMarcar { get; set; }

	[Description("El techo optimista: todo lo discrecional a cero. NO es una recomendacion; "
		+ "nadie vive recortando el 100% de su ocio.")]
	[JsonPropertyName("techo")] public decimal Techo { get; set; }

	[JsonPropertyName("alcanza")] public bool Alcanza { get; set; }

	[Description("false cuando queda plata sin marcar que podria cambiar la respuesta. Ahi la "
		+ "respuesta honesta NO es 'no alcanza': es 'no alcanza con lo que sabemos, y falta "
		+ "clasificar esto'.")]
	[JsonPropertyName("respuestaCompleta")] public bool RespuestaCompleta { get; set; }

	[JsonPropertyName("lectura")] public string Lectura { get; set; } = "";

	[Description("Sobre que mes se calculo. Es el ultimo mes COMPLETO: el mes en curso daria un "
		+ "total a medias y una respuesta optimista.")]
	[JsonPropertyName("mesDeReferencia")] public DateTime MesDeReferencia { get; set; }

	[JsonPropertyName("carpeta")] public string Carpeta { get; set; } = "";

	[JsonPropertyName("detalle")] public string Detalle { get; set; } = "";
}

internal sealed class TasaDeImprevistosParaLaIA
{
	[JsonPropertyName("hayDatos")] public bool HayDatos { get; set; }

	[Description("Cuantos EVENTOS imprevistos hubo. No cargos: una hospitalizacion con tres "
		+ "facturas cuenta como UNO.")]
	[JsonPropertyName("cuantos")] public int Cuantos { get; set; }

	[Description("Cuanto sumaron entre todos.")]
	[JsonPropertyName("total")] public decimal Total { get; set; }

	[JsonPropertyName("meses")] public decimal Meses { get; set; }

	[Description("false cuando hay menos de dos eventos. Con uno solo no hay tasa: hay una "
		+ "anecdota, y NO se puede estimar cuanto apartar. Decilo en vez de promediar.")]
	[JsonPropertyName("hayTasa")] public bool HayTasa { get; set; }

	[Description("Cuanto conviene apartar por mes, segun lo que le paso a ESTA persona.")]
	[JsonPropertyName("porMes")] public decimal PorMes { get; set; }

	[Description("Cada cuantos meses ocurre uno. Es mas facil de creer que un promedio mensual.")]
	[JsonPropertyName("cadaCuantosMeses")] public decimal CadaCuantosMeses { get; set; }

	[JsonPropertyName("lectura")] public string Lectura { get; set; } = "";

	[JsonPropertyName("desde")] public DateTime Desde { get; set; }
	[JsonPropertyName("hasta")] public DateTime Hasta { get; set; }

	[JsonPropertyName("carpeta")] public string Carpeta { get; set; } = "";

	[JsonPropertyName("detalle")] public string Detalle { get; set; } = "";
}
