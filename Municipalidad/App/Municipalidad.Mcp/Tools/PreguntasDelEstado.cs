using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;
using ModelContextProtocol.Server;
using Municipalidad.App;

namespace Municipalidad.Mcp.Tools
{
	/// <summary>
	/// La pregunta del asistente sobre QUE TAN AL DIA ESTA ESTE REGISTRO: hasta donde leyo la app la
	/// narrativa de la municipalidad, que tiene la cuenta y que aporte de la persona falta. Solo lectura,
	/// y sin un solo parametro.
	/// </summary>
	[McpServerToolType]
	internal sealed class PreguntasDelEstado
	{
		private readonly LectorDeJournal lector;

		public PreguntasDelEstado(LectorDeJournal lector)
		{
			ArgumentNullException.ThrowIfNull(lector);

			this.lector = lector;
		}

		[McpServerTool(Name = "que_tan_al_dia_esta_esto", UseStructuredContent = true)]
		[Description("Contesta qué tan al día está este registro: hasta dónde leyó la app la narrativa de "
			+ "la municipalidad, qué tiene la cuenta, y qué aporte de la persona falta. Úsela ANTES de "
			+ "contestar cualquier otra pregunta cuando la persona espere ver algo nuevo, y para '¿esto "
			+ "está actualizado?', '¿por qué no aparece mi último pago?' y '¿qué le falta a mi app?'. "
			+ "ultimoEventoLeido ES LA ÚNICA MANERA DE SABER SI ENTRÓ ALGO NUEVO. Es un número que sólo "
			+ "sube. Pregúntelo, pídale a la persona que abra su app o la refresque, y vuelva a "
			+ "preguntarlo: si subió, entraron hechos nuevos y vale la pena repetir la pregunta original. "
			+ "Si sigue igual, la app no trajo nada y ninguna otra tool va a contestar distinto. Cero "
			+ "quiere decir que todavía no ha entrado ningún hecho de la municipalidad por el feed, que "
			+ "es un valor legal y no un error: los hechos se numeran desde uno. "
			+ "USTED NO PUEDE DISPARAR UN REFRESCO. Este servidor sólo lee la carpeta; quien trae hechos "
			+ "nuevos es la app de la persona cuando ella la abre. No prometa traerlos ni diga que va a "
			+ "revisar de nuevo en un rato. "
			+ "LA CUENTA ES HÍBRIDA Y ESTA TOOL DICE QUÉ MITAD FALTA. La municipalidad manda los cobros, "
			+ "los recibos y la mora; la persona aporta cuánto quiere apartar por mes y el valor de su "
			+ "finca si lo declaró. hayReservaAdoptada y las fincas con valorDeclarado en false son "
			+ "aportes que la persona todavía no hizo. Eso lo pone ella en su app: no lo invente y no se "
			+ "lo pida para usarlo como si estuviera registrado. "
			+ "fincasSinAvaluoPublicado ES LA RAZÓN DE QUE UNA RESPUESTA SOBRE EL IBI PUEDA QUEDAR A "
			+ "MEDIAS. El avalúo lo publica la MUNICIPALIDAD, no la persona: sin él, esta app puede "
			+ "decir que el avalúo cambió pero no de cuánto a cuánto. Dígalo así cuando salga, y no use "
			+ "el valor declarado en su lugar.")]
		public QueTanAlDiaEstaEsto QueTanAlDiaEstaEsto()
		{
			DateTime preguntadoEn = ElRelojQueLeeElAsistente.ElInstanteEnQueSePregunto();

			return lector.Leer(
				api => ElEstadoDeEsteRegistro(api, preguntadoEn),
				() => new QueTanAlDiaEstaEsto
				{
					HayDatos = false,
					PreguntadoEn = ElRelojQueLeeElAsistente.ElDiaDe(preguntadoEn),
					Carpeta = lector.Carpeta,
					Detalle = LoQueSeLeDiceAlAsistente.LaAppNuncaSeAbrio,
				});
		}

