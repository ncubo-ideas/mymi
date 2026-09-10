import AppIntents

// LAS CUATRO RUTAS, ESPEJADAS. Son las mismas cuatro que las facturas ya midieron, y estan
// espejadas a proposito: si una anda con facturas y no con examenes, la diferencia esta en el
// dato y no en como escribimos la intent. Eso es lo que hace comparables los dos resultados.
//
//   voz, nombrando la app        EstadoDeExamenesIntent        <-> TotalDeFacturasIntent
//   el indice, sin frase         BuscarExamenesIntent          <-> BuscarFacturasIntent
//   un modelo en cadena          ExamenesComoDatosIntent       <-> FacturasComoDatosIntent
//   la persona preguntando       PreguntarSobreMisExamenesIntent <-> PreguntarSobreMisFacturas

// LA DEL PISO. Contesta en voz alta, y contesta lo que importa: que pide algo y que no.
struct EstadoDeExamenesIntent: AppIntent {

    static let title: LocalizedStringResource = "Cómo salieron mis exámenes"

    static let description = IntentDescription(
        "Dice cuáles de los exámenes de la persona piden atención, según el veredicto que ya calculó su expediente."
    )

    // Contestar, no abrir la app. Si abre la app, el canal no sirve para lo que se quiere.
    static let openAppWhenRun = false

    func perform() async throws -> some IntentResult & ProvidesDialog & ReturnsValue<String> {
        let dicho = Examenes.estado()
        return .result(value: dicho, dialog: IntentDialog(stringLiteral: dicho))
    }
}

// LA DEL TECHO, Y NO DECLARA NINGUNA FRASE -- igual que BuscarFacturasIntent.
//
// Si Siri llega aca, es porque el indice semantico macheo las palabras de la persona con
// nuestras entidades. Es la unica de las cuatro que puede probar algo sin que nadie nombre la
// app, y por eso el silencio en `phrases` es deliberado y no un olvido.
struct BuscarExamenesIntent: AppIntent {

    static let title: LocalizedStringResource = "Buscar en mis exámenes"

    static let description = IntentDescription(
        "Busca entre los exámenes de la persona por analito o por lo que dice su veredicto, y responde con lo que encontró."
    )

    static let openAppWhenRun = false

    @Parameter(title: "Qué buscar")
    var texto: String

    static var parameterSummary: some ParameterSummary {
        Summary("Buscar \(\.$texto) en mis exámenes")
    }

    func perform() async throws -> some IntentResult & ProvidesDialog & ReturnsValue<String> {
        let encontrados = try await ExamenQuery().entities(matching: texto)

        let respuesta: String
        if encontrados.isEmpty {
            // Una respuesta vacia honesta, no una disculpa. El modulo dice lo que sabe.
            respuesta = "No hay exámenes que coincidan con «\(texto)»."
        } else {
            respuesta = encontrados
                .map { "\($0.analito): \($0.valorLegible), \($0.veredicto)" }
                .joined(separator: ". ")
        }

        return .result(value: respuesta, dialog: IntentDialog(stringLiteral: respuesta))
    }
}

// LA PIEZA PARA LA CADENA. Devuelve los hechos CON SU ENCUADRE, nunca el json pelado.
//
// La leccion es de las facturas: pasandole json pelado, Claude contesto «the math checks out...
// what do you want done with it?» y ofrecio escribir tipos. Con examenes el riesgo no es que
// conteste al lado, es que DIAGNOSTIQUE -- y sin la linea base que el expediente si tiene.
// Ver Examenes.encuadre: le dice explicitamente que el veredicto ya viene hecho.
struct ExamenesComoDatosIntent: AppIntent {

    static let title: LocalizedStringResource = "Exámenes (datos para una IA)"

    static let description = IntentDescription(
        "Entrega los exámenes de la persona con su veredicto y su encuadre, para pasarle a un modelo dentro de un atajo."
    )

    static let openAppWhenRun = false

    // Sin ProvidesDialog a proposito: nadie quiere que le lean un json en voz alta.
    func perform() async throws -> some IntentResult & ReturnsValue<String> {
        .result(value: Examenes.paraUnModelo())
    }
}

// LA QUE COMPONE PREGUNTA + HECHOS. El gemelo de PreguntarSobreMisFacturasIntent.
//
// Un @Parameter String se puede aca por lo mismo que alla: el muro de «AppEntity and AppEnum
// are the only allowed types» es de las FRASES habladas, no de los parametros dentro de Atajos.
struct PreguntarSobreMisExamenesIntent: AppIntent {

    static let title: LocalizedStringResource = "Preguntar sobre mis exámenes"

    static let description = IntentDescription(
        "Arma la pregunta de la persona junto con sus exámenes y su encuadre, lista para pasarle a un modelo dentro de un atajo."
    )

    static let openAppWhenRun = false

    @Parameter(title: "Qué querés saber")
    var pregunta: String

    static var parameterSummary: some ParameterSummary {
        Summary("Preguntar \(\.$pregunta) sobre mis exámenes")
    }

    func perform() async throws -> some IntentResult & ReturnsValue<String> {
        // El orden importa: marco, hechos, y la pregunta AL FINAL. Un modelo pesa mas lo
        // ultimo que lee, y lo ultimo tiene que ser lo que se le pide.
        let armado = """
        \(Examenes.encuadre)

        \(Examenes.comoDatos())

        La persona pregunta: \(pregunta)
        """
        return .result(value: armado)
    }
}
