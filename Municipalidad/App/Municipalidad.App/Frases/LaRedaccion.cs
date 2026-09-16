using System;

namespace Municipalidad.App.Frases
{
	/// <summary>
	/// COMO SE ARMA CADA RESPUESTA a partir de lo que el dominio mando: su clave, sus claves de
	/// clausula y sus valores. Una funcion por respuesta, y el anidado -- que clausula va en que hueco --
	/// esta escrito una sola vez, aca.
	/// <para>
	/// Ningun valor se traduce al entrar en un hueco: el nombre del rubro, la etiqueta del periodo y el
	/// numero de finca salieron del registro de la municipalidad y se muestran tal como se asentaron.
	/// </para>
	/// </summary>
	public static class LaRedaccion
	{
		/// <summary>
		/// POR QUE SE PAGA MAS. Tres claves anidadas: el marco, la direccion del cambio, y la explicacion
		/// -- que a su vez cita la ley del rubro y, cuando el salto es de avaluo, el estado del avaluo.
		/// </summary>
		public static string Variacion(Frases frases,
			string rubro, string desde, string hasta, decimal antes, decimal ahora, decimal proporcion,
			string claveDelCambio, string claveDeLaExplicacion, string claveDeLaLey,
			string claveDelAvaluo, string finca, int fincas, decimal[] avaluos)
		{
			string cambio = frases.Diga("variacion.cambio." + claveDelCambio,
				("porcentaje", frases.Porcentaje(Math.Abs(proporcion))));

			string avaluo = ElAvaluo(frases, claveDelAvaluo, desde, hasta, finca, fincas, avaluos);

			string explicacion = frases.Diga("variacion.explicacion." + claveDeLaExplicacion,
				("ley", frases.Diga("ley." + claveDeLaLey)),
				("avaluo", avaluo));

			return frases.Diga("variacion.marco",
				("rubro", rubro),
				("antes", frases.Monto(antes)),
				("desde", desde),
				("ahora", frases.Monto(ahora)),
				("hasta", hasta),
				("cambio", cambio),
				("explicacion", explicacion));
		}

		/// <summary>
		/// LA CLAUSULA DEL AVALUO. Los dos montos llegan como cero filas o como dos: ese largo es la
		/// misma pregunta que el dominio contesta con SePuedeDecirElAvaluo, y leerlo de ahi evita que
		/// esta capa tenga su propia opinion sobre cuando hay avaluo.
		/// </summary>
		private static string ElAvaluo(Frases frases, string clave, string desde, string hasta,
			string finca, int fincas, decimal[] avaluos)
		{
			if (clave == "no-aplica") return "";

			return avaluos.Length == 2
				? frases.Diga("avaluo." + clave,
					("finca", finca),
					("antes", frases.Monto(avaluos[0])),
					("desde", desde),
					("ahora", frases.Monto(avaluos[1])),
					("hasta", hasta))
				: frases.Diga("avaluo." + clave,
					("finca", finca),
					("fincas", fincas.ToString()),
					("desde", desde),
					("hasta", hasta));
		}

		/// <summary>POR QUE LA SERIE DE UN RUBRO TRAE LO QUE TRAE. El plural lo decide la clave.</summary>
		public static string EstadoDeLaSerie(Frases frases, string clave, string rubro, int cuantasLineas,
			string losRubrosQueHay)
		{
			if (clave == "rubro-desconocido")
				return string.IsNullOrWhiteSpace(rubro)
					? frases.Diga("serie.rubro-desconocido.sinNombre", ("losQueHay", losRubrosQueHay))
					: frases.Diga("serie.rubro-desconocido", ("rubro", rubro), ("losQueHay", losRubrosQueHay));

			if (clave == "con-historia")
				return frases.Diga(cuantasLineas == 1 ? "serie.con-historia.una" : "serie.con-historia.varias",
					("rubro", rubro), ("cuantas", cuantasLineas.ToString()));

			return frases.Diga("serie." + clave, ("rubro", rubro));
		}

		/// <summary>POR QUE EL COTEJO DE UN DIA TRAE LO QUE TRAE.</summary>
		public static string Cotejo(Frases frases, string clave, DateTime dia, int cuantosPeriodos)
		{
			if (clave == "hay-periodos-sin-comprobante")
				return frases.Diga(
					cuantosPeriodos == 1
						? "cotejo.hay-periodos-sin-comprobante.uno"
						: "cotejo.hay-periodos-sin-comprobante.varios",
					("dia", frases.Dia(dia)), ("cuantos", cuantosPeriodos.ToString()));

			return frases.Diga("cotejo." + clave, ("dia", frases.Dia(dia)));
		}

