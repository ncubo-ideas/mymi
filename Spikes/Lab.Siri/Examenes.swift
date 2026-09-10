import Foundation

// EL GEMELO DE Facturas.swift, Y ESTA VEZ CON EL NEGOCIO DE VERDAD.
//
// Facturas midio si un asistente puede alcanzar un dato nuestro. Sirvio, pero una factura es
// un dato complaciente: plano, sin veredicto, y sin consecuencia si el asistente lo lee mal.
// Examenes es el negocio real de Gnothi, y trae tres cosas que las facturas no tienen:
//
//   UN VEREDICTO YA CALCULADO   el dominio no entrega "5.2 ng/mL", entrega "paso de 3.0 a 5.2
//                               en un ano, que es demasiado para el tiempo transcurrido".
//                               Ese juicio es de las reglas del negocio, no del asistente.
//
//   UNA SEVERIDAD               informativo / vigilar / consulte pronto / urgente. Es lo que
//                               ordena la carátula, y es lo que un modelo NO debe reinterpretar.
//
//   DATO DE SALUD               ponerlo en el indice de Spotlight no es lo mismo que poner una
//                               compra de supermercado. Ver la nota al final de este archivo.
//
// LOS NUMEROS SON EL GUION SEMBRADO DE Examenes, no inventados: salen de
// Examenes/App/Examenes.Consola/Muestras.cs, el expediente de Sonia. Se toma la ULTIMA
// medicion de cada analito, que es lo que la carátula muestra.
//
// Las fechas son literales por la misma razon que alla: un guion con fechas moviles no es
// reproducible.
enum Examenes {

    /// Los cinco analitos del expediente, cada uno como algo que el sistema puede indexar.
    ///
    /// Existen como entidades separadas y no como un resumen porque la ruta semantica necesita
    /// algo que MACHEAR: «como salio mi colesterol» solo puede funcionar si el colesterol es
    /// una cosa que el indice conoce.
    static func todos() -> [ExamenEntity] {
        [
            // EL QUE CUENTA LA HISTORIA. Sin la linea base de 3.0 -> 3.1 -> 3.0 el 5.2 es un
            // numero un poco arriba del techo; con ella es un salto sobre alguien que venia
            // planchado. El veredicto ya lleva esa lectura adentro.
            examen("e1", "Antígeno prostático", 5.2, "ng/mL", "2026-08-21",
                   "consulte pronto",
                   "pasó de 3,0 a 5,2 ng/mL en un año, que es demasiado para el tiempo transcurrido"),

            examen("e2", "Colesterol total", 280, "mg/dL", "2026-08-21",
                   "vigilar",
                   "subió de 200 a 280 mg/dL en un año, por encima de lo deseable"),

            examen("e3", "Hemoglobina", 14.2, "g/dL", "2026-08-21",
                   "informativo",
                   "se mantiene en 14,2 g/dL, dentro de lo normal"),

            // EL CONTRASTE DE ALVARO, y por eso esta aca: la misma forma de magnitud que la
            // orina, distinta conducta, y la diferencia vive en el analito.
            examenBinario("e4", "Sangre oculta en heces", "positivo", "2026-08-21",
                          "consulte pronto",
                          "volvió a salir positiva: ya se había repetido en 2025 y tambien dio positivo"),

            examenBinario("e5", "Examen de orina", "normal", "2026-08-21",
                          "informativo",
                          "color ámbar, sin sangre y sin nitritos"),
        ]
    }

    /// LO QUE SIRI DICE EN VOZ ALTA. Ordenado por severidad y no por fecha: si algo pide
    /// atencion, va primero. Es la misma regla con la que la carátula ordena el cuadro.
    ///
    /// No dice "tenes 5 examenes": un conteo no le sirve a nadie. Dice que pide algo y que no.
    static func estado() -> String {
        let e = todos()
        let piden = e.filter { $0.severidad != "informativo" }

        if piden.isEmpty {
            return "Tus \(e.count) exámenes están dentro de lo normal."
        }

        // Enumeracion en castellano: comas y una sola "y" antes del ultimo. Un
        // joined(separator: " y ") suena a robot -- y esto lo lee Siri en voz alta.
        let nombres = piden.map { $0.analito }
        let cuales: String
        if nombres.count == 1 {
            cuales = nombres[0]
        } else {
            cuales = nombres.dropLast().joined(separator: ", ") + " y " + nombres[nombres.count - 1]
        }
        return "De tus \(e.count) exámenes, \(piden.count) piden algo: \(cuales). "
             + "El resto está dentro de lo normal."
    }

