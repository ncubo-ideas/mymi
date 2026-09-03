import UIKit
import FoundationModels

// EL DISEÑO DE VERDAD: el C# es dueño de la conversacion, y el Swift ARMA EL TRANSCRIPT DESDE
// CERO en cada paso. No se le pide nada al estado de la sesion -que tras un aborto queda vacio-.
//
//   paso 1  intencion   senuelo que captura la tool call y aborta  -> Said(calls: [...])
//   paso 2  reanudar    transcript fabricado con el resultado que el C# calculo -> Said(text)

struct Centinela: Error {}
final class Espia: @unchecked Sendable { var visto: [String] = [] }
let espia = Espia()

struct Senuelo: Tool {
    let name = "examenes_evaluar_tendencia"
    let description = "Dice si un analito de la persona viene subiendo, bajando o estable."
    @Generable struct Arguments {
        @Guide(description: "Cual analito. Valores validos: AntigenoProstatico, ColesterolTotal, ColesterolLdl, Hemoglobina.")
        var analito: String
    }
    func call(arguments a: Arguments) async throws -> String {
        espia.visto.append(a.analito)
        throw Centinela()
    }
}

@main
final class AD: UIResponder, UIApplicationDelegate {
    var window: UIWindow?
    func application(_ a: UIApplication, didFinishLaunchingWithOptions o: [UIApplication.LaunchOptionsKey: Any]? = nil) -> Bool {
        window = UIWindow(frame: UIScreen.main.bounds); window?.rootViewController = UIViewController(); window?.makeKeyAndVisible()

        let pregunta = "¿Como viene mi colesterol total?"
        let hechos = """
        {"hayDatos":true,"analito":"Colesterol total","direccion":"subiendo","primero":{"valor":182},"ultimo":{"valor":228},"unidad":"mg/dL","umbral":200,"lectura":"tu colesterol total viene subiendo: de 182 a 228 mg/dL en 29 meses, y paso el umbral de 200"}
        """

        Task {
            // ── PASO 1 · la intencion
            let s1 = LanguageModelSession(tools: [Senuelo()]) { "Sos el asistente de Gnothi. Prosa corta, sin markdown." }
            do { _ = try await s1.respond(to: pregunta); print("PUENTE: 1 no aborto") }
            catch { print("PUENTE: 1 intercepto -> \(espia.visto)") }

            guard let analito = espia.visto.first else { print("PUENTE: 1 NO capturo nada"); return }

            // ── PASO 2 · el transcript armado a mano, con el resultado del "C#"
            let idLlamada = UUID().uuidString
            let entradas: [Transcript.Entry] = [
                .prompt(Transcript.Prompt(segments: [.text(Transcript.TextSegment(
                    content: "Sos el asistente de Gnothi. Prosa corta, sin markdown.\n\n\(pregunta)"))])),
                .toolCalls(Transcript.ToolCalls([Transcript.ToolCall(
                    id: idLlamada, toolName: "examenes_evaluar_tendencia",
                    arguments: GeneratedContent(properties: ["analito": analito]))])),
                .toolOutput(Transcript.ToolOutput(
                    id: idLlamada, toolName: "examenes_evaluar_tendencia",
                    segments: [.text(Transcript.TextSegment(content: hechos))])),
            ]

            let antes = espia.visto.count
            let s2 = LanguageModelSession(tools: [Senuelo()], transcript: Transcript(entries: entradas))
            do {
                let r = try await s2.respond(to: "Contestale a la persona con esos datos.")
                print("PUENTE: 2 OK · el senuelo se volvio a llamar = \(espia.visto.count > antes)")
                print("PUENTE-RESPUESTA: \(r.content)")
            } catch {
                print("PUENTE: 2 fallo -> \(type(of: error)): \(error)")
            }
        }
        return true
    }
}