		private QueTanAlDiaEstaEsto ElEstadoDeEsteRegistro(MunicipalidadAPI api, DateTime preguntadoEn)
		{
			bool hayConsulta = api.HayAlgunaConsulta();

			List<FincaDeLaPersona> fincas = new List<FincaDeLaPersona>();

			foreach (string numero in api.NumerosDeLasFincas())
			{
				bool declarado = api.TieneValorDeclaradoLaFinca(numero, preguntadoEn);
				bool avaluada = api.TieneAvaluoLaFinca(numero, preguntadoEn);

				fincas.Add(new FincaDeLaPersona
				{
					Numero = numero,
					AvaluoPublicado = avaluada,
					Avaluo = avaluada ? api.AvaluoDeFincaAl(numero, preguntadoEn) : 0m,
					ValorDeclarado = declarado,
					Valor = declarado ? api.ValorDeFincaAl(numero, preguntadoEn) : 0m,
				});
			}

			int sinValor = 0;
			int sinAvaluo = 0;

			foreach (FincaDeLaPersona cada in fincas)
			{
				if (!cada.ValorDeclarado) sinValor++;
				if (!cada.AvaluoPublicado) sinAvaluo++;
			}

			return new QueTanAlDiaEstaEsto
			{
				HayDatos = true,
				PreguntadoEn = ElRelojQueLeeElAsistente.ElDiaDe(preguntadoEn),
				UltimoEventoLeido = api.UltimoEventoAplicado(),
				RubrosDelCatalogo = new List<string>(api.LosRubrosQueHay()),
				RubrosConHistoria = new List<string>(api.RubrosConHistoria()),
				HayAlgunaConsulta = hayConsulta,
				DiaDeLaUltimaConsulta =
					hayConsulta ? ElRelojQueLeeElAsistente.ElDiaDe(api.ConsultaMasReciente()) : "",
				HayReservaAdoptada = api.SePuedeVerElFaltanteDeReserva(),
				Fincas = fincas,
				FincasSinValorDeclarado = sinValor,
				FincasSinAvaluoPublicado = sinAvaluo,
				LaFotoDeLaCuenta = api.EstadoDeLaCuenta(),
				Carpeta = lector.Carpeta,
				Detalle = LoQueSeLeDiceAlAsistente.TodoLoQueValeLaPenaDecir(
					PregunteloDosVecesAlrededorDeUnRefresco,
					LoQueSeLeDiceAlAsistente.LoQueAportaLaPersonaNoLoAportaUsted,
					sinAvaluo > 0 ? SinElAvaluoElIbiQuedaAMedias : "",
					hayConsulta ? "" : SinConsultaNoSePuedeCotejar,
					LaFotoEsUnDocumentoDeLaApp,
					LoQueSeLeDiceAlAsistente.LoQueFaltaLoTraeUnRefresco),
			};
		}

		private const string PregunteloDosVecesAlrededorDeUnRefresco =
			"ultimoEventoLeido SÓLO SIRVE COMPARADO CONSIGO MISMO. Pregúntelo, pídale a la persona que "
			+ "abra o refresque su app, y vuelva a preguntarlo: si el número subió, entraron hechos "
			+ "nuevos. Un número solo no dice si el registro está al día, porque nadie sabe cuántos "
			+ "hechos ha publicado la municipalidad.";

		private const string SinElAvaluoElIbiQuedaAMedias =
			"HAY FINCAS SIN AVALÚO PUBLICADO. El avalúo lo manda la MUNICIPALIDAD y es el valor con que "
			+ "ella cobra: sin él, esta app puede decir que el cobro de bienes inmuebles subió por un "
			+ "avalúo nuevo, pero no de cuánto a cuánto pasó. Diga la mitad que sí hay y diga que la "
			+ "otra falta, y que llega sola cuando la municipalidad la publique. "
			+ "NO USE EL VALOR DECLARADO PARA TAPAR ESE HUECO: es un número de la persona, no el que le "
			+ "cobraron, y presentarlo como el avalúo afirmaría como hecho algo que nadie publicó.";

		private const string SinConsultaNoSePuedeCotejar =
			"NO HAY NINGUNA CONSULTA AL PORTAL REGISTRADA, así que que_quedo_sin_comprobante no va a "
			+ "poder cotejar nada. Eso no dice que no falte ningún comprobante: dice que esa pregunta "
			+ "todavía no se puede contestar.";

		private const string LaFotoEsUnDocumentoDeLaApp =
			"laFotoDeLaCuenta ES EL DOCUMENTO QUE LA APP MISMA COMPONE, y viene tal cual para las cuentas "
			+ "que ningún campo de arriba trae: cuántos cobros y cuántos pagos hay registrados. Cítelo si "
			+ "necesita una de ésas. Para todo lo demás use los campos de arriba, que vienen con su tipo: "
			+ "no saque de la foto un número que ya está aquí arriba.";
	}

	/// <summary>
	/// Una finca de la persona con sus DOS valuaciones: el avaluo que publico la municipalidad y el
	/// valor que declaro la persona. Ninguno contesta por el otro.
	/// </summary>
	internal sealed class FincaDeLaPersona
	{
		[JsonPropertyName("numero")]
		[Description("El número con que la municipalidad identifica esta finca.")]
		public string Numero { get; init; } = "";

		[JsonPropertyName("avaluoPublicado")]
		[Description("false cuando la MUNICIPALIDAD todavía no ha publicado un avalúo de esta finca. El "
			+ "avalúo es el valor con que ella cobra: llega por su feed, no lo pone la persona.")]
		public bool AvaluoPublicado { get; init; }

		[JsonPropertyName("avaluo")]
		[Description("El avalúo con que LA MUNICIPALIDAD cobra esta finca, vigente al día preguntado. "
			+ "Cero quiere decir que no ha publicado ninguno, no que la finca valga cero.")]
		public decimal Avaluo { get; init; }

		[JsonPropertyName("valorDeclarado")]
		[Description("false cuando la persona no ha declarado el valor de esta finca en su app. Es un "
			+ "aporte de ella, no un dato que la municipalidad mande: no lo invente.")]
		public bool ValorDeclarado { get; init; }

