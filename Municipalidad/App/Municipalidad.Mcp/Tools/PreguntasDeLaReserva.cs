using System;
using System.ComponentModel;
using System.Text.Json.Serialization;
using ModelContextProtocol.Server;
using Municipalidad.App;

namespace Municipalidad.Mcp.Tools
{
	/// <summary>
	/// La pregunta del asistente sobre CUANTO APARTAR POR MES: lo que la app propone y lo que la persona
	/// adopto, cada mitad legible sola. Solo lectura, y sin un solo parametro.
	/// </summary>
	[McpServerToolType]
	internal sealed class PreguntasDeLaReserva
	{
		private readonly LectorDeJournal lector;

		public PreguntasDeLaReserva(LectorDeJournal lector)
		{
			ArgumentNullException.ThrowIfNull(lector);

			this.lector = lector;
		}

		[McpServerTool(Name = "cuanto_apartar", UseStructuredContent = true)]
		[Description("Contesta cuánto debería apartar esta persona por mes para sus pagos municipales: "
			+ "lo que ESTA APP PROPONE a partir de lo que ya pagó, y lo que LA PERSONA ADOPTÓ, con lo que "
			+ "le falta a lo segundo para cubrir lo primero. Úsela para '¿cuánto debo apartar al mes?', "
			+ "'¿me alcanza lo que estoy guardando?' y '¿cuánto voy a pagar este año?'. "
			+ "SON DOS COSAS DE DOS DUEÑOS Y NO SE PUEDEN CONFUNDIR. propuesto lo calculó esta app "
			+ "dividiendo entre doce lo que proyecta para el año; adoptado lo puso la persona en su app y "
			+ "es SUYO. Que no coincidan no es un error: la persona puede haber decidido apartar más o "
			+ "menos a propósito. Nunca presente lo propuesto como una obligación ni como algo que la "
			+ "municipalidad haya pedido. "
			+ "LAS DOS MITADES SE LEEN POR SEPARADO, A PROPÓSITO. sePuedeProponer puede ser true mientras "
			+ "hayReservaAdoptada es false: entonces hay propuesta y no hay con qué compararla, y eso es "
			+ "una respuesta completa. Tener una no es condición para leer la otra. "
			+ "SIN UN PAGO REGISTRADO NO HAY PROYECCIÓN Y NO SE INVENTA. sePuedeProponer en false quiere "
			+ "decir que esta app no ha visto ni un comprobante, así que no hay de dónde proyectar. NO "
			+ "estime un monto a partir de otra tool y no divida nada entre doce usted. "
			+ "LA PERSONA ADOPTA SU RESERVA EN SU APP, NO AQUÍ. Si no ha adoptado ninguna, dígaselo y "
			+ "dígale que eso se pone en su app. Este servidor no puede escribir, y no le pida a la "
			+ "persona que le dicte el monto para que usted lo use como si estuviera registrado. "
			+ "URGENTE QUIERE DECIR DOS COSAS A LA VEZ. El faltante sólo se vuelve urgente si además lo "
			+ "que vence este mes pasa lo que la persona aparta por mes. Un faltante por sí solo es un "
			+ "aviso; dice lo explica entero. "
			+ "ESTA TOOL NO RECIBE NINGÚN PARÁMETRO. El año y el día con que se contestó salen en la "
			+ "respuesta.")]
		public LaReservaDeEstaPersona CuantoApartar()
		{
			DateTime preguntadoEn = ElRelojQueLeeElAsistente.ElInstanteEnQueSePregunto();

			return lector.Leer(
				api => LaReservaAlDiaDeHoy(api, preguntadoEn),
				() => new LaReservaDeEstaPersona
				{
					HayDatos = false,
					PreguntadoEn = ElRelojQueLeeElAsistente.ElDiaDe(preguntadoEn),
					Anio = preguntadoEn.Year,
					Carpeta = lector.Carpeta,
					Detalle = LoQueSeLeDiceAlAsistente.LaAppNuncaSeAbrio,
				});
		}

