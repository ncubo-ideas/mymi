import AppIntents
import Foundation

// LOS APP INTENTS DE GNOTHI, ADENTRO DE LA APP DE .NET.
//
// Este archivo es Swift y vive dentro de Gnothi.iOS a proposito: es lo unico de la app que no
// puede ser C#. App Intents usa property wrappers y result builders -- features del lenguaje
// Swift sin representacion en Objective-C -- asi que la intent TIENE que ser Swift. Lo que no
// tiene que ser es otra app.
//
// COMO ENTRA. armar-intents.sh lo compila con swiftc a una libreria estatica, el csproj la
// enlaza con ForceLoad, y el mismo script genera Metadata.appintents con
// appintentsmetadataprocessor y la mete al bundle. Ese ultimo paso es el que el build de .NET
// no hace, y es toda la razon por la que dotnet/macios#24863 dice que "no funciona".
//
// UNA SOLA INTENT, GENERICA, y es la distincion que encontro el companero (HALLAZGOS.md,
// hallazgo 7): los TIPOS son estaticos -- la metadata se sella al compilar -- pero las
// INSTANCIAS son dinamicas. Un modulo que llegue el ano que viene no puede agregar una intent,
// pero SI aparece en la foto que esta intent lee. La forma es fija; el ruteo, no.
//
// LEE LA FOTO LOCAL, no la del App Group. Gnothi.Movil/Shell.cs la escribe en la carpeta
// propia de la app (Library/Application Support/instantanea/) justamente para esto: dentro de
// una misma app no hace falta contenedor compartido, ni entitlement, ni portal de Apple.
enum FotoLocal {

    /// Tiene que coincidir con Shell.Fotografiar() del lado de .NET.
    static var ruta: URL {
        FileManager.default.urls(for: .applicationSupportDirectory, in: .userDomainMask)[0]
            .appendingPathComponent("instantanea")
            .appendingPathComponent("gnothi-instantanea.json")
    }

    enum Resultado {
        case sinFoto(String)      // el Host nunca escribio, o escribio en otro lado
        case ilegible(String)
        case foto(json: String, escritaEn: Date?)
    }

    static func leer() -> Resultado {
        let url = ruta
        guard let data = try? Data(contentsOf: url) else { return .sinFoto(url.path) }
        guard let raiz = (try? JSONSerialization.jsonObject(with: data)) as? [String: Any] else {
            return .ilegible(url.path)
        }
        var fecha: Date? = nil
        if let texto = raiz["escritaEn"] as? String { fecha = ISO8601DateFormatter().date(from: texto) }
        return .foto(json: String(decoding: data, as: UTF8.self), escritaEn: fecha)
    }

    /// El encuadre: de donde salio y CUANDO. Sin la fecha, un modelo presenta como de ahora
    /// algo de hace tres dias.
    static func encuadre(escritaEn: Date?) -> String {
        let cuando: String
        if let d = escritaEn {
            let f = DateFormatter()
            f.locale = Locale(identifier: "es_CR")
            f.dateStyle = .medium
            f.timeStyle = .short
            cuando = "La ultima vez que Mymi actualizo esto fue el \(f.string(from: d))."
        } else {
            cuando = "No se sabe cuando se actualizo por ultima vez."
        }
        return """
        Estos son los datos REALES de la persona que te esta preguntando, leidos por Mymi de \
        los registros que sus instituciones le entregaron y que viven en su propio telefono. No \
        vienen de ningun servidor.

        \(cuando) Si ella espera ver algo mas nuevo, decile que abra la app de Mymi.

        Contestale sobre SUS numeros, en su idioma, sin explicarle la estructura del json. En \
        prosa corrida, sin markdown: nada de tablas, pipes, asteriscos ni encabezados, porque \
        esto puede terminar leido en voz alta o mostrado como texto plano.
        """
    }
}

struct LoQueGnothiTieneIntent: AppIntent {

    static let title: LocalizedStringResource = "Lo que Mymi tiene"

    static let description = IntentDescription(
        "Devuelve los datos reales que Mymi tiene en este telefono -- facturas, examenes, lo que la persona haya autorizado -- con la fecha en que los actualizo."
    )

    static let openAppWhenRun = false

    func perform() async throws -> some IntentResult & ProvidesDialog & ReturnsValue<String> {
        let dicho: String
        switch FotoLocal.leer() {
        case .sinFoto(let ruta):
            // Distinguir "nunca corrio" de "corrio y no escribio" es lo que el log GNOTHI: hace
            // del otro lado: si dice "instantanea: N modulos" y esto sale, las rutas no
            // coinciden.
            dicho = "Mymi todavia no dejo ninguna foto. Abri la app una vez y volve a preguntar. Se busco en: \(ruta)"
        case .ilegible(let ruta):
            dicho = "Hay un archivo en \(ruta) pero no se pudo leer como json."
        case .foto(let json, let escritaEn):
            dicho = "\(FotoLocal.encuadre(escritaEn: escritaEn))\n\n\(json)"
        }
        return .result(value: dicho, dialog: IntentDialog(stringLiteral: dicho))
    }
}

struct GnothiShortcuts: AppShortcutsProvider {
    static var appShortcuts: [AppShortcut] {
        // El nombre de la app va en la frase porque Apple lo exige, y hoy es "Mymi", que es como
        // se llama el producto. Antes decia "Gnothi Host", para no chocar con el spike de Swift
        // llamado "Gnothi"; el nombre nuevo no choca con el spike y ademas se dice mejor en voz
        // alta: «Que tiene Mymi».
        AppShortcut(
            intent: LoQueGnothiTieneIntent(),
            phrases: [
                "Qué tiene \(.applicationName)",
                "Mis datos en \(.applicationName)",
                "Qué sabe \(.applicationName) de mí"
            ],
            shortTitle: "Lo que Mymi tiene",
            systemImageName: "externaldrive.badge.checkmark"
        )
    }
}
