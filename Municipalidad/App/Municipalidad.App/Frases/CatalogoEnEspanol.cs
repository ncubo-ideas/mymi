using System.Collections.Generic;

namespace Municipalidad.App.Frases
{
	/// <summary>
	/// LA REDACCION EN ESPANOL, que es la que el dominio traia escrita adentro hasta que la frase se
	/// partio en clave y valores. Palabra por palabra la misma: mover la redaccion no es reescribirla.
	/// <para>
	/// Cada clave es una FRASE ENTERA y no un pedazo. El espanol concuerda en genero y el ingles no, asi
	/// que un catalogo que pegara fragmentos daria concordancias rotas que ninguna prueba ve. Los huecos
	/// llevan valores del registro, que cruzan sin traducirse.
	/// </para>
	/// </summary>
	internal static class CatalogoEnEspanol
	{
		internal static IReadOnlyDictionary<string, string> ElCatalogo()
		{
			return new Dictionary<string, string>
			{
				["variacion.marco"] =
					"En {rubro}, un período pasó de {antes} en {desde} a {ahora} en {hasta}: {cambio}. {explicacion}",
				["variacion.cambio.mas"] = "un {porcentaje} más",
				["variacion.cambio.menos"] = "un {porcentaje} menos",
				["variacion.cambio.igual"] = "exactamente lo mismo",
				["variacion.explicacion.cambio-de-tarifa"] =
					"Es un salto, y este rubro {ley}: lo que cambió fue la tarifa, porque la municipalidad "
					+ "actualizó lo que cobra por el servicio.",
				["variacion.explicacion.cambio-de-avaluo"] =
					"Es un salto, y este rubro {ley}: lo que cambió fue el valor de la finca. {avaluo}",
				["variacion.explicacion.cambio-menor"] =
					"Cambió, pero por debajo del umbral con que se preguntó, así que esta app no lo explica "
					+ "como un salto.",
				["variacion.explicacion.sin-cambio"] =
					"Queda por debajo del ruido con que se preguntó, así que esta app no lo cuenta como un "
					+ "cambio y no hay nada que explicar.",

				["ley.por-unidad"] = "se cobra por unidad",
				["ley.sobre-base-imponible"] =
					"se cobra sobre la base imponible, que es el valor de la finca",

				["avaluo.con-avaluo-publicado"] =
					"El avalúo con que la municipalidad cobró la finca {finca} pasó de {antes} en {desde} a "
					+ "{ahora} en {hasta}.",
				["avaluo.sin-finca"] =
					"Esta cuenta no tiene ninguna finca incorporada, así que no hay sobre cuál leer el "
					+ "avalúo: la municipalidad manda la finca cuando la incorpora a tu cuenta, y su avalúo "
					+ "cuando lo fija. Esta app dice el salto y no de cuánto a cuánto pasó el avalúo.",
				["avaluo.varias-fincas"] =
					"Esta cuenta tiene {fincas} fincas y la línea del recibo no dice de cuál de ellas es, "
					+ "porque el portal consolida por cédula. Esta app no elige una por vos: dice el salto y "
					+ "no de cuánto a cuánto pasó el avalúo.",
				["avaluo.sin-avaluo-publicado"] =
					"La municipalidad no publicó un avalúo de la finca {finca} que cubra {desde} y {hasta}, "
					+ "así que esta app dice el salto y no de cuánto a cuánto pasó el avalúo. Lo que vos "
					+ "hayás declarado es tuyo y no es el valor con que te cobraron.",

				["serie.rubro-desconocido"] =
					"«{rubro}» no es ninguno de los rubros que esta app conoce, así que no hay serie que "
					+ "mostrar. No es que falte información: ese rubro no existe en esta cuenta. Los que hay "
					+ "son {losQueHay}.",
				["serie.rubro-desconocido.sinNombre"] =
					"No se pidió ningún rubro, así que no hay serie que mostrar. Los rubros que esta app "
					+ "conoce son {losQueHay}.",
				["serie.sin-historia"] =
					"Esta app no vio pagar nada de {rubro}, así que su serie viene vacía. El rubro sí "
					+ "existe, y la serie deja de venir vacía en cuanto entre un recibo suyo.",
				["serie.con-historia.una"] =
					"La serie de {rubro} trae {cuantas} línea de comprobante, de la más vieja a la más "
					+ "reciente.",
				["serie.con-historia.varias"] =
					"La serie de {rubro} trae {cuantas} líneas de comprobante, de la más vieja a la más "
					+ "reciente.",

				["cotejo.sin-consulta"] =
					"No hay ninguna consulta del {dia} registrada, así que no hay con qué cotejar lo que "
					+ "esta app vio pagar. Esto no dice que todo esté en orden: dice que la pregunta todavía "
					+ "no se puede contestar, y se va a poder en cuanto se registre una consulta de ese día.",
				["cotejo.nada-sin-comprobante"] =
					"La consulta del {dia} se cotejó y no destapó ningún período: todo lo que el portal da "
					+ "por pagado, esta app lo vio pagar.",
				["cotejo.hay-periodos-sin-comprobante.uno"] =
					"La consulta del {dia} destapa {cuantos} período que el portal da por pagados y esta app "
					+ "no vio pagar.",
				["cotejo.hay-periodos-sin-comprobante.varios"] =
					"La consulta del {dia} destapa {cuantos} períodos que el portal da por pagados y esta "
					+ "app no vio pagar.",

				["periodoSinComprobante"] =
					"La consulta da por pagado el período {periodo} de {rubro} y esta app no vio ese recibo. "
					+ "No dice que esté sin pagar: dice que el comprobante no le llegó a la app.",

				["atraso"] =
					"El período {periodo} de {rubro} venció el {vencio} y no está pagado: {monto}, con "
					+ "{dias} de atraso al {fecha}. A una tasa anual de {tasa}, pagarlo ese día proyecta "
					+ "costar {proyectado} más de mora. Es una proyección de esta app y no la mora que la "
					+ "municipalidad cobra: la mora real llega en el recibo.",

				["vencimiento.marco"] =
					"El período {periodo} de {rubro} vence el {vence}: {monto}. {apura}",
				["vencimiento.apura.urgente"] = "Al {fecha} vence ese mismo día.",
				["vencimiento.apura.aviso"] =
					"Al {fecha} faltan {dias}, dentro de los {aviso} de aviso con que se preguntó: es un "
					+ "recordatorio y no una parada obligatoria.",
				["vencimiento.apura.ninguna"] =
					"Al {fecha} faltan {dias}, más de los {aviso} de aviso con que se preguntó, así que "
					+ "todavía no apura.",

				["reservaPropuesta"] =
					"Para {anio} esta app proyecta {proyeccion} en el año, así que propone apartar "
					+ "{mensual} por mes. Es una proyección de esta app sobre lo que ya pagaste, no un monto "
					+ "que la municipalidad haya puesto al cobro.",

				["faltante.marco"] =
					"La app propone apartar {propuesto} por mes y vos adoptaste {declarado}. {falta} "
					+ "{aviso} {vence}",
				["faltante.falta.cubre"] =
					"Lo que apartás cubre lo que la app propone, así que no te falta nada.",
				["faltante.falta.falta"] =
					"Te faltan {monto} por mes, un {porcentaje} de lo propuesto.",
				["faltante.aviso.avisa"] =
					"Pasa del {umbral} con que se preguntó, así que se avisa.",
				["faltante.aviso.no-avisa"] =
					"No pasa del {umbral} con que se preguntó, así que no se avisa.",
				["faltante.vence.urgente"] =
					"Y este mes vencen {monto}, más de lo que apartás por mes: eso es lo que lo vuelve "
					+ "urgente.",
				["faltante.vence.solo-vence"] =
					"Este mes vencen {monto}, más de lo que apartás por mes. Por sí solo eso no lo vuelve "
					+ "urgente: para eso haría falta además que lo adoptado quedara por debajo del umbral.",
				["faltante.vence.alcanza"] =
					"Este mes vencen {monto}, que lo que apartás alcanza a cubrir.",

				["etiqueta.cambio-de-tarifa"] = "cambio de tarifa",
				["etiqueta.cambio-de-avaluo"] = "cambio de avalúo",
				["etiqueta.cambio-menor"] = "cambio menor",
				["etiqueta.sin-cambio"] = "sin cambio",
				["etiqueta.urgente"] = "urgente",
				["etiqueta.aviso"] = "aviso",
				["etiqueta.ninguna"] = "",
				["etiqueta.rubro-desconocido"] = "rubro desconocido",
				["etiqueta.sin-historia"] = "sin historia",
				["etiqueta.con-historia"] = "con historia",
				["etiqueta.sin-consulta"] = "sin consulta",
				["etiqueta.nada-sin-comprobante"] = "nada sin comprobante",
				["etiqueta.hay-periodos-sin-comprobante"] = "hay períodos sin comprobante",
			};
		}
	}
}