		private LaReservaDeEstaPersona LaReservaAlDiaDeHoy(MunicipalidadAPI api, DateTime preguntadoEn)
		{
			bool sePuedeProponer = api.SePuedeProyectar();
			bool sePuedeVerElFaltante = api.SePuedeVerElFaltanteDeReserva();

			decimal proyeccionDelAnio = 0m;
			decimal propuestoPorMes = 0m;
			string diceLaPropuesta = SinUnPagoNoHayDeDondeProyectar;

			if (sePuedeProponer)
			{
				RegistroDeLaReservaPropuesta propuesta = api.ReservaPropuestaPara(preguntadoEn.Year);

				proyeccionDelAnio = propuesta.Proyeccion;
				propuestoPorMes = propuesta.PropuestoPorMes;
				diceLaPropuesta = propuesta.Dice;
			}

			decimal adoptado = 0m;
			decimal loQueFalta = 0m;
			decimal proporcionDelFaltante = 0m;
			decimal venceEsteMes = 0m;
			string urgencia = "";
			string diceElFaltante =
				sePuedeProponer ? LaPersonaNoHaAdoptadoNinguna : SinProyeccionNoHayContraQueMedir;

			if (sePuedeVerElFaltante)
			{
				RegistroDelFaltanteDeReserva faltante =
					api.FaltanteDeReservaAl(preguntadoEn, LosJuiciosSinFirmar.UmbralDeReserva);

				adoptado = faltante.Declarado;
				loQueFalta = faltante.Faltante;
				proporcionDelFaltante = faltante.Proporcion;
				venceEsteMes = faltante.VenceEsteMes;
				urgencia = faltante.Urgencia;
				diceElFaltante = faltante.Dice;
			}

			return new LaReservaDeEstaPersona
			{
				HayDatos = true,
				PreguntadoEn = ElRelojQueLeeElAsistente.ElDiaDe(preguntadoEn),
				Anio = preguntadoEn.Year,
				UmbralConQueSeAvisa = LosJuiciosSinFirmar.UmbralDeReserva,

				SePuedeProponer = sePuedeProponer,
				ProyeccionDelAnio = proyeccionDelAnio,
				Propuesto = propuestoPorMes,
				DiceLaPropuesta = diceLaPropuesta,

				HayReservaAdoptada = sePuedeVerElFaltante,
				Adoptado = adoptado,
				Faltante = loQueFalta,
				ProporcionDelFaltante = proporcionDelFaltante,
				VenceEsteMes = venceEsteMes,
				Urgencia = urgencia,
				DiceElFaltante = diceElFaltante,

				Carpeta = lector.Carpeta,
				Detalle = LoQueSeLeDiceAlAsistente.TodoLoQueValeLaPenaDecir(
					LoQueSeLeDiceAlAsistente.LaFraseEsLaRespuesta,
					LasDosMitadesTienenDuenosDistintos,
					sePuedeVerElFaltante ? LoQueSeLeDiceAlAsistente.EsteSiEstaFirmado : "",
					sePuedeProponer ? "" : SinUnPagoNoHayDeDondeProyectar,
					sePuedeProponer && !sePuedeVerElFaltante ? LaPersonaNoHaAdoptadoNinguna : "",
					LoQueSeLeDiceAlAsistente.LoQueAportaLaPersonaNoLoAportaUsted,
					LoQueSeLeDiceAlAsistente.LoQueFaltaLoTraeUnRefresco),
			};
		}

		private const string LasDosMitadesTienenDuenosDistintos =
			"LO PROPUESTO ES DE LA APP Y LO ADOPTADO ES DE LA PERSONA, y cada mitad se lee sola. No "
			+ "presente lo propuesto como un monto que haya que apartar ni como algo que la "
			+ "municipalidad pidió: es una proyección de esta app sobre lo que la persona ya pagó. Que "
			+ "la persona haya adoptado otro número puede ser una decisión suya y no un descuido.";

		private const string SinUnPagoNoHayDeDondeProyectar =
			"Esta app no ha visto ni un comprobante de pago, así que no hay de dónde proyectar y no se "
			+ "inventa un monto. Eso no quiere decir que la persona no pague: quiere decir que sus pagos "
			+ "todavía no han llegado a esta app.";

		private const string LaPersonaNoHaAdoptadoNinguna =
			"LA PERSONA NO HA ADOPTADO NINGUNA RESERVA todavía, así que no hay contra qué comparar la "
			+ "propuesta y no falta nada por culpa de esta app. Eso lo pone ella en su app; este servidor "
			+ "no puede escribir. Si la persona le dice de viva voz cuánto aparta, no lo tome como un "
			+ "dato registrado ni lo reste de la propuesta.";

		private const string SinProyeccionNoHayContraQueMedir =
			"Sin proyección no hay contra qué medir lo adoptado, así que esta mitad no se puede "
			+ "contestar. No es cero: es que falta la otra mitad.";
	}

