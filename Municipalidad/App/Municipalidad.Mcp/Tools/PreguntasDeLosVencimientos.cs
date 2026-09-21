using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;
using ModelContextProtocol.Server;
using Municipalidad.App;

namespace Municipalidad.Mcp.Tools
{
	/// <summary>
	/// La pregunta del asistente sobre QUE SE LE VENCE A ESTA PERSONA: lo que esta por vencer y lo que
	/// ya vencio, cada uno con la frase del registro. Solo lectura, y sin un solo parametro.
	/// </summary>
	[McpServerToolType]
	internal sealed class PreguntasDeLosVencimientos
	{
		private readonly LectorDeJournal lector;

		public PreguntasDeLosVencimientos(LectorDeJournal lector)
		{
			ArgumentNullException.ThrowIfNull(lector);

			this.lector = lector;
		}

		[McpServerTool(Name = "que_se_me_vence", UseStructuredContent = true)]
		[Description("Contesta qué se le vence a esta persona y qué ya se le venció, con la fecha de "
			+ "cada período, su monto y cuántos días faltan o cuántos lleva de atraso. Úsela para '¿qué "
			+ "se me vence?', '¿tengo algo pendiente?', '¿estoy atrasado?', '¿cuándo tengo que pagar?' y "
			+ "para cualquier pregunta sobre recordatorios de pago. "
			+ "ÉSTA ES LA PREGUNTA QUE EL PORTAL DE LA MUNICIPALIDAD NO CONTESTA, y es la razón de ser de "
			+ "esta app. El portal dice cuánto debe la persona HOY; no dice qué está por vencerse. Si la "
			+ "persona pregunta cuánto debe, eso lo ve en su portal; lo que trae esta tool es lo otro. "
			+ "SON DOS LISTAS Y NO SE MEZCLAN. porVencer son períodos que todavía NO se han vencido: "
			+ "faltan tantos días. vencidos son períodos que YA se vencieron y siguen sin pagar. No los "
			+ "junte en una sola lista ni sume sus montos en un total: son dos situaciones distintas y la "
			+ "persona hace cosas distintas con cada una. "
			+ "urgencia ES DE TRES PALABRAS Y 'aviso' NO ES UNA PARADA OBLIGATORIA. 'ninguna' es que "
			+ "todavía falta bastante; 'aviso' es un recordatorio de cortesía porque el vencimiento cae "
			+ "dentro de los días de aviso con que se preguntó; 'urgente' es un período ya vencido. No "
			+ "presente un 'aviso' como algo que hay que hacer hoy. "
			+ "EL COSTO DE PAGAR TARDE ES UNA PROYECCIÓN DE ESTA APP Y NO UN COBRO. costoProyectado sale "
			+ "de una tasa que esta app reconstruyó de recibos viejos, para dar una idea. La mora de "
			+ "verdad llega en el recibo de la municipalidad, esta app la recibe y no la recalcula. Nunca "
			+ "sume costoProyectado al monto y lo presente como lo que hay que pagar. "
			+ "UNA LISTA VACÍA ES UNA RESPUESTA. porVencer vacío quiere decir que a la fecha de hoy no "
			+ "hay nada dentro de la ventana de aviso, y vencidos vacío quiere decir que no hay nada "
			+ "vencido sin pagar en lo que esta app ha visto. Ninguna de las dos es un hueco. "
			+ "ESTA TOOL NO RECIBE NINGÚN PARÁMETRO. La fecha con que se contesta es AHORA, y sale en la "
			+ "respuesta: cítela. Los días de aviso y la tasa también salen y no se los puede pasar "
			+ "usted.")]
		public LoQueSeVence QueSeMeVence()
		{
			DateTime preguntadoEn = ElRelojQueLeeElAsistente.ElInstanteEnQueSePregunto();

			return lector.Leer(
				api => LoQueSeLeVenceAEstaPersona(api, preguntadoEn),
				() => new LoQueSeVence
				{
					HayDatos = false,
					PreguntadoEn = ElRelojQueLeeElAsistente.ElDiaDe(preguntadoEn),
					Carpeta = lector.Carpeta,
					Detalle = LoQueSeLeDiceAlAsistente.LaAppNuncaSeAbrio,
				});
		}

