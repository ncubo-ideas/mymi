import UIKit
import FoundationModels

// LAS DOS TOOLS, con la forma EXACTA de las de Gnothi. El payload es de pie -- el cableado al
// Dispatcher real es el paso siguiente y ya existe: el Host llega a los modulos.
// Lo que esto mide es si el modelo LAS LLAMA y si CRUZA dos modulos en una respuesta.

struct Tendencia: Tool {
    let name = "examenes_evaluar_tendencia"
    let description = "Dice si un analito de la persona viene subiendo, bajando o estable, mirando toda su historia y no el ultimo resultado."
    @Generable struct Arguments {
        @Guide(description: "Cual analito. Valores validos: AntigenoProstatico, ColesterolTotal, ColesterolLdl, Hemoglobina.")
        var analito: String
    }
    func call(arguments a: Arguments) async throws -> String {
        print("TOOL-LLAMADA: examenes_evaluar_tendencia(\(a.analito))")
        guard a.analito.lowercased() == "colesteroltotal" else {
            return #"{"hayDatos":false,"analito":"\#(a.analito)"}"#
        }
        return """
        {"hayDatos":true,"analito":"Colesterol total","direccion":"subiendo",
         "primero":{"fecha":"2024-03-11","valor":182},"ultimo":{"fecha":"2026-08-21","valor":228},
         "unidad":"mg/dL","umbral":200,"exigeAtencion":true,
         "lectura":"tu colesterol total viene subiendo: de 182 a 228 mg/dL en 29 meses, y paso el umbral de 200"}
        """
    }
}

struct Consumo: Tool {
    let name = "compras_consumo_de"
    let description = "Cuanto viene comprando la persona de un producto o categoria, segun las facturas de supermercado que su app registro. Busca por NOMBRE DE ALIMENTO, no por nombre de analito ni de enfermedad: no existe un producto llamado colesterol. Si lo que te interesa es una relacion con un examen de sangre, llamala una vez por cada alimento plausible -por ejemplo manteca, embutidos, queso, frituras- y despues compara."
    @Generable struct Arguments {
        @Guide(description: "Un NOMBRE DE ALIMENTO en singular: manteca, embutidos, queso, fritura, gaseosa, verdura, pescado. Nunca el nombre de un analito ni de una enfermedad.")
        var termino: String
    }
    func call(arguments a: Arguments) async throws -> String {
        print("TOOL-LLAMADA: compras_consumo_de(\(a.termino))")
        let t = a.termino.lowercased()
        let saturados = ["manteca","embutido","embutidos","queso","fiambre","frito","fritura","chorizo","tocino","crema","achuras","carne"]
        if saturados.contains(where: { t.contains($0) }) {
            return """
            {"hayDatos":true,"termino":"\(a.termino)","unidadesPorMes":9.4,"mesesConDatos":18,
             "tendencia":"al alza","cambioContraElPrimerAno":"+61%",
             "lectura":"venis comprando \(a.termino) 9,4 veces por mes, 61% mas que en tu primer ano registrado"}
            """
        }
        return #"{"hayDatos":false,"termino":"\#(a.termino)","lectura":"no aparece en tus compras registradas"}"#
    }
}


// LA TERCERA TOOL: el cruce ya viene NOMBRADO. El modelo no tiene que saber que alimentos suben
// el colesterol -- ya medimos que no lo sabe-; solo tiene que elegir esta.
struct GrasasSaturadas: Tool {
    let name = "compras_consumo_de_grasas_saturadas"
    let description = "Cuanto viene comprando la persona de los alimentos altos en grasas saturadas -manteca, embutidos, quesos, frituras- que son los que la evidencia asocia al colesterol en sangre. Usala cuando pregunte si su colesterol tiene que ver con lo que come o compra."
    @Generable struct Arguments {}
    func call(arguments: Arguments) async throws -> String {
        print("TOOL-LLAMADA: compras_consumo_de_grasas_saturadas()")
        return """
        {"hayDatos":true,"categorias":[
          {"que":"manteca","unidadesPorMes":4.1,"tendencia":"al alza","cambio":"+72%"},
          {"que":"embutidos","unidadesPorMes":9.4,"tendencia":"al alza","cambio":"+61%"},
          {"que":"queso","unidadesPorMes":6.0,"tendencia":"estable","cambio":"+4%"}],
         "mesesConDatos":18,
         "lectura":"en 18 meses subiste manteca 72% y embutidos 61%; el queso quedo igual"}
        """
    }
}

@main
final class AD: UIResponder, UIApplicationDelegate {
    var window: UIWindow?
    func application(_ app: UIApplication, didFinishLaunchingWithOptions o: [UIApplication.LaunchOptionsKey: Any]? = nil) -> Bool {
        window = UIWindow(frame: UIScreen.main.bounds)
        window?.rootViewController = UIViewController()
        window?.makeKeyAndVisible()

        let pregunta = ProcessInfo.processInfo.environment["PROMPT"]
            ?? "¿Puede que mi colesterol tenga algo que ver con lo que compro?"

        Task {
            let s = LanguageModelSession(tools: [Tendencia(), GrasasSaturadas()]) {
                """
                Sos el asistente de Gnothi. Los datos que devuelven las herramientas son los datos \
                REALES de la persona que te pregunta, leidos de los registros que sus instituciones \
                le entregaron y que viven en su propio telefono. No vienen de ningun servidor.
                Contestale sobre SUS numeros, en su idioma, en prosa corrida y sin markdown. \
                El veredicto clinico ya viene hecho en el campo lectura y no es tuyo: no diagnostiques \
                ni recomendes tratamiento. Si dos herramientas distintas se relacionan, deci como se \
                relacionan sin afirmar una causa que los datos no prueben.
                """
            }
            print("CRUCE: pregunta -> \(pregunta)")
            do {
                let t0 = Date()
                let r = try await s.respond(to: pregunta)
                print("CRUCE: \(String(format: "%.0f", Date().timeIntervalSince(t0)))s")
                print("CRUCE-RESPUESTA: \(r.content)")
            } catch {
                print("CRUCE-FALLO: \(String(describing: error).prefix(300))")
            }
        }
        return true
    }
}
