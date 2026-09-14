using System.Collections.Generic;

namespace Municipalidad.App.Frases
{
	/// <summary>
	/// LA REDACCION EN INGLES. Las claves son exactamente las del catalogo en espanol, y una prueba lo
	/// afirma: una clave que existe en uno y no en el otro es una frase que llega a medias sin que nada
	/// avise.
	/// <para>
	/// Los huecos llevan valores del registro de la municipalidad -- el nombre del rubro, la etiqueta
	/// del periodo, el numero de finca -- y esos NO se traducen en ningun idioma.
	/// </para>
	/// </summary>
	internal static class CatalogoEnIngles
	{
		internal static IReadOnlyDictionary<string, string> ElCatalogo()
		{
			return new Dictionary<string, string>
			{
				["variacion.marco"] =
					"In {rubro}, one period went from {antes} in {desde} to {ahora} in {hasta}: {cambio}. {explicacion}",
				["variacion.cambio.mas"] = "{porcentaje} more",
				["variacion.cambio.menos"] = "{porcentaje} less",
				["variacion.cambio.igual"] = "exactly the same",
				["variacion.explicacion.cambio-de-tarifa"] =
					"That is a jump, and this charge {ley}: what changed was the rate, because the "
					+ "municipality updated what it charges for the service.",
				["variacion.explicacion.cambio-de-avaluo"] =
					"That is a jump, and this charge {ley}: what changed was the value of the property. "
					+ "{avaluo}",
				["variacion.explicacion.cambio-menor"] =
					"It changed, but below the threshold it was asked with, so this app does not explain it "
					+ "as a jump.",
				["variacion.explicacion.sin-cambio"] =
					"It falls below the noise it was asked with, so this app does not count it as a change "
					+ "and there is nothing to explain.",

				["ley.por-unidad"] = "is charged per unit",
				["ley.sobre-base-imponible"] =
					"is charged on the taxable base, which is the value of the property",

				["avaluo.con-avaluo-publicado"] =
					"The assessment the municipality charged property {finca} on went from {antes} in "
					+ "{desde} to {ahora} in {hasta}.",
				["avaluo.sin-finca"] =
					"This account has no property attached, so there is none to read the assessment from: "
					+ "the municipality sends the property when it attaches one to your account, and its "
					+ "assessment when it sets one. This app says the jump, not what the assessment went "
					+ "from and to.",
				["avaluo.varias-fincas"] =
					"This account has {fincas} properties and the receipt line does not say which of them it "
					+ "is, because the portal consolidates by tax id. This app does not pick one for you: it "
					+ "says the jump, not what the assessment went from and to.",
				["avaluo.sin-avaluo-publicado"] =
					"The municipality has not published an assessment of property {finca} covering {desde} "
					+ "and {hasta}, so this app says the jump, not what the assessment went from and to. "
					+ "Whatever you declared is yours and is not the value you were charged on.",

				["serie.rubro-desconocido"] =
					"“{rubro}” is not one of the charges this app knows, so there is no series to "
					+ "show. It is not that information is missing: that charge does not exist in this "
					+ "account. The ones that do are {losQueHay}.",
				["serie.rubro-desconocido.sinNombre"] =
					"No charge was asked for, so there is no series to show. The charges this app knows are "
					+ "{losQueHay}.",
				["serie.sin-historia"] =
					"This app has not seen anything paid for {rubro}, so its series comes back empty. The "
					+ "charge does exist, and the series stops coming back empty as soon as a receipt for it "
					+ "arrives.",
				["serie.con-historia.una"] =
					"The series for {rubro} carries {cuantas} receipt line, from the oldest to the most "
					+ "recent.",
				["serie.con-historia.varias"] =
					"The series for {rubro} carries {cuantas} receipt lines, from the oldest to the most "
					+ "recent.",

				["cotejo.sin-consulta"] =
					"There is no enquiry of {dia} on record, so there is nothing to check what this app saw "
					+ "paid against. This does not say everything is in order: it says the question cannot "
					+ "be answered yet, and it will be as soon as an enquiry of that day is recorded.",
				["cotejo.nada-sin-comprobante"] =
					"The enquiry of {dia} was checked and turned up no period: everything the portal treats "
					+ "as paid, this app saw paid.",
				["cotejo.hay-periodos-sin-comprobante.uno"] =
					"The enquiry of {dia} turns up {cuantos} period the portal treats as paid and this app "
					+ "did not see paid.",
				["cotejo.hay-periodos-sin-comprobante.varios"] =
					"The enquiry of {dia} turns up {cuantos} periods the portal treats as paid and this app "
					+ "did not see paid.",

				["periodoSinComprobante"] =
					"The enquiry treats period {periodo} of {rubro} as paid and this app did not see that "
					+ "receipt. It does not say it is unpaid: it says the receipt never reached the app.",

				["atraso"] =
					"Period {periodo} of {rubro} fell due on {vencio} and is unpaid: {monto}, {dias} "
					+ "overdue as of {fecha}. At an annual rate of {tasa}, paying it that day is projected "
					+ "to cost {proyectado} more in interest. That is a projection by this app and not the "
					+ "interest the municipality charges: the real interest arrives on the receipt.",

				["vencimiento.marco"] =
					"Period {periodo} of {rubro} falls due on {vence}: {monto}. {apura}",
				["vencimiento.apura.urgente"] = "As of {fecha} it falls due that same day.",
				["vencimiento.apura.aviso"] =
					"As of {fecha} there are {dias} to go, within the {aviso} days of notice it was asked "
					+ "with: it is a reminder and not a required stop.",
				["vencimiento.apura.ninguna"] =
					"As of {fecha} there are {dias} to go, more than the {aviso} days of notice it was "
					+ "asked with, so it is not pressing yet.",

				["reservaPropuesta"] =
					"For {anio} this app projects {proyeccion} over the year, so it suggests setting aside "
					+ "{mensual} a month. It is a projection by this app over what you have already paid, "
					+ "not an amount the municipality has billed.",

				["faltante.marco"] =
					"The app suggests setting aside {propuesto} a month and you adopted {declarado}. "
					+ "{falta} {aviso} {vence}",
				["faltante.falta.cubre"] =
					"What you set aside covers what the app suggests, so nothing is missing.",
				["faltante.falta.falta"] =
					"You are short {monto} a month, {porcentaje} of what was suggested.",
				["faltante.aviso.avisa"] =
					"That is over the {umbral} it was asked with, so it is flagged.",
				["faltante.aviso.no-avisa"] =
					"That is not over the {umbral} it was asked with, so it is not flagged.",
				["faltante.vence.urgente"] =
					"And {monto} falls due this month, more than you set aside a month: that is what makes "
					+ "it urgent.",
				["faltante.vence.solo-vence"] =
					"{monto} falls due this month, more than you set aside a month. On its own that does not "
					+ "make it urgent: for that what you adopted would also have to fall below the threshold.",
				["faltante.vence.alcanza"] =
					"{monto} falls due this month, which what you set aside covers.",

				["etiqueta.cambio-de-tarifa"] = "rate change",
				["etiqueta.cambio-de-avaluo"] = "assessment change",
				["etiqueta.cambio-menor"] = "minor change",
				["etiqueta.sin-cambio"] = "no change",
				["etiqueta.urgente"] = "urgent",
				["etiqueta.aviso"] = "notice",
				["etiqueta.ninguna"] = "",
				["etiqueta.rubro-desconocido"] = "unknown charge",
				["etiqueta.sin-historia"] = "no history",
				["etiqueta.con-historia"] = "with history",
				["etiqueta.sin-consulta"] = "no enquiry",
				["etiqueta.nada-sin-comprobante"] = "nothing without a receipt",
				["etiqueta.hay-periodos-sin-comprobante"] = "periods without a receipt",
			};
		}
	}
}
