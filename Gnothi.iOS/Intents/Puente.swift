import Foundation
import FoundationModels

// EL PUENTE AL MODELO DEL TELEFONO. Lo que cruza de C# a Swift, y nada mas que esto.
//
// EL C# SIGUE SIENDO DUENO DE LA CONVERSACION. Cada paso manda el intercambio completo y el
// catalogo, y recibe UN paso -texto, o tool calls-. Es la forma exacta de IModel.Next(), y por
// eso ModelFoundation puede entrar por esa interfaz sin que AgentLoop se entere.
//
// POR QUE SENUELOS: Foundation Models ejecuta las tools el mismo, y nosotros necesitamos la
// INTENCION, no el efecto -las tools las corre el Dispatcher del Host, con sus permisos y su
// medidor-. Asi que se le pasan tools que capturan los argumentos y lanzan. Medido en
// Spikes/Lab.Puente: el modelo dice que quiere llamar y con que, y el trabajo no se hace.
//
// Y EL ESQUEMA SE ARMA EN RUNTIME. Los tools de Gnothi tienen parametros arbitrarios que el
// manifiesto declara, asi que no puede haber un `Arguments` compilado por tool:
// DynamicGenerationSchema + GenerationSchema(root:dependencies:) los construyen al vuelo.

@available(iOS 26.0, *)
private struct LlamadaCapturada: @unchecked Sendable {
	var nombre: String
	var argumentos: GeneratedContent
}

@available(iOS 26.0, *)
private final class Captura: @unchecked Sendable {
	var llamadas: [LlamadaCapturada] = []
}

private struct Detener: Error {}

// UN SOLO TIPO DE TOOL para todas. Arguments es GeneratedContent -que ya es
// ConvertibleFromGeneratedContent-, asi que el esquema no viaja en el tipo sino en `parameters`.
@available(iOS 26.0, *)
private struct Senuelo: Tool {
	typealias Arguments = GeneratedContent

	let name: String
	let description: String
	let parameters: GenerationSchema
	let captura: Captura

	func call(arguments: GeneratedContent) async throws -> String {
		captura.llamadas.append(LlamadaCapturada(nombre: name, argumentos: arguments))
		throw Detener()
	}
}

// ── el json de entrada ──────────────────────────────────────────────────────────────────────
//
//   conversacion  [ {"tipo":"asked","texto":...}
//                   {"tipo":"said","texto":...,"calls":[{"id","name","args":{...}}]}
//                   {"tipo":"returned","callId":...,"json":...} ]
//   tools         [ {"name","description","params":[{"name","description"}]} ]
//
// `params` es plano a proposito en esta primera version: los tools de Gnothi que llevan
// argumentos usan hoy un solo string cerrado -el analito, el termino-. Cuando aparezca uno
// anidado, DynamicGenerationSchema ya admite el arbol; lo que falta es traducir el json-schema
// del manifiesto, no la capacidad.

@available(iOS 26.0, *)
private func esquema(_ nombre: String, _ params: [[String: Any]]) -> GenerationSchema? {
	let propiedades = params.compactMap { p -> DynamicGenerationSchema.Property? in
		guard let n = p["name"] as? String else { return nil }
		return DynamicGenerationSchema.Property(
			name: n,
			description: p["description"] as? String,
			schema: DynamicGenerationSchema(type: String.self))
	}
	let raiz = DynamicGenerationSchema(name: nombre, description: nil, properties: propiedades)
	return try? GenerationSchema(root: raiz, dependencies: [])
}

@available(iOS 26.0, *)
private func entradas(_ conversacion: [[String: Any]]) -> [Transcript.Entry] {
	var salida: [Transcript.Entry] = []
	for e in conversacion {
		let tipo = e["tipo"] as? String ?? ""
		switch tipo {
		case "asked":
			salida.append(.prompt(Transcript.Prompt(segments: [
				.text(Transcript.TextSegment(content: e["texto"] as? String ?? ""))])))
		case "said":
			if let texto = e["texto"] as? String, !texto.isEmpty {
				salida.append(.response(Transcript.Response(assetIDs: [], segments: [
					.text(Transcript.TextSegment(content: texto))])))
			}
			if let calls = e["calls"] as? [[String: Any]], !calls.isEmpty {
				let cs = calls.compactMap { c -> Transcript.ToolCall? in
					guard let id = c["id"] as? String, let n = c["name"] as? String else { return nil }
					let args = (c["args"] as? [String: String]) ?? [:]
					return Transcript.ToolCall(
						id: id, toolName: n,
						arguments: GeneratedContent(properties: args.map { ($0.key, $0.value as any ConvertibleToGeneratedContent) },
													uniquingKeysWith: { a, _ in a }))
				}
				if !cs.isEmpty { salida.append(.toolCalls(Transcript.ToolCalls(cs))) }
			}
		case "returned":
			salida.append(.toolOutput(Transcript.ToolOutput(
				id: e["callId"] as? String ?? "",
				toolName: e["toolName"] as? String ?? "",
				segments: [.text(Transcript.TextSegment(content: e["json"] as? String ?? ""))])))
		default:
			break
		}
	}
	return salida
}

