import AppIntents
import CoreSpotlight

// THE PART THAT MATTERS, and it is here because the phrase test alone answers the wrong
// question.
//
// A phrase like "cuánto gasté en Gnothi" WORKS, but nobody talks like that. The person
// thinks about her invoices, not about the app that holds them -- asking for the app by
// name is like asking Excel how much you spent. Apple requires the app name in phrases to
// disambiguate, so that route has a ceiling built into it.
//
// The route WITHOUT the app name has two doors:
//
//   App Schemas         Apple's curated domains (Mail, Photos, Messages...).
//                       Gnothi fits none of them. Same wall as Android's schemaCategory.
//                       CLOSED, and not by choice.
//
//   Spotlight's index    an app contributes ENTITIES, and Siri draws on that index for
//                       personal context. No phrase, no app name, no curated domain.
//                       THIS IS THE ONE WORTH MEASURING.
//
// On Android the equivalent (AppSearch) was left in note 23 as "unverified: we don't even
// know who the system's global querier is". On iOS it is documented, so we can actually
// find out.

// ONE INVOICE, as something the system can index and Siri can reason about.
//
// The @Property(indexingKey:) mappings are the whole trick: they say which Spotlight
// attribute each of our fields becomes. That is what turns a private struct into something
// the system's semantic index can match against a question we never anticipated.
struct FacturaEntity: AppEntity, IndexedEntity {

    static let typeDisplayRepresentation: TypeDisplayRepresentation = "Factura"

    // `FacturaEntity()` has to exist so Facturas.swift can build one field by field.
    //
    // The @Property ones carry NO default, and that is not a style choice: EntityProperty
    // publishes no init(wrappedValue:) alongside indexingKey:, so `var comercio: String = ""`
    // does not compile at all. Swift initialises the backing storage from the attribute's
    // own arguments and leaves those properties out of the memberwise init -- which is
    // exactly what we want, since whoever builds the invoice assigns them before use.
    var id: String = ""

    @Property(indexingKey: \.displayName)
    var comercio: String

    @Property(indexingKey: \.contentDescription)
    var detalle: String

    // Not indexed: Spotlight has no numeric attribute that means "amount", and forcing one
    // would be inventing meaning. The amount travels inside `detalle` as text, which is what
    // a language model reads anyway.
    var monto: Double = 0

    var fecha: Date = .now

    // WHAT SIRI READS BACK. If the semantic route works at all, this is the shape of what
    // the person hears -- so it is written for an ear, not for a log.
    var displayRepresentation: DisplayRepresentation {
        DisplayRepresentation(
            title: "\(comercio)",
            subtitle: "\(Self.plata(monto)) · \(Self.dia(fecha))"
        )
    }

    static var defaultQuery = FacturaQuery()

    static func plata(_ m: Double) -> String {
        let f = NumberFormatter()
        f.numberStyle = .decimal
        f.locale = Locale(identifier: "es_CR")
        f.minimumFractionDigits = 2
        return "₡" + (f.string(from: NSNumber(value: m)) ?? "\(m)")
    }

    static func dia(_ d: Date) -> String {
        let f = DateFormatter()
        f.locale = Locale(identifier: "es_CR")
        f.dateStyle = .medium
        return f.string(from: d)
    }
}

// HOW THE SYSTEM ASKS US FOR INVOICES.
//
// EntityStringQuery -- and not the plainer EntityQuery -- is deliberate: it adds
// entities(matching: String), which is how Spotlight and Siri hand us a piece of what the
// person said and ask "is any of this yours?". Without it we can only answer by id, and the
// natural-language route never gets a chance.
struct FacturaQuery: EntityStringQuery {

    func entities(for identifiers: [String]) async throws -> [FacturaEntity] {
        Facturas.todas().filter { identifiers.contains($0.id) }
    }

    func entities(matching string: String) async throws -> [FacturaEntity] {
        let q = string.folding(options: .diacriticInsensitive, locale: .current).lowercased()
        return Facturas.todas().filter {
            $0.comercio.folding(options: .diacriticInsensitive, locale: .current)
                .lowercased().contains(q)
            || $0.detalle.folding(options: .diacriticInsensitive, locale: .current)
                .lowercased().contains(q)
        }
    }

    // What the system offers before the person has typed anything -- in Shortcuts pickers
    // and in Spotlight suggestions.
    func suggestedEntities() async throws -> [FacturaEntity] {
        Facturas.todas()
    }
}

// THE INTENT WITH NO APP NAME IN IT.
//
// This one declares NO phrase at all, on purpose. If Siri reaches it, it is because the
// semantic index matched the person's own words to our entities -- which is exactly the
// thing we could not measure on Android.
struct BuscarFacturasIntent: AppIntent {

    static let title: LocalizedStringResource = "Buscar en mis facturas"

    static let description = IntentDescription(
        "Busca entre las facturas de la persona por comercio, monto o fecha, y responde con lo que encontró."
    )

    static let openAppWhenRun = false

    @Parameter(title: "Qué buscar")
    var texto: String

    static var parameterSummary: some ParameterSummary {
        Summary("Buscar \(\.$texto) en mis facturas")
    }

    func perform() async throws -> some IntentResult & ProvidesDialog & ReturnsValue<String> {
        let encontradas = try await FacturaQuery().entities(matching: texto)

        let respuesta: String
        if encontradas.isEmpty {
            // An honest empty answer, not an apology. The module says what it knows.
            respuesta = "No encontré facturas que coincidan con «\(texto)»."
        } else {
            let suma = encontradas.reduce(0) { $0 + $1.monto }
            let lista = encontradas
                .map { "\($0.comercio) por \(FacturaEntity.plata($0.monto))" }
                .joined(separator: ", ")
            respuesta = encontradas.count == 1
                ? "Tenés una: \(lista)."
                : "Tenés \(encontradas.count), que suman \(FacturaEntity.plata(suma)): \(lista)."
        }

        return .result(value: respuesta, dialog: IntentDialog(stringLiteral: respuesta))
    }
}
