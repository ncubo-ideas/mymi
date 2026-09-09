using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;
using ModelContextProtocol.Server;
using Municipalidad.App;

namespace Municipalidad.Mcp.Tools
{
	/// <summary>
	/// La pregunta del asistente sobre CUANTO HA PAGADO ESTA PERSONA: el total de un anio y la serie de
	/// comprobantes de cada rubro. Solo lectura, y su unico parametro dice de que anio se pregunta.
	/// </summary>
	[McpServerToolType]
	internal sealed class PreguntasDeLoPagado
	{
		private readonly LectorDeJournal lector;

		public PreguntasDeLoPagado(LectorDeJournal lector)
		{
			ArgumentNullException.ThrowIfNull(lector);

			this.lector = lector;
		}

		[McpServerTool(Name = "cuanto_he_pagado", UseStructuredContent = true)]
		[Description("Contesta cuánto ha pagado esta persona a su municipalidad en un año, y trae la "
			+ "serie completa de comprobantes de cada rubro: qué período cubre cada línea, cuánto costó y "
			+ "cuánta mora traía. Úsela para '¿cuánto he pagado este año?', '¿cuánto llevo gastado en "
			+ "basura?' y '¿qué he pagado?'. "
			+ "EL TOTAL INCLUYE LA MORA QUE VINO EN CADA RECIBO, porque es lo que la persona pagó de "
			+ "verdad. No lo presente como el monto del servicio. "
			+ "UNA LÍNEA DE RECIBO PUEDE CUBRIR VARIOS PERÍODOS, y es lo que hace que estos números no se "
			+ "puedan leer de un recibo. cubre dice cuántos, y porPeriodo dice cuánto sale cada uno. Use "
			+ "porPeriodo cuando compare dos épocas: comparar montos de líneas que cubren distinta "
			+ "cantidad de períodos no compara nada. "
			+ "inferido MARCA LO QUE LA APP DEDUJO Y ES IMPORTANTE. Cuando una línea es la primera de un "
			+ "rubro no hay línea anterior con qué saber cuántos períodos cubre, así que la app supone "
			+ "uno. Esa fila viene con inferido en true. Dígalo cuando la cite: es una suposición de esta "
			+ "app y no un dato del recibo. "
			+ "estado DICE POR QUÉ UN RUBRO NO TRAE SERIE. 'con-historia' es que sí trae; 'sin-historia' "
			+ "es que esta app no ha visto ni un comprobante de ese rubro, que NO quiere decir que la "
			+ "persona no lo pague; 'rubro-desconocido' no debería salir de aquí, porque sólo se "
			+ "preguntan rubros del catálogo. "
			+ "UN AÑO EN CERO NO ES UN AÑO SIN PAGOS. Puede ser que los pagos de ese año no hayan llegado "
			+ "todavía a esta app. Mire los períodos de las series para ver de qué años sí hay "
			+ "comprobantes, y si el año que pidió no aparece ahí, dígalo así en vez de reportar cero "
			+ "pelado. "
			+ "anio ES EL AÑO POR EL QUE SE PREGUNTA, no un dato de la cuenta. OMÍTALO si la persona no "
			+ "nombró ninguno y se contesta por el año en curso. La respuesta siempre dice con qué año "
			+ "contestó: cítelo. Las series vienen enteras y no las recorta el año, a propósito, para que "
			+ "se vea de qué años hay comprobantes.")]
		public LoPagadoPorEstaPersona CuantoHePagado(
			[Description("El año por el que se pregunta, en cuatro dígitos. Omítalo para el año en "
				+ "curso. Es el alcance de la pregunta, no un hecho de la cuenta de la persona.")]
			int anio = 0)
		{
			int elAnio = anio > 0 ? anio : ElRelojQueLeeElAsistente.ElInstanteEnQueSePregunto().Year;

			return lector.Leer(
				api => LoQuePagoEn(api, elAnio, anio > 0),
				() => new LoPagadoPorEstaPersona
				{
					HayDatos = false,
					Anio = elAnio,
					ElAnioLoPidioQuienPregunta = anio > 0,
					Carpeta = lector.Carpeta,
					Detalle = LoQueSeLeDiceAlAsistente.LaAppNuncaSeAbrio,
				});
		}

