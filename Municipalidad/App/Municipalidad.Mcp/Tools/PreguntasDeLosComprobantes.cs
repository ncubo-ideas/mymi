using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;
using ModelContextProtocol.Server;
using Municipalidad.App;

namespace Municipalidad.Mcp.Tools
{
	/// <summary>
	/// La pregunta del asistente sobre QUE FIGURA PAGADO SIN COMPROBANTE: los periodos que la ultima
	/// consulta al portal da por pagados y de los que esta app nunca vio un recibo. Solo lectura, y sin
	/// un solo parametro.
	/// </summary>
	[McpServerToolType]
	internal sealed class PreguntasDeLosComprobantes
	{
		private readonly LectorDeJournal lector;

		public PreguntasDeLosComprobantes(LectorDeJournal lector)
		{
			ArgumentNullException.ThrowIfNull(lector);

			this.lector = lector;
		}

		[McpServerTool(Name = "que_quedo_sin_comprobante", UseStructuredContent = true)]
		[Description("Contesta qué períodos figuran pagados en el portal de la municipalidad y esta app "
			+ "nunca vio pagar: los que quedaron SIN COMPROBANTE. Úsela para '¿tengo todos mis "
			+ "comprobantes?', '¿me falta algún recibo?' y '¿pagué esto en ventanilla?'. "
			+ "QUÉ ES UN PERÍODO SIN COMPROBANTE. La app coteja dos cosas: lo que la última consulta al "
			+ "portal dejó de mostrar como pendiente, y los recibos que la app sí vio. Un período que el "
			+ "portal da por saldado y que no tiene recibo aquí probablemente se pagó por un camino que "
			+ "no deja comprobante digital -- ventanilla, por ejemplo. NO ES UN PAGO PERDIDO Y NO ES UNA "
			+ "DEUDA: es un pago sin respaldo en esta app. "
			+ "SE DICE, NO SE TAPA, y ésa es la decisión detrás de esta tool: un hueco nombrado es una "
			+ "respuesta; un hueco callado se ve igual que no tener huecos. "
			+ "estado ES DE TRES PALABRAS Y DOS DE ELLAS SE PARECEN Y NO SON LO MISMO. "
			+ "'hay-periodos-sin-comprobante' es que sí quedaron períodos así, y vienen en las filas. "
			+ "'nada-sin-comprobante' es que se cotejó y no quedó ninguno: todo lo que el portal da por "
			+ "pagado, esta app lo vio pagar. 'sin-consulta' es que NO SE PUDO COTEJAR porque no hay "
			+ "ninguna consulta al portal registrada, y ESO NO QUIERE DECIR QUE TODO ESTÉ EN ORDEN: "
			+ "quiere decir que la pregunta todavía no se puede contestar. Nunca reporte 'sin-consulta' "
			+ "como si fuera 'nada-sin-comprobante'. "
			+ "LA CONSULTA ES UNA FOTO CON FECHA. El portal dice qué está pendiente HOY, y mañana dice "
			+ "otra cosa, así que el cotejo es contra el día de la consulta más reciente que la app "
			+ "registró y no contra hoy. Ese día sale en la respuesta: cítelo. "
			+ "ESTA TOOL NO RECIBE NINGUNA FECHA Y NO ES UN OLVIDO. El día del cotejo tiene que ser un "
			+ "día en que hubo consulta, y eso lo sabe el registro. Si la persona le nombra una fecha, no "
			+ "la use: no hay por dónde pasarla.")]
		public LoQueQuedoSinComprobante QueQuedoSinComprobante()
		{
			return lector.Leer(ElCotejoDeLaUltimaConsulta, () => new LoQueQuedoSinComprobante
			{
				HayDatos = false,
				Carpeta = lector.Carpeta,
				Detalle = LoQueSeLeDiceAlAsistente.LaAppNuncaSeAbrio,
			});
		}

		private LoQueQuedoSinComprobante ElCotejoDeLaUltimaConsulta(MunicipalidadAPI api)
		{
			if (!api.HayAlgunaConsulta())
				return new LoQueQuedoSinComprobante
				{
					HayDatos = true,
					HayAlgunaConsulta = false,
					Estado = "sin-consulta",
					Dice = NuncaSeHaRegistradoUnaConsulta,
					Carpeta = lector.Carpeta,
					Detalle = LoQueSeLeDiceAlAsistente.TodoLoQueValeLaPenaDecir(
						NoPoderCotejarNoEsEstarEnOrden,
						LoQueSeLeDiceAlAsistente.LoQueFaltaLoTraeUnRefresco),
				};

			DateTime laConsulta = api.ConsultaMasReciente();

			RegistroDePeriodosSinComprobante cotejo = api.PeriodosPagadosSinComprobante(laConsulta);

			List<PeriodoSinRecibo> filas = new List<PeriodoSinRecibo>();

			for (int fila = 0; fila < cotejo.Rubro.Length; fila++)
				filas.Add(new PeriodoSinRecibo
				{
					Rubro = cotejo.Rubro[fila],
					Periodo = cotejo.Periodo[fila],
					Dice = cotejo.Dice[fila],
				});

			return new LoQueQuedoSinComprobante
			{
				HayDatos = true,
				HayAlgunaConsulta = true,
				DiaDeLaConsulta = ElRelojQueLeeElAsistente.ElDiaDe(laConsulta),
				Estado = cotejo.Estado,
				Dice = cotejo.EstadoDicho,
				Periodos = filas,
				Carpeta = lector.Carpeta,
				Detalle = LoQueSeLeDiceAlAsistente.TodoLoQueValeLaPenaDecir(
					LoQueSeLeDiceAlAsistente.LaFraseEsLaRespuesta,
					UnPeriodoSinComprobanteNoEsUnaDeuda,
					NoPoderCotejarNoEsEstarEnOrden,
					LoQueSeLeDiceAlAsistente.ElRubroViajaComoLlave,
					LoQueSeLeDiceAlAsistente.LoQueFaltaLoTraeUnRefresco),
			};
		}