private func cadena(_ s: String) -> UnsafeMutablePointer<CChar> {
	// strdup y no un buffer propio: el C# la libera llamando a gnothi_fm_liberar.
	return strdup(s)!
}

// ── LO QUE VE EL C# ─────────────────────────────────────────────────────────────────────────

/// 0 no disponible · 1 dice estar disponible pero NO genera · 2 genera de verdad.
///
/// Son tres y no dos porque `availability` MIENTE: en iOS 26.2 devuelve `available` y la
/// generacion falla con ModelManagerError 1026. Medido en Lab.Siri/HALLAZGOS.md seccion 8. Un
/// `if isAvailable` pasa el simulador y se rompe donde importa, asi que el estado 2 se comprueba
/// GENERANDO.
@_cdecl("gnothi_fm_estado")
public func gnothi_fm_estado() -> Int32 {
	// El framework existe recien en 26; abajo de eso no hay modelo del telefono y punto.
	guard #available(iOS 26.0, *) else { return 0 }
	return estadoImpl()
}

/// POR QUE NO HAY MODELO, en palabras. El estado 0 los mete a todos en la misma bolsa y son tres
/// situaciones distintas: una se espera, otra se enciende y la tercera no tiene arreglo.
///
/// Costo medido: el 2026-09-17 el puente dijo `0` tres veces en un iPhone 17 Pro y cada una fue por
/// un motivo distinto -el idioma de Siri cruzado con el del aparato, y despues el salto a iOS 27-.
/// Sin el motivo, cada diagnostico fue media hora de hipotesis. La cadena la libera el C#.
@_cdecl("gnothi_fm_razon")
public func gnothi_fm_razon() -> UnsafeMutablePointer<CChar> {
	guard #available(iOS 26.0, *) else { return cadena("iOS < 26: no existe FoundationModels") }
	return cadena(razonImpl())
}

@available(iOS 26.0, *)
private func razonImpl() -> String {
	switch SystemLanguageModel.default.availability {
	case .available:
		return "disponible"
	case .unavailable(let motivo):
		switch motivo {
		case .deviceNotEligible:
			return "el aparato no es elegible para Apple Intelligence"
		case .appleIntelligenceNotEnabled:
			// El caso que mas cuesta: tambien cae aca el idioma de Siri cruzado con el del
			// aparato, que Apple documenta y que no se ve como un interruptor apagado.
			return "Apple Intelligence no esta encendido para este aparato (revisar tambien que el idioma de Siri y el del aparato coincidan)"
		case .modelNotReady:
			return "el modelo todavia se esta descargando o preparando: es cuestion de esperar"
		@unknown default:
			return "no disponible, por un motivo que esta version del puente no conoce"
		}
	@unknown default:
		return "availability devolvio algo que esta version del puente no conoce"
	}
}

@available(iOS 26.0, *)
private func estadoImpl() -> Int32 {
	guard SystemLanguageModel.default.isAvailable else { return 0 }
	let sem = DispatchSemaphore(value: 0)
	var genera = false
	Task {
		do { _ = try await LanguageModelSession().respond(to: "di ok"); genera = true }
		catch { genera = false }
		sem.signal()
	}
	// Aca vencerse SI significa lo que dice: si la primera generacion no vuelve en 200 s, este
	// aparato no sirve para contestarle a nadie. El 1 no miente.
	_ = sem.wait(timeout: .now() + 200)
	return genera ? 2 : 1
}

