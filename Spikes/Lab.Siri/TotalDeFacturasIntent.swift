import AppIntents

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
    func perform() async throws -> some IntentResult & ReturnsValue<String> {
        .result(value: Facturas.totalComoDatos())
    }
}
