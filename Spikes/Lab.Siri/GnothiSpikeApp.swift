import SwiftUI
import AppIntents
import CoreSpotlight

@main
struct GnothiSpikeApp: App {
    var body: some Scene {
        WindowGroup {
            ContentView()
        }
    }
}

// THE APP ITSELF IS ALMOST BESIDE THE POINT. Nobody is meant to use this screen: the test
// happens outside it -- in Siri, in Spotlight, in the Shortcuts app.
//
// What the screen IS for: saying exactly what to try and what to report, so the result is
// comparable and not "creo que funcionó".
struct ContentView: View {

    @State private var probado: Set<String> = []
    @State private var indexado: String = "indexando…"

    // TWO GROUPS, AND THE SPLIT IS THE WHOLE POINT.
    //
    // Naming the app works but nobody talks that way -- a person thinks about her invoices,
    // not about the app holding them. So the phrase tests are the FLOOR: they only prove the
    // channel carries a voice answer at all.
    //
    // The tests that never say "Gnothi" are the CEILING, and they are the ones that decide
    // whether this is worth building. If only the floor passes, we have a working mechanism
    // nobody would use.
    private let piso: [(String, String, String)] = [
        ("p1", "Siri contesta en voz alta",
         "«Cuánto gasté en Gnothi». Tiene que CONTESTAR, no abrir la app."),
        ("p2", "Lo mismo escrito",
         "Mantené el botón lateral y escribí la misma frase.")
    ]

    private let techo: [(String, String, String)] = [
        ("t1", "Sin nombrar la app",
         "«¿Cuánto gasté en la farmacia?» — sin decir Gnothi en ningún lado."),
        ("t2", "Por comercio",
         "«Buscá mis facturas del Automercado». Tampoco nombra la app."),
        ("t3", "Spotlight encuentra una factura",
         "Deslizá abajo en inicio y buscá «Automercado». ¿Aparece la factura, no solo la app?"),
        ("t4", "Spotlight por concepto",
         "Buscá «combustible». ¿Sale la de la gasolinera?")
    ]

    private let cadena: [(String, String, String)] = [
        ("c1", "La pieza existe en Atajos",
         "Atajos → buscar «Gnothi». Tienen que estar las acciones sin configurar nada."),
        ("c2", "Encadenar hacia Claude",
         "Atajo: 1) Gnothi «Facturas en datos» 2) «Ask Claude» con ese resultado."),
        ("c3", "Encadenar sin que salga nada",
         "El mismo atajo pero con «Use Model» → On Device. ¿Contesta sin red?")
    ]

    var body: some View {
        NavigationStack {
            List {
                Section {
                    VStack(alignment: .leading, spacing: 6) {
                        Text(Facturas.total()).font(.headline)
                        Text(indexado).font(.caption).foregroundStyle(.secondary)
                    }
                    .padding(.vertical, 2)
                } header: {
                    Text("El dato quemado")
                }

                grupo("EL PISO · nombrando la app",
                      "Esto casi seguro funciona. Solo prueba que el canal existe.",
                      piso)

                grupo("EL TECHO · sin nombrar la app",
                      "ESTA es la pregunta que decide. Si acá falla todo, tenemos un mecanismo que nadie usaría.",
                      techo)

                grupo("LA CADENA · la persona lo arma",
                      "Mide si Gnothi sirve de pieza para que ella lo combine con la IA que quiera.",
                      cadena)

                Section {
                    Text("Las de EL PISO y LA CADENA andan en cualquier iPhone con iOS 18. Las de EL TECHO dependen del índice semántico, y ahí sí puede pesar tener Apple Intelligence (15 Pro o 16+).")
                        .font(.caption).foregroundStyle(.secondary)
                } header: {
                    Text("Antes de sacar conclusiones")
                }
            }
            .navigationTitle("Gnothi · spike Siri")
        }
        .task {
            // Registers our phrases with the system. It normally happens on install; calling
            // it makes the first run deterministic, because a spike whose failure might just
            // be "todavía no lo registró" teaches nothing.
            GnothiShortcuts.updateAppShortcutParameters()

            // THE LINE THE CEILING DEPENDS ON. Without this the entities exist only inside
            // our process and Siri has nothing to match against -- every "sin nombrar la
            // app" test would fail for a reason that has nothing to do with the question.
            do {
                try await CSSearchableIndex.default().indexAppEntities(Facturas.todas())
                indexado = "\(Facturas.todas().count) facturas entregadas al índice de Spotlight."
            } catch {
                indexado = "NO se pudo indexar: \(error.localizedDescription)"
            }
        }
    }

    @ViewBuilder
    private func grupo(
        _ titulo: String, _ nota: String, _ items: [(String, String, String)]
    ) -> some View {
        Section {
            ForEach(items, id: \.0) { item in
                Button {
                    if probado.contains(item.0) { probado.remove(item.0) }
                    else { probado.insert(item.0) }
                } label: {
                    HStack(alignment: .top, spacing: 12) {
                        Image(systemName: probado.contains(item.0) ? "checkmark.circle.fill" : "circle")
                            .foregroundStyle(probado.contains(item.0) ? .teal : .secondary)
                        VStack(alignment: .leading, spacing: 3) {
                            Text(item.1).font(.subheadline.weight(.semibold))
                            Text(item.2).font(.caption).foregroundStyle(.secondary)
                        }
                    }
                    .padding(.vertical, 2)
                }
                .buttonStyle(.plain)
            }
        } header: {
            Text(titulo)
        } footer: {
            Text(nota)
        }
    }
}