		private const string NuncaSeHaRegistradoUnaConsulta =
			"No hay ninguna consulta al portal registrada en esta app, así que no hay con qué cotejar lo "
			+ "que la app vio pagar. Esto NO dice que todo esté en orden: dice que la pregunta todavía no "
			+ "se puede contestar, y se va a poder en cuanto entre una consulta.";

		private const string UnPeriodoSinComprobanteNoEsUnaDeuda =
			"UN PERÍODO SIN COMPROBANTE NO ES UNA DEUDA Y NO ES UN PAGO PERDIDO. El portal lo da por "
			+ "saldado; lo que falta es el respaldo en esta app, casi siempre porque se pagó por un "
			+ "camino que no deja comprobante digital. No le diga a la persona que debe ese período, y no "
			+ "lo sume a nada que se le venza.";

		private const string NoPoderCotejarNoEsEstarEnOrden =
			"NO PODER COTEJAR NO ES ESTAR EN ORDEN. Si el estado dice 'sin-consulta', la respuesta es que "
			+ "la pregunta no se puede contestar todavía, y presentarla como 'no falta ningún "
			+ "comprobante' sería decir algo que nadie midió.";
	}

	/// <summary>Un periodo que figura pagado y no tiene recibo en esta app, como fila.</summary>
	internal sealed class PeriodoSinRecibo
	{
		[JsonPropertyName("rubro")]
		[Description("El rubro de este período, por su nombre en el registro.")]
		public string Rubro { get; init; } = "";

		[JsonPropertyName("periodo")]
		[Description("El período, en el alfabeto de su rubro: '2024-07' en uno mensual, '2024-T3' en uno "
			+ "trimestral.")]
		public string Periodo { get; init; } = "";

		[JsonPropertyName("dice")]
		[Description("LA RESPUESTA de esta fila, en una frase que compuso el dominio. Dígala.")]
		public string Dice { get; init; } = "";
	}

	/// <summary>El cotejo contra la ultima consulta al portal, con su estado nombrado.</summary>
	internal sealed class LoQueQuedoSinComprobante
	{
		[JsonPropertyName("hayDatos")]
		[Description("false cuando la app todavía no se ha abierto en esta computadora; entonces el "
			+ "resto viene vacío.")]
		public bool HayDatos { get; init; }

		[JsonPropertyName("hayAlgunaConsulta")]
		[Description("false cuando no hay ninguna consulta al portal registrada. Entonces NO SE PUDO "
			+ "COTEJAR, que no es lo mismo que no falte ningún comprobante.")]
		public bool HayAlgunaConsulta { get; init; }

		[JsonPropertyName("diaDeLaConsulta")]
		[Description("EL DÍA DE LA CONSULTA CONTRA LA QUE SE COTEJÓ, en yyyy-MM-dd. No es hoy: es la "
			+ "foto del portal más reciente que la app registró. Cítelo.")]
		public string DiaDeLaConsulta { get; init; } = "";

		[JsonPropertyName("estado")]
		[Description("UNA DE TRES PALABRAS: 'hay-periodos-sin-comprobante', 'nada-sin-comprobante' o "
			+ "'sin-consulta'. Las dos últimas se parecen y NO son lo mismo: la primera es un cotejo que "
			+ "salió limpio, la segunda es un cotejo que no se pudo hacer.")]
		public string Estado { get; init; } = "";

		[JsonPropertyName("dice")]
		[Description("LA RESPUESTA, en una frase que compuso el dominio y que dice cuál de los tres "
			+ "estados es y qué significa. Dígala en vez de deducirla del estado.")]
		public string Dice { get; init; } = "";

		[JsonPropertyName("periodos")]
		[Description("Una fila por período que figura pagado sin comprobante. Vacío cuando el cotejo "
			+ "salió limpio o cuando no se pudo hacer: lea estado para saber cuál de los dos.")]
		public IReadOnlyList<PeriodoSinRecibo> Periodos { get; init; } = new List<PeriodoSinRecibo>();

		[JsonPropertyName("carpeta")]
		[Description("La carpeta de la persona, donde su app guarda el registro de su cuenta.")]
		public string Carpeta { get; init; } = "";

		[JsonPropertyName("detalle")]
		[Description("Lo que esta respuesta no dice, y lo que no hay que hacer con ella. Léalo antes de "
			+ "contestar.")]
		public string Detalle { get; init; } = "";
	}
}