/// Un paso. Devuelve {"texto":...,"calls":[{"id","name","args":{...}}]} o {"error":...}.
/// NO llamar desde el hilo principal: adentro espera a que termine la generacion.
@_cdecl("gnothi_fm_paso")
public func gnothi_fm_paso(
	_ conversacionJson: UnsafePointer<CChar>,
	_ toolsJson: UnsafePointer<CChar>,
	_ instrucciones: UnsafePointer<CChar>
) -> UnsafeMutablePointer<CChar> {
	guard #available(iOS 26.0, *) else {
		return cadena("{\"error\":\"este telefono no llega a iOS 26: no hay modelo local\"}")
	}
	return pasoImpl(conversacionJson, toolsJson, instrucciones)
}

@available(iOS 26.0, *)
private func pasoImpl(
	_ conversacionJson: UnsafePointer<CChar>,
	_ toolsJson: UnsafePointer<CChar>,
	_ instrucciones: UnsafePointer<CChar>
) -> UnsafeMutablePointer<CChar> {

	let conv = (try? JSONSerialization.jsonObject(with: Data(String(cString: conversacionJson).utf8))) as? [[String: Any]] ?? []
	let decl = (try? JSONSerialization.jsonObject(with: Data(String(cString: toolsJson).utf8))) as? [[String: Any]] ?? []
	let sistema = String(cString: instrucciones)

	let captura = Captura()
	let tools: [any Tool] = decl.compactMap { d in
		guard let n = d["name"] as? String,
			  let e = esquema(n, d["params"] as? [[String: Any]] ?? []) else { return nil }
		return Senuelo(name: n, description: d["description"] as? String ?? "",
					   parameters: e, captura: captura)
	}

	var previas = entradas(conv)
	if !sistema.isEmpty {
		previas.insert(.instructions(Transcript.Instructions(
			segments: [.text(Transcript.TextSegment(content: sistema))], toolDefinitions: [])), at: 0)
	}

	// La ultima entrada es la que dispara el paso; el resto es historia.
	let ultima = previas.popLast()
	let sesion = LanguageModelSession(tools: tools, transcript: Transcript(entries: previas))

	var respuesta: [String: Any] = [:]
	let sem = DispatchSemaphore(value: 0)

	Task {
		do {
			var disparo = "Continua."
			if case .prompt(let p)? = ultima, case .text(let t)? = p.segments.first { disparo = t.content }
			if case .toolOutput? = ultima { disparo = "Contestale a la persona con esos datos." }

			if case .toolOutput(let o)? = ultima {
				// El resultado que calculo el Host entra como historia, no como pregunta.
				let conHistoria = Transcript(entries: previas + [.toolOutput(o)])
				let s2 = LanguageModelSession(tools: tools, transcript: conHistoria)
				let r = try await s2.respond(to: disparo)
				respuesta["texto"] = r.content
			} else {
				let r = try await sesion.respond(to: disparo)
				respuesta["texto"] = r.content
			}
		} catch is Detener {
			respuesta["calls"] = captura.llamadas.map { l in
				["id": UUID().uuidString, "name": l.nombre, "args": l.argumentos.jsonString]
			}
		} catch {
			if captura.llamadas.isEmpty {
				respuesta["error"] = "\(type(of: error)): \(error)"
			} else {
				respuesta["calls"] = captura.llamadas.map { l in
					["id": UUID().uuidString, "name": l.nombre, "args": l.argumentos.jsonString]
				}
			}
		}
		sem.signal()
	}

	// EL TECHO, Y POR QUE ES TAN ALTO. Medido: una pregunta tardo 354 s de punta a punta -el
	// guardrail de Apple recien termino de revisar la salida a los 5:54-. Con el techo en 300 s
	// esto se vencia, devolvia {} y la respuesta real llegaba tarde a un semaforo que ya nadie
	// esperaba. El modelo del telefono es lento y variable: 18 s la buena, 235 s la de tres
	// tools, 354 s esta.
	//
	// Y VENCERSE TIENE QUE DOLER. Antes se devolvia {} a secas, que del lado del C# se lee como
	// "el modelo no dijo nada": sin error, sin log, pantalla en blanco. Eso costo una prueba
	// perdida y media hora de diagnostico. Ahora dice que se vencio y cuanto espero.
	if sem.wait(timeout: .now() + 900) == .timedOut {
		return cadena("{\"error\":\"el modelo del telefono no contesto en 900 s\"}")
	}

	let datos = (try? JSONSerialization.data(withJSONObject: respuesta)) ?? Data("{}".utf8)
	return cadena(String(data: datos, encoding: .utf8) ?? "{}")
}

@_cdecl("gnothi_fm_liberar")
public func gnothi_fm_liberar(_ p: UnsafeMutablePointer<CChar>?) {
	if let p { free(p) }
}