		[JsonPropertyName("valor")]
		[Description("El valor que LA PERSONA declaró, vigente al día preguntado. Cero quiere decir que "
			+ "no ha declarado ninguno, no que la finca valga cero. NO ES el avalúo con que le cobran: "
			+ "para eso está avaluo, y los dos pueden diferir sin que ninguno esté mal.")]
		public decimal Valor { get; init; }
	}

	/// <summary>Hasta donde leyo la app, que tiene la cuenta, y que aporte de la persona falta.</summary>
	internal sealed class QueTanAlDiaEstaEsto
	{
		[JsonPropertyName("hayDatos")]
		[Description("false cuando la app todavía no se ha abierto en esta computadora; entonces el "
			+ "resto viene vacío.")]
		public bool HayDatos { get; init; }

		[JsonPropertyName("preguntadoEn")]
		[Description("EL DÍA CON QUE SE CONTESTÓ, en yyyy-MM-dd.")]
		public string PreguntadoEn { get; init; } = "";

		[JsonPropertyName("ultimoEventoLeido")]
		[Description("HASTA DÓNDE LEYÓ LA APP la narrativa de la municipalidad. Sólo sube. Pregúntelo "
			+ "antes y después de que la persona refresque su app: si subió, entraron hechos nuevos. "
			+ "Cero es 'todavía no ha entrado ninguno', y es un valor legal.")]
		public long UltimoEventoLeido { get; init; }

		[JsonPropertyName("rubrosDelCatalogo")]
		[Description("Los rubros que esta app conoce, por su nombre en el registro. Es el catálogo, no "
			+ "un hecho de esta cuenta.")]
		public IReadOnlyList<string> RubrosDelCatalogo { get; init; } = new List<string>();

		[JsonPropertyName("rubrosConHistoria")]
		[Description("Los rubros de los que esta app ha visto pagar algo. Un rubro del catálogo que no "
			+ "esté aquí no es un rubro que la persona no pague: es uno cuyos pagos no han llegado.")]
		public IReadOnlyList<string> RubrosConHistoria { get; init; } = new List<string>();

		[JsonPropertyName("hayAlgunaConsulta")]
		[Description("false cuando no hay ninguna foto del portal registrada. Sin ella no se puede "
			+ "cotejar qué figura pagado sin comprobante.")]
		public bool HayAlgunaConsulta { get; init; }

		[JsonPropertyName("diaDeLaUltimaConsulta")]
		[Description("El día de la foto del portal más reciente, en yyyy-MM-dd. Vacío cuando no hay "
			+ "ninguna.")]
		public string DiaDeLaUltimaConsulta { get; init; } = "";

		[JsonPropertyName("hayReservaAdoptada")]
		[Description("false cuando la persona todavía no ha adoptado una reserva mensual en su app. Es "
			+ "un aporte de ella: no lo invente y no se lo pida para usarlo como si estuviera "
			+ "registrado.")]
		public bool HayReservaAdoptada { get; init; }

		[JsonPropertyName("fincas")]
		[Description("Las fincas de esta persona, cada una con el avalúo que publicó la municipalidad y "
			+ "con el valor que ella declaró.")]
		public IReadOnlyList<FincaDeLaPersona> Fincas { get; init; } = new List<FincaDeLaPersona>();

		[JsonPropertyName("fincasSinValorDeclarado")]
		[Description("Cuántas fincas están sin valor declarado POR LA PERSONA. Es un aporte suyo que "
			+ "todavía no hizo; no bloquea la explicación de un salto en el IBI, para eso está "
			+ "fincasSinAvaluoPublicado.")]
		public int FincasSinValorDeclarado { get; init; }

		[JsonPropertyName("fincasSinAvaluoPublicado")]
		[Description("Cuántas fincas están sin avalúo publicado POR LA MUNICIPALIDAD. Mientras haya "
			+ "alguna, una respuesta sobre por qué subió el IBI queda a medias, y hay que decirlo: "
			+ "ésta es la mitad que falta, no la declarada.")]
		public int FincasSinAvaluoPublicado { get; init; }

		[JsonPropertyName("laFotoDeLaCuenta")]
		[Description("EL DOCUMENTO QUE LA APP MISMA COMPONE, tal cual, para las cuentas que ningún campo "
			+ "de arriba trae: cuántos cobros y cuántos pagos hay. Cítelo sólo para ésas; lo demás ya "
			+ "está arriba y viene con su tipo.")]
		public string LaFotoDeLaCuenta { get; init; } = "";

		[JsonPropertyName("carpeta")]
		[Description("La carpeta de la persona, donde su app guarda el registro de su cuenta.")]
		public string Carpeta { get; init; } = "";

		[JsonPropertyName("detalle")]
		[Description("Lo que esta respuesta no dice, y lo que no hay que hacer con ella. Léalo antes de "
			+ "contestar.")]
		public string Detalle { get; init; } = "";
	}
}