		private LoPagadoPorEstaPersona LoQuePagoEn(MunicipalidadAPI api, int elAnio, bool loPidieron)
		{
			decimal total = api.LoPagadoEn(elAnio);

			List<SerieDeUnRubro> series = new List<SerieDeUnRubro>();

			foreach (string rubro in api.LosRubrosQueHay())
			{
				RegistroDelHistorial historial = api.HistorialDe(rubro);

				List<LineaDeComprobante> lineas = new List<LineaDeComprobante>();

				for (int fila = 0; fila < historial.Periodo.Length; fila++)
					lineas.Add(new LineaDeComprobante
					{
						Periodo = historial.Periodo[fila],
						Monto = historial.Monto[fila],
						Mora = historial.Mora[fila],
						Cubre = historial.Cubre[fila],
						PorPeriodo = historial.PorPeriodo[fila],
						Inferido = historial.Inferido[fila],
					});

				series.Add(new SerieDeUnRubro
				{
					Rubro = rubro,
					Estado = historial.Estado,
					Dice = historial.EstadoDicho,
					Lineas = lineas,
				});
			}

			return new LoPagadoPorEstaPersona
			{
				HayDatos = true,
				Anio = elAnio,
				ElAnioLoPidioQuienPregunta = loPidieron,
				TotalDelAnio = total,
				Series = series,
				Carpeta = lector.Carpeta,
				Detalle = LoQueSeLeDiceAlAsistente.TodoLoQueValeLaPenaDecir(
					LoQueSeLeDiceAlAsistente.LaFraseEsLaRespuesta,
					ElTotalTraeLaMoraAdentro,
					total == 0m ? UnAnioEnCeroNoEsUnAnioSinPagos : "",
					LoQueLaAppInfirioVaMarcado,
					LoQueSeLeDiceAlAsistente.ElRubroViajaComoLlave,
					LoQueSeLeDiceAlAsistente.LaAppRecibeYAvisaPeroNoVerifica,
					LoQueSeLeDiceAlAsistente.LoQueFaltaLoTraeUnRefresco),
			};
		}

		private const string ElTotalTraeLaMoraAdentro =
			"EL TOTAL DEL AÑO INCLUYE LA MORA que vino en cada recibo, porque es lo que la persona pagó "
			+ "de verdad. Si le interesa separar el servicio de la mora, están en columnas distintas en "
			+ "cada línea; no reste usted un total de otro.";

		private const string UnAnioEnCeroNoEsUnAnioSinPagos =
			"EL TOTAL DE ESE AÑO VIENE EN CERO, y eso no es lo mismo que un año sin pagos. Puede ser que "
			+ "los comprobantes de ese año no hayan llegado todavía a esta app. Mire los períodos de las "
			+ "series: dicen de qué años sí hay comprobantes. Si el año pedido no aparece ahí, dígalo así "
			+ "en vez de reportar cero pelado.";

		private const string LoQueLaAppInfirioVaMarcado =
			"LAS FILAS CON inferido EN true LAS DEDUJO ESTA APP. Cuando una línea es la primera de un "
			+ "rubro no hay con qué saber cuántos períodos cubre, así que se supone uno. Es una "
			+ "suposición de esta app y no un dato del recibo: dígalo cuando cite esa fila.";
	}

	/// <summary>Una linea de comprobante como fila, con cuantos periodos cubre.</summary>
	internal sealed class LineaDeComprobante
	{
		[JsonPropertyName("periodo")]
		[Description("El período más reciente que esta línea cubre, en el alfabeto de su rubro.")]
		public string Periodo { get; init; } = "";

		[JsonPropertyName("monto")]
		[Description("Lo que costó esta línea entera, cubra un período o varios.")]
		public decimal Monto { get; init; }

