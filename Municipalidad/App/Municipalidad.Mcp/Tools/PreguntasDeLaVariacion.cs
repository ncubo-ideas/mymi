using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;
using ModelContextProtocol.Server;
using Municipalidad.App;

namespace Municipalidad.Mcp.Tools
{
	/// <summary>
	/// La pregunta del asistente sobre POR QUE UN COBRO SUBIO: la variacion de cada rubro entre sus dos
	/// ultimos comprobantes, con lo que la explica. Solo lectura, y sin un solo parametro.
	/// </summary>
	[McpServerToolType]
	internal sealed class PreguntasDeLaVariacion
	{
		private readonly LectorDeJournal lector;

		public PreguntasDeLaVariacion(LectorDeJournal lector)
		{
			System.ArgumentNullException.ThrowIfNull(lector);

			this.lector = lector;
		}

		[McpServerTool(Name = "por_que_pago_mas", UseStructuredContent = true)]
		[Description("Contesta por qué subió lo que esta persona paga, rubro por rubro: entre cuáles dos "
			+ "períodos cambió, de cuánto a cuánto, y QUÉ LO EXPLICA. Úsela para '¿por qué este trimestre "
			+ "pago más?', '¿por qué me subió la basura?', '¿por qué subió mi IBI?' y '¿qué cambió desde "
			+ "mi último pago?'. "
			+ "LO QUE EXPLICA UN AUMENTO DEPENDE DE CÓMO SE COBRA ESE RUBRO, y ésa es la mitad que ningún "
			+ "portal contesta. Basura y policía se cobran POR UNIDAD, así que si suben es porque la "
			+ "municipalidad cambió la TARIFA. Limpieza, bienes inmuebles y parques se cobran SOBRE EL "
			+ "VALOR DE LA FINCA, así que si suben es porque cambió el AVALÚO. El mismo +80 % significa "
			+ "dos cosas distintas según el rubro, y el registro ya decidió cuál: está en explicacion y "
			+ "está dicho en dice. "
			+ "explicacion ES UNA DE CUATRO PALABRAS. 'cambio-de-tarifa' es la municipalidad cobrando "
			+ "distinto por el mismo servicio; 'cambio-de-avaluo' es el valor de la finca moviéndose, por "
			+ "una declaración nueva o un avalúo nuevo; 'cambio-menor' es un cambio real pero por debajo "
			+ "del umbral con que se preguntó; 'sin-cambio' es ruido. No las mezcle y no invente una "
			+ "quinta. "
			+ "UN AUMENTO NUNCA APURA, y urgencia lo dice: siempre viene 'ninguna'. Un aumento ya pasó -- "
			+ "lo que apura es lo que vence, y eso lo contesta que_se_me_vence. No presente un aumento "
			+ "como algo que hay que atender hoy. "
			+ "CON UN SOLO COMPROBANTE NO HAY VARIACIÓN Y ESO ES UNA RESPUESTA. sePuedeVer en false "
			+ "quiere decir que este rubro no tiene dos comprobantes que comparar; dice lo dice con "
			+ "palabras. NO es que el cobro no haya subido, y no es un hueco que usted pueda llenar "
			+ "restando dos números de otra tool. "
			+ "ESTA TOOL NO RECIBE NINGÚN PARÁMETRO Y NO ES UN OLVIDO. Contesta por los cinco rubros de "
			+ "una vez, así que si la persona preguntó por uno, encuéntrelo en las filas en vez de pedir "
			+ "otra cosa. El umbral y el ruido con que se midió salen en la respuesta y no se los puede "
			+ "pasar usted.")]
		public VariacionesDeLaCuenta PorQuePagoMas()
		{
			return lector.Leer(LaVariacionDeCadaRubro, () => new VariacionesDeLaCuenta
			{
				HayDatos = false,
				Carpeta = lector.Carpeta,
				Detalle = LoQueSeLeDiceAlAsistente.LaAppNuncaSeAbrio,
			});
		}