		/// <summary>UN PERIODO QUE EL PORTAL DA POR PAGADO Y AQUI NO TIENE RECIBO.</summary>
		public static string PeriodoSinComprobante(Frases frases, string periodo, string rubro)
		{
			return frases.Diga("periodoSinComprobante", ("periodo", periodo), ("rubro", rubro));
		}

		/// <summary>ALGO QUE YA VENCIO, con su proyeccion de mora dicha COMO proyeccion.</summary>
		public static string Atraso(Frases frases, string periodo, string rubro, string vencio,
			decimal monto, int diasDeAtraso, DateTime fecha, decimal tasaAnual, decimal proyectado)
		{
			return frases.Diga("atraso",
				("periodo", periodo),
				("rubro", rubro),
				("vencio", vencio),
				("monto", frases.Monto(monto)),
				("dias", frases.Dias(diasDeAtraso)),
				("fecha", frases.Dia(fecha)),
				("tasa", frases.Porcentaje(tasaAnual)),
				("proyectado", frases.Monto(proyectado)));
		}

		/// <summary>ALGO QUE TODAVIA NO VENCE. La clausula de urgencia la nombra el dominio.</summary>
		public static string Vencimiento(Frases frases, string periodo, string rubro, string vence,
			decimal monto, string claveDeUrgencia, int faltan, DateTime fecha, int diasDeAviso)
		{
			string apura = frases.Diga("vencimiento.apura." + claveDeUrgencia,
				("fecha", frases.Dia(fecha)),
				("dias", frases.Dias(faltan)),
				("aviso", diasDeAviso.ToString()));

			return frases.Diga("vencimiento.marco",
				("periodo", periodo),
				("rubro", rubro),
				("vence", vence),
				("monto", frases.Monto(monto)),
				("apura", apura));
		}

		/// <summary>LO QUE LA APP PROPONE APARTAR, dicho COMO propuesta de la app.</summary>
		public static string ReservaPropuesta(Frases frases, int anio, decimal proyeccion, decimal mensual)
		{
			return frases.Diga("reservaPropuesta",
				("anio", anio.ToString()),
				("proyeccion", frases.Monto(proyeccion)),
				("mensual", frases.Monto(mensual)));
		}

		/// <summary>
		/// LO PROPUESTO CONTRA LO ADOPTADO. Tres clausulas con su propia clave, porque las tres cambian
		/// por su cuenta y una sola frase con matices adentro no podria decir cual de las tres cambio.
		/// </summary>
		public static string Faltante(Frases frases, decimal propuesto, decimal declarado,
			string claveDeLoQueFalta, decimal faltante, decimal proporcion,
			string claveDelAviso, decimal umbral,
			string claveDeLoQueVence, decimal venceEsteMes)
		{
			string falta = frases.Diga("faltante.falta." + claveDeLoQueFalta,
				("monto", frases.Monto(faltante)),
				("porcentaje", frases.Porcentaje(proporcion)));

			string aviso = frases.Diga("faltante.aviso." + claveDelAviso,
				("umbral", frases.Porcentaje(umbral)));

			string vence = frases.Diga("faltante.vence." + claveDeLoQueVence,
				("monto", frases.Monto(venceEsteMes)));

			return frases.Diga("faltante.marco",
				("propuesto", frases.Monto(propuesto)),
				("declarado", frases.Monto(declarado)),
				("falta", falta),
				("aviso", aviso),
				("vence", vence));
		}

		/// <summary>
		/// COMO SE DICE EL NOMBRE DE UN ESTADO. El codigo es identidad y cruza el wire sin tocarse; esto
		/// es lo que una persona lee. Un codigo sin dicho se dice con su propio codigo, que es lo que la
		/// pantalla mostraba antes de que existiera esta tabla.
		/// </summary>
		public static string Etiqueta(Frases frases, string codigo)
		{
			if (string.IsNullOrWhiteSpace(codigo)) return "";
			string dicho = frases.Diga("etiqueta." + codigo);
			return dicho.StartsWith("{", StringComparison.Ordinal) ? codigo : dicho;
		}
	}
}
