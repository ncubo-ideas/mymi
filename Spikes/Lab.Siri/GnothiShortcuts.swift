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

        // WITH A PARAMETER. Tests something the total cannot: whether Siri pulls a value out
        // of the sentence and hands it to us. If this works, the assistant is not just
        // matching a fixed string -- it is parsing.
        AppShortcut(
            intent: BuscarFacturasIntent(),
            phrases: [
                "Buscá \(\.$texto) en \(.applicationName)",
                "Buscar \(\.$texto) en mis facturas de \(.applicationName)"
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
    }

    // NOTE: AppShortcutsProvider also exposes `shortcutTileColor` for the tile in the
    // Shortcuts gallery. Deliberately NOT set -- the enum's case names have moved between
    // SDK versions, and a spike that fails to compile over a cosmetic property answers
    // nothing. Add it once the real gate is built.
}