		private VariacionesDeLaCuenta LaVariacionDeCadaRubro(MunicipalidadAPI api)
		{
			List<string> conHistoria = new List<string>(api.RubrosConHistoria());

			List<VariacionDeUnRubro> filas = new List<VariacionDeUnRubro>();

			foreach (string rubro in api.LosRubrosQueHay())
			{
				bool tieneHistoria = conHistoria.Contains(rubro);

				if (!tieneHistoria)
				{
					filas.Add(new VariacionDeUnRubro
					{
						Rubro = rubro,
						TieneHistoria = false,
						SePuedeVer = false,
						Dice = EsteRubroNoTieneNiUnComprobante,
					});

					continue;
				}

				if (!api.SePuedeVerLaVariacionDe(rubro))
				{
					filas.Add(new VariacionDeUnRubro
					{
						Rubro = rubro,
						TieneHistoria = true,
						SePuedeVer = false,
						Dice = EsteRubroTieneUnSoloComprobante,
					});

					continue;
				}

				RegistroDeLaVariacion variacion =
					api.VariacionDe(rubro, LosJuiciosSinFirmar.UmbralDeSalto, LosJuiciosSinFirmar.Ruido, ConsultasDelModulo.IdiomaDelRegistro);

				filas.Add(new VariacionDeUnRubro
				{
					Rubro = variacion.Rubro,
					TieneHistoria = true,
					SePuedeVer = true,
					Desde = variacion.Desde,
					Hasta = variacion.Hasta,
					Antes = variacion.Antes,
					Ahora = variacion.Ahora,
					Proporcion = variacion.Proporcion,
					Explicacion = variacion.Explicacion,
					EsSalto = variacion.EsSalto,
					Urgencia = variacion.Urgencia,
					Dice = variacion.Dice,
				});
			}

			return new VariacionesDeLaCuenta
			{
				HayDatos = true,
				UmbralDeSalto = LosJuiciosSinFirmar.UmbralDeSalto,
				Ruido = LosJuiciosSinFirmar.Ruido,
				Rubros = filas,
				Carpeta = lector.Carpeta,
				Detalle = LoQueSeLeDiceAlAsistente.TodoLoQueValeLaPenaDecir(
					LoQueSeLeDiceAlAsistente.LaFraseEsLaRespuesta,
					LoQueSeLeDiceAlAsistente.NadieLosHaFirmado,
					UnAumentoNoApuraYEsoLoDiceElRegistro,
					LoQueSeLeDiceAlAsistente.ElRubroViajaComoLlave,
					LoQueSeLeDiceAlAsistente.LaAppRecibeYAvisaPeroNoVerifica,
					LoQueSeLeDiceAlAsistente.LoQueFaltaLoTraeUnRefresco),
			};
		}

		private const string EsteRubroNoTieneNiUnComprobante =
			"Esta app no ha visto ni un comprobante de este rubro, así que no hay nada que comparar. Eso "
			+ "no quiere decir que la persona no lo pague: quiere decir que sus pagos de este rubro no "
			+ "han llegado a esta app.";

		private const string EsteRubroTieneUnSoloComprobante =
			"Este rubro tiene un solo período con comprobante, y una variación necesita dos. No es que "
			+ "el cobro no haya cambiado: es que todavía no hay contra qué compararlo.";

		private const string UnAumentoNoApuraYEsoLoDiceElRegistro =
			"UN AUMENTO NUNCA ES URGENTE Y urgencia SIEMPRE DICE 'ninguna'. Es una decisión del registro "
			+ "y no un descuido: un aumento ya ocurrió, y lo que sí puede apurar es un vencimiento. No "
			+ "presente un aumento como algo que hay que resolver hoy, y si la persona pregunta qué tiene "
			+ "que hacer ya, eso lo contesta que_se_me_vence.";
	}

	/// <summary>Un rubro y su variacion, o la constancia en palabras de por que no la tiene.</summary>
	internal sealed class VariacionDeUnRubro
	{
		[JsonPropertyName("rubro")]
		[Description("El rubro del que habla esta fila, por su nombre en el registro. Es una llave, no "
			+ "un título: no lo traduzca ni lo abrevie.")]
		public string Rubro { get; init; } = "";

		[JsonPropertyName("tieneHistoria")]
		[Description("false cuando esta app no ha visto ni un comprobante de este rubro. NO quiere decir "
			+ "que la persona no lo pague.")]
		public bool TieneHistoria { get; init; }

