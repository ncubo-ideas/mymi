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
         "Mantené el botón lateral y escribí la misma frase."),
        ("p3", "Lo mismo, pero exámenes",
         "«Cómo salieron mis exámenes en Gnothi». Si p1 pasa y esta falla, el problema es del dato y no del canal.")
    ]

    private let techo: [(String, String, String)] = [
        ("t1", "Sin nombrar la app",
         "«¿Cuánto gasté en la farmacia?» — sin decir Gnothi en ningún lado."),
        ("t2", "Por comercio",
         "«Buscá mis facturas del Automercado». Tampoco nombra la app."),
        ("t3", "Spotlight encuentra una factura",
         "Deslizá abajo en inicio y buscá «Automercado». ¿Aparece la factura, no solo la app?"),
        ("t4", "Spotlight por concepto",
         "Buscá «combustible». ¿Sale la de la gasolinera? MEDIDO: no sale, aunque esa palabra esté impresa en el subtítulo del propio resultado."),
        ("t5", "Spotlight por analito",
         "Buscá «colesterol». Está en el título del examen, así que por lo medido en t3 DEBERÍA salir."),
        ("t6", "Spotlight por veredicto",
         "Buscá «vigilar» o «subió». Está en el veredicto, no en el título: por lo medido en t4 NO debería salir. Este par confirma el hallazgo en una segunda entidad."),
        ("t7", "Dos negocios, un índice",
         "Buscá «Gnothi» en Spotlight. ¿Aparecen facturas Y exámenes sin confundirse? Mide si el mecanismo escala a un aparato con varios negocios."),
        ("t8", "Sin nombrar la app, exámenes",
         "«¿Cómo salió mi colesterol?» — sin decir Gnothi. Es el techo de exámenes, y acá el analito SÍ es lo que la persona diría.")
    ]

    private let cadena: [(String, String, String)] = [
        ("c1", "La pieza existe en Atajos",
         "Atajos → buscar «Gnothi». Tienen que estar las acciones sin configurar nada."),
        ("c2", "Encadenar hacia Claude",
         "Atajo: 1) Gnothi «Facturas en datos» 2) «Ask Claude» con ese resultado."),
        ("c3", "Encadenar sin que salga nada",
         "El mismo atajo pero con «Use Model» → On Device. ¿Contesta sin red?"),
        ("c4", "¿El encuadre arregla la respuesta?",
         "Repetir c2. Antes Claude contestaba «the math checks out… what do you want done with it?» porque recibía JSON pelado. Ahora los datos viajan con su marco: ¿contesta sobre las facturas, y en español?"),
        ("c5", "La persona pregunta lo suyo",
         "Atajo: 1) Gnothi «Preguntar sobre mis facturas» escribiendo «¿en qué gasté más?» 2) «Ask Claude». La pregunta va al final, que es lo que un modelo pesa más."),
        ("c6", "¿Se acabaron las tablas en crudo?",
         "Repetir c4. En la corrida anterior Claude armó una tabla de markdown y la alerta la mostró con los pipes a la vista. Ahora va la superficie pegada: ¿contesta en prosa, sin | ni ---?"),
        ("c7", "Exámenes hacia Claude",
         "Atajo: 1) Gnothi «Exámenes en datos» 2) «Ask Claude». Lo que se mide NO es si contesta: es si RESPETA el veredicto del expediente en vez de diagnosticar por su cuenta."),
        ("c8", "Preguntar sobre mis exámenes",
         "Atajo: 1) Gnothi «Preguntar sobre mis exámenes» escribiendo «¿tengo algo de qué preocuparme?» 2) «Ask Claude». La pregunta que más invita a diagnosticar.")
    ]

    // EL PUENTE · lo unico de este spike que NO tiene los datos quemados.
    //
    // Todo lo de arriba contesta con numeros escritos en Swift, y estuvo bien: la pregunta era
    // si iOS deja que un asistente alcance una tool nuestra. Ya sabemos que si.
    //
    // Estas tres miden la siguiente: si puede contestar con las facturas y los examenes DE
    // VERDAD, los que el modulo leyo del journal de la persona. El Host es una app aparte, en
    // .NET, y no se pueden llamar -- asi que se dejan un archivo en el contenedor compartido.
    //
    // r3 ES LA IMPORTANTE Y LA QUE MAS FACIL SE OLVIDA: una foto vieja podria seguir
    // contestando por un modulo que la persona ya apago. Si eso pasa, el consentimiento tiene
    // un agujero y no sirve de nada que la compuerta funcione en las otras dos puertas.
    private let real: [(String, String, String)] = [
        ("r1", "¿Existe el contenedor compartido?",
         "Corré el atajo «Lo que Gnothi tiene de verdad» SIN abrir la app del Host. Tiene que decir si falta el App Group o si solo falta la foto. Los dos mensajes son distintos a propósito."),
        ("r2", "Los números de verdad",
         "Abrí la app del Host de Gnothi una vez, volvé y repetí r1. ¿Salen las facturas reales del journal, con la fecha de actualización?"),
        ("r3", "Apagar un módulo lo saca de la foto",
         "En la app del Host, apagá el switch de facturas. Sin tocar nada más, repetí r1. Facturas NO puede aparecer. Si aparece, el consentimiento tiene un agujero.")
    ]

    // CON APPLE INTELLIGENCE · el techo de verdad.
    //
    // Todo el techo de arriba se midio en un 13 Pro, que NO puede tener Apple Intelligence -- la
    // pieza que enrutaria voz -> indice semantico -> app no estaba en el telefono. Por eso t3 y
    // t4 quedaron "no concluyentes" y t9 "no medible". Estas son las mismas preguntas, con el
    // aparato que si las puede contestar.
    //
    // LA CONDICION QUE APPLE EXIGE Y QUE CUESTA UNA TARDE SI NO SE SABE: idioma del aparato =
    // idioma de Siri, y en una variante soportada. La lista de Apple trae espanol de Mexico,
    // Espana y US. Costa Rica no figura. Es la explicacion mas probable de que en el simulador
    // no aparezca el interruptor aunque la Mac tenga Apple Intelligence activo.
    private let conAI: [(String, String, String)] = [
        ("a0", "Antes de nada: el aparato y el idioma",
         "iPhone 15 Pro o 16+, iOS 26, 7 GB libres. Ajustes → Apple Intelligence & Siri → encendido. IDIOMA DEL APARATO = IDIOMA DE SIRI, en español de México, España o US — no Costa Rica. Wi-Fi y cargador mientras baja el modelo."),
        ("a1", "t3 otra vez: sin nombrar la app",
         "«¿Cuánto gasté en la farmacia?». En el 13 Pro cayó en búsqueda web. ¿Con Apple Intelligence llega a Gnothi?"),
        ("a2", "t4 otra vez: por comercio",
         "«Buscá mis facturas del Automercado». En el 13 Pro se lo llevó Apple Cash. ¿Ahora nos elige?"),
        ("a3", "t8b: exámenes sin nombrar la app",
         "«¿Cómo salió mi colesterol?». El analito es justo lo que la persona SÍ diría. Es la que más importa del techo."),
        ("a4", "t6/t6b otra vez — pero preguntándole a SIRI, no a Spotlight",
         "«¿Tengo algo que vigilar en mis exámenes?» «¿Gasté en combustible?». Lo que t6/t6b midieron fue Spotlight como buscador de TEXTO, y eso puede seguir roto sin que importe. Lo que falta medir es que SIRI consulte el índice semánticamente (IOS27.md). El indexingKey ya estaba puesto."),
        ("a5", "t9: «Use Model» sin red",
         "Atajo: Gnothi «Facturas en datos» → «Use Model» → On Device. Modo avión. ¿Contesta? Si sí, es la Puerta 2 con el modelo de Apple: gratis, sin llave, sin que el dato salga."),
        ("a6", "Antes de pedir teléfono: el simulador",
         "Tu Mac cumple lo que Apple pide («Simulators use the host Mac's model»). Poné el simulador en Ajustes → General → Idioma y región → español (México), y Siri en el mismo idioma. Si aparece el interruptor de Apple Intelligence, a5 se puede correr ahí. Es un ajuste, no un aparato.")
    ]

    private var fotoEnPantalla: String {
        switch Instantanea.leer() {
        case .sinContenedor:
            return "SIN CONTENEDOR: el App Group '\(Instantanea.grupo)' no esta aprovisionado."
        case .sinFoto(let carpeta):
            return "SIN FOTO. Buscada en: \(carpeta)/\(Instantanea.archivo)"
        case .ilegible(let ruta):
            return "HAY ARCHIVO en \(ruta) pero no es json legible."
        case .foto(let json, let escritaEn):
            let cuando = escritaEn.map { ISO8601DateFormatter().string(from: $0) } ?? "sin fecha"
            return "FOTO ENCONTRADA · \(json.count) caracteres · escrita \(cuando)"
        }
    }

    var body: some View {
        NavigationStack {
            List {
                Section {
                    VStack(alignment: .leading, spacing: 6) {
                        Text(Facturas.total()).font(.headline)
                        Text(Examenes.estado()).font(.headline)
                        Text(indexado).font(.caption).foregroundStyle(.secondary)
                    }
                    .padding(.vertical, 2)
                } header: {
                    Text("El dato quemado")
                }

                // LA FOTO, DICHA EN PANTALLA. Hasta aca solo las intents sabian si habia foto, y
                // se enteraba quien le preguntaba a Siri. En la Mac nadie escribe la foto (la
                // escribe Gnothi.iOS en el telefono), se copia a mano, y «no la encuentra» sin
                // la ruta que miro la app cuesta una tarde. Se lee al pintar, asi que relanzar
                // la app es releer.
                Section {
                    Text(fotoEnPantalla).font(.caption).textSelection(.enabled)
                } header: {
                    Text("El dato real · la foto del Host")
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

                grupo("EL PUENTE · datos de verdad",
                      "Necesita la OTRA app: Gnothi.iOS, el Host de .NET. Sin ella instalada y corrida al menos una vez, estas tres no miden nada.",
                      real)

                grupo("CON APPLE INTELLIGENCE · el techo de verdad",
                      "Las de arriba corrieron en un 13 Pro, que no puede tener Apple Intelligence. Éstas necesitan un 15 Pro o 16+ prestado — salvo a5, que puede andar en el simulador si el idioma coincide (a6).",
                      conAI)

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
                // DOS TIPOS DE ENTIDAD, DOS LLAMADAS. No hay una version que tome entidades
                // mezcladas: indexAppEntities es generico sobre UN tipo, asi que van dos.
                //
                // Que las dos entren al mismo indice es en si una prueba (t7): si Spotlight
                // devuelve facturas y examenes sin confundirlos, el mecanismo escala a un
                // aparato con varios negocios adentro, que es como Gnothi va a vivir.
                try await CSSearchableIndex.default().indexAppEntities(Facturas.todas())
                try await CSSearchableIndex.default().indexAppEntities(Examenes.todos())
                indexado = "\(Facturas.todas().count) facturas y \(Examenes.todos().count) "
                         + "exámenes entregados al índice de Spotlight."
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