		[JsonPropertyName("mora")]
		[Description("La mora que venía en el recibo por esta línea. Es un hecho recibido: esta app no "
			+ "la calcula ni la discute.")]
		public decimal Mora { get; init; }

		[JsonPropertyName("cubre")]
		[Description("Cuántos períodos cubre esta línea. Un recibo puede pagar varios meses de una vez.")]
		public int Cubre { get; init; }

		[JsonPropertyName("porPeriodo")]
		[Description("Lo que sale UN período de esta línea. Es la columna con la que se comparan dos "
			+ "épocas; el monto entero no compara nada si las líneas cubren distinta cantidad.")]
		public decimal PorPeriodo { get; init; }

		[JsonPropertyName("inferido")]
		[Description("true cuando esta app DEDUJO cuántos períodos cubre la línea, porque no había línea "
			+ "anterior con qué saberlo. Es una suposición de la app: dígalo.")]
		public bool Inferido { get; init; }
	}

	/// <summary>La serie de comprobantes de un rubro, con el estado que dice por que esta o falta.</summary>
	internal sealed class SerieDeUnRubro
	{
		[JsonPropertyName("rubro")]
		[Description("El rubro de esta serie, por su nombre en el registro.")]
		public string Rubro { get; init; } = "";

		[JsonPropertyName("estado")]
		[Description("'con-historia', 'sin-historia' o 'rubro-desconocido'. 'sin-historia' quiere decir "
			+ "que esta app no ha visto un comprobante de ese rubro, NO que la persona no lo pague.")]
		public string Estado { get; init; } = "";

		[JsonPropertyName("dice")]
		[Description("LA RESPUESTA de esta serie, en una frase que compuso el dominio. Dígala.")]
		public string Dice { get; init; } = "";

		[JsonPropertyName("lineas")]
		[Description("Una fila por línea de comprobante, de la más vieja a la más reciente. Vacío cuando "
			+ "el rubro no tiene historia en esta app.")]
		public IReadOnlyList<LineaDeComprobante> Lineas { get; init; } = new List<LineaDeComprobante>();
	}

	/// <summary>El total de un anio y la serie de comprobantes de cada rubro del catalogo.</summary>
	internal sealed class LoPagadoPorEstaPersona
	{
		[JsonPropertyName("hayDatos")]
		[Description("false cuando la app todavía no se ha abierto en esta computadora; entonces el "
			+ "resto viene vacío.")]
		public bool HayDatos { get; init; }

		[JsonPropertyName("anio")]
		[Description("EL AÑO CON QUE SE CONTESTÓ EL TOTAL. Cítelo: si quien preguntó no nombró ninguno, "
			+ "es el año en curso y puede no ser el que la persona tenía en mente.")]
		public int Anio { get; init; }

		[JsonPropertyName("elAnioLoPidioQuienPregunta")]
		[Description("true cuando el año vino en la pregunta; false cuando se tomó el año en curso "
			+ "porque nadie nombró uno.")]
		public bool ElAnioLoPidioQuienPregunta { get; init; }

		[JsonPropertyName("totalDelAnio")]
		[Description("LO QUE ESTA PERSONA PAGÓ EN ESE AÑO según los comprobantes que esta app vio, mora "
			+ "incluida. Cero no es un año sin pagos: puede ser un año que la app no ha traído.")]
		public decimal TotalDelAnio { get; init; }

		[JsonPropertyName("series")]
		[Description("Una serie por rubro del catálogo, ENTERA y no recortada por el año, para que se "
			+ "vea de qué años hay comprobantes.")]
		public IReadOnlyList<SerieDeUnRubro> Series { get; init; } = new List<SerieDeUnRubro>();

		[JsonPropertyName("carpeta")]
		[Description("La carpeta de la persona, donde su app guarda el registro de su cuenta.")]
		public string Carpeta { get; init; } = "";

		[JsonPropertyName("detalle")]
		[Description("Lo que esta respuesta no dice, y lo que no hay que hacer con ella. Léalo antes de "
			+ "contestar.")]
		public string Detalle { get; init; } = "";
	}
}
