import AppIntents
import Foundation

// LA CARATULA LEE LO QUE EL HOST DE VERDAD LE DEJO.
//
// TODO LO DEMAS DE ESTE SPIKE TIENE LOS DATOS QUEMADOS, y eso fue correcto: la pregunta era si
// iOS deja que un asistente alcance una tool nuestra, y meter el Host de .NET en el medio
// habria mezclado esa pregunta con otra mucho mas grande. Ya sabemos que la alcanza.
//
// ESTE ARCHIVO CONTESTA LA SIGUIENTE: ¿puede contestar con las facturas DE VERDAD de la
// persona, las que su modulo leyo de su journal?
//
// POR QUE NO SE LLAMAN DIRECTO. Los App Intents solo existen en Swift -- son property wrappers
// y result builders, features del lenguaje, sin representacion en Objective-C, que es por donde
// pasan las bindings de .NET. Y el issue dotnet/macios#24863 sigue abierto sin respuesta.
// Cruzarlos costaria NativeAOT, y NativeAOT prohibe cargar modulos en runtime.
//
// ASI QUE NO SE LLAMAN: SE DEJAN UN ARCHIVO.
//
//     Gnothi.iOS (.NET) corre  ->  escribe la foto en el contenedor compartido
//                                            |
//     Siri: "que dice Gnothi de verdad"      |
//                                            v
//                     esta intent (Swift) ->  la lee  ->  contesta
//
// Funciona porque las dos apps son NUESTRAS: App Groups exige el mismo Team ID. Es tambien la
// razon por la que esto NO sirve para que una institucion entregue su propia caratula.
//
// EL PRECIO: la respuesta sale de una foto, no en vivo. Pero eso ya era asi -- el journal solo
// avanza cuando la app corre, y las instrucciones del modulo ya dicen «si el total no cambio,
// pedile que abra su app y apriete Refresh».
enum Instantanea {

    /// Tiene que coincidir con Entitlements.plist de Gnothi.iOS y con el del portal de Apple.
    ///
    /// EN LA MAC LLEVA EL PREFIJO DEL EQUIPO. macOS identifica los Group Containers como
    /// «TEAMID.group.…»; con el string pelado, containerURL(...) no falla: devuelve una carpeta
    /// propia que ninguna otra app comparte, y el sintoma seria «sin foto» sin explicacion. El
    /// TEAMID es el DEVELOPMENT_TEAM del pbxproj y el mismo de Gnothi.mac.entitlements.
    #if os(macOS)
    static let grupo = "W5SWU29W6D.group.com.ncubo.gnothi"
    #else
    static let grupo = "group.com.ncubo.gnothi"
    #endif

    /// Tiene que coincidir con Instantanea.NombreDelArchivo del lado de .NET.
    static let archivo = "gnothi-instantanea.json"

    /// LO QUE SE ENCONTRO, y los casos de "no hay" se distinguen A PROPOSITO.
    ///
    /// Un spike que contesta "no hay datos" sin decir POR QUE no ensena nada: no se sabe si
    /// falto aprovisionar el App Group, si la app del Host nunca corrio, o si la persona apago
    /// el modulo. Son tres arreglos distintos.
    enum Resultado {
        case sinContenedor          // el App Group no esta aprovisionado
        case sinFoto(String)        // el contenedor existe pero el Host nunca escribio
        case ilegible(String)       // hay archivo y no se pudo parsear
        case foto(json: String, escritaEn: Date?)
    }

    static func leer() -> Resultado {
        guard let carpeta = FileManager.default
            .containerURL(forSecurityApplicationGroupIdentifier: grupo) else {
            return .sinContenedor
        }

        let url = carpeta.appendingPathComponent(archivo)
        guard let data = try? Data(contentsOf: url) else {
            return .sinFoto(carpeta.path)
        }

        guard let raiz = (try? JSONSerialization.jsonObject(with: data)) as? [String: Any] else {
            return .ilegible(url.path)
        }

        var fecha: Date? = nil
        if let texto = raiz["escritaEn"] as? String {
            // El Host escribe DateTimeOffset con fracciones de segundo
            // («2026-09-21T21:35:45.03653+00:00»), y el formateador por omision NO las admite:
            // devolvia nil y la foto salia «sin fecha» aunque la fecha estuviera ahi. Se prueban
            // las dos formas porque el formateador con fracciones rechaza un texto sin ellas.
            let conFraccion = ISO8601DateFormatter()
            conFraccion.formatOptions = [.withInternetDateTime, .withFractionalSeconds]
            fecha = conFraccion.date(from: texto) ?? ISO8601DateFormatter().date(from: texto)
        }

        return .foto(json: String(decoding: data, as: UTF8.self), escritaEn: fecha)
    }

    /// El encuadre para esta foto. Dice de donde salio Y CUANDO, que es lo que la diferencia
    /// del dato quemado: un modelo que no sabe la fecha presenta como de ahora algo de hace
    /// tres dias.
    static func encuadre(escritaEn: Date?) -> String {
        let cuando: String
        if let d = escritaEn {
            let f = DateFormatter()
            f.locale = Locale(identifier: "es_CR")
            f.dateStyle = .medium
            f.timeStyle = .short
            cuando = "La ultima vez que la app de Gnothi corrio y actualizo esto fue el \(f.string(from: d))."
        } else {
            cuando = "No se sabe cuando se actualizo por ultima vez."
        }

        return """
        Estos son los datos REALES de la persona que te esta preguntando, leidos por Gnothi de \
        los registros que sus instituciones le entregaron y que viven en su propio telefono. No \
        vienen de ningun servidor.

        \(cuando) Si ella espera ver algo mas nuevo, decile que abra la app de Gnothi.

        Contestale sobre SUS numeros, en su idioma, sin explicarle la estructura del json. \
        Contesta en prosa corrida, sin markdown: nada de tablas, pipes, asteriscos ni \
        encabezados, porque esto puede terminar leido en voz alta o mostrado como texto plano.
        """
    }
}

// LA PRUEBA. Una intent que contesta con lo que haya de verdad, y que cuando no hay dice
// EXACTAMENTE que falta.
struct HechosRealesIntent: AppIntent {

    static let title: LocalizedStringResource = "Lo que Gnothi tiene de verdad"

    static let description = IntentDescription(
        "Devuelve los datos reales que el Host de Gnothi dejo en este telefono, con la fecha en que los actualizo."
    )

    static let openAppWhenRun = false

    func perform() async throws -> some IntentResult & ProvidesDialog & ReturnsValue<String> {
        let dicho: String

        switch Instantanea.leer() {
        case .sinContenedor:
            dicho = """
            No hay contenedor compartido. El App Group '\(Instantanea.grupo)' no esta \
            aprovisionado: hay que habilitarlo en el portal de Apple para ESTA app y para la \
            del Host, y volver a firmar las dos.
            """

        case .sinFoto(let carpeta):
            dicho = """
            El contenedor existe pero no hay ninguna foto todavia. Abri la app del Host de \
            Gnothi una vez y volve a preguntar. Buscada en: \(carpeta)
            """

        case .ilegible(let ruta):
            dicho = "Hay un archivo en \(ruta) pero no se pudo leer como json."

        case .foto(let json, let escritaEn):
            dicho = "\(Instantanea.encuadre(escritaEn: escritaEn))\n\n\(json)"
        }

        return .result(value: dicho, dialog: IntentDialog(stringLiteral: dicho))
    }
}
