import AppIntents
import CoreSpotlight
import Foundation

// UN EXAMEN, como algo que el sistema puede indexar. El gemelo de FacturaEntity, y esta vez
// con una pregunta mas precisa que hacerle a iOS.
//
// LO QUE FacturaEntity YA MIDIO: de una entidad nuestra, Spotlight indexa `displayName` y
// nada mas. Buscar «gasolinera» encuentra la factura; buscar «combustible» no encuentra nada,
// aunque esa palabra este impresa en el subtitulo del propio resultado.
//
// POR QUE EXAMENES PUEDE CAMBIAR LA CONCLUSION, y no solo repetirla. Con facturas, la unica
// puerta que existe es el nombre del comercio -- y el nombre del comercio es justamente lo que
// uno NO recuerda. Con examenes la unica puerta es el nombre del analito, y eso SI es lo que
// la persona diria: «colesterol», «hemoglobina». La misma limitacion del sistema puede ser
// fatal en un negocio y aceptable en el otro.
//
// LA PRUEBA QUE LO DECIDE, y esta armada a proposito para ser el par de gasolinera/combustible:
//
//     buscar «colesterol»        esta en `analito`   -> displayName          esperado: SALE
//     buscar «subió» / «vigilar» esta en `veredicto` -> contentDescription   esperado: NO SALE
//
// Si sale el primero y no el segundo, el hallazgo se confirma en una segunda entidad y deja de
// ser una rareza de como escribimos las facturas.
struct ExamenEntity: AppEntity, IndexedEntity {

    static let typeDisplayRepresentation: TypeDisplayRepresentation = "Examen"

    var id: String = ""

    // LA UNICA PUERTA CONOCIDA. Va el nombre del analito y no "examen de sangre": lo que la
    // persona dice es el analito.
    @Property(indexingKey: \.displayName)
    var analito: String

    // El veredicto que ya calculo el expediente. Se mapea a contentDescription sabiendo que
    // NO se va a poder buscar -- se mapea igual porque es lo que se muestra, y porque dejarlo
    // fuera haria que el resultado en pantalla fuera un titulo pelado.
    @Property(indexingKey: \.contentDescription)
    var veredicto: String

    // Sin indexar, y no por olvido: Spotlight no tiene atributo que signifique "severidad", y
    // forzar uno seria inventar semantica. Viaja en el JSON, que es lo que lee un modelo.
    var severidad: String = "informativo"

    // El valor ya formateado con su unidad. Se guarda legible y no como Double porque nunca se
    // calcula nada con el aca -- se muestra y se le pasa a un modelo.
    var valorLegible: String = ""

    var fecha: Date = .now

    // LO QUE SIRI LEE. El titulo es el analito porque es lo que se busca; el subtitulo lleva el
    // valor y la severidad, que es lo que decide si la persona tiene que hacer algo.
    var displayRepresentation: DisplayRepresentation {
        DisplayRepresentation(
            title: "\(analito)",
            subtitle: "\(valorLegible) · \(severidad)"
        )
    }

    static var defaultQuery = ExamenQuery()

    static func numero(_ v: Double) -> String {
        let f = NumberFormatter()
        f.numberStyle = .decimal
        f.locale = Locale(identifier: "es_CR")
        f.minimumFractionDigits = v == v.rounded() ? 0 : 1
        return f.string(from: NSNumber(value: v)) ?? "\(v)"
    }

    static func dia(_ iso: String) -> Date {
        let f = DateFormatter()
        f.dateFormat = "yyyy-MM-dd"
        f.locale = Locale(identifier: "es_CR")
        return f.date(from: iso) ?? Date()
    }
}

// COMO EL SISTEMA NOS PIDE EXAMENES.
//
// `EntityStringQuery` y no `EntityQuery`, por lo mismo que en FacturaQuery: agrega
// entities(matching:), que es como Spotlight y Siri nos pasan un pedazo de lo que dijo la
// persona y preguntan «¿esto es tuyo?».
//
// OJO CON LEER DE MAS EN ESTA CLASE. Aca el match busca en analito Y en veredicto, o sea que
// nuestra implementacion SI encuentra «vigilar». Que la busqueda de Spotlight no lo encuentre
// no es culpa de este codigo -- es que Spotlight machea contra su indice, no llamandonos. La
// diferencia entre lo que esta clase puede y lo que el sistema usa es, precisamente, el techo.
struct ExamenQuery: EntityStringQuery {

    func entities(for identifiers: [String]) async throws -> [ExamenEntity] {
        Examenes.todos().filter { identifiers.contains($0.id) }
    }

    func entities(matching string: String) async throws -> [ExamenEntity] {
        let q = plano(string)
        return Examenes.todos().filter {
            plano($0.analito).contains(q)
            || plano($0.veredicto).contains(q)
            || plano($0.severidad).contains(q)
        }
    }

    func suggestedEntities() async throws -> [ExamenEntity] {
        Examenes.todos()
    }

    private func plano(_ s: String) -> String {
        s.folding(options: .diacriticInsensitive, locale: .current).lowercased()
    }
}