		[JsonPropertyName("sePuedeVer")]
		[Description("false cuando no hay dos períodos que comparar. Entonces los números vienen en cero "
			+ "y NO se pueden usar: lea dice.")]
		public bool SePuedeVer { get; init; }

		[JsonPropertyName("desde")]
		[Description("El período del comprobante anterior, escrito en el alfabeto de su rubro: los "
			+ "mensuales dicen '2025-06' y los trimestrales '2025-T2'.")]
		public string Desde { get; init; } = "";

		[JsonPropertyName("hasta")]
		[Description("El período del comprobante más reciente, en el mismo alfabeto.")]
		public string Hasta { get; init; } = "";

		[JsonPropertyName("antes")]
		[Description("Lo que costaba UN período en el comprobante anterior. Ya viene prorrateado: una "
			+ "línea que cubría tres meses ya está dividida entre tres.")]
		public decimal Antes { get; init; }

		[JsonPropertyName("ahora")]
		[Description("Lo que cuesta UN período en el comprobante más reciente, prorrateado igual.")]
		public decimal Ahora { get; init; }

		[JsonPropertyName("proporcion")]
		[Description("El cambio como proporción: 0.6 es un 60 % más. Sale ya calculado; no lo derive "
			+ "dividiendo los dos montos.")]
		public decimal Proporcion { get; init; }

		[JsonPropertyName("explicacion")]
		[Description("QUÉ EXPLICA EL CAMBIO, en una de cuatro palabras: 'cambio-de-tarifa', "
			+ "'cambio-de-avaluo', 'cambio-menor' o 'sin-cambio'. Lo decidió la ley de cálculo del rubro, "
			+ "así que no lo deduzca del monto.")]
		public string Explicacion { get; init; } = "";

		[JsonPropertyName("esSalto")]
		[Description("true cuando el cambio pasa el umbral con que se preguntó. false puede ser un "
			+ "cambio real por debajo del umbral: lea explicacion para saber cuál de los dos.")]
		public bool EsSalto { get; init; }

		[JsonPropertyName("urgencia")]
		[Description("Siempre 'ninguna' en un aumento, a propósito: un aumento ya pasó. Lo que apura es "
			+ "un vencimiento.")]
		public string Urgencia { get; init; } = "";

		[JsonPropertyName("dice")]
		[Description("LA RESPUESTA, en una frase que compuso el dominio: de cuánto a cuánto, entre "
			+ "cuáles períodos, y por qué. Dígala en vez de rearmarla.")]
		public string Dice { get; init; } = "";
	}

	/// <summary>La variacion de cada rubro del catalogo, con los numeros con que se pregunto.</summary>
	internal sealed class VariacionesDeLaCuenta
	{
		[JsonPropertyName("hayDatos")]
		[Description("false cuando la app todavía no se ha abierto en esta computadora; entonces el "
			+ "resto viene vacío.")]
		public bool HayDatos { get; init; }

		[JsonPropertyName("umbralDeSalto")]
		[Description("EL UMBRAL CON QUE SE MIDIÓ: 0.25 quiere decir que un cambio de más de 25 % se "
			+ "llama salto. Lo puso esta app y NADIE LO HA FIRMADO. Dígalo.")]
		public decimal UmbralDeSalto { get; init; }

		[JsonPropertyName("ruido")]
		[Description("Por debajo de esta proporción un cambio se considera ruido y no se avisa. Lo puso "
			+ "esta app y NADIE LO HA FIRMADO.")]
		public decimal Ruido { get; init; }

		[JsonPropertyName("rubros")]
		[Description("Una fila por rubro del catálogo, incluidos los que no tienen con qué contestar. "
			+ "Una fila sin variación es una respuesta, no un hueco.")]
		public IReadOnlyList<VariacionDeUnRubro> Rubros { get; init; } = new List<VariacionDeUnRubro>();

		[JsonPropertyName("carpeta")]
		[Description("La carpeta de la persona, donde su app guarda el registro de su cuenta.")]
		public string Carpeta { get; init; } = "";

		[JsonPropertyName("detalle")]
		[Description("Lo que esta respuesta no dice, y lo que no hay que hacer con ella. Léalo antes de "
			+ "contestar.")]
		public string Detalle { get; init; } = "";
	}
}