		private LoQueSeVence LoQueSeLeVenceAEstaPersona(MunicipalidadAPI api, DateTime preguntadoEn)
		{
			RegistroDeVencimientos vencimientos =
				api.VencimientosAl(preguntadoEn, LosJuiciosSinFirmar.DiasDeAviso, ConsultasDelModulo.IdiomaDelRegistro);

			RegistroDeAtrasos atrasos = api.AtrasosAl(preguntadoEn, LosJuiciosSinFirmar.TasaDeMora, ConsultasDelModulo.IdiomaDelRegistro);

			List<PeriodoPorVencer> porVencer = new List<PeriodoPorVencer>();

			for (int fila = 0; fila < vencimientos.Rubro.Length; fila++)
				porVencer.Add(new PeriodoPorVencer
				{
					Rubro = vencimientos.Rubro[fila],
					Periodo = vencimientos.Periodo[fila],
					Vence = vencimientos.Vence[fila],
					Monto = vencimientos.Monto[fila],
					Faltan = vencimientos.Faltan[fila],
					Urgencia = vencimientos.Urgencia[fila],
					Dice = vencimientos.Dice[fila],
				});

			List<PeriodoVencido> vencidos = new List<PeriodoVencido>();

			for (int fila = 0; fila < atrasos.Rubro.Length; fila++)
				vencidos.Add(new PeriodoVencido
				{
					Rubro = atrasos.Rubro[fila],
					Periodo = atrasos.Periodo[fila],
					Vencio = atrasos.Vencio[fila],
					Monto = atrasos.Monto[fila],
					Dias = atrasos.Dias[fila],
					CostoProyectado = atrasos.CostoProyectado[fila],
					Urgencia = atrasos.Urgencia[fila],
					Dice = atrasos.Dice[fila],
				});

			return new LoQueSeVence
			{
				HayDatos = true,
				PreguntadoEn = ElRelojQueLeeElAsistente.ElDiaDe(preguntadoEn),
				DiasDeAviso = LosJuiciosSinFirmar.DiasDeAviso,
				TasaConQueSeProyecto = LosJuiciosSinFirmar.TasaDeMora,
				PorVencer = porVencer,
				Vencidos = vencidos,
				Carpeta = lector.Carpeta,
				Detalle = LoQueSeLeDiceAlAsistente.TodoLoQueValeLaPenaDecir(
					LoQueSeLeDiceAlAsistente.LaFraseEsLaRespuesta,
					LasDosListasNoSeSuman,
					LoQueSeLeDiceAlAsistente.NadieLosHaFirmado,
					LoQueSeLeDiceAlAsistente.LaTasaSoloProyecta,
					porVencer.Count == 0 ? NadaPorVencerEsUnaRespuesta : "",
					vencidos.Count == 0 ? NadaVencidoEsUnaRespuesta : "",
					LoQueSeLeDiceAlAsistente.ElRubroViajaComoLlave,
					LoQueSeLeDiceAlAsistente.LoQueFaltaLoTraeUnRefresco),
			};
		}

		private const string LasDosListasNoSeSuman =
			"LAS DOS LISTAS NO SE SUMAN Y NO SE JUNTAN. Un período por vencer y uno vencido son "
			+ "situaciones distintas y la persona hace cosas distintas con cada una; un total que las "
			+ "mezcle no es un número que nadie haya calculado. Si la persona quiere saber cuánto debe en "
			+ "total, eso lo dice su portal, que es la autoridad sobre su deuda.";

		private const string NadaPorVencerEsUnaRespuesta =
			"NO HAY NADA POR VENCER a la fecha de hoy dentro de la ventana de aviso, y eso es una "
			+ "respuesta. No quiere decir que la persona no tenga nada que pagar más adelante: quiere "
			+ "decir que nada cae dentro de los días de aviso con que se preguntó.";

		private const string NadaVencidoEsUnaRespuesta =
			"NO HAY NADA VENCIDO SIN PAGAR en lo que esta app ha visto, y eso es una respuesta. No es una "
			+ "constancia de que la persona esté al día con la municipalidad: es lo que este registro "
			+ "tiene. Su portal es la autoridad sobre eso.";
	}

	/// <summary>Un periodo que todavia no vence, como fila.</summary>
	internal sealed class PeriodoPorVencer
	{
		[JsonPropertyName("rubro")]
		[Description("El rubro de este período, por su nombre en el registro.")]
		public string Rubro { get; init; } = "";

		[JsonPropertyName("periodo")]
		[Description("El período, en el alfabeto de su rubro: '2025-09' en uno mensual, '2025-T3' en uno "
			+ "trimestral.")]
		public string Periodo { get; init; } = "";

		[JsonPropertyName("vence")]
		[Description("El día en que vence, en yyyy-MM-dd. Es el último día del período; lo decidió la "
			+ "cadencia del rubro.")]
		public string Vence { get; init; } = "";

		[JsonPropertyName("monto")]
		[Description("Lo que la municipalidad puso al cobro por este período.")]
		public decimal Monto { get; init; }

