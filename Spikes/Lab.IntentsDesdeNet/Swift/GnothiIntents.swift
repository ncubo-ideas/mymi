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
        // LO QUE 6b PRUEBA, Y ES MAS QUE "HOLA": que la intent de Swift LEE lo que el C# de la
        // MISMA app escribio al arrancar. Ese es exactamente el camino que Gnothi.iOS usaria
        // -- el Host deja la foto, la intent la lee -- pero sin App Group, porque ya no son dos
        // apps: es un archivo en la carpeta propia.
        //
        // Si esto sale con el texto del C#, el puente .NET -> Swift dentro de UNA app esta
        // probado, y lo que queda para Gnothi es cambiar "desde-net.txt" por la foto.
        let carpeta = FileManager.default.urls(for: .applicationSupportDirectory, in: .userDomainMask)[0]
        let archivo = carpeta.appendingPathComponent("desde-net.txt")
        let desdeNet = (try? String(contentsOf: archivo, encoding: .utf8))
            ?? "(NO encontre desde-net.txt en \(carpeta.path): el C# no lo escribio, o Swift busca en otra carpeta)"

        // Un texto que no se puede confundir con nada del sistema, para que si Siri lo dice no
        // haya duda de que salio de aca.
        let dicho = "Hola desde Swift, adentro de una app de .NET. La metadata entro. Y el C# me dejo escrito: \(desdeNet)"
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
