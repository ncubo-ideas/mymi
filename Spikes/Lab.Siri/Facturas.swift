import Foundation

// THE MODULE'S PLACE, FAKED. In Gnothi the facts come from a module reading the person's
// own journal folder on this device -- the Host does not know what an invoice is, it only
// publishes what the module declares.
//
// The numbers are hardcoded ON PURPOSE. This spike asks whether iOS lets an assistant reach
// a tool of ours; bringing the real Host in would mix that with a much bigger question --
// how the .NET core reaches Swift -- and a failure would not say which half broke.
//
// They are the same numbers the Android spike used, so the two are comparable.
enum Facturas {

    /// The five invoices, itemised. They exist as separate entities and not as one total
    /// because the semantic route needs something to MATCH: "cuánto gasté en la farmacia"
    /// can only work if the pharmacy is a thing the index knows about.
    static func todas() -> [FacturaEntity] {
        [
            factura("f1", "Automercado",       "Compra de supermercado",      12_450.00, -3),
            factura("f2", "Farmacia La Bomba", "Medicamentos y farmacia",      3_200.50, -8),
            factura("f3", "Gasolinera Delta",  "Combustible",                  9_800.00, -11),
            factura("f4", "Ferretería EPA",    "Materiales de construcción",   4_100.00, -19),
            factura("f5", "Panadería Musmanni","Panadería",                    1_446.00, -22)
        ]
    }

    /// The facts, phrased the way the person would want to hear them. Note there is no
    /// "I am an assistant" framing: the module states what it knows, and whoever asked --
    /// Siri, a Shortcut, Claude -- decides what to do with it.
    static func total() -> String {
        let f = todas()
        let suma = f.reduce(0) { $0 + $1.monto }
        return "Tenés \(f.count) facturas registradas, por un total de \(FacturaEntity.plata(suma))."
    }

    /// Same facts as data, for the Shortcuts path. A shortcut chaining this into "Ask Claude"
    /// wants something a model can parse, not a sentence.
    static func totalComoDatos() -> String {
        let f = todas()
        let detalle = f
            .map { "{\"comercio\":\"\($0.comercio)\",\"monto\":\($0.monto)}" }
            .joined(separator: ",")
        return """
        {"hayDatos":true,"cantidadDeFacturas":\(f.count),\
        "total":\(f.reduce(0) { $0 + $1.monto }),"moneda":"CRC",\
        "facturas":[\(detalle)]}
        """
    }

    private static func factura(
        _ id: String, _ comercio: String, _ detalle: String, _ monto: Double, _ diasAtras: Int
    ) -> FacturaEntity {
        var e = FacturaEntity()
        e.id = id
        e.comercio = comercio
        // The description carries the amount as TEXT because Spotlight has no numeric
        // attribute meaning "amount" -- and a language model reads this anyway.
        e.detalle = "\(detalle) por \(FacturaEntity.plata(monto))"
        e.monto = monto
        e.fecha = Calendar.current.date(byAdding: .day, value: diasAtras, to: Date()) ?? Date()
        return e
    }
}
