import AppIntents

// THE FLOOR, AND ONLY THE FLOOR.
//
// Every phrase here MUST contain the app name -- Apple requires it to disambiguate against
// system commands and other apps. That is also this route's ceiling: a person thinks about
// her invoices, not about the app that holds them, so "cuánto gasté en Gnothi" is a sentence
// nobody says naturally. It proves the channel works; it does not prove anyone would use it.
//
// The route that does NOT need the app name is in FacturaEntity.swift: entities handed to
// Spotlight's semantic index. `BuscarFacturasIntent` deliberately declares no phrase at all
// down there, so if Siri reaches it, it is because the index matched the person's own words.
//
// On Android there was no floor either -- an app outside Gemini's curated vocabulary is
// simply invisible, with no phrase escape hatch. Here at least the floor exists.
struct GnothiShortcuts: AppShortcutsProvider {

    static var appShortcuts: [AppShortcut] {

        AppShortcut(
            intent: TotalDeFacturasIntent(),

            // THE TRAP THAT COSTS AN AFTERNOON: drop \(.applicationName) from a phrase and
            // the project still COMPILES -- the phrase just never fires at runtime, with no
            // error anywhere. It is the iOS cousin of the <id> ≠ <functionId> mismatch that
            // cost us an afternoon on Android.
            //
            // .applicationName resolves to the target's Display Name, so whatever that says
            // is what the person has to pronounce. Set it to "Gnothi".
            //
            // Five phrasings on purpose: this measures how much slack Siri gives us, the
            // same way trying utils / notes / email as schema categories measured Gemini's.
            phrases: [
                "Cuánto gasté en \(.applicationName)",
                "Cuánto he gastado en \(.applicationName)",
                "Mis facturas en \(.applicationName)",
                "Total de facturas en \(.applicationName)",
                "Preguntale a \(.applicationName) por mis facturas"
            ],

            shortTitle: "Total de facturas",
            systemImageName: "doc.text.magnifyingglass"
        )

        // WHAT WE WANTED HERE, AND WHAT iOS ALLOWS. Worth writing down: it is a finding,
        // not a detail.
        //
        // The intent below takes free text, and the plan was to interpolate it into the
        // phrase -- "Buscá <texto> en Gnothi" -- to measure whether Siri pulls a value out of
        // the sentence instead of just matching a fixed string.
        //
        // It does NOT compile: "AppEntity and AppEnum are the only allowed types for texto".
        // A phrase can only carry a parameter drawn from a CLOSED set the app declares, so
        // free text spoken into a phrase does not exist on iOS. That is a wall, not a mistake
        // in how we wrote it, and it belongs in the report.
        //
        // The phrase therefore stays parameterless: it fires the intent and Siri then ASKS
        // for the value. Measuring parsing would mean typing the parameter as FacturaEntity
        // and letting FacturaQuery.entities(matching:) resolve it -- a different experiment,
        // and one that overlaps the ceiling this spike already measures through the index.
        AppShortcut(
            intent: BuscarFacturasIntent(),
            phrases: [
                "Buscá algo en mis facturas de \(.applicationName)",
                "Buscar en las facturas de \(.applicationName)"
            ],
            shortTitle: "Buscar facturas",
            systemImageName: "magnifyingglass"
        )

        // The building block for the Shortcuts chain -- Gnothi -> Ask Claude. One phrase,
        // because nobody is meant to say this one out loud: it is meant to be dragged into
        // a shortcut the person builds.
        AppShortcut(
            intent: FacturasComoDatosIntent(),
            phrases: [
                "Datos de facturas de \(.applicationName)"
            ],
            shortTitle: "Facturas en datos",
            systemImageName: "curlybraces"
        )

        // LA QUE COMPONE PREGUNTA + HECHOS. Lleva una frase generica y SIN el parametro
        // adentro: interpolar un String en una frase no compila -- «AppEntity and AppEnum are
        // the only allowed types»-- asi que la frase dispara la intent y Siri pide la pregunta
        // aparte. Dentro de Atajos el parametro se escribe normal, que es su uso principal.
        AppShortcut(
            intent: PreguntarSobreMisFacturasIntent(),
            phrases: [
                "Preguntar sobre mis facturas en \(.applicationName)"
            ],
            shortTitle: "Preguntar sobre mis facturas",
            systemImageName: "text.bubble"
        )

        // ─────────────────────────────────────────────────────────────────────────────────
        // EXAMENES. Las mismas cuatro rutas, espejadas, para que los resultados se puedan
        // comparar con los de facturas sin discutir si la diferencia fue como escribimos la
        // intent. Ver ExamenesIntents.swift.
        //
        // CUIDADO AL AGREGAR MAS: Apple admite hasta 10 AppShortcut por app. Con estas van 8.
        // Al pasarse, el sistema no da error -- deja de registrar las que sobran, y la que
        // desaparece no es necesariamente la ultima.

        AppShortcut(
            intent: EstadoDeExamenesIntent(),

            // Cinco frases, como en el total de facturas, y por la misma razon: mide cuanta
            // holgura da Siri. Notar que ninguna dice "examenes de sangre" -- la persona
            // pregunta por el resultado, no por el procedimiento.
            phrases: [
                "Cómo salieron mis exámenes en \(.applicationName)",
                "Cómo van mis exámenes en \(.applicationName)",
                "Mis exámenes en \(.applicationName)",
                "Qué dicen mis exámenes en \(.applicationName)",
                "Preguntale a \(.applicationName) por mis exámenes"
            ],

            shortTitle: "Cómo salieron",
            systemImageName: "heart.text.square"
        )

        AppShortcut(
            intent: BuscarExamenesIntent(),
            phrases: [
                "Buscá algo en mis exámenes de \(.applicationName)",
                "Buscar en los exámenes de \(.applicationName)"
            ],
            shortTitle: "Buscar exámenes",
            systemImageName: "magnifyingglass.circle"
        )

        AppShortcut(
            intent: ExamenesComoDatosIntent(),
            phrases: [
                "Datos de exámenes de \(.applicationName)"
            ],
            shortTitle: "Exámenes en datos",
            systemImageName: "curlybraces.square"
        )

        AppShortcut(
            intent: PreguntarSobreMisExamenesIntent(),
            phrases: [
                "Preguntar sobre mis exámenes en \(.applicationName)"
            ],
            shortTitle: "Preguntar sobre mis exámenes",
            systemImageName: "text.bubble.fill"
        )
    }

    // NOTE: AppShortcutsProvider also exposes `shortcutTileColor` for the tile in the
    // Shortcuts gallery. Deliberately NOT set -- the enum's case names have moved between
    // SDK versions, and a spike that fails to compile over a cosmetic property answers
    // nothing. Add it once the real gate is built.
}