	/// <summary>Lo que la app propone apartar y lo que la persona adopto, cada mitad legible sola.</summary>
	internal sealed class LaReservaDeEstaPersona
	{
		[JsonPropertyName("hayDatos")]
		[Description("false cuando la app todavía no se ha abierto en esta computadora; entonces el "
			+ "resto viene vacío.")]
		public bool HayDatos { get; init; }

		[JsonPropertyName("preguntadoEn")]
		[Description("EL DÍA CON QUE SE CONTESTÓ, en yyyy-MM-dd. Lo que vence este mes es respecto de "
			+ "este día.")]
		public string PreguntadoEn { get; init; } = "";

		[JsonPropertyName("anio")]
		[Description("El año para el que se propone apartar, tomado del día de hoy. Es la etiqueta de la "
			+ "propuesta: la proyección sale de lo que la persona ya pagó, no del calendario.")]
		public int Anio { get; init; }

		[JsonPropertyName("umbralConQueSeAvisa")]
		[Description("Cuánto puede quedar lo adoptado por debajo de lo propuesto antes de avisar: 0.10 "
			+ "son diez por ciento. Este umbral SÍ lo firmó la persona, el 2026-09-03.")]
		public decimal UmbralConQueSeAvisa { get; init; }

		[JsonPropertyName("sePuedeProponer")]
		[Description("false cuando esta app no ha visto ni un comprobante y por lo tanto no hay de dónde "
			+ "proyectar. Entonces los montos propuestos vienen en cero y NO se pueden usar.")]
		public bool SePuedeProponer { get; init; }

		[JsonPropertyName("proyeccionDelAnio")]
		[Description("Lo que esta app proyecta que la persona va a pagar en el año entero. Es una "
			+ "proyección sobre lo ya pagado, no un cobro.")]
		public decimal ProyeccionDelAnio { get; init; }

		[JsonPropertyName("propuesto")]
		[Description("LO QUE ESTA APP PROPONE APARTAR POR MES: la proyección del año entre doce. Es de "
			+ "la app, no de la municipalidad y no de la persona.")]
		public decimal Propuesto { get; init; }

		[JsonPropertyName("diceLaPropuesta")]
		[Description("LA RESPUESTA de esta mitad, en una frase que compuso el dominio, y ya dice que es "
			+ "una propuesta de la app. Dígala.")]
		public string DiceLaPropuesta { get; init; } = "";

		[JsonPropertyName("hayReservaAdoptada")]
		[Description("false cuando la persona todavía no ha adoptado ninguna reserva en su app. "
			+ "Entonces la mitad de abajo viene en cero y NO se puede usar: eso lo pone ella, no usted.")]
		public bool HayReservaAdoptada { get; init; }

		[JsonPropertyName("adoptado")]
		[Description("LO QUE LA PERSONA DECIDIÓ APARTAR POR MES. Es suyo. Cero aquí quiere decir que no "
			+ "ha adoptado ninguna, no que aparte cero.")]
		public decimal Adoptado { get; init; }

		[JsonPropertyName("faltante")]
		[Description("Lo que le falta a lo adoptado para cubrir lo propuesto. Cero quiere decir que lo "
			+ "cubre.")]
		public decimal Faltante { get; init; }

		[JsonPropertyName("proporcionDelFaltante")]
		[Description("El faltante como proporción de lo propuesto: 0.1 son diez por ciento. Sale ya "
			+ "calculado.")]
		public decimal ProporcionDelFaltante { get; init; }

		[JsonPropertyName("venceEsteMes")]
		[Description("Lo que vence en el mes del día preguntado. Es lo único que puede volver urgente un "
			+ "faltante.")]
		public decimal VenceEsteMes { get; init; }

		[JsonPropertyName("urgencia")]
		[Description("'ninguna', 'aviso' o 'urgente'. Urgente exige DOS cosas a la vez: que el faltante "
			+ "pase el umbral y que lo que vence este mes pase lo que la persona aparta.")]
		public string Urgencia { get; init; } = "";

		[JsonPropertyName("diceElFaltante")]
		[Description("LA RESPUESTA de esta mitad, en una frase que compuso el dominio y que nombra las "
			+ "dos mitades y de quién es cada una. Dígala entera.")]
		public string DiceElFaltante { get; init; } = "";

		[JsonPropertyName("carpeta")]
		[Description("La carpeta de la persona, donde su app guarda el registro de su cuenta.")]
		public string Carpeta { get; init; } = "";

		[JsonPropertyName("detalle")]
		[Description("Lo que esta respuesta no dice, y lo que no hay que hacer con ella. Léalo antes de "
			+ "contestar.")]
		public string Detalle { get; init; } = "";
	}
}