    /// EL ENCUADRE, Y NO ES EL DE FACTURAS. La diferencia es el punto.
    ///
    /// Con facturas el marco solo tiene que decir de quien son los datos. Aca tiene que decir
    /// ademas algo mas dificil: QUE EL VEREDICTO YA VIENE HECHO Y NO ES DEL MODELO.
    ///
    /// El dominio de Examenes nunca diagnostica -- entrega un `ParaSonia` y una `Severidad`
    /// calculados por sus reglas, con la serie completa detras. Un modelo que recibe "5.2
    /// ng/mL" y no sabe eso va a opinar, y va a opinar sin la linea base. Eso es peor que no
    /// contestar.
    ///
    /// Asi que el marco pide explicitamente lo contrario de lo que un asistente hace por
    /// defecto: no interpretar, no diagnosticar, y no reemplazar a quien la atiende.
    static var encuadre: String {
        """
        Estos son exámenes de laboratorio de la persona que te está preguntando, leídos de su \
        propio expediente guardado en su teléfono. No vienen de ningún servidor ni de ninguna \
        empresa.

        Cada examen ya trae un veredicto y una severidad calculados por las reglas de su \
        expediente, con toda la serie histórica detrás. ESE JUICIO NO ES TUYO: no lo \
        reinterpretes, no diagnostiques, y no le sugieras tratamientos. Ayudale a entender lo \
        que su expediente ya dice, en su idioma, y si algo pide atención decile que lo vea con \
        quien la atiende.

        Contestá en prosa corrida, sin markdown: nada de tablas, viñetas, asteriscos ni \
        encabezados. Tu respuesta puede terminar leída en voz alta o mostrada como texto \
        plano, donde un `|---|` no se ve como una tabla sino como basura.
        """
    }

    /// Los hechos con su marco. Es lo que va hacia un modelo dentro de un atajo.
    static func paraUnModelo() -> String {
        "\(encuadre)\n\n\(comoDatos())"
    }

    /// Los mismos hechos como datos. Lleva la severidad y el veredicto, no solo el numero:
    /// un modelo que recibe el valor pelado no tiene con que evitar opinar.
    static func comoDatos() -> String {
        let detalle = todos()
            .map {
                "{\"analito\":\"\($0.analito)\",\"valor\":\"\($0.valorLegible)\","
              + "\"severidad\":\"\($0.severidad)\",\"veredicto\":\"\($0.veredicto)\"}"
            }
            .joined(separator: ",")
        return """
        {"hayDatos":true,"cantidadDeExamenes":\(todos().count),\
        "pidenAtencion":\(todos().filter { $0.severidad != "informativo" }.count),\
        "examenes":[\(detalle)]}
        """
    }

    private static func examen(
        _ id: String, _ analito: String, _ valor: Double, _ unidad: String,
        _ fecha: String, _ severidad: String, _ veredicto: String
    ) -> ExamenEntity {
        var e = ExamenEntity()
        e.id = id
        e.analito = analito
        e.veredicto = veredicto
        e.valorLegible = "\(ExamenEntity.numero(valor)) \(unidad)"
        e.severidad = severidad
        e.fecha = ExamenEntity.dia(fecha)
        return e
    }

    private static func examenBinario(
        _ id: String, _ analito: String, _ resultado: String,
        _ fecha: String, _ severidad: String, _ veredicto: String
    ) -> ExamenEntity {
        var e = ExamenEntity()
        e.id = id
        e.analito = analito
        e.veredicto = veredicto
        e.valorLegible = resultado
        e.severidad = severidad
        e.fecha = ExamenEntity.dia(fecha)
        return e
    }
}

// SOBRE PONER SALUD EN EL INDICE DEL SISTEMA -- y esto es una decision, no un detalle.
//
// `indexAppEntities` entrega estas entidades a Spotlight, que es un indice del SISTEMA. Para
// una compra de supermercado da igual. Para «Antígeno prostático 5,2 -- consulte pronto» no:
// queda buscable por cualquiera que agarre el telefono desbloqueado.
//
// En el spike se indexa igual, porque medir el techo es el punto y sin indice no hay techo que
// medir. Pero en el producto esto NO se hereda: la decision de que un examen entre al indice
// del sistema es de la persona, no nuestra, y probablemente por analito y no en bloque.
//
// Queda escrito aca para que nadie copie este archivo pensando que la decision ya se tomo.