		[JsonPropertyName("faltan")]
		[Description("Cuántos días faltan para el vencimiento, contados desde la fecha con que se "
			+ "preguntó.")]
		public int Faltan { get; init; }

		[JsonPropertyName("urgencia")]
		[Description("'ninguna' o 'aviso'. 'aviso' es un recordatorio de cortesía y NUNCA una parada "
			+ "obligatoria.")]
		public string Urgencia { get; init; } = "";

		[JsonPropertyName("dice")]
		[Description("LA RESPUESTA de esta fila, en una frase que compuso el dominio. Dígala.")]
		public string Dice { get; init; } = "";
	}

	/// <summary>Un periodo que ya vencio y sigue sin pagar, como fila.</summary>
	internal sealed class PeriodoVencido
	{
		[JsonPropertyName("rubro")]
		[Description("El rubro de este período, por su nombre en el registro.")]
		public string Rubro { get; init; } = "";

		[JsonPropertyName("periodo")]
		[Description("El período, en el alfabeto de su rubro.")]
		public string Periodo { get; init; } = "";

		[JsonPropertyName("vencio")]
		[Description("El día en que venció, en yyyy-MM-dd.")]
		public string Vencio { get; init; } = "";

		[JsonPropertyName("monto")]
		[Description("Lo que la municipalidad puso al cobro por este período.")]
		public decimal Monto { get; init; }

		[JsonPropertyName("dias")]
		[Description("Cuántos días lleva de atraso a la fecha con que se preguntó.")]
		public int Dias { get; init; }

		[JsonPropertyName("costoProyectado")]
		[Description("LO QUE ESTA APP PROYECTA que costaría de mora pagarlo ese día. NO es la mora que "
			+ "la municipalidad cobra: sale de una tasa reconstruida de recibos viejos. No lo sume al "
			+ "monto.")]
		public decimal CostoProyectado { get; init; }

		[JsonPropertyName("urgencia")]
		[Description("'urgente' en un período vencido. Esto sí vale la pena plantearlo.")]
		public string Urgencia { get; init; } = "";

		[JsonPropertyName("dice")]
		[Description("LA RESPUESTA de esta fila, en una frase que compuso el dominio, y ya dice que el "
			+ "costo es una proyección. Dígala entera.")]
		public string Dice { get; init; } = "";
	}

	/// <summary>Lo que esta por vencer y lo que ya vencio, con los numeros con que se pregunto.</summary>
	internal sealed class LoQueSeVence
	{
		[JsonPropertyName("hayDatos")]
		[Description("false cuando la app todavía no se ha abierto en esta computadora; entonces el "
			+ "resto viene vacío.")]
		public bool HayDatos { get; init; }

		[JsonPropertyName("preguntadoEn")]
		[Description("EL DÍA CON QUE SE CONTESTÓ, en yyyy-MM-dd. Cuántos días faltan y cuántos de atraso "
			+ "son respecto de este día y de ningún otro. Cítelo.")]
		public string PreguntadoEn { get; init; } = "";

		[JsonPropertyName("diasDeAviso")]
		[Description("Cuántos días antes del vencimiento se marca 'aviso'. Lo puso esta app y NADIE LO "
			+ "HA FIRMADO.")]
		public int DiasDeAviso { get; init; }

		[JsonPropertyName("tasaConQueSeProyecto")]
		[Description("La tasa anual con que se proyectó el costo de pagar tarde: 0.062 son 6,2 %. La "
			+ "reconstruyó esta app y NADIE LA HA FIRMADO. No es la mora de la municipalidad.")]
		public decimal TasaConQueSeProyecto { get; init; }

		[JsonPropertyName("porVencer")]
		[Description("Los períodos que todavía NO se han vencido. Vacío es una respuesta.")]
		public IReadOnlyList<PeriodoPorVencer> PorVencer { get; init; } = new List<PeriodoPorVencer>();

		[JsonPropertyName("vencidos")]
		[Description("Los períodos que YA se vencieron y siguen sin pagar. Vacío es una respuesta, y no "
			+ "una constancia de estar al día con la municipalidad.")]
		public IReadOnlyList<PeriodoVencido> Vencidos { get; init; } = new List<PeriodoVencido>();

		[JsonPropertyName("carpeta")]
		[Description("La carpeta de la persona, donde su app guarda el registro de su cuenta.")]
		public string Carpeta { get; init; } = "";

		[JsonPropertyName("detalle")]
		[Description("Lo que esta respuesta no dice, y lo que no hay que hacer con ella. Léalo antes de "
			+ "contestar.")]
		public string Detalle { get; init; } = "";
	}
}
