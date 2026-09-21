using System.Collections.Generic;

namespace Examenes.App.Frases
{
	/// <summary>
	/// LA REDACCION EN EL IDIOMA DEL REGISTRO. El espanol de este catalogo es el de Costa Rica y es
	/// voseo -- mostrale, repeti, conversalo --: no lo pase a tuteo.
	/// <para>
	/// Los huecos llevan valores del registro del laboratorio -- un numero medido y el simbolo de su
	/// unidad -- y esos NO se traducen en ningun idioma.
	/// </para>
	/// <para>
	/// LOS UMBRALES Y LAS PERIODICIDADES SON ORIENTATIVOS, y este catalogo es donde eso se puede
	/// romper de un adjetivo: una frase dice lo que la serie muestra y se calla. No hay diagnosticos
	/// aca, y todo consejo lleva su plazo porque lo trae la accion.
	/// </para>
	/// </summary>
	internal static class CatalogoEnEspanol
	{
		internal static IReadOnlyDictionary<string, string> ElCatalogo()
		{
			return new Dictionary<string, string>
			{
				// ------------------------------------------------- como se llama cada analito

				["nombre.antigeno-prostatico"] = "Antígeno prostático",
				["nombre.colesterol-total"] = "Colesterol total",
				["nombre.colesterol-ldl"] = "Colesterol LDL",
				["nombre.colesterol-hdl"] = "Colesterol HDL",
				["nombre.hemoglobina"] = "Hemoglobina",
				["nombre.trigliceridos"] = "Triglicéridos",
				["nombre.glucosa-en-ayunas"] = "Glucosa en ayunas",
				["nombre.nitrogeno-ureico"] = "Nitrógeno ureico",
				["nombre.sodio"] = "Sodio",
				["nombre.potasio"] = "Potasio",
				["nombre.cloro"] = "Cloro",
				["nombre.proteinas-totales"] = "Proteínas totales",
				["nombre.color-de-la-orina"] = "Color de la orina",
				["nombre.nitritos"] = "Nitritos",
				["nombre.sangre-en-orina"] = "Sangre en orina",
				["nombre.proteina-en-orina"] = "Proteína en orina",
				["nombre.cetonas"] = "Cetonas",
				["nombre.glucosa-en-orina"] = "Glucosa en orina",
				["nombre.color-de-las-heces"] = "Color de las heces",
				["nombre.sangre-en-heces"] = "Sangre en heces",
				["nombre.examen-de-orina"] = "Examen general de orina",
				["nombre.examen-de-heces"] = "Examen general de heces",
				["nombre.colesterol-detallado"] = "Colesterol HDL/LDL",

				// EL MISMO NOMBRE CON SU ARTICULO, que es lo que entra en el hueco de una frase.
				// Es una frase nominal entera y no media frase: el genero de un nombre no se
				// puede pegar aparte sin romper la concordancia en espanol.

				["elNombre.antigeno-prostatico"] = "el antígeno prostático",
				["elNombre.colesterol-total"] = "el colesterol total",
				["elNombre.colesterol-ldl"] = "el colesterol LDL",
				["elNombre.colesterol-hdl"] = "el colesterol HDL",
				["elNombre.hemoglobina"] = "la hemoglobina",
				["elNombre.trigliceridos"] = "los triglicéridos",
				["elNombre.glucosa-en-ayunas"] = "la glucosa en ayunas",
				["elNombre.nitrogeno-ureico"] = "el nitrógeno ureico",
				["elNombre.sodio"] = "el sodio",
				["elNombre.potasio"] = "el potasio",
				["elNombre.cloro"] = "el cloro",
				["elNombre.proteinas-totales"] = "las proteínas totales",
				["elNombre.color-de-la-orina"] = "el color de la orina",
				["elNombre.nitritos"] = "los nitritos",
				["elNombre.sangre-en-orina"] = "la sangre en orina",
				["elNombre.proteina-en-orina"] = "la proteína en orina",
				["elNombre.cetonas"] = "las cetonas",
				["elNombre.glucosa-en-orina"] = "la glucosa en orina",
				["elNombre.color-de-las-heces"] = "el color de las heces",
				["elNombre.sangre-en-heces"] = "la sangre en heces",

				// Y EL MISMO NOMBRE COMO POSESIVO, por lo mismo: "tu ultimo" y "tu ultima"
				// concuerdan con el nombre, y en ingles no concuerda nada.

				["tuUltimo.antigeno-prostatico"] = "tu último antígeno prostático",
				["tuUltimo.colesterol-total"] = "tu último colesterol total",
				["tuUltimo.colesterol-ldl"] = "tu último colesterol LDL",
				["tuUltimo.colesterol-hdl"] = "tu último colesterol HDL",
				["tuUltimo.hemoglobina"] = "tu última hemoglobina",
				["tuUltimo.trigliceridos"] = "tus últimos triglicéridos",
				["tuUltimo.glucosa-en-ayunas"] = "tu última glucosa en ayunas",
				["tuUltimo.nitrogeno-ureico"] = "tu último nitrógeno ureico",
				["tuUltimo.sodio"] = "tu último sodio",
				["tuUltimo.potasio"] = "tu último potasio",
				["tuUltimo.cloro"] = "tu último cloro",
				["tuUltimo.proteinas-totales"] = "tus últimas proteínas totales",
				["tuUltimo.color-de-la-orina"] = "tu último color de la orina",
				["tuUltimo.nitritos"] = "tus últimos nitritos",
				["tuUltimo.sangre-en-orina"] = "tu última prueba de sangre en orina",
				["tuUltimo.proteina-en-orina"] = "tu última proteína en orina",
				["tuUltimo.cetonas"] = "tus últimas cetonas",
				["tuUltimo.glucosa-en-orina"] = "tu última glucosa en orina",
				["tuUltimo.color-de-las-heces"] = "tu último color de las heces",
				["tuUltimo.sangre-en-heces"] = "tu última prueba de sangre en heces",

				// ------------------------------------------------- los conjuntos cerrados

				["severidad.informativo"] = "informativo",
				["severidad.vigilar"] = "vigilar",
				["severidad.consulte-pronto"] = "consulte pronto",
				["severidad.urgente"] = "urgente",

				["veredicto.indeterminado"] = "indeterminado",
				["veredicto.observado"] = "observado",
				["veredicto.estable"] = "estable",
				["veredicto.mejorando"] = "mejorando",
				["veredicto.empeorando"] = "empeorando",
				["veredicto.probable-error-de-medicion"] = "probable error de medición",
				["veredicto.pico-no-confirmado"] = "pico no confirmado",
				["veredicto.hallazgo-no-confirmado"] = "hallazgo no confirmado",
				["veredicto.no-aplica"] = "no aplica",
				["veredicto.requiere-sexo"] = "requiere sexo",

				["tendencia.indeterminada"] = "indeterminada",
				["tendencia.estable"] = "estable",
				["tendencia.en-ascenso"] = "en ascenso",
				["tendencia.en-descenso"] = "en descenso",
				["tendencia.salto-inverosimil"] = "salto inverosímil",
				["tendencia.no-aplica"] = "no aplica",

				["presencia.indeterminada"] = "indeterminada",
				["presencia.nunca-aparecio"] = "nunca apareció",
				["presencia.aparecio"] = "apareció",
				["presencia.persiste"] = "persiste",
				["presencia.no-confirmado"] = "no confirmado",
				["presencia.cedio"] = "cedió",

				["constancia.indeterminada"] = "indeterminada",
				["constancia.siempre-esperable"] = "siempre lo esperable",
				["constancia.aparecio"] = "apareció",
				["constancia.persiste"] = "persiste",
				["constancia.no-confirmado"] = "no confirmado",
				["constancia.volvio"] = "volvió a lo esperable",

				["sexo.hombre"] = "hombre",
				["sexo.mujer"] = "mujer",
				["sexo.desconocido"] = "desconocido",

				// LA ACCION ENTERA, con su plazo adentro. Todo consejo lleva plazo, y por eso el
				// plazo no es un texto suelto que alguien pueda olvidarse de pegar.

				["accion.mostrarle-esto-a-tu-medico"] = "mostrále esto a tu médico {plazo}",
				["accion.conversarlo-con-tu-medico"] = "conversalo con tu médico {plazo}",
				["accion.repetir-el-examen"] = "repetí el examen {plazo}",
				["accion.buscar-atencion-medica"] = "buscá atención médica {plazo}",

				["plazo.en-la-proxima-cita"] = "en la próxima cita",
				["plazo.este-mes"] = "este mes",
				["plazo.esta-semana"] = "esta semana",
				["plazo.pronto"] = "pronto",
				["plazo.cuando-el-medico-lo-indique"] = "cuando el médico lo indique",

				// ------------------------------------------------- lo que dio cada prueba

				["categoria.positivo"] = "positivo",
				["categoria.negativo"] = "negativo",
				["categoria.no-realizado"] = "no realizado",

				["categoria.transparente"] = "transparente",
				["categoria.amarillo-palido"] = "amarillo pálido",
				["categoria.ambar"] = "ámbar",
				["categoria.oscuro"] = "oscuro",
				["categoria.rojizo"] = "rojizo",

				["categoria.chocolate"] = "chocolate",
				["categoria.marron"] = "marrón",
				["categoria.verdoso"] = "verdoso",
				["categoria.negro"] = "negro",
				["categoria.palido"] = "pálido",

				["resultado.no-realizado"] = "el laboratorio no corrió esta prueba",
				["resultado.sin-hallazgos"] = "sin hallazgos",

				["resultado.nitritos.positivo"] =
					"los nitritos salieron positivos, que suele indicar una infección urinaria",
				["resultado.nitritos.negativo"] = "los nitritos salieron negativos, que es lo esperable",

				["resultado.sangre-en-orina.positivo"] =
					"apareció sangre en la orina, y eso conviene que lo revise un médico pronto",
				["resultado.sangre-en-orina.negativo"] =
					"no apareció sangre en la orina, que es lo esperable",

				["resultado.proteina-en-orina.positivo"] = "salió proteína en la orina",
				["resultado.proteina-en-orina.negativo"] = "no salió proteína en la orina",

				["resultado.cetonas.positivo"] = "salieron cetonas en la orina",
				["resultado.cetonas.negativo"] = "no salieron cetonas en la orina",

				["resultado.glucosa-en-orina.positivo"] = "salió glucosa en la orina",
				["resultado.glucosa-en-orina.negativo"] = "no salió glucosa en la orina",

				["resultado.sangre-en-heces.positivo"] =
					"apareció sangre en las heces, que muchas veces se debe a algo común; conviene "
					+ "revisarlo",
				["resultado.sangre-en-heces.negativo"] =
					"no apareció sangre en las heces, que es lo esperable",

				["resultado.color-de-la-orina.esperable"] =
					"el color de la orina salió {categoria}, que es de lo esperable",
				["resultado.color-de-la-orina.hallazgo"] = "el color de la orina salió {categoria}",

				["resultado.color-de-las-heces.esperable"] =
					"el color de las heces salió {categoria}, que es de lo esperable",
				["resultado.color-de-las-heces.hallazgo"] = "el color de las heces salió {categoria}",

				// COMO SE LEE LA FILA DE UN EXAMEN QUE MIDIO VARIAS COSAS. La primera va sola
				// y las demas nombran que se midio, que es como lo informa el laboratorio.

				["lista.parte"] = "{analito} {resultado}",

				// EL NOMBRE CORTO CON QUE UN URIANALISIS NOMBRA LO QUE MIDIO. Adentro de esa
				// fila el sujeto ya es la orina, asi que repetirlo en cada parte sobra.
				["corto.nitritos"] = "nitritos",
				["corto.sangre-en-orina"] = "sangre",
				["corto.proteina-en-orina"] = "proteína",
				["corto.cetonas"] = "cetonas",
				["corto.glucosa-en-orina"] = "glucosa",
				["corto.color-de-la-orina"] = "color",
				["corto.sangre-en-heces"] = "sangre",
				["corto.color-de-las-heces"] = "color",
				["lista.colesterol-detallado"] = "LDL {ldl} / HDL {hdl}",

				// ------------------------------------------------- contra que se lee el valor

				["referencia.depende-del-sexo"] = "depende de tu sexo",
				["referencia.hasta"] = "hasta {techo}",
				["referencia.entre"] = "entre {piso} y {techo}",
				["referencia.esperable-negativo"] = "lo esperable es negativo",
				["referencia.color-de-la-orina"] = "lo esperable es transparente, amarillo pálido o ámbar",
				["referencia.color-de-las-heces"] = "lo esperable es chocolate, marrón o verdoso",

				// ------------------------------------------------- las lineas del grafico

				["linea.antigeno-prostatico.habitual"] = "tu nivel de siempre ronda {valor}",
				["linea.antigeno-prostatico.techo"] = "a tu edad, lo esperable llega hasta {valor}",
				["linea.colesterol-total.partida"] = "venías de {valor}",
				["linea.colesterol-total.meta"] = "tu meta ahora es bajar a {valor}",
				["linea.colesterol-ldl.meta"] = "tu meta de LDL es bajar de {valor}",
				["linea.hemoglobina.piso"] = "por debajo de {valor} ya es anemia",
				["linea.hemoglobina.techo"] = "lo normal llega hasta {valor}",
				["linea.glucosa-en-ayunas.piso"] = "por debajo de {valor} es una hipoglucemia",
				["linea.glucosa-en-ayunas.techo"] = "lo normal en ayunas llega hasta {valor}",
				["linea.trigliceridos.meta"] = "la meta es bajar de {valor}",
				["linea.sodio.piso"] = "por debajo de {valor} es hiponatremia",
				["linea.sodio.techo"] = "lo normal llega hasta {valor}",
				["linea.potasio.piso"] = "por debajo de {valor} es hipopotasemia",
				["linea.potasio.techo"] = "lo normal llega hasta {valor}",
				["linea.cloro.piso"] = "por debajo de {valor} es mas bajo de lo habitual",
				["linea.cloro.techo"] = "lo normal llega hasta {valor}",
				["linea.nitrogeno-ureico.piso"] = "por debajo de {valor} es mas bajo de lo habitual",
				["linea.nitrogeno-ureico.techo"] = "lo normal llega hasta {valor}",
				["linea.proteinas-totales.piso"] = "por debajo de {valor} es mas bajo de lo habitual",
				["linea.proteinas-totales.techo"] = "lo normal llega hasta {valor}",

				// ------------------------------------------------- lo que el analito concluye

				["lectura.sin-serie.una"] =
					"una sola medición: todavía no hay serie para decir para dónde viene",
				["lectura.sin-serie.ninguna"] = "todavía no hay mediciones de este analito",
				["lectura.no-aplica"] = "{analito} no corresponde a tu caso",
				["lectura.requiere-sexo.aplicabilidad"] =
					"sin saber tu sexo no puedo decir si {analito} te corresponde",
				["lectura.requiere-sexo.rango"] =
					"{analito} se compara contra rangos distintos según el sexo, y de vos todavía no lo sé",

				["lectura.antigeno-prostatico.pico-no-confirmado"] =
					"se repitió el examen y el valor volvió a {valor}: el pico anterior no se "
					+ "confirmó, y estás en tu nivel de siempre ({nivel})",
				["lectura.antigeno-prostatico.salto-inverosimil"] =
					"cambió {cambio} en {dias}, que es demasiado para el tiempo transcurrido",
				["lectura.antigeno-prostatico.viene-subiendo"] =
					"viene subiendo a {ritmo}, y por encima de {limite} por año conviene mirarlo",
				["lectura.antigeno-prostatico.se-salio-de-lo-suyo"] =
					"venías alrededor de {nivel} y esta vez dio {valor}: lo que importa acá no es el "
					+ "número sino que se salió de lo tuyo",
				["lectura.antigeno-prostatico.estable-dentro"] =
					"se mantiene alrededor de {valor} desde hace {dias}",
				["lectura.antigeno-prostatico.estable-fuera"] =
					"se mantiene estable, pero en {valor} está por encima de lo esperado a tu edad",
				["lectura.antigeno-prostatico.bajo"] = "bajó de {antes} a {ahora}",
				["lectura.antigeno-prostatico.subio"] = "subió de {antes} a {ahora} en {dias}",

				["lectura.colesterol-total.salto-inverosimil"] =
					"pasó de {antes} a {ahora} en {dias}, que no es un cambio que el cuerpo pueda "
					+ "hacer en ese tiempo",
				["lectura.colesterol-total.va-bien"] = "vas bien: {meta}",
				["lectura.colesterol-total.va-bien-desde"] = "venías de {nivel} y vas bien: {meta}",
				["lectura.colesterol-total.estable-dentro"] =
					"se mantiene en {valor}, dentro de lo deseable",
				["lectura.colesterol-total.estable-fuera"] =
					"lleva tiempo en {valor}, por encima de lo deseable",
				["lectura.colesterol-total.subio-dentro"] =
					"subió de {antes} a {ahora} en {dias}, todavía dentro de lo deseable",
				["lectura.colesterol-total.subio-fuera-joven"] =
					"subió a {valor}. A tu edad esto se corrige, y {comoObjetivo}",
				["lectura.colesterol-total.subio-fuera"] =
					"subió de {antes} a {ahora} en {dias}. {ComoObjetivo}",

				["lectura.colesterol-ldl.salto-inverosimil"] = "el LDL pasó de {antes} a {ahora} en {dias}",
				["lectura.colesterol-ldl.estable-dentro"] =
					"el LDL se mantiene en {valor}, dentro de lo deseable",
				["lectura.colesterol-ldl.estable-fuera"] = "el LDL lleva tiempo en {valor}{hdl}",
				["lectura.colesterol-ldl.bajo"] = "el LDL bajó de {antes} a {ahora}",
				["lectura.colesterol-ldl.subio-dentro"] =
					"el LDL subió de {antes} a {ahora}, todavía dentro de lo deseable",
				["lectura.colesterol-ldl.subio-fuera"] = "el LDL subió de {antes} a {ahora}{hdl}",

				["lectura.hemoglobina.una-sola-con-anemia"] =
					"una sola medición ({valor}): todavía no hay serie, pero ese valor ya indica "
					+ "anemia {coletilla}",
				["lectura.hemoglobina.salto-inverosimil"] =
					"cambió {cambio} en {dias}, que es demasiado para el tiempo transcurrido",
				["lectura.hemoglobina.estable-dentro"] = "se mantiene en {valor}, dentro de lo normal",
				["lectura.hemoglobina.estable-fuera"] = "lleva tiempo en {valor}{coletilla}",
				["lectura.hemoglobina.volvio"] = "volvió a {valor}, dentro de lo normal",
				["lectura.hemoglobina.paso-fuera"] = "pasó de {antes} a {ahora}{coletilla}",

				["lectura.trigliceridos.salto-inverosimil"] =
					"los triglicéridos cambiaron {cambio} en {dias}, que es demasiado hasta para un "
					+ "analito que se mueve rápido",
				["lectura.trigliceridos.estable-dentro"] =
					"los triglicéridos se mantienen en {valor}, dentro de lo deseable",
				["lectura.trigliceridos.estable-fuera"] = "los triglicéridos llevan tiempo en {valor}",
				["lectura.trigliceridos.bajo"] = "los triglicéridos bajaron de {antes} a {ahora}",
				["lectura.trigliceridos.subio-dentro"] =
					"los triglicéridos subieron de {antes} a {ahora}, todavía dentro de lo deseable",
				["lectura.trigliceridos.subio-fuera"] =
					"los triglicéridos subieron de {antes} a {ahora}",

				["lectura.glucosa-en-ayunas.salto-inverosimil"] =
					"la glucosa en ayunas cambió {cambio} en {dias}, que es demasiado para el tiempo "
					+ "transcurrido",
				["lectura.glucosa-en-ayunas.estable-dentro"] =
					"la glucosa en ayunas se mantiene en {valor}, dentro de lo normal",
				["lectura.glucosa-en-ayunas.estable-fuera"] =
					"la glucosa en ayunas lleva tiempo en {valor}, fuera de lo normal",
				["lectura.glucosa-en-ayunas.volvio"] =
					"la glucosa en ayunas volvió a {valor}, dentro de lo normal",
				["lectura.glucosa-en-ayunas.paso-fuera"] =
					"la glucosa en ayunas pasó de {antes} a {ahora}, fuera de lo normal",

				["lectura.sodio.salto-inverosimil"] =
					"el sodio cambió {cambio} en {dias}, que es demasiado para el tiempo transcurrido",
				["lectura.sodio.estable-dentro"] = "el sodio se mantiene en {valor}, dentro de lo normal",
				["lectura.sodio.estable-fuera"] = "el sodio lleva tiempo en {valor}, fuera de lo normal",
				["lectura.sodio.volvio"] = "el sodio volvió a {valor}, dentro de lo normal",
				["lectura.sodio.paso-fuera"] = "el sodio pasó de {antes} a {ahora}, fuera de lo normal",

				["lectura.potasio.salto-inverosimil"] =
					"el potasio cambió {cambio} en {dias}, que es demasiado para el tiempo transcurrido",
				["lectura.potasio.estable-dentro"] =
					"el potasio se mantiene en {valor}, dentro de lo normal",
				["lectura.potasio.estable-fuera"] =
					"el potasio lleva tiempo en {valor}, fuera de lo normal",
				["lectura.potasio.volvio"] = "el potasio volvió a {valor}, dentro de lo normal",
				["lectura.potasio.paso-fuera"] = "el potasio pasó de {antes} a {ahora}, fuera de lo normal",

				["lectura.cloro.salto-inverosimil"] =
					"el cloro cambió {cambio} en {dias}, que es demasiado para el tiempo transcurrido",
				["lectura.cloro.estable-dentro"] = "el cloro se mantiene en {valor}, dentro de lo normal",
				["lectura.cloro.estable-fuera"] = "el cloro lleva tiempo en {valor}, fuera de lo normal",
				["lectura.cloro.volvio"] = "el cloro volvió a {valor}, dentro de lo normal",
				["lectura.cloro.paso-fuera"] = "el cloro pasó de {antes} a {ahora}, fuera de lo normal",

				["lectura.nitrogeno-ureico.salto-inverosimil"] =
					"el nitrógeno ureico cambió {cambio} en {dias}, que es demasiado para el tiempo "
					+ "transcurrido",
				["lectura.nitrogeno-ureico.estable-dentro"] =
					"el nitrógeno ureico se mantiene en {valor}, dentro de lo normal",
				["lectura.nitrogeno-ureico.estable-fuera"] =
					"el nitrógeno ureico lleva tiempo en {valor}, fuera de lo normal",
				["lectura.nitrogeno-ureico.volvio"] =
					"el nitrógeno ureico volvió a {valor}, dentro de lo normal",
				["lectura.nitrogeno-ureico.paso-fuera"] =
					"el nitrógeno ureico pasó de {antes} a {ahora}, fuera de lo normal",

				["lectura.proteinas-totales.salto-inverosimil"] =
					"las proteínas totales cambiaron {cambio} en {dias}, que es demasiado para el "
					+ "tiempo transcurrido",
				["lectura.proteinas-totales.estable-dentro"] =
					"las proteínas totales se mantienen en {valor}, dentro de lo normal",
				["lectura.proteinas-totales.estable-fuera"] =
					"las proteínas totales llevan tiempo en {valor}, fuera de lo normal",
				["lectura.proteinas-totales.volvio"] =
					"las proteínas totales volvieron a {valor}, dentro de lo normal",
				["lectura.proteinas-totales.paso-fuera"] =
					"las proteínas totales pasaron de {antes} a {ahora}, fuera de lo normal",

				["lectura.nitritos.nunca-aparecio"] = "los nitritos salieron negativos las {cuantas}",
				["lectura.nitritos.persiste"] =
					"los nitritos siguen positivos en las {seguidas} pruebas seguidas: la infección "
					+ "no cedió",
				["lectura.nitritos.aparecio"] =
					"los nitritos salieron positivos, que suele indicar una infección urinaria",
				["lectura.nitritos.no-confirmado"] =
					"los nitritos salieron positivos una vez y en la prueba siguiente volvieron a "
					+ "negativo",
				["lectura.nitritos.cedio"] =
					"los nitritos estuvieron positivos {positivas} y en la última ya salieron negativos",

				["lectura.cetonas.nunca-aparecio"] = "no salieron cetonas en las {cuantas}",
				["lectura.cetonas.persiste"] =
					"las cetonas salieron en las {seguidas} pruebas seguidas: ya no se explican por "
					+ "un ayuno de un día",
				["lectura.cetonas.aparecio"] =
					"salieron cetonas en la orina, que con un ayuno largo o una dieta baja en "
					+ "carbohidratos es esperable",
				["lectura.cetonas.no-confirmado"] =
					"las cetonas salieron una vez y en la prueba siguiente ya no estaban",
				["lectura.cetonas.cedio"] =
					"las cetonas salieron {positivas} y en la última ya no están",

				["lectura.glucosa-en-orina.nunca-aparecio"] =
					"no salió glucosa en la orina en las {cuantas}",
				["lectura.glucosa-en-orina.persiste"] =
					"la glucosa salió en la orina en las {seguidas} pruebas seguidas",
				["lectura.glucosa-en-orina.aparecio"] =
					"salió glucosa en la orina, que casi siempre significa que la glucosa en sangre "
					+ "estuvo alta",
				["lectura.glucosa-en-orina.no-confirmado"] =
					"la glucosa salió en la orina una vez y en la prueba siguiente ya no",
				["lectura.glucosa-en-orina.cedio"] =
					"la glucosa salió en la orina {positivas} y en la última ya no",

				["lectura.proteina-en-orina.nunca-aparecio"] =
					"no salió proteína en la orina en las {cuantas}",
				["lectura.proteina-en-orina.persiste"] =
					"la proteína salió en las {seguidas} pruebas seguidas: una proteinuria que se "
					+ "repite ya no es pasajera",
				["lectura.proteina-en-orina.aparecio"] =
					"salió proteína en la orina, que una sola vez suele ser pasajero —fiebre, "
					+ "ejercicio— y lo que corresponde es repetirlo",
				["lectura.proteina-en-orina.no-confirmado"] =
					"la proteína salió una vez y en la prueba siguiente ya no estaba",
				["lectura.proteina-en-orina.cedio"] =
					"la proteína salió {positivas} y en la última ya no está",

				["lectura.sangre-en-orina.nunca-aparecio"] =
					"en las {pruebas} que te hiciste nunca apareció sangre en la orina",
				["lectura.sangre-en-orina.persiste"] =
					"la sangre en la orina apareció en las {seguidas} pruebas seguidas: no fue algo "
					+ "pasajero",
				["lectura.sangre-en-orina.aparecio"] =
					"apareció sangre en la orina, y eso conviene que lo revise un médico pronto",
				["lectura.sangre-en-orina.no-confirmado"] =
					"apareció sangre en la orina una vez y la prueba siguiente salió limpia: fue pasajera",
				["lectura.sangre-en-orina.cedio"] =
					"la sangre en la orina apareció {positivas} y en la última ya no está",

				["lectura.sangre-en-heces.nunca-aparecio"] =
					"en las {pruebas} que te hiciste nunca apareció sangre en las heces",
				["lectura.sangre-en-heces.persiste"] =
					"la sangre en las heces apareció en las {seguidas} pruebas seguidas: ya no es un "
					+ "resultado suelto",
				["lectura.sangre-en-heces.aparecio"] =
					"apareció sangre en las heces por primera vez; muchas veces se debe a algo común",
				["lectura.sangre-en-heces.no-confirmado"] =
					"una vez apareció sangre en las heces y en la prueba siguiente ya no estaba",
				["lectura.sangre-en-heces.cedio"] =
					"la sangre en las heces apareció {positivas} y en la última ya no está",

				["lectura.color-de-la-orina.siempre-esperable"] =
					"en las {cuantas} que te lo hiciste el color de la orina estuvo dentro de lo "
					+ "esperable",
				["lectura.color-de-la-orina.persiste-la-misma"] =
					"el color de la orina salió {categoria} las {seguidas} veces seguidas: ya no es "
					+ "un resultado suelto",
				["lectura.color-de-la-orina.persiste"] =
					"el color de la orina no vuelve a lo esperable en las {seguidas} últimas veces",
				["lectura.color-de-la-orina.aparecio"] =
					"el color de la orina salió {categoria}, y la vez anterior era de lo esperable",
				["lectura.color-de-la-orina.no-confirmado"] =
					"una vez el color de la orina salió fuera de lo esperable y en el examen "
					+ "siguiente volvió a {categoria}",
				["lectura.color-de-la-orina.volvio"] =
					"el color de la orina estuvo fuera de lo esperable {noEsperables} y en la última "
					+ "ya salió {categoria}",

				["lectura.color-de-las-heces.siempre-esperable"] =
					"en las {cuantas} que te lo hiciste el color de las heces estuvo dentro de lo "
					+ "esperable",
				["lectura.color-de-las-heces.persiste-la-misma"] =
					"el color de las heces salió {categoria} las {seguidas} veces seguidas: ya no es "
					+ "un resultado suelto",
				["lectura.color-de-las-heces.persiste"] =
					"el color de las heces no vuelve a lo esperable en las {seguidas} últimas veces",
				["lectura.color-de-las-heces.aparecio"] =
					"el color de las heces salió {categoria}, y la vez anterior era de lo esperable",
				["lectura.color-de-las-heces.no-confirmado"] =
					"una vez el color de las heces salió fuera de lo esperable y en el examen "
					+ "siguiente volvió a {categoria}",
				["lectura.color-de-las-heces.volvio"] =
					"el color de las heces estuvo fuera de lo esperable {noEsperables} y en la última "
					+ "ya salió {categoria}",

				// ------------------------------------------------- las clausulas que cuelgan

				["meta.ya-llego"] = "estás en {valor}, dentro de lo deseable",
				["meta.falta"] = "de {valor} a {objetivo}: te faltan {falta}",
				["meta.como-objetivo.ya-llego"] = "y ya estás dentro de lo deseable",
				["meta.como-objetivo.falta"] = "tu meta ahora es bajar a {objetivo}",

				["hdl.juega-a-favor"] = ", y tu HDL de {valor} juega a favor",
				["hdl.no-alcanza"] = ", y tu HDL de {valor} no alcanza para compensarlo",

				["anemia.ninguna"] = ", fuera de lo normal",
				["anemia.leve"] = ", que es anemia leve",
				["anemia.moderada"] = ", que es anemia moderada",
				["anemia.severa"] = ", que es anemia severa",

				["grado.leve"] = "leve",
				["grado.moderada"] = "moderada",
				["grado.severa"] = "severa",

				// ------------------------------------------------- a que ritmo viene cambiando

				["velocidad.por-ano"] = "{ritmo} por año",
				["velocidad.no-se-interpreta"] = "no se interpreta por velocidad",

				// ------------------------------------------------- el antigeno, ano por ano

				["ano.sin-comparacion"] = "{ano}: {valor}",
				["ano.con-comparacion"] = "{ano}: {valor} ({cambio} contra {anterior})",
				["banda.expresada"] = "{piso}–{techo} {unidad}: {cuantas}",

				// ------------------------------------------------- lo que falta medir

				["control.vencido"] =
					"{analito} es de {hace}, y conviene repetirlo cada {cada}",
				["control.al-dia"] = "{analito} es de {hace}: al día",
				["control.hace.anos"] = "hace {anios}",
				["control.hace.dias"] = "hace {dias}",
				["control.cada.anos"] = "{anios}",
				["control.cada.meses"] = "{meses}",

				// ------------------------------------------------- lo que esta bien

				["enRango.todos.uno"] = "Tus {enSuRango} valor está en su rango.",
				["enRango.todos.varios"] = "Tus {enSuRango} valores están en su rango.",
				["enRango.algunos.uno"] = "{enSuRango} de tus {analitos} valor está en su rango.",
				["enRango.algunos.varios"] = "{enSuRango} de tus {analitos} valores están en su rango.",

				// ------------------------------------------------- el dato que falta

				["pedidoDeSexo.marco"] =
					"Sin saber tu sexo no puedo interpretar {sinInterpretar} de tus {analitos} "
					+ "resultados: {porque}.",
				["pedidoDeSexo.porQue"] = "Es para comparar tus resultados contra los rangos correctos.",
				["pedidoDeSexo.rango.uno"] = "el rango de {cuales} es distinto según el sexo",
				["pedidoDeSexo.rango.varios"] = "los rangos de {cuales} son distintos según el sexo",
				["pedidoDeSexo.aplicabilidad.uno"] =
					"hay un examen —{cuales}— que quizás no te corresponda",
				["pedidoDeSexo.aplicabilidad.varios"] =
					"hay {cuantos} exámenes —{cuales}— que quizás no te correspondan",
			};
		}
	}
}
