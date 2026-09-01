import AppIntents

// LO QUE APORTA LA PUERTA, y solo la puerta.
//
// En la Puerta 1 el encuadre viaja en el handshake de MCP y el destino es un chat que
// renderiza markdown. Aca el destino es una ALERTA DE TEXTO PLANO de la app Atajos, y eso
// el modulo no tiene como saberlo: `Facturas.encuadre` dice que son los datos, no donde
// van a terminar dibujados.
//
// Es la misma propiedad que la R14 nombra del otro lado: `Dispatcher.Serve` no sabe quien
// lo llamo. Justamente por eso lo que depende del llamante lo tiene que poner la puerta.
enum Superficie {

    /// Lo que se le dice al modelo cuando la respuesta cae en la app Atajos.
    static let atajo = """
        Tu respuesta se va a mostrar en una alerta de texto plano del telefono, que NO \
        dibuja markdown. No uses tablas, ni pipes, ni asteriscos, ni encabezados: eso sale \
        en crudo y se lee peor. Escribi en prosa corta; si tenes que enumerar, usa lineas \
        sueltas. Se breve, que la pantalla es chica.
        """
}

// THE iOS GATE. This is the App Intents equivalent of what Lab.Caratula declared in
// gnothi_app_function_service.xml on Android -- one tool, described well enough that an
// assistant can decide to call it from the description alone.
//
// WHAT WE ARE MEASURING. On Android the equivalent open surface (App Actions / GET_THING)
// is ONE WAY: the assistant extracts parameters, launches the app, and there is no path
// back. It cannot speak our answer. App Intents claims it can. That claim is the spike.
struct TotalDeFacturasIntent: AppIntent {

    // Shown in Shortcuts and Spotlight. Keep it in the person's language: this string is
    // read by a person building a shortcut, not by a machine.
    static let title: LocalizedStringResource = "Total de facturas"

    // THE DESCRIPTION IS FOR THE MODEL, not for the UI, and it is the whole reason
    // Lab.Caratula had to move from AppFunctions v1 to v2 on Android -- v1 had no field
    // for it, so Gemini had nothing to route on. Here it exists from the start.
    static let description = IntentDescription(
        "Devuelve cuánto gastó la persona en total, leído de sus propias facturas guardadas en este dispositivo. No consulta a ninguna empresa."
    )

    // THE LINE THAT DECIDES THE WHOLE EXPERIMENT.
    //
    // false -> Siri answers in place; the person stays where they were.
    // true  -> iOS launches the app and we learn nothing new, because that is exactly the
    //          behaviour Android already gives us and that we called "flaco" in note 23.
    //
    // If this is left at its default the spike silently measures the wrong thing.
    static let openAppWhenRun = false

    // The result composes three protocols, and each one buys a different surface:
    //
    //   IntentResult    the shortcut can continue to a next step  (-> Ask Claude)
    //   ReturnsValue    the value is available to that next step
    //   ProvidesDialog  Siri SAYS it out loud when asked by voice
    //
    // ProvidesDialog is the one Android has no answer for.
    func perform() async throws -> some IntentResult & ProvidesDialog & ReturnsValue<String> {
        let hechos = Facturas.total()

        // value: what the next Shortcuts action receives.
        // dialog: what Siri speaks. Same content here; they are allowed to differ, and in
        // the real Host they probably should -- a model wants json, a person wants a sentence.
        return .result(value: hechos, dialog: IntentDialog(stringLiteral: hechos))
    }
}

// THE SECOND TOOL, and it exists to answer a different question than the first.
//
// The one above proves the channel works. This one is shaped for the Shortcuts chain
// -- Gnothi -> Ask Claude -- where the consumer is a model and not an ear. If both work,
// we know the same declaration serves the assistant AND the person's own wiring, which is
// the property that makes this worth building on top of the real Host.
struct FacturasComoDatosIntent: AppIntent {

    static let title: LocalizedStringResource = "Facturas (datos para una IA)"

    static let description = IntentDescription(
        "Devuelve las facturas de la persona en JSON, para encadenar hacia un modelo dentro de un atajo."
    )

    static let openAppWhenRun = false

    // No ProvidesDialog on purpose: nobody wants json read out loud. This one is a
    // building block, not an answer.
    //
    // DEVUELVE LOS HECHOS CON SU ENCUADRE, no el json pelado. Ver Facturas.encuadre: sin eso,
    // el modelo del otro lado del atajo lee una estructura de datos y ofrece escribir tipos.
    // Medido en el telefono del companero.
    func perform() async throws -> some IntentResult & ReturnsValue<String> {
        .result(value: Facturas.paraUnModelo(superficie: Superficie.atajo))
    }
}

// LA TERCERA FORMA — la persona pregunta, nosotros componemos.
//
// Las otras dos dejan al atajo con una mitad: `TotalDeFacturas` da una frase ya redactada
// (buena para el oido, cerrada para razonar encima) y `FacturasComoDatos` da los hechos
// (buenos para razonar, mudos sobre que se quiere saber).
//
// Esta toma la PREGUNTA de la persona y devuelve pregunta + marco + hechos, listo para
// entrar a un `Ask Claude` o a un `Use Model`. La persona escribe lo suyo y no tiene que
// saber armar un prompt.
//
// POR QUE UN PARAMETRO String SI PUEDE ACA. El companero midio que interpolar un String en
// una FRASE de Siri no compila -- «AppEntity and AppEnum are the only allowed types». Ese
// muro es de las frases habladas. Dentro de la app Atajos un parametro String es normal, y
// por eso esta intent NO declara frase: se usa arrastrandola a un atajo.
struct PreguntarSobreMisFacturasIntent: AppIntent {

    static let title: LocalizedStringResource = "Preguntar sobre mis facturas"

    static let description = IntentDescription(
        "Arma la pregunta de la persona junto con sus facturas, lista para pasarle a un modelo dentro de un atajo."
    )

    static let openAppWhenRun = false

    @Parameter(title: "Qué querés saber")
    var pregunta: String

    static var parameterSummary: some ParameterSummary {
        Summary("Preguntar \(\.$pregunta) sobre mis facturas")
    }

    func perform() async throws -> some IntentResult & ReturnsValue<String> {
        // El orden importa: primero el marco, despues los hechos, y la pregunta AL FINAL.
        // Un modelo pesa mas lo ultimo que lee, y lo ultimo tiene que ser lo que se le pide.
        //
        // Cuatro piezas y CADA UNA TIENE DUEÑO DISTINTO -- por eso vale la pena verlas juntas:
        //
        //   encuadre    el MODULO   que son estos datos      igual por las tres puertas
        //   superficie  la PUERTA   donde se va a mostrar    cambia con el llamante
        //   hechos      el MODULO   los numeros              igual por las tres puertas
        //   pregunta    la PERSONA  que quiere saber         la trae ella
        let armado = """
        \(Facturas.encuadre)

        \(Superficie.atajo)

        \(Facturas.totalComoDatos())

        La persona pregunta: \(pregunta)
        """
        return .result(value: armado)
    }
}
