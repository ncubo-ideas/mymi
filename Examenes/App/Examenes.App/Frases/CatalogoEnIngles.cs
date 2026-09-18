using System.Collections.Generic;

namespace Examenes.App.Frases
{
	/// <summary>
	/// LA REDACCION EN EL OTRO IDIOMA QUE ESTA APP HABLA. Es una traduccion de las frases del modulo,
	/// no del registro: los numeros y los simbolos de unidad entran por los huecos tal como los asento
	/// el laboratorio.
	/// <para>
	/// LAS CLAVES SON LAS MISMAS que en el catalogo del idioma del registro, y eso no es estilo: una
	/// clave que esta en uno y no en el otro es una frase que llega a medias -- con un {hueco} crudo a
	/// la vista -- y ninguna otra comprobacion la ve.
	/// </para>
	/// <para>
	/// EL TONO ES EL MISMO: una frase dice lo que la serie muestra y se calla. No hay diagnosticos
	/// aca, y todo consejo lleva su plazo porque lo trae la accion.
	/// </para>
	/// </summary>
	internal static class CatalogoEnIngles
	{
		internal static IReadOnlyDictionary<string, string> ElCatalogo()
		{
			return new Dictionary<string, string>
			{
				// ------------------------------------------------- como se llama cada analito

				["nombre.antigeno-prostatico"] = "Prostate-specific antigen",
				["nombre.colesterol-total"] = "Total cholesterol",
				["nombre.colesterol-ldl"] = "LDL cholesterol",
				["nombre.colesterol-hdl"] = "HDL cholesterol",
				["nombre.hemoglobina"] = "Hemoglobin",
				["nombre.trigliceridos"] = "Triglycerides",
				["nombre.glucosa-en-ayunas"] = "Fasting glucose",
				["nombre.nitrogeno-ureico"] = "Blood urea nitrogen",
				["nombre.sodio"] = "Sodium",
				["nombre.potasio"] = "Potassium",
				["nombre.cloro"] = "Chloride",
				["nombre.proteinas-totales"] = "Total protein",
				["nombre.color-de-la-orina"] = "Urine color",
				["nombre.nitritos"] = "Nitrites",
				["nombre.sangre-en-orina"] = "Blood in urine",
				["nombre.proteina-en-orina"] = "Protein in urine",
				["nombre.cetonas"] = "Ketones",
				["nombre.glucosa-en-orina"] = "Glucose in urine",
				["nombre.color-de-las-heces"] = "Stool color",
				["nombre.sangre-en-heces"] = "Blood in stool",
				["nombre.examen-de-orina"] = "Urinalysis",
				["nombre.examen-de-heces"] = "Stool test",
				["nombre.colesterol-detallado"] = "HDL/LDL cholesterol",

				// El ingles no lleva articulo delante de estos nombres, asi que las dos tablas
				// que en espanol resuelven el genero aca solo cambian la mayuscula inicial.

				["elNombre.antigeno-prostatico"] = "prostate-specific antigen",
				["elNombre.colesterol-total"] = "total cholesterol",
				["elNombre.colesterol-ldl"] = "LDL cholesterol",
				["elNombre.colesterol-hdl"] = "HDL cholesterol",
				["elNombre.hemoglobina"] = "hemoglobin",
				["elNombre.trigliceridos"] = "triglycerides",
				["elNombre.glucosa-en-ayunas"] = "fasting glucose",
				["elNombre.nitrogeno-ureico"] = "blood urea nitrogen",
				["elNombre.sodio"] = "sodium",
				["elNombre.potasio"] = "potassium",
				["elNombre.cloro"] = "chloride",
				["elNombre.proteinas-totales"] = "total protein",
				["elNombre.color-de-la-orina"] = "urine color",
				["elNombre.nitritos"] = "nitrites",
				["elNombre.sangre-en-orina"] = "blood in urine",
				["elNombre.proteina-en-orina"] = "protein in urine",
				["elNombre.cetonas"] = "ketones",
				["elNombre.glucosa-en-orina"] = "glucose in urine",
				["elNombre.color-de-las-heces"] = "stool color",
				["elNombre.sangre-en-heces"] = "blood in stool",

				["tuUltimo.antigeno-prostatico"] = "your last prostate-specific antigen",
				["tuUltimo.colesterol-total"] = "your last total cholesterol",
				["tuUltimo.colesterol-ldl"] = "your last LDL cholesterol",
				["tuUltimo.colesterol-hdl"] = "your last HDL cholesterol",
				["tuUltimo.hemoglobina"] = "your last hemoglobin",
				["tuUltimo.trigliceridos"] = "your last triglycerides",
				["tuUltimo.glucosa-en-ayunas"] = "your last fasting glucose",
				["tuUltimo.nitrogeno-ureico"] = "your last blood urea nitrogen",
				["tuUltimo.sodio"] = "your last sodium",
				["tuUltimo.potasio"] = "your last potassium",
				["tuUltimo.cloro"] = "your last chloride",
				["tuUltimo.proteinas-totales"] = "your last total protein",
				["tuUltimo.color-de-la-orina"] = "your last urine color",
				["tuUltimo.nitritos"] = "your last nitrites",
				["tuUltimo.sangre-en-orina"] = "your last blood-in-urine test",
				["tuUltimo.proteina-en-orina"] = "your last protein in urine",
				["tuUltimo.cetonas"] = "your last ketones",
				["tuUltimo.glucosa-en-orina"] = "your last glucose in urine",
				["tuUltimo.color-de-las-heces"] = "your last stool color",
				["tuUltimo.sangre-en-heces"] = "your last blood-in-stool test",

				// ------------------------------------------------- los conjuntos cerrados

				["severidad.informativo"] = "for information",
				["severidad.vigilar"] = "keep an eye on it",
				["severidad.consulte-pronto"] = "see a doctor soon",
				["severidad.urgente"] = "urgent",

				["veredicto.indeterminado"] = "undetermined",
				["veredicto.observado"] = "observed",
				["veredicto.estable"] = "steady",
				["veredicto.mejorando"] = "improving",
				["veredicto.empeorando"] = "worsening",
				["veredicto.probable-error-de-medicion"] = "likely measurement error",
				["veredicto.pico-no-confirmado"] = "spike not confirmed",
				["veredicto.hallazgo-no-confirmado"] = "finding not confirmed",
				["veredicto.no-aplica"] = "not applicable",
				["veredicto.requiere-sexo"] = "needs your sex",

				["tendencia.indeterminada"] = "undetermined",
				["tendencia.estable"] = "steady",
				["tendencia.en-ascenso"] = "rising",
				["tendencia.en-descenso"] = "falling",
				["tendencia.salto-inverosimil"] = "implausible jump",
				["tendencia.no-aplica"] = "not applicable",

				["presencia.indeterminada"] = "undetermined",
				["presencia.nunca-aparecio"] = "never showed up",
				["presencia.aparecio"] = "showed up",
				["presencia.persiste"] = "persists",
				["presencia.no-confirmado"] = "not confirmed",
				["presencia.cedio"] = "cleared",

				["constancia.indeterminada"] = "undetermined",
				["constancia.siempre-esperable"] = "always as expected",
				["constancia.aparecio"] = "showed up",
				["constancia.persiste"] = "persists",
				["constancia.no-confirmado"] = "not confirmed",
				["constancia.volvio"] = "back to what is expected",

				["sexo.hombre"] = "male",
				["sexo.mujer"] = "female",
				["sexo.desconocido"] = "unknown",

				["accion.mostrarle-esto-a-tu-medico"] = "show this to your doctor {plazo}",
				["accion.conversarlo-con-tu-medico"] = "talk it over with your doctor {plazo}",
				["accion.repetir-el-examen"] = "repeat the test {plazo}",
				["accion.buscar-atencion-medica"] = "seek medical care {plazo}",

				["plazo.en-la-proxima-cita"] = "at your next appointment",
				["plazo.este-mes"] = "this month",
				["plazo.esta-semana"] = "this week",
				["plazo.pronto"] = "soon",
				["plazo.cuando-el-medico-lo-indique"] = "when your doctor says so",

				// ------------------------------------------------- lo que dio cada prueba

				["categoria.positivo"] = "positive",
				["categoria.negativo"] = "negative",
				["categoria.no-realizado"] = "not run",

				["categoria.transparente"] = "clear",
				["categoria.amarillo-palido"] = "pale yellow",
				["categoria.ambar"] = "amber",
				["categoria.oscuro"] = "dark",
				["categoria.rojizo"] = "reddish",

				["categoria.chocolate"] = "chocolate",
				["categoria.marron"] = "brown",
				["categoria.verdoso"] = "greenish",
				["categoria.negro"] = "black",
				["categoria.palido"] = "pale",

				["resultado.no-realizado"] = "the lab did not run this test",
				["resultado.sin-hallazgos"] = "nothing out of the ordinary",

				["resultado.nitritos.positivo"] =
					"nitrites came back positive, which often points to a urinary infection",
				["resultado.nitritos.negativo"] = "nitrites came back negative, which is what is expected",

				["resultado.sangre-en-orina.positivo"] =
					"blood showed up in your urine, and that is worth having a doctor look at soon",
				["resultado.sangre-en-orina.negativo"] =
					"no blood showed up in your urine, which is what is expected",

				["resultado.proteina-en-orina.positivo"] = "protein showed up in your urine",
				["resultado.proteina-en-orina.negativo"] = "no protein showed up in your urine",

				["resultado.cetonas.positivo"] = "ketones showed up in your urine",
				["resultado.cetonas.negativo"] = "no ketones showed up in your urine",

				["resultado.glucosa-en-orina.positivo"] = "glucose showed up in your urine",
				["resultado.glucosa-en-orina.negativo"] = "no glucose showed up in your urine",

				["resultado.sangre-en-heces.positivo"] =
					"blood showed up in your stool, which often comes from something common; it is "
					+ "worth looking into",
				["resultado.sangre-en-heces.negativo"] =
					"no blood showed up in your stool, which is what is expected",

				["resultado.color-de-la-orina.esperable"] =
					"your urine color came back {categoria}, which is within what is expected",
				["resultado.color-de-la-orina.hallazgo"] = "your urine color came back {categoria}",

				["resultado.color-de-las-heces.esperable"] =
					"your stool color came back {categoria}, which is within what is expected",
				["resultado.color-de-las-heces.hallazgo"] = "your stool color came back {categoria}",

				["lista.parte"] = "{analito} {resultado}",

				["corto.nitritos"] = "nitrites",
				["corto.sangre-en-orina"] = "blood",
				["corto.proteina-en-orina"] = "protein",
				["corto.cetonas"] = "ketones",
				["corto.glucosa-en-orina"] = "glucose",
				["corto.color-de-la-orina"] = "color",
				["corto.sangre-en-heces"] = "blood",
				["corto.color-de-las-heces"] = "color",
				["lista.colesterol-detallado"] = "LDL {ldl} / HDL {hdl}",

				// ------------------------------------------------- contra que se lee el valor

				["referencia.depende-del-sexo"] = "depends on your sex",
				["referencia.hasta"] = "up to {techo}",
				["referencia.entre"] = "between {piso} and {techo}",
				["referencia.esperable-negativo"] = "negative is what is expected",
				["referencia.color-de-la-orina"] = "clear, pale yellow or amber is what is expected",
				["referencia.color-de-las-heces"] = "chocolate, brown or greenish is what is expected",

				// ------------------------------------------------- las lineas del grafico

				["linea.antigeno-prostatico.habitual"] = "your usual level sits around {valor}",
				["linea.antigeno-prostatico.techo"] = "at your age, what is expected goes up to {valor}",
				["linea.colesterol-total.partida"] = "you were coming from {valor}",
				["linea.colesterol-total.meta"] = "your goal now is to get down to {valor}",
				["linea.colesterol-ldl.meta"] = "your LDL goal is to get below {valor}",
				["linea.hemoglobina.piso"] = "below {valor} is already anemia",
				["linea.hemoglobina.techo"] = "normal goes up to {valor}",
				["linea.glucosa-en-ayunas.piso"] = "below {valor} is hypoglycemia",
				["linea.glucosa-en-ayunas.techo"] = "normal fasting goes up to {valor}",
				["linea.trigliceridos.meta"] = "the goal is to get below {valor}",
				["linea.sodio.piso"] = "below {valor} is hyponatremia",
				["linea.sodio.techo"] = "normal goes up to {valor}",
				["linea.potasio.piso"] = "below {valor} is hypokalemia",
				["linea.potasio.techo"] = "normal goes up to {valor}",
				["linea.cloro.piso"] = "below {valor} is lower than usual",
				["linea.cloro.techo"] = "normal goes up to {valor}",
				["linea.nitrogeno-ureico.piso"] = "below {valor} is lower than usual",
				["linea.nitrogeno-ureico.techo"] = "normal goes up to {valor}",
				["linea.proteinas-totales.piso"] = "below {valor} is lower than usual",
				["linea.proteinas-totales.techo"] = "normal goes up to {valor}",

				// ------------------------------------------------- lo que el analito concluye

				["lectura.sin-serie.una"] =
					"a single measurement: there is no series yet to say which way it is going",
				["lectura.sin-serie.ninguna"] = "there are no measurements of this test yet",
				["lectura.no-aplica"] = "{analito} does not apply to your case",
				["lectura.requiere-sexo.aplicabilidad"] =
					"without knowing your sex I cannot say whether {analito} applies to you",
				["lectura.requiere-sexo.rango"] =
					"{analito} is read against different ranges depending on sex, and I do not know "
					+ "yours yet",

				["lectura.antigeno-prostatico.pico-no-confirmado"] =
					"the test was repeated and the value came back to {valor}: the earlier spike was "
					+ "not confirmed, and you are at your usual level ({nivel})",
				["lectura.antigeno-prostatico.salto-inverosimil"] =
					"it changed by {cambio} in {dias}, which is too much for the time that passed",
				["lectura.antigeno-prostatico.viene-subiendo"] =
					"it has been rising at {ritmo}, and above {limite} a year it is worth looking at",
				["lectura.antigeno-prostatico.se-salio-de-lo-suyo"] =
					"you were around {nivel} and this time it came back {valor}: what matters here is "
					+ "not the number but that it moved off your own level",
				["lectura.antigeno-prostatico.estable-dentro"] =
					"it has stayed around {valor} for {dias}",
				["lectura.antigeno-prostatico.estable-fuera"] =
					"it is steady, but at {valor} it is above what is expected at your age",
				["lectura.antigeno-prostatico.bajo"] = "it went down from {antes} to {ahora}",
				["lectura.antigeno-prostatico.subio"] = "it went up from {antes} to {ahora} in {dias}",

				["lectura.colesterol-total.salto-inverosimil"] =
					"it went from {antes} to {ahora} in {dias}, which is not a change the body can "
					+ "make in that time",
				["lectura.colesterol-total.va-bien"] = "you are on track: {meta}",
				["lectura.colesterol-total.va-bien-desde"] =
					"you were coming from {nivel} and you are on track: {meta}",
				["lectura.colesterol-total.estable-dentro"] =
					"it is holding at {valor}, within what is desirable",
				["lectura.colesterol-total.estable-fuera"] =
					"it has been at {valor} for a while, above what is desirable",
				["lectura.colesterol-total.subio-dentro"] =
					"it went up from {antes} to {ahora} in {dias}, still within what is desirable",
				["lectura.colesterol-total.subio-fuera-joven"] =
					"it went up to {valor}. At your age this can be turned around, and {comoObjetivo}",
				["lectura.colesterol-total.subio-fuera"] =
					"it went up from {antes} to {ahora} in {dias}. {ComoObjetivo}",

				["lectura.colesterol-ldl.salto-inverosimil"] =
					"LDL went from {antes} to {ahora} in {dias}",
				["lectura.colesterol-ldl.estable-dentro"] =
					"LDL is holding at {valor}, within what is desirable",
				["lectura.colesterol-ldl.estable-fuera"] = "LDL has been at {valor} for a while{hdl}",
				["lectura.colesterol-ldl.bajo"] = "LDL went down from {antes} to {ahora}",
				["lectura.colesterol-ldl.subio-dentro"] =
					"LDL went up from {antes} to {ahora}, still within what is desirable",
				["lectura.colesterol-ldl.subio-fuera"] = "LDL went up from {antes} to {ahora}{hdl}",

				["lectura.hemoglobina.una-sola-con-anemia"] =
					"a single measurement ({valor}): there is no series yet, but that value already "
					+ "points to {coletilla} anemia",
				["lectura.hemoglobina.salto-inverosimil"] =
					"it changed by {cambio} in {dias}, which is too much for the time that passed",
				["lectura.hemoglobina.estable-dentro"] = "it is holding at {valor}, within normal",
				["lectura.hemoglobina.estable-fuera"] = "it has been at {valor} for a while{coletilla}",
				["lectura.hemoglobina.volvio"] = "it came back to {valor}, within normal",
				["lectura.hemoglobina.paso-fuera"] = "it went from {antes} to {ahora}{coletilla}",

				["lectura.trigliceridos.salto-inverosimil"] =
					"triglycerides changed by {cambio} in {dias}, which is too much even for a test "
					+ "that moves fast",
				["lectura.trigliceridos.estable-dentro"] =
					"triglycerides are holding at {valor}, within what is desirable",
				["lectura.trigliceridos.estable-fuera"] =
					"triglycerides have been at {valor} for a while",
				["lectura.trigliceridos.bajo"] = "triglycerides went down from {antes} to {ahora}",
				["lectura.trigliceridos.subio-dentro"] =
					"triglycerides went up from {antes} to {ahora}, still within what is desirable",
				["lectura.trigliceridos.subio-fuera"] =
					"triglycerides went up from {antes} to {ahora}",

				["lectura.glucosa-en-ayunas.salto-inverosimil"] =
					"fasting glucose changed by {cambio} in {dias}, which is too much for the time "
					+ "that passed",
				["lectura.glucosa-en-ayunas.estable-dentro"] =
					"fasting glucose is holding at {valor}, within normal",
				["lectura.glucosa-en-ayunas.estable-fuera"] =
					"fasting glucose has been at {valor} for a while, outside normal",
				["lectura.glucosa-en-ayunas.volvio"] =
					"fasting glucose came back to {valor}, within normal",
				["lectura.glucosa-en-ayunas.paso-fuera"] =
					"fasting glucose went from {antes} to {ahora}, outside normal",

				["lectura.sodio.salto-inverosimil"] =
					"sodium changed by {cambio} in {dias}, which is too much for the time that passed",
				["lectura.sodio.estable-dentro"] = "sodium is holding at {valor}, within normal",
				["lectura.sodio.estable-fuera"] =
					"sodium has been at {valor} for a while, outside normal",
				["lectura.sodio.volvio"] = "sodium came back to {valor}, within normal",
				["lectura.sodio.paso-fuera"] = "sodium went from {antes} to {ahora}, outside normal",

				["lectura.potasio.salto-inverosimil"] =
					"potassium changed by {cambio} in {dias}, which is too much for the time that passed",
				["lectura.potasio.estable-dentro"] = "potassium is holding at {valor}, within normal",
				["lectura.potasio.estable-fuera"] =
					"potassium has been at {valor} for a while, outside normal",
				["lectura.potasio.volvio"] = "potassium came back to {valor}, within normal",
				["lectura.potasio.paso-fuera"] = "potassium went from {antes} to {ahora}, outside normal",

				["lectura.cloro.salto-inverosimil"] =
					"chloride changed by {cambio} in {dias}, which is too much for the time that passed",
				["lectura.cloro.estable-dentro"] = "chloride is holding at {valor}, within normal",
				["lectura.cloro.estable-fuera"] =
					"chloride has been at {valor} for a while, outside normal",
				["lectura.cloro.volvio"] = "chloride came back to {valor}, within normal",
				["lectura.cloro.paso-fuera"] = "chloride went from {antes} to {ahora}, outside normal",

				["lectura.nitrogeno-ureico.salto-inverosimil"] =
					"blood urea nitrogen changed by {cambio} in {dias}, which is too much for the "
					+ "time that passed",
				["lectura.nitrogeno-ureico.estable-dentro"] =
					"blood urea nitrogen is holding at {valor}, within normal",
				["lectura.nitrogeno-ureico.estable-fuera"] =
					"blood urea nitrogen has been at {valor} for a while, outside normal",
				["lectura.nitrogeno-ureico.volvio"] =
					"blood urea nitrogen came back to {valor}, within normal",
				["lectura.nitrogeno-ureico.paso-fuera"] =
					"blood urea nitrogen went from {antes} to {ahora}, outside normal",

				["lectura.proteinas-totales.salto-inverosimil"] =
					"total protein changed by {cambio} in {dias}, which is too much for the time that "
					+ "passed",
				["lectura.proteinas-totales.estable-dentro"] =
					"total protein is holding at {valor}, within normal",
				["lectura.proteinas-totales.estable-fuera"] =
					"total protein has been at {valor} for a while, outside normal",
				["lectura.proteinas-totales.volvio"] =
					"total protein came back to {valor}, within normal",
				["lectura.proteinas-totales.paso-fuera"] =
					"total protein went from {antes} to {ahora}, outside normal",

				["lectura.nitritos.nunca-aparecio"] = "nitrites came back negative all {cuantas}",
				["lectura.nitritos.persiste"] =
					"nitrites are still positive across {seguidas} tests in a row: the infection did "
					+ "not clear",
				["lectura.nitritos.aparecio"] =
					"nitrites came back positive, which often points to a urinary infection",
				["lectura.nitritos.no-confirmado"] =
					"nitrites came back positive once and were negative again on the next test",
				["lectura.nitritos.cedio"] =
					"nitrites were positive {positivas} and came back negative on the last one",

				["lectura.cetonas.nunca-aparecio"] = "no ketones showed up in {cuantas}",
				["lectura.cetonas.persiste"] =
					"ketones showed up across {seguidas} tests in a row: a one-day fast no longer "
					+ "explains them",
				["lectura.cetonas.aparecio"] =
					"ketones showed up in your urine, which is expected with a long fast or a "
					+ "low-carbohydrate diet",
				["lectura.cetonas.no-confirmado"] =
					"ketones showed up once and were gone on the next test",
				["lectura.cetonas.cedio"] =
					"ketones showed up {positivas} and are gone on the last one",

				["lectura.glucosa-en-orina.nunca-aparecio"] =
					"no glucose showed up in your urine in {cuantas}",
				["lectura.glucosa-en-orina.persiste"] =
					"glucose showed up in your urine across {seguidas} tests in a row",
				["lectura.glucosa-en-orina.aparecio"] =
					"glucose showed up in your urine, which almost always means blood glucose ran high",
				["lectura.glucosa-en-orina.no-confirmado"] =
					"glucose showed up in your urine once and was gone on the next test",
				["lectura.glucosa-en-orina.cedio"] =
					"glucose showed up in your urine {positivas} and is gone on the last one",

				["lectura.proteina-en-orina.nunca-aparecio"] =
					"no protein showed up in your urine in {cuantas}",
				["lectura.proteina-en-orina.persiste"] =
					"protein showed up across {seguidas} tests in a row: proteinuria that repeats is "
					+ "no longer passing",
				["lectura.proteina-en-orina.aparecio"] =
					"protein showed up in your urine, which on its own is usually passing —fever, "
					+ "exercise— and what follows is to repeat it",
				["lectura.proteina-en-orina.no-confirmado"] =
					"protein showed up once and was gone on the next test",
				["lectura.proteina-en-orina.cedio"] =
					"protein showed up {positivas} and is gone on the last one",

				["lectura.sangre-en-orina.nunca-aparecio"] =
					"across the {pruebas} you have had, blood never showed up in your urine",
				["lectura.sangre-en-orina.persiste"] =
					"blood showed up in your urine across {seguidas} tests in a row: it was not passing",
				["lectura.sangre-en-orina.aparecio"] =
					"blood showed up in your urine, and that is worth having a doctor look at soon",
				["lectura.sangre-en-orina.no-confirmado"] =
					"blood showed up in your urine once and the next test came back clean: it was passing",
				["lectura.sangre-en-orina.cedio"] =
					"blood showed up in your urine {positivas} and is gone on the last one",

				["lectura.sangre-en-heces.nunca-aparecio"] =
					"across the {pruebas} you have had, blood never showed up in your stool",
				["lectura.sangre-en-heces.persiste"] =
					"blood showed up in your stool across {seguidas} tests in a row: it is no longer "
					+ "a one-off result",
				["lectura.sangre-en-heces.aparecio"] =
					"blood showed up in your stool for the first time; it often comes from something "
					+ "common",
				["lectura.sangre-en-heces.no-confirmado"] =
					"blood showed up in your stool once and was gone on the next test",
				["lectura.sangre-en-heces.cedio"] =
					"blood showed up in your stool {positivas} and is gone on the last one",

				["lectura.color-de-la-orina.siempre-esperable"] =
					"across the {cuantas} you have had it, your urine color stayed within what is "
					+ "expected",
				["lectura.color-de-la-orina.persiste-la-misma"] =
					"your urine color came back {categoria} {seguidas} times in a row: it is no "
					+ "longer a one-off result",
				["lectura.color-de-la-orina.persiste"] =
					"your urine color has not come back to what is expected in the last {seguidas} times",
				["lectura.color-de-la-orina.aparecio"] =
					"your urine color came back {categoria}, and the time before it was within what "
					+ "is expected",
				["lectura.color-de-la-orina.no-confirmado"] =
					"once your urine color fell outside what is expected and on the next test it was "
					+ "{categoria} again",
				["lectura.color-de-la-orina.volvio"] =
					"your urine color fell outside what is expected {noEsperables} and on the last "
					+ "one it came back {categoria}",

				["lectura.color-de-las-heces.siempre-esperable"] =
					"across the {cuantas} you have had it, your stool color stayed within what is "
					+ "expected",
				["lectura.color-de-las-heces.persiste-la-misma"] =
					"your stool color came back {categoria} {seguidas} times in a row: it is no "
					+ "longer a one-off result",
				["lectura.color-de-las-heces.persiste"] =
					"your stool color has not come back to what is expected in the last {seguidas} times",
				["lectura.color-de-las-heces.aparecio"] =
					"your stool color came back {categoria}, and the time before it was within what "
					+ "is expected",
				["lectura.color-de-las-heces.no-confirmado"] =
					"once your stool color fell outside what is expected and on the next test it was "
					+ "{categoria} again",
				["lectura.color-de-las-heces.volvio"] =
					"your stool color fell outside what is expected {noEsperables} and on the last "
					+ "one it came back {categoria}",

				// ------------------------------------------------- las clausulas que cuelgan

				["meta.ya-llego"] = "you are at {valor}, within what is desirable",
				["meta.falta"] = "from {valor} to {objetivo}: {falta} to go",
				["meta.como-objetivo.ya-llego"] = "you are already within what is desirable",
				["meta.como-objetivo.falta"] = "your goal now is to get down to {objetivo}",

				["hdl.juega-a-favor"] = ", and your HDL of {valor} works in your favor",
				["hdl.no-alcanza"] = ", and your HDL of {valor} is not enough to offset it",

				["anemia.ninguna"] = ", outside normal",
				["anemia.leve"] = ", which is mild anemia",
				["anemia.moderada"] = ", which is moderate anemia",
				["anemia.severa"] = ", which is severe anemia",

				["grado.leve"] = "mild",
				["grado.moderada"] = "moderate",
				["grado.severa"] = "severe",

				// ------------------------------------------------- a que ritmo viene cambiando

				["velocidad.por-ano"] = "{ritmo} a year",
				["velocidad.no-se-interpreta"] = "it is not read by rate of change",

				// ------------------------------------------------- el antigeno, ano por ano

				["ano.sin-comparacion"] = "{ano}: {valor}",
				["ano.con-comparacion"] = "{ano}: {valor} ({cambio} against {anterior})",
				["banda.expresada"] = "{piso}–{techo} {unidad}: {cuantas}",

				// ------------------------------------------------- lo que falta medir

				["control.vencido"] = "{analito} is from {hace}, and it is worth repeating every {cada}",
				["control.al-dia"] = "{analito} is from {hace}: up to date",
				["control.hace.anos"] = "{anios} ago",
				["control.hace.dias"] = "{dias} ago",
				["control.cada.anos"] = "{anios}",
				["control.cada.meses"] = "{meses}",

				// ------------------------------------------------- lo que esta bien

				["enRango.todos.uno"] = "Your {enSuRango} value is in range.",
				["enRango.todos.varios"] = "All {enSuRango} of your values are in range.",
				["enRango.algunos.uno"] = "{enSuRango} of your {analitos} values is in range.",
				["enRango.algunos.varios"] = "{enSuRango} of your {analitos} values are in range.",

				// ------------------------------------------------- el dato que falta

				["pedidoDeSexo.marco"] =
					"Without knowing your sex I cannot read {sinInterpretar} of your {analitos} "
					+ "results: {porque}.",
				["pedidoDeSexo.porQue"] = "It is to compare your results against the right ranges.",
				["pedidoDeSexo.rango.uno"] = "the range for {cuales} differs by sex",
				["pedidoDeSexo.rango.varios"] = "the ranges for {cuales} differ by sex",
				["pedidoDeSexo.aplicabilidad.uno"] =
					"there is one test —{cuales}— that may not apply to you",
				["pedidoDeSexo.aplicabilidad.varios"] =
					"there are {cuantos} tests —{cuales}— that may not apply to you",
			};
		}
	}
}
