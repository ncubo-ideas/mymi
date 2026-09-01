import AppIntents

// LA INTENT MAS CHICA QUE PUEDE EXISTIR, y esta chica a proposito.
//
// Este archivo NO prueba nada de Gnothi. Prueba una sola cosa: si una intent escrita en Swift,
// compilada a una libreria estatica y ENLAZADA DENTRO DE UNA APP DE .NET, la ve la app Atajos.
// Todo lo que no sea eso -- datos, journals, el Host -- esta afuera para que un fallo diga que
// fallo.
//
// SIN AppIntentsPackage. El que reporto dotnet/macios#24863 lo uso, y hay reportes de que
// declararlo duplica la metadata y el SO no puede resolver que intent usar. Una variable menos.
//
// El nombre de la app va en la frase porque Apple lo exige para desambiguar: sin
// \(.applicationName) compila igual y NUNCA dispara. Ya nos costo una tarde.
struct ProbarDesdeNetIntent: AppIntent {

    static let title: LocalizedStringResource = "Probar desde .NET"

    static let description = IntentDescription(
        "Contesta un texto fijo. Si esto aparece en Atajos, la metadata de App Intents entro al bundle de una app de .NET."
    )

    static let openAppWhenRun = false

    func perform() async throws -> some IntentResult & ProvidesDialog & ReturnsValue<String> {
        // Un texto que no se puede confundir con nada del sistema, para que si Siri lo dice no
        // haya duda de que salio de aca.
        let dicho = "Hola desde Swift, adentro de una app de .NET. La metadata entro."
        return .result(value: dicho, dialog: IntentDialog(stringLiteral: dicho))
    }
}

struct GnothiIntentsShortcuts: AppShortcutsProvider {
    static var appShortcuts: [AppShortcut] {
        AppShortcut(
            intent: ProbarDesdeNetIntent(),
            phrases: [
                "Probar \(.applicationName)",
                "Qué dice \(.applicationName)"
            ],
            shortTitle: "Probar desde .NET",
            systemImageName: "hammer"
        )
    }
}
